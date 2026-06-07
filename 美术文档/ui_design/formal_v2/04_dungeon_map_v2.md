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
last_verified: 2026-06-07
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
| 选中详情抢画面 | 右侧详情面板容易把地图挤成辅助区域 | 暂时取消常驻选中详情，让地图、路线和节点成为视觉中心。 |
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

`dungeon_map` 是路线地图界面，不是节点按钮菜单，也不是详情面板页。

```text
当前层级 -> 地图与节点主视觉 -> 少量必要按钮 -> 进入 / 确认路线
```

视觉目标：

* 路线网络是主视觉。
* 可走、预览、未知、锁定的节点状态一眼可辨。
* 暂时不做常驻选中节点详情；需要的信息用小型 tooltip 或节点图标状态表达。
* 背包整理、撤离、层级信息是辅助，不抢地图和节点的权重。
* 地图底图和节点必须融合：节点应嵌在岩台、地层、羊皮纸剖面或铜质地图牌上，不能像漂浮按钮贴在海面背景上。
* 层地图是一张可推进的大地图，不是一屏静态节点板；玩家沿路线前进时，镜头应能向当前节点和更深处持续推进。
* 地图需要足够纵深：前景、中景、远景都要有可读路线或地貌线索，让玩家感觉还有未抵达区域。
* 每一层可以有独立生态主题，不必都画成洞窟或深渊视觉；地底草原、地下森林、晶洞、遗迹、雾谷、矿坑、湿地都可以成为某一层的主视觉。
* 画风贴近日系二次元冒险地图，明亮、干净、有童话式深渊感，不走欧美写实地图或冷硬战术雷达。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Minimal Status: layer / depth / small resource icons        │
│                                                            │
│                                                            │
│            Large Scrollable Layer Route Map                │
│ foreground -> midground -> distant route continuation       │
│                                                            │
│ small legend / route menu                 Confirm Route     │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `layer_header` | `64,36 1792x72` | 当前层、深度、少量资源图标。 |
| `route_canvas` | `80,120 1760x820` | 可推进大地图、节点网络、路线连线、地图地貌主视觉。 |
| `node_tooltip` | 跟随选中节点，小尺寸 | 暂时只显示极短风险 / 奖励图标，不做常驻详情面板。 |
| `map_action_button` | `1560,850 260x120` | 确认路线 / 进入节点主行动。 |
| `route_legend` | `80,920 760x80` | 节点状态图例和战争迷雾说明，低权重。 |

---

## 5. 信息层级

默认阅读顺序：

1. 当前层级和当前位置。
2. 可达路线和可见分支。
3. 选中节点的极简风险 / 奖励提示。
4. 进入节点 / 确认路线主行动。
5. 背包压力、撤离和已通层直达等辅助信息。

节点牌只显示：

* 节点图标。
* 风险等级或未知状态。
* 当前 / 可达 / 已探索 / 锁定状态。

暂时不做常驻 `selected_node_detail`。文字解释若需要，进入轻量 tooltip 或后续版本再补。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `EnterSelectedNode` / `ConfirmRoute` | 地图右下唯一主按钮。 |
| Secondary | 整理背包、查看路线、撤离 / 返回安全区 | 低权重按钮。 |
| Tertiary | 图例、种子信息、筛选 | 图标或小文本入口。 |
| Danger | 撤离、进入高危未知节点 | 视觉标记风险，必要时二次确认。 |

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| 每个节点按钮 | 节点仍可点击，但视觉上是地图对象，不是普通按钮。 |
| 进入节点 | 在地图右下显示一个主行动，不做右侧大详情面板。 |
| 背包整理 | 降级为辅助入口，提示背包压力时才提升。 |
| 撤离 | 可见但低权重；若有损失或阶段影响，进入确认。 |
| 层级切换 | 放到 `layer_header` 或 `layer_select`，不在地图主视觉中平铺。 |

---

## 8. 场景隐喻

地图界面像一张明亮、可直接操作的冒险路线图，可参考 `美术文档/美术风格参考/兰斯地图.png` 的“地图作为主体、节点和路线直接可见”的处理：

* 路线线段像探索线索。
* 未知节点被雾遮住。
* 当前节点有清晰定位。
* 大节点岛 / 节点牌是视觉焦点，但要像地图地貌的一部分。
* 边角只保留少量必要按钮。
* 底图可以是深渊纵剖、洞窟地层、羊皮纸地图、机械测绘图，也可以是某一层的独立生态地貌，例如地底草原、地下森林、晶洞、遗迹、雾谷、矿坑或湿地。
* 不使用海面、水域地平线或纯天空背景；如果某层确实有水边湿地，也必须让路线和节点长在岸线、栈道、岩台或遗迹结构上。
* 节点和路线要长在地貌上：铜质节点牌固定在岩层、树根、草甸道路、遗迹平台或羊皮纸路径上，路线像刻线、绳桥、轨迹、发光矿脉或被踩出的道路。
* 一张层地图可以大于单屏。概念图和后续实现都应预留镜头推进感：当前节点在前 / 中景，远处能看到下一个区域和更深路线。

AI 或后续美术提示应描述 large scrollable route map, foreground-to-background depth, camera-travel feeling, branching route, fogged nodes, brass markers, parchment/mechanical chart, distinct layer biome 等可视概念。

Formal V2 概念图提示还应追加：

```text
Japanese anime fantasy game UI concept art, large scrollable layer route map, strong foreground-to-background depth, camera-travel feeling, distinct subterranean biome for this layer, route nodes embedded into roads, terraces, trees, ruins or brass map plates, paths carved into terrain, warm hand-painted 2D background, soft cel shading, clean low-density interface, no ocean horizon, no floating disconnected nodes, no western realistic tactical map
```

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
  NodeTooltip
  MapActionButton
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
* 暂时不做常驻详情区，不在每个节点上堆长文本。
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
| `ui_panel_info` | 图例、极简 tooltip 和少量状态提示。 |
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
4. 暂时不需要常驻选中节点详情；节点状态和极简 tooltip 足够支撑选择。
5. 进入节点是唯一主行动。
6. 背包整理和撤离不抢主行动权重。
7. 路线、雾层、节点点击不会互相遮挡。
8. 底图、节点和路线像同一张地图系统，不能出现“海面底图 + 漂浮节点”的割裂感。
9. 地图有足够纵深，能支持玩家沿路线前进和镜头向前推移的想象。
10. 当前层有明确生态主题，且允许和其他层明显不同，不把所有层都画成同一种深渊洞窟。

---

## 12. 用户确认问题

建议确认以下 5 点：

1. `dungeon_map` 是否采用“地图 / 节点视觉中心 + 少量必要按钮”的结构。
2. 是否确认暂时不做常驻选中节点详情，只保留极简 tooltip / 图标状态。
3. 是否接受“可推进大地图 + 镜头前移”的层地图方向。
4. 是否接受每一层按独立生态主题设计，例如草原层、森林层、晶洞层或遗迹层。
5. 是否允许第一版继续复用当前地图背景和节点图标，先验证地图决策结构。
