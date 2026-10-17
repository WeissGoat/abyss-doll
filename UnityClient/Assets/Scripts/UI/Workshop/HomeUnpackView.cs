using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 卸货 at the counter (docs/specs/2026-10-18-home-evening-thread.md, A): what the dive brought home, one row each with
// where it goes (卖掉 / 收着 / 架子 / 给她), and Zero's word on one of them. 去摆货营业 moves on to the business panel;
// what is left undecided stays in the stash. Rules live in HomeEveningService.
public class HomeUnpackView : MonoBehaviour {
    public const string RootName = "HomeUnpack_Runtime";
    public const string BusinessLabel = "去摆货营业";
    public const string KeepAllLabel = "剩下的都收着";
    public static readonly string[] ChoiceLabels = { "卖掉", "收着", "架子", "给她" };
    private const int VisibleRows = 6;

    private Text _title;
    private Text _line;
    private Text _message;
    private Transform _list;
    private System.Action _onBusiness;
    private System.Action _onChanged;
    private readonly List<GameObject> _rows = new List<GameObject>();

    public bool IsShowing => gameObject.activeSelf;

    public static HomeUnpackView Create(Transform parent, System.Action onChanged) {
        GameObject root = UITheme.CreateModal(parent, RootName, new Vector2(1180f, 860f), 544);
        HomeUnpackView view = root.AddComponent<HomeUnpackView>();
        view._onChanged = onChanged;
        view.Build(root.transform.Find("Dialog"));
        root.SetActive(false);
        return view;
    }

    public void Show(System.Action onBusiness) {
        _onBusiness = onBusiness;
        _message.text = string.Empty;
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        List<ItemEntity> haul = HomeEveningService.Haul(player);
        string line = HomeEveningService.HaulLine(player, HomeEveningService.Remarkable(player, haul));
        _line.text = string.IsNullOrEmpty(line) ? string.Empty : $"零号：「{line}」";
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Refresh();
    }

    private void Refresh() {
        foreach (GameObject row in _rows) {
            if (row != null) {
                if (Application.isPlaying) {
                    Destroy(row);
                } else {
                    DestroyImmediate(row);
                }
            }
        }
        _rows.Clear();

        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        List<ItemEntity> haul = HomeEveningService.Haul(player);
        _title.text = haul.Count > 0 ? $"今天带回来的（还有 {haul.Count} 件）" : "今天带回来的都有去处了";
        for (int i = 0; i < haul.Count && i < VisibleRows; i++) {
            _rows.Add(CreateRow(player, haul[i], i));
        }

        if (haul.Count > VisibleRows) {
            Text more = UITheme.CreateText(_list, "More_Text", $"还有 {haul.Count - VisibleRows} 件，先处理上面的。", UITheme.CaptionSize, UITheme.TextSecondary, TextAnchor.MiddleCenter);
            StudioBenchView.PlaceRow(more.rectTransform, VisibleRows, 40f, 8f);
            _rows.Add(more.gameObject);
        }
    }

    private GameObject CreateRow(PlayerProfile player, ItemEntity item, int index) {
        bool wished = WishService.Offerable(player).Contains(item);
        Image row = UITheme.CreatePanel(_list, "Haul_" + index, wished ? UITheme.PanelRaised : UITheme.Inset);
        UITheme.AddBorder(row, wished ? UITheme.Accent : UITheme.Border, wished ? 2f : 1f);
        StudioBenchView.PlaceRow(row.rectTransform, index, 76f, 8f);

        Image icon = UITheme.CreatePanel(row.transform, "Icon_Image", Color.white);
        icon.raycastTarget = false;
        UITheme.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(14f, 0f), new Vector2(60f, 60f));
        VisualUIHelper.ApplyContainSprite(icon, VisualAssetService.ResolveItemIconID(item), new Vector2(60f, 60f), Color.white, UITheme.Border, false);
        Text name = UITheme.CreateText(row.transform, "Name_Text", item.Name, UITheme.BodySize - 2, UITheme.TextPrimary);
        name.rectTransform.anchorMin = new Vector2(0f, 0.5f);
        name.rectTransform.anchorMax = new Vector2(1f, 1f);
        name.rectTransform.offsetMin = new Vector2(88f, 0f);
        name.rectTransform.offsetMax = new Vector2(-470f, -6f);
        Text detail = UITheme.CreateText(row.transform, "Detail_Text", $"{ItemPresentationRules.TypeLabel(item)} · 估值 {item.BaseValue}G{(wished ? " · 她想要的" : string.Empty)}", UITheme.CaptionSize - 2, wished ? UITheme.Accent : UITheme.TextSecondary);
        detail.rectTransform.anchorMin = new Vector2(0f, 0f);
        detail.rectTransform.anchorMax = new Vector2(1f, 0.5f);
        detail.rectTransform.offsetMin = new Vector2(88f, 6f);
        detail.rectTransform.offsetMax = new Vector2(-470f, 0f);

        List<HaulChoice> choices = HomeEveningService.Choices(player, item);
        HaulChoice[] order = { HaulChoice.Give, HaulChoice.Shelve, HaulChoice.Keep, HaulChoice.Sell };
        int slot = 0;
        foreach (HaulChoice choice in order) {
            if (!choices.Contains(choice)) {
                continue;
            }

            bool primary = choice == HaulChoice.Sell || choice == HaulChoice.Give;
            Button button = UITheme.CreateButton(row.transform, "Choice_" + choice, ChoiceLabels[(int)choice], primary ? UITheme.Accent : UITheme.PanelRaised, primary ? UITheme.OnAccent : UITheme.TextPrimary, UITheme.CaptionSize);
            if (!primary) {
                UITheme.AddBorder(button.targetGraphic, UITheme.Border, 1f);
            }

            UITheme.Place((RectTransform)button.transform, new Vector2(1f, 0.5f), new Vector2(-14f - slot * 112f, 0f), new Vector2(100f, 50f));
            HaulChoice picked = choice;
            button.onClick.AddListener(() => Decide(item, picked));
            slot++;
        }

        return row.gameObject;
    }

    private void Decide(ItemEntity item, HaulChoice choice) {
        bool done = HomeEveningService.Decide(GameRoot.Core?.CurrentPlayer, item, choice, out string message, out WishResult wish);
        _message.text = message;
        if (wish != null) {
            HomeWishView.AnnounceWish(wish);
        }

        if (done && choice == HaulChoice.Sell) {
            GameAudio.PlaySfx("coin");
        }

        Refresh();
        _onChanged?.Invoke();
    }

    private void KeepAll() {
        int count = HomeEveningService.KeepAll(GameRoot.Core?.CurrentPlayer);
        _message.text = count > 0 ? $"{count} 件收进了仓库。" : string.Empty;
        Refresh();
        _onChanged?.Invoke();
    }

    private void GoBusiness() {
        gameObject.SetActive(false);
        System.Action next = _onBusiness;
        _onBusiness = null;
        next?.Invoke();
    }

    private void Build(Transform dialog) {
        _title = UITheme.CreateText(dialog, "Title_Text", string.Empty, UITheme.HeadingSize, UITheme.Accent, TextAnchor.MiddleCenter);
        UITheme.Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(1000f, 44f));
        _line = UITheme.CreateText(dialog, "ZeroLine_Text", string.Empty, UITheme.BodySize - 1, UITheme.TextPrimary, TextAnchor.MiddleCenter);
        UITheme.Place(_line.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -76f), new Vector2(1060f, 36f));
        Text hint = UITheme.CreateText(dialog, "Hint_Text", "卖掉：摆进橱窗（满了进倾倒箱），营业时还能调。收着：留在仓库。架子：摆上阁楼，不再出售。", UITheme.CaptionSize - 2, UITheme.TextSecondary, TextAnchor.MiddleCenter);
        UITheme.Place(hint.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -112f), new Vector2(1080f, 28f));

        GameObject list = new GameObject("Haul_List", typeof(RectTransform));
        list.transform.SetParent(dialog, false);
        RectTransform listRect = (RectTransform)list.transform;
        listRect.anchorMin = Vector2.zero;
        listRect.anchorMax = Vector2.one;
        listRect.offsetMin = new Vector2(36f, 130f);
        listRect.offsetMax = new Vector2(-36f, -150f);
        _list = list.transform;

        _message = UITheme.CreateText(dialog, "Message_Text", string.Empty, UITheme.CaptionSize, UITheme.Warning, TextAnchor.MiddleCenter);
        UITheme.Place(_message.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 96f), new Vector2(1000f, 28f));
        Button keepAll = UITheme.CreateButton(dialog, "KeepAll_Button", KeepAllLabel, UITheme.PanelRaised, UITheme.TextPrimary);
        UITheme.AddBorder(keepAll.targetGraphic, UITheme.Border, 1.5f);
        UITheme.Place((RectTransform)keepAll.transform, new Vector2(0.5f, 0f), new Vector2(-160f, 28f), new Vector2(260f, 58f));
        keepAll.onClick.AddListener(KeepAll);
        Button business = UITheme.CreateButton(dialog, "GoBusiness_Button", BusinessLabel, UITheme.Accent, UITheme.OnAccent);
        UITheme.Place((RectTransform)business.transform, new Vector2(0.5f, 0f), new Vector2(160f, 28f), new Vector2(260f, 58f));
        business.onClick.AddListener(GoBusiness);
    }
}
