---
id: p3_mission_readme
title: P3 Mission 长任务协议
type: tool
role: 全局
domain: agent_workflow
status: active
source_of_truth: true
related:
  - AGENTS.md
  - tools/p3-mission/SKILL.md
  - tools/p3-mission/references/csv-schema.md
  - tools/p3-mission/references/execution-protocol.md
  - tools/p3-mission/references/route-and-source-intake.md
  - tools/p3-mission/references/verification-and-evidence.md
last_verified: 2026-06-08
update_rule: 调整 P3 长任务拆分、执行、状态回写或恢复规则时同步本文件。
---

# P3 Mission 长任务协议

`tools/p3-mission` 是 Project P3 的本地长任务协议工具。它已经吸收 `misc/Missions/` 中适合 P3 的 CSV 闭环、恢复、REVIEW、声明证据一致和反暂停经验；后续只维护 `p3-mission`，不再维护两套 mission skill。

新建 mission 必须基于已有详细来源材料；来源可以是 Markdown、目录、非 Markdown 文档、事实来源或已批准计划。`p3-mission` 不允许从一句话目标直接生成；它只负责阶段进度规划、执行、证据回写和恢复，不负责补写需求文档。

核心目标：

```text
提出目标 -> 规划任务 -> 执行任务 -> 更新状态 -> 恢复并继续执行
```

## 适用场景

- 已有详细来源材料，例如 spec、事实来源、目录包或已批准计划。
- 一次工作预计超过 1 小时，且涉及 3 个以上可验证步骤。
- 需要跨会话恢复，例如配置批次、文档治理、Unity 验证补强、美术接入、UI 绑定或多职能交接。
- 用户明确要求“长期任务”“拆任务跑”“mission”“继续 mission”。

短小修复、单文件说明、一次性问答不需要 mission。

## 文件形态

```text
tools/p3-mission/
  SKILL.md                         # agent 执行协议入口
  README.md                        # 人类和知识库入口
  references/
    csv-schema.md                  # CSV 字段和状态规范
    execution-protocol.md          # 执行 / 回写 / 恢复规则
    route-and-source-intake.md      # CSV / Markdown / 自然语言 / resume 路由
    verification-and-evidence.md    # required_tools / 受限验收 / 证据等级
  scripts/p3_mission.py            # CSV 读写和校验内核
  New-P3Mission.ps1                # 从详细来源创建 mission CSV
  Test-P3Mission.ps1               # 校验 mission CSV
  Get-P3NextIssue.ps1              # 找到下一条待执行行
  Update-P3MissionState.ps1        # 安全更新某一行状态
```

任务工件位置：

- `.mission/*.csv`：默认本地恢复工件，不提交。
- `missions/*.csv`：用户批准后可作为正式执行队列提交。

## 常用命令

从详细来源创建一个本地 mission：

```powershell
.\tools\p3-mission\New-P3Mission.ps1 -Goal "补齐 P1 战斗 UI 验收闭环" -Source "开发文档/xx_详细规格.md" -Role "程序"
```

`-Source` / `-SourceSpec` 只记录来源引用；是否足够详细由执行 agent 按 skill 规则判断。未提供来源时脚本会拒绝创建并提示先补来源。

校验 CSV：

```powershell
.\tools\p3-mission\Test-P3Mission.ps1 -Path ".mission\20260607_120000-task.csv"
```

严格校验会要求 `DONE` 行的 evidence 覆盖 `required_tools`：

```powershell
.\tools\p3-mission\Test-P3Mission.ps1 -Path ".mission\20260607_120000-task.csv" -Strict
```

恢复时寻找下一条未完成任务：

```powershell
.\tools\p3-mission\Get-P3NextIssue.ps1 -Latest
```

更新行状态：

```powershell
.\tools\p3-mission\Update-P3MissionState.ps1 `
  -Path ".mission\20260607_120000-task.csv" `
  -Id "P1-01" `
  -Status "DONE" `
  -Evidence "shell: Test-P3Mission passed; Unity smoke: CombatHUDIntentBindingSmokeTest.Run passed" `
  -Notes "status_writeback:agent_status/program.md; done_at:2026-06-07"
```

## Agent 使用入口

Agent 执行长期任务时先读 `tools/p3-mission/SKILL.md`，再按 CSV 行的 `read_before` 读取 P3 事实来源。`SKILL.md` 是流程入口，不替代 `AGENTS.md`、`PROJECT_STATUS.md`、`agent_status/*`、GDD、开发文档、美术文档或配置源。

`misc/Missions` 已归档为上游参考，不再作为 active skill 或路由入口。遇到旧文档、旧 CSV 或旧说明时，按 `references/route-and-source-intake.md` 迁移到 `.mission/*.csv` 或 `missions/*.csv`。

## `/goal` 建议

如果使用 Codex CLI 的 `/goal` 跑长期任务，先发送一句普通消息并等待回复，再执行：

```text
/goal @.mission/<task>.csv
```

恢复时优先：

```powershell
.\tools\p3-mission\Get-P3NextIssue.ps1 -Latest
```

然后继续第一条未完成行。
