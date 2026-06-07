---
name: p3-mission
description: Use when a Project P3 task is long-running, multi-step, needs durable CSV planning, status writeback, and recovery/continue behavior without Trellis hooks; triggers include requests to use p3-mission, mission, long task, split into tasks, continue mission, resume mission, or any P3 task that needs cross-session recovery and evidence-backed status writeback.
---

# P3 Mission Skill

你现在使用 P3 mission 协议。它是 Project P3 的本地长任务队列，不是 Trellis，不启用 hooks，不替代项目事实来源。

## 触发场景

使用本协议处理：

- 用户要求长期任务、mission、拆任务执行、持续执行或恢复继续。
- 任务预计超过 1 小时，或包含 3 个以上可验证步骤。
- 任务需要跨职能状态回写、验证证据、可恢复 CSV。

短任务直接执行，不创建 mission。

## 用户触发方式

用户不需要手动运行脚本。用户只要用自然语言说明意图，agent 负责创建、执行、更新和恢复 mission。

典型触发说法：

- `用 p3-mission 跑这个长期任务：<目标>`
- `把这个目标拆成 mission，然后继续执行`
- `这个任务比较长，按 mission 机制跑`
- `继续上次的 mission`
- `恢复 mission，接着做`

如果用户没有明确说 mission，但任务预计超过 1 小时、包含 3 个以上可验证步骤，或需要跨会话恢复，agent 应主动使用本协议。

## 总流程

```text
目标 -> mission CSV -> 逐行执行 -> 验证证据 -> 状态回写 -> REVIEW -> 完成或追加缺口
```

## 先读规则

创建或执行 mission 前先读：

- `AGENTS.md`
- `PROJECT_STATUS.md`
- 与任务相关的 `agent_status/*`
- CSV 行里 `read_before` 指定的事实来源

不要把聊天记录当作唯一事实来源。CSV 记录执行状态，P3 文档记录项目事实。

## 目录约定

- `.mission/*.csv`：本地临时 mission / 恢复工件，默认不提交。
- `missions/*.csv`：用户明确批准后保留的正式 mission 队列，可提交。
- 不在 `tools/p3-mission/` 里存具体项目 mission；那里只放工具、协议和模板。

## 创建 mission

如果用户只给目标，先创建本地 mission：

```powershell
.\tools\p3-mission\New-P3Mission.ps1 -Goal "<用户目标>" -Role "<主职能>"
```

然后把 `PLAN-01` 拆成 3-12 条具体 `TASK` 行，保留最后 `REVIEW-01` 行。每条 `TASK` 必须填写：

- `read_before`
- `scope`
- `out_of_scope`
- `files`
- `verify`
- `status_writeback`
- `required_tools`

拆完后运行：

```powershell
.\tools\p3-mission\Test-P3Mission.ps1 -Path "<mission.csv>"
```

字段规则见 `references/csv-schema.md`。

## 执行 mission

每轮只执行第一条未完成行：

```powershell
.\tools\p3-mission\Get-P3NextIssue.ps1 -Path "<mission.csv>"
```

执行步骤：

1. 将当前行状态设为 `DOING`。
2. 按 `read_before` 读取上下文。
3. 只做 `scope` 范围内的工作，遵守 `out_of_scope`。
4. 按 `verify` 和 `required_tools` 获取实际证据。
5. 按 `status_writeback` 更新对应状态页或事实文档。
6. 证据足够后设为 `DONE`，写入 `evidence` 和 `notes`。
7. 继续下一条，直到 `REVIEW-01`。

状态更新命令：

```powershell
.\tools\p3-mission\Update-P3MissionState.ps1 -Path "<mission.csv>" -Id "<row id>" -Status "DOING"
```

完成行时必须带证据：

```powershell
.\tools\p3-mission\Update-P3MissionState.ps1 -Path "<mission.csv>" -Id "<row id>" -Status "DONE" -Evidence "<command/result/status-writeback evidence>" -Notes "done_at:<YYYY-MM-DD>"
```

## REVIEW 行

`REVIEW-01` 只在前面所有 `TASK` 行都 `DONE` 或明确 `BLOCKED` 后执行。

Review 要检查：

- 原始目标是否达成。
- CSV 行状态是否和实际证据一致。
- `required_tools` 是否实际调用或写明受限原因。
- P3 完成协议要求的状态页是否已经回写。
- 是否存在被夸大的声明，例如只做静态检查却声称 Unity 全链路通过。

发现缺口时追加新的 `TASK` 行和下一条 `REVIEW-02`，继续执行；不要只在最终回复里口头列缺口。

## 恢复继续

用户说“继续 mission”“resume”“接着做”时：

```powershell
.\tools\p3-mission\Get-P3NextIssue.ps1 -Latest
```

如果找到未完成 mission，读取返回的 `next` 行，从该行继续。不要重新规划已经 `DONE` 的行。

## 停止条件

只能在以下情况下停下：

- 所有行完成，且 REVIEW 判定目标达成。
- 剩余行全部 `BLOCKED`，并且每条都有 `blocked:<reason>`。
- 用户明确要求暂停、停止或改变任务边界。

## P3 特有约束

- 不默认强制 git commit；只有用户或 mission 行明确要求时才提交。
- 不把 `.mission/*.csv` 当作项目事实来源；它只是恢复工件。
- `.mission/*.csv` 默认不提交；`missions/*.csv` 可作为用户批准的正式队列提交。
- 做完有实际意义的开发、策划、配置、美术、UI 或验收工作后，必须按 `AGENTS.md` 完成协议回写对应状态页。
- Unity / 配置 / 美术验证不可伪造；无法运行时写 `validation_limited:<reason>` 和替代证据。
