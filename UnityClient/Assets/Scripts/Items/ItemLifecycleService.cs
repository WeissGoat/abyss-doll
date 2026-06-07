using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ItemLifecycleResult {
    public bool Success;
    public string Reason;
    public ItemEntity Item;
    public ItemLifecycleTransition Transition;
    public ItemContainerType FromContainer;
    public ItemContainerType ToContainer;
    public int MoneyDelta;

    public static ItemLifecycleResult Fail(string reason, ItemEntity item = null) {
        return new ItemLifecycleResult {
            Success = false,
            Reason = reason,
            Item = item,
            Transition = ItemLifecycleTransition.None,
            FromContainer = item != null ? item.ContainerType : ItemContainerType.Unknown,
            ToContainer = item != null ? item.ContainerType : ItemContainerType.Unknown
        };
    }

    public static ItemLifecycleResult Complete(
        ItemEntity item,
        ItemLifecycleTransition transition,
        ItemContainerType fromContainer,
        ItemContainerType toContainer,
        int moneyDelta = 0) {
        return new ItemLifecycleResult {
            Success = true,
            Reason = string.Empty,
            Item = item,
            Transition = transition,
            FromContainer = fromContainer,
            ToContainer = toContainer,
            MoneyDelta = moneyDelta
        };
    }
}

public class ItemLifecycleBatchResult {
    public bool Success = true;
    public string Reason = string.Empty;
    public List<ItemLifecycleResult> Results = new List<ItemLifecycleResult>();
    public int ItemCount;
    public int EstimatedValue;
    public int MoneyDelta;

    public void Add(ItemLifecycleResult result) {
        if (result == null) {
            return;
        }

        Results.Add(result);
        if (!result.Success) {
            Success = false;
            if (string.IsNullOrEmpty(Reason)) {
                Reason = result.Reason;
            }
            return;
        }

        ItemCount++;
        MoneyDelta += result.MoneyDelta;
        if (result.Item != null) {
            EstimatedValue += result.Item.BaseValue;
        }
    }
}

public static class ItemLifecycleService {
    public static void MarkGeneratedToInbox(ItemEntity item, string source) {
        if (item == null) {
            return;
        }

        EnsureRuntimeState(item);
        item.OwnerScope = ItemOwnerScope.Run;
        item.ContainerType = ItemContainerType.Inbox;
        Debug.Log($"[ItemLifecycle] {source}: generated [{item.Name}] into Inbox.");
    }

    public static void MarkBackpackItem(ItemEntity item, ItemOwnerScope fallbackOwnerScope = ItemOwnerScope.Run, bool bindItem = false) {
        if (item == null) {
            return;
        }

        EnsureRuntimeState(item);
        if (item.OwnerScope == ItemOwnerScope.Unknown
            || item.OwnerScope == ItemOwnerScope.Consumed
            || item.OwnerScope == ItemOwnerScope.Lost) {
            item.OwnerScope = fallbackOwnerScope;
        }

        item.ContainerType = ItemContainerType.Backpack;
        item.IsBinding = item.IsBinding || bindItem;
    }

    public static void MarkGroundInventoryItem(PlayerProfile player, ItemEntity item, string source) {
        if (player == null || item == null) {
            return;
        }

        EnsureRuntimeState(item);
        if (!player.StashInventory.Contains(item)) {
            player.StashInventory.Add(item);
        }

        MoveToGroundInventory(item);
        Debug.Log($"[ItemLifecycle] {source}: registered [{item.Name}] in GroundInventory.");
    }

    public static int CountOwnedItems(PlayerProfile player, string configID) {
        if (player == null || string.IsNullOrEmpty(configID)) {
            return 0;
        }

        int count = player.StashInventory.Count(item => item != null && item.ConfigID == configID);
        BackpackGrid grid = ResolveGrid(player);
        if (grid != null) {
            count += grid.ContainedItems.Count(item => item != null && item.ConfigID == configID);
        }

        return count;
    }

    public static bool TryConsumeBackpackItem(PlayerProfile player, ItemEntity item, string source, out ItemLifecycleResult result) {
        result = ValidatePlayerAndItem(player, item);
        if (result != null) {
            return false;
        }

        BackpackGrid grid = ResolveGrid(player);
        if (grid == null || !grid.ContainedItems.Contains(item)) {
            result = ItemLifecycleResult.Fail($"物品 [{item.Name}] 不在当前背包中，无法消耗。", item);
            return false;
        }

        ItemContainerType fromContainer = item.ContainerType;
        RemoveFromBackpack(player, grid, item, true);
        MoveToTerminal(item, ItemOwnerScope.Consumed, ItemContainerType.Consumed);
        RecalculateBackpack(player);
        result = ItemLifecycleResult.Complete(item, ItemLifecycleTransition.BackpackToConsumed, fromContainer, ItemContainerType.Consumed);
        Debug.Log($"[ItemLifecycle] {source}: consumed backpack item [{item.Name}].");
        return true;
    }

    public static bool TryConsumeOwnedItemForCost(PlayerProfile player, string configID, string source, out ItemEntity consumedItem, out ItemLifecycleResult result) {
        consumedItem = null;
        result = null;

        if (player == null) {
            result = ItemLifecycleResult.Fail("玩家数据不存在，无法扣除材料。");
            return false;
        }

        if (string.IsNullOrEmpty(configID)) {
            result = ItemLifecycleResult.Fail("材料配置 ID 为空，无法扣除材料。");
            return false;
        }

        ItemEntity stashItem = player.StashInventory.FirstOrDefault(item => item != null && item.ConfigID == configID);
        if (stashItem != null) {
            player.StashInventory.Remove(stashItem);
            ItemContainerType fromContainer = stashItem.ContainerType;
            MoveToTerminal(stashItem, ItemOwnerScope.Consumed, ItemContainerType.Consumed);
            consumedItem = stashItem;
            result = ItemLifecycleResult.Complete(stashItem, ItemLifecycleTransition.GroundInventoryToConsumed, fromContainer, ItemContainerType.Consumed);
            Debug.Log($"[ItemLifecycle] {source}: consumed ground inventory item [{stashItem.Name}] for cost.");
            return true;
        }

        BackpackGrid grid = ResolveGrid(player);
        ItemEntity backpackItem = grid?.ContainedItems.FirstOrDefault(item => item != null && item.ConfigID == configID);
        if (grid == null || backpackItem == null) {
            result = ItemLifecycleResult.Fail($"缺少材料 [{configID}]。");
            return false;
        }

        ItemContainerType backpackFromContainer = backpackItem.ContainerType;
        RemoveFromBackpack(player, grid, backpackItem, true);
        MoveToTerminal(backpackItem, ItemOwnerScope.Consumed, ItemContainerType.Consumed);
        RecalculateBackpack(player);
        consumedItem = backpackItem;
        result = ItemLifecycleResult.Complete(backpackItem, ItemLifecycleTransition.BackpackToConsumed, backpackFromContainer, ItemContainerType.Consumed);
        Debug.Log($"[ItemLifecycle] {source}: consumed backpack item [{backpackItem.Name}] for cost.");
        return true;
    }

    public static bool TrySellItem(PlayerProfile player, ItemEntity item, string source, out ItemLifecycleResult result) {
        result = ValidatePlayerAndItem(player, item);
        if (result != null) {
            return false;
        }

        BackpackGrid grid = ResolveGrid(player);
        if (grid != null && grid.ContainedItems.Contains(item)) {
            ItemContainerType fromContainer = item.ContainerType;
            RemoveFromBackpack(player, grid, item, true);
            player.Money += item.BaseValue;
            MoveToTerminal(item, ItemOwnerScope.Workshop, ItemContainerType.Sold);
            RecalculateBackpack(player);
            result = ItemLifecycleResult.Complete(item, ItemLifecycleTransition.BackpackToSold, fromContainer, ItemContainerType.Sold, item.BaseValue);
            Debug.Log($"[ItemLifecycle] {source}: sold backpack item [{item.Name}] for {item.BaseValue}G.");
            return true;
        }

        if (player.StashInventory.Contains(item)) {
            ItemContainerType fromContainer = item.ContainerType;
            player.StashInventory.Remove(item);
            player.Money += item.BaseValue;
            MoveToTerminal(item, ItemOwnerScope.Workshop, ItemContainerType.Sold);
            result = ItemLifecycleResult.Complete(item, ItemLifecycleTransition.GroundInventoryToSold, fromContainer, ItemContainerType.Sold, item.BaseValue);
            Debug.Log($"[ItemLifecycle] {source}: sold ground inventory item [{item.Name}] for {item.BaseValue}G.");
            return true;
        }

        result = ItemLifecycleResult.Fail($"物品 [{item.Name}] 不在背包或局外库存中，无法出售。", item);
        return false;
    }

    public static ItemLifecycleBatchResult SellAllSellableItems(PlayerProfile player, string source) {
        ItemLifecycleBatchResult batch = new ItemLifecycleBatchResult();
        if (player == null) {
            batch.Success = false;
            batch.Reason = "玩家数据不存在，无法出售。";
            return batch;
        }

        BackpackGrid grid = ResolveGrid(player);
        List<ItemEntity> targets = new List<ItemEntity>();
        if (grid != null) {
            targets.AddRange(grid.ContainedItems.Where(item => item != null));
        }
        targets.AddRange(player.StashInventory.Where(item => item != null));

        foreach (ItemEntity item in targets) {
            TrySellItem(player, item, source, out ItemLifecycleResult result);
            batch.Add(result);
        }

        return batch;
    }

    public static bool MarkDetachedItemLost(ItemEntity item, string source, out ItemLifecycleResult result) {
        result = null;
        if (item == null) {
            result = ItemLifecycleResult.Fail("物品数据不存在，无法丢弃。");
            return false;
        }

        EnsureRuntimeState(item);
        ItemContainerType fromContainer = item.ContainerType;
        MoveToTerminal(item, ItemOwnerScope.Lost, ItemContainerType.Lost);
        result = ItemLifecycleResult.Complete(item, ItemLifecycleTransition.ActiveDiscardToLost, fromContainer, ItemContainerType.Lost);
        Debug.Log($"[ItemLifecycle] {source}: discarded detached item [{item.Name}] to Lost.");
        return true;
    }

    public static bool MarkInboxItemLost(ItemEntity item, string source, out ItemLifecycleResult result) {
        result = null;
        if (item == null) {
            result = ItemLifecycleResult.Fail("物品数据不存在，无法丢弃。");
            return false;
        }

        EnsureRuntimeState(item);
        ItemContainerType fromContainer = item.ContainerType;
        MoveToTerminal(item, ItemOwnerScope.Lost, ItemContainerType.Lost);
        result = ItemLifecycleResult.Complete(item, ItemLifecycleTransition.InboxToLost, fromContainer, ItemContainerType.Lost);
        Debug.Log($"[ItemLifecycle] {source}: unresolved inbox item [{item.Name}] moved to Lost.");
        return true;
    }

    public static ItemLifecycleBatchResult ExtractBackpackItemsToGroundInventory(PlayerProfile player, string source, Predicate<ItemEntity> filter = null) {
        ItemLifecycleBatchResult batch = new ItemLifecycleBatchResult();
        BackpackGrid grid = ResolveGrid(player);
        if (player == null || grid == null) {
            batch.Success = false;
            batch.Reason = "玩家背包不存在，无法撤离结算。";
            return batch;
        }

        List<ItemEntity> targets = grid.ContainedItems
            .Where(item => item != null && (filter == null || filter(item)))
            .ToList();

        foreach (ItemEntity item in targets) {
            ItemContainerType fromContainer = item.ContainerType;
            RemoveFromBackpack(player, grid, item, true);
            if (!player.StashInventory.Contains(item)) {
                player.StashInventory.Add(item);
            }
            MoveToGroundInventory(item);
            batch.Add(ItemLifecycleResult.Complete(item, ItemLifecycleTransition.BackpackToGroundInventory, fromContainer, ItemContainerType.GroundInventory));
        }

        RecalculateBackpack(player);
        Debug.Log($"[ItemLifecycle] {source}: extracted {batch.ItemCount} backpack item(s) to GroundInventory.");
        return batch;
    }

    public static ItemLifecycleBatchResult ResolveDefeatBackpackItems(PlayerProfile player, string source) {
        ItemLifecycleBatchResult batch = new ItemLifecycleBatchResult();
        BackpackGrid grid = ResolveGrid(player);
        if (player == null || grid == null) {
            batch.Success = false;
            batch.Reason = "玩家背包不存在，无法战败结算。";
            return batch;
        }

        List<ItemEntity> targets = grid.ContainedItems.Where(item => item != null).ToList();
        foreach (ItemEntity item in targets) {
            ItemContainerType fromContainer = item.ContainerType;
            RemoveFromBackpack(player, grid, item, true);
            if (ShouldPreserveOnDefeat(item)) {
                if (!player.StashInventory.Contains(item)) {
                    player.StashInventory.Add(item);
                }
                MoveToGroundInventory(item);
                batch.Add(ItemLifecycleResult.Complete(item, ItemLifecycleTransition.BackpackToGroundInventory, fromContainer, ItemContainerType.GroundInventory));
            } else {
                MoveToTerminal(item, ItemOwnerScope.Lost, ItemContainerType.Lost);
                batch.Add(ItemLifecycleResult.Complete(item, ItemLifecycleTransition.BackpackToLost, fromContainer, ItemContainerType.Lost));
            }
        }

        RecalculateBackpack(player);
        Debug.Log($"[ItemLifecycle] {source}: resolved defeat for {batch.ItemCount} backpack item(s).");
        return batch;
    }

    private static ItemLifecycleResult ValidatePlayerAndItem(PlayerProfile player, ItemEntity item) {
        if (player == null) {
            return ItemLifecycleResult.Fail("玩家数据不存在。", item);
        }

        if (item == null) {
            return ItemLifecycleResult.Fail("物品数据不存在。");
        }

        EnsureRuntimeState(item);
        return null;
    }

    private static BackpackGrid ResolveGrid(PlayerProfile player) {
        return player?.ActiveDoll?.RuntimeGrid as BackpackGrid;
    }

    private static void RemoveFromBackpack(PlayerProfile player, BackpackGrid grid, ItemEntity item, bool publishEvent) {
        if (grid == null || item == null || !grid.ContainedItems.Contains(item)) {
            return;
        }

        grid.RemoveItem(item);
        if (publishEvent && !string.IsNullOrEmpty(item.InstanceID)) {
            GameEventBus.PublishItemRemoved(item.InstanceID);
        }
    }

    private static void RecalculateBackpack(PlayerProfile player) {
        if (player?.ActiveDoll != null) {
            GridSolver.RecalculateAllEffects(player.ActiveDoll);
        }
    }

    private static void MoveToTerminal(ItemEntity item, ItemOwnerScope ownerScope, ItemContainerType containerType) {
        EnsureRuntimeState(item);
        item.OwnerScope = ownerScope;
        item.ContainerType = containerType;
        ClearGridPosition(item);
    }

    private static void MoveToGroundInventory(ItemEntity item) {
        EnsureRuntimeState(item);
        item.OwnerScope = ItemOwnerScope.Workshop;
        item.ContainerType = ItemContainerType.GroundInventory;
        ClearGridPosition(item);
    }

    private static void ClearGridPosition(ItemEntity item) {
        if (item.Grid?.CurrentPos != null && item.Grid.CurrentPos.Length >= 2) {
            item.Grid.CurrentPos[0] = -1;
            item.Grid.CurrentPos[1] = -1;
        }
    }

    private static bool ShouldPreserveOnDefeat(ItemEntity item) {
        if (item == null) {
            return false;
        }

        if (item.IsBinding) {
            return true;
        }

        return item.Tags != null && item.Tags.Contains("Bond");
    }

    private static void EnsureRuntimeState(ItemEntity item) {
        if (item == null) {
            return;
        }

        if (item.Tags == null) {
            item.Tags = new List<string>();
        }

        if (item.DynamicTags == null) {
            item.DynamicTags = new List<string>();
        }

        if (item.Durability <= 0f) {
            item.Durability = 1f;
        }
    }
}
