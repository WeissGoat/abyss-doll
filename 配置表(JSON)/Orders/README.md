---
id: config_orders_readme
title: 订单配置字段说明 (Orders Config)
type: config
role: 策划
domain: config_orders
status: active
source_of_truth: true
related:
  - 配置表(JSON)/README.md
  - 配置表(JSON)/Factions/README.md
  - 配置表(JSON)/Rewards/README.md
  - 配置表(JSON)/Rumors/README.md
  - 设计文档/规则卡/07_势力声望与订单规则卡.md
  - 设计文档/content_packs/24_势力订单声望内容包.md
  - 设计文档/config/audits/46_经济压力正式配置承接审计.md
  - 设计文档/config/designs/48_经济压力传闻正式配置落地设计.md
  - 设计文档/config/designs/49_经济压力势力正式配置落地设计.md
  - 设计文档/config/designs/50_经济压力订单正式配置落地设计.md
  - 设计文档/config/validation/51_经济压力正式配置Validator与固定验收样例.md
  - 设计文档/config/tasks/52_经济压力正式配置实现任务拆分.md
  - 设计文档/config/gates/53_经济压力正式配置ID锁定与冲突检查.md
  - 设计文档/config/gates/54_经济压力正式配置README字段口径检查.md
  - 设计文档/config/gates/55_经济压力订单ID最终锁定表.md
last_verified: 2026-05-25
update_rule: 修改订单字段、需求表达、奖励、声望 / 信任变化、失败惩罚、刷新权重或黑市背叛口径时同步本文件。
---

# Orders Config

`Orders/*.json` defines weekly town orders. Runtime state is stored as order instances on `PlayerProfile`.

正式订单池、19 条订单模板、需求表达、失败 / 背叛裁决、Validator 和固定验收样例见 `设计文档/config/designs/50_经济压力订单正式配置落地设计.md`；最终 OrderPoolID / OrderID 见 `设计文档/config/gates/55_经济压力订单ID最终锁定表.md`。经济压力整体验收规格、跨域 Validator、固定样例和回写证据模板见 `设计文档/config/validation/51_经济压力正式配置Validator与固定验收样例.md`；配置实现派发顺序和证据模板见 `设计文档/config/tasks/52_经济压力正式配置实现任务拆分.md`；当前 ID 锁定与旧样例迁移口径见 `设计文档/config/gates/53_经济压力正式配置ID锁定与冲突检查.md`；README 字段口径检查见 `设计文档/config/gates/54_经济压力正式配置README字段口径检查.md`。这些文档是 JSON 修改前策划清单、验收规格、任务拆分、ID 证据和字段口径证据，不代表 `Orders/*.json` 已经完成。

## 1. Required fields

- `OrderID`: stable unique id.
- `FactionID`: must reference `Factions/*.json`.
- `OrderType`: enum value from `EconomyOrderType`.
- `Requirement.RequiredCount`: number of matching items to deliver.
- `Weight`: positive value for weekly order selection.

## 2. Current requirement matching

- `RequiredItemIDs` restricts delivery to specific item configs.
- `RequiredTags` requires all listed tags on each delivered item.
- `ForbiddenTags` blocks matching items.
- `MinGridCost` requires item grid cost to be at least the configured value.
- `MinLayer` requires the player's highest unlocked dungeon layer.

## 3. Current rewards

- `FixedGold` is paid directly.
- `RewardID` optionally references `Rewards/*.json`.
- `ReputationDelta` and `TrustDelta` modify faction runtime state on completion.

## 4. Formal target fields

以下字段承接 `50_经济压力订单正式配置落地设计.md`，用于后续 `ECON-ORDERS-JSON`。当前程序未全部消费时，可以先作为正式配置目标落 README 和 Validator warning；不能因此把 JSON 标记为完成。

| 字段名 | 类型 | 目标 | Validator 口径 |
|---|---|---|---|
| `OrderPoolID` | string | 订单所属池，用于势力刷新、传闻加权和 fallback。 | 当前开放订单缺池 warning；订单池落地后缺池 error。 |
| `OrderTags` | string[] | 用于传闻权重、势力偏好、UI 分类和 Validator 查询。 | 被传闻或势力引用的 tag 必须存在。 |
| `DeadlineDays` | int | 订单期限。 | 小于 1 时 error。 |
| `Requirement.ItemQuery` | object | 用标签、层级、大小、风险、唯一物等条件描述交付目标。 | Requirement 无可判定目标时 error。 |
| `Requirement.ExploreCondition` | object | 探索报告、安全区信号、路线节点等非物品目标条件。 | 探索订单缺条件时 error。 |
| `Requirement.TargetItem` | object | Boss 核心、大型委托物、唯一物等目标规则。 | 唯一物未声明互斥时 error。 |
| `Requirement.CaptureRule` | object | 活体捕获、容器、SAN / 风险和交付裁决。 | 活体捕获无容器或实例状态时 error。 |
| `LargeCargo` | object | 大型委托物形状、面积、可旋转性和战败 / 放弃流向。 | 大型物无法放入背包或流向不明时 error。 |
| `FailurePenalty` | object | 超期、放弃、背叛的声望、信任、冷却和日志。 | 失败无后果或无原因时 warning / error。 |
| `BetrayalPolicy` | object | 黑市背叛订单的原订单裁决、正规势力惩罚、二次确认和黑市奖励。 | 背叛只发奖励不处理原订单时 error。 |
| `MutexGroupID` | string | Boss 核心或唯一委托物互斥组。 | 唯一物可重复交付时 error。 |
| `UnlockRefs` | string[] | 完成订单后的图纸、服务、势力、事件或房间记忆解锁。 | 引用不存在时 warning；关键奖励缺失时 error。 |
| `VisualID` | string | 订单图标或订单板展示资源。 | 美术未接入时 warning，进入美术验收前必须稳定。 |

## 5. Locked IDs and migration

| ID 类型 | 当前状态 | 后续处理 |
|---|---|---|
| `pool_guild_exploration` | 未见 JSON 主 ID。 | 新增公会探索订单池。 |
| `pool_workshop_engineering` | 未见 JSON 主 ID。 | 新增工坊工程订单池，可承接旧样例订单意图。 |
| `pool_mage_relic` | 未见 JSON 主 ID。 | 新增法师塔遗物订单池。 |
| `pool_alchemy_purification` | 未见 JSON 主 ID。 | 新增炼金净化订单池。 |
| `pool_black_market_betrayal` | 未见 JSON 主 ID。 | 新增黑市背叛订单池。 |
| `order_mechanic_scrap_drive` | 当前旧样例 JSON 已占用。 | 实现时迁移为工坊工程池正式订单，或标记为 deprecated / test sample。 |

正式 `OrderID` 已在 `设计文档/config/gates/55_经济压力订单ID最终锁定表.md` 锁定为 19 条订单模板。后续配置实现以 `55` 为准；如调整订单 ID，必须同步 `50`、`52`、`53`、`54`、`55` 和本 README。

## 6. Validator rules

正式扩展阶段应覆盖 `CFG-ORDER-*` 和相关 `CFG-ECON-X-*` 检查，具体见 `51_经济压力正式配置Validator与固定验收样例.md`。当前 README 只完成字段口径，不代表 `Orders/*.json` 已完成。
