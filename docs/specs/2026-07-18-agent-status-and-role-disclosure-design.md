---
id: agent_status_and_role_disclosure_design
title: Agent 状态页与 Role 阅读范围优化设计
type: design
role: 全局
domain: agent_workflow
status: historical
source_of_truth: false
last_verified: 2026-10-01
update_rule: 历史设计记录，不再更新；现行事实以 agent_status/README.md 与 知识库/views/*.md 为准。
---

# Agent 状态页与 Role 阅读范围优化设计

> 历史设计：已于 2026-07-18 实施（`5045175`）。现行事实以 `agent_status/README.md` 与 `知识库/views/*.md` 为准。

## 目标

把当前“Role 已能选对，但进入任务后会加载大量全局和历史上下文”的路由，收敛为真正的渐进式披露：先判断职责，再读取最小必要上下文，只有任务确实涉及跨职能、验收或恢复时才继续展开。

本轮聚焦两件事：

1. 压缩 `agent_status/`，删除已经沉淀到事实文档、当前摘要或 Git 提交中的过时历史。
2. 为每个 active Role 明确必读、条件读取和追溯读取范围，并补齐 Owner / 剧情的入口对称性。

## 非目标

- 不改变 active Role 词表。
- 不改变 Owner、专业执行、Owner 自验、外部验收和状态回写的职责协议。
- 不迁移业务事实文档，不修改 GDD、配置源、程序代码或美术资产。
- 不在本轮引入机器路由配置或改造索引生成器的数据结构。
- 不删除仍承载未完成阻塞、验收边界、当前交接或后续动作的状态信息。

## 设计

### 1. 状态页统一为“当前快照 + 少量最近记录”

active Role 状态页统一保留以下顺序：

1. 最后更新
2. 当前关注
3. 最近完成（只保留仍影响当前判断的少量条目）
4. 下一步建议
5. 问题 / 阻塞
6. 关键证据入口

以下内容从 active 状态页正文移除：

- 已经写入专业事实文档，且不会影响当前决策的历史过程。
- 已被后续结论覆盖的旧验收结果、旧路线判断和旧任务记录。
- 仅用于追溯、但不再影响当前工作的长日期日志。

仍保留：

- 当前未解决的阻塞、`validation_limited:*` 和验收边界。
- 当前仍需要执行或复验的下一步。
- 为理解当前事实不可替代的最近一条上下文。

`agent_status/README.md` 增加该模板和“默认只读当前快照，历史记录按需追溯”的规则。历史内容不另建第二套进度表；需要追溯时回到事实文档、验收记录或 Git 历史。

### 2. 统一 Role 阅读层级

所有 Role 采用以下披露层级：

```text
L0 根路由：AGENTS.md，选择主责任 Role 和全局硬边界
L1 全局快照：PROJECT_STATUS.md 的当前阶段、优先级、阻塞和交接
L2 Role 上下文：对应知识库 View + 对应 agent_status 当前快照
L3 任务事实：目标模块、GDD、剧情、开发、配置或美术事实文档
L4 条件展开：受影响 Role、rules/02、专项 Skill、验收证据或恢复记录
```

规则：

- `agent_status/director.md` 不再对所有 Role 默认必读；仅游戏导演、Owner 或明确涉及全局体验 / 跨职能交接的任务读取。
- 状态页默认读取当前快照，不要求通读历史段落。
- L3 只选择一个主要事实入口开始，不因 `related` 关系网自动展开全部关联文档。
- L4 必须由任务范围触发，不能因为某文档存在关联边就自动读取。

### 3. Role 入口范围

| Role | 必读 | 按任务读取 | 仅验收 / 恢复时读取 |
|---|---|---|---|
| 全局 | `AGENTS.md`、`PROJECT_STATUS.md` 当前快照 | `rules/01`、`rules/02`、全局入口事实 | 历史状态、完整索引图 |
| 游戏导演 | `知识库/views/director.md`、`agent_status/director.md` 当前快照 | `13`、`14`、目标细案、`09`、`11` | 受影响专业状态页和历史验收 |
| Owner | `知识库/views/owner.md`、目标模块入口 | 受影响 Role View / 状态快照、对应事实文档、`rules/02` | 外部验收证据、恢复记录 |
| 剧情 | `知识库/views/narrative.md`、`设计文档/剧情/README.md`、目标剧情事实文档 | `GDD_05`、规则卡 10、内容包 23、叙事播放开发文档 | 叙事运行时验收和历史演出记录 |
| 策划 | `知识库/views/design.md`、`agent_status/design.md` 当前快照 | `09`、`11`、目标 GDD / 数值 / 配置 README | 配置 Validator、固定样例和历史审计 |
| 程序 | `知识库/views/program.md`、`agent_status/program.md` 当前快照 | 程序大纲、架构规则、目标开发文档、相关 GDD / 配置 / 测试入口 | Unity 运行时证据、P0 / smoke 历史 |
| 美术 | `知识库/views/art.md`、`agent_status/art.md` 当前快照 | 美术 README、正式美术路线、目标 UI / Manifest / 资源规格 | Approved、ArtAcceptance 和运行时截图历史 |
| 知识库 | `知识库/README.md`、`agent_status/README.md` | `rules/01`、索引脚本、目标文档元数据和关联边 | 全量索引审计和历史迁移记录 |

其中 Owner / 剧情新增 View，但专业事实仍分别以目标模块文档和 `设计文档/剧情/` 为准；View 只承担阅读顺序和条件分支，不建立新的事实来源。

### 4. 入口文档的职责收敛

- `AGENTS.md`：保留 Role 路由、全局硬边界和 L0/L1/L2 规则，不列出专业文档的完整阅读清单。
- `知识库/views/*.md`：每个 View 只维护本 Role 的必读、条件读取、追溯读取和边界提醒。
- `agent_status/*.md`：只维护当前快照和少量仍有效的最近上下文，不承接稳定规则。
- `rules/02`：继续维护执行、Owner、自验、外部验收和回写协议，不在 View 中重复全文。
- `DOCS_INDEX.md` / `docs_index.json`：继续作为索引和关系图，不作为默认全量阅读清单。

## 实施文件范围

### 修改

- `AGENTS.md`
- `agent_status/README.md`
- `agent_status/director.md`
- `agent_status/design.md`
- `agent_status/program.md`
- `agent_status/art.md`
- `知识库/README.md`
- `知识库/views/director.md`
- `知识库/views/design.md`
- `知识库/views/program.md`
- `知识库/views/art.md`

### 新增

- `知识库/views/owner.md`
- `知识库/views/narrative.md`

### 生成与验证

- 通过 `Generate-DocsIndex.ps1` 刷新 `DOCS_INDEX.md` 和 `docs_index.json`。
- 运行 `Validate-Docs.ps1`，确认 Role 白名单、元数据、双向关系和索引完整性。

## 验收标准

1. active 状态页不再以长日期日志作为默认阅读路径。
2. 每个 active Role 都有明确的必读、条件读取和追溯读取范围。
3. Owner 与剧情都有稳定入口，但没有新增事实来源或第二套进度表。
4. 纯策划、程序、美术任务不再默认要求读取 `agent_status/director.md`。
5. Owner 任务能从模块入口进入，并按受影响 Role 条件展开。
6. 文档索引生成和校验通过，工作区中其他 Agent 的美术改动不被纳入提交。
