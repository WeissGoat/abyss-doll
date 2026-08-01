---
id: spec_formal_v2_replacement_plan_catalog_v2_bridge
title: Formal V2 主动迭代计划 Catalog V2 桥接设计
type: spec
role: 美术
domain: art_asset_production
status: active
source_of_truth: false
related: []
last_verified: 2026-08-01
update_rule: Catalog schema、主动迭代计划字段或批量执行输入契约变化时同步本文件。
---

# Formal V2 主动迭代计划 Catalog V2 桥接设计

## 目标

让 `Generate-FormalV2ReplacementPlan.ps1` 只根据当前 Catalog V2 的 Requirement 与 active immutable PromptRevision 生成正式主动迭代计划，使 `Run-ArtProductionBatch.ps1` 能直接消费准确的 Request、Revision 和 PromptFormat 证据。

本轮只修复计划生成到批量 dry-run 的桥接，不调用图片 provider，不生成 raw，不修改 `processed/`、`selected/`、Approved、Unity、Registry 或运行时状态。

## 根因

当前 Catalog 使用：

- `RequestID`
- `RequirementStatus`
- `RequirementFingerprint`
- `PromptAuthoringStatus`
- `ActivePromptRevisionID`
- `PromptRevisions[].Variants`

但主动迭代计划生成器仍读取旧的 `CompileStatus` 与 `PromptVariants`。因此重新生成计划时会丢失正式 `RequestID` / fingerprint，错误判断 Prompt readiness，并可能继续暴露 `natural_language_v1` / `danbooru_tags_v1`。

## 设计

### Catalog V2 解析

在计划生成器内部增加单一解析边界，输入一个 Catalog Request，输出：

- `RequestID`
- `RequirementFingerprint`
- `PromptAuthoringStatus`
- active `PromptRevisionID`
- active `RevisionFingerprint`
- active Revision 中状态为 `ready` 的 v2 formats
- 无法执行时的明确阻塞原因

active Revision 必须由 `ActivePromptRevisionID` 精确匹配 `PromptRevisions`。只有 Revision 本身 `Status=ready`，且 Variant `Status=ready` 时，该 format 才能进入计划。正式计划只接受 `natural_language_v2` 与 `danbooru_tags_v2`。

### 计划字段

每个计划 Item 持久化：

- `RequestID`
- `RequirementFingerprint`
- `PromptAuthoringStatus`
- `PromptRevisionID`
- `PromptRevisionFingerprint`
- `PromptFormats`
- `PromptReady`
- `PromptBlockReason`

`PromptReady=true` 要求 Requirement ready、authoring 为 `prompt_ready`、active Revision 有效且至少存在一个 ready v2 Variant。缺少条件时 Item 仍可出现在审计计划中，但必须 `PromptReady=false` 并记录原因；后续 Runner 继续 fail closed，不得 fallback 到 v1 或 Manifest 旧 Prompt。

### Manifest 对齐

计划生成时校验 Catalog 与 Manifest `CompiledRequest` 指针：

- RequestID 一致；
- RequirementFingerprint 一致；
- PromptAuthoringStatus 一致；
- ActivePromptRevisionID 一致。

不一致时标记阻塞，不用 Catalog 或 Manifest 任一侧静默覆盖另一侧。

### 批量验证

修复后选择一个 background 与一个 icon 生成 run-scoped 计划，执行：

1. 计划生成；
2. Catalog strict；
3. `Run-ArtProductionBatch.ps1 -DryRun`；
4. 核对 background / icon route 分组、RequestID、PromptRevisionID、PromptFormat 和生成命令。

本轮 claim ceiling 为 `dry_run_planned`。

## 兼容策略

旧 `PromptVariants` 和 v1 formats 仅作为 Catalog 迁移证据保留，不再作为 Formal V2 主动迭代计划的正式输入。旧测试 fixture 迁移到 Catalog V2 结构；不在新解析器中增加隐式 legacy fallback。

## 测试

至少覆盖：

- active natural-language v2 Revision 正确进入计划；
- active Danbooru v2 Revision 正确进入计划；
- `prompt_authoring_required` 标记为 blocked；
- active Revision 缺失或 stale 时标记为 blocked；
- Manifest pointer 与 Catalog 不一致时标记为 blocked；
- v1-only legacy 数据不能成为正式 PromptReady；
- 现有资产分类、Approved 存在性和 VisualID 过滤保持兼容。

## 非目标

- 不修改 Prompt 创作逻辑；
- 不自动发布 PromptRevision；
- 不修改 provider 选择策略；
- 不新增批量执行入口；
- 不推进到 raw、processed、selected、Approved 或 Unity 接入。
