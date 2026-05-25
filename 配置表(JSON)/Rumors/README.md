---
id: config_rumors_readme
title: 传闻配置字段说明 (Rumors Config)
type: config
role: 策划
domain: config_rumors
status: active
source_of_truth: true
related:
  - 配置表(JSON)/README.md
  - 配置表(JSON)/Items/README.md
  - 配置表(JSON)/Orders/README.md
  - 设计文档/rules/04_小镇经济结算与压力链规则卡.md
  - 设计文档/content_packs/22_小镇经济月租内容包.md
  - 设计文档/config/audits/46_经济压力正式配置承接审计.md
  - 设计文档/config/designs/48_经济压力传闻正式配置落地设计.md
  - 设计文档/config/validation/51_经济压力正式配置Validator与固定验收样例.md
  - 设计文档/config/tasks/52_经济压力正式配置实现任务拆分.md
  - 设计文档/config/gates/53_经济压力正式配置ID锁定与冲突检查.md
  - 设计文档/config/gates/54_经济压力正式配置README字段口径检查.md
last_verified: 2026-05-25
update_rule: 修改传闻字段、目标标签、价格倍率、渠道、持续时间、刷新权重或订单权重加成时同步本文件。
---

# Rumors Config

`Rumors/*.json` defines weekly economy rumors. Current runtime support applies item sell price multipliers and order tag weight boosts.

正式传闻池、8 条传闻的字段级设计、刷新组合规则、来源追踪、Validator 和固定验收样例见 `设计文档/config/designs/48_经济压力传闻正式配置落地设计.md`。经济压力整体验收规格、跨域 Validator、固定样例和 `DES-V4-001` 回写证据模板见 `设计文档/config/validation/51_经济压力正式配置Validator与固定验收样例.md`；配置实现派发顺序和证据模板见 `设计文档/config/tasks/52_经济压力正式配置实现任务拆分.md`；当前 ID 锁定与旧样例迁移口径见 `设计文档/config/gates/53_经济压力正式配置ID锁定与冲突检查.md`；README 字段口径检查见 `设计文档/config/gates/54_经济压力正式配置README字段口径检查.md`。这些文档是 JSON 修改前策划清单、验收规格、任务拆分、ID 证据和字段口径证据，不代表 `Rumors/*.json` 已经完成。

## 1. Required fields

- `RumorID`: stable unique id.
- `RumorType`: enum value from `EconomyRumorType`.
- `Channel`: enum value from `EconomySellChannel`.
- `PriceMultiplier`: multiplier applied to matching item base value.
- `DurationDays`: active duration after weekly refresh.
- `Weight`: positive value for weekly rumor selection.

## 2. Current matching fields

- `TargetTags` applies price changes to items with any matching static or dynamic tag.
- Empty `TargetTags` applies to all items in the configured channel.
- `BoostedOrderTags` doubles effective order selection weight for matching order tags.

## 3. Formal target fields

以下字段承接 `48_经济压力传闻正式配置落地设计.md`，用于后续 `ECON-RUMORS-JSON`。当前程序未全部消费时，可以先作为正式配置目标落 README 和 Validator warning；不能因此把 JSON 标记为完成。

| 字段名 | 类型 | 目标 | Validator 口径 |
|---|---|---|---|
| `TargetQuery` | object | 用 `AnyOf`、`AllOf`、`NoneOf`、`ItemIDs`、`LayerRange` 描述传闻目标。 | 目标查询为空或不可兑现时 error。 |
| `SourceTrace` | object | 声明目标来源：层级、节点、怪物、物品、订单或势力。 | 当前开放版本无法追踪来源时 error。 |
| `SellChannelModifiers` | object[] | 定义传闻对倾倒箱、橱窗、订单槽、黑市槽等渠道的倍率影响。 | 传闻存在但没有任何渠道消费时 error。 |
| `OrderWeightModifiers` | object[] | 定义传闻对 FactionID、OrderPoolID、OrderTag 的刷新权重影响。 | 引用不存在时 warning / error。 |
| `RiskPremium` | object | 定义高价背后的 NightTax、黑市怀疑、订单风险或隔夜风险。 | 风险溢价无风险提示时 error。 |
| `RiskHintKey` | string | UI / 日报展示风险提示的文案 key。 | 风险传闻缺 key 时 warning；黑市 / 违禁传闻缺 key 时 error。 |
| `RefreshGroup` | string | 传闻刷新池分组，用于互斥和保底。 | 同组互斥传闻同时刷新时 error。 |
| `FallbackRole` | enum | 标记 `DeadlockGuard`、`RouteGoal`、`OrderBoost`、`BlackMarketWindow` 等职责。 | 低层补救传闻缺职责时 warning。 |

## 4. Locked IDs and migration

| RumorID | 当前状态 | 后续处理 |
|---|---|---|
| `rumor_origin_stone_demand` | 未见 JSON 主 ID。 | 新增第一层源石需求传闻。 |
| `rumor_slime_crash` | 未见 JSON 主 ID。 | 新增史莱姆素材跌价传闻。 |
| `rumor_scrap_workshop_shortage` | 未见 JSON 主 ID。 | 新增工坊废料短缺传闻，可承接旧样例意图。 |
| `rumor_corrosion_sample_premium` | 未见 JSON 主 ID。 | 新增腐蚀样本溢价传闻。 |
| `rumor_contraband_night_channel` | 未见 JSON 主 ID。 | 新增黑市窗口 / 违禁品传闻。 |
| `rumor_faction_medical_request` | 未见 JSON 主 ID。 | 新增医疗 / 炼金需求传闻。 |
| `rumor_weapon_collector_visit` | 未见 JSON 主 ID。 | 新增武器藏家到访传闻。 |
| `rumor_low_layer_bulk_buy` | 未见 JSON 主 ID。 | 新增低层材料统购传闻。 |
| `rumor_mechanical_price_up` | 当前旧样例 JSON 已占用。 | 实现时迁移为 `rumor_scrap_workshop_shortage`，或标记为 deprecated / test sample。 |

## 5. Validator rules

正式扩展阶段应覆盖 `CFG-RUMOR-*` 和相关 `CFG-ECON-X-*` 检查，具体见 `51_经济压力正式配置Validator与固定验收样例.md`。当前 README 只完成字段口径，不代表 `Rumors/*.json` 已完成。
