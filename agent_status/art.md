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
  - 版本规划/12_正式版长期版本节点规划.md
  - agent_status/README.md
  - PROJECT_STATUS.md
  - 开发文档/00_程序开发大纲.md
  - 开发文档/14_Unity运行时美术自动验收方案.md
  - 开发文档/09_视觉资源系统程序开发规范.md
  - agent_status/program.md
  - 设计文档/GDD_00_系统关联总图.md
  - agent_status/design.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - 美术文档/13_正式纵切UI与素材覆盖矩阵.md
  - 美术文档/ui_design/ui_iteration_process.md
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/README.md
  - 知识库/views/art.md
last_verified: 2026-05-25
update_rule: 美术或 UI 视觉流水线任务完成后更新本文件。
---

# 美术 / UI 状态

## 最后更新

2026-05-25

## 当前关注

支撑正式版核心纵切。按最新 `09` 路线，美术 / UI 作为 P5 表现支撑，只围绕当前 P0-P4 功能纵切补表达、资源和截图验收，不继续横向铺所有界面。

PM 版本节点中，美术线当前 21 个界面都已具备 Formal V1 active 规格。`combat_hud` 战斗资源、`maintenance_panel` / `daily_bill_report` 新增图标、A4 的 `shop_staging` / `order_board` / `rumor_board` local_v0 图标已由 UI 程序侧接入并通过 ArtAcceptance `20260524_212423`；`doll_room`、`chassis_upgrade_panel` 和 `business_settlement` 已进入 active。当前 latest `program_integrate=62`，包括 4 个新地图节点图标、17 个 P1 战斗可读性素材、此前 12 个 P2 当前依赖素材、3 个底盘升级图标、3 个营业结算图标、3 个维护可读性图标、3 个每日账单经济图标、9 个正式局外成长底盘 / 义体前置素材，以及 8 个 Room Memento 房间纪念物资产；美术侧等待程序接入后统一截图验收。

美术侧已把“可接入覆盖”、“缺图生成”和“视觉质量替换”拆开：`可接入素材清单.md` 给程序看，`缺图生成计划.md` 给美术侧执行 `generate_needed` 新素材跑图，`素材质量替换清单.md` 给美术侧执行 local_v0 / placeholder 同名替换。当前 `generate_needed=0`，质量清单为 `technical_fix=0`、`visual_v2_replace=70`，表示没有必须先修的技术风险，所有 local_v0 素材不阻塞程序接入，后续按 Visual V2 同名替换。

P1 战斗可读性 active 合同已补齐：`combat_hud` 现在包含怪物意图图标、战斗状态图标、命中 / 破盾反馈、封格 / 塞包 overlay。P3 底盘升级 active 合同已补齐：`chassis_upgrade_panel` 包含当前底盘、下一底盘、容量变化、材料缺口、蓝图前置和升级确认。P3 维护可读性 active 合同已补齐：`maintenance_panel` 现在把磨损修复、侵蚀净化和下潜许可拆成独立 VisualID。P3 Room Memento 已按 `44_局外成长人偶特质房间正式配置落地设计.md` 前置补齐 8 个房间纪念物 VisualID。P4 营业结算 active 合同已补齐：`business_settlement` 位于 `shop_staging` 和 `daily_bill_report` 之间，承接顾客流、成交反馈、未售出 / 黑市风险摘要和进入账单动作。P4 每日账单 active 合同已补齐：`daily_bill_report` 现在把收入、支出和月租债务拆成独立 VisualID。latest 可接入清单当前为 `program_integrate=62`、`acceptance_needed=82`、`generate_needed=0`；新增缺图项均已有 Approved PNG 和 `.meta`，但质量层级为 `local_v0`，不视为最终美术。

Visual V2 执行入口已补齐：`美术文档/_generated/VisualV2生成计划.md/json` 当前规划 70 项替换资源，`PromptReadyItems=70`，其中背景 1 项、节点图标 4 项、底盘 / 义体 9 项、Room Memento 8 项、UI 图标 / 反馈 / 槽位 48 项。最近 NovelAI 探测批次 `nai_visual_v2_probe_20260525_01` 因 HTTP 402 Anlas 不足失败；该问题不属于提示词、Manifest、串行生成或预处理脚本缺口。

Visual V2 工具链已补齐安全替换流程：已接入素材可用 `Run-ArtGeneration.ps1 -PreserveStatus` 生成候选，用 `CandidateBatchID` 只预处理本批 raw，再用 `Sync-ApprovedArt.ps1 -QualityTier formal_ai_v2 -ClearCandidate` 同名替换 Approved。该流程不会把原 `approved` / `registered` / `validated` 状态回退到 `generated`。

美术文档已收敛为四层入口：`README.md` 只做导航，`10_正式版核心纵切美术路线.md` 作为当前规划入口，`00_美术流水线总览.md` 作为端到端资产生产工作流入口，`ui_design/README.md` 作为 UI 版本和 active 规格入口。`archive/` 保存 MVP 记录和旧批次交付快照。

Formal V1 运行时验收已工具化：`Generate-FormalV1AcceptanceQueue.ps1` 会从 active `screen_layouts.json`、Manifest、latest ArtAcceptance 和 Registry 快照生成 `美术文档/ui_design/_generated/FormalV1验收队列.md/json`。程序侧交接已进一步收敛到 `美术文档/_generated/程序接入交接清单.md/json`，该清单合并 `program_integrate`、截图覆盖和 ArtAcceptance 重跑队列。当前交接为 `VisualAssetRegistry register count=62`、`Screens needing ArtAcceptance capture coverage=6`、`Screens needing ArtAcceptance rerun=15`；latest ArtAcceptance `20260524_212423` 已覆盖 15 个截图点，但早于当前 active 规格。

## 必读文件

- `知识库/views/art.md`
- `美术文档/README.md`
- `美术文档/10_正式版核心纵切美术路线.md`
- `美术文档/00_美术流水线总览.md`
- `美术文档/ui_design/README.md`
- `美术文档/ui_design/ui_iteration_process.md`
- `美术文档/ui_design/formal_v1/screen_structure_review.md`
- `美术文档/01_Manifest规范.md`
- `开发文档/09_视觉资源系统程序开发规范.md`
- `开发文档/14_Unity运行时美术自动验收方案.md`

## 工作边界

- 运行时 UI 目标是纯 UGUI。不要新增 UI Toolkit、UXML、USS 或 `UIDocument` 运行时流程。
- `美术文档/ui_design/screen_layouts.json` 是当前 active UI 对接规格；程序接入、素材生成和可接入素材清单只认 active。
- MVP UI 设计已冻结到 `美术文档/ui_design/versions/mvp_baseline_2026-05-22/`，作为历史基线和回退参考。
- Formal V1 先写在 `美术文档/ui_design/formal_v1/`；用户确认后再逐界面修改 active。
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
- 已复核最新 `版本规划/09`、`11`、`12`，并将美术侧推进口径同步为：P5 不独立铺量，只服务 P0-P4 当前纵切；`知识库/views/art.md`、`10_正式版核心纵切美术路线.md`、`13_正式纵切UI与素材覆盖矩阵.md` 和 UI versions 入口已更新。
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
- 已新增 `Generate-FormalV1AcceptanceQueue.ps1` / `generate_formal_v1_acceptance_queue.py`，从 active UI 规格、Manifest、latest ArtAcceptance 和 Registry 快照生成 Formal V1 运行时美术验收队列；当前 `screens=21`、`captured=15`、`program_register_visuals=9`、`review_previous_screenshot=12`，最新快照为 `美术文档/ui_design/_generated/formal_v1_acceptance_snapshots/20260525_044724_formal_v1_acceptance_queue_20260525_02.*`。
- 已新增 `Generate-ArtProgramHandoff.ps1` / `generate_art_program_handoff.py`，把 `可接入素材清单` 与 `FormalV1验收队列` 合成为程序侧一站式交接清单；latest 为 `美术文档/_generated/程序接入交接清单.md/json`，当前汇总 `program_integrate=45`、`add_capture=6`、`rerun_acceptance=15`，快照为 `美术文档/_generated/art_program_handoff_snapshots/20260525_045747_program_handoff_20260525_01.*`。
- 已按 `40_局外成长底盘正式配置落地设计.md` 和 `42_局外成长义体正式配置落地设计.md` 前置补齐 9 个正式成长 VisualID：`chassis_standard_frame_icon`、`chassis_compact_raider_icon`、`chassis_bulwark_carrier_icon`、`prosthetic_focus_lens_icon`、`prosthetic_anchor_left_arm_icon`、`prosthetic_charge_coil_arm_icon`、`prosthetic_san_regulator_core_icon`、`prosthetic_salvage_fingertips_icon`、`prosthetic_mender_spine_icon`；已补 seed、Prompt / Spec、local_v0 Approved PNG 和 Unity `.meta`。
- 已刷新 latest 可接入清单、缺图生成计划、质量清单、Visual V2 计划、Formal V1 验收队列和程序交接清单：当前 `program_integrate=54`、`generate_needed=0`、`technical_fix=0`、`visual_v2_replace=62`，正式 AI 替换批次为 `nai_visual_v2_20260525_07`。
- 已按 `44_局外成长人偶特质房间正式配置落地设计.md` 前置补齐 8 个 Room Memento VisualID：`memento_first_repair_patch`、`memento_first_chassis_frame`、`memento_first_prosthetic_case`、`memento_san_collapse_blanket`、`memento_miracle_burn_mark`、`memento_boss1_lamp`、`memento_layer2_corrosion_vial`、`memento_return_mark`；已补 seed、Prompt / Spec、local_v0 Approved PNG 和 Unity `.meta`。
- 已刷新 latest 可接入清单、缺图生成计划、质量清单、Visual V2 计划、Formal V1 验收队列和程序交接清单：当前 `program_integrate=62`、`generate_needed=0`、`technical_fix=0`、`visual_v2_replace=70`，正式 AI 替换批次为 `nai_visual_v2_20260525_08`。

## 下一步建议

1. 程序侧优先按 `美术文档/_generated/程序接入交接清单.md` 推进：登记 62 个 VisualID，补 `business_settlement`、`chassis_upgrade_panel`、`doll_interaction`、`doll_room`、`faction_shop`、`scenario_event` 6 个 ArtAcceptance 截图点，并在登记后重跑 15 个已有截图界面。
2. NovelAI Anlas 恢复后，优先按 `美术文档/_generated/VisualV2生成计划.md` 的 `nai_visual_v2_20260525_08` 串行替换 70 个 local_v0 素材；替换仍走 `PreserveStatus -> CandidateBatchID -> QualityTier=formal_ai_v2` 流程。
3. 程序接入后，美术侧基于 `美术文档/ui_design/_generated/FormalV1验收队列.md` 和最新 ArtAcceptance 逐屏验收 21 个 active Formal V1 界面的结构、缺图、黑块、点击遮挡、列表有效数据和图标可读性。
4. 缺图生成计划 latest 当前为 `planned=0`；后续只有新增配置 / preset 导致新的 `generate_needed` 时再启用 `Generate-ArtBatchPlan.ps1`。
5. 每次替换或技术修复 Approved PNG 后，同时刷新 `可接入素材清单` 和 `素材质量替换清单`，并分别保留 integration / quality snapshot。
6. 后续新增怪物时继续按 `CombatVisualID -> monster_*_combat -> Approved/Monsters/Combat` 流程补图。

## 问题 / 阻塞

- 当前工作区仍有大量程序、策划、Unity 资产和知识库生成物处于脏状态；美术侧提交时只纳入本轮美术流水线相关文件。
- P0 的 `validated` 是 MVP Baseline 骨架验收通过，不代表 Formal V1 正式结构已通过；P0 / P1 Formal V1 仍需要程序接入后的运行时截图验收。
- `combat_hud` 已有 2026-05-24 最新 ArtAcceptance 截图可用于当前规格验收；其他界面的 UI 迭代候选报告仍可能基于较旧截图，需要逐界面确认。
- NovelAI 当前因 Anlas 余额不足无法继续正式跑图；local_v0 可接入图标只用于先解锁程序接入和运行时验收，后续需要替换为正式 AI 美术版。
- 62 个 latest `program_integrate` 素材中有多项仍是 local_v0；正式验收时可以验收结构、绑定和可读性，不应把 local_v0 视为最终视觉质量。
- `tools/ComfyUI_NAIDGenerator/` 未跟踪，需要决定是否纳入正式美术流水线。

## 完成回写清单

- 更新本文件的 `最后更新`。
- 在 `最近完成` 记录简短事实。
- 如有变化，刷新 `当前关注`、`下一步建议` 和阻塞项。
- 如果需要程序或策划跟进，在 `PROJECT_STATUS.md` 增加跨职能交接。
- 美术侧每轮实际交付完成后，提交本轮美术相关改动；提交范围必须排除程序、策划、子模块或本地工具无关改动。

