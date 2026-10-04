using UnityEditor;

public static class P3DevMenu {
    private const string PlayPrologueMenu = "Tools/P3/启动时播放序章";
    private const string UseSaveMenu = "Tools/P3/Editor 里读写存档";

    [MenuItem(UseSaveMenu)]
    private static void ToggleUseSave() {
        P3DevSettings.UseSaveInEditor = !P3DevSettings.UseSaveInEditor;
    }

    [MenuItem(UseSaveMenu, true)]
    private static bool ToggleUseSaveValidate() {
        Menu.SetChecked(UseSaveMenu, P3DevSettings.UseSaveInEditor);
        return true;
    }

    [MenuItem(PlayPrologueMenu)]
    private static void TogglePlayPrologue() {
        P3DevSettings.PlayPrologueOnStartup = !P3DevSettings.PlayPrologueOnStartup;
    }

    [MenuItem(PlayPrologueMenu, true)]
    private static bool TogglePlayPrologueValidate() {
        Menu.SetChecked(PlayPrologueMenu, P3DevSettings.PlayPrologueOnStartup);
        return true;
    }
}
