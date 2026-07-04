using System;
using System.Collections.Generic;

public enum PrologueFirstDiveReentryStage {
    OpeningBlackScreen,
    StatusCard,
    HalfOpenWorkshop,
    Layer1Run
}

public static class PrologueFirstDiveReentryResolver {
    public static PrologueFirstDiveReentryStage Resolve(NarrativeStateStore state, DungeonManager dungeon = null) {
        if ((dungeon?.CurrentLayer != null && dungeon.CurrentLayer.LayerID == NarrativeCommandBridge.T0FirstDiveLayerID)
            || (state != null && state.GetFlag("Layer1FirstDeparted"))) {
            return PrologueFirstDiveReentryStage.Layer1Run;
        }

        if (state != null && state.GetFlag("FirstDiveUnlocked")) {
            return PrologueFirstDiveReentryStage.HalfOpenWorkshop;
        }

        if (state != null && state.GetFlag("No0Started")) {
            return PrologueFirstDiveReentryStage.StatusCard;
        }

        return PrologueFirstDiveReentryStage.OpeningBlackScreen;
    }
}

public sealed class PrologueFirstDiveDepartureFlowResult {
    public readonly List<PrologueOpeningNodePlayback> Nodes = new List<PrologueOpeningNodePlayback>();
    public readonly List<string> VisualSequence = new List<string>();
    public readonly List<string> LineKeys = new List<string>();
    public readonly List<string> Errors = new List<string>();
    public NarrativeStateStore State;
    public int LinesShown;
    public int CommandsExecuted;
    public int LayerConfirmRequests;
    public int UnlockUiRequests;
    public int PrologueActionRequests;
    public int StatusCardRequests;
    public bool Layer1FirstDepartedWritten;

    public bool Success {
        get {
            return Errors.Count == 0
                && State != null
                && Layer1FirstDepartedWritten
                && LinesShown == 3
                && VisualSequence.Contains(PrologueFirstDiveDepartureFlow.DepartBlackVisualID)
                && LayerConfirmRequests == 0
                && UnlockUiRequests == 0
                && PrologueActionRequests == 0
                && StatusCardRequests == 0;
        }
    }
}

public sealed class PrologueFirstDiveDepartureFlow {
    public const string PrologueActionEventType = "PrologueActionClicked";
    public const string LayerConfirmContext = "layer_confirm";
    public const string FirstDiveDepartActionID = "first_dive_depart";
    public const string FirstDepartNodeID = "T0_01A_FirstDepart";
    public const string DepartBlackVisualID = "cg_t0_01a_depart_black";

    private readonly NarrativeConfigDatabase _database;
    private readonly INarrativeScriptRuntime _runtime;
    private readonly NarrativeStateStore _state;
    private readonly INarrativeCommandSink _externalSink;

    public PrologueFirstDiveDepartureFlow(
        NarrativeConfigDatabase database,
        INarrativeScriptRuntime runtime = null,
        NarrativeStateStore state = null,
        INarrativeCommandSink externalSink = null) {
        _database = database ?? new NarrativeConfigDatabase();
        _runtime = runtime ?? new P3YarnLikeNarrativeRuntime();
        _state = state ?? new NarrativeStateStore();
        _externalSink = externalSink;
    }

    public PrologueFirstDiveDepartureFlowResult PlayFirstDepart() {
        PrologueFirstDiveDepartureFlowResult result = new PrologueFirstDiveDepartureFlowResult {
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
            NarrativeEventContext.Create(PrologueActionEventType, LayerConfirmContext).WithValue("action", FirstDiveDepartActionID),
            FirstDepartNodeID,
            result);

        result.Layer1FirstDepartedWritten = _state.GetFlag("Layer1FirstDeparted");
        if (!result.Layer1FirstDepartedWritten) {
            result.Errors.Add("Layer1FirstDeparted flag was not written.");
        }

        if (result.LinesShown != 3) {
            result.Errors.Add($"Expected three departure lines, got [{result.LinesShown}].");
        }

        if (!result.VisualSequence.Contains(DepartBlackVisualID)) {
            result.Errors.Add("Departure black visual was not requested.");
        }

        if (result.LayerConfirmRequests != 0 || result.UnlockUiRequests != 0 || result.PrologueActionRequests != 0) {
            result.Errors.Add("Departure flow should not reopen layer confirm, unlock UI, or expose new prologue actions.");
        }

        return result;
    }

    public static void SeedReadyToDepartFlags(NarrativeStateStore state) {
        if (state == null) {
            return;
        }

        state.SetFlag("PrologueStarted");
        state.SetFlag("DebtNoticeSeen");
        state.SetFlag("RepairNoteSeen");
        state.SetFlag("LastCoreShardSeen");
        state.SetFlag("No0Found");
        state.SetFlag("No0Started");
        state.SetFlag("No0WakeDialogueSeen");
        state.SetFlag("FirstStatusShown");
        state.SetFlag("FirstCoreWiped");
        state.SetFlag("FirstDiveUnlocked");
        state.SetFlag("Layer1ConfirmOpened");
        state.SetFlag("Layer1FirstDeparted", false);
    }

    private static void PlayTriggeredNode(
        NarrativeTriggerService triggerService,
        NarrativeScheduler scheduler,
        NarrativePlaybackService playback,
        NarrativeEventContext eventContext,
        string expectedNodeID,
        PrologueFirstDiveDepartureFlowResult result) {
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
        foreach (NarrativeLineOutput line in playbackResult.Lines) {
            result.LineKeys.Add(line.LineKey);
        }

        if (!playbackResult.Success) {
            result.Errors.AddRange(playbackResult.Errors);
        }
    }

    private sealed class RecordingNarrativeCommandSink : INarrativeCommandSink {
        private readonly PrologueFirstDiveDepartureFlowResult _result;
        private readonly INarrativeCommandSink _externalSink;

        public RecordingNarrativeCommandSink(PrologueFirstDiveDepartureFlowResult result, INarrativeCommandSink externalSink) {
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
            _result.PrologueActionRequests++;
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
