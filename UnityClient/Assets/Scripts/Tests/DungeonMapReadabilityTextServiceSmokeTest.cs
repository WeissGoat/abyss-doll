using System.Linq;
using UnityEngine;

public static class DungeonMapReadabilityTextServiceSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Dungeon Map Readability Text Service Smoke Test ===");

        TestInitialMapSnapshot();
        TestMovementSnapshotUpdatesVisibility();
        TestSeedIssueSnapshot();

        Debug.Log("=== Dungeon Map Readability Text Service Smoke Test Finished ===");
    }

    private static void TestInitialMapSnapshot() {
        CoreBackend core = CreateCore();
        core.Dungeon.LoadLayer(1);
        DungeonLayer layer = core.Dungeon.CurrentLayer;
        DungeonSeedAcceptanceReport seedReport = DungeonSeedAcceptanceService.BuildReport(1, 1024, "V-L1-SEED-1024-ROUTE-01");
        DungeonMapReadabilitySnapshot snapshot = DungeonMapReadabilityTextService.BuildSnapshot(layer, seedReport);

        bool passed = snapshot.Success
            && snapshot.LayerID == 1
            && snapshot.NodeCount > 0
            && snapshot.EdgeCount > 0
            && snapshot.RevealedCount > 0
            && snapshot.PreviewCount > 0
            && snapshot.HiddenCount > 0
            && snapshot.BossCount == 1
            && snapshot.NodeLines.Any(line => line.IsEntry)
            && snapshot.CombinedText.Contains("深渊地图")
            && snapshot.CombinedText.Contains("节点");

        if (passed) {
            Debug.Log("Dungeon Map Readability Initial Snapshot PASSED.");
        } else {
            Debug.LogError($"Dungeon Map Readability Initial Snapshot FAILED. Success={snapshot.Success}, Layer={snapshot.LayerID}, Nodes={snapshot.NodeCount}, Edges={snapshot.EdgeCount}, Revealed={snapshot.RevealedCount}, Preview={snapshot.PreviewCount}, Hidden={snapshot.HiddenCount}, Boss={snapshot.BossCount}, Issues={string.Join(";", snapshot.IssueLines)}, Text={snapshot.CombinedText}");
        }
    }

    private static void TestMovementSnapshotUpdatesVisibility() {
        CoreBackend core = CreateCore();
        core.Dungeon.LoadLayer(1);
        DungeonLayer layer = core.Dungeon.CurrentLayer;
        NodeBase entry = GetNode(layer, 0, 0);
        NodeBase next = entry?.NextNodes != null && entry.NextNodes.Count > 0 ? entry.NextNodes[0] : null;

        if (entry != null) {
            layer.CurrentNode = entry;
            entry.IsVisited = true;
        }

        DungeonMapReadabilitySnapshot snapshot = DungeonMapReadabilityTextService.BuildSnapshot(layer);
        DungeonMapReadabilityNodeLine currentLine = snapshot.NodeLines.FirstOrDefault(line => line.NodeID == entry?.NodeID);
        DungeonMapReadabilityNodeLine nextLine = snapshot.NodeLines.FirstOrDefault(line => line.NodeID == next?.NodeID);
        DungeonMapReadabilityNodeLine futureLine = snapshot.NodeLines.FirstOrDefault(line => line.Row == 2);

        bool passed = snapshot.Success
            && currentLine != null
            && currentLine.IsCurrent
            && nextLine != null
            && nextLine.IsNextCandidate
            && nextLine.Visibility == DungeonNodeVisibilityState.Revealed
            && futureLine != null
            && futureLine.Visibility == DungeonNodeVisibilityState.Preview
            && snapshot.CurrentNodeText.Contains(entry.NodeID);

        if (passed) {
            Debug.Log("Dungeon Map Readability Movement Snapshot PASSED.");
        } else {
            Debug.LogError($"Dungeon Map Readability Movement Snapshot FAILED. Current={currentLine?.StatusText}, Next={nextLine?.StatusText}, Future={futureLine?.StatusText}, CurrentText={snapshot.CurrentNodeText}, Text={snapshot.CombinedText}");
        }
    }

    private static void TestSeedIssueSnapshot() {
        CreateCore();
        DungeonSeedAcceptanceReport missing = DungeonSeedAcceptanceService.BuildReport(999, 1, "missing_layer_smoke");
        DungeonMapReadabilitySnapshot snapshot = DungeonMapReadabilityTextService.BuildSeedSnapshot(missing);

        bool passed = !snapshot.Success
            && snapshot.IssueLines.Any(line => line.Contains("Layer config missing"))
            && snapshot.CombinedText.Contains("验收问题")
            && snapshot.LayerID == 999;

        if (passed) {
            Debug.Log("Dungeon Map Readability Seed Issue Snapshot PASSED.");
        } else {
            Debug.LogError($"Dungeon Map Readability Seed Issue Snapshot FAILED. Success={snapshot.Success}, Layer={snapshot.LayerID}, Issues={string.Join(";", snapshot.IssueLines)}, Text={snapshot.CombinedText}");
        }
    }

    private static CoreBackend CreateCore() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        return core;
    }

    private static NodeBase GetNode(DungeonLayer layer, int rowIndex, int columnIndex) {
        if (layer?.NodeRows == null
            || rowIndex < 0
            || rowIndex >= layer.NodeRows.Count
            || layer.NodeRows[rowIndex] == null
            || columnIndex < 0
            || columnIndex >= layer.NodeRows[rowIndex].Count) {
            return null;
        }

        return layer.NodeRows[rowIndex][columnIndex];
    }
}
