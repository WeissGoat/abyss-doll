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
  - rules/02_智能体任务路由与完成协议.md
  - agent_status/program.md
  - 知识库/README.md
  - 开发文档/00_程序开发大纲.md
  - 开发文档/16_程序主流程闭环与架构收口推进计划.md
  - 开发文档/19_UnityMCP验收编排层设计.md
  - 开发文档/20_UnityMCP验收编排层实现计划.md
  - 开发文档/rules/01_客户端分层与领域架构规范.md
  - 开发文档/rules/02_Unity表现层与编辑器构建规范.md
  - 开发文档/rules/00_程序开发总规则.md
  - tools/config/README.md
  - 配置表(JSON)/README.md
last_verified: 2026-10-01
update_rule: 程序入口文档、Unity 工程边界、配置同步或验证流程变化时同步本文件。
---

# 程序智能体阅读入口

> 本页只负责程序 Role 的阅读顺序和上下文导航；稳定跨职能协议见 `rules/02_智能体任务路由与完成协议.md`，当前进度见 `agent_status/program.md`，专业事实以开发文档、代码和测试结果为准。纯 UGUI 与 UI 程序接入属于程序专业范围。

## 必读

1. `PROJECT_STATUS.md` 当前阶段、总优先级、跨职能交接和阻塞快照。
2. `agent_status/program.md` 当前关注、最近完成、下一步和阻塞。
3. `开发文档/00_程序开发大纲.md`。
4. `开发文档/rules/01_客户端分层与领域架构规范.md`、`开发文档/rules/00_程序开发总规则.md`。

## 按任务读取

- 当前系统：读取目标系统开发文档、相关 GDD、配置 README 和测试入口。
- UI / 视觉资源接入：读取 `开发文档/rules/03_视觉资源系统程序开发规范.md` 和对应美术 View。
- 叙事播放：读取 `开发文档/18_全局叙事播放系统开发方案.md`、Narrative 配置 README 和目标剧情事实。
- Owner 统筹任务：只读取 Owner View 指定的受影响专业入口，不默认加载全部状态页。

## 验收 / 恢复时

- 编译、配置、Smoke、P0 / T0 功能路径使用 `p3-program-validation`（共享脚本在 `tools/agent/p3-validation-core`）；不以 ArtAcceptance 或静态截图代替程序通过，美术 lane 的注册目标检查、bounded UGUI inspection、MCP capture ticket 和 ArtRunID 不进入程序 Profile。
- Owner 外部验收、状态回写或跨职能交接时读取 `rules/02_智能体任务路由与完成协议.md`。
- Unity / MCP 受限时记录 `validation_limited:*`，并回到对应 RunID 证据包或 Git 历史追溯。

## 常用事实来源

| 任务类型 | 优先读取 |
|---|---|
| 程序系统归属 | `开发文档/00_程序开发大纲.md` |
| 当前程序推进顺序 | `开发文档/16_程序主流程闭环与架构收口推进计划.md` |
| MCP-first 验收编排 | `开发文档/19_UnityMCP验收编排层设计.md` |
| MCP-first 验收实现任务 | `开发文档/20_UnityMCP验收编排层实现计划.md` |
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
