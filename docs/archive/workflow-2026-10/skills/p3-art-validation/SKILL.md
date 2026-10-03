---
name: p3-art-validation
description: Use when Project P3 needs runtime UI or art diagnosis, a light runtime check that moves registered art to runtime_validated, T0 visual sealing, or full visual regression.
---

# P3 Art Validation

The default is the light check: a binding check, a screenshot of the real screen, an Agent review and a clean Console. Passing it moves the checked VisualIDs from `registered` to `runtime_validated`. The ArtRun framework (`p3_art_*` tools) is kept only for `t0_art_seal` and `art_regression`; read [artrun.md](references/artrun.md) before using either.

## Light check

1. Read `agent_status/art.md` and the active UI / art facts for the screen. Name the run `artcheck_<yyyyMMdd_HHmm>_<screen>`; its evidence folder is `UnityClient/Logs/P3ArtCheck/<RunID>/`.
2. Binding: `python tools/美术工具/check_art_binding.py --visual-id <id> [--visual-id <id> ...] --out UnityClient/Logs/P3ArtCheck/<RunID>/binding.json`. Any failure stops the check; Registry and import fixes go to `p3-art-asset-production`.
3. Enter Play Mode and reach the screen any way that works: the normal player path, a debug menu, `execute_code`, or a test fixture. Play Mode changes made to get there are fine because nothing is saved; note the route in `review.md`. Seals (`t0_art_seal`) still use the player path. If the screen doesn't exist in the game yet, stop with `art_blocked:target_screen_unreachable` and hand off to program.
4. Read the Console (`read_console`) so later errors can be told apart. Make sure the Game View is 1920x1080, then take the screenshot: `manage_camera` with `action=screenshot`, `capture_source=game_view`, `output_folder=Logs/P3ArtCheck/<RunID>`, `screenshot_file_name=<screen>.png`, `include_image=true`, and no `camera`, so Screen Space Overlay UI is included. Check that the saved PNG is 1920x1080.
5. Review the image against the spec. For each VisualID, check that it shows where it should, at the right scale, crop and layer, and that the text over it stays readable. Write `review.md` in the evidence folder with the screen, the route, the VisualIDs, what you saw, and a verdict of `passed` or `failed` with reasons.
6. Read the Console again and save new errors and exceptions to `console.txt`, or write `none`. New errors fail the check.
7. Stop Play Mode. If the check passed, add one line to `agent_status/art.md` with the date, RunID, VisualIDs and `runtime_validated`.

## Hard rules

- Save nothing you changed to reach or inspect the screen: no scene, prefab, asset, config or save-data writes. Fixes go to program or `p3-art-asset-production`. Why: the check must not change the game it checks.
- A debug route proves the art renders, not that players can get there: it gives `runtime_validated`, never `player_path_verified`. Why: the two claims answer different questions.
- Don't run full program smoke tests. Why: that is program validation, and it takes minutes.
- A screenshot is evidence only when it's saved in the evidence folder. Images that only appear in the conversation are for diagnosis. Why: evidence has to outlive the conversation.
- `runtime_validated` isn't written to the Manifest or `RegistryStatus`, and it doesn't imply `player_path_verified` or `regression_passed`, which need their own evidence. Why: a runtime result belongs to a screen and a date, and goes stale when either changes.
- Program passing doesn't mean art passing, and art passing doesn't mean program passing. Why: they check different things.
