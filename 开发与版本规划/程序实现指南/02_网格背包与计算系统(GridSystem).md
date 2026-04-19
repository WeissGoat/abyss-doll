# 网格背包与计算系统 (Grid System)

> **定位：** 本文档指导程序如何实现“背包俄罗斯方块”逻辑，以及局内 Effect 效果工厂的核心算法。
> **技术难点：** 形状碰撞检测、旋转坐标系转换、工厂模式解耦效果。

## 1. 核心背包容器 (BackpackGrid)

背包不仅仅是一个 List，它是实打实的二维矩阵。我们需要一个二维数组来记录每个格子“被谁占了”。

```csharp
public class BackpackGrid {
    public int Width { get; private set; }
    public int Height { get; private set; }
    
    // 二维矩阵，存放对应格子上物品的 InstanceID。为 null 代表空位。
    private string[,] _gridMatrix; 
    
    public List<ItemEntity> ContainedItems { get; private set; }

    public BackpackGrid(ChassisComponent chassis) {
        Width = chassis.GridWidth;
        Height = chassis.GridHeight;
        _gridMatrix = new string[Width, Height];
        ContainedItems = new List<ItemEntity>();
        
        // 根据底盘的 GridMask 初始化死格
        for(int x = 0; x < Width; x++) {
            for(int y = 0; y < Height; y++) {
                if(!chassis.GridMask[x][y]) {
                    _gridMatrix[x,y] = "LOCKED_CELL";
                }
            }
        }
    }
}
```

## 2. 形状变换与碰撞检测逻辑

**旋转坐标计算 (Rotate Shape)：**
当物品发生 90 度旋转时，原本的局部坐标 `(x, y)` 应当转换为 `(-y, x)`。计算后重新将所有点偏移为以 `(0,0)` 为基准的正数坐标。

**能否放置判定 (CanPlaceItem)：**
```csharp
public bool CanPlaceItem(ItemEntity item, int targetX, int targetY) {
    // 1. 获取物品当前旋转状态下的所有相对坐标
    Vector2Int[] currentShape = GetRotatedShape(item.GridComp);

    foreach(var point in currentShape) {
        int checkX = targetX + point.x;
        int checkY = targetY + point.y;

        // 2. 越界检测
        if(checkX < 0 || checkX >= Width || checkY < 0 || checkY >= Height) {
            return false;
        }

        // 3. 碰撞检测
        if(!string.IsNullOrEmpty(_gridMatrix[checkX, checkY])) {
            // 如果检查的格子不是空位，且不是这个物品自己（移动时），则无法放置
            if(_gridMatrix[checkX, checkY] != item.InstanceID) {
                return false;
            }
        }
    }
    return true; // 完美无瑕，可以放置！
}
```

## 3. 效果工厂与网格解算器 (GridSolver & EffectFactory)

**所有的物品连结Buff、自身特效，统一采用工厂模式 (Factory Pattern) 组织，全部继承自 `EffectBase`。**
这能彻底干掉 if/else 面条代码。

```csharp
// 效果基类
public abstract class EffectBase {
    public string EffectID { get; protected set; }
    public int Level { get; protected set; }
    
    public virtual void Init(EffectData data) {
        EffectID = data.EffectID;
        Level = data.Level;
    }
    
    // 激活效果
    public abstract void Apply(ItemEntity provider, ItemEntity target);
    // 移除效果
    public abstract void Remove(ItemEntity provider, ItemEntity target);
}

// 具体的伤害增幅效果实现
public class DamageMultiplierEffect : EffectBase {
    private float _multiplier;
    
    public override void Init(EffectData data) {
        base.Init(data);
        _multiplier = data.Params[0] + (Level * 0.05f); // 支持升级带来的系数成长
    }

    public override void Apply(ItemEntity provider, ItemEntity target) {
        if(target.CombatComp != null) {
            target.CombatComp.RuntimeDamage *= (1.0f + _multiplier);
        }
    }
    
    public override void Remove(ItemEntity provider, ItemEntity target) {
        // 还原逻辑
    }
}

// 效果工厂
public static class EffectFactory {
    public static EffectBase CreateEffect(EffectData data) {
        switch(data.EffectID) {
            case "DamageMultiplier":
                var dmgEff = new DamageMultiplierEffect();
                dmgEff.Init(data);
                return dmgEff;
            // case "xxx": return new XXXEffect();
            default: return null;
        }
    }
}
```

```csharp
// 网格解算器
public class GridSolver {
    public static void RecalculateAllEffects(BackpackGrid grid) {
        // 1. 清空所有物品之前的运行时 Buff
        foreach(var item in grid.ContainedItems) {
            ResetRuntimeStats(item);
        }
        
        // 2. 遍历每一个物品，生成并执行 Effect
        foreach(var providerItem in grid.ContainedItems) {
            if(providerItem.CombatComp == null || providerItem.CombatComp.Effects == null) continue;
            
            foreach(var effectData in providerItem.CombatComp.Effects) {
                EffectBase effect = EffectFactory.CreateEffect(effectData);
                
                // 3. 寻找符合方向的相邻格子 (如果是 TargetDirection.Self 则目标为自己)
                List<ItemEntity> targets = GetTargetItems(providerItem, effectData.Target, grid);
                
                foreach(var targetItem in targets) {
                    effect.Apply(providerItem, targetItem);
                }
            }
        }
    }
}
```