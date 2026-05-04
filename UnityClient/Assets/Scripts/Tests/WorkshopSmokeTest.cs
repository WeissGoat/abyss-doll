using UnityEngine;

public static class WorkshopSmokeTest {
    public static void Run() {
        try {
            Debug.Log("=== Running Workshop Smoke Test ===");

            CoreBackend core = new CoreBackend();
            core.InitAllSystems();

            GameRoot.Core = core;

            var player = core.CurrentPlayer;
            var doll = player.ActiveDoll;

            if (doll == null || doll.Chassis == null) {
                Debug.LogError("Bootstrap failed: ActiveDoll or Chassis is null.");
                return;
            }

            BackpackGrid grid = doll.RuntimeGrid as BackpackGrid;
            ItemEntity sellTarget = grid?.ContainedItems.Find(item => item != null && item.ConfigID == "gear_wooden_shield");
            if (grid == null || sellTarget == null) {
                Debug.LogError("Backpack sell bootstrap failed: missing runtime grid or wooden shield.");
                return;
            }

            Debug.Log($"[Before] Chassis: {doll.Chassis.ChassisID} (Grid: {doll.Chassis.GridWidth}x{doll.Chassis.GridHeight}), Money: {player.Money}");

            int initialBackpackCount = grid.ContainedItems.Count;
            bool sold = core.Workshop.SellItem(sellTarget, player);

            if (sold && player.Money == sellTarget.BaseValue) {
                Debug.Log("Single Item Sell PASSED.");
            } else {
                Debug.LogError($"Single Item Sell FAILED. Expected money {sellTarget.BaseValue}, got {player.Money}, Sold={sold}");
            }

            if (grid.ContainedItems.Count == initialBackpackCount - 1 && grid.GetItemAt(3, 0) == null) {
                Debug.Log("Single Item Backpack Removal PASSED.");
            } else {
                Debug.LogError($"Single Item Backpack Removal FAILED. Expected backpack count {initialBackpackCount - 1}, got {grid.ContainedItems.Count}");
            }

            player.Money = 1500;
            var coreMaterial = ConfigManager.CreateItem("mat_core_tier1");
            if (coreMaterial == null) {
                Debug.LogError("Config 'mat_core_tier1' not found. Ensure the JSON exists.");
                return;
            }
            player.StashInventory.Add(coreMaterial);

            core.Workshop.UpgradeDollChassis(doll);

            if (player.Money == 500) {
                Debug.Log("Money Deduction PASSED.");
            } else {
                Debug.LogError($"Money Deduction FAILED. Expected 500, got {player.Money}");
            }

            if (player.StashInventory.Count == 0) {
                Debug.Log("Material Deduction PASSED.");
            } else {
                Debug.LogError($"Material Deduction FAILED. Expected 0 items, got {player.StashInventory.Count}");
            }

            if (doll.Chassis.ChassisID == "chassis_lv2_expanded" && doll.Chassis.GridWidth == 5 && doll.Chassis.GridHeight == 5) {
                Debug.Log("Chassis Upgrade PASSED.");
            } else {
                Debug.LogError($"Chassis Upgrade FAILED. Current Chassis: {doll.Chassis.ChassisID} ({doll.Chassis.GridWidth}x{doll.Chassis.GridHeight})");
            }

            Debug.Log("=== Workshop Smoke Test Finished ===");
        } catch (System.Exception ex) {
            Debug.LogError($"[WorkshopSmokeTest Crash] {ex.Message}\n{ex.StackTrace}");
        }
    }
}
