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
  - 美术文档/02_资源规格与接入规范.md
  - 美术文档/01_Manifest规范.md
  - 美术文档/00_美术流水线总览.md
  - 美术文档/README.md
  - 美术文档/04_美术风格基准.md
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/design_boards/README.md
last_verified: 2026-06-14
update_rule: 修改对应工具入口、参数或执行流程时同步本文件。
---

# 美术工具

> **定位：** 存放 Project P3 美术流水线脚本。脚本优先服务于“配置表扫描、Manifest 增量更新、批量生成、预处理和验收记录”。

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

注意：第一步只负责资产需求发现与台账更新，不自动填写 `PromptEN`、`NegativePromptEN` 和 `Spec`。这些字段在第二步由美术 Agent 逐项补全。

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

## Generate-ArtPrompts.ps1

根据 Manifest 中的 `Status=todo` 条目补全第二步字段：

* `PromptCN`
* `PromptEN`
* `NegativePromptEN`
* `Spec`

同时将状态推进到 `prompted`，并生成：

* `美术文档/_generated/AI绘图提示词清单.md`

使用方式：

```powershell
.\tools\美术工具\Generate-ArtPrompts.ps1
```

默认只处理 `todo` 项；需要重写已有提示词时使用：

```powershell
.\tools\美术工具\Generate-ArtPrompts.ps1 -Overwrite
```

提示词规范：

* `PromptEN` 必须使用英文，只写视觉语言。
* `PromptCN` 用于人工审阅。
* `Spec` 是结构化对象，供后续预处理脚本读取。
* `PromptEN` 禁止项目名、作品名、玩法黑话、`Unity`、`UGUI` 等绘图工具无法理解的词。

## Run-ArtGeneration.ps1

读取 Manifest 中 `Status=prompted` 的条目，按 `PromptEN`、`NegativePromptEN` 和结构化 `Spec` 调用 `tools/ai-image-gateway` 批量生成候选图。

输出目录固定为：

```text
UnityClient/Assets/Art/_IncomingAI/<VisualID>/
  raw/
  processed/
  selected/
  contact_sheet/
  manifest_snapshot.json
  generation.json
  notes.md
```

`BatchID` 只写入 Manifest 和 `generation.json`，不作为目录层级。

使用方式：

```powershell
.\tools\美术工具\Run-ArtGeneration.ps1 -Provider mock -Limit 1 -Variants 2
```

NovelAI 实跑建议使用本地配置。脚本会把 `-Variants` 拆成多次 `count=1` 请求，并默认每张图间隔 1 秒。

凭证优先级：先读 `NAI_ACCESS_TOKEN`；如果当前机器没有设置该环境变量，网关会尝试从 `F:\my_project\new\tags_machine\novelai\client.py` 的 `NAIClient.get_access_token()` 解析 token。不要把真实 token 写入命令、文档或提交记录；需要换路径时设置 `NAI_CLIENT_PY`。

```powershell
Copy-Item .\tools\美术工具\ai_image_gateway.example.yaml .\tools\美术工具\ai_image_gateway.local.yaml
.\tools\美术工具\Run-ArtGeneration.ps1 -Config .\tools\美术工具\ai_image_gateway.local.yaml -Provider novelai -Domain item -Limit 5 -Variants 4 -DelaySeconds 1
```

常用参数：

* `-DryRun`：只打印计划，不生成图片、不改 Manifest。
* `-Domain`、`-VisualID`、`-Priority`：过滤资产。
* `-Limit`：限制本次处理数量。
* `-Seed`：固定基础 seed，便于复现。
* `-DelaySeconds`：每张图之间的等待时间，当前默认 1 秒。
* `-Extra key=value`：透传 provider 参数。
* `-Overwrite`：允许覆盖同名 raw 输出。
* `-PreserveStatus`：用于已接入素材的 Visual V2 候选生成；保留原 `Status`，只写入 `CandidateBatchID` 和 `CandidateRawFiles`。
* `-SkipIntegrationCandidates`：只生成图片，不刷新可接入素材清单。默认不要使用。

非 `-DryRun` 生成完成后，脚本会默认刷新 `美术文档/_generated/可接入素材清单.*`，并在 `美术文档/_generated/art_integration_snapshots/` 写入一份 `generation` 快照。刚生成的 raw 素材会在清单中标为 `art_process`，表示还需要预处理和筛选，不能交给程序接入。

## Optimize-ArtAssets.ps1

读取 `_IncomingAI/<VisualID>/raw`，按 Manifest 的结构化 `Spec` 输出 `processed` 和 `contact_sheet`。

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

把 `_IncomingAI/<VisualID>/selected` 或 fallback 的 `processed` 中当前图片同步到 Manifest 的 `OutputPath`。

同步规则：

* 用 `_IncomingAI` 下的一级目录名匹配 Manifest 的 `VisualID`。
* 优先取 `selected/` 下按文件名升序第一张图片。
* 如果启用 fallback 且 `selected/` 为空，则取 `processed/` 下按文件名升序第一张图片。
* 复制到 `Approved` 目标路径后，更新 `SelectedPath`、`ApprovedPath` 和 `Status=approved`。
* 覆盖已有 PNG 时保留 Unity `.meta` 文件。
* 使用 `-CandidateBatchID` 做 Visual V2 同名替换时，默认启用严格 `.meta` guard：目标 PNG 和目标 `.meta` 必须已经存在；同步前后 `.meta` 字节必须完全一致，否则脚本失败。

使用方式：

```powershell
.\tools\美术工具\Sync-ApprovedArt.ps1 -BatchID nai_p0_item_20260508_01 -Overwrite
```

Visual V2 同名替换使用 `-CandidateBatchID` 和 `-QualityTier formal_ai_v2`。若 `selected` 为空，可加 `-AllowProcessedFallback` 使用本批候选 processed 第一张；同步后可用 `-ClearCandidate` 清理候选字段：

```powershell
.\tools\美术工具\Sync-ApprovedArt.ps1 -Status approved -VisualID ui_icon_diary -CandidateBatchID nai_visual_v2_20260525_01 -AllowProcessedFallback -Overwrite -QualityTier formal_ai_v2 -ClearCandidate
```

这条流程的目标是“替换图片内容，不让程序重新接入”。因此必须保持同一个 `VisualID`、同一个 Manifest `OutputPath`、同一个 Unity `.meta` / GUID。`-AllowNewTargetWithCandidate` 只允许在明确创建新资产路径时使用，不能用于已接入素材的正式图替换。

非 `-DryRun` 同步完成后，脚本会默认刷新“可接入素材清单”，并写入一份 `approved_sync` 快照，方便程序侧直接查看当前哪些 Approved 素材已经可以接入。需要只做同步、不刷新清单时使用 `-SkipIntegrationCandidates`。

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
* `art_approve`：`_IncomingAI/<VisualID>/selected` 已有候选，等待同步到 Approved。
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

* 只处理 `缺图生成计划` 中 `PromptReady=true` 的条目。
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

## Run-FormalV2PromptReadyGeneration.ps1

Reads `formal_v2_prompt_readiness.json` and runs the prompt-ready `program_integrate` assets through `Run-ArtGeneration.ps1` in domain batches. This is a convenience executor for Formal V2 full-quality reruns after prompts have passed the readiness gate.

Important rules:

* It always calls NovelAI with `-Concurrency 1`.
* Keep `-DelaySeconds 1` or higher; NovelAI should not be run in parallel.
* Use `-DryRun` first to verify the selected VisualIDs and generated commands.
* Use `-RequireToken` for real runs so missing `NAI_ACCESS_TOKEN` fails before any batch starts.
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
