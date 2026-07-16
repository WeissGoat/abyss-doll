# Evidence and writeback

## Run evidence

Use a production RunID and store orchestration evidence under:

```text
UnityClient/Logs/P3ArtProduction/<ProductionRunID>/
  request.json
  source-audit.json
  asset-contract.json
  production-plan.json
  generation-summary.json
  technical-review.json
  visual-review.json
  selection-decision.json
  approved-sync.json
  unity-import.json
  registry-result.json
  runtime-validation.json
  summary.json
```

This is evidence, not a second project progress table.

Per VisualID, keep working evidence in the existing `_IncomingAI/<VisualID>/` structure, including `generation.json`, `process_report.json`, contact sheets, and `production_decision.json`.

## Freshness gate

Before completion, compare the timestamps/fingerprints of:

- source configuration/active UI/seed;
- Manifest;
- Approved asset and `.meta`;
- integration candidates;
- quality backlog;
- program handoff;
- Registry snapshot;
- runtime validation RunID.

Mark stale outputs and refresh them through their owning scripts. Do not hand-edit generated reports.

## Writeback

After meaningful work:

1. Refresh Manifest/integration/quality/program-handoff generated outputs as required.
2. Update `agent_status/art.md` with completion, current concern, next action, blockers, and evidence.
3. Update `agent_status/program.md` only when Unity import, Registry, binding, or program handoff state materially changes.
4. Update asset-specific facts when the Asset Contract, identity/style anchor, approved output, or acceptance rule changes.
5. Update `PROJECT_STATUS.md` only for project-stage, cross-functional handoff, or blocking changes.

## Claim limits

- raw exists: candidate generation complete;
- processed/contact sheet: ready for selection;
- selected: candidate selection complete;
- Approved + stable `.meta`: asset complete;
- live Registry/UI binding: integration complete;
- runtime evidence: runtime art validation complete;
- normal player-path evidence: player-path integration complete.

