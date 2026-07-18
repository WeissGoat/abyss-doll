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

本目录存放不同职能智能体的交接状态。

- `director.md`：游戏导演 / 制作人 / 玩家体验主线 / 系统开放节奏 / 任务 Owner 工作流状态。
- `pm.md`：旧 PM / 版本规划兼容入口，不再作为 active 工作角色。
- `art.md`：美术 / UI / 视觉流水线状态。
- `design.md`：策划 / 数值 / GDD / 配置意图状态。
- `program.md`：程序 / Unity / 验证状态。

状态页只记录 `最后更新`、`最近完成`、`当前关注`、`下一步建议`、`问题 / 阻塞` 和关键证据入口，不承接稳定工作规则、专业契约或第二套进度表。稳定跨职能协议统一见 `rules/02_智能体任务路由与完成协议.md`，专业事实回到目标领域文档。

不新增 Owner 或剧情专属状态页。Owner 回写所有受影响职能状态页；剧情专业事实写入 `设计文档/剧情/`，涉及全局体验时同步导演状态，涉及系统规则或配置时同步策划状态。

每个复制出来的智能体完成一次有实际意义的任务后，应更新所有受影响状态页。记录要简洁、事实化、可延续，让下一个智能体不读聊天记录也能继续接手。
