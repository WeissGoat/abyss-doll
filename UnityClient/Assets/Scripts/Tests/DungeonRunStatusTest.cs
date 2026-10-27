using System.Collections.Generic;
using UnityEngine;

// The map's run status (DungeonRunStatusService): the next step's SAN, the nearest exit the fog does not hide, and the
// loot an evacuation now would bring home.
public static class DungeonRunStatusTest {
    public static void Run() {
        Debug.Log("=== Running Dungeon Run Status Test ===");

        TestNextStepCostAndStepsLeft();
        TestNearestExitIgnoresTheFog();
        TestCarriedLootCountsOnlyWhatIsStillInTheBag();

        Debug.Log("=== Dungeon Run Status Test Finished ===");
    }

    private static void TestNextStepCostAndStepsLeft() {
        CoreBackend core = CreateCore();
        core.Dungeon.LoadLayer(1);
        DungeonLayer layer = core.Dungeon.CurrentLayer;
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        doll.RuntimeGrid = new BackpackGrid(doll.Chassis);
        doll.Status.SAN_Current = 10;

        NodeBase start = layer.EntryNodes[0];
        layer.CurrentNode = start;
        start.IsVisited = true;
        CombatNode next = new CombatNode { NodeID = "status_next", MapRow = start.MapRow + 1 };
        start.NextNodes = new List<NodeBase> { next };

        int cost = ConfigManager.Dungeons[1].SANCostPerNode;
        DungeonRunStatus status = DungeonRunStatusService.Build(core.Dungeon, core.CurrentPlayer);
        bool valid = cost > 0
            && status.LayerID == 1
            && status.LayerName == ConfigManager.Dungeons[1].Name
            && status.NextStepSanMin == cost
            && status.NextStepSanMax == cost
            && status.StepsSanAllows == 10 / cost
            && status.SAN == 10;

        if (valid) {
            Debug.Log("Run Status Next Step Cost PASSED.");
        } else {
            Debug.LogError($"Run Status Next Step Cost FAILED. Cost={cost}, Min={status.NextStepSanMin}, Max={status.NextStepSanMax}, Steps={status.StepsSanAllows}, Layer={status.LayerID} {status.LayerName}");
        }
    }

    private static void TestNearestExitIgnoresTheFog() {
        CoreBackend core = CreateCore();
        core.Dungeon.LoadLayer(1);
        DungeonLayer layer = core.Dungeon.CurrentLayer;
        NodeBase start = layer.EntryNodes[0];
        layer.CurrentNode = start;
        start.IsVisited = true;

        // start -> a -> safe room: two steps, in sight.
        CombatNode a = new CombatNode { NodeID = "status_a", MapRow = start.MapRow + 1 };
        SafeRoomNode near = new SafeRoomNode { NodeID = "status_safe", MapRow = start.MapRow + 2 };
        a.NextNodes = new List<NodeBase> { near };
        start.NextNodes = new List<NodeBase> { a };
        int seen = DungeonRunStatusService.FindStepsToKnownExit(layer, out NodeBase seenExit);

        // start -> a -> b -> c -> stairs: the stairs sit past the preview range, so the player cannot know about them.
        CombatNode b = new CombatNode { NodeID = "status_b", MapRow = start.MapRow + 2 };
        CombatNode c = new CombatNode { NodeID = "status_c", MapRow = start.MapRow + 3 };
        StairsNode far = new StairsNode { NodeID = "status_stairs", MapRow = start.MapRow + 4, LayerID = 1 };
        a.NextNodes = new List<NodeBase> { b };
        b.NextNodes = new List<NodeBase> { c };
        c.NextNodes = new List<NodeBase> { far };
        int hidden = DungeonRunStatusService.FindStepsToKnownExit(layer, out NodeBase hiddenExit);

        if (seen == 2 && seenExit == near && hidden == -1 && hiddenExit == null) {
            Debug.Log("Run Status Nearest Known Exit PASSED.");
        } else {
            Debug.LogError($"Run Status Nearest Known Exit FAILED. Seen={seen} ({seenExit?.NodeID ?? "null"}), Hidden={hidden} ({hiddenExit?.NodeID ?? "null"})");
        }
    }

    private static void TestCarriedLootCountsOnlyWhatIsStillInTheBag() {
        CoreBackend core = CreateCore();
        core.Dungeon.LoadLayer(1);
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        BackpackGrid grid = new BackpackGrid(doll.Chassis);
        doll.RuntimeGrid = grid;

        ItemEntity kept = ConfigManager.CreateItem("mat_core_tier1");
        ItemEntity dropped = ConfigManager.CreateItem("loot_gear_scrap");
        ItemEntity brought = ConfigManager.CreateItem("gear_wooden_shield");
        grid.PlaceItem(kept, 0, 0);
        grid.PlaceItem(brought, 3, 0);
        DungeonEventBus.PublishCombatLootCollected(new CombatLootCollectionResult {
            NodeID = "status_loot",
            AcceptedItems = new List<ItemEntity> { kept, dropped }
        });

        DungeonRunStatus status = DungeonRunStatusService.Build(core.Dungeon, core.CurrentPlayer);
        if (status.LootCount == 1 && status.LootValue == kept.BaseValue) {
            Debug.Log("Run Status Carried Loot PASSED.");
        } else {
            Debug.LogError($"Run Status Carried Loot FAILED. Count={status.LootCount}, Value={status.LootValue}, Expected={kept.BaseValue}");
        }
    }

    private static CoreBackend CreateCore() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        return core;
    }
}
