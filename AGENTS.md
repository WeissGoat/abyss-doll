# Project P3 · 智能体入口

> 所有 Agent（Claude Code、Codex 等）开工先读这一页和 `PROJECT_STATUS.md`。其余文档按任务需要再查，不必通读。

## 这是什么游戏

日系二次元地底奇幻。玩家照看一个脆弱的人偶「零号」，带她一次次潜入深渊：在背包格子里战斗，决定走哪条路、捡什么、何时撤；回到小镇出售、维修、成长，再为了房租和下一个目标继续下潜。

主循环：工坊 / 小镇 → 下潜（节点地图）→ 战斗（背包格子、AP、怪物意图）→ 战利品取舍 → 撤离或深入 → 回城出售、维修、成长 → 再下潜。

当前目标是 1–2 小时的 Demo。范围、里程碑、下一步和已知问题都在 `PROJECT_STATUS.md`。

## 开工

1. 读 `PROJECT_STATUS.md`。
2. 看 `git status --short` 和 `git branch --show-current`：可能有用户或其他会话的改动，也可能有别的会话在同一个工作树里切过分支。
3. 按任务查需要的文档和代码（见下方"去哪找"）。

## 怎么跑、怎么测

- Unity 2022.3.60f1，工程在 `UnityClient/`，只有一个场景 `Assets/Scenes/SampleScene.unity`，生命周期入口是 `GameRoot`。
- Unity MCP（`.mcp.json` 里的 `unityMCP`）在 Editor 打开时可用：`refresh_unity` 编译，`read_console` 看报错，`run_tests` / `get_test_job` 跑测试，`manage_editor` 进出 Play Mode，`manage_camera` 截图。
- 自动测试：Skill `p3-program-validation`。EditMode（程序集 `P3.SmokeTests.Editor`，`Scripts/Tests/*Test.Run`）测规则；PlayMode（`Assets/Tests/PlayMode/P3PlayTests` 跑 `Scripts/Tests/PlayMode/*PlayScenario.Run`，真实帧）测时序和拖拽这类要跑起来才会出的问题。Editor 关着时可以用命令行跑：`Unity.exe -batchmode -projectPath UnityClient -runTests -testPlatform EditMode|PlayMode -testResults <xml>`（Editor 开着会因为工程被占用而退出）。
- 看游戏实际画面：Skill `p3-playtest`。游戏里的操作用 `P3DevTools`（一行调用、返回文字报告，例如 `return P3DevTools.Describe();`）；游戏内按 F1 是同一套功能的开发者面板。遮挡用文字查：`Occlusion()` 列出被别的东西盖住的物品和按钮，`DragCheck()` 把物品拿到背包上方看有没有被挡。
- 驱动打包版：`python tools/agent/p3cmd.py launch` 以 `-p3cmd` 启动最新的包，之后 `python tools/agent/p3cmd.py <P3DevTools 方法或 screenshot / wait / quit>`，和 MCP 驱动 Editor 一样一行一条命令。
- 交给别的智能体做的检查（打包版走查、自检复跑、模拟扫参、日志排查、一致性检查）：用户级 Skill `delegate`，在本仓库用 `python tools/agent/delegate.py <名字> <任务说明.md>`（带上 `tools/agent/delegate_rules.md`）。不写 `--profile <壳/模型>` 时用 `codex/deepseek`，任务需要时再指定别的，例如 `codex/gemini`；逗号分隔可写备选链，例如 `codex/deepseek,codex/gemini`。Hermes 没有只读，只读任务会跳过它；只回报告到 `tmp/agent_reports/`，运行期间工作区有改动会附 patch 警告。只读任务加 `--mode read`；驱动打包版加 `--lock build`；同一时间只让一个智能体操作 Editor。
- 打包：Editor 开着时用菜单 `Tools/P3/打包 Windows 试玩版`（或 MCP `return P3BuildTools.BuildWindows(false);`），Editor 关着时用 `.\tools\build\Build-Windows.ps1`。输出在 `Builds/Windows/`（不进 git）。打完用 `.\tools\build\Test-WindowsBuild.ps1` 自检（包自己从标题开新游戏、玩完序章、再跑 3 次下潜和夜间营业；`P3DevTools.PlayPrologue()` 在 Editor 里也能一行玩完序章），再用 `Pack-WindowsBuild.ps1` 压缩。
- 出图：通用方法（找网关、选后端、串行出候选、拼图评审）用用户级 Skill `art-gen`；网关是 submodule `tools/ai-image-gateway`，配置 `config.local.yaml` 不进 git；正式资产按 `p3-art` 入库。
- 改了 `配置表(JSON)/` 后先同步：`.\tools\config\Sync-Configs.ps1 -Clean`。加改内容见 Skill `p3-content`。
- Editor 没开时，写明 `validation_limited:unity_editor_not_running`，能做的静态检查照做。
- 开工检查：`python tools/agent/agent_health_check.py`（分支、改动、距上次游戏改动的天数、Skill 校验）。

## 代码约定

- 领域逻辑是纯 C#，由 `CoreBackend` 统一提供（`Combat`、`Dungeon`、`Inventory`、`Items`、`Workshop`、`Narrative` 等目录），不依赖 MonoBehaviour。
- UI Controller 只显示状态、转发玩家请求；规则不写进 UI。后端通过事件总线（`GameEventBus`、`CombatEventBus`、`DungeonEventBus`）和视觉队列（`VisualQueue`）通知界面。
- 内容和数值走配置，不在代码里写死。
- 详细架构：`开发文档/rules/01_客户端分层与领域架构规范.md`。

## 去哪找

| 想知道 | 看 |
|---|---|
| 当前目标、里程碑、已知问题 | `PROJECT_STATUS.md` |
| 游戏系统规则 | `设计文档/GDD/`（先看 `GDD_00_系统关联总图.md`）、`设计文档/规则卡/` |
| 剧情和序章演出 | `设计文档/剧情/`、`版本规划/0-12小时细案/` |
| 全游戏远期蓝图 | `版本规划/13_正式版全局体验总线与开放节奏.md`（Demo 范围以外只作参考） |
| 数值基准 | `数值模型设计/` |
| 程序结构 | `开发文档/00_程序开发大纲.md`、`开发文档/rules/` |
| 配置字段 | `配置表(JSON)/README.md` 和各子目录 README |
| 美术规格和流水线 | `美术文档/README.md`、`tools/美术工具/README.md` |
| 设计 spec 和历史方案 | `docs/specs/`、`docs/plans/` |
| 旧工作流（角色、执行道、分角色状态页） | `docs/archive/workflow-2026-10/`，只作历史参考 |

## 硬边界

下面七条防的是真实损坏，任何情况下都不偏离：

1. `配置表(JSON)/` 是配置源；`UnityClient/Assets/StreamingAssets/Configs` 是同步副本，不手改。原因：每次同步都会覆盖它。
2. 运行时 UI 只用 UGUI，不引入 UI Toolkit、UXML、USS 或 `UIDocument`；游戏规则不写进 UI Controller。原因：界面和测试都按 UGUI 搭建，写在 UI 里的规则测不到、也没法复用。
3. 运行时只引用 `UnityClient/Assets/Art/Approved`；`_IncomingAI` 是不进 git 的工作区。原因：引用 `_IncomingAI` 的东西在别的机器和构建里会丢。
4. `_generated` 目录由脚本生成；除非任务就是修复生成物，否则通过对应脚本刷新。原因：手改会在下次生成时被覆盖，也会和源数据对不上。
5. `tools/ai-image-gateway` 是 submodule，别的项目也在用；除非任务就是它，否则不在父仓库提交它内部的改动。
6. 移动、复制、重命名 Unity 资产时连同 `.meta` 一起处理，保留 GUID。原因：Prefab、场景和 Registry 都按 GUID 引用资源。
7. 工作区里可能有用户或其他会话的改动：不回滚、不覆盖、不顺手清理，只暂存本任务的文件。原因：多个会话共用这个仓库。

## 完成标准

- 代码改动：编译通过，这批相关的测试通过（`run_tests` 按名字只跑相关的几个）。全量测试、跑局模拟和打包放到一组批次的末尾一起跑，不新增失败（已知失败列在 `PROJECT_STATUS.md`）。
- 玩家看得见的改动：一组批次（例如 M2 剩下的几批）全部做完后，打一次包，用打包自检验证（`Test-WindowsBuild.ps1`，新流程要加进 `P3BuildSmoke`），只看一张最相关的截图（640 宽）确认布局，然后交给制作人试玩。批次中间不进 Play Mode、不截图；只有自检失败需要排查、或制作人反馈了画面问题时才一步步走查。手感和体验以制作人试玩为准。
- 如实报告：没验证的写明没验证和原因，不把"写完了"说成"能用了"。

## 工作方式

- 一组批次用一个分支，每批一到几个提交；整组做完、自检通过、制作人确认后再 fast-forward 合并到 `main`。不主动 push。批次之间不停下来等确认，除非遇到需要制作人拍板的设计问题。
- 状态只记在 `PROJECT_STATUS.md` 一处：做完或发现问题时更新对应的行，历史交给 git log。
- 需要先设计的功能，写一页 spec 放进 `docs/specs/`；不再新增规划层或第二份进度表。
- 设计文档是参考，不是合同。实现中发现设计不合理，直接提出来，并修改设计文档本身。
- 本页之外的做法都是默认做法，可以按情况偏离，但要在最终回复里说明偏离了什么、为什么。

## Skill

Skill 的源文件在 `.codex/skills/<name>/SKILL.md`；`.claude/skills/<name>/SKILL.md` 只是给 Claude Code 的转发入口。增删、改名或修改 frontmatter 时同步两处，`python tools/agent/validate_skills.py` 会检查。

| Skill | 用途 |
|---|---|
| `p3-playtest` | 进 Play Mode 看游戏实际画面：到达指定界面、截图、读日志、检查登记的美术素材是否正确上屏。 |
| `p3-program-validation` | 编译、自动测试、配置同步和 UI 规格检查。 |
| `p3-content` | 通过配置加改物品、怪物、掉落、地图层、成长和经济数值，并校验。 |
| `p3-art` | 美术生产：生成 → 挑选 → 入库（Approved、Unity、Registry），包括角色立绘和叙事 CG。 |
| `p3-mission` | 可选：跨会话的长任务队列（`.mission/*.csv`，只在本地，不提交）。 |
