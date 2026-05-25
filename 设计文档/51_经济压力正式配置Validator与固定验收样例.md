---
id: design_51_economic_pressure_validator_acceptance
title: 经济压力正式配置Validator与固定验收样例
type: acceptance_spec
role: 策划
domain: formal_config_acceptance
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
  - 设计文档/52_经济压力正式配置实现任务拆分.md
  - 设计文档/53_经济压力正式配置ID锁定与冲突检查.md
  - 设计文档/54_经济压力正式配置README字段口径检查.md
  - 配置表(JSON)/Economy/README.md
  - 配置表(JSON)/Rumors/README.md
  - 配置表(JSON)/Factions/README.md
  - 配置表(JSON)/Orders/README.md
last_verified: 2026-05-25
update_rule: 调整 ECON-VALIDATION 的 Validator 覆盖、固定验收样例、失败信号、证据模板或台账回写口径时同步本文件。
---

# 经济压力正式配置Validator与固定验收样例

> **定位：** 本文承接 `46_经济压力正式配置承接审计.md` 的 `ECON-VALIDATION`。它是策划侧验收规格，不直接修改 `配置表(JSON)`，不代表经济压力 JSON、程序 Validator 或 UI 表现已经完成。它说明后续 `Economy`、`Rumors`、`Factions`、`Orders` 配置源落地后，应如何用 Validator、固定样例和人工复核证明经济压力链可用。

---

## 1. 使用边界

本文回答四个问题：

1. `47` 到 `50` 的字段级设计真正落到 JSON 后，哪些检查项必须通过。
2. 哪些跨域检查能证明月租、传闻、订单、声望和黑市背叛不会断链。
3. 哪些固定样例能证明经济压力形成“看见目标 -> 下潜取物 -> 回城处理 -> 支付 / 成长 / 接新目标”的循环。
4. 哪些证据可以回写 `agent_status/design.md` 的 `DES-V4-001`，哪些情况仍必须判为未完成；如影响长期节点门禁，再通知 PM 更新 `09`。

本文不定义：

| 不定义项 | 归属 |
|---|---|
| 经济核心、账单、顾客、违禁、NightTax、DeadlockGuard 字段清单 | `47_经济压力核心正式配置落地设计.md` |
| 传闻池、倍率、安全区间、来源追踪和订单权重字段清单 | `48_经济压力传闻正式配置落地设计.md` |
| 势力、声望 / 信任阶梯、订单池引用和黑市怀疑字段清单 | `49_经济压力势力正式配置落地设计.md` |
| 订单池、订单模板、需求表达、奖励和背叛裁决字段清单 | `50_经济压力订单正式配置落地设计.md` |
| Validator 程序入口、报告格式和命令参数 | 开发文档中的 P0 Validator / 自动验收需求 |
| 运行时 UI、美术资源和截图验收 | 美术文档、开发文档和 ArtAcceptance |
| 真实 JSON 修改和配置实现派发顺序 | `52_经济压力正式配置实现任务拆分.md` |
| 当前工作区 ID 锁定证据 | `53_经济压力正式配置ID锁定与冲突检查.md` |

---

## 2. 验收总原则

经济压力配置验收必须同时证明静态引用、运行时语义、跨域闭环和玩家可读性。

| 验收层 | 证明内容 | 不足时的风险 |
|---|---|---|
| 静态 Validator | 字段存在、枚举合法、ID 唯一、跨配置引用不断、数值在安全范围内。 | JSON 能加载，但账单、传闻、订单、势力或奖励运行时断链。 |
| 跨域 Validator | 传闻能追到物品 / 订单来源，订单能追到奖励 / 势力 / 层级，经济保护能覆盖硬保护物。 | 单个配置域都通过，但玩家无法把目标转成下潜路线。 |
| 固定样例 | 同一初始状态下，日结、月租、传闻、订单刷新、黑市背叛和死锁补救能复现。 | 配置随机可用，但不能证明经济目标稳定驱动下一轮下潜。 |
| 人工体验复核 | 玩家能读懂为什么缺钱、去哪拿货、卖给谁、是否背叛、下次下潜目标是什么。 | 工具通过，但体验仍像结算表和字段堆叠。 |

配置实现任务不能只凭“JSON 已修改”判定完成。最低证据应包含：

1. 配置 ID 清单。
2. 引用检查结果。
3. `.\tools\config\Sync-Configs.ps1 -Clean` 结果或未运行原因。
4. Validator 报告或等待程序工具补强的明确缺口。
5. 固定样例摘要或人工验收记录。
6. `agent_status/design.md` 对应策划 / 配置状态更新；如影响长期节点门禁，PM 同步 `09`。

---

## 3. Validator 覆盖矩阵

### 3.1 Economy / Core

| ValidatorID | 覆盖 | 等级 | 检查目标 | 失败信号 |
|---|---|---|---|---|
| `CFG-ECON-001` | `ECON-CORE` | error | `WeekLength`、`MonthLength`、`DebtInterestRate`、`LightDebtThresholdRatio`、`PawnValueMultiplier` 合法。 | 日期推进、月租、欠债或典当倍率出现 0、负值或无法解释的极端值。 |
| `CFG-ECON-002` | `ECON-CORE` | error | `RentCurve.Month` 不重复，`BaseRent > 0`，曲线来源可读。 | 月租重复、倒退或缺来源说明。 |
| `CFG-ECON-003` | `ECON-CORE` | error | `PawnProtectedTags` 覆盖 `OrderBound`、`StoryLocked`、`Memento`、`MainQuest`、`KeyItem`。 | 订单物、主线物、纪念物或情感锚点可被自动典当。 |
| `CFG-ECON-004` | `ECON-CORE` | warning / error | `DailyReportPolicy`、`WeeklyReportPolicy`、`MonthlyBillPolicy` 至少包含收入、支出、未售物、传闻贡献、月租倒计时和缺口提示。 | 玩家无法复盘钱从哪里来、压力从哪里来。 |
| `CFG-ECON-005` | `ECON-CORE` | error | `SellChannelRules` 至少覆盖 `DumpBox`、`ShowcaseGrid`、`OrderSlot`、`PawnPool`；如果存在黑市或违禁规则，必须覆盖 `BlackMarketSlot`。 | 传闻或订单要求的处理渠道不存在。 |
| `CFG-ECON-006` | `ECON-CORE` | warning | `CustomerPool` 至少有普通买方和高价值买方；黑市顾客不能常驻普通渠道。 | 橱窗 / 黑市差异只剩倍率，没有风险和目标感。 |
| `CFG-CONTRA-001` | `ECON-CORE` | error | `ContrabandRules` 的目标标签存在，普通渠道、黑市渠道和 NightTax 裁决不冲突。 | 同一违禁物既被普通渠道合法买走，又触发非法隔夜税。 |
| `CFG-CONTRA-002` | `ECON-CORE` | error | 每个违禁规则必须有 Warning、清理方式和 NightTax 或黑市风险之一。 | 违禁物高收益但无提示、无代价。 |
| `CFG-NIGHT-001` | `ECON-CORE` | error | `NightTaxPolicy` 声明评价容器、严重度区间、结算输出和日报栏目。 | 隔夜惩罚静默发生或无法复盘。 |
| `CFG-DEADLOCK-001` | `ECON-CORE` | error | 前三层阶段至少存在一种可见补救路径：低层回刷、轻债、典当、基础订单或保底传闻。 | 一次月租 / 维护失败后直接不可恢复。 |

### 3.2 Rumors

| ValidatorID | 覆盖 | 等级 | 检查目标 | 失败信号 |
|---|---|---|---|---|
| `CFG-RUMOR-001` | `ECON-RUMORS` | error | `RumorID` 唯一，`RumorType`、`DurationDays`、`Weight` 合法。 | 传闻无法稳定刷新或重复覆盖。 |
| `CFG-RUMOR-002` | `ECON-RUMORS` | error | `TargetQuery` 至少包含 `AnyOf` 或 `AllOf`，目标标签能追到 Items 或 Orders。 | 传闻目标没有任何可获得物或订单承接。 |
| `CFG-RUMOR-003` | `ECON-RUMORS` | warning / error | `PriceMultiplier` 在安全区间内：常规涨价 1.05-1.5，风险涨价可到 1.8，跌价不低于 0.3。 | 传闻一刷就破坏经济曲线。 |
| `CFG-RUMOR-004` | `ECON-RUMORS` | error | `SourceTrace` 至少声明层级、节点、怪物、物品或订单之一。 | 玩家不知道去哪兑现传闻。 |
| `CFG-RUMOR-005` | `ECON-RUMORS` | error | 每个当前开放层级至少有 1 条可兑现传闻。 | 周报给出当前版本无法兑现的目标。 |
| `CFG-RUMOR-006` | `ECON-RUMORS` | error | 黑市传闻必须开启 `BlackMarketSlot`，声明风险输出，并要求二次确认。 | 黑市高价无入口、无确认、无风险。 |
| `CFG-RUMOR-007` | `ECON-RUMORS` | error | `RiskPremium` 必须有 `RiskHintKey`，并引用 NightTax、违禁规则、黑市怀疑或订单风险之一。 | 风险溢价只涨价不承担后果。 |
| `CFG-RUMOR-008` | `ECON-RUMORS` | warning / error | `OrderWeightModifiers` 引用的 FactionID、OrderTag 或订单池存在；迁移期缺引用给 warning。 | 传闻提高不存在订单的刷新权重。 |
| `CFG-RUMOR-009` | `ECON-RUMORS` | error | 同一刷新池中不能出现互斥传闻，例如同一标签同时大涨和大跌。 | 周报同日给出互相抵消或矛盾目标。 |

### 3.3 Factions

| ValidatorID | 覆盖 | 等级 | 检查目标 | 失败信号 |
|---|---|---|---|---|
| `CFG-FACTION-001` | `ECON-FACTIONS` | error | `FactionID` 唯一，`FactionRole`、`VisibleOrderSlots`、`MaxActiveOrders` 合法。 | 势力无法显示或订单槽非法。 |
| `CFG-FACTION-002` | `ECON-FACTIONS` | error | 正规势力必须有 Rank 0-5，阈值递增且最低为 0。 | 声望解锁跳级、倒退或无法进入。 |
| `CFG-FACTION-003` | `ECON-FACTIONS` | error | `IsBlackMarket=true` 的势力必须有 `TrustRanks`、黑市订单池、风险标签和背叛入口。 | 黑市只是一套高价普通商店。 |
| `CFG-FACTION-004` | `ECON-FACTIONS` | warning / error | `OrderPoolIDs` 至少 1 个，且能在 Orders 或订单池设计中追到。 | 势力存在但不能派发目标。 |
| `CFG-FACTION-005` | `ECON-FACTIONS` | warning / error | `UnlockRefs` 引用 Rewards、Shops、Services、Blueprints 或 ScenarioEvents；迁移期缺引用给 warning。 | 声望提升没有玩家可见收益。 |
| `CFG-FACTION-006` | `ECON-FACTIONS` | error | 正规势力必须声明与黑市怀疑或背叛的关系。 | 黑市路线无长期代价。 |
| `CFG-FACTION-007` | `ECON-FACTIONS` | error | 传闻引用的 `FactionID` 必须存在，例如工坊短缺和医疗会需求。 | 周报或订单板引用不存在势力。 |
| `CFG-FACTION-008` | `ECON-FACTIONS` | error | 每个当前开放势力至少有一个可刷新订单池和一个玩家可见解锁方向。 | 开放势力是空壳。 |
| `CFG-FACTION-009` | `ECON-FACTIONS` | error | Boss 核心互斥规则不能允许同一唯一物被多个势力同时完成。 | 一个唯一 Boss 核心被多方重复交付。 |

### 3.4 Orders

| ValidatorID | 覆盖 | 等级 | 检查目标 | 失败信号 |
|---|---|---|---|---|
| `CFG-ORDER-001` | `ECON-ORDERS` | error | `OrderID` 唯一，`FactionID`、`OrderPoolID`、`OrderType` 合法。 | 订单无法归属或刷新。 |
| `CFG-ORDER-002` | `ECON-ORDERS` | error | 订单池引用的候选订单全部存在，fallback 存在且可刷新。 | 周刷新抽到空订单或死循环。 |
| `CFG-ORDER-003` | `ECON-ORDERS` | error | `Requirement` 至少包含 ItemID、ItemQuery、ExploreCondition、TargetItem 或 CaptureRule 之一。 | 订单只有描述，没有可判定目标。 |
| `CFG-ORDER-004` | `ECON-ORDERS` | warning / error | `RewardID`、`UnlockRefs`、目标物、目标节点、目标怪物引用存在；迁移期缺引用给 warning。 | 完成订单后奖励或目标断链。 |
| `CFG-ORDER-005` | `ECON-ORDERS` | error | `DeadlineDays >= 1`，`Weight > 0`，声望 / 信任变化在安全范围。 | 订单立刻过期、永不刷新或声望爆炸。 |
| `CFG-ORDER-006` | `ECON-ORDERS` | error | `LargeCargo` 必须有形状、面积、可旋转性和战败 / 放弃流向。 | 大型委托物无法放入背包或丢失流向不明。 |
| `CFG-ORDER-007` | `ECON-ORDERS` | error | `LiveCapture` 必须有捕获容器、捕获后实例状态和 SAN / 风险提示。 | 活体捕获变成普通掉落物。 |
| `CFG-ORDER-008` | `ECON-ORDERS` | error | `BlackMarketBetrayal` 必须声明原订单裁决、正规势力惩罚、二次确认和黑市奖励。 | 背叛订单只发奖励，不处理原订单和长期代价。 |
| `CFG-ORDER-009` | `ECON-ORDERS` | error | 当前已开放层级每个开放势力至少有 1 条可完成订单。 | 刷出当前版本无法完成的订单。 |
| `CFG-ORDER-010` | `ECON-ORDERS` | error | Boss 核心互斥订单不能同时完成。 | 唯一物被重复结算。 |

---

## 4. 跨域检查

单域 Validator 通过不等于经济压力链通过。以下跨域检查必须纳入 `ECON-VALIDATION`。

| CrossID | 检查目标 | 关联域 | 等级 | 失败信号 |
|---|---|---|---|---|
| `CFG-ECON-X-001` | 每条传闻的目标标签能追到至少一个物品、订单或已开放层级来源。 | Rumors -> Items / Orders / Dungeons | error | 周报目标无法兑现。 |
| `CFG-ECON-X-002` | 传闻倍率能被至少一个出售渠道消费，且渠道有日报或成交反馈。 | Rumors -> Economy | error | 传闻存在但不会影响任何成交。 |
| `CFG-ECON-X-003` | 订单奖励能追到 Rewards 或等价固定奖励字段，且声望 / 信任改变量合法。 | Orders -> Rewards / Factions | warning / error | 完成订单后奖励缺失或声望异常。 |
| `CFG-ECON-X-004` | 势力订单池中的订单不越过玩家当前已开放层级，除非配置为明确预告。 | Factions -> Orders -> Dungeons | error | 第一层阶段刷新第三层或更高层目标。 |
| `CFG-ECON-X-005` | 黑市背叛订单引用的原订单、目标物、正规势力惩罚和黑市奖励都存在。 | Orders -> Factions -> Economy | error | 背叛链只完成一半。 |
| `CFG-ECON-X-006` | `OrderBound`、`StoryLocked`、`Memento`、`MainQuest`、`KeyItem` 物品不能被普通出售、自动典当或制造消耗。 | Economy -> Items / Orders / Crafting | error | 系统自动处理玩家不该失去的物品。 |
| `CFG-ECON-X-007` | DeadlockGuard 补救路径必须至少有一个真实入口：低层传闻、基础订单、典当、轻债或安全下潜。 | Economy -> Rumors / Orders / Dungeons | error | 死锁保护配置存在但没有实际路径。 |
| `CFG-ECON-X-008` | 日报、周报、月账单三者能解释同一周期的收入、支出、目标和风险，不互相矛盾。 | Economy -> Rumors / Orders / Factions | warning / error | 玩家看到的经济信息互相冲突。 |

---

## 5. 固定验收样例组

### 5.1 样例摘要字段

每条固定样例至少输出或人工记录：

| 字段 | 要求 |
|---|---|
| `AcceptanceID` | 稳定 ID，例如 `V-PACK-ECON-DAILY-01`。 |
| `InitialState` | 日期、金币、已通层、已接订单、传闻、势力声望、背包 / 仓库关键物。 |
| `ConfigScope` | 覆盖的配置域和任务 ID。 |
| `ActionPath` | 玩家操作路径。 |
| `ExpectedResult` | 通过条件。 |
| `FailureSignals` | 失败信号。 |
| `Evidence` | Validator、同步、运行时截图、人工复核或等待工具补强说明。 |

### 5.2 核心闭环样例

| AcceptanceID | 覆盖任务 | InitialState | ActionPath | ExpectedResult | FailureSignals |
|---|---|---|---|---|---|
| `V-PACK-ECON-DAILY-01` | `ECON-CORE`、`ECON-RUMORS` | 第一层撤离，带回普通物、源石类高价值物，周报存在源石需求传闻。 | 把普通物放倾倒箱，高价值物放橱窗，结束日结。 | 日报显示两类收入、未售物、传闻倍率贡献、月租倒计时和收益来源。 | 传闻倍率不生效；日报无法解释收入；未售物丢失。 |
| `V-PACK-ECON-ROUTE-01` | `ECON-RUMORS`、`ECON-ORDERS`、`ECON-CORE` | 第二层入口已解锁，周报刷新工坊废料短缺或医疗会需求。 | 查看周报 -> 选择对应路线 -> 带回目标物 -> 出售或交订单。 | 玩家能从传闻追踪到物品来源、路线和收益 / 订单收益。 | 传闻目标没有来源；路线目标不可达；出售和订单都不消费目标。 |
| `V-PACK-ECON-RENT-01` | `ECON-CORE`、`ECON-ORDERS` | 第 28 天，金币不足月租，SafeBox 有普通高价值物、订单绑定物和纪念物。 | 触发月租结算 -> 打开典当建议 -> 玩家确认典当普通物。 | 订单物 / 纪念物不被推荐；典当补足或进入轻债；账单记录可复盘。 | 自动典当硬保护物；没有确认；轻债或补救路径不可见。 |
| `V-PACK-ECON-BLACK-01` | `ECON-CORE`、`ECON-RUMORS`、`ECON-FACTIONS` | 黑市窗口开启，玩家持有违禁物，黑市信任满足最低门槛。 | 放入黑市槽并二次确认。 | 高价成交，记录黑市收入、信任变化、正规势力怀疑或债主风险，不再触发同物 NightTax。 | 黑市无二次确认；普通渠道也能高价出售；风险日志缺失。 |
| `V-PACK-ORDER-REFRESH-01` | `ECON-FACTIONS`、`ECON-ORDERS`、`ECON-RUMORS` | `RunSeed=240524`，第 4 周，玩家已到第二层，机械工坊 Rank 2。 | 刷新势力订单板。 | 固定 seed 下订单可复现，不重复，不越层，至少 1 单当前可完成。 | 订单池空、越层、重复或全不可完成。 |
| `V-PACK-ORDER-BETRAYAL-01` | `ECON-FACTIONS`、`ECON-ORDERS`、`ECON-CORE` | 玩家持有机械工坊绑定 Boss 核心，黑市信任 >= 7，原正规订单进行中。 | 接黑市背叛订单并二次确认交付。 | 原订单转 `Betrayed`，黑市奖励发放，正规势力惩罚和怀疑记录写入日志。 | 原订单仍可完成；黑市只发奖励；正规势力无长期代价。 |

### 5.3 模块级补充样例

| AcceptanceID | 覆盖 | 通过标准 |
|---|---|---|
| `V-ECON-CORE-RENT-01` | 月租 / 轻债 | 第 28 天金币略低于月租 10% 以内时进入轻债，不直接失败。 |
| `V-ECON-CORE-PAWN-01` | 典当保护 | 只推荐普通高价值物，不推荐 `OrderBound`、`Memento`、`MainQuest` 或 `KeyItem`。 |
| `V-ECON-CORE-CONTRA-01` | NightTax | 违禁物隔夜触发可复盘代价，走黑市成交后不重复触发同物 NightTax。 |
| `V-ECON-CORE-DEADLOCK-01` | 死锁补救 | 前三层阶段重度缺钱时至少显示低层回刷、基础订单、典当或轻债之一。 |
| `V-ECON-RUMOR-ORIGIN-01` | 源石需求 | 传闻能改变源石类物品售价，并在日报列出贡献。 |
| `V-ECON-RUMOR-SLIME-01` | 跌价传闻 | 史莱姆掉落跌价清楚展示，玩家可选择囤货、低价处理或转订单 / 制造。 |
| `V-ECON-RUMOR-CORROSION-01` | 腐蚀样本溢价 | 高收益和 NightTax / 黑市风险同时可见。 |
| `V-ECON-RUMOR-FALLBACK-01` | 低层补救 | 低层统购传闻能为缺钱玩家提供回刷理由。 |
| `V-ECON-FACTION-GUILD-01` | 冒险者公会 | 第二层阶段可刷新探索报告或安全区信号订单，且不越层。 |
| `V-ECON-FACTION-BLACK-01` | 黑市信任 | 信任达到 7 后允许背叛订单，并记录正规势力惩罚。 |
| `V-ECON-FACTION-SUSPICION-01` | 黑市怀疑 | 黑市信任高且怀疑未消除时，正规高价值订单权重下降或成本上浮。 |
| `V-ECON-ORDER-LARGE-01` | 大型委托物 | 3x3 炉芯可旋转 / 移动，不能普通出售，交付后发底盘蓝图或等价奖励。 |
| `V-ECON-ORDER-CAPTURE-01` | 活体捕获 | 捕获容器状态更新，交付后发心智催化材料，风险提示可见。 |
| `V-ECON-ORDER-SPORE-01` | 限时腐坏 | 超期后订单失败或样本腐坏，失败原因可见，不吞物。 |
| `V-ECON-ORDER-MUTEX-01` | Boss 核心互斥 | 同一 Boss 核心交给一个势力后，另一个核心订单不可再完成。 |

---

## 6. 批次验收顺序

经济压力配置实现后，建议按以下顺序验收，避免高层样例掩盖底层断链。

| 顺序 | 批次 | 必跑检查 | 通过后才能进入 |
|---:|---|---|---|
| 1 | 静态加载 | `CFG-ECON-*`、`CFG-CONTRA-*`、`CFG-NIGHT-*`、`CFG-DEADLOCK-*`、`CFG-RUMOR-*`、`CFG-FACTION-*`、`CFG-ORDER-*`。 | 跨域检查。 |
| 2 | 跨域引用 | `CFG-ECON-X-*` 全部 error 通过，warning 有记录。 | 固定样例。 |
| 3 | 经济基础样例 | `V-PACK-ECON-DAILY-01`、`V-PACK-ECON-RENT-01`。 | 传闻 / 路线样例。 |
| 4 | 传闻路线样例 | `V-PACK-ECON-ROUTE-01`、`V-ECON-RUMOR-*`。 | 订单 / 势力样例。 |
| 5 | 订单势力样例 | `V-PACK-ORDER-REFRESH-01`、`V-ECON-ORDER-*`、`V-ECON-FACTION-*`。 | 黑市和背叛样例。 |
| 6 | 黑市 / 死锁样例 | `V-PACK-ECON-BLACK-01`、`V-PACK-ORDER-BETRAYAL-01`、`V-ECON-CORE-DEADLOCK-01`。 | `agent_status/design.md` 状态回写；如影响长期节点门禁，PM 同步 `09`。 |

---

## 7. 失败分级

| 等级 | 含义 | 处理 |
|---|---|---|
| error | 配置不能视为正式完成。 | 阻止 `DES-V4-001` 标记 `配置完成`，必须修配置、补引用或在程序表另行补 Validator 实现。 |
| warning | 可运行但有迁移、占位或表现缺口。 | 可进入人工复核，但必须记录原因、责任职能和后续补强项。 |
| info | 只做统计或观察。 | 不阻断，但可以影响后续调参和内容补量。 |

以下情况必须是 error：

1. 订单、势力、传闻或奖励引用不存在且无迁移说明。
2. 黑市背叛没有二次确认、原订单裁决或正规势力惩罚。
3. 订单绑定、剧情锁定、纪念物、主线物或关键物会被普通出售、自动典当或制造消耗。
4. 当前开放层级没有任何可完成订单或可兑现传闻。
5. 前三层阶段可能进入不可恢复经济死锁，且没有可见补救路径。

---

## 8. 证据与回写模板

完成经济压力配置实现或验收后，至少回写以下证据。

### 8.1 配置证据

```text
配置变更：
- Economy: <新增 / 修改文件列表>
- Rumors: <新增 / 修改文件列表>
- Factions: <新增 / 修改文件列表>
- Orders: <新增 / 修改文件列表>
- 关联 Rewards / Items / Dungeons: <如有>

同步结果：
- .\tools\config\Sync-Configs.ps1 -Clean: <通过 / 未运行 + 原因>

Validator：
- error: <数量>
- warning: <数量>
- 覆盖：CFG-ECON / CFG-CONTRA / CFG-NIGHT / CFG-DEADLOCK / CFG-RUMOR / CFG-FACTION / CFG-ORDER / CFG-ECON-X

固定样例：
- V-PACK-ECON-DAILY-01: <通过 / 未跑 + 原因>
- V-PACK-ECON-ROUTE-01: <通过 / 未跑 + 原因>
- V-PACK-ECON-RENT-01: <通过 / 未跑 + 原因>
- V-PACK-ECON-BLACK-01: <通过 / 未跑 + 原因>
- V-PACK-ORDER-REFRESH-01: <通过 / 未跑 + 原因>
- V-PACK-ORDER-BETRAYAL-01: <通过 / 未跑 + 原因>
```

### 8.2 状态回写口径

`DES-V4-001` 的状态只能按证据更新：

| 证据状态 | `agent_status/design.md` 状态建议 | 说明 |
|---|---|---|
| 只有 `46-51` 文档完成，未改 JSON。 | `进行中` | 策划配置设计证据充分，但不是配置源完成。 |
| JSON 已落地，静态 Validator error 为 0，但固定样例未跑完。 | `验收中` | 配置源可进入验收，但不能标记 `配置完成`。 |
| JSON 已落地，Sync 通过，Validator error 为 0，核心固定样例通过，warning 有归属。 | `配置完成` | 仅代表策划 / 配置职能完成，不代表程序 UI / 美术全部完成。 |
| 程序服务、UI / 美术和配置验收全部满足 V4 门禁。 | `已完成` | 需要跨职能证据，不由策划单独标记。 |

---

## 9. 完成判定

`ECON-VALIDATION` 作为策划验收规格完成的判定：

1. `CFG-ECON-*`、`CFG-CONTRA-*`、`CFG-NIGHT-*`、`CFG-DEADLOCK-*`、`CFG-RUMOR-*`、`CFG-FACTION-*`、`CFG-ORDER-*` 覆盖矩阵清晰。
2. 跨域检查 `CFG-ECON-X-*` 能覆盖 Economy / Rumors / Factions / Orders / Rewards / Items / Dungeons 的关键断链风险。
3. 固定样例覆盖日结、路线传闻、月租典当、黑市出售、订单刷新、黑市背叛和死锁补救。
4. error / warning 分级明确，能防止把字段说明、README、修改前设计或局部 JSON 误标为 `配置完成`。
5. `DES-V4-001` 的回写证据模板清晰。

在真实 JSON、程序 Validator 和固定样例执行证据落地前，`46` 到 `53` 都只能作为经济压力的策划 / 配置证据，不代表 `配置完成`。
