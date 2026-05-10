# 奖励表配置字段说明 (Rewards Config)

> 本目录用于配置所有掉落、宝箱、事件、任务、节点结算等奖励。调用方只配置 `RewardID`，具体保底、权重、空掉落和奖励组合都由奖励表负责。

## 1. 基本约定

*   一个奖励表一个 JSON 文件。
*   文件名建议与 `RewardID` 一致。
*   怪物、节点、事件等来源不再直接配置掉落池，而是引用 `RewardID`。
*   MVP 过渡期允许怪物旧字段 `LootPool` 保留为 fallback，但新内容必须走奖励表。

## 2. RewardConfig 字段

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `RewardID` | string | 奖励表全局唯一 ID | 如 `reward_monster_elite_scrap_guard` |
| `Name` | string | 策划可读名称 | 仅用于日志、编辑器和排查 |
| `Tags` | array | 奖励表标签 | 如 `Monster`、`Boss`、`Layer1` |
| `Guaranteed` | array | 保底奖励列表 | 必定生成 |
| `WeightedPools` | array | 权重奖励池列表 | 每个池独立掷骰 |

## 3. RewardEntry 字段

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `Type` | enum | 奖励条目类型 | `Item`、`Money`、`RewardRef`、`Nothing` |
| `ItemID` | string | 物品 ID | `Type=Item` 时填写，指向 `/Items` |
| `Money` | int | 金币数量 | `Type=Money` 时填写 |
| `RewardID` | string | 子奖励表 ID | `Type=RewardRef` 时填写 |
| `Weight` | int | 权重 | 仅权重池条目需要 |
| `Count` | int | 固定数量 | 默认 1 |
| `MinCount` | int | 随机数量下限 | 可选 |
| `MaxCount` | int | 随机数量上限 | 可选 |
| `Condition` | string | 条件表达式 | 可选，后续接条件系统 |

## 4. WeightedPool 字段

| 字段名 | 数据类型 | 注释说明 |
| :--- | :--- | :--- |
| `PoolID` | string | 池子 ID，用于日志定位 |
| `RollCount` | int | 掷骰次数，默认 1 |
| `AllowDuplicate` | bool | 多次掷骰时是否允许重复抽到同一条 |
| `Entries` | array | 权重条目列表 |

## 5. MVP 示例

### 5.1 普通怪：拾荒虫

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

### 5.2 Boss：废铁守卫

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

## 6. 引用方式

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

## 7. 校验规则

*   `RewardID` 必须唯一。
*   `Type=Item` 的 `ItemID` 必须存在。
*   `Type=RewardRef` 的 `RewardID` 必须存在。
*   权重池中 `Weight <= 0` 的条目应被忽略并输出警告。
*   `RewardRef` 必须检测循环引用。
*   调用方配置的 `RewardID` 缺失时应警告，并在 MVP 迁移期 fallback 到旧 `LootPool`。

## 8. 当前 MVP 奖励表

已落地文件：

*   `reward_monster_mob_scavenger_bug.json`
*   `reward_monster_elite_scrap_guard.json`
*   `reward_monster_mob_acid_slime.json`
*   `reward_monster_elite_mutant_amalgam.json`

`reward_monster_elite_scrap_guard` 是当前关键验证样例：`Guaranteed` 保底生成 `mat_core_tier1`，`WeightedPools` 额外随机生成 1 件战利品。
