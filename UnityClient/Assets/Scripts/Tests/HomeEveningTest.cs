using System.Collections.Generic;
using UnityEngine;

// The evening at home (docs/specs/2026-10-17-home-loop.md, batch 2): a dive writes a journal page as it goes, the
// page waits at the door once, Zero's lines follow the day's facts, the night report carries the page, and the
// journal survives a save.
public static class HomeEveningTest {
    public static void Run() {
        Debug.Log("=== Running Home Evening Test ===");

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

        // A dive: one node, a hard fight (Zero ends it at 10 HP), then home.
        player.DiveJournal.Clear();
        Check(core.Dungeon.StartRunAtLayer(1), "dive did not start");
        NodeBase entry = core.Dungeon.CurrentLayer.EntryNodes != null && core.Dungeon.CurrentLayer.EntryNodes.Count > 0
            ? core.Dungeon.CurrentLayer.EntryNodes[0]
            : core.Dungeon.CurrentLayer.RootNode;
        core.Dungeon.MoveToNode(entry);
        doll.Status.HP_Current = 10;
        DiveLogService.RecordCombat(new CombatOutcomeReport {
            OutcomeType = CombatOutcomeType.Victory,
            ActiveDollHP = 10,
            EnemyFighters = new List<CombatOutcomeFighterSnapshot> {
                new CombatOutcomeFighterSnapshot { Name = "锈蚀猎犬" },
                new CombatOutcomeFighterSnapshot { Name = "锈蚀猎犬" }
            }
        });
        DungeonEventBus.PublishDungeonEvacuated();

        DiveLog page = DiveLogService.PendingArrival(player);
        Check(player.DiveJournal.Count == 1 && page != null, $"journal has {player.DiveJournal.Count} page(s), pending arrival {page != null}");
        Check(page != null && page.Steps.Count == 1 && page.Steps[0].Hurt, "the fight's step is not marked hurt");
        Check(page != null && page.DefeatedEnemies.Count == 2 && !page.Defeated, "defeated enemies or outcome wrong");
        Check(DiveLogService.Highlight(page) == DiveHighlight.NearlyFell, $"highlight {DiveLogService.Highlight(page)}, expected NearlyFell");
        string arrival = ZeroLineService.ArrivalLine(page);
        Check(arrival == "差一点。真的只差一点。" || arrival == "我还站着。……还站着。" || arrival == "很疼。但是背包是满的。", "arrival line: " + arrival);
        Check(HomeJournalView.BuildBody(page).Contains("锈蚀猎犬 ×2"), "page body: " + HomeJournalView.BuildBody(page));

        // The page opens once.
        page.Shown = true;
        Check(DiveLogService.PendingArrival(player) == null, "the page waits at the door again after being shown");

        // The night report carries the page; the night line answers it.
        DayEndReport night = DayCycleService.FinishNight(player);
        DayEndReport pending = DayCycleService.TakePendingReport();
        DayEndReport report = pending ?? night;
        HashSet<string> facts = ZeroLineService.NightFacts(report, player);
        Check(report != null && report.DiveLog == page, "night report has no dive page");
        Check(facts.Contains("dive") && facts.Contains("nearly_fell") && facts.Contains("hurt"), "night facts: " + string.Join(",", facts));
        Check(!string.IsNullOrEmpty(ZeroLineService.NightLine(report, player)), "no night line");

        // Highlights rank defeat first, then near misses, the boss, a bypassed elite, loot left behind.
        Check(DiveLogService.Highlight(new DiveLog { Defeated = true, BossDefeated = true, HPMax = 100, LowestHP = 5 }) == DiveHighlight.Defeated, "defeat is not the highlight");
        Check(DiveLogService.Highlight(new DiveLog { BossDefeated = true, EliteBypassed = true, HPMax = 100, LowestHP = 60 }) == DiveHighlight.BossDefeated, "boss is not the highlight");
        Check(DiveLogService.Highlight(new DiveLog { EliteBypassed = true, LeftBehind = 2, HPMax = 100, LowestHP = 60 }) == DiveHighlight.EliteBypassed, "bypassed elite is not the highlight");
        Check(DiveLogService.Highlight(new DiveLog { HPMax = 100, LowestHP = 60 }) == DiveHighlight.Ordinary, "plain dive is not ordinary");

        // The most specific line wins; the same day always reads the same.
        string emptyHanded = ZeroLineService.Pick(ZeroLineService.ArrivalPool, new HashSet<string> { "ordinary", "empty_handed" }, 3);
        Check(emptyHanded == "什么都没找到。……对不起。", "empty-handed line: " + emptyHanded);
        Check(ZeroLineService.Pick(ZeroLineService.NightPool, new HashSet<string> { "rest" }, 4) == ZeroLineService.Pick(ZeroLineService.NightPool, new HashSet<string> { "rest" }, 4), "same day, different line");

        // The journal is part of the save.
        SaveFile restored = SaveService.Deserialize(SaveService.Serialize(player));
        Check(restored.Player.DiveJournal.Count == 1 && restored.Player.DiveJournal[0].DefeatedEnemies.Count == 2 && restored.Player.DiveJournal[0].Shown, "journal lost in the save");

        // Exactly a fifth of max HP lost in one fight counts as hurt (0.2f * 100 is a hair over 20 in float).
        doll.Status.HP_Current = doll.Status.HP_Max;
        DiveLogService.Begin(player, 1, false);
        DiveLogService.RecordNode(new CombatNode(), "node_combat_icon");
        DiveLogService.RecordCombat(new CombatOutcomeReport { OutcomeType = CombatOutcomeType.Victory, ActiveDollHP = doll.Status.HP_Max - doll.Status.HP_Max / 5 });
        Check(DiveLogService.Current.Steps[0].Hurt, "a fifth of max HP lost is not marked hurt");

        if (failures.Count == 0) {
            Debug.Log("Home Evening PASSED.");
        } else {
            Debug.LogError("Home Evening FAILED: " + string.Join("; ", failures));
        }

        Debug.Log("=== Home Evening Test Finished ===");
    }
}
