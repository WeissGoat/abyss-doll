---
id: lighter_agent_workflow_design
title: Agent 工作流与美术流程减负设计
type: design
role: 全局
domain: agent_workflow
status: active
source_of_truth: false
related:
  - AGENTS.md
  - rules/01_文档维护与新增控制规则.md
  - rules/02_智能体任务路由与完成协议.md
  - 美术文档/00_美术流水线总览.md
last_verified: 2026-10-01
update_rule: 合并后改为 historical；现行规则以 AGENTS.md、rules/01、rules/02 与美术流水线总览为准。
---

# Agent 工作流与美术流程减负设计

> 2026-10-01 经用户授权，直接在分支 `refactor/lighter-agent-workflow` 实施，用户复核后再合并。中小型变更按本设计新增的规则，实施步骤附在文末，不另写 plan。

## 1. 问题

- 7/17 之后游戏运行时代码、Prefab、场景零提交；7-8 月提交集中在美术管线和 agent 工具，优先级第 1 项 T0-01A 没有推进。
- 严格美术流程运行约 3 周，10 个资源到 `registered`，0 个 `runtime_validated`；一次 Catalog 迁移让 308 条 PromptRevision 全部作废。
- 美术生产开工必读约 5 万 token；知识库要求手工维护 1,517 条双向 related。
- 文档规模超出可维护范围：Skill 死链挂了 2.5 个月、"Never" 规则写反、状态页过期，都长期没人发现。

根因有三：所有任务一刀切走最重流程；脚本已经强制的规则又在文档里复述一遍；元工作没有预算约束。

## 2. 目标与非目标

目标：低风险工作走轻流程，高风险工作保留现有门禁；减少每次开工必读和完成必写；进度以玩家结果衡量。

非目标：不改任何美术脚本的门禁逻辑（fail-closed 校验全部保留）；不删除事实文档，不改 Manifest / Catalog 数据；不改 Role 词表和 P3 Mission 机制。

## 3. 设计

### 3.1 三条执行道（rules/02、AGENTS.md）

| 道 | 适用 | 要求 | 回写 |
|---|---|---|---|
| 探索道 | 原型、概念图、玩法 / UI 试做、技术 spike、方案对比 | 产出留在探索区，不碰 Approved、`配置表(JSON)`、正式场景 / Prefab 和 active 事实文档；结论只算探索结论 | 不回写；值得保留的结论转成标准道任务 |
| 标准道（默认） | 常规代码、配置、文档、素材生产与标准资产入库 | 相关测试 / 校验通过，声明不超过证据 | 状态页的当前关注、阻塞或交接变化时改一行 |
| 严格道 | Owner 完整模块、不可逆或高成本操作、核心身份资产、发布门禁 | 现有 Owner 流水、实现切片字段、外部验收 | 完整回写矩阵 |

### 3.2 元工作预算（rules/02、AGENTS.md）

流程、校验器、文档治理、美术管线和 agent 工具只在解阻当前优先级的玩家结果、或修复已发生的问题时做；新增门禁、状态或必填字段要写明它拦住的真实问题；脚本已强制的规则不在文档里复述。开工健康检查显示距最近一次游戏代码 / 配置改动的天数，让漂移可见。

### 3.3 文档治理（rules/01、知识库/README.md、tools/docs）

`related` 改为单向填写，反向链接由索引脚本生成（`docs_index.json` 的 `referenced_by`）；校验器不再要求双向。中小型变更只写 spec 并附实施步骤。

### 3.4 美术快速道 / 完整道（美术文档/00、p3-art-asset-production）

- 快速道：非 UI Skin、非首次风格锚点、且不替换游戏里已绑定资产的 `standard_asset`（图标、背景、普通 UI 图）。复用现有批量脚本，一次 contact sheet 评审覆盖整批，整批一个 `ArtImportRunID`；只在硬失败、事实冲突或缺授权时停；声明上限 `registered`，被游戏消费时再做运行时验收。
- 完整道：`character_portrait_set`、叙事 CG / 漫画 Panel、UI Skin（九宫格）、首次锚点、替换游戏里已绑定的资产。保留全部现有门禁。
- Skill 正文只留目的、分道、阶段阅读表、命令表和硬规则；与参考文档重复的规则删掉，只出现在正文里的规则挪进对应阶段的参考文档。

## 4. 保留不动

不可逆操作保护（Approved 覆盖、`.meta` / GUID、Unity 资产移动）、完成状态分级、核心角色身份与套组门禁、全部脚本 fail-closed 校验、AGENTS.md 全局硬边界。

## 5. 实施步骤

1. `related` 单向与反向链接生成（`0ba28ad`）。
2. 本设计稿。
3. 三条执行道、元工作预算、回写减负：rules/02、rules/01、rules/README.md、AGENTS.md、agent_status/README.md。
4. 美术快速道 / 完整道与 Skill 正文瘦身：美术文档/00、p3-art-asset-production 的 SKILL 与 references。
5. 健康检查游戏进度信号：agent_health_check.py、tools/agent/README.md。
6. 验证：文档索引与校验、Skill 校验、单元测试、健康检查。

## 6. 验收

- 文档校验、Skill 校验、单元测试通过。
- 美术快速道开工必读 token 低于实施前。
- AGENTS.md 不变长。
