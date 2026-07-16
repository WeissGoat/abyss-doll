using System;
using System.Collections.Generic;
using System.Collections;
using UnityEngine;

public sealed class PrologueFirstDiveController : MonoBehaviour, INarrativeCommandSink {
    private const string ContinueText = "\u7ee7\u7eed";

    public Canvas targetCanvas;
    public P3DialogueOverlayController dialogueOverlay;
    public WorkshopUIController workshopController;
    public bool playOpeningOnStart;
    public bool presentDepartureWithRuntimeOverlay;
    public float autoAdvanceSeconds = 3.8f;

    public PrologueOpeningFlowResult LastOpeningResult { get; private set; }
    public PrologueFirstDiveDepartureFlowResult LastDepartureResult { get; private set; }
    public int LastLayerConfirmLayerID { get; private set; }
    public bool FirstDiveLayerConfirmOpened { get; private set; }
    public bool FirstDiveDepartureInProgress { get; private set; }
    public bool FirstDiveStartRunSucceeded { get; private set; }
    public string LastFirstDiveStartFailure { get; private set; }
    public PrologueFirstDiveReentryStage LastAppliedReentryStage { get; private set; }
    public NarrativeStateStore NarrativeState {
        get { return _state; }
    }
    public bool IsPresentingTimelineForDebug {
        get { return _isPresentingTimeline; }
    }

    public int ActiveTimelineNodeIndexForDebug {
        get { return _activeTimelineNodeIndex; }
    }

    public int ActiveTimelineStepIndexForDebug {
        get { return _activeTimelineStepIndex; }
    }

    public float NextTimelineAdvanceAtForDebug {
        get { return _nextTimelineAdvanceAt; }
    }

    public int UpdateTickCountForDebug {
        get { return _updateTickCount; }
    }

    public string TimelineDebugSummary {
        get {
            int nodeCount = _activeTimelineNodes != null ? _activeTimelineNodes.Count : 0;
            return $"presenting={_isPresentingTimeline}, nodes={nodeCount}, nodeIndex={_activeTimelineNodeIndex}, "
                + $"stepIndex={_activeTimelineStepIndex}, next={_nextTimelineAdvanceAt:0.000}, "
                + $"now={Time.unscaledTime:0.000}, frame={Time.frameCount}, updateTicks={_updateTickCount}";
        }
    }

    public event Action FirstDiveDepartRequested;
    public event Action BeforeFirstDiveStartRun;
    public event Action<bool> FirstDiveStartRunCompleted;

    private string _currentVisualID = string.Empty;
    private string _currentFallbackVisualID = string.Empty;
    private string _currentStatusCardText = string.Empty;
    private readonly NarrativeStateStore _state = new NarrativeStateStore();
    private bool _isPresentingTimeline;
    private List<PrologueOpeningNodePlayback> _activeTimelineNodes;
    private int _activeTimelineNodeIndex;
    private int _activeTimelineStepIndex;
    private float _nextTimelineAdvanceAt;
    private int _updateTickCount;

    private void Start() {
        EnsureRuntimeBindings();
        if (playOpeningOnStart) {
            PlayOpeningToNo0Found();
        }
    }

    private void OnEnable() {
        EnsureRuntimeBindings();
    }

    public void EnsureRuntimeBindings() {
        EnsureOverlay();
        EnsureWorkshopBinding();
    }

    public void RequestPrologueActionForRuntime(string actionID) {
        HandleOverlayActionRequested(actionID);
    }

    public void RequestContinueForRuntime() {
        HandleOverlayContinueRequested();
    }

    private void Update() {
        _updateTickCount++;
        if (!_isPresentingTimeline || _activeTimelineNodes == null) {
            return;
        }

        if (dialogueOverlay != null && dialogueOverlay.IsAutoEnabled && Time.unscaledTime >= _nextTimelineAdvanceAt) {
            AdvanceActiveTimeline();
        }
    }

    public void PlayOpeningToNo0Found() {
        EnsureConfigsLoaded();
        EnsureOverlay();

        PrologueOpeningNarrativeFlow flow = new PrologueOpeningNarrativeFlow(
            ConfigManager.Narrative,
            new P3YarnLikeNarrativeRuntime(),
            _state,
            this);
        LastOpeningResult = flow.PlayOpeningToNo0Found();

        if (LastOpeningResult.Success && dialogueOverlay != null) {
            StopAllCoroutines();
            BeginTimeline(LastOpeningResult.Nodes);
        } else if (LastOpeningResult != null) {
            Debug.LogError($"[PrologueFirstDiveController] Opening flow failed: {string.Join(" | ", LastOpeningResult.Errors)}");
        }
    }

    public void PlayVisual(NarrativeVisualCommandRequest request) {
        _currentVisualID = request?.VisualID ?? string.Empty;
        _currentFallbackVisualID = request?.FallbackVisualID ?? string.Empty;
    }

    public void ShowCharacter(NarrativeCharacterCommandRequest request) {
        if (!string.IsNullOrEmpty(request?.VisualID)) {
            _currentVisualID = request.VisualID;
            _currentFallbackVisualID = request.FallbackVisualID;
        }
    }

    public void ShowPrologueAction(NarrativePrologueActionCommandRequest request) {
    }

    public void ShowStatusCard(NarrativeStatusCardCommandRequest request) {
    }

    public void UnlockUi(NarrativeUnlockUiCommandRequest request) {
        if (request == null || request.GateID != PrologueHalfOpenNarrativeFlow.ShallowGateID) {
            return;
        }

        if (workshopController == null) {
            workshopController = FindObjectOfType<WorkshopUIController>();
        }

        EnsureWorkshopBinding();
        workshopController?.EnterPrologueHalfOpen();
    }

    public void RequestLayerConfirm(NarrativeLayerConfirmCommandRequest request) {
        LastLayerConfirmLayerID = request?.LayerID ?? 0;
        FirstDiveLayerConfirmOpened = false;

        if (request == null || !request.FirstDiveOnly || request.LayerID != NarrativeCommandBridge.T0FirstDiveLayerID) {
            return;
        }

        _state.SetFlag("FirstDiveUnlocked");
        _state.SetFlag("Layer1ConfirmOpened");

        if (workshopController == null) {
            workshopController = FindObjectOfType<WorkshopUIController>();
        }

        workshopController?.OpenFirstDiveLayerConfirmPanel(RequestFirstDiveDeparture);
        FirstDiveLayerConfirmOpened = workshopController != null && workshopController.IsDungeonStartLayerPanelOpen;
    }

    public PrologueFirstDiveReentryStage ResolveReentryStage() {
        return PrologueFirstDiveReentryResolver.Resolve(_state, GameRoot.Core?.Dungeon);
    }

    public PrologueFirstDiveReentryStage ApplyReentryState() {
        LastAppliedReentryStage = ResolveReentryStage();
        if (workshopController == null) {
            workshopController = FindObjectOfType<WorkshopUIController>();
        }

        if (LastAppliedReentryStage == PrologueFirstDiveReentryStage.HalfOpenWorkshop) {
            workshopController?.EnterPrologueHalfOpen();
        } else if (LastAppliedReentryStage == PrologueFirstDiveReentryStage.OpeningBlackScreen && playOpeningOnStart) {
            PlayOpeningToNo0Found();
        }

        return LastAppliedReentryStage;
    }

    public void RequestFirstDiveDeparture() {
        if (FirstDiveDepartureInProgress) {
            return;
        }

        FirstDiveDepartureInProgress = true;
        FirstDiveStartRunSucceeded = false;
        LastFirstDiveStartFailure = string.Empty;
        FirstDiveDepartRequested?.Invoke();
        EnsureReadyToDepartFlags();
        EnsureConfigsLoaded();
        EnsureOverlay();

        if (Application.isPlaying && presentDepartureWithRuntimeOverlay && dialogueOverlay != null) {
            StopAllCoroutines();
            StartCoroutine(PresentFirstDiveDepartureAndStart());
            return;
        }

        bool played = PlayFirstDiveDepartureNode();
        if (played) {
            StartFirstDiveRun();
        }

        FirstDiveDepartureInProgress = false;
    }

    private IEnumerator PresentOpeningTimeline(PrologueOpeningFlowResult result) {
        _currentVisualID = string.Empty;
        _currentFallbackVisualID = string.Empty;
        _isPresentingTimeline = true;

        foreach (PrologueOpeningNodePlayback node in result.Nodes) {
            if (node?.PlaybackResult?.Steps == null) {
                continue;
            }

            foreach (NarrativePlaybackStep step in node.PlaybackResult.Steps) {
                if (step == null || !isActiveAndEnabled) {
                    continue;
                }

                if (step.StepType == NarrativePlaybackStepType.Command && step.Command != null) {
                    if (ApplyTimelineCommand(step.Command)) {
                        EndActiveTimeline(false);
                        yield break;
                    }

                    continue;
                }

                if (step.StepType == NarrativePlaybackStepType.Line && step.Line != null) {
                    dialogueOverlay.PresentLine(new NarrativeOverlayPayload {
                        BlockingMode = "modal",
                        SpeakerName = ResolveSpeakerName(step.Line.SpeakerID),
                        Text = step.Line.Text,
                        ContinueLabel = ContinueText,
                        UseBlackout = _currentVisualID == "cg_t0_01a_black_wake",
                        VisualRequest = BuildCurrentVisualRequest()
                    });

                    yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, autoAdvanceSeconds));
                }
            }
        }

        EndActiveTimeline(true);
    }

    private IEnumerator PresentTimeline(List<PrologueOpeningNodePlayback> nodes) {
        _currentVisualID = string.Empty;
        _currentFallbackVisualID = string.Empty;
        _isPresentingTimeline = true;

        foreach (PrologueOpeningNodePlayback node in nodes) {
            if (node?.PlaybackResult?.Steps == null) {
                continue;
            }

            foreach (NarrativePlaybackStep step in node.PlaybackResult.Steps) {
                if (step == null || !isActiveAndEnabled) {
                    continue;
                }

                if (step.StepType == NarrativePlaybackStepType.Command && step.Command != null) {
                    if (ApplyTimelineCommand(step.Command)) {
                        EndActiveTimeline(false);
                        yield break;
                    }

                    continue;
                }

                if (step.StepType == NarrativePlaybackStepType.Line && step.Line != null) {
                    dialogueOverlay.PresentLine(new NarrativeOverlayPayload {
                        BlockingMode = "modal",
                        SpeakerName = ResolveSpeakerName(step.Line.SpeakerID),
                        Text = step.Line.Text,
                        ContinueLabel = ContinueText,
                        UseBlackout = _currentVisualID == "cg_t0_01a_black_wake",
                        VisualRequest = BuildCurrentVisualRequest()
                    });

                    yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, autoAdvanceSeconds));
                }
            }
        }

        EndActiveTimeline(true);
    }

    private IEnumerator PresentFirstDiveDepartureAndStart() {
        bool played = PlayFirstDiveDepartureNode();
        if (played && LastDepartureResult != null) {
            yield return PresentDepartureTimeline(LastDepartureResult);
            StartFirstDiveRun();
        }

        FirstDiveDepartureInProgress = false;
    }

    private bool PlayFirstDiveDepartureNode() {
        PrologueFirstDiveDepartureFlow flow = new PrologueFirstDiveDepartureFlow(
            ConfigManager.Narrative,
            new P3YarnLikeNarrativeRuntime(),
            _state,
            this);
        LastDepartureResult = flow.PlayFirstDepart();

        if (LastDepartureResult != null && LastDepartureResult.Success) {
            return true;
        }

        string errors = LastDepartureResult == null ? "departure result is null" : string.Join(" | ", LastDepartureResult.Errors);
        Debug.LogError($"[PrologueFirstDiveController] First dive departure flow failed: {errors}");
        _state.SetFlag("Layer1FirstDeparted", false);
        ReopenFirstDiveConfirmAfterFailure();
        return false;
    }

    private bool StartFirstDiveRun() {
        BeforeFirstDiveStartRun?.Invoke();

        bool started = GameRoot.Core?.Dungeon != null
            && GameRoot.Core.Dungeon.StartRunAtLayer(NarrativeCommandBridge.T0FirstDiveLayerID);
        FirstDiveStartRunSucceeded = started;
        FirstDiveStartRunCompleted?.Invoke(started);

        if (started) {
            LastFirstDiveStartFailure = string.Empty;
            workshopController?.CloseDungeonStartLayerPanel();
            dialogueOverlay?.Hide();
            GameFlowController.Instance?.EnterDungeonMap();
            return true;
        }

        _state.SetFlag("Layer1FirstDeparted", false);
        LastFirstDiveStartFailure = BuildFirstDiveStartFailureText();
        Debug.LogWarning($"[PrologueFirstDiveController] First dive start failed. {LastFirstDiveStartFailure}");
        ReopenFirstDiveConfirmAfterFailure();
        return false;
    }

    private IEnumerator PresentDepartureTimeline(PrologueFirstDiveDepartureFlowResult result) {
        string currentVisualID = string.Empty;
        string currentFallbackVisualID = string.Empty;

        foreach (PrologueOpeningNodePlayback node in result.Nodes) {
            if (node?.PlaybackResult?.Steps == null) {
                continue;
            }

            foreach (NarrativePlaybackStep step in node.PlaybackResult.Steps) {
                if (step == null) {
                    continue;
                }

                if (step.StepType == NarrativePlaybackStepType.Command && step.Command != null) {
                    ApplyTimelineCommand(step.Command, ref currentVisualID, ref currentFallbackVisualID);
                    if (currentVisualID == PrologueFirstDiveDepartureFlow.DepartBlackVisualID) {
                        PresentBlackout(currentVisualID, currentFallbackVisualID);
                        yield return new WaitForSecondsRealtime(Mathf.Max(0.25f, autoAdvanceSeconds));
                    }

                    continue;
                }

                if (step.StepType == NarrativePlaybackStepType.Line && step.Line != null) {
                    dialogueOverlay.PresentLine(new NarrativeOverlayPayload {
                        BlockingMode = "modal",
                        SpeakerName = ResolveSpeakerName(step.Line.SpeakerID),
                        Text = step.Line.Text,
                        ContinueLabel = ContinueText,
                        UseBlackout = currentVisualID == PrologueFirstDiveDepartureFlow.DepartBlackVisualID,
                        VisualRequest = BuildVisualRequest(currentVisualID, currentFallbackVisualID)
                    });

                    yield return new WaitForSecondsRealtime(Mathf.Max(0.1f, autoAdvanceSeconds));
                }
            }
        }
    }

    private void PresentBlackout(string visualID, string fallbackVisualID) {
        if (dialogueOverlay == null) {
            return;
        }

        dialogueOverlay.PresentLine(new NarrativeOverlayPayload {
            BlockingMode = "modal",
            SpeakerName = string.Empty,
            Text = string.Empty,
            ContinueLabel = string.Empty,
            UseBlackout = true,
            VisualRequest = BuildVisualRequest(visualID, fallbackVisualID)
        });
    }

    private bool ApplyTimelineCommand(NarrativeCommandOutput command) {
        if (command == null || command.Arguments == null || command.Arguments.Count == 0) {
            return false;
        }

        if (command.CommandName == "play_visual") {
            _currentVisualID = command.Arguments[0];
            _currentFallbackVisualID = command.Arguments.Count > 1 ? command.Arguments[1] : string.Empty;
        } else if (command.CommandName == "show_character") {
            ResolveCharacterVisual(command, out _currentVisualID, out _currentFallbackVisualID);
        } else if (command.CommandName == "show_status_card") {
            _currentVisualID = "ui_status_card_prologue";
            _currentFallbackVisualID = "ui_panel_main";
            PresentStatusCard(command.Arguments[0]);
        } else if (command.CommandName == "show_prologue_action") {
            PresentPrologueAction(command);
            return true;
        }

        return false;
    }

    private void ApplyTimelineCommand(NarrativeCommandOutput command, ref string currentVisualID, ref string currentFallbackVisualID) {
        if (command == null || command.Arguments == null || command.Arguments.Count == 0) {
            return;
        }

        if (command.CommandName == "play_visual") {
            currentVisualID = command.Arguments[0];
            currentFallbackVisualID = command.Arguments.Count > 1 ? command.Arguments[1] : string.Empty;
        } else if (command.CommandName == "show_character") {
            ResolveCharacterVisual(command, out currentVisualID, out currentFallbackVisualID);
        }
    }

    private static void ResolveCharacterVisual(NarrativeCommandOutput command, out string visualID, out string fallbackVisualID) {
        visualID = string.Empty;
        fallbackVisualID = "doll_proto_0_stand";

        if (command == null || command.Arguments == null || command.Arguments.Count == 0) {
            return;
        }

        string characterID = command.Arguments[0];
        if (characterID == "no0") {
            visualID = "stand_no0_weak_sitting";
            fallbackVisualID = "doll_proto_0_stand";
            return;
        }

        if (command.Arguments.Count > 4) {
            visualID = command.Arguments[4];
        }

        if (command.Arguments.Count > 5) {
            fallbackVisualID = command.Arguments[5];
        }
    }

    private NarrativeVisualRequest BuildCurrentVisualRequest() {
        if (string.IsNullOrEmpty(_currentVisualID) && string.IsNullOrEmpty(_currentFallbackVisualID)) {
            return null;
        }

        return new NarrativeVisualRequest {
            VisualID = _currentVisualID,
            FallbackVisualID = _currentFallbackVisualID,
            UseCover = true
        };
    }

    private NarrativeVisualRequest BuildVisualRequest(string visualID, string fallbackVisualID) {
        if (string.IsNullOrEmpty(visualID) && string.IsNullOrEmpty(fallbackVisualID)) {
            return null;
        }

        return new NarrativeVisualRequest {
            VisualID = visualID,
            FallbackVisualID = fallbackVisualID,
            UseCover = true
        };
    }

    private void HandleOverlayActionRequested(string actionID) {
        if (string.IsNullOrEmpty(actionID)) {
            return;
        }

        if (actionID == PrologueDollWakeNarrativeFlow.StartDollActionID) {
            PrepareStateForStartDollAction();
            if (_isPresentingTimeline) {
                EndActiveTimeline(false);
            }

            StopAllCoroutines();
            PlayStartDollAction();
        } else if (actionID == PrologueDollWakeNarrativeFlow.WipeCoreActionID) {
            PrepareStateForWipeCoreAction();
            if (_isPresentingTimeline) {
                return;
            }

            PlayCoreWipeAction();
        }
    }

    private void PrepareStateForStartDollAction() {
        _state.SetFlag("PrologueStarted");
        _state.SetFlag("DebtNoticeSeen");
        _state.SetFlag("RepairNoteSeen");
        _state.SetFlag("LastCoreShardSeen");
        _state.SetFlag("No0Found");
        _state.SetFlag("No0Started", false);
        _state.SetFlag("No0WakeDialogueSeen", false);
        _state.SetFlag("FirstStatusShown", false);
        _state.SetFlag("FirstCoreWiped", false);
        _state.SetFlag("FirstDiveUnlocked", false);
        _state.SetFlag("Layer1ConfirmOpened", false);
        _state.SetFlag("Layer1FirstDeparted", false);
    }

    private void PrepareStateForWipeCoreAction() {
        _state.SetFlag("PrologueStarted");
        _state.SetFlag("DebtNoticeSeen");
        _state.SetFlag("RepairNoteSeen");
        _state.SetFlag("LastCoreShardSeen");
        _state.SetFlag("No0Found");
        _state.SetFlag("No0Started");
        _state.SetFlag("No0WakeDialogueSeen");
        _state.SetFlag("FirstStatusShown");
        _state.SetFlag("FirstCoreWiped", false);
        _state.SetFlag("FirstDiveUnlocked", false);
        _state.SetFlag("Layer1ConfirmOpened", false);
        _state.SetFlag("Layer1FirstDeparted", false);
    }

    private void HandleWorkshopPrologueActionRequested(string actionID) {
        if (actionID != WorkshopUIController.PrologueOpenLayer1ConfirmActionID) {
            return;
        }

        List<PrologueOpeningNodePlayback> nodes = new List<PrologueOpeningNodePlayback>();
        AppendTriggeredNodes(
            nodes,
            NarrativeEventContext.Create(
                PrologueHalfOpenNarrativeFlow.PrologueActionEventType,
                PrologueHalfOpenNarrativeFlow.LayerConfirmContext)
                .WithValue("action", actionID));

        if (nodes.Count > 0 && Application.isPlaying) {
            StopAllCoroutines();
            BeginTimeline(nodes);
            return;
        }

        if (_state.GetFlag("FirstDiveUnlocked") && _state.GetFlag("Layer1ConfirmOpened")) {
            if (workshopController == null) {
                workshopController = FindObjectOfType<WorkshopUIController>();
            }

            workshopController?.OpenFirstDiveLayerConfirmPanel(RequestFirstDiveDeparture);
            FirstDiveLayerConfirmOpened = workshopController != null && workshopController.IsDungeonStartLayerPanelOpen;
        }
    }

    private void PlayStartDollAction() {
        List<PrologueOpeningNodePlayback> nodes = new List<PrologueOpeningNodePlayback>();
        AppendTriggeredNodes(
            nodes,
            NarrativeEventContext.Create(
                PrologueDollWakeNarrativeFlow.PrologueActionEventType,
                PrologueDollWakeNarrativeFlow.PrologueContext)
                .WithValue("action", PrologueDollWakeNarrativeFlow.StartDollActionID));
        AppendTriggeredNodes(
            nodes,
            NarrativeEventContext.Create(
                PrologueDollWakeNarrativeFlow.NarrativeFlagSetEventType,
                PrologueDollWakeNarrativeFlow.PrologueContext)
                .WithValue("flag", "No0Started"));
        AppendTriggeredNodes(
            nodes,
            NarrativeEventContext.Create(
                PrologueDollWakeNarrativeFlow.NarrativeFlagSetEventType,
                PrologueDollWakeNarrativeFlow.WorkshopContext)
                .WithValue("flag", "No0WakeDialogueSeen"));

        if (nodes.Count > 0 && Application.isPlaying) {
            StopAllCoroutines();
            BeginTimeline(nodes);
        }
    }

    private void PlayCoreWipeAction() {
        List<PrologueOpeningNodePlayback> nodes = new List<PrologueOpeningNodePlayback>();
        AppendTriggeredNodes(
            nodes,
            NarrativeEventContext.Create(
                PrologueDollWakeNarrativeFlow.PrologueActionEventType,
                PrologueDollWakeNarrativeFlow.WorkshopContext)
                .WithValue("action", PrologueDollWakeNarrativeFlow.WipeCoreActionID));
        AppendTriggeredNodes(
            nodes,
            NarrativeEventContext.Create(
                PrologueHalfOpenNarrativeFlow.NarrativeFlagSetEventType,
                PrologueHalfOpenNarrativeFlow.WorkshopContext)
                .WithValue("flag", "FirstCoreWiped"));

        if (nodes.Count > 0 && Application.isPlaying) {
            StopAllCoroutines();
            BeginTimeline(nodes);
        }
    }

    private void BeginTimeline(List<PrologueOpeningNodePlayback> nodes) {
        _activeTimelineNodes = nodes;
        _activeTimelineNodeIndex = 0;
        _activeTimelineStepIndex = 0;
        _nextTimelineAdvanceAt = Time.unscaledTime;
        _currentVisualID = string.Empty;
        _currentFallbackVisualID = string.Empty;
        _currentStatusCardText = string.Empty;
        _isPresentingTimeline = true;
        AdvanceActiveTimeline();
    }

    private void AdvanceActiveTimeline() {
        while (_activeTimelineNodes != null && _activeTimelineNodeIndex < _activeTimelineNodes.Count) {
            PrologueOpeningNodePlayback node = _activeTimelineNodes[_activeTimelineNodeIndex];
            if (node?.PlaybackResult?.Steps == null) {
                _activeTimelineNodeIndex++;
                _activeTimelineStepIndex = 0;
                continue;
            }

            while (_activeTimelineStepIndex < node.PlaybackResult.Steps.Count) {
                NarrativePlaybackStep step = node.PlaybackResult.Steps[_activeTimelineStepIndex];
                _activeTimelineStepIndex++;
                if (step == null) {
                    continue;
                }

                if (step.StepType == NarrativePlaybackStepType.Command && step.Command != null) {
                    if (ApplyTimelineCommand(step.Command)) {
                        EndActiveTimeline(false);
                        return;
                    }

                    continue;
                }

                if (step.StepType == NarrativePlaybackStepType.Line && step.Line != null) {
                    dialogueOverlay.PresentLine(new NarrativeOverlayPayload {
                        BlockingMode = "modal",
                        SpeakerName = ResolveSpeakerName(step.Line.SpeakerID),
                        Text = step.Line.Text,
                        ContinueLabel = ContinueText,
                        UseBlackout = _currentVisualID == "cg_t0_01a_black_wake",
                        VisualRequest = BuildCurrentVisualRequest()
                    });
                    _nextTimelineAdvanceAt = Time.unscaledTime + Mathf.Max(0.1f, autoAdvanceSeconds);
                    return;
                }
            }

            _activeTimelineNodeIndex++;
            _activeTimelineStepIndex = 0;
        }

        EndActiveTimeline(true);
    }

    private void EndActiveTimeline(bool hideOverlay) {
        _isPresentingTimeline = false;
        _activeTimelineNodes = null;
        _activeTimelineNodeIndex = 0;
        _activeTimelineStepIndex = 0;
        _nextTimelineAdvanceAt = 0f;

        if (hideOverlay) {
            dialogueOverlay?.Hide();
        }
    }

    private void AppendTriggeredNodes(List<PrologueOpeningNodePlayback> nodes, NarrativeEventContext eventContext) {
        EnsureConfigsLoaded();

        NarrativeTriggerService triggerService = new NarrativeTriggerService(ConfigManager.Narrative, _state);
        NarrativeScheduler scheduler = new NarrativeScheduler(_state);
        NarrativeCommandBridge bridge = new NarrativeCommandBridge(ConfigManager.Narrative, _state, this);
        NarrativePlaybackService playback = new NarrativePlaybackService(
            ConfigManager.Narrative,
            new P3YarnLikeNarrativeRuntime(),
            _state,
            scheduler,
            bridge);

        foreach (NarrativeRequest request in triggerService.Evaluate(eventContext)) {
            NarrativeScheduleResult scheduleResult = scheduler.Submit(request);
            if (scheduleResult != NarrativeScheduleResult.Started && scheduleResult != NarrativeScheduleResult.Replaced) {
                Debug.LogWarning($"[PrologueFirstDiveController] Could not schedule node [{request.YarnNode}] for event [{eventContext.EventType}], result [{scheduleResult}].");
                continue;
            }

            NarrativePlaybackResult playbackResult = playback.PlayActive();
            nodes.Add(new PrologueOpeningNodePlayback {
                TriggerID = request.TriggerID,
                NodeID = request.YarnNode,
                PlaybackResult = playbackResult
            });

            if (!playbackResult.Success) {
                Debug.LogError($"[PrologueFirstDiveController] Node [{request.YarnNode}] failed: {string.Join(" | ", playbackResult.Errors)}");
            }
        }
    }

    private void PresentStatusCard(string cardID) {
        if (dialogueOverlay == null || string.IsNullOrEmpty(cardID)) {
            return;
        }

        _currentStatusCardText = BuildStatusCardText(cardID);
        dialogueOverlay.PresentLine(new NarrativeOverlayPayload {
            BlockingMode = "modal_light",
            SpeakerName = "零号",
            Text = _currentStatusCardText,
            ContinueLabel = string.Empty,
            VisualRequest = new NarrativeVisualRequest {
                VisualID = "ui_status_card_prologue",
                FallbackVisualID = "ui_panel_main",
                UseCover = false
            }
        });
    }

    private void PresentPrologueAction(NarrativeCommandOutput command) {
        if (dialogueOverlay == null || command == null || command.Arguments == null || command.Arguments.Count < 2) {
            return;
        }

        string actionID = command.Arguments[0];
        string labelKey = command.Arguments[1];
        string prompt = ResolveActionPrompt(actionID);
        if (actionID == PrologueDollWakeNarrativeFlow.WipeCoreActionID && !string.IsNullOrEmpty(_currentStatusCardText)) {
            prompt = _currentStatusCardText + "\n\n" + prompt;
            _currentVisualID = "ui_status_card_prologue";
            _currentFallbackVisualID = "ui_panel_main";
        }

        dialogueOverlay.PresentLine(new NarrativeOverlayPayload {
            BlockingMode = "modal",
            SpeakerName = string.Empty,
            Text = prompt,
            ContinueLabel = string.Empty,
            ActionID = actionID,
            ActionLabel = ResolveActionLabel(actionID, labelKey),
            UseBlackout = _currentVisualID == "cg_t0_01a_black_wake",
            VisualRequest = BuildCurrentVisualRequest()
        });
    }

    private static string ResolveActionPrompt(string actionID) {
        if (actionID == PrologueDollWakeNarrativeFlow.StartDollActionID) {
            return "\u7070\u5c18\u4e0b\u9732\u51fa\u4e00\u679a\u6ca1\u6709\u7f16\u53f7\u7684\u6838\u5fc3\u4ed3\u3002\u73b0\u5728\uff0c\u53ea\u5269\u4e0b\u628a\u5979\u53eb\u9192\u3002";
        }

        if (actionID == PrologueDollWakeNarrativeFlow.WipeCoreActionID) {
            return "\u6838\u5fc3\u4ed3\u8fd8\u6709\u7070\u3002\u5148\u64e6\u5e72\u51c0\uff0c\u786e\u8ba4\u5979\u8fd8\u80fd\u6491\u591a\u4e45\u3002";
        }

        switch (actionID) {
            case PrologueDollWakeNarrativeFlow.StartDollActionID:
                return "灰尘下露出一枚没有编号的核心仓。现在，只剩下把她从废墟里叫醒。";
            case PrologueDollWakeNarrativeFlow.WipeCoreActionID:
                return "状态卡停在不稳定的红线边缘。先擦开核心仓，确认她还能撑多久。";
            default:
                return string.Empty;
        }
    }

    private static string ResolveActionLabel(string actionID, string labelKey) {
        if (actionID == PrologueDollWakeNarrativeFlow.StartDollActionID) {
            return "\u542f\u52a8\u96f6\u53f7";
        }

        if (actionID == PrologueDollWakeNarrativeFlow.WipeCoreActionID) {
            return "\u64e6\u53bb\u6838\u5fc3\u4ed3\u7070\u5c18";
        }

        switch (actionID) {
            case PrologueDollWakeNarrativeFlow.StartDollActionID:
                return "启动人偶";
            case PrologueDollWakeNarrativeFlow.WipeCoreActionID:
                return "擦去核心仓灰尘";
            default:
                return string.IsNullOrEmpty(labelKey) ? "继续" : labelKey;
        }
    }

    private static string BuildStatusCardText(string cardID) {
        if (cardID == "first_status_unstable") {
            return "\u6838\u5fc3\uff1a\u4e0d\u7a33\n\u884c\u52a8\u4f59\u91cf\uff1a\u4e00\u6b21\n\u6682\u7a33\u65b9\u5f0f\uff1a\u6d45\u5c42\u6676\u6838";
        }

        if (cardID == "first_status_stable") {
            return "\u6838\u5fc3\uff1a\u77ed\u65f6\u7a33\u5b9a\n\u884c\u52a8\u4f59\u91cf\uff1a\u4e00\u6b21\n\u4e0b\u6f5c\u8bb8\u53ef\uff1a\u6d45\u5c42\u4e00\u6b21";
        }

        switch (cardID) {
            case "first_status_unstable":
                return "核心：不稳定\n行动余量：不足一次\n浅层晶核：可暂时稳定";
            case "first_status_stable":
                return "核心：短时稳定\n行动余量：一次\n下潜许可：浅层一次";
            default:
                return cardID;
        }
    }

    private void EnsureReadyToDepartFlags() {
        PrologueFirstDiveDepartureFlow.SeedReadyToDepartFlags(_state);
    }

    private void ReopenFirstDiveConfirmAfterFailure() {
        if (workshopController == null) {
            workshopController = FindObjectOfType<WorkshopUIController>();
        }

        workshopController?.OpenFirstDiveLayerConfirmPanel(RequestFirstDiveDeparture);
        FirstDiveLayerConfirmOpened = workshopController != null && workshopController.IsDungeonStartLayerPanelOpen;
    }

    private string BuildFirstDiveStartFailureText() {
        DiveReadinessResult readiness = DiveReadinessService.Evaluate(
            GameRoot.Core?.CurrentPlayer,
            NarrativeCommandBridge.T0FirstDiveLayerID,
            false);
        return readiness == null ? "First dive readiness unavailable." : readiness.BuildSummary();
    }

    private void EnsureOverlay() {
        if (dialogueOverlay != null) {
            dialogueOverlay.EnsureBuilt();
            dialogueOverlay.ActionRequested -= HandleOverlayActionRequested;
            dialogueOverlay.ActionRequested += HandleOverlayActionRequested;
            dialogueOverlay.ContinueRequested -= HandleOverlayContinueRequested;
            dialogueOverlay.ContinueRequested += HandleOverlayContinueRequested;
            return;
        }

        if (targetCanvas == null) {
            targetCanvas = GetComponentInParent<Canvas>();
        }

        if (targetCanvas != null) {
            dialogueOverlay = P3DialogueOverlayController.CreateUnder(targetCanvas);
            dialogueOverlay.ActionRequested -= HandleOverlayActionRequested;
            dialogueOverlay.ActionRequested += HandleOverlayActionRequested;
            dialogueOverlay.ContinueRequested -= HandleOverlayContinueRequested;
            dialogueOverlay.ContinueRequested += HandleOverlayContinueRequested;
        }
    }

    private void EnsureWorkshopBinding() {
        if (workshopController == null) {
            workshopController = FindObjectOfType<WorkshopUIController>();
        }

        if (workshopController != null) {
            workshopController.PrologueActionRequested -= HandleWorkshopPrologueActionRequested;
            workshopController.PrologueActionRequested += HandleWorkshopPrologueActionRequested;
        }
    }

    private static string ResolveSpeakerName(string speakerID) {
        if (speakerID == "protagonist") {
            return "\u4e3b\u89d2";
        }

        if (speakerID == "no0") {
            return "\u96f6\u53f7";
        }

        switch (speakerID) {
            case "protagonist":
                return "主角";
            case "no0":
                return "零号";
            default:
                return speakerID ?? string.Empty;
        }
    }

    private static void EnsureConfigsLoaded() {
        if (ConfigManager.Narrative == null || ConfigManager.Narrative.NodeCount == 0) {
            ConfigManager.LoadAllConfigs();
        }
    }

    private void OnDestroy() {
        if (dialogueOverlay != null) {
            dialogueOverlay.ActionRequested -= HandleOverlayActionRequested;
            dialogueOverlay.ContinueRequested -= HandleOverlayContinueRequested;
        }

        if (workshopController != null) {
            workshopController.PrologueActionRequested -= HandleWorkshopPrologueActionRequested;
        }
    }

    private void HandleOverlayContinueRequested() {
        if (!_isPresentingTimeline || _activeTimelineNodes == null) {
            return;
        }

        _nextTimelineAdvanceAt = Time.unscaledTime;
        AdvanceActiveTimeline();
    }
}
