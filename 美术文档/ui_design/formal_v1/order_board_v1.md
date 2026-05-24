---
id: art_ui_formal_v1_order_board
title: 势力订单板界面 Formal V1
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
update_rule: 修改势力订单板正式结构、资源槽位或程序迁移要求时同步本文件。
---

# 势力订单板界面 Formal V1

> **目标：** 把势力订单做成正式目标选择界面。玩家能比较订单目标、截止日、所需物品、奖励、声望和失败风险。

## 1. Formal V1 结构

```text
Order Board
  order_board_background
  order_board_card
    order_header
    order_list_panel
    order_detail_panel
    order_reward_panel
    order_action_panel
```

| ZoneID | Rect | 目标 |
|---|---|---|
| `order_board_background` | `0,0 1920x1080` | 复用工坊背景，表示委托公告板或订单台。 |
| `order_board_card` | `260,90 1400x900` | 订单板主容器。 |
| `order_header` | `320,130 1280x110` | 订单容量、刷新时间、势力入口和标题分隔。 |
| `order_list_panel` | `320,270 500x610` | 可接取 / 已接取订单列表。 |
| `order_detail_panel` | `860,270 470x610` | 选中订单目标物、数量、失败代价和备注。 |
| `order_reward_panel` | `1360,270 240x260` | 金币、材料、声望和解锁预览。 |
| `order_action_panel` | `1360,560 240x320` | 接取、提交、放弃、返回等动作。 |

当前已写入 active `screen_layouts.json`，状态为 `active_spec / FormalV1`。

## 2. 信息层级

| 层级 | 内容 |
|---|---|
| 最高 | 可提交订单、即将过期订单、主行动按钮。 |
| 次高 | 目标物、需求数量、奖励和声望变化。 |
| 辅助 | 订单来源、刷新时间、失败或背叛代价。 |

订单名、势力名、截止日、数量和奖励全部由 Unity Text 渲染。

## 3. 新增与复用资源

| VisualID | 用途 |
|---|---|
| `bg_workshop_day` | 工坊背景。 |
| `ui_panel_main` / `ui_panel_info` | 主容器、详情、奖励和动作面板。 |
| `ui_list_row_normal` / `ui_list_row_selected` | 订单列表行。 |
| `ui_button_primary` / `ui_button_secondary` / `ui_button_danger` | 接取/提交、返回、放弃。 |
| `ui_icon_order` | 订单本体和交付目标。 |
| `ui_icon_faction` | 势力来源、声望和委托方。 |
| `ui_icon_deadline` | 截止日和时间压力。 |
| `ui_icon_money` / `ui_icon_warning` | 奖励与风险。 |

## 4. 程序迁移要求

关键路径：

| 路径 | VisualID / 组件 |
|---|---|
| `OrderBoardPanel_Runtime/Background_Image` | `bg_workshop_day` |
| `OrderBoardPanel_Runtime/OrderBoardCard_Image` | `Panel.Main` / `ui_panel_main` |
| `OrderBoardPanel_Runtime/OrderBoardCard_Image/HeaderPanel/OrderIcon_Image` | `Icon.Order` / `ui_icon_order` |
| `OrderBoardPanel_Runtime/OrderBoardCard_Image/HeaderPanel/FactionIcon_Image` | `Icon.Faction` / `ui_icon_faction` |
| `OrderBoardPanel_Runtime/OrderBoardCard_Image/OrderListPanel/OrderRow_Template` | `List.Row.Normal` / `ui_list_row_normal` |
| `OrderBoardPanel_Runtime/OrderBoardCard_Image/OrderListPanel/DeadlineIcon_Image` | `Icon.Deadline` / `ui_icon_deadline` |
| `OrderBoardPanel_Runtime/OrderBoardCard_Image/ActionPanel/Accept_Button` | `Button.Primary` / `ui_button_primary` |
| `OrderBoardPanel_Runtime/OrderBoardCard_Image/ActionPanel/Abandon_Button` | `Button.Danger` / `ui_button_danger` |

UI 不直接修改物品、金币、声望或截止日，只调用订单 / 声望服务。

## 5. 验收标准

1. 订单列表、选中订单详情、奖励预览、截止日和主要动作同时可见。
2. 玩家能区分可接取、已接取、可提交、即将过期和高风险订单。
3. 订单目标物使用物品图标和 Unity Text，不在订单图标里烘焙物品名或数字。
4. 接取 / 提交是主行动，放弃 / 背叛类动作使用危险按钮样式。
5. UI Controller 不写死订单规则或奖励结算。
