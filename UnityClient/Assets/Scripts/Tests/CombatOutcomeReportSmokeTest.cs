using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

public static class CombatOutcomeReportSmokeTest {
    private static CombatOutcomeReport _lastReport;

    public static void Run() {
        Debug.Log("=== Running Combat Outcome Report Smoke Test ===");

        TestVictoryOutcomeReport();
        TestHpDefeatOutcomeReport();
        TestSanCollapseOutcomeReport();
        TestSettlementUIConsumesCombatOutcomeReport();
        TestGameFlowSettlementUsesCombatOutcomeReportForDefeat();

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
                && HasTimelineEvent(report, CombatTimelineEventType.Outcome)
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
                && HasTimelineEvent(report, CombatTimelineEventType.DamageDealt)
                && HasTimelineEvent(report, CombatTimelineEventType.Outcome)
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

    private static void TestSanCollapseOutcomeReport() {
        CoreBackend core = BootstrapCore();
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        doll.Status.SAN_Current = 0;

        _lastReport = null;
        CombatEventBus.OnCombatOutcomePrepared += CaptureOutcomeReport;

        try {
            core.Combat.StartCombat(new List<string> { "mob_scavenger_bug" });

            CombatOutcomeReport report = core.Combat.LastOutcomeReport;
            bool passed = report != null
                && ReferenceEquals(report, _lastReport)
                && core.Combat.CurrentState == CombatState.End
                && report.OutcomeType == CombatOutcomeType.Defeat
                && report.DefeatReason == CombatDefeatReasonType.PlayerSanCollapsed
                && report.ActiveDollSAN == 0
                && report.ActiveDollMaxSAN == doll.Status.SAN_Max
                && report.EnemyAliveCount == 1
                && report.EnemyTotalCount == 1
                && report.PlayerAliveCount == 1
                && report.Title == "SAN 崩溃"
                && HasTimelineEvent(report, CombatTimelineEventType.Outcome)
                && report.Summary.Contains("SAN 归零");

            if (passed) {
                Debug.Log("Combat Outcome SAN Collapse Report PASSED.");
            } else {
                Debug.LogError($"Combat Outcome SAN Collapse Report FAILED. Type={report?.OutcomeType}, Reason={report?.DefeatReason}, SAN={report?.ActiveDollSAN}/{report?.ActiveDollMaxSAN}, Enemy={report?.EnemyAliveCount}/{report?.EnemyTotalCount}, State={core.Combat.CurrentState}, Captured={_lastReport != null}");
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

    private static bool HasTimelineEvent(CombatOutcomeReport report, CombatTimelineEventType eventType) {
        if (report?.TimelineEvents == null) {
            return false;
        }

        return report.TimelineEvents.Exists(entry => entry != null && entry.EventType == eventType);
    }

    private static void TestSettlementUIConsumesCombatOutcomeReport() {
        GameObject canvasObj = CreateCanvas();
        GameObject settlementObj = new GameObject("CombatOutcomeSettlementBindingTestPanel");
        settlementObj.transform.SetParent(canvasObj.transform, false);
        settlementObj.AddComponent<RectTransform>();
        SettlementUIController controller = settlementObj.AddComponent<SettlementUIController>();
        controller.titleText = CreateText(settlementObj.transform, "TitleText");
        controller.summaryText = CreateText(settlementObj.transform, "SummaryText");
        controller.lootText = CreateText(settlementObj.transform, "LootText");
        controller.continueBtn = CreateButton(settlementObj.transform, "ContinueButton");

        CombatOutcomeReport report = new CombatOutcomeReport {
            OutcomeType = CombatOutcomeType.Defeat,
            DefeatReason = CombatDefeatReasonType.PlayerSanCollapsed,
            Title = "SAN 崩溃",
            Summary = "魔偶 SAN 归零。当前 SAN 0/100。",
            ActiveDollName = "原型机·零",
            ActiveDollHP = 91,
            ActiveDollMaxHP = 100,
            ActiveDollSAN = 0,
            ActiveDollMaxSAN = 100,
            PlayerAliveCount = 1,
            EnemyAliveCount = 1,
            EnemyTotalCount = 1,
            TimelineEvents = new List<CombatTimelineEvent> {
                new CombatTimelineEvent {
                    Sequence = 0,
                    EventType = CombatTimelineEventType.CombatStarted,
                    Title = "战斗开始",
                    Detail = "遭遇 1 个敌人。"
                },
                new CombatTimelineEvent {
                    Sequence = 1,
                    EventType = CombatTimelineEventType.Outcome,
                    Title = "战斗失败",
                    Detail = "失败原因：PlayerSanCollapsed。"
                }
            }
        };

        try {
            MethodInfo presentMethod = typeof(SettlementUIController).GetMethod(
                "Present",
                new[] { typeof(CombatOutcomeReport), typeof(System.Action) });
            if (presentMethod == null) {
                Debug.LogError("Combat Outcome Settlement UI Binding FAILED. Present(CombatOutcomeReport, Action) missing.");
                return;
            }

            presentMethod.Invoke(controller, new object[] { report, null });
            string allText = CollectText(canvasObj);
            bool passed = allText.Contains("SAN 崩溃")
                && allText.Contains("魔偶 SAN 归零")
                && allText.Contains("原型机·零")
                && allText.Contains("战斗时间线")
                && allText.Contains("战斗失败")
                && allText.Contains("失败原因");

            if (passed) {
                Debug.Log("Combat Outcome Settlement UI Binding PASSED.");
            } else {
                Debug.LogError($"Combat Outcome Settlement UI Binding FAILED. Text={allText}");
            }
        } finally {
            Object.DestroyImmediate(canvasObj);
        }
    }

    private static GameObject CreateCanvas() {
        GameObject canvasObj = new GameObject("CombatOutcomeSettlementBindingTestCanvas");
        Canvas canvas = canvasObj.AddComponent<Canvas>();
        canvas.renderMode = RenderMode.ScreenSpaceOverlay;
        canvasObj.AddComponent<GraphicRaycaster>();
        return canvasObj;
    }

    private static Text CreateText(Transform parent, string name) {
        GameObject textObj = new GameObject(name);
        textObj.transform.SetParent(parent, false);
        textObj.AddComponent<RectTransform>();
        return textObj.AddComponent<Text>();
    }

    private static Button CreateButton(Transform parent, string name) {
        GameObject buttonObj = new GameObject(name);
        buttonObj.transform.SetParent(parent, false);
        buttonObj.AddComponent<RectTransform>();
        buttonObj.AddComponent<Image>();
        return buttonObj.AddComponent<Button>();
    }

    private static string CollectText(GameObject root) {
        Text[] texts = root.GetComponentsInChildren<Text>(true);
        return string.Join("\n", texts.Select(text => text != null ? text.text : string.Empty));
    }

    private static void TestGameFlowSettlementUsesCombatOutcomeReportForDefeat() {
        CoreBackend core = BootstrapCore();
        core.CurrentPlayer.ActiveDoll.Status.SAN_Current = 0;
        core.Combat.StartCombat(new List<string> { "mob_scavenger_bug" });
        CombatOutcomeReport report = core.Combat.LastOutcomeReport;
        if (report == null || report.OutcomeType != CombatOutcomeType.Defeat) {
            Debug.LogError($"Combat Outcome GameFlow Settlement Binding FAILED. Missing defeat report. Type={report?.OutcomeType}");
            return;
        }

        GameObject canvasObj = CreateCanvas();
        GameObject flowObj = new GameObject("CombatOutcomeGameFlowBindingTestFlow");
        GameObject settlementObj = new GameObject("CombatOutcomeGameFlowBindingTestSettlementPanel");
        settlementObj.transform.SetParent(canvasObj.transform, false);
        settlementObj.AddComponent<RectTransform>();
        SettlementUIController settlement = settlementObj.AddComponent<SettlementUIController>();
        settlement.titleText = CreateText(settlementObj.transform, "TitleText");
        settlement.summaryText = CreateText(settlementObj.transform, "SummaryText");
        settlement.lootText = CreateText(settlementObj.transform, "LootText");
        settlement.continueBtn = CreateButton(settlementObj.transform, "ContinueButton");
        settlementObj.SetActive(false);

        try {
            GameFlowController flow = flowObj.AddComponent<GameFlowController>();
            flow.settlementPanel = settlementObj;

            MethodInfo enterSettlementMethod = typeof(GameFlowController).GetMethod(
                "OnEnterSettlementScreen",
                BindingFlags.Instance | BindingFlags.NonPublic);
            if (enterSettlementMethod == null) {
                Debug.LogError("Combat Outcome GameFlow Settlement Binding FAILED. OnEnterSettlementScreen missing.");
                return;
            }

            DungeonSettlementResult dungeonResult = new DungeonSettlementResult {
                IsVictory = false,
                LostCount = 1,
                LostEstimatedValue = 10,
                StashCountAfterSettlement = 0
            };

            enterSettlementMethod.Invoke(flow, new object[] { dungeonResult });
            string allText = CollectText(canvasObj);
            bool passed = allText.Contains(report.Title)
                && allText.Contains(report.Summary)
                && allText.Contains(report.ActiveDollName)
                && report.TimelineEvents.Count > 0
                && allText.Contains(report.TimelineEvents[report.TimelineEvents.Count - 1].Title);

            if (passed) {
                Debug.Log("Combat Outcome GameFlow Settlement Binding PASSED.");
            } else {
                Debug.LogError($"Combat Outcome GameFlow Settlement Binding FAILED. Text={allText}");
            }
        } finally {
            Object.DestroyImmediate(flowObj);
            Object.DestroyImmediate(canvasObj);
        }
    }
}
