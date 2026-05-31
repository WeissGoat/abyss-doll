---
id: art_ui_formal_v2_combat_hud
title: Combat HUD Formal V2 方案
type: art
role: 美术
domain: ui_design
status: draft
source_of_truth: false
related:
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/ui_design/formal_v1/combat_hud_v1.md
last_verified: 2026-05-31
update_rule: 编写或确认 combat_hud Formal V2 详细方案时同步本文件。
---

# Combat HUD Formal V2 方案

> **状态：** Formal V2 设计草案。本文用于给用户确认 `combat_hud` 的正式化方向；确认并迁移到 active `screen_layouts.json` 前，不作为程序或素材生成规格。

---

## 1. Formal V1 问题

`combat_hud` Formal V1 已经完成关键结构转向：左玩家、右敌人、底部居中背包、敌人血条贴脚下。但运行时仍可能有以下 UX 问题：

| 问题 | 表现 | V2 处理 |
|---|---|---|
| 舞台感不足 | 背包、状态、意图、按钮可能挤满画面 | 固定“战斗舞台优先”，UI 压低到下方和两侧。 |
| 回合决策不聚焦 | 敌人意图、物品选择、AP、结束回合分散 | 建立“回合决策带”：选物品 -> 看目标 -> 执行 / 结束。 |
| 敌方像 UI 元素 | 实体、意图、血条仍可能像面板组 | 敌人作为舞台对象，UI 贴附实体，不包进大卡片。 |
| 背包指令区缺少战斗语义 | 背包只是格子 | 背包上方增加物品指令、AP 消耗、目标提示和干扰预告。 |

---

## 2. 玩家目标

战斗界面的玩家目标是：

1. 看懂敌人这一回合准备做什么。
2. 判断自己的 HP / SAN / AP 和背包状态。
3. 从背包选择物品或装备，对敌人或自身执行行动。
4. 理解行动结果和下一回合风险。

Formal V2 不应让玩家从一堆按钮里找“攻击”。核心交互应围绕背包物品、目标和回合行动展开。

---

## 3. Formal V2 体验定位

`combat_hud` 是战斗舞台 + 背包指令区。

参考方向可以接近《杀戮尖塔》的意图可读性和《背包英雄》的背包操作感，但本项目的结构重点是：

```text
左：玩家魔偶
中：战斗反馈 / VFX
右：敌方实体
下：背包指令区
```

Formal V2 的核心区别：

* 不再把敌人做成卡片主视觉。
* 不再让动作按钮抢走背包玩法的核心地位。
* 意图图标贴近敌人，行动选择贴近背包。
* 中央保留清楚的命中、弹道、伤害和异常反馈空间。

---

## 4. 主结构草案

参考分辨率 `1920x1080`。

```text
┌────────────────────────────────────────────────────────────┐
│ Turn Banner: Round / Phase / Short warning                 │
│                                                            │
│ Player Stage       VFX Corridor             Enemy Stage     │
│ doll, hp/san       hit, damage, effects     1-3 enemies     │
│                                                            │
│ Intent Row near enemies          Player risk near doll      │
│                                                            │
│ Item Command Strip: selected item / AP / target / cancel    │
│ Combat Backpack: centered 100x100 grid                      │
│ End Turn: one primary button, away from item drag path      │
└────────────────────────────────────────────────────────────┘
```

| ZoneID | 建议位置 | 作用 |
|---|---|---|
| `battle_scene` | `0,0 1920x1080` | 战斗背景。 |
| `turn_banner` | `660,32 600x76` | 回合、阶段、短警告。 |
| `player_stage` | `84,170 450x560` | 玩家魔偶、脚底阴影、HP / SAN / Shield 贴附。 |
| `enemy_stage` | `1180,130 660x560` | 1-3 个敌方实体、意图、状态、脚下血条。 |
| `vfx_corridor` | `540,190 620x360` | 伤害数字、弹道、命中、破盾、异常反馈。 |
| `command_strip` | `610,560 700x96` | 当前物品、AP 消耗、目标、取消选择。 |
| `combat_backpack` | `610,670 700x360` | 底部居中背包，格子 `100x100`。 |
| `end_turn_anchor` | `1330,810 260x88` | 结束回合唯一主按钮，避开拖拽路径。 |

---

## 5. 信息层级

默认阅读顺序：

1. 敌人意图：攻击、防御、腐蚀、塞包、封格、未知。
2. 玩家当前风险：HP、Shield、SAN、AP。
3. 背包可用行动：哪些物品可用、目标是谁、AP 是否足够。
4. 战斗反馈：刚刚发生了什么。
5. 细节文本：选中敌人、选中物品或悬停时才展开。

避免默认展示：

* 不默认展示长段战斗日志。
* 不把每个敌人所有属性塞进卡片。
* 不把所有操作按钮排成一行。

---

## 6. 行动层级

| 层级 | 行动 | 表达 |
|---|---|---|
| Primary | `EndTurn` 或当前确认行动 | 同屏唯一最高权重按钮。 |
| Core Interaction | 拖拽物品、选择目标、使用物品 | 背包格、目标环、command strip。 |
| Secondary | 取消选择、查看详情、切换目标 | 小按钮 / 图标。 |
| Danger | 撤离、放弃、丢弃关键物 | 默认不在普通战斗 HUD 展开。 |

V2 建议：

* 没有选中物品时，主按钮是 `EndTurn`。
* 选中可用物品且需要目标时，主提示转为“选择目标”，不要生成第二个同权按钮。
* 选中无需目标的物品时，`command_strip` 出现确认使用，但仍不得遮挡背包。

---

## 7. 按钮合并 / 降级 / 隐藏策略

| 当前元素 | V2 策略 |
|---|---|
| 普通攻击按钮 | 若攻击来自物品 / 装备，合并到背包物品交互；不作为常驻大按钮。 |
| 结束回合 | 保留为唯一主按钮，位置远离背包拖拽热区。 |
| 取消选择 | 降级为 `command_strip` 小按钮。 |
| 目标切换 | 优先点击敌人实体 / 目标环；不做大按钮列表。 |
| 战斗日志 | 收起为最近 2-3 条短反馈，完整日志进入可展开面板。 |
| 撤离 | 若当前规则允许，作为危险操作进入二次确认，不与结束回合同权。 |

---

## 8. 场景隐喻

`combat_hud` 的镜头语言：

* 横版舞台，玩家在左，敌人在右。
* 中央是攻击和受击路径。
* 敌人脚下有落点、血条和目标环。
* 背包像战术盘，放在画面下方，玩家从背包中“发动”行动。

素材提示词应描述“side-view battle stage, full body enemy, transparent combat sprite, readable silhouette”等视觉内容，不使用“UGUI”“程序绑定”“塔科夫like”等工具或黑话词。

---

## 9. 程序迁移影响

Formal V2 确认后，程序主要调整 HUD 层级、锚点和状态展示：

```text
CombatPanel
  BattleScene
  StageRoot
    PlayerStage
    VfxCorridor
    EnemyStage
  TurnBanner
  CommandStrip
  CombatBackpack
  EndTurnAnchor
```

关键绑定：

| 绑定 | 来源 |
|---|---|
| 敌人意图 | `CombatReadabilityTextService` / `MonsterIntentPreviewService`。 |
| 敌人实体 | `MonsterEntity.CombatVisualID`。 |
| 玩家状态 | `CombatSystem` / 战斗只读快照。 |
| 命中 / 破盾 / 封格 / 塞包 | `CombatTimelineRecorder` 或对应战斗事件。 |
| 背包格 | 现有全局背包显示层和 `InventoryDisplaySpec`。 |

约束：

* 背包格仍为 `100x100`，间距 `5`。
* 命中反馈、目标环、状态图标不拦截背包或敌人点击。
* UI Controller 不在表现层计算伤害、AP 或怪物行动。

---

## 10. 素材需求变化

第一版复用现有素材：

| VisualID | 用法 |
|---|---|
| `bg_combat_abyss` | 战斗背景。 |
| `doll_proto_0_stand` | 玩家魔偶临时实体。 |
| `monster_*_combat` | 敌人战斗实体。 |
| `ui_combat_entity_shadow` | 玩家 / 敌人脚底阴影。 |
| `ui_combat_target_ring` | 目标环。 |
| `ui_combat_intent_*` | 敌人意图图标。 |
| `ui_combat_status_*` | 状态图标。 |
| `ui_combat_feedback_*` | 命中 / 破盾短反馈。 |
| `ui_inventory_*` | 背包和格子。 |

可能新增但不立即跑图：

| 候选 VisualID | 说明 |
|---|---|
| `ui_combat_command_strip` | 选中物品 / AP / 目标提示条。 |
| `ui_combat_enemy_intent_bubble` | 意图图标的贴附气泡或小底板。 |
| `ui_combat_player_risk_plate` | 玩家 HP / SAN 风险小面板。 |

新增项只有确认并写入 active 后进入 Manifest。

---

## 11. UX 验收标准

运行时截图需要满足：

1. 玩家在左、敌人在右，敌人不是卡片主视觉。
2. 背包位于底部居中，格子 `100x100`，拖拽路径不被按钮遮挡。
3. 敌人意图 3 秒内可读，贴近敌人而不是挤在角落。
4. HP / Shield / SAN / AP 可读，但不抢占战斗舞台。
5. 中央有足够空间显示命中、伤害和 VFX。
6. 默认最高权重行动只有一个。
7. 近期战斗反馈可见，但长日志不压过背包和敌人。
8. 所有反馈图层不阻挡敌人选择、背包拖拽和结束回合按钮。

---

## 12. 用户确认问题

建议确认以下 4 点：

1. 是否把 `combat_hud` 锁定为“战斗舞台 + 底部背包指令区”的结构。
2. 普通攻击是否不再作为常驻大按钮，而尽量由背包物品 / 装备交互承载。
3. 敌人意图是否贴近敌方实体，并保留中央 `vfx_corridor`。
4. 结束回合是否作为默认唯一主按钮，位置避开背包拖拽路径。
