# P3 Mission CSV Schema

Use this schema for `.mission/*.csv` and `missions/*.csv`.

## Header

```csv
id,kind,priority,role,phase,title,goal,scope,out_of_scope,read_before,files,commands,verify,required_tools,status,status_writeback,evidence,notes
```

The header order is fixed. All fields are written as quoted CSV values by the tool.

## Fields

| Field | Rule |
|---|---|
| `id` | Stable row id, for example `PLAN-01`, `P1-01`, `CFG-02`, `REVIEW-01`. |
| `kind` | `TASK` or `REVIEW`. |
| `priority` | `P0`, `P1`, `P2`, or `P3`. |
| `role` | `PM`, `策划`, `程序`, `UI程序`, `美术`, `知识库`, or `全局`. |
| `phase` | String phase/order value, commonly `1`, `2`, `99`. |
| `title` | Short action title. Prefer verb-led wording. |
| `goal` | The row's contribution to the user's original goal. |
| `scope` | What this row may change or decide. Must be executable. |
| `out_of_scope` | What this row must not do. Use it to prevent drift. |
| `read_before` | Required context files, separated with semicolons. |
| `files` | Expected files or directories, separated with semicolons. |
| `commands` | Useful commands, separated with semicolons. |
| `verify` | Evidence required before the row can become `DONE`. |
| `required_tools` | Tools that must be used or explicitly marked limited, for example `shell;unity;browser`. |
| `status` | `TODO`, `DOING`, `REVIEW`, `FIX`, `DONE`, or `BLOCKED`. |
| `status_writeback` | P3 status page or fact document to update after meaningful work. |
| `evidence` | Verification summary. Required for `DONE`. |
| `notes` | Completion date, blockers, risks, assumptions, and recovery notes. |

For source intake and evidence mapping, see `route-and-source-intake.md` and `verification-and-evidence.md`.

## Status Values

| Status | Meaning |
|---|---|
| `TODO` | Not started. |
| `DOING` | Actively being implemented or researched. |
| `REVIEW` | Implementation is done and evidence/review is being checked. |
| `FIX` | Review found a gap and the row is being repaired. |
| `DONE` | Evidence and required P3 status writeback are complete. |
| `BLOCKED` | Cannot proceed without user or external action; `notes` must include `blocked:<reason>`. |

`DONE` requires non-empty `evidence`. Prefer `notes` to include `done_at:<YYYY-MM-DD>`.

## Review Rows

Every mission must end with a `kind=REVIEW` row. Default id: `REVIEW-01`.

The review row checks whether the original user goal was actually met. It must compare:

- User goal and non-goals.
- All prior row statuses.
- Evidence level vs delivery claims.
- Required status writeback.
- Any `validation_limited`, risk, assumption, or blocker notes.

When review finds a gap, append concrete follow-up `TASK` rows and a new `REVIEW-02`. Do not close the mission by only listing gaps in the final answer.

## Paths

- `.mission/*.csv`: local recovery artifact, not committed by default.
- `missions/*.csv`: user-approved formal mission queue, can be committed.
- `tools/p3-mission/`: tool and protocol source only; never store project mission CSVs here.
- `misc/_archive/Missions_*/`: archived upstream reference only; never use as an active queue or skill source.

## Notes Tags

| Tag | Meaning |
|---|---|
| `done_at:<date>` | Completion date. |
| `blocked:<reason>` | Blocker reason. |
| `validation_limited:<reason>` | Why requested validation could not be fully run. |
| `manual_test:<steps>` | Steps a user can run when external validation becomes available. |
| `evidence:<check>` | Extra evidence note when the `evidence` field needs a tagged summary. |
| `status_writeback:<path>` | Status page or fact doc that was updated. |
| `risk:<low|medium|high> <note>` | Residual risk. |
| `assumption:<note>` | Assumption used to keep work moving. |
| `followup:<id>` | Follow-up row added by review. |
| `picked_reason:<why>` | Why this row was selected next. |
