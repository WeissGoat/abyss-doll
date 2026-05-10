# 奖励与掉落系统 (RewardSystem)

> **定位：** 统一处理战斗掉落、节点奖励、事件奖励、宝箱奖励、任务奖励等所有“从规则源头生成收益”的逻辑。调用方只配置 `RewardID`，不直接写物品权重或保底规则。

## 1. 设计目标

当前 `CombatNode` 直接读取怪物 `LootPool` 并随机生成一个物品。这种方式能跑通 MVP，但会很快遇到问题：

*   Boss 必掉核心、普通随机掉落、额外奖励会散落在不同调用方里。
*   战斗、宝箱、事件、任务都需要掉落时，容易复制随机逻辑。
*   配置层无法复用奖励包，例如“1 层普通怪基础奖励”“2 层高危素材奖励”。
*   后续掉落率加成、诅咒奖励、事件奖励倍率、保底奖励统计都缺统一入口。

因此引入独立的 `RewardSystem`：

```text
Source Config
Monster / Node / Event / Quest
        |
        | RewardID
        v
RewardSystem.Roll(rewardID, context)
        |
        v
RewardRollResult
        |
        +-> CombatLootPickupResult / Stash / Money / Future Reward Sink
```

## 2. 核心原则

*   **调用方只关心 RewardID。** 怪物、节点、事件、宝箱等不再直接维护权重池。
*   **奖励表负责组合。** 一个奖励表可以同时包含保底奖励、权重奖励、空掉落、嵌套奖励引用。
*   **生成和归属分离。** `RewardSystem` 负责“生成了什么”，调用方负责“这些奖励进入拾取面板、仓库、金币还是事件结算”。
*   **可回归测试。** 随机源必须可注入或可固定种子，保证掉落逻辑可测试。
*   **兼容迁移。** MVP 过渡期允许怪物旧字段 `LootPool` 作为 fallback，但新配置应迁移到 `RewardID`。

## 3. 配置结构

奖励表放在：

```text
配置表(JSON)/Rewards/
```

推荐一个奖励表一文件，文件名与 `RewardID` 保持一致。

### 3.1 RewardConfig

```json
{
  "RewardID": "reward_monster_elite_scrap_guard",
  "Name": "1层守门人奖励",
  "Tags": ["Monster", "Boss", "Layer1"],
  "Guaranteed": [
    {
      "Type": "Item",
      "ItemID": "mat_core_tier1",
      "Count": 1
    }
  ],
  "WeightedPools": [
    {
      "PoolID": "bonus_loot",
      "RollCount": 1,
      "AllowDuplicate": false,
      "Entries": [
        { "Type": "Item", "ItemID": "gear_tactical_blade", "Weight": 35, "Count": 1 },
        { "Type": "Item", "ItemID": "loot_rusty_coil", "Weight": 80, "Count": 1 },
        { "Type": "Item", "ItemID": "con_cheap_sedative", "Weight": 20, "Count": 1 },
        { "Type": "Nothing", "Weight": 10 }
      ]
    }
  ]
}
```

### 3.2 字段说明

| 字段 | 类型 | 说明 |
| :--- | :--- | :--- |
| `RewardID` | string | 奖励表唯一 ID。调用方只配置这个 ID。 |
| `Name` | string | 策划可读名称，仅用于编辑器和日志。 |
| `Tags` | array | 奖励表标签，用于筛选、调试、后续掉落率修正。 |
| `Guaranteed` | array | 保底奖励，必定生成。 |
| `WeightedPools` | array | 权重池列表，每个池子可独立掷骰。 |

### 3.3 RewardEntry

| 字段 | 类型 | 说明 |
| :--- | :--- | :--- |
| `Type` | enum | `Item`、`Money`、`RewardRef`、`Nothing`。MVP 优先实现 `Item` 和 `Nothing`。 |
| `ItemID` | string | 当 `Type=Item` 时，指向 `Items` 配置。 |
| `Money` | int | 当 `Type=Money` 时，生成金币。 |
| `RewardID` | string | 当 `Type=RewardRef` 时，引用另一个奖励表。 |
| `Weight` | int | 权重池条目权重。保底奖励不需要。 |
| `Count` | int | 固定数量，默认 1。 |
| `MinCount` / `MaxCount` | int | 可选，随机数量范围。若存在则优先于 `Count`。 |
| `Condition` | string | 可选，后续接条件系统，例如 `Layer>=2`、`HasTag:ToxicBoost`。 |

### 3.4 WeightedPool

| 字段 | 类型 | 说明 |
| :--- | :--- | :--- |
| `PoolID` | string | 池子 ID，便于日志定位。 |
| `RollCount` | int | 掷骰次数。默认 1。 |
| `AllowDuplicate` | bool | 同一个池子多次掷骰时是否允许抽到同一条目。 |
| `Entries` | array | 权重条目。 |

## 4. 调用方配置

### 4.1 怪物

怪物不再直接配置掉落池，而是配置奖励表 ID：

```json
{
  "MonsterID": "elite_scrap_guard",
  "RewardID": "reward_monster_elite_scrap_guard"
}
```

旧字段 `LootPool` 迁移期保留，但标记为 deprecated。运行时优先读取 `RewardID`；如果为空，再 fallback 到旧 `LootPool`。

### 4.2 深渊节点

节点可以配置额外奖励，用于宝箱、安全区事件、Boss 额外清算等：

```json
{
  "NodeType": "CombatNode",
  "MonsterIDs": ["mob_scavenger_bug"],
  "RewardID": "reward_node_combat_layer1_bonus",
  "Weight": 60
}
```

节点奖励与怪物奖励可以并存。解析顺序建议为：

1.  逐个解析怪物 `RewardID`。
2.  解析节点自身 `RewardID`。
3.  合并为一个 `RewardRollResult`。
4.  交给战利品拾取面板。

### 4.3 事件与任务

后续事件、订单、宝箱、祭坛同样只配置 `RewardID`。调用方根据奖励类型决定是否进入背包、仓库、金币或声望。

## 5. 运行时结构

### 5.1 数据对象

```csharp
[Serializable]
public class RewardConfig {
    public string RewardID;
    public string Name;
    public List<string> Tags;
    public List<RewardEntry> Guaranteed;
    public List<RewardPool> WeightedPools;
}

[Serializable]
public class RewardPool {
    public string PoolID;
    public int RollCount = 1;
    public bool AllowDuplicate = true;
    public List<RewardEntry> Entries;
}

[Serializable]
public class RewardEntry {
    public string Type;
    public string ItemID;
    public string RewardID;
    public int Money;
    public int Weight;
    public int Count = 1;
    public int MinCount;
    public int MaxCount;
    public string Condition;
}
```

### 5.2 上下文

```csharp
public class RewardContext {
    public string SourceType;   // Monster, Node, Event, Quest
    public string SourceID;
    public int LayerID;
    public string NodeID;
    public PlayerProfile Player;
    public DollEntity ActiveDoll;
}
```

上下文不应让奖励表直接依赖 UI。它只提供规则判断需要的信息，例如层数、来源、玩家状态、未来掉落率修正等。

### 5.3 结果

```csharp
public class RewardRollResult {
    public string RootRewardID;
    public List<RewardGrant> Grants;
    public List<ItemEntity> GeneratedItems;
    public int Money;
    public List<string> Logs;
}
```

MVP 阶段 `GeneratedItems` 直接用于 `CombatLootPickupResult.OfferedItems`。后续如果奖励类型变多，可让调用方根据 `RewardGrant.Type` 分发。

## 6. RewardSystem 职责

推荐新增纯 C# 模块：

```text
UnityClient/Assets/Scripts/Rewards/RewardSystem.cs
UnityClient/Assets/Scripts/DataEntities/RewardConfig.cs
```

核心 API：

```csharp
public class RewardSystem {
    public RewardRollResult Roll(string rewardID, RewardContext context);
}
```

执行流程：

1.  从 `ConfigManager.Rewards` 查找 `RewardConfig`。
2.  解析 `Guaranteed`，生成保底奖励。
3.  逐个解析 `WeightedPools`。
4.  处理 `Nothing`、`Item`、`Money`、`RewardRef`。
5.  对 `RewardRef` 做递归深度限制，避免循环引用。
6.  返回 `RewardRollResult`。

## 7. 随机与测试

掉落系统必须可回归。实现时不要把随机完全写死在 `UnityEngine.Random` 上。

建议提供接口：

```csharp
public interface IRewardRandom {
    int Range(int minInclusive, int maxExclusive);
}
```

默认运行时使用 Unity 随机源；测试时使用固定种子随机源。

至少需要以下测试：

*   `RewardSystemSmokeTest`：保底奖励一定出现。
*   `RewardWeightedPoolTest`：固定种子下权重池结果稳定。
*   `RewardRefCycleGuardTest`：奖励表循环引用不会死循环。
*   `CombatRewardIntegrationTest`：`CombatNode` 使用怪物 `RewardID` 生成战利品拾取结果。
*   `RewardConfigValidationTest`：缺失 `ItemID`、缺失 `RewardID`、空权重池会报错或警告。

## 8. ConfigManager 接入

新增：

```csharp
public static Dictionary<string, RewardConfig> Rewards = new Dictionary<string, RewardConfig>();
```

启动加载：

```text
配置表(JSON)/Rewards/*.json -> ConfigManager.Rewards
```

引用校验：

*   所有调用方配置的 `RewardID` 必须存在。
*   所有 `Type=Item` 的 `ItemID` 必须存在。
*   所有 `Type=RewardRef` 的 `RewardID` 必须存在。
*   权重池中 `Weight <= 0` 的条目忽略并警告。
*   `RewardRef` 递归深度超过上限时中断并报错。

## 9. MVP 迁移步骤

### 第一步：新增 Rewards 配置

建立以下奖励表：

*   `reward_monster_mob_scavenger_bug`
*   `reward_monster_elite_scrap_guard`
*   `reward_monster_mob_acid_slime`
*   `reward_monster_elite_mutant_amalgam`

### 第二步：怪物配置改为 RewardID

怪物保留旧 `LootPool` 作为 fallback，但新增：

```json
{
  "RewardID": "reward_monster_elite_scrap_guard"
}
```

### 第三步：CombatNode 改用 RewardSystem

`CombatNode.PrepareLootPickupResult()` 不再直接 `RollLootForMonster()`，而是：

```text
foreach monsterID:
    monster = ConfigManager.Monsters[monsterID]
    rewardResult = RewardSystem.Roll(monster.RewardID, context)
    result.OfferedItems.AddRange(rewardResult.GeneratedItems)
```

### 第四步：补自动化测试

先覆盖 Boss 保底掉落，再覆盖普通怪随机掉落。现有战利品拾取 UI 不需要大改。

## 10. MVP 示例

### 10.1 普通怪奖励

```json
{
  "RewardID": "reward_monster_mob_scavenger_bug",
  "Name": "拾荒虫奖励",
  "Tags": ["Monster", "Layer1"],
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

### 10.2 Boss 保底奖励

```json
{
  "RewardID": "reward_monster_elite_scrap_guard",
  "Name": "废铁守卫奖励",
  "Tags": ["Monster", "Boss", "Layer1"],
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

## 11. 后续扩展点

*   掉落率加成：通过 `RewardContext` 注入全局倍率或标签倍率。
*   稀有度保底：记录连续未出稀有物次数，后续在 `RewardSystem` 中修正权重。
*   奖励预览：UI 可读取 RewardConfig 展示“可能掉落”。
*   声望/图纸/义体奖励：扩展 `RewardEntry.Type`。
*   节点奖励组合：节点自身奖励表通过 `RewardRef` 组合怪物掉落、宝箱奖励、事件奖励。

## 12. 2026-05-10 落地状态

已完成第一版可运行接入：

*   `ConfigManager` 已加载 `StreamingAssets/Configs/Rewards` 到 `ConfigManager.Rewards`。
*   新增 `RewardConfig`、`RewardSystem`、`RewardContext`、`RewardRollResult`，支持 `Guaranteed`、`WeightedPools`、`Item`、`Money`、`RewardRef`、`Nothing`。
*   `MonsterEntity` 与 `NodePoolEntry` 已支持 `RewardID`。
*   `CombatNode` 胜利结算优先通过怪物 `RewardID` 调用 `RewardSystem`，奖励表缺失时 fallback 到旧 `LootPool`。
*   已配置 4 个 MVP 怪物奖励表，`elite_scrap_guard` 保底掉落 `mat_core_tier1`，并额外执行 1 次权重掉落。
*   已补 `RewardSystemSmokeTest` 与 `CombatLootDropTest` 集成覆盖。

当前保留限制：

*   战斗拾取面板当前只接收 `Item`。如果奖励表生成 `Money`，`CombatNode` 会记录 warning，但不会直接发放金币；后续应由具体 Reward Sink 决定金币、声望、蓝图等非物品奖励的归属。
*   `Condition` 字段已预留但尚未执行条件解析。
