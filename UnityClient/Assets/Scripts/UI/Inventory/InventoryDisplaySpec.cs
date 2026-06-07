using UnityEngine;
using UnityEngine.UI;

public readonly struct InventoryLayoutProfile {
    public InventoryLayoutProfile(Vector2 anchoredPosition, float scale) {
        AnchoredPosition = anchoredPosition;
        Scale = scale;
    }

    public Vector2 AnchoredPosition { get; }
    public float Scale { get; }
}

public static class InventoryDisplaySpec {
    public const float CellSize = 100f;
    public const float CellSpacing = 5f;
    public const float ChassisPanelPadding = 52f;

    public static readonly Vector2 CellSizeVector = new Vector2(CellSize, CellSize);
    public static readonly Vector2 CellSpacingVector = new Vector2(CellSpacing, CellSpacing);

    public static void ApplyGridLayout(GridLayoutGroup layoutGroup) {
        if (layoutGroup == null) {
            return;
        }

        layoutGroup.cellSize = CellSizeVector;
        layoutGroup.spacing = CellSpacingVector;
        layoutGroup.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
    }

    public static Vector2 ResolveGridSize(int width, int height) {
        int safeWidth = Mathf.Max(1, width);
        int safeHeight = Mathf.Max(1, height);

        return new Vector2(
            safeWidth * CellSize + (safeWidth - 1) * CellSpacing,
            safeHeight * CellSize + (safeHeight - 1) * CellSpacing);
    }

    public static Vector2 ResolveItemSize(int columns, int rows) {
        return new Vector2(Mathf.Max(1, columns) * CellSize, Mathf.Max(1, rows) * CellSize);
    }

    public static Vector2 ResolveItemPivot(int columns, int rows) {
        int safeColumns = Mathf.Max(1, columns);
        int safeRows = Mathf.Max(1, rows);
        return new Vector2(0.5f / safeColumns, 1.0f - (0.5f / safeRows));
    }

    public static Vector2Int ResolveDragCellOffset(Vector2 localPoint, ItemEntity item) {
        int offsetX = Mathf.RoundToInt(localPoint.x / CellSize);
        int offsetY = Mathf.RoundToInt(-localPoint.y / CellSize);

        if (item?.Grid != null && BackpackGrid.TryGetRotatedBounds(item, item.Grid.Rotation, out int columns, out int rows)) {
            offsetX = Mathf.Clamp(offsetX, 0, Mathf.Max(0, columns - 1));
            offsetY = Mathf.Clamp(offsetY, 0, Mathf.Max(0, rows - 1));
        }

        return new Vector2Int(offsetX, offsetY);
    }

    public static Vector2 ResolveChassisPanelSize(Vector2 gridSize) {
        return gridSize + new Vector2(ChassisPanelPadding, ChassisPanelPadding);
    }

    public static InventoryLayoutProfile ResolveLayoutProfile(InventoryPresentationMode mode) {
        switch (mode) {
            case InventoryPresentationMode.Workshop:
                return new InventoryLayoutProfile(new Vector2(-120f, -290f), 1f);
            case InventoryPresentationMode.Combat:
                return new InventoryLayoutProfile(new Vector2(0f, -290f), 1f);
            case InventoryPresentationMode.CombatLoot:
                return new InventoryLayoutProfile(new Vector2(-380f, 0f), 1f);
            case InventoryPresentationMode.SafeRoom:
            case InventoryPresentationMode.Stairs:
                return new InventoryLayoutProfile(new Vector2(500f, -150f), 0.78f);
            case InventoryPresentationMode.DungeonMap:
                return new InventoryLayoutProfile(new Vector2(0f, -250f), 1f);
            default:
                return new InventoryLayoutProfile(Vector2.zero, 1f);
        }
    }
}
