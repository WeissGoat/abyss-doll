using UnityEngine;
using UnityEngine.UI;

public class SafeRoomUIController : MonoBehaviour {
    public Button restBtn;
    public Button evacuateBtn;
    public Text itemHintText;

    private SafeRoomNode _currentSafeRoomNode;
    private StairsNode _currentStairsNode;

    void Start() {
        EnsureHintText();

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
        SetButtonLabel(restBtn, "休整");
        SetButtonLabel(evacuateBtn, "返回小镇");
        SetPrimaryInteractable(true);
        RefreshItemHints();
    }

    public void Setup(StairsNode node) {
        _currentSafeRoomNode = null;
        _currentStairsNode = node;
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
}
