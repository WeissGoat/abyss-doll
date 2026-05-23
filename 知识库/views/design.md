---
id: kb_view_design
title: 策划智能体阅读入口
type: view
role: 策划
domain: agent_context_view
status: active
source_of_truth: false
related:
  - AGENTS.md
  - PROJECT_STATUS.md
  - agent_status/design.md
  - 知识库/README.md
  - 设计文档/GDD_00_系统关联总图.md
  - 版本规划/09_正式版核心纵切开发路线.md
  - 数值模型设计/00_基准价值与空间本位模型.md
  - 配置表(JSON)/README.md
  - 开发文档/00_程序开发大纲.md
last_verified: 2026-05-23
update_rule: 策划入口文档、GDD、配置源、数值模型或路线变化时同步本文件。
---

# 策划智能体阅读入口

> 本页是策划 / 数值智能体的开工导航，不替代 GDD、配置源或数值模型。

## 开工顺序

1. `PROJECT_STATUS.md`：确认当前阶段、总优先级、跨职能交接和阻塞项。
2. `agent_status/design.md`：确认策划侧当前关注、最近完成和下一步建议。
3. `版本规划/09_正式版核心纵切开发路线.md`：确认当前顶层路线。
4. `设计文档/GDD_00_系统关联总图.md`：确认系统关系和规则入口。
5. 当前任务涉及的 `设计文档/GDD_*.md`、`数值模型设计/` 或 `配置表(JSON)/*/README.md`。

## 常用事实来源

| 任务类型 | 优先读取 |
|---|---|
| 顶层阶段 / 纵切路线 | `版本规划/09_正式版核心纵切开发路线.md` |
| 系统关系 / GDD 入口 | `设计文档/GDD_00_系统关联总图.md` |
| 数值基准 | `数值模型设计/00_基准价值与空间本位模型.md` |
| 配置意图 / 表结构 | `配置表(JSON)/README.md` 和对应子目录 README |
| 程序落地边界 | `开发文档/00_程序开发大纲.md` 和对应系统开发文档 |
| 视觉表达需求 | `美术文档/10_正式版核心纵切美术路线.md` |

## 边界提醒

- `配置表(JSON)` 是配置版本源；不要把 Unity 运行时副本当成策划源数据。
- `UnityClient/Assets/StreamingAssets/Configs` 是脚本生成副本，进入 Unity 或自动化验证前由 `.\tools\config\Sync-Configs.ps1 -Clean` 刷新。
- 系统规则变化必须回写对应 GDD；配置字段或意图变化必须同步配置 README。
- 如果策划变更影响程序实现或美术表达，需要同步 `PROJECT_STATUS.md` 和目标职能状态页。
