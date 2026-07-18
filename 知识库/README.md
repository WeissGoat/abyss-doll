---
id: knowledge_base_readme
title: 知识库规范
type: kb
role: 知识库
domain: knowledge_base
status: active
source_of_truth: true
related:
  - AGENTS.md
  - GEMINI.md
  - DOCS_INDEX.md
  - rules/README.md
  - rules/01_文档维护与新增控制规则.md
  - rules/02_智能体任务路由与完成协议.md
  - 知识库/views/director.md
  - 知识库/views/owner.md
  - 知识库/views/narrative.md
  - 知识库/views/pm.md
  - 知识库/views/art.md
  - 知识库/views/design.md
  - 知识库/views/program.md
  - tools/agent/README.md
last_verified: 2026-07-18
update_rule: 调整元数据字段、索引策略、文档治理规则或职责入口归属时更新本文件。
---

# 知识库规范

> 本目录定义 Project P3 的轻量知识库规则。当前阶段不迁移目录、不引入站点框架，先采用“原地 Markdown + YAML 元数据头 + 自动索引”的方式。

## 目标

让人和复制出来的智能体都能快速判断：

- 哪些文档是当前事实来源。
- 哪些文档是历史资料、草稿或生成物。
- 某个职能应该先读哪些文档。
- 修改某类内容后需要同步哪些文档。
- 文档之间如何形成双向网状关联，尤其是美术、程序、策划之间的交接关系。
- 哪些文档缺少元数据或断开关联，后续需要治理。

## 职责入口唯一归属

- `AGENTS.md`：根职责路由和所有 Agent 都必须遵守的全局硬边界。
- `rules/`：稳定的跨职能工作协议；任务角色、Owner 流水和完成要求统一见 `rules/02_智能体任务路由与完成协议.md`。
- `知识库/views/`：按职能提供阅读顺序和上下文导航，不是专业事实来源，也不记录当前进度。
- `agent_status/`：记录当前关注、最近完成、下一步建议、阻塞和证据入口，不承接稳定规则。
- 剧情、GDD、配置、开发与美术文档：记录对应 Role 的专业事实。

## Active Role 词表

active 文档的 `role` 只允许使用：`全局`、`游戏导演`、`Owner`、`剧情`、`策划`、`程序`、`美术`、`知识库`。

- `Owner` 表示文档由跨职能模块整体责任 Role 维护，例如 T0-01 / T0-01A；Owner 可以切换专业 Role，但 Owner 定位不变化。
- `剧情` 是独立专业 Role，承接剧情大纲、叙事结构、角色关系、对白节奏和 CG / 漫画叙事一致性，不归入策划。
- `策划`、`程序`、`美术` 表示对应专业事实归属；纯 UGUI 属于程序，UI 视觉属于美术。
- `全局` 只用于项目级入口、状态、根规则和知识库规范，不用于代替跨职能模块 Owner。
- 专工、执行者、验收者、自验、回写和 P3 Mission 不是 Role，不能写入 active 文档的 `role` 字段。

## 元数据头格式

核心文档建议在文件开头加入 YAML front matter：

```yaml
---
id: gdd_00
title: 系统关联总图
type: gdd
role: 策划
domain: system_overview
status: active
source_of_truth: true
related:
  - 版本规划/09_正式版核心纵切开发路线.md
  - 开发文档/00_程序开发大纲.md
last_verified: 2026-05-23
update_rule: 修改系统关系、资源流向或优先级时同步本文件。
---
```

## 字段说明

| 字段 | 必填 | 说明 |
|---|---|---|
| `id` | 是 | 全局稳定 ID，建议小写字母、数字和下划线。 |
| `title` | 是 | 文档标题，优先使用正文 H1。 |
| `type` | 是 | 文档类型，如 `entry`、`status`、`gdd`、`dev`、`art`、`plan`、`config`、`tool`。 |
| `role` | 是 | 文档的主要专业归属或整体模块责任归属；active 值必须来自正式 Role 词表。 |
| `domain` | 是 | 领域标签，如 `agent_workflow`、`inventory_combat`、`art_pipeline`。 |
| `status` | 是 | `active`、`historical`、`draft`、`generated`、`deprecated`。 |
| `source_of_truth` | 否 | 是否当前事实来源，布尔值。 |
| `related` | 否 | 关联文档路径列表。关联必须双向维护，形成可校验的文档网。 |
| `last_verified` | 否 | 最近确认日期，格式 `YYYY-MM-DD`。 |
| `update_rule` | 否 | 修改该文档或相关系统时的同步规则。 |

## 推荐状态

- `active`：当前有效文档。
- `historical`：历史验证资料，可参考但不作为当前口径。
- `draft`：草稿或待确认。
- `generated`：由脚本生成，不应手写维护。
- `deprecated`：已废弃，只保留追溯价值。

## 索引文件

索引由脚本生成：

- `DOCS_INDEX.md`：给人阅读的文档索引。
- `docs_index.json`：给智能体和工具读取的机器索引。
- `知识库/views/`：按职能整理的阅读入口，只做上下文导航，不替代事实来源文档。
- `知识库/views/owner.md`：Owner 模块阅读入口，负责按受影响 Role 条件展开。
- `知识库/views/narrative.md`：剧情 Role 阅读入口，负责叙事事实、系统事件和演出规格的条件展开。
- `知识库/views/pm.md`：PM / 版本规划兼容入口，只负责迁移旧链接。
- `tools/agent/README.md`：复制智能体开工前健康检查入口，用于发现工作区和知识库风险。
- 索引会统计文档关联数、跨职能关联数和职能之间的关联边。

生成命令：

```powershell
.\tools\docs\Generate-DocsIndex.ps1
```

校验命令：

```powershell
.\tools\docs\Validate-Docs.ps1
```

## 当前落地策略

1. 当前索引内 Markdown 已补齐元数据头。
2. `related` 采用双向关系：A 关联 B 时，B 也必须关联 A。
3. 美术、程序、策划文档如果存在实现、规则、配置或交付关系，应优先互相关联，而不是只挂到总入口。
4. 生成物目录默认不纳入索引，避免把脚本产物误当作事实来源。
5. `UnityClient/Assets/StreamingAssets/Configs` 是 `配置表(JSON)` 同步出来的运行时副本，不纳入索引、不手写维护。
6. 后续各职能智能体在修改文档时，必须同步检查相关文档元数据和双向关联。
7. 新增、重写、拆分或归档文档前，必须先按 `rules/01_文档维护与新增控制规则.md` 判断是否应补充已有文档、是否需要归档旧文档，以及是否会造成重复事实来源。

## 渐进式阅读层级

复制出来的智能体默认按以下层级读取：

```text
L0 AGENTS.md：选择主责任 Role 和全局硬边界
L1 PROJECT_STATUS.md：读取当前阶段、优先级、阻塞和交接快照
L2 Role View + 受影响状态页：读取职责导航和当前快照
L3 目标事实文档：读取本任务唯一主要事实入口
L4 条件展开：读取受影响职能、rules/02、专项 Skill、验收或恢复材料
```

- `related` 表示文档关系，不表示默认必读。
- `agent_status/*.md` 默认只读当前快照，不通读历史日期日志。
- `agent_status/director.md` 只对游戏导演、Owner 和明确跨职能任务默认展开。
- Owner 使用 `知识库/views/owner.md`；剧情使用 `知识库/views/narrative.md`。两者均为导航入口，不建立新的事实来源或进度表。

## 关联网络规则

- GDD 与对应程序实现文档、配置 README、数值模型需要互链。
- 美术 / UI 文档与视觉资源程序规范、Unity 表现层规范、运行时验收方案需要互链。
- 版本路线、项目状态、职能状态页作为跨职能入口，但不替代领域文档之间的直接关系。
- `知识库/views/` 是复制智能体的职能导航页；导航页可以链接事实来源，但事实口径仍以 GDD、开发文档、美术文档、配置源和状态页为准。
- 历史 MVP 文档可以互链到历史规划或当前参考文档，但不能覆盖当前 GDD、开发文档和美术路线的事实来源地位。
- `.\tools\docs\Validate-Docs.ps1` 会检查关联路径存在、不能自指、不能重复，并且必须双向。
