---
id: art_ui_formal_v1_maintenance_panel
title: 机体维护整备界面 Formal V1
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
update_rule: 修改维护整备界面正式结构或程序迁移要求时同步本文件。
---

# 机体维护整备界面 Formal V1

> **目标：** 把局外维护从工坊摘要升级为正式整备界面。玩家要能看懂当前人偶状态、磨损/侵蚀、维护费用、材料缺口和是否允许下潜。

---

## 1. Formal V1 结构

```text
Maintenance Panel
  maintenance_background
  maintenance_card
    maintenance_header
    doll_condition_panel
    dive_readiness_panel
    material_cost_list
    wear_corrosion_panel
    repair_action_panel
```

| ZoneID | Rect | 目标 |
|---|---|---|
| `maintenance_background` | `0,0 1920x1080` | 复用工坊背景，形成局外维护场景。 |
| `maintenance_card` | `280,110 1360x860` | 维护主容器。 |
| `maintenance_header` | `340,150 1240x120` | 标题、维护图标、金币和关闭入口。 |
| `doll_condition_panel` | `340,300 520x230` | HP、SAN、Bond、疲劳和核心状态。 |
| `dive_readiness_panel` | `340,550 520x140` | 下潜许可、不可下潜原因和建议处理项。 |
| `material_cost_list` | `340,710 520x210` | 维护消耗、材料缺口和金币费用。 |
| `wear_corrosion_panel` | `900,300 680x280` | 磨损、侵蚀、异常状态和维护后变化预览。 |
| `repair_action_panel` | `900,620 680x300` | 完整维护、使用修复剂、暂缓维护和返回工坊。 |

当前已写入 active `screen_layouts.json`，状态为 `active_spec / FormalV1`。

---

## 2. 信息层级

| 层级 | 内容 |
|---|---|
| 最高 | 下潜许可、关键风险和 Full Repair 主行动。 |
| 次高 | HP / SAN / Bond / 磨损 / 侵蚀状态。 |
| 辅助 | 材料缺口、金币费用、维护后变化预览。 |

文字、数值、材料名、按钮文案都由 Unity Text 渲染，不烘焙进图片。

---

## 3. 可复用资源

| VisualID | 用途 |
|---|---|
| `bg_workshop_day` | 工坊背景。 |
| `ui_panel_main` | 维护主容器和操作面板。 |
| `ui_panel_info` | 状态、费用、风险、材料面板。 |
| `ui_list_row_normal` | 材料/状态列表行。 |
| `ui_button_primary` | Full Repair。 |
| `ui_button_secondary` | Use Repair Kit / Close。 |
| `ui_button_danger` | Postpone / 暂缓维护。 |
| `ui_icon_maintenance` | 维护状态和整备检查图标。 |
| `ui_icon_wear_repair` | 磨损修复、结构补强和修复后变化预览图标。 |
| `ui_icon_corruption_purify` | 侵蚀净化、污染清理和 SAN 风险处理图标。 |
| `ui_icon_dive_permit` | 下潜许可、出发前检查通过和许可恢复图标。 |
| `ui_icon_warning` | 风险、不可下潜、材料不足提示。 |
| `ui_icon_money` | 费用和金币。 |
| `ui_title_divider` | 标题分隔。 |

---

## 4. 程序迁移要求

关键路径：

| 路径 | VisualID / 组件 |
|---|---|
| `MaintenancePanel_Runtime/MaintenanceBackground_Image` | `bg_workshop_day` |
| `MaintenancePanel_Runtime/MaintenanceCard_Image` | `Panel.Main` / `ui_panel_main` |
| `MaintenancePanel_Runtime/MaintenanceCard_Image/HeaderPanel` | `Panel.Info` / `ui_panel_info` |
| `MaintenancePanel_Runtime/MaintenanceCard_Image/HeaderPanel/MaintenanceIcon_Image` | `Icon.Maintenance` / `ui_icon_maintenance` |
| `MaintenancePanel_Runtime/MaintenanceCard_Image/DollConditionPanel/WarningIcon_Image` | `Icon.Warning` / `ui_icon_warning` |
| `MaintenancePanel_Runtime/MaintenanceCard_Image/DiveReadinessPanel/ReadinessIcon_Image` | `Icon.DivePermit` / `ui_icon_dive_permit` |
| `MaintenancePanel_Runtime/MaintenanceCard_Image/WearCorrosionPanel/WearRepairIcon_Image` | `Icon.WearRepair` / `ui_icon_wear_repair` |
| `MaintenancePanel_Runtime/MaintenanceCard_Image/WearCorrosionPanel/CorruptionPurifyIcon_Image` | `Icon.CorruptionPurify` / `ui_icon_corruption_purify` |
| `MaintenancePanel_Runtime/MaintenanceCard_Image/MaterialCostListPanel/CostRow_Template` | `List.Row.Normal` / `ui_list_row_normal` |
| `MaintenancePanel_Runtime/MaintenanceCard_Image/RepairActionPanel/FullRepair_Button` | `Button.Primary` / `ui_button_primary` |
| `MaintenancePanel_Runtime/MaintenanceCard_Image/RepairActionPanel/Postpone_Button` | `Button.Danger` / `ui_button_danger` |

第一版可以由 `WorkshopUIController` 打开该面板，但维护数据和扣费逻辑不应写在 UI 中。

---

## 5. 验收标准

1. 维护界面打开后能同时看到状态、磨损/侵蚀、费用、材料缺口和下潜许可。
2. Full Repair 是最明显主行动，暂缓维护作为危险动作清楚可见。
3. 磨损修复、侵蚀净化、下潜许可、警告、维护、金币图标按 contain 显示，不遮挡按钮或列表。
4. 背景、面板和装饰不拦截按钮射线。
5. 图片中不包含文字、数字、材料名或按钮文案。
