---
id: kb_view_design
title: 策划智能体阅读入口
type: view
role: 策划
domain: agent_context_view
status: active
source_of_truth: false
related:
  - AGENTS.md
  - PROJECT_STATUS.md
  - rules/02_智能体任务路由与完成协议.md
  - agent_status/design.md
  - 知识库/README.md
  - 设计文档/README.md
  - 设计文档/GDD/GDD_00_系统关联总图.md
  - 版本规划/09_正式版核心纵切开发路线.md
  - 版本规划/11_纵切批次与需求文档承接矩阵.md
  - 数值模型设计/00_基准价值与空间本位模型.md
  - 配置表(JSON)/README.md
  - 开发文档/00_程序开发大纲.md
last_verified: 2026-07-18
update_rule: 策划入口文档、GDD、配置源、数值模型或路线变化时同步本文件。
---

# 策划智能体阅读入口

> 本页只负责策划 Role 的阅读顺序和上下文导航；稳定跨职能协议见 `rules/02_智能体任务路由与完成协议.md`，当前进度见 `agent_status/design.md`，专业事实以目标 GDD、配置源和数值模型为准。剧情是独立 Role，不归入本页。

## 必读

1. `PROJECT_STATUS.md` 当前阶段、总优先级、跨职能交接和阻塞快照。
2. `agent_status/design.md` 当前关注、最近完成、下一步和阻塞。
3. `版本规划/09_正式版核心纵切开发路线.md`。
4. `设计文档/GDD/GDD_00_系统关联总图.md`。

## 按任务读取

- 需求承接：读取 `版本规划/11_纵切批次与需求文档承接矩阵.md`。
- 配置工作项、准入和源落地证据：读取 `设计文档/config/26_正式配置设计与填充推进计划.md` 和 `设计文档/config/gates/56_正式配置源落地准入门禁.md`。
- 目标系统：读取目标 `GDD`、数值模型和 `配置表(JSON)/*/README.md`，不要展开无关系统。
- 剧情大纲、对白和叙事结构：转入 `知识库/views/narrative.md`，不要把剧情事实归入策划。

## 验收 / 恢复时

- 配置源落地、Validator、固定样例或配置表现验收时读取对应 `config/gates`、`config/validation` 和交付承接文档。
- 需要跨职能完成口径或 Owner 回写时读取 `rules/02_智能体任务路由与完成协议.md`，并只同步受影响状态页。
- 恢复旧配置判断时回到目标审计、源 JSON 和 Git 历史，不把历史状态日志当作当前事实。

## 常用事实来源

| 任务类型 | 优先读取 |
|---|---|
| 顶层阶段 / 纵切路线 | `版本规划/09_正式版核心纵切开发路线.md` |
| 策划 / 配置工作项状态 | `agent_status/design.md`、`设计文档/config/26_正式配置设计与填充推进计划.md`、`设计文档/config/gates/56_正式配置源落地准入门禁.md` |
| 系统关系 / GDD 入口 | `设计文档/GDD/GDD_00_系统关联总图.md` |
| 数值基准 | `数值模型设计/00_基准价值与空间本位模型.md` |
| 配置意图 / 表结构 | `配置表(JSON)/README.md` 和对应子目录 README |
| 程序落地边界 | `开发文档/00_程序开发大纲.md` 和对应系统开发文档 |
| 视觉表达需求 | `美术文档/10_正式版核心纵切美术路线.md` |

## 边界提醒

- `配置表(JSON)` 是配置版本源；不要把 Unity 运行时副本当成策划源数据。
- `UnityClient/Assets/StreamingAssets/Configs` 是脚本生成副本，进入 Unity 或自动化验证前由 `.\tools\config\Sync-Configs.ps1 -Clean` 刷新。
- 系统规则变化必须回写对应 GDD；配置字段或意图变化必须同步配置 README。
- 如果策划变更影响程序实现或美术表达，需要同步 `PROJECT_STATUS.md` 和目标职能状态页。
- 所有进入开发、配置、表现或自动验收的系统需求，都必须有详细需求文档承接；路线文档、优先级列表或聊天里的一句话不能单独作为开发输入。
- 若当前只有一句功能项，策划智能体先补齐需求文档，再交给程序、美术或配置侧；文档至少说明目标、范围外、规则 / 算法、配置字段、表现需求、Validator / 验收样例和完成判定。
- 策划审计、README 字段口径、任务拆分和 JSON 修改前设计属于策划 / 配置设计证据，不等于配置源完成；只有配置源落地并通过校验后，才能在 `agent_status/design.md` 标记 `配置完成`。
- 程序实现、美术交付和 PM 里程碑不写入 `设计文档/config/26_正式配置设计与填充推进计划.md`；需要进度表时按职能分别写入对应状态页或事实文档。
