using System;
using System.Collections.Generic;

public sealed class PrologueDollWakeFlowResult {
    public readonly List<PrologueOpeningNodePlayback> Nodes = new List<PrologueOpeningNodePlayback>();
    public readonly List<string> VisualSequence = new List<string>();
    public readonly List<string> CharacterSequence = new List<string>();
    public readonly List<string> PrologueActionIDs = new List<string>();
    public readonly List<string> StatusCardIDs = new List<string>();
    public readonly List<string> Errors = new List<string>();
    public NarrativeStateStore State;
    public int LinesShown;
    public int CommandsExecuted;
    public int UnlockUiRequests;
    public int LayerConfirmRequests;

    public bool Success {
        get {
            return Errors.Count == 0
                && State != null
                && State.GetFlag("No0Started")
                && State.GetFlag("No0WakeDialogueSeen")
                && State.GetFlag("FirstStatusShown")
                && State.GetFlag("FirstCoreWiped")
                && !State.GetFlag("FirstDiveUnlocked")
                && !State.GetFlag("Layer1ConfirmOpened");
        }
    }
}

public sealed class PrologueDollWakeNarrativeFlow {
    public const string PrologueActionEventType = "PrologueActionClicked";
    public const string NarrativeFlagSetEventType = "NarrativeFlagSet";
    public const string PrologueContext = "prologue";
    public const string WorkshopContext = "workshop";
    public const string StartDollActionID = "start_doll";
    public const string WipeCoreActionID = "wipe_core";
    public const string StartDollNodeID = "T0_01A_StartDoll";
    public const string No0WakeNodeID = "T0_01A_No0Wake";
    public const string FirstStatusCardNodeID = "T0_01A_FirstStatusCard";
    public const string FirstCoreWipeNodeID = "T0_01A_FirstCoreWipe";

    private readonly NarrativeConfigDatabase _database;
    private readonly INarrativeScriptRuntime _runtime;
    private readonly NarrativeStateStore _state;
    private readonly INarrativeCommandSink _externalSink;

    public PrologueDollWakeNarrativeFlow(
        NarrativeConfigDatabase database,
        INarrativeScriptRuntime runtime = null,
        NarrativeStateStore state = null,
        INarrativeCommandSink externalSink = null) {
        _database = database ?? new NarrativeConfigDatabase();
        _runtime = runtime ?? new P3YarnLikeNarrativeRuntime();
        _state = state ?? new NarrativeStateStore();
        _externalSink = externalSink;
    }

    public PrologueDollWakeFlowResult PlayStartDollToCoreWipe() {
        PrologueDollWakeFlowResult result = new PrologueDollWakeFlowResult {
            State = _state
        };

        _state.SetFlag("PrologueStarted");
        _state.SetFlag("DebtNoticeSeen");
        _state.SetFlag("RepairNoteSeen");
        _state.SetFlag("LastCoreShardSeen");
        _state.SetFlag("No0Found");

        RecordingNarrativeCommandSink sink = new RecordingNarrativeCommandSink(result, _externalSink);
        NarrativeTriggerService triggerService = new NarrativeTriggerService(_database, _state);
        NarrativeScheduler scheduler = new NarrativeScheduler(_state);
        NarrativeCommandBridge bridge = new NarrativeCommandBridge(_database, _state, sink);
        NarrativePlaybackService playback = new NarrativePlaybackService(_database, _runtime, _state, scheduler, bridge);

        PlayTriggeredNode(
            triggerService,
            scheduler,
            playback,
            NarrativeEventContext.Create(PrologueActionEventType, PrologueContext).WithValue("action", StartDollActionID),
            StartDollNodeID,
            result);

        int startDollActionCountAfterClick = result.PrologueActionIDs.Count;

        PlayTriggeredNode(
            triggerService,
            scheduler,
            playback,
            NarrativeEventContext.Create(NarrativeFlagSetEventType, PrologueContext).WithValue("flag", "No0Started"),
            No0WakeNodeID,
            result);

        PlayTriggeredNode(
            triggerService,
            scheduler,
            playback,
            NarrativeEventContext.Create(NarrativeFlagSetEventType, WorkshopContext).WithValue("flag", "No0WakeDialogueSeen"),
            FirstStatusCardNodeID,
            result);

        int statusCardCountBeforeWipe = result.StatusCardIDs.Count;
        int wipeActionCountBeforeClick = CountAction(result.PrologueActionIDs, WipeCoreActionID);

        PlayTriggeredNode(
            triggerService,
            scheduler,
            playback,
            NarrativeEventContext.Create(PrologueActionEventType, WorkshopContext).WithValue("action", WipeCoreActionID),
            FirstCoreWipeNodeID,
            result);

        if (!_state.GetFlag("No0Started")) {
            result.Errors.Add("No0Started flag was not written.");
        }

        if (!_state.GetFlag("No0WakeDialogueSeen")) {
            result.Errors.Add("No0WakeDialogueSeen flag was not written.");
        }

        if (!_state.GetFlag("FirstStatusShown")) {
            result.Errors.Add("FirstStatusShown flag was not written.");
        }

        if (!_state.GetFlag("FirstCoreWiped")) {
            result.Errors.Add("FirstCoreWiped flag was not written.");
        }

        if (_state.GetFlag("FirstDiveUnlocked") || _state.GetFlag("Layer1ConfirmOpened")) {
            result.Errors.Add("Doll wake flow should not unlock shallow gate or open layer confirm.");
        }

        if (startDollActionCountAfterClick != 0) {
            result.Errors.Add("Start Doll click replayed a prologue action button.");
        }

        if (statusCardCountBeforeWipe != 1) {
            result.Errors.Add($"Expected one status card before core wipe, got [{statusCardCountBeforeWipe}].");
        }

        if (wipeActionCountBeforeClick != 1) {
            result.Errors.Add($"Expected one Wipe Core action before click, got [{wipeActionCountBeforeClick}].");
        }

        if (result.UnlockUiRequests != 0 || result.LayerConfirmRequests != 0) {
            result.Errors.Add("Doll wake flow sent UI unlock or layer confirm requests.");
        }

        return result;
    }

    private static void PlayTriggeredNode(
        NarrativeTriggerService triggerService,
        NarrativeScheduler scheduler,
        NarrativePlaybackService playback,
        NarrativeEventContext eventContext,
        string expectedNodeID,
        PrologueDollWakeFlowResult result) {
        List<NarrativeRequest> requests = triggerService.Evaluate(eventContext);
        if (requests.Count != 1) {
            result.Errors.Add($"Expected one trigger for [{expectedNodeID}], got [{requests.Count}].");
            return;
        }

        NarrativeRequest request = requests[0];
        if (!string.Equals(request.YarnNode, expectedNodeID, StringComparison.Ordinal)) {
            result.Errors.Add($"Expected node [{expectedNodeID}], got [{request.YarnNode}].");
            return;
        }

        NarrativeScheduleResult scheduleResult = scheduler.Submit(request);
        if (scheduleResult != NarrativeScheduleResult.Started && scheduleResult != NarrativeScheduleResult.Replaced) {
            result.Errors.Add($"Could not start node [{expectedNodeID}], schedule result [{scheduleResult}].");
            return;
        }

        NarrativePlaybackResult playbackResult = playback.PlayActive();
        result.Nodes.Add(new PrologueOpeningNodePlayback {
            TriggerID = request.TriggerID,
            NodeID = request.YarnNode,
            PlaybackResult = playbackResult
        });
        result.LinesShown += playbackResult.Lines.Count;
        result.CommandsExecuted += playbackResult.Commands.Count;

        if (!playbackResult.Success) {
            result.Errors.AddRange(playbackResult.Errors);
        }
    }

    private static int CountAction(List<string> actionIDs, string actionID) {
        int count = 0;
        foreach (string value in actionIDs) {
            if (value == actionID) {
                count++;
            }
        }

        return count;
    }

    private sealed class RecordingNarrativeCommandSink : INarrativeCommandSink {
        private readonly PrologueDollWakeFlowResult _result;
        private readonly INarrativeCommandSink _externalSink;

        public RecordingNarrativeCommandSink(PrologueDollWakeFlowResult result, INarrativeCommandSink externalSink) {
            _result = result;
            _externalSink = externalSink;
        }

        public void PlayVisual(NarrativeVisualCommandRequest request) {
            if (!string.IsNullOrEmpty(request?.VisualID)) {
                _result.VisualSequence.Add(request.VisualID);
            }

            _externalSink?.PlayVisual(request);
        }

        public void ShowCharacter(NarrativeCharacterCommandRequest request) {
            if (!string.IsNullOrEmpty(request?.CharacterID)) {
                _result.CharacterSequence.Add(request.CharacterID);
            }

            _externalSink?.ShowCharacter(request);
        }

        public void ShowPrologueAction(NarrativePrologueActionCommandRequest request) {
            if (!string.IsNullOrEmpty(request?.ActionID)) {
                _result.PrologueActionIDs.Add(request.ActionID);
            }

            _externalSink?.ShowPrologueAction(request);
        }

        public void ShowStatusCard(NarrativeStatusCardCommandRequest request) {
            if (!string.IsNullOrEmpty(request?.CardID)) {
                _result.StatusCardIDs.Add(request.CardID);
            }

            _externalSink?.ShowStatusCard(request);
        }

        public void UnlockUi(NarrativeUnlockUiCommandRequest request) {
            _result.UnlockUiRequests++;
            _externalSink?.UnlockUi(request);
        }

        public void RequestLayerConfirm(NarrativeLayerConfirmCommandRequest request) {
            _result.LayerConfirmRequests++;
            _externalSink?.RequestLayerConfirm(request);
        }
    }
}
