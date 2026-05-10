using UnityEngine;

public static class CombatLootDropTest {
    private static CombatLootPickupResult _preparedLootResult;
    private static bool _nodeSettlementCompleted;

    public static void Run() {
        Debug.Log("=== Running Combat Loot Drop Test ===");

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;

        var doll = core.CurrentPlayer.ActiveDoll;
        doll.RuntimeGrid = new BackpackGrid(doll.Chassis);

        CombatNode combatNode = new CombatNode {
            NodeID = "test_combat_loot"
        };
        combatNode.MonsterIDs.Add("mob_scavenger_bug");
        combatNode.NextNodes.Add(new SafeRoomNode { NodeID = "test_next_node" });

        core.Dungeon.CurrentLayer = new DungeonLayer {
            LayerID = 1,
            RootNode = combatNode,
            CurrentNode = combatNode
        };

        _preparedLootResult = null;
        _nodeSettlementCompleted = false;
        DungeonEventBus.OnCombatLootPrepared += HandleCombatLootPrepared;
        DungeonEventBus.OnNodeSettlementCompleted += HandleNodeSettlementCompleted;

        int beforeCount = ((BackpackGrid)doll.RuntimeGrid).ContainedItems.Count;
        combatNode.ResolveAfterVictory();
        int afterPrepareCount = ((BackpackGrid)doll.RuntimeGrid).ContainedItems.Count;

        if (_preparedLootResult != null &&
            _preparedLootResult.OfferedItems.Count == 1 &&
            afterPrepareCount == beforeCount &&
            !_nodeSettlementCompleted) {
            Debug.Log("Combat Loot Preparation PASSED.");
        } else {
            Debug.LogError($"Combat Loot Preparation FAILED. Offered={_preparedLootResult?.OfferedItems.Count ?? 0}, BackpackCount={afterPrepareCount}, SettlementCompleted={_nodeSettlementCompleted}");
        }

        if (_preparedLootResult != null) {
            ItemEntity offeredItem = _preparedLootResult.OfferedItems[0];
            ((BackpackGrid)doll.RuntimeGrid).PlaceItem(offeredItem, 0, 0);
            combatNode.ConfirmLootCollection();
        }

        int afterConfirmCount = ((BackpackGrid)doll.RuntimeGrid).ContainedItems.Count;
        if (afterConfirmCount == beforeCount + 1 && _nodeSettlementCompleted) {
            Debug.Log("Combat Loot Confirmation PASSED.");
        } else {
            Debug.LogError($"Combat Loot Confirmation FAILED. Expected backpack count {beforeCount + 1}, got {afterConfirmCount}, SettlementCompleted={_nodeSettlementCompleted}");
        }

        CombatNode eliteNode = new CombatNode {
            NodeID = "test_elite_reward_loot"
        };
        eliteNode.MonsterIDs.Add("elite_scrap_guard");
        core.Dungeon.CurrentLayer = new DungeonLayer {
            LayerID = 1,
            RootNode = eliteNode,
            CurrentNode = eliteNode
        };

        _preparedLootResult = null;
        _nodeSettlementCompleted = false;
        int beforeEliteCount = ((BackpackGrid)doll.RuntimeGrid).ContainedItems.Count;
        eliteNode.ResolveAfterVictory();
        int afterElitePrepareCount = ((BackpackGrid)doll.RuntimeGrid).ContainedItems.Count;

        if (_preparedLootResult != null &&
            _preparedLootResult.OfferedItems.Count >= 2 &&
            HasOfferedItem(_preparedLootResult, "mat_core_tier1") &&
            afterElitePrepareCount == beforeEliteCount &&
            !_nodeSettlementCompleted) {
            Debug.Log("Combat RewardSystem Integration PASSED.");
        } else {
            Debug.LogError($"Combat RewardSystem Integration FAILED. Offered={_preparedLootResult?.OfferedItems.Count ?? 0}, HasCore={HasOfferedItem(_preparedLootResult, "mat_core_tier1")}, BackpackCount={afterElitePrepareCount}, SettlementCompleted={_nodeSettlementCompleted}");
        }

        DungeonEventBus.OnCombatLootPrepared -= HandleCombatLootPrepared;
        DungeonEventBus.OnNodeSettlementCompleted -= HandleNodeSettlementCompleted;

        Debug.Log("=== Combat Loot Drop Test Finished ===");
    }

    private static bool HasOfferedItem(CombatLootPickupResult result, string configID) {
        if (result?.OfferedItems == null) {
            return false;
        }

        foreach (ItemEntity item in result.OfferedItems) {
            if (item != null && item.ConfigID == configID) {
                return true;
            }
        }

        return false;
    }

    private static void HandleCombatLootPrepared(CombatLootPickupResult result) {
        _preparedLootResult = result;
        Debug.Log($"Received combat loot payload. OfferedCount={result?.OfferedItems.Count ?? 0}");
    }

    private static void HandleNodeSettlementCompleted() {
        _nodeSettlementCompleted = true;
        Debug.Log("Received OnNodeSettlementCompleted event.");
    }
}
