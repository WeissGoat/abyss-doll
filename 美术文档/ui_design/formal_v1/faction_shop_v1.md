---
id: art_ui_formal_v1_faction_shop
title: 势力商店界面 Formal V1
type: art
role: 美术
domain: ui_design
status: active
source_of_truth: true
related:
  - 美术文档/ui_design/README.md
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/ui_design/versions/migration_log.md
  - 美术文档/13_正式纵切UI与素材覆盖矩阵.md
last_verified: 2026-05-24
update_rule: 修改势力商店结构、声望/信任展示或程序迁移要求时同步本文件。
---

# 势力商店界面 Formal V1

> **状态：** 已写入 active `screen_layouts.json`，作为当前依赖 UI 规格和后续程序接入口。
> **策划依据：** `设计文档/rules/07_势力声望与订单规则卡.md`、`设计文档/content_packs/24_势力订单声望内容包.md`。
> **目标：** 把势力声望、商店解锁、黑市信任和商品兑换做成稳定入口。玩家能比较不同势力的解锁、价格、库存和风险，而不是把势力商店藏在订单板的备注里。

## 1. 结构

```text
faction_shop_background
  faction_shop_card
    faction_header
    faction_list_panel
    reputation_track_panel
    shop_item_list
    selected_item_detail
    shop_action_panel
    black_market_risk_panel
```

## 2. 区域

| ZoneID | 参考区域 | 目标 |
|---|---|---|
| `faction_shop_background` | `0,0 1920x1080` | 复用工坊背景，表现小镇拜访/交易空间。 |
| `faction_shop_card` | `260,90 1400x900` | 商店主容器。 |
| `faction_header` | `320,130 1280x110` | 当前势力、声望等级、信任状态和标题分隔。 |
| `faction_list_panel` | `320,270 320x610` | 势力列表，显示正规势力和黑市入口。 |
| `reputation_track_panel` | `680,270 360x260` | 声望/信任等级、下一解锁和怀疑状态。 |
| `shop_item_list` | `680,560 520x320` | 可购买商品、图纸、服务和情报。 |
| `selected_item_detail` | `1240,270 360x420` | 选中商品详情、条件、价格和库存。 |
| `shop_action_panel` | `1240,720 360x160` | 购买、关闭、前往订单等操作。 |
| `black_market_risk_panel` | `320,900 880x70` | 黑市风险、正规势力怀疑和背叛后果提示。 |

## 3. VisualID

| VisualID | 用途 |
|---|---|
| `bg_workshop_day` | 背景。 |
| `ui_panel_main` / `ui_panel_info` | 主容器和信息区。 |
| `ui_list_row_normal` / `ui_list_row_selected` | 势力与商品列表。 |
| `ui_icon_faction` | 势力入口。 |
| `ui_icon_reputation` | 正规声望。 |
| `ui_icon_trust` | 黑市信任。 |
| `ui_icon_black_market` | 黑市入口和风险。 |
| `ui_icon_money` | 价格。 |
| `ui_icon_warning` | 怀疑、锁定和风险。 |

## 4. 程序边界

1. UI 只展示 Faction / Shop / Reward 数据，不直接改金币、声望、库存或解锁。
2. 购买、兑换、黑市交易和跳转订单都通过服务接口完成。
3. 商品名、价格、库存、声望等级、解锁条件和风险说明由 Unity Text 渲染。
4. 黑市相关操作必须有明确风险区和二次确认入口，不能混同普通购买按钮。

## 5. 验收

1. 势力列表、声望/信任轨、商品列表、选中详情和动作按钮同时可见。
2. 正规势力和黑市入口在视觉上能区分，黑市风险不会被藏到小字里。
3. 未解锁商品能显示条件，不提前作为可购买状态。
4. 所有面板和图标不阻挡列表滚动、商品选择或按钮点击。
