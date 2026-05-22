using UnityEngine;

public struct InventoryInteractionContext {
    public DollEntity ActiveDoll;
    public bool AllowStageDiscard;
    public string Source;

    public static InventoryInteractionContext FromCurrentDoll(string source, bool allowStageDiscard = false) {
        return new InventoryInteractionContext {
            ActiveDoll = GameRoot.Core?.CurrentPlayer?.ActiveDoll,
            AllowStageDiscard = allowStageDiscard,
            Source = source
        };
    }
}

public struct InventoryItemPlacement {
    public bool WasInGrid;
    public int X;
    public int Y;

    public static InventoryItemPlacement Missing => new InventoryItemPlacement {
        WasInGrid = false,
        X = -1,
        Y = -1
    };
}

public static class InventoryInteractionService {
    public static bool TryGetGrid(InventoryInteractionContext context, out BackpackGrid grid, out string reason) {
        grid = context.ActiveDoll?.RuntimeGrid as BackpackGrid;
        if (grid == null) {
            reason = "当前魔偶背包不存在。";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public static bool RequestPickUp(
        ItemEntity item,
        InventoryInteractionContext context,
        out InventoryItemPlacement previousPlacement,
        out string reason) {
        previousPlacement = InventoryItemPlacement.Missing;

        if (!ValidateItem(item, out reason)) {
            return false;
        }

        if (!TryGetGrid(context, out BackpackGrid grid, out reason)) {
            return false;
        }

        if (!grid.ContainedItems.Contains(item)) {
            reason = $"物品 [{item.Name}] 不在当前背包中。";
            return false;
        }

        previousPlacement = new InventoryItemPlacement {
            WasInGrid = true,
            X = item.Grid?.CurrentPos != null && item.Grid.CurrentPos.Length > 0 ? item.Grid.CurrentPos[0] : -1,
            Y = item.Grid?.CurrentPos != null && item.Grid.CurrentPos.Length > 1 ? item.Grid.CurrentPos[1] : -1
        };

        grid.RemoveItem(item);
        Recalculate(context);
        GameEventBus.PublishItemRemoved(item.InstanceID);
        return true;
    }

    public static bool RequestPlace(
        ItemEntity item,
        int x,
        int y,
        InventoryInteractionContext context,
        out string reason) {
        if (!ValidateItem(item, out reason)) {
            return false;
        }

        if (!TryGetGrid(context, out BackpackGrid grid, out reason)) {
            return false;
        }

        if (!grid.PlaceItem(item, x, y)) {
            reason = $"物品 [{item.Name}] 无法放置到 ({x},{y})。";
            return false;
        }

        Recalculate(context);
        GameEventBus.PublishItemPlaced(item.InstanceID, x, y);
        return true;
    }

    public static bool RequestRestore(
        ItemEntity item,
        InventoryItemPlacement placement,
        InventoryInteractionContext context,
        out string reason) {
        if (!placement.WasInGrid) {
            reason = "物品没有可恢复的背包位置。";
            return false;
        }

        return RequestPlace(item, placement.X, placement.Y, context, out reason);
    }

    public static bool RequestStageDiscard(
        ItemEntity item,
        InventoryInteractionContext context,
        out string reason) {
        if (!ValidateItem(item, out reason)) {
            return false;
        }

        if (!context.AllowStageDiscard) {
            reason = "当前界面不允许暂存丢弃背包物品。";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    public static bool CanPlaceItem(ItemEntity item, int x, int y, InventoryInteractionContext context) {
        return ValidateItem(item, out _)
            && TryGetGrid(context, out BackpackGrid grid, out _)
            && grid.CanPlaceItem(item, x, y);
    }

    private static bool ValidateItem(ItemEntity item, out string reason) {
        if (item == null) {
            reason = "物品数据不存在。";
            return false;
        }

        reason = string.Empty;
        return true;
    }

    private static void Recalculate(InventoryInteractionContext context) {
        if (context.ActiveDoll != null) {
            GridSolver.RecalculateAllEffects(context.ActiveDoll);
        }
    }
}
