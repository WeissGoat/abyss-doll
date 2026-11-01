---
id: agent_routing_p0_hardening_plan
title: Agent 路由 P0 治理加固实施计划
type: plan
role: 知识库
domain: agent_workflow
status: historical
source_of_truth: false
last_verified: 2026-10-01
update_rule: 历史计划记录，不再更新；现行事实以 AGENTS.md、rules/02_智能体任务路由与完成协议.md 与 tools/docs/validate_docs.py 为准。
---

# Agent Routing P0 Hardening Implementation Plan

> 历史计划：已于 2026-07-18 实施（`23847c6`）。现行事实以 `AGENTS.md`、`rules/02_智能体任务路由与完成协议.md` 与 `tools/docs/validate_docs.py` 为准。

**Goal:** 压缩项目级状态入口、自动阻止状态 / View 结构退化，并清零现有文档元数据缺口。

**Architecture:** `PROJECT_STATUS.md` 继续作为唯一 L1 项目快照；`validate_docs.py` 在现有索引与关联校验之外增加正文结构门禁。校验规则拆成可单测纯函数，两份旧计划只补 YAML 元数据。

**Tech Stack:** Markdown、YAML front matter、Python 3、`unittest`、PowerShell 文档索引脚本。

## Global Constraints

- 不改变 active Role、Owner 流水、执行 / 验收协议或项目业务优先级。
- `PROJECT_STATUS.md` 正文最多 80 行，不计算 YAML front matter。
- active 状态页正文最多 80 行并禁止日期日志标题。
- active Role View 必须保留三层阅读结构。
- 两份旧计划只补 YAML，不修改实施内容。
- 只暂存本计划列出的文件和索引生成物；不包含并发美术改动、生成物或 submodule。

---

### Task 1: 为治理规则建立失败测试

**Files:**
- Create: `tools/docs/tests/test_validate_docs.py`
- Modify: `tools/docs/validate_docs.py`

**Interfaces:**
- Produces: `second_level_headings(body)`, `validate_required_headings(path, body, required)`, `validate_body_line_limit(path, body, limit)`, `validate_no_dated_log_headings(path, body)`。

- [ ] **Step 1: 写状态页和 View 的失败测试**

  测试合法快照、缺章节、超过 80 行、日期日志标题、合法 View 和缺三层 View。

- [ ] **Step 2: 运行测试确认失败**

  ```powershell
  python -m unittest tools.docs.tests.test_validate_docs -v
  ```

  预期：因辅助函数尚不存在而失败。

- [ ] **Step 3: 实现最小纯函数和主校验接入**

  保留现有校验，新增显式 `ACTIVE_STATUS_DOCS`、`ACTIVE_ROLE_VIEWS`、必需标题和正文行数门禁。

- [ ] **Step 4: 运行单元测试确认通过**

  ```powershell
  python -m unittest tools.docs.tests.test_validate_docs -v
  ```

  预期：全部测试通过。

### Task 2: 压缩 PROJECT_STATUS 当前快照

**Files:**
- Modify: `PROJECT_STATUS.md`

**Interfaces:**
- Consumes: 当前阶段、顶层目标、有效优先级、跨职能交接、阻塞和下一步。
- Produces: 不超过 80 行正文的唯一 L1 项目快照。

- [ ] **Step 1: 保留现有 YAML 和当前有效事实**

  删除按日期增长的完成日志和已经被后续判断覆盖的旧通过 / 否决记录。

- [ ] **Step 2: 重写为八个固定章节**

  `最后更新 / 当前阶段 / 当前顶层目标 / 当前优先级 / 跨职能交接 / 问题与阻塞 / 下一步总建议 / 关键入口`。

- [ ] **Step 3: 验证正文体量和日期日志**

  ```powershell
  python -m unittest tools.docs.tests.test_validate_docs -v
  .\tools\docs\Validate-Docs.ps1
  ```

### Task 3: 补齐两份计划文档元数据

**Files:**
- Modify: `docs/plans/2026-07-18-unified-art-processing-rounds.md`
- Modify: `docs/plans/2026-07-18-zero-dialogue-neutral-character-portrait-pilot.md`

- [ ] **Step 1: 添加 YAML front matter**

  使用稳定 ID、原标题、`type: plan`、`role: 美术`、主题 domain、`status: active`、`source_of_truth: false`、验证日期和更新规则。

- [ ] **Step 2: 确认正文内容未变化**

  除文件头新增 YAML 外，不修改原实施计划正文。

### Task 4: 生成索引、严格验证并提交

**Files:**
- Generated: `DOCS_INDEX.md`
- Generated: `docs_index.json`

- [ ] **Step 1: 运行完整验证**

  ```powershell
  python -m unittest tools.docs.tests.test_validate_docs -v
  python -m py_compile tools/docs/validate_docs.py
  .\tools\docs\Generate-DocsIndex.ps1
  .\tools\docs\Validate-Docs.ps1
  git diff --check
  ```

  预期：单元测试通过，文档校验输出 `missing_metadata=0`。

- [ ] **Step 2: 运行严格健康检查**

  ```powershell
  .\tools\agent\Invoke-AgentHealthCheck.ps1 -Strict
  ```

  工作区既有无关改动可以导致 WARN / 非零；必须确认本任务目标文件校验通过并精确暂存。

- [ ] **Step 3: 精确暂存和提交**

  ```powershell
  git commit -m "docs: harden progressive disclosure p0"
  ```
