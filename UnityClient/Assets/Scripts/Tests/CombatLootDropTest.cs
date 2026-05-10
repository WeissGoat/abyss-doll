using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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

        RunCombatLootBackpackDiscardUITest(core);

        Debug.Log("=== Combat Loot Drop Test Finished ===");
    }

    private static void RunCombatLootBackpackDiscardUITest(CoreBackend core) {
        GameObject canvasObj = new GameObject("CombatLootDiscardUITestCanvas");
        canvasObj.AddComponent<Canvas>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject flowObj = new GameObject("GameFlowController");
        GameFlowController flow = flowObj.AddComponent<GameFlowController>();
        SetGameFlowInstance(flow);
        SetGameFlowScreen(flow, "CombatLoot");

        var doll = core.CurrentPlayer.ActiveDoll;
        doll.RuntimeGrid = new BackpackGrid(doll.Chassis);
        BackpackGrid grid = doll.RuntimeGrid as BackpackGrid;
        ItemEntity backpackItem = ConfigManager.CreateItem("loot_gear_scrap");
        bool placed = grid != null && backpackItem != null && grid.PlaceItem(backpackItem, 0, 0);
        if (!placed) {
            Debug.LogError("Combat Loot Backpack Discard UI FAILED. Could not place test backpack item.");
            UnityEngine.Object.DestroyImmediate(canvasObj);
            UnityEngine.Object.DestroyImmediate(flowObj);
            return;
        }

        GameObject slotObj = new GameObject("Slot_0_0");
        slotObj.transform.SetParent(canvasObj.transform, false);
        slotObj.AddComponent<RectTransform>();

        GameObject itemObj = new GameObject("BackpackItemUI");
        itemObj.transform.SetParent(canvasObj.transform, false);
        itemObj.AddComponent<RectTransform>();
        itemObj.AddComponent<Image>();
        itemObj.AddComponent<CanvasGroup>();
        DraggableItemUI itemUI = itemObj.AddComponent<DraggableItemUI>();
        itemUI.SetupData(backpackItem);
        itemUI.SnapToSlot(slotObj.transform, 0, 0);

        PointerEventData eventData = new PointerEventData(null) {
            position = new Vector2(32f, 32f)
        };

        itemUI.OnBeginDrag(eventData);
        itemUI.OnEndDrag(eventData);

        bool canStage = flow.CanStageRemovedBackpackItems();
        bool stagedForDiscard = itemUI != null && itemUI.IsPendingDiscard;
        bool removedFromGrid = placed && grid != null && !grid.ContainedItems.Contains(backpackItem);

        InvokeDiscardDetachedBackpackItems(flow);
        bool queuedOrDestroyedAfterDiscard = Application.isPlaying || itemUI == null;

        if (canStage && stagedForDiscard && removedFromGrid && queuedOrDestroyedAfterDiscard) {
            Debug.Log("Combat Loot Backpack Discard UI PASSED.");
        } else {
            Debug.LogError($"Combat Loot Backpack Discard UI FAILED. CanStage={canStage}, Pending={stagedForDiscard}, RemovedFromGrid={removedFromGrid}, QueuedOrDestroyed={queuedOrDestroyedAfterDiscard}");
        }

        UnityEngine.Object.DestroyImmediate(canvasObj);
        UnityEngine.Object.DestroyImmediate(flowObj);
    }

    private static void SetGameFlowScreen(GameFlowController controller, string screenName) {
        Type screenType = typeof(GameFlowController).GetNestedType("GameScreenState", BindingFlags.NonPublic);
        object state = Enum.Parse(screenType, screenName);
        typeof(GameFlowController)
            .GetField("_currentScreen", BindingFlags.Instance | BindingFlags.NonPublic)
            .SetValue(controller, state);
    }

    private static void SetGameFlowInstance(GameFlowController controller) {
        typeof(GameFlowController)
            .GetProperty("Instance", BindingFlags.Static | BindingFlags.Public)
            .GetSetMethod(true)
            .Invoke(null, new object[] { controller });
    }

    private static void InvokeDiscardDetachedBackpackItems(GameFlowController controller) {
        typeof(GameFlowController)
            .GetMethod("DiscardDetachedBackpackItems", BindingFlags.Instance | BindingFlags.NonPublic)
            .Invoke(controller, null);
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
