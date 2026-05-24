using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class WorkshopCostPaymentResult {
    public bool Success;
    public string Reason;
    public int MoneySpent;
    public List<ItemEntity> ConsumedItems = new List<ItemEntity>();
}

public static class WorkshopCostService {
    public static bool CanAfford(CraftingCost cost, PlayerProfile player, out string reason) {
        reason = string.Empty;
        if (cost == null) {
            reason = "Cost config is missing.";
            return false;
        }

        if (player == null) {
            reason = "Player profile is missing.";
            return false;
        }

        if (cost.Money < 0) {
            reason = $"Cost money is invalid: {cost.Money}.";
            return false;
        }

        if (player.Money < cost.Money) {
            reason = $"Not enough money. Need={cost.Money}, Current={player.Money}.";
            return false;
        }

        if (cost.RequiredItems == null) {
            return true;
        }

        foreach (CraftingRequirement requirement in cost.RequiredItems) {
            if (requirement == null || string.IsNullOrEmpty(requirement.ConfigID) || requirement.Count <= 0) {
                reason = "Cost has invalid required item entry.";
                return false;
            }

            int ownedCount = CountOwnedItems(player, requirement.ConfigID);
            if (ownedCount < requirement.Count) {
                reason = $"Missing item [{requirement.ConfigID}]. Need={requirement.Count}, Current={ownedCount}.";
                return false;
            }
        }

        return true;
    }

    public static bool TryPayCost(CraftingCost cost, PlayerProfile player, string source, out WorkshopCostPaymentResult result) {
        result = new WorkshopCostPaymentResult();
        if (!CanAfford(cost, player, out string reason)) {
            result.Success = false;
            result.Reason = reason;
            return false;
        }

        if (!TryCollectCostItems(cost, player, out List<ItemEntity> itemsToConsume, out string collectReason)) {
            result.Success = false;
            result.Reason = collectReason;
            return false;
        }

        player.Money -= cost.Money;
        result.MoneySpent = cost.Money;

        bool removedBackpackItem = false;
        foreach (ItemEntity item in itemsToConsume) {
            if (TryRemoveCollectedItem(player, item, out bool wasBackpackItem)) {
                removedBackpackItem |= wasBackpackItem;
                result.ConsumedItems.Add(item);
                MarkConsumed(item);
            }
        }

        if (removedBackpackItem && player.ActiveDoll != null) {
            GridSolver.RecalculateAllEffects(player.ActiveDoll);
        }

        result.Success = true;
        result.Reason = string.Empty;
        Debug.Log($"[WorkshopCostService] {source}: paid {result.MoneySpent}G and {result.ConsumedItems.Count} item(s).");
        return true;
    }

    public static int CountOwnedItems(PlayerProfile player, string configID) {
        if (player == null || string.IsNullOrEmpty(configID)) {
            return 0;
        }

        int count = player.StashInventory.Count(item => item != null && item.ConfigID == configID);
        BackpackGrid grid = player.ActiveDoll?.RuntimeGrid as BackpackGrid;
        if (grid != null) {
            count += grid.ContainedItems.Count(item => item != null && item.ConfigID == configID);
        }

        return count;
    }

    private static bool TryCollectCostItems(CraftingCost cost, PlayerProfile player, out List<ItemEntity> itemsToConsume, out string reason) {
        itemsToConsume = new List<ItemEntity>();
        reason = string.Empty;

        if (player == null) {
            reason = "Player profile is missing.";
            return false;
        }

        if (cost?.RequiredItems == null) {
            return true;
        }

        foreach (CraftingRequirement requirement in cost.RequiredItems) {
            for (int i = 0; i < requirement.Count; i++) {
                ItemEntity item = FindNextConsumableItem(player, requirement.ConfigID, itemsToConsume);
                if (item == null) {
                    reason = $"Missing item [{requirement.ConfigID}].";
                    return false;
                }

                itemsToConsume.Add(item);
            }
        }

        return true;
    }

    private static ItemEntity FindNextConsumableItem(PlayerProfile player, string configID, List<ItemEntity> excludedItems) {
        ItemEntity stashItem = player.StashInventory.FirstOrDefault(item =>
            item != null
            && item.ConfigID == configID
            && !excludedItems.Contains(item));
        if (stashItem != null) {
            return stashItem;
        }

        BackpackGrid grid = player.ActiveDoll?.RuntimeGrid as BackpackGrid;
        return grid?.ContainedItems.FirstOrDefault(item =>
            item != null
            && item.ConfigID == configID
            && !excludedItems.Contains(item));
    }

    private static bool TryRemoveCollectedItem(PlayerProfile player, ItemEntity item, out bool wasBackpackItem) {
        wasBackpackItem = false;
        if (player == null || item == null) {
            return false;
        }

        if (player.StashInventory.Remove(item)) {
            return true;
        }

        BackpackGrid grid = player.ActiveDoll?.RuntimeGrid as BackpackGrid;
        if (grid == null || !grid.ContainedItems.Contains(item)) {
            return false;
        }

        grid.RemoveItem(item);
        wasBackpackItem = true;
        if (!string.IsNullOrEmpty(item.InstanceID)) {
            GameEventBus.PublishItemRemoved(item.InstanceID);
        }

        return true;
    }

    private static void MarkConsumed(ItemEntity item) {
        if (item == null) {
            return;
        }

        if (item.Grid?.CurrentPos != null && item.Grid.CurrentPos.Length >= 2) {
            item.Grid.CurrentPos[0] = -1;
            item.Grid.CurrentPos[1] = -1;
        }
    }
}
