---
id: dev_02_grid_system
title: 网格背包与计算系统 (Grid System)
type: dev
role: 程序
domain: grid_inventory
status: active
source_of_truth: true
related:
  - 开发文档/数据与实体定义/03_物品与网格实体.md
  - 数值模型设计/00_基准价值与空间本位模型.md
  - 配置表(JSON)/Items/README.md
  - 设计文档/GDD_01_背包战斗与局内网格机制.md
  - 设计文档/GDD_06_物品系统与物品生命周期.md
last_verified: 2026-05-23
update_rule: 修改对应程序架构、接口契约、验证流程或 Unity 实现边界时同步本文件。
---

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

### 2.1 旋转坐标计算

物品旋转只允许使用 `0/90/180/270` 四种角度。任何输入角度都必须先通过 `BackpackGrid.NormalizeRotation()` 归一化。

旋转规则：

```text
0:   ( x,  y)
90:  (-y,  x)
180: (-x, -y)
270: ( y, -x)
```

旋转后必须重新将所有局部坐标平移到以左上 `(0,0)` 为基准的正数坐标。否则 L 形、长条形或 180 度旋转后的物品会因为出现负坐标而被误判越界。

当前正式入口：

```csharp
BackpackGrid.NormalizeRotation(int rotation);
BackpackGrid.GetNormalizedShapeCells(ItemEntity item, int rotation);
BackpackGrid.TryGetRotatedBounds(ItemEntity item, int rotation, out int width, out int height);
```

### 2.2 放置校验结果

`CanPlaceItem()` 只适合历史兼容和简单 bool 判断。正式交互、拖拽预览、失败提示和自动化测试应使用 `EvaluatePlacement()`。

```csharp
public enum BackpackPlacementFailure {
    None,
    MissingItem,
    MissingShape,
    OutOfBounds,
    LockedCell,
    OccupiedCell
}

public struct BackpackPlacementResult {
    public bool CanPlace;
    public BackpackPlacementFailure Failure;
    public string Reason;
    public int X;
    public int Y;
    public int Rotation;
    public List<int[]> OccupiedCells;
}
```

约定：

* `OccupiedCells` 是旋转后、平移到目标坐标后的真实占格列表。
* UI 拖拽预览必须消费 `OccupiedCells`，不得自己重新推导占格。
* `Failure` 用于自动化测试和日志定位，`Reason` 用于 UI 失败提示。
* `PlaceItem(item, x, y, rotation)` 会在成功后写回 `item.Grid.CurrentPos` 和规范化后的 `item.Grid.Rotation`。

### 2.3 背包交互服务

UI 不直接调用 `BackpackGrid.PlaceItem()` / `RemoveItem()`。玩家交互统一经过 `InventoryInteractionService`：

```csharp
RequestPickUp(...)
PreviewPlacement(...)
RequestRotateHeldItem(...)
RequestPlace(...)
RequestRestore(...)
RequestStageDiscard(...)
```

旋转只允许发生在“已拿起”的交互态。仍在背包矩阵中的物品不能直接修改 `Rotation`，否则矩阵占格和物品自身状态会不同步。

成功放置后，服务负责：

```text
BackpackGrid.PlaceItem
-> GridSolver.RecalculateAllEffects
-> GameEventBus.PublishItemPlaced
```

因此相邻、方向、光环、连接链路和被动效果必须在放置后立即刷新。

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
