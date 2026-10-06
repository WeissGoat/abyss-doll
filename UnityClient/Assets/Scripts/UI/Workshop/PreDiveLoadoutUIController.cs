using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

// 出发前整备 screen (docs/specs/2026-10-04-pre-dive-loadout.md): the backpack grid on the left (shown by
// InventoryPresentationController in Loadout mode), the stash on the right. Rules live in PreDiveLoadoutService;
// this class only shows state and forwards clicks.
public class PreDiveLoadoutUIController : MonoBehaviour {
    public const string PanelName = "PreDiveLoadoutPanel_Runtime";

    private static readonly Color TextColor = UITheme.TextPrimary;
    private static readonly Color MutedTextColor = UITheme.TextSecondary;
    private static readonly Color RowColor = UITheme.PanelRaised;

    public static PreDiveLoadoutUIController Active { get; private set; }

    private Font _font;
    private Text _titleText;
    private Text _statusText;
    private Text _messageText;
    private Transform _listContent;
    private GameObject _confirmRoot;
    private Text _confirmText;
    private WorkshopShopPanel _shopPanel;
    private int _layerID = 1;
    private Action _onBack;
    private Func<int, bool> _onDepart;
    private readonly List<GameObject> _rows = new List<GameObject>();

    public int LayerID => _layerID;

    public static PreDiveLoadoutUIController Create(Transform canvas) {
        GameObject root = new GameObject(PanelName, typeof(RectTransform));
        root.transform.SetParent(canvas, false);
        PreDiveLoadoutUIController controller = root.AddComponent<PreDiveLoadoutUIController>();
        controller.Build();
        root.SetActive(false);
        return controller;
    }

    // onDepart starts the run and returns whether it started.
    public void Present(int layerID, Action onBack, Func<int, bool> onDepart) {
        _layerID = layerID;
        _onBack = onBack;
        _onDepart = onDepart;
        Active = this;
        transform.SetAsFirstSibling();
        _confirmRoot.SetActive(false);
        _shopPanel?.Hide();
        _messageText.text = string.Empty;
        Refresh();
    }

    public void OpenShop() {
        _confirmRoot.SetActive(false);
        _shopPanel.Show(Refresh);
    }

    // Called by DraggableItemUI when the player clicks a backpack item on this screen.
    public bool HandleBackpackItemClicked(ItemEntity item) {
        if (!isActiveAndEnabled || item == null) {
            return false;
        }

        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        bool returned = PreDiveLoadoutService.TryReturnToStash(player, item, out string reason);
        _messageText.text = returned ? $"{item.Name} 放回了仓库。" : reason;
        Refresh();
        return true;
    }

    private void OnDisable() {
        if (Active == this) {
            Active = null;
        }
    }

    private void OnEnable() {
        Active = this;
    }

    public void Refresh() {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        if (player == null) {
            return;
        }

        string layerName = ConfigManager.Dungeons.TryGetValue(_layerID, out DungeonConfig layer) ? layer.Name : string.Empty;
        _titleText.text = $"出发前整备 · 第 {_layerID} 层 {layerName}";
        _statusText.text = $"第 {player.CurrentDay} 天 · 资金 {player.Money}G · 从仓库挑要带下去的东西，整备不花时间。";
        RebuildStashList(player);
    }

    private void RebuildStashList(PlayerProfile player) {
        foreach (GameObject row in _rows) {
            if (row != null) {
                row.SetActive(false);
                Destroy(row);
            }
        }
        _rows.Clear();

        int total = 0;
        AddGroup(player, LoadoutStashGroup.Gear, "装备", ref total);
        AddGroup(player, LoadoutStashGroup.Consumable, "消耗品", ref total);
        AddGroup(player, LoadoutStashGroup.LootAndMaterial, "战利品与材料", ref total);
        if (total == 0) {
            _rows.Add(CreateLabel(_listContent, "Empty_Text", "仓库是空的。", 20, MutedTextColor, TextAnchor.MiddleLeft, 60f).gameObject);
        }
    }

    private void AddGroup(PlayerProfile player, LoadoutStashGroup group, string title, ref int total) {
        List<ItemEntity> items = PreDiveLoadoutService.GetStashItems(player, group);
        if (items.Count == 0) {
            return;
        }

        total += items.Count;
        _rows.Add(CreateLabel(_listContent, "Group_" + group, $"{title}（{items.Count}）", 22, UITheme.Accent, TextAnchor.LowerLeft, 44f).gameObject);
        foreach (ItemEntity item in items) {
            _rows.Add(CreateStashRow(item));
        }
    }

    private GameObject CreateStashRow(ItemEntity item) {
        GameObject row = new GameObject("StashRow_" + item.InstanceID, typeof(RectTransform));
        row.transform.SetParent(_listContent, false);
        row.AddComponent<LayoutElement>().preferredHeight = 76f;
        Image background = row.AddComponent<Image>();
        background.color = RowColor;
        Button button = row.AddComponent<Button>();
        button.targetGraphic = background;
        button.onClick.AddListener(() => HandleStashItemClicked(item));

        HorizontalLayoutGroup layout = row.AddComponent<HorizontalLayoutGroup>();
        layout.childAlignment = TextAnchor.MiddleLeft;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
        layout.padding = new RectOffset(12, 12, 8, 8);
        layout.spacing = 14f;

        GameObject iconObj = new GameObject("ItemIcon_Image", typeof(RectTransform));
        iconObj.transform.SetParent(row.transform, false);
        Image icon = iconObj.AddComponent<Image>();
        icon.raycastTarget = false;
        VisualUIHelper.ApplyContainSprite(icon, VisualAssetService.ResolveItemIconID(item), new Vector2(56f, 56f), Color.white, ResolveItemTint(item));

        string size = BackpackGrid.TryGetRotatedBounds(item, item.Grid?.Rotation ?? 0, out int columns, out int rows)
            ? $"{columns}×{rows}"
            : "?";
        Text label = CreateLabel(row.transform, "ItemLabel_Text", $"{item.Name}\n{ResolveTypeLabel(item)} · 占 {size} 格 · {item.BaseValue}G", 19, TextColor, TextAnchor.MiddleLeft, 60f);
        label.rectTransform.sizeDelta = new Vector2(560f, 60f);
        return row;
    }

    private void HandleStashItemClicked(ItemEntity item) {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        bool brought = PreDiveLoadoutService.TryBringFromStash(player, item, out string reason);
        _messageText.text = brought ? $"{item.Name} 放进了背包。" : reason;
        Refresh();
    }

    private void HandleRestoreClicked() {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        if (player?.LastDiveLoadout == null || player.LastDiveLoadout.Count == 0) {
            _messageText.text = "还没有出发过，没有上次的装备。";
            return;
        }

        LoadoutRestoreReport report = PreDiveLoadoutService.RestoreLastLoadout(player);
        if (report.PlacedCount == 0 && report.DidNotFit.Count == 0) {
            _messageText.text = "上次的装备都不在仓库里了。";
        } else {
            _messageText.text = report.DidNotFit.Count == 0
                ? $"带上了上次的 {report.PlacedCount} 件装备。"
                : $"带上了 {report.PlacedCount} 件；放不下：{string.Join("、", report.DidNotFit)}。";
        }
        Refresh();
    }

    private void HandleDepartClicked() {
        if (!PreDiveLoadoutService.HasWeapon(GameRoot.Core?.CurrentPlayer)) {
            DollEntity doll = GameRoot.Core?.CurrentPlayer?.ActiveDoll;
            _confirmText.text = $"背包里没有武器，只能徒手攻击（每次 {ItemUseService.GetUnarmedDamage(doll)} 伤害）。\n补给铺可以买到基础武器。";
            _confirmRoot.SetActive(true);
            return;
        }

        Depart();
    }

    private void Depart() {
        _confirmRoot.SetActive(false);
        if (_onDepart != null && !_onDepart(_layerID)) {
            _messageText.text = "现在不能出发，回选层界面看看原因。";
        }
    }

    private void Build() {
        _font = UITheme.Font;
        Stretch((RectTransform)transform);
        Image backdrop = gameObject.AddComponent<Image>();
        backdrop.color = UITheme.Background;

        _titleText = CreateLabel(transform, "Title_Text", string.Empty, 36, UITheme.TextOnDark, TextAnchor.MiddleLeft, 56f);
        PlaceTopLeft(_titleText.rectTransform, new Vector2(60f, -36f), new Vector2(1100f, 56f));
        _statusText = CreateLabel(transform, "Status_Text", string.Empty, 20, UITheme.TextOnDarkMuted, TextAnchor.MiddleLeft, 32f);
        PlaceTopLeft(_statusText.rectTransform, new Vector2(60f, -96f), new Vector2(1100f, 32f));

        Text backpackCaption = CreateLabel(transform, "BackpackCaption_Text", "背包：左键放回仓库，右键旋转，拖动调整位置", 20, UITheme.TextOnDarkMuted, TextAnchor.MiddleCenter, 32f);
        PlaceCenter(backpackCaption.rectTransform, new Vector2(-420f, 250f), new Vector2(640f, 32f));

        GameObject card = new GameObject("StashCard", typeof(RectTransform));
        card.transform.SetParent(transform, false);
        PlaceCenter((RectTransform)card.transform, new Vector2(430f, -30f), new Vector2(820f, 800f));
        Image cardImage = card.AddComponent<Image>();
        cardImage.color = UITheme.Panel;
        UITheme.ApplyBoardSkin(cardImage, VisualAssetService.UIPanelMainID);

        Text stashTitle = CreateLabel(card.transform, "StashTitle_Text", "仓库：点物品放进背包第一个放得下的位置", 22, TextColor, TextAnchor.MiddleLeft, 40f);
        PlaceTopLeft(stashTitle.rectTransform, new Vector2(28f, -18f), new Vector2(760f, 40f));

        _listContent = CreateScrollList(card.transform, new Vector2(28f, -66f), new Vector2(764f, 560f));

        _messageText = CreateLabel(card.transform, "Message_Text", string.Empty, 20, UITheme.Warning, TextAnchor.MiddleLeft, 36f);
        PlaceTopLeft(_messageText.rectTransform, new Vector2(28f, -636f), new Vector2(764f, 36f));

        CreateButton(card.transform, "Restore_Button", "带上上次的装备", new Vector2(28f, -692f), new Vector2(240f, 70f), UITheme.PanelRaised, HandleRestoreClicked);
        CreateButton(card.transform, "Shop_Button", "补给铺", new Vector2(282f, -692f), new Vector2(150f, 70f), UITheme.PanelRaised, OpenShop);
        CreateButton(card.transform, "Back_Button", "返回", new Vector2(446f, -692f), new Vector2(130f, 70f), UITheme.PanelRaised, () => _onBack?.Invoke());
        CreateButton(card.transform, "Depart_Button", "出发", new Vector2(590f, -692f), new Vector2(202f, 70f), UITheme.Accent, HandleDepartClicked);

        BuildConfirm();
        _shopPanel = WorkshopShopPanel.Create(transform);
    }

    // The confirm sits on its own sorting layer so it covers the backpack grid, which is a later sibling.
    private void BuildConfirm() {
        _confirmRoot = new GameObject("NoWeaponConfirm", typeof(RectTransform));
        _confirmRoot.transform.SetParent(transform, false);
        Stretch((RectTransform)_confirmRoot.transform);
        Canvas overlay = _confirmRoot.AddComponent<Canvas>();
        overlay.overrideSorting = true;
        overlay.sortingOrder = 500;
        _confirmRoot.AddComponent<GraphicRaycaster>();
        _confirmRoot.AddComponent<Image>().color = new Color(0.1f, 0.06f, 0.03f, 0.62f);

        GameObject dialog = new GameObject("Dialog", typeof(RectTransform));
        dialog.transform.SetParent(_confirmRoot.transform, false);
        PlaceCenter((RectTransform)dialog.transform, Vector2.zero, new Vector2(760f, 280f));
        Image dialogImage = dialog.AddComponent<Image>();
        dialogImage.color = UITheme.Panel;
        UITheme.ApplyBoardSkin(dialogImage, VisualAssetService.UIPanelMainID);

        _confirmText = CreateLabel(dialog.transform, "Confirm_Text", string.Empty, 24, TextColor, TextAnchor.MiddleCenter, 100f);
        PlaceTopLeft(_confirmText.rectTransform, new Vector2(30f, -34f), new Vector2(700f, 100f));
        CreateButton(dialog.transform, "ConfirmDepart_Button", "仍然出发", new Vector2(40f, -172f), new Vector2(210f, 70f), UITheme.Accent, Depart);
        CreateButton(dialog.transform, "ConfirmShop_Button", "去补给铺", new Vector2(275f, -172f), new Vector2(210f, 70f), UITheme.PanelRaised, OpenShop);
        CreateButton(dialog.transform, "ConfirmCancel_Button", "再想想", new Vector2(510f, -172f), new Vector2(210f, 70f), UITheme.PanelRaised, () => _confirmRoot.SetActive(false));
        _confirmRoot.SetActive(false);
    }

    private Transform CreateScrollList(Transform parent, Vector2 topLeft, Vector2 size) {
        GameObject viewport = new GameObject("StashList_Viewport", typeof(RectTransform));
        viewport.transform.SetParent(parent, false);
        RectTransform viewportRect = (RectTransform)viewport.transform;
        PlaceTopLeft(viewportRect, topLeft, size);
        viewport.AddComponent<RectMask2D>();
        Image hitArea = viewport.AddComponent<Image>();
        hitArea.color = new Color(0f, 0f, 0f, 0.001f);

        GameObject content = new GameObject("StashList_Content", typeof(RectTransform));
        content.transform.SetParent(viewport.transform, false);
        RectTransform contentRect = (RectTransform)content.transform;
        contentRect.anchorMin = new Vector2(0f, 1f);
        contentRect.anchorMax = new Vector2(1f, 1f);
        contentRect.pivot = new Vector2(0.5f, 1f);
        contentRect.anchoredPosition = Vector2.zero;
        contentRect.sizeDelta = Vector2.zero;
        VerticalLayoutGroup layout = content.AddComponent<VerticalLayoutGroup>();
        layout.childControlWidth = true;
        layout.childControlHeight = true;
        layout.childForceExpandWidth = true;
        layout.childForceExpandHeight = false;
        layout.spacing = 6f;
        content.AddComponent<ContentSizeFitter>().verticalFit = ContentSizeFitter.FitMode.PreferredSize;

        ScrollRect scroll = viewport.AddComponent<ScrollRect>();
        scroll.content = contentRect;
        scroll.viewport = viewportRect;
        scroll.horizontal = false;
        scroll.movementType = ScrollRect.MovementType.Clamped;
        scroll.scrollSensitivity = 30f;
        return content.transform;
    }

    private Button CreateButton(Transform parent, string name, string label, Vector2 topLeft, Vector2 size, Color color, Action onClick) {
        GameObject buttonObj = new GameObject(name, typeof(RectTransform));
        buttonObj.transform.SetParent(parent, false);
        PlaceTopLeft((RectTransform)buttonObj.transform, topLeft, size);
        Image image = buttonObj.AddComponent<Image>();
        image.color = color;
        UITheme.ApplyButtonSkin(image, color);
        Button button = buttonObj.AddComponent<Button>();
        button.targetGraphic = image;
        button.onClick.AddListener(() => onClick());
        Text text = CreateLabel(buttonObj.transform, "Label_Text", label, 24, color == UITheme.Accent ? UITheme.OnAccent : UITheme.TextPrimary, TextAnchor.MiddleCenter, size.y);
        Stretch(text.rectTransform);
        return button;
    }

    private Text CreateLabel(Transform parent, string name, string content, int fontSize, Color color, TextAnchor alignment, float height) {
        GameObject textObj = new GameObject(name, typeof(RectTransform));
        textObj.transform.SetParent(parent, false);
        Text text = textObj.AddComponent<Text>();
        text.font = _font;
        text.fontSize = fontSize;
        text.color = color;
        text.alignment = alignment;
        text.text = content;
        text.raycastTarget = false;
        text.horizontalOverflow = HorizontalWrapMode.Wrap;
        text.verticalOverflow = VerticalWrapMode.Truncate;
        textObj.AddComponent<LayoutElement>().preferredHeight = height;
        ((RectTransform)textObj.transform).sizeDelta = new Vector2(0f, height);
        return text;
    }

    private static string ResolveTypeLabel(ItemEntity item) {
        switch (item?.ItemType) {
            case "Weapon": return "武器";
            case "Armor": return "防具";
            case "Anchor": return "护符";
            case "Consumable": return "消耗品";
            case "Loot": return "战利品";
            case "QuestItem": return "委托物品";
            default: return "材料";
        }
    }

    private static Color ResolveItemTint(ItemEntity item) {
        switch (item?.ItemType) {
            case "Weapon": return new Color(0.75f, 0.18f, 0.18f, 1f);
            case "Armor": return new Color(0.35f, 0.45f, 0.7f, 1f);
            case "Consumable": return new Color(0.22f, 0.65f, 0.3f, 1f);
            default: return new Color(0.6f, 0.52f, 0.32f, 1f);
        }
    }

    private static void Stretch(RectTransform rect) {
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;
    }

    private static void PlaceTopLeft(RectTransform rect, Vector2 position, Vector2 size) {
        rect.anchorMin = new Vector2(0f, 1f);
        rect.anchorMax = new Vector2(0f, 1f);
        rect.pivot = new Vector2(0f, 1f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }

    private static void PlaceCenter(RectTransform rect, Vector2 position, Vector2 size) {
        rect.anchorMin = new Vector2(0.5f, 0.5f);
        rect.anchorMax = new Vector2(0.5f, 0.5f);
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
    }
}
