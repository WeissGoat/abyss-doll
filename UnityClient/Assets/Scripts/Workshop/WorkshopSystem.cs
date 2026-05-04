using System.Linq;
using UnityEngine;

public class WorkshopSystem {
    // 检查玩家是否有足够的钱和材料
    public bool CanAfford(CraftingCost cost, PlayerProfile player) {
        if (player.Money < cost.Money) return false;
        
        foreach (var reqItem in cost.RequiredItems) {
            // 统计大仓库里这个配置ID的物品总量
            int countInStash = player.StashInventory.Count(item => item.ConfigID == reqItem.ConfigID);
            if (countInStash < reqItem.Count) {
                return false;
            }
        }
        return true;
    }
    
    // 执行扣除
    private void DeductCost(CraftingCost cost, PlayerProfile player) {
        player.Money -= cost.Money;
        
        foreach (var reqItem in cost.RequiredItems) {
            for (int i = 0; i < reqItem.Count; i++) {
                // 找出并删除所需素材（优先删带负面词条或价值低的）
                var itemToRemove = player.StashInventory.First(item => item.ConfigID == reqItem.ConfigID);
                player.StashInventory.Remove(itemToRemove);
            }
        }
    }

    // 升级魔偶底盘
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
                // 深拷贝一份新的底盘数据赋予人偶
                string chassisJson = Newtonsoft.Json.JsonConvert.SerializeObject(nextChassis);
                doll.Chassis = Newtonsoft.Json.JsonConvert.DeserializeObject<ChassisComponent>(chassisJson);
                
                // 重置运行时网格
                // 现实中可能需要先将包里原有的物品缓存下来，升级后再放回去。MVP为了简化，只扩展空网格
                doll.RuntimeGrid = new BackpackGrid(doll.Chassis);
                
                Debug.Log($"[WorkshopSystem] 成功升级魔偶底盘为：{nextID}，新尺寸：{doll.Chassis.GridWidth}x{doll.Chassis.GridHeight}");
            } else {
                Debug.LogError($"[WorkshopSystem] Next Chassis ID not found in config: {nextID}");
            }
        } else {
            Debug.LogWarning("[WorkshopSystem] Cannot afford to upgrade chassis.");
        }
    }

    // 制造与装备义体
    public void CraftAndEquipProsthetic(string recipeID, DollEntity doll) {
        if (!ConfigManager.CraftingRecipes.TryGetValue(recipeID, out var recipe)) {
            Debug.LogError($"[WorkshopSystem] Recipe not found: {recipeID}");
            return;
        }
        
        if (CanAfford(recipe.Cost, GameRoot.Core.CurrentPlayer)) {
            DeductCost(recipe.Cost, GameRoot.Core.CurrentPlayer);
            
            string prosID = recipe.TargetProstheticID;
            
            if (!doll.EquippedProsthetics.Contains(prosID)) {
                doll.EquippedProsthetics.Add(prosID);
                Debug.Log($"[WorkshopSystem] 成功安装义体插件：{prosID}");
            }
        } else {
            Debug.LogWarning($"[WorkshopSystem] Cannot afford to craft prosthetic: {recipeID}");
        }
    }

    public bool SellItem(ItemEntity item, PlayerProfile player) {
        if (item == null || player == null) {
            return false;
        }

        if (!player.StashInventory.Contains(item)) {
            Debug.LogWarning($"[WorkshopSystem] Cannot sell item [{item?.Name ?? "null"}] because it is not in the stash.");
            return false;
        }

        player.StashInventory.Remove(item);
        player.Money += item.BaseValue;
        Debug.Log($"[WorkshopSystem] Sold item [{item.Name}] for {item.BaseValue}G.");
        return true;
    }

    public int SellAllStashItems(PlayerProfile player) {
        if (player == null || player.StashInventory == null || player.StashInventory.Count == 0) {
            return 0;
        }

        int totalValue = 0;
        foreach (var item in player.StashInventory) {
            if (item != null) {
                totalValue += item.BaseValue;
            }
        }

        int soldCount = player.StashInventory.Count;
        player.StashInventory.Clear();
        player.Money += totalValue;
        Debug.Log($"[WorkshopSystem] Sold all stash items. Count={soldCount}, TotalValue={totalValue}G.");
        return totalValue;
    }
}
