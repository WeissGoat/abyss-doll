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
  - agent_status/art.md
  - 知识库/README.md
  - 美术文档/README.md
  - 美术文档/10_正式版核心纵切美术路线.md
  - 美术文档/00_美术流水线总览.md
  - 美术文档/ui_design/README.md
  - 美术文档/ui_design/ui_iteration_process.md
  - 美术文档/ui_design/formal_v1/screen_structure_review.md
  - 开发文档/09_视觉资源系统程序开发规范.md
  - 开发文档/14_Unity运行时美术自动验收方案.md
last_verified: 2026-05-24
update_rule: 美术入口文档、流水线、UI 交付或程序侧视觉契约变化时同步本文件。
---

# 美术智能体阅读入口

> 本页是美术 / UI 智能体的开工导航，不替代事实来源文档。具体规则以被链接文档为准。

## 开工顺序

1. `PROJECT_STATUS.md`：确认当前阶段、总优先级、跨职能交接和阻塞项。
2. `agent_status/art.md`：确认美术侧当前关注、最近完成和下一步建议。
3. `美术文档/README.md`：确认美术文档目录、推荐阅读顺序和生成物边界。
4. `美术文档/10_正式版核心纵切美术路线.md`：确认正式版 Alpha 美术和 UI 路线。
5. 当前任务涉及的 UI、Manifest、资源规格或运行时验收文档。

## 常用事实来源

| 任务类型 | 优先读取 |
|---|---|
| 美术路线 / Alpha 交付 | `美术文档/10_正式版核心纵切美术路线.md` |
| 美术流水线 / Manifest | `美术文档/00_美术流水线总览.md`、`美术文档/01_Manifest规范.md` |
| UI 设计交付 | `美术文档/ui_design/README.md`、`美术文档/ui_design/ui_iteration_process.md`、`美术文档/ui_design/handoff_checklist.md` |
| Formal V1 结构迭代 | `美术文档/ui_design/formal_v1/screen_structure_review.md`、`美术文档/ui_design/formal_v1/combat_hud_v1.md` |
| 程序接入契约 | `开发文档/09_视觉资源系统程序开发规范.md` |
| 运行时验收 | `开发文档/14_Unity运行时美术自动验收方案.md`、`美术文档/09_运行时美术验收记录.md` |

## 边界提醒

- `UnityClient/Assets/Art/Approved` 存放正式入库运行时美术资源。
- `UnityClient/Assets/Art/_IncomingAI` 是 AI 出图工作区，应保持忽略。
- `美术文档/_generated` 与 `美术文档/ui_design/_generated` 是脚本输出，优先通过工具刷新。
- 刷新 Manifest 前先同步配置：`.\tools\config\Sync-Configs.ps1 -Clean`。
- UI 结构迭代时，`screen_layouts.json` 是 active；`versions/` 是 baseline / 可选暂存，不是程序接入口。
- `combat_hud` 已进入 Formal V1 active 规格，第一批 `monster_*_combat` 战斗实体、脚底阴影和目标光环已入库；下一步交给 UI 程序接入并由美术侧截图验收。
- 如果美术交付影响程序接入或策划规则，需要同步 `PROJECT_STATUS.md` 和对应职能状态页。
