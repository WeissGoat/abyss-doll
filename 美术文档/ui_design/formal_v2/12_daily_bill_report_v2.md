---
id: art_ui_formal_v2_daily_bill_report
title: Daily Bill Report Formal V2 方案
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v1/daily_bill_report_v1.md
last_verified: 2026-06-01
update_rule: 编写或确认 daily_bill_report Formal V2 详细方案时同步本文件。
---

# Daily Bill Report Formal V2 方案

> **状态：** Formal V2 设计草案。本文用于确认 `daily_bill_report` 的正式化方向；确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`daily_bill_report` Formal V1 已覆盖净收益、收支、未售出和月租压力，但仍像账单表格：

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 数字堆叠 | 收入、支出、明细、压力都用面板显示 | 改为桌面账本，中心只放今日结论和压力轨。 |
| 月租压力情绪不够 | 债务只是警告面板 | 用月租 / 债务进度条和盖章表现压力。 |
| 行动并列 | Continue、Review Sell、Defer Payment 权重接近 | Continue / Close Day 是唯一主行动，延后付款为危险折叠。 |
| 和营业结算重复 | 都显示收入和成交 | 本页只做最终收支、压力和次日决策，不重复营业演出。 |

---

## 2. 玩家目标

玩家进入每日账单时，目标是：

1. 看懂今天最终赚了还是亏了。
2. 知道月租 / 债务压力变得更好还是更差。
3. 注意未售出、高风险物和明天需要处理的事。
4. 结束当天或返回处理关键问题。

---

## 3. Formal V2 体验定位

`daily_bill_report` 是“桌面账本”，不是收支后台。

```text
打开的账本
  -> 今日结论
  -> 收入 / 支出摘要
  -> 月租压力轨
  -> 明日提示
  -> 结束当天
```

视觉目标：

* 像在工坊桌上翻开账本，而不是数据面板。
* 今日净结果和月租压力是主视觉。
* 收支明细只保留摘要，完整列表折叠。
* 气氛温暖但带压力，不能过度硬核。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Header: day end / current money                            │
│                                                            │
│ Open Ledger Center                                         │
│ left page: income summary      right page: expense/rent    │
│                                                            │
│ Pressure Rail / Tomorrow Hint             Close Day        │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `bill_background` | `0,0 1920x1080` | 工坊夜间 / 桌面背景。 |
| `open_ledger` | `360,150 1200x660` | 打开的账本主视觉。 |
| `income_page` | `420,230 500x420` | 总收入、主要收入来源、今日净结果。 |
| `expense_page` | `1000,230 500x420` | 支出、维护费、月租 / 债务。 |
| `pressure_rail` | `430,680 1060x100` | 月租进度、欠债压力、明日风险。 |
| `tomorrow_hint` | `420,820 620x110` | 明日建议、未售出物摘要。 |
| `bill_action_panel` | `1120,810 380x130` | Close Day / Return Workshop / Defer。 |

---

## 5. 信息层级

默认阅读顺序：

1. 今日净收益和当前金币。
2. 月租 / 债务压力是否变化。
3. 收入和支出的最大来源。
4. 未售出或明日风险。
5. 结束当天。

不默认展示：

* 所有交易明细。
* 所有公式。
* 每个未售出物的完整来源。
* 延后付款选项。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `CloseDay` / `ContinueNextDay` | 唯一主行动。 |
| Secondary | 返回工坊处理、查看完整明细 | 小按钮。 |
| Tertiary | 展开交易列表、查看未售出详情 | 折叠详情。 |
| Danger | 延后付款 / 欠债处理 | 默认折叠，二次确认。 |

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| Continue | 保留为 Close Day / Continue Next Day 主行动。 |
| Review Sell | 次行动，只在未售出风险明显时出现。 |
| Defer Payment | 危险折叠，不能和 Continue 同权。 |
| 收支明细列表 | 默认折叠为摘要。 |
| 返回工坊 | 次行动，位置低于主行动。 |

---

## 8. 场景隐喻

每日账单像“夜里收拾工坊时翻开的账本”：

* 桌面上有账本、金币、账单、蜡烛或暖灯。
* 收入页和支出页分开，但不做密集表格。
* 月租压力像进度条、欠条夹或红色印章。
* 未售出物像压在账本边上的小纸条。

---

## 9. 程序迁移影响

确认后建议层级：

```text
DailyBillReportPanel
  BillBackground
  OpenLedger
  IncomePage
  ExpensePage
  PressureRail
  TomorrowHint
  BillActionPanel
```

关键绑定：

| 绑定 | 来源 |
|---|---|
| 今日收支结果 | DailyBillResult。 |
| 当前金币 | Economy snapshot。 |
| 月租 / 债务压力 | RentDebtService / EconomyPressureSnapshot。 |
| 未售出 / 明日提示 | Shop / Inventory / Economy result。 |
| 结束当天 | Time / GameFlow 服务。 |

---

## 10. 素材需求变化

第一版复用：

| VisualID | 用法 |
|---|---|
| `bg_workshop_day` | 可临时作为工坊背景，后续替换夜间桌面。 |
| `ui_panel_main` / `ui_panel_info` | 账本页和摘要区。 |
| `ui_icon_bill` | 账单标题。 |
| `ui_icon_income` / `ui_icon_expense` | 收入和支出。 |
| `ui_icon_debt_rent` | 月租 / 债务压力。 |
| `ui_icon_money` / `ui_icon_warning` | 金币和风险。 |
| `ui_button_primary` / `ui_button_secondary` / `ui_button_danger` | 结束、返回、延后。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `ui_daily_bill_open_ledger` | 打开的账本主视觉。 |
| `ui_daily_bill_pressure_rail` | 月租 / 债务压力轨。 |
| `bg_workshop_night_desk` | 夜间桌面账本背景。 |
| `ui_daily_bill_stamp` | 已结算 / 欠款 / 警告印章。 |

---

## 11. UX 验收标准

运行时截图需要满足：

1. 3 秒内能看出这是日终账本。
2. 今日净收益和月租 / 债务压力是视觉中心。
3. 收入、支出和压力分区清楚，但不变成完整表格。
4. Close Day / Continue Next Day 是唯一主行动。
5. 延后付款是危险动作，不和主行动同权。
6. 营业成交细节不和 `business_settlement` 重复。

---

## 12. 用户确认问题

建议确认以下 3 点：

1. `daily_bill_report` 是否采用“打开账本 + 月租压力轨”的结构。
2. 是否把完整收支列表默认折叠，只保留今日结论和压力。
3. 是否把延后付款降级为危险折叠动作。
