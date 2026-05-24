---
id: art_ui_formal_v1_scenario_event
title: 剧本事件界面 Formal V1
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
update_rule: 修改剧本事件表现结构、AVG/气泡/日志/系统弹窗槽位或程序迁移要求时同步本文件。
---

# 剧本事件界面 Formal V1

> **状态：** 已写入 active `screen_layouts.json`，作为当前依赖 UI 规格和后续程序接入口。
> **策划依据：** `设计文档/10_剧本调度与事件队列规则卡.md`、`设计文档/23_长期记忆剧情内容包.md`。
> **目标：** 给主线 AVG、系统弹窗、气泡、LorePanel、事件日志和跳过摘要提供统一表现规格。事件队列负责状态，UI 只负责展示和选择。

## 1. 结构

```text
scenario_event_overlay
  event_modal_card
    event_header
    speaker_visual_slot
    dialogue_text_panel
    choice_list_panel
    command_summary_panel
    event_action_bar
```

## 2. 区域

| ZoneID | 参考区域 | 目标 |
|---|---|---|
| `scenario_event_overlay` | `0,0 1920x1080` | 暗化背景或复用当前场景，事件层不烘焙文字。 |
| `event_modal_card` | `300,130 1320x820` | Blocking AVG / SystemModal 主容器。 |
| `event_header` | `360,170 1200x90` | 事件类型、优先级、来源、标题图标。 |
| `speaker_visual_slot` | `360,290 380x430` | 人偶、势力徽记或物品/lore 图标。 |
| `dialogue_text_panel` | `780,290 780x260` | 当前对白、系统说明或 Lore 正文。 |
| `choice_list_panel` | `780,580 780x210` | 选项列表，支持 1-4 个选择。 |
| `command_summary_panel` | `360,740 560x120` | 跳过摘要、奖励/惩罚/旗标提示。 |
| `event_action_bar` | `960,820 600x90` | 继续、跳过、确认、关闭。 |

## 3. VisualID

| VisualID | 用途 |
|---|---|
| `ui_panel_main` / `ui_panel_info` | 事件主卡和信息区。 |
| `ui_list_row_normal` / `ui_list_row_selected` | 选项行。 |
| `ui_icon_event` | 事件入口。 |
| `ui_icon_lore` | Lore / 图鉴。 |
| `ui_icon_skip` | 跳过 / 摘要。 |
| `ui_icon_warning` | 债务、崩溃、不可跳过、指令风险。 |
| `doll_proto_0_stand` | 人偶说话者 fallback。 |

## 4. 程序边界

1. 事件触发、排序、跳过、命令执行和旗标写入都归事件系统。
2. UI 不直接执行奖励、惩罚、AddBond、RemoveGold、SetFlag 等命令。
3. 跳过按钮只请求事件系统执行跳过流程；必须显示 SummaryOnSkip 结果摘要。
4. 非阻塞气泡可以复用本规格的 header / small panel，但不打开主 modal。

## 5. 验收

1. Blocking 事件能显示说话者、正文、选项和动作栏。
2. SystemModal 与 LorePanel 能复用同一结构，但图标、警告和按钮层级不同。
3. 跳过不隐藏关键状态变化摘要。
4. 面板不遮挡自身按钮射线；背景层不接收点击。
