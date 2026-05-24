using System.Linq;
using UnityEngine;

public static class GrowthReadabilityTextServiceSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Growth Readability Text Service Smoke Test ===");

        TestReadyDiveSnapshot();
        TestMaintenanceBlockerSnapshot();
        TestCraftingGapSnapshot();

        Debug.Log("=== Growth Readability Text Service Smoke Test Finished ===");
    }

    private static void TestReadyDiveSnapshot() {
        CoreBackend core = CreateCore();
        GrowthReadabilitySnapshot snapshot = GrowthReadabilityTextService.BuildSnapshot(core.CurrentPlayer, 1);
        GrowthReadabilityActionLine diveLine = snapshot.ActionLines.FirstOrDefault(line => line.Type == GrowthActionType.DiveReadiness);

        bool passed = snapshot.Success
            && snapshot.CanDive
            && snapshot.DiveStatusText.Contains("可以下潜")
            && diveLine != null
            && diveLine.CanExecute
            && snapshot.CombinedText.Contains("局外整备")
            && snapshot.CombinedText.Contains("建议行动");

        if (passed) {
            Debug.Log("Growth Readability Ready Dive Snapshot PASSED.");
        } else {
            Debug.LogError($"Growth Readability Ready Dive Snapshot FAILED. Success={snapshot.Success}, CanDive={snapshot.CanDive}, DiveLine={diveLine != null}, Text={snapshot.CombinedText}");
        }
    }

    private static void TestMaintenanceBlockerSnapshot() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        DollEntity doll = player.ActiveDoll;

        player.Money = 250;
        player.StashInventory.Clear();
        AddStashItems(player, "loot_gear_scrap", 1);
        doll.Status.WearAndTear = DiveReadinessService.ExtremeWearThreshold;
        doll.Status.HP_Current = Mathf.Max(1, doll.Status.HP_Max - 10);

        GrowthReadabilitySnapshot snapshot = GrowthReadabilityTextService.BuildSnapshot(player, 1);
        GrowthReadabilityActionLine maintenanceLine = snapshot.ActionLines.FirstOrDefault(line => line.ActionID == "maint_basic_patch");

        bool passed = snapshot.Success
            && !snapshot.CanDive
            && snapshot.DiveStatusText.Contains("暂不能下潜")
            && snapshot.IssueLines.Any(line => line.Contains("阻断"))
            && maintenanceLine != null
            && maintenanceLine.CanExecute
            && maintenanceLine.StatusText == "可执行"
            && snapshot.CombinedText.Contains("下潜检查");

        if (passed) {
            Debug.Log("Growth Readability Maintenance Blocker Snapshot PASSED.");
        } else {
            Debug.LogError($"Growth Readability Maintenance Blocker Snapshot FAILED. Success={snapshot.Success}, CanDive={snapshot.CanDive}, Maintenance={maintenanceLine?.StatusText}, Issues={snapshot.IssueLines.Count}, Text={snapshot.CombinedText}");
        }
    }

    private static void TestCraftingGapSnapshot() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        player.Money = 100;
        player.StashInventory.Clear();

        GrowthReadabilitySnapshot snapshot = GrowthReadabilityTextService.BuildSnapshot(player, 1);
        GrowthReadabilityActionLine craftLine = snapshot.ActionLines.FirstOrDefault(line => line.ActionID == "craft_pros_power_arm");

        bool passed = snapshot.Success
            && craftLine != null
            && craftLine.Status == GrowthActionStatus.MissingRequirements
            && craftLine.MissingMoney == 200
            && craftLine.MissingItemLines.Any(line => line.Contains("缺 2 个"))
            && craftLine.CostText.Contains("金币 100/300")
            && snapshot.MissingRequirementActionCount > 0;

        if (passed) {
            Debug.Log("Growth Readability Crafting Gap Snapshot PASSED.");
        } else {
            Debug.LogError($"Growth Readability Crafting Gap Snapshot FAILED. Success={snapshot.Success}, Craft={craftLine?.Status}, MissingMoney={craftLine?.MissingMoney ?? -1}, MissingItems={craftLine?.MissingItemLines.Count ?? -1}, Text={snapshot.CombinedText}");
        }
    }

    private static CoreBackend CreateCore() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();
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
