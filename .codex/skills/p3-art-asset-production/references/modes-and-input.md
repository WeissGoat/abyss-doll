# Modes and input contract

## Default behavior

Use `interactive` with `auto_until_decision`: proceed without asking while facts, deterministic checks, and evaluation thresholds support a clear action. Ask only when a decision gate is reached.

Use `auto` only after explicit user authorization such as “全自动执行”, “不用中途问我”, or an equivalent scoped instruction.

## Normalized request

Create an internal request with at least:

```yaml
mode: interactive
source_ref: []
visual_ids: []
asset_type: unknown
operation: new_asset
production_profile: standard_asset
asset_set_id: null
asset_id: null
set_role: null
source_assets: []
target_tier: formal_ai_v2
scope:
  include: [generation, preprocessing, selection, approved_sync, unity_import, registry, runtime_validation]
  exclude: [art_direction_change, gameplay_change]
permissions:
  allow_provider_fallback: true
  allow_automatic_selection: true
  allow_automatic_approved_sync: false
  allow_same_visualid_overwrite: false
  allow_new_approved_path: false
limits:
  max_rounds: 3
  initial_variants: 4
  repair_variants: 2
  max_provider_switches: 2
  max_runtime_iterations: 2
interaction:
  pause_on_direction_change: true
  pause_before_core_asset_approved: true
  pause_on_low_confidence: true
execution:
  request_catalog_path: 美术文档/_generated/art_generation_requests.json
  request_id: null
  prompt_format: auto
```

`production_profile` controls workspace and orchestration only:

- `standard_asset`: independent or Manifest-batch-oriented runtime assets;
- `character_portrait_set`: runtime portrait members that require character facts, optional set relationships, identity consistency, richer interaction, or set-level review.

Do not add a required `generation_mode`, provider, or closed `allowed_methods` field to the normalized request. State the desired result, inputs, constraints, permissions, limits, and claim ceiling. Choose current tools later in the run-scoped production plan.

`execution.request_catalog_path`, `request_id`, and optional `prompt_revision_id` point to persisted Requirement and PromptRevision work. They do not change the method-neutral requirement. `prompt_format=auto` selects a ready active Revision Variant at execution time; `natural_language_v2` and `danbooru_tags_v2` are explicit format requests and must fail if unavailable, stale, invalid, or missing hard-constraint mappings.

For character portrait members, use `AssetSetID`, `AssetID`, `SetRole`, `SourceAssets`, and optional derivation relationships only when facts support them. A non-runtime identity master or turnaround belongs to the design layer as an `AssetID` with an anchor role and no `VisualID`. A difference that Unity consumes belongs to the production layer and requires a `VisualID` before formal production.

## Full-auto defaults

When full automation is explicitly authorized, set automatic selection and Approved synchronization true only inside the user's stated scope. Do not infer authorization for new VisualIDs, new active UI direction, gameplay/config changes, or unrelated Approved replacements.

Recommended explicit auto policy:

```yaml
mode: auto
allow_approved_sync: true
allow_same_visualid_replace: true
allow_new_visualid: false
allow_provider_fallback: true
allow_runtime_integration: true
stop_on_fact_conflict: true
stop_on_hard_gate_failure: true
```

## Claim ceiling

Lock the maximum claim before work starts:

- exploration request: `candidate_generated`
- production without Unity scope: `asset_approved`
- integration scope: `runtime_validated` only when the request includes formal runtime art validation evidence;
- asset-only integration scope: `registered` when the request stops after Unity / Registry integration;
- player-path scope with evidence: `player_path_verified`
