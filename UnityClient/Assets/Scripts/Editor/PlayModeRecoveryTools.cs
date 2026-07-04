#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayModeRecoveryTools
{
    private const string MainScenePath = "Assets/Scenes/SampleScene.unity";
    private static bool _pendingRecoveryAfterExitPlayMode;

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

    public static void ReportRuntimeState()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        string scenePath = string.IsNullOrEmpty(activeScene.path) ? "<no-scene-path>" : activeScene.path;
        GameObject gameManager = GameObject.Find("GameManager");
        GameFlowController foundFlow = Object.FindObjectOfType<GameFlowController>(true);
        PrologueFirstDiveController prologue = Object.FindObjectOfType<PrologueFirstDiveController>(true);
        P3DialogueOverlayController overlay = Object.FindObjectOfType<P3DialogueOverlayController>(true);
        string gameManagerState = gameManager == null
            ? "missing"
            : $"activeSelf={gameManager.activeSelf}, activeInHierarchy={gameManager.activeInHierarchy}";
        string foundFlowState = foundFlow == null
            ? "missing"
            : $"componentEnabled={foundFlow.enabled}, activeInHierarchy={foundFlow.gameObject.activeInHierarchy}, object={foundFlow.gameObject.name}";
        string prologueState = prologue == null
            ? "missing"
            : $"componentEnabled={prologue.enabled}, activeInHierarchy={prologue.gameObject.activeInHierarchy}, stage={prologue.LastAppliedReentryStage}";
        string overlayState = overlay == null
            ? "missing"
            : $"isShowing={overlay.IsShowing}, blocks={overlay.IsInputBlocking}, actionVisible={overlay.HasActionVisible}, visual={overlay.LastVisualID}, stage=[{overlay.StageDebugSummary}], speaker={overlay.CurrentSpeaker}, text={overlay.CurrentText}";
        Debug.Log(
            $"[PlayModeRecoveryTools] RuntimeState isPlaying={EditorApplication.isPlaying}, " +
            $"isPlayingOrWillChangePlaymode={EditorApplication.isPlayingOrWillChangePlaymode}, " +
            $"isPaused={EditorApplication.isPaused}, timeScale={Time.timeScale}, frame={Time.frameCount}, " +
            $"scene={activeScene.name}, path={scenePath}, " +
            $"coreReady={GameRoot.IsCoreReady()}, gameFlowPresent={GameFlowController.Instance != null}, " +
            $"gameManager={gameManagerState}, foundFlow={foundFlowState}, " +
            $"prologue={prologueState}, overlay={overlayState}");
    }

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

    private static void RecoverMainSceneInEditMode()
    {
        Debug.Log("[PlayModeRecoveryTools] Recovering main scene and entering PlayMode.");
        EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        EditorApplication.delayCall -= EnterPlayModeOnDelay;
        EditorApplication.delayCall += EnterPlayModeOnDelay;
    }
}
#endif
