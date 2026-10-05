using System.Collections.Generic;
using UnityEngine;

// Player settings (M3 Demo shell): master volume and the display mode. Volume is kept in PlayerPrefs and applied on
// start-up; Unity itself remembers the window size and mode, DisplayStartupGuard catches a window left too small.
public static class GameSettingsService {
    private const string VolumeKey = "P3.Settings.Volume";
    public const int VolumeStep = 10;

    public static int VolumePercent => Mathf.RoundToInt(Mathf.Clamp01(PlayerPrefs.GetFloat(VolumeKey, 1f)) * 100f);

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void ApplySavedVolume() {
        AudioListener.volume = VolumePercent / 100f;
    }

    public static int SetVolumePercent(int percent) {
        percent = Mathf.Clamp(percent, 0, 100);
        AudioListener.volume = percent / 100f;
        PlayerPrefs.SetFloat(VolumeKey, percent / 100f);
        PlayerPrefs.Save();
        return percent;
    }

    // Distinct sizes the monitor offers, at least 1280x720, smallest first; the current size is always included.
    public static List<Vector2Int> AvailableResolutions() {
        List<Vector2Int> sizes = new List<Vector2Int>();
        foreach (Resolution resolution in Screen.resolutions) {
            Vector2Int size = new Vector2Int(resolution.width, resolution.height);
            if (size.x >= DisplayStartupGuard.MinimumWidth && size.y >= DisplayStartupGuard.MinimumHeight && !sizes.Contains(size)) {
                sizes.Add(size);
            }
        }

        Vector2Int current = CurrentSize;
        if (!sizes.Contains(current)) {
            sizes.Add(current);
        }

        sizes.Sort((a, b) => a.x != b.x ? a.x.CompareTo(b.x) : a.y.CompareTo(b.y));
        return sizes;
    }

    public static Vector2Int CurrentSize => new Vector2Int(Screen.width, Screen.height);

    public static bool IsFullscreen => Screen.fullScreenMode != FullScreenMode.Windowed;

    public static void ApplyDisplay(Vector2Int size, bool fullscreen) {
        Screen.SetResolution(size.x, size.y, fullscreen ? FullScreenMode.FullScreenWindow : FullScreenMode.Windowed);
    }

    public static string DescribeSize(Vector2Int size) {
        return $"{size.x} × {size.y}";
    }
}
