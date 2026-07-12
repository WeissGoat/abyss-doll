---
name: p3-art-validation
description: Use when Project P3 needs runtime UI or art diagnosis, live Game View inspection, focused visual acceptance, bounded presentation iteration, T0 visual sealing, or full visual regression.
---

# P3 Art Validation

Use MCP live inspection as the default; capture only decision evidence.

1. Read `agent_status/art.md`, active UI/art facts, and choose a registered TargetID and Profile.
2. Create an ArtRunID and pin the Unity instance with `set_active_instance`.
3. Call `p3_validation_readiness`, `p3_art_open_target`, and `p3_art_inspect_target`.
4. View the current Game View with `manage_camera(action="screenshot", capture_source="game_view", include_image=true)`; omit `camera` so Screen Space Overlay UGUI is included. Before formal capture, select the registered target's `1920x1080` Game View size.
5. Diagnose from the live image plus the bounded UGUI snapshot.
6. When evidence is needed, call `p3_art_prepare_capture`, execute the returned exact `manage_camera` arguments, then call `p3_art_finalize_capture`. A live image with other dimensions is diagnosis only.
7. Complete `p3_art_run_profile` and merge the ArtRunID result.

Read [profile-routing.md](references/profile-routing.md), [mcp-live-inspection.md](references/mcp-live-inspection.md), [evidence-policy.md](references/evidence-policy.md), and [iteration-boundaries.md](references/iteration-boundaries.md).

Never accept arbitrary screenshot paths, fabricate player state, run full P0, or mutate domain rules. Only `art_regression` may start the legacy ArtAcceptance runner.
