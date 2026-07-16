using System.Linq;
using UnityEngine;

public static class TownEconomyServiceSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Town Economy Service Smoke Test ===");

        TestDailyBusinessReportAdvancesCalendar();
        TestMonthlyRentPaid();
        TestLightDebtAccepted();
        TestPawnRequiredAndPlayerSelectedPawnPaysRent();
        TestProtectedItemsCannotBePawned();
        TestWeeklyRefreshAcceptsAndDeliversOrder();
        TestRumorMultiplierAppliesToSellValue();

        Debug.Log("=== Town Economy Service Smoke Test Finished ===");
    }

    private static void TestDailyBusinessReportAdvancesCalendar() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        player.Money = 100;
        player.CurrentDay = 1;
        player.CurrentMonth = 1;
        player.CurrentMonthDay = 1;

        DailyEconomyReport report = TownEconomyService.SettleDailyBusiness(
            player,
            new DailyEconomySettlementInput {
                DumpBoxIncome = 150,
                ShowcaseIncome = 200,
                Expenses = 50
            });

        bool passed = report.Success
            && report.GrossIncome == 350
            && report.NetDelta == 300
            && report.EndingMoney == 400
            && player.Money == 400
            && player.CurrentDay == 2
            && player.CurrentMonthDay == 2
            && player.MonthlyGrossIncome == 350
            && report.RentCountdown == 27;

        if (passed) {
            Debug.Log("Town Economy Daily Business Report PASSED.");
        } else {
            Debug.LogError($"Town Economy Daily Business Report FAILED. Success={report.Success}, Money={player.Money}, Day={player.CurrentDay}, MonthDay={player.CurrentMonthDay}, Gross={report.GrossIncome}, Net={report.NetDelta}, RentCountdown={report.RentCountdown}");
        }
    }

    private static void TestMonthlyRentPaid() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        player.Money = 1350;
        player.CurrentMonth = 1;
        player.CurrentMonthDay = 28;

        MonthlyRentSettlementReport report = TownEconomyService.ResolveMonthlyRent(player);

        bool passed = report.Success
            && report.Status == MonthlyRentSettlementStatus.Paid
            && report.TotalBill == 1200
            && report.PaidAmount == 1200
            && player.Money == 150
            && player.EconomyDebtAmount == 0
            && player.DebtLevel == 0;

        if (passed) {
            Debug.Log("Town Economy Monthly Rent Paid PASSED.");
        } else {
            Debug.LogError($"Town Economy Monthly Rent Paid FAILED. Success={report.Success}, Status={report.Status}, Bill={report.TotalBill}, Money={player.Money}, Debt={player.EconomyDebtAmount}, Reason={report.Reason}");
        }
    }

    private static void TestLightDebtAccepted() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        player.Money = 1120;
        player.CurrentMonth = 1;
        player.CurrentMonthDay = 28;

        MonthlyRentSettlementReport report = TownEconomyService.ResolveMonthlyRent(player);

        bool passed = report.Success
            && report.Status == MonthlyRentSettlementStatus.LightDebtAccepted
            && report.TotalBill == 1200
            && report.Shortfall == 80
            && player.Money == 0
            && player.EconomyDebtAmount == 80
            && player.DebtLevel == 1
            && player.ConsecutiveHeavyDefaultCount == 0;

        if (passed) {
            Debug.Log("Town Economy Light Debt PASSED.");
        } else {
            Debug.LogError($"Town Economy Light Debt FAILED. Success={report.Success}, Status={report.Status}, Bill={report.TotalBill}, Shortfall={report.Shortfall}, Money={player.Money}, Debt={player.EconomyDebtAmount}, DebtLevel={player.DebtLevel}");
        }
    }

    private static void TestPawnRequiredAndPlayerSelectedPawnPaysRent() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        player.Money = 1050;
        player.CurrentMonth = 1;
        player.CurrentMonthDay = 28;
        player.StashInventory.Clear();
        ItemEntity pawnItem = AddStashItem(player, "gear_charge_pistol");

        MonthlyRentSettlementReport preview = TownEconomyService.ResolveMonthlyRent(player);
        MonthlyRentSettlementReport paid = TownEconomyService.ResolveMonthlyRent(player, new[] { pawnItem });

        bool passed = preview.Status == MonthlyRentSettlementStatus.RequiresPawn
            && preview.PawnCandidates.Count > 0
            && preview.PawnCandidates.Any(candidate => candidate.Item == pawnItem)
            && player.HasPendingMonthlyRent == false
            && paid.Success
            && paid.Status == MonthlyRentSettlementStatus.Paid
            && paid.PawnIncome == 200
            && paid.PawnedItems.Count == 1
            && player.Money == 50
            && !player.StashInventory.Contains(pawnItem);

        if (passed) {
            Debug.Log("Town Economy Player Selected Pawn Pays Rent PASSED.");
        } else {
            Debug.LogError($"Town Economy Player Selected Pawn Pays Rent FAILED. PreviewStatus={preview.Status}, PreviewCandidates={preview.PawnCandidates.Count}, PaidSuccess={paid.Success}, PaidStatus={paid.Status}, PawnIncome={paid.PawnIncome}, Money={player.Money}, Pending={player.HasPendingMonthlyRent}, ContainsPawn={player.StashInventory.Contains(pawnItem)}");
        }
    }

    private static void TestProtectedItemsCannotBePawned() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        player.Money = 1050;
        player.CurrentMonth = 1;
        player.CurrentMonthDay = 28;
        player.StashInventory.Clear();
        ItemEntity protectedItem = AddStashItem(player, "gear_chainsaw_sword");
        protectedItem.Tags.Add("StoryLocked");

        MonthlyRentSettlementReport report = TownEconomyService.ResolveMonthlyRent(player);

        bool passed = report.Success
            && report.Status == MonthlyRentSettlementStatus.HeavyDefault
            && report.PawnCandidates.Count == 0
            && player.Money == 0
            && player.EconomyDebtAmount == 150
            && player.DebtLevel == 2
            && player.ConsecutiveHeavyDefaultCount == 1
            && player.StashInventory.Contains(protectedItem);

        if (passed) {
            Debug.Log("Town Economy Protected Pawn Boundary PASSED.");
        } else {
            Debug.LogError($"Town Economy Protected Pawn Boundary FAILED. Success={report.Success}, Status={report.Status}, Candidates={report.PawnCandidates.Count}, Money={player.Money}, Debt={player.EconomyDebtAmount}, DebtLevel={player.DebtLevel}, HeavyDefaults={player.ConsecutiveHeavyDefaultCount}, ContainsProtected={player.StashInventory.Contains(protectedItem)}");
        }
    }

    private static void TestWeeklyRefreshAcceptsAndDeliversOrder() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        player.Money = 0;
        player.CurrentDay = 1;
        player.HighestUnlockedDungeonLayer = 1;
        player.StashInventory.Clear();
        ItemEntity scrap = AddStashItem(player, "loot_gear_scrap");
        ItemEntity coil = AddStashItem(player, "loot_rusty_coil");

        WeeklyEconomyRefreshReport refresh = TownEconomyService.RefreshWeeklyEconomy(player, 42);
        EnsureMechanicalPriceRumor(player, refresh);
        OrderInstanceState order = refresh.AvailableOrders.FirstOrDefault(instance => instance.OrderID == "order_mechanic_scrap_drive");
        OrderAcceptReport accepted = TownEconomyService.AcceptOrder(player, order?.InstanceID);
        OrderDeliveryReport delivered = TownEconomyService.DeliverOrder(player, order?.InstanceID, new[] { scrap, coil });
        FactionRuntimeState faction = player.FactionStates.FirstOrDefault(state => state.FactionID == "faction_mechanic_workshop");

        bool passed = refresh.Success
            && refresh.ActiveRumors.Any(rumor => rumor.RumorID == "rumor_mechanical_price_up")
            && order != null
            && accepted.Success
            && delivered.Success
            && delivered.DeliveredItems.Count == 2
            && delivered.GoldReward == 180
            && delivered.RewardMoney == 40
            && player.Money == 220
            && faction != null
            && faction.ReputationValue == 10
            && faction.TrustValue == 2
            && faction.CompletedOrderIDs.Contains("order_mechanic_scrap_drive")
            && !player.StashInventory.Contains(scrap)
            && !player.StashInventory.Contains(coil);

        if (passed) {
            Debug.Log("Town Economy Order Delivery PASSED.");
        } else {
            Debug.LogError($"Town Economy Order Delivery FAILED. Refresh={refresh.Success}, Order={order?.OrderID}, Accept={accepted.Success}, Deliver={delivered.Success}, Money={player.Money}, Rep={faction?.ReputationValue ?? -1}, Trust={faction?.TrustValue ?? -1}, Reason={delivered.Reason ?? accepted.Reason ?? refresh.Reason}");
        }
    }

    private static void TestRumorMultiplierAppliesToSellValue() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        player.Money = 10;
        player.CurrentDay = 1;
        player.StashInventory.Clear();
        ItemEntity scrap = AddStashItem(player, "loot_gear_scrap");

        WeeklyEconomyRefreshReport refresh = TownEconomyService.RefreshWeeklyEconomy(player, 42);
        EnsureMechanicalPriceRumor(player, refresh);
        EconomySellLine value = TownEconomyService.CalculateItemSellValue(player, scrap, EconomySellChannel.DumpBox);
        EconomySellReport sell = TownEconomyService.SellItems(player, new[] { scrap }, EconomySellChannel.DumpBox);

        bool passed = refresh.Success
            && value.FinalValue == 150
            && value.AppliedRumorIDs.Contains("rumor_mechanical_price_up")
            && sell.Success
            && sell.TotalIncome == 150
            && player.Money == 160
            && !player.StashInventory.Contains(scrap);

        if (passed) {
            Debug.Log("Town Economy Rumor Sell Multiplier PASSED.");
        } else {
            Debug.LogError($"Town Economy Rumor Sell Multiplier FAILED. Refresh={refresh.Success}, Value={value.FinalValue}, Applied={string.Join(",", value.AppliedRumorIDs)}, Sell={sell.Success}, Income={sell.TotalIncome}, Money={player.Money}, Reason={sell.Reason ?? refresh.Reason}");
        }
    }

    private static CoreBackend CreateCore() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        return core;
    }

    private static ItemEntity AddStashItem(PlayerProfile player, string configID) {
        ItemEntity item = ConfigManager.CreateItem(configID);
        if (item != null) {
            player.StashInventory.Add(item);
        }

        return item;
    }

    private static void EnsureMechanicalPriceRumor(PlayerProfile player, WeeklyEconomyRefreshReport refresh) {
        const string rumorID = "rumor_mechanical_price_up";
        if (player == null || !ConfigManager.Rumors.ContainsKey(rumorID)) {
            return;
        }

        if (player.ActiveRumors.Any(rumor => rumor != null && rumor.RumorID == rumorID)) {
            return;
        }

        ActiveRumorState forcedRumor = new ActiveRumorState {
            RumorID = rumorID,
            StartDay = Mathf.Max(1, player.CurrentDay),
            ExpireDay = Mathf.Max(1, player.CurrentDay) + Mathf.Max(1, ConfigManager.Rumors[rumorID].DurationDays) - 1,
            LogSeed = 0
        };
        player.ActiveRumors.Add(forcedRumor);
        refresh?.ActiveRumors.Add(forcedRumor);
    }
}
