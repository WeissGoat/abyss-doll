using System.Collections.Generic;
using UnityEngine;

// The end of the Demo (docs/specs/2026-10-24-layer3-demo-end.md, B): beating the deepest layer's boss marks the dive;
// at the door she says the song line, that night the ending's talk comes whatever else happened, and after it the
// ending is spent: the next time the same boss falls only the door line comes back.
public static class DemoEndingTest {
    public static void Run() {
        Debug.Log("=== Running Demo Ending Test ===");

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();

        PlayerProfile player = core.CurrentPlayer;
        List<string> failures = new List<string>();
        void Check(bool condition, string message) {
            if (!condition) {
                failures.Add(message);
            }
        }

        Check(DemoEndingService.IsDeepestLayer(3) && !DemoEndingService.IsDeepestLayer(2) && !DemoEndingService.IsDeepestLayer(4), "layer 3 is not the deepest configured layer");
        Check(ConfigManager.Scenes.ContainsKey(DemoEndingService.ComicSceneID), "the ending's comic lines are missing");

        // The dive that beat the boss, with a near fall on the way (its own talk would otherwise win).
        DiveLog log = new DiveLog { Day = 9, LayerID = 2, BossDefeated = true, FinalBossDefeated = true, HPStart = 100, HPEnd = 30, HPMax = 100, LowestHP = 5 };
        player.DiveJournal.Add(log);
        player.CurrentDay = 10;
        Check(DemoEndingService.Pending(player), "the ending is not pending after the deepest boss");
        Check(DiveLogService.Facts(log).Contains(DemoEndingService.FinalBossFact), "the dive does not carry the final_boss fact");

        HomeLine door = ZeroLineService.ArrivalPick(log);
        Check(door != null && door.Text.Contains("那首歌"), "the door line is not the song line: " + door?.Text);

        DayEndReport night = new DayEndReport { Choice = DayChoice.Dive, DiveLayerID = 2, EndedDay = 9, NewDay = 10, DiveLog = log };
        player.LastNightTalkDay = 8;
        Check(DemoEndingService.IsEndingNight(night, player), "the night is not the ending's");
        NightTalkConfig talk = PromiseService.PickTalk(night, player);
        Check(talk != null && talk.TalkID == "talk_demo_end", "the ending's talk did not come: " + talk?.TalkID);

        DemoEndingStats stats = DemoEndingService.Stats(player);
        Check(stats.Dives == 1 && DemoEndCard.BuildStats(stats).Contains("下潜 1 次"), "end card numbers: " + DemoEndCard.BuildStats(stats));

        DemoEndingService.Finish(player);
        SaveFile restored = SaveService.Deserialize(SaveService.Serialize(player));
        Check(restored.Player.DemoEndingSeen, "the ending being seen is lost in the save");
        Check(!DemoEndingService.Pending(player) && !DemoEndingService.IsEndingNight(night, player), "the ending would play again");
        NightTalkConfig later = PromiseService.PickTalk(new DayEndReport { Choice = DayChoice.Dive, EndedDay = 12, NewDay = 13, DiveLog = log }, player);
        Check(later == null || later.TalkID != "talk_demo_end", "the ending's talk came back after the ending");
        Check(ZeroLineService.ArrivalPick(log).Text.Contains("那首歌"), "the door line about the song is gone on later clears");

        if (failures.Count == 0) {
            Debug.Log("Demo Ending PASSED.");
        } else {
            Debug.LogError("Demo Ending FAILED: " + string.Join("; ", failures));
        }

        Debug.Log("=== Demo Ending Test Finished ===");
    }
}
