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
- each selected `character_portrait_set` member belongs to an AssetSet whose `LatestConsistencyReview` is `passed` for the exact current `SetSnapshotFingerprint`.

For a formal static-Sprite handoff, start with:

```powershell
.\tools\美术工具\Invoke-ArtApprovedUnityRegistration.ps1 `
  -Phase Plan `
  -ArtImportRunID <ArtImportRunID> `
  -VisualID <VisualID> `
  -UnityInstance <Name@hash>
```

Review `approved-plan.json` at the configured gate. After authorization, use the same entry with `-Phase SyncApproved -AuthorizeApprovedSync` plus the explicit new-target or overwrite permission. The entry delegates all formal file writes to `Sync-ApprovedArt.ps1`; do not manually copy around its guards.

For a portrait set, run `Invoke-PortraitSetGate.ps1 -Phase Check -AssetSetID <AssetSetID>` before the Approved plan. Missing or stale set evidence blocks both direct sync and the registration plan. A failed retrospective review does not roll back existing Approved/Registry bytes, but it prevents further Approved replacement and `runtime_validated` claims.

## Unity import

After `SyncApproved`:

1. Pin the exact Unity instance and collect the Console baseline.
2. Refresh Unity and use an internal `Wait-UnityIdle`; this is infrastructure, not a production state.
3. Temporarily enable `scripting_ext` and collect a target-bounded importer pre-snapshot.
4. Execute `Tools/P3 Art/Rebuild Approved Sprite Registry` for static Approved sprites.
5. Refresh and wait again, then collect the importer post-snapshot and live Registry target snapshot.
6. Execute `Tools/P3 Art/Validate Approved Display Specs` and collect the Console delta.
7. Restore `scripting_ext=false`.
8. Write `unity-import.json`, `registry-result.json`, and `console-delta.json`, then call `-Phase Finalize`.

The normal session tool policy remains `core + testing + docs`. `scripting_ext` is on-demand only because core asset tools cannot expose every TextureImporter field or ScriptableObject Registry entry.

Approved on disk is not `unity_imported` until the live Editor recognizes the expected asset.

## Registry

- Static Approved sprites may use `Tools/P3 Art/Rebuild Approved Sprite Registry` and `Validate Approved Display Specs`.
- Prefab, dynamic portrait, Live2D, DollPuppet, audio, and VFX use their dedicated registry/import path; do not force them through sprite rebuild.
- Menu execution success means only that the command was invoked. Read Console/result evidence and inspect the live Registry state.
- A target is `registered` only when the raw Registry Entries contain exactly one matching VisualID, `TryGetEntry` succeeds, the Sprite is non-null, and its AssetPath/GUID match the Approved target.
- The rebuild menu scans all Approved sprites and does not remove stale entries. Block Approved basename collisions before invoking it; keep unrelated stale-entry diagnostics separate from the target result.

Finalize writes Manifest `RegistryStatus=registered` while keeping the production-axis `Status=approved`. It also refreshes integration candidates, program handoff, and Registry-gap reports. Do not add a `unity_available` field or set the main Manifest Status to `registered`.

## Runtime validation

Delegate to `p3-art-validation`:

- focused asset/screen change: `art_focus`;
- a bounded set of affected screens: `art_runtime`;
- persisted presentation adjustment: `art_iteration` with before/after and reload;
- T0 semantic seal: `t0_art_seal`;
- broad resource/UI change or release: `art_regression`.

Track separately:

```text
approved -> unity_imported -> registered -> runtime_validated -> player_path_verified -> regression_passed
```

`runtime_validated` is produced by `p3-art-validation` ArtRun Finalize after the formal TargetID consumes the registered VisualID and binding, display, Console, capture, and Agent review checks pass. Do not create or expose a separate `runtime_bound` claim.

Runner visibility proves neither normal player-path reachability nor correct domain behavior.
