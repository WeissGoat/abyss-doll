# P3 Mission Verification And Evidence

Load this reference when writing `verify`, `required_tools`, `evidence`, or review rows.

## Tool Mapping

`required_tools` is an execution contract. Use every listed tool or record a limitation.

| Work | Typical `required_tools` | Evidence minimum |
|---|---|---|
| Docs / planning | `shell` | File diff, targeted validation, and status writeback |
| Config / JSON | `shell` | Sync, schema/static validation, reference check, fixed sample when available |
| Unity code | `shell;unity` | Relevant tests or trigger result; if Unity cannot run, record why |
| Runtime UI | `shell;unity` | Smoke/runtime evidence and affected status pages |
| Browser app | `shell;browser` or `shell;browser;playwright` | Screenshot/snapshot and scenario result |
| Art pipeline | `shell` plus named art tool | Manifest/update/validation output and generated snapshot |

Do not infer completion from a tool name alone. Evidence must say what actually ran or what was inspected.

## Evidence Levels

Match claims to evidence:

| Evidence | Can support | Cannot support |
|---|---|---|
| Static read/diff | Documentation or code-shape claim | Runtime behavior |
| Unit/smoke test | Tested function/path | End-to-end player flow unless it covers that flow |
| Dry run/mock/fixture | Alternative or isolated behavior | Real side effect, real integration, production readiness |
| Unity/browser/manual run | The observed scenario | Untested scenarios outside the run |

Use precise language: `static_check`, `unit_test`, `unity_smoke`, `manual_review`, `validation_limited`, or `manual_test`.

## Limited Validation

Limited validation is allowed only after reachable checks are exhausted. It is not a shortcut for slow or inconvenient tests.

Valid reasons include:

- External credential, paid service, human approval, or third-party action is required.
- Unity Editor, browser target, or external service is unavailable in the current environment after reasonable setup attempts.
- Continuing would require destructive action or fabricated evidence.

Invalid reasons include:

- Dependency missing but installable.
- Local service not started but startable.
- Test setup is mildly inconvenient.
- E2E path is complex but locally reachable.

Record limited validation with:

- `validation_limited:<objective reason>`
- `manual_test:<command or steps>`
- `evidence:<reachable checks actually completed>`
- `risk:<low|medium|high> <remaining risk>`

## Review Evidence

`REVIEW-*` rows must compare the original goal with delivered evidence. They should check:

- Every prior `TASK` row is `DONE` or explicitly `BLOCKED`.
- `DONE` rows have evidence and status writeback.
- `required_tools` were used or limitation notes exist.
- The final answer does not overclaim beyond evidence.
- P3 status pages or fact documents contain the durable project truth.

If review finds actionable gaps, append follow-up `TASK` rows and a new `REVIEW-*` row instead of closing with a gap list only.
