using System.Collections.Generic;
using UnityEngine;
using System.IO;
using Newtonsoft.Json;

public static class ConfigManager {
    public static Dictionary<string, DollEntity> Dolls = new Dictionary<string, DollEntity>();
    public static Dictionary<string, ChassisComponent> Chassis = new Dictionary<string, ChassisComponent>();
    public static Dictionary<string, ItemEntity> Items = new Dictionary<string, ItemEntity>();
    public static Dictionary<string, MonsterEntity> Monsters = new Dictionary<string, MonsterEntity>();
    public static Dictionary<int, DungeonConfig> Dungeons = new Dictionary<int, DungeonConfig>();
    public static Dictionary<string, ProstheticEntity> Prosthetics = new Dictionary<string, ProstheticEntity>();
    public static Dictionary<string, CraftingRecipeConfig> CraftingRecipes = new Dictionary<string, CraftingRecipeConfig>();

    public static void LoadAllConfigs() {
        ResetAllCaches();

        string basePath = Path.Combine(Application.streamingAssetsPath, "Configs");

        if (!Directory.Exists(basePath)) {
            Debug.LogError($"[ConfigManager] Config directory not found: {basePath}");
            return;
        }

        // 1. Dolls
        LoadConfigsIntoDict(Path.Combine(basePath, "Dolls"), Dolls, d => d.DollID);
        // 2. Chassis
        LoadConfigsIntoDict(Path.Combine(basePath, "Chassis"), Chassis, c => c.ChassisID);
        // 3. Items
        LoadConfigsIntoDict(Path.Combine(basePath, "Items"), Items, i => i.ConfigID);
        // 4. Monsters
        LoadConfigsIntoDict(Path.Combine(basePath, "Monsters"), Monsters, m => m.MonsterID);
        // 5. Dungeons
        LoadConfigsIntoDict(Path.Combine(basePath, "Dungeons"), Dungeons, d => d.LayerID);
        // 6. Prosthetics
        LoadConfigsIntoDict(Path.Combine(basePath, "Prosthetics"), Prosthetics, p => p.ProstheticID);
        // 7. CraftingRecipes
        LoadConfigsIntoDict(Path.Combine(basePath, "CraftingRecipes"), CraftingRecipes, c => c.RecipeID);

        Debug.Log($"[ConfigManager] Configs loaded successfully! Items: {Items.Count}, Monsters: {Monsters.Count}, Dungeons: {Dungeons.Count}");
    }

    public static void ResetAllCaches() {
        Dolls.Clear();
        Chassis.Clear();
        Items.Clear();
        Monsters.Clear();
        Dungeons.Clear();
        Prosthetics.Clear();
        CraftingRecipes.Clear();
    }

    private static void LoadConfigsIntoDict<K, T>(string dirPath, Dictionary<K, T> dict, System.Func<T, K> keySelector) {
        if (!Directory.Exists(dirPath)) return;

        string[] files = Directory.GetFiles(dirPath, "*.json");
        foreach (string file in files) {
            try {
                string json = File.ReadAllText(file);
                T obj = JsonConvert.DeserializeObject<T>(json);
                if (obj != null) {
                    K key = keySelector(obj);
                    if (!dict.ContainsKey(key)) {
                        dict.Add(key, obj);
                    } else {
                        Debug.LogWarning($"[ConfigManager] Duplicate key found: {key} in {typeof(T).Name}");
                    }
                }
            } catch (System.Exception e) {
                Debug.LogError($"[ConfigManager] Failed to load {file}: {e.Message}");
            }
        }
    }

    // Factory methods
    public static ItemEntity CreateItem(string configID) {
        if (Items.TryGetValue(configID, out ItemEntity template)) {
            // Simple deep copy using JSON serialization
            string json = JsonConvert.SerializeObject(template);
            ItemEntity newItem = JsonConvert.DeserializeObject<ItemEntity>(json);
            newItem.InstanceID = System.Guid.NewGuid().ToString();
            return newItem;
        }
        Debug.LogError($"[ConfigManager] Item ConfigID not found: {configID}");
        return null;
    }
}
