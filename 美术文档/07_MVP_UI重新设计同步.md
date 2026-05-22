---
id: art_mvp_ui_redesign_sync
title: MVP UI 重新设计同步
type: art
role: 美术
domain: mvp_ui_archive
status: historical
source_of_truth: false
related:
  - AGENTS.md
  - PROJECT_STATUS.md
last_verified: 2026-05-23
update_rule: 历史记录仅在追溯或修正归档事实时更新。
---

# MVP UI 重新设计同步

> **定位：** 给美术侧同步当前 MVP UI 的重设计需求，明确需要输出的界面方案、UI 皮肤资产、交付格式和程序接入边界。
> **更新时间：** 2026-05-13

---

## 1. 当前问题

当前 Unity 版本已经能跑通 MVP 主要流程，但 UI 仍以程序骨架为主：

* 大量界面是纯色面板、文字和默认按钮。
* 已接入真实背景、图标和头像，但缺少统一 UI 皮肤承托。
* 弹窗、列表、按钮、状态标签没有统一视觉语言。
* 背包网格、战利品拾取、结算、小镇弹窗等高频界面还缺少明确美术版式。
* 当前 UI 主要服务功能验证，不应作为最终 MVP 视觉标准。

因此下一步不是零散补几张图，而是需要美术侧提供一套 **MVP UI 视觉方案**。

这些 UI 资产统一作为 `SourceType=preset` 进入 Manifest。具体需求维护在：

```text
美术文档/art_requirements_seed.json
```

后续程序反馈的新缺图、临时色块、未接入 UI 皮肤，也先追加到这个种子文件，再由 `Update-ArtManifest.ps1` 增量进入 Manifest。

UI 设计本身不只由本文档管理。正式设计流入口为：

```text
美术文档/ui_design/
  design_tokens.json
  component_catalog.json
  screen_layouts.json
  handoff_checklist.md
```

程序交付前运行：

```powershell
.\tools\美术工具\Validate-UIDesign.ps1
```

生成的 `ui_design/_generated/ui_design_handoff.md` 作为程序 agent 的 UI 接入摘要。

---

## 2. UI 重设计目标

MVP UI 重设计需要达成：

* 保持当前功能闭环不变：小镇 -> 深渊地图 -> 战斗 -> 战利品拾取 -> 结算 -> 小镇。
* 强化“地底奇幻冒险 + 蒸汽朋克 + 机械魔偶”的气质。
* 让背包、战斗、战利品取舍成为视觉重点。
* UI 元素要能适配 `1920x1080` 参考分辨率，并兼容 CanvasScaler 缩放。
* 不把文字烘焙进图片，文字仍由 Unity Text 组件渲染。
* 面板和按钮优先支持复用，避免每个界面都出一套完全不同的皮肤。

---

## 3. 程序接入边界

程序侧当前可接：

* Sprite 图片资源。
* 可切九宫格的 UI Sprite。
* 简单 Prefab 结构，后续再逐步接入。
* VisualID -> VisualAssetRegistry 的资源映射。
* `contain` / `cover` / 固定容器 DisplaySpec。

程序侧暂不希望 UI 方案依赖：

* 运行时读取 `art_manifest.json`。
* 把玩法文字、数值、按钮文案直接画在图片里。
* 每个分辨率单独出一套 UI。
* 复杂 Shader、复杂动效或大量 Animator 状态机。
* 需要玩法代码改数据结构才能展示的 UI 布局。

---

## 4. 需要美术输出的内容

每个重点界面建议输出：

* 一张 `1920x1080` 界面效果图。
* 一张标注图，标出主要容器、按钮、列表、插图区域。
* 可复用 UI 资产切图清单。
* 关键资产的 VisualID 建议。
* 如果有 9-slice 面板，需要标注边框安全区域。
* 如果需要程序配合变更布局，需要单独列出。

美术输出可以先是静态稿，不要求第一版就做 Prefab。

---

## 5. 界面重设计优先级

### P0：MVP 第一观感界面

| 界面 | 当前问题 | 美术侧建议输出 |
|---|---|---|
| 工坊主界面 | 只有背景和功能按钮，魔偶/底盘没有形成视觉中心 | 工坊主界面 layout、魔偶立绘摆位、背包/底盘展示区域、出发/出售/义体入口按钮样式。 |
| 战斗界面 | 背景和怪物头像已接，但 HUD、敌人卡、玩家站位仍像调试面板 | 战斗 HUD layout、怪物卡片框、玩家魔偶站位、HP/AP/SAN/Shield 样式、结束回合按钮样式。 |
| 背包与战利品拾取 | MVP 核心取舍玩法，但面板和格子仍很临时 | 背包底盘框、格子可用/锁定/悬停状态、战利品掉落区、确认按钮、拾取说明区。 |

### P1：流程闭环关键反馈

| 界面 | 当前问题 | 美术侧建议输出 |
|---|---|---|
| 深渊地图 | 背景和节点图标已接，但节点底板、路线连接、状态反馈较弱 | 地图节点底板、路线连接线、已访问/当前可选/锁定状态、层级标题区域。 |
| 结算界面 | 胜利/战败只是文字差异，反馈不够强 | 撤离成功面板、战败面板、战利品清单样式、损失清单样式、返回小镇按钮。 |
| 安全屋/阶梯房间 | 纯文本和按钮，没有房间感 | 安全屋房间插图/背景、阶梯入口插图/背景、继续深入/返回小镇按钮样式。 |

### P2：小镇功能弹窗

| 界面 | 当前问题 | 美术侧建议输出 |
|---|---|---|
| 出售界面 | 列表能用，但缺少小镇交易感 | 出售面板、列表行、金币/价值图标、出售按钮、批量出售按钮。 |
| 义体制造界面 | 有义体图标，但制造 UI 不成体系 | 义体制造面板、材料需求行、已装备/可制造/材料不足状态。 |
| 出发层选择界面 | 纯列表，缺少“选择深渊入口”的氛围 | 层入口卡片、锁定状态、可进入状态、层缩略图或入口图。 |

---

## 6. 通用 UI 皮肤资产建议

第一版建议优先做可复用组件，而不是每个界面单独重画。

| 类型 | 建议 VisualID | 用途 |
|---|---|---|
| 主弹窗面板 | `ui_panel_main` | 出售、义体、层选择、结算等大弹窗。 |
| 小信息面板 | `ui_panel_info` | 提示、说明、数值摘要。 |
| 列表行底板 | `ui_list_row_normal` | 出售列表、义体列表、层选择列表。 |
| 列表行选中 | `ui_list_row_selected` | 当前选中层、当前选中条目。 |
| 主按钮 | `ui_button_primary` | 出发、确认、继续。 |
| 次按钮 | `ui_button_secondary` | 返回、关闭、取消。 |
| 危险按钮 | `ui_button_danger` | 丢弃、卖出、撤离等高风险操作。 |
| 标题装饰 | `ui_title_divider` | 各界面标题下方装饰线。 |
| 金币图标 | `ui_icon_money` | 出售、结算、价值显示。 |
| 锁定图标 | `ui_icon_locked` | 层选择、未解锁功能。 |
| 已装备图标 | `ui_icon_equipped` | 义体制造列表。 |

如果使用九宫格，请使用透明 PNG，并在交付说明中写明建议 border。

当前这些通用项已经进入 preset 种子文件，会由 Manifest 统一生成提示词、规格和 Approved 目标路径。

---

## 7. 背包与战利品专项要求

背包是当前 MVP 的核心玩法展示，不建议只用普通矩形格子。

需要明确：

* 背包底盘框是否使用 `chassis_*_frame` 作为整体框。
* 格子是否需要独立贴图：可用格、锁定格、悬停格、可放置预览、不可放置预览。
* 背包玩法格尺寸以当前 Unity 拖拽逻辑为准，参考容器为 `100x100`；物品图标和格子装饰在格内按需要等比适配。
* 物品图标仍保持按道具 Shape 占格，不用图标源图尺寸撑布局。
* 战利品拾取界面中，战利品应明显位于背包外侧，表达“拖入背包进行取舍”。
* 拖拽状态最好有高亮、阴影或边框反馈。

建议 VisualID：

```text
ui_inventory_slot_available
ui_inventory_slot_locked
ui_inventory_slot_hover
ui_inventory_slot_valid
ui_inventory_slot_invalid
ui_inventory_chassis_panel
ui_loot_pickup_panel
ui_loot_drop_zone
```

---

## 8. 战斗 HUD 专项要求

战斗界面需要在不遮挡背包的前提下强化信息层级。

需要明确：

* 玩家魔偶立绘是否放入战斗界面。
* 怪物头像卡是否保留 `320x320` 大头像，还是拆成大图 + 小状态条。
* HP、护盾、AP、SAN 的显示方式。
* 玩家选择武器后，敌人可选状态如何高亮。
* 敌方行动/玩家回合的状态提示区域。

建议 VisualID：

```text
ui_combat_enemy_card
ui_combat_enemy_card_selected
ui_combat_status_bar_hp
ui_combat_status_bar_shield
ui_combat_ap_pip
ui_combat_turn_banner
```

---

## 9. 交付格式建议

第一批 UI 重设计建议按以下方式交付：

```text
美术文档/_ui_design/
  workshop_main/
    preview_1920x1080.png
    layout_notes.md
    export_list.md
  combat_hud/
    preview_1920x1080.png
    layout_notes.md
    export_list.md
  inventory_loot/
    preview_1920x1080.png
    layout_notes.md
    export_list.md
```

正式切图仍放入：

```text
UnityClient/Assets/Art/Approved/UI/
```

命名建议：

```text
ui_<screen_or_component>_<usage>.png
```

---

## 10. 第一批建议任务

建议美术侧第一批不要铺太宽，先做三张关键界面稿：

1. 工坊主界面：确定整体 UI 风格、魔偶立绘、底盘展示、主按钮形态。
2. 战斗界面：确定 HUD、敌人卡片、玩家信息和背包不互相遮挡。
3. 背包与战利品拾取界面：确定格子、底盘、战利品掉落区和取舍反馈。

这三张定下来后，程序侧可以开始把 UI 皮肤组件化，再向出售、义体、结算、层选择等弹窗扩展。

## 11. 设计系统化后的执行方式

UI 重设计现在拆成三条并行线：

* `设计系统线`：维护 `design_tokens.json`、`component_catalog.json` 和 `screen_layouts.json`，让界面、组件、程序接入有结构化来源。
* `界面方案线`：先做工坊主界面、战斗界面、背包与战利品拾取三张 `1920x1080` 方案稿，用来确认版式和整体观感。
* `资产生产线`：把可复用 UI 切图作为 preset entry 进入 Manifest，按 `ui_panel_main`、`ui_button_primary`、`ui_inventory_slot_available` 等 VisualID 批量生成、预处理、筛选和接入。

第一批可以先跑 P1 的 UI 资产：通用面板、主/次按钮、背包格状态、战利品面板、敌人卡框、HP/护盾状态条。P2 的结算、地图路线、列表行和状态小图标可以在 P1 风格稳定后继续扩展。
