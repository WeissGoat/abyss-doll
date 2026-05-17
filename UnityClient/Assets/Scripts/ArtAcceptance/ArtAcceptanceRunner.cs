using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

public class ArtAcceptanceRunner : MonoBehaviour {
    private const string SchemaVersion = "1.0";
    private const string ProjectName = "P3";
    private const string ModeAuto = "auto";
    private const int ReferenceWidth = 1920;
    private const int ReferenceHeight = 1080;
    private const float VisibleAlphaThreshold = 0.01f;
    private static ArtAcceptanceRunner _activeRunner;

    private readonly ArtAcceptanceReport _report = new ArtAcceptanceReport();
    private readonly ArtAcceptanceUiSnapshotReport _uiSnapshot = new ArtAcceptanceUiSnapshotReport();
    private ArtAcceptanceRegistrySnapshotReport _registrySnapshot = new ArtAcceptanceRegistrySnapshotReport();

    private string _runID;
    private string _outputRoot;
    private string _screenshotsRoot;
    private bool _autoExitPlayMode;
    private bool _runtimeCoreReady;
    private bool _manualRunActive;
    private bool _isAdvancingManualRun;
    private bool _manualYieldPending;
    private float _manualYieldUntilTime;
    private float _manualRunStartedAt;
    private readonly Stack<IEnumerator> _manualRoutineStack = new Stack<IEnumerator>();

    public static void BeginAutomatedRun(bool autoExitPlayMode) {
        if (_activeRunner != null && _activeRunner._report.IsRunning) {
            Debug.LogWarning("[ArtAcceptance] A run is already active.");
            return;
        }

        GameObject runnerObject = new GameObject("ArtAcceptanceRunner_Auto");
        DontDestroyOnLoad(runnerObject);
        ArtAcceptanceRunner runner = runnerObject.AddComponent<ArtAcceptanceRunner>();
        runner._autoExitPlayMode = autoExitPlayMode;
        _activeRunner = runner;
        runner.BeginManualRun();
    }

    public static void EditorTickActiveRun() {
        if (_activeRunner != null) {
            _activeRunner.AdvanceManualRun();
        }
    }

    private void Update() {
        AdvanceManualRun();
    }

    private void BeginManualRun() {
        _manualRunActive = true;
        _manualRunStartedAt = Time.realtimeSinceStartup;
        _manualRoutineStack.Clear();
        _manualRoutineStack.Push(RunAcceptanceFlow());
        AdvanceManualRun();
    }

    private void AdvanceManualRun() {
        if (!_manualRunActive || _isAdvancingManualRun || _manualRoutineStack.Count == 0) {
            return;
        }

        _isAdvancingManualRun = true;
        try {
#if UNITY_EDITOR
            EditorApplication.QueuePlayerLoopUpdate();
#endif

            if (Time.realtimeSinceStartup - _manualRunStartedAt > 90f) {
                FailManualRun("Automated art acceptance timed out after 90 seconds.");
                return;
            }

            if (_manualYieldPending) {
                if (Time.realtimeSinceStartup < _manualYieldUntilTime) {
                    return;
                }

                _manualYieldPending = false;
                _manualYieldUntilTime = 0f;
            }

            int guard = 0;
            while (_manualRunActive && _manualRoutineStack.Count > 0 && guard < 128) {
                guard++;
                IEnumerator activeRoutine = _manualRoutineStack.Peek();
                bool hasNext = false;
                object current = null;

                try {
                    hasNext = activeRoutine != null && activeRoutine.MoveNext();
                    if (hasNext) {
                        current = activeRoutine.Current;
                    }
                } catch (Exception ex) {
                    FailManualRun($"Automated art acceptance crashed: {ex.Message}", ex);
                    return;
                }

                if (!_manualRunActive || _manualRoutineStack.Count == 0) {
                    return;
                }

                if (!hasNext) {
                    if (_manualRunActive && _manualRoutineStack.Count > 0) {
                        _manualRoutineStack.Pop();
                    }
                    continue;
                }

                IEnumerator nestedRoutine = current as IEnumerator;
                if (nestedRoutine != null) {
                    _manualRoutineStack.Push(nestedRoutine);
                    continue;
                }

                BeginManualYield(current);
                return;
            }

            if (guard >= 128) {
                Debug.LogWarning("[ArtAcceptance] Manual runner guard limit reached for this tick; continuing next tick.");
            }
        } finally {
            _isAdvancingManualRun = false;
        }
    }

    private void BeginManualYield(object yieldInstruction) {
        _manualYieldPending = true;
        _manualYieldUntilTime = Time.realtimeSinceStartup + ResolveManualYieldSeconds(yieldInstruction);

#if UNITY_EDITOR
        EditorApplication.QueuePlayerLoopUpdate();
#endif
    }

    private float ResolveManualYieldSeconds(object yieldInstruction) {
        if (yieldInstruction == null) {
            return 0.02f;
        }

        return yieldInstruction is WaitForEndOfFrame ? 0.02f : 0.02f;
    }

    private void FailManualRun(string message, Exception exception = null) {
        AddError(message);
        if (exception != null) {
            Debug.LogError($"[ArtAcceptance] {message}\n{exception}");
        } else {
            Debug.LogError($"[ArtAcceptance] {message}");
        }

        try {
            if (!string.IsNullOrEmpty(_outputRoot)) {
                FinalizeReport();
                WriteOutputs();
            }
        } catch (Exception writeException) {
            Debug.LogError($"[ArtAcceptance] Failed to write failure report: {writeException}");
        }

        CompleteRun();
    }

    private IEnumerator RunAcceptanceFlow() {
        if (!TryPrepareRun()) {
            CompleteRun();
            yield break;
        }

        Debug.Log($"[ArtAcceptance] Automated art acceptance started. Output={_outputRoot}");

        yield return RunStep("WaitForRuntimeReady", WaitForRuntimeReady);
        LogFlowCheckpoint("WaitForRuntimeReady");
        SafeExecute("CaptureGlobalEnvironment", CaptureGlobalEnvironment);

        yield return RunStep("CaptureWorkshopMain", CaptureWorkshopMain);
        LogFlowCheckpoint("CaptureWorkshopMain");
        yield return RunStep("CaptureSellPanel", CaptureSellPanel);
        LogFlowCheckpoint("CaptureSellPanel");
        yield return RunStep("CaptureProstheticPanel", CaptureProstheticPanel);
        LogFlowCheckpoint("CaptureProstheticPanel");
        yield return RunStep("CaptureLayerSelect", CaptureLayerSelect);
        LogFlowCheckpoint("CaptureLayerSelect");
        yield return RunStep("CaptureDungeonMap", CaptureDungeonMap);
        LogFlowCheckpoint("CaptureDungeonMap");
        yield return RunStep("CaptureSafeRoom", CaptureSafeRoom);
        LogFlowCheckpoint("CaptureSafeRoom");
        yield return RunStep("CaptureStairsRoom", CaptureStairsRoom);
        LogFlowCheckpoint("CaptureStairsRoom");
        yield return RunStep("CaptureCombatHud", CaptureCombatHud);
        LogFlowCheckpoint("CaptureCombatHud");
        yield return RunStep("CaptureInventoryLoot", CaptureInventoryLoot);
        LogFlowCheckpoint("CaptureInventoryLoot");
        yield return RunStep("CaptureSettlement", CaptureSettlement);
        LogFlowCheckpoint("CaptureSettlement");

        SafeExecute("BuildRegistrySnapshot", () => {
            _registrySnapshot = BuildRegistrySnapshot();
            ApplyRegistryResultToReport(_registrySnapshot);
        });

        FinalizeReport();
        WriteOutputs();

        Debug.Log($"[ArtAcceptance] Automated art acceptance finished. Status={_report.Status}. Report={Path.Combine(_outputRoot, "report.json")}");

        CompleteRun();
    }

    private bool TryPrepareRun() {
        try {
            PrepareOutputDirectories();
            InitializeReport();
            return true;
        } catch (Exception ex) {
            Debug.LogError($"[ArtAcceptance] Failed to prepare run: {ex}");
            return false;
        }
    }

    private IEnumerator RunStep(string stepName, Func<IEnumerator> stepFactory) {
        Debug.Log($"[ArtAcceptance] Step started: {stepName}");

        IEnumerator routine = null;
        Exception factoryException = null;
        try {
            routine = stepFactory();
        } catch (Exception ex) {
            factoryException = ex;
        }

        if (factoryException != null) {
            AddError($"Step [{stepName}] failed before execution: {factoryException.Message}");
            Debug.LogError($"[ArtAcceptance] Step failed before execution: {stepName}\n{factoryException}");
            yield break;
        }

        Stack<IEnumerator> routineStack = new Stack<IEnumerator>();
        if (routine != null) {
            routineStack.Push(routine);
        }

        while (routineStack.Count > 0) {
            IEnumerator activeRoutine = routineStack.Peek();
            object current = null;
            bool hasNext = false;
            Exception moveException = null;

            try {
                hasNext = activeRoutine != null && activeRoutine.MoveNext();
                if (hasNext) {
                    current = activeRoutine.Current;
                }
            } catch (Exception ex) {
                moveException = ex;
            }

            if (moveException != null) {
                AddError($"Step [{stepName}] crashed: {moveException.Message}");
                Debug.LogError($"[ArtAcceptance] Step crashed: {stepName}\n{moveException}");
                yield break;
            }

            if (!hasNext) {
                routineStack.Pop();
                continue;
            }

            IEnumerator nestedRoutine = current as IEnumerator;
            if (nestedRoutine != null) {
                routineStack.Push(nestedRoutine);
                continue;
            }

            yield return current;
        }

        Debug.Log($"[ArtAcceptance] Step finished: {stepName}");
    }

    private void LogFlowCheckpoint(string stepName) {
        Debug.Log($"[ArtAcceptance] Flow checkpoint reached after step: {stepName}");
    }

    private void SafeExecute(string stepName, Action action) {
        try {
            action();
            Debug.Log($"[ArtAcceptance] Step finished: {stepName}");
        } catch (Exception ex) {
            AddError($"Step [{stepName}] crashed: {ex.Message}");
            Debug.LogError($"[ArtAcceptance] Step crashed: {stepName}\n{ex}");
        }
    }

    private void CompleteRun() {
        _manualRunActive = false;
        _manualYieldPending = false;
        _manualYieldUntilTime = 0f;
        _manualRoutineStack.Clear();
        _activeRunner = null;

#if UNITY_EDITOR
        if (_autoExitPlayMode) {
            EditorApplication.isPlaying = false;
        }
#endif
    }

    private void PrepareOutputDirectories() {
        _runID = DateTime.Now.ToString("yyyyMMdd_HHmmss");
        string logsRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs"));
        _outputRoot = Path.Combine(logsRoot, "ArtAcceptance", "latest");
        _screenshotsRoot = Path.Combine(_outputRoot, "screenshots");

        if (Directory.Exists(_screenshotsRoot)) {
            Directory.Delete(_screenshotsRoot, true);
        }

        Directory.CreateDirectory(_screenshotsRoot);
    }

    private void InitializeReport() {
        _report.SchemaVersion = SchemaVersion;
        _report.RunID = _runID;
        _report.Project = ProjectName;
        _report.Mode = ModeAuto;
        _report.StartedAt = DateTime.Now.ToString("o");
        _report.OutputRoot = "UnityClient/Logs/ArtAcceptance/latest";
        _report.ReferenceResolution = new ArtAcceptanceResolution {
            Width = ReferenceWidth,
            Height = ReferenceHeight
        };
        _report.ActualResolution = new ArtAcceptanceResolution {
            Width = ReferenceWidth,
            Height = ReferenceHeight
        };
        _report.Status = "RUNNING";
        _report.IsRunning = true;

        _uiSnapshot.SchemaVersion = SchemaVersion;
        _uiSnapshot.RunID = _runID;
    }

    private IEnumerator WaitForRuntimeReady() {
        Debug.Log("[ArtAcceptance] Waiting for runtime readiness...");
        float start = Time.realtimeSinceStartup;
        float nextDiagnosticAt = 0f;
        while (Time.realtimeSinceStartup - start < 12f) {
            if (GameRoot.Core != null &&
                GameRoot.Core.CurrentPlayer != null &&
                GameRoot.Core.CurrentPlayer.ActiveDoll != null &&
                GameFlowController.Instance != null) {
                _runtimeCoreReady = true;
                Debug.Log("[ArtAcceptance] Runtime ready.");
                yield return WaitFrames(6);
                yield break;
            }

            float elapsed = Time.realtimeSinceStartup - start;
            if (elapsed >= nextDiagnosticAt) {
                Debug.Log($"[ArtAcceptance] Runtime readiness probe: elapsed={elapsed:0.0}s, {BuildRuntimeReadinessSummary()}");
                nextDiagnosticAt += 3f;
            }

            yield return null;
        }

        AddError($"Runtime did not become ready within timeout. {BuildRuntimeReadinessSummary()}");
        Debug.LogWarning("[ArtAcceptance] Runtime readiness timeout. Continuing with diagnostic captures where possible.");
    }

    private string BuildRuntimeReadinessSummary() {
        bool hasCore = GameRoot.Core != null;
        bool hasPlayer = GameRoot.Core?.CurrentPlayer != null;
        bool hasDoll = GameRoot.Core?.CurrentPlayer?.ActiveDoll != null;
        bool hasFlow = GameFlowController.Instance != null;
        return $"Core={hasCore}, Player={hasPlayer}, ActiveDoll={hasDoll}, FlowController={hasFlow}";
    }

    private void CaptureGlobalEnvironment() {
        CanvasScaler scaler = FindObjectOfType<CanvasScaler>();
        if (scaler == null) {
            _report.CanvasScaler = new ArtAcceptanceCanvasScalerInfo { Found = false };
            AddError("Main CanvasScaler not found.");
        } else {
            _report.CanvasScaler = new ArtAcceptanceCanvasScalerInfo {
                Found = true,
                Mode = scaler.uiScaleMode.ToString(),
                ReferenceResolution = $"{Mathf.RoundToInt(scaler.referenceResolution.x)}x{Mathf.RoundToInt(scaler.referenceResolution.y)}",
                MatchWidthOrHeight = scaler.matchWidthOrHeight
            };

            if (scaler.uiScaleMode != CanvasScaler.ScaleMode.ScaleWithScreenSize) {
                AddError($"CanvasScaler mode should be ScaleWithScreenSize, actual={scaler.uiScaleMode}.");
            }

            if (Mathf.RoundToInt(scaler.referenceResolution.x) != ReferenceWidth ||
                Mathf.RoundToInt(scaler.referenceResolution.y) != ReferenceHeight) {
                AddError($"CanvasScaler reference resolution should be {ReferenceWidth}x{ReferenceHeight}, actual={scaler.referenceResolution}.");
            }
        }

        if (Screen.width != ReferenceWidth || Screen.height != ReferenceHeight) {
            Debug.Log($"[ArtAcceptance] Game View resolution is {Screen.width}x{Screen.height}; screenshots render at {ReferenceWidth}x{ReferenceHeight}.");
        }
    }

    private IEnumerator CaptureWorkshopMain() {
        Debug.Log("[ArtAcceptance] Capturing workshop_main...");
        ArtAcceptanceCaptureRecord capture = BeginCapture("workshop_main", "screenshots/workshop_main.png");

        if (!RequireRuntimeCore(capture) || !RequireFlowController(capture)) {
            CompleteSkipped(capture);
            yield break;
        }

        GameFlowController.Instance.EnterWorkshop();
        yield return WaitForVisualStable();
        yield return CaptureCurrentScreen(capture);
    }

    private IEnumerator CaptureSellPanel() {
        Debug.Log("[ArtAcceptance] Capturing sell_panel...");
        ArtAcceptanceCaptureRecord capture = BeginCapture("sell_panel", "screenshots/sell_panel.png");

        if (!RequireRuntimeCore(capture) || !RequireFlowController(capture)) {
            CompleteSkipped(capture);
            yield break;
        }

        GameFlowController.Instance.EnterWorkshop();
        yield return WaitForVisualStable();

        WorkshopUIController workshopController = FindObjectOfType<WorkshopUIController>();
        if (workshopController == null) {
            capture.Warnings.Add("WorkshopUIController not found for sell panel capture.");
            CompleteSkipped(capture);
            yield break;
        }

        workshopController.OpenSellPanel();
        yield return WaitForVisualStable();
        yield return CaptureCurrentScreen(capture);
        CloseWorkshopAcceptanceOverlays(workshopController);
    }

    private IEnumerator CaptureProstheticPanel() {
        Debug.Log("[ArtAcceptance] Capturing prosthetic_panel...");
        ArtAcceptanceCaptureRecord capture = BeginCapture("prosthetic_panel", "screenshots/prosthetic_panel.png");

        if (!RequireRuntimeCore(capture) || !RequireFlowController(capture)) {
            CompleteSkipped(capture);
            yield break;
        }

        GameFlowController.Instance.EnterWorkshop();
        yield return WaitForVisualStable();

        WorkshopUIController workshopController = FindObjectOfType<WorkshopUIController>();
        if (workshopController == null) {
            capture.Warnings.Add("WorkshopUIController not found for prosthetic panel capture.");
            CompleteSkipped(capture);
            yield break;
        }

        workshopController.OpenProstheticPanel();
        yield return WaitForVisualStable();
        yield return CaptureCurrentScreen(capture);
        CloseWorkshopAcceptanceOverlays(workshopController);
    }

    private IEnumerator CaptureLayerSelect() {
        Debug.Log("[ArtAcceptance] Capturing layer_select...");
        ArtAcceptanceCaptureRecord capture = BeginCapture("layer_select", "screenshots/layer_select.png");

        if (!RequireRuntimeCore(capture) || !RequireFlowController(capture) || !RequireDungeon(capture)) {
            CompleteSkipped(capture);
            yield break;
        }

        int previousHighestUnlockedLayer = GameRoot.Core.CurrentPlayer.HighestUnlockedDungeonLayer;
        UnlockConfiguredLayersForAcceptance();
        GameFlowController.Instance.EnterWorkshop();
        yield return WaitForVisualStable();

        WorkshopUIController workshopController = FindObjectOfType<WorkshopUIController>();
        if (workshopController == null) {
            GameRoot.Core.CurrentPlayer.HighestUnlockedDungeonLayer = previousHighestUnlockedLayer;
            capture.Warnings.Add("WorkshopUIController not found for layer select capture.");
            CompleteSkipped(capture);
            yield break;
        }

        workshopController.OpenDungeonStartLayerPanel();
        yield return WaitForVisualStable();
        yield return CaptureCurrentScreen(capture);
        CloseWorkshopAcceptanceOverlays(workshopController);
        GameRoot.Core.CurrentPlayer.HighestUnlockedDungeonLayer = previousHighestUnlockedLayer;
    }

    private IEnumerator CaptureDungeonMap() {
        Debug.Log("[ArtAcceptance] Capturing dungeon_map...");
        ArtAcceptanceCaptureRecord capture = BeginCapture("dungeon_map", "screenshots/dungeon_map.png");

        if (!RequireRuntimeCore(capture) || !RequireFlowController(capture) || !RequireDungeon(capture)) {
            CompleteSkipped(capture);
            yield break;
        }

        int layerID = ResolveAcceptanceLayerID();
        if (layerID <= 0) {
            capture.Warnings.Add("No unlocked dungeon layer config found.");
            CompleteSkipped(capture);
            yield break;
        }

        bool started = GameRoot.Core.Dungeon.StartRunAtLayer(layerID);
        if (!started) {
            capture.Warnings.Add($"Dungeon.StartRunAtLayer({layerID}) returned false.");
            CompleteSkipped(capture);
            yield break;
        }

        // Switch the visible screen to DungeonMap panel.
        // Without this, the workshopPanel remains active and the screenshot captures workshop instead.
        GameFlowController.Instance.EnterDungeonMap();
        yield return WaitForVisualStable();
        DungeonMapUIController mapController = FindObjectOfType<DungeonMapUIController>();
        if (mapController != null) {
            mapController.RefreshMap();
        }

        yield return WaitForVisualStable();
        yield return CaptureCurrentScreen(capture);
    }

    private IEnumerator CaptureSafeRoom() {
        Debug.Log("[ArtAcceptance] Capturing safe_room...");
        ArtAcceptanceCaptureRecord capture = BeginCapture("safe_room", "screenshots/safe_room.png");

        if (!RequireRuntimeCore(capture) || !RequireFlowController(capture)) {
            CompleteSkipped(capture);
            yield break;
        }

        SafeRoomNode node = new SafeRoomNode {
            NodeID = "art_acceptance_safe_room_preview"
        };

        GameFlowController.Instance.EnterSafeRoom(node);
        yield return WaitForVisualStable();
        yield return CaptureCurrentScreen(capture);
    }

    private IEnumerator CaptureStairsRoom() {
        Debug.Log("[ArtAcceptance] Capturing stairs_room...");
        ArtAcceptanceCaptureRecord capture = BeginCapture("stairs_room", "screenshots/stairs_room.png");

        if (!RequireRuntimeCore(capture) || !RequireFlowController(capture) || !RequireDungeon(capture)) {
            CompleteSkipped(capture);
            yield break;
        }

        EnsureAcceptanceDungeonLayer();
        int layerID = GameRoot.Core?.Dungeon?.CurrentLayer?.LayerID ?? ResolveAcceptanceLayerID();
        StairsNode node = new StairsNode {
            NodeID = "art_acceptance_stairs_room_preview",
            LayerID = Mathf.Max(1, layerID)
        };

        GameFlowController.Instance.EnterStairs(node);
        yield return WaitForVisualStable();
        yield return CaptureCurrentScreen(capture);
    }

    private IEnumerator CaptureCombatHud() {
        Debug.Log("[ArtAcceptance] Capturing combat_hud...");
        ArtAcceptanceCaptureRecord capture = BeginCapture("combat_hud", "screenshots/combat_hud.png");

        if (!RequireRuntimeCore(capture) || !RequireFlowController(capture) || GameRoot.Core?.Combat == null) {
            if (GameRoot.Core?.Combat == null) {
                capture.Errors.Add("CombatSystem is missing.");
                AddError($"Capture [{capture.ScreenTag}] requires CombatSystem.");
            }
            CompleteSkipped(capture);
            yield break;
        }

        List<string> monsterIDs = ResolveAcceptanceMonsterIDs();
        if (monsterIDs.Count == 0) {
            capture.Warnings.Add("No monster config found for combat HUD capture.");
            CompleteSkipped(capture);
            yield break;
        }

        GameRoot.Core.Combat.StartCombat(monsterIDs);
        GameFlowController.Instance.EnterCombat();
        yield return WaitForVisualStable();
        yield return CaptureCurrentScreen(capture);
    }

    private IEnumerator CaptureInventoryLoot() {
        Debug.Log("[ArtAcceptance] Capturing inventory_loot...");
        ArtAcceptanceCaptureRecord capture = BeginCapture("inventory_loot", "screenshots/inventory_loot.png");

        if (!RequireRuntimeCore(capture) || !RequireFlowController(capture)) {
            CompleteSkipped(capture);
            yield break;
        }

        CombatLootPickupResult lootResult = BuildAcceptanceLootResult();
        if (lootResult == null || lootResult.OfferedItems.Count == 0) {
            capture.Warnings.Add("No item config found for inventory loot capture.");
            CompleteSkipped(capture);
            yield break;
        }

        Debug.Log("[ArtAcceptance] inventory_loot uses an acceptance-only loot payload; real reward settlement is not invoked.");
        GameFlowController.Instance.EnterCombatLoot(lootResult);
        yield return WaitForVisualStable();
        yield return CaptureCurrentScreen(capture);
    }

    private IEnumerator CaptureSettlement() {
        Debug.Log("[ArtAcceptance] Capturing settlement...");
        ArtAcceptanceCaptureRecord capture = BeginCapture("settlement", "screenshots/settlement.png");

        if (!RequireRuntimeCore(capture) || !RequireFlowController(capture)) {
            CompleteSkipped(capture);
            yield break;
        }

        DungeonSettlementResult settlementResult = BuildAcceptanceSettlementResult();
        if (settlementResult == null) {
            capture.Warnings.Add("Failed to build settlement preview payload.");
            CompleteSkipped(capture);
            yield break;
        }

        GameFlowController.Instance.EnterSettlementPreview(settlementResult);
        yield return WaitForVisualStable();
        yield return CaptureCurrentScreen(capture);
    }

    private ArtAcceptanceCaptureRecord BeginCapture(string screenTag, string relativeFile) {
        ArtAcceptanceCaptureRecord capture = new ArtAcceptanceCaptureRecord {
            Index = _report.Captures.Count + 1,
            ScreenTag = screenTag,
            File = relativeFile,
            CapturedAt = DateTime.Now.ToString("o"),
            Status = "running",
            Resolution = $"{ReferenceWidth}x{ReferenceHeight}"
        };
        _report.Captures.Add(capture);
        return capture;
    }

    private IEnumerator CaptureCurrentScreen(ArtAcceptanceCaptureRecord capture) {
        capture.ActiveControllers = CollectActiveControllers();
        ArtAcceptanceUiCaptureSnapshot uiCapture = BuildUiCaptureSnapshot(capture.ScreenTag);
        _uiSnapshot.Captures.Add(uiCapture);
        ApplyUiRisksToCapture(uiCapture, capture);
        ApplyRequiredUiChecksToCapture(uiCapture, capture);

        string absolutePath = Path.Combine(_outputRoot, capture.File.Replace('/', Path.DirectorySeparatorChar));
        Directory.CreateDirectory(Path.GetDirectoryName(absolutePath));

        // Wait for UI layout to settle before capturing.
        yield return WaitSecondsRealtime(0.1f);
        Canvas.ForceUpdateCanvases();
        yield return WaitSecondsRealtime(0.05f);

        RenderCameraScreenshot(absolutePath, capture);

        capture.Status = File.Exists(absolutePath) ? "captured" : "failed";
        if (capture.Status == "failed") {
            capture.Errors.Add($"Screenshot file was not created: {absolutePath}");
            AddError($"Capture [{capture.ScreenTag}] failed to write screenshot.");
        }
    }

    /// <summary>
    /// Synchronous screenshot via Camera.Render + RenderTexture.
    /// This approach works at any point in the frame — no WaitForEndOfFrame required.
    /// Temporarily converts Screen Space Overlay canvases to Camera mode so UI is captured.
    /// </summary>
    private void RenderCameraScreenshot(string absolutePath, ArtAcceptanceCaptureRecord capture) {
        Camera camera = Camera.main;
        if (camera == null) {
            Camera[] cameras = FindObjectsOfType<Camera>();
            foreach (Camera cam in cameras) {
                if (cam != null && cam.isActiveAndEnabled) {
                    camera = cam;
                    break;
                }
            }
        }

        if (camera == null) {
            AddError($"No active camera found for screenshot [{absolutePath}].");
            Debug.LogError($"[ArtAcceptance] No active camera for screenshot: {absolutePath}");
            return;
        }

        int width = ReferenceWidth;
        int height = ReferenceHeight;

        // Temporarily attach Screen Space Overlay canvases to this camera so they render.
        Canvas[] allCanvases = FindObjectsOfType<Canvas>();
        var overlayBackup = new System.Collections.Generic.List<(Canvas canvas, RenderMode mode, Camera cam)>();
        foreach (Canvas c in allCanvases) {
            if (c != null && c.isActiveAndEnabled && c.renderMode == RenderMode.ScreenSpaceOverlay) {
                overlayBackup.Add((c, c.renderMode, c.worldCamera));
                c.renderMode = RenderMode.ScreenSpaceCamera;
                c.worldCamera = camera;
            }
        }

        RenderTexture renderTexture = null;
        RenderTexture previousActive = RenderTexture.active;
        RenderTexture previousTarget = camera.targetTexture;
        float previousAspect = camera.aspect;
        Texture2D screenshot = null;

        try {
            renderTexture = new RenderTexture(width, height, 24, RenderTextureFormat.ARGB32);
            renderTexture.antiAliasing = 1;
            renderTexture.Create();

            camera.targetTexture = renderTexture;
            camera.aspect = (float)width / height;
            camera.Render();

            RenderTexture.active = renderTexture;
            screenshot = new Texture2D(width, height, TextureFormat.RGB24, false);
            screenshot.ReadPixels(new Rect(0, 0, width, height), 0, 0);
            screenshot.Apply();

            ApplyScreenshotContentChecks(screenshot, capture);
            File.WriteAllBytes(absolutePath, screenshot.EncodeToPNG());
            Debug.Log($"[ArtAcceptance] Screenshot written: {absolutePath} ({width}x{height})");
        } catch (Exception ex) {
            AddError($"Failed to write screenshot [{absolutePath}]: {ex.Message}");
            Debug.LogError($"[ArtAcceptance] Failed to write screenshot: {absolutePath}\n{ex}");
        } finally {
            // Restore camera state.
            camera.targetTexture = previousTarget;
            camera.aspect = previousAspect;
            RenderTexture.active = previousActive;

            if (renderTexture != null) {
                renderTexture.Release();
                Destroy(renderTexture);
            }
            if (screenshot != null) {
                Destroy(screenshot);
            }

            // Restore overlay canvases.
            foreach (var backup in overlayBackup) {
                if (backup.canvas != null) {
                    backup.canvas.renderMode = backup.mode;
                    backup.canvas.worldCamera = backup.cam;
                }
            }
        }
    }

    private void CompleteSkipped(ArtAcceptanceCaptureRecord capture) {
        capture.Status = "skipped";
        capture.ActiveControllers = CollectActiveControllers();
        if (capture.Warnings.Count == 0 && capture.Errors.Count == 0) {
            capture.Warnings.Add("Capture skipped because required runtime state was unavailable.");
        }
    }

    private IEnumerator WaitForVisualStable() {
        yield return WaitSecondsRealtime(0.1f);
        Canvas.ForceUpdateCanvases();
        yield return WaitSecondsRealtime(0.1f);
        Canvas.ForceUpdateCanvases();
    }

    private IEnumerator WaitFrames(int frameCount) {
        yield return WaitSecondsRealtime(Mathf.Max(1, frameCount) / 60f);
    }

    private IEnumerator WaitSecondsRealtime(float seconds) {
        float deadline = Time.realtimeSinceStartup + Mathf.Max(0.01f, seconds);
        while (Time.realtimeSinceStartup < deadline) {
            yield return null;
        }
    }

    private bool RequireRuntimeCore(ArtAcceptanceCaptureRecord capture) {
        if (_runtimeCoreReady ||
            (GameRoot.Core != null && GameRoot.Core.CurrentPlayer != null && GameRoot.Core.CurrentPlayer.ActiveDoll != null)) {
            return true;
        }

        capture.Errors.Add($"Runtime core is missing. {BuildRuntimeReadinessSummary()}");
        AddError($"Capture [{capture.ScreenTag}] requires runtime core.");
        return false;
    }

    private bool RequireFlowController(ArtAcceptanceCaptureRecord capture) {
        if (GameFlowController.Instance != null) {
            return true;
        }

        capture.Errors.Add("GameFlowController.Instance is missing.");
        AddError($"Capture [{capture.ScreenTag}] requires GameFlowController.");
        return false;
    }

    private bool RequireDungeon(ArtAcceptanceCaptureRecord capture) {
        if (GameRoot.Core?.Dungeon != null) {
            return true;
        }

        capture.Errors.Add("DungeonManager is missing.");
        AddError($"Capture [{capture.ScreenTag}] requires DungeonManager.");
        return false;
    }

    private int ResolveAcceptanceLayerID() {
        if (GameRoot.Core?.CurrentPlayer == null || ConfigManager.Dungeons == null) {
            return 0;
        }

        int highestUnlocked = Mathf.Max(1, GameRoot.Core.CurrentPlayer.HighestUnlockedDungeonLayer);
        int selected = 0;
        foreach (var kvp in ConfigManager.Dungeons) {
            if (kvp.Key <= highestUnlocked && (selected == 0 || kvp.Key < selected)) {
                selected = kvp.Key;
            }
        }

        return selected;
    }

    private void CloseWorkshopAcceptanceOverlays(WorkshopUIController workshopController = null) {
        WorkshopUIController controller = workshopController != null
            ? workshopController
            : FindObjectOfType<WorkshopUIController>();
        if (controller == null) {
            return;
        }

        controller.CloseSellPanel();
        controller.CloseProstheticPanel();
        controller.CloseDungeonStartLayerPanel();
    }

    private void UnlockConfiguredLayersForAcceptance() {
        if (GameRoot.Core?.CurrentPlayer == null || ConfigManager.Dungeons == null || ConfigManager.Dungeons.Count == 0) {
            return;
        }

        int highestConfiguredLayer = 1;
        foreach (int layerID in ConfigManager.Dungeons.Keys) {
            if (layerID > highestConfiguredLayer) {
                highestConfiguredLayer = layerID;
            }
        }

        GameRoot.Core.CurrentPlayer.HighestUnlockedDungeonLayer = Mathf.Max(
            GameRoot.Core.CurrentPlayer.HighestUnlockedDungeonLayer,
            highestConfiguredLayer);
    }

    private void EnsureAcceptanceDungeonLayer() {
        if (GameRoot.Core?.Dungeon == null || GameRoot.Core.Dungeon.CurrentLayer != null) {
            return;
        }

        int layerID = ResolveAcceptanceLayerID();
        if (layerID > 0) {
            GameRoot.Core.Dungeon.StartRunAtLayer(layerID);
        }
    }

    private List<string> ResolveAcceptanceMonsterIDs() {
        List<string> monsterIDs = new List<string>();
        DungeonLayer layer = GameRoot.Core?.Dungeon?.CurrentLayer;
        if (layer?.RootNode != null) {
            NodeBase node = layer.RootNode;
            while (node != null) {
                CombatNode combatNode = node as CombatNode;
                if (combatNode != null && combatNode.MonsterIDs != null && combatNode.MonsterIDs.Count > 0) {
                    monsterIDs.AddRange(combatNode.MonsterIDs);
                    return monsterIDs;
                }

                node = node.NextNodes != null && node.NextNodes.Count > 0 ? node.NextNodes[0] : null;
            }
        }

        if (ConfigManager.Monsters == null) {
            return monsterIDs;
        }

        foreach (var kvp in ConfigManager.Monsters) {
            if (kvp.Value != null && !string.IsNullOrEmpty(kvp.Key)) {
                monsterIDs.Add(kvp.Key);
                break;
            }
        }

        return monsterIDs;
    }

    private CombatLootPickupResult BuildAcceptanceLootResult() {
        CombatLootPickupResult result = new CombatLootPickupResult {
            NodeID = "art_acceptance_loot_preview"
        };

        if (ConfigManager.Items == null) {
            return result;
        }

        int count = 0;
        foreach (var kvp in ConfigManager.Items) {
            if (count >= 4) {
                break;
            }

            ItemEntity item = ConfigManager.CreateItem(kvp.Key);
            if (item == null) {
                continue;
            }

            result.OfferedItems.Add(item);
            result.TotalEstimatedValue += item.BaseValue;
            count++;
        }

        return result;
    }

    private DungeonSettlementResult BuildAcceptanceSettlementResult() {
        DungeonSettlementResult result = new DungeonSettlementResult {
            IsVictory = true,
            StashCountAfterSettlement = GameRoot.Core?.CurrentPlayer?.StashInventory?.Count ?? 0
        };

        if (ConfigManager.Items == null) {
            return result;
        }

        int count = 0;
        foreach (var kvp in ConfigManager.Items) {
            if (count >= 3) {
                break;
            }

            ItemEntity item = ConfigManager.CreateItem(kvp.Key);
            if (item == null) {
                continue;
            }

            result.PickedUpCount++;
            result.PickedUpEstimatedValue += item.BaseValue;
            result.PickedUpNames.Add(item.Name);

            if (count < 2) {
                result.BroughtOutCount++;
                result.BroughtOutEstimatedValue += item.BaseValue;
                result.BroughtOutNames.Add(item.Name);
                result.LootTransferredCount++;
                result.LootEstimatedValue += item.BaseValue;
                result.LootNames.Add(item.Name);
            } else {
                result.LostCount++;
                result.LostEstimatedValue += item.BaseValue;
                result.LostNames.Add(item.Name);
            }

            count++;
        }

        return result;
    }

    private ArtAcceptanceRegistrySnapshotReport BuildRegistrySnapshot() {
        ArtAcceptanceRegistrySnapshotReport snapshot = new ArtAcceptanceRegistrySnapshotReport {
            SchemaVersion = SchemaVersion,
            RunID = _runID
        };

        VisualAssetRegistry registry = Resources.Load<VisualAssetRegistry>("VisualAssetRegistry");
        snapshot.RegistryFound = registry != null;
        if (registry == null) {
            return snapshot;
        }

        registry.RebuildLookup();
        snapshot.EntryCount = registry.Entries != null ? registry.Entries.Count : 0;
        snapshot.MissingSpriteFound = registry.MissingSprite != null;
        snapshot.MissingSpriteVisualID = VisualAssetService.MissingSpriteVisualID;
        snapshot.MissingSpriteName = registry.MissingSprite != null ? registry.MissingSprite.name : string.Empty;

        foreach (string visualID in GetRequiredP0VisualIDs()) {
            if (!registry.TryGetEntry(visualID, out VisualAssetEntry entry) || entry == null || entry.Sprite == null) {
                snapshot.MissingRequiredVisualIDs.Add(visualID);
            }
        }

        if (registry.Entries != null) {
            foreach (VisualAssetEntry entry in registry.Entries) {
                if (entry == null || string.IsNullOrEmpty(entry.VisualID)) {
                    continue;
                }

                ArtAcceptanceRegistryEntrySnapshot entrySnapshot = new ArtAcceptanceRegistryEntrySnapshot {
                    VisualID = entry.VisualID,
                    SpriteName = entry.Sprite != null ? entry.Sprite.name : string.Empty,
                    HasSprite = entry.Sprite != null,
                    HasPrefab = entry.Prefab != null,
                    HasAudioClip = entry.AudioClip != null,
                    HasMaterial = entry.Material != null
                };

                if (entry.Sprite != null) {
                    entrySnapshot.TextureSize = entry.Sprite.texture != null
                        ? $"{entry.Sprite.texture.width}x{entry.Sprite.texture.height}"
                        : string.Empty;
                    entrySnapshot.SpriteRect = FormatRect(entry.Sprite.rect);
                    entrySnapshot.SpriteBorder = FormatVector4(entry.Sprite.border);
                }

                snapshot.Entries.Add(entrySnapshot);
            }
        }

        return snapshot;
    }

    private void ApplyRegistryResultToReport(ArtAcceptanceRegistrySnapshotReport snapshot) {
        _report.Registry.RegistryFound = snapshot.RegistryFound;
        _report.Registry.MissingSpriteFound = snapshot.MissingSpriteFound;
        _report.Registry.MissingSpriteVisualID = snapshot.MissingSpriteVisualID;
        _report.Registry.EntryCount = snapshot.EntryCount;
        _report.Registry.MissingRequiredVisualIDs = snapshot.MissingRequiredVisualIDs;

        if (!snapshot.RegistryFound) {
            AddError("VisualAssetRegistry was not found in Resources.");
        }

        if (!snapshot.MissingSpriteFound) {
            AddError("VisualAssetRegistry.MissingSprite is not assigned.");
        }

        foreach (string missingVisualID in snapshot.MissingRequiredVisualIDs) {
            AddError($"Required P0 VisualID is missing or has no sprite: {missingVisualID}");
        }
    }

    private string[] GetRequiredP0VisualIDs() {
        return new[] {
            VisualAssetService.UIPanelInfoID,
            VisualAssetService.UIButtonPrimaryID,
            VisualAssetService.UIButtonSecondaryID,
            VisualAssetService.UIButtonDangerID,
            VisualAssetService.UIInventoryChassisPanelID,
            VisualAssetService.UIInventorySlotAvailableID,
            VisualAssetService.UIInventorySlotLockedID,
            VisualAssetService.UIInventorySlotHoverID,
            VisualAssetService.UIInventorySlotValidID,
            VisualAssetService.UIInventorySlotInvalidID,
            VisualAssetService.UILootPickupPanelID,
            VisualAssetService.UILootDropZoneID,
            VisualAssetService.UICombatEnemyCardID,
            VisualAssetService.UICombatEnemyCardSelectedID,
            VisualAssetService.UICombatStatusBarHpID,
            VisualAssetService.UICombatStatusBarShieldID,
            VisualAssetService.UICombatApPipID,
            VisualAssetService.UICombatTurnBannerID,
            VisualAssetService.UIIconMoneyID
        };
    }

    private ArtAcceptanceUiCaptureSnapshot BuildUiCaptureSnapshot(string screenTag) {
        ArtAcceptanceUiCaptureSnapshot capture = new ArtAcceptanceUiCaptureSnapshot {
            ScreenTag = screenTag
        };

        Canvas[] canvases = FindObjectsOfType<Canvas>();
        foreach (Canvas canvas in canvases) {
            if (canvas == null || !canvas.gameObject.activeInHierarchy) {
                continue;
            }

            ArtAcceptanceCanvasSnapshot canvasSnapshot = new ArtAcceptanceCanvasSnapshot {
                Name = canvas.name,
                Path = BuildTransformPath(canvas.transform),
                RenderMode = canvas.renderMode.ToString(),
                SortingOrder = canvas.sortingOrder
            };

            RectTransform[] rects = canvas.GetComponentsInChildren<RectTransform>(false);
            foreach (RectTransform rect in rects) {
                if (rect == null) {
                    continue;
                }

                ArtAcceptanceUiElementSnapshot element = BuildUiElementSnapshot(canvas.transform, rect);
                canvasSnapshot.Elements.Add(element);
            }

            capture.Canvases.Add(canvasSnapshot);
        }

        return capture;
    }

    private ArtAcceptanceUiElementSnapshot BuildUiElementSnapshot(Transform canvasTransform, RectTransform rect) {
        ArtAcceptanceUiElementSnapshot element = new ArtAcceptanceUiElementSnapshot {
            Name = rect.name,
            Path = BuildRelativePath(canvasTransform, rect.transform),
            Active = rect.gameObject.activeInHierarchy,
            AnchoredPosition = FormatVector2(rect.anchoredPosition),
            SizeDelta = FormatVector2(rect.sizeDelta),
            AnchorMin = FormatVector2(rect.anchorMin),
            AnchorMax = FormatVector2(rect.anchorMax),
            Pivot = FormatVector2(rect.pivot)
        };

        Component[] components = rect.GetComponents<Component>();
        foreach (Component component in components) {
            element.ComponentTypes.Add(component != null ? component.GetType().Name : "MissingScript");
        }

        Image image = rect.GetComponent<Image>();
        if (image != null) {
            element.Image = new ArtAcceptanceImageSnapshot {
                Found = true,
                Enabled = image.enabled,
                SpriteName = image.sprite != null ? image.sprite.name : string.Empty,
                Type = image.type.ToString(),
                RaycastTarget = image.raycastTarget,
                PreserveAspect = image.preserveAspect
            };
        }

        Button button = rect.GetComponent<Button>();
        if (button != null) {
            element.Button = new ArtAcceptanceButtonSnapshot {
                Found = true,
                Interactable = button.interactable,
                HasTargetGraphic = button.targetGraphic != null
            };
        }

        Text text = rect.GetComponent<Text>();
        if (text != null) {
            element.Text = new ArtAcceptanceTextSnapshot {
                Found = true,
                Enabled = text.enabled,
                TextLength = string.IsNullOrEmpty(text.text) ? 0 : text.text.Length,
                RaycastTarget = text.raycastTarget,
                FontSize = text.fontSize
            };
        }

        CanvasGroup canvasGroup = rect.GetComponent<CanvasGroup>();
        if (canvasGroup != null) {
            element.CanvasGroup = new ArtAcceptanceCanvasGroupSnapshot {
                Found = true,
                Alpha = canvasGroup.alpha,
                Interactable = canvasGroup.interactable,
                BlocksRaycasts = canvasGroup.blocksRaycasts
            };
        }

        element.Risks = EvaluateElementRisks(rect, element);
        return element;
    }

    private List<string> EvaluateElementRisks(RectTransform rect, ArtAcceptanceUiElementSnapshot element) {
        List<string> risks = new List<string>();

        if (!IsElementVisibleForAcceptance(rect)) {
            return risks;
        }

        Image imageComponent = rect.GetComponent<Image>();
        bool imageVisible = element.Image != null &&
            element.Image.Found &&
            imageComponent != null &&
            IsGraphicVisibleForAcceptance(imageComponent);

        if (imageVisible) {
            if (string.IsNullOrEmpty(element.Image.SpriteName)) {
                risks.Add("ImageEnabledButSpriteEmpty");
            }

            if (element.Image.SpriteName == VisualAssetService.MissingSpriteVisualID ||
                element.Image.SpriteName.IndexOf("missing", StringComparison.OrdinalIgnoreCase) >= 0) {
                risks.Add("MissingSpriteVisible");
            }

            if (element.Image.RaycastTarget &&
                IsElementRaycastRelevant(rect) &&
                rect.sizeDelta.x >= 500f &&
                rect.sizeDelta.y >= 300f &&
                !element.Button.Found) {
                risks.Add("LargeNonButtonImageBlocksRaycasts");
            }

            if (RequiresSlicedSprite(element.Image.SpriteName) && element.Image.Type != Image.Type.Sliced.ToString()) {
                risks.Add("ExpectedSlicedImageButTypeIsNotSliced");
            }
        }

        if (element.Button != null && element.Button.Found) {
            if (!element.Button.HasTargetGraphic) {
                risks.Add("ButtonMissingTargetGraphic");
            }

            if (element.Image == null || !element.Image.Found || string.IsNullOrEmpty(element.Image.SpriteName)) {
                risks.Add("ButtonMissingImageSprite");
            }
        }

        GridSlotUI slot = rect.GetComponent<GridSlotUI>();
        if (slot != null && (Mathf.Abs(rect.sizeDelta.x - 100f) > 0.1f || Mathf.Abs(rect.sizeDelta.y - 100f) > 0.1f)) {
            risks.Add($"InventorySlotSizeNot100x100:{FormatVector2(rect.sizeDelta)}");
        }

        return risks;
    }

    private bool IsElementVisibleForAcceptance(RectTransform rect) {
        if (rect == null || !rect.gameObject.activeInHierarchy) {
            return false;
        }

        Canvas canvas = rect.GetComponentInParent<Canvas>();
        if (canvas != null && !canvas.enabled) {
            return false;
        }

        CanvasGroup[] groups = rect.GetComponentsInParent<CanvasGroup>(true);
        foreach (CanvasGroup group in groups) {
            if (group != null && group.alpha <= VisibleAlphaThreshold) {
                return false;
            }
        }

        return true;
    }

    private bool IsGraphicVisibleForAcceptance(Graphic graphic) {
        if (graphic == null || !graphic.enabled) {
            return false;
        }

        if (graphic.color.a <= VisibleAlphaThreshold) {
            return false;
        }

        return graphic.canvasRenderer == null || graphic.canvasRenderer.GetAlpha() > VisibleAlphaThreshold;
    }

    private bool IsElementRaycastRelevant(RectTransform rect) {
        CanvasGroup[] groups = rect.GetComponentsInParent<CanvasGroup>(true);
        foreach (CanvasGroup group in groups) {
            if (group != null && !group.blocksRaycasts) {
                return false;
            }
        }

        return true;
    }

    private bool RequiresSlicedSprite(string spriteName) {
        if (string.IsNullOrEmpty(spriteName)) {
            return false;
        }

        return spriteName.StartsWith("ui_button_", StringComparison.OrdinalIgnoreCase)
            || spriteName.StartsWith("ui_panel_", StringComparison.OrdinalIgnoreCase)
            || spriteName == VisualAssetService.UIInventoryChassisPanelID
            || spriteName == VisualAssetService.UILootPickupPanelID
            || spriteName == VisualAssetService.UILootDropZoneID
            || spriteName == VisualAssetService.UICombatEnemyCardID
            || spriteName == VisualAssetService.UICombatEnemyCardSelectedID
            || spriteName == VisualAssetService.UICombatStatusBarHpID
            || spriteName == VisualAssetService.UICombatStatusBarShieldID
            || spriteName == VisualAssetService.UICombatTurnBannerID;
    }

    private void ApplyRequiredUiChecksToCapture(ArtAcceptanceUiCaptureSnapshot uiCapture, ArtAcceptanceCaptureRecord capture) {
        if (uiCapture == null || capture == null) {
            return;
        }

        switch (uiCapture.ScreenTag) {
            case "inventory_loot":
                RequireActiveController(capture, nameof(CombatLootUIController));
                RequireVisibleElement(uiCapture, capture, "CombatLootPanel_Runtime");
                RequireVisibleElement(uiCapture, capture, "PickupPanel");
                RequireVisibleElement(uiCapture, capture, "LootDropZone");
                RequireVisibleElement(uiCapture, capture, "Continue_Button");
                RequireVisibleTextCount(uiCapture, capture, 2);
                break;
            case "settlement":
                RequireActiveController(capture, nameof(SettlementUIController));
                RequireVisibleElement(uiCapture, capture, "SettlementPanel_Runtime");
                RequireVisibleElement(uiCapture, capture, "Title_Text");
                RequireVisibleElement(uiCapture, capture, "Summary_Text");
                RequireVisibleElement(uiCapture, capture, "Loot_Text");
                RequireVisibleElement(uiCapture, capture, "Continue_Button");
                RequireVisibleTextCount(uiCapture, capture, 4);
                break;
        }
    }

    private void RequireActiveController(ArtAcceptanceCaptureRecord capture, string controllerName) {
        if (capture == null || string.IsNullOrEmpty(controllerName)) {
            return;
        }

        if (capture.ActiveControllers == null || !capture.ActiveControllers.Contains(controllerName)) {
            AddCaptureError(capture, $"Required active controller missing: {controllerName}.");
        }
    }

    private void RequireVisibleElement(ArtAcceptanceUiCaptureSnapshot uiCapture, ArtAcceptanceCaptureRecord capture, string elementName) {
        if (uiCapture == null || string.IsNullOrEmpty(elementName)) {
            return;
        }

        foreach (ArtAcceptanceCanvasSnapshot canvas in uiCapture.Canvases) {
            foreach (ArtAcceptanceUiElementSnapshot element in canvas.Elements) {
                if (element.Name == elementName && IsSnapshotElementVisible(element)) {
                    return;
                }
            }
        }

        AddCaptureError(capture, $"Required visible UI element missing: {elementName}.");
    }

    private void RequireVisibleTextCount(ArtAcceptanceUiCaptureSnapshot uiCapture, ArtAcceptanceCaptureRecord capture, int minCount) {
        if (uiCapture == null) {
            return;
        }

        int count = 0;
        foreach (ArtAcceptanceCanvasSnapshot canvas in uiCapture.Canvases) {
            foreach (ArtAcceptanceUiElementSnapshot element in canvas.Elements) {
                if (element.Text != null &&
                    element.Text.Found &&
                    element.Text.Enabled &&
                    element.Text.TextLength > 0 &&
                    IsSnapshotElementVisible(element)) {
                    count++;
                }
            }
        }

        if (count < minCount) {
            AddCaptureError(capture, $"Visible text count below requirement: actual={count}, required={minCount}.");
        }
    }

    private bool IsSnapshotElementVisible(ArtAcceptanceUiElementSnapshot element) {
        if (element == null || !element.Active) {
            return false;
        }

        return element.CanvasGroup == null || !element.CanvasGroup.Found || element.CanvasGroup.Alpha > VisibleAlphaThreshold;
    }

    private void ApplyScreenshotContentChecks(Texture2D screenshot, ArtAcceptanceCaptureRecord capture) {
        if (screenshot == null || capture == null) {
            return;
        }

        if (IsScreenshotVisuallyBlank(screenshot)) {
            AddCaptureError(capture, "Screenshot appears visually blank or near-solid color.");
        }
    }

    private bool IsScreenshotVisuallyBlank(Texture2D screenshot) {
        if (screenshot == null) {
            return true;
        }

        int width = screenshot.width;
        int height = screenshot.height;
        int stepX = Mathf.Max(1, width / 32);
        int stepY = Mathf.Max(1, height / 18);
        Color32 first = screenshot.GetPixel(0, 0);
        int sampled = 0;
        int different = 0;

        for (int y = 0; y < height; y += stepY) {
            for (int x = 0; x < width; x += stepX) {
                sampled++;
                Color32 current = screenshot.GetPixel(x, y);
                int delta =
                    Mathf.Abs(current.r - first.r) +
                    Mathf.Abs(current.g - first.g) +
                    Mathf.Abs(current.b - first.b);
                if (delta > 8) {
                    different++;
                }
            }
        }

        if (sampled == 0) {
            return true;
        }

        return different < Mathf.Max(4, sampled / 100);
    }

    private void ApplyUiRisksToCapture(ArtAcceptanceUiCaptureSnapshot uiCapture, ArtAcceptanceCaptureRecord capture) {
        foreach (ArtAcceptanceCanvasSnapshot canvas in uiCapture.Canvases) {
            foreach (ArtAcceptanceUiElementSnapshot element in canvas.Elements) {
                foreach (string risk in element.Risks) {
                    string message = $"{uiCapture.ScreenTag}:{element.Path}:{risk}";
                    if (!capture.Warnings.Contains(message)) {
                        capture.Warnings.Add(message);
                    }

                    if (risk == "MissingSpriteVisible") {
                        AddWarning(message);
                    }
                }
            }
        }
    }

    private List<string> CollectActiveControllers() {
        List<string> names = new List<string>();
        MonoBehaviour[] behaviours = FindObjectsOfType<MonoBehaviour>();
        foreach (MonoBehaviour behaviour in behaviours) {
            if (behaviour == null || !behaviour.isActiveAndEnabled) {
                continue;
            }

            string typeName = behaviour.GetType().Name;
            if (typeName.EndsWith("Controller", StringComparison.Ordinal) ||
                typeName == nameof(GameRoot) ||
                typeName == nameof(GameFlowController) ||
                typeName == nameof(ArtAcceptanceRunner)) {
                if (!names.Contains(typeName)) {
                    names.Add(typeName);
                }
            }
        }

        names.Sort(StringComparer.Ordinal);
        return names;
    }

    private void FinalizeReport() {
        _report.FinishedAt = DateTime.Now.ToString("o");
        _report.IsRunning = false;

        bool hasCaptured = false;
        foreach (ArtAcceptanceCaptureRecord capture in _report.Captures) {
            if (capture.Status == "captured") {
                hasCaptured = true;
            }

            foreach (string warning in capture.Warnings) {
                if (!_report.Warnings.Contains(warning)) {
                    _report.Warnings.Add(warning);
                }
            }

            foreach (string error in capture.Errors) {
                if (!_report.Errors.Contains(error)) {
                    _report.Errors.Add(error);
                }
            }
        }

        if (!hasCaptured) {
            AddError("No screenshots were captured.");
        }

        if (_report.Errors.Count > 0) {
            _report.Status = "FAILED";
        } else if (_report.Warnings.Count > 0) {
            _report.Status = "WARNING";
        } else {
            _report.Status = "PASSED";
        }
    }

    private void WriteOutputs() {
        Directory.CreateDirectory(_outputRoot);
        File.WriteAllText(Path.Combine(_outputRoot, "report.json"), JsonUtility.ToJson(_report, true));
        File.WriteAllText(Path.Combine(_outputRoot, "ui_snapshot.json"), JsonUtility.ToJson(_uiSnapshot, true));
        File.WriteAllText(Path.Combine(_outputRoot, "registry_snapshot.json"), JsonUtility.ToJson(_registrySnapshot, true));
        File.WriteAllText(Path.Combine(_outputRoot, "notes.txt"), BuildNotesText());
    }

    private string BuildNotesText() {
        return "ArtAcceptance latest output is overwritten on each run.\n" +
               "Trigger: UnityClient/Logs/.art_acceptance_trigger = RUN_ART_ACCEPTANCE\n" +
               $"RunID: {_runID}\n" +
               $"Status: {_report.Status}\n";
    }

    private void AddWarning(string message) {
        if (!string.IsNullOrEmpty(message) && !_report.Warnings.Contains(message)) {
            _report.Warnings.Add(message);
        }
    }

    private void AddCaptureError(ArtAcceptanceCaptureRecord capture, string message) {
        if (capture == null || string.IsNullOrEmpty(message)) {
            return;
        }

        if (!capture.Errors.Contains(message)) {
            capture.Errors.Add(message);
        }
    }

    private void AddError(string message) {
        if (!string.IsNullOrEmpty(message) && !_report.Errors.Contains(message)) {
            _report.Errors.Add(message);
        }
    }

    private string BuildTransformPath(Transform transform) {
        if (transform == null) {
            return string.Empty;
        }

        List<string> parts = new List<string>();
        Transform current = transform;
        while (current != null) {
            parts.Add(current.name);
            current = current.parent;
        }

        parts.Reverse();
        return string.Join("/", parts.ToArray());
    }

    private string BuildRelativePath(Transform root, Transform target) {
        if (root == null || target == null) {
            return string.Empty;
        }

        List<string> parts = new List<string>();
        Transform current = target;
        while (current != null && current != root.parent) {
            parts.Add(current.name);
            if (current == root) {
                break;
            }
            current = current.parent;
        }

        parts.Reverse();
        return string.Join("/", parts.ToArray());
    }

    private string FormatVector2(Vector2 value) {
        return $"{value.x:0.##},{value.y:0.##}";
    }

    private string FormatVector4(Vector4 value) {
        return $"{value.x:0.##},{value.y:0.##},{value.z:0.##},{value.w:0.##}";
    }

    private string FormatRect(Rect value) {
        return $"{value.x:0.##},{value.y:0.##},{value.width:0.##},{value.height:0.##}";
    }
}
