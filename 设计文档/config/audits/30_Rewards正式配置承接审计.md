---
id: design_30_rewards_formal_config_audit
title: Rewards正式配置承接审计
type: audit
role: 策划
domain: formal_config_authoring
status: active
source_of_truth: true
related:
  - 设计文档/README.md
  - 设计文档/config/26_正式配置设计与填充推进计划.md
  - 设计文档/delivery/15_P0主干配置表现验收承接清单.md
  - 设计文档/delivery/17_P2长期循环叙事承接清单.md
  - 设计文档/content_packs/19_第一层正式核心内容包.md
  - 设计文档/content_packs/20_第二层背包压力内容包.md
  - 设计文档/content_packs/25_第三层路线侵蚀内容包.md
  - 设计文档/config/designs/31_第一层正式配置落地设计.md
  - 设计文档/config/designs/32_第二层正式配置落地设计.md
  - 设计文档/config/designs/33_第三层正式配置落地设计.md
  - 设计文档/config/tasks/34_前三层正式配置实现任务拆分.md
  - 配置表(JSON)/Rewards/README.md
  - 配置表(JSON)/Items/README.md
  - 配置表(JSON)/Monsters/README.md
  - 配置表(JSON)/Dungeons/README.md
  - agent_status/design.md
last_verified: 2026-05-25
update_rule: 调整 Rewards 正式配置字段、前三层奖励内容缺口、订单 / 成长 / 节点奖励引用或 Validator 建议时同步本文件。
---

# Rewards正式配置承接审计

> **定位：** 本文是 `26_正式配置设计与填充推进计划.md` 中 W1「配置承接审计」的第四份落地产物。它只审计 `Rewards` 配置域：当前奖励 JSON、配置 README、P0 / P2 承接清单、前三层内容包以及 `Items` / `Monsters` / `Dungeons` 的引用关系是否已经对齐。它不直接改配置值，也不替代 `配置表(JSON)/Rewards/README.md`。

---

## 1. 审计结论

截至 2026-05-25，`配置表(JSON)/Rewards` 已具备基础奖励表结构：`RewardID`、`Tags`、`Guaranteed`、`WeightedPools`、`Item`、`Money`、`RewardRef`、`Nothing`。当前已有 8 个奖励表，能支撑一二层部分怪物、宝箱和事件节点奖励。

当前结论：

| 项 | 判断 | 说明 |
|---|---|---|
| 字段结构 | 部分满足 | 已能表达保底和权重池，但缺正式版所需的来源职责、奖励稀有度预算、层级预算、成长 / 订单 / 声望 / 解锁奖励字段和验收标签。 |
| 首批内容量 | 不足 | 当前 8 个奖励表只覆盖 4 个怪物、2 个宝箱、2 个事件；缺第三层、正式 Boss、精英拆分、订单奖励、成长资源、剧情 / 房间记忆触发奖励。 |
| 调用关系 | 部分满足 | Monsters 已通过 `RewardID` 引用奖励表；Dungeons 一二层已有 Treasure / Event 节点奖励引用，但当前节点池与内容包仍未完全对齐。 |
| Boss 奖励 | 不满足 | 当前 Boss 级奖励挂在 `elite_scrap_guard` 和 `elite_mutant_amalgam` 上；`28/29` 已指出二者应回归精英，正式 Boss 应拆成独立奖励表。 |
| 经济 / 订单承接 | 不足 | 当前奖励主要是物品和少量金币，缺订单奖励结构、声望 / 信任 / 图纸 / 服务解锁、黑市背叛收益和账单可复盘来源。 |
| Validator 支撑 | 不足 | 当前可校验 ItemID / RewardRef 基础引用，但尚不能校验内容包覆盖、Boss 保底、层级收益预算、Money 上限、RewardRef 循环、订单奖励字段和节点奖励引用闭环。 |

因此，`Rewards` 当前状态应标记为：

```text
规则可开发：已完成
配置可承接：进行中
正式配置填充：未完成
```

---

## 2. 当前配置事实

当前 `配置表(JSON)/Rewards` 共 8 个奖励表：

| RewardID | 类型判断 | 当前内容 | 当前问题 |
|---|---|---|---|
| `reward_monster_mob_scavenger_bug` | 一层普通怪 | 齿轮废料、修复包、锈蚀短匕、拼木盾权重掉落。 | 能支撑基础拾取，但缺小金币或经济来源标签；没有与第一层完整普通怪池拆分。 |
| `reward_monster_mob_acid_slime` | 二层普通怪 / 酸液样例 | 锈红线圈、毒雾滤芯、镇静剂。 | `28` 已指出 `mob_acid_slime` 层级职责需拆分；奖励也应随一层轻塞包 / 二层成熟酸液职责拆分。 |
| `reward_monster_elite_scrap_guard` | 一层精英 / 临时 Boss | 保底 `mat_core_tier1`，权重给折刃战术刀、锈红线圈、镇静剂。 | 当前带 Boss 保底核心；若 `elite_scrap_guard` 回归精英，`mat_core_tier1` 应迁移到 `boss_gatekeeper_mk1` 奖励表。 |
| `reward_monster_elite_mutant_amalgam` | 二层精英 / 临时 Boss | 保底 `mat_core_tier2`，权重给链锯剑、充能手铳、裂铁胸甲、毒雾滤芯。 | 当前带 Boss 保底核心；`gear_chainsaw_sword` 未被前三层内容包承接；二层正式 Boss 应为 `boss_spore_foundry`。 |
| `reward_node_treasure_layer1` | 一层宝箱 | 保底齿轮废料，权重给修复包、锈红线圈或空。 | 缺 `trade_miner_lamp`、`gear_tactical_blade` 等内容包宝箱目标；奖励职责偏保守。 |
| `reward_node_treasure_layer2` | 二层宝箱 | 保底毒雾滤芯，权重给一阶机核、锈红线圈或空。 | 缺第二层宝箱目标：链钩短枪、稳定安瓿、活孢子笼、晶化鳞片等。 |
| `reward_node_event_layer1` | 一层事件 | 保底 12 金币。 | 事件奖励只有金币，无法承接矿工提灯、裂纹圣牌、房间记忆触发等内容包职责。 |
| `reward_node_event_layer2` | 二层事件 | 保底 18 金币。 | 事件奖励只有金币，无法承接封存遗物匣、情报、订单线索、路线信息等内容包职责。 |

现有奖励表更像“可运行样例”，不是正式收益结构。它能验证 `RewardSystem` 基础解析，但不能支撑前三层正式体验。

---

## 3. 正式字段缺口

`Rewards` README 当前字段可以表达基础随机掉落，但正式配置还需要补以下字段组。

### 3.1 建议新增静态字段

| 字段组 | 建议字段 | 用途 | 优先级 |
|---|---|---|---|
| 身份 | `RewardRole`、`SourceType`、`LayerRange`、`ContentPackRefs` | 说明奖励服务普通怪、精英、Boss、宝箱、事件、订单还是剧情。 | P0 |
| 收益预算 | `RewardTier`、`ExpectedValueBand`、`RiskBudgetTag`、`GuaranteedValueBand` | 让奖励与路线风险、怪物难度和层级进度匹配。 | P0 |
| 成长回流 | `GrowthHooks`、`CraftingRefs`、`UnlockRefs`、`MemoryRefs` | 表达奖励进入义体、底盘、房间记忆或剧情的后续去向。 | P1 |
| 订单 / 声望 | `FactionRewardRefs`、`ReputationDelta`、`TrustDelta`、`OrderProgressRefs` | 支撑 `17` 和 `24` 的订单奖励、黑市背叛和势力声望。 | P1 |
| 验收 | `ValidationTags`、`SeedSampleRefs`、`AuditNotes` | 让 Validator 和固定 seed 摘要能证明奖励覆盖。 | P0 |

### 3.2 RewardEntry 类型缺口

| 类型 | 当前状态 | 正式建议 |
|---|---|---|
| `Item` | 已支持。 | 增加 `ItemTags` 或 `ItemPoolRef` 之前，先保持显式 `ItemID`，确保前三层内容可审计。 |
| `Money` | 已支持。 | 增加层级上限和经济来源标签，避免金币奖励破坏月租 / 账单压力。 |
| `RewardRef` | 已支持。 | 必须做循环引用检测、最大展开深度和重复保底合并规则。 |
| `Nothing` | 已支持。 | 只能出现在权重池，不能作为 Boss 或关键订单的唯一结果。 |
| `Reputation` | 缺失。 | P4 订单 / 势力上线前需要新增或通过订单系统外层字段表达。 |
| `Unlock` | 缺失。 | 图纸、商店、服务、剧情旗标不应伪装成普通物品。 |
| `Memory` | 缺失。 | Boss 胜利、首次战败、关键战利品展示需要能触发 RoomMemory 或 ScenarioEvent。 |

---

## 4. 前三层奖励内容缺口

### 4.1 第一层

| 奖励来源 | 内容包要求 | 当前状态 | 缺口 |
|---|---|---|---|
| 普通怪 | 齿轮废料、小金币、基础消耗品。 | `mob_scavenger_bug` 已有基础物品。 | 缺小金币来源或经济标签；其他普通怪奖励未配置。 |
| 精英 | 折刃战术刀、裂纹圣牌。 | `elite_scrap_guard` 有折刃战术刀，但没有裂纹圣牌。 | 需新增 `trade_cracked_relic` 后接入精英奖励。 |
| Boss | 必掉一阶机核，概率裂铁胸甲或裂纹圣牌。 | 当前保底挂在 `elite_scrap_guard`。 | 需新建 `reward_boss_gatekeeper_mk1`，并从精英奖励中拆出 Boss 保底。 |
| 宝箱 | 修复包、锈红线圈、矿工提灯、低风险贸易品。 | `reward_node_treasure_layer1` 有修复包和线圈。 | 缺矿工提灯、低风险高价物、路线主题差异。 |
| 事件 | 金币、矿工提灯、裂纹圣牌、风险选项。 | `reward_node_event_layer1` 只有 12 金币。 | 需拆事件奖励表，不能用单一金币替代事件内容。 |

### 4.2 第二层

| 奖励来源 | 内容包要求 | 当前状态 | 缺口 |
|---|---|---|---|
| 普通怪 | 腐蚀材料、晶化鳞片、活体孢子样本、反制消耗品。 | `mob_acid_slime` 有毒雾滤芯和镇静剂。 | 缺成熟酸液 / 结晶路线的专用奖励表。 |
| 精英 | 链钩短枪、腐蚀壁盾、封存遗物匣、二阶材料。 | `elite_mutant_amalgam` 有高阶装备样例。 | 缺第二层两个精英的独立奖励；链锯剑不属于当前内容包。 |
| Boss | 二阶机核碎片、稀有材料。 | 当前保底 `mat_core_tier2` 挂在 `elite_mutant_amalgam`。 | 需新建 `reward_boss_spore_foundry`，并对齐 `mat_core_tier2_fragment` 命名。 |
| 宝箱 | 稳定安瓿、链钩短枪、材料、订单目标线索。 | `reward_node_treasure_layer2` 有毒雾滤芯、一阶机核、线圈。 | 当前更像一层延伸；缺二层正式奖励职责。 |
| 事件 / 订单 | 封存遗物匣、订单线索、黑市诱惑、路线信息。 | `reward_node_event_layer2` 只有 18 金币。 | 需拆事件奖励与订单奖励，补声望 / 信任 / 图纸字段。 |

### 4.3 第三层

| 奖励来源 | 内容包要求 | 当前状态 | 缺口 |
|---|---|---|---|
| 普通怪 / 精英 | 孢晶琥珀、活性菌丝、污染材料、低唱化石。 | 无第三层奖励表。 | 需新增第三层普通怪、精英和污染路线奖励池。 |
| Boss | 必掉三阶机核种，概率低唱化石或污染义体材料。 | 无第三层 Boss 奖励表。 | 需新增 `reward_boss_mycelium_oracle`。 |
| 宝箱 / 事件 | 争夺孢核、黑市线索、路线侵蚀事件奖励。 | 无第三层节点奖励表。 | 需新增 Treasure / Event / Hazard 奖励池。 |
| 订单 | 工坊、法师塔、炼金公会、黑市争夺同一核心样本。 | 无订单奖励配置。 | 需支持金币、声望、信任、图纸、服务解锁和背叛奖励。 |

---

## 5. 与其他配置域的关系

| 配置域 | 当前关系 | 正式要求 |
|---|---|---|
| `Items` | Rewards 直接引用 `ItemID`。 | 所有 `ItemID` 必须存在；关键奖励应覆盖内容包要求的来源和去向。 |
| `Monsters` | Monsters 通过 `RewardID` 调用奖励表。 | 普通怪、精英、Boss 需要拆分奖励职责；Boss 保底不应挂在精英上。 |
| `Dungeons` | Treasure / Event 节点可配置 `RewardID`。 | 节点奖励需与 MapProfile、路线主题、风险等级和内容包必出内容对齐。 |
| `Orders` | 目前只有承接文档，没有配置域落地。 | 订单奖励必须能表达金币、声望、信任、图纸、解锁和黑市背叛。 |
| 经济 | Money 可作为奖励条目。 | 金币奖励必须有来源标签和层级预算，避免破坏出售 / 月租压力。 |
| 房间记忆 / 剧情 | 当前 RewardEntry 不支持。 | Boss 胜利、关键战利品和事件奖励需要能触发 RoomMemory 或 ScenarioEvent。 |

---

## 6. Validator 建议

### 6.1 P0 必须校验

1. `RewardID` 全局唯一，文件名建议与 `RewardID` 一致。
2. 所有 `Type=Item` 的 `ItemID` 存在于 `Items`。
3. 所有 `Type=RewardRef` 的 `RewardID` 存在且无循环引用。
4. 所有 `Weight` 必须大于 0；`Weight <= 0` 输出 warning 或 error。
5. `Guaranteed` 与 `WeightedPools` 至少有一项非空；除非明确标记为空奖励。
6. `Nothing` 不允许出现在 `Guaranteed`。
7. Monsters / Dungeons 引用的 `RewardID` 必须存在。
8. Boss 奖励表必须有保底成长钩子，且不能只给金币或 `Nothing`。

### 6.2 P1 建议校验

1. 每层至少有普通怪、精英、Boss、宝箱、事件五类奖励来源。
2. 第一至第三层 Boss 奖励分别覆盖 `mat_core_tier1`、二阶成长材料、三阶成长材料。
3. 节点奖励的 `Tags` 与调用节点类型一致，例如 `Treasure` 节点不引用纯 `Monster` 奖励。
4. 关键内容包物品必须至少有一个来源奖励表。
5. Money 奖励按层级预算检查上下限。
6. 订单奖励必须能被 Validator 找到，并能解释金币、声望、信任或解锁来源。

### 6.3 验收样例

| 验收 ID | 输入 | 期望 |
|---|---|---|
| `V-REWARD-REF-01` | 遍历所有 Monsters / Dungeons 的 `RewardID`。 | 全部存在，且能解析到至少一个有效奖励结果。 |
| `V-REWARD-BOSS-01` | 击败第一层正式 Boss。 | 保底获得一阶成长材料，额外奖励与背包空间形成取舍。 |
| `V-REWARD-NODE-01` | 固定 seed 生成 Treasure / Event 节点。 | 节点奖励能解析，且标签与节点类型匹配。 |
| `V-REWARD-ORDER-01` | 完成第二层或第三层订单。 | 发放金币 / 声望 / 信任 / 图纸或解锁，日志可复盘来源。 |
| `V-REWARD-COVERAGE-01` | 检查前三层内容包关键物品。 | 每个关键物品至少存在一个奖励、商店、订单或事件来源。 |

---

## 7. 下一步配置优先级

### P0：先保证引用闭环

1. 把 `Rewards` README 从 MVP 示例升级为正式字段说明。
2. 将 `reward_monster_elite_scrap_guard` 从 Boss 样例改回精英奖励口径。
3. 将 `reward_monster_elite_mutant_amalgam` 从 Boss 样例改回精英奖励口径。
4. 新增正式 Boss 奖励表：`reward_boss_gatekeeper_mk1`、`reward_boss_spore_foundry`、`reward_boss_mycelium_oracle`。
5. 补第三层基础奖励表：普通怪、精英、宝箱、事件。

### P1：再补内容包覆盖

1. 按 `19/20/25` 给关键物品补来源奖励。
2. 给宝箱 / 事件奖励拆出路线主题差异，不再只用单一金币。
3. 为订单奖励预留金币、声望、信任、图纸和解锁字段。
4. 为 Boss 胜利和关键战利品预留 RoomMemory / ScenarioEvent 触发口径。

### P2：最后接经济和长期循环

1. 将 Money 奖励接入经济账单来源说明。
2. 将订单奖励接入 `17` / `24` 的势力声望和黑市背叛结构。
3. 将奖励覆盖结果纳入固定 seed 验收报告。

---

## 8. 完成判定

`Rewards` 达到正式配置承接标准，需要满足：

1. 所有 Monsters / Dungeons / Orders 引用的 `RewardID` 都存在且可解析。
2. 前三层普通怪、精英、Boss、宝箱、事件、订单都有独立奖励职责。
3. Boss 保底成长材料从精英奖励中拆出，并落到正式 Boss 奖励表。
4. 关键内容包物品至少有一个明确来源。
5. Money、订单、声望、信任、图纸、解锁和房间记忆奖励有字段口径。
6. Validator 能检查引用、循环、权重、层级覆盖、Boss 保底和固定 seed 奖励摘要。

当前审计只完成“缺口识别”。在 README、JSON、Validator 和验收样例落地前，不应把 `Rewards` 标记为配置完成。
