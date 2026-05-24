---
id: dev_04_workshop_system
title: 工坊与养成逻辑 (Workshop & Crafting System)
type: dev
role: 程序
domain: workshop
status: active
source_of_truth: true
related:
  - 开发文档/数据与实体定义/05_经济与社会实体.md
  - 数值模型设计/01_经济循环与通缩模型.md
  - 配置表(JSON)/CraftingRecipes/README.md
  - 设计文档/GDD_10_势力声望与订单系统.md
  - 设计文档/GDD_04_小镇循环与经济物价波浪模型.md
last_verified: 2026-05-23
update_rule: 修改对应程序架构、接口契约、验证流程或 Unity 实现边界时同步本文件。
---

# 工坊与养成逻辑 (Workshop & Crafting System)

> **定位：** 指导局外资源转化为实力，包括升级底盘、制造并装备义体。
> **原则：** 义体效果与物品效果完全同源，统一使用 `EffectData` + `EffectFactory`。

## 1. 成本来源

MVP 当前不再强依赖仓库。玩家撤离后战利品可能仍在背包里，因此工坊成本统计必须覆盖：

*   `PlayerProfile.StashInventory`
*   当前魔偶 `RuntimeGrid` 背包内物品

扣除成本时优先消耗仓库材料；仓库不足时再从背包移除，并发布 `GameEventBus.PublishItemRemoved` 以刷新表现层。

## 2. 升级魔偶底盘

底盘升级仍读取 `Chassis.UpgradeCost`，复用工坊成本扣除逻辑。升级成功后替换 `DollEntity.Chassis`，并重建 `BackpackGrid`。

MVP 简化口径：升级会重建背包网格，不做旧物品自动回填。

## 3. 制造与装备义体

义体通过 `CraftingRecipeConfig.TargetProstheticID` 指向 `ProstheticEntity`：

```csharp
public class ProstheticEntity {
    public string ProstheticID;
    public string Name;
    public string Level;
    public string SlotType;
    public List<EffectData> Effects;
}
```

统一约束：

*   只使用 `Effects: EffectData[]`。
*   不再保留 `PassiveEffect` / `PassiveEffects` 兼容字段。
*   同一个 `SlotType` 同时只装备一个义体；制造新义体时会替换同槽位旧义体。
*   制造成功后立即 `GridSolver.RecalculateAllEffects(doll)`。

## 4. 义体效果应用

义体效果分两类消费：

*   网格/物品派生值：`GridSolver.RecalculateAllEffects` 遍历 `doll.EquippedProsthetics`，读取每个义体的 `Effects`，通过 `EffectFactory` 实例化并作用于背包物品。
*   战斗事件：`DollFighter.ProcessEffects` 同样遍历义体 `Effects`，当 `EffectBase.ListenEvent` 匹配当前 `CombatEventType` 时执行。

`Target` 口径：

*   `Global`：作用于全部背包物品。
*   物品 Tag，如 `Melee`：只作用于拥有该 Tag 的物品。
*   `Self`：用于战斗事件类效果，例如 `RestoreSANOnCombatEnd`。

## 5. MVP 已接入效果

*   `DamageMultiplier`：`pros_power_arm` 使用，目标 `Melee`，用于提高近战武器实际伤害。
*   `RestoreSANOnCombatEnd`：`pros_cooling_system` 使用，战斗胜利发布 `OnCombatEnd` 时恢复 SAN。

## 6. UI 入口

`WorkshopUIController` 会根据 `ConfigManager.CraftingRecipes` 自动生成义体制造按钮。按钮状态由 `WorkshopSystem.CanAfford` 和当前是否已装备决定。

## 7. 下潜许可服务

`DiveReadinessService` 是局外成长进入深渊前的统一领域检查入口，不归属 UI，也不由 `DungeonManager` 直接散写规则。

当前检查范围：

*   玩家档案、层级 ID、层级配置和层级解锁。
*   当前出战人偶是否存在。
*   磨损与侵蚀阈值：`WearAndTear >= 90` 禁止下潜，`Corruption >= 90` 禁止下潜；高磨损 / 高侵蚀只返回 warning。
*   底盘是否存在、底盘 ID、网格尺寸、`GridMask` 与运行时 `BackpackGrid` 是否一致。
*   已装备义体引用是否存在、槽位是否为空、同槽位是否重复。

返回结构：

*   `DiveReadinessResult.CanDive`：是否允许下潜。
*   `Issues`：结构化阻断 / 警告 / 信息，供深渊入口、工坊 UI 或后续局外界面复用。
*   `BuildSummary()`：给现有事件和占位 UI 使用的短文本原因。
*   `RemovedProstheticIDs`：正式开始下潜时按规则自动卸下的非法义体引用。

调用边界：

*   `DungeonManager.CanStartAtLayer()` 只做无副作用预检。
*   `DungeonManager.StartRunAtLayer()` 在真正开始前允许 `autoUnequipIllegalProsthetics`，只自动修复规则允许自动卸下的义体问题。
*   维护、净化、底盘修复和背包网格重建仍应由后续局外成长 / 维护服务处理，不能在下潜入口里静默修正。

验证：

*   `DiveReadinessSmokeTest.Run` 覆盖正常下潜、极端磨损、极端侵蚀、缺底盘、运行时网格不匹配、非法义体自动卸下和 warning 不阻断下潜。
