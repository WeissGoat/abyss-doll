---
id: art_ui_formal_v1_chassis_upgrade_panel
title: 底盘升级界面 Formal V1
type: art
role: 美术
domain: ui_design
status: active
source_of_truth: true
related:
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/ui_design/formal_v2/08_chassis_upgrade_panel_v2.md
  - 美术文档/13_正式纵切UI与素材覆盖矩阵.md
  - 美术文档/ui_design/versions/migration_log.md
last_verified: 2026-05-25
update_rule: 修改底盘升级界面正式结构或程序迁移要求时同步本文件。
---

# 底盘升级界面 Formal V1

> **目标：** 把底盘升级从工坊摘要拆成正式局外成长界面。玩家要能同时看懂当前底盘、下一阶段底盘、容量变化、材料/金币缺口、蓝图前置和升级确认。

---

## 1. Formal V1 结构

```text
Chassis Upgrade Panel
  upgrade_background
  upgrade_card
    upgrade_header
    current_chassis_preview
    next_chassis_preview
    cost_and_requirement_list
    upgrade_delta_panel
    upgrade_action_panel
```

| ZoneID | Rect | 目标 |
|---|---|---|
| `upgrade_background` | `0,0 1920x1080` | 复用工坊背景，表达局外工程台环境。 |
| `upgrade_card` | `220,110 1480x860` | 底盘升级主容器。 |
| `upgrade_header` | `280,150 1360x110` | 标题、底盘升级图标、蓝图状态和关闭入口。 |
| `current_chassis_preview` | `300,300 520x430` | 当前底盘和当前背包格局预览。 |
| `next_chassis_preview` | `860,300 520x430` | 升级后底盘、容量变化和新解锁格局预览。 |
| `cost_and_requirement_list` | `1410,300 250x430` | 金币、核心材料、普通材料、图纸和前置条件列表。 |
| `upgrade_delta_panel` | `300,760 740x160` | 容量、可用格、负载、下潜许可变化摘要。 |
| `upgrade_action_panel` | `1080,760 580x160` | 升级、追踪材料、暂缓和返回工坊动作。 |

当前已写入 active `screen_layouts.json`，状态为 `active_spec / FormalV1`。

---

## 2. 信息层级

| 层级 | 内容 |
|---|---|
| 最高 | 当前底盘 -> 下一底盘、升级按钮、材料/蓝图是否满足。 |
| 次高 | 容量变化、可用格变化、负载变化、下潜许可影响。 |
| 辅助 | 材料追踪、缺口说明、最高级/未解锁原因。 |

文字、材料数、费用、容量变化和按钮文案由 Unity Text 渲染。

---

## 3. 可复用资源

| VisualID | 用途 |
|---|---|
| `bg_workshop_day` | 工坊背景。 |
| `ui_panel_main` | 升级主容器。 |
| `ui_panel_info` | 当前/下一底盘预览、材料列表、变化摘要和操作区。 |
| `ui_list_row_normal` | 材料需求普通行。 |
| `ui_list_row_selected` | 缺口或重点材料行。 |
| `ui_button_primary` | Upgrade。 |
| `ui_button_secondary` | Track Materials / Close。 |
| `ui_button_danger` | Postpone。 |
| `ui_inventory_chassis_panel` | 底盘预览承托。 |
| `chassis_chassis_lv1_basic_frame` | 当前底盘示例。 |
| `chassis_chassis_lv2_expanded_frame` | 下一底盘示例。 |
| `ui_icon_chassis_upgrade` | 升级收益、容量扩展和主行动符号。 |
| `ui_icon_blueprint` | 蓝图/工程线解锁状态。 |
| `ui_icon_material_need` | 材料不足或缺件提示。 |
| `ui_icon_money` | 金币费用。 |
| `ui_icon_warning` | 未满足、最高级、不可下潜风险。 |

---

## 4. 程序迁移要求

关键路径：

| 路径 | VisualID / 组件 |
|---|---|
| `ChassisUpgradePanel_Runtime/Background_Image` | `bg_workshop_day` |
| `ChassisUpgradePanel_Runtime/UpgradeCard_Image` | `Panel.Main` / `ui_panel_main` |
| `ChassisUpgradePanel_Runtime/UpgradeCard_Image/HeaderPanel/UpgradeIcon_Image` | `Icon.ChassisUpgrade` / `ui_icon_chassis_upgrade` |
| `ChassisUpgradePanel_Runtime/UpgradeCard_Image/HeaderPanel/BlueprintIcon_Image` | `Icon.Blueprint` / `ui_icon_blueprint` |
| `ChassisUpgradePanel_Runtime/UpgradeCard_Image/CurrentChassisPanel/CurrentChassis_Image` | `chassis_chassis_lv1_basic_frame` |
| `ChassisUpgradePanel_Runtime/UpgradeCard_Image/NextChassisPanel/NextChassis_Image` | `chassis_chassis_lv2_expanded_frame` |
| `ChassisUpgradePanel_Runtime/UpgradeCard_Image/RequirementListPanel/RequirementRow_Template` | `List.Row.Normal` / `ui_list_row_normal` |
| `ChassisUpgradePanel_Runtime/UpgradeCard_Image/RequirementListPanel/MissingMaterialIcon_Image` | `Icon.MaterialNeed` / `ui_icon_material_need` |
| `ChassisUpgradePanel_Runtime/UpgradeCard_Image/ActionPanel/Upgrade_Button` | `Button.Primary` / `ui_button_primary` |
| `ChassisUpgradePanel_Runtime/UpgradeCard_Image/ActionPanel/TrackMaterials_Button` | `Button.Secondary` / `ui_button_secondary` |
| `ChassisUpgradePanel_Runtime/UpgradeCard_Image/ActionPanel/Postpone_Button` | `Button.Danger` / `ui_button_danger` |

第一版可以由 `WorkshopUIController` 打开该面板，但升级数据、扣费、材料消耗、底盘切换和下潜许可检查不应写在 UI 中。

---

## 5. 验收标准

1. 底盘升级界面打开后能同时看到当前底盘、下一底盘、容量变化、材料缺口和升级按钮。
2. 材料不足、蓝图未解锁或已最高级状态必须可读，且主按钮禁用时仍能看清原因。
3. 底盘预览不创建第二套背包数据，不接收拖拽，不影响工坊全局背包。
4. 所有名称、数量、费用、按钮文案和说明由 Unity Text 渲染，不烘焙进图片。
5. 背景、面板、图标和底盘预览不阻挡按钮射线。
