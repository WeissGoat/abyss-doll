using UnityEngine;

public static class MaintenanceServiceSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Maintenance Service Smoke Test ===");

        TestBasicPatchRestoresDivePermit();
        TestPurificationRestoresDivePermit();
        TestInsufficientCostDoesNotMutateState();
        TestCostCanConsumeBackpackMaterial();

        Debug.Log("=== Maintenance Service Smoke Test Finished ===");
    }

    private static void TestBasicPatchRestoresDivePermit() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        DollEntity doll = player.ActiveDoll;

        player.Money = 250;
        player.StashInventory.Clear();
        AddStashItems(player, "loot_gear_scrap", 1);
        doll.Status.WearAndTear = DiveReadinessService.ExtremeWearThreshold;
        doll.Status.HP_Current = Mathf.Max(1, doll.Status.HP_Max - 10);

        DiveReadinessResult beforeReadiness = DiveReadinessService.Evaluate(player, 1);
        MaintenanceApplicationResult result = MaintenanceService.Apply(player, doll, "maint_basic_patch");
        DiveReadinessResult afterReadiness = DiveReadinessService.Evaluate(player, 1);

        bool passed = !beforeReadiness.CanDive
            && result.Success
            && player.Money == 200
            && WorkshopCostService.CountOwnedItems(player, "loot_gear_scrap") == 0
            && Mathf.Approximately(doll.Status.WearAndTear, DiveReadinessService.ExtremeWearThreshold - 40f)
            && doll.Status.HP_Current == doll.Status.HP_Max
            && afterReadiness.CanDive;

        if (passed) {
            Debug.Log("Maintenance Basic Patch Restores Dive Permit PASSED.");
        } else {
            Debug.LogError($"Maintenance Basic Patch Restores Dive Permit FAILED. Success={result.Success}, Reason={result.Reason}, Money={player.Money}, Scrap={WorkshopCostService.CountOwnedItems(player, "loot_gear_scrap")}, Wear={doll.Status.WearAndTear}, HP={doll.Status.HP_Current}/{doll.Status.HP_Max}, BeforeDive={beforeReadiness.CanDive}, AfterDive={afterReadiness.CanDive}");
        }
    }

    private static void TestPurificationRestoresDivePermit() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        DollEntity doll = player.ActiveDoll;

        player.Money = 250;
        player.StashInventory.Clear();
        AddStashItems(player, "loot_toxic_filter", 1);
        doll.Status.Corruption = DiveReadinessService.ExtremeCorruptionThreshold;
        doll.Status.SAN_Current = Mathf.Max(1, doll.Status.SAN_Max - 5);

        DiveReadinessResult beforeReadiness = DiveReadinessService.Evaluate(player, 1);
        MaintenanceApplicationResult result = MaintenanceService.Apply(player, doll, "maint_purification_flush");
        DiveReadinessResult afterReadiness = DiveReadinessService.Evaluate(player, 1);

        bool passed = !beforeReadiness.CanDive
            && result.Success
            && player.Money == 170
            && WorkshopCostService.CountOwnedItems(player, "loot_toxic_filter") == 0
            && Mathf.Approximately(doll.Status.Corruption, DiveReadinessService.ExtremeCorruptionThreshold - 40f)
            && doll.Status.SAN_Current == doll.Status.SAN_Max
            && afterReadiness.CanDive;

        if (passed) {
            Debug.Log("Maintenance Purification Restores Dive Permit PASSED.");
        } else {
            Debug.LogError($"Maintenance Purification Restores Dive Permit FAILED. Success={result.Success}, Reason={result.Reason}, Money={player.Money}, Filter={WorkshopCostService.CountOwnedItems(player, "loot_toxic_filter")}, Corruption={doll.Status.Corruption}, SAN={doll.Status.SAN_Current}/{doll.Status.SAN_Max}, BeforeDive={beforeReadiness.CanDive}, AfterDive={afterReadiness.CanDive}");
        }
    }

    private static void TestInsufficientCostDoesNotMutateState() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        DollEntity doll = player.ActiveDoll;

        player.Money = 0;
        player.StashInventory.Clear();
        doll.RuntimeGrid = new BackpackGrid(doll.Chassis);
        doll.Status.WearAndTear = DiveReadinessService.ExtremeWearThreshold;
        doll.Status.HP_Current = 25;

        MaintenanceApplicationResult result = MaintenanceService.Apply(player, doll, "maint_basic_patch");

        bool passed = !result.Success
            && player.Money == 0
            && Mathf.Approximately(doll.Status.WearAndTear, DiveReadinessService.ExtremeWearThreshold)
            && doll.Status.HP_Current == 25
            && result.ConsumedItems.Count == 0;

        if (passed) {
            Debug.Log("Maintenance Insufficient Cost Rollback PASSED.");
        } else {
            Debug.LogError($"Maintenance Insufficient Cost Rollback FAILED. Success={result.Success}, Reason={result.Reason}, Money={player.Money}, Wear={doll.Status.WearAndTear}, HP={doll.Status.HP_Current}, Consumed={result.ConsumedItems.Count}");
        }
    }

    private static void TestCostCanConsumeBackpackMaterial() {
        CoreBackend core = CreateCore();
        PlayerProfile player = core.CurrentPlayer;
        DollEntity doll = player.ActiveDoll;

        player.Money = 250;
        player.StashInventory.Clear();
        doll.RuntimeGrid = new BackpackGrid(doll.Chassis);
        BackpackGrid grid = doll.RuntimeGrid as BackpackGrid;
        ItemEntity scrap = ConfigManager.CreateItem("loot_gear_scrap");
        bool placed = grid != null && scrap != null && grid.PlaceItem(scrap, 0, 0);
        doll.Status.WearAndTear = DiveReadinessService.ExtremeWearThreshold;

        MaintenanceApplicationResult result = MaintenanceService.Apply(player, doll, "maint_basic_patch");

        bool passed = placed
            && result.Success
            && player.Money == 200
            && grid != null
            && !grid.ContainedItems.Contains(scrap)
            && WorkshopCostService.CountOwnedItems(player, "loot_gear_scrap") == 0
            && DiveReadinessService.Evaluate(player, 1).CanDive;

        if (passed) {
            Debug.Log("Maintenance Backpack Material Cost PASSED.");
        } else {
            Debug.LogError($"Maintenance Backpack Material Cost FAILED. Placed={placed}, Success={result.Success}, Reason={result.Reason}, Money={player.Money}, GridCount={grid?.ContainedItems.Count ?? -1}, ScrapOwned={WorkshopCostService.CountOwnedItems(player, "loot_gear_scrap")}");
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
