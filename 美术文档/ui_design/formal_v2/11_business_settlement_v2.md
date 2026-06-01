---
id: art_ui_formal_v2_business_settlement
title: Business Settlement Formal V2 方案
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v1/business_settlement_v1.md
last_verified: 2026-06-01
update_rule: 编写或确认 business_settlement Formal V2 详细方案时同步本文件。
---

# Business Settlement Formal V2 方案

> **状态：** Formal V2 设计草案。本文用于确认 `business_settlement` 的正式化方向；确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`business_settlement` Formal V1 已补上营业反馈阶段，但结构仍偏结果面板：

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 营业感不足 | 顾客流、收入、风险都是面板 | 改成工坊前厅营业小舞台。 |
| 信息像账单提前展示 | 收入、feed、风险同时密集出现 | 只保留成交爆点、顾客流和未售出摘要，详细账目留给账单。 |
| 跳过 / 返回 / 进入账单并列 | 阶段目标不够明确 | `ContinueToBill` 是唯一主行动，跳过只是演出控制。 |
| 动画和静态结构边界不清 | 第一版容易为了动效拖慢接入 | V2 明确静态摘要也可成立，动效后续增强。 |

---

## 2. 玩家目标

玩家进入营业结算界面时，目标是：

1. 感受到今天店面营业发生了。
2. 看见主要成交、金币增长和顾客反馈。
3. 注意未售出或黑市风险。
4. 进入每日账单看完整收支。

---

## 3. Formal V2 体验定位

`business_settlement` 是“营业小演出”，不是最终账单。

```text
店面营业舞台
  -> 顾客流
  -> 成交爆点
  -> 金币增长
  -> 未售出 / 风险摘要
  -> 进入每日账单
```

视觉目标：

* 截图里应像工坊前厅正在收摊。
* 成交反馈和金币增长是视觉中心。
* 交易 feed 最多 3 条，避免变成表格。
* 详细收入 / 支出在 `daily_bill_report` 处理。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Header: business day / short result                        │
│                                                            │
│ Customer Lane          Revenue Moment       Unsold Tray     │
│ silhouettes/cards      sale spark/money     risk summary    │
│                                                            │
│ Short Feed                                  Continue Bill   │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `business_background` | `0,0 1920x1080` | 工坊前厅 / 柜台营业背景。 |
| `customer_lane` | `220,220 520x440` | 顾客剪影、偏好、成交对象。 |
| `revenue_moment` | `780,210 500x460` | 成交爆点、金币增长、渠道收入摘要。 |
| `unsold_risk_tray` | `1320,220 360x330` | 未售出、拒收、黑市风险摘要。 |
| `short_transaction_feed` | `260,700 820x160` | 最近 2-3 条成交。 |
| `business_next_action` | `1180,690 500x180` | 进入每日账单、演出加速 / 跳过。 |

---

## 5. 信息层级

默认阅读顺序：

1. 今天营业是否成功。
2. 最大成交或金币增长。
3. 顾客流和成交反馈。
4. 未售出 / 黑市风险。
5. 进入每日账单。

不默认展示：

* 完整收支明细。
* 所有顾客记录。
* 所有渠道收益。
* 复杂价格公式。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `ContinueToDailyBill` | 唯一主行动。 |
| Secondary | 跳过 / 加速演出、返回出货分配 | 小按钮，弱化。 |
| Tertiary | 查看完整成交 feed | 展开详情。 |
| Danger | 黑市失败处理 | 只在摘要中提示，进入账单或市场处理。 |

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| Continue Bill | 保留为唯一主行动。 |
| Skip | 演出控制，不和继续账单同权。 |
| Back to Staging | 只在营业未确认或测试状态显示，正式流程弱化。 |
| 交易 feed 操作 | 默认不可逐行操作。 |
| 风险处理 | 不在本页直接执行，转入账单或市场。 |

---

## 8. 场景隐喻

营业结算像“工坊前厅收摊时的一瞬间”：

* 柜台上有钱袋、收据、售出空位和未售出物。
* 顾客以剪影、手牌、便签或小头像表现。
* 成交爆点可以是金币跳动、暖光、印章或小火花。
* 未售出 / 黑市风险像被放在边上的封条盒。

---

## 9. 程序迁移影响

确认后建议层级：

```text
BusinessSettlementPanel
  BusinessBackground
  CustomerLane
  RevenueMoment
  UnsoldRiskTray
  ShortTransactionFeed
  BusinessNextAction
```

关键绑定：

| 绑定 | 来源 |
|---|---|
| 顾客流 / 成交对象 | BusinessSettlementResult。 |
| 金币增长 | Economy settlement snapshot。 |
| 未售出 / 黑市风险 | Shop staging / Economy result。 |
| 进入账单 | GameFlow。 |

---

## 10. 素材需求变化

第一版复用：

| VisualID | 用法 |
|---|---|
| `bg_workshop_day` | 前厅营业背景。 |
| `ui_icon_customer` | 顾客流。 |
| `ui_icon_sale_spark` | 成交爆点。 |
| `ui_icon_business_settlement` | 阶段标识。 |
| `ui_icon_money` / `ui_icon_warning` | 收益和风险。 |
| `ui_panel_main` / `ui_panel_info` | 成交区和摘要区。 |
| `ui_button_primary` / `ui_button_secondary` | 进入账单和演出控制。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `ui_business_counter_stage` | 营业柜台舞台。 |
| `ui_business_revenue_burst` | 金币增长爆点装饰。 |
| `ui_business_unsold_tray` | 未售出托盘。 |

---

## 11. UX 验收标准

运行时截图需要满足：

1. 3 秒内能看出这是营业反馈阶段。
2. 成交爆点和金币增长是视觉中心。
3. 顾客流、未售出风险和短 feed 都存在，但不压成表格。
4. Continue To Bill 是唯一主行动。
5. 没有动画时，静态摘要也能表达“营业已发生”。
6. 详细账目不在本页展开。

---

## 12. 用户确认问题

建议确认以下 3 点：

1. `business_settlement` 是否定位为“营业小演出”，而不是完整账单。
2. 是否把交易 feed 限制为短摘要，完整收支交给 `daily_bill_report`。
3. 是否把 Continue To Bill 作为唯一主行动。
