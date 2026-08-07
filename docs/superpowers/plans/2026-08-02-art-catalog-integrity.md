# Art Catalog Integrity Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Restore a strict-valid formal art Request Catalog and make duplicate VisualIDs, stale Summary values, and partial-catalog overwrites fail closed.

**Architecture:** Add one shared `art_catalog_integrity.py` module used by Manifest discovery, request compilation, PromptRevision publication, and strict validation. Config entries that explicitly share the same `IconID` are canonicalized into one Manifest Entry with multiple `RequirementSources`; every other duplicate remains an error. Catalog Summary is always recomputed from the final complete Requests list.

**Tech Stack:** Python 3 standard library, PowerShell wrappers, `unittest`, existing P3 JSON schemas and atomic JSON writers.

## Global Constraints

- `配置表(JSON)` remains the gameplay configuration source; do not hand-edit `UnityClient/Assets/StreamingAssets/Configs`.
- Do not hand-edit `美术文档/_generated`; refresh it through owning scripts.
- One formal `VisualID` produces one Manifest Entry, one OutputPath, and one Request.
- A shared visual is allowed only when every source config explicitly names the same VisualID field; equal Prompt text is never reuse evidence.
- Preserve existing lifecycle, selected, Approved, `.meta`, GUID, Registry, and PromptRevision evidence whenever its semantic fingerprint remains valid.
- A scoped `--visual-id` compile must merge into the full catalog and must never delete unrelated Requests.
- Validate all in-memory outputs before replacing any generated file.
- Keep the user's unrelated `AGENTS.md` worktree change unstaged.

---

### Task 1: Shared Catalog Integrity Primitives

**Files:**
- Create: `tools/美术工具/art_catalog_integrity.py`
- Create: `tools/美术工具/tests/test_art_catalog_integrity.py`

**Interfaces:**
- Consumes: Manifest Entry dictionaries and Request dictionaries.
- Produces: `CatalogIntegrityError`, `canonicalize_manifest_entries`, `validate_manifest_uniqueness`, `recompute_catalog_summary`, and `merge_request_catalog`.

- [ ] **Step 1: Write failing tests for explicit reuse, conflicting duplicates, Summary, and scoped merge**

Create fixtures and tests with these exact expectations:

```python
import unittest

from art_catalog_integrity import (
    CatalogIntegrityError,
    canonicalize_manifest_entries,
    merge_request_catalog,
    recompute_catalog_summary,
)


def source(config_id: str, visual_id: str, *, explicit: bool = True) -> dict:
    return {
        "Domain": "item",
        "SourceType": "config",
        "ConfigID": config_id,
        "ConfigSource": f"配置表(JSON)/Items/{config_id}.json",
        "DisplayName": config_id,
        "AssetType": "icon",
        "VisualID": visual_id,
        "OutputPath": f"UnityClient/Assets/Art/Approved/Items/Icons/{visual_id}.png",
        "ProductionProfile": "standard_asset",
        "RequirementSources": [
            {
                "SourceType": "config",
                "ConfigID": config_id,
                "ConfigSource": f"配置表(JSON)/Items/{config_id}.json",
                "DisplayName": config_id,
                "VisualIDField": "IconID" if explicit else "derived",
                "ExplicitVisualID": explicit,
            }
        ],
    }


class ArtCatalogIntegrityTests(unittest.TestCase):
    def test_explicit_shared_icon_becomes_one_entry(self) -> None:
        entries = canonicalize_manifest_entries([
            source("gear_iron_armor", "item_gear_iron_armor_icon"),
            source("gear_cracked_iron_armor", "item_gear_iron_armor_icon"),
        ])
        self.assertEqual(len(entries), 1)
        self.assertEqual(entries[0]["ConfigID"], "gear_iron_armor")
        self.assertEqual(
            [item["ConfigID"] for item in entries[0]["RequirementSources"]],
            ["gear_iron_armor", "gear_cracked_iron_armor"],
        )
        self.assertEqual(entries[0]["VisualReusePolicy"]["Mode"], "shared_visual")

    def test_derived_duplicate_visual_id_fails(self) -> None:
        with self.assertRaisesRegex(
            CatalogIntegrityError,
            "visual_id_collision_conflict:item_demo_icon",
        ):
            canonicalize_manifest_entries([
                source("item_a", "item_demo_icon", explicit=False),
                source("item_b", "item_demo_icon", explicit=False),
            ])

    def test_summary_is_derived_from_requests(self) -> None:
        requests = [
            {"RequirementStatus": "ready", "PromptAuthoringStatus": "prompt_ready"},
            {"RequirementStatus": "ready", "PromptAuthoringStatus": "prompt_authoring_required"},
            {"RequirementStatus": "invalid", "PromptAuthoringStatus": "prompt_invalid"},
        ]
        self.assertEqual(
            recompute_catalog_summary(requests),
            {
                "Ready": 2,
                "StyleResolutionRequired": 0,
                "Unsupported": 0,
                "Invalid": 1,
                "UnchangedPublished": 0,
                "PromptAuthoringRequired": 1,
                "PromptReady": 1,
            },
        )

    def test_scoped_merge_preserves_uncompiled_requests(self) -> None:
        previous = [
            {"VisualID": "keep", "RequestID": "keep@1"},
            {"VisualID": "replace", "RequestID": "replace@1"},
        ]
        compiled = [{"VisualID": "replace", "RequestID": "replace@2"}]
        self.assertEqual(
            [item["RequestID"] for item in merge_request_catalog(previous, compiled, {"replace"})],
            ["keep@1", "replace@2"],
        )
```

- [ ] **Step 2: Run the new test file and verify failure**

Run:

```powershell
python -m unittest tools/美术工具/tests/test_art_catalog_integrity.py -v
```

Expected: FAIL because `art_catalog_integrity` does not exist.

- [ ] **Step 3: Implement the focused helper module**

Implement these public functions and constants:

```python
SUMMARY_KEYS = (
    "Ready",
    "StyleResolutionRequired",
    "Unsupported",
    "Invalid",
    "UnchangedPublished",
    "PromptAuthoringRequired",
    "PromptReady",
)


class CatalogIntegrityError(ValueError):
    pass


def primary_source_rank(entry: dict[str, Any]) -> tuple[int, str]:
    config_id = str(entry.get("ConfigID", ""))
    visual_id = str(entry.get("VisualID", ""))
    expected = f"item_{config_id}_icon"
    return (0 if expected == visual_id else 1, config_id)


def requirement_source_rank(source: dict[str, Any], visual_id: str) -> tuple[int, str]:
    config_id = str(source.get("ConfigID", ""))
    return (0 if f"item_{config_id}_icon" == visual_id else 1, config_id)


def canonicalize_manifest_entries(entries: list[dict[str, Any]]) -> list[dict[str, Any]]:
    by_visual: dict[str, list[dict[str, Any]]] = {}
    for entry in entries:
        visual_id = str(entry.get("VisualID", ""))
        if not visual_id:
            raise CatalogIntegrityError("visual_id_missing")
        by_visual.setdefault(visual_id, []).append(copy.deepcopy(entry))

    result: list[dict[str, Any]] = []
    for visual_id, group in sorted(by_visual.items()):
        if len(group) == 1:
            result.append(group[0])
            continue
        sources = [source for entry in group for source in entry.get("RequirementSources", [])]
        if len(sources) != len(group) or not all(source.get("ExplicitVisualID") is True for source in sources):
            raise CatalogIntegrityError(f"visual_id_collision_conflict:{visual_id}")
        structural = {
            json.dumps(
                {
                    "Domain": entry.get("Domain"),
                    "AssetType": entry.get("AssetType"),
                    "ProductionProfile": entry.get("ProductionProfile"),
                    "OutputPath": entry.get("OutputPath"),
                    "Spec": entry.get("Spec", {}),
                },
                sort_keys=True,
                ensure_ascii=False,
            )
            for entry in group
        }
        if len(structural) != 1:
            raise CatalogIntegrityError(f"visual_id_collision_contract_mismatch:{visual_id}")
        primary = min(group, key=primary_source_rank)
        primary["RequirementSources"] = sorted(
            sources,
            key=lambda source: requirement_source_rank(source, visual_id),
        )
        primary["VisualReusePolicy"] = {
            "Mode": "shared_visual",
            "DecisionSource": "config_explicit_visual_id",
            "Reason": "Multiple active config records explicitly reference the same visual field value.",
        }
        result.append(primary)
    return sorted(result, key=lambda item: (str(item.get("Domain", "")), str(item.get("VisualID", ""))))
```

Implement `validate_manifest_uniqueness` for duplicate `VisualID` and duplicate non-empty `OutputPath`; implement `recompute_catalog_summary` by counting Request fields; implement `merge_request_catalog` by preserving previous Requests whose VisualID is outside `selected_visual_ids`, replacing selected ones, sorting by VisualID, and rejecting duplicate VisualIDs or RequestIDs.

- [ ] **Step 4: Run the focused tests**

Run:

```powershell
python -m unittest tools/美术工具/tests/test_art_catalog_integrity.py -v
```

Expected: all tests PASS.

- [ ] **Step 5: Commit Task 1**

```powershell
git add -- tools/美术工具/art_catalog_integrity.py tools/美术工具/tests/test_art_catalog_integrity.py
git commit -m "feat: add art catalog integrity primitives"
```

---

### Task 2: Canonicalize Explicit Config Visual Reuse in Manifest Discovery

**Files:**
- Modify: `tools/美术工具/update_art_manifest.py`
- Modify: `tools/美术工具/tests/test_update_art_manifest.py`

**Interfaces:**
- Consumes: Task 1 `canonicalize_manifest_entries`.
- Produces: one canonical Manifest Entry per explicit shared `IconID`, with ordered `RequirementSources` and `VisualReusePolicy`.

- [ ] **Step 1: Add failing item reuse and conflict tests**

Add tests that create two temporary item JSON files with the same explicit `IconID`, invoke `scan_items`, canonicalize the resulting entries, and assert one output Entry. Add a second test where two preset entries collide without explicit source evidence and assert `visual_id_collision_conflict`.

The explicit source binding written by `scan_items` must be exactly:

```python
{
    "SourceType": "config",
    "ConfigID": data["ConfigID"],
    "ConfigSource": repo_path(path, project_root),
    "DisplayName": data.get("Name", data["ConfigID"]),
    "SourceFactsCN": source_facts,
    "VisualIDField": "IconID" if data.get("IconID") else "derived",
    "ExplicitVisualID": bool(data.get("IconID")),
}
```

- [ ] **Step 2: Run the update-manifest tests and verify failure**

```powershell
python -m unittest tools/美术工具/tests/test_update_art_manifest.py -v
```

Expected: new assertions fail because `RequirementSources` is absent and duplicate entries remain.

- [ ] **Step 3: Add source bindings and canonicalization to the owning script**

Import Task 1 helpers. Extend `PRESERVE_FIELDS` with `RequirementSources` and `VisualReusePolicy`. In `scan_items`, pass the source binding through `extra_fields={"RequirementSources": [binding]}`. After all scanners and presets have populated `entries`, run:

```python
entries = canonicalize_manifest_entries(entries)
validate_manifest_uniqueness(entries)
```

Build `existing_map` by grouping old entries and selecting `min(group, key=primary_source_rank)` so the current duplicate generated Manifest can migrate without silently taking the last duplicate. Do not merge lifecycle fields from secondary entries; the primary matching `item_<ConfigID>_icon` owns the retained lifecycle state.

- [ ] **Step 4: Run update-manifest tests**

```powershell
python -m unittest tools/美术工具/tests/test_update_art_manifest.py -v
```

Expected: all tests PASS.

- [ ] **Step 5: Commit Task 2**

```powershell
git add -- tools/美术工具/update_art_manifest.py tools/美术工具/tests/test_update_art_manifest.py
git commit -m "fix: canonicalize shared config visual ids"
```

---

### Task 3: Compile a Complete Catalog Safely

**Files:**
- Modify: `tools/美术工具/compile_art_generation_requests.py`
- Modify: `tools/美术工具/tests/test_compile_art_generation_requests.py`

**Interfaces:**
- Consumes: Task 1 uniqueness, merge, and Summary helpers.
- Produces: a full Request Catalog for full or scoped compiles, with no duplicate pointers.

- [ ] **Step 1: Add failing duplicate and partial-compile tests**

Add:

```python
def test_duplicate_manifest_visual_id_fails_before_compilation(self) -> None:
    manifest = self.make_manifest([self.entry("item_demo_icon"), self.entry("item_demo_icon")])
    with self.assertRaisesRegex(CatalogIntegrityError, "visual_id_duplicate:item_demo_icon"):
        compile_manifest_requests(manifest, project_root=Path.cwd())


def test_scoped_compile_preserves_unselected_requests(self) -> None:
    first = compile_manifest_requests(self.two_entry_manifest(), project_root=Path.cwd())
    changed = copy.deepcopy(first["Manifest"])
    changed["Entries"][0]["DisplayName"] = "changed"
    second = compile_manifest_requests(
        changed,
        project_root=Path.cwd(),
        visual_ids={changed["Entries"][0]["VisualID"]},
        previous_catalog=first["Catalog"],
    )
    self.assertEqual(len(second["Catalog"]["Requests"]), 2)
    self.assertEqual(
        second["Catalog"]["Requests"][1]["RequestID"],
        first["Catalog"]["Requests"][1]["RequestID"],
    )
```

- [ ] **Step 2: Run the compiler tests and verify failure**

```powershell
python -m unittest tools/美术工具/tests/test_compile_art_generation_requests.py -v
```

Expected: duplicate test or scoped preservation test FAILS.

- [ ] **Step 3: Integrate integrity checks and full-catalog merge**

At the start of `compile_manifest_requests`, call `validate_manifest_uniqueness(working["Entries"])`. Compile selected entries into `compiled_requests`; then use:

```python
requests = merge_request_catalog(
    previous_catalog.get("Requests", []) if previous_catalog else [],
    compiled_requests,
    selected if selected else {str(entry.get("VisualID", "")) for entry in working.get("Entries", [])},
)
summary = recompute_catalog_summary(requests)
```

For a scoped compile, keep untouched Manifest entry pointers unchanged and replace only selected pointers. For a full compile, require every canonical Manifest Entry to have exactly one compiled pointer. Set Catalog `Summary=summary`; keep the migration report scoped to items compiled in this invocation.

- [ ] **Step 4: Run compiler and wrapper tests**

```powershell
python -m unittest tools/美术工具/tests/test_compile_art_generation_requests.py -v
```

Expected: all tests PASS, including the PowerShell UTF-8 dry run.

- [ ] **Step 5: Commit Task 3**

```powershell
git add -- tools/美术工具/compile_art_generation_requests.py tools/美术工具/tests/test_compile_art_generation_requests.py
git commit -m "fix: preserve full art request catalog on scoped compile"
```

---

### Task 4: Recompute Summary on Publish and Validate Derived State

**Files:**
- Modify: `tools/美术工具/publish_art_prompt_revision.py`
- Modify: `tools/美术工具/validate_art_generation_requests.py`
- Modify: `tools/美术工具/tests/test_art_prompt_authoring_tools.py`
- Modify: `tools/美术工具/tests/test_validate_art_generation_requests.py`

**Interfaces:**
- Consumes: Task 1 `recompute_catalog_summary` and uniqueness validation.
- Produces: Summary-correct Prompt publication and `catalog_summary_stale` strict errors.

- [ ] **Step 1: Add failing publish and strict-validation tests**

In `test_art_prompt_authoring_tools.py`, publish one revision from a catalog whose Summary begins with `PromptReady=0`; assert persisted Summary becomes `PromptReady=1` and `PromptAuthoringRequired=0`.

In `test_validate_art_generation_requests.py`, mutate an otherwise valid catalog:

```python
catalog["Summary"]["PromptReady"] = 99
errors = validate_request_catalog(catalog, manifest, strict=True)
self.assertIn("catalog_summary_stale", errors)
```

Also assert duplicate Manifest OutputPath and duplicate VisualID are reported before pointer validation.

- [ ] **Step 2: Run both test files and verify failure**

```powershell
python -m unittest tools/美术工具/tests/test_art_prompt_authoring_tools.py tools/美术工具/tests/test_validate_art_generation_requests.py -v
```

Expected: Summary tests FAIL.

- [ ] **Step 3: Recompute and validate Summary**

In `publish_revision_file`, after all revisions are applied and before Manifest synchronization, set:

```python
updated["Summary"] = recompute_catalog_summary(updated.get("Requests", []))
```

Validate the updated Catalog and synchronized Manifest in memory before any `write_json` call. In `validate_request_catalog`, call Manifest uniqueness validation, independently compute `expected_summary`, and append `catalog_summary_stale` when it differs from `catalog.get("Summary")`.

- [ ] **Step 4: Run focused tests**

```powershell
python -m unittest tools/美术工具/tests/test_art_prompt_authoring_tools.py tools/美术工具/tests/test_validate_art_generation_requests.py -v
```

Expected: all tests PASS.

- [ ] **Step 5: Commit Task 4**

```powershell
git add -- tools/美术工具/publish_art_prompt_revision.py tools/美术工具/validate_art_generation_requests.py tools/美术工具/tests/test_art_prompt_authoring_tools.py tools/美术工具/tests/test_validate_art_generation_requests.py
git commit -m "fix: derive art catalog summary from requests"
```

---

### Task 5: Migrate the Current Catalog and Prove Strict Readiness

**Files:**
- Generated by scripts: `美术文档/_generated/art_manifest.json`
- Generated by scripts: `美术文档/_generated/art_generation_requests.json`
- Generated by scripts: `美术文档/_generated/art_generation_request_migration.json`
- Generated by scripts: `美术文档/_generated/视觉资产Manifest.md`
- Modify: `tools/美术工具/README.md`
- Modify: `agent_status/art.md`
- Regenerate: `DOCS_INDEX.md`
- Regenerate: `docs_index.json`

**Interfaces:**
- Consumes: Tasks 1-4.
- Produces: 308 canonical Manifest Entries, 308 unique Requests, and a strict-valid Catalog.

- [ ] **Step 1: Run the relevant unit-test slice**

```powershell
python -m unittest `
  tools/美术工具/tests/test_art_catalog_integrity.py `
  tools/美术工具/tests/test_update_art_manifest.py `
  tools/美术工具/tests/test_compile_art_generation_requests.py `
  tools/美术工具/tests/test_art_prompt_authoring_tools.py `
  tools/美术工具/tests/test_validate_art_generation_requests.py -v
```

Expected: all tests PASS.

- [ ] **Step 2: Regenerate the canonical Manifest**

```powershell
.\tools\美术工具\Update-ArtManifest.ps1
```

Expected:

- Manifest Entries: `308`.
- Each of these has two ordered `RequirementSources`: `item_gear_iron_armor_icon`, `item_gear_wooden_shield_icon`, `item_loot_rusty_coil_icon`, `item_loot_toxic_filter_icon`, `item_mat_core_tier1_icon`.
- Primary ConfigIDs are respectively `gear_iron_armor`, `gear_wooden_shield`, `loot_rusty_coil`, `loot_toxic_filter`, `mat_core_tier1`.
- Approved paths, selected paths, lifecycle state, and RegistryStatus remain unchanged from the primary entries.

- [ ] **Step 3: Compile the complete Request Catalog**

```powershell
.\tools\美术工具\Compile-ArtGenerationRequests.ps1 -Overwrite
```

Expected: `Ready=308`, `PromptReady=3`, `PromptAuthoringRequired=305`, no duplicate RequestID.

- [ ] **Step 4: Run strict validation and a scoped no-loss dry run**

```powershell
python tools/美术工具/validate_art_generation_requests.py `
  --manifest 美术文档/_generated/art_manifest.json `
  --request-catalog 美术文档/_generated/art_generation_requests.json `
  --strict

.\tools\美术工具\Compile-ArtGenerationRequests.ps1 `
  -VisualID doll_zero_hurt `
  -DryRun
```

Expected: strict validator prints `[OK] compiled art generation request catalog`; scoped dry run reports one compiled item but does not write or remove any of the 308 persisted Requests.

- [ ] **Step 5: Update operational documentation and current status**

Document the new error codes and shared-source schema in `tools/美术工具/README.md`. In `agent_status/art.md`, replace the duplicate-Catalog blocker with the exact 308/308 strict-pass evidence and retain the separate Zero consistency blocker for the next plan.

- [ ] **Step 6: Regenerate and validate documentation**

```powershell
.\tools\docs\Generate-DocsIndex.ps1
.\tools\docs\Validate-Docs.ps1
```

Expected: documentation validation passes.

- [ ] **Step 7: Run completion checks and commit generated migration**

```powershell
.\tools\agent\Invoke-AgentHealthCheck.ps1
git diff --check
git status --short
git add -- tools/美术工具/README.md agent_status/art.md DOCS_INDEX.md docs_index.json 美术文档/_generated/art_manifest.json 美术文档/_generated/art_generation_requests.json 美术文档/_generated/art_generation_request_migration.json 美术文档/_generated/视觉资产Manifest.md
git commit -m "fix: restore strict art request catalog"
```

Expected: only task files are staged; the user's unrelated `AGENTS.md` change remains unstaged.

---

## Plan Self-Review

- Spec coverage: VisualID uniqueness, explicit reuse, RequestID uniqueness, OutputPath uniqueness, Summary derivation, partial compile preservation, in-memory validation, current five-collision migration, and strict validation all have owning tasks.
- Maintainability: all shared rules live in one focused module; callers do not duplicate count or uniqueness logic.
- Scope: no character IdentityContract, replacement rubric, image generation, Approved replacement, Unity, Registry, or runtime work is included.
- Expected migration: 313 duplicate Entries become 308 canonical Entries without changing config source VisualIDs.
- Type consistency: `RequirementSources`, `VisualReusePolicy`, `recompute_catalog_summary`, and `merge_request_catalog` names are stable across tasks.
