---
id: art_ui_formal_v2_prosthetic_panel
title: Prosthetic Panel Formal V2 方案
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v1/prosthetic_panel_v1.md
last_verified: 2026-06-01
update_rule: 编写或确认 prosthetic_panel Formal V2 详细方案时同步本文件。
---

# Prosthetic Panel Formal V2 方案

> **状态：** Formal V2 设计草案。本文用于确认 `prosthetic_panel` 的正式化方向；确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`prosthetic_panel` Formal V1 已能展示配方行、材料和 Craft / Equipped 状态，但仍像制作列表：

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 义体缺少身体关系 | 列表行说明槽位，玩家不直观看到装到哪里 | 右侧固定魔偶改造椅和槽位预览。 |
| 配方列表过重 | 每行堆图标、槽位、材料、按钮 | 左侧只做义体柜选择，详情集中到中间工作托盘。 |
| Craft / Equip 行为混杂 | 玩家难区分制造、装备、已装备 | 当前选中义体决定主行动：制造、装备或已装备。 |
| 材料缺口不直观 | 读材料数量才能理解 | 材料以 3-5 个 token 表达，缺口高亮。 |

---

## 2. 玩家目标

玩家进入义体界面时，目标是：

1. 找到适合当前魔偶和下潜目标的义体。
2. 判断该义体是否已拥有、可制造、可装备。
3. 看懂它会影响哪个槽位和哪些能力。
4. 制造或装备当前选中的义体。

---

## 3. Formal V2 体验定位

`prosthetic_panel` 是“义体柜 + 改造椅”，不是配方表。

```text
义体柜
  -> 选中一个义体
  -> 工作托盘展示材料与效果
  -> 魔偶改造椅展示槽位
  -> 制造 / 装备
```

视觉目标：

* 义体像陈列在柜子中的手工器械。
* 魔偶坐在机械感但不冷硬的改造椅上。
* 玩家一次只评估一个义体，减少列表噪声。
* 蒸汽工艺只作为黄铜、玻璃、齿轮、皮革细节存在。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Header: prosthetic title / owned / close                   │
│                                                            │
│ Prosthetic Cabinet     Work Tray          Doll Chair        │
│ selectable parts       selected part      slot preview      │
│                        materials/effect   equipped marks    │
│                                                            │
│ Material Tokens        Effect Preview     Primary Action    │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `prosthetic_background` | `0,0 1920x1080` | 工作室背景。 |
| `prosthetic_cabinet` | `160,160 480x720` | 义体候选，行只显示图标、名称和状态。 |
| `selected_prosthetic_tray` | `690,180 520x500` | 当前义体大图、槽位、功能摘要、材料。 |
| `doll_mod_chair` | `1260,160 500x620` | 魔偶改造椅、可装备槽位和已装备提示。 |
| `prosthetic_material_row` | `700,720 500x120` | 当前义体材料 token。 |
| `prosthetic_action_panel` | `1260,810 500x120` | 制造 / 装备主行动和返回。 |

---

## 5. 信息层级

默认阅读顺序：

1. 当前选中的义体是什么。
2. 它装在哪个槽位，当前是否已装备。
3. 能否制造或装备。
4. 材料缺口和效果摘要。
5. 其他义体候选。

不默认展示：

* 所有义体完整材料公式。
* 每行 Craft 按钮。
* 长效果描述。
* 多个槽位的完整内部数据。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `CraftSelected` / `EquipSelected` | 根据选中义体状态切换，一个主按钮。 |
| Secondary | 切换义体、返回工作室、查看材料来源 | 左侧选择和小按钮。 |
| Tertiary | 筛选槽位、排序、查看详细效果 | 图标化工具。 |
| Danger | 拆卸或替换高风险义体 | 后续若有，必须二次确认。 |

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| 每行 Craft | 移到选中详情区，变成唯一主行动。 |
| Equipped 行内状态 | 改为魔偶槽位标记 + 选中义体状态。 |
| Close | 低权重返回工作室。 |
| 材料详情 | 折叠到材料 token 的详情提示。 |
| 列表行操作按钮 | 默认移除，列表只负责选择。 |

---

## 8. 场景隐喻

义体界面像一个小型改造室：

* 左边是带玻璃门的义体柜，义体像工具或收藏物摆放。
* 中间是木质 / 黄铜工作托盘，展示当前选中部件。
* 右边是魔偶坐在机械改造椅上，槽位用柔和光点标注。
* 整体应有手工维修与奇幻器械感，不做科幻实验室。

---

## 9. 程序迁移影响

确认后建议层级：

```text
ProstheticPanel
  ProstheticBackground
  ProstheticCabinet
  SelectedProstheticTray
  DollModChair
  ProstheticMaterialRow
  ProstheticActionPanel
```

关键绑定：

| 绑定 | 来源 |
|---|---|
| 义体候选 | Prosthetic 配置 / 已解锁配方。 |
| 当前装备 | DollGrowth / ProstheticInstance 只读快照。 |
| 材料缺口 | Inventory / CraftingService 只读检查。 |
| 制造 / 装备 | 领域服务执行，UI 不直接改状态。 |

---

## 10. 素材需求变化

第一版复用：

| VisualID | 用法 |
|---|---|
| `bg_workshop_day` | 工作室背景。 |
| `doll_proto_0_stand` | 魔偶临时展示。 |
| `prosthetic_*_icon` | 义体候选和选中大图。 |
| `ui_panel_main` / `ui_panel_info` | 柜子、工作托盘、槽位面板。 |
| `ui_icon_equipped` | 已装备标记。 |
| `ui_icon_material_need` / `ui_icon_money` | 材料和费用。 |
| `ui_button_primary` / `ui_button_secondary` | 制造 / 装备 / 返回。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `ui_prosthetic_cabinet_frame` | 义体柜框体。 |
| `ui_prosthetic_work_tray` | 当前义体工作托盘。 |
| `ui_doll_mod_chair_frame` | 魔偶改造椅 UI / 背景层。 |
| `ui_prosthetic_slot_marker` | 身体槽位标记。 |

---

## 11. UX 验收标准

运行时截图需要满足：

1. 3 秒内能看出这是义体改造界面。
2. 一次只强调一个选中义体。
3. 魔偶槽位关系清楚，不只靠文字说明。
4. 主按钮根据状态明确是制造、装备或不可执行。
5. 材料缺口不需要读长列表即可理解。
6. 列表行不堆多个按钮。

---

## 12. 用户确认问题

建议确认以下 3 点：

1. `prosthetic_panel` 是否采用“左义体柜 + 中工作托盘 + 右魔偶改造椅”的结构。
2. 是否取消每行 Craft 按钮，改为选中义体后统一主行动。
3. 魔偶槽位是否作为右侧视觉重点，而不是仅在列表中显示槽位文字。
