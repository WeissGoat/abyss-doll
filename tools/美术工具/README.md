# 美术工具

> **定位：** 存放 Project P3 美术流水线脚本。脚本优先服务于“配置表扫描、Manifest 增量更新、批量生成、预处理和验收记录”。

## Update-ArtManifest.ps1

根据最新 `UnityClient/Assets/StreamingAssets/Configs` 扫描当前需要的视觉资产，并增量更新：

* `美术文档/_generated/art_manifest.json`
* `美术文档/_generated/视觉资产Manifest.md`

使用方式：

```powershell
.\tools\美术工具\Update-ArtManifest.ps1
```

实现说明：

* `Update-ArtManifest.ps1` 是 PowerShell 包装器，保持 ASCII 安全，适配 Windows PowerShell。
* 实际逻辑在 `update_art_manifest.py`，负责 UTF-8 中文读写和 Manifest 合并。

默认会保留已有 Manifest 中的状态、提示词、批次路径、筛选路径、Registry 状态和备注；新增配置项会标记为 `todo`，旧配置项会标记为 `deprecated`。

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

## Optimize-ArtAssets.ps1

读取 `_IncomingAI/<VisualID>/raw`，按 Manifest 的结构化 `Spec` 输出 `processed` 和 `contact_sheet`。

使用方式：

```powershell
.\tools\美术工具\Optimize-ArtAssets.ps1 -BatchID nai_p0_item_20260508_01 -Overwrite
```

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
