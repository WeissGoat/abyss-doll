# 深渊怪物配置字段说明 (Monsters Config)

> 本目录下的 JSON 文件定义深渊战斗节点中遭遇的敌对实体。怪物的攻击、技能、背包干涉统一通过 `AI.Actions` 配置，不再使用旧的 `DamageValue`、`AttacksPerTurn`、`GridInterference` 字段。

## 字段说明表

| 字段名 (Key) | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `MonsterID` | string | 怪物的全局唯一 ID | 必填项，如 `elite_mutant_amalgam` |
| `Name` | string | 怪物显示名称 | |
| `Layer` | int | 推荐出没的深渊层数 | 用于标识怪物的数值跨度级别 |
| `HP` | int | 怪物生命值上限 | 测试 DPS 输出检测的沙袋血量 |
| `RewardID` | string | 击败该怪物后触发的奖励表 ID | 指向 `/Rewards` |
| `AI` | object | 怪物行动配置 | 必填，详见下方 `AI.Actions` |
| `LootPool` | array | 旧版掉落池 | Deprecated，仅奖励系统迁移期 fallback 使用 |

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
| `ActionType` | string | 行动类型。MVP 支持 `DamageTarget`、`ReduceWeaponDamage`、`AddCursedItem`。 |
| `Target` | string | 目标选择器。MVP 支持 `FirstAlivePlayer`、`RandomPlayer`、`LowestHpPlayer`、`RandomPlayerWeapon`、`PlayerGridFirstFit`。 |
| `Weight` | int | 权重随机选择权重。小于等于 0 不会被选中。 |
| `CooldownTurns` | int | 行动使用后的敌方回合冷却。 |
| `UsesPerCombat` | int | 单场战斗最大使用次数，0 表示不限。 |
| `Condition` | string | 行动条件。MVP 支持 `Always`、`PlayerHasWeapon`、`PlayerGridHasSpace`。 |
| `Params` | object | 行动专属参数。 |

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
