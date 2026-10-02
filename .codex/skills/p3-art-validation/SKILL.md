---
name: p3-art-validation
description: Use when Project P3 needs runtime UI or art diagnosis, a light runtime check that moves registered art to runtime_validated, T0 visual sealing, or full visual regression.
---

# P3 Art Validation

The default is the light check: a binding check, a screenshot of the real screen, an Agent review and a clean Console. Passing it moves the checked VisualIDs from `registered` to `runtime_validated`. The ArtRun framework (`p3_art_*` tools) is kept only for `t0_art_seal` and `art_regression`; read [artrun.md](references/artrun.md) before using either.

## Light check

1. Read `agent_status/art.md` and the active UI / art facts for the screen. Name the run `artcheck_<yyyyMMdd_HHmm>_<screen>`; its evidence folder is `UnityClient/Logs/P3ArtCheck/<RunID>/`.
2. Binding: `python tools/美术工具/check_art_binding.py --visual-id <id> [--visual-id <id> ...] --out UnityClient/Logs/P3ArtCheck/<RunID>/binding.json`. Any failure stops the check; Registry and import fixes go to `p3-art-asset-production`.
3. Enter Play Mode and reach the screen through the normal player path. If you can't drive it, ask the user to bring the screen up. If it can't be reached without faking state, stop with `art_blocked:target_screen_unreachable` and hand off to program.
4. Read the Console (`read_console`) so later errors can be told apart. Make sure the Game View is 1920x1080, then take the screenshot: `manage_camera` with `action=screenshot`, `capture_source=game_view`, `output_folder=Logs/P3ArtCheck/<RunID>`, `screenshot_file_name=<screen>.png`, `include_image=true`, and no `camera`, so Screen Space Overlay UI is included. Check that the saved PNG is 1920x1080.
5. Review the image against the spec. For each VisualID, check that it shows where it should, at the right scale, crop and layer, and that the text over it stays readable. Write `review.md` in the evidence folder with the screen, the VisualIDs, what you saw, and a verdict of `passed` or `failed` with reasons.
6. Read the Console again and save new errors and exceptions to `console.txt`, or write `none`. New errors fail the check.
7. Stop Play Mode. If the check passed, add one line to `agent_status/art.md` with the date, RunID, VisualIDs and `runtime_validated`.

## Hard rules

- The check only looks. It doesn't edit UI, prefabs, assets, config or domain state; fixes go to program or `p3-art-asset-production`.
- Never fabricate player state, call arbitrary C# or menus to reach a screen, or run full program smoke tests.
- A screenshot is evidence only when it's saved in the evidence folder. Images that only appear in the conversation are for diagnosis.
- `runtime_validated` isn't written to the Manifest or `RegistryStatus`, and it doesn't imply `player_path_verified` or `regression_passed`, which need their own evidence.
- Program passing doesn't mean art passing, and art passing doesn't mean program passing.
