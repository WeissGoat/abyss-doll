using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic;

public class DungeonMapUIController : MonoBehaviour {
    private const float NodeButtonWidth = 160f;
    private const float NodeButtonHeight = 122f;
    private const float NodeColumnSpacing = 230f;
    private const float NodeRowSpacing = 150f;
    private const float RouteLineHeight = 20f;

    public GameObject nodeButtonPrefab;
    public Transform contentParent;
    public Button openBackpackBtn;
    public Button closeBackpackBtn;
    public Text backpackHintText;
    public Image backgroundImage;
    private Image mapDepthVeilImage;
    private Image firstDiveGuideImage;
    private Text firstDiveGuideText;
    private float mapPathMinX;
    private float mapPathMaxX;

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
        CreateRouteLines(layer, rows, nodePositions);

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
        DungeonMapNodePresentation presentation = DungeonMapVisibilityService.BuildNodePresentation(layer, node);
        bool isSelectable = IsNodeSelectable(layer, node);
        ApplyNodeButtonSkin(btn, node, presentation, !isSelectable || node.IsVisited);

        if (txt != null) {
            txt.text = BuildNodeLabel(node, presentation);
        }

        ApplyNodeIcon(btnGo.transform, node, presentation);

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

    private string BuildNodeLabel(NodeBase node, DungeonMapNodePresentation presentation) {
        if (presentation != null && presentation.IsHidden) {
            return "未知";
        }

        string riskLabel = CompactRiskLabel(presentation?.RiskLabel);
        string nodeLabel = BuildNodeTypeLabel(node);
        if (presentation != null && presentation.IsPreview) {
            return string.IsNullOrEmpty(riskLabel) ? $"{nodeLabel}\n预览" : $"{nodeLabel}\n{riskLabel}";
        }

        return string.IsNullOrEmpty(riskLabel) ? nodeLabel : $"{nodeLabel}\n{riskLabel}";
    }

    private string CompactRiskLabel(string riskLabel) {
        if (string.IsNullOrEmpty(riskLabel) || riskLabel == "未知风险") {
            return string.Empty;
        }

        if (riskLabel.Contains("安全") || riskLabel.Contains("低")) {
            return "低险";
        }

        if (riskLabel.Contains("中")) {
            return "中险";
        }

        if (riskLabel.Contains("高") || riskLabel.Contains("危险")) {
            return "高险";
        }

        return riskLabel.Length > 4 ? riskLabel.Substring(0, 4) : riskLabel;
    }

    private string BuildNodeTypeLabel(NodeBase node) {
        if (node is CombatNode) {
            if (VisualAssetService.ResolveNodeIconID(node) == VisualAssetService.BossNodeIconID) {
                return "首领";
            }

            return "战斗";
        }

        if (node is SafeRoomNode) {
            return "安全屋";
        }

        if (node is StairsNode) {
            return "阶梯";
        }

        if (node is TreasureNode) {
            return "宝藏";
        }

        if (node is RestStopNode) {
            return "营地";
        }

        if (node is EventNode) {
            return "事件";
        }

        if (node is HazardNode) {
            return "危险";
        }

        return "未知";
    }

    private void ApplyNodeIcon(Transform buttonTransform, NodeBase node, DungeonMapNodePresentation presentation) {
        if (buttonTransform == null) {
            return;
        }

        Transform existing = buttonTransform.Find("NodeIcon_Image");
        Image icon = existing != null ? existing.GetComponent<Image>() : null;
        if (icon == null) {
            icon = CreateNodeChildImage(buttonTransform, "NodeIcon_Image");
        }

        Image fogVeil = EnsureNodeChildImage(buttonTransform, "NodeFogVeil_Image");
        Image labelPlate = EnsureNodeChildImage(buttonTransform, "NodeLabelPlate_Image");
        RectTransform fogRect = fogVeil.rectTransform;
        fogRect.anchorMin = new Vector2(0.5f, 1f);
        fogRect.anchorMax = new Vector2(0.5f, 1f);
        fogRect.pivot = new Vector2(0.5f, 1f);
        fogRect.anchoredPosition = new Vector2(0f, -7f);
        fogRect.sizeDelta = new Vector2(98f, 88f);

        RectTransform iconRect = icon.rectTransform;
        iconRect.anchorMin = new Vector2(0.5f, 1f);
        iconRect.anchorMax = new Vector2(0.5f, 1f);
        iconRect.pivot = new Vector2(0.5f, 1f);
        iconRect.anchoredPosition = new Vector2(0f, -18f);

        RectTransform labelPlateRect = labelPlate.rectTransform;
        labelPlateRect.anchorMin = new Vector2(0.5f, 0f);
        labelPlateRect.anchorMax = new Vector2(0.5f, 0f);
        labelPlateRect.pivot = new Vector2(0.5f, 0f);
        labelPlateRect.anchoredPosition = new Vector2(0f, 7f);
        labelPlateRect.sizeDelta = new Vector2(112f, 38f);

        bool isHidden = presentation != null && presentation.IsHidden;
        bool isPreview = presentation != null && presentation.IsPreview;
        if (isHidden) {
            fogVeil.gameObject.SetActive(true);
            VisualUIHelper.ApplySlicedSprite(
                fogVeil,
                VisualAssetService.UIDungeonNodePlateID,
                new Color(1f, 1f, 1f, 0.32f),
                new Color(0.18f, 0.22f, 0.25f, 0.28f),
                false);
            fogVeil.transform.SetAsFirstSibling();
            VisualUIHelper.ApplyContainSprite(
                icon,
                VisualAssetService.UIIconLockedID,
                new Vector2(62f, 62f),
                new Color(0.78f, 0.82f, 0.86f, 0.62f),
                new Color(0.38f, 0.44f, 0.5f, 0.72f),
                false);
        } else {
            fogVeil.gameObject.SetActive(false);
            string visualID = VisualAssetService.ResolveNodeIconID(node);
            Color registeredColor = isPreview ? new Color(1f, 1f, 1f, 0.58f) : new Color(1f, 1f, 1f, 0.86f);
            VisualUIHelper.ApplyContainSprite(icon, visualID, new Vector2(68f, 68f), registeredColor, ResolveNodeFallbackTint(node, presentation), false);
        }

        Color labelPlateColor = isHidden
            ? new Color(0.07f, 0.1f, 0.12f, 0.56f)
            : isPreview ? new Color(0.1f, 0.14f, 0.14f, 0.58f) : new Color(0.08f, 0.12f, 0.11f, 0.66f);
        VisualUIHelper.ApplySlicedSprite(
            labelPlate,
            VisualAssetService.UIListRowNormalID,
            labelPlateColor,
            labelPlateColor,
            false);
        labelPlate.transform.SetAsLastSibling();

        Text label = buttonTransform.GetComponentInChildren<Text>();
        if (label != null) {
            RectTransform textRect = label.GetComponent<RectTransform>();
            textRect.anchorMin = new Vector2(0f, 0f);
            textRect.anchorMax = new Vector2(1f, 0f);
            textRect.offsetMin = new Vector2(9f, 8f);
            textRect.offsetMax = new Vector2(-9f, 42f);
            label.alignment = TextAnchor.MiddleCenter;
            label.fontSize = 15;
            label.resizeTextForBestFit = true;
            label.resizeTextMinSize = 10;
            label.resizeTextMaxSize = 15;
            label.horizontalOverflow = HorizontalWrapMode.Wrap;
            label.verticalOverflow = VerticalWrapMode.Truncate;
            label.raycastTarget = false;
            label.color = isHidden
                ? new Color(0.72f, 0.78f, 0.84f, 0.82f)
                : isPreview ? new Color(0.86f, 0.88f, 0.9f, 0.88f) : Color.white;
            label.transform.SetAsLastSibling();
        }
    }

    private Color ResolveNodeFallbackTint(NodeBase node, DungeonMapNodePresentation presentation = null) {
        if (presentation != null && presentation.IsHidden) {
            return new Color(0.18f, 0.22f, 0.25f, 0.52f);
        }

        if (presentation != null && presentation.IsPreview) {
            return new Color(0.32f, 0.34f, 0.38f, 0.7f);
        }

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
            float height = Mathf.Max(540f, NodeButtonHeight + Mathf.Max(0, maxNodesInRow - 1) * NodeRowSpacing + 220f);
            contentRect.anchorMin = new Vector2(0.5f, 0.5f);
            contentRect.anchorMax = new Vector2(0.5f, 0.5f);
            contentRect.pivot = new Vector2(0.5f, 0.5f);
            contentRect.anchoredPosition = new Vector2(0f, -150f);
            contentRect.localScale = Vector3.one;
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
        mapPathMinX = startX;
        mapPathMaxX = startX + totalWidth;
        for (int rowIndex = 0; rowIndex < rows.Count; rowIndex++) {
            List<NodeBase> row = rows[rowIndex];
            if (row == null || row.Count == 0) {
                continue;
            }

            float depth01 = rows.Count <= 1 ? 0f : rowIndex / (float)(rows.Count - 1);
            float rowSpacing = NodeRowSpacing * Mathf.Lerp(1f, 0.56f, depth01);
            float rowHeight = Mathf.Max(0, row.Count - 1) * rowSpacing;
            float startY = rowHeight * 0.5f;
            float centerY = Mathf.Lerp(42f, -86f, depth01) + Mathf.Sin(depth01 * Mathf.PI * 1.25f) * 34f;
            float curveX = Mathf.Sin(depth01 * Mathf.PI) * 34f;
            for (int columnIndex = 0; columnIndex < row.Count; columnIndex++) {
                NodeBase node = row[columnIndex];
                if (node == null) {
                    continue;
                }

                positions[node] = new Vector2(startX + rowIndex * NodeColumnSpacing + curveX, centerY + startY - columnIndex * rowSpacing);
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
            float depthScale = Mathf.Lerp(1.02f, 0.76f, ResolveNodeDepth01(anchoredPosition.x));
            rect.localScale = new Vector3(depthScale, depthScale, 1f);
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

    private float ResolveNodeDepth01(float x) {
        if (Mathf.Abs(mapPathMaxX - mapPathMinX) < 0.01f) {
            return 0f;
        }

        return Mathf.Clamp01((x - mapPathMinX) / (mapPathMaxX - mapPathMinX));
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

    private void ApplyNodeButtonSkin(Button button, NodeBase node, DungeonMapNodePresentation presentation, bool dimmed) {
        if (button == null) {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image == null) {
            image = button.gameObject.AddComponent<Image>();
        }

        Color fallback = ResolveNodeFallbackTint(node, presentation);
        Color plateColor = BuildNodePlateColor(fallback, 0.5f);
        if (dimmed) {
            fallback = new Color(fallback.r * 0.45f, fallback.g * 0.45f, fallback.b * 0.45f, 0.48f);
            plateColor = BuildNodePlateColor(fallback, 0.42f);
        }

        if (presentation != null && presentation.IsHidden) {
            plateColor = new Color(0.08f, 0.12f, 0.14f, 0.34f);
        } else if (presentation != null && presentation.IsPreview) {
            plateColor = new Color(0.18f, 0.24f, 0.26f, 0.44f);
        }

        VisualUIHelper.ApplySimpleSprite(
            image,
            VisualAssetService.UIDungeonNodePlateID,
            plateColor,
            fallback,
            true,
            false);
        button.targetGraphic = image;
    }

    private Color BuildNodePlateColor(Color fallback, float alpha) {
        return new Color(
            Mathf.Clamp01(fallback.r * 0.52f + 0.08f),
            Mathf.Clamp01(fallback.g * 0.52f + 0.1f),
            Mathf.Clamp01(fallback.b * 0.52f + 0.1f),
            alpha);
    }

    private void ApplyMapBackground() {
        RectTransform rootRect = transform as RectTransform;
        if (rootRect != null) {
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
        }

        backgroundImage = VisualUIHelper.EnsurePanelBackground(transform, backgroundImage, "DungeonMapBackground_Image");
        DungeonLayer currentLayer = GameRoot.Core?.Dungeon?.CurrentLayer;
        bool isT0FirstLayer = currentLayer != null && currentLayer.LayerID == NarrativeCommandBridge.T0FirstDiveLayerID;
        string visualID = isT0FirstLayer
            ? "cg_t0_01a_p04_panel02_shallow_gate_glow"
            : VisualAssetService.ResolveDungeonMapBackgroundID(currentLayer);
        VisualUIHelper.ApplyCoverSprite(backgroundImage, visualID, new Color(0.9f, 0.94f, 0.88f, 1f), new Color(0.05f, 0.08f, 0.1f, 0.92f));

        mapDepthVeilImage = VisualUIHelper.EnsurePanelBackground(transform, mapDepthVeilImage, "DungeonMapDepthVeil_Image");
        VisualUIHelper.ApplySolidColor(mapDepthVeilImage, new Color(0.006f, 0.022f, 0.032f, isT0FirstLayer ? 0.20f : 0.18f));
        backgroundImage.transform.SetAsFirstSibling();
        mapDepthVeilImage.transform.SetSiblingIndex(Mathf.Min(1, transform.childCount - 1));
        ApplyFirstDiveMapGuide(isT0FirstLayer);
    }

    private void ApplyFirstDiveMapGuide(bool visible) {
        if (firstDiveGuideImage == null) {
            Transform existing = transform.Find("FirstDiveMapGuide_Image");
            firstDiveGuideImage = existing != null ? existing.GetComponent<Image>() : null;
        }

        if (firstDiveGuideImage == null) {
            GameObject guideObj = new GameObject("FirstDiveMapGuide_Image");
            guideObj.transform.SetParent(transform, false);
            firstDiveGuideImage = guideObj.AddComponent<Image>();
        }

        firstDiveGuideImage.gameObject.SetActive(visible);
        if (!visible) {
            if (firstDiveGuideText != null) {
                firstDiveGuideText.gameObject.SetActive(false);
            }
            return;
        }

        RectTransform guideRect = firstDiveGuideImage.rectTransform;
        guideRect.anchorMin = new Vector2(0f, 1f);
        guideRect.anchorMax = new Vector2(0f, 1f);
        guideRect.pivot = new Vector2(0f, 1f);
        guideRect.anchoredPosition = new Vector2(36f, -36f);
        guideRect.sizeDelta = new Vector2(520f, 86f);
        VisualUIHelper.ApplySolidColor(firstDiveGuideImage, new Color(0.004f, 0.018f, 0.026f, 0.58f), true);
        firstDiveGuideImage.transform.SetAsLastSibling();

        if (firstDiveGuideText == null) {
            Transform existingText = firstDiveGuideImage.transform.Find("FirstDiveMapGuide_Text");
            firstDiveGuideText = existingText != null ? existingText.GetComponent<Text>() : null;
        }

        if (firstDiveGuideText == null) {
            GameObject textObj = new GameObject("FirstDiveMapGuide_Text");
            textObj.transform.SetParent(firstDiveGuideImage.transform, false);
            firstDiveGuideText = textObj.AddComponent<Text>();
        }

        Font defaultFont = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        firstDiveGuideText.gameObject.SetActive(true);
        firstDiveGuideText.font = defaultFont;
        firstDiveGuideText.fontSize = 22;
        firstDiveGuideText.color = new Color(0.82f, 0.96f, 1f, 0.96f);
        firstDiveGuideText.alignment = TextAnchor.MiddleLeft;
        firstDiveGuideText.horizontalOverflow = HorizontalWrapMode.Wrap;
        firstDiveGuideText.verticalOverflow = VerticalWrapMode.Truncate;
        firstDiveGuideText.raycastTarget = false;
        firstDiveGuideText.text = "\u9996\u6b21\u4e0b\u6f5c\uff1a\u65e7\u77ff\u4e95\u6d45\u7f1d\n\u53ea\u8d70\u5230\u7b2c\u4e00\u5904\u8def\u6807\uff0c\u96f6\u53f7\u5f02\u5e38\u5c31\u64a4\u56de\u3002";
        RectTransform textRect = firstDiveGuideText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(24f, 10f);
        textRect.offsetMax = new Vector2(-24f, -10f);
    }

    private void CreateRouteLines(DungeonLayer layer, List<List<NodeBase>> rows, Dictionary<NodeBase, Vector2> nodePositions) {
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

                    if (!DungeonMapVisibilityService.ShouldRenderRouteLine(layer, node, next)) {
                        continue;
                    }

                    CreateRouteLine(layer, node, next, fromPosition, toPosition);
                }
            }
        }
    }

    private void CreateRouteLine(DungeonLayer layer, NodeBase fromNode, NodeBase toNode, Vector2 fromPosition, Vector2 toPosition) {
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

        Color routeColor = ResolveRouteLineFallbackColor(layer, fromNode, toNode);
        VisualUIHelper.ApplySimpleSprite(
            routeImage,
            VisualAssetService.UIDungeonRouteLineID,
            routeColor,
            routeColor,
            false,
            false);
        routeObj.transform.SetAsFirstSibling();

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

    private Color ResolveRouteLineFallbackColor(DungeonLayer layer, NodeBase fromNode, NodeBase toNode) {
        DungeonNodeVisibilityState fromVisibility = DungeonMapVisibilityService.ResolveVisibility(layer, fromNode);
        DungeonNodeVisibilityState toVisibility = DungeonMapVisibilityService.ResolveVisibility(layer, toNode);
        if (fromVisibility == DungeonNodeVisibilityState.Hidden || toVisibility == DungeonNodeVisibilityState.Hidden) {
            return new Color(0.18f, 0.22f, 0.25f, 0.22f);
        }

        if (fromVisibility == DungeonNodeVisibilityState.Preview || toVisibility == DungeonNodeVisibilityState.Preview) {
            return new Color(0.42f, 0.5f, 0.48f, 0.34f);
        }

        return new Color(0.58f, 0.68f, 0.56f, 0.48f);
    }

    private Image EnsureNodeChildImage(Transform parent, string objectName) {
        Transform existing = parent.Find(objectName);
        Image image = existing != null ? existing.GetComponent<Image>() : null;
        return image != null ? image : CreateNodeChildImage(parent, objectName);
    }

    private Image CreateNodeChildImage(Transform parent, string objectName) {
        GameObject imageObj = new GameObject(objectName, typeof(RectTransform));
        imageObj.transform.SetParent(parent, false);
        Image image = imageObj.AddComponent<Image>();
        image.raycastTarget = false;
        return image;
    }
}
