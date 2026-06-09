#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

[InitializeOnLoad]
public static class ArtAcceptanceEditorDaemon {
    private const string TriggerCommand = "RUN_ART_ACCEPTANCE";
    private const string DoneMarker = "DONE";
    private const string PendingCommandKey = "ArtAcceptance_PendingCommand";
    private const string AutoExitKey = "ArtAcceptance_AutoExitPlayMode";
    private const string AcceptanceScenePath = "Assets/Scenes/SampleScene.unity";

    private static readonly string LogsDir;
    private static readonly string TriggerFile;
    private static FileSystemWatcher _watcher;
    private static bool _needsRefresh;
    private static double _nextPollTime;
    private static double _playModeEnteredAt = -1d;
    private static double _nextPendingDiagnosticTime;

    static ArtAcceptanceEditorDaemon() {
        LogsDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs"));
        if (!Directory.Exists(LogsDir)) {
            Directory.CreateDirectory(LogsDir);
        }

        TriggerFile = Path.Combine(LogsDir, ".art_acceptance_trigger");
        if (!File.Exists(TriggerFile)) {
            File.WriteAllText(TriggerFile, string.Empty);
        }

        _watcher = new FileSystemWatcher(LogsDir, ".art_acceptance_trigger");
        _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
        _watcher.Changed += OnTriggerFileChanged;
        _watcher.EnableRaisingEvents = true;

        EditorApplication.update += OnEditorUpdate;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        _needsRefresh = true;
        if (EditorApplication.isPlaying) {
            _playModeEnteredAt = EditorApplication.timeSinceStartup;
        }
    }

    [MenuItem("Tools/P3 Art/Run Automated Art Acceptance")]
    public static void RunFromMenu() {
        QueueRun(autoExitPlayMode: false);
    }

    public static void RunFromBatchmode() {
        EnsureAcceptanceSceneOpen();
        QueueRun(autoExitPlayMode: true);
    }

    private static void EnsureAcceptanceSceneOpen() {
        if (!Application.isBatchMode) {
            return;
        }

        if (!File.Exists(AcceptanceScenePath)) {
            Debug.LogWarning($"[ArtAcceptanceEditorDaemon] Acceptance scene not found: {AcceptanceScenePath}");
            return;
        }

        EditorSceneManager.OpenScene(AcceptanceScenePath, OpenSceneMode.Single);
    }

    private static void OnTriggerFileChanged(object source, FileSystemEventArgs e) {
        _needsRefresh = true;
    }

    private static void OnEditorUpdate() {
        if (EditorApplication.isPlaying) {
            ArtAcceptanceRunner.EditorTickActiveRun();
        }

        if (!_needsRefresh && EditorApplication.timeSinceStartup < _nextPollTime) {
            DrivePendingRun();
            return;
        }

        _nextPollTime = EditorApplication.timeSinceStartup + 1.0d;
        _needsRefresh = false;
        string content;
        try {
            content = File.ReadAllText(TriggerFile).Trim();
        } catch {
            return;
        }

        if (string.IsNullOrEmpty(content) || content == DoneMarker) {
            DrivePendingRun();
            return;
        }

        if (content.ToUpperInvariant() != TriggerCommand) {
            Debug.LogWarning($"[ArtAcceptanceEditorDaemon] Unknown command [{content}]. Expected [{TriggerCommand}].");
            TryWriteTrigger(DoneMarker);
            return;
        }

        Debug.Log($"[ArtAcceptanceEditorDaemon] Received command: {content}");
        TryWriteTrigger(DoneMarker);
        QueueRun(autoExitPlayMode: true);
    }

    private static void QueueRun(bool autoExitPlayMode) {
        EditorPrefs.SetString(PendingCommandKey, TriggerCommand);
        EditorPrefs.SetBool(AutoExitKey, autoExitPlayMode);

        if (EditorApplication.isPlaying) {
            Debug.Log("[ArtAcceptanceEditorDaemon] Leaving current PlayMode to start art acceptance from a clean runtime state.");
            EditorApplication.isPlaying = false;
            return;
        }

        RefreshAssetDatabaseForPendingRun();
        EditorApplication.delayCall += DrivePendingRun;
    }

    private static void EnterPlayModeForPendingRun() {
        if (string.IsNullOrEmpty(EditorPrefs.GetString(PendingCommandKey, string.Empty))) {
            return;
        }

        Debug.Log("[ArtAcceptanceEditorDaemon] Entering PlayMode for automated art acceptance.");
        EditorApplication.isPlaying = true;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange change) {
        if (change == PlayModeStateChange.EnteredPlayMode) {
            _playModeEnteredAt = EditorApplication.timeSinceStartup;
        }

        if (change == PlayModeStateChange.EnteredEditMode) {
            _playModeEnteredAt = -1d;
            EditorApplication.delayCall += () => {
                RefreshAssetDatabaseForPendingRun();
                DrivePendingRun();
            };
        }
    }

    private static void RefreshAssetDatabaseForPendingRun() {
        if (string.IsNullOrEmpty(EditorPrefs.GetString(PendingCommandKey, string.Empty))) {
            return;
        }

        try {
            AssetDatabase.Refresh();
        } catch (System.Exception ex) {
            Debug.LogWarning($"[ArtAcceptanceEditorDaemon] AssetDatabase.Refresh failed before art acceptance run: {ex.Message}");
        }
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded() {
        if (!EditorApplication.isPlaying && !string.IsNullOrEmpty(EditorPrefs.GetString(PendingCommandKey, string.Empty))) {
            Debug.Log("[ArtAcceptanceEditorDaemon] Scripts reloaded with pending art acceptance command.");
            EditorApplication.delayCall += DrivePendingRun;
        }
    }

    private static void DrivePendingRun() {
        if (EditorPrefs.GetString(PendingCommandKey, string.Empty) != TriggerCommand) {
            return;
        }

        if (EditorApplication.isPlaying) {
            TryStartPendingRunIfReady();
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) {
            LogPendingDiagnostic("[ArtAcceptanceEditorDaemon] Pending art acceptance is waiting for compilation or PlayMode transition.");
            return;
        }

        EnterPlayModeForPendingRun();
    }

    private static void LogPendingDiagnostic(string message) {
        if (EditorApplication.timeSinceStartup < _nextPendingDiagnosticTime) {
            return;
        }

        _nextPendingDiagnosticTime = EditorApplication.timeSinceStartup + 3d;
        Debug.Log(message);
    }

    private static void TryStartPendingRunIfReady() {
        if (!EditorApplication.isPlaying) {
            return;
        }

        if (EditorPrefs.GetString(PendingCommandKey, string.Empty) != TriggerCommand) {
            return;
        }

        if (_playModeEnteredAt < 0d) {
            _playModeEnteredAt = EditorApplication.timeSinceStartup;
        }

        double elapsed = EditorApplication.timeSinceStartup - _playModeEnteredAt;
        bool runtimeReady = GameRoot.IsCoreReady() && GameFlowController.Instance != null;
        if (elapsed < 2.5d || (!runtimeReady && elapsed < 20d)) {
            return;
        }

        bool autoExit = EditorPrefs.GetBool(AutoExitKey, true);
        EditorPrefs.DeleteKey(PendingCommandKey);
        EditorPrefs.DeleteKey(AutoExitKey);

        Debug.Log($"[ArtAcceptanceEditorDaemon] Starting runtime ArtAcceptanceRunner. RuntimeReady={runtimeReady}, Elapsed={elapsed:0.0}s");
        ArtAcceptanceRunner.BeginAutomatedRun(autoExit);
    }

    private static void TryWriteTrigger(string content) {
        try {
            File.WriteAllText(TriggerFile, content);
        } catch {
            // Best effort: the next file write will retrigger the watcher.
        }
    }
}
#endif
