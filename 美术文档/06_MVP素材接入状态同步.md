# MVP 素材接入状态同步

> **定位：** 给美术侧同步当前 Unity 已接入素材、已入库但未展示素材、配置引用但缺失素材，以及下一批需要补齐的资源。
> **更新时间：** 2026-05-15

---

## 1. 当前程序接入方式

程序侧当前接入链路为：

```text
Approved PNG / Prefab -> VisualAssetRegistry -> 配置 VisualID -> Unity UI DisplaySpec 容器
```

当前约定：

* Unity 运行时不依赖 `art_manifest.json`。
* 美术正式交付资源放入 `UnityClient/Assets/Art/Approved/`。
* 程序通过 `VisualID` 从 `VisualAssetRegistry` 读取 Sprite。
* 图标、头像、义体图标使用 `contain`。
* 背景使用 `cover`，核心内容应留在中心 4:3 安全区。
* CanvasScaler 参考分辨率为 `1920x1080`。

---

## 2. 已经接入 Unity 的素材

这些资源已经进入 Approved，并且当前 Unity 界面会实际读取使用。

| 类型 | 当前用途 | 已接入 VisualID / 规则 | 说明 |
|---|---|---|---|
| 物品图标 | 背包、战利品拾取、工坊出售列表 | `item_*_icon` | 物品图标容器为 `64x64`，等比 contain。 |
| 怪物头像 | 战斗敌人卡片 | `monster_{MonsterID}_portrait` | 怪物头像容器为 `320x320`，等比 contain。 |
| 节点图标 | 深渊地图节点 | `node_combat_icon`, `node_boss_icon`, `node_safe_room_icon`, `node_stairs_icon` | 节点图标容器为 `80x80`，等比 contain。 |
| 义体图标 | 工坊义体制造列表 | `prosthetic_{ProstheticID}_icon` | 义体图标容器为 `80x80`，等比 contain。 |
| 深渊地图背景 | 深渊路线图 | `bg_dungeon_map`, `bg_dungeon_layer_1`, `bg_dungeon_layer_2` | 背景按 `1920x1080` 参考视口 cover 裁切。 |
| 战斗背景 | 战斗界面 | `bg_combat_abyss` | 背景按 `1920x1080` 参考视口 cover 裁切。 |
| 工坊背景 | 小镇/工坊主界面 | `bg_workshop_day` | 背景按 `1920x1080` 参考视口 cover 裁切。 |
| 缺失占位图 | 缺图 fallback | `ui_missing_sprite` | 资源缺失时用于保证流程不中断。 |
| P0 UI 皮肤 | 工坊、战斗 HUD、战利品拾取 | 见第 4 节可接入清单 | 已进入 `Approved/UI`，可交给程序按 `VisualID` 接入。 |

---

## 3. Approved 中已有但当前未展示的素材

这些资源已经在 Approved 中，但当前 UI 还没有实际摆放到界面里。它们不是美术缺图，属于程序下一步接入或 UI 版式待定。

| 类型 | 已有资源 | 当前状态 | 建议用途 |
|---|---|---|---|
| 魔偶立绘 | `doll_proto_0_stand` | 已入库，未展示 | 工坊主界面魔偶展示、战斗玩家站位、结算展示。 |
| 背包底盘 | `chassis_chassis_lv1_basic_frame`, `chassis_chassis_lv2_expanded_frame` | 已入库，未展示 | 背包网格底盘框、工坊底盘展示、升级预览。 |

需要美术侧补充的信息：

* 魔偶立绘在工坊主界面的推荐摆放区域。
* 魔偶立绘在战斗界面是否作为玩家站位展示。
* 背包底盘框与格子 UI 的层级关系：底盘框在格子下方、上方，还是拆分为底框和边框。

---

## 4. 当前可交给程序接入的 UI 皮肤

截至 2026-05-15，第一批 P0 UI 皮肤已经进入：

```text
UnityClient/Assets/Art/Approved/UI/
```

这些资源不再是占位图，可以交给程序侧登记 `VisualAssetRegistry` 并接入到 UGUI。文本、数字、按钮文案仍由 Unity Text 渲染，不烘焙在 Sprite 中。

| VisualID | 用途 | Unity 建议 |
|---|---|---|
| `ui_panel_info` | 小信息面板、状态摘要、说明区 | Image Type 使用 `Sliced`，Sprite border `48,48,48,48`。 |
| `ui_button_primary` | 出发、确认、继续等主操作按钮 | Image Type 使用 `Sliced`，Sprite border `64,64,48,48`。 |
| `ui_button_secondary` | 返回、取消、关闭等次操作按钮 | Image Type 使用 `Sliced`，Sprite border `64,64,48,48`。 |
| `ui_button_danger` | 丢弃、放弃等危险操作按钮 | Image Type 使用 `Sliced`，Sprite border `64,64,48,48`。 |
| `ui_inventory_chassis_panel` | 背包底盘/网格承托面板 | Image Type 使用 `Sliced` 或固定尺寸，Sprite border `64,64,64,64`。 |
| `ui_inventory_slot_available` | 背包可用格 | 固定显示 `100x100`，Image Type 使用 `Simple`。 |
| `ui_inventory_slot_locked` | 背包锁定格 | 固定显示 `100x100`，Image Type 使用 `Simple`。 |
| `ui_inventory_slot_hover` | 背包悬停格 | 固定显示 `100x100`，Image Type 使用 `Simple`。 |
| `ui_inventory_slot_valid` | 背包可放置反馈格 | 固定显示 `100x100`，Image Type 使用 `Simple`。 |
| `ui_inventory_slot_invalid` | 背包不可放置反馈格 | 固定显示 `100x100`，Image Type 使用 `Simple`。 |
| `ui_loot_pickup_panel` | 战利品拾取主面板 | Image Type 使用 `Sliced`，Sprite border `64,64,64,64`。 |
| `ui_loot_drop_zone` | 战利品掉落/待拾取区域 | Image Type 使用 `Sliced`，Sprite border `48,48,48,48`。 |
| `ui_combat_enemy_card` | 敌人卡片普通状态 | 可固定显示或 Sliced，Sprite border `64,64,64,64`。 |
| `ui_combat_enemy_card_selected` | 敌人卡片选中状态 | 可固定显示或 Sliced，Sprite border `64,64,64,64`。 |
| `ui_combat_status_bar_hp` | HP 状态条轨道 | Image Type 使用 `Sliced`，Sprite border `48,48,32,32`；填充值由程序单独控制。 |
| `ui_combat_status_bar_shield` | 护盾状态条轨道 | Image Type 使用 `Sliced`，Sprite border `48,48,32,32`；填充值由程序单独控制。 |
| `ui_combat_ap_pip` | AP 行动点圆点 | 固定小图标，Image Type 使用 `Simple`。 |
| `ui_combat_turn_banner` | 回合提示条 | Image Type 使用 `Sliced`，Sprite border `64,64,48,48`。 |
| `ui_icon_money` | 金币/价值图标 | 固定小图标，Image Type 使用 `Simple`。 |

注意：

* 背包玩法格尺寸仍以程序现有 `100x100` 为准，UI 皮肤只作为格子底图或状态覆盖。
* 背景、面板、状态条轨道等非交互 Image 默认 `raycastTarget=false`。
* 背包格、物品、按钮、敌人卡片等交互对象保留程序原有射线逻辑。
* 程序侧可以先接第 4 节清单，不需要接第 5 节仍未完成的 UI 资源。

---

## 5. 配置已引用但 Approved 缺失的素材

截至 2026-05-15，配置已引用且属于 MVP 主流程的 P0/P1 视觉资源没有新的 Approved 缺失项。

已完成补齐：

| VisualID | 当前引用位置 | 状态 |
|---|---|---|
| `node_stairs_icon` | `Dungeons/layer_1.json` 与 `Dungeons/layer_2.json` 的 `EndNode.NodeIconID` | 已进入 Approved，并已登记到 `VisualAssetRegistry`。 |

---

## 6. 当前界面美术缺口

这些界面已有功能，但 UI 仍主要是程序色块、文字和按钮。后续可以作为 UI 重设计或面板皮肤资源的需求来源。

| 界面 | 当前接入状态 | 缺少的美术资源 |
|---|---|---|
| 安全屋/阶梯房间 | 纯文本和按钮，未接背景或插图 | 安全屋背景/插图、阶梯房间背景/插图、房间面板皮肤。 |
| 战斗战利品拾取 | 物品图标已接，第一批面板和按钮皮肤已进入 Approved | 标题装饰、物品详情区细节、最终 Unity 布局微调。 |
| 撤离/战败结算 | 纯色底和文本 | 胜利/撤离结算背景、战败结算背景、结算清单面板、胜败插图。 |
| 出发深渊层选择 | 纯色卡片列表 | 层入口卡片皮肤、锁定/可进入状态图标、层缩略图或入口插图。 |
| 背包网格 | 物品图标已接，第一批格子状态和底盘皮肤已进入 Approved | 程序接入后的拖拽状态切换验证。 |
| 工坊出售面板 | 物品图标已接，面板仍是程序色块 | 出售面板皮肤、列表行皮肤、按钮皮肤、金币/价值小图标。 |
| 义体制造面板 | 义体图标已接，面板仍是程序色块 | 制造面板皮肤、材料需求行皮肤、已装备/可制造状态图标。 |
| 战斗 HUD | 战斗背景和怪物头像已接，敌人卡、状态条、AP 和按钮皮肤已进入 Approved | 玩家魔偶站位、SAN 专用表现、行动按钮布局验证。 |
| 深渊地图 | 背景和节点图标部分接入 | 阶梯节点图标、节点底板、路线连接线、已访问/可访问/锁定状态皮肤。 |

---

## 7. 建议同步给美术的优先级

P0：立即补齐

* 暂无配置已引用但 Approved 缺失的 P0 素材。

P1：已有资源落界面前需要版式确认

* `doll_proto_0_stand` 的工坊/战斗摆放建议。
* `chassis_*_frame` 与背包格子的层级和视觉方案。

P1：MVP 体验明显提升的界面资源

* 程序接入后的战斗战利品拾取界面微调。
* 撤离/战败结算面板皮肤。
* 安全屋/阶梯房间背景或插图。

P2：整体 UI 皮肤系统

* 通用按钮。
* 通用弹窗面板。
* 列表行底板。
* 标题装饰。
* 状态图标：锁定、已选、可进入、已装备、可制造。

---

## 8. 程序侧当前可直接接入的命名规则

美术补图时优先按以下 VisualID 命名，程序可以更快接入：

| 资源 | 命名 |
|---|---|
| 物品图标 | `item_{ItemConfigID}_icon` |
| 怪物头像 | `monster_{MonsterID}_portrait` |
| 节点图标 | `node_{node_type}_icon` |
| 义体图标 | `prosthetic_{ProstheticID}_icon` |
| 工坊背景 | `bg_workshop_day` |
| 战斗背景 | `bg_combat_abyss` |
| 深渊地图背景 | `bg_dungeon_map` 或 `bg_dungeon_layer_{LayerID}` |
| 魔偶立绘 | `doll_{DollID}_stand` |
| 背包底盘 | `chassis_{ChassisID}_frame` |

如果美术侧要新增 UI 皮肤类资源，建议先统一使用：

```text
ui_<screen_or_component>_<usage>
```

示例：

```text
ui_combat_loot_panel
ui_settlement_victory_panel
ui_settlement_defeat_panel
ui_inventory_slot_available
ui_inventory_slot_locked
ui_button_primary
ui_button_danger
```
