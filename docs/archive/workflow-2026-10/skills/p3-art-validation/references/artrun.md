# ArtRun profiles

Use the ArtRun framework only for these two profiles. Its design is in `开发文档/19_UnityMCP验收编排层设计.md` §14.

| Profile | Use | Evidence |
|---|---|---|
| `t0_art_seal` | Seal the fixed T0 semantic screens | One `seal` capture per registered seal target |
| `art_regression` | Full visual regression after broad UI changes or before a release | The legacy ArtAcceptance package of the exact source RunID |

Create the run first: `tools/agent/p3-validation-core/New-P3ValidationRun.ps1 -Domain art -ProfileId <profile>`. When you're done, merge it with `Merge-P3ValidationEvidence.ps1 -Domain art -RunId <ArtRunID>`.

## t0_art_seal

1. Pin the Unity instance with `set_active_instance`, then call `p3_validation_readiness`.
2. For each registered seal target, reach the screen through the player path, then call `p3_art_open_target` and `p3_art_inspect_target`. The inspect tool records the live inspection; never hand-write an inspection payload.
3. Set the Game View to the target's reference size (1920x1080). Call `p3_art_prepare_capture` with `capture_role=seal`, run the exact `manage_camera` arguments it returns, then call `p3_art_finalize_capture`. Never import a user-supplied path.
4. After checking the images, UGUI snapshots, binding evidence and Console, record the review with `p3_art_run_profile(action="record_review")`. Then call `p3_art_run_profile(action="complete")`.

## art_regression

Call `p3_art_run_regression` with the ArtRunID. It's the only path that starts the legacy ArtAcceptance runner. It locks the source RunID and imports only that run's report, its UI and Registry snapshots, its checklist, and the PNG files regenerated under its `screenshots/` folder.

If a target can't be reached, record `art_blocked:target_screen_unreachable` and hand off to program. Don't construct fake gameplay state.
