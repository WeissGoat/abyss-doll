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
last_verified: 2026-06-01
update_rule: 编写或确认 workshop_main Formal V2 详细方案时同步本文件。
---

# Workshop Main Formal V2 方案

> **状态：** Formal V2 设计草案。本文用于给用户确认 `workshop_main` 的正式化方向；确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`workshop_main` Formal V1 已经把工坊拆成状态区、出发区、服务区、魔偶区和背包工作台，但运行时仍容易退回“功能按钮菜单”，并且主界面承担了过多功能：

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 主行动不够像空间入口 | 出发和出售、维护、义体等入口视觉权重接近 | 把下潜入口做成工坊里的升降机 / 深渊门，成为唯一最高权重行动。 |
| 服务入口像按钮列表 | 出售、订单、传闻、账单、维护等平铺 | 改为房间热点和子界面入口：工作室、账本角、深渊门、魔偶互动。 |
| 主界面不够像家 | 背包、工作台、账本、维护都挤在主界面 | 主界面改为以魔偶为中心的安心房间，复杂操作拆到工作室。 |
| 魔偶和背包关系弱 | 魔偶展示和背包格像两个独立控件 | 背包和改造移到 `workshop_studio`，主界面只保留魔偶状态与房间热点。 |
| 状态信息像调试条 | 金币、日期、房租、SAN、维护状态横向堆 | 收敛为压力轨，突出“今天是否适合下潜”。 |

---

## 2. 玩家目标

玩家打开工坊主界面时，真实目标不是“找按钮”，而是决定下一步做什么：

1. 我现在能不能下潜。
2. 下潜前是否必须维护、整理背包或处理账单压力。
3. 我该去工作室、市场账本、魔偶互动还是深渊入口。

因此 V2 的默认状态应让玩家先看到“出发准备是否成立”，再看到可修正问题的空间入口。

---

## 3. Formal V2 体验定位

`workshop_main` 是局外主 Hub，也是玩家回到深渊之外的“家”。它不再承载完整背包和改造操作。

```text
安心房间
  -> 魔偶中心展示
  -> 深渊入口主行动
  -> 工作室入口
  -> 市场 / 账本低权重入口
  -> 少量今日状态
```

视觉目标：

* 第一眼像一个温暖、可居住、让人安心的房间，而不是后台菜单。
* 魔偶是画面中心，玩家先感受到角色状态和陪伴感。
* 主行动是“下潜 / 整备完成后出发”。
* 其他功能是少量空间热点，点击后进入子界面。
* 背包、底盘、义体、改造椅拆到 `workshop_studio`，主界面不显示完整背包格。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Light Status Strip: day / money / rent / doll mood         │
│                                                            │
│ Cozy Room Scene                                            │
│                   Doll Center                              │
│                                                            │
│ Workshop Hotspot       Abyss Door / Lift      Ledger Corner │
│ small entry            primary action         small entry   │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `home_room_scene` | `0,0 1920x1080` | 温暖房间背景，木质、布艺、植物、窗光和少量深渊器械。 |
| `light_status_strip` | `64,32 1792x72` | 今日、金币、月租压力、魔偶状态，用图标化方式呈现。 |
| `doll_center` | `620,210 680x610` | 魔偶作为视觉中心，展示情绪、状态和互动入口。 |
| `abyss_door` | `1240,180 500x520` | 深渊门 / 升降机 / 洞口，主行动热点。 |
| `workshop_entry` | `160,560 360x260` | 工作室入口，进入背包、底盘、义体、改造椅。 |
| `ledger_corner` | `1380,720 340x220` | 市场、账本、订单的低权重入口。 |

---

## 5. 信息层级

默认信息顺序：

1. 魔偶当前状态和情绪。
2. 是否可以下潜，以及是否有明显阻塞。
3. 今日压力摘要：金币、日期、月租。
4. 工作室 / 账本 / 市场等低权重入口。

避免默认展开：

* 不默认显示背包格。
* 不默认显示所有订单、传闻、账单明细。
* 不默认显示全部维护项目、底盘 / 义体配方列表。

这些信息进入对应子界面、轻量浮层或房间热点提示。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `StartDive` / `OpenLayerSelect` | `abyss_gate` 内唯一最高权重按钮。 |
| Secondary | 工作室、账本、市场、魔偶互动 | 空间热点 + 小图标入口。 |
| Tertiary | 日志、详情、筛选、帮助 | 图标按钮或小入口。 |
| Danger | 典当补款、放弃订单、黑市交易 | 不在默认 Hub 直接执行，进入子界面后二次确认。 |

主行动规则：

* 默认可见最高权重按钮只有一个。
* 若下潜被阻塞，主按钮显示不可执行状态，轻量状态提示给出第一个修正入口。
* 次级入口不能使用和主行动同等尺寸 / 颜色的按钮。

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| 出发 / 层选择 | 合并为 `abyss_gate` 主入口，点击后进入 `layer_select`。 |
| 维护 | 默认从魔偶互动或工作室进入；严重风险才用状态图标提示。 |
| 义体 / 底盘 / 背包 | 不在主界面展开，统一进入 `workshop_studio`。 |
| 出售 / 订单 / 传闻 / 账单 | 收敛到 `ledger_corner`，默认只显示入口。 |
| 人偶互动 / 房间 | 以魔偶中心和房间热点表达，不和下潜主行动并列。 |
| 测试 / debug 入口 | Formal V2 默认隐藏，不出现在正式截图验收中。 |

---

## 8. 场景隐喻

`workshop_main` 的视觉隐喻是“深渊边缘的家”：

* 中央是魔偶所在的房间中心，承担情感和状态。
* 一侧是通向深渊的门或升降机，承担主行动。
* 另一侧是工作室门、工具角或楼梯，承担改造入口。
* 角落保留账本 / 委托板 / 出货箱，承担经济压力。
* 木质、布艺、植物、暖光和小收藏物应多于硬冷机械。

`workshop_studio` 的视觉隐喻是“专门的改造工作室”：

* 左侧是打开的背包和物品格。
* 右侧是魔偶坐在带黄铜机械臂的改造椅上。
* 中间是少量选中部件对比和确认操作。

AI 或后续美术资产提示词应使用这些可视描述，不使用“功能菜单”“UGUI 可读性”等程序词。

---

## 9. 程序迁移影响

Formal V2 确认后，程序侧主要是 UGUI 层级重排，不应改写玩法规则：

```text
WorkshopPanel
  HomeRoomScene
  LightStatusStrip
  DollCenter
  AbyssDoor
    StartDiveButton
  WorkshopEntry
  LedgerCorner
```

程序绑定建议：

| 绑定 | 来源 |
|---|---|
| `DiveReady` / 阻塞原因 | `DiveReadinessService` / 只读快照。 |
| 维护风险 | `MaintenanceService` / `GrowthFeedbackService`。 |
| 经济压力 | `TownEconomyOverviewService`。 |
| 魔偶状态 | `DollCoreStateReadabilityService`。 |

约束：

* UI Controller 只展示快照和打开子界面，不直接扣金币、改背包、改 HP / SAN。
* `AbyssDoor`、`DollCenter`、`WorkshopEntry`、`LedgerCorner` 可用 Button / Hotspot 实现，但视觉上不能是普通按钮列。

---

## 10. 素材需求变化

第一版尽量复用现有素材：

| VisualID | 用法 |
|---|---|
| `bg_workshop_day` | 工坊背景，后续可同名替换为更正式 Hub 背景。 |
| `doll_proto_0_stand` | 魔偶维护舱临时展示。 |
| `ui_panel_main` / `ui_panel_info` | 轻状态条、热点浮层和少量提示。 |
| `ui_button_primary` / `ui_button_secondary` | 主行动和次行动。 |
| `ui_inventory_chassis_panel` / `ui_inventory_slot_*` | `workshop_studio` 的左侧背包和改造工作区。 |
| `ui_icon_money` / `ui_icon_warning` | 压力轨和阻塞提示。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `ui_workshop_abyss_gate_frame` | 深渊入口 / 升降机主行动框。 |
| `ui_workshop_hotspot_marker` | 空间热点标记。 |
| `ui_workshop_pressure_rail` | 顶部压力轨专用底板。 |
| `bg_workshop_home_room` | 更正式的安心房间主界面背景。 |
| `bg_workshop_studio` | 专门的改造工作室背景。 |
| `ui_workshop_studio_chair` | 魔偶改造椅 / 机械臂焦点素材。 |

这些新增项只有在用户确认 V2 结构并写入 active 后，才进入 seed / Manifest / Prompt。

---

## 11. UX 验收标准

运行时截图需要满足：

1. 3 秒内能看出这是工坊 Hub，不是按钮菜单。
2. 3 秒内能感受到这是安全、温暖的房间，不是硬核工业控制台。
3. 魔偶是主界面视觉中心。
4. “下潜 / 出发”是唯一最高权重行动。
5. 工作室、市场账本、魔偶互动是空间入口，不和主行动等权。
4. 顶部压力信息紧凑，不横向压满成调试条。
5. 主界面不显示完整背包格；背包格留给 `workshop_studio`。
6. 中文文本不压边、不和按钮重叠。
7. debug / 验收专用入口不出现在正式截图。

---

## 12. 用户确认问题

建议优先确认以下 3 点：

1. `workshop_main` 是否采用“魔偶为中心的安心房间 + 少量空间入口”的空间骨架。
2. 下潜是否作为工坊默认唯一主行动，维护 / 出售 / 订单等都降级为空间入口。
3. 是否将背包、底盘、义体、改造椅拆到 `workshop_studio` 子界面承接。
