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
  - rules/README.md
  - rules/01_文档维护与新增控制规则.md
  - rules/02_智能体任务路由与完成协议.md
  - 版本规划/README.md
  - 版本规划/13_正式版全局体验总线与开放节奏.md
  - 版本规划/14_0-12小时候选主循环细案设计.md
  - 版本规划/09_正式版核心纵切开发路线.md
  - agent_status/director.md
  - agent_status/pm.md
  - agent_status/program.md
  - agent_status/design.md
  - 设计文档/README.md
  - 设计文档/delivery/00_策划文档开发交付审计.md
  - 设计文档/规则卡/01_局外时间与日程口径规则卡.md
  - 设计文档/规则卡/02_物品背包旋转与生命周期规则卡.md
  - 设计文档/规则卡/03_战斗回合与怪物意图规则卡.md
  - 设计文档/规则卡/04_小镇经济结算与压力链规则卡.md
  - 设计文档/规则卡/05_局外成长与维护规则卡.md
  - 设计文档/规则卡/06_标签与特质规则卡.md
  - 设计文档/规则卡/07_势力声望与订单规则卡.md
  - 设计文档/规则卡/08_人偶核心状态与好感双轨规则卡.md
  - 设计文档/规则卡/09_人偶交互事件与反馈规则卡.md
  - 设计文档/规则卡/10_剧本调度与事件队列规则卡.md
  - 设计文档/规则卡/11_人偶房间布局与视觉叙事规则卡.md
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
  - 知识库/views/director.md
  - 知识库/views/pm.md
  - 知识库/views/art.md
  - 知识库/views/design.md
  - 知识库/views/program.md
  - tools/agent/README.md
  - tools/p3-mission/README.md
last_verified: 2026-07-17
update_rule: 项目阶段、总目标、跨职能交接或阻塞项变化时更新本文件。
---

# 项目状态

> 项目级状态页。复制出来的智能体如果改变了当前里程碑、跨职能交接、阻塞项或下一步总优先级，需要更新这里。

## 最后更新

2026-07-17

## 当前阶段

正式版核心纵切。

项目已经从 MVP 闭环验证转入正式版纵切开发。这里的“纵切”是开发顺序，不是质量降级：被选中的系统按正式版标准完整处理。当前开发应按纵向链路推进：

```text
证据审计 -> 策划详细设计 -> 配置源落地 -> UI/UX 与美术设计 / 素材 -> 程序功能与素材接入 -> Owner 自验 -> 职能总监验收 -> 状态回写
```

## 当前顶层目标

`版本规划/13_正式版全局体验总线与开放节奏.md`

`版本规划/14_0-12小时候选主循环细案设计.md`

`版本规划/09_正式版核心纵切开发路线.md` 继续作为开发宏观路线；`版本规划/11_纵切批次与需求文档承接矩阵.md` 继续作为需求文档承接门禁。

## 当前版本规划入口

`版本规划/README.md`

## 当前优先级

1. 游戏导演 / 制作人侧已完成首批细案启动判断：`13` 已从 8-12 小时候选主循环扩展为全系统体验归位，并补充全游戏模块宏观设计矩阵；现有 `GDD_01` 到 `GDD_12` 均保留，不砍正式系统；8-12 小时只作为候选主循环稳定门槛，不作为完整游戏范围上限。
2. 0-12 小时进入细案 Owner 逐个完善阶段：`14` 已改为按 `13` 第 12 节游戏时长横向切分的设计入口，具体细案在 `版本规划/0-12小时细案/` 中一案一文；当前 `T0 序章首次循环` 已重规划为 0-30 分钟 V2，明确完整 T0 结束点不是进入第一层，而是完成第一次浅层行动、带回结果、回到工坊照看并形成下一轮理由；`T1 第一层搜打撤成形` 已补为 30 分钟-2 小时游戏设计稿，`T2..T4` 仍是待设计队列，未完成设计前不作为实现 Owner 正式开工依据。
3. 后续具体执行进入实现 Owner 纵切启动阶段：一个 agent 对一个已完成细案的实现切片负责到底，覆盖证据审计、本切片具体方案、配置源落地、UI/UX 与美术接入方案、程序功能与素材接入、Owner 自验和状态回写。Owner 自验不等于最终完成，最终完成需要主策 / 主程 / 主美 / 游戏导演中对应角色的外部验收；职能 agent 默认作为总监验收，不再作为常规分包承接者。
4. C1-C3 配置程序支持已先收口：策划前三层配置源已完成，程序侧已完成当前可做的同步、静态审计和 P0 入口接入；后续只在 Unity runtime 补跑或人工体验发现真实阻断时按 bug 修复追加处理。
5. 继续围绕“玩家正常 UI 可达 + 调用真实领域服务 + 状态真实变化 + smoke / P0 证据”补主流程闭环；不再按程序 / 策划 / 美术横向拆任务，而按 `14` 已完成细案派发实现切片。
6. UI / 美术接入跟随可玩闭环：有 Approved 资源和 active UI 规格时一起接入；ArtAcceptance、debug preview、只读快照或静态面板不能替代真实玩家结果。
7. 第四 / 五层、高层订单链、完整势力线、完整房间 / 日记 / 纪念物、完整好感交互和剧本事件队列已经纳入全游戏宏观大纲；实现上按玩家时长段逐步开放深度，不能因为宏观保留就一次性横向铺量。

当前程序正式版本完成口径：以 `agent_status/program.md`、开发文档、代码事实和验收证据为准。只读数据层、ArtAcceptance 截图、debug 入口或静态面板不能直接标记为可玩完成；服务存在但正常 UI 不可达，仍属于“功能未开放完”。本轮程序推进的执行顺序是 `CFG-01..02 -> FLOW-01..07 -> ARCH-01..05 -> REVIEW-01`。

## 职能状态页

- 游戏导演 / 制作人：`agent_status/director.md`
- 美术 / UI：`agent_status/art.md`
- 策划 / 数值：`agent_status/design.md`
- 程序 / Unity：`agent_status/program.md`
- 旧 PM 兼容入口：`agent_status/pm.md`

## 最近完成

本节只保留最近对全局阶段、跨职能交接或下一步总优先级有影响的摘要；详细执行记录回到对应职能状态页和事实文档。

- 2026-07-17 已完成 Agent 职责入口分层：`AGENTS.md` 收敛为根职责路由和全局硬边界，`rules/02_智能体任务路由与完成协议.md` 统一承接任务角色选择、Owner 责任流水、自验 / 外部验收、状态回写和 Git 完成要求，`知识库/views/*.md` 只保留职能阅读导航。该调整不改变正式版核心纵切阶段、T0/T1 状态或当前业务优先级。
- 2026-07-11 已完成 `T0-01A` 封板候选设计与开发承接：Owner 具体设计按 `T0-PRE-01..11` 锁定黑屏、漫画页、债务/手记/核心碎片、发现与启动零号、正式对白、状态/擦灰、FormalV2 半开放工坊和首潜许可卡的逐镜执行稿；开发方案改为 `SEAL-DES / ART / FLOW / UI / VAL` 增量顺序，明确不重做已完成的 NARR/T0-FLOW 主链。当前可声明“A 段封板候选文档完成”，不可声明素材、Unity 或运行时商业化封板通过。

- 2026-07-11 已补齐 `T0-PRE` 商业化基线并写入 `版本规划/0-12小时细案/T0-01_序章首次循环开发总方案.md` 第 3.1 节：完整 T0 现在有 16 张目标截图序列，覆盖 `黑屏醒来 -> 破败工坊 -> 债务 / 手记 / 核心碎片 -> 发现并启动零号 -> 苏醒照看 -> 半开放工坊 -> 首潜许可 -> 浅缘短遭遇 -> 首件带回物 -> 回城照看与下一轮目标`。每张图均明确主视觉、允许 UI、禁止 UI、首要通过标准和 A/B/C 归属。本轮只完成设计基线，不声明 Unity、配置源、素材或完整 T0 验收完成；下一步转入 `T0-01A` 封板候选和 `T0-01B / T0-01C` Owner 具体设计。
- 2026-07-11 已完成 `T0-01` 文档体系重构：新增 `版本规划/0-12小时细案/T0-01_序章首次循环开发总方案.md` 作为完整 T0 开发总入口，明确后续按 `T0-PRE 商业化基线 -> T0-01A 开场到首潜许可封板 -> T0-01B 第一层浅缘短遭遇到带回第一件东西 -> T0-01C 回城照看到下一轮目标 -> T0-VAL-02 完整序章验收` 推进；`T0-VAL-01` 改为 A 段运行时效果验收记录，不再作为完整 T0 完成依据。本轮只调整文档口径、互链和状态页，不声明 Unity 实现、配置源、素材或商业化验收完成。
- 2026-07-10 0-12 小时细案 Owner 已将 `版本规划/0-12小时细案/T0-01_序章首次循环.md` 重规划为 V2：T0 现在按商业化序章体验描述 `黑屏与破败工坊 -> 债务 / 手记 / 核心碎片 -> 发现零号 -> 启动零号 -> 苏醒照看 -> 首潜许可 -> 第一层浅缘短遭遇 -> 带回第一件东西 -> 回城照看与下一轮目标`。同步更新 `版本规划/14_0-12小时候选主循环细案设计.md`，并建议后续新增 `T0-01B` 和 `T0-01C` Owner 具体设计承接浅层遭遇、带回物和回城照看；这不是 Unity 实现完成，也不改变 `T0-VAL-01` 仍只是运行时纵切条件通过的边界。
- 2026-07-08 `T0-VAL-01` 在第 15 节人工否决后完成一轮正式版重做并进入 `conditional_pass:t0_val_01_formal_v2_runtime_slice`。最新证据为 `UnityClient/Logs/T0Validation/t0_val_01_final_capture_report.txt`：`captured_at=2026-07-08 00:46:12`、`count=10`、`semantic_failed=False`，contact sheet 为 `UnityClient/Logs/T0Validation/t0_val_01_final_contact_sheet_latest.png`。当前可声明 T0-01A 从开局人偶状态到首次下潜许可并进入第一层地图的运行时纵切成立；不可声明完整 T0 序章、最终 CG / Live2D / ArtAcceptance 美术封版完成。Owner 自验与 subagent 复验均为条件通过，剩余问题是漫画页排版、AUTO/LOG 旧 UI 感、半开放工坊和首潜许可卡最终 polish。
- 2026-07-08 美术侧已完成 `T0-01A` 序章 CG 首版 Approved 素材：p01 醒来底图和 p02-p06 共 15 张漫画 Panel 已整理到 `UnityClient/Assets/Art/Approved/NarrativeCG/T0/`，并写入 `UnityClient/Assets/Art/_IncomingAI/_page_reviews/t0_01a_prologue_cg_approved_review_20260708.md` 与总览图 `t0_01a_approved_contact_20260708.png`。当前可声明 `素材完成`，但 `program_integrate=0`，尚未完成运行时漫画页接入、VisualID 消费或六张 T0 漫画页截图验收。
- 2026-07-10 美术侧按用户反馈完成 `T0-01A` 序章 CG 风格纠偏替换：6 个关键风格锚点已用同 VisualID 覆盖 Approved，并保留原 Unity `.meta`。证据为 `UnityClient/Assets/Art/_IncomingAI/_page_reviews/t0_01a_style_correction_review_20260710.md`、`t0_01a_style_correction_contact_20260710.png` 和 `美术文档/_generated/art_integration_snapshots/20260710_012453_t0_01a_style_fix_20260710_verify.md`。当前可声明 `风格纠偏素材替换完成`；`Validate-ArtGeneratedJson.ps1` 仅受既有 `offline_registry_candidate` 问题限制，仍不可声明运行时漫画播放、VisualAssetRegistry 新登记或最终商业验收通过。
- 2026-07-06 用户人工验收再次否决 `T0-VAL-01`，项目级当前口径改为 `validation_failed:t0_val_01_commercial_prologue_not_passed`。复核结论已写入 `版本规划/0-12小时细案/T0-VAL-01_最终实际效果优化文档.md` 第 15 节：`UnityClient/Logs/T0Validation/t0_val_01_final_capture_report.txt` 的 `semantic_failed=False` 只证明 8 个语义状态曾被截到，不能证明商业化序章成立。当前 P0 问题是没有真正序章开场、启动零号缺少仪式感、工坊未适配 FormalV2 正式布局、美术 / UI 仍像 staging 拼装；不能再声明 `T0-VAL-01 完成` 或 `formal_v2_runtime_slice_passed`。
- 2026-07-05 根据用户“按正式版做、Owner 完成整个对应内容、直接接入 FormalV2 工坊设计”的要求，已重设 `T0-VAL-01` 正式版优化方案并写入 `版本规划/0-12小时细案/T0-VAL-01_最终实际效果优化文档.md` 第 13 节。新口径明确：对话默认手动点击推进，`AUTO` 默认关闭；T0 半开放工坊直接接入 `workshop_main` FormalV2 active zones；`启动零号` 是独立仪式交互；首潜确认必须改为固定第一层许可卡；截图 runner 必须做语义断言。当前状态仍为 `validation_failed:t0_val_01_commercial_effect_not_passed`，不是完成。
- 2026-07-05 用户人工验收否决 `T0-VAL-01`，当前项目级口径改为 `validation_failed:t0_val_01_commercial_effect_not_passed`。实现 Owner 已按商业化序章标准复验并写回 `版本规划/0-12小时细案/T0-VAL-01_最终实际效果优化文档.md` 第 12 节：程序主链可跑到第一层，但当前 staging 截图不满足商业化序章验收。关键阻断包括 `final_06_half_open_workshop` 实际与首潜确认重复、半开放工坊证据缺失、开场过程 CG 像技术演示、`启动人偶` 没有仪式感、首潜确认仍像通用层列表 / 调试面板。
- 2026-07-05 `T0-VAL-01` 最终实际效果验收已完成两轮通过：Owner 自验结论为 `自验通过，提交 subagent 验收`，独立 subagent 复验结论为 `通过`。证据入口为 `版本规划/0-12小时细案/T0-VAL-01_最终实际效果优化文档.md` 第 11 节、`UnityClient/Logs/T0Validation/t0_val_01_final_capture_report.txt` 和 8 张 `t0_val_01_final_*.png` 固定截图；`NarrativeOverlaySmokeTest.Run` 与 `PrologueFirstDivePermissionSmokeSuite.Run` 通过，Unity Console error=0。项目级口径：当前 staging 纵切标准下，T0-01A 从开局人偶状态到首次下潜许可并进入第一层的玩家可见路径已验收通过；正式 CG 资产封版、债务纸可读文本和首潜许可卡视觉强化仍是后续美术 / UI 优化，不作为当前阻塞。
- 2026-07-05 `T0-VAL-01` live Unity MCP Owner 自验已通过，且 15:12-15:20 已补抓一组有效截图替换旧的纯色过渡帧证据：连续路径从开场过程 CG / 对白推进到 `启动人偶`、状态小卡、擦灰、半开放工坊、第一层确认，并在点击 `出发` 后写入 `Layer1FirstDeparted`、调用 `DungeonManager.StartRunAtLayer(1)`、进入 `DungeonMap`；最终状态为 `overlay=isShowing=False, blocks=False`。关键截图位于 `UnityClient/Logs/T0Validation/t0_val_01_livefix_01_start_doll_action.png`、`_02_wipe_action.png`、`_03_half_open_workshop.png`、`_04_layer_confirm.png`、`_05_final_dungeon_map.png`，阻断态截图为 `t0_val_01_rerun_07_blocked_extreme_wear.png`。当前声明边界是“开局人偶状态到首次下潜许可并进入第一层的玩家主链已可现场验证”，不是“整个 T0 序章完成”或“商业最终演出通过”；正式 CG、ArtAcceptance 和主职能外部验收仍需另行收口。
- 2026-07-05 P0 总入口已通过：最新 `UnityClient/Logs/P0Validation/latest/report.md` 为 RunID=`20260705_150220`，`Status=Passed`，Errors=0，Blocked=0，ValidationLimitations=0，Warnings=33；`ConfigSync`、`ConfigValidator`、`UnitySmokeTests`、`UIDesignValidation`、`ArtAcceptanceLatest` 均通过。本轮修复了地下城节点 outcome 预期、二层地图布局、黄金路径战斗掉落验收口径、经济传闻固定样例、`CombatLootDropTest.Run` 临时 `GameFlowController.Instance` 恢复和 AutoTest/TestReport 采集链路。当前声明边界：P0 自动验收入口已收口，不等于 T0 序章正式 CG / 商业演出验收完成。
- 已基于 `T0-01A_开局人偶状态到首次下潜许可开发方案.md` 生成本地 p3-mission 任务包 `.mission/20260621_233306-T0-01A-开局人偶状态到首次下潜许可-根据开发方案制定可执行任务包.csv`：12 个 TASK + 1 个 REVIEW，strict 校验通过。它是后续执行恢复队列，不替代开发方案或状态页，也不代表 Unity / 配置 / 素材已落地。
- 已将 `T0-01A_开局人偶状态到首次下潜许可开发方案.md` 的 `P0 事实审计` 改为设计阶段已完成结论：现有工坊、层选择、下潜许可和第一层进入链路可复用；全局叙事运行时、T0-01A Narrative 内容源、半开放工坊和首潜确认态仍是后续开发缺口。后续实现不再从“事实审计”开始，而是从 NARR-00 / NARR-03 全局叙事最小底座和 T0-01A 内容源落地开始。
- 已新增 `T0-01A_开局人偶状态到首次下潜许可开发方案.md`：基于 Owner 具体设计 V4，按玩家流程拆 `P0 事实审计 -> 黑屏醒来 -> 工坊过程 CG -> 发现零号 -> 启动人偶 -> 零号苏醒 -> 状态小卡与擦灰 -> 浅层入口 -> 第一层确认 -> 出发进入第一层 -> 联调证据`，并明确每段的策划、美术 / UI、程序、配置 / 文案和验证工作。当前只是开发方案 V1，未声明 Unity 代码、配置源或素材落地。
- 游戏导演 / 制作人侧已在 `版本规划/13_正式版全局体验总线与开放节奏.md` 新增 `全游戏模块宏观设计矩阵`，并压缩重复的深渊五层表和 8-12 小时五幕表；后续实现切片必须引用时长段、细案切片 ID、`14` 对应细案和 `13` 模块宏观设计行。
- 0-12 小时细案 Owner 已将细案口径从“实现 / 验收切片”拉回“游戏时长段设计”：`版本规划/14_0-12小时候选主循环细案设计.md` 现在按 `T0` 0-30 分钟、`T1` 30 分钟-2 小时、`T2` 2-5 小时、`T3` 5-8 小时、`T4` 8-12 小时组织设计；`版本规划/0-12小时细案/T0-01_序章首次循环.md` 已改为 0-30 分钟序章体验设计稿。原 `T0-01 / T0-02 / T0-03` 机械拆分稿已归档为历史参考。
- 0-12 小时细案 Owner 已新增 `版本规划/0-12小时细案/T1_第一层搜打撤成形.md`：T1 以 30 分钟-2 小时完整体验段承接 T0，编排再次准备、第一层路线、战斗意图、背包取舍、中段路线后果、撤离 / 深入判断、回城复盘和 T2 钩子，不按 `T1-01/T1-02/T1-03` 拆验收点。
- 游戏导演 / 制作人侧已细化任务 Owner 责任流水：Owner 按证据审计、策划详细设计、配置源落地、UI/UX 与美术设计 / 素材、程序功能与素材接入、Owner 自验、职能总监验收和状态回写闭环负责；Owner 自验不替代主职能外部验收。
- 游戏导演 / 制作人侧已确认当前优先级仍是补 0-12 小时游戏设计：下一步先完善 `T2 2-5 小时：小镇压力与下潜目标成形`，把出售、账单、维护、传闻 / 基础订单和成长目标融合成下一轮下潜理由，而不是继续拆验收点。
- 美术全局方向已纠偏：正式主轴为日系二次元地底奇幻冒险 + 低信息密度；黄铜 / 铜件 / 暖灯 / 蒸汽朋克不再作为全局风格关键词，也不作为 active UI 或正向素材提示词的默认材料 / 灯光方向。
- 0-12 小时候选主循环不再以 `13` 的旧派发表派发，也不再按 `T1-01/T1-02/T1-03` 这类功能 / 验收点拆细案；后续由 `14` 管时长段设计入口，由 `版本规划/0-12小时细案/` 管具体设计细案。实现 Owner 在设计细案完成后再自行拆具体实现切片。
- 美术 / UI 侧已直接接管 FormalV2 运行时视觉 / 布局精修，并确认纯 UGUI 表现层 polish 默认由美术侧闭环：最新 ArtAcceptance `RunID=20260613_020257` 工具层 `PASSED`、21/21 captured、Registry 278、`MissingRequiredVisualIDs=0`、`RealGameplay=21`、`FormalV1Template=0`；当前 FormalV2 runtime UI visual seal 通过纵切基线，后续共享子面板差异化、动画 / VFX 和小尺寸可读性属于美术 / UI 质量迭代。
- 策划 / 配置侧 C1-C3 前三层配置源已标记为 `配置完成` 并完成当前可做的同步 / 静态审计；后续只按真实缺口补齐、验收修复、字段迁移或数值校准处理，不重复派发基础配置。
- 已新增全局 agent rules 入口 `rules/README.md` 和 `rules/01_文档维护与新增控制规则.md`：后续新增、重写、拆分或归档项目文档前，必须先判断是否补充已有文档、是否归档旧文档，以及是否会制造重复事实来源；策划业务规则统一称为 `设计文档/规则卡/`。
- `p3-mission` 已收敛为当前唯一长期任务恢复协议；旧 PM / 版本规划路由、旧 `12` 长期节点页和 `misc/Missions` 只保留兼容或归档说明，不再作为 active 派发入口。

历史完成细项按职能追溯：游戏导演看 `agent_status/director.md`，美术 / UI 看 `agent_status/art.md`，策划 / 配置看 `agent_status/design.md` 与 `设计文档/config/26_正式配置设计与填充推进计划.md`，程序 / Unity 看 `agent_status/program.md`，知识库和旧入口迁移看 `DOCS_INDEX.md` 与 `版本规划/README.md`。
## 跨职能交接

本节只保留当前仍影响派发和验收的全局交接规则；具体执行细节回到对应职能状态页。

- 游戏导演 / 制作人维护 `13` 体验总线、`09` 开发路线、`PROJECT_STATUS.md` 和任务 Owner 工作流；0-12 小时细案 Owner 维护 `14` 与 `版本规划/0-12小时细案/`；变更阶段、开放节奏、优先级、里程碑、细案或跨职能交接时同步目标状态页。
- 下一批实际执行必须先区分三层文档：细案按时长段写玩家体验；Owner 具体设计把一个玩家结果写到镜头、交互、UI 状态、对白和转场层；开发方案才拆策划、美术 / UI、程序、配置 / 文案、联调、验证和状态回写。开发方案不得重新改体验方向。
- Owner 自验只是第一层证据整理；最终完成必须记录对应主策 / 主程 / 主美 / 游戏导演外部验收结论，或明确 `validation_limited:*` / 阻塞原因。
- 涉及美术 / UI 的实现切片必须按 `美术文档/04_美术风格基准.md` 和 Formal V2 当前规则验收：日系二次元地底奇幻冒险是主轴，不能把黄铜暖灯、棕金机械 UI 或蒸汽朋克当作默认世界气质或生图提示词基础。
- 美术 / UI 与程序侧统一使用纯 UGUI；程序接入只以 `美术文档/ui_design/screen_layouts.json` 当前 active 规格为准，draft 和 `versions/` 不作为程序接入口。
- FormalV2 当前工具层 ArtAcceptance 通过，核心 P0 UI 结构和当前纵切 UI 基线通过；后续视觉 / 布局精修由美术 / UI 侧直接处理，程序侧只在领域服务、Unity 工程约束、自动验收工具或 UGUI 底层能力出现阻断时介入，资源登记和批量补图不重开。
- 策划与程序做运行时验证前应先同步配置：`./tools/config/Sync-Configs.ps1 -Clean`；配置事实来源是 `配置表(JSON)`，不要手写维护 `UnityClient/Assets/StreamingAssets/Configs`。
- `11` 不记录实现进度；实现状态、证据入口和验收状态统一回到对应职能状态页和事实文档。字段说明、README、审计、任务拆分或 JSON 修改前设计不等于配置源完成。
- 长任务、恢复继续、跨会话拆分和 mission 请求统一使用 `.codex/skills/p3-mission/`；旧 `misc/Missions` 和旧 PM 路由只保留兼容 / 归档说明。
- 任意智能体新增、重写、拆分、归档或调整项目文档前，必须先读 `rules/01_文档维护与新增控制规则.md`；优先补充已有事实来源，确需新增时同步检查旧文档归档 / 废弃、README / 状态页入口、`related` 双向互链，并运行 `./tools/docs/Generate-DocsIndex.ps1` 和 `./tools/docs/Validate-Docs.ps1`。
## 问题 / 阻塞

- `T0-VAL-01` 已从第 15 节硬失败修复到 `conditional_pass:t0_val_01_formal_v2_runtime_slice`。当前不再是“无序章 / 无工坊 / 无许可”的 P0 主链阻塞，但仍不是最终商业化封版；剩余风险集中在漫画页排版、对白 UI 皮肤、半开放工坊视觉完成度、首潜许可卡仪式感和最终美术验收。
- P0 总入口阻塞已于 2026-07-05 解除：最新报告 `UnityClient/Logs/P0Validation/latest/report.md` 为 `Passed`、Errors=0、Blocked=0。当前仍需注意 warning 不是阻断，但后续若脱离 MCP 跑自动化，应复核 `.test_trigger` 后台唤醒稳定性。
- 当前工作区已有前序 UI、美术、生成物和 submodule 相关脏文件。后续智能体开工前应先运行 `.\tools\agent\Invoke-AgentHealthCheck.ps1`，提交时严格收窄范围。
- `CombatLootDropTest.Run` 旧精英保底断言和 TestReport 缺失问题已在 P0 RunID=`20260705_150220` 中验证收口；后续只按真实回归失败重开 bug。
- `tools/ComfyUI_NAIDGenerator/` 当前未跟踪，后续需要决定它是 vendor 代码、submodule，还是本地专用工具。
- `tools/ai-image-gateway` 子模块内部有未提交改动；如需处理，应进入子模块内部单独处理。

## 下一步总建议

1. `T0-PRE` 商业化截图 / 验收基线已补齐；下一步先按 16 张目标截图推进 `T0-01A` 封板候选，再新增 `T0-01B / T0-01C` Owner 具体设计；不要继续把完整 T0 需求塞回 `T0-01A` 或 `T0-VAL-01`。
2. `T0-VAL-01` 当前以条件通过作为运行时纵切证据收口；若继续追最终商业化封版，应按 `T0-01` V2 重新审视演出链路，并单独推进漫画页排版 / 字幕条、对白 UI 皮肤、半开放工坊最终视觉、首潜许可卡专属视觉和 ArtAcceptance / 人工主美验收。
3. P0 总入口已通过，后续不再把 `DungeonNodeTypesSmokeTest`、`DungeonStairsProgressionTest`、`MainFlowGoldenPathSmokeTest`、`TownEconomyServiceSmokeTest` 或 `CombatLootDropTest.Run` 作为当前阻塞队列重复派发；若未来回归失败，按具体失败项重开程序 bug。
4. `T0-01B / T0-01C` 设计收口后，再由细案 Owner 完善 `T2 2-5 小时：小镇压力与下潜目标成形` 设计稿。它要承接 `T1 第一层搜打撤成形` 的路线经验、战利品差异、撤离 / 深入判断和第二层好奇心，把小镇从回城菜单推进为出售、账单、维护、传闻 / 基础订单和成长目标共同制造下一轮理由的体验段。
5. 程序侧已完成 `FLOW-01..FLOW-07` 一轮补强和 `ARCH-01..ARCH-05` 当前收口；后续只在 Unity runtime / 人工体验发现正常 UI 不可达、按钮未调用真实领域服务或状态未回写时，按真实缺口追加 bug 修复或验收补强。
6. UI / 美术侧已接管 FormalV2 运行时视觉 / 布局精修，并完成 ArtAcceptance `20260613_020257` 复验。本轮资源 / Registry / 工具门禁和当前纵切 UI 基线通过；下一步继续由美术 / UI 侧直接做共享子面板差异化、背景候选、动效 / VFX 和小尺寸可读性复查；程序侧只在真实领域服务、自动验收工具或 Unity 工程阻断时介入。实现 Owner 接入时仍必须同时满足玩家主流程入口和真实领域服务操作闭环，不能只做截图覆盖。
7. 策划 / 配置侧继续推进 C4 及后续配置；C1-C3 已配置完成并完成当前程序侧可做审计，后续只按真实缺口补齐、验收修复、字段迁移或数值校准。

