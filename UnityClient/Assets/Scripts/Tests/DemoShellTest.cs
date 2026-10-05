using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// M3 batch 3: the title screen offers 继续游戏 only with a save, a new game over a save asks first, and the settings
// keep the volume inside 0-100%.
public static class DemoShellTest {
    public static void Run() {
        Debug.Log("=== Running Demo Shell Test ===");
        List<string> failures = new List<string>();
        void Check(bool condition, string message) {
            if (!condition) {
                failures.Add(message);
            }
        }

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        // The title goes under the scene's main canvas when one is open; otherwise under a test canvas.
        GameObject ownCanvas = null;
        if (MainCanvas.Find() == null) {
            ownCanvas = new GameObject("InventoryCanvas", typeof(RectTransform));
            ownCanvas.AddComponent<Canvas>().renderMode = RenderMode.ScreenSpaceOverlay;
        }

        GameObject canvasObj = MainCanvas.Find().gameObject;
        try {
            // No save: 新游戏 / 设置 / 退出游戏; 新游戏 starts right away.
            int newGames = 0;
            TitleScreen.Show(null, () => { }, () => newGames++);
            List<string> labels = Labels(canvasObj);
            Check(!labels.Contains(TitleScreen.ContinueLabel) && labels.Contains(TitleScreen.NewGameLabel) && labels.Contains(TitleScreen.SettingsLabel) && labels.Contains(TitleScreen.QuitLabel), "title without a save: " + string.Join(",", labels));
            Click(canvasObj, TitleScreen.NewGameLabel);
            Check(newGames == 1, "新游戏 did not start a game");
            DestroyTitle(canvasObj);

            // With a save: 继续游戏 restores; 新游戏 asks before starting.
            SaveFile save = new SaveFile { Player = core.CurrentPlayer, SavedAt = "2026-10-05 12:00" };
            int continues = 0;
            newGames = 0;
            TitleScreen.Show(save, () => continues++, () => newGames++);
            labels = Labels(canvasObj);
            Check(labels.Contains(TitleScreen.ContinueLabel), "title with a save has no 继续游戏");
            Click(canvasObj, TitleScreen.NewGameLabel);
            Check(newGames == 0 && Labels(canvasObj).Contains("开始新游戏"), "新游戏 over a save did not ask first");
            Click(canvasObj, "开始新游戏");
            Check(newGames == 1, "the confirmed new game did not start");
            DestroyTitle(canvasObj);

            // Volume stays inside 0-100%.
            int before = GameSettingsService.VolumePercent;
            Check(GameSettingsService.SetVolumePercent(130) == 100 && GameSettingsService.SetVolumePercent(-5) == 0, "volume not clamped");
            GameSettingsService.SetVolumePercent(before);
            Check(GameSettingsService.AvailableResolutions().Count > 0, "no resolution to choose");
        } finally {
            DestroyTitle(canvasObj);
            if (ownCanvas != null) {
                Object.DestroyImmediate(ownCanvas);
            }

            GameRoot.Core = null;
        }

        if (failures.Count == 0) {
            Debug.Log("Demo Shell PASSED.");
        } else {
            Debug.LogError("Demo Shell FAILED: " + string.Join("; ", failures));
        }

        Debug.Log("=== Demo Shell Test Finished ===");
    }

    private static List<string> Labels(GameObject root) {
        List<string> labels = new List<string>();
        Transform title = root.transform.Find(TitleScreen.PanelName);
        if (title == null) {
            return labels;
        }

        foreach (Button button in title.GetComponentsInChildren<Button>(true)) {
            Text text = button.GetComponentInChildren<Text>();
            if (text != null) {
                labels.Add(text.text);
            }
        }

        return labels;
    }

    private static void Click(GameObject root, string label) {
        Transform title = root.transform.Find(TitleScreen.PanelName);
        if (title == null) {
            return;
        }

        foreach (Button button in title.GetComponentsInChildren<Button>(true)) {
            Text text = button.GetComponentInChildren<Text>();
            if (text != null && text.text == label) {
                button.onClick.Invoke();
                return;
            }
        }
    }

    // Destroy() is deferred in Edit Mode; drop whatever title is left so the next case starts clean.
    private static void DestroyTitle(GameObject root) {
        Transform title = root.transform.Find(TitleScreen.PanelName);
        while (title != null) {
            Object.DestroyImmediate(title.gameObject);
            title = root.transform.Find(TitleScreen.PanelName);
        }
    }
}
