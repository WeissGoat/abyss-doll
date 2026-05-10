using UnityEngine;
using UnityEngine.UI;

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

            RunProstheticCraftAndEffectTest(core);
            RunWorkshopSellPanelUITest(core);

            Debug.Log("=== Workshop Smoke Test Finished ===");
        } catch (System.Exception ex) {
            Debug.LogError($"[WorkshopSmokeTest Crash] {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static void RunWorkshopSellPanelUITest(CoreBackend core) {
        GameObject canvasObj = new GameObject("WorkshopSellUITestCanvas");
        canvasObj.AddComponent<Canvas>();
        canvasObj.AddComponent<GraphicRaycaster>();

        GameObject workshopObj = new GameObject("WorkshopPanel");
        workshopObj.transform.SetParent(canvasObj.transform, false);
        workshopObj.AddComponent<RectTransform>();
        WorkshopUIController controller = workshopObj.AddComponent<WorkshopUIController>();
        controller.moneyText = CreateTestText(workshopObj.transform);
        controller.chassisInfoText = CreateTestText(workshopObj.transform);

        core.CurrentPlayer.ActiveDoll.RuntimeGrid = new BackpackGrid(core.CurrentPlayer.ActiveDoll.Chassis);
        ItemEntity sellTarget = ConfigManager.CreateItem("loot_gear_scrap");
        ((BackpackGrid)core.CurrentPlayer.ActiveDoll.RuntimeGrid).PlaceItem(sellTarget, 0, 0);

        controller.RefreshUI();
        controller.OpenSellPanel();

        bool panelIsSeparate = controller.sellPanel != null && controller.sellPanel.transform.parent == canvasObj.transform;
        bool panelOpened = controller.sellPanel != null && controller.sellPanel.activeSelf;
        bool listBuilt = controller.stashListParent != null && controller.stashListParent.childCount > 0;
        bool prostheticListBuilt = controller.prostheticListParent != null && controller.prostheticListParent.childCount > 0;

        if (panelIsSeparate && panelOpened && listBuilt && prostheticListBuilt) {
            Debug.Log("Workshop Sell Panel UI PASSED.");
        } else {
            Debug.LogError($"Workshop Sell Panel UI FAILED. Separate={panelIsSeparate}, Opened={panelOpened}, SellRows={controller.stashListParent?.childCount ?? 0}, ProstheticRows={controller.prostheticListParent?.childCount ?? 0}");
        }

        controller.CloseSellPanel();
        Object.DestroyImmediate(canvasObj);
    }

    private static void RunProstheticCraftAndEffectTest(CoreBackend core) {
        var player = core.CurrentPlayer;
        var doll = player.ActiveDoll;
        doll.RuntimeGrid = new BackpackGrid(doll.Chassis);
        BackpackGrid grid = doll.RuntimeGrid as BackpackGrid;

        ItemEntity meleeWeapon = ConfigManager.CreateItem("gear_rusty_dagger");
        grid.PlaceItem(meleeWeapon, 0, 0);

        player.Money = 2000;
        AddStashItems(player, "loot_gear_scrap", 3);

        bool craftedPowerArm = core.Workshop.CraftAndEquipProsthetic("craft_pros_power_arm", doll);
        bool hasPowerArm = doll.EquippedProsthetics.Contains("pros_power_arm");
        bool damageBuffed = meleeWeapon.Combat.RuntimeDamage > meleeWeapon.Combat.BaseValue;

        if (craftedPowerArm && hasPowerArm && damageBuffed) {
            Debug.Log("Prosthetic Craft Damage Effect PASSED.");
        } else {
            Debug.LogError($"Prosthetic Craft Damage Effect FAILED. Crafted={craftedPowerArm}, Equipped={hasPowerArm}, Base={meleeWeapon.Combat.BaseValue}, Runtime={meleeWeapon.Combat.RuntimeDamage}");
        }

        player.Money = 2000;
        AddStashItems(player, "loot_gear_scrap", 2);
        bool craftedCooling = core.Workshop.CraftAndEquipProsthetic("craft_pros_cooling_system", doll);
        doll.Status.SAN_Current = 10;
        int beforeSAN = doll.Status.SAN_Current;

        CombatFaction faction = new CombatFaction { Type = FactionType.Player };
        DollFighter fighter = new DollFighter(doll, faction);
        faction.Fighters.Add(fighter);
        CombatEventBus.Publish(CombatEventType.OnCombatEnd, faction);
        fighter.Cleanup();

        bool restoredSAN = doll.Status.SAN_Current == beforeSAN + 2;
        if (craftedCooling && doll.EquippedProsthetics.Contains("pros_cooling_system") && restoredSAN) {
            Debug.Log("Prosthetic Combat End SAN Effect PASSED.");
        } else {
            Debug.LogError($"Prosthetic Combat End SAN Effect FAILED. Crafted={craftedCooling}, Equipped={doll.EquippedProsthetics.Contains("pros_cooling_system")}, SAN={doll.Status.SAN_Current}, Expected={beforeSAN + 2}");
        }
    }

    private static void AddStashItems(PlayerProfile player, string configID, int count) {
        for (int i = 0; i < count; i++) {
            ItemEntity item = ConfigManager.CreateItem(configID);
            if (item != null) {
                player.StashInventory.Add(item);
            }
        }
    }

    private static Text CreateTestText(Transform parent) {
        GameObject obj = new GameObject("TestText");
        obj.transform.SetParent(parent, false);
        return obj.AddComponent<Text>();
    }
}
