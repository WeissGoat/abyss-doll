using System;
using System.Reflection;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

public static class CombatLootDropTest {
    private static CombatLootPickupResult _preparedLootResult;
    private static CombatLootCollectionResult _lastCollectionResult;
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
        _lastCollectionResult = null;
        _nodeSettlementCompleted = false;
        DungeonEventBus.OnCombatLootPrepared += HandleCombatLootPrepared;
        DungeonEventBus.OnCombatLootCollected += HandleCombatLootCollected;
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

        ItemEntity acceptedOfferedItem = null;
        bool placedOfferedItem = false;
        if (_preparedLootResult != null) {
            acceptedOfferedItem = _preparedLootResult.OfferedItems[0];
            placedOfferedItem = PlaceViaInventoryService(acceptedOfferedItem, 0, 0, "CombatLootAccept", out string placeReason);
            if (!placedOfferedItem) {
                Debug.LogError($"Combat Loot Confirmation setup FAILED. Could not place offered item through InventoryInteractionService: {placeReason}");
            }
            combatNode.ConfirmLootCollection();
        }

        int afterConfirmCount = ((BackpackGrid)doll.RuntimeGrid).ContainedItems.Count;
        bool acceptedCollectionResult = _lastCollectionResult != null
            && acceptedOfferedItem != null
            && _lastCollectionResult.AcceptedItems.Contains(acceptedOfferedItem)
            && _lastCollectionResult.DiscardedItems.Count == 0;
        if (placedOfferedItem && afterConfirmCount == beforeCount + 1 && _nodeSettlementCompleted && acceptedCollectionResult) {
            Debug.Log("Combat Loot Confirmation PASSED.");
        } else {
            Debug.LogError($"Combat Loot Confirmation FAILED. PlacedByService={placedOfferedItem}, Expected backpack count {beforeCount + 1}, got {afterConfirmCount}, SettlementCompleted={_nodeSettlementCompleted}, AcceptedResult={acceptedCollectionResult}");
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

        RunCombatLootConfirmationRequiresBackpackPlacement(core);

        DungeonEventBus.OnCombatLootPrepared -= HandleCombatLootPrepared;
        DungeonEventBus.OnCombatLootCollected -= HandleCombatLootCollected;
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
        InventoryPresentationController presentation = flowObj.AddComponent<InventoryPresentationController>();
        presentation.SetContext(InventoryPresentationMode.CombatLoot);

        var doll = core.CurrentPlayer.ActiveDoll;
        doll.RuntimeGrid = new BackpackGrid(doll.Chassis);
        BackpackGrid grid = doll.RuntimeGrid as BackpackGrid;
        ItemEntity backpackItem = ConfigManager.CreateItem("loot_gear_scrap");
        string placeReason = string.Empty;
        bool placed = grid != null
            && backpackItem != null
            && PlaceViaInventoryService(backpackItem, 0, 0, "CombatLootDiscardUITestSetup", out placeReason);
        if (!placed) {
            Debug.LogError($"Combat Loot Backpack Discard UI FAILED. Could not place test backpack item through InventoryInteractionService: {placeReason}");
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

    private static void RunCombatLootConfirmationRequiresBackpackPlacement(CoreBackend core) {
        var doll = core.CurrentPlayer.ActiveDoll;
        doll.RuntimeGrid = new BackpackGrid(doll.Chassis);
        BackpackGrid grid = doll.RuntimeGrid as BackpackGrid;

        CombatNode discardNode = new CombatNode {
            NodeID = "test_unplaced_loot_discard"
        };
        discardNode.MonsterIDs.Add("mob_scavenger_bug");
        core.Dungeon.CurrentLayer = new DungeonLayer {
            LayerID = 1,
            RootNode = discardNode,
            CurrentNode = discardNode
        };

        _preparedLootResult = null;
        _lastCollectionResult = null;
        _nodeSettlementCompleted = false;

        int beforeCount = grid?.ContainedItems.Count ?? -1;
        discardNode.ResolveAfterVictory();
        int afterPrepareCount = grid?.ContainedItems.Count ?? -1;
        ItemEntity offeredItem = _preparedLootResult?.OfferedItems.Count > 0
            ? _preparedLootResult.OfferedItems[0]
            : null;

        discardNode.ConfirmLootCollection();
        int afterConfirmCount = grid?.ContainedItems.Count ?? -1;
        bool discardedCollectionResult = _lastCollectionResult != null
            && offeredItem != null
            && _lastCollectionResult.AcceptedItems.Count == 0
            && _lastCollectionResult.DiscardedItems.Contains(offeredItem);

        if (_preparedLootResult != null
            && offeredItem != null
            && afterPrepareCount == beforeCount
            && afterConfirmCount == beforeCount
            && _nodeSettlementCompleted
            && discardedCollectionResult) {
            Debug.Log("Combat Loot Unplaced Confirmation PASSED.");
        } else {
            Debug.LogError($"Combat Loot Unplaced Confirmation FAILED. Offered={_preparedLootResult?.OfferedItems.Count ?? 0}, Before={beforeCount}, AfterPrepare={afterPrepareCount}, AfterConfirm={afterConfirmCount}, SettlementCompleted={_nodeSettlementCompleted}, DiscardedResult={discardedCollectionResult}");
        }
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

    private static bool PlaceViaInventoryService(ItemEntity item, int x, int y, string source, out string reason) {
        InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll(source);
        return InventoryInteractionService.RequestPlace(item, x, y, context, out reason);
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

    private static void HandleCombatLootCollected(CombatLootCollectionResult result) {
        _lastCollectionResult = result;
        Debug.Log($"Received combat loot collection. Accepted={result?.AcceptedItems.Count ?? 0}, Discarded={result?.DiscardedItems.Count ?? 0}");
    }

    private static void HandleNodeSettlementCompleted() {
        _nodeSettlementCompleted = true;
        Debug.Log("Received OnNodeSettlementCompleted event.");
    }
}
