using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UI;

// 今晚营业 (GDD_04 three steps, Demo cut): stash on the left, the showcase grid in the middle, the dump box on the
// right. At night it ends with 开始营业; during the day it is the same screen for arranging (营业准备), closed with
// 关闭. Rules live in NightBusinessService / DayCycleService; this only shows state and forwards clicks.
public class NightBusinessPanel : MonoBehaviour {
    public const string PanelName = "NightBusinessPanel_Runtime";
    public const string OpenLabel = "开始营业";
    public const string DumpAllLabel = "一键倾倒战利品";
    private const float Cell = 92f;

    private Text _title;
    private Text _hint;
    private Text _totals;
    private Text _message;
    private Transform _stashList;
    private Transform _dumpList;
    private RectTransform _showcaseRoot;
    private Button _openButton;
    private Button _closeButton;
    private Button _dumpAllButton;
    private bool _nightMode;
    private Action _onOpened;
    private Action _onClosed;
    private readonly List<GameObject> _dynamic = new List<GameObject>();

    public bool IsNightMode => _nightMode;

    public static NightBusinessPanel Create(Transform parent) {
        GameObject root = UITheme.CreateModal(parent, PanelName, new Vector2(1640f, 900f), 545);
        NightBusinessPanel panel = root.AddComponent<NightBusinessPanel>();
        panel.Build(root.transform.Find("Dialog"));
        root.SetActive(false);
        return panel;
    }

    // night: the day's business, ends with 开始营业 (onOpened); 先不开张 goes back to the town, still at night.
    // Otherwise daytime arranging, closed with 关闭.
    public void Show(bool night, Action onOpened, Action onClosed) {
        _nightMode = night;
        _onOpened = onOpened;
        _onClosed = onClosed;
        _message.text = string.Empty;
        _openButton.gameObject.SetActive(night);
        UITheme.SetButtonLabel(_closeButton, night ? "先不开张" : "关闭");
        ((RectTransform)_dumpAllButton.transform).anchoredPosition = new Vector2(night ? -340f : -170f, 22f);
        ((RectTransform)_closeButton.transform).anchoredPosition = new Vector2(night ? 340f : 170f, 22f);
        gameObject.SetActive(true);
        transform.SetAsLastSibling();
        Refresh();
    }

    public void Hide() {
        gameObject.SetActive(false);
    }

    private void Refresh() {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        if (player == null) {
            return;
        }

        _title.text = _nightMode ? $"第 {player.CurrentDay} 天 · 夜晚 · 今晚营业" : "营业准备（白天摆好，夜里开张）";
        _hint.text = "点仓库里的东西放进倾倒箱（八折，快速清货）或橱窗（溢价，相邻同类再加价）；点已经摆上的东西取回。";

        foreach (GameObject obj in _dynamic) {
            if (obj != null) {
                obj.SetActive(false);
                Destroy(obj);
            }
        }
        _dynamic.Clear();

        List<BusinessLine> lines = NightBusinessService.Preview(player);
        BuildStashRows(player);
        BuildDumpRows(lines.Where(line => line.Channel == BusinessChannel.DumpBox).ToList());
        BuildShowcase(player, lines.Where(line => line.Channel == BusinessChannel.Showcase).ToList());

        int dump = lines.Where(line => line.Channel == BusinessChannel.DumpBox).Sum(line => line.Value);
        int showcase = lines.Where(line => line.Channel == BusinessChannel.Showcase).Sum(line => line.Value);
        _totals.text = lines.Count == 0
            ? "今晚还没有摆货：开张也只是一张 0G 的账单。"
            : $"预计收入：倾倒箱 {dump}G + 橱窗 {showcase}G = {dump + showcase}G";
    }

    private void BuildStashRows(PlayerProfile player) {
        List<ItemEntity> items = player.StashInventory.Where(NightBusinessService.CanSell).OrderBy(item => item.ItemType == nameof(ItemType.Loot) ? 0 : 1).ThenByDescending(item => item.BaseValue).ToList();
        if (items.Count == 0) {
            AddRowText(_stashList, "仓库里没有能卖的东西。");
        }

        foreach (ItemEntity item in items) {
            Image row = CreateRow(_stashList, item, $"{item.Name}\n{TypeName(item)} · 基价 {item.BaseValue}G");
            Button dump = UITheme.CreateButton(row.transform, "Dump_Button", "倾倒", UITheme.PanelRaised, UITheme.TextPrimary, UITheme.CaptionSize);
            UITheme.Place((RectTransform)dump.transform, new Vector2(1f, 0.5f), new Vector2(-104f, 0f), new Vector2(80f, 44f));
            dump.onClick.AddListener(() => Move(item, false));
            Button show = UITheme.CreateButton(row.transform, "Showcase_Button", "橱窗", UITheme.Accent, UITheme.OnAccent, UITheme.CaptionSize);
            UITheme.Place((RectTransform)show.transform, new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(80f, 44f));
            show.onClick.AddListener(() => Move(item, true));
        }
    }

    private void BuildDumpRows(List<BusinessLine> lines) {
        if (lines.Count == 0) {
            AddRowText(_dumpList, "倾倒箱是空的。");
        }

        foreach (BusinessLine line in lines) {
            Image row = CreateRow(_dumpList, line.Item, $"{line.Item.Name}\n{line.Value}G");
            Button back = UITheme.CreateButton(row.transform, "Return_Button", "取回", UITheme.PanelRaised, UITheme.TextPrimary, UITheme.CaptionSize);
            UITheme.Place((RectTransform)back.transform, new Vector2(1f, 0.5f), new Vector2(-12f, 0f), new Vector2(80f, 44f));
            ItemEntity item = line.Item;
            back.onClick.AddListener(() => Return(item));
        }
    }

    private void BuildShowcase(PlayerProfile player, List<BusinessLine> lines) {
        BackpackGrid grid = NightBusinessService.BuildShowcaseGrid(player);
        _showcaseRoot.sizeDelta = new Vector2(grid.Width * Cell, grid.Height * Cell);
        for (int y = 0; y < grid.Height; y++) {
            for (int x = 0; x < grid.Width; x++) {
                Image cell = UITheme.CreatePanel(_showcaseRoot, "Cell", UITheme.Inset);
                UITheme.AddBorder(cell, UITheme.Border, 1f);
                cell.raycastTarget = false;
                UITheme.Place(cell.rectTransform, new Vector2(0f, 1f), new Vector2(x * Cell + 2f, -y * Cell - 2f), new Vector2(Cell - 4f, Cell - 4f));
                _dynamic.Add(cell.gameObject);
            }
        }

        foreach (BusinessLine line in lines) {
            ItemEntity item = line.Item;
            if (!BackpackGrid.TryGetRotatedBounds(item, item.Grid.Rotation, out int columns, out int rows)) {
                continue;
            }

            string bonus = line.AdjacentMatches > 0 ? $"\n相邻 +{line.AdjacentMatches}" : string.Empty;
            Button block = UITheme.CreateButton(_showcaseRoot, "ShowcaseItem_" + item.InstanceID, $"{item.Name}\n{line.Value}G{bonus}", ResolveTint(item), UITheme.TextPrimary, UITheme.CaptionSize - 2);
            UITheme.AddBorder(block.targetGraphic, line.AdjacentMatches > 0 ? UITheme.Accent : UITheme.Border, line.AdjacentMatches > 0 ? 2.5f : 1f);
            UITheme.Place((RectTransform)block.transform, new Vector2(0f, 1f), new Vector2(item.Grid.CurrentPos[0] * Cell + 4f, -item.Grid.CurrentPos[1] * Cell - 4f), new Vector2(columns * Cell - 8f, rows * Cell - 8f));
            block.onClick.AddListener(() => Return(item));
            _dynamic.Add(block.gameObject);
        }
    }

    private Image CreateRow(Transform list, ItemEntity item, string label) {
        Image row = UITheme.CreatePanel(list, "Row_" + item.InstanceID, UITheme.PanelRaised);
        row.gameObject.AddComponent<LayoutElement>().preferredHeight = 64f;
        Image icon = UITheme.CreatePanel(row.transform, "Icon_Image", Color.white);
        icon.raycastTarget = false;
        UITheme.Place(icon.rectTransform, new Vector2(0f, 0.5f), new Vector2(8f, 0f), new Vector2(48f, 48f));
        VisualUIHelper.ApplyContainSprite(icon, VisualAssetService.ResolveItemIconID(item), new Vector2(48f, 48f), Color.white, ResolveTint(item));
        Text text = UITheme.CreateText(row.transform, "Label_Text", label, UITheme.CaptionSize - 1, UITheme.TextPrimary);
        text.rectTransform.anchorMin = Vector2.zero;
        text.rectTransform.anchorMax = Vector2.one;
        text.rectTransform.offsetMin = new Vector2(64f, 2f);
        text.rectTransform.offsetMax = new Vector2(-196f, -2f);
        _dynamic.Add(row.gameObject);
        return row;
    }

    private void AddRowText(Transform list, string content) {
        Text text = UITheme.CreateText(list, "Empty_Text", content, UITheme.CaptionSize, UITheme.TextMuted);
        text.gameObject.AddComponent<LayoutElement>().preferredHeight = 48f;
        _dynamic.Add(text.gameObject);
    }

    private void Move(ItemEntity item, bool showcase) {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        string reason;
        bool moved = showcase
            ? NightBusinessService.TryMoveToShowcase(player, item, out reason)
            : NightBusinessService.TryMoveToDumpBox(player, item, out reason);
        _message.text = moved ? string.Empty : reason;
        Refresh();
    }

    private void Return(ItemEntity item) {
        NightBusinessService.TryReturnToStash(GameRoot.Core?.CurrentPlayer, item);
        _message.text = string.Empty;
        Refresh();
    }

    private void DumpAll() {
        int count = NightBusinessService.DumpAllLoot(GameRoot.Core?.CurrentPlayer);
        _message.text = count > 0 ? $"倒进倾倒箱 {count} 件战利品。" : "仓库里没有可以倾倒的战利品。";
        Refresh();
    }

    private void OpenBusiness() {
        Hide();
        _onOpened?.Invoke();
    }

    private void Close() {
        Hide();
        _onClosed?.Invoke();
    }

    private static string TypeName(ItemEntity item) {
        switch (item?.ItemType) {
            case "Weapon": return "武器";
            case "Armor": return "防具";
            case "Consumable": return "消耗品";
            case "Loot": return NightBusinessService.IsContraband(item) ? "战利品 · 违禁" : "战利品";
            default: return "其他";
        }
    }

    private static Color ResolveTint(ItemEntity item) {
        switch (item?.ItemType) {
            case "Weapon": return new Color(0.45f, 0.16f, 0.16f, 1f);
            case "Armor": return new Color(0.2f, 0.27f, 0.42f, 1f);
            case "Consumable": return new Color(0.14f, 0.36f, 0.2f, 1f);
            default: return new Color(0.42f, 0.33f, 0.12f, 1f);
        }
    }

    private void Build(Transform dialog) {
        _title = UITheme.CreateText(dialog, "Title_Text", string.Empty, UITheme.HeadingSize, UITheme.Accent);
        UITheme.Place(_title.rectTransform, new Vector2(0f, 1f), new Vector2(32f, -18f), new Vector2(900f, 44f));
        _hint = UITheme.CreateText(dialog, "Hint_Text", string.Empty, UITheme.CaptionSize, UITheme.TextSecondary);
        UITheme.Place(_hint.rectTransform, new Vector2(0f, 1f), new Vector2(32f, -64f), new Vector2(1560f, 30f));

        _stashList = CreateColumn(dialog, "仓库", new Vector2(32f, -110f), new Vector2(560f, 620f));
        _dumpList = CreateColumn(dialog, "倾倒箱（八折）", new Vector2(1048f, -110f), new Vector2(560f, 620f));

        Text showcaseTitle = UITheme.CreateText(dialog, "Showcase_Title", "橱窗（溢价，相邻同类加价）", UITheme.BodySize, UITheme.TextPrimary, TextAnchor.MiddleCenter);
        UITheme.Place(showcaseTitle.rectTransform, new Vector2(0.5f, 1f), new Vector2(0f, -110f), new Vector2(420f, 34f));
        GameObject showcase = new GameObject("Showcase_Grid", typeof(RectTransform));
        showcase.transform.SetParent(dialog, false);
        _showcaseRoot = (RectTransform)showcase.transform;
        UITheme.Place(_showcaseRoot, new Vector2(0.5f, 1f), new Vector2(0f, -156f), new Vector2(4 * Cell, 4 * Cell));

        _totals = UITheme.CreateText(dialog, "Totals_Text", string.Empty, UITheme.BodySize, UITheme.Accent, TextAnchor.MiddleCenter);
        UITheme.Place(_totals.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 120f), new Vector2(1200f, 36f));
        _message = UITheme.CreateText(dialog, "Message_Text", string.Empty, UITheme.CaptionSize, UITheme.Warning, TextAnchor.MiddleCenter);
        UITheme.Place(_message.rectTransform, new Vector2(0.5f, 0f), new Vector2(0f, 88f), new Vector2(1200f, 28f));

        _dumpAllButton = UITheme.CreateButton(dialog, "DumpAll_Button", DumpAllLabel, UITheme.PanelRaised, UITheme.TextPrimary);
        UITheme.AddBorder(_dumpAllButton.targetGraphic, UITheme.Border, 1.5f);
        UITheme.Place((RectTransform)_dumpAllButton.transform, new Vector2(0.5f, 0f), new Vector2(-170f, 22f), new Vector2(280f, 60f));
        _dumpAllButton.onClick.AddListener(DumpAll);

        _openButton = UITheme.CreateButton(dialog, "OpenBusiness_Button", OpenLabel, UITheme.Accent, UITheme.OnAccent, UITheme.HeadingSize - 2);
        UITheme.Place((RectTransform)_openButton.transform, new Vector2(0.5f, 0f), new Vector2(0f, 22f), new Vector2(280f, 60f));
        _openButton.onClick.AddListener(OpenBusiness);

        _closeButton = UITheme.CreateButton(dialog, "CloseBusiness_Button", "关闭", UITheme.PanelRaised, UITheme.TextPrimary);
        UITheme.AddBorder(_closeButton.targetGraphic, UITheme.Border, 1.5f);
        UITheme.Place((RectTransform)_closeButton.transform, new Vector2(0.5f, 0f), new Vector2(170f, 22f), new Vector2(280f, 60f));
        _closeButton.onClick.AddListener(Close);
    }

    private Transform CreateColumn(Transform dialog, string title, Vector2 topLeft, Vector2 size) {
        Text header = UITheme.CreateText(dialog, title + "_Title", title, UITheme.BodySize, UITheme.TextPrimary);
        UITheme.Place(header.rectTransform, new Vector2(0f, 1f), topLeft, new Vector2(size.x, 34f));

        Image frame = UITheme.CreatePanel(dialog, title + "_Frame", UITheme.Inset);
        UITheme.AddBorder(frame, UITheme.Border, 1f);
        UITheme.Place(frame.rectTransform, new Vector2(0f, 1f), topLeft + new Vector2(0f, -40f), size - new Vector2(0f, 40f));
        frame.gameObject.AddComponent<RectMask2D>();

        GameObject content = new GameObject("Content", typeof(RectTransform));
        content.transform.SetParent(frame.transform, false);
        RectTransform contentRect = (RectTransform)content.transform;
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.padding = new RectOffset(8, 8, 8, 8);
        layout.spacing = 6f;
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = frame.gameObject.AddComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.viewport = frame.rectTransform;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        return content.transform;
    }
}
