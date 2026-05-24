using UnityEngine;

public static class DungeonSeedAcceptanceSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Dungeon Seed Acceptance Smoke Test ===");

        TestLayerOneFixedSeedIsStableAndReachable();
        TestLayerTwoFixedSeedIsStableAndReachable();
        TestMissingLayerFailsWithIssue();

        Debug.Log("=== Dungeon Seed Acceptance Smoke Test Finished ===");
    }

    private static void TestLayerOneFixedSeedIsStableAndReachable() {
        CreateCore();
        DungeonSeedAcceptanceReport first = DungeonSeedAcceptanceService.BuildReport(1, 1024, "V-L1-SEED-1024-ROUTE-01");
        DungeonSeedAcceptanceReport second = DungeonSeedAcceptanceService.BuildReport(1, 1024, "V-L1-SEED-1024-ROUTE-01");

        bool stable = first.Summary == second.Summary;
        bool valid = first.IsValid
            && stable
            && first.LayerID == 1
            && first.RequestedRunSeed == 1024
            && first.RowCount > 0
            && first.NodeCount >= first.RowCount
            && first.EdgeCount >= first.NodeCount - 1;

        if (valid) {
            Debug.Log("Layer 1 Fixed Seed Acceptance PASSED.");
        } else {
            Debug.LogError($"Layer 1 Fixed Seed Acceptance FAILED. Valid={first.IsValid}, Stable={stable}, Rows={first.RowCount}, Nodes={first.NodeCount}, Edges={first.EdgeCount}, Issues={string.Join(";", first.Issues)}\nFirst={first.Summary}\nSecond={second.Summary}");
        }
    }

    private static void TestLayerTwoFixedSeedIsStableAndReachable() {
        CreateCore();
        DungeonSeedAcceptanceReport first = DungeonSeedAcceptanceService.BuildReport(2, 2048, "V-L2-DIRECT-2048-ROUTE-01");
        DungeonSeedAcceptanceReport second = DungeonSeedAcceptanceService.BuildReport(2, 2048, "V-L2-DIRECT-2048-ROUTE-01");

        bool stable = first.Summary == second.Summary;
        bool valid = first.IsValid
            && stable
            && first.LayerID == 2
            && first.RequestedRunSeed == 2048
            && first.RowCount > 0
            && first.NodeCount >= first.RowCount
            && first.EdgeCount >= first.NodeCount - 1;

        if (valid) {
            Debug.Log("Layer 2 Fixed Seed Acceptance PASSED.");
        } else {
            Debug.LogError($"Layer 2 Fixed Seed Acceptance FAILED. Valid={first.IsValid}, Stable={stable}, Rows={first.RowCount}, Nodes={first.NodeCount}, Edges={first.EdgeCount}, Issues={string.Join(";", first.Issues)}\nFirst={first.Summary}\nSecond={second.Summary}");
        }
    }

    private static void TestMissingLayerFailsWithIssue() {
        CreateCore();
        DungeonSeedAcceptanceReport report = DungeonSeedAcceptanceService.BuildReport(999, 1, "missing_layer_smoke");

        bool valid = !report.IsValid
            && report.Issues.Count == 1
            && report.Summary.Contains("Layer config missing");

        if (valid) {
            Debug.Log("Missing Layer Seed Acceptance Failure PASSED.");
        } else {
            Debug.LogError($"Missing Layer Seed Acceptance Failure FAILED. Valid={report.IsValid}, Issues={string.Join(";", report.Issues)}, Summary={report.Summary}");
        }
    }

    private static CoreBackend CreateCore() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        return core;
    }
}
