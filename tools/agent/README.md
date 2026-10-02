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
  - 开发文档/19_UnityMCP验收编排层设计.md
  - 开发文档/20_UnityMCP验收编排层实现计划.md
  - 知识库/README.md
last_verified: 2026-10-02
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

P0 核心 Unity smoke 会覆盖背包交互、奖励系统、战斗战利品、深渊节点闭环、怪物行动、阶梯/楼层推进、小镇经济出售链路、工坊维护/成长链路、黄金路径 `MainFlowGoldenPathSmokeTest.Run` 和视觉资源登记；`Invoke-UnitySmokeTests.ps1` 的默认列表只保留少量可单独快速触发的背包/布局测试。

`report.json` 会单独输出 `ErrorCount`、`WarningCount`、`BlockedCount`、`LimitationCount`、`ValidationLimitations` 和 `SmokeTestRegistry`。例如 Unity Editor 未运行时，ConfigValidator / Unity smoke 会记录为 `Blocked` 与 `validation_limited:UnityEditorNotRunning`，不会混入 `ErrorCount` 冒充配置或代码错误；ArtAcceptance latest 的真实失败仍会保留为 `Failed`。

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

这是兼容路径：运行前需要 Unity Editor 已打开当前项目，并由 `AutoTestDaemon` 监听 `UnityClient/Logs/.test_trigger`。有 Unity MCP 时优先按 `p3-program-validation` 用 `run_tests` 运行。

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
- `tools/docs/Validate-Docs.ps1` 对应的知识库索引和 `related` 链接是否通过校验。
- `tools/agent/validate_skills.py` 的 Skill 校验：`.codex/skills` 每个 Skill 有唯一 `name` 和 `description`；`.claude/skills` 转发入口与源 frontmatter 一致且无孤儿；`tools/p3-mission` 与 `.codex/skills/p3-mission` 内容一致；Skill 文档里的链接、`.codex/skills/...` 路由和仓库路径存在；入口文档和 Skill 中出现的 `p3-*` Skill 名都已注册。结构问题和失效的 Skill 路由为 `ERROR`，其他失效仓库路径和未知 Skill 名为 `WARN`。单独运行：`python tools/agent/validate_skills.py`；回归测试：`python -m unittest tools.agent.tests.test_validate_skills -v`。
- 当前分支和 HEAD，方便复制智能体记录上下文。
- 游戏进度（`INFO`）：距最近一次游戏代码 / 配置提交的天数和工作区未提交的游戏改动数。游戏路径是 `UnityClient/Assets` 与 `配置表(JSON)`，不含 `Art`、`Resources`、`StreamingAssets`、任意 `Editor` 目录和 `Scripts/*Acceptance` 验收工具；超过 7 天没有游戏改动时提示回到当前优先级的玩家结果（`rules/02` 元工作预算）。回归测试：`python -m unittest tools.agent.tests.test_agent_health_check -v`。
- 背包交互服务、DisplaySpec 与 GridLayoutGroup 资产布局 smoke test 可通过 `Invoke-UnitySmokeTests.ps1` 单独执行。
- P0 统一验收可通过 `Invoke-P0Validation.ps1` 执行，并生成机器可读 JSON 与人工可读 Markdown 报告。

## 结果约定

- `PASS`：当前检查通过。
- `WARN`：可以继续工作，但提交前需要确认风险项并严格收窄暂存范围。
- `ERROR`：存在必须处理的问题，例如知识库校验失败。
- `INFO`：只提供信号，不影响结果和退出码。

默认模式下，`WARN` 返回成功退出码，避免阻断已有脏工作区中的正常任务。`-Strict` 模式下，`WARN` 也会返回失败退出码。

## Unity MCP 验收入口

程序与美术验收分别由 `.codex/skills/p3-program-validation` 和 `.codex/skills/p3-art-validation` 负责。`.test_trigger`、`Invoke-P0Validation.ps1` 和旧菜单仍保留为没有 MCP 时的兼容 / 故障回退入口。

- 程序自动化：smoke 测试由 `UnityClient/Assets/Editor/P3SmokeTests/` 包装成 Unity Test Framework 的 EditMode 用例，用 MCP `run_tests`（`assembly_names=["P3.SmokeTests.Editor"]`）运行，`get_test_job` 读结果；smoke set ID 是用例分类。
- 美术运行时检查：`tools/美术工具/check_art_binding.py` 做绑定检查，再在真实界面截图、Agent 评审并检查 Console，证据放 `UnityClient/Logs/P3ArtCheck/<RunID>/`。
- ArtRun 框架（`tools/agent/p3-validation-core/` 与 `p3_art_*` 工具）只用于 `t0_art_seal` 和 `art_regression`；`Merge-P3ReleaseEvidence.ps1` 只用于读取旧 RunID。
- 旧混合 `p3-validation`、`p3_run_unity_profile`、混合 Profile 和 `p3-release-validation` 已移除。

MCP 客户端配置：Codex 在用户级 `~/.codex/config.toml` 的 `[mcp_servers.unityMCP]` 中配置；Claude Code 使用仓库根目录 `.mcp.json` 的 `unityMCP`，两者使用同一条 `uvx --offline --from mcpforunityserver==10.0.0 mcp-for-unity --transport stdio` 命令。Claude Code 的项目级 MCP 需在本机批准一次，批准记录写在不提交的 `.claude/settings.local.json`（`enabledMcpjsonServers`）。升级 `UnityClient/Packages/manifest.json` 中的 `com.coplaydev.unity-mcp` 时，同步两处的 `mcpforunityserver` 版本。
