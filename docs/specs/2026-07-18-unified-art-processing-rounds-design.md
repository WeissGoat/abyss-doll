---
id: spec_unified_art_processing_rounds
title: 美术资产统一数字轮次加工与安全背景处理设计
type: design
role: 美术
domain: art_asset_processing
status: historical
source_of_truth: false
related: []
last_verified: 2026-10-01
update_rule: 历史设计记录，不再更新；现行事实以 美术文档/00_美术流水线总览.md 与 p3-art-asset-production 的 references/candidate-evaluation.md 为准。
---

# 美术资产统一数字轮次加工与安全背景处理设计

> 历史设计：已于 2026-07-18 实施（`26f6be2`、`4592221`、`5950592`、`5646064`）。现行事实以 `美术文档/00_美术流水线总览.md` 与 `p3-art-asset-production` 的 `references/candidate-evaluation.md` 为准。

## 1. 背景与问题

现有 `Optimize-ArtAssets.ps1` 在 `AlphaRequired=true` 或 `Background=transparent` 时，会直接把全图中与角落背景色接近的像素透明化，然后把结果写入平坦的 `processed/*.png`。该行为把“最终需要透明背景”和“允许使用全局颜色阈值去底”混为一体，也把“脚本产生了输出”和“加工结果通过技术门禁”混为一体。

首个 `character_portrait_set` 试跑 `doll_zero_dialogue_neutral` 已证明这一假设不成立：白底 RGB 原图中的白布眼罩、浅色皮肤、裙面和肢体区域被误删为透明洞，但脚本仍成功生成 `processed` 和 contact sheet，接入清单随后把该文件路由为待筛选候选。

本设计重构 `_IncomingAI` 的加工阶段，使普通批量资产继续保持一条命令完成，同时让角色立绘和其他高风险图片可以由 Agent 选择处理能力、保留多轮证据，并阻止失败加工结果进入 Approved。

## 2. 目标与非目标

目标：

- `standard_asset` 和 `character_portrait_set` 使用同一套 `processed/<数字>/` 物理结构。
- 普通安全资产仍可由一条批量命令完成加工、检查和轮次决策。
- `AlphaRequired` 只描述最终结果，不再隐式授权自动去底。
- 自动去底只处理可证明安全的简单连通背景；高风险图片交给 Agent 编排。
- Approved、接入清单、质量替换和恢复任务共用同一候选 Resolver。
- 平坦 `processed/*.png` 可以一次性迁移，且不修改 Approved、Registry、`.meta` GUID 或运行时绑定。

非目标：

- 不在本设计中固定分割、mask、图生图或 provider 实现。
- 不改变 Manifest 主状态流。
- 不取消 `selected/` 或 Manifest `SelectedPath`。
- 不让 `_IncomingAI` 成为 Unity、Prefab 或 Registry 的运行时来源。
- 不在迁移时把缺少历史证据的旧加工结果伪造为技术通过。

## 3. 统一工作区

```text
UnityClient/Assets/Art/_IncomingAI/
  standard_assets/<VisualID>/
    raw/
    processed/
      1/
      2/
    selected/
    contact_sheet/
    process_report.json
    production_decision.json

  character_portraits/<VisualID>/
    raw/
    processed/
      1/
      2/
    selected/
    contact_sheet/
    asset_contract.json
    production_plan.json
    reference_inputs.json
    generation.json
    process_report.json
    production_decision.json
```

不再使用单独的 `iterations/`。所有加工、去底、mask、分割、尺寸调整和局部处理结果统一进入 `processed/<数字>/`。

### 3.1 轮次目录

```text
processed/
  1/
    001.png
    002.png
    mask.png                 # 按需
    process_report.json
    technical_review.json
    visual_review.json       # 按需
    decision.json
    contact_sheet.png
```

规则：

- 目录名必须是十进制正整数，不加 `round_`，不使用零填充作为排序依据。
- 首轮固定为 `1`；新轮次为当前合法数字目录最大值加一。
- 排序必须先解析整数，因此顺序是 `1, 2, 10`，不是字符串顺序。
- 已完成轮次不可默认覆盖；修复、换工具或重试必须新建下一轮。
- 非数字目录、临时目录和未完成目录不参与候选解析。
- 图片文件名只需在该轮内稳定、唯一和可排序；新轮次推荐 `001.png`、`002.png`，迁移旧文件时保留原文件名以减少路径改写。

### 3.2 目录语义

- `raw/`：不可变的生成、导入或图片编辑输入及其原始证据。
- `processed/<数字>/`：第 N 次加工输出；候选可通过、失败或等待决策。
- `selected/`：从加工候选中明确选择出的正式候选。
- 根部 `process_report.json`：数字轮次的轻量生成索引，不复制每轮详细审查内容。
- `production_decision.json`：P3 生产状态机的当前决策、交互恢复点和权限边界；不代替轮次 `decision.json`。
- 顶层 `contact_sheet/`：保留现有兼容入口，由最新轮次生成可快速查看的副本；轮次目录中的 `contact_sheet.png` 才是该轮证据来源。

## 4. 背景处理策略

在 `Spec.ProcessSpec.BackgroundPolicy` 中写入以下稳定策略之一：

```text
preserve
already_transparent
auto_simple
agent_required
```

语义：

- `preserve`：保持原背景，不执行透明化；用于背景、CG 和明确要求全幅画面的资产。
- `already_transparent`：输入应已有有效 alpha；只验证和执行确定性尺寸/画布处理。
- `auto_simple`：允许处理简单、边界连通、与前景可区分的纯色背景；风险检查失败时停止。
- `agent_required`：禁止批处理脚本自行决定去底；由 Agent 检查图片并选择当前可用能力。

`AlphaRequired=true` 仍是最终 SourceSpec，但不决定 `BackgroundPolicy`。策略缺失时不得继续沿用旧的全局颜色去底；迁移完成后正式 Manifest 必须显式具有策略。

建议默认路由：

- 背景、叙事 CG：`preserve`。
- 已声明后端原生透明的图片：`already_transparent`。
- 普通物品图标、节点、轮廓清楚的单色底资产：`auto_simple`。
- 角色立绘、白发白衣、玻璃、烟雾、光效和复杂半透明材质：`agent_required`。

ProductionProfile 不决定图片方法。Profile 可以影响默认风险等级，但 Agent 仍按 Asset Contract、原图和当前能力规划实际处理。

## 5. 自动处理与 Agent 处理

### 5.1 普通批量流程

外部仍是一条命令：

```powershell
.\tools\美术工具\Optimize-ArtAssets.ps1 -Status generated
```

内部流程：

```text
raw
  -> 分配下一数字轮次
  -> 临时生成加工候选
  -> 自动技术检查
  -> 写轮次报告和 decision.json
  -> 原子发布 processed/<数字>/
```

自动处理失败时，命令仍写出失败轮次证据，但该轮不能成为 Approved fallback。

### 5.2 `auto_simple` 算法边界

旧算法的全图颜色替换必须删除。新的简单背景能力从画布边界建立背景种子，只移除与边界连通且满足背景相似条件的区域，并在边缘做有限羽化。

至少在以下情况停止自动去底：

- 主体触碰画布边界；
- 边缘背景颜色不稳定或存在多个大面积背景区域；
- 背景色大量出现在主体内部；
- 检测到白发、浅色服装、玻璃、烟雾、强光或半透明材质风险；
- 去底前后主体面积、包围盒或连通结构异常变化；
- 主体内部出现新增的大面积透明洞。

### 5.3 Agent 复杂处理

`agent_required` 由 `p3-art-asset-production` 编排，图片生成或编辑能力委托给 `generate-image` 或当前可用工具。要求和 Asset Contract 保持方法中立；实际能力、provider、输入、mask、输出和失败记录在轮次证据中。

Agent 可以在同一轮写出多个加工候选，也可以在后续数字轮次换能力或修复。角色立绘必须检查脸、眼罩、前后发、发梢、手、脚、披肩/裙子分界、主体内部透明洞以及黑底、白底、棋盘格三种预览。

## 6. 技术门禁与轮次决策

自动检查至少覆盖：

- 文件可解码、格式和目标尺寸；
- alpha 是否符合策略和 SourceSpec；
- 主体包围盒、基线、安全区和面积变化；
- 新增内部透明洞；
- 主体连通结构异常碎裂；
- 半透明边缘比例异常增长；
- 输出 hash 与报告一致；
- 白底、黑底和棋盘格预览可生成。

轮次 `decision.json` 使用逐候选结果，不增加 `EligibleCandidates` 或 `SelectedCandidate`：

```json
{
  "State": "passed",
  "Candidates": [
    {
      "File": "001.png",
      "Status": "passed",
      "SHA256": "..."
    },
    {
      "File": "002.png",
      "Status": "failed",
      "Reason": "transparent_holes_detected"
    }
  ]
}
```

允许的轮次状态：

- `passed`：至少一张候选通过，且没有未解决的轮次级硬阻塞。
- `decision_required`：存在需要 Agent 或用户选择的处理方式、方向或候选。
- `failed`：本轮没有可进入选择阶段的候选。
- `legacy_unverified`：迁移自旧平坦目录，缺少足以声明通过的历史证据。

## 7. Selected 与 Approved Resolver

所有消费者共用一个 Resolver，优先级固定为：

```text
1. Manifest.SelectedPath
2. selected/ 中按稳定文件名排序的第一张，兼容旧流程
3. processed/ 最大数字目录中的单一通过候选
```

第三步只在以下条件全部满足时生效：

- 最新数字目录存在完整 `decision.json`；
- 轮次 `State=passed`；
- `Candidates` 中恰好只有一张 `Status=passed`；
- 对应文件存在，且 hash、尺寸和格式与报告一致。

如果最新轮次失败、等待决策或为 `legacy_unverified`，不得回退到更早数字轮次。存在两张以上通过候选时，不得取文件名第一张；必须由 Agent 或用户选择并复制到 `selected/`，再写 Manifest `SelectedPath`。

当 `Sync-ApprovedArt.ps1` 使用最新 processed 单候选 fallback 时，同步必须把实际来源写入 `SelectedPath`，再写 `ApprovedPath` 和 `Status=approved`。具有完整通过决策的最新单候选轮次默认允许该 fallback；现有 `--allow-processed-fallback` 参数继续被接受但标记为 deprecated，不再是启用安全数字轮次 fallback 的必要条件。新的 Resolver 不再读取平坦 `processed/*.png`。

## 8. Manifest 与生成报告兼容

Manifest 主状态保持：

```text
todo -> prompted -> generated -> selected -> approved
```

只有 raw、正在加工、轮次失败或已有 processed 但未选择时，主状态都保持 `generated`。加工细节由轮次证据表达。

接入候选报告增加：

```text
ProcessedRound
ProcessingState
ProcessedCandidate
```

路由：

- 无合法数字轮次但有 raw：`art_process`。
- 最新轮次为 `failed`、`decision_required` 或 `legacy_unverified`：`art_select`，Reason 明确修复或决策需求。
- 最新轮次 `passed` 且存在通过候选：`art_select`。
- selected 或 SelectedPath 存在：`art_approve`。
- Approved 后保持现有 Unity/Registry/验收路由。

## 9. 工具边界

新增共享 Python 模块 `art_processing.py`，负责：

- 数字轮次枚举、整数排序和下一序号分配；
- 临时轮次、锁和原子发布；
- `decision.json` schema 校验；
- 最新轮次与单一通过候选解析；
- selected/processed/Approved 候选优先级；
- 工作区路径和文件边界校验。

现有工具调整：

- `Optimize-ArtAssets.ps1`：保留公开入口；按 BackgroundPolicy 创建新轮次并写技术证据。
- `Sync-ApprovedArt.ps1`：改用共享 Resolver。
- `Generate-ArtIntegrationCandidates.ps1`：改用共享 Resolver 和 ProcessingState。
- 质量替换、CandidateBatch 和其他读取 processed 的脚本：禁止自行扫描文件，统一调用共享模块。
- Agent 外部图片能力产生的加工结果必须通过安全导入/登记入口进入新数字轮次，不能直接写 selected 或 Approved。

## 10. 并发、异常与可恢复性

- 为每个 VisualID 工作区建立短期加工锁，防止两个执行者分配同一数字序号。
- 候选先写入不参与扫描的临时目录；报告和 decision 完整后，原子改名为数字目录。
- 执行失败也应发布一个有 `State=failed` 的完整轮次；进程崩溃留下的临时目录由健康检查报告，不参与 Resolver。
- 生产模式禁止覆盖已发布数字轮次；诊断覆盖必须是单独、显式且不用于正式证据的能力。
- dry-run 必须显示目标 VisualID、BackgroundPolicy、将分配的数字轮次、输入和预计动作，但不创建目录或修改 Manifest。

## 11. 一次性迁移

迁移顺序：

1. 先实现共享 Resolver、schema 和测试。
2. DryRun 盘点 `standard_assets` 与 `character_portraits` 中的平坦 `processed/*.png`、现有 selected、Manifest 引用和 `.meta`。
3. 将平坦文件迁移到 `processed/1/`；移动 Unity 资产时同步移动对应 `.meta`，保留 GUID。
4. 为缺少完整历史门禁的旧结果写 `State=legacy_unverified`，不得伪造 `passed`。
5. 已有 `selected/`、Approved 和运行时绑定保持不变。Manifest `SelectedPath` 通常保持不变；如果它明确指向本次被移动的平坦 processed 文件，则在确认迁移前后 SHA-256 相同后改写为新的数字轮次路径。
6. 删除文档中的 `iterations/` 约定，更新统一数字轮次结构。
7. 为 Manifest Spec 补齐显式 BackgroundPolicy，并通过 owning generator 刷新生成物。
8. 刷新接入、质量和程序交接报告。
9. 比较 Approved、Registry、原始 source 和 `.meta` 保护哈希。

迁移 readiness gate 完成后，不再支持旧平坦 processed 作为生产输入；若仍发现平坦文件，返回 `blocked:art_processed_round_migration_required`。

## 12. Zero 试跑恢复

当前损坏结果：

```text
character_portraits/doll_zero_dialogue_neutral/processed/r01_001.png
```

迁移为：

```text
processed/1/r01_001.png
processed/1/process_report.json
processed/1/technical_review.json
processed/1/visual_review.json
processed/1/decision.json   # State=failed
```

下一次保白去底或前景分割从 `processed/2/` 开始。只有新轮次技术和视觉完整性通过后，才能进入 selected 或单候选 Approved fallback；当前任务的 Approved 权限仍保持关闭。

## 13. 测试与验收

单元测试：

- 数字目录按 `1, 2, 10` 排序；非法名称被忽略或拒绝。
- 下一序号分配、锁和原子发布。
- SelectedPath、selected 和 processed fallback 优先级。
- 最新轮次失败时不得回退旧轮次。
- 最新轮次单一通过候选允许 fallback；多候选必须 selected。
- decision 缺失、hash 不一致、文件缺失时拒绝候选。
- BackgroundPolicy 缺失时不触发旧全局颜色去底。
- 边界连通去底保留与背景同色但不连通的主体内部区域。
- 白发、白布、浅色皮肤和裙面回归样例不能产生大面积透明洞。

集成测试：

- `standard_asset` 一条命令创建数字轮次、报告和决策。
- `character_portrait_set` 可以注册外部处理候选并恢复下一轮。
- 接入清单正确显示 ProcessedRound 和 ProcessingState。
- Approved Resolver 对 selected 和最新单候选 round 兼容。
- 迁移 DryRun 和正式迁移文件计数一致，`.meta` 同步移动。

边界验收：

- Approved 和 Registry 保护哈希不变。
- `_IncomingAI` 没有被 Prefab、Registry 或运行时代码引用。
- 未通过候选不能进入 Approved。
- Zero 旧损坏结果被明确记录为失败轮次，不能继续显示为可直接选择的合格 processed。

## 14. 完成口径

设计实施完成要求：

- 所有 active 工具和文档使用 `processed/<数字>/`。
- 平坦 processed 和 `iterations/` 不再作为生产结构。
- BackgroundPolicy 显式存在且不由 AlphaRequired 隐式推导危险操作。
- 标准批处理仍保持一条命令体验。
- 角色复杂加工可以按数字轮次留下能力、mask、技术和视觉证据。
- Approved、接入清单和恢复任务共用同一 Resolver。
- 迁移、测试、文档校验和保护哈希全部有新鲜证据。
