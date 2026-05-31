---
id: agent_status_art
title: 美术 / UI 状态
type: status
role: 美术
domain: art_pipeline
status: active
source_of_truth: true
related:
  - 版本规划/09_正式版核心纵切开发路线.md
  - agent_status/README.md
  - PROJECT_STATUS.md
  - 开发文档/00_程序开发大纲.md
  - 开发文档/14_Unity运行时美术自动验收方案.md
  - 开发文档/09_视觉资源系统程序开发规范.md
  - agent_status/program.md
  - 设计文档/GDD/GDD_00_系统关联总图.md
  - agent_status/design.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - 美术文档/10_美术验收截图优化与真实数据驱动演进方案.md
  - 美术文档/13_正式纵切UI与素材覆盖矩阵.md
  - 美术文档/ui_design/ui_iteration_process.md
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/README.md
  - 知识库/views/art.md
last_verified: 2026-05-31
update_rule: 美术或 UI 视觉流水线任务完成后更新本文件。
---

# 美术 / UI 状态

## 最后更新

2026-05-31

## 当前关注

支撑正式版核心纵切。按最新 `09` 路线，美术 / UI 作为 P5 表现支撑，只围绕当前 P0-P4 功能纵切补表达、资源和截图验收，不继续横向铺所有界面。

PM 版本节点中，美术线当前 21 个界面都已具备 Formal V1 active 规格。美术侧已主动触发 latest ArtAcceptance `20260527_002436`，工具层 21/21 captured、`PASSED`、Registry 191、MissingRequiredVisualIDs=0、UI snapshot risks=0；latest 程序交接清单为 `program_integrate=0`、`add_capture=0`、`rerun_acceptance=0`。本轮人工验收结论是“资源接入通过、画面不完全通过”：`combat_hud`、`dungeon_map`、`safe_room`、`stairs_room`、`sell_panel`、`prosthetic_panel` 需要程序侧清理截图状态或补有效展示数据后重跑；其余界面多为通过或条件通过，后续继续 Visual V2 同名替换。

当前新增重点是 Formal V2 UX/UI 重构。Formal V1 证明了功能区域、VisualID 和截图链路可运行，但整体体验仍偏按钮菜单 / debug 面板。Formal V2 先作为 draft 设计层推进，不修改 active `screen_layouts.json`，不触发素材生成，也不要求程序接入；V2-A 优先重审 `workshop_main`、`combat_hud`、`inventory_loot`、`dungeon_map`、`settlement` 五个核心主流程界面。

截至 2026-05-31，V2-A 五个核心界面已全部补齐可评审草案：`workshop_main` 定义工坊 Hub、深渊入口主行动、工作台、魔偶维护舱和市场账本；`combat_hud` 定义战斗舞台、敌方实体、背包指令区和回合决策层级；`inventory_loot` 定义撤离清点台、容量压力和确认带出主行动；`dungeon_map` 定义路线图主视觉、节点详情和进入节点主行动；`settlement` 定义报告式结算、收益损失、状态变化和下一步主行动。Figma、Unity MCP、截图标注和 PlayMode 布局扫描已写入 Formal V2 工具策略：当前不作为前置阻塞，结构确认后再评估接入。

美术侧已把“可接入覆盖”、“缺图生成”和“视觉质量替换”拆开：`可接入素材清单.md` 给程序看，`缺图生成计划.md` 给美术侧执行 `generate_needed` 新素材跑图，`素材质量替换清单.md` 给美术侧执行 local_v0 / placeholder 同名替换。当前 `generate_needed=0`，质量清单为 `technical_fix=0`、`visual_v2_replace=121`，表示没有必须先修的技术风险；历史本地生成 Approved 已统一补标 `QualityTier=local_v0`，这些素材不阻塞程序接入，后续按 Visual V2 同名替换。

美术侧已新增“需求候选扫描”前哨：`Scan-ArtRequirementCandidates.ps1` 会扫描最新设计文档、配置表、版本规划和 active UI 文档，生成 `美术文档/_generated/美术需求候选清单.md/json`。当前 latest 为 `new_candidate=36`、`approved_without_manifest=0`、`seed_only=0`、`manifest_managed=158`；这些候选只用于人工审查，确认后才写入 `art_requirements_seed.json` 或等待正式配置字段落地，不自动进入 Manifest。

P1 战斗可读性 active 合同已补齐：`combat_hud` 现在包含怪物意图图标、战斗状态图标、命中 / 破盾反馈、封格 / 塞包 overlay。P1 结算结果 active 合同已补齐：`settlement` 现在包含胜利、HP 战败、SAN 崩溃、HP+SAN 复合战败和队伍溃败五态结果徽记。P3 底盘升级 active 合同已补齐：`chassis_upgrade_panel` 包含当前底盘、下一底盘、容量变化、材料缺口、蓝图前置和升级确认。P3 维护可读性 active 合同已补齐：`maintenance_panel` 现在把磨损修复、侵蚀净化和下潜许可拆成独立 VisualID。P3 Room Memento 已按 `44_局外成长人偶特质房间正式配置落地设计.md` 前置补齐 8 个房间纪念物 VisualID。P4 营业结算 active 合同已补齐：`business_settlement` 位于 `shop_staging` 和 `daily_bill_report` 之间，承接顾客流、成交反馈、未售出 / 黑市风险摘要和进入账单动作。P4 每日账单 active 合同已补齐：`daily_bill_report` 现在把收入、支出和月租债务拆成独立 VisualID。P4 经济压力传闻 / 势力 / 订单已按 `48_经济压力传闻正式配置落地设计.md`、`49_经济压力势力正式配置落地设计.md` 和 `50_经济压力订单正式配置落地设计.md` 前置补齐 42 个 VisualID。latest 可接入清单当前为 `program_integrate=0`、`acceptance_needed=191`、`generate_needed=0`；新增缺图项均已有 Approved PNG 和 `.meta`，但多数质量层级仍为 `local_v0`，不视为最终美术。

Visual V2 执行入口已补齐：`美术文档/_generated/VisualV2生成计划.md/json` 当前规划 121 项替换资源，`PromptReadyItems=121`，下一批次为 `nai_visual_v2_20260526_04`，其中 P1 仍剩 24 项、P2 97 项。NovelAI token 链路已完成 P0 新节点图标、P1 核心战斗意图图标和 P1 首批战斗反馈 / 标记替换，后续继续串行生成，脚本每次请求 1 张图，图间隔 1 秒。

Visual V2 工具链已补齐安全替换流程：已接入素材可用 `Run-ArtGeneration.ps1 -PreserveStatus` 生成候选，用 `CandidateBatchID` 只预处理本批 raw，再用 `Sync-ApprovedArt.ps1 -QualityTier formal_ai_v2 -ClearCandidate` 同名替换 Approved。该流程不会把原 `approved` / `registered` / `validated` 状态回退到 `generated`。`Sync-ApprovedArt.ps1 -CandidateBatchID` 默认启用严格 `.meta` guard：目标 PNG 和目标 `.meta` 必须已存在，且同步前后 `.meta` 字节必须一致，确保正式图替换只改 PNG 内容，不要求程序侧重新登记同一资产。

美术文档已收敛为四层入口：`README.md` 只做导航，`10_正式版核心纵切美术路线.md` 作为当前规划入口，`00_美术流水线总览.md` 作为端到端资产生产工作流入口，`ui_design/README.md` 作为 UI 版本和 active 规格入口。`archive/` 保存 MVP 记录和旧批次交付快照。

Formal V1 运行时验收已工具化：`Generate-FormalV1AcceptanceQueue.ps1` 会从 active `screen_layouts.json`、Manifest、latest ArtAcceptance 和 Registry 快照生成 `美术文档/ui_design/_generated/FormalV1验收队列.md/json`。程序侧交接已进一步收敛到 `美术文档/_generated/程序接入交接清单.md/json`，该清单合并 `program_integrate`、截图覆盖和 ArtAcceptance 重跑队列。当前 latest 为 `FormalV1ScreenCount=21`、`CapturedScreenCount=21`、`art_review_ready=21`，程序侧交接为 `program_integrate=0`、`add_capture=0`、`rerun_acceptance=0`。

## 必读文件

- `知识库/views/art.md`
- `美术文档/README.md`
- `美术文档/10_正式版核心纵切美术路线.md`
- `美术文档/00_美术流水线总览.md`
- `美术文档/ui_design/README.md`
- `美术文档/ui_design/ui_iteration_process.md`
- `美术文档/ui_design/formal_v1/screen_structure_review.md`
- `美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md`
- `美术文档/01_Manifest规范.md`
- `开发文档/09_视觉资源系统程序开发规范.md`
- `开发文档/14_Unity运行时美术自动验收方案.md`

## 工作边界

- 运行时 UI 目标是纯 UGUI。不要新增 UI Toolkit、UXML、USS 或 `UIDocument` 运行时流程。
- `美术文档/ui_design/screen_layouts.json` 是当前 active UI 对接规格；程序接入、素材生成和可接入素材清单只认 active。
- MVP UI 设计已冻结到 `美术文档/ui_design/versions/mvp_baseline_2026-05-22/`，作为历史基线和回退参考。
- Formal V1 先写在 `美术文档/ui_design/formal_v1/`；用户确认后再逐界面修改 active。
- Formal V2 先写在 `美术文档/ui_design/formal_v2/`；用户确认并写入 active 前，只是 UX/UI 设计草案，不作为程序接入口或素材生成入口。
- `versions/formal_v1_candidate/` 是复杂界面的可选暂存区，不是必经流程。
- `美术文档/art_requirements_seed.json` 维护配置表无法扫描出的视觉需求。
- `美术文档/_generated` 与 `美术文档/ui_design/_generated` 是生成输出。
- 正式运行时资源放在 `UnityClient/Assets/Art/Approved`。
- AI 出图工作区 `UnityClient/Assets/Art/_IncomingAI` 保持忽略。
- 每次 AI 出图、预处理或 Approved 同步完成后，都必须维护一版可接入素材清单：`美术文档/_generated/可接入素材清单.md/json` 作为 latest，`美术文档/_generated/art_integration_snapshots/` 作为历史快照。只有明确说明“不更新交付清单”的本地调试才允许使用 `-SkipIntegrationCandidates`。

## 常用命令

```powershell
.\tools\config\Sync-Configs.ps1 -Clean
.\tools\美术工具\Update-ArtManifest.ps1
.\tools\美术工具\Scan-ArtRequirementCandidates.ps1
.\tools\美术工具\Generate-ArtPrompts.ps1
.\tools\美术工具\Validate-UIDesign.ps1
.\tools\美术工具\Scan-UIIterationCandidates.ps1
.\tools\美术工具\Generate-ArtIntegrationCandidates.ps1
.\tools\美术工具\Generate-ArtBatchPlan.ps1
.\tools\美术工具\Generate-ArtQualityBacklog.ps1
.\tools\美术工具\Generate-VisualV2Plan.ps1
.\tools\美术工具\Generate-LocalV0Art.ps1
.\tools\美术工具\Generate-FormalV1AcceptanceQueue.ps1
.\tools\美术工具\Generate-ArtProgramHandoff.ps1
```

## 最近完成

- 已完成 P0 UI 运行时验收：`workshop_main`、`combat_hud`、`inventory_loot` 当前 MVP Baseline 为 `validated`。
- 已建立 Formal V1 UI 结构设计层：`combat_hud`、`workshop_main`、`inventory_loot`、`dungeon_map`、`settlement`。
- 已冻结 MVP UI Baseline：`美术文档/ui_design/versions/mvp_baseline_2026-05-22/`。
- 已建立 UI 设计迭代流程：Formal V1 文档确认后修改 active `screen_layouts.json`，再生成素材和交给程序接入。
- PM 已将美术线纳入 `版本规划/09_正式版核心纵切开发路线.md`：A1 聚焦 `combat_hud` Formal V1，A2/A3 依次承接拾取、战斗、地图、工坊和结算界面。
- 已把 `combat_hud` active 规格切到 Formal V1：底部居中背包、左玩家/右敌人实体舞台、敌人脚下血条、中央 `vfx_space`。
- 已扩展 Manifest 扫描与提示词生成：`MonsterEntity.CombatVisualID` 会扫出 `monster_*_combat` 透明战斗实体素材需求，并新增 `ui_combat_entity_shadow` / `ui_combat_target_ring`。
- 已完成 `combat_hud` Formal V1 第一批战斗资源入库：`monster_mob_scavenger_bug_combat`、`monster_mob_acid_slime_combat`、`monster_elite_scrap_guard_combat`、`monster_elite_mutant_amalgam_combat`、`ui_combat_entity_shadow`、`ui_combat_target_ring`，并补齐 Unity `.meta`。
- 已新增 `Scan-UIIterationCandidates.ps1`，可自动扫描 active UI 规格、Manifest、Approved 资源和最新 ArtAcceptance 输出，生成 UI 迭代候选报告。
- 当前扫描报告的旧截图早于 2026-05-24 active 规格；后续需要用最新 ArtAcceptance 逐屏覆盖当前 Formal V1 结构。
- 已新增 `Generate-ArtIntegrationCandidates.ps1`，生成 `美术文档/_generated/可接入素材清单.md/json`；`Sync-ApprovedArt.ps1` 非 DryRun 同步后会默认刷新清单，程序侧可按 `program_integrate` 自助接入。
- 已把可接入素材清单升级为 `latest + snapshot` 机制：`Run-ArtGeneration.ps1`、`Optimize-ArtAssets.ps1`、`Sync-ApprovedArt.ps1` 非 DryRun 后默认刷新 latest，并在 `美术文档/_generated/art_integration_snapshots/` 留一份阶段快照；新增 `art_process` 状态表示 raw 已生成但还需预处理。
- UI 程序侧已接入 `combat_hud` Formal V1，并通过 ArtAcceptance `20260524_043441`：左玩家、右敌方实体、底部居中背包、敌人脚下血条和目标光环均已出现在运行时截图中，报告无 warnings / errors。
- 已把 `workshop_main` / `inventory_loot` active 规格切到 Formal V1，并通过 `Validate-UIDesign.ps1`；程序交付摘要 `ui_design_handoff.md` 已重新生成。
- 已把 P1 首批五个界面 `dungeon_map`、`settlement`、`layer_select`、`sell_panel`、`prosthetic_panel` 切到 Formal V1 active 规格，补齐对应结构文档和迁移记录；程序侧可按 active 规格分批接入。
- 已把 A3 房间节点 `safe_room`、`stairs_room` 切到 Formal V1 active 规格，补齐结构文档、迁移记录和 `13_正式纵切UI与素材覆盖矩阵.md`；程序侧可按 active 规格分批接入。
- 已把 `maintenance_panel`、`daily_bill_report` 切到 Formal V1 active 规格，补齐结构文档、迁移记录和覆盖矩阵；当前 active Formal V1 UI 覆盖增至 12 个界面。
- 已新增 `ui_icon_maintenance`、`ui_icon_bill`、`ui_icon_warning` 三个 preset UI 图标需求，并补齐英文绘图提示词、负面提示词和结构化 Spec。
- 已明确美术侧生成交付协议：每轮生成、预处理或 Approved 同步后都维护 latest 可接入素材清单，并留下 snapshot，供程序侧按 `program_integrate` 自助接入。
- 已将可接入素材清单维护规则同步到 `AGENTS.md` 美术智能体入口：AI 出图、预处理、Approved 同步三类动作都必须刷新 latest 并留 snapshot。
- 已按 NovelAI 串行生成、预处理并同步 `ui_icon_maintenance`、`ui_icon_bill`、`ui_icon_warning` 到 `UnityClient/Assets/Art/Approved/UI/`，并补齐 Unity `.meta`。
- 已留档 P1 图标可接入历史快照：`美术文档/_generated/art_integration_snapshots/20260524_075054_p1_ui_icons_approved_ready_repo_state.*`。
- 已把 A4 三个界面 `shop_staging`、`order_board`、`rumor_board` 切到 Formal V1 active 规格，补齐结构文档、迁移记录和覆盖矩阵。
- 已新增 `ui_icon_shop_channel`、`ui_icon_black_market`、`ui_icon_order`、`ui_icon_faction`、`ui_icon_deadline`、`ui_icon_rumor`、`ui_icon_price_up`、`ui_icon_price_down` 八个 preset UI 图标需求，并补齐英文绘图提示词、负面提示词和结构化 Spec。
- 已尝试用 NovelAI 串行生成 A4 八个 UI 图标，失败原因为 HTTP 402：Anlas 余额不足，不是并发或 rate limit；已留档 `美术文档/_generated/art_integration_snapshots/20260524_084327_generation_nai_a4_ui_icons_20260524_01.*`。
- 已用 local_v0 方式补齐 A4 八个 UI 图标的透明 PNG 可接入临时版，并同步到 `UnityClient/Assets/Art/Approved/UI/`，同时补齐 Unity `.meta`：`ui_icon_shop_channel`、`ui_icon_black_market`、`ui_icon_order`、`ui_icon_faction`、`ui_icon_deadline`、`ui_icon_rumor`、`ui_icon_price_up`、`ui_icon_price_down`。
- 已刷新 A4 local_v0 图标可接入历史快照：`美术文档/_generated/art_integration_snapshots/20260524_085725_a4_ui_icons_local_v0_approved_repo_state.*`。
- UI 程序侧已按当时 latest `program_integrate` 队列接入一批 Approved VisualID，并绑定 `combat_hud`、`maintenance_panel`、`daily_bill_report`、`shop_staging`、`order_board`、`rumor_board`；ArtAcceptance `20260524_212423` 通过，`MissingRequiredVisualIDs=0`，无 missing sprite 命中。
- 已完成美术文档高内聚整理：`README.md` 明确规划层 / 工作流层 / 契约与数据层 / 交付与历史层；`00_美术流水线总览.md` 收敛为资产生产流程；`10_正式版核心纵切美术路线.md` 收敛为当前规划和近期顺序；`13_正式纵切UI与素材覆盖矩阵.md` 作为 active Formal V1 覆盖和素材批次入口；`ui_design/README.md` 明确程序只接 active `screen_layouts.json`。
- 已将 `06_MVP素材接入状态同步.md`、`07_MVP_UI重新设计同步.md`、`11_P0_UI骨架接入交付.md`、`12_P1_UI骨架接入准备.md` 归档到 `美术文档/archive/`，并新增 `archive/README.md` 说明归档规则和替代入口。
- 已将 `ui_design/ui_iteration_process.md` 升级为独立 UI 版本迭代工作流：baseline -> design draft -> 用户确认 -> active -> handoff -> seed/Manifest/Prompt/Spec -> Approved -> 可接入清单 -> 程序接入 -> ArtAcceptance -> validated。
- 已复核最新 `版本规划/09`、`11`，并将美术侧推进口径同步为：P5 不独立铺量，只服务 P0-P4 当前纵切；`知识库/views/art.md`、`10_正式版核心纵切美术路线.md`、`13_正式纵切UI与素材覆盖矩阵.md` 和 UI versions 入口已更新。
- 已将 `doll_room` 从 Formal V1 草案推进到 active `screen_layouts.json` 规格，补齐 `bg_doll_room_attic`、`ui_icon_diary`、`ui_room_memento_slot` 三个 preset 资产需求、Manifest / Prompt / Spec 和 local_v0 Approved PNG。
- 已刷新 latest 可接入素材清单并留档 `美术文档/_generated/art_integration_snapshots/20260525_003240_formal_v1_19_active_doll_room_ready.*`；当前清单显示 `program_integrate=12`、`acceptance_needed=82`、`generate_needed=0`。
- 已同步美术路线、UI 覆盖矩阵、UI 设计入口、Formal V1 总览、迁移日志和知识库美术入口：当前 active Formal V1 覆盖为 20 个界面，暂无剩余 draft UI 队列。
- 已新增 `Generate-ArtQualityBacklog.ps1` / `generate_art_quality_backlog.py`，自动从 Manifest 和 Approved PNG 生成 `美术文档/_generated/素材质量替换清单.md/json`，并在 `art_quality_snapshots/` 留历史快照。
- 已将质量替换队列接入 `README.md`、`00_美术流水线总览.md`、`10_正式版核心纵切美术路线.md`、`13_正式纵切UI与素材覆盖矩阵.md` 和 `tools/美术工具/README.md`；后续程序接入看 `可接入素材清单`，美术精修看 `素材质量替换清单`。
- 已修复 4 张怪物头像 Approved PNG 的半透明边缘技术风险：`monster_mob_scavenger_bug_portrait`、`monster_mob_acid_slime_portrait`、`monster_elite_scrap_guard_portrait`、`monster_elite_mutant_amalgam_portrait` 现在符合 `AlphaRequired=false`。
- 已刷新质量清单快照 `美术文档/_generated/art_quality_snapshots/20260525_004827_monster_portrait_alpha_fixed_visual_v2_backlog.*`；当前 `technical_fix=0`、`visual_v2_replace=20`、`spec_review=0`。
- 已补齐 Visual V2 安全替换工具链：`Run-ArtGeneration.ps1` 支持 `-PreserveStatus`，`Optimize-ArtAssets.ps1` 支持 `-CandidateBatchID`，`Sync-ApprovedArt.ps1` 支持 `-CandidateBatchID` / `-QualityTier` / `-ClearCandidate`；Python 端兼容 `utf-8-sig` Manifest。
- 已用临时 Manifest 和 mock provider 验证 Visual V2 流程：`ui_button_primary` 测试样本保持 `Status=approved`，同步后写入 `QualityTier=formal_ai_v2` / `ReplacementBatchID`，并清理候选字段，不污染正式 Manifest 和 Approved 目录。
- 已刷新质量清单快照 `美术文档/_generated/art_quality_snapshots/20260525_011427_visual_v2_tooling_ready.*`；当前仍为 `technical_fix=0`、`visual_v2_replace=20`、`spec_review=0`，本轮没有真实素材替换。
- 已新增 `Generate-VisualV2Plan.ps1` / `generate_visual_v2_plan.py`，把 `visual_v2_replace` 队列转成 NovelAI 可执行批次计划；latest 为 `美术文档/_generated/VisualV2生成计划.md/json`，历史快照在 `美术文档/_generated/visual_v2_plan_snapshots/`。
- 已生成 Visual V2 执行计划 `nai_visual_v2_20260525_01`：计划替换 20 个 local_v0 资源，全部具备英文提示词、负面提示词和结构化 Spec，尺寸分布为 `512x512=19`、`1920x1080=1`。
- 已用 NovelAI 对 `ui_icon_diary` 做串行生成探测，失败原因为 HTTP 402：单张 512x512 需要 5 Anlas，当前仅 2 Anlas；未生成图片，也未把 mock 或失败输出同步为正式资源。
- 已刷新质量清单快照 `美术文档/_generated/art_quality_snapshots/20260525_013017_nai_visual_v2_probe_anlas_blocked.*`；当前仍为 `technical_fix=0`、`visual_v2_replace=20`、`spec_review=0`。
- 已把 P1 战斗可读性增补写入 `combat_hud` active 规格、组件目录、Formal V1 文档和迁移记录：新增 `Combat.IntentIcon`、`Combat.StatusIcon`、`Combat.HitFeedback`、`Combat.ShieldBreakFeedback`、`Combat.GridLockMarker`、`Combat.JunkPreviewMarker` 六类组件。
- 已在 `art_requirements_seed.json` 和提示词生成脚本中补齐 17 个战斗可读性 VisualID 的英文绘图提示词、中文说明、负面提示词和结构化 Spec；图标 / 反馈 / 背包格 overlay 尺寸分别按 `512x512`、`512x512`、`256x256` 管理。
- 已刷新 Manifest / Prompt / UI handoff / 可接入清单 / 质量清单并留档 `20260525_020335_p1_combat_readability_contract_prompt_fix.*` 与 `20260525_020357_p1_combat_readability_contract_prompt_fix.*`；当前 `generate_needed=21`、`technical_fix=0`、`visual_v2_replace=20`。
- 已新增 `Generate-ArtBatchPlan.ps1` / `generate_art_batch_plan.py`，把 `可接入素材清单` 中的 `generate_needed` 队列转成 NovelAI 可执行缺图跑图计划；latest 为 `美术文档/_generated/缺图生成计划.md/json`，历史快照在 `美术文档/_generated/art_generation_plan_snapshots/`。
- 已生成缺图执行计划 `nai_missing_assets_20260525_01`：计划生成 21 个缺失素材，全部具备英文提示词、负面提示词和结构化 Spec，构成为 P0 新节点图标 4 个、P1 战斗可读性 UI 素材 17 个；已记录 NovelAI 上次探测 HTTP 402 Anlas 不足，后续额度恢复后按计划串行跑图。
- 已新增 `Generate-LocalV0Art.ps1` / `generate_local_v0_art.py`，在 NovelAI Anlas 不足时为已有 Prompt / Spec 的缺图项生成明确标记的 local_v0 Approved 素材，不冒充正式 AI 产物。
- 已用 `local_v0_missing_assets_20260525_01` 补齐 21 个缺图 Approved PNG 和 Unity `.meta`：4 个新地图节点图标、17 个 P1 战斗可读性 UI 素材。latest 可接入清单已刷新为 `program_integrate=33`、`acceptance_needed=82`、`generate_needed=0`。
- 已刷新质量清单与 Visual V2 计划：当前 `technical_fix=0`、`visual_v2_replace=41`、`spec_review=0`，正式 AI 替换批次为 `nai_visual_v2_20260525_02`。
- 已将 `chassis_upgrade_panel` 从 Formal V1 草案推进到 active `screen_layouts.json` 规格，补齐 `ui_icon_chassis_upgrade`、`ui_icon_blueprint`、`ui_icon_material_need` 三个 preset UI 图标需求、Manifest / Prompt / Spec 和 local_v0 Approved PNG。
- 已刷新 latest 可接入素材清单、缺图生成计划、质量清单和 Visual V2 计划：当前 `program_integrate=36`、`acceptance_needed=82`、`generate_needed=0`、`technical_fix=0`、`visual_v2_replace=44`、`spec_review=0`，正式 AI 替换批次为 `nai_visual_v2_20260525_03`。
- 已将 `business_settlement` 作为 P4 经济压力链中 `shop_staging -> business_settlement -> daily_bill_report` 的中间营业反馈界面写入 active `screen_layouts.json`，补齐 `business_settlement_v1.md`、组件目录、迁移记录、路线文档和覆盖矩阵。
- 已补齐 `ui_icon_customer`、`ui_icon_sale_spark`、`ui_icon_business_settlement` 三个 preset UI 图标需求、英文提示词、中文说明、负面提示词和结构化 Spec，并用 `local_v0_business_settlement_20260525_01` 生成 Approved PNG 和 Unity `.meta`。
- 已刷新 latest 可接入素材清单、缺图生成计划、质量清单和 Visual V2 计划：当前 `program_integrate=39`、`acceptance_needed=82`、`generate_needed=0`、`technical_fix=0`、`visual_v2_replace=47`、`spec_review=0`，正式 AI 替换批次为 `nai_visual_v2_20260525_04`。
- 已补齐 `maintenance_panel` 维护可读性增量：新增 `ui_icon_wear_repair`、`ui_icon_corruption_purify`、`ui_icon_dive_permit` 三个 preset UI 图标需求、英文提示词、中文说明、负面提示词和结构化 Spec，并用 `local_v0_maintenance_readability_20260525_01` 生成 Approved PNG 和 Unity `.meta`。
- 已刷新 latest 可接入素材清单、缺图生成计划、质量清单和 Visual V2 计划：当前 `program_integrate=42`、`acceptance_needed=82`、`generate_needed=0`、`technical_fix=0`、`visual_v2_replace=50`、`spec_review=0`，正式 AI 替换批次为 `nai_visual_v2_20260525_05`。
- 已补齐 `daily_bill_report` 每日账单经济可读性增量：新增 `ui_icon_income`、`ui_icon_expense`、`ui_icon_debt_rent` 三个 preset UI 图标需求、英文提示词、中文说明、负面提示词和结构化 Spec，并用 `local_v0_daily_bill_readability_20260525_01` 生成 Approved PNG 和 Unity `.meta`。
- 已刷新 latest 可接入素材清单、缺图生成计划、质量清单和 Visual V2 计划：当前 `program_integrate=45`、`acceptance_needed=82`、`generate_needed=0`、`technical_fix=0`、`visual_v2_replace=53`、`spec_review=0`，正式 AI 替换批次为 `nai_visual_v2_20260525_06`。
- 已新增 `Generate-FormalV1AcceptanceQueue.ps1` / `generate_formal_v1_acceptance_queue.py`，从 active UI 规格、Manifest、latest ArtAcceptance 和 Registry 快照生成 Formal V1 运行时美术验收队列；当前 `screens=21`、`captured=15`、`program_register_visuals=10`、`review_previous_screenshot=11`，最新快照为 `美术文档/ui_design/_generated/formal_v1_acceptance_snapshots/20260525_235820_local_v0_quality_normalized.*`。
- 已新增 `Generate-ArtProgramHandoff.ps1` / `generate_art_program_handoff.py`，把 `可接入素材清单` 与 `FormalV1验收队列` 合成为程序侧一站式交接清单；latest 为 `美术文档/_generated/程序接入交接清单.md/json`，当前汇总 `program_integrate=45`、`add_capture=6`、`rerun_acceptance=15`，快照为 `美术文档/_generated/art_program_handoff_snapshots/20260525_045747_program_handoff_20260525_01.*`。
- 已按 `40_局外成长底盘正式配置落地设计.md` 和 `42_局外成长义体正式配置落地设计.md` 前置补齐 9 个正式成长 VisualID：`chassis_standard_frame_icon`、`chassis_compact_raider_icon`、`chassis_bulwark_carrier_icon`、`prosthetic_focus_lens_icon`、`prosthetic_anchor_left_arm_icon`、`prosthetic_charge_coil_arm_icon`、`prosthetic_san_regulator_core_icon`、`prosthetic_salvage_fingertips_icon`、`prosthetic_mender_spine_icon`；已补 seed、Prompt / Spec、local_v0 Approved PNG 和 Unity `.meta`。
- 已刷新 latest 可接入清单、缺图生成计划、质量清单、Visual V2 计划、Formal V1 验收队列和程序交接清单：当前 `program_integrate=54`、`generate_needed=0`、`technical_fix=0`、`visual_v2_replace=62`，正式 AI 替换批次为 `nai_visual_v2_20260525_07`。
- 已按 `44_局外成长人偶特质房间正式配置落地设计.md` 前置补齐 8 个 Room Memento VisualID：`memento_first_repair_patch`、`memento_first_chassis_frame`、`memento_first_prosthetic_case`、`memento_san_collapse_blanket`、`memento_miracle_burn_mark`、`memento_boss1_lamp`、`memento_layer2_corrosion_vial`、`memento_return_mark`；已补 seed、Prompt / Spec、local_v0 Approved PNG 和 Unity `.meta`。
- 已刷新 latest 可接入清单、缺图生成计划、质量清单、Visual V2 计划、Formal V1 验收队列和程序交接清单：当前 `program_integrate=62`、`generate_needed=0`、`technical_fix=0`、`visual_v2_replace=70`，正式 AI 替换批次为 `nai_visual_v2_20260525_08`。
- 已按 `48_经济压力传闻正式配置落地设计.md` 和 `49_经济压力势力正式配置落地设计.md` 前置补齐 13 个 P4 经济压力 VisualID：8 个传闻图标与 5 个势力徽章；已补 seed、Prompt / Spec、local_v0 Approved PNG 和 Unity `.meta`。
- 已对 13 个 P4 经济压力图标做 focused verification：PNG 均为 `512x512`，有 alpha，主体 bbox 居中；Manifest 均为 `Status=approved`、`QualityTier=local_v0`，`PromptEN` 和结构化 `Spec.SourceSpec=512x512` 齐全。
- 已按 `50_经济压力订单正式配置落地设计.md` 前置补齐 29 个 P4 经济压力订单 VisualID：19 个订单模板图标与 10 个订单类型 / 状态复用图标；已补 seed、Prompt / Spec、local_v0 Approved PNG 和 Unity `.meta`。
- 已对 29 个 P4 经济压力订单图标做 focused verification：PNG 均为 `512x512` RGBA，含 alpha，主体 bbox 居中；Manifest 均为 `Status=approved`、`QualityTier=local_v0`，`PromptEN`、`NegativePromptEN` 和结构化 `Spec.SourceSpec=512x512` 齐全。
- 已在 P4 经济压力传闻 / 势力 / 订单批次后刷新 latest 可接入清单、缺图生成计划、质量清单、Visual V2 计划、Formal V1 验收队列和程序交接清单。
- 已把 `settlement` 升级为 CombatOutcomeReport 五态结果报告 active 合同：新增胜利、HP 战败、SAN 崩溃、HP+SAN 复合战败和队伍溃败五个结果徽记 VisualID，并补齐 seed、Prompt / Spec、local_v0 Approved PNG 和 Unity `.meta`。
- 已在 `settlement` 五态结果徽记批次后刷新 latest 可接入清单、缺图生成计划、质量清单、Visual V2 计划、Formal V1 验收队列和程序交接清单。
- 已新增 `Scan-ArtRequirementCandidates.ps1` / `scan_art_requirement_candidates.py`，从设计文档、配置表、版本规划和 active UI 文档扫描潜在美术需求候选；latest 输出 `new_candidate=36`、`approved_without_manifest=0`、`seed_only=0`、`manifest_managed=158`，快照为 `美术文档/_generated/art_requirement_candidate_snapshots/20260525_074546_art_requirement_candidates_v2_20260525.*`。
- 已新增 `Normalize-ArtQualityTier.ps1` / `normalize_art_quality_tier.py`，把历史本地生成 Approved 资产规范化为 `QualityTier=local_v0`：本轮识别 `legacy_candidates=132`，实际补标 35 条，正式质量层级 `formal_ai_v2/final/production` 不会被降级。
- 已验证 NovelAI token 链路并完成首个正式 AI 替换：`node_eventnode_icon` 已生成、预处理并同步到 `UnityClient/Assets/Art/Approved/Nodes/Icons/node_eventnode_icon.png`，Manifest 中 `QualityTier=formal_ai_v2`、`ReplacementBatchID=nai_visual_v2_probe_20260525_02`。
- 已完成 P0 新节点图标正式 AI 替换：`node_hazardnode_icon`、`node_reststopnode_icon`、`node_treasurenode_icon` 已通过 NovelAI 串行生成、预处理和 Approved 同名替换；连同 `node_eventnode_icon`，4 个新节点图标均为 `QualityTier=formal_ai_v2`。
- 已刷新 latest 队列：`program_integrate=0`、`acceptance_needed=191`、`generate_needed=0`、`technical_fix=0`、`visual_v2_replace=128`；Visual V2 下一执行批次为 `nai_visual_v2_20260526_02`，`PromptReadyItems=128`。
- 已完成 P1 核心战斗意图图标正式 AI 替换：`ui_combat_intent_attack`、`ui_combat_intent_defend`、`ui_combat_intent_buff`、`ui_combat_intent_debuff` 均已串行生成、预处理、人工筛选并同步为 `QualityTier=formal_ai_v2`；`debuff` 已因首轮语义偏差修正提示词模板和专用负面词。
- 已刷新 latest 队列：`program_integrate=0`、`generate_needed=0`、`technical_fix=0`、`visual_v2_replace=124`，Visual V2 下一执行批次为 `nai_visual_v2_20260526_03`；Formal V1 验收队列显示 21 个 active 界面均已 captured，当前进入美术截图验收。
- 已加固 Visual V2 Approved 同名替换工具：`Sync-ApprovedArt.ps1 -CandidateBatchID` 默认拒绝创建新 Approved 路径，要求目标 PNG / `.meta` 已存在并校验 `.meta` 同步前后不变；`-AllowNewTargetWithCandidate` 仅用于明确创建新资产路径，不用于已接入素材的正式图替换。已用临时 Manifest 验证 strict dry-run、缺目标默认失败和显式放行新目标三种路径。
- 已完成 P1 首批战斗反馈 / 标记正式 AI 替换：`ui_combat_feedback_hit`、`ui_combat_feedback_shield_break`、`ui_combat_grid_lock_marker` 已通过 NovelAI 串行生成、预处理、contact sheet 人工筛选，并以 `meta_guard=strict` 同名同步为 `QualityTier=formal_ai_v2`；Approved PNG 尺寸 / alpha 校验通过，Unity `.meta` 未改变。
- 已刷新 latest 队列：`program_integrate=0`、`generate_needed=0`、`technical_fix=0`、`visual_v2_replace=121`，Visual V2 下一执行批次为 `nai_visual_v2_20260526_04`。
- 已主动触发并验收程序接入后的 latest ArtAcceptance：RunID=`20260527_002436`，21/21 截图、`PASSED`、Registry EntryCount=191、MissingRequiredVisualIDs=0、UI snapshot risks=0；已刷新 Formal V1 验收队列和程序交接清单，当前 `program_integrate=0`、`add_capture=0`、`rerun_acceptance=0`。
- 已把本轮人工验收写入 `美术文档/09_运行时美术验收记录.md`：`inventory_loot`、`settlement` 通过；`layer_select`、`maintenance_panel`、`daily_bill_report`、`business_settlement`、`chassis_upgrade_panel`、`doll_interaction`、`doll_room`、`order_board`、`rumor_board`、`faction_shop`、`scenario_event`、`shop_staging`、`workshop_main` 条件通过；`combat_hud`、`dungeon_map`、`safe_room`、`stairs_room`、`sell_panel`、`prosthetic_panel` 不通过，需程序侧返修截图状态或补有效列表数据后重跑。
- 已建立 Formal V2 UX/UI 重构设计层：新增 `美术文档/ui_design/formal_v2/README.md`、`00_formal_v2_ux_ui_overview.md` 和 V2-A 五个核心界面设计入口，明确 Formal V2 先解决按钮堆叠、主次行动不清、场景隐喻不足和正式感不足。
- 已将 V2-A 五个核心界面从入口占位推进为可评审草案：`01_workshop_main_v2.md`、`02_combat_hud_v2.md`、`03_inventory_loot_v2.md`、`04_dungeon_map_v2.md`、`05_settlement_v2.md` 均已覆盖 Formal V1 问题、玩家目标、主结构、信息层级、行动层级、程序迁移、素材变化和 UX 验收标准。
- 已在 Formal V2 总方案补充工具策略：Figma、Unity MCP、截图标注和 PlayMode 布局扫描只作为设计 / 验收辅助，不替代 active `screen_layouts.json`、Manifest、程序交接清单和 ArtAcceptance。

## 下一步建议

1. 先让用户审 `workshop_main` Formal V2 方案，确认工坊 Hub 空间骨架、深渊入口主行动、功能入口降级和是否需要低保真线框。
2. 再依次审 `combat_hud`、`inventory_loot`、`dungeon_map`、`settlement`；五个界面确认后，再决定逐界面迁移 active 还是先补 Figma / 静态线框。
3. 程序侧在 Formal V2 active 更新前继续按 Formal V1 `screen_layouts.json` 工作；不要按 `formal_v2/*.md` 草案接入 Unity。
4. 程序侧仍可并行返修 Formal V1 ArtAcceptance 截图状态：`combat_hud`、`dungeon_map`、`safe_room`、`stairs_room` 清理叠层；`sell_panel` 和 `prosthetic_panel` 补有效列表 / 配方行。
5. 美术侧按 `美术文档/_generated/VisualV2生成计划.md` 的 `nai_visual_v2_20260526_04` 继续串行替换 121 个 local_v0 素材；每次只请求 1 张图，图间隔 1 秒，替换仍走 `PreserveStatus -> CandidateBatchID -> QualityTier=formal_ai_v2` 流程，并保持同 VisualID、同 Approved 路径、同 Unity `.meta` / GUID。

## 问题 / 阻塞

- 当前工作区仍有大量程序、策划、Unity 资产和知识库生成物处于脏状态；美术侧提交时只纳入本轮美术流水线相关文件。
- P0 的 `validated` 是 MVP Baseline 骨架验收通过，不代表 Formal V1 正式结构已通过；本轮 latest `20260527_002436` 已确认资源接入通过，但 `combat_hud` 等 6 个界面仍需程序返修后复验。
- NovelAI token / 单图生成链路已验证，P0 节点与 P1 核心战斗意图图标正式替换已完成；完整批量生成仍取决于 Anlas 余额。local_v0 可接入图标只用于先解锁程序接入和运行时验收，后续需要替换为正式 AI 美术版。
- latest `program_integrate=0` 不等于美术最终通过；当前还有 6 个 Formal V1 界面需程序返修后复验，以及 121 个 local_v0 Visual V2 替换项，正式验收时可以验收结构、绑定和可读性，但不应把 local_v0 视为最终视觉质量。
- Formal V2 目前只是 draft 设计层；如果程序侧需要接入，必须等待用户确认并由美术侧更新 active `screen_layouts.json`。
- `tools/ComfyUI_NAIDGenerator/` 未跟踪，需要决定是否纳入正式美术流水线。

## 完成回写清单

- 更新本文件的 `最后更新`。
- 在 `最近完成` 记录简短事实。
- 如有变化，刷新 `当前关注`、`下一步建议` 和阻塞项。
- 如果需要程序或策划跟进，在 `PROJECT_STATUS.md` 增加跨职能交接。
- 美术侧每轮实际交付完成后，提交本轮美术相关改动；提交范围必须排除程序、策划、子模块或本地工具无关改动。

