using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DungeonMapUIController : MonoBehaviour {
    private const float NodeButtonWidth = 140f;
    private const float NodeButtonHeight = 88f;
    private const float NodeButtonSpacing = 20f;

    public GameObject nodeButtonPrefab;
    public Transform contentParent;
    public Button openBackpackBtn;
    public Button closeBackpackBtn;
    public Text backpackHintText;
    public Image backgroundImage;

    public void RefreshMap() {
        EnsureInventoryControls();
        ApplyMapBackground();

        if (contentParent == null || nodeButtonPrefab == null) {
            return;
        }

        ClearNodeButtons();

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

        ConfigureMapLayout(path.Count);

        bool foundCurrent = false;

        for (int i = 0; i < path.Count; i++) {
            NodeBase node = path[i];
            GameObject btnGo = Instantiate(nodeButtonPrefab, contentParent);
            ConfigureNodeButtonLayout(btnGo);
            Button btn = btnGo.GetComponent<Button>();
            Text txt = btnGo.GetComponentInChildren<Text>();
            Image img = btnGo.GetComponent<Image>();

            if (txt != null) {
                txt.text = BuildNodeLabel(node);
            }

            ApplyNodeIcon(btnGo.transform, node);

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

        RebuildMapLayout();
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
            if (VisualAssetService.ResolveNodeIconID(node) == VisualAssetService.BossNodeIconID) {
                return "首领节点\n(消耗SAN)";
            }

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

    private void ApplyNodeIcon(Transform buttonTransform, NodeBase node) {
        if (buttonTransform == null) {
            return;
        }

        Transform existing = buttonTransform.Find("NodeIcon_Image");
        Image icon = existing != null ? existing.GetComponent<Image>() : null;
        if (icon == null) {
            GameObject iconObj = new GameObject("NodeIcon_Image");
            iconObj.transform.SetParent(buttonTransform, false);
            icon = iconObj.AddComponent<Image>();

            RectTransform iconRect = iconObj.GetComponent<RectTransform>();
            iconRect.anchorMin = new Vector2(0.5f, 1f);
            iconRect.anchorMax = new Vector2(0.5f, 1f);
            iconRect.pivot = new Vector2(0.5f, 1f);
            iconRect.anchoredPosition = new Vector2(0f, -8f);
            iconRect.sizeDelta = new Vector2(42f, 42f);
        }

        string visualID = VisualAssetService.ResolveNodeIconID(node);
        VisualUIHelper.ApplySprite(icon, visualID, Color.white, ResolveNodeFallbackTint(node));

        Text label = buttonTransform.GetComponentInChildren<Text>();
        if (label != null) {
            RectTransform textRect = label.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6f, 4f);
            textRect.offsetMax = new Vector2(-6f, -46f);
            label.alignment = TextAnchor.LowerCenter;
            label.raycastTarget = false;
        }
    }

    private Color ResolveNodeFallbackTint(NodeBase node) {
        if (node is CombatNode) {
            return new Color(0.75f, 0.22f, 0.18f, 1f);
        }

        if (node is SafeRoomNode) {
            return new Color(0.24f, 0.7f, 0.48f, 1f);
        }

        if (node is StairsNode) {
            return new Color(0.78f, 0.7f, 0.42f, 1f);
        }

        return Color.white;
    }

    private void ClearNodeButtons() {
        List<GameObject> children = new List<GameObject>();
        foreach (Transform child in contentParent) {
            children.Add(child.gameObject);
        }

        foreach (GameObject child in children) {
            child.SetActive(false);
            DestroyRuntimeObject(child);
        }
    }

    private void ConfigureMapLayout(int nodeCount) {
        RectTransform contentRect = contentParent as RectTransform;
        if (contentRect != null) {
            float width = Mathf.Max(NodeButtonWidth, nodeCount * NodeButtonWidth + Mathf.Max(0, nodeCount - 1) * NodeButtonSpacing);
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(width, NodeButtonHeight);
        }

        HorizontalLayoutGroup layout = contentParent.GetComponent<HorizontalLayoutGroup>();
        if (layout == null) {
            layout = contentParent.gameObject.AddComponent<HorizontalLayoutGroup>();
        }

        layout.spacing = NodeButtonSpacing;
        layout.childAlignment = TextAnchor.MiddleCenter;
        layout.childControlWidth = false;
        layout.childControlHeight = false;
        layout.childForceExpandWidth = false;
        layout.childForceExpandHeight = false;
    }

    private void ConfigureNodeButtonLayout(GameObject buttonObject) {
        if (buttonObject == null) {
            return;
        }

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        if (rect != null) {
            rect.sizeDelta = new Vector2(NodeButtonWidth, NodeButtonHeight);
        }

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        if (layoutElement == null) {
            layoutElement = buttonObject.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = NodeButtonWidth;
        layoutElement.preferredWidth = NodeButtonWidth;
        layoutElement.flexibleWidth = 0f;
        layoutElement.minHeight = NodeButtonHeight;
        layoutElement.preferredHeight = NodeButtonHeight;
        layoutElement.flexibleHeight = 0f;
    }

    private void RebuildMapLayout() {
        RectTransform contentRect = contentParent as RectTransform;
        if (contentRect == null) {
            return;
        }

        Canvas.ForceUpdateCanvases();
        LayoutRebuilder.ForceRebuildLayoutImmediate(contentRect);
    }

    private void DestroyRuntimeObject(GameObject target) {
        if (target == null) {
            return;
        }

        if (Application.isPlaying) {
            Destroy(target);
        } else {
            DestroyImmediate(target);
        }
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

    private void ApplyMapBackground() {
        backgroundImage = VisualUIHelper.EnsurePanelBackground(transform, backgroundImage, "DungeonMapBackground_Image");
        string visualID = VisualAssetService.ResolveDungeonMapBackgroundID(GameRoot.Core?.Dungeon?.CurrentLayer);
        VisualUIHelper.ApplySprite(backgroundImage, visualID, Color.white, new Color(0.05f, 0.08f, 0.1f, 0.92f), false);
    }
}
