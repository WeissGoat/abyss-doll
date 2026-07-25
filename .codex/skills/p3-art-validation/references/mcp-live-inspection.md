# MCP live inspection

Use this order:

```text
set_active_instance
-> p3_validation_readiness
-> p3_art_open_target
-> p3_art_inspect_target
-> manage_camera screenshot, game_view, include_image=true, camera omitted
-> diagnose
-> p3_art_prepare_capture / manage_camera / p3_art_finalize_capture
-> p3_art_run_profile record_review
-> p3_art_run_profile finalize_runtime_validated
```

If the target is unreachable, record `art_blocked:target_screen_unreachable` and hand off to program. Do not construct fake gameplay state.

`p3_art_inspect_target` writes the bounded snapshot and records it in the active ArtRun. Do not use a manually constructed `record_inspection` payload. The live inspector derives VisualID only from `Assets/Art/Approved/`, then compares its path and GUID with the live `VisualAssetRegistry`; `_IncomingAI` or a duplicate live match is a binding failure.

The finalizer requires a registered TargetID, a non-empty target binding contract, passed binding evidence, no blocking inspection issue, a required final/seal/after capture, and an approved Agent review. It is idempotent for an already finalized ArtRun.

For a formal capture, the Game View must use the target's registered reference size (currently `1920x1080`). The inline image may be downscaled by `max_resolution`; `p3_art_finalize_capture` validates the original PNG dimensions.
