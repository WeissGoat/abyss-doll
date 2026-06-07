using UnityEngine;

public class WorkshopSystem {
    public bool CanAfford(CraftingCost cost, PlayerProfile player) {
        return WorkshopCostService.CanAfford(cost, player, out _);
    }

    public ChassisUpgradeResult UpgradeDollChassis(DollEntity doll) {
        ChassisUpgradeResult result = ChassisUpgradeService.Upgrade(GameRoot.Core?.CurrentPlayer, doll);
        if (!result.Success) {
            Debug.LogWarning($"[WorkshopSystem] Cannot upgrade chassis: {result.Reason}");
        }

        return result;
    }

    public bool CraftAndEquipProsthetic(string recipeID, DollEntity doll) {
        return CraftAndEquipProstheticWithResult(recipeID, doll).Success;
    }

    public ProstheticCraftingResult CraftAndEquipProstheticWithResult(string recipeID, DollEntity doll) {
        ProstheticCraftingResult result = ProstheticCraftingService.CraftAndEquip(GameRoot.Core?.CurrentPlayer, doll, recipeID);
        if (!result.Success) {
            Debug.LogWarning($"[WorkshopSystem] Cannot craft prosthetic [{recipeID}]: {result.Reason}");
        }

        return result;
    }

    public bool CanApplyMaintenance(string maintenanceID, DollEntity doll, out string reason) {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        return MaintenanceService.CanApply(player, doll, maintenanceID, out reason);
    }

    public MaintenanceApplicationResult ApplyMaintenance(string maintenanceID, DollEntity doll) {
        PlayerProfile player = GameRoot.Core?.CurrentPlayer;
        return MaintenanceService.Apply(player, doll, maintenanceID);
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
