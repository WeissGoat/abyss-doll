---
id: art_ui_formal_v1_stairs_room
title: 深渊阶梯房间界面 Formal V1
type: art
role: 美术
domain: ui_design
status: active
source_of_truth: true
related:
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/13_正式纵切UI与素材覆盖矩阵.md
  - 美术文档/ui_design/formal_v2/15_stairs_room_v2.md
last_verified: 2026-06-02
update_rule: 修改阶梯房间正式结构或程序迁移要求时同步本文件。
---

# 深渊阶梯房间界面 Formal V1

> **目标：** 把阶梯房间做成层间决策界面。进入下一层、撤离、查看下一层风险和整理背包之间的取舍要清楚可读。

---

## 1. Formal V1 结构

```text
Stairs Room
  stairs_room_background
  next_layer_briefing
  stairs_choice_panel
    Descend / Organize / Extract
  carry_risk_panel
  stairs_inventory_workbench
```

| ZoneID | Rect | 目标 |
|---|---|---|
| `stairs_room_background` | `0,0 1920x1080` | 阶梯/升降井房间背景，必须完全不透明。 |
| `next_layer_briefing` | `96,72 680x220` | 下一层层级、风险、预期奖励、解锁状态和撤离后果。 |
| `stairs_choice_panel` | `96,340 460x420` | 继续进入下一层、整理背包、撤离等关键选择。 |
| `carry_risk_panel` | `1220,90 560x260` | 当前背包重量、格外物品、重要战利品和损失风险提示。 |
| `stairs_inventory_workbench` | `600,610 720x380` | 进入下一层前的最后背包整理区。 |

当前已写入 active `screen_layouts.json`，状态为 `active_spec / FormalV1`。

---

## 2. 信息层级

| 层级 | 内容 |
|---|---|
| 最高 | Descend / Extract 的层间决策。 |
| 次高 | 下一层风险、当前携带风险、格外物品处理。 |
| 辅助 | 战利品价值、层级解锁、背包整理入口。 |

文字、层名、风险、奖励和按钮文案全部由 Unity Text 渲染。

---

## 3. 可复用资源

| VisualID | 用途 |
|---|---|
| `bg_stairs_room` | 阶梯房间背景。 |
| `ui_panel_main` | 选择面板和背包工作台底板。 |
| `ui_panel_info` | 下一层简报和携带风险提示。 |
| `ui_button_primary` | Descend。 |
| `ui_button_secondary` | Organize。 |
| `ui_button_danger` | Extract。 |
| `ui_inventory_chassis_panel` | 背包底盘承托。 |
| `ui_inventory_slot_available` | 背包格。 |

---

## 4. 程序迁移要求

关键路径：

| 路径 | VisualID / 组件 |
|---|---|
| `StairsRoomPanel_Runtime/StairsRoomBackground_Image` | `bg_stairs_room` |
| `StairsRoomPanel_Runtime/NextLayerBriefingPanel` | `Panel.Info` / `ui_panel_info` |
| `StairsRoomPanel_Runtime/StairsChoicePanel` | `Panel.Main` / `ui_panel_main` |
| `StairsRoomPanel_Runtime/StairsChoicePanel/Descend_Button` | `Button.Primary` / `ui_button_primary` |
| `StairsRoomPanel_Runtime/StairsChoicePanel/Extract_Button` | `Button.Danger` / `ui_button_danger` |
| `StairsRoomPanel_Runtime/CarryRiskPanel` | `Panel.Info` / `ui_panel_info` |
| `StairsRoomPanel_Runtime/StairsInventoryWorkbench/ChassisFrame_Image` | `Inventory.ChassisPanel` / `ui_inventory_chassis_panel` |
| `InventoryCanvas/GridContainer` | `Inventory.Slot` / `ui_inventory_slot_available` |

通过阶梯继续深入仍视为同一轮探索，保留本轮战利品账本；UI 只表达状态，不复制规则。

---

## 5. 验收标准

1. 阶梯房间截图能明显表达“下一层入口”，背景使用 `bg_stairs_room` 且无透明黑洞。
2. Descend、Extract、Organize 操作层级清楚，进入下一层是主行动，撤离是结束本轮行动。
3. 下一层风险、当前携带风险、格外物品和战利品价值提示可读。
4. 背包格保持 100x100 与 5 间距，拖拽不被背景或面板阻挡。
5. 继续深入不清空本轮探索账本的规则由程序状态保证，不烘焙进图片。
