#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayModeRecoveryTools
{
    private const string MainScenePath = "Assets/Scenes/SampleScene.unity";
    private const string T0FinalCapturePendingKey = "P3_T0FinalCapturePending";
    private static bool _pendingRecoveryAfterExitPlayMode;

    [MenuItem("Tools/P3/T0 Validation/Recover Main Scene And Enter PlayMode")]
    public static void RecoverMainSceneAndEnterPlayMode()
    {
        if (EditorApplication.isPlaying)
        {
            Debug.Log("[PlayModeRecoveryTools] PlayMode is active. Leaving PlayMode before recovering the main scene.");
            _pendingRecoveryAfterExitPlayMode = true;
            EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
            EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
            EditorApplication.isPlaying = false;
            return;
        }

        RecoverMainSceneInEditMode();
    }

    [MenuItem("Tools/P3/T0 Validation/Report Runtime State")]
    public static void ReportRuntimeState()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        string scenePath = string.IsNullOrEmpty(activeScene.path) ? "<no-scene-path>" : activeScene.path;
        GameObject gameManager = GameObject.Find("GameManager");
        GameFlowController foundFlow = Object.FindObjectOfType<GameFlowController>(true);
        PrologueFirstDiveController prologue = Object.FindObjectOfType<PrologueFirstDiveController>(true);
        P3DialogueOverlayController overlay = Object.FindObjectOfType<P3DialogueOverlayController>(true);
        DungeonStartLayerUIController layerPanel = Object.FindObjectOfType<DungeonStartLayerUIController>(true);
        string gameManagerState = gameManager == null
            ? "missing"
            : $"activeSelf={gameManager.activeSelf}, activeInHierarchy={gameManager.activeInHierarchy}";
        string foundFlowState = foundFlow == null
            ? "missing"
            : $"componentEnabled={foundFlow.enabled}, activeInHierarchy={foundFlow.gameObject.activeInHierarchy}, object={foundFlow.gameObject.name}";
        string prologueState = prologue == null
            ? "missing"
            : $"componentEnabled={prologue.enabled}, activeInHierarchy={prologue.gameObject.activeInHierarchy}, stage={prologue.LastAppliedReentryStage}, timeline=[{prologue.TimelineDebugSummary}]";
        string overlayState = overlay == null
            ? "missing"
            : $"isShowing={overlay.IsShowing}, blocks={overlay.IsInputBlocking}, actionVisible={overlay.HasActionVisible}, visual={overlay.LastVisualID}, stage=[{overlay.StageDebugSummary}], speaker={overlay.CurrentSpeaker}, text={overlay.CurrentText}";
        string layerPanelState = layerPanel == null
            ? "missing"
            : $"active={layerPanel.gameObject.activeInHierarchy}, firstDive={layerPanel.IsFirstDiveMode}, layer={layerPanel.SelectedLayerID}, confirmVisible={layerPanel.confirmBtn != null && layerPanel.confirmBtn.gameObject.activeInHierarchy}, confirmInteractable={layerPanel.confirmBtn != null && layerPanel.confirmBtn.interactable}, departCallback={layerPanel.HasFirstDiveDepartCallback}";
        Debug.Log(
            $"[PlayModeRecoveryTools] RuntimeState isPlaying={EditorApplication.isPlaying}, " +
            $"isPlayingOrWillChangePlaymode={EditorApplication.isPlayingOrWillChangePlaymode}, " +
            $"isPaused={EditorApplication.isPaused}, timeScale={Time.timeScale}, frame={Time.frameCount}, " +
            $"scene={activeScene.name}, path={scenePath}, " +
            $"coreReady={GameRoot.IsCoreReady()}, gameFlowPresent={GameFlowController.Instance != null}, " +
            $"gameManager={gameManagerState}, foundFlow={foundFlowState}, " +
            $"prologue={prologueState}, overlay={overlayState}, layerPanel={layerPanelState}");
    }

    [MenuItem("Tools/P3/T0 Validation/Enter PlayMode Now")]
    public static void EnterPlayModeNow()
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            Debug.Log("[PlayModeRecoveryTools] EnterPlayModeNow skipped because PlayMode is already active or changing.");
            return;
        }

        Debug.Log("[PlayModeRecoveryTools] EnterPlayModeNow entering PlayMode.");
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/P3/T0 Validation/Capture Final Screenshot Set")]
    public static void CaptureFinalScreenshotSet()
    {
        SessionState.SetBool(T0FinalCapturePendingKey, false);
        if (EditorApplication.isPlaying)
        {
            T0ValidationFinalCaptureRunner.StartCapture();
            return;
        }

        Debug.Log("[PlayModeRecoveryTools] Recovering main scene for T0 final screenshot capture.");
        EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        CreateT0FinalCaptureBootstrap();
        EditorApplication.isPlaying = true;
    }

    [MenuItem("Tools/P3/T0 Validation/Exit PlayMode Now")]
    public static void ExitPlayModeNow()
    {
        if (!EditorApplication.isPlaying)
        {
            Debug.Log("[PlayModeRecoveryTools] ExitPlayModeNow skipped because PlayMode is not active.");
            return;
        }

        Debug.Log("[PlayModeRecoveryTools] ExitPlayModeNow leaving PlayMode.");
        EditorApplication.isPlaying = false;
    }

    [MenuItem("Tools/P3/T0 Validation/Force Prologue Startup Now")]
    public static void ForcePrologueStartupNow()
    {
        GameFlowController flow = Object.FindObjectOfType<GameFlowController>(true);
        if (flow == null)
        {
            Debug.LogError("[PlayModeRecoveryTools] ForcePrologueStartupNow failed: GameFlowController missing.");
            return;
        }

        flow.EnsurePrologueOpeningForRuntime();
        Debug.Log("[PlayModeRecoveryTools] ForcePrologueStartupNow invoked GameFlowController.EnsurePrologueOpeningForRuntime.");
    }

    [MenuItem("Tools/P3/T0 Validation/Click Visible Overlay Action")]
    public static void ClickVisibleOverlayAction()
    {
        P3DialogueOverlayController overlay = Object.FindObjectOfType<P3DialogueOverlayController>(true);
        if (overlay == null || overlay.actionButton == null || !overlay.actionButton.gameObject.activeInHierarchy)
        {
            Debug.LogError("[PlayModeRecoveryTools] ClickVisibleOverlayAction failed: visible action button missing.");
            return;
        }

        Debug.Log($"[PlayModeRecoveryTools] Clicking overlay action. visual={overlay.LastVisualID}, text={overlay.CurrentText}");
        overlay.actionButton.onClick.Invoke();
    }

    [MenuItem("Tools/P3/T0 Validation/Click Workshop Shallow Gate")]
    public static void ClickWorkshopShallowGate()
    {
        WorkshopUIController workshop = Object.FindObjectOfType<WorkshopUIController>(true);
        if (workshop == null || workshop.departBtn == null || !workshop.departBtn.gameObject.activeInHierarchy)
        {
            Debug.LogError("[PlayModeRecoveryTools] ClickWorkshopShallowGate failed: shallow gate button missing.");
            return;
        }

        Debug.Log($"[PlayModeRecoveryTools] Clicking shallow gate. available={workshop.IsPrologueShallowGateAvailable}");
        workshop.departBtn.onClick.Invoke();
    }

    [MenuItem("Tools/P3/T0 Validation/Click First Dive Confirm")]
    public static void ClickFirstDiveConfirm()
    {
        DungeonStartLayerUIController panel = Object.FindObjectOfType<DungeonStartLayerUIController>(true);
        if (panel == null || panel.confirmBtn == null || !panel.confirmBtn.gameObject.activeInHierarchy)
        {
            Debug.LogError("[PlayModeRecoveryTools] ClickFirstDiveConfirm failed: confirm button missing.");
            return;
        }

        Debug.Log($"[PlayModeRecoveryTools] Clicking first dive confirm. firstDive={panel.IsFirstDiveMode}, layer={panel.SelectedLayerID}, interactable={panel.confirmBtn.interactable}");
        panel.confirmBtn.onClick.Invoke();
    }

    [MenuItem("Tools/P3/T0 Validation/Click First Dive Close")]
    public static void ClickFirstDiveClose()
    {
        DungeonStartLayerUIController panel = Object.FindObjectOfType<DungeonStartLayerUIController>(true);
        if (panel == null || panel.closeBtn == null || !panel.closeBtn.gameObject.activeInHierarchy)
        {
            Debug.LogError("[PlayModeRecoveryTools] ClickFirstDiveClose failed: close button missing.");
            return;
        }

        Debug.Log("[PlayModeRecoveryTools] Clicking first dive close.");
        panel.closeBtn.onClick.Invoke();
    }

    [MenuItem("Tools/P3/T0 Validation/Set First Doll Extreme Wear")]
    public static void SetFirstDollExtremeWear()
    {
        DollEntity doll = GameRoot.Core?.CurrentPlayer?.ActiveDoll;
        if (doll == null)
        {
            Debug.LogError("[PlayModeRecoveryTools] SetFirstDollExtremeWear failed: active doll missing.");
            return;
        }

        doll.Status.WearAndTear = DiveReadinessService.ExtremeWearThreshold;
        Debug.Log($"[PlayModeRecoveryTools] Active doll wear set to extreme threshold {DiveReadinessService.ExtremeWearThreshold}.");
    }

    [MenuItem("Tools/P3/T0 Validation/Reset First Doll Wear")]
    public static void ResetFirstDollWear()
    {
        DollEntity doll = GameRoot.Core?.CurrentPlayer?.ActiveDoll;
        if (doll == null)
        {
            Debug.LogError("[PlayModeRecoveryTools] ResetFirstDollWear failed: active doll missing.");
            return;
        }

        doll.Status.WearAndTear = 0f;
        Debug.Log("[PlayModeRecoveryTools] Active doll wear reset to 0.");
    }

    private static void EnterPlayModeOnDelay()
    {
        EditorApplication.delayCall -= EnterPlayModeOnDelay;
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Debug.Log("[PlayModeRecoveryTools] Entering PlayMode from recovered main scene.");
        EditorApplication.isPlaying = true;
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (!_pendingRecoveryAfterExitPlayMode || state != PlayModeStateChange.EnteredEditMode)
        {
            return;
        }

        _pendingRecoveryAfterExitPlayMode = false;
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        RecoverMainSceneInEditMode();
    }

    private static void HandleT0FinalCapturePlayModeStateChanged(PlayModeStateChange state)
    {
        if (!SessionState.GetBool(T0FinalCapturePendingKey, false) || state != PlayModeStateChange.EnteredPlayMode)
        {
            return;
        }

        SessionState.SetBool(T0FinalCapturePendingKey, false);
        EditorApplication.playModeStateChanged -= HandleT0FinalCapturePlayModeStateChanged;
        T0ValidationFinalCaptureRunner.StartCapture();
    }

    [InitializeOnLoadMethod]
    private static void InitializeT0FinalCaptureRecovery()
    {
        EditorApplication.playModeStateChanged -= HandleT0FinalCapturePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandleT0FinalCapturePlayModeStateChanged;
        if (EditorApplication.isPlaying && SessionState.GetBool(T0FinalCapturePendingKey, false))
        {
            EditorApplication.delayCall -= StartPendingT0FinalCapture;
            EditorApplication.delayCall += StartPendingT0FinalCapture;
        }
    }

    private static void StartPendingT0FinalCapture()
    {
        if (!EditorApplication.isPlaying || !SessionState.GetBool(T0FinalCapturePendingKey, false))
        {
            return;
        }

        SessionState.SetBool(T0FinalCapturePendingKey, false);
        T0ValidationFinalCaptureRunner.StartCapture();
    }

    private static void CreateT0FinalCaptureBootstrap()
    {
        GameObject existing = GameObject.Find("T0ValidationFinalCaptureRunner_Bootstrap");
        if (existing != null)
        {
            Object.DestroyImmediate(existing);
        }

        GameObject runnerObject = new GameObject("T0ValidationFinalCaptureRunner_Bootstrap");
        runnerObject.AddComponent<T0ValidationFinalCaptureRunner>();
    }

    private static void RecoverMainSceneInEditMode()
    {
        Debug.Log("[PlayModeRecoveryTools] Recovering main scene and entering PlayMode.");
        EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        EditorApplication.delayCall -= EnterPlayModeOnDelay;
        EditorApplication.delayCall += EnterPlayModeOnDelay;
    }
}
#endif
