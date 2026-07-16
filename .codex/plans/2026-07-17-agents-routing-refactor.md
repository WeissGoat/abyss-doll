# Project P3 Agent Responsibility Routing Refactor Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 `AGENTS.md` 重构为清晰的职责路由入口，并把稳定的跨职能执行与完成协议集中到唯一全局规则文件中。

**Architecture:** 根入口只负责开工顺序、任务分类、全局硬边界和完成摘要；`rules/02_智能体任务路由与完成协议.md` 负责 Owner 流水、验收和回写矩阵；现有职能视图继续负责阅读导航，状态页与领域文档继续分别承担状态和专业事实。

**Tech Stack:** Markdown、YAML front matter、PowerShell、Python 文档索引与校验脚本、Git。

## Global Constraints

- 不覆盖实施前快照 `66fad84` 中已存在的有效规则。
- 不修改 `tools/ai-image-gateway` 子模块内部内容。
- 不纳入 `.superpowers/`、`UnityClient/UnityClient/` 或 `~/`。
- `AGENTS.md` 不记录当前业务进展，不复制职能专项操作细节。
- `rules/02` 只定义跨职能工作方式，不替代 GDD、开发、美术、配置或状态事实。
- 所有新增或调整的知识库文档必须维护 YAML 元数据、双向 `related` 和生成索引。
- 没有新鲜校验证据不得声明重构完成。

---

### Task 1: 建立唯一跨职能任务路由与完成协议

**Files:**
- Create: `rules/02_智能体任务路由与完成协议.md`
- Modify: `rules/README.md`

**Interfaces:**
- Consumes: `.codex/specs/2026-07-17-agents-routing-refactor-design.md`、当前 `AGENTS.md` 的全局工作模式、实现切片承接字段、完成协议与 Git 规范。
- Produces: 根入口可链接的唯一稳定协议；后续 Task 2 删除重复内容时以该文件为承接证据。

- [ ] **Step 1: 从当前 `AGENTS.md` 建立迁移核对清单**

Run:

```powershell
rg -n "全局工作模式|实现切片承接至少包含|完成协议|Git 规范|Owner 自验|外部验收|状态回写" AGENTS.md
```

Expected: 输出当前所有待迁移规则位置，且不修改文件。

- [ ] **Step 2: 新建全局协议文件**

文件必须包含以下完整结构：

```markdown
# 智能体任务路由与完成协议

## 适用范围
## 任务角色与主责任选择
## 标准责任流水
## 实现切片承接字段
## Owner、自验、外部验收与专工
## 防重复派发与完成判定
## 状态与事实回写矩阵
## 验证受限与阻塞记录
## Git 与工作区完成要求
```

YAML 必须使用稳定 ID `rule_agent_task_routing_and_completion_protocol`，`type: rule`、`role: 全局`、`domain: agent_workflow`、`status: active`、`source_of_truth: true`，并与 `AGENTS.md`、`rules/README.md`、`PROJECT_STATUS.md`、`知识库/README.md` 双向关联。

- [ ] **Step 3: 更新全局规则入口**

在 `rules/README.md` 的“当前规则”中登记：

```markdown
- `rules/02_智能体任务路由与完成协议.md`：定义任务角色选择、Owner 责任流水、自验 / 外部验收、状态回写和 Git 完成要求。
```

同时更新 `related`、`last_verified: 2026-07-17` 和 `update_rule`，明确调整任务路由或完成协议时同步本 README。

- [ ] **Step 4: 验证协议覆盖迁移内容**

Run:

```powershell
rg -n "细案 Owner|实现 Owner|专工|玩家结果|范围外|事实来源|允许改动范围|Owner 自验|外部验收|validation_limited|PROJECT_STATUS|git status --short|\.meta" rules/02_智能体任务路由与完成协议.md
```

Expected: 每个关键词均至少命中一次，且规则含义完整。

- [ ] **Step 5: 提交协议基础**

```powershell
git add -- rules/02_智能体任务路由与完成协议.md rules/README.md
git commit -m "docs: centralize agent routing and completion protocol"
```

### Task 2: 将 AGENTS 重写为轻量职责路由入口

**Files:**
- Modify: `AGENTS.md`

**Interfaces:**
- Consumes: Task 1 的 `rules/02`，现有 `知识库/views/*.md`、`PROJECT_STATUS.md`、状态页和项目 skills。
- Produces: 所有 Agent 的唯一根路由入口。

- [ ] **Step 1: 保存当前关键规则语义快照**

Run:

```powershell
rg -n "配置表\(JSON\)|StreamingAssets|纯 UGUI|Approved|_IncomingAI|_generated|ai-image-gateway|p3-mission|image_gen|openai_images|gemini_chat_image|NovelAI|没有验收证据|\.meta" AGENTS.md
```

Expected: 关键硬边界全部可定位，供重写后逐项对照。

- [ ] **Step 2: 重写正文结构**

`AGENTS.md` 正文必须按以下顺序组织：

```markdown
# Project P3 智能体入口
## 入口定位与事实优先级
## 30 秒开工流程
## 任务职责路由
## 全局不可违反边界
## 长任务与项目 Skill 路由
## 验证与完成摘要
## 健康检查
```

任务职责路由表必须覆盖：游戏导演 / 制作人、0-12 小时细案 Owner、实现 Owner、主策 / 数值、主程 / Unity、主美 / UI、UI 程序职责、知识库智能体、P3 Mission。

- [ ] **Step 3: 保留当前新增的美术 Skill 路由语义**

根入口必须保留精简后的路由：

```markdown
- 正式美术资产端到端生产：`p3-art-asset-production`。
- 纯图片生成 / 编辑：`generate-image`。
- 叙事 CG 与漫画播放：`p3-narrative-cg-comic`。
- 新概念图默认 `image_gen`，不可用时转 `openai_images`；局部差分优先 `gemini_chat_image`；NovelAI inpaint 仅用于明确 mask 且允许随机性的场景。
```

不得恢复已经被快照替换掉的旧后端等待规则。

- [ ] **Step 4: 删除根入口中的重复信息**

删除或改为链接：职能长篇必读清单、UI 版本细则、配置准入细则、程序实现细节、工具命令、知识库本轮目标、完整 Owner 流水和完整完成协议。

- [ ] **Step 5: 运行结构断言**

Run:

```powershell
$text = Get-Content -Raw -Encoding UTF8 AGENTS.md
@(
  '# Project P3 智能体入口',
  '## 30 秒开工流程',
  '## 任务职责路由',
  '## 全局不可违反边界',
  'rules/02_智能体任务路由与完成协议.md',
  'p3-art-asset-production',
  'p3-narrative-cg-comic',
  '配置表(JSON)',
  '纯 UGUI',
  '没有验收证据'
) | ForEach-Object { if (-not $text.Contains($_)) { throw "missing: $_" } }
if (($text -split "`r?`n").Count -gt 220) { throw 'AGENTS.md remains too large' }
```

Expected: exit 0，且 `AGENTS.md` 不超过 220 行。

- [ ] **Step 6: 提交轻入口**

```powershell
git add -- AGENTS.md
git commit -m "docs: simplify AGENTS into responsibility router"
```

### Task 3: 对齐职能导航、知识库入口和状态说明

**Files:**
- Modify: `知识库/views/director.md`
- Modify: `知识库/views/art.md`
- Modify: `知识库/views/design.md`
- Modify: `知识库/views/program.md`
- Modify: `知识库/README.md`
- Modify: `agent_status/README.md`

**Interfaces:**
- Consumes: Task 2 的职责路由表与 Task 1 的全局协议。
- Produces: 每个职责路径可落到明确的阅读导航，同时视图和状态页不承担第二套事实来源。

- [ ] **Step 1: 为四个职能视图增加统一定位句**

每个视图在标题后明确：

```markdown
> 本页只负责该职能的阅读顺序和上下文导航；稳定跨职能协议见 `rules/02_智能体任务路由与完成协议.md`，当前进度见对应 `agent_status/*.md`，专业事实以目标领域文档为准。
```

导演视图额外说明细案 Owner 与实现 Owner 的选择入口；美术、策划、程序视图分别保留各自专业事实路由。

- [ ] **Step 2: 更新知识库职责边界**

在 `知识库/README.md` 中明确四类唯一归属：

```markdown
- `AGENTS.md`：根职责路由和全局硬边界。
- `rules/`：稳定的跨职能工作协议。
- `知识库/views/`：阅读导航，不是事实来源。
- `agent_status/` 与领域文档：当前状态与专业事实。
```

- [ ] **Step 3: 更新状态页说明**

在 `agent_status/README.md` 中明确状态页只记录最近完成、当前关注、下一步建议、阻塞和证据入口，不承接稳定工作规则。

- [ ] **Step 4: 维护双向关联和验证日期**

将 `rules/02_智能体任务路由与完成协议.md` 加入上述文档的 `related`，并将 `last_verified` 更新为 `2026-07-17`。确保 `rules/02` 反向列出这些文档。

- [ ] **Step 5: 提交导航对齐**

```powershell
git add -- 知识库/views/director.md 知识库/views/art.md 知识库/views/design.md 知识库/views/program.md 知识库/README.md agent_status/README.md rules/02_智能体任务路由与完成协议.md
git commit -m "docs: align role views with agent router"
```

### Task 4: 回写全局状态与入口同步规则

**Files:**
- Modify: `PROJECT_STATUS.md`
- Modify: `agent_status/director.md`
- Modify: `GEMINI.md`

**Interfaces:**
- Consumes: 已落地的根路由、全局协议和职能视图。
- Produces: 项目级变更记录、导演判断和 AI 上下文入口的一致描述。

- [ ] **Step 1: 更新项目状态摘要**

在 `PROJECT_STATUS.md` 的最近完成中新增事实化记录：Agent 入口已经完成职责路由分层；不改变游戏阶段、T0 状态或业务优先级。将 `rules/02` 加入 `related`。

- [ ] **Step 2: 更新导演状态**

在 `agent_status/director.md` 的最近完成中记录：职责入口已从“所有规则集中在根文件”调整为“根路由 + 全局协议 + 职能视图”；当前 Owner 工作模式未改变。更新最后日期与 `related`。

- [ ] **Step 3: 更新 GEMINI 上下文路由**

将 AI 行为规范中的稳定 Owner 细节改为链接 `rules/02`，保留项目角色定位和专业入口，避免再次复制完整执行协议。

- [ ] **Step 4: 提交状态回写**

```powershell
git add -- PROJECT_STATUS.md agent_status/director.md GEMINI.md
git commit -m "docs: record agent routing architecture update"
```

### Task 5: 生成索引并验证典型任务路由

**Files:**
- Modify generated: `DOCS_INDEX.md`
- Modify generated: `docs_index.json`

**Interfaces:**
- Consumes: Tasks 1-4 的最终文档关系。
- Produces: 可校验的索引、健康检查结果和职责路由验收证据。

- [ ] **Step 1: 重新生成知识库索引**

Run:

```powershell
.\tools\docs\Generate-DocsIndex.ps1
```

Expected: exit 0，`DOCS_INDEX.md` 和 `docs_index.json` 更新。

- [ ] **Step 2: 运行知识库校验**

Run:

```powershell
.\tools\docs\Validate-Docs.ps1
```

Expected: 输出 `[docs] validation passed`，无断链、重复 ID 或缺失元数据。

- [ ] **Step 3: 验证七个任务路由样例**

Run:

```powershell
rg -n "T0-T4|实现 Owner|主策 / 数值|主程 / Unity|主美 / UI|知识库智能体|P3 Mission" AGENTS.md
rg -n "Owner 自验|外部验收|validation_limited|状态与事实回写矩阵" rules/02_智能体任务路由与完成协议.md
```

Expected: 七类职责均能从根入口定位，完成与限制规则均在唯一协议中定位。

- [ ] **Step 4: 运行 Agent 健康检查**

Run:

```powershell
.\tools\agent\Invoke-AgentHealthCheck.ps1
```

Expected: 本次重构不新增 ERROR；允许继续报告实施前已经存在的脏 submodule 和排除目录 WARN。

- [ ] **Step 5: 检查最终差异与根入口规模**

Run:

```powershell
git diff --check
(Get-Content -LiteralPath AGENTS.md -Encoding UTF8).Count
git status --short
```

Expected: 本次新增或修改的 Markdown 无新增格式错误；`AGENTS.md` 不超过 220 行；状态只包含本任务文件和明确排除的既有本地路径。

- [ ] **Step 6: 提交索引与验证结果**

```powershell
git add -- DOCS_INDEX.md docs_index.json
git commit -m "docs: regenerate index for agent routing refactor"
```

- [ ] **Step 7: 最终提交审计**

Run:

```powershell
git log --oneline -7
git status --short
```

Expected: 包含实施前 checkpoint、设计稿和本计划定义的分步提交；工作区只剩明确排除的既有路径。
