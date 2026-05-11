# MVP 素材接入状态同步

> **定位：** 给美术侧同步当前 Unity 已接入素材、已入库但未展示素材、配置引用但缺失素材，以及下一批需要补齐的资源。
> **更新时间：** 2026-05-11

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
| 节点图标 | 深渊地图节点 | `node_combat_icon`, `node_boss_icon`, `node_safe_room_icon` | 节点图标容器为 `80x80`，等比 contain。 |
| 义体图标 | 工坊义体制造列表 | `prosthetic_{ProstheticID}_icon` | 义体图标容器为 `80x80`，等比 contain。 |
| 深渊地图背景 | 深渊路线图 | `bg_dungeon_map`, `bg_dungeon_layer_1`, `bg_dungeon_layer_2` | 背景按 `1920x1080` 参考视口 cover 裁切。 |
| 战斗背景 | 战斗界面 | `bg_combat_abyss` | 背景按 `1920x1080` 参考视口 cover 裁切。 |
| 工坊背景 | 小镇/工坊主界面 | `bg_workshop_day` | 背景按 `1920x1080` 参考视口 cover 裁切。 |
| 缺失占位图 | 缺图 fallback | `ui_missing_sprite` | 资源缺失时用于保证流程不中断。 |

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

## 4. 配置已引用但 Approved 缺失的素材

这些 VisualID 已被配置或程序引用，但当前 Approved 中没有对应正式图，会显示 missing sprite 或 fallback。

| 优先级 | VisualID | 当前引用位置 | 需要美术补齐 |
|---|---|---|---|
| P0 | `node_stairs_icon` | `Dungeons/layer_1.json` 与 `Dungeons/layer_2.json` 的 `EndNode.NodeIconID` | 阶梯/向下入口节点图标。 |

`node_stairs_icon` 建议规格：

* 路径：`UnityClient/Assets/Art/Approved/Nodes/Icons/node_stairs_icon.png`
* 源图：`512x512`
* 背景：透明
* Unity 显示容器：`80x80`
* FitMode：`contain`
* 构图：强轮廓、低细节、能表达“进入下一层/返回小镇的关底入口”

---

## 5. 当前界面美术缺口

这些界面已有功能，但 UI 仍主要是程序色块、文字和按钮。后续可以作为 UI 重设计或面板皮肤资源的需求来源。

| 界面 | 当前接入状态 | 缺少的美术资源 |
|---|---|---|
| 安全屋/阶梯房间 | 纯文本和按钮，未接背景或插图 | 安全屋背景/插图、阶梯房间背景/插图、房间面板皮肤。 |
| 战斗战利品拾取 | 物品图标已接，面板仍是纯色遮罩 | 战利品拾取面板、标题装饰、按钮皮肤、掉落区底纹。 |
| 撤离/战败结算 | 纯色底和文本 | 胜利/撤离结算背景、战败结算背景、结算清单面板、胜败插图。 |
| 出发深渊层选择 | 纯色卡片列表 | 层入口卡片皮肤、锁定/可进入状态图标、层缩略图或入口插图。 |
| 背包网格 | 物品图标已接，格子仍是程序色块 | 可用格、锁定格、悬停格、放置成功/失败反馈、底盘框层级设计。 |
| 工坊出售面板 | 物品图标已接，面板仍是程序色块 | 出售面板皮肤、列表行皮肤、按钮皮肤、金币/价值小图标。 |
| 义体制造面板 | 义体图标已接，面板仍是程序色块 | 制造面板皮肤、材料需求行皮肤、已装备/可制造状态图标。 |
| 战斗 HUD | 战斗背景和怪物头像已接 | 怪物卡片框、玩家魔偶站位、血条/护盾/AP/SAN UI 皮肤、行动按钮皮肤。 |
| 深渊地图 | 背景和节点图标部分接入 | 阶梯节点图标、节点底板、路线连接线、已访问/可访问/锁定状态皮肤。 |

---

## 6. 建议同步给美术的优先级

P0：立即补齐

* `node_stairs_icon`

P1：已有资源落界面前需要版式确认

* `doll_proto_0_stand` 的工坊/战斗摆放建议。
* `chassis_*_frame` 与背包格子的层级和视觉方案。

P1：MVP 体验明显提升的界面资源

* 战斗战利品拾取面板皮肤。
* 撤离/战败结算面板皮肤。
* 安全屋/阶梯房间背景或插图。

P2：整体 UI 皮肤系统

* 通用按钮。
* 通用弹窗面板。
* 列表行底板。
* 标题装饰。
* 状态图标：锁定、已选、可进入、已装备、可制造。

---

## 7. 程序侧当前可直接接入的命名规则

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

