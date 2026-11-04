using UnityEngine;
using UnityEngine.UI;

public class SafeRoomUIController : MonoBehaviour {
    public Button restBtn;
    public Button evacuateBtn;
    public Text itemHintText;
    public Image backgroundImage;
    public Image backdropImage;

    private SafeRoomNode _currentSafeRoomNode;
    private StairsNode _currentStairsNode;
    // The lift's parcel: a list of this dive's loot she can send home from here (DungeonManager.TrySendHome).
    private const int ParcelRows = 6;
    private RectTransform _parcelPanel;
    private Text _parcelHeader;
    private readonly System.Collections.Generic.List<Button> _parcelButtons = new System.Collections.Generic.List<Button>();

    void Start() {
        EnsureHintText();
        ApplyRoomSkin(VisualAssetService.ResolveSafeRoomBackgroundID());

        if (restBtn != null) {
            restBtn.onClick.AddListener(HandlePrimaryAction);
        }

        if (evacuateBtn != null) {
            evacuateBtn.onClick.AddListener(HandleEvacuateAction);
        }
    }

    public void Setup(SafeRoomNode node) {
        _currentSafeRoomNode = node;
        _currentStairsNode = null;
        ApplyRoomSkin(VisualAssetService.ResolveSafeRoomBackgroundID());
        SetTitle("升 降 梯");
        SetButtonLabel(restBtn, "继续深入");
        SetButtonLabel(evacuateBtn, "带着收获回城");
        SetPrimaryInteractable(true);
        RefreshItemHints();
        RefreshParcel();
    }

    public void Setup(StairsNode node) {
        _currentSafeRoomNode = null;
        _currentStairsNode = node;
        ApplyRoomSkin(VisualAssetService.ResolveStairsRoomBackgroundID());
        string blocked = GameRoot.Core?.Dungeon?.NextLayerBlockReason();
        SetButtonLabel(restBtn, node != null && node.CanEnterNextLayer() ? "进入下一层" : string.IsNullOrEmpty(blocked) ? "深渊尽头" : "底盘撑不住");
        SetTitle("安 全 区");
        SetButtonLabel(evacuateBtn, "返回小镇");
        SetPrimaryInteractable(node != null && node.CanEnterNextLayer());
        RefreshItemHints();
        RefreshParcel();
        // The deepest boss just fell: the end of the Demo starts here (DemoEndingComic).
        DemoEndingComic.PlayIfDue(null);
    }

    private void HandlePrimaryAction() {
        if (_currentSafeRoomNode != null) {
            _currentSafeRoomNode.Continue();
            return;
        }

        if (_currentStairsNode != null && _currentStairsNode.CanEnterNextLayer()) {
            _currentStairsNode.EnterNextLayer();
        }
    }

    private void HandleEvacuateAction() {
        if (_currentSafeRoomNode != null) {
            _currentSafeRoomNode.Evacuate();
            return;
        }

        if (_currentStairsNode != null) {
            _currentStairsNode.ReturnToTown();
        }
    }

    private void EnsureHintText() {
        if (itemHintText != null) {
            return;
        }

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameObject hintObj = new GameObject("ItemHint_Text");
        hintObj.transform.SetParent(transform, false);

        RectTransform hintRect = hintObj.AddComponent<RectTransform>();
        hintRect.anchorMin = new Vector2(0.5f, 0f);
        hintRect.anchorMax = new Vector2(0.5f, 0f);
        hintRect.pivot = new Vector2(0.5f, 0f);
        hintRect.anchoredPosition = new Vector2(0f, 24f);
        hintRect.sizeDelta = new Vector2(760f, 200f);

        itemHintText = hintObj.AddComponent<Text>();
        itemHintText.font = defaultFont;
        itemHintText.fontSize = 24;
        itemHintText.color = new Color(0.92f, 0.95f, 0.98f);
        itemHintText.alignment = TextAnchor.UpperCenter;
        itemHintText.raycastTarget = false;
    }

    public void RefreshItemHints() {
        EnsureHintText();
        if (itemHintText == null) {
            return;
        }

        if (_currentStairsNode != null) {
            int currentLayer = GameRoot.Core?.Dungeon?.CurrentLayer?.LayerID ?? _currentStairsNode.LayerID;
            string blocked = GameRoot.Core?.Dungeon?.NextLayerBlockReason();
            itemHintText.text = _currentStairsNode.CanEnterNextLayer()
                ? $"阶梯间\n继续深入将进入第 {currentLayer + 1} 层；返回小镇会立刻结算当前带出的战利品。"
                : !string.IsNullOrEmpty(blocked)
                    ? $"阶梯间\n{blocked}先回小镇升级底盘。"
                    : "阶梯间\n已经抵达当前最深处；请返回小镇并结算战利品。";
            return;
        }

        BackpackGrid grid = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;
        if (grid == null) {
            itemHintText.text = LiftLine();
            return;
        }

        System.Collections.Generic.List<string> lines = new System.Collections.Generic.List<string>();
        foreach (var item in grid.ContainedItems) {
            if (item == null || item.ItemType != nameof(ItemType.Consumable)) {
                continue;
            }

            lines.Add($"{item.Name}: {ItemPresentationRules.BuildUseHint(item)}");
        }

        itemHintText.text = LiftLine() + (lines.Count > 0
            ? "\n" + string.Join("\n", lines)
            : "\n背包里没有能用的消耗品。");

        // Bond 2: Zero says what she senses on the paths ahead.
        if (BondService.Level(GameRoot.Core?.CurrentPlayer?.ActiveDoll) >= HomeLoop.HintLevel) {
            string hint = HomeLoop.PathHint(_currentSafeRoomNode);
            if (!string.IsNullOrEmpty(hint)) {
                itemHintText.text = $"零号：「{hint}」\n" + itemHintText.text;
            }
        }
    }

    // One button per piece of this dive's loot still in the backpack, best value per cell first; one that does not fit
    // what is left of the parcel is greyed out with the reason.
    public void RefreshParcel() {
        DungeonManager dungeon = GameRoot.Core?.Dungeon;
        EnsureParcelPanel();
        bool open = dungeon != null && dungeon.CanSendHomeHere && dungeon.LiftParcelCapacity() > 0;
        _parcelPanel.gameObject.SetActive(open);
        if (!open) {
            return;
        }

        int free = dungeon.LiftParcelCapacity() - dungeon.LiftParcelUsed();
        System.Collections.Generic.List<ItemEntity> loot = dungeon.CarriedRunLoot();
        loot.Sort((a, b) => (b.BaseValue / DungeonManager.ItemCells(b)).CompareTo(a.BaseValue / DungeonManager.ItemCells(a)));
        _parcelHeader.text = $"升降梯货篮：还能送 {free}/{dungeon.LiftParcelCapacity()} 格\n送上去的东西直接到家，战败也不会丢";
        if (loot.Count == 0) {
            _parcelHeader.text += "\n（这一趟还没捡到能送的东西）";
        }

        for (int i = 0; i < ParcelRows; i++) {
            Button button = _parcelButtons[i];
            ItemEntity item = i < loot.Count ? loot[i] : null;
            button.gameObject.SetActive(item != null);
            if (item == null) {
                continue;
            }

            int cells = DungeonManager.ItemCells(item);
            UITheme.SetButtonLabel(button, $"送上去 · {item.Name}  {cells} 格 · {item.BaseValue}G");
            button.interactable = cells <= free;
            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => {
                if (GameRoot.Core.Dungeon.TrySendHome(item, out string reason)) {
                    GameAudio.PlaySfx("click");
                } else {
                    Debug.Log("[SafeRoomUI] " + reason);
                }

                RefreshParcel();
                RefreshItemHints();
            });
        }
    }

    private void EnsureParcelPanel() {
        if (_parcelPanel != null) {
            return;
        }

        Image plate = UITheme.CreatePanel(transform, "LiftParcel_Panel", new Color(0.08f, 0.06f, 0.04f, 0.82f));
        _parcelPanel = plate.rectTransform;
        _parcelPanel.anchorMin = new Vector2(0f, 1f);
        _parcelPanel.anchorMax = new Vector2(0f, 1f);
        _parcelPanel.pivot = new Vector2(0f, 1f);
        _parcelPanel.anchoredPosition = new Vector2(48f, -170f);
        _parcelPanel.sizeDelta = new Vector2(500f, 96f + ParcelRows * 50f);

        _parcelHeader = UITheme.CreateText(_parcelPanel, "LiftParcelHeader_Text", string.Empty, 22, UITheme.TextOnDark, TextAnchor.UpperLeft);
        RectTransform header = _parcelHeader.rectTransform;
        header.anchorMin = new Vector2(0f, 1f);
        header.anchorMax = new Vector2(1f, 1f);
        header.pivot = new Vector2(0.5f, 1f);
        header.anchoredPosition = new Vector2(0f, -12f);
        header.sizeDelta = new Vector2(-32f, 84f);

        for (int i = 0; i < ParcelRows; i++) {
            Button button = UITheme.CreateButton(_parcelPanel, $"LiftParcelItem{i}_Button", string.Empty, UITheme.PanelRaised, UITheme.TextPrimary, 20);
            RectTransform rect = (RectTransform)button.transform;
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(1f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -96f - i * 50f);
            rect.sizeDelta = new Vector2(-32f, 42f);
            _parcelButtons.Add(button);
        }
    }

    // What the lift offers: home now with what she carries, or on to the next lift or the boss.
    private string LiftLine() {
        DungeonLayer layer = GameRoot.Core?.Dungeon?.CurrentLayer;
        string ahead = string.Empty;
        if (layer != null && _currentSafeRoomNode != null) {
            int rows = 0;
            for (int row = _currentSafeRoomNode.MapRow + 1; row < layer.NodeRows.Count; row++) {
                NodeBase first = layer.NodeRows[row].Count > 0 ? layer.NodeRows[row][0] : null;
                if (first is SafeRoomNode || first is StairsNode || first is CombatNode && first.NodeID.EndsWith("_boss")) {
                    ahead = first is SafeRoomNode ? $"再走 {rows} 格是下一座升降梯。" : $"再走 {rows} 格是首领。";
                    break;
                }

                rows++;
            }
        }

        return "升降梯\n现在回城，背包里的东西就落袋了；继续深入，" + (string.IsNullOrEmpty(ahead) ? "前面没有回头路。" : ahead);
    }

    private void SetTitle(string label) {
        Text title = transform.Find("Title_Text")?.GetComponent<Text>();
        if (title != null) {
            title.text = label;
        }
    }

    private void SetButtonLabel(Button button, string label) {
        Text text = button != null ? button.GetComponentInChildren<Text>() : null;
        if (text != null) {
            text.text = label;
        }
    }

    private void SetPrimaryInteractable(bool interactable) {
        if (restBtn != null) {
            restBtn.interactable = interactable;
        }
    }

    private void ApplyButtonSkins() {
        VisualUIHelper.ApplyButtonSkin(restBtn, VisualAssetService.UIButtonPrimaryID, new Color(0.75f, 0.48f, 0.18f));
        VisualUIHelper.ApplyButtonSkin(evacuateBtn, VisualAssetService.UIButtonSecondaryID, new Color(0.28f, 0.34f, 0.38f));
        UITheme.StyleButton(restBtn, true);
        UITheme.StyleButton(evacuateBtn, false);
    }

    private void ApplyRoomSkin(string backgroundVisualID) {
        Image rootImage = GetComponent<Image>();
        if (rootImage != null) {
            rootImage.color = Color.clear;
            rootImage.raycastTarget = false;
        }

        backgroundImage = VisualUIHelper.EnsurePanelBackground(transform, backgroundImage, "RoomBackground_Image");
        VisualUIHelper.ApplyCoverSprite(backgroundImage, backgroundVisualID, Color.white, new Color(0.06f, 0.075f, 0.08f, 0.94f));
        backdropImage = EnsureRoomBackdrop(backdropImage, "RoomBackdrop_Image");

        if (backgroundImage != null) {
            int backgroundIndex = Mathf.Min(1, backgroundImage.transform.parent.childCount - 1);
            backgroundImage.transform.SetSiblingIndex(backgroundIndex);
        }

        ApplyButtonSkins();
        Transform title = transform.Find("Title_Text");
        Text titleLabel = title != null ? title.GetComponent<Text>() : null;
        if (titleLabel != null) {
            titleLabel.color = UITheme.AccentOnDark;
        }
    }

    private Image EnsureRoomBackdrop(Image current, string objectName) {
        Image image = VisualUIHelper.EnsurePanelBackground(transform, current, objectName);
        if (image == null) {
            return null;
        }

        VisualUIHelper.ApplySolidColor(image, new Color(0.015f, 0.016f, 0.014f, 1f));
        image.transform.SetAsFirstSibling();
        return image;
    }
}
