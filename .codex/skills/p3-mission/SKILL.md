---
name: p3-mission
description: Use when a Project P3 request is a long task, mission request, resume request, interrupted multi-step task, or cross-session workflow that needs durable progress tracking and evidence-backed status writeback.
---

# P3 Mission

## Overview

P3 Mission is Project P3's local long-task protocol: a resumable CSV queue plus evidence-backed status writeback.

It is not Trellis, does not install hooks, and does not replace `AGENTS.md`, `PROJECT_STATUS.md`, `agent_status/*`, GDD, development docs, art docs, or configuration sources.

## When To Use

Use this skill when the user says `p3-mission`, `mission`, `long task`, `continue mission`, `resume mission`, or `继续上次的 mission`; or when the task likely takes more than one hour, has three or more independently verifiable steps, may be interrupted, or crosses multiple P3 status/fact sources.

Do not create a mission for a short one-turn fix. Execute directly and follow `AGENTS.md`.

## Quick Reference

| Need | Command |
|---|---|
| Create | `.\tools\p3-mission\New-P3Mission.ps1 -Goal "<goal>" -Role "<role>"` |
| Validate | `.\tools\p3-mission\Test-P3Mission.ps1 -Path "<mission.csv>"` |
| Next row | `.\tools\p3-mission\Get-P3NextIssue.ps1 -Path "<mission.csv>"` |
| Resume | `.\tools\p3-mission\Get-P3NextIssue.ps1 -Latest` |
| Update | `.\tools\p3-mission\Update-P3MissionState.ps1 -Path "<mission.csv>" -Id "<id>" -Status "<status>"` |

Codex discovery copy: `.codex/skills/p3-mission/`.
Source/tool repository: `tools/p3-mission/`.

## Workflow

1. Read `AGENTS.md`, `PROJECT_STATUS.md`, relevant `agent_status/*`, and row `read_before`.
2. Create or locate the mission CSV.
3. Replace `PLAN-01` with 3-12 concrete `TASK` rows and one final `REVIEW-*` row.
4. Validate before implementation.
5. Execute only the first active row returned by `Get-P3NextIssue.ps1`.
6. Mark `DOING`, work inside `scope`, avoid `out_of_scope`, verify, write evidence, update P3 status, then mark `DONE`.
7. Continue until review proves the original goal is met or every remaining row is genuinely blocked.

Read `references/execution-protocol.md` for execution, recovery, review, and anti-pause rules.
Read `references/csv-schema.md` for fields and allowed values.

## P3 Adaptation

Borrow from `misc/Missions`: durable CSV state, recovery, explicit review rows, claim/evidence alignment, and anti-pause discipline.

Do not copy its multi-skill router, `issues/*.csv` model, or mandatory per-row commit behavior. P3 uses `.mission/*.csv` for local recovery and `missions/*.csv` only when the user approves a formal queue.

## Common Mistakes

| Mistake | Correction |
|---|---|
| CSV becomes project truth | CSV is execution state; P3 docs/status pages hold truth. |
| `DONE` after static inspection | Record real validation or `validation_limited:<reason>`. |
| Stop after checkpoint | Continue unless stop conditions in `execution-protocol.md` are met. |
| Replan completed rows | Resume from the first active row. |
| Commit `.mission/*.csv` | Keep local missions untracked by default. |
