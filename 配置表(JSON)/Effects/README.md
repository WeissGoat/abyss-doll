---
id: config_effects_readme
title: 效果配置字典说明 (EffectEnums Config)
type: config
role: 策划
domain: config_effects
status: active
source_of_truth: true
related:
  - 开发文档/数据与实体定义/02_人偶与状态实体.md
  - 配置表(JSON)/README.md
  - 配置表(JSON)/Dolls/README.md
  - 设计文档/GDD/GDD_08_人偶养成子模块全案.md
  - 设计文档/GDD/GDD_03_人偶实体对象与好感双轨机制.md
  - 设计文档/GDD/GDD_09_标签与特质系统.md
  - 设计文档/规则卡/06_标签与特质规则卡.md
  - 设计文档/规则卡/08_人偶核心状态与好感双轨规则卡.md
  - 设计文档/config/audits/36_局外成长正式配置承接审计.md
  - 设计文档/config/designs/37_局外成长正式配置落地设计.md
  - 设计文档/config/tasks/38_局外成长正式配置实现任务拆分.md
  - 设计文档/config/gates/39_局外成长正式配置ID锁定与冲突检查.md
  - 设计文档/config/designs/41_局外成长效果特质正式配置落地设计.md
  - 设计文档/config/designs/42_局外成长义体正式配置落地设计.md
  - 设计文档/config/designs/43_局外成长制造维护正式配置落地设计.md
  - 设计文档/config/designs/44_局外成长人偶特质房间正式配置落地设计.md
  - 设计文档/config/validation/45_局外成长正式配置Validator与固定验收样例.md
last_verified: 2026-05-25
update_rule: 修改配置字段、数据源规则或表间引用时同步本文件。
---

# 效果配置字典说明 (EffectEnums Config)

> `EffectEnums.json` 是底层行为词典。物品效果、义体效果、维护效果、动态标签、特质、房间反馈和部分怪物能力都可以引用这里的效果定义。
>
> 注意：效果字典不直接实例化给玩家。它定义 `EffectID`、参数格式、触发时机、可附着范围和验证规则，供 `Items`、`Prosthetics`、`Dolls`、维护、特质和事件配置引用。

## 正式职责

| 职责 | 说明 |
|---|---|
| 效果词典 | 说明 `EffectID` 如何被程序识别、需要哪些参数、在哪些触发点可用。 |
| 标签定义 | 说明 Tag 的分组、可附着对象、玩家可见性和生命周期。 |
| 动态状态 | 支撑污染、恐惧、适应期、义体疲劳等临时状态。 |
| 特质规则 | 支撑人偶创伤、成长、转化和长期记忆。 |
| 维护 / 房间反馈 | 支撑维护方案、交互反馈和房间纪念物的非数值效果。 |

## 效果字段

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| `EffectID` | string | 是 | 效果全局唯一 ID，必须与程序可识别 ID 或脚本映射一致。 |
| `Name` / `DisplayName` | string | 是 | 策划和 UI 可读名称。 |
| `Type` | string | 是 | 分类，例如 `Buff`、`Debuff`、`Action`、`Modifier`、`StateTag`、`Trait`、`Feedback`。 |
| `TriggerTiming` | string | 是 | 默认生效时机，例如 `Passive`、`OnUse`、`OnTurnStart`、`OnCombatEnd`、`OnDungeonEnter`、`OnMaintenanceComplete`。 |
| `AttachableScope` | string[] | 是 | 可附着对象，例如 `Item`、`Prosthetic`、`DollRuntime`、`Monster`、`Room`、`Order`。 |
| `VisibleToPlayer` | bool | 是 | 是否需要在 UI 中解释给玩家。隐藏效果必须有调试说明。 |
| `Description` | string | 是 | 机制说明。 |
| `ConfigSchema` | object | 是 | 参数配置规范，定义 `Target`、`Level`、`Trigger`、`Resource`、`Operation`、`Params` 的合法值。 |
| `StackPolicy` | string | 否 | 叠加规则，例如 `NoStack`、`StackBySource`、`RefreshDuration`、`AddValue`。 |
| `ClearRule` | string/object | 否 | 清除条件，例如战斗结束、维护后、日程推进、特质转化。 |

## 标签字段

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| `TagID` | string | 是 | 标签唯一 ID。 |
| `TagGroup` | string | 是 | 分组，例如 `ItemRole`、`Risk`、`Emotion`、`Material`、`Faction`、`RoomMemory`。 |
| `AttachableScope` | string[] | 是 | 可附着对象。 |
| `VisibleToPlayer` | bool | 是 | 是否可见。 |
| `DurationType` | string | 否 | `Permanent`、`RunOnly`、`CombatOnly`、`UntilMaintenance`、`UntilEventResolved`。 |
| `StackPolicy` | string | 否 | 叠加规则。 |
| `ClearRule` | string/object | 否 | 清除或转化规则。 |

## 特质字段

| 字段名 | 类型 | 必填 | 说明 |
|---|---|---|---|
| `TraitID` | string | 是 | 特质唯一 ID。 |
| `Polarity` | string | 是 | `Positive`、`Negative`、`Mixed`。 |
| `SourceCondition` | object | 是 | 获得条件，例如战败、SAN 崩溃、Boss 胜利、维护路线。 |
| `Trigger` | string | 是 | 生效触发点。 |
| `Conditions` | object | 否 | 生效条件。 |
| `Effects` | object[] | 是 | 引用效果或状态修改。 |
| `ConflictGroup` | string[] | 否 | 互斥组。 |
| `TransformRule` | object | 是 | 转化、消除或压制路径；负面特质不能没有处理方式。 |
| `FeedbackRefs` | string[] | 否 | UI / 文案 / 房间反馈引用。 |

## 通用 Modifier 效果

`ModifyResourceCost` 用于修改资源成本。配置侧必须声明：

| 字段 | 示例 | 说明 |
|---|---|---|
| `EffectID` | `ModifyResourceCost` | 通用资源成本修正效果。 |
| `Trigger` | `OnDungeonMoveCost` | 触发点。 |
| `Resource` | `SAN` | 被修改的资源。 |
| `Operation` | `AddFlat` | 运算方式，支持 `AddFlat`、`AddPercent`、`Multiply`。 |
| `Params[0]` | `1` | 修正数值。 |

结算顺序固定为 `AddFlat -> AddPercent -> Multiply`，避免配置列表顺序影响最终结果。

## 本轮目标配置

| 类型 | 目标 |
|---|---|
| 义体效果 | 支撑路线预览、背包干涉抵抗、充能爆发、SAN 调节、材料拆解和长线恢复。 |
| 维护效果 | 支撑 HP、SAN、磨损、侵蚀、Broken、义体稳定和下潜许可。 |
| 特质 | 支撑 8 个正式特质：孢雾恐惧、边缘焦虑、废料囤积、稳定双手、奇迹余辉、坚定归还、脆弱信任、工坊慰藉。 |
| 动态标签 | 支撑污染、适应期、调节疲劳、义体稳定、房间记忆等临时状态。 |

## Validator 建议

| ValidatorID | 检查 |
|---|---|
| `CFG-EFFECT-001` | `EffectID`、`Type`、`TriggerTiming`、`ConfigSchema` 必填。 |
| `CFG-EFFECT-002` | `ConfigSchema` 参数数量、类型和默认值必须可被引用配置解释。 |
| `CFG-TAG-001` | `TagID`、`TagGroup`、`AttachableScope`、`DurationType` 值域合法。 |
| `CFG-TRAIT-001` | 特质必须有获得条件、生效规则、可见反馈和转化 / 消除路径。 |
| `CFG-FEEDBACK-001` | 反馈效果必须声明触发条件、收益 / 无收益裁决和表现 fallback。 |

## 完成口径

README 字段口径完成只代表 `GROWTH-README` 的文档侧完成，不代表效果、标签、特质或反馈 JSON 已完成。`设计文档/config/designs/41_局外成长效果特质正式配置落地设计.md` 是 `GROWTH-EFFECTS-TRAITS` 的 JSON 修改前字段级设计，`设计文档/config/designs/44_局外成长人偶特质房间正式配置落地设计.md` 是 `GROWTH-DOLL-TRAIT-ROOM` 的反馈 / 特质引用字段级设计，二者都不代表 JSON 已完成。JSON 修改后必须回写 `agent_status/design.md` 的配置状态和证据入口；如影响长期节点门禁，再通知 PM 更新 `版本规划/09_正式版核心纵切开发路线.md`。
