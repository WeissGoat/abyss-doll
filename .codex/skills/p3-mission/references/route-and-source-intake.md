# P3 Mission Route And Source Intake

Load this reference when the user provides a document path, CSV path, natural-language long goal, or resume request.

## Spec Gate

New P3 missions require an existing detailed source document. The source can be a spec, active P3 fact document, approved plan, or detailed implementation/progress document, but it must define enough scope, non-goals, validation, and status writeback to split stage progress safely.

Do not generate a mission from a one-sentence goal, vague chat request, or TODO title. If the source is missing or too thin, stop mission creation and ask for/specify the needed spec work through the appropriate non-mission workflow.

P3 Mission only handles stage progress planning, execution, evidence, status writeback, and recovery after the source document exists.

## Routing Order

Use the first matching route:

1. Existing `.csv` path: validate the CSV, then execute the first active row.
2. Existing `.md` path: classify the document using the rules below; create a new mission only if it passes the spec gate.
3. Empty input, `continue`, `resume`, `继续`, or `接着做`: recover with `Get-P3NextIssue.ps1 -Latest`.
4. Natural-language goal without a detailed source document: do not create a mission; require a spec/fact document first.
5. Short one-turn task: do not create a mission.

If a supplied path looks intentional but does not exist, ask for the minimum correction.

## Markdown Classification

Treat a Markdown document as mission-ready only when it clearly provides:

- Goal and stage boundary.
- Scope and out-of-scope.
- Relevant P3 fact sources or file areas.
- Validation/evidence expectations.
- Status writeback targets or affected roles.

If it lacks these, it is not a valid p3-mission source yet.

Use `missions/*.csv` only for user-approved formal queues. Use `.mission/*.csv` for local execution queues generated from detailed but non-formal specs. Do not use either path for one-sentence requests.

## Queue Locations

| Queue | Use | Git behavior |
|---|---|---|
| `.mission/*.csv` | Local recovery for stage-progress execution from a detailed source spec | Do not commit by default |
| `missions/*.csv` | User-approved formal queue worth reviewing with the project | Can be committed with related protocol/status changes |
| `tools/p3-mission/` | Skill and tool source | Never store project mission CSVs here |

P3 intentionally does not use upstream `issues/*.csv`; that path belongs to archived `misc/Missions`.

## Planning From Sources

When converting a source into rows:

- Extract the original goal, non-goals, stage boundary, scope, constraints, evidence needs, expected status writeback, and relevant P3 role.
- Split by independently verifiable outcomes, not by vague activity.
- Keep each row small enough to finish without dragging unrelated refactors into the same commit.
- Put required context in `read_before`; put expected files in `files`; put concrete commands in `commands` or `verify`.
- Add one final `REVIEW-*` row whose `goal` names the original user/source goal.

## Approved-Source Checklist

Before creating any new mission CSV, confirm:

- The source is current, not an archived or superseded document.
- It does not conflict with `AGENTS.md`, `PROJECT_STATUS.md`, `agent_status/*`, or active P3 fact sources.
- It is detailed enough to define stage progress rows without inventing requirements.
- Every formal row has a real `status_writeback` target.
- Verification does not rely on mock, dry-run, fixture, or static evidence to claim runtime completion.

If any item is missing, do not create a mission. First create or update the source spec through PM/design/program/art documentation work, then run p3-mission from that document.
