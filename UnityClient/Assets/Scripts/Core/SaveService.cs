using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json;
using UnityEngine;

[Serializable]
public class SaveFile {
    public int Version = SaveService.Version;
    public string SavedAt;
    public PlayerProfile Player;
    // The backpack lives in the doll's runtime grid, which is not serialized; its items are stored here with their cells.
    public List<ItemEntity> Backpack = new List<ItemEntity>();
}

// One autosave slot (M2 batch 5). The game saves only in town: entering the workshop, after the night, and whenever
// the town state changed (AutoSave skips identical writes). A dive in progress is not saved; quitting mid-dive
// continues from the last town save. Off in the Editor unless switched on (Tools/P3), and off for the build
// self-check (-p3smoke) or with -p3nosave, so tests never touch the producer's save.
public static class SaveService {
    public const int Version = 1;
    public const string FileName = "save_1.json";

    private static string _lastWritten;
    private static bool? _enabledOverride;

    public static bool Enabled {
        get {
            if (_enabledOverride.HasValue) {
                return _enabledOverride.Value;
            }

            if (Application.isEditor) {
                return P3DevSettings.UseSaveInEditor;
            }

            string[] args = Environment.GetCommandLineArgs();
            return Array.IndexOf(args, "-p3smoke") < 0 && Array.IndexOf(args, "-p3nosave") < 0;
        }
        set { _enabledOverride = value; }
    }

    public static string SavePath => Path.Combine(Application.persistentDataPath, FileName);

    public static bool HasSave => Enabled && File.Exists(SavePath);

    public static string Serialize(PlayerProfile player) {
        SaveFile file = new SaveFile {
            SavedAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            Player = player
        };
        BackpackGrid grid = player?.ActiveDoll?.RuntimeGrid as BackpackGrid;
        if (grid != null) {
            file.Backpack.AddRange(grid.ContainedItems);
        }

        return JsonConvert.SerializeObject(file, Formatting.Indented, new JsonSerializerSettings {
            ReferenceLoopHandling = ReferenceLoopHandling.Ignore
        });
    }

    public static SaveFile Deserialize(string json) {
        SaveFile file = JsonConvert.DeserializeObject<SaveFile>(json);
        if (file?.Player == null) {
            throw new InvalidDataException("The save has no player data.");
        }

        PlayerProfile player = file.Player;
        player.StashInventory = player.StashInventory ?? new List<ItemEntity>();
        player.DumpBox = player.DumpBox ?? new List<ItemEntity>();
        player.Showcase = player.Showcase ?? new List<ItemEntity>();
        player.LastDiveLoadout = player.LastDiveLoadout ?? new List<LoadoutSlotRecord>();
        file.Backpack = file.Backpack ?? new List<ItemEntity>();
        return file;
    }

    // Writes the save when the state differs from the last write. Returns whether a file was written.
    public static bool AutoSave(PlayerProfile player) {
        if (!Enabled || player?.ActiveDoll == null) {
            return false;
        }

        try {
            string json = Serialize(player);
            if (json == _lastWritten) {
                return false;
            }

            // The timestamp changes every minute; compare without it so an unchanged state is not rewritten.
            string withoutTime = StripTime(json);
            if (_lastWritten != null && withoutTime == StripTime(_lastWritten)) {
                return false;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(SavePath));
            string temp = SavePath + ".tmp";
            File.WriteAllText(temp, json);
            if (File.Exists(SavePath)) {
                File.Delete(SavePath);
            }
            File.Move(temp, SavePath);
            _lastWritten = json;
            return true;
        } catch (Exception e) {
            Debug.LogWarning($"[SaveService] Autosave failed: {e.Message}");
            return false;
        }
    }

    public static SaveFile ReadSave(out string reason) {
        reason = string.Empty;
        if (!HasSave) {
            reason = "没有存档。";
            return null;
        }

        try {
            return Deserialize(File.ReadAllText(SavePath));
        } catch (Exception e) {
            reason = "存档读不出来：" + e.Message;
            Debug.LogWarning("[SaveService] " + reason);
            return null;
        }
    }

    // Replaces the running player with the saved one and rebuilds the backpack grid.
    public static void Restore(CoreBackend core, SaveFile file) {
        if (core == null || file?.Player == null) {
            return;
        }

        core.CurrentPlayer?.ActiveDoll?.CleanupRuntime();
        core.CurrentPlayer = file.Player;
        DollEntity doll = file.Player.ActiveDoll;
        if (doll?.Chassis != null) {
            BackpackGrid grid = new BackpackGrid(doll.Chassis);
            doll.RuntimeGrid = grid;
            foreach (ItemEntity item in file.Backpack) {
                if (item?.Grid?.CurrentPos == null || item.Grid.CurrentPos.Length < 2
                    || !grid.PlaceItem(item, item.Grid.CurrentPos[0], item.Grid.CurrentPos[1], item.Grid.Rotation)) {
                    Debug.LogWarning($"[SaveService] Backpack item [{item?.Name}] did not fit back; moved to the stash.");
                    ItemLifecycleService.MarkGroundInventoryItem(file.Player, item, "SaveRestore");
                }
            }

            doll.InitializeRuntime();
            GridSolver.RecalculateAllEffects(doll);
        }

        _lastWritten = null;
        Debug.Log($"[SaveService] Restored day {file.Player.CurrentDay}, money {file.Player.Money}, saved {file.SavedAt}.");
    }

    public static void Delete() {
        _lastWritten = null;
        if (Enabled && File.Exists(SavePath)) {
            File.Delete(SavePath);
        }
    }

    private static string StripTime(string json) {
        int start = json.IndexOf("\"SavedAt\"", StringComparison.Ordinal);
        if (start < 0) {
            return json;
        }

        int end = json.IndexOf('\n', start);
        return end < 0 ? json.Substring(0, start) : json.Substring(0, start) + json.Substring(end);
    }
}
