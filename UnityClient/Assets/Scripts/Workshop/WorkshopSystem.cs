using System.Linq;
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
            if (CountOwnedItems(player, reqItem.ConfigID) < reqItem.Count) {
                return false;
            }
        }

        return true;
    }

    private void DeductCost(CraftingCost cost, PlayerProfile player) {
        player.Money -= cost.Money;

        foreach (var reqItem in cost.RequiredItems) {
            for (int i = 0; i < reqItem.Count; i++) {
                RemoveOwnedItemForCost(player, reqItem.ConfigID);
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

        if (TrySellItemFromBackpack(item, player)) {
            return true;
        }

        if (player.StashInventory.Contains(item)) {
            player.StashInventory.Remove(item);
            player.Money += item.BaseValue;
            Debug.Log($"[WorkshopSystem] Sold stash item [{item.Name}] for {item.BaseValue}G.");
            return true;
        }

        Debug.LogWarning($"[WorkshopSystem] Cannot sell item [{item?.Name ?? "null"}] because it is neither in the backpack nor the stash.");
        return false;
    }

    public int SellAllStashItems(PlayerProfile player) {
        if (player == null) {
            return 0;
        }

        BackpackGrid grid = player.ActiveDoll?.RuntimeGrid as BackpackGrid;
        int totalValue = 0;
        int soldCount = 0;

        if (grid != null && grid.ContainedItems.Count > 0) {
            foreach (var item in grid.ContainedItems.ToList()) {
                if (item == null) {
                    continue;
                }

                totalValue += item.BaseValue;
                soldCount++;
                RemoveBackpackItemForSale(item, player);
            }
        }

        foreach (var item in player.StashInventory) {
            if (item != null) {
                totalValue += item.BaseValue;
                soldCount++;
            }
        }

        player.StashInventory.Clear();
        player.Money += totalValue;
        Debug.Log($"[WorkshopSystem] Sold all available items. Count={soldCount}, TotalValue={totalValue}G.");
        return totalValue;
    }

    private bool TrySellItemFromBackpack(ItemEntity item, PlayerProfile player) {
        BackpackGrid grid = player.ActiveDoll?.RuntimeGrid as BackpackGrid;
        if (grid == null || !grid.ContainedItems.Contains(item)) {
            return false;
        }

        RemoveBackpackItemForSale(item, player);
        player.Money += item.BaseValue;
        Debug.Log($"[WorkshopSystem] Sold backpack item [{item.Name}] for {item.BaseValue}G.");
        return true;
    }

    private void RemoveBackpackItemForSale(ItemEntity item, PlayerProfile player) {
        BackpackGrid grid = player.ActiveDoll?.RuntimeGrid as BackpackGrid;
        if (grid == null || item == null || !grid.ContainedItems.Contains(item)) {
            return;
        }

        grid.RemoveItem(item);
        GridSolver.RecalculateAllEffects(player.ActiveDoll);
        GameEventBus.PublishItemRemoved(item.InstanceID);
    }

    private int CountOwnedItems(PlayerProfile player, string configID) {
        if (player == null || string.IsNullOrEmpty(configID)) {
            return 0;
        }

        int count = player.StashInventory.Count(item => item != null && item.ConfigID == configID);
        BackpackGrid grid = player.ActiveDoll?.RuntimeGrid as BackpackGrid;
        if (grid != null) {
            count += grid.ContainedItems.Count(item => item != null && item.ConfigID == configID);
        }

        return count;
    }

    private bool RemoveOwnedItemForCost(PlayerProfile player, string configID) {
        if (player == null || string.IsNullOrEmpty(configID)) {
            return false;
        }

        ItemEntity stashItem = player.StashInventory.FirstOrDefault(item => item != null && item.ConfigID == configID);
        if (stashItem != null) {
            player.StashInventory.Remove(stashItem);
            return true;
        }

        BackpackGrid grid = player.ActiveDoll?.RuntimeGrid as BackpackGrid;
        ItemEntity backpackItem = grid?.ContainedItems.FirstOrDefault(item => item != null && item.ConfigID == configID);
        if (grid == null || backpackItem == null) {
            return false;
        }

        grid.RemoveItem(backpackItem);
        GridSolver.RecalculateAllEffects(player.ActiveDoll);
        GameEventBus.PublishItemRemoved(backpackItem.InstanceID);
        return true;
    }
}
