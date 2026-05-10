using System;
using System.Collections.Generic;
using System.IO;
using Newtonsoft.Json.Linq;
using UnityEngine;

public class ConfigValidationReport {
    public readonly List<string> Errors = new List<string>();
    public readonly List<string> Warnings = new List<string>();

    public bool IsValid => Errors.Count == 0;

    public void AddError(string message) {
        if (!string.IsNullOrEmpty(message)) {
            Errors.Add(message);
        }
    }

    public void AddWarning(string message) {
        if (!string.IsNullOrEmpty(message)) {
            Warnings.Add(message);
        }
    }

    public void LogSummary() {
        foreach (string warning in Warnings) {
            Debug.LogWarning($"[ConfigValidator] {warning}");
        }

        foreach (string error in Errors) {
            Debug.LogError($"[ConfigValidator] {error}");
        }

        Debug.Log($"[ConfigValidator] Completed. Errors={Errors.Count}, Warnings={Warnings.Count}");
    }
}

public static class ConfigValidator {
    private static readonly HashSet<string> MetadataOnlyTags = new HashSet<string>(StringComparer.OrdinalIgnoreCase) {
        "Material",
        "CoreMaterial",
        "Toxic",
        "Cursed"
    };

    public static ConfigValidationReport ValidateLoadedConfigs(bool includeVisualAssets = true) {
        ConfigValidationReport report = new ConfigValidationReport();

        ValidateItems(report);
        ValidateDolls(report);
        ValidateChassis(report);
        ValidateProsthetics(report);
        ValidateCraftingRecipes(report);
        ValidateRewards(report);
        ValidateMonsters(report);
        ValidateDungeons(report);

        if (includeVisualAssets) {
            ValidateVisualAssets(report);
        }

        return report;
    }

    private static void ValidateItems(ConfigValidationReport report) {
        foreach (var kvp in ConfigManager.Items) {
            ItemEntity item = kvp.Value;
            if (item == null) {
                report.AddError($"Item [{kvp.Key}] deserialized as null.");
                continue;
            }

            if (string.IsNullOrEmpty(item.ConfigID)) {
                report.AddError($"Item file loaded with empty ConfigID under key [{kvp.Key}].");
            }

            if (!Enum.TryParse(item.ItemType, out ItemType _)) {
                report.AddError($"Item [{item.ConfigID}] has unknown ItemType [{item.ItemType}].");
            }

            if (!Enum.TryParse(item.Rarity, out ItemRarity _)) {
                report.AddError($"Item [{item.ConfigID}] has unknown Rarity [{item.Rarity}].");
            }

            if (item.Grid == null || item.Grid.Shape == null || item.Grid.Shape.Length == 0) {
                report.AddError($"Item [{item.ConfigID}] has no grid shape.");
            }

            ValidateCombatConfig(report, item.ConfigID, item.Combat);
            WarnMetadataOnlyTags(report, $"Item [{item.ConfigID}]", item.Tags);
        }
    }

    private static void ValidateCombatConfig(ConfigValidationReport report, string ownerID, ItemCombatComponent combat) {
        if (combat == null) {
            return;
        }

        if (!Enum.TryParse(combat.TriggerType, out TriggerType _)) {
            report.AddError($"[{ownerID}] has unknown TriggerType [{combat.TriggerType}].");
        }

        if (!Enum.TryParse(combat.DamageType, out DamageType _)) {
            report.AddError($"[{ownerID}] has unknown DamageType [{combat.DamageType}].");
        }

        ValidateEffects(report, ownerID, combat.Effects);
    }

    private static void ValidateDolls(ConfigValidationReport report) {
        foreach (var kvp in ConfigManager.Dolls) {
            DollEntity doll = kvp.Value;
            if (doll == null) {
                report.AddError($"Doll [{kvp.Key}] deserialized as null.");
                continue;
            }

            if (!ConfigManager.Chassis.ContainsKey(doll.DefaultChassisID)) {
                report.AddError($"Doll [{doll.DollID}] references missing DefaultChassisID [{doll.DefaultChassisID}].");
                continue;
            }

            ValidateDollInitialItems(report, doll);
        }
    }

    private static void ValidateDollInitialItems(ConfigValidationReport report, DollEntity doll) {
        ChassisComponent chassis = ConfigManager.Chassis[doll.DefaultChassisID];
        BackpackGrid grid = new BackpackGrid(chassis);

        foreach (DollInitialItemConfig initialItem in doll.InitialItems) {
            if (initialItem == null || string.IsNullOrEmpty(initialItem.ItemConfigID)) {
                report.AddError($"Doll [{doll.DollID}] has an empty initial item entry.");
                continue;
            }

            ItemEntity item = ConfigManager.CreateItem(initialItem.ItemConfigID);
            if (item == null) {
                report.AddError($"Doll [{doll.DollID}] references missing initial item [{initialItem.ItemConfigID}].");
                continue;
            }

            if (item.Grid != null) {
                item.Grid.Rotation = initialItem.Rotation;
            }

            if (!grid.PlaceItem(item, initialItem.X, initialItem.Y)) {
                report.AddError($"Doll [{doll.DollID}] initial item [{initialItem.ItemConfigID}] cannot be placed at ({initialItem.X},{initialItem.Y}) rotation={initialItem.Rotation}.");
            }
        }
    }

    private static void ValidateChassis(ConfigValidationReport report) {
        foreach (var kvp in ConfigManager.Chassis) {
            ChassisComponent chassis = kvp.Value;
            if (chassis == null) {
                report.AddError($"Chassis [{kvp.Key}] deserialized as null.");
                continue;
            }

            if (chassis.GridWidth <= 0 || chassis.GridHeight <= 0) {
                report.AddError($"Chassis [{chassis.ChassisID}] has invalid grid size {chassis.GridWidth}x{chassis.GridHeight}.");
            }

            if (chassis.UpgradeCost == null) {
                continue;
            }

            if (!string.IsNullOrEmpty(chassis.UpgradeCost.NextChassisID) && !ConfigManager.Chassis.ContainsKey(chassis.UpgradeCost.NextChassisID)) {
                report.AddError($"Chassis [{chassis.ChassisID}] references missing NextChassisID [{chassis.UpgradeCost.NextChassisID}].");
            }

            ValidateCost(report, $"Chassis [{chassis.ChassisID}] UpgradeCost", chassis.UpgradeCost);
        }
    }

    private static void ValidateProsthetics(ConfigValidationReport report) {
        foreach (var kvp in ConfigManager.Prosthetics) {
            ProstheticEntity prosthetic = kvp.Value;
            if (prosthetic == null) {
                report.AddError($"Prosthetic [{kvp.Key}] deserialized as null.");
                continue;
            }

            ValidateEffects(report, $"Prosthetic [{prosthetic.ProstheticID}]", prosthetic.Effects);
        }
    }

    private static void ValidateCraftingRecipes(ConfigValidationReport report) {
        foreach (var kvp in ConfigManager.CraftingRecipes) {
            CraftingRecipeConfig recipe = kvp.Value;
            if (recipe == null) {
                report.AddError($"Crafting recipe [{kvp.Key}] deserialized as null.");
                continue;
            }

            if (!ConfigManager.Prosthetics.ContainsKey(recipe.TargetProstheticID)) {
                report.AddError($"Crafting recipe [{recipe.RecipeID}] references missing prosthetic [{recipe.TargetProstheticID}].");
            }

            ValidateCost(report, $"Crafting recipe [{recipe.RecipeID}] Cost", recipe.Cost);
        }
    }

    private static void ValidateRewards(ConfigValidationReport report) {
        foreach (var kvp in ConfigManager.Rewards) {
            RewardConfig reward = kvp.Value;
            if (reward == null) {
                report.AddError($"Reward [{kvp.Key}] deserialized as null.");
                continue;
            }

            ValidateRewardEntries(report, reward.RewardID, "Guaranteed", reward.Guaranteed, false);

            if (reward.WeightedPools == null) {
                continue;
            }

            foreach (RewardPool pool in reward.WeightedPools) {
                if (pool == null) {
                    report.AddError($"Reward [{reward.RewardID}] has a null weighted pool.");
                    continue;
                }

                if (pool.RollCount < 0) {
                    report.AddError($"Reward [{reward.RewardID}] pool [{pool.PoolID}] has negative RollCount [{pool.RollCount}].");
                }

                ValidateRewardEntries(report, reward.RewardID, pool.PoolID, pool.Entries, true);
            }
        }
    }

    private static void ValidateRewardEntries(ConfigValidationReport report, string rewardID, string poolID, List<RewardEntry> entries, bool requireWeight) {
        if (entries == null) {
            return;
        }

        int positiveWeightCount = 0;
        foreach (RewardEntry entry in entries) {
            if (entry == null) {
                report.AddError($"Reward [{rewardID}] pool [{poolID}] has a null entry.");
                continue;
            }

            if (entry.Weight > 0) {
                positiveWeightCount++;
            }

            string type = string.IsNullOrEmpty(entry.Type) ? "Item" : entry.Type;
            switch (type) {
                case "Nothing":
                    break;
                case "Item":
                    if (string.IsNullOrEmpty(entry.ItemID) || !ConfigManager.Items.ContainsKey(entry.ItemID)) {
                        report.AddError($"Reward [{rewardID}] pool [{poolID}] references missing ItemID [{entry.ItemID}].");
                    }
                    break;
                case "Money":
                    if (entry.Money <= 0 && entry.Count <= 0 && entry.MinCount <= 0) {
                        report.AddError($"Reward [{rewardID}] pool [{poolID}] Money entry has no positive amount.");
                    }
                    break;
                case "RewardRef":
                    if (string.IsNullOrEmpty(entry.RewardID) || !ConfigManager.Rewards.ContainsKey(entry.RewardID)) {
                        report.AddError($"Reward [{rewardID}] pool [{poolID}] references missing RewardID [{entry.RewardID}].");
                    }
                    break;
                default:
                    report.AddError($"Reward [{rewardID}] pool [{poolID}] uses unsupported entry Type [{type}].");
                    break;
            }
        }

        if (requireWeight && entries.Count > 0 && positiveWeightCount == 0) {
            report.AddError($"Reward [{rewardID}] pool [{poolID}] has no positive-weight entries.");
        }
    }

    private static void ValidateMonsters(ConfigValidationReport report) {
        foreach (var kvp in ConfigManager.Monsters) {
            MonsterEntity monster = kvp.Value;
            if (monster == null) {
                report.AddError($"Monster [{kvp.Key}] deserialized as null.");
                continue;
            }

            if (!string.IsNullOrEmpty(monster.RewardID) && !ConfigManager.Rewards.ContainsKey(monster.RewardID)) {
                report.AddError($"Monster [{monster.MonsterID}] references missing RewardID [{monster.RewardID}].");
            }

            if (monster.LootPool != null) {
                foreach (LootPoolEntry entry in monster.LootPool) {
                    if (entry == null) {
                        report.AddError($"Monster [{monster.MonsterID}] has a null legacy LootPool entry.");
                        continue;
                    }

                    if (!string.IsNullOrEmpty(entry.ItemID) && !ConfigManager.Items.ContainsKey(entry.ItemID)) {
                        report.AddError($"Monster [{monster.MonsterID}] legacy LootPool references missing ItemID [{entry.ItemID}].");
                    }
                }
            }

            ValidateMonsterAI(report, monster);
        }

        ValidateMonsterJsonDoesNotUseOldBehaviorFields(report);
    }

    private static void ValidateMonsterAI(ConfigValidationReport report, MonsterEntity monster) {
        if (monster.AI == null) {
            report.AddError($"Monster [{monster.MonsterID}] is missing AI config.");
            return;
        }

        if (!MonsterActionSelectorFactory.IsSelectorRegistered(monster.AI.Selector)) {
            report.AddError($"Monster [{monster.MonsterID}] uses unknown AI selector [{monster.AI.Selector}].");
        }

        if (monster.AI.Actions == null || monster.AI.Actions.Count == 0) {
            report.AddError($"Monster [{monster.MonsterID}] must define at least one AI.Actions entry.");
            return;
        }

        int selectableWeight = 0;
        foreach (MonsterActionConfig action in monster.AI.Actions) {
            ValidateMonsterAction(report, monster, action, ref selectableWeight);
        }

        if (selectableWeight <= 0) {
            report.AddError($"Monster [{monster.MonsterID}] has no positive-weight AI action.");
        }
    }

    private static void ValidateMonsterAction(ConfigValidationReport report, MonsterEntity monster, MonsterActionConfig action, ref int selectableWeight) {
        if (action == null) {
            report.AddError($"Monster [{monster.MonsterID}] has a null AI action.");
            return;
        }

        if (string.IsNullOrEmpty(action.ActionID)) {
            report.AddError($"Monster [{monster.MonsterID}] has an AI action with empty ActionID.");
        }

        if (!MonsterActionFactory.IsActionRegistered(action.ActionType)) {
            report.AddError($"Monster [{monster.MonsterID}] action [{action.ActionID}] uses unknown ActionType [{action.ActionType}].");
        }

        if (!MonsterActionConfigParser.TryParseTarget(action.Target, MonsterTargetType.FirstAlivePlayer, out MonsterTargetType targetType)) {
            report.AddError($"Monster [{monster.MonsterID}] action [{action.ActionID}] uses unknown Target [{action.Target}].");
        } else if (MonsterActionConfigParser.TryParseActionType(action.ActionType, out MonsterActionType actionType)) {
            MonsterTargetType defaultTarget = MonsterActionConfigParser.GetDefaultTarget(actionType);
            MonsterActionConfigParser.TryParseTarget(action.Target, defaultTarget, out targetType);

            if (!MonsterActionConfigParser.IsTargetCompatible(actionType, targetType)) {
                report.AddError($"Monster [{monster.MonsterID}] action [{action.ActionID}] Target [{action.Target}] is not compatible with ActionType [{action.ActionType}].");
            }
        }

        if (!MonsterActionConditionEvaluator.IsConditionRegistered(action.Condition)) {
            report.AddError($"Monster [{monster.MonsterID}] action [{action.ActionID}] uses unknown Condition [{action.Condition}].");
        }

        if (action.Weight > 0) {
            selectableWeight += action.Weight;
        }

        if (action.CooldownTurns < 0) {
            report.AddError($"Monster [{monster.MonsterID}] action [{action.ActionID}] has negative CooldownTurns [{action.CooldownTurns}].");
        }

        if (action.UsesPerCombat < 0) {
            report.AddError($"Monster [{monster.MonsterID}] action [{action.ActionID}] has negative UsesPerCombat [{action.UsesPerCombat}].");
        }

        ValidateMonsterActionParams(report, monster, action);
    }

    private static void ValidateMonsterActionParams(ConfigValidationReport report, MonsterEntity monster, MonsterActionConfig action) {
        MonsterActionParamReader reader = new MonsterActionParamReader(action);
        if (!MonsterActionConfigParser.TryParseActionType(action.ActionType, out MonsterActionType actionType)) {
            return;
        }

        switch (actionType) {
            case MonsterActionType.DamageTarget:
                if (reader.GetInt("Damage", 0) <= 0) {
                    report.AddError($"Monster [{monster.MonsterID}] action [{action.ActionID}] DamageTarget requires positive Params.Damage.");
                }

                if (reader.Has("RepeatCount") && reader.GetInt("RepeatCount", 0) <= 0) {
                    report.AddError($"Monster [{monster.MonsterID}] action [{action.ActionID}] DamageTarget has invalid Params.RepeatCount.");
                }
                break;
            case MonsterActionType.ReduceWeaponDamage:
                float multiplier = reader.GetFloat("Multiplier", 0f);
                if (multiplier <= 0f || multiplier > 1f) {
                    report.AddError($"Monster [{monster.MonsterID}] action [{action.ActionID}] ReduceWeaponDamage requires 0 < Params.Multiplier <= 1.");
                }

                if (reader.GetInt("DurationPlayerTurns", 0) <= 0) {
                    report.AddError($"Monster [{monster.MonsterID}] action [{action.ActionID}] ReduceWeaponDamage requires positive Params.DurationPlayerTurns.");
                }
                break;
            case MonsterActionType.AddCursedItem:
                string itemID = reader.GetString("ItemID", string.Empty);
                if (string.IsNullOrEmpty(itemID) || !ConfigManager.Items.ContainsKey(itemID)) {
                    report.AddError($"Monster [{monster.MonsterID}] action [{action.ActionID}] AddCursedItem references missing Params.ItemID [{itemID}].");
                }

                WarnMetadataOnlyTags(report, $"Monster [{monster.MonsterID}] action [{action.ActionID}] OverrideTags", reader.GetStringList("OverrideTags"));
                if (reader.Has("OverrideValue") && reader.GetInt("OverrideValue", 0) < 0) {
                    report.AddError($"Monster [{monster.MonsterID}] action [{action.ActionID}] AddCursedItem has negative Params.OverrideValue.");
                }
                break;
        }
    }

    private static void ValidateMonsterJsonDoesNotUseOldBehaviorFields(ConfigValidationReport report) {
        string monstersPath = Path.Combine(Application.streamingAssetsPath, "Configs", "Monsters");
        if (!Directory.Exists(monstersPath)) {
            return;
        }

        string[] oldFields = {
            "DamageValue",
            "AttacksPerTurn",
            "GridInterference",
            "GridInterferenceParams"
        };

        foreach (string file in Directory.GetFiles(monstersPath, "*.json")) {
            try {
                JObject root = JObject.Parse(File.ReadAllText(file));
                string monsterID = root.Value<string>("MonsterID") ?? Path.GetFileNameWithoutExtension(file);
                foreach (string oldField in oldFields) {
                    if (root.ContainsKey(oldField)) {
                        report.AddError($"Monster [{monsterID}] still contains deprecated field [{oldField}]. Use AI.Actions instead.");
                    }
                }
            } catch (Exception ex) {
                report.AddError($"Monster config [{Path.GetFileName(file)}] could not be scanned for deprecated fields: {ex.Message}");
            }
        }
    }

    private static void ValidateDungeons(ConfigValidationReport report) {
        foreach (var kvp in ConfigManager.Dungeons) {
            DungeonConfig dungeon = kvp.Value;
            if (dungeon == null) {
                report.AddError($"Dungeon [{kvp.Key}] deserialized as null.");
                continue;
            }

            if (!ConfigManager.Monsters.ContainsKey(dungeon.BossNode)) {
                report.AddError($"Dungeon layer [{dungeon.LayerID}] references missing BossNode monster [{dungeon.BossNode}].");
            }

            if (dungeon.ExpectedNodeCount <= 0) {
                report.AddError($"Dungeon layer [{dungeon.LayerID}] has invalid ExpectedNodeCount [{dungeon.ExpectedNodeCount}].");
            }

            foreach (NodePoolEntry entry in dungeon.NodePool) {
                if (entry == null) {
                    report.AddError($"Dungeon layer [{dungeon.LayerID}] has a null NodePool entry.");
                    continue;
                }

                if (!NodeFactory.IsNodeTypeRegistered(entry.NodeType)) {
                    report.AddError($"Dungeon layer [{dungeon.LayerID}] references unknown NodeType [{entry.NodeType}].");
                }

                if (!string.IsNullOrEmpty(entry.RewardID) && !ConfigManager.Rewards.ContainsKey(entry.RewardID)) {
                    report.AddError($"Dungeon layer [{dungeon.LayerID}] node reward references missing RewardID [{entry.RewardID}].");
                }

                if (entry.NodeType == "CombatNode") {
                    foreach (string monsterID in entry.MonsterIDs) {
                        if (!ConfigManager.Monsters.ContainsKey(monsterID)) {
                            report.AddError($"Dungeon layer [{dungeon.LayerID}] CombatNode references missing MonsterID [{monsterID}].");
                        }
                    }
                }
            }
        }
    }

    private static void ValidateVisualAssets(ConfigValidationReport report) {
        foreach (var kvp in ConfigManager.Items) {
            string visualID = VisualAssetService.ResolveItemIconID(kvp.Value);
            if (!string.IsNullOrEmpty(visualID) && !VisualAssetService.TryGetSprite(visualID, out _)) {
                report.AddWarning($"Item [{kvp.Key}] icon VisualID [{visualID}] is not registered.");
            }
        }

        foreach (var kvp in ConfigManager.Monsters) {
            MonsterEntity monster = kvp.Value;
            string portraitID = !string.IsNullOrEmpty(monster.PortraitID)
                ? monster.PortraitID
                : $"monster_{monster.MonsterID}_portrait";

            if (!VisualAssetService.TryGetSprite(portraitID, out _)) {
                report.AddWarning($"Monster [{monster.MonsterID}] portrait VisualID [{portraitID}] is not registered.");
            }
        }
    }

    private static void ValidateEffects(ConfigValidationReport report, string ownerID, List<EffectData> effects) {
        if (effects == null) {
            return;
        }

        foreach (EffectData effect in effects) {
            if (effect == null) {
                report.AddError($"[{ownerID}] has a null effect entry.");
                continue;
            }

            if (!EffectFactory.IsEffectRegistered(effect.EffectID)) {
                report.AddError($"[{ownerID}] references unimplemented EffectID [{effect.EffectID}].");
            }
        }
    }

    private static void ValidateCost(ConfigValidationReport report, string ownerID, CraftingCost cost) {
        if (cost == null) {
            report.AddError($"{ownerID} is missing.");
            return;
        }

        if (cost.Money < 0) {
            report.AddError($"{ownerID} has negative Money [{cost.Money}].");
        }

        if (cost.RequiredItems == null) {
            return;
        }

        foreach (CraftingRequirement requirement in cost.RequiredItems) {
            if (requirement == null) {
                report.AddError($"{ownerID} has a null RequiredItems entry.");
                continue;
            }

            if (!ConfigManager.Items.ContainsKey(requirement.ConfigID)) {
                report.AddError($"{ownerID} references missing required item [{requirement.ConfigID}].");
            }

            if (requirement.Count <= 0) {
                report.AddError($"{ownerID} requires non-positive count [{requirement.Count}] for item [{requirement.ConfigID}].");
            }
        }
    }

    private static void WarnMetadataOnlyTags(ConfigValidationReport report, string ownerID, List<string> tags) {
        if (tags == null) {
            return;
        }

        foreach (string tag in tags) {
            if (MetadataOnlyTags.Contains(tag)) {
                report.AddWarning($"{ownerID} uses tag [{tag}], which is currently metadata-only unless referenced directly by a recipe or future rule.");
            }
        }
    }
}
