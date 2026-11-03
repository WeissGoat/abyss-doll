using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using System.Collections.Generic;

// The layer's route map (docs/specs/2026-10-28-dungeon-map-redesign.md): the layer's painted terrain, bigger than the
// screen and scrolled by dragging or by following Zero, with the run's node network laid on it by code - trodden paths
// between nodes, and each node a brass medallion on a stone pad. What lies more than the fog allows ahead sinks into mist.
public class DungeonMapUIController : MonoBehaviour {
    // Map space, in canvas units (the screen is 1920x1080).
    private const float NodeButtonSize = 128f;
    private const float MedallionSize = 92f;
    private const float GlyphSize = 50f;
    private const float GlowSize = 138f;
    private const float PadWidth = 150f;
    private const float PadHeight = 92f;
    private const float ColumnSpacing = 280f;
    private const float MinColumnSpacing = 230f;
    private const float RowSpacing = 185f;
    private const float WorldPaddingX = 320f;
    private const float WorldPaddingY = 260f;
    private const float RouteLineHeight = 44f;
    private const float RouteGlowHeight = 5f;
    private const float FollowSeconds = 0.45f;
    private const string PrologueMapVisualID = "cg_t0_01a_p04_panel02_shallow_gate_glow";
    // The terrain's ground mask (see MapGround) is registered as the terrain's ID plus this.
    private const string GroundSuffix = "_ground";
    private const float GroundMarginY = 90f;

    public GameObject nodeButtonPrefab;
    public Transform contentParent;
    public Button openBackpackBtn;
    public Button closeBackpackBtn;
    public Text backpackHintText;
    public Image backgroundImage;
    private Image firstDiveGuideImage;
    private Text firstDiveGuideText;
    private Text mapHintText;
    private Image runStatusPlate;
    private Text runStatusText;
    private ScrollRect mapScroll;
    private RectTransform mapWorld;
    private Image terrainImage;
    private string cachedGroundID;
    private MapGround cachedGround;
    private Image fogImage;
    private RectTransform tooltip;
    private Text tooltipText;
    private Vector2 followFrom;
    private Vector2 followTo;
    private float followTime = -1f;
    private int shownLayerID = -1;
    private int shownRunSeed;
    // The how-to lines (where you stand, how to open the bag) only stay up for the first dives.
    private const int BeginnerDiveCount = 2;
    // Nodes the player can still get to from where they stand; everything else on the layer is behind them.
    private readonly HashSet<NodeBase> reachableNodes = new HashSet<NodeBase>();

    public void RefreshMap() {
        EnsureInventoryControls();
        ApplyMapBackground();

        if (contentParent == null || nodeButtonPrefab == null) {
            return;
        }

        EnsureMapWorld();
        ClearNodeButtons();
        HideTooltip();

        DungeonLayer layer = GameRoot.Core.Dungeon.CurrentLayer;
        if (layer == null || layer.RootNode == null) {
            return;
        }

        List<List<NodeBase>> rows = GetRenderableRows(layer);
        CollectReachableNodes(layer);
        ApplyMapHint(layer);
        ApplyRunStatus(layer);
        MapGround ground = ResolveGround(layer);
        Vector2 worldSize = ResolveWorldSize(layer, rows, ground);
        Dictionary<NodeBase, Vector2> nodePositions = BuildNodePositions(rows, worldSize, ground);
        ConfigureWorld(layer, rows, nodePositions, worldSize);
        CreateRouteLines(layer, rows, nodePositions);

        foreach (List<NodeBase> row in rows) {
            foreach (NodeBase node in row) {
                CreateNodeButton(layer, node, nodePositions);
            }
        }

        FocusOnCurrentPosition(layer, nodePositions);
    }

    private void Update() {
        if (followTime < 0f || mapWorld == null) {
            return;
        }

        followTime += Time.unscaledDeltaTime;
        float t = Mathf.Clamp01(followTime / FollowSeconds);
        mapWorld.anchoredPosition = Vector2.Lerp(followFrom, followTo, 1f - (1f - t) * (1f - t));
        if (t >= 1f) {
            followTime = -1f;
        }
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

    // ---- The scrolling map ----

    // Full-screen viewport (clipped, so the map beyond the screen counts as hidden, not off screen) holding the map:
    // terrain, the mist, then MapLayout with the routes and node buttons.
    private void EnsureMapWorld() {
        if (mapScroll == null) {
            Transform existing = transform.Find("MapViewport");
            mapScroll = existing != null ? existing.GetComponent<ScrollRect>() : null;
        }

        if (mapScroll == null) {
            GameObject viewportObj = new GameObject("MapViewport", typeof(RectTransform), typeof(RectMask2D), typeof(ScrollRect));
            viewportObj.transform.SetParent(transform, false);
            UITheme.Stretch((RectTransform)viewportObj.transform);
            mapScroll = viewportObj.GetComponent<ScrollRect>();

            GameObject worldObj = new GameObject("MapWorld", typeof(RectTransform));
            worldObj.transform.SetParent(viewportObj.transform, false);
            mapWorld = (RectTransform)worldObj.transform;
            mapWorld.anchorMin = mapWorld.anchorMax = mapWorld.pivot = new Vector2(0.5f, 0.5f);

            terrainImage = new GameObject("MapTerrain_Image", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            terrainImage.transform.SetParent(mapWorld, false);
            UITheme.Stretch(terrainImage.rectTransform);
            fogImage = new GameObject("MapFog_Image", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            fogImage.transform.SetParent(mapWorld, false);
            fogImage.raycastTarget = false;

            mapScroll.viewport = (RectTransform)viewportObj.transform;
            mapScroll.content = mapWorld;
            mapScroll.horizontal = true;
            mapScroll.vertical = true;
            mapScroll.movementType = ScrollRect.MovementType.Clamped;
            mapScroll.inertia = true;
            mapScroll.decelerationRate = 0.08f;
            mapScroll.scrollSensitivity = 40f;
        }

        mapWorld = mapScroll.content;
        terrainImage = mapWorld.Find("MapTerrain_Image")?.GetComponent<Image>();
        fogImage = mapWorld.Find("MapFog_Image")?.GetComponent<Image>();

        // The scene's MapLayout becomes the layer above terrain and mist; buttons stay its direct children (P3DevTools).
        if (contentParent.parent != mapWorld) {
            contentParent.SetParent(mapWorld, false);
        }

        RectTransform layoutRect = contentParent as RectTransform;
        if (layoutRect != null) {
            UITheme.Stretch(layoutRect);
            layoutRect.localScale = Vector3.one;
        }

        HorizontalLayoutGroup layout = contentParent.GetComponent<HorizontalLayoutGroup>();
        if (layout != null) {
            layout.enabled = false;
            layout.childControlWidth = false;
            layout.childControlHeight = false;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;
        }

        terrainImage.transform.SetSiblingIndex(0);
        fogImage.transform.SetSiblingIndex(1);
        contentParent.SetSiblingIndex(2);
        mapScroll.transform.SetSiblingIndex(Mathf.Min(1, transform.childCount - 1));
    }

    private string ResolveTerrainVisualID(DungeonLayer layer) {
        return PrologueShallowRun.IsPrologueLayer(layer) ? PrologueMapVisualID : VisualAssetService.ResolveDungeonMapBackgroundID(layer);
    }

    private MapGround ResolveGround(DungeonLayer layer) {
        string id = ResolveTerrainVisualID(layer) + GroundSuffix;
        if (id == cachedGroundID) {
            return cachedGround;
        }

        cachedGroundID = id;
        cachedGround = VisualAssetService.TryGetSprite(id, out Sprite mask) ? MapGround.FromTexture(mask.texture) : null;
        if (cachedGround != null && !cachedGround.HasGround) {
            cachedGround = null;
        }

        return cachedGround;
    }

    // The painting's own shape, grown when the network needs more room (on a terrain with a ground mask, room within
    // the ground's span); never smaller than the screen.
    private Vector2 ResolveWorldSize(DungeonLayer layer, List<List<NodeBase>> rows, MapGround ground) {
        Vector2 painting = VisualAssetService.TryGetSprite(ResolveTerrainVisualID(layer), out Sprite sprite)
            ? sprite.rect.size
            : new Vector2(1920f, 1080f);
        int maxNodesInColumn = 1;
        foreach (List<NodeBase> row in rows) {
            maxNodesInColumn = Mathf.Max(maxNodesInColumn, row?.Count ?? 0);
        }

        float neededWidth = ground != null
            ? (Mathf.Max(0, rows.Count - 1) * MinColumnSpacing + NodeButtonSize) / (ground.MaxU - ground.MinU)
            : WorldPaddingX * 2f + Mathf.Max(0, rows.Count - 1) * MinColumnSpacing;
        float neededHeight = WorldPaddingY * 2f + Mathf.Max(0, maxNodesInColumn - 1) * RowSpacing;
        float scale = Mathf.Max(1f, neededWidth / painting.x, neededHeight / painting.y, 1920f / painting.x, 1080f / painting.y);
        return painting * scale;
    }

    // Depth runs left to right, one column per row of the network. Columns spread over the painting up to
    // ColumnSpacing apart; each column sways a little, and every node is nudged by its own ID so the routes look walked
    // rather than ruled. With a ground mask the columns spread over the ground's span instead, each centred on the
    // ground under it, and nodes still off the ground step onto the nearest ground (MapGround.Settle).
    private Dictionary<NodeBase, Vector2> BuildNodePositions(List<List<NodeBase>> rows, Vector2 worldSize, MapGround ground) {
        Dictionary<NodeBase, Vector2> positions = new Dictionary<NodeBase, Vector2>();
        if (rows == null || rows.Count == 0) {
            return positions;
        }

        float spanStart = ground != null ? (ground.MinU - 0.5f) * worldSize.x + NodeButtonSize * 0.5f : -worldSize.x * 0.5f + WorldPaddingX;
        float span = ground != null ? (ground.MaxU - ground.MinU) * worldSize.x - NodeButtonSize : worldSize.x - WorldPaddingX * 2f;
        float spacing = rows.Count <= 1 ? 0f : Mathf.Min(ColumnSpacing, span / (rows.Count - 1));
        float startX = spanStart + (span - (rows.Count - 1) * spacing) * 0.5f;
        for (int column = 0; column < rows.Count; column++) {
            List<NodeBase> row = rows[column];
            if (row == null) {
                continue;
            }

            float columnX = startX + column * spacing;
            float sway = ground != null
                ? (ground.CenterV(columnX / worldSize.x + 0.5f) - 0.5f) * worldSize.y
                : Mathf.Sin(column * 0.85f + 0.6f) * 70f;
            float top = (row.Count - 1) * RowSpacing * 0.5f;
            for (int index = 0; index < row.Count; index++) {
                NodeBase node = row[index];
                if (node == null) {
                    continue;
                }

                int hash = StableHash(node.NodeID);
                Vector2 jitter = new Vector2((hash % 61) - 30f, ((hash / 61) % 45) - 22f);
                positions[node] = new Vector2(columnX, sway + top - index * RowSpacing) + jitter;
            }
        }

        ground?.Settle(positions, worldSize, spacing * 0.4f, NodeButtonSize * 1.05f, GroundMarginY);
        return positions;
    }

    private static int StableHash(string text) {
        unchecked {
            int hash = 17;
            foreach (char c in text ?? string.Empty) {
                hash = hash * 31 + c;
            }

            return hash & 0x7fffffff;
        }
    }

    // Terrain at the map's size; the mist covers everything from the first column the fog still hides to the far end.
    private void ConfigureWorld(DungeonLayer layer, List<List<NodeBase>> rows, Dictionary<NodeBase, Vector2> positions, Vector2 worldSize) {
        mapWorld.sizeDelta = worldSize;
        VisualUIHelper.ApplySimpleSprite(terrainImage, ResolveTerrainVisualID(layer), Color.white, new Color(0.05f, 0.08f, 0.1f, 1f), true, false);

        float fogStartX = float.MaxValue;
        for (int column = 0; column < rows.Count; column++) {
            bool allHidden = rows[column] != null && rows[column].Count > 0;
            float columnX = float.MaxValue;
            foreach (NodeBase node in rows[column]) {
                allHidden &= DungeonMapVisibilityService.ResolveVisibility(layer, node) == DungeonNodeVisibilityState.Hidden;
                if (positions.TryGetValue(node, out Vector2 position)) {
                    columnX = Mathf.Min(columnX, position.x);
                }
            }

            if (allHidden && columnX < float.MaxValue) {
                fogStartX = columnX - ColumnSpacing * 0.6f;
                break;
            }
        }

        bool fogged = fogStartX < float.MaxValue;
        fogImage.gameObject.SetActive(fogged);
        if (fogged) {
            RectTransform fogRect = fogImage.rectTransform;
            fogRect.anchorMin = new Vector2(0f, 0f);
            fogRect.anchorMax = new Vector2(0f, 1f);
            fogRect.pivot = new Vector2(0f, 0.5f);
            float left = fogStartX + worldSize.x * 0.5f;
            fogRect.anchoredPosition = new Vector2(left, 0f);
            fogRect.sizeDelta = new Vector2(Mathf.Max(0f, worldSize.x - left), 0f);
            VisualUIHelper.ApplySimpleSprite(fogImage, VisualAssetService.UIMapFogID, Color.white, new Color(0.03f, 0.05f, 0.07f, 0.72f), false, false);
            // A soft gradient meant to stretch over whatever the fog covers; a borderless sliced image draws the same
            // and tells LayoutCheck the stretch is wanted.
            fogImage.type = Image.Type.Sliced;
        }

        RebuildMapLayout();
    }

    // Brings Zero's node (or the entrances before her first step) to the middle of the screen: at once for a new map,
    // gliding there when she has just moved.
    private void FocusOnCurrentPosition(DungeonLayer layer, Dictionary<NodeBase, Vector2> positions) {
        Vector2 focus = Vector2.zero;
        int count = 0;
        if (layer.CurrentNode != null && positions.TryGetValue(layer.CurrentNode, out Vector2 current)) {
            focus = current + new Vector2(ColumnSpacing * 0.9f, 0f);
            count = 1;
        } else if (layer.EntryNodes != null) {
            foreach (NodeBase entry in layer.EntryNodes) {
                if (entry != null && positions.TryGetValue(entry, out Vector2 position)) {
                    focus += position;
                    count++;
                }
            }

            focus = count > 0 ? focus / count + new Vector2(ColumnSpacing * 0.9f, 0f) : focus;
        }

        Vector2 target = ClampWorldPosition(-focus);
        bool sameMap = shownLayerID == layer.LayerID && shownRunSeed == layer.RunSeed;
        shownLayerID = layer.LayerID;
        shownRunSeed = layer.RunSeed;
        mapScroll.StopMovement();
        if (!sameMap || !Application.isPlaying) {
            mapWorld.anchoredPosition = target;
            followTime = -1f;
            return;
        }

        followFrom = ClampWorldPosition(mapWorld.anchoredPosition);
        followTo = target;
        followTime = 0f;
    }

    private Vector2 ClampWorldPosition(Vector2 position) {
        RectTransform viewport = mapScroll.viewport;
        Vector2 view = viewport != null && viewport.rect.width > 1f ? viewport.rect.size : new Vector2(1920f, 1080f);
        Vector2 slack = Vector2.Max(Vector2.zero, (mapWorld.sizeDelta - view) * 0.5f);
        return new Vector2(Mathf.Clamp(position.x, -slack.x, slack.x), Mathf.Clamp(position.y, -slack.y, slack.y));
    }

    // ---- Nodes ----

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
        bool isCurrent = node == layer.CurrentNode;
        bool isUnreachable = IsNodeUnreachable(layer, node);

        // The label stays on the button for tools and tests (P3DevTools reads it); on screen the medallion says it all.
        if (txt != null) {
            txt.text = BuildNodeLabel(node, presentation);
            txt.enabled = false;
            txt.raycastTarget = false;
        }

        ApplyNodeMedallion(btnGo.transform, node, presentation, isCurrent, isSelectable, isUnreachable);
        BindNodeTooltip(btnGo, layer, node, presentation, isSelectable);

        Debug.Log($"[DungeonMapUI] Render node button: {node.NodeID}, Type={node.GetType().Name}, Label={txt?.text?.Replace('\n', ' ')}");

        if (btn == null) {
            return;
        }

        Image hitArea = btn.GetComponent<Image>();
        if (hitArea == null) {
            hitArea = btn.gameObject.AddComponent<Image>();
        }

        VisualUIHelper.ApplySolidColor(hitArea, new Color(1f, 1f, 1f, 0f), true);
        btn.targetGraphic = btnGo.transform.Find("NodeMedallion_Image")?.GetComponent<Image>() ?? hitArea;
        btn.transition = Selectable.Transition.ColorTint;
        ColorBlock colors = btn.colors;
        colors.normalColor = Color.white;
        colors.highlightedColor = new Color(1.12f, 1.08f, 0.98f, 1f);
        colors.pressedColor = new Color(0.82f, 0.78f, 0.7f, 1f);
        colors.disabledColor = Color.white;
        colors.colorMultiplier = 1f;
        btn.colors = colors;

        if (!isSelectable || node.IsVisited) {
            btn.interactable = false;
            return;
        }

        btn.interactable = true;
        btn.onClick.AddListener(() => {
            HideTooltip();
            GameRoot.Core.Dungeon.MoveToNode(node);
        });
    }

    private void ConfigureNodeButtonLayout(GameObject buttonObject, Vector2 anchoredPosition) {
        RectTransform rect = buttonObject.GetComponent<RectTransform>();
        if (rect != null) {
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
            rect.anchoredPosition = anchoredPosition;
            rect.sizeDelta = new Vector2(NodeButtonSize, NodeButtonSize);
            rect.localScale = Vector3.one;
        }

        LayoutElement layoutElement = buttonObject.GetComponent<LayoutElement>();
        if (layoutElement != null) {
            layoutElement.ignoreLayout = true;
        }
    }

    // Stone pad, glow ring (the next steps), brass medallion, the node's glyph and, over Zero's node, a gold arrow.
    // Fog-hidden nodes are dark empty medallions; nodes passed are dulled; branches she can no longer reach fade out.
    private void ApplyNodeMedallion(Transform button, NodeBase node, DungeonMapNodePresentation presentation, bool isCurrent, bool isSelectable, bool isUnreachable) {
        bool isHidden = presentation != null && presentation.IsHidden;
        bool isPreview = presentation != null && presentation.IsPreview;
        bool isPassed = node.IsVisited && !isCurrent;

        Image pad = EnsureNodeChildImage(button, "NodePad_Image");
        Place(pad.rectTransform, new Vector2(0f, -30f), new Vector2(PadWidth, PadHeight));
        VisualUIHelper.ApplySimpleSprite(pad, VisualAssetService.UIMapNodePadID, new Color(1f, 1f, 1f, isHidden ? 0.45f : 1f), new Color(0.32f, 0.3f, 0.26f, 0.8f), false, true);

        Image glow = EnsureNodeChildImage(button, "NodeGlow_Image");
        Place(glow.rectTransform, Vector2.zero, new Vector2(GlowSize, GlowSize));
        glow.gameObject.SetActive(isSelectable);
        if (isSelectable) {
            VisualUIHelper.ApplySimpleSprite(glow, VisualAssetService.UIMapNodeGlowID, new Color(0.55f, 1f, 0.95f, 1f), new Color(0.45f, 0.95f, 0.9f, 0.6f), false, true);
            UIPulse.Attach(glow.gameObject, 0.45f, 1f, 0.05f, 2.8f);
        }

        Image medallion = EnsureNodeChildImage(button, "NodeMedallion_Image");
        float medallionScale = isCurrent ? 1.1f : 1f;
        Place(medallion.rectTransform, Vector2.zero, Vector2.one * MedallionSize * medallionScale);
        Color medallionTint = isHidden ? new Color(0.36f, 0.4f, 0.44f, 0.82f)
            : isPassed ? new Color(0.62f, 0.58f, 0.52f, 1f)
            : isPreview ? new Color(0.82f, 0.82f, 0.8f, 1f)
            : Color.white;
        VisualUIHelper.ApplySimpleSprite(medallion, VisualAssetService.UIMapMedallionID, medallionTint, new Color(0.4f, 0.28f, 0.14f, 1f), false, true);

        Image glyph = EnsureNodeChildImage(button, "NodeIcon_Image");
        Place(glyph.rectTransform, Vector2.zero, Vector2.one * GlyphSize * medallionScale);
        glyph.gameObject.SetActive(!isHidden);
        if (!isHidden) {
            float glyphAlpha = isPassed ? 0.55f : isPreview ? 0.7f : 1f;
            VisualUIHelper.ApplySimpleSprite(glyph, VisualAssetService.ResolveMapGlyphID(node), new Color(1f, 1f, 1f, glyphAlpha), ResolveNodeFallbackTint(node), false, true);
        }

        Image arrow = EnsureNodeChildImage(button, "NodeCurrentArrow_Image");
        Place(arrow.rectTransform, new Vector2(0f, MedallionSize * 0.5f + 38f), new Vector2(54f, 54f));
        arrow.gameObject.SetActive(isCurrent);
        if (isCurrent) {
            VisualUIHelper.ApplySimpleSprite(arrow, VisualAssetService.UIMapCurrentArrowID, Color.white, UITheme.AccentOnDark, false, true);
            UIPulse.Attach(arrow.gameObject, 0.75f, 1f, 0.1f, 3f);
        }

        // Children paint in order: pad, glow, medallion, glyph, arrow; the hidden label last.
        pad.transform.SetSiblingIndex(0);
        glow.transform.SetSiblingIndex(1);
        medallion.transform.SetSiblingIndex(2);
        glyph.transform.SetSiblingIndex(3);
        arrow.transform.SetSiblingIndex(4);

        CanvasGroup group = button.GetComponent<CanvasGroup>();
        if (group == null) {
            group = button.gameObject.AddComponent<CanvasGroup>();
        }

        group.alpha = isUnreachable ? 0.4f : 1f;
    }

    private static void Place(RectTransform rect, Vector2 position, Vector2 size) {
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0.5f, 0.5f);
        rect.anchoredPosition = position;
        rect.sizeDelta = size;
        rect.localScale = Vector3.one;
    }

    private string BuildNodeLabel(NodeBase node, DungeonMapNodePresentation presentation) {
        if (presentation != null && presentation.IsHidden) {
            return "未知";
        }

        string riskLabel = CompactRiskLabel(presentation?.RiskLabel);
        string nodeLabel = BuildNodeTypeLabel(node);
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
        if (node is PrologueBeatNode beat) {
            return beat.DisplayLabel;
        }

        if (node is CombatNode) {
            if (VisualAssetService.ResolveNodeIconID(node) == VisualAssetService.BossNodeIconID) {
                return "首领";
            }

            return "战斗";
        }

        if (node is SafeRoomNode) {
            return "升降梯";
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

        if (node is TreasureNode || node is PrologueBringBackNode) {
            return new Color(0.86f, 0.64f, 0.2f, 1f);
        }

        if (node is RestStopNode) {
            return new Color(0.26f, 0.58f, 0.68f, 1f);
        }

        if (node is EventNode || node is PrologueVoiceNode) {
            return new Color(0.55f, 0.43f, 0.7f, 1f);
        }

        if (node is HazardNode) {
            return new Color(0.62f, 0.26f, 0.2f, 1f);
        }

        return Color.white;
    }

    // ---- Tooltip: what a node is, how risky, and what stepping there costs ----

    private void BindNodeTooltip(GameObject button, DungeonLayer layer, NodeBase node, DungeonMapNodePresentation presentation, bool isSelectable) {
        EventTrigger trigger = button.GetComponent<EventTrigger>();
        if (trigger == null) {
            trigger = button.AddComponent<EventTrigger>();
        }

        trigger.triggers.Clear();
        EventTrigger.Entry enter = new EventTrigger.Entry { eventID = EventTriggerType.PointerEnter };
        enter.callback.AddListener(_ => ShowTooltip(button.transform as RectTransform, BuildTooltip(layer, node, presentation, isSelectable)));
        trigger.triggers.Add(enter);
        EventTrigger.Entry exit = new EventTrigger.Entry { eventID = EventTriggerType.PointerExit };
        exit.callback.AddListener(_ => HideTooltip());
        trigger.triggers.Add(exit);
    }

    private string BuildTooltip(DungeonLayer layer, NodeBase node, DungeonMapNodePresentation presentation, bool isSelectable) {
        if (presentation != null && presentation.IsHidden) {
            return "雾里看不清";
        }

        string title = BuildNodeTypeLabel(node);
        string risk = CompactRiskLabel(presentation?.RiskLabel);
        string text = string.IsNullOrEmpty(risk) ? $"<b>{title}</b>" : $"<b>{title}</b> · {risk}";
        if (!string.IsNullOrEmpty(presentation?.RiskHint)) {
            text += $"\n{presentation.RiskHint}";
        }

        if (node == layer.CurrentNode) {
            text += "\n零号在这里";
        } else if (node.IsVisited) {
            text += "\n已经走过";
        } else if (isSelectable) {
            int cost = GameRoot.Core.Dungeon.PreviewSanCost(node);
            text += cost > 0 ? $"\n走过去 -{cost} SAN" : "\n走过去不耗 SAN";
        } else if (IsNodeUnreachable(layer, node)) {
            text += "\n从这里已经走不到";
        }

        return text;
    }

    private void ShowTooltip(RectTransform node, string text) {
        if (node == null || string.IsNullOrEmpty(text)) {
            return;
        }

        if (tooltip == null) {
            Image plate = new GameObject("MapNodeTooltip_Image", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            plate.transform.SetParent(mapWorld, false);
            VisualUIHelper.ApplySlicedSprite(plate, VisualAssetService.UIMapStatusPlateID, Color.white, new Color(0.08f, 0.06f, 0.04f, 0.92f), false);
            tooltip = plate.rectTransform;
            tooltip.pivot = new Vector2(0.5f, 0f);
            tooltipText = UITheme.CreateText(plate.transform, "MapNodeTooltip_Text", string.Empty, UITheme.CaptionSize, UITheme.TextOnDark, TextAnchor.MiddleCenter);
            tooltipText.supportRichText = true;
            tooltipText.raycastTarget = false;
            UITheme.Stretch(tooltipText.rectTransform);
            tooltipText.rectTransform.offsetMin = new Vector2(26f, 12f);
            tooltipText.rectTransform.offsetMax = new Vector2(-26f, -12f);
        }

        tooltip.gameObject.SetActive(true);
        tooltipText.text = text;
        float width = Mathf.Clamp(tooltipText.preferredWidth + 56f, 160f, 420f);
        tooltip.sizeDelta = new Vector2(width, tooltipText.preferredHeight + 28f);
        tooltip.anchorMin = tooltip.anchorMax = new Vector2(0.5f, 0.5f);
        tooltip.anchoredPosition = node.anchoredPosition + new Vector2(0f, NodeButtonSize * 0.5f + 18f);
        tooltip.SetAsLastSibling();
    }

    private void HideTooltip() {
        if (tooltip != null) {
            tooltip.gameObject.SetActive(false);
        }
    }

    // ---- Routes: trodden paths between nodes ----

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

    // One path strip from node to node, tiled along its length; the steps open from Zero's node also get a thin glowing
    // thread on top.
    private void CreateRouteLine(DungeonLayer layer, NodeBase fromNode, NodeBase toNode, Vector2 fromPosition, Vector2 toPosition) {
        GameObject routeObj = new GameObject("DungeonRouteLine_Image", typeof(RectTransform));
        routeObj.transform.SetParent(contentParent, false);
        Image routeImage = routeObj.AddComponent<Image>();

        Vector2 delta = toPosition - fromPosition;
        RectTransform routeRect = (RectTransform)routeObj.transform;
        routeRect.anchorMin = routeRect.anchorMax = routeRect.pivot = new Vector2(0.5f, 0.5f);
        routeRect.anchoredPosition = (fromPosition + toPosition) * 0.5f;
        routeRect.sizeDelta = new Vector2(Mathf.Max(16f, delta.magnitude), RouteLineHeight);
        routeRect.localEulerAngles = new Vector3(0f, 0f, Mathf.Atan2(delta.y, delta.x) * Mathf.Rad2Deg);

        string pathID = VisualAssetService.ResolveMapPathID(layer);
        Color routeColor = ResolveRouteColor(layer, fromNode, toNode);
        bool hasPath = VisualUIHelper.ApplySimpleSprite(routeImage, pathID, routeColor, new Color(0.58f, 0.5f, 0.38f, routeColor.a * 0.7f), false, false);
        if (hasPath && routeImage.sprite != null) {
            routeImage.type = Image.Type.Tiled;
            routeImage.pixelsPerUnitMultiplier = routeImage.sprite.rect.height / RouteLineHeight * (100f / routeImage.sprite.pixelsPerUnit);
        }

        if (fromNode == layer?.CurrentNode && IsNodeSelectable(layer, toNode)
            || layer?.CurrentNode == null && IsNodeSelectable(layer, toNode) && fromNode != null && fromNode.IsVisited) {
            Image thread = new GameObject("RouteThread_Image", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            thread.transform.SetParent(routeObj.transform, false);
            RectTransform threadRect = thread.rectTransform;
            threadRect.anchorMin = new Vector2(0f, 0.5f);
            threadRect.anchorMax = new Vector2(1f, 0.5f);
            threadRect.sizeDelta = new Vector2(-NodeButtonSize * 0.7f, RouteGlowHeight);
            VisualUIHelper.ApplySolidColor(thread, new Color(0.55f, 1f, 0.95f, 0.8f));
            UIPulse.Attach(thread.gameObject, 0.5f, 1f, 0f, 2.8f);
        }

        routeObj.transform.SetAsFirstSibling();
    }

    // The walked path stays bright, routes still open read normally, what is behind or out of reach goes dark.
    private Color ResolveRouteColor(DungeonLayer layer, NodeBase fromNode, NodeBase toNode) {
        if (fromNode != null && toNode != null && fromNode.IsVisited && (toNode.IsVisited || toNode == layer?.CurrentNode)) {
            return new Color(1f, 0.92f, 0.76f, 1f);
        }

        if (fromNode == layer?.CurrentNode && IsNodeSelectable(layer, toNode)) {
            return Color.white;
        }

        if (IsNodeUnreachable(layer, fromNode) || IsNodeUnreachable(layer, toNode) || (fromNode != null && fromNode.IsVisited && fromNode != layer?.CurrentNode)) {
            return new Color(0.55f, 0.55f, 0.55f, 0.38f);
        }

        DungeonNodeVisibilityState fromVisibility = DungeonMapVisibilityService.ResolveVisibility(layer, fromNode);
        DungeonNodeVisibilityState toVisibility = DungeonMapVisibilityService.ResolveVisibility(layer, toNode);
        if (fromVisibility == DungeonNodeVisibilityState.Hidden || toVisibility == DungeonNodeVisibilityState.Hidden) {
            return new Color(0.6f, 0.62f, 0.66f, 0.35f);
        }

        if (fromVisibility == DungeonNodeVisibilityState.Preview || toVisibility == DungeonNodeVisibilityState.Preview) {
            return new Color(0.85f, 0.85f, 0.85f, 0.7f);
        }

        return new Color(0.92f, 0.92f, 0.92f, 0.92f);
    }

    // ---- Reachability ----

    private void CollectReachableNodes(DungeonLayer layer) {
        reachableNodes.Clear();
        Queue<NodeBase> pending = new Queue<NodeBase>();
        if (layer?.CurrentNode != null) {
            pending.Enqueue(layer.CurrentNode);
        } else if (layer?.EntryNodes != null && layer.EntryNodes.Count > 0) {
            foreach (NodeBase entry in layer.EntryNodes) {
                pending.Enqueue(entry);
            }
        } else if (layer?.RootNode != null) {
            pending.Enqueue(layer.RootNode);
        }

        while (pending.Count > 0) {
            NodeBase node = pending.Dequeue();
            if (node == null || !reachableNodes.Add(node) || node.NextNodes == null) {
                continue;
            }

            foreach (NodeBase next in node.NextNodes) {
                pending.Enqueue(next);
            }
        }
    }

    private bool IsNodeUnreachable(DungeonLayer layer, NodeBase node) {
        return node != null && !node.IsVisited && node != layer?.CurrentNode && !reachableNodes.Contains(node);
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

    // ---- Screen furniture: hint, promise, status plate, backpack ----

    private void ApplyMapHint(DungeonLayer layer) {
        if (mapHintText == null) {
            Transform existing = transform.Find("MapHint_Text");
            mapHintText = existing != null ? existing.GetComponent<Text>() : null;
        }

        if (mapHintText == null) {
            mapHintText = UITheme.CreateText(transform, "MapHint_Text", string.Empty, UITheme.BodySize, UITheme.TextOnDark, TextAnchor.MiddleCenter);
            RectTransform hintRect = mapHintText.rectTransform;
            hintRect.anchorMin = new Vector2(0.5f, 1f);
            hintRect.anchorMax = new Vector2(0.5f, 1f);
            hintRect.pivot = new Vector2(0.5f, 1f);
            hintRect.anchoredPosition = new Vector2(0f, -28f);
            hintRect.sizeDelta = new Vector2(760f, 40f);
            Outline outline = mapHintText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        mapHintText.text = !IsBeginner() ? string.Empty
            : layer?.CurrentNode == null
                ? "点一个发光的节点出发；拖动地图可以看更远"
                : "金色箭头是零号的位置；点发光的节点继续深入";
        mapHintText.transform.SetAsLastSibling();
        ApplyPromiseLine();
    }

    // The promise made to Zero last night (PromiseService), kept in view for the whole dive.
    private void ApplyPromiseLine() {
        Transform existing = transform.Find("Promise_Text");
        Text promise = existing != null ? existing.GetComponent<Text>() : null;
        if (promise == null) {
            promise = UITheme.CreateText(transform, "Promise_Text", string.Empty, UITheme.BodySize - 2, new Color(1f, 0.86f, 0.58f), TextAnchor.MiddleLeft);
            RectTransform rect = promise.rectTransform;
            rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(44f, -176f);
            rect.sizeDelta = new Vector2(760f, 40f);
            Outline outline = promise.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        promise.text = PromiseService.MapLine(GameRoot.Core?.CurrentPlayer);
        promise.gameObject.SetActive(!string.IsNullOrEmpty(promise.text));
        promise.transform.SetAsLastSibling();
    }

    private static bool IsBeginner() {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        return player == null || player.DiveJournal == null || player.DiveJournal.Count < BeginnerDiveCount;
    }

    // Top-left plate: Zero's state and what the next step costs, the nearest exit in sight and what she carries out if
    // she turns back now (DungeonRunStatusService), so pushing on or heading for an exit is decided on the map itself.
    // The prologue's shallow run keeps its own guide in that corner instead.
    private void ApplyRunStatus(DungeonLayer layer) {
        if (runStatusPlate == null) {
            Transform existing = transform.Find("RunStatus_Image");
            runStatusPlate = existing != null ? existing.GetComponent<Image>() : null;
        }

        if (runStatusPlate == null) {
            GameObject plateObj = new GameObject("RunStatus_Image", typeof(RectTransform));
            plateObj.transform.SetParent(transform, false);
            runStatusPlate = plateObj.AddComponent<Image>();
            RectTransform plateRect = runStatusPlate.rectTransform;
            plateRect.anchorMin = plateRect.anchorMax = plateRect.pivot = new Vector2(0f, 1f);
            plateRect.anchoredPosition = new Vector2(28f, -28f);
            plateRect.sizeDelta = new Vector2(640f, 136f);
            runStatusText = UITheme.CreateText(runStatusPlate.transform, "RunStatus_Text", string.Empty, UITheme.BodySize - 2, UITheme.TextOnDark, TextAnchor.MiddleLeft);
            runStatusText.supportRichText = true;
            runStatusText.lineSpacing = 1.1f;
            runStatusText.horizontalOverflow = HorizontalWrapMode.Wrap;
            runStatusText.verticalOverflow = VerticalWrapMode.Truncate;
            RectTransform textRect = runStatusText.rectTransform;
            UITheme.Stretch(textRect);
            textRect.offsetMin = new Vector2(34f, 14f);
            textRect.offsetMax = new Vector2(-30f, -14f);
        }

        if (runStatusText == null) {
            runStatusText = runStatusPlate.GetComponentInChildren<Text>(true);
        }

        bool visible = layer != null && !PrologueShallowRun.IsPrologueLayer(layer);
        runStatusPlate.gameObject.SetActive(visible);
        if (!visible) {
            return;
        }

        VisualUIHelper.ApplySlicedSprite(runStatusPlate, VisualAssetService.UIMapStatusPlateID, Color.white, new Color(0.08f, 0.06f, 0.04f, 0.88f), false);
        runStatusPlate.transform.SetAsLastSibling();
        runStatusText.text = BuildRunStatusText(DungeonRunStatusService.Build(GameRoot.Core.Dungeon, GameRoot.Core.CurrentPlayer));
    }

    private static string BuildRunStatusText(DungeonRunStatus status) {
        string title = string.IsNullOrEmpty(status.LayerName) ? $"第 {status.LayerID} 层" : $"第 {status.LayerID} 层 · {status.LayerName}";
        bool hpLow = status.HPMax > 0 && status.HP * 10 < status.HPMax * 3;
        bool sanLow = status.StepsSanAllows >= 0 && status.StepsSanAllows <= 2;
        string doll = $"{Tint($"HP {status.HP}/{status.HPMax}", hpLow)}    {Tint($"SAN {status.SAN}/{status.SANMax}", sanLow)}"
            + $"    磨损 {Mathf.RoundToInt(status.Wear)}    侵蚀 {Mathf.RoundToInt(status.Corruption)}";

        string step = status.NextStepSanMax <= 0
            ? "下一步不耗 SAN"
            : status.NextStepSanMin == status.NextStepSanMax
                ? $"下一步 -{status.NextStepSanMin} SAN"
                : $"下一步 -{status.NextStepSanMin}~{status.NextStepSanMax} SAN";
        if (status.StepsSanAllows >= 0) {
            step += Tint($"，还够走 {status.StepsSanAllows} 步", sanLow);
        }

        string exit = status.StepsToExit > 0 ? $"最近出口：{status.ExitLabel} {status.StepsToExit} 步" : "前方还没看到出口";
        string loot = status.LootCount > 0 ? $"带出 {status.LootCount} 件 ≈ {status.LootValue}G" : "还没捡到东西";
        return $"<b><color=#F2B864>{title}</color></b>      {loot}\n{doll}\n{step}  ·  {exit}";
    }

    private static string Tint(string text, bool warn) {
        return warn ? $"<color=#FF8A6E>{text}</color>" : text;
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
                ? "整理背包中：右键旋转物品；拖出格子的物品会在关闭背包时丢弃"
                : IsBeginner() ? "右下角打开背包，整理局内物资" : string.Empty;
        }
    }

    private void EnsureInventoryControls() {
        if (backpackHintText == null) {
            backpackHintText = UITheme.CreateText(transform, "BackpackHint_Text", string.Empty, UITheme.BodySize, UITheme.TextOnDark, TextAnchor.MiddleRight);
            backpackHintText.raycastTarget = false;
            backpackHintText.horizontalOverflow = HorizontalWrapMode.Wrap;
            RectTransform hintRect = backpackHintText.rectTransform;
            hintRect.anchorMin = hintRect.anchorMax = hintRect.pivot = new Vector2(1f, 0f);
            // Clear of the chassis, which opens at the bottom centre.
            hintRect.anchoredPosition = new Vector2(-180f, 46f);
            hintRect.sizeDelta = new Vector2(500f, 72f);
            Outline outline = backpackHintText.gameObject.AddComponent<Outline>();
            outline.effectColor = new Color(0f, 0f, 0f, 0.85f);
            outline.effectDistance = new Vector2(1.5f, -1.5f);
        }

        if (openBackpackBtn == null) {
            openBackpackBtn = CreateRoundButton("OpenBackpack_Button", "整理背包");
        }

        if (closeBackpackBtn == null) {
            closeBackpackBtn = CreateRoundButton("CloseBackpack_Button", "收起背包");
        }

        openBackpackBtn.transform.SetAsLastSibling();
        closeBackpackBtn.transform.SetAsLastSibling();
        backpackHintText.transform.SetAsLastSibling();
    }

    // A round brass backpack button in the bottom-right corner, its name in small type underneath.
    private Button CreateRoundButton(string objectName, string label) {
        Image image = new GameObject(objectName, typeof(RectTransform), typeof(Image), typeof(Button)).GetComponent<Image>();
        image.transform.SetParent(transform, false);
        RectTransform rect = image.rectTransform;
        rect.anchorMin = rect.anchorMax = rect.pivot = new Vector2(1f, 0f);
        rect.anchoredPosition = new Vector2(-40f, 44f);
        rect.sizeDelta = new Vector2(124f, 124f);
        VisualUIHelper.ApplySimpleSprite(image, VisualAssetService.UIMapBackpackButtonID, Color.white, new Color(0.55f, 0.4f, 0.2f, 1f), true, true);
        Button button = image.GetComponent<Button>();
        button.targetGraphic = image;

        Text text = UITheme.CreateText(image.transform, "Text", label, UITheme.CaptionSize, UITheme.TextOnDark, TextAnchor.MiddleCenter);
        text.raycastTarget = false;
        RectTransform textRect = text.rectTransform;
        textRect.anchorMin = new Vector2(0f, 0f);
        textRect.anchorMax = new Vector2(1f, 0f);
        textRect.pivot = new Vector2(0.5f, 1f);
        textRect.anchoredPosition = new Vector2(0f, 4f);
        textRect.sizeDelta = new Vector2(40f, 28f);
        Outline outline = text.gameObject.AddComponent<Outline>();
        outline.effectColor = new Color(0f, 0f, 0f, 0.9f);
        outline.effectDistance = new Vector2(1.5f, -1.5f);
        return button;
    }

    // ---- Background and the prologue's guide ----

    // A dark fill behind the map (the painting always covers the screen); the prologue's shallow run keeps its guide.
    private void ApplyMapBackground() {
        RectTransform rootRect = transform as RectTransform;
        if (rootRect != null) {
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
        }

        backgroundImage = VisualUIHelper.EnsurePanelBackground(transform, backgroundImage, "DungeonMapBackground_Image");
        AspectRatioFitter fitter = backgroundImage.GetComponent<AspectRatioFitter>();
        if (fitter != null) {
            fitter.enabled = false;
        }

        VisualUIHelper.ApplySolidColor(backgroundImage, new Color(0.02f, 0.03f, 0.035f, 1f));
        backgroundImage.transform.SetAsFirstSibling();
        Transform oldVeil = transform.Find("DungeonMapDepthVeil_Image");
        if (oldVeil != null) {
            oldVeil.gameObject.SetActive(false);
        }

        // Only the prologue's shallow run sits in front of the workshop door; every later layer-1 dive shows the layer itself.
        ApplyFirstDiveMapGuide(PrologueShallowRun.IsPrologueLayer(GameRoot.Core?.Dungeon?.CurrentLayer));
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
        firstDiveGuideText.text = "首次下潜：旧矿井浅缝\n只走到第一处路标，零号异常就撤回。";
        RectTransform textRect = firstDiveGuideText.rectTransform;
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.offsetMin = new Vector2(24f, 10f);
        textRect.offsetMax = new Vector2(-24f, -10f);
    }

    // ---- Helpers ----

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

    private Image EnsureNodeChildImage(Transform parent, string objectName) {
        Transform existing = parent.Find(objectName);
        Image image = existing != null ? existing.GetComponent<Image>() : null;
        if (image != null) {
            return image;
        }

        GameObject imageObj = new GameObject(objectName, typeof(RectTransform));
        imageObj.transform.SetParent(parent, false);
        image = imageObj.AddComponent<Image>();
        image.raycastTarget = false;
        return image;
    }
}
