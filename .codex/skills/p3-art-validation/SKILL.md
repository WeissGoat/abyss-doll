---
name: p3-art-validation
description: Use when Project P3 needs runtime UI or art diagnosis, live Game View inspection, focused visual acceptance, bounded presentation iteration, T0 visual sealing, or full visual regression.
---

# P3 Art Validation

Use MCP live inspection as the default; capture only decision evidence. The public art state transition is `registered -> runtime_validated`; binding is an internal ArtRun check, not a separate user-facing state.

1. Read `agent_status/art.md`, active UI/art facts, and choose a registered TargetID and Profile.
2. Create an ArtRunID and pin the Unity instance with `set_active_instance`.
3. Call `p3_validation_readiness`, `p3_art_open_target`, and `p3_art_inspect_target`. The inspect tool records the live inspection into the ArtRun; do not submit a hand-authored inspection JSON.
4. View the current Game View with `manage_camera(action="screenshot", capture_source="game_view", include_image=true)`; omit `camera` so Screen Space Overlay UGUI is included. Before formal capture, select the registered target's `1920x1080` Game View size.
5. Diagnose from the live image plus the bounded UGUI snapshot.
6. When evidence is needed, call `p3_art_prepare_capture`, execute the returned exact `manage_camera` arguments, then call `p3_art_finalize_capture`. A live image with other dimensions is diagnosis only.
7. Record the Agent art review with `p3_art_run_profile(action="record_review")` only after checking the live image, bounded UGUI snapshot, registry/binding evidence, and target Console.
8. Call `p3_art_run_profile(action="finalize_runtime_validated")`. Only this action may write the `runtime_validated` claim and `runtime-validation.json`.
9. Complete the profile / merge the ArtRunID result. Keep `player_path_verified` and `regression_passed` as separate claims.

Read each reference when its step comes up: [profile-routing.md](references/profile-routing.md) when choosing the Profile (step 1), [mcp-live-inspection.md](references/mcp-live-inspection.md) before steps 2-5, [evidence-policy.md](references/evidence-policy.md) before formal capture, review, or finalize (steps 6-9), and [iteration-boundaries.md](references/iteration-boundaries.md) only for `art_iteration` or any persisted presentation change.

Never accept arbitrary screenshot paths, hand-author binding evidence, fabricate player state, run full P0, or mutate domain rules. A target with no `required_visual_ids`, a missing Registry entry, a wrong live Sprite path/GUID, a blocking issue, missing final capture, or a failed/limited review cannot finalize. Only `art_regression` may start the legacy ArtAcceptance runner.
