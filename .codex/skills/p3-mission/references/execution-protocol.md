---
id: p3_mission_execution_protocol
title: P3 Mission 执行与恢复协议
type: tool
role: 全局
domain: agent_workflow
status: active
source_of_truth: true
related:
  - tools/p3-mission/README.md
  - tools/p3-mission/SKILL.md
  - tools/p3-mission/references/csv-schema.md
last_verified: 2026-06-07
update_rule: 修改 mission 执行、验收、状态回写或恢复流程时同步本文件。
---

# P3 Mission 执行与恢复协议

## 1. 提出目标

用户给出目标后，先判断是否需要 mission。满足任一条件即可使用：

- 跨 3 个以上步骤。
- 需要多轮验证。
- 需要跨会话恢复。
- 涉及多个职能状态页或事实文档。

创建命令：

```powershell
.\tools\p3-mission\New-P3Mission.ps1 -Goal "<goal>" -Role "<role>"
```

## 2. 规划任务

把 `PLAN-01` 替换为 3-12 条 `TASK` 行。每条行必须：

- 可独立验证。
- 有明确 `read_before`。
- 有明确 `verify`。
- 有明确 `status_writeback`。
- 不跨越过大的职能边界。

规划完成后运行：

```powershell
.\tools\p3-mission\Test-P3Mission.ps1 -Path "<mission.csv>"
```

## 3. 执行任务

从下一条未完成行开始：

```powershell
.\tools\p3-mission\Get-P3NextIssue.ps1 -Path "<mission.csv>"
```

执行当前行时：

1. 状态改为 `DOING`。
2. 读取 `read_before`。
3. 执行 `scope`，不做 `out_of_scope`。
4. 运行 `commands` / `verify` 中适用的命令。
5. 把证据写入 `evidence`。
6. 按 `status_writeback` 更新 P3 状态页或事实文档。
7. 状态改为 `DONE`。

## 4. 更新状态

状态更新命令只修改 CSV，不替代实际开发和文档回写：

```powershell
.\tools\p3-mission\Update-P3MissionState.ps1 -Path "<mission.csv>" -Id "<id>" -Status "DOING"
```

完成时：

```powershell
.\tools\p3-mission\Update-P3MissionState.ps1 -Path "<mission.csv>" -Id "<id>" -Status "DONE" -Evidence "<evidence>" -Notes "done_at:<date>; status_writeback:<path>"
```

阻塞时：

```powershell
.\tools\p3-mission\Update-P3MissionState.ps1 -Path "<mission.csv>" -Id "<id>" -Status "BLOCKED" -Notes "blocked:<reason>"
```

## 5. 恢复并继续

恢复命令：

```powershell
.\tools\p3-mission\Get-P3NextIssue.ps1 -Latest
```

如果返回 `next`，继续该行。不要重做 `DONE` 行；如果 `DONE` 行证据被发现不成立，把该行改为 `FIX` 并记录原因。

## 6. 状态回写矩阵

| 任务类型 | 回写位置 |
|---|---|
| PM / 版本规划 | `agent_status/pm.md`，必要时 `PROJECT_STATUS.md` 和 `版本规划/09_正式版核心纵切开发路线.md`。 |
| 策划 / 数值 / 配置 | `agent_status/design.md`，必要时配置事实文档和 GDD。 |
| 程序 / Unity / 验证 | `agent_status/program.md`，必要时开发文档。 |
| 美术 / UI | `agent_status/art.md`，必要时美术文档。 |
| 知识库 / 工具 / 文档索引 | 对应工具文档，必要时 `PROJECT_STATUS.md` 或状态页。 |

状态页只写事实、证据入口、下一步和阻塞；不要粘贴聊天记录。

## 7. 证据等级

- 命令通过：记录命令和关键结果。
- Unity 自动化通过：记录测试名、RunID 或日志路径。
- 文档校验通过：记录校验命令。
- 无法运行：记录 `validation_limited:<reason>`，并说明替代证据和风险。

不要用静态检查冒充 Unity 运行时验证，不要用 README 或审计结论冒充配置源完成。
