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
    public static Dictionary<string, RewardConfig> Rewards = new Dictionary<string, RewardConfig>();
    public static Dictionary<string, MaintenanceConfig> MaintenanceConfigs = new Dictionary<string, MaintenanceConfig>();
    public static Dictionary<string, EconomyConfig> EconomyConfigs = new Dictionary<string, EconomyConfig>();
    public static Dictionary<string, FactionConfig> Factions = new Dictionary<string, FactionConfig>();
    public static Dictionary<string, OrderConfig> Orders = new Dictionary<string, OrderConfig>();
    public static Dictionary<string, RumorConfig> Rumors = new Dictionary<string, RumorConfig>();
    public static NarrativeConfigDatabase Narrative = new NarrativeConfigDatabase();

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
        // 8. Rewards
        LoadConfigsIntoDict(Path.Combine(basePath, "Rewards"), Rewards, r => r.RewardID);
        // 9. Maintenance
        LoadConfigsIntoDict(Path.Combine(basePath, "Maintenance"), MaintenanceConfigs, m => m.MaintenanceID);
        // 10. Economy
        LoadConfigsIntoDict(Path.Combine(basePath, "Economy"), EconomyConfigs, e => e.EconomyConfigID);
        // 11. Factions
        LoadConfigsIntoDict(Path.Combine(basePath, "Factions"), Factions, f => f.FactionID);
        // 12. Orders
        LoadConfigsIntoDict(Path.Combine(basePath, "Orders"), Orders, o => o.OrderID);
        // 13. Rumors
        LoadConfigsIntoDict(Path.Combine(basePath, "Rumors"), Rumors, r => r.RumorID);
        // 14. Narrative
        Narrative = NarrativeConfigDatabase.LoadFromDirectory(Path.Combine(basePath, "Narrative"));

        Debug.Log($"[ConfigManager] Configs loaded successfully! Items: {Items.Count}, Monsters: {Monsters.Count}, Dungeons: {Dungeons.Count}, Rewards: {Rewards.Count}, Maintenance: {MaintenanceConfigs.Count}, Economy: {EconomyConfigs.Count}, Factions: {Factions.Count}, Orders: {Orders.Count}, Rumors: {Rumors.Count}, NarrativeNodes: {Narrative.NodeCount}, NarrativeTriggers: {Narrative.TriggerCount}");
    }

    public static void ResetAllCaches() {
        Dolls.Clear();
        Chassis.Clear();
        Items.Clear();
        Monsters.Clear();
        Dungeons.Clear();
        Prosthetics.Clear();
        CraftingRecipes.Clear();
        Rewards.Clear();
        MaintenanceConfigs.Clear();
        EconomyConfigs.Clear();
        Factions.Clear();
        Orders.Clear();
        Rumors.Clear();
        Narrative = new NarrativeConfigDatabase();
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
            newItem.OwnerScope = ItemOwnerScope.Unknown;
            newItem.ContainerType = ItemContainerType.Generated;
            newItem.Durability = newItem.Durability <= 0f ? 1f : newItem.Durability;
            if (newItem.Tags == null) {
                newItem.Tags = new List<string>();
            }
            if (newItem.DynamicTags == null) {
                newItem.DynamicTags = new List<string>();
            }
            return newItem;
        }
        Debug.LogError($"[ConfigManager] Item ConfigID not found: {configID}");
        return null;
    }
}
