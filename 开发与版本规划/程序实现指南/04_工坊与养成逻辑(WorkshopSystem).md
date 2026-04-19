# 工坊与养成逻辑 (Workshop & Crafting System)

> **定位：** 本文档指导程序实现局外资源转化为实力的部分，包括升级底盘和制造义体。
> **原则：** 义体插件的效果实现与物品效果（`EffectBase`）完全同源，保持高度一致。

## 1. 制造配方数据解析 (Crafting Recipe)

由于我们定义了 MVP 的配置表，系统需要读取制造成本并进行库存比对。

```csharp
public class WorkshopSystem {
    // 检查玩家是否有足够的钱和材料
    public bool CanAfford(CraftingRecipeConfig recipe, PlayerProfile player) {
        if(player.Money < recipe.Cost.Money) return false;
        
        foreach(var reqItem in recipe.Cost.RequiredItems) {
            // 统计大仓库里这个配置ID的物品总量
            int countInStash = player.StashInventory.Count(item => item.ConfigID == reqItem.ConfigID);
            if(countInStash < reqItem.Count) {
                return false;
            }
        }
        return true;
    }
    
    // 执行扣除
    private void DeductCost(CraftingRecipeConfig recipe, PlayerProfile player) {
        player.Money -= recipe.Cost.Money;
        
        foreach(var reqItem in recipe.Cost.RequiredItems) {
            for(int i=0; i<reqItem.Count; i++) {
                // 找出并删除所需素材（优先删带负面词条或价值低的）
                var itemToRemove = player.StashInventory.First(item => item.ConfigID == reqItem.ConfigID);
                player.StashInventory.Remove(itemToRemove);
            }
        }
    }
}
```

## 2. 制造与装备义体 (Prosthetics System)

**核心机制：义体是脱离网格独立存在的，相当于传统RPG的全身光环。**
在我们的架构优化中，义体的效果与物品的效果底层是一模一样的！

```csharp
[System.Serializable]
public class ProstheticEntity {
    public string ProstheticID;
    public string SlotType; // Head, Core, Arm...
    
    // 义体的被动效果，复用 EffectData！
    public List<EffectData> PassiveEffects; 
}
```

## 3. 义体效果的局内应用 (Apply Passive Effects)

当玩家进入战斗或进行网格计算时，把义体里的 `EffectData` 也塞进 `EffectFactory` 里执行即可。

```csharp
// 在 GridSolver.RecalculateAllEffects 中统一调用：
public static void ApplyProstheticPassives(DollEntity doll) {
    foreach(string prosID in doll.EquippedProsthetics) {
        var prosConfig = ConfigManager.GetProstheticConfig(prosID);
        
        foreach(var effectData in prosConfig.PassiveEffects) {
            // 使用完全一样的工厂！
            EffectBase effect = EffectFactory.CreateEffect(effectData);
            
            // 全局光环通常作用于整个背包
            if(effectData.Target == TargetDirection.Global) {
                foreach(var targetItem in doll.RuntimeGrid.ContainedItems) {
                    effect.Apply(null, targetItem); // 提供者可为空，表示系统/义体赋予
                }
            }
        }
    }
}
```

---
**本篇总结：**
MVP 阶段的局外养成，其终极目的非常明确——消耗玩家从深渊带回来的材料，以此换取更大的空间以及全局乘伤被动。
通过将**义体特效与网格物品特效统一继承自 `EffectBase`**，极大地降低了程序的维护成本，实现了高度的数据驱动。