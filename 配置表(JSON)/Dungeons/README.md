# 深渊地图层级配置说明 (Dungeons Config)

> 位于本目录下的 JSON 文件定义了深渊宏观“环境层”的参数。
> 决定了该层的长度、走一步的代价，以及这里盘踞着什么样的怪物。

## 字段说明表

| 字段名 (Key) | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `LayerID` | int | 深渊层级的深度序号 | 1代表第一层(最浅)，数字越大越深越难 |
| `Name` | string | 该深渊层级的显示名称 | 如 "污染矿带" |
| `SANCostPerNode` | int | 移动税（理智流失） | **核心痛点：** 玩家每经过一个非安全区节点强制扣除的SAN值。深层此数值应急剧放大。 |
| `ExpectedNodeCount`| int | 预期生成的入口到Boss路径长度 | 包含关底 Boss，不包含 `EndNode` 终点房 |
| `NodePool` | array | 沿途节点的刷新池 | 采用权重随机系统 (Weighted Pool)，可生成不同类型的节点(战斗/安全区等) |
| `BossNode` | string | 关底守门人的怪物ID | 指向 `Monsters` 配置表中的精英或BossID |
| `EndNode` | object | 关底 Boss 之后的层终点节点 | 当前配置为 `{ "NodeType": "StairsNode" }` |

## 层终点与阶梯房

每层地图生成时会在 Boss 节点之后追加 `EndNode` 配置声明的节点。MVP 中该节点配置为 `StairsNode`，显示为“阶梯”。因此实际地图节点数量为：

```text
实际地图节点数 = ExpectedNodeCount + 1
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
