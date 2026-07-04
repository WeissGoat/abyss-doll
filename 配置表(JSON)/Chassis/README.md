---
id: config_chassis_readme
title: 局外底盘配置字段说明 (Chassis Config)
type: config
role: 策划
domain: config_chassis
status: active
source_of_truth: true
related:
  - 数值模型设计/00_基准价值与空间本位模型.md
  - 配置表(JSON)/CraftingRecipes/README.md
  - 配置表(JSON)/README.md
  - 配置表(JSON)/Prosthetics/README.md
  - 配置表(JSON)/Rewards/README.md
  - 设计文档/GDD/GDD_08_人偶养成子模块全案.md
  - 设计文档/GDD/GDD_04_小镇循环与经济物价波浪模型.md
  - 设计文档/规则卡/05_局外成长与维护规则卡.md
  - 设计文档/config/audits/36_局外成长正式配置承接审计.md
  - 设计文档/config/designs/37_局外成长正式配置落地设计.md
  - 设计文档/config/tasks/38_局外成长正式配置实现任务拆分.md
  - 设计文档/config/gates/39_局外成长正式配置ID锁定与冲突检查.md
  - 设计文档/config/designs/40_局外成长底盘正式配置落地设计.md
  - 设计文档/config/designs/42_局外成长义体正式配置落地设计.md
  - 设计文档/config/designs/43_局外成长制造维护正式配置落地设计.md
  - 设计文档/config/validation/45_局外成长正式配置Validator与固定验收样例.md
last_verified: 2026-05-25
update_rule: 修改配置字段、数据源规则或表间引用时同步本文件。
---

# 局外底盘配置字段说明 (Chassis Config)

> 本目录定义人偶可安装的底盘。底盘不是单纯等级，而是正式构筑方向：它决定背包网格形状、义体槽位、维护代价、下潜许可和路线倾向。
>
> 当前 JSON 仍可能保留 MVP 字段。正式配置落地时按本文补字段；程序尚未支持的字段先作为策划 / Validator warning，不把正式口径降级为旧字段。

## 正式职责

| 职责 | 说明 |
|---|---|
| 背包形状 | 定义 `GridWidth`、`GridHeight`、`GridMask` 和可用槽位标签。 |
| 构筑方向 | 用 `ChassisRole` 区分标准、轻装、重载、路线预览、订单承载等玩法职责。 |
| 义体骨架 | 用 `AllowedProstheticSlots` 决定可安装槽位，而不是让义体自由堆叠。 |
| 局外代价 | 用图纸、材料、金币、适应期和维护倍率约束强底盘。 |
| 验收入口 | 通过下潜许可、背包格、义体槽、维护费用和路线反馈验证。 |

## 字段说明

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| `ChassisID` | string | 是 | 全局唯一 ID。正式 ID 建议使用 `chassis_*`，例如 `chassis_standard_frame`。 |
| `DisplayName` | string | 是 | 玩家可见名称。旧字段 `Name` 仅作为兼容别名。 |
| `DescriptionKey` | string | 是 | 本地化描述 Key，说明构筑职责和代价。 |
| `ChassisRole` | string[] | 是 | 构筑职责标签，例如 `Baseline`、`Scout`、`HeavyCarry`、`RoutePreview`、`GridResist`。 |
| `VisualID` | string | 是 | UI / 美术图标或底盘表现资源 ID；缺图时必须有 fallback。 |
| `GridWidth` | int | 是 | 背包网格宽度。 |
| `GridHeight` | int | 是 | 背包网格高度。 |
| `GridMask` | bool[][] | 是 | 网格可用性掩码；允许异形背包，但必须与宽高一致。 |
| `BaseSlotTags` | string[] | 否 | 底盘自带槽位语义，例如 `StableCore`、`WideCargo`、`FragileEdge`。 |
| `AllowedProstheticSlots` | string[] | 是 | 可安装义体槽位，例如 `CorePump`、`LeftArm`、`RightArm`、`HeadSensor`、`BackSpine`。 |
| `UnlockCondition` | object | 否 | 解锁条件，包含图纸、层级、Boss / 精英材料、订单或剧情条件。初始底盘可为空。 |
| `BlueprintRef` | string | 否 | 图纸 ID。可放在 `UnlockCondition` 内，也可作为兼容字段独立存在。 |
| `BuildCost` | object | 否 | 首次制造 / 解锁成本。 |
| `InstallCost` | object | 否 | 更换或安装成本。 |
| `AdaptationDays` | int | 否 | 适应期天数；强底盘不能完全无代价。 |
| `MaintenanceModifier` | float | 否 | 维护费用或磨损倍率，默认 1.0。 |
| `RuleModifiers` | object[] | 否 | 底盘提供的规则修正，例如路线预览、封格抗性、负重修正。 |
| `RiskTags` | string[] | 否 | 风险标签，例如 `LowCapacity`、`HighMaintenance`。 |
| `EndingTags` | string[] | 否 | 长线构筑 / 结局倾向标签。 |

## 成本对象

`BuildCost`、`InstallCost` 和旧 `UpgradeCost` 均使用同一成本结构：

| 字段名 | 类型 | 说明 |
|---|---|---|
| `Money` | int | 金币成本。 |
| `RequiredItems` | array | 消耗物品列表，元素包含 `ConfigID` 和 `Count`。 |
| `RequiredTags` | array | 可选。按标签消耗的材料条件。 |
| `ConsumePolicy` | string | 可选。默认 `ConsumeAll`；订单绑定、剧情锁定和情感锚点物不得被普通制造误消耗。 |

旧 `UpgradeCost.NextChassisID` 只作为兼容字段。正式底盘更推荐用 `CraftingRecipes` 的 `RecipeType=ChassisUnlock` 表达解锁关系。

## 本轮目标配置

| ChassisID | 职责 | 设计口径 |
|---|---|---|
| `chassis_standard_frame` | 标准 / 教学 | 初始底盘，提供基础背包格、基础义体槽和无适应期对照。 |
| `chassis_compact_raider` | 轻装 / 路线预览 | 总面积较小但路线信息更强，适合撤离和探索。 |
| `chassis_bulwark_carrier` | 重载 / 订单承载 | 面积更大、抗干涉更强，但维护倍率和适应期更高。 |

## Validator 建议

| ValidatorID | 检查 |
|---|---|
| `CFG-CHASSIS-001` | `ChassisID`、`DisplayName`、`ChassisRole`、`GridWidth`、`GridHeight`、`GridMask` 必填且 ID 唯一。 |
| `CFG-CHASSIS-002` | `GridMask` 尺寸必须与宽高一致，且至少存在一个可用格。 |
| `CFG-CHASSIS-003` | `AllowedProstheticSlots` 必须来自正式槽位枚举。 |
| `CFG-CHASSIS-004` | `VisualID`、图纸、材料和配方引用必须存在，或显式标记为占位 warning。 |
| `CFG-CHASSIS-005` | `AdaptationDays`、`MaintenanceModifier` 值域合法，强底盘不能无代价。 |

## 完成口径

README 字段口径完成只代表 `GROWTH-README` 的文档侧完成，不代表底盘 JSON 已完成。`设计文档/config/designs/40_局外成长底盘正式配置落地设计.md` 是 `GROWTH-CHASSIS` 的 JSON 修改前字段级设计，也不代表 JSON 已完成。JSON 修改后必须回写 `agent_status/design.md` 的配置状态和完成证据；如影响长期节点门禁，再通知 PM 更新 `版本规划/09_正式版核心纵切开发路线.md`。
