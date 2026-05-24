---
id: art_ui_formal_v1_rumor_board
title: 传闻情报板界面 Formal V1
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
update_rule: 修改传闻情报板正式结构、资源槽位或程序迁移要求时同步本文件。
---

# 传闻情报板界面 Formal V1

> **目标：** 把每日传闻、价格波动和深渊情报做成正式局外决策入口。玩家能看到今天哪些物品涨跌、哪些节点更危险、下一次下潜该优先考虑什么。

## 1. Formal V1 结构

```text
Rumor Board
  rumor_board_background
  rumor_board_card
    rumor_header
    today_rumor_list
    price_wave_panel
    rumor_detail_panel
    recommendation_panel
    rumor_bottom_hint
```

| ZoneID | Rect | 目标 |
|---|---|---|
| `rumor_board_background` | `0,0 1920x1080` | 复用工坊背景，表现局外情报板。 |
| `rumor_board_card` | `280,90 1360x900` | 传闻情报主容器。 |
| `rumor_header` | `340,130 1240x110` | 日期、市场状态、传闻图标和标题分隔。 |
| `today_rumor_list` | `340,270 520x560` | 今日传闻列表。 |
| `price_wave_panel` | `900,270 360x560` | 价格涨跌和持续时间。 |
| `rumor_detail_panel` | `1300,270 280x360` | 选中传闻详情和影响对象。 |
| `recommendation_panel` | `1300,660 280x220` | 推荐目标、规划下潜和返回。 |
| `rumor_bottom_hint` | `340,850 920x100` | 有效期、可信度和经济风险说明。 |

当前已写入 active `screen_layouts.json`，状态为 `active_spec / FormalV1`。

## 2. 信息层级

| 层级 | 内容 |
|---|---|
| 最高 | 今日关键传闻、明显价格涨跌、推荐行动。 |
| 次高 | 传闻可信度、影响对象、剩余天数。 |
| 辅助 | 深渊情报、经济风险、当前未处理事项。 |

传闻标题、物品名、倍率、有效期和说明全部由 Unity Text 渲染。

## 3. 新增与复用资源

| VisualID | 用途 |
|---|---|
| `bg_workshop_day` | 工坊背景。 |
| `ui_panel_main` / `ui_panel_info` | 主容器、列表、详情和推荐面板。 |
| `ui_list_row_normal` / `ui_list_row_selected` | 传闻和价格波动行。 |
| `ui_button_primary` / `ui_button_secondary` | 规划下潜与返回。 |
| `ui_icon_rumor` | 传闻、情报和推荐目标。 |
| `ui_icon_price_up` | 价格上涨。 |
| `ui_icon_price_down` | 价格下跌。 |
| `ui_icon_warning` / `ui_icon_money` | 风险与价格信息。 |

## 4. 程序迁移要求

关键路径：

| 路径 | VisualID / 组件 |
|---|---|
| `RumorBoardPanel_Runtime/Background_Image` | `bg_workshop_day` |
| `RumorBoardPanel_Runtime/RumorBoardCard_Image` | `Panel.Main` / `ui_panel_main` |
| `RumorBoardPanel_Runtime/RumorBoardCard_Image/HeaderPanel/RumorIcon_Image` | `Icon.Rumor` / `ui_icon_rumor` |
| `RumorBoardPanel_Runtime/RumorBoardCard_Image/TodayRumorListPanel/RumorRow_Template` | `List.Row.Normal` / `ui_list_row_normal` |
| `RumorBoardPanel_Runtime/RumorBoardCard_Image/PriceWavePanel/PriceUpIcon_Image` | `Icon.PriceUp` / `ui_icon_price_up` |
| `RumorBoardPanel_Runtime/RumorBoardCard_Image/PriceWavePanel/PriceDownIcon_Image` | `Icon.PriceDown` / `ui_icon_price_down` |
| `RumorBoardPanel_Runtime/RumorBoardCard_Image/RecommendationPanel/PlanExpedition_Button` | `Button.Primary` / `ui_button_primary` |
| `RumorBoardPanel_Runtime/RumorBoardCard_Image/RecommendationPanel/Close_Button` | `Button.Secondary` / `ui_button_secondary` |

传闻详情和推荐面板只展示与跳转，不直接修改地图、价格或订单。

## 5. 验收标准

1. 今日传闻、价格涨跌、选中详情和推荐行动同时可见。
2. 涨价和跌价方向通过图标区分，倍率、物品名和有效期由 Unity Text 渲染。
3. 高风险、低可信或即将过期传闻能用 warning 图标或运行时 tint 表达。
4. Plan Expedition 只是跳转 / 规划入口，不直接修改地图节点或经济状态。
5. 背景、面板、分隔线和图标不阻挡列表滚动或按钮点击。
