using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// The end of the day, upstairs in the attic (docs/specs/2026-10-17-home-loop.md): Zero says one thing about the day
// (ZeroLineService night pool) and a sheet on the table keeps the numbers the old 「第 N 天结束」 card showed. Some
// nights her word comes with two replies, which may promise her something for the next dive (PromiseService,
// docs/specs/2026-10-18-home-evening-thread.md C). 晚安 moves to the morning. She stands close as a large portrait
// whose face follows what she says (the line's Face, doll_zero_bust_<face>); without the portrait art the room's
// small Zero is all there is.
public class HomeNightTalkView : MonoBehaviour {
    public const string RootName = "HomeNightTalk_Runtime";
    public const string GoodnightLabel = "晚安";
    public const string BustVisualID = "doll_zero_bust";
    private static readonly Color NightTint = new Color(0.9f, 0.89f, 0.97f, 1f);

    private Text _title;
    private Text _body;
    private Text _line;
    private GameObject _bubble;
    private Text _note;
    private Image _bust;
    private readonly Button[] _replies = new Button[2];
    private NightTalkConfig _talk;
    private DayEndReport _report;
    private System.Action _onGoodnight;

    public bool IsShowing => gameObject.activeSelf;
    public bool HasReplies => _talk != null && _replies[0].gameObject.activeSelf;
    public string CurrentLine => _line.text;
    // The face on her large portrait now (ZeroFace), or empty when there is no portrait.
    public string CurrentFace { get; private set; } = string.Empty;

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
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        _report = report;
        _talk = PromiseService.PickTalk(report, player);
        HomeLine night = _talk == null ? ZeroLineService.NightPick(report, player) : null;
        string line = _talk != null ? _talk.Line : night?.Text;
        ShowFace(_talk != null ? _talk.Face : night?.Face);
        _bubble.SetActive(!string.IsNullOrEmpty(line));
        _line.text = $"「{line}」";
        _note.text = string.Empty;
        for (int i = 0; i < _replies.Length; i++) {
            bool show = _talk != null && i < _talk.Replies.Count;
            _replies[i].gameObject.SetActive(show);
            if (show) {
                UITheme.SetButtonLabel(_replies[i], _talk.Replies[i].Text);
            }
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    public void Reply(int index) {
        if (_talk == null || index < 0 || index >= _talk.Replies.Count) {
            return;
        }

        NightReplyResult result = PromiseService.Reply(GameRoot.Core?.CurrentPlayer, _report, _talk.Replies[index]);
        _line.text = $"「{result.Answer}」";
        ShowFace(result.Face);
        List<string> notes = new List<string>();
        if (result.Bond != null && result.Bond.PointsAdded > 0) {
            notes.Add($"羁绊 +{result.Bond.PointsAdded}");
        }

        if (result.Promise != null) {
            notes.Add($"约好了：{result.Promise.Text}");
        }

        _note.text = string.Join("    ", notes);
        foreach (Button reply in _replies) {
            reply.gameObject.SetActive(false);
        }

        StudioCareView.AnnounceBond(result.Bond);
    }

    private void ShowFace(string face) {
        if (string.IsNullOrEmpty(face) || face == ZeroFace.Neutral || !VisualAssetService.TryGetSprite(BustVisualID + "_" + face, out _)) {
            face = ZeroFace.Neutral;
        }

        string visualID = face == ZeroFace.Neutral ? BustVisualID : BustVisualID + "_" + face;
        // A little into the night light, so she sits in the dimmed room rather than on top of it.
        bool shown = VisualUIHelper.ApplySprite(_bust, visualID, NightTint, Color.clear);
        _bust.gameObject.SetActive(shown);
        CurrentFace = shown ? face : string.Empty;
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

        // Zero close by the window, on the right, standing on the bottom edge.
        _bust = UITheme.CreatePanel(transform, "ZeroBust", Color.white);
        _bust.raycastTarget = false;
        UITheme.Place(_bust.rectTransform, new Vector2(0.5f, 0f), new Vector2(560f, 0f), new Vector2(720f, 920f));
        _bust.gameObject.SetActive(false);

        Image sheet = UITheme.CreatePanel(transform, "NightSheet", UITheme.Panel);
        UITheme.ApplyBoardSkin(sheet, VisualAssetService.UIPanelMainID);
        UITheme.Place(sheet.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-300f, -150f), new Vector2(780f, 520f));
        _title = UITheme.CreateText(sheet.transform, "Title_Text", string.Empty, UITheme.TitleSize, UITheme.Accent, TextAnchor.MiddleCenter);
        UITheme.Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -50f), new Vector2(680f, 52f));
        _body = UITheme.CreateText(sheet.transform, "Body_Text", string.Empty, UITheme.BodySize - 2, UITheme.TextPrimary, TextAnchor.UpperLeft);
        _body.lineSpacing = 1.25f;
        UITheme.Place(_body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -100f), new Vector2(680f, 290f));
        Button goodnight = UITheme.CreateButton(sheet.transform, "Goodnight_Button", GoodnightLabel, UITheme.Accent, UITheme.OnAccent);
        UITheme.Place((RectTransform)goodnight.transform, new Vector2(0.5f, 0f), new Vector2(0f, 44f), new Vector2(240f, 60f));
        goodnight.onClick.AddListener(Goodnight);

        // Her line hangs beside her head, above the sheet.
        Image bubble = UITheme.CreatePanel(transform, "ZeroBubble", UITheme.PanelRaised);
        UITheme.AddBorder(bubble, UITheme.Border, 1.5f);
        bubble.raycastTarget = false;
        UITheme.Place(bubble.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-150f, 395f), new Vector2(760f, 110f));
        _bubble = bubble.gameObject;
        _line = UITheme.CreateText(bubble.transform, "Line_Text", string.Empty, UITheme.BodySize, UITheme.TextPrimary, TextAnchor.MiddleCenter);
        UITheme.Stretch(_line.rectTransform);
        _line.rectTransform.offsetMin = new Vector2(18f, 22f);
        _line.rectTransform.offsetMax = new Vector2(-18f, -6f);
        _note = UITheme.CreateText(bubble.transform, "Note_Text", string.Empty, UITheme.CaptionSize - 2, UITheme.Accent, TextAnchor.LowerCenter);
        UITheme.Stretch(_note.rectTransform);
        _note.rectTransform.offsetMin = new Vector2(12f, 6f);
        _note.rectTransform.offsetMax = new Vector2(-12f, 0f);

        // Two replies side by side under her word, on some nights.
        for (int i = 0; i < _replies.Length; i++) {
            int index = i;
            Button reply = UITheme.CreateButton(transform, "NightReply_" + i, string.Empty, UITheme.PanelRaised, UITheme.TextPrimary, UITheme.CaptionSize + 1);
            UITheme.AddBorder(reply.targetGraphic, UITheme.Accent, 1.5f);
            UITheme.Place((RectTransform)reply.transform, new Vector2(0.5f, 0.5f), new Vector2(-307f + i * 315f, 268f), new Vector2(300f, 56f));
            reply.onClick.AddListener(() => Reply(index));
            reply.gameObject.SetActive(false);
            _replies[i] = reply;
        }
    }
}
