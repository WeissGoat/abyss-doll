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
  - agent_status/director.md
  - agent_status/pm.md
  - PROJECT_STATUS.md
  - rules/README.md
  - rules/01_文档维护与新增控制规则.md
  - 知识库/README.md
  - 知识库/views/director.md
  - 知识库/views/pm.md
  - 知识库/views/art.md
  - 知识库/views/design.md
  - 知识库/views/program.md
  - tools/agent/README.md
  - tools/p3-mission/README.md
  - 版本规划/13_正式版全局体验总线与开放节奏.md
  - 版本规划/14_0-12小时候选主循环细案设计.md
  - 版本规划/09_正式版核心纵切开发路线.md
  - 版本规划/README.md
  - 版本规划/11_纵切批次与需求文档承接矩阵.md
last_verified: 2026-07-14
update_rule: 修改智能体分工、开工流程或完成协议时同步本文件。
---

# Project P3 智能体入口

> 复制出来的智能体开工前先读这里。不要先改代码、配置、美术流水线或设计文档。

## 当前事实来源

1. 先读 `PROJECT_STATUS.md`，确认项目总目标、当前阶段、跨职能交接和阻塞项。
2. 再读取游戏导演 / 制作人状态页，确认玩家体验主线、系统开放节奏、任务 Owner 工作流和下一步总优先级：
   - 游戏导演 / 制作人：`agent_status/director.md`
3. 按任务涉及范围读取对应职能状态页：
   - 美术 / UI：`agent_status/art.md`
   - 策划 / 数值：`agent_status/design.md`
   - 程序 / Unity：`agent_status/program.md`
4. 需要快速定位上下文时，读取对应视图：
   - 游戏导演 / 制作人：`知识库/views/director.md`
   - 美术 / UI：`知识库/views/art.md`
   - 策划 / 数值：`知识库/views/design.md`
   - 程序 / Unity：`知识库/views/program.md`
5. `GEMINI.md` 是项目知识库和上下文路由。
6. `rules/README.md` 是全局 agent 必读规则入口；新增、重写、拆分或归档项目文档前必须先读 `rules/01_文档维护与新增控制规则.md`。
7. `版本规划/13_正式版全局体验总线与开放节奏.md` 是当前游戏导演 / 制作人层顶层文档，定义玩家体验主线、系统开放节奏、深渊包装和任务 Owner 工作流。
8. `版本规划/14_0-12小时候选主循环细案设计.md` 是 0-12 小时细案索引与承接规则；每个具体细案一案一文，存放在 `版本规划/0-12小时细案/`。未细化完成的切片不作为实现 Owner 正式开工依据。
9. `版本规划/09_正式版核心纵切开发路线.md` 是当前开发核心路线，定义正式版纵切宏观总线、系统优先级和阶段边界。
10. `版本规划/11_纵切批次与需求文档承接矩阵.md` 是批次与需求承接矩阵，说明 GDD / 功能优先级进入哪个纵切批次，以及是否已有详细需求文档支撑。
11. `版本规划/README.md` 是版本规划目录入口，说明当前 active 文档、归档文档和职责边界。`版本规划/12_正式版长期版本节点规划.md` 只保留为旧链接兼容页，不再作为 active 事实来源。

如果旧 MVP 文档与当前 GDD、`13` 体验总线、`14` 细案、`09` 路线或状态页冲突，以当前 GDD、`13`、`14`、`09` 和状态页为准。

当前开发文档关系：

```text
13：游戏导演 / 制作人层，定义玩家体验主线、系统开放节奏、深渊包装和任务 Owner 工作流
  ↓
14：0-12 小时细案 Owner 层，维护 T0-T4 细案索引、承接规则和一案一文入口
  ↓
09：当前开发核心，定义正式版纵切宏观总线、系统优先级和阶段边界
  ↓
11：纵切批次与需求文档承接矩阵，定义 GDD / 功能优先级何时进入纵切批次以及是否有详细需求文档支撑
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

## 全局 agent rules

- 根目录 `rules/` 只存放跨职能 agent 必读规则和工作流约束。
- `rules/01_文档维护与新增控制规则.md` 是所有文档新增、重写、拆分、归档和关联维护前的必读规则。
- `设计文档/规则卡/` 是策划业务规则卡，不再称为项目 `rules`。
- `开发文档/rules/` 是程序 / Unity 专项规则，继续作为程序职能必读规范。

## 长任务协议

当用户要求长期任务、拆任务执行、持续执行、`mission` 或恢复继续时，优先使用项目内 Codex 可识别 skill：`.codex/skills/p3-mission/SKILL.md`。
`tools/p3-mission` 是独立工具源码仓库；如果当前会话没有自动加载 skill，再读取 `tools/p3-mission/README.md` 和 `tools/p3-mission/SKILL.md` 作为 fallback。
`misc/Missions` 只是已归档的上游参考，不再作为 active skill、路由入口或第二套 mission 系统维护。

P3 mission 是非侵入式本地任务队列：用 `.mission/*.csv` 或 `missions/*.csv` 记录目标、任务拆分、执行状态、验证证据和状态回写；不启用 Trellis hooks，不替代 `AGENTS.md`、`PROJECT_STATUS.md`、`agent_status/*` 或各职能事实文档。
新建 P3 mission 必须基于已有详细来源材料，并通过 `New-P3Mission.ps1 -Source <来源路径或引用>` 生成（兼容 `-SourceSpec`）。来源可以是 Markdown、目录、非 Markdown 文档、事实来源或已批准计划；脚本只记录 `source_ref` 并在缺来源时提醒，是否足够详细由 agent 按 skill 判断。不得从一句话目标、聊天结论或模糊 TODO 直接生成 mission；缺少来源时，先由对应职能补齐详细需求 / 计划文档，再进入 p3-mission。

## 角色

> 本区提供基础职能边界。对应职能智能体完成任务后，可以继续补充和迭代自己的工作细节。

| 角色 | 工作范围 | 需要关注的文件 | 需要修改的文件 | 完成后回写 |
|---|---|---|---|---|
| 游戏导演 / 制作人智能体 | 玩家体验主线、系统开放节奏、深渊包装、任务 Owner 拆分、优先级排序、完成口径和制作推进 | `PROJECT_STATUS.md`、`agent_status/director.md`、`版本规划/13_正式版全局体验总线与开放节奏.md`、`版本规划/09_正式版核心纵切开发路线.md`、`版本规划/11_纵切批次与需求文档承接矩阵.md`、三职能状态页、当前系统事实来源 | `版本规划/`、`PROJECT_STATUS.md`、`agent_status/director.md`、必要的职能状态和交接文档 | `agent_status/director.md`，影响阶段目标、开放节奏、任务 Owner 工作流或跨职能验收口径时同步 `PROJECT_STATUS.md`、`13` 和必要的 `09` |
| 0-12 小时细案 Owner | 按 `13` 第 12 节的游戏时长横向切分统一设计 T0-T4：玩家旅程、剧情 / 新手节奏、系统自然开放、关键状态变化、UI / 美术表达、下一段钩子和轻量实现承接提示 | `PROJECT_STATUS.md`、`agent_status/director.md`、`版本规划/13_正式版全局体验总线与开放节奏.md`、`版本规划/14_0-12小时候选主循环细案设计.md`、目标细案文档、`版本规划/09_正式版核心纵切开发路线.md`、`版本规划/11_纵切批次与需求文档承接矩阵.md`、相关 GDD / 美术 / 开发事实来源 | `版本规划/14_0-12小时候选主循环细案设计.md`、`版本规划/0-12小时细案/`、`agent_status/director.md`、必要时同步 `13` / `09` / `PROJECT_STATUS.md` | `agent_status/director.md`；影响细案入口、开工顺序、阶段门禁或完成口径时同步 `PROJECT_STATUS.md`、`14`、目标细案文档和必要的 `13` / `09` |
| 实现 Owner 智能体 | 对一个已完成细案的实现切片纵切负责：证据审计 / 本切片具体方案 / 配置 / UI / 美术 / 程序接入 / Owner 自验 / 状态回写 | `知识库/views/director.md`、`13`、`14`、`09`、`11`、受影响职能状态页和当前系统事实来源 | 按实现切片允许范围修改 `设计文档/`、`配置表(JSON)/`、`UnityClient/`、`美术文档/`、`开发文档/` 和状态页 | 所有受影响状态页；影响细案或全局体验时同步 `agent_status/director.md` 和 `PROJECT_STATUS.md` |
| 主美 / UI 视觉总监 | 视觉方向、Manifest 口径、AI 素材质量、UI 结构版本、运行时美术验收和美术交接验收 | `agent_status/art.md`、`美术文档/README.md`、`美术文档/00_美术流水线总览.md`、`美术文档/10_正式版核心纵切美术路线.md`、`美术文档/ui_design/README.md`、`开发文档/rules/03_视觉资源系统程序开发规范.md` | 默认只改美术事实文档和状态页；作为任务 Owner 或耗时专工时可改 Approved 素材与 UI 规格 | `agent_status/art.md` |
| 主程 / Unity 架构总监 | Unity 架构、CoreBackend、UGUI、Validator、测试、编辑器自动化和程序验收 | `agent_status/program.md`、`开发文档/00_程序开发大纲.md`、`开发文档/rules/01_客户端分层与领域架构规范.md`、`开发文档/rules/02_Unity表现层与编辑器构建规范.md`、`开发文档/rules/00_程序开发总规则.md` | 默认只改开发文档、测试口径和状态页；作为任务 Owner 时可改 Unity 代码 | `agent_status/program.md` |
| 主策 / 数值总监 | GDD 规则、经济循环、物品生命周期、深渊节奏、数值假设、配置意图和策划验收 | `agent_status/design.md`、`设计文档/GDD/GDD_00_系统关联总图.md`、`版本规划/13_正式版全局体验总线与开放节奏.md`、`版本规划/09_正式版核心纵切开发路线.md`、`数值模型设计/00_基准价值与空间本位模型.md`、当前任务涉及的 GDD 和配置 README | 默认只改设计 / 配置事实文档和状态页；作为任务 Owner 时可改配置源 | `agent_status/design.md` |
| 知识库智能体 | 文档元数据、索引、校验脚本、知识库规范、职能阅读入口 | `知识库/README.md`、`知识库/views/`、`rules/README.md`、`rules/01_文档维护与新增控制规则.md`、`DOCS_INDEX.md`、`docs_index.json`、核心入口文档 | `AGENTS.md`、`PROJECT_STATUS.md`、`rules/`、`知识库/`、`tools/docs/`、必要的文档元数据头 | `PROJECT_STATUS.md` |

## 职能路由

### 全局工作模式

后续默认采用 `细案 Owner 统一设计 + 实现 Owner 纵切负责 + 职能总监验收`。

- 0-12 小时 T0-T4 的详细设计由细案 Owner 按 `13` 第 12 节的游戏时长横向切分逐段完善；`14` 维护时长段细案索引与承接规则，具体细案在 `版本规划/0-12小时细案/` 中一案一文。未完成设计的时长段不作为实现 Owner 正式开工依据。
- 实现 Owner 负责一个已完成细案的实现切片，而不是只负责一个职能切片。
- 实现 Owner 可以同时处理本切片具体方案、配置源、程序功能、UI/UX、美术接入、验证和状态回写，但必须遵守细案和所有受影响事实来源。
- 实现 Owner 不是四个职能 agent 的简单叠加，而是围绕同一个玩家结果按阶段承担策划落地、配置、美术 / UI、程序和自验职责。
- 标准责任流水：`证据审计 -> 策划详细设计 -> 配置源落地 -> UI/UX 与美术设计 / 素材 -> 程序功能与素材接入 -> Owner 自验 -> 职能总监验收 -> 状态回写`。
- Owner 作为策划时，先补详细规则、边界、配置字段、UI / 美术表现需求、验收样例和范围外；作为配置执行者时，落地 JSON 源、同步配置并提供 Validator / 固定样例证据。
- Owner 作为美术 / UI 执行者时，按 active UI 规格、VisualID、风格基准和素材需求完成设计 / 接入，或向专工提供明确输入、输出和验收标准；作为程序时，接入真实领域服务、运行时 UI / 素材和状态变化，并提供 smoke / P0 / 人工流程证据。
- Owner 自验是第一层验收：证明该实现切片按玩家路径跑通并整理证据；最终完成仍需要对应主策、主程、主美 / UI 视觉总监或游戏导演 / 制作人的外部验收。
- 主策、主程、主美默认负责总体设计和验收，不作为常规实现分包。
- 图片生成、大批量素材处理、大规模配置填充、批量校验等耗时工作可以单独拆给专门 agent；这些 agent 只交付指定产物，不自行改变体验方向、规则方向、系统优先级或完成口径。
- 旧 PM / 版本规划智能体不再作为 active 路由；`agent_status/pm.md` 和 `知识库/views/pm.md` 仅保留为兼容入口。

实现切片承接至少包含：

- 玩家结果：完成后玩家能看到或做到什么。
- 范围外：本轮明确不做什么。
- 细案来源：对应 `14` 的细案切片 ID 和 `13` 的模块宏观设计行。
- Owner 责任流水：本切片从证据审计到状态回写覆盖哪些阶段，哪些阶段由专工协助。
- 事实来源：GDD、开发文档、美术规格、配置 README、状态页。
- 允许改动范围：配置、代码、UI、素材、文档中哪些可以改。
- 需要验收的主职能：主策、主程、主美中的哪些需要验收。
- Owner 自验：玩家路径、固定样例、截图 / 报告、人工复核等第一层验收证据。
- 外部验收：主策 / 主程 / 主美 / 游戏导演中哪些人验收，验收未完成时不得标记已完成。
- 验证证据：smoke、P0 报告、配置校验、ArtAcceptance、人工验收或 `validation_limited:*`。
- 状态回写：所有受影响状态页和事实文档。

### 游戏导演 / 制作人智能体

主要关注：

- 维护玩家体验主线、系统开放节奏、深渊包装、正式版阶段目标和任务 Owner 工作流。
- 把用户的方向性决定拆成玩家结果导向的细案或实现切片，明确目标、范围外、事实来源、允许改动范围、验收方式和状态回写。
- 兼管制作推进，但不恢复旧 PM 式横向派发表；优先判断体验是否成立，再决定制作顺序。
- 不替代 GDD、程序开发文档或美术文档的事实来源；具体规则、实现和视觉交付仍回写到对应职能文档。

必读：

- `agent_status/director.md`
- `PROJECT_STATUS.md`
- `版本规划/13_正式版全局体验总线与开放节奏.md`
- `版本规划/14_0-12小时候选主循环细案设计.md`
- `版本规划/README.md`
- `版本规划/09_正式版核心纵切开发路线.md`
- `版本规划/11_纵切批次与需求文档承接矩阵.md`
- `agent_status/design.md`
- `agent_status/program.md`
- `agent_status/art.md`
- 当前任务涉及的 GDD、开发文档、美术路线或配置 README。

版本规划规则：

- `13` 是当前游戏导演 / 制作人层顶层体验总线；玩家体验主线、系统开放节奏、深渊包装、任务 Owner 工作流或阶段体验门禁变化时，优先同步 `13`。
- `14` 是 0-12 小时按游戏时长推进的细案索引与承接规则；T0-T4 细案入口、时长段设计口径、实现切片入口或细案状态变化时同步 `14`，具体玩家旅程、剧情 / 新手节奏、系统开放、关键状态和下一段钩子优先同步目标细案文档。
- `09` 是当前开发核心；系统优先级、长期节点、阶段门禁、职能进度入口或防重复派发规则变化时，同步 `09`。
- `11` 是纵切批次与需求文档承接矩阵；某份 GDD 的功能进入当前批次、退出当前批次、改变正式完成口径，或新增需求文档门禁项时，同步 `11`。`11` 不记录实现状态。
- 阶段目标、玩家体验主线、系统开放节奏、0-12 小时细案或宏观路线变化时，同步 `版本规划/13_正式版全局体验总线与开放节奏.md`、`版本规划/14_0-12小时候选主循环细案设计.md`、目标细案文档、`版本规划/09_正式版核心纵切开发路线.md` 和 `PROJECT_STATUS.md` 中受影响部分。
- 项目总目标、跨职能交接、阻塞项或下一步总优先级变化时，同步 `PROJECT_STATUS.md`。
- 游戏导演 / 制作人自身判断、最近完成、下一步建议和阻塞项写入 `agent_status/director.md`。
- 派发工作前先检索对应职能状态页：游戏导演 / 细案 Owner 看 `agent_status/director.md`，策划 / 配置看 `agent_status/design.md`，程序看 `agent_status/program.md`，美术 / UI 看 `agent_status/art.md`。已完成或进行中的同名玩家结果不得重复派发，只能按验收补强、缺口修复、表现补强或配置补齐处理。
- 详细执行进度由实现 Owner 回写所有受影响状态页和事实文档。策划配置事实写入 `设计文档/config/26_正式配置设计与填充推进计划.md` 和 `agent_status/design.md`；程序实现写入开发文档和 `agent_status/program.md`；美术交付写入美术文档和 `agent_status/art.md`；全局体验、开放节奏、细案和任务 Owner 口径写入 `agent_status/director.md`、`PROJECT_STATUS.md` 和必要时的 `13` / `14` / `09`。
- 不要恢复旧跨职能执行表。`13` 只保留体验总线和任务 Owner 工作流；`14` 只保留 0-12 小时细案索引、承接规则和实现切片入口；具体细案一案一文；`09` 只保留开发宏观路线、长期节点、职能入口和防重复派发规则。
- 字段说明、README、审计结论、任务拆分和 JSON 修改前设计不等于配置源完成；只有配置源落地并通过校验后，才能在状态页中标记 `配置完成`。
- 后续如果需要新增进度表、验收状态表或防重复派发表，优先合并进对应细案 / 实现切片来源、职能状态页或事实文档；只有影响体验总线、细案入口、长期节点、跨职能入口或通用状态标记规则时才同步 `13`、`14` 或 `09`。

### 美术 / UI 智能体

主要关注：

- 视觉流水线、Manifest、AI 素材生成、正式入库资源、UI 设计交付、运行时美术验收。
- 负责 UI 从 MVP Baseline 到 Formal V1 / 后续版本的结构设计、版本冻结、active 规格更新和美术验收；涉及纯 UGUI 的布局、视觉层级、皮肤绑定、VisualID 表现、截图验收和非玩法 UI polish，默认由美术 / UI 智能体直接闭环。只有领域服务、后端规则、Unity 工程约束、自动验收工具或 UGUI 底层能力阻断时，再交由程序侧处理。

必读：

- `agent_status/art.md`
- `美术文档/README.md`
- `美术文档/10_正式版核心纵切美术路线.md`
- `美术文档/00_美术流水线总览.md`
- `美术文档/ui_design/README.md`
- `美术文档/ui_design/ui_iteration_process.md`
- `美术文档/ui_design/formal_v1/screen_structure_review.md`
- `美术文档/ui_design/formal_v2/00_formal_v2_ux_ui_overview.md`
- `开发文档/rules/03_视觉资源系统程序开发规范.md`

UI 版本规则：

- `美术文档/ui_design/screen_layouts.json` 是当前 active UI 对接规格；程序和素材生成都以它为准。
- 已通过验收的 UI 设计先冻结到 `美术文档/ui_design/versions/`，例如 `mvp_baseline_2026-05-22`。
- `formal_v1/*.md` 是正式结构设计文档；用户确认后，美术智能体逐界面修改 active `screen_layouts.json`。
- `formal_v2/*.md` 是 UX/UI 重构设计草案，用于解决按钮堆叠、主次行动不清和正式感不足；用户确认并写入 active 前，不作为程序接入口，也不触发素材生成。
- `versions/formal_v1_candidate/` 只是复杂界面的可选暂存区，不是必经流程，也不是程序接入口。
- 从正式需求准入、Manifest、候选生产、Agent 筛选、Approved、Unity 导入、Registry 到运行时验收的端到端美术资产生产优先使用项目 skill：`.codex/skills/p3-art-asset-production/SKILL.md`。默认采用交互模式，只在方向变化、低置信度、核心资产准入或未授权正式替换时停下询问；用户提前声明全自动后，可在锁定事实和授权范围内自动执行到验收与状态回写。
- 纯图片生成 / 编辑能力，包括文生图、图生图、差分、inpaint、后端选择、Prompt 格式和后端排障，使用项目 skill：`.codex/skills/p3-generate-image/SKILL.md`（Skill 名 `generate-image`）；正式资产的 Manifest、筛选、Approved、Unity 接入和验收仍由 `p3-art-asset-production` 负责。
- 叙事 CG、CG 底图、漫画页分格播放、Panel VisualID、一致性修复或运行时漫画截图验收优先使用项目 skill：`.codex/skills/p3-narrative-cg-comic/SKILL.md`；不得在未锁定角色 / 场景 / 风格锚点时直接全量独立文生图。
- 设计图 / 概念图默认使用 Codex 内置 `image_gen` 生成；若当前工具环境没有暴露 `image_gen`，自动改用 AI 图片网关的 `openai_images` 后端。不得自动改用 NovelAI、Gemini 图生图、Grok 或 mock 代替新概念图默认路由。
- 图片整体满足要求但局部有问题，或需要角色 / 状态 / 姿态差分时，优先使用 `gemini_chat_image` 图生图；只有存在明确 mask、允许一定随机性且指向性要求不强时，才使用 NovelAI inpaint。
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
- 把进入开发、配置、表现或验收的系统需求整理成可沟通、可开发、可验收的详细需求文档，不能只在路线文档或聊天结论里留一句话。

必读：

- `agent_status/design.md`
- `设计文档/GDD/GDD_00_系统关联总图.md`
- `版本规划/09_正式版核心纵切开发路线.md`
- `版本规划/11_纵切批次与需求文档承接矩阵.md`
- `数值模型设计/00_基准价值与空间本位模型.md`
- 当前任务涉及的 `设计文档/GDD/GDD_*.md`
- 配置源实现前读取 `设计文档/config/gates/56_正式配置源落地准入门禁.md`

策划需求文档规则：

- 所有进入开发、配置、表现或自动验收的系统需求，都必须有详细需求文档承接；不能只用 `09` 路线中的一句优先级、聊天结论或状态页条目替代。
- 详细需求文档可以是对应 GDD、规则卡、承接清单、内容包、配置 README 或开发需求文档，但必须能让目标职能不依赖聊天上下文也能理解目标、范围、规则、字段、边界和验收方式。
- 若现有文档已经足够详细，策划智能体应在交接中明确引用“完整交付包”；若只存在一句功能项或优先级，必须先补文档，再推动程序、美术或配置落地。
- 每份系统需求文档至少覆盖：设计目标、范围外、玩家可见体验、核心规则 / 状态机 / 算法、关键配置字段、跨系统关系、UI / 美术表现需求、Validator 或自动验收要求、样例场景和完成判定。
- `版本规划/09_正式版核心纵切开发路线.md` 只定义当前开发核心、系统优先级和阶段边界；它不是具体系统需求书，也不替代 GDD、规则卡、配置 README 或程序开发需求。

配置规则：

- `配置表(JSON)` 是版本源。
- `UnityClient/Assets/StreamingAssets/Configs` 是运行时生成副本。
- `设计文档/config/26_正式配置设计与填充推进计划.md` 是策划 / 配置侧防重复执行入口：`8.3 策划侧完整执行规划与状态总览` 记录策划 agent 的完整规划和工作项状态，`8.4 策划配置执行状态表` 记录配置源 JSON 落地的批次与子项状态。
- 策划侧完整规划必须按工作性质分层记录：需求 / 规则、配置设计准入、配置源落地、验收 / 校准、后续内容池。只有配置源落地层进入 `8.4` 子项台账；其余层级只能作为策划证据或设计准入证据。
- 进入配置源 JSON 实现前，先按 `设计文档/config/gates/56_正式配置源落地准入门禁.md` 检查是否达到 `设计准入`；未准入时先补 GDD、规则卡、配置承接审计、落地设计、任务拆分、ID 锁定或验收样例。
- 字段说明、README、审计结论、任务拆分、ID 锁定、JSON 修改前设计和准入门禁都只是策划 / 配置证据，不等于 `配置完成`。
- 只有配置源已落地、同步通过、关键引用 error 为 0，并有 Validator / 固定样例 / 人工复核证据后，才能在 `agent_status/design.md` 标记 `配置完成`。
- 任一策划工作项、配置批次或配置子项开始、完成或阻塞时，必须同步更新 `26` 的对应状态和 `agent_status/design.md`；已是 `进行中` 或 `配置完成` 的同名项不得重复派发，只能按缺口补齐、验收修复、字段迁移或数值校准建立新子项。
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
- `开发文档/rules/01_客户端分层与领域架构规范.md`
- `开发文档/rules/02_Unity表现层与编辑器构建规范.md`
- `开发文档/12_程序开发优化建议与重构路线.md`
- `开发文档/rules/00_程序开发总规则.md`

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
- `开发文档/rules/02_Unity表现层与编辑器构建规范.md`
- `开发文档/05_表现层架构与事件总线(ViewAndEventBus).md`
- `开发文档/rules/03_视觉资源系统程序开发规范.md`
- `开发文档/rules/00_程序开发总规则.md`
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

- 新增、重写、拆分、归档或调整项目文档前，必须先按 `rules/01_文档维护与新增控制规则.md` 判断是否应补充既有文档、是否需要归档旧文档，以及是否会造成重复事实来源。
- `配置表(JSON)` 是源数据；`UnityClient/Assets/StreamingAssets/Configs` 是生成副本。
- `UnityClient/Assets/StreamingAssets/Configs` 是 Unity 运行时读取副本，不作为事实来源、不手写维护、不纳入知识库索引。
- `UnityClient/Assets/Art/Approved` 存放正式入库运行时美术资源。
- `UnityClient/Assets/Art/_IncomingAI` 是 AI 出图工作区，应保持忽略。
- `美术文档/_generated` 与 `美术文档/ui_design/_generated` 是脚本输出；除非任务明确要求修复生成物，否则应通过对应流水线更新。
- `tools/ai-image-gateway` 是 submodule。除非任务明确处理该子模块，否则不要在父仓库提交其内部改动。

## 完成协议

每个复制出来的智能体完成一次有实际意义的任务后、最终回复前，必须回写状态。

1. 如果工作影响项目阶段、总目标、跨职能交接或阻塞项，更新 `PROJECT_STATUS.md`。
2. 如果工作影响玩家体验主线、系统开放节奏、深渊包装、任务 Owner 工作流、优先级或全局完成口径，更新 `agent_status/director.md`，必要时同步 `版本规划/13_正式版全局体验总线与开放节奏.md`。
3. 如果工作影响 0-12 小时 T0-T4 细案、实现切片入口、玩家路径、验收样例或细案状态，更新目标细案文档、`版本规划/14_0-12小时候选主循环细案设计.md` 和 `agent_status/director.md`。
4. 实现 Owner 必须更新所有受影响职能状态页：
   - 美术 / UI 工作 -> `agent_status/art.md`
   - 策划 / 数值工作 -> `agent_status/design.md`
   - 程序 / Unity 工作 -> `agent_status/program.md`
5. 在状态页里更新：
   - `最后更新`
   - `最近完成`
   - `当前关注`
   - `下一步建议`
   - `问题 / 阻塞`，如有
6. 如果产生了给其他职能的交接，必须同时写入 `PROJECT_STATUS.md` 和目标职能状态页。
7. 任意智能体完成开发、配置、策划、美术、UI 或验收工作后，必须更新对应职能状态页，写清最近完成、当前关注、下一步建议、阻塞项和关键证据入口。
8. 同一玩家结果涉及多个职能时，由实现 Owner 分别回写各职能状态页和事实文档；不要写一条跨职能混合执行行来替代各职能事实。
9. 不得在 `11` 或其他需求承接文档中另写实现进度表；实现进度统一回到对应职能状态页和事实文档。
10. 不得在没有验收证据时把条目标记为 `已完成`。字段说明、README、审计结论、任务拆分和 JSON 修改前设计只能作为策划 / 配置证据，不能直接标记 `配置完成`。
11. 如果某项工作需要进度挂钩表，按细案 / 实现切片或职能写入对应状态页或事实文档：全局体验 / 开放节奏写 `agent_status/director.md` 或 `13`，0-12 小时细案写目标细案文档与 `14` 索引，策划 / 配置写 `agent_status/design.md` 或 `设计文档/config/26_正式配置设计与填充推进计划.md`，程序写 `agent_status/program.md` 或开发文档，美术 / UI 写 `agent_status/art.md` 或美术文档。
12. 如果工作影响长期里程碑、节点门禁、职能进度入口、状态标记规则或跨职能防重复派发规则，再同步 `版本规划/09_正式版核心纵切开发路线.md`。
13. 状态记录要短、事实化、可延续，不要粘贴聊天记录。

## Git 规范

- 工作区可能已有用户或其他智能体的改动。不要回滚无关脏文件。
- 只暂存与当前任务相关的文件。
- 移动 Unity 资产时，必须一起移动 `.meta` 文件。
- 提交前查看 `git status --short`，确认没有误提交生成物或无关文件。

