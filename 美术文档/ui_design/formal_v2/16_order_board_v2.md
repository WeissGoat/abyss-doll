---
id: art_ui_formal_v2_order_board
title: Order Board Formal V2 方案
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v1/order_board_v1.md
last_verified: 2026-06-02
update_rule: 编写或确认 order_board Formal V2 详细方案时同步本文件。
---

# Order Board Formal V2 方案

> **状态：** Formal V2 设计草案。本文用于确认 `order_board` 的正式化方向；确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`order_board` Formal V1 已覆盖订单列表、详情、奖励、截止日和动作，但整体仍像业务列表：

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 委托感不足 | 多块面板承载订单字段 | 改为小镇公告板 / 委托墙，纸张和印章表达订单。 |
| 主行动不清 | 接取、提交、放弃、返回并列 | 根据选中订单状态只显示一个主行动。 |
| 风险弱 | 失败、背叛、截止日容易变成小字 | 用红线、倒计时印章和封条表达风险。 |
| 势力关系弱 | 势力图标只是字段 | 委托纸张带势力徽章和声望变化。 |

---

## 2. 玩家目标

玩家进入订单板时，目标是：

1. 找到最值得处理的订单。
2. 看懂目标物、截止日、奖励和失败风险。
3. 接取、提交或放弃当前选中订单。
4. 判断订单与势力声望 / 黑市风险的关系。

---

## 3. Formal V2 体验定位

`order_board` 是“势力委托公告板”，不是订单表格。

```text
公告板
  -> 订单纸张列表
  -> 选中订单详情
  -> 奖励 / 风险印章
  -> 接取 / 提交
```

视觉目标：

* 订单像贴在公告板上的纸张。
* 玩家一次只评估一张选中订单。
* 奖励、截止日和风险用印章 / 标签表达。
* 放弃和背叛类动作必须明显危险化。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Header: order capacity / refresh / faction tabs            │
│                                                            │
│ Notice Board Papers       Selected Contract                │
│ order list                target, deadline, faction        │
│                                                            │
│ Reward Seals              Action Panel                     │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `order_board_background` | `0,0 1920x1080` | 工坊 / 小镇公告板背景。 |
| `notice_board_papers` | `170,150 650x760` | 订单纸张列表。 |
| `selected_contract` | `880,170 600x470` | 目标物、数量、截止日、势力备注。 |
| `reward_seal_panel` | `880,690 600x180` | 金币、声望、材料、解锁奖励。 |
| `order_action_panel` | `1520,230 250x560` | 接取 / 提交 / 放弃 / 返回。 |
| `order_risk_strip` | `170,910 1310x90` | 即将过期、高风险、背叛提示。 |

---

## 5. 信息层级

默认阅读顺序：

1. 哪些订单可提交 / 即将过期。
2. 当前选中订单要什么。
3. 奖励和声望变化是什么。
4. 失败或放弃风险。
5. 主行动。

不默认展开：

* 全部势力背景。
* 完整奖励公式。
* 每行多个动作按钮。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `AcceptOrder` / `SubmitOrder` | 根据订单状态切换，唯一主按钮。 |
| Secondary | 切换订单、返回、前往相关库存 | 列表选择 / 小按钮。 |
| Tertiary | 查看势力详情、查看来源 | 折叠详情。 |
| Danger | 放弃、背叛、黑市违约 | 危险样式和二次确认。 |

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| 列表行动作 | 移除，列表只负责选择。 |
| Accept / Submit | 选中详情区统一主行动。 |
| Abandon | 危险动作，默认低权重或二次确认。 |
| 前往势力 / 商品 | 次行动，不抢主流程。 |

---

## 8. 场景隐喻

订单板像挂满委托纸的公告墙：

* 左侧纸张堆叠，带势力徽章、截止日小章和选中钉子。
* 右侧展开当前合同，像拿到桌上细看。
* 奖励是金币袋、材料、徽章和声望印章。
* 高风险订单有红线、封条或深色边角。

---

## 9. 程序迁移影响

确认后建议层级：

```text
OrderBoardPanel
  OrderBoardBackground
  NoticeBoardPapers
  SelectedContract
  RewardSealPanel
  OrderActionPanel
  OrderRiskStrip
```

关键绑定：

| 绑定 | 来源 |
|---|---|
| 订单列表和状态 | OrderService。 |
| 目标物和需求数量 | Order config / Inventory check。 |
| 奖励和声望变化 | Reward / Faction service preview。 |
| 接取 / 提交 / 放弃 | Order domain service。 |

---

## 10. 素材需求变化

第一版复用：

| VisualID | 用法 |
|---|---|
| `bg_workshop_day` | 公告板背景临时复用。 |
| `ui_panel_main` / `ui_panel_info` | 合同和奖励面板。 |
| `ui_list_row_normal` / `ui_list_row_selected` | 订单纸张临时底板。 |
| `ui_icon_order` / `ui_icon_faction` | 订单与势力。 |
| `ui_icon_deadline` / `ui_icon_warning` | 截止日和风险。 |
| `ui_button_primary` / `ui_button_danger` | 接取 / 提交 / 放弃。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `ui_order_notice_board` | 订单公告板。 |
| `ui_order_contract_paper` | 委托纸张底板。 |
| `ui_order_reward_seal` | 奖励印章。 |
| `ui_order_deadline_stamp` | 截止日印章。 |

---

## 11. UX 验收标准

1. 3 秒内能看出这是势力订单板。
2. 可提交和即将过期订单优先可见。
3. 当前订单目标、奖励、截止日和风险清楚。
4. 列表行不堆动作按钮。
5. 放弃 / 背叛动作危险化。

---

## 12. 概念图提示词

```text
Use case: ui-mockup
Asset type: Formal V2 game UI concept art for order_board
Primary request: A cozy fantasy and light steampunk faction order board interface.
Scene/backdrop: warm workshop or town guild corner with a large wooden notice board, pinned parchment contracts, wax seals, small brass lamps.
Subject: left side pinned order papers list, center-right selected contract sheet with target item icons and deadline stamps, reward seals, one clear accept or submit button, danger abandon action as small red sealed option.
Style: warm hand-painted fantasy UI, parchment, wood, brass, low information density, readable hierarchy without real text.
Composition: 16:9 landscape, notice board as main visual, selected contract prominent.
Avoid: modern task management app, spreadsheet rows, dense buttons, real readable words, logos, watermark.
```
