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

## Source audit

Confirm detailed sources, active UI/config state, existing Manifest/Approved/Registry/runtime state, duplicate work, and evidence freshness. A formal production run must not be based only on a one-line chat request or an unreviewed candidate scan.

## Requirement admission

Classify `source_type` as `config`, `derived`, or `preset`, and classify `operation` separately, for example `new_asset`, `same_visualid_replacement`, `character_difference`, or `localized_repair`. Reuse existing generic assets where appropriate. Do not invent a formal VisualID solely because a term appears in documentation.

## Asset Contract

Lock:

```yaml
visual_id: ...
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

## Production routing

- New concept or new formal image: delegate text-to-image.
- Existing image with local defects or state difference: delegate image-to-image.
- Explicit mask with acceptable drift: delegate inpaint.
- Narrative page/panel: additionally follow `p3-narrative-cg-comic`.
- Dynamic portrait/Live2D/DollPuppet: use its dedicated asset contract and integration path.

## Loop policy

Default:

1. Generate 4 initial candidates.
2. If the best candidate is close but locally defective, repair it with 2 variants.
3. If all candidates share the same defect, revise the prompt/contract interpretation.
4. If the provider persistently violates identity, composition, format, or availability, switch provider.
5. Stop after 3 rounds or 2 provider switches unless the request grants different limits.

Never force a candidate past the hard gate because the loop budget is exhausted.
