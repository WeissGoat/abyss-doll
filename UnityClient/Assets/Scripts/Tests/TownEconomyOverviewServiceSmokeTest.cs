using System.Linq;
using UnityEngine;

public static class TownEconomyOverviewServiceSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Town Economy Overview Service Smoke Test ===");

        TestSellCandidatesUseRumorBestChannel();
        TestAcceptedOrderReportsDeliveryProgress();
        TestRentPressureAndFactionSummary();

        Debug.Log("=== Town Economy Overview Service Smoke Test Finished ===");
    }

    private static void TestSellCandidatesUseRumorBestChannel() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        player.CurrentDay = 1;
        player.StashInventory.Clear();
        ItemEntity scrap = AddStashItem(player, "loot_gear_scrap");

        WeeklyEconomyRefreshReport refresh = TownEconomyService.RefreshWeeklyEconomy(player, 42);
        TownEconomyOverviewReport report = TownEconomyOverviewService.BuildReport(player);
        TownEconomySellCandidate candidate = report.SellCandidates.FirstOrDefault(item => item.Item == scrap);
        TownEconomySellChannelValue dumpValue = candidate?.ChannelValues.FirstOrDefault(value => value.Channel == EconomySellChannel.DumpBox.ToString());

        bool passed = refresh.Success
            && report.Success
            && report.Rumors.Any(rumor => rumor.RumorID == "rumor_mechanical_price_up")
            && candidate != null
            && candidate.SourceContainer == "GroundInventory"
            && candidate.BestChannel == EconomySellChannel.DumpBox.ToString()
            && candidate.BestValue == 150
            && dumpValue != null
            && dumpValue.FinalValue == 150
            && dumpValue.AppliedRumorIDs.Contains("rumor_mechanical_price_up");

        if (passed) {
            Debug.Log("Town Economy Overview Sell Candidate PASSED.");
        } else {
            Debug.LogError($"Town Economy Overview Sell Candidate FAILED. Refresh={refresh.Success}, Report={report.Success}, Best={candidate?.BestChannel}:{candidate?.BestValue ?? -1}, Dump={dumpValue?.FinalValue ?? -1}, Rumors={report.Rumors.Count}, Reason={report.Reason ?? refresh.Reason}");
        }
    }

    private static void TestAcceptedOrderReportsDeliveryProgress() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        player.CurrentDay = 1;
        player.HighestUnlockedDungeonLayer = 1;
        player.StashInventory.Clear();
        ItemEntity scrap = AddStashItem(player, "loot_gear_scrap");
        ItemEntity coil = AddStashItem(player, "loot_rusty_coil");

        WeeklyEconomyRefreshReport refresh = TownEconomyService.RefreshWeeklyEconomy(player, 42);
        OrderInstanceState order = refresh.AvailableOrders.FirstOrDefault(instance => instance.OrderID == "order_mechanic_scrap_drive");
        OrderAcceptReport accept = TownEconomyService.AcceptOrder(player, order?.InstanceID);
        TownEconomyOverviewReport report = TownEconomyOverviewService.BuildReport(player);
        TownEconomyOrderCard card = report.Orders.FirstOrDefault(item => item.InstanceID == order?.InstanceID);

        bool passed = refresh.Success
            && accept.Success
            && report.Success
            && card != null
            && card.Status == EconomyOrderStatus.Accepted.ToString()
            && card.RequiredCount == 2
            && card.MatchedOwnedCount == 2
            && card.MissingCount == 0
            && card.CanDeliver
            && !card.CanAccept
            && card.FixedGold == 180
            && card.GuaranteedRewardMoneyPreview == 40
            && card.ReputationDelta == 10
            && card.TrustDelta == 2
            && card.MatchingItemInstanceIDs.Contains(scrap.InstanceID)
            && card.MatchingItemInstanceIDs.Contains(coil.InstanceID);

        if (passed) {
            Debug.Log("Town Economy Overview Order Progress PASSED.");
        } else {
            Debug.LogError($"Town Economy Overview Order Progress FAILED. Refresh={refresh.Success}, Accept={accept.Success}, Report={report.Success}, Status={card?.Status}, Matched={card?.MatchedOwnedCount ?? -1}, Missing={card?.MissingCount ?? -1}, CanDeliver={card?.CanDeliver ?? false}, RewardMoney={card?.GuaranteedRewardMoneyPreview ?? -1}, Reason={card?.BlockReason ?? report.Reason ?? accept.Reason ?? refresh.Reason}");
        }
    }

    private static void TestRentPressureAndFactionSummary() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        player.Money = 1050;
        player.CurrentMonth = 1;
        player.CurrentMonthDay = 28;
        player.StashInventory.Clear();
        AddStashItem(player, "gear_charge_pistol");

        ItemEntity scrap = AddStashItem(player, "loot_gear_scrap");
        ItemEntity coil = AddStashItem(player, "loot_rusty_coil");
        WeeklyEconomyRefreshReport refresh = TownEconomyService.RefreshWeeklyEconomy(player, 42);
        OrderInstanceState order = refresh.AvailableOrders.FirstOrDefault(instance => instance.OrderID == "order_mechanic_scrap_drive");
        TownEconomyService.AcceptOrder(player, order?.InstanceID);
        OrderDeliveryReport delivery = TownEconomyService.DeliverOrder(player, order?.InstanceID, new[] { scrap, coil });

        TownEconomyOverviewReport report = TownEconomyOverviewService.BuildReport(player);
        TownEconomyFactionSummary faction = report.Factions.FirstOrDefault(item => item.FactionID == "faction_mechanic_workshop");

        bool passed = refresh.Success
            && delivery.Success
            && report.Success
            && report.EstimatedMonthlyBill == 1200
            && report.RentCountdown == 1
            && report.PawnCandidates.Any(candidate => candidate.ItemID == "gear_charge_pistol" && candidate.PawnValue == 200)
            && faction != null
            && faction.ReputationValue == 10
            && faction.TrustValue == 2
            && faction.CompletedOrderCount == 1;

        if (passed) {
            Debug.Log("Town Economy Overview Rent And Faction PASSED.");
        } else {
            Debug.LogError($"Town Economy Overview Rent And Faction FAILED. Refresh={refresh.Success}, Delivery={delivery.Success}, Report={report.Success}, Bill={report.EstimatedMonthlyBill}, Countdown={report.RentCountdown}, PawnCandidates={report.PawnCandidates.Count}, Rep={faction?.ReputationValue ?? -1}, Trust={faction?.TrustValue ?? -1}, Completed={faction?.CompletedOrderCount ?? -1}, Reason={report.Reason ?? delivery.Reason ?? refresh.Reason}");
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
}
