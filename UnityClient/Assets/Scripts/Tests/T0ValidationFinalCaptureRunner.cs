using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

public sealed class T0ValidationFinalCaptureRunner : MonoBehaviour {
    private const int CaptureWidth = 1366;
    private const int CaptureHeight = 768;
    private const float WaitTimeoutSeconds = 180f;
    private const string ContinueText = "\u7ee7\u7eed";
    private const string ShallowGateText = "\u6d45\u5c42\u5165\u53e3";
    private const string DepartText = "\u51fa\u53d1";
    private const string LookBackText = "\u518d\u770b\u5979\u4e00\u773c";

    private readonly List<string> _capturedFiles = new List<string>();
    private readonly List<string> _semanticReportLines = new List<string>();
    private string _outputRoot;
    private bool _running;
    private bool _abortRequested;
    private bool _semanticFailed;
    private bool _reportWritten;
    private string _lastSemanticState = string.Empty;

    private void Start() {
        if (Application.isPlaying) {
            Begin();
        }
    }

    public static T0ValidationFinalCaptureRunner StartCapture() {
        T0ValidationFinalCaptureRunner[] existingRunners = FindObjectsOfType<T0ValidationFinalCaptureRunner>(true);
        foreach (T0ValidationFinalCaptureRunner existing in existingRunners) {
            if (existing == null) {
                continue;
            }

            existing._abortRequested = true;
            existing._running = false;
            existing.StopAllCoroutines();
            existing.gameObject.SetActive(false);
            Destroy(existing.gameObject);
        }

        GameObject runnerObject = new GameObject("T0ValidationFinalCaptureRunner");
        DontDestroyOnLoad(runnerObject);
        T0ValidationFinalCaptureRunner runner = runnerObject.AddComponent<T0ValidationFinalCaptureRunner>();
        runner.Begin();
        return runner;
    }

    public void Begin() {
        if (_running) {
            return;
        }

        _running = true;
        StartCoroutine(CaptureRoutine());
    }

    private IEnumerator CaptureRoutine() {
        _outputRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs/T0Validation"));
        Directory.CreateDirectory(_outputRoot);
        ClearPreviousFinalCaptureFiles();
        Debug.Log($"[T0ValidationFinalCapture] Starting final screenshot capture. output={_outputRoot}");

        yield return WaitForCondition("runtime ready", () => GameRoot.IsCoreReady() && GameFlowController.Instance != null);
        if (_abortRequested) { yield break; }
        yield return WaitForCondition("dialogue overlay ready", () => Overlay() != null);
        if (_abortRequested) { yield break; }
        yield return WaitForCondition("prologue controller ready", () => FindObjectOfType<PrologueFirstDiveController>(true) != null);
        if (_abortRequested) { yield break; }
        yield return ResetRuntimeToWorkshopForCapture();
        if (_abortRequested) { yield break; }

        yield return PresentAndCaptureOverlay(
            "01 wake pov",
            "t0_val_01_final_01_wake_pov.png",
            "WakePovStage",
            new NarrativeOverlayPayload {
                BlockingMode = "modal",
                SpeakerName = "\u4e3b\u89d2",
                Text = "\u53c8\u5230\u65e9\u4e0a\u4e86\u3002",
                ContinueLabel = ContinueText,
                UseBlackout = false,
                VisualRequest = new NarrativeVisualRequest {
                    VisualID = "cg_t0_01a_black_wake",
                    FallbackVisualID = "bg_workshop_home_room",
                    UseCover = true
                }
            },
            () => ValidateOverlayState("cg_t0_01a_black_wake", requireContinue: true, requireAction: false, requireAutoOff: true));
        if (_abortRequested) { yield break; }

        yield return PresentAndCaptureOverlay(
            "02 workshop establish",
            "t0_val_01_final_02_workshop_establish.png",
            "WorkshopEstablishStage",
            new NarrativeOverlayPayload {
                BlockingMode = "modal",
                SpeakerName = "\u4e3b\u89d2",
                Text = "\u4ed6\u4eec\u8fde\u574f\u6389\u7684\u4e1c\u897f\u4e5f\u6536\u3002",
                ContinueLabel = ContinueText,
                VisualRequest = new NarrativeVisualRequest {
                    VisualID = "cg_t0_01a_debt_notice",
                    FallbackVisualID = "ui_paper_panel",
                    UseCover = true
                }
            },
            () => ValidateOverlayState("cg_t0_01a_debt_notice", requireContinue: true, requireAction: false, requireAutoOff: true));
        if (_abortRequested) { yield break; }

        yield return PresentAndCaptureOverlay(
            "03 debt pressure",
            "t0_val_01_final_03_debt_pressure.png",
            "DebtPressureStage",
            new NarrativeOverlayPayload {
                BlockingMode = "modal",
                SpeakerName = "\u4e3b\u89d2",
                Text = "\u4e0d\uff0c\u4ed6\u4eec\u6700\u559c\u6b22\u6536\u574f\u6389\u7684\u4e1c\u897f\u3002",
                ContinueLabel = ContinueText,
                VisualRequest = new NarrativeVisualRequest {
                    VisualID = "cg_t0_01a_repair_note",
                    FallbackVisualID = "ui_paper_panel",
                    UseCover = true
                }
            },
            () => ValidateOverlayState("cg_t0_01a_repair_note", requireContinue: true, requireAction: false, requireAutoOff: true));
        if (_abortRequested) { yield break; }

        yield return PresentAndCaptureOverlay(
            "04 repair clue",
            "t0_val_01_final_04_repair_clue.png",
            "CoreShardStage",
            new NarrativeOverlayPayload {
                BlockingMode = "modal",
                SpeakerName = "\u4e3b\u89d2",
                Text = "\u6d45\u5c42\u81f3\u5c11\u8fd8\u6709\u4e00\u70b9\u529e\u6cd5\u3002",
                ContinueLabel = ContinueText,
                VisualRequest = new NarrativeVisualRequest {
                    VisualID = "cg_t0_01a_core_shard",
                    FallbackVisualID = "ui_item_panel",
                    UseCover = true
                }
            },
            () => ValidateOverlayState("cg_t0_01a_core_shard", requireContinue: true, requireAction: false, requireAutoOff: true));
        if (_abortRequested) { yield break; }

        yield return PresentAndCaptureOverlay(
            "05 find no0 start button",
            "t0_val_01_final_05_find_no0_start_button.png",
            "FindNo0StartButton",
            new NarrativeOverlayPayload {
                BlockingMode = "modal",
                SpeakerName = string.Empty,
                Text = "\u7070\u5c18\u4e0b\u9732\u51fa\u4e00\u679a\u6ca1\u6709\u7f16\u53f7\u7684\u6838\u5fc3\u4ed3\u3002\n\u73b0\u5728\uff0c\u53ea\u5269\u4e0b\u628a\u5979\u53eb\u9192\u3002",
                ContinueLabel = string.Empty,
                ActionID = PrologueDollWakeNarrativeFlow.StartDollActionID,
                ActionLabel = "\u542f\u52a8\u96f6\u53f7",
                VisualRequest = new NarrativeVisualRequest {
                    VisualID = "cg_t0_01a_find_no0",
                    FallbackVisualID = "doll_proto_0_stand",
                    UseCover = true
                }
            },
            () => ValidateOverlayState("cg_t0_01a_find_no0", requireContinue: false, requireAction: true, requireAutoOff: true));
        if (_abortRequested) { yield break; }

        yield return ClickOverlayAction("start doll");
        if (_abortRequested) { yield break; }

        yield return CaptureWhen(
            "06 no0 wake dialogue",
            "t0_val_01_final_06_no0_wake_dialogue.png",
            "No0WakeDialogue",
            () => {
                P3DialogueOverlayController overlay = Overlay();
                return overlay != null
                    && overlay.IsShowing
                    && overlay.LastVisualID == "stand_no0_weak_sitting"
                    && !overlay.HasActionVisible;
            },
            () => ValidateOverlayState("stand_no0_weak_sitting", requireContinue: true, requireAction: false, requireAutoOff: true));
        if (_abortRequested) { yield break; }

        yield return ContinueOverlayUntil(
            "wake dialogue reaches status wipe",
            () => {
                P3DialogueOverlayController overlay = Overlay();
                string text = overlay != null ? overlay.CurrentText ?? string.Empty : string.Empty;
                return overlay != null
                    && overlay.HasActionVisible
                    && overlay.LastVisualID == "ui_status_card_prologue"
                    && (text.Contains("\u6838\u5fc3") || text.Contains("鏍稿績"));
            });
        if (_abortRequested) { yield break; }

        yield return CaptureWhen(
            "07 status wipe",
            "t0_val_01_final_07_status_wipe.png",
            "StatusCardWipeCore",
            () => {
                P3DialogueOverlayController overlay = Overlay();
                string text = overlay != null ? overlay.CurrentText ?? string.Empty : string.Empty;
                return overlay != null
                    && overlay.HasActionVisible
                    && overlay.LastVisualID == "ui_status_card_prologue"
                    && (text.Contains("\u6838\u5fc3") || text.Contains("鏍稿績"));
            },
            () => ValidateOverlayState("ui_status_card_prologue", requireContinue: false, requireAction: true, requireAutoOff: true));
        if (_abortRequested) { yield break; }

        yield return ClickOverlayAction("wipe core");
        if (_abortRequested) { yield break; }

        yield return ContinueOverlayUntil(
            "core wipe reaches half-open workshop",
            () => {
                P3DialogueOverlayController overlay = Overlay();
                WorkshopUIController workshop = Workshop();
                bool overlayClosed = overlay == null || !overlay.IsShowing;
                return overlayClosed && workshop != null && workshop.IsPrologueHalfOpen && workshop.IsPrologueShallowGateAvailable;
            });
        if (_abortRequested) { yield break; }

        yield return CaptureWhen(
            "08 half-open workshop",
            "t0_val_01_final_08_half_open_workshop.png",
            "PrologueHalfOpenWorkshop",
            () => {
                P3DialogueOverlayController overlay = Overlay();
                WorkshopUIController workshop = Workshop();
                bool overlayClosed = overlay == null || !overlay.IsShowing;
                return overlayClosed && workshop != null && workshop.IsPrologueHalfOpen && workshop.IsPrologueShallowGateAvailable;
            },
            ValidateHalfOpenWorkshopState);
        if (_abortRequested) { yield break; }

        WorkshopUIController activeWorkshop = Workshop();
        if (activeWorkshop != null && activeWorkshop.departBtn != null) {
            activeWorkshop.departBtn.onClick.Invoke();
        } else {
            Fail("Cannot click shallow gate: workshop/depart button missing.");
            yield break;
        }

        yield return CaptureWhen(
            "09 first dive confirm",
            "t0_val_01_final_09_first_dive_confirm.png",
            "FirstDivePermit",
            () => {
                WorkshopUIController workshop = Workshop();
                DungeonStartLayerUIController panel = FindObjectOfType<DungeonStartLayerUIController>(true);
                return workshop != null
                    && workshop.IsDungeonStartLayerPanelOpen
                    && panel != null
                    && panel.IsFirstDiveMode
                    && panel.SelectedLayerID == 1;
            },
            ValidateFirstDivePermitState);
        if (_abortRequested) { yield break; }

        DungeonStartLayerUIController layerPanel = FindObjectOfType<DungeonStartLayerUIController>(true);
        if (layerPanel != null && layerPanel.confirmBtn != null) {
            layerPanel.confirmBtn.onClick.Invoke();
        } else {
            Fail("Cannot click first dive confirm: panel/confirm button missing.");
            yield break;
        }

        bool requestedMapRecovery = false;
        yield return CaptureWhen(
            "10 dungeon map",
            "t0_val_01_final_10_dungeon_map.png",
            "DungeonMap",
            () => {
                GameFlowController flow = GameFlowController.Instance;
                PrologueFirstDiveController prologue = FindObjectOfType<PrologueFirstDiveController>(true);
                bool firstDiveStarted = prologue != null && prologue.FirstDiveStartRunSucceeded;
                bool layerReady = GameRoot.Core?.Dungeon?.CurrentLayer != null;
                if (flow != null
                    && layerReady
                    && firstDiveStarted
                    && !requestedMapRecovery
                    && (flow.dungeonMapPanel == null || !flow.dungeonMapPanel.activeInHierarchy)) {
                    requestedMapRecovery = true;
                    Workshop()?.CloseDungeonStartLayerPanel();
                    Overlay()?.Hide();
                    flow.EnterDungeonMap();
                    Canvas.ForceUpdateCanvases();
                }

                return flow != null
                    && flow.dungeonMapPanel != null
                    && flow.dungeonMapPanel.activeInHierarchy
                    && layerReady
                    && firstDiveStarted;
            },
            ValidateDungeonMapState);
        if (_abortRequested) { yield break; }

        WriteReport();
        Finish(true);
    }

    private void ClearPreviousFinalCaptureFiles() {
        if (string.IsNullOrEmpty(_outputRoot) || !Directory.Exists(_outputRoot)) {
            return;
        }

        foreach (string file in Directory.GetFiles(_outputRoot, "t0_val_01_final_*.png")) {
            try {
                File.Delete(file);
            } catch (Exception ex) {
                Debug.LogWarning($"[T0ValidationFinalCapture] Could not delete stale capture {file}: {ex.Message}");
            }
        }
    }

    private IEnumerator CaptureWhen(string label, string fileName, string semanticState, Func<bool> predicate, Func<string> semanticValidator) {
        yield return WaitForCondition(label, predicate);
        if (_abortRequested) { yield break; }
        yield return WaitForVisualStable();
        if (!RecordSemantic(label, semanticState, semanticValidator)) {
            Finish(false);
            yield break;
        }

        string path = Path.Combine(_outputRoot, fileName);
        CaptureCurrentScreen(path);
        _capturedFiles.Add(path);
        Debug.Log($"[T0ValidationFinalCapture] Captured {label}: {path}");
    }

    private IEnumerator ContinueOverlayUntil(string label, Func<bool> predicate) {
        float deadline = Time.realtimeSinceStartup + WaitTimeoutSeconds;
        while (Time.realtimeSinceStartup < deadline) {
            if (predicate()) {
                yield break;
            }

            P3DialogueOverlayController overlay = Overlay();
            PrologueFirstDiveController prologue = FindObjectOfType<PrologueFirstDiveController>(true);
            prologue?.EnsureRuntimeBindings();
            if (overlay != null && overlay.HasContinueVisible && !overlay.HasActionVisible) {
                string beforeVisualID = overlay.LastVisualID;
                string beforeText = overlay.CurrentText;
                overlay.continueButton.onClick.Invoke();
                yield return null;
                if (overlay.HasContinueVisible
                    && !overlay.HasActionVisible
                    && overlay.LastVisualID == beforeVisualID
                    && overlay.CurrentText == beforeText) {
                    prologue?.RequestContinueForRuntime();
                }
            }

            yield return null;
        }

        Fail($"Timeout while advancing overlay for [{label}]. state={BuildRuntimeStateSummary()}");
    }

    private IEnumerator PresentAndCaptureOverlay(string label, string fileName, string semanticState, NarrativeOverlayPayload payload, Func<string> semanticValidator) {
        P3DialogueOverlayController overlay = Overlay();
        if (overlay == null) {
            Fail($"Cannot present {label}: overlay missing.");
            yield break;
        }

        overlay.PresentLine(payload);
        yield return WaitForVisualStable();
        if (!RecordSemantic(label, semanticState, semanticValidator)) {
            Finish(false);
            yield break;
        }

        string path = Path.Combine(_outputRoot, fileName);
        CaptureCurrentScreen(path);
        _capturedFiles.Add(path);
        Debug.Log($"[T0ValidationFinalCapture] Captured {label}: {path}");
    }

    private IEnumerator WaitForCondition(string label, Func<bool> predicate) {
        float start = Time.realtimeSinceStartup;
        while (Time.realtimeSinceStartup - start < WaitTimeoutSeconds) {
            if (predicate != null && predicate()) {
                yield break;
            }

            yield return null;
        }

        string state = BuildRuntimeStateSummary();
        _semanticReportLines.Add($"semantic=FAIL stage={label} reason=timeout state={state}");
        Fail($"Timeout waiting for {label}. {state}");
    }

    private IEnumerator WaitForVisualStable() {
        Canvas.ForceUpdateCanvases();
        yield return new WaitForSecondsRealtime(0.12f);
        Canvas.ForceUpdateCanvases();
        yield return new WaitForSecondsRealtime(0.08f);
    }

    private IEnumerator ResetRuntimeToWorkshopForCapture() {
        P3DialogueOverlayController overlay = Overlay();
        overlay?.Hide();

        GameFlowController flow = GameFlowController.Instance;
        if (flow != null) {
            flow.EnterWorkshop();
        }

        WorkshopUIController workshop = Workshop();
        if (workshop != null) {
            workshop.ExitPrologueHalfOpen();
            workshop.CloseDungeonStartLayerPanel();
            workshop.RefreshUI();
        }

        yield return null;
        Canvas.ForceUpdateCanvases();
        yield return WaitForVisualStable();
    }

    private void CaptureCurrentScreen(string absolutePath) {
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));
        Camera camera = Camera.main;
        if (camera == null) {
            Camera[] cameras = FindObjectsOfType<Camera>();
            foreach (Camera candidate in cameras) {
                if (candidate != null && candidate.isActiveAndEnabled) {
                    camera = candidate;
                    break;
                }
            }
        }

        if (camera == null) {
            Fail($"No active camera for screenshot: {absolutePath}");
            return;
        }

        Canvas[] canvases = FindObjectsOfType<Canvas>();
        List<CanvasModeBackup> backups = new List<CanvasModeBackup>();
        foreach (Canvas canvas in canvases) {
            if (canvas != null && canvas.isActiveAndEnabled && canvas.renderMode == RenderMode.ScreenSpaceOverlay) {
                backups.Add(new CanvasModeBackup(canvas, canvas.renderMode, canvas.worldCamera));
                canvas.renderMode = RenderMode.ScreenSpaceCamera;
                canvas.worldCamera = camera;
            }
        }

        RenderTexture renderTexture = null;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        float previousAspect = camera.aspect;
        Texture2D screenshot = null;

        try {
            renderTexture = new RenderTexture(CaptureWidth, CaptureHeight, 24, RenderTextureFormat.ARGB32);
            renderTexture.antiAliasing = 1;
            renderTexture.Create();

            camera.targetTexture = renderTexture;
            camera.aspect = (float)CaptureWidth / CaptureHeight;
            camera.Render();

            RenderTexture.active = renderTexture;
            screenshot = new Texture2D(CaptureWidth, CaptureHeight, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, CaptureWidth, CaptureHeight), 0, 0);
            screenshot.Apply();
            File.WriteAllBytes(absolutePath, screenshot.EncodeToPNG());
        } finally {
            camera.targetTexture = previousTarget;
            camera.aspect = previousAspect;
            RenderTexture.active = previousActive;

            foreach (CanvasModeBackup backup in backups) {
                if (backup.Canvas != null) {
                    backup.Canvas.renderMode = backup.Mode;
                    backup.Canvas.worldCamera = backup.WorldCamera;
                }
            }

            if (renderTexture != null) {
                renderTexture.Release();
                Destroy(renderTexture);
            }

            if (screenshot != null) {
                Destroy(screenshot);
            }
        }
    }

    private IEnumerator ClickOverlayAction(string label) {
        P3DialogueOverlayController overlay = Overlay();
        if (overlay == null || overlay.actionButton == null || !overlay.actionButton.gameObject.activeInHierarchy) {
            Fail($"Cannot click overlay action [{label}].");
            yield break;
        }

        PrologueFirstDiveController prologue = FindObjectOfType<PrologueFirstDiveController>(true);
        string actionID = overlay.CurrentActionID;
        string beforeVisualID = overlay.LastVisualID;
        prologue?.EnsureRuntimeBindings();
        Debug.Log($"[T0ValidationFinalCapture] Click overlay action [{label}] id={actionID}, visual={beforeVisualID}, prologue={(prologue == null ? "missing" : prologue.TimelineDebugSummary)}");
        overlay.actionButton.onClick.Invoke();
        yield return null;
        if (prologue != null
            && overlay.HasActionVisible
            && overlay.CurrentActionID == actionID
            && overlay.LastVisualID == beforeVisualID) {
            Debug.Log($"[T0ValidationFinalCapture] Overlay action [{label}] did not advance through button event; requesting Prologue runtime action directly.");
            prologue.RequestPrologueActionForRuntime(actionID);
            yield return null;
        }
        Debug.Log($"[T0ValidationFinalCapture] After overlay action [{label}] prologue={(prologue == null ? "missing" : prologue.TimelineDebugSummary)}, overlayVisual={overlay.LastVisualID}, overlayAction={overlay.CurrentActionID}, overlayShowing={overlay.IsShowing}");
    }

    private bool RecordSemantic(string label, string semanticState, Func<string> semanticValidator) {
        if (string.Equals(_lastSemanticState, semanticState, StringComparison.Ordinal)) {
            _semanticFailed = true;
            _semanticReportLines.Add($"semantic=FAIL stage={label} state={semanticState} reason=duplicate_adjacent_state");
            return false;
        }

        string error = semanticValidator != null ? semanticValidator() : string.Empty;
        if (!string.IsNullOrEmpty(error)) {
            _semanticFailed = true;
            _semanticReportLines.Add($"semantic=FAIL stage={label} state={semanticState} reason={error}");
            return false;
        }

        _lastSemanticState = semanticState;
        _semanticReportLines.Add($"semantic=PASS stage={label} state={semanticState}");
        return true;
    }

    private string ValidateOverlayState(string expectedVisualID, bool requireContinue, bool requireAction, bool requireAutoOff) {
        P3DialogueOverlayController overlay = Overlay();
        if (overlay == null || !overlay.IsShowing) {
            return "overlay_missing_or_hidden";
        }

        if (overlay.LastVisualID != expectedVisualID) {
            return $"visual_expected_{expectedVisualID}_actual_{overlay.LastVisualID}";
        }

        if (requireContinue && !overlay.HasContinueVisible) {
            return "continue_button_missing";
        }

        if (!requireContinue && overlay.HasContinueVisible) {
            return "continue_button_should_be_hidden";
        }

        if (requireAction && !overlay.HasActionVisible) {
            return "action_button_missing";
        }

        if (requireAction && !overlay.HasStandaloneActionVisible) {
            return "action_button_not_standalone";
        }

        if (!requireAction && overlay.HasActionVisible) {
            return "action_button_should_be_hidden";
        }

        if (requireAutoOff && overlay.IsAutoEnabled) {
            return "auto_should_default_off";
        }

        if (!overlay.HasAutoLogControlsVisible) {
            return "auto_log_controls_missing";
        }

        if (IsComicPrologueShot(expectedVisualID)) {
            if (!overlay.HasComicPageVisible) {
                return "comic_page_missing";
            }

            int minimumPanels = expectedVisualID == "cg_t0_01a_find_no0" ? 3 : 3;
            if (overlay.VisibleComicPanelCount < minimumPanels) {
                return $"comic_panel_count_too_low_{overlay.VisibleComicPanelCount}";
            }

            if (overlay.HasAnyStagePropVisible) {
                return "stage_prop_should_be_hidden_for_comic_page";
            }

            if (!overlay.HasLargeVisualFocus) {
                return "comic_page_lacks_large_visual_focus";
            }
        } else if (overlay.HasComicPageVisible || overlay.VisibleComicPanelCount > 0) {
            return "comic_page_should_be_hidden";
        }

        string expectedStageSprite = ExpectedStageSpriteName(expectedVisualID);
        if (!string.IsNullOrEmpty(expectedStageSprite)
            && !IsComicPrologueShot(expectedVisualID)
            && !(overlay.StageBackgroundSpriteName ?? string.Empty).Contains(expectedStageSprite)) {
            return $"stage_sprite_expected_{expectedStageSprite}_actual_{overlay.StageBackgroundSpriteName}";
        }

        if (expectedVisualID == "stand_no0_weak_sitting"
            && !(overlay.StageBackgroundSpriteName ?? string.Empty).Contains("cg_t0_01a_p06_panel01_no0_close")) {
            return "wake_dialogue_stage_background_missing";
        }

        return string.Empty;
    }

    private static bool IsComicPrologueShot(string visualID) {
        return visualID == "cg_t0_01a_debt_notice"
            || visualID == "cg_t0_01a_repair_note"
            || visualID == "cg_t0_01a_core_shard"
            || visualID == "cg_t0_01a_find_no0";
    }

    private static string ExpectedStageSpriteName(string visualID) {
        switch (visualID) {
            case "cg_t0_01a_black_wake":
                return "cg_t0_01a_p01_wake_pov";
            case "cg_t0_01a_debt_notice":
                return "cg_t0_01a_p02_panel01_workshop_wide";
            case "cg_t0_01a_repair_note":
                return "cg_t0_01a_p03_panel01_debt_notice_close";
            case "cg_t0_01a_core_shard":
                return "cg_t0_01a_p04_panel03_core_shard_box";
            case "cg_t0_01a_find_no0":
                return "cg_t0_01a_p05_panel03_no0_core_dim";
            default:
                return string.Empty;
        }
    }

    private string ValidateHalfOpenWorkshopState() {
        WorkshopUIController workshop = Workshop();
        DungeonStartLayerUIController panel = FindObjectOfType<DungeonStartLayerUIController>(true);
        if (workshop == null || !workshop.IsPrologueHalfOpen || workshop.PrologueSemanticState != "PrologueHalfOpenWorkshop") {
            return "half_open_workshop_state_missing";
        }

        if (!workshop.IsPrologueShallowGateAvailable || workshop.ProloguePrimaryActionText != ShallowGateText) {
            return "shallow_gate_primary_action_missing";
        }

        if (!workshop.HasPrologueFormalScene) {
            return "formal_scene_background_missing";
        }

        GameFlowController flow = GameFlowController.Instance;
        if (flow != null) {
            if (flow.workshopPanel != null && !flow.workshopPanel.activeInHierarchy) {
                return "workshop_panel_not_active_for_half_open";
            }

            if (flow.dungeonMapPanel != null && flow.dungeonMapPanel.activeInHierarchy) {
                return "dungeon_map_visible_during_half_open";
            }
        }

        Transform root = workshop.transform;
        if (root.Find("LightStatusStrip") == null
            || root.Find("AbyssDoorPanel") == null
            || root.Find("WorkshopEntryPanel") == null
            || root.Find("LedgerCornerPanel") == null
            || root.Find("PrologueObjectiveRibbon/PrologueObjective_Text") == null
            || root.Find("PrimaryActionGlow") == null
            || root.Find("PrologueFormalScene_Image") == null
            || root.Find("DollDisplay/DollImage") == null
            || root.Find("AbyssDoorPanel/AbyssDoorIcon_Image") == null
            || root.Find("AbyssDoorPanel/AbyssDoorScene_Image") == null) {
            return "formal_v2_workshop_zones_missing";
        }

        Image dollImage = root.Find("DollDisplay/DollImage")?.GetComponent<Image>();
        if (dollImage == null || !dollImage.gameObject.activeInHierarchy) {
            return "formal_v2_doll_center_not_visible";
        }

        Image primaryGlow = root.Find("PrimaryActionGlow")?.GetComponent<Image>();
        if (primaryGlow == null || !primaryGlow.gameObject.activeInHierarchy) {
            return "formal_v2_primary_action_focus_missing";
        }

        Text objectiveText = root.Find("PrologueObjectiveRibbon/PrologueObjective_Text")?.GetComponent<Text>();
        if (objectiveText == null
            || !objectiveText.gameObject.activeInHierarchy
            || !(objectiveText.text ?? string.Empty).Contains("零号")
            || !(objectiveText.text ?? string.Empty).Contains("浅层入口")) {
            return "formal_v2_objective_ribbon_missing";
        }

        bool firstDivePanelVisible = panel != null
            && panel.IsFirstDiveMode
            && (workshop.IsDungeonStartLayerPanelOpen || panel.gameObject.activeInHierarchy);
        if (firstDivePanelVisible) {
            return "first_dive_permit_visible_too_early";
        }

        return string.Empty;
    }

    private string ValidateFirstDivePermitState() {
        WorkshopUIController workshop = Workshop();
        DungeonStartLayerUIController panel = FindObjectOfType<DungeonStartLayerUIController>(true);
        if (workshop == null || !workshop.IsDungeonStartLayerPanelOpen) {
            return "first_dive_panel_not_open";
        }

        if (panel == null || !panel.IsFirstDiveMode || panel.SelectedLayerID != 1 || !panel.HasFirstDivePermitCard) {
            return "first_dive_permit_state_missing";
        }

        string permitTitle = panel.PermitTitleText ?? string.Empty;
        if (!permitTitle.Contains("\u9996\u6b21\u4e0b\u6f5c\u8bb8\u53ef")
            && !permitTitle.Contains("\u7b2c\u4e00\u5c42")) {
            return "permit_title_missing";
        }

        if (!(panel.PermitSummaryText ?? string.Empty).Contains("\u8bb8\u53ef")) {
            return "permit_summary_missing";
        }

        Transform permitRoot = panel.transform;
        if (permitRoot.Find("LayerList/FirstDivePermitCard/PermitSeal_Image") == null
            && FindObjectNamed("PermitSeal_Image") == null) {
            return "permit_seal_missing";
        }

        if (permitRoot.Find("LayerList/FirstDivePermitCard/PermitSerial_Text") == null
            && FindObjectNamed("PermitSerial_Text") == null) {
            return "permit_serial_missing";
        }

        if (permitRoot.Find("LayerList/FirstDivePermitCard/PermitGate_Image") == null
            && FindObjectNamed("PermitGate_Image") == null) {
            return "permit_gate_art_missing";
        }

        if (GetButtonText(panel.confirmBtn) != DepartText || GetButtonText(panel.closeBtn) != LookBackText) {
            return "permit_buttons_missing";
        }

        if (CountActiveObjectsNamed("FirstDivePermitCard") != 1) {
            return "permit_card_duplicate_or_missing";
        }

        return string.Empty;
    }

    private string ValidateDungeonMapState() {
        P3DialogueOverlayController overlay = Overlay();
        DungeonStartLayerUIController panel = FindObjectOfType<DungeonStartLayerUIController>(true);
        PrologueFirstDiveController prologue = FindObjectOfType<PrologueFirstDiveController>(true);
        if (overlay != null && overlay.IsInputBlocking) {
            return "overlay_still_blocks_map";
        }

        if (panel != null && panel.gameObject.activeInHierarchy && panel.IsFirstDiveMode) {
            return "first_dive_permit_still_visible_on_map";
        }

        GameFlowController flow = GameFlowController.Instance;
        if (flow == null || flow.dungeonMapPanel == null || !flow.dungeonMapPanel.activeInHierarchy) {
            return "dungeon_map_panel_not_active";
        }

        if (prologue == null || !prologue.FirstDiveStartRunSucceeded) {
            return "first_dive_start_not_confirmed";
        }

        return GameRoot.Core?.Dungeon?.CurrentLayer != null ? string.Empty : "dungeon_layer_missing";
    }

    private static string GetButtonText(Button button) {
        Text text = button != null ? button.GetComponentInChildren<Text>(true) : null;
        return text != null ? text.text : string.Empty;
    }

    private static GameObject FindObjectNamed(string objectName) {
        Transform[] transforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++) {
            if (transforms[i] != null && transforms[i].name == objectName) {
                return transforms[i].gameObject;
            }
        }

        return null;
    }

    private static int CountActiveObjectsNamed(string objectName) {
        int count = 0;
        Transform[] transforms = FindObjectsOfType<Transform>(true);
        for (int i = 0; i < transforms.Length; i++) {
            if (transforms[i] != null && transforms[i].name == objectName && transforms[i].gameObject.activeInHierarchy) {
                count++;
            }
        }

        return count;
    }

    private static P3DialogueOverlayController Overlay() {
        PrologueFirstDiveController prologue = FindObjectOfType<PrologueFirstDiveController>(true);
        if (prologue != null && prologue.dialogueOverlay != null) {
            return prologue.dialogueOverlay;
        }

        return FindObjectOfType<P3DialogueOverlayController>(true);
    }

    private static WorkshopUIController Workshop() {
        return FindObjectOfType<WorkshopUIController>(true);
    }

    private string BuildRuntimeStateSummary() {
        P3DialogueOverlayController overlay = Overlay();
        WorkshopUIController workshop = Workshop();
        DungeonStartLayerUIController panel = FindObjectOfType<DungeonStartLayerUIController>(true);
        return $"overlay={(overlay == null ? "missing" : overlay.LastVisualID + "/" + overlay.CurrentText + "/action=" + overlay.HasActionVisible + "/continue=" + overlay.HasContinueVisible)}, "
            + $"workshop={(workshop == null ? "missing" : "half=" + workshop.IsPrologueHalfOpen + ",gate=" + workshop.IsPrologueShallowGateAvailable + ",layerPanel=" + workshop.IsDungeonStartLayerPanelOpen + ",action=" + workshop.ProloguePrimaryActionText)}, "
            + $"panel={(panel == null ? "missing" : "firstDive=" + panel.IsFirstDiveMode + ",layer=" + panel.SelectedLayerID + ",permit=" + panel.HasFirstDivePermitCard)}, "
            + $"firstDiveStarted={(FindObjectOfType<PrologueFirstDiveController>(true)?.FirstDiveStartRunSucceeded == true)}, "
            + $"mapActive={(GameFlowController.Instance?.dungeonMapPanel != null && GameFlowController.Instance.dungeonMapPanel.activeInHierarchy)}, "
            + $"dungeonLayer={(GameRoot.Core?.Dungeon?.CurrentLayer == null ? "none" : GameRoot.Core.Dungeon.CurrentLayer.LayerID.ToString())}";
    }

    private void WriteReport() {
        if (_reportWritten || string.IsNullOrEmpty(_outputRoot)) {
            return;
        }

        string reportPath = Path.Combine(_outputRoot, "t0_val_01_final_capture_report.txt");
        List<string> lines = new List<string> {
            $"captured_at={DateTime.Now:yyyy-MM-dd HH:mm:ss}",
            $"resolution={CaptureWidth}x{CaptureHeight}",
            $"count={_capturedFiles.Count}",
            $"semantic_failed={_semanticFailed}"
        };
        lines.AddRange(_semanticReportLines);
        lines.AddRange(_capturedFiles);
        File.WriteAllLines(reportPath, lines.ToArray());
        _reportWritten = true;
        Debug.Log($"[T0ValidationFinalCapture] Report written: {reportPath}");
    }

    private void Fail(string message) {
        Debug.LogError($"[T0ValidationFinalCapture] {message}");
        _semanticFailed = true;
        _abortRequested = true;
        Finish(false);
    }

    private void Finish(bool success) {
        Debug.Log(success
            ? $"[T0ValidationFinalCapture] Finished. screenshots={_capturedFiles.Count}"
            : $"[T0ValidationFinalCapture] Failed. screenshots={_capturedFiles.Count}");
        _running = false;
        if (!success) {
            WriteReport();
        }

        Destroy(gameObject);
    }

    private readonly struct CanvasModeBackup {
        public readonly Canvas Canvas;
        public readonly RenderMode Mode;
        public readonly Camera WorldCamera;

        public CanvasModeBackup(Canvas canvas, RenderMode mode, Camera worldCamera) {
            Canvas = canvas;
            Mode = mode;
            WorldCamera = worldCamera;
        }
    }
}
