---
id: workflow_round2_design
title: 工作流第二轮精简设计
type: design
role: 全局
domain: agent_workflow
status: active
source_of_truth: false
related:
  - AGENTS.md
  - rules/01_文档维护与新增控制规则.md
  - rules/02_智能体任务路由与完成协议.md
  - 知识库/README.md
  - 知识库/views/owner.md
  - 开发文档/rules/04_自动化测试与验收流程规范.md
  - 美术文档/00_美术流水线总览.md
last_verified: 2026-10-01
update_rule: 实施完成后改为 historical；现行规则以 AGENTS.md、rules/02、开发文档/rules/04 与验收 Skill 为准。
---

# 工作流第二轮精简设计

> 2026-10-01 用户在对话中确认三项：去掉 superpowers；Owner 从 Role 改为做法；验收改用标准测试和轻量美术检查。直接在分支 `refactor/workflow-round2` 实施，用户复核后合并。

## 1. 问题

- **两套进度跟踪。** superpowers 和 P3 Mission 都管执行进度。14 份 plan 里有 12 份一个勾都没打，进度实际记在 Mission CSV；已实施的文档里 draft、active、implemented、planned 混在一起。
- **Owner 定位冲突。** Owner 被定义成要和专业 Role 二选一的 Role，实际是跨领域交付的做法。28 个 Mission、226 行任务里没有一行用 Owner。外部验收没写明由谁做，容易变成做事的会话自己验自己。
- **验收框架用得少。** Unity 侧约 30 个文件、12 个自定义 MCP 工具，加一套 RunID 证据目录，几乎只被框架自己的测试用过：真实程序验收约 1 次，美术 0 个 `runtime_validated`，发布 2 次都是测试。55 个 smoke 测试只能靠 AutoTestDaemon 盯 `.test_trigger` 触发。

## 2. 设计

### 2.1 去掉 superpowers（已完成，`07dd5db`）

spec 移到 `docs/specs/`，历史 plan 移到 `docs/plans/`。不再单独写 plan；跨会话执行用定稿 spec 生成 P3 Mission。

### 2.2 Role 与 Owner

- **Role 就是领域**：全局、游戏导演、剧情、策划、程序、美术、知识库。领域决定读哪套 View、写哪个状态页、文档由谁维护。
- **Owner 是做法，不是 Role。** 跨领域交付完整模块或玩家结果时走严格道，按 Owner 流程推进，流程本身不变。
  - 文档 `role` 不再用 Owner：T0 / T1 细案归游戏导演，跨领域 View 归全局，美术方案归美术。
  - Mission 每行写实际领域；旧的 Owner 行按兼容别名提示改成游戏导演，与回写矩阵中"玩家路径、细案状态归导演状态页"一致。
- **外部验收写明由谁做。** 体验、方向、优先级和最终完成由用户验收；剧情、策划、程序、美术的专业复核由做事会话以外的独立会话完成。做事的会话只能声明自验。

### 2.3 验收

- **程序。** smoke 测试包成 Unity Test Framework 的 EditMode 用例：独立测试程序集用反射调用现有 `Run()`，测试本身不改。分类沿用 `program_validation_profiles.json` 的 smoke set。运行顺序是编译、用 Unity MCP 自带的 `run_tests` 运行、读结果。
- **美术。** 轻量检查由四部分组成：
  - 绑定检查脚本：Approved 文件的 GUID 与 Registry 条目一致；
  - 在真实界面截图；
  - Agent 看图评审；
  - Console 没有新错误。

  证据放 `UnityClient/Logs/P3ArtCheck/<RunID>/`，结论一行写进 `agent_status/art.md`。原 ArtRun 框架只留给 `t0_art_seal` 和 `art_regression`。
- **发布。** 删除 `p3-release-validation`。需要发布门禁时用里程碑检查清单，`Merge-P3ReleaseEvidence.ps1` 保留。
- **MCP 环境。** 自定义 MCP 工具保留不删，MCP 环境由用户负责。

## 3. 保留不动

- AutoTestDaemon、`.test_trigger`、P0 脚本和现有自定义工具代码。
- "程序通过不等于美术通过"的声明边界。
- 三条执行道和元工作预算。

## 4. 实施步骤

1. 去掉 superpowers（`07dd5db`）。
2. 本设计稿。
3. Role 与 Owner，涉及：
   - `AGENTS.md`、`rules/02`、`rules/README.md`；
   - `知识库/README.md`、受影响的 View 和 `agent_status/README.md`；
   - `validate_docs.py`、P3 Mission 角色表，以及受影响文档的 `role` 字段。
4. 程序验收：测试程序集与包装、`p3-program-validation`、`开发文档/rules/04`、`tools/agent/README.md`；用 MCP `run_tests` 实测。
5. 美术验收：绑定检查脚本与测试、`p3-art-validation`、`美术文档/00` 中 `runtime_validated` 的定义。
6. 删除 `p3-release-validation`，更新引用。
7. 校验文档、Skill、单元测试和健康检查，然后把本设计稿标为 historical。

## 5. 验收

- 文档校验、Skill 校验和单元测试通过。
- MCP `run_tests` 能发现并运行全部 smoke 用例，失败的用例如实记录。
- 验收 Skill 从 3 个减到 2 个，正文变短。
