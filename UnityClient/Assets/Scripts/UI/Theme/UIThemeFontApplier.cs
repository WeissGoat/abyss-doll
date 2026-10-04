using UnityEngine;
using UnityEngine.UI;

// Switches every UGUI Text that still uses the built-in font to UITheme.Font. Most screens are assembled in code and
// prefabs from many places; sweeping a few times a second covers all of them (including ones spawned later) without
// touching each controller. Screens rebuilt on UITheme set the font themselves and are skipped.
public class UIThemeFontApplier : MonoBehaviour {
    private const float SweepInterval = 0.25f;
    private float _nextSweep;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Install() {
        if (FindObjectOfType<UIThemeFontApplier>() != null) {
            return;
        }

        GameObject host = new GameObject("UIThemeFontApplier");
        DontDestroyOnLoad(host);
        host.AddComponent<UIThemeFontApplier>();
    }

    private void Update() {
        if (Time.unscaledTime < _nextSweep) {
            return;
        }

        _nextSweep = Time.unscaledTime + SweepInterval;
        Sweep();
    }

    public static int Sweep() {
        Font themeFont = UITheme.Font;
        if (UITheme.IsLegacyFont(themeFont)) {
            return 0;
        }

        int changed = 0;
        foreach (Text text in FindObjectsOfType<Text>(true)) {
            if (text != null && UITheme.IsLegacyFont(text.font)) {
                text.font = themeFont;
                changed++;
            }
        }

        return changed;
    }
}
