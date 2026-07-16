# Approved, Unity, Registry, and runtime validation

## Approved gate

Require all:

- selected candidate exists;
- technical gate passed;
- visual threshold/policy passed;
- Manifest VisualID and OutputPath match;
- quality tier is explicit;
- no fact conflict;
- current mode/permissions allow synchronization;
- same-VisualID replacement passes strict `.meta` and GUID preservation.

Use existing `Sync-ApprovedArt.ps1`. Do not manually copy around its guards.

## Unity import

After Approved changes:

1. Refresh Unity and wait for AssetDatabase/compilation readiness.
2. Collect a Console baseline/delta.
3. Verify asset type, import settings, path, `.meta`, and GUID.
4. Run the appropriate display/import validator.

Approved on disk is not `unity_imported` until the live Editor recognizes the expected asset.

## Registry

- Static Approved sprites may use `Tools/P3 Art/Rebuild Approved Sprite Registry` and `Validate Approved Display Specs`.
- Prefab, dynamic portrait, Live2D, DollPuppet, audio, and VFX use their dedicated registry/import path; do not force them through sprite rebuild.
- Menu execution success means only that the command was invoked. Read Console/result evidence and inspect the live Registry state.

## Runtime validation

Delegate to `p3-art-validation`:

- focused asset/screen change: `art_focus`;
- a bounded set of affected screens: `art_runtime`;
- persisted presentation adjustment: `art_iteration` with before/after and reload;
- T0 semantic seal: `t0_art_seal`;
- broad resource/UI change or release: `art_regression`.

Track separately:

```text
approved -> unity_imported -> registered -> runtime_bound -> target_captured -> player_path_verified -> regression_passed
```

Runner visibility proves neither normal player-path reachability nor correct domain behavior.

