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
  - 设计文档/28_Monsters正式配置承接审计.md
  - 设计文档/29_Dungeons正式配置承接审计.md
  - 设计文档/30_Rewards正式配置承接审计.md
  - 设计文档/31_第一层正式配置落地设计.md
  - 设计文档/32_第二层正式配置落地设计.md
  - 设计文档/33_第三层正式配置落地设计.md
  - 设计文档/48_经济压力传闻正式配置落地设计.md
  - 设计文档/50_经济压力订单正式配置落地设计.md
  - 设计文档/52_经济压力正式配置实现任务拆分.md
  - 设计文档/GDD_01_背包战斗与局内网格机制.md
last_verified: 2026-05-25
update_rule: 修改配置字段、数据源规则或表间引用时同步本文件。
---

# 深渊地图层级配置说明 (Dungeons Config)

> 位于本目录下的 JSON 文件定义了深渊宏观“环境层”的参数。
> 决定了该层的长度、走一步的代价，以及这里盘踞着什么样的怪物。

正式配置字段、前三层地图画像缺口、NodePool / BossNode / SafeZone 语义差异和 Validator 建议见 `设计文档/29_Dungeons正式配置承接审计.md`。该审计是配置改造依据，不代表本文字段或 `layer_*.json` 已经全部落地。

## 正式配置基本规则

`Dungeons` 是深渊层级、地图画像、路线网络和节点内容池的静态配置源。正式层级配置必须回答六个问题：

1. 这一层承担什么主题、节奏和内容包职责。
2. 同层有哪些 MapProfile，分别对应什么路线主题和必出内容。
3. 节点网络如何保证有效路线、精英可绕、Boss 前缓冲和节点类型覆盖。
4. 普通遭遇、精英、Boss、宝箱、事件、危险和订单目标分别从哪些池抽取。
5. Boss 后安全区如何处理免费恢复、撤离、继续深入和探索账本保留。
6. 固定 seed 生成结果能否被 Validator 或自动化试玩复现。

运行时地图实例不得写入静态 dungeon JSON。已生成的 `NodeRows`、`Edges`、节点实例 ID、玩家当前位置、已访问节点、可见迷雾、本轮抽到的具体奖励和撤离 / 深入选择只属于运行时探索状态、Run 账本或验收报告。

当前 `layer_1.json` / `layer_2.json` 是可运行基线，不代表正式字段已完成。后续 JSON 补齐前，先以本文字段口径和 `29_Dungeons正式配置承接审计.md` 为准。

## 字段说明表

| 字段名 (Key) | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `LayerID` | int | 深渊层级的深度序号 | 1代表第一层(最浅)，数字越大越深越难 |
| `Name` | string | 该深渊层级的显示名称 | 如 "污染矿带" |
| `LayerKey` | string | 层级内部键 | 建议如 `layer_1_shallow_corridor`，用于日志、seed 摘要和文案映射 |
| `DisplayNameKey` | string | 文案表键 | 正式配置建议必填；临时层可沿用 `Name` |
| `LayerThemeTags` | array<string> | 层级主题标签 | 例如 `Tutorial`, `BagPressure`, `RouteCorrosion`, `OrderConflict` |
| `ContentPackRefs` | array<string> | 内容包归属 | 例如 `ContentPack19`, `ContentPack20`, `ContentPack25` |
| `SANCostPerNode` | int | 移动税（理智流失） | **核心痛点：** 玩家每经过一个非安全区节点强制扣除的SAN值。深层此数值应急剧放大。 |
| `ExpectedNodeCount`| int | 入口到 Boss 的路径长度摘要 | 正式网络下建议等于 `RowCount + 1`，用于调试和节奏描述，不代表 UI 实际节点数 |
| `MapProfileID` | string | 地图生成画像 ID | 同一 `LayerID + RunSeed + MapProfileID` 应生成稳定节点网络 |
| `MapProfiles` | array<object> | 同层可选地图画像列表 | 正式字段；兼容旧 `MapProfileID` 单画像写法 |
| `RouteThemeWeights` | object | 路线主题权重 | 例如 Safe / RiskReward / Attrition / OrderTrack |
| `NodeTypeWeights` | object | 节点类型权重 | 用于控制 Combat / Treasure / Event / Elite / Hazard 等比例 |
| `RequiredNodes` | array<object> | 必出节点或必出节点组 | 用于 Boss、可绕精英、关键事件、订单目标和教学节点 |
| `Constraints` | object | 路线生成约束 | 详见 `Constraints` 字段组 |
| `EncounterPools` | object | 普通 / 精英 / Boss 遭遇池 | 详见 `EncounterPools` 字段组 |
| `TreasureRewardPoolRefs` | array<string> | 宝箱奖励池引用 | 指向 Rewards |
| `EventPoolRefs` | array<string> | 事件池引用 | 指向事件 / 节点模板配置；当前可先预留 |
| `HazardPoolRefs` | array<string> | 危险节点池引用 | 指向 Hazard 或节点模板配置；当前可先预留 |
| `NodeRewardRefs` | array<string> | 节点奖励引用集合 | 便于 Validator 检查节点奖励可解析 |
| `PostBossNode` | object | Boss 后节点语义 | 正式建议使用 `SafeZoneNode` 或带 SafeZone 语义的 `StairsNode` |
| `SafeZoneRules` | object | 安全区恢复、撤离和深入规则 | 详见 `SafeZoneRules` 字段组 |
| `UnlockRule` | object | 层级解锁规则 | 支撑已通层直达 |
| `DirectStartAllowed` | bool | 已解锁后是否允许小镇直达 | 默认已解锁层允许 |
| `PreviousLayerRequired` | int | 首次解锁需要通关的前置层 | 例如第 2 层为 1 |
| `NextLayerID` | int | 阶梯深入目标层 | 默认 `LayerID + 1` |
| `FixedSeedSamples` | array<object> | 固定 seed 验收样例 | 详见 `FixedSeedSamples` 字段组 |
| `ExpectedRouteSummary` | object | 预期路线摘要 | 用于验收有效路线数、精英可绕、节点覆盖 |
| `ValidationScenarioRefs` | array<string> | 关联验收场景 | 指向 `15`、`19/20/25` 的场景编号 |
| `MapBackgroundID` | string | 地图背景 VisualID | 地图 UI / 美术验收引用 |
| `NodeIconSet` | string | 节点图标集合 | 节点类型到图标的映射 |
| `RouteThemeVisuals` | object | 路线主题表现引用 | 例如安全路线、侵蚀路线、订单路线颜色 / 图标 |
| `FogProfile` | string | 战争迷雾配置 | 支撑未知节点、预览范围和可见信息 |
| `NodeRevealDepth` | int | 完整揭示行数 | 从当前位置向后完整显示多少行，允许 `0`，缺省为 `1` |
| `NodePreviewDepth` | int | 预览行数 | 完整揭示范围之后预览多少行，允许 `0`，缺省为 `1` |
| `MapSeed` | int | 配置固定种子 | 用于固定回归；正式 run 可叠加运行时 seed |
| `RowCount` | int | Boss 前路线行数 | 不包含 Boss 行和 `EndNode` 终点行 |
| `MinWidth` / `MaxWidth` | int | 每行节点数量范围 | 当前正式基础生成器按行生成 2-4 个节点 |
| `MinRouteCount` | int | 入口行最低路线数 | 决定开局可选入口数量，必须 `<= MaxWidth` |
| `NodePool` | array | 沿途节点的刷新池 | 采用权重随机系统 (Weighted Pool)，可生成不同类型的节点(战斗/安全区等) |
| `BossNode` | string | 关底守门人的怪物ID | 指向 `Monsters` 配置表中的精英或BossID |
| `EndNode` | object | 关底 Boss 之后的层终点节点 | 当前配置为 `{ "NodeType": "StairsNode" }` |

## MapProfiles (地图画像字段组)

正式版同一层可以有多个地图画像，避免层级只靠单个 `MapProfileID` 表达。

| 字段名 | 数据类型 | 说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `ProfileID` | string | 地图画像 ID | 例如 `layer1_tutorial_branching` |
| `ProfileRole` | enum | 画像职责 | `Tutorial`, `SafeRoute`, `RiskReward`, `BagPressure`, `OrderTrack`, `RouteAttrition`, `ContestedOrder` |
| `RouteThemeWeights` | object | 当前画像路线主题权重 | 覆盖层级默认权重 |
| `RequiredNodes` | array<object> | 当前画像必出节点 | 可要求第几行、节点类型、怪物或奖励 |
| `ValidationSeed` | int | 推荐验收 seed | 用于固定回归 |

## Constraints (路线约束字段组)

| 字段名 | 数据类型 | 说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `MinCombatPerRoute` | int | 每条有效路线最少战斗节点 | 第一层可低，第二层后提高 |
| `MaxSameTypeStreak` | int | 同类节点最大连续数量 | 防止全战斗或全安全路线 |
| `EliteBypassRequired` | bool | 精英是否必须可绕 | 第一 / 二层正式配置建议为 `true` |
| `BossBufferRequired` | bool | Boss 前是否必须有缓冲节点 | 正式配置建议为 `true` |
| `MinEffectiveRouteCount` | int | 最少有效路线数 | 用于固定 seed 摘要校验 |
| `MaxRestStopPerRoute` | int | 单路线最多休整点 | 防止安全区过密 |

## EncounterPools (遭遇池字段组)

| 字段名 | 数据类型 | 说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `Minor` | array<object> | 普通战斗遭遇池 | 元素可包含 `MonsterIDs`、`EncounterTags`、`Weight` |
| `Elite` | array<object> | 精英遭遇池 | 应与普通池分开，便于可绕和风险校验 |
| `BossEncounterID` | string | Boss 怪物 ID | 正式建议指向 `boss_*`，旧 `BossNode` 可兼容 |
| `EncounterTags` | array<string> | 遭遇主题标签 | 例如 `BagPressure`, `SanPressure`, `Corrosion`, `OrderTarget` |

## SafeZoneRules (安全区字段组)

Boss 后安全区按当前设计口径为免费全恢复，但不清除探索账本、物品污染、订单状态或剧情旗标。

| 字段名 | 数据类型 | 说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `FreeRecoverHP` | bool | 是否免费恢复 HP | Boss 后安全区为 `true` |
| `FreeRecoverSAN` | bool | 是否免费恢复 SAN | Boss 后安全区为 `true` |
| `ClearItemPollution` | bool | 是否清除物品污染 | 默认 `false` |
| `ClearOrderState` | bool | 是否清除订单状态 | 必须为 `false` |
| `AllowExtract` | bool | 是否允许撤离回城 | Boss 后安全区为 `true` |
| `AllowDescend` | bool | 是否允许继续深入 | 下一层配置存在时为 `true` |
| `PreserveRunLedger` | bool | 深入下一层时是否保留本轮账本 | 必须为 `true` |

## FixedSeedSamples (固定 seed 验收字段组)

| 字段名 | 数据类型 | 说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `RunSeed` | int | 固定 seed | 与 `LayerID + MapProfileID` 共同复现地图 |
| `ExpectedEffectiveRouteCount` | int | 预期有效路线数 | Validator 可对比生成结果 |
| `ExpectedNodeTypeCoverage` | array<string> | 预期节点类型覆盖 | 例如 `CombatNode`, `TreasureNode`, `EventNode`, `EliteNode`, `SafeZoneNode` |
| `ExpectedBossID` | string | 预期 Boss | 必须和 BossEncounter / BossNode 一致 |
| `Notes` | string | 人工验收说明 | 简短记录该 seed 用途 |

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

正式版中，Boss 后节点应具备安全区语义。当前可由 `EndNode.NodeType=StairsNode` 兼容承载，但 README 和 Validator 需要按 `SafeZoneRules` 判断它是否满足：

* 免费恢复 HP。
* 免费恢复 SAN。
* 允许撤离回城。
* 若下一层存在，允许继续深入。
* 保留物品污染、订单状态、剧情旗标和本轮探索账本。

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
* UI 根据 `FogProfile`、`NodeRevealDepth`、`NodePreviewDepth` 和运行时当前位置展示 `Revealed` / `Preview` / `Hidden` 三类节点。
* Hidden 节点用“迷雾 / 未知”占位，Preview 节点只表达类型与风险，不泄露完整路线信息。
* 路线连线只在起点和终点均不是 Hidden 时渲染。
* `RiskLevel` 可由配置显式覆盖；未配置时程序按节点类型、怪物数量、Boss 标记和路线主题推断。
* `NodePool` 已支持 `CombatNode`、`SafeRoomNode`、`TreasureNode`、`EventNode`、`RestStopNode`、`HazardNode`。
* 非战斗结果节点通过 `DungeonOutcomeNode` 统一处理标题、描述、资源变化、奖励表掉落和节点结果事件。

## NodePool (节点刷新池对象) 内部字段

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `NodeType` | string | 欲刷新的节点类型 | `CombatNode`, `SafeRoomNode` 等 |
| `NodeIconID` | string | 地图节点图标 VisualID | 可选；未配置时由 UI 使用占位 / 默认表现 |
| `Title` | string | 节点结果标题 | `TreasureNode` / `EventNode` / `RestStopNode` / `HazardNode` 可配置 |
| `Description` | string | 节点结果描述 | 进入节点结算界面时展示 |
| `RiskLevel` | string | 节点风险等级 | 可选；合法值 `Unknown`、`Safe`、`Low`、`Medium`、`High`、`Boss` |
| `RiskHint` | string | 节点风险提示 | 可选；用于后续 tooltip / 详情面板 |
| `MonsterIDs` | array | (如果是战斗节点)怪物的ID列表 | 支持配置多个ID生成群殴节点 |
| `RewardID` | string | 节点自身额外奖励表 ID | 可选，指向 `/Rewards`；与怪物奖励并存 |
| `OutcomeEffects` | array | 节点结算效果列表 | 当前支持 `ModifyResource` 修改 `HP` / `SAN` / `Money` |
| `Weight` | int | 随机抽取的权重值 | 权重越高，该节点在路径中出现的概率越大 |

## 已支持节点类型

| 节点类型 | 用途 | 主要配置 |
| :--- | :--- | :--- |
| `CombatNode` | 战斗节点 | `MonsterIDs`，可选 `RewardID` |
| `SafeRoomNode` | 安全区 / 小休整 | 当前走安全区 UI，不使用 `OutcomeEffects` |
| `StairsNode` | 层终点 / 深入或返回小镇 | 应配置在 `EndNode` |
| `TreasureNode` | 宝箱 / 物资节点 | `Title`、`Description`、`RewardID` |
| `EventNode` | 事件节点 | `Title`、`Description`、`RewardID`、`OutcomeEffects` |
| `RestStopNode` | 层内营地 | `Title`、`Description`、`OutcomeEffects` |
| `HazardNode` | 风险 / 损耗节点 | `Title`、`Description`、`OutcomeEffects` |
| `OrderTargetNode` | 订单目标节点 | 正式预留，至少需要 `OrderRefs`、`Title`、`Description` |
| `RouteCorrosionNode` | 路线侵蚀节点 | 第三层预留，至少需要 `RouteCorrosion` 或 `OutcomeEffects` |
| `SafeZoneNode` | Boss 后安全区 | 正式推荐终点语义，可兼容当前 `StairsNode` |

`TreasureNode`、`EventNode`、`RestStopNode`、`HazardNode` 共用 `DungeonOutcomeNode`，它们不会在 UI 里直接修改状态，而是由节点领域对象结算后发布事件给表现层。

## OutcomeEffects 字段

当前支持的节点结果效果：

```json
{
  "Type": "ModifyResource",
  "Resource": "SAN",
  "Amount": -1
}
```

规则：

* `Type` 为空时默认按 `ModifyResource` 解析。
* `Resource` 当前只允许 `HP`、`SAN`、`Money`。
* `Amount` 可以为正数或负数；`0` 会被 Validator 警告。
* 资源变化由节点领域对象处理，并通过事件刷新 UI。

## 节点奖励说明

战斗节点的常规战利品应优先来自怪物 `RewardID`。如果节点本身还需要额外奖励，例如宝箱、事件补偿、关卡奖励，可在 `NodePool` 条目上额外配置 `RewardID`。

解析顺序建议：

1.  逐个解析 `MonsterIDs` 对应怪物的 `RewardID`。
2.  若节点条目自身配置了 `RewardID`，再解析节点奖励。
3.  合并后进入同一个战利品拾取面板。

非战斗奖励节点也走同一套战利品拾取与本轮战利品统计：

* 节点奖励生成后进入战利品拾取面板。
* 玩家拖入背包并确认后，节点发布 `CombatLootCollected`，由 `DungeonManager` 记录本轮已拾取战利品。
* 未放入背包的奖励确认后计为丢弃，不进入本轮已拾取战利品账本。
* 节点 UI 不直接改背包归属，只负责展示和确认。

---

## Validator 规则建议

P0 检查：

* `LayerID` 唯一，并在当前正式纵切要求下至少覆盖连续 `1/2/3` 的目标状态；缺失第三层时输出配置缺口。
* `MapProfileID` 或 `MapProfiles[].ProfileID` 非空且唯一。
* `MinWidth <= MaxWidth`，`MinRouteCount <= MaxWidth`，`RowCount > 0`。
* `NodePool` 非空，所有 `NodeType` 在已支持或正式预留枚举中。
* `CombatNode.MonsterIDs` 必须能在 Monsters 找到。
* `BossNode` / `EncounterPools.BossEncounterID` 必须能在 Monsters 找到。
* Boss 指向 `elite_*` 时输出 warning，除非明确允许临时兼任。
* `EndNode.NodeType` 必须为 `StairsNode`、`SafeZoneNode` 或其他具备层终点语义的节点。
* 正式层级建议配置 `MapBackgroundID` 和 `NodeIconSet`，迁移期可 warning。

正式配置检查：

* `19/20/25` 中进入当前批次的 MapProfile、Boss、关键节点必须存在对应配置或明确替代 ID。
* 固定 seed 生成结果必须达到 `Constraints.MinEffectiveRouteCount`。
* 第一层和第二层至少一个精英节点必须可绕；不可把精英做成唯一通路，除非画像明确是高压变体。
* Boss 前一行至少有一个非精英节点。
* 第一层至少覆盖 Combat / Treasure / Event / Elite / Boss / SafeZone。
* 第二层增加 BagPressure / Hazard 或等价压力节点。
* 第三层增加 RouteCorrosion / OrderTarget 或等价路线侵蚀节点。
* Boss 后安全区必须允许撤离 / 深入，并免费恢复 HP / SAN，但不清除物品污染、订单状态和探索账本。
* 同一 `LayerID + RunSeed + MapProfileID` 必须生成相同节点、连线和绑定内容。
* 已解锁层直接出发时只生成目标层，不重复生成前置层。

## 当前 JSON 状态

截至 2026-05-25，当前 `layer_1.json` 和 `layer_2.json` 只代表可运行基线。它们尚未完整填写本文新增的正式字段，也尚未完成 `29_Dungeons正式配置承接审计.md` 中列出的一二层 Boss / 精英拆分、节点池职责拆分、第一 / 第二层正式 MapProfile 和第三层样例配置。

后续推进顺序：

1. 先补 README 字段口径和 Validator 门禁。
2. 再修正一二层 Boss 与精英职责，分离层内 RestStop 和 Boss 后 SafeZone。
3. 再补第一层、第二层正式 MapProfile 和固定 seed 摘要。
4. 最后定义第三层 RouteCorrosion / OrderTarget / Boss 入场变量并新增最小样例。
