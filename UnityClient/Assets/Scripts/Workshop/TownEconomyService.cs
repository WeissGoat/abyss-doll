using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum MonthlyRentSettlementStatus {
    None = 0,
    Paid = 1,
    LightDebtAccepted = 2,
    RequiresPawn = 3,
    HeavyDefault = 4
}

public class DailyEconomySettlementInput {
    public int DumpBoxIncome;
    public int ShowcaseIncome;
    public int OrderIncome;
    public int BlackMarketIncome;
    public int Expenses;
    public bool AdvanceCalendar = true;
    public List<string> NotableEvents = new List<string>();

    public int GrossIncome => DumpBoxIncome + ShowcaseIncome + OrderIncome + BlackMarketIncome;
}

public class DailyEconomyReport {
    public bool Success;
    public string Reason;
    public int Day;
    public int Month;
    public int MonthDay;
    public int StartingMoney;
    public int EndingMoney;
    public int GrossIncome;
    public int Expenses;
    public int NetDelta;
    public int MonthlyGrossIncome;
    public bool IsWeeklySettlement;
    public bool IsMonthlyRentDue;
    public int RentCountdown;
    public int EstimatedMonthlyBill;
    public int EstimatedMonthlyShortfall;
    public List<string> NotableEvents = new List<string>();
    public MonthlyRentSettlementReport MonthlyRentReport;
}

public class MonthlyRentSettlementReport {
    public bool Success;
    public string Reason;
    public MonthlyRentSettlementStatus Status;
    public int Month;
    public int StartingMoney;
    public int EndingMoney;
    public int BaseRent;
    public int WorkshopMaintenance;
    public int LicenseFee;
    public int DebtPrincipal;
    public int DebtInterest;
    public int TotalBill;
    public int PaidAmount;
    public int Shortfall;
    public int NewDebtAmount;
    public int PawnIncome;
    public List<PawnCandidate> PawnCandidates = new List<PawnCandidate>();
    public List<PawnCandidate> PawnedItems = new List<PawnCandidate>();
}

public class PawnCandidate {
    public ItemEntity Item;
    public string ItemID;
    public string Name;
    public int BaseValue;
    public int PawnValue;
    public string SourceContainer;
}

public class WeeklyEconomyRefreshReport {
    public bool Success;
    public string Reason;
    public int Week;
    public List<ActiveRumorState> ActiveRumors = new List<ActiveRumorState>();
    public List<OrderInstanceState> AvailableOrders = new List<OrderInstanceState>();
    public List<string> Logs = new List<string>();
}

public class OrderAcceptReport {
    public bool Success;
    public string Reason;
    public OrderInstanceState OrderInstance;
}

public class OrderDeliveryReport {
    public bool Success;
    public string Reason;
    public OrderInstanceState OrderInstance;
    public string OrderID;
    public string FactionID;
    public int GoldReward;
    public int RewardMoney;
    public int ReputationDelta;
    public int TrustDelta;
    public int NewReputationValue;
    public int NewTrustValue;
    public List<ItemEntity> DeliveredItems = new List<ItemEntity>();
    public List<ItemEntity> GeneratedRewardItems = new List<ItemEntity>();
    public List<string> Logs = new List<string>();
}

public class EconomySellLine {
    public ItemEntity Item;
    public string ItemID;
    public string Name;
    public int BaseValue;
    public int FinalValue;
    public float Multiplier = 1f;
    public List<string> AppliedRumorIDs = new List<string>();
}

public class EconomySellReport {
    public bool Success = true;
    public string Reason;
    public string Channel;
    public int StartingMoney;
    public int EndingMoney;
    public int TotalIncome;
    public List<EconomySellLine> SoldItems = new List<EconomySellLine>();
    public List<string> FailedItems = new List<string>();
}

internal class OwnedEconomyItemSlot {
    public ItemEntity Item;
    public string SourceContainer;
}

public static class TownEconomyService {
    public const string DefaultEconomyConfigID = "economy_town_v1";

    public static DailyEconomyReport SettleDailyBusiness(
        PlayerProfile player,
        DailyEconomySettlementInput input,
        string economyConfigID = DefaultEconomyConfigID) {
        input = input ?? new DailyEconomySettlementInput();
        DailyEconomyReport report = new DailyEconomyReport();

        if (!TryResolveContext(player, economyConfigID, out EconomyConfig config, out string reason)) {
            report.Success = false;
            report.Reason = reason;
            return report;
        }

        NormalizeCalendar(player);

        report.Success = true;
        report.Day = player.CurrentDay;
        report.Month = player.CurrentMonth;
        report.MonthDay = player.CurrentMonthDay;
        report.StartingMoney = player.Money;
        report.GrossIncome = Mathf.Max(0, input.GrossIncome);
        report.Expenses = Mathf.Max(0, input.Expenses);
        report.NetDelta = report.GrossIncome - report.Expenses;
        report.NotableEvents.AddRange(input.NotableEvents ?? new List<string>());

        player.Money = Mathf.Max(0, player.Money + report.NetDelta);
        player.MonthlyGrossIncome += report.GrossIncome;
        player.LastDailyEconomySettlementDay = player.CurrentDay;

        report.IsWeeklySettlement = config.WeekLength > 0 && player.CurrentMonthDay % config.WeekLength == 0;
        report.IsMonthlyRentDue = player.CurrentMonthDay >= config.MonthLength;
        report.EstimatedMonthlyBill = CalculateMonthlyBill(player, config, out _, out _, out _, out _, out _);
        report.EstimatedMonthlyShortfall = Mathf.Max(0, report.EstimatedMonthlyBill - player.Money);
        report.MonthlyGrossIncome = player.MonthlyGrossIncome;

        if (report.IsMonthlyRentDue) {
            report.MonthlyRentReport = ResolveMonthlyRent(player, null, economyConfigID);
            report.Success = report.MonthlyRentReport.Success || report.MonthlyRentReport.Status == MonthlyRentSettlementStatus.RequiresPawn;
            if (!string.IsNullOrEmpty(report.MonthlyRentReport.Reason)) {
                report.Reason = report.MonthlyRentReport.Reason;
            }

            if (report.MonthlyRentReport.Status == MonthlyRentSettlementStatus.RequiresPawn) {
                report.EndingMoney = player.Money;
                report.RentCountdown = 0;
                return report;
            }
        }

        if (input.AdvanceCalendar) {
            AdvanceCalendar(player, config);
        }

        report.EndingMoney = player.Money;
        report.RentCountdown = CalculateRentCountdown(player, config);
        return report;
    }

    public static MonthlyRentSettlementReport ResolveMonthlyRent(
        PlayerProfile player,
        IEnumerable<ItemEntity> selectedPawnItems = null,
        string economyConfigID = DefaultEconomyConfigID) {
        MonthlyRentSettlementReport report = new MonthlyRentSettlementReport();
        if (!TryResolveContext(player, economyConfigID, out EconomyConfig config, out string reason)) {
            report.Success = false;
            report.Reason = reason;
            return report;
        }

        NormalizeCalendar(player);
        report.Month = player.CurrentMonth;
        report.StartingMoney = player.Money;
        report.TotalBill = CalculateMonthlyBill(
            player,
            config,
            out int baseRent,
            out int workshopMaintenance,
            out int licenseFee,
            out int debtPrincipal,
            out int debtInterest);
        report.BaseRent = baseRent;
        report.WorkshopMaintenance = workshopMaintenance;
        report.LicenseFee = licenseFee;
        report.DebtPrincipal = debtPrincipal;
        report.DebtInterest = debtInterest;

        List<ItemEntity> pawnItems = selectedPawnItems?.Where(item => item != null).Distinct().ToList();
        if (pawnItems != null && pawnItems.Count > 0) {
            foreach (ItemEntity item in pawnItems) {
                if (!TryPawnItem(player, item, config, out PawnCandidate pawnedItem, out string pawnReason)) {
                    report.Success = false;
                    report.Status = MonthlyRentSettlementStatus.RequiresPawn;
                    report.Reason = pawnReason;
                    report.EndingMoney = player.Money;
                    report.PawnCandidates = BuildPawnCandidates(player, config);
                    return report;
                }

                report.PawnedItems.Add(pawnedItem);
                report.PawnIncome += pawnedItem.PawnValue;
            }
        }

        if (player.Money >= report.TotalBill) {
            player.Money -= report.TotalBill;
            player.EconomyDebtAmount = 0;
            player.DebtLevel = 0;
            player.ConsecutiveHeavyDefaultCount = 0;
            player.HasPendingMonthlyRent = false;
            player.PendingMonthlyBillAmount = 0;
            player.PendingMonthlyRentDay = 0;
            player.MonthlyGrossIncome = 0;

            report.Success = true;
            report.Status = MonthlyRentSettlementStatus.Paid;
            report.PaidAmount = report.TotalBill;
            report.EndingMoney = player.Money;
            return report;
        }

        int shortfall = report.TotalBill - player.Money;
        report.Shortfall = shortfall;
        float lightDebtLimit = report.TotalBill * Mathf.Clamp01(config.LightDebtThresholdRatio);
        if (shortfall <= Mathf.CeilToInt(lightDebtLimit)) {
            AcceptDebt(player, shortfall, false);
            report.Success = true;
            report.Status = MonthlyRentSettlementStatus.LightDebtAccepted;
            report.PaidAmount = report.StartingMoney + report.PawnIncome;
            report.NewDebtAmount = player.EconomyDebtAmount;
            report.EndingMoney = player.Money;
            return report;
        }

        report.PawnCandidates = BuildPawnCandidates(player, config);
        int candidateValue = report.PawnCandidates.Sum(candidate => candidate.PawnValue);
        if ((pawnItems == null || pawnItems.Count == 0) && candidateValue > 0) {
            player.HasPendingMonthlyRent = true;
            player.PendingMonthlyBillAmount = report.TotalBill;
            player.PendingMonthlyRentDay = player.CurrentDay;

            report.Success = false;
            report.Status = MonthlyRentSettlementStatus.RequiresPawn;
            report.Reason = "Monthly rent requires player-selected pawn items.";
            report.EndingMoney = player.Money;
            return report;
        }

        AcceptDebt(player, shortfall, true);
        report.Success = true;
        report.Status = MonthlyRentSettlementStatus.HeavyDefault;
        report.Reason = "Monthly rent could not be paid after available pawn resolution.";
        report.PaidAmount = report.StartingMoney + report.PawnIncome;
        report.NewDebtAmount = player.EconomyDebtAmount;
        report.EndingMoney = player.Money;
        return report;
    }

    public static List<PawnCandidate> BuildPawnCandidates(PlayerProfile player, string economyConfigID = DefaultEconomyConfigID) {
        if (!TryResolveContext(player, economyConfigID, out EconomyConfig config, out _)) {
            return new List<PawnCandidate>();
        }

        return BuildPawnCandidates(player, config);
    }

    public static int CalculateMonthlyBill(PlayerProfile player, string economyConfigID = DefaultEconomyConfigID) {
        if (!TryResolveContext(player, economyConfigID, out EconomyConfig config, out _)) {
            return 0;
        }

        return CalculateMonthlyBill(player, config, out _, out _, out _, out _, out _);
    }

    public static WeeklyEconomyRefreshReport RefreshWeeklyEconomy(
        PlayerProfile player,
        int runSeed = 0,
        string economyConfigID = DefaultEconomyConfigID) {
        WeeklyEconomyRefreshReport report = new WeeklyEconomyRefreshReport();
        if (!TryResolveContext(player, economyConfigID, out EconomyConfig config, out string reason)) {
            report.Success = false;
            report.Reason = reason;
            return report;
        }

        NormalizeEconomicState(player);
        int week = CalculateCurrentWeek(player, config);
        report.Week = week;

        if (player.LastEconomyRefreshWeek == week) {
            report.Success = true;
            report.ActiveRumors.AddRange(GetActiveRumors(player));
            report.AvailableOrders.AddRange(player.ActiveOrders.Where(IsAvailableOrder));
            report.Logs.Add($"Economy week [{week}] already refreshed.");
            return report;
        }

        player.ActiveRumors.RemoveAll(rumor => rumor == null || rumor.ExpireDay < player.CurrentDay || !ConfigManager.Rumors.ContainsKey(rumor.RumorID));
        player.ActiveOrders.RemoveAll(order => order == null || IsAvailableOrder(order));

        RollWeeklyRumors(player, config, week, runSeed, report);
        RollWeeklyOrders(player, config, week, runSeed, report);

        player.LastEconomyRefreshWeek = week;
        report.Success = true;
        report.ActiveRumors.AddRange(GetActiveRumors(player));
        report.AvailableOrders.AddRange(player.ActiveOrders.Where(IsAvailableOrder));
        return report;
    }

    public static OrderAcceptReport AcceptOrder(PlayerProfile player, string orderInstanceID) {
        OrderAcceptReport report = new OrderAcceptReport();
        if (player == null) {
            report.Reason = "Player profile is missing.";
            return report;
        }

        NormalizeEconomicState(player);
        OrderInstanceState instance = player.ActiveOrders.FirstOrDefault(order => order != null && order.InstanceID == orderInstanceID);
        if (instance == null) {
            report.Reason = $"Order instance [{orderInstanceID}] is missing.";
            return report;
        }

        if (!Enum.TryParse(instance.Status, true, out EconomyOrderStatus status) || status != EconomyOrderStatus.Available) {
            report.Reason = $"Order instance [{orderInstanceID}] is not available.";
            return report;
        }

        if (!ConfigManager.Orders.TryGetValue(instance.OrderID, out OrderConfig config)) {
            report.Reason = $"Order config [{instance.OrderID}] is missing.";
            return report;
        }

        FactionRuntimeState factionState = GetOrCreateFactionState(player, instance.FactionID);
        int activeCount = player.ActiveOrders.Count(order => order != null
            && order.FactionID == instance.FactionID
            && (IsAcceptedOrder(order) || IsInProgressOrder(order)));
        int maxActive = ConfigManager.Factions.TryGetValue(instance.FactionID, out FactionConfig factionConfig)
            ? Mathf.Max(1, factionConfig.MaxActiveOrders)
            : 3;

        if (activeCount >= maxActive) {
            report.Reason = $"Faction [{instance.FactionID}] active order limit reached.";
            return report;
        }

        instance.Status = EconomyOrderStatus.Accepted.ToString();
        instance.AcceptedDay = Mathf.Max(1, player.CurrentDay);
        instance.DeadlineDay = instance.AcceptedDay + Mathf.Max(1, config.DeadlineDays);
        factionState.FactionID = instance.FactionID;

        report.Success = true;
        report.OrderInstance = instance;
        return report;
    }

    public static OrderDeliveryReport DeliverOrder(PlayerProfile player, string orderInstanceID, IEnumerable<ItemEntity> selectedItems) {
        OrderDeliveryReport report = new OrderDeliveryReport();
        if (player == null) {
            report.Reason = "Player profile is missing.";
            return report;
        }

        NormalizeEconomicState(player);
        OrderInstanceState instance = player.ActiveOrders.FirstOrDefault(order => order != null && order.InstanceID == orderInstanceID);
        if (instance == null) {
            report.Reason = $"Order instance [{orderInstanceID}] is missing.";
            return report;
        }

        if (!ConfigManager.Orders.TryGetValue(instance.OrderID, out OrderConfig orderConfig)) {
            report.Reason = $"Order config [{instance.OrderID}] is missing.";
            return report;
        }

        report.OrderInstance = instance;
        report.OrderID = orderConfig.OrderID;
        report.FactionID = orderConfig.FactionID;

        if (!IsAcceptedOrder(instance) && !IsInProgressOrder(instance)) {
            report.Reason = $"Order instance [{orderInstanceID}] cannot be delivered from status [{instance.Status}].";
            return report;
        }

        if (instance.DeadlineDay > 0 && player.CurrentDay > instance.DeadlineDay) {
            ExpireOrder(player, instance, orderConfig);
            report.Reason = $"Order instance [{orderInstanceID}] is expired.";
            return report;
        }

        List<ItemEntity> items = selectedItems?.Where(item => item != null).Distinct().ToList() ?? new List<ItemEntity>();
        if (!TrySelectDeliveryItems(orderConfig, items, out List<ItemEntity> deliveredItems, out string deliveryReason)) {
            report.Reason = deliveryReason;
            return report;
        }

        foreach (ItemEntity item in deliveredItems) {
            if (!TryRemoveOwnedItem(player, item, true, out string removeReason)) {
                report.Reason = removeReason;
                return report;
            }

            instance.DeliveredItemInstanceIDs.Add(item.InstanceID);
            report.DeliveredItems.Add(item);
        }

        int rewardMoney = GrantOrderReward(player, orderConfig, report);
        FactionRuntimeState factionState = GetOrCreateFactionState(player, orderConfig.FactionID);
        factionState.ReputationValue = Mathf.Max(0, factionState.ReputationValue + orderConfig.ReputationDelta);
        factionState.TrustValue = Mathf.Max(0, factionState.TrustValue + orderConfig.TrustDelta);
        if (!factionState.CompletedOrderIDs.Contains(orderConfig.OrderID)) {
            factionState.CompletedOrderIDs.Add(orderConfig.OrderID);
        }

        player.Money += Mathf.Max(0, orderConfig.FixedGold);
        instance.Status = EconomyOrderStatus.Completed.ToString();

        report.Success = true;
        report.GoldReward = Mathf.Max(0, orderConfig.FixedGold);
        report.RewardMoney = rewardMoney;
        report.ReputationDelta = orderConfig.ReputationDelta;
        report.TrustDelta = orderConfig.TrustDelta;
        report.NewReputationValue = factionState.ReputationValue;
        report.NewTrustValue = factionState.TrustValue;
        report.Logs.Add($"Order [{orderConfig.OrderID}] completed.");
        return report;
    }

    public static EconomySellReport SellItems(PlayerProfile player, IEnumerable<ItemEntity> selectedItems, EconomySellChannel channel) {
        EconomySellReport report = new EconomySellReport {
            Channel = channel.ToString()
        };

        if (player == null) {
            report.Success = false;
            report.Reason = "Player profile is missing.";
            return report;
        }

        NormalizeEconomicState(player);
        report.StartingMoney = player.Money;
        List<ItemEntity> items = selectedItems?.Where(item => item != null).Distinct().ToList() ?? new List<ItemEntity>();
        foreach (ItemEntity item in items) {
            EconomySellLine line = CalculateItemSellValue(player, item, channel);
            if (line.FinalValue <= 0) {
                report.Success = false;
                report.FailedItems.Add($"{item.Name}: no sell value.");
                continue;
            }

            if (!CanSellInChannel(item, channel, out string blockReason)) {
                report.Success = false;
                report.FailedItems.Add($"{item.Name}: {blockReason}");
                continue;
            }

            if (!TryRemoveOwnedItem(player, item, true, out string removeReason)) {
                report.Success = false;
                report.FailedItems.Add($"{item.Name}: {removeReason}");
                continue;
            }

            player.Money += line.FinalValue;
            report.TotalIncome += line.FinalValue;
            report.SoldItems.Add(line);
        }

        report.EndingMoney = player.Money;
        if (!report.Success && string.IsNullOrEmpty(report.Reason)) {
            report.Reason = "One or more items could not be sold.";
        }

        return report;
    }

    public static EconomySellLine CalculateItemSellValue(PlayerProfile player, ItemEntity item, EconomySellChannel channel) {
        EconomySellLine line = new EconomySellLine {
            Item = item,
            ItemID = item?.ConfigID,
            Name = item?.Name,
            BaseValue = Mathf.Max(0, item?.BaseValue ?? 0)
        };

        if (player == null || item == null || line.BaseValue <= 0) {
            line.FinalValue = 0;
            return line;
        }

        float multiplier = 1f;
        foreach (ActiveRumorState activeRumor in GetActiveRumors(player)) {
            if (!ConfigManager.Rumors.TryGetValue(activeRumor.RumorID, out RumorConfig rumor)) {
                continue;
            }

            if (!RumorAppliesToItem(rumor, item, channel)) {
                continue;
            }

            multiplier *= Mathf.Max(0.01f, rumor.PriceMultiplier);
            line.AppliedRumorIDs.Add(rumor.RumorID);
        }

        line.Multiplier = multiplier;
        line.FinalValue = Mathf.Max(0, Mathf.RoundToInt(line.BaseValue * multiplier));
        return line;
    }

    private static void NormalizeEconomicState(PlayerProfile player) {
        if (player == null) {
            return;
        }

        NormalizeCalendar(player);
        if (player.FactionStates == null) {
            player.FactionStates = new List<FactionRuntimeState>();
        }

        if (player.ActiveOrders == null) {
            player.ActiveOrders = new List<OrderInstanceState>();
        }

        if (player.ActiveRumors == null) {
            player.ActiveRumors = new List<ActiveRumorState>();
        }
    }

    private static int CalculateCurrentWeek(PlayerProfile player, EconomyConfig config) {
        int weekLength = Mathf.Max(1, config?.WeekLength ?? 7);
        int currentDay = Mathf.Max(1, player?.CurrentDay ?? 1);
        return ((currentDay - 1) / weekLength) + 1;
    }

    private static List<ActiveRumorState> GetActiveRumors(PlayerProfile player) {
        if (player?.ActiveRumors == null) {
            return new List<ActiveRumorState>();
        }

        int currentDay = Mathf.Max(1, player.CurrentDay);
        return player.ActiveRumors
            .Where(rumor => rumor != null
                && rumor.StartDay <= currentDay
                && rumor.ExpireDay >= currentDay
                && ConfigManager.Rumors.ContainsKey(rumor.RumorID))
            .ToList();
    }

    private static void RollWeeklyRumors(
        PlayerProfile player,
        EconomyConfig config,
        int week,
        int runSeed,
        WeeklyEconomyRefreshReport report) {
        List<RumorConfig> candidates = ConfigManager.Rumors.Values
            .Where(rumor => rumor != null && rumor.Weight > 0 && rumor.DurationDays > 0)
            .OrderBy(rumor => rumor.RumorID)
            .ToList();
        if (candidates.Count == 0) {
            report.Logs.Add("No rumor config available for weekly refresh.");
            return;
        }

        int seed = BuildDeterministicSeed(runSeed, week, 17);
        RumorConfig selected = PickWeighted(candidates, rumor => rumor.Weight, seed);
        if (selected == null) {
            report.Logs.Add("No positive-weight rumor selected.");
            return;
        }

        player.ActiveRumors.Add(new ActiveRumorState {
            RumorID = selected.RumorID,
            StartDay = Mathf.Max(1, player.CurrentDay),
            ExpireDay = Mathf.Max(1, player.CurrentDay) + Mathf.Max(1, selected.DurationDays) - 1,
            LogSeed = seed
        });
        report.Logs.Add($"Rumor [{selected.RumorID}] activated for week [{week}].");
    }

    private static void RollWeeklyOrders(
        PlayerProfile player,
        EconomyConfig config,
        int week,
        int runSeed,
        WeeklyEconomyRefreshReport report) {
        List<FactionConfig> factions = ConfigManager.Factions.Values
            .Where(faction => faction != null && !string.IsNullOrEmpty(faction.FactionID))
            .OrderBy(faction => faction.FactionID)
            .ToList();
        if (factions.Count == 0) {
            report.Logs.Add("No faction config available for weekly order refresh.");
            return;
        }

        foreach (FactionConfig faction in factions) {
            FactionRuntimeState factionState = GetOrCreateFactionState(player, faction.FactionID);
            if (factionState.CooldownUntilDay > 0 && factionState.CooldownUntilDay > player.CurrentDay) {
                report.Logs.Add($"Faction [{faction.FactionID}] skipped by cooldown until day [{factionState.CooldownUntilDay}].");
                continue;
            }

            List<OrderConfig> candidates = ConfigManager.Orders.Values
                .Where(order => IsOrderConfigAvailable(player, order, factionState, faction.FactionID))
                .OrderBy(order => order.OrderID)
                .ToList();
            if (candidates.Count == 0) {
                report.Logs.Add($"Faction [{faction.FactionID}] has no available order candidates.");
                continue;
            }

            int slots = Mathf.Max(1, faction.VisibleOrderSlots);
            HashSet<string> usedOrderIDs = new HashSet<string>();
            for (int slot = 0; slot < slots; slot++) {
                List<OrderConfig> slotCandidates = candidates
                    .Where(order => !usedOrderIDs.Contains(order.OrderID))
                    .ToList();
                if (slotCandidates.Count == 0) {
                    break;
                }

                int seed = BuildDeterministicSeed(runSeed, week, 101 + slot + usedOrderIDs.Count + faction.FactionID.Length);
                OrderConfig selected = PickWeighted(slotCandidates, order => ResolveOrderWeight(player, order), seed);
                if (selected == null) {
                    break;
                }

                usedOrderIDs.Add(selected.OrderID);
                player.ActiveOrders.Add(new OrderInstanceState {
                    InstanceID = $"{selected.OrderID}_w{week}_s{slot}",
                    OrderID = selected.OrderID,
                    FactionID = selected.FactionID,
                    Status = EconomyOrderStatus.Available.ToString(),
                    GeneratedDay = Mathf.Max(1, player.CurrentDay),
                    LogSeed = seed
                });
                report.Logs.Add($"Order [{selected.OrderID}] generated for faction [{faction.FactionID}].");
            }
        }
    }

    private static bool IsActiveOrder(OrderInstanceState order) {
        return IsAvailableOrder(order) || IsAcceptedOrder(order) || IsInProgressOrder(order);
    }

    private static bool IsAvailableOrder(OrderInstanceState order) {
        return HasOrderStatus(order, EconomyOrderStatus.Available);
    }

    private static bool IsAcceptedOrder(OrderInstanceState order) {
        return HasOrderStatus(order, EconomyOrderStatus.Accepted);
    }

    private static bool IsInProgressOrder(OrderInstanceState order) {
        return HasOrderStatus(order, EconomyOrderStatus.InProgress);
    }

    private static bool HasOrderStatus(OrderInstanceState order, EconomyOrderStatus expected) {
        return order != null
            && Enum.TryParse(order.Status, true, out EconomyOrderStatus status)
            && status == expected;
    }

    private static bool IsOrderConfigAvailable(PlayerProfile player, OrderConfig order, FactionRuntimeState factionState, string factionID) {
        if (order == null
            || string.IsNullOrEmpty(order.OrderID)
            || !string.Equals(order.FactionID, factionID, StringComparison.OrdinalIgnoreCase)
            || order.Weight <= 0) {
            return false;
        }

        if (order.Requirement != null
            && order.Requirement.MinLayer > 0
            && (player?.HighestUnlockedDungeonLayer ?? 1) < order.Requirement.MinLayer) {
            return false;
        }

        bool onceOnly = string.Equals(order.RepeatPolicy, "Once", StringComparison.OrdinalIgnoreCase)
            || string.Equals(order.RepeatPolicy, "Unique", StringComparison.OrdinalIgnoreCase);
        return !onceOnly
            || factionState == null
            || factionState.CompletedOrderIDs == null
            || !factionState.CompletedOrderIDs.Contains(order.OrderID);
    }

    private static int ResolveOrderWeight(PlayerProfile player, OrderConfig order) {
        if (order == null) {
            return 0;
        }

        int weight = Mathf.Max(0, order.Weight);
        foreach (ActiveRumorState activeRumor in GetActiveRumors(player)) {
            if (!ConfigManager.Rumors.TryGetValue(activeRumor.RumorID, out RumorConfig rumor)
                || rumor.BoostedOrderTags == null
                || rumor.BoostedOrderTags.Count == 0
                || order.Tags == null) {
                continue;
            }

            if (order.Tags.Any(tag => rumor.BoostedOrderTags.Any(boostedTag => string.Equals(tag, boostedTag, StringComparison.OrdinalIgnoreCase)))) {
                weight += Mathf.Max(1, order.Weight);
            }
        }

        return weight;
    }

    private static FactionRuntimeState GetOrCreateFactionState(PlayerProfile player, string factionID) {
        NormalizeEconomicState(player);
        FactionRuntimeState state = player.FactionStates.FirstOrDefault(faction =>
            faction != null && string.Equals(faction.FactionID, factionID, StringComparison.OrdinalIgnoreCase));
        if (state != null) {
            return state;
        }

        state = new FactionRuntimeState {
            FactionID = factionID
        };
        player.FactionStates.Add(state);
        return state;
    }

    private static void ExpireOrder(PlayerProfile player, OrderInstanceState instance, OrderConfig config) {
        if (instance == null || config == null) {
            return;
        }

        instance.Status = EconomyOrderStatus.Expired.ToString();
        instance.FailureReason = "Deadline expired.";

        FactionRuntimeState factionState = GetOrCreateFactionState(player, config.FactionID);
        factionState.FailedOrderCount++;
        if (config.FailurePenalty != null) {
            factionState.ReputationValue = Mathf.Max(0, factionState.ReputationValue + config.FailurePenalty.ExpiredReputationDelta);
            if (config.FailurePenalty.CooldownDays > 0) {
                factionState.CooldownUntilDay = Mathf.Max(factionState.CooldownUntilDay, player.CurrentDay + config.FailurePenalty.CooldownDays);
            }
        }
    }

    private static bool TrySelectDeliveryItems(
        OrderConfig orderConfig,
        List<ItemEntity> selectedItems,
        out List<ItemEntity> deliveredItems,
        out string reason) {
        deliveredItems = new List<ItemEntity>();
        reason = string.Empty;

        if (orderConfig == null) {
            reason = "Order config is missing.";
            return false;
        }

        OrderRequirementConfig requirement = orderConfig.Requirement ?? new OrderRequirementConfig();
        int requiredCount = Mathf.Max(1, requirement.RequiredCount);
        foreach (ItemEntity item in selectedItems ?? new List<ItemEntity>()) {
            if (ItemMatchesRequirement(item, requirement)) {
                deliveredItems.Add(item);
                if (deliveredItems.Count >= requiredCount) {
                    return true;
                }
            }
        }

        reason = $"Order [{orderConfig.OrderID}] requires [{requiredCount}] matching item(s), selected [{deliveredItems.Count}].";
        return false;
    }

    private static bool ItemMatchesRequirement(ItemEntity item, OrderRequirementConfig requirement) {
        if (item == null || requirement == null) {
            return false;
        }

        if (requirement.RequiredItemIDs != null
            && requirement.RequiredItemIDs.Count > 0
            && !requirement.RequiredItemIDs.Any(requiredID => string.Equals(requiredID, item.ConfigID, StringComparison.OrdinalIgnoreCase))) {
            return false;
        }

        List<string> tags = CollectItemTags(item);
        if (requirement.RequiredTags != null
            && requirement.RequiredTags.Count > 0
            && !requirement.RequiredTags.All(requiredTag => tags.Any(tag => string.Equals(tag, requiredTag, StringComparison.OrdinalIgnoreCase)))) {
            return false;
        }

        if (requirement.ForbiddenTags != null
            && requirement.ForbiddenTags.Any(forbiddenTag => tags.Any(tag => string.Equals(tag, forbiddenTag, StringComparison.OrdinalIgnoreCase)))) {
            return false;
        }

        return item.Grid == null || requirement.MinGridCost <= 0 || item.Grid.GridCost >= requirement.MinGridCost;
    }

    private static bool TryRemoveOwnedItem(PlayerProfile player, ItemEntity item, bool recalculateGrid, out string reason) {
        reason = string.Empty;
        if (player == null || item == null) {
            reason = "Player or item is missing.";
            return false;
        }

        if (player.StashInventory != null && player.StashInventory.Remove(item)) {
            ClearItemGridPosition(item);
            return true;
        }

        BackpackGrid grid = player.ActiveDoll?.RuntimeGrid as BackpackGrid;
        if (grid == null || !grid.ContainedItems.Contains(item)) {
            reason = $"Item [{item.Name}] is not owned by the player.";
            return false;
        }

        grid.RemoveItem(item);
        ClearItemGridPosition(item);
        if (!string.IsNullOrEmpty(item.InstanceID)) {
            GameEventBus.PublishItemRemoved(item.InstanceID);
        }

        if (recalculateGrid && player.ActiveDoll != null) {
            GridSolver.RecalculateAllEffects(player.ActiveDoll);
        }

        return true;
    }

    private static int GrantOrderReward(PlayerProfile player, OrderConfig orderConfig, OrderDeliveryReport report) {
        if (player == null || orderConfig == null || string.IsNullOrEmpty(orderConfig.RewardID)) {
            return 0;
        }

        RewardRollResult result = new RewardSystem().Roll(orderConfig.RewardID, new RewardContext {
            SourceType = "Order",
            SourceID = orderConfig.OrderID,
            Player = player,
            ActiveDoll = player.ActiveDoll
        });

        int money = Mathf.Max(0, result.Money);
        player.Money += money;
        if (result.GeneratedItems != null) {
            foreach (ItemEntity item in result.GeneratedItems) {
                if (item == null) {
                    continue;
                }

                player.StashInventory.Add(item);
                report.GeneratedRewardItems.Add(item);
            }
        }

        if (result.Logs != null) {
            report.Logs.AddRange(result.Logs);
        }

        return money;
    }

    private static bool CanSellInChannel(ItemEntity item, EconomySellChannel channel, out string reason) {
        reason = string.Empty;
        if (item == null) {
            reason = "Item is missing.";
            return false;
        }

        if (item.BaseValue <= 0) {
            reason = "Item has no sell value.";
            return false;
        }

        return true;
    }

    private static bool RumorAppliesToItem(RumorConfig rumor, ItemEntity item, EconomySellChannel channel) {
        if (rumor == null || item == null) {
            return false;
        }

        if (!string.IsNullOrEmpty(rumor.Channel)
            && Enum.TryParse(rumor.Channel, true, out EconomySellChannel rumorChannel)
            && rumorChannel != channel) {
            return false;
        }

        if (rumor.TargetTags == null || rumor.TargetTags.Count == 0) {
            return true;
        }

        List<string> tags = CollectItemTags(item);
        return rumor.TargetTags.Any(targetTag => tags.Any(tag => string.Equals(tag, targetTag, StringComparison.OrdinalIgnoreCase)));
    }

    private static List<string> CollectItemTags(ItemEntity item) {
        List<string> tags = new List<string>();
        if (item?.Tags != null) {
            tags.AddRange(item.Tags.Where(tag => !string.IsNullOrEmpty(tag)));
        }

        List<string> dynamicTags = GetDynamicTags(item);
        if (dynamicTags != null) {
            tags.AddRange(dynamicTags.Where(tag => !string.IsNullOrEmpty(tag)));
        }

        return tags;
    }

    private static void ClearItemGridPosition(ItemEntity item) {
        if (item?.Grid?.CurrentPos == null || item.Grid.CurrentPos.Length < 2) {
            return;
        }

        item.Grid.CurrentPos[0] = -1;
        item.Grid.CurrentPos[1] = -1;
    }

    private static int BuildDeterministicSeed(int runSeed, int week, int salt) {
        unchecked {
            int seed = 17;
            seed = seed * 31 + runSeed;
            seed = seed * 31 + week;
            seed = seed * 31 + salt;
            return seed == int.MinValue ? int.MaxValue : Mathf.Abs(seed);
        }
    }

    private static T PickWeighted<T>(List<T> candidates, Func<T, int> weightSelector, int seed) where T : class {
        if (candidates == null || candidates.Count == 0) {
            return null;
        }

        int totalWeight = candidates.Sum(candidate => Mathf.Max(0, weightSelector(candidate)));
        if (totalWeight <= 0) {
            return null;
        }

        System.Random random = new System.Random(seed);
        int roll = random.Next(0, totalWeight);
        int cursor = 0;
        foreach (T candidate in candidates) {
            cursor += Mathf.Max(0, weightSelector(candidate));
            if (roll < cursor) {
                return candidate;
            }
        }

        return candidates[candidates.Count - 1];
    }

    private static bool TryResolveContext(PlayerProfile player, string economyConfigID, out EconomyConfig config, out string reason) {
        config = null;
        reason = string.Empty;

        if (player == null) {
            reason = "Player profile is missing.";
            return false;
        }

        string resolvedConfigID = string.IsNullOrEmpty(economyConfigID) ? DefaultEconomyConfigID : economyConfigID;
        if (!ConfigManager.EconomyConfigs.TryGetValue(resolvedConfigID, out config)) {
            reason = $"Economy config [{resolvedConfigID}] is missing.";
            return false;
        }

        return true;
    }

    private static int CalculateMonthlyBill(
        PlayerProfile player,
        EconomyConfig config,
        out int baseRent,
        out int workshopMaintenance,
        out int licenseFee,
        out int debtPrincipal,
        out int debtInterest) {
        int month = Mathf.Max(1, player?.CurrentMonth ?? 1);
        baseRent = ResolveBaseRent(config, month);
        workshopMaintenance = Mathf.Max(0, config.WorkshopMaintenanceBase);
        licenseFee = Mathf.Max(0, config.LicenseFeeBase);
        debtPrincipal = Mathf.Max(0, player?.EconomyDebtAmount ?? 0);
        debtInterest = Mathf.CeilToInt(debtPrincipal * Mathf.Max(0f, config.DebtInterestRate));
        return baseRent + workshopMaintenance + licenseFee + debtPrincipal + debtInterest;
    }

    private static int ResolveBaseRent(EconomyConfig config, int month) {
        if (config?.RentCurve == null || config.RentCurve.Count == 0) {
            return 0;
        }

        RentCurveStepConfig best = null;
        foreach (RentCurveStepConfig step in config.RentCurve) {
            if (step == null || step.Month <= 0 || step.BaseRent <= 0) {
                continue;
            }

            if (step.Month <= month && (best == null || step.Month > best.Month)) {
                best = step;
            }
        }

        if (best != null) {
            return best.BaseRent;
        }

        return config.RentCurve.Where(step => step != null && step.BaseRent > 0).OrderBy(step => step.Month).FirstOrDefault()?.BaseRent ?? 0;
    }

    private static void AcceptDebt(PlayerProfile player, int shortfall, bool heavyDefault) {
        int paid = Mathf.Min(player.Money, Mathf.Max(0, player.Money));
        player.Money -= paid;
        player.EconomyDebtAmount = Mathf.Max(0, shortfall);
        player.DebtLevel = heavyDefault ? 2 : 1;
        player.ConsecutiveHeavyDefaultCount = heavyDefault ? player.ConsecutiveHeavyDefaultCount + 1 : 0;
        player.HasPendingMonthlyRent = false;
        player.PendingMonthlyBillAmount = 0;
        player.PendingMonthlyRentDay = 0;
        player.MonthlyGrossIncome = 0;
    }

    private static List<PawnCandidate> BuildPawnCandidates(PlayerProfile player, EconomyConfig config) {
        List<PawnCandidate> candidates = new List<PawnCandidate>();
        if (player == null) {
            return candidates;
        }

        foreach (OwnedEconomyItemSlot slot in EnumerateOwnedItems(player)) {
            ItemEntity item = slot.Item;
            if (!IsPawnable(item, config)) {
                continue;
            }

            int pawnValue = CalculatePawnValue(item, config);
            if (pawnValue <= 0) {
                continue;
            }

            candidates.Add(new PawnCandidate {
                Item = item,
                ItemID = item.ConfigID,
                Name = item.Name,
                BaseValue = item.BaseValue,
                PawnValue = pawnValue,
                SourceContainer = slot.SourceContainer
            });
        }

        return candidates
            .OrderByDescending(candidate => candidate.PawnValue)
            .ThenBy(candidate => candidate.ItemID)
            .ToList();
    }

    private static IEnumerable<OwnedEconomyItemSlot> EnumerateOwnedItems(PlayerProfile player) {
        if (player?.StashInventory != null) {
            foreach (ItemEntity item in player.StashInventory) {
                yield return new OwnedEconomyItemSlot {
                    Item = item,
                    SourceContainer = "GroundInventory"
                };
            }
        }

        BackpackGrid grid = player?.ActiveDoll?.RuntimeGrid as BackpackGrid;
        if (grid == null) {
            yield break;
        }

        foreach (ItemEntity item in grid.ContainedItems) {
            yield return new OwnedEconomyItemSlot {
                Item = item,
                SourceContainer = "Backpack"
            };
        }
    }

    private static bool IsPawnable(ItemEntity item, EconomyConfig config) {
        if (item == null || IsBindingItem(item) || item.BaseValue <= 0) {
            return false;
        }

        if (string.Equals(item.ItemType, nameof(ItemType.QuestItem), StringComparison.OrdinalIgnoreCase)
            || string.Equals(item.ItemType, nameof(ItemType.Anchor), StringComparison.OrdinalIgnoreCase)) {
            return false;
        }

        return !HasProtectedTag(item.Tags, config) && !HasProtectedTag(GetDynamicTags(item), config);
    }

    private static bool HasProtectedTag(List<string> tags, EconomyConfig config) {
        if (tags == null || config?.PawnProtectedTags == null) {
            return false;
        }

        foreach (string tag in tags) {
            if (config.PawnProtectedTags.Any(protectedTag => string.Equals(tag, protectedTag, StringComparison.OrdinalIgnoreCase))) {
                return true;
            }
        }

        return false;
    }

    private static int CalculatePawnValue(ItemEntity item, EconomyConfig config) {
        if (item == null || config == null) {
            return 0;
        }

        return Mathf.Max(1, Mathf.FloorToInt(item.BaseValue * Mathf.Clamp(config.PawnValueMultiplier, 0.01f, 1f)));
    }

    private static bool TryPawnItem(PlayerProfile player, ItemEntity item, EconomyConfig config, out PawnCandidate pawnedItem, out string reason) {
        pawnedItem = null;
        reason = string.Empty;

        if (player == null || item == null) {
            reason = "Player or pawn item is missing.";
            return false;
        }

        if (!IsPawnable(item, config)) {
            reason = $"Item [{item.Name}] is protected and cannot be pawned.";
            return false;
        }

        int pawnValue = CalculatePawnValue(item, config);
        if (pawnValue <= 0) {
            reason = $"Item [{item.Name}] has no pawn value.";
            return false;
        }

        string sourceContainer = "GroundInventory";
        bool removed = player.StashInventory != null && player.StashInventory.Remove(item);
        if (!removed) {
            BackpackGrid grid = player.ActiveDoll?.RuntimeGrid as BackpackGrid;
            if (grid == null || !grid.ContainedItems.Contains(item)) {
                reason = $"Item [{item.Name}] is not in stash or backpack.";
                return false;
            }

            sourceContainer = "Backpack";
            grid.RemoveItem(item);
            if (!string.IsNullOrEmpty(item.InstanceID)) {
                GameEventBus.PublishItemRemoved(item.InstanceID);
            }

            if (player.ActiveDoll != null) {
                GridSolver.RecalculateAllEffects(player.ActiveDoll);
            }
        }

        player.Money += pawnValue;
        if (item.Grid?.CurrentPos != null && item.Grid.CurrentPos.Length >= 2) {
            item.Grid.CurrentPos[0] = -1;
            item.Grid.CurrentPos[1] = -1;
        }

        pawnedItem = new PawnCandidate {
            Item = item,
            ItemID = item.ConfigID,
            Name = item.Name,
            BaseValue = item.BaseValue,
            PawnValue = pawnValue,
            SourceContainer = sourceContainer
        };
        return true;
    }

    private static bool IsBindingItem(ItemEntity item) {
        if (item == null) {
            return false;
        }

        System.Reflection.FieldInfo bindingField = typeof(ItemEntity).GetField("IsBinding");
        return bindingField != null
            && bindingField.FieldType == typeof(bool)
            && (bool)bindingField.GetValue(item);
    }

    private static List<string> GetDynamicTags(ItemEntity item) {
        System.Reflection.FieldInfo dynamicTagsField = typeof(ItemEntity).GetField("DynamicTags");
        if (dynamicTagsField == null || !typeof(List<string>).IsAssignableFrom(dynamicTagsField.FieldType)) {
            return null;
        }

        return dynamicTagsField.GetValue(item) as List<string>;
    }

    private static void NormalizeCalendar(PlayerProfile player) {
        if (player == null) {
            return;
        }

        player.CurrentDay = Mathf.Max(1, player.CurrentDay);
        player.CurrentMonth = Mathf.Max(1, player.CurrentMonth);
        player.CurrentMonthDay = Mathf.Max(1, player.CurrentMonthDay);
    }

    private static void AdvanceCalendar(PlayerProfile player, EconomyConfig config) {
        player.CurrentDay++;
        player.CurrentMonthDay++;
        if (player.CurrentMonthDay > config.MonthLength) {
            player.CurrentMonth++;
            player.CurrentMonthDay = 1;
        }
    }

    private static int CalculateRentCountdown(PlayerProfile player, EconomyConfig config) {
        if (player == null || config == null) {
            return 0;
        }

        return Mathf.Max(0, config.MonthLength - player.CurrentMonthDay + 1);
    }
}
