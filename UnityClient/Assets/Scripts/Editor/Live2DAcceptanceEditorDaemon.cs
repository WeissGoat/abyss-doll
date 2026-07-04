#if UNITY_EDITOR
using System.IO;
using UnityEditor;
using UnityEngine;

[InitializeOnLoad]
public static class Live2DAcceptanceEditorDaemon {
    private const string DoneMarker = "DONE";
    private const string PendingCommandKey = "Live2DAcceptance_PendingCommand";
    private const string AutoExitKey = "Live2DAcceptance_AutoExitPlayMode";

    private static readonly string LogsDir;
    private static readonly string TriggerFile;
    private static FileSystemWatcher watcher;
    private static bool needsRefresh;
    private static double nextPollTime;
    private static double playModeEnteredAt = -1d;
    private static double nextPendingDiagnosticTime;

    static Live2DAcceptanceEditorDaemon() {
        LogsDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs"));
        if (!Directory.Exists(LogsDir)) {
            Directory.CreateDirectory(LogsDir);
        }

        TriggerFile = Path.Combine(LogsDir, ".live2d_acceptance_trigger");
        if (!File.Exists(TriggerFile)) {
            File.WriteAllText(TriggerFile, string.Empty);
        }

        watcher = new FileSystemWatcher(LogsDir, ".live2d_acceptance_trigger");
        watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
        watcher.Changed += OnTriggerFileChanged;
        watcher.EnableRaisingEvents = true;

        EditorApplication.update += OnEditorUpdate;
        EditorApplication.playModeStateChanged += OnPlayModeStateChanged;

        needsRefresh = true;
        if (EditorApplication.isPlaying) {
            playModeEnteredAt = EditorApplication.timeSinceStartup;
        }
    }

    [MenuItem("Tools/P3 Art/Run Live2D Acceptance")]
    public static void RunFromMenu() {
        QueueRun(autoExitPlayMode: false);
    }

    public static void RunFromBatchmode() {
        QueueRun(autoExitPlayMode: true);
    }

    private static void OnTriggerFileChanged(object source, FileSystemEventArgs e) {
        needsRefresh = true;
    }

    private static void OnEditorUpdate() {
        if (!needsRefresh && EditorApplication.timeSinceStartup < nextPollTime) {
            DrivePendingRun();
            return;
        }

        nextPollTime = EditorApplication.timeSinceStartup + 1.0d;
        needsRefresh = false;

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

        if (content.ToUpperInvariant() != Live2DAcceptanceRunner.TriggerCommand) {
            Debug.LogWarning($"[Live2DAcceptanceEditorDaemon] Unknown command [{content}]. Expected [{Live2DAcceptanceRunner.TriggerCommand}].");
            TryWriteTrigger(DoneMarker);
            return;
        }

        Debug.Log($"[Live2DAcceptanceEditorDaemon] Received command: {content}");
        TryWriteTrigger(DoneMarker);
        QueueRun(autoExitPlayMode: true);
    }

    private static void QueueRun(bool autoExitPlayMode) {
        EditorPrefs.SetString(PendingCommandKey, Live2DAcceptanceRunner.TriggerCommand);
        EditorPrefs.SetBool(AutoExitKey, autoExitPlayMode);

        if (EditorApplication.isPlaying) {
            Debug.Log("[Live2DAcceptanceEditorDaemon] Leaving current PlayMode to start Live2D acceptance from a clean state.");
            EditorApplication.isPlaying = false;
            return;
        }

        RefreshAssetDatabaseForPendingRun();
        EditorApplication.delayCall += DrivePendingRun;
    }

    private static void OnPlayModeStateChanged(PlayModeStateChange change) {
        if (change == PlayModeStateChange.EnteredPlayMode) {
            playModeEnteredAt = EditorApplication.timeSinceStartup;
        }

        if (change == PlayModeStateChange.EnteredEditMode) {
            playModeEnteredAt = -1d;
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
            Debug.LogWarning($"[Live2DAcceptanceEditorDaemon] AssetDatabase.Refresh failed before run: {ex.Message}");
        }
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded() {
        if (!EditorApplication.isPlaying && !string.IsNullOrEmpty(EditorPrefs.GetString(PendingCommandKey, string.Empty))) {
            Debug.Log("[Live2DAcceptanceEditorDaemon] Scripts reloaded with pending command.");
            EditorApplication.delayCall += DrivePendingRun;
        }
    }

    private static void DrivePendingRun() {
        if (EditorPrefs.GetString(PendingCommandKey, string.Empty) != Live2DAcceptanceRunner.TriggerCommand) {
            return;
        }

        if (EditorApplication.isPlaying) {
            TryStartPendingRunIfReady();
            return;
        }

        if (EditorApplication.isCompiling || EditorApplication.isPlayingOrWillChangePlaymode) {
            LogPendingDiagnostic("[Live2DAcceptanceEditorDaemon] Pending run is waiting for compilation or PlayMode transition.");
            return;
        }

        Debug.Log("[Live2DAcceptanceEditorDaemon] Entering PlayMode for Live2D acceptance.");
        EditorApplication.isPlaying = true;
    }

    private static void TryStartPendingRunIfReady() {
        if (!EditorApplication.isPlaying) {
            return;
        }

        if (EditorPrefs.GetString(PendingCommandKey, string.Empty) != Live2DAcceptanceRunner.TriggerCommand) {
            return;
        }

        if (playModeEnteredAt < 0d) {
            playModeEnteredAt = EditorApplication.timeSinceStartup;
        }

        double elapsed = EditorApplication.timeSinceStartup - playModeEnteredAt;
        if (elapsed < 1.0d) {
            return;
        }

        bool autoExit = EditorPrefs.GetBool(AutoExitKey, true);
        EditorPrefs.DeleteKey(PendingCommandKey);
        EditorPrefs.DeleteKey(AutoExitKey);

        Debug.Log($"[Live2DAcceptanceEditorDaemon] Starting runtime runner. Elapsed={elapsed:0.0}s");
        Live2DAcceptanceRunner.BeginAutomatedRun(autoExit);
    }

    private static void LogPendingDiagnostic(string message) {
        if (EditorApplication.timeSinceStartup < nextPendingDiagnosticTime) {
            return;
        }

        nextPendingDiagnosticTime = EditorApplication.timeSinceStartup + 3d;
        Debug.Log(message);
    }

    private static void TryWriteTrigger(string content) {
        try {
            File.WriteAllText(TriggerFile, content);
        } catch {
            // Best effort: a future write will retrigger the watcher.
        }
    }
}
#endif
