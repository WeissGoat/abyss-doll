using System;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class PlayModeRuntimeEntrySceneBootstrap
{
    private const string MainSceneName = "SampleScene";
    private const string MainScenePath = "Assets/Scenes/SampleScene.unity";

    private static bool _mainSceneLoadRequested;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureRuntimeEntryScene()
    {
        _mainSceneLoadRequested = false;
        SceneManager.sceneLoaded -= HandleSceneLoaded;
        SceneManager.sceneLoaded += HandleSceneLoaded;
        ForceLoadMainSceneIfNeeded("BeforeSceneLoad");
    }

    private static void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (IsMainScene(scene))
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
            return;
        }

        ForceLoadMainSceneIfNeeded($"Loaded {DescribeScene(scene)}");
    }

    private static void ForceLoadMainSceneIfNeeded(string reason)
    {
        Scene activeScene = SceneManager.GetActiveScene();
        if (IsMainScene(activeScene))
        {
            return;
        }

        if (_mainSceneLoadRequested)
        {
            return;
        }

        _mainSceneLoadRequested = true;
        Debug.LogWarning($"[PlayModeRuntimeEntrySceneBootstrap] {reason}. Loading {MainScenePath} for runtime validation.");
        SceneManager.LoadScene(MainSceneName, LoadSceneMode.Single);
    }

    private static bool IsMainScene(Scene scene)
    {
        if (!scene.IsValid())
        {
            return false;
        }

        if (string.Equals(scene.name, MainSceneName, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        string normalizedPath = NormalizePath(scene.path);
        return string.Equals(normalizedPath, MainScenePath, StringComparison.OrdinalIgnoreCase);
    }

    private static string DescribeScene(Scene scene)
    {
        if (!scene.IsValid())
        {
            return "<invalid-scene>";
        }

        string normalizedPath = NormalizePath(scene.path);
        if (!string.IsNullOrEmpty(normalizedPath))
        {
            return normalizedPath;
        }

        return string.IsNullOrEmpty(scene.name) ? "<unnamed-scene>" : scene.name;
    }

    private static string NormalizePath(string scenePath)
    {
        return string.IsNullOrEmpty(scenePath) ? string.Empty : scenePath.Replace('\\', '/');
    }
}
