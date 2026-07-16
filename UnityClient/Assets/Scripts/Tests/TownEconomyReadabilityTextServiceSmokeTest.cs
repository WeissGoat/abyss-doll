using System.Linq;
using UnityEngine;

public static class TownEconomyReadabilityTextServiceSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Town Economy Readability Text Service Smoke Test ===");

        TestSellRumorSnapshot();
        TestOrderProgressSnapshot();
        TestRentPressurePawnAndFactionSnapshot();

        Debug.Log("=== Town Economy Readability Text Service Smoke Test Finished ===");
    }

    private static void TestSellRumorSnapshot() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        player.CurrentDay = 1;
        player.StashInventory.Clear();
        AddStashItem(player, "loot_gear_scrap");

        WeeklyEconomyRefreshReport refresh = TownEconomyService.RefreshWeeklyEconomy(player, 42);
        EnsureMechanicalPriceRumor(player, refresh);
        TownEconomyReadabilitySnapshot snapshot = TownEconomyReadabilityTextService.BuildSnapshot(player);
        TownEconomyReadabilitySellLine sellLine = snapshot.SellLines.FirstOrDefault(line => line.ItemID == "loot_gear_scrap");
        TownEconomyReadabilityRumorLine rumorLine = snapshot.RumorLines.FirstOrDefault(line => line.RumorID == "rumor_mechanical_price_up");

        bool passed = refresh.Success
            && snapshot.Success
            && sellLine != null
            && sellLine.BestValue == 150
            && sellLine.ChannelLines.Any(line => line.Contains("传闻"))
            && rumorLine != null
            && rumorLine.StatusText.Contains("x1.5")
            && snapshot.CombinedText.Contains("小镇经济")
            && snapshot.CombinedText.Contains("传闻")
            && snapshot.SellableCount > 0
            && snapshot.TotalBestSellValue >= 150;

        if (passed) {
            Debug.Log("Town Economy Readability Sell Rumor Snapshot PASSED.");
        } else {
            Debug.LogError($"Town Economy Readability Sell Rumor Snapshot FAILED. Refresh={refresh.Success}, Snapshot={snapshot.Success}, SellValue={sellLine?.BestValue ?? -1}, Rumor={rumorLine != null}, Text={snapshot.CombinedText}");
        }
    }

    private static void TestOrderProgressSnapshot() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        player.CurrentDay = 1;
        player.HighestUnlockedDungeonLayer = 1;
        player.StashInventory.Clear();
        AddStashItem(player, "loot_gear_scrap");
        AddStashItem(player, "loot_rusty_coil");

        WeeklyEconomyRefreshReport refresh = TownEconomyService.RefreshWeeklyEconomy(player, 42);
        OrderInstanceState order = refresh.AvailableOrders.FirstOrDefault(instance => instance.OrderID == "order_mechanic_scrap_drive");
        OrderAcceptReport accept = TownEconomyService.AcceptOrder(player, order?.InstanceID);
        TownEconomyReadabilitySnapshot snapshot = TownEconomyReadabilityTextService.BuildSnapshot(player);
        TownEconomyReadabilityOrderLine orderLine = snapshot.OrderLines.FirstOrDefault(line => line.InstanceID == order?.InstanceID);

        bool passed = refresh.Success
            && accept.Success
            && snapshot.Success
            && orderLine != null
            && orderLine.CanDeliver
            && orderLine.StatusText == "可交付"
            && orderLine.ProgressText == "2/2"
            && orderLine.RewardText.Contains("220G")
            && snapshot.DeliverableOrderCount == 1
            && snapshot.CombinedText.Contains("订单");

        if (passed) {
            Debug.Log("Town Economy Readability Order Progress Snapshot PASSED.");
        } else {
            Debug.LogError($"Town Economy Readability Order Progress Snapshot FAILED. Refresh={refresh.Success}, Accept={accept.Success}, Snapshot={snapshot.Success}, Status={orderLine?.StatusText}, Progress={orderLine?.ProgressText}, Reward={orderLine?.RewardText}, Text={snapshot.CombinedText}");
        }
    }

    private static void TestRentPressurePawnAndFactionSnapshot() {
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

        TownEconomyReadabilitySnapshot snapshot = TownEconomyReadabilityTextService.BuildSnapshot(player);
        TownEconomyReadabilityPawnLine pawnLine = snapshot.PawnLines.FirstOrDefault(line => line.ItemID == "gear_charge_pistol");
        TownEconomyReadabilityFactionLine factionLine = snapshot.FactionLines.FirstOrDefault(line => line.FactionID == "faction_mechanic_workshop");

        bool passed = refresh.Success
            && delivery.Success
            && snapshot.Success
            && snapshot.RentPressureText.Contains("1200G")
            && snapshot.PressureLines.Any(line => line.Contains("可典当候选"))
            && pawnLine != null
            && pawnLine.PawnValue == 200
            && factionLine != null
            && factionLine.ReputationValue == 10
            && factionLine.TrustValue == 2
            && snapshot.CombinedText.Contains("典当候选")
            && snapshot.CombinedText.Contains("势力");

        if (passed) {
            Debug.Log("Town Economy Readability Rent Pressure Snapshot PASSED.");
        } else {
            Debug.LogError($"Town Economy Readability Rent Pressure Snapshot FAILED. Refresh={refresh.Success}, Delivery={delivery.Success}, Snapshot={snapshot.Success}, Rent={snapshot.RentPressureText}, Pawn={pawnLine?.PawnValue ?? -1}, Rep={factionLine?.ReputationValue ?? -1}, Trust={factionLine?.TrustValue ?? -1}, Text={snapshot.CombinedText}");
        }
    }

    private static CoreBackend CreateCore() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();
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
