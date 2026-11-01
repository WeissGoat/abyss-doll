using System.Collections.Generic;
using System.IO;
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
        Check(json.Contains("\"Version\": " + SaveService.Version), "save has no version");
        Check(!json.Contains("\"Effects\""), "items are saved whole instead of as config ID plus their own state");
        TestItemsFollowTheConfig(Check);
        TestVersionOneSaveLoads(Check);
        TestWritesOnlyAfterSessionBegins(loaded, Check);

        if (failures.Count == 0) {
            Debug.Log("Save Service PASSED.");
        } else {
            Debug.LogError("Save Service FAILED: " + string.Join("; ", failures));
        }

        Debug.Log("=== Save Service Test Finished ===");
    }

    // Phase 2 M0: a saved item comes back from the current config (a retuned dagger hits harder in an old save), keeps
    // its own state (cell, wear, a monster's junk stamp), and an item removed from the config is dropped, not a crash.
    private static void TestItemsFollowTheConfig(System.Action<bool, string> check) {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        PlayerProfile player = core.CurrentPlayer;
        BackpackGrid grid = (BackpackGrid)player.ActiveDoll.RuntimeGrid;
        ItemEntity dagger = grid.ContainedItems.Find(item => item.ConfigID == "gear_rusty_dagger");
        dagger.Durability = 0.4f;
        dagger.CorruptionLevel = 2;
        ItemEntity junk = ConfigManager.CreateItem("loot_gear_scrap");
        junk.Tags.Add("Junk");
        junk.BaseValue = 0;
        ItemLifecycleService.MarkGroundInventoryItem(player, junk, "Test");
        ItemEntity gone = ConfigManager.CreateItem("loot_rusty_coil");
        gone.ConfigID = "loot_removed_from_config";
        player.StashInventory.Add(gone);
        string json = SaveService.Serialize(player);

        ItemEntity template = ConfigManager.Items["gear_rusty_dagger"];
        int oldDamage = template.Combat.BaseValue;
        string oldName = template.Name;
        try {
            template.Combat.BaseValue = oldDamage + 7;
            template.Name = "retuned dagger";
            SaveFile file = SaveService.Deserialize(json);
            ItemEntity loadedDagger = file.Backpack.Find(item => item.InstanceID == dagger.InstanceID);
            check(loadedDagger != null && loadedDagger.Combat.BaseValue == oldDamage + 7 && loadedDagger.Name == "retuned dagger", "a saved item did not take the current config's numbers");
            check(loadedDagger != null && Mathf.Approximately(loadedDagger.Durability, 0.4f) && loadedDagger.CorruptionLevel == 2
                && loadedDagger.Grid.CurrentPos[0] == dagger.Grid.CurrentPos[0] && loadedDagger.Grid.CurrentPos[1] == dagger.Grid.CurrentPos[1], "a saved item lost its own state");
            ItemEntity loadedJunk = file.Player.StashInventory.Find(item => item.InstanceID == junk.InstanceID);
            check(loadedJunk != null && loadedJunk.Tags.Contains("Junk") && loadedJunk.BaseValue == 0, "a monster's junk stamp was lost");
            check(!file.Player.StashInventory.Exists(item => item.InstanceID == gone.InstanceID) && !file.Player.StashInventory.Contains(null), "an item missing from the config was not dropped cleanly");
        } finally {
            template.Combat.BaseValue = oldDamage;
            template.Name = oldName;
        }
    }

    // A save from the Demo build (version 1: whole items) still loads, with its items placed and rebuilt.
    private static void TestVersionOneSaveLoads(System.Action<bool, string> check) {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        PlayerProfile player = core.CurrentPlayer;
        player.CurrentDay = 4;
        BackpackGrid grid = (BackpackGrid)player.ActiveDoll.RuntimeGrid;
        SaveFile old = new SaveFile { Version = 1, SavedAt = "2026-11-01 01:00", Player = player };
        old.Backpack.AddRange(grid.ContainedItems);
        string json = Newtonsoft.Json.JsonConvert.SerializeObject(old, Newtonsoft.Json.Formatting.Indented, new Newtonsoft.Json.JsonSerializerSettings {
            ReferenceLoopHandling = Newtonsoft.Json.ReferenceLoopHandling.Ignore
        });

        SaveFile file = SaveService.Deserialize(json);
        CoreBackend fresh = new CoreBackend();
        fresh.InitAllSystems();
        GameRoot.Core = fresh;
        SaveService.Restore(fresh, file);
        BackpackGrid loaded = fresh.CurrentPlayer.ActiveDoll.RuntimeGrid as BackpackGrid;
        check(fresh.CurrentPlayer.CurrentDay == 4 && file.Run == null && !PrologueProgress.InPrologue(fresh.CurrentPlayer), "a version 1 save did not load as a town save");
        check(loaded != null && loaded.ContainedItems.Count == grid.ContainedItems.Count, $"a version 1 backpack came back with {loaded?.ContainedItems.Count} of {grid.ContainedItems.Count}");
    }

    // On disk, in a scratch folder: the title-time refresh must not write over the save (the 2026-11-01 lost
    // progress), each write keeps the previous save, and a broken save falls back to it.
    private static void TestWritesOnlyAfterSessionBegins(PlayerProfile player, System.Action<bool, string> check) {
        string folder = Path.Combine(Path.GetTempPath(), "p3_save_test_" + System.Guid.NewGuid().ToString("N"));
        bool sessionWas = SaveService.SessionStarted;
        SaveService.DirectoryOverride = folder;
        SaveService.Enabled = true;
        try {
            SaveService.EndSession();
            check(!SaveService.AutoSave(player) && !File.Exists(SaveService.SavePath), "autosave wrote before the session began");

            SaveService.BeginSession();
            player.Money = 500;
            check(SaveService.AutoSave(player) && File.Exists(SaveService.SavePath), "autosave did not write after the session began");
            player.Money = 600;
            check(SaveService.AutoSave(player) && File.Exists(SaveService.PreviousSavePath), "the previous save was not kept");
            check(SaveService.ReadSave(out _)?.Player.Money == 600, "the latest save was not read back");

            File.WriteAllText(SaveService.SavePath, "{ broken");
            SaveFile fallback = SaveService.ReadSave(out string reason);
            check(fallback?.Player.Money == 500 && !string.IsNullOrEmpty(reason), "a broken save did not fall back to the previous one");
        } finally {
            SaveService.EndSession();
            if (sessionWas) {
                SaveService.BeginSession();
            }

            SaveService.ClearEnabledOverride();
            SaveService.DirectoryOverride = null;
            if (Directory.Exists(folder)) {
                Directory.Delete(folder, true);
            }
        }
    }
}
