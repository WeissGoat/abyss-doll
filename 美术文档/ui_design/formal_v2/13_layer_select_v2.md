---
id: art_ui_formal_v2_layer_select
title: Layer Select Formal V2 方案
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v1/layer_select_v1.md
last_verified: 2026-06-02
update_rule: 编写或确认 layer_select Formal V2 详细方案时同步本文件。
---

# Layer Select Formal V2 方案

> **状态：** Formal V2 设计草案。本文用于确认 `layer_select` 的正式化方向；确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`layer_select` Formal V1 已能显示可进入层、锁定层和确认下潜，但整体仍像弹窗列表：

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 层级选择缺少“下潜前”仪式感 | 层行列表在弹窗中平铺 | 改为深渊入口前的航行 / 升降井选择台。 |
| 锁定层像普通不可点行 | 锁图标和普通层行权重接近 | 锁定层放到深渊剖面图下方，用封印 / 锁链视觉区分。 |
| 风险预览不足 | 玩家只知道选了哪层 | 右侧显示该层风险、推荐整备和主要目标。 |
| Confirm 像普通按钮 | 出发行动不够突出 | “开始下潜”作为唯一主行动，和深渊入口视觉绑定。 |

---

## 2. 玩家目标

玩家进入层选择界面时，目标是：

1. 选择本次要下潜的层。
2. 判断当前整备是否适合该层。
3. 看懂锁定层的解锁方向。
4. 确认下潜或返回工坊继续准备。

---

## 3. Formal V2 体验定位

`layer_select` 是“深渊入口的层级选择台”，不是列表弹窗。

```text
深渊入口
  -> 垂直层级剖面
  -> 选中层风险卡
  -> 整备检查
  -> 开始下潜
```

视觉目标：

* 画面中心是深渊垂直剖面或升降井，不是普通面板。
* 可进入层像停靠点，锁定层像被雾、封印或锁链遮住。
* 玩家一次只评估一个选中层。
* 温暖工坊边缘和深渊冷光形成对比，但不变成硬核机械控制台。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Header: dive preparation / money / close                   │
│                                                            │
│ Abyss Shaft Map          Selected Layer Briefing           │
│ layer nodes vertical     risk, target, readiness           │
│                                                            │
│ Locked Layer Hints       Readiness Strip     Start Dive    │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `layer_select_background` | `0,0 1920x1080` | 深渊入口 / 升降井背景。 |
| `abyss_shaft_map` | `220,150 700x760` | 垂直层级剖面、可进入节点和锁定节点。 |
| `selected_layer_briefing` | `980,180 600x420` | 当前选中层风险、推荐整备、目标摘要。 |
| `readiness_strip` | `980,640 600x120` | HP/SAN、背包空间、维护许可的简短检查。 |
| `locked_layer_hint` | `250,820 620x120` | 锁定层解锁条件摘要。 |
| `layer_action_panel` | `980,800 600x120` | Start Dive、返回工坊。 |

---

## 5. 信息层级

默认阅读顺序：

1. 当前选中哪一层。
2. 该层是否可进入。
3. 风险和推荐整备是否匹配。
4. 锁定层的解锁方向。
5. 开始下潜或返回。

不默认展开：

* 完整层级剧情说明。
* 全部怪物 / 节点列表。
* 复杂解锁公式。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `StartDiveSelectedLayer` | 唯一主按钮，与深渊入口绑定。 |
| Secondary | 选择层、返回工坊 | 层节点 / 小按钮。 |
| Tertiary | 查看整备建议、查看锁定详情 | 折叠详情。 |
| Danger | 无准备强行下潜 | 如后续存在，必须二次确认。 |

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前入口 | V2 策略 |
|---|---|
| 层行列表 | 改为深渊剖面节点。 |
| Confirm | 改为 Start Dive，唯一主行动。 |
| Close | 返回工坊，低权重。 |
| 锁定层 | 不作为普通行，放到剖面下方锁定区域。 |

---

## 8. 场景隐喻

层选择界面像站在深渊入口前：

* 左侧或中央是垂直向下的深渊剖面。
* 可进入层有微光停靠点，锁定层被雾气、锁链或封印遮住。
* 右侧是工坊留下的地图板、灯具和整备检查纸条。
* 主行动像启动升降机或打开下潜门。

---

## 9. 程序迁移影响

确认后建议层级：

```text
LayerSelectPanel
  LayerSelectBackground
  AbyssShaftMap
  SelectedLayerBriefing
  ReadinessStrip
  LockedLayerHint
  LayerActionPanel
```

关键绑定：

| 绑定 | 来源 |
|---|---|
| 可进入层 / 锁定层 | Dungeon / Layer 配置和解锁服务。 |
| 当前选中层 | UI selection state。 |
| 风险 / 推荐整备 | DiveReadinessService / DungeonPreviewService。 |
| 开始下潜 | 流程服务，UI 不直接切换关卡状态。 |

---

## 10. 素材需求变化

第一版复用：

| VisualID | 用法 |
|---|---|
| `bg_layer_select` | 深渊入口背景。 |
| `ui_panel_main` / `ui_panel_info` | 风险卡、整备条和行动面板。 |
| `ui_list_row_selected` | 可临时作为选中层节点底板。 |
| `ui_icon_locked` | 锁定层。 |
| `ui_icon_warning` | 风险提示。 |
| `ui_button_primary` / `ui_button_secondary` | 开始下潜 / 返回。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `ui_layer_abyss_shaft_map` | 深渊剖面地图底板。 |
| `ui_layer_node_available` | 可进入层节点。 |
| `ui_layer_node_locked` | 锁定层节点。 |
| `ui_layer_readiness_stamp` | 整备检查章。 |

---

## 11. UX 验收标准

1. 3 秒内能看出这是下潜层级选择界面。
2. 当前选中层、锁定层和可进入层视觉区分明显。
3. Start Dive 是唯一主行动。
4. 风险和整备检查可读，但不形成数据墙。
5. 锁定层有解锁方向提示，不像普通不可点按钮。

---

## 12. 概念图提示词

```text
Use case: ui-mockup
Asset type: Formal V2 game UI concept art for layer_select
Primary request: A Japanese anime subterranean fantasy game interface showing an abyss entrance layer selection screen.
Scene/backdrop: cozy workshop threshold opening into a deep vertical abyss shaft, carved lift frame, rune guide light, misty depth.
Subject: a vertical abyss cross-section map with glowing selectable layer stops on the left, locked lower layers covered by mist and small lock charms, selected layer briefing panel on the right, readiness check strip, one large start dive button.
Style: soft hand-painted fantasy UI, parchment and wood details, luminous cave accents, low information density, clear visual hierarchy, no brass palette, no steampunk machinery.
Composition: 16:9 landscape, central-left abyss shaft as main visual, right side briefing card, bottom-right primary action.
Avoid: dense spreadsheet UI, sci-fi control room, hard industrial dashboard, unreadable tiny text, real readable words, logos, watermark.
```
