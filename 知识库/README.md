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
last_verified: 2026-05-23
update_rule: 调整元数据字段、索引策略或文档治理规则时更新本文件。
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
| `role` | 是 | 主要负责职能，如 `全局`、`策划`、`程序`、`美术`、`知识库`。 |
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
5. 后续各职能智能体在修改文档时，必须同步检查相关文档元数据和双向关联。

## 关联网络规则

- GDD 与对应程序实现文档、配置 README、数值模型需要互链。
- 美术 / UI 文档与视觉资源程序规范、Unity 表现层规范、运行时验收方案需要互链。
- 版本路线、项目状态、职能状态页作为跨职能入口，但不替代领域文档之间的直接关系。
- 历史 MVP 文档可以互链到历史规划或当前参考文档，但不能覆盖当前 GDD、开发文档和美术路线的事实来源地位。
- `.\tools\docs\Validate-Docs.ps1` 会检查关联路径存在、不能自指、不能重复，并且必须双向。
