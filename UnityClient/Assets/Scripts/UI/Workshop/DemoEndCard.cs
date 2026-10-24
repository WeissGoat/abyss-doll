using UnityEngine;
using UnityEngine.UI;

// The end of the Demo, last beat (DemoEndingService): after 晚安 on the ending's night, 「第一章 · 完」 with a line of
// what lies ahead and the run's numbers; the town goes on, or back to the title.
public class DemoEndCard : MonoBehaviour {
    public const string RootName = "DemoEndCard_Runtime";
    public const string TitleText = "第一章 · 完";
    public const string Teaser = "深渊下面，有人在唱她的歌。";
    public const string ContinueLabel = "继续经营";
    public const string TitleLabel = "回到标题";

    private Text _stats;

    public static DemoEndCard Active { get; private set; }
    public static bool IsShowing => Active != null && Active.gameObject.activeSelf;

    public static DemoEndCard Show(DemoEndingStats stats) {
        Canvas canvas = MainCanvas.Find();
        if (canvas == null) {
            return null;
        }

        if (Active == null) {
            GameObject root = UITheme.CreateModal(canvas.transform, RootName, new Vector2(900f, 640f), 900);
            root.GetComponent<Image>().color = new Color(0.01f, 0.02f, 0.03f, 0.94f);
            Active = root.AddComponent<DemoEndCard>();
            Active.Build((RectTransform)root.transform.Find("Dialog"));
        }

        Active._stats.text = BuildStats(stats);
        Active.gameObject.SetActive(true);
        Active.transform.SetAsLastSibling();
        return Active;
    }

    public static string BuildStats(DemoEndingStats stats) {
        stats = stats ?? new DemoEndingStats();
        return $"第 {stats.Day} 天    下潜 {stats.Dives} 次\n"
            + $"和零号的羁绊  {stats.BondLevel} 级\n"
            + $"守住的约定  {stats.KeptPromises} 个    阁楼上的纪念物  {stats.Mementos} 件";
    }

    public void Close() {
        gameObject.SetActive(false);
    }

    private void OnDestroy() {
        if (Active == this) {
            Active = null;
        }
    }

    private void Build(RectTransform dialog) {
        Text title = UITheme.CreateText(dialog, "Title_Text", TitleText, UITheme.TitleSize + 8, UITheme.Accent, TextAnchor.MiddleCenter);
        UITheme.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -90f), new Vector2(800f, 80f));
        Text teaser = UITheme.CreateText(dialog, "Teaser_Text", Teaser, UITheme.HeadingSize, UITheme.TextPrimary, TextAnchor.MiddleCenter);
        UITheme.Place(teaser.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -190f), new Vector2(800f, 50f));
        _stats = UITheme.CreateText(dialog, "Stats_Text", string.Empty, UITheme.BodySize, UITheme.TextSecondary, TextAnchor.MiddleCenter);
        _stats.lineSpacing = 1.4f;
        UITheme.Place(_stats.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -280f), new Vector2(800f, 150f));
        Text thanks = UITheme.CreateText(dialog, "Thanks_Text", "感谢游玩。存档会接着走，可以继续经营。", UITheme.CaptionSize, UITheme.TextSecondary, TextAnchor.MiddleCenter);
        UITheme.Place(thanks.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 130f), new Vector2(800f, 36f));

        Button goOn = UITheme.CreateButton(dialog, "DemoEndContinue_Button", ContinueLabel, UITheme.Accent, UITheme.OnAccent);
        UITheme.Place((RectTransform)goOn.transform, new Vector2(0.5f, 0f), new Vector2(-150f, 44f), new Vector2(240f, 60f));
        goOn.onClick.AddListener(Close);
        Button title2 = UITheme.CreateButton(dialog, "DemoEndTitle_Button", TitleLabel, UITheme.PanelRaised, UITheme.TextPrimary);
        UITheme.AddBorder(title2.targetGraphic, UITheme.Border, 1.5f);
        UITheme.Place((RectTransform)title2.transform, new Vector2(0.5f, 0f), new Vector2(150f, 44f), new Vector2(240f, 60f));
        title2.onClick.AddListener(GameRoot.RestartGame);
    }
}
