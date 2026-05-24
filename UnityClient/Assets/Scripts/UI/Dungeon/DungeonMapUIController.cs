using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DungeonMapUIController : MonoBehaviour {
    private const float NodeButtonWidth = 160f;
    private const float NodeButtonHeight = 150f;
    private const float NodeColumnSpacing = 230f;
    private const float NodeRowSpacing = 190f;
    private const float RouteLineHeight = 24f;

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

        List<List<NodeBase>> rows = GetRenderableRows(layer);
        Dictionary<NodeBase, Vector2> nodePositions = BuildNodePositions(rows);
        ConfigureMapLayout(rows);
        CreateRouteLines(rows, nodePositions);

        foreach (List<NodeBase> row in rows) {
            foreach (NodeBase node in row) {
                CreateNodeButton(layer, node, nodePositions);
            }
        }

        RebuildMapLayout();
    }

    private List<List<NodeBase>> GetRenderableRows(DungeonLayer layer) {
        if (layer?.NodeRows != null && layer.NodeRows.Count > 0) {
            return layer.NodeRows;
        }

        List<List<NodeBase>> fallbackRows = new List<List<NodeBase>>();
        NodeBase current = layer?.RootNode;
        while (current != null) {
            fallbackRows.Add(new List<NodeBase> { current });
            current = current.NextNodes != null && current.NextNodes.Count > 0 ? current.NextNodes[0] : null;
        }

        return fallbackRows;
    }

    private void CreateNodeButton(DungeonLayer layer, NodeBase node, Dictionary<NodeBase, Vector2> nodePositions) {
        if (node == null || nodePositions == null || !nodePositions.TryGetValue(node, out Vector2 anchoredPosition)) {
            return;
        }

        GameObject btnGo = Instantiate(nodeButtonPrefab, contentParent);
        ConfigureNodeButtonLayout(btnGo, anchoredPosition);
        Button btn = btnGo.GetComponent<Button>();
        Text txt = btnGo.GetComponentInChildren<Text>();
        bool isSelectable = IsNodeSelectable(layer, node);
        ApplyNodeButtonSkin(btn, node, !isSelectable || node.IsVisited);

        if (txt != null) {
            txt.text = BuildNodeLabel(node);
        }

        ApplyNodeIcon(btnGo.transform, node);

        Debug.Log($"[DungeonMapUI] Render node button: {node.NodeID}, Type={node.GetType().Name}, Label={txt?.text?.Replace('\n', ' ')}");

        if (btn == null) {
            return;
        }

        if (!isSelectable || node.IsVisited) {
            btn.interactable = false;
            return;
        }

        btn.interactable = true;
        btn.onClick.AddListener(() => {
            GameRoot.Core.Dungeon.MoveToNode(node);
        });
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

        if (node is TreasureNode) {
            return "宝箱\n(战利品)";
        }

        if (node is RestStopNode) {
            return "营地\n(小休整)";
        }

        if (node is EventNode) {
            return "事件\n(未知)";
        }

        if (node is HazardNode) {
            return "危险\n(损耗)";
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
            iconRect.anchoredPosition = new Vector2(0f, -10f);
        }

        string visualID = VisualAssetService.ResolveNodeIconID(node);
        VisualUIHelper.ApplyContainSprite(icon, visualID, VisualDisplaySpecs.NodeIcon, Color.white, ResolveNodeFallbackTint(node), false);

        Text label = buttonTransform.GetComponentInChildren<Text>();
        if (label != null) {
            RectTransform textRect = label.GetComponent<RectTransform>();
            textRect.anchorMin = Vector2.zero;
            textRect.anchorMax = Vector2.one;
            textRect.offsetMin = new Vector2(6f, 8f);
            textRect.offsetMax = new Vector2(-6f, -96f);
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

        if (node is TreasureNode) {
            return new Color(0.86f, 0.64f, 0.2f, 1f);
        }

        if (node is RestStopNode) {
            return new Color(0.26f, 0.58f, 0.68f, 1f);
        }

        if (node is EventNode) {
            return new Color(0.55f, 0.43f, 0.7f, 1f);
        }

        if (node is HazardNode) {
            return new Color(0.62f, 0.26f, 0.2f, 1f);
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

    private void ConfigureMapLayout(List<List<NodeBase>> rows) {
        int rowCount = rows?.Count ?? 0;
        int maxNodesInRow = 1;
        if (rows != null) {
            foreach (List<NodeBase> row in rows) {
                maxNodesInRow = Mathf.Max(maxNodesInRow, row?.Count ?? 0);
            }
        }

        RectTransform contentRect = contentParent as RectTransform;
        if (contentRect != null) {
            float width = Mathf.Max(720f, NodeButtonWidth + Mathf.Max(0, rowCount - 1) * NodeColumnSpacing);
            float height = Mathf.Max(360f, NodeButtonHeight + Mathf.Max(0, maxNodesInRow - 1) * NodeRowSpacing);
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.sizeDelta = new Vector2(width, height);
        }

        HorizontalLayoutGroup layout = contentParent.GetComponent<HorizontalLayoutGroup>();
        if (layout != null) {
            layout.enabled = false;
            layout.spacing = 0f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }
    }

    private Dictionary<NodeBase, Vector2> BuildNodePositions(List<List<NodeBase>> rows) {
        Dictionary<NodeBase, Vector2> positions = new Dictionary<NodeBase, Vector2>();
        if (rows == null || rows.Count == 0) {
            return positions;
        }

        float totalWidth = Mathf.Max(0, rows.Count - 1) * NodeColumnSpacing;
        float startX = -totalWidth * 0.5f;
        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++) {
            List<NodeBase> row = rows[rowIndex];
            if (row == null || row.Count == 0) {
                continue;
            }

            float rowHeight = Mathf.Max(0, row.Count - 1) * NodeRowSpacing;
            float startY = rowHeight * 0.5f;
            for (int columnIndex = 0; columnIndex < row.Count; columnIndex++) {
                NodeBase node = row[columnIndex];
                if (node == null) {
                    continue;
                }

                positions[node] = new Vector2(startX + rowIndex * NodeColumnSpacing, startY - columnIndex * NodeRowSpacing);
            }
        }

        return positions;
    }

    private void ConfigureNodeButtonLayout(GameObject buttonObject, Vector2 anchoredPosition) {
        if (buttonObject == null) {
            return;
        }

        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        if (rect != null) {
            rect.anchorMin = new Vector2(0.5f, 0.5f);
            rect.anchorMax = new Vector2(0.5f, 0.5f);
            rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
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

    private bool IsNodeSelectable(DungeonLayer layer, NodeBase node) {
        if (layer == null || node == null || node.IsVisited) {
            return false;
        }

        if (layer.CurrentNode == null) {
            return (layer.EntryNodes != null && layer.EntryNodes.Contains(node))
                || (layer.EntryNodes == null || layer.EntryNodes.Count == 0) && node == layer.RootNode;
        }

        return layer.CurrentNode.NextNodes != null && layer.CurrentNode.NextNodes.Contains(node);
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

        VisualUIHelper.ApplyButtonSkin(openBackpackBtn, VisualAssetService.UIButtonSecondaryID, new Color(0.2f, 0.52f, 0.86f));
        VisualUIHelper.ApplyButtonSkin(closeBackpackBtn, VisualAssetService.UIButtonDangerID, new Color(0.82f, 0.36f, 0.2f));
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

    private void ApplyNodeButtonSkin(Button button, NodeBase node, bool dimmed) {
        if (button == null) {
            return;
        }

        Color fallback = ResolveNodeFallbackTint(node);
        if (dimmed) {
            fallback = new Color(fallback.r * 0.35f, fallback.g * 0.35f, fallback.b * 0.35f, 0.92f);
        }

        Image image = button.GetComponent<Image>();
        if (image == null) {
            image = button.gameObject.AddComponent<Image>();
        }

        VisualUIHelper.ApplySimpleSprite(
            image,
            VisualAssetService.UIDungeonNodePlateID,
            Color.white,
            fallback,
            true,
            false);
        button.targetGraphic = image;

        if (dimmed) {
            image.color = new Color(0.28f, 0.28f, 0.28f, 0.92f);
        }
    }

    private void ApplyMapBackground() {
        backgroundImage = VisualUIHelper.EnsurePanelBackground(transform, backgroundImage, "DungeonMapBackground_Image");
        string visualID = VisualAssetService.ResolveDungeonMapBackgroundID(GameRoot.Core?.Dungeon?.CurrentLayer);
        VisualUIHelper.ApplyCoverSprite(backgroundImage, visualID, Color.white, new Color(0.05f, 0.08f, 0.1f, 0.92f));
    }

    private void CreateRouteLines(List<List<NodeBase>> rows, Dictionary<NodeBase, Vector2> nodePositions) {
        if (rows == null || nodePositions == null || contentParent == null) {
            return;
        }

        foreach (List<NodeBase> row in rows) {
            if (row == null) {
                continue;
            }

            foreach (NodeBase node in row) {
                if (node?.NextNodes == null || !nodePositions.TryGetValue(node, out Vector2 fromPosition)) {
                    continue;
                }

                foreach (NodeBase next in node.NextNodes) {
                    if (next == null || !nodePositions.TryGetValue(next, out Vector2 toPosition)) {
                        continue;
                    }

                    CreateRouteLine(fromPosition, toPosition);
                }
            }
        }
    }

    private void CreateRouteLine(Vector2 fromPosition, Vector2 toPosition) {
        GameObject routeObj = new GameObject("DungeonRouteLine_Image");
        routeObj.transform.SetParent(contentParent, false);
        Image routeImage = routeObj.AddComponent<Image>();

        Vector2 delta = toPosition - fromPosition;
        RectTransform routeRect = routeObj.GetComponent<RectTransform>();
        routeRect.anchorMin = new Vector2(0.5f, 0.5f);
        routeRect.anchorMax = new Vector2(0.5f, 0.5f);
        routeRect.pivot = new Vector2(0.5f, 0.5f);
        routeRect.anchoredPosition = (fromPosition + toPosition) * 0.5f;
        routeRect.sizeDelta = new Vector2(Mathf.Max(16f, delta.magnitude), RouteLineHeight);
        routeRect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        VisualUIHelper.ApplySimpleSprite(
            routeImage,
            VisualAssetService.UIDungeonRouteLineID,
            Color.white,
            new Color(0.7f, 0.58f, 0.32f, 0.75f),
            false,
            false);

        LayoutElement layoutElement = routeObj.GetComponent<LayoutElement>();
        if (layoutElement == null) {
            layoutElement = routeObj.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = routeRect.sizeDelta.x;
        layoutElement.preferredWidth = routeRect.sizeDelta.x;
        layoutElement.flexibleWidth = 0f;
        layoutElement.minHeight = RouteLineHeight;
        layoutElement.preferredHeight = RouteLineHeight;
        layoutElement.flexibleHeight = 0f;
    }
}
