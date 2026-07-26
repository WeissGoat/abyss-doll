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
  - 美术文档/人设/04_零号AI后端出图提示词对比.md
  - 美术文档/人设/05_零号立绘素材设计与交付清单.md
  - 开发文档/17_Live2DSpine运行时接入评估.md
  - 开发文档/18_全局叙事播放系统开发方案.md
last_verified: 2026-07-26
update_rule: 美术或 UI 视觉流水线任务完成后更新本文件。
---

# 美术 / UI 状态

## 最后更新

2026-07-26

## 当前关注

- `zero_cold_portrait_chain_20260726_01` 已完成首个人物差分链路复测，但没有产生可替换候选：`Run-CharacterPortraitSet.ps1` 的 Windows PowerShell 5.1 中文默认路径回归已通过真实 wrapper 测试修复；套组 dry-run 与 Gemini smoke 通过。Gemini 初始 2 张和定向修复 2 张均返回 `1376x768 RGB JPEG` 并烘焙棋盘格，其中仅首张的身份与 cold 手臂后收语义接近可用；OpenAI Images 单参考与双参考各 1 张虽返回 RGBA PNG，但均移除白布眼罩、暴露眼睛，触发身份硬失败。本轮未发布 `processed/3`、未覆盖既有 90 分 selected，`doll_zero_cold` 继续以 `processed/2 -> selected` 为权威状态；下一步需补 `SourceAssets -> 实际参考图` Resolver 和独立前景/背景处理能力后再跑。证据：`UnityClient/Logs/P3ArtProduction/zero_cold_portrait_chain_20260726_01/summary.json`。

- `formalv2_standard_stylebatch_20260725_01` 已完成首个新风格通用批量全自动闭环：`bg_combat_abyss` 与 `ui_icon_warning` 各有 1 张本批可解码 raw、各有 1 次 HTTP 504，均发布为不可变 `processed/4`，经 Agent 93 / 96 分 guarded selection 后完成同 VisualID Approved 替换。`art_import_formalv2_standard_stylebatch_20260725_01` 的 live Unity importer、Registry 唯一项、路径和 GUID 全部通过，两项 Manifest 均保持 `Status=approved` 并写 `RegistryStatus=registered`；Approved SHA-256 为 `880e6125f860712edecec1a943e6a2a8de79d9b64117b799631b5ceab7745477` / `a76de6477380f27a1a32dfbffdea7b08599e7812733eb2c15e725a5abfb46e19`，原 `.meta`/GUID 保持不变。该结论不包含 UGUI 绑定或 `runtime_validated`。预检同时修复了 `Compile-ArtGenerationRequests.ps1` 在 Windows PowerShell 5.1 下的中文默认路径失败，并增加 wrapper dry-run 回归测试。
- `formalv2_standard_stylepilot_20260724_03` 已完成一次真实标准背景修复批次：`bg_workshop_day` 使用 `openai_images / gpt-image-2` 串行生成 2 张 raw，发布到不可变 `processed/4`；`001.png` 以 95 分通过 Agent 评审并 guarded selection，当前公开状态为 `selected`。Approved、Unity、Registry 均未改变。
- Agent 主导双格式 PromptRevision 改造正在执行：Task 1 已把正式编译入口切到 `PromptAuthoringContext + TechnicalRequest + PreservationContract + RequirementFingerprint`，不再机械生成可执行 Prompt；相同 RequirementFingerprint 会保留既有 Revision。当前仅完成编译核心与 focused tests，运行消费、Catalog strict、Pilot Revision 和全量迁移尚未完成；此前运行态字段不参与 stale、语义字段变化触发 stale 的边界保持不变。
- 美术生产按 `standard_asset`、`character_portrait_set`、`_legacy_runs` 分 Profile 管理；Manifest v3 已为 313 个 Entry 持久化 `RequestID + RequestFingerprint`，Catalog 保存两种 Prompt Variant，标准批量 / 角色套组共用门禁；标准批量已完成真实 provider 试跑并保留完整 raw / processed / review 证据。
- Formal V2 UI Skin 专项 route 已通过首个真实 Pilot：`ui_skin=deterministic_template` 会由批量执行器分流到确定性 NineSlice 适配器；`ui_button_primary` 已发布 `processed/3` 并完成 Agent guarded selection，公开状态停在 `selected`，Approved / Unity / Registry 未变化。
- 首个 `character_portrait_set` 正式接入试跑已到 `registered`：`doll_zero_dialogue_neutral` 已同步到 Approved，Unity importer 与尺寸符合合同，Registry 唯一条目、路径和 GUID 均匹配；本轮明确不含 Prefab / UGUI 绑定和运行时验收。
- 正式 UI / 素材接入仍以 active `screen_layouts.json`、Manifest、程序交接清单和 ArtAcceptance 为准；静态预览、contact sheet 或生成物不能替代运行时证据。
- T0-01A 的 CG / 漫画页和 FormalV2 工坊素材继续按锚点、一致性、Approved、Unity 接入和运行时验收顺序推进。
- GIF 一致性优化已接入逐帧 `ACTION_TEXT` 边车；真实 Illya 8 帧 GIF 已完成生成与重编码，白发校服身份比旧流程稳定，当前因第 4 帧可见跳变和自动 `temporal_flicker` 标记处于 `review_required`。

## 最近完成
- Request Catalog 迁移与角色差分事实补齐已完成：`Version=3`、313/313 `ready`、14 成员 dry-run 无依赖错误；保护字段 / Approved / Registry 哈希不变。加入 PowerShell wrapper UTF-8 默认路径回归后，美术工具全量测试为 `192 passed`，Request strict、语法和文档校验均通过。
- `formalv2_ui_button_primary_skin_pilot_20260719_01` 已打通 UI Skin 专项批次：两张 512x160 transparent raw 候选发布到不可变 `processed/3`，两者 NineSlice 技术门禁均通过、ConnectedComponents=1，四向边带覆盖为 top/bottom 90.23%、left/right 86.25%。Agent 对原尺寸和 220x64 预览评审后选择 `001.png`，评分 92、领先 6 分；selected SHA-256 为 `005bbbaa7ef2139d5d637e18f82dffc9b64b51aa8f3efdbeb044b5035df82ed4`，Approved 保持 `83ce19e0bbf22c628dff748f095ff9b23286b5c37dae3462687c0e85399cf8bd`，Run 为 `selection_complete`。
- 通用批量流已补齐可恢复执行与 guarded selection：`Run-ArtProductionBatch.ps1` 可消费缺图或 Formal V2 主动替换计划，按运行时 `AssetClass -> provider` 分组执行现有生成 / 预处理工具，并以本批可解码 raw 阻断旧报告误判；`Select-ArtCandidate.ps1` 只允许最新通过轮次、Agent 评分至少 88 的候选进入 selected，保留 Approved 主状态。
- `formalv2_standard_pipeline_pilot_20260719_02` 已真实打通两类素材到 selected：`bg_combat_abyss processed/2/001.png` 以 93 分替换工作区旧 selected，`ui_icon_warning processed/2/001.png` 以 89 分首次建立 selected；两者各生成 2 张 raw，OpenAI Images `gpt-image-2` 请求 4/4 成功，Run 最终为 `selection_complete`，Approved / Unity / Registry 均未变化。
- `formalv2_standard_batch_20260719_01` 已完成 4 项标准素材的 smoke、串行 raw 生成、`processed/2` 发布与 Agent 筛选：`bg_workshop_day` 以 92 分进入 selected，processed / selected SHA-256 均为 `7e5ce09cd60375c10d5489bf27deca4fa6e2bfe2a3dd555cfa91b0ad10cc8b6f`；`ui_button_primary`、`ui_panel_main`、`ui_list_row_normal` 因 `subject_outside_safe_canvas` 和碎片化风险保持技术失败，未覆盖原 selected 或 Approved。
- `doll_zero_dialogue_neutral` 已完成 `selected -> approved -> unity_imported -> registered`：Approved / selected SHA-256 均为 `fd1d68528e685fd9075b294bb9b0ca903e4eddcddd2deab2db323def29180284`，Unity GUID 为 `7d7b3a5d2f28634469b19bb5aa52664b`，Manifest 保持 `Status=approved` 并写入 `RegistryStatus=registered`；Console error 与目标 warning 均为 0。
- `_IncomingAI` 已迁移为 Profile 目录，现有 standard asset 工作区和历史目录均保留可追溯路径，Approved / Registry 文件哈希未改变。
- 零号差分已完成正式准入：Manifest 的 `zero_dialogue_portrait_v1` 当前包含 neutral 与 13 个新增成员，新增成员均为 `character_portrait_set`、已生成 Prompt/Spec，并建立独立 Asset Contract、production plan 和 manifest snapshot；当前仍未进入 Approved。
- 零号 P0 复用候选中的 `stand_neutral`、`dialogue_command_ready`、`maintenance_sit` 已完成正式导入、保前景透明处理、processed/1 登记与 selected；三者评分分别为 89、91、90，仍未进入 Approved。
- 历史对白/P1 候选中 `talk_small`、`confused`、`tired` 已以 90、89、91 分进入 selected；`thoughtful`、`trust_soft`、`cold` 分别因与 command-ready 过近、手指缺陷、状态辨识度不足停在 processed/1 `decision_required`，已并入定向修复批次。
- 零号剩余差分生成批次已取得 `7/7` 可解码新 raw：`hurt / repair_relief` 与三张定向修复由 Gemini 完成，`low_san / depressed` 经 OpenAI `gpt-image-2` fallback 完成；每次请求均为 `count=1`，实际返回尺寸已记录为 Gemini `1376x768`、OpenAI `1024x1024`，尚未进行透明背景处理、processed 登记或 selected 决策。
- 零号剩余 7 个成员已完成前景保护透明处理、不可变数字轮次登记与 selected：`low_san=91`、`hurt=90`、`repair_relief=89`、`depressed=91`、`thoughtful=92`、`trust_soft=90`、`cold=90`。`low_san` 的 OpenAI 初稿曾露出眼形，已通过显式 mask 的 NovelAI inpaint 把白布改为不透明，并以小范围确定性后处理保留微弱非眼形红光；违规原稿只保留在 raw 证据中。
- `zero_dialogue_portrait_v1` 已完成 14 成员套组一致性评审并以 `passed_with_recorded_risks` 通过 selected 口径：对白状态在小尺寸可区分；`stand_neutral` 渲染更淡、维护坐姿族头发/头部占比更大、`cold / repair_relief` 提示较克制，均记录为非阻塞风险，不扩大声明为 Approved 或运行时通过。
- GIF 小循环工具测试为 `50 passed, 2 subtests passed`，`ACTION_TEXT` 聚焦测试为 `24 passed, 5 subtests passed`；真实 8 帧 Run 已输出 `320x180` GIF、contact sheet 和 review 报告，技术硬门禁无失败。

## 下一步建议

- 继续按 `background / icon / standard_asset` 分组扩大 Formal V2 通用批次；每批先 dry-run 和 provider smoke，完成 Agent 评审后用 guarded selection 收敛到 selected。UI Skin 继续使用独立专项 route，并以 `ui_button_primary processed/3` 作为首个九宫格修复验证。
- 正式出图前对实际 provider 做配置检查和最小 smoke；批量只消费 `natural_language_v1` / `danbooru_tags_v1`，角色差分不得静默 fallback。
- Formal V2 active 主动迭代桥接已落地 `Generate-FormalV2ReplacementPlan.ps1`：从 UI Skin 基准和 `V2-A active` 场景提取已有 VisualID，输出独立 `visual_v2_replace` 计划，不伪装成 `generate_needed`，provider / method 保持 `agent_selected`。
- UI panel、button、list row 等 nine-slice 素材已增加硬门禁：边带覆盖不足或透明碎片过多分别记录为 `nine_slice_edge_coverage_low`、`nine_slice_many_components`；下一步只用 `ui_button_primary` 验证参考图 / 模板修复到新数字轮次。
- 以本次 neutral 试跑为模板，逐个评估其余 13 个零号立绘成员的 Approved 授权与接入顺序；不得把 neutral 的授权扩大为整套自动准入。
- 后续程序任务如需实际消费 `doll_zero_dialogue_neutral`，再单独处理 Prefab / UGUI / VisualAssetService 绑定和运行时容器裁切；完成后使用正式 TargetID 走 ArtRun runtime validation，不手写 Manifest 状态。
- T0-01A 继续按角色、场景、风格锚点重做需要替换的 Panel，并在页级 contact sheet 通过后再进行同 VisualID 覆盖。
- 运行时 UI / 视觉问题优先走 `p3-art-validation`；正式资产准入走 `p3-art-asset-production`，纯生成 / 差分走 `generate-image`。

## 问题 / 阻塞

- Formal V2 静态预验收在当前 `program_integrate=0` 时写出 `reviewed=0`，总生成物验证仍把 0 视为失败；严格聚合校验另有既有 `offline_registry_candidate changed_existing=19`，需区分空队列与审查缺失并单独处理 Registry candidate。
- `formalv2_standard_stylepilot_20260724_03` 已完成 provider smoke 与真实候选生成；后续批次仍须先 smoke、再按最新数字轮次评审。正式生产仍受 Approved 授权门禁约束，角色差分 Danbooru `unsupported` 时需补映射或改选自然语言后端。
- `Generate-ArtIntegrationCandidates` 当前不会把 Approved 状态下的新 `CandidateBatchID + selected` 明确暴露为 replacement-ready，主动同名迭代的批次状态仍主要依赖 ProductionRun 证据。
- neutral 已无 Approved / Unity / Registry 阻塞；其余 13 个角色立绘仍停在 selected / Approved 授权边界。残余质量风险是部分源图状态差异较克制，此外 `OccupiedBBoxTransparency` 会把全身立绘的正常负空间误报为透明洞。
- 外部 provider 仍可能超时或限流；GIF 自动检查仍有 `temporal_flicker`、`background_drift`、`identity_check_limited`，受限时不得声明正式资产或视觉验收完成。
- T0-01A 最终运行时漫画播放、ArtAcceptance 和外部美术验收仍未封板。

## 关键证据入口

- `UnityClient/Logs/P3ArtProduction/formalv2_ui_button_primary_skin_pilot_20260719_01/{summary,visual-review}.json`
- `UnityClient/Logs/P3ArtProduction/formalv2_standard_stylepilot_20260724_03/{summary,visual-review,selection-decision}.json`
- `UnityClient/Logs/P3ArtProduction/formalv2_standard_stylebatch_20260725_01/{summary,visual-review,selection-decision}.json`
- `UnityClient/Logs/P3ArtImport/art_import_formalv2_standard_stylebatch_20260725_01/{approved-plan,approved-sync,unity-import,registry-result,console-delta,summary}.json`
- `UnityClient/Logs/P3ArtProduction/formalv2_standard_pipeline_pilot_20260719_02/summary.json`
- `UnityClient/Logs/P3ArtProduction/zero_portrait_differences_20260718_01/consistency-review.json`
- `UnityClient/Logs/P3ArtProduction/zero_portrait_differences_20260718_01/set-contact-sheet.png`
- `UnityClient/Logs/P3ArtProduction/zero_portrait_differences_20260718_01/task06-selection-decision.json`
- `美术文档/人设/05_零号立绘素材设计与交付清单.md`
- `UnityClient/Assets/Art/_IncomingAI/character_portraits/doll_zero_dialogue_neutral/production_decision.json`
- `UnityClient/Logs/P3ArtProduction/zero_dialogue_neutral_pilot_20260718_01/selection-decision.json`
- `UnityClient/Logs/P3ArtProduction/zero_dialogue_neutral_pilot_20260718_01/summary.json`
- `UnityClient/Logs/P3ArtImport/art_import_zero_dialogue_neutral_plan_20260718_02/approved-plan.json`
- `UnityClient/Logs/P3ArtImport/art_import_zero_dialogue_neutral_plan_20260718_02/summary.json`
- `docs/superpowers/specs/2026-07-18-art-approved-unity-registry-automation-design.md`
- `docs/superpowers/plans/2026-07-18-art-approved-unity-registry-automation.md`
- `docs/superpowers/specs/2026-07-18-ai-image-gateway-transparent-streaming-design.md`
- `美术文档/00_美术流水线总览.md`
- `美术文档/_generated/{art_generation_requests.json,art_generation_request_migration.json,FormalV2主动迭代计划.json}`
- `美术文档/10_正式版核心纵切美术路线.md`
- `美术文档/18_CG底图与漫画式播放演出工作流.md`
- `美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md`
