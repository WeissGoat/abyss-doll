---
id: config_factions_readme
title: 势力配置字段说明 (Factions Config)
type: config
role: 策划
domain: config_factions
status: active
source_of_truth: true
related:
  - 配置表(JSON)/README.md
  - 配置表(JSON)/Orders/README.md
  - 设计文档/07_势力声望与订单规则卡.md
  - 设计文档/24_势力订单声望内容包.md
  - 设计文档/46_经济压力正式配置承接审计.md
  - 设计文档/49_经济压力势力正式配置落地设计.md
  - 设计文档/50_经济压力订单正式配置落地设计.md
  - 设计文档/51_经济压力正式配置Validator与固定验收样例.md
  - 设计文档/52_经济压力正式配置实现任务拆分.md
  - 设计文档/53_经济压力正式配置ID锁定与冲突检查.md
  - 设计文档/54_经济压力正式配置README字段口径检查.md
last_verified: 2026-05-25
update_rule: 修改势力字段、订单槽位、声望阶梯、黑市标记、解锁引用或势力标签时同步本文件。
---

# Factions Config

`Factions/*.json` defines town economy factions used by orders, reputation, trust, and black-market hooks.

正式势力池、5 个势力的字段级设计、正规声望阶梯、黑市信任阶梯、冲突规则、Validator 和固定验收样例见 `设计文档/49_经济压力势力正式配置落地设计.md`。经济压力整体验收规格、跨域 Validator、固定样例和 `DES-V4-001` 回写证据模板见 `设计文档/51_经济压力正式配置Validator与固定验收样例.md`；配置实现派发顺序和证据模板见 `设计文档/52_经济压力正式配置实现任务拆分.md`；当前 ID 锁定与冲突检查见 `设计文档/53_经济压力正式配置ID锁定与冲突检查.md`；README 字段口径检查见 `设计文档/54_经济压力正式配置README字段口径检查.md`。这些文档是 JSON 修改前策划清单、验收规格、任务拆分、ID 证据和字段口径证据，不代表 `Factions/*.json` 已经完成。

## 1. Required fields

- `FactionID`: stable unique id.
- `DisplayName`: player-facing faction name.
- `VisibleOrderSlots`: how many weekly order candidates this faction exposes.
- `MaxActiveOrders`: accepted/in-progress order cap for this faction.

## 2. Formal target fields

以下字段承接 `49_经济压力势力正式配置落地设计.md`，用于后续 `ECON-FACTIONS-JSON`。当前程序未全部消费时，可以先作为正式配置目标落 README 和 Validator warning；不能因此把 JSON 标记为完成。

| 字段名 | 类型 | 目标 | Validator 口径 |
|---|---|---|---|
| `FactionRole` | enum | 标记 `Guild`、`Workshop`、`MageTower`、`AlchemyGuild`、`BlackMarket` 等势力职责。 | 当前开放势力缺角色 warning；黑市缺角色 error。 |
| `FactionTags` | string[] | 用于传闻、订单、奖励和 UI 过滤。 | 标签为空可 warning；被传闻或订单引用的标签必须存在。 |
| `ReputationRanks` | object[] | 正规势力 Rank 0-5 阈值、奖励、订单池权重和解锁引用。 | 阈值不递增或最低阈值不是 0 时 error。 |
| `TrustRanks` | object[] | 黑市信任阶梯、背叛订单入口、风险上升和特殊渠道解锁。 | `IsBlackMarket=true` 但缺信任阶梯时 error。 |
| `OrderPoolIDs` | string[] | 该势力可刷新的订单池 ID。 | 当前开放势力没有订单池 warning / error。 |
| `UnlockRefs` | string[] | 指向 Rewards、Blueprints、Services、Shops 或 ScenarioEvents 的解锁项。 | 引用缺失时 warning；关键解锁缺失需记录后续归属。 |
| `SuspicionPolicy` | object | 正规势力对黑市背叛、违禁交易和怀疑恢复的响应。 | 黑市路线无长期代价时 error。 |
| `ConflictFactionIDs` | string[] | 与黑市或其他势力的冲突关系，用于 Boss 核心互斥和背叛裁决。 | Boss 核心互斥链缺冲突关系时 error。 |
| `VisualID` | string | 势力徽章或订单板展示资源。 | 美术未接入时 warning，进入美术验收前必须稳定。 |

## 3. Locked IDs and migration

| FactionID | 当前状态 | 后续处理 |
|---|---|---|
| `faction_adventurer_guild` | 未见 JSON 主 ID。 | 新增冒险者公会。 |
| `faction_mechanic_workshop` | 当前 JSON 已占用，且与 `53` 锁定 ID 一致。 | 继续补正式字段、订单池、声望阶梯和解锁引用；不重复创建同 ID。 |
| `faction_mage_tower` | 未见 JSON 主 ID。 | 新增法师塔。 |
| `faction_alchemy_guild` | 未见 JSON 主 ID。 | 新增炼金协会。 |
| `faction_black_market` | 未见 JSON 主 ID。 | 新增黑市；必须包含信任、风险和背叛入口。 |

## 4. Validator rules

正式扩展阶段应覆盖 `CFG-FACTION-*` 和相关 `CFG-ECON-X-*` 检查，具体见 `51_经济压力正式配置Validator与固定验收样例.md`。当前 README 只完成字段口径，不代表 `Factions/*.json` 已完成。
