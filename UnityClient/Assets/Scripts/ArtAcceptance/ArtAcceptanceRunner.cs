using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.UI;

#if UNITY_EDITOR
using UnityEditor;
#endif

/// <summary>
/// 美术自动验收流程控制核心。
/// 负责：入口触发、手动协程栈驱动、流程编排、报告初始化/输出、全局环境检查和基础 helpers。
/// 
/// 具体截图步骤见 ArtAcceptanceCaptureSteps.cs (partial class)
/// 截图渲染逻辑见 ArtAcceptanceScreenCapture.cs (partial class)
/// 验收 payload 构造见 ArtAcceptancePayloadFactory.cs (partial class)
/// UI 层级扫描见 ArtAcceptanceUiScanner.cs (partial class)
/// </summary>
public partial class ArtAcceptanceRunner : MonoBehaviour {
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

    // ──────────────────────────────────────────
    // 入口与生命周期
    // ──────────────────────────────────────────

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

    // ──────────────────────────────────────────
    // 手动协程栈驱动器
    // ──────────────────────────────────────────

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

    // ──────────────────────────────────────────
    // 主流程编排
    // ──────────────────────────────────────────

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
        yield return RunStep("CaptureMaintenancePanel", CaptureMaintenancePanel);
        LogFlowCheckpoint("CaptureMaintenancePanel");
        yield return RunStep("CaptureDailyBillReport", CaptureDailyBillReport);
        LogFlowCheckpoint("CaptureDailyBillReport");
        yield return RunStep("CaptureShopStaging", CaptureShopStaging);
        LogFlowCheckpoint("CaptureShopStaging");
        yield return RunStep("CaptureOrderBoard", CaptureOrderBoard);
        LogFlowCheckpoint("CaptureOrderBoard");
        yield return RunStep("CaptureRumorBoard", CaptureRumorBoard);
        LogFlowCheckpoint("CaptureRumorBoard");
        yield return RunStep("CaptureBusinessSettlement", CaptureBusinessSettlement);
        LogFlowCheckpoint("CaptureBusinessSettlement");
        yield return RunStep("CaptureChassisUpgradePanel", CaptureChassisUpgradePanel);
        LogFlowCheckpoint("CaptureChassisUpgradePanel");
        yield return RunStep("CaptureDollInteraction", CaptureDollInteraction);
        LogFlowCheckpoint("CaptureDollInteraction");
        yield return RunStep("CaptureDollRoom", CaptureDollRoom);
        LogFlowCheckpoint("CaptureDollRoom");
        yield return RunStep("CaptureFactionShop", CaptureFactionShop);
        LogFlowCheckpoint("CaptureFactionShop");
        yield return RunStep("CaptureScenarioEvent", CaptureScenarioEvent);
        LogFlowCheckpoint("CaptureScenarioEvent");
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

    // ──────────────────────────────────────────
    // 流程辅助
    // ──────────────────────────────────────────

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

    // ──────────────────────────────────────────
    // 初始化与运行时就绪检查
    // ──────────────────────────────────────────

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

    // ──────────────────────────────────────────
    // BeginCapture (带 DataSource 标记)
    // ──────────────────────────────────────────

    private ArtAcceptanceCaptureRecord BeginCapture(string screenTag, string relativeFile, string dataSource = "real_gameplay") {
        ArtAcceptanceCaptureRecord capture = new ArtAcceptanceCaptureRecord {
            Index = _report.Captures.Count + 1,
            ScreenTag = screenTag,
            File = relativeFile,
            CapturedAt = DateTime.Now.ToString("o"),
            Status = "running",
            Resolution = $"{ReferenceWidth}x{ReferenceHeight}",
            DataSource = dataSource
        };
        _report.Captures.Add(capture);
        return capture;
    }

    // ──────────────────────────────────────────
    // 前置条件检查
    // ──────────────────────────────────────────

    private void CompleteSkipped(ArtAcceptanceCaptureRecord capture) {
        capture.Status = "skipped";
        capture.ActiveControllers = CollectActiveControllers();
        if (capture.Warnings.Count == 0 && capture.Errors.Count == 0) {
            capture.Warnings.Add("Capture skipped because required runtime state was unavailable.");
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

    // ──────────────────────────────────────────
    // Wait helpers
    // ──────────────────────────────────────────

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

    // ──────────────────────────────────────────
    // Registry snapshot & P0 VisualID 检查
    // ──────────────────────────────────────────

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
        List<string> visualIDs = new List<string> {
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
            VisualAssetService.UICombatEntityShadowID,
            VisualAssetService.UICombatTargetRingID,
            VisualAssetService.UICombatStatusBarHpID,
            VisualAssetService.UICombatStatusBarShieldID,
            VisualAssetService.UICombatApPipID,
            VisualAssetService.UICombatTurnBannerID,
            VisualAssetService.UIIconMoneyID,
            VisualAssetService.UIIconMaintenanceID,
            VisualAssetService.UIIconBillID,
            VisualAssetService.UIIconWarningID,
            VisualAssetService.UIIconShopChannelID,
            VisualAssetService.UIIconBlackMarketID,
            VisualAssetService.UIIconOrderID,
            VisualAssetService.UIIconFactionID,
            VisualAssetService.UIIconDeadlineID,
            VisualAssetService.UIIconRumorID,
            VisualAssetService.UIIconPriceUpID,
            VisualAssetService.UIIconPriceDownID,
            VisualAssetService.UIIconIncomeID,
            VisualAssetService.UIIconExpenseID,
            VisualAssetService.UIIconDebtRentID,
            VisualAssetService.UIIconWearRepairID,
            VisualAssetService.UIIconCorruptionPurifyID,
            VisualAssetService.UIIconDivePermitID,
            VisualAssetService.UIIconBusinessSettlementID,
            VisualAssetService.UIIconCustomerID,
            VisualAssetService.UIIconSaleSparkID,
            VisualAssetService.UIIconChassisUpgradeID,
            VisualAssetService.UIIconBlueprintID,
            VisualAssetService.UIIconMaterialNeedID,
            VisualAssetService.UIIconTouchID,
            VisualAssetService.UIIconTalkID,
            VisualAssetService.UIIconGiftID,
            VisualAssetService.UIIconMementoID,
            VisualAssetService.UIIconDiaryID,
            VisualAssetService.UIRoomMementoSlotID,
            VisualAssetService.DollRoomAtticBackgroundID,
            VisualAssetService.UIIconReputationID,
            VisualAssetService.UIIconTrustID,
            VisualAssetService.UIIconEventID,
            VisualAssetService.UIIconLoreID,
            VisualAssetService.UIIconSkipID,
            VisualAssetService.UISettlementOutcomeVictoryID,
            VisualAssetService.UISettlementOutcomeHpDefeatID,
            VisualAssetService.UISettlementOutcomeSanCollapseID,
            VisualAssetService.UISettlementOutcomeHpSanDefeatID,
            VisualAssetService.UISettlementOutcomePartyWipeID,
            VisualAssetService.SafeRoomBackgroundID,
            VisualAssetService.StairsRoomBackgroundID,
            VisualAssetService.LayerSelectBackgroundID,
            VisualAssetService.SettlementVictoryBackgroundID,
            VisualAssetService.SettlementDefeatBackgroundID,
            VisualAssetService.UIPanelMainID,
            VisualAssetService.UIListRowNormalID,
            VisualAssetService.UIListRowSelectedID,
            VisualAssetService.UISettlementVictoryPanelID,
            VisualAssetService.UISettlementDefeatPanelID,
            VisualAssetService.UIDungeonNodePlateID,
            VisualAssetService.UIDungeonRouteLineID,
            VisualAssetService.UIIconLockedID,
            VisualAssetService.UIIconEquippedID,
            VisualAssetService.UITitleDividerID
        };

        if (ConfigManager.Monsters != null) {
            foreach (var kvp in ConfigManager.Monsters) {
                MonsterEntity monster = kvp.Value;
                if (monster == null) {
                    continue;
                }

                string combatVisualID = VisualAssetService.ResolveMonsterCombatVisualID(monster);
                if (!string.IsNullOrEmpty(combatVisualID) && !visualIDs.Contains(combatVisualID)) {
                    visualIDs.Add(combatVisualID);
                }
            }
        }

        return visualIDs.ToArray();
    }

    // ──────────────────────────────────────────
    // 报告构建与输出
    // ──────────────────────────────────────────

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
        int realGameplay = 0;
        int acceptancePreview = 0;
        int formalV1Template = 0;

        foreach (ArtAcceptanceCaptureRecord capture in _report.Captures) {
            if (capture.Status == "captured") {
                hasCaptured = true;
            }

            // DataSource 统计
            if (capture.DataSource == "real_gameplay") {
                realGameplay++;
            } else if (capture.DataSource == "acceptance_preview") {
                acceptancePreview++;
            } else if (capture.DataSource == "formal_v1_template") {
                formalV1Template++;
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

        _report.DataSourceSummary = new ArtAcceptanceDataSourceSummary {
            RealGameplay = realGameplay,
            AcceptancePreview = acceptancePreview,
            FormalV1Template = formalV1Template,
            Total = _report.Captures.Count
        };

        // 查找上一次 RunID
        _report.PreviousRunID = ResolvePreviousRunID();

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

        // 自动生成验收清单
        try {
            string checklist = BuildAcceptanceChecklist();
            File.WriteAllText(Path.Combine(_outputRoot, "acceptance_checklist.md"), checklist);
            Debug.Log("[ArtAcceptance] acceptance_checklist.md written.");
        } catch (Exception ex) {
            Debug.LogWarning($"[ArtAcceptance] Failed to write acceptance_checklist.md: {ex.Message}");
        }

        // History 留档：将 latest/ 完整拷贝到 history/<RunID>/
        try {
            ArchiveToHistory();
        } catch (Exception ex) {
            Debug.LogWarning($"[ArtAcceptance] Failed to archive to history: {ex.Message}");
        }
    }

    private string BuildNotesText() {
        return "ArtAcceptance latest output is overwritten on each run.\n" +
               "Trigger: UnityClient/Logs/.art_acceptance_trigger = RUN_ART_ACCEPTANCE\n" +
               $"RunID: {_runID}\n" +
               $"Status: {_report.Status}\n" +
               $"PreviousRunID: {(_report.PreviousRunID ?? "none")}\n";
    }

    // ──────────────────────────────────────────
    // History 留档
    // ──────────────────────────────────────────

    private void ArchiveToHistory() {
        string logsRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs"));
        string historyRoot = Path.Combine(logsRoot, "ArtAcceptance", "history", _runID);

        if (Directory.Exists(historyRoot)) {
            Directory.Delete(historyRoot, true);
        }

        CopyDirectoryRecursive(_outputRoot, historyRoot);
        Debug.Log($"[ArtAcceptance] Archived to history: {historyRoot}");
    }

    private void CopyDirectoryRecursive(string sourceDir, string destDir) {
        Directory.CreateDirectory(destDir);

        foreach (string file in Directory.GetFiles(sourceDir)) {
            string destFile = Path.Combine(destDir, Path.GetFileName(file));
            File.Copy(file, destFile, true);
        }

        foreach (string subDir in Directory.GetDirectories(sourceDir)) {
            string destSubDir = Path.Combine(destDir, Path.GetFileName(subDir));
            CopyDirectoryRecursive(subDir, destSubDir);
        }
    }

    private string ResolvePreviousRunID() {
        string logsRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs"));
        string historyRoot = Path.Combine(logsRoot, "ArtAcceptance", "history");

        if (!Directory.Exists(historyRoot)) {
            return null;
        }

        string[] dirs = Directory.GetDirectories(historyRoot);
        if (dirs.Length == 0) {
            return null;
        }

        // RunID 格式 yyyyMMdd_HHmmss，按字符串排序即可按时间排序
        System.Array.Sort(dirs, StringComparer.Ordinal);

        // 最新的历史 RunID (排除当前)
        for (int i = dirs.Length - 1; i >= 0; i--) {
            string dirName = Path.GetFileName(dirs[i]);
            if (dirName != _runID) {
                return dirName;
            }
        }

        return null;
    }

    // ──────────────────────────────────────────
    // 验收清单自动生成
    // ──────────────────────────────────────────

    private string BuildAcceptanceChecklist() {
        System.Text.StringBuilder sb = new System.Text.StringBuilder();
        sb.AppendLine("# 美术验收清单 (自动生成)");
        sb.AppendLine();
        sb.AppendLine($"> **RunID:** {_runID}");
        sb.AppendLine($"> **Status:** {_report.Status}");
        sb.AppendLine($"> **Time:** {_report.StartedAt} → {_report.FinishedAt}");
        if (!string.IsNullOrEmpty(_report.PreviousRunID)) {
            sb.AppendLine($"> **PreviousRunID:** {_report.PreviousRunID}");
        }
        sb.AppendLine();

        // DataSource 统计
        sb.AppendLine("## 数据来源统计");
        sb.AppendLine();
        sb.AppendLine($"| 类别 | 数量 |");
        sb.AppendLine($"|---|---|");
        sb.AppendLine($"| 真实游戏流 (real_gameplay) | {_report.DataSourceSummary.RealGameplay} |");
        sb.AppendLine($"| 验收构造数据 (acceptance_preview) | {_report.DataSourceSummary.AcceptancePreview} |");
        sb.AppendLine($"| V1 模板面板 (formal_v1_template) | {_report.DataSourceSummary.FormalV1Template} |");
        sb.AppendLine($"| **合计** | **{_report.DataSourceSummary.Total}** |");
        sb.AppendLine();

        // 逐截图点清单
        sb.AppendLine("## 截图点清单");
        sb.AppendLine();
        sb.AppendLine("| # | ScreenTag | Status | DataSource | Errors | Warnings | 美术验收结论 |");
        sb.AppendLine("|---|---|---|---|---|---|---|");

        foreach (ArtAcceptanceCaptureRecord capture in _report.Captures) {
            string statusIcon = capture.Status == "captured" ? "✅" : (capture.Status == "skipped" ? "⏭️" : "❌");
            sb.AppendLine($"| {capture.Index} | `{capture.ScreenTag}` | {statusIcon} {capture.Status} | {capture.DataSource} | {capture.Errors.Count} | {capture.Warnings.Count} | _待填写_ |");
        }

        sb.AppendLine();

        // 全局 Errors
        if (_report.Errors.Count > 0) {
            sb.AppendLine("## 全局 Errors");
            sb.AppendLine();
            foreach (string error in _report.Errors) {
                sb.AppendLine($"- ❌ {error}");
            }
            sb.AppendLine();
        }

        sb.AppendLine("---");
        sb.AppendLine("*本文件由 ArtAcceptanceRunner 自动生成。美术侧在「美术验收结论」列填写验收判定即可。*");

        return sb.ToString();
    }

    // ──────────────────────────────────────────
    // 通用 helpers
    // ──────────────────────────────────────────

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
