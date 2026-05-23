---
id: art_ui_formal_v1_combat_hud
title: 战斗界面 Formal V1
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: true
related:
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/11_Alpha_P0_UI骨架接入交付.md
last_verified: 2026-05-23
update_rule: 修改战斗界面正式结构、敌我站位、背包交互区或程序迁移要求时同步本文件。
---

# 战斗界面 Formal V1

> **目标：** 把战斗界面从 MVP 的“敌人卡片 + 背包 + 状态框”改为正式版战斗舞台：左侧玩家人偶，右侧敌方实体，中间留给行动、攻击、受击和 VFX，背包作为核心战斗操作盘固定在下方或右下。

---

## 1. 参考方向

结构参考不是照抄，而是取核心关系：

| 游戏 | 可参考点 |
|---|---|
| 杀戮尖塔 | 左玩家、右敌人、敌人实体在战斗舞台上，血条和意图贴近实体。 |
| 背包英雄 | 背包是战斗操作核心，格子与物品状态必须长期稳定、清晰、可交互。 |

本项目正式战斗界面应结合两者：

```text
战斗舞台：玩家人偶 vs 深渊敌方实体
核心操作：背包物品选择、AP 消耗、目标选择
反馈空间：攻击、受击、护盾、SAN、伤害数字、状态图标
```

---

## 2. MVP Baseline 问题

| 项目 | MVP 结构 | 问题 |
|---|---|---|
| 敌人表现 | 右上敌人卡片 | 敌人像 UI 卡，不像战斗实体；后续动画和受击表现空间不足。 |
| 玩家状态 | 底部状态框集中显示 | HP、Shield、SAN、AP 容易拥挤，和玩家人偶关系弱。 |
| 战斗空间 | 背景上叠 UI | 中央攻击、弹道、伤害数字、状态变化缺少明确舞台。 |
| 背包 | 右下功能格 | 可用，但像独立调试面板，和行动区关系还不够正式。 |
| 目标选择 | 点击敌人卡片 | 后续应点击敌人实体或敌人站位热区。 |

---

## 3. Formal V1 结构

参考分辨率：`1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│                      Turn / Intent Banner                  │
│                                                            │
│   Player Stage                         Enemy Stage         │
│   ┌──────────────┐        VFX Space      ┌──────────────┐  │
│   │ Doll Entity  │   ───────────────▶    │ Enemy Entity │  │
│   │ HP/SAN/AP    │                       │ HP/Intent    │  │
│   └──────────────┘                       └──────────────┘  │
│                                                            │
│           Combat Log / Target Hint / Selected Item         │
│                                                            │
│   Action Strip                  Backpack Combat Board      │
│   End Turn / Cancel             100x100 grid + items       │
└────────────────────────────────────────────────────────────┘
```

### 主区域

| ZoneID | Rect | 目标 |
|---|---|---|
| `battle_background` | `0,0 1920x1080` | 战斗背景，cover 适配，不承载关键交互。 |
| `turn_banner` | `660,32 600x84` | 当前回合、敌方行动提示、目标提示的轻量横条。 |
| `player_stage` | `120,230 420x660` | 玩家人偶实体、脚底基线、受击/VFX 挂点。 |
| `enemy_stage` | `980,170 760x560` | 1-3 个敌人实体站位，非卡片主表现。 |
| `vfx_space` | `520,180 520x520` | 攻击、投射物、伤害数字、状态变化的中央空间。 |
| `player_status_cluster` | `120,820 520x150` | 玩家 HP、Shield、SAN、AP，贴近玩家侧。 |
| `target_hint` | `650,760 560x88` | 当前选中物品、AP 消耗、目标选择提示。 |
| `action_strip` | `650,880 560x110` | 结束回合、取消选择、可选战斗按钮。 |
| `combat_backpack` | `1260,560 560x440` | 战斗背包，保持 100x100 格和 5 间距。 |

---

## 4. 敌方实体结构

敌人不再以 `ui_combat_enemy_card` 作为主要表现。Formal V1 中，敌人应是右侧舞台内的实体：

```text
EnemySlot
  EnemyClickHotspot_Button
  EnemyShadow_Image
  EnemySprite_Image
  EnemyHpBar
  EnemyShieldBar
  EnemyIntentIcon_Image
  EnemyStatusIcons
  EnemyName_Text
```

第一轮可继续使用现有怪物 portrait 作为 `EnemySprite_Image` 的临时实体图，但显示方式要像站在舞台上的敌人，而不是放在卡框里。

### 敌人数量布局

| 敌人数 | 布局 |
|---|---|
| 1 | 右中单体，尺寸最大。 |
| 2 | 右侧上下或前后错位，当前目标稍微前移/高亮。 |
| 3 | 三角站位，前排 1 个、后排 2 个，避免横向排成 UI 列表。 |

---

## 5. 玩家结构

玩家人偶保留左侧实体表现：

```text
PlayerSlot
  DollShadow_Image
  DollSprite_Image
  PlayerStatusCluster
  PlayerStatusIcons
  HitVfxAnchor
```

玩家状态不再塞进一个拥挤面板，而是拆成：

| 状态 | 表现 |
|---|---|
| HP | 红色条 + 数值，最优先。 |
| Shield | 蓝色/青色条，位于 HP 下方。 |
| SAN | 紫色/冷色短条或数值，靠近角色但不抢 HP。 |
| AP | pip 或小齿轮点，贴近行动区。 |

---

## 6. 背包与行动区

背包仍是战斗核心，不缩小格子。

```text
CombatBackpack
  ChassisFrame
  GridContainer
  InventoryItemLayer
  SelectionOverlay
```

行动区应靠近背包：

```text
ActionStrip
  SelectedItemSummary
  EndTurn_Button
  CancelSelection_Button
  OptionalAction_Button
```

原则：

1. 玩家选择武器后，`target_hint` 提示选择敌人。
2. 可攻击敌人实体出现高亮，不依赖大卡片。
3. 背包格合法/非法、高亮、hover 状态继续使用现有 `ui_inventory_slot_*`。
4. `EndTurn_Button` 仍使用 `ui_button_primary` 或更高优先级按钮皮肤。

---

## 7. 资源策略

### 第一轮可复用

| VisualID | 用途 |
|---|---|
| `bg_combat_abyss` | 战斗背景。 |
| `doll_proto_0_stand` | 玩家人偶临时实体。 |
| `ui_panel_info` | 状态承托底板。 |
| `ui_button_primary` | 结束回合。 |
| `ui_button_secondary` | 取消/次级操作。 |
| `ui_combat_status_bar_hp` | HP 条底图。 |
| `ui_combat_status_bar_shield` | Shield 条底图。 |
| `ui_combat_ap_pip` | AP 点。 |
| `ui_inventory_chassis_panel` | 背包底盘。 |
| `ui_inventory_slot_*` | 背包格状态。 |

### 建议新增

| VisualID | 用途 | 是否阻塞 Formal V1 |
|---|---|---|
| `ui_combat_enemy_target_ring` | 敌人实体选中/可攻击光圈。 | 不阻塞，可先用 tint/outline。 |
| `ui_combat_entity_shadow` | 玩家和敌人脚下阴影。 | 不阻塞，可先用半透明椭圆。 |
| `ui_combat_intent_attack` | 敌人攻击意图图标。 | 不阻塞，可先用文本。 |
| `ui_combat_intent_defend` | 敌人防御意图图标。 | 不阻塞。 |
| `ui_combat_status_cluster` | 玩家状态组底板。 | 不阻塞，可先复用 `ui_panel_info`。 |

---

## 8. 程序迁移要求

| 模块 | MVP | Formal V1 |
|---|---|---|
| 敌人容器 | `EnemyCardsRoot` | `EnemyStageRoot` |
| 敌人表现 | 卡片内 portrait | 舞台实体 `EnemySprite_Image` |
| 敌人点击 | 点击卡片 | 点击 `EnemyClickHotspot_Button` 或实体 hit area |
| 敌人血条 | 卡片内 | 敌人头顶/脚下小型状态条 |
| 敌人选中 | selected card sprite | target ring / outline / tint |
| 玩家状态 | 单个信息框 | 拆分状态 cluster |
| VFX 空间 | 无明确区域 | `VfxLayer` 在玩家与敌人之间 |

建议 Unity 层级：

```text
CombatPanel
  CombatBackground_Image
  StageRoot
    PlayerStageRoot
      PlayerDoll_Image
      PlayerShadow_Image
      PlayerVfxAnchor
    EnemyStageRoot
      EnemySlot_0
      EnemySlot_1
      EnemySlot_2
    VfxLayer
  TurnBanner
  PlayerStatusCluster
  TargetHintPanel
  ActionStrip
  CombatBackpackAnchor
```

---

## 9. 验收标准

Formal V1 接入后，ArtAcceptance 至少检查：

1. 截图中玩家实体位于左侧，敌方实体位于右侧，不再以大卡片作为主要敌人表现。
2. 中央留有可见战斗舞台空间，未被背包、状态框或敌人信息遮满。
3. 背包格仍为 100x100，物品显示和点击/拖拽不受影响。
4. 敌人实体有可点击热区，选中态能被截图看出。
5. 玩家 HP、Shield、SAN、AP 可读，且不明显压叠。
6. 结束回合按钮可见并可点击。
7. 背景、阴影、状态条、装饰图不拦截敌人点击、背包点击或按钮点击。

---

## 10. 待确认问题

1. 战斗背包最终放右下还是底部横向操作盘？当前建议右下，保留现有背包接入成本。
2. 敌人实体第一轮是否继续用 portrait，还是立刻补 `monster_*_battle_stand`？
3. 敌人意图第一轮用文本还是新增 icon？
4. 是否需要在 Formal V1 同时加入简单战斗日志，还是先用目标提示替代？
