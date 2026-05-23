---
id: art_ui_formal_v1_prosthetic_panel
title: 义体制造界面 Formal V1
type: art
role: 美术
domain: ui_design
status: active
source_of_truth: true
related:
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
last_verified: 2026-05-24
update_rule: 修改义体制造界面正式结构或程序迁移要求时同步本文件。
---

# 义体制造界面 Formal V1

> **目标：** 把义体制造弹窗正式化：配方、材料缺口、可制造状态和已装备状态在同一列表结构中清晰表达。

---

## 1. Formal V1 结构

```text
Workshop Prosthetic Modal
  modal_backdrop
  prosthetic_card
    prosthetic_header
      title / summary / Close
    prosthetic_list
      row: prosthetic icon / slot / material need / Craft / Equipped
```

| ZoneID | Rect | 目标 |
|---|---|---|
| `modal_backdrop` | `0,0 1920x1080` | 隔离工坊底层，形成弹窗焦点。 |
| `prosthetic_card` | `380,150 1160x780` | 义体制造主容器。 |
| `prosthetic_header` | `420,180 1080x140` | 标题、可制造数量、已装备摘要和关闭按钮。 |
| `prosthetic_list` | `430,330 1060x560` | 义体配方列表。 |

当前已写入 active `screen_layouts.json`，状态为 `active_spec / FormalV1`。

---

## 2. 信息层级

| 层级 | 内容 |
|---|---|
| 最高 | 可制造义体、材料缺口和 Craft 按钮。 |
| 次高 | 已装备状态、槽位和功能方向。 |
| 辅助 | 配方说明、材料数量、不可制造原因。 |

文字、材料数、槽位名和按钮文案由 Unity Text 渲染。

---

## 3. 可复用资源

| VisualID | 用途 |
|---|---|
| `ui_panel_main` | 义体主容器和列表底纹。 |
| `ui_list_row_normal` | 普通配方行。 |
| `ui_list_row_selected` | 已装备或焦点配方行。 |
| `ui_button_primary` | Craft。 |
| `ui_button_secondary` | Close。 |
| `ui_icon_equipped` | 已装备状态。 |
| `ui_title_divider` | 标题分隔。 |

---

## 4. 程序迁移要求

关键路径：

| 路径 | VisualID / 组件 |
|---|---|
| `WorkshopProstheticPanel_Runtime/ProstheticPanel_Card` | `Panel.Main` / `ui_panel_main` |
| `WorkshopProstheticPanel_Runtime/ProstheticPanel_Card/TitleDivider_Image` | `Title.Divider` / `ui_title_divider` |
| `WorkshopProstheticPanel_Runtime/ProstheticPanel_Card/Close_Button` | `Button.Secondary` / `ui_button_secondary` |
| `WorkshopProstheticPanel_Runtime/ProstheticPanel_Card/ProstheticList_Scroll` | `Panel.Main` / `ui_panel_main` |
| `ProstheticRow_*` | `List.Row.Normal` / `ui_list_row_normal` |
| `ProstheticRow_*/EquippedIcon_Image` | `Icon.Equipped` / `ui_icon_equipped` |
| `ProstheticRow_*/Craft_Button` | `Button.Primary` / `ui_button_primary` |

已装备图标不阻挡列表行或按钮点击。义体面板打开时出售面板应关闭。

---

## 5. 验收标准

1. 义体面板打开时至少展示 1 条义体配方行。
2. 每行包含图标、名称、槽位、材料需求和 Craft/Equipped 状态。
3. 已装备行使用选中态或已装备图标，Icon.Equipped 不阻挡点击。
4. 材料不足时 Craft_Button 不可交互但仍可读。
5. 列表可滚动，义体图标按 `prosthetic_*_icon` contain 显示。
