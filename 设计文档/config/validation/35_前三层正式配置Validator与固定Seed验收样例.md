---
id: design_35_first_three_layers_validator_seed_acceptance
title: 前三层正式配置Validator与固定Seed验收样例
type: acceptance_spec
role: 策划
domain: formal_config_acceptance
status: active
source_of_truth: true
related:
  - PROJECT_STATUS.md
  - 设计文档/README.md
  - 设计文档/delivery/13_策划跨系统验收场景矩阵.md
  - 设计文档/config/26_正式配置设计与填充推进计划.md
  - 设计文档/config/tasks/34_前三层正式配置实现任务拆分.md
  - 开发文档/15_P0配置Validator与自动验收底座需求.md
  - agent_status/design.md
last_verified: 2026-05-25
update_rule: 调整前三层正式配置 Validator 覆盖、固定 seed 样例、验收证据、失败信号或完成判定时同步本文件。
---

# 前三层正式配置Validator与固定Seed验收样例

> **定位：** 本文是策划侧验收样例设计，不是程序实现文档，也不直接修改 JSON。`34` 说明前三层正式配置后续应该拆成哪些实现任务；本文说明这些任务完成后，Validator、固定 seed 和人工复核应该如何证明配置真的可用。

---

## 1. 使用边界

本文只回答三个问题：

1. `34` 中的 `L1/L2/L3` 配置实现任务完成后，应该用哪些检查项验收。
2. 哪些固定 seed / 手动路径能证明前三层内容形成体验，而不只是 ID 存在。
3. 什么证据可以回写 `agent_status/design.md`，什么情况必须判为未完成；如影响长期节点门禁，再通知 PM 更新 `09`。

本文不定义：

| 不定义项 | 归属 |
|---|---|
| 具体物品、怪物、地图、奖励 ID 的设计内容 | `31` / `32` / `33` |
| 配置实现任务、依赖顺序和交付证据格式 | `34` |
| Validator 程序入口、报告格式和脚本参数 | `开发文档/15_P0配置Validator与自动验收底座需求.md` |
| 运行时 UI 或美术截图验收 | 美术文档、开发文档和 ArtAcceptance |

---

## 2. 验收总原则

前三层正式配置验收必须同时覆盖静态配置和玩家路径。

| 验收层 | 证明内容 | 不足时的风险 |
|---|---|---|
| 静态 Validator | 字段存在、枚举合法、ID 引用不断、Boss 保底和层级规则可解析。 | 配置看似完成，但运行时掉落、地图、订单或怪物引用断裂。 |
| 固定 seed | 同一层级、同一 MapProfile、同一 seed 能复现路线、节点、战斗、奖励和结算摘要。 | 配置随机可跑，但无法回归节奏和路线风险。 |
| 人工体验复核 | 玩家能读懂目标、风险、取舍、奖励和失败原因。 | 工具通过，但体验仍像测试数据堆叠。 |

配置实现任务不能只凭“JSON 已修改”判定完成。最低完成证据应包含：

1. 配置 ID 清单。
2. 引用检查结果。
3. `.\tools\config\Sync-Configs.ps1 -Clean` 结果或未运行原因。
4. Validator 报告或等待程序工具补强的明确缺口。
5. 固定 seed 摘要或人工验收记录。
6. `agent_status/design.md` 对应策划 / 配置状态更新；如影响长期节点门禁，PM 同步 `09`。

---

## 3. Validator 覆盖矩阵

### 3.1 主干静态检查

| ValidatorID | 对应任务 | 等级 | 检查目标 | 失败信号 |
|---|---|---|---|---|
| `CFG-ITEM-ROLE-001` | `L1/L2/L3-ITEMS` | error | 正式物品必须有 `ItemRole`、`LayerTags`、形状、旋转、生命周期、来源和去向。 | 物品只有名称 / 价格，没有玩法职责；订单物或污染物缺去向。 |
| `CFG-ITEM-SHAPE-001` | `L1/L2/L3-ITEMS` | error | `ShapeCells` 非空、格子不重复、旋转后仍合法。 | 物品无法放入背包，或旋转后占格异常。 |
| `CFG-ITEM-LIFECYCLE-001` | `L1/L2/L3-ITEMS` | error | 委托物、绑定物、污染物、可出售物的撤离 / 战败 / 出售 / 制造去向可解析。 | 订单物能普通出售，污染物隔夜无处理规则，绑定物战败丢失规则不明。 |
| `CFG-REWARD-REF-001` | `L1/L2/L3-REWARDS` | error | 奖励引用的 ItemID / Currency / Unlock / Reputation 存在。 | 奖励池出现不存在物品，Boss 奖励引用未开放层内容。 |
| `CFG-REWARD-BOSS-001` | `L1/L2/L3-REWARDS` | error | 每层成长钥匙保底挂在 Boss 奖励，不挂在精英或随机宝箱。 | `mat_core_tier1/2/3` 由精英或随机宝箱承担保底。 |
| `CFG-MONSTER-REF-001` | `L1/L2/L3-MONSTERS` | error | 怪物 `RewardID`、`CombatVisualID`、层级投放标签和意图引用存在。 | 怪物胜利后无奖励、无可读意图或战斗图引用缺失。 |
| `CFG-MONSTER-ROLE-001` | `L1/L2/L3-MONSTERS` | warning / error | `Mob`、`Elite`、`Boss` 职责不混淆；Boss / 精英有区别机制。 | 普通怪、精英、Boss 只有血量差异；精英承担 Boss 保底。 |
| `CFG-DUNGEON-REF-001` | `L1/L2/L3-DUNGEONS` | error | 节点池引用的怪物、奖励、事件、Boss、SafeZone、Stairs 存在。 | 地图生成到空节点、悬空奖励或不存在 Boss。 |
| `CFG-DUNGEON-ROUTE-001` | `L1/L2/L3-DUNGEONS` | error | 每层至少有低风险路线、高收益路线和 Boss 可达路径。 | 单线推进、Boss 不可达、精英不可绕。 |
| `CFG-DUNGEON-SAFEZONE-001` | `L1/L2/L3-DUNGEONS` | error | Boss 后 SafeZone 免费恢复 HP / SAN，但不清理背包取舍、订单、污染或探索账本。 | 安全区收费、无恢复，或把订单 / 污染 / 探索状态误清空。 |
| `CFG-LAYER-DIRECT-001` | `L2/L3-DUNGEONS` | error | 已通层直达第二 / 第三层，不重新生成前置层。 | 从第二层出发仍要打一层，或第三层入口不检查解锁。 |
| `CFG-ORDER-CONFLICT-001` | `L2/L3-ORDERS` | error | 订单物绑定、提交目标、互斥关系、失败裁决和奖励引用可解析。 | 同一委托物可重复提交给多个势力；订单只是文本提示。 |
| `CFG-ROUTE-CORROSION-001` | `L3-DUNGEONS` | error | `RouteCorrosion` 是本次下潜变量，能影响 Boss 入场，并在撤离 / 本层结束后清理。 | 路线侵蚀永久污染存档，或不影响任何节点 / Boss。 |
| `CFG-SEED-001` | `L1/L2/L3-SEED` | error | 固定 seed 摘要稳定，且覆盖目标路径。 | 同 seed 重跑路线变化，或 seed 只覆盖普通战斗。 |

### 3.2 内容职责检查

每条正式配置至少归入一个职责。Validator 可先 warning，Strict 或候选版本应升级为 error。

| 职责 | 适用配置 | 检查方式 |
|---|---|---|
| 教学 | 一层物品、普通怪、基础节点 | 是否标记教学目标、低风险投放和可读反馈。 |
| 检验 | 精英、Boss、路线压力节点 | 是否要求玩家使用已学规则解决。 |
| 取舍 | 大形状物品、风险奖励、订单目标、污染物 | 是否占用空间、带来风险、改变路线或经济选择。 |
| 回流 | 材料、成长钥匙、房间记忆、订单奖励 | 是否能进入局外成长、经济、叙事或关系系统。 |
| 目标 | 订单、传闻、Boss 保底、层级直达 | 是否给下一次下潜明确方向。 |

---

## 4. 固定 Seed 组

### 4.1 Seed 摘要字段

每条固定 seed 至少输出或人工记录以下字段：

| 字段 | 要求 |
|---|---|
| `SeedID` | 稳定 ID，例如 `V-L1-SEED-1024-ROUTE-01`。 |
| `LayerID` | 目标层级。 |
| `RunSeed` | 固定随机种子。 |
| `MapProfileID` | 使用的地图画像。 |
| `RouteSummary` | 节点序列、分叉、Boss、SafeZone 和 Stairs。 |
| `EncounterSummary` | 关键怪物、精英、Boss 或事件。 |
| `RewardSummary` | 关键掉落、Boss 保底、订单奖励或房间记忆钩子。 |
| `ExpectedPlayerChoice` | 玩家应做出的路线、背包或订单取舍。 |
| `PassCriteria` | 通过条件。 |
| `FailureSignals` | 失败信号。 |

### 4.2 第一层 seed

| SeedID | 建议参数 | 覆盖任务 | 目标路径 | 通过条件 | 失败信号 |
|---|---|---|---|---|---|
| `V-L1-SEED-1024-ROUTE-01` | `LayerID=1`、`RunSeed=1024`、`MapProfileID=layer1_tutorial_branching` | `L1-DUNGEONS`、`L1-SEED` | 普通战斗 -> 宝箱 / 事件分叉 -> 可绕精英 -> Boss -> Boss 后 SafeZone。 | Boss 可达；精英可绕；Boss 后免费恢复；可撤离或继续深入。 | 单线推进；Boss 后无 SafeZone；精英不可绕；奖励引用缺失。 |
| `V-L1-PACK-RUN-01` | `LayerID=1`、`RunSeed=1024` | `L1-ITEMS`、`L1-REWARDS` | 拾取武器 / 材料 / 交易物，触发至少一次旋转或舍弃判断。 | 物品形状、旋转、来源去向可读；带回后能出售或作为材料。 | 掉落物无形状；非法放置无反馈；材料无局外去向。 |
| `V-L1-PACK-FAIL-01` | `LayerID=1`、`RunSeed=1101`、`MapProfileID=layer1_event_miner_remains` | `L1-ITEMS`、`L1-DUNGEONS` | 事件获得高价值物，随后战败或撤离复核生命周期。 | 新获物、绑定物、材料按规则分流；战败原因可复盘。 | 全部无差别保留或丢失；事件物无生命周期。 |

### 4.3 第二层 seed

| SeedID | 建议参数 | 覆盖任务 | 目标路径 | 通过条件 | 失败信号 |
|---|---|---|---|---|---|
| `V-L2-DIRECT-2048-ROUTE-01` | `LayerID=2`、`RunSeed=2048`、`MapProfileID=layer2_bag_pressure` | `L2-DUNGEONS`、`L2-SEED` | 小镇直接进入第二层 -> 背包压力路线 -> Boss -> Boss 后 SafeZone。 | 不重新生成第一层；二层节点池、Boss 和 SafeZone 生效。 | 需要重打一层；二层 Boss 不可达；安全区清空订单状态。 |
| `V-L2-PACK-RUN-01` | `LayerID=2`、`RunSeed=2048` | `L2-ITEMS`、`L2-MONSTERS` | 遇到对包干涉怪，获得反制物或风险材料。 | 至少出现塞包 / 封格 / 腐蚀 / 污染中的两类压力；有反制窗口。 | 怪物永久毁物；反制物无效果；玩家不知道格子为何不可用。 |
| `V-L2-PACK-ORDER-01` | `LayerID=2`、`RunSeed=2202`、`MapProfileID=layer2_order_spore_hunt` | `L2-ORDERS`、`L2-REWARDS` | 接取占格订单 -> 下潜获得 `order_live_spore_cage` -> 回城交付。 | 订单物占格且不可普通出售；交付奖励引用可解析；失败 / 放弃代价可读。 | 订单只是文本；目标物可卖掉且订单不失败；奖励断链。 |
| `V-L2-PACK-BOSS-01` | `LayerID=2`、`RunSeed=2048` | `L2-REWARDS`、`L2-DUNGEONS` | 击败二层 Boss 并复核二阶成长材料保底。 | `reward_boss_spore_foundry` 保底二阶成长材料；精英不承担保底。 | Boss 掉落空池；二阶材料来自精英保底。 |

### 4.4 第三层 seed

| SeedID | 建议参数 | 覆盖任务 | 目标路径 | 通过条件 | 失败信号 |
|---|---|---|---|---|---|
| `V-L3-DIRECT-3072-ROUTE-01` | `LayerID=3`、`RunSeed=3072`、`MapProfileID=layer3_route_attrition` | `L3-DUNGEONS`、`L3-SEED` | 小镇直接进入第三层 -> 路线侵蚀分叉 -> Boss。 | 不重新生成一二层；路线侵蚀变量随路线累积；Boss 入场受变量影响。 | 仍需打前置层；路线侵蚀不影响任何结果；变量永久污染存档。 |
| `V-L3-ATTRITION-01` | `LayerID=3`、`RunSeed=3072` | `L3-DUNGEONS`、`L3-MONSTERS` | 连续选择高收益路线，承受 SAN / 污染 / 路线压力。 | 风险收益可读；压力来自节点、怪物或事件组合。 | 高收益路线无代价；压力不可复盘。 |
| `V-L3-CORRUPT-ITEM-01` | `LayerID=3`、`RunSeed=3072` | `L3-ITEMS`、`L3-EVENTS-RUMORS-MEMORY` | 带回污染物，回城后触发处理提示或房间记忆。 | 污染物有隔夜规则、风险提示和处理去向。 | 污染物只是标签；隔夜无结果；房间 / 事件无反馈。 |
| `V-L3-ORDER-CONFLICT-01` | `LayerID=3`、`RunSeed=3303`、`MapProfileID=layer3_order_conflict_core` | `L3-ORDERS`、`L3-REWARDS` | 获得争夺孢核，在工坊 / 法师塔 / 黑市三方中只能选择一方。 | 互斥提交生效；奖励和声望 / 背叛标记可解析。 | 同一物品可三方重复交付；黑市无风险标记。 |
| `V-L3-BOSS-REWARD-01` | `LayerID=3`、`RunSeed=3072` | `L3-REWARDS`、`L3-DUNGEONS` | 击败三层 Boss 并复核三阶成长钥匙保底。 | `reward_boss_mycelium_oracle` 保底 `mat_core_tier3_seed`；Boss 后 SafeZone 可用。 | 三阶材料由精英或宝箱保底；Boss 后无恢复。 |

---

## 5. 验收批次

### 5.1 第一批：第一层模板闭合

执行顺序：

```text
L1-ITEMS -> L1-REWARDS -> L1-MONSTERS -> L1-DUNGEONS -> L1-SEED
```

最低通过标准：

1. `CFG-ITEM-ROLE-001`、`CFG-REWARD-REF-001`、`CFG-MONSTER-REF-001`、`CFG-DUNGEON-REF-001` 无 error。
2. `V-L1-SEED-1024-ROUTE-01` 可复现 Boss 和 Boss 后 SafeZone。
3. 至少一个拾取路径能触发背包旋转或舍弃。

### 5.2 第二批：第二层背包压力

执行顺序：

```text
L2-ITEMS -> L2-REWARDS -> L2-MONSTERS -> L2-DUNGEONS -> L2-ORDERS -> L2-SEED
```

最低通过标准：

1. `CFG-LAYER-DIRECT-001` 证明第二层直达不重打一层。
2. `CFG-ORDER-CONFLICT-001` 证明订单物绑定、交付和奖励引用可解析。
3. `V-L2-PACK-RUN-01` 至少覆盖一种对包干涉和一种反制方式。

### 5.3 第三批：第三层路线侵蚀

执行顺序：

```text
L3-ITEMS -> L3-REWARDS -> L3-MONSTERS -> L3-DUNGEONS -> L3-ORDERS -> L3-EVENTS-RUMORS-MEMORY -> L3-SEED
```

最低通过标准：

1. `CFG-ROUTE-CORROSION-001` 证明路线侵蚀只属于本次下潜，并影响 Boss 或节点结果。
2. `CFG-ORDER-CONFLICT-001` 证明三势力互斥订单不可重复交付。
3. `V-L3-CORRUPT-ITEM-01` 证明污染物有局外处理结果。
4. `V-L3-BOSS-REWARD-01` 证明三阶成长钥匙由 Boss 保底。

---

## 6. 回写证据模板

后续执行配置任务后，建议在提交说明、状态页或任务记录中使用以下证据格式：

```text
TaskID:
Config files:
Changed IDs:
Reference check:
Sync result:
Validator result:
Seed result:
Manual acceptance:
Status writeback:
Known gaps:
```

示例：

```text
TaskID: L1-REWARDS
Config files: 配置表(JSON)/Rewards/layer1_rewards.json
Changed IDs: reward_boss_gatekeeper_mk1, reward_node_treasure_layer1
Reference check: all ItemID resolved; mat_core_tier1 moved to boss guarantee
Sync result: Sync-Configs.ps1 -Clean passed
Validator result: CFG-REWARD-REF-001 passed; CFG-REWARD-BOSS-001 passed
Seed result: V-L1-SEED-1024-ROUTE-01 reward summary stable
Manual acceptance: boss reward visible after victory
Status writeback: agent_status/design.md updated to 验收中; notify PM to update 09 only if long-term gate changes
Known gaps: none
```

---

## 7. 完成判定

前三层正式配置验收设计完成，不代表 JSON 已完成。只有后续真实配置实现同时满足以下条件，才能把对应任务标为 `配置完成` 或 `验收中`：

1. 对应 `Lx-*` 任务有配置源改动和 ID 清单。
2. 主干 Validator 无 error。
3. 固定 seed 覆盖该任务声称覆盖的玩家路径。
4. Boss 保底、安全区、层级直达、订单互斥、污染物去向等关键规则没有被人工解释替代。
5. 失败信号已经写入报告、状态页或任务记录。
6. `agent_status/design.md` 已更新策划 / 配置状态和证据；如影响长期节点门禁，PM 已同步 `09`。

如果 Validator 尚未具备某项能力，可以把该项标为“等待程序工具补强”，但不能因此把配置任务标为已完成。若只能人工复核，应明确人工复核步骤、复核人、复核日期和未覆盖风险。
