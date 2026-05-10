# 深渊与战斗循环系统 (Dungeon & Combat System)

> **定位：** 指导程序实现局内的爬塔结构与手动回合制战斗。
> **重构亮点：** 
> 1. 深渊地图采用类似《杀戮尖塔》的预生成树状图节点（NodeBase）。
> 2. 战斗采用 `FighterEntity` 包装层，区分数据存储与战斗运行状态。
> 3. **新增：** 引入 `CombatFaction`（阵营）概念，支持多对多战斗。
> 4. 战斗循环改为纯正的手动回合制，由玩家控制回合结束。

## 1. 深渊地图生成与管理 (Dungeon Tree)

深渊的路线是提前生成的，玩家一目了然。我们需要抽象出 `Layer` 和 `NodeBase`。

```csharp
// 节点基类，工厂模式产出
public abstract class NodeBase {
    public string NodeID;
    public bool IsVisited;
    public List<NodeBase> NextNodes; // 分支路线
    
    // 玩家进入该节点时触发
    public abstract void OnEnterNode();
}

public class CombatNode : NodeBase {
    public List<string> MonsterIDs; // 支持多个怪物
    public override void OnEnterNode() {
        GameManager.Instance.EnterCombatScene(MonsterIDs);
    }
}

public class SafeRoomNode : NodeBase {
    public override void OnEnterNode() {
        // 打开篝火/回复 UI
    }
}

// 楼层控制器
public class DungeonLayer {
    public int LayerID;
    public NodeBase RootNode;
    public NodeBase CurrentNode;
    
    public void GenerateMapTree() {
        // 根据配置表生成分支树结构
    }
}

// 深渊总控
public class DungeonManager : MonoBehaviour {
    public DungeonLayer CurrentLayer;
    
    public void MoveToNode(NodeBase targetNode) {
        // 验证 targetNode 是否属于 CurrentNode.NextNodes
        CurrentLayer.CurrentNode = targetNode;
        // 结算 SAN 值移动税
        GameManager.Instance.CurrentPlayer.ActiveDoll.SAN_Current -= 1;
        
        targetNode.OnEnterNode();
    }
}
```

## 2. 战斗包装器与阵营 (Fighter & Faction)

战斗发生时，决不能直接在原生的 `DollEntity` 或 `MonsterEntity` 上写乱七八糟的战斗逻辑。需要一层只存活在战斗场景的 Wrapper。
同时，引入**阵营 (Faction)** 的概念，以便未来支持“多打多”或者“召唤物”的战局。

```csharp
// 战斗阵营包装器
public class CombatFaction {
    public enum FactionType { Player, Enemy, Neutral }
    public FactionType Type;
    public List<FighterEntity> Fighters = new List<FighterEntity>();
    
    public bool IsWipedOut() {
        // 检查阵营是否全灭
        return Fighters.TrueForAll(f => f.RuntimeHP <= 0);
    }
    
    public void OnTurnStart() {
        foreach(var f in Fighters) if(f.RuntimeHP > 0) f.OnTurnStart();
    }
    
    public void OnTurnEnd() {
        foreach(var f in Fighters) if(f.RuntimeHP > 0) f.OnTurnEnd();
    }
}

// 战斗者基类
public abstract class FighterEntity {
    public CombatFaction ParentFaction; // 所属阵营
    public int RuntimeHP;
    public int RuntimeMaxHP;
    public int RuntimeShield;
    
    public abstract void Attack(FighterEntity target, ItemEntity weaponSource = null);
    public virtual void TakeDamage(int damage) {
        if(RuntimeShield > 0) {
            // 扣护盾逻辑
        }
        RuntimeHP -= damage;
        // 检测死亡
    }
    
    public virtual void OnTurnStart() { }
    public virtual void OnTurnEnd() { }
}

public class DollFighter : FighterEntity {
    public DollEntity DataRef; // 指向源数据
    
    public DollFighter(DollEntity doll, CombatFaction faction) {
        ParentFaction = faction;
        DataRef = doll;
        RuntimeHP = doll.HP_Current;
        RuntimeMaxHP = doll.HP_Max;
    }
    
    // 战斗结束时，将剩余血量同步回源数据
    public void SyncDataBack() {
        DataRef.HP_Current = RuntimeHP;
    }
}

public class MonsterFighter : FighterEntity {
    public MonsterEntity DataRef;
    public string RuntimeID;
    
    public MonsterFighter(MonsterEntity monster, CombatFaction faction) {
        ParentFaction = faction;
        DataRef = monster;
        RuntimeHP = monster.HP;
        RuntimeMaxHP = monster.HP;
    }
    
    // 怪物行动不再从 Attack() 写死入口进入。
    // 敌方回合由 MonsterActionRunner 根据 DataRef.AI.Actions 选择并执行 Action。
}
```

## 3. 手动回合制循环机 (Turn-Based Combat System)

系统按照**阵营**来流转回合。这样即使后期加入“人偶的无人机僚机”或者“3个怪物”，系统依然稳如泰山。

```csharp
public class CombatSystem : MonoBehaviour {
    public CombatFaction PlayerFaction;
    public CombatFaction EnemyFaction;
    
    public enum CombatState { PlayerTurn, EnemyTurn, End }
    public CombatState CurrentState;

    public void StartCombat(List<string> monsterIDs) {
        // 1. 初始化玩家阵营
        PlayerFaction = new CombatFaction { Type = CombatFaction.FactionType.Player };
        PlayerFaction.Fighters.Add(new DollFighter(GameManager.Instance.CurrentPlayer.ActiveDoll, PlayerFaction));
        // 未来可以这里加召唤物：PlayerFaction.Fighters.Add(new DroneFighter(...));
        
        // 2. 初始化敌人阵营
        EnemyFaction = new CombatFaction { Type = CombatFaction.FactionType.Enemy };
        foreach(var id in monsterIDs) {
            EnemyFaction.Fighters.Add(new MonsterFighter(ConfigManager.GetMonster(id), EnemyFaction));
        }
        
        // 3. 游戏开始
        StartPlayerTurn();
    }
    
    public void StartPlayerTurn() {
        CurrentState = CombatState.PlayerTurn;
        PlayerFaction.OnTurnStart();
        
        // 恢复可用 AP 点数
        // 解锁 UI，等待玩家拖拽物品或手动点击武器
    }
    
    // UI 按钮绑定的事件：玩家操作结束
    public void EndPlayerTurn() {
        if(CurrentState != CombatState.PlayerTurn) return;
        PlayerFaction.OnTurnEnd();
        
        // 检测敌人是否已经全灭（如果被反伤打死等）
        if (EnemyFaction.IsWipedOut()) { HandleVictory(); return; }
        
        StartEnemyTurn();
    }
    
    public void StartEnemyTurn() {
        CurrentState = CombatState.EnemyTurn;
        EnemyFaction.OnTurnStart();
        
        // 怪物依次执行数据驱动行动
        foreach(var enemy in EnemyFaction.Fighters) {
            if(enemy.RuntimeHP > 0 && !PlayerFaction.IsWipedOut()) {
                MonsterActionRunner.ExecuteTurn(enemy, context);
            }
        }
        
        EnemyFaction.OnTurnEnd();
        
        if(!PlayerFaction.IsWipedOut()) {
            StartPlayerTurn();
        } else {
            HandleDefeat();
        }
    }
    
private void HandleVictory() {
        CurrentState = CombatState.End;
        // 结算掉落、同步血量
        ((DollFighter)PlayerFaction.Fighters[0]).SyncDataBack();
    }
    
    private void HandleDefeat() {
        CurrentState = CombatState.End;
        // 处理战败惩罚
    }
}
```

## 4. 战斗胜利奖励与 RewardSystem

战斗胜利后的奖励不应由 `CombatNode` 直接维护权重随机。`CombatNode` 的职责是“根据当前战斗来源请求奖励，并把奖励交给拾取界面”，具体保底、权重、空掉落、组合奖励由 `RewardSystem` 负责。

### 4.1 调用关系

```text
CombatSystem.HandleVictory()
        |
        v
CombatNode.ResolveAfterVictory()
        |
        v
RewardSystem.Roll(monster.RewardID, context)
        |
        v
CombatLootPickupResult.OfferedItems
        |
        v
CombatLootUIController 手动拾取
```

### 4.2 CombatNode 规则

*   每个怪物通过 `MonsterEntity.RewardID` 指向奖励表。
*   同一战斗节点有多个怪物时，逐个解析怪物奖励并合并。
*   节点自身也可以有 `RewardID`，用于宝箱、事件、关底额外奖励等。
*   MVP 迁移期如果怪物没有 `RewardID`，允许 fallback 到旧 `LootPool`，但新配置不得继续依赖旧字段。
*   `CombatNode` 不负责判断“保底掉落”或“权重掉落”的细节。

### 4.3 结果归属

战斗胜利奖励仍然先进入战利品拾取面板，不直接塞入背包。玩家手动拖入背包后，`CombatNode.ConfirmLootCollection()` 根据物品是否仍在背包中区分 accepted / discarded。

这保证 RewardSystem 只负责“生成奖励”，不直接决定玩家是否真的带走奖励。

### 4.4 详细文档

奖励表结构、运行时对象、随机测试和配置迁移见 [`10_奖励与掉落系统(RewardSystem).md`](./10_奖励与掉落系统(RewardSystem).md)。

## 5. 怪物 AI 与行动系统

敌方回合不应长期由 `CombatSystem` 写死“逐个怪物普通攻击第一个玩家目标”。酸液腐蚀、强塞诅咒物、召唤、蓄力、偷 AP、污染格子等行为应统一抽象为 `MonsterAction`。

推荐方向：

```text
CombatSystem.StartEnemyTurn()
        |
        v
MonsterActionRunner.ExecuteTurn(monster, context)
        |
        v
IMonsterActionSelector.SelectAction(context)
        |
        v
MonsterAction.Execute(context)
```

第一阶段采用轻量的权重行动列表，不直接实现完整行为树；但 `Selector` 需要通过接口隔离，未来可以替换成行为树 Selector。

详细数据结构、运行时类、目标选择、RuntimeModifier、`ReduceWeaponDamage` 与 `AddCursedItem` 迁移方案见 [`11_怪物AI与行动系统(MonsterActionAI).md`](./11_怪物AI与行动系统(MonsterActionAI).md)。
