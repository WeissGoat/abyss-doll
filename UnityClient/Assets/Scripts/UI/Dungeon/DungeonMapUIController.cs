using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DungeonMapUIController : MonoBehaviour {
    public GameObject nodeButtonPrefab;
    public Transform contentParent;
    public Button openBackpackBtn;
    public Button closeBackpackBtn;
    public Text backpackHintText;

    public void RefreshMap() {
        EnsureInventoryControls();

        if (contentParent == null || nodeButtonPrefab == null) {
            return;
        }

        foreach (Transform child in contentParent) {
            child.gameObject.SetActive(false);
            Destroy(child.gameObject);
        }

        DungeonLayer layer = GameRoot.Core.Dungeon.CurrentLayer;
        if (layer == null || layer.RootNode == null) {
            return;
        }

        List<NodeBase> path = new List<NodeBase>();
        NodeBase curr = layer.RootNode;
        while (curr != null) {
            path.Add(curr);
            curr = curr.NextNodes != null && curr.NextNodes.Count > 0 ? curr.NextNodes[0] : null;
        }

        bool foundCurrent = false;

        for (int i = 0; i < path.Count; i++) {
            NodeBase node = path[i];
            GameObject btnGo = Instantiate(nodeButtonPrefab, contentParent);
            Button btn = btnGo.GetComponent<Button>();
            Text txt = btnGo.GetComponentInChildren<Text>();
            Image img = btnGo.GetComponent<Image>();

            if (txt != null) {
                txt.text = BuildNodeLabel(node);
            }

            Debug.Log($"[DungeonMapUI] Render node button: {node.NodeID}, Type={node.GetType().Name}, Label={txt?.text?.Replace('\n', ' ')}");

            if (node.IsVisited) {
                if (img != null) {
                    img.color = new Color(0.3f, 0.3f, 0.3f);
                }
                btn.interactable = false;
                continue;
            }

            if (!foundCurrent) {
                if (img != null) {
                    img.color = new Color(0.2f, 0.8f, 0.2f);
                }
                btn.interactable = true;
                btn.onClick.AddListener(() => {
                    GameRoot.Core.Dungeon.MoveToNode(node);
                });
                foundCurrent = true;
            } else {
                if (img != null) {
                    img.color = Color.black;
                }
                btn.interactable = false;
            }
        }
    }

    public void BindBackpackControls(GameFlowController flow, bool isOpen) {
        EnsureInventoryControls();

        if (openBackpackBtn != null) {
            openBackpackBtn.onClick.RemoveAllListeners();
            openBackpackBtn.onClick.AddListener(() => flow?.OpenDungeonMapInventory());
        }

        if (closeBackpackBtn != null) {
            closeBackpackBtn.onClick.RemoveAllListeners();
            closeBackpackBtn.onClick.AddListener(() => flow?.CloseDungeonMapInventory());
        }

        RefreshInventoryControls(isOpen);
    }

    public void RefreshInventoryControls(bool isOpen) {
        if (openBackpackBtn != null) {
            openBackpackBtn.gameObject.SetActive(!isOpen);
        }

        if (closeBackpackBtn != null) {
            closeBackpackBtn.gameObject.SetActive(isOpen);
        }

        if (backpackHintText != null) {
            backpackHintText.text = isOpen
                ? "整理背包中：拖出格子的物品会在关闭背包时丢弃"
                : "点击右下角打开背包，整理局内物资";
        }
    }

    private string BuildNodeLabel(NodeBase node) {
        if (node is CombatNode) {
            return "战斗节点\n(消耗SAN)";
        }

        if (node is SafeRoomNode) {
            return "安全屋\n(休整)";
        }

        if (node is StairsNode) {
            return "阶梯\n(深入/返回)";
        }

        return "未知节点";
    }

    private void EnsureInventoryControls() {
        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");

        if (backpackHintText == null) {
            GameObject hintObj = new GameObject("BackpackHint_Text");
            hintObj.transform.SetParent(transform, false);
            Text hintText = hintObj.AddComponent<Text>();
            hintText.font = defaultFont;
            hintText.fontSize = 26;
            hintText.color = new Color(0.95f, 0.95f, 0.95f);
            hintText.alignment = TextAnchor.MiddleRight;
            hintText.raycastTarget = false;
            RectTransform hintRect = hintObj.GetComponent<RectTransform>();
            hintRect.anchorMin = new Vector2(1f, 0f);
            hintRect.anchorMax = new Vector2(1f, 0f);
            hintRect.pivot = new Vector2(1f, 0f);
            hintRect.anchoredPosition = new Vector2(-30f, 120f);
            hintRect.sizeDelta = new Vector2(620f, 70f);
            backpackHintText = hintText;
        }

        if (openBackpackBtn == null) {
            openBackpackBtn = CreateActionButton("OpenBackpack_Button", "整理背包", new Vector2(-30f, 25f), defaultFont);
        }

        if (closeBackpackBtn == null) {
            closeBackpackBtn = CreateActionButton("CloseBackpack_Button", "关闭背包", new Vector2(-30f, 25f), defaultFont);
            Image buttonImage = closeBackpackBtn.GetComponent<Image>();
            if (buttonImage != null) {
                buttonImage.color = new Color(0.82f, 0.36f, 0.2f);
            }
        }
    }

    private Button CreateActionButton(string objectName, string label, Vector2 anchoredPosition, Font font) {
        GameObject buttonObj = new GameObject(objectName);
        buttonObj.transform.SetParent(transform, false);
        Image buttonImage = buttonObj.AddComponent<Image>();
        buttonImage.color = new Color(0.2f, 0.52f, 0.86f);
        Button button = buttonObj.AddComponent<Button>();
        RectTransform buttonRect = buttonObj.GetComponent<RectTransform>();
        buttonRect.anchorMin = new Vector2(1f, 0f);
        buttonRect.anchorMax = new Vector2(1f, 0f);
        buttonRect.pivot = new Vector2(1f, 0f);
        buttonRect.anchoredPosition = anchoredPosition;
        buttonRect.sizeDelta = new Vector2(220f, 72f);

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(buttonObj.transform, false);
        Text buttonText = textObj.AddComponent<Text>();
        buttonText.font = font;
        buttonText.fontSize = 28;
        buttonText.color = Color.white;
        buttonText.alignment = TextAnchor.MiddleCenter;
        buttonText.text = label;
        buttonText.raycastTarget = false;
        RectTransform textRect = textObj.GetComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;

        return button;
    }
}
