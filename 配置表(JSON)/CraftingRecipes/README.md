---
id: config_craftingrecipes_readme
title: 工坊制造配方字段说明 (Crafting Recipes Config)
type: config
role: 策划
domain: config_crafting
status: active
source_of_truth: true
related:
  - 开发文档/数据与实体定义/05_经济与社会实体.md
  - 开发文档/04_工坊与养成逻辑(WorkshopSystem).md
  - 数值模型设计/01_经济循环与通缩模型.md
  - 数值模型设计/00_基准价值与空间本位模型.md
  - 配置表(JSON)/Chassis/README.md
  - 配置表(JSON)/README.md
  - 配置表(JSON)/Prosthetics/README.md
  - 设计文档/GDD/GDD_08_人偶养成子模块全案.md
  - 设计文档/rules/05_局外成长与维护规则卡.md
  - 设计文档/GDD/GDD_10_势力声望与订单系统.md
  - 设计文档/rules/07_势力声望与订单规则卡.md
  - 设计文档/GDD/GDD_04_小镇循环与经济物价波浪模型.md
  - 设计文档/config/audits/36_局外成长正式配置承接审计.md
  - 设计文档/config/designs/37_局外成长正式配置落地设计.md
  - 设计文档/config/tasks/38_局外成长正式配置实现任务拆分.md
  - 设计文档/config/gates/39_局外成长正式配置ID锁定与冲突检查.md
  - 设计文档/config/designs/40_局外成长底盘正式配置落地设计.md
  - 设计文档/config/designs/41_局外成长效果特质正式配置落地设计.md
  - 设计文档/config/designs/42_局外成长义体正式配置落地设计.md
  - 设计文档/config/designs/43_局外成长制造维护正式配置落地设计.md
  - 设计文档/config/validation/45_局外成长正式配置Validator与固定验收样例.md
last_verified: 2026-05-25
update_rule: 修改配置字段、数据源规则或表间引用时同步本文件。
---

# 工坊制造配方字段说明 (Crafting Recipes Config)

> 本目录定义小镇工坊中的制造、解锁、维护、材料转化和订单加工配方。正式版中，配方不是简单成本清单，而是把深渊掉落、局外成长、经济压力和订单目标连接起来的核心配置。
>
> 当前 JSON 仍可能保留 MVP 字段。正式配置落地时按本文补字段；程序尚未支持的字段先作为策划 / Validator warning，不把正式口径降级为旧字段。

## 正式职责

| 职责 | 说明 |
|---|---|
| 制造 | 把前三层材料、Boss 核心和图纸转化为义体、底盘解锁或关键物。 |
| 维护 | 支持 HP、SAN、磨损、侵蚀、义体稳定和下潜许可恢复。 |
| 材料转化 | 让废料、源石、污染物和精英材料进入长期构筑链。 |
| 订单加工 | 支撑势力订单和黑市路线的特殊处理。 |
| 消耗边界 | 保护订单绑定物、剧情锁定物和情感锚点物不被普通制造误消耗。 |

## 字段说明

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| `RecipeID` | string | 是 | 配方全局唯一 ID。 |
| `RecipeType` | string | 是 | 配方类型：`Prosthetic`、`ChassisUnlock`、`Maintenance`、`MaterialConvert`、`OrderProcess`、`ItemCraft`。 |
| `DisplayName` | string | 是 | 玩家可见名称。 |
| `DescriptionKey` | string | 是 | 本地化描述 Key，说明用途、输出和风险。 |
| `RequiredItems` | array | 否 | 指定 ID 输入，元素包含 `ConfigID` 和 `Count`。 |
| `RequiredTags` | array | 否 | 按标签输入，例如 `MechanicalScrap`、`PurificationMaterial`。 |
| `GoldCost` | int | 否 | 金币成本。旧 `Cost.Money` 可兼容映射。 |
| `BlueprintCondition` | string/object | 否 | 图纸或解锁条件。 |
| `OutputItemID` | string | 否 | 输出物品 ID。 |
| `OutputProstheticID` | string | 否 | 输出义体 ID。旧 `TargetProstheticID` 可兼容映射。 |
| `OutputChassisUnlock` | string | 否 | 解锁底盘 ID。 |
| `OutputMaintenanceID` | string | 否 | 触发维护方案 ID。 |
| `OutputCount` | int | 否 | 输出数量，默认 1。 |
| `ConsumePolicy` | string | 是 | 消耗策略，默认 `ConsumeAll`。 |
| `InheritTagsPolicy` | string | 否 | 标签继承策略，例如污染 / 订单 / 纪念物标签是否进入输出。 |
| `CancelPolicy` | string | 是 | 取消策略，例如 `RefundUnstarted`、`NoCancelAfterComplete`。 |
| `UnlockCondition` | object | 否 | 层级、Boss、订单、剧情或设施条件。 |
| `RiskTags` | string[] | 否 | 配方带来的风险标签。 |

## 兼容字段

当前旧字段仍可由程序兼容读取，但正式配置应逐步迁移：

| 旧字段 | 正式字段 |
|---|---|
| `TargetProstheticID` | `OutputProstheticID` |
| `Cost.Money` | `GoldCost` |
| `Cost.RequiredItems` | `RequiredItems` |

## 消耗与取消策略

| 策略 | 说明 |
|---|---|
| `ConsumeAll` | 普通材料和金币完整消耗。 |
| `KeepQuestBound` | 订单绑定、剧情锁定、情感锚点物不能被普通制造消耗。 |
| `KeepRiskTags` | 污染物参与制造时，风险标签进入输出或副作用。 |
| `RefundUnstarted` | 未开始制造可全额返还。 |
| `NoCancelAfterComplete` | 已完成制造不能撤销。 |
| `PartialRefundAfterStart` | 未来支持制造时间后再启用；当前不默认使用。 |

## 本轮目标配置

| 配方组 | 目标量 | 说明 |
|---|---:|---|
| 义体制造 | 6 | 对应 `prosthetic_focus_lens`、`prosthetic_anchor_left_arm`、`prosthetic_charge_coil_arm`、`prosthetic_san_regulator_core`、`prosthetic_salvage_fingertips`、`prosthetic_mender_spine`。 |
| 底盘解锁 | 2 | 对应 `chassis_compact_raider`、`chassis_bulwark_carrier`。 |
| 维护方案 | 5 | `maint_basic_patch`、`maint_standard_overhaul`、`maint_purification_flush`、`maint_deep_therapy`、`maint_field_tuning`。 |

## Validator 建议

| ValidatorID | 检查 |
|---|---|
| `CFG-CRAFT-001` | `RecipeID`、`RecipeType`、输入、输出和成本必填。 |
| `CFG-CRAFT-002` | 输入 ItemID / Tag / Blueprint 引用存在，且不能引用运行时副本。 |
| `CFG-CRAFT-003` | 输出 ItemID / ProstheticID / ChassisID / MaintenanceID 引用存在或明确为解锁。 |
| `CFG-CRAFT-004` | `ConsumePolicy` 不消耗订单绑定、剧情锁定或情感锚点物。 |
| `CFG-MAINT-001` | 维护方案必须声明目标状态、成本、恢复量、副作用和下潜许可影响。 |
| `CFG-MAINT-002` | 维护不能同时免费清除 HP、SAN、磨损、侵蚀、订单、污染物和特质历史。 |

## 完成口径

README 字段口径完成只代表 `GROWTH-README` 的文档侧完成，不代表配方 JSON 已完成。JSON 修改后必须回写 `agent_status/design.md` 的配置状态和完成证据；如影响长期节点门禁，再通知 PM 更新 `版本规划/09_正式版核心纵切开发路线.md`。
