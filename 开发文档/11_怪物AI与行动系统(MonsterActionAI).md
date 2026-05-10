# 怪物 AI 与行动系统 (MonsterActionAI)

> **定位：** 本文档定义怪物回合中“怪物决定做什么、对谁做、怎么结算”的程序架构。目标是把普通攻击、技能、背包干涉、状态施加、召唤、蓄力等行为统一为数据驱动的 `MonsterAction`，避免把怪物逻辑继续写死在 `CombatSystem` 或 `MonsterFighter.Attack()` 中。
>
> **当前阶段目标：** 先落地轻量的“可替换 AI Selector + Action 执行器”，支持 MVP 的普通攻击、酸液软体减伤、畸变融合体强塞诅咒物。暂不实现完整行为树，但为后续行为树接入预留边界。

---

## 1. 设计目标

当前怪物回合逻辑近似为：

```text
CombatSystem.StartEnemyTurn()
    foreach enemy:
        enemy.Attack(firstAlivePlayer)
```

这种方式能跑通基础战斗，但很快会遇到问题：

*   普通攻击、技能、背包干涉、召唤等会散落在 `CombatSystem`、`MonsterFighter` 或各类 if/switch 中。
*   怪物 AI 无法通过配置扩展，只能程序写死。
*   酸液软体、畸变融合体这类“规则恶心”的怪物需要直接改背包/武器运行态，容易污染战斗主循环。
*   后续如果上行为树，不应推翻现有技能执行逻辑。

因此引入独立的 `MonsterActionAI`：

```text
CombatSystem
    |
    v
MonsterActionRunner.ExecuteTurn(monster, combatContext)
    |
    v
IMonsterActionSelector.SelectAction(context)
    |
    v
MonsterActionFactory.Create(actionConfig).Execute(context)
```

核心原则：

*   **CombatSystem 只管回合推进。** 它不理解“酸液腐蚀”“塞诅咒物”“召唤小怪”等细节。
*   **MonsterAction 只管具体行为。** 普攻也是 Action，技能也是 Action。
*   **AI Selector 只管选哪个 Action。** 第一版用权重随机，未来可换优先级、阶段 AI 或行为树。
*   **TargetSelector 独立。** 技能不重复写“找随机武器”“找最低血目标”“找背包空位”的逻辑。
*   **临时战斗修正独立。** ReduceDamage 不永久改物品配置，而是挂临时 `CombatRuntimeModifier`。

---

## 2. 与行为树的关系

第一版不是完整行为树，而是轻量 AI：

```text
筛选可用行动 -> 按权重/条件选一个 -> 执行行动
```

它没有 `Selector / Sequence / Decorator / Blackboard` 那套完整行为树节点系统。这样做是刻意的：

*   MVP 怪物数量少，行为目标明确。
*   当前最需要验证的是“怪物能制造局内压力”，不是复杂智能。
*   完整行为树会增加编辑器、调试、黑板状态和可视化成本。

但架构要预留升级口：

```csharp
public interface IMonsterActionSelector {
    MonsterActionConfig SelectAction(MonsterActionContext context, MonsterAIConfig aiConfig);
}
```

第一版：

```text
WeightedRandomSelector
```

后续可替换为：

```text
PrioritySelector
PhaseSelector
ConditionalSelector
BehaviorTreeSelector
```

只要 `MonsterActionRunner` 依赖接口而不是具体 selector，未来接行为树时只替换“如何选行动”，不重写“行动怎么执行”。

---

## 3. 配置结构

`MonsterEntity.GridInterference`、`GridInterferenceParams`、`DamageValue`、`AttacksPerTurn` 不再作为运行时入口。MonsterActionAI 落地时必须一次性迁到 `AI.Actions`，不保留旧字段兜底。若迁移出现问题，用 Git 回退，不在代码中长期维护两套路径。

### 3.1 MonsterEntity 新结构

```json
{
  "MonsterID": "mob_acid_slime",
  "Name": "酸液软体",
  "Layer": 2,
  "HP": 80,
  "RewardID": "reward_monster_mob_acid_slime",
  "AI": {
    "Selector": "WeightedRandom",
    "Actions": [
      {
        "ActionID": "acid_basic_attack",
        "ActionType": "DamageTarget",
        "Target": "RandomPlayer",
        "Weight": 70,
        "CooldownTurns": 0,
        "UsesPerCombat": 0,
        "Condition": "Always",
        "Params": {
          "Damage": 30,
          "RepeatCount": 1
        }
      },
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

### 3.2 MonsterAIConfig

```csharp
[Serializable]
public class MonsterAIConfig {
    public string Selector = "WeightedRandom";
    public List<MonsterActionConfig> Actions = new List<MonsterActionConfig>();
}
```

字段说明：

| 字段 | 类型 | 说明 |
| :--- | :--- | :--- |
| `Selector` | string | 行动选择器。MVP 为 `WeightedRandom`。 |
| `Actions` | array | 怪物可执行的行动列表。 |

### 3.3 MonsterActionConfig

```csharp
[Serializable]
public class MonsterActionConfig {
    public string ActionID;
    public string ActionType;
    public string Target;
    public int Weight = 100;
    public int CooldownTurns;
    public int UsesPerCombat;
    public string Condition = "Always";
    public Dictionary<string, object> Params;
}
```

字段说明：

| 字段 | 类型 | 说明 |
| :--- | :--- | :--- |
| `ActionID` | string | 行动实例 ID，用于冷却、日志、调试。 |
| `ActionType` | string | 行动类型，对应 `MonsterActionBase` 子类。 |
| `Target` | string | 目标选择器 ID，例如 `RandomPlayerWeapon`。 |
| `Weight` | int | 权重随机选择时的权重。小于等于 0 视为不可被随机选中。 |
| `CooldownTurns` | int | 行动使用后的冷却回合数。 |
| `UsesPerCombat` | int | 每场战斗最多使用次数。0 表示不限。 |
| `Condition` | string | 行动条件。MVP 支持少量字符串条件，未来接条件系统。 |
| `Params` | object | 行动专属参数。 |

### 3.4 Params 类型建议

Unity `JsonUtility` 不适合动态字典。当前项目已使用 Newtonsoft.Json，因此建议：

```csharp
public Dictionary<string, JToken> Params;
```

并提供安全读取工具：

```csharp
public int GetInt(string key, int defaultValue = 0);
public float GetFloat(string key, float defaultValue = 0f);
public string GetString(string key, string defaultValue = "");
public List<string> GetStringList(string key);
```

这样新增 Action 时不需要改基础数据容器字段。

### 3.5 字符串配置与枚举运行时

JSON 表中继续使用字符串，保证策划、数值 agent 和美术/内容 agent 可读：

```json
{
  "ActionType": "ReduceWeaponDamage",
  "Target": "RandomPlayerWeapon",
  "Condition": "PlayerHasWeapon"
}
```

程序运行时不应在各处散落 `normalized == "RandomPlayerWeapon"` 这类裸字符串判断。当前实现通过 `MonsterActionConfigParser` 将字符串集中解析为：

*   `MonsterAISelectorType`
*   `MonsterActionType`
*   `MonsterTargetType`
*   `MonsterActionConditionType`

`ConfigValidator` 负责在加载后检查未知枚举值，并校验 `ActionType` 与 `Target` 是否兼容。后续新增行为时，优先扩展枚举、解析器、兼容性校验与对应 Action，而不是在业务逻辑里新增零散字符串分支。

---

## 4. 运行时核心类

建议目录：

```text
UnityClient/Assets/Scripts/Combat/MonsterAI/
    MonsterActionRunner.cs
    MonsterActionFactory.cs
    MonsterActionBase.cs
    MonsterActionContext.cs
    MonsterActionRuntimeState.cs
    MonsterActionParamReader.cs
    Selectors/
        IMonsterActionSelector.cs
        WeightedRandomMonsterActionSelector.cs
    Targeting/
        MonsterTargetSelector.cs
        MonsterActionTargets.cs
    Actions/
        DamageTargetAction.cs
        ReduceWeaponDamageAction.cs
        AddCursedItemAction.cs
```

### 4.1 MonsterActionRunner

职责：怪物回合入口，协调 selector、cooldown、action execution。

```csharp
public class MonsterActionRunner {
    public void ExecuteTurn(MonsterFighter actor, MonsterActionContext context) {
        MonsterAIConfig ai = actor.DataRef.AI;
        MonsterActionConfig selected = _selector.SelectAction(context, ai);
        MonsterActionBase action = MonsterActionFactory.Create(selected);
        action.Execute(context);
        context.RuntimeState.MarkUsed(actor, selected);
    }
}
```

注意：

*   一个怪物一回合默认执行一个 Action。
*   如果要表达 `AttacksPerTurn` 风格，必须在 `DamageTargetAction.Params.RepeatCount` 中配置。
*   如果没有 AI 配置，视为配置错误，由 `ConfigValidator` 阻断；运行时不读取旧 `DamageValue / AttacksPerTurn`。

### 4.2 MonsterActionContext

```csharp
public class MonsterActionContext {
    public CombatSystem Combat;
    public MonsterFighter Actor;
    public CombatFaction PlayerFaction;
    public CombatFaction EnemyFaction;
    public DollEntity PlayerDoll;
    public BackpackGrid PlayerGrid;
    public MonsterActionRuntimeState RuntimeState;
}
```

上下文只提供规则需要的信息，不直接依赖 UI。

### 4.3 MonsterActionRuntimeState

记录冷却和使用次数：

```csharp
public class MonsterActionRuntimeState {
    public int GetCooldown(string actorInstanceID, string actionID);
    public int GetUsedCount(string actorInstanceID, string actionID);
    public void MarkUsed(MonsterFighter actor, MonsterActionConfig action);
    public void TickCooldownsAtEnemyTurnStart();
    public void Clear();
}
```

MVP 可用 `MonsterFighter.RuntimeID` 或战斗内分配的序号作为 actor key。不要直接只用 `MonsterID`，因为同一战斗可能有两只同类怪。

---

## 5. AI Selector

### 5.1 WeightedRandomSelector

第一版选择流程：

```text
1. 遍历 Actions
2. 过滤 Weight <= 0
3. 过滤冷却中 Action
4. 过滤 UsesPerCombat 已用完 Action
5. 过滤 Condition 不满足 Action
6. 按 Weight 随机选一个
7. 如果没有可用 Action，记录错误并跳过本次行动；正常配置应由 `ConfigValidator` 保证至少存在一个可用行动
```

接口：

```csharp
public interface IMonsterActionSelector {
    MonsterActionConfig SelectAction(MonsterActionContext context, MonsterAIConfig aiConfig);
}
```

### 5.2 条件系统 MVP 版本

先用字符串条件即可：

| Condition | 含义 |
| :--- | :--- |
| `Always` | 永远可用 |
| `PlayerHasWeapon` | 玩家背包中存在武器 |
| `PlayerGridHasSpace` | 玩家背包存在可放入指定物品的空间 |
| `ActorHPBelowPercent:0.5` | 怪物血量低于 50% |
| `PlayerHPBelowPercent:0.5` | 玩家血量低于 50% |

实现上建议 `MonsterActionConditionEvaluator`，后续可复用全局 `ConditionEvaluator`。

---

## 6. Target Selector

不要让每个 Action 自己查找目标。目标选择统一走 `MonsterTargetSelector`。

### 6.1 目标类型

```csharp
public class MonsterActionTargets {
    public List<FighterEntity> Fighters = new List<FighterEntity>();
    public List<ItemEntity> Items = new List<ItemEntity>();
    public List<Vector2Int> Cells = new List<Vector2Int>();
}
```

### 6.2 MVP Target 列表

| Target | 返回 |
| :--- | :--- |
| `FirstAlivePlayer` | 第一个存活玩家 Fighter |
| `RandomPlayer` | 随机存活玩家 Fighter |
| `LowestHPPlayer` | 当前 HP 最低玩家 Fighter |
| `RandomPlayerWeapon` | 玩家背包中的随机武器 Item |
| `AllPlayerWeapons` | 玩家背包中的所有武器 Item |
| `PlayerGridFirstFit` | 玩家背包中可放置指定物品的第一个坐标 |

当前只有一个人偶，但仍按 Faction/Fighter 写，避免后续加召唤物或多角色时重构。

---

## 7. Action 类型设计

### 7.1 DamageTargetAction

普通攻击也使用 Action：

```json
{
  "ActionType": "DamageTarget",
  "Target": "FirstAlivePlayer",
  "Params": {
    "Damage": 30,
    "RepeatCount": 1
  }
}
```

执行：

```csharp
target.TakeDamage(damage);
GameEventBus.PublishAttackAction(actor.Name, target.Name, actionName);
GameEventBus.PublishDamageDealt(actor.Name, target.Name, damage);
```

### 7.2 ReduceWeaponDamageAction

酸液软体的腐蚀技能。

配置：

```json
{
  "ActionType": "ReduceWeaponDamage",
  "Target": "RandomPlayerWeapon",
  "Params": {
    "Multiplier": 0.5,
    "DurationPlayerTurns": 1
  }
}
```

执行：

```text
1. TargetSelector 找到随机玩家武器。
2. 创建 CombatRuntimeModifier。
3. 将 modifier 注册到 RuntimeModifierSystem。
4. 重新计算玩家背包派生数值。
5. 发日志/事件提示。
```

不要永久修改：

```text
ItemCombatComponent.BaseValue
ItemConfig JSON
```

只影响运行时：

```text
RuntimeDamage
```

### 7.3 AddCursedItemAction

畸变融合体的强塞污染物。

配置：

```json
{
  "ActionType": "AddCursedItem",
  "Target": "PlayerGridFirstFit",
  "Params": {
    "ItemID": "loot_gear_scrap",
    "OverrideTags": ["Cursed", "Toxic"],
    "OverrideValue": 0
  }
}
```

执行：

```text
1. 根据 ItemID 创建物品实例。
2. 应用 OverrideTags / OverrideValue / OverrideName 等覆盖。
3. 在玩家背包查找第一个可放置位置。
4. 放入背包，触发 GridSolver.RecalculateAllEffects。
5. 广播 GameEventBus.PublishItemPlaced。
6. 如果没有空间，记录失败并执行预设的无空间处理。
```

MVP 无空间处理：

```text
只记录 warning，不造成额外惩罚。
```

后续可扩展：

```text
NoSpaceHandling = LoseSAN / DamagePlayer / DropRandomItem / ReplaceItem
```

---

## 8. Runtime Modifier 系统

`ReduceWeaponDamage` 不应直接写死进 `GridSolver` 或 `ItemEntity`，建议新增临时战斗修正层。

### 8.1 数据结构

```csharp
public class CombatRuntimeModifier {
    public string ModifierID;
    public string SourceID;
    public string TargetItemInstanceID;
    public string Stat; // RuntimeDamage
    public float Multiplier = 1f;
    public int RemainingPlayerTurns;
}
```

### 8.2 RuntimeModifierSystem

```csharp
public class RuntimeModifierSystem {
    public void AddModifier(CombatRuntimeModifier modifier);
    public void ApplyItemModifiers(ItemEntity item);
    public void TickPlayerTurnEnd();
    public void ClearCombatModifiers();
}
```

应用顺序：

```text
1. GridSolver 重置物品 RuntimeDamage = BaseValue
2. GridSolver 应用物品/义体的增益
3. RuntimeModifierSystem.ApplyAll() 应用临时战斗修正
```

原因：

*   临时减伤应该压在所有正向构筑之后，让玩家感到“这把武器这回合真的被腐蚀了”。
*   战斗结束必须清空临时 modifier，不能污染局外数据。

### 8.3 生命周期

*   战斗开始：`RuntimeModifierSystem.ClearCombatModifiers()`
*   玩家回合开始或结束：按设计 tick duration。建议 MVP 用玩家回合结束 tick。
*   战斗结束：清空所有 combat-only modifier。

---

## 9. CombatSystem 接入点

当前敌方回合应从：

```csharp
foreach (var fighter in EnemyFaction.Fighters) {
    MonsterFighter enemy = fighter as MonsterFighter;
    if (enemy != null && enemy.RuntimeHP > 0 && !PlayerFaction.IsWipedOut()) {
        FighterEntity target = PlayerFaction.Fighters.Find(candidate => candidate.RuntimeHP > 0);
        if (target != null) {
            for (int i = 0; i < enemy.DataRef.AttacksPerTurn; i++) {
                enemy.Attack(target);
            }
        }
    }
}
```

迁移为：

```csharp
foreach (var fighter in EnemyFaction.Fighters) {
    MonsterFighter enemy = fighter as MonsterFighter;
    if (enemy == null || enemy.RuntimeHP <= 0 || PlayerFaction.IsWipedOut()) {
        continue;
    }

    MonsterActionContext context = MonsterActionContext.FromCombat(this, enemy);
    MonsterActionRunner.ExecuteTurn(context);
}
```

`MonsterFighter.Attack()` 可以保留为 `DamageTargetAction` 内部复用的底层伤害方法，但不再承担 AI 决策职责。

---

## 10. 一次性重构策略

### 10.1 原则：不保留兼容期

MonsterActionAI 落地时不做双轨兼容：

*   `CombatSystem` 敌方回合只调用 `MonsterActionRunner`。
*   `MonsterFighter` 不再通过 `DamageValue / AttacksPerTurn` 决策攻击。
*   `GridInterference / GridInterferenceParams` 不再作为运行时入口。
*   所有怪物配置必须在同一批变更中补齐 `AI.Actions`。
*   `ConfigValidator` 对缺失 `AI.Actions` 的怪物报错，而不是 warning。
*   若重构出现大问题，使用 Git revert 回退整批改动，不在代码里维护旧新版并存。

这样做会牺牲一点迁移过程的柔性，但能避免战斗系统长期背负两套怪物行为路径。怪物 AI 这类高频扩展系统，一旦保留旧字段兜底，后续定位 bug 会很痛苦。

### 10.2 旧字段删除对照

迁移前：

```json
{
  "DamageValue": 30,
  "AttacksPerTurn": 1,
  "GridInterference": "ReduceDamage",
  "GridInterferenceParams": {
    "Target": "RandomWeapon",
    "Effect": 0.5,
    "DurationTurns": 1
  }
}
```

重构后：

```json
{
  "AI": {
    "Selector": "WeightedRandom",
    "Actions": [
      {
        "ActionID": "acid_basic_attack",
        "ActionType": "DamageTarget",
        "Target": "FirstAlivePlayer",
        "Weight": 70,
        "Params": {
          "Damage": 30,
          "RepeatCount": 1
        }
      },
      {
        "ActionID": "acid_corrode_weapon",
        "ActionType": "ReduceWeaponDamage",
        "Target": "RandomPlayerWeapon",
        "Weight": 30,
        "CooldownTurns": 2,
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

### 10.3 必须同批完成的删除项

数据结构层：

*   从 `MonsterEntity` 删除或停止反序列化依赖 `DamageValue`。
*   从 `MonsterEntity` 删除或停止反序列化依赖 `AttacksPerTurn`。
*   从 `MonsterEntity` 删除 `GridInterference`。
*   从 `MonsterEntity` 删除 `GridInterferenceParams`。

配置层：

*   所有 `Monsters/*.json` 必须补齐 `AI.Selector` 与 `AI.Actions`。
*   普通攻击必须显式配置为 `DamageTargetAction`。
*   酸液软体腐蚀必须显式配置为 `ReduceWeaponDamageAction`。
*   畸变融合体强塞物品必须显式配置为 `AddCursedItemAction`。

代码层：

*   `CombatSystem.StartEnemyTurn()` 不再读取怪物旧攻击字段。
*   `MonsterFighter.Attack()` 可以降级为底层伤害辅助方法，也可以由 `DamageTargetAction` 直接调用 `target.TakeDamage()`。
*   `ConfigValidator` 不再提示旧字段“未实现 warning”，而是校验新 AI 配置是否完整。

---

## 11. ConfigValidator 扩展

MonsterActionAI 落地后，`ConfigValidator` 不再把旧 `GridInterference` 当作可接受的过渡字段。校验规则必须改为“怪物 AI 配置缺失即错误”：

*   每个怪物必须配置 `AI`。
*   每个怪物必须至少有一个 `AI.Actions`。
*   `MonsterEntity.AI.Selector` 必须存在于 `MonsterActionSelectorFactory`。
*   `ActionType` 必须存在于 `MonsterActionFactory`。
*   `Target` 必须存在于 `MonsterTargetSelector`。
*   `Weight >= 0`。
*   `CooldownTurns >= 0`。
*   `UsesPerCombat >= 0`。
*   `DamageTargetAction` 必须配置 `Damage > 0`。
*   `ReduceWeaponDamageAction` 必须配置 `0 < Multiplier < 1`、`DurationPlayerTurns > 0`。
*   `AddCursedItemAction` 必须配置存在的 `ItemID`。
*   `PlayerGridFirstFit` 类目标如果依赖 `ItemID`，校验该物品是否存在且有 Grid。
*   配置中出现 `DamageValue`、`AttacksPerTurn`、`GridInterference`、`GridInterferenceParams` 旧字段时应报错，提醒它们已被 `AI.Actions` 取代。

---

## 12. 自动化测试计划

### 12.1 MonsterActionFactorySmokeTest

验证：

*   `DamageTarget`
*   `ReduceWeaponDamage`
*   `AddCursedItem`

都能通过 `ActionType` 创建。

### 12.2 MonsterWeightedSelectorTest

使用固定随机源验证：

*   冷却中的 Action 不会被选中。
*   `Condition` 不满足的 Action 不会被选中。
*   权重选择在固定随机源下稳定。

### 12.3 MonsterDamageActionTest

验证普通攻击从 Action 系统执行，造成与 `DamageTargetAction.Params.Damage` 一致的伤害。

### 12.4 ReduceWeaponDamageActionTest

验证：

*   玩家背包有武器时，随机武器被挂 modifier。
*   `RuntimeDamage` 被乘以 `Multiplier`。
*   持续时间结束后，伤害恢复。
*   战斗结束后 modifier 清空。

### 12.5 AddCursedItemActionTest

验证：

*   有空间时，诅咒物被放入背包。
*   覆盖 tags 生效。
*   覆盖 value 生效。
*   没空间时不会崩溃，有 warning。

### 12.6 CombatEnemyTurnIntegrationTest

验证 `CombatSystem.StartEnemyTurn()` 不再直接调用写死攻击循环，而是通过 `MonsterActionRunner` 执行怪物行动。

---

## 13. MVP 落地顺序

建议按以下顺序推进。注意：这是一个原子重构任务，完成前不要提交半套兼容实现。

1.  新增 `MonsterAIConfig`、`MonsterActionConfig` 数据结构。
2.  新增 `MonsterActionBase`、`MonsterActionFactory`、`MonsterActionRunner`。
3.  新增 `WeightedRandomMonsterActionSelector`。
4.  新增 `MonsterTargetSelector`，先支持 `FirstAlivePlayer`、`RandomPlayerWeapon`、`PlayerGridFirstFit`。
5.  实现 `DamageTargetAction`、`ReduceWeaponDamageAction`、`AddCursedItemAction`。
6.  新增 `RuntimeModifierSystem`。
7.  给 `BackpackGrid` 增加 `TryPlaceFirstAvailable(ItemEntity item, out int x, out int y)`。
8.  一次性迁移所有 `Monsters/*.json` 到 `AI.Actions`，包括普通怪和 Boss。
9.  删除或停止依赖 `DamageValue`、`AttacksPerTurn`、`GridInterference`、`GridInterferenceParams`。
10. 接入 `CombatSystem.StartEnemyTurn()`，敌方回合只走 `MonsterActionRunner`。
11. 扩展 `ConfigValidator`，让缺失 AI 或残留旧字段直接失败。
12. 补自动化测试并跑全量回归。

第一批提交应包含完整可运行链路：数据结构、Action 执行、配置迁移、校验、测试。不要把“新系统已写好但旧系统仍兜底”作为中间状态长期保留。

---

## 14. 后续扩展方向

### 14.1 更多 Action

*   `ApplyStatusAction`：给玩家或物品施加状态。
*   `SummonMonsterAction`：召唤小怪加入 EnemyFaction。
*   `ModifyAPAction`：偷取或锁定玩家 AP。
*   `LockGridCellAction`：临时污染/封锁背包格。
*   `MoveItemAction`：打乱玩家背包中的物品位置。
*   `ChargeAction`：进入蓄力状态，下回合释放强技能。
*   `EscapeAction`：怪物逃跑，提前结束战斗奖励或改变掉落。

### 14.2 行为树接入

未来如果需要复杂 Boss AI，可新增：

```json
{
  "AI": {
    "Selector": "BehaviorTree",
    "TreeID": "bt_mutant_amalgam"
  }
}
```

`BehaviorTreeSelector` 只负责根据 Tree/Blackboard 选出一个 `MonsterActionConfig`，不直接执行技能。这样行为树成为“决策层”，MonsterAction 仍是“执行层”。

### 14.3 黑板数据

可逐步增加：

*   当前回合数
*   怪物已使用过的 Action
*   玩家背包剩余空格
*   玩家武器数量
*   玩家是否携带 Toxic/Cursed 物品
*   Boss 阶段

黑板不要在 MVP 一开始就做复杂，只在 selector 需要时逐步引入。

---

## 15. 当前落地结论

截至 2026-05-10，MonsterActionAI 第一轮已完成落地：

*   `MonsterEntity` 已改为 `AI.Actions` 数据结构，不再声明 `DamageValue`、`AttacksPerTurn`、`GridInterference`、`GridInterferenceParams`。
*   `CombatSystem.StartEnemyTurn()` 已改为通过 `MonsterActionRunner` 执行敌方行动，不再直接调用 `MonsterFighter.Attack()` 的旧普通攻击循环。
*   已实现 MVP 三类 Action：`DamageTarget`、`ReduceWeaponDamage`、`AddCursedItem`。
*   `ReduceWeaponDamage` 使用战斗运行时修正，不永久修改物品配置；玩家回合结束后按 `DurationPlayerTurns` 过期。
*   `AddCursedItem` 使用 `BackpackGrid.TryPlaceFirstAvailable()` 往玩家背包空位塞入物品，并支持 `OverrideTags`、`OverrideValue`。
*   4 个怪物配置已统一迁移到 `AI.Actions`；`ConfigValidator` 对缺失 AI 或残留旧行为字段直接报错。
*   `MonsterActionAITest` 覆盖敌方回合 Runner、腐蚀武器伤害、塞入诅咒物三条链路。

当前保留的 `LootPool` 只属于奖励系统迁移期 fallback，不再承载怪物行动或背包干涉逻辑。后续如果奖励系统完全去掉旧 fallback，可再单独移除 `MonsterEntity.LootPool` 与配置中的旧掉落池。
