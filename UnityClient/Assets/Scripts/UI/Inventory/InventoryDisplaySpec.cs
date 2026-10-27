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
    // The combat tray sits under the stage and is never taller than this: up to 5 rows fit (the planned cap is 7×5,
    // docs/specs/2026-10-26-combat-screen-redesign.md), so a bigger chassis draws smaller cells, not a taller tray.
    public const float CombatTrayGridHeight = 370f;
    public const float CombatTrayMaxScale = 0.89f;
    // In a fight the grid is Zero's chassis: a brass frame (ui_combat_chassis_frame, 9-sliced) drawn at 1/2.3 of its
    // pixels. Its opening starts this far in from each edge, in sprite pixels (left, bottom, right, top); the grid sits
    // a few pixels inside that. With the tray at y -302 the frame stands on the chassis panel at the bottom of the screen.
    public const float ChassisFramePixelsPerUnitMultiplier = 2.3f;
    public static readonly Vector4 ChassisFrameInnerMargin = new Vector4(40f, 37f, 42f, 38f);
    public const float ChassisFrameGridInset = 6f;

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

    public static float ResolveCombatScale(Vector2 gridSize) {
        return Mathf.Min(CombatTrayMaxScale, CombatTrayGridHeight / Mathf.Max(1f, gridSize.y));
    }

    // The chassis frame's size on screen and where its centre is from the grid's centre, for a grid drawn at gridOnScreen.
    public static Vector2 ResolveChassisFrameSize(Vector2 gridOnScreen, out Vector2 centreOffset) {
        Vector4 margin = ChassisFrameInnerMargin / ChassisFramePixelsPerUnitMultiplier;
        float left = margin.x + ChassisFrameGridInset;
        float bottom = margin.y + ChassisFrameGridInset;
        float right = margin.z + ChassisFrameGridInset;
        float top = margin.w + ChassisFrameGridInset;
        centreOffset = new Vector2((right - left) * 0.5f, (top - bottom) * 0.5f);
        return gridOnScreen + new Vector2(left + right, top + bottom);
    }

    public static Vector2 ResolveChassisPanelSize(Vector2 gridSize) {
        return gridSize + new Vector2(ChassisPanelPadding, ChassisPanelPadding);
    }

    public static InventoryLayoutProfile ResolveLayoutProfile(InventoryPresentationMode mode) {
        switch (mode) {
            case InventoryPresentationMode.Workshop:
                return new InventoryLayoutProfile(new Vector2(-650f, -320f), 0.62f);
            case InventoryPresentationMode.Combat:
                return new InventoryLayoutProfile(new Vector2(-60f, -302f), CombatTrayMaxScale);
            case InventoryPresentationMode.CombatLoot:
                return new InventoryLayoutProfile(new Vector2(0f, -30f), 1f);
            case InventoryPresentationMode.SafeRoom:
            case InventoryPresentationMode.Stairs:
                return new InventoryLayoutProfile(new Vector2(500f, -60f), 0.78f);
            case InventoryPresentationMode.Loadout:
                return new InventoryLayoutProfile(new Vector2(-420f, 20f), 1f);
            case InventoryPresentationMode.DungeonMap:
                return new InventoryLayoutProfile(new Vector2(0f, -250f), 1f);
            default:
                return new InventoryLayoutProfile(Vector2.zero, 1f);
        }
    }
}
