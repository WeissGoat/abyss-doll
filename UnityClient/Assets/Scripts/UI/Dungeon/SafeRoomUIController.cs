using UnityEngine;
using UnityEngine.UI;

public class SafeRoomUIController : MonoBehaviour {
    public Button restBtn;
    public Button evacuateBtn;
    public Text itemHintText;

    private SafeRoomNode _currentNode;

    void Start() {
        EnsureHintText();

        if (restBtn != null) {
            restBtn.onClick.AddListener(() => {
                if (_currentNode != null) {
                    _currentNode.Rest();
                }
            });
        }
        if (evacuateBtn != null) {
            evacuateBtn.onClick.AddListener(() => {
                if (_currentNode != null) {
                    _currentNode.Evacuate();
                }
            });
        }
    }

    public void Setup(SafeRoomNode node) {
        _currentNode = node;
        RefreshItemHints();
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
}
