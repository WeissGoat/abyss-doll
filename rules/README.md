---
id: agent_rules_readme
title: 全局 Agent Rules 入口
type: entry
role: 全局
domain: agent_rules
status: active
source_of_truth: true
related:
  - AGENTS.md
  - PROJECT_STATUS.md
  - 知识库/README.md
  - rules/01_文档维护与新增控制规则.md
  - rules/02_智能体任务路由与完成协议.md
last_verified: 2026-10-01
update_rule: 新增、归档或调整全局 agent 必读规则、任务路由或完成协议时同步本文件。
---

# 全局 Agent Rules 入口

> 根目录 `rules/` 只存放跨职能 agent 必读规则和工作流约束，不存放游戏业务规则。

## 命名边界

- `rules/`：全局 agent 必读规则，约束文档治理、开工流程、完成协议等跨职能工作方式。
- `开发文档/rules/`：程序 / Unity 专项规则，约束架构、表现层、资源接入和编码边界。
- `设计文档/规则卡/`：策划业务规则卡，定义游戏系统的状态机、算法、字段口径和验收样例，不再称为项目 `rules`。

## 当前规则

- `rules/01_文档维护与新增控制规则.md`：新增、重写、拆分、归档或调整项目文档前必读；含设计 / 计划文档的位置与状态规则。
- `rules/02_智能体任务路由与完成协议.md`：定义三条执行道、元工作预算、任务角色选择、Owner 责任流水、设计 → 计划 → 执行顺序、自验 / 外部验收、状态回写和 Git 完成要求。

## 使用规则

1. 开工时先读 `AGENTS.md`，再按任务范围读取本目录中相关规则。
2. 若规则之间冲突，以更具体、更新、且被 `AGENTS.md` 或项目状态页引用的规则为准。
3. 新增全局规则前，先判断是否能补充既有规则；确需新增时同步本 README、`AGENTS.md`、`PROJECT_STATUS.md` 和 `知识库/README.md`。
