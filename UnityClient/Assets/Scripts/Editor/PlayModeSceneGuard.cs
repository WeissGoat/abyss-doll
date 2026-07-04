#if UNITY_EDITOR
using System;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

[InitializeOnLoad]
public static class PlayModeSceneGuard
{
    private const string MainScenePath = "Assets/Scenes/SampleScene.unity";

    static PlayModeSceneGuard()
    {
        EditorApplication.playModeStateChanged -= HandlePlayModeStateChanged;
        EditorApplication.playModeStateChanged += HandlePlayModeStateChanged;
        EnsurePlayModeStartScene(false);
        TryRestoreMainSceneInEditMode(false);
    }

    private static void HandlePlayModeStateChanged(PlayModeStateChange state)
    {
        if (state == PlayModeStateChange.ExitingEditMode)
        {
            EnsurePlayModeStartScene(true);
        }
        else if (state == PlayModeStateChange.EnteredEditMode)
        {
            EnsurePlayModeStartScene(false);
            TryRestoreMainSceneInEditMode(true);
        }
    }

    private static void EnsurePlayModeStartScene(bool logReroute)
    {
        SceneAsset mainScene = AssetDatabase.LoadAssetAtPath<SceneAsset>(MainScenePath);
        if (mainScene == null)
        {
            if (EditorSceneManager.playModeStartScene != null)
            {
                EditorSceneManager.playModeStartScene = null;
            }

            Debug.LogWarning($"[PlayModeSceneGuard] Could not load runtime entry scene at {MainScenePath}.");
            return;
        }

        if (EditorSceneManager.playModeStartScene != mainScene)
        {
            EditorSceneManager.playModeStartScene = mainScene;
        }

        if (!logReroute)
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        string activeScenePath = NormalizePath(activeScene.path);
        if (!string.Equals(activeScenePath, MainScenePath, StringComparison.OrdinalIgnoreCase))
        {
            string displayPath = string.IsNullOrEmpty(activeScenePath) ? "<unsaved-scene>" : activeScenePath;
            Debug.LogWarning($"[PlayModeSceneGuard] Active scene is {displayPath}. Play mode will start from {MainScenePath} for runtime validation.");
        }
    }

    private static string NormalizePath(string scenePath)
    {
        return string.IsNullOrEmpty(scenePath) ? string.Empty : scenePath.Replace('\\', '/');
    }

    private static void TryRestoreMainSceneInEditMode(bool logRestore)
    {
        if (EditorApplication.isPlayingOrWillChangePlaymode)
        {
            return;
        }

        Scene activeScene = SceneManager.GetActiveScene();
        string activeScenePath = NormalizePath(activeScene.path);
        if (!ShouldRestoreMainScene(activeScenePath))
        {
            return;
        }

        Scene openedScene = EditorSceneManager.OpenScene(MainScenePath, OpenSceneMode.Single);
        if (logRestore && openedScene.IsValid())
        {
            string displayPath = string.IsNullOrEmpty(activeScenePath) ? "<unsaved-scene>" : activeScenePath;
            Debug.LogWarning($"[PlayModeSceneGuard] Active edit scene was {displayPath}. Restored {MainScenePath}.");
        }
    }

    private static bool ShouldRestoreMainScene(string activeScenePath)
    {
        if (string.IsNullOrEmpty(activeScenePath))
        {
            return true;
        }

        if (string.Equals(activeScenePath, MainScenePath, StringComparison.OrdinalIgnoreCase))
        {
            return false;
        }

        return activeScenePath.Contains("/Temp/__Backupscenes/")
            || activeScenePath.StartsWith("Temp/__Backupscenes/", StringComparison.OrdinalIgnoreCase);
    }
}
#endif
