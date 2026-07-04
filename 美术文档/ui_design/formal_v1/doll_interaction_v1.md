---
id: art_ui_formal_v1_doll_interaction
title: 人偶交互界面 Formal V1
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
  - 美术文档/ui_design/formal_v2/19_doll_interaction_v2.md
last_verified: 2026-06-02
update_rule: 修改人偶交互结构、触摸/对话/赠礼/保养入口或程序迁移要求时同步本文件。
---

# 人偶交互界面 Formal V1

> **状态：** 已写入 active `screen_layouts.json`，作为当前依赖 UI 规格和后续程序接入口。
> **策划依据：** `设计文档/规则卡/08_人偶核心状态与好感双轨规则卡.md`、`设计文档/规则卡/09_人偶交互事件与反馈规则卡.md`。
> **目标：** 给触摸、对话、赠礼、保养和特殊交互一个正式入口。界面表达“照料与反馈”，但不把 Bond / SAN / 情绪裁决写进 UI。

## 1. 结构

```text
doll_interaction_background
  doll_interaction_card
    condition_header
    doll_stage
    interaction_menu
    gift_or_topic_panel
    feedback_panel
    action_hint_panel
```

## 2. 区域

| ZoneID | 参考区域 | 目标 |
|---|---|---|
| `doll_interaction_background` | `0,0 1920x1080` | 复用工坊背景，表现照料场景。 |
| `doll_interaction_card` | `220,80 1480x920` | 交互主容器。 |
| `condition_header` | `280,120 1360x100` | HP、SAN、Bond、情绪、每日交互次数和警告。 |
| `doll_stage` | `1020,230 520x610` | 人偶立绘、表情/待机动作和点击热区。 |
| `interaction_menu` | `300,250 300x520` | 触摸、对话、赠礼、保养、特殊交互按钮。 |
| `gift_or_topic_panel` | `630,250 350x520` | 礼物、话题、保养材料或交互目标列表。 |
| `feedback_panel` | `300,800 1240x130` | 反馈台词、结果提示和上限/冷却原因。 |
| `action_hint_panel` | `1560,250 100x520` | 安抚、跳过、关闭等轻量操作。 |

## 3. VisualID

| VisualID | 用途 |
|---|---|
| `bg_workshop_day` | 背景。 |
| `doll_proto_0_stand` | 人偶展示。 |
| `ui_icon_touch` | 触摸。 |
| `ui_icon_talk` | 对话。 |
| `ui_icon_gift` | 赠礼。 |
| `ui_icon_maintenance` | 保养。 |
| `ui_icon_warning` | 低 SAN、冷却、上限和拒绝。 |
| `ui_icon_memento` | 特殊交互或记忆反馈入口。 |

## 4. 程序边界

1. UI 生成交互请求，最终 SAN / Bond / 情绪 / 特质变化由人偶核心状态系统裁决。
2. 触摸区、话题池、礼物列表和保养材料来自配置或运行时服务，不写进图片。
3. 超过上限仍可显示反馈，但不能由 UI 自行继续增加数值。
4. `Broken`、低 SAN 和拒绝状态必须有可见提示，不能用普通温馨反馈覆盖。

## 5. 验收

1. 人偶舞台、交互菜单、礼物/话题列表和反馈区同时可见。
2. 触摸、对话、赠礼、保养和特殊入口图标能被区分。
3. 普通文本、数值、台词和按钮文案全部由 Unity Text 渲染。
4. 立绘和装饰面板不拦截按钮、列表或触摸热区射线。
