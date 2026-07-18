---
id: agent_status_readme
title: 智能体状态页说明
type: status
role: 全局
domain: agent_status
status: active
source_of_truth: false
related:
  - AGENTS.md
  - rules/02_智能体任务路由与完成协议.md
  - agent_status/director.md
  - agent_status/pm.md
  - agent_status/program.md
  - agent_status/design.md
  - agent_status/art.md
last_verified: 2026-07-18
update_rule: 新增或调整职能状态页、状态记录字段或状态与稳定规则边界时同步本文件。
---

# 智能体状态页说明

本目录只保存各 active Role 的当前交接快照，不承接稳定工作规则、专业契约、完整执行历史或第二套进度表。

## 状态页入口

- `director.md`：游戏导演当前判断、Owner 级跨职能交接和全局体验阻塞。
- `design.md`：策划、数值、GDD 与配置源当前状态。
- `program.md`：程序、Unity、自动验证与工程边界当前状态。
- `art.md`：美术、UI、素材生产与运行时美术验证当前状态。
- `pm.md`：旧 PM / 版本规划兼容入口，不再作为 active Role。

不新增 Owner 或剧情专属状态页。Owner 更新所有受影响专业状态页；剧情事实写入 `设计文档/剧情/`，影响全局体验时同步导演状态，影响系统规则或配置时同步策划状态。

## 统一结构

active 状态页按以下顺序维护：

1. `最后更新`
2. `当前关注`
3. `最近完成`
4. `下一步建议`
5. `问题 / 阻塞`
6. `关键证据入口`

默认只读取这些当前快照。已经沉淀到事实文档、验收记录或 Git 提交的历史过程从 active 状态页删除；需要追溯时直接读取对应事实来源或 Git 历史，不在本目录建立历史副本。

## 记录边界

- 保留仍影响当前决策的结论、未完成动作、验收边界、阻塞和 `validation_limited:*`。
- `最近完成` 只保留少量仍与当前工作直接相关的结果，不按日期无限追加。
- 稳定跨职能协议统一见 `rules/02_智能体任务路由与完成协议.md`。
- 专业事实回到剧情、GDD、配置、开发或美术文档；状态页只给出简短结论和关键证据入口。
- 每次任务只更新受影响状态页；纯专业任务不因为存在关联关系而改写所有状态页。
