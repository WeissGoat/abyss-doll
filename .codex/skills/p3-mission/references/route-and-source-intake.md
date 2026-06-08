# P3 Mission Route And Source Intake

Load this reference when the user provides a document path, CSV path, natural-language long goal, or resume request.

## Routing Order

Use the first matching route:

1. Existing `.csv` path: validate the CSV, then execute the first active row.
2. Existing `.md` path: classify the document using the rules below.
3. Empty input, `continue`, `resume`, `继续`, or `接着做`: recover with `Get-P3NextIssue.ps1 -Latest`.
4. Natural-language goal that is long, cross-session, or 3+ verifiable steps: create a local `.mission/*.csv`.
5. Short one-turn task: do not create a mission.

If a supplied path looks intentional but does not exist, ask for the minimum correction.

## Markdown Classification

Treat a Markdown document as an approved formal queue source only when one of these is true:

- The user explicitly says it is approved for execution.
- It is a current P3 fact source that already defines scope, tasks, validation, and status writeback.
- It is a plan/spec document the user asks to turn into a mission queue.

Otherwise treat it as long-task context and create `.mission/*.csv` rather than `missions/*.csv`.

## Queue Locations

| Queue | Use | Git behavior |
|---|---|---|
| `.mission/*.csv` | Local recovery for long tasks and exploratory execution | Do not commit by default |
| `missions/*.csv` | User-approved formal queue worth reviewing with the project | Can be committed with related protocol/status changes |
| `tools/p3-mission/` | Skill and tool source | Never store project mission CSVs here |

P3 intentionally does not use upstream `issues/*.csv`; that path belongs to archived `misc/Missions`.

## Planning From Sources

When converting a source into rows:

- Extract the original goal, non-goals, scope, constraints, evidence needs, expected status writeback, and relevant P3 role.
- Split by independently verifiable outcomes, not by vague activity.
- Keep each row small enough to finish without dragging unrelated refactors into the same commit.
- Put required context in `read_before`; put expected files in `files`; put concrete commands in `commands` or `verify`.
- Add one final `REVIEW-*` row whose `goal` names the original user/source goal.

## Approved-Source Checklist

Before creating `missions/*.csv` from a formal source, confirm:

- The source is current, not an archived or superseded document.
- It does not conflict with `AGENTS.md`, `PROJECT_STATUS.md`, `agent_status/*`, or active P3 fact sources.
- Every formal row has a real `status_writeback` target.
- Verification does not rely on mock, dry-run, fixture, or static evidence to claim runtime completion.

If any item is missing, create `.mission/*.csv` for discovery/planning or add a preparatory `TASK` row to close the gap.
