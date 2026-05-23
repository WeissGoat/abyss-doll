---
id: agent_health_check
title: 智能体开工健康检查
type: tool
role: 全局
domain: agent_workflow
status: active
source_of_truth: true
related:
  - AGENTS.md
  - PROJECT_STATUS.md
  - 知识库/README.md
last_verified: 2026-05-24
update_rule: 调整复制智能体开工检查项、风险路径或提交前检查流程时同步本文件。
---

# 智能体开工健康检查

`tools/agent` 存放复制智能体开工前使用的本地检查工具。当前目标是先发现风险，不自动修改或清理文件。

## 使用方式

```powershell
.\tools\agent\Invoke-AgentHealthCheck.ps1
```

提交前或自动化场景可使用严格模式，让警告也返回失败：

```powershell
.\tools\agent\Invoke-AgentHealthCheck.ps1 -Strict
```

## 当前检查项

- Git 工作区是否已有暂存、未暂存或未跟踪文件。
- 是否存在容易误提交的生成物、运行时副本、本地工具目录或 submodule 改动。
- `tools/docs/Validate-Docs.ps1` 对应的知识库索引和双向关系是否通过校验。
- 当前分支和 HEAD，方便复制智能体记录上下文。

## 结果约定

- `PASS`：当前检查通过。
- `WARN`：可以继续工作，但提交前需要确认风险项并严格收窄暂存范围。
- `ERROR`：存在必须处理的问题，例如知识库校验失败。

默认模式下，`WARN` 返回成功退出码，避免阻断已有脏工作区中的正常任务。`-Strict` 模式下，`WARN` 也会返回失败退出码。
