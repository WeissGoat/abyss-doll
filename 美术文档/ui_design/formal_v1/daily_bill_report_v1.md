---
id: art_ui_formal_v1_daily_bill_report
title: 每日账单报告界面 Formal V1
type: art
role: 美术
domain: ui_design
status: active
source_of_truth: true
related:
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/13_正式纵切UI与素材覆盖矩阵.md
  - 美术文档/ui_design/versions/migration_log.md
last_verified: 2026-05-24
update_rule: 修改每日账单报告界面正式结构或程序迁移要求时同步本文件。
---

# 每日账单报告界面 Formal V1

> **目标：** 把每日收入支出和长期压力做成正式账单界面。玩家要能看懂今天赚了什么、花了什么、哪些物品滞留，以及欠债或月租风险如何变化。

---

## 1. Formal V1 结构

```text
Daily Bill Report
  bill_background
  bill_card
    bill_header
    bill_summary_panel
    pressure_warning_panel
    unsold_goods_panel
    income_expense_list
    bill_action_panel
```

| ZoneID | Rect | 目标 |
|---|---|---|
| `bill_background` | `0,0 1920x1080` | 复用工坊背景，表现日终收账后的局外空间。 |
| `bill_card` | `340,100 1240x880` | 每日账单主容器。 |
| `bill_header` | `400,140 1120x120` | 标题、日期、账单图标、当前金币。 |
| `bill_summary_panel` | `400,290 520x250` | 净收益、收入、支出、租金进度和维护成本摘要。 |
| `pressure_warning_panel` | `400,570 520x190` | 欠债、即将到期账单和明日压力提示。 |
| `unsold_goods_panel` | `400,790 520x140` | 未售出物、滞留物和违禁品隔夜代价。 |
| `income_expense_list` | `960,290 560x500` | 收入、支出、维护、租金和出售收益明细。 |
| `bill_action_panel` | `960,820 560x110` | 继续下一天、返回工坊处理、延后付款。 |

当前已写入 active `screen_layouts.json`，状态为 `active_spec / FormalV1`。

---

## 2. 信息层级

| 层级 | 内容 |
|---|---|
| 最高 | 今日净收益、当前金币、下一次压力和 Continue 主行动。 |
| 次高 | 收入/支出明细、未售出物和欠债风险。 |
| 辅助 | 日期、租金进度、建议去出售或维护的提示。 |

日期、价格、收入、支出、物品名和风险原因全部由 Unity Text 渲染。

---

## 3. 可复用资源

| VisualID | 用途 |
|---|---|
| `bg_workshop_day` | 工坊背景。 |
| `ui_panel_main` | 账单主容器和行动面板。 |
| `ui_panel_info` | 总结、预警、未售出物和明细面板。 |
| `ui_list_row_normal` | 普通收支明细行。 |
| `ui_list_row_selected` | 高风险未售出物或焦点行。 |
| `ui_button_primary` | Continue。 |
| `ui_button_secondary` | Review Sell / Close。 |
| `ui_button_danger` | Defer Payment。 |
| `ui_icon_bill` | 账单图标。 |
| `ui_icon_income` | 总收入、出售收入、订单收入和当日进账。 |
| `ui_icon_expense` | 总支出、维护费、材料费和经营成本。 |
| `ui_icon_debt_rent` | 月租进度、欠债压力、到期账单和不可支付风险。 |
| `ui_icon_warning` | 普通危险、违禁品或滞留风险。 |
| `ui_icon_money` | 当前金币、余额和价值。 |
| `ui_title_divider` | 标题分隔。 |

---

## 4. 程序迁移要求

关键路径：

| 路径 | VisualID / 组件 |
|---|---|
| `DailyBillReportPanel_Runtime/BillBackground_Image` | `bg_workshop_day` |
| `DailyBillReportPanel_Runtime/BillCard_Image` | `Panel.Main` / `ui_panel_main` |
| `DailyBillReportPanel_Runtime/BillCard_Image/HeaderPanel/BillIcon_Image` | `Icon.Bill` / `ui_icon_bill` |
| `DailyBillReportPanel_Runtime/BillCard_Image/SummaryPanel/IncomeIcon_Image` | `Icon.Income` / `ui_icon_income` |
| `DailyBillReportPanel_Runtime/BillCard_Image/SummaryPanel/ExpenseIcon_Image` | `Icon.Expense` / `ui_icon_expense` |
| `DailyBillReportPanel_Runtime/BillCard_Image/SummaryPanel/DebtRentIcon_Image` | `Icon.DebtRent` / `ui_icon_debt_rent` |
| `DailyBillReportPanel_Runtime/BillCard_Image/PressureWarningPanel/WarningIcon_Image` | `Icon.Warning` / `ui_icon_warning` |
| `DailyBillReportPanel_Runtime/BillCard_Image/PressureWarningPanel/DebtRentIcon_Image` | `Icon.DebtRent` / `ui_icon_debt_rent` |
| `DailyBillReportPanel_Runtime/BillCard_Image/IncomeExpenseListPanel/BillRow_Template` | `List.Row.Normal` / `ui_list_row_normal` |
| `DailyBillReportPanel_Runtime/BillCard_Image/IncomeExpenseListPanel/IncomeIcon_Image` | `Icon.Income` / `ui_icon_income` |
| `DailyBillReportPanel_Runtime/BillCard_Image/IncomeExpenseListPanel/ExpenseIcon_Image` | `Icon.Expense` / `ui_icon_expense` |
| `DailyBillReportPanel_Runtime/BillCard_Image/UnsoldGoodsPanel/UnsoldRow_Template` | `List.Row.Selected` / `ui_list_row_selected` |
| `DailyBillReportPanel_Runtime/BillCard_Image/ActionPanel/Continue_Button` | `Button.Primary` / `ui_button_primary` |
| `DailyBillReportPanel_Runtime/BillCard_Image/ActionPanel/DeferPayment_Button` | `Button.Danger` / `ui_button_danger` |

第一版可以从结算或工坊流程触发，但扣款、欠债、天数推进和压力裁决必须由经济/时间服务处理。

---

## 5. 验收标准

1. 账单界面打开后能一眼看到净收益、总收入、总支出、当前金币和下一次压力。
2. 总收入、总支出和月租/债务压力分别使用独立图标，不再只依赖金币或警告图标。
3. 收入/支出明细至少展示 1 条列表行。
4. 欠债、即将到期、未售出高风险物品使用债务图标、warning 图标或运行时 tint 表达。
5. Continue 是主行动，返回工坊/出售是次行动，延后付款是危险行动。
6. 图片中不包含文字、数字、价格、日期或物品名。
