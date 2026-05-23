---
id: art_ui_formal_v1_layer_select
title: 出发层选择界面 Formal V1
type: art
role: 美术
domain: ui_design
status: active
source_of_truth: true
related:
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
last_verified: 2026-05-24
update_rule: 修改出发层选择界面正式结构或程序迁移要求时同步本文件。
---

# 出发层选择界面 Formal V1

> **目标：** 把出发层选择正式化：可进入层、锁定层、当前选择和确认下潜操作在深渊入口背景上清楚表达。

---

## 1. Formal V1 结构

```text
Layer Select
  layer_select_background
  layer_select_modal
    title / summary / divider
    layer_list
      row: layer name / unlocked or locked / selected state
    layer_actions
      close / confirm
```

| ZoneID | Rect | 目标 |
|---|---|---|
| `layer_select_background` | `0,0 1920x1080` | 深渊入口背景。 |
| `layer_select_modal` | `480,180 960x720` | 层列表、锁定状态和出发确认主容器。 |
| `layer_list` | `560,380 800x390` | 可进入层和锁定层列表。 |
| `layer_actions` | `520,820 880x90` | 开始下潜和返回按钮。 |

当前已写入 active `screen_layouts.json`，状态为 `active_spec / FormalV1`。

---

## 2. 信息层级

| 层级 | 内容 |
|---|---|
| 最高 | 可进入层、当前选中层和 Confirm。 |
| 次高 | 锁定层、解锁状态、Close。 |
| 辅助 | 层级说明、风险摘要、已通层提示。 |

文字、层名、解锁条件和按钮文案由 Unity Text 渲染。

---

## 3. 可复用资源

| VisualID | 用途 |
|---|---|
| `bg_layer_select` | 层选择背景。 |
| `ui_panel_main` | 主弹窗面板。 |
| `ui_list_row_normal` | 普通层行。 |
| `ui_list_row_selected` | 当前选中层行。 |
| `ui_button_primary` | Confirm。 |
| `ui_button_secondary` | Close。 |
| `ui_icon_locked` | 锁定层图标。 |
| `ui_title_divider` | 标题分隔。 |

---

## 4. 程序迁移要求

关键路径：

| 路径 | VisualID / 组件 |
|---|---|
| `DungeonStartLayerPanel_Runtime/LayerSelectBackground_Image` | `bg_layer_select` |
| `DungeonStartLayerPanel_Runtime/DungeonStartLayer_Card` | `Panel.Main` / `ui_panel_main` |
| `DungeonStartLayerPanel_Runtime/DungeonStartLayer_Card/TitleDivider_Image` | `Title.Divider` / `ui_title_divider` |
| `DungeonStartLayerPanel_Runtime/DungeonStartLayer_Card/LayerList/DungeonStartLayer_*` | `List.Row.Normal` / `ui_list_row_normal` |
| `DungeonStartLayer_*/LockedIcon_Image` | `Icon.Locked` / `ui_icon_locked` |
| `DungeonStartLayer_Card/Confirm_Button` | `Button.Primary` / `ui_button_primary` |
| `DungeonStartLayer_Card/Close_Button` | `Button.Secondary` / `ui_button_secondary` |

锁定图标、标题装饰和背景不阻挡列表行或按钮点击。不可进入层的 Button 应不可交互。

---

## 5. 验收标准

1. 层选择界面背景使用 `bg_layer_select`，不透出工坊杂乱背景。
2. 至少显示第 1 层和第 2 层行，锁定层显示 `ui_icon_locked`。
3. 选中行使用 `ui_list_row_selected` 或明显高亮，未选中行使用 `ui_list_row_normal`。
4. Confirm 只在可进入层可交互，Close 可返回工坊。
5. 锁定图标、标题装饰和背景不阻挡列表行或按钮点击。
