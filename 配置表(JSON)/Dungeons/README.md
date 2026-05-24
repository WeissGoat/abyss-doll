---
id: config_dungeons_readme
title: 深渊地图层级配置说明 (Dungeons Config)
type: config
role: 策划
domain: config_dungeons
status: active
source_of_truth: true
related:
  - 开发文档/03_深渊与战斗循环(DungeonCombat).md
  - 开发文档/数据与实体定义/04_深渊与战斗实体.md
  - 开发文档/10_奖励与掉落系统(RewardSystem).md
  - 数值模型设计/02_战斗伤害与生存公式.md
  - 数值模型设计/03_深渊产出与掉落期望.md
  - 配置表(JSON)/README.md
  - 配置表(JSON)/Rewards/README.md
  - 设计文档/GDD_02_深渊地图遍历与搜打撤抉择.md
  - 设计文档/GDD_01_背包战斗与局内网格机制.md
last_verified: 2026-05-23
update_rule: 修改配置字段、数据源规则或表间引用时同步本文件。
---

# 深渊地图层级配置说明 (Dungeons Config)

> 位于本目录下的 JSON 文件定义了深渊宏观“环境层”的参数。
> 决定了该层的长度、走一步的代价，以及这里盘踞着什么样的怪物。

## 字段说明表

| 字段名 (Key) | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `LayerID` | int | 深渊层级的深度序号 | 1代表第一层(最浅)，数字越大越深越难 |
| `Name` | string | 该深渊层级的显示名称 | 如 "污染矿带" |
| `SANCostPerNode` | int | 移动税（理智流失） | **核心痛点：** 玩家每经过一个非安全区节点强制扣除的SAN值。深层此数值应急剧放大。 |
| `ExpectedNodeCount`| int | 入口到 Boss 的路径长度摘要 | 正式网络下建议等于 `RowCount + 1`，用于调试和节奏描述，不代表 UI 实际节点数 |
| `MapProfileID` | string | 地图生成画像 ID | 同一 `LayerID + RunSeed + MapProfileID` 应生成稳定节点网络 |
| `MapSeed` | int | 配置固定种子 | 用于固定回归；正式 run 可叠加运行时 seed |
| `RowCount` | int | Boss 前路线行数 | 不包含 Boss 行和 `EndNode` 终点行 |
| `MinWidth` / `MaxWidth` | int | 每行节点数量范围 | 当前正式基础生成器按行生成 2-4 个节点 |
| `MinRouteCount` | int | 入口行最低路线数 | 决定开局可选入口数量，必须 `<= MaxWidth` |
| `NodePool` | array | 沿途节点的刷新池 | 采用权重随机系统 (Weighted Pool)，可生成不同类型的节点(战斗/安全区等) |
| `BossNode` | string | 关底守门人的怪物ID | 指向 `Monsters` 配置表中的精英或BossID |
| `EndNode` | object | 关底 Boss 之后的层终点节点 | 当前配置为 `{ "NodeType": "StairsNode" }` |

## 层级解锁与出发入口

深渊起始层不由 dungeon JSON 单独配置，而由玩家档案中的 `HighestUnlockedDungeonLayer` 控制。

规则：

* 新档默认只能从 `LayerID = 1` 开始。
* 当玩家到达第 N 层 `EndNode/StairsNode` 时，如果存在 `LayerID = N + 1` 的 dungeon 配置，则解锁从下一层开始。
* 小镇出发界面只显示或启用 `LayerID <= HighestUnlockedDungeonLayer` 且配置存在的层。
* 从第 2 层或更深层直接出发不会自动发放前置层奖励。
* 直接从已解锁层出发属于新一轮探索；通过阶梯进入下一层仍属于同一轮探索。

配置要求：

* MVP 阶段建议保持 `LayerID` 连续，例如 `1, 2`。
* 如果未来允许跳号层级，需要在程序层补充“下一层映射”配置；当前不支持。
* UI 展示层级名称时读取 `Name`；锁定文案由程序根据上一层通关条件生成，不额外写入 dungeon JSON。
* 解锁入口依赖配置出来的终点节点。若未来新增非阶梯终点，只有明确实现“层通关解锁”语义的节点才应触发入口解锁。

## 正式地图网络与层终点

每层地图生成时先按 `RowCount` / `MinWidth` / `MaxWidth` / `MinRouteCount` 生成 Boss 前多行节点网络，再追加固定 Boss 行，最后追加 `EndNode` 配置声明的终点节点。

正式基础结构：

```text
入口行 / 路线行若干 -> Boss CombatNode -> EndNode
```

`ExpectedNodeCount` 不再表示实际地图按钮数量。正式网络下实际按钮数量由每行宽度决定：

```text
实际地图节点数 = sum(每个路线行宽度) + 1 个 Boss + 1 个 EndNode
```

阶梯房不是随机节点，当前应写在 `EndNode`。示例：

```json
"EndNode": {
  "NodeType": "StairsNode"
}
```

它提供两个选择：

1. 进入下一层：如果存在 `LayerID + 1` 的配置，则加载下一层，并保留本次探索已拾取战利品账本。
2. 返回小镇：触发撤离结算，统计玩家最终仍带在背包里的本次战利品。

如果已经没有下一层配置，阶梯房会显示为“深渊尽头”，只能返回小镇。

## 地图生成字段示例

```json
{
  "ExpectedNodeCount": 6,
  "MapProfileID": "layer_1_tutorial_branching",
  "MapSeed": 1001,
  "RowCount": 5,
  "MinWidth": 2,
  "MaxWidth": 3,
  "MinRouteCount": 2
}
```

当前程序侧基础保证：

* 同一 `LayerID + RunSeed + MapProfileID` 的节点类型、节点 ID 和连线可复现。
* 每个非终点节点至少连向下一行 1 个节点。
* 每个非入口节点至少有上一行连入。
* Boss 行固定 1 个 `CombatNode`，读取 `BossNode` 和 `BossNodeIconID`。
* EndNode 行固定 1 个配置节点，当前前两层为 `StairsNode`。
* UI 根据生成后的 `NodeRows` 和 `NextNodes` 展示多路线网络；只允许点击入口节点或当前节点的后继节点。

## NodePool (节点刷新池对象) 内部字段

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `NodeType` | string | 欲刷新的节点类型 | `CombatNode`, `SafeRoomNode` 等 |
| `MonsterIDs` | array | (如果是战斗节点)怪物的ID列表 | 支持配置多个ID生成群殴节点 |
| `RewardID` | string | 节点自身额外奖励表 ID | 可选，指向 `/Rewards`；与怪物奖励并存 |
| `Weight` | int | 随机抽取的权重值 | 权重越高，该节点在路径中出现的概率越大 |

## 节点奖励说明

战斗节点的常规战利品应优先来自怪物 `RewardID`。如果节点本身还需要额外奖励，例如宝箱、事件补偿、关卡奖励，可在 `NodePool` 条目上额外配置 `RewardID`。

解析顺序建议：

1.  逐个解析 `MonsterIDs` 对应怪物的 `RewardID`。
2.  若节点条目自身配置了 `RewardID`，再解析节点奖励。
3.  合并后进入同一个战利品拾取面板。
