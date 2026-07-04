---
id: config_prosthetics_readme
title: 义体插件配置字段说明 (Prosthetics Config)
type: config
role: 策划
domain: config_prosthetics
status: active
source_of_truth: true
related:
  - 数值模型设计/00_基准价值与空间本位模型.md
  - 配置表(JSON)/Chassis/README.md
  - 配置表(JSON)/CraftingRecipes/README.md
  - 配置表(JSON)/README.md
  - 配置表(JSON)/Rewards/README.md
  - 设计文档/GDD/GDD_08_人偶养成子模块全案.md
  - 设计文档/GDD/GDD_04_小镇循环与经济物价波浪模型.md
  - 设计文档/规则卡/05_局外成长与维护规则卡.md
  - 设计文档/config/audits/36_局外成长正式配置承接审计.md
  - 设计文档/config/designs/37_局外成长正式配置落地设计.md
  - 设计文档/config/tasks/38_局外成长正式配置实现任务拆分.md
  - 设计文档/config/gates/39_局外成长正式配置ID锁定与冲突检查.md
  - 设计文档/config/designs/41_局外成长效果特质正式配置落地设计.md
  - 设计文档/config/designs/42_局外成长义体正式配置落地设计.md
  - 设计文档/config/designs/43_局外成长制造维护正式配置落地设计.md
  - 设计文档/config/validation/45_局外成长正式配置Validator与固定验收样例.md
last_verified: 2026-05-25
update_rule: 修改配置字段、数据源规则或表间引用时同步本文件。
---

# 义体插件配置字段说明 (Prosthetics Config)

本目录定义局外工坊可制造、安装和维护的义体插件。义体不占用背包网格，但必须受底盘 `AllowedProstheticSlots`、同槽互斥、制造成本、维护代价和触发反馈约束。

当前 JSON 仍可能保留 MVP 字段。正式配置落地时按本文补字段；程序尚未支持的字段先作为策划 / Validator warning，不把正式口径降级为旧字段。

## 正式职责

| 职责 | 说明 |
|---|---|
| 构筑方向 | 每个义体必须服务明确玩法，例如地图阅读、背包干涉抵抗、战斗爆发、SAN 稳定、材料转化或长线恢复。 |
| 装配约束 | 义体必须声明槽位、冲突组和底盘适配关系。 |
| 触发可读 | 义体效果必须说明触发时机、条件、反馈和失败边界。 |
| 局外消耗 | 义体必须接到制造配方、安装成本、维护倍率或磨损 / 侵蚀代价。 |
| 验收入口 | 至少能通过一条固定路径验证材料来源、安装、触发和反馈。 |

## 字段说明

| 字段 | 类型 | 必填 | 说明 |
|---|---|---|---|
| `ProstheticID` | string | 是 | 义体全局唯一 ID。正式 ID 建议使用 `prosthetic_*`。 |
| `DisplayName` | string | 是 | 玩家可见名称。旧字段 `Name` 仅作为兼容别名。 |
| `DescriptionKey` | string | 是 | 本地化描述 Key，说明构筑职责、触发和代价。 |
| `VisualID` | string | 是 | UI 图标或运行时表现资源 ID；缺图时必须有 fallback。 |
| `Tier` | string | 是 | 品级，例如 `Primary`、`Advanced`、`Rare`。旧字段 `Level` 作为兼容别名。 |
| `SlotType` | string | 是 | 安装槽位，例如 `CorePump`、`LeftArm`、`RightArm`、`HeadSensor`、`BackSpine`、`RightHand`。 |
| `Tags` | string[] | 是 | 玩法标签，例如 `RoutePreview`、`PackInterferenceResist`、`Charge`、`SanStability`。 |
| `EffectRefs` | object[] | 是 | 引用 `Effects` 字典或效果配置；旧 `Effects` 字段可兼容映射到本字段。 |
| `TriggerRules` | object[] | 是 | 触发规则，说明何时生效、触发次数、条件和目标。 |
| `CraftRecipeRef` | string | 是 | 对应制造配方 ID。初始赠送义体也应声明来源策略。 |
| `UnlockCondition` | object | 否 | 解锁条件，例如图纸、层级、Boss 核心、订单或剧情条件。 |
| `CraftCost` | object | 否 | 可选的冗余展示成本；正式消耗以 `CraftingRecipes` 为准。 |
| `InstallCost` | object | 否 | 安装 / 更换成本。 |
| `ConflictGroup` | string[] | 否 | 互斥组，同组不能同时生效。 |
| `MaintenanceModifier` | float | 否 | 对维护费用、磨损或稳定性的倍率，默认 1.0。 |
| `WearImpact` | object | 否 | 触发或下潜后产生的磨损影响。 |
| `CorruptionImpact` | object | 否 | 污染 / 侵蚀影响。 |
| `FeedbackRefs` | string[] | 否 | 义体触发、失效、安装或维护时的反馈 ID。 |

## EffectRefs / TriggerRules

义体效果继续可复用 `EffectFactory` / `EffectData` 结构，但正式配置必须补充触发语义。

```json
{
  "EffectID": "ReducePackInterference",
  "Level": 1,
  "Target": "Self",
  "Trigger": "OnFirstPackInterferencePerCombat",
  "Params": [1]
}
```

| 字段 | 类型 | 说明 |
|---|---|---|
| `EffectID` | string | 对应 `Effects` 字典或 C# `EffectFactory` 可识别 ID。 |
| `Level` | int | 效果等级。 |
| `Target` | string | 生效目标，例如 `Self`、`Global`、物品 Tag、怪物或资源类型。 |
| `Trigger` | string | 触发点，例如 `OnDungeonEnter`、`OnCombatStart`、`OnFirstPackInterferencePerCombat`、`OnExtractionSettlement`。 |
| `Resource` | string | 可选。资源类效果的目标资源，例如 `SAN`、`HP`、`Money`、`Wear`。 |
| `Operation` | string | 可选。运算方式，例如 `AddFlat`、`AddPercent`、`Multiply`。 |
| `Params` | float[] | 效果参数，含义见 `配置表(JSON)/Effects/README.md`。 |

## 本轮目标配置

| ProstheticID | 槽位 | 职责 |
|---|---|---|
| `prosthetic_focus_lens` | `HeadSensor` | 地图阅读和路线规划。 |
| `prosthetic_anchor_left_arm` | `LeftArm` | 抵抗背包干涉和封格。 |
| `prosthetic_charge_coil_arm` | `RightArm` | 充能武器爆发。 |
| `prosthetic_san_regulator_core` | `CorePump` | 降低短线 SAN 崩溃风险。 |
| `prosthetic_salvage_fingertips` | `RightHand` | 材料转化和拾荒收益。 |
| `prosthetic_mender_spine` | `BackSpine` | 长线恢复和安全区后续修复。 |

## Validator 建议

| ValidatorID | 检查 |
|---|---|
| `CFG-PROSTHETIC-001` | `ProstheticID`、`DisplayName`、`SlotType`、`Tier`、`EffectRefs` 或 `TriggerRules` 必填。 |
| `CFG-PROSTHETIC-002` | `SlotType` 必须被至少一个底盘允许；`RightHand` 等子槽可通过 `SlotCompatibilityAliases` 命中底盘槽位，但应给 warning。 |
| `CFG-PROSTHETIC-003` | `EffectID`、`CraftRecipeRef`、`FeedbackRefs` 引用存在或明确标记为占位 warning。 |
| `CFG-PROSTHETIC-004` | `ConflictGroup` 不得形成同义体自冲突。 |
| `CFG-PROSTHETIC-005` | 维护代价、磨损影响和触发频率不能形成无限收益。 |
| `CFG-PROSTHETIC-006` | `ConflictGroup` 不得导致同一义体自冲突，同槽同组义体不能同时生效。 |
| `CFG-PROSTHETIC-007` | 高收益义体必须至少有维护倍率、磨损、侵蚀、冷却、材料成本或槽位冲突等长期代价。 |
| `CFG-PROSTHETIC-008` | 旧 `pros_*` ID 不应作为正式内容输出；如保留必须写入 `LegacyAliases` 或测试兼容清单。 |

## 完成口径

README 字段口径完成只代表 `GROWTH-README` 的文档侧完成，不代表义体 JSON 已完成。`设计文档/config/designs/42_局外成长义体正式配置落地设计.md` 是 `GROWTH-PROSTHETICS` 的 JSON 修改前字段级设计，也不代表 JSON 已完成。JSON 修改后必须回写 `agent_status/design.md` 的配置状态和完成证据；如影响长期节点门禁，再通知 PM 更新 `版本规划/09_正式版核心纵切开发路线.md`。
