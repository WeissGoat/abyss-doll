using System.Collections.Generic;
using UnityEngine;

public static class DungeonStairsProgressionTest {
    private static DungeonSettlementResult _lastSettlementResult;

    public static void Run() {
        Debug.Log("=== Running Dungeon Stairs Progression Test ===");

        TestLayerEndsWithStairsAfterBoss();
        TestEnterNextLayerKeepsRunLootLedger();
        TestStairsReturnSettlesRunLoot();

        Debug.Log("=== Dungeon Stairs Progression Test Finished ===");
    }

    private static void TestLayerEndsWithStairsAfterBoss() {
        CoreBackend core = CreateCore();
        core.Dungeon.LoadLayer(1);

        List<NodeBase> path = BuildLinearPath(core.Dungeon.CurrentLayer);
        DungeonConfig config = ConfigManager.Dungeons[1];
        NodeBase bossNode = path.Count >= 2 ? path[path.Count - 2] : null;
        NodeBase finalNode = path.Count >= 1 ? path[path.Count - 1] : null;

        bool pathShapeValid = path.Count == config.ExpectedNodeCount + 1
            && config.EndNode != null
            && config.EndNode.NodeType == "StairsNode"
            && bossNode is CombatNode bossCombat
            && bossCombat.MonsterIDs.Count == 1
            && bossCombat.MonsterIDs[0] == config.BossNode
            && finalNode is StairsNode stairs
            && stairs.LayerID == config.LayerID
            && finalNode.NextNodes.Count == 0;

        if (pathShapeValid) {
            Debug.Log("Configured Layer Stairs Generation PASSED.");
        } else {
            Debug.LogError($"Configured Layer Stairs Generation FAILED. PathCount={path.Count}, Expected={config.ExpectedNodeCount + 1}, EndNode={config.EndNode?.NodeType ?? "null"}, BossType={bossNode?.GetType().Name ?? "null"}, FinalType={finalNode?.GetType().Name ?? "null"}");
        }

        int beforeSan = core.CurrentPlayer.ActiveDoll.Status.SAN_Current;
        core.Dungeon.MoveToNode(finalNode);
        int afterSan = core.CurrentPlayer.ActiveDoll.Status.SAN_Current;
        if (beforeSan == afterSan) {
            Debug.Log("Stairs SAN Cost PASSED.");
        } else {
            Debug.LogError($"Stairs SAN Cost FAILED. Before={beforeSan}, After={afterSan}");
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
        List<NodeBase> path = BuildLinearPath(layer);
        return path.Count > 0 ? path[path.Count - 1] : null;
    }

    private static void HandleSettlementPrepared(DungeonSettlementResult result) {
        _lastSettlementResult = result;
    }
}
