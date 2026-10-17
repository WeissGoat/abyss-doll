using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// The shelf in the attic (docs/specs/2026-10-17-home-loop.md, batch 4): what her wishes brought home, the milestones
// (the first boss, the first defeat, the first rent paid, the first wish) and anything the player chose to keep. A
// thing on it shows when it came and what she said. Rules live in MementoService.
public class HomeShelfView : MonoBehaviour {
    public const string RootName = "HomeShelf_Runtime";
    public const string KeepLabel = "摆一件东西上去";
    public const string KeptLine = "（零号看了它很久。）";

    private Transform _grid;
    private Text _detail;
    private Text _empty;
    private HomeItemPicker _picker;
    private readonly List<GameObject> _slots = new List<GameObject>();
    private System.Action _onChanged;

    public bool IsShowing => gameObject.activeSelf;

    public static HomeShelfView Create(Transform parent, Transform pickerParent, System.Action onChanged) {
        GameObject root = UITheme.CreateModal(parent, RootName, new Vector2(760f, 700f), 542);
        HomeShelfView view = root.AddComponent<HomeShelfView>();
        view._onChanged = onChanged;
        view._picker = HomeItemPicker.Create(pickerParent);
        view.Build(root.transform.Find("Dialog"));
        root.SetActive(false);
        return view;
    }

    public void Show() {
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Refresh();
    }

    private void Refresh() {
        foreach (GameObject slot in _slots) {
            if (slot != null) {
                if (Application.isPlaying) {
                    Destroy(slot);
                } else {
                    DestroyImmediate(slot);
                }
            }
        }
        _slots.Clear();

        List<Memento> shelf = GameRoot.Core?.CurrentPlayer?.Shelf ?? new List<Memento>();
        _empty.gameObject.SetActive(shelf.Count == 0);
        _detail.text = string.Empty;
        int start = Mathf.Max(0, shelf.Count - MementoService.ShelfSlots);
        for (int i = 0; i < MementoService.ShelfSlots; i++) {
            Memento memento = start + i < shelf.Count ? shelf[start + i] : null;
            Button slot = UITheme.CreateButton(_grid, "ShelfSlot_" + i, string.Empty, memento != null ? UITheme.PanelRaised : UITheme.Inset, UITheme.TextPrimary, UITheme.CaptionSize - 2);
            RectTransform rect = (RectTransform)slot.transform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2((i % 3 - 1) * 200f, -(i / 3) * 140f);
            rect.sizeDelta = new Vector2(184f, 128f);
            slot.interactable = memento != null;
            if (memento != null) {
                Image icon = UITheme.CreatePanel(slot.transform, "Icon_Image", Color.white);
                icon.raycastTarget = false;
                UITheme.Place(icon.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -46f), new Vector2(64f, 64f));
                VisualUIHelper.ApplyContainSprite(icon, memento.IconID, new Vector2(64f, 64f), Color.white, UITheme.Border, false);
                Text label = slot.GetComponentInChildren<Text>();
                label.text = memento.Name;
                label.alignment = TextAnchor.LowerCenter;
                label.rectTransform.offsetMin = new Vector2(6f, 8f);
                Memento picked = memento;
                slot.onClick.AddListener(() => _detail.text = $"{picked.Name} · 第 {picked.Day} 天\n「{picked.Line}」");
            }

            _slots.Add(slot.gameObject);
        }
    }

    private void OpenKeep() {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        List<ItemEntity> items = (player?.StashInventory ?? new List<ItemEntity>())
            .Where(item => item != null && (item.Tags == null || (!item.Tags.Contains("CoreMaterial") && !item.Tags.Contains("OrderBound"))))
            .OrderByDescending(item => item.BaseValue)
            .ToList();
        if (items.Count == 0) {
            PlayerToast.Show("仓库里没有能摆上去的东西。", PlayerToastKind.Info);
            return;
        }

        _picker.Show("摆上架子", "摆上去的东西留在家里，不再出售。", items, "摆上去", item => {
            MementoService.Keep(player, item, KeptLine);
            Refresh();
            _onChanged?.Invoke();
        });
    }

    private void Build(Transform dialog) {
        Text title = UITheme.CreateText(dialog, "Title_Text", "架子上的东西", UITheme.HeadingSize, UITheme.Accent, TextAnchor.MiddleCenter);
        UITheme.Place(title.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -28f), new Vector2(600f, 44f));
        GameObject grid = new GameObject("Shelf_Grid", typeof(RectTransform));
        grid.transform.SetParent(dialog, false);
        UITheme.Place((RectTransform)grid.transform, new Vector2(0.5f, 1f), new Vector2(0f, -86f), new Vector2(600f, 420f));
        ((RectTransform)grid.transform).pivot = new Vector2(0.5f, 1f);
        _grid = grid.transform;
        _empty = UITheme.CreateText(dialog, "Empty_Text", "还什么都没有。完成她的心愿，或者把舍不得卖的东西摆上来。", UITheme.CaptionSize, UITheme.TextSecondary, TextAnchor.MiddleCenter);
        UITheme.Place(_empty.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -200f), new Vector2(620f, 60f));
        _detail = UITheme.CreateText(dialog, "Detail_Text", string.Empty, UITheme.CaptionSize + 1, UITheme.TextPrimary, TextAnchor.MiddleCenter);
        UITheme.Place(_detail.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 104f), new Vector2(660f, 64f));
        Button keep = UITheme.CreateButton(dialog, "ShelfKeep_Button", KeepLabel, UITheme.PanelRaised, UITheme.TextPrimary, UITheme.CaptionSize + 1);
        UITheme.AddBorder(keep.targetGraphic, UITheme.Border, 1.5f);
        UITheme.Place((RectTransform)keep.transform, new Vector2(0.5f, 0f), new Vector2(-130f, 30f), new Vector2(230f, 56f));
        keep.onClick.AddListener(OpenKeep);
        Button close = UITheme.CreateButton(dialog, "ShelfClose_Button", "合上", UITheme.Accent, UITheme.OnAccent);
        UITheme.Place((RectTransform)close.transform, new Vector2(0.5f, 0f), new Vector2(130f, 30f), new Vector2(200f, 56f));
        close.onClick.AddListener(() => gameObject.SetActive(false));
    }
}
