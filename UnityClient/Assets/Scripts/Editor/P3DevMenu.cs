using UnityEditor;

public static class P3DevMenu {
    private const string PlayPrologueMenu = "Tools/P3/启动时播放序章";

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
