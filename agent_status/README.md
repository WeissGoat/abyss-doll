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
  - agent_status/director.md
  - agent_status/pm.md
  - agent_status/program.md
  - agent_status/design.md
  - agent_status/art.md
last_verified: 2026-05-23
update_rule: 新增或调整职能状态页时同步本文件。
---

# 智能体状态页说明

本目录存放不同职能智能体的交接状态。

- `director.md`：游戏导演 / 制作人 / 玩家体验主线 / 系统开放节奏 / 任务 Owner 工作流状态。
- `pm.md`：旧 PM / 版本规划兼容入口，不再作为 active 工作角色。
- `art.md`：美术 / UI / 视觉流水线状态。
- `design.md`：策划 / 数值 / GDD / 配置意图状态。
- `program.md`：程序 / Unity / 验证状态。

每个复制出来的智能体完成一次有实际意义的任务后，应更新所有受影响状态页。任务 Owner 负责跨职能回写；主策、主程、主美默认负责验收和总体设计状态。记录要简洁、稳定，让下一个智能体不读聊天记录也能继续接手。
