# P3 Mission Execution Protocol

This reference is loaded when the agent is creating, executing, reviewing, or resuming a P3 mission.

## 1. Create Or Locate A Mission

Route inputs with `references/route-and-source-intake.md`.

If the user gives detailed source material and asks to generate a mission, create a local mission:

```powershell
.\.codex\skills\p3-mission\New-P3Mission.ps1 -Goal "<goal>" -Source "<source path or reference>" -Role "<role>"
```

If the user gives only a one-sentence goal or vague long-task request, do not run `New-P3Mission`. Ask for or create the missing detailed source material through the appropriate non-mission workflow first. P3 Mission starts after mission-ready source exists.

If the user asks to continue or resume:

```powershell
.\.codex\skills\p3-mission\Get-P3NextIssue.ps1 -Latest
```

If multiple unfinished missions exist, choose the most recently modified one unless the user clearly names another file.

## 2. Plan Rows

Replace `PLAN-01` with 3-12 concrete `TASK` rows plus one final `REVIEW-01`.

Each `TASK` row must be:

- Independently verifiable.
- Small enough to finish without swallowing unrelated refactors.
- Explicit about `read_before`, `scope`, `out_of_scope`, `verify`, `required_tools`, and `status_writeback`.
- Labelled with the closest domain: `全局`, `游戏导演`, `剧情`, `策划`, `程序`, `美术`, or `知识库` (labels only; there are no per-domain status pages). Legacy `PM` / `UI程序` / `Owner` values in old CSVs still validate with a warning; new rows use `游戏导演` / `程序`.

When planning from a source, keep approved formal queues in `missions/*.csv` only if the source is current and user-approved; otherwise use `.mission/*.csv`. If the source is too thin to define rows without inventing requirements, stop mission creation.

Before execution:

```powershell
.\.codex\skills\p3-mission\Test-P3Mission.ps1 -Path "<mission.csv>"
```

## 3. Execute The Next Active Row

Get the next row:

```powershell
.\.codex\skills\p3-mission\Get-P3NextIssue.ps1 -Path "<mission.csv>"
```

For that row:

1. Set `status=DOING`.
2. Read `AGENTS.md`, `PROJECT_STATUS.md`, and row `read_before`.
3. Work only inside `scope`.
4. Respect `out_of_scope`.
5. Use every `required_tools` entry or record `validation_limited:<reason>`.
6. Run the row's `verify` command or the closest available validation.
7. Update the row's `status_writeback` target when the work has real project meaning.
8. Set `status=DONE` only after evidence and status writeback exist.

Status commands:

```powershell
.\.codex\skills\p3-mission\Update-P3MissionState.ps1 -Path "<mission.csv>" -Id "<id>" -Status "DOING"
.\.codex\skills\p3-mission\Update-P3MissionState.ps1 -Path "<mission.csv>" -Id "<id>" -Status "DONE" -Evidence "<evidence>" -Notes "done_at:<YYYY-MM-DD>; status_writeback:<path>"
.\.codex\skills\p3-mission\Update-P3MissionState.ps1 -Path "<mission.csv>" -Id "<id>" -Status "BLOCKED" -Notes "blocked:<reason>"
```

## 4. Status Writeback

Status lives on one page: `PROJECT_STATUS.md`. When a row changes a milestone, the next step or a known issue, update the matching line there. Facts go into the docs, config or code the row changed. Never use the mission CSV as the final project fact source.

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

## 8. When To Stop

Keep going through reachable rows; finishing a phase or wanting to summarize is not a reason to stop. Stop when the latest `REVIEW-*` row confirms the original goal, when every remaining row is `BLOCKED` with `blocked:<reason>` and needs the user or something external, or when the user asks to pause, stop, cancel or change the boundary.
