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
    public int Rotation;

    public static InventoryItemPlacement Missing => new InventoryItemPlacement {
        WasInGrid = false,
        X = -1,
        Y = -1,
        Rotation = 0
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
            Y = item.Grid?.CurrentPos != null && item.Grid.CurrentPos.Length > 1 ? item.Grid.CurrentPos[1] : -1,
            Rotation = item.Grid != null ? BackpackGrid.NormalizeRotation(item.Grid.Rotation) : 0
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
        int rotation = item?.Grid != null ? item.Grid.Rotation : 0;
        return RequestPlace(item, x, y, rotation, context, out reason);
    }

    public static bool RequestPlace(
        ItemEntity item,
        int x,
        int y,
        int rotation,
        InventoryInteractionContext context,
        out string reason) {
        if (!ValidateItem(item, out reason)) {
            return false;
        }

        if (!TryGetGrid(context, out BackpackGrid grid, out reason)) {
            return false;
        }

        BackpackPlacementResult placement = grid.EvaluatePlacement(item, x, y, rotation);
        if (!placement.CanPlace) {
            reason = string.IsNullOrEmpty(placement.Reason)
                ? $"物品 [{item.Name}] 无法放置到 ({x},{y})。"
                : placement.Reason;
            return false;
        }

        grid.PlaceItem(item, x, y, placement.Rotation);
        ItemLifecycleService.MarkBackpackItem(item, ItemOwnerScope.Run);
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

        return RequestPlace(item, placement.X, placement.Y, placement.Rotation, context, out reason);
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
        int rotation = item?.Grid != null ? item.Grid.Rotation : 0;
        return CanPlaceItem(item, x, y, rotation, context);
    }

    public static bool CanPlaceItem(ItemEntity item, int x, int y, int rotation, InventoryInteractionContext context) {
        return PreviewPlacement(item, x, y, rotation, context, out BackpackPlacementResult result, out _)
            && result.CanPlace;
    }

    public static bool PreviewPlacement(
        ItemEntity item,
        int x,
        int y,
        InventoryInteractionContext context,
        out BackpackPlacementResult result,
        out string reason) {
        int rotation = item?.Grid != null ? item.Grid.Rotation : 0;
        return PreviewPlacement(item, x, y, rotation, context, out result, out reason);
    }

    public static bool PreviewPlacement(
        ItemEntity item,
        int x,
        int y,
        int rotation,
        InventoryInteractionContext context,
        out BackpackPlacementResult result,
        out string reason) {
        result = default;

        if (!ValidateItem(item, out reason)) {
            return false;
        }

        if (!TryGetGrid(context, out BackpackGrid grid, out reason)) {
            return false;
        }

        result = grid.EvaluatePlacement(item, x, y, rotation);
        reason = result.Reason;
        return true;
    }

    public static bool RequestRotateHeldItem(
        ItemEntity item,
        int rotationDelta,
        InventoryInteractionContext context,
        out int newRotation,
        out string reason) {
        newRotation = 0;

        if (!ValidateItem(item, out reason)) {
            return false;
        }

        if (item.Grid == null) {
            reason = $"物品 [{item.Name}] 缺少背包形状。";
            return false;
        }

        if (TryGetGrid(context, out BackpackGrid grid, out _) && grid.ContainedItems.Contains(item)) {
            reason = $"物品 [{item.Name}] 仍在背包中，需先拿起再旋转。";
            return false;
        }

        if (!BackpackGrid.TryResolveNextAllowedRotation(item, rotationDelta, out newRotation, out reason)) {
            return false;
        }

        item.Grid.Rotation = newRotation;
        reason = string.Empty;
        return true;
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
