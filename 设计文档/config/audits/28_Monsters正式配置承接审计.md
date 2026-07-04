---
id: design_28_monsters_formal_config_audit
title: Monsters正式配置承接审计
type: audit
role: 策划
domain: formal_config_authoring
status: active
source_of_truth: true
related:
  - 设计文档/README.md
  - 设计文档/config/26_正式配置设计与填充推进计划.md
  - 设计文档/规则卡/03_战斗回合与怪物意图规则卡.md
  - 设计文档/delivery/15_P0主干配置表现验收承接清单.md
  - 设计文档/content_packs/19_第一层正式核心内容包.md
  - 设计文档/content_packs/20_第二层背包压力内容包.md
  - 设计文档/content_packs/25_第三层路线侵蚀内容包.md
  - 设计文档/config/designs/31_第一层正式配置落地设计.md
  - 设计文档/config/designs/32_第二层正式配置落地设计.md
  - 设计文档/config/designs/33_第三层正式配置落地设计.md
  - 设计文档/config/tasks/34_前三层正式配置实现任务拆分.md
  - 配置表(JSON)/Monsters/README.md
  - 配置表(JSON)/Rewards/README.md
  - 配置表(JSON)/Dungeons/README.md
  - agent_status/design.md
last_verified: 2026-05-25
update_rule: 调整 Monsters 正式配置字段、前三层怪物 / Boss 内容缺口、AI 行为承接、奖励引用或 Validator 建议时同步本文件。
---

# Monsters正式配置承接审计

> **定位：** 本文是 `26_正式配置设计与填充推进计划.md` 中 W1「配置承接审计」的第二份落地产物。它只审计 `Monsters` 配置域：当前 JSON、配置 README、战斗规则卡、P0 承接清单和前三层内容包之间是否已经对齐。它不直接改配置值，也不替代 `配置表(JSON)/Monsters/README.md`。

---

## 1. 审计结论

截至 2026-05-25，`配置表(JSON)/Monsters` 已具备最基础的怪物配置结构：怪物 ID、层级、HP、视觉 ID、奖励 ID 和 `AI.Actions`。但它仍停留在测试 / MVP 过渡结构，尚未达到正式配置承接标准。

当前结论：

| 项 | 判断 | 说明 |
|---|---|---|
| 字段结构 | 部分满足 | 已有 `MonsterID`、`Layer`、`HP`、`PortraitID`、`CombatVisualID`、`RewardID`、`AI.Actions`；缺正式版所需的怪物职责、意图预告、行为序列、反制说明、难度预算、遭遇标签和内容包归属。 |
| 首批内容量 | 明显不足 | 当前只有 4 个怪物 JSON；前三层内容包共规划 20 个怪物 / 精英 / Boss，当前只覆盖少量一二层样例。 |
| 战斗可读性 | 不足 | `AI.Actions` 能执行行为，但缺 `IntentType`、`PowerPreview`、`TargetPreview`、`CounterHint` 等玩家可见意图字段。 |
| Boss 承接 | 不满足 | 当前 `Dungeons/layer_1.json` 把 `elite_scrap_guard` 作为 Boss，`layer_2.json` 把 `elite_mutant_amalgam` 作为 Boss；内容包要求独立 Boss：`boss_gatekeeper_mk1`、`boss_spore_foundry`、`boss_mycelium_oracle`。 |
| 奖励引用 | 部分满足 | 4 个怪物都有 `RewardID` 且 Rewards 有对应表，但怪物 JSON 仍保留旧 `LootPool` fallback，正式内容应收口到 Rewards。 |
| Validator 支撑 | 不足 | 当前可校验基础字段和奖励引用，但尚不能校验意图可读性、行为类型覆盖、内容包覆盖、Boss 独立性和层级难度边界。 |

因此，`Monsters` 当前状态应标记为：

```text
规则可开发：已完成
配置可承接：进行中
正式配置填充：未完成
```

---

## 2. 当前配置事实

当前 `配置表(JSON)/Monsters` 共 4 个怪物：

| MonsterID | 当前层级 | 当前职责判断 | 当前问题 |
|---|---:|---|---|
| `mob_scavenger_bug` | 1 | 第一层基础攻击怪。 | 只会攻击；缺内容包要求的低概率防御、怪物职责、意图预告字段和层级 / 遭遇标签。 |
| `mob_acid_slime` | 2 | 酸液 / 腐蚀测试怪。 | 内容包第一层也需要 `mob_acid_slime` 作为轻度塞包教学怪；当前配置为 Layer 2 且行为是 `ReduceWeaponDamage`，与第一层轻塞包职责不一致，也不等同第二层 `mob_acid_slime_mature`。 |
| `elite_scrap_guard` | 1 | 第一层精英 / 当前临时 Boss。 | 当前只会重击；内容包要求防御、重击、低频封边。Dungeons 当前把它当一层 Boss，但内容包要求独立 `boss_gatekeeper_mk1`。 |
| `elite_mutant_amalgam` | 2 | 第二层塞包 / 诅咒精英 / 当前临时 Boss。 | 当前行为接近塞包精英，但缺蓄力意图和反制窗口。Dungeons 当前把它当二层 Boss，但内容包要求独立 `boss_spore_foundry`。掉落中含 `gear_chainsaw_sword`，该物品未被前三层内容包承接。 |

当前 Rewards 中已有对应 4 个奖励表：

| RewardID | 对应怪物 | 状态 | 问题 |
|---|---|---|---|
| `reward_monster_mob_scavenger_bug` | `mob_scavenger_bug` | 已有 | 可作为第一层普通怪奖励样例。 |
| `reward_monster_mob_acid_slime` | `mob_acid_slime` | 已有 | 需随 `mob_acid_slime` 的层级职责拆分一并调整。 |
| `reward_monster_elite_scrap_guard` | `elite_scrap_guard` | 已有 | 当前带 Boss 风格保底核心；若 `elite_scrap_guard` 回归精英，Boss 核心奖励应迁移给 `boss_gatekeeper_mk1`。 |
| `reward_monster_elite_mutant_amalgam` | `elite_mutant_amalgam` | 已有 | 若不再作为二层 Boss，应将 Boss 级成长奖励迁移给 `boss_spore_foundry`。 |

---

## 3. 正式字段缺口

`Monsters` README 当前偏程序行动结构说明。按 `03` 规则卡和 `15` 承接清单，正式配置还需要补以下字段组。

### 3.1 建议新增静态字段

| 字段组 | 建议字段 | 用途 | 优先级 |
|---|---|---|---|
| 身份 | `DisplayNameKey`、`MonsterRole`、`LoreTags`、`ContentPackRefs` | 标记怪物教学 / 检验职责、文案键、世界观标签和内容包归属。 | P0 |
| 层级 | `LayerRange`、`EncounterTags`、`DifficultyBudget`、`SpawnWeightTags` | 让怪物能被 Dungeons / MapProfile 按层级、路线和风险选用。 | P0 |
| 意图 | `IntentList`、`IntentSequence`、`Reliability`、`PowerPreview`、`TargetPreview`、`CounterHintKey` | 让玩家能提前看懂怪物行为。 | P0 |
| 行为 | `ActionPattern`、`PhaseRules`、`CooldownPolicy`、`FallbackAction` | 表达循环、蓄力、阶段变化和目标失效后的备用行为。 | P0 |
| 目标 | `TargetRule`、`ItemTargetFilter`、`GridTargetRule`、`RetargetPolicy` | 让攻击、对包干涉和腐蚀的目标选择可校验。 | P0 |
| 对包干涉 | `GridLockProfile`、`AddJunkProfile`、`MoveItemProfile`、`PollutionProfile`、`CorrodeItemProfile` | 与 `02` 和 `03` 规则卡对齐，不让 AI 私自定义裁决。 | P1 |
| 奖励 | `RewardID`、`DropThemeTags`、`BossGuaranteedRefs`、`MemoryRewardRefs` | 把怪物风险和掉落 / 房间记忆 / 成长材料连接起来。 | P1 |
| 表现 | `PortraitID`、`CombatVisualID`、`IntentIconSet`、`ActionVfxRef`、`HitFeedbackRef`、`DefeatFeedbackRef` | 让战斗 HUD、运行时美术验收和意图图标可承接。 | P1 |

### 3.2 明确运行时排除字段

以下字段不应写入怪物静态 JSON，只能存在于战斗实例或战斗日志：

| 字段 | 归属 |
|---|---|
| `CurrentHP`、`CurrentShield`、`CurrentIntent`、`SelectedActionID` | 战斗实例。 |
| `CooldownRemaining`、`UsesRemaining`、`ChargeStacks`、`PhaseIndex` | 战斗状态。 |
| `SummonedBy`、`StunTurns`、`TemporaryModifiers` | 临时效果。 |
| `DamageDealtLog`、`RewardRollResult`、`KilledByItemID` | 战斗结算日志。 |

---

## 4. 前三层内容缺口

### 4.1 第一层缺口

第一层内容包规划 5 个普通 / 精英怪 + 1 个 Boss，当前覆盖情况如下：

| 设计怪物 | 当前配置 | 状态 | 处理建议 |
|---|---|---|---|
| `mob_scavenger_bug` | 已有 | 基础可用 | 补 `MonsterRole`、低概率 `Defend`、意图预告字段和 Layer1 遭遇标签。 |
| `mob_rust_hound` | 缺失 | 未配置 | 新增 AP 节奏教学怪，行为为 `Attack` + `ChargeAttack`。 |
| `mob_acid_slime` | 已有但层级 / 职责不一致 | 需拆分或调整 | 建议保留第一层 `mob_acid_slime` 为轻度塞包教学；第二层另建 `mob_acid_slime_mature`。 |
| `mob_lost_miner_echo` | 缺失 | 未配置 | 新增低压 SAN 教学怪，连接 `trade_miner_lamp` 和探索事件。 |
| `elite_scrap_guard` | 已有 | 行为不足 | 补 `Defend`、`HeavyAttack`、低频 `GridLockEdge`，并从 Boss 位回归可绕精英。 |
| `boss_gatekeeper_mk1` | 缺失 | 未配置 | 新增第一层正式 Boss；迁移一阶机核保底奖励和 Boss / SafeZone 链路。 |

第一层最低完成标准：

1. 5 个普通 / 精英怪 + 1 个 Boss 都有对应 `MonsterID`。
2. 至少覆盖 4 类意图：`Attack`、`Defend`、`AddJunkSmall`、`SanPressureSmall`。
3. `elite_scrap_guard` 不再兼任 Boss；`boss_gatekeeper_mk1` 作为一层 Boss 独立配置。
4. `V-COMBAT-INTENT-01`、`V-L1-PACK-RUN-03`、`V-L1-PACK-FAIL-01` 有可配置输入。

### 4.2 第二层缺口

第二层内容包规划 4 个普通怪、2 个精英、1 个 Boss，当前只覆盖 2 个近似件：

| 设计怪物 | 当前配置 | 状态 | 处理建议 |
|---|---|---|---|
| `mob_acid_slime_mature` | 近似 `mob_acid_slime` | 不一致 | 新增或重命名成熟酸液软体，职责为稳定塞包 / 低级污染，不与第一层酸液软体混用。 |
| `mob_crystal_guard` | 缺失 | 未配置 | 新增防御与封边普通怪。 |
| `mob_soul_midge_swarm` | 缺失 | 未配置 | 新增低 HP 多体 SAN 压力怪。 |
| `mob_rust_cultivator` | 缺失 | 未配置 | 新增物品腐蚀预告怪。 |
| `elite_crystal_bulwark` | 缺失 | 未配置 | 新增封格精英。 |
| `elite_mutant_amalgam` | 已有 | 行为接近但不足 | 补 `Charge`、`AddJunkSmall`、`AttackHeavy` 序列和反制窗口；从 Boss 位回归精英。 |
| `boss_spore_foundry` | 缺失 | 未配置 | 新增第二层正式 Boss；迁移二阶成长材料保底奖励和 Boss / SafeZone 链路。 |

第二层最低完成标准：

1. 至少 4 个普通怪、2 个精英、1 个 Boss 有配置。
2. 至少覆盖 5 类机制：`AddJunkSmall`、`GridLockEdge`、`SanPressureSmall`、`CorrodeItemLow`、`Charge`。
3. `elite_mutant_amalgam` 不再兼任 Boss；`boss_spore_foundry` 作为二层 Boss 独立配置。
4. 至少一个怪物能被 `con_solvent_spray` 反制，形成“压力 -> 反制 -> 验收”闭环。

### 4.3 第三层缺口

第三层内容包规划 5 个普通怪、2 个精英、1 个 Boss，当前无正式配置覆盖：

| 类别 | 代表怪物 | 缺口 |
|---|---|---|
| 污染普通怪 | `mob_mycelium_crawler` | 缺物品实例污染教学怪。 |
| SAN / 标记怪 | `mob_nerve_midge_swarm` | 缺 SAN 中压和弱槽标记。 |
| 蓄力 / 封边怪 | `mob_spore_archer` | 缺远程蓄力和边缘封锁组合。 |
| 防御 / 修复怪 | `mob_shell_grafter` | 缺自修复 / 护卫型普通怪。 |
| 剧情压力怪 | `mob_echo_pilgrim` | 缺剧情 / SAN 压力入口。 |
| 污染精英 | `elite_spore_matriarch` | 缺召唤和中级污染精英。 |
| 构筑检查精英 | `elite_vein_knight` | 缺弱槽标记和穿刺检查。 |
| 路线侵蚀 Boss | `boss_mycelium_oracle` | 缺读取 `RouteCorrosion` 入场变量的 Boss。 |

第三层当前不建议一次性填满全部 JSON。建议先在 README / Validator 定义污染、弱槽标记、路线侵蚀入场变量和 Boss 阶段字段，再配置 2 个普通怪、1 个精英、1 个 Boss 样例。

---

## 5. 现有条目优先修正建议

| 优先级 | MonsterID | 问题 | 建议 |
|---|---|---|---|
| P0 | `elite_scrap_guard` | 当前在 Dungeons 中承担一层 Boss，但内容包定位是可绕精英。 | 新增 `boss_gatekeeper_mk1`，把一阶机核保底和 Boss 节点迁移给 Boss；`elite_scrap_guard` 补防御 / 封边精英行为。 |
| P0 | `elite_mutant_amalgam` | 当前在 Dungeons 中承担二层 Boss，但内容包定位是塞包精英。 | 新增 `boss_spore_foundry`，把二层 Boss 节点迁移给 Boss；`elite_mutant_amalgam` 补蓄力 / 塞包 / 重击序列。 |
| P0 | `mob_acid_slime` | Layer=2，但第一层内容包也需要酸液软体；当前行为不是轻度塞包。 | 拆成 `mob_acid_slime`（Layer1 轻塞包）和 `mob_acid_slime_mature`（Layer2 污染 / 塞包）。 |
| P0 | `mob_scavenger_bug` | 只有攻击，缺低概率防御和意图字段。 | 补 `Defend` 或正式意图字段，作为 `V-COMBAT-INTENT-01` 基础样例。 |
| P1 | `elite_mutant_amalgam` 掉落 | 掉落 `gear_chainsaw_sword` 未被前三层内容包承接。 | 将 `gear_chainsaw_sword` 标为后续批次，或明确放入第二层 / 第三层内容包。 |
| P1 | 所有怪物 | 仍保留 `LootPool` fallback。 | 迁移期可保留，但正式新增怪物必须只走 `RewardID` / Rewards。 |

---

## 6. Validator 建议

### 6.1 立即加入 P0 的检查

| 检查项 | 规则 |
|---|---|
| `MonsterID` 与文件名一致 | 避免内容包、Dungeons 和 Rewards 引用错位。 |
| `RewardID` 必须存在 | 怪物引用的奖励表必须能在 `Rewards` 找到。 |
| `PortraitID` / `CombatVisualID` 非空 | 正式怪物必须有头像和战斗实体引用，至少输出 warning。 |
| `AI.Actions` 非空 | 每只怪物至少有一个合法行动。 |
| Action 字段合法 | `ActionID` 唯一；`ActionType`、`Target`、`Condition` 在合法枚举中；`Weight > 0` 的行动才参与选择。 |
| 基础参数存在 | `DamageTarget` 必须有 `Damage`；`AddCursedItem` 必须有 `ItemID`；`ReduceWeaponDamage` 必须有 `Multiplier`。 |
| Boss 独立性 warning | Dungeons 的 `BossNode` 若引用 `elite_*`，输出 warning，除非配置明确标记 `CanActAsBoss=true`。 |

### 6.2 进入正式配置后加入的检查

| 检查项 | 规则 |
|---|---|
| 内容包覆盖 | `19/20/25` 中进入当前批次的怪物 / Boss 必须存在对应 `MonsterID`。 |
| 意图可读 | 正式怪物每个高权重行动必须能映射到 `IntentType`、预告数值和 UI 图标。 |
| 机制覆盖 | 第一层至少覆盖攻击 / 防御 / 轻塞包 / SAN 小压；第二层至少覆盖塞包 / 封格 / 腐蚀 / 蓄力；第三层至少覆盖污染 / 标记 / 路线侵蚀 Boss 变量。 |
| 层级合法 | `LayerRange` 或 `Layer` 必须和内容包、Dungeons 引用一致。 |
| 目标合法 | 对包干涉行动必须声明目标规则和失败 fallback，不允许静默覆盖已有物品。 |
| 奖励风险匹配 | 精英 / Boss 必须有高于普通怪的奖励承接，且 Boss 核心材料不可只挂在精英奖励里。 |
| 旧掉落收口 | 新正式怪物不允许只配置 `LootPool`；`LootPool` 只作为迁移期 fallback。 |

---

## 7. 下一步配置任务

### T1：同步 Monsters README 字段

目标：让配置说明能承接正式怪物和意图规则。

范围：

* 补身份 / 层级字段：`MonsterRole`、`LayerRange`、`EncounterTags`、`DifficultyBudget`。
* 补意图字段：`IntentType`、`PowerPreview`、`TargetPreview`、`Reliability`、`CounterHintKey`。
* 补行为结构：`IntentSequence`、`PhaseRules`、`FallbackAction`。
* 补对包干涉配置：`GridLockProfile`、`AddJunkProfile`、`PollutionProfile`、`CorrodeItemProfile`。
* 明确运行时字段不得写入静态配置。

完成证据：`配置表(JSON)/Monsters/README.md` 更新，并能映射 `15` 的 Monsters 承接字段。

### T2：拆分 Boss 与精英职责

目标：让一二层 Boss 不再由精英临时兼任。

优先处理：

1. 新增 `boss_gatekeeper_mk1`。
2. 新增 `boss_spore_foundry`。
3. 将 `elite_scrap_guard` 回归一层可绕精英。
4. 将 `elite_mutant_amalgam` 回归二层塞包精英。
5. 对应更新 Dungeons BossNode 和 Rewards。

完成证据：`layer_1` / `layer_2` 的 BossNode 指向正式 Boss，Boss 奖励和 SafeZone 链路可验收。

### T3：补第一层怪物教学链

目标：让第一层能完整教学战斗意图。

优先新增 / 修正：

1. `mob_scavenger_bug`
2. `mob_rust_hound`
3. `mob_acid_slime`
4. `mob_lost_miner_echo`
5. `elite_scrap_guard`

完成证据：第一层 5 怪 + 1 Boss 可配置，并覆盖攻击、防御、轻塞包、低压 SAN。

### T4：补第二层背包压力链

目标：让第二层怪物压力不是纯数值升级。

优先新增 / 修正：

1. `mob_acid_slime_mature`
2. `mob_crystal_guard`
3. `mob_rust_cultivator`
4. `elite_crystal_bulwark`
5. `elite_mutant_amalgam`
6. `boss_spore_foundry`

完成证据：`V-L2-PACK-RUN-01`、`V-L2-PACK-ELITE-01`、`V-L2-PACK-BOSS-01` 有怪物配置输入。

### T5：先定义第三层机制字段，再填样例

目标：避免第三层怪物先堆 JSON、后补规则。

优先定义：

* `PolluteItemLow / Mid`
* `MarkWeakSlot`
* `RouteCorrosion`
* `SummonSpore`
* `PierceAttack`
* Boss `PhaseRules`

完成证据：第三层至少 2 个普通怪、1 个精英、1 个 Boss 样例可配置，并能支持 `V-L3-PACK-ROUTE-01` 和 `V-L3-PACK-BOSS-01`。

---

## 8. 对 26 的回写判断

本审计完成后，`26` 的 W1 进度应更新为：

| 工作包 | 旧状态 | 新状态 |
|---|---|---|
| `Items` 配置承接审计 | 进行中：首轮审计已完成 | 进行中：等待 README 字段同步和配置落地。 |
| `Monsters` 配置承接审计 | 未开始 | 进行中：字段缺口、内容缺口、现有条目差异、Boss / 精英职责错位和 Validator 建议已完成首轮审计。 |
| `Dungeons` / `Rewards` 配置承接审计 | 未开始 | 未开始。 |

下一份审计建议推进 `Dungeons`，因为 Monsters 审计已经发现 BossNode、层级引用、MapProfile 和 Rewards 之间存在直接承接关系。
