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
