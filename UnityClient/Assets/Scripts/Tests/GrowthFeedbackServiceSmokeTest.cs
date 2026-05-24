using System.Linq;
using UnityEngine;

public static class GrowthFeedbackServiceSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Growth Feedback Service Smoke Test ===");

        TestCraftingGapReportsMissingMaterialAndMoney();
        TestMaintenanceSuggestionRestoresDiveBlocker();
        TestReadyStateIncludesDiveSuggestion();

        Debug.Log("=== Growth Feedback Service Smoke Test Finished ===");
    }

    private static void TestCraftingGapReportsMissingMaterialAndMoney() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        player.Money = 100;
        player.StashInventory.Clear();

        GrowthFeedbackReport report = GrowthFeedbackService.BuildReport(player, 1);
        GrowthActionSuggestion suggestion = report.Suggestions.FirstOrDefault(item => item.ActionID == "craft_pros_power_arm");
        GrowthCostRequirementLine scrapLine = suggestion?.ItemRequirements.FirstOrDefault(line => line.ItemID == "loot_gear_scrap");

        bool passed = report.Success
            && suggestion != null
            && suggestion.Status == GrowthActionStatus.MissingRequirements
            && suggestion.MissingMoney == 200
            && scrapLine != null
            && scrapLine.MissingCount == 2;

        if (passed) {
            Debug.Log("Growth Feedback Crafting Gap PASSED.");
        } else {
            Debug.LogError($"Growth Feedback Crafting Gap FAILED. Success={report.Success}, Status={suggestion?.Status}, MissingMoney={suggestion?.MissingMoney ?? -1}, MissingScrap={scrapLine?.MissingCount ?? -1}, Reason={suggestion?.Reason ?? report.Reason}");
        }
    }

    private static void TestMaintenanceSuggestionRestoresDiveBlocker() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        DollEntity doll = player.ActiveDoll;

        player.Money = 250;
        player.StashInventory.Clear();
        AddStashItems(player, "loot_gear_scrap", 1);
        doll.Status.WearAndTear = DiveReadinessService.ExtremeWearThreshold;
        doll.Status.HP_Current = Mathf.Max(1, doll.Status.HP_Max - 10);

        GrowthFeedbackReport before = GrowthFeedbackService.BuildReport(player, 1);
        GrowthActionSuggestion maintenance = before.Suggestions.FirstOrDefault(item => item.ActionID == "maint_basic_patch");
        MaintenanceApplicationResult applied = MaintenanceService.Apply(player, doll, "maint_basic_patch");
        GrowthFeedbackReport after = GrowthFeedbackService.BuildReport(player, 1);

        bool passed = before.Success
            && !before.CanDive
            && maintenance != null
            && maintenance.Status == GrowthActionStatus.Available
            && applied.Success
            && after.CanDive
            && after.Suggestions.Any(item => item.Type == GrowthActionType.DiveReadiness && item.CanExecute);

        if (passed) {
            Debug.Log("Growth Feedback Maintenance Recovery PASSED.");
        } else {
            Debug.LogError($"Growth Feedback Maintenance Recovery FAILED. BeforeCanDive={before.CanDive}, Maintenance={maintenance?.Status}, Applied={applied.Success}, AfterCanDive={after.CanDive}, Reason={maintenance?.Reason ?? applied.Reason ?? before.Reason}");
        }
    }

    private static void TestReadyStateIncludesDiveSuggestion() {
        CoreBackend core = CreateCore();
        GrowthFeedbackReport report = GrowthFeedbackService.BuildReport(core.CurrentPlayer, 1);

        bool passed = report.Success
            && report.CanDive
            && report.Suggestions.Any(item => item.Type == GrowthActionType.DiveReadiness && item.CanExecute);

        if (passed) {
            Debug.Log("Growth Feedback Ready Dive Suggestion PASSED.");
        } else {
            Debug.LogError($"Growth Feedback Ready Dive Suggestion FAILED. Success={report.Success}, CanDive={report.CanDive}, Summary={report.DiveSummary}, Reason={report.Reason}");
        }
    }

    private static CoreBackend CreateCore() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        return core;
    }

    private static void AddStashItems(PlayerProfile player, string configID, int count) {
        for (int i = 0; i < count; i++) {
            ItemEntity item = ConfigManager.CreateItem(configID);
            if (item != null) {
                player.StashInventory.Add(item);
            }
        }
    }
}
