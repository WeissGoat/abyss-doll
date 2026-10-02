---
name: p3-program-validation
description: Run Project P3 compile, smoke-test, config and UI-spec validation; smoke tests run through the Unity Test Framework with Unity MCP run_tests. Use for program regressions and automated tests; never use for art acceptance, screenshots, visual seals, or art review claims.
---

# P3 Program Validation

Smoke tests run as EditMode cases of the `P3.SmokeTests.Editor` assembly (`UnityClient/Assets/Editor/P3SmokeTests/`). Every class whose name ends in `Test` and has a static `Run()` is a case, and so is every entry of the smoke sets in `UnityClient/Assets/Editor/P3Validation/program_validation_profiles.json`. The smoke-set IDs (`foundation`, `t0_core`, `p0_core`) are the case categories. A case fails on any Error, Exception or Assert log.

## Steps

1. Read `agent_status/program.md` and the affected program facts.
2. Read `mcpforunity://editor/state`: the editor must not be playing or compiling. Check `manage_scene(action="get_loaded_scenes")`; if a scene is dirty, ask the user before running.
3. After code changes, call `refresh_unity` with `compile=request`, then `read_console` for errors. A compile error fails the run.
4. Call `run_tests` with `mode=EditMode`, `assembly_names=["P3.SmokeTests.Editor"]`, `include_failed_tests=true`, and the filter for the scope below. Poll `get_test_job` with `wait_timeout=60`. Leave `include_details` off for large runs; the logs are long.
5. Run the static checks the scope needs. Sync configs before step 3 when the config source changed.
6. Report the counts, the failing cases and each one's first error. Say which failures are new compared with the known failures in `agent_status/program.md`. Keep claims within [claims.md](references/claims.md).

| Scope | `run_tests` filter | Static checks |
|---|---|---|
| Small program change | `test_names` of the affected cases, or `category_names` of the affected set | None |
| T0 path | `category_names=["t0_core"]` plus the affected cases | Config sync, UI spec |
| Large change before merging, or P0 | None (all cases) | Config sync, UI spec |

Static checks, run from the repo root:

- Config sync: `.\tools\config\Sync-Configs.ps1 -Clean`
- UI spec: `.\tools\美术工具\Validate-UIDesign.ps1`

Config validation runs inside Unity as the `ConfigValidationSmokeTest.Run` case (category `p0_core`).

A new smoke test is a static class whose name ends in `Test`, with a static `Run()` (see `开发文档/rules/04_自动化测试与验收流程规范.md`). The wrapper picks it up after compiling. Add it to a smoke set only when it belongs to the T0 or P0 gate.

## Hard rules

- No screenshots, art acceptance or visual claims; runtime art goes through `p3-art-validation`.
- Don't use `execute_code`, `manage_ui` or arbitrary `execute_menu_item` as validation steps.
- Without MCP, the compatibility paths are AutoTestDaemon (`UnityClient/Logs/.test_trigger`) and `tools/agent/Invoke-P0Validation.ps1`.
