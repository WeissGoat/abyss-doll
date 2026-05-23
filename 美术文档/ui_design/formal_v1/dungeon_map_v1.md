---
id: art_ui_formal_v1_dungeon_map
title: 深渊地图界面 Formal V1
type: art
role: 美术
domain: ui_design
status: active
source_of_truth: true
related:
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
last_verified: 2026-05-24
update_rule: 修改深渊地图正式结构或程序迁移要求时同步本文件。
---

# 深渊地图界面 Formal V1

> **目标：** 把深渊地图从节点按钮容器升级为正式路线地图：层级推进、当前节点、可达节点、锁定节点、风险预览和背包整理入口都清晰。

---

## 1. Formal V1 结构

```text
┌────────────────────────────────────────────────────────────┐
│ Layer / Depth / Current Risk                               │
│                                                            │
│          Dungeon Route Map                                 │
│     node -> node -> branch -> node                         │
│                                                            │
│ Selected Node Detail             Inventory Prep / Return   │
└────────────────────────────────────────────────────────────┘
```

### 主区域

| ZoneID | Rect | 目标 |
|---|---|---|
| `map_background` | `0,0 1920x1080` | 深渊地图背景。 |
| `layer_header` | `96,48 900x96` | 当前层、深度、风险摘要。 |
| `route_canvas` | `220,150 1480x660` | 节点、路线、分支、当前节点。 |
| `selected_node_detail` | `220,830 680x160` | 选中节点说明、风险、预期奖励。 |
| `inventory_controls` | `1040,830 760x160` | 整理背包、关闭背包、继续探索。 |

当前已写入 active `screen_layouts.json`，状态为 `active_spec / FormalV1`。程序和素材生成均以 active 规格为准。

---

## 2. 节点表现

节点不是普通按钮列表，而是地图上的路线点：

```text
MapNode
  NodePlate_Image
  NodeIcon_Image
  NodeStateOverlay
  LockedIcon_Image
  CurrentMarker
  ButtonHotspot
```

状态：

| 状态 | 表现 |
|---|---|
| 当前节点 | marker 或高亮环。 |
| 可进入 | 正常亮度，可点击。 |
| 不可达 | 降亮，不可点击。 |
| 锁定 | 锁图标 + 降亮。 |
| 已访问 | 低亮度或打勾状态，后续补图标。 |

---

## 3. 可复用资源

| VisualID | 用途 |
|---|---|
| `bg_dungeon_map` | 地图背景。 |
| `ui_dungeon_node_plate` | 节点底板。 |
| `ui_dungeon_route_line` | 路线线条。 |
| `ui_icon_locked` | 锁定状态。 |
| `ui_panel_info` | 说明区。 |
| `ui_button_secondary` | 整理背包。 |
| `ui_button_danger` | 关闭背包/放弃类操作。 |

---

## 4. 程序迁移要求

建议节点：

```text
DungeonMapPanel
  MapBackground_Image
  LayerHeader
  RouteCanvas
    RouteLinesLayer
    NodesLayer
    MarkersLayer
  SelectedNodeDetail
  InventoryControls
```

路线线条和背景不拦截点击，点击热区仍在节点按钮上。

active 规格中的关键路径：

| 路径 | VisualID / 组件 |
|---|---|
| `DungeonMapPanel/DungeonMapBackground_Image` | `bg_dungeon_map` |
| `DungeonMapPanel/LayerHeaderPanel` | `Panel.Info` / `ui_panel_info` |
| `DungeonMapPanel/RouteCanvas/RouteLinesLayer/DungeonRouteLine_Image` | `Map.RouteLine` / `ui_dungeon_route_line` |
| `DungeonMapPanel/RouteCanvas/NodesLayer/NodeButton(Clone)` | `Map.NodePlate` / `ui_dungeon_node_plate` |
| `DungeonMapPanel/RouteCanvas/NodesLayer/NodeButton(Clone)/LockedIcon_Image` | `Icon.Locked` / `ui_icon_locked` |
| `DungeonMapPanel/SelectedNodeDetailPanel` | `Panel.Info` / `ui_panel_info` |
| `DungeonMapPanel/InventoryControlsPanel/OpenBackpack_Button` | `Button.Secondary` / `ui_button_secondary` |
| `DungeonMapPanel/InventoryControlsPanel/CloseBackpack_Button` | `Button.Danger` / `ui_button_danger` |

---

## 5. 验收标准

1. 地图截图中可以看出路线、节点、当前层和当前节点。
2. 可进入、锁定、不可达状态至少有一种可见区分。
3. 路线线条不遮挡节点点击。
4. 背包整理入口清楚，但不抢地图主体。
5. 关闭背包/整理背包逻辑不受皮肤影响。
