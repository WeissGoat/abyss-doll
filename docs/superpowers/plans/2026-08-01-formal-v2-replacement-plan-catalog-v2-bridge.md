---
id: plan_formal_v2_replacement_plan_catalog_v2_bridge
title: Formal V2 主动迭代计划 Catalog V2 桥接实施计划
type: plan
role: 美术
domain: art_asset_production
status: active
source_of_truth: false
related: []
last_verified: 2026-08-01
update_rule: Catalog V2 bridge 的实现范围、验证命令或交付边界变化时同步本文件。
---

# Formal V2 Replacement Plan Catalog V2 Bridge Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the Formal V2 replacement planner persist and validate the exact active Catalog V2 Requirement and PromptRevision evidence, then prove the background and icon routes through a bounded dry-run.

**Architecture:** Add one normalization boundary in `generate_formal_v2_replacement_plan.py` that resolves Catalog V2 request evidence, validates the Manifest pointer, and returns either executable v2 prompt metadata or one fail-closed reason. Keep blocked entries visible in the audit plan, while the existing batch runner remains responsible for provider compatibility and command construction.

**Tech Stack:** Python 3, `unittest`, PowerShell wrappers, JSON/Markdown generated evidence, existing P3 art production scripts.

## Global Constraints

- Formal prompt formats are exactly `natural_language_v2` and `danbooru_tags_v2`.
- Do not fall back to `PromptVariants`, `CompileStatus`, `natural_language_v1`, or `danbooru_tags_v1`.
- `PromptReady=true` requires a ready Requirement, aligned Manifest pointer, `prompt_ready` authoring state, an exact active ready Revision, and at least one ready v2 Variant.
- This implementation may not call image providers or modify raw, `processed/`, `selected/`, Approved, Unity, Registry, or runtime state.
- The bounded validation set is exactly `bg_combat_abyss` and `ui_icon_warning`; use one variant and `openai_images` routes for background and icon.
- The maximum claim for the validation run is `dry_run_planned`.
- Work directly on `main` as explicitly authorized; do not create a branch or worktree.
- Do not modify or commit `tools/ai-image-gateway` or `.mission/*.csv`.

---

### Task 1: Strict Catalog V2 Planner Evidence

**Files:**
- Modify: `tools/美术工具/tests/test_generate_formal_v2_replacement_plan.py`
- Modify: `tools/美术工具/generate_formal_v2_replacement_plan.py`

**Interfaces:**
- Consumes: Manifest `Entry.CompiledRequest` and Catalog V2 `Requests[]` with `PromptRevisions[].Variants`.
- Produces: `resolve_prompt_evidence(entry: dict[str, Any], request: dict[str, Any] | None) -> dict[str, Any]` and plan Item fields `RequestID`, `RequirementFingerprint`, `PromptAuthoringStatus`, `PromptRevisionID`, `PromptRevisionFingerprint`, `PromptFormats`, `PromptReady`, and `PromptBlockReason`.

- [ ] **Step 1: Replace the legacy fixture with a reusable Catalog V2 fixture**

Add a test helper that writes a ready Catalog V2 request and its aligned Manifest pointer:

```python
def attach_catalog_v2_request(
    entry: dict,
    *,
    ready_formats: tuple[str, ...] = ("natural_language_v2", "danbooru_tags_v2"),
) -> dict:
    visual_id = entry["VisualID"]
    request_id = f"{visual_id}@requirement"
    requirement_fingerprint = f"requirement-{visual_id}"
    revision_id = f"{request_id}/prompt-001"
    variants = {
        format_id: {"Format": format_id, "Status": "ready"}
        for format_id in ready_formats
    }
    request = {
        "VisualID": visual_id,
        "RequestID": request_id,
        "RequirementStatus": "ready",
        "RequirementFingerprint": requirement_fingerprint,
        "PromptAuthoringStatus": "prompt_ready",
        "ActivePromptRevisionID": revision_id,
        "PromptRevisions": [{
            "PromptRevisionID": revision_id,
            "RequirementFingerprint": requirement_fingerprint,
            "Status": "ready",
            "RevisionFingerprint": f"revision-{visual_id}",
            "Variants": variants,
        }],
    }
    entry["CompiledRequest"] = {
        "RequestID": request_id,
        "RequirementFingerprint": requirement_fingerprint,
        "PromptAuthoringStatus": "prompt_ready",
        "ActivePromptRevisionID": revision_id,
    }
    return request
```

- [ ] **Step 2: Add failing ready and blocked evidence tests**

Add focused tests asserting:

```python
self.assertEqual(item["RequirementFingerprint"], "requirement-ui_button_primary")
self.assertEqual(item["PromptRevisionID"], "ui_button_primary@requirement/prompt-001")
self.assertEqual(item["PromptRevisionFingerprint"], "revision-ui_button_primary")
self.assertEqual(set(item["PromptFormats"]), {"natural_language_v2", "danbooru_tags_v2"})
self.assertTrue(item["PromptReady"])
self.assertEqual(item["PromptBlockReason"], "")
```

Cover these exact blocked outcomes in separate subtests or test methods:

```python
"catalog_request_missing"
"requirement_not_ready"
"prompt_authoring_required"
"manifest_pointer_mismatch:RequestID"
"manifest_pointer_mismatch:RequirementFingerprint"
"manifest_pointer_mismatch:PromptAuthoringStatus"
"manifest_pointer_mismatch:ActivePromptRevisionID"
"active_prompt_revision_missing"
"prompt_revision_stale"
"prompt_revision_not_ready"
"prompt_revision_no_ready_v2_variant"
```

Also add a legacy-only request with `CompileStatus` and `PromptVariants` and assert `PromptReady` is false, `PromptFormats` is empty, and no v1 format appears in the serialized Item.

- [ ] **Step 3: Run the focused test and confirm RED**

Run:

```powershell
python -m unittest tools/美术工具/tests/test_generate_formal_v2_replacement_plan.py -v
```

Expected: FAIL because the current planner emits `RequestFingerprint`, reads legacy `PromptVariants`, and does not expose revision evidence or block reasons.

- [ ] **Step 4: Implement the single Catalog V2 normalization boundary**

In `generate_formal_v2_replacement_plan.py`, import the canonical format constants and implement the resolver with first-failure semantics:

```python
from art_prompt_revision import PROMPT_FORMATS

def resolve_prompt_evidence(
    entry: dict[str, Any],
    request: dict[str, Any] | None,
) -> dict[str, Any]:
    compiled = entry.get("CompiledRequest")
    compiled = compiled if isinstance(compiled, dict) else {}
    evidence = {
        "RequestID": str((request or {}).get("RequestID", "")),
        "RequirementFingerprint": str((request or {}).get("RequirementFingerprint", "")),
        "PromptAuthoringStatus": str((request or {}).get("PromptAuthoringStatus", "")),
        "PromptRevisionID": str((request or {}).get("ActivePromptRevisionID", "")),
        "PromptRevisionFingerprint": "",
        "PromptFormats": [],
        "PromptReady": False,
        "PromptBlockReason": "",
    }
    if request is None:
        evidence["PromptBlockReason"] = "catalog_request_missing"
        return evidence
    if request.get("RequirementStatus") != "ready":
        evidence["PromptBlockReason"] = "requirement_not_ready"
        return evidence
    if request.get("PromptAuthoringStatus") != "prompt_ready":
        evidence["PromptBlockReason"] = "prompt_authoring_required"
        return evidence
    for field in ("RequestID", "RequirementFingerprint", "PromptAuthoringStatus", "ActivePromptRevisionID"):
        if compiled.get(field) != request.get(field):
            evidence["PromptBlockReason"] = f"manifest_pointer_mismatch:{field}"
            return evidence
    revision = next(
        (
            value for value in request.get("PromptRevisions", [])
            if isinstance(value, dict)
            and value.get("PromptRevisionID") == request.get("ActivePromptRevisionID")
        ),
        None,
    )
    if revision is None:
        evidence["PromptBlockReason"] = "active_prompt_revision_missing"
        return evidence
    evidence["PromptRevisionFingerprint"] = str(revision.get("RevisionFingerprint", ""))
    if revision.get("RequirementFingerprint") != request.get("RequirementFingerprint"):
        evidence["PromptBlockReason"] = "prompt_revision_stale"
        return evidence
    if revision.get("Status") != "ready":
        evidence["PromptBlockReason"] = "prompt_revision_not_ready"
        return evidence
    variants = revision.get("Variants")
    variants = variants if isinstance(variants, dict) else {}
    evidence["PromptFormats"] = [
        format_id for format_id in PROMPT_FORMATS
        if isinstance(variants.get(format_id), dict)
        and variants[format_id].get("Status") == "ready"
    ]
    if not evidence["PromptFormats"]:
        evidence["PromptBlockReason"] = "prompt_revision_no_ready_v2_variant"
        return evidence
    evidence["PromptReady"] = True
    return evidence
```

Update `build_item()` to merge this evidence and remove the legacy `prompt_ready()` path, `RequestFingerprint`, `PromptVariants`, and v1 format discovery.

- [ ] **Step 5: Run focused planner and runner tests and confirm GREEN**

Run:

```powershell
python -m unittest tools/美术工具/tests/test_generate_formal_v2_replacement_plan.py tools/美术工具/tests/test_run_art_production_batch.py -v
```

Expected: all tests pass; no planner Item contains `RequestFingerprint` or a v1 Prompt format.

- [ ] **Step 6: Review the diff and commit the planner slice**

Run:

```powershell
git diff --check
git diff -- tools/美术工具/generate_formal_v2_replacement_plan.py tools/美术工具/tests/test_generate_formal_v2_replacement_plan.py
git add -- tools/美术工具/generate_formal_v2_replacement_plan.py tools/美术工具/tests/test_generate_formal_v2_replacement_plan.py
git commit -m "fix: bridge Formal V2 replacement plans to Catalog V2"
```

Expected: commit contains only the planner and its focused test file.

### Task 2: Bounded Background and Icon Dry-Run

**Files:**
- Modify: `tools/美术工具/README.md`
- Modify: `agent_status/art.md`
- Generate ignored evidence: `UnityClient/Logs/P3ArtProduction/formalv2_catalog_v2_bridge_dryrun_20260801_01/plan.json`
- Generate ignored evidence: `UnityClient/Logs/P3ArtProduction/formalv2_catalog_v2_bridge_dryrun_20260801_01/plan.md`
- Generate ignored evidence: `UnityClient/Logs/P3ArtProduction/formalv2_catalog_v2_bridge_dryrun_20260801_01/batch/`

**Interfaces:**
- Consumes: the current Manifest, strict Catalog V2, and the Task 1 plan Item evidence.
- Produces: a run-scoped audit plan and dry-run execution plan with separate background/icon groups, exact active Revision IDs, provider-compatible v2 formats, and no provider invocation.

- [ ] **Step 1: Validate the current Catalog before generating the run plan**

Run:

```powershell
python tools/美术工具/validate_art_generation_requests.py --manifest 美术文档/_generated/art_manifest.json --request-catalog 美术文档/_generated/art_generation_requests.json --strict
```

Expected: exit 0 with the current Request Catalog passing strict validation.

- [ ] **Step 2: Generate a run-scoped two-item replacement plan**

Run:

```powershell
$run = "UnityClient/Logs/P3ArtProduction/formalv2_catalog_v2_bridge_dryrun_20260801_01"
New-Item -ItemType Directory -Force -Path $run | Out-Null
.\tools\美术工具\Generate-FormalV2ReplacementPlan.ps1 `
  -BatchID formalv2_catalog_v2_bridge_dryrun_20260801_01 `
  -VisualID bg_combat_abyss,ui_icon_warning `
  -Variants 1 `
  -DelaySeconds 0 `
  -OutputJson "$run/plan.json" `
  -OutputMarkdown "$run/plan.md"
```

Expected: exactly two planned Items and two PromptReady Items. Each Item contains its exact current `RequestID`, `RequirementFingerprint`, `PromptRevisionID`, `PromptRevisionFingerprint`, and only v2 `PromptFormats`.

- [ ] **Step 3: Run the batch planner in dry-run mode with explicit routes**

Run:

```powershell
.\tools\美术工具\Run-ArtProductionBatch.ps1 `
  -PlanPath "$run/plan.json" `
  -RequestCatalogPath 美术文档/_generated/art_generation_requests.json `
  -Route background=openai_images,icon=openai_images `
  -ProductionRunID formalv2_catalog_v2_bridge_dryrun_20260801_01 `
  -LogRoot "$run/batch" `
  -DryRun
```

Expected: exit 0, no blocked Items, and two execution Groups. The background and icon groups use `openai_images`, `natural_language_v2`, their exact active `PromptRevisionID`, and generation commands containing `--request-catalog`, `--prompt-format`, and `--prompt-revision-id` without executing those commands.

- [ ] **Step 4: Assert the dry-run evidence and mutation boundary**

Use PowerShell to load the generated JSON files and assert:

```powershell
$plan = Get-Content -Raw -Encoding UTF8 "$run/plan.json" | ConvertFrom-Json
if ($plan.Items.Count -ne 2 -or ($plan.Items | Where-Object { -not $_.PromptReady }).Count -ne 0) { throw "plan_not_ready" }
if (($plan.Items.PromptFormats | ForEach-Object { $_ }) -match '_v1$') { throw "legacy_prompt_format" }
$execution = Get-ChildItem -Recurse -File "$run/batch" -Filter execution-plan.json | Select-Object -First 1
if (-not $execution) { throw "execution_plan_missing" }
$payload = Get-Content -Raw -Encoding UTF8 $execution.FullName | ConvertFrom-Json
if ($payload.Blocked.Count -ne 0 -or $payload.Groups.Count -ne 2) { throw "unexpected_dry_run_shape" }
if (($payload.Groups.AssetClass | Sort-Object) -join ',' -ne 'background,icon') { throw "route_group_mismatch" }
```

Also inspect `git status --short` and confirm no tracked raw, processed, selected, Approved, Unity asset, Registry, or submodule file changed.

- [ ] **Step 5: Update the existing tool and art status facts**

In `tools/美术工具/README.md`, update the existing Formal V2 replacement-plan section to state that it consumes Catalog V2 only, persists exact Requirement/Revision evidence, and emits explicit block reasons without v1 fallback.

In `agent_status/art.md`, replace or extend an existing Catalog V2 bullet without increasing the status page beyond its current compact structure. Record the run ID, the two VisualIDs, both explicit routes, strict/dry-run evidence, claim ceiling `dry_run_planned`, and that no provider or asset-state mutation occurred.

- [ ] **Step 6: Run full verification**

Run:

```powershell
python -m unittest discover -s tools/美术工具/tests -p "test_*.py" -v
python tools/美术工具/validate_art_generation_requests.py --manifest 美术文档/_generated/art_manifest.json --request-catalog 美术文档/_generated/art_generation_requests.json --strict
.\tools\docs\Generate-DocsIndex.ps1
.\tools\docs\Validate-Docs.ps1
git diff --check
git status --short
```

Expected: all art-tool tests pass, Catalog strict exits 0, docs validation passes, diff check is clean, and only the README/status/index files belonging to this task are tracked changes.

- [ ] **Step 7: Commit the dry-run and documentation slice**

Run:

```powershell
git add -- tools/美术工具/README.md agent_status/art.md DOCS_INDEX.md docs_index.json
git diff --cached --check
git commit -m "docs: record Catalog V2 replacement plan dry-run"
.\tools\agent\Invoke-AgentHealthCheck.ps1 -Strict
```

Expected: the run-scoped evidence remains ignored, the commit contains only existing facts and regenerated indexes, and the strict health check passes on a clean worktree.

## Self-Review

- Spec coverage: strict V2 parsing, exact active Revision, Manifest pointer alignment, explicit block reasons, v1 rejection, bounded background/icon dry-run, and non-mutation boundaries are each mapped to a test or execution step.
- Placeholder scan: every implementation and verification step contains concrete code, commands, and expected results.
- Type consistency: the plan consistently uses `RequirementFingerprint`, `PromptRevisionID`, `PromptRevisionFingerprint`, `PromptFormats`, `PromptReady`, and `PromptBlockReason`; legacy `RequestFingerprint` appears only in removal assertions.
