using UnityEngine;
using UnityEngine.UI;

// The end of the day, upstairs in the attic (docs/specs/2026-10-17-home-loop.md): Zero says one thing about the day
// (ZeroLineService night pool) and a sheet on the table keeps the numbers the old 「第 N 天结束」 card showed. 晚安
// moves to the morning.
public class HomeNightTalkView : MonoBehaviour {
    public const string RootName = "HomeNightTalk_Runtime";
    public const string GoodnightLabel = "晚安";

    private Text _title;
    private Text _body;
    private Text _line;
    private GameObject _bubble;
    private System.Action _onGoodnight;

    public bool IsShowing => gameObject.activeSelf;

    public static HomeNightTalkView Create(Transform parent) {
        GameObject root = new GameObject(RootName, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        UITheme.Stretch((RectTransform)root.transform);
        HomeNightTalkView view = root.AddComponent<HomeNightTalkView>();
        view.Build();
        root.SetActive(false);
        return view;
    }

    public void Show(DayEndReport report, System.Action onGoodnight) {
        _onGoodnight = onGoodnight;
        _title.text = $"第 {report.EndedDay} 天的夜里";
        _body.text = TownDayHUD.BuildNightText(report);
        string line = ZeroLineService.NightLine(report, GameRoot.Core?.CurrentPlayer);
        _bubble.SetActive(!string.IsNullOrEmpty(line));
        _line.text = $"「{line}」";
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    private void Goodnight() {
        gameObject.SetActive(false);
        System.Action next = _onGoodnight;
        _onGoodnight = null;
        next?.Invoke();
    }

    private void Build() {
        // The room dims a little: it is night upstairs.
        Image dim = UITheme.CreatePanel(transform, "NightDim", new Color(0.05f, 0.05f, 0.14f, 0.55f));
        UITheme.Stretch(dim.rectTransform);
        dim.raycastTarget = true;

        Image sheet = UITheme.CreatePanel(transform, "NightSheet", UITheme.Panel);
        UITheme.ApplyBoardSkin(sheet, VisualAssetService.UIPanelMainID);
        UITheme.Place(sheet.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-280f, -20f), new Vector2(780f, 520f));
        _title = UITheme.CreateText(sheet.transform, "Title_Text", string.Empty, UITheme.TitleSize, UITheme.Accent, TextAnchor.MiddleCenter);
        UITheme.Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(680f, 52f));
        _body = UITheme.CreateText(sheet.transform, "Body_Text", string.Empty, UITheme.BodySize - 2, UITheme.TextPrimary, TextAnchor.UpperLeft);
        _body.lineSpacing = 1.25f;
        UITheme.Place(_body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(680f, 290f));
        Button goodnight = UITheme.CreateButton(sheet.transform, "Goodnight_Button", GoodnightLabel, UITheme.Accent, UITheme.OnAccent);
        UITheme.Place((RectTransform)goodnight.transform, new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(240f, 60f));
        goodnight.onClick.AddListener(Goodnight);

        // Zero stands by the window in the attic; her line hangs beside her.
        Image bubble = UITheme.CreatePanel(transform, "ZeroBubble", UITheme.PanelRaised);
        UITheme.AddBorder(bubble, UITheme.Border, 1.5f);
        bubble.raycastTarget = false;
        UITheme.Place(bubble.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(520f, 280f), new Vector2(520f, 96f));
        _bubble = bubble.gameObject;
        _line = UITheme.CreateText(bubble.transform, "Line_Text", string.Empty, UITheme.BodySize, UITheme.TextPrimary, TextAnchor.MiddleCenter);
        UITheme.Stretch(_line.rectTransform);
        _line.rectTransform.offsetMin = new Vector2(18f, 6f);
        _line.rectTransform.offsetMax = new Vector2(-18f, -6f);
    }
}
