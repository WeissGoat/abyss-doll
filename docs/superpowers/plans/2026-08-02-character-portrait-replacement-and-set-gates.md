---
id: character_portrait_replacement_and_set_gates_plan
title: Character Portrait Replacement and Set Gates Implementation Plan
type: plan
role: 美术
domain: character_portrait_production
status: historical
source_of_truth: false
last_verified: 2026-10-01
update_rule: 历史实施计划，不再更新；现行事实以美术文档与 p3-art-asset-production 为准。
---

# Character Portrait Replacement and Set Gates Implementation Plan

> 历史计划：已于 2026-08-07 实施（`ad7139e`、`6ad5118`）。现行事实以 `.codex/skills/p3-art-asset-production/SKILL.md` 及其 `references/candidate-evaluation.md` 为准。

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make character portrait replacement and Approved admission fail closed on a structured identity contract, six-dimensional evidence, and a current whole-set consistency fingerprint.

**Architecture:** Keep stable character facts in `art_requirements_seed.json`, compile them into the existing Manifest and Request Catalog, and centralize review rules in two focused Python modules. `portrait_review_contract.py` owns the six-dimensional item schema; `portrait_set_gate.py` owns current selected-member snapshots, review evidence, and the single Approved preflight. Existing selection, Approved sync, and Unity registration orchestration call these helpers instead of implementing parallel rules.

**Tech Stack:** Python 3 standard library, Pillow, PowerShell wrappers, `unittest`, existing P3 Manifest/PromptRevision/Approved tooling.

## Global Constraints

- Execute this plan after `2026-08-02-art-catalog-integrity.md`; the formal Manifest and Request Catalog must already be unique and strict-valid.
- Do not change the standard-asset route or its current replacement policy.
- Do not hard-code Zero or any future character identity facts in Python.
- `character_portrait_set` remains generation-method-neutral; this work does not select text-to-image, image-to-image, inpaint, provider, or backend.
- A character replacement always uses `Identity`, `Costume`, `Proportion`, `Framing`, `Technical`, and `TargetFit`.
- `Identity`, `Costume`, `Proportion`, `Framing`, and `Technical` are mandatory protected dimensions; `TargetFit` is the intended change dimension.
- Existing Approved files, `.meta`, GUIDs, Registry entries, and registered state are not rolled back by migration or by a failed retrospective review.
- A missing or stale current set review blocks the next `selected -> Approved` sync for that set.
- Current Zero may remain `registered`, but it must not be described as current-set-consistency-passed or `runtime_validated` until the new set gate passes.
- Keep the user's unrelated `AGENTS.md` worktree change unstaged.

---

### Task 1: Move Character Identity and Presentation Groups into the Requirement Source

**Files:**
- Modify: `美术文档/art_requirements_seed.json`
- Modify: `tools/美术工具/update_art_manifest.py`
- Modify: `tools/美术工具/tests/test_update_art_manifest.py`

**Interfaces:**
- Consumes: the existing preset seed and current Manifest runtime pointer.
- Produces: seed-owned `AssetSets`, entry-owned `PresentationGroup`, and a Manifest snapshot that preserves only `LatestConsistencyReview` as runtime evidence.
- Public helpers:

```python
def validate_seed_asset_sets(asset_sets: Any) -> dict[str, dict[str, Any]]:
    pass

def merge_seed_asset_sets(
    seed_sets: dict[str, dict[str, Any]],
    existing_sets: dict[str, Any],
) -> dict[str, dict[str, Any]]:
    pass
```

- [ ] **Step 1: Add failing tests for AssetSet source ownership and pointer preservation**

Add tests using a temporary seed with one portrait set. The expected merge is:

```python
seed_sets = {
    "demo_portraits": {
        "ProductionProfile": "character_portrait_set",
        "StyleRef": {"Profile": "character_portrait_v1"},
        "IdentitySources": ["character.md"],
        "IdentityContract": {
            "Version": 1,
            "Required": ["silver loose hair"],
            "Forbidden": ["tied hair"],
            "Conditional": ["red glow only when the target state requires it"],
        },
        "ConsistencyRules": ["preserve character identity across presentation groups"],
    }
}
existing_sets = {
    "demo_portraits": {
        "IdentityLocks": ["legacy compiler-owned value"],
        "LatestConsistencyReview": {
            "State": "passed",
            "SetSnapshotFingerprint": "old-fingerprint",
            "ProductionRunID": "old-run",
            "EvidencePath": "old/review.json",
        },
    }
}

merged = merge_seed_asset_sets(seed_sets, existing_sets)
self.assertEqual(merged["demo_portraits"]["IdentityContract"], seed_sets["demo_portraits"]["IdentityContract"])
self.assertNotIn("IdentityLocks", merged["demo_portraits"])
self.assertEqual(
    merged["demo_portraits"]["LatestConsistencyReview"],
    existing_sets["demo_portraits"]["LatestConsistencyReview"],
)
```

Also assert that `add_preset_assets` copies `PresentationGroup` from a seed Entry and that a seed set without all three `IdentityContract` lists fails with `asset_set_identity_contract_invalid:<AssetSetID>`.

- [ ] **Step 2: Run update-manifest tests and verify failure**

```powershell
python -m unittest tools/美术工具/tests/test_update_art_manifest.py -v
```

Expected: new tests FAIL because the seed has no `AssetSets` owner and `PresentationGroup` is not copied.

- [ ] **Step 3: Add the stable Zero AssetSet contract to the seed**

Add top-level `AssetSets.zero_dialogue_portrait_v1`:

```json
{
  "ProductionProfile": "character_portrait_set",
  "StyleRef": {"Profile": "character_portrait_v1"},
  "IdentitySources": [
    "美术文档/人设/03_零号初版人设方案.md",
    "美术文档/人设/05_零号立绘素材设计与交付清单.md"
  ],
  "IdentityContract": {
    "Version": 1,
    "Required": [
      "petite and slender doll-girl proportions without childlike or mature-tall drift",
      "pale silver or gray-white long loose hair, naturally unbound, reaching below the waist",
      "soft worn white cloth blindfold fully covering both eyes",
      "complete gray shawl separate from the inner dress and covering both shoulders, chest, and upper arms",
      "simple pale short inner dress as a separate garment",
      "bare legs and both complete bare feet",
      "no chest ornament, with the core chamber hidden in normal presentation",
      "small restrained doll-joint seams at wrists, knees, and ankles"
    ],
    "Forbidden": [
      "tied hair, ponytail, twintails, braids, buns, short hair, or heavy curls",
      "black, metal, or permanently glowing blindfold, or fully visible normal eyes",
      "one exposed shoulder, shawl merged into a one-piece dress, or untargeted large clothing tears",
      "necklace, chest badge, number plate, large chest ornament, or normally exposed core chamber",
      "shoes, socks, boots, anklets, or incomplete feet",
      "mature-tall proportions, childlike proportions, or erotic posing",
      "large ball joints, heavy mechanical limbs, heavy armor, blood, or gore"
    ],
    "Conditional": [
      "red light may show through the white cloth only when the target state explicitly requires it",
      "minor shawl displacement, joint cracks, and additional wear are allowed only when the target state explicitly requires them",
      "core exposure or a lifted blindfold is allowed only for an explicitly declared maintenance or cut-in state"
    ]
  },
  "ConsistencyRules": [
    "preserve identity, costume structure, proportions, and rendering direction across the complete set",
    "apply framing and rapid-switch checks within each PresentationGroup"
  ]
}
```

Add `PresentationGroup` to the 14 current Entries exactly as follows:

```text
dialogue_standing:
  doll_zero_dialogue_neutral
  doll_zero_dialogue_command_ready
  doll_zero_low_san
  doll_zero_hurt
  doll_zero_trust_soft
  doll_zero_tired
  doll_zero_cold
  doll_zero_depressed
  doll_zero_dialogue_talk_small
  doll_zero_dialogue_confused
  doll_zero_dialogue_thoughtful

front_standing:
  doll_zero_stand_neutral

maintenance_seated:
  doll_zero_maintenance_sit
  doll_zero_repair_relief
```

- [ ] **Step 4: Implement seed AssetSet loading and deterministic merging**

Add:

```python
def validate_seed_asset_sets(asset_sets: Any) -> dict[str, dict[str, Any]]:
    if not isinstance(asset_sets, dict):
        raise ValueError("Preset seed AssetSets must be an object")
    result: dict[str, dict[str, Any]] = {}
    for asset_set_id, value in asset_sets.items():
        if not isinstance(value, dict):
            raise ValueError(f"asset_set_invalid:{asset_set_id}")
        contract = value.get("IdentityContract")
        if not isinstance(contract, dict) or int(contract.get("Version", 0) or 0) < 1:
            raise ValueError(f"asset_set_identity_contract_invalid:{asset_set_id}")
        if not all(isinstance(contract.get(key), list) for key in ("Required", "Forbidden", "Conditional")):
            raise ValueError(f"asset_set_identity_contract_invalid:{asset_set_id}")
        result[str(asset_set_id)] = copy.deepcopy(value)
    return result


def merge_seed_asset_sets(
    seed_sets: dict[str, dict[str, Any]],
    existing_sets: dict[str, Any],
) -> dict[str, dict[str, Any]]:
    result = copy.deepcopy(seed_sets)
    for asset_set_id, asset_set in result.items():
        existing = existing_sets.get(asset_set_id, {}) if isinstance(existing_sets, dict) else {}
        review = existing.get("LatestConsistencyReview") if isinstance(existing, dict) else None
        if isinstance(review, dict):
            asset_set["LatestConsistencyReview"] = copy.deepcopy(review)
    return result
```

Read the seed once in `main`, pass its Entries to the existing preset scanner, set Manifest `AssetSets` from `merge_seed_asset_sets`, and add `PresentationGroup` to `extra_fields`. Do not copy legacy `IdentityLocks` or compiler-generated `Members` from the old Manifest.

- [ ] **Step 5: Run update-manifest tests**

```powershell
python -m unittest tools/美术工具/tests/test_update_art_manifest.py -v
```

Expected: all tests PASS.

- [ ] **Step 6: Commit Task 1**

```powershell
git add -- 美术文档/art_requirements_seed.json tools/美术工具/update_art_manifest.py tools/美术工具/tests/test_update_art_manifest.py
git commit -m "feat: source portrait identity contracts from art requirements"
```

---

### Task 2: Compile IdentityContract Without Character-Specific Python Constants

**Files:**
- Modify: `tools/美术工具/compile_art_generation_requests.py`
- Modify: `tools/美术工具/art_style_catalog.py`
- Modify: `tools/美术工具/art_prompt_compiler.py`
- Modify: `tools/美术工具/tests/test_compile_art_generation_requests.py`
- Modify: `tools/美术工具/tests/test_art_style_catalog.py`
- Modify: `tools/美术工具/tests/test_art_prompt_compiler.py`

**Interfaces:**
- Consumes: Task 1 `AssetSets.<ID>.IdentityContract` and Entry `PresentationGroup`.
- Produces: method-neutral hard constraints with stable IDs and a member snapshot containing `PresentationGroup`.

- [ ] **Step 1: Replace legacy-lock tests with IdentityContract tests**

Use this fixture:

```python
asset_sets = {
    "demo_portraits": {
        "ProductionProfile": "character_portrait_set",
        "StyleRef": {"Profile": "character_portrait_v1"},
        "IdentitySources": ["character.md"],
        "IdentityContract": {
            "Version": 1,
            "Required": ["silver loose hair", "white cloth blindfold"],
            "Forbidden": ["tied hair", "black blindfold"],
            "Conditional": ["red glow only when required by the target state"],
        },
        "ConsistencyRules": ["preserve identity"],
    }
}
```

Build a portrait Entry with `AssetSetID="demo_portraits"`, `ProductionProfile="character_portrait_set"`, and `PresentationGroup="dialogue_standing"`, then obtain the values used below with:

```python
resolved = resolve_entry_layers(entry, catalog, asset_sets)
brief = build_canonical_visual_brief(entry, catalog, asset_sets)
request = compile_requirement_request(entry, catalog, asset_sets)
```

Assert:

```python
self.assertEqual(resolved["IdentityContract"], asset_sets["demo_portraits"]["IdentityContract"])
self.assertEqual(resolved["PresentationGroup"], "dialogue_standing")
self.assertIn("silver loose hair", brief["Preserve"])
self.assertIn("tied hair", brief["Forbidden"])
self.assertIn("red glow only when required by the target state", brief["Conditional"])
self.assertEqual(
    [item["ID"] for item in request["PromptAuthoringContext"]["HardConstraints"]["Conditional"]],
    ["brief:conditional:0"],
)
```

Also assert that a portrait AssetSet missing a valid `IdentityContract` compiles to `RequirementStatus=invalid` with `asset_set_identity_contract_invalid`.

- [ ] **Step 2: Run compiler tests and verify failure**

```powershell
python -m unittest tools/美术工具/tests/test_compile_art_generation_requests.py tools/美术工具/tests/test_art_style_catalog.py tools/美术工具/tests/test_art_prompt_compiler.py -v
```

Expected: tests FAIL because `IdentityLocks` is still hard-coded and `Conditional` is absent.

- [ ] **Step 3: Remove Zero-specific compiler defaults and validate source contracts**

Delete `ZERO_IDENTITY_LOCKS`. In `_asset_sets`, copy only the seed-owned set and attach deterministic Members:

```python
member = {
    "AssetID": entry.get("AssetID", ""),
    "VisualID": entry.get("VisualID", ""),
    "SetRole": entry.get("SetRole", ""),
    "PresentationGroup": entry.get("PresentationGroup", ""),
    "SourceAssets": copy.deepcopy(entry.get("SourceAssets", [])),
}
```

For each `character_portrait_set`, require `ProductionProfile`, `IdentitySources`, a valid `IdentityContract`, non-empty Members, and non-empty `PresentationGroup` on every member. Do not synthesize character facts in Python.

- [ ] **Step 4: Resolve and compile all three IdentityContract lists**

In `resolve_entry_layers`, expose `IdentityContract`, `IdentitySources`, `ConsistencyRules`, `PresentationGroup`, `SetRole`, and `SourceAssets`.

In `build_canonical_visual_brief`, use:

```python
contract = resolved.get("IdentityContract", {})
brief["Preserve"] = _dedupe(_list(intent.get("Preserve")) + _list(contract.get("Required")))
brief["Forbidden"] = _dedupe(_list(intent.get("ForbiddenElements")) + _list(contract.get("Forbidden")))
brief["Conditional"] = _dedupe(_list(contract.get("Conditional")))
brief["PresentationGroup"] = resolved.get("PresentationGroup", "")
```

Add `HardConstraints.Conditional` through `_context_items(brief.get("Conditional"), "conditional", source="AssetSet.IdentityContract.Conditional")`. Keep Prompt authoring Agent-owned; these fields are constraints and evidence, not executable Prompt text.

- [ ] **Step 5: Run focused compiler tests**

```powershell
python -m unittest tools/美术工具/tests/test_compile_art_generation_requests.py tools/美术工具/tests/test_art_style_catalog.py tools/美术工具/tests/test_art_prompt_compiler.py tools/美术工具/tests/test_art_prompt_revision.py -v
```

Expected: all tests PASS; PromptRevision validation requires mappings for the new `brief:conditional:*` IDs.

- [ ] **Step 6: Commit Task 2**

```powershell
git add -- tools/美术工具/compile_art_generation_requests.py tools/美术工具/art_style_catalog.py tools/美术工具/art_prompt_compiler.py tools/美术工具/tests/test_compile_art_generation_requests.py tools/美术工具/tests/test_art_style_catalog.py tools/美术工具/tests/test_art_prompt_compiler.py
git commit -m "feat: compile portrait identity contracts"
```

---

### Task 3: Add the Shared Six-Dimensional Portrait Review Contract

**Files:**
- Create: `tools/美术工具/portrait_review_contract.py`
- Create: `tools/美术工具/tests/test_portrait_review_contract.py`

**Interfaces:**
- Produces: `PORTRAIT_DIMENSIONS`, `DEFAULT_PROTECTED_DIMENSIONS`, `normalize_portrait_scores`, `validate_dimension_evidence`, and `validate_portrait_review_item`.
- Consumed by: Tasks 4 and 5.

- [ ] **Step 1: Write failing contract tests**

Create a valid item helper:

```python
def valid_item() -> dict:
    scores = {
        "Identity": 94,
        "Costume": 92,
        "Proportion": 91,
        "Framing": 90,
        "Technical": 94,
        "TargetFit": 95,
    }
    scores["Total"] = round(sum(scores.values()) / 6)
    return {
        "VisualID": "doll_demo_hurt",
        "ReviewRubricVersion": "character_portrait_v3",
        "Scores": scores,
        "DimensionEvidence": {
            name: {
                "Finding": f"independent finding for {name}",
                "Evidence": [f"IdentityContract or image observation for {name}"],
            }
            for name in (
                "Identity", "Costume", "Proportion", "Framing", "Technical", "TargetFit"
            )
        },
    }
```

Tests must assert:

```python
self.assertEqual(validate_portrait_review_item(valid_item())["Scores"]["Total"], 93)

missing = valid_item()
missing["Scores"].pop("Costume")
with self.assertRaisesRegex(ValueError, "portrait_score_missing:Costume"):
    validate_portrait_review_item(missing)

duplicated = valid_item()
for evidence in duplicated["DimensionEvidence"].values():
    evidence["Finding"] = "same copied sentence"
with self.assertRaisesRegex(ValueError, "portrait_dimension_evidence_not_independent"):
    validate_portrait_review_item(duplicated)
```

Also cover score range, deterministic Total mismatch, missing evidence, and `TargetFit < 88` when `require_selection_threshold=True`.

- [ ] **Step 2: Run the new test file and verify failure**

```powershell
python -m unittest tools/美术工具/tests/test_portrait_review_contract.py -v
```

Expected: FAIL because the module does not exist.

- [ ] **Step 3: Implement the focused contract**

Use these stable constants:

```python
PORTRAIT_DIMENSIONS = (
    "Identity",
    "Costume",
    "Proportion",
    "Framing",
    "Technical",
    "TargetFit",
)
DEFAULT_PROTECTED_DIMENSIONS = PORTRAIT_DIMENSIONS[:5]
PORTRAIT_RUBRIC_VERSION = "character_portrait_v3"
```

`normalize_portrait_scores` must require integer values `0..100`, require all six dimensions, and require `Total == round(sum(dimensions) / 6)`. `validate_dimension_evidence` must require one object per dimension, a non-empty `Finding`, and a non-empty `Evidence` list; reject when all normalized Findings are identical. `validate_portrait_review_item` must require the rubric version and optionally enforce `TargetFit >= 88` and `Total >= 88`.

- [ ] **Step 4: Run contract tests**

```powershell
python -m unittest tools/美术工具/tests/test_portrait_review_contract.py -v
```

Expected: all tests PASS.

- [ ] **Step 5: Commit Task 3**

```powershell
git add -- tools/美术工具/portrait_review_contract.py tools/美术工具/tests/test_portrait_review_contract.py
git commit -m "feat: add portrait review contract"
```

---

### Task 4: Enforce the Contract During Same-VisualID Selection

**Files:**
- Modify: `tools/美术工具/select_art_candidate.py`
- Modify: `tools/美术工具/tests/test_select_art_candidate.py`

**Interfaces:**
- Consumes: Task 3 review validation and the Manifest Entry `ProductionProfile`.
- Produces: guarded character replacement, immutable previous-selected bytes, and evidence usable by the set Prepare stage.

- [ ] **Step 1: Add failing portrait replacement tests**

Add a portrait fixture with `ProductionProfile=character_portrait_set`. Cover these exact cases:

```python
with self.assertRaisesRegex(ValueError, "replacement_baseline_review_required"):
    self.select_portrait_replacement(baseline_scores={"Total": 90, "Identity": 90})

with self.assertRaisesRegex(ValueError, "replacement_protected_dimensions_required"):
    self.select_portrait_replacement(protected_dimensions=[])

result = self.select_portrait_replacement(
    baseline_scores=complete_scores(total=90),
    candidate_scores=complete_scores(total=92),
    protected_dimensions=list(DEFAULT_PROTECTED_DIMENSIONS),
)
self.assertEqual(result["PolicyResult"]["ProtectedDimensions"], list(DEFAULT_PROTECTED_DIMENSIONS))
self.assertTrue(Path(result["PreviousSelected"]["EvidencePath"]).is_file())
```

Keep the existing standard-asset replacement test unchanged to prove that standard assets do not inherit portrait-only schema requirements.

Add a fixture method `run_portrait_selection(baseline_scores, candidate_scores=None, protected_dimensions=None)` that writes the temporary Manifest, latest processed candidate, and review JSON, invokes `select_art_candidate` with the fixture paths, and returns its decision. Define `complete_scores(total)` in the test module as six dimensions all set to `total` plus `Total=total`; use independent `DimensionEvidence` findings for each dimension.

- [ ] **Step 2: Run selection tests and verify failure**

```powershell
python -m unittest tools/美术工具/tests/test_select_art_candidate.py -v
```

Expected: new portrait tests FAIL.

- [ ] **Step 3: Validate character candidate and baseline evidence**

Before `validate_replacement`, detect:

```python
is_portrait = entry.get("ProductionProfile") == "character_portrait_set"
```

For portraits:

- validate the candidate item with `validate_portrait_review_item(item, require_selection_threshold=True)` after loading the single `item` matching the target VisualID from the review's `Items` list;
- require `ReplacementBaseline.Scores` and `ReplacementBaseline.DimensionEvidence` to satisfy the same six-dimensional schema;
- raise `replacement_baseline_review_required` for legacy or incomplete baselines;
- require every `DEFAULT_PROTECTED_DIMENSIONS` item in `ReplacementPolicy.ProtectedDimensions`;
- reject an empty or partial list with `replacement_protected_dimensions_required`;
- compare the five protected scores and keep the existing total/delta checks.

Do not require this schema for `standard_asset`.

- [ ] **Step 4: Preserve the actual previous selected file before overwrite**

Add:

```python
def preserve_previous_selected(
    target: Path,
    review_path: Path,
    visual_id: str,
    target_hash: str,
) -> Path:
    destination = review_path.parent / "replacement-baselines" / visual_id / f"{target_hash}{target.suffix.lower()}"
    if destination.exists() and hashlib.sha256(destination.read_bytes()).hexdigest() != target_hash:
        raise ValueError("replacement_baseline_evidence_hash_mismatch")
    if not destination.exists():
        atomic_copy(target, destination)
    return destination
```

Call it only after all review checks pass and before copying the new candidate. Store `PreviousSelected.EvidencePath`, `Scores`, and `DimensionEvidence` in both `production_decision.json` and `selection-decision.json`.

- [ ] **Step 5: Run selection tests**

```powershell
python -m unittest tools/美术工具/tests/test_select_art_candidate.py -v
```

Expected: all tests PASS.

- [ ] **Step 6: Commit Task 4**

```powershell
git add -- tools/美术工具/select_art_candidate.py tools/美术工具/tests/test_select_art_candidate.py
git commit -m "fix: guard character portrait replacements"
```

---

### Task 5: Add Current-Set Snapshot Prepare and Finalize Gates

**Files:**
- Create: `tools/美术工具/portrait_set_gate.py`
- Create: `tools/美术工具/Invoke-PortraitSetGate.ps1`
- Create: `tools/美术工具/tests/test_portrait_set_gate.py`

**Interfaces:**
- Consumes: Manifest AssetSet snapshot, current selected files, Task 3 review contract, and optional previous-selected evidence from Task 4.
- Produces: `SetSnapshotFingerprint`, contact evidence, a validated `LatestConsistencyReview`, and `require_current_set_review` for downstream preflight.
- Public signatures:

```python
def build_set_snapshot(
    manifest: dict[str, Any],
    asset_set_id: str,
    incoming_root: Path,
    project_root: Path,
) -> dict[str, Any]:
    pass

def prepare_set_review(
    *,
    manifest_path: Path,
    incoming_root: Path,
    evidence_root: Path,
    asset_set_id: str,
    production_run_id: str,
    baseline_map_path: Path | None = None,
) -> dict[str, Any]:
    pass

def finalize_set_review(
    *,
    manifest_path: Path,
    incoming_root: Path,
    asset_set_id: str,
    production_run_id: str,
    review_path: Path,
) -> dict[str, Any]:
    pass

def require_current_set_review(
    *,
    manifest: dict[str, Any],
    asset_set_id: str,
    incoming_root: Path,
    project_root: Path,
) -> dict[str, Any]:
    pass
```

- [ ] **Step 1: Write failing snapshot and finalize tests**

Build a temporary three-member set with two members in `dialogue_standing` and one in `maintenance_seated`. Assert:

```python
snapshot = build_set_snapshot(manifest, "demo_portraits", incoming_root, project_root)
self.assertEqual([item["VisualID"] for item in snapshot["Members"]], ["portrait_a", "portrait_b", "portrait_sit"])
self.assertEqual(len(snapshot["SetSnapshotFingerprint"]), 64)

prepared = prepare_set_review(
    manifest_path=self.manifest_path,
    incoming_root=self.incoming_root,
    evidence_root=self.evidence_root,
    asset_set_id="demo_portraits",
    production_run_id="portrait_set_test_01",
)
self.assertTrue(Path(prepared["Evidence"]["ContactSheet"]).is_file())
self.assertTrue(Path(prepared["Evidence"]["SmallSizeStrip"]).is_file())

review = self.valid_set_review(prepared)
review_path = self.evidence_root / "portrait_set_test_01" / "portrait-set-gate" / "consistency-review.json"
review_path.write_text(json.dumps(review, ensure_ascii=False), encoding="utf-8")
finalized = finalize_set_review(
    manifest_path=self.manifest_path,
    incoming_root=self.incoming_root,
    asset_set_id="demo_portraits",
    production_run_id="portrait_set_test_01",
    review_path=review_path,
)
self.assertEqual(finalized["State"], "passed")
self.assertEqual(
        json.loads(self.manifest_path.read_text(encoding="utf-8"))["AssetSets"]["demo_portraits"]["LatestConsistencyReview"]["SetSnapshotFingerprint"],
    prepared["SetSnapshotFingerprint"],
)
```

Also test:

- modifying one selected image makes the old review fail with `portrait_set_review_stale`;
- omitting one member fails with `portrait_set_member_selected_missing:<VisualID>`;
- missing a member review, group check, SHA, or non-passed state fails Finalize;
- a different `IdentityContract` or `PresentationGroup` changes `SetSnapshotFingerprint`;
- `require_current_set_review` returns success only for the exact current fingerprint.

Define `self.valid_set_review(prepared)` in the test fixture rather than using an undefined helper. It must copy `prepared["Members"]`, set every member `Status` to `passed`, create the seven named checks for each distinct `PresentationGroup`, create the four named `CrossGroupChecks`, set `State` to `passed`, and use `prepared["SetSnapshotFingerprint"]` verbatim.

- [ ] **Step 2: Run the new test file and verify failure**

```powershell
python -m unittest tools/美术工具/tests/test_portrait_set_gate.py -v
```

Expected: FAIL because the gate module does not exist.

- [ ] **Step 3: Implement deterministic set snapshots**

The fingerprint payload must be exactly:

```python
payload = {
    "AssetSetID": asset_set_id,
    "IdentityContract": copy.deepcopy(asset_set["IdentityContract"]),
    "Members": [
        {
            "VisualID": item["VisualID"],
            "SelectedSHA256": item["SelectedSHA256"],
            "PresentationGroup": item["PresentationGroup"],
        }
        for item in sorted(members, key=lambda value: value["VisualID"])
    ],
}
fingerprint = hashlib.sha256(canonical_json(payload).encode("utf-8")).hexdigest()
```

Resolve one selected file per member using Manifest `SelectedPath` first, then the existing safe selected resolver. Do not fall back to an older failed or ambiguous processed round.

- [ ] **Step 4: Implement Prepare evidence**

`prepare_set_review` writes under `UnityClient/Logs/P3ArtProduction/<ProductionRunID>/portrait-set-gate/`:

```text
prepare.json
set-contact-sheet.png
small-size-strip.png
replacement-comparisons.png   # only when previous-selected evidence exists
```

`prepare.json` stores the complete member snapshot, `SetSnapshotFingerprint`, `IdentityContract`, presentation groups, evidence paths, and `State=review_required`. Use Pillow with fixed cell sizes and labels; image generation is deterministic and must not alter source files.

Discover replacement baselines from the current member workspace `production_decision.json -> PreviousSelected.EvidencePath`. Also accept an optional CLI `--baseline-map` JSON for retrospective migration evidence.

- [ ] **Step 5: Implement Finalize review validation**

Require this review shape:

```json
{
  "Schema": "p3-portrait-set-review@1",
  "ProductionRunID": "run-id",
  "AssetSetID": "demo_portraits",
  "SetSnapshotFingerprint": "sha256",
  "State": "passed",
  "Members": [
    {
      "VisualID": "portrait_a",
      "SelectedSHA256": "sha256",
      "PresentationGroup": "dialogue_standing",
      "Status": "passed",
      "Finding": "member-specific conclusion"
    }
  ],
  "Groups": {
    "dialogue_standing": {
      "Checks": {
        "Identity": {"Status": "passed", "Finding": "all members preserve the IdentityContract required hair, blindfold, and body anchors"},
        "Costume": {"Status": "passed", "Finding": "the gray shawl remains separate from the inner dress and normal core exposure is absent"},
        "Proportion": {"Status": "passed", "Finding": "dialogue members retain the same head-to-body proportion and seated members form a separate declared family"},
        "Framing": {"Status": "passed", "Finding": "same-group members share head position, bottom anchor, occupied height, and horizontal center"},
        "Technical": {"Status": "passed", "Finding": "all selected files match the Manifest dimensions, alpha, format, and clean-edge requirements"},
        "TargetFit": {"Status": "passed", "Finding": "each member's state and pose remain readable at the target small-size strip"},
        "RapidSwitch": {"Status": "passed", "Finding": "adjacent dialogue states switch without an unexplained baseline or scale jump"}
      }
    }
  },
  "CrossGroupChecks": {
    "Identity": {"Status": "passed", "Finding": "all presentation groups still depict the same character identity"},
    "Costume": {"Status": "passed", "Finding": "costume structure and conditional exposure rules remain compatible across groups"},
    "Proportion": {"Status": "passed", "Finding": "declared standing and seated families remain proportionally coherent"},
    "RenderingDirection": {"Status": "passed", "Finding": "linework, value structure, and rendering direction remain one set even where framing differs"}
  },
  "FailedMembers": []
}
```

Allow check status `passed` or `passed_with_risk`, but require top-level `State=passed`, no failed members, exact current member SHA/group coverage, all seven checks for every present group, and all four cross-group checks. On success, atomically update only:

```python
manifest["AssetSets"][asset_set_id]["LatestConsistencyReview"] = {
    "State": "passed",
    "SetSnapshotFingerprint": current["SetSnapshotFingerprint"],
    "ProductionRunID": production_run_id,
    "EvidencePath": repo_path(review_path, project_root),
}
```

- [ ] **Step 6: Add PowerShell wrapper and run tests**

Expose `Prepare`, `Finalize`, and `Check` phases. The wrapper must only serialize parameters to the Python CLI.

```powershell
python -m unittest tools/美术工具/tests/test_portrait_set_gate.py -v
```

Expected: all tests PASS.

- [ ] **Step 7: Commit Task 5**

```powershell
git add -- tools/美术工具/portrait_set_gate.py tools/美术工具/Invoke-PortraitSetGate.ps1 tools/美术工具/tests/test_portrait_set_gate.py
git commit -m "feat: add portrait set consistency gate"
```

---

### Task 6: Block Approved Sync and Surface the Blocker in Unity Registration Planning

**Files:**
- Modify: `tools/美术工具/sync_approved_art.py`
- Modify: `tools/美术工具/art_approved_unity_registration.py`
- Modify: `tools/美术工具/tests/test_sync_approved_art.py`
- Modify: `tools/美术工具/tests/test_art_approved_unity_registration.py`

**Interfaces:**
- Consumes: Task 5 `require_current_set_review`.
- Produces: one shared fail-closed decision for direct Approved sync and MCP-driven Approved/Unity/Registry planning.
- Refactor signature:

```python
def build_sync_plan(
    manifest: dict[str, Any],
    in_root: Path,
    args: argparse.Namespace,
) -> list[tuple[dict[str, Any], Path, Path, str, bytes | None]]:
    pass
```

- [ ] **Step 1: Add failing downstream gate tests**

For `sync_approved_art.py`, refactor the plan-building loop into the callable `build_sync_plan`. Assert:

```python
with self.assertRaisesRegex(ValueError, "portrait_set_review_missing:demo_portraits"):
    build_sync_plan(manifest_without_review, self.in_root, self.sync_args())

plan = build_sync_plan(standard_asset_manifest, self.in_root, self.sync_args())
self.assertEqual(len(plan), 1)
```

For `art_approved_unity_registration.create_plan`, assert a stale character set produces:

```python
self.assertEqual(plan["blocking_errors"][0]["code"], "blocked:portrait_set_review_stale")
self.assertIn("demo_portraits", plan["blocking_errors"][0]["details"])
```

and a standard asset remains unblocked.

- [ ] **Step 2: Run downstream tests and verify failure**

```powershell
python -m unittest tools/美术工具/tests/test_sync_approved_art.py tools/美术工具/tests/test_art_approved_unity_registration.py -v
```

Expected: new tests FAIL.

- [ ] **Step 3: Call the shared gate before any Approved mutation**

In direct sync, call `require_current_set_review` for every selected Entry whose `ProductionProfile=character_portrait_set` before printing or copying any item. Report exact errors:

```text
portrait_set_review_missing:<AssetSetID>
portrait_set_review_stale:<AssetSetID>
portrait_set_member_selected_missing:<VisualID>
```

In `create_plan`, call the same helper and append one blocking error per affected set, deduplicated by `AssetSetID`. Keep plan creation read-only and retain all current `.meta`, importer, collision, and authorization checks.

- [ ] **Step 4: Run downstream tests**

```powershell
python -m unittest tools/美术工具/tests/test_sync_approved_art.py tools/美术工具/tests/test_art_approved_unity_registration.py -v
```

Expected: all tests PASS.

- [ ] **Step 5: Commit Task 6**

```powershell
git add -- tools/美术工具/sync_approved_art.py tools/美术工具/art_approved_unity_registration.py tools/美术工具/tests/test_sync_approved_art.py tools/美术工具/tests/test_art_approved_unity_registration.py
git commit -m "fix: require current portrait set review before approved sync"
```

---

### Task 7: Migrate Zero, Re-review the Current 14-Member Set, and Update Workflow Facts

**Files:**
- Generated by scripts: `美术文档/_generated/art_manifest.json`
- Generated by scripts: `美术文档/_generated/art_generation_requests.json`
- Generated by scripts: `美术文档/_generated/art_generation_request_migration.json`
- Generated by scripts: `美术文档/_generated/视觉资产Manifest.md`
- Create as local evidence (ignored by Git, do not stage): `UnityClient/Logs/P3ArtProduction/zero_portrait_set_regate_20260802_01/portrait-set-gate/*`
- Modify: `.codex/skills/p3-art-asset-production/SKILL.md`
- Modify: `.codex/skills/p3-art-asset-production/references/candidate-evaluation.md`
- Modify: `.codex/skills/p3-art-asset-production/references/approved-and-unity.md`
- Modify: `tools/美术工具/README.md`
- Modify: `美术文档/00_美术流水线总览.md`
- Modify: `美术文档/01_Manifest规范.md`
- Modify: `美术文档/03_AI生成与筛选规范.md`
- Modify: `agent_status/art.md`
- Regenerate: `DOCS_INDEX.md`
- Regenerate: `docs_index.json`

**Interfaces:**
- Consumes: Tasks 1-6 and the current selected portrait files.
- Produces: migrated facts, a current evidence-backed Zero set result, and updated Agent operating instructions.

- [ ] **Step 1: Run the complete focused test slice**

```powershell
python -m unittest `
  tools/美术工具/tests/test_update_art_manifest.py `
  tools/美术工具/tests/test_compile_art_generation_requests.py `
  tools/美术工具/tests/test_art_style_catalog.py `
  tools/美术工具/tests/test_art_prompt_compiler.py `
  tools/美术工具/tests/test_art_prompt_revision.py `
  tools/美术工具/tests/test_portrait_review_contract.py `
  tools/美术工具/tests/test_select_art_candidate.py `
  tools/美术工具/tests/test_portrait_set_gate.py `
  tools/美术工具/tests/test_sync_approved_art.py `
  tools/美术工具/tests/test_art_approved_unity_registration.py -v
```

Expected: all tests PASS.

- [ ] **Step 2: Regenerate Manifest and Request Catalog**

```powershell
.\tools\美术工具\Update-ArtManifest.ps1
.\tools\美术工具\Compile-ArtGenerationRequests.ps1 -Overwrite
python tools/美术工具/validate_art_generation_requests.py `
  --manifest 美术文档/_generated/art_manifest.json `
  --request-catalog 美术文档/_generated/art_generation_requests.json `
  --strict
```

Expected after the preceding Catalog plan:

- 308 Manifest Entries and 308 unique Requests remain.
- Zero AssetSet has `IdentityContract`, 14 Members, and all three PresentationGroups.
- The three old Zero PromptRevisions become stale because the hard identity contract changed; `PromptReady=0` and `PromptAuthoringRequired=308` is expected until a future generation run authors new immutable revisions.
- Strict validation passes; stale Prompt authoring status is not a Catalog integrity failure.

- [ ] **Step 3: Prepare retrospective baseline evidence for the three August replacements**

Create `baseline-map.json` under the new run directory mapping:

```json
{
  "doll_zero_dialogue_command_ready": "UnityClient/Assets/Art/_IncomingAI/character_portraits/doll_zero_dialogue_command_ready/processed/1/001.png",
  "doll_zero_hurt": "UnityClient/Assets/Art/_IncomingAI/character_portraits/doll_zero_hurt/processed/1/001.png",
  "doll_zero_tired": "UnityClient/Assets/Art/_IncomingAI/character_portraits/doll_zero_tired/processed/1/001.png"
}
```

Verify the three files still match the recorded old selected SHA values:

```text
command_ready: a0336a401527c538462168da5ed69fe52e9ba4ccb2c3348d08e2227a17eecbe4
hurt:          f062e6db0a9494acc08be367148663a432602e0fff2d38800139e5b103fc1017
tired:         a954bb0bd968b0e9f7cdbdccf3173c49c837aa8ac0f741093f589fe125ef253f
```

If any hash differs, stop with `validation_limited:replacement_baseline_bytes_missing`; do not invent a baseline score.

- [ ] **Step 4: Prepare current 14-member evidence**

```powershell
.\tools\美术工具\Invoke-PortraitSetGate.ps1 `
  -Phase Prepare `
  -AssetSetID zero_dialogue_portrait_v1 `
  -ProductionRunID zero_portrait_set_regate_20260802_01 `
  -BaselineMap UnityClient/Logs/P3ArtProduction/zero_portrait_set_regate_20260802_01/baseline-map.json
```

Expected: current contact sheet, small-size strip, old/new replacement comparison, and `prepare.json` cover all 14 current selected SHA values.

- [ ] **Step 5: Author evidence-backed reviews without forcing a pass**

Using the current images and active character facts, write six-dimensional candidate/baseline review files for command-ready, hurt, and tired with distinct `DimensionEvidence`. Write `consistency-review.json` using the exact current fingerprint and all group/cross-group checks.

Decision rule:

- if all current members meet the identity contract and group checks, set top-level `State=passed` and Finalize;
- if `doll_zero_hurt` still has the observed large shawl/skirt tearing or rapid-switch framing jump, set it to failed, keep top-level `State=failed`, do not Finalize, and record `blocked:portrait_member_repair_required:doll_zero_hurt` in `agent_status/art.md`;
- do not downgrade Manifest `Status`, delete Approved, alter `.meta`, or edit Registry as part of this retrospective review.

- [ ] **Step 6: Finalize only a genuinely passed current snapshot**

When and only when the review is passed:

```powershell
.\tools\美术工具\Invoke-PortraitSetGate.ps1 `
  -Phase Finalize `
  -AssetSetID zero_dialogue_portrait_v1 `
  -ProductionRunID zero_portrait_set_regate_20260802_01 `
  -ReviewPath UnityClient/Logs/P3ArtProduction/zero_portrait_set_regate_20260802_01/portrait-set-gate/consistency-review.json
```

If the review fails, instead prove the gate is active:

```powershell
.\tools\美术工具\Invoke-ArtApprovedUnityRegistration.ps1 `
  -Phase Plan `
  -ArtImportRunID zero_portrait_set_gate_probe_20260802_01 `
  -VisualID doll_zero_hurt
```

Expected: the Plan contains `blocked:portrait_set_review_missing` or `blocked:portrait_set_review_stale`; no Approved file changes.

- [ ] **Step 7: Update the Skill and operating documentation**

Use `superpowers:writing-skills` while editing the production Skill. Record:

- portrait replacement reviews use `character_portrait_v3` and all six dimensions;
- legacy/incomplete baselines stop at `replacement_baseline_review_required`;
- previous selected bytes are preserved before overwrite;
- a current `SetSnapshotFingerprint` review is mandatory before Approved sync;
- Prepare/Finalize commands and evidence paths;
- standard assets are unchanged;
- `registered` is retained when a retrospective set review fails, while `runtime_validated` remains unclaimed.

Change the `2026-08-02 状态` notes in `01` and `03` from design-only to the actual verified implementation result. Update `agent_status/art.md` with either the passed fingerprint/evidence path or the exact member repair blocker.

- [ ] **Step 8: Regenerate docs, run strict checks, and commit migration**

```powershell
.\tools\docs\Generate-DocsIndex.ps1
.\tools\docs\Validate-Docs.ps1
.\tools\agent\Invoke-AgentHealthCheck.ps1 -Strict
git diff --check
git status --short
```

Stage only task files and generated outputs. Do not stage `AGENTS.md`.

```powershell
git add -- .codex/skills/p3-art-asset-production tools/美术工具/README.md 美术文档/00_美术流水线总览.md 美术文档/01_Manifest规范.md 美术文档/03_AI生成与筛选规范.md 美术文档/_generated agent_status/art.md DOCS_INDEX.md docs_index.json
git commit -m "feat: enforce portrait replacement and set gates"
```

Expected: strict checks pass. The commit may truthfully report either a current passed Zero fingerprint or an evidence-backed `doll_zero_hurt` repair blocker; it must not convert a failed review into a pass.

---

## Plan Self-Review

- Spec coverage: source-owned IdentityContract, no compiler character constants, PresentationGroup, six dimensions, default five protected dimensions, independent evidence, legacy baseline stop, previous-selected bytes, current set fingerprint, Prepare/Finalize, Approved fail-closed, Unity Plan preflight, current Zero re-review, and no rollback all have owning tasks.
- Maintainability: item-review rules live only in `portrait_review_contract.py`; set snapshot and admission rules live only in `portrait_set_gate.py`; downstream scripts call these modules.
- Scope: no image-generation method, runtime UGUI binding, gameplay logic, Registry schema, or standard-asset behavior changes are included.
- Type consistency: `IdentityContract`, `PresentationGroup`, `DimensionEvidence`, `SetSnapshotFingerprint`, `LatestConsistencyReview`, `character_portrait_v3`, and the three set-gate phases are named consistently throughout.
- Failure honesty: the plan permits implementation completion with a verified current-member repair blocker; it never requires passing the existing hurt image.
