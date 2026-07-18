# Zero Dialogue Neutral Character Portrait Pilot Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Admit `doll_zero_dialogue_neutral` as the first `character_portrait_set` member, import the existing dialogue candidate, preprocess and review it, then stop before Approved.

**Architecture:** The preset seed remains the requirement source, `Update-ArtManifest.ps1` owns Manifest regeneration, and `art_workspace.py` owns Profile paths. A new narrow import tool handles existing project images without pretending they were generated. Existing preprocessing then produces the technical candidate and contact sheet; the pilot writes a resumable decision record only.

**Tech Stack:** Python 3.10, Pillow, JSON, PowerShell wrappers, Project P3 Manifest and art workspace scripts.

## Global Constraints

- `VisualID=doll_zero_dialogue_neutral`.
- `ProductionProfile=character_portrait_set`.
- `AssetSetID=zero_dialogue_portrait_v1`, `AssetID=zero_dialogue_neutral`, `SetRole=neutral_dialogue_master`.
- Workspace is exactly `UnityClient/Assets/Art/_IncomingAI/character_portraits/doll_zero_dialogue_neutral/`.
- Source is exactly `美术文档/人设/AI出图/zero_dialogue_differences_20260712_01/selected/zero_dialogue_neutral.png`.
- Output contract is `UnityClient/Assets/Art/Approved/Dolls/doll_zero_dialogue_neutral.png`, but this file must not be created or changed.
- ProductionRunID is `zero_dialogue_neutral_pilot_20260718_01`.
- Do not modify `doll_proto_0_stand`, Approved, Registry, Prefabs, runtime code, or Unity state.
- Do not read `_legacy_runs`, use a flat `_IncomingAI/<VisualID>` path, or infer the image method from the Profile.
- Maximum claim is `processed_candidate` or `decision_required`.
- Preserve unrelated dirty-worktree changes and do not auto-commit implementation files.

---

### Task 1: Admit the formal preset requirement

**Files:**
- Modify: `美术文档/art_requirements_seed.json`
- Generated: `美术文档/_generated/art_manifest.json`
- Generated: `美术文档/_generated/视觉资产Manifest.md`

**Interfaces:**
- Consumes: `update_art_manifest.add_preset_assets()`.
- Produces: a unique Manifest entry resolved by `art_workspace.workspace_path()`.

- [ ] **Step 1: Save protected hashes**

Save SHA-256 records under `$env:TEMP/P3CharacterPortraitPilot/protected-before.json` for all files under `UnityClient/Assets/Art/Approved`, `UnityClient/Assets/Resources/VisualAssetRegistry.asset`, and the current Manifest.

Expected: no project files change.

- [ ] **Step 2: Add the exact seed entry**

```json
{
  "Domain": "doll",
  "PresetCategory": "character_dialogue_portrait",
  "ConfigID": "zero_dialogue_neutral",
  "DisplayName": "零号-对话中性立绘",
  "AssetType": "portrait",
  "VisualID": "doll_zero_dialogue_neutral",
  "ProductionProfile": "character_portrait_set",
  "AssetSetID": "zero_dialogue_portrait_v1",
  "AssetID": "zero_dialogue_neutral",
  "SetRole": "neutral_dialogue_master",
  "SourceAssets": [],
  "OutputPath": "UnityClient/Assets/Art/Approved/Dolls/doll_zero_dialogue_neutral.png",
  "Priority": "P0",
  "Screen": "workshop_main,doll_room",
  "Usage": "工坊与人偶房间默认对话立绘母版",
  "ProgramReference": "future_dialogue_portrait_binding",
  "SourceFactsCN": "零号 P0 对话中性立绘：3/4 对话站姿，银白长发自然散发，白布遮眼，灰披肩完整覆盖双肩和胸口，浅色独立内衬裙，裸腿裸足，常态无红光；来源见零号初版人设方案与立绘素材设计交付清单。"
}
```

- [ ] **Step 3: Regenerate and assert the Manifest**

Run:

```powershell
.\tools\美术工具\Update-ArtManifest.ps1
```

Expected: 300 entries. Assert the new entry contains the exact Profile, IDs, set fields and OutputPath above, with `Status=todo`.

### Task 2: Add a safe existing-candidate import tool using TDD

**Files:**
- Create: `tools/美术工具/import_art_candidate.py`
- Create: `tools/美术工具/Import-ArtCandidate.ps1`
- Create: `tools/美术工具/tests/test_import_art_candidate.py`
- Modify: `tools/美术工具/README.md`

**Interfaces:**
- Consumes: Manifest path, incoming root, VisualID, source image, batch ID, destination filename, source review path and dry-run flag.
- Produces: copied raw image, `reference_inputs.json`, `generation.json`, and updated Manifest fields `BatchID`, `RawPath`, `Status=generated`.

- [ ] **Step 1: Write failing tests**

Tests must cover:

```python
def test_import_uses_character_profile_workspace_and_updates_only_target_entry(): ...
def test_import_rejects_missing_or_duplicate_visual_id(): ...
def test_import_rejects_source_inside_legacy_runs(): ...
def test_dry_run_writes_nothing(): ...
```

The successful case must assert destination `character_portraits/doll_zero_dialogue_neutral/raw/r01_001.png`, identical SHA-256, preserved unrelated entry, and no `SelectedPath` or `ApprovedPath`.

- [ ] **Step 2: Run tests and verify RED**

Run:

```powershell
python -X utf8 -m unittest tools.美术工具.tests.test_import_art_candidate -v
```

Expected: FAIL because `import_art_candidate` does not exist.

- [ ] **Step 3: Implement the minimal Python tool**

Required callable:

```python
def import_candidate(
    *,
    manifest_path: Path,
    incoming_root: Path,
    visual_id: str,
    source_path: Path,
    destination_name: str,
    batch_id: str,
    source_review: str,
    dry_run: bool = False,
) -> dict[str, object]:
    ...
```

Rules:

- require exactly one Manifest entry;
- resolve destination only with `workspace_path(incoming_root, entry)`;
- reject source paths under `incoming_root/_legacy_runs`;
- accept only decodable PNG/JPG/JPEG/WEBP images;
- destination filename must be one safe filename segment;
- copy bytes with `shutil.copy2`;
- record SHA-256, dimensions, mode, source/destination repo paths, source review, `capability=import_existing_candidate`, timestamp and batch ID;
- set only `BatchID`, `RawPath`, `Status=generated`, and append an import note;
- never set selected, Approved, Registry or runtime state.

- [ ] **Step 4: Add the PowerShell wrapper and README**

Wrapper parameters:

```powershell
param(
  [string]$ManifestPath = "",
  [string]$IncomingRoot = "",
  [Parameter(Mandatory=$true)][string]$VisualID,
  [Parameter(Mandatory=$true)][string]$SourcePath,
  [string]$DestinationName = "r01_001.png",
  [Parameter(Mandatory=$true)][string]$BatchID,
  [string]$SourceReview = "",
  [switch]$DryRun
)
```

- [ ] **Step 5: Run tests and verify GREEN**

Run the focused import tests, full `tools/美术工具/tests` suite, and `py_compile` for the new tool.

Expected: all pass.

### Task 3: Populate prompt/spec and import the candidate

**Files:**
- Generated/modify: `美术文档/_generated/art_manifest.json`
- Create: `UnityClient/Assets/Art/_IncomingAI/character_portraits/doll_zero_dialogue_neutral/asset_contract.json`
- Create: `UnityClient/Assets/Art/_IncomingAI/character_portraits/doll_zero_dialogue_neutral/production_plan.json`
- Create/generated: raw and provenance files.

**Interfaces:**
- Consumes: Tasks 1-2.
- Produces: one generated-state Manifest member and raw imported candidate.

- [ ] **Step 1: Generate prompt/spec only for the new VisualID**

```powershell
.\tools\美术工具\Generate-ArtPrompts.ps1 -VisualID doll_zero_dialogue_neutral
```

Assert `Status=prompted`, `1024x1536`, transparent background, alpha required, contain, bottom-center, 6% safe padding and 94% baseline.

- [ ] **Step 2: Write the contract and plan**

Write `asset_contract.json` from the approved spec and:

```json
{
  "production_run_id": "zero_dialogue_neutral_pilot_20260718_01",
  "mode": "interactive",
  "operation": "new_asset",
  "production_profile": "character_portrait_set",
  "selected_capability": "import_existing_candidate",
  "allow_approved_sync": false,
  "resume_from": "PREPROCESS"
}
```

- [ ] **Step 3: Dry-run the import**

```powershell
.\tools\美术工具\Import-ArtCandidate.ps1 -VisualID doll_zero_dialogue_neutral -SourcePath "美术文档/人设/AI出图/zero_dialogue_differences_20260712_01/selected/zero_dialogue_neutral.png" -DestinationName r01_001.png -BatchID imported_zero_dialogue_neutral_20260718_01 -SourceReview "美术文档/人设/AI出图/zero_dialogue_differences_20260712_01/selection_review.md" -DryRun
```

Expected: destination under `character_portraits`; no writes.

- [ ] **Step 4: Execute the import**

Run the same command without `-DryRun`. Assert source/destination SHA-256 match, `Status=generated`, correct `RawPath`, and empty Selected/Approved paths.

### Task 4: Preprocess and inspect the candidate

**Files:**
- Generated: `processed/r01_001.png`
- Generated: `contact_sheet/doll_zero_dialogue_neutral_contact_sheet.png`
- Generated: `process_report.json`

**Interfaces:**
- Consumes: imported raw and Manifest Spec.
- Produces: processed candidate, contact sheet and technical metrics.

- [ ] **Step 1: Run preprocessing dry-run**

```powershell
.\tools\美术工具\Optimize-ArtAssets.ps1 -VisualID doll_zero_dialogue_neutral -Status generated -Limit 1 -DryRun -SkipIntegrationCandidates
```

Expected: `selected=1`, `raw=1`, `spec=1024x1536`.

- [ ] **Step 2: Run preprocessing**

```powershell
.\tools\美术工具\Optimize-ArtAssets.ps1 -VisualID doll_zero_dialogue_neutral -Status generated -Limit 1 -Overwrite -SkipIntegrationCandidates
```

Expected: one output and one contact sheet; no Approved write.

- [ ] **Step 3: Measure technical integrity**

Use Pillow to assert PNG, RGBA, 1024x1536, transparent corners, nonempty alpha bbox and safe canvas margins. Record transparent ratio, alpha bbox occupancy and suspicious interior transparency in white hair/blindfold regions.

- [ ] **Step 4: Inspect visually**

Inspect processed PNG and contact sheet at full size. Check hair edges, blindfold, feet, fingers, cloak/dress separation, baseline, identity and whether the pose is sufficiently 3/4 for dialogue.

### Task 5: Record the decision, refresh reports and verify boundaries

**Files:**
- Create: `production_decision.json`
- Create: `UnityClient/Logs/P3ArtProduction/zero_dialogue_neutral_pilot_20260718_01/summary.json`
- Modify: `agent_status/art.md`
- Refresh owned generated reports and docs index.

**Interfaces:**
- Consumes: Task 4 metrics and visual inspection.
- Produces: `processed_candidate` or `decision_required`.

- [ ] **Step 1: Score and decide**

Score semantic 25, style 20, identity 20, composition 15, small-size 10 and engineering 10. Set `decision_required` if alpha extraction damages hair/blindfold/hands/feet, the pose is not sufficiently 3/4, total is below 88, or identity confidence is low. Otherwise set `processed_candidate`. Never set Approved.

- [ ] **Step 2: Refresh reports**

```powershell
.\tools\美术工具\Generate-ArtIntegrationCandidates.ps1
.\tools\美术工具\Generate-ArtQualityBacklog.ps1
.\tools\美术工具\Generate-ArtProgramHandoff.ps1
```

Expected: the new asset remains art-owned and never appears in `program_integrate`.

- [ ] **Step 3: Verify protected state**

Compare Approved and Registry SHA-256 against Task 1. Verify the original source candidate is unchanged.

- [ ] **Step 4: Validate project state**

```powershell
.\tools\docs\Generate-DocsIndex.ps1
.\tools\docs\Validate-Docs.ps1
git diff --check
git status --short
```

Expected: docs pass, protected files unchanged, and only pilot/previously dirty files are present.

- [ ] **Step 5: Handoff**

Report exact state, evidence paths, score, hard failures and next recommendation. If `decision_required`, ask one concrete question and set `resume_from=SELECTION_DECISION`.
