using System.Collections.Generic;
using UnityEngine;

public static class CombatReadabilityTextServiceSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Combat Readability Text Service Smoke Test ===");

        TestIntentSnapshot();
        TestVictoryOutcomeSnapshot();
        TestDefeatOutcomeSnapshotWithTimeline();

        Debug.Log("=== Combat Readability Text Service Smoke Test Finished ===");
    }

    private static void TestIntentSnapshot() {
        CoreBackend core = CreateCoreWithEmptyGrid();
        core.Combat.StartCombat(new List<string> { "mob_scavenger_bug" });

        CombatIntentReadabilitySnapshot snapshot = CombatReadabilityTextService.BuildIntentSnapshot(core.Combat);
        CombatIntentReadabilityLine line = snapshot.EnemyLines.Count > 0 ? snapshot.EnemyLines[0] : null;

        bool passed = snapshot.Success
            && snapshot.Header == "敌方意图"
            && line != null
            && line.MonsterID == "mob_scavenger_bug"
            && line.IntentType == MonsterIntentType.Attack
            && line.StatusText.Contains("攻击")
            && line.DetailText.Contains("造成约 10 伤害")
            && snapshot.CombinedText.Contains("敌方意图");

        if (passed) {
            Debug.Log("Combat Readability Intent Snapshot PASSED.");
        } else {
            Debug.LogError($"Combat Readability Intent Snapshot FAILED. Success={snapshot.Success}, Monster={line?.MonsterID}, Intent={line?.IntentType}, Text={snapshot.CombinedText}");
        }
    }

    private static void TestVictoryOutcomeSnapshot() {
        CombatOutcomeReadabilitySnapshot snapshot = CombatReadabilityTextService.BuildOutcomeSnapshot(CreateVictoryReport());
        bool passed = snapshot.Success
            && snapshot.OutcomeType == CombatOutcomeType.Victory
            && snapshot.Title == "战斗胜利"
            && snapshot.FighterLines.Count >= 2
            && snapshot.TimelineLines.Count > 0
            && snapshot.CombinedText.Contains("战斗者状态")
            && snapshot.CombinedText.Contains("最近战斗记录");

        if (passed) {
            Debug.Log("Combat Readability Victory Outcome Snapshot PASSED.");
        } else {
            Debug.LogError($"Combat Readability Victory Outcome Snapshot FAILED. Success={snapshot.Success}, Type={snapshot.OutcomeType}, Fighters={snapshot.FighterLines.Count}, Timeline={snapshot.TimelineLines.Count}, Text={snapshot.CombinedText}");
        }
    }

    private static void TestDefeatOutcomeSnapshotWithTimeline() {
        CombatOutcomeReadabilitySnapshot snapshot = CombatReadabilityTextService.BuildOutcomeSnapshot(CreateHpDefeatReport(), 2);
        bool passed = snapshot.Success
            && snapshot.OutcomeType == CombatOutcomeType.Defeat
            && snapshot.DefeatReason == CombatDefeatReasonType.PlayerHpDepleted
            && snapshot.Title == "HP 战败"
            && snapshot.TimelineLines.Count == 2
            && snapshot.CombinedText.Contains("HP 归零")
            && snapshot.CombinedText.Contains("最近战斗记录");

        if (passed) {
            Debug.Log("Combat Readability Defeat Outcome Snapshot PASSED.");
        } else {
            Debug.LogError($"Combat Readability Defeat Outcome Snapshot FAILED. Success={snapshot.Success}, Type={snapshot.OutcomeType}, Reason={snapshot.DefeatReason}, Timeline={snapshot.TimelineLines.Count}, Text={snapshot.CombinedText}");
        }
    }

    private static CombatOutcomeReport CreateVictoryReport() {
        CombatOutcomeReport report = new CombatOutcomeReport {
            OutcomeType = CombatOutcomeType.Victory,
            DefeatReason = CombatDefeatReasonType.None,
            Title = "战斗胜利",
            Summary = "已击败全部敌人。本场敌人 1 个。"
        };
        report.PlayerFighters.Add(new CombatOutcomeFighterSnapshot {
            Name = "原型机·零",
            RuntimeHP = 100,
            RuntimeMaxHP = 100,
            IsAlive = true
        });
        report.EnemyFighters.Add(new CombatOutcomeFighterSnapshot {
            Name = "拾荒虫",
            RuntimeHP = 0,
            RuntimeMaxHP = 40,
            IsAlive = false
        });
        report.TimelineEvents.Add(new CombatTimelineEvent {
            Sequence = 0,
            EventType = CombatTimelineEventType.Outcome,
            Title = "战斗胜利",
            Detail = "敌方全灭。"
        });
        return report;
    }

    private static CombatOutcomeReport CreateHpDefeatReport() {
        CombatOutcomeReport report = new CombatOutcomeReport {
            OutcomeType = CombatOutcomeType.Defeat,
            DefeatReason = CombatDefeatReasonType.PlayerHpDepleted,
            Title = "HP 战败",
            Summary = "魔偶 HP 归零。剩余敌人 1/1。"
        };
        report.PlayerFighters.Add(new CombatOutcomeFighterSnapshot {
            Name = "原型机·零",
            RuntimeHP = 0,
            RuntimeMaxHP = 100,
            IsAlive = false
        });
        report.EnemyFighters.Add(new CombatOutcomeFighterSnapshot {
            Name = "拾荒虫",
            RuntimeHP = 40,
            RuntimeMaxHP = 40,
            IsAlive = true
        });
        report.TimelineEvents.Add(new CombatTimelineEvent {
            Sequence = 0,
            EventType = CombatTimelineEventType.TurnStarted,
            Title = "敌方回合开始"
        });
        report.TimelineEvents.Add(new CombatTimelineEvent {
            Sequence = 1,
            EventType = CombatTimelineEventType.DamageDealt,
            Title = "造成伤害",
            Detail = "拾荒虫 使用 scavenger_basic_attack 对 原型机·零 造成 10 点伤害。"
        });
        report.TimelineEvents.Add(new CombatTimelineEvent {
            Sequence = 2,
            EventType = CombatTimelineEventType.Outcome,
            Title = "战斗失败",
            Detail = "失败原因：PlayerHpDepleted。"
        });
        return report;
    }

    private static CoreBackend CreateCoreWithEmptyGrid() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();

        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        doll.RuntimeGrid = new BackpackGrid(doll.Chassis);
        GridSolver.RecalculateAllEffects(doll);
        return core;
    }
}
