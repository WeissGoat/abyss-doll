---
id: character_portrait_reference_and_background_processing_plan
title: Character Portrait Reference And Background Processing Implementation Plan
type: plan
role: 美术
domain: character_portrait_production
status: historical
source_of_truth: false
last_verified: 2026-10-01
update_rule: 历史实施计划，不再更新；现行事实以美术文档与 p3-art-asset-production 为准。
---

# Character Portrait Reference And Background Processing Implementation Plan

> 历史计划：已于 2026-07-26 实施（`45b6c1b`、`f783322`）。现行事实以 `美术文档/00_美术流水线总览.md` 与 `.codex/skills/p3-art-asset-production/SKILL.md` 为准。

**Goal:** Resolve logical portrait SourceAssets into real image evidence, pass them through the formal PromptRevision generation route, add guarded explicit background processing, and rerun `doll_zero_cold` without risking its existing selected asset.

**Architecture:** A pure resolver maps AssetSet relationships to approved or selected files. The existing bottom-level generator gains reference-image support and chooses image-to-image only when references exist. A separate background candidate tool writes run-scoped staging outputs, while the existing processing-round registrar remains the only publisher of `processed/<n>`.

**Tech Stack:** Python 3.10, Pillow, unittest, PowerShell 5.1 wrappers, ai-image-gateway `ImageService`, JSON evidence, P3 numeric processing rounds.

## Global Constraints

- Work directly on `main`; do not create a branch or worktree.
- Keep requirements and PromptRevision method-neutral; provider choice belongs to Run evidence.
- Never overwrite published PromptRevisions or numeric processing rounds.
- Preserve the existing `doll_zero_cold` 90-point selected file unless a new candidate passes every hard gate and scores higher.
- Do not modify Approved, `.meta`, GUID, Unity, Registry, Prefab, UGUI, or runtime validation in this mission.
- Use TDD for every code task and commit only scoped files.

---

### Task 1: Resolve Portrait Reference Assets

**Files:**
- Create: `tools/美术工具/portrait_reference_resolver.py`
- Create: `tools/美术工具/tests/test_portrait_reference_resolver.py`

**Interfaces:**
- Consumes: `resolve_portrait_references(manifest: dict, target_entry: dict, project_root: Path) -> list[dict]`.
- Produces: ordered records with `AssetID`, `VisualID`, `Role`, `Path`, `State`, `SHA256`, `Width`, `Height`, and `Mode`.

- [ ] **Step 1: Write failing resolver tests**

Cover Approved priority, Manifest SelectedPath fallback, unique workspace selected fallback, missing AssetID, duplicate AssetID, missing file, corrupt image, multiple selected files, and `_legacy_runs` rejection.

- [ ] **Step 2: Run the focused tests and confirm RED**

Run: `python -m unittest tools.美术工具.tests.test_portrait_reference_resolver -v`

Expected: import failure because `portrait_reference_resolver.py` does not exist.

- [ ] **Step 3: Implement the pure resolver**

Use Manifest facts first, normalize project-relative paths, verify the resolved path remains under the project and outside `_legacy_runs`, decode with Pillow, and calculate SHA-256 from file bytes. Do not write files.

- [ ] **Step 4: Run focused tests and confirm GREEN**

Run: `python -m unittest tools.美术工具.tests.test_portrait_reference_resolver -v`

Expected: all resolver tests pass.

- [ ] **Step 5: Commit**

```powershell
git add -- 'tools/美术工具/portrait_reference_resolver.py' 'tools/美术工具/tests/test_portrait_reference_resolver.py'
git commit -m "feat: resolve portrait reference assets"
```

### Task 2: Add Resolved References To Portrait Plans

**Files:**
- Modify: `tools/美术工具/run_character_portrait_set.py`
- Modify: `tools/美术工具/tests/test_run_character_portrait_set.py`

**Interfaces:**
- Consumes: `resolve_portrait_references(...)` from Task 1.
- Produces: each executable plan item contains `ResolvedReferenceAssets`; unresolved logical sources make the plan `decision_required`.

- [ ] **Step 1: Write failing plan tests**

Add one test proving neutral resolves to an Approved PNG record, one proving missing real files block before execution, and one proving members with no SourceAssets still plan normally.

- [ ] **Step 2: Run focused tests and confirm RED**

Run: `python -m unittest tools.美术工具.tests.test_run_character_portrait_set -v`

Expected: new assertions fail because plans contain only logical `ReferenceAssets`.

- [ ] **Step 3: Integrate the resolver**

Pass `project_root` into plan construction, preserve logical `SourceAssets`, add `ResolvedReferenceAssets`, and surface resolver exceptions as target-scoped plan errors.

- [ ] **Step 4: Run focused tests and confirm GREEN**

Run: `python -m unittest tools.美术工具.tests.test_run_character_portrait_set -v`

Expected: all portrait planning tests pass.

- [ ] **Step 5: Commit**

```powershell
git add -- 'tools/美术工具/run_character_portrait_set.py' 'tools/美术工具/tests/test_run_character_portrait_set.py'
git commit -m "feat: plan portrait references from real assets"
```

### Task 3: Pass Reference Images Through The Formal Generator

**Files:**
- Modify: `tools/美术工具/run_art_generation.py`
- Modify: `tools/美术工具/Run-ArtGeneration.ps1`
- Modify: `tools/美术工具/run_character_portrait_set.py`
- Modify: `tools/美术工具/tests/test_run_art_generation_requests.py`
- Modify: `tools/美术工具/tests/test_run_character_portrait_set.py`

**Interfaces:**
- Consumes: repeated `--reference-image <path>` arguments and exact active PromptRevision.
- Produces: `image_to_image` gateway calls plus generation evidence containing ordered reference path/hash/size records.

- [ ] **Step 1: Write failing generator tests**

Use a fake image service to prove references select `image_to_image`, no references keep `generate`, Prompt text equals the published Variant, changed reference hash fails before the provider call, and evidence stores exact reference records.

- [ ] **Step 2: Run focused tests and confirm RED**

Run: `python -m unittest tools.美术工具.tests.test_run_art_generation_requests tools.美术工具.tests.test_run_character_portrait_set -v`

Expected: reference arguments or image-to-image assertions fail.

- [ ] **Step 3: Implement reference-aware generation**

Add CLI and wrapper parameters, read and verify references before requests, call `ImageService.image_to_image(ImageToImageRequest(...))` when non-empty, and preserve current generate behavior otherwise. The adapter must not append preservation prose.

- [ ] **Step 4: Pass resolved paths from the portrait executor**

Append one `--reference-image` per resolved record in order. Store the portrait-set plan before child execution so recovery can reconstruct inputs.

- [ ] **Step 5: Run focused and wrapper tests**

Run: `python -m unittest tools.美术工具.tests.test_run_art_generation_requests tools.美术工具.tests.test_run_character_portrait_set -v`

Expected: all tests pass and PowerShell dry-run remains UTF-8 safe.

- [ ] **Step 6: Commit**

```powershell
git add -- 'tools/美术工具/run_art_generation.py' 'tools/美术工具/Run-ArtGeneration.ps1' 'tools/美术工具/run_character_portrait_set.py' 'tools/美术工具/tests/test_run_art_generation_requests.py' 'tools/美术工具/tests/test_run_character_portrait_set.py'
git commit -m "feat: generate portrait differences from references"
```

### Task 4: Add Guarded Background Candidate Preparation

**Files:**
- Create: `tools/美术工具/prepare_art_background_candidate.py`
- Create: `tools/美术工具/Prepare-ArtBackgroundCandidate.ps1`
- Create: `tools/美术工具/tests/test_prepare_art_background_candidate.py`
- Reuse: `tools/美术工具/art_background.py`
- Reuse: `tools/美术工具/register_art_processing_round.py`

**Interfaces:**
- Consumes: `--input`, `--staging-dir`, `--method alpha_passthrough|connected_border|explicit_mask`, optional `--mask`, and expected input SHA-256.
- Produces: staged PNG candidate plus `background-processing.json`; never writes `processed/`, selected, or Approved.

- [ ] **Step 1: Write failing background candidate tests**

Cover valid alpha passthrough, connected-border removal, explicit mask application, mask size mismatch, invalid all-black/all-white mask, input hash mismatch, immutable non-empty staging rejection, and refusal of selected/Approved/processed output paths.

- [ ] **Step 2: Run focused tests and confirm RED**

Run: `python -m unittest tools.美术工具.tests.test_prepare_art_background_candidate -v`

Expected: import failure because the tool does not exist.

- [ ] **Step 3: Implement staging-only processing**

Reuse `process_background()` and `measure_candidate()`. For explicit masks, set output Alpha from the supplied mask after size and range checks. Write evidence atomically only after the candidate is decodable and measured.

- [ ] **Step 4: Add the PowerShell wrapper and dry-run test**

The wrapper resolves UTF-8 default paths and mirrors Python exit codes. `-DryRun` prints the plan and writes nothing.

- [ ] **Step 5: Run focused tests and confirm GREEN**

Run: `python -m unittest tools.美术工具.tests.test_prepare_art_background_candidate -v`

Expected: all tests pass.

- [ ] **Step 6: Commit**

```powershell
git add -- 'tools/美术工具/prepare_art_background_candidate.py' 'tools/美术工具/Prepare-ArtBackgroundCandidate.ps1' 'tools/美术工具/tests/test_prepare_art_background_candidate.py'
git commit -m "feat: stage explicit art background processing"
```

### Task 5: Publish Cold PromptRevision 002 And Dry Run

**Files:**
- Modify generated through tool: `美术文档/_generated/art_generation_requests.json`
- Modify generated through tool: `美术文档/_generated/art_manifest.json`
- Runtime evidence: `UnityClient/Logs/P3ArtProduction/<RunID>/prompt-revisions.json`

**Interfaces:**
- Consumes: current `doll_zero_cold` Requirement and approved neutral reference evidence.
- Produces: immutable `doll_zero_cold@<fingerprint>/prompt-002` with independent ready natural-language and Danbooru variants.

- [ ] **Step 1: Export the authoring package**

Run `Export-ArtPromptAuthoringPackage.ps1` for `doll_zero_cold` into a new Run directory and verify it contains context but no executable legacy prompt.

- [ ] **Step 2: Author prompt-002**

Write both formats independently. Preserve identity and cold performance; request real Alpha when supported or a clean segmentation matte when it is not; explicitly forbid painted checkerboards and visible eyes. Map every hard constraint.

- [ ] **Step 3: Publish and validate**

Run `Publish-ArtPromptRevision.ps1`, then strict Catalog validation. Confirm prompt-001 remains byte-identical and prompt-002 becomes active.

- [ ] **Step 4: Dry-run the portrait set**

Run `Run-CharacterPortraitSet.ps1 -AssetSetID zero_dialogue_portrait_v1 -VisualID doll_zero_cold -DryRun`. Confirm the exact prompt-002 and neutral Approved path/hash appear.

- [ ] **Step 5: Commit generated Catalog changes only**

```powershell
git add -- '美术文档/_generated/art_generation_requests.json' '美术文档/_generated/art_manifest.json'
git commit -m "feat: author cold portrait revision 002"
```

### Task 6: Execute And Evaluate The Cold Pilot

**Files:**
- Runtime evidence only: `UnityClient/Logs/P3ArtProduction/<ProductionRunID>/`
- Ignored workspace only: `UnityClient/Assets/Art/_IncomingAI/character_portraits/doll_zero_cold/`

**Interfaces:**
- Consumes: prompt-002, resolved neutral reference, available providers, existing selected baseline.
- Produces: raw evidence and either no new round or guarded `processed/3` plus a selection decision.

- [ ] **Step 1: Record the immutable baseline**

Record existing processed round, selected path, score, SHA-256, Approved hashes and Registry hashes before provider calls.

- [ ] **Step 2: Check provider configuration and smoke the selected backend**

Run config-only, then minimum Gemini smoke. Stop with `validation_limited:provider_unavailable:<provider>` if no successful image exists.

- [ ] **Step 3: Generate two reference-driven candidates**

Execute the portrait route with Gemini and `Variants=2`, each request `count=1`. Switch provider only with explicit Run evidence when capability results justify it.

- [ ] **Step 4: Prepare background candidates**

For each promising raw, choose the smallest safe explicit method. Write only to Run staging. Do not publish a numeric round until the candidate passes deterministic checks.

- [ ] **Step 5: Register the next round when eligible**

Use `Register-ArtProcessingRound.ps1` with input hashes and decision evidence. Expected next round is `processed/3`; if no candidate passes, publish nothing.

- [ ] **Step 6: Review and guarded selection**

Inspect full-size, dialogue-size, neutral comparison and old cold comparison. Replace selected only when the new candidate has no hard failures and scores at least 91. Otherwise record `preserve_existing_selected`.

### Task 7: Documentation, Full Verification And Mission Review

**Files:**
- Modify: `tools/美术工具/README.md`
- Modify: `.codex/skills/p3-art-asset-production/SKILL.md` only if the stable entry commands changed
- Modify: `agent_status/art.md`
- Modify: `PROJECT_STATUS.md` only if the project-level blocker changes
- Generated: `docs_index.json`

**Interfaces:**
- Consumes: implementation commits and Pilot evidence.
- Produces: durable facts and evidence-backed final claim.

- [ ] **Step 1: Update tool and workflow facts**

Document reference resolution, reference-aware generation, staging-only background processing and the actual cold outcome without claiming Approved or runtime completion.

- [ ] **Step 2: Run complete validation**

```powershell
python -m unittest discover -s tools/美术工具/tests -p "test_*.py" -v
python tools/美术工具/validate_art_generation_requests.py --manifest 美术文档/_generated/art_manifest.json --request-catalog 美术文档/_generated/art_generation_requests.json --strict
.\tools\docs\Generate-DocsIndex.ps1
.\tools\docs\Validate-Docs.ps1
.\tools\agent\Invoke-AgentHealthCheck.ps1 -Strict
git diff --check
```

- [ ] **Step 3: Verify invariants**

Compare Approved images, `.meta`, GUID and Registry hashes to the baseline. Verify the old selected is unchanged unless a strictly better candidate was selected through the guarded command.

- [ ] **Step 4: Commit scoped documentation and generated index changes**

Commit only files owned by this task. Keep `_IncomingAI`, Run images, local config and provider smoke evidence ignored.

- [ ] **Step 5: Complete Mission review**

Check every implementation and Pilot acceptance item against evidence, record any `validation_limited:*`, and leave the working tree clean.
