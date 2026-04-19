# 深渊与战斗循环系统 (Dungeon & Combat System)

> **定位：** 指导程序实现局内的爬塔结构与手动回合制战斗。
> **重构亮点：** 
> 1. 深渊地图采用类似《杀戮尖塔》的预生成树状图节点（NodeBase）。
> 2. 战斗采用 `FighterEntity` 包装层，区分数据存储与战斗运行状态。
> 3. 战斗循环改为纯正的手动回合制，由玩家控制回合结束。

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
    public string MonsterID;
    public override void OnEnterNode() {
        GameManager.Instance.EnterCombatScene(MonsterID);
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

## 2. 战斗包装器 (FighterEntity)

战斗发生时，决不能直接在原生的 `DollEntity` 或 `MonsterEntity` 上写乱七八糟的战斗逻辑。需要一层只存活在战斗场景的 Wrapper。

```csharp
// 战斗者基类
public abstract class FighterEntity {
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
    
    public DollFighter(DollEntity doll) {
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
    
    public MonsterFighter(MonsterEntity monster) {
        DataRef = monster;
        RuntimeHP = monster.HP;
        RuntimeMaxHP = monster.HP;
    }
    
    public override void Attack(FighterEntity target, ItemEntity weaponSource = null) {
        // 执行怪物的行为树/AI
        target.TakeDamage(DataRef.DamageValue);
    }
}
```

## 3. 手动回合制循环机 (Turn-Based Combat System)

废弃了跑条倒计时，改为由玩家主动点击“回合结束”。

```csharp
public class CombatSystem : MonoBehaviour {
    public DollFighter PlayerFighter;
    public MonsterFighter EnemyFighter;
    
    public enum CombatState { PlayerTurn, EnemyTurn, End }
    public CombatState CurrentState;

    public void StartCombat(string monsterID) {
        PlayerFighter = new DollFighter(GameManager.Instance.CurrentPlayer.ActiveDoll);
        EnemyFighter = new MonsterFighter(ConfigManager.GetMonster(monsterID));
        
        StartPlayerTurn();
    }
    
    public void StartPlayerTurn() {
        CurrentState = CombatState.PlayerTurn;
        PlayerFighter.OnTurnStart();
        // 恢复可用 AP 点数
        // 解锁 UI，等待玩家拖拽物品或手动点击武器
    }
    
    // UI 按钮绑定的事件：玩家操作结束
    public void EndPlayerTurn() {
        if(CurrentState != CombatState.PlayerTurn) return;
        PlayerFighter.OnTurnEnd();
        
        StartEnemyTurn();
    }
    
    public void StartEnemyTurn() {
        CurrentState = CombatState.EnemyTurn;
        EnemyFighter.OnTurnStart();
        
        // 怪物执行攻击
        EnemyFighter.Attack(PlayerFighter);
        
        EnemyFighter.OnTurnEnd();
        
        if(PlayerFighter.RuntimeHP > 0) {
            StartPlayerTurn();
        } else {
            HandleDefeat();
        }
    }
}
```