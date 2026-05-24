using UnityEngine;

public static class DungeonMapVisibilitySmokeTest {
    public static void Run() {
        Debug.Log("=== Running Dungeon Map Visibility Smoke Test ===");

        TestLayerConfigDefinesFogContract();
        TestInitialFogAndRiskPresentation();
        TestMovementUpdatesVisibleRange();
        TestConfiguredRiskOverridesInference();

        Debug.Log("=== Dungeon Map Visibility Smoke Test Finished ===");
    }

    private static void TestLayerConfigDefinesFogContract() {
        CoreBackend core = CreateCore();
        DungeonConfig layer1 = ConfigManager.Dungeons.ContainsKey(1) ? ConfigManager.Dungeons[1] : null;
        DungeonConfig layer2 = ConfigManager.Dungeons.ContainsKey(2) ? ConfigManager.Dungeons[2] : null;

        bool valid = layer1 != null
            && layer2 != null
            && !string.IsNullOrEmpty(layer1.FogProfile)
            && !string.IsNullOrEmpty(layer2.FogProfile)
            && layer1.NodeRevealDepth == 1
            && layer1.NodePreviewDepth == 1
            && layer2.NodeRevealDepth == 1
            && layer2.NodePreviewDepth == 1;

        if (core != null && valid) {
            Debug.Log("Dungeon Fog Config Contract PASSED.");
        } else {
            Debug.LogError($"Dungeon Fog Config Contract FAILED. L1={layer1?.FogProfile ?? "null"} {layer1?.NodeRevealDepth ?? -1}/{layer1?.NodePreviewDepth ?? -1}, L2={layer2?.FogProfile ?? "null"} {layer2?.NodeRevealDepth ?? -1}/{layer2?.NodePreviewDepth ?? -1}");
        }
    }

    private static void TestInitialFogAndRiskPresentation() {
        CoreBackend core = CreateCore();
        core.Dungeon.LoadLayer(1);
        DungeonLayer layer = core.Dungeon.CurrentLayer;

        NodeBase entry = GetNode(layer, 0, 0);
        NodeBase previewNode = GetNode(layer, 1, 0);
        NodeBase hiddenNode = GetNode(layer, 2, 0);
        NodeBase bossNode = GetBossNode(layer);

        DungeonMapNodePresentation entryPresentation = DungeonMapVisibilityService.BuildNodePresentation(layer, entry);
        DungeonMapNodePresentation previewPresentation = DungeonMapVisibilityService.BuildNodePresentation(layer, previewNode);
        DungeonMapNodePresentation hiddenPresentation = DungeonMapVisibilityService.BuildNodePresentation(layer, hiddenNode);
        DungeonMapNodePresentation bossPresentation = DungeonMapVisibilityService.BuildNodePresentation(layer, bossNode);

        bool valid = entryPresentation.IsRevealed
            && previewPresentation.IsPreview
            && hiddenPresentation.IsHidden
            && bossPresentation.IsPreview
            && !string.IsNullOrEmpty(entryPresentation.RiskLabel);

        if (valid) {
            Debug.Log("Initial Dungeon Fog Presentation PASSED.");
        } else {
            Debug.LogError($"Initial Dungeon Fog Presentation FAILED. Entry={entryPresentation.Visibility}, Preview={previewPresentation.Visibility}, Hidden={hiddenPresentation.Visibility}, Boss={bossPresentation.Visibility}, Risk={entryPresentation.RiskLabel}");
        }
    }

    private static void TestMovementUpdatesVisibleRange() {
        CoreBackend core = CreateCore();
        core.Dungeon.LoadLayer(1);
        DungeonLayer layer = core.Dungeon.CurrentLayer;

        NodeBase entry = GetNode(layer, 0, 0);
        NodeBase next = entry?.NextNodes != null && entry.NextNodes.Count > 0 ? entry.NextNodes[0] : null;
        NodeBase futureNode = GetNode(layer, 2, 0);

        if (entry != null) {
            layer.CurrentNode = entry;
            entry.IsVisited = true;
        }

        DungeonNodeVisibilityState nextVisibility = DungeonMapVisibilityService.ResolveVisibility(layer, next);
        DungeonNodeVisibilityState futureVisibility = DungeonMapVisibilityService.ResolveVisibility(layer, futureNode);

        bool valid = entry != null
            && next != null
            && nextVisibility == DungeonNodeVisibilityState.Revealed
            && futureVisibility == DungeonNodeVisibilityState.Preview;

        if (valid) {
            Debug.Log("Dungeon Fog Movement Update PASSED.");
        } else {
            Debug.LogError($"Dungeon Fog Movement Update FAILED. Entry={entry?.NodeID ?? "null"}, Next={next?.NodeID ?? "null"}:{nextVisibility}, Future={futureNode?.NodeID ?? "null"}:{futureVisibility}");
        }
    }

    private static void TestConfiguredRiskOverridesInference() {
        HazardNode node = new HazardNode {
            NodeID = "risk_override_smoke",
            RouteTheme = "Safe"
        };
        node.Init(new NodePoolEntry {
            NodeType = "HazardNode",
            RiskLevel = "Low",
            RiskHint = "Smoke risk override."
        });

        DungeonNodeRiskLevel risk = DungeonMapVisibilityService.ResolveRiskLevel(node);
        DungeonMapNodePresentation presentation = DungeonMapVisibilityService.BuildNodePresentation(new DungeonLayer {
            LayerID = 1,
            RootNode = node,
            CurrentNode = node
        }, node);

        bool valid = risk == DungeonNodeRiskLevel.Low
            && presentation.RiskLabel == "低风险"
            && presentation.RiskHint == "Smoke risk override.";

        if (valid) {
            Debug.Log("Configured Risk Override PASSED.");
        } else {
            Debug.LogError($"Configured Risk Override FAILED. Risk={risk}, Label={presentation.RiskLabel}, Hint={presentation.RiskHint}");
        }
    }

    private static CoreBackend CreateCore() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        return core;
    }

    private static NodeBase GetNode(DungeonLayer layer, int rowIndex, int columnIndex) {
        if (layer?.NodeRows == null
            || rowIndex < 0
            || rowIndex >= layer.NodeRows.Count
            || layer.NodeRows[rowIndex] == null
            || columnIndex < 0
            || columnIndex >= layer.NodeRows[rowIndex].Count) {
            return null;
        }

        return layer.NodeRows[rowIndex][columnIndex];
    }

    private static NodeBase GetBossNode(DungeonLayer layer) {
        if (layer?.NodeRows == null || layer.NodeRows.Count < 2) {
            return null;
        }

        return GetNode(layer, layer.NodeRows.Count - 2, 0);
    }
}
