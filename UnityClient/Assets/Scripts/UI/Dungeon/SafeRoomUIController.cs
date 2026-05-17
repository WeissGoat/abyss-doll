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
        SetButtonLabel(restBtn, "休整");
        SetButtonLabel(evacuateBtn, "返回小镇");
        SetPrimaryInteractable(true);
        RefreshItemHints();
    }

    public void Setup(StairsNode node) {
        _currentSafeRoomNode = null;
        _currentStairsNode = node;
        ApplyRoomSkin(VisualAssetService.ResolveStairsRoomBackgroundID());
        SetButtonLabel(restBtn, node != null && node.CanEnterNextLayer() ? "进入下一层" : "深渊尽头");
        SetButtonLabel(evacuateBtn, "返回小镇");
        SetPrimaryInteractable(node != null && node.CanEnterNextLayer());
        RefreshItemHints();
    }

    private void HandlePrimaryAction() {
        if (_currentSafeRoomNode != null) {
            _currentSafeRoomNode.Rest();
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
        hintRect.sizeDelta = new Vector2(760f, 160f);

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
            itemHintText.text = _currentStairsNode.CanEnterNextLayer()
                ? $"阶梯间\n继续深入将进入第 {currentLayer + 1} 层；返回小镇会立刻结算当前带出的战利品。"
                : "阶梯间\n已经抵达当前最深处；请返回小镇并结算战利品。";
            return;
        }

        BackpackGrid grid = GameRoot.Core?.CurrentPlayer?.ActiveDoll?.RuntimeGrid as BackpackGrid;
        if (grid == null) {
            itemHintText.text = "安全屋中可整理背包，并点击消耗品立即使用。";
            return;
        }

        System.Collections.Generic.List<string> lines = new System.Collections.Generic.List<string>();
        foreach (var item in grid.ContainedItems) {
            if (item == null || item.ItemType != nameof(ItemType.Consumable)) {
                continue;
            }

            lines.Add($"{item.Name}: {ItemPresentationRules.BuildUseHint(item)}");
        }

        itemHintText.text = lines.Count > 0
            ? "安全屋补给\n" + string.Join("\n", lines)
            : "安全屋补给\n当前背包里没有可用消耗品。";
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
