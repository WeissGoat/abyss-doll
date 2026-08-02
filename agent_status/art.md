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
  - 开发文档/rules/03_视觉资源系统程序开发规范.md
  - agent_status/program.md
  - 设计文档/GDD/GDD_00_系统关联总图.md
  - agent_status/design.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - 美术文档/05_AI图片网关接入方案.md
  - 美术文档/13_正式纵切UI与素材覆盖矩阵.md
  - 美术文档/ui_design/ui_iteration_process.md
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/README.md
  - 知识库/views/art.md
  - 美术文档/16_Live2D角色动画资产接入规格.md
  - 美术文档/17_Agent原生动态立绘资产接入规格.md
  - 美术文档/18_CG底图与漫画式播放演出工作流.md
  - 美术文档/19_T0-01序章CG细案.md
  - 美术文档/20_GIF小循环人物替换工作流.md
  - .codex/skills/p3-generate-image/SKILL.md
  - .codex/skills/p3-art-asset-production/SKILL.md
  - .codex/skills/p3-narrative-cg-comic/SKILL.md
  - 版本规划/0-12小时细案/T0-01_序章首次循环开发总方案.md
  - 美术文档/人设/README.md
  - 美术文档/人设/01_人设参考获取规则.md
  - 美术文档/人设/02_零号原型参考_失明少女.md
  - 美术文档/人设/03_零号初版人设方案.md
  - 美术文档/人设/05_零号立绘素材设计与交付清单.md
  - 开发文档/17_Live2DSpine运行时接入评估.md
  - 开发文档/18_全局叙事播放系统开发方案.md
last_verified: 2026-08-02
update_rule: 美术或 UI 视觉流水线任务完成后更新本文件。
---

# 美术 / UI 状态

## 最后更新

2026-08-02

## 当前关注

- `formalv2_catalog_v2_bridge_batch_20260801_01_exec` 已完成首轮真实 Catalog V2 通用批量执行：`bg_combat_abyss` 与 `ui_icon_warning` 各生成 2/2 张可解码 raw，均通过技术门禁并发布到不可变 `processed/5`；Agent 评审后分别以 95 分选择 `processed/5/001.png`、以 97 分选择 `processed/5/002.png`，两项都通过当前 selected 基线、严格分数增量和保护维度校验。生产 Run 为 `selection_complete`；随后 `art_import_formalv2_catalog_v2_bridge_batch_20260802_01` 已把最新 selected 幂等同步到 Approved，并通过 live Unity importer、Registry 唯一项和目标 Console 校验，当前公开状态为 `registered`，不包含 UGUI 绑定、ArtAcceptance 或 `runtime_validated`。生产证据：`UnityClient/Logs/P3ArtProduction/formalv2_catalog_v2_bridge_batch_20260801_01_exec/{summary,generation-summary,visual-review,selection-decision}.json`。
- 美术生产安全收口已完成实现：正式 v2 generation evidence 与 legacy recovery evidence 隔离；Registrar 重算 `technical_review_v2` 并要求外部授权的 override；selected replacement 使用当前基线、严格分数增量和保护维度门禁；角色立绘 `Run-CharacterPortraitSet.ps1` 已支持 `Automatic` / `Resume`、processing decision、visual review、不可变 generation snapshot 和依赖阻塞。压力审计后补上 Resume 旧 checkpoint 绕过、旧 processed 轮次回退、缺失 visual-review SHA 和棋盘底误处理的回归保护，并闭合人物立绘 override 状态计算。全量美术工具回归为 271 tests passed。
- `zero_cold_reference_pilot_20260726_02` 已打通首个正式角色差分参考图闭环：`SourceAssets` 解析到 neutral Approved 的真实路径、SHA-256、尺寸和模式，exact `prompt-002` 通过 Gemini `image_to_image` 串行生成 2 张可解码 raw；第二张经显式连通边界背景处理、1024x1536 RGBA 规范化和人工多背景边缘检查后发布为不可变 `processed/3/001.png`。新候选以 92 分通过 guarded selection，严格高于旧 selected 的 90 分，并复用 `selected/001.png` 及原 `.meta`；当前公开状态为 `selected`。cold Approved 仍不存在，neutral Approved、`.meta`、Registry 和 Registry `.meta` 哈希未变，本轮不声明 Unity、Registry 或运行时完成。证据：`UnityClient/Logs/P3ArtProduction/zero_cold_reference_pilot_20260726_02/{summary,visual-review,selection-decision}.json`。
- `formalv2_standard_stylebatch_20260725_01` 已完成首个新风格通用批量全自动闭环：`bg_combat_abyss` 与 `ui_icon_warning` 各有 1 张本批可解码 raw、各有 1 次 HTTP 504，均发布为不可变 `processed/4`，经 Agent 93 / 96 分 guarded selection 后完成同 VisualID Approved 替换。`art_import_formalv2_standard_stylebatch_20260725_01` 的 live Unity importer、Registry 唯一项、路径和 GUID 全部通过，两项 Manifest 均保持 `Status=approved` 并写 `RegistryStatus=registered`；Approved SHA-256 为 `880e6125f860712edecec1a943e6a2a8de79d9b64117b799631b5ceab7745477` / `a76de6477380f27a1a32dfbffdea7b08599e7812733eb2c15e725a5abfb46e19`，原 `.meta`/GUID 保持不变。该结论不包含 UGUI 绑定或 `runtime_validated`。预检同时修复了 `Compile-ArtGenerationRequests.ps1` 在 Windows PowerShell 5.1 下的中文默认路径失败，并增加 wrapper dry-run 回归测试。
- Agent 主导双格式 PromptRevision v2 已落地：编译器只产出 Requirement context，Agent 独立发布不可变 `natural_language_v2` / `danbooru_tags_v2`，provider adapter 只做确定性序列化；通用批量和角色立绘都消费 exact active Revision。当前 313 个 Requirement 全部 `ready`，其中 `bg_combat_abyss`、`ui_icon_warning`、`doll_zero_cold` 已发布独立 Agent-authored `prompt-001`，其余 310 项保持 `prompt_authoring_required`；旧 v1 仅保留为 `LegacyPromptVariants`。`formalv2_catalog_v2_bridge_dryrun_20260801_01` 已验证主动替换 planner 严格解析 Catalog v2、校验 Manifest pointer，并为背景 / 图标分别编译 `openai_images + natural_language_v2 + exact prompt-001` 命令：2 planned、2 ready、2 groups、0 blocked、Catalog strict 通过，claim ceiling 为 `dry_run_planned`。证据位于 `UnityClient/Logs/P3ArtProduction/formalv2_catalog_v2_bridge_dryrun_20260801_01/`；本轮未调用 provider，未修改 raw、processed、selected、Approved、Unity、Registry 或运行时资产。
- 美术生产按 `standard_asset`、`character_portrait_set`、`_legacy_runs` 分 Profile 管理；Manifest 为 313 个 Entry 持久化 `RequestID + RequirementFingerprint + PromptAuthoringStatus + ActivePromptRevisionID`，Catalog 保存 Requirement 与不可变双格式 PromptRevision，标准批量 / 角色套组共用门禁；标准批量已完成真实 provider 试跑并保留完整 raw / processed / review 证据。
- Formal V2 UI Skin 专项 route 已通过首个真实 Pilot：`ui_skin=deterministic_template` 会由批量执行器分流到确定性 NineSlice 适配器；`ui_button_primary` 已发布 `processed/3` 并完成 Agent guarded selection，公开状态停在 `selected`，Approved / Unity / Registry 未变化。
- 首个 `character_portrait_set` 正式接入试跑已到 `registered`：`doll_zero_dialogue_neutral` 已同步到 Approved，Unity importer 与尺寸符合合同，Registry 唯一条目、路径和 GUID 均匹配；本轮明确不含 Prefab / UGUI 绑定和运行时验收。
- 正式 UI / 素材接入仍以 active `screen_layouts.json`、Manifest、程序交接清单和 ArtAcceptance 为准；静态预览、contact sheet 或生成物不能替代运行时证据。
- T0-01A 的 CG / 漫画页和 FormalV2 工坊素材继续按锚点、一致性、Approved、Unity 接入和运行时验收顺序推进。
- GIF 一致性优化已接入逐帧 `ACTION_TEXT` 边车；真实 Illya 8 帧 GIF 已完成生成与重编码，白发校服身份比旧流程稳定，当前因第 4 帧可见跳变和自动 `temporal_flicker` 标记处于 `review_required`。
## 最近完成

- `formalv2_standard_ui_batch_20260801_01` completed the Formal V2 standard batch for `bg_dungeon_map`, `ui_icon_locked`, and `ui_icon_money`: 4/4 raw each, latest numeric `processed` rounds passed technical gates, and Agent visual review selected scores were 93 / 99 / 97. The batch summary is now `selection_complete`; the three Manifest entries remain `approved`, and this production run claims only `selected`.
- The same batch completed protected Approved -> Unity import -> Registry integration in `art_import_formalv2_standard_ui_batch_20260801_01`: all three Registry entries have `match_count=1` and `TryGetEntry=true`, target Console error/warning count is 0, and the public claim is `registered`, without UGUI binding or `runtime_validated`.
- Fixed `Select-ArtCandidate.ps1` nested `batch/<run>/summary.json` lookup by `ProductionRunID`; the regression suite is 13/13 passing.
- `formalv2_catalog_v2_bridge_batch_20260801_01_exec` 的两项最新 selected 已在 `art_import_formalv2_catalog_v2_bridge_batch_20260802_01` 完成 `selected -> Approved -> Unity import -> Registry`：Approved 与 selected SHA-256 分别为 `5a784c6913a559ea15a0a3f04e71017a27295984ab7df79ef030487e570b5271`、`a10b80b1bf5d646a6612c47c3c49d08d54b8d3fdec3a7330e2b8dc6c830debf4`；原 `.meta` / GUID 保持不变，Registry 各 `match_count=1`、`TryGetEntry=true`，目标 Console error / warning 为 0。Run `summary.claim=registered`，不包含 UGUI 绑定、ArtAcceptance 或 `runtime_validated`。
- PromptRevision Catalog v2 迁移已完成：313/313 Requirement `ready`，3 个 Pilot `prompt_ready`、310 个 `prompt_authoring_required`，共 3 个已发布 Revision。美术工具全量 `215 tests passed`，Catalog strict 与文档校验通过；标准批量按 `PromptRevisionID` 隔离生成组，角色套组 dry-run 解析 exact Revision 和参考关系。保护字段 / Approved / `.meta` / Registry 哈希保持不变。
- `formalv2_ui_button_primary_skin_pilot_20260719_01` 已打通 UI Skin 专项批次：两张 512x160 transparent raw 候选发布到不可变 `processed/3`，两者 NineSlice 技术门禁均通过、ConnectedComponents=1，四向边带覆盖为 top/bottom 90.23%、left/right 86.25%。Agent 对原尺寸和 220x64 预览评审后选择 `001.png`，评分 92、领先 6 分；selected SHA-256 为 `005bbbaa7ef2139d5d637e18f82dffc9b64b51aa8f3efdbeb044b5035df82ed4`，Approved 保持 `83ce19e0bbf22c628dff748f095ff9b23286b5c37dae3462687c0e85399cf8bd`，Run 为 `selection_complete`。
- 通用批量流已补齐可恢复执行与 guarded selection：`Run-ArtProductionBatch.ps1` 可消费缺图或 Formal V2 主动替换计划，按运行时 `AssetClass -> provider` 分组执行现有生成 / 预处理工具，并以本批可解码 raw 阻断旧报告误判；`Select-ArtCandidate.ps1` 只允许最新通过轮次、Agent 评分至少 88 的候选进入 selected，保留 Approved 主状态。
- `doll_zero_dialogue_neutral` 已完成 `selected -> approved -> unity_imported -> registered`：Approved / selected SHA-256 均为 `fd1d68528e685fd9075b294bb9b0ca903e4eddcddd2deab2db323def29180284`，Unity GUID 为 `7d7b3a5d2f28634469b19bb5aa52664b`，Manifest 保持 `Status=approved` 并写入 `RegistryStatus=registered`；Console error 与目标 warning 均为 0。
- `zero_pose_variation_20260801_01` 的 `doll_zero_dialogue_command_ready`、`doll_zero_hurt`、`doll_zero_tired` 已完成 segmentation -> `processed/3` -> guarded replacement `selected`（92 / 91 / 93），并在 `art_import_zero_pose_variation_20260801_03` 完成 `Approved -> Unity import -> Registry`：三项均为 1024x1536 Sprite、透明、Bilinear、无 Mipmap，Registry 各 `match_count=1`、`TryGetEntry=true`，目标 Console error/warning 为 0。Manifest 保持 `Status=approved` 并写入 `RegistryStatus=registered`；本轮不包含 Prefab/UGUI 运行时绑定、ArtAcceptance 或 `runtime_validated`。证据：`UnityClient/Logs/P3ArtImport/art_import_zero_pose_variation_20260801_03/{summary,approved-plan,approved-sync,unity-import,registry-result,console-delta,finalize-stage}.json`。
- `Run-CharacterPortraitSet` Resume 安全回归已补齐：旧 checkpoint 不能绕过当前 Prompt/reference/raw/processed freshness，最新失败数字轮次不能回退旧通过轮次，visual review 必须带候选 SHA，已授权技术 override 会同步计算 `AutomaticStatus` / 最终状态。该修复不改变 Approved、Unity、Registry 或运行时状态。
- `_IncomingAI` 已迁移为 Profile 目录，现有 standard asset 工作区和历史目录均保留可追溯路径，Approved / Registry 文件哈希未改变。
- 零号差分已完成正式准入：Manifest 的 `zero_dialogue_portrait_v1` 当前包含 neutral 与 13 个新增成员，新增成员均为 `character_portrait_set`、已生成 Prompt/Spec，并建立独立 Asset Contract、production plan 和 manifest snapshot；其中 `dialogue_command_ready` 已完成 Approved -> Unity -> Registry，剩余成员仍按单项授权推进。
- 零号 P0 复用候选中的 `stand_neutral`、`maintenance_sit` 已完成正式导入、保前景透明处理、processed/1 登记与 selected；`dialogue_command_ready` 已由本轮姿势差分替换并完成 `registered`。前两者评分分别为 89、90，仍未进入 Approved。
- 历史对白/P1 候选中 `talk_small`、`confused`、`tired` 已以 90、89、91 分进入 selected；`thoughtful`、`trust_soft`、`cold` 分别因与 command-ready 过近、手指缺陷、状态辨识度不足停在 processed/1 `decision_required`，已并入定向修复批次。
- 零号剩余 7 个成员已完成前景保护透明处理、不可变数字轮次登记与 selected：`low_san=91`、`hurt=90`、`repair_relief=89`、`depressed=91`、`thoughtful=92`、`trust_soft=90`、`cold=90`。`low_san` 的 OpenAI 初稿曾露出眼形，已通过显式 mask 的 NovelAI inpaint 把白布改为不透明，并以小范围确定性后处理保留微弱非眼形红光；违规原稿只保留在 raw 证据中。
- `zero_dialogue_portrait_v1` 已完成 14 成员套组一致性评审并以 `passed_with_recorded_risks` 通过 selected 口径：对白状态在小尺寸可区分；`stand_neutral` 渲染更淡、维护坐姿族头发/头部占比更大、`cold / repair_relief` 提示较克制，均记录为非阻塞风险，不扩大声明为 Approved 或运行时通过。

## 下一步建议

- `bg_combat_abyss` 与 `ui_icon_warning` 的最新 `processed/5` selected 已通过独立 ArtImportRun 接入到 `registered`；后续不重复生成或重跑本批，除非出现新的 Requirement / PromptRevision / 质量替换需求。若程序实际消费，再单独安排 UGUI 绑定和正式 ArtRun 运行时验收。
- 继续按 `background / icon / standard_asset` 分组扩大 Formal V2 通用批次；每批先 dry-run 和 provider smoke，完成 Agent 评审后用 guarded selection 收敛到 selected。UI Skin 继续使用独立专项 route，并以 `ui_button_primary processed/3` 作为首个九宫格修复验证。
- 正式出图前对实际 provider 做配置检查和最小 smoke；批量只消费已发布 `natural_language_v2` / `danbooru_tags_v2`，provider adapter 不得改写 Prompt，角色差分不得静默 fallback。
- Formal V2 active 主动迭代桥接已落地 `Generate-FormalV2ReplacementPlan.ps1`：从 UI Skin 基准和 `V2-A active` 场景提取已有 VisualID，输出独立 `visual_v2_replace` 计划，不伪装成 `generate_needed`，provider / method 保持 `agent_selected`。
- UI panel、button、list row 等 nine-slice 素材已增加硬门禁：边带覆盖不足或透明碎片过多分别记录为 `nine_slice_edge_coverage_low`、`nine_slice_many_components`；下一步只用 `ui_button_primary` 验证参考图 / 模板修复到新数字轮次。
- 以本次 neutral 与姿势差分试跑为模板，逐个评估其余 10 个零号立绘成员的 Approved 授权与接入顺序；不得把单项授权扩大为整套自动准入。
- 后续程序任务如需实际消费 `doll_zero_dialogue_neutral`，再单独处理 Prefab / UGUI / VisualAssetService 绑定和运行时容器裁切；完成后使用正式 TargetID 走 ArtRun runtime validation，不手写 Manifest 状态。
- T0-01A 继续按角色、场景、风格锚点重做需要替换的 Panel，并在页级 contact sheet 通过后再进行同 VisualID 覆盖。
- 运行时 UI / 视觉问题优先走 `p3-art-validation`；正式资产准入走 `p3-art-asset-production`，纯生成 / 差分走 `generate-image`。

## 问题 / 阻塞

- `art_validation_flow_probe_20260802_01` 已真实启动 `art_focus` 并停在 `AwaitingLiveInspection`。当前 `art_validation_targets.json` 只注册 `workshop_main / dungeon_map / dialogue_overlay / t0_prologue`，且四项 `required_visual_ids` 均为空；`combat_hud / maintenance_panel` 返回 `art_blocked:unknown_target`，现有目标即使完成截图也会在 Finalize 被 `art_blocked:runtime_binding_contract_missing` 拒绝。因此 `bg_combat_abyss / ui_icon_warning` 当前只能保持 `registered`。本轮随后发生 `validation_limited:unity_mcp_session_unavailable`：Unity 进程和 15555 端口仍正常，但 Codex MCP 会话未重新挂载，Run 可从现有 ID 恢复。
- The initial `zero_pose_variation_20260801_01` attempt was blocked by checkerboard background removal, but the follow-up `segmentation_retry_20260801_02` used explicit `rembg` masks and completed a new immutable `processed/3` round. All three members passed visual review and guarded replacement selection; the old checkerboard failure remains historical evidence only.

- `formalv2_catalog_v2_bridge_batch_20260801_01_exec` 当时记录的 `validation_limited:unity_editor_unavailable` 已由 `art_import_formalv2_catalog_v2_bridge_batch_20260802_01` 正式解除；最新 selected 已同步到 Approved 并完成 Unity / Registry 现场验证。当前仅保留运行时消费与 `runtime_validated` 的后续边界。
- 严格生成物聚合校验当前唯一失败为既有 `offline_registry_candidate changed_existing=22`：离线 candidate 缺少 22 个 live Registry 条目，范围是 T0 CG / 工坊资源及 Zero 已登记立绘，不包含本批 `bg_combat_abyss` / `ui_icon_warning`。该离线快照滞后需单独刷新收口，不影响本批 live Unity `registered` 证据。
- `formalv2_standard_stylepilot_20260724_03` 已完成 provider smoke 与真实候选生成；后续批次仍须先 smoke、再按最新数字轮次评审。正式生产仍受 Approved 授权门禁约束，角色差分 Danbooru `unsupported` 时需补映射或改选自然语言后端。
- `Generate-ArtIntegrationCandidates` 当前不会把 Approved 状态下的新 `CandidateBatchID + selected` 明确暴露为 replacement-ready，主动同名迭代的批次状态仍主要依赖 ProductionRun 证据。
- neutral 与三张姿势差分已无 Approved / Unity / Registry 阻塞；其余 10 个角色立绘仍停在 selected / Approved 授权边界。残余质量风险是部分源图状态差异较克制；`OccupiedBBoxTransparency` 的全身负空间误报已降为 warning，真实内部透明洞仍是 hard failure。
- 外部 provider 仍可能超时或限流；GIF 自动检查仍有 `temporal_flicker`、`background_drift`、`identity_check_limited`，受限时不得声明正式资产或视觉验收完成。
- T0-01A 最终运行时漫画播放、ArtAcceptance 和外部美术验收仍未封板。

## 关键证据入口

- `UnityClient/Logs/P3ArtProduction/zero_portrait_differences_20260718_01/{consistency-review,set-contact-sheet,task06-selection-decision}` evidence files
- `美术文档/人设/05_零号立绘素材设计与交付清单.md`
- `UnityClient/Assets/Art/_IncomingAI/character_portraits/doll_zero_dialogue_neutral/production_decision.json`
- `UnityClient/Logs/P3ArtProduction/zero_dialogue_neutral_pilot_20260718_01/selection-decision.json`
- `UnityClient/Logs/P3ArtProduction/zero_dialogue_neutral_pilot_20260718_01/summary.json`
- `UnityClient/Logs/P3ArtImport/art_import_zero_dialogue_neutral_plan_20260718_02/approved-plan.json`
- `UnityClient/Logs/P3ArtImport/art_import_zero_dialogue_neutral_plan_20260718_02/summary.json`
- `docs/superpowers/{specs/2026-07-18-art-approved-unity-registry-automation-design,plans/2026-07-18-art-approved-unity-registry-automation}.md`
- `docs/superpowers/specs/2026-07-18-ai-image-gateway-transparent-streaming-design.md`
- `美术文档/00_美术流水线总览.md`
- `美术文档/_generated/{art_generation_requests.json,art_generation_request_migration.json,FormalV2主动迭代计划.json}`
- `美术文档/10_正式版核心纵切美术路线.md`
- `美术文档/18_CG底图与漫画式播放演出工作流.md`
- `美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md`
- `zero_pose_variation_20260801_01` 当前已完成 segmentation -> processed/3 -> selected，并在 `art_import_zero_pose_variation_20260801_03` 完成 Approved -> Unity import -> Registry；三张姿势差分评分 92 / 91 / 93。证据：`UnityClient/Logs/P3ArtProduction/zero_pose_variation_20260801_01/segmentation_retry_20260801_02/` 与 `UnityClient/Logs/P3ArtImport/art_import_zero_pose_variation_20260801_03/`。
