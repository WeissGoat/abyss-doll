---
id: art_ui_formal_v1_shop_staging
title: 出货分配界面 Formal V1
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
update_rule: 修改出货分配界面正式结构、资源槽位或程序迁移要求时同步本文件。
---

# 出货分配界面 Formal V1

> **目标：** 把仓库出货从单纯出售扩展为正式的出货分配界面。玩家能把物品分配到柜台、订单、黑市或暂存，并理解收益、风险和截止日。

## 1. Formal V1 结构

```text
Shop Staging
  shop_staging_background
  shop_staging_card
    shop_staging_header
    storage_item_list
    channel_lane_board
    staging_preview_panel
    staging_action_panel
    staging_bottom_hint
```

| ZoneID | Rect | 目标 |
|---|---|---|
| `shop_staging_background` | `0,0 1920x1080` | 复用工坊背景，表示营业前整理出货。 |
| `shop_staging_card` | `220,90 1480x900` | 出货分配主容器。 |
| `shop_staging_header` | `280,130 1360x110` | 日期、金币、风险摘要和标题分隔。 |
| `storage_item_list` | `280,270 470x560` | 可出货仓库列表。 |
| `channel_lane_board` | `790,270 520x560` | 柜台、订单、黑市、暂存四类去向。 |
| `staging_preview_panel` | `1350,270 290x420` | 当前选择的收益、风险、截止日和隔夜代价。 |
| `staging_action_panel` | `1350,720 290x170` | 确认、重置、黑市/危险动作和返回。 |
| `staging_bottom_hint` | `280,850 1030x100` | 未分配物、容量和今日收益预测。 |

当前已写入 active `screen_layouts.json`，状态为 `active_spec / FormalV1`。

## 2. 信息层级

| 层级 | 内容 |
|---|---|
| 最高 | 未分配物、确认出货、危险渠道提示。 |
| 次高 | 物品估值、渠道容量、订单/黑市收益差异。 |
| 辅助 | 违禁、隔夜代价、今日营业预测。 |

文字、价格、数量、订单名、截止日和风险说明全部由 Unity Text 渲染，不烘焙进图片。

## 3. 新增与复用资源

| VisualID | 用途 |
|---|---|
| `bg_workshop_day` | 工坊背景。 |
| `ui_panel_main` / `ui_panel_info` | 主容器和信息面板。 |
| `ui_list_row_normal` / `ui_list_row_selected` | 仓库物品和渠道行。 |
| `ui_button_primary` / `ui_button_secondary` / `ui_button_danger` | 确认、返回、危险渠道动作。 |
| `ui_icon_shop_channel` | 普通出货渠道。 |
| `ui_icon_black_market` | 黑市或高风险渠道。 |
| `ui_icon_order` | 订单渠道槽。 |
| `ui_icon_money` / `ui_icon_warning` | 金币与风险提示。 |

## 4. 程序迁移要求

关键路径：

| 路径 | VisualID / 组件 |
|---|---|
| `ShopStagingPanel_Runtime/Background_Image` | `bg_workshop_day` |
| `ShopStagingPanel_Runtime/StagingCard_Image` | `Panel.Main` / `ui_panel_main` |
| `ShopStagingPanel_Runtime/StagingCard_Image/StorageItemListPanel/ItemRow_Template` | `List.Row.Normal` / `ui_list_row_normal` |
| `ShopStagingPanel_Runtime/StagingCard_Image/ChannelLaneBoard/CounterLaneIcon_Image` | `Icon.ShopChannel` / `ui_icon_shop_channel` |
| `ShopStagingPanel_Runtime/StagingCard_Image/ChannelLaneBoard/OrderLaneIcon_Image` | `Icon.Order` / `ui_icon_order` |
| `ShopStagingPanel_Runtime/StagingCard_Image/ChannelLaneBoard/BlackMarketLaneIcon_Image` | `Icon.BlackMarket` / `ui_icon_black_market` |
| `ShopStagingPanel_Runtime/StagingCard_Image/ActionPanel/Confirm_Button` | `Button.Primary` / `ui_button_primary` |
| `ShopStagingPanel_Runtime/StagingCard_Image/ActionPanel/BlackMarket_Button` | `Button.Danger` / `ui_button_danger` |

UI 只提交分配结果，不直接改金币、声望、物品归属或隔夜风险。

## 5. 验收标准

1. 仓库列表、渠道 lane、收益风险预览和确认动作同时可见。
2. 普通柜台、订单、黑市、暂存四类去向视觉上可区分。
3. 黑市或违禁风险必须通过 warning / danger 视觉表达。
4. 价格、数量、物品名、订单名、截止日和风险说明均由 Unity Text 渲染。
5. 列表行和渠道容器不阻挡按钮射线；后续拖拽接入时不得破坏 ScrollRect。
