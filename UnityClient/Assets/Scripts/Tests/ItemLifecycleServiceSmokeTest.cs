using UnityEngine;

public static class ItemLifecycleServiceSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Item Lifecycle Service Smoke Test ===");

        TestRuntimeDefaultsAndInboxDiscard();
        TestBackpackConsumeAndSell();
        TestCostConsumptionPriority();
        TestEvacuationExtraction();
        TestDefeatResolution();

        Debug.Log("=== Item Lifecycle Service Smoke Test Finished ===");
    }

    private static void TestRuntimeDefaultsAndInboxDiscard() {
        CoreBackend core = CreateCore();
        ItemEntity inboxItem = ConfigManager.CreateItem("loot_gear_scrap");

        bool createdAsGenerated = inboxItem != null
            && inboxItem.ContainerType == ItemContainerType.Generated
            && inboxItem.OwnerScope == ItemOwnerScope.Unknown
            && inboxItem.DynamicTags != null
            && Mathf.Approximately(inboxItem.Durability, 1f);

        ItemLifecycleService.MarkGeneratedToInbox(inboxItem, "LifecycleSmokeInbox");
        bool movedToInbox = inboxItem.ContainerType == ItemContainerType.Inbox
            && inboxItem.OwnerScope == ItemOwnerScope.Run;

        bool lost = ItemLifecycleService.MarkInboxItemLost(inboxItem, "LifecycleSmokeInbox", out ItemLifecycleResult lostResult);
        bool lostStateValid = lost
            && lostResult.Transition == ItemLifecycleTransition.InboxToLost
            && inboxItem.ContainerType == ItemContainerType.Lost
            && inboxItem.OwnerScope == ItemOwnerScope.Lost
            && inboxItem.Grid.CurrentPos[0] == -1
            && inboxItem.Grid.CurrentPos[1] == -1;

        if (createdAsGenerated && movedToInbox && lostStateValid) {
            Debug.Log("Item Lifecycle Inbox Discard PASSED.");
        } else {
            Debug.LogError($"Item Lifecycle Inbox Discard FAILED. Created={createdAsGenerated}, Inbox={movedToInbox}, Lost={lostStateValid}, Result={lostResult?.Transition}");
        }
    }

    private static void TestBackpackConsumeAndSell() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        BackpackGrid grid = player.ActiveDoll.RuntimeGrid as BackpackGrid;

        ItemEntity repairKit = grid?.ContainedItems.Find(item => item != null && item.ConfigID == "con_repair_kit");
        int beforeConsumeCount = grid?.ContainedItems.Count ?? -1;
        bool consumed = ItemLifecycleService.TryConsumeBackpackItem(player, repairKit, "LifecycleSmokeConsume", out ItemLifecycleResult consumeResult);
        bool consumeValid = consumed
            && consumeResult.Transition == ItemLifecycleTransition.BackpackToConsumed
            && repairKit.ContainerType == ItemContainerType.Consumed
            && !grid.ContainedItems.Contains(repairKit)
            && grid.ContainedItems.Count == beforeConsumeCount - 1;

        ItemEntity shield = grid?.ContainedItems.Find(item => item != null && item.ConfigID == "gear_wooden_shield");
        int beforeMoney = player.Money;
        int beforeSellCount = grid?.ContainedItems.Count ?? -1;
        bool sold = ItemLifecycleService.TrySellItem(player, shield, "LifecycleSmokeSell", out ItemLifecycleResult sellResult);
        bool sellValid = sold
            && sellResult.Transition == ItemLifecycleTransition.BackpackToSold
            && player.Money == beforeMoney + shield.BaseValue
            && shield.ContainerType == ItemContainerType.Sold
            && !grid.ContainedItems.Contains(shield)
            && grid.ContainedItems.Count == beforeSellCount - 1;

        if (consumeValid && sellValid) {
            Debug.Log("Item Lifecycle Backpack Consume Sell PASSED.");
        } else {
            Debug.LogError($"Item Lifecycle Backpack Consume Sell FAILED. Consume={consumeValid} ({consumeResult?.Reason}), Sell={sellValid} ({sellResult?.Reason})");
        }
    }

    private static void TestCostConsumptionPriority() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        BackpackGrid grid = ResetBackpack(core);

        ItemEntity groundMaterial = ConfigManager.CreateItem("loot_gear_scrap");
        ItemEntity backpackMaterial = ConfigManager.CreateItem("loot_gear_scrap");
        ItemLifecycleService.MarkGroundInventoryItem(player, groundMaterial, "LifecycleSmokeCost");
        grid.PlaceItem(backpackMaterial, 0, 0);
        ItemLifecycleService.MarkBackpackItem(backpackMaterial, ItemOwnerScope.Run);

        bool firstConsumed = ItemLifecycleService.TryConsumeOwnedItemForCost(player, "loot_gear_scrap", "LifecycleSmokeCost", out ItemEntity firstItem, out ItemLifecycleResult firstResult);
        bool firstValid = firstConsumed
            && firstItem == groundMaterial
            && firstResult.Transition == ItemLifecycleTransition.GroundInventoryToConsumed
            && !player.StashInventory.Contains(groundMaterial)
            && grid.ContainedItems.Contains(backpackMaterial);

        bool secondConsumed = ItemLifecycleService.TryConsumeOwnedItemForCost(player, "loot_gear_scrap", "LifecycleSmokeCost", out ItemEntity secondItem, out ItemLifecycleResult secondResult);
        bool secondValid = secondConsumed
            && secondItem == backpackMaterial
            && secondResult.Transition == ItemLifecycleTransition.BackpackToConsumed
            && !grid.ContainedItems.Contains(backpackMaterial)
            && backpackMaterial.ContainerType == ItemContainerType.Consumed;

        if (firstValid && secondValid) {
            Debug.Log("Item Lifecycle Cost Consumption PASSED.");
        } else {
            Debug.LogError($"Item Lifecycle Cost Consumption FAILED. First={firstValid} ({firstResult?.Reason}), Second={secondValid} ({secondResult?.Reason})");
        }
    }

    private static void TestEvacuationExtraction() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        BackpackGrid grid = ResetBackpack(core);

        ItemEntity loot = ConfigManager.CreateItem("loot_gear_scrap");
        ItemEntity boundShield = ConfigManager.CreateItem("gear_wooden_shield");
        grid.PlaceItem(loot, 0, 0);
        ItemLifecycleService.MarkBackpackItem(loot, ItemOwnerScope.Run);
        grid.PlaceItem(boundShield, 1, 0);
        ItemLifecycleService.MarkBackpackItem(boundShield, ItemOwnerScope.Workshop, true);

        ItemLifecycleBatchResult extraction = ItemLifecycleService.ExtractBackpackItemsToGroundInventory(player, "LifecycleSmokeEvacuate");
        bool extractionValid = extraction.Success
            && extraction.ItemCount == 2
            && grid.ContainedItems.Count == 0
            && player.StashInventory.Contains(loot)
            && player.StashInventory.Contains(boundShield)
            && loot.ContainerType == ItemContainerType.GroundInventory
            && boundShield.ContainerType == ItemContainerType.GroundInventory
            && loot.Grid.CurrentPos[0] == -1
            && boundShield.Grid.CurrentPos[0] == -1;

        if (extractionValid) {
            Debug.Log("Item Lifecycle Evacuation Extraction PASSED.");
        } else {
            Debug.LogError($"Item Lifecycle Evacuation Extraction FAILED. Success={extraction.Success}, Count={extraction.ItemCount}, Grid={grid.ContainedItems.Count}, Stash={player.StashInventory.Count}");
        }
    }

    private static void TestDefeatResolution() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        BackpackGrid grid = ResetBackpack(core);

        ItemEntity unboundLoot = ConfigManager.CreateItem("loot_gear_scrap");
        ItemEntity boundShield = ConfigManager.CreateItem("gear_wooden_shield");
        grid.PlaceItem(unboundLoot, 0, 0);
        ItemLifecycleService.MarkBackpackItem(unboundLoot, ItemOwnerScope.Run);
        grid.PlaceItem(boundShield, 1, 0);
        ItemLifecycleService.MarkBackpackItem(boundShield, ItemOwnerScope.Workshop, true);

        ItemLifecycleBatchResult defeat = ItemLifecycleService.ResolveDefeatBackpackItems(player, "LifecycleSmokeDefeat");
        bool defeatValid = defeat.Success
            && defeat.ItemCount == 2
            && grid.ContainedItems.Count == 0
            && unboundLoot.ContainerType == ItemContainerType.Lost
            && boundShield.ContainerType == ItemContainerType.GroundInventory
            && player.StashInventory.Contains(boundShield)
            && !player.StashInventory.Contains(unboundLoot);

        if (defeatValid) {
            Debug.Log("Item Lifecycle Defeat Resolution PASSED.");
        } else {
            Debug.LogError($"Item Lifecycle Defeat Resolution FAILED. Success={defeat.Success}, Count={defeat.ItemCount}, Grid={grid.ContainedItems.Count}, Stash={player.StashInventory.Count}, Loot={unboundLoot.ContainerType}, Bound={boundShield.ContainerType}");
        }
    }

    private static CoreBackend CreateCore() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        return core;
    }

    private static BackpackGrid ResetBackpack(CoreBackend core) {
        BackpackGrid grid = new BackpackGrid(core.CurrentPlayer.ActiveDoll.Chassis);
        core.CurrentPlayer.ActiveDoll.RuntimeGrid = grid;
        return grid;
    }
}
