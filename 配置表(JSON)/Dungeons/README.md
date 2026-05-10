# 深渊地图层级配置说明 (Dungeons Config)

> 位于本目录下的 JSON 文件定义了深渊宏观“环境层”的参数。
> 决定了该层的长度、走一步的代价，以及这里盘踞着什么样的怪物。

## 字段说明表

| 字段名 (Key) | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `LayerID` | int | 深渊层级的深度序号 | 1代表第一层(最浅)，数字越大越深越难 |
| `Name` | string | 该深渊层级的显示名称 | 如 "污染矿带" |
| `SANCostPerNode` | int | 移动税（理智流失） | **核心痛点：** 玩家每经过一个非安全区节点强制扣除的SAN值。深层此数值应急剧放大。 |
| `ExpectedNodeCount`| int | 预期生成的节点总长度 | 决定了从入口走到Boss的平均步数 |
| `NodePool` | array | 沿途节点的刷新池 | 采用权重随机系统 (Weighted Pool)，可生成不同类型的节点(战斗/安全区等) |
| `BossNode` | string | 关底守门人的怪物ID | 指向 `Monsters` 配置表中的精英或BossID |

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
