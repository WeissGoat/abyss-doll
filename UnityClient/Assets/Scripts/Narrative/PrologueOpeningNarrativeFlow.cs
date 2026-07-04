using System;
using System.Collections.Generic;

public sealed class PrologueOpeningNodePlayback {
    public string TriggerID;
    public string NodeID;
    public NarrativePlaybackResult PlaybackResult;

    public int LineCount {
        get { return PlaybackResult?.Lines?.Count ?? 0; }
    }
}

public sealed class PrologueOpeningFlowResult {
    public readonly List<PrologueOpeningNodePlayback> Nodes = new List<PrologueOpeningNodePlayback>();
    public readonly List<string> VisualSequence = new List<string>();
    public readonly List<string> PrologueActionIDs = new List<string>();
    public readonly List<string> Errors = new List<string>();
    public NarrativeStateStore State;
    public int LinesShown;
    public int CommandsExecuted;
    public int StatusCardRequests;
    public int UnlockUiRequests;
    public int LayerConfirmRequests;

    public bool Success {
        get { return Errors.Count == 0 && State != null && State.GetFlag("No0Found"); }
    }

    public bool HasNoPlayerActionsBeforeNo0 {
        get { return PrologueActionIDs.Count == 0 && StatusCardRequests == 0 && UnlockUiRequests == 0 && LayerConfirmRequests == 0; }
    }

    public bool HasOnlyStartDollActionAfterNo0 {
        get {
            return PrologueActionIDs.Count == 1
                && PrologueActionIDs[0] == "start_doll"
                && StatusCardRequests == 0
                && UnlockUiRequests == 0
                && LayerConfirmRequests == 0;
        }
    }
}

public sealed class PrologueOpeningNarrativeFlow {
    public const string OpeningEventType = "PrologueStarted";
    public const string NodeCompletedEventType = "NarrativeNodeCompleted";
    public const string PrologueContext = "prologue";
    public const string OpeningNodeID = "T0_01A_Opening_CG";
    public const string FindNo0NodeID = "T0_01A_FindNo0";

    private readonly NarrativeConfigDatabase _database;
    private readonly INarrativeScriptRuntime _runtime;
    private readonly NarrativeStateStore _state;
    private readonly INarrativeCommandSink _externalSink;

    public PrologueOpeningNarrativeFlow(
        NarrativeConfigDatabase database,
        INarrativeScriptRuntime runtime = null,
        NarrativeStateStore state = null,
        INarrativeCommandSink externalSink = null) {
        _database = database ?? new NarrativeConfigDatabase();
        _runtime = runtime ?? new P3YarnLikeNarrativeRuntime();
        _state = state ?? new NarrativeStateStore();
        _externalSink = externalSink;
    }

    public PrologueOpeningFlowResult PlayOpeningToNo0Found() {
        PrologueOpeningFlowResult result = new PrologueOpeningFlowResult {
            State = _state
        };

        RecordingNarrativeCommandSink sink = new RecordingNarrativeCommandSink(result, _externalSink);
        NarrativeTriggerService triggerService = new NarrativeTriggerService(_database, _state);
        NarrativeScheduler scheduler = new NarrativeScheduler(_state);
        NarrativeCommandBridge bridge = new NarrativeCommandBridge(_database, _state, sink);
        NarrativePlaybackService playback = new NarrativePlaybackService(_database, _runtime, _state, scheduler, bridge);

        PlayTriggeredNode(
            triggerService,
            scheduler,
            playback,
            NarrativeEventContext.Create(OpeningEventType, PrologueContext),
            OpeningNodeID,
            result);

        PlayTriggeredNode(
            triggerService,
            scheduler,
            playback,
            NarrativeEventContext.Create(NodeCompletedEventType, PrologueContext).WithValue("node", OpeningNodeID),
            FindNo0NodeID,
            result);

        if (!_state.GetFlag("PrologueStarted")) {
            result.Errors.Add("PrologueStarted flag was not written.");
        }

        if (!_state.GetFlag("DebtNoticeSeen") || !_state.GetFlag("RepairNoteSeen") || !_state.GetFlag("LastCoreShardSeen")) {
            result.Errors.Add("One or more process CG flags were not written.");
        }

        if (!_state.GetFlag("No0Found")) {
            result.Errors.Add("No0Found flag was not written.");
        }

        if (_state.GetFlag("No0Started")) {
            result.Errors.Add("No0Started should not be written before the player clicks Start Doll.");
        }

        if (!result.HasOnlyStartDollActionAfterNo0) {
            result.Errors.Add("Opening flow must expose exactly one Start Doll action after No0 is found.");
        }

        return result;
    }

    private static void PlayTriggeredNode(
        NarrativeTriggerService triggerService,
        NarrativeScheduler scheduler,
        NarrativePlaybackService playback,
        NarrativeEventContext eventContext,
        string expectedNodeID,
        PrologueOpeningFlowResult result) {
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

    private sealed class RecordingNarrativeCommandSink : INarrativeCommandSink {
        private readonly PrologueOpeningFlowResult _result;
        private readonly INarrativeCommandSink _externalSink;

        public RecordingNarrativeCommandSink(PrologueOpeningFlowResult result, INarrativeCommandSink externalSink) {
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
            _externalSink?.ShowCharacter(request);
        }

        public void ShowPrologueAction(NarrativePrologueActionCommandRequest request) {
            if (!string.IsNullOrEmpty(request?.ActionID)) {
                _result.PrologueActionIDs.Add(request.ActionID);
            }

            _externalSink?.ShowPrologueAction(request);
        }

        public void ShowStatusCard(NarrativeStatusCardCommandRequest request) {
            _result.StatusCardRequests++;
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
