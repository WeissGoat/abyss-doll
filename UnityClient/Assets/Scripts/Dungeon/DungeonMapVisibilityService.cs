using System;
using UnityEngine;

public enum DungeonNodeVisibilityState {
    Hidden = 0,
    Preview = 1,
    Revealed = 2
}

public enum DungeonNodeRiskLevel {
    Unknown = 0,
    Safe = 1,
    Low = 2,
    Medium = 3,
    High = 4,
    Boss = 5
}

public sealed class DungeonMapNodePresentation {
    public DungeonNodeVisibilityState Visibility { get; }
    public DungeonNodeRiskLevel RiskLevel { get; }
    public string RiskLabel { get; }
    public string RiskHint { get; }

    public bool IsHidden => Visibility == DungeonNodeVisibilityState.Hidden;
    public bool IsPreview => Visibility == DungeonNodeVisibilityState.Preview;
    public bool IsRevealed => Visibility == DungeonNodeVisibilityState.Revealed;

    public DungeonMapNodePresentation(DungeonNodeVisibilityState visibility, DungeonNodeRiskLevel riskLevel, string riskLabel, string riskHint) {
        Visibility = visibility;
        RiskLevel = riskLevel;
        RiskLabel = riskLabel;
        RiskHint = riskHint;
    }
}

public static class DungeonMapVisibilityService {
    public const int DefaultRevealDepth = 1;
    public const int DefaultPreviewDepth = 1;

    public static DungeonMapNodePresentation BuildNodePresentation(DungeonLayer layer, NodeBase node) {
        DungeonNodeVisibilityState visibility = ResolveVisibility(layer, node);
        DungeonNodeRiskLevel riskLevel = ResolveRiskLevel(node);
        return new DungeonMapNodePresentation(
            visibility,
            riskLevel,
            ResolveRiskLabel(riskLevel),
            ResolveRiskHint(node, riskLevel));
    }

    public static DungeonNodeVisibilityState ResolveVisibility(DungeonLayer layer, NodeBase node) {
        if (layer == null || node == null) {
            return DungeonNodeVisibilityState.Hidden;
        }

        if (node.IsVisited || node == layer.CurrentNode) {
            return DungeonNodeVisibilityState.Revealed;
        }

        if (layer.CurrentNode == null && layer.EntryNodes != null && layer.EntryNodes.Contains(node)) {
            return DungeonNodeVisibilityState.Revealed;
        }

        if (layer.CurrentNode != null && layer.CurrentNode.NextNodes != null && layer.CurrentNode.NextNodes.Contains(node)) {
            return DungeonNodeVisibilityState.Revealed;
        }

        if (node.MapRow < 0) {
            return DungeonNodeVisibilityState.Revealed;
        }

        int currentRow = layer.CurrentNode != null ? layer.CurrentNode.MapRow : -1;
        if (layer.CurrentNode != null && node.MapRow <= currentRow) {
            return DungeonNodeVisibilityState.Revealed;
        }

        int distance = node.MapRow - currentRow;
        if (distance <= 0) {
            return DungeonNodeVisibilityState.Revealed;
        }

        int revealDepth = ResolveRevealDepth(layer);
        if (distance <= revealDepth) {
            return DungeonNodeVisibilityState.Revealed;
        }

        int previewDepth = ResolvePreviewDepth(layer);
        if (distance <= revealDepth + previewDepth || IsBossNode(node)) {
            return DungeonNodeVisibilityState.Preview;
        }

        return DungeonNodeVisibilityState.Hidden;
    }

    public static bool ShouldRenderRouteLine(DungeonLayer layer, NodeBase from, NodeBase to) {
        DungeonNodeVisibilityState fromVisibility = ResolveVisibility(layer, from);
        DungeonNodeVisibilityState toVisibility = ResolveVisibility(layer, to);
        return fromVisibility != DungeonNodeVisibilityState.Hidden
            && toVisibility != DungeonNodeVisibilityState.Hidden;
    }

    public static int ResolveRevealDepth(DungeonLayer layer) {
        DungeonConfig config = ResolveLayerConfig(layer);
        return config != null && config.NodeRevealDepth >= 0
            ? config.NodeRevealDepth
            : DefaultRevealDepth;
    }

    public static int ResolvePreviewDepth(DungeonLayer layer) {
        DungeonConfig config = ResolveLayerConfig(layer);
        return config != null && config.NodePreviewDepth >= 0
            ? config.NodePreviewDepth
            : DefaultPreviewDepth;
    }

    public static DungeonNodeRiskLevel ResolveRiskLevel(NodeBase node) {
        if (node == null) {
            return DungeonNodeRiskLevel.Unknown;
        }

        if (!string.IsNullOrEmpty(node.RiskLevel)
            && Enum.TryParse(node.RiskLevel, true, out DungeonNodeRiskLevel configuredRisk)) {
            return configuredRisk;
        }

        DungeonNodeRiskLevel inferred = InferRiskFromNode(node);
        return ApplyRouteThemeRisk(inferred, node.RouteTheme);
    }

    public static string ResolveRiskLabel(DungeonNodeRiskLevel riskLevel) {
        switch (riskLevel) {
            case DungeonNodeRiskLevel.Safe:
                return "安全";
            case DungeonNodeRiskLevel.Low:
                return "低风险";
            case DungeonNodeRiskLevel.Medium:
                return "中风险";
            case DungeonNodeRiskLevel.High:
                return "高风险";
            case DungeonNodeRiskLevel.Boss:
                return "首领";
            default:
                return "未知风险";
        }
    }

    private static DungeonConfig ResolveLayerConfig(DungeonLayer layer) {
        if (layer == null || !ConfigManager.Dungeons.ContainsKey(layer.LayerID)) {
            return null;
        }

        return ConfigManager.Dungeons[layer.LayerID];
    }

    private static DungeonNodeRiskLevel InferRiskFromNode(NodeBase node) {
        if (node is StairsNode || node is SafeRoomNode) {
            return DungeonNodeRiskLevel.Safe;
        }

        if (node is RestStopNode) {
            return DungeonNodeRiskLevel.Low;
        }

        if (IsBossNode(node)) {
            return DungeonNodeRiskLevel.Boss;
        }

        if (node is HazardNode) {
            return DungeonNodeRiskLevel.High;
        }

        if (node is TreasureNode || node is EventNode) {
            return DungeonNodeRiskLevel.Medium;
        }

        if (node is CombatNode combatNode) {
            if (combatNode.MonsterIDs != null) {
                foreach (string monsterID in combatNode.MonsterIDs) {
                    if (string.IsNullOrEmpty(monsterID)) {
                        continue;
                    }

                    if (monsterID.StartsWith("boss_", StringComparison.OrdinalIgnoreCase)) {
                        return DungeonNodeRiskLevel.Boss;
                    }

                    if (monsterID.StartsWith("elite_", StringComparison.OrdinalIgnoreCase)) {
                        return DungeonNodeRiskLevel.High;
                    }
                }

                if (combatNode.MonsterIDs.Count > 1) {
                    return DungeonNodeRiskLevel.High;
                }
            }

            return DungeonNodeRiskLevel.Medium;
        }

        return DungeonNodeRiskLevel.Unknown;
    }

    private static DungeonNodeRiskLevel ApplyRouteThemeRisk(DungeonNodeRiskLevel riskLevel, string routeTheme) {
        if (riskLevel == DungeonNodeRiskLevel.Safe
            || riskLevel == DungeonNodeRiskLevel.Boss
            || string.IsNullOrEmpty(routeTheme)) {
            return riskLevel;
        }

        if (string.Equals(routeTheme, "RiskReward", StringComparison.OrdinalIgnoreCase)
            || string.Equals(routeTheme, "Attrition", StringComparison.OrdinalIgnoreCase)) {
            return BumpRisk(riskLevel);
        }

        if (string.Equals(routeTheme, "Safe", StringComparison.OrdinalIgnoreCase)
            && riskLevel == DungeonNodeRiskLevel.Medium) {
            return DungeonNodeRiskLevel.Low;
        }

        return riskLevel;
    }

    private static DungeonNodeRiskLevel BumpRisk(DungeonNodeRiskLevel riskLevel) {
        switch (riskLevel) {
            case DungeonNodeRiskLevel.Unknown:
                return DungeonNodeRiskLevel.Medium;
            case DungeonNodeRiskLevel.Low:
                return DungeonNodeRiskLevel.Medium;
            case DungeonNodeRiskLevel.Medium:
                return DungeonNodeRiskLevel.High;
            default:
                return riskLevel;
        }
    }

    private static string ResolveRiskHint(NodeBase node, DungeonNodeRiskLevel riskLevel) {
        if (node != null && !string.IsNullOrEmpty(node.RiskHint)) {
            return node.RiskHint;
        }

        switch (riskLevel) {
            case DungeonNodeRiskLevel.Safe:
                return "通常不会造成探索损耗。";
            case DungeonNodeRiskLevel.Low:
                return "预计只产生轻量损耗或恢复机会。";
            case DungeonNodeRiskLevel.Medium:
                return "可能产生战斗、事件或背包取舍。";
            case DungeonNodeRiskLevel.High:
                return "预计会带来明显 HP、SAN 或战利品压力。";
            case DungeonNodeRiskLevel.Boss:
                return "层末守门战，击败后进入层终点。";
            default:
                return "具体风险被深渊雾气遮蔽。";
        }
    }

    private static bool IsBossNode(NodeBase node) {
        return node is CombatNode
            && (string.Equals(node.NodeIconID, VisualAssetService.BossNodeIconID, StringComparison.OrdinalIgnoreCase)
                || (!string.IsNullOrEmpty(node.NodeID) && node.NodeID.IndexOf("boss", StringComparison.OrdinalIgnoreCase) >= 0));
    }
}
