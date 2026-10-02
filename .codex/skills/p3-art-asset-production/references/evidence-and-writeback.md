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
  portrait-set-plan.json          # character_portrait_set only
  portrait-set-run.json           # resumable checkpoint, character_portrait_set only
  generation/<VisualID>.json      # immutable child generation snapshots
  processing/<VisualID>/          # run-scoped staging before Registrar
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

When Approved-to-Unity registration is executed as a separate resumable handoff, use an ArtImportRunID under:

```text
UnityClient/Logs/P3ArtImport/<ArtImportRunID>/
  request.json
  approved-plan.json
  approved-sync.json
  unity-import.json
  registry-result.json
  console-delta.json
  finalize-stage.json
  generated-writeback.json
  summary.json
```

The ProductionRun summary references the ArtImportRunID and final claim; it does not copy the evidence or create another progress table. `unity_imported` is evidence-only. Successful finalization keeps Manifest `Status=approved` and writes the existing `RegistryStatus=registered`.

For a portrait run, `portrait-set-run.json` stores the current stage, result, PromptRevision and reference fingerprints, generation snapshot path and output hashes, processed round evidence, selection evidence, and `PendingDecision`. It is a checkpoint, not a second Manifest. Resume must verify every referenced file before skipping a child operation. `visual-review/<VisualID>.json` is the per-member review consumed by the orchestrator; it must contain `HardGate=passed`, a numeric score, a recommendation, and a non-empty candidate SHA. The portrait Registrar requires this evidence before publishing a passed numeric round.

Per VisualID, keep working evidence in the profile workspace:

```text
UnityClient/Assets/Art/_IncomingAI/
  standard_assets/<VisualID>/
    raw/
    processed/
      1/
        decision.json
      2/
    selected/
    contact_sheet/
    generation.json
    process_report.json
    production_decision.json

  character_portraits/<VisualID>/
    raw/
    processed/
      1/
        decision.json
      2/
    selected/
    contact_sheet/
    asset_contract.json
    production_plan.json
    reference_inputs.json
    generation.json
    process_report.json
    production_decision.json

  _legacy_runs/
```

The immediate children of `standard_assets/` and `character_portraits/` are VisualIDs. `_legacy_runs/` is historical only and must never feed Manifest production, preprocessing, Approved sync, or integration queues.

Use generic candidate filenames such as `raw/r01_001.png` and `processed/2/001.png`. Each published round records the actual tool, capability, provider if applicable, inputs, reason, outputs, errors, timestamps, hashes, dimensions, formats, and decision state. Do not encode a generation-method taxonomy into stable directories, AssetIDs, VisualIDs, or required contract fields.

`Register-ArtProcessingRound.ps1` re-runs the technical review against the real staging image. The submitted `technical_review_v2` and its `ReviewFingerprint` must match; an optional `technical_override.json` is accepted only when the corresponding RuleID is explicitly authorized on the command line. The round decision preserves both `AutomaticStatus` and `AppliedOverrides`.

For character sets, store set-level request, member plan, relationship graph, consistency review, contact sheet, and runtime sequence evidence under the ProductionRunID directory. Do not add an `AssetSetID` directory level between `character_portraits/` and `<VisualID>`.

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
- latest processed round with decision evidence: ready for selection or explicit repair/decision;
- selected: candidate selection complete;
- selected replacement: candidate passed the current baseline, strict score delta, protected-dimension, and overwrite-authorization checks;
- Approved + stable `.meta`: formal asset complete;
- live AssetDatabase importer evidence: `unity_imported`;
- live Registry target evidence: `registered` and usable by VisualID;
- Prefab / UGUI consumption: part of the light check, not a separate claim;
- runtime evidence: `runtime_validated` after the `p3-art-validation` light check passes;
- normal player-path evidence: player-path integration complete.
