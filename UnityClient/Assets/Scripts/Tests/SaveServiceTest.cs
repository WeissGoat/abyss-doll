using System.Collections.Generic;
using UnityEngine;

// M2 batch 5: a town save round-trips the player (calendar, money, debt, Zero's state, backpack cells, stash, the
// night's dump box and showcase, last loadout) without touching the save file on disk.
public static class SaveServiceTest {
    public static void Run() {
        Debug.Log("=== Running Save Service Test ===");

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        PlayerProfile player = core.CurrentPlayer;
        DollEntity doll = player.ActiveDoll;
        BackpackGrid grid = (BackpackGrid)doll.RuntimeGrid;
        List<string> failures = new List<string>();
        void Check(bool condition, string message) {
            if (!condition) {
                failures.Add(message);
            }
        }

        player.CurrentDay = 9;
        player.Money = 777;
        player.EconomyDebtAmount = 120;
        player.IsNight = true;
        doll.Status.HP_Current = 42;
        doll.Status.WearAndTear = 33f;
        ItemEntity stashItem = ConfigManager.CreateItem("loot_rusty_coil");
        ItemLifecycleService.MarkGroundInventoryItem(player, stashItem, "Test");
        ItemEntity dumped = ConfigManager.CreateItem("loot_gear_scrap");
        ItemLifecycleService.MarkGroundInventoryItem(player, dumped, "Test");
        NightBusinessService.TryMoveToDumpBox(player, dumped, out _);
        ItemEntity shown = ConfigManager.CreateItem("trade_miner_lamp");
        ItemLifecycleService.MarkGroundInventoryItem(player, shown, "Test");
        NightBusinessService.TryMoveToShowcase(player, shown, out _);
        ItemEntity dagger = grid.ContainedItems.Find(item => item.ConfigID == "gear_rusty_dagger");
        int[] daggerCell = { dagger.Grid.CurrentPos[0], dagger.Grid.CurrentPos[1] };
        int backpackCount = grid.ContainedItems.Count;

        string json = SaveService.Serialize(player);
        SaveFile file = SaveService.Deserialize(json);

        CoreBackend fresh = new CoreBackend();
        fresh.InitAllSystems();
        GameRoot.Core = fresh;
        SaveService.Restore(fresh, file);
        PlayerProfile loaded = fresh.CurrentPlayer;
        BackpackGrid loadedGrid = loaded.ActiveDoll?.RuntimeGrid as BackpackGrid;

        Check(loaded.CurrentDay == 9 && loaded.Money == 777 && loaded.EconomyDebtAmount == 120 && loaded.IsNight, "calendar / money / debt / night not restored");
        Check(loaded.ActiveDoll.Status.HP_Current == 42 && Mathf.Approximately(loaded.ActiveDoll.Status.WearAndTear, 33f), "doll status not restored");
        Check(loadedGrid != null && loadedGrid.ContainedItems.Count == backpackCount, $"backpack has {loadedGrid?.ContainedItems.Count} of {backpackCount}");
        ItemEntity loadedDagger = loadedGrid?.ContainedItems.Find(item => item.ConfigID == "gear_rusty_dagger");
        Check(loadedDagger != null && loadedDagger.Grid.CurrentPos[0] == daggerCell[0] && loadedDagger.Grid.CurrentPos[1] == daggerCell[1] && loadedGrid.GetItemAt(daggerCell[0], daggerCell[1]) == loadedDagger, "dagger not back in its cell");
        Check(loaded.StashInventory.Exists(item => item.InstanceID == stashItem.InstanceID), "stash item lost");
        Check(loaded.DumpBox.Count == 1 && loaded.DumpBox[0].InstanceID == dumped.InstanceID, "dump box lost");
        Check(loaded.Showcase.Count == 1 && loaded.Showcase[0].Grid.CurrentPos != null, "showcase lost");
        Check(loaded.ActiveDoll.Chassis != null && loaded.ActiveDoll.Chassis.ChassisID == doll.Chassis.ChassisID, "chassis lost");
        Check(json.Contains("\"Version\": 1"), "save has no version");

        if (failures.Count == 0) {
            Debug.Log("Save Service PASSED.");
        } else {
            Debug.LogError("Save Service FAILED: " + string.Join("; ", failures));
        }

        Debug.Log("=== Save Service Test Finished ===");
    }
}
