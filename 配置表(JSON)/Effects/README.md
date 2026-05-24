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
  - 设计文档/GDD_08_人偶养成子模块全案.md
  - 设计文档/GDD_03_人偶实体对象与好感双轨机制.md
  - 设计文档/GDD_09_标签与特质系统.md
  - 设计文档/06_标签与特质规则卡.md
  - 设计文档/08_人偶核心状态与好感双轨规则卡.md
last_verified: 2026-05-24
update_rule: 修改配置字段、数据源规则或表间引用时同步本文件。
---

# 效果配置字典说明 (EffectEnums Config)

> 位于本目录下的 `EffectEnums.json` 文件是整个游戏“底层行为逻辑”的数据字典大全。
> 所有的物品连结增益、义体被动光环、消耗品效果，甚至是怪物的特技，底层都是由这里的 `EffectID` 驱动的。
>
> **注意：这里的文件并不直接实例化给玩家，而是作为一张“词典”或“说明书”，供策划在填写 Items / Prosthetics 等配置表里的 `Params` 数组时进行参考。它规范了每个 Effect 到底需要几个参数，每个参数的含义是什么。**

## 字段说明表

| 字段名 (Key) | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `EffectID` | string | 效果的全局唯一ID | 必须与 C# 中 `EffectFactory` 实例化的 ID 完全一致 |
| `Name` | string | 效果显示名称 | 方便策划辨认 |
| `Type` | string | 效果分类 | `Buff`(增益), `Debuff`(减益), `Action`(单次动作如回血) |
| `TriggerTiming` | string | 预期生效时机 | `Passive`(被动/连结光环), `OnUse`(触发时), `OnTurnStart`(回合开始), `OnCombatEnd`(战斗结束) |
| `Description` | string | 效果机制的文字描述 | |
| `ConfigSchema`| object | **参数配置规范** | **极其重要！** 明确定义在 `Items` 或 `Prosthetics` 中配置该 Effect 时，`Target` (生效目标), `Level` (等级), 以及 `Params` (浮点数组) 这三个核心字段应该怎么填。 |

## 通用 Modifier 效果

`ModifyResourceCost` 用于“修改某个资源成本”这类规则，不绑定具体系统。配置侧必须声明：

| 字段 | 示例 | 说明 |
| :--- | :--- | :--- |
| `EffectID` | `ModifyResourceCost` | 通用资源成本修正效果 |
| `Trigger` | `OnDungeonMoveCost` | 触发点。当前已接入深渊移动成本 |
| `Resource` | `SAN` | 被修改的资源 |
| `Operation` | `AddFlat` | 运算方式，支持 `AddFlat`、`AddPercent`、`Multiply` |
| `Params[0]` | `1` | 修正数值 |

结算顺序固定为 `AddFlat -> AddPercent -> Multiply`，避免背包物品列表顺序影响最终结果。
