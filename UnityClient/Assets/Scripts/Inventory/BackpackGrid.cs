using System;
using System.Collections.Generic;

public enum BackpackPlacementFailure {
    None,
    MissingItem,
    MissingShape,
    RotationNotAllowed,
    OutOfBounds,
    LockedCell,
    OccupiedCell
}

public struct BackpackPlacementResult {
    public bool CanPlace;
    public BackpackPlacementFailure Failure;
    public string Reason;
    public int X;
    public int Y;
    public int Rotation;
    public List<int[]> OccupiedCells;

    public static BackpackPlacementResult Fail(BackpackPlacementFailure failure, string reason, int x, int y, int rotation, List<int[]> occupiedCells = null) {
        return new BackpackPlacementResult {
            CanPlace = false,
            Failure = failure,
            Reason = reason,
            X = x,
            Y = y,
            Rotation = rotation,
            OccupiedCells = occupiedCells ?? new List<int[]>()
        };
    }

    public static BackpackPlacementResult Success(int x, int y, int rotation, List<int[]> occupiedCells) {
        return new BackpackPlacementResult {
            CanPlace = true,
            Failure = BackpackPlacementFailure.None,
            Reason = string.Empty,
            X = x,
            Y = y,
            Rotation = rotation,
            OccupiedCells = occupiedCells ?? new List<int[]>()
        };
    }
}

public class BackpackGrid {
    public const int RotationStep = 90;
    public const int FullRotation = 360;
    private const string LockedCellMarker = "LOCKED_CELL";

    public int Width { get; private set; }
    public int Height { get; private set; }
    
    // The core matrix storing the InstanceID of the item occupying the cell
    private string[,] _gridMatrix;
    
    public List<ItemEntity> ContainedItems { get; private set; }
    
    public BackpackGrid(ChassisComponent chassis) {
        Width = chassis.GridWidth;
        Height = chassis.GridHeight;
        _gridMatrix = new string[Width, Height];
        ContainedItems = new List<ItemEntity>();
        
        // Initialize locked cells based on the chassis mask
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                if (chassis.GridMask != null && chassis.GridMask.Length > x && chassis.GridMask[x].Length > y) {
                    if (!chassis.GridMask[x][y]) {
                        _gridMatrix[x, y] = LockedCellMarker;
                    }
                }
            }
        }
    }

    public static int NormalizeRotation(int rotation) {
        int normalized = rotation % FullRotation;
        if (normalized < 0) {
            normalized += FullRotation;
        }

        return (normalized / RotationStep) * RotationStep;
    }

    public static int GetAllowedRotationSteps(ItemEntity item) {
        if (item?.Grid == null || !item.Grid.CanRotate) {
            return 1;
        }

        if (item.Grid.RotationSteps == 2 || item.Grid.RotationSteps == 4) {
            return item.Grid.RotationSteps;
        }

        return 1;
    }

    public static bool CanRotateItem(ItemEntity item) {
        return GetAllowedRotationSteps(item) > 1;
    }

    public static bool IsRotationAllowed(ItemEntity item, int rotation) {
        int normalizedRotation = NormalizeRotation(rotation);
        int allowedSteps = GetAllowedRotationSteps(item);
        if (allowedSteps <= 1) {
            return normalizedRotation == 0;
        }

        return normalizedRotation >= 0 && normalizedRotation < allowedSteps * RotationStep;
    }

    public static bool TryResolveNextAllowedRotation(ItemEntity item, int rotationDelta, out int newRotation, out string reason) {
        newRotation = item?.Grid != null ? NormalizeRotation(item.Grid.Rotation) : 0;

        if (item?.Grid == null) {
            reason = "物品缺少背包形状，无法旋转。";
            return false;
        }

        int allowedSteps = GetAllowedRotationSteps(item);
        if (allowedSteps <= 1) {
            reason = $"物品 [{item.Name}] 配置为不可旋转。";
            return false;
        }

        int deltaSteps = rotationDelta / RotationStep;
        if (deltaSteps == 0 && rotationDelta != 0) {
            deltaSteps = rotationDelta > 0 ? 1 : -1;
        }

        int currentIndex = NormalizeRotation(item.Grid.Rotation) / RotationStep;
        int nextIndex = (currentIndex + deltaSteps) % allowedSteps;
        if (nextIndex < 0) {
            nextIndex += allowedSteps;
        }

        newRotation = nextIndex * RotationStep;
        reason = string.Empty;
        return true;
    }

    public static TargetDirection RotateDirection(TargetDirection direction, int rotation) {
        switch (direction) {
            case TargetDirection.Right:
            case TargetDirection.Left:
            case TargetDirection.Up:
            case TargetDirection.Down:
                break;
            default:
                return direction;
        }

        int x = 0;
        int y = 0;
        switch (direction) {
            case TargetDirection.Right:
                x = 1;
                break;
            case TargetDirection.Left:
                x = -1;
                break;
            case TargetDirection.Up:
                y = -1;
                break;
            case TargetDirection.Down:
                y = 1;
                break;
        }

        switch (NormalizeRotation(rotation)) {
            case 90:
                int rotated90X = -y;
                int rotated90Y = x;
                x = rotated90X;
                y = rotated90Y;
                break;
            case 180:
                x = -x;
                y = -y;
                break;
            case 270:
                int rotated270X = y;
                int rotated270Y = -x;
                x = rotated270X;
                y = rotated270Y;
                break;
        }

        if (x > 0) {
            return TargetDirection.Right;
        }
        if (x < 0) {
            return TargetDirection.Left;
        }
        if (y < 0) {
            return TargetDirection.Up;
        }
        return TargetDirection.Down;
    }

    public static List<int[]> GetNormalizedShapeCells(ItemEntity item, int rotation) {
        List<int[]> cells = new List<int[]>();
        if (item?.Grid == null || item.Grid.Shape == null || item.Grid.Shape.Length == 0) {
            return cells;
        }

        int normalizedRotation = NormalizeRotation(rotation);
        int minX = int.MaxValue;
        int minY = int.MaxValue;

        foreach (var point in item.Grid.Shape) {
            if (point == null || point.Length < 2) {
                continue;
            }

            int px = point[0];
            int py = point[1];
            int rotatedX = px;
            int rotatedY = py;

            switch (normalizedRotation) {
                case 90:
                    rotatedX = -py;
                    rotatedY = px;
                    break;
                case 180:
                    rotatedX = -px;
                    rotatedY = -py;
                    break;
                case 270:
                    rotatedX = py;
                    rotatedY = -px;
                    break;
            }

            cells.Add(new int[] { rotatedX, rotatedY });
            if (rotatedX < minX) {
                minX = rotatedX;
            }
            if (rotatedY < minY) {
                minY = rotatedY;
            }
        }

        if (cells.Count == 0) {
            return cells;
        }

        for (int i = 0; i < cells.Count; i++) {
            cells[i][0] -= minX;
            cells[i][1] -= minY;
        }

        return cells;
    }

    public static bool TryGetRotatedBounds(ItemEntity item, int rotation, out int width, out int height) {
        width = 0;
        height = 0;
        List<int[]> cells = GetNormalizedShapeCells(item, rotation);
        if (cells.Count == 0) {
            return false;
        }

        foreach (int[] cell in cells) {
            if (cell[0] + 1 > width) {
                width = cell[0] + 1;
            }
            if (cell[1] + 1 > height) {
                height = cell[1] + 1;
            }
        }

        return width > 0 && height > 0;
    }

    // Calculates the actual occupied cells based on position, shape, and rotation
    public List<int[]> GetOccupiedCells(ItemEntity item, int targetX, int targetY) {
        int rotation = item?.Grid?.Rotation ?? 0;
        return GetOccupiedCells(item, targetX, targetY, rotation);
    }

    public List<int[]> GetOccupiedCells(ItemEntity item, int targetX, int targetY, int rotation) {
        List<int[]> cells = new List<int[]>();

        foreach (int[] cell in GetNormalizedShapeCells(item, rotation)) {
            cells.Add(new int[] { targetX + cell[0], targetY + cell[1] });
        }

        return cells;
    }

    public BackpackPlacementResult EvaluatePlacement(ItemEntity item, int targetX, int targetY) {
        int rotation = item?.Grid?.Rotation ?? 0;
        return EvaluatePlacement(item, targetX, targetY, rotation);
    }

    public BackpackPlacementResult EvaluatePlacement(ItemEntity item, int targetX, int targetY, int rotation) {
        int normalizedRotation = NormalizeRotation(rotation);
        if (item == null) {
            return BackpackPlacementResult.Fail(BackpackPlacementFailure.MissingItem, "物品数据不存在。", targetX, targetY, normalizedRotation);
        }

        if (item.Grid == null || item.Grid.Shape == null || item.Grid.Shape.Length == 0) {
            return BackpackPlacementResult.Fail(BackpackPlacementFailure.MissingShape, $"物品 [{item.Name}] 缺少背包形状。", targetX, targetY, normalizedRotation);
        }

        if (!IsRotationAllowed(item, normalizedRotation)) {
            return BackpackPlacementResult.Fail(
                BackpackPlacementFailure.RotationNotAllowed,
                $"物品 [{item.Name}] 不允许使用 {normalizedRotation} 度朝向。",
                targetX,
                targetY,
                normalizedRotation);
        }

        List<int[]> occupiedCells = GetOccupiedCells(item, targetX, targetY, normalizedRotation);
        if (occupiedCells.Count == 0) {
            return BackpackPlacementResult.Fail(BackpackPlacementFailure.MissingShape, $"物品 [{item.Name}] 没有有效占格。", targetX, targetY, normalizedRotation);
        }

        foreach (var cell in occupiedCells) {
            int x = cell[0];
            int y = cell[1];

            if (x < 0 || x >= Width || y < 0 || y >= Height) {
                return BackpackPlacementResult.Fail(
                    BackpackPlacementFailure.OutOfBounds,
                    $"物品 [{item.Name}] 超出背包边界 ({x},{y})。",
                    targetX,
                    targetY,
                    normalizedRotation,
                    occupiedCells);
            }

            string occupant = _gridMatrix[x, y];
            if (occupant == LockedCellMarker) {
                return BackpackPlacementResult.Fail(
                    BackpackPlacementFailure.LockedCell,
                    $"物品 [{item.Name}] 覆盖了不可用格 ({x},{y})。",
                    targetX,
                    targetY,
                    normalizedRotation,
                    occupiedCells);
            }

            if (!string.IsNullOrEmpty(occupant) && occupant != item.InstanceID) {
                return BackpackPlacementResult.Fail(
                    BackpackPlacementFailure.OccupiedCell,
                    $"物品 [{item.Name}] 与已有物品冲突 ({x},{y})。",
                    targetX,
                    targetY,
                    normalizedRotation,
                    occupiedCells);
            }
        }

        return BackpackPlacementResult.Success(targetX, targetY, normalizedRotation, occupiedCells);
    }

    public bool CanPlaceItem(ItemEntity item, int targetX, int targetY) {
        return EvaluatePlacement(item, targetX, targetY).CanPlace;
    }

    public bool CanPlaceItem(ItemEntity item, int targetX, int targetY, int rotation) {
        return EvaluatePlacement(item, targetX, targetY, rotation).CanPlace;
    }

    public bool PlaceItem(ItemEntity item, int targetX, int targetY) {
        int rotation = item?.Grid?.Rotation ?? 0;
        return PlaceItem(item, targetX, targetY, rotation);
    }

    public bool PlaceItem(ItemEntity item, int targetX, int targetY, int rotation) {
        BackpackPlacementResult placement = EvaluatePlacement(item, targetX, targetY, rotation);
        if (!placement.CanPlace) {
            return false;
        }
        
        // Remove item from previous position if it was already in the grid
        if (ContainedItems.Contains(item)) {
            RemoveItem(item);
        }
        
        foreach (var cell in placement.OccupiedCells) {
            _gridMatrix[cell[0], cell[1]] = item.InstanceID;
        }
        
        item.Grid.CurrentPos = new int[] { targetX, targetY };
        item.Grid.Rotation = placement.Rotation;
        ContainedItems.Add(item);
        
        return true;
    }

    public bool TryFindFirstAvailable(ItemEntity item, out int targetX, out int targetY) {
        targetX = -1;
        targetY = -1;

        if (item == null) {
            return false;
        }

        for (int y = 0; y < Height; y++) {
            for (int x = 0; x < Width; x++) {
                if (CanPlaceItem(item, x, y)) {
                    targetX = x;
                    targetY = y;
                    return true;
                }
            }
        }

        return false;
    }

    public bool TryPlaceFirstAvailable(ItemEntity item, out int targetX, out int targetY) {
        if (!TryFindFirstAvailable(item, out targetX, out targetY)) {
            return false;
        }

        return PlaceItem(item, targetX, targetY);
    }

    public void RemoveItem(ItemEntity item) {
        if (!ContainedItems.Contains(item)) return;
        
        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                if (_gridMatrix[x, y] == item.InstanceID) {
                    _gridMatrix[x, y] = null;
                }
            }
        }
        ContainedItems.Remove(item);
    }

    public List<ItemEntity> ClearAllItems() {
        List<ItemEntity> removedItems = new List<ItemEntity>(ContainedItems);

        for (int x = 0; x < Width; x++) {
            for (int y = 0; y < Height; y++) {
                if (_gridMatrix[x, y] != LockedCellMarker) {
                    _gridMatrix[x, y] = null;
                }
            }
        }

        ContainedItems.Clear();
        return removedItems;
    }
    
    public ItemEntity GetItemAt(int x, int y) {
        if (x < 0 || x >= Width || y < 0 || y >= Height) return null;
        string id = _gridMatrix[x, y];
        if (string.IsNullOrEmpty(id) || id == LockedCellMarker) return null;
        return ContainedItems.Find(i => i.InstanceID == id);
    }
    
    public void DebugPrintGrid() {
        string output = "Backpack Grid:\n";
        for (int y = 0; y < Height; y++) {
            for (int x = 0; x < Width; x++) {
                string cell = _gridMatrix[x, y];
                if (cell == null) output += "[ ] ";
                else if (cell == LockedCellMarker) output += "[X] ";
                else output += $"[*] "; // Simplified for visual
            }
            output += "\n";
        }
        UnityEngine.Debug.Log(output);
    }
}
