using System.Collections.Generic;
using UnityEngine;

public static class CombatOutcomeReportSmokeTest {
    private static CombatOutcomeReport _lastReport;

    public static void Run() {
        Debug.Log("=== Running Combat Outcome Report Smoke Test ===");

        TestVictoryOutcomeReport();
        TestHpDefeatOutcomeReport();

        Debug.Log("=== Combat Outcome Report Smoke Test Finished ===");
    }

    private static void TestVictoryOutcomeReport() {
        CoreBackend core = BootstrapCore();
        _lastReport = null;
        CombatEventBus.OnCombatOutcomePrepared += CaptureOutcomeReport;

        try {
            core.Combat.StartCombat(new List<string> { "mob_scavenger_bug" });
            FighterEntity enemy = core.Combat.EnemyFaction.Fighters[0];
            enemy.TakeDamage(enemy.RuntimeMaxHP);
            core.Combat.EndPlayerTurn();

            CombatOutcomeReport report = core.Combat.LastOutcomeReport;
            bool passed = report != null
                && ReferenceEquals(report, _lastReport)
                && report.OutcomeType == CombatOutcomeType.Victory
                && report.DefeatReason == CombatDefeatReasonType.None
                && report.EnemyAliveCount == 0
                && report.EnemyTotalCount == 1
                && report.PlayerAliveCount == 1
                && report.PlayerFighters.Count == 1
                && report.EnemyFighters.Count == 1
                && report.Summary.Contains("已击败全部敌人");

            if (passed) {
                Debug.Log("Combat Outcome Victory Report PASSED.");
            } else {
                Debug.LogError($"Combat Outcome Victory Report FAILED. Type={report?.OutcomeType}, Reason={report?.DefeatReason}, Enemy={report?.EnemyAliveCount}/{report?.EnemyTotalCount}, Captured={_lastReport != null}");
            }
        } finally {
            CombatEventBus.OnCombatOutcomePrepared -= CaptureOutcomeReport;
        }
    }

    private static void TestHpDefeatOutcomeReport() {
        CoreBackend core = BootstrapCore();
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        doll.Status.HP_Current = 5;

        _lastReport = null;
        CombatEventBus.OnCombatOutcomePrepared += CaptureOutcomeReport;

        try {
            core.Combat.StartCombat(new List<string> { "mob_scavenger_bug" });
            core.Combat.EndPlayerTurn();

            CombatOutcomeReport report = core.Combat.LastOutcomeReport;
            bool passed = report != null
                && ReferenceEquals(report, _lastReport)
                && report.OutcomeType == CombatOutcomeType.Defeat
                && report.DefeatReason == CombatDefeatReasonType.PlayerHpDepleted
                && report.ActiveDollHP == 0
                && report.ActiveDollMaxHP == doll.Status.HP_Max
                && report.EnemyAliveCount == 1
                && report.EnemyTotalCount == 1
                && report.PlayerAliveCount == 0
                && report.Title == "HP 战败"
                && report.Summary.Contains("HP 归零");

            if (passed) {
                Debug.Log("Combat Outcome HP Defeat Report PASSED.");
            } else {
                Debug.LogError($"Combat Outcome HP Defeat Report FAILED. Type={report?.OutcomeType}, Reason={report?.DefeatReason}, HP={report?.ActiveDollHP}/{report?.ActiveDollMaxHP}, Enemy={report?.EnemyAliveCount}/{report?.EnemyTotalCount}, Captured={_lastReport != null}");
            }
        } finally {
            CombatEventBus.OnCombatOutcomePrepared -= CaptureOutcomeReport;
        }
    }

    private static CoreBackend BootstrapCore() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();
        return core;
    }

    private static void CaptureOutcomeReport(CombatOutcomeReport report) {
        _lastReport = report;
    }
}
