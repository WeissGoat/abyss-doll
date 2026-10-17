using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// Zero's wish in the hall (docs/specs/2026-10-17-home-loop.md, batch 4): a thought bubble beside her with the wish and
// how to grant it. For a wish she wants an item for, clicking the bubble opens the stash things she would take;
// handing one over puts it on the attic shelf. Rules live in WishService.
public class HomeWishView : MonoBehaviour {
    public const string RootName = "HomeWish_Runtime";
    public const string GiveLabel = "给她";

    private Button _bubble;
    private Text _wishText;
    private Text _hintText;
    private HomeItemPicker _picker;
    private System.Action _onChanged;

    public static HomeWishView Create(Transform parent, Transform pickerParent, System.Action onChanged) {
        GameObject root = new GameObject(RootName, typeof(RectTransform));
        root.transform.SetParent(parent, false);
        UITheme.Stretch((RectTransform)root.transform);
        HomeWishView view = root.AddComponent<HomeWishView>();
        view._onChanged = onChanged;
        view._picker = HomeItemPicker.Create(pickerParent);
        view.Build();
        return view;
    }

    public void Refresh(bool hidden) {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        WishConfig wish = WishService.Active(player);
        bool show = wish != null && !hidden;
        _bubble.gameObject.SetActive(show);
        if (!show) {
            return;
        }

        _wishText.text = $"（{wish.Text}）";
        bool bring = wish.Kind == "BringTag";
        int offerable = bring ? WishService.Offerable(player).Count : 0;
        _hintText.text = bring
            ? (offerable > 0 ? $"仓库里有 {offerable} 件她会喜欢的，点这里交给她" : wish.Hint)
            : wish.Hint;
        _hintText.color = bring && offerable > 0 ? UITheme.Accent : UITheme.TextSecondary;
    }

    private void OpenGift() {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        WishConfig wish = WishService.Active(player);
        if (wish == null) {
            return;
        }

        if (wish.Kind != "BringTag") {
            PlayerToast.Show($"零号的心愿：{wish.Text}。{wish.Hint}。", PlayerToastKind.Info);
            return;
        }

        List<ItemEntity> items = WishService.Offerable(player);
        if (items.Count == 0) {
            PlayerToast.Show($"仓库里还没有她想要的。{wish.Hint}。", PlayerToastKind.Info);
            return;
        }

        _picker.Show($"{wish.Text}", "交给她的东西会放上阁楼的架子，不再出售。", items, GiveLabel, item => {
            WishResult result = WishService.Give(player, item);
            if (result != null) {
                AnnounceWish(result);
            }

            _onChanged?.Invoke();
        });
    }

    public static void AnnounceWish(WishResult result) {
        if (result?.Wish == null) {
            return;
        }

        GameAudio.PlaySfx("coin");
        PlayerToast.Show($"零号：「{result.Wish.DoneLine}」（羁绊 +{WishService.BondReward}，架子上多了一样东西）", PlayerToastKind.Success);
        StudioCareView.AnnounceBond(result.Bond);
    }

    private void Build() {
        _bubble = UITheme.CreateButton(transform, "WishBubble", string.Empty, UITheme.PanelRaised, UITheme.TextPrimary);
        UITheme.AddBorder(_bubble.targetGraphic, UITheme.Border, 1.5f);
        UITheme.Place((RectTransform)_bubble.transform, new Vector2(0.5f, 0.5f), new Vector2(-300f, 300f), new Vector2(460f, 96f));
        Text label = _bubble.GetComponentInChildren<Text>();
        label.gameObject.SetActive(false);
        _wishText = UITheme.CreateText(_bubble.transform, "Wish_Text", string.Empty, UITheme.BodySize, UITheme.TextPrimary, TextAnchor.UpperCenter);
        UITheme.Stretch(_wishText.rectTransform);
        _wishText.rectTransform.offsetMin = new Vector2(14f, 0f);
        _wishText.rectTransform.offsetMax = new Vector2(-14f, -12f);
        _wishText.raycastTarget = false;
        _hintText = UITheme.CreateText(_bubble.transform, "WishHint_Text", string.Empty, UITheme.CaptionSize - 2, UITheme.TextSecondary, TextAnchor.LowerCenter);
        UITheme.Stretch(_hintText.rectTransform);
        _hintText.rectTransform.offsetMin = new Vector2(14f, 12f);
        _hintText.rectTransform.offsetMax = new Vector2(-14f, 0f);
        _hintText.raycastTarget = false;
        _bubble.onClick.AddListener(OpenGift);
        _bubble.gameObject.SetActive(false);
    }
}

// A small list of stash items with one action each (give to Zero, put on the shelf).
public class HomeItemPicker : MonoBehaviour {
    public const string RootName = "HomeItemPicker_Runtime";

    private Text _title;
    private Text _note;
    private Transform _list;
    private readonly List<GameObject> _rows = new List<GameObject>();

    public bool IsShowing => gameObject.activeSelf;

    public static HomeItemPicker Create(Transform parent) {
        GameObject root = UITheme.CreateModal(parent, RootName, new Vector2(680f, 640f), 545);
        HomeItemPicker picker = root.AddComponent<HomeItemPicker>();
        picker.Build(root.transform.Find("Dialog"));
        root.SetActive(false);
        return picker;
    }

    public void Show(string title, string note, List<ItemEntity> items, string action, System.Action<ItemEntity> onPick) {
        _title.text = title;
        _note.text = note;
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

        for (int i = 0; i < items.Count && i < 6; i++) {
            ItemEntity item = items[i];
            Image row = UITheme.CreatePanel(_list, "Pick_" + i, UITheme.PanelRaised);
            StudioBenchView.PlaceRow(row.rectTransform, i, 64f, 8f);
            Image icon = UITheme.CreatePanel(row.transform, "Icon_Image", Color.white);
            UITheme.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(40f, 0f), new Vector2(52f, 52f));
            VisualUIHelper.ApplyContainSprite(icon, VisualAssetService.ResolveItemIconID(item), new Vector2(52f, 52f), Color.white, UITheme.Border, false);
            Text name = UITheme.CreateText(row.transform, "Name_Text", $"{item.Name}    估值 {item.BaseValue}G", UITheme.CaptionSize, UITheme.TextPrimary);
            name.rectTransform.anchorMin = Vector2.zero;
            name.rectTransform.anchorMax = Vector2.one;
            name.rectTransform.offsetMin = new Vector2(80f, 0f);
            name.rectTransform.offsetMax = new Vector2(-140f, 0f);
            Button pick = UITheme.CreateButton(row.transform, "Pick_Button", action, UITheme.Accent, UITheme.OnAccent, UITheme.CaptionSize);
            UITheme.Place((RectTransform)pick.transform, new Vector2(1f, 0.5f), new Vector2(-66f, 0f), new Vector2(112f, 48f));
            pick.onClick.AddListener(() => {
                gameObject.SetActive(false);
                onPick(item);
            });
            _rows.Add(row.gameObject);
        }

        gameObject.SetActive(true);
        transform.SetAsLastSibling();
    }

    private void Build(Transform dialog) {
        _title = UITheme.CreateText(dialog, "Title_Text", string.Empty, UITheme.HeadingSize, UITheme.Accent, TextAnchor.MiddleCenter);
        UITheme.Place(_title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -26f), new Vector2(600f, 44f));
        _note = UITheme.CreateText(dialog, "Note_Text", string.Empty, UITheme.CaptionSize, UITheme.TextSecondary, TextAnchor.MiddleCenter);
        UITheme.Place(_note.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -72f), new Vector2(600f, 30f));
        GameObject list = new GameObject("Pick_List", typeof(RectTransform));
        list.transform.SetParent(dialog, false);
        RectTransform listRect = (RectTransform)list.transform;
        listRect.anchorMin = Vector2.zero;
        listRect.anchorMax = Vector2.one;
        listRect.offsetMin = new Vector2(36f, 100f);
        listRect.offsetMax = new Vector2(-36f, -112f);
        _list = list.transform;
        Button close = UITheme.CreateButton(dialog, "PickClose_Button", "算了", UITheme.PanelRaised, UITheme.TextPrimary);
        UITheme.AddBorder(close.targetGraphic, UITheme.Border, 1.5f);
        UITheme.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(0f, 30f), new Vector2(200f, 56f));
        close.onClick.AddListener(() => gameObject.SetActive(false));
    }
}
