---
id: p3_mission_csv_schema
title: P3 Mission CSV 字段规范
type: tool
role: 全局
domain: agent_workflow
status: active
source_of_truth: true
related:
  - tools/p3-mission/README.md
  - tools/p3-mission/SKILL.md
  - tools/p3-mission/references/execution-protocol.md
last_verified: 2026-06-07
update_rule: 修改 mission CSV 字段、状态枚举或校验规则时同步本文件。
---

# P3 Mission CSV 字段规范

固定表头：

```csv
id,kind,priority,role,phase,title,goal,scope,out_of_scope,read_before,files,commands,verify,required_tools,status,status_writeback,evidence,notes
```

## 字段说明

| 字段 | 规则 |
|---|---|
| `id` | 稳定 ID，如 `P1-01`、`CFG-02`、`REVIEW-01`。 |
| `kind` | `TASK` 或 `REVIEW`。 |
| `priority` | `P0`、`P1`、`P2`、`P3`。 |
| `role` | `PM`、`策划`、`程序`、`UI程序`、`美术`、`知识库`、`全局`。 |
| `phase` | 阶段序号，字符串即可。 |
| `title` | 动词开头的一句话标题。 |
| `goal` | 该行服务的目标。 |
| `scope` | 本行要做什么，必须可执行。 |
| `out_of_scope` | 本行明确不做什么，防止扩张。 |
| `read_before` | 执行前必须读的文档，分号分隔。 |
| `files` | 预计涉及文件或目录，分号分隔。 |
| `commands` | 可能使用的命令，分号分隔。 |
| `verify` | 验收方式，必须能产生证据。 |
| `required_tools` | 必须实际调用或记录受限原因的工具，如 `shell;unity;browser`。 |
| `status` | `TODO`、`DOING`、`REVIEW`、`FIX`、`DONE`、`BLOCKED`。 |
| `status_writeback` | 完成后要回写的状态页或事实文档。 |
| `evidence` | 验证证据摘要；`DONE` 必填。 |
| `notes` | 阻塞、风险、假设、状态回写和完成时间。 |

## 状态规则

| 状态 | 含义 |
|---|---|
| `TODO` | 尚未开始。 |
| `DOING` | 正在实现或整理。 |
| `REVIEW` | 已实现，正在审查和补证据。 |
| `FIX` | Review 发现问题，正在修复。 |
| `DONE` | 验证证据和状态回写都已完成。 |
| `BLOCKED` | 无法继续，`notes` 必须包含 `blocked:<reason>`。 |

`DONE` 行必须有 `evidence`。建议 `notes` 包含 `done_at:<YYYY-MM-DD>`。

## REVIEW 行

每个 mission 最后一行必须是 `kind=REVIEW`。默认 ID 为 `REVIEW-01`。

`REVIEW` 行用于整体验收，不直接实现功能。发现缺口时追加新的 `TASK` 行和 `REVIEW-02`。

## 路径约定

- `.mission/*.csv`：本地恢复工件，默认不提交。
- `missions/*.csv`：用户批准的正式执行队列，可随任务提交。

## Notes 标签

| 标签 | 含义 |
|---|---|
| `done_at:<date>` | 完成日期。 |
| `blocked:<reason>` | 阻塞原因。 |
| `validation_limited:<reason>` | 验证受限原因。 |
| `status_writeback:<path>` | 已回写的状态页或事实文档。 |
| `risk:<level> <note>` | 风险。 |
| `assumption:<note>` | 为继续执行采用的假设。 |
| `followup:<id>` | Review 追加的后续任务。 |
