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
```

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
- integration scope: `runtime_bound`
- runtime validation scope: `runtime_validated`
- player-path scope with evidence: `player_path_verified`

