using System.Collections.Generic;
using UnityEngine;

// T0 C segment (docs/specs/2026-10-05-t0-first-loop.md): back from the shallow edge, a three-line recap and one light
// choice — care for her, or look at the notice — then the night closes quietly and the normal day loop opens.
public static class PrologueReturnHome {
    public const string CareChoice = "care";
    public const string NoticeChoice = "notice";

    // True right after the prologue's first dive settles, until Finish runs.
    public static bool IsPending(DungeonManager dungeon) {
        PrologueRunState run = PrologueShallowRun.Current;
        return run != null && !run.ReturnHomeDone && dungeon != null && dungeon.IsPrologueRun;
    }

    public static string BuildRecap(PlayerProfile player) {
        PrologueRunState run = PrologueShallowRun.Current;
        DollEntity doll = player?.ActiveDoll;
        if (run == null || doll == null) {
            return string.Empty;
        }

        string brought = !string.IsNullOrEmpty(run.BroughtBackName) ? run.BroughtBackName : run.Defeated ? "没有（她倒下了）" : "没有";
        List<string> costs = new List<string> { $"HP {run.HPBefore}→{doll.Status.HP_Current}" };
        if (doll.Status.SAN_Current != run.SANBefore) {
            costs.Add($"SAN {run.SANBefore}→{doll.Status.SAN_Current}");
        }

        int wear = Mathf.RoundToInt(doll.Status.WearAndTear - run.WearBefore);
        int corruption = Mathf.RoundToInt(doll.Status.Corruption - run.CorruptionBefore);
        if (wear > 0) {
            costs.Add($"磨损 +{wear}");
        }

        if (corruption > 0) {
            costs.Add($"侵蚀 +{corruption}");
        }

        return $"带回：{brought}\n消耗：{string.Join("，", costs)}\n可处理：先照看她 / 先看催缴纸";
    }

    // 先照看她: free, first time only — HP and SAN full, wear and corruption cleared; uses the core dust if she brought it.
    public static bool Care(PlayerProfile player) {
        DollEntity doll = player?.ActiveDoll;
        if (doll == null) {
            return false;
        }

        doll.Status.HP_Current = doll.Status.HP_Max;
        doll.Status.SAN_Current = doll.Status.SAN_Max;
        doll.Status.WearAndTear = 0f;
        doll.Status.Corruption = 0f;
        return PrologueShallowRun.Current?.BroughtBackID == PrologueShallowRun.CoreDustID
            && ItemLifecycleService.TryConsumeOwnedItemForCost(player, PrologueShallowRun.CoreDustID, "PrologueCare", out _, out _);
    }

    // 先看催缴纸: the scrap she brought sells at the dump-box price; returns the income (0 when nothing sold).
    public static int ReadNotice(PlayerProfile player) {
        if (player == null || PrologueShallowRun.Current?.BroughtBackID != PrologueShallowRun.ScrapID) {
            return 0;
        }

        ItemEntity scrap = player.StashInventory.Find(item => item != null && item.ConfigID == PrologueShallowRun.ScrapID);
        if (scrap == null) {
            return 0;
        }

        EconomySellReport report = TownEconomyService.SellItems(player, new[] { scrap }, EconomySellChannel.DumpBox);
        return report != null && report.Success ? report.TotalIncome : 0;
    }

    public static bool BroughtMemory => PrologueShallowRun.Current?.BroughtBackID == PrologueShallowRun.MemoryNoiseID;

    // The night closes with an empty business and no night card; the next morning is the first normal day.
    public static void Finish(PlayerProfile player) {
        if (PrologueShallowRun.Current != null) {
            PrologueShallowRun.Current.ReturnHomeDone = true;
        }

        if (player != null && player.IsNight) {
            DayCycleService.FinishNight(player);
            DayCycleService.TakePendingReport();
        }
    }
}
