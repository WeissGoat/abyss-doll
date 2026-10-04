// Developer switches stored on this machine only (EditorPrefs). Builds play the prologue unless started with
// -p3skipprologue (there is no save yet, so a playtester restarting the build can skip it).
public static class P3DevSettings {
    private const string PlayPrologueKey = "P3.Dev.PlayPrologueOnStartup";
    private const string UseSaveKey = "P3.Dev.UseSaveInEditor";

    // Off by default: Play Mode in the Editor neither offers 继续游戏 nor writes the save (SaveService). The Editor
    // shares the build's save folder, so this keeps test sessions away from the producer's save.
    public static bool UseSaveInEditor {
        get {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(UseSaveKey, false);
#else
            return true;
#endif
        }
        set {
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetBool(UseSaveKey, value);
#endif
        }
    }

    // Off by default in the Editor so Play Mode opens straight into the workshop with the starting backpack.
    // Toggle with Tools/P3/启动时播放序章, the F1 panel, or P3DevTools.SetPrologueOnStartup(true).
    public static bool PlayPrologueOnStartup {
        get {
#if UNITY_EDITOR
            return UnityEditor.EditorPrefs.GetBool(PlayPrologueKey, false);
#else
            return System.Array.IndexOf(System.Environment.GetCommandLineArgs(), "-p3skipprologue") < 0;
#endif
        }
        set {
#if UNITY_EDITOR
            UnityEditor.EditorPrefs.SetBool(PlayPrologueKey, value);
#endif
        }
    }
}
