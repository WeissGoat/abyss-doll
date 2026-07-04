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
        "Cursed"
    };

    public static ConfigValidationReport ValidateLoadedConfigs(bool includeVisualAssets = true) {
        ConfigValidationReport report = new ConfigValidationReport();

        ValidateItems(report);
        ValidateDolls(report);
        ValidateChassis(report);
        ValidateProsthetics(report);
        ValidateCraftingRecipes(report);
        ValidateMaintenanceConfigs(report);
        ValidateEconomyConfigs(report);
        ValidateFactionConfigs(report);
        ValidateOrderConfigs(report);
        ValidateRumorConfigs(report);
        ValidateRewards(report);
        ValidateMonsters(report);
        ValidateDungeons(report);
        NarrativeConfigValidator.Validate(ConfigManager.Narrative, report);

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

            ValidateItemGrid(report, item);

            ValidateCombatConfig(report, item.ConfigID, item.Combat);
            WarnMetadataOnlyTags(report, $"Item [{item.ConfigID}]", item.Tags);
        }

        ValidateItemJsonGridFields(report);
    }

    private static void ValidateItemGrid(ConfigValidationReport report, ItemEntity item) {
        if (item.Grid == null) {
            report.AddError($"Item [{item.ConfigID}] has no Grid component.");
            return;
        }

        if (item.Grid.Shape == null || item.Grid.Shape.Length == 0) {
            report.AddError($"Item [{item.ConfigID}] has no grid shape.");
        }

        if (item.Grid.GridCost <= 0) {
            report.AddError($"Item [{item.ConfigID}] has invalid GridCost [{item.Grid.GridCost}].");
        }

        if (item.Grid.CanRotate) {
            if (item.Grid.RotationSteps != 2 && item.Grid.RotationSteps != 4) {
                report.AddError($"Item [{item.ConfigID}] CanRotate=true requires RotationSteps to be 2 or 4.");
            }
            return;
        }

        if (item.Grid.RotationSteps != 1) {
            report.AddError($"Item [{item.ConfigID}] CanRotate=false requires RotationSteps=1.");
        }
    }

    private static void ValidateItemJsonGridFields(ConfigValidationReport report) {
        string itemsPath = Path.Combine(Application.streamingAssetsPath, "Configs", "Items");
        if (!Directory.Exists(itemsPath)) {
            return;
        }

        foreach (string file in Directory.GetFiles(itemsPath, "*.json")) {
            try {
                JObject root = JObject.Parse(File.ReadAllText(file));
                string itemID = root.Value<string>("ConfigID") ?? Path.GetFileNameWithoutExtension(file);
                JToken grid = root["Grid"];
                if (grid == null || grid.Type != JTokenType.Object) {
                    report.AddError($"Item [{itemID}] is missing Grid object.");
                    continue;
                }

                JObject gridObject = (JObject)grid;
                if (!gridObject.ContainsKey("CanRotate")) {
                    report.AddError($"Item [{itemID}] Grid is missing required field [CanRotate].");
                }

                if (!gridObject.ContainsKey("RotationSteps")) {
                    report.AddError($"Item [{itemID}] Grid is missing required field [RotationSteps].");
                }
            } catch (Exception ex) {
                report.AddError($"Item config [{Path.GetFileName(file)}] could not be scanned for Grid rotation fields: {ex.Message}");
            }
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

    private static void ValidateMaintenanceConfigs(ConfigValidationReport report) {
        foreach (var kvp in ConfigManager.MaintenanceConfigs) {
            MaintenanceConfig maintenance = kvp.Value;
            if (maintenance == null) {
                report.AddError($"Maintenance [{kvp.Key}] deserialized as null.");
                continue;
            }

            if (string.IsNullOrEmpty(maintenance.MaintenanceID)) {
                report.AddError($"Maintenance file loaded with empty MaintenanceID under key [{kvp.Key}].");
            }

            if (!string.Equals(kvp.Key, maintenance.MaintenanceID, StringComparison.Ordinal)) {
                report.AddWarning($"Maintenance [{kvp.Key}] dictionary key differs from MaintenanceID [{maintenance.MaintenanceID}].");
            }

            ValidateCost(report, $"Maintenance [{maintenance.MaintenanceID}] Cost", maintenance.Cost);

            if (maintenance.DailyLimit < 0) {
                report.AddError($"Maintenance [{maintenance.MaintenanceID}] DailyLimit must be >= 0.");
            }

            if (maintenance.Effects == null || maintenance.Effects.Count == 0) {
                report.AddError($"Maintenance [{maintenance.MaintenanceID}] must define at least one effect.");
                continue;
            }

            bool hasDivePermitRecoveryEffect = false;
            for (int i = 0; i < maintenance.Effects.Count; i++) {
                MaintenanceEffectConfig effect = maintenance.Effects[i];
                if (effect == null) {
                    report.AddError($"Maintenance [{maintenance.MaintenanceID}] Effects[{i}] is null.");
                    continue;
                }

                if (!Enum.TryParse(effect.TargetState, true, out MaintenanceTargetState targetState)) {
                    report.AddError($"Maintenance [{maintenance.MaintenanceID}] Effects[{i}] has unknown TargetState [{effect.TargetState}].");
                    continue;
                }

                if (effect.Amount <= 0f) {
                    report.AddError($"Maintenance [{maintenance.MaintenanceID}] Effects[{i}] amount must be positive.");
                }

                if (targetState == MaintenanceTargetState.Wear || targetState == MaintenanceTargetState.Corruption) {
                    hasDivePermitRecoveryEffect = true;
                }
            }

            if (maintenance.RestoresDivePermit && !hasDivePermitRecoveryEffect) {
                report.AddWarning($"Maintenance [{maintenance.MaintenanceID}] RestoresDivePermit=true but has no Wear/Corruption recovery effect.");
            }
        }
    }

    private static void ValidateEconomyConfigs(ConfigValidationReport report) {
        if (ConfigManager.EconomyConfigs.Count == 0) {
            report.AddError("Economy config domain is empty. Add at least one Economy/*.json config.");
            return;
        }

        foreach (var kvp in ConfigManager.EconomyConfigs) {
            EconomyConfig economy = kvp.Value;
            if (economy == null) {
                report.AddError($"Economy [{kvp.Key}] deserialized as null.");
                continue;
            }

            if (string.IsNullOrEmpty(economy.EconomyConfigID)) {
                report.AddError($"Economy file loaded with empty EconomyConfigID under key [{kvp.Key}].");
            }

            if (!string.Equals(kvp.Key, economy.EconomyConfigID, StringComparison.Ordinal)) {
                report.AddWarning($"Economy [{kvp.Key}] dictionary key differs from EconomyConfigID [{economy.EconomyConfigID}].");
            }

            if (economy.WeekLength <= 0) {
                report.AddError($"Economy [{economy.EconomyConfigID}] WeekLength must be positive.");
            }

            if (economy.MonthLength <= 0) {
                report.AddError($"Economy [{economy.EconomyConfigID}] MonthLength must be positive.");
            } else if (economy.WeekLength > 0 && economy.MonthLength % economy.WeekLength != 0) {
                report.AddWarning($"Economy [{economy.EconomyConfigID}] MonthLength [{economy.MonthLength}] is not divisible by WeekLength [{economy.WeekLength}].");
            }

            if (economy.WorkshopMaintenanceBase < 0) {
                report.AddError($"Economy [{economy.EconomyConfigID}] WorkshopMaintenanceBase must be >= 0.");
            }

            if (economy.LicenseFeeBase < 0) {
                report.AddError($"Economy [{economy.EconomyConfigID}] LicenseFeeBase must be >= 0.");
            }

            if (economy.DebtInterestRate < 0f) {
                report.AddError($"Economy [{economy.EconomyConfigID}] DebtInterestRate must be >= 0.");
            }

            if (economy.LightDebtThresholdRatio < 0f || economy.LightDebtThresholdRatio > 1f) {
                report.AddError($"Economy [{economy.EconomyConfigID}] LightDebtThresholdRatio must be between 0 and 1.");
            }

            if (economy.PawnValueMultiplier <= 0f || economy.PawnValueMultiplier > 1f) {
                report.AddError($"Economy [{economy.EconomyConfigID}] PawnValueMultiplier must satisfy 0 < value <= 1.");
            }

            ValidateRentCurve(report, economy);
        }
    }

    private static void ValidateRentCurve(ConfigValidationReport report, EconomyConfig economy) {
        if (economy.RentCurve == null || economy.RentCurve.Count == 0) {
            report.AddError($"Economy [{economy.EconomyConfigID}] must define at least one RentCurve step.");
            return;
        }

        HashSet<int> months = new HashSet<int>();
        bool hasFirstMonth = false;
        foreach (RentCurveStepConfig step in economy.RentCurve) {
            if (step == null) {
                report.AddError($"Economy [{economy.EconomyConfigID}] has a null RentCurve step.");
                continue;
            }

            if (step.Month <= 0) {
                report.AddError($"Economy [{economy.EconomyConfigID}] has RentCurve step with invalid Month [{step.Month}].");
            }

            if (step.BaseRent <= 0) {
                report.AddError($"Economy [{economy.EconomyConfigID}] Month [{step.Month}] BaseRent must be positive.");
            }

            if (!months.Add(step.Month)) {
                report.AddError($"Economy [{economy.EconomyConfigID}] has duplicated RentCurve Month [{step.Month}].");
            }

            hasFirstMonth |= step.Month == 1;
        }

        if (!hasFirstMonth) {
            report.AddWarning($"Economy [{economy.EconomyConfigID}] RentCurve has no Month=1 baseline.");
        }
    }

    private static void ValidateFactionConfigs(ConfigValidationReport report) {
        foreach (var kvp in ConfigManager.Factions) {
            FactionConfig faction = kvp.Value;
            if (faction == null) {
                report.AddError($"Faction [{kvp.Key}] deserialized as null.");
                continue;
            }

            if (string.IsNullOrEmpty(faction.FactionID)) {
                report.AddError($"Faction file loaded with empty FactionID under key [{kvp.Key}].");
            }

            if (!string.Equals(kvp.Key, faction.FactionID, StringComparison.Ordinal)) {
                report.AddWarning($"Faction [{kvp.Key}] dictionary key differs from FactionID [{faction.FactionID}].");
            }

            if (faction.VisibleOrderSlots <= 0) {
                report.AddError($"Faction [{faction.FactionID}] VisibleOrderSlots must be positive.");
            }

            if (faction.MaxActiveOrders <= 0) {
                report.AddError($"Faction [{faction.FactionID}] MaxActiveOrders must be positive.");
            }

            if (faction.ReputationRanks == null) {
                continue;
            }

            HashSet<int> ranks = new HashSet<int>();
            foreach (ReputationRankConfig rank in faction.ReputationRanks) {
                if (rank == null) {
                    report.AddError($"Faction [{faction.FactionID}] has a null ReputationRanks entry.");
                    continue;
                }

                if (rank.Rank <= 0) {
                    report.AddError($"Faction [{faction.FactionID}] has invalid ReputationRank Rank [{rank.Rank}].");
                }

                if (rank.Threshold < 0) {
                    report.AddError($"Faction [{faction.FactionID}] rank [{rank.Rank}] Threshold must be >= 0.");
                }

                if (!ranks.Add(rank.Rank)) {
                    report.AddError($"Faction [{faction.FactionID}] has duplicated ReputationRank [{rank.Rank}].");
                }
            }
        }
    }

    private static void ValidateOrderConfigs(ConfigValidationReport report) {
        foreach (var kvp in ConfigManager.Orders) {
            OrderConfig order = kvp.Value;
            if (order == null) {
                report.AddError($"Order [{kvp.Key}] deserialized as null.");
                continue;
            }

            if (string.IsNullOrEmpty(order.OrderID)) {
                report.AddError($"Order file loaded with empty OrderID under key [{kvp.Key}].");
            }

            if (!string.Equals(kvp.Key, order.OrderID, StringComparison.Ordinal)) {
                report.AddWarning($"Order [{kvp.Key}] dictionary key differs from OrderID [{order.OrderID}].");
            }

            if (string.IsNullOrEmpty(order.FactionID) || !ConfigManager.Factions.ContainsKey(order.FactionID)) {
                report.AddError($"Order [{order.OrderID}] references missing FactionID [{order.FactionID}].");
            }

            if (!Enum.TryParse(order.OrderType, true, out EconomyOrderType _)) {
                report.AddError($"Order [{order.OrderID}] has unknown OrderType [{order.OrderType}].");
            }

            if (order.DeadlineDays <= 0) {
                report.AddError($"Order [{order.OrderID}] DeadlineDays must be positive.");
            }

            if (order.Weight <= 0) {
                report.AddError($"Order [{order.OrderID}] Weight must be positive.");
            }

            if (!string.IsNullOrEmpty(order.RewardID) && !ConfigManager.Rewards.ContainsKey(order.RewardID)) {
                report.AddError($"Order [{order.OrderID}] references missing RewardID [{order.RewardID}].");
            }

            ValidateOrderRequirement(report, order);

            if (order.FailurePenalty != null && order.FailurePenalty.CooldownDays < 0) {
                report.AddError($"Order [{order.OrderID}] FailurePenalty.CooldownDays must be >= 0.");
            }
        }
    }

    private static void ValidateOrderRequirement(ConfigValidationReport report, OrderConfig order) {
        OrderRequirementConfig requirement = order.Requirement;
        if (requirement == null) {
            report.AddError($"Order [{order.OrderID}] must define Requirement.");
            return;
        }

        if (requirement.RequiredCount <= 0) {
            report.AddError($"Order [{order.OrderID}] Requirement.RequiredCount must be positive.");
        }

        if (requirement.MinGridCost < 0) {
            report.AddError($"Order [{order.OrderID}] Requirement.MinGridCost must be >= 0.");
        }

        if (requirement.MinLayer < 0) {
            report.AddError($"Order [{order.OrderID}] Requirement.MinLayer must be >= 0.");
        }

        bool hasRequiredItem = requirement.RequiredItemIDs != null && requirement.RequiredItemIDs.Count > 0;
        bool hasRequiredTag = requirement.RequiredTags != null && requirement.RequiredTags.Count > 0;
        if (!hasRequiredItem && !hasRequiredTag) {
            report.AddError($"Order [{order.OrderID}] Requirement must define RequiredItemIDs or RequiredTags.");
        }

        if (requirement.RequiredItemIDs == null) {
            return;
        }

        foreach (string itemID in requirement.RequiredItemIDs) {
            if (string.IsNullOrEmpty(itemID) || !ConfigManager.Items.ContainsKey(itemID)) {
                report.AddError($"Order [{order.OrderID}] Requirement references missing item [{itemID}].");
            }
        }
    }

    private static void ValidateRumorConfigs(ConfigValidationReport report) {
        foreach (var kvp in ConfigManager.Rumors) {
            RumorConfig rumor = kvp.Value;
            if (rumor == null) {
                report.AddError($"Rumor [{kvp.Key}] deserialized as null.");
                continue;
            }

            if (string.IsNullOrEmpty(rumor.RumorID)) {
                report.AddError($"Rumor file loaded with empty RumorID under key [{kvp.Key}].");
            }

            if (!string.Equals(kvp.Key, rumor.RumorID, StringComparison.Ordinal)) {
                report.AddWarning($"Rumor [{kvp.Key}] dictionary key differs from RumorID [{rumor.RumorID}].");
            }

            if (!Enum.TryParse(rumor.RumorType, true, out EconomyRumorType _)) {
                report.AddError($"Rumor [{rumor.RumorID}] has unknown RumorType [{rumor.RumorType}].");
            }

            if (!Enum.TryParse(rumor.Channel, true, out EconomySellChannel _)) {
                report.AddError($"Rumor [{rumor.RumorID}] has unknown Channel [{rumor.Channel}].");
            }

            if (rumor.PriceMultiplier <= 0f) {
                report.AddError($"Rumor [{rumor.RumorID}] PriceMultiplier must be positive.");
            }

            if (rumor.DurationDays <= 0) {
                report.AddError($"Rumor [{rumor.RumorID}] DurationDays must be positive.");
            }

            if (rumor.Weight <= 0) {
                report.AddError($"Rumor [{rumor.RumorID}] Weight must be positive.");
            }

            if (rumor.TargetLayer < 0) {
                report.AddError($"Rumor [{rumor.RumorID}] TargetLayer must be >= 0.");
            }
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

            ValidateDungeonMapProfile(report, dungeon);

            if (dungeon.NodePool == null || dungeon.NodePool.Count == 0) {
                report.AddError($"Dungeon layer [{dungeon.LayerID}] must define at least one NodePool entry.");
                ValidateDungeonNodeEntry(report, dungeon, dungeon.EndNode, "EndNode", false);
                continue;
            }

            foreach (NodePoolEntry entry in dungeon.NodePool) {
                ValidateDungeonNodeEntry(report, dungeon, entry, "NodePool", true);

                if (entry != null && IsStairsNodeType(entry.NodeType)) {
                    report.AddWarning($"Dungeon layer [{dungeon.LayerID}] has StairsNode inside NodePool. Stairs are usually intended for EndNode.");
                }
            }

            ValidateDungeonNodeEntry(report, dungeon, dungeon.EndNode, "EndNode", false);
        }
    }

    private static void ValidateDungeonMapProfile(ConfigValidationReport report, DungeonConfig dungeon) {
        if (dungeon == null) {
            return;
        }

        if (string.IsNullOrEmpty(dungeon.MapProfileID)) {
            report.AddError($"Dungeon layer [{dungeon.LayerID}] must define MapProfileID for formal map generation.");
        }

        if (dungeon.RowCount <= 0) {
            report.AddError($"Dungeon layer [{dungeon.LayerID}] must define positive RowCount.");
        }

        if (dungeon.MinWidth <= 0) {
            report.AddError($"Dungeon layer [{dungeon.LayerID}] must define positive MinWidth.");
        }

        if (dungeon.MaxWidth <= 0) {
            report.AddError($"Dungeon layer [{dungeon.LayerID}] must define positive MaxWidth.");
        }

        if (dungeon.MinRouteCount <= 0) {
            report.AddError($"Dungeon layer [{dungeon.LayerID}] must define positive MinRouteCount.");
        }

        if (dungeon.MinWidth > 0 && dungeon.MaxWidth > 0 && dungeon.MaxWidth < dungeon.MinWidth) {
            report.AddError($"Dungeon layer [{dungeon.LayerID}] MaxWidth [{dungeon.MaxWidth}] must be >= MinWidth [{dungeon.MinWidth}].");
        }

        if (dungeon.MinRouteCount > 0 && dungeon.MaxWidth > 0 && dungeon.MinRouteCount > dungeon.MaxWidth) {
            report.AddError($"Dungeon layer [{dungeon.LayerID}] MinRouteCount [{dungeon.MinRouteCount}] must be <= MaxWidth [{dungeon.MaxWidth}].");
        }

        if (dungeon.RowCount > 0 && dungeon.ExpectedNodeCount > 0 && dungeon.ExpectedNodeCount != dungeon.RowCount + 1) {
            report.AddWarning($"Dungeon layer [{dungeon.LayerID}] ExpectedNodeCount [{dungeon.ExpectedNodeCount}] should equal RowCount + Boss [{dungeon.RowCount + 1}] under the formal network map contract.");
        }

        if (string.IsNullOrEmpty(dungeon.FogProfile)) {
            report.AddWarning($"Dungeon layer [{dungeon.LayerID}] should define FogProfile for formal map visibility.");
        }

        if (dungeon.NodeRevealDepth < 0) {
            report.AddError($"Dungeon layer [{dungeon.LayerID}] NodeRevealDepth [{dungeon.NodeRevealDepth}] must be >= 0.");
        }

        if (dungeon.NodePreviewDepth < 0) {
            report.AddError($"Dungeon layer [{dungeon.LayerID}] NodePreviewDepth [{dungeon.NodePreviewDepth}] must be >= 0.");
        }
    }

    private static void ValidateDungeonNodeEntry(ConfigValidationReport report, DungeonConfig dungeon, NodePoolEntry entry, string owner, bool requirePositiveWeight) {
        if (entry == null) {
            report.AddError($"Dungeon layer [{dungeon.LayerID}] has a null {owner} entry.");
            return;
        }

        if (!NodeFactory.IsNodeTypeRegistered(entry.NodeType)) {
            report.AddError($"Dungeon layer [{dungeon.LayerID}] {owner} references unknown NodeType [{entry.NodeType}].");
        }

        if (requirePositiveWeight && entry.Weight <= 0) {
            report.AddError($"Dungeon layer [{dungeon.LayerID}] {owner} [{entry.NodeType}] must have positive Weight.");
        }

        if (!string.IsNullOrEmpty(entry.RewardID) && !ConfigManager.Rewards.ContainsKey(entry.RewardID)) {
            report.AddError($"Dungeon layer [{dungeon.LayerID}] {owner} reward references missing RewardID [{entry.RewardID}].");
        }

        ValidateDungeonNodeRisk(report, dungeon, entry, owner);
        ValidateDungeonNodeOutcomes(report, dungeon, entry, owner);

        if (IsCombatNodeType(entry.NodeType)) {
            if (entry.MonsterIDs == null) {
                report.AddError($"Dungeon layer [{dungeon.LayerID}] {owner} CombatNode has null MonsterIDs.");
                return;
            }

            foreach (string monsterID in entry.MonsterIDs) {
                if (!ConfigManager.Monsters.ContainsKey(monsterID)) {
                    report.AddError($"Dungeon layer [{dungeon.LayerID}] {owner} CombatNode references missing MonsterID [{monsterID}].");
                }
            }
        }

        if (IsTreasureNodeType(entry.NodeType) && string.IsNullOrEmpty(entry.RewardID)) {
            report.AddWarning($"Dungeon layer [{dungeon.LayerID}] {owner} TreasureNode has no RewardID configured.");
        }
    }

    private static void ValidateDungeonNodeRisk(ConfigValidationReport report, DungeonConfig dungeon, NodePoolEntry entry, string owner) {
        if (entry == null || string.IsNullOrEmpty(entry.RiskLevel)) {
            return;
        }

        if (!Enum.TryParse(entry.RiskLevel, true, out DungeonNodeRiskLevel _)) {
            report.AddError($"Dungeon layer [{dungeon.LayerID}] {owner} [{entry.NodeType}] has unknown RiskLevel [{entry.RiskLevel}].");
        }
    }

    private static void ValidateDungeonNodeOutcomes(ConfigValidationReport report, DungeonConfig dungeon, NodePoolEntry entry, string owner) {
        if (entry?.OutcomeEffects == null) {
            return;
        }

        for (int i = 0; i < entry.OutcomeEffects.Count; i++) {
            DungeonNodeOutcomeConfig outcome = entry.OutcomeEffects[i];
            if (outcome == null) {
                report.AddError($"Dungeon layer [{dungeon.LayerID}] {owner} [{entry.NodeType}] OutcomeEffects[{i}] is null.");
                continue;
            }

            string type = string.IsNullOrEmpty(outcome.Type) ? nameof(DungeonNodeOutcomeType.ModifyResource) : outcome.Type;
            if (!Enum.TryParse(type, true, out DungeonNodeOutcomeType outcomeType)) {
                report.AddError($"Dungeon layer [{dungeon.LayerID}] {owner} [{entry.NodeType}] OutcomeEffects[{i}] has unknown Type [{outcome.Type}].");
                continue;
            }

            switch (outcomeType) {
                case DungeonNodeOutcomeType.ModifyResource:
                    if (!Enum.TryParse(outcome.Resource, true, out EffectResourceType resource)
                        || (resource != EffectResourceType.HP
                            && resource != EffectResourceType.SAN
                            && resource != EffectResourceType.Money)) {
                        report.AddError($"Dungeon layer [{dungeon.LayerID}] {owner} [{entry.NodeType}] OutcomeEffects[{i}] ModifyResource uses unsupported Resource [{outcome.Resource}].");
                    }

                    if (outcome.Amount == 0) {
                        report.AddWarning($"Dungeon layer [{dungeon.LayerID}] {owner} [{entry.NodeType}] OutcomeEffects[{i}] modifies [{outcome.Resource}] by 0.");
                    }
                    break;
            }
        }
    }

    private static bool IsCombatNodeType(string nodeType) {
        return string.Equals(nodeType, "CombatNode", StringComparison.OrdinalIgnoreCase)
            || string.Equals(nodeType, "Combat", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsStairsNodeType(string nodeType) {
        return string.Equals(nodeType, "StairsNode", StringComparison.OrdinalIgnoreCase)
            || string.Equals(nodeType, "Stairs", StringComparison.OrdinalIgnoreCase);
    }

    private static bool IsTreasureNodeType(string nodeType) {
        return string.Equals(nodeType, "TreasureNode", StringComparison.OrdinalIgnoreCase)
            || string.Equals(nodeType, "Treasure", StringComparison.OrdinalIgnoreCase);
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
            string portraitID = VisualAssetService.ResolveMonsterPortraitID(monster);
            string combatVisualID = VisualAssetService.ResolveMonsterCombatVisualID(monster);

            if (!VisualAssetService.TryGetSprite(portraitID, out _)) {
                report.AddWarning($"Monster [{monster.MonsterID}] portrait VisualID [{portraitID}] is not registered.");
            }

            if (!VisualAssetService.TryGetSprite(combatVisualID, out _)) {
                report.AddWarning($"Monster [{monster.MonsterID}] combat VisualID [{combatVisualID}] is not registered.");
            }
        }

        foreach (var kvp in ConfigManager.Prosthetics) {
            ProstheticEntity prosthetic = kvp.Value;
            string iconID = VisualAssetService.ResolveProstheticIconID(prosthetic);
            if (!VisualAssetService.TryGetSprite(iconID, out _)) {
                report.AddWarning($"Prosthetic [{prosthetic.ProstheticID}] icon VisualID [{iconID}] is not registered.");
            }
        }

        ValidateRequiredSprite(report, VisualAssetService.CombatNodeIconID, "Node icon");
        ValidateRequiredSprite(report, VisualAssetService.BossNodeIconID, "Node icon");
        ValidateRequiredSprite(report, VisualAssetService.SafeRoomNodeIconID, "Node icon");
        ValidateRequiredSprite(report, VisualAssetService.StairsNodeIconID, "Node icon");
        ValidateRequiredSprite(report, VisualAssetService.WorkshopBackgroundID, "Background");
        ValidateRequiredSprite(report, VisualAssetService.CombatBackgroundID, "Background");
        ValidateRequiredSprite(report, VisualAssetService.DefaultDungeonMapBackgroundID, "Background");
        ValidateRequiredSprite(report, VisualAssetService.SafeRoomBackgroundID, "Background");
        ValidateRequiredSprite(report, VisualAssetService.StairsRoomBackgroundID, "Background");
        ValidateRequiredSprite(report, VisualAssetService.LayerSelectBackgroundID, "Background");
        ValidateRequiredSprite(report, VisualAssetService.SettlementVictoryBackgroundID, "Background");
        ValidateRequiredSprite(report, VisualAssetService.SettlementDefeatBackgroundID, "Background");
        ValidateRequiredSprite(report, VisualAssetService.UIPanelMainID, "UI skin");
        ValidateRequiredSprite(report, VisualAssetService.UIListRowNormalID, "UI skin");
        ValidateRequiredSprite(report, VisualAssetService.UIListRowSelectedID, "UI skin");
        ValidateRequiredSprite(report, VisualAssetService.UISettlementVictoryPanelID, "UI skin");
        ValidateRequiredSprite(report, VisualAssetService.UISettlementDefeatPanelID, "UI skin");
        ValidateRequiredSprite(report, VisualAssetService.UIDungeonNodePlateID, "UI skin");
        ValidateRequiredSprite(report, VisualAssetService.UIDungeonRouteLineID, "UI skin");
        ValidateRequiredSprite(report, VisualAssetService.UICombatEntityShadowID, "UI combat skin");
        ValidateRequiredSprite(report, VisualAssetService.UICombatTargetRingID, "UI combat skin");
        ValidateRequiredSprite(report, VisualAssetService.UIIconLockedID, "UI icon");
        ValidateRequiredSprite(report, VisualAssetService.UIIconEquippedID, "UI icon");
        ValidateRequiredSprite(report, VisualAssetService.UIIconMoneyID, "UI icon");
        ValidateRequiredSprite(report, VisualAssetService.UIIconMaintenanceID, "UI icon");
        ValidateRequiredSprite(report, VisualAssetService.UIIconBillID, "UI icon");
        ValidateRequiredSprite(report, VisualAssetService.UIIconWarningID, "UI icon");
        ValidateRequiredSprite(report, VisualAssetService.UIIconShopChannelID, "UI icon");
        ValidateRequiredSprite(report, VisualAssetService.UIIconBlackMarketID, "UI icon");
        ValidateRequiredSprite(report, VisualAssetService.UIIconOrderID, "UI icon");
        ValidateRequiredSprite(report, VisualAssetService.UIIconFactionID, "UI icon");
        ValidateRequiredSprite(report, VisualAssetService.UIIconDeadlineID, "UI icon");
        ValidateRequiredSprite(report, VisualAssetService.UIIconRumorID, "UI icon");
        ValidateRequiredSprite(report, VisualAssetService.UIIconPriceUpID, "UI icon");
        ValidateRequiredSprite(report, VisualAssetService.UIIconPriceDownID, "UI icon");
        ValidateRequiredSprite(report, VisualAssetService.UITitleDividerID, "UI skin");

        foreach (var kvp in ConfigManager.Dungeons) {
            string layerBackgroundID = $"bg_dungeon_layer_{kvp.Key}";
            if (!VisualAssetService.TryGetSprite(layerBackgroundID, out _)) {
                report.AddWarning($"Dungeon layer [{kvp.Key}] background VisualID [{layerBackgroundID}] is not registered. Falling back to [{VisualAssetService.DefaultDungeonMapBackgroundID}].");
            }
        }
    }

    private static void ValidateRequiredSprite(ConfigValidationReport report, string visualID, string label) {
        if (!VisualAssetService.TryGetSprite(visualID, out _)) {
            report.AddWarning($"{label} VisualID [{visualID}] is not registered.");
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
                continue;
            }

            EffectBase effectInstance = EffectFactory.CreateEffect(effect);
            effectInstance?.ValidateConfig(report, ownerID, effect);
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
