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
last_verified: 2026-05-25
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

```powershell
Copy-Item .\tools\美术工具\ai_image_gateway.example.yaml .\tools\美术工具\ai_image_gateway.local.yaml
$env:NAI_ACCESS_TOKEN = "<token>"
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

使用方式：

```powershell
.\tools\美术工具\Sync-ApprovedArt.ps1 -BatchID nai_p0_item_20260508_01 -Overwrite
```

Visual V2 同名替换使用 `-CandidateBatchID` 和 `-QualityTier formal_ai_v2`。若 `selected` 为空，可加 `-AllowProcessedFallback` 使用本批候选 processed 第一张；同步后可用 `-ClearCandidate` 清理候选字段：

```powershell
.\tools\美术工具\Sync-ApprovedArt.ps1 -Status approved -VisualID ui_icon_diary -CandidateBatchID nai_visual_v2_20260525_01 -AllowProcessedFallback -Overwrite -QualityTier formal_ai_v2 -ClearCandidate
```

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

Visual V2 替换不得改变 `VisualID`、Approved 目标路径、DisplaySpec 或程序绑定。

标准 Visual V2 替换流程：

```powershell
.\tools\美术工具\Run-ArtGeneration.ps1 -Status approved -VisualID <VisualID> -PreserveStatus -BatchID nai_visual_v2_20260525_01 -Variants 4 -DelaySeconds 1
.\tools\美术工具\Optimize-ArtAssets.ps1 -Status approved -CandidateBatchID nai_visual_v2_20260525_01 -Overwrite
.\tools\美术工具\Sync-ApprovedArt.ps1 -Status approved -VisualID <VisualID> -CandidateBatchID nai_visual_v2_20260525_01 -AllowProcessedFallback -Overwrite -QualityTier formal_ai_v2 -ClearCandidate
.\tools\美术工具\Generate-ArtIntegrationCandidates.ps1 -Snapshot -SnapshotTag visual_v2_20260525_01
.\tools\美术工具\Generate-ArtQualityBacklog.ps1 -Snapshot -SnapshotTag visual_v2_20260525_01
```

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
