using UnityEngine;

public class WorkshopSystem {
    public bool CanAfford(CraftingCost cost, PlayerProfile player) {
        if (cost == null || player == null) {
            return false;
        }

        if (player.Money < cost.Money) {
            return false;
        }

        foreach (var reqItem in cost.RequiredItems) {
            if (ItemLifecycleService.CountOwnedItems(player, reqItem.ConfigID) < reqItem.Count) {
                return false;
            }
        }

        return true;
    }

    private void DeductCost(CraftingCost cost, PlayerProfile player) {
        player.Money -= cost.Money;

        foreach (var reqItem in cost.RequiredItems) {
            for (int i = 0; i < reqItem.Count; i++) {
                if (!ItemLifecycleService.TryConsumeOwnedItemForCost(player, reqItem.ConfigID, "WorkshopCost", out _, out ItemLifecycleResult result)) {
                    Debug.LogWarning($"[WorkshopSystem] Failed to deduct item cost [{reqItem.ConfigID}]: {result?.Reason ?? "Unknown reason"}");
                }
            }
        }
    }

    public void UpgradeDollChassis(DollEntity doll) {
        if (doll.Chassis == null) return;

        if (!ConfigManager.Chassis.TryGetValue(doll.Chassis.ChassisID, out var currentChassisConfig)) {
            return;
        }

        if (currentChassisConfig.UpgradeCost == null) {
            Debug.Log($"[WorkshopSystem] Chassis {doll.Chassis.ChassisID} is already at max level (No UpgradeCost found).");
            return;
        }

        if (CanAfford(currentChassisConfig.UpgradeCost, GameRoot.Core.CurrentPlayer)) {
            DeductCost(currentChassisConfig.UpgradeCost, GameRoot.Core.CurrentPlayer);

            string nextID = currentChassisConfig.UpgradeCost.NextChassisID;
            if (ConfigManager.Chassis.TryGetValue(nextID, out var nextChassis)) {
                string chassisJson = Newtonsoft.Json.JsonConvert.SerializeObject(nextChassis);
                doll.Chassis = Newtonsoft.Json.JsonConvert.DeserializeObject<ChassisComponent>(chassisJson);
                doll.RuntimeGrid = new BackpackGrid(doll.Chassis);

                Debug.Log($"[WorkshopSystem] Chassis upgraded to {nextID}. New size: {doll.Chassis.GridWidth}x{doll.Chassis.GridHeight}");
            } else {
                Debug.LogError($"[WorkshopSystem] Next Chassis ID not found in config: {nextID}");
            }
        } else {
            Debug.LogWarning("[WorkshopSystem] Cannot afford to upgrade chassis.");
        }
    }

    public bool CraftAndEquipProsthetic(string recipeID, DollEntity doll) {
        if (doll == null) {
            Debug.LogError("[WorkshopSystem] Cannot craft prosthetic because doll is null.");
            return false;
        }

        if (!ConfigManager.CraftingRecipes.TryGetValue(recipeID, out var recipe)) {
            Debug.LogError($"[WorkshopSystem] Recipe not found: {recipeID}");
            return false;
        }

        if (!ConfigManager.Prosthetics.TryGetValue(recipe.TargetProstheticID, out var prostheticConfig)) {
            Debug.LogError($"[WorkshopSystem] Prosthetic config not found: {recipe.TargetProstheticID}");
            return false;
        }

        PlayerProfile player = GameRoot.Core.CurrentPlayer;
        if (!CanAfford(recipe.Cost, player)) {
            Debug.LogWarning($"[WorkshopSystem] Cannot afford to craft prosthetic: {recipeID}");
            return false;
        }

        DeductCost(recipe.Cost, player);
        UnequipSameSlotProsthetic(doll, prostheticConfig);

        if (!doll.EquippedProsthetics.Contains(prostheticConfig.ProstheticID)) {
            doll.EquippedProsthetics.Add(prostheticConfig.ProstheticID);
            Debug.Log($"[WorkshopSystem] Crafted and equipped prosthetic: {prostheticConfig.ProstheticID}");
        }

        GridSolver.RecalculateAllEffects(doll);
        return true;
    }

    public bool CanApplyMaintenance(string maintenanceID, DollEntity doll, out string reason) {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        return MaintenanceService.CanApply(player, doll, maintenanceID, out reason);
    }

    public MaintenanceApplicationResult ApplyMaintenance(string maintenanceID, DollEntity doll) {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        return MaintenanceService.Apply(player, doll, maintenanceID);
    }

    private void UnequipSameSlotProsthetic(DollEntity doll, ProstheticEntity newProsthetic) {
        if (doll?.EquippedProsthetics == null || newProsthetic == null || string.IsNullOrEmpty(newProsthetic.SlotType)) {
            return;
        }

        for (int i = doll.EquippedProsthetics.Count - 1; i >= 0; i--) {
            string equippedID = doll.EquippedProsthetics[i];
            if (ConfigManager.Prosthetics.TryGetValue(equippedID, out var equippedConfig)
                && equippedConfig.SlotType == newProsthetic.SlotType
                && equippedConfig.ProstheticID != newProsthetic.ProstheticID) {
                doll.EquippedProsthetics.RemoveAt(i);
                Debug.Log($"[WorkshopSystem] Unequipped prosthetic [{equippedID}] from slot [{newProsthetic.SlotType}].");
            }
        }
    }

    public bool SellItem(ItemEntity item, PlayerProfile player) {
        if (item == null || player == null) {
            return false;
        }

        bool sold = ItemLifecycleService.TrySellItem(player, item, "WorkshopSell", out ItemLifecycleResult result);
        if (!sold) {
            Debug.LogWarning($"[WorkshopSystem] Cannot sell item [{item?.Name ?? "null"}]: {result?.Reason ?? "Unknown reason"}");
        }

        return sold;
    }

    public int SellAllStashItems(PlayerProfile player) {
        if (player == null) {
            return 0;
        }

        ItemLifecycleBatchResult result = ItemLifecycleService.SellAllSellableItems(player, "WorkshopSellAll");
        if (!result.Success) {
            Debug.LogWarning($"[WorkshopSystem] Sell all completed with failures: {result.Reason}");
        }

        Debug.Log($"[WorkshopSystem] Sold all available items. Count={result.ItemCount}, TotalValue={result.MoneyDelta}G.");
        return result.MoneyDelta;
    }
}
