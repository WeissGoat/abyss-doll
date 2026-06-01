---
id: art_ui_formal_v2_shop_staging
title: Shop Staging Formal V2 方案
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v1/shop_staging_v1.md
last_verified: 2026-06-01
update_rule: 编写或确认 shop_staging Formal V2 详细方案时同步本文件。
---

# Shop Staging Formal V2 方案

> **状态：** Formal V2 设计草案。本文用于确认 `shop_staging` 的正式化方向；确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`shop_staging` Formal V1 已建立仓库、渠道、收益风险和确认动作，但信息密度仍偏高：

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 渠道像四列数据板 | 柜台、订单、黑市、暂存并列 | 改为店面陈列台，普通陈列是主流程，订单 / 黑市是侧抽屉。 |
| 玩家目标被表格淹没 | 收益、风险、截止日、容量同时出现 | 默认只看今日陈列槽和预计收入，选中后看风险。 |
| 黑市风险不够像危险选择 | 黑市是普通 lane | 黑市入口降级，带封条 / 暗格视觉，不和普通营业同权。 |
| 确认营业不够突出 | Start Business 和其他按钮接近 | Start Business 成为唯一主行动。 |

---

## 2. 玩家目标

玩家进入出货分配界面时，目标是：

1. 决定今天店面要摆出哪些东西。
2. 把部分物品交给订单或特殊渠道。
3. 看懂预计收益、未分配物和高风险渠道。
4. 开始营业。

---

## 3. Formal V2 体验定位

`shop_staging` 是“营业前摆货”，不是仓库管理表。

```text
仓库货架
  -> 店面陈列槽
  -> 订单 / 黑市侧抽屉
  -> 今日收益预览
  -> 开始营业
```

视觉目标：

* 视觉中心是柜台陈列槽，而不是数据列。
* 物品像摆在店面或托盘上，形成经营感。
* 玩家默认只需要处理少量展示槽。
* 订单和黑市是可选分流，不抢普通营业主流程。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Header: date / money / shop title                          │
│                                                            │
│ Storage Shelf       Display Counter Slots       Channel Box │
│ inventory items     4-8 staged goods            order/black │
│                                                            │
│ Today Preview / Unassigned Goods           Start Business  │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `shop_background` | `0,0 1920x1080` | 工坊前厅 / 店面背景。 |
| `storage_shelf` | `150,170 430x700` | 可出货仓库，列表只负责选择。 |
| `display_counter` | `620,200 760x520` | 今日店面陈列槽，视觉中心。 |
| `channel_side_box` | `1420,210 330x360` | 订单槽、黑市暗格、暂存入口。 |
| `today_preview_strip` | `620,750 760x130` | 预计收益、未分配、容量。 |
| `shop_staging_action_panel` | `1420,630 330x250` | Start Business、重置、返回。 |

---

## 5. 信息层级

默认阅读顺序：

1. 今日柜台摆了哪些物品。
2. 是否还有未分配物或容量问题。
3. 预计收入和主要风险。
4. 订单 / 黑市可选分流。
5. 开始营业。

不默认展示：

* 所有渠道收益公式。
* 每个物品的完整来源。
* 黑市详细规则。
* 长订单列表。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `StartBusiness` | 唯一主行动。 |
| Core Interaction | 将物品加入 / 移出陈列槽 | 货架选择、陈列槽、后续可支持拖拽。 |
| Secondary | 打开订单槽、打开黑市暗格、重置 | 侧抽屉 / 小按钮。 |
| Danger | 黑市出货、违禁物隔夜 | 二次确认或明确危险视觉。 |

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| 四个渠道 lane | 普通陈列为中心，订单 / 黑市 / 暂存改成侧抽屉。 |
| Confirm | 命名和视觉统一为 Start Business。 |
| Black Market | 降级为危险侧入口，默认不抢主流程。 |
| Reset | 次行动。 |
| 返回 | 低权重。 |

---

## 8. 场景隐喻

出货分配界面像“开店前整理柜台”：

* 中央是柜台或展示桌，物品摆在格子里。
* 左侧是仓库箱或货架。
* 右侧是订单夹、暗格、封条和临时存放箱。
* 开始营业按钮像打开店门或翻转营业牌。

---

## 9. 程序迁移影响

确认后建议层级：

```text
ShopStagingPanel
  ShopBackground
  StorageShelf
  DisplayCounter
  ChannelSideBox
  TodayPreviewStrip
  ShopStagingActionPanel
```

关键绑定：

| 绑定 | 来源 |
|---|---|
| 可出货物品 | Inventory / Sellable query。 |
| 陈列槽状态 | ShopStagingService 暂存快照。 |
| 订单槽 / 黑市槽 | Economy / Order / Faction 规则快照。 |
| 收益和风险预览 | TownEconomyPreviewService。 |
| 开始营业 | Economy service，UI 不直接改金币或物品归属。 |

---

## 10. 素材需求变化

第一版复用：

| VisualID | 用法 |
|---|---|
| `bg_workshop_day` | 店面背景。 |
| `item_*_icon` | 货物图标。 |
| `ui_panel_main` / `ui_panel_info` | 货架、柜台、侧抽屉。 |
| `ui_icon_shop_channel` | 普通陈列渠道。 |
| `ui_icon_order` | 订单槽。 |
| `ui_icon_black_market` | 黑市暗格。 |
| `ui_icon_money` / `ui_icon_warning` | 收益和风险。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `ui_shop_display_counter` | 店面陈列台。 |
| `ui_shop_display_slot` | 出货陈列槽。 |
| `ui_shop_channel_side_box` | 订单 / 黑市侧抽屉。 |
| `ui_shop_open_sign` | 开始营业主行动装饰。 |

---

## 11. UX 验收标准

运行时截图需要满足：

1. 3 秒内能看出这是营业前摆货界面。
2. 中央陈列槽是视觉中心。
3. Start Business 是唯一主行动。
4. 订单和黑市不和普通陈列同权，黑市有危险视觉。
5. 未分配物和预计收益可读，但不形成数据墙。
6. 后续即使加入拖拽，也不破坏货架、陈列台和侧抽屉边界。

---

## 12. 用户确认问题

建议确认以下 3 点：

1. `shop_staging` 是否采用“店面陈列台为中心”的结构。
2. 订单和黑市是否从主 lane 降级为侧抽屉。
3. Start Business 是否作为唯一主行动。
