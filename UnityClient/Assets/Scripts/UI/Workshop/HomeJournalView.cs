using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// The journal page in the hall (docs/specs/2026-10-17-home-loop.md): when Zero comes through the door it opens on the
// dive just finished, with the route drawn as node icons (a red cross where she got hurt), what came home and what
// was lost, and her one line about the day, said up close as her large portrait with the line's face (ZeroBust). The
// wall journal opens the same page on any past dive.
public class HomeJournalView : MonoBehaviour {
    public const string RootName = "HomeJournal_Runtime";
    public const string CareLabel = "去工作室看看她";
    public const string CloseLabel = "收起手账";
    public const string PreviousLabel = "‹ 上一页";
    public const string NextLabel = "下一页 ›";

    private const float StepSize = 64f;
    private const float StepGap = 26f;

    private Image _page;
    private Text _title;
    private Text _stamp;
    private RectTransform _route;
    private Text _body;
    private GameObject _bubble;
    private Text _bubbleText;
    private Image _bust;
    private Button _care;
    private Button _close;
    private Button _previous;
    private Button _next;
    private readonly List<GameObject> _steps = new List<GameObject>();
    private System.Action _onCare;
    private System.Action<bool> _onShowing;
    private int _index;

    public bool IsShowing => gameObject.activeSelf;
    public bool IsArrival { get; private set; }
    // Her face on the portrait beside the page (ZeroFace), or empty when the portrait is not up.
    public string CurrentFace { get; private set; } = string.Empty;
    public bool HasPortrait => IsShowing && _bust.gameObject.activeSelf;

    // onShowing tells the room the page opened or closed (the hall hides its plaques under it).
    public static HomeJournalView Create(Transform parent, System.Action onCare, System.Action<bool> onShowing) {
        GameObject root = new GameObject(RootName, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        UITheme.Stretch((RectTransform)root.transform);
        HomeJournalView view = root.AddComponent<HomeJournalView>();
        view._onCare = onCare;
        view._onShowing = onShowing;
        view.Build();
        root.SetActive(false);
        return view;
    }

    // The page for the dive that just ended, with Zero's line; marks it shown.
    public void ShowArrival(DiveLog log) {
        List<DiveLog> journal = GameRoot.Core?.CurrentPlayer?.DiveJournal;
        _index = journal != null ? journal.IndexOf(log) : -1;
        IsArrival = true;
        log.Shown = true;
        Present(log);
    }

    // The wall journal: the newest page first, older ones with ‹ 上一页.
    public void OpenJournal() {
        List<DiveLog> journal = GameRoot.Core?.CurrentPlayer?.DiveJournal;
        if (journal == null || journal.Count == 0) {
            return;
        }

        IsArrival = false;
        _index = journal.Count - 1;
        Present(journal[_index]);
    }

    public void Close() {
        gameObject.SetActive(false);
        _onShowing?.Invoke(false);
    }

    private void Turn(int delta) {
        List<DiveLog> journal = GameRoot.Core?.CurrentPlayer?.DiveJournal;
        if (journal == null || journal.Count == 0) {
            return;
        }

        _index = Mathf.Clamp(_index + delta, 0, journal.Count - 1);
        Present(journal[_index]);
    }

    private void Present(DiveLog log) {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        _title.text = $"第 {log.Day} 天 · 第 {log.LayerID} 层";
        _stamp.text = log.Defeated ? "战败" : "平安撤离";
        _stamp.color = log.Defeated ? UITheme.Danger : UITheme.Positive;
        BuildRoute(log);
        _body.text = BuildBody(log);

        HomeLine said = ZeroLineService.ArrivalPick(log);
        string line = said?.Text;
        _bubble.SetActive(!string.IsNullOrEmpty(line));
        _bubbleText.text = $"「{line}」";
        CurrentFace = string.IsNullOrEmpty(line) ? string.Empty : ZeroBust.Apply(_bust, said.Face, Color.white);
        _bust.gameObject.SetActive(!string.IsNullOrEmpty(CurrentFace));
        // Up close the bubble sits above her face, under the room tabs; beside her small figure it hangs by her head.
        ((RectTransform)_bubble.transform).anchoredPosition = HasPortrait ? new Vector2(-260f, 300f) : new Vector2(-310f, 300f);

        List<DiveLog> journal = GameRoot.Core?.CurrentPlayer?.DiveJournal;
        int count = journal?.Count ?? 0;
        _care.gameObject.SetActive(IsArrival);
        UITheme.SetButtonLabel(_close, IsArrival ? CloseLabel : "合上");
        // Arriving: 照看她 | 收起手账. On the wall: ‹ 上一页 | 合上 | 下一页 › (合上 used to sit on 下一页).
        ((RectTransform)_close.transform).anchoredPosition = IsArrival ? new Vector2(150f, 40f) : new Vector2(0f, 40f);
        ((RectTransform)_previous.transform).anchoredPosition = new Vector2(-260f, 40f);
        ((RectTransform)_next.transform).anchoredPosition = new Vector2(260f, 40f);
        _previous.gameObject.SetActive(!IsArrival);
        _next.gameObject.SetActive(!IsArrival);
        _previous.interactable = _index > 0;
        _next.interactable = _index >= 0 && _index < count - 1;
        _onShowing?.Invoke(true);
    }

    public static string BuildBody(DiveLog log) {
        List<string> lines = new List<string>();
        lines.Add(log.BroughtOut > 0
            ? $"带回 {log.BroughtOut} 件，约 {log.BroughtOutValue}G：{Join(log.BroughtOutNames)}"
            : "这次什么都没带回来。");
        if (log.Lost > 0) {
            lines.Add($"丢在下面 {log.Lost} 件：{Join(log.LostNames)}");
        }

        if (log.LeftBehind > 0) {
            lines.Add($"背包装不下，留下了 {log.LeftBehind} 件。");
        }

        if (log.DefeatedEnemies.Count > 0) {
            lines.Add("打倒：" + string.Join("、", log.DefeatedEnemies
                .GroupBy(name => name)
                .Select(group => group.Count() > 1 ? $"{group.Key} ×{group.Count()}" : group.Key)));
        }

        if (log.Defeated && !string.IsNullOrEmpty(log.DefeatedBy)) {
            lines.Add($"倒在了 {log.DefeatedBy} 面前。");
        } else if (log.EliteBypassed) {
            lines.Add("绕开了精英。");
        }

        if (!string.IsNullOrEmpty(log.PromiseText)) {
            lines.Add($"约定「{log.PromiseText}」：{(log.PromiseKept ? "做到了" : "没做到")}。");
        }

        lines.Add($"零号  HP {log.HPStart} → {log.HPEnd} / {log.HPMax}    SAN {log.SANStart} → {log.SANEnd} / {log.SANMax}");
        return string.Join("\n", lines);
    }

    private static string Join(List<string> names) {
        if (names == null || names.Count == 0) {
            return string.Empty;
        }

        List<string> shown = names.Take(5).ToList();
        return string.Join("、", shown) + (names.Count > shown.Count ? $" 等 {names.Count} 件" : string.Empty);
    }

    private void BuildRoute(DiveLog log) {
        foreach (GameObject step in _steps) {
            if (step != null) {
                if (Application.isPlaying) {
                    Destroy(step);
                } else {
                    DestroyImmediate(step);
                }
            }
        }
        _steps.Clear();

        int count = log.Steps.Count;
        float scale = Mathf.Min(1f, _route.rect.width > 0f ? _route.rect.width / Mathf.Max(1f, count * (StepSize + StepGap)) : 1f);
        float size = StepSize * scale;
        float pitch = (StepSize + StepGap) * scale;
        float start = -(count - 1) * pitch * 0.5f;
        for (int i = 0; i < count; i++) {
            DiveLogStep step = log.Steps[i];
            float x = start + i * pitch;
            if (i > 0) {
                Image link = UITheme.CreatePanel(_route, "Link_" + i, UITheme.Border);
                link.raycastTarget = false;
                UITheme.Place(link.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x - pitch * 0.5f, 10f), new Vector2(pitch - size, 3f));
                _steps.Add(link.gameObject);
            }

            Image icon = UITheme.CreatePanel(_route, "Step_" + i, Color.white);
            UITheme.Place(icon.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, 10f), new Vector2(size, size));
            VisualUIHelper.ApplyContainSprite(icon, step.IconID, new Vector2(size, size), Color.white, UITheme.Border, false);
            _steps.Add(icon.gameObject);

            Text kind = UITheme.CreateText(_route, "StepKind_" + i, step.Kind, UITheme.CaptionSize - 3, step.Boss ? UITheme.Accent : UITheme.TextSecondary, TextAnchor.UpperCenter);
            UITheme.Place(kind.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(x, -size * 0.5f - 6f), new Vector2(pitch, 24f));
            _steps.Add(kind.gameObject);

            if (step.Hurt) {
                // A red cross on the icon's corner, outlined so it reads on red and dark icons alike.
                Text cross = UITheme.CreateText(icon.transform, "Hurt_Text", "×", Mathf.RoundToInt(46f * scale), UITheme.Danger, TextAnchor.MiddleCenter);
                UITheme.Place(cross.rectTransform, new Vector2(1f, 1f), new Vector2(-4f * scale, -4f * scale), new Vector2(40f * scale, 40f * scale));
                UITheme.AddBorder(cross, UITheme.OnAccent, 2f);
            }
        }
    }

    private void Build() {
        // Zero at the left of the hall, where her small figure stands, close enough to read her face.
        _bust = UITheme.CreatePanel(transform, "ZeroBust", Color.white);
        _bust.raycastTarget = false;
        UITheme.Place(_bust.rectTransform, new Vector2(0.5f, 0f), new Vector2(-600f, 0f), ZeroBust.Size);
        _bust.gameObject.SetActive(false);

        _page = UITheme.CreatePanel(transform, "JournalPage", UITheme.Panel);
        UITheme.ApplyBoardSkin(_page, VisualAssetService.UIPanelMainID);
        UITheme.Place(_page.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(400f, 10f), new Vector2(900f, 640f));

        _title = UITheme.CreateText(_page.transform, "Title_Text", string.Empty, UITheme.HeadingSize, UITheme.Accent);
        UITheme.Place(_title.rectTransform, new Vector2(0f, 1f), new Vector2(250f, -50f), new Vector2(440f, 44f));
        _stamp = UITheme.CreateText(_page.transform, "Stamp_Text", string.Empty, UITheme.HeadingSize, UITheme.Positive, TextAnchor.MiddleRight);
        UITheme.Place(_stamp.rectTransform, new Vector2(1f, 1f), new Vector2(-150f, -50f), new Vector2(240f, 44f));

        GameObject route = new GameObject("Route", typeof(RectTransform));
        route.transform.SetParent(_page.transform, false);
        _route = (RectTransform)route.transform;
        UITheme.Place(_route, new Vector2(0.5f, 1f), new Vector2(0f, -160f), new Vector2(820f, 120f));

        _body = UITheme.CreateText(_page.transform, "Body_Text", string.Empty, UITheme.BodySize - 2, UITheme.TextPrimary, TextAnchor.UpperLeft);
        _body.lineSpacing = 1.25f;
        UITheme.Place(_body.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -360f), new Vector2(800f, 240f));

        _care = UITheme.CreateButton(_page.transform, "JournalCare_Button", CareLabel, UITheme.Accent, UITheme.OnAccent);
        UITheme.Place((RectTransform)_care.transform, new Vector2(0.5f, 0f), new Vector2(-150f, 40f), new Vector2(260f, 58f));
        _care.onClick.AddListener(() => {
            Close();
            _onCare?.Invoke();
        });
        _close = UITheme.CreateButton(_page.transform, "JournalClose_Button", CloseLabel, UITheme.PanelRaised, UITheme.TextPrimary);
        UITheme.AddBorder(_close.targetGraphic, UITheme.Border, 1.5f);
        UITheme.Place((RectTransform)_close.transform, new Vector2(0.5f, 0f), new Vector2(150f, 40f), new Vector2(220f, 58f));
        _close.onClick.AddListener(Close);
        _previous = UITheme.CreateButton(_page.transform, "JournalPrevious_Button", PreviousLabel, UITheme.PanelRaised, UITheme.TextPrimary, UITheme.CaptionSize);
        UITheme.Place((RectTransform)_previous.transform, new Vector2(0.5f, 0f), new Vector2(-150f, 40f), new Vector2(180f, 58f));
        _previous.onClick.AddListener(() => Turn(-1));
        _next = UITheme.CreateButton(_page.transform, "JournalNext_Button", NextLabel, UITheme.PanelRaised, UITheme.TextPrimary, UITheme.CaptionSize);
        UITheme.Place((RectTransform)_next.transform, new Vector2(0.5f, 0f), new Vector2(330f, 40f), new Vector2(180f, 58f));
        _next.onClick.AddListener(() => Turn(1));

        // Zero's line, in a bubble beside her head (she stands at the left of the hall).
        Image bubble = UITheme.CreatePanel(transform, "ZeroBubble", UITheme.PanelRaised);
        UITheme.AddBorder(bubble, UITheme.Border, 1.5f);
        bubble.raycastTarget = false;
        UITheme.Place(bubble.rectTransform, new Vector2(0.5f, 0.5f), new Vector2(-310f, 300f), new Vector2(480f, 96f));
        _bubble = bubble.gameObject;
        _bubbleText = UITheme.CreateText(bubble.transform, "Line_Text", string.Empty, UITheme.BodySize, UITheme.TextPrimary, TextAnchor.MiddleCenter);
        UITheme.Stretch(_bubbleText.rectTransform);
        _bubbleText.rectTransform.offsetMin = new Vector2(18f, 6f);
        _bubbleText.rectTransform.offsetMax = new Vector2(-18f, -6f);
    }
}
