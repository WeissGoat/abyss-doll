---
id: dev_03_dungeon_combat
title: 深渊与战斗循环系统 (Dungeon & Combat System)
type: dev
role: 程序
domain: dungeon_combat
status: active
source_of_truth: true
related:
  - 开发文档/数据与实体定义/04_深渊与战斗实体.md
  - 开发文档/11_怪物AI与行动系统(MonsterActionAI).md
  - 开发文档/10_奖励与掉落系统(RewardSystem).md
  - 数值模型设计/02_战斗伤害与生存公式.md
  - 数值模型设计/03_深渊产出与掉落期望.md
  - 配置表(JSON)/Dungeons/README.md
  - 配置表(JSON)/Monsters/README.md
  - 配置表(JSON)/Rewards/README.md
  - 设计文档/GDD_02_深渊地图遍历与搜打撤抉择.md
  - 设计文档/GDD_01_背包战斗与局内网格机制.md
last_verified: 2026-05-25
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

当前正式基础结构为多行节点网络：

```text
入口行 / 路线行若干 -> Boss CombatNode -> StairsNode
```

`ExpectedNodeCount` 只作为“入口到 Boss 的路径长度摘要”，正式网络下建议等于 `RowCount + 1`。实际地图按钮数量由 `NodeRows` 决定：

```text
实际地图按钮数 = sum(路线行宽度) + 1 个 Boss + 1 个 EndNode
```

`RowCount` 表示 Boss 前路线行数，不包含 Boss 行和 `EndNode` 终点行。当 `EndNode` 配置为 `StairsNode` 时，程序会在 Boss 后追加该阶梯房。

配置示例：

```json
{
  "ExpectedNodeCount": 6,
  "MapProfileID": "layer_1_tutorial_branching",
  "MapSeed": 1001,
  "RowCount": 5,
  "MinWidth": 2,
  "MaxWidth": 3,
  "MinRouteCount": 2,
  "BossNode": "elite_scrap_guard",
  "EndNode": {
    "NodeType": "StairsNode"
  }
}
```

阶梯房的职责：

* 进入阶梯房：通过 `DungeonSafeZoneService.RestoreActiveDollToFull()` 免费恢复当前魔偶 HP / SAN 到上限，作为层间安全区节奏释放。
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
    public List<NodeBase> EntryNodes;
    public List<List<NodeBase>> NodeRows;
    public int RunSeed;
    public string MapProfileID;
    
    public void GenerateMapTree(DungeonConfig config, int runSeed) {
        // 根据 RowCount / Width / seed 生成多行节点网络，并在 Boss 后追加 DungeonConfig.EndNode
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
        // 验证 targetNode 是否为入口节点，或属于 CurrentNode.NextNodes
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

### 1.4 非战斗结果节点

深渊路线不应只由战斗和安全区组成。当前程序侧已提供一组可配置的非战斗结果节点：

| 节点类型 | 运行时类 | 主要用途 |
|---|---|---|
| `TreasureNode` | `TreasureNode : DungeonOutcomeNode` | 宝箱、物资箱、路线收益节点。 |
| `EventNode` | `EventNode : DungeonOutcomeNode` | 异常事件、交易事件、轻量风险收益事件。 |
| `RestStopNode` | `RestStopNode : DungeonOutcomeNode` | 层内小休整，不等同于可撤离安全区。 |
| `HazardNode` | `HazardNode : DungeonOutcomeNode` | 风险房、污染房、资源损耗节点。 |

这些节点共用 `DungeonOutcomeNode`，由领域对象负责结算，不把规则写进 UI Controller。

配置字段：

```json
{
  "NodeType": "EventNode",
  "NodeIconID": "node_event_icon",
  "Title": "微光裂隙",
  "Description": "裂隙中传来微弱回响。",
  "RewardID": "reward_node_event_layer1",
  "OutcomeEffects": [
    {
      "Type": "ModifyResource",
      "Resource": "SAN",
      "Amount": -1
    }
  ],
  "Weight": 10
}
```

运行规则：

* `Title` / `Description` 只控制结算展示文本。
* `OutcomeEffects` 当前支持 `ModifyResource`，可修改 `HP`、`SAN`、`Money`。
* 资源变化在节点领域对象中执行，并通过 `GameEventBus` 刷新表现层。
* 如果节点配置 `RewardID`，节点调用 `RewardSystem.Roll()` 生成奖励。
* 奖励中的 `Money` 直接进入玩家金币；生成的物品进入战利品拾取面板。
* 玩家确认拾取后，节点根据物品是否仍在背包中发布 `CombatLootCollected`，再发布 `NodeSettlementCompleted`。
* 没有物品奖励的结果节点发布 `OnDungeonNodeResolutionPrepared`，由 `GameFlowController` 切到 `NodeResolution` 结果界面；玩家确认后发布 `NodeSettlementCompleted`。

事件流：

```text
DungeonManager.MoveToNode()
        |
        v
DungeonOutcomeNode.OnEnterNode()
        |
        +-- 有物品奖励 -> DungeonEventBus.OnCombatLootPrepared -> 战利品拾取 UI -> ConfirmLootCollection()
        |
        +-- 无物品奖励 -> DungeonEventBus.OnDungeonNodeResolutionPrepared -> 节点结果 UI -> NodeSettlementCompleted
```

验收：

* `DungeonNodeTypesSmokeTest.Run` 验证节点类型注册、宝箱奖励拾取流、结果节点资源变化、配置中节点类型覆盖。
* `DungeonStairsProgressionTest.Run` 验证新增节点不会破坏正式地图网络、阶梯进层、安全区恢复、战利品账本保留和地图点击路径。
* `ConfigValidationSmokeTest.Run` 验证 `OutcomeEffects`、`RewardID` 和节点配置引用。

### 1.5 地图迷雾与路线风险表达

深渊地图可见性和风险提示由 `DungeonMapVisibilityService` 统一计算，UI 只读取 presentation，不直接判断节点是否该隐藏、预览或显示风险。

领域规则：

* `DungeonConfig.FogProfile` 标记当前层的迷雾策略，用于配置审计、日志和后续多 profile 扩展。
* `DungeonConfig.NodeRevealDepth` 表示从当前位置向后完整揭示的行数，默认 `1`，允许显式配置为 `0`。
* `DungeonConfig.NodePreviewDepth` 表示完整揭示范围之后的预览行数，默认 `1`，允许显式配置为 `0`。
* 已访问节点、当前节点、入口节点和当前节点的后继节点始终为 `Revealed`。
* 预览节点只显示类型与风险等级，不承诺完整奖励或怪物信息。
* 超出预览范围的节点为 `Hidden`，地图 UI 使用“迷雾 / 未知”占位，不依赖真实美术资源。
* Boss 节点在当前基础实现中保持 `Preview`，避免玩家完全不知道路线终点。

路线风险：

* 节点可通过 `NodePoolEntry.RiskLevel` 显式配置风险等级：`Unknown`、`Safe`、`Low`、`Medium`、`High`、`Boss`。
* 节点可通过 `RiskHint` 配置简短提示，用于后续 tooltip 或详情面板。
* 未配置 `RiskLevel` 时，程序根据节点类型、怪物数量、Boss 标记和 `RouteTheme` 做基础推断。
* `RouteTheme=RiskReward` 或 `Attrition` 会提升非安全节点风险；`RouteTheme=Safe` 可降低普通战斗风险。

表现规则：

* `DungeonMapUIController` 调用 `DungeonMapVisibilityService.BuildNodePresentation()` 构建显示数据。
* Hidden 节点使用纯色占位和“迷雾 / 未知”文案。
* Preview 节点降低透明度，并在标签前增加“预览”。
* 路线连线只在起点和终点都不是 Hidden 时显示，避免提前泄露隐藏路线结构。

验收：

* `DungeonMapVisibilitySmokeTest.Run` 验证层配置迷雾字段、初始可见范围、移动后的可见范围和配置风险覆盖。
* `DungeonStairsProgressionTest.Run` 的地图布局检查按可渲染路线线段计数，避免战争迷雾导致测试误判。
* `ConfigValidationSmokeTest.Run` 验证 `FogProfile`、迷雾深度和 `RiskLevel` 合法性。

### 1.6 固定 seed 验收摘要

固定 seed 验收由 `DungeonSeedAcceptanceService` 生成稳定摘要，作为后续前三层正式配置和 P0 报告的可复用底座。该服务不改变运行时流程，只读取 `DungeonConfig` 和 `DungeonLayer.GenerateMapTree()` 的生成结果。

摘要至少包含：

* `SeedID`、`LayerID`、请求 seed、实际解析 seed 和 `MapProfileID`。
* 行数、入口数、节点数、连线数。
* Boss 节点、阶梯节点是否存在，以及是否从所有入口可达。
* 每一行节点的类型、节点 ID、路线主题、风险等级、奖励 ID、怪物 ID 和后继坐标。
* 失败原因列表，供 Validator、smoke test 或人工验收报告引用。

当前 smoke test：

* `DungeonSeedAcceptanceSmokeTest.Run` 验证 `V-L1-SEED-1024-ROUTE-01` 和 `V-L2-DIRECT-2048-ROUTE-01` 同 seed 摘要稳定、Boss / Stairs 可达。
* 缺失层配置会返回失败摘要和明确 issue，不应静默通过。

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

### 3.1 战斗结果报告

战斗胜负确定后，`CombatSystem` 负责生成只读结果报告，并在清理战斗运行态前发布给表现层或流程层消费。UI 不应自行判断胜负原因，也不应为了展示结果反查或修改战斗状态。

失败判定由 `CombatDefeatConditionService` 统一处理，当前正式入口包含：

* 当前出战人偶战斗 HP 归零。
* 玩家阵营全部失去战斗能力。
* 当前出战人偶 `SAN_Current <= 0`。

`CombatSystem`、`CombatOutcomeReportService` 和后续占位 / 正式 UI 必须共用该服务输出的 `CombatDefeatEvaluation`，避免流程判定、报告原因和表现文案各自维护一套逻辑。

当前契约：

```text
CombatDefeatConditionService.Evaluate()
        |
        v
CombatSystem.HandleVictory() / HandleDefeat()
        |
        v
CombatOutcomeReportService.BuildVictory() / BuildDefeat()
        |
        v
CombatSystem.LastOutcomeReport
        |
        v
CombatEventBus.OnCombatOutcomePrepared
```

`CombatOutcomeReport` 至少包含：

* `OutcomeType`：胜利 / 战败。
* `DefeatReason`：HP 归零、SAN 崩溃、HP 与 SAN 同时归零、玩家阵营全灭或未知。
* `Title` / `Summary`：供占位 UI 或正式 UI 直接展示的短文本。
* 玩家与敌方 `CombatOutcomeFighterSnapshot`：记录 HP、护盾、AP 和存活状态。
* 当前人偶 HP / SAN 快照：用于战败复盘和后续失败反馈。
* `TimelineEvents`：由 `CombatTimelineRecorder` 输出的只读战斗时间线，用于占位 UI 或正式 UI 展示战斗复盘。

`CombatTimelineRecorder` 属于战斗领域数据层，不依赖 UI。它在战斗开始、回合开始 / 结束、伤害结算、怪物物品干涉和胜负结算时记录结构化事件。事件只保存 UI 可消费快照，不持有战斗对象引用，避免表现层在战斗清理后反查运行态。

`CombatReadabilityTextService` 是当前占位 UI / 正式 UI 的文本适配层。它只消费 `MonsterIntentPreviewService` 和 `CombatOutcomeReport`，输出怪物意图、战斗者状态和最近战斗记录的只读文本快照；不得在该服务中修改战斗状态、背包状态、掉落归属或美术资源绑定。

验收：

* `CombatOutcomeReportSmokeTest.Run` 覆盖胜利报告、HP 战败报告、SAN 崩溃战败报告、事件派发、`TimelineEvents` 和 `LastOutcomeReport` 快照一致性。
* `CombatReadabilityTextServiceSmokeTest.Run` 覆盖怪物意图文本、胜利结果文本、战败结果文本和最近时间线裁剪输出。
* 报告生成必须发生在 `PlayerFaction.Cleanup()` / `EnemyFaction.Cleanup()` 之前，避免清理监听或运行态后丢失复盘数据。

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
