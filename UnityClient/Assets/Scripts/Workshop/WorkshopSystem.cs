using System.Linq;
using UnityEngine;

public class WorkshopSystem {
    // 检查玩家是否有足够的钱和材料
    public bool CanAfford(CraftingRecipeConfig recipe, PlayerProfile player) {
        if (player.Money < recipe.Cost.Money) return false;
        
        foreach (var reqItem in recipe.Cost.RequiredItems) {
            // 统计大仓库里这个配置ID的物品总量
            int countInStash = player.StashInventory.Count(item => item.ConfigID == reqItem.ConfigID);
            if (countInStash < reqItem.Count) {
                return false;
            }
        }
        return true;
    }
    
    // 执行扣除
    private void DeductCost(CraftingRecipeConfig recipe, PlayerProfile player) {
        player.Money -= recipe.Cost.Money;
        
        foreach (var reqItem in recipe.Cost.RequiredItems) {
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

        // 在完整的配置表中，ChassisComponent 应该包含 UpgradeCost 字段
        // MVP阶段，为了保证测试先占位（目前 ChassisComponent 定义中未加 UpgradeCost，此处写死或跳过具体实现）
        Debug.Log("[WorkshopSystem] UpgradeDollChassis called. Implementation pending Config alignment.");
    }

    // 制造与装备义体
    public void CraftAndEquipProsthetic(string recipeID, DollEntity doll) {
        if (!ConfigManager.CraftingRecipes.TryGetValue(recipeID, out var recipe)) {
            Debug.LogError($"[WorkshopSystem] Recipe not found: {recipeID}");
            return;
        }
        
        if (CanAfford(recipe, GameRoot.Core.CurrentPlayer)) {
            DeductCost(recipe, GameRoot.Core.CurrentPlayer);
            
            string prosID = recipe.TargetProstheticID;
            
            if (!doll.EquippedProsthetics.Contains(prosID)) {
                doll.EquippedProsthetics.Add(prosID);
                Debug.Log($"[WorkshopSystem] 成功安装义体插件：{prosID}");
            }
        } else {
            Debug.LogWarning($"[WorkshopSystem] Cannot afford to craft prosthetic: {recipeID}");
        }
    }
}
