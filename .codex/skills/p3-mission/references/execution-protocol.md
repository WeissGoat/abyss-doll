# P3 Mission Execution Protocol

This reference is loaded when the agent is creating, executing, reviewing, or resuming a P3 mission.

## 1. Create Or Locate A Mission

Route inputs with `references/route-and-source-intake.md`.

If the user gives a detailed source spec/fact document and asks to generate a mission, create a local mission:

```powershell
.\tools\p3-mission\New-P3Mission.ps1 -Goal "<goal>" -SourceSpec "<spec.md>" -Role "<role>"
```

If the user gives only a one-sentence goal or vague long-task request, do not run `New-P3Mission`. Ask for or create the missing detailed spec through the appropriate non-mission workflow first. P3 Mission starts after the spec exists.

If the user asks to continue or resume:

```powershell
.\tools\p3-mission\Get-P3NextIssue.ps1 -Latest
```

If multiple unfinished missions exist, choose the most recently modified one unless the user clearly names another file.

Do not scan or revive archived `misc/Missions` skills. Their content has been absorbed into `p3-mission`.

## 2. Plan Rows

Replace `PLAN-01` with 3-12 concrete `TASK` rows plus one final `REVIEW-01`.

Each `TASK` row must be:

- Independently verifiable.
- Small enough to finish without swallowing unrelated refactors.
- Explicit about `read_before`, `scope`, `out_of_scope`, `verify`, `required_tools`, and `status_writeback`.
- Assigned to the closest P3 role: `PM`, `策划`, `程序`, `UI程序`, `美术`, `知识库`, or `全局`.

When planning from a Markdown source, keep approved formal queues in `missions/*.csv` only if the source is current and user-approved; otherwise use `.mission/*.csv`. If the source is too thin to define rows without inventing requirements, stop mission creation.

Before execution:

```powershell
.\tools\p3-mission\Test-P3Mission.ps1 -Path "<mission.csv>"
```

## 3. Execute The Next Active Row

Get the next row:

```powershell
.\tools\p3-mission\Get-P3NextIssue.ps1 -Path "<mission.csv>"
```

For that row:

1. Set `status=DOING`.
2. Read `AGENTS.md`, `PROJECT_STATUS.md`, the relevant `agent_status/*`, and row `read_before`.
3. Work only inside `scope`.
4. Respect `out_of_scope`.
5. Use every `required_tools` entry or record `validation_limited:<reason>`.
6. Run the row's `verify` command or the closest available validation.
7. Update the row's `status_writeback` target when the work has real project meaning.
8. Set `status=DONE` only after evidence and status writeback exist.

Status commands:

```powershell
.\tools\p3-mission\Update-P3MissionState.ps1 -Path "<mission.csv>" -Id "<id>" -Status "DOING"
.\tools\p3-mission\Update-P3MissionState.ps1 -Path "<mission.csv>" -Id "<id>" -Status "DONE" -Evidence "<evidence>" -Notes "done_at:<YYYY-MM-DD>; status_writeback:<path>"
.\tools\p3-mission\Update-P3MissionState.ps1 -Path "<mission.csv>" -Id "<id>" -Status "BLOCKED" -Notes "blocked:<reason>"
```

## 4. P3 Status Writeback Matrix

| Work type | Writeback target |
|---|---|
| PM / version planning | `agent_status/pm.md`; update `PROJECT_STATUS.md` or `版本规划/09_正式版核心纵切开发路线.md` only when stage, priority, handoff, or blockers change. |
| Design / economy / config | `agent_status/design.md`; update GDD, rules, config docs, or JSON source when they are the fact source. |
| Program / Unity / validation | `agent_status/program.md`; update development docs when contracts or architecture change. |
| UI program | `agent_status/program.md`; update `agent_status/art.md` only for art handoff or runtime art acceptance impact. |
| Art / UI design | `agent_status/art.md`; update art docs, UI specs, manifests, or integration snapshots as required. |
| Knowledge base / tools | Relevant tool docs or status pages; update `PROJECT_STATUS.md` only if project-level workflow or blockers change. |

Never use the mission CSV as the final project fact source.

## 5. Evidence Levels

Be honest about evidence. Static inspection, README edits, dry runs, mock data, fixtures, and string checks do not prove runtime integration.

Use `references/verification-and-evidence.md` when choosing `required_tools`, writing `verify`, or judging limited validation.

Use these tags when validation is limited:

- `validation_limited:<objective reason>`
- `manual_test:<command or steps the user can run later>`
- `risk:<low|medium|high> <remaining risk>`
- `evidence:<what was actually checked>`

If Unity, browser, or external validation cannot run, record why and finish every reachable alternative check.

Do not claim "passed", "integrated", "playable", or "complete" beyond the evidence level recorded in the row.

## 6. Review Row

`REVIEW-*` rows do not implement features. They test whether the mission's claims match evidence.

Before closing review, check:

- The review compares against the original user/source goal, not only the current row titles.
- All prior `TASK` rows are `DONE` or explicitly `BLOCKED`.
- `DONE` rows have evidence and status writeback.
- Delivery claims do not overstate evidence level.
- `required_tools` were actually used or limitation notes exist.
- The original user goal is met, not merely a subset of it.
- P3 completion protocol in `AGENTS.md` was followed.

If review finds gaps:

1. Append follow-up `TASK` rows.
2. Append a new `REVIEW-(N+1)` row.
3. Mark the current review `DONE` with evidence describing the gap conversion.
4. Continue to the new follow-up rows.

If same-model sub-agent review is unavailable in the current environment, do an independent-context self-review and record `validation_limited:same-model review unavailable`; do not claim a sub-agent review happened.

## 7. Resume Rules

When resuming:

- Do not replan rows already marked `DONE`.
- Reopen a `DONE` row to `FIX` only if evidence is false, stale, or contradicted.
- Prefer the first active row returned by the tool.
- If the CSV state conflicts with the actual worktree, correct the CSV before continuing and record why in `notes`.

## 8. Anti-Pause Rules From Missions, Adapted For P3

Partial completion is not a stop condition. Continue after checkpoints, status updates, and blocker repairs.

Do not stop just because:

- A phase finished.
- A few rows are done.
- A row was blocked but later repaired.
- The next row is harder or dirtier.
- Validation is limited but alternatives remain.
- You want to summarize progress.

Stop only when:

- All rows are complete and the latest `REVIEW-*` row says the original goal is met.
- Every remaining active row is `BLOCKED` with `blocked:<reason>` and needs user or external action.
- The user explicitly asks to pause, stop, cancel, or change the task boundary.

## 9. Common Rationalizations

| Rationalization | Reality |
|---|---|
| "I created the CSV, so this turn can end." | CSV creation is setup. Validate it and start execution unless the user asked only for a plan. |
| "The user gave one sentence, so I can draft rows from inference." | No. Create or request the detailed spec first; p3-mission only handles stage progress planning/execution after source docs exist. |
| "This row has static evidence, so it is done." | Static evidence must be labeled as such; do not claim runtime or end-to-end completion. |
| "The status page is updated, so the work is complete." | Status writeback records evidence; it does not replace implementation or validation. |
| "A review found gaps, so I should ask the user." | Convert actionable gaps into rows. Ask only for human-required decisions. |
| "The worktree is dirty, so I should stop." | Scope carefully, avoid unrelated changes, and keep moving unless the dirty state makes the row impossible. |
| "Missions upstream used four CSV states, so P3 should too." | P3 uses one `status` field plus evidence/status writeback. Do not reintroduce upstream schema. |
