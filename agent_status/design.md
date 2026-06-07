---
id: agent_status_design
title: 策划 / 数值 状态
type: status
role: 策划
domain: design_balance
status: active
source_of_truth: true
related:
  - 版本规划/09_正式版核心纵切开发路线.md
  - 版本规划/11_纵切批次与需求文档承接矩阵.md
  - agent_status/README.md
  - PROJECT_STATUS.md
  - 开发文档/00_程序开发大纲.md
  - agent_status/program.md
  - 数值模型设计/00_基准价值与空间本位模型.md
  - 配置表(JSON)/README.md
  - 设计文档/README.md
  - 设计文档/GDD/GDD_00_系统关联总图.md
  - 设计文档/delivery/00_策划文档开发交付审计.md
  - 设计文档/rules/01_局外时间与日程口径规则卡.md
  - 设计文档/rules/02_物品背包旋转与生命周期规则卡.md
  - 设计文档/rules/03_战斗回合与怪物意图规则卡.md
  - 设计文档/rules/04_小镇经济结算与压力链规则卡.md
  - 设计文档/rules/05_局外成长与维护规则卡.md
  - 设计文档/rules/06_标签与特质规则卡.md
  - 设计文档/rules/07_势力声望与订单规则卡.md
  - 设计文档/rules/08_人偶核心状态与好感双轨规则卡.md
  - 设计文档/rules/09_人偶交互事件与反馈规则卡.md
  - 设计文档/rules/10_剧本调度与事件队列规则卡.md
  - 设计文档/rules/11_人偶房间布局与视觉叙事规则卡.md
  - 设计文档/delivery/12_策划交付落地矩阵.md
  - 设计文档/delivery/13_策划跨系统验收场景矩阵.md
  - 设计文档/delivery/14_策划配置表现验收承接规格.md
  - 设计文档/delivery/15_P0主干配置表现验收承接清单.md
  - 设计文档/delivery/16_P1人偶成长情感承接清单.md
  - 设计文档/delivery/17_P2长期循环叙事承接清单.md
  - 设计文档/content_packs/18_正式版内容生产规格.md
  - 设计文档/config/26_正式配置设计与填充推进计划.md
  - 设计文档/config/audits/27_Items正式配置承接审计.md
  - 设计文档/config/audits/28_Monsters正式配置承接审计.md
  - 设计文档/config/audits/29_Dungeons正式配置承接审计.md
  - 设计文档/config/audits/30_Rewards正式配置承接审计.md
  - 设计文档/config/designs/31_第一层正式配置落地设计.md
  - 设计文档/config/designs/32_第二层正式配置落地设计.md
  - 设计文档/config/designs/33_第三层正式配置落地设计.md
  - 设计文档/config/tasks/34_前三层正式配置实现任务拆分.md
  - 设计文档/config/validation/35_前三层正式配置Validator与固定Seed验收样例.md
  - 设计文档/config/audits/36_局外成长正式配置承接审计.md
  - 设计文档/config/designs/37_局外成长正式配置落地设计.md
  - 设计文档/config/tasks/38_局外成长正式配置实现任务拆分.md
  - 设计文档/config/gates/39_局外成长正式配置ID锁定与冲突检查.md
  - 设计文档/config/designs/40_局外成长底盘正式配置落地设计.md
  - 设计文档/config/designs/41_局外成长效果特质正式配置落地设计.md
  - 设计文档/config/designs/42_局外成长义体正式配置落地设计.md
  - 设计文档/config/designs/43_局外成长制造维护正式配置落地设计.md
  - 设计文档/config/designs/44_局外成长人偶特质房间正式配置落地设计.md
  - 设计文档/config/validation/45_局外成长正式配置Validator与固定验收样例.md
  - 设计文档/config/audits/46_经济压力正式配置承接审计.md
  - 设计文档/config/designs/47_经济压力核心正式配置落地设计.md
  - 设计文档/config/designs/48_经济压力传闻正式配置落地设计.md
  - 设计文档/config/designs/49_经济压力势力正式配置落地设计.md
  - 设计文档/config/designs/50_经济压力订单正式配置落地设计.md
  - 设计文档/config/validation/51_经济压力正式配置Validator与固定验收样例.md
  - 设计文档/config/tasks/52_经济压力正式配置实现任务拆分.md
  - 设计文档/config/gates/53_经济压力正式配置ID锁定与冲突检查.md
  - 设计文档/config/gates/54_经济压力正式配置README字段口径检查.md
  - 设计文档/config/gates/55_经济压力订单ID最终锁定表.md
  - 设计文档/config/gates/56_正式配置源落地准入门禁.md
  - 设计文档/content_packs/19_第一层正式核心内容包.md
  - 设计文档/content_packs/20_第二层背包压力内容包.md
  - 开发文档/15_P0配置Validator与自动验收底座需求.md
  - 设计文档/content_packs/21_人偶成长修复内容包.md
  - 设计文档/content_packs/22_小镇经济月租内容包.md
  - 设计文档/content_packs/23_长期记忆剧情内容包.md
  - 设计文档/content_packs/24_势力订单声望内容包.md
  - 设计文档/content_packs/25_第三层路线侵蚀内容包.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - agent_status/art.md
  - 知识库/views/design.md
last_verified: 2026-06-07
update_rule: 策划、数值、GDD 或配置意图任务完成后更新本文件。
---

# 策划 / 数值 状态

## 最后更新

2026-06-08

## 当前关注

保持正式版核心纵切的规则、GDD、数值模型和配置意图一致。优先推进纵向系统闭环，不横向铺大量内容。纵切只代表开发顺序，不代表质量降级；被选中的系统按正式版标准完整处理。

`设计文档/` 已按文档用途完成分区：`GDD/`、`rules/`、`delivery/`、`content_packs/`、`config/` 和 `_archive/`。策划 agent 后续先读 `设计文档/README.md` 判断文档类型，再进入具体事实来源。

PM 版本节点中，策划线当前落在 A1 前置：为 A2 背包与战斗正式纵切先输出规则卡、配置影响、运行时预期和验证需求。

新增策划交付门禁：所有进入开发、配置、表现或自动验收的系统需求，都必须有详细需求文档承接；不能只用路线文档、优先级列表或聊天结论中的一句话替代。

下一阶段策划工作已从“继续补设计文档”切换为“配置源 JSON 落地”。`设计文档/config/26_正式配置设计与填充推进计划.md` 已作为执行方案入口，按 `C1 第一层正式配置落地 -> C2 第二层直达与背包压力配置 -> C3 第三层路线侵蚀配置 -> C4 局外成长配置落地 -> C5 经济压力与订单配置落地` 推进。C1 与 C2 均已配置完成；当前策划配置执行批次切到 C3，下一项为 `L3-ITEMS`，只处理第三层路线侵蚀 / 污染物、订单冲突物、局外回流物和三层关键材料源配置，不扩局外成长、经济压力、美术 UI 或第四层内容。C3 开工前只做 `56` 准入复核；一旦开始写 `配置表(JSON)/Items`，同步把 `26` 的 C3 与 `L3-ITEMS` 改为 `进行中`。

防重复策划 / 配置开发状态已挂到 `设计文档/config/26_正式配置设计与填充推进计划.md`：`8.3 策划侧完整执行规划与状态总览` 是策划 agent 的完整工作规划入口，按需求 / 规则、配置设计准入、配置源落地、验收 / 校准、后续内容池分层记录；`8.4 策划配置执行状态表` 是配置源 JSON 落地的最小防重复台账，包含 C1-C5 批次总状态和子项级状态。后续完成任一策划工作项、配置批次或配置子项后，必须同步更新 `26` 和本状态页；没有源 JSON、同步和校验证据时，不得把状态标为 `配置完成`。

## 必读文件

- `知识库/views/design.md`
- `设计文档/README.md`
- `设计文档/GDD/GDD_00_系统关联总图.md`
- `设计文档/config/26_正式配置设计与填充推进计划.md`
- `设计文档/config/audits/27_Items正式配置承接审计.md`
- `设计文档/config/audits/28_Monsters正式配置承接审计.md`
- `设计文档/config/audits/29_Dungeons正式配置承接审计.md`
- `设计文档/config/audits/30_Rewards正式配置承接审计.md`
- `设计文档/config/audits/46_经济压力正式配置承接审计.md`
- `设计文档/config/designs/47_经济压力核心正式配置落地设计.md`
- `设计文档/config/designs/48_经济压力传闻正式配置落地设计.md`
- `设计文档/config/designs/49_经济压力势力正式配置落地设计.md`
- `设计文档/config/designs/50_经济压力订单正式配置落地设计.md`
- `设计文档/config/validation/51_经济压力正式配置Validator与固定验收样例.md`
- `设计文档/config/tasks/52_经济压力正式配置实现任务拆分.md`
- `设计文档/config/gates/53_经济压力正式配置ID锁定与冲突检查.md`
- `设计文档/config/gates/54_经济压力正式配置README字段口径检查.md`
- `设计文档/config/gates/55_经济压力订单ID最终锁定表.md`
- `设计文档/config/gates/56_正式配置源落地准入门禁.md`
- `版本规划/09_正式版核心纵切开发路线.md`
- `版本规划/11_纵切批次与需求文档承接矩阵.md`
- `数值模型设计/00_基准价值与空间本位模型.md`
- 当前系统对应的 `设计文档/GDD/GDD_*.md`
- 配置意图变化时，对应的 `配置表(JSON)/*/README.md`

## 工作边界

- 当前路线是正式版核心纵切，不是继续给 MVP 打补丁。
- 如果旧 MVP 规划文档与当前 GDD 或 `09` 路线冲突，以当前 GDD 和 `09` 路线为准。
- `设计文档/` 不会在同一批次全部并行开发，具体纵切批次、当前依赖和正式完成口径以 `版本规划/11_纵切批次与需求文档承接矩阵.md` 为准。
- 系统规则写入 `设计文档/`。
- 程序边界和实现契约写入 `开发文档/`。
- 数值假设写入 `数值模型设计/` 或对应配置 README。
- `设计文档/GDD/` 是系统设计母文档；`设计文档/rules/` 是规则卡；`设计文档/delivery/` 是交付 / 验收承接；`设计文档/content_packs/` 是内容生产规格与具体内容包；`设计文档/config/` 是正式配置设计、审计、任务、验收和准入链路。
- 内容包是内容填充层，不是顶层开发路线。第四层及以后内容草案已归入 `设计文档/_archive/content_backlog/`，不列入当前优先推进范围。

## 当前策划优先级

1. 背包与物品生命周期。
2. 战斗与怪物机制框架。
3. 深渊探索与层级节奏。
4. 局外成长系统。
5. 经济与长期压力。
6. UI 与美术工作流作为规则表达层。

## 最近完成

- 已完成 C1 第一层正式配置源工作区收尾：确认当前未提交配置表变更均属于第一层正式配置落地范围，并完成 14 个 Items、10 个 Rewards、6 个 Monsters、`layer_1.json`、5 条 FixedSeedSamples、Boss / 精英保底归属和 Boss 后 SafeZone 的静态复核；`./tools/config/Sync-Configs.ps1 -Clean` 已通过。本次收尾只处理 C1 配置源提交，不改变 C3 下一步仍从 `L3-ITEMS` 开始的计划。
- 已完成 C2 第二层直达与背包压力配置源落地：`L2-ITEMS`、`L2-REWARDS`、`L2-MONSTERS`、`L2-DUNGEONS`、`L2-ORDERS`、`L2-SEED` 均已在 `设计文档/config/26_正式配置设计与填充推进计划.md` 标记为 `配置完成`。本轮新增 / 补强第二层 12 件物品、14 个奖励、7 个怪物、3 个订单、2 个必要 Faction 引用和 `配置表(JSON)/Dungeons/layer_2.json`；`layer_2.json` 已具备 `DirectStartAllowed=true`、`PreviousLayerRequired=1`、Boss `boss_spore_foundry`、Boss 后免费恢复 HP / SAN 的 `StairsNode + SafeZoneRules`，并补 `V-L2-DIRECT-2048-ROUTE-01`、`V-L2-PACK-RUN-01`、`V-L2-PACK-ORDER-01`、`V-L2-PACK-ELITE-01`、`V-L2-PACK-BOSS-01` 五条固定样例。已通过 C2 静态 Validator 等价检查和 `./tools/config/Sync-Configs.ps1 -Clean`；`ConfigValidationSmokeTest.Run` / P0 Unity 步骤因 Unity Editor 未运行被工具阻塞，未作为通过证据。C3 已解锁为 `待开始`，当前子项为 `L3-ITEMS`。
- 已完成 C2 后的策划收尾：`设计文档/config/26_正式配置设计与填充推进计划.md` 已补充 C1 / C2 完成记录、C3 开工方案和 C3 边界；后续不重复派发 C1 / C2 基础配置，只按缺口补齐、验收修复、字段迁移或数值校准处理。当前下一步明确为 C3 `L3-ITEMS`，先准入复核，再落第三层 Items 源 JSON。
- 历史阶段性收尾：2026-05-28 策划 / 配置侧当时未开启新的 C2 源 JSON 修改；`26` 与本状态页当时统一为 C1 `配置完成`、C2 `待开始`、当前子项 `L2-ITEMS`。该记录已被 2026-06-07 的 C2 配置完成记录覆盖。最新 P0 验证报告 `UnityClient/Logs/P0Validation/latest/report.json` 为 `Passed`，RunID=`20260527_014423`，Errors=0；C1 配置侧不再记录为 P0 阻塞。仍保留 `CombatLootDropTest.Run` 的旧精英机核断言作为程序测试口径缺口，因为该测试文件当前仍断言 `elite_scrap_guard` 奖励包含 `mat_core_tier1`。
- 已完成 C1 的 `L1-SEED` 并收口第一层正式配置源：`配置表(JSON)/Dungeons/layer_1.json` 的 `FixedSeedSamples` 已覆盖 `V-L1-SEED-1024-ROUTE-01`、`V-L1-PACK-RUN-01`、`V-L1-PACK-FAIL-01`、`V-L1-BOSS-REWARD-01`、`V-L1-PACK-PRESSURE-01` 五条固定样例，并同步补入 `ValidationScenarioRefs`；每条样例具备 seed、层级、MapProfile、路线 / 遭遇 / 奖励摘要、玩家选择、通过条件和失败信号。已运行 L1-SEED 静态证据检查、`./tools/config/Sync-Configs.ps1 -Clean`、`ConfigValidationSmokeTest.Run`、`DungeonSeedAcceptanceSmokeTest.Run`、`RewardSystemSmokeTest.Run`、`ItemLifecycleServiceSmokeTest.Run` 和 Boss 保底结构检查，均通过。`CombatLootDropTest.Run` 仍按旧口径要求 `elite_scrap_guard` 保底 `mat_core_tier1`，已记录为程序测试口径缺口，不反向改变 C1 配置。`26` 已将 C1 改为 `配置完成`，C2 解锁为 `待开始`，当前子项为 `L2-ITEMS`。
- 已完成 C1 的 `L1-DUNGEONS` 配置源落地：`配置表(JSON)/Dungeons/layer_1.json` 已改为第一层正式地图源配置，`MapProfileID` 迁移为 `layer1_tutorial_branching`，新增 `layer1_tutorial_branching` / `layer1_event_miner_remains` 两个 MapProfile；普通战斗池覆盖 `mob_scavenger_bug`、`mob_rust_hound`、`mob_acid_slime`、`mob_lost_miner_echo`；`elite_scrap_guard` 只作为可绕精英，`BossNode` / `EncounterPools.BossEncounterID` 指向 `boss_gatekeeper_mk1`；节点奖励引用第一层宝箱 / 事件奖励；Boss 后 `StairsNode + SafeZoneRules` 为免费恢复 HP / SAN、允许撤离 / 深入、不清污染 / 订单 / 探索账本；补 `V-L1-SEED-1024-ROUTE-01`、`V-L1-PACK-FAIL-01`、`V-L1-BOSS-REWARD-01`、`V-L1-PACK-PRESSURE-01` 四条 FixedSeedSamples。已运行静态引用检查、`./tools/config/Sync-Configs.ps1 -Clean`、`ConfigValidationSmokeTest.Run`、`DungeonNodeTypesSmokeTest.Run`、`DungeonSeedAcceptanceSmokeTest.Run`，均通过；`26` 的 C1 当前子项已推进到 `L1-SEED`。
- 已补强策划侧防重复执行状态：`设计文档/config/26_正式配置设计与填充推进计划.md` 的 8.3 新增策划执行分层和 `DES-EXEC-LEDGER` 工作项，明确需求 / 规则、配置设计准入、配置源落地、验收 / 校准、后续内容池各自的状态位置和完成口径；`AGENTS.md` 同步要求策划完整规划按工作性质分层记录，只有配置源落地进入 8.4 子项台账。本次是策划证据补强，不改变 `L1-DUNGEONS` 的配置完成状态。
- 已将接下来配置落地规划写入 `设计文档/config/26_正式配置设计与填充推进计划.md`：新增 `配置源落地执行方案` 和 `策划配置执行状态表`，明确 C1-C5 批次、每批修改范围、完成标准、当前状态和 C1 第一层正式配置开工顺序。当前下一步不再继续扩写设计文档，而是执行 C1：第一层 `Items -> Rewards -> Monsters -> Dungeons -> Seed` 配置源 JSON 落地、同步、校验和状态回写；每完成一个批次或子项都要回写状态表。
- 已补强 `设计文档/config/26_正式配置设计与填充推进计划.md`：新增 `8.3 策划侧完整执行规划与状态总览`，把策划文档结构、需求门禁、主干配置审计、C1-C5 配置源落地、局外成长、经济压力和长期内容池都纳入策划侧防重复规划；原配置源落地台账调整为 `8.4 策划配置执行状态表`，继续以“批次总状态 + 子项执行台账 + 状态回写规则”作为 JSON 落地最小状态单位。后续策划 agent 完成一个工作项或子项就更新 `26` 和本状态页，防止重复开发或误判完成。
- 已完成 C1 的 `L1-MONSTERS` 配置源落地：`配置表(JSON)/Monsters` 补强 `mob_scavenger_bug`、`mob_acid_slime`、`elite_scrap_guard`，新增 `mob_rust_hound`、`mob_lost_miner_echo`、`boss_gatekeeper_mk1`；`mob_acid_slime` 已切到 Layer1 与 `reward_monster_mob_acid_slime_l1`，`elite_scrap_guard` 回归精英职责并移除一阶机核保底，`boss_gatekeeper_mk1` 承担第一层 Boss / RouteGate 与 `reward_boss_gatekeeper_mk1`。已运行 7 个 Monsters JSON 解析检查、RewardID / AddCursedItem / LootPool 引用检查、Boss / 精英拆分检查、`./tools/config/Sync-Configs.ps1 -Clean`、`ConfigValidationSmokeTest.Run` 和 `MonsterActionAITest.Run` 单项验证；早期完整 P0 `20260527_012751` 曾被 P0 包装器报告匹配问题阻塞，不是怪物配置引用错误；最新阶段性收尾 P0 已通过。`26` 的 C1 当前子项已推进到 `L1-DUNGEONS`。
- 已完成 C1 的 `L1-REWARDS` 配置源落地：`配置表(JSON)/Rewards` 保留并补强 `reward_monster_mob_scavenger_bug`、`reward_monster_elite_scrap_guard`、`reward_node_treasure_layer1`、`reward_node_event_layer1`，新增 `reward_monster_mob_rust_hound`、`reward_monster_mob_acid_slime_l1`、`reward_monster_mob_lost_miner_echo`、`reward_boss_gatekeeper_mk1`、`reward_node_event_layer1_miner_pack`、`reward_node_event_layer1_cracked_relic`；`mat_core_tier1` 已从精英保底迁移到 `reward_boss_gatekeeper_mk1`。已运行 Rewards JSON UTF-8 解析检查、Reward ItemID / RewardRef 引用检查、Boss 保底迁移检查、`./tools/config/Sync-Configs.ps1 -Clean` 和 `.\tools\agent\Invoke-P0Validation.ps1 -TimeoutSeconds 180`；ConfigSync、ConfigValidator、UI 规格和 ArtAcceptance latest 通过；早期完整 P0 报告 `20260527_010158` 曾被程序 smoke 旧口径和无关布局测试超时阻塞，最新阶段性收尾 P0 已通过，报告见 `UnityClient/Logs/P0Validation/latest/report.json`。
- 已完成 C1 的 `L1-ITEMS` 配置源落地：`配置表(JSON)/Items` 中 10 个既有第一层相关 ItemID 已补正式字段，新增 `gear_plank_shield_l1`、`gear_cracked_iron_armor`、`trade_miner_lamp`、`trade_cracked_relic`；`loot_toxic_filter` 改为 L 形 3 格四向旋转，`con_repair_kit` 改为 1x2 两向旋转。已运行 JSON 解析检查、`./tools/config/Sync-Configs.ps1 -Clean`、`.\tools\agent\Invoke-P0Validation.ps1 -TimeoutSeconds 180` 和 `.\tools\docs\Validate-Docs.ps1`；P0 验证状态为 `Passed`、Errors=0，报告见 `UnityClient/Logs/P0Validation/latest/report.json`，剩余 warning 为 metadata-only 标签和既有 smoke warning。
- 已完成 `设计文档/` 结构重构：系统 GDD 迁入 `设计文档/GDD/`，规则卡迁入 `rules/`，交付 / 验收承接迁入 `delivery/`，内容生产规格与内容包迁入 `content_packs/`，正式配置审计 / 设计 / 任务 / 验收 / 门禁迁入 `config/` 分区。
- 已重写 `设计文档/README.md`：新增目录地图、文档类型词典、正式配置链路说明和 active 文件地图；明确 `配置承接审计` 是配置源现状与正式设计之间的缺口审计，`正式配置落地设计` 是 JSON 修改前配置设计清单，不等于配置完成。
- 已同步 `AGENTS.md`、`GEMINI.md`、`PROJECT_STATUS.md`、`知识库/views/design.md`、`版本规划/11_纵切批次与需求文档承接矩阵.md`、`tools/docs/validate_docs.py` 和 active 文档内的设计文档路径引用；重新生成 `DOCS_INDEX.md` / `docs_index.json` 并通过知识库校验。
- 已创建复制策划智能体使用的状态页。
- 已在 `AGENTS.md` 和状态文档中对齐项目路线与 GDD 路径。
- PM 已将策划线纳入 `版本规划/09_正式版核心纵切开发路线.md` 和 `版本规划/11_纵切批次与需求文档承接矩阵.md`：A1 准备 A2 规则卡，A2 聚焦背包与战斗，A3/A4 承接深渊、局外成长和经济压力。
- 已将版本规划批次口径并入 `版本规划/11_纵切批次与需求文档承接矩阵.md`，明确 `GDD_00` 到 `GDD_12` 的系统批次、当前依赖、正式完成口径和需求文档承接状态。
- 已将 `设计文档/GDD/GDD_02_深渊地图遍历与搜打撤抉择.md` 的地图生成规则改为正式版节点网络：多行节点、连线生成、路线主题、战争迷雾和 seed 可复现验收。
- 已新增 `设计文档/delivery/00_策划文档开发交付审计.md`，按开发交付标准审计 `GDD_00` 到 `GDD_12`，明确当前 P0 缺口是局外时间口径统一、物品背包规则卡、战斗怪物意图规则卡和深渊地图生成开发规则卡。
- 已新增 `设计文档/rules/01_局外时间与日程口径规则卡.md`，统一局外 Day / Week / Month 与局内 AP 的边界，并同步修正 `GDD_00`、`GDD_03`、`GDD_07`、`GDD_08`、`GDD_09`、`GDD_11`、`GDD_12` 的旧 AP 日程口径。
- 已更新策划交付审计：局外时间 P0-1 标记为已完成正式统一；深渊地图生成 P0-4 已并入 `GDD_02`，可作为地图生成器当前纵切开发依据，后续重点转为配置 README 与 Validator 对齐。
- 已新增 `设计文档/rules/02_物品背包旋转与生命周期规则卡.md`，把物品定义、实例状态、容器、旋转放置、战败 / 撤离 / 出售 / 制造和怪物对包干涉统一为正式规则。
- 已新增 `设计文档/rules/03_战斗回合与怪物意图规则卡.md`，把战斗回合状态机、局内 AP、护盾、怪物意图、目标选择、多怪行动顺序和胜负结算统一为正式规则；`GDD_01` 已调整为可开发。
- 已新增 `设计文档/rules/04_小镇经济结算与压力链规则卡.md`，把每日营业结算、售价公式、传闻价格波、违禁品隔夜代价、月租账单和欠债裁决统一为正式规则；`GDD_04` 已调整为可开发。
- 已新增 `设计文档/rules/05_局外成长与维护规则卡.md`，把底盘、义体、心智天赋、维护、换装、成长实例字段和下潜许可检查统一为正式规则；`GDD_08` 已调整为可开发。
- 已新增 `设计文档/rules/06_标签与特质规则卡.md`，把标签命名、动态标签生命周期、查询表达、特质触发器、事件监听白名单、互斥 / 优先级 / 叠加、消除转化和持久化规则统一为正式规则；`GDD_09` 已调整为可开发。
- 已新增 `设计文档/rules/07_势力声望与订单规则卡.md`，把势力字段、订单定义 / 实例字段、订单刷新算法、截止日状态机、奖励引用、声望阈值、黑市背叛和委托物绑定统一为正式规则；`GDD_10` 已调整为可开发。
- 已新增 `设计文档/rules/08_人偶核心状态与好感双轨规则卡.md`，把人偶实例字段、SAN / Bond 阈值、情绪状态机、奇迹裁决、崩溃处置、特质输入事件和与交互系统的分工统一为正式规则；`GDD_03` 已调整为可开发。
- 已新增 `设计文档/rules/09_人偶交互事件与反馈规则卡.md`，把交互事件结构、触摸 / 对话 / 赠礼 / 保养 / 特殊交互、每日上限、偏好、反馈选择、对话池抽取和验收样例统一为正式规则；`GDD_12` 已调整为可开发。
- 已新增 `设计文档/rules/10_剧本调度与事件队列规则卡.md`，把剧本事件字段、触发器表达式、事件队列优先级、对话节点结构、系统指令、跳过 / 重入和存档旗标统一为正式规则；`GDD_05` 已调整为可开发。
- 已新增 `设计文档/rules/11_人偶房间布局与视觉叙事规则卡.md`，把房间布局数据、家具槽、展示品、纪念物、窗外状态、待机表现、资源 fallback、Validator 和验收样例统一为正式规则；`GDD_11` 已调整为可开发。
- 已清理 `设计文档/delivery/00_策划文档开发交付审计.md`、`GDD_09` 和 `GDD_12` 中容易误导开发的旧阶段措辞：局外交互不再写成 AP 消耗，审计表述已调整为正式纵切口径。
- 已新增 `设计文档/delivery/12_策划交付落地矩阵.md`，把 `GDD_00` 到 `GDD_12` 的规则事实来源、配置承接、表现承接、验收证据和后续补强优先级串成策划交付导航。
- 已新增 `设计文档/delivery/13_策划跨系统验收场景矩阵.md`，从玩家路径定义备战下潜、战斗拾取、撤离回流、战败创伤、地图路线、经济压力、成长再挑战、人偶修复、订单、剧情和房间视觉日记的跨系统验收场景。
- 已复查 `设计文档/` 全部 GDD 与规则卡交付状态：按“GDD + 规则卡 + 落地矩阵 + 验收场景”的文档组口径，当前已可交付开发；深渊地图节点网络生成已在 `GDD_02` 补到算法、约束、seed 复现和验收层级。
- 已更新 `设计文档/delivery/00_策划文档开发交付审计.md` 和 `设计文档/delivery/12_策划交付落地矩阵.md`，明确部分 GDD 正文偏体验叙述不是阻塞，但开发任务必须引用完整交付包，不能只读单份 GDD。
- 已新增 `设计文档/delivery/14_策划配置表现验收承接规格.md`，把规则卡之后的配置字段、表现信息、验收样例和内容量承接粒度统一成策划交付规格。
- 已新增 `设计文档/delivery/15_P0主干配置表现验收承接清单.md`，把 `Items`、`Monsters`、`Dungeons` 三条 P0 主干拆成配置字段、玩家可见信息、首批内容、验收样例和完成判定。
- 已新增 `开发文档/15_P0配置Validator与自动验收底座需求.md`，把 P0 配置 / Validator / 验收底座转成程序可开发工具需求，作为策划承接清单的程序落地点。
- 已将“系统需求不能只是一句话，必须有详细需求文档承接”写入 `AGENTS.md`、`知识库/views/design.md` 和 `设计文档/README.md`，作为后续策划 agent 的开工门禁。
- 已新增 `设计文档/delivery/16_P1人偶成长情感承接清单.md`，把人偶核心状态、局外成长、标签特质、交互反馈和房间视觉叙事拆成配置字段、玩家可见信息、验收样例和联合验收路径。
- 已新增 `设计文档/delivery/17_P2长期循环叙事承接清单.md`，把长期经济、势力订单、剧本事件队列和房间长期记忆拆成配置字段、玩家可见信息、首批内容、验收样例和联合验收路径。
- 已新增 `设计文档/content_packs/18_正式版内容生产规格.md`，规定物品、怪物、地图画像、人偶成长、经济、订单、剧情事件和房间记忆等具体内容条目的玩家承诺、玩法职责、系统连接、内容包组织和验收口径。
- 已新增 `设计文档/content_packs/19_第一层正式核心内容包.md`，把第一层浅渊回廊的 12 件物品、5 只怪物、1 个 Boss、2 个地图画像、3 条传闻、2 个事件、3 个房间记忆和 4 条联合验收路径整理为正式内容包。
- 已新增 `设计文档/content_packs/20_第二层背包压力内容包.md`，把第二层腐蚀甬道的 12 件物品、4 只普通怪、2 个精英、1 个 Boss、2 个地图画像、3 个订单、3 个事件、3 条传闻、3 个房间记忆和 6 条联合验收路径整理为正式内容包。
- 已新增 `设计文档/content_packs/21_人偶成长修复内容包.md`，把人偶成长修复主干的 3 个底盘、6 个义体、5 个维护方案、8 个特质、8 条交互反馈、8 个房间纪念物和 6 条联合验收路径整理为正式内容包。
- 已新增 `设计文档/content_packs/22_小镇经济月租内容包.md`，把小镇经济月租主干的 8 条传闻、6 类顾客、4 套账单、4 类违禁品规则、1 条月租曲线、典当边界和 6 条联合验收路径整理为正式内容包。
- 已新增 `设计文档/content_packs/23_长期记忆剧情内容包.md`，把长期记忆剧情主干的 8 个主线事件、8 个 Bond 事件、5 个债务事件、8 个深渊 lore、10 个纪念物、12 条日记、7 个窗外状态、7 条待机规则和 6 条联合验收路径整理为正式内容包。
- 已完成全设计文档交付审计 2.0：`00_策划文档开发交付审计.md` 和 `12_策划交付落地矩阵.md` 已从“GDD + 规则卡可开发”升级为“规则层、承接层、首批正式内容包层可交付”，并将 `19` 到 `23` 标记为可拆分到配置、表现和验收的正式内容包。
- 已新增 `设计文档/content_packs/24_势力订单声望内容包.md`，把 5 个势力、5 套声望 / 信任阶梯、5 个订单池、订单模板草案、12 类奖励、黑市背叛和 7 条联合验收路径整理为正式内容包；后续经济压力首批正式订单 ID 以 `55_经济压力订单ID最终锁定表.md` 为准。
- 已新增 `设计文档/content_packs/25_第三层路线侵蚀内容包.md`，把第三层菌脉深庭的 12 件物品、5 只普通怪、2 个精英、1 个 Boss、2 个地图画像、4 个订单、3 个事件、3 条传闻、4 个房间记忆和 5 条联合验收路径整理为正式内容包；深渊前三层已形成低压入口 -> 背包压力 -> 路线侵蚀 / 订单冲突的正式递进。
- 已新增 `设计文档/README.md`，明确设计文档目录分层和阅读顺序：GDD 是系统设计母文档，规则卡负责可开发裁决，交付矩阵负责配置 / 表现 / 验收承接，`18` 负责内容生产规格，`19+` 负责具体内容填充；同时将第四层组合压力内容包归档为后续层内容草案。
- 已新增 `设计文档/config/26_正式配置设计与填充推进计划.md`，把下一阶段策划工作落为正式配置设计、配置验收标准和正式内容填充节奏，并用 3-5 小时纵切体验、8-12 小时主线目标反推内容量。
- 已新增 `设计文档/config/audits/27_Items正式配置承接审计.md`，完成 `Items` 配置域首轮承接审计，输出字段缺口、前三层物品内容缺口、现有条目差异和 Validator 建议；`26` 的 W1 已标记为进行中。
- 已同步 `设计文档/README.md`、`配置表(JSON)/Items/README.md` 和版本规划边界：`27` 是 `26` W1 的配置审计产物，不是新的顶层路线，也不代表配置字段已经落地。
- 已新增 `设计文档/config/audits/28_Monsters正式配置承接审计.md`，完成 `Monsters` 配置域首轮承接审计，明确当前 4 个怪物 JSON 与前三层内容包的差异、Boss / 精英职责错位、意图可读字段缺口和 Validator 建议。
- 已同步 `设计文档/config/26_正式配置设计与填充推进计划.md`、版本规划边界、`设计文档/README.md`、`配置表(JSON)/Monsters/README.md`、`配置表(JSON)/Dungeons/README.md` 和 `配置表(JSON)/Rewards/README.md`：`28` 是 W1 配置审计产物，不代表怪物 JSON 已完成。
- 已新增 `设计文档/config/audits/29_Dungeons正式配置承接审计.md`，完成 `Dungeons` 配置域首轮承接审计，明确当前一二层 `layer_*.json` 与前三层地图画像、NodePool、BossNode、安全区语义和 seed 验收之间的差距。
- 已同步 `设计文档/config/26_正式配置设计与填充推进计划.md`、版本规划边界、`设计文档/README.md` 和 `配置表(JSON)/Dungeons/README.md`：`29` 是 W1 配置审计产物，不代表层级 JSON 已完成。
- 已新增 `设计文档/config/audits/30_Rewards正式配置承接审计.md`，完成 `Rewards` 配置域首轮承接审计，明确当前 8 个奖励 JSON 与前三层奖励、正式 Boss 保底、订单 / 成长 / 节点奖励引用和 Validator 建议之间的差距。
- 已同步 `设计文档/config/26_正式配置设计与填充推进计划.md`、版本规划边界、`设计文档/README.md`、`配置表(JSON)/Rewards/README.md` 以及 Items / Monsters / Dungeons README：`30` 是 W1 配置审计产物，不代表奖励 JSON 已完成。
- 已按 `30_Rewards正式配置承接审计.md` 扩展 `配置表(JSON)/Rewards/README.md`：补充 `RewardRole`、`SourceType`、`LayerRange`、`RewardTier`、价值预算、成长 / 订单 / 解锁 / 房间记忆字段、正式奖励职责和校验规则；该动作只完成字段口径，不代表奖励 JSON 已完成。
- 已按 `27_Items正式配置承接审计.md` 扩展 `配置表(JSON)/Items/README.md`：补充正式配置基本规则、`ItemRole`、`DescriptionKey`、`LayerTags`、生命周期、来源去向、经济 / 风险 / 表现字段、物品职责口径和 Validator 建议；该动作只完成字段口径，不代表物品 JSON 已完成。
- 已按 `28_Monsters正式配置承接审计.md` 扩展 `配置表(JSON)/Monsters/README.md`：补充正式怪物职责、层级遭遇、意图可读、行为 / 阶段、目标规则、对包干涉、Boss / 精英边界和 Validator 建议；该动作只完成字段口径，不代表怪物 JSON 已完成。
- 已按 `29_Dungeons正式配置承接审计.md` 扩展 `配置表(JSON)/Dungeons/README.md`：补充正式地图画像、路线约束、遭遇池、安全区语义、层级入口、固定 seed 验收和 Validator 建议；该动作只完成字段口径，不代表层级 JSON 已完成。
- 已新增 `设计文档/config/designs/31_第一层正式配置落地设计.md`，作为 `26` W2 的第一层配置落地设计产物；该文档把 `19` 内容包和 `27-30` 审计结论合成为后续改 JSON 前的 Items / Rewards / Monsters / Dungeons 策划清单，不代表第一层 JSON 已完成。
- 已新增 `设计文档/config/designs/32_第二层正式配置落地设计.md`，作为 `26` W2 的第二层配置落地设计产物；该文档把 `20` 内容包和 `27-30` 审计结论合成为后续改 JSON 前的 Items / Rewards / Monsters / Dungeons / Orders 策划清单，不代表第二层 JSON 已完成。
- 已同步 `09` 的版本规划边界：长期节点、职能入口和防重复派发规则保留在 `09`；第二层配置 ID、字段、奖励、怪物、地图和订单细节保留在 `32`。
- 已新增 `设计文档/config/designs/33_第三层正式配置落地设计.md`，作为 `26` W2 的第三层配置落地设计产物；该文档把 `25` 内容包和 `27-30` 审计结论合成为后续改 JSON 前的 Items / Rewards / Monsters / Dungeons / Orders / Events / Rumors / RoomMemory 策划清单，不代表第三层 JSON 已完成。
- 已同步 `26` 和版本规划边界：W2 前三层配置落地设计在策划设计层已闭合，下一步应拆前三层配置实现任务或补 Validator 样例，不应继续新增第四层内容包。
- 已新增 `设计文档/config/tasks/34_前三层正式配置实现任务拆分.md`，把 `31` / `32` / `33` 拆成后续可派发的 `L1/L2/L3` 配置实现任务、依赖顺序、交付证据、Validator / seed 验收要求和完成判定；该文档不修改 JSON，也不代表前三层配置已完成。
- 已新增 `设计文档/config/validation/35_前三层正式配置Validator与固定Seed验收样例.md`，把 `34` 的任务 ID 接到 `CFG-*` Validator、`V-L1/L2/L3-*` 固定 seed、失败信号和回写证据模板；该文档不修改 JSON，也不代表前三层配置已完成。
- 已新增 `设计文档/config/audits/36_局外成长正式配置承接审计.md`，完成 `Chassis`、`Prosthetics`、`CraftingRecipes`、`Dolls`、`Effects` 首轮配置承接审计，明确现有 README / JSON 仍是 MVP / 原型口径，并输出正式字段缺口、内容缺口、Validator 建议和后续 `GROWTH-*` 任务边界。
- 已重构 `设计文档/config/26_正式配置设计与填充推进计划.md`：该文档现在只承载策划 / 配置侧的内容量目标、配置域审计、前三层配置落地设计、局外成长配置审计和验收样例；程序实现进度、美术交付进度和 PM 里程碑分别回到对应职能状态页与事实文档，长期节点口径回到 `09`。
- 已新增 `设计文档/config/designs/37_局外成长正式配置落地设计.md`，把 `36` 的审计缺口和 `21` 的内容包拆成 `GROWTH-README`、`GROWTH-CHASSIS`、`GROWTH-PROSTHETICS`、`GROWTH-CRAFT-MAINT`、`GROWTH-DOLL-TRAIT-ROOM` 的 JSON 修改前策划清单；该文档不修改 JSON，也不代表局外成长配置完成。
- 已完成 `GROWTH-README`：`配置表(JSON)/Chassis/README.md`、`Prosthetics/README.md`、`CraftingRecipes/README.md`、`Dolls/README.md`、`Effects/README.md` 已同步局外成长正式字段口径、运行时边界和 Validator 建议；该动作只完成字段说明承接，不代表局外成长 JSON 已完成。
- 已新增 `设计文档/config/tasks/38_局外成长正式配置实现任务拆分.md`，把 `37` 的局外成长配置清单拆成 `GROWTH-ID-LOCK`、`GROWTH-CHASSIS`、`GROWTH-EFFECTS-TRAITS`、`GROWTH-PROSTHETICS`、`GROWTH-CRAFT-MAINT`、`GROWTH-DOLL-TRAIT-ROOM`、`GROWTH-VALIDATION` 的派发顺序、交付证据和验收口径；该文档不修改 JSON，也不代表局外成长配置完成。
- 已新增 `设计文档/config/gates/39_局外成长正式配置ID锁定与冲突检查.md`，完成 `GROWTH-ID-LOCK` 当前工作区 ID 冲突检查：旧 MVP ID 与正式 ID 无阻塞冲突，`maint_basic_patch` / `maint_purification_flush` 已被当前 JSON 占用且与设计一致；该文档不修改 JSON，也不代表局外成长配置完成。
- 已新增 `设计文档/config/designs/40_局外成长底盘正式配置落地设计.md`，完成 `GROWTH-CHASSIS` 的 3 个正式底盘字段级设计、旧 ID 迁移口径、Validator 和固定验收样例；该文档不修改 JSON，也不代表底盘配置完成。
- 已新增 `设计文档/config/designs/41_局外成长效果特质正式配置落地设计.md`，完成 `GROWTH-EFFECTS-TRAITS` 的义体效果、维护效果、动态标签、8 个特质、Validator 和固定验收样例字段级设计；该文档不修改 JSON，也不代表效果 / 特质配置完成。
- 已新增 `设计文档/config/designs/42_局外成长义体正式配置落地设计.md`，完成 `GROWTH-PROSTHETICS` 的 6 个正式义体字段级设计、旧 `pros_*` 迁移口径、`RightHand` 槽位兼容策略、Validator 和固定验收样例；该文档不修改 JSON，也不代表义体配置完成。
- 已新增 `设计文档/config/designs/43_局外成长制造维护正式配置落地设计.md`，完成 `GROWTH-CRAFT-MAINT` 的 6 个义体制造配方、2 个底盘解锁配方、5 个维护方案、材料来源、消耗 / 取消策略、Validator 和固定验收样例字段级设计；该文档不修改 JSON，也不代表配方或维护配置完成。
- 已新增 `设计文档/config/designs/44_局外成长人偶特质房间正式配置落地设计.md`，完成 `GROWTH-DOLL-TRAIT-ROOM` 的人偶静态档案、运行时拆分、8 个特质、8 条反馈、8 个房间纪念物、Validator 和固定验收样例字段级设计；该文档不修改 JSON，也不代表人偶 / 特质 / 反馈 / 房间记忆配置完成。
- 已新增 `设计文档/config/validation/45_局外成长正式配置Validator与固定验收样例.md`，完成 `GROWTH-VALIDATION` 的 Validator 覆盖矩阵、固定验收样例、warning / error 分级和回写证据模板；该文档不修改 JSON，也不代表局外成长配置完成。
- 已新增 `设计文档/config/audits/46_经济压力正式配置承接审计.md`，完成 `DES-V4-001` 的 `Economy`、`Orders`、`Factions`、`Rumors` 首轮配置承接审计，明确当前只有最小样例骨架，并输出正式字段缺口、内容缺口、Validator 建议和后续 `ECON-*` 任务边界；该文档不修改 JSON，也不代表经济压力配置完成。
- 已新增 `设计文档/config/designs/47_经济压力核心正式配置落地设计.md`，完成 `ECON-CORE` 的 JSON 修改前字段级设计，覆盖日账单、周报、月租、出售渠道、顾客、违禁品、NightTax、DeadlockGuard、Validator 和固定验收样例；该文档不修改 JSON，也不代表经济压力配置完成。
- 已新增 `设计文档/config/designs/48_经济压力传闻正式配置落地设计.md`，完成 `ECON-RUMORS` 的 JSON 修改前字段级设计，覆盖 8 条正式传闻、传闻字段、刷新组合规则、来源追踪、Validator 和固定验收样例；该文档不修改 JSON，也不代表传闻配置完成。
- 已新增 `设计文档/config/designs/49_经济压力势力正式配置落地设计.md`，完成 `ECON-FACTIONS` 的 JSON 修改前字段级设计，覆盖 5 个正式势力、正规声望阶梯、黑市信任阶梯、订单池引用、冲突规则、Validator 和固定验收样例；该文档不修改 JSON，也不代表势力配置完成。
- 已新增 `设计文档/config/designs/50_经济压力订单正式配置落地设计.md`，完成 `ECON-ORDERS` 的 JSON 修改前字段级设计，覆盖 5 个订单池、19 条订单模板、需求表达、失败 / 背叛裁决、Validator 和固定验收样例；该文档不修改 JSON，也不代表订单配置完成。
- 已新增 `设计文档/config/validation/51_经济压力正式配置Validator与固定验收样例.md`，完成 `ECON-VALIDATION` 的 Validator 覆盖矩阵、跨域检查、固定验收样例、warning / error 分级和 `DES-V4-001` 回写证据模板；该文档不修改 JSON，也不代表经济压力配置完成。
- 已新增 `设计文档/config/tasks/52_经济压力正式配置实现任务拆分.md`，完成 `ECON-TASKS` 的配置实现派发层，明确经济压力配置实现的任务顺序、交付证据、验收顺序和 `DES-V4-001` 回写口径；该文档不修改 JSON，也不代表经济压力配置完成。
- 已新增 `设计文档/config/gates/53_经济压力正式配置ID锁定与冲突检查.md`，完成 `ECON-ID-LOCK` 当前工作区 ID 冲突检查：阻塞性 ID 冲突为 0，`economy_town_v1` 和 `faction_mechanic_workshop` 可复用扩展，`rumor_mechanical_price_up` 与 `order_mechanic_scrap_drive` 后续需迁移或标记为旧样例；该文档不修改 JSON，也不代表经济压力配置完成。
- 已新增 `设计文档/config/gates/54_经济压力正式配置README字段口径检查.md`，完成 `ECON-README-CHECK` 策划证据：`Economy` / `Factions` / `Rumors` / `Orders` 四个 README 可承接后续 JSON 实现字段口径，旧样例迁移 warning 已记录；该文档不修改 JSON，也不代表经济压力配置完成。
- 已新增 `设计文档/config/gates/55_经济压力订单ID最终锁定表.md`，完成 `ECON-ORDERS-JSON` 前置 ID 锁定证据：5 个正式 OrderPoolID 和 19 个正式 OrderID 已锁定，旧 `order_pool_*` 草案 ID 和旧样例 `order_mechanic_scrap_drive` 的迁移口径已明确；该文档不修改 JSON，也不代表订单配置完成。
- 已新增 `设计文档/config/gates/56_正式配置源落地准入门禁.md`，统一前三层、局外成长和经济压力进入配置源 JSON 实现前的设计准入、实现证据和状态回写口径；该文档不修改 JSON，也不代表任何配置源已完成。
- 已在 `设计文档/config/tasks/34_前三层正式配置实现任务拆分.md` 的 `L1-ITEMS` 下补充开工准入核查表，明确第一层物品当前 JSON 现实、目标 ID、必改 / 必补条目、字段覆盖、同步 / Validator / 状态回写证据和禁止误判口径；该补充仍是策划交付，不代表物品 JSON 已完成。
- 已在 `设计文档/config/tasks/34_前三层正式配置实现任务拆分.md` 的 `L1-REWARDS` 下补充开工准入核查表，明确当前 9 个奖励 JSON 现实、目标 RewardID、Boss 保底迁移、正式字段覆盖、ItemID 引用检查、同步 / Validator / 状态回写证据和禁止误判口径；该补充仍是策划交付，不代表奖励 JSON 已完成。
- 已在 `设计文档/config/tasks/34_前三层正式配置实现任务拆分.md` 的 `L1-MONSTERS` 下补充开工准入核查表，明确当前 4 个怪物 JSON 现实、目标 MonsterID、Boss / 精英拆分、RewardID 依赖、第一层意图边界、正式字段覆盖、同步 / Validator / 状态回写证据和禁止误判口径；该补充仍是策划交付，不代表怪物 JSON 已完成。
- 已在 `设计文档/config/tasks/34_前三层正式配置实现任务拆分.md` 的 `L1-DUNGEONS` 与 `L1-SEED` 下补充开工准入核查表，明确第一层地图当前 JSON 现实、目标 MapProfile、Boss / 精英拆分、节点池、Boss 后 SafeZone、固定 seed 摘要、同步 / Validator / 状态回写证据和禁止误判口径；至此第一层 `L1-ITEMS -> L1-REWARDS -> L1-MONSTERS -> L1-DUNGEONS -> L1-SEED` 的配置源实现前设计准入链条已完整，但不代表任何 JSON 已完成。
- 已在 `设计文档/config/tasks/34_前三层正式配置实现任务拆分.md` 的第二层 `L2-ITEMS`、`L2-REWARDS`、`L2-MONSTERS`、`L2-DUNGEONS`、`L2-ORDERS` 和 `L2-SEED` 下补充开工准入核查表，明确第二层直达、背包压力、订单占格、Boss 保底迁移、Boss 后 SafeZone、固定 seed 摘要、同步 / Validator / 状态回写证据和禁止误判口径；该补充仍是策划交付，不代表第二层 JSON 已完成。

## 下一步建议

1. 先以 `设计文档/README.md` 作为策划文档阅读入口，避免把 GDD、规则卡、交付矩阵和内容包混成同一层级。
2. 派发任何系统开发、配置、表现或验收任务前，先检查是否已有详细需求文档；若只有一句功能项，先补文档再交接。
3. 按 `26_正式配置设计与填充推进计划.md` 的 `配置源落地执行方案` 进入 C3：第三层路线侵蚀配置。下一步执行 `L3-ITEMS`：先按 `56_正式配置源落地准入门禁.md` 复核设计准入，再修改 `配置表(JSON)/Items`，补第三层侵蚀 / 污染物、路线取舍物、订单冲突物、局外回流物和三层关键材料，并保留形状 / 旋转 / 生命周期 / 风险字段、同步、Validator / 固定样例和状态回写证据。
4. 策划工作项、C1-C5 任一批次或子项开始、完成或阻塞时，同步更新 `26` 的 `8.3 策划侧完整执行规划与状态总览`、必要时更新 `8.4 策划配置执行状态表` 和本状态页；需求 / 规则、配置设计准入、配置源落地、验收 / 校准、后续内容池要分层记录，已完成或进行中的同名配置批次 / 子项不得重复派发，只能按真实缺口做补齐、验收修复、字段迁移或数值校准。
5. 以交付审计 2.0 的完整交付包为单位派发开发任务：GDD 负责体验定位，规则卡负责硬规则，承接清单负责配置 / 表现 / 验收粒度，`19` 到 `25` 中已进入优先级的内容包负责首批正式内容。
6. 按 `27_Items正式配置承接审计.md` 继续处理第一层关键物品差异、第二层反制 / 订单物和 Validator 建议；不要把 README 字段口径视为物品 JSON 已完成。
7. 按 `28_Monsters正式配置承接审计.md` 继续处理 Boss / 精英职责拆分、前三层怪物内容缺口和 Validator 建议；不要把 README 字段口径视为怪物 JSON 已完成。
8. 按 `29_Dungeons正式配置承接审计.md` 继续处理 MapProfile、NodePool、BossNode、SafeZone / Stairs 和 seed 验收建议；不要把 README 字段口径视为层级 JSON 已完成。
9. 按 `30_Rewards正式配置承接审计.md` 处理 `Rewards` 字段口径、Boss 奖励、第三层奖励、订单 / 成长 / 节点奖励引用和 Validator 建议；不要直接把审计结论视为配置已完成。
10. 按 `15_P0主干配置表现验收承接清单.md` 将 P0 主干落到 `配置表(JSON)/Items/README.md`、`配置表(JSON)/Monsters/README.md`、`配置表(JSON)/Dungeons/README.md`、`配置表(JSON)/Rewards/README.md`。
11. 按 `16_P1人偶成长情感承接清单.md` 和 `21_人偶成长修复内容包.md` 将 P1 主干落到 `Dolls`、`Effects`、`Chassis`、`Prosthetics`、`CraftingRecipes` 和房间 / 交互配置 README。
12. 按 `17_P2长期循环叙事承接清单.md` 将 P2 主干落到经济、Factions / Orders / Rewards、ScenarioEvents / DialogueTrees、RoomMemory / Mementos 等配置 README。
13. 按 `19_第一层正式核心内容包.md`、`20_第二层背包压力内容包.md` 和 `25_第三层路线侵蚀内容包.md` 将前三层内容包落到 `Items`、`Monsters`、`Dungeons`、`Rewards`、Orders、经济传闻、事件、房间记忆和路线侵蚀验收。
14. 按 `22_小镇经济月租内容包.md`、`23_长期记忆剧情内容包.md` 和 `24_势力订单声望内容包.md` 将经济、长期剧情、势力订单主干落到对应配置 README、UI / 美术规格和验收清单。
15. 将 `02_物品背包旋转与生命周期规则卡.md` 同步到 `配置表(JSON)/Items/README.md` 和物品 / 背包 Validator。
16. 将 `03_战斗回合与怪物意图规则卡.md` 同步到 `配置表(JSON)/Monsters/README.md`、怪物配置、意图 UI 和战斗 Validator。
17. 将 `GDD_02` 的 `LayerConfig` / `MapProfile` 同步到 `配置表(JSON)/Dungeons/README.md`，并补地图生成 seed 验收样例与 Validator 规则。
18. 将 `11_人偶房间布局与视觉叙事规则卡.md` 同步到 `配置表(JSON)/Dolls/README.md`、房间资源清单、UI / 美术规格和 Validator。
19. 清点各规则卡剩余配置 README / Validator 对接项，按当前开发优先级拆到配置与开发文档。
20. 若设计变更影响配置，同时更新 GDD 和对应 `配置表(JSON)` README 或样例配置。

## 问题 / 阻塞

- ConfigValidationSmokeTest.Run / P0 Unity 步骤当前因 Unity Editor 未运行被工具阻塞；C2 已用静态 Validator 等价检查和配置同步作为配置侧证据，后续 Unity 打开后可补跑运行时 smoke。
- 当前工作区已有前序设计 / 美术文档 / 程序 / 生成物脏文件，提交时必须严格收窄范围，避免覆盖其他 agent 的改动。
- 需要更强的配置校验层，让策划改动可以被机械检查。
- `L1-ITEMS` P0 验证仍有非阻塞 warning：`Material`、`CoreMaterial`、`Cursed` 等标签当前在程序侧仍是 metadata-only；后续 Validator 强化或规则接入时需要决定是否继续 warning、转为可执行标签，或从 `Tags` 迁移到纯策划字段。
- `CombatLootDropTest.Run` 仍有旧测试口径：当前正式配置要求 `boss_gatekeeper_mk1` 保底 `mat_core_tier1`，`elite_scrap_guard` 只作为可绕精英；但该测试仍断言精英奖励包含 `mat_core_tier1`。策划侧已用结构检查确认 Boss 保底和精英非保底成立，后续由程序侧把该测试改为 Boss 保底口径。

## 完成回写清单

- 更新本文件的 `最后更新`。
- 在 `最近完成` 记录简短事实。
- 如有变化，刷新 `当前关注`、`下一步建议` 和阻塞项。
- 如果需要美术或程序跟进，在 `PROJECT_STATUS.md` 增加跨职能交接。

