using System.Collections.Generic;
using UnityEngine;

// Home upgrades (docs/specs/2026-10-17-home-loop.md, batch 5): bought once with money; the bench lamp makes
// maintenance cheaper, the mattress improves rest, the gramophone gives SAN at night, the showcase grows a column,
// the second shelf holds more; all saved.
public static class HomeUpgradeTest {
    public static void Run() {
        Debug.Log("=== Running Home Upgrade Test ===");

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();
        DayCycleService.TakePendingReport();

        PlayerProfile player = core.CurrentPlayer;
        DollEntity doll = player.ActiveDoll;
        List<string> failures = new List<string>();
        void Check(bool condition, string message) {
            if (!condition) {
                failures.Add(message);
            }
        }

        Check(ConfigManager.HomeUpgrades.Count == 5, $"{ConfigManager.HomeUpgrades.Count} upgrades in config");

        // Money and once only.
        player.Money = 100;
        Check(!HomeUpgradeService.Buy(player, "upg_bench_lamp", out string poor) && poor.Contains("钱不够"), "bought the lamp without money: " + poor);
        player.Money = 5000;
        Check(HomeUpgradeService.Buy(player, "upg_bench_lamp", out _) && player.Money == 5000 - 350, $"lamp purchase left {player.Money}G");
        Check(!HomeUpgradeService.Buy(player, "upg_bench_lamp", out _), "bought the lamp twice");

        // The lamp: quick repair costs 15% less.
        int listPrice = ConfigManager.MaintenanceConfigs["maint_quick_repair"].Cost.Money;
        int discounted = Mathf.RoundToInt(listPrice * 0.85f);
        int before = player.Money;
        doll.Status.WearAndTear = 80f;
        MaintenanceApplicationResult repair = MaintenanceService.Apply(player, doll, "maint_quick_repair");
        Check(repair.Success && discounted < listPrice && before - player.Money == discounted, $"discounted repair cost {before - player.Money}G, list {listPrice}G");

        // The mattress: rest gives more SAN than the base ratio.
        EconomyConfig economy = TownEconomyService.GetConfig();
        HomeUpgradeService.Buy(player, "upg_mattress", out _);
        player.IsNight = false;
        doll.Status.SAN_Current = 0;
        DayCycleService.Rest(player, out string restReason);
        int expected = Mathf.Min(doll.Status.SAN_Max, Mathf.CeilToInt(doll.Status.SAN_Max * Mathf.Clamp01(economy.RestSANRecoverRatio + 0.2f)));
        Check(doll.Status.SAN_Current == expected, $"rest SAN {doll.Status.SAN_Current}, expected {expected} ({restReason})");

        // The gramophone: SAN +5 after the night.
        HomeUpgradeService.Buy(player, "upg_gramophone", out _);
        doll.Status.SAN_Current = 10;
        DayCycleService.FinishNight(player);
        DayCycleService.TakePendingReport();
        Check(doll.Status.SAN_Current == 15, $"night SAN {doll.Status.SAN_Current}");

        // The showcase and the shelf.
        int width = NightBusinessService.BuildShowcaseGrid(player).Width;
        HomeUpgradeService.Buy(player, "upg_showcase", out _);
        Check(NightBusinessService.BuildShowcaseGrid(player).Width == width + 1, "showcase did not grow");
        Check(MementoService.ShelfSlots(player) == MementoService.BaseShelfSlots, "shelf starts bigger than 6");
        HomeUpgradeService.Buy(player, "upg_shelf", out _);
        Check(MementoService.ShelfSlots(player) == 9, "second shelf did not add room");

        SaveFile restored = SaveService.Deserialize(SaveService.Serialize(player));
        Check(restored.Player.HomeUpgrades.Count == 5, "upgrades lost in the save");

        if (failures.Count == 0) {
            Debug.Log("Home Upgrade PASSED.");
        } else {
            Debug.LogError("Home Upgrade FAILED: " + string.Join("; ", failures));
        }

        Debug.Log("=== Home Upgrade Test Finished ===");
    }
}
