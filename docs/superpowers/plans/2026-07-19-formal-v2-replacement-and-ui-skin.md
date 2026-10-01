---
id: formal_v2_replacement_and_ui_skin_plan
title: Formal V2 Replacement And UI Skin Implementation Plan
type: plan
role: 美术
domain: art_pipeline
status: historical
source_of_truth: false
last_verified: 2026-10-01
update_rule: 历史实施计划，不再更新；现行事实以美术文档与 p3-art-asset-production 为准。
---

# Formal V2 Replacement And UI Skin Implementation Plan

> 历史计划：已于 2026-07-26 实施（`839aff9`）。现行事实以 `美术文档/00_美术流水线总览.md` 与 `.codex/skills/p3-art-asset-production/SKILL.md` 为准。

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task with review checkpoints.

**Goal:** Add a Formal V2 active-asset replacement bridge and make the deterministic candidate gate explicitly validate nine-slice UI skins.

**Architecture:** Keep Manifest and Formal V2 documents as facts. A new generated replacement report reads the active Formal V2 overview plus Manifest and emits `visual_v2_replace` items; it does not mutate Approved or create a second progress table. The shared candidate reviewer receives optional nine-slice contract data and applies UI-specific connected-component and edge-coverage checks while preserving existing standard-asset behavior.

**Tech Stack:** Python 3, PowerShell wrappers, Pillow, unittest, existing P3 art workspace and generated-report conventions.

## Global Constraints

- `UnityClient/Assets/Art/Approved` is unchanged by the bridge or technical reviewer.
- New processing remains under `_IncomingAI/standard_assets/<VisualID>/processed/<next integer>/`.
- `visual_v2_replace` and `generate_needed` remain separate actions.
- Provider and generation method remain Agent-selected; the replacement plan must not hard-code a provider.
- Existing `processed/<n>` rounds are immutable.

### Task 1: Formal V2 Replacement Bridge

**Files:**
- Create: `tools/美术工具/generate_formal_v2_replacement_plan.py`
- Create: `tools/美术工具/Generate-FormalV2ReplacementPlan.ps1`
- Create: `tools/美术工具/tests/test_generate_formal_v2_replacement_plan.py`

**Interfaces:**
- `parse_formal_v2_overview(path: Path) -> dict[str, str]` extracts backtick VisualIDs from the Formal V2 UI Skin table and rows marked `V2-A active`.
- `build_payload(overview_path: Path, manifest_path: Path, batch_id: str, variants: int, delay_seconds: float) -> dict[str, object]` returns `Items`, `Summary`, and `RunConfig` with `Action=visual_v2_replace`.
- CLI writes `美术文档/_generated/FormalV2主动迭代计划.json/.md` and optional timestamped snapshots.

- [x] Write tests for active-row extraction, draft-row exclusion, Manifest filtering, and method-neutral plan items.
- [x] Run `python -m unittest tools/美术工具/tests/test_generate_formal_v2_replacement_plan.py -v` and verify the new tests fail before implementation.
- [x] Implement the parser and payload builder using UTF-8 JSON/Markdown output and existing workspace resolution.
- [x] Run the focused tests and verify the bridge emits only Manifest-managed, Approved-backed VisualIDs.

### Task 2: Nine-Slice Candidate Gate

**Files:**
- Modify: `tools/美术工具/art_background.py`
- Modify: `tools/美术工具/optimize_art_assets.py`
- Modify: `tools/美术工具/tests/test_art_background.py`
- Modify: `tools/美术工具/tests/test_optimize_art_assets.py`

**Interfaces:**
- Extend `review_candidate(..., process_spec: Mapping[str, Any] | None = None, asset_type: str = "")` without breaking existing callers.
- When `ProcessSpec.NineSlice.Enabled` is true, report `NineSliceMetrics`, fail `nine_slice_many_components` above the configured/default limit of 24, and fail `nine_slice_edge_coverage_low` when any configured border band has less than 35% alpha coverage.

- [x] Add failing tests for disconnected UI fragments and insufficient border coverage.
- [x] Run the focused tests and verify the new assertions fail before implementation.
- [x] Pass `ProcessSpec` and `AssetType` from `optimize_art_assets.py` into `review_candidate`.
- [x] Implement bounded border-band metrics and UI-specific hard failures; keep standard icons at warning-only component behavior.
- [x] Run `python -m unittest tools/美术工具/tests/test_art_background.py tools/美术工具/tests/test_optimize_art_assets.py -v`.

### Task 3: Documentation And Generated-Report Contract

**Files:**
- Modify: `tools/美术工具/README.md`
- Modify: `美术文档/00_美术流水线总览.md`
- Modify: `agent_status/art.md`

- [x] Document the replacement bridge command, output, and separation from `缺图生成计划`.
- [x] Document UI Skin routing and the `nine_slice_*` technical failure meanings.
- [x] Record the new bridge and UI gate as the next art-pipeline focus in the art status page.

### Task 4: First Repair Dry Run

**Files:**
- Generated only: `美术文档/_generated/FormalV2主动迭代计划.*` and snapshot evidence.

- [x] Run the replacement bridge against current Formal V2/Manifest facts.
- [x] Confirm `ui_button_primary` is present with `Action=visual_v2_replace` and `AssetClass=ui_skin`.
- [x] Run a dry-run repair plan for `ui_button_primary` only; do not call the provider and do not touch Approved.
- [x] Run generated JSON validation and the focused tests.

### Task 5: Batch Generation And Processing Executor

**Files:**
- Create: `tools/美术工具/run_art_production_batch.py`
- Create: `tools/美术工具/Run-ArtProductionBatch.ps1`
- Create: `tools/美术工具/tests/test_run_art_production_batch.py`

**Interfaces:**
- Consume either `缺图生成计划.json` or `FormalV2主动迭代计划.json`.
- Resolve runtime provider routes without changing the method-neutral source plan.
- Execute existing `run_art_generation.py` and `optimize_art_assets.py` commands per isolated provider/status group.
- Stop at latest numeric `processed/<n>` and write ProductionRun evidence; never touch selected or Approved.

- [x] Cover missing-asset and same-VisualID replacement plan shapes with failing tests.
- [x] Implement runtime route parsing, filtering, grouping, dry-run commands, execution, and resumable evidence.
- [x] Verify unrouted or prompt-blocked items are reported without entering a generation group.

### Task 6: Guarded Candidate Selection

**Files:**
- Create: `tools/美术工具/select_art_candidate.py`
- Create: `tools/美术工具/Select-ArtCandidate.ps1`
- Create: `tools/美术工具/tests/test_select_art_candidate.py`

**Interfaces:**
- Consume the Agent-authored ProductionRun `visual-review.json` item for one VisualID.
- Require the reviewed candidate to belong to the latest numeric round, pass the decision/hash contract, and score at least 88.
- Copy only to the Profile workspace `selected/`, update Manifest `SelectedPath`, preserve approved/registered/validated main status, and write `production_decision.json`.
- Require explicit permission before replacing an existing selected file; never touch Approved.

- [x] Add failing tests for successful selection, status preservation, overwrite permission, stale-round rejection, threshold rejection, and dry-run.
- [x] Implement atomic selected/Manifest/evidence writes.
- [x] Add PowerShell wrapper and focused verification.

### Task 7: End-To-End Standard Batch Pilot

- [x] Dry-run a bounded Formal V2 standard-asset batch with Agent-selected runtime provider routes.
- [x] Execute generation and processing without including Approved, Unity, or Registry.
- [x] Inspect latest numeric rounds, author `visual-review.json`, and use the guarded selector for qualifying candidates.
- [x] Record final per-VisualID claims as selected, processed_failed, or decision_required.
- [x] Run the complete art-tool tests, generated JSON validation, docs validation, and diff checks. The only non-green generated check is the pre-existing offline Registry candidate mismatch.

### Task 8: UI Skin Capability Route And Button Pilot

**Files:**
- Create: `tools/美术工具/generate_ui_skin_candidate.py`
- Create: `tools/美术工具/Generate-UISkinCandidate.ps1`
- Create: `tools/美术工具/tests/test_generate_ui_skin_candidate.py`
- Modify: `tools/美术工具/run_art_production_batch.py`
- Modify: `tools/美术工具/tests/test_run_art_production_batch.py`

**Interfaces:**
- `generate_ui_skin_candidates(...)` consumes one Manifest entry whose `ProcessSpec.NineSlice.Enabled=true`, a runtime-selected capability, BatchID, and variant count.
- `deterministic_template` is the first capability adapter. It derives canvas, border, accent family, empty content area, and engineering geometry from the Asset Contract; it is a runtime route, not a Manifest field or stable directory name.
- Outputs use the existing Profile workspace `raw/`, update only replacement candidate fields while preserving the main status, and write method evidence to `generation.json`.
- `Run-ArtProductionBatch.ps1 -Route ui_skin=deterministic_template` calls the UI Skin adapter, then the existing optimizer and nine-slice gates. It never calls a generic image provider for that group.

- [x] Write failing tests for NineSlice admission, two decodable method-evidence raw candidates, current-status preservation, technical-gate compatibility, and batch command routing.
- [x] Implement deterministic button, panel/list-row, and status-bar template families with contract-derived borders and transparent output.
- [x] Integrate the capability command into the generic batch executor without changing non-UI provider routes.
- [x] Run a bounded `ui_button_primary` dry-run and real Run; publish the next immutable round as `processed/3`.
- [x] Inspect source-size and 220x64 previews, author visual review, and use guarded selection only if threshold and lead requirements pass.
- [x] Update the tool README, art pipeline overview, Skill execution rule, art status, and full verification evidence.
