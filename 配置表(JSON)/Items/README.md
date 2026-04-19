# 局内物品与网格实体字段说明 (Items Config)

> 本目录包含了游戏中数量最多、最复杂的配置表。
> **所有能放进人偶“背包网格”里的东西（武器、防具、耗材、素材、高价垃圾），底层都是这个结构。**
> 这是检验“1CV(单格) = 100金币 = 10DPS”价值模型的唯一阵地。

## 核心字段说明表

| 字段名 (Key) | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `ConfigID` | string | 物品配置表的全局唯一ID | 必填项 |
| `Name` | string | 物品显示名称 | |
| `ItemType` | enum | 物品的大类归属 | `Weapon`(武器), `Armor`(防具), `Consumable`(消耗品), `Loot`(纯卖钱的战利品), `QuestItem`(不可售卖的任务/核心物), `Anchor`(情感锚点物) |
| `Rarity` | enum | 物品稀有度（品级） | `Common`(白), `Uncommon`(蓝), `Rare`(紫), `Epic`(金), `Cursed`(红,诅咒绑定) |
| `BaseValue` | int | **基准估值 (E)** | 物品在普通物价下卖入商店的金币价格。必须与 GridCost 挂钩核算。 |
| `Grid` | object | 网格空间占用组件 | 详细定义该物品的形状及占地大小（见下表） |
| `Combat` | object | 战斗交互组件（可选） | 武器/防具/消耗品必带；纯战利品(Loot)此项为 `null` 或不存在 |
| `Tags` | array<enum> | 物品标签组 | 用于配合义体连结、怪物干涉、特质检测。如：`Mechanical`(机械), `Toxic`(毒性,放包里掉SAN), `Heavy`(沉重), `Melee`(近战) |

---

## Grid (网格占用组件) 内部字段

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `Shape` | array<Vector2>| 形状坐标点集，定义在二维网格里的相对形状 | 如 L 型为 `[[0,0], [0,1], [1,0]]` |
| `GridCost` | int | 实际占用的总格子数 | 价值公式中计算 `(CV)` 的基准标尺 |

---

## Combat (战斗与交互组件) 内部字段
如果物品是武器、防具或药水，则具备此组件。

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `TriggerType` | enum | 该物品生效（开火/防御）的触发条件 | `Passive`(被动/自动生效), `Manual`(手动点,需耗局内AP) |
| `APCost` | int | 触发一次消耗的行动点数(AP) | 结合单次伤害计算该物品的AP收益(DPA) |
| `DamageType` | enum | 输出的效果类型 | `Physical`(物理伤害), `Energy`(能量伤害), `Shield`(产生临时护盾), `Heal`(回血), `RestoreSAN`(回理智) |
| `BaseValue` | int | 基础伤害/治疗/护盾的数值 | 计算战斗力的基础项 |
| `AdjacencyBuffs`| array | 连结/拼图增益机制（高级特性） | 当放置在特定位置时触发（见下表） |

### AdjacencyBuffs (相邻增益) 详细字段
用于实现背包拼图游戏的核心爽点：比如“放在右侧的武器增加30%伤害”。

| 字段名 | 数据类型 | 注释说明 | 可选项 / 备注 |
| :--- | :--- | :--- | :--- |
| `TargetDirection` | enum | 连结的判定方向 | `Right`, `Left`, `Up`, `Down`, `AllAdjacent`(所有相邻格) |
| `TargetTags` | array | 必须匹配的受击方标签才生效 | 如 `["Weapon"]` 或 `["Energy"]` |
| `EffectType` | enum | 施加的增益/减益类型 | `DamageMultiplier`(最终伤害乘区), `CooldownReduction`(冷却缩减) |
| `Value` | float | 增幅的具体数值系数 | 如 `0.3` 代表增幅 30% |