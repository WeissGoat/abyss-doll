---
id: art_ui_formal_v1_business_settlement
title: 营业结算演出界面 Formal V1
type: art
role: 美术
domain: ui_design
status: active
source_of_truth: true
related:
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/13_正式纵切UI与素材覆盖矩阵.md
  - 美术文档/ui_design/versions/migration_log.md
  - 设计文档/04_小镇经济结算与压力链规则卡.md
  - 设计文档/GDD_04_小镇循环与经济物价波浪模型.md
last_verified: 2026-05-25
update_rule: 修改营业结算演出界面正式结构、资源槽位或程序迁移要求时同步本文件。
---

# 营业结算演出界面 Formal V1

> **目标：** 在 `shop_staging` 和 `daily_bill_report` 之间补上“开始营业后的成交反馈阶段”。它负责顾客流、成交爆点、金币增长、未售出 / 黑市风险摘要和进入账单的过渡，不替代最终账单复盘。

---

## 1. Formal V1 结构

```text
Business Settlement
  business_background
  business_stage_card
    business_header
    customer_flow_lane
    revenue_pulse_panel
    risk_and_unsold_panel
    transaction_feed_panel
    business_bottom_hint
    business_action_panel
```

| ZoneID | Rect | 目标 |
|---|---|---|
| `business_background` | `0,0 1920x1080` | 复用工坊背景，表现营业发生在工坊前厅或柜台空间。 |
| `business_stage_card` | `180,90 1560x900` | 营业结算主舞台，承载顾客流、成交反馈、收入脉冲和阶段摘要。 |
| `business_header` | `240,130 1440x110` | 标题、日期、营业结算图标、当前金币和标题分隔。 |
| `customer_flow_lane` | `260,280 580x460` | 顾客流、顾客偏好、成交对象和入店离店节奏。 |
| `revenue_pulse_panel` | `880,280 420x460` | 成交爆点、金币增长和各渠道收入脉冲。 |
| `risk_and_unsold_panel` | `1340,280 320x280` | 未售出物、违禁拒收、黑市风险和隔夜代价摘要。 |
| `transaction_feed_panel` | `1340,590 320x150` | 最近成交记录、顾客偏好命中和收益来源 feed。 |
| `business_bottom_hint` | `260,780 580x140` | 营业阶段说明、跳过提示、下一步账单预告。 |
| `business_action_panel` | `880,780 780x140` | 进入每日账单、跳过 / 加速演出、返回出货分配等动作。 |

当前已写入 active `screen_layouts.json`，状态为 `active_spec / FormalV1`。

---

## 2. 信息层级

| 层级 | 内容 |
|---|---|
| 最高 | 成交反馈、金币增长、进入每日账单主行动。 |
| 次高 | 顾客流、交易 feed、未售出 / 黑市风险摘要。 |
| 辅助 | 跳过演出、返回出货分配、阶段说明。 |

数字、物品名、顾客名、日期、渠道名、收益来源和按钮文案全部由 Unity Text 渲染，不烘焙进图片。

---

## 3. 新增与复用资源

| VisualID | 用途 |
|---|---|
| `bg_workshop_day` | 工坊背景。 |
| `ui_panel_main` / `ui_panel_info` | 主舞台和信息面板。 |
| `ui_button_primary` / `ui_button_secondary` | 进入账单、跳过、返回等动作。 |
| `ui_list_row_normal` | 成交 feed 和顾客摘要行。 |
| `ui_title_divider` | 标题分隔。 |
| `ui_icon_money` | 金币增长和收入提示。 |
| `ui_icon_warning` | 未售出、拒收、黑市风险和隔夜代价提示。 |
| `ui_icon_customer` | 顾客流、顾客偏好和成交对象。 |
| `ui_icon_sale_spark` | 成交爆点、金币增长和收入脉冲。 |
| `ui_icon_business_settlement` | 营业结算阶段标题和流程标识。 |

---

## 4. 程序迁移要求

关键路径：

| 路径 | VisualID / 组件 |
|---|---|
| `BusinessSettlementPanel_Runtime/Background_Image` | `bg_workshop_day` |
| `BusinessSettlementPanel_Runtime/StageCard_Image` | `Panel.Main` / `ui_panel_main` |
| `BusinessSettlementPanel_Runtime/StageCard_Image/HeaderPanel/SettlementIcon_Image` | `Icon.BusinessSettlement` / `ui_icon_business_settlement` |
| `BusinessSettlementPanel_Runtime/StageCard_Image/HeaderPanel/MoneyIcon_Image` | `Icon.Money` / `ui_icon_money` |
| `BusinessSettlementPanel_Runtime/StageCard_Image/CustomerFlowLane/CustomerIcon_Image` | `Icon.Customer` / `ui_icon_customer` |
| `BusinessSettlementPanel_Runtime/StageCard_Image/RevenuePulsePanel/SaleSpark_Image` | `Icon.SaleSpark` / `ui_icon_sale_spark` |
| `BusinessSettlementPanel_Runtime/StageCard_Image/RiskAndUnsoldPanel/WarningIcon_Image` | `Icon.Warning` / `ui_icon_warning` |
| `BusinessSettlementPanel_Runtime/StageCard_Image/TransactionFeedPanel/FeedRow_Template` | `List.Row.Normal` / `ui_list_row_normal` |
| `BusinessSettlementPanel_Runtime/StageCard_Image/ActionPanel/ContinueBill_Button` | `Button.Primary` / `ui_button_primary` |
| `BusinessSettlementPanel_Runtime/StageCard_Image/ActionPanel/Skip_Button` | `Button.Secondary` / `ui_button_secondary` |
| `BusinessSettlementPanel_Runtime/StageCard_Image/ActionPanel/BackToStaging_Button` | `Button.Secondary` / `ui_button_secondary` |

程序侧第一版可以不做动画，直接展示静态成交摘要并允许进入每日账单。UI 只展示经济服务提供的结算结果，不直接修改金币、物品归属、声望、时间或账单状态。

---

## 5. 验收标准

1. 点击 Start Business 后，营业结算演出界面位于 `shop_staging` 和 `daily_bill_report` 之间。
2. 截图中能同时看到顾客流、成交爆点、金币增长区域、风险 / 未售出摘要和进入账单主按钮。
3. 数字、物品名、顾客名、日期和收益来源全部由 Unity Text 渲染，不烘焙到图片。
4. 未售出、违禁拒收或黑市风险必须通过 warning 图标或运行时 tint 表达。
5. 没有动画时也能以静态摘要通过结构验收；后续动效只增强，不改变 ScreenID、VisualID 或区域契约。
