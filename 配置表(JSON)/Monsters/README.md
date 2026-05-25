---
id: config_monsters_readme
title: 深渊怪物配置字段说明 (Monsters Config)
type: config
role: 策划
domain: config_monsters
status: active
source_of_truth: true
related:
  - 开发文档/03_深渊与战斗循环(DungeonCombat).md
  - 开发文档/11_怪物AI与行动系统(MonsterActionAI).md
  - 数值模型设计/02_战斗伤害与生存公式.md
  - 配置表(JSON)/README.md
  - 设计文档/28_Monsters正式配置承接审计.md
  - 设计文档/29_Dungeons正式配置承接审计.md
  - 设计文档/30_Rewards正式配置承接审计.md
  - 设计文档/31_第一层正式配置落地设计.md
  - 设计文档/32_第二层正式配置落地设计.md
  - 设计文档/33_第三层正式配置落地设计.md
last_verified: 2026-05-25
update_rule: 修改配置字段、数据源规则或表间引用时同步本文件。
---

# 深渊怪物配置字段说明 (Monsters Config)

> 本目录下的 JSON 文件定义深渊战斗节点中遭遇的敌对实体。怪物的攻击、技能、背包干涉统一通过 `AI.Actions` 配置，不再使用旧的 `DamageValue`、`AttacksPerTurn`、`GridInterference` 字段。

正式配置字段、前三层怪物 / Boss 内容缺口、Boss / 精英职责拆分和 Validator 建议见 `设计文档/28_Monsters正式配置承接审计.md`。该审计是配置改造依据，不代表本文字段已经全部落地。

## 正式配置基本规则

`Monsters` 是深渊战斗遭遇的静态配置源。正式怪物配置必须回答五个问题：

1. 这只怪物承担什么教学、检验、压力或 Boss 职责。
2. 它属于哪些层级、路线主题和内容包。
3. 玩家在行动前能看到什么意图、数值预告和反制提示。
4. 它如何通过 `AI.Actions`、意图序列、阶段规则和目标规则执行行为。
5. 它击败后如何进入 Rewards、成长材料、房间记忆或剧情回流。

运行时战斗状态不得写入静态怪物 JSON。`CurrentHP`、`CurrentShield`、`CurrentIntent`、冷却剩余、充能层数、阶段索引、临时效果和掉落 roll 结果只属于战斗实例或战斗日志。

当前 `Monsters/*.json` 是可运行基线，不代表正式字段已完成。后续 JSON 补齐前，先以本文字段口径和 `28_Monsters正式配置承接审计.md` 为准。

## 字段说明表

| 字段名 (Key) | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `MonsterID` | string | 怪物的全局唯一 ID | 必填项，如 `elite_mutant_amalgam` |
| `Name` | string | 怪物显示名称 | |
| `DisplayNameKey` | string | 文案表键 | 正式配置建议必填；测试怪可留空但 Validator 输出 warning |
| `MonsterRole` | enum 或 array | 怪物玩法职责 | `Tutorial`, `Pressure`, `CounterCheck`, `Elite`, `Boss`, `RouteGate`, `SanPressure`, `PackInterference`, `Corruption`, `Narrative` |
| `ContentPackRefs` | array<string> | 内容包归属 | 例如 `ContentPack19`, `ContentPack20`, `ContentPack25` |
| `LoreTags` | array<string> | 世界观、生态和剧情标签 | 用于剧情、房间记忆和美术需求筛选 |
| `Layer` | int | 推荐出没的深渊层数 | 用于标识怪物的数值跨度级别 |
| `LayerRange` | object | 可出现层级区间 | 例如 `{ "Min": 1, "Max": 2 }`；与旧 `Layer` 兼容 |
| `EncounterTags` | array<string> | 遭遇标签 | `Combat`, `Elite`, `Boss`, `SafeRoute`, `HighRisk`, `PackPressure`, `SanPressure` |
| `DifficultyBudget` | int 或 object | 难度预算 | 用于 MapProfile 和节点池控强度 |
| `SpawnWeightTags` | array<string> | 生成权重标签 | 用于路线主题、seed 验收和后续生成器筛选 |
| `HP` | int | 怪物生命值上限 | 测试 DPS 输出检测的沙袋血量 |
| `IntentList` | array<object> | 玩家可见意图定义 | 详见 `IntentList` 字段组 |
| `IntentSequence` | array<string> | 固定或半固定意图序列 | 用于蓄力、Boss 阶段和教学怪 |
| `ActionPattern` | object | 行动循环、选择器和节奏规则 | 与 `AI.Actions` 配套 |
| `PhaseRules` | array<object> | 阶段规则 | Boss 和精英使用；普通怪可为空 |
| `TargetRule` | object | 目标选择规则总口径 | 统一角色目标、背包目标和 fallback |
| `GridInterference` | object | 对包干涉配置组 | 正式字段组，不使用旧单字段裁决 |
| `RewardID` | string | 击败该怪物后触发的奖励表 ID | 指向 `/Rewards` |
| `DropThemeTags` | array<string> | 掉落主题标签 | 辅助 Rewards / Orders / 传闻连接 |
| `BossGuaranteedRefs` | array<string> | Boss 保底奖励引用 | 只有正式 Boss 或明确 `CanActAsBoss=true` 的怪物使用 |
| `MemoryRewardRefs` | array<string> | 击败后可产生的房间记忆 / 日记引用 | 精英、Boss、剧情怪优先填写 |
| `PortraitID` | string | 怪物头像 VisualID | 用于战斗 HUD / 图鉴 / 结算 |
| `CombatVisualID` | string | 战斗实体 VisualID | 正式战斗表现和 ArtAcceptance 依赖 |
| `IntentIconSet` | string | 意图图标集合 | 映射 `IntentType` 到 UI 表现 |
| `ActionVfxRef` | string 或 object | 行动 VFX 引用 | 可按 ActionID 分配 |
| `HitFeedbackRef` | string | 受击反馈引用 | 可选 |
| `DefeatFeedbackRef` | string | 击败反馈引用 | 可选 |
| `AI` | object | 怪物行动配置 | 必填，详见下方 `AI.Actions` |
| `LootPool` | array | 旧版掉落池 | Deprecated，仅奖励系统迁移期 fallback 使用 |

## IntentList (意图可读字段组)

`IntentList` 用来描述玩家在怪物行动前能看到什么，而不是直接执行行为。执行仍由 `AI.Actions` 和行动选择器负责。

| 字段名 | 数据类型 | 说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `IntentID` | string | 意图定义 ID | 通常与 ActionID 或阶段动作对应 |
| `IntentType` | enum | 玩家看到的意图类型 | `Attack`, `Defend`, `Charge`, `AddJunk`, `GridLock`, `CorrodeItem`, `PolluteItem`, `SanPressure`, `Summon`, `Buff`, `Debuff`, `BossPhase` |
| `PowerPreview` | int 或 string | 数值预告 | 例如伤害、护盾、塞包数量；不确定数值可用区间字符串 |
| `TargetPreview` | enum 或 string | 目标预告 | `Doll`, `LowestHpDoll`, `RandomDoll`, `Weapon`, `BackpackEmptySlot`, `BackpackEdge`, `WeakSlot`, `RouteState` |
| `Reliability` | enum | 意图可靠度 | `Fixed`, `High`, `Medium`, `Low`, `HiddenBoss` |
| `CounterHintKey` | string | 反制提示文案键 | 例如提示用溶剂、护盾、清格或优先击杀 |
| `IntentIconRef` | string | 单独意图图标 VisualID | 可覆盖 `IntentIconSet` 默认图标 |

## 行为与阶段字段组

| 字段名 | 数据类型 | 说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `ActionPattern.Selector` | string | 行动选择器 | `WeightedRandom`, `Sequence`, `PhaseSequence`, `ConditionalPriority` |
| `ActionPattern.CooldownPolicy` | string | 冷却策略 | `PerAction`, `SharedGroup`, `None` |
| `ActionPattern.FallbackAction` | string | 条件不满足时的备用行动 | 必须指向 `AI.Actions.ActionID` |
| `PhaseRules[].Trigger` | string | 阶段切换条件 | `HpBelowPercent`, `TurnReached`, `RouteCorrosionAbove`, `SummonsDefeated` |
| `PhaseRules[].ActionOverrides` | array<string> | 阶段动作覆盖 | 指向 ActionID |
| `PhaseRules[].IntentOverrides` | array<string> | 阶段意图覆盖 | 指向 IntentID |

## TargetRule (目标选择字段组)

| 字段名 | 数据类型 | 说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `TargetRule.CharacterTarget` | enum | 角色目标规则 | `FirstAlivePlayer`, `RandomPlayer`, `LowestHpPlayer`, `HighestThreatPlayer` |
| `TargetRule.ItemTargetFilter` | object | 物品目标筛选 | 例如标签、类型、稀有度、是否可腐蚀 |
| `TargetRule.GridTargetRule` | enum | 网格目标规则 | `FirstFit`, `RandomEmpty`, `EdgeEmpty`, `LargestFreeArea`, `AdjacentToWeapon`, `WeakSlot` |
| `TargetRule.RetargetPolicy` | enum | 目标失效后处理 | `FallbackAction`, `RetargetOnce`, `SkipAction`, `UseBasicAttack` |

## GridInterference (对包干涉字段组)

正式对包干涉必须引用统一 profile，不在怪物行动中临时写裁决。

| 字段名 | 数据类型 | 说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `GridLockProfile` | string | 封格 / 封边规则 | 例如锁定边缘空格、弱槽或随机空格 |
| `AddJunkProfile` | string | 塞入废料 / 诅咒物规则 | 需要对应 Items 配置 |
| `MoveItemProfile` | string | 推挤 / 移动物品规则 | 高风险机制，默认不启用 |
| `PollutionProfile` | string | 污染物品或背包规则 | 第三层优先使用 |
| `CorrodeItemProfile` | string | 腐蚀武器 / 防具 / 材料规则 | 需要玩家可见预告 |

## AI.Actions

怪物每回合默认选择并执行一个 Action。普通攻击也是 Action；如果想表达旧版“每回合多段攻击”，请在 `DamageTarget.Params.RepeatCount` 中配置。

```json
{
  "AI": {
    "Selector": "WeightedRandom",
    "Actions": [
      {
        "ActionID": "acid_corrode_weapon",
        "ActionType": "ReduceWeaponDamage",
        "Target": "RandomPlayerWeapon",
        "Weight": 30,
        "CooldownTurns": 2,
        "UsesPerCombat": 0,
        "Condition": "PlayerHasWeapon",
        "Params": {
          "Multiplier": 0.5,
          "DurationPlayerTurns": 1
        }
      }
    ]
  }
}
```

| 字段名 | 数据类型 | 说明 |
| :--- | :--- | :--- |
| `Selector` | string | 行动选择器。MVP 支持 `WeightedRandom`。 |
| `ActionID` | string | 行动实例 ID，用于日志、冷却、测试定位。 |
| `ActionType` | string | 行动类型。当前支持 `DamageTarget`、`ReduceWeaponDamage`、`AddCursedItem`；正式版预留 `Defend`、`Charge`、`GridLock`、`PolluteItem`、`CorrodeItem`、`SanPressure`、`Summon`。 |
| `Target` | string | 目标选择器。MVP 支持 `FirstAlivePlayer`、`RandomPlayer`、`LowestHpPlayer`、`RandomPlayerWeapon`、`PlayerGridFirstFit`。 |
| `Weight` | int | 权重随机选择权重。小于等于 0 不会被选中。 |
| `CooldownTurns` | int | 行动使用后的敌方回合冷却。 |
| `UsesPerCombat` | int | 单场战斗最大使用次数，0 表示不限。 |
| `Condition` | string | 行动条件。MVP 支持 `Always`、`PlayerHasWeapon`、`PlayerGridHasSpace`。 |
| `IntentID` | string | 对应的玩家可见意图 | 正式配置建议高权重 Action 必填 |
| `Params` | object | 行动专属参数。 |

配置表保持字符串写法，程序会集中解析为运行时枚举并做校验。新增行为时，不要直接在代码里散落字符串比较；应同步扩展 `MonsterActionType`、`MonsterTargetType`、`MonsterActionConditionType` 与 `ConfigValidator`。

## RewardID

怪物不应该直接维护复杂掉落逻辑。击败怪物时，战斗节点优先读取怪物的 `RewardID`，再交给 `RewardSystem` 解析。

```json
{
  "MonsterID": "elite_scrap_guard",
  "RewardID": "reward_monster_elite_scrap_guard"
}
```

奖励表负责声明保底奖励、权重奖励、空掉落和奖励组合，详见 [`../Rewards/README.md`](../Rewards/README.md)。

## LootPool

`LootPool` 是早期 MVP 直连掉落字段。引入 `RewardSystem` 后，该字段只作为奖励系统迁移期 fallback 保留，不再承载怪物 AI 或技能逻辑。

---

## 正式怪物职责口径

| `MonsterRole` | 设计含义 | 字段要求 |
| :--- | :--- | :--- |
| `Tutorial` | 教学怪，低压解释规则 | 意图可靠、反制提示清晰、奖励简单 |
| `Pressure` | 普通压力怪 | 至少一个明确机制和一个基础攻击 |
| `CounterCheck` | 检查玩家是否带了反制物或构筑 | 必须有 `CounterHintKey` 和目标规则 |
| `PackInterference` | 背包干涉怪 | 必须配置 `GridInterference` 和失败 fallback |
| `SanPressure` | SAN / 情绪压力怪 | 必须有玩家可见 SAN 意图 |
| `Corruption` | 污染 / 腐蚀怪 | 必须配置风险 profile 和表现提示 |
| `Elite` | 精英怪，可绕或高收益 | 必须有高于普通怪的难度预算和奖励主题 |
| `Boss` | 层级 Boss | 必须有独立 `MonsterID`、`PhaseRules`、Boss 奖励和层级终点承接 |
| `RouteGate` | 路线门槛怪 | 必须能被 MapProfile / Dungeons 按路线引用 |
| `Narrative` | 剧情 / 房间记忆怪 | 必须有 `LoreTags`、`MemoryRewardRefs` 或事件引用 |

## Boss 与精英边界

正式配置中，精英怪不能默认兼任 Boss。若某个 `elite_*` 被临时挂在 `BossNode`，Validator 应输出 warning。

正式 Boss 要求：

* 使用 `boss_*` 命名。
* `MonsterRole` 包含 `Boss`。
* 配置 `PhaseRules` 或明确 `NoPhaseBoss=true` 的例外字段。
* 配置 `BossGuaranteedRefs` 或通过 `RewardID` 指向 Boss 奖励。
* 被 Dungeons 的 `BossNode` 引用，并与层级 SafeZone / Stairs / 撤离节奏闭环。

`elite_scrap_guard` 和 `elite_mutant_amalgam` 当前只应作为精英职责继续维护；一层 / 二层 Boss 应分别补 `boss_gatekeeper_mk1` 和 `boss_spore_foundry`。

## Validator 规则建议

P0 检查：

* `MonsterID` 全局唯一，且文件名与 `MonsterID` 一致。
* `RewardID` 必须能在 `Rewards` 找到。
* `AI.Actions` 非空，且 `ActionID` 在单怪内唯一。
* `ActionType`、`Target`、`Condition` 必须在合法枚举中。
* `Weight > 0` 的 Action 才参与选择；全为 0 必须报错。
* `DamageTarget` 必须有 `Damage`；`AddCursedItem` 必须有 `ItemID`；`ReduceWeaponDamage` 必须有 `Multiplier`。
* 正式怪物必须有 `PortraitID` 和 `CombatVisualID`，迁移期可 warning。
* Dungeons `BossNode` 引用 `elite_*` 时输出 warning，除非显式 `CanActAsBoss=true`。

正式配置检查：

* 进入当前批次的 `19/20/25` 怪物 / 精英 / Boss 必须存在对应 `MonsterID` 或明确替代 ID。
* 高权重 Action 必须能映射到 `IntentID`、`IntentType`、`PowerPreview` 和 UI 图标。
* 第一层至少覆盖攻击、防御、轻塞包、低压 SAN。
* 第二层至少覆盖塞包、封格、腐蚀、蓄力。
* 第三层至少覆盖污染、弱槽标记、路线侵蚀 Boss 变量。
* 对包干涉行动必须声明目标规则和失败 fallback，不允许静默覆盖已有物品。
* 新正式怪物不允许只配置 `LootPool`；`RewardID` 是奖励事实来源。

## 当前 JSON 状态

截至 2026-05-25，当前 4 个 `Monsters/*.json` 只代表可运行基线。它们尚未完整填写本文新增的正式字段，也尚未完成 `28_Monsters正式配置承接审计.md` 中列出的 Boss / 精英职责拆分、第一层教学链、第二层背包压力链和第三层污染机制样例。

后续推进顺序：

1. 先让 `elite_scrap_guard` / `elite_mutant_amalgam` 回归精英职责，并新增正式 Boss ID。
2. 再补第一层怪物教学链。
3. 再补第二层背包压力链和反制闭环。
4. 最后定义第三层污染、弱槽和路线侵蚀 Boss 样例。
