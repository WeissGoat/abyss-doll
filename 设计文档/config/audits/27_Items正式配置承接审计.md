---
id: design_27_items_formal_config_audit
title: Items正式配置承接审计
type: audit
role: 策划
domain: formal_config_authoring
status: active
source_of_truth: true
related:
  - 设计文档/README.md
  - 设计文档/config/26_正式配置设计与填充推进计划.md
  - 设计文档/规则卡/02_物品背包旋转与生命周期规则卡.md
  - 设计文档/delivery/15_P0主干配置表现验收承接清单.md
  - 设计文档/content_packs/19_第一层正式核心内容包.md
  - 设计文档/content_packs/20_第二层背包压力内容包.md
  - 设计文档/content_packs/25_第三层路线侵蚀内容包.md
  - 设计文档/config/designs/31_第一层正式配置落地设计.md
  - 设计文档/config/designs/32_第二层正式配置落地设计.md
  - 设计文档/config/designs/33_第三层正式配置落地设计.md
  - 设计文档/config/tasks/34_前三层正式配置实现任务拆分.md
  - 配置表(JSON)/Items/README.md
  - agent_status/design.md
last_verified: 2026-05-25
update_rule: 调整 Items 正式配置字段、前三层物品内容缺口、Validator 建议或配置填充优先级时同步本文件。
---

# Items正式配置承接审计

> **定位：** 本文是 `26_正式配置设计与填充推进计划.md` 中 W1「配置承接审计」的第一份落地产物。它只审计 `Items` 配置域：当前 JSON、配置 README、规则卡、P0 承接清单和前三层内容包之间是否已经对齐。它不直接改配置值，也不替代 `配置表(JSON)/Items/README.md`。

---

## 1. 审计结论

截至 2026-05-25，`配置表(JSON)/Items` 已具备基础网格物品结构，能支持背包旋转、基础战斗数值、出售价值和图标引用。但它还没有达到正式配置承接标准。

当前结论：

| 项 | 判断 | 说明 |
|---|---|---|
| 字段结构 | 部分满足 | 已有 `ConfigID`、`ItemType`、`Rarity`、`BaseValue`、`IconID`、`Grid`、`Combat`、`Tags`；缺少正式版要求的职责、生命周期、来源、去向、风险、经济渠道和绑定字段。 |
| 首批内容量 | 不足 | 当前只有 13 个 JSON；前三层内容包共规划 36 件物品，当前只覆盖一部分第一层和少量第二层 / 成长材料。 |
| 物品职责 | 不足 | 当前条目多为测试 / 基础装备，缺少明确的 `ItemRole`、来源、去向、订单 / 传闻 / 房间记忆连接。 |
| 设计一致性 | 有差异 | 多个现有条目的形状、旋转或用途与 `19/20/25` 内容包描述不一致。 |
| Validator 支撑 | 不足 | 当前可校验基础旋转字段，但尚不能校验正式版生命周期、内容包覆盖、来源去向和风险字段。 |

因此，`Items` 当前状态应标记为：

```text
规则可开发：已完成
配置可承接：进行中
正式配置填充：未完成
```

---

## 2. 当前配置事实

当前 `配置表(JSON)/Items` 共 13 个物品：

| ConfigID | 类型 | 职责判断 | 当前问题 |
|---|---|---|---|
| `gear_rusty_dagger` | Weapon | 第一层低 AP 武器。 | 缺来源、去向、生命周期、经济渠道和职责字段。 |
| `gear_tactical_blade` | Weapon | 第一层旋转长条武器。 | 与内容包“折刃战术刀”职责接近；缺方向 / 相邻效果字段。 |
| `gear_wooden_shield` | Armor | 第一层基础防御。 | 当前形状为 1x2，可旋转；内容包要求 2x2 基础叠盾，占格压力不足。 |
| `gear_iron_armor` | Armor | 第一层 / Boss 大件防御。 | 当前形状为 2x2；内容包要求 2x3，大件压缩空间不足。 |
| `con_repair_kit` | Consumable | HP 恢复消耗品。 | 当前 1x1；内容包要求 1x2，占格成本不足。 |
| `con_cheap_sedative` | Consumable | SAN 稳定消耗品。 | 基础可用；缺节点 / 战斗使用边界和来源。 |
| `loot_gear_scrap` | Loot | 基础材料 / 低价填充物。 | 类型为 Loot，但职责更接近材料；缺材料用途和制造引用。 |
| `loot_rusty_coil` | Loot | 小件贸易 / 材料。 | 缺传闻、订单、制造去向。 |
| `loot_toxic_filter` | Loot | 风险材料。 | 当前为 2x2 不可旋转；内容包要求 L 形 3 格、可旋转，且应带低污染风险。 |
| `mat_core_tier1` | QuestItem | 第一层 Boss 成长材料。 | `QuestItem` 会暗示不可出售 / 任务锁定；内容包定位是稀有材料，需确认是否应改为材料类或新增材料职责字段。 |
| `mat_core_tier2` | QuestItem | 第二层成长材料。 | 图标仍引用 `item_mat_core_tier1_icon`；缺第二层来源和制造去向。 |
| `gear_charge_pistol` | Weapon | 第二层蓄力爆发武器。 | 已覆盖第二层候选物；缺蓄力 / AP 规划职责字段和来源。 |
| `gear_chainsaw_sword` | Weapon | 高阶重型武器。 | 当前未在前三层内容包中明确承接，可能是测试或后续内容；需决定保留批次。 |

---

## 3. 正式字段缺口

`Items` README 当前字段偏基础实现。按 `02` 规则卡和 `15` 承接清单，正式配置还需要补以下字段组。

### 3.1 建议新增静态字段

| 字段组 | 建议字段 | 用途 | 优先级 |
|---|---|---|---|
| 身份 | `ItemRole`、`DescriptionKey`、`LayerTags` | 标记玩法职责、文案键、层级归属。 | P0 |
| 空间 | `DefaultRotation`、`AllowedRotations`、`PlacementTags` | 让旋转、固定朝向、容器限制可配置。 | P0 |
| 生命周期 | `CanSell`、`SellChannel`、`BattleLossRule`、`DefaultBinding` | 支撑出售、撤离、战败、绑定和委托物。 | P0 |
| 来源 | `DropPoolRefs`、`RewardRefs`、`CraftingRefs`、`ShopRefs`、`OrderRefs` | 避免物品无法获得或无法消耗。 | P0 |
| 经济 | `ValueTags`、`RumorRefs`、`ContrabandRule`、`DismantleProfile` | 支撑传闻、黑市、拆解和经济压力。 | P1 |
| 风险 | `RiskTags`、`PollutionProfile`、`CorruptionProfile`、`OvernightRule` | 支撑污染、隔夜代价和第三层路线侵蚀。 | P1 |
| 表现 | `IconVisualID`、`GridPreviewStyle`、`RiskIconRefs`、`PickupHintKey` | 让背包、拾取、出售和验收可读。 | P1 |

### 3.2 明确运行时排除字段

以下字段不应写入静态物品 JSON，只能存在于实例、存档或运行时账本：

| 字段 | 归属 |
|---|---|
| `InstanceID`、`CurrentContainer`、`GridPosition`、`CurrentRotation` | 物品实例 / 背包状态。 |
| `Durability`、`CorruptionLevel`、`DynamicTags`、`IsBinding` | 运行时实例状态。 |
| `OwnerScope`、`SourceRunID`、`CreatedDay`、`LastMovedAt` | 存档 / 日志状态。 |

---

## 4. 前三层内容缺口

### 4.1 第一层缺口

第一层内容包规划 12 件物品，当前覆盖情况如下：

| 设计物品 | 当前配置 | 状态 | 处理建议 |
|---|---|---|---|
| `gear_rusty_dagger` | 已有 | 已有基础配置 | 补职责、来源、去向和经济字段。 |
| `gear_tactical_blade` | 已有 | 基础接近 | 补方向 / 相邻效果和展示职责。 |
| `gear_wooden_shield` | 已有 | 形状不一致 | 改为 2x2 或新增正式盾牌，保留旧测试盾另命名。 |
| `gear_iron_armor` | 已有 | 形状不一致 | 改为 2x3 或新增 `gear_cracked_iron_armor`。 |
| `con_repair_kit` | 已有 | 形状不一致 | 改为 1x2 或新增低级修复包区分测试件。 |
| `con_cheap_sedative` | 已有 | 基础可用 | 补 SAN 使用边界和来源。 |
| `loot_gear_scrap` | 已有 | 基础可用 | 补材料用途和制造引用。 |
| `loot_rusty_coil` | 已有 | 基础可用 | 补传闻 / 订单 / 制造去向。 |
| `loot_toxic_filter` | 已有 | 形状和旋转不一致 | 改为 L 形 3 格可旋转，或新增正式 `loot_toxic_filter_l1`。 |
| `mat_core_tier1` | 已有 | 类型需确认 | 建议从 `QuestItem` 调整为材料职责，委托物另用绑定字段表达。 |
| `trade_miner_lamp` | 缺失 | 未配置 | 新增 1x2 贸易品，连接传闻和房间展示。 |
| `trade_cracked_relic` | 缺失 | 未配置 | 新增 2x2 风险贸易品，连接黑市 / 房间记忆。 |

第一层最低完成标准：

1. 12 件设计物品都能在 JSON 中找到对应 `ConfigID`。
2. 至少 3 件物品能体现空间取舍：长条、2x2 大件、L 形异形。
3. 至少 3 件物品能体现局外回流：出售、制造、房间记忆或传闻。
4. `V-ITEM-ROTATE-01`、`V-ITEM-LIFE-01`、`V-ITEM-SELL-01` 有可配置输入。

### 4.2 第二层缺口

第二层内容包规划 12 件物品，当前只覆盖 2 件或近似件：

| 设计物品 | 当前配置 | 状态 | 处理建议 |
|---|---|---|---|
| `gear_charge_pistol` | 已有 | 基础可用 | 补蓄力职责、来源、去向和第二层标签。 |
| `mat_core_tier2_fragment` | 近似 `mat_core_tier2` | 不一致 | 建议新增碎片 ID，避免和完整二阶核心混淆。 |
| `gear_chain_hook` | 缺失 | 未配置 | 新增中程 / 拉扯构筑武器。 |
| `gear_corroded_bulwark` | 缺失 | 未配置 | 新增 2x3 防具，带维护代价。 |
| `con_solvent_spray` | 缺失 | 未配置 | 新增清理封格 / 残渣的反制消耗品。 |
| `con_stabilizer_ampoule` | 缺失 | 未配置 | 新增 SAN / 污染反制消耗品。 |
| `loot_acid_gland` | 缺失 | 未配置 | 新增 L 形风险材料。 |
| `loot_crystal_scale` | 缺失 | 未配置 | 新增第二层通用成长材料。 |
| `loot_warped_plate` | 缺失 | 未配置 | 新增 2x2 重材料。 |
| `order_live_spore_cage` | 缺失 | 未配置 | 新增 2x3 委托物，占格、不可普通出售。 |
| `trade_luminous_fungus` | 缺失 | 未配置 | 新增 1x2 贸易品。 |
| `trade_sealed_relic_box` | 缺失 | 未配置 | 新增 2x3 风险遗物箱。 |

第二层最低完成标准：

1. 至少新增 8 件第二层正式物品，优先覆盖反制消耗品、风险材料、委托物和大件遗物。
2. `order_live_spore_cage` 必须具备 `DefaultBinding`、`CanSell=false`、订单引用和战败裁决字段。
3. `con_solvent_spray` 必须能被 Validator 识别为对包干涉反制物。
4. `V-L2-PACK-ORDER-01` 和 `V-L2-PACK-BOSS-01` 有可配置输入。

### 4.3 第三层缺口

第三层内容包规划 12 件物品，当前无正式配置覆盖：

| 类别 | 代表物品 | 缺口 |
|---|---|---|
| 异形构筑武器 | `gear_spore_lance`、`gear_vein_sickle` | 缺长条 / L 形中期武器。 |
| 污染防具 | `gear_mycelium_cloak` | 缺抗污染但带维护代价的防具。 |
| 污染反制 | `con_purifying_salt`、`con_anchor_charm` | 缺中级污染 / SAN / 路线侵蚀反制物。 |
| 风险材料 | `loot_living_mycelium`、`loot_corroded_nerve` | 缺隔夜风险和局外处理入口。 |
| 多势力订单物 | `order_contested_spore_core` | 缺 3x3 大件委托物和互斥订单目标。 |
| 高价遗物 | `trade_singing_fossil` | 缺经济 / 房间 / 剧情多去向物。 |

第三层当前不建议立即全部填 JSON。建议先在 README 和 Validator 层定义污染、隔夜、订单互斥所需字段，再补 4-6 件核心样例物。

---

## 5. 现有条目优先修正建议

| 优先级 | ConfigID | 问题 | 建议 |
|---|---|---|---|
| P0 | `loot_toxic_filter` | 与第一层内容包 L 形 3 格、可旋转、轻污染承诺不一致。 | 改为 L 形 3 格并允许旋转，补 `RiskTags` / `OvernightRule`。 |
| P0 | `gear_wooden_shield` | 当前 1x2，无法承担基础叠盾大件取舍。 | 改为 2x2，或新增正式盾牌并把当前条目标记为轻盾。 |
| P0 | `con_repair_kit` | 当前 1x1，缺占格成本。 | 改为 1x2，或新增正式修复包。 |
| P0 | `trade_miner_lamp` | 第一层探索 / 传闻 / 房间记忆关键物缺失。 | 新增 1x2 贸易品。 |
| P0 | `trade_cracked_relic` | 第一层高价风险物缺失。 | 新增 2x2 风险贸易品。 |
| P1 | `mat_core_tier1` | `QuestItem` 表达可能过窄。 | 增加材料职责字段，或改为可被制造引用的稀有材料。 |
| P1 | `mat_core_tier2` | 图标复用 tier1，且与内容包 `fragment` 不一致。 | 新增 `mat_core_tier2_fragment`，修正视觉 ID。 |
| P1 | `gear_chainsaw_sword` | 未被前三层内容包承接。 | 标记为后续批次或补入某个正式内容包。 |

---

## 6. Validator 建议

### 6.1 立即加入 P0 的检查

| 检查项 | 规则 |
|---|---|
| `Grid.Shape` 与 `Grid.GridCost` 一致 | `GridCost` 必须等于 Shape 坐标数量。 |
| 旋转字段合法 | `CanRotate=false` 时 `RotationSteps=1`；`CanRotate=true` 时只允许 `2/4`。 |
| `IconID` 非空 | 所有正式物品必须有图标引用。 |
| `ItemType` 与 `Combat` 一致 | Weapon / Armor / Consumable 必须有 `Combat` 或明确 `UseProfile`；纯 Loot 不应随意带战斗组件，除非有风险 / 被动职责说明。 |
| `BaseValue` 与 `GridCost` 粗校验 | 普通贸易品低于每格 50 或高于每格 500 时输出 warning，稀有 / 风险物可豁免但需标签。 |

### 6.2 进入正式配置后加入的检查

| 检查项 | 规则 |
|---|---|
| 内容包覆盖 | `19/20/25` 中进入当前批次的物品必须存在对应 `ConfigID`。 |
| 来源去向闭环 | 正式物品必须至少有一个来源和一个去向：掉落、奖励、制造、出售、订单、房间展示中的任意组合。 |
| 委托物保护 | 带 `OrderBound` 或 `QuestItem` 职责的物品必须 `CanSell=false` 或指定特殊出售渠道。 |
| 风险物可读 | 带 `RiskTags` 的物品必须有风险表现字段和验收提示。 |
| 传闻引用有效 | `RumorRefs` 指向的传闻必须能在经济配置中找到。 |

---

## 7. 下一步配置任务

### T1：同步 Items README 字段

目标：让配置说明先能承接正式规则。

范围：

* 补 `ItemRole`、`DescriptionKey`、`LayerTags`。
* 补生命周期字段：`CanSell`、`SellChannel`、`BattleLossRule`、`DefaultBinding`。
* 补来源去向字段：`DropPoolRefs`、`RewardRefs`、`CraftingRefs`、`OrderRefs`。
* 明确运行时字段不得写入静态配置。

完成证据：`配置表(JSON)/Items/README.md` 更新，并能映射 `15` 的 Items 承接字段。

### T2：修正第一层关键物品

目标：让第一层 `V-L1-PACK-RUN-01` 有完整输入。

优先处理：

1. `gear_wooden_shield`
2. `con_repair_kit`
3. `loot_toxic_filter`
4. `trade_miner_lamp`
5. `trade_cracked_relic`

完成证据：第一层 12 件物品全部有配置或明确替代 ID。

### T3：补第二层反制和订单物

目标：让第二层背包压力不是纯惩罚。

优先新增：

1. `con_solvent_spray`
2. `con_stabilizer_ampoule`
3. `order_live_spore_cage`
4. `trade_sealed_relic_box`
5. `loot_crystal_scale`

完成证据：`V-L2-PACK-RUN-01`、`V-L2-PACK-ORDER-01` 有可配置输入。

### T4：定义第三层污染字段后再填内容

目标：先把污染 / 隔夜 / 订单互斥字段定下来。

优先处理：

* `RiskTags`
* `PollutionProfile`
* `OvernightRule`
* `OrderConflictGroup`
* `RoomMemoryRefs`

完成证据：第三层至少 4 件核心样例物可配置，并能支持 `V-L3-PACK-CORRUPT-ITEM-01`。

---

## 8. 对 26 的回写判断

本审计完成后，`26` 的 W1 进度应更新为：

| 工作包 | 旧状态 | 新状态 |
|---|---|---|
| `Items` 配置承接审计 | 未开始 | 进行中：字段缺口、内容缺口、现有条目差异和 Validator 建议已完成首轮审计。 |
| `Monsters` 配置承接审计 | 未开始 | 未开始。 |
| `Dungeons` / `Rewards` 配置承接审计 | 未开始 | 未开始。 |

下一份审计建议推进 `Monsters`，因为战斗正式纵切需要怪物意图、对包干涉、掉落和战斗表现一起承接。
