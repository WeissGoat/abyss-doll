---
id: kb_view_pm
title: PM 智能体阅读入口
type: view
role: PM
domain: agent_context_view
status: active
source_of_truth: false
related:
  - AGENTS.md
  - PROJECT_STATUS.md
  - agent_status/pm.md
  - 知识库/README.md
  - 版本规划/09_正式版核心纵切开发路线.md
  - 版本规划/10_正式版核心纵切版本节点规划.md
last_verified: 2026-05-24
update_rule: PM 入口文档、版本路线、里程碑或跨职能交接口径变化时同步本文件。
---

# PM 智能体阅读入口

> 本页是 PM / 版本规划智能体的开工导航，不替代路线、GDD、开发文档、美术文档或状态页。

## 开工顺序

1. `PROJECT_STATUS.md`：确认当前阶段、顶层目标、总优先级、跨职能交接和阻塞项。
2. `agent_status/pm.md`：确认 PM 侧当前判断、最近完成和下一步建议。
3. `版本规划/09_正式版核心纵切开发路线.md`：确认正式版核心纵切的阶段边界、系统优先级和里程碑。
4. `版本规划/10_正式版核心纵切版本节点规划.md`：确认当前版本节点、三线交付和验收顺序。
5. 按当前工作包读取 `agent_status/design.md`、`agent_status/program.md`、`agent_status/art.md`。
6. 进入具体系统事实来源：GDD、开发文档、美术路线、配置 README 或数值模型。

## 常用事实来源

| 任务类型 | 优先读取 |
|---|---|
| 项目阶段 / 总优先级 | `PROJECT_STATUS.md` |
| PM 状态 / 下一步建议 | `agent_status/pm.md` |
| 顶层路线 / 里程碑 | `版本规划/09_正式版核心纵切开发路线.md` |
| 版本节点 / 三线交付 | `版本规划/10_正式版核心纵切版本节点规划.md` |
| 系统规则落点 | `设计文档/GDD_00_系统关联总图.md` 和对应 `GDD_*.md` |
| 程序落地边界 | `开发文档/00_程序开发大纲.md` 和对应系统开发文档 |
| 美术 / UI 交付 | `美术文档/10_正式版核心纵切美术路线.md`、`美术文档/ui_design/README.md` |
| 配置源和数值假设 | `配置表(JSON)/README.md`、`数值模型设计/00_基准价值与空间本位模型.md` |

## 边界提醒

- PM 管理版本规划，不直接替代策划规则、程序实现或美术交付。
- 阶段目标、系统优先级或里程碑变化，必须同步 `09` 路线和 `PROJECT_STATUS.md`。
- 具体系统规则变化，必须同步对应 GDD；实现契约变化同步开发文档；视觉交付变化同步美术文档。
- 每个纵切工作包都应写清目标、范围外、涉及事实来源、职能交接和验收方式。
- 当前旧 MVP 文档是历史资料；如果与当前 GDD 或 `09` 路线冲突，以当前 GDD 和 `09` 路线为准。

