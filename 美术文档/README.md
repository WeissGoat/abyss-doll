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
  - 开发文档/09_视觉资源系统程序开发规范.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - 美术文档/13_正式纵切UI与素材覆盖矩阵.md
  - 美术文档/00_美术流水线总览.md
  - 美术文档/ui_design/README.md
  - agent_status/art.md
  - tools/美术工具/README.md
  - 知识库/views/art.md
last_verified: 2026-05-24
update_rule: 美术文档结构、推荐阅读顺序或外部契约变化时同步本文件。
---

# 美术文档索引

> **定位：** Project P3 美术 / UI 文档入口。本文件只负责导航和职责边界，不承载详细规格。
> **更新时间：** 2026-05-24

## 先看哪里

| 想确认什么 | 看哪个文档 | 说明 |
|---|---|---|
| 当前美术规划和优先级 | [10_正式版核心纵切美术路线.md](10_正式版核心纵切美术路线.md) | 美术侧当前路线的唯一规划入口。 |
| 当前哪些 UI 已覆盖、哪些还没进 active | [13_正式纵切UI与素材覆盖矩阵.md](13_正式纵切UI与素材覆盖矩阵.md) | 把 `09` 路线和 `11` 批次矩阵翻译成美术覆盖表。 |
| 端到端美术生产怎么走 | [00_美术流水线总览.md](00_美术流水线总览.md) | 只讲流程：需求发现、Manifest、提示词、出图、预处理、Approved、交接。 |
| UI 结构版本怎么管理 | [ui_design/README.md](ui_design/README.md) | UI 设计系统入口，说明 active / baseline / draft / handoff。 |
| 程序当前应接入哪些素材 | [_generated/可接入素材清单.md](_generated/可接入素材清单.md) | 程序侧只处理 `program_integrate` 队列。 |
| 当前美术状态和下一步 | [../agent_status/art.md](../agent_status/art.md) | 智能体交接状态页。 |

## 文档分层

### 规划层

| 文档 | 职责 |
|---|---|
| [10_正式版核心纵切美术路线.md](10_正式版核心纵切美术路线.md) | 当前美术规划、正式版原则、近期执行顺序。 |
| [13_正式纵切UI与素材覆盖矩阵.md](13_正式纵切UI与素材覆盖矩阵.md) | UI / 素材覆盖矩阵、active 界面、draft 界面和缺口队列。 |
| [../agent_status/art.md](../agent_status/art.md) | 当前事实状态、最近完成、下一步建议和阻塞。 |

### 工作流层

| 工作流 | 主入口 | 细节文档 |
|---|---|---|
| 资产生产流水线 | [00_美术流水线总览.md](00_美术流水线总览.md) | [01_Manifest规范.md](01_Manifest规范.md)、[02_资源规格与接入规范.md](02_资源规格与接入规范.md)、[03_AI生成与筛选规范.md](03_AI生成与筛选规范.md)、[04_美术风格基准.md](04_美术风格基准.md)、[05_AI图片网关接入方案.md](05_AI图片网关接入方案.md) |
| UI 设计版本流水线 | [ui_design/README.md](ui_design/README.md) | [ui_design/ui_iteration_process.md](ui_design/ui_iteration_process.md)、[ui_design/formal_v1/screen_structure_review.md](ui_design/formal_v1/screen_structure_review.md)、[ui_design/versions/migration_log.md](ui_design/versions/migration_log.md) |
| 运行时验收流水线 | [08_Unity运行时美术验收工具需求.md](08_Unity运行时美术验收工具需求.md) | [09_运行时美术验收记录.md](09_运行时美术验收记录.md)、[../开发文档/14_Unity运行时美术自动验收方案.md](../开发文档/14_Unity运行时美术自动验收方案.md) |

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
| [11_P0_UI骨架接入交付.md](11_P0_UI骨架接入交付.md) | P0 批次交付快照，具体接入仍以 active JSON 和 handoff 为准。 |
| [12_P1_UI骨架接入准备.md](12_P1_UI骨架接入准备.md) | P1 批次交付快照，具体接入仍以 active JSON 和 handoff 为准。 |
| [06_MVP素材接入状态同步.md](06_MVP素材接入状态同步.md) | 历史归档，不作为当前规划入口。 |
| [07_MVP_UI重新设计同步.md](07_MVP_UI重新设计同步.md) | 历史归档，不作为正式 UI 目标。 |

## 三套工作流

### UI 设计版本流

```text
baseline / 当前截图问题
  -> formal_v1 设计文档
  -> 用户确认
  -> active screen_layouts.json
  -> Validate-UIDesign.ps1
  -> 程序接入
  -> ArtAcceptance
```

程序只接 active `screen_layouts.json`。`formal_v1/*.md` 如果还没写入 active，只是设计草案，不是程序接入口。

### 资产生产流

```text
config / derived / preset
  -> art_manifest.json
  -> PromptCN / PromptEN / NegativePromptEN / Spec
  -> AI 生成
  -> 预处理
  -> 筛选
  -> Approved
  -> 可接入素材清单
```

`_IncomingAI` 是工作区，不进程序接入；`Approved` 是正式区。每次生成、预处理或同步 Approved 后，都要刷新 latest 可接入清单并留 snapshot。

### 程序接入与验收流

```text
可接入素材清单 program_integrate
  -> VisualAssetRegistry 登记
  -> UI / 配置绑定
  -> ArtAcceptance 截图
  -> 09_运行时美术验收记录.md
  -> 缺口回填到 seed / UI active / Manifest
```

## 当前状态

截至 2026-05-24：

* active Formal V1 UI 已覆盖 15 个界面，详见 [13_正式纵切UI与素材覆盖矩阵.md](13_正式纵切UI与素材覆盖矩阵.md)。
* `faction_shop`、`doll_interaction`、`scenario_event`、`doll_room` 已有 Formal V1 设计草案，但尚未进入 active `screen_layouts.json`，暂不作为程序接入口。
* 最新可接入素材清单显示 `program_integrate=17`，程序侧可按清单登记和接入。
* NovelAI 当前存在 Anlas 余额不足风险，local_v0 素材只用于先解锁程序接入和验收，后续需要替换为正式版。

## 机器生成文件

* [_generated/art_manifest.json](_generated/art_manifest.json)：机器可读 Manifest。
* [_generated/视觉资产Manifest.md](_generated/视觉资产Manifest.md)：脚本生成的 Manifest 摘要，方便快速查看。
* [_generated/AI绘图提示词清单.md](_generated/AI绘图提示词清单.md)：脚本补全后的提示词清单，供出图和审阅。
* [_generated/可接入素材清单.md](_generated/可接入素材清单.md)：当前可接入素材 latest，程序侧优先按其中 `program_integrate` 队列接入。
* [_generated/art_integration_snapshots/](_generated/art_integration_snapshots/)：每次生成、预处理或 Approved 同步后的可接入素材清单快照。
* [ui_design/_generated/ui_design_handoff.md](ui_design/_generated/ui_design_handoff.md)：UI 设计校验后生成的程序交付摘要。

生成命令：

```powershell
.\tools\美术工具\Update-ArtManifest.ps1
.\tools\美术工具\Generate-ArtPrompts.ps1
.\tools\美术工具\Validate-UIDesign.ps1
.\tools\美术工具\Generate-ArtIntegrationCandidates.ps1 -Snapshot -SnapshotTag manual_review
```

## 外部契约

* [版本规划/09_正式版核心纵切开发路线.md](../版本规划/09_正式版核心纵切开发路线.md)：当前正式版核心纵切的顶层路线，以此为准。
* [版本规划/10_正式版核心纵切版本节点规划.md](../版本规划/10_正式版核心纵切版本节点规划.md)：执行层节点规划。
* [版本规划/11_设计文档纵切批次矩阵.md](../版本规划/11_设计文档纵切批次矩阵.md)：设计文档批次与完成口径。
* [开发文档/09_视觉资源系统程序开发规范.md](../开发文档/09_视觉资源系统程序开发规范.md)：程序侧 `VisualID -> VisualAssetRegistry -> Unity Asset` 契约。
* [tools/美术工具/README.md](../tools/美术工具/README.md)：美术流水线脚本说明。

