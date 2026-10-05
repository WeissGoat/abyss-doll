using System.Collections.Generic;
using UnityEngine;

// M2 batch 4 (GDD_04 three steps, Demo cut): after the day's dive or rest comes the night; goods placed in the dump
// box or the showcase sell when the business opens, income is counted before rent, and the next day starts.
public static class NightBusinessTest {
    public static void Run() {
        Debug.Log("=== Running Night Business Test ===");

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();
        DayCycleService.TakePendingReport();

        PlayerProfile player = core.CurrentPlayer;
        EconomyConfig config = TownEconomyService.GetConfig();
        List<string> failures = new List<string>();
        void Check(bool condition, string message) {
            if (!condition) {
                failures.Add(message);
            }
        }

        ItemEntity Add(string configID) {
            ItemEntity item = ConfigManager.CreateItem(configID);
            ItemLifecycleService.MarkGroundInventoryItem(player, item, "Test");
            return item;
        }

        // Dump box: eighty percent of the sell value; contraband is refused; core materials stay out of 一键倾倒.
        ItemEntity scrap = Add("loot_gear_scrap");
        ItemEntity coil = Add("loot_rusty_coil");
        ItemEntity core1 = Add("mat_core_tier1");
        ItemEntity relic = Add("trade_cracked_relic");
        Check(!NightBusinessService.TryMoveToDumpBox(player, relic, out string contrabandReason) && contrabandReason.Contains("违禁"), "contraband in the dump box: " + contrabandReason);
        int dumped = NightBusinessService.DumpAllLoot(player);
        Check(dumped == 2 && player.DumpBox.Contains(scrap) && player.DumpBox.Contains(coil) && !player.DumpBox.Contains(core1) && player.StashInventory.Contains(relic), $"一键倾倒 moved {dumped}");

        // Showcase: premium, plus a bonus for each neighbour sharing a market tag.
        Check(NightBusinessService.TryReturnToStash(player, coil) && player.StashInventory.Contains(coil), "taking the coil back failed");
        ItemEntity lamp = Add("trade_miner_lamp");
        bool coilPlaced = NightBusinessService.TryMoveToShowcase(player, coil, out string r1);
        bool lampPlaced = NightBusinessService.TryMoveToShowcase(player, lamp, out string r2);
        Check(coilPlaced && lampPlaced, $"showcase placement: {r1} {r2}");
        List<BusinessLine> preview = NightBusinessService.Preview(player);
        BusinessLine coilLine = preview.Find(line => line.Item == coil);
        BusinessLine scrapLine = preview.Find(line => line.Item == scrap);
        int coilSell = TownEconomyService.CalculateItemSellValue(player, coil, EconomySellChannel.Showcase).FinalValue;
        float expectedMultiplier = config.ShowcaseMultiplier * (1f + config.ShowcaseAdjacencyBonus);
        Check(coilLine != null && coilLine.AdjacentMatches == 1 && coilLine.Value == Mathf.RoundToInt(coilSell * expectedMultiplier), $"coil showcase value {coilLine?.Value}, matches {coilLine?.AdjacentMatches}");
        Check(scrapLine != null && scrapLine.Value == Mathf.RoundToInt(TownEconomyService.CalculateItemSellValue(player, scrap, EconomySellChannel.DumpBox).FinalValue * config.DumpBoxMultiplier), $"scrap dump value {scrapLine?.Value}");

        // A 2x2 item that does not fit any more is refused with a reason.
        for (int i = 0; i < 6; i++) {
            NightBusinessService.TryMoveToShowcase(player, Add("loot_warped_plate"), out _);
        }
        ItemEntity extra = Add("loot_warped_plate");
        Check(!NightBusinessService.TryMoveToShowcase(player, extra, out string fullReason) && fullReason.Contains("放不下"), "full showcase: " + fullReason);

        // The night: a dive or rest leaves the day open; nothing can start until the business opens.
        int expectedIncome = 0;
        foreach (BusinessLine line in NightBusinessService.Preview(player)) {
            expectedIncome += line.Value;
        }
        int moneyBefore = player.Money;
        Check(DayCycleService.Rest(player, out _) != null && player.IsNight && player.CurrentDay == 1, "rest did not lead into the night");
        Check(!DayCycleService.CanStartDay(player, out string nightReason) && nightReason.Contains("营业"), "a day could start at night: " + nightReason);
        DayEndReport night = DayCycleService.FinishNight(player);
        Check(night != null && night.Business != null && night.Business.Total == expectedIncome && player.Money == moneyBefore + expectedIncome, $"business income {night?.Business?.Total}, expected {expectedIncome}, money {moneyBefore} -> {player.Money}");
        Check(!player.IsNight && player.CurrentDay == 2 && player.DumpBox.Count == 0 && player.Showcase.Count == 0 && scrap.ContainerType == ItemContainerType.Sold, "the night did not close cleanly");
        Check(TownDayHUD.BuildNightText(night).Contains("今晚营业"), "night card does not mention the business");

        // An empty night still closes with a 0G business.
        DayCycleService.Rest(player, out _);
        DayEndReport empty = DayCycleService.FinishNight(player);
        Check(empty != null && empty.Business.Total == 0 && TownDayHUD.BuildNightText(empty).Contains("0G"), "empty night text: " + TownDayHUD.BuildNightText(empty));

        // Playtest fix: the night does not open the business screen by itself; the choice panel offers 去营业 instead.
        DayCycleService.Rest(player, out _);
        GameObject root = new GameObject("NightChoiceTestRoot", typeof(RectTransform));
        try {
            TownDayHUD hud = TownDayHUD.Ensure(root.transform, null);
            hud.ShowPendingDayEvents();
            Transform panel = root.transform.Find(TownDayHUD.RootName + "/" + NightBusinessPanel.PanelName);
            UnityEngine.UI.Button dive = FindButton(root.transform, "Dive_Button");
            UnityEngine.UI.Button rest = FindButton(root.transform, "Rest_Button");
            Check(panel == null || !panel.gameObject.activeSelf, "the business screen opened by itself");
            Check(dive != null && dive.GetComponentInChildren<UnityEngine.UI.Text>().text == TownDayHUD.GoBusinessLabel && dive.interactable && rest != null && !rest.gameObject.activeSelf, "the night choice does not offer 去营业");
            dive?.onClick.Invoke();
            panel = root.transform.Find(TownDayHUD.RootName + "/" + NightBusinessPanel.PanelName);
            Check(panel != null && panel.gameObject.activeSelf && player.IsNight, "去营业 did not open the business screen");
        } finally {
            Object.DestroyImmediate(root);
            DayCycleService.FinishNight(player);
            DayCycleService.TakePendingReport();
        }

        if (failures.Count == 0) {
            Debug.Log("Night Business PASSED.");
        } else {
            Debug.LogError("Night Business FAILED: " + string.Join("; ", failures));
        }

        Debug.Log("=== Night Business Test Finished ===");
    }

    private static UnityEngine.UI.Button FindButton(Transform root, string name) {
        foreach (UnityEngine.UI.Button button in root.GetComponentsInChildren<UnityEngine.UI.Button>(true)) {
            if (button.name == name) {
                return button;
            }
        }

        return null;
    }
}
