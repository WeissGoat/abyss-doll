---
name: p3-mission
description: Use when a Project P3 request is a long task, mission request, resume request, interrupted multi-step task, or cross-session workflow that needs durable progress tracking and evidence-backed status writeback.
---

# P3 Mission

## Overview

P3 Mission is Project P3's only maintained mission workflow: a resumable CSV queue plus evidence-backed status writeback.

It is not Trellis, does not install hooks, and does not replace `AGENTS.md`, `PROJECT_STATUS.md`, `agent_status/*`, GDD, development docs, art docs, or configuration sources.

`misc/Missions` is an archived upstream reference only. Do not route work into its separate skills or maintain a second mission system.

## Core Rules

- Read `AGENTS.md` and the relevant P3 status/fact sources before changing project files.
- Use `.mission/*.csv` for local recovery artifacts and `missions/*.csv` only when the user approves a formal queue.
- Keep the CSV as execution state, not project truth. Meaningful work must write back to `agent_status/*` or the relevant fact document.
- Every mission needs concrete `TASK` rows and a final `REVIEW-*` row that checks the original goal against evidence.
- Do not stop at setup, checkpoints, partial completion, or limited validation while reachable rows remain.

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

## Input Routing

- Existing CSV: validate it, then execute the first active row.
- Markdown document: read `references/route-and-source-intake.md` to decide whether it is an approved formal queue source or long-task context.
- Natural-language long goal: create `.mission/*.csv`, replace the placeholder with 3-12 rows, validate, then execute.
- Resume request: use `Get-P3NextIssue.ps1 -Latest`.
- Short one-turn fix: skip mission and follow `AGENTS.md` directly.

## Workflow

1. Read `AGENTS.md`, `PROJECT_STATUS.md`, relevant `agent_status/*`, and row `read_before`.
2. Create or locate the mission CSV.
3. Replace `PLAN-01` with 3-12 concrete `TASK` rows and one final `REVIEW-*` row.
4. Validate before implementation.
5. Execute only the first active row returned by `Get-P3NextIssue.ps1`.
6. Mark `DOING`, work inside `scope`, avoid `out_of_scope`, verify, write evidence, update P3 status, then mark `DONE`.
7. Continue until review proves the original goal is met or every remaining row is genuinely blocked.

Read `references/route-and-source-intake.md` for routing, approved-source handling, and queue location rules.
Read `references/execution-protocol.md` for execution, recovery, review, and anti-pause rules.
Read `references/verification-and-evidence.md` for validation tool mapping, limited validation, and claim/evidence alignment.
Read `references/csv-schema.md` for fields and allowed values.

## P3 Adaptation

Absorbed from `misc/Missions`: durable CSV state, source routing, recovery, explicit review rows, claim/evidence alignment, limited validation discipline, and anti-pause rules.

Do not copy its multi-skill router, `issues/*.csv` model, four-status CSV schema, or mandatory per-row commit behavior. P3 uses one skill, one schema, P3 status writeback, and normal scoped git commits.

## Common Mistakes

| Mistake | Correction |
|---|---|
| CSV becomes project truth | CSV is execution state; P3 docs/status pages hold truth. |
| `DONE` after static inspection | Record real validation or `validation_limited:<reason>`. |
| Stop after checkpoint | Continue unless stop conditions in `execution-protocol.md` are met. |
| Replan completed rows | Resume from the first active row. |
| Commit `.mission/*.csv` | Keep local missions untracked by default. |
| Reuse `misc/Missions` skills | Use `p3-mission`; archived Missions is reference material only. |
