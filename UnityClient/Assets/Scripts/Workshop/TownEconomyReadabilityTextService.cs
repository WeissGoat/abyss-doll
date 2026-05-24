using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEngine;

public class TownEconomyReadabilitySnapshot {
    public bool Success;
    public string Reason;
    public string Header;
    public string CalendarText;
    public string MoneyText;
    public string RentPressureText;
    public string CombinedText;
    public int SellableCount;
    public int TotalBestSellValue;
    public int AvailableOrderCount;
    public int AcceptedOrderCount;
    public int DeliverableOrderCount;
    public int ActiveRumorCount;
    public int FactionCount;
    public int PawnCandidateCount;
    public List<string> PressureLines = new List<string>();
    public List<TownEconomyReadabilitySellLine> SellLines = new List<TownEconomyReadabilitySellLine>();
    public List<TownEconomyReadabilityOrderLine> OrderLines = new List<TownEconomyReadabilityOrderLine>();
    public List<TownEconomyReadabilityRumorLine> RumorLines = new List<TownEconomyReadabilityRumorLine>();
    public List<TownEconomyReadabilityFactionLine> FactionLines = new List<TownEconomyReadabilityFactionLine>();
    public List<TownEconomyReadabilityPawnLine> PawnLines = new List<TownEconomyReadabilityPawnLine>();
}

public class TownEconomyReadabilitySellLine {
    public string InstanceID;
    public string ItemID;
    public string Name;
    public string SourceContainer;
    public string BestChannel;
    public int BestValue;
    public bool IsSellable;
    public string StatusText;
    public string DetailText;
    public List<string> ChannelLines = new List<string>();
}

public class TownEconomyReadabilityOrderLine {
    public string InstanceID;
    public string OrderID;
    public string FactionID;
    public string FactionName;
    public string Title;
    public string Status;
    public bool CanAccept;
    public bool CanDeliver;
    public string StatusText;
    public string ProgressText;
    public string DeadlineText;
    public string RewardText;
    public string DetailText;
}

public class TownEconomyReadabilityRumorLine {
    public string RumorID;
    public string Title;
    public string Channel;
    public string StatusText;
    public string DetailText;
    public string TargetText;
    public int DaysRemaining;
}

public class TownEconomyReadabilityFactionLine {
    public string FactionID;
    public string Title;
    public bool IsBlackMarket;
    public int ReputationValue;
    public int TrustValue;
    public int CurrentRank;
    public int NextRankThreshold;
    public string StatusText;
    public string DetailText;
}

public class TownEconomyReadabilityPawnLine {
    public string ItemID;
    public string Name;
    public string SourceContainer;
    public int PawnValue;
    public string DetailText;
}

public static class TownEconomyReadabilityTextService {
    public static TownEconomyReadabilitySnapshot BuildSnapshot(
        PlayerProfile player,
        int sellLimit = 6,
        int orderLimit = 6,
        int rumorLimit = 4,
        int factionLimit = 4,
        int pawnLimit = 4) {
        return BuildSnapshot(
            TownEconomyOverviewService.BuildReport(player),
            sellLimit,
            orderLimit,
            rumorLimit,
            factionLimit,
            pawnLimit);
    }

    public static TownEconomyReadabilitySnapshot BuildSnapshot(
        TownEconomyOverviewReport report,
        int sellLimit = 6,
        int orderLimit = 6,
        int rumorLimit = 4,
        int factionLimit = 4,
        int pawnLimit = 4) {
        TownEconomyReadabilitySnapshot snapshot = new TownEconomyReadabilitySnapshot {
            Header = "小镇经济",
        };

        if (report == null || !report.Success) {
            snapshot.Success = false;
            snapshot.Reason = report?.Reason ?? "Town economy overview report is missing.";
            snapshot.CalendarText = "经济数据不可用";
            snapshot.MoneyText = snapshot.Reason;
            snapshot.RentPressureText = snapshot.Reason;
            snapshot.CombinedText = BuildCombinedText(snapshot);
            return snapshot;
        }

        snapshot.Success = true;
        snapshot.CalendarText = $"第 {report.Day} 天 / 第 {report.Month} 月 {report.MonthDay} 日 / 第 {report.CurrentWeek} 周";
        snapshot.MoneyText = $"金币 {report.Money} / 月收入 {report.MonthlyGrossIncome} / 欠债 {report.DebtAmount}";
        snapshot.RentPressureText = BuildRentPressureText(report);
        AppendPressureLines(snapshot, report);
        AppendSellLines(snapshot, report, sellLimit);
        AppendOrderLines(snapshot, report, orderLimit);
        AppendRumorLines(snapshot, report, rumorLimit);
        AppendFactionLines(snapshot, report, factionLimit);
        AppendPawnLines(snapshot, report, pawnLimit);
        snapshot.CombinedText = BuildCombinedText(snapshot);
        return snapshot;
    }

    private static void AppendPressureLines(TownEconomyReadabilitySnapshot snapshot, TownEconomyOverviewReport report) {
        if (report.HasPendingMonthlyRent) {
            snapshot.PressureLines.Add($"月租待处理: {report.PendingMonthlyBillAmount}G，触发日 {report.PendingMonthlyRentDay}");
        }

        if (report.EstimatedMonthlyShortfall > 0) {
            snapshot.PressureLines.Add($"预计月末缺口: {report.EstimatedMonthlyShortfall}G");
        }

        if (report.DebtLevel > 0 || report.DebtAmount > 0) {
            snapshot.PressureLines.Add($"欠债压力: 等级 {report.DebtLevel}，本金 {report.DebtAmount}G");
        }

        if (report.PawnCandidates != null && report.PawnCandidates.Count > 0) {
            int pawnValue = report.PawnCandidates.Sum(candidate => Mathf.Max(0, candidate?.PawnValue ?? 0));
            snapshot.PressureLines.Add($"可典当候选: {report.PawnCandidates.Count} 件，预估 {pawnValue}G");
        }
    }

    private static void AppendSellLines(TownEconomyReadabilitySnapshot snapshot, TownEconomyOverviewReport report, int limit) {
        if (report.SellCandidates == null) {
            return;
        }

        int safeLimit = ResolveLimit(limit, report.SellCandidates.Count);
        foreach (TownEconomySellCandidate candidate in report.SellCandidates) {
            if (candidate == null) {
                continue;
            }

            if (candidate.IsSellable) {
                snapshot.SellableCount++;
                snapshot.TotalBestSellValue += Mathf.Max(0, candidate.BestValue);
            }

            if (snapshot.SellLines.Count >= safeLimit) {
                continue;
            }

            snapshot.SellLines.Add(BuildSellLine(candidate));
        }
    }

    private static TownEconomyReadabilitySellLine BuildSellLine(TownEconomySellCandidate candidate) {
        TownEconomyReadabilitySellLine line = new TownEconomyReadabilitySellLine {
            InstanceID = candidate.InstanceID,
            ItemID = candidate.ItemID,
            Name = string.IsNullOrEmpty(candidate.Name) ? candidate.ItemID : candidate.Name,
            SourceContainer = candidate.SourceContainer,
            BestChannel = candidate.BestChannel,
            BestValue = candidate.BestValue,
            IsSellable = candidate.IsSellable,
            StatusText = candidate.IsSellable ? "可出售" : "不可出售",
            DetailText = candidate.IsSellable
                ? $"最佳渠道 {FormatChannel(candidate.BestChannel)}，估值 {candidate.BestValue}G"
                : candidate.Reason
        };

        if (candidate.ChannelValues != null) {
            foreach (TownEconomySellChannelValue value in candidate.ChannelValues) {
                if (value == null) {
                    continue;
                }

                string rumorText = value.AppliedRumorIDs != null && value.AppliedRumorIDs.Count > 0
                    ? $"，传闻 {string.Join(",", value.AppliedRumorIDs)}"
                    : string.Empty;
                string status = value.CanSell ? $"{value.FinalValue}G" : value.Reason;
                line.ChannelLines.Add($"{FormatChannel(value.Channel)}: {status}{rumorText}");
            }
        }

        return line;
    }

    private static void AppendOrderLines(TownEconomyReadabilitySnapshot snapshot, TownEconomyOverviewReport report, int limit) {
        if (report.Orders == null) {
            return;
        }

        int safeLimit = ResolveLimit(limit, report.Orders.Count);
        foreach (TownEconomyOrderCard order in report.Orders) {
            if (order == null) {
                continue;
            }

            CountOrder(snapshot, order);
            if (snapshot.OrderLines.Count >= safeLimit) {
                continue;
            }

            snapshot.OrderLines.Add(BuildOrderLine(order));
        }
    }

    private static void CountOrder(TownEconomyReadabilitySnapshot snapshot, TownEconomyOrderCard order) {
        EconomyOrderStatus status = ParseOrderStatus(order.Status);
        if (status == EconomyOrderStatus.Available) {
            snapshot.AvailableOrderCount++;
        } else if (status == EconomyOrderStatus.Accepted || status == EconomyOrderStatus.InProgress) {
            snapshot.AcceptedOrderCount++;
        }

        if (order.CanDeliver) {
            snapshot.DeliverableOrderCount++;
        }
    }

    private static TownEconomyReadabilityOrderLine BuildOrderLine(TownEconomyOrderCard order) {
        TownEconomyReadabilityOrderLine line = new TownEconomyReadabilityOrderLine {
            InstanceID = order.InstanceID,
            OrderID = order.OrderID,
            FactionID = order.FactionID,
            FactionName = order.FactionName,
            Title = string.IsNullOrEmpty(order.DisplayName) ? order.OrderID : order.DisplayName,
            Status = order.Status,
            CanAccept = order.CanAccept,
            CanDeliver = order.CanDeliver,
            StatusText = FormatOrderStatus(order),
            ProgressText = $"{order.MatchedOwnedCount}/{order.RequiredCount}",
            DeadlineText = order.DeadlineDay > 0
                ? $"剩余 {order.DaysRemaining} 天"
                : "无期限",
            RewardText = BuildOrderRewardText(order),
            DetailText = BuildOrderDetailText(order)
        };
        return line;
    }

    private static string BuildOrderRewardText(TownEconomyOrderCard order) {
        int money = Mathf.Max(0, order.FixedGold) + Mathf.Max(0, order.GuaranteedRewardMoneyPreview);
        List<string> parts = new List<string>();
        if (money > 0) {
            parts.Add($"{money}G");
        }

        if (order.ReputationDelta != 0) {
            parts.Add($"声望 {FormatSigned(order.ReputationDelta)}");
        }

        if (order.TrustDelta != 0) {
            parts.Add($"信任 {FormatSigned(order.TrustDelta)}");
        }

        if (order.RewardItemPreviewIDs != null && order.RewardItemPreviewIDs.Count > 0) {
            parts.Add($"物品 {string.Join(",", order.RewardItemPreviewIDs)}");
        }

        return parts.Count == 0 ? "无预览奖励" : string.Join(" / ", parts);
    }

    private static string BuildOrderDetailText(TownEconomyOrderCard order) {
        if (!string.IsNullOrEmpty(order.BlockReason)) {
            return order.BlockReason;
        }

        if (order.CanDeliver) {
            return "材料已满足，可交付。";
        }

        if (order.MissingCount > 0) {
            return $"还缺 {order.MissingCount} 件匹配物。";
        }

        if (order.CanAccept) {
            return "可接取。";
        }

        return string.IsNullOrEmpty(order.Description) ? string.Empty : order.Description;
    }

    private static void AppendRumorLines(TownEconomyReadabilitySnapshot snapshot, TownEconomyOverviewReport report, int limit) {
        if (report.Rumors == null) {
            return;
        }

        snapshot.ActiveRumorCount = report.Rumors.Count;
        int safeLimit = ResolveLimit(limit, report.Rumors.Count);
        foreach (TownEconomyRumorCard rumor in report.Rumors) {
            if (rumor == null || snapshot.RumorLines.Count >= safeLimit) {
                continue;
            }

            snapshot.RumorLines.Add(BuildRumorLine(rumor));
        }
    }

    private static TownEconomyReadabilityRumorLine BuildRumorLine(TownEconomyRumorCard rumor) {
        return new TownEconomyReadabilityRumorLine {
            RumorID = rumor.RumorID,
            Title = string.IsNullOrEmpty(rumor.DisplayName) ? rumor.RumorID : rumor.DisplayName,
            Channel = rumor.Channel,
            DaysRemaining = rumor.DaysRemaining,
            StatusText = $"{FormatChannel(rumor.Channel)} x{rumor.PriceMultiplier:0.##}，剩余 {rumor.DaysRemaining} 天",
            TargetText = BuildRumorTargetText(rumor),
            DetailText = rumor.Description
        };
    }

    private static string BuildRumorTargetText(TownEconomyRumorCard rumor) {
        List<string> parts = new List<string>();
        if (rumor.TargetTags != null && rumor.TargetTags.Count > 0) {
            parts.Add($"目标 {string.Join(",", rumor.TargetTags)}");
        }

        if (rumor.BoostedOrderTags != null && rumor.BoostedOrderTags.Count > 0) {
            parts.Add($"订单 {string.Join(",", rumor.BoostedOrderTags)}");
        }

        return parts.Count == 0 ? "全局影响" : string.Join(" / ", parts);
    }

    private static void AppendFactionLines(TownEconomyReadabilitySnapshot snapshot, TownEconomyOverviewReport report, int limit) {
        if (report.Factions == null) {
            return;
        }

        snapshot.FactionCount = report.Factions.Count;
        int safeLimit = ResolveLimit(limit, report.Factions.Count);
        foreach (TownEconomyFactionSummary faction in report.Factions) {
            if (faction == null || snapshot.FactionLines.Count >= safeLimit) {
                continue;
            }

            snapshot.FactionLines.Add(BuildFactionLine(faction));
        }
    }

    private static TownEconomyReadabilityFactionLine BuildFactionLine(TownEconomyFactionSummary faction) {
        string cooldownText = faction.IsOnCooldown ? $"冷却至第 {faction.CooldownUntilDay} 天" : "可接触";
        return new TownEconomyReadabilityFactionLine {
            FactionID = faction.FactionID,
            Title = string.IsNullOrEmpty(faction.DisplayName) ? faction.FactionID : faction.DisplayName,
            IsBlackMarket = faction.IsBlackMarket,
            ReputationValue = faction.ReputationValue,
            TrustValue = faction.TrustValue,
            CurrentRank = faction.CurrentRank,
            NextRankThreshold = faction.NextRankThreshold,
            StatusText = cooldownText,
            DetailText = $"声望 {faction.ReputationValue} / 信任 {faction.TrustValue} / 已完成 {faction.CompletedOrderCount} / 可接 {faction.AvailableOrderCount}"
        };
    }

    private static void AppendPawnLines(TownEconomyReadabilitySnapshot snapshot, TownEconomyOverviewReport report, int limit) {
        if (report.PawnCandidates == null) {
            return;
        }

        snapshot.PawnCandidateCount = report.PawnCandidates.Count;
        int safeLimit = ResolveLimit(limit, report.PawnCandidates.Count);
        foreach (PawnCandidate candidate in report.PawnCandidates) {
            if (candidate == null || snapshot.PawnLines.Count >= safeLimit) {
                continue;
            }

            snapshot.PawnLines.Add(new TownEconomyReadabilityPawnLine {
                ItemID = candidate.ItemID,
                Name = string.IsNullOrEmpty(candidate.Name) ? candidate.ItemID : candidate.Name,
                SourceContainer = candidate.SourceContainer,
                PawnValue = candidate.PawnValue,
                DetailText = $"{FormatContainer(candidate.SourceContainer)} / 原值 {candidate.BaseValue}G / 典当 {candidate.PawnValue}G"
            });
        }
    }

    private static string BuildRentPressureText(TownEconomyOverviewReport report) {
        if (report.HasPendingMonthlyRent) {
            return $"月租待处理: {report.PendingMonthlyBillAmount}G";
        }

        string shortfall = report.EstimatedMonthlyShortfall > 0
            ? $"，预计缺 {report.EstimatedMonthlyShortfall}G"
            : "，预计可支付";
        return $"月租倒计时 {report.RentCountdown} 天 / 预计账单 {report.EstimatedMonthlyBill}G{shortfall}";
    }

    private static string BuildCombinedText(TownEconomyReadabilitySnapshot snapshot) {
        StringBuilder builder = new StringBuilder();
        builder.AppendLine(snapshot.Header ?? "小镇经济");
        AppendLineIfNotEmpty(builder, snapshot.CalendarText);
        AppendLineIfNotEmpty(builder, snapshot.MoneyText);
        AppendLineIfNotEmpty(builder, snapshot.RentPressureText);

        if (snapshot.PressureLines.Count > 0) {
            builder.AppendLine("压力提示:");
            AppendBullets(builder, snapshot.PressureLines);
        }

        if (snapshot.SellLines.Count > 0) {
            builder.AppendLine("可出售物:");
            foreach (TownEconomyReadabilitySellLine line in snapshot.SellLines) {
                builder.AppendLine($"- {line.StatusText} {line.Name} | {line.DetailText}");
            }
        }

        if (snapshot.OrderLines.Count > 0) {
            builder.AppendLine("订单:");
            foreach (TownEconomyReadabilityOrderLine line in snapshot.OrderLines) {
                builder.AppendLine($"- {line.StatusText} {line.Title} | 进度 {line.ProgressText} | 奖励 {line.RewardText} | {line.DeadlineText}");
            }
        }

        if (snapshot.RumorLines.Count > 0) {
            builder.AppendLine("传闻:");
            foreach (TownEconomyReadabilityRumorLine line in snapshot.RumorLines) {
                builder.AppendLine($"- {line.Title} | {line.StatusText} | {line.TargetText}");
            }
        }

        if (snapshot.FactionLines.Count > 0) {
            builder.AppendLine("势力:");
            foreach (TownEconomyReadabilityFactionLine line in snapshot.FactionLines) {
                builder.AppendLine($"- {line.Title} | {line.StatusText} | {line.DetailText}");
            }
        }

        if (snapshot.PawnLines.Count > 0) {
            builder.AppendLine("典当候选:");
            foreach (TownEconomyReadabilityPawnLine line in snapshot.PawnLines) {
                builder.AppendLine($"- {line.Name} | {line.DetailText}");
            }
        }

        builder.Append("汇总: 可售 ");
        builder.Append(snapshot.SellableCount);
        builder.Append(" / 预估 ");
        builder.Append(snapshot.TotalBestSellValue);
        builder.Append("G / 可交付订单 ");
        builder.Append(snapshot.DeliverableOrderCount);
        builder.Append(" / 传闻 ");
        builder.Append(snapshot.ActiveRumorCount);
        return builder.ToString().TrimEnd();
    }

    private static void AppendLineIfNotEmpty(StringBuilder builder, string text) {
        if (!string.IsNullOrEmpty(text)) {
            builder.AppendLine(text);
        }
    }

    private static void AppendBullets(StringBuilder builder, List<string> lines) {
        foreach (string line in lines) {
            if (!string.IsNullOrEmpty(line)) {
                builder.AppendLine($"- {line}");
            }
        }
    }

    private static string FormatOrderStatus(TownEconomyOrderCard order) {
        if (order == null) {
            return "未知";
        }

        if (order.IsExpired) {
            return "已过期";
        }

        if (order.CanDeliver) {
            return "可交付";
        }

        if (order.CanAccept) {
            return "可接取";
        }

        EconomyOrderStatus status = ParseOrderStatus(order.Status);
        switch (status) {
            case EconomyOrderStatus.Accepted:
            case EconomyOrderStatus.InProgress:
                return "进行中";
            case EconomyOrderStatus.Available:
                return "待接取";
            case EconomyOrderStatus.Completed:
                return "已完成";
            case EconomyOrderStatus.Expired:
                return "已过期";
            case EconomyOrderStatus.Abandoned:
                return "已放弃";
            case EconomyOrderStatus.Betrayed:
                return "已背叛";
            default:
                return order.Status;
        }
    }

    private static EconomyOrderStatus ParseOrderStatus(string status) {
        EconomyOrderStatus parsed;
        if (System.Enum.TryParse(status, true, out parsed)) {
            return parsed;
        }

        return EconomyOrderStatus.Available;
    }

    private static int ResolveLimit(int requestedLimit, int totalCount) {
        return requestedLimit <= 0 ? totalCount : Mathf.Min(requestedLimit, totalCount);
    }

    private static string FormatChannel(string channel) {
        if (string.IsNullOrEmpty(channel)) {
            return "未知渠道";
        }

        if (string.Equals(channel, EconomySellChannel.DumpBox.ToString(), System.StringComparison.OrdinalIgnoreCase)) {
            return "倾倒箱";
        }

        if (string.Equals(channel, EconomySellChannel.Showcase.ToString(), System.StringComparison.OrdinalIgnoreCase)) {
            return "橱窗";
        }

        if (string.Equals(channel, EconomySellChannel.BlackMarket.ToString(), System.StringComparison.OrdinalIgnoreCase)) {
            return "黑市";
        }

        return channel;
    }

    private static string FormatContainer(string container) {
        if (string.Equals(container, "Backpack", System.StringComparison.OrdinalIgnoreCase)) {
            return "背包";
        }

        if (string.Equals(container, "GroundInventory", System.StringComparison.OrdinalIgnoreCase)) {
            return "仓库";
        }

        return string.IsNullOrEmpty(container) ? "未知容器" : container;
    }

    private static string FormatSigned(int value) {
        return value >= 0 ? $"+{value}" : value.ToString();
    }
}
