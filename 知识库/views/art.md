---
id: kb_view_art
title: 美术智能体阅读入口
type: view
role: 美术
domain: agent_context_view
status: active
source_of_truth: false
related:
  - AGENTS.md
  - PROJECT_STATUS.md
  - rules/02_智能体任务路由与完成协议.md
  - 版本规划/09_正式版核心纵切开发路线.md
  - 版本规划/11_纵切批次与需求文档承接矩阵.md
  - agent_status/art.md
  - 知识库/README.md
  - 美术文档/README.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - 美术文档/13_正式纵切UI与素材覆盖矩阵.md
  - 美术文档/00_美术流水线总览.md
  - 美术文档/ui_design/README.md
  - 美术文档/ui_design/ui_iteration_process.md
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 美术文档/ui_design/formal_v2/README.md
  - 美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md
  - 开发文档/rules/03_视觉资源系统程序开发规范.md
  - 开发文档/14_Unity运行时美术自动验收方案.md
last_verified: 2026-10-01
update_rule: 美术入口文档、流水线、UI 交付或程序侧视觉契约变化时同步本文件。
---

# 美术智能体阅读入口

> 本页只负责美术 Role 的阅读顺序和上下文导航；稳定跨职能协议见 `rules/02_智能体任务路由与完成协议.md`，当前进度见 `agent_status/art.md`，专业事实以目标美术文档为准。

## 必读

1. `PROJECT_STATUS.md` 当前阶段、总优先级、跨职能交接和阻塞快照。
2. `agent_status/art.md` 当前关注、最近完成、下一步和阻塞。
3. `美术文档/README.md`。
4. `美术文档/10_正式版核心纵切美术路线.md`。

## 按任务读取

- 正式资产准入：读取 `p3-art-asset-production`、Manifest、资源规格、Approved 和目标 Asset Contract。
- 纯生成 / 差分：读取 `p3-generate-image` 和目标角色 / 场景 / 风格事实。
- 叙事 CG / 漫画页：读取 `p3-narrative-cg-comic`、`美术文档/18_CG底图与漫画式播放演出工作流.md` 和目标 CG 细案。
- UI / 运行时验收：读取 active `screen_layouts.json`、程序交接清单和 `p3-art-validation`；不把静态预览当作运行时证据。
- Owner 统筹任务：只读取实际受影响的策划、程序或剧情入口。

## 验收 / 恢复时

- 正式资产准入使用 `p3-art-asset-production`；发布聚合使用 `p3-release-validation`。
- 运行时视觉诊断、截图和受限迭代使用 `p3-art-validation`：默认先用 MCP 直接查看 Game View 和 bounded UGUI snapshot，需要决策证据时才用 capture ticket 截图；Art Profile 只记录技术结果与证据，`ArtAcceptanceRunner` 仅由 `art_regression` 使用。
- Owner 外部验收、状态回写或跨职能交接时读取 `rules/02_智能体任务路由与完成协议.md`。
- 恢复历史候选或旧 ArtRun 时只读取目标 Asset / RunID 证据，不通读美术状态历史。

## 常用事实来源

| 任务类型 | 优先读取 |
|---|---|
| 版本规划对齐 / 当前优先级 | `版本规划/09_正式版核心纵切开发路线.md`、`版本规划/11_纵切批次与需求文档承接矩阵.md` |
| 美术路线 / 纵切交付 | `美术文档/10_正式版核心纵切美术路线.md` |
| 美术流水线 / Manifest | `美术文档/00_美术流水线总览.md`、`美术文档/01_Manifest规范.md` |
| 新增美术需求候选审查 | `美术文档/_generated/美术需求候选清单.md`、`美术文档/art_requirements_seed.json` |
| UI 设计交付 | `美术文档/ui_design/README.md`、`美术文档/ui_design/ui_iteration_process.md`、`美术文档/ui_design/handoff_checklist.md` |
| Formal V1 结构迭代 | `美术文档/ui_design/formal_v1/screen_structure_review.md`、`美术文档/ui_design/formal_v1/combat_hud_v1.md` |
| Formal V2 UX/UI 重构 | `美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md`、`美术文档/ui_design/formal_v2/README.md` |
| 程序接入契约 | `开发文档/rules/03_视觉资源系统程序开发规范.md` |
| 运行时验收 | `开发文档/14_Unity运行时美术自动验收方案.md`、`美术文档/09_运行时美术验收记录.md` |

## 边界提醒

- `UnityClient/Assets/Art/Approved` 存放正式入库运行时美术资源。
- `UnityClient/Assets/Art/_IncomingAI` 是 AI 出图工作区，应保持忽略。
- `美术文档/_generated` 与 `美术文档/ui_design/_generated` 是脚本输出，优先通过工具刷新。
- 刷新 Manifest 前先同步配置：`.\tools\config\Sync-Configs.ps1 -Clean`。
- 新增策划 / 配置 / UI 文档后，先运行 `.\tools\美术工具\Scan-ArtRequirementCandidates.ps1` 审查候选；确认后再写 `art_requirements_seed.json` 或等待正式配置字段落地。
- UI 结构迭代时，`screen_layouts.json` 是 active；`formal_v1/`、`formal_v2/` 和 `versions/` 都不是程序接入口。
- Figma、Unity MCP、截图标注和 PlayMode 布局扫描只是 Formal V2 的辅助工具；正式接入口仍必须回到 active `screen_layouts.json`、Manifest、程序交接清单和 ArtAcceptance。
- 如果美术交付影响程序接入或策划规则，需要同步 `PROJECT_STATUS.md` 和对应职能状态页。
