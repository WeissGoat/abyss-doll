---
id: config_items_readme
title: 局内物品与网格实体字段说明 (Items Config)
type: config
role: 策划
domain: config_items
status: active
source_of_truth: true
related:
  - 开发文档/02_网格背包与计算系统(GridSystem).md
  - 开发文档/数据与实体定义/03_物品与网格实体.md
  - 数值模型设计/00_基准价值与空间本位模型.md
  - 配置表(JSON)/README.md
  - 配置表(JSON)/Rumors/README.md
  - 设计文档/GDD_01_背包战斗与局内网格机制.md
  - 设计文档/GDD_06_物品系统与物品生命周期.md
  - 设计文档/06_标签与特质规则卡.md
  - 设计文档/27_Items正式配置承接审计.md
  - 设计文档/30_Rewards正式配置承接审计.md
  - 设计文档/31_第一层正式配置落地设计.md
  - 设计文档/32_第二层正式配置落地设计.md
  - 设计文档/33_第三层正式配置落地设计.md
  - 设计文档/43_局外成长制造维护正式配置落地设计.md
  - 设计文档/48_经济压力传闻正式配置落地设计.md
  - 设计文档/50_经济压力订单正式配置落地设计.md
  - 设计文档/52_经济压力正式配置实现任务拆分.md
last_verified: 2026-05-25
update_rule: 修改配置字段、数据源规则或表间引用时同步本文件。
---

# 局内物品与网格实体字段说明 (Items Config)

> 本目录包含了游戏中数量最多、最复杂的配置表。
> **所有能放进人偶“背包网格”里的东西（武器、防具、耗材、素材、高价垃圾），底层都是这个结构。**
> 这是检验“1CV(单格) = 100金币 = 10DPS”价值模型的唯一阵地。

正式配置字段、前三层内容缺口、现有条目差异和 Validator 建议见 `设计文档/27_Items正式配置承接审计.md`。该审计是配置改造依据，不代表本文字段已经全部落地。

## 正式配置基本规则

`Items` 是所有可进入背包网格的静态配置源。正式配置必须回答四个问题：

1. 这个物品在玩法里承担什么职责。
2. 玩家从哪里获得它。
3. 它在局内、结算、出售、制造、订单或房间记忆中会去向哪里。
4. 它的风险、价值和表现能否被 Validator 或固定验收路径检查。

运行时实例状态不得写入静态物品 JSON。`InstanceID`、当前位置、当前旋转、耐久、污染值、动态标签、绑定状态、来源 RunID 等只属于存档、背包实例或运行时账本。

当前 `Items/*.json` 是可运行基线，不代表正式字段已完成。后续 JSON 补齐前，先以本文字段口径和 `27_Items正式配置承接审计.md` 为准。

## 核心字段说明表

| 字段名 (Key) | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `ConfigID` | string | 物品配置表的全局唯一ID | 必填项 |
| `Name` | string | 物品显示名称 | |
| `DescriptionKey` | string | 文案表键，用于拾取、出售、订单和详情面板描述 | 正式配置建议必填；临时测试物可留空但 Validator 输出 warning |
| `ItemType` | enum | 物品的大类归属 | `Weapon`(武器), `Armor`(防具), `Consumable`(消耗品), `Loot`(纯卖钱的战利品), `QuestItem`(不可售卖的任务/核心物), `Anchor`(情感锚点物) |
| `ItemRole` | enum 或 array | 物品的玩法职责 | `Tutorial`, `BuildCore`, `Defense`, `Recovery`, `Material`, `Trade`, `OrderBound`, `RiskObject`, `RoomMemory`, `BossKey`, `Contraband` |
| `Rarity` | enum | 物品稀有度（品级） | `Common`(白), `Uncommon`(蓝), `Rare`(紫), `Epic`(金), `Cursed`(红,诅咒绑定) |
| `BaseValue` | int | **基准估值 (E)** | 物品在普通物价下卖入商店的金币价格。必须与 GridCost 挂钩核算。 |
| `LayerTags` | array<string> | 层级归属和内容包归属 | 例如 `Layer1`, `Layer2`, `Layer3`, `ContentPack19` |
| `Grid` | object | 网格空间占用组件 | 详细定义该物品的形状及占地大小（见下表） |
| `Combat` | object | 战斗/交互/被动效果组件（可选） | 武器/防具/消耗品必带；带运行时效果的战利品也可以配置 `Passive` 效果 |
| `Tags` | array<enum> | 物品标签组 | 用于配合义体连结、怪物干涉、特质检测。如：`Mechanical`(机械), `Toxic`(毒性分类), `Heavy`(沉重), `Melee`(近战)。标签本身不直接执行逻辑 |
| `Lifecycle` | object | 出售、战败、撤离、绑定和局外去向规则 | 见 `Lifecycle` 字段组 |
| `SourceRefs` | object | 物品来源引用 | 见 `SourceRefs` 字段组 |
| `SinkRefs` | object | 物品消耗 / 去向引用 | 见 `SinkRefs` 字段组 |
| `Economy` | object | 传闻、价值标签、黑市和拆解口径 | 见 `Economy` 字段组 |
| `Risk` | object | 污染、腐蚀、违禁、隔夜等风险规则 | 见 `Risk` 字段组 |
| `Visual` | object | 图标、网格预览、风险标记和拾取提示 | 见 `Visual` 字段组；`IconID` 旧字段可兼容到 `Visual.IconVisualID` |

---

## Grid (网格占用组件) 内部字段

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `Shape` | array<Vector2>| 形状坐标点集，定义在二维网格里的相对形状 | 如 L 型为 `[[0,0], [0,1], [1,0]]` |
| `CanRotate` | bool | 是否允许玩家在背包整理、战利品拾取等交互中旋转该物品 | 必填。不可旋转物品必须为 `false` |
| `RotationSteps` | int | 可用朝向数量 | 必填。`1`=仅 0 度；`2`=0/90；`4`=0/90/180/270 |
| `GridCost` | int | 实际占用的总格子数 | 价值公式中计算 `(CV)` 的基准标尺 |
| `DefaultRotation` | int | 默认朝向 | 默认 `0`；仅允许 `0/90/180/270` 中配置可用朝向 |
| `AllowedRotations` | array<int> | 允许朝向列表 | 建议由 `CanRotate` 和 `RotationSteps` 推导；正式配置可显式写入便于 Validator 检查 |
| `PlacementTags` | array<string> | 容器、槽位或特殊放置限制 | 例如 `Backpack`, `WorkshopOnly`, `OrderCargo`, `NoAutoSort` |

### Grid 旋转配置规则

旋转能力必须由配置显式声明，运行时不会根据物品形状自动推断。

| 配置组合 | 运行时规则 |
| :--- | :--- |
| `CanRotate=false`, `RotationSteps=1` | 固定 0 度。任何非 0 角度放置都会被 `ConfigValidator` 或 `BackpackGrid` 拒绝 |
| `CanRotate=true`, `RotationSteps=2` | 两向旋转，只允许 `0/90` 循环 |
| `CanRotate=true`, `RotationSteps=4` | 四向旋转，允许 `0/90/180/270` 循环 |

配置校验要求：

* `Grid.CanRotate` 与 `Grid.RotationSteps` 都是必填字段。
* `CanRotate=false` 时 `RotationSteps` 必须为 `1`。
* `CanRotate=true` 时 `RotationSteps` 只能为 `2` 或 `4`。
* 方向型效果的 `Target` 会随物品当前旋转后的朝向重新计算。

---

## Lifecycle (物品生命周期字段组)

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `CanSell` | bool | 是否允许普通出售 | `OrderBound`、核心委托物、部分剧情物通常为 `false` |
| `SellChannel` | enum | 可出售渠道 | `Shop`, `BlackMarket`, `OrderOnly`, `None` |
| `BattleLossRule` | enum | 战败时处理方式 | `Keep`, `Drop`, `Destroy`, `ConvertToScrap`, `InjureDollAndKeep` |
| `EvacuationRule` | enum | 撤离成功后的流向 | `KeepInBackpack`, `MoveToTownInventory`, `ConvertToReward`, `ResolveOrder` |
| `DefaultBinding` | enum | 默认绑定规则 | `None`, `RunBound`, `OrderBound`, `DollBound`, `StoryBound` |
| `CraftingConsumeRule` | enum | 制造 / 维护消耗规则 | `Consumable`, `ToolKept`, `Ingredient`, `Catalyst` |

## SourceRefs (来源引用字段组)

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `DropPoolRefs` | array<string> | 怪物或节点掉落池引用 | 指向 Monsters / Rewards / Dungeons 中的掉落来源 |
| `RewardRefs` | array<string> | 奖励配置引用 | 指向 `Rewards/*.json` |
| `CraftingRefs` | array<string> | 可被配方制造或转化的引用 | 指向 CraftingRecipes |
| `ShopRefs` | array<string> | 商店、黑市或事件商人来源 | 当前可预留，未落 JSON 前 Validator 只做 warning |
| `OrderRefs` | array<string> | 订单需求或奖励引用 | 指向 Orders / Factions 相关配置 |

## SinkRefs (去向引用字段组)

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `SellOutcomeRefs` | array<string> | 出售后的经济或声望结果 | 普通出售可为空；特殊出售需填写 |
| `CraftingAsIngredientRefs` | array<string> | 作为材料时能进入的配方 | 正式材料建议至少 1 条 |
| `OrderSubmitRefs` | array<string> | 可提交到哪些订单 | 委托物和势力需求物必填 |
| `RoomMemoryRefs` | array<string> | 可形成的房间纪念物 / 日记引用 | 高价遗物、Boss 材料、剧情物建议填写 |
| `ScenarioEventRefs` | array<string> | 可触发的剧本事件引用 | 剧情物、违禁品、关键遗物使用 |

## Economy (经济字段组)

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `ValueTags` | array<string> | 价格和需求分类 | 例如 `Scrap`, `Relic`, `Medical`, `Fungal`, `Contraband` |
| `RumorRefs` | array<string> | 传闻 / 价格波引用 | 指向经济传闻配置；未配置时可为空 |
| `ContrabandRule` | enum | 违禁品处理规则 | `None`, `IllegalToSell`, `BlackMarketOnly`, `OvernightPenalty` |
| `DismantleProfile` | string | 拆解产物配置 | 可指向 CraftingRecipes 或未来 DismantleProfiles |

## Risk (风险字段组)

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `RiskTags` | array<string> | 风险分类 | `Toxic`, `Corroded`, `Cursed`, `Unstable`, `OrderConflict`, `Contraband` |
| `PollutionProfile` | string | 污染 / SAN / 腐蚀影响 | 第三层路线侵蚀物品优先补 |
| `CorruptionProfile` | string | 腐蚀背包、义体或人偶的规则引用 | 可预留到 Effects / Traits |
| `OvernightRule` | enum | 隔夜处理 | `None`, `Decay`, `PolluteInventory`, `Convert`, `TriggerEvent` |
| `OrderConflictGroup` | string | 互斥订单或势力冲突组 | 第二 / 三层委托物和争夺物使用 |

## Visual (表现字段组)

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `IconVisualID` | string | 运行时图标 VisualID | 兼容旧 `IconID`；正式配置建议统一到 VisualID |
| `GridPreviewStyle` | enum | 网格内显示风格 | `Normal`, `LargeGear`, `Material`, `QuestCargo`, `RiskObject` |
| `RiskIconRefs` | array<string> | 风险角标 VisualID | 风险物品必须填写或继承默认 |
| `PickupHintKey` | string | 拾取提示文案键 | 用于解释不可出售、订单绑定、隔夜风险等 |

---

## Combat (战斗与交互组件) 内部字段
如果物品是武器、防具、药水，或需要在背包中持续生效的特殊战利品，则具备此组件。

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `TriggerType` | enum | 该物品生效（开火/防御）的触发条件 | `Passive`(被动/自动生效), `Manual`(手动点,需耗局内AP) |
| `APCost` | int | 触发一次消耗的行动点数(AP) | 结合单次伤害计算该物品的AP收益(DPA) |
| `DamageType` | enum | 输出的效果类型 | `None`(无直接战斗数值), `Physical`(物理伤害), `Energy`(能量伤害), `Shield`(产生临时护盾), `Heal`(回血), `RestoreSAN`(回理智) |
| `BaseValue` | int | 基础伤害/治疗/护盾的数值 | 计算战斗力的基础项 |
| `Effects` | array | 统一效果列表 | 元素使用 `EffectData`。通用成本修正可配置 `ModifyResourceCost` |

## EffectData 通用字段

| 字段 | 类型 | 说明 |
| :--- | :--- | :--- |
| `EffectID` | string | 对应 C# `EffectBase` 派生类，由 `EffectFactory` 实例化 |
| `Level` | int | 效果等级 |
| `Target` | string | 目标/方向/分类。含义由具体 Effect 定义 |
| `Trigger` | string | 可选。通用 modifier 的触发点，例如 `OnDungeonMoveCost` |
| `Resource` | string | 可选。通用 modifier 的资源类型，例如 `SAN`、`HP`、`Money`、`AP` |
| `Operation` | string | 可选。通用 modifier 的运算方式，例如 `AddFlat`、`AddPercent`、`Multiply` |
| `Params` | float[] | 效果参数。`ModifyResourceCost` 使用 `Params[0]` 作为修正值 |

### AdjacencyBuffs (相邻增益) 详细字段
用于实现背包拼图游戏的核心爽点：比如“放在右侧的武器增加30%伤害”。

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `TargetDirection` | enum | 连结的判定方向 | `Right`, `Left`, `Up`, `Down`, `AllAdjacent`(所有相邻格) |
| `TargetTags` | array | 必须匹配的受击方标签才生效 | 如 `["Weapon"]` 或 `["Energy"]` |
| `EffectType` | enum | 施加的增益/减益类型 | `DamageMultiplier`(最终伤害乘区), `CooldownReduction`(冷却缩减) |
| `Value` | float | 增幅的具体数值系数 | 如 `0.3` 代表增幅 30% |

---

## 正式物品职责口径

| `ItemRole` | 设计含义 | 字段要求 |
| :--- | :--- | :--- |
| `Tutorial` | 教学物，用于解释基础规则 | 来源清晰，风险较低，表现提示明确 |
| `BuildCore` | 构筑核心武器、防具或义体材料 | 必须有战斗 / 构筑字段和至少一个来源 |
| `Recovery` | HP / SAN / 状态恢复物 | 必须有 `Combat` 或 `UseProfile`，并说明使用边界 |
| `Material` | 制造、维护或升级材料 | 必须至少有一个 `CraftingAsIngredientRefs` 或明确后续补齐 |
| `Trade` | 主要用于出售或价格波动 | 必须有 `CanSell=true`、`SellChannel` 和价值标签 |
| `OrderBound` | 委托物或订单目标 | 必须有 `DefaultBinding=OrderBound`、`CanSell=false` 或特殊 `SellChannel` |
| `RiskObject` | 会带来污染、腐蚀、违禁或隔夜风险 | 必须填写 `RiskTags` 和玩家可见风险表现 |
| `RoomMemory` | 可沉淀为房间展示、日记或长期记忆 | 必须填写 `RoomMemoryRefs` 或 `ScenarioEventRefs` |
| `BossKey` | Boss 或层级推进关键材料 | 必须有关联 Boss / 奖励 / 成长去向 |

## Validator 规则建议

P0 检查：

* `ConfigID` 全局唯一，且文件名与 `ConfigID` 一致。
* `Grid.Shape` 坐标数量必须等于 `Grid.GridCost`。
* `Grid.CanRotate=false` 时 `RotationSteps=1`。
* `Grid.CanRotate=true` 时 `RotationSteps` 只能为 `2` 或 `4`。
* `Weapon` / `Armor` / `Consumable` 必须带 `Combat`、`UseProfile` 或明确的非战斗职责。
* 正式物品必须有 `IconID` 或 `Visual.IconVisualID`。
* `BaseValue / GridCost` 超出常规价值带时输出 warning；稀有、Boss、风险物可用 `ValueTags` 或 `RiskTags` 豁免。

正式配置检查：

* 进入当前批次的 `19/20/25` 内容包物品必须存在对应 `ConfigID` 或明确替代 ID。
* 正式物品至少有一个来源和一个去向：掉落、奖励、制造、出售、订单、房间展示或剧情事件。
* `OrderBound` 物品必须禁止普通出售，或指定特殊提交 / 黑市渠道。
* `RiskTags` 非空时必须有风险表现字段和验收提示。
* `RumorRefs`、`RewardRefs`、`CraftingRefs`、`OrderRefs`、`RoomMemoryRefs` 指向的配置必须可解析；未落地配置可先 warning，进入正式 JSON 后升级为 error。

## 当前 JSON 状态

截至 2026-05-25，当前 13 个 `Items/*.json` 只代表可运行基线。它们尚未完整填写本文新增的正式字段，也尚未完成 `27_Items正式配置承接审计.md` 中列出的第一层关键物品修正、第二层反制 / 订单物补齐和第三层污染字段样例。

后续推进顺序：

1. 先修正第一层关键物品的形状、旋转、职责、来源和去向。
2. 再补第二层反制消耗品、风险材料和订单绑定物。
3. 再定义第三层污染、隔夜和订单互斥样例。
4. 最后将 Validator warning 逐步升级为正式门禁。
