---
id: tools_art_readme
title: 美术工具
type: tool
role: 美术
domain: art_tooling
status: active
source_of_truth: false
related:
  - 美术文档/03_AI生成与筛选规范.md
  - 美术文档/05_AI图片网关接入方案.md
  - 美术文档/20_GIF小循环人物替换工作流.md
  - 美术文档/02_资源规格与接入规范.md
  - 美术文档/01_Manifest规范.md
  - 美术文档/00_美术流水线总览.md
  - 美术文档/README.md
  - 美术文档/04_美术风格基准.md
  - 美术文档/人设/README.md
  - 美术文档/人设/01_人设参考获取规则.md
  - 美术文档/人设/04_零号AI后端出图提示词对比.md
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/design_boards/README.md
last_verified: 2026-07-26
update_rule: 修改对应工具入口、参数或执行流程时同步本文件。
---

# 美术工具

> **定位：** 存放 Project P3 美术流水线脚本。脚本优先服务于“配置表扫描、Manifest 增量更新、批量生成、预处理和验收记录”。

## Migrate-ArtProcessedRounds.ps1

一次性把旧的平坦 `processed/*.png` 迁移到 `processed/1/`，保留原文件名并同时迁移相邻 `.meta`。默认命令只生成迁移计划，确认没有冲突后才允许执行：

```powershell
.\tools\美术工具\Migrate-ArtProcessedRounds.ps1
.\tools\美术工具\Migrate-ArtProcessedRounds.ps1 -Execute
```

如果已有 `processed/1/` 与平坦文件并存，工具会阻断迁移。旧候选默认登记为 `legacy_unverified`；已有生产决策硬失败的候选登记为 `failed`。只有 SelectedPath 精确指向被迁移文件时才改写路径；Approved、Registry 和运行时资源不会被触碰。

## Prepare-ArtBackgroundCandidate.ps1

为需要显式背景处理的候选生成 run-scoped staging 输出。工具支持保留已有 Alpha、连通边界背景移除和显式 mask 三种当前能力，但能力选择属于本轮 Agent 决策，不写入 Manifest 的稳定需求合同。

```powershell
.\tools\美术工具\Prepare-ArtBackgroundCandidate.ps1 `
  -InputPath "UnityClient/Assets/Art/_IncomingAI/character_portraits/doll_zero_cold/raw/20260726_002.png" `
  -StagingDirectory "UnityClient/Logs/P3ArtProduction/<ProductionRunID>/background-staging" `
  -Method connected_border `
  -ExpectedInputSHA256 <sha256> `
  -DryRun
```

`alpha_passthrough` 只接受已经具有有效透明度的图片；`connected_border` 只适合与画布边界连通且可稳定区分的简单背景；`explicit_mask` 必须提供同尺寸、非全黑且非全白的 mask。输出包含候选 PNG 与 `background-processing.json`，并拒绝把 staging 指向任何 `processed/`、`selected/` 或 Approved 路径。通过技术和视觉检查后，仍须使用 `Register-ArtProcessingRound.ps1` 发布下一不可变数字轮次。

## Register-ArtProcessingRound.ps1

将 Agent 产生的候选处理结果登记为下一个不可变的 `processed/<正整数>/` 轮次。staging 目录必须包含直接子级候选图片、`decision.json`、`process_report.json` 和 `technical_review.json`；角色立绘的 `passed` 轮次还必须包含 `visual_review.json`。`technical_review.json` 使用 `technical_review_v2`，Registrar 会根据当前 Manifest Spec 和真实候选重新计算结果并核对 `ReviewFingerprint`，不能靠手写 `Status=passed` 绕过技术门禁。

登记不会修改 Manifest 的主状态、`selected/`、Approved、Registry 或运行时绑定。正式登记前先执行 dry-run：

```powershell
.\tools\美术工具\Register-ArtProcessingRound.ps1 `
  -VisualID doll_zero_dialogue_neutral `
  -StagingDirectory F:\tmp\doll_round `
  -DryRun
```

确认后去掉 `-DryRun` 登记。staging 中的候选必须通过 SHA-256、尺寸、格式和路径边界校验；`SelectedPath`、`ApprovedPath` 等正式资产状态字段会被拒绝。

技术自动结论保存在 `technical_review.json`，最终有效状态保存在 `decision.json` 的 `Status`，并附带 `AutomaticStatus` 与 `AppliedOverrides`。例外必须放在独立 `technical_override.json`，同时通过命令显式授权对应 RuleID：

```powershell
.\tools\美术工具\Register-ArtProcessingRound.ps1 `
  -VisualID doll_zero_dialogue_neutral `
  -StagingDirectory F:\tmp\doll_round `
  -AllowTechnicalOverride subject_outside_safe_canvas
```

当前只允许把白名单中的启发式规则降级；解码、SHA、尺寸、格式、Alpha 合同和 nine-slice 结构错误不可 override。角色 `OccupiedBBoxTransparency` 只产生 `high_occupied_bbox_transparency` warning，真实内部透明洞使用 `unexpected_transparent_holes` hard failure。

## Invoke-GifCharacterReplace.ps1

对 `8-30` 帧小循环 GIF 做可恢复的人物替换工作流。输入为必填文字描述和 `0-N` 张可选参考图；用户文字优先于参考图，参考图只用于生成身份预审帧。默认使用 Gemini 整帧图生图，每帧独立请求；用户批准外观锚点和动作预审前不会运行完整批次。

新建运行并先做 dry-run：

```powershell
.\tools\美术工具\Invoke-GifCharacterReplace.ps1 `
  -InputGif "F:\input\source.gif" `
  -Prompt "替换为银发机械师，保持原动作和背景" `
  -Reference "F:\refs\front.png","F:\refs\side.png" `
  -OutputRoot "F:\output" `
  -DryRun
```

恢复、批准和编码：

```powershell
.\tools\美术工具\Invoke-GifCharacterReplace.ps1 -RunID "gif_replace_..." -OutputRoot "F:\output" -ApproveAppearanceAnchor
.\tools\美术工具\Invoke-GifCharacterReplace.ps1 -RunID "gif_replace_..." -OutputRoot "F:\output" -ApprovePreview -Resume
.\tools\美术工具\Invoke-GifCharacterReplace.ps1 -RunID "gif_replace_..." -OutputRoot "F:\output" -RerunFrame 4,9 -RepairMode strict
.\tools\美术工具\Invoke-GifCharacterReplace.ps1 -RunID "gif_replace_..." -OutputRoot "F:\output" -EncodeOnly
```

每个运行目录保存输入、完整 RGBA 原帧、身份契约、预审、raw 输出、请求元数据、风险报告、contact sheet 和结果 GIF。FFmpeg 可用时 `-Encoder auto` 优先使用 FFmpeg；当前工作区无 FFmpeg 时自动降级 Pillow，并保留原时长、帧数、画布和 loop。此工具只产生探索工作区证据，不修改 Manifest、Approved、Registry、Unity 或网关子模块；视觉风险是人工复核提示，不是正式美术验收。

Agent 执行纯图片生成、图生图、差分或 inpaint 前，先读取 `.codex/skills/p3-generate-image/SKILL.md`（Skill 名 `generate-image`）；正式资产从需求准入到 Approved、Unity 和验收由 `.codex/skills/p3-art-asset-production/SKILL.md` 编排。本文件只负责具体脚本参数。

## Generate-DanbooruCharacterReference.ps1

拉取 Danbooru 公开 tag / post 元数据，生成零号人设参考研究报告，不下载图片，不写入 Approved、Manifest、Registry 或 DollPuppet 包。

使用方式：

```powershell
.\tools\美术工具\Generate-DanbooruCharacterReference.ps1 -Fast
```

输出：

* `美术文档/_generated/danbooru_character_reference/zero_doll_reference_report.md`
* `美术文档/_generated/danbooru_character_reference/zero_doll_reference_raw.json`

执行规则以 [美术文档/人设/01_人设参考获取规则.md](../../美术文档/人设/01_人设参考获取规则.md) 为准：默认搜索组必须包含 `1girl rating:g`，候选角色榜需要二次过滤男性 / 非目标对象并归并同角色变体，报告必须包含中文角色名、中文作品名和中文 tag 语义。

## Generate-ZeroPrototypeBackendBatch.ps1

使用 `tools/ai-image-gateway/config.local.yaml` 中已配置的 `openai_images`、`gemini_chat_image` 和 `novelai` 三个后端，为零号初版人设各生成 3 张候选图。默认输出到系统临时目录 `P3CharacterDesign`，不进入正式 `_IncomingAI` Profile、Approved / Manifest / Registry；需要保留到任务指定位置时显式传 `--output-dir`。

使用方式：

```powershell
.\tools\美术工具\Generate-ZeroPrototypeBackendBatch.ps1
```

可只跑某个后端：

```powershell
.\tools\美术工具\Generate-ZeroPrototypeBackendBatch.ps1 -Only chatgpt -Count 1
.\tools\美术工具\Generate-ZeroPrototypeBackendBatch.ps1 -Only gemini_nanobanana -Count 1
.\tools\美术工具\Generate-ZeroPrototypeBackendBatch.ps1 -Only novelai -Count 1
```

提示词与批次口径见 [美术文档/人设/04_零号AI后端出图提示词对比.md](../../美术文档/人设/04_零号AI后端出图提示词对比.md)。

需要把图片直接放入人设目录时，使用 `-OutputDir`：

```powershell
.\tools\美术工具\Generate-ZeroPrototypeBackendBatch.ps1 -OutputDir "F:\design\game\project\p3\美术文档\人设\AI出图\zero_v1_rerun_20260707_01"
```

## Test-AIImageBackends.ps1

使用 `tools/ai-image-gateway/config.local.yaml` 对 `chatgpt` / `openai_images`、`gemini` / `gemini_chat_image`、`novelai` 三个配置后端做最小真实出图 smoke。默认输出到系统临时目录 `P3BackendSmoke`，不进入 `_IncomingAI`、Approved、Manifest 或 Registry。

使用方式：

```powershell
.\tools\美术工具\Test-AIImageBackends.ps1
```

只检查配置解析与凭证字段是否存在，不实际请求 API：

```powershell
.\tools\美术工具\Test-AIImageBackends.ps1 -CheckConfigOnly
```

可只测某个后端或指定证据目录：

```powershell
.\tools\美术工具\Test-AIImageBackends.ps1 -Backend chatgpt -Attempts 1
.\tools\美术工具\Test-AIImageBackends.ps1 -Backend gemini,novelai -Attempts 2 -OutputDir "C:\Users\WhiteSheep\AppData\Local\Temp\P3BackendSmoke\manual_retest"
```

输出：

* `summary.json`：每个后端的尝试次数、成功 / 失败、模型、字节数、样图路径和错误信息。
* `config_summary.json`：脱敏后的配置摘要，只记录 provider、model、endpoint 和凭证字段是否存在。
* `provider.log`：网关和 provider 的详细日志，用于确认限流、HTTP 错误或 transport 错误。
* `*_attempt*.png|jpg|webp`：每个成功后端的一张最小 smoke 样图。

NovelAI 限流时可拉长外层重试间隔：

```powershell
.\tools\美术工具\Test-AIImageBackends.ps1 -Backend novelai -Attempts 3 -RetryDelaySeconds 30
```

## Generate-ZeroPrototypeTurnaroundBatch.ps1

使用 `tools/ai-image-gateway/config.local.yaml` 中已配置的 `openai_images`、`gemini_chat_image` 和 `novelai` 三个后端，为零号初版人设生成三视图候选图。每张候选图本身应包含正面 / 侧面 / 背面三视图，输出只进入 `美术文档/人设/AI出图/`，不进入 Approved / Manifest / Registry。

使用方式：

```powershell
.\tools\美术工具\Generate-ZeroPrototypeTurnaroundBatch.ps1 -OutputDir "F:\design\game\project\p3\美术文档\人设\AI出图\zero_v1_turnaround_20260708_01"
```

可只跑某个后端：

```powershell
.\tools\美术工具\Generate-ZeroPrototypeTurnaroundBatch.ps1 -Only chatgpt -Count 1
.\tools\美术工具\Generate-ZeroPrototypeTurnaroundBatch.ps1 -Only gemini_nanobanana -Count 1
.\tools\美术工具\Generate-ZeroPrototypeTurnaroundBatch.ps1 -Only novelai -Count 1
```

只刷新已有图片的总汇总和 contact sheet：

```powershell
.\tools\美术工具\Generate-ZeroPrototypeTurnaroundBatch.ps1 -RefreshOnly -OutputDir "F:\design\game\project\p3\美术文档\人设\AI出图\zero_v1_turnaround_20260708_01"
```

三视图提示词与批次结论见 [美术文档/人设/04_零号AI后端出图提示词对比.md](../../美术文档/人设/04_零号AI后端出图提示词对比.md)。

## Generate-ZeroPrototypePoseActionBatch.ps1

为零号生成单张单视角 / 单动作人设图，不生成三视图拼图。默认使用 Gemini 后端逐张生成 8 个动作：正面待机、3/4 斜侧、侧身行走、背面回头、维护坐姿、启动准备、低 SAN 红眼透布、受损跪撑。输出只进入 `美术文档/人设/AI出图/`，不进入 Approved / Manifest / Registry。

当前三后端汇总固定按 ChatGPT / Gemini / NovelAI 三列刷新，即使只补跑一个后端也不会丢失其他后端的已有统计。ChatGPT 动作草稿使用 `832x1216`、`quality=medium` 以降低长图中转超时；Gemini / NovelAI 使用 `1024x1536`。NovelAI 常态动作禁止红光透布，只有启动、低 SAN 和受损动作允许红光。

使用方式：

```powershell
.\tools\美术工具\Generate-ZeroPrototypePoseActionBatch.ps1 -OutputDir "F:\design\game\project\p3\美术文档\人设\AI出图\zero_v1_pose_actions_20260710_01"
```

可指定后端或动作：

```powershell
.\tools\美术工具\Generate-ZeroPrototypePoseActionBatch.ps1 -Backend novelai -Pose pose_06_activation_ready -OutputDir "F:\design\game\project\p3\美术文档\人设\AI出图\zero_v1_pose_actions_20260710_01"
.\tools\美术工具\Generate-ZeroPrototypePoseActionBatch.ps1 -Backend gemini_nanobanana,novelai -Count 1
```

只刷新已有图片的总汇总和 contact sheet：

```powershell
.\tools\美术工具\Generate-ZeroPrototypePoseActionBatch.ps1 -RefreshOnly -OutputDir "F:\design\game\project\p3\美术文档\人设\AI出图\zero_v1_pose_actions_20260710_01"
```

## Update-ArtManifest.ps1

根据最新 `UnityClient/Assets/StreamingAssets/Configs` 扫描当前需要的视觉资产，并增量更新：

* `美术文档/_generated/art_manifest.json`
* `美术文档/_generated/视觉资产Manifest.md`

使用方式：

```powershell
.\tools\config\Sync-Configs.ps1 -Clean
.\tools\美术工具\Update-ArtManifest.ps1
```

实现说明：

* `Update-ArtManifest.ps1` 是 PowerShell 包装器，保持 ASCII 安全，适配 Windows PowerShell。
* 实际逻辑在 `update_art_manifest.py`，负责 UTF-8 中文读写和 Manifest 合并。

默认会保留已有 Manifest 中的状态、提示词、批次路径、筛选路径、Registry 状态和备注；新增配置项会标记为 `todo`，旧配置项会标记为 `deprecated`。

配置表扫不出的需求由 `美术文档/art_requirements_seed.json` 提供，进入 Manifest 后仍统一标记为 `SourceType=preset`。这类需求包括背景、通用 UI 皮肤、背包格子、战斗 HUD、结算面板和程序侧反馈缺口。

注意：第一步只负责资产需求发现与台账更新，不自动创作最终 Prompt。正式流程由 `Compile-ArtGenerationRequests.ps1` 编译 Requirement，再由 Agent 发布 PromptRevision。

## Scan-ArtRequirementCandidates.ps1

扫描最新设计文档、配置表、版本规划和 active UI 文档，生成“可能需要美术资产但还没纳管”的候选清单。它是人工审查前哨，不会自动修改 Manifest 或 `art_requirements_seed.json`。

输出：

* `美术文档/_generated/美术需求候选清单.json`
* `美术文档/_generated/美术需求候选清单.md`

使用 `-Snapshot` 时额外输出：

* `美术文档/_generated/art_requirement_candidate_snapshots/YYYYMMDD_HHMMSS_<SnapshotTag>.json`
* `美术文档/_generated/art_requirement_candidate_snapshots/YYYYMMDD_HHMMSS_<SnapshotTag>.md`

使用方式：

```powershell
.\tools\美术工具\Scan-ArtRequirementCandidates.ps1
.\tools\美术工具\Scan-ArtRequirementCandidates.ps1 -Snapshot -SnapshotTag manual_review
```

主要分类：

* `explicit_visual_id`：文档或配置中直接出现的潜在 VisualID。
* `derived_visual_candidate`：从订单、传闻、势力、底盘、义体等内容 ID 推导出的潜在图标 ID。

主要状态：

* `new_candidate`：Manifest / seed / Approved 都未纳管，需要美术审查。
* `approved_without_manifest`：Approved 有同名 PNG，但 Manifest 未纳管，需要判断是否补 Manifest。
* `seed_only`：seed 已有但 Manifest 未出现，通常需要重新运行 `Update-ArtManifest.ps1`。
* `manifest_managed`：已纳入 Manifest，本报告不要求处理。

确认要纳管的候选，应该人工写入 `美术文档/art_requirements_seed.json`，或等待配置 JSON 增加正式 `VisualID` / `IconVisualID` 字段后再运行 `Update-ArtManifest.ps1`。不要直接从候选报告生成图片。

## Prompt Requirement 与 Revision 工具

正式 Prompt 流程分三步：

```powershell
.\tools\美术工具\Compile-ArtGenerationRequests.ps1 -Overwrite
.\tools\美术工具\Export-ArtPromptAuthoringPackage.ps1 `
  -VisualID bg_combat_abyss,ui_icon_warning `
  -OutputPath UnityClient/Logs/P3ArtProduction/<RunID>/authoring-package.json
.\tools\美术工具\Publish-ArtPromptRevision.ps1 `
  -RevisionPath UnityClient/Logs/P3ArtProduction/<RunID>/prompt-revisions.json
```

`Compile-ArtGenerationRequests.ps1` 只生成 `PromptAuthoringContext`、`TechnicalRequest`、`PreservationContract` 和 `RequirementFingerprint`。当状态为 `prompt_authoring_required` 时，Agent 根据 authoring package 独立创作 `natural_language_v2` 与 `danbooru_tags_v2`；ready Variant 必须完整填写 `ConstraintMapping`。发布成功后 Catalog 和 Manifest pointer 同步为 `prompt_ready + ActivePromptRevisionID`。

`Generate-ArtPrompts.ps1` 仅保留旧 Manifest 的 `PromptCN / PromptEN / NegativePromptEN` 兼容和迁移用途。它生成的 v1 Prompt 不得作为新正式批次输入；Catalog 中只作为 `LegacyPromptVariants` 证据保留。

## Run-ArtGeneration.ps1

读取 Manifest 目标条目和 Request Catalog，解析 exact active PromptRevision，再调用 `tools/ai-image-gateway` 生成候选图。默认正式格式为 `natural_language_v2` 或 `danbooru_tags_v2`；缺少 active Revision、fingerprint / pointer 不匹配或目标 Variant 未 ready 时，在 provider 调用前失败。

输出目录由 Manifest 的 `ProductionProfile` 解析：

```text
standard_asset         -> UnityClient/Assets/Art/_IncomingAI/standard_assets/<VisualID>/
character_portrait_set -> UnityClient/Assets/Art/_IncomingAI/character_portraits/<VisualID>/

每个 VisualID 工作区：
  raw/
  processed/
  selected/
  contact_sheet/
  manifest_snapshot.json
  generation.json
  notes.md
```

缺少 `ProductionProfile` 的旧 Manifest entry 由 Resolver 归一为 `standard_asset`。`BatchID` 只写入 Manifest 和 `generation.json`，不作为目录层级；`_legacy_runs/` 只保存非 VisualID 历史工作，永不参与生产扫描。

使用方式：

```powershell
.\tools\美术工具\Run-ArtGeneration.ps1 -Provider mock -Limit 1 -Variants 2
```

真实后端建议使用 `tools/ai-image-gateway/config.local.yaml`。脚本会把 `-Variants` 拆成多次 `count=1` 请求，并默认每张图间隔 1 秒。`auto` 为 OpenAI/Gemini 选择 `natural_language_v2`，为 NovelAI 选择 `danbooru_tags_v2`。adapter 只能序列化已发布内容，不能追加质量词、重写自然语言或重复保持 / 改变要求。

角色差分等任务可重复传入 `-ReferenceImage`。每个引用必须是当前解析计划中的真实图片，生成前会重新校验文件 SHA-256；存在引用时使用网关 `image_to_image`，不存在引用时继续使用 `generate`。引用顺序、路径、角色、哈希、尺寸和模式会写入 `ProviderRequest.ReferenceImages` 与 `generation.json`，因此后续恢复不需要 Agent 重新猜测参考关系。

凭证优先级：先读 `NAI_ACCESS_TOKEN`；如果当前机器没有设置该环境变量，网关会尝试从 `F:\my_project\new\tags_machine\novelai\client.py` 的 `NAIClient.get_access_token()` 解析 token。不要把真实 token 写入命令、文档或提交记录；需要换路径时设置 `NAI_CLIENT_PY`。

```powershell
Copy-Item .\tools\美术工具\ai_image_gateway.example.yaml .\tools\ai-image-gateway\config.local.yaml
.\tools\美术工具\Run-ArtGeneration.ps1 -Config .\tools\ai-image-gateway\config.local.yaml -Provider openai_images -Domain item -Limit 5 -Variants 4 -DelaySeconds 2
```

常用参数：

* `-DryRun`：只打印计划，不生成图片、不改 Manifest。
* `-RequestCatalog`、`-RequestID`、`-PromptRevisionID`：选择持久化 Requirement 与不可变 Revision；正常情况下使用 Manifest active pointer。
* `-PromptFormat`：`auto`、`natural_language_v2` 或 `danbooru_tags_v2`。
* `-ReferenceImage` / `-ReferenceImageSHA256` / `-ReferenceImageRole`：按相同顺序传入真实参考图片、计划时锁定的哈希和关系角色；适用于角色身份、姿势或其他明确视觉来源。
* `-AllowLegacyPrompt`：仅用于非正式恢复；输出证据固定标记 `legacy_unverified`，批量与角色套组执行器不会传入。
* `-Domain`、`-VisualID`、`-Priority`：过滤资产。
* `-Limit`：限制本次处理数量。
* `-Seed`：固定基础 seed，便于复现。
* `-DelaySeconds`：每张图之间的等待时间，当前默认 1 秒。
* `-Extra key=value`：透传 provider 参数。
* `-Overwrite`：允许覆盖同名 raw 输出。
* `-PreserveStatus`：用于已接入素材的 Visual V2 候选生成；保留原 `Status`，只写入 `CandidateBatchID` 和 `CandidateRawFiles`。
* `-SkipIntegrationCandidates`：只生成图片，不刷新可接入素材清单。默认不要使用。

每个正式 `generation.json` 使用 `EvidenceMode=formal_v2`，保存 `RequirementSnapshot`、`PromptRevisionID`、`PromptRevisionFingerprint`、`PromptRevisionSnapshot`、`PromptFormat` 和精确 `ProviderRequest`，不再顶层复制旧 `PromptEN / NegativePromptEN`。显式 `-AllowLegacyPrompt` 的非正式恢复使用 `EvidenceMode=legacy_unverified`，旧字段只保存在 `LegacyPromptInput`。非 `-DryRun` 生成完成后，脚本会默认刷新 `美术文档/_generated/可接入素材清单.*`，并在 `美术文档/_generated/art_integration_snapshots/` 写入一份 `generation` 快照。刚生成的 raw 素材会在清单中标为 `art_process`，表示还需要预处理和筛选，不能交给程序接入。

## Run-CharacterPortraitSet.ps1

按 `AssetSetID` 和成员顺序执行角色立绘套组。执行器保留逻辑 `SourceAssets`，并通过 Manifest 中同一 `AssetID` 的成员解析真实 `ResolvedReferenceAssets`：优先 Approved，之后使用合法 Manifest `SelectedPath`，再使用工作区唯一 selected 图片；缺失、重复、损坏、越界或 `_legacy_runs` 引用会在 provider 调用前阻断。

```powershell
.\tools\美术工具\Run-CharacterPortraitSet.ps1 `
  -AssetSetID zero_dialogue_portrait_v1 `
  -VisualID doll_zero_cold `
  -Provider gemini_chat_image `
  -Variants 2 `
  -ProductionRunID <ProductionRunID> `
  -DryRun
```

dry-run 必须显示 exact active PromptRevision、Prompt Variant 以及每张真实参考图的路径、角色和 SHA-256。正式执行会把引用按相同顺序传给 `Run-ArtGeneration.ps1`。`character_portrait_set` 只规定工作区、身份一致性和验收方式，不把成员写死为图生图；没有 SourceAssets 的成员仍可由 Agent 根据当前工具选择其他合适能力。

## Import-ArtCandidate.ps1

把项目内已有图片作为“导入候选”放入 Manifest entry 的 Profile 工作区，用于复用已审阅概念图、外部绘制结果或历史候选。工具通过 `ProductionProfile + VisualID` Resolver 定位 `raw/`，写入 `reference_inputs.json` 与方法中立的 `generation.json`，并只把目标 entry 推进到 `Status=generated`。

```powershell
.\tools\美术工具\Import-ArtCandidate.ps1 `
  -VisualID doll_zero_dialogue_neutral `
  -SourcePath "美术文档/人设/AI出图/zero_dialogue_differences_20260712_01/selected/zero_dialogue_neutral.png" `
  -DestinationName r01_001.png `
  -BatchID imported_zero_dialogue_neutral_20260718_01 `
  -SourceReview "美术文档/人设/AI出图/zero_dialogue_differences_20260712_01/selection_review.md" `
  -DryRun
```

安全边界：

* 要求 Manifest 中目标 VisualID 唯一；未知或重复条目直接失败。
* 禁止从 `_IncomingAI/_legacy_runs` 导入。
* 只接受可解码的 PNG / JPG / JPEG / WEBP，复制前后校验 SHA-256。
* 不写 `SelectedPath`、Approved、Registry 或运行时状态；导入后仍必须执行预处理和候选决策。

## Optimize-ArtAssets.ps1

读取 Manifest entry 的 Profile 工作区 `raw/`，按结构化 `Spec` 输出 `processed` 和 `contact_sheet`。

使用方式：

```powershell
.\tools\美术工具\Optimize-ArtAssets.ps1 -BatchID nai_p0_item_20260508_01 -Overwrite
```

Visual V2 候选批次使用 `-CandidateBatchID`，只处理 Manifest 中 `CandidateRawFiles` 对应的新 raw 文件，不会把历史 raw 全部重新预处理：

```powershell
.\tools\美术工具\Optimize-ArtAssets.ps1 -Status approved -CandidateBatchID nai_visual_v2_20260525_01 -Overwrite
```

非 `-DryRun` 预处理完成后，脚本会默认刷新“可接入素材清单”，并写入一份 `processed` 快照。processed 已有候选但还未 selected 时，清单状态为 `art_select`。需要跳过清单刷新时使用 `-SkipIntegrationCandidates`。

## Sync-ApprovedArt.ps1

把 Manifest entry 的 Profile 工作区中当前安全候选同步到 Manifest 的 `OutputPath`。

同步规则：

* 通过统一 Resolver 使用 `ProductionProfile + VisualID` 定位工作区，不扫描 `_IncomingAI` 根目录或 `_legacy_runs/` 猜测资产。
* 候选优先级固定为 Manifest `SelectedPath`、`selected/` 下按文件名升序第一张、最新数字 `processed/<number>/` 中唯一且通过的候选。
* 最新数字轮次为 `failed`、`decision_required`、`legacy_unverified`、决策证据不完整或有多个通过候选时，禁止回退更早轮次或自动晋级 Approved。
* `-AllowProcessedFallback` 只保留 CLI 兼容；安全数字轮次单候选解析已自动执行，不能绕过轮次决策。
* 复制到 `Approved` 目标路径后，更新 `SelectedPath`、`ApprovedPath` 和 `Status=approved`。
* 覆盖已有 PNG 时保留 Unity `.meta` 文件。
* 使用 `-CandidateBatchID` 做 Visual V2 同名替换时，默认启用严格 `.meta` guard：目标 PNG 和目标 `.meta` 必须已经存在；同步前后 `.meta` 字节必须完全一致，否则脚本失败。

使用方式：

```powershell
.\tools\美术工具\Sync-ApprovedArt.ps1 -BatchID nai_p0_item_20260508_01 -Overwrite
```

Visual V2 同名替换使用 `-CandidateBatchID` 和 `-QualityTier formal_ai_v2`。若 `selected` 为空，Resolver 只接受最新数字轮次中唯一且通过、且输入属于本 CandidateBatch 的候选；`-AllowProcessedFallback` 为 deprecated 兼容参数。同步后可用 `-ClearCandidate` 清理候选字段：

```powershell
.\tools\美术工具\Sync-ApprovedArt.ps1 -Status approved -VisualID ui_icon_diary -CandidateBatchID nai_visual_v2_20260525_01 -AllowProcessedFallback -Overwrite -QualityTier formal_ai_v2 -ClearCandidate
```

这条流程的目标是“替换图片内容，不让程序重新接入”。因此必须保持同一个 `VisualID`、同一个 Manifest `OutputPath`、同一个 Unity `.meta` / GUID。`-AllowNewTargetWithCandidate` 只允许在明确创建新资产路径时使用，不能用于已接入素材的正式图替换。

非 `-DryRun` 同步完成后，脚本会默认刷新“可接入素材清单”，并写入一份 `approved_sync` 快照，方便程序侧直接查看当前哪些 Approved 素材已经可以接入。需要只做同步、不刷新清单时使用 `-SkipIntegrationCandidates`。

## Invoke-ArtApprovedUnityRegistration.ps1

把正式静态 Sprite 的后半段编排为 `selected -> approved -> unity_imported -> registered`。该入口不直接调用 MCP，也不复制 `Sync-ApprovedArt.ps1` 的文件逻辑；Agent 在三个脚本阶段之间调用 Unity MCP，并把 live evidence 写入同一 ArtImportRunID。

只生成计划，不修改 Approved：

```powershell
.\tools\美术工具\Invoke-ArtApprovedUnityRegistration.ps1 `
  -Phase Plan `
  -ArtImportRunID art_import_example_01 `
  -VisualID doll_zero_dialogue_neutral `
  -UnityInstance UnityClient@c0741596
```

计划写入 `UnityClient/Logs/P3ArtImport/<ArtImportRunID>/request.json` 与 `approved-plan.json`。它记录 selected hash、OutputPath、现有 `.meta` / GUID、importer 预期、Approved basename 冲突和授权需求。

用户通过门禁后同步 Approved：

```powershell
.\tools\美术工具\Invoke-ArtApprovedUnityRegistration.ps1 `
  -Phase SyncApproved `
  -ArtImportRunID art_import_example_01 `
  -AuthorizeApprovedSync `
  -AllowNewApprovedTarget
```

已有目标覆盖改用 `-AllowExistingTargetOverwrite`。`SyncApproved` 调用现有 `Sync-ApprovedArt.ps1`，随后写 `approved-sync.json`；同 VisualID 替换仍必须保留 `.meta` 字节和 GUID。

Agent 随后通过 Unity MCP 锁定实例、刷新并等待 Editor 空闲、临时启用 `scripting_ext` 读取 importer/Registry、执行两个 P3 Art 菜单、采集 Console delta，并写：

```text
unity-import.json
registry-result.json
console-delta.json
```

证据完成后执行：

```powershell
.\tools\美术工具\Invoke-ArtApprovedUnityRegistration.ps1 `
  -Phase Finalize `
  -ArtImportRunID art_import_example_01
```

`Finalize` 深度核对 run ID、Unity 实例、时间、VisualID 集合、当前 `.meta` GUID、importer、Registry 唯一条目和 Console；通过后保持 Manifest `Status=approved`，写 `RegistryStatus=registered`，并刷新可接入、程序交接和 Registry 缺口清单。它不创建 Prefab / UGUI 绑定，不进入 PlayMode，也不运行 ArtAcceptance。

## Generate-ArtIntegrationCandidates.ps1

扫描 Manifest、active UI 规格、Approved 素材、Unity `.meta` 和 `VisualAssetRegistry`，生成面向程序接入的当前素材队列。

输出：

* `美术文档/_generated/可接入素材清单.json`
* `美术文档/_generated/可接入素材清单.md`

使用 `-Snapshot` 时额外输出：

* `美术文档/_generated/art_integration_snapshots/YYYYMMDD_HHMMSS_<SnapshotTag>.json`
* `美术文档/_generated/art_integration_snapshots/YYYYMMDD_HHMMSS_<SnapshotTag>.md`

使用方式：

```powershell
.\tools\美术工具\Generate-ArtIntegrationCandidates.ps1
.\tools\美术工具\Generate-ArtIntegrationCandidates.ps1 -Snapshot -SnapshotTag manual_review
```

如果为了避开当前工作区里的未提交 Registry 改动，需要用某个临时 Registry 快照生成报告，可以用 `-RegistryPath` 指向实际读取文件，并用 `-RegistryDisplayPath` 写入稳定显示路径，避免把本机临时路径写进清单：

```powershell
.\tools\美术工具\Generate-ArtIntegrationCandidates.ps1 -RegistryPath $tempRegistry -RegistryDisplayPath 'UnityClient/Assets/Resources/VisualAssetRegistry.asset@HEAD' -Snapshot -SnapshotTag repo_state
```

报告按 `VisualID` 聚合。同一素材如果被多个配置或界面引用，只出现一条，并在 `ReferencedBy` 中合并来源。

`可接入素材清单.*` 是 latest，永远覆盖；`art_integration_snapshots/` 是历史快照，用来追溯每次生成、预处理和 Approved 同步后队列变化。

主要状态：

* `program_integrate`：Approved PNG 已存在，程序侧应导入 / 登记 `VisualAssetRegistry`。
* `acceptance_needed`：Registry 已能找到素材，下一步是运行时截图验收或回填 Manifest 状态。
* `art_approve`：当前 Manifest entry 的 Profile 工作区 `selected/` 已有候选，等待同步到 Approved。
* `art_select`：`processed` 已有候选，等待美术筛选。
* `art_process`：`raw` 已有候选，等待预处理和 contact sheet。
* `generate_needed`：Manifest 有需求，但还没有可接入素材。

## Generate-ArtQualityBacklog.ps1

扫描 Manifest 和 Approved PNG，生成面向美术侧的质量替换队列。它不替代“可接入素材清单”：可接入清单给程序看，质量替换清单给美术看。

输出：

* `美术文档/_generated/素材质量替换清单.json`
* `美术文档/_generated/素材质量替换清单.md`

使用 `-Snapshot` 时额外输出：

* `美术文档/_generated/art_quality_snapshots/YYYYMMDD_HHMMSS_<SnapshotTag>.json`
* `美术文档/_generated/art_quality_snapshots/YYYYMMDD_HHMMSS_<SnapshotTag>.md`

使用方式：

```powershell
.\tools\美术工具\Generate-ArtQualityBacklog.ps1
.\tools\美术工具\Generate-ArtQualityBacklog.ps1 -Snapshot -SnapshotTag manual_review
```

主要状态：

* `technical_fix`：当前 Approved 文件有技术风险，例如不透明规格却含透明像素；优先于视觉精修处理。
* `visual_v2_replace`：当前图可用于程序接入和验收，但只是 local_v0 / placeholder，后续用同名 VisualID 替换正式版。
* `spec_review`：素材尺寸与 Manifest SourceSpec 不一致，需要确认是素材错误还是规格要调整。

### fix_opaque_art_alpha.py

处理 `Generate-ArtQualityBacklog.ps1` 输出中的 `technical_fix` 队列。它只针对 `AlphaRequired=false` 的 Approved PNG，把 alpha 通道统一设为 255；不改 RGB 内容、尺寸、Approved 路径或 Unity `.meta`。

使用方式：

```powershell
python .\tools\美术工具\fix_opaque_art_alpha.py --dry-run
python .\tools\美术工具\fix_opaque_art_alpha.py
.\tools\美术工具\Generate-ArtQualityBacklog.ps1 -Snapshot -SnapshotTag opaque_alpha_fixed
```

可用 `--visual-id <VisualID>` 限定单个或少量素材；处理后应重新刷新质量清单，确认 `technical_fix=0`。

## Normalize-ArtQualityTier.ps1

把早期本地生成但没有显式 `QualityTier` 的历史 Approved 素材规范化为 `QualityTier=local_v0`。该脚本只处理带有本地生成证据的 Manifest 条目，不会把 `formal_ai_v2`、`final` 或 `production` 降级。

输出：

* `美术文档/_generated/local_v0_quality_normalization.json`
* `美术文档/_generated/local_v0_quality_normalization.md`

使用 `-Snapshot` 时额外输出到：

* `美术文档/_generated/art_quality_snapshots/YYYYMMDD_HHMMSS_<SnapshotTag>.json`
* `美术文档/_generated/art_quality_snapshots/YYYYMMDD_HHMMSS_<SnapshotTag>.md`

使用方式：

```powershell
.\tools\美术工具\Normalize-ArtQualityTier.ps1 -DryRun -Snapshot -SnapshotTag dry_run
.\tools\美术工具\Normalize-ArtQualityTier.ps1 -Snapshot -SnapshotTag local_v0_quality_normalized
.\tools\美术工具\Generate-ArtQualityBacklog.ps1 -Snapshot -SnapshotTag local_v0_quality_normalized
.\tools\美术工具\Generate-VisualV2Plan.ps1 -Snapshot -SnapshotTag local_v0_quality_normalized -BatchID nai_visual_v2_20260526_01
```

判定规则：

* `QualityTier=local_v0/placeholder/temporary/fallback` 保持不变。
* `BatchID`、`Notes`、`SelectedPath` 或 `ApprovedPath` 中含 `local_v0`、`generated locally` 或 `approved directly in Approved`，且当前没有正式质量层级时，补写 `QualityTier=local_v0`。
* `QualityTier=formal_ai_v2/final/production` 永不降级。

Visual V2 替换不得改变 `VisualID`、Approved 目标路径、DisplaySpec、Unity `.meta` / GUID 或程序绑定。`Sync-ApprovedArt.ps1 -CandidateBatchID` 默认会校验这一点；如果目标 PNG 或 `.meta` 不存在，脚本会拒绝把候选图同步为 Visual V2 替换。

标准 Visual V2 替换流程：

```powershell
.\tools\美术工具\Run-ArtGeneration.ps1 -Status approved -VisualID <VisualID> -PreserveStatus -BatchID nai_visual_v2_20260525_01 -Variants 4 -DelaySeconds 1
.\tools\美术工具\Optimize-ArtAssets.ps1 -Status approved -CandidateBatchID nai_visual_v2_20260525_01 -Overwrite
.\tools\美术工具\Sync-ApprovedArt.ps1 -Status approved -VisualID <VisualID> -CandidateBatchID nai_visual_v2_20260525_01 -AllowProcessedFallback -Overwrite -QualityTier formal_ai_v2 -ClearCandidate
.\tools\美术工具\Generate-ArtIntegrationCandidates.ps1 -Snapshot -SnapshotTag visual_v2_20260525_01
.\tools\美术工具\Generate-ArtQualityBacklog.ps1 -Snapshot -SnapshotTag visual_v2_20260525_01
```

## Generate-ArtBatchPlan.ps1

读取 `可接入素材清单.json` 和 Manifest，把 `generate_needed` 队列转成可执行的缺图跑图计划。它不生成图片，只固定本批应该跑哪些 VisualID、BatchID、输出目录、每项生成 / 预处理 / 同步命令和最近一次 NovelAI 探测结果。

输出：

* `美术文档/_generated/缺图生成计划.json`
* `美术文档/_generated/缺图生成计划.md`

使用 `-Snapshot` 时额外输出：

* `美术文档/_generated/art_generation_plan_snapshots/YYYYMMDD_HHMMSS_<SnapshotTag>.json`
* `美术文档/_generated/art_generation_plan_snapshots/YYYYMMDD_HHMMSS_<SnapshotTag>.md`

使用方式：

```powershell
.\tools\美术工具\Generate-ArtBatchPlan.ps1 -Snapshot -SnapshotTag nai_missing_assets_20260525_01 -BatchID nai_missing_assets_20260525_01
```

当前用途：

* P1 战斗可读性新增图标 / 反馈 / overlay。
* 最新配置推导出的新地图节点图标。
* 后续任何 Manifest 已有 Prompt / Spec 但尚未形成 Approved PNG 的素材。

当 NovelAI Anlas 不足时，也应刷新本计划并记录 `-LastProbeNote`，明确当前是外部额度不足，而不是缺少提示词、规格或流水线：

```powershell
.\tools\美术工具\Generate-ArtBatchPlan.ps1 -Snapshot -SnapshotTag nai_missing_assets_20260525_01_anlas_blocked -BatchID nai_missing_assets_20260525_01 -LastProbeBatchID nai_visual_v2_probe_20260525_01 -LastProbeNote "NovelAI HTTP 402: Not enough Anlas."
```

## Generate-LocalV0Art.ps1

读取 `缺图生成计划.json` 和 Manifest，为已有 Prompt / Spec 但暂时无法跑 NovelAI 的缺图项生成确定性的 local_v0 Approved PNG。它的用途是解锁 VisualID、Registry 和运行时 UI 验收，不替代正式 AI 出图。

输出：

* Manifest 中对应条目推进到 `Status=approved`
* `QualityTier=local_v0`
* `ApprovedPath` 对应 PNG
* Unity `.meta`

使用方式：

```powershell
.\tools\美术工具\Generate-LocalV0Art.ps1 -BatchID local_v0_missing_assets_20260525_01 -Overwrite
.\tools\美术工具\Generate-ArtIntegrationCandidates.ps1 -Snapshot -SnapshotTag local_v0_missing_assets_20260525_01
.\tools\美术工具\Generate-ArtQualityBacklog.ps1 -Snapshot -SnapshotTag local_v0_missing_assets_20260525_01
.\tools\美术工具\Generate-VisualV2Plan.ps1 -Snapshot -SnapshotTag local_v0_missing_assets_20260525_01 -BatchID nai_visual_v2_20260525_02
```

约束：

* 只处理 `缺图生成计划` 中 `PromptReady=true` 的条目。这里是旧 local_v0 计划的可生成标记，不等于 Catalog v2 的 `PromptAuthoringStatus=prompt_ready`，也不能替代 active PromptRevision 门禁。
* 生成物必须保留 `QualityTier=local_v0`，并进入 `visual_v2_replace` 队列。
* 不允许把 local_v0 当最终美术验收通过，只能用于程序接入、布局验证和可读性预验收。
* 正式替换仍走 Visual V2 流程，不能改变同名 `VisualID`、Approved 路径或 DisplaySpec。

## Generate-VisualV2Plan.ps1

读取 `素材质量替换清单.json` 和 Manifest，把 `visual_v2_replace` 队列转成可执行的正式跑图计划。它不生成图片，只生成当前应该跑哪些 VisualID、用哪个 BatchID、每个素材的生成/预处理/同步命令，以及最近一次 NovelAI 探测结果。

输出：

* `美术文档/_generated/VisualV2生成计划.json`
* `美术文档/_generated/VisualV2生成计划.md`

使用 `-Snapshot` 时额外输出：

* `美术文档/_generated/visual_v2_plan_snapshots/YYYYMMDD_HHMMSS_<SnapshotTag>.json`
* `美术文档/_generated/visual_v2_plan_snapshots/YYYYMMDD_HHMMSS_<SnapshotTag>.md`

使用方式：

```powershell
.\tools\美术工具\Generate-VisualV2Plan.ps1 -Snapshot -SnapshotTag nai_visual_v2_20260525_01 -BatchID nai_visual_v2_20260525_01
```

当 NovelAI Anlas 不足时，也应刷新本计划并记录 `-LastProbeNote`，明确当前是外部额度不足，而不是素材队列、提示词或流水线缺失：

```powershell
.\tools\美术工具\Generate-VisualV2Plan.ps1 -Snapshot -SnapshotTag nai_visual_v2_20260525_01_anlas_blocked -BatchID nai_visual_v2_20260525_01 -LastProbeBatchID nai_visual_v2_probe_20260525_01 -LastProbeNote "NovelAI HTTP 402: Not enough Anlas."
```

## Generate-FormalV2ReplacementPlan.ps1

读取 Formal V2 总方案中的 UI Skin 基准和标记为 `V2-A active` 的场景行，再与当前 Manifest、Approved 文件交叉核对，生成“已有 VisualID 主动质量迭代”计划。它和 `缺图生成计划` 分离：前者动作固定为 `visual_v2_replace`，后者只处理 `generate_needed`。

输出：

* `美术文档/_generated/FormalV2主动迭代计划.json`
* `美术文档/_generated/FormalV2主动迭代计划.md`
* 使用 `-Snapshot` 时写入 `美术文档/_generated/formal_v2_replacement_snapshots/<timestamp>_<tag>/`

```powershell
.\tools\美术工具\Generate-FormalV2ReplacementPlan.ps1 `
  -Snapshot `
  -SnapshotTag formalv2_v2a_active `
  -BatchID formalv2_v2a_replacement_20260719_01 `
  -Variants 2
```

计划不写死 provider 或生成方式，字段使用 `agent_selected`；Agent 应按 `AssetClass` 选择当前能力。`background` 可走普通文生图 / 图生图，`ui_skin` 必须优先参考图编辑、模板合成或确定性生成。计划不会修改 Manifest 主状态、Approved、Unity 或 Registry。

## Run-ArtProductionBatch.ps1

统一执行 `缺图生成计划.json` 或 `FormalV2主动迭代计划.json`，将计划推进到最新数字 `processed/<n>`。普通素材编排现有 `run_art_generation.py`；NineSlice UI Skin 可按本 Run 的 capability route 分流到专项适配器，再统一进入 `optimize_art_assets.py`。执行器不会选择候选、同步 Approved 或调用 Unity。

Formal V2 计划保持 method-neutral；provider 只在本次运行参数中选择：

```powershell
.\tools\美术工具\Run-ArtProductionBatch.ps1 `
  -PlanPath "美术文档/_generated/FormalV2主动迭代计划.json" `
  -Route "background=openai_images","icon=openai_images","standard_asset=openai_images" `
  -AssetClass "background","icon","standard_asset" `
  -ProductionRunID formalv2_standard_batch_20260719_01 `
  -DryRun
```

确认后去掉 `-DryRun`。执行器按 `AssetClass + provider + Manifest status + PromptRevisionID` 隔离分组，避免同一 VisualID 的不同 Revision 共享生成组；每张图仍是串行 `count=1`。只有本批 `generation.json` 中存在可解码 raw 时才允许进入预处理，不能用子进程退出码或旧 `process_report.json` 冒充成功。UI Skin 未提供明确运行时 route 时保持 `route_required:ui_skin`，不会混入通用文生图组。

NineSlice UI Skin 首个专项 capability 为 `deterministic_template`。它从 Manifest 合同读取源尺寸、透明、safe padding、AssetType 和 Border，生成两个无文字候选；能力名只出现在 Run 参数与 `generation.json`，不写入 Manifest、VisualID 或目录契约：

```powershell
.\tools\美术工具\Run-ArtProductionBatch.ps1 `
  -PlanPath "美术文档/_generated/FormalV2主动迭代计划.json" `
  -Route "ui_skin=deterministic_template" `
  -VisualID ui_button_primary `
  -ProductionRunID formalv2_ui_button_primary_skin_pilot_20260719_01 `
  -DryRun
```

确认路由后去掉 `-DryRun`。真实 Run 仍只发布下一不可变 `processed/<n>`；Agent 必须查看原尺寸与 DisplaySpec 预览、写 `visual-review.json`，再通过 `Select-ArtCandidate.ps1` 晋级。

Run evidence 写入 `UnityClient/Logs/P3ArtProduction/<ProductionRunID>/`。处理完成状态为 `review_required`、`processed_failed`、`decision_required` 或 `raw_failed`，仍需 Agent 查看原图、contact sheet 和 DisplaySpec 预览。

## Select-ArtCandidate.ps1

把 Agent 已写入 `visual-review.json` 的选择决定安全晋级到 Profile 工作区 `selected/`：

```powershell
.\tools\美术工具\Select-ArtCandidate.ps1 `
  -VisualID bg_combat_abyss `
  -ReviewPath "UnityClient/Logs/P3ArtProduction/<ProductionRunID>/visual-review.json" `
  -AllowSelectedOverwrite `
  -DryRun
```

命令只接受最新数字轮次中的候选，要求技术状态 `passed`、Agent `RecommendedAction=select`、总分至少 `88`，并重新校验处理证据与 `CandidateSHA256`。去掉 `-DryRun` 后写入 `selected/`、Manifest `SelectedPath`、工作区 `production_decision.json` 和 Run `selection-decision.json`。

已有 selected 内容不同时，评审项必须声明 `SelectionMode=replacement`，并同时提供 `ReviewRubricVersion`、当前 `ReplacementBaseline` 和 `ReplacementPolicy`。基线 `SelectedPath` / `SelectedSHA256` 必须仍与执行时目标一致；候选总分必须达到 `max(88, MinimumScore, baseline Total + MinimumScoreDelta)`，且每个 `ProtectedDimensions` 分数不得下降。质量门禁通过后仍须显式使用 `-AllowSelectedOverwrite`；该参数只授权覆盖文件，不能绕过基线、分数或保护维度检查。比较结果会写入 `PreviousSelected` 和 `PolicyResult`。

候选与现有 selected 的 SHA 相同时返回 `already_selected`，不重复复制，也不要求伪造 replacement 基线；Manifest 的 `approved / registered / validated` 主状态保持不变。

目标路径优先使用 Manifest 中合法的 `SelectedPath`。旧 entry 未回写该字段时，如果 `selected/` 只有一张受支持图片，则复用该历史路径和相邻 `.meta`；存在多张图片时以 `selected_target_ambiguous` 失败，禁止创建第三种隐式目标。目录为空时才使用 `<VisualID>.<候选后缀>` 建立新 selected。

对已是 `approved / registered / validated` 的同 VisualID 替换，Manifest 主状态保持不变。命令不修改 Approved、`.meta`、GUID、Unity 或 Registry。Run 中所有目标完成选择后，`summary.json` 自动收敛为 `selection_complete`。

## Nine-slice UI Skin 技术门禁

`Optimize-ArtAssets.ps1` 读取 Manifest `ProcessSpec.NineSlice`。启用后除了原尺寸、Alpha 和安全画布检查，还会输出 `NineSliceMetrics` 并执行：

* `nine_slice_many_components`：透明前景连通区域超过 `MaxConnectedComponents`，默认 `24`；通常表示漂浮粒子、碎花或脱离边框的装饰过多。
* `nine_slice_edge_coverage_low`：配置的 top / bottom / left / right border band 任一方向 Alpha 覆盖率低于 `MinEdgeCoveragePercent`，默认 `35%`；表示拉伸边带不连续或只有中央孤立图形。

这两项是技术硬失败，候选只能停在最新 `processed/<n>`，不得回退旧轮次或进入 selected。修复时发布下一数字轮次；普通非 nine-slice 图标仍只把连通区域过多作为 warning。

## Run-FormalV2PromptReadyGeneration.ps1

Reads the legacy `formal_v2_prompt_readiness.json` selection report and runs its `program_integrate` assets through `Run-ArtGeneration.ps1` in domain batches. This is a compatibility convenience entry, not the Catalog v2 authoring gate: every selected asset must still resolve a current active PromptRevision, or generation fails before the provider call.

Important rules:

* It always calls NovelAI with `-Concurrency 1`.
* Keep `-DelaySeconds 1` or higher; NovelAI should not be run in parallel.
* Use `-DryRun` first to verify the selected VisualIDs and generated commands.
* Use `-RequireToken` for real runs so missing `NAI_ACCESS_TOKEN` fails before any batch starts.
* A readiness-report `prompt_ready` value only controls this legacy selection list; the executable prompt always comes from the exact active Catalog v2 Revision.
* It uses `-Status approved -PreserveStatus`, so outputs are candidates for same-path replacement and still need preprocessing, review, and strict meta guarded `Sync-ApprovedArt.ps1` before Approved PNGs are replaced.

Examples:

```powershell
.\tools\美术工具\Generate-FormalV2PromptReadiness.ps1 -Snapshot -SnapshotTag formalv2_prompt_ready_before_run
.\tools\美术工具\Run-FormalV2PromptReadyGeneration.ps1 -DryRun -Variants 1 -DelaySeconds 1
.\tools\美术工具\Run-FormalV2PromptReadyGeneration.ps1 -Domain item -Variants 1 -DelaySeconds 1 -RequireToken
```

## Validate-UIDesign.ps1

校验 UI 设计系统的结构化文件，并生成程序交付摘要：

* `美术文档/ui_design/design_tokens.json`
* `美术文档/ui_design/component_catalog.json`
* `美术文档/ui_design/screen_layouts.json`
* `美术文档/ui_design/_generated/ui_design_handoff.md`

使用方式：

```powershell
.\tools\美术工具\Validate-UIDesign.ps1
```

校验内容：

* 组件引用的 `VisualID` 是否存在于 `art_requirements_seed.json` 或 Manifest。
* 界面引用的 `ComponentID` 是否存在于组件目录。
* 界面要求的 `VisualID` 是否已被美术流水线纳管。

## Generate-FormalV2DesignBoards.ps1

生成 Formal V2 的结构设计图，用于评审界面布局、视觉重心、主行动和信息层级。它输出的是确定性 layout board，不是 AI 概念图，不进入 Approved、Manifest 或程序接入清单。

输出：

* `美术文档/ui_design/formal_v2/design_boards/*.png`
* `美术文档/ui_design/formal_v2/design_boards/formal_v2_design_boards.json`
* `美术文档/ui_design/formal_v2/design_boards/README.md`

使用方式：

```powershell
.\tools\美术工具\Generate-FormalV2DesignBoards.ps1 -Overwrite
```

默认覆盖 21 个 active UI 界面，并额外生成 `workshop_studio` 拆分图。若要输出到临时目录，可使用：

```powershell
.\tools\美术工具\Generate-FormalV2DesignBoards.ps1 -OutDir tmp/formal_v2_boards -Overwrite
```

注意：这些图只用于 Formal V2 草案评审。用户确认某个界面后，仍需先更新 active `screen_layouts.json`，再通过 `Validate-UIDesign.ps1` 进入素材和程序交接。

## Scan-UIIterationCandidates.ps1

扫描当前 UI 规格、Manifest、Approved 资源和最新 ArtAcceptance 输出，生成“这版哪些界面能看、哪些适合进入 UI 迭代、哪些还缺程序接入或重跑验收”的候选清单。

输出：

* `美术文档/ui_design/_generated/ui_iteration_candidates.json`
* `美术文档/ui_design/_generated/ui_iteration_candidates.md`

使用方式：

```powershell
.\tools\美术工具\Scan-UIIterationCandidates.ps1
```

报告中的主要状态：

* `viewable_but_stale`：已有截图可看，但截图早于当前 active UI 规格，需要重跑 ArtAcceptance 后才能验收当前版本。
* `registry_or_acceptance_gap`：Approved 素材已具备，但最新 registry / 截图还没反映出来，通常需要 Unity 导入或重跑验收。
* `mvp_baseline_review_ready`：MVP 骨架截图可看，适合进入 Formal V1 结构审查。
* `review_ready`：当前截图可直接进入 UI 设计或视觉精修判断。

## Generate-FormalV1AcceptanceQueue.ps1

读取 active `screen_layouts.json`、Manifest、最新 ArtAcceptance 报告和 Registry 快照，生成 21 个 Formal V1 界面的运行时美术验收队列。它用于统一验收，不替代程序侧实际截图工具。

输出：

* `美术文档/ui_design/_generated/FormalV1验收队列.json`
* `美术文档/ui_design/_generated/FormalV1验收队列.md`

使用 `-Snapshot` 时额外输出：

* `美术文档/ui_design/_generated/formal_v1_acceptance_snapshots/YYYYMMDD_HHMMSS_<SnapshotTag>.json`
* `美术文档/ui_design/_generated/formal_v1_acceptance_snapshots/YYYYMMDD_HHMMSS_<SnapshotTag>.md`

使用方式：

```powershell
.\tools\美术工具\Generate-FormalV1AcceptanceQueue.ps1 -Snapshot -SnapshotTag formal_v1_acceptance_queue
```

主要队列：

* `program_register_visuals`：Approved 素材已具备，但 latest Registry 或截图还没反映，程序先登记并重跑 ArtAcceptance。
* `capture_coverage_needed`：active Formal V1 界面尚未被 ArtAcceptance latest 覆盖，需要程序补截图点或验收入口。
* `review_previous_screenshot`：已有旧截图可粗看，但必须重跑后才能判定当前 active 规格。
* `art_review_ready`：当前截图和数据足够进入正式美术验收。

## Generate-ArtProgramHandoff.ps1

读取 `可接入素材清单.json` 和 `FormalV1验收队列.json`，生成给程序侧的一站式接入交接清单。它不替代两个源报告，而是把程序下一步动作压缩成 VisualID 登记、截图覆盖和 ArtAcceptance 重跑三类队列。

输出：

* `美术文档/_generated/程序接入交接清单.json`
* `美术文档/_generated/程序接入交接清单.md`

使用 `-Snapshot` 时额外输出：

* `美术文档/_generated/art_program_handoff_snapshots/YYYYMMDD_HHMMSS_<SnapshotTag>.json`
* `美术文档/_generated/art_program_handoff_snapshots/YYYYMMDD_HHMMSS_<SnapshotTag>.md`

使用方式：

```powershell
.\tools\美术工具\Generate-ArtProgramHandoff.ps1 -Snapshot -SnapshotTag program_handoff
```

主要队列：

* `RegisterVisualIDs`：程序登记 Approved PNG 到 `VisualAssetRegistry`。
* `AddArtAcceptanceCaptureScreens`：程序补 ArtAcceptance 截图覆盖。
* `RerunArtAcceptanceScreens`：程序登记或补截图后重跑验收。
* `ArtReviewAfterRerunScreens`：重跑后交给美术侧逐屏验收。

## Generate-ArtRegistryGapChecklist.ps1

读取 `程序接入交接清单.json` 和当前 `UnityClient/Assets/Resources/VisualAssetRegistry.asset`，生成更直接的 VisualAssetRegistry 登记缺口核对表。该工具只读 Unity 资产，不修改 Registry；它用于让程序侧确认哪些 Approved VisualID 仍未登记，以及 Approved PNG / `.meta` 是否已经齐全。

输出：

* `美术文档/_generated/VisualAssetRegistry登记缺口清单.json`
* `美术文档/_generated/VisualAssetRegistry登记缺口清单.md`

使用 `-Snapshot` 时额外输出：

* `美术文档/_generated/art_registry_gap_snapshots/YYYYMMDD_HHMMSS_<SnapshotTag>.json`
* `美术文档/_generated/art_registry_gap_snapshots/YYYYMMDD_HHMMSS_<SnapshotTag>.md`

使用方式：

```powershell
.\tools\美术工具\Generate-ArtRegistryGapChecklist.ps1 -Snapshot -SnapshotTag formalv2_registry_gap
```

程序侧处理口径：

1. 在 Unity Editor 中执行 `Tools/P3 Art/Rebuild Approved Sprite Registry`。
2. 保存 `UnityClient/Assets/Resources/VisualAssetRegistry.asset`。
3. 重跑 VisualAsset / ArtAcceptance 验收，并将新的 `UnityClient/Logs/ArtAcceptance/latest` 交回美术侧验收。

## Generate-FormalV2RuntimeAcceptanceStatus.ps1

读取 latest ArtAcceptance、`VisualAssetRegistry登记缺口清单.json` 和 `程序接入交接清单.json`，生成 FormalV2 运行时美术验收门禁状态。它不替代人工截图验收，只判断当前证据是否足够进入美术侧逐屏评审。

输出：

* `美术文档/_generated/FormalV2运行时验收状态.json`
* `美术文档/_generated/FormalV2运行时验收状态.md`

使用 `-Snapshot` 时额外输出：

* `美术文档/_generated/formal_v2_runtime_acceptance_snapshots/YYYYMMDD_HHMMSS_<SnapshotTag>.json`
* `美术文档/_generated/formal_v2_runtime_acceptance_snapshots/YYYYMMDD_HHMMSS_<SnapshotTag>.md`

使用方式：

```powershell
.\tools\美术工具\Generate-FormalV2RuntimeAcceptanceStatus.ps1 -Snapshot -SnapshotTag formalv2_runtime_status
```

主要 Gate：

* `waiting_registry`：当前 handoff 资产仍未登记到 `VisualAssetRegistry.asset`。
* `waiting_art_acceptance_rerun`：Registry 已无缺口，但 latest ArtAcceptance 仍不是新 RunID。
* `runtime_failed_needs_fix`：已有新 ArtAcceptance，但报告仍失败或有 missing VisualID。
* `ready_for_art_review`：具备进入美术侧逐屏截图验收的证据。

## Generate-FormalV2VisualSemanticReview.ps1

Reads `formal_v2_asset_review.json` and generates the Formal V2 visual-semantic review ledger. This is separate from the static technical asset review: `Generate-FormalV2AssetReview.ps1` checks PNG / `.meta` / size / alpha / quality tier; this tool records semantic, style, and small-size readability risks that should be revisited after runtime screenshots exist.

Outputs:

* `美术文档/_generated/formal_v2_asset_review/visual_semantic_review.json`
* `美术文档/_generated/formal_v2_asset_review/visual_semantic_review.md`

With `-Snapshot`, it also writes:

* `美术文档/_generated/formal_v2_asset_review_snapshots/YYYYMMDD_HHMMSS_<SnapshotTag>/`

Example:

```powershell
.\tools\美术工具\Generate-FormalV2VisualSemanticReview.ps1 -Snapshot -SnapshotTag formalv2_semantic_review
```

Risk levels:

* `ok_for_current_v2`: usable for current V2 and ready for normal runtime screenshot review.
* `watch_runtime_readability`: does not block registration, but runtime screenshots should verify small-size readability.
* `style_mismatch_watch`: does not block registration, but runtime screenshots should verify it does not clash with the overall style.
* `secondary_replacement_candidate`: does not block registration; if runtime screenshots prove it hurts recognition or mood, prioritize a second-pass NovelAI same-VisualID replacement.

## Validate-ArtGeneratedJson.ps1

读取美术流水线的关键 `_generated/*.json` 输出，并强制按 UTF-8 / UTF-8 BOM 解析，避免 PowerShell 默认编码导致中文路径或中文字段被误判为 JSON 损坏。该工具不生成新内容，只校验程序交接和美术验收所依赖的关键 JSON 是否可读，并输出核心计数。

覆盖报告：

* `美术文档/_generated/可接入素材清单.json`
* `美术文档/_generated/程序接入交接清单.json`
* `美术文档/_generated/素材质量替换清单.json`
* `美术文档/_generated/美术需求候选清单.json`
* `美术文档/_generated/formal_v2_asset_review/formal_v2_asset_review.json`
* `美术文档/_generated/formal_v2_asset_review/visual_semantic_review.json`
* `美术文档/_generated/formal_v2_prompt_readiness/formal_v2_prompt_readiness.json`
* `美术文档/_generated/VisualAssetRegistry登记缺口清单.json`
* `美术文档/_generated/FormalV2运行时验收状态.json`

使用方式：

```powershell
.\tools\美术工具\Validate-ArtGeneratedJson.ps1 -Strict
```

当前 FormalV2 交接门禁中，推荐在刷新 handoff、asset review 或 prompt readiness 后运行一次；输出应至少确认 `program_integrate`、`generate_needed`、`technical_fix`、`visual_v2_replace`、`new_candidate`、`reviewed/pass` 和 `prompt_ready/prompt_blocked`。

## Generate-FormalV2AssetReview.ps1

读取当前程序接入交接清单中的 `ProgramIntegrateVisuals`，对待程序登记的 Formal V2 Approved 素材做美术侧静态预验收。它不替代 Unity 运行时截图验收，只用于在程序登记前确认 PNG、`.meta`、质量层级、尺寸和 alpha 与 Manifest `SourceSpec` 是否一致，并生成可快速扫看的 contact sheet。

输出：

* `美术文档/_generated/formal_v2_asset_review/formal_v2_asset_review.json`
* `美术文档/_generated/formal_v2_asset_review/formal_v2_asset_review.md`
* `美术文档/_generated/formal_v2_asset_review/contact_sheets/*.png`

使用 `-Snapshot` 时额外输出：

* `美术文档/_generated/formal_v2_asset_review_snapshots/YYYYMMDD_HHMMSS_<SnapshotTag>/`

使用方式：

```powershell
.\tools\美术工具\Generate-FormalV2AssetReview.ps1 -Snapshot -SnapshotTag formalv2_program_integrate_58_precheck
```

主要检查：

* Approved PNG 是否存在。
* Unity `.meta` 是否存在。
* `QualityTiers` 是否包含 `formal_ai_v2` / `final` / `production`。
* PNG 格式、宽高是否匹配 Manifest `SourceSpec`。
* `AlphaRequired=false` 的素材是否存在透明或半透明像素。

结果口径：

* `pass`：可以交给程序登记；仍需登记后跑 ArtAcceptance 做运行时构图和可读性验收。
* `warn`：可以继续交接，但需要运行时截图重点复核。
* `fail`：先由美术侧修复，不建议交给程序登记。
