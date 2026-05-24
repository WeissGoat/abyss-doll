using System.Collections.Generic;
using System.Text;

public class GrowthReadabilitySnapshot {
    public bool Success;
    public string Reason;
    public int LayerID;
    public bool CanDive;
    public string Header;
    public string DiveStatusText;
    public string DiveDetailText;
    public string CombinedText;
    public int AvailableActionCount;
    public int MissingRequirementActionCount;
    public int BlockedActionCount;
    public List<string> IssueLines = new List<string>();
    public List<GrowthReadabilityActionLine> ActionLines = new List<GrowthReadabilityActionLine>();
}

public class GrowthReadabilityActionLine {
    public GrowthActionType Type;
    public GrowthActionStatus Status;
    public string ActionID;
    public string TargetID;
    public string Title;
    public bool CanExecute;
    public string StatusText;
    public string DetailText;
    public string CostText;
    public int MissingMoney;
    public List<string> MissingItemLines = new List<string>();
}

public static class GrowthReadabilityTextService {
    public static GrowthReadabilitySnapshot BuildSnapshot(PlayerProfile player, int targetLayerID, int suggestionLimit = 6) {
        return BuildSnapshot(GrowthFeedbackService.BuildReport(player, targetLayerID), suggestionLimit);
    }

    public static GrowthReadabilitySnapshot BuildSnapshot(GrowthFeedbackReport report, int suggestionLimit = 6) {
        GrowthReadabilitySnapshot snapshot = new GrowthReadabilitySnapshot {
            Header = "局外整备",
            LayerID = report?.LayerID ?? 0
        };

        if (report == null || !report.Success) {
            snapshot.Success = false;
            snapshot.Reason = report?.Reason ?? "Growth feedback report is missing.";
            snapshot.DiveStatusText = "整备数据不可用";
            snapshot.DiveDetailText = snapshot.Reason;
            snapshot.CombinedText = BuildCombinedText(snapshot);
            return snapshot;
        }

        snapshot.Success = true;
        snapshot.CanDive = report.CanDive;
        snapshot.DiveStatusText = report.CanDive
            ? $"第 {report.LayerID} 层：可以下潜"
            : $"第 {report.LayerID} 层：暂不能下潜";
        snapshot.DiveDetailText = string.IsNullOrEmpty(report.DiveSummary)
            ? "Ready to dive."
            : report.DiveSummary;

        AppendIssueLines(snapshot, report);
        AppendActionLines(snapshot, report, suggestionLimit);
        snapshot.CombinedText = BuildCombinedText(snapshot);
        return snapshot;
    }

    private static void AppendIssueLines(GrowthReadabilitySnapshot snapshot, GrowthFeedbackReport report) {
        if (snapshot == null || report?.DiveIssues == null) {
            return;
        }

        foreach (DiveReadinessIssue issue in report.DiveIssues) {
            if (issue == null) {
                continue;
            }

            string severity = FormatIssueSeverity(issue.Severity);
            string hint = string.IsNullOrEmpty(issue.ActionHint) ? string.Empty : $" / {issue.ActionHint}";
            snapshot.IssueLines.Add($"{severity}: {issue.Message}{hint}");
        }
    }

    private static void AppendActionLines(GrowthReadabilitySnapshot snapshot, GrowthFeedbackReport report, int suggestionLimit) {
        if (snapshot == null || report?.Suggestions == null) {
            return;
        }

        int safeLimit = suggestionLimit <= 0 ? report.Suggestions.Count : suggestionLimit;
        int added = 0;
        foreach (GrowthActionSuggestion suggestion in report.Suggestions) {
            if (suggestion == null) {
                continue;
            }

            CountSuggestion(snapshot, suggestion);
            if (added >= safeLimit) {
                continue;
            }

            snapshot.ActionLines.Add(BuildActionLine(suggestion));
            added++;
        }
    }

    private static void CountSuggestion(GrowthReadabilitySnapshot snapshot, GrowthActionSuggestion suggestion) {
        switch (suggestion.Status) {
            case GrowthActionStatus.Available:
                snapshot.AvailableActionCount++;
                break;
            case GrowthActionStatus.MissingRequirements:
                snapshot.MissingRequirementActionCount++;
                break;
            default:
                snapshot.BlockedActionCount++;
                break;
        }
    }

    private static GrowthReadabilityActionLine BuildActionLine(GrowthActionSuggestion suggestion) {
        GrowthReadabilityActionLine line = new GrowthReadabilityActionLine {
            Type = suggestion.Type,
            Status = suggestion.Status,
            ActionID = suggestion.ActionID,
            TargetID = suggestion.TargetID,
            Title = string.IsNullOrEmpty(suggestion.Title) ? suggestion.ActionID : suggestion.Title,
            CanExecute = suggestion.CanExecute,
            StatusText = FormatActionStatus(suggestion.Status),
            DetailText = BuildActionDetailText(suggestion),
            CostText = BuildCostText(suggestion),
            MissingMoney = suggestion.MissingMoney
        };

        if (suggestion.ItemRequirements != null) {
            foreach (GrowthCostRequirementLine requirement in suggestion.ItemRequirements) {
                if (requirement != null && requirement.MissingCount > 0) {
                    string name = string.IsNullOrEmpty(requirement.Name) ? requirement.ItemID : requirement.Name;
                    line.MissingItemLines.Add($"{name} 缺 {requirement.MissingCount} 个 ({requirement.OwnedCount}/{requirement.RequiredCount})");
                }
            }
        }

        return line;
    }

    private static string BuildActionDetailText(GrowthActionSuggestion suggestion) {
        if (suggestion == null) {
            return string.Empty;
        }

        string description = string.IsNullOrEmpty(suggestion.Description) ? string.Empty : suggestion.Description;
        string reason = string.IsNullOrEmpty(suggestion.Reason) ? string.Empty : suggestion.Reason;
        if (string.IsNullOrEmpty(description)) {
            return reason;
        }

        if (string.IsNullOrEmpty(reason)) {
            return description;
        }

        return $"{description} / {reason}";
    }

    private static string BuildCostText(GrowthActionSuggestion suggestion) {
        if (suggestion == null) {
            return string.Empty;
        }

        List<string> parts = new List<string>();
        if (suggestion.RequiredMoney > 0) {
            string moneyText = suggestion.MissingMoney > 0
                ? $"金币 {suggestion.OwnedMoney}/{suggestion.RequiredMoney}，缺 {suggestion.MissingMoney}"
                : $"金币 {suggestion.RequiredMoney}";
            parts.Add(moneyText);
        }

        if (suggestion.ItemRequirements != null) {
            foreach (GrowthCostRequirementLine requirement in suggestion.ItemRequirements) {
                if (requirement == null) {
                    continue;
                }

                string name = string.IsNullOrEmpty(requirement.Name) ? requirement.ItemID : requirement.Name;
                parts.Add($"{name} {requirement.OwnedCount}/{requirement.RequiredCount}");
            }
        }

        return parts.Count == 0 ? "无消耗" : string.Join("；", parts);
    }

    private static string BuildCombinedText(GrowthReadabilitySnapshot snapshot) {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(snapshot.Header ?? "局外整备");
        if (!string.IsNullOrEmpty(snapshot.DiveStatusText)) {
            builder.AppendLine(snapshot.DiveStatusText);
        }

        if (!string.IsNullOrEmpty(snapshot.DiveDetailText)) {
            builder.AppendLine(snapshot.DiveDetailText);
        }

        if (snapshot.IssueLines.Count > 0) {
            builder.AppendLine("下潜检查:");
            foreach (string line in snapshot.IssueLines) {
                builder.Append("- ");
                builder.AppendLine(line);
            }
        }

        if (snapshot.ActionLines.Count > 0) {
            builder.AppendLine("建议行动:");
            foreach (GrowthReadabilityActionLine line in snapshot.ActionLines) {
                if (line == null) {
                    continue;
                }

                builder.Append("- ");
                builder.Append(line.StatusText);
                builder.Append(" ");
                builder.Append(line.Title);
                if (!string.IsNullOrEmpty(line.CostText)) {
                    builder.Append(" | ");
                    builder.Append(line.CostText);
                }
                if (!string.IsNullOrEmpty(line.DetailText)) {
                    builder.Append(" | ");
                    builder.Append(line.DetailText);
                }
                builder.AppendLine();
            }
        }

        builder.Append("汇总: 可执行 ");
        builder.Append(snapshot.AvailableActionCount);
        builder.Append(" / 缺口 ");
        builder.Append(snapshot.MissingRequirementActionCount);
        builder.Append(" / 阻断 ");
        builder.Append(snapshot.BlockedActionCount);
        return builder.ToString().TrimEnd();
    }

    private static string FormatIssueSeverity(DiveReadinessIssueSeverity severity) {
        switch (severity) {
            case DiveReadinessIssueSeverity.Blocker:
                return "阻断";
            case DiveReadinessIssueSeverity.Warning:
                return "警告";
            default:
                return "信息";
        }
    }

    private static string FormatActionStatus(GrowthActionStatus status) {
        switch (status) {
            case GrowthActionStatus.Available:
                return "可执行";
            case GrowthActionStatus.MissingRequirements:
                return "缺口";
            default:
                return "阻断";
        }
    }
}
