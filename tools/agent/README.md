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
  - 开发文档/15_P0配置Validator与自动验收底座需求.md
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

P0 配置 Validator 与自动验收底座使用：

```powershell
.\tools\agent\Invoke-P0Validation.ps1
```

该命令会串联配置同步、`ConfigValidationSmokeTest.Run`、核心 Unity smoke tests、UI 规格校验和 ArtAcceptance latest 摘要，并输出：

```text
UnityClient/Logs/P0Validation/latest/
  report.json
  report.md
  config_validation.json
  smoke_tests.json
  ui_validation.json
  art_acceptance_summary.json
```

P0 核心 Unity smoke 会覆盖背包交互、奖励系统、战斗战利品、深渊节点闭环、怪物行动、阶梯/楼层推进和视觉资源登记；`Invoke-UnitySmokeTests.ps1` 的默认列表只保留少量可单独快速触发的背包/布局测试。

快速静态检查可跳过 Unity：

```powershell
.\tools\agent\Invoke-P0Validation.ps1 -SkipUnity
```

提交前或候选版本门禁可使用严格模式，让 warning 也导致失败：

```powershell
.\tools\agent\Invoke-P0Validation.ps1 -Strict
```

默认不重跑 ArtAcceptance，只读取 `UnityClient/Logs/ArtAcceptance/latest/report.json` 并判断是否早于 active UI / art 规格。需要刷新截图时显式运行：

```powershell
.\tools\agent\Invoke-P0Validation.ps1 -SkipArtAcceptance:$false
```

背包表现与资产布局相关 Unity smoke test 可单独使用：

```powershell
.\tools\agent\Invoke-UnitySmokeTests.ps1
```

P0 配置 Validator 与自动验收底座的目标命令、报告格式和门禁要求见 `开发文档/15_P0配置Validator与自动验收底座需求.md`。

运行前需要 Unity Editor 已打开当前项目，并由 `AutoTestDaemon` 监听 `UnityClient/Logs/.test_trigger`。

默认会依次触发：

- `InventoryInteractionServiceSmokeTest.Run`
- `InventoryDisplaySpecSmokeTest.Run`
- `InventoryGridLayoutAssetValidatorTest.Run`

也可以指定测试名：

```powershell
.\tools\agent\Invoke-UnitySmokeTests.ps1 -Tests InventoryDisplaySpecSmokeTest.Run
```

## 当前检查项

- Git 工作区是否已有暂存、未暂存或未跟踪文件。
- 是否存在容易误提交的生成物、运行时副本、本地工具目录或 submodule 改动。
- `tools/docs/Validate-Docs.ps1` 对应的知识库索引和双向关系是否通过校验。
- 当前分支和 HEAD，方便复制智能体记录上下文。
- 背包交互服务、DisplaySpec 与 GridLayoutGroup 资产布局 smoke test 可通过 `Invoke-UnitySmokeTests.ps1` 单独执行。
- P0 统一验收可通过 `Invoke-P0Validation.ps1` 执行，并生成机器可读 JSON 与人工可读 Markdown 报告。

## 结果约定

- `PASS`：当前检查通过。
- `WARN`：可以继续工作，但提交前需要确认风险项并严格收窄暂存范围。
- `ERROR`：存在必须处理的问题，例如知识库校验失败。

默认模式下，`WARN` 返回成功退出码，避免阻断已有脏工作区中的正常任务。`-Strict` 模式下，`WARN` 也会返回失败退出码。
