# Production state machine

## States

```text
CREATED
-> SOURCE_AUDIT
-> REQUIREMENT_ADMISSION
-> ASSET_CONTRACT
-> PRODUCTION_PLAN
-> BACKEND_PREFLIGHT
-> GENERATION
-> PREPROCESS
-> PROCESSING_ROUND_PUBLISH
-> TECHNICAL_REVIEW
-> VISUAL_REVIEW
-> SELECTION_DECISION
-> REPAIR_OR_REGENERATE
-> APPROVED_GATE
-> APPROVED_SYNC
-> UNITY_IMPORT
-> REGISTRY_INTEGRATION
-> RUNTIME_VALIDATION
-> WRITEBACK
-> COMPLETE
```

Side states:

```text
DECISION_REQUIRED | RETRYABLE | BLOCKED | FAILED | LIMITED
```

`PREPROCESS` and Agent-owned complex editing produce staging evidence. Publish it only as the next immutable `processed/<positive integer>/` round. Read the latest numeric round only: `failed`, `decision_required`, `legacy_unverified`, or multiple passed candidates stop progression and never fall back to an earlier round.

## Source audit

Confirm detailed sources, active UI/config state, existing Manifest/Approved/Registry/runtime state, duplicate work, and evidence freshness. A formal production run must not be based only on a one-line chat request or an unreviewed candidate scan.

## Requirement admission

Classify `source_type` as `config`, `derived`, or `preset`; classify `operation` separately, for example `new_asset`, `same_visualid_replacement`, `character_difference`, or `localized_repair`; and select one production profile. Reuse existing generic assets where appropriate. Do not invent a formal VisualID solely because a term appears in documentation.

Select `standard_asset` for ordinary independent or Manifest-batch-oriented runtime assets. Select `character_portrait_set` when a runtime portrait member depends on character facts, optional source assets, identity continuity, related portrait members, richer interaction, or set-level acceptance. Do not select the profile from the intended generation tool.

## Asset Contract

Lock:

```yaml
visual_id: ...
production_profile: standard_asset | character_portrait_set
asset_set_id: ... # optional
asset_id: ... # optional
set_role: ... # optional
source_assets: []
asset_type: ...
source_spec: ...
display_spec: ...
composition_spec: ...
style_anchors: []
identity_anchors: []
must_include: []
must_preserve: []
allowed_changes: []
forbidden: []
output_path: ...
quality_tier: ...
```

Missing or contradictory identity, style, use, output, or DisplaySpec facts block formal generation.

The Asset Contract states outcomes and constraints. Do not put a required generation method, provider, or closed capability list into it.

## Production planning

After admission:

1. Inspect the active facts, source assets, available project scripts, MCP tools, and image capabilities.
2. Choose the smallest useful current capability or combination of capabilities for this run and round.
3. Record the choice, inputs, outputs, reason, and failures in production evidence.
4. Re-plan when evidence shows another capability is safer or more effective.

Record the actual capability and inputs in round evidence, not in the method-neutral Asset Contract. Standard deterministic processing may publish a round directly; character or complex editing must stage outputs and register them through the guarded round entry before selection.

Do not infer the method from `character_difference`, `new_asset`, or the production profile. A character portrait member may be created, derived, adjusted, imported, or processed by any suitable current/future tool. Narrative page/panel work still follows `p3-narrative-cg-comic`; dynamic portrait, Live2D, and DollPuppet still use their dedicated contracts and integration paths.

## Character portrait set flow

For `character_portrait_set`:

1. Read the active character design facts and any stable design-layer `AssetID` anchors.
2. Lock `AssetSetID`, runtime member `AssetID`/`VisualID`, `SetRole`, required output, and optional source relationships.
3. Resolve the member workspace under `character_portraits/<VisualID>/`.
4. Plan and execute with current tools without assuming a particular generation method.
5. Review each member individually, then compare related members for identity, scale, baseline, costume, lighting, and presentation consistency.
6. Repair only affected members when possible; do not regenerate an entire set solely because one member fails.
7. Pass per-member Approved gates and set-level consistency gates before claiming the set complete.

## Loop policy

Default:

1. Generate 4 initial candidates.
2. If the best candidate is close but locally defective, repair it with 2 variants.
3. If all candidates share the same defect, revise the prompt/contract interpretation.
4. If the provider persistently violates identity, composition, format, or availability, switch provider.
5. Stop after 3 rounds or 2 provider switches unless the request grants different limits.

Never force a candidate past the hard gate because the loop budget is exhausted.
