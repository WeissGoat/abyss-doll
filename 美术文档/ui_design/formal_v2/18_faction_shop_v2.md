---
id: art_ui_formal_v2_faction_shop
title: Faction Shop Formal V2 方案
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v1/faction_shop_v1.md
last_verified: 2026-06-02
update_rule: 编写或确认 faction_shop Formal V2 详细方案时同步本文件。
---

# Faction Shop Formal V2 方案

> **状态：** Formal V2 设计草案。本文用于确认 `faction_shop` 的正式化方向；确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`faction_shop` Formal V1 已覆盖势力列表、声望轨、商品列表、详情和黑市风险，但仍像多页商店表：

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 势力存在感弱 | 势力只是左侧列表 | 改为小镇势力柜台，当前势力代表和徽记占据视觉中心。 |
| 声望轨像数据条 | 解锁 / 信任难形成情绪 | 用印章册、契约章和柜台态度表现关系。 |
| 黑市风险不够独立 | 黑市入口和普通势力列表同级 | 黑市做成暗门 / 侧帘 / 封条柜台，明显区别正规商店。 |
| 商品详情像普通卡 | 买卖缺少交易场景 | 商品放在柜台托盘上，价格和条件围绕托盘展示。 |

---

## 2. 玩家目标

玩家进入势力商店时，目标是：

1. 选择要拜访的势力或黑市。
2. 看懂当前声望、信任、解锁和风险。
3. 浏览商品、图纸、服务或情报。
4. 执行购买 / 兑换，或前往订单提高关系。

---

## 3. Formal V2 体验定位

`faction_shop` 是“势力柜台交易”，不是商品表。

```text
势力柜台
  -> 势力选择
  -> 声望 / 信任册
  -> 商品陈列
  -> 选中商品托盘
  -> 购买 / 兑换
```

视觉目标：

* 当前势力有明确的柜台、徽记和气质。
* 商品是陈列物，不是纯文本行。
* 黑市风险在空间上被隔开。
* 购买主行动唯一，风险动作二次确认。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Header: faction name / reputation / close                  │
│                                                            │
│ Faction Counter       Goods Shelf        Selected Goods     │
│ emblem, relation      shop items         price, condition   │
│                                                            │
│ Reputation Book       Black Market Risk  Purchase Action    │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `faction_shop_background` | `0,0 1920x1080` | 小镇势力柜台 / 商铺背景。 |
| `faction_counter` | `120,150 460x520` | 当前势力徽记、代表、关系气质。 |
| `reputation_book` | `120,700 460x190` | 声望 / 信任轨、下一解锁。 |
| `goods_shelf` | `630,160 560x700` | 商品、图纸、服务和情报列表。 |
| `selected_goods_tray` | `1230,180 500x430` | 选中商品大图、条件、价格、库存。 |
| `black_market_risk` | `1230,640 500x130` | 黑市怀疑、背叛风险、封条。 |
| `faction_shop_action_panel` | `1230,810 500x120` | Buy / Exchange / Close。 |

---

## 5. 信息层级

默认阅读顺序：

1. 当前势力是谁。
2. 声望 / 信任是否满足。
3. 当前选中商品是什么。
4. 价格、库存、风险。
5. 购买 / 兑换主行动。

不默认展开：

* 所有势力历史。
* 完整黑市规则。
* 多势力商品对比。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `BuySelectedFactionGoods` / `ExchangeSelected` | 唯一主按钮。 |
| Secondary | 切换势力、切换商品、返回、前往订单 | 列表选择 / 小按钮。 |
| Tertiary | 查看声望详情、查看解锁来源 | 折叠详情。 |
| Danger | 黑市交易、背叛相关兑换 | 危险视觉和二次确认。 |

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| 势力列表 | 可以保留，但表现为柜台标签或徽章。 |
| 商品行购买 | 移到选中商品托盘，列表只选择。 |
| 黑市入口 | 独立暗门 / 侧帘视觉，不和正规势力同权。 |
| 前往订单 | 次行动，服务声望提升。 |

---

## 8. 场景隐喻

势力商店像小镇里不同势力的柜台：

* 正规势力有整洁柜台、徽章、账册和可信灯光。
* 黑市有侧帘、封条、暗格、压低的灯光。
* 商品放在柜台托盘上，条件和价格像标签。
* 声望轨像盖章册，不像经验条。

---

## 9. 程序迁移影响

确认后建议层级：

```text
FactionShopPanel
  FactionShopBackground
  FactionCounter
  ReputationBook
  GoodsShelf
  SelectedGoodsTray
  BlackMarketRisk
  FactionShopActionPanel
```

关键绑定：

| 绑定 | 来源 |
|---|---|
| 势力列表和当前势力 | FactionService。 |
| 声望 / 信任 / 怀疑 | Reputation / Trust runtime state。 |
| 商品列表和条件 | Faction shop config / unlock service。 |
| 购买 / 兑换 | Economy / Inventory / Faction services。 |

---

## 10. 素材需求变化

第一版复用：

| VisualID | 用法 |
|---|---|
| `bg_workshop_day` | 临时商铺背景。 |
| `ui_panel_main` / `ui_panel_info` | 柜台、商品、详情。 |
| `ui_list_row_normal` / `ui_list_row_selected` | 商品和势力列表。 |
| `ui_icon_faction` / `ui_icon_reputation` / `ui_icon_trust` | 势力和关系。 |
| `ui_icon_black_market` / `ui_icon_warning` | 黑市与风险。 |
| `ui_icon_money` | 价格。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `bg_faction_shop_counter` | 势力柜台背景。 |
| `ui_reputation_stamp_book` | 声望盖章册。 |
| `ui_black_market_side_curtain` | 黑市侧帘。 |
| `ui_faction_goods_tray` | 商品托盘。 |

---

## 11. UX 验收标准

1. 3 秒内能看出这是势力商店。
2. 正规势力和黑市视觉明显不同。
3. 当前势力、声望 / 信任、选中商品和价格清楚。
4. 购买 / 兑换是唯一主行动。
5. 黑市风险不可被藏进小字。

---

## 12. 概念图提示词

```text
Use case: ui-mockup
Asset type: Formal V2 game UI concept art for faction_shop
Primary request: A cozy Japanese anime subterranean fantasy faction shop interface in a town market.
Scene/backdrop: small town faction counter with wooden shelves, faction emblem, stamp book, crystal lamps, a shadowed black market side curtain.
Subject: left faction counter and reputation stamp book, center goods shelf, right selected goods tray with price tags and conditions, black market risk strip, one clear purchase button.
Style: soft hand-painted fantasy UI, parchment panels, wood, cloth and emblem details, low information density, clear contrast between official shop and black market, no brass palette, no steampunk machinery.
Composition: 16:9 landscape, faction counter and selected goods tray as main visual.
Avoid: modern e-commerce screen, spreadsheet, sci-fi store, dense text, real readable words, logos, watermark.
```
