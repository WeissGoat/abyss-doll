using System;
using System.Collections.Generic;
using System.Linq;

public enum GrowthActionType {
    CraftProsthetic,
    ApplyMaintenance,
    DiveReadiness
}

public enum GrowthActionStatus {
    Available,
    MissingRequirements,
    Blocked
}

public class GrowthCostRequirementLine {
    public string ItemID;
    public string Name;
    public int RequiredCount;
    public int OwnedCount;
    public int MissingCount;

    public bool IsMet {
        get { return MissingCount <= 0; }
    }
}

public class GrowthActionSuggestion {
    public GrowthActionType Type;
    public GrowthActionStatus Status;
    public string ActionID;
    public string TargetID;
    public string Title;
    public string Description;
    public string Reason;
    public int RequiredMoney;
    public int OwnedMoney;
    public int MissingMoney;
    public List<GrowthCostRequirementLine> ItemRequirements = new List<GrowthCostRequirementLine>();
    public List<string> Tags = new List<string>();

    public bool CanExecute {
        get { return Status == GrowthActionStatus.Available; }
    }
}

public class GrowthFeedbackReport {
    public bool Success;
    public string Reason;
    public int LayerID;
    public bool CanDive;
    public string DiveSummary;
    public List<DiveReadinessIssue> DiveIssues = new List<DiveReadinessIssue>();
    public List<GrowthActionSuggestion> Suggestions = new List<GrowthActionSuggestion>();

    public IEnumerable<GrowthActionSuggestion> AvailableActions {
        get { return Suggestions.Where(suggestion => suggestion != null && suggestion.CanExecute); }
    }
}

public static class GrowthFeedbackService {
    public static GrowthFeedbackReport BuildReport(PlayerProfile player, int targetLayerID) {
        GrowthFeedbackReport report = new GrowthFeedbackReport {
            LayerID = targetLayerID
        };

        if (player == null) {
            report.Success = false;
            report.Reason = "Player profile is missing.";
            return report;
        }

        report.Success = true;
        AppendDiveReadiness(player, targetLayerID, report);
        AppendMaintenanceSuggestions(player, report);
        AppendCraftingSuggestions(player, report);
        SortSuggestions(report);
        return report;
    }

    public static GrowthActionSuggestion BuildCraftingSuggestion(PlayerProfile player, CraftingRecipeConfig recipe) {
        GrowthActionSuggestion suggestion = new GrowthActionSuggestion {
            Type = GrowthActionType.CraftProsthetic,
            Status = GrowthActionStatus.Blocked
        };

        if (player == null) {
            suggestion.Reason = "Player profile is missing.";
            return suggestion;
        }

        if (recipe == null || string.IsNullOrEmpty(recipe.RecipeID)) {
            suggestion.Reason = "Recipe config is missing.";
            return suggestion;
        }

        suggestion.ActionID = recipe.RecipeID;
        suggestion.TargetID = recipe.TargetProstheticID;
        suggestion.Title = recipe.RecipeID;
        suggestion.Tags.Add("Crafting");

        if (!string.IsNullOrEmpty(recipe.TargetProstheticID)
            && ConfigManager.Prosthetics.TryGetValue(recipe.TargetProstheticID, out ProstheticEntity prosthetic)) {
            suggestion.Title = string.IsNullOrEmpty(prosthetic.Name) ? prosthetic.ProstheticID : prosthetic.Name;
            suggestion.Description = $"Craft prosthetic [{prosthetic.ProstheticID}].";
            if (!string.IsNullOrEmpty(prosthetic.SlotType)) {
                suggestion.Tags.Add(prosthetic.SlotType);
            }
        } else {
            suggestion.Status = GrowthActionStatus.Blocked;
            suggestion.Reason = $"Target prosthetic [{recipe.TargetProstheticID}] is missing.";
            return suggestion;
        }

        ApplyCostState(player, recipe.Cost, suggestion);
        return suggestion;
    }

    public static GrowthActionSuggestion BuildMaintenanceSuggestion(PlayerProfile player, DollEntity doll, MaintenanceConfig config) {
        GrowthActionSuggestion suggestion = new GrowthActionSuggestion {
            Type = GrowthActionType.ApplyMaintenance,
            Status = GrowthActionStatus.Blocked
        };

        if (player == null) {
            suggestion.Reason = "Player profile is missing.";
            return suggestion;
        }

        if (doll == null) {
            suggestion.Reason = "Doll is missing.";
            return suggestion;
        }

        if (config == null || string.IsNullOrEmpty(config.MaintenanceID)) {
            suggestion.Reason = "Maintenance config is missing.";
            return suggestion;
        }

        suggestion.ActionID = config.MaintenanceID;
        suggestion.TargetID = doll.DollID;
        suggestion.Title = string.IsNullOrEmpty(config.Name) ? config.MaintenanceID : config.Name;
        suggestion.Description = config.Description;
        suggestion.Tags.Add("Maintenance");

        if (!ShouldSuggestMaintenance(doll, config)) {
            suggestion.Status = GrowthActionStatus.Blocked;
            suggestion.Reason = "No relevant damaged state for this maintenance.";
            return suggestion;
        }

        ApplyCostState(player, config.Cost, suggestion);
        if (suggestion.Status == GrowthActionStatus.Available
            && !MaintenanceService.CanApply(player, doll, config.MaintenanceID, out string reason)) {
            suggestion.Status = GrowthActionStatus.Blocked;
            suggestion.Reason = reason;
        }

        return suggestion;
    }

    private static void AppendDiveReadiness(PlayerProfile player, int targetLayerID, GrowthFeedbackReport report) {
        DiveReadinessResult readiness = DiveReadinessService.Evaluate(player, targetLayerID);
        report.CanDive = readiness.CanDive;
        report.DiveSummary = readiness.BuildSummary();
        report.DiveIssues.AddRange(readiness.Issues);

        if (readiness.CanDive) {
            report.Suggestions.Add(new GrowthActionSuggestion {
                Type = GrowthActionType.DiveReadiness,
                Status = GrowthActionStatus.Available,
                ActionID = $"dive_layer_{targetLayerID}",
                TargetID = targetLayerID.ToString(),
                Title = $"Dive to layer {targetLayerID}",
                Description = "Current doll can start this dive.",
                Reason = report.DiveSummary
            });
            return;
        }

        report.Suggestions.Add(new GrowthActionSuggestion {
            Type = GrowthActionType.DiveReadiness,
            Status = GrowthActionStatus.Blocked,
            ActionID = $"dive_layer_{targetLayerID}",
            TargetID = targetLayerID.ToString(),
            Title = $"Dive to layer {targetLayerID}",
            Description = "Current doll cannot start this dive.",
            Reason = report.DiveSummary
        });
    }

    private static void AppendMaintenanceSuggestions(PlayerProfile player, GrowthFeedbackReport report) {
        DollEntity doll = player.ActiveDoll;
        foreach (MaintenanceConfig config in ConfigManager.MaintenanceConfigs.Values.OrderBy(config => config.MaintenanceID)) {
            GrowthActionSuggestion suggestion = BuildMaintenanceSuggestion(player, doll, config);
            if (suggestion.Status != GrowthActionStatus.Blocked || HasActionableCostGap(suggestion)) {
                report.Suggestions.Add(suggestion);
            }
        }
    }

    private static void AppendCraftingSuggestions(PlayerProfile player, GrowthFeedbackReport report) {
        foreach (CraftingRecipeConfig recipe in ConfigManager.CraftingRecipes.Values.OrderBy(recipe => recipe.RecipeID)) {
            GrowthActionSuggestion suggestion = BuildCraftingSuggestion(player, recipe);
            if (suggestion.Status != GrowthActionStatus.Blocked || HasActionableCostGap(suggestion)) {
                report.Suggestions.Add(suggestion);
            }
        }
    }

    private static void ApplyCostState(PlayerProfile player, CraftingCost cost, GrowthActionSuggestion suggestion) {
        if (cost == null) {
            suggestion.Status = GrowthActionStatus.Blocked;
            suggestion.Reason = "Cost config is missing.";
            return;
        }

        suggestion.RequiredMoney = Math.Max(0, cost.Money);
        suggestion.OwnedMoney = Math.Max(0, player.Money);
        suggestion.MissingMoney = Math.Max(0, suggestion.RequiredMoney - suggestion.OwnedMoney);

        if (cost.RequiredItems != null) {
            foreach (CraftingRequirement requirement in cost.RequiredItems) {
                if (requirement == null || string.IsNullOrEmpty(requirement.ConfigID) || requirement.Count <= 0) {
                    suggestion.Status = GrowthActionStatus.Blocked;
                    suggestion.Reason = "Cost has invalid required item entry.";
                    return;
                }

                int ownedCount = WorkshopCostService.CountOwnedItems(player, requirement.ConfigID);
                suggestion.ItemRequirements.Add(new GrowthCostRequirementLine {
                    ItemID = requirement.ConfigID,
                    Name = ResolveItemName(requirement.ConfigID),
                    RequiredCount = requirement.Count,
                    OwnedCount = ownedCount,
                    MissingCount = Math.Max(0, requirement.Count - ownedCount)
                });
            }
        }

        if (suggestion.MissingMoney > 0 || suggestion.ItemRequirements.Any(line => line.MissingCount > 0)) {
            suggestion.Status = GrowthActionStatus.MissingRequirements;
            suggestion.Reason = BuildMissingReason(suggestion);
            return;
        }

        suggestion.Status = GrowthActionStatus.Available;
        suggestion.Reason = "Requirements met.";
    }

    private static bool ShouldSuggestMaintenance(DollEntity doll, MaintenanceConfig config) {
        if (doll == null || config?.Effects == null) {
            return false;
        }

        foreach (MaintenanceEffectConfig effect in config.Effects) {
            if (effect == null || string.IsNullOrEmpty(effect.TargetState)) {
                continue;
            }

            if (!Enum.TryParse(effect.TargetState, true, out MaintenanceTargetState targetState)) {
                continue;
            }

            switch (targetState) {
                case MaintenanceTargetState.Wear:
                    if (doll.Status.WearAndTear > 0f) {
                        return true;
                    }
                    break;
                case MaintenanceTargetState.Corruption:
                    if (doll.Status.Corruption > 0f) {
                        return true;
                    }
                    break;
                case MaintenanceTargetState.HP:
                    if (doll.Status.HP_Current < doll.Status.HP_Max) {
                        return true;
                    }
                    break;
                case MaintenanceTargetState.SAN:
                    if (doll.Status.SAN_Current < doll.Status.SAN_Max) {
                        return true;
                    }
                    break;
            }
        }

        return false;
    }

    private static string ResolveItemName(string itemID) {
        if (!string.IsNullOrEmpty(itemID)
            && ConfigManager.Items.TryGetValue(itemID, out ItemEntity item)
            && !string.IsNullOrEmpty(item.Name)) {
            return item.Name;
        }

        return itemID;
    }

    private static string BuildMissingReason(GrowthActionSuggestion suggestion) {
        List<string> parts = new List<string>();
        if (suggestion.MissingMoney > 0) {
            parts.Add($"money {suggestion.MissingMoney}");
        }

        foreach (GrowthCostRequirementLine line in suggestion.ItemRequirements) {
            if (line.MissingCount > 0) {
                parts.Add($"{line.ItemID} x{line.MissingCount}");
            }
        }

        return parts.Count == 0
            ? "Requirements missing."
            : $"Missing {string.Join(", ", parts)}.";
    }

    private static bool HasActionableCostGap(GrowthActionSuggestion suggestion) {
        if (suggestion == null) {
            return false;
        }

        return suggestion.MissingMoney > 0
            || suggestion.ItemRequirements.Any(line => line != null && line.MissingCount > 0);
    }

    private static void SortSuggestions(GrowthFeedbackReport report) {
        report.Suggestions = report.Suggestions
            .OrderBy(suggestion => suggestion.Type == GrowthActionType.DiveReadiness ? 0 : 1)
            .ThenBy(suggestion => suggestion.Status == GrowthActionStatus.Available ? 0 : 1)
            .ThenBy(suggestion => suggestion.Type.ToString())
            .ThenBy(suggestion => suggestion.ActionID)
            .ToList();
    }
}
