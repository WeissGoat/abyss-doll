---
id: config_economy_readme
title: 小镇经济配置字段说明 (Economy Config)
type: config
role: 策划
domain: config_economy
status: active
source_of_truth: true
related:
  - 配置表(JSON)/README.md
  - 设计文档/GDD/GDD_04_小镇循环与经济物价波浪模型.md
  - 设计文档/规则卡/04_小镇经济结算与压力链规则卡.md
  - 设计文档/content_packs/22_小镇经济月租内容包.md
  - 设计文档/config/audits/46_经济压力正式配置承接审计.md
  - 设计文档/config/designs/47_经济压力核心正式配置落地设计.md
  - 设计文档/config/validation/51_经济压力正式配置Validator与固定验收样例.md
  - 设计文档/config/tasks/52_经济压力正式配置实现任务拆分.md
  - 设计文档/config/gates/53_经济压力正式配置ID锁定与冲突检查.md
  - 设计文档/config/gates/54_经济压力正式配置README字段口径检查.md
  - 设计文档/config/designs/49_经济压力势力正式配置落地设计.md
  - 开发文档/04_工坊与养成逻辑(WorkshopSystem).md
last_verified: 2026-05-25
update_rule: 修改小镇经济周期、月租曲线、欠债利息、轻债阈值、典当折扣或保护标签时同步本文件。
---

# 小镇经济配置字段说明 (Economy Config)

> 本目录是小镇经济压力链的配置源。当前程序已接入 `TownEconomyService`，先覆盖日结报告、月租扣款、轻度欠账、典当候选和重度违约裁决。传闻、顾客、订单和声望仍待后续服务接入。

经济压力整体验收规格、跨域 Validator、固定样例和 `DES-V4-001` 回写证据模板见 `设计文档/config/validation/51_经济压力正式配置Validator与固定验收样例.md`；配置实现派发顺序和证据模板见 `设计文档/config/tasks/52_经济压力正式配置实现任务拆分.md`；当前 ID 锁定与冲突检查见 `设计文档/config/gates/53_经济压力正式配置ID锁定与冲突检查.md`；README 字段口径检查见 `设计文档/config/gates/54_经济压力正式配置README字段口径检查.md`。这些文档是策划规格、任务拆分、ID 证据和字段口径证据，不代表 `Economy/*.json` 已经完成。

## 1. 文件规则

* 一个经济配置一个 JSON 文件。
* 文件名建议与 `EconomyConfigID` 一致。
* 默认运行时配置 ID 为 `economy_town_v1`。
* Unity 运行时读取同步后的 `StreamingAssets/Configs/Economy`，不要手写运行时副本。

## 2. EconomyConfig 当前运行时字段

| 字段名 | 类型 | 说明 |
|---|---|---|
| `EconomyConfigID` | string | 配置全局唯一 ID。 |
| `Name` | string | 策划可读名称。 |
| `WeekLength` | int | 周期天数；当前正式口径为 7。 |
| `MonthLength` | int | 月周期天数；当前正式口径为 28。 |
| `RentCurveID` | string | 月租曲线 ID，用于日志和策划定位。 |
| `RentCurve` | array | 月租阶梯。`TownEconomyService` 取不大于当前月份的最高阶梯。 |
| `WorkshopMaintenanceBase` | int | 月账单中的固定工坊维护费。当前首版为 0，后续可随设施解锁提高。 |
| `LicenseFeeBase` | int | 月账单中的固定许可证 / 保护费。当前首版为 0，后续接剧情或势力压力。 |
| `DebtInterestRate` | float | 上月欠债利息。示例：`0.1` 表示 10%。 |
| `LightDebtThresholdRatio` | float | 轻度欠账阈值，按总账单比例判断。缺口小于等于该比例时自动转下月债务。 |
| `PawnValueMultiplier` | float | 典当折扣倍率，必须满足 `0 < value <= 1`。 |
| `PawnProtectedTags` | array | 典当硬保护标签。命中任意标签时不进入典当候选。 |

## 3. 正式扩展字段目标

以下字段承接 `47_经济压力核心正式配置落地设计.md`，用于后续 `ECON-CORE-JSON`。当前程序未全部消费时，可以先作为正式配置目标落 README 和 Validator warning；不能因此把 JSON 标记为完成。

| 字段名 | 类型 | 目标 | Validator 口径 |
|---|---|---|---|
| `DailyReportPolicy` | object | 定义日结报告栏目：出售收入、未售物、传闻倍率、订单收入、NightTax、月租倒计时。 | 缺失时 warning；正式配置完成前应覆盖核心栏目。 |
| `WeeklyReportPolicy` | object | 定义周报栏目：本周传闻、订单刷新、势力变化、下潜目标提示。 | 缺失时 warning；若 Rumors 已落地但无周报入口，升为 error。 |
| `MonthlyBillPolicy` | object | 定义月租账单、维护费、许可证 / 保护费、轻债、重度违约和日报回放。 | 月租存在但账单来源不可解释时 error。 |
| `SellChannelRules` | object[] | 定义 `DumpBox`、`ShowcaseGrid`、`OrderSlot`、`PawnPool`、`BlackMarketSlot` 的准入、价格、保护标签和反馈。 | 当前至少应覆盖普通出售、订单交付和典当；黑市字段落地时必须有 `BlackMarketSlot`。 |
| `CustomerPool` | object[] | 定义普通买方、高价值买方、工坊、医疗、武器藏家、黑市顾客等顾客职责。 | 缺普通买方或高价值买方 warning；黑市顾客常驻普通渠道为 error。 |
| `ContrabandRules` | object[] | 定义违禁标签、普通渠道拒收、黑市溢价、警告文案、清理方式和隔夜风险。 | 违禁物可高价出售但无风险提示时 error。 |
| `NightTaxPolicy` | object | 定义隔夜检查容器、风险严重度、SAN / 侵蚀 / 债务影响和日报复盘。 | 隔夜惩罚静默发生或不可复盘时 error。 |
| `DeadlockGuard` | object | 定义前三层阶段缺钱时的低层回刷、基础订单、典当、轻债或保底传闻补救入口。 | 没有任何真实补救路径时 error。 |
| `EconomyLogKeys` | string[] | 定义日报、周报、月账单和黑市风险的日志 key。 | 可 warning；进入 UI / 表现验收前必须稳定。 |

## 4. ID 与迁移口径

| ID | 当前状态 | 后续处理 |
|---|---|---|
| `economy_town_v1` | 当前 JSON 已占用，且与 `53` 锁定 ID 一致。 | 继续补正式字段、Validator 和验收证据；不重复创建同 ID。 |

## 5. RentCurveStep 字段

| 字段名 | 类型 | 说明 |
|---|---|---|
| `Month` | int | 生效月份。必须为正数，不允许重复。 |
| `BaseRent` | int | 该月份基础月租。必须为正数。 |

## 6. 当前首批配置

| EconomyConfigID | 目标 |
|---|---|
| `economy_town_v1` | 按 28 天月周期提供首版月租压力：第 1 月 1200G，第 2 月 2400G，第 3 月 4200G，第 4 月起 5200G；轻债阈值 10%，典当折扣 25%。 |

## 7. Validator 规则

`ConfigValidator.ValidateEconomyConfigs` 会检查：

* 至少存在一个 Economy 配置。
* `WeekLength`、`MonthLength`、`RentCurve.Month`、`RentCurve.BaseRent` 为正。
* `MonthLength` 不能不合法；非 `WeekLength` 整除时给 warning。
* `DebtInterestRate >= 0`。
* `0 <= LightDebtThresholdRatio <= 1`。
* `0 < PawnValueMultiplier <= 1`。
* `RentCurve.Month` 不重复。

正式扩展阶段还应补充 `CFG-ECON-*`、`CFG-CONTRA-*`、`CFG-NIGHT-*`、`CFG-DEADLOCK-*` 检查，具体见 `51_经济压力正式配置Validator与固定验收样例.md`。

## 8. 程序边界

* `TownEconomyService` 是领域服务，不依赖 UI。
* 每日营业由 `SettleDailyBusiness` 产出 `DailyEconomyReport`。
* 月租由 `ResolveMonthlyRent` 产出 `MonthlyRentSettlementReport`。
* 缺口小于轻债阈值时转入 `PlayerProfile.EconomyDebtAmount`。
* 缺口较大且存在可典当物时只返回候选，不自动典当，必须由玩家或 UI 传入选中的典当物。
* 订单绑定物、剧情锁定物、人偶情感锚点和主线关键物通过 `PawnProtectedTags` 或绑定状态保护，不进入典当候选。
