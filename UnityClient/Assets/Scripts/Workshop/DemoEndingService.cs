using System.Collections.Generic;
using System.Linq;

// What the end card lists: how far this run came.
public class DemoEndingStats {
    public int Day;
    public int Dives;
    public int BondLevel;
    public int KeptPromises;
    public int Mementos;
}

// The end of the Demo (docs/specs/2026-10-24-layer3-demo-end.md, B): the boss of the deepest layer falls and Zero hears
// the music box song further down (a comic page at the stairs), says so at the door (the arrival pool's final_boss
// line), and that night talks about it (the night talk tagged ending); after 晚安 the card 「第一章 · 完」. It plays once
// per save; the town goes on afterwards.
public static class DemoEndingService {
    // The dive beat the deepest layer's boss (dive log fact, every time).
    public const string FinalBossFact = "final_boss";
    // Tonight is the ending's night (night fact, the first time only).
    public const string EndingFact = "ending";
    public const string ComicSceneID = "demo_end_oracle";
    public const string ComicVisualID = "cg_demo_end_oracle";

    // The layer with nothing configured below it.
    public static bool IsDeepestLayer(int layerID) {
        return layerID > 0 && ConfigManager.Dungeons.ContainsKey(layerID) && !ConfigManager.Dungeons.ContainsKey(layerID + 1);
    }

    // Beaten the deepest boss, the ending not played yet.
    public static bool Pending(PlayerProfile player) {
        return player != null && !player.DemoEndingSeen && (player.DiveJournal ?? new List<DiveLog>()).Any(log => log != null && log.FinalBossDefeated);
    }

    // The comic at the stairs right after that fight, once.
    public static bool ComicDue(PlayerProfile player) {
        return player != null && !player.DemoEndingSeen && !player.DemoEndingComicShown && DiveLogService.Current != null && DiveLogService.Current.FinalBossDefeated;
    }

    public static void MarkComicShown(PlayerProfile player) {
        if (player != null) {
            player.DemoEndingComicShown = true;
        }
    }

    // The night of the ending: her talk is the ending's.
    public static bool IsEndingNight(DayEndReport report, PlayerProfile player) {
        return report?.DiveLog != null && report.DiveLog.FinalBossDefeated && player != null && !player.DemoEndingSeen;
    }

    public static DemoEndingStats Stats(PlayerProfile player) {
        DemoEndingStats stats = new DemoEndingStats();
        if (player == null) {
            return stats;
        }

        stats.Day = player.CurrentDay;
        stats.Dives = (player.DiveJournal ?? new List<DiveLog>()).Count(log => log != null && !log.Prologue);
        stats.BondLevel = BondService.Level(player.ActiveDoll);
        stats.KeptPromises = player.KeptPromises;
        stats.Mementos = player.Shelf?.Count ?? 0;
        return stats;
    }

    // The card has been shown; the save goes on as an ordinary town.
    public static void Finish(PlayerProfile player) {
        if (player != null) {
            player.DemoEndingSeen = true;
        }
    }
}
