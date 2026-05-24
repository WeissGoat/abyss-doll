using System.Collections.Generic;
using System.Text;

public class CombatIntentReadabilitySnapshot {
    public bool Success;
    public string Reason;
    public string Header;
    public string CombinedText;
    public List<CombatIntentReadabilityLine> EnemyLines = new List<CombatIntentReadabilityLine>();
}

public class CombatIntentReadabilityLine {
    public string MonsterID;
    public string RuntimeID;
    public string MonsterName;
    public int RuntimeHP;
    public int RuntimeMaxHP;
    public int RuntimeShield;
    public bool IsAlive;
    public bool HasSelectedIntent;
    public bool CanExecute;
    public MonsterIntentType IntentType = MonsterIntentType.Unknown;
    public string IntentTitle;
    public string IntentDescription;
    public string StatusText;
    public string DetailText;
}

public class CombatOutcomeReadabilitySnapshot {
    public bool Success;
    public string Reason;
    public CombatOutcomeType OutcomeType = CombatOutcomeType.None;
    public CombatDefeatReasonType DefeatReason = CombatDefeatReasonType.None;
    public string Title;
    public string Summary;
    public string CombinedText;
    public int TimelineOmittedCount;
    public List<string> FighterLines = new List<string>();
    public List<string> TimelineLines = new List<string>();
}

public static class CombatReadabilityTextService {
    public static CombatIntentReadabilitySnapshot BuildIntentSnapshot(CombatSystem combat) {
        CombatIntentReadabilitySnapshot snapshot = new CombatIntentReadabilitySnapshot {
            Header = "敌方意图"
        };

        MonsterIntentReport report = MonsterIntentPreviewService.BuildReport(combat);
        if (report == null || !report.Success) {
            snapshot.Success = false;
            snapshot.Reason = report?.Reason ?? "Monster intent report is missing.";
            snapshot.CombinedText = snapshot.Reason;
            return snapshot;
        }

        snapshot.Success = true;
        if (report.Monsters == null || report.Monsters.Count == 0) {
            snapshot.Header = "暂无敌人";
            snapshot.CombinedText = snapshot.Header;
            return snapshot;
        }

        StringBuilder builder = new StringBuilder();
        builder.AppendLine(snapshot.Header);
        foreach (MonsterIntentCard card in report.Monsters) {
            CombatIntentReadabilityLine line = BuildIntentLine(card);
            if (line == null) {
                continue;
            }

            snapshot.EnemyLines.Add(line);
            builder.Append("- ");
            builder.Append(line.StatusText);
            if (!string.IsNullOrEmpty(line.DetailText)) {
                builder.Append(" | ");
                builder.Append(line.DetailText);
            }
            builder.AppendLine();
        }

        snapshot.CombinedText = builder.ToString().TrimEnd();
        return snapshot;
    }

    public static CombatOutcomeReadabilitySnapshot BuildOutcomeSnapshot(CombatOutcomeReport report, int timelineLimit = 6) {
        CombatOutcomeReadabilitySnapshot snapshot = new CombatOutcomeReadabilitySnapshot();
        if (report == null) {
            snapshot.Success = false;
            snapshot.Reason = "Combat outcome report is missing.";
            snapshot.CombinedText = snapshot.Reason;
            return snapshot;
        }

        snapshot.Success = true;
        snapshot.OutcomeType = report.OutcomeType;
        snapshot.DefeatReason = report.DefeatReason;
        snapshot.Title = string.IsNullOrEmpty(report.Title) ? "战斗结果" : report.Title;
        snapshot.Summary = report.Summary ?? string.Empty;

        AppendFighterLines(snapshot.FighterLines, "玩家", report.PlayerFighters);
        AppendFighterLines(snapshot.FighterLines, "敌方", report.EnemyFighters);
        AppendTimelineLines(snapshot, report.TimelineEvents, timelineLimit);

        snapshot.CombinedText = BuildOutcomeCombinedText(snapshot);
        return snapshot;
    }

    private static CombatIntentReadabilityLine BuildIntentLine(MonsterIntentCard card) {
        if (card == null) {
            return null;
        }

        CombatIntentReadabilityLine line = new CombatIntentReadabilityLine {
            MonsterID = card.MonsterID,
            RuntimeID = card.RuntimeID,
            MonsterName = string.IsNullOrEmpty(card.MonsterName) ? "未知敌人" : card.MonsterName,
            RuntimeHP = card.RuntimeHP,
            RuntimeMaxHP = card.RuntimeMaxHP,
            RuntimeShield = card.RuntimeShield,
            IsAlive = card.IsAlive
        };

        string hpText = BuildHpText(card.RuntimeHP, card.RuntimeMaxHP, card.RuntimeShield);
        if (!card.IsAlive) {
            line.StatusText = $"{line.MonsterName}: 已击倒";
            line.DetailText = hpText;
            return line;
        }

        MonsterActionIntentPreview selected = card.SelectedIntent;
        if (selected == null) {
            line.StatusText = $"{line.MonsterName}: 暂无可执行意图";
            line.DetailText = BuildNoIntentDetail(card, hpText);
            return line;
        }

        line.HasSelectedIntent = true;
        line.CanExecute = selected.CanExecute;
        line.IntentType = selected.IntentType;
        line.IntentTitle = string.IsNullOrEmpty(selected.Title) ? "未知意图" : selected.Title;
        line.IntentDescription = selected.Description ?? string.Empty;
        line.StatusText = $"{line.MonsterName}: {line.IntentTitle}";

        if (selected.CanExecute) {
            line.DetailText = string.IsNullOrEmpty(selected.Description)
                ? hpText
                : $"{hpText} / {selected.Description}";
        } else {
            string blockReason = string.IsNullOrEmpty(selected.BlockReason) ? "条件不足" : selected.BlockReason;
            line.DetailText = string.IsNullOrEmpty(selected.Description)
                ? $"{hpText} / 不可执行：{blockReason}"
                : $"{hpText} / {selected.Description} / 不可执行：{blockReason}";
        }

        return line;
    }

    private static string BuildNoIntentDetail(MonsterIntentCard card, string hpText) {
        if (card?.ActionIntents == null || card.ActionIntents.Count == 0) {
            return $"{hpText} / 未配置行动";
        }

        foreach (MonsterActionIntentPreview intent in card.ActionIntents) {
            if (intent != null && !string.IsNullOrEmpty(intent.BlockReason)) {
                return $"{hpText} / 阻塞：{intent.BlockReason}";
            }
        }

        return $"{hpText} / 所有行动当前都不可执行";
    }

    private static string BuildHpText(int hp, int maxHp, int shield) {
        string shieldText = shield > 0 ? $", Shield {shield}" : string.Empty;
        return $"HP {System.Math.Max(0, hp)}/{maxHp}{shieldText}";
    }

    private static void AppendFighterLines(List<string> output, string prefix, List<CombatOutcomeFighterSnapshot> fighters) {
        if (output == null || fighters == null) {
            return;
        }

        foreach (CombatOutcomeFighterSnapshot fighter in fighters) {
            if (fighter == null) {
                continue;
            }

            string shieldText = fighter.RuntimeShield > 0 ? $", Shield {fighter.RuntimeShield}" : string.Empty;
            string stateText = fighter.IsAlive ? "存活" : "失去战斗能力";
            output.Add($"{prefix}: {fighter.Name} HP {fighter.RuntimeHP}/{fighter.RuntimeMaxHP}{shieldText} ({stateText})");
        }
    }

    private static void AppendTimelineLines(CombatOutcomeReadabilitySnapshot snapshot, List<CombatTimelineEvent> timelineEvents, int timelineLimit) {
        if (snapshot == null || timelineEvents == null || timelineEvents.Count == 0) {
            return;
        }

        int safeLimit = timelineLimit <= 0 ? timelineEvents.Count : timelineLimit;
        int startIndex = System.Math.Max(0, timelineEvents.Count - safeLimit);
        snapshot.TimelineOmittedCount = startIndex;

        for (int i = startIndex; i < timelineEvents.Count; i++) {
            CombatTimelineEvent entry = timelineEvents[i];
            if (entry == null) {
                continue;
            }

            snapshot.TimelineLines.Add(FormatTimelineLine(entry));
        }
    }

    private static string FormatTimelineLine(CombatTimelineEvent entry) {
        string title = string.IsNullOrEmpty(entry.Title) ? entry.EventType.ToString() : entry.Title;
        if (!string.IsNullOrEmpty(entry.Detail)) {
            return $"#{entry.Sequence + 1} {title}: {entry.Detail}";
        }

        if (entry.EventType == CombatTimelineEventType.DamageDealt) {
            return $"#{entry.Sequence + 1} {title}: {entry.ActorName} 对 {entry.TargetName} 造成 {entry.Amount} 点伤害。";
        }

        return $"#{entry.Sequence + 1} {title}";
    }

    private static string BuildOutcomeCombinedText(CombatOutcomeReadabilitySnapshot snapshot) {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(snapshot.Title);
        if (!string.IsNullOrEmpty(snapshot.Summary)) {
            builder.AppendLine(snapshot.Summary);
        }

        if (snapshot.FighterLines.Count > 0) {
            builder.AppendLine("战斗者状态:");
            foreach (string line in snapshot.FighterLines) {
                builder.Append("- ");
                builder.AppendLine(line);
            }
        }

        if (snapshot.TimelineLines.Count > 0) {
            builder.AppendLine("最近战斗记录:");
            if (snapshot.TimelineOmittedCount > 0) {
                builder.AppendLine($"- 已省略前 {snapshot.TimelineOmittedCount} 条记录。");
            }

            foreach (string line in snapshot.TimelineLines) {
                builder.Append("- ");
                builder.AppendLine(line);
            }
        }

        return builder.ToString().TrimEnd();
    }
}
