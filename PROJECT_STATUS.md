---
id: project_status
title: 项目状态
type: status
role: 全局
domain: project_status
status: active
source_of_truth: true
related:
  - AGENTS.md
  - GEMINI.md
  - 版本规划/README.md
  - 版本规划/09_正式版核心纵切开发路线.md
  - agent_status/pm.md
  - agent_status/program.md
  - agent_status/design.md
  - 设计文档/README.md
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
  - 设计文档/content_packs/21_人偶成长修复内容包.md
  - 设计文档/content_packs/22_小镇经济月租内容包.md
  - 设计文档/content_packs/23_长期记忆剧情内容包.md
  - 设计文档/content_packs/24_势力订单声望内容包.md
  - 设计文档/content_packs/25_第三层路线侵蚀内容包.md
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
  - 开发文档/15_P0配置Validator与自动验收底座需求.md
  - agent_status/art.md
  - 知识库/views/pm.md
  - 知识库/views/art.md
  - 知识库/views/design.md
  - 知识库/views/program.md
  - tools/agent/README.md
  - tools/p3-mission/README.md
last_verified: 2026-06-08
update_rule: 项目阶段、总目标、跨职能交接或阻塞项变化时更新本文件。
---

# 项目状态

> 项目级状态页。复制出来的智能体如果改变了当前里程碑、跨职能交接、阻塞项或下一步总优先级，需要更新这里。

## 最后更新

2026-06-08

## 当前阶段

正式版核心纵切。

项目已经从 MVP 闭环验证转入正式版纵切开发。这里的“纵切”是开发顺序，不是质量降级：被选中的系统按正式版标准完整处理。当前开发应按纵向链路推进：

```text
设计规则 -> 配置结构 -> 运行行为 -> UI/表现 -> 验证 -> 文档同步
```

## 当前顶层目标

`版本规划/09_正式版核心纵切开发路线.md`

## 当前版本规划入口

`版本规划/README.md`

## 当前优先级

1. C1-C3 配置程序支持先收口：策划前三层配置源已完成，程序侧已完成当前可做的同步、静态审计和 P0 入口接入；后续只在 Unity runtime 补跑或人工体验发现真实阻断时按 bug 修复追加处理。
2. 继续开发“功能未开放完”的主流程闭环：以 `开发文档/16_程序主流程闭环与架构收口推进计划.md` 的 `FLOW-01..FLOW-07` 为准，围绕小镇出发、层选择、地图、战斗 / 非战斗节点、战利品、阶梯 / 撤离、出售、维护、再下潜做玩家正常 UI 可达和真实领域服务回写。
3. 程序架构优化 / 收口紧随主流程推进：以 `ARCH-01..ARCH-05` 为准，当前已完成职责审计、首批工坊领域操作服务化、只读快照 / 可操作服务配对；下一步优先收紧 Validator、P0 报告字段、Unity smoke 发现机制和 `validation_limited` 语义。
4. UI / 美术接入跟随可玩闭环：有 Approved 资源和 active UI 规格时一起接入；ArtAcceptance、debug preview、只读快照或静态面板不能替代“玩家正常 UI 可达 + 调用真实领域服务 + 状态真实变化 + smoke / P0 证据”。
5. 策划 / 配置侧继续按 `agent_status/design.md` 和 `设计文档/config/26_正式配置设计与填充推进计划.md` 推进后续配置，C1-C3 不再重复派发，只按真实缺口做补齐、验收修复、字段迁移或数值校准。
6. 第四层以后副本内容包、完整房间经营、完整好感剧情、完整势力线和大量内容铺量暂缓，避免压过当前正式版核心纵切。

当前程序正式版本完成口径：以 `agent_status/program.md`、开发文档、代码事实和验收证据为准。只读数据层、ArtAcceptance 截图、debug 入口或静态面板不能直接标记为可玩完成；服务存在但正常 UI 不可达，仍属于“功能未开放完”。本轮程序推进的执行顺序是 `CFG-01..02 -> FLOW-01..07 -> ARCH-01..05 -> REVIEW-01`。

## 职能状态页

- PM / 版本规划：`agent_status/pm.md`
- 美术 / UI：`agent_status/art.md`
- 策划 / 数值：`agent_status/design.md`
- 程序 / Unity：`agent_status/program.md`

## 最近完成

- PM / 工具侧已调整 `p3-mission` 来源门禁：新建 mission 必须提供详细来源材料作为 `source_ref`，来源可为 Markdown、目录、非 Markdown 文档、事实来源或已批准计划；脚本不硬判格式 / 长度，只在缺来源时提醒，是否 mission-ready 由 agent 按 skill 判断。不得从一句话目标直接生成；`p3-mission` 只负责阶段进度规划、执行、证据回写和恢复，缺来源时先回到对应职能补文档。
- PM / 工具侧已将 `misc/Missions` 的长期任务机制按 P3 规范收敛进 `p3-mission`：项目只维护 `.codex/skills/p3-mission/` 和 `tools/p3-mission`，旧 `misc/Missions` 已归档为上游参考，不再作为 active skill 或第二套路由。
- 策划侧已完成 C3 第三层路线侵蚀配置源落地：`L3-ITEMS`、`L3-REWARDS`、`L3-MONSTERS`、`L3-DUNGEONS`、`L3-ORDERS-RUMORS-EVENTS`、`L3-SEED` 均已在 `设计文档/config/26_正式配置设计与填充推进计划.md` 标记为 `配置完成`。本轮新增第三层 12 件物品、15 个奖励、8 个怪物、4 个订单、3 条传闻、1 个新 Faction 和 `配置表(JSON)/Dungeons/layer_3.json`；`layer_3.json` 已具备 `DirectStartAllowed=true`、`PreviousLayerRequired=2`、Boss `boss_mycelium_oracle`、Boss 后免费恢复 HP / SAN 的 SafeZone、路线侵蚀 / 订单冲突 MapProfile 和 5 条固定样例。已通过 C3 静态交叉引用检查（error / warning 为 0）和 `./tools/config/Sync-Configs.ps1 -Clean`；P0 入口已运行，`ConfigValidator` / Unity smoke 因 Unity Editor 未运行阻塞，ArtAcceptance 为既有 UI / 美术验收问题，未作为 C3 配置失败证据。C4 已解锁为 `待开始`，当前子项为 `GROWTH-ID-LOCK`。
- 策划侧已完成 C2 第二层直达与背包压力配置源落地，并完成 C2 收尾 / C3 开工口径同步：`L2-ITEMS`、`L2-REWARDS`、`L2-MONSTERS`、`L2-DUNGEONS`、`L2-ORDERS`、`L2-SEED` 均已在 `设计文档/config/26_正式配置设计与填充推进计划.md` 标记为 `配置完成`。本轮新增 / 补强第二层 12 件物品、14 个奖励、7 个怪物、3 个订单、2 个必要 Faction 引用和 `配置表(JSON)/Dungeons/layer_2.json`；`layer_2.json` 已具备 `DirectStartAllowed=true`、`PreviousLayerRequired=1`、Boss `boss_spore_foundry`、Boss 后免费恢复 HP / SAN 的 `StairsNode + SafeZoneRules`，并补 5 条固定样例。已通过 C2 静态 Validator 等价检查和 `./tools/config/Sync-Configs.ps1 -Clean`；`ConfigValidationSmokeTest.Run` / P0 Unity 步骤因 Unity Editor 未运行被工具阻塞，未作为通过证据。C3 已解锁为 `待开始`，当前子项为 `L3-ITEMS`；后续只按缺口修复处理 C1 / C2，不重复派发基础配置。
- 策划侧已完成 C1 第一层正式配置源收口：`L1-ITEMS`、`L1-REWARDS`、`L1-MONSTERS`、`L1-DUNGEONS`、`L1-SEED` 均已在 `设计文档/config/26_正式配置设计与填充推进计划.md` 标记为 `配置完成`。`layer_1.json` 已补 2 个 MapProfile、11 个 NodePool、5 条 FixedSeedSamples 和 9 条 ValidationScenarioRefs；Boss 指向 `boss_gatekeeper_mk1`，`elite_scrap_guard` 只作为可绕精英；Boss 后 `StairsNode + SafeZoneRules` 为免费恢复 HP / SAN、允许撤离 / 深入、不清污染 / 订单 / 探索账本。已通过 L1-SEED 静态证据检查、`Sync-Configs.ps1 -Clean`、`ConfigValidationSmokeTest.Run`、`DungeonSeedAcceptanceSmokeTest.Run`、`RewardSystemSmokeTest.Run`、`ItemLifecycleServiceSmokeTest.Run` 和 Boss 保底结构检查。该历史记录已被 C2 完成记录推进；当前下一项为 C3 `L3-ITEMS`。
- 策划侧已完成 `设计文档/` 目录结构重构：GDD、规则卡、交付承接、内容包和正式配置链路已分别迁入 `GDD/`、`rules/`、`delivery/`、`content_packs/`、`config/` 分区；`设计文档/README.md` 已重写为目录地图和名词词典，明确“配置承接审计”是配置源缺口审计，“正式配置落地设计”是 JSON 修改前配置设计清单。
- 项目结构整理已提交：`d5434b8 chore: tidy project structure`。
- `tools/ai-image-gateway` 已登记为 submodule。
- 运行时 Prefab 已移动到 `UnityClient/Assets/Prefabs`。
- 已从受版本控制的 Unity 工程路径中移除旧 UI Toolkit 运行时资产。
- 已新增 `tools/config/Sync-Configs.ps1`，用于把 `配置表(JSON)` 同步到 Unity `StreamingAssets`。
- `AGENTS.md` 已扩展为复制智能体的统一入口。
- 已新增项目总状态页和美术 / 策划 / 程序三个职能状态页。
- 知识库智能体已完成轻量知识库首版：元数据规范、核心文档元数据头、索引生成和校验脚本。
- 已为当前索引内全部 Markdown 文档补齐元数据头，并重新生成 `DOCS_INDEX.md` 与 `docs_index.json`。
- 已把文档 `related` 元数据升级为双向关系网，让美术、程序、策划、配置、数值和版本路线文档形成可校验互链。
- 知识库校验已补上索引新鲜度检查，会扫描当前 Markdown 清单并与 `docs_index.json` 比对，避免新增文档未入索引时误通过。
- 已新增 `知识库/views/art.md`、`知识库/views/design.md`、`知识库/views/program.md`，作为复制智能体按职能开工的阅读入口。
- 已明确 `UnityClient/Assets/StreamingAssets/Configs` 是由 `配置表(JSON)` 同步生成的运行时副本，不作为知识库事实来源。
- 美术智能体已建立 UI 设计版本化流程：MVP Baseline 冻结、Formal V1 结构设计、active `screen_layouts.json` 确认后更新、再进入素材生成和程序接入。
- P0 UI MVP Baseline 已运行时验收通过；下一步不是继续美化 MVP 结构，而是先重审 Formal V1 战斗/工坊/拾取等正式结构。
- PM / 版本规划智能体已建立状态页和阅读入口，用于维护正式版核心纵切的阶段判断、里程碑、优先级和跨职能交接。
- 已新增 `tools/agent/Invoke-AgentHealthCheck.ps1`，用于复制智能体开工前检查工作区脏文件、易误提交路径、submodule 风险和知识库校验状态。
- PM 已将 `版本规划/09_正式版核心纵切开发路线.md` 收敛为正式版宏观总线，只承载阶段判断、功能优先级和主线边界。
- 策划侧已统一正式版纵切口径：纵切只代表系统推进顺序，不代表降低完成标准；深渊地图规则已改为正式版节点网络生成。
- 策划侧已新增 `设计文档/delivery/00_策划文档开发交付审计.md`，按开发交付标准审计 `GDD_00` 到 `GDD_12`，明确当前 P0 缺口是局外时间口径统一、物品背包规则卡、战斗怪物意图规则卡和深渊地图生成开发规则卡。
- 策划侧已新增 `设计文档/rules/01_局外时间与日程口径规则卡.md`，并同步修正 GDD 中旧局外 AP 口径；局外时间统一为 Day / Week / Month，局内 AP 只用于战斗回合。
- 策划侧已更新交付审计：深渊地图生成规则已并入 `GDD_02`，可作为程序第一版地图生成器依据；下一步转为配置 README 和 Validator 对齐。
- 策划侧已新增 `设计文档/rules/02_物品背包旋转与生命周期规则卡.md`，统一物品定义、实例状态、容器、旋转放置、战败 / 撤离 / 出售 / 制造和怪物对包干涉规则。
- 策划侧已新增 `设计文档/rules/03_战斗回合与怪物意图规则卡.md`，统一战斗回合状态机、局内 AP、护盾、怪物意图、目标选择、多怪行动顺序和胜负结算规则；`GDD_01` 已从“需规则卡”调整为“可开发”。
- 策划侧已新增 `设计文档/rules/04_小镇经济结算与压力链规则卡.md`，统一每日营业结算、售价公式、传闻价格波、违禁品隔夜代价、月租账单和欠债裁决；`GDD_04` 已从“需规则卡”调整为“可开发”。
- 策划侧已新增 `设计文档/rules/05_局外成长与维护规则卡.md`，统一底盘、义体、心智天赋、维护、换装、成长实例字段和下潜许可检查；`GDD_08` 已从“需规则卡”调整为“可开发”。
- 策划侧已新增 `设计文档/rules/06_标签与特质规则卡.md`，统一标签命名、动态标签生命周期、查询表达、特质触发器、事件监听白名单、互斥 / 优先级 / 叠加、消除转化和持久化规则；`GDD_09` 已从“需规则卡”调整为“可开发”。
- 策划侧已新增 `设计文档/rules/07_势力声望与订单规则卡.md`，统一势力字段、订单定义 / 实例字段、订单刷新算法、截止日状态机、奖励引用、声望阈值、黑市背叛和委托物绑定；`GDD_10` 已从“需规则卡”调整为“可开发”。
- 策划侧已新增 `设计文档/rules/08_人偶核心状态与好感双轨规则卡.md`，统一人偶实例字段、SAN / Bond 阈值、情绪状态机、奇迹裁决、崩溃处置、特质输入事件和与交互系统的分工；`GDD_03` 已从“需规则卡”调整为“可开发”。
- 策划侧已新增 `设计文档/rules/09_人偶交互事件与反馈规则卡.md`，统一交互事件结构、触摸 / 对话 / 赠礼 / 保养 / 特殊交互、每日上限、偏好、反馈选择、对话池抽取和验收样例；`GDD_12` 已从“需规则卡”调整为“可开发”。
- 策划侧已新增 `设计文档/rules/10_剧本调度与事件队列规则卡.md`，统一剧本事件字段、触发器表达式、事件队列优先级、对话节点结构、系统指令、跳过 / 重入和存档旗标；`GDD_05` 已从“暂不交付”调整为“可开发”。
- 策划侧已新增 `设计文档/rules/11_人偶房间布局与视觉叙事规则卡.md`，统一房间布局数据、家具槽、展示品、纪念物、窗外状态、待机表现、资源 fallback 和验收样例；`GDD_11` 已从“暂不交付”调整为“可开发”。
- 美术侧已把 `combat_hud` 写入 Formal V1 active 规格：底部居中背包、左玩家/右敌方实体舞台、敌人脚下血条，并扩展 Manifest 扫描 `CombatVisualID` 生成 `monster_*_combat` 战斗实体需求。
- 美术侧已完成 `combat_hud` Formal V1 第一批战斗资源入库：4 个 `monster_*_combat` 透明战斗实体、`ui_combat_entity_shadow` 和 `ui_combat_target_ring`，并补齐 Unity `.meta`。
- 美术侧已新增可接入素材清单：`美术文档/_generated/可接入素材清单.md/json`，后续每次生成或同步 Approved 素材后刷新，程序侧可按 `program_integrate` 队列自助接入。
- 策划侧已新增 `设计文档/content_packs/21_人偶成长修复内容包.md`，把人偶成长修复主干的底盘、义体、维护、特质、交互反馈、房间纪念物和联合验收路径整理为正式内容包。
- 策划侧已新增 `设计文档/content_packs/22_小镇经济月租内容包.md`，把小镇经济主干的传闻、顾客、账单、违禁品、月租曲线、典当和联合验收路径整理为正式内容包。
- 策划侧已新增 `设计文档/content_packs/23_长期记忆剧情内容包.md`，把长期叙事主干的主线事件、Bond 事件、债务事件、深渊 lore、房间纪念物、日记、窗外状态、待机规则和联合验收路径整理为正式内容包。
- 策划侧已完成交付审计 2.0：`设计文档/delivery/00_策划文档开发交付审计.md` 和 `设计文档/delivery/12_策划交付落地矩阵.md` 已把 `19` 到 `23` 正式内容包纳入“内容包可拆分”交付口径。
- 策划侧已新增 `设计文档/content_packs/24_势力订单声望内容包.md`，把势力、声望 / 信任阶梯、订单池、订单模板、奖励、黑市背叛和联合验收路径整理为独立正式内容包；交付审计和落地矩阵已同步为 `19` 到 `24` 内容包口径。
- 策划侧已新增 `设计文档/content_packs/25_第三层路线侵蚀内容包.md`，把第三层“菌脉深庭”的物品、怪物、地图画像、路线侵蚀、污染物品生命周期、多势力订单冲突、Boss 和联合验收路径整理为正式内容包；前三层深渊内容递进已形成正式设计链路。
- 策划侧已新增 `设计文档/README.md` 作为设计文档阅读入口，明确 `GDD_00` 到 `GDD_12` 是系统设计母文档，`01` 到 `11` 是规则卡，`12` 到 `17` 是交付 / 验收承接，`18` 是内容生产规格，`19+` 是具体内容包；第四层组合压力内容包已归入后续层内容草案，不列入当前优先推进范围。
- PM 侧已将功能开发优先级从“按层数 / 内容包推进”调整为“配置校验底座 -> 背包战斗 -> 深渊地图 -> 局外成长 -> 经济目标 -> 表现支撑”；第四层以后与大量内容铺量暂缓。
- 程序需求侧已新增 `开发文档/15_P0配置Validator与自动验收底座需求.md`，明确 P0 配置校验、统一验收命令、报告格式、门禁等级和 seed 回归标准。
- PM 侧已将原 `11` / `12` 职责合并为 `版本规划/11_纵切批次与需求文档承接矩阵.md`，核对 `09` 中 P0-P5 和 GDD 批次职责均有详细文档承接；后续新增版本规划需求必须同步 `11` 审计。
- UI 程序侧已接入 `combat_hud` Formal V1：运行时展示左玩家、右敌人实体、底部居中背包、敌人脚下血条和目标光环；ArtAcceptance `20260524_043441` 已通过，下一步交美术侧做正式截图验收。
- 美术侧已将 A3 房间节点 `safe_room` / `stairs_room` 写入 Formal V1 active UI 规格，并刷新 `20260524_065808_a3_rooms_formal_v1_active_repo_state` 可接入素材快照；程序侧可按 active `screen_layouts.json` 分批接入。
- UI 程序侧已按 `美术文档/_generated/可接入素材清单.md` 的 `program_integrate` 区块接入 17 个 Approved VisualID，并绑定 `combat_hud`、`maintenance_panel`、`daily_bill_report`、`shop_staging`、`order_board`、`rumor_board`；ArtAcceptance `20260524_212423` 已通过，15 个截图点均 captured，`MissingRequiredVisualIDs=0`。
- PM 侧已按用户反馈完成版本规划结构收口：active 目录只保留 `09` / `11` / `README`；`09` 是正式版宏观总线，承载长期节点、节点门禁、职能进度入口和防重复派发规则；`11` 是纵切批次与需求文档承接门禁。详细进度按职能回到 `agent_status/*` 和对应事实文档；`26` 只承载策划 / 配置侧正式配置计划，不混写程序或美术工作。`12_正式版长期版本节点规划.md` 仅保留为旧链接兼容页，不再作为 active 事实来源。
- 策划侧已新增 `设计文档/config/audits/27_Items正式配置承接审计.md`，作为 `26` W1 的 Items 首轮审计成果；该文档只说明字段、内容和 Validator 缺口，不代表配置 JSON 已完成。
- 策划侧已新增 `设计文档/config/audits/28_Monsters正式配置承接审计.md`，作为 `26` W1 的 Monsters 首轮审计成果；该文档明确当前 4 个怪物 JSON 与前三层内容包的差异、Boss / 精英职责错位、意图可读字段缺口和 Validator 建议，不代表怪物配置已完成。
- 策划侧已新增 `设计文档/config/audits/29_Dungeons正式配置承接审计.md`，作为 `26` W1 的 Dungeons 首轮审计成果；该文档明确当前一二层配置与前三层地图画像、NodePool、BossNode、安全区语义和 seed 验收之间的差距，不代表层级 JSON 已完成。
- 策划侧已新增 `设计文档/config/audits/30_Rewards正式配置承接审计.md`，作为 `26` W1 的 Rewards 首轮审计成果；该文档明确当前 8 个奖励 JSON 与前三层奖励、正式 Boss 保底、订单 / 成长 / 节点奖励引用和 Validator 建议之间的差距，不代表奖励配置已完成。
- 策划侧已按 `30_Rewards正式配置承接审计.md` 扩展 `配置表(JSON)/Rewards/README.md` 正式字段口径；该动作只完成 Rewards 字段说明承接，不代表奖励 JSON 已完成。
- 策划侧已按 `27_Items正式配置承接审计.md` 扩展 `配置表(JSON)/Items/README.md` 正式字段口径；该动作只完成 Items 字段说明承接，不代表物品 JSON 已完成。
- 策划侧已按 `28_Monsters正式配置承接审计.md` 扩展 `配置表(JSON)/Monsters/README.md` 正式字段口径；该动作只完成 Monsters 字段说明承接，不代表怪物 JSON 已完成。
- 策划侧已按 `29_Dungeons正式配置承接审计.md` 扩展 `配置表(JSON)/Dungeons/README.md` 正式字段口径；该动作只完成 Dungeons 字段说明承接，不代表层级 JSON 已完成。
- 策划侧已新增 `设计文档/config/designs/31_第一层正式配置落地设计.md`，作为 `26` W2 的第一层配置落地设计产物；该文档把第一层内容包和四份配置审计合成为后续改 JSON 前的 Items / Rewards / Monsters / Dungeons 策划清单，不代表第一层 JSON 已完成。
- 策划侧已新增 `设计文档/config/designs/32_第二层正式配置落地设计.md`，作为 `26` W2 的第二层配置落地设计产物；该文档把第二层内容包和四份配置审计合成为后续改 JSON 前的 Items / Rewards / Monsters / Dungeons / Orders 策划清单，不代表第二层 JSON 已完成。
- 策划侧已新增 `设计文档/config/designs/33_第三层正式配置落地设计.md`，作为 `26` W2 的第三层配置落地设计产物；该文档把第三层内容包和四份配置审计合成为后续改 JSON 前的 Items / Rewards / Monsters / Dungeons / Orders / Events / Rumors / RoomMemory 策划清单，不代表第三层 JSON 已完成。
- 策划侧已同步 `26` 和 `09`：W2 前三层正式配置补齐在策划设计层已闭合，下一步应拆前三层配置实现任务或补 Validator 样例；第四层以后仍保持后续内容生产批次。
- 策划侧已新增 `设计文档/config/tasks/34_前三层正式配置实现任务拆分.md`，把 `31` / `32` / `33` 拆成后续可派发的 `L1/L2/L3` 配置实现任务、依赖顺序、交付证据、Validator / seed 验收要求和完成判定；该文档不修改 JSON，也不代表前三层配置已完成。
- 策划侧已新增 `设计文档/config/validation/35_前三层正式配置Validator与固定Seed验收样例.md`，把 `34` 的 `L1/L2/L3` 任务接到 `CFG-*` Validator、`V-L1/L2/L3-*` 固定 seed、失败信号和回写证据模板；该文档是验收样例设计，不代表配置 JSON 已完成。
- 策划侧已新增 `设计文档/config/audits/36_局外成长正式配置承接审计.md`，完成 `Chassis`、`Prosthetics`、`CraftingRecipes`、`Dolls`、`Effects` 的首轮配置承接审计，明确当前配置仍是 MVP / 原型口径，并输出字段缺口、内容缺口、Validator 建议和后续 `GROWTH-*` 任务边界；该文档不修改 JSON，也不代表局外成长配置完成。
- 策划侧已新增 `设计文档/config/designs/37_局外成长正式配置落地设计.md`，把 `36` 的审计缺口和 `21` 的内容包拆成 `GROWTH-README`、`GROWTH-CHASSIS`、`GROWTH-PROSTHETICS`、`GROWTH-CRAFT-MAINT`、`GROWTH-DOLL-TRAIT-ROOM` 的 JSON 修改前策划清单；该文档不修改 JSON，也不代表局外成长配置完成。
- 策划侧已完成 `GROWTH-README`：`配置表(JSON)/Chassis`、`Prosthetics`、`CraftingRecipes`、`Dolls`、`Effects` 五个 README 已同步正式字段口径、运行时边界和 Validator 建议；该动作只完成字段说明承接，不代表局外成长 JSON 已完成。
- 策划侧已新增 `设计文档/config/tasks/38_局外成长正式配置实现任务拆分.md`，把 `37` 的局外成长配置清单拆成 `GROWTH-ID-LOCK`、`GROWTH-CHASSIS`、`GROWTH-EFFECTS-TRAITS`、`GROWTH-PROSTHETICS`、`GROWTH-CRAFT-MAINT`、`GROWTH-DOLL-TRAIT-ROOM`、`GROWTH-VALIDATION` 的派发顺序、交付证据和验收口径；该文档不修改 JSON，也不代表局外成长配置完成。
- 策划侧已新增 `设计文档/config/gates/39_局外成长正式配置ID锁定与冲突检查.md`，完成 `GROWTH-ID-LOCK` 当前工作区 ID 冲突检查：旧 MVP ID 与正式 ID 无阻塞冲突，`maint_basic_patch` / `maint_purification_flush` 已被当前 JSON 占用且与设计一致；该文档不修改 JSON，也不代表局外成长配置完成。
- 策划侧已新增 `设计文档/config/designs/40_局外成长底盘正式配置落地设计.md`，完成 `GROWTH-CHASSIS` 的 3 个正式底盘字段级设计、旧 ID 迁移口径、Validator 和固定验收样例；该文档不修改 JSON，也不代表底盘配置完成。
- 策划侧已新增 `设计文档/config/designs/41_局外成长效果特质正式配置落地设计.md`，完成 `GROWTH-EFFECTS-TRAITS` 的义体效果、维护效果、动态标签、8 个特质、Validator 和固定验收样例字段级设计；该文档不修改 JSON，也不代表效果 / 特质配置完成。
- 策划侧已新增 `设计文档/config/designs/42_局外成长义体正式配置落地设计.md`，完成 `GROWTH-PROSTHETICS` 的 6 个正式义体字段级设计、旧 ID 迁移、`RightHand` 槽位兼容、Validator 和固定验收样例；该文档不修改 JSON，也不代表义体配置完成。
- 策划侧已新增 `设计文档/config/designs/43_局外成长制造维护正式配置落地设计.md`，完成 `GROWTH-CRAFT-MAINT` 的 6 个义体制造配方、2 个底盘解锁配方、5 个维护方案、材料来源、消耗 / 取消策略、Validator 和固定验收样例字段级设计；该文档不修改 JSON，也不代表配方或维护配置完成。
- 策划侧已新增 `设计文档/config/designs/44_局外成长人偶特质房间正式配置落地设计.md`，完成 `GROWTH-DOLL-TRAIT-ROOM` 的人偶静态档案、运行时拆分、8 个特质、8 条反馈、8 个房间纪念物、Validator 和固定验收样例字段级设计；该文档不修改 JSON，也不代表人偶 / 特质 / 反馈 / 房间记忆配置完成。
- 策划侧已新增 `设计文档/config/validation/45_局外成长正式配置Validator与固定验收样例.md`，完成 `GROWTH-VALIDATION` 的 Validator 覆盖矩阵、固定验收样例、warning / error 分级和状态回写证据模板；该文档不修改 JSON，也不代表局外成长配置完成。
- 策划侧已新增 `设计文档/config/audits/46_经济压力正式配置承接审计.md`，完成 `DES-V4-001` 的经济压力首轮配置承接审计，明确当前 `Economy` / `Orders` / `Factions` / `Rumors` 只有最小样例骨架，并输出正式字段缺口、内容缺口、Validator 建议和后续 `ECON-*` 任务边界；该文档不修改 JSON，也不代表经济压力配置完成。
- 策划侧已新增 `设计文档/config/designs/47_经济压力核心正式配置落地设计.md`，完成 `ECON-CORE` 的 JSON 修改前字段级设计，覆盖日账单、周报、月租、出售渠道、顾客、违禁品、NightTax、DeadlockGuard、Validator 和固定验收样例；该文档不修改 JSON，也不代表经济压力配置完成。
- 策划侧已新增 `设计文档/config/designs/48_经济压力传闻正式配置落地设计.md`，完成 `ECON-RUMORS` 的 JSON 修改前字段级设计，覆盖 8 条正式传闻、传闻字段、刷新组合规则、来源追踪、Validator 和固定验收样例；该文档不修改 JSON，也不代表传闻配置完成。
- 策划侧已新增 `设计文档/config/designs/49_经济压力势力正式配置落地设计.md`，完成 `ECON-FACTIONS` 的 JSON 修改前字段级设计，覆盖 5 个正式势力、正规声望阶梯、黑市信任阶梯、订单池引用、冲突规则、Validator 和固定验收样例；该文档不修改 JSON，也不代表势力配置完成。
- 策划侧已新增 `设计文档/config/designs/50_经济压力订单正式配置落地设计.md`，完成 `ECON-ORDERS` 的 JSON 修改前字段级设计，覆盖 5 个订单池、19 条订单模板、需求表达、失败 / 背叛裁决、Validator 和固定验收样例；该文档不修改 JSON，也不代表订单配置完成。
- 策划侧已新增 `设计文档/config/validation/51_经济压力正式配置Validator与固定验收样例.md`，完成 `ECON-VALIDATION` 的 Validator 覆盖矩阵、跨域检查、固定验收样例、warning / error 分级和 `DES-V4-001` 回写证据模板；该文档不修改 JSON，也不代表经济压力配置完成。
- 策划侧已新增 `设计文档/config/tasks/52_经济压力正式配置实现任务拆分.md`，完成 `ECON-TASKS` 的配置实现派发层，明确 `ECON-ID-LOCK -> ECON-README-CHECK -> ECON-CORE-JSON -> ECON-FACTIONS-JSON -> ECON-RUMORS-JSON -> ECON-ORDERS-JSON -> ECON-CROSS-REFS -> ECON-VALIDATION-RUN -> ECON-STATUS-WRITEBACK` 的顺序、交付证据和状态回写口径；该文档不修改 JSON，也不代表经济压力配置完成。
- 策划侧已新增 `设计文档/config/gates/53_经济压力正式配置ID锁定与冲突检查.md`，完成 `ECON-ID-LOCK` 当前工作区 ID 冲突检查：阻塞性 ID 冲突为 0，`economy_town_v1` 和 `faction_mechanic_workshop` 可复用扩展，`rumor_mechanical_price_up` 与 `order_mechanic_scrap_drive` 后续需迁移或标记为旧样例；该文档不修改 JSON，也不代表经济压力配置完成。
- 策划侧已新增 `设计文档/config/gates/54_经济压力正式配置README字段口径检查.md`，完成 `ECON-README-CHECK` 策划证据：`Economy` / `Factions` / `Rumors` / `Orders` 四个 README 已能承接后续 JSON 实现字段口径，旧样例迁移 warning 已记录；该文档不修改 JSON，也不代表经济压力配置完成。
- 策划侧已新增 `设计文档/config/gates/55_经济压力订单ID最终锁定表.md`，锁定经济压力首批 5 个正式 OrderPoolID 和 19 个正式 OrderID；旧 `order_pool_*` 只作为草案迁移别名，`order_mechanic_scrap_drive` 不进入正式订单池。该文档不修改 JSON，也不代表订单配置完成。
- 策划侧已新增 `设计文档/config/gates/56_正式配置源落地准入门禁.md`，统一前三层、局外成长和经济压力进入配置源 JSON 实现前的设计准入、实现证据和状态回写口径；该文档不修改 JSON，也不代表任何配置源已完成。
- 美术侧已完成 local_v0 质量层级规范化、P0 新节点图标、P1 核心战斗意图图标和 P1 首批战斗反馈 / 标记 NovelAI 正式替换：`node_eventnode_icon`、`node_hazardnode_icon`、`node_reststopnode_icon`、`node_treasurenode_icon`、`ui_combat_intent_attack`、`ui_combat_intent_defend`、`ui_combat_intent_buff`、`ui_combat_intent_debuff`、`ui_combat_feedback_hit`、`ui_combat_feedback_shield_break`、`ui_combat_grid_lock_marker` 已同步为 `formal_ai_v2`；latest 程序交接清单为 `program_integrate=0`、`add_capture=0`、`rerun_acceptance=0`，Visual V2 后续替换批次为 `nai_v2a_runtime_quality_20260608_01`。
- 美术侧已完成 Formal V1 素材接入运行时验收：ArtAcceptance `20260527_002436` 工具层 `PASSED`、21/21 captured、Registry 191、MissingRequiredVisualIDs=0，latest 程序交接清单 `program_integrate=0`、`add_capture=0`、`rerun_acceptance=0`；人工画面验收结论为资源接入通过、画面不完全通过，`combat_hud`、`dungeon_map`、`safe_room`、`stairs_room`、`sell_panel`、`prosthetic_panel` 需程序侧清理截图状态或补展示数据后复验。
- 美术侧已启动 Formal V2 UX/UI 重构设计层：先不修改 active `screen_layouts.json`，而是在 `美术文档/ui_design/formal_v2/` 建立总方案和 V2-A 五个核心界面入口，优先解决 Formal V1 运行时仍像按钮菜单 / debug 面板的问题。
- 美术侧已完成 Formal V2-A active 迁移：`workshop_main`、`combat_hud`、`inventory_loot`、`dungeon_map`、`settlement` 已在 `美术文档/ui_design/screen_layouts.json` 标记为 `StructureVersion=FormalV2`，`Validate-UIDesign.ps1` 通过；当前程序交接为 `program_integrate=0`、`add_capture=0`、`rerun_acceptance=16`，缺图生成队列为 `generate_needed=29`。

## 跨职能交接

- 美术/UI 与程序智能体都应以纯 UGUI 作为运行时 UI 目标。
- 策划与程序智能体做运行时验证前应先同步配置：`.\tools\config\Sync-Configs.ps1 -Clean`。
- 程序侧需要修正 `CombatLootDropTest.Run` 的旧奖励断言：当前正式配置要求 `boss_gatekeeper_mk1` 保底 `mat_core_tier1`，`elite_scrap_guard` 只作为可绕精英；该测试仍断言精英奖励包含 `mat_core_tier1`。策划侧已用结构检查确认 Boss 保底和精英非保底成立，程序侧后续应把测试改为 Boss 保底口径。
- 美术智能体刷新 Manifest 前应先同步配置，确保视觉需求跟随当前配置源。
- 美术 / UI 的程序接入只以 `美术文档/ui_design/screen_layouts.json` 当前 active 规格为准；`versions/` baseline 和 candidate 不作为程序接入口。
- Formal V1 / Formal V2 UI 文档由美术侧先提出，用户确认后再更新 active 规格、生成素材并交给程序接入；`formal_v2/*.md` 在确认前只是 UX/UI draft，程序侧不得直接按 draft 接入。当前 V2-A 五屏已经完成 active 迁移，程序侧可按 active `screen_layouts.json` 接入；V2-B / V2-C 仍是 draft。
- 美术侧每次生成或同步 Approved 素材后会刷新 `美术文档/_generated/可接入素材清单.md/json`；程序侧接入新素材前优先查看其中 `program_integrate` 条目。
- 当前 latest `program_integrate=58`、`generate_needed=0`、`rerun_acceptance=16`。`program_integrate=58` 表示美术侧缺图批次 `nai_formalv2_missing_20260608_01` 已真实 NovelAI 生成并同步 58 个新增 Approved VisualID，程序侧需要按 `美术文档/_generated/可接入素材清单.md` 登记 / 接入；这批包含 23 个物品图标、34 个怪物战斗 / 头像素材和 1 个背景。`rerun_acceptance=16` 是仍为 FormalV1 的非 V2-A 屏截图早于当前 active 规格日期，不代表 V2-A 五屏已完成运行时接入。V2-A 五屏需要程序侧按 active FormalV2 规格重排运行时 UI。美术侧已完成 `nai_formalv2_quality_20260608_01_p1_combat_readability` 10 个既有 UI 资源同名替换为 `formal_ai_v2`，保持同 `VisualID`、同 Approved 路径、同 Unity `.meta` / GUID，因此这些替换项程序侧无需重新登记；剩余 VisualV2 同名替换为 111 项。
- Formal V1 UI 的“资源登记完成”和“运行时截图覆盖完成”不等于“玩家可玩接入完成”。后续程序侧声明某界面可玩完成前，必须同时补齐玩家主流程可达入口，以及关键按钮 / 操作调用真实后端或领域服务的闭环证据；ArtAcceptance、debug 入口、验收专用 preview 对象或只读快照只能作为资源 / 截图接入证据。
- `program_integrate` 17 个 Approved VisualID 曾完成 UI 程序接入记录；按当前正式版本完成口径，该记录只作为历史接入事实，不计为 P5 表现支撑正式版本完成。美术侧如继续推进，应基于 ArtAcceptance 截图验收后回写 `agent_status/art.md`，如影响长期节点门禁再通知 PM 更新 `09`。
- 长任务、恢复继续、跨会话拆分和 mission 请求统一使用 `.codex/skills/p3-mission/`；`tools/p3-mission` 是独立工具源码仓库。新建 mission 必须有详细来源材料，可通过 `-Source` / `-SourceSpec` 记录为 `source_ref`，一句话目标不能直接进入 mission；`misc/Missions` 已归档，不再作为 active skill、任务路由或第二套 CSV 执行系统。
- 任意智能体修改系统规则时，必须更新对应 GDD 或开发文档，不能只改代码或配置。
- 任意智能体新增或调整文档关联时，必须维护 `related` 双向互链，并运行 `.\tools\docs\Validate-Docs.ps1`。
- 复制智能体需要快速定位上下文时，优先读取 `知识库/views/` 下对应职能入口，再进入事实来源文档。
- 配置相关问题以 `配置表(JSON)` 为源；不要手写修改 `UnityClient/Assets/StreamingAssets/Configs` 运行时副本。
- PM / 版本规划智能体负责维护 `09` 路线、`PROJECT_STATUS.md` 和跨职能版本拆分；变更阶段、优先级、里程碑或交接时必须同步目标职能状态页。
- 当前版本规划以 `版本规划/09_正式版核心纵切开发路线.md` 为宏观总线，同时承载长期版本节点、节点门禁、职能进度入口和防重复派发规则；GDD 批次与需求文档门禁以 `版本规划/11_纵切批次与需求文档承接矩阵.md` 为准；不再维护单独的 `10` 执行层文档、独立 `12` active 规划文档或跨职能进度台账文档。
- `版本规划/11_纵切批次与需求文档承接矩阵.md` 不记录实现进度；实现状态、证据入口和验收状态统一回到对应职能状态页和事实文档。
- 任意智能体完成进入正式纵切的开发、配置、策划、美术、UI 或验收工作后，必须更新对应职能状态页。同一系统涉及多个职能时拆开记录，不写跨职能混合执行行。`09` 不复述各职能详细方案，只在长期节点、节点门禁、职能入口、状态标记规则或防重复派发规则变化时更新。配置类工作要区分策划 / 配置设计完成与配置源完成，README 或修改前设计不能直接标记为 `配置完成`。

## 问题 / 阻塞

- 当前工作区已有前序 UI、美术、生成物和 submodule 相关脏文件。后续智能体开工前应先运行 `.\tools\agent\Invoke-AgentHealthCheck.ps1`，提交时严格收窄范围。
- `CombatLootDropTest.Run` 旧精英保底断言已按正式 Boss 保底口径修正；后续若 Unity runtime 补跑发现新阻断，再按程序 bug 追加处理。
- `tools/ComfyUI_NAIDGenerator/` 当前未跟踪，后续需要决定它是 vendor 代码、submodule，还是本地专用工具。
- `tools/ai-image-gateway` 子模块内部有未提交改动；如需处理，应进入子模块内部单独处理。

## 下一步总建议

1. 程序侧继续执行 `开发文档/16_程序主流程闭环与架构收口推进计划.md` 的剩余任务：当前下一项是 `ARCH-04`，收口 Validator、P0 报告字段、Unity smoke 发现机制和 `validation_limited` 表达。
2. `FLOW-01..FLOW-07` 已完成一轮程序补强，但仍需要 Unity runtime / 人工体验复核；若发现正常 UI 不可达、按钮未调用真实领域服务或状态未回写，按对应 `FLOW-*` 追加 bug 修复或验收补强，不重开基础线。
3. `ARCH-04` 后继续 `ARCH-05`：把已完成项、未开放项、补强项和架构项命名收口到程序状态页与开发文档，防止后续复制智能体重复开发。
4. UI / 美术侧可按 active `screen_layouts.json` 推进 V2-A 五屏 FormalV2 规格；程序接入时必须同时满足玩家主流程入口和真实领域服务操作闭环，不能只做截图覆盖。
5. 策划 / 配置侧继续推进 C4 及后续配置；C1-C3 已配置完成并完成当前程序侧可做审计，后续只按真实缺口补齐、验收修复、字段迁移或数值校准。

