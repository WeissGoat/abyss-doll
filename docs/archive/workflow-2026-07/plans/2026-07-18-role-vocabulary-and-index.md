---
id: role_vocabulary_and_index_plan
title: Project P3 Role Vocabulary and Knowledge Index Implementation Plan
type: plan
role: 全局
domain: agent_workflow
status: historical
source_of_truth: false
related:
  - docs/specs/2026-07-18-role-vocabulary-and-index-design.md
last_verified: 2026-10-01
update_rule: 历史实施计划，不再更新；现行 Role 词表以 AGENTS.md 与 知识库/README.md 为准。
---

# Project P3 Role Vocabulary and Knowledge Index Implementation Plan

> 历史计划：已于 2026-07-18 执行（`f3c1865`）。现行 Role 词表以 `AGENTS.md` 与 `知识库/README.md` 为准。

**Goal:** 将 Owner、剧情、策划、程序、美术等 Role 概念统一到根路由、active 文档元数据和知识索引，并自动阻止旧 Role 重新进入 active 文档。

**Architecture:** `AGENTS.md` 和 `rules/02` 定义 Role 语义；领域入口说明如何进入各 Role；active 文档 front matter 使用标准词表；`tools/docs/validate_docs.py` 对 active Role 做机器校验；生成索引展示最终归属。

**Tech Stack:** Markdown、YAML front matter、Python、PowerShell、Git。

## Global Constraints

- 正式 active Role 仅允许：`全局`、`游戏导演`、`Owner`、`剧情`、`策划`、`程序`、`美术`、`知识库`。
- `Owner` 是跨职能模块整体责任 Role，可以在处理中切换专业视角。
- `剧情` 是独立 Role，不归入策划。
- `专工`、执行、自验、验收、回写和 P3 Mission 不属于 Role。
- 不改变 Owner 责任流水、执行 / 验收流程、状态回写矩阵和当前业务状态。
- 历史 / archived / 兼容文档可以保留旧 Role 用于追溯；校验只阻止 active 文档使用旧 Role。
- 不触碰当前工作区中的美术工具、Skill、生成物或 `tools/ai-image-gateway` 改动。

---

### Task 1: 为 active Role 增加机器校验

**Files:**
- Modify: `tools/docs/validate_docs.py`

**Interfaces:**
- Consumes: `docs_index.json` 中每个文档的 `role` 和 `status`。
- Produces: active 文档 Role 词表错误，供元数据迁移验证使用。

- [ ] **Step 1: 增加标准 active Role 常量**

在 `CORE_DOCS` 后增加：

```python
ACTIVE_ROLES = {
    "全局",
    "游戏导演",
    "Owner",
    "剧情",
    "策划",
    "程序",
    "美术",
    "知识库",
}
```

- [ ] **Step 2: 增加 active Role 校验**

在 metadata / ID 校验之后增加：

```python
    for doc in docs:
        if doc.get("status") != "active":
            continue
        role = doc.get("role")
        if role not in ACTIVE_ROLES:
            errors.append(f"active doc uses unsupported role '{role}': {doc['path']}")
```

- [ ] **Step 3: 运行校验并确认预期失败**

Run:

```powershell
.\tools\docs\Validate-Docs.ps1
```

Expected: FAIL，并列出 active 文档中的 `0-12 小时细案 Owner`、`实现 Owner` 和 `剧情 Owner`；不得出现其他新错误。

### Task 2: 迁移 active 文档 Role 元数据

**Files:**
- Modify: `版本规划/14_0-12小时候选主循环细案设计.md`
- Modify: `版本规划/0-12小时细案/T0-01_序章首次循环.md`
- Modify: `版本规划/0-12小时细案/T1_第一层搜打撤成形.md`
- Modify: `版本规划/0-12小时细案/T0-01_序章首次循环开发总方案.md`
- Modify: `版本规划/0-12小时细案/T0-01A_开局人偶状态到首次下潜许可实现设计.md`
- Modify: `版本规划/0-12小时细案/T0-01A_开局人偶状态到首次下潜许可开发方案.md`
- Modify: `版本规划/0-12小时细案/T0-VAL-01_最终实际效果优化文档.md`
- Modify: `设计文档/剧情/README.md`
- Modify: `设计文档/剧情/00_剧情大纲.md`
- Modify: `设计文档/剧情/01_序章演出与对话节奏.md`

**Interfaces:**
- Consumes: Task 1 的 active Role 词表。
- Produces: active 文档只使用标准 Role；历史拆分稿保持不变。

- [ ] **Step 1: 迁移游戏导演与 Owner 文档**

应用以下精确映射：

```text
版本规划/14_0-12小时候选主循环细案设计.md -> role: 游戏导演
版本规划/0-12小时细案/T0-01_序章首次循环.md -> role: Owner
版本规划/0-12小时细案/T1_第一层搜打撤成形.md -> role: Owner
版本规划/0-12小时细案/T0-01_序章首次循环开发总方案.md -> role: Owner
版本规划/0-12小时细案/T0-01A_开局人偶状态到首次下潜许可实现设计.md -> role: Owner
版本规划/0-12小时细案/T0-01A_开局人偶状态到首次下潜许可开发方案.md -> role: Owner
版本规划/0-12小时细案/T0-VAL-01_最终实际效果优化文档.md -> role: Owner
```

只修改 front matter 的 `role` 与 `last_verified: 2026-07-18`；不改正文业务内容。

- [ ] **Step 2: 迁移剧情文档**

以下文档统一改为 `role: 剧情` 和 `last_verified: 2026-07-18`：

```text
设计文档/剧情/README.md
设计文档/剧情/00_剧情大纲.md
设计文档/剧情/01_序章演出与对话节奏.md
```

- [ ] **Step 3: 确认 active 旧 Role 已清零**

Run:

```powershell
rg -n "^role: (0-12 小时细案 Owner|实现 Owner|剧情 Owner|主策|主程|主美|UI 程序)$" --glob '*.md'
```

Expected: 只允许命中 historical / archived 文档，不得命中 active 文档。

### Task 3: 更新 Role 概念和阅读入口

**Files:**
- Modify: `AGENTS.md`
- Modify: `rules/02_智能体任务路由与完成协议.md`
- Modify: `知识库/README.md`
- Modify: `知识库/views/director.md`
- Modify: `知识库/views/art.md`
- Modify: `知识库/views/design.md`
- Modify: `知识库/views/program.md`
- Modify: `agent_status/README.md`
- Modify: `GEMINI.md`

**Interfaces:**
- Consumes: 标准 Role 词表和 Task 2 的文档归属。
- Produces: 人工阅读入口与机器索引使用相同 Role 语义。

- [ ] **Step 1: 更新 AGENTS Role 路由**

`AGENTS.md` 必须：

```text
增加 Owner Role 行：完整模块或玩家结果的跨职能整体负责。
增加 剧情 Role 行：剧情大纲、叙事结构、对白、CG / 漫画一致性。
保留 游戏导演、策划、程序、美术、知识库。
删除 0-12 小时细案 Owner、实现 Owner、UI 程序职责和 P3 Mission 的 Role 表行。
在表后明确：Owner 可切换专业 Role；专工和 P3 Mission 不是 Role。
```

P3 Mission 继续留在“长任务与项目 Skill 路由”中。

- [ ] **Step 2: 更新 rules/02 Role 定义**

只修改 Role 概念相关部分：

```text
Owner = 完整模块 / 玩家结果的跨职能整体责任 Role。
剧情 = 独立专业 Role。
策划 / 程序 / 美术 = 专业 Role。
专工 = 某个 Role 工作的独立委派方式，不是 Role。
执行 / 自验 / 外部验收 / 回写 = 环节，不是 Role。
```

将正文中的 `实现 Owner` 统一改为 `Owner`；将 `细案 Owner` 改为“Owner 承接细案任务时”；将 `主策 / 主程 / 主美` 改为 `策划 / 程序 / 美术`。不改变流程顺序和完成条件。

- [ ] **Step 3: 更新知识库规范**

在 `知识库/README.md` 增加 active Role 词表，并明确：

```markdown
`role` 表示文档主要专业归属或整体模块责任归属。
Owner、剧情可以作为 role 值。
专工、执行者、验收者、P3 Mission 不能作为 role 值。
```

- [ ] **Step 4: 更新职能视图和状态说明**

```text
director.md：增加 Owner 与剧情入口；删除实现 Owner / 细案 Owner 作为 Role 的表达。
art.md：统一称美术 Role。
design.md：统一称策划 Role，并明确剧情不归入策划。
program.md：统一称程序 Role，UI 程序属于程序专业范围。
agent_status/README.md：不新增 Owner / 剧情专属状态页；Owner 回写受影响职能状态，剧情事实以剧情文档为入口。
```

- [ ] **Step 5: 更新 GEMINI Role 说明**

将角色定位收敛为游戏导演、Owner、剧情、策划、程序、美术、知识库，并说明 Owner 可切换专业 Role；不复制执行 / 验收流程。

- [ ] **Step 6: 检查旧 Role 文案**

Run:

```powershell
rg -n "主策|主程|主美|实现 Owner|细案 Owner|剧情 Owner|UI 程序职责" AGENTS.md rules/02_智能体任务路由与完成协议.md 知识库/README.md 知识库/views agent_status/README.md GEMINI.md
```

Expected: 不再把这些词作为 active Role；若在历史说明中保留，必须明确标注为旧称谓。

### Task 4: 回写 Role 迭代状态

**Files:**
- Modify: `PROJECT_STATUS.md`
- Modify: `agent_status/director.md`

**Interfaces:**
- Consumes: Tasks 1-3 的最终 Role 模型。
- Produces: 项目级和导演状态中的最小事实记录，不改变业务阶段。

- [ ] **Step 1: 更新 PROJECT_STATUS**

在最近完成顶部增加 2026-07-18 记录：Role 词表已统一，Owner 与剧情成为正式 Role，旧 Owner 变体退出 active 索引；不改变项目阶段和业务优先级。

- [ ] **Step 2: 更新 director 状态**

更新 `最后更新`、`last_verified` 和最近完成，说明 Owner 是跨职能模块责任 Role，剧情是独立专业 Role；原执行 / 验收流程未调整。

### Task 5: 生成索引并完成验证

**Files:**
- Modify generated: `DOCS_INDEX.md`
- Modify generated: `docs_index.json`

**Interfaces:**
- Consumes: Tasks 1-4 的文档和元数据。
- Produces: 新 Role 分类索引与最终校验证据。

- [ ] **Step 1: 重新生成索引**

Run:

```powershell
.\tools\docs\Generate-DocsIndex.ps1
```

Expected: exit 0，索引中的 active Role 包含 `Owner` 与 `剧情`。

- [ ] **Step 2: 运行知识库校验**

Run:

```powershell
.\tools\docs\Validate-Docs.ps1
```

Expected: `[docs] validation passed`；active 文档没有 unsupported role。

- [ ] **Step 3: 验证 Role 统计**

Run:

```powershell
$index = Get-Content -Raw -LiteralPath docs_index.json -Encoding UTF8 | ConvertFrom-Json
$index.documents | Where-Object status -eq 'active' | Group-Object role | Sort-Object Name | Select-Object Name,Count
```

Expected: active Role 只来自标准词表，且 `Owner`、`剧情` 均至少有一份文档。

- [ ] **Step 4: 运行格式与健康检查**

Run:

```powershell
git diff --check
.\tools\agent\Invoke-AgentHealthCheck.ps1
```

Expected: 本次文件无格式错误；健康检查不新增 ERROR，允许继续报告现有美术改动和 submodule WARN。

- [ ] **Step 5: 提交 Role 迭代**

只暂存本计划列出的文件：

```powershell
git status --short
git add -- <本计划列出的 Role 与索引文件>
git commit -m "docs: normalize project roles and knowledge index"
```
