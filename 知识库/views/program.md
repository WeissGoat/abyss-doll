---
id: kb_view_program
title: 程序智能体阅读入口
type: view
role: 程序
domain: agent_context_view
status: active
source_of_truth: false
related:
  - AGENTS.md
  - PROJECT_STATUS.md
  - agent_status/program.md
  - 知识库/README.md
  - 开发文档/00_程序开发大纲.md
  - 开发文档/16_程序主流程闭环与架构收口推进计划.md
  - 开发文档/rules/01_客户端分层与领域架构规范.md
  - 开发文档/rules/02_Unity表现层与编辑器构建规范.md
  - 开发文档/rules/00_程序开发总规则.md
  - tools/config/README.md
  - 配置表(JSON)/README.md
last_verified: 2026-05-23
update_rule: 程序入口文档、Unity 工程边界、配置同步或验证流程变化时同步本文件。
---

# 程序智能体阅读入口

> 本页是程序 / Unity 智能体的开工导航，不替代开发文档、代码或测试结果。

## 开工顺序

1. `PROJECT_STATUS.md`：确认当前阶段、总优先级、跨职能交接和阻塞项。
2. `agent_status/program.md`：确认程序侧当前关注、最近完成和下一步建议。
3. `开发文档/00_程序开发大纲.md`：确认系统归属和程序开发口径。
4. `开发文档/rules/01_客户端分层与领域架构规范.md`、`开发文档/rules/00_程序开发总规则.md`：确认架构和编码边界。
5. 当前任务涉及的系统开发文档、GDD、配置 README 和测试入口。

## 常用事实来源

| 任务类型 | 优先读取 |
|---|---|
| 程序系统归属 | `开发文档/00_程序开发大纲.md` |
| 当前程序推进顺序 | `开发文档/16_程序主流程闭环与架构收口推进计划.md` |
| 客户端架构 | `开发文档/rules/01_客户端分层与领域架构规范.md` |
| Unity 表现层 / 编辑器 | `开发文档/rules/02_Unity表现层与编辑器构建规范.md` |
| 编码规范 / 架构约定 | `开发文档/rules/00_程序开发总规则.md` |
| 配置同步 | `tools/config/README.md`、`配置表(JSON)/README.md` |
| UI / 视觉资源接入 | `开发文档/rules/03_视觉资源系统程序开发规范.md`、`美术文档/ui_design/README.md` |

## 边界提醒

- 运行时 UI 使用纯 UGUI，不重新引入 UI Toolkit、UXML、USS 或 `UIDocument` 运行时路径。
- 游戏规则写在后端或领域服务里，不写进 UI Controller。
- `配置表(JSON)` 是配置源；`UnityClient/Assets/StreamingAssets/Configs` 是生成副本，运行前先同步。
- 移动 Unity 资产时必须同步处理 `.meta` 文件，保留 GUID。
- 修改系统契约时优先补 Validator、测试和对应开发文档。
