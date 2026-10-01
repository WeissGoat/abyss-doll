---
id: agents_entry
title: Project P3 智能体入口
type: entry
role: 全局
domain: agent_workflow
status: active
source_of_truth: true
related:
  - agent_status/README.md
  - agent_status/director.md
  - agent_status/pm.md
  - PROJECT_STATUS.md
  - rules/README.md
  - rules/01_文档维护与新增控制规则.md
  - rules/02_智能体任务路由与完成协议.md
  - 知识库/README.md
  - 知识库/views/director.md
  - 知识库/views/owner.md
  - 知识库/views/narrative.md
  - 知识库/views/pm.md
  - 知识库/views/art.md
  - 知识库/views/design.md
  - 知识库/views/program.md
  - 设计文档/剧情/README.md
  - tools/agent/README.md
  - tools/p3-mission/README.md
  - 版本规划/13_正式版全局体验总线与开放节奏.md
  - 版本规划/14_0-12小时候选主循环细案设计.md
  - 版本规划/09_正式版核心纵切开发路线.md
  - 版本规划/README.md
  - 版本规划/11_纵切批次与需求文档承接矩阵.md
last_verified: 2026-10-01
update_rule: 修改根职责路由、全局硬边界、开工入口或完成摘要时同步本文件；详细执行协议同步 rules/02。
---

# Project P3 智能体入口

> 所有复制出来的智能体先从这里选择职责路径。不要先改代码、配置、美术流水线或设计文档，也不要求每个 Agent 通读所有职能手册。

## 入口定位与事实优先级

`AGENTS.md` 只负责根职责路由和所有 Agent 都必须遵守的硬边界。详细跨职能执行协议见 `rules/02_智能体任务路由与完成协议.md`；职能阅读顺序见 `知识库/views/*.md`；当前状态见 `PROJECT_STATUS.md` 与 `agent_status/*.md`；专业事实仍以目标 GDD、配置、开发或美术文档为准。

发生冲突时，优先采用更具体、更新、active 且被当前入口引用的事实来源。旧 MVP 文档与当前 GDD、`13`、`14`、`09` 或状态页冲突时，以当前事实来源为准。

当前顶层关系保持：

```text
13：游戏导演 / 制作人层体验总线
  -> 14：0-12 小时细案索引与承接规则
  -> 09：正式版纵切宏观路线
  -> 11：批次与详细需求文档门禁
```

## 共享术语与程序证据入口

- **局内**：深渊探索 / 副本内行动。
- **局外**：小镇、工坊与养成。
- 程序任务按开发文档和 `p3-program-validation` 路由；`UnityClient/Logs/` 只作为运行时日志与验收证据入口。AutoTestDaemon、`.test_trigger` 和 headless smoke 的具体协议以 `开发文档/rules/04_自动化测试与验收流程规范.md` 为准。

## 30 秒开工流程

1. 选道（见 `rules/02`）：探索道做原型和试验，不碰正式资产、配置源和 active 文档；标准道是默认；严格道用于 Owner 完整模块、不可逆或高成本操作、核心身份资产和发布门禁。
2. 读取 `PROJECT_STATUS.md` 的当前优先级、交接和阻塞；只有游戏导演、Owner 或跨职能任务才读 `agent_status/director.md`。
3. 按下方任务职责路由选一个主责任 Role，读取对应 `知识库/views/*.md`（剧情用 `narrative.md`，Owner 用 `owner.md`）和受影响 Role 的状态页当前快照。
4. 读取目标细案、GDD、配置 README、开发文档、美术规格或专项规则；不要用聊天结论替代事实文档。探索道可跳过第 2-3 步。
5. 改动前运行 `.\tools\agent\Invoke-AgentHealthCheck.ps1`，提交前可加 `-Strict`；长期或恢复任务走 P3 Mission。

优先推进当前优先级的玩家结果；流程、校验器、美术管线和 agent 工具等元工作，只在解阻玩家结果或修复真实问题时做（`rules/02` 元工作预算）。

默认按以下层级渐进读取，不因 `related` 关联边自动展开全部文档：

```text
L0 AGENTS.md：根路由和全局硬边界
L1 PROJECT_STATUS.md：当前阶段、优先级、阻塞和交接快照
L2 Role View + 受影响状态页：职责上下文和当前快照
L3 目标事实文档：模块、剧情、GDD、配置、开发或美术事实
L4 条件展开：跨职能 View、rules/02、专项 Skill、验收证据或恢复记录
```

## 任务职责路由

| 任务特征 | 默认主责任 | 首读入口 | 进一步读取 |
|---|---|---|---|
| 玩家体验主线、系统开放节奏、深渊包装、全局优先级 | 游戏导演 | `知识库/views/director.md` | `13`、`14`、目标细案、`09`、`11` |
| 完整模块或玩家结果的跨职能整体负责 | Owner | `知识库/views/owner.md` + 目标模块入口 | 按实际影响展开剧情、策划、程序、美术 View 和事实文档 |
| 剧情大纲、叙事结构、角色关系、对白、CG / 漫画一致性 | 剧情 | `知识库/views/narrative.md` + `设计文档/剧情/README.md` | 剧情大纲、目标叙事文档、相关细案与演出规格 |
| GDD、规则卡、数值、配置意图或配置源 | 策划 | `知识库/views/design.md` | `agent_status/design.md` 和目标设计 / 配置事实 |
| Unity、领域服务、架构、纯 UGUI、Validator、自动测试 | 程序 | `知识库/views/program.md` | `agent_status/program.md`、开发规则和 `开发文档/rules/04_自动化测试与验收流程规范.md` |
| UI 视觉、素材生产、Manifest、VisualID、运行时美术验收 | 美术 | `知识库/views/art.md` | `agent_status/art.md` 和美术事实文档 |
| 文档元数据、索引、关联网 | 知识库 | `知识库/README.md` | `rules/01`、工具脚本和核心入口 |

正式 active Role 为 `全局`、`游戏导演`、`Owner`、`剧情`、`策划`、`程序`、`美术`、`知识库`。Owner 对完整模块或玩家结果进行跨职能整体负责，处理中可以切换剧情、策划、程序和美术 Role，但 Owner 定位不变化。专工是独立 Agent 处理某个 Role 工作的委派方式，P3 Mission 是长任务机制，执行 / 自验 / 验收 / 回写是任务环节，它们都不是 Role。完整 Role 选择和 Owner 协议见 `rules/02_智能体任务路由与完成协议.md`。

旧 PM / 版本规划角色不再作为 active 路由；`agent_status/pm.md` 与 `知识库/views/pm.md` 只保留兼容入口。

## 全局不可违反边界

- 新增、重写、拆分、归档或调整项目文档前，必须先读 `rules/01_文档维护与新增控制规则.md`，优先补充已有事实来源，禁止制造第二套进度表或事实来源。
- `配置表(JSON)` 是版本源；`UnityClient/Assets/StreamingAssets/Configs` 是生成副本，不手写维护、不作为事实来源、不纳入知识库索引。
- 运行时 UI 只使用纯 UGUI；不要引入 UI Toolkit、UXML、USS 或 `UIDocument`。游戏规则必须留在后端或领域服务，不写进 UI Controller。
- `UnityClient/Assets/Art/Approved` 存放正式运行时美术资源；`UnityClient/Assets/Art/_IncomingAI` 是忽略的 AI 工作区。
- `美术文档/_generated` 与 `美术文档/ui_design/_generated` 是流水线输出；除非任务明确修复生成物，否则通过对应脚本刷新。
- `tools/ai-image-gateway` 是 submodule；除非任务明确处理它，否则不要在父仓库提交其内部改动。
- 移动、复制或重命名 Unity 资产时必须同步处理 `.meta`，保留 GUID。
- 工作区可能已有用户或其他 Agent 的改动；不得回滚、覆盖或顺带清理无关脏文件，只暂存当前任务相关文件。
- 没有验收证据不得标记 `已完成`。字段说明、README、审计、任务拆分或修改前设计不能直接视为实现完成或配置完成。
- Owner 必须更新所有受影响职能状态页和事实文档；Owner 自验不替代剧情、策划、程序、美术或游戏导演的外部验收。

## 长任务与项目 Skill 路由

- 长期任务、拆任务执行、持续执行、`mission` 或恢复继续：优先使用 `.codex/skills/p3-mission/SKILL.md`；没有加载时再读取 `tools/p3-mission/README.md` 与 fallback `SKILL.md`。新建 Mission 必须基于已有详细来源材料。
- 需要先设计再实现的功能、重构或流程变更：设计写入 `docs/specs/`，实施步骤附在文末，不再单独写 plan；跨会话执行用定稿 spec 生成 P3 Mission。规则见 `rules/01`、`rules/02`；不使用 superpowers 插件的头脑风暴、计划和执行流程。
- 正式美术资产从准入、候选生产、Approved、Unity 接入到运行时验收：使用 `p3-art-asset-production`。
- 纯图片生成 / 编辑、差分、inpaint、后端选择和生成证据：使用 `p3-generate-image`。
- 叙事 CG、漫画页、Panel VisualID、一致性修复与运行时漫画验收：使用 `p3-narrative-cg-comic`。
- 新概念图优先用宿主内置生图（Codex `image_gen`），没有时转 AI 图片网关 `openai_images`。局部差分优先 `gemini_chat_image`；NovelAI inpaint 仅用于存在明确 mask、允许随机性且指向性要求不强的场景。
- 程序自动化、运行时美术和发布聚合分别使用 `p3-program-validation`、`p3-art-validation`、`p3-release-validation`，不得混淆程序通过与美术通过。
- 项目 Skill 由 `.codex/skills/*/` 提供；p3-mission 的工具源 `tools/p3-mission/` 与 `.codex/skills/p3-mission/` 必须内容一致。`.claude/skills/<name>/SKILL.md` 只是 Claude Code 的发现转发入口（frontmatter 与源一致，正文指向源文件）。新增、改名、删除 Skill，修改其 frontmatter 或修改 p3-mission 时，在同一改动内同步这些副本。
- 编写或修改 Skill：正文保留硬约束和按阶段读取表，references 只在进入对应阶段时读取，不要求开工前全部读完；健康检查的 Skill 校验会检查副本一致和引用路径。

## 验证与完成摘要

最终回复前确认：已运行与风险相称的验证并如实记录（通过、失败或 `validation_limited:*`）；声明不超过证据；`git status --short` 已检查，只暂存本任务文件。

状态回写按道执行：探索道不回写；标准道只在状态变化时改受影响状态页一行；严格道按 `rules/02` 回写矩阵同步事实文档、`PROJECT_STATUS.md`、导演状态与必要的 `13` / `14` / `09`。防重复派发、完成判定与 Git 要求见 `rules/02`。
