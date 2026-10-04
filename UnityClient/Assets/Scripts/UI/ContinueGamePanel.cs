using System;
using UnityEngine;
using UnityEngine.UI;

// Shown at start-up when a save exists: 继续游戏 (load it, skip the prologue) or 新游戏 (start over; the first
// autosave replaces the old save). The Demo's title screen comes with the M3 shell.
public static class ContinueGamePanel {
    public const string PanelName = "ContinueGamePanel_Runtime";
    public const string ContinueLabel = "继续游戏";
    public const string NewGameLabel = "新游戏";

    public static void Show(SaveFile save, Action onContinue, Action onNewGame) {
        Canvas canvas = MainCanvas.Find();
        if (canvas == null) {
            onNewGame?.Invoke();
            return;
        }

        GameObject root = UITheme.CreateModal(canvas.transform, PanelName, new Vector2(680f, 340f), 700);
        Transform dialog = root.transform.Find("Dialog");
        Text title = UITheme.CreateText(dialog, "Title_Text", "Project P3", UITheme.TitleSize, UITheme.Accent, TextAnchor.MiddleCenter);
        UITheme.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(600f, 52f));

        PlayerProfile player = save.Player;
        string summary = $"存档：第 {player.CurrentDay} 天 · 资金 {player.Money}G · 零号 HP {player.ActiveDoll?.Status.HP_Current ?? 0}/{player.ActiveDoll?.Status.HP_Max ?? 0}\n保存于 {save.SavedAt}";
        Text body = UITheme.CreateText(dialog, "Body_Text", summary, UITheme.BodySize, UITheme.TextPrimary, TextAnchor.MiddleCenter);
        UITheme.Place(body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(600f, 80f));

        Text note = UITheme.CreateText(dialog, "Note_Text", "新游戏会在第一次自动保存时覆盖这个存档。", UITheme.CaptionSize - 2, UITheme.TextMuted, TextAnchor.MiddleCenter);
        UITheme.Place(note.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 100f), new Vector2(600f, 26f));

        Button continueButton = UITheme.CreateButton(dialog, "Continue_Button", ContinueLabel, UITheme.Accent, UITheme.Background, UITheme.HeadingSize - 2);
        UITheme.Place((RectTransform)continueButton.transform, new Vector2(0.5f, 0f), new Vector2(-140f, 28f), new Vector2(240f, 64f));
        continueButton.onClick.AddListener(() => {
            UnityEngine.Object.Destroy(root);
            onContinue?.Invoke();
        });

        Button newButton = UITheme.CreateButton(dialog, "NewGame_Button", NewGameLabel, UITheme.PanelRaised, UITheme.TextPrimary, UITheme.HeadingSize - 2);
        UITheme.AddBorder(newButton.targetGraphic, UITheme.Border, 1.5f);
        UITheme.Place((RectTransform)newButton.transform, new Vector2(0.5f, 0f), new Vector2(140f, 28f), new Vector2(240f, 64f));
        newButton.onClick.AddListener(() => {
            UnityEngine.Object.Destroy(root);
            onNewGame?.Invoke();
        });
    }
}
