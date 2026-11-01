---
id: agent_status_and_role_disclosure_plan
title: Agent 状态页与 Role 阅读范围优化实施计划
type: plan
role: 全局
domain: agent_workflow
status: historical
source_of_truth: false
last_verified: 2026-10-01
update_rule: 历史计划记录，不再更新；现行事实以 agent_status/README.md 与 知识库/views/*.md 为准。
---

# Agent 状态页与 Role 阅读范围优化 Implementation Plan

> 历史计划：已于 2026-07-18 实施（`5045175`）。现行事实以 `agent_status/README.md` 与 `知识库/views/*.md` 为准。

**Goal:** 压缩 active Agent 状态页的历史噪声，并为全部 active Role 建立明确的必读、条件读取和追溯读取范围。

**Architecture:** 保留 `AGENTS.md` 作为 L0 根路由；把全局状态、Role View、角色状态快照和任务事实文档按层级拆开。新增 Owner / 剧情 View 只承担导航，不建立事实来源；索引继续由现有脚本生成。

**Tech Stack:** Markdown、YAML front matter、PowerShell 文档索引脚本、`Validate-Docs.ps1`。

## Global Constraints

- 不改变 active Role 词表和 `rules/02_智能体任务路由与完成协议.md` 的执行 / 验收协议。
- 不删除仍承载当前阻塞、验收边界、交接或下一步动作的状态信息。
- 不修改 GDD、配置源、程序代码、美术资产或其他并发工作区改动。
- 只暂存本计划列出的文件以及由文档索引脚本生成的 `DOCS_INDEX.md`、`docs_index.json`。
- 完成前运行 `Invoke-AgentHealthCheck.ps1`、`Generate-DocsIndex.ps1` 和 `Validate-Docs.ps1`。

---

### Task 1: 收敛状态页模板并压缩历史

**Files:**
- Modify: `agent_status/README.md`
- Modify: `agent_status/director.md`
- Modify: `agent_status/design.md`
- Modify: `agent_status/program.md`
- Modify: `agent_status/art.md`

**Interfaces:**
- Consumes: 当前状态页中的最后更新、当前关注、最近完成、下一步、问题 / 阻塞和证据入口。
- Produces: 每个 active Role 状态页的统一当前快照结构；不新增历史台账。

- [ ] **Step 1: 建立状态页保留清单**

  对每个状态页逐段检查：保留当前阻塞、未完成下一步、验收边界和仍影响当前决策的最近上下文；删除已沉淀到事实文档或被后续结论覆盖的旧日期日志。

- [ ] **Step 2: 更新状态页 README 模板**

  在 `agent_status/README.md` 写明统一章节顺序，并明确默认只读当前快照，历史追溯回到事实文档、验收记录或 Git 历史。

- [ ] **Step 3: 压缩导演、策划、程序和美术状态页**

  保留各职能当前有效事实和证据入口，移除过时的长日志；不改变事实文档中的项目结论。

- [ ] **Step 4: 检查状态页边界**

  运行：

  ```powershell
  rg -n "validation_limited:|问题 / 阻塞|下一步建议|当前关注|最近完成" agent_status
  ```

  预期：每个 active 状态页都有当前快照章节，未解决限制和后续动作仍可追踪。

### Task 2: 新增 Owner 与剧情 Role View

**Files:**
- Create: `知识库/views/owner.md`
- Create: `知识库/views/narrative.md`

**Interfaces:**
- Consumes: `AGENTS.md` 路由表、目标模块文档、`设计文档/剧情/README.md` 和 `rules/02_智能体任务路由与完成协议.md`。
- Produces: Owner / 剧情的 L2 阅读入口，均不承载专业事实或进度台账。

- [ ] **Step 1: 写 Owner View**

  固定顺序为：`PROJECT_STATUS.md` 当前快照 -> 目标模块入口 -> Owner 承接字段 -> 受影响 Role View / 状态快照 -> 目标事实文档 -> `rules/02` 与验收证据（按需）。

- [ ] **Step 2: 写剧情 View**

  固定顺序为：`PROJECT_STATUS.md` 当前快照 -> `设计文档/剧情/README.md` -> 目标剧情事实 -> `GDD_05` / 规则卡 10 / 内容包 23 / 叙事播放开发文档（按任务展开）。

- [ ] **Step 3: 写元数据和双向关系**

  两个 View 使用 `type: view`、对应 active Role、`domain: agent_context_view`，并维护与 `AGENTS.md`、知识库 README、目标入口的双向 `related` 关系。

### Task 3: 优化根路由和既有 Role View

**Files:**
- Modify: `AGENTS.md`
- Modify: `知识库/README.md`
- Modify: `知识库/views/director.md`
- Modify: `知识库/views/design.md`
- Modify: `知识库/views/program.md`
- Modify: `知识库/views/art.md`

**Interfaces:**
- Consumes: Task 1 的状态页模板和 Task 2 的 Owner / 剧情入口。
- Produces: 根路由与所有 View 对 L0-L4 披露层级使用同一口径。

- [ ] **Step 1: 修改 30 秒开工流程**

  保留读取 `PROJECT_STATUS.md` 当前快照；将 `agent_status/director.md` 改为游戏导演、Owner 或明确跨职能任务的条件读取。

- [ ] **Step 2: 修改 Role 路由表**

  Owner 首读 `知识库/views/owner.md`；剧情首读 `知识库/views/narrative.md`；既有专业 Role 继续使用各自 View。

- [ ] **Step 3: 给每个 View 增加三层阅读范围**

  明确 `必读`、`按任务读取`、`验收 / 恢复时读取`，并删除与根入口和 `rules/02` 重复的完整协议段落。

- [ ] **Step 4: 更新知识库规范**

  在 `知识库/README.md` 记录 L0-L4 层级和“related 不等于必读”的规则；确认 View 只做导航，不建立新的事实来源。

### Task 4: 生成索引并验证文档治理

**Files:**
- Generated: `DOCS_INDEX.md`
- Generated: `docs_index.json`

**Interfaces:**
- Consumes: Task 1-3 的 Markdown 和 YAML 元数据。
- Produces: 反映新 View 和压缩后文档的索引、关系计数与 Role 统计。

- [ ] **Step 1: 运行健康检查**

  运行：

  ```powershell
  .\tools\agent\Invoke-AgentHealthCheck.ps1
  ```

  预期：允许工作区已有 WARN，但确认本任务文件可单独暂存。

- [ ] **Step 2: 生成索引**

  运行：

  ```powershell
  .\tools\docs\Generate-DocsIndex.ps1
  ```

- [ ] **Step 3: 校验索引**

  运行：

  ```powershell
  .\tools\docs\Validate-Docs.ps1
  ```

  预期：无 active Role 白名单错误、无断链、无非双向关联；允许保留既有 `missing_metadata=2`，若数字变化必须说明原因。

- [ ] **Step 4: 检查任务边界**

  运行：

  ```powershell
  git status --short
  git diff --check
  ```

  预期：仅本计划文件和索引生成物被暂存，其他美术工具、生成物、submodule 和本地目录保持未暂存。

### Task 5: 提交文档改动

**Files:**
- Commit only: Task 1-4 files listed above.

- [ ] **Step 1: 暂存本任务文件**

  使用明确路径执行 `git add`，不使用 `git add -A`。

- [ ] **Step 2: 创建提交**

  ```powershell
  git commit -m "docs: streamline agent status and role disclosure"
  ```

- [ ] **Step 3: 记录验证结果**

  在最终摘要中区分验证通过、既有 warning 和未纳入提交的并发改动。
