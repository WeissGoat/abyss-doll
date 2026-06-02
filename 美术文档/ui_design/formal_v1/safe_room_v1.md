---
id: art_ui_formal_v1_safe_room
title: 深渊安全区界面 Formal V1
type: art
role: 美术
domain: ui_design
status: active
source_of_truth: true
related:
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/13_正式纵切UI与素材覆盖矩阵.md
  - 美术文档/ui_design/formal_v2/14_safe_room_v2.md
last_verified: 2026-06-02
update_rule: 修改安全区正式结构或程序迁移要求时同步本文件。
---

# 深渊安全区界面 Formal V1

> **目标：** 把安全区从恢复提示升级为正式节奏节点。休整、整理背包、使用消耗品、撤离或继续深入都在同一个房间场景中清楚表达。

---

## 1. Formal V1 结构

```text
Safe Room
  safe_room_background
  rest_status_panel
  room_action_panel
    Continue / Organize / Use Item / Extract
  safe_inventory_workbench
  doll_comm_panel
```

| ZoneID | Rect | 目标 |
|---|---|---|
| `safe_room_background` | `0,0 1920x1080` | 安全区房间背景，必须完全不透明。 |
| `rest_status_panel` | `96,72 620x180` | 恢复结果、当前 HP/SAN、背包压力和本轮探索摘要。 |
| `room_action_panel` | `96,300 420x430` | 继续深入、撤离、整理背包、使用消耗品。 |
| `safe_inventory_workbench` | `600,610 720x380` | 安全区背包整理与安全收纳提示区。 |
| `doll_comm_panel` | `1340,190 430x560` | 魔偶通讯、状态恢复、后续轻互动或维护入口。 |

当前已写入 active `screen_layouts.json`，状态为 `active_spec / FormalV1`。

---

## 2. 信息层级

| 层级 | 内容 |
|---|---|
| 最高 | Continue / Extract 的局内去留选择。 |
| 次高 | HP/SAN 恢复结果、本轮战利品、背包压力。 |
| 辅助 | 安全收纳容量、魔偶通讯、后续轻互动入口。 |

文字、数值、恢复量和按钮文案全部由 Unity Text 渲染，不烘焙进图片。

---

## 3. 可复用资源

| VisualID | 用途 |
|---|---|
| `bg_safe_room` | 安全区房间背景。 |
| `ui_panel_main` | 操作面板和背包工作台底板。 |
| `ui_panel_info` | 状态、通讯和提示面板。 |
| `ui_button_primary` | Continue。 |
| `ui_button_secondary` | Organize / Use Item / 通讯入口。 |
| `ui_button_danger` | Extract。 |
| `ui_inventory_chassis_panel` | 背包底盘承托。 |
| `ui_inventory_slot_available` | 背包格。 |

---

## 4. 程序迁移要求

关键路径：

| 路径 | VisualID / 组件 |
|---|---|
| `SafeRoomPanel_Runtime/SafeRoomBackground_Image` | `bg_safe_room` |
| `SafeRoomPanel_Runtime/RestStatusPanel` | `Panel.Info` / `ui_panel_info` |
| `SafeRoomPanel_Runtime/RoomActionPanel` | `Panel.Main` / `ui_panel_main` |
| `SafeRoomPanel_Runtime/RoomActionPanel/Continue_Button` | `Button.Primary` / `ui_button_primary` |
| `SafeRoomPanel_Runtime/RoomActionPanel/Extract_Button` | `Button.Danger` / `ui_button_danger` |
| `SafeRoomPanel_Runtime/SafeInventoryWorkbench/ChassisFrame_Image` | `Inventory.ChassisPanel` / `ui_inventory_chassis_panel` |
| `InventoryCanvas/GridContainer` | `Inventory.Slot` / `ui_inventory_slot_available` |
| `SafeRoomPanel_Runtime/DollCommPanel` | `Panel.Info` / `ui_panel_info` |

背包仍复用全局 `GridContainer` 和 `InventoryItemLayer`，不创建第二套背包数据。

---

## 5. 验收标准

1. 安全区截图能明显区别于战斗/地图界面，背景使用 `bg_safe_room` 且无透明黑洞。
2. Continue、Extract、Organize / Use Item 操作层级清楚，撤离是危险操作。
3. 背包格保持 100x100 与 5 间距，拖拽不被背景或面板阻挡。
4. HP/SAN 恢复、当前层、战利品摘要和安全收纳提示可读。
5. 安全区不推进局外天数的规则不由 UI 文案烘焙，文本由程序渲染。
