using System.Collections.Generic;
using UnityEngine;

// M2 batch 3: wear and corruption build up in the abyss and come home (GDD_02 §5.1); maintenance and rest bring them
// down; an extreme value blocks the next dive with a reason.
public static class StrainAndMaintenanceTest {
    public static void Run() {
        Debug.Log("=== Running Strain And Maintenance Test ===");

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

        // Walking nodes adds corruption; HP lost in combat adds wear.
        Check(core.Dungeon.StartRunAtLayer(1), "dive did not start");
        NodeBase entry = core.Dungeon.CurrentLayer.EntryNodes != null && core.Dungeon.CurrentLayer.EntryNodes.Count > 0
            ? core.Dungeon.CurrentLayer.EntryNodes[0]
            : core.Dungeon.CurrentLayer.RootNode;
        float corruptionBefore = doll.Status.Corruption;
        core.Dungeon.MoveToNode(entry);
        Check(doll.Status.Corruption > corruptionBefore, $"entering a node left corruption at {doll.Status.Corruption}");

        DollFighter fighter = new DollFighter(doll, new CombatFaction { Type = FactionType.Player });
        float wearBefore = doll.Status.WearAndTear;
        fighter.TakeDamage(20);
        Check(Mathf.Approximately(doll.Status.WearAndTear, wearBefore + 20 * doll.Stats.WearPerHPLost), $"20 HP lost gave wear {doll.Status.WearAndTear - wearBefore}");
        fighter.Cleanup();

        // A defeat adds the wear spike; the night card shows the day's change.
        float wearBeforeDefeat = doll.Status.WearAndTear;
        doll.Status.HP_Current = 0;
        DungeonEventBus.PublishDungeonDefeated();
        DayCycleService.FinishNight(player);
        DayEndReport defeat = DayCycleService.TakePendingReport();
        Check(Mathf.Approximately(doll.Status.WearAndTear, Mathf.Min(100f, wearBeforeDefeat + doll.Stats.DefeatWearGain)), $"defeat wear {wearBeforeDefeat} -> {doll.Status.WearAndTear}");
        Check(defeat != null && defeat.WearAfter > defeat.WearBefore && defeat.CorruptionAfter > defeat.CorruptionBefore, "defeat report does not show the wear / corruption gain");

        // Rest takes some off.
        doll.Status.WearAndTear = 50f;
        doll.Status.Corruption = 50f;
        DayCycleService.Rest(player, out _);
        DayCycleService.FinishNight(player);
        EconomyConfig config = TownEconomyService.GetConfig();
        Check(Mathf.Approximately(doll.Status.WearAndTear, 50f - config.RestWearReduction) && Mathf.Approximately(doll.Status.Corruption, 50f - config.RestCorruptionReduction), $"rest left wear {doll.Status.WearAndTear}, corruption {doll.Status.Corruption}");

        // Extreme wear blocks the dive in Chinese, money-only maintenance fixes it.
        doll.Status.WearAndTear = 95f;
        DiveReadinessResult blocked = DiveReadinessService.Evaluate(player, 1);
        Check(!blocked.CanDive && blocked.BuildSummary().Contains("磨损"), "extreme wear: " + blocked.BuildSummary());
        player.Money = 500;
        MaintenanceApplicationResult repair = MaintenanceService.Apply(player, doll, "maint_quick_repair");
        Check(repair.Success && player.Money == 500 - ConfigManager.MaintenanceConfigs["maint_quick_repair"].Cost.Money && doll.Status.WearAndTear < 90f, $"quick repair: {repair.Reason}, wear {doll.Status.WearAndTear}, money {player.Money}");
        Check(DiveReadinessService.Evaluate(player, 1).CanDive, "still blocked after repair: " + DiveReadinessService.Evaluate(player, 1).BuildSummary());

        player.Money = 0;
        Check(!MaintenanceService.CanApply(player, doll, "maint_quick_cleanse", out string poorReason) && poorReason.Contains("钱不够"), "poor cleanse reason: " + poorReason);

        if (failures.Count == 0) {
            Debug.Log("Strain And Maintenance PASSED.");
        } else {
            Debug.LogError("Strain And Maintenance FAILED: " + string.Join("; ", failures));
        }

        Debug.Log("=== Strain And Maintenance Test Finished ===");
    }
}
