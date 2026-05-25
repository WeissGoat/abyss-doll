---
id: config_rewards_readme
title: 奖励表配置字段说明 (Rewards Config)
type: config
role: 策划
domain: config_rewards
status: active
source_of_truth: true
related:
  - 开发文档/03_深渊与战斗循环(DungeonCombat).md
  - 开发文档/10_奖励与掉落系统(RewardSystem).md
  - 数值模型设计/03_深渊产出与掉落期望.md
  - 配置表(JSON)/Chassis/README.md
  - 配置表(JSON)/README.md
  - 配置表(JSON)/Dungeons/README.md
  - 配置表(JSON)/Orders/README.md
  - 配置表(JSON)/Prosthetics/README.md
  - 设计文档/GDD/GDD_02_深渊地图遍历与搜打撤抉择.md
  - 设计文档/GDD/GDD_10_势力声望与订单系统.md
  - 设计文档/rules/07_势力声望与订单规则卡.md
  - 设计文档/GDD/GDD_04_小镇循环与经济物价波浪模型.md
  - 设计文档/config/audits/28_Monsters正式配置承接审计.md
  - 设计文档/config/audits/29_Dungeons正式配置承接审计.md
  - 设计文档/config/audits/30_Rewards正式配置承接审计.md
  - 设计文档/config/designs/31_第一层正式配置落地设计.md
  - 设计文档/config/designs/32_第二层正式配置落地设计.md
  - 设计文档/config/designs/33_第三层正式配置落地设计.md
  - 设计文档/config/designs/49_经济压力势力正式配置落地设计.md
  - 设计文档/config/designs/50_经济压力订单正式配置落地设计.md
  - 设计文档/config/tasks/52_经济压力正式配置实现任务拆分.md
last_verified: 2026-05-25
update_rule: 修改配置字段、数据源规则或表间引用时同步本文件。
---

# 奖励表配置字段说明 (Rewards Config)

> 本目录用于配置所有掉落、宝箱、事件、任务、节点结算等奖励。调用方只配置 `RewardID`，具体保底、权重、空掉落和奖励组合都由奖励表负责。

正式配置字段、前三层奖励内容缺口、Boss 保底错位、订单 / 成长 / 节点奖励引用和 Validator 建议见 `设计文档/config/audits/30_Rewards正式配置承接审计.md`。该审计是配置改造依据，不代表本文字段或奖励 JSON 已经全部落地。

## 1. 基本约定

*   一个奖励表一个 JSON 文件。
*   文件名建议与 `RewardID` 一致。
*   怪物、节点、事件等来源不再直接配置掉落池，而是引用 `RewardID`。
*   怪物旧字段 `LootPool` 只允许作为迁移期 fallback；正式内容必须走奖励表。
*   奖励表只定义“可获得什么”和“为什么从这里获得”；物品实例归属、背包拾取、撤离 / 战败流向仍由对应系统裁决。
*   Boss、订单、剧情和房间记忆奖励必须能被 Validator 追踪来源，不允许只在文本描述中发放。

## 2. RewardConfig 字段

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `RewardID` | string | 奖励表全局唯一 ID | 如 `reward_monster_elite_scrap_guard` |
| `Name` | string | 策划可读名称 | 仅用于日志、编辑器和排查 |
| `Tags` | array | 奖励表标签 | 如 `Monster`、`Boss`、`Layer1` |
| `RewardRole` | enum | 奖励职责 | `MobDrop`、`EliteDrop`、`BossDrop`、`TreasureNode`、`EventNode`、`OrderReward`、`ScenarioReward`、`RoomMemoryReward` |
| `SourceType` | enum | 调用来源类型 | `Monster`、`DungeonNode`、`Order`、`ScenarioEvent`、`RoomMemory`、`Shop`、`SystemGrant` |
| `LayerRange` | object | 推荐层级范围 | `{ "Min": 1, "Max": 3 }`；跨层奖励必须说明原因 |
| `ContentPackRefs` | array | 内容包来源 | 如 `design_pack_layer1_formal_core` |
| `RewardTier` | enum | 收益等级 | `Minor`、`Standard`、`Elite`、`Boss`、`Order`、`Story` |
| `ExpectedValueBand` | object | 期望价值区间 | 用于经济预算和掉落强度检查 |
| `GuaranteedValueBand` | object | 保底价值区间 | Boss / 订单奖励必须配置或说明豁免 |
| `RiskBudgetTag` | string | 风险收益标签 | 如 `LowRisk`、`RiskReward`、`Attrition`、`OrderConflict` |
| `Guaranteed` | array | 保底奖励列表 | 必定生成 |
| `WeightedPools` | array | 权重奖励池列表 | 每个池独立掷骰 |
| `GrowthHooks` | array | 成长回流说明 | 可引用底盘、义体、维护、制造或下潜许可方向 |
| `CraftingRefs` | array | 制造 / 材料引用 | 可选，用于说明奖励进入哪些配方或材料缺口 |
| `UnlockRefs` | array | 解锁引用 | 可选，用于图纸、商店、服务、剧情旗标或系统开关 |
| `MemoryRefs` | array | 房间记忆 / 剧情引用 | 可选，用于 Boss 胜利、首次战败、关键战利品展示等 |
| `FactionRewardRefs` | array | 势力 / 订单奖励引用 | 可选，用于声望、信任、黑市背叛和势力解锁 |
| `ValidationTags` | array | Validator 标签 | 如 `P0Reward`、`Layer1BossGuaranteed`、`OrderReward` |
| `SeedSampleRefs` | array | 固定 seed 验收引用 | 可选，用于自动化报告定位 |
| `AuditNotes` | string | 审计备注 | 仅策划 / Validator 排查使用，不作为玩家文本 |

## 3. RewardEntry 字段

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `Type` | enum | 奖励条目类型 | 当前已支持 `Item`、`Money`、`RewardRef`、`Nothing`；正式扩展预留 `Reputation`、`Trust`、`Unlock`、`Memory`、`ScenarioFlag` |
| `ItemID` | string | 物品 ID | `Type=Item` 时填写，指向 `/Items` |
| `Money` | int | 金币数量 | `Type=Money` 时填写 |
| `RewardID` | string | 子奖励表 ID | `Type=RewardRef` 时填写 |
| `FactionID` | string | 势力 ID | `Type=Reputation` 或 `Type=Trust` 时填写；正式订单接入前可由订单系统外层字段表达 |
| `ReputationDelta` | int | 声望变化 | 正值奖励，负值惩罚 |
| `TrustDelta` | int | 黑市 / 特定势力信任变化 | 用于背叛或隐秘渠道 |
| `UnlockID` | string | 解锁 ID | `Type=Unlock` 时填写，指向图纸、商店、服务或剧情旗标 |
| `MemoryID` | string | 房间记忆 ID | `Type=Memory` 时填写，指向 RoomMemory / Mementos |
| `ScenarioFlagID` | string | 剧情旗标 ID | `Type=ScenarioFlag` 时填写 |
| `Weight` | int | 权重 | 仅权重池条目需要 |
| `Count` | int | 固定数量 | 默认 1 |
| `MinCount` | int | 随机数量下限 | 可选 |
| `MaxCount` | int | 随机数量上限 | 可选 |
| `Condition` | string | 条件表达式 | 可选，后续接条件系统 |
| `SourceNote` | string | 来源备注 | 可选，用于账单、日志和审计摘要 |

## 4. WeightedPool 字段

| 字段名 | 数据类型 | 注释说明 |
| :--- | :--- | :--- |
| `PoolID` | string | 池子 ID，用于日志定位 |
| `RollCount` | int | 掷骰次数，默认 1 |
| `AllowDuplicate` | bool | 多次掷骰时是否允许重复抽到同一条 |
| `Entries` | array | 权重条目列表 |

## 5. 正式奖励职责

| RewardRole | 使用场景 | 必须满足 |
|---|---|---|
| `MobDrop` | 普通怪掉落 | 服务教学、材料、低价经济或反制消耗品；不能承载 Boss 保底成长钩子。 |
| `EliteDrop` | 精英怪掉落 | 明显高于普通怪，能解释路线风险；至少一项奖励带来背包、经济或构筑取舍。 |
| `BossDrop` | Boss 掉落 | 必须有保底成长钩子，且 Boss 后 SafeZone 撤离时能带回。 |
| `TreasureNode` | 宝箱节点 | 服务路线收益差异；可包含空结果，但不能让高风险路线收益长期低于安全路线。 |
| `EventNode` | 事件节点 | 允许金币、物品、路线信息、风险物或剧情触发；不能长期只给固定金币。 |
| `OrderReward` | 订单奖励 | 必须能表达金币、声望、信任、图纸、服务解锁或黑市背叛收益。 |
| `ScenarioReward` | 剧情奖励 | 必须支持旗标、解锁或系统指令，不应伪装成普通物品。 |
| `RoomMemoryReward` | 房间记忆奖励 | 用于 Boss 胜利、首次战败、关键战利品展示等长期反馈。 |

## 6. 正式示例

### 6.1 普通怪：拾荒虫

```json
{
  "RewardID": "reward_monster_mob_scavenger_bug",
  "Name": "拾荒虫奖励",
  "Tags": ["Monster", "Layer1"],
  "RewardRole": "MobDrop",
  "SourceType": "Monster",
  "LayerRange": { "Min": 1, "Max": 1 },
  "ContentPackRefs": ["design_pack_layer1_formal_core"],
  "RewardTier": "Minor",
  "ExpectedValueBand": { "Min": 80, "Max": 180 },
  "RiskBudgetTag": "LowRisk",
  "Guaranteed": [],
  "WeightedPools": [
    {
      "PoolID": "main",
      "RollCount": 1,
      "AllowDuplicate": true,
      "Entries": [
        { "Type": "Item", "ItemID": "loot_gear_scrap", "Weight": 55, "Count": 1 },
        { "Type": "Item", "ItemID": "con_repair_kit", "Weight": 20, "Count": 1 },
        { "Type": "Item", "ItemID": "gear_rusty_dagger", "Weight": 15, "Count": 1 },
        { "Type": "Item", "ItemID": "gear_wooden_shield", "Weight": 10, "Count": 1 }
      ]
    }
  ]
}
```

### 6.2 Boss：一层守门机核

```json
{
  "RewardID": "reward_boss_gatekeeper_mk1",
  "Name": "一层守门机核奖励",
  "Tags": ["Monster", "Boss", "Layer1"],
  "RewardRole": "BossDrop",
  "SourceType": "Monster",
  "LayerRange": { "Min": 1, "Max": 1 },
  "ContentPackRefs": ["design_pack_layer1_formal_core"],
  "RewardTier": "Boss",
  "ExpectedValueBand": { "Min": 650, "Max": 1100 },
  "GuaranteedValueBand": { "Min": 300, "Max": 500 },
  "RiskBudgetTag": "BossGate",
  "GrowthHooks": ["ChassisTier1", "ProstheticTier1"],
  "MemoryRefs": ["room_memory_first_gatekeeper_core"],
  "ValidationTags": ["Layer1BossGuaranteed"],
  "Guaranteed": [
    { "Type": "Item", "ItemID": "mat_core_tier1", "Count": 1 }
  ],
  "WeightedPools": [
    {
      "PoolID": "bonus",
      "RollCount": 1,
      "AllowDuplicate": false,
      "Entries": [
        { "Type": "Item", "ItemID": "gear_tactical_blade", "Weight": 35, "Count": 1 },
        { "Type": "Item", "ItemID": "loot_rusty_coil", "Weight": 80, "Count": 1 },
        { "Type": "Item", "ItemID": "con_cheap_sedative", "Weight": 20, "Count": 1 }
      ]
    }
  ]
}
```

> 当前 JSON 中 `reward_monster_elite_scrap_guard` 仍是历史验证样例；正式配置应将 Boss 保底迁移到 `reward_boss_gatekeeper_mk1`，让 `elite_scrap_guard` 回归精英奖励。

### 6.3 订单奖励

```json
{
  "RewardID": "reward_order_workshop_spore_core_frame",
  "Name": "机械工坊孢核框架订单奖励",
  "Tags": ["Order", "Layer3", "Workshop"],
  "RewardRole": "OrderReward",
  "SourceType": "Order",
  "LayerRange": { "Min": 3, "Max": 3 },
  "ContentPackRefs": ["design_pack_layer3_route_corrosion"],
  "RewardTier": "Order",
  "RiskBudgetTag": "OrderConflict",
  "FactionRewardRefs": ["faction_workshop"],
  "Guaranteed": [
    { "Type": "Money", "Money": 280, "SourceNote": "Workshop order payout" },
    { "Type": "Reputation", "FactionID": "faction_workshop", "ReputationDelta": 25 },
    { "Type": "Unlock", "UnlockID": "blueprint_chassis_vein_frame" }
  ],
  "WeightedPools": []
}
```

## 7. 引用方式

怪物：

```json
{
  "MonsterID": "elite_scrap_guard",
  "RewardID": "reward_monster_elite_scrap_guard"
}
```

节点：

```json
{
  "NodeType": "CombatNode",
  "MonsterIDs": ["mob_scavenger_bug"],
  "RewardID": "reward_node_combat_layer1_bonus",
  "Weight": 60
}
```

订单：

```json
{
  "OrderID": "order_workshop_spore_core_frame",
  "RewardID": "reward_order_workshop_spore_core_frame"
}
```

## 8. 校验规则

*   `RewardID` 必须唯一。
*   文件名建议与 `RewardID` 一致。
*   `Type=Item` 的 `ItemID` 必须存在。
*   `Type=RewardRef` 的 `RewardID` 必须存在。
*   `RewardRef` 必须检测循环引用和最大展开深度。
*   权重池中 `Weight <= 0` 的条目应被忽略并输出警告。
*   `Guaranteed` 与 `WeightedPools` 至少有一项非空；明确空奖励必须有 `RewardRole` 和 `AuditNotes` 说明。
*   `Nothing` 不允许出现在 `Guaranteed`。
*   `BossDrop` 必须有至少一个成长钩子或明确的剧情 / 解锁钩子。
*   `OrderReward` 必须能解释金币、声望、信任、图纸、解锁或黑市收益来源。
*   `Money` 奖励应按层级预算检查上下限，避免破坏经济压力。
*   节点奖励的 `Tags` / `RewardRole` 应与调用节点类型一致。
*   调用方配置的 `RewardID` 缺失时应警告，并在 MVP 迁移期 fallback 到旧 `LootPool`。

## 9. 当前奖励表状态

已落地文件：

*   `reward_monster_mob_scavenger_bug.json`
*   `reward_monster_elite_scrap_guard.json`
*   `reward_monster_mob_acid_slime.json`
*   `reward_monster_elite_mutant_amalgam.json`
*   `reward_node_treasure_layer1.json`
*   `reward_node_event_layer1.json`
*   `reward_node_treasure_layer2.json`
*   `reward_node_event_layer2.json`

当前文件只是可运行基础奖励表，不代表正式内容已完成：

* `reward_monster_elite_scrap_guard` 和 `reward_monster_elite_mutant_amalgam` 仍带 Boss 保底职责，后续需回归精英奖励。
* 第一层、第二层正式 Boss 奖励表尚未拆出。
* 第三层普通怪、精英、Boss、宝箱和事件奖励尚未落地。
* 订单奖励、声望 / 信任 / 图纸 / 解锁奖励、RoomMemory / ScenarioEvent 奖励尚未落地。

节点奖励已接入 `DungeonOutcomeNode`：

* `TreasureNode` 和 `EventNode` 可通过 `RewardID` 引用奖励表。
* 节点奖励生成后进入战利品拾取面板，不直接塞入背包。
* 玩家确认拾取后，已放入背包的物品进入本轮战利品账本；未放入背包的物品按丢失处理。
* `RestStopNode` / `HazardNode` 当前主要通过 `OutcomeEffects` 处理资源变化，不强制配置奖励表。
