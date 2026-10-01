# Workspace profiles and character portrait sets

## Readiness gate

The active workspace contract is:

```text
UnityClient/Assets/Art/_IncomingAI/
  standard_assets/<VisualID>/
  character_portraits/<VisualID>/
  _legacy_runs/
```

Before a production run, verify that project docs and tools recognize this layout, all old Manifest VisualID workspaces have been migrated into `standard_assets/`, non-VisualID historical directories are under `_legacy_runs/`, and no legacy flat `_IncomingAI/<VisualID>/` workspace remains. If migration or resolver support is incomplete, stop with `blocked:art_workspace_profile_migration_required`. Do not silently fall back to the old layout.

## Profile semantics

`standard_asset` selects the standard independent/Manifest-batch-oriented workspace and ordinary per-asset review behavior.

`character_portrait_set` selects the character portrait workspace plus character-fact reading, optional set relationships, identity continuity checks, richer interaction, and set-level acceptance. It does not select a generation method or provider.

Always derive workspace paths through `tools/美术工具/art_workspace.py`. Do not duplicate path logic in each script or infer the profile from a VisualID prefix.

Inside either profile, production state is `raw -> processed/<positive integer> -> selected -> Approved`. Numeric rounds are shared infrastructure, not a generation-method taxonomy. Standard assets may publish a deterministic round in one command; character portraits may use any suitable text/image/editing capability, but the Agent must stage and register the result before selection.

## Three-layer boundary

Use these layers:

1. Design facts: character design, approved non-runtime masters, turnarounds, style/identity facts, and other source `AssetID` anchors.
2. Production work: runtime-consumed members under `character_portraits/<VisualID>/`.
3. Runtime assets: Manifest `OutputPath`, Approved, Registry/dedicated package, and runtime evidence.

Do not assign a VisualID to a non-runtime design master solely to make it fit the production pipeline. Use the existing production-side `AssetID` and an anchor role. Assign a VisualID only when Unity or a runtime package consumes the asset as an independent member.

IDs: stable `AssetID` for production and design members, `VisualID` only for runtime-consumed assets, `AssetSetID` for related members, and `ProductionRunID` for one execution. Do not create a separate `AnchorID`.

## Character portrait requirement contract

Require the design source to state the desired runtime result, member role, use context, output/display specifications, identity/style facts, must-preserve rules, allowed changes, forbidden content, quality target, interaction policy, and acceptance target.

Allow optional relationships such as:

```yaml
asset_set_id: zero_dialogue_portrait_v1
asset_id: zero_dialogue_confused
visual_id: doll_zero_dialogue_confused
set_role: expression_difference
source_assets:
  - asset_id: zero_identity_master_v1
    role: identity_anchor
  - asset_id: zero_dialogue_neutral
    role: optional_source
```

Relationships describe facts and provenance, not required operations. `source_assets` may be empty. Do not require every portrait to derive from another image.

## Agent-owned production decisions

After locking the requirement:

1. Inspect current tools and capabilities.
2. Choose an appropriate approach for the current member and evidence state.
3. Combine or change tools when useful and authorized.
4. Record actual decisions per run and round.
5. Preserve requirement, identity, engineering, permission, and Approved safety gates regardless of tool choice.

For an existing failed or decision-required latest round, allocate the next integer. Never overwrite the failed evidence, skip directly to `selected/`, or reuse an older passed round as an implicit fallback.

Do not maintain a closed list of supported generation methods in this skill. New tools should become usable without changing the profile contract when they can satisfy the same evidence and safety requirements.

## Set-level acceptance

Keep every runtime member in its own VisualID workspace. Keep `AssetSetID` relationships in facts and ProductionRun evidence, not in physical nesting.

Before set completion:

- confirm all required members reached their individual claim level;
- compare identity, proportions, costume, rendering, scale, baseline, and runtime crop;
- inspect contact sheets and, when relevant, rapid switching in the actual dialogue presentation;
- return only failing members to production unless the shared facts or anchor are invalid;
- do not claim the set complete while a required member is blocked, limited, stale, or awaiting a configured decision.
