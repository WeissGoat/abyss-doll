---
id: art_ui_formal_v2_sell_panel
title: Sell Panel Formal V2 方案
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v1/sell_panel_v1.md
last_verified: 2026-06-01
update_rule: 编写或确认 sell_panel Formal V2 详细方案时同步本文件。
---

# Sell Panel Formal V2 方案

> **状态：** Formal V2 设计草案。本文用于确认 `sell_panel` 的新语义：小镇商店 / 市场交易界面。确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`sell_panel` Formal V1 已能展示可出售物品和单行出售动作，但语义已和后续经营流程冲突。当前修正为：工坊内“卖出 / 出货”由 `shop_staging` 的出货分配承接，`sell_panel` 改作小镇商店 / 市场交易界面。

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 和 `shop_staging` 边界冲突 | 两者都像卖东西 | `shop_staging` 负责工坊出货分配，`sell_panel` 负责小镇商店买卖。 |
| 商店感不足 | 物品行 + Sell 按钮像管理后台 | 改为小镇店铺柜台：左商店货架、右玩家货物、中间交易详情。 |
| 买入 / 卖出混杂风险 | 旧面板只考虑卖出 | 明确为 Buy / Sell 双标签，但默认只突出当前选中交易。 |
| 价格反馈不直观 | 玩家要逐行读价值 | 交易详情集中显示价格、库存、关系折扣和风险标记。 |

---

## 2. 玩家目标

玩家进入小镇商店界面时，目标是：

1. 浏览小镇商店当前出售的商品。
2. 买入补给、材料或特殊货物。
3. 必要时把玩家货物卖给商店换钱。
4. 看懂当前价格是否受声望、传闻、供需或黑市风险影响。

---

## 3. Formal V2 体验定位

`sell_panel` 是“小镇商店柜台”，不是工坊出货界面。

```text
小镇商店
  -> Buy / Sell 标签
  -> 商店货架或玩家货物
  -> 交易详情
  -> 价格反馈
  -> 买入 / 卖出确认
```

视觉目标：

* 第一眼像小镇店铺或市场柜台，而不是工坊内的出货台。
* 左侧是商店货架，右侧是玩家货物或背包摘要，中间是交易详情。
* 默认只突出当前选中交易，买入和卖出不同时抢主行动。
* 声望折扣、行情涨跌、违禁风险以少量图标和短文本表达。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Header: town shop title / money / close                    │
│                                                            │
│ Shop Shelf             Trade Counter        Player Goods    │
│ buy/sell tabs          selected trade       owned goods     │
│                                                            │
│ Price Feedback         Stock / Risk         Primary Trade   │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `town_shop_background` | `0,0 1920x1080` | 小镇商店 / 市场柜台背景，和工坊空间区分。 |
| `shop_shelf` | `150,170 520x700` | 商店商品，Buy 标签下显示。 |
| `trade_counter` | `720,210 520x500` | 当前选中交易、价格、折扣、风险。 |
| `player_goods` | `1280,170 490x700` | 玩家货物，Sell 标签下显示。 |
| `price_feedback_strip` | `720,740 520x120` | 行情、声望折扣、传闻影响。 |
| `shop_action_panel` | `1280,820 490x120` | Buy Selected / Sell Selected、返回。 |

---

## 5. 信息层级

默认阅读顺序：

1. 当前处于 Buy 还是 Sell。
2. 当前选中交易是什么。
3. 价格、库存、折扣和风险。
4. 执行后金币和库存如何变化。
5. 其他商品或玩家货物。

不默认展开：

* 全部来源字段。
* 完整价格公式。
* Sell All / Buy All。
* 每行买入 / 卖出按钮。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `BuySelected` / `SellSelected` | 根据当前标签切换，一个主按钮。 |
| Secondary | 切换 Buy / Sell、选择商品、返回 | 标签 / 列表选择 / 小按钮。 |
| Tertiary | 筛选、排序、查看来源 | 图标工具。 |
| Danger | Sell All / Buy All / 出售绑定物 / 黑市交易 | 默认隐藏或二次确认。 |

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| 单行 Sell | 移到交易详情区，列表行不放出售按钮。 |
| 单行 Buy | 移到交易详情区，列表行不放购买按钮。 |
| Sell All / Buy All | 危险动作，默认折叠或二次确认。 |
| Close | 次行动。 |
| 物品详情 | 选中后在托盘 / 估价牌展示。 |
| 来源字段 | 只在悬停或详情中展开。 |

---

## 8. 场景隐喻

`sell_panel` 像小镇里的商店或市场柜台：

* 左侧是商店货架、补给箱或店主柜台。
* 中央是交易柜台和打开的小账本。
* 右侧是玩家携带货物 / 背包摘要。
* 行情涨跌、声望折扣和黑市风险像贴在柜台上的小签。
* 不使用工坊出货台视觉，避免和 `shop_staging` 混淆。

---

## 9. 程序迁移影响

确认后建议层级：

```text
SellPanel
  TownShopBackground
  ShopShelf
  TradeCounter
  PlayerGoods
  PriceFeedbackStrip
  ShopActionPanel
```

关键绑定：

| 绑定 | 来源 |
|---|---|
| 商店商品 | ShopInventory / TownMarket 配置或运行时快照。 |
| 玩家可交易货物 | Inventory / SellableItemQuery。 |
| 价格 | EconomyPricingService / Rumor price modifiers / faction discount。 |
| 绑定 / 违禁 / 高风险 | Item lifecycle / Order / Economy 标记。 |
| 买入 / 卖出执行 | Economy / Inventory / Shop 领域服务。 |

---

## 10. 素材需求变化

第一版复用：

| VisualID | 用法 |
|---|---|
| `bg_town_shop` / `bg_workshop_day` | 小镇商店背景；临时阶段可复用工坊背景，但正式图应区分。 |
| `item_*_icon` | 物品候选。 |
| `ui_panel_main` / `ui_panel_info` | 商店货架、交易柜台、价格反馈。 |
| `ui_icon_money` | 价值提示。 |
| `ui_icon_warning` / `ui_icon_locked` | 风险和绑定标记。 |
| `ui_button_primary` / `ui_button_secondary` / `ui_button_danger` | 买入 / 卖出、返回、危险动作。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `bg_town_shop` | 小镇商店 / 市场柜台背景。 |
| `ui_shop_trade_counter` | 交易柜台。 |
| `ui_shop_price_tag` | 价格 / 折扣 / 行情标记。 |
| `ui_shop_risk_tag` | 风险小签。 |

---

## 10.1 概念图状态

旧 `sell_panel_formal_v2_concept.png` 已归档到 `concepts/archive/2026-06-01_workshop_studio_and_shop_semantics/`。归档原因：画面表达的是工坊出售 / 估价柜台，但当前语义已改为小镇商店 / 市场交易。

后续重新出图时，提示词必须明确：

* cozy fantasy town shop interior
* small market counter, shop shelves, merchant stall feeling
* buy and sell trade interface
* not workshop shipping, not warehouse distribution, not appraisal-only counter

---

## 11. UX 验收标准

运行时截图需要满足：

1. 3 秒内能看出这是小镇商店 / 市场交易界面。
2. Buy / Sell 当前标签清楚，不混成出货分配。
3. 当前选中交易和价格反馈是视觉中心。
4. 列表行不堆买入 / 卖出按钮。
5. Buy Selected 或 Sell Selected 是唯一主行动。
6. Sell All / Buy All 或黑市交易必须是危险动作。
7. `sell_panel` 和 `shop_staging` 的功能边界清楚：前者小镇商店买卖，后者工坊营业前出货分配。

---

## 12. 用户确认问题

建议确认以下 3 点：

1. `sell_panel` 是否定位为“小镇商店 / 市场交易”。
2. 是否采用 Buy / Sell 双标签，但默认只突出当前选中交易。
3. Sell All / Buy All 是否默认折叠并二次确认。
