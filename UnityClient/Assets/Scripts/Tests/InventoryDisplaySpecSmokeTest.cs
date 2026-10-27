using UnityEngine;
using UnityEngine.UI;

public static class InventoryDisplaySpecSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Inventory Display Spec Smoke Test ===");

        bool gridSizeOk = Approximately(InventoryDisplaySpec.ResolveGridSize(4, 3), new Vector2(415f, 310f));
        bool itemSizeOk = Approximately(InventoryDisplaySpec.ResolveItemSize(2, 3), new Vector2(200f, 300f));
        bool itemPivotOk = Approximately(InventoryDisplaySpec.ResolveItemPivot(2, 3), new Vector2(0.25f, 0.8333333f), 0.0001f);

        InventoryLayoutProfile safeRoomProfile = InventoryDisplaySpec.ResolveLayoutProfile(InventoryPresentationMode.SafeRoom);
        InventoryLayoutProfile combatProfile = InventoryDisplaySpec.ResolveLayoutProfile(InventoryPresentationMode.Combat);
        InventoryLayoutProfile workshopProfile = InventoryDisplaySpec.ResolveLayoutProfile(InventoryPresentationMode.Workshop);
        bool profileOk =
            Approximately(safeRoomProfile.AnchoredPosition, new Vector2(500f, -60f))
            && Approximately(new Vector2(safeRoomProfile.Scale, 0f), new Vector2(0.78f, 0f), 0.0001f)
            && Approximately(combatProfile.AnchoredPosition, new Vector2(-60f, -302f))
            // The satchel around a full-height tray stays on a 1920×1080 screen and under the stage (top below y 560).
            && SatchelOnScreen(combatProfile.AnchoredPosition, InventoryDisplaySpec.CombatTrayGridHeight)
            // The combat tray keeps 5 rows (7×5 cap) inside its height: a 4×4 grid at the full tray scale, 5 rows smaller.
            && Mathf.Abs(InventoryDisplaySpec.ResolveCombatScale(InventoryDisplaySpec.ResolveGridSize(4, 4)) - InventoryDisplaySpec.CombatTrayMaxScale) < 0.0001f
            && InventoryDisplaySpec.ResolveGridSize(7, 5).y * InventoryDisplaySpec.ResolveCombatScale(InventoryDisplaySpec.ResolveGridSize(7, 5)) <= InventoryDisplaySpec.CombatTrayGridHeight + 0.01f
            && workshopProfile.AnchoredPosition.x <= -620f
            && workshopProfile.AnchoredPosition.y <= -300f
            && workshopProfile.Scale <= 0.65f;

        GameObject layoutGo = new GameObject("InventoryDisplaySpecSmokeTest_Layout");
        GridLayoutGroup layoutGroup = layoutGo.AddComponent<GridLayoutGroup>();
        InventoryDisplaySpec.ApplyGridLayout(layoutGroup);
        bool layoutOk = Approximately(layoutGroup.cellSize, InventoryDisplaySpec.CellSizeVector)
            && Approximately(layoutGroup.spacing, InventoryDisplaySpec.CellSpacingVector)
            && layoutGroup.constraint == GridLayoutGroup.Constraint.FixedColumnCount;
        Object.DestroyImmediate(layoutGo);

        if (gridSizeOk && itemSizeOk && itemPivotOk && profileOk && layoutOk) {
            Debug.Log("Inventory Display Spec PASSED.");
        } else {
            Debug.LogError($"Inventory Display Spec FAILED. GridSize={gridSizeOk}, ItemSize={itemSizeOk}, Pivot={itemPivotOk}, Profile={profileOk}, Layout={layoutOk}");
        }

        Debug.Log("=== Inventory Display Spec Smoke Test Finished ===");
    }

    // y measured from the screen's centre, up positive.
    private static bool SatchelOnScreen(Vector2 trayCentre, float gridHeight) {
        Vector2 size = InventoryDisplaySpec.ResolveSatchelSize(new Vector2(gridHeight, gridHeight), out Vector2 offset);
        float centre = trayCentre.y + offset.y;
        float bottom = centre - size.y * 0.5f;
        float top = centre + size.y * 0.5f;
        return bottom >= -540.5f && top <= 540f - 560f;
    }

    private static bool Approximately(Vector2 actual, Vector2 expected, float tolerance = 0.01f) {
        return Mathf.Abs(actual.x - expected.x) <= tolerance && Mathf.Abs(actual.y - expected.y) <= tolerance;
    }
}
