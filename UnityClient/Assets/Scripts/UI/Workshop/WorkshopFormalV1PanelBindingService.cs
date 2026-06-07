using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public sealed class WorkshopFormalV1PanelBinding {
    public static readonly WorkshopFormalV1PanelBinding Empty = new WorkshopFormalV1PanelBinding();

    public readonly Dictionary<string, string> PanelTexts = new Dictionary<string, string>();
    public readonly Dictionary<string, string> RowTexts = new Dictionary<string, string>();
}

public static class WorkshopFormalV1PanelBindingService {
    public static WorkshopFormalV1PanelBinding Build(string screenID, PlayerProfile player) {
        if (player == null) {
            return WorkshopFormalV1PanelBinding.Empty;
        }

        switch (screenID) {
            case "maintenance_panel":
                return BuildMaintenanceBinding(player);
            case "daily_bill_report":
                return BuildDailyBillBinding(player);
            case "shop_staging":
                return BuildShopStagingBinding(player);
            case "order_board":
                return BuildOrderBoardBinding(player);
            case "rumor_board":
                return BuildRumorBoardBinding(player);
            case "business_settlement":
                return BuildBusinessSettlementBinding(player);
            case "chassis_upgrade_panel":
                return BuildChassisUpgradeBinding(player);
            case "doll_interaction":
                return BuildDollInteractionBinding(player);
            case "doll_room":
                return BuildDollRoomBinding(player);
            case "faction_shop":
                return BuildFactionShopBinding(player);
            case "scenario_event":
                return BuildScenarioEventBinding(player);
            default:
                return WorkshopFormalV1PanelBinding.Empty;
        }
    }

    public static string ResolveText(Dictionary<string, string> values, string key, string fallback) {
        if (values != null && values.TryGetValue(key, out string value) && !string.IsNullOrEmpty(value)) {
            return value;
        }

        return fallback;
    }

    private static WorkshopFormalV1PanelBinding BuildMaintenanceBinding(PlayerProfile player) {
        GrowthReadabilitySnapshot growth = GrowthReadabilityTextService.BuildSnapshot(player, ResolveTargetLayer(player), 5);
        DollCoreStateReadabilitySnapshot doll = DollCoreStateReadabilityService.BuildSnapshot(player);
        GrowthReadabilityActionLine maintenance = FirstAction(growth, GrowthActionType.ApplyMaintenance);

        WorkshopFormalV1PanelBinding binding = new WorkshopFormalV1PanelBinding();
        binding.PanelTexts["DollConditionPanel"] = JoinLines(
            "人偶状态",
            doll.HPText,
            doll.SANText,
            doll.MaintenanceText,
            doll.ChassisText);
        binding.PanelTexts["DiveReadinessPanel"] = JoinLines(
            "下潜许可",
            growth.DiveStatusText,
            FirstNonEmpty(growth.DiveDetailText, "未发现下潜阻断。"),
            FormatLines(growth.IssueLines, 3, "- "));
        binding.PanelTexts["MaterialCostListPanel"] = JoinLines(
            "维护 / 制造建议",
            FormatGrowthActions(growth.ActionLines, 5));
        binding.PanelTexts["WearCorrosionPanel"] = JoinLines(
            "磨损与侵蚀",
            doll.MaintenanceText,
            FormatLines(doll.WarningLines, 4, "- "));
        binding.PanelTexts["RepairActionPanel"] = JoinLines(
            "整备概览",
            $"可执行 {growth.AvailableActionCount} / 缺口 {growth.MissingRequirementActionCount} / 阻断 {growth.BlockedActionCount}");
        binding.RowTexts["CostRow_Template"] = maintenance != null
            ? $"{maintenance.StatusText} {maintenance.Title} | {maintenance.CostText}"
            : "暂无维护消耗。";
        binding.RowTexts["StatusRow_Template"] = FirstNonEmpty(FirstLine(growth.IssueLines), doll.MaintenanceText, "状态正常。");
        return binding;
    }

    private static WorkshopFormalV1PanelBinding BuildDailyBillBinding(PlayerProfile player) {
        TownEconomyReadabilitySnapshot economy = BuildEconomySnapshot(player);
        WorkshopFormalV1PanelBinding binding = new WorkshopFormalV1PanelBinding();
        binding.PanelTexts["SummaryPanel"] = JoinLines(
            "账单摘要",
            economy.CalendarText,
            economy.MoneyText,
            economy.RentPressureText);
        binding.PanelTexts["PressureWarningPanel"] = JoinLines(
            "压力提示",
            FormatLines(economy.PressureLines, 4, "- "));
        binding.PanelTexts["UnsoldGoodsPanel"] = JoinLines(
            "待处理物品",
            $"可售 {economy.SellableCount} 件 / 预估 {economy.TotalBestSellValue}G",
            FormatSellLines(economy.SellLines, 5));
        binding.PanelTexts["IncomeExpenseListPanel"] = JoinLines(
            "收入 / 支出",
            $"可接订单 {economy.AvailableOrderCount} / 进行中 {economy.AcceptedOrderCount} / 可交付 {economy.DeliverableOrderCount}",
            FormatOrderLines(economy.OrderLines, 4));
        binding.PanelTexts["ActionPanel"] = JoinLines(
            "账单动作",
            $"传闻 {economy.ActiveRumorCount} / 势力 {economy.FactionCount} / 典当候选 {economy.PawnCandidateCount}");
        binding.RowTexts["UnsoldRow_Template"] = FirstSellLine(economy);
        binding.RowTexts["BillRow_Template"] = FirstNonEmpty(economy.RentPressureText, "无账单压力。");
        return binding;
    }

    private static WorkshopFormalV1PanelBinding BuildShopStagingBinding(PlayerProfile player) {
        TownEconomyReadabilitySnapshot economy = BuildEconomySnapshot(player);
        WorkshopFormalV1PanelBinding binding = new WorkshopFormalV1PanelBinding();
        binding.PanelTexts["ItemListPanel"] = JoinLines(
            "出售候选",
            FormatSellLines(economy.SellLines, 6));
        binding.PanelTexts["ChannelPanel"] = JoinLines(
            "渠道估值",
            FormatChannelLines(economy.SellLines.FirstOrDefault()));
        binding.PanelTexts["OrderLanePanel"] = JoinLines(
            "订单渠道",
            FormatOrderLines(economy.OrderLines, 3));
        binding.PanelTexts["BlackMarketLanePanel"] = JoinLines(
            "黑市 / 传闻",
            FormatRumorLines(economy.RumorLines, 4));
        binding.PanelTexts["ActionPanel"] = JoinLines(
            "出售概览",
            $"可售 {economy.SellableCount} 件 / 最佳估值 {economy.TotalBestSellValue}G");
        binding.RowTexts["ItemRow_Template"] = FirstSellLine(economy);
        binding.RowTexts["SelectedItemRow"] = FirstNonEmpty(FormatChannelLines(economy.SellLines.FirstOrDefault()), "未选择物品。");
        return binding;
    }

    private static WorkshopFormalV1PanelBinding BuildOrderBoardBinding(PlayerProfile player) {
        TownEconomyReadabilitySnapshot economy = BuildEconomySnapshot(player);
        TownEconomyReadabilityOrderLine selected = economy.OrderLines.FirstOrDefault();
        WorkshopFormalV1PanelBinding binding = new WorkshopFormalV1PanelBinding();
        binding.PanelTexts["OrderListPanel"] = JoinLines(
            "订单列表",
            FormatOrderLines(economy.OrderLines, 6));
        binding.PanelTexts["OrderDetailPanel"] = selected != null
            ? JoinLines("订单详情", selected.Title, selected.StatusText, selected.ProgressText, selected.DetailText)
            : "订单详情\n当前没有可展示订单。";
        binding.PanelTexts["RewardPreviewPanel"] = selected != null
            ? JoinLines("奖励预览", selected.RewardText)
            : "奖励预览\n暂无。";
        binding.PanelTexts["DeadlinePanel"] = selected != null
            ? JoinLines("期限", selected.DeadlineText)
            : "期限\n暂无。";
        binding.PanelTexts["ActionPanel"] = $"订单概览\n可接 {economy.AvailableOrderCount} / 进行中 {economy.AcceptedOrderCount} / 可交付 {economy.DeliverableOrderCount}";
        binding.RowTexts["OrderRow_Template"] = selected != null ? $"{selected.StatusText} {selected.Title}" : "暂无订单。";
        binding.RowTexts["SelectedOrderRow_Template"] = selected != null ? $"{selected.ProgressText} | {selected.RewardText}" : "未选择订单。";
        return binding;
    }

    private static WorkshopFormalV1PanelBinding BuildRumorBoardBinding(PlayerProfile player) {
        TownEconomyReadabilitySnapshot economy = BuildEconomySnapshot(player);
        TownEconomyReadabilityRumorLine selected = economy.RumorLines.FirstOrDefault();
        WorkshopFormalV1PanelBinding binding = new WorkshopFormalV1PanelBinding();
        binding.PanelTexts["TodayRumorListPanel"] = JoinLines(
            "今日传闻",
            FormatRumorLines(economy.RumorLines, 6));
        binding.PanelTexts["PriceWavePanel"] = selected != null
            ? JoinLines("价格波动", selected.StatusText, selected.TargetText)
            : "价格波动\n暂无传闻。";
        binding.PanelTexts["RumorDetailPanel"] = selected != null
            ? JoinLines("传闻详情", selected.Title, selected.DetailText)
            : "传闻详情\n暂无。";
        binding.PanelTexts["RecommendationPanel"] = JoinLines(
            "建议行动",
            FirstSellLine(economy),
            $"活跃传闻 {economy.ActiveRumorCount}");
        binding.PanelTexts["BottomHintPanel"] = JoinLines(
            "风险备注",
            FormatLines(economy.PressureLines, 3, "- "));
        binding.RowTexts["RumorRow_Template"] = selected != null ? $"{selected.Title} | {selected.StatusText}" : "暂无传闻。";
        binding.RowTexts["SelectedRumorRow_Template"] = selected != null ? selected.TargetText : "未选择传闻。";
        return binding;
    }

    private static WorkshopFormalV1PanelBinding BuildBusinessSettlementBinding(PlayerProfile player) {
        TownEconomyReadabilitySnapshot economy = BuildEconomySnapshot(player);
        WorkshopFormalV1PanelBinding binding = new WorkshopFormalV1PanelBinding();
        binding.PanelTexts["CustomerFlowPanel"] = JoinLines(
            "客流与订单",
            $"可接订单 {economy.AvailableOrderCount}",
            $"可交付订单 {economy.DeliverableOrderCount}");
        binding.PanelTexts["SaleHighlightPanel"] = JoinLines(
            "销售亮点",
            FirstSellLine(economy));
        binding.PanelTexts["RevenuePanel"] = JoinLines(
            "营收摘要",
            economy.MoneyText,
            $"最佳出售估值 {economy.TotalBestSellValue}G",
            FormatSellLines(economy.SellLines, 4));
        binding.PanelTexts["RiskSummaryPanel"] = JoinLines(
            "未售与压力",
            economy.RentPressureText,
            FormatLines(economy.PressureLines, 3, "- "));
        binding.PanelTexts["NextBillPanel"] = JoinLines(
            "下一步",
            "进入日账单前先确认可售物、订单交付和月租缺口。");
        binding.RowTexts["CustomerRow_Template"] = economy.OrderLines.FirstOrDefault() != null
            ? economy.OrderLines.First().Title
            : "暂无顾客订单。";
        binding.RowTexts["SaleRow_Template"] = FirstSellLine(economy);
        return binding;
    }

    private static WorkshopFormalV1PanelBinding BuildChassisUpgradeBinding(PlayerProfile player) {
        DollCoreStateReadabilitySnapshot doll = DollCoreStateReadabilityService.BuildSnapshot(player);
        DollEntity activeDoll = player?.ActiveDoll;
        bool canUpgrade = ChassisUpgradeService.CanUpgrade(player, activeDoll, out string chassisUpgradeReason);
        GrowthReadabilitySnapshot growth = GrowthReadabilityTextService.BuildSnapshot(player, ResolveTargetLayer(player), 6);
        List<GrowthReadabilityActionLine> crafts = growth.ActionLines
            .Where(line => line != null && line.Type == GrowthActionType.CraftProsthetic)
            .ToList();

        WorkshopFormalV1PanelBinding binding = new WorkshopFormalV1PanelBinding();
        binding.PanelTexts["CurrentChassisPanel"] = JoinLines(
            "当前底盘",
            doll.ChassisText,
            doll.StatsText);
        binding.PanelTexts["NextChassisPanel"] = JoinLines(
            "可制造义体",
            FormatGrowthActions(crafts, 5));
        binding.PanelTexts["CapacityDeltaPanel"] = JoinLines(
            "容量变化",
            "底盘升级已接入 ChassisUpgradeService；工坊义体面板已接入 ProstheticCraftingService。",
            canUpgrade ? "当前底盘可升级。" : $"当前不可升级：{chassisUpgradeReason}");
        binding.PanelTexts["MaterialNeedPanel"] = JoinLines(
            "材料缺口",
            FormatGrowthActions(crafts.Where(line => line.Status == GrowthActionStatus.MissingRequirements), 4));
        binding.PanelTexts["UpgradeActionPanel"] = JoinLines(
            "升级动作",
            $"可执行 {growth.AvailableActionCount} / 缺口 {growth.MissingRequirementActionCount}");
        binding.RowTexts["CurrentChassisRow"] = doll.ChassisText;
        binding.RowTexts["NextChassisRow"] = crafts.FirstOrDefault() != null ? crafts.First().Title : "暂无制造目标。";
        binding.RowTexts["MaterialNeedRow_Template"] = crafts.FirstOrDefault() != null ? crafts.First().CostText : "暂无材料缺口。";
        return binding;
    }

    private static WorkshopFormalV1PanelBinding BuildDollInteractionBinding(PlayerProfile player) {
        DollCoreStateReadabilitySnapshot doll = DollCoreStateReadabilityService.BuildSnapshot(player);
        DollInteractionDailyReadabilitySnapshot daily = DollInteractionReadabilityTextService.BuildDailySnapshot(player, recentLogLimit: 4);
        WorkshopFormalV1PanelBinding binding = new WorkshopFormalV1PanelBinding();
        binding.PanelTexts["DollStagePanel"] = JoinLines(
            "人偶舞台",
            doll.Name,
            doll.EmotionText,
            doll.BondStageText,
            doll.SANText);
        binding.PanelTexts["InteractionMenuPanel"] = JoinLines(
            "今日交互",
            daily.TouchCountText,
            daily.TalkCountText,
            daily.GiftCountText,
            daily.RepeatTouchText);
        binding.PanelTexts["GiftTopicListPanel"] = JoinLines(
            "礼物 / 话题",
            "赠礼选择 UI 后续接入，只读层不直接消耗物品。",
            $"最近记录 {daily.RecentLogLines.Count}");
        binding.PanelTexts["FeedbackPanel"] = JoinLines(
            "交互反馈",
            FormatLines(daily.RecentLogLines, 4, "- "));
        binding.RowTexts["TalkOptionRow_Template"] = daily.TalkCountText;
        binding.RowTexts["GiftOptionRow_Template"] = daily.GiftCountText;
        return binding;
    }

    private static WorkshopFormalV1PanelBinding BuildDollRoomBinding(PlayerProfile player) {
        DollCoreStateReadabilitySnapshot doll = DollCoreStateReadabilityService.BuildSnapshot(player);
        DollInteractionDailyReadabilitySnapshot daily = DollInteractionReadabilityTextService.BuildDailySnapshot(player, recentLogLimit: 4);
        WorkshopFormalV1PanelBinding binding = new WorkshopFormalV1PanelBinding();
        binding.PanelTexts["RoomStagePanel"] = JoinLines(
            "房间状态",
            doll.Name,
            doll.EmotionText,
            doll.BondStageText,
            doll.MaintenanceText);
        binding.PanelTexts["MementoDisplayPanel"] = JoinLines(
            "纪念物",
            FormatLines(doll.TraitLines, 5, "- "));
        binding.PanelTexts["DiaryPanel"] = JoinLines(
            "日记",
            daily.CombinedText);
        binding.PanelTexts["ObservationPanel"] = JoinLines(
            "观察",
            FormatLines(doll.WarningLines, 4, "- "));
        binding.RowTexts["MementoSlotRow_0"] = FirstNonEmpty(FirstLine(doll.TraitLines), "暂无特质 / 纪念物。");
        binding.RowTexts["DiaryRow_Template"] = FirstNonEmpty(FirstLine(daily.RecentLogLines), daily.TouchCountText);
        return binding;
    }

    private static WorkshopFormalV1PanelBinding BuildFactionShopBinding(PlayerProfile player) {
        TownEconomyReadabilitySnapshot economy = BuildEconomySnapshot(player);
        TownEconomyReadabilityFactionLine selected = economy.FactionLines.FirstOrDefault();
        WorkshopFormalV1PanelBinding binding = new WorkshopFormalV1PanelBinding();
        binding.PanelTexts["FactionListPanel"] = JoinLines(
            "势力列表",
            FormatFactionLines(economy.FactionLines, 6));
        binding.PanelTexts["StandingPanel"] = selected != null
            ? JoinLines("声望 / 信任", selected.Title, selected.StatusText, selected.DetailText)
            : "声望 / 信任\n暂无势力。";
        binding.PanelTexts["GoodsListPanel"] = JoinLines(
            "商品 / 订单入口",
            FormatOrderLines(economy.OrderLines, 4));
        binding.PanelTexts["SelectedGoodsPanel"] = JoinLines(
            "详情",
            selected != null ? selected.DetailText : "未选择势力。");
        binding.RowTexts["FactionRow_Template"] = selected != null ? $"{selected.Title} | {selected.StatusText}" : "暂无势力。";
        binding.RowTexts["GoodsRow_Template"] = economy.OrderLines.FirstOrDefault() != null
            ? economy.OrderLines.First().Title
            : "暂无商品 / 订单。";
        return binding;
    }

    private static WorkshopFormalV1PanelBinding BuildScenarioEventBinding(PlayerProfile player) {
        DollCoreStateReadabilitySnapshot doll = DollCoreStateReadabilityService.BuildSnapshot(player);
        TownEconomyReadabilitySnapshot economy = BuildEconomySnapshot(player);
        WorkshopFormalV1PanelBinding binding = new WorkshopFormalV1PanelBinding();
        binding.PanelTexts["SpeakerPanel"] = JoinLines(
            "发言者",
            doll.Name,
            doll.EmotionText);
        binding.PanelTexts["EventTextPanel"] = JoinLines(
            "事件上下文",
            economy.CalendarText,
            economy.RentPressureText,
            doll.MaintenanceText);
        binding.PanelTexts["ChoiceListPanel"] = JoinLines(
            "可选行动",
            "正式剧情事件队列尚未接入；当前展示玩家状态驱动的事件上下文。");
        binding.PanelTexts["LorePanel"] = JoinLines(
            "背景线索",
            FormatRumorLines(economy.RumorLines, 3));
        binding.PanelTexts["ActionSummaryPanel"] = JoinLines(
            "行动摘要",
            $"金币 / 压力: {economy.MoneyText}",
            $"人偶: {doll.SANText}");
        binding.RowTexts["ChoiceRow_Template"] = FirstNonEmpty(FirstLine(economy.PressureLines), "观察情况。");
        binding.RowTexts["SelectedChoiceRow_Template"] = doll.MaintenanceText;
        return binding;
    }

    private static TownEconomyReadabilitySnapshot BuildEconomySnapshot(PlayerProfile player) {
        return TownEconomyReadabilityTextService.BuildSnapshot(player);
    }

    private static int ResolveTargetLayer(PlayerProfile player) {
        if (player == null) {
            return 1;
        }

        if (player.LastSelectedDungeonStartLayer > 0) {
            return player.LastSelectedDungeonStartLayer;
        }

        return Mathf.Max(1, player.HighestUnlockedDungeonLayer);
    }

    private static GrowthReadabilityActionLine FirstAction(GrowthReadabilitySnapshot snapshot, GrowthActionType type) {
        return snapshot?.ActionLines?.FirstOrDefault(line => line != null && line.Type == type);
    }

    private static string JoinLines(params string[] lines) {
        return string.Join("\n", lines.Where(line => !string.IsNullOrEmpty(line)));
    }

    private static string FirstNonEmpty(params string[] values) {
        foreach (string value in values) {
            if (!string.IsNullOrEmpty(value)) {
                return value;
            }
        }

        return string.Empty;
    }

    private static string FirstLine(List<string> lines) {
        if (lines == null) {
            return string.Empty;
        }

        return lines.FirstOrDefault(line => !string.IsNullOrEmpty(line)) ?? string.Empty;
    }

    private static string FormatLines(List<string> lines, int limit, string prefix) {
        if (lines == null || lines.Count == 0) {
            return "暂无。";
        }

        return string.Join("\n", lines.Where(line => !string.IsNullOrEmpty(line)).Take(limit).Select(line => $"{prefix}{line}"));
    }

    private static string FormatGrowthActions(IEnumerable<GrowthReadabilityActionLine> lines, int limit) {
        if (lines == null) {
            return "暂无建议行动。";
        }

        List<string> formatted = lines
            .Where(line => line != null)
            .Take(limit)
            .Select(line => $"- {line.StatusText} {line.Title} | {line.CostText}")
            .ToList();
        return formatted.Count == 0 ? "暂无建议行动。" : string.Join("\n", formatted);
    }

    private static string FormatSellLines(List<TownEconomyReadabilitySellLine> lines, int limit) {
        if (lines == null || lines.Count == 0) {
            return "暂无可售物。";
        }

        return string.Join("\n", lines.Take(limit).Select(line => $"- {line.StatusText} {line.Name} | {line.DetailText}"));
    }

    private static string FormatOrderLines(List<TownEconomyReadabilityOrderLine> lines, int limit) {
        if (lines == null || lines.Count == 0) {
            return "暂无订单。";
        }

        return string.Join("\n", lines.Take(limit).Select(line => $"- {line.StatusText} {line.Title} | {line.ProgressText} | {line.RewardText}"));
    }

    private static string FormatRumorLines(List<TownEconomyReadabilityRumorLine> lines, int limit) {
        if (lines == null || lines.Count == 0) {
            return "暂无传闻。";
        }

        return string.Join("\n", lines.Take(limit).Select(line => $"- {line.Title} | {line.StatusText}"));
    }

    private static string FormatFactionLines(List<TownEconomyReadabilityFactionLine> lines, int limit) {
        if (lines == null || lines.Count == 0) {
            return "暂无势力。";
        }

        return string.Join("\n", lines.Take(limit).Select(line => $"- {line.Title} | {line.DetailText}"));
    }

    private static string FormatChannelLines(TownEconomyReadabilitySellLine line) {
        if (line?.ChannelLines == null || line.ChannelLines.Count == 0) {
            return "暂无渠道估值。";
        }

        return string.Join("\n", line.ChannelLines.Take(4).Select(value => $"- {value}"));
    }

    private static string FirstSellLine(TownEconomyReadabilitySnapshot economy) {
        TownEconomyReadabilitySellLine line = economy?.SellLines?.FirstOrDefault();
        if (line == null) {
            return "暂无可售物。";
        }

        return $"{line.StatusText} {line.Name} | {line.DetailText}";
    }
}
