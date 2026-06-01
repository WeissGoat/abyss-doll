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

> **状态：** Formal V2 设计草案。本文用于确认 `sell_panel` 的正式化方向；确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`sell_panel` Formal V1 已能展示可出售物品和单行出售动作，但仍像临时库存列表：

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 出售缺少柜台感 | 物品行 + Sell 按钮像管理后台 | 改为工坊柜台 / 估价托盘，物品先放入托盘再确认出售。 |
| Sell All 风险过高 | 批量出售容易和单件出售同权 | 批量出售默认隐藏或二次确认，不作为主流程。 |
| 估值不够直观 | 玩家要逐行读价值 | 右侧估价牌集中显示选中物 / 托盘总价。 |
| 和 `shop_staging` 边界模糊 | 都处理出售 | `sell_panel` 只做即时出售 / 清仓，`shop_staging` 做营业前分配。 |

---

## 2. 玩家目标

玩家进入出售界面时，目标是：

1. 快速把不需要的物品换成金币。
2. 看懂当前选择能卖多少钱。
3. 避免误卖关键物品或订单物品。
4. 完成单件或小批量出售后返回工坊。

---

## 3. Formal V2 体验定位

`sell_panel` 是“估价柜台”，不是完整经营界面。

```text
库存货架
  -> 选择物品
  -> 估价托盘
  -> 风险标记
  -> 确认出售
```

视觉目标：

* 中央有明确的柜台托盘，承接当前要卖的物品。
* 列表只负责选择，出售动作集中在右侧。
* 订单绑定、违禁、高价值物以标记提示，不靠长文本。
* 低信息密度，优先服务“快速出售”。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Header: sell title / money / close                         │
│                                                            │
│ Storage Shelf          Appraisal Tray       Value Ledger    │
│ selectable items       selected basket      total/risk      │
│                                                            │
│ Item Risk Marks        Short Preview        Primary Sell    │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `sell_background` | `0,0 1920x1080` | 工坊柜台背景。 |
| `storage_shelf` | `180,180 560x680` | 可售物品，行只显示图标、名称、价值摘要和风险标记。 |
| `appraisal_tray` | `790,230 520x460` | 当前选中物或待售托盘。 |
| `value_ledger` | `1340,230 360x320` | 总价、风险、是否绑定订单。 |
| `sell_short_preview` | `790,720 520x140` | 出售后金币变化、库存变化。 |
| `sell_action_panel` | `1340,600 360x260` | Sell Selected、清空托盘、返回。 |

---

## 5. 信息层级

默认阅读顺序：

1. 当前托盘里准备卖什么。
2. 能获得多少金币。
3. 是否有订单绑定、违禁或高价值风险。
4. 出售后结果。
5. 库存中其他可售物。

不默认展开：

* 全部来源字段。
* 完整价格公式。
* Sell All。
* 每行出售按钮。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `SellSelectedTray` | 唯一主按钮。 |
| Secondary | 选择物品、移出托盘、返回 | 列表选择 / 小按钮。 |
| Tertiary | 筛选、排序、查看来源 | 图标工具。 |
| Danger | Sell All / 出售绑定物 | 默认隐藏或二次确认。 |

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| 单行 Sell | 移到右侧估价区，列表行不放出售按钮。 |
| Sell All | 危险动作，默认折叠或二次确认。 |
| Close | 次行动。 |
| 物品详情 | 选中后在托盘 / 估价牌展示。 |
| 来源字段 | 只在悬停或详情中展开。 |

---

## 8. 场景隐喻

出售界面像工坊前厅的估价柜台：

* 左侧是货架或仓库箱。
* 中央是放物品的托盘。
* 右侧是小账本、钱袋和风险标记。
* 高风险物品像贴了封条或红色小签。

---

## 9. 程序迁移影响

确认后建议层级：

```text
SellPanel
  SellBackground
  StorageShelf
  AppraisalTray
  ValueLedger
  SellShortPreview
  SellActionPanel
```

关键绑定：

| 绑定 | 来源 |
|---|---|
| 可售物品 | Inventory / SellableItemQuery。 |
| 估价 | EconomyPricingService。 |
| 绑定 / 违禁 / 高风险 | Item lifecycle / Order / Economy 标记。 |
| 出售执行 | Economy / Inventory 领域服务。 |

---

## 10. 素材需求变化

第一版复用：

| VisualID | 用法 |
|---|---|
| `bg_workshop_day` | 柜台背景。 |
| `item_*_icon` | 物品候选。 |
| `ui_panel_main` / `ui_panel_info` | 货架、托盘、账本。 |
| `ui_icon_money` | 价值提示。 |
| `ui_icon_warning` / `ui_icon_locked` | 风险和绑定标记。 |
| `ui_button_primary` / `ui_button_secondary` / `ui_button_danger` | 出售、返回、危险动作。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `ui_sell_appraisal_tray` | 估价托盘。 |
| `ui_sell_value_ledger` | 估价账本底板。 |
| `ui_sell_risk_tag` | 风险小签。 |

---

## 11. UX 验收标准

运行时截图需要满足：

1. 3 秒内能看出这是工坊出售 / 估价柜台。
2. 当前待售物和预计金币收益是视觉中心。
3. 列表行不堆出售按钮。
4. Sell Selected 是唯一主行动。
5. Sell All 或出售绑定物必须是危险动作。
6. `sell_panel` 和 `shop_staging` 的功能边界清楚：前者快速出售，后者营业分配。

---

## 12. 用户确认问题

建议确认以下 3 点：

1. `sell_panel` 是否定位为“即时出售 / 清仓估价柜台”。
2. 是否取消每行 Sell，改为托盘选中后统一出售。
3. Sell All 是否默认折叠并二次确认。
