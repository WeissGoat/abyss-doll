using System.Collections.Generic;
using System.Linq;
using System.Text;

public class DungeonMapReadabilitySnapshot {
    public bool Success;
    public string Reason;
    public string Header;
    public int LayerID;
    public int RunSeed;
    public string MapProfileID;
    public string LayerText;
    public string SeedText;
    public string NetworkText;
    public string VisibilityText;
    public string CurrentNodeText;
    public string CombinedText;
    public int RowCount;
    public int EntryNodeCount;
    public int NodeCount;
    public int EdgeCount;
    public int RevealedCount;
    public int PreviewCount;
    public int HiddenCount;
    public int HighRiskCount;
    public int BossCount;
    public List<string> IssueLines = new List<string>();
    public List<DungeonMapReadabilityNodeLine> NodeLines = new List<DungeonMapReadabilityNodeLine>();
    public List<DungeonMapReadabilityRouteLine> RouteLines = new List<DungeonMapReadabilityRouteLine>();
}

public class DungeonMapReadabilityNodeLine {
    public string NodeID;
    public string NodeType;
    public int Row;
    public int Column;
    public DungeonNodeVisibilityState Visibility;
    public DungeonNodeRiskLevel RiskLevel;
    public string VisibilityText;
    public string RiskText;
    public string RiskHint;
    public string RouteTheme;
    public string RewardID;
    public bool IsCurrent;
    public bool IsVisited;
    public bool IsEntry;
    public bool IsBoss;
    public bool IsNextCandidate;
    public string CoordinateText;
    public string NextCoordinateText;
    public string StatusText;
    public string DetailText;
}

public class DungeonMapReadabilityRouteLine {
    public string FromNodeID;
    public string ToNodeID;
    public string FromCoordinateText;
    public string ToCoordinateText;
    public bool IsVisible;
    public string StatusText;
}

public static class DungeonMapReadabilityTextService {
    public static DungeonMapReadabilitySnapshot BuildSnapshot(
        DungeonLayer layer,
        DungeonSeedAcceptanceReport seedReport = null,
        int nodeLimit = 64,
        int routeLimit = 96) {
        DungeonMapReadabilitySnapshot snapshot = new DungeonMapReadabilitySnapshot {
            Header = "深渊地图"
        };

        if (layer == null) {
            snapshot.Success = false;
            snapshot.Reason = "Dungeon layer is missing.";
            snapshot.LayerText = "地图数据不可用";
            snapshot.CombinedText = BuildCombinedText(snapshot);
            return snapshot;
        }

        snapshot.Success = true;
        snapshot.LayerID = layer.LayerID;
        snapshot.RunSeed = layer.RunSeed;
        snapshot.MapProfileID = layer.MapProfileID;
        snapshot.RowCount = layer.NodeRows?.Count ?? 0;
        snapshot.EntryNodeCount = layer.EntryNodes?.Count ?? 0;
        snapshot.NodeCount = CountNodes(layer);
        snapshot.EdgeCount = CountEdges(layer);
        snapshot.LayerText = $"第 {layer.LayerID} 层 / Profile {layer.MapProfileID ?? "unknown"}";
        snapshot.SeedText = $"Seed {layer.RunSeed}";
        snapshot.NetworkText = $"行 {snapshot.RowCount} / 入口 {snapshot.EntryNodeCount} / 节点 {snapshot.NodeCount} / 路线 {snapshot.EdgeCount}";
        snapshot.CurrentNodeText = BuildCurrentNodeText(layer);

        AppendSeedReport(snapshot, seedReport);
        AppendNodeLines(snapshot, layer, nodeLimit);
        AppendRouteLines(snapshot, layer, routeLimit);
        snapshot.VisibilityText = $"已揭示 {snapshot.RevealedCount} / 预览 {snapshot.PreviewCount} / 隐藏 {snapshot.HiddenCount}";
        snapshot.CombinedText = BuildCombinedText(snapshot);
        return snapshot;
    }

    public static DungeonMapReadabilitySnapshot BuildSeedSnapshot(DungeonSeedAcceptanceReport seedReport) {
        DungeonMapReadabilitySnapshot snapshot = new DungeonMapReadabilitySnapshot {
            Header = "深渊地图 Seed 验收"
        };

        if (seedReport == null) {
            snapshot.Success = false;
            snapshot.Reason = "Dungeon seed acceptance report is missing.";
            snapshot.LayerText = "Seed 验收数据不可用";
            snapshot.CombinedText = BuildCombinedText(snapshot);
            return snapshot;
        }

        snapshot.Success = seedReport.IsValid;
        snapshot.Reason = seedReport.IsValid ? string.Empty : "Seed acceptance has issues.";
        snapshot.LayerID = seedReport.LayerID;
        snapshot.RunSeed = seedReport.ResolvedRunSeed;
        snapshot.MapProfileID = seedReport.MapProfileID;
        snapshot.RowCount = seedReport.RowCount;
        snapshot.EntryNodeCount = seedReport.EntryNodeCount;
        snapshot.NodeCount = seedReport.NodeCount;
        snapshot.EdgeCount = seedReport.EdgeCount;
        snapshot.LayerText = $"第 {seedReport.LayerID} 层 / Profile {seedReport.MapProfileID ?? "unknown"}";
        snapshot.SeedText = $"SeedID {seedReport.SeedID ?? "unnamed"} / 请求 {seedReport.RequestedRunSeed} / 实际 {seedReport.ResolvedRunSeed}";
        snapshot.NetworkText = $"行 {seedReport.RowCount} / 入口 {seedReport.EntryNodeCount} / 节点 {seedReport.NodeCount} / 路线 {seedReport.EdgeCount}";
        AppendSeedReport(snapshot, seedReport);
        snapshot.CombinedText = BuildCombinedText(snapshot);
        return snapshot;
    }

    private static void AppendSeedReport(DungeonMapReadabilitySnapshot snapshot, DungeonSeedAcceptanceReport seedReport) {
        if (snapshot == null || seedReport == null) {
            return;
        }

        if (!seedReport.IsValid) {
            snapshot.IssueLines.AddRange(seedReport.Issues.Where(issue => !string.IsNullOrEmpty(issue)));
        }

        if (!seedReport.HasBossNode) {
            snapshot.IssueLines.Add("Boss node missing.");
        }

        if (!seedReport.HasStairsNode) {
            snapshot.IssueLines.Add("Stairs node missing.");
        }

        if (seedReport.HasBossNode && !seedReport.BossReachableFromAllEntries) {
            snapshot.IssueLines.Add("Boss is not reachable from all entry nodes.");
        }

        if (seedReport.HasStairsNode && !seedReport.StairsReachableFromAllEntries) {
            snapshot.IssueLines.Add("Stairs is not reachable from all entry nodes.");
        }

        snapshot.IssueLines = snapshot.IssueLines
            .Where(issue => !string.IsNullOrEmpty(issue))
            .Distinct()
            .ToList();
    }

    private static void AppendNodeLines(DungeonMapReadabilitySnapshot snapshot, DungeonLayer layer, int nodeLimit) {
        if (snapshot == null || layer?.NodeRows == null) {
            return;
        }

        int safeLimit = ResolveLimit(nodeLimit, snapshot.NodeCount);
        foreach (List<NodeBase> row in layer.NodeRows) {
            if (row == null) {
                continue;
            }

            foreach (NodeBase node in row) {
                if (node == null) {
                    continue;
                }

                DungeonMapReadabilityNodeLine line = BuildNodeLine(layer, node);
                CountNode(snapshot, line);
                if (snapshot.NodeLines.Count < safeLimit) {
                    snapshot.NodeLines.Add(line);
                }
            }
        }
    }

    private static DungeonMapReadabilityNodeLine BuildNodeLine(DungeonLayer layer, NodeBase node) {
        DungeonMapNodePresentation presentation = DungeonMapVisibilityService.BuildNodePresentation(layer, node);
        bool isEntry = layer?.EntryNodes != null && layer.EntryNodes.Contains(node);
        bool isNextCandidate = layer?.CurrentNode?.NextNodes != null && layer.CurrentNode.NextNodes.Contains(node);
        DungeonMapReadabilityNodeLine line = new DungeonMapReadabilityNodeLine {
            NodeID = node.NodeID,
            NodeType = node.GetType().Name,
            Row = node.MapRow,
            Column = node.MapColumn,
            Visibility = presentation.Visibility,
            RiskLevel = presentation.RiskLevel,
            VisibilityText = FormatVisibility(presentation.Visibility),
            RiskText = presentation.RiskLabel,
            RiskHint = presentation.RiskHint,
            RouteTheme = node.RouteTheme,
            RewardID = node.RewardID,
            IsCurrent = layer?.CurrentNode == node,
            IsVisited = node.IsVisited,
            IsEntry = isEntry,
            IsBoss = IsBossNode(node),
            IsNextCandidate = isNextCandidate,
            CoordinateText = BuildCoordinateText(node),
            NextCoordinateText = BuildNextCoordinateText(node),
        };
        line.StatusText = BuildNodeStatusText(line);
        line.DetailText = BuildNodeDetailText(line);
        return line;
    }

    private static void CountNode(DungeonMapReadabilitySnapshot snapshot, DungeonMapReadabilityNodeLine line) {
        if (snapshot == null || line == null) {
            return;
        }

        switch (line.Visibility) {
            case DungeonNodeVisibilityState.Revealed:
                snapshot.RevealedCount++;
                break;
            case DungeonNodeVisibilityState.Preview:
                snapshot.PreviewCount++;
                break;
            default:
                snapshot.HiddenCount++;
                break;
        }

        if (line.RiskLevel == DungeonNodeRiskLevel.High || line.RiskLevel == DungeonNodeRiskLevel.Boss) {
            snapshot.HighRiskCount++;
        }

        if (line.IsBoss) {
            snapshot.BossCount++;
        }
    }

    private static void AppendRouteLines(DungeonMapReadabilitySnapshot snapshot, DungeonLayer layer, int routeLimit) {
        if (snapshot == null || layer?.NodeRows == null) {
            return;
        }

        int safeLimit = ResolveLimit(routeLimit, snapshot.EdgeCount);
        foreach (List<NodeBase> row in layer.NodeRows) {
            if (row == null) {
                continue;
            }

            foreach (NodeBase node in row) {
                if (node?.NextNodes == null) {
                    continue;
                }

                foreach (NodeBase next in node.NextNodes) {
                    if (next == null || snapshot.RouteLines.Count >= safeLimit) {
                        continue;
                    }

                    bool isVisible = DungeonMapVisibilityService.ShouldRenderRouteLine(layer, node, next);
                    snapshot.RouteLines.Add(new DungeonMapReadabilityRouteLine {
                        FromNodeID = node.NodeID,
                        ToNodeID = next.NodeID,
                        FromCoordinateText = BuildCoordinateText(node),
                        ToCoordinateText = BuildCoordinateText(next),
                        IsVisible = isVisible,
                        StatusText = isVisible ? "可见路线" : "迷雾遮蔽"
                    });
                }
            }
        }
    }

    private static string BuildCombinedText(DungeonMapReadabilitySnapshot snapshot) {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(snapshot.Header ?? "深渊地图");
        AppendLineIfNotEmpty(builder, snapshot.LayerText);
        AppendLineIfNotEmpty(builder, snapshot.SeedText);
        AppendLineIfNotEmpty(builder, snapshot.NetworkText);
        AppendLineIfNotEmpty(builder, snapshot.VisibilityText);
        AppendLineIfNotEmpty(builder, snapshot.CurrentNodeText);

        if (snapshot.IssueLines.Count > 0) {
            builder.AppendLine("验收问题:");
            foreach (string issue in snapshot.IssueLines) {
                builder.AppendLine($"- {issue}");
            }
        }

        if (snapshot.NodeLines.Count > 0) {
            builder.AppendLine("节点:");
            foreach (DungeonMapReadabilityNodeLine line in snapshot.NodeLines) {
                builder.AppendLine($"- {line.CoordinateText} {line.StatusText} | {line.DetailText}");
            }
        }

        if (snapshot.RouteLines.Count > 0) {
            builder.AppendLine("路线:");
            foreach (DungeonMapReadabilityRouteLine line in snapshot.RouteLines) {
                builder.AppendLine($"- {line.FromCoordinateText}->{line.ToCoordinateText} | {line.StatusText}");
            }
        }

        builder.Append("汇总: 节点 ");
        builder.Append(snapshot.NodeCount);
        builder.Append(" / 路线 ");
        builder.Append(snapshot.EdgeCount);
        builder.Append(" / 高风险 ");
        builder.Append(snapshot.HighRiskCount);
        builder.Append(" / Boss ");
        builder.Append(snapshot.BossCount);
        return builder.ToString().TrimEnd();
    }

    private static string BuildCurrentNodeText(DungeonLayer layer) {
        if (layer?.CurrentNode == null) {
            return "当前位置: 入口选择前";
        }

        return $"当前位置: {BuildCoordinateText(layer.CurrentNode)} {layer.CurrentNode.NodeID}";
    }

    private static string BuildNodeStatusText(DungeonMapReadabilityNodeLine line) {
        List<string> parts = new List<string>();
        if (line.IsCurrent) {
            parts.Add("当前");
        } else if (line.IsNextCandidate) {
            parts.Add("可前往");
        } else if (line.IsVisited) {
            parts.Add("已访问");
        } else if (line.IsEntry) {
            parts.Add("入口");
        }

        parts.Add(line.VisibilityText);
        parts.Add(line.RiskText);
        return string.Join(" / ", parts.Where(part => !string.IsNullOrEmpty(part)));
    }

    private static string BuildNodeDetailText(DungeonMapReadabilityNodeLine line) {
        List<string> parts = new List<string> {
            line.NodeType,
            string.IsNullOrEmpty(line.RouteTheme) ? "路线 Main" : $"路线 {line.RouteTheme}"
        };

        if (!string.IsNullOrEmpty(line.RewardID)) {
            parts.Add($"奖励 {line.RewardID}");
        }

        if (!string.IsNullOrEmpty(line.NextCoordinateText)) {
            parts.Add($"后继 {line.NextCoordinateText}");
        }

        if (!string.IsNullOrEmpty(line.RiskHint)) {
            parts.Add(line.RiskHint);
        }

        return string.Join(" | ", parts);
    }

    private static string FormatVisibility(DungeonNodeVisibilityState visibility) {
        switch (visibility) {
            case DungeonNodeVisibilityState.Revealed:
                return "已揭示";
            case DungeonNodeVisibilityState.Preview:
                return "预览";
            default:
                return "隐藏";
        }
    }

    private static string BuildCoordinateText(NodeBase node) {
        if (node == null) {
            return "R?C?";
        }

        return $"R{node.MapRow}C{node.MapColumn}";
    }

    private static string BuildNextCoordinateText(NodeBase node) {
        if (node?.NextNodes == null || node.NextNodes.Count == 0) {
            return string.Empty;
        }

        List<string> coordinates = new List<string>();
        foreach (NodeBase next in node.NextNodes) {
            if (next != null) {
                coordinates.Add(BuildCoordinateText(next));
            }
        }

        coordinates.Sort();
        return string.Join(",", coordinates);
    }

    private static int CountNodes(DungeonLayer layer) {
        if (layer?.NodeRows == null) {
            return 0;
        }

        return layer.NodeRows.Sum(row => row?.Count ?? 0);
    }

    private static int CountEdges(DungeonLayer layer) {
        int count = 0;
        if (layer?.NodeRows == null) {
            return count;
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

    private static bool IsBossNode(NodeBase node) {
        return node is CombatNode
            && !string.IsNullOrEmpty(node.NodeID)
            && node.NodeID.IndexOf("boss", System.StringComparison.OrdinalIgnoreCase) >= 0;
    }

    private static int ResolveLimit(int requestedLimit, int totalCount) {
        return requestedLimit <= 0 ? totalCount : System.Math.Min(requestedLimit, totalCount);
    }

    private static void AppendLineIfNotEmpty(StringBuilder builder, string line) {
        if (!string.IsNullOrEmpty(line)) {
            builder.AppendLine(line);
        }
    }
}
