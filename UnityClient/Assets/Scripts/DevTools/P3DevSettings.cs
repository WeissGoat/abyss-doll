// Developer switches stored on this machine only (EditorPrefs). Builds ignore them and always play the full game.
public static class P3DevSettings {
    private const string PlayPrologueKey = "P3.Dev.PlayPrologueOnStartup";

    // Off by default in the Editor so Play Mode opens straight into the workshop with the starting backpack.
    // Toggle with Tools/P3/启动时播放序章, the F1 panel, or P3DevTools.SetPrologueOnStartup(true).
    public static bool PlayPrologueOnStartup {
        get {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(PlayPrologueKey, false);
#else
            return true;
#endif
        }
        set {
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetBool(PlayPrologueKey, value);
#endif
        }
    }
}
