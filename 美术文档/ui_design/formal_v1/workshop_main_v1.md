---
id: art_ui_formal_v1_workshop_main
title: 工坊主界面 Formal V1
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: true
related:
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/ui_design/formal_v2/01_workshop_main_v2.md
last_verified: 2026-05-23
update_rule: 修改工坊主界面正式结构或程序迁移要求时同步本文件。
---

# 工坊主界面 Formal V1

> **目标：** 把工坊从 MVP 的功能按钮堆叠，改为正式局外工作台：魔偶维护、背包装配、经济压力、出发准备和功能入口都围绕“修理工坊”展开。

---

## 1. MVP Baseline 问题

| 项目 | MVP 结构 | 问题 |
|---|---|---|
| 顶部状态 | 超长金币条 | 像调试状态条，信息密度和视觉比例不正式。 |
| 左侧按钮 | 功能按钮纵向堆叠 | 可用但像菜单，不像工坊工作流。 |
| 魔偶展示 | 右侧独立立绘 | 可保留，但缺少维护台/状态关系。 |
| 背包 | 中下显示格子 | 与工坊整备关系还不够清晰。 |
| 底盘信息 | 左下文字块 | 信息层级弱，后续扩展困难。 |

---

## 2. Formal V1 结构

```text
┌────────────────────────────────────────────────────────────┐
│ Day / Rent / Money / SAN / Doll Condition                  │
│                                                            │
│  Workshop Actions        Doll Maintenance Bay              │
│  Start Expedition        Doll stand + condition summary     │
│  Sell / Prosthetics      Repair / prosthetic hooks          │
│                                                            │
│  Chassis & Backpack Workbench                              │
│  100x100 grid + equipped items + chassis summary            │
│                                                            │
│  Context Hint / Current Pressure / Next Objective           │
└────────────────────────────────────────────────────────────┘
```

### 主区域

| ZoneID | Rect | 目标 |
|---|---|---|
| `workshop_background` | `0,0 1920x1080` | 工坊背景，cover，低抢占。 |
| `status_cluster` | `64,32 760x84` | 金币、天数、房租压力、SAN/维护摘要，紧凑化。 |
| `expedition_panel` | `88,170 360x360` | 出发深渊主入口和风险摘要。 |
| `service_panel` | `88,550 360x260` | 出售、义体制造、维修等次级入口。 |
| `doll_bay` | `1120,140 540x760` | 魔偶展示和维护状态。 |
| `backpack_workbench` | `520,590 560x420` | 背包底盘、格子、已装备物品。 |
| `chassis_summary` | `520,450 560x120` | 当前底盘、容量、负载和升级提示。 |
| `bottom_hint` | `88,870 1000x120` | 当前任务、说明、警告。 |

---

## 3. 设计口径

1. “出发深渊”是主行动，视觉优先级最高。
2. “出售”和“义体制造”是工坊服务，不应和出发按钮等权。
3. 背包应像工作台上的装配区，不是悬浮调试网格。
4. 顶部状态区要紧凑，避免横跨全屏。
5. 魔偶展示应成为局外情感和维护核心，后续可挂载好感、损伤、义体槽位提示。

---

## 4. 可复用资源

| VisualID | 用途 |
|---|---|
| `bg_workshop_day` | 工坊背景。 |
| `doll_proto_0_stand` | 魔偶展示。 |
| `ui_panel_info` | 信息承托。 |
| `ui_panel_main` | 可用于工坊工作台主框。 |
| `ui_button_primary` | 出发深渊。 |
| `ui_button_secondary` | 出售、义体、维修。 |
| `ui_inventory_chassis_panel` | 背包底盘。 |
| `ui_inventory_slot_available` | 背包格。 |
| `ui_icon_money` | 金币状态。 |

---

## 5. 程序迁移要求

优先调整表现层锚点和层级，不改变工坊业务逻辑。

建议新增或整理节点：

```text
WorkshopPanel
  WorkshopBackground_Image
  StatusCluster
  ExpeditionPanel
  ServicePanel
  DollBay
  ChassisSummaryPanel
  BackpackWorkbenchAnchor
  BottomHintPanel
```

背包仍复用全局 `GridContainer` 和 `InventoryItemLayer`，锚到 `BackpackWorkbenchAnchor`。

---

## 6. 验收标准

1. 顶部状态区不再横跨全屏，金币、天数和压力信息清晰。
2. 出发深渊是最明显主行动。
3. 出售、义体制造是次级服务入口。
4. 魔偶展示、背包工作台、工坊入口互不重叠。
5. 背包格仍为 100x100，物品显示和交互不受影响。
6. 背景和装饰不拦截按钮或背包射线。
