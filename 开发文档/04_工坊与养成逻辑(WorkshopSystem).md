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
  - 配置表(JSON)/Economy/README.md
  - 设计文档/GDD_10_势力声望与订单系统.md
  - 设计文档/GDD_04_小镇循环与经济物价波浪模型.md
last_verified: 2026-05-25
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

## 8. 维护真实服务

`MaintenanceService` 是局外成长的维护执行入口，用于把磨损、侵蚀、HP、SAN 等状态从“下潜阻断原因”转化为可被玩家用金币和材料解决的局外循环。

配置来源：

*   `配置表(JSON)/Maintenance/*.json` 是维护方案源数据。
*   `ConfigManager.MaintenanceConfigs` 负责运行时加载。
*   `ConfigValidator.ValidateMaintenanceConfigs` 校验 `MaintenanceID`、费用、目标状态、恢复量、每日次数字段和下潜许可恢复标记。

维护配置结构：

```csharp
public class MaintenanceConfig {
    public string MaintenanceID;
    public string Name;
    public string Description;
    public string QualityTier;
    public CraftingCost Cost;
    public List<MaintenanceEffectConfig> Effects;
    public int DailyLimit;
    public bool RestoresDivePermit;
}
```

执行规则：

*   `MaintenanceService.CanApply` 在扣费前完整校验玩家、人偶、配置、费用和全部效果合法性。
*   `WorkshopCostService` 统一统计与支付维护费用，覆盖仓库和当前出战人偶背包内材料；扣除顺序为仓库优先，仓库不足时再消耗背包材料。
*   维护效果目前支持 `Wear`、`Corruption`、`HP`、`SAN`。`Wear` / `Corruption` 是降低值；`HP` / `SAN` 是恢复值并夹取到上限。
*   HP / SAN 变化会发布 `GameEventBus.PublishHPChanged` / `PublishSANChanged`，供表现层刷新。
*   `WorkshopSystem.CanApplyMaintenance` 和 `WorkshopSystem.ApplyMaintenance` 只作为工坊系统的薄入口，不承载维护规则。
*   `RestoresDivePermit` 是配置语义标记，实际是否解除阻断仍由 `DiveReadinessService.Evaluate` 重新计算，不在维护服务中硬写“许可状态”。

当前首批配置：

*   `maint_basic_patch`：消耗金币和 `loot_gear_scrap`，降低磨损并恢复 HP。
*   `maint_purification_flush`：消耗金币和 `loot_toxic_filter`，降低侵蚀并恢复 SAN。

验证：

*   `MaintenanceServiceSmokeTest.Run` 覆盖磨损维护解除下潜阻断、侵蚀净化解除下潜阻断、费用不足不修改状态、维护费用可消耗背包材料。

## 9. 局外成长反馈与占位文本

`GrowthFeedbackService` 是局外成长 UI 的只读数据入口，用于把下潜许可、维护、制造和材料 / 金币缺口统一整理成 `GrowthFeedbackReport`。UI 不应直接扫描配置、背包或仓库去拼规则，也不应为了展示缺口执行维护或制造。

`GrowthReadabilityTextService` 是占位 UI / 正式 UI 的文本适配层。它只消费 `GrowthFeedbackReport`，输出下潜许可、下潜检查 issue、建议行动、费用缺口和汇总计数等 UI 可直接展示的文本快照。

边界：

*   `GrowthFeedbackService` 可以读取玩家、配置、仓库和当前背包，不能修改状态。
*   `GrowthReadabilityTextService` 只能做文本和快照适配，不能执行 `MaintenanceService.Apply`、义体制造、背包扣除或美术资源绑定。
*   工坊 UI、层级选择 UI 和后续正式整备界面应优先消费 `GrowthReadabilitySnapshot` 或 `GrowthFeedbackReport`，不要各自复制下潜许可和材料缺口逻辑。

验证：

*   `GrowthFeedbackServiceSmokeTest.Run` 覆盖制造缺口、维护解除下潜阻断和可下潜建议。
*   `GrowthReadabilityTextServiceSmokeTest.Run` 覆盖可下潜文本、维护阻断文本、制造缺口文本和汇总计数。

## 10. 人偶核心状态只读快照与占位文本

`DollCoreStateReadabilityService` 是人偶核心状态 UI 的只读数据入口，用于把当前出战人偶的 HP、SAN、SAN 阈值、推导情绪、维护风险、Bond 阶段、底盘、属性、特质和已装备义体整理成 UI 可直接展示的结构化快照。

输出结构：

*   `DollCoreStateReadabilitySnapshot`：包含基础数值、百分比、文本行、警告行、义体行和 `CombinedText`。
*   `DollCoreEmotionState`：当前第一版按 SAN 阈值和 Bond 等级推导 `Energetic`、`Calm`、`Tired`、`Depressed`、`Panic`、`Broken`。
*   `DollCoreProstheticLine`：把已装备义体 ID 映射为名称、槽位、等级和配置存在性，供占位 UI 或正式 UI 列表展示。

边界：

*   该服务只读取 `PlayerProfile.ActiveDoll` / `DollEntity` 和必要配置，不修改 HP、SAN、Bond、义体、特质、背包或维护状态。
*   维护风险只做展示和警告汇总，真正能否下潜仍由 `DiveReadinessService` 判定，真正恢复状态仍由 `MaintenanceService` 执行。
*   UI 不应直接从 `DollEntity` 拼接核心状态文案；后续工坊主页、人偶房间、层级选择和战败复盘应优先消费该快照。

验证：

*   `DollCoreStateReadabilityServiceSmokeTest.Run` 覆盖默认状态、SAN 阈值、维护风险、Bond 阶段、义体 / 特质列表和缺失人偶失败快照。

## 11. 人偶基础交互服务

`DollInteractionService` 是人偶日常交互的领域服务入口，用于把触摸、对话和赠礼统一结算为结构化结果。UI 只能调用服务并展示 `DollInteractionResult`，不得直接修改 Bond、SAN、每日次数或物品归属。

运行时状态：

*   `PlayerProfile.DollInteractionState` 保存按 Day 分组的交互计数。
*   `DollDailyInteractionState` 记录当日触摸、对话、赠礼次数、连续触摸区域和简短日志。
*   旧档案或测试档案如果缺失运行时状态，由服务入口自动补齐，不要求 UI 或外部流程手动初始化。

当前首版范围：

*   `ExecuteTouch` 支持工坊触摸，前 5 次产生少量 Bond；同一区域连续超过 3 次或超过每日有效次数后只返回反馈，不再产出数值。
*   低 SAN 时触摸头部 / 脸部会触发压力反馈，可能扣减少量 Bond / SAN；SAN 为 0 时只允许安抚反馈，不直接恢复状态。
*   `ExecuteTalk` 支持工坊日常对话与安全区上下文对话；每日前 3 次有效，之后返回兜底闲聊但不产出数值。
*   `GiveGift` 支持玩家已拥有物品赠礼；接受时才消耗物品，拒收时不消耗。第一版按物品类型、标签、价值和当日赠礼次数计算 Bond，诅咒类礼物拒收并产生负反馈。

边界：

*   本服务不实现完整对话池、不推进 Day、不绑定语音 / 立绘 / 动画、不读运行时美术资源。
*   保养 / 修复 / 净化的真实消耗和状态恢复仍由 `MaintenanceService` 负责；后续如需要“保养交互表现”，只在交互层包装反馈，不复制维护规则。
*   赠礼消耗只处理已拥有的仓库或当前出战背包物品；如果移除背包物品，会发布 `GameEventBus.PublishItemRemoved` 并重算背包效果。

验证：

*   `DollInteractionServiceSmokeTest.Run` 覆盖触摸每日上限、连续区域防刷、低 SAN 压力反馈、赠礼接受消耗、拒收不消耗、对话上限和场景权限。

## 12. 小镇经济压力链服务

`TownEconomyService` 是 P4 小镇经济压力链的领域服务入口。它不依赖 UI，也不在 UI Controller 中散写经济规则；后续账单、典当和营业界面只读取服务产出的报告对象。

`TownEconomyOverviewService` 是小镇经济 UI 的只读数据入口，用于汇总可出售物、各出售渠道估值、当前订单进度、有效传闻、势力摘要、月租压力和典当候选。它只读取玩家、配置、仓库和当前背包，不执行出售、订单交付、典当或扣款。

`TownEconomyReadabilityTextService` 是占位 UI / 正式 UI 的文本适配层。它只消费 `TownEconomyOverviewReport`，输出日历、金币、月租压力、可出售物、订单、传闻、势力和典当候选的结构化文本快照。后续正式 UI 可以直接消费 `TownEconomyReadabilitySnapshot`，也可以在表现层替换为卡片、列表或图标展示。

配置来源：

*   `配置表(JSON)/Economy/*.json` 是小镇经济配置源。
*   `ConfigManager.EconomyConfigs` 负责运行时加载。
*   `ConfigValidator.ValidateEconomyConfigs` 校验周期、月租曲线、欠债利息、轻债阈值和典当折扣。

当前配置结构：

```csharp
public class EconomyConfig {
    public string EconomyConfigID;
    public int WeekLength;
    public int MonthLength;
    public string RentCurveID;
    public List<RentCurveStepConfig> RentCurve;
    public int WorkshopMaintenanceBase;
    public int LicenseFeeBase;
    public float DebtInterestRate;
    public float LightDebtThresholdRatio;
    public float PawnValueMultiplier;
    public List<string> PawnProtectedTags;
}
```

执行规则：

*   `SettleDailyBusiness` 根据当日倾倒箱、橱窗、订单、黑市和支出输入生成 `DailyEconomyReport`，推进日历并记录月收入。
*   每月末由 `ResolveMonthlyRent` 计算 `BaseRent + WorkshopMaintenance + LicenseFee + DebtPrincipal + DebtInterest`。
*   金币足够时直接扣款；缺口小于等于 `LightDebtThresholdRatio` 时转入下月欠债。
*   缺口较大且存在可典当物时只返回 `PawnCandidates`，不自动典当；玩家或后续 UI 必须显式传入选中的典当物。
*   典当折扣由 `PawnValueMultiplier` 控制；命中 `PawnProtectedTags`、绑定物、订单物、剧情物、情感锚点或主线关键物不进入典当候选。
*   典当如果移除当前背包物品，会发布 `GameEventBus.PublishItemRemoved` 并重新计算背包效果。
*   `TownEconomyOverviewService` 和 `TownEconomyReadabilityTextService` 只做读模型和文本适配，不修改玩家金钱、订单状态、物品归属、背包格子或势力声望。

当前首批配置：

*   `economy_town_v1`：28 天月周期，第 1 月 1200G、第 2 月 2400G、第 3 月 4200G、第 4 月起 5200G，轻债阈值 10%，典当折扣 25%。

验证：

*   `TownEconomyServiceSmokeTest.Run` 覆盖日结报告与日历推进、月租支付、轻度欠账、玩家选择典当补足月租、保护物不进入典当候选。
*   `TownEconomyOverviewServiceSmokeTest.Run` 覆盖传闻影响最佳出售渠道、已接订单交付进度、月租压力、典当候选和势力摘要。
*   `TownEconomyReadabilityTextServiceSmokeTest.Run` 覆盖出售 / 传闻文本、订单进度文本、月租压力 / 典当 / 势力文本和汇总计数。
