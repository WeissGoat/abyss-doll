---
id: design_29_dungeons_formal_config_audit
title: Dungeons正式配置承接审计
type: audit
role: 策划
domain: formal_config_authoring
status: active
source_of_truth: true
related:
  - 设计文档/README.md
  - 设计文档/config/26_正式配置设计与填充推进计划.md
  - 设计文档/GDD/GDD_02_深渊地图遍历与搜打撤抉择.md
  - 设计文档/delivery/15_P0主干配置表现验收承接清单.md
  - 设计文档/content_packs/19_第一层正式核心内容包.md
  - 设计文档/content_packs/20_第二层背包压力内容包.md
  - 设计文档/content_packs/25_第三层路线侵蚀内容包.md
  - 设计文档/config/designs/31_第一层正式配置落地设计.md
  - 设计文档/config/designs/32_第二层正式配置落地设计.md
  - 设计文档/config/designs/33_第三层正式配置落地设计.md
  - 设计文档/config/tasks/34_前三层正式配置实现任务拆分.md
  - 配置表(JSON)/Dungeons/README.md
  - 配置表(JSON)/Monsters/README.md
  - 配置表(JSON)/Rewards/README.md
  - agent_status/design.md
last_verified: 2026-05-25
update_rule: 调整 Dungeons 正式配置字段、前三层地图画像、节点池、Boss / SafeZone / Stairs、层级入口或 Validator 建议时同步本文件。
---

# Dungeons正式配置承接审计

> **定位：** 本文是 `26_正式配置设计与填充推进计划.md` 中 W1「配置承接审计」的第三份落地产物。它只审计 `Dungeons` 配置域：当前 `layer_*.json`、配置 README、`GDD_02`、P0 承接清单和前三层内容包之间是否已经对齐。它不直接改配置值，也不替代 `配置表(JSON)/Dungeons/README.md`。

---

## 1. 审计结论

截至 2026-05-25，`配置表(JSON)/Dungeons` 已具备基础层级配置和简单节点网络输入：`LayerID`、`MapProfileID`、`RowCount`、宽度、节点池、BossNode 和 EndNode。它可以支撑一二层基础地图生成，但尚未达到正式配置承接标准。

当前结论：

| 项 | 判断 | 说明 |
|---|---|---|
| 字段结构 | 部分满足 | 已有基础网络字段，但缺路线主题、必出节点、节点类型约束、遭遇池、宝箱 / 事件 / 精英池、Boss / SafeZone 语义和 seed 验收摘要字段。 |
| 首批层级 | 不足 | 当前只有 `layer_1.json`、`layer_2.json`；第三层 `layer_3` 未配置。 |
| 地图画像 | 不足 | 当前 `layer_1_tutorial_branching`、`layer_2_pressure_branching` 只是单个 `MapProfileID` 字符串，没有独立画像参数、路线主题权重和必出内容。 |
| 节点池 | 不足 | 第一层 NodePool 只有普通战斗和 SafeRoom；第二层 NodePool 只有普通战斗和 SafeRoom。缺 Treasure、Event、RestStop、Elite、Hazard、订单目标等正式节点。 |
| Boss 承接 | 不满足 | 当前一层 BossNode 为 `elite_scrap_guard`，二层 BossNode 为 `elite_mutant_amalgam`；`28` 已指出二者应回归精英，正式 Boss 应为 `boss_gatekeeper_mk1`、`boss_spore_foundry`。 |
| 安全区 / 终点 | 部分满足 | 当前 `EndNode=StairsNode` 能表达层末阶梯，但 Dungeons README 中仍把 Boss 后终点称为 EndNode / Stairs，缺正式 `SafeZone` 配置语义和免费全恢复 / 撤离 / 深入规则字段。 |
| Validator 支撑 | 不足 | 当前可校验基础字段和引用，但尚不能校验路线数量、精英可绕、Boss 前缓冲、节点类型覆盖、内容包覆盖和 seed 摘要。 |

因此，`Dungeons` 当前状态应标记为：

```text
规则可开发：已完成
配置可承接：进行中
正式配置填充：未完成
```

---

## 2. 当前配置事实

当前 `配置表(JSON)/Dungeons` 共 2 个层级：

| LayerID | MapProfileID | RowCount | Width | NodePool | BossNode | EndNode | 当前问题 |
|---:|---|---:|---|---|---|---|---|
| 1 | `layer_1_tutorial_branching` | 5 | 2-3 | `CombatNode` 拾荒虫、双拾荒虫、`SafeRoomNode` | `elite_scrap_guard` | `StairsNode` | 缺 Event / Treasure / Elite 区分；SafeRoom 被放入普通 NodePool；Boss 临时引用精英；缺第一层第二地图画像。 |
| 2 | `layer_2_pressure_branching` | 6 | 2-4 | `CombatNode` 酸液软体、双酸液软体、`SafeRoomNode` | `elite_mutant_amalgam` | `StairsNode` | 缺第二层正式 MapProfile 名称；缺背包压力节点、Event、Treasure、Elite、订单目标；Boss 临时引用精英；SafeRoom 权重过高。 |

与当前 GDD / 内容包相比，现有 Dungeons 更像“可运行基础地图输入”，不是“正式层级内容配置”。

---

## 3. 正式字段缺口

`Dungeons` README 当前已描述基础网络字段，但仍缺少正式内容承接字段。

### 3.1 建议新增静态字段

| 字段组 | 建议字段 | 用途 | 优先级 |
|---|---|---|---|
| 身份 | `LayerKey`、`DisplayNameKey`、`LayerThemeTags`、`ContentPackRefs` | 标记层级主题、文案键和内容包归属。 | P0 |
| 地图画像 | `MapProfiles[]`、`RouteThemeWeights`、`NodeTypeWeights`、`RequiredNodes` | 支撑同层多画像和路线主题，而不是只有一个字符串。 | P0 |
| 节点约束 | `Constraints.MinCombatPerRoute`、`MaxSameTypeStreak`、`EliteBypassRequired`、`BossBufferRequired`、`MinEffectiveRouteCount` | 让路线质量可校验。 | P0 |
| 遭遇池 | `EncounterPools.Minor`、`EncounterPools.Elite`、`BossEncounterID`、`EncounterTags` | 让普通战斗、精英和 Boss 分层抽取。 | P0 |
| 奖励 / 事件 | `TreasureRewardPoolRefs`、`EventPoolRefs`、`HazardPoolRefs`、`NodeRewardRefs` | 让 Treasure / Event / Hazard 不再只能靠 CombatNode 表达。 | P0 |
| 安全区 / 终点 | `PostBossNode`、`SafeZoneRules`、`AllowExtract`、`AllowDescend`、`FreeRecoverHP`、`FreeRecoverSAN` | 明确 Boss 后安全区免费全恢复、撤离 / 深入语义。 | P0 |
| 层级入口 | `UnlockRule`、`DirectStartAllowed`、`PreviousLayerRequired`、`NextLayerID` | 支撑已通层直达和阶梯深入。 | P1 |
| 调试验收 | `FixedSeedSamples`、`ExpectedRouteSummary`、`ValidationScenarioRefs` | 支撑自动化试玩和策划复查。 | P1 |
| 表现 | `MapBackgroundID`、`NodeIconSet`、`RouteThemeVisuals`、`FogProfile` | 支撑地图 UI、战争迷雾和美术验收。 | P1 |

### 3.2 明确运行时排除字段

以下字段不应写入静态 dungeon JSON，只能存在于运行时地图实例、存档或验收报告：

| 字段 | 归属 |
|---|---|
| 已生成 `NodeRows`、`Edges`、节点唯一实例 ID | 地图实例。 |
| 当前玩家所在节点、已访问节点、可见迷雾范围 | 运行时探索状态。 |
| 本轮随机抽到的具体遭遇 / 奖励结果 | 运行时账本或验收报告。 |
| 本轮已拾取战利品、撤离 / 深入选择、失败原因 | Run 账本。 |

---

## 4. 前三层内容缺口

### 4.1 第一层缺口

第一层内容包规划 2 个 MapProfile，当前只有 1 个基础画像。

| 设计要求 | 当前配置 | 状态 | 处理建议 |
|---|---|---|---|
| `layer1_tutorial_branching` | 近似 `layer_1_tutorial_branching` | 名称接近但字段不足 | 补 Safe / RiskReward 路线主题、必出宝箱 / 事件 / 可绕精英和 seed 摘要。 |
| `layer1_event_miner_remains` | 缺失 | 未配置 | 新增探索向地图画像，提高矿工事件、矿灯和失声矿工影权重。 |
| 普通战斗池 | 只有 `mob_scavenger_bug` | 不足 | 增加 `mob_rust_hound`、`mob_acid_slime`、`mob_lost_miner_echo`。 |
| 精英节点 | 未区分 Elite NodePool | 不足 | 增加 `CombatElite` / `elite_scrap_guard`，并保证可绕。 |
| Boss | `elite_scrap_guard` | 不一致 | 改为 `boss_gatekeeper_mk1`。 |
| Treasure / Event | 缺失 | 未配置 | 增加矿工背包、裂纹圣牌事件和宝箱奖励池。 |
| Boss 后安全区 | `EndNode=StairsNode` | 语义不足 | 增加 `SafeZone` / `PostBossNode` 正式字段或在 README 明确 StairsNode 如何承载免费全恢复和撤离 / 深入。 |

第一层最低完成标准：

1. 至少 2 个 MapProfile：教学分支、矿工探索。
2. 至少 2 条有效路线：安全路线、高收益路线。
3. 至少 1 个可绕精英节点。
4. BossNode 指向 `boss_gatekeeper_mk1`。
5. Boss 后固定出现安全区，且 HP / SAN 免费全恢复、撤离 / 深入规则可验收。

### 4.2 第二层缺口

第二层内容包规划 2 个 MapProfile，当前只有 1 个基础画像，且名称和内容不完全一致。

| 设计要求 | 当前配置 | 状态 | 处理建议 |
|---|---|---|---|
| `layer2_bag_pressure` | 近似 `layer_2_pressure_branching` | 名称和内容不一致 | 统一命名，补 BagPressure 遭遇、Elite、Treasure、Event、Boss / SafeZone。 |
| `layer2_order_spore_hunt` | 缺失 | 未配置 | 新增订单追踪画像，增加订单目标节点和孢子相关遭遇权重。 |
| 普通战斗池 | 只有 `mob_acid_slime` | 不足 | 增加成熟酸液软体、结晶守卫、窃魂虫群、锈蚀培植者。 |
| 精英节点 | 未区分 Elite NodePool | 不足 | 增加晶壁守卫、畸变融合体精英池，至少 1 个可绕。 |
| Boss | `elite_mutant_amalgam` | 不一致 | 改为 `boss_spore_foundry`。 |
| 背包压力节点 | 只靠酸液软体战斗 | 不足 | 增加 `Hazard` / `Event` / `CombatMinor(BagPressure)` 标签和反制入口。 |
| 订单目标 | 缺失 | 未配置 | 增加 `OrderTarget` 或 Event 绑定 `order_live_spore_cage`。 |

第二层最低完成标准：

1. 至少 2 个 MapProfile：背包压力、订单孢子追踪。
2. 至少 3 条路线倾向：Safe、RiskReward、Attrition / OrderTrack。
3. 至少 1 个稳定背包压力遭遇，且可被溶剂喷雾、预留空格或绕路反制。
4. BossNode 指向 `boss_spore_foundry`。
5. 直达第二层时不生成第一层，且地图内容池为第二层。

### 4.3 第三层缺口

第三层内容包规划 2 个 MapProfile，当前完全没有 `layer_3.json`。

| 设计要求 | 当前配置 | 状态 | 处理建议 |
|---|---|---|---|
| `layer3_route_attrition` | 缺失 | 未配置 | 新增路线侵蚀画像，支持 Safe / Attrition / RiskReward。 |
| `layer3_contested_order` | 缺失 | 未配置 | 新增订单冲突画像，支持争夺孢核目标节点。 |
| 第三层怪物池 | 缺失 | 未配置 | 待 `28` 中第三层怪物样例字段定义后再填。 |
| RouteCorrosion | 缺失 | 未配置 | 先定义地图画像 / Hazard / Boss 入场变量字段。 |
| Boss | 缺失 | 未配置 | 新增 `boss_mycelium_oracle`。 |
| 安全区 | 缺失 | 未配置 | Boss 后仍免费恢复 HP / SAN，但不清物品污染和订单状态。 |

第三层当前不建议直接堆完整 JSON。建议先在 Dungeons README 定义 `RouteCorrosion`、订单目标节点、Boss 入场变量和 MapProfile 约束字段，再补 1 个可验收样例画像。

---

## 5. 现有条目优先修正建议

| 优先级 | 配置项 | 问题 | 建议 |
|---|---|---|---|
| P0 | `layer_1.BossNode` | 指向 `elite_scrap_guard`，与正式内容包不一致。 | 改为 `boss_gatekeeper_mk1`；`elite_scrap_guard` 回归精英池。 |
| P0 | `layer_2.BossNode` | 指向 `elite_mutant_amalgam`，与正式内容包不一致。 | 改为 `boss_spore_foundry`；`elite_mutant_amalgam` 回归精英池。 |
| P0 | `NodePool` 节点类型 | 只有 Combat / SafeRoom，无法承接 Treasure、Event、Elite、Hazard、OrderTarget。 | 拆 `NodePool` 或增加 `NodeTag` / `EncounterKind`，让正式节点类型可抽取和校验。 |
| P0 | SafeRoom 权重 | 当前 SafeRoom 进入普通 NodePool，第二层权重 45 过高，可能让路线过于安全。 | Boss 后安全区应固定；层内 RestStop 另配低权重，不等同撤离安全区。 |
| P0 | `MapProfileID` | 只是字符串，不承载画像参数。 | 增加 `MapProfiles` 或 `ProfileRules`，写入路线主题、必出节点、约束和 seed 样例。 |
| P1 | 第三层 | 无 `layer_3.json`。 | 先补字段，再新增第三层最小样例配置。 |
| P1 | `ExpectedNodeCount` | 目前作为摘要存在，但未绑定校验语义。 | 明确它仅为调试摘要，实际节点数由 `RowCount` 和宽度生成。 |

---

## 6. Validator 建议

### 6.1 立即加入 P0 的检查

| 检查项 | 规则 |
|---|---|
| `LayerID` 唯一且连续 | 当前正式纵切至少要求 1、2、3 连续，避免直达和阶梯失效。 |
| `MapProfileID` 非空 | 每层必须有画像 ID；后续若改为数组，则每个画像 ID 必须唯一。 |
| 宽度合法 | `MinWidth <= MaxWidth`，`MinRouteCount <= MaxWidth`，`RowCount > 0`。 |
| NodePool 非空 | 每层至少有一个可抽节点。 |
| `CombatNode.MonsterIDs` 有效 | 所有怪物 ID 必须存在于 Monsters。 |
| `BossNode` 有效 | BossNode 必须存在于 Monsters。 |
| `BossNode` 精英 warning | BossNode 指向 `elite_*` 时输出 warning，除非明确允许精英兼任 Boss。 |
| `EndNode.NodeType` 有效 | 当前必须为可识别终点节点，例如 `StairsNode` 或正式 `SafeZoneNode`。 |
| `MapBackgroundID` / `NodeIconID` 非空 | 正式层级和节点必须有基础视觉引用，至少输出 warning。 |

### 6.2 进入正式配置后加入的检查

| 检查项 | 规则 |
|---|---|
| 内容包覆盖 | `19/20/25` 中进入当前批次的 MapProfile、Boss、关键节点必须存在对应配置。 |
| 路线数量 | 固定 seed 摘要中有效路线数量必须达到 `MinEffectiveRouteCount`。 |
| 精英可绕 | 第一层和第二层至少一个精英节点必须可绕；不可把精英做成唯一通路，除非画像明确是高压变体。 |
| Boss 前缓冲 | Boss 前一行至少有一个非精英节点。 |
| 节点类型覆盖 | 第一层至少覆盖 Combat / Treasure / Event / Elite / Boss / SafeZone；第二层增加 BagPressure / Hazard 或等价压力节点；第三层增加 RouteCorrosion / OrderTarget。 |
| SafeZone 语义 | Boss 后必须进入允许撤离 / 深入的安全区，且免费恢复 HP / SAN，不清除物品污染、订单状态和探索账本。 |
| Seed 可复现 | 同一 `LayerID + RunSeed + MapProfileID` 生成相同节点、连线和绑定内容。 |
| 层级直达 | 已解锁层直接出发时必须只生成目标层，不重复生成前置层。 |

---

## 7. 下一步配置任务

### T1：同步 Dungeons README 字段

目标：让配置说明能承接正式地图网络。

范围：

* 补 MapProfile / RouteTheme / RequiredNodes / Constraints 字段。
* 补普通遭遇池、精英遭遇池、BossEncounter、Treasure、Event、Hazard、OrderTarget。
* 补 SafeZone / Stairs 的正式语义：免费全恢复、撤离、深入、账本保留。
* 补固定 seed 摘要和 Validator 输出要求。

完成证据：`配置表(JSON)/Dungeons/README.md` 更新，并能映射 `GDD_02` 和 `15` 的 Dungeons 承接字段。

### T2：修正一二层 Boss 与节点池职责

目标：让现有可运行层级不再用精英临时顶 Boss。

优先处理：

1. `layer_1.BossNode -> boss_gatekeeper_mk1`
2. `layer_2.BossNode -> boss_spore_foundry`
3. 增加 Elite 节点 / 遭遇池字段。
4. 将层内 RestStop 与 Boss 后 SafeZone 分开。

完成证据：一二层 BossNode 与 `28` 的 Monsters 审计一致。

### T3：补第一层正式画像

目标：让第一层不是只有拾荒虫路线。

优先新增 / 修正：

1. `layer1_tutorial_branching`
2. `layer1_event_miner_remains`
3. Treasure / Event / Elite 节点
4. Seed 摘要：`LayerID=1`、`RunSeed=1024`

完成证据：`V-L1-PACK-RUN-01`、`V-L1-PACK-RUN-02`、`V-L1-PACK-RUN-03` 有地图配置输入。

### T4：补第二层正式画像

目标：让第二层稳定承担背包压力和订单目标。

优先新增 / 修正：

1. `layer2_bag_pressure`
2. `layer2_order_spore_hunt`
3. BagPressure / Hazard / OrderTarget 节点
4. Seed 摘要：`LayerID=2`、`RunSeed=2048`

完成证据：`V-L2-DIRECT-2048-ROUTE-01`、`V-L2-PACK-RUN-01`、`V-L2-PACK-ORDER-01` 有地图配置输入。

### T5：定义第三层字段后再填样例

目标：先让第三层路线侵蚀有字段，再新增 JSON。

优先定义：

* `RouteCorrosion`
* `OrderTarget`
* `BossEntryModifiers`
* `AttritionRoute`
* `ContestedOrderRoute`

完成证据：第三层至少 1 个 MapProfile 样例可配置，并能支持 `V-L3-PACK-DIRECT-01`、`V-L3-PACK-ROUTE-01`、`V-L3-PACK-BOSS-01`。

---

## 8. 对 26 的回写判断

本审计完成后，`26` 的 W1 进度应更新为：

| 工作包 | 旧状态 | 新状态 |
|---|---|---|
| `Items` 配置承接审计 | 进行中：首轮审计已完成 | 进行中：等待 README 字段同步和配置落地。 |
| `Monsters` 配置承接审计 | 进行中：首轮审计已完成 | 进行中：等待 README 字段同步和配置落地。 |
| `Dungeons` 配置承接审计 | 未开始 | 进行中：字段缺口、前三层地图画像缺口、NodePool / BossNode / SafeZone 语义差异和 Validator 建议已完成首轮审计。 |
| `Rewards` 配置承接审计 | 未开始 | 未开始。 |

下一份审计建议推进 `Rewards`，因为 Items、Monsters、Dungeons 审计都已经指向奖励引用、Boss 保底、节点奖励和内容包产出闭环。
