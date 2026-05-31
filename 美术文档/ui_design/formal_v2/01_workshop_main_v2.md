---
id: art_ui_formal_v2_workshop_main
title: Workshop Main Formal V2 方案
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v1/workshop_main_v1.md
last_verified: 2026-05-31
update_rule: 编写或确认 workshop_main Formal V2 详细方案时同步本文件。
---

# Workshop Main Formal V2 方案

> **状态：** Formal V2 设计草案。本文用于给用户确认 `workshop_main` 的正式化方向；确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`workshop_main` Formal V1 已经把工坊拆成状态区、出发区、服务区、魔偶区和背包工作台，但运行时仍容易退回“功能按钮菜单”：

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 主行动不够像空间入口 | 出发和出售、维护、义体等入口视觉权重接近 | 把下潜入口做成工坊里的升降机 / 深渊门，成为唯一最高权重行动。 |
| 服务入口像按钮列表 | 出售、订单、传闻、账单、维护等平铺 | 改为工坊空间热点：工作台、维护舱、账本柜台、委托板。 |
| 魔偶和背包关系弱 | 魔偶展示和背包格像两个独立控件 | 让魔偶维护舱和背包工作台共享“整备状态”语义。 |
| 状态信息像调试条 | 金币、日期、房租、SAN、维护状态横向堆 | 收敛为压力轨，突出“今天是否适合下潜”。 |

---

## 2. 玩家目标

玩家打开工坊主界面时，真实目标不是“找按钮”，而是决定下一步做什么：

1. 我现在能不能下潜。
2. 下潜前是否必须维护、整理背包或处理账单压力。
3. 我该去工作台、维护舱、市场账本还是深渊入口。

因此 V2 的默认状态应让玩家先看到“出发准备是否成立”，再看到可修正问题的空间入口。

---

## 3. Formal V2 体验定位

`workshop_main` 是局外主 Hub，不是所有功能的全展开面板。

```text
工坊空间
  -> 今日压力轨
  -> 深渊入口主行动
  -> 魔偶维护舱
  -> 背包 / 底盘工作台
  -> 市场账本 / 委托板
```

视觉目标：

* 第一眼像一个可操作的工坊，而不是后台菜单。
* 主行动是“下潜 / 整备完成后出发”。
* 其他功能是空间入口，点击后进入子界面。
* 背包保持 `100x100` 玩法格，不因 Hub 构图缩小。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Pressure Rail: Day / Money / Rent / Doll Risk / Dive Ready │
│                                                            │
│ Abyss Gate        Prep Desk / Current Task       Doll Bay   │
│ primary action    next objective and warnings    condition  │
│                                                            │
│ Market Ledger     Backpack Workbench             Doll Stage │
│ orders / rumors   100x100 grid, chassis          emotions   │
│                                                            │
│ Context Strip: selected hotspot detail and secondary action │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `workshop_scene` | `0,0 1920x1080` | 工坊背景，表达空间，不承载文字。 |
| `pressure_rail` | `64,32 1792x72` | 今日、金币、月租压力、魔偶状态、下潜许可摘要。 |
| `abyss_gate` | `72,142 420x470` | 主行动区，升降机 / 深渊门 / 出发确认。 |
| `prep_summary` | `520,132 620x240` | 当前最需要处理的问题：维护、背包、账单、订单。 |
| `doll_bay` | `1260,132 560x560` | 魔偶立绘、维护风险、情绪 / SAN 摘要。 |
| `backpack_workbench` | `520,430 680x520` | 背包和底盘工作台，格子保持 `100x100`。 |
| `market_ledger` | `72,650 420x300` | 出售、订单、传闻、账本入口，低于深渊入口权重。 |
| `context_strip` | `1260,720 560x230` | 当前选中空间热点的详情和次行动。 |

---

## 5. 信息层级

默认信息顺序：

1. `DiveReady`：能否下潜，以及阻塞原因。
2. `CurrentPressure`：金币、月租、日数、魔偶风险。
3. `NextFix`：如果不能下潜，最推荐处理什么。
4. `InventoryCapacity`：背包容量和关键物品状态。
5. `SecondaryOpportunities`：订单、传闻、出售收益、义体制造。

避免默认展开：

* 不默认显示所有订单。
* 不默认显示所有传闻。
* 不默认显示全部维护项目明细。
* 不默认显示完整底盘 / 义体配方列表。

这些信息进入对应子界面或 `context_strip`。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `StartDive` / `OpenLayerSelect` | `abyss_gate` 内唯一最高权重按钮。 |
| Secondary | 整理背包、维护、出售、订单、传闻、义体 | 空间热点 + `context_strip` 次按钮。 |
| Tertiary | 日志、详情、筛选、帮助 | 图标按钮或小入口。 |
| Danger | 典当补款、放弃订单、黑市交易 | 不在默认 Hub 直接执行，进入子界面后二次确认。 |

主行动规则：

* 默认可见最高权重按钮只有一个。
* 若下潜被阻塞，主按钮显示不可执行状态，`prep_summary` 给出第一个修正入口。
* 次级入口不能使用和主行动同等尺寸 / 颜色的按钮。

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| 出发 / 层选择 | 合并为 `abyss_gate` 主入口，点击后进入 `layer_select`。 |
| 维护 | 降级为 `doll_bay` 热点；风险严重时在 `prep_summary` 提升为推荐修正。 |
| 义体 / 底盘 / 背包 | 合并到 `backpack_workbench` 和工作台热点，进入对应子界面。 |
| 出售 / 订单 / 传闻 / 账单 | 收敛到 `market_ledger`，默认只显示压力摘要和入口。 |
| 人偶互动 / 房间 | 从魔偶维护舱进入，不和下潜主行动并列。 |
| 测试 / debug 入口 | Formal V2 默认隐藏，不出现在正式截图验收中。 |

---

## 8. 场景隐喻

`workshop_main` 的视觉隐喻是“深渊边缘的维修工坊”：

* 左侧是通向深渊的升降机或铁门，承担主行动。
* 中央是铺开的工作台，承担背包、底盘、义体和材料。
* 右侧是魔偶维护舱，承担角色状态和情感入口。
* 左下是账本 / 出货柜台 / 委托板，承担经济压力。

AI 或后续美术资产提示词应使用这些可视描述，不使用“功能菜单”“UGUI 可读性”等程序词。

---

## 9. 程序迁移影响

Formal V2 确认后，程序侧主要是 UGUI 层级重排，不应改写玩法规则：

```text
WorkshopPanel
  WorkshopScene
  PressureRail
  AbyssGate
    StartDiveButton
    DiveReadinessSummary
  PrepSummary
  DollBay
  BackpackWorkbench
    GridContainer
    InventoryItemLayer
  MarketLedger
  ContextStrip
```

程序绑定建议：

| 绑定 | 来源 |
|---|---|
| `DiveReady` / 阻塞原因 | `DiveReadinessService` / 只读快照。 |
| 维护风险 | `MaintenanceService` / `GrowthFeedbackService`。 |
| 背包容量 | 现有背包 / DisplaySpec。 |
| 经济压力 | `TownEconomyOverviewService`。 |
| 魔偶状态 | `DollCoreStateReadabilityService`。 |

约束：

* UI Controller 只展示快照和打开子界面，不直接扣金币、改背包、改 HP / SAN。
* 背包仍复用全局 `GridContainer` 和 `InventoryItemLayer`。
* `AbyssGate`、`DollBay`、`MarketLedger` 可用 Button / Hotspot 实现，但视觉上不能是普通按钮列。

---

## 10. 素材需求变化

第一版尽量复用现有素材：

| VisualID | 用法 |
|---|---|
| `bg_workshop_day` | 工坊背景，后续可同名替换为更正式 Hub 背景。 |
| `doll_proto_0_stand` | 魔偶维护舱临时展示。 |
| `ui_panel_main` / `ui_panel_info` | 压力轨、准备摘要、上下文条。 |
| `ui_button_primary` / `ui_button_secondary` | 主行动和次行动。 |
| `ui_inventory_chassis_panel` / `ui_inventory_slot_*` | 背包工作台。 |
| `ui_icon_money` / `ui_icon_warning` | 压力轨和阻塞提示。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `ui_workshop_abyss_gate_frame` | 深渊入口 / 升降机主行动框。 |
| `ui_workshop_hotspot_marker` | 空间热点标记。 |
| `ui_workshop_pressure_rail` | 顶部压力轨专用底板。 |

这些新增项只有在用户确认 V2 结构并写入 active 后，才进入 seed / Manifest / Prompt。

---

## 11. UX 验收标准

运行时截图需要满足：

1. 3 秒内能看出这是工坊 Hub，不是按钮菜单。
2. “下潜 / 出发”是唯一最高权重行动。
3. 维护、背包、市场账本、魔偶互动是空间入口，不和主行动等权。
4. 顶部压力信息紧凑，不横向压满成调试条。
5. 背包工作台格子仍为 `100x100`，物品拖拽层不被装饰遮挡。
6. 中文文本不压边、不和按钮重叠。
7. debug / 验收专用入口不出现在正式截图。

---

## 12. 用户确认问题

建议优先确认以下 3 点：

1. `workshop_main` 是否采用“左侧深渊入口、中央工作台、右侧魔偶维护舱、左下账本柜台”的空间骨架。
2. 下潜是否作为工坊默认唯一主行动，维护 / 出售 / 订单等都降级为空间入口。
3. 是否允许第一版继续复用现有背景和 UI 皮肤，只先验证结构，后续再补专用工坊热点素材。
