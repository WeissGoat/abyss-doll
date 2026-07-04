using System;
using System.Collections.Generic;

public sealed class PrologueHalfOpenFlowResult {
    public readonly List<PrologueOpeningNodePlayback> Nodes = new List<PrologueOpeningNodePlayback>();
    public readonly List<string> UnlockGateIDs = new List<string>();
    public readonly List<string> WorkshopActionIDs = new List<string>();
    public readonly List<string> Errors = new List<string>();
    public NarrativeStateStore State;
    public int LinesShown;
    public int CommandsExecuted;
    public int LayerConfirmRequests;

    public bool Success {
        get {
            return Errors.Count == 0
                && State != null
                && State.GetFlag("FirstDiveUnlocked")
                && State.GetFlag("Layer1ConfirmOpened")
                && LayerConfirmRequests == 1;
        }
    }
}

public sealed class PrologueHalfOpenNarrativeFlow {
    public const string NarrativeFlagSetEventType = "NarrativeFlagSet";
    public const string PrologueActionEventType = "PrologueActionClicked";
    public const string WorkshopContext = "workshop";
    public const string LayerConfirmContext = "layer_confirm";
    public const string FirstDiveUnlockNodeID = "T0_01A_FirstDiveUnlock";
    public const string Layer1ConfirmNodeID = "T0_01A_Layer1Confirm";
    public const string ShallowGateID = "shallow_gate";

    private readonly NarrativeConfigDatabase _database;
    private readonly INarrativeScriptRuntime _runtime;
    private readonly NarrativeStateStore _state;
    private readonly WorkshopUIController _workshop;

    public PrologueHalfOpenNarrativeFlow(
        NarrativeConfigDatabase database,
        WorkshopUIController workshop,
        INarrativeScriptRuntime runtime = null,
        NarrativeStateStore state = null) {
        _database = database ?? new NarrativeConfigDatabase();
        _runtime = runtime ?? new P3YarnLikeNarrativeRuntime();
        _state = state ?? new NarrativeStateStore();
        _workshop = workshop;
    }

    public PrologueHalfOpenFlowResult PlayCoreWipedToLayerConfirmRequest() {
        PrologueHalfOpenFlowResult result = new PrologueHalfOpenFlowResult {
            State = _state
        };

        SeedPreviousPrologueFlags();

        RecordingNarrativeCommandSink sink = new RecordingNarrativeCommandSink(result, _workshop);
        NarrativeTriggerService triggerService = new NarrativeTriggerService(_database, _state);
        NarrativeScheduler scheduler = new NarrativeScheduler(_state);
        NarrativeCommandBridge bridge = new NarrativeCommandBridge(_database, _state, sink);
        NarrativePlaybackService playback = new NarrativePlaybackService(_database, _runtime, _state, scheduler, bridge);

        PlayTriggeredNode(
            triggerService,
            scheduler,
            playback,
            NarrativeEventContext.Create(NarrativeFlagSetEventType, WorkshopContext).WithValue("flag", "FirstCoreWiped"),
            FirstDiveUnlockNodeID,
            result);

        if (!_state.GetFlag("FirstDiveUnlocked")) {
            result.Errors.Add("FirstDiveUnlocked flag was not written.");
        }

        if (_workshop == null || !_workshop.IsPrologueHalfOpen || !_workshop.IsPrologueShallowGateAvailable) {
            result.Errors.Add("Workshop did not enter prologue half-open shallow gate state.");
        }

        if (result.UnlockGateIDs.Count != 1 || result.UnlockGateIDs[0] != ShallowGateID) {
            result.Errors.Add($"Expected one shallow gate unlock, got [{string.Join("|", result.UnlockGateIDs)}].");
        }

        bool panelOpenBeforeClick = _workshop != null && _workshop.IsDungeonStartLayerPanelOpen;
        _workshop?.departBtn?.onClick.Invoke();

        if (panelOpenBeforeClick || (_workshop != null && _workshop.IsDungeonStartLayerPanelOpen)) {
            result.Errors.Add("Shallow gate click opened the generic dungeon layer panel directly.");
        }

        if (result.WorkshopActionIDs.Count != 1
            || result.WorkshopActionIDs[0] != WorkshopUIController.PrologueOpenLayer1ConfirmActionID
            || _workshop?.LastPrologueActionRequested != WorkshopUIController.PrologueOpenLayer1ConfirmActionID) {
            result.Errors.Add($"Expected workshop action open_layer1_confirm, got [{string.Join("|", result.WorkshopActionIDs)}].");
        }

        PlayTriggeredNode(
            triggerService,
            scheduler,
            playback,
            NarrativeEventContext.Create(PrologueActionEventType, LayerConfirmContext)
                .WithValue("action", WorkshopUIController.PrologueOpenLayer1ConfirmActionID),
            Layer1ConfirmNodeID,
            result);

        if (!_state.GetFlag("Layer1ConfirmOpened")) {
            result.Errors.Add("Layer1ConfirmOpened flag was not written.");
        }

        if (result.LayerConfirmRequests != 1) {
            result.Errors.Add($"Expected one layer confirm request, got [{result.LayerConfirmRequests}].");
        }

        return result;
    }

    private void SeedPreviousPrologueFlags() {
        _state.SetFlag("PrologueStarted");
        _state.SetFlag("DebtNoticeSeen");
        _state.SetFlag("RepairNoteSeen");
        _state.SetFlag("LastCoreShardSeen");
        _state.SetFlag("No0Found");
        _state.SetFlag("No0Started");
        _state.SetFlag("No0WakeDialogueSeen");
        _state.SetFlag("FirstStatusShown");
        _state.SetFlag("FirstCoreWiped");
    }

    private static void PlayTriggeredNode(
        NarrativeTriggerService triggerService,
        NarrativeScheduler scheduler,
        NarrativePlaybackService playback,
        NarrativeEventContext eventContext,
        string expectedNodeID,
        PrologueHalfOpenFlowResult result) {
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
        private readonly PrologueHalfOpenFlowResult _result;
        private readonly WorkshopUIController _workshop;

        public RecordingNarrativeCommandSink(PrologueHalfOpenFlowResult result, WorkshopUIController workshop) {
            _result = result;
            _workshop = workshop;
        }

        public void PlayVisual(NarrativeVisualCommandRequest request) {
        }

        public void ShowCharacter(NarrativeCharacterCommandRequest request) {
        }

        public void ShowPrologueAction(NarrativePrologueActionCommandRequest request) {
        }

        public void ShowStatusCard(NarrativeStatusCardCommandRequest request) {
        }

        public void UnlockUi(NarrativeUnlockUiCommandRequest request) {
            if (!string.IsNullOrEmpty(request?.GateID)) {
                _result.UnlockGateIDs.Add(request.GateID);
            }

            if (request != null && request.GateID == ShallowGateID) {
                _workshop?.EnterPrologueHalfOpen(actionID => _result.WorkshopActionIDs.Add(actionID));
            }
        }

        public void RequestLayerConfirm(NarrativeLayerConfirmCommandRequest request) {
            _result.LayerConfirmRequests++;
        }
    }
}
