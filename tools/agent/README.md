# 智能体开工检查

`tools/agent` 放开工前用的本地检查工具。它们只发现风险，不自动修改或清理文件。

## 开工检查

```powershell
python tools/agent/agent_health_check.py            # 完整检查；PowerShell 也可用 .\tools\agent\Invoke-AgentHealthCheck.ps1
python tools/agent/agent_health_check.py --brief    # 几行简报；Claude Code 开会话时由 .claude/settings.json 的 hook 自动运行
python tools/agent/agent_health_check.py --strict   # 警告也返回失败，适合提交前
```

完整检查包括：

- 当前分支和 HEAD。
- 工作区里已暂存、未暂存和未跟踪的文件。
- 容易误提交的路径：`_generated` 生成物、`StreamingAssets/Configs` 副本、本地工具目录、submodule。
- Skill 校验（`validate_skills.py`）：`.codex/skills` 每个 Skill 有唯一的 `name` 和 `description`；`.claude/skills` 的转发入口和源 frontmatter 一致、没有孤儿；Skill 文档里的链接、`.codex/skills/...` 路由和仓库路径存在；`AGENTS.md` 和 Skill 里提到的 `p3-*` Skill 名都已注册。结构问题和失效的 Skill 路由报 `ERROR`，其他失效路径和未知 Skill 名报 `WARN`。
- 游戏进度（`INFO`）：距最近一次游戏代码 / 配置提交的天数，以及工作区里未提交的游戏改动数。游戏路径是 `UnityClient/Assets` 和 `配置表(JSON)`，不含 `Art`、`Resources`、`StreamingAssets`、任意 `Editor` 目录和 `Scripts/*Acceptance`。超过 7 天没有游戏改动时，提示优先推进 `PROJECT_STATUS.md` 的当前里程碑。

简报（`--brief`）只输出分支、未提交改动数、游戏进度和 `PROJECT_STATUS.md` 里「现在」一节的前几行，总是返回 0。

结果：`PASS` 通过；`WARN` 可以继续，但提交前要收窄暂存范围、确认风险项；`ERROR` 必须先处理；`INFO` 只提供信号，不影响退出码。默认模式下 `WARN` 返回 0，`--strict` 下返回 1。

输出统一是 UTF-8；PowerShell 包装脚本会把控制台解码也设成 UTF-8，避免 Windows 默认 GBK 造成乱码。

回归测试：

```powershell
python -m unittest tools.agent.tests.test_agent_health_check tools.agent.tests.test_validate_skills -v
```

## Unity 测试的兼容入口

Editor 打开且有 Unity MCP 时，按 Skill `p3-program-validation` 用 `run_tests` 运行测试。下面两个脚本是没有 MCP 时的兼容入口，需要 Editor 已打开，并由 `AutoTestDaemon` 监听 `UnityClient/Logs/.test_trigger`：

- `Invoke-UnitySmokeTests.ps1`：触发少量背包 / 布局 smoke test，可用 `-Tests <Type>.Run` 指定。
- `Invoke-P0Validation.ps1`：串联配置同步、`ConfigValidationSmokeTest.Run`、核心 smoke、UI 规格校验和 ArtAcceptance latest 摘要，报告写到 `UnityClient/Logs/P0Validation/latest/`。`-SkipUnity` 只跑静态检查。

`p3-validation-core/` 是旧验证框架（ArtRun）的脚本，计划在 M1 随框架一起删除（见 `PROJECT_STATUS.md`）。

## Unity MCP 客户端配置

Codex 在用户级 `~/.codex/config.toml` 的 `[mcp_servers.unityMCP]` 里配置；Claude Code 用仓库根目录 `.mcp.json` 的 `unityMCP`。两者用同一条 `uvx --offline --from mcpforunityserver==10.0.0 mcp-for-unity --transport stdio` 命令。Claude Code 的项目级 MCP 需要在本机批准一次，批准记录写在不提交的 `.claude/settings.local.json`（`enabledMcpjsonServers`）。升级 `UnityClient/Packages/manifest.json` 里的 `com.coplaydev.unity-mcp` 时，同步两处的 `mcpforunityserver` 版本。
