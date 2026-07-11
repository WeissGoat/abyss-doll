---
id: dev_readme
title: 开发文档目录入口
type: dev
role: 程序
domain: program_docs_index
status: active
source_of_truth: true
related:
  - 开发文档/00_程序开发大纲.md
  - 开发文档/16_程序主流程闭环与架构收口推进计划.md
  - 开发文档/18_全局叙事播放系统开发方案.md
  - 开发文档/19_UnityMCP验收编排层设计.md
  - 开发文档/rules/README.md
  - 开发文档/archive/README.md
last_verified: 2026-07-12
update_rule: 开发文档目录结构、事实来源分层或归档规则变化时同步本文件。
---

# 开发文档目录入口

> 本目录是程序 / Unity 侧事实文档入口。后续 agent 开工时先读 `开发文档/00_程序开发大纲.md` 和 `开发文档/rules/README.md`，再进入具体系统文档。

## 当前分层

| 分层 | 目录 | 用途 |
|---|---|---|
| 程序总入口 | `开发文档/00_程序开发大纲.md` | 当前系统索引、开发口径、系统归属和正式纵切入口。 |
| 规范与约定 | `开发文档/rules/` | 架构、编码、UGUI、视觉资源、自动化测试等硬约束。 |
| 系统实现文档 | `开发文档/01-19*.md`、`开发文档/数据与实体定义/` | 当前仍 active 的系统设计、运行时链路、Validator、主流程闭环、架构收口、叙事播放和验收方案。 |
| 历史归档 | `开发文档/archive/` | 已被当前路线吸收或替代的早期评估、历史建议和旧方案。 |

## 必读顺序

1. `PROJECT_STATUS.md`：确认当前项目阶段和跨职能交接。
2. `agent_status/program.md`：确认程序侧最近完成、当前关注和阻塞项。
3. `开发文档/00_程序开发大纲.md`：确认系统归属和功能落点。
4. `开发文档/rules/00_程序开发总规则.md`：确认开工前设计卡、分层边界、配置 / Validator / 测试要求。
5. 当前系统对应的开发文档、GDD、配置 README 和 smoke test。

## 维护规则

- 新增程序硬约束、协作约定、Unity 表现层规范或自动化测试规范时，优先放入 `开发文档/rules/`。
- 新增当前系统实现方案、服务边界、配置字段、Validator 需求或验收方案时，放在 `开发文档/` 根目录或对应系统子目录。
- 被当前文档吸收、与正式纵切口径冲突、只保留历史价值的文档，移动到 `开发文档/archive/`，并把 front matter `status` 改为 `archived`。
- 移动文档后必须同步 `related`、跨目录引用、`知识库/views/program.md`、`agent_status/program.md`，并重新生成 / 校验知识库索引。
