using System.Collections.Generic;
using UnityEngine;

// M2 batch 2 (GDD_07 Demo slice): one core choice per day (dive or rest), weekly rent at the end of each 7-day
// period, a rent the player cannot cover stays pending without stopping the calendar, HP comes back from a dive as
// it is, and repeated heavy defaults end the run.
public static class DayCycleTest {
    public static void Run() {
        Debug.Log("=== Running Day Cycle Test ===");

        List<string> failures = new List<string>();
        void Check(bool condition, string message) {
            if (!condition) {
                failures.Add(message);
            }
        }

        CoreBackend core = NewCore();
        PlayerProfile player = core.CurrentPlayer;
        DollEntity doll = player.ActiveDoll;
        EconomyConfig config = TownEconomyService.GetConfig();
        int rent1 = TownEconomyService.ResolveBaseRent(config, 1);
        Check(TownEconomyService.GetRentPeriodLength(config) == 7, $"rent period is {TownEconomyService.GetRentPeriodLength(config)} days, expected 7");

        // Calendar text.
        DayCalendarInfo day1 = DayCycleService.GetCalendar(player);
        Check(day1.Day == 1 && day1.Week == 1 && day1.WeekDayName == "星期一" && day1.RentCountdown == 7 && day1.NextRent == rent1, $"day 1 calendar: week {day1.Week} {day1.WeekDayName}, rent {day1.NextRent} in {day1.RentCountdown}");

        // Rest: a day passes and Zero recovers.
        doll.Status.HP_Current = 20;
        doll.Status.SAN_Current = 10;
        DayEndReport rest = RestDay(player, out string restReason);
        Check(rest != null && rest.Choice == DayChoice.Rest, "rest failed: " + restReason);
        Check(player.CurrentDay == 2, $"day after rest is {player.CurrentDay}");
        Check(doll.Status.HP_Current == doll.Status.HP_Max && doll.Status.SAN_Current == doll.Status.SAN_Max, $"rest left HP {doll.Status.HP_Current}, SAN {doll.Status.SAN_Current}");
        Check(rest != null && rest.HPBefore == 20 && rest.HPAfter == doll.Status.HP_Max, "rest report HP numbers wrong");
        Check(DayCycleService.TakePendingReport() == rest && DayCycleService.TakePendingReport() == null, "rest report not handed to the night card exactly once");

        // A dive brings HP back as it is; a defeat (HP 0) blocks the next dive until a rest.
        Check(core.Dungeon.StartRunAtLayer(1), "dive did not start");
        doll.Status.HP_Current = 0;
        DungeonEventBus.PublishDungeonDefeated();
        DayCycleService.FinishNight(player);
        DayEndReport dive = DayCycleService.TakePendingReport();
        Check(dive != null && dive.Choice == DayChoice.Dive && dive.DiveDefeated && dive.HPAfter == 0, "defeat day report wrong");
        Check(player.CurrentDay == 3, $"day after the dive is {player.CurrentDay}");
        Check(!DayCycleService.CanDive(player, out string hpReason) && hpReason.Contains("HP 0"), "diving with HP 0 is allowed: " + hpReason);
        Check(!core.Dungeon.StartRunAtLayer(1), "StartRunAtLayer started a run with HP 0");
        RestDay(player, out _);
        Check(DayCycleService.CanDive(player, out string afterRest), "still cannot dive after resting: " + afterRest);

        // Rent is charged on the night of day 7.
        player.Money = rent1 + 100;
        while (player.CurrentDay < 7) {
            RestDay(player, out _);
        }
        DayCalendarInfo rentDay = DayCycleService.GetCalendar(player);
        Check(rentDay.RentCountdown == 1, $"day 7 rent countdown is {rentDay.RentCountdown}");
        DayEndReport paidNight = RestDay(player, out _);
        Check(paidNight?.Rent != null && paidNight.Rent.Status == MonthlyRentSettlementStatus.Paid && player.Money == 100, $"week 1 rent: {paidNight?.Rent?.Status}, money {player.Money}");
        Check(player.CurrentDay == 8 && DayCycleService.GetCalendar(player).WeekDayName == "星期一", $"after the rent night: day {player.CurrentDay}");

        // Week 2: not enough money and something to pawn -> the bill stays pending, the day still ends.
        int rent2 = TownEconomyService.ResolveBaseRent(config, 2);
        ItemEntity pistol = ConfigManager.CreateItem("gear_charge_pistol");
        ItemLifecycleService.MarkGroundInventoryItem(player, pistol, "Test");
        player.Money = 0;
        while (player.CurrentDay < 14) {
            RestDay(player, out _);
        }
        DayEndReport pendingNight = RestDay(player, out _);
        Check(pendingNight?.Rent != null && pendingNight.Rent.Status == MonthlyRentSettlementStatus.RequiresPawn && pendingNight.RentPending, $"week 2 rent: {pendingNight?.Rent?.Status}");
        Check(player.CurrentDay == 15, $"a pending rent stopped the calendar at day {player.CurrentDay}");
        Check(player.HasPendingMonthlyRent && player.PendingMonthlyBillAmount == rent2, $"pending bill {player.PendingMonthlyBillAmount}, expected {rent2}");
        Check(!DayCycleService.CanStartDay(player, out string pendingReason) && RestDay(player, out _) == null, "a day could start with rent pending: " + pendingReason);

        // 先欠着 with nothing paid is a heavy default; a second one in a row is bankruptcy.
        MonthlyRentSettlementReport owed = TownEconomyService.AcceptPendingRentAsDebt(player);
        Check(owed.Status == MonthlyRentSettlementStatus.HeavyDefault && player.EconomyDebtAmount == rent2 && !player.HasPendingMonthlyRent, $"owing week 2: {owed.Status}, debt {player.EconomyDebtAmount}");
        Check(!TownEconomyService.IsBankrupt(player), "bankrupt after one default");
        Check(DayCycleService.CanStartDay(player, out _), "cannot start a day after owing the rent");
        while (player.CurrentDay < 21) {
            RestDay(player, out _);
        }
        RestDay(player, out _);
        if (player.HasPendingMonthlyRent) {
            TownEconomyService.AcceptPendingRentAsDebt(player);
        }
        Check(TownEconomyService.IsBankrupt(player), $"not bankrupt after two heavy defaults (count {player.ConsecutiveHeavyDefaultCount})");
        Check(!DayCycleService.CanStartDay(player, out string bankruptReason) && bankruptReason.Contains("破产"), "a bankrupt workshop can still start a day");

        // Pawning the selected item pays a pending bill.
        core = NewCore();
        player = core.CurrentPlayer;
        player.StashInventory.Clear();
        ItemEntity pawnItem = ConfigManager.CreateItem("gear_charge_pistol");
        ItemLifecycleService.MarkGroundInventoryItem(player, pawnItem, "Test");
        int pawnValue = TownEconomyService.BuildPawnCandidates(player).Find(candidate => candidate.Item == pawnItem)?.PawnValue ?? 0;
        player.Money = rent1 - pawnValue + 5;
        while (player.CurrentDay < 7) {
            RestDay(player, out _);
        }
        RestDay(player, out _);
        bool pendingBeforePawn = player.HasPendingMonthlyRent;
        MonthlyRentSettlementReport pawned = TownEconomyService.ResolveMonthlyRent(player, new[] { pawnItem });
        Check(pendingBeforePawn && pawned.Status == MonthlyRentSettlementStatus.Paid && player.Money == 5 && !player.HasPendingMonthlyRent && !player.StashInventory.Contains(pawnItem),
            $"pawn to pay: pending={pendingBeforePawn}, {pawned.Status}, money {player.Money}");

        // Night card text names the choice and the rent.
        string night = TownDayHUD.BuildNightText(new DayEndReport { Choice = DayChoice.Rest, HPBefore = 10, HPAfter = 100, HPMax = 100, SANMax = 50 });
        Check(night.Contains("停工休息"), "night text: " + night);

        if (failures.Count == 0) {
            Debug.Log("Day Cycle PASSED.");
        } else {
            Debug.LogError("Day Cycle FAILED: " + string.Join("; ", failures));
        }

        Debug.Log("=== Day Cycle Test Finished ===");
    }

    // A rest day including its night (the business opens with nothing on sale).
    private static DayEndReport RestDay(PlayerProfile player, out string reason) {
        return DayCycleService.Rest(player, out reason) == null ? null : DayCycleService.FinishNight(player);
    }

    private static CoreBackend NewCore() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();
        DayCycleService.TakePendingReport();
        return core;
    }
}
