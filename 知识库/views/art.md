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
last_verified: 2026-05-29
update_rule: 美术入口文档、流水线、UI 交付或程序侧视觉契约变化时同步本文件。
---

# 美术智能体阅读入口

> 本页是美术 / UI 智能体的开工导航，不替代事实来源文档。具体规则以被链接文档为准。

## 开工顺序

1. `PROJECT_STATUS.md`：确认当前阶段、总优先级、跨职能交接和阻塞项。
2. `agent_status/art.md`：确认美术侧当前关注、最近完成和下一步建议。
3. `美术文档/README.md`：确认美术文档目录、推荐阅读顺序和生成物边界。
4. `美术文档/10_正式版核心纵切美术路线.md`：确认正式版纵切美术和 UI 路线。
5. 当前任务涉及的 UI、Manifest、资源规格或运行时验收文档。

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
- P5 UI / 美术只服务当前 P0-P4 功能纵切，不以横向铺满所有界面为近期目标。
- 当前 active Formal V1 已覆盖 21 个界面；Formal V2 已进入 UX/UI draft 设计层，V2-A 五个核心界面 `workshop_main`、`combat_hud`、`inventory_loot`、`dungeon_map`、`settlement` 已有可评审草案。程序侧仍只按 active `screen_layouts.json` 接入。
- Figma、Unity MCP、截图标注和 PlayMode 布局扫描只是 Formal V2 的辅助工具；正式接入口仍必须回到 active `screen_layouts.json`、Manifest、程序交接清单和 ArtAcceptance。
- `combat_hud` Formal V1 已完成程序接入和 ArtAcceptance；当前程序侧优先按 `美术文档/_generated/可接入素材清单.md` 的 `program_integrate` 队列登记剩余 Approved 素材，美术侧随后做截图验收。
- 如果美术交付影响程序接入或策划规则，需要同步 `PROJECT_STATUS.md` 和对应职能状态页。

# 2026-07-12 验收入口

运行时美术诊断、截图和受限迭代入口为 `.codex/skills/p3-art-validation`。所有 Art Profile 均要求外部主美复核，机器结果不能直接封板。
