using System.Collections.Generic;
using System.Text;

public static class DungeonSeedAcceptanceService {
    public static DungeonSeedAcceptanceReport BuildReport(int layerID, int runSeed, string seedID = null) {
        if (!ConfigManager.Dungeons.TryGetValue(layerID, out DungeonConfig config)) {
            DungeonSeedAcceptanceReport missingReport = new DungeonSeedAcceptanceReport {
                SeedID = seedID,
                LayerID = layerID,
                RequestedRunSeed = runSeed
            };
            missingReport.Issues.Add($"Layer config missing: {layerID}");
            missingReport.Summary = BuildSummary(missingReport, null);
            return missingReport;
        }

        return BuildReport(config, runSeed, seedID);
    }

    public static DungeonSeedAcceptanceReport BuildReport(DungeonConfig config, int runSeed, string seedID = null) {
        DungeonSeedAcceptanceReport report = new DungeonSeedAcceptanceReport {
            SeedID = seedID,
            LayerID = config?.LayerID ?? 0,
            RequestedRunSeed = runSeed,
            MapProfileID = config?.MapProfileID
        };

        if (config == null) {
            report.Issues.Add("Dungeon config is null.");
            report.Summary = BuildSummary(report, null);
            return report;
        }

        NodeFactory.Initialize();
        DungeonLayer layer = new DungeonLayer();
        layer.GenerateMapTree(config, runSeed);

        report.ResolvedRunSeed = layer.RunSeed;
        report.MapProfileID = layer.MapProfileID;
        report.RowCount = layer.NodeRows?.Count ?? 0;
        report.EntryNodeCount = layer.EntryNodes?.Count ?? 0;
        report.NodeCount = CountNodes(layer);
        report.EdgeCount = CountEdges(layer);

        NodeBase bossNode = FindBossNode(layer);
        NodeBase stairsNode = FindStairsNode(layer);
        report.HasBossNode = bossNode != null;
        report.HasStairsNode = stairsNode != null;
        report.BossReachableFromAllEntries = bossNode != null && AllEntriesReach(layer, bossNode);
        report.StairsReachableFromAllEntries = stairsNode != null && AllEntriesReach(layer, stairsNode);

        ValidateReport(report);
        report.Summary = BuildSummary(report, layer);
        return report;
    }

    private static void ValidateReport(DungeonSeedAcceptanceReport report) {
        if (report.RowCount <= 0) {
            report.Issues.Add("Generated map has no rows.");
        }

        if (report.EntryNodeCount <= 0) {
            report.Issues.Add("Generated map has no entry nodes.");
        }

        if (!report.HasBossNode) {
            report.Issues.Add("Generated map has no boss node.");
        }

        if (!report.HasStairsNode) {
            report.Issues.Add("Generated map has no stairs node.");
        }

        if (report.HasBossNode && !report.BossReachableFromAllEntries) {
            report.Issues.Add("Boss node is not reachable from every entry node.");
        }

        if (report.HasStairsNode && !report.StairsReachableFromAllEntries) {
            report.Issues.Add("Stairs node is not reachable from every entry node.");
        }
    }

    private static string BuildSummary(DungeonSeedAcceptanceReport report, DungeonLayer layer) {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine($"SeedID={report.SeedID ?? "unnamed"}");
        builder.AppendLine($"Layer={report.LayerID};RequestedRunSeed={report.RequestedRunSeed};ResolvedRunSeed={report.ResolvedRunSeed};Profile={report.MapProfileID ?? string.Empty}");
        builder.AppendLine($"Rows={report.RowCount};Entries={report.EntryNodeCount};Nodes={report.NodeCount};Edges={report.EdgeCount};Boss={report.HasBossNode};Stairs={report.HasStairsNode};BossReachable={report.BossReachableFromAllEntries};StairsReachable={report.StairsReachableFromAllEntries}");

        if (layer?.NodeRows != null) {
            for (int rowIndex = 0; rowIndex < layer.NodeRows.Count; rowIndex++) {
                List<NodeBase> row = layer.NodeRows[rowIndex];
                builder.Append($"R{rowIndex}:");
                if (row != null) {
                    for (int columnIndex = 0; columnIndex < row.Count; columnIndex++) {
                        if (columnIndex > 0) {
                            builder.Append("|");
                        }

                        builder.Append(BuildNodeSummary(row[columnIndex]));
                    }
                }
                builder.AppendLine();
            }
        }

        if (report.Issues.Count > 0) {
            builder.Append("Issues=");
            builder.AppendLine(string.Join(";", report.Issues));
        }

        return builder.ToString().TrimEnd();
    }

    private static string BuildNodeSummary(NodeBase node) {
        if (node == null) {
            return "null";
        }

        StringBuilder builder = new StringBuilder();
        builder.Append($"C{node.MapColumn}:{node.GetType().Name}:{node.NodeID}");
        builder.Append($":Theme={node.RouteTheme ?? string.Empty}");
        builder.Append($":Risk={node.RiskLevel ?? string.Empty}");
        builder.Append($":Reward={node.RewardID ?? string.Empty}");

        if (node is CombatNode combatNode && combatNode.MonsterIDs != null && combatNode.MonsterIDs.Count > 0) {
            builder.Append(":Monsters=");
            builder.Append(string.Join(",", combatNode.MonsterIDs));
        }

        builder.Append(":Next=");
        builder.Append(BuildNextSummary(node));
        return builder.ToString();
    }

    private static string BuildNextSummary(NodeBase node) {
        if (node?.NextNodes == null || node.NextNodes.Count == 0) {
            return "-";
        }

        List<string> nextIDs = new List<string>();
        foreach (NodeBase next in node.NextNodes) {
            if (next == null) {
                continue;
            }

            nextIDs.Add($"R{next.MapRow}C{next.MapColumn}");
        }

        nextIDs.Sort();
        return string.Join(",", nextIDs);
    }

    private static int CountNodes(DungeonLayer layer) {
        int count = 0;
        if (layer?.NodeRows == null) {
            return 0;
        }

        foreach (List<NodeBase> row in layer.NodeRows) {
            count += row?.Count ?? 0;
        }

        return count;
    }

    private static int CountEdges(DungeonLayer layer) {
        int count = 0;
        if (layer?.NodeRows == null) {
            return 0;
        }

        foreach (List<NodeBase> row in layer.NodeRows) {
            if (row == null) {
                continue;
            }

            foreach (NodeBase node in row) {
                count += node?.NextNodes?.Count ?? 0;
            }
        }

        return count;
    }

    private static NodeBase FindBossNode(DungeonLayer layer) {
        if (layer?.NodeRows == null) {
            return null;
        }

        foreach (List<NodeBase> row in layer.NodeRows) {
            if (row == null) {
                continue;
            }

            foreach (NodeBase node in row) {
                if (node is CombatNode && !string.IsNullOrEmpty(node.NodeID) && node.NodeID.Contains("boss")) {
                    return node;
                }
            }
        }

        return null;
    }

    private static NodeBase FindStairsNode(DungeonLayer layer) {
        if (layer?.NodeRows == null) {
            return null;
        }

        foreach (List<NodeBase> row in layer.NodeRows) {
            if (row == null) {
                continue;
            }

            foreach (NodeBase node in row) {
                if (node is StairsNode) {
                    return node;
                }
            }
        }

        return null;
    }

    private static bool AllEntriesReach(DungeonLayer layer, NodeBase target) {
        if (layer?.EntryNodes == null || layer.EntryNodes.Count == 0 || target == null) {
            return false;
        }

        foreach (NodeBase entry in layer.EntryNodes) {
            if (!CanReach(entry, target)) {
                return false;
            }
        }

        return true;
    }

    private static bool CanReach(NodeBase start, NodeBase target) {
        if (start == null || target == null) {
            return false;
        }

        HashSet<NodeBase> visited = new HashSet<NodeBase>();
        Queue<NodeBase> queue = new Queue<NodeBase>();
        queue.Enqueue(start);
        visited.Add(start);

        while (queue.Count > 0) {
            NodeBase current = queue.Dequeue();
            if (current == target) {
                return true;
            }

            if (current.NextNodes == null) {
                continue;
            }

            foreach (NodeBase next in current.NextNodes) {
                if (next != null && visited.Add(next)) {
                    queue.Enqueue(next);
                }
            }
        }

        return false;
    }
}

public sealed class DungeonSeedAcceptanceReport {
    public string SeedID;
    public int LayerID;
    public int RequestedRunSeed;
    public int ResolvedRunSeed;
    public string MapProfileID;
    public int RowCount;
    public int EntryNodeCount;
    public int NodeCount;
    public int EdgeCount;
    public bool HasBossNode;
    public bool HasStairsNode;
    public bool BossReachableFromAllEntries;
    public bool StairsReachableFromAllEntries;
    public readonly List<string> Issues = new List<string>();
    public string Summary;

    public bool IsValid => Issues.Count == 0
        && RowCount > 0
        && EntryNodeCount > 0
        && HasBossNode
        && HasStairsNode
        && BossReachableFromAllEntries
        && StairsReachableFromAllEntries;
}
