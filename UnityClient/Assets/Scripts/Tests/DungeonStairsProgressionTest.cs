using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public static class DungeonStairsProgressionTest {
    private static DungeonSettlementResult _lastSettlementResult;
    private static int _lastUnlockedStartLayerID;

    public static void Run() {
        Debug.Log("=== Running Dungeon Stairs Progression Test ===");

        TestLayerEndsWithStairsAfterBoss();
        TestDungeonMapSeedReproducible();
        TestDungeonMapMovementRules();
        TestDungeonStartLayerDefaultsAndLockedValidation();
        TestStairsUnlocksNextStartLayer();
        TestStartRunAtUnlockedLayerResetsRunLootLedger();
        TestEnterNextLayerKeepsRunLootLedger();
        TestStairsReturnSettlesRunLoot();
        TestLayerTwoMapLayoutKeepsNodeButtonsReadable();
        TestMovementSanCostIncludesBackpackEffects();

        Debug.Log("=== Dungeon Stairs Progression Test Finished ===");
    }

    private static void TestLayerEndsWithStairsAfterBoss() {
        CoreBackend core = CreateCore();
        core.Dungeon.LoadLayer(1);

        DungeonLayer layer = core.Dungeon.CurrentLayer;
        DungeonConfig config = ConfigManager.Dungeons[1];
        NodeBase bossNode = GetBossNode(layer);
        NodeBase finalNode = FindLastNode(layer);

        bool networkShapeValid = layer != null
            && layer.NodeRows.Count == config.RowCount + 2
            && layer.EntryNodes.Count >= config.MinRouteCount
            && CountNodes(layer) > config.ExpectedNodeCount + 1
            && CountEdges(layer) >= CountNodes(layer) - 1
            && config.EndNode != null
            && config.EndNode.NodeType == "StairsNode"
            && bossNode is CombatNode bossCombat
            && bossCombat.MonsterIDs.Count == 1
            && bossCombat.MonsterIDs[0] == config.BossNode
            && finalNode is StairsNode stairs
            && stairs.LayerID == config.LayerID
            && finalNode.NextNodes.Count == 0
            && AllRowsBeforeTargetReach(layer, bossNode)
            && AllNodesReach(layer, finalNode);

        if (networkShapeValid) {
            Debug.Log("Configured Layer Network Stairs Generation PASSED.");
        } else {
            Debug.LogError($"Configured Layer Network Stairs Generation FAILED. Rows={layer?.NodeRows.Count ?? -1}/{config.RowCount + 2}, Entries={layer?.EntryNodes.Count ?? -1}/{config.MinRouteCount}, Nodes={CountNodes(layer)}, Edges={CountEdges(layer)}, EndNode={config.EndNode?.NodeType ?? "null"}, BossType={bossNode?.GetType().Name ?? "null"}, FinalType={finalNode?.GetType().Name ?? "null"}");
        }

        int beforeSan = core.CurrentPlayer.ActiveDoll.Status.SAN_Current;
        if (bossNode != null && finalNode != null) {
            layer.CurrentNode = bossNode;
            core.Dungeon.MoveToNode(finalNode);
        }
        int afterSan = core.CurrentPlayer.ActiveDoll.Status.SAN_Current;
        if (beforeSan == afterSan) {
            Debug.Log("Stairs SAN Cost PASSED.");
        } else {
            Debug.LogError($"Stairs SAN Cost FAILED. Before={beforeSan}, After={afterSan}");
        }
    }

    private static void TestDungeonMapSeedReproducible() {
        CreateCore();
        DungeonConfig config = ConfigManager.Dungeons[1];
        DungeonLayer first = new DungeonLayer();
        DungeonLayer second = new DungeonLayer();
        first.GenerateMapTree(config, 12345);
        second.GenerateMapTree(config, 12345);

        bool reproducible = !string.IsNullOrEmpty(first.GenerationSummary)
            && first.GenerationSummary == second.GenerationSummary;

        if (reproducible) {
            Debug.Log("Dungeon Map Seed Reproducible PASSED.");
        } else {
            Debug.LogError($"Dungeon Map Seed Reproducible FAILED. First={first.GenerationSummary}\nSecond={second.GenerationSummary}");
        }
    }

    private static void TestDungeonMapMovementRules() {
        CoreBackend core = CreateCore();
        core.Dungeon.LoadLayer(1);

        DungeonLayer layer = core.Dungeon.CurrentLayer;
        NodeBase entry = layer?.EntryNodes.Count > 0 ? layer.EntryNodes[0] : null;
        NodeBase invalidInitialTarget = layer?.NodeRows.Count > 1 && layer.NodeRows[1].Count > 0 ? layer.NodeRows[1][0] : null;
        NodeBase validNextTarget = entry?.NextNodes != null && entry.NextNodes.Count > 0 ? entry.NextNodes[0] : null;

        bool entryAllowed = entry != null && core.Dungeon.CanMoveToNode(entry);
        bool nonEntryRejected = invalidInitialTarget != null && !core.Dungeon.CanMoveToNode(invalidInitialTarget);
        if (invalidInitialTarget != null) {
            core.Dungeon.MoveToNode(invalidInitialTarget);
        }

        bool invalidMoveHadNoEffect = layer != null
            && layer.CurrentNode == null
            && (invalidInitialTarget == null || !invalidInitialTarget.IsVisited);

        if (layer != null) {
            layer.CurrentNode = entry;
        }
        bool nextAllowed = validNextTarget != null && core.Dungeon.CanMoveToNode(validNextTarget);

        if (entryAllowed && nonEntryRejected && invalidMoveHadNoEffect && nextAllowed) {
            Debug.Log("Dungeon Map Movement Rules PASSED.");
        } else {
            Debug.LogError($"Dungeon Map Movement Rules FAILED. EntryAllowed={entryAllowed}, NonEntryRejected={nonEntryRejected}, InvalidNoEffect={invalidMoveHadNoEffect}, NextAllowed={nextAllowed}");
        }
    }

    private static void TestLayerTwoMapLayoutKeepsNodeButtonsReadable() {
        CoreBackend core = CreateCore();
        core.Dungeon.TryUnlockNextStartLayerFromClearedLayer(1);
        core.Dungeon.StartRunAtLayer(2);

        GameObject canvasObj = new GameObject("DungeonMapLayoutTestCanvas");
        canvasObj.AddComponent<Canvas>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject mapObj = new GameObject("DungeonMapPanel");
        mapObj.transform.SetParent(canvasObj.transform, false);
        mapObj.AddComponent<RectTransform>();
        DungeonMapUIController controller = mapObj.AddComponent<DungeonMapUIController>();

        GameObject contentObj = new GameObject("MapLayout");
        contentObj.transform.SetParent(mapObj.transform, false);
        RectTransform contentRect = contentObj.AddComponent<RectTransform>();
        contentRect.sizeDelta = new Vector2(100f, 100f);
        HorizontalLayoutGroup layout = contentObj.AddComponent<HorizontalLayoutGroup>();
        layout.childControlWidth = true;
        controller.contentParent = contentObj.transform;
        GameObject nodeButtonPrefab = CreateNodeButtonPrefab();
        controller.nodeButtonPrefab = nodeButtonPrefab;

        controller.RefreshMap();

        DungeonLayer layer = core.Dungeon.CurrentLayer;
        int expectedNodeCount = CountNodes(layer);
        int expectedRouteLineCount = CountEdges(layer);
        int nodeButtonCount = 0;
        int routeLineCount = 0;
        int interactableNodeButtons = 0;
        float expectedLayoutWidth = Mathf.Max(720f, 160f + Mathf.Max(0, layer.NodeRows.Count - 1) * 230f);
        foreach (Transform child in contentObj.transform) {
            Button button = child.GetComponent<Button>();
            if (button != null) {
                nodeButtonCount++;
                if (button.interactable) {
                    interactableNodeButtons++;
                }
            } else if (child.name == "DungeonRouteLine_Image") {
                routeLineCount++;
            }
        }

        bool childCountMatches = nodeButtonCount == expectedNodeCount
            && routeLineCount == expectedRouteLineCount
            && contentObj.transform.childCount == expectedNodeCount + expectedRouteLineCount;
        bool layoutWidthExpanded = contentRect.sizeDelta.x >= expectedLayoutWidth;
        bool layoutNoLongerCompresses = !layout.enabled && !layout.childControlWidth && !layout.childForceExpandWidth;
        bool onlyEntryNodesInteractable = interactableNodeButtons == layer.EntryNodes.Count;
        bool buttonsHavePreferredWidth = true;
        foreach (Transform child in contentObj.transform) {
            if (child.GetComponent<Button>() == null) {
                continue;
            }

            LayoutElement element = child.GetComponent<LayoutElement>();
            buttonsHavePreferredWidth &= element != null && element.preferredWidth >= 160f;
        }

        if (childCountMatches && layoutWidthExpanded && layoutNoLongerCompresses && onlyEntryNodesInteractable && buttonsHavePreferredWidth) {
            Debug.Log("Dungeon Map Layer 2 Layout PASSED.");
        } else {
            Debug.LogError($"Dungeon Map Layer 2 Layout FAILED. Nodes={nodeButtonCount}/{expectedNodeCount}, Routes={routeLineCount}/{expectedRouteLineCount}, ChildCount={contentObj.transform.childCount}, Width={contentRect.sizeDelta.x}/{expectedLayoutWidth}, LayoutEnabled={layout.enabled}, Compress={layout.childControlWidth}, ForceExpand={layout.childForceExpandWidth}, Interactable={interactableNodeButtons}/{layer.EntryNodes.Count}, PreferredWidth={buttonsHavePreferredWidth}");
        }

        Object.DestroyImmediate(canvasObj);
        Object.DestroyImmediate(nodeButtonPrefab);
    }

    private static void TestDungeonStartLayerDefaultsAndLockedValidation() {
        CoreBackend core = CreateCore();

        bool defaultsValid = core.CurrentPlayer.HighestUnlockedDungeonLayer == 1
            && core.CurrentPlayer.LastSelectedDungeonStartLayer == 1;
        bool canStartLayerOne = core.Dungeon.CanStartAtLayer(1);
        bool rejectsLockedLayerTwo = !core.Dungeon.CanStartAtLayer(2) && !core.Dungeon.StartRunAtLayer(2);

        if (defaultsValid && canStartLayerOne && rejectsLockedLayerTwo && core.Dungeon.CurrentLayer == null) {
            Debug.Log("Dungeon Start Layer Defaults PASSED.");
        } else {
            Debug.LogError($"Dungeon Start Layer Defaults FAILED. Highest={core.CurrentPlayer.HighestUnlockedDungeonLayer}, Last={core.CurrentPlayer.LastSelectedDungeonStartLayer}, Can1={canStartLayerOne}, Reject2={rejectsLockedLayerTwo}, CurrentLayer={core.Dungeon.CurrentLayer?.LayerID.ToString() ?? "null"}");
        }
    }

    private static void TestStairsUnlocksNextStartLayer() {
        CoreBackend core = CreateCore();
        core.Dungeon.LoadLayer(1);

        _lastUnlockedStartLayerID = -1;
        DungeonEventBus.OnDungeonStartLayerUnlocked += HandleStartLayerUnlocked;
        StairsNode stairs = FindLastNode(core.Dungeon.CurrentLayer) as StairsNode;
        core.Dungeon.CurrentLayer.CurrentNode = FindPredecessor(core.Dungeon.CurrentLayer, stairs);
        core.Dungeon.MoveToNode(stairs);
        DungeonEventBus.OnDungeonStartLayerUnlocked -= HandleStartLayerUnlocked;

        bool unlockValid = stairs != null
            && core.CurrentPlayer.HighestUnlockedDungeonLayer == 2
            && _lastUnlockedStartLayerID == 2
            && core.Dungeon.CanStartAtLayer(2);

        if (unlockValid) {
            Debug.Log("Stairs Unlocks Next Start Layer PASSED.");
        } else {
            Debug.LogError($"Stairs Unlocks Next Start Layer FAILED. Stairs={stairs != null}, Highest={core.CurrentPlayer.HighestUnlockedDungeonLayer}, EventLayer={_lastUnlockedStartLayerID}, CanStart2={core.Dungeon.CanStartAtLayer(2)}");
        }
    }

    private static void TestStartRunAtUnlockedLayerResetsRunLootLedger() {
        CoreBackend core = CreateCore();
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        BackpackGrid grid = ResetBackpack(doll);

        core.Dungeon.LoadLayer(1);
        core.Dungeon.TryUnlockNextStartLayerFromClearedLayer(1);
        ItemEntity previousRunLoot = ConfigManager.CreateItem("loot_gear_scrap");
        grid.PlaceItem(previousRunLoot, 0, 0);
        DungeonEventBus.PublishCombatLootCollected(new CombatLootCollectionResult {
            NodeID = "previous_run",
            AcceptedItems = new List<ItemEntity> { previousRunLoot }
        });

        bool startedLayerTwo = core.Dungeon.StartRunAtLayer(2);
        _lastSettlementResult = null;
        DungeonEventBus.OnDungeonSettlementPrepared += HandleSettlementPrepared;
        DungeonEventBus.PublishDungeonEvacuated();
        DungeonEventBus.OnDungeonSettlementPrepared -= HandleSettlementPrepared;

        bool resetValid = startedLayerTwo
            && core.Dungeon.CurrentLayer != null
            && core.Dungeon.CurrentLayer.LayerID == 2
            && core.CurrentPlayer.LastSelectedDungeonStartLayer == 2
            && _lastSettlementResult != null
            && _lastSettlementResult.PickedUpCount == 0
            && _lastSettlementResult.BroughtOutCount == 0
            && _lastSettlementResult.LostCount == 0;

        if (resetValid) {
            Debug.Log("Start Run At Unlocked Layer Resets Loot Ledger PASSED.");
        } else {
            Debug.LogError($"Start Run At Unlocked Layer Resets Loot Ledger FAILED. Started={startedLayerTwo}, Layer={core.Dungeon.CurrentLayer?.LayerID ?? -1}, Last={core.CurrentPlayer.LastSelectedDungeonStartLayer}, Picked={_lastSettlementResult?.PickedUpCount ?? -1}, Brought={_lastSettlementResult?.BroughtOutCount ?? -1}, Lost={_lastSettlementResult?.LostCount ?? -1}");
        }
    }

    private static void TestEnterNextLayerKeepsRunLootLedger() {
        CoreBackend core = CreateCore();
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        BackpackGrid grid = ResetBackpack(doll);

        core.Dungeon.LoadLayer(1);
        ItemEntity carriedLoot = ConfigManager.CreateItem("loot_gear_scrap");
        grid.PlaceItem(carriedLoot, 0, 0);
        DungeonEventBus.PublishCombatLootCollected(new CombatLootCollectionResult {
            NodeID = "stairs_progression_layer_1",
            AcceptedItems = new List<ItemEntity> { carriedLoot }
        });

        core.Dungeon.EnterNextLayer();
        bool enteredLayerTwo = core.Dungeon.CurrentLayer != null && core.Dungeon.CurrentLayer.LayerID == 2;

        _lastSettlementResult = null;
        DungeonEventBus.OnDungeonSettlementPrepared += HandleSettlementPrepared;
        DungeonEventBus.PublishDungeonEvacuated();
        DungeonEventBus.OnDungeonSettlementPrepared -= HandleSettlementPrepared;

        bool ledgerPreserved = _lastSettlementResult != null
            && _lastSettlementResult.IsVictory
            && _lastSettlementResult.PickedUpCount == 1
            && _lastSettlementResult.BroughtOutCount == 1
            && _lastSettlementResult.LostCount == 0;

        if (enteredLayerTwo && ledgerPreserved) {
            Debug.Log("Enter Next Layer Loot Ledger PASSED.");
        } else {
            Debug.LogError($"Enter Next Layer Loot Ledger FAILED. EnteredLayerTwo={enteredLayerTwo}, Picked={_lastSettlementResult?.PickedUpCount ?? -1}, Brought={_lastSettlementResult?.BroughtOutCount ?? -1}, Lost={_lastSettlementResult?.LostCount ?? -1}");
        }
    }

    private static void TestStairsReturnSettlesRunLoot() {
        CoreBackend core = CreateCore();
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        BackpackGrid grid = ResetBackpack(doll);

        core.Dungeon.LoadLayer(1);
        StairsNode stairs = FindLastNode(core.Dungeon.CurrentLayer) as StairsNode;
        ItemEntity carriedLoot = ConfigManager.CreateItem("loot_gear_scrap");
        grid.PlaceItem(carriedLoot, 0, 0);
        DungeonEventBus.PublishCombatLootCollected(new CombatLootCollectionResult {
            NodeID = "stairs_return",
            AcceptedItems = new List<ItemEntity> { carriedLoot }
        });

        _lastSettlementResult = null;
        DungeonEventBus.OnDungeonSettlementPrepared += HandleSettlementPrepared;
        stairs?.ReturnToTown();
        DungeonEventBus.OnDungeonSettlementPrepared -= HandleSettlementPrepared;

        bool returnSettled = _lastSettlementResult != null
            && _lastSettlementResult.IsVictory
            && _lastSettlementResult.PickedUpCount == 1
            && _lastSettlementResult.BroughtOutCount == 1;

        if (stairs != null && returnSettled) {
            Debug.Log("Stairs Return Settlement PASSED.");
        } else {
            Debug.LogError($"Stairs Return Settlement FAILED. StairsFound={stairs != null}, Picked={_lastSettlementResult?.PickedUpCount ?? -1}, Brought={_lastSettlementResult?.BroughtOutCount ?? -1}");
        }
    }

    private static void TestMovementSanCostIncludesBackpackEffects() {
        CoreBackend core = CreateCore();
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        BackpackGrid grid = ResetBackpack(doll);

        ItemEntity toxicLoot = ConfigManager.CreateItem("loot_toxic_filter");
        bool placed = toxicLoot != null && grid.PlaceItem(toxicLoot, 0, 0);
        SanCostTestNode testNode = new SanCostTestNode {
            NodeID = "san_cost_effect_test_node"
        };

        core.Dungeon.CurrentLayer = new DungeonLayer {
            LayerID = 1,
            RootNode = testNode,
            CurrentNode = null
        };

        int beforeSan = doll.Status.SAN_Current;
        core.Dungeon.MoveToNode(testNode);
        int afterSan = doll.Status.SAN_Current;

        int expectedCost = ConfigManager.Dungeons[1].SANCostPerNode + 1;
        int actualCost = beforeSan - afterSan;
        if (placed && actualCost == expectedCost) {
            Debug.Log("Movement SAN Cost Item Effects PASSED.");
        } else {
            Debug.LogError($"Movement SAN Cost Item Effects FAILED. Placed={placed}, ExpectedCost={expectedCost}, ActualCost={actualCost}");
        }
    }

    private static CoreBackend CreateCore() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        return core;
    }

    private static BackpackGrid ResetBackpack(DollEntity doll) {
        BackpackGrid grid = new BackpackGrid(doll.Chassis);
        doll.RuntimeGrid = grid;
        return grid;
    }

    private static List<NodeBase> BuildLinearPath(DungeonLayer layer) {
        List<NodeBase> path = new List<NodeBase>();
        NodeBase current = layer?.RootNode;
        while (current != null) {
            path.Add(current);
            current = current.NextNodes != null && current.NextNodes.Count > 0 ? current.NextNodes[0] : null;
        }

        return path;
    }

    private static NodeBase FindLastNode(DungeonLayer layer) {
        if (layer?.NodeRows != null && layer.NodeRows.Count > 0) {
            List<NodeBase> lastRow = layer.NodeRows[layer.NodeRows.Count - 1];
            return lastRow != null && lastRow.Count > 0 ? lastRow[0] : null;
        }

        List<NodeBase> path = BuildLinearPath(layer);
        return path.Count > 0 ? path[path.Count - 1] : null;
    }

    private static NodeBase GetBossNode(DungeonLayer layer) {
        if (layer?.NodeRows == null || layer.NodeRows.Count < 2) {
            return null;
        }

        List<NodeBase> bossRow = layer.NodeRows[layer.NodeRows.Count - 2];
        return bossRow != null && bossRow.Count > 0 ? bossRow[0] : null;
    }

    private static NodeBase FindPredecessor(DungeonLayer layer, NodeBase target) {
        if (layer?.NodeRows == null || target == null) {
            return null;
        }

        foreach (List<NodeBase> row in layer.NodeRows) {
            if (row == null) {
                continue;
            }

            foreach (NodeBase node in row) {
                if (node?.NextNodes != null && node.NextNodes.Contains(target)) {
                    return node;
                }
            }
        }

        return null;
    }

    private static int CountNodes(DungeonLayer layer) {
        int count = 0;
        if (layer?.NodeRows == null) {
            return 0;
        }

        foreach (List<NodeBase> row in layer.NodeRows) {
            count += row?.Count ?? 0;
        }

        return count;
    }

    private static int CountEdges(DungeonLayer layer) {
        int count = 0;
        if (layer?.NodeRows == null) {
            return 0;
        }

        foreach (List<NodeBase> row in layer.NodeRows) {
            if (row == null) {
                continue;
            }

            foreach (NodeBase node in row) {
                count += node?.NextNodes?.Count ?? 0;
            }
        }

        return count;
    }

    private static bool AllNodesReach(DungeonLayer layer, NodeBase target) {
        if (layer?.NodeRows == null || target == null) {
            return false;
        }

        foreach (List<NodeBase> row in layer.NodeRows) {
            if (row == null) {
                continue;
            }

            foreach (NodeBase node in row) {
                if (node != target && !CanReach(node, target)) {
                    return false;
                }
            }
        }

        return true;
    }

    private static bool AllRowsBeforeTargetReach(DungeonLayer layer, NodeBase target) {
        if (layer?.NodeRows == null || target == null) {
            return false;
        }

        for (int rowIndex = 0; rowIndex < layer.NodeRows.Count; rowIndex++) {
            List<NodeBase> row = layer.NodeRows[rowIndex];
            if (row == null || row.Contains(target)) {
                return true;
            }

            foreach (NodeBase node in row) {
                if (!CanReach(node, target)) {
                    return false;
                }
            }
        }

        return false;
    }

    private static bool CanReach(NodeBase start, NodeBase target) {
        if (start == null || target == null) {
            return false;
        }

        HashSet<NodeBase> visited = new HashSet<NodeBase>();
        Queue<NodeBase> queue = new Queue<NodeBase>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0) {
            NodeBase current = queue.Dequeue();
            if (current == target) {
                return true;
            }

            if (current.NextNodes == null) {
                continue;
            }

            foreach (NodeBase next in current.NextNodes) {
                if (next != null && visited.Add(next)) {
                    queue.Enqueue(next);
                }
            }
        }

        return false;
    }

    private static void HandleSettlementPrepared(DungeonSettlementResult result) {
        _lastSettlementResult = result;
    }

    private static void HandleStartLayerUnlocked(int layerID) {
        _lastUnlockedStartLayerID = layerID;
    }

    private static GameObject CreateNodeButtonPrefab() {
        Font font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
        GameObject prefab = new GameObject("NodeButtonPrefab_Test");
        prefab.AddComponent<RectTransform>().sizeDelta = new Vector2(120f, 80f);
        prefab.AddComponent<Image>();
        prefab.AddComponent<Button>();

        GameObject textObj = new GameObject("Text");
        textObj.transform.SetParent(prefab.transform, false);
        RectTransform textRect = textObj.AddComponent<RectTransform>();
        textRect.anchorMin = Vector2.zero;
        textRect.anchorMax = Vector2.one;
        textRect.sizeDelta = Vector2.zero;
        Text text = textObj.AddComponent<Text>();
        text.font = font;
        text.alignment = TextAnchor.MiddleCenter;
        text.raycastTarget = false;

        return prefab;
    }

    private class SanCostTestNode : NodeBase {
        public override void OnEnterNode() {
            Debug.Log($"[Test] Entered {NodeID} without combat side effects.");
        }
    }
}
