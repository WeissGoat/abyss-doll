---
id: agents_entry
title: Project P3 智能体入口
type: entry
role: 全局
domain: agent_workflow
status: active
source_of_truth: true
related:
  - GEMINI.md
  - agent_status/README.md
  - agent_status/pm.md
  - PROJECT_STATUS.md
  - 知识库/README.md
  - 知识库/views/pm.md
  - 知识库/views/art.md
  - 知识库/views/design.md
  - 知识库/views/program.md
  - tools/agent/README.md
  - 版本规划/09_正式版核心纵切开发路线.md
  - 版本规划/README.md
  - 版本规划/11_设计文档纵切批次矩阵.md
last_verified: 2026-05-24
update_rule: 修改智能体分工、开工流程或完成协议时同步本文件。
---

# Project P3 智能体入口

> 复制出来的智能体开工前先读这里。不要先改代码、配置、美术流水线或设计文档。

## 当前事实来源

1. 先读 `PROJECT_STATUS.md`，确认项目总目标、当前阶段、跨职能交接和阻塞项。
2. 再按任务职能读取对应状态页：
   - PM / 版本规划：`agent_status/pm.md`
   - 美术 / UI：`agent_status/art.md`
   - 策划 / 数值：`agent_status/design.md`
   - 程序 / Unity：`agent_status/program.md`
3. 需要快速定位上下文时，读取对应职能视图：
   - PM / 版本规划：`知识库/views/pm.md`
   - 美术 / UI：`知识库/views/art.md`
   - 策划 / 数值：`知识库/views/design.md`
   - 程序 / Unity：`知识库/views/program.md`
4. `GEMINI.md` 是项目知识库和上下文路由。
5. `版本规划/09_正式版核心纵切开发路线.md` 是当前开发核心与顶层路线。所有职能的阶段判断、优先级和是否偏离当前目标，先以 `09` 为准。
6. `版本规划/11_设计文档纵切批次矩阵.md` 当前标题为“设计文档纵切批次矩阵”，说明 `设计文档/GDD_00` 到 `GDD_12` 的系统批次、当前依赖和正式完成口径。
7. `版本规划/README.md` 是版本规划目录入口，说明当前 active 文档、归档文档和职责边界。

如果旧 MVP 文档与当前 GDD、`09` 路线或状态页冲突，以当前 GDD、`09` 路线和状态页为准。

当前开发文档关系：

```text
09：当前开发核心，定义正式版纵切顶层路线、系统优先级和阶段边界
  ↓
11：GDD 纵切批次矩阵，定义每份设计文档何时进入纵切批次以及按什么正式口径完成
```

## 开工健康检查

复制出来的智能体开始实际改动前，建议先运行：

```powershell
.\tools\agent\Invoke-AgentHealthCheck.ps1
```

它只报告风险，不会清理或修改文件。提交前可以使用严格模式：

```powershell
.\tools\agent\Invoke-AgentHealthCheck.ps1 -Strict
```

## 角色

> 本区提供基础职能边界。对应职能智能体完成任务后，可以继续补充和迭代自己的工作细节。

| 角色 | 工作范围 | 需要关注的文件 | 需要修改的文件 | 完成后回写 |
|---|---|---|---|---|
| PM / 版本规划智能体 | 版本路线、阶段判断、里程碑拆分、优先级排序、跨职能交接、完成标准和状态同步 | `PROJECT_STATUS.md`、`版本规划/README.md`、`版本规划/09_正式版核心纵切开发路线.md`、`版本规划/11_设计文档纵切批次矩阵.md`、`agent_status/pm.md`、三职能状态页、当前系统事实来源 | `版本规划/`、`PROJECT_STATUS.md`、`agent_status/pm.md`、必要的职能状态和交接文档 | `agent_status/pm.md`，影响项目阶段或交接时同步 `PROJECT_STATUS.md` |
| 美术智能体 | 视觉流水线、Manifest、AI 素材筛选、正式资源入库、UI 结构版本迭代、UI 视觉交付、运行时美术验收 | `美术文档/README.md`、`美术文档/00_美术流水线总览.md`、`美术文档/10_正式版核心纵切美术路线.md`、`美术文档/ui_design/README.md`、`美术文档/ui_design/ui_iteration_process.md`、`美术文档/ui_design/formal_v1/screen_structure_review.md`、`开发文档/09_视觉资源系统程序开发规范.md` | `美术文档/`、`美术文档/ui_design/`、`UnityClient/Assets/Art/Approved`、必要的美术状态和交接文档 | `agent_status/art.md` |
| 程序智能体 | Unity 客户端、C# 架构、CoreBackend、UGUI、Validator、测试、编辑器自动化 | `开发文档/00_程序开发大纲.md`、`开发文档/00_客户端核心架构规范.md`、`开发文档/00_Unity表现层与编辑器构建规范.md`、`开发文档/12_程序开发优化建议与重构路线.md`、`开发文档/13_编程规范与架构约定.md` | `UnityClient/Assets/Scripts`、`UnityClient/Assets/Tests`、`UnityClient/Assets/Editor`、`开发文档/`、必要的配置同步和验证脚本 | `agent_status/program.md` |
| UI 程序智能体 | UGUI 表现层、Prefab、VisualID 绑定、运行时美术验收 | UI 设计交付、视觉资源契约、表现层架构、当前 UI 代码 | `UnityClient/Assets/Scripts/UI`、`UnityClient/Assets/Prefabs`、必要的开发文档 | `agent_status/program.md`，有美术交接时同步 `agent_status/art.md` |
| 策划智能体 | GDD 规则、经济循环、物品生命周期、深渊节奏、数值假设、配置意图 | `设计文档/GDD_00_系统关联总图.md`、`版本规划/09_正式版核心纵切开发路线.md`、`数值模型设计/00_基准价值与空间本位模型.md`、当前任务涉及的 `设计文档/GDD_*.md` 和配置 README | `设计文档/`、`数值模型设计/`、`配置表(JSON)/`、`版本规划/`、必要的策划状态和交接文档 | `agent_status/design.md` |
| 知识库智能体 | 文档元数据、索引、校验脚本、知识库规范、职能阅读入口 | `知识库/README.md`、`知识库/views/`、`DOCS_INDEX.md`、`docs_index.json`、核心入口文档 | `AGENTS.md`、`PROJECT_STATUS.md`、`知识库/`、`tools/docs/`、必要的文档元数据头 | `PROJECT_STATUS.md` |

## 职能路由

### PM / 版本规划智能体

主要关注：

- 维护正式版核心纵切的版本路线、阶段判断、优先级、里程碑和跨职能交接。
- 把用户的方向性决定拆成可执行工作包，明确目标、范围外、事实来源、职能落点和验收方式。
- 不替代 GDD、程序开发文档或美术文档的事实来源；具体规则、实现和视觉交付仍回写到对应职能文档。

必读：

- `agent_status/pm.md`
- `PROJECT_STATUS.md`
- `版本规划/README.md`
- `版本规划/09_正式版核心纵切开发路线.md`
- `版本规划/11_设计文档纵切批次矩阵.md`
- `agent_status/design.md`
- `agent_status/program.md`
- `agent_status/art.md`
- 当前任务涉及的 GDD、开发文档、美术路线或配置 README。

版本规划规则：

- `09` 是当前开发核心；阶段目标、系统优先级或纵切批次变化时，优先同步 `09`。
- `11` 是 GDD 纵切批次矩阵；某份 GDD 的功能进入当前批次、退出当前批次或改变正式完成口径时，同步 `11`。
- 阶段目标、系统优先级或里程碑变化时，同步 `版本规划/09_正式版核心纵切开发路线.md`。
- 项目总目标、跨职能交接、阻塞项或下一步总优先级变化时，同步 `PROJECT_STATUS.md`。
- PM 自身判断、最近完成、下一步建议和阻塞项写入 `agent_status/pm.md`。
- 每个正式纵切工作包至少包含：目标、范围外、涉及事实来源、策划 / 程序 / 美术交接、验收方式。

### 美术 / UI 智能体

主要关注：

- 视觉流水线、Manifest、AI 素材生成、正式入库资源、UI 设计交付、运行时美术验收。
- 负责 UI 从 MVP Baseline 到 Formal V1 / 后续版本的结构设计、版本冻结、active 规格更新和美术验收，不直接接管 Unity UI 程序实现。

必读：

- `agent_status/art.md`
- `美术文档/README.md`
- `美术文档/10_正式版核心纵切美术路线.md`
- `美术文档/00_美术流水线总览.md`
- `美术文档/ui_design/README.md`
- `美术文档/ui_design/ui_iteration_process.md`
- `美术文档/ui_design/formal_v1/screen_structure_review.md`
- `开发文档/09_视觉资源系统程序开发规范.md`

UI 版本规则：

- `美术文档/ui_design/screen_layouts.json` 是当前 active UI 对接规格；程序和素材生成都以它为准。
- 已通过验收的 UI 设计先冻结到 `美术文档/ui_design/versions/`，例如 `mvp_baseline_2026-05-22`。
- `formal_v1/*.md` 是正式结构设计文档；用户确认后，美术智能体逐界面修改 active `screen_layouts.json`。
- `versions/formal_v1_candidate/` 只是复杂界面的可选暂存区，不是必经流程，也不是程序接入口。
- 素材生成、Manifest 回填和程序交接必须发生在 active 规格更新并通过 `Validate-UIDesign.ps1` 之后。
- 每次 AI 出图、预处理或同步 Approved 素材后，必须刷新 `美术文档/_generated/可接入素材清单.md` 和 `.json`，并在 `美术文档/_generated/art_integration_snapshots/` 留快照；程序侧优先读取 latest 中的 `program_integrate` 条目自助接入。
- 美术侧每轮实际交付完成后，必须更新 `agent_status/art.md`，并将本轮美术相关改动单独提交；不要混入程序、策划、子模块或本地工具无关改动。

常用命令：

```powershell
.\tools\config\Sync-Configs.ps1 -Clean
.\tools\美术工具\Update-ArtManifest.ps1
.\tools\美术工具\Generate-ArtPrompts.ps1
.\tools\美术工具\Validate-UIDesign.ps1
.\tools\美术工具\Generate-ArtIntegrationCandidates.ps1
```

### 策划 / 数值 智能体

主要关注：

- GDD 规则、经济循环、物品生命周期、深渊节奏、数值假设、配置意图。

必读：

- `agent_status/design.md`
- `设计文档/GDD_00_系统关联总图.md`
- `版本规划/09_正式版核心纵切开发路线.md`
- `版本规划/11_设计文档纵切批次矩阵.md`
- `数值模型设计/00_基准价值与空间本位模型.md`
- 当前任务涉及的 `设计文档/GDD_*.md`

配置规则：

- `配置表(JSON)` 是版本源。
- `UnityClient/Assets/StreamingAssets/Configs` 是运行时生成副本。
- 运行 Unity 或自动化验证前先同步：

```powershell
.\tools\config\Sync-Configs.ps1 -Clean
```

### 程序 / Unity 智能体

主要关注：

- Unity 客户端、C# 架构、CoreBackend、UGUI、Validator、测试、编辑器自动化。

必读：

- `agent_status/program.md`
- `开发文档/00_程序开发大纲.md`
- `开发文档/00_客户端核心架构规范.md`
- `开发文档/00_Unity表现层与编辑器构建规范.md`
- `开发文档/12_程序开发优化建议与重构路线.md`
- `开发文档/13_编程规范与架构约定.md`

工程规则：

- 运行时 UI 使用纯 UGUI。不要重新引入 UI Toolkit、UXML、USS 或 `UIDocument` 运行时路径。
- 游戏规则写在后端或领域服务里，不写进 UI Controller。
- 配置或系统契约变更要优先补 Validator 和聚焦测试。
- 移动 Unity 资产时必须一起移动 `.meta` 文件，保留 GUID。

常用测试触发：

```powershell
Set-Content -Path "UnityClient/Logs/.test_trigger" -Value "RUN_ALL_TESTS"
```

### UI 程序智能体

主要关注：

- Unity 运行时 UI 表现层工程：UGUI 面板、Prefab/层级、CanvasScaler、Sprite 绑定、VisualID 接入、交互射线、动效反馈、运行时美术验收截图。
- 把美术 / UI 设计交付落到 Unity 中，但不接管玩法规则、数值配置或后端状态。

必读：

- `agent_status/program.md`
- `agent_status/art.md`
- `开发文档/00_Unity表现层与编辑器构建规范.md`
- `开发文档/05_表现层架构与事件总线(ViewAndEventBus).md`
- `开发文档/09_视觉资源系统程序开发规范.md`
- `开发文档/13_编程规范与架构约定.md`
- `美术文档/02_资源规格与接入规范.md`
- `美术文档/ui_design/README.md`
- `美术文档/ui_design/_generated/ui_design_handoff.md`

工作边界：

- 运行时 UI 只使用纯 UGUI，不引入 UI Toolkit、UXML、USS 或 `UIDocument`。
- UI Controller 只调用后端 API、监听 EventBus、展示状态和失败原因；不得直接修改 HP、SAN、金币、背包格子、掉落归属、怪物 AI 或结算结果。
- 背包真实规则统一走 `InventoryInteractionService`；背包显示、物品 UI 同步和层级统一走 `InventoryPresentationController`。
- `GameFlowController` 只作为流程上下文入口，不要把具体 UI 构建、背包同步、玩法规则继续塞回去。
- 美术资源通过 `VisualAssetService` / VisualID 接入；不要让运行时代码依赖 `art_manifest.json`。
- 接入新 Approved 素材前，优先查看 `美术文档/_generated/可接入素材清单.md`，其中 `program_integrate` 是当前可登记 / 可接入队列。
- 移动、复制、重命名 Unity 资产时必须同步处理 `.meta`，保留 GUID。

重点检查：

- CanvasScaler 是否按项目规范使用 `1920x1080` 参考分辨率。
- 图标、头像、立绘、底盘、背景是否按 DisplaySpec 使用固定容器和 `preserveAspect`，避免 `SetNativeSize` 撑坏布局。
- 背景应使用 cover 逻辑，图标和头像应使用 contain 逻辑。
- 面板遮罩、`CanvasGroup.blocksRaycasts`、按钮射线、拖拽层级不能阻断核心交互。
- 新 UI 接入后应跑对应 smoke test 或运行时美术验收脚本；如果 Unity 已有实例导致 batchmode 不能运行，必须记录原因。

### 知识库智能体

当前职能：

- 搭建轻量知识库，不迁移现有目录，不引入站点框架。
- 维护 Markdown 元数据头规范、双向文档关系网、文档索引生成脚本和校验脚本。
- 让美术、程序、策划、配置、数值和版本路线文档通过 `related` 形成可校验互链。

本轮目标：

- 新增 `知识库/README.md` 作为元数据规范。
- 新增 `tools/docs/Generate-DocsIndex.ps1` 和 `tools/docs/Validate-Docs.ps1`。
- 生成 `DOCS_INDEX.md` 与 `docs_index.json`。
- 补齐当前索引内全部 Markdown 元数据头。
- 把文档 `related` 升级为双向关系网，并确保知识库校验通过。

## 共享边界

- `配置表(JSON)` 是源数据；`UnityClient/Assets/StreamingAssets/Configs` 是生成副本。
- `UnityClient/Assets/StreamingAssets/Configs` 是 Unity 运行时读取副本，不作为事实来源、不手写维护、不纳入知识库索引。
- `UnityClient/Assets/Art/Approved` 存放正式入库运行时美术资源。
- `UnityClient/Assets/Art/_IncomingAI` 是 AI 出图工作区，应保持忽略。
- `美术文档/_generated` 与 `美术文档/ui_design/_generated` 是脚本输出；除非任务明确要求修复生成物，否则应通过对应流水线更新。
- `tools/ai-image-gateway` 是 submodule。除非任务明确处理该子模块，否则不要在父仓库提交其内部改动。

## 完成协议

每个复制出来的智能体完成一次有实际意义的任务前，必须回写状态。

1. 如果工作影响项目阶段、总目标、跨职能交接或阻塞项，更新 `PROJECT_STATUS.md`。
2. 更新对应职能状态页：
   - PM / 版本规划工作 -> `agent_status/pm.md`
   - 美术 / UI 工作 -> `agent_status/art.md`
   - 策划 / 数值工作 -> `agent_status/design.md`
   - 程序 / Unity 工作 -> `agent_status/program.md`
3. 在职能状态页里更新：
   - `最后更新`
   - `最近完成`
   - `当前关注`
   - `下一步建议`
   - `问题 / 阻塞`，如有
4. 如果产生了给其他职能的交接，必须同时写入 `PROJECT_STATUS.md` 和目标职能状态页。
5. 状态记录要短、事实化、可延续，不要粘贴聊天记录。

## Git 规范

- 工作区可能已有用户或其他智能体的改动。不要回滚无关脏文件。
- 只暂存与当前任务相关的文件。
- 移动 Unity 资产时，必须一起移动 `.meta` 文件。
- 提交前查看 `git status --short`，确认没有误提交生成物或无关文件。

