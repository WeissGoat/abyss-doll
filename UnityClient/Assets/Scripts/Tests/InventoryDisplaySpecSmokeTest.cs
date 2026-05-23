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
        bool profileOk =
            Approximately(safeRoomProfile.AnchoredPosition, new Vector2(500f, -150f))
            && Approximately(new Vector2(safeRoomProfile.Scale, 0f), new Vector2(0.78f, 0f), 0.0001f)
            && Approximately(combatProfile.AnchoredPosition, new Vector2(620f, -240f))
            && Approximately(new Vector2(combatProfile.Scale, 0f), new Vector2(1f, 0f), 0.0001f);

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

    private static bool Approximately(Vector2 actual, Vector2 expected, float tolerance = 0.01f) {
        return Mathf.Abs(actual.x - expected.x) <= tolerance && Mathf.Abs(actual.y - expected.y) <= tolerance;
    }
}
