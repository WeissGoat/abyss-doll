---
id: design_45_outgame_growth_validator_acceptance
title: 局外成长正式配置Validator与固定验收样例
type: acceptance_spec
role: 策划
domain: formal_config_acceptance
status: active
source_of_truth: true
related:
  - PROJECT_STATUS.md
  - 设计文档/README.md
  - 设计文档/delivery/13_策划跨系统验收场景矩阵.md
  - 设计文档/content_packs/21_人偶成长修复内容包.md
  - 设计文档/config/26_正式配置设计与填充推进计划.md
  - 设计文档/config/audits/36_局外成长正式配置承接审计.md
  - 设计文档/config/designs/37_局外成长正式配置落地设计.md
  - 设计文档/config/tasks/38_局外成长正式配置实现任务拆分.md
  - 设计文档/config/gates/39_局外成长正式配置ID锁定与冲突检查.md
  - 设计文档/config/designs/40_局外成长底盘正式配置落地设计.md
  - 设计文档/config/designs/41_局外成长效果特质正式配置落地设计.md
  - 设计文档/config/designs/42_局外成长义体正式配置落地设计.md
  - 设计文档/config/designs/43_局外成长制造维护正式配置落地设计.md
  - 设计文档/config/designs/44_局外成长人偶特质房间正式配置落地设计.md
  - 开发文档/15_P0配置Validator与自动验收底座需求.md
  - 配置表(JSON)/Chassis/README.md
  - 配置表(JSON)/Prosthetics/README.md
  - 配置表(JSON)/CraftingRecipes/README.md
  - 配置表(JSON)/Dolls/README.md
  - 配置表(JSON)/Effects/README.md
  - agent_status/design.md
last_verified: 2026-05-25
update_rule: 调整 GROWTH-VALIDATION 的 Validator 覆盖、固定验收样例、失败信号、证据模板或台账回写口径时同步本文件。
---

# 局外成长正式配置Validator与固定验收样例

> **定位：** 本文承接 `38_局外成长正式配置实现任务拆分.md` 的 `GROWTH-VALIDATION`。它是策划侧验收规格，不直接修改 `配置表(JSON)`，不代表局外成长 JSON 已完成。它说明后续底盘、效果 / 特质、义体、制造 / 维护、人偶 / 反馈 / 房间配置源落地后，应如何用 Validator、固定样例和人工复核证明配置可用。

---

## 1. 使用边界

本文回答三个问题：

1. `40` 到 `44` 的字段级设计真正落到 JSON 后，哪些检查项必须通过。
2. 哪些固定样例能证明局外成长形成“带回材料 -> 制造 / 维护 -> 下潜许可 -> 再次下潜”的闭环。
3. 哪些证据可以回写 `agent_status/design.md` 的 `DES-V3-002`，哪些情况仍必须判为未完成；如影响长期节点门禁，再通知 PM 更新 `09`。

本文不定义：

| 不定义项 | 归属 |
|---|---|
| 具体底盘、义体、效果、配方、维护、人偶、特质、反馈、纪念物字段清单 | `40` / `41` / `42` / `43` / `44` |
| 配置实现任务顺序和派发边界 | `38` |
| Validator 程序入口、报告格式和命令参数 | `开发文档/15_P0配置Validator与自动验收底座需求.md` |
| 运行时 UI、美术资源和截图验收 | 美术文档、开发文档和 ArtAcceptance |
| 月租、订单、声望、传闻价格波完整经济配置 | 后续经济压力配置任务 |

---

## 2. 验收总原则

局外成长配置验收必须同时证明静态引用、运行时语义和玩家路径。

| 验收层 | 证明内容 | 不足时的风险 |
|---|---|---|
| 静态 Validator | 字段存在、枚举合法、ID 引用不断、旧 ID 迁移清楚、运行时字段不混入静态配置。 | JSON 能加载，但后续制造、安装、维护、人偶反馈或房间记忆断链。 |
| 固定样例 | 同一初始状态下，制造、安装、维护、特质、反馈和房间记忆能复现。 | 配置随机可用，但不能证明成长闭环稳定。 |
| 人工体验复核 | 玩家能读懂成长方向、材料缺口、维护代价、下潜许可和反馈结果。 | 工具通过，但体验仍像字段堆叠。 |

配置实现任务不能只凭“JSON 已修改”判定完成。最低证据应包含：

1. 配置 ID 清单。
2. 引用检查结果。
3. `.\tools\config\Sync-Configs.ps1 -Clean` 结果或未运行原因。
4. Validator 报告或等待程序工具补强的明确缺口。
5. 固定样例摘要或人工验收记录。
6. `agent_status/design.md` 对应策划 / 配置状态更新；如影响长期节点门禁，PM 同步 `09`。

---

## 3. Validator 覆盖矩阵

### 3.1 底盘

| ValidatorID | 覆盖 | 等级 | 检查目标 | 失败信号 |
|---|---|---|---|---|
| `CFG-CHASSIS-001` | `GROWTH-CHASSIS` | error | `ChassisID`、`DisplayName`、`DescriptionKey`、`ChassisRole`、`GridWidth`、`GridHeight`、`GridMask` 必填。 | 底盘只有旧 `Level` / `UpgradeCost`。 |
| `CFG-CHASSIS-002` | `GROWTH-CHASSIS` | error | `GridMask` 与宽高一致，至少有 1 个可用格。 | 背包网格无法生成或格子越界。 |
| `CFG-CHASSIS-003` | `GROWTH-CHASSIS` | error | `ChassisRole` 非空，且能解释构筑职责。 | 轻装 / 重载只表现为数值更高。 |
| `CFG-CHASSIS-004` | `GROWTH-CHASSIS` | error | `AllowedProstheticSlots` 来自正式槽位枚举，且每个正式义体至少被一个底盘允许或兼容允许。 | 义体无可安装底盘。 |
| `CFG-CHASSIS-005` | `GROWTH-CHASSIS` | warning | `VisualID` 存在或明确 fallback。 | 工坊展示缺图且无 fallback。 |
| `CFG-CHASSIS-006` | `GROWTH-CHASSIS` | warning / error | 解锁、图纸、成本和材料引用存在；暂未落地来源时必须 warning。 | 直接引用不存在材料并静默通过。 |
| `CFG-CHASSIS-007` | `GROWTH-CHASSIS` | error | `AdaptationDays >= 0`、`MaintenanceModifier > 0`；强底盘必须有代价。 | 重载底盘无维护、适应期或风险。 |
| `CFG-CHASSIS-008` | `GROWTH-CHASSIS` | warning | 旧 `chassis_lv*` ID 只作为兼容别名或测试 ID，不作为正式主输出。 | 配方输出旧底盘 ID。 |

### 3.2 效果 / 标签 / 特质

| ValidatorID | 覆盖 | 等级 | 检查目标 | 失败信号 |
|---|---|---|---|---|
| `CFG-EFFECT-001` | `GROWTH-EFFECTS-TRAITS` | error | `EffectID`、`Type`、`TriggerTiming`、`AttachableScope`、`ConfigSchema` 必填。 | 义体引用的效果无法解释参数。 |
| `CFG-EFFECT-002` | `GROWTH-EFFECTS-TRAITS` | error | 引用效果时参数数量、类型和值域与 schema 一致。 | 参数拼错或值域越界仍通过。 |
| `CFG-EFFECT-003` | `GROWTH-EFFECTS-TRAITS` | warning / error | 影响核心决策的效果必须玩家可见，或有反馈引用。 | 路线预览、背包抗干涉、SAN 调节静默发生。 |
| `CFG-TAG-001` | `GROWTH-EFFECTS-TRAITS` | error | `TagID`、`TagGroup`、`DurationType`、`StackPolicy`、`ClearRule` 必填且合法。 | 动态标签永久残留。 |
| `CFG-TAG-002` | `GROWTH-EFFECTS-TRAITS` | error | `UntilMaintenance` 类标签必须至少被维护、事件或转化规则处理。 | 污染、疲劳或边缘焦虑无法解除。 |
| `CFG-TRAIT-001` | `GROWTH-EFFECTS-TRAITS` | error | 特质必须有获得条件、生效规则、反馈和 `TransformRule`。 | 特质只有名字和描述。 |
| `CFG-TRAIT-002` | `GROWTH-EFFECTS-TRAITS` | error | 负面特质必须有转化、消除或压制路径。 | 负面特质造成永久死锁。 |
| `CFG-TRAIT-003` | `GROWTH-EFFECTS-TRAITS` | error | 同一 `ConflictGroup` 不能多个当前有效特质无限叠加。 | 信任类、孢雾创伤类特质同时生效且重复惩罚。 |
| `CFG-TRAIT-004` | `GROWTH-EFFECTS-TRAITS` | warning / error | 特质引用的 `EffectID`、`TagID`、`FeedbackID` 必须存在或有占位 warning。 | 特质触发后找不到反馈或效果。 |
| `CFG-TRAIT-005` | `GROWTH-EFFECTS-TRAITS` | error | 正面特质必须有触发上限、唯一策略或叠加上限。 | 正面特质无限收益。 |

### 3.3 义体

| ValidatorID | 覆盖 | 等级 | 检查目标 | 失败信号 |
|---|---|---|---|---|
| `CFG-PROSTHETIC-001` | `GROWTH-PROSTHETICS` | error | `ProstheticID`、`DisplayName`、`DescriptionKey`、`VisualID`、`Tier`、`SlotType`、`Tags` 必填。 | 义体只有旧 `Name`、`Level` 和泛化槽位。 |
| `CFG-PROSTHETIC-002` | `GROWTH-PROSTHETICS` | warning / error | `SlotType` 必须被底盘允许；通过 `SlotCompatibilityAliases` 命中给 warning；完全未命中给 error。 | `RightHand` 无兼容口径。 |
| `CFG-PROSTHETIC-003` | `GROWTH-PROSTHETICS` | error | `EffectRefs` 非空，每个效果存在且参数合法。 | 义体只写战斗加成，没有触发效果。 |
| `CFG-PROSTHETIC-004` | `GROWTH-PROSTHETICS` | error | `TriggerRules` 声明触发点、次数、冷却和失败处理。 | 义体每次都无限触发。 |
| `CFG-PROSTHETIC-005` | `GROWTH-PROSTHETICS` | warning / error | `CraftRecipeRef` 存在或明确列为后续 `GROWTH-CRAFT-MAINT` warning。 | 义体无法制造或来源不明。 |
| `CFG-PROSTHETIC-006` | `GROWTH-PROSTHETICS` | error | `ConflictGroup` 不得自冲突，同槽同组不能同时生效。 | 同一槽位重复装备。 |
| `CFG-PROSTHETIC-007` | `GROWTH-PROSTHETICS` | error | 高收益义体必须有维护、磨损、侵蚀、冷却、材料成本或槽位冲突。 | 强义体没有长期代价。 |
| `CFG-PROSTHETIC-008` | `GROWTH-PROSTHETICS` | warning | 旧 `pros_*` ID 不作为正式主 ID；保留时必须出现在兼容清单。 | 正式配方输出旧 `pros_*`。 |

### 3.4 配方 / 维护

| ValidatorID | 覆盖 | 等级 | 检查目标 | 失败信号 |
|---|---|---|---|---|
| `CFG-CRAFT-001` | `GROWTH-CRAFT-MAINT` | error | `RecipeID`、`RecipeType`、`DisplayName`、`DescriptionKey`、`GoldCost`、`ConsumePolicy`、`CancelPolicy` 必填。 | 配方只有输入 / 输出，没有消耗和取消策略。 |
| `CFG-CRAFT-002` | `GROWTH-CRAFT-MAINT` | error | 每个配方必须且只能声明一种主输出。 | 同一配方同时输出物品和义体。 |
| `CFG-CRAFT-003` | `GROWTH-CRAFT-MAINT` | warning / error | `RequiredItems.ConfigID` 必须存在；待补材料必须 warning。 | 不存在材料静默通过。 |
| `CFG-CRAFT-004` | `GROWTH-CRAFT-MAINT` | warning / error | `OutputProstheticID` 必须存在于 `42` 或实际 JSON；旧输出只能 warning。 | 配方制造不存在义体。 |
| `CFG-CRAFT-005` | `GROWTH-CRAFT-MAINT` | warning / error | `OutputChassisUnlock` 必须存在于 `40` 或实际 JSON。 | 底盘解锁输出断链。 |
| `CFG-CRAFT-006` | `GROWTH-CRAFT-MAINT` | error | `ConsumePolicy` 保护订单绑定、剧情锁定和情感锚点物。 | 制造消耗订单物或纪念物。 |
| `CFG-MAINT-001` | `GROWTH-CRAFT-MAINT` | error | `MaintenanceID`、`DisplayName`、`DescriptionKey`、`TargetStates`、`EffectRefs`、`GoldCost`、`DiveReadinessImpact` 必填。 | 维护只写恢复数值，不能解释下潜许可。 |
| `CFG-MAINT-002` | `GROWTH-CRAFT-MAINT` | warning / error | `EffectRefs.EffectID` 存在于 `41` 或实际 JSON。 | 维护引用不存在效果。 |
| `CFG-MAINT-003` | `GROWTH-CRAFT-MAINT` | error | 维护不能免费清除 HP、SAN、Wear、Corruption、Broken、订单、污染物和特质历史。 | 维护变成一键全恢复。 |
| `CFG-MAINT-004` | `GROWTH-CRAFT-MAINT` | warning / error | `TraitTransformRefs` 引用的特质有转化 / 压制路径。 | 深层安抚无法推进特质。 |
| `CFG-MAINT-005` | `GROWTH-CRAFT-MAINT` | error | `RestoresDivePermit=true` 时声明恢复后的最低下潜许可边界。 | 维护 ID 强行通过下潜许可。 |
| `CFG-MAINT-006` | `GROWTH-CRAFT-MAINT` | error | 已存在维护 ID 不能重复创建；已有 ID 只能补字段。 | `maint_basic_patch` 被重复定义。 |

### 3.5 人偶 / 反馈 / 房间记忆

| ValidatorID | 覆盖 | 等级 | 检查目标 | 失败信号 |
|---|---|---|---|---|
| `CFG-DOLL-001` | `GROWTH-DOLL-TRAIT-ROOM` | error | `DollID`、`DisplayName`、`BaseMaxHP`、`BaseMaxSAN`、`DefaultChassisID`、`FallbackVisualID` 必填。 | 人偶静态档案缺默认底盘或 fallback。 |
| `CFG-DOLL-002` | `GROWTH-DOLL-TRAIT-ROOM` | error | 静态 `Dolls` 不得包含当前 HP / SAN / Bond / 磨损 / 侵蚀等运行时状态。 | 运行时状态混进静态配置。 |
| `CFG-DOLL-003` | `GROWTH-DOLL-TRAIT-ROOM` | error / warning | `DefaultChassisID` 引用存在底盘；旧 ID 只能作为迁移 warning。 | 默认底盘仍指向旧 `chassis_lv1_basic` 且无迁移。 |
| `CFG-DOLL-004` | `GROWTH-DOLL-TRAIT-ROOM` | warning / error | `InitialItems` 不作为正式静态字段，正式档案引用 `InitialLoadoutRef`。 | 测试初始装备计入正式完成。 |
| `CFG-DOLL-005` | `GROWTH-DOLL-TRAIT-ROOM` | warning / error | 反馈、交互、房间引用存在或有 fallback。 | 人偶交互反馈断链。 |
| `CFG-DOLL-006` | `GROWTH-DOLL-TRAIT-ROOM` | warning | 表情、动作和房间表现有 fallback。 | 表现资源缺失且无替代。 |
| `CFG-TRAIT-006` | `GROWTH-DOLL-TRAIT-ROOM` | error | 8 个首批 `TraitID` 不重复，字段齐全。 | 特质重复或缺触发。 |
| `CFG-TRAIT-007` | `GROWTH-DOLL-TRAIT-ROOM` | error | 负面特质 `TransformRule` 可追到维护、事件或成长路径。 | 负面特质无法恢复。 |
| `CFG-TRAIT-008` | `GROWTH-DOLL-TRAIT-ROOM` | warning / error | 特质引用的 `EffectID`、`TagID`、`FeedbackID` 存在或有占位 warning。 | 特质引用断链。 |
| `CFG-TRAIT-009` | `GROWTH-DOLL-TRAIT-ROOM` | error | 同一 `ConflictGroup` 的当前有效特质不能无限叠加。 | 同类惩罚重复叠加。 |
| `CFG-TRAIT-010` | `GROWTH-DOLL-TRAIT-ROOM` | error | 正面特质有触发上限或唯一策略。 | 正面特质无限收益。 |
| `CFG-FEEDBACK-001` | `GROWTH-DOLL-TRAIT-ROOM` | error | `FeedbackID`、`FeedbackType`、`TriggerCondition`、`ResultPolicy` 必填。 | 反馈只有文案，没有裁决。 |
| `CFG-FEEDBACK-002` | `GROWTH-DOLL-TRAIT-ROOM` | error | 低 SAN 抗拒类反馈不得增加 Bond。 | 低 SAN 强摸仍涨好感。 |
| `CFG-FEEDBACK-003` | `GROWTH-DOLL-TRAIT-ROOM` | error | 赠礼反馈声明接受 / 拒收和消耗策略。 | 礼物既保留又结算。 |
| `CFG-FEEDBACK-004` | `GROWTH-DOLL-TRAIT-ROOM` | error | 安全区反馈不得重复结算 HP / SAN 全恢复。 | 安全区二次恢复。 |
| `CFG-FEEDBACK-005` | `GROWTH-DOLL-TRAIT-ROOM` | warning | 反馈资源缺失时有 fallback。 | 缺文案或表现导致空反馈。 |
| `CFG-ROOM-MEMORY-001` | `GROWTH-DOLL-TRAIT-ROOM` | error | `MementoID`、触发条件、房间、槽位和优先级必填。 | 纪念物不能稳定展示。 |
| `CFG-ROOM-MEMORY-002` | `GROWTH-DOLL-TRAIT-ROOM` | error | 纪念物声明 `InventoryLinkPolicy`，不得复制物品。 | 房间记忆变成物品复制来源。 |
| `CFG-ROOM-MEMORY-003` | `GROWTH-DOLL-TRAIT-ROOM` | error | 纪念物触发条件可解析。 | Boss / 战败 / 赠礼经历无法生成记忆。 |
| `CFG-ROOM-MEMORY-004` | `GROWTH-DOLL-TRAIT-ROOM` | error | 同一槽位多个纪念物有稳定优先级。 | 房间展示不稳定。 |
| `CFG-ROOM-MEMORY-005` | `GROWTH-DOLL-TRAIT-ROOM` | warning | 视觉资源缺失时有日记或 fallback。 | 记忆触发但玩家看不到。 |

---

## 4. 固定验收样例组

### 4.1 样例摘要字段

每条固定样例至少输出或人工记录：

| 字段 | 要求 |
|---|---|
| `AcceptanceID` | 稳定 ID，例如 `V-PACK-DOLL-GROWTH-01`。 |
| `InitialState` | 人偶状态、底盘、义体、材料、金币、已通层、订单或房间状态。 |
| `ConfigScope` | 覆盖的配置域和任务 ID。 |
| `ActionPath` | 玩家操作路径。 |
| `ExpectedResult` | 通过条件。 |
| `FailureSignals` | 失败信号。 |
| `Evidence` | Validator、同步、运行时截图、人工复核或等待工具补强说明。 |

### 4.2 核心闭环样例

| AcceptanceID | 覆盖任务 | InitialState | ActionPath | ExpectedResult | FailureSignals |
|---|---|---|---|---|---|
| `V-PACK-DOLL-RECOVER-01` | `GROWTH-CRAFT-MAINT`、`GROWTH-DOLL-TRAIT-ROOM` | 战败后 HP / SAN / Wear 受损，拥有基础废料和金币。 | 执行 `maint_basic_patch` -> 安抚反馈 -> 下潜许可检查。 | HP / Wear 改善，金币 / 材料扣除；下潜许可重新判断；反馈不重复结算恢复。 | 维护免费全清；低 SAN 触摸涨 Bond；维护 ID 强行通过下潜许可。 |
| `V-PACK-DOLL-GROWTH-01` | `GROWTH-CHASSIS`、`GROWTH-PROSTHETICS`、`GROWTH-CRAFT-MAINT` | 已通第二层，拥有二层材料和金币。 | 制造 `prosthetic_anchor_left_arm` -> 安装到允许底盘 -> 进入第二层背包干涉战。 | 材料 / 金币扣除；义体槽位合法；首次背包干涉降低；磨损或维护代价生效。 | 义体无配方；槽位无底盘允许；背包干涉被完全免疫或无反馈。 |
| `V-PACK-DOLL-EMOTION-01` | `GROWTH-EFFECTS-TRAITS`、`GROWTH-CRAFT-MAINT`、`GROWTH-DOLL-TRAIT-ROOM` | 获得 `trait_fear_of_spores`，带有 `tag_corrosion_taint`。 | 执行 `maint_purification_flush` -> 击败对应精英 -> 回城查看状态。 | 污染降低，特质被转化或压制，历史保留，玩家能复盘来源。 | 负面特质永久死锁；净化顺手清掉磨损 / 订单 / 全部历史。 |
| `V-PACK-DOLL-ROOM-01` | `GROWTH-DOLL-TRAIT-ROOM` | 第一层 Boss 胜利后赠送矿灯。 | 回城进入房间 -> 查看纪念物 / 日记。 | 生成 `memento_boss1_lamp`，房间显示或日记记录经历，不复制物品。 | 纪念物变成物品复制；资源缺失无 fallback；触发条件不稳定。 |
| `V-PACK-DOLL-DIVE-01` | `GROWTH-CHASSIS`、`GROWTH-PROSTHETICS`、`GROWTH-CRAFT-MAINT` | 更换 `chassis_bulwark_carrier` 并安装重载义体，处于适应期。 | 执行下潜许可检查 -> 进入第二层或第三层。 | 下潜许可读取底盘 / 义体 / HP / SAN / Wear / Corruption；适应期产生提示或代价。 | 更换底盘后无检查；适应期不存在；非法义体被静默销毁。 |

### 4.3 模块级补充样例

| AcceptanceID | 覆盖 | 通过标准 |
|---|---|---|
| `V-GROWTH-CHASSIS-01` | 默认底盘 | 初始人偶解析 `chassis_standard_frame`，旧 `chassis_lv1_basic` 只作为迁移 warning。 |
| `V-GROWTH-CHASSIS-02` | 轻装底盘 | `chassis_compact_raider` 解锁 / 安装后背包或形状变化，路线提示可见，适应期 1 天生效。 |
| `V-GROWTH-CHASSIS-03` | 重载底盘 | `chassis_bulwark_carrier` 提供承载优势，同时维护倍率和适应期代价生效。 |
| `V-GROWTH-EFFECT-01` | 路线预览效果 | `effect_route_preview_hint` 只显示相邻倾向，不完全透视地图。 |
| `V-GROWTH-EFFECT-02` | SAN 调节效果 | 大额 SAN 损失被部分削减，并附加 `tag_san_regulation_fatigue`。 |
| `V-GROWTH-TAG-01` | 污染标签 | `tag_corrosion_taint` 能被净化处理，但不顺手清掉所有状态。 |
| `V-GROWTH-PROSTHETIC-01` | 地图阅读义体 | `prosthetic_focus_lens` 装备后路线预览出现；无 UI 支持时报告 warning。 |
| `V-GROWTH-PROSTHETIC-02` | 背包抗干涉义体 | `prosthetic_anchor_left_arm` 降低首次干涉，不免疫后续压力。 |
| `V-GROWTH-PROSTHETIC-05` | 拾荒指 | `prosthetic_salvage_fingertips` 只在撤离结算提供材料识别 / 拆解，不提供战斗收益。 |
| `V-GROWTH-CRAFT-01` | 义体制造 | 制造锚定左臂扣除材料和金币，输出正式义体 ID。 |
| `V-GROWTH-CRAFT-03` | 待补材料来源 | `prosthetic_mender_spine` 若引用未实现材料，Validator warning 或阻止配置完成。 |
| `V-GROWTH-MAINT-01` | 基础维护 | `maint_basic_patch` 恢复 HP / Wear，但不清 Corruption、Broken 和特质历史。 |
| `V-GROWTH-DOLL-01` | 人偶静态档案 | `Dolls` 读取基础上限、默认底盘、房间和 fallback，不读取当前 HP / SAN。 |
| `V-GROWTH-FEEDBACK-01` | 低 SAN 反馈 | 低 SAN 触摸脸部触发抗拒反馈，不增加 Bond。 |
| `V-GROWTH-ROOM-01` | Boss 纪念物 | Boss 胜利和赠礼生成房间纪念物或日记，不复制物品。 |

---

## 5. 批次验收顺序

### 5.1 第一批：静态引用闭合

执行顺序：

```text
ID lock -> Chassis -> Effects / Tags / Traits -> Prosthetics -> Crafting / Maintenance -> Dolls / Feedback / RoomMemory
```

最低通过标准：

1. 所有新增正式 ID 无重复。
2. 旧 MVP ID 均有迁移、兼容或测试边界。
3. `Chassis`、`Prosthetics`、`CraftingRecipes`、`Dolls`、`Effects` 之间的主引用不断。
4. 待补材料、待补 UI、待补程序字段必须以 warning 记录，不能静默通过。

### 5.2 第二批：成长链路闭合

执行顺序：

```text
材料来源 -> 配方制造 -> 义体安装 -> 维护代价 -> 下潜许可
```

最低通过标准：

1. 至少 1 个底盘、1 个义体、1 个配方、1 个维护方案能形成完整路径。
2. 制造和维护不会复制材料、吞掉订单物或清除情感锚点物。
3. 下潜许可不是简单读维护 ID，而是读取 HP / SAN / Wear / Corruption / 底盘 / 义体状态。
4. `V-PACK-DOLL-GROWTH-01` 和 `V-PACK-DOLL-DIVE-01` 有证据。

### 5.3 第三批：情感与长期记忆闭合

执行顺序：

```text
战败 / 高压经历 -> 特质 / 标签 -> 维护 / 安抚 -> 反馈 -> 房间记忆
```

最低通过标准：

1. 至少 1 个负面特质有来源、影响、转化 / 压制路径和历史保留。
2. 低 SAN、维护、安全区、赠礼或 Boss 胜利至少各有一种反馈落点或 fallback。
3. 房间纪念物不复制物品，不成为仓库。
4. `V-PACK-DOLL-EMOTION-01` 和 `V-PACK-DOLL-ROOM-01` 有证据。

---

## 6. Warning / Error 分级

| 情况 | 等级 | 原因 |
|---|---|---|
| ID 重复、必填字段缺失、引用不存在、运行时字段混入静态配置 | error | 会导致配置不可用或语义错误。 |
| 负面特质无转化、维护全清所有状态、下潜许可强行通过 | error | 会破坏正式版规则和长期循环。 |
| 配方消耗订单绑定物、纪念物复制物品、低 SAN 抗拒仍涨 Bond | error | 会破坏玩家承诺。 |
| VisualID、UI 表达、程序字段暂未支持但有 fallback 或占位说明 | warning | 不阻止策划配置设计，但不能宣称表现 / 程序完成。 |
| 待补材料来源、待补图纸、待补房间资源有明确后续任务 | warning | 可进入配置实现观察，但不能标记最终配置完成。 |
| 旧 ID 只作为兼容别名或测试 fixture | warning | 允许迁移期存在，但不能作为正式主 ID。 |

进入 Strict 或候选版前，warning 必须被处理为：

1. 已补配置 / 程序 / 美术资源。
2. 明确降级为可接受 fallback。
3. 移出当前正式完成口径。

---

## 7. 回写证据模板

后续执行 `GROWTH-VALIDATION` 或 JSON 实现后，建议使用以下格式回写状态页、提交说明或任务记录：

```text
TaskID:
Config files:
Changed IDs:
Reference check:
Sync result:
Validator result:
Acceptance samples:
Manual review:
Status writeback:
Known gaps:
```

示例：

```text
TaskID: GROWTH-CRAFT-MAINT
Config files: 配置表(JSON)/CraftingRecipes/*.json
Changed IDs: craft_prosthetic_anchor_left_arm, maint_basic_patch
Reference check: OutputProstheticID resolved; RequiredItems resolved except mat_layer2_elite_plate warning
Sync result: Sync-Configs.ps1 -Clean passed
Validator result: CFG-CRAFT-001/002/004 passed; CFG-CRAFT-003 warning for pending material
Acceptance samples: V-GROWTH-CRAFT-01 passed; V-GROWTH-MAINT-01 passed
Manual review: maintenance does not clear Corruption/Broken/trait history
Status writeback: agent_status/design.md DES-V3-002 remains 进行中; evidence appended; notify PM to update 09 only if long-term gate changes
Known gaps: material source requires L2 item implementation
```

---

## 8. 完成判定

本文完成后，只能把 `GROWTH-VALIDATION` 记录为策划验收规格完成，不代表局外成长配置完成。

`DES-V3-002` 只有在后续真实配置实现同时满足以下条件后，才能从 `进行中` 推进为 `配置完成` 或 `验收中`：

1. `40` / `41` / `42` / `43` / `44` 中的目标配置源均已落到 `配置表(JSON)` 或明确等价结构。
2. `.\tools\config\Sync-Configs.ps1 -Clean` 成功，运行时副本不是手写维护。
3. 本文列出的主干 Validator 无 error。
4. warning 有清单、归属和处理计划。
5. `V-PACK-DOLL-RECOVER-01`、`V-PACK-DOLL-GROWTH-01`、`V-PACK-DOLL-EMOTION-01`、`V-PACK-DOLL-ROOM-01` 至少四条核心样例有证据，或明确标记等待程序工具补强。
6. 任何“等待程序工具补强”的样例不得用于宣称配置已完成，只能进入 `验收中` 或 `进行中`。
7. `agent_status/design.md` 的 `DES-V3-002` 已更新状态、证据入口和最近更新；如影响长期节点门禁，PM 已同步 `09`。
8. `agent_status/design.md` 已回写最近完成和下一步建议。

---

## 9. 下一步建议

1. 若继续策划侧推进，下一步转向 `DES-V4-001` 经济压力配置：月租、账单、订单、声望、传闻价格波和死锁检查。
2. 若进入配置实现，按 `38` 的顺序先实现 `GROWTH-CHASSIS` 和 `GROWTH-EFFECTS-TRAITS`，再实现义体、配方 / 维护、人偶 / 反馈 / 房间记忆。
3. 若进入程序验收工具补强，把本文 ValidatorID 和固定样例交给 `开发文档/15_P0配置Validator与自动验收底座需求.md` 承接，不在本文写程序实现方案。
