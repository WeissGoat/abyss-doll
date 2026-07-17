---
id: art_readme
title: 美术文档索引
type: art
role: 美术
domain: art_pipeline
status: active
source_of_truth: true
related:
  - GEMINI.md
  - 开发文档/14_Unity运行时美术自动验收方案.md
  - 开发文档/19_UnityMCP验收编排层设计.md
  - 开发文档/rules/03_视觉资源系统程序开发规范.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - 美术文档/13_正式纵切UI与素材覆盖矩阵.md
  - 美术文档/14_FormalV2素材候选审查记录.md
  - 美术文档/16_Live2D角色动画资产接入规格.md
  - 美术文档/17_Agent原生动态立绘资产接入规格.md
  - 美术文档/18_CG底图与漫画式播放演出工作流.md
  - 美术文档/19_T0-01序章CG细案.md
  - 美术文档/20_GIF小循环人物替换工作流.md
  - 美术文档/人设/README.md
  - 美术文档/人设/01_人设参考获取规则.md
  - 美术文档/人设/02_零号原型参考_失明少女.md
  - 美术文档/人设/03_零号初版人设方案.md
  - 美术文档/人设/04_零号AI后端出图提示词对比.md
  - 美术文档/00_美术流水线总览.md
  - 美术文档/ui_design/README.md
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 美术文档/archive/README.md
  - agent_status/art.md
  - tools/美术工具/README.md
  - 知识库/views/art.md
last_verified: 2026-07-18
update_rule: 美术文档结构、推荐阅读顺序或外部契约变化时同步本文件。
---

# 美术文档索引

> **定位：** Project P3 美术 / UI 文档入口。本文件只负责导航和职责边界，不承载详细规格。
> **更新时间：** 2026-07-18

## 先看哪里

| 想确认什么 | 看哪个文档 | 说明 |
|---|---|---|
| 当前美术规划和优先级 | [10_正式版核心纵切美术路线.md](10_正式版核心纵切美术路线.md) | 美术侧当前路线的唯一规划入口。 |
| 当前哪些 UI 已覆盖、哪些还没进 active | [13_正式纵切UI与素材覆盖矩阵.md](13_正式纵切UI与素材覆盖矩阵.md) | 把 `09` 路线和 `11` 批次矩阵翻译成美术覆盖表。 |
| 端到端美术生产怎么走 | [00_美术流水线总览.md](00_美术流水线总览.md) | 只讲流程：需求发现、Manifest、提示词、出图、预处理、Approved、交接。 |
| Agent 如何端到端生产正式美术资产 | [../.codex/skills/p3-art-asset-production/SKILL.md](../.codex/skills/p3-art-asset-production/SKILL.md) | 从需求准入、Asset Contract、候选生成与 Agent 评分推进到 Approved、Unity 接入、运行时验收和状态回写；支持交互与全自动模式。 |
| Agent 如何生成或调整图片 | [../.codex/skills/p3-generate-image/SKILL.md](../.codex/skills/p3-generate-image/SKILL.md) | `generate-image` 只负责文生图、图生图、差分、inpaint、后端和 raw 生成证据；不负责 Approved 或 Unity 接入。 |
| 小循环 GIF 如何逐帧替换人物 | [20_GIF小循环人物替换工作流.md](20_GIF小循环人物替换工作流.md) | 独立工具工作流，定义拆帧、身份包、两帧预审、独立逐帧图生图、风险检查和 GIF 重编码；当前为已批准设计、尚未实现。 |
| UI 结构版本怎么管理 | [ui_design/README.md](ui_design/README.md) | UI 设计系统入口，说明 active / baseline / draft / handoff。 |
| Formal V2 UX/UI 怎么推进 | [ui_design/formal_v2/00_formal_v2_ux_ui_overview.md](ui_design/formal_v2/00_formal_v2_ux_ui_overview.md) | 当前 Formal V2 总方案，解决按钮堆叠和正式感不足。 |
| 程序下一步接入 / 验收要做什么 | [_generated/程序接入交接清单.md](_generated/程序接入交接清单.md) | 程序侧一站式入口，汇总 VisualID 登记、截图覆盖和 ArtAcceptance 重跑队列。 |
| 最新文档 / 配置里可能新增了哪些美术需求 | [_generated/美术需求候选清单.md](_generated/美术需求候选清单.md) | 美术侧审查入口，只提示候选，不自动写 Manifest 或 seed。 |
| FormalV2 候选哪些准入 / 暂缓 | [14_FormalV2素材候选审查记录.md](14_FormalV2素材候选审查记录.md) | 人工审查候选素材，记录哪些进入 seed / Manifest，哪些是误报或暂缓。 |
| 人设参考怎么获取和过滤 | [人设/01_人设参考获取规则.md](人设/01_人设参考获取规则.md) | 角色参考研究规则：`1girl rating:g`、二次过滤、重复角色归并、中文名字段和报告解释口径。 |
| 零号当前指定原型是谁 | [人设/02_零号原型参考_失明少女.md](人设/02_零号原型参考_失明少女.md) | 用户指定以《漆黑的子弹》失明少女为原型，记录资料、图片链接、可转译设计点和禁止照搬项。 |
| 零号初版人设是什么 | [人设/03_零号初版人设方案.md](人设/03_零号初版人设方案.md) | 当前初版方案：白布遮眼、灰披肩、超短内衬裙、裸腿裸足、核心仓隐藏和初始三无。 |
| 零号三后端出图提示词 | [人设/04_零号AI后端出图提示词对比.md](人设/04_零号AI后端出图提示词对比.md) | ChatGPT / Gemini(nanobanana) / NovelAI 的差异化提示词、工具入口和候选图输出口径。 |
| 程序当前应接入哪些素材 | [_generated/可接入素材清单.md](_generated/可接入素材清单.md) | 程序侧素材来源清单，只处理 `program_integrate` 队列。 |
| 当前哪些缺图素材可直接跑图 | [_generated/缺图生成计划.md](_generated/缺图生成计划.md) | 美术侧处理 `generate_needed` 队列。 |
| 当前哪些素材只是临时质量 | [_generated/素材质量替换清单.md](_generated/素材质量替换清单.md) | 美术侧处理 `technical_fix` 和 `visual_v2_replace` 队列。 |
| Formal V1 运行时验收怎么统一排队 | [ui_design/_generated/FormalV1验收队列.md](ui_design/_generated/FormalV1验收队列.md) | 美术侧按该队列等待程序登记、补截图或逐屏验收。 |
| 首版动态魔偶立绘怎么做 | [17_Agent原生动态立绘资产接入规格.md](17_Agent原生动态立绘资产接入规格.md) | 当前首版主线：Agent 生成分层图、rig JSON、motion JSON、expression JSON，Unity importer / runtime 生成 Prefab 和独立验收。 |
| CG 底图 / 漫画式播放怎么做 | [18_CG底图与漫画式播放演出工作流.md](18_CG底图与漫画式播放演出工作流.md) | 运行时漫画页拼装、Panel VisualID、T0 样板、CG 底图生产与验收口径。 |
| T0-01 序章 CG 具体画什么 | [19_T0-01序章CG细案.md](19_T0-01序章CG细案.md) | T0-01A 序章漫画页分镜、Panel VisualID、构图要求、零号表现和截图验收清单。 |
| Cubism / Spine 外部导出怎么接入 | [16_Live2D角色动画资产接入规格.md](16_Live2D角色动画资产接入规格.md) | 只定义获得 Cubism / Spine 工具链或外部 rigger 交付后的兼容接入，不作为首版阻塞项。 |
| 当前美术状态和下一步 | [../agent_status/art.md](../agent_status/art.md) | 智能体交接状态页。 |

## 文档分层

### 规划层

| 文档 | 职责 |
|---|---|
| [10_正式版核心纵切美术路线.md](10_正式版核心纵切美术路线.md) | 当前美术规划、正式版原则、近期执行顺序。 |
| [13_正式纵切UI与素材覆盖矩阵.md](13_正式纵切UI与素材覆盖矩阵.md) | UI / 素材覆盖矩阵、active 界面、draft 界面和缺口队列。 |
| [人设/README.md](人设/README.md) | 人设 Owner 入口，承接角色参考研究、零号人设方向、交付包和 DollPuppet 前置规则。 |
| [../agent_status/art.md](../agent_status/art.md) | 当前事实状态、最近完成、下一步建议和阻塞。 |

### 工作流层

| 工作流 | 主入口 | 细节文档 |
|---|---|---|
| 资产生产流水线 | [00_美术流水线总览.md](00_美术流水线总览.md) | [01_Manifest规范.md](01_Manifest规范.md)、[02_资源规格与接入规范.md](02_资源规格与接入规范.md)、[03_AI生成与筛选规范.md](03_AI生成与筛选规范.md)、[04_美术风格基准.md](04_美术风格基准.md)、[05_AI图片网关接入方案.md](05_AI图片网关接入方案.md) |
| GIF 小循环人物替换 | [20_GIF小循环人物替换工作流.md](20_GIF小循环人物替换工作流.md) | 独立于图片网关契约，调用 `generate-image` 能力，首版只覆盖 8-30 帧小循环。 |
| 人设参考研究流水线 | [人设/README.md](人设/README.md) | [人设/01_人设参考获取规则.md](人设/01_人设参考获取规则.md)、[_generated/danbooru_character_reference/zero_doll_reference_report.md](_generated/danbooru_character_reference/zero_doll_reference_report.md) |
| UI 设计版本流水线 | [ui_design/README.md](ui_design/README.md) | [ui_design/ui_iteration_process.md](ui_design/ui_iteration_process.md)、[ui_design/formal_v1/screen_structure_review.md](ui_design/formal_v1/screen_structure_review.md)、[ui_design/formal_v2/00_formal_v2_ux_ui_overview.md](ui_design/formal_v2/00_formal_v2_ux_ui_overview.md)、[ui_design/versions/migration_log.md](ui_design/versions/migration_log.md) |
| 运行时验收流水线 | [../.codex/skills/p3-art-validation/SKILL.md](../.codex/skills/p3-art-validation/SKILL.md) | [../开发文档/19_UnityMCP验收编排层设计.md](../开发文档/19_UnityMCP验收编排层设计.md)、[09_运行时美术验收记录.md](09_运行时美术验收记录.md)；旧 Runner 契约见 `开发文档/14`。 |
| Agent 原生动态立绘流水线 | [17_Agent原生动态立绘资产接入规格.md](17_Agent原生动态立绘资产接入规格.md) | 首版 DollPuppet 包、JSON rig / motion / expression、Unity Prefab、fallback sprite 和独立多帧验收。 |
| CG 底图 / 漫画式播放流水线 | [18_CG底图与漫画式播放演出工作流.md](18_CG底图与漫画式播放演出工作流.md) | 叙事 Page、Panel VisualID、LayoutPreset、字幕条、逐格显现、T0 样板和运行时截图验收；T0-01 具体分镜见 [19_T0-01序章CG细案.md](19_T0-01序章CG细案.md)。 |
| Cubism / Spine 兼容流水线 | [16_Live2D角色动画资产接入规格.md](16_Live2D角色动画资产接入规格.md) | 外部工具链或 rigger 交付后的 Cubism / Spine runtime 包接入。 |

### 契约与数据层

| 文件 | 用途 |
|---|---|
| [art_requirements_seed.json](art_requirements_seed.json) | 配置表扫不出的 preset 资产需求种子。 |
| [ui_design/screen_layouts.json](ui_design/screen_layouts.json) | 当前 active UI 对接规格，程序和素材生成只认这里。 |
| [ui_design/component_catalog.json](ui_design/component_catalog.json) | UI 组件目录和 VisualID 映射。 |
| [ui_design/design_tokens.json](ui_design/design_tokens.json) | 参考分辨率、间距、颜色、层级等 UI 基础标准。 |

### 交付与历史层

| 文档 | 当前定位 |
|---|---|
| [archive/README.md](archive/README.md) | 归档入口，保存 MVP 记录和旧批次交付快照。 |
| [archive/11_P0_UI骨架接入交付.md](archive/11_P0_UI骨架接入交付.md) | P0 批次交付快照，具体接入仍以 active JSON 和 handoff 为准。 |
| [archive/12_P1_UI骨架接入准备.md](archive/12_P1_UI骨架接入准备.md) | P1 批次交付快照，具体接入仍以 active JSON 和 handoff 为准。 |
| [archive/06_MVP素材接入状态同步.md](archive/06_MVP素材接入状态同步.md) | 历史归档，不作为当前规划入口。 |
| [archive/07_MVP_UI重新设计同步.md](archive/07_MVP_UI重新设计同步.md) | 历史归档，不作为正式 UI 目标。 |

## 四套工作流

### UI 设计版本流

```text
baseline / 当前截图问题
  -> formal_v1 设计文档
  -> formal_v2 UX/UI 设计文档（当 Formal V1 仍像按钮菜单时）
  -> 用户确认
  -> active screen_layouts.json
  -> Validate-UIDesign.ps1
  -> 程序接入
  -> ArtAcceptance
```

程序只接 active `screen_layouts.json`。`formal_v1/*.md` 如果还没写入 active，只是设计草案，不是程序接入口。
`formal_v2/*.md` 同样只是设计草案：用于重审玩家目标、主次行动、场景隐喻和信息架构；用户确认并写入 active 前，不允许要求程序接入或触发素材生成。

完整版本迭代规则见 [ui_design/ui_iteration_process.md](ui_design/ui_iteration_process.md)。该工作流负责：冻结旧版本 baseline、编写新版本 draft、用户确认后修改 active、生成 handoff、程序接入、运行时验收和 validated 回填。

### 资产生产流

```text
design / config / ui active
  -> requirement candidate scan
  -> confirmed config / derived / preset
  -> art_manifest.json
  -> PromptCN / PromptEN / NegativePromptEN / Spec
  -> AI 生成
  -> 预处理
  -> 筛选
  -> Approved
  -> 可接入素材清单
```

`_IncomingAI` 是工作区，不进程序接入；`Approved` 是正式区。每次生成、预处理或同步 Approved 后，都要刷新 latest 可接入清单并留 snapshot。

`generate_needed` 缺图队列先生成 [_generated/缺图生成计划.md](_generated/缺图生成计划.md)，再由 `$p3-art-asset-production` 编排并调用 `$generate-image` 产生 raw 候选，之后继续完成预处理、Agent 筛选和 Approved 门禁。`visual_v2_replace` 质量替换队列看 [_generated/VisualV2生成计划.md](_generated/VisualV2生成计划.md)，两者不要混用。

`美术需求候选清单` 是人工审查前哨：它扫描最新设计文档、配置 README / JSON、版本规划和 active UI 文档中出现的显式 VisualID 或可派生图标候选。确认后再写入 `art_requirements_seed.json` 或等待正式配置字段落地；不能把候选报告直接当成 Manifest。

### CG 底图 / 漫画式播放流

```text
剧情 / 细案 / 参考
  -> 漫画页分镜表
  -> Panel VisualID 清单
  -> preset seed / Manifest
  -> Prompt / Spec
  -> AI 生成与 contact sheet
  -> Approved Panel
  -> 运行时漫画页拼装
  -> 截图 / Play 路径验收
```

该工作流使用运行时拼装方案：图片只做无字 Panel，UGUI 负责黑色分隔、版式、底部字幕条、逐格显现和关键按钮。T0-01A 是首个样板；通用规则见 [18_CG底图与漫画式播放演出工作流.md](18_CG底图与漫画式播放演出工作流.md)，序章具体页、Panel 和验收清单见 [19_T0-01序章CG细案.md](19_T0-01序章CG细案.md)。

### 程序接入与验收流

```text
程序接入交接清单
  -> 可接入素材清单 program_integrate
  -> VisualAssetRegistry 登记
  -> UI / 配置绑定
  -> ArtAcceptance 截图
  -> 09_运行时美术验收记录.md
  -> 缺口回填到 seed / UI active / Manifest
```

## 当前状态

截至 2026-05-26：

* active Formal V1 UI 已覆盖 21 个界面，详见 [13_正式纵切UI与素材覆盖矩阵.md](13_正式纵切UI与素材覆盖矩阵.md)。
* Formal V2 UX/UI 重构已建立 draft 设计层，当前总方案见 [ui_design/formal_v2/00_formal_v2_ux_ui_overview.md](ui_design/formal_v2/00_formal_v2_ux_ui_overview.md)；V2-A 优先重审 `workshop_main`、`combat_hud`、`inventory_loot`、`dungeon_map`、`settlement`。
* 最新美术需求候选清单显示 `new_candidate=55`、`approved_without_manifest=0`、`seed_only=0`、`manifest_managed=244`；本轮已从候选中准入 29 个 FormalV2 新素材需求，详见 [14_FormalV2素材候选审查记录.md](14_FormalV2素材候选审查记录.md)。
* 最新可接入素材清单显示 `program_integrate=0`、`acceptance_needed=191`，当前没有新的 Approved 素材登记队列，重点转为运行时截图验收和 Manifest 状态回填。
* 最新程序接入交接清单显示 `program_integrate=0`、`Screens needing ArtAcceptance capture coverage=6`、`Screens needing ArtAcceptance rerun=15`，程序侧优先按该清单补截图和重跑验收。
* 最新缺图生成计划显示 `generate_needed=0`；当前没有阻塞程序接入的新缺图项。
* 最新素材质量替换清单显示 `technical_fix=0`、`visual_v2_replace=128`；历史本地生成图已统一补标 `QualityTier=local_v0`，这些素材不阻塞程序接入，但要在 Visual V2 批次同名替换。
* Formal V1 验收队列已生成：21 个 active 界面纳入队列，15 个有 latest 旧截图可粗看，10 个仍需程序登记 VisualID 或补截图后重跑 ArtAcceptance。
* NovelAI token 链路已通过 P0 新节点图标验证；`node_eventnode_icon`、`node_hazardnode_icon`、`node_reststopnode_icon`、`node_treasurenode_icon` 已同步为 `formal_ai_v2`，后续按 `nai_visual_v2_20260526_02` 队列继续串行替换。

## 机器生成文件

* [_generated/art_manifest.json](_generated/art_manifest.json)：机器可读 Manifest。
* [_generated/视觉资产Manifest.md](_generated/视觉资产Manifest.md)：脚本生成的 Manifest 摘要，方便快速查看。
* [_generated/AI绘图提示词清单.md](_generated/AI绘图提示词清单.md)：脚本补全后的提示词清单，供出图和审阅。
* [_generated/美术需求候选清单.md](_generated/美术需求候选清单.md)：从设计 / 配置 / UI 文档扫描出的潜在新增美术需求，人工审查后才进入 seed 或配置。
* [_generated/art_requirement_candidate_snapshots/](_generated/art_requirement_candidate_snapshots/)：美术需求候选清单历史快照。
* [_generated/可接入素材清单.md](_generated/可接入素材清单.md)：当前可接入素材 latest，程序侧优先按其中 `program_integrate` 队列接入。
* [_generated/程序接入交接清单.md](_generated/程序接入交接清单.md)：程序接入 latest，一站式汇总 VisualID 登记、截图覆盖和 ArtAcceptance 重跑队列。
* [_generated/art_program_handoff_snapshots/](_generated/art_program_handoff_snapshots/)：程序接入交接清单历史快照。
* [_generated/art_integration_snapshots/](_generated/art_integration_snapshots/)：每次生成、预处理或 Approved 同步后的可接入素材清单快照。
* [_generated/缺图生成计划.md](_generated/缺图生成计划.md)：当前 `generate_needed` 缺图队列的可执行跑图计划。
* [_generated/art_generation_plan_snapshots/](_generated/art_generation_plan_snapshots/)：缺图跑图计划历史快照。
* [_generated/素材质量替换清单.md](_generated/素材质量替换清单.md)：当前质量替换 latest，区分技术修复和 Visual V2 同名替换。
* [_generated/local_v0_quality_normalization.md](_generated/local_v0_quality_normalization.md)：历史本地生成素材的 `QualityTier=local_v0` 规范化记录。
* [_generated/art_quality_snapshots/](_generated/art_quality_snapshots/)：素材质量替换清单历史快照。
* [ui_design/_generated/ui_design_handoff.md](ui_design/_generated/ui_design_handoff.md)：UI 设计校验后生成的程序交付摘要。
* [ui_design/_generated/FormalV1验收队列.md](ui_design/_generated/FormalV1验收队列.md)：21 个 active Formal V1 界面的运行时美术验收队列。
* [ui_design/_generated/formal_v1_acceptance_snapshots/](ui_design/_generated/formal_v1_acceptance_snapshots/)：Formal V1 验收队列历史快照。

生成命令：

```powershell
.\tools\美术工具\Update-ArtManifest.ps1
.\tools\美术工具\Scan-ArtRequirementCandidates.ps1 -Snapshot -SnapshotTag manual_review
.\tools\美术工具\Generate-ArtPrompts.ps1
.\tools\美术工具\Validate-UIDesign.ps1
.\tools\美术工具\Generate-ArtIntegrationCandidates.ps1 -Snapshot -SnapshotTag manual_review
.\tools\美术工具\Generate-ArtBatchPlan.ps1 -Snapshot -SnapshotTag manual_review
.\tools\美术工具\Generate-ArtQualityBacklog.ps1 -Snapshot -SnapshotTag manual_review
.\tools\美术工具\Normalize-ArtQualityTier.ps1 -DryRun -Snapshot -SnapshotTag manual_review
.\tools\美术工具\Generate-FormalV1AcceptanceQueue.ps1 -Snapshot -SnapshotTag manual_review
.\tools\美术工具\Generate-ArtProgramHandoff.ps1 -Snapshot -SnapshotTag manual_review
```

## 外部契约

* [版本规划/09_正式版核心纵切开发路线.md](../版本规划/09_正式版核心纵切开发路线.md)：当前正式版核心纵切的顶层路线，以此为准。
* [版本规划/11_纵切批次与需求文档承接矩阵.md](../版本规划/11_纵切批次与需求文档承接矩阵.md)：设计文档批次与完成口径。
* [开发文档/rules/03_视觉资源系统程序开发规范.md](../开发文档/rules/03_视觉资源系统程序开发规范.md)：程序侧 `VisualID -> VisualAssetRegistry -> Unity Asset` 契约。
* [tools/美术工具/README.md](../tools/美术工具/README.md)：美术流水线脚本说明。

