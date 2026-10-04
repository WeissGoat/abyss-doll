using System;
using UnityEngine;

// Unity stores the last window size in the registry and reuses it on the next launch, so one accidental resize (or a
// test run in a small window) made every later start of the playtest build open as a 637x357 window (found
// 2026-10-04). On startup a window smaller than 1280x720 goes back to borderless fullscreen at desktop resolution.
// Explicit -screen-* arguments (used by tools/build/Test-WindowsBuild.ps1) are respected. Until the settings menu
// exists (M3), Alt+Enter still toggles windowed mode.
public static class DisplayStartupGuard {
    public const int MinimumWidth = 1280;
    public const int MinimumHeight = 720;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
    private static void EnsureUsableWindow() {
        if (Application.isEditor || HasExplicitScreenArgument()) {
            return;
        }

        if (!NeedsReset(Screen.fullScreenMode, Screen.width, Screen.height)) {
            return;
        }

        Resolution desktop = Screen.currentResolution;
        Debug.Log($"[DisplayStartupGuard] Window {Screen.width}x{Screen.height} ({Screen.fullScreenMode}) is too small; switching to fullscreen {desktop.width}x{desktop.height}.");
        Screen.SetResolution(desktop.width, desktop.height, FullScreenMode.FullScreenWindow);
    }

    public static bool NeedsReset(FullScreenMode mode, int width, int height) {
        bool windowed = mode == FullScreenMode.Windowed;
        return windowed && (width < MinimumWidth || height < MinimumHeight);
    }

    private static bool HasExplicitScreenArgument() {
        foreach (string arg in Environment.GetCommandLineArgs()) {
            if (arg.StartsWith("-screen-", StringComparison.OrdinalIgnoreCase)) {
                return true;
            }
        }

        return false;
    }
}
