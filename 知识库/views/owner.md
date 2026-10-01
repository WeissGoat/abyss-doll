---
id: kb_view_owner
title: 跨领域交付（Owner 做法）阅读入口
type: view
role: 全局
domain: agent_context_view
status: active
source_of_truth: false
related:
  - AGENTS.md
  - 知识库/README.md
  - 知识库/views/director.md
last_verified: 2026-07-18
update_rule: 跨领域交付入口、阅读顺序或条件展开规则变化时同步本文件。
---

# 跨领域交付（Owner 做法）阅读入口

> 本页是按 Owner 做法跨领域交付完整模块或玩家结果时的阅读顺序。Owner 是做法，不是 Role，没有独立状态页；当前状态回到目标模块文档和所有受影响专业状态页，稳定执行 / 验收协议见 `rules/02_智能体任务路由与完成协议.md`。

## 必读

1. `PROJECT_STATUS.md` 的当前阶段、优先级、阻塞和交接快照。
2. 目标模块的唯一入口文档，例如 T0-01 开发总方案、T0-01A 开发方案或目标系统完整方案。
3. 模块文档中的玩家结果、范围外、事实来源、允许改动范围、受影响 Role、验证证据和状态回写要求。

Owner 应先确认“本轮对哪个完整玩家结果负责”，再决定需要展开哪些专业上下文。不要先通读全部专业手册和状态历史。

## 按任务读取

| 受影响范围 | 读取入口 | 继续读取 |
|---|---|---|
| 玩家体验、开放节奏、模块完成口径 | `知识库/views/director.md`、`agent_status/director.md` 当前快照 | `13`、`14`、目标细案、必要的 `09` / `11` |
| 剧情结构、对白、角色关系、CG / 漫画叙事一致性 | `知识库/views/narrative.md` | 目标剧情事实、相关演出规格 |
| GDD、规则、数值、配置意图或配置源 | `知识库/views/design.md`、`agent_status/design.md` 当前快照 | 目标 GDD、规则卡、数值模型、配置 README |
| Unity、领域服务、纯 UGUI、Validator 或自动测试 | `知识库/views/program.md`、`agent_status/program.md` 当前快照 | 目标开发文档、代码、配置和测试入口 |
| UI 视觉、素材、Manifest、VisualID 或运行时美术 | `知识库/views/art.md`、`agent_status/art.md` 当前快照 | 目标美术规格、active UI、Manifest 和验收入口 |

只读取实际受影响的领域。关联关系、README 列表或索引中的其他职能不自动成为必读。

## 验收 / 恢复时读取

- 进入执行、自验、外部验收或状态回写时读取 `rules/02_智能体任务路由与完成协议.md`。
- 长期、拆分、恢复或持续执行时使用 P3 Mission，并读取已有 Mission 来源材料和当前任务状态。
- 程序、美术或发布验证分别进入 `p3-program-validation`、`p3-art-validation`、`p3-release-validation`。
- 叙事 CG / 漫画页生产与运行时验收进入 `p3-narrative-cg-comic`。

## 边界提醒

- 做事的会话可以切换领域视角，但每个领域的事实和状态写回各自文档。
- 自验只证明完整玩家路径已有可复核证据；外部验收由用户或独立会话完成，见 `rules/02_智能体任务路由与完成协议.md`。
- 必须更新所有受影响领域的状态页和事实文档；不建立 Owner 专属进度副本。
- 未完成详细设计的时长段、聊天结论或模糊 TODO 不能单独作为正式实现依据。
