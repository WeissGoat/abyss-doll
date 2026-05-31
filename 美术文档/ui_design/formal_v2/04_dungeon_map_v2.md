---
id: art_ui_formal_v2_dungeon_map
title: Dungeon Map Formal V2 方案
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v1/dungeon_map_v1.md
last_verified: 2026-05-31
update_rule: 编写或确认 dungeon_map Formal V2 详细方案时同步本文件。
---

# Dungeon Map Formal V2 方案

> **状态：** Formal V2 设计草案。本文用于给用户确认 `dungeon_map` 的正式化方向；确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`dungeon_map` Formal V1 已有路线、节点、选中详情和背包整理入口，但仍可能偏“节点按钮图”：

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 路线决策感弱 | 节点像一组可点按钮 | 把地图变成路线规划板，强化分支、风险和目的地。 |
| 选中详情弱 | 节点说明可能只是文本框 | 选中节点详情必须解释风险、奖励、状态和进入后果。 |
| 地图与背包关系弱 | 背包整理入口和进入节点并列 | 背包整理降级为整备辅助，不和进入节点同权。 |
| 战争迷雾不够分层 | 已知 / 预览 / 未知状态不明显 | 建立清晰雾层、预览层和锁定层。 |

---

## 2. 玩家目标

玩家在深渊地图界面的目标是：

1. 判断下一步走哪条路线。
2. 理解每个可达节点的风险、奖励和未知程度。
3. 决定是否进入节点、整理背包或撤离。
4. 保持对当前层级目标和出口方向的理解。

---

## 3. Formal V2 体验定位

`dungeon_map` 是路线决策界面，不是节点按钮菜单。

```text
当前层级 -> 路线网络 -> 选中节点详情 -> 进入节点主行动
```

视觉目标：

* 路线网络是主视觉。
* 可走、预览、未知、锁定的节点状态一眼可辨。
* 选中节点后，详情区解释“为什么要去 / 为什么危险”。
* 背包整理、撤离、层级信息是辅助，不抢进入节点的权重。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Layer Header: layer, depth, route seed, retreat state       │
│                                                            │
│ Route Canvas                                  Node Detail   │
│ branching paths, fog, current node            risk/reward   │
│                                                enter action │
│                                                            │
│ Route Legend / Backpack Pressure / Retreat                 │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `layer_header` | `64,36 1792x80` | 当前层、深度、路线状态、已通层提示。 |
| `route_canvas` | `80,150 1240x760` | 节点网络和路线连线主视觉。 |
| `selected_node_detail` | `1360,150 480x560` | 选中节点详情、风险、奖励、状态。 |
| `map_action_panel` | `1360,740 480x170` | 进入节点主行动、撤离 / 背包整理次行动。 |
| `route_legend` | `80,930 1240x80` | 节点状态图例、战争迷雾说明、背包压力短提示。 |

---

## 5. 信息层级

默认阅读顺序：

1. 当前层级和当前位置。
2. 可达路线和可见分支。
3. 选中节点风险 / 奖励 / 节点类型。
4. 进入节点主行动。
5. 背包压力、撤离和已通层直达等辅助信息。

节点牌只显示：

* 节点图标。
* 风险等级或未知状态。
* 当前 / 可达 / 已探索 / 锁定状态。

文字解释进入 `selected_node_detail`。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `EnterSelectedNode` | 详情区下方唯一主按钮。 |
| Secondary | 整理背包、查看路线、撤离 / 返回安全区 | 低权重按钮。 |
| Tertiary | 图例、种子信息、筛选 | 图标或小文本入口。 |
| Danger | 撤离、进入高危未知节点 | 视觉标记风险，必要时二次确认。 |

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| 每个节点按钮 | 节点仍可点击，但视觉上是地图对象，不是普通按钮。 |
| 进入节点 | 只在选中详情区显示一个主行动。 |
| 背包整理 | 降级为辅助入口，提示背包压力时才提升。 |
| 撤离 | 可见但低权重；若有损失或阶段影响，进入确认。 |
| 层级切换 | 放到 `layer_header` 或 `layer_select`，不在地图主视觉中平铺。 |

---

## 8. 场景隐喻

地图界面像一张贴在工坊或探险手册上的深渊路线图：

* 路线线段像探索线索。
* 未知节点被雾遮住。
* 当前节点有清晰定位。
* 选中详情像旁边的勘探记录卡。

AI 或后续美术提示应描述 map board, branching route, fogged nodes, brass markers, parchment/mechanical chart 等可视概念。

---

## 9. 程序迁移影响

确认后建议层级：

```text
DungeonMapPanel
  LayerHeader
  RouteCanvas
    RouteLines
    NodeButtons
    FogLayer
  SelectedNodeDetail
  MapActionPanel
  RouteLegend
```

关键绑定：

| 绑定 | 来源 |
|---|---|
| 路线网络 | `DungeonManager` / `DungeonMapVisibilityService`。 |
| 节点状态 | 可达、已探索、预览、隐藏、锁定。 |
| 风险提示 | 节点配置 `RiskLevel` / `RiskHint`。 |
| 奖励预览 | 节点类型和 Reward 引用。 |
| 进入节点 | 只调用领域层移动 / 进入接口。 |

约束：

* 节点点击和路线可视层分离，路线线段不拦截节点点击。
* 详情区只展示选中节点，不在每个节点上堆长文本。
* 进入节点按钮不可在未选中或不可达时误触。

---

## 10. 素材需求变化

第一版复用：

| VisualID | 用法 |
|---|---|
| `bg_dungeon_map` | 地图背景。 |
| `ui_dungeon_node_plate` | 节点底板。 |
| `ui_dungeon_route_line` | 路线线段。 |
| `node_*_icon` | 节点类型图标。 |
| `ui_panel_info` | 选中节点详情和图例。 |
| `ui_button_primary` / `ui_button_secondary` / `ui_button_danger` | 进入、整理、撤离。 |
| `ui_icon_locked` / `ui_icon_warning` | 锁定和风险提示。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `ui_dungeon_current_node_marker` | 当前节点定位标记。 |
| `ui_dungeon_fog_overlay` | 战争迷雾覆盖层。 |
| `ui_dungeon_risk_badge` | 风险等级小徽记。 |

---

## 11. UX 验收标准

运行时截图需要满足：

1. 3 秒内能看出当前任务是选下一条路线。
2. 路线网络是主视觉，不是按钮列表。
3. 当前、可达、预览、隐藏、锁定状态可辨。
4. 选中节点详情能解释风险、奖励和进入后果。
5. 进入节点是唯一主行动。
6. 背包整理和撤离不抢主行动权重。
7. 路线、雾层、节点点击不会互相遮挡。

---

## 12. 用户确认问题

建议确认以下 3 点：

1. `dungeon_map` 是否采用“路线图主视觉 + 右侧节点详情 + 进入节点主行动”的结构。
2. 背包整理是否降级为辅助入口，只在容量压力明显时强调。
3. 是否允许第一版继续复用当前地图背景和节点图标，先验证路线决策结构。
