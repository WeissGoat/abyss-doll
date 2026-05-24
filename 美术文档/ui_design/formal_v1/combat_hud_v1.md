---
id: art_ui_formal_v1_combat_hud
title: 战斗界面 Formal V1
type: art
role: 美术
domain: ui_design
status: active
source_of_truth: true
related:
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/archive/11_P0_UI骨架接入交付.md
last_verified: 2026-05-25
update_rule: 修改战斗界面正式结构、敌我站位、背包交互区、战斗实体素材或程序迁移要求时同步本文档。
---

# 战斗界面 Formal V1

> **目标：** 把战斗界面从 MVP 的“敌人卡片 + 背包 + 状态框”迁移为正式战斗舞台：左侧玩家魔偶，右侧敌方实体，中间保留行动反馈和 VFX 空间，背包作为战斗核心操作盘固定在底部居中。

结构对接文件：

* `美术文档/ui_design/screen_layouts.json`
* `美术文档/ui_design/component_catalog.json`
* `美术文档/art_requirements_seed.json`

---

## 1. 已确认结论

| 项 | Formal V1 结论 |
|---|---|
| 背包位置 | 底部居中。保持 100x100 玩法格和 5 间距，不为了布局压缩格子。 |
| 怪物表现 | 不再以敌人卡片作为主表现。优先使用 `MonsterEntity.CombatVisualID` 对应的战斗实体图。 |
| 现有怪物图 | 当前 4 张 `portrait` 有明显暗色背景或头像/半身裁切，只作为历史 fallback；Formal V1 已补齐对应 `monster_*_combat`。 |
| 敌人血条 | 放在敌人实体脚下，护盾条同组贴近，不放回卡片框。 |
| 中央区域 | 美术侧定为 `vfx_space`，用于攻击轨迹、伤害数字、命中反馈和状态变化，不放固定面板。 |

---

## 2. 现有怪物素材评估

| VisualID | 评估 | 结论 |
|---|---|---|
| `monster_mob_scavenger_bug_portrait` | 形体可读，但带暗色背景和场景阴影。 | 仅作为历史 fallback；正式接入使用 `monster_mob_scavenger_bug_combat`。 |
| `monster_mob_acid_slime_portrait` | 轮廓适合实体，但背景不是透明，主体偏图标/头像构图。 | 仅作为历史 fallback；正式接入使用 `monster_mob_acid_slime_combat`。 |
| `monster_elite_scrap_guard_portrait` | 半身头像，缺少脚底和战斗站位基准。 | 仅作为历史 fallback；正式接入使用 `monster_elite_scrap_guard_combat`。 |
| `monster_elite_mutant_amalgam_portrait` | 形体强，但背景和构图仍是 portrait。 | 仅作为历史 fallback；正式接入使用 `monster_elite_mutant_amalgam_combat`。 |

正式战斗实体素材规格：

```text
Source: png, transparent background, alpha required
SourceSize: 1024x1024
Display: contain in ~360x420 ui_px container
Pivot: bottom_center
Composition: full-body battle stance, side or three-quarter side view
Required: visible feet/body base, clear silhouette, no baked background
```

---

## 3. 结构布局

参考分辨率：`1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│                    Turn / Intent Banner                    │
│                                                            │
│  Player Stage             VFX Space          Enemy Stage    │
│  Doll + shadow        hit / damage / FX      1-3 entities   │
│                                             HP bars at feet │
│                                                            │
│                    Target Hint Panel                       │
│                    Action Strip                            │
│                 Centered Combat Backpack                   │
│  Player Status      100x100 grid + item layer               │
└────────────────────────────────────────────────────────────┘
```

### 主区域

| ZoneID | Rect | 目标 |
|---|---|---|
| `battle_background` | `0,0 1920x1080` | 战斗背景，cover 适配，不承载关键交互。 |
| `turn_banner` | `660,32 600x84` | 当前回合、敌方行动、目标选择提示。 |
| `player_stage` | `100,190 440x620` | 玩家魔偶、脚底阴影、受击/VFX 挂点。 |
| `enemy_stage` | `1240,160 600x500` | 1-3 个敌方实体站位、点击热区、脚下血条和选中光环。 |
| `vfx_space` | `560,180 620x240` | 攻击轨迹、投射物、伤害数字、命中反馈。 |
| `target_hint` | `680,430 560x68` | 当前选中物品、AP 消耗、目标选择提示。 |
| `action_strip` | `680,510 560x84` | 结束回合、取消选择、临时战斗操作按钮。 |
| `combat_backpack` | `680,610 560x440` | 战斗背包，底部居中，100x100 格。 |
| `player_status_cluster` | `120,830 500x150` | 玩家 HP、护盾、SAN、AP。 |

---

## 4. 敌方实体结构

Formal V1 中，敌人是舞台实体，不是 UI 卡片。

```text
EnemySlot_*
  EnemyClickHotspot_Button
  EnemyShadow_Image
  EnemyTargetRing_Image
  EnemySprite_Image
  EnemyFootHpBar
  EnemyFootShieldBar
  EnemyIntentIcon_or_Text
  EnemyStatusIcons
  EnemyName_Text
```

布局规则：

| 敌人数 | 布局 |
|---|---|
| 1 | 右中单体，尺寸最大，脚下血条居中。 |
| 2 | 右侧前后/上下错位，当前目标可略前移或加高亮。 |
| 3 | 三角站位，前排 1 个、后排 2 个，避免横向排成列表。 |

表现规则：

* `EnemySprite_Image` 优先绑定 `CombatVisualID`。
* `PortraitID` 只作为缺图时的临时 fallback，不作为 Formal V1 验收标准。
* `EnemyTargetRing_Image` 位于实体脚下，表达可选中/已选中目标。
* `EnemyFootHpBar` 和 `EnemyFootShieldBar` 贴近脚下，不进入头像框或卡片框。
* 阴影、光环和状态条默认 `raycastTarget=false`，点击只由 hotspot 或实体 hit area 接收。

---

## 5. 玩家、背包和行动区

玩家侧：

```text
PlayerStageRoot
  PlayerShadow_Image
  PlayerDoll_Image
  PlayerVfxAnchor
```

玩家状态：

| 状态 | 表现 |
|---|---|
| HP | 红色条 + 数值，放在 `player_status_cluster`。 |
| Shield | 蓝灰色条，位于 HP 下方。 |
| SAN | 冷紫/蓝色短条或数值，不抢 HP 权重。 |
| AP | pip 或小齿轮点，靠近行动区。 |

战斗背包：

```text
CombatBackpack
  ChassisFrame
  GridContainer
  InventoryItemLayer
  SelectionOverlay
```

原则：

1. 背包底部居中，是战斗操作核心，不再挤到右下角。
2. 背包格固定 `100x100`，间距 `5`；正式结构适配优先移动容器，不缩小玩法格。
3. `ActionStrip` 紧贴背包上方，承载结束回合和取消选择。
4. `TargetHint` 位于行动区上方，展示当前物品、AP 消耗和目标提示。

---

## 6. 资源策略

### 第一轮继续复用

| VisualID | 用途 |
|---|---|
| `bg_combat_abyss` | 战斗背景。 |
| `doll_proto_0_stand` | 玩家魔偶临时实体。 |
| `ui_panel_info` | 玩家状态、目标提示承托。 |
| `ui_button_primary` | 结束回合。 |
| `ui_button_secondary` | 取消选择/次级操作。 |
| `ui_combat_status_bar_hp` | 玩家和敌人 HP 条轨道。 |
| `ui_combat_status_bar_shield` | 玩家和敌人护盾条轨道。 |
| `ui_combat_ap_pip` | AP 点。 |
| `ui_combat_turn_banner` | 回合提示条。 |
| `ui_inventory_chassis_panel` | 背包底盘。 |
| `ui_inventory_slot_*` | 背包格状态。 |

### Formal V1 新增

| VisualID | 来源 | 用途 | 当前状态 |
|---|---|---|---|
| `monster_mob_scavenger_bug_combat` | `CombatVisualID` | 拾荒虫战斗实体。 | 已入库 Approved |
| `monster_mob_acid_slime_combat` | `CombatVisualID` | 酸液软体战斗实体。 | 已入库 Approved |
| `monster_elite_scrap_guard_combat` | `CombatVisualID` | 废铁守卫战斗实体。 | 已入库 Approved |
| `monster_elite_mutant_amalgam_combat` | `CombatVisualID` | 畸变融合体战斗实体。 | 已入库 Approved |
| `ui_combat_entity_shadow` | preset | 玩家/敌人脚底阴影。 | 已入库 Approved |
| `ui_combat_target_ring` | preset | 当前目标脚下光环。 | 已入库 Approved |

### P1 战斗可读性新增

| 类别 | VisualID | 用途 | 当前状态 |
|---|---|---|---|
| 怪物意图 | `ui_combat_intent_attack` / `defend` / `buff` / `debuff` / `grid_lock` / `add_junk` / `move_item` / `san_pressure` / `charge` / `unknown` | 敌人下一行动预告。伤害、护盾、倒计时和数量由 Unity Text 叠加，图标不烘焙文字或数字。 | 已写入 active 规格与 Manifest seed，待跑图。 |
| 战斗状态 | `ui_combat_status_corrosion` / `curse` / `stun` | 腐蚀、诅咒、眩晕等短状态图标。 | 已写入 active 规格与 Manifest seed，待跑图。 |
| 短暂反馈 | `ui_combat_feedback_hit` / `ui_combat_feedback_shield_break` | 命中、伤害数字承托、破盾短反馈，挂在 `VfxLayer`。 | 已写入 active 规格与 Manifest seed，待跑图。 |
| 背包干扰标记 | `ui_combat_grid_lock_marker` / `ui_combat_junk_preview_marker` | 封格和塞包预告 overlay，严格对齐 100x100 背包格。 | 已写入 active 规格与 Manifest seed，待跑图。 |

规格约束：

* 意图图标源图 `512x512`、透明 PNG，运行时建议 `64x64`。
* 状态图标源图 `512x512`、透明 PNG，运行时建议 `48x48`。
* 命中 / 破盾反馈源图 `512x512`、透明 PNG，运行时建议 `140x140`。
* 背包格 overlay 源图 `256x256`、透明 PNG，运行时固定 `100x100`，不参与 `LayoutGroup` 尺寸计算。

---

## 7. 程序迁移要求

| 模块 | MVP | Formal V1 |
|---|---|---|
| 敌人容器 | `EnemyCardsRoot` | `EnemyStageRoot` |
| 敌人表现 | 卡片内 portrait | 舞台实体 `EnemySprite_Image` |
| 敌人点击 | 点击卡片 | 点击实体 hotspot 或 hit area |
| 敌人血条 | 卡片内 | 实体脚下 |
| 敌人选中 | selected card sprite | target ring / outline / tint |
| 敌人意图 | 文本或无明确承托 | `EnemyIntentAnchor` 图标 + 运行时数值文本 |
| 敌人状态 | 卡片/文本混合 | `EnemyStatusIcons` 图标组 |
| 背包位置 | 右下 | 底部居中 |
| 背包干扰 | 无明确视觉层 | `GridLockMarker` / `JunkPreviewMarker` 对齐 100x100 slot |
| VFX 空间 | 无明确区域 | `VfxLayer` 位于玩家与敌人之间 |

建议 Unity 层级：

```text
CombatPanel
  BackgroundImage
  StageRoot
    PlayerStageRoot
      PlayerShadow_Image
      PlayerDoll_Image
      PlayerVfxAnchor
    EnemyStageRoot
      EnemySlot_0
      EnemySlot_1
      EnemySlot_2
      EnemySlot_*/EnemyIntentAnchor/IntentIcon_Image
      EnemySlot_*/EnemyStatusIcons/StatusIcon_Template
    VfxLayer
      HitFeedback_Template
      ShieldBreakFeedback_Template
  TurnBanner
  TargetHintPanel
  ActionStrip
  PlayerStatusCluster
  CombatBackpackAnchor
  InventoryCanvas/GridContainer/GridLockMarker_Template
  InventoryCanvas/GridContainer/JunkPreviewMarker_Template
```

---

## 8. 验收标准

Formal V1 接入后，ArtAcceptance 至少检查：

1. 截图中玩家实体位于左侧，敌方实体位于右侧，敌人不再以大卡片作为主表现。
2. 背包位于底部居中，100x100 格和 5 间距保持不变。
3. 敌人 HP/Shield 条贴近敌人脚下，目标选中光环或等效高亮可见。
4. 中央留有可见战斗空间，未被背包、状态框或固定面板遮满。
5. `EnemySprite_Image` 优先使用 `CombatVisualID`；若临时 fallback 到 `PortraitID`，验收记录必须标注为素材缺口。
6. 背景、阴影、状态条、目标光环、VFX 层不拦截敌人点击、背包点击或按钮点击。
7. 敌人意图图标位于敌人实体上方或近侧，运行时数字/倒计时由 Text 叠加，图标本身不含文字、字母或数字。
8. 敌人状态图标不遮挡 `EnemyClickHotspot_Button`，点击敌人主体和脚下光环仍能选中目标。
9. 封格和塞包预告标记严格对齐 100x100 背包格，不改变 `GridContainer` 尺寸、格间距或物品拖拽层级。
10. 命中和破盾反馈只在 `VfxLayer` 短暂出现，不作为常驻面板，不阻挡敌人、背包或按钮射线。

---

## 9. 当前交付状态

* active `screen_layouts.json` 已切到 `combat_hud` Formal V1 active spec。
* Manifest 扫描器已把 `MonsterEntity.CombatVisualID` 扫出为 `monster_*_combat` 战斗实体需求。
* 美术侧已完成第一批战斗实体和战斗 UI 辅助素材入库：4 个 `monster_*_combat`、`ui_combat_entity_shadow`、`ui_combat_target_ring`。
* P1 战斗可读性增补已写入 active UI 规格、组件目录和 preset seed：意图图标、状态图标、命中/破盾反馈、封格/塞包 overlay。
* 下一步刷新 Manifest / Prompt / 可接入清单；素材生成完成后再交 UI 程序侧接入并由美术侧用 ArtAcceptance 截图验收。

