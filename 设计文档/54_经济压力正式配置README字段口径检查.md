---
id: design_54_economic_pressure_readme_field_check
title: 经济压力正式配置README字段口径检查
type: audit
role: 策划
domain: formal_config_authoring
status: active
source_of_truth: true
related:
  - PROJECT_STATUS.md
  - agent_status/design.md
  - 设计文档/README.md
  - 设计文档/26_正式配置设计与填充推进计划.md
  - 设计文档/46_经济压力正式配置承接审计.md
  - 设计文档/47_经济压力核心正式配置落地设计.md
  - 设计文档/48_经济压力传闻正式配置落地设计.md
  - 设计文档/49_经济压力势力正式配置落地设计.md
  - 设计文档/50_经济压力订单正式配置落地设计.md
  - 设计文档/51_经济压力正式配置Validator与固定验收样例.md
  - 设计文档/52_经济压力正式配置实现任务拆分.md
  - 设计文档/53_经济压力正式配置ID锁定与冲突检查.md
  - 设计文档/55_经济压力订单ID最终锁定表.md
  - 配置表(JSON)/Economy/README.md
  - 配置表(JSON)/Rumors/README.md
  - 配置表(JSON)/Factions/README.md
  - 配置表(JSON)/Orders/README.md
last_verified: 2026-05-25
update_rule: 调整经济压力配置 README 字段口径、正式目标字段、迁移 warning、Validator 口径或 ECON-README-CHECK 证据时同步本文件。
---

# 经济压力正式配置README字段口径检查

> 本文是 `52_经济压力正式配置实现任务拆分.md` 中 `ECON-README-CHECK` 的证据文档。它只检查并补齐 `Economy`、`Rumors`、`Factions`、`Orders` 四个 README 是否足以承接经济压力正式配置字段、ID 锁定和旧样例迁移口径；不直接修改 JSON，也不代表经济压力配置已经完成。

---

## 1. 检查范围

| 配置域 | README | 承接设计 |
|---|---|---|
| `Economy` | `配置表(JSON)/Economy/README.md` | `47_经济压力核心正式配置落地设计.md`、`53_经济压力正式配置ID锁定与冲突检查.md` |
| `Rumors` | `配置表(JSON)/Rumors/README.md` | `48_经济压力传闻正式配置落地设计.md`、`53_经济压力正式配置ID锁定与冲突检查.md` |
| `Factions` | `配置表(JSON)/Factions/README.md` | `49_经济压力势力正式配置落地设计.md`、`53_经济压力正式配置ID锁定与冲突检查.md` |
| `Orders` | `配置表(JSON)/Orders/README.md` | `50_经济压力订单正式配置落地设计.md`、`53_经济压力正式配置ID锁定与冲突检查.md` |

---

## 2. 检查结论

| 配置域 | 检查前问题 | 本轮补充 | 结论 |
|---|---|---|---|
| `Economy` | 只覆盖当前运行时月租、轻债和典当字段；缺日报、周报、月账单、出售渠道、顾客、违禁、NightTax、DeadlockGuard 等正式目标字段。 | 已补正式扩展字段目标、`economy_town_v1` ID 复用口径和 `CFG-ECON / CFG-CONTRA / CFG-NIGHT / CFG-DEADLOCK` Validator 口径。 | README 字段口径可承接 `ECON-CORE-JSON`。 |
| `Rumors` | 只覆盖当前 `TargetTags`、`Channel`、`PriceMultiplier` 和 `BoostedOrderTags`；缺目标查询、来源追踪、渠道倍率、订单权重、风险提示、刷新组和旧样例迁移口径。 | 已补 `TargetQuery`、`SourceTrace`、`SellChannelModifiers`、`OrderWeightModifiers`、`RiskPremium`、`RiskHintKey`、`RefreshGroup`、`FallbackRole`，并记录 8 个正式 RumorID 与 `rumor_mechanical_price_up` 迁移口径。 | README 字段口径可承接 `ECON-RUMORS-JSON`。 |
| `Factions` | 只覆盖最小势力字段；缺势力角色、声望 / 信任阶梯、订单池引用、解锁引用、怀疑 / 背叛关系、冲突势力和 VisualID。 | 已补 `FactionRole`、`FactionTags`、`ReputationRanks`、`TrustRanks`、`OrderPoolIDs`、`UnlockRefs`、`SuspicionPolicy`、`ConflictFactionIDs`、`VisualID`，并记录 5 个正式 FactionID 迁移口径。 | README 字段口径可承接 `ECON-FACTIONS-JSON`。 |
| `Orders` | 只覆盖当前最小订单字段；缺 OrderPool、复杂需求、探索条件、大型委托物、活体捕获、失败 / 背叛、互斥唯一物、解锁和 VisualID。 | 已补 `OrderPoolID`、`OrderTags`、`Requirement.ItemQuery`、`Requirement.ExploreCondition`、`Requirement.TargetItem`、`Requirement.CaptureRule`、`LargeCargo`、`FailurePenalty`、`BetrayalPolicy`、`MutexGroupID`、`UnlockRefs`、`VisualID`，并记录 5 个订单池和旧样例订单迁移口径。 | README 字段口径可承接 `ECON-ORDERS-JSON`。 |

总体结论：`ECON-README-CHECK` 策划证据完成。四个 README 已能作为后续 JSON 实现的字段口径入口，但还没有配置源 JSON 落地、同步结果、Validator 结果或固定验收样例结果。

---

## 3. 迁移期 warning

| warning | 处理方式 |
|---|---|
| 当前程序未消费全部正式扩展字段。 | README 明确这些字段是正式配置目标；程序未消费时只能作为 warning 或等待程序补强，不能删除字段目标。 |
| `rumor_mechanical_price_up` 是旧样例传闻。 | 后续实现时迁移为 `rumor_scrap_workshop_shortage`，或标记为 deprecated / test sample，避免进入正式内容池。 |
| `order_mechanic_scrap_drive` 是旧样例订单。 | 后续实现时迁移为工坊工程池正式订单，或标记为 deprecated / test sample，避免进入正式内容池。 |
| `OrderID` 已有最终逐条清单。 | `55_经济压力订单ID最终锁定表.md` 已锁定 5 个订单池和 19 条订单模板；进入 `ECON-ORDERS-JSON` 时以 `55` 为准。 |
| README 字段口径不等于 JSON 完成。 | `agent_status/design.md` 的经济压力配置仍保持 `进行中`；只有配置源落地并通过校验后才能进入 `配置完成` 判断。 |

---

## 4. 后续执行规则

1. 后续进入 `ECON-CORE-JSON`、`ECON-FACTIONS-JSON`、`ECON-RUMORS-JSON`、`ECON-ORDERS-JSON` 时，先按本文和对应 README 检查字段是否齐全。
2. 如果实现时需要新增字段，先回写对应 README、`52`、本文和 `agent_status/design.md`；如影响长期节点门禁，再通知 PM 更新 `09`，然后再改 JSON。
3. 如果实现时需要删减正式目标字段，必须说明删减原因、影响的 Validator / 固定样例和替代字段。
4. README 中标记为“当前程序未消费”的字段，仍属于正式配置目标；不能因为程序暂未消费就把配置任务视为完成。
5. 旧样例迁移必须在 JSON 实现证据中留下记录。

---

## 5. 回写口径

| 目标 | 回写方式 |
|---|---|
| `26` W3 | 经济压力证据补充为 `46`、`47`、`48`、`49`、`50`、`51`、`52`、`53`、本文和 `55`。 |
| `52` | `ECON-README-CHECK` 增加本文作为交付证据。 |
| `agent_status/design.md` | 最近完成记录 `ECON-README-CHECK` 和订单最终 ID 锁定证据；状态仍为 `进行中`。 |
| `agent_status/design.md` | 最近完成记录 `ECON-README-CHECK` 已完成证据文档。 |
| 经济相关配置 README | 增加本文作为 README 字段口径检查入口。 |

---

## 6. 完成判定

本文完成后，`52` 的 `ECON-README-CHECK` 可视为策划证据完成。`DES-V4-001` 仍保持 `进行中`，不能标记为 `配置完成`，原因如下：

1. 本文只补 README 字段口径，没有修改 `配置表(JSON)`。
2. `ECON-CORE-JSON`、`ECON-FACTIONS-JSON`、`ECON-RUMORS-JSON`、`ECON-ORDERS-JSON` 尚未落地。
3. 旧样例 `rumor_mechanical_price_up`、`order_mechanic_scrap_drive` 的迁移或废弃口径尚未在 JSON 实现证据中落地。
4. 配置同步、Validator、跨域引用检查和固定验收样例尚未形成通过证据。
