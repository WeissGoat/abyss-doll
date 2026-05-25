---
id: art_ui_formal_v1_doll_room
title: 人偶房间界面 Formal V1
type: art
role: 美术
domain: ui_design
status: active
source_of_truth: false
related:
  - 美术文档/ui_design/README.md
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/ui_design/versions/migration_log.md
  - 美术文档/13_正式纵切UI与素材覆盖矩阵.md
last_verified: 2026-05-25
update_rule: 修改人偶房间结构、纪念物/日记/窗外状态槽位或程序迁移要求时同步本文件。
---

# 人偶房间界面 Formal V1

> **状态：** 已写入 active `screen_layouts.json`，可作为程序接入口和素材生产入口。
> **策划依据：** `设计文档/rules/11_人偶房间布局与视觉叙事规则卡.md`、`设计文档/content_packs/23_长期记忆剧情内容包.md`。
> **目标：** 把人偶房间做成零压力视觉日记入口：房间背景、待机人偶、纪念物、窗外状态、日记本和观察反馈共同记录长期经历。

## 1. 结构

```text
doll_room_background
  window_state_area
  doll_idle_stage
  memento_display_area
  diary_panel
  room_detail_panel
  room_action_strip
```

## 2. 区域

| ZoneID | 参考区域 | 目标 |
|---|---|---|
| `doll_room_background` | `0,0 1920x1080` | 完全不透明房间背景，cover 适配。 |
| `window_state_area` | `120,100 460x320` | 窗外状态、红月、雨夜、债务阴影等覆盖槽。 |
| `doll_idle_stage` | `680,180 520x700` | 人偶待机、低 SAN、Bond、战败后姿态。 |
| `memento_display_area` | `1230,180 460x520` | 纪念物、展示品、礼物剪影和空槽。 |
| `diary_panel` | `120,470 480x360` | 日记条目和房间记忆列表。 |
| `room_detail_panel` | `1230,730 460x170` | 选中纪念物、窗外状态或日记详情。 |
| `room_action_strip` | `680,910 520x80` | 观察、日记、返回工坊等轻操作。 |

## 3. VisualID

| VisualID | 用途 |
|---|---|
| `bg_doll_room_attic` | 人偶房间基础背景。 |
| `doll_proto_0_stand` | 人偶待机 fallback。 |
| `ui_room_memento_slot` | 纪念物/展示品槽位。 |
| `ui_icon_diary` | 日记本入口。 |
| `ui_icon_memento` | 纪念物。 |
| `ui_icon_warning` | 低 SAN、崩溃、债务阴影或资源缺失。 |
| `ui_panel_info` | 日记、详情和提示区。 |

## 4. 程序边界

1. 房间进入、观察、查看日记和展柜均为零时耗，不推进 Day。
2. 房间不承担仓库功能；展示品是快照或视觉引用，来源物出售后按规则清空展示。
3. 纪念物、窗外状态、待机姿态和日记条目来自房间/事件系统，不由 UI 自行判断。
4. 缺资源时按 fallback 显示剪影或隐藏槽位，并记录缺失资源。

## 5. 验收

1. 房间截图能明显区别于工坊、战斗和深渊房间。
2. 人偶待机、窗外状态、纪念物展示、日记和详情区互不遮挡。
3. 点击房间区域只触发观察/详情，不推进 Day，不触发营业结算。
4. 背景必须完全不透明，不允许出现透明黑洞。
