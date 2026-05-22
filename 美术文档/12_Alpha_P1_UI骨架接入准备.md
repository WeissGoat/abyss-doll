---
id: art_alpha_p1_ui_skeleton_plan
title: Alpha P1 UI 骨架接入准备
type: art
role: 美术
domain: ui_handoff
status: active
source_of_truth: true
related:
  - AGENTS.md
  - PROJECT_STATUS.md
last_verified: 2026-05-23
update_rule: 修改美术流水线、资源规格、UI 交付或运行时验收要求时同步本文件。
---

# Alpha P1 UI 骨架接入准备

> **更新时间：** 2026-05-22
> **定位：** P0 UI 接入等待期间，美术侧提前完成 P1 界面骨架草案。本文用于后续程序接入前的准备说明，不替代 `screen_layouts.json` 和 `ui_design_handoff.md`。

---

## 1. 当前状态

P1 五个界面已经从 `planned` 推进到 `draft`：

| ScreenID | 状态 | 说明 |
|---|---|---|
| `dungeon_map` | `draft` | 已补全地图背景、节点路线、背包整理入口、控制器绑定和验收标准。 |
| `settlement` | `draft` | 已补全胜利/战败背景、结算面板、标题分隔、返回按钮和验收标准。 |
| `sell_panel` | `draft` | 已补全出售弹窗、列表、按钮、金币图标、运行时行对象和验收标准。 |
| `prosthetic_panel` | `draft` | 已补全义体制造弹窗、配方列表、已装备图标、制造按钮和验收标准。 |
| `layer_select` | `draft` | 已补全层选择背景、层列表、锁定图标、确认/返回按钮和验收标准。 |

P1 暂不标记为 `handoff`，原因是程序侧当前应优先完成 P0 三个界面接入。P0 验收通过后，可将本批 P1 从 `draft` 推进到 `handoff`。

---

## 2. 资产覆盖

本批 P1 所需核心 VisualID 已在 Approved 中存在：

```text
bg_dungeon_map
bg_layer_select
bg_settlement_victory
bg_settlement_defeat
ui_panel_main
ui_panel_info
ui_list_row_normal
ui_list_row_selected
ui_button_primary
ui_button_secondary
ui_button_danger
ui_dungeon_node_plate
ui_dungeon_route_line
ui_settlement_victory_panel
ui_settlement_defeat_panel
ui_icon_money
ui_icon_locked
ui_icon_equipped
ui_title_divider
```

也就是说，P1 当前主要不是缺图问题，而是等 P0 接入经验稳定后再进入程序实现。

---

## 3. 接入顺序建议

建议顺序：

1. `dungeon_map`
2. `layer_select`
3. `settlement`
4. `sell_panel`
5. `prosthetic_panel`

原因：

* `dungeon_map` 和 `layer_select` 直接影响下潜入口和深层节奏。
* `settlement` 是局内闭环结束反馈，和正式纵切体验强相关。
* `sell_panel`、`prosthetic_panel` 依赖有效列表数据展示，接入时需要同时确认至少一条行数据可见。

---

## 4. 各界面重点

### dungeon_map

规格入口：

```text
screen_layouts.json -> dungeon_map
ui_design_handoff.md -> 深渊地图界面
```

重点：

* `backgroundImage` 使用 `bg_dungeon_map`。
* 节点底板使用 `ui_dungeon_node_plate`。
* 路线连接线使用 `ui_dungeon_route_line`。
* 打开背包按钮使用 `ui_button_secondary`。
* 关闭背包按钮使用 `ui_button_danger`。
* 路线和背景不拦截节点点击。

### settlement

重点：

* 成功撤离使用 `bg_settlement_victory` 和 `ui_settlement_victory_panel`。
* 战败结算使用 `bg_settlement_defeat` 和 `ui_settlement_defeat_panel`。
* 标题分隔使用 `ui_title_divider`。
* 返回工坊按钮使用 `ui_button_primary`。
* 文字仍由 `SettlementUIController` 渲染，不烘焙进图片。

### sell_panel

重点：

* 主容器 `SellPanel_Card` 使用 `ui_panel_main`。
* 列表行使用 `ui_list_row_normal`，选中或焦点后续可用 `ui_list_row_selected`。
* `SellAll_Button` 使用 `ui_button_danger`。
* 单行 `Sell_Button` 使用 `ui_button_secondary`。
* 验收时必须至少展示一条可出售物品行。

### prosthetic_panel

重点：

* 主容器 `ProstheticPanel_Card` 使用 `ui_panel_main`。
* 普通配方行使用 `ui_list_row_normal`。
* 已装备行使用 `ui_list_row_selected` 或 `ui_icon_equipped`。
* `Craft_Button` 使用 `ui_button_primary`。
* 验收时必须至少展示一条义体配方行。

### layer_select

重点：

* 全屏背景使用 `bg_layer_select`。
* 主容器 `DungeonStartLayer_Card` 使用 `ui_panel_main`。
* 可进入行使用 `ui_list_row_normal` / `ui_list_row_selected`。
* 锁定层显示 `ui_icon_locked`。
* `Confirm_Button` 使用 `ui_button_primary`。
* `Close_Button` 使用 `ui_button_secondary`。

---

## 5. 验收方式

P1 程序接入后，美术侧仍使用 ArtAcceptance：

```powershell
Set-Content -LiteralPath UnityClient\Logs\.art_acceptance_trigger -Value RUN_ART_ACCEPTANCE
```

检查：

```text
UnityClient/Logs/ArtAcceptance/latest/report.json
UnityClient/Logs/ArtAcceptance/latest/ui_snapshot.json
UnityClient/Logs/ArtAcceptance/latest/registry_snapshot.json
UnityClient/Logs/ArtAcceptance/latest/screenshots/
```

通过门槛：

* `MissingRequiredVisualIDs = []`。
* P1 截图无纯蓝底、黑块、缺失 Sprite。
* 列表类界面有有效行数据，不能只显示空壳。
* 关键按钮和列表行可点击。
* 背景、分隔线、图标不阻挡交互射线。

---

## 6. 推进条件

满足以下条件后，可以把 P1 从 `draft` 推进到 `handoff`：

1. P0 三个界面完成程序接入并通过运行时美术验收。
2. 程序侧确认本批 P1 接入窗口。
3. 美术侧复查 `ui_design_handoff.md` 中 P1 的对象路径和当前代码一致。
4. `sell_panel` 和 `prosthetic_panel` 的验收场景能稳定生成至少一条有效行数据。
