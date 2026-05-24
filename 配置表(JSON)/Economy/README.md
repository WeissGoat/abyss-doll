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
  - 设计文档/GDD_04_小镇循环与经济物价波浪模型.md
  - 设计文档/04_小镇经济结算与压力链规则卡.md
  - 设计文档/22_小镇经济月租内容包.md
  - 开发文档/04_工坊与养成逻辑(WorkshopSystem).md
  - 版本规划/12_正式版长期版本节点规划.md
last_verified: 2026-05-25
update_rule: 修改小镇经济周期、月租曲线、欠债利息、轻债阈值、典当折扣或保护标签时同步本文件。
---

# 小镇经济配置字段说明 (Economy Config)

> 本目录是小镇经济压力链的配置源。当前程序已接入 `TownEconomyService`，先覆盖日结报告、月租扣款、轻度欠账、典当候选和重度违约裁决。传闻、顾客、订单和声望仍待后续服务接入。

## 1. 文件规则

* 一个经济配置一个 JSON 文件。
* 文件名建议与 `EconomyConfigID` 一致。
* 默认运行时配置 ID 为 `economy_town_v1`。
* Unity 运行时读取同步后的 `StreamingAssets/Configs/Economy`，不要手写运行时副本。

## 2. EconomyConfig 字段

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

## 3. RentCurveStep 字段

| 字段名 | 类型 | 说明 |
|---|---|---|
| `Month` | int | 生效月份。必须为正数，不允许重复。 |
| `BaseRent` | int | 该月份基础月租。必须为正数。 |

## 4. 当前首批配置

| EconomyConfigID | 目标 |
|---|---|
| `economy_town_v1` | 按 28 天月周期提供首版月租压力：第 1 月 1200G，第 2 月 2400G，第 3 月 4200G，第 4 月起 5200G；轻债阈值 10%，典当折扣 25%。 |

## 5. Validator 规则

`ConfigValidator.ValidateEconomyConfigs` 会检查：

* 至少存在一个 Economy 配置。
* `WeekLength`、`MonthLength`、`RentCurve.Month`、`RentCurve.BaseRent` 为正。
* `MonthLength` 不能不合法；非 `WeekLength` 整除时给 warning。
* `DebtInterestRate >= 0`。
* `0 <= LightDebtThresholdRatio <= 1`。
* `0 < PawnValueMultiplier <= 1`。
* `RentCurve.Month` 不重复。

## 6. 程序边界

* `TownEconomyService` 是领域服务，不依赖 UI。
* 每日营业由 `SettleDailyBusiness` 产出 `DailyEconomyReport`。
* 月租由 `ResolveMonthlyRent` 产出 `MonthlyRentSettlementReport`。
* 缺口小于轻债阈值时转入 `PlayerProfile.EconomyDebtAmount`。
* 缺口较大且存在可典当物时只返回候选，不自动典当，必须由玩家或 UI 传入选中的典当物。
* 订单绑定物、剧情锁定物、人偶情感锚点和主线关键物通过 `PawnProtectedTags` 或绑定状态保护，不进入典当候选。
