using System.Collections.Generic;
using UnityEngine;

public class ProstheticCraftingResult {
    public bool Success;
    public string Reason;
    public string RecipeID;
    public string ProstheticID;
    public string SlotType;
    public bool Equipped;
    public bool EffectsRecalculated;
    public int MoneySpent;
    public List<ItemEntity> ConsumedItems = new List<ItemEntity>();
    public List<string> UnequippedProstheticIDs = new List<string>();

    public string FeedbackText {
        get {
            if (!Success) {
                return string.IsNullOrEmpty(Reason) ? "Prosthetic crafting failed." : Reason;
            }

            string replacedText = UnequippedProstheticIDs.Count > 0
                ? $" Replaced {string.Join(", ", UnequippedProstheticIDs)}."
                : string.Empty;
            return $"Crafted and equipped [{ProstheticID}]. Money -{MoneySpent}, items {ConsumedItems.Count}.{replacedText}";
        }
    }
}

public static class ProstheticCraftingService {
    public static bool CanCraftAndEquip(PlayerProfile player, DollEntity doll, string recipeID, out string reason) {
        return TryResolveContext(player, doll, recipeID, out CraftingRecipeConfig recipe, out _, out reason)
            && WorkshopCostService.CanAfford(recipe.Cost, player, out reason);
    }

    public static ProstheticCraftingResult CraftAndEquip(PlayerProfile player, DollEntity doll, string recipeID) {
        ProstheticCraftingResult result = new ProstheticCraftingResult {
            RecipeID = recipeID
        };

        if (!TryResolveContext(player, doll, recipeID, out CraftingRecipeConfig recipe, out ProstheticEntity prosthetic, out string reason)) {
            result.Success = false;
            result.Reason = reason;
            return result;
        }

        result.ProstheticID = prosthetic.ProstheticID;
        result.SlotType = prosthetic.SlotType;

        if (!WorkshopCostService.TryPayCost(recipe.Cost, player, $"ProstheticCraft:{recipeID}", out WorkshopCostPaymentResult paymentResult)) {
            result.Success = false;
            result.Reason = paymentResult.Reason;
            return result;
        }

        result.MoneySpent = paymentResult.MoneySpent;
        result.ConsumedItems.AddRange(paymentResult.ConsumedItems);
        EnsureEquippedList(doll);
        UnequipSameSlot(doll, prosthetic, result.UnequippedProstheticIDs);

        if (!doll.EquippedProsthetics.Contains(prosthetic.ProstheticID)) {
            doll.EquippedProsthetics.Add(prosthetic.ProstheticID);
        }

        GridSolver.RecalculateAllEffects(doll);
        result.Success = true;
        result.Reason = string.Empty;
        result.Equipped = doll.EquippedProsthetics.Contains(prosthetic.ProstheticID);
        result.EffectsRecalculated = true;

        Debug.Log($"[ProstheticCraftingService] {result.FeedbackText}");
        return result;
    }

    private static bool TryResolveContext(
        PlayerProfile player,
        DollEntity doll,
        string recipeID,
        out CraftingRecipeConfig recipe,
        out ProstheticEntity prosthetic,
        out string reason) {
        recipe = null;
        prosthetic = null;
        reason = string.Empty;

        if (player == null) {
            reason = "Player profile is missing.";
            return false;
        }

        if (doll == null) {
            reason = "Doll is missing.";
            return false;
        }

        if (string.IsNullOrEmpty(recipeID)) {
            reason = "Recipe ID is empty.";
            return false;
        }

        if (!ConfigManager.CraftingRecipes.TryGetValue(recipeID, out recipe)) {
            reason = $"Recipe [{recipeID}] is missing.";
            return false;
        }

        if (string.IsNullOrEmpty(recipe.TargetProstheticID)) {
            reason = $"Recipe [{recipeID}] has no target prosthetic.";
            return false;
        }

        if (!ConfigManager.Prosthetics.TryGetValue(recipe.TargetProstheticID, out prosthetic)) {
            reason = $"Prosthetic config [{recipe.TargetProstheticID}] is missing.";
            return false;
        }

        if (string.IsNullOrEmpty(prosthetic.SlotType)) {
            reason = $"Prosthetic [{prosthetic.ProstheticID}] has no slot type.";
            return false;
        }

        return true;
    }

    private static void EnsureEquippedList(DollEntity doll) {
        if (doll.EquippedProsthetics == null) {
            doll.EquippedProsthetics = new List<string>();
        }
    }

    private static void UnequipSameSlot(DollEntity doll, ProstheticEntity newProsthetic, List<string> unequippedIDs) {
        if (doll?.EquippedProsthetics == null || newProsthetic == null || string.IsNullOrEmpty(newProsthetic.SlotType)) {
            return;
        }

        for (int i = doll.EquippedProsthetics.Count - 1; i >= 0; i--) {
            string equippedID = doll.EquippedProsthetics[i];
            if (ConfigManager.Prosthetics.TryGetValue(equippedID, out ProstheticEntity equippedConfig)
                && equippedConfig.SlotType == newProsthetic.SlotType
                && equippedConfig.ProstheticID != newProsthetic.ProstheticID) {
                doll.EquippedProsthetics.RemoveAt(i);
                unequippedIDs.Add(equippedID);
                Debug.Log($"[ProstheticCraftingService] Unequipped prosthetic [{equippedID}] from slot [{newProsthetic.SlotType}].");
            }
        }
    }
}
