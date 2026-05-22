---
id: dev_03_dungeon_combat
title: 深渊与战斗循环系统 (Dungeon & Combat System)
type: dev
role: 程序
domain: dungeon_combat
status: active
source_of_truth: true
related:
  - AGENTS.md
  - PROJECT_STATUS.md
last_verified: 2026-05-23
update_rule: 修改对应程序架构、接口契约、验证流程或 Unity 实现边界时同步本文件。
---

# 深渊与战斗循环系统 (Dungeon & Combat System)

> **定位：** 指导程序实现局内的爬塔结构与手动回合制战斗。
> **重构亮点：** 
> 1. 深渊地图采用类似《杀戮尖塔》的预生成树状图节点（NodeBase）。
> 2. 战斗采用 `FighterEntity` 包装层，区分数据存储与战斗运行状态。
> 3. **新增：** 引入 `CombatFaction`（阵营）概念，支持多对多战斗。
> 4. 战斗循环改为纯正的手动回合制，由玩家控制回合结束。

## 1. 深渊地图生成与管理 (Dungeon Tree)

深渊的路线是提前生成的，玩家一目了然。我们需要抽象出 `Layer` 和 `NodeBase`。

### 1.1 层终点：阶梯房

每一层的最后一个可进入节点应配置为 `StairsNode`（显示名：阶梯）。它不是 `NodePool` 随机出来的普通节点，而是由 `DungeonConfig.EndNode` 显式声明，程序只负责按配置在关底 Boss 节点之后实例化该节点。

当前 MVP 的线性结构为：

```text
沿途随机节点 ... -> Boss CombatNode -> StairsNode
```

`ExpectedNodeCount` 仍表示“从入口到 Boss 的路径长度，包含 Boss”。当 `EndNode` 配置为 `StairsNode` 时，实际地图按钮数量会是 `ExpectedNodeCount + 1`，多出来的 1 个就是配置声明的阶梯房。

配置示例：

```json
{
  "ExpectedNodeCount": 3,
  "BossNode": "elite_scrap_guard",
  "EndNode": {
    "NodeType": "StairsNode"
  }
}
```

阶梯房的职责：

* 进入下一层：调用 `DungeonManager.EnterNextLayer()`，加载 `CurrentLayer.LayerID + 1`。
* 返回小镇：发布 `DungeonEventBus.PublishDungeonEvacuated()`，走现有撤离结算。
* 无下一层时：进入下一层按钮不可用，仅允许返回小镇结算。

结算归属调整：

* Boss 战胜利后只完成战斗节点自身结算，并通过 `OnNodeSettlementCompleted` 通知外界。
* `DungeonManager` 不再因为打完 Boss 自动撤离；如果 Boss 后存在配置出来的 `EndNode`，则发布 `OnNodeResolutionFinished` 回到地图等待玩家选择阶梯。
* 仅当某个终端节点没有后继节点时，`DungeonManager` 才保留自动撤离 fallback，用于异常配置兜底。
* 进入下一层不会清空本次探索已拾取战利品账本，玩家跨层后返回小镇仍能正确统计本次带出/遗失战利品。
* 到达第 N 层阶梯时，如果存在第 `N + 1` 层配置，应解锁“从下一层开始下潜”的局外入口。该解锁属于 `PlayerProfile` 永久进度，不属于当前 run 临时状态。

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

public class StairsNode : NodeBase {
    public override void OnEnterNode() {
        // 打开阶梯 UI：进入下一层 / 返回小镇
    }
}

// 楼层控制器
public class DungeonLayer {
    public int LayerID;
    public NodeBase RootNode;
    public NodeBase CurrentNode;
    
    public void GenerateMapTree() {
        // 根据配置表生成分支树结构，并在 Boss 后追加 DungeonConfig.EndNode
    }
}

// 深渊总控
public class DungeonManager : MonoBehaviour {
    public DungeonLayer CurrentLayer;
    private PlayerProfile _player;

    public bool CanStartAtLayer(int layerID) {
        return _player != null
            && layerID >= 1
            && layerID <= _player.HighestUnlockedDungeonLayer
            && ConfigManager.Dungeons.ContainsKey(layerID);
    }

    public bool StartRunAtLayer(int layerID) {
        if(!CanStartAtLayer(layerID)) return false;
        _player.LastSelectedDungeonStartLayer = layerID;
        ResetRunLootLedger();
        LoadLayer(layerID);
        return true;
    }

    public bool EnterNextLayer() {
        int nextLayerID = CurrentLayer.LayerID + 1;
        if(!ConfigManager.Dungeons.ContainsKey(nextLayerID)) return false;
        LoadLayer(nextLayerID, resetRunLootLedger: false);
        return true;
    }
    
    public void MoveToNode(NodeBase targetNode) {
        // 验证 targetNode 是否属于 CurrentNode.NextNodes
        CurrentLayer.CurrentNode = targetNode;
        // 结算 SAN 值移动税：节点基础消耗 + 背包物品 Effect 额外消耗
        int sanCost = GetBaseSanCostForNode(targetNode) + GetExtraSanCostFromBackpackEffects(targetNode);
        DungeonEventBus.PublishNodeEntered(targetNode, sanCost);
        
        targetNode.OnEnterNode();
    }
}
```

### 1.2 层入口解锁与指定层出发

MVP 新增“可选起始层”规则：玩家第一次只能从第 1 层出发；通过第 1 层后，小镇出发界面允许直接选择第 2 层作为新的起点。

领域规则：

* `PlayerProfile.HighestUnlockedDungeonLayer` 初始为 `1`。
* `DungeonManager` 或更高一层的 `CoreBackend` 提供 `UnlockDungeonStartLayer(int layerID)`，只在 `layerID` 存在配置时生效。
* 解锁触发点建议放在进入 `StairsNode` 时：这代表玩家已经打完该层 Boss，并抵达层终点。
* 如果当前层是最后一层，或者 `ConfigManager.Dungeons` 中不存在下一层，则不提升解锁，只在阶梯 UI 中显示“深渊尽头/返回小镇”。
* 从已解锁层直接出发时调用 `StartRunAtLayer(layerID)`，必须重置本次 run 战利品账本，避免继承上一轮探索统计。
* 跨层深入仍走 `EnterNextLayer()`，不重置本次 run 战利品账本，保持“同一轮探索”的结算统计。
* `StartRunAtLayer(layerID)` 应返回 `bool`，UI 只能根据成功/失败展示反馈，不能自行 fallback 到第 1 层。
* 解锁成功时发布 `OnDungeonStartLayerUnlockedEvent(layerID)`；如果已经解锁过，则不重复发布。

推荐 API：

```csharp
public bool TryUnlockNextStartLayerFromClearedLayer(int clearedLayerID) {
    int nextLayerID = clearedLayerID + 1;
    if (!ConfigManager.Dungeons.ContainsKey(nextLayerID)) {
        return false;
    }

    if (_player.HighestUnlockedDungeonLayer < nextLayerID) {
        _player.HighestUnlockedDungeonLayer = nextLayerID;
        return true;
    }

    return false;
}
```

验收口径：

* 新档只显示第 1 层可选。
* 到达 1 层阶梯后，`HighestUnlockedDungeonLayer` 变为 `2`。
* 返回小镇后再次点击“出发深渊”，层级选择界面显示第 1 层和第 2 层。
* 选择第 2 层会直接 `LoadLayer(2)`，地图根节点属于 2 层配置。
* 未解锁层或不存在配置的层不能被 UI 或测试绕过进入。
* 从小镇直接选择第 2 层时，本轮战利品账本为空；从 1 层阶梯继续进入 2 层时，本轮战利品账本保留。

### 1.3 节点移动 SAN 消耗与物品效果

`DungeonManager` 负责计算进入节点时的总 SAN 消耗，但不应硬编码某个物品标签的特殊规则。当前规则为：

```text
TotalSanCost = EffectModifierResolver.Resolve(
    Trigger = OnDungeonMoveCost,
    Resource = SAN,
    BaseValue = BaseNodeSanCost
)
```

落地约定：

* `SafeRoomNode`、`StairsNode` 的基础消耗仍为 0。
* 背包物品与已装备义体都可以通过 `EffectModifierResolver` 参与成本修正。
* 需要修改移动 SAN 成本时，配置 `ModifyResourceCost`，并声明 `Trigger = OnDungeonMoveCost`、`Resource = SAN`、`Operation = AddFlat`。
* `Toxic` 标签只作为分类、展示、条件筛选信息，不直接触发扣 SAN。
* 【污染滤芯】MVP 配置为 `ModifyResourceCost.Params = [1]`，表示每进入一个节点额外消耗 1 SAN。

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
