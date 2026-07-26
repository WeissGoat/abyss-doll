---
id: plan_agent_authored_art_prompt_revisions
title: Agent 主导双格式美术 PromptRevision 实施计划
type: plan
role: 美术
domain: art_asset_production
status: active
source_of_truth: false
related: []
last_verified: 2026-07-26
update_rule: 修改 PromptAuthoringContext、PromptRevision、双格式 Variant、执行器迁移、测试或完成口径时更新本文档。
---

# Agent 主导双格式美术 PromptRevision Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 Project P3 的机械 Prompt 拼接替换为“机器编译需求上下文、Agent 独立创作自然语言与 Danbooru tags、版本化发布、脚本确定性重跑”的统一系统，并让通用批量素材和角色立绘共同消费它。

**Architecture:** `art_prompt_compiler.py` 只生成 `PromptAuthoringContext + TechnicalRequest + PreservationContract + RequirementFingerprint`。新增 `art_prompt_revision.py` 管理不可变 PromptRevision、双 Variant、约束映射和 active 版本；导出/发布 CLI 负责 Agent 交互面。批量、角色套组和底层生成器只消费已发布 Revision，provider adapter 只做协议序列化。

**Tech Stack:** Python 3、PowerShell 5.1/7、`unittest`、JSON Catalog、现有 P3 美术 Manifest/批次/角色套组工具。

## Global Constraints

- 当前工作区直接在 `main` 执行，不新建分支或 worktree。
- 结构化 Style、VisualIntent、IdentityLocks、RequiredChanges 只作为事实、参考和硬要求；编译器不得机械生成正式 Prompt。
- 每个 AI PromptRevision 固定拥有独立 `natural_language_v2` 与 `danbooru_tags_v2` 槽位；两者不得互相机械转换。
- 已发布 PromptRevision 不可覆盖；Prompt、标签、参考图、背景策略或能力改变时必须新建 revision。
- provider adapter 不得追加、重写、重复拼接自然语言或 tags。
- 旧 `natural_language_v1` / `danbooru_tags_v1` 只保留为 `legacy_compiled` 证据，不进入新的正式批次。
- 313 个现有 Entry 按需迁移 PromptRevision；不得要求一次性由 Agent 重写全部 Prompt。
- 不修改 Approved 文件、`.meta`、GUID、Registry、Manifest 公开资产状态或 `_IncomingAI` 候选。
- NineSlice 确定性专项能力可使用 `PromptAuthoringStatus=not_required`，不能被普通 AI Prompt 绕过。
- 所有代码任务先写失败测试，再实现最小行为；每个任务完成后独立提交。

---

## File Structure

- Modify `tools/美术工具/art_prompt_compiler.py`: 需求上下文、硬约束语义 ID、RequirementFingerprint；移除正式 Prompt serializer 职责。
- Modify `tools/美术工具/compile_art_generation_requests.py`: Catalog v2、旧 Revision 保留、Manifest 指针和 legacy v1 迁移。
- Create `tools/美术工具/art_prompt_revision.py`: Revision schema、双 Variant 校验、不可变发布、active 选择和 fingerprint。
- Create `tools/美术工具/export_art_prompt_authoring_package.py`: 导出 Agent 编写所需的单项/批量上下文包。
- Create `tools/美术工具/Export-ArtPromptAuthoringPackage.ps1`: PowerShell 稳定入口。
- Create `tools/美术工具/publish_art_prompt_revision.py`: 发布 JSON Revision 到 Catalog。
- Create `tools/美术工具/Publish-ArtPromptRevision.ps1`: PowerShell 稳定入口。
- Modify `tools/美术工具/validate_art_generation_requests.py`: Catalog v2、Revision、Variant、stale 和 legacy 校验。
- Modify `tools/美术工具/run_art_generation.py`: 选择 PromptRevision、v2 序列化和 evidence。
- Modify `tools/美术工具/run_art_production_batch.py`: 通用批量 PromptAuthoring Gate、Revision 分组和 v2 命令。
- Modify `tools/美术工具/run_character_portrait_set.py`: 套组 active Revision、参考依赖和 v2 计划。
- Modify PowerShell wrappers and targeted tests under `tools/美术工具/tests/`.
- Modify art workflow docs, both art skills, `agent_status/art.md`, `PROJECT_STATUS.md`, and generated docs index after implementation evidence exists.

---

### Task 1: Requirement Context Compiler v2

**Files:**
- Modify: `tools/美术工具/art_prompt_compiler.py`
- Modify: `tools/美术工具/compile_art_generation_requests.py`
- Modify: `tools/美术工具/tests/test_art_prompt_compiler.py`
- Modify: `tools/美术工具/tests/test_compile_art_generation_requests.py`

**Interfaces:**
- Produces: `build_prompt_authoring_context(entry, catalog, asset_sets) -> dict[str, Any]`.
- Produces: `compile_requirement_request(entry, catalog, asset_sets, compiler_version=2) -> dict[str, Any]`.
- Produces Request fields: `RequirementFingerprint`, `RequirementStatus`, `PromptAuthoringStatus`, `PromptAuthoringContext`, `TechnicalRequest`, `PreservationContract`, `ActivePromptRevisionID`, `PromptRevisions`.
- Later tasks consume stable hard-constraint IDs from `PromptAuthoringContext.HardConstraints.*[].ID`.

- [ ] **Step 1: Replace compiler expectations with failing v2 tests**

```python
def test_portrait_compiles_authoring_context_without_executable_prompt(self) -> None:
    request = compile_requirement_request(self.portrait, self.catalog, self.asset_sets)
    self.assertEqual(request["RequirementStatus"], "ready")
    self.assertEqual(request["PromptAuthoringStatus"], "prompt_authoring_required")
    self.assertNotIn("PromptVariants", request)
    self.assertNotIn("Positive", json.dumps(request, ensure_ascii=False))
    hard = request["PromptAuthoringContext"]["HardConstraints"]
    self.assertTrue(any(item["Text"] == "silver hair" for item in hard["Identity"]))
    self.assertTrue(any("冷淡" in item["Text"] for item in hard["RequiredChanges"]))
```

- [ ] **Step 2: Run focused tests and confirm the old serializer fails them**

Run: `python -m unittest tools.美术工具.tests.test_art_prompt_compiler tools.美术工具.tests.test_compile_art_generation_requests -v`

Expected: FAIL because `compile_requirement_request` and v2 fields do not exist.

- [ ] **Step 3: Implement context compilation and requirement fingerprint**

```python
def compile_requirement_request(entry, catalog, asset_sets, *, compiler_version=2):
    context = build_prompt_authoring_context(entry, catalog, asset_sets)
    body = {
        "CompilerVersion": compiler_version,
        "PromptAuthoringContext": context,
        "TechnicalRequest": _technical_request(entry, context),
        "PreservationContract": _preservation_contract(context),
    }
    requirement_fingerprint = sha256_json(body)
    return {
        "RequestID": f"{entry['VisualID']}@{requirement_fingerprint[:12]}",
        "VisualID": entry["VisualID"],
        "ProductionProfile": entry.get("ProductionProfile", "standard_asset"),
        "RequirementFingerprint": requirement_fingerprint,
        "RequirementStatus": "ready",
        "PromptAuthoringStatus": "prompt_authoring_required",
        **body,
        "ActivePromptRevisionID": "",
        "PromptRevisions": [],
    }
```

- [ ] **Step 4: Preserve revisions only when RequirementFingerprint matches**

```python
def carry_forward_prompt_revisions(new_request, previous_request):
    if previous_request.get("RequirementFingerprint") != new_request["RequirementFingerprint"]:
        return new_request
    revisions = copy.deepcopy(previous_request.get("PromptRevisions", []))
    new_request["PromptRevisions"] = revisions
    new_request["ActivePromptRevisionID"] = previous_request.get("ActivePromptRevisionID", "")
    new_request["PromptAuthoringStatus"] = "prompt_ready" if revisions else "prompt_authoring_required"
    return new_request
```

- [ ] **Step 5: Update Manifest pointer and mark old v1 output legacy**

Manifest `CompiledRequest` must contain `RequestID`, `RequirementFingerprint`, `RequirementStatus`, `PromptAuthoringStatus`, and `ActivePromptRevisionID`. Existing `PromptEN` fields remain unchanged compatibility evidence; Catalog migration records legacy variants under `LegacyPromptVariants` with `Status=legacy_compiled`.

- [ ] **Step 6: Run focused tests**

Run: `python -m unittest tools.美术工具.tests.test_art_prompt_compiler tools.美术工具.tests.test_compile_art_generation_requests -v`

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add -- tools/美术工具/art_prompt_compiler.py tools/美术工具/compile_art_generation_requests.py tools/美术工具/tests/test_art_prompt_compiler.py tools/美术工具/tests/test_compile_art_generation_requests.py
git commit -m "feat: compile art prompt authoring contexts"
```

---

### Task 2: PromptRevision Core And Validation

**Files:**
- Create: `tools/美术工具/art_prompt_revision.py`
- Create: `tools/美术工具/tests/test_art_prompt_revision.py`

**Interfaces:**
- Produces: `compute_revision_fingerprint(revision) -> str`.
- Produces: `validate_prompt_revision(request, revision) -> list[str]`.
- Produces: `publish_prompt_revision(request, revision, activate=True) -> dict[str, Any]`.
- Produces: `select_prompt_variant(request, prompt_revision_id="", prompt_format="auto", provider="") -> tuple[dict, str, dict]`.

- [ ] **Step 1: Write failing schema and immutability tests**

```python
def test_publish_requires_independent_dual_variant_slots(self) -> None:
    revision = make_revision()
    revision["Variants"].pop("danbooru_tags_v2")
    self.assertIn("prompt_variant_missing:danbooru_tags_v2", validate_prompt_revision(self.request, revision))

def test_publish_rejects_duplicate_revision_id_with_changed_content(self) -> None:
    published = publish_prompt_revision(self.request, make_revision(), activate=True)
    changed = make_revision()
    changed["Variants"]["natural_language_v2"]["Positive"] = "changed"
    with self.assertRaisesRegex(ValueError, "prompt_revision_immutable"):
        publish_prompt_revision(published, changed, activate=True)
```

- [ ] **Step 2: Run test and confirm module is missing**

Run: `python -m unittest tools.美术工具.tests.test_art_prompt_revision -v`

Expected: FAIL with import error.

- [ ] **Step 3: Implement revision fingerprint and dual Variant validation**

```python
PROMPT_FORMAT_BY_PROVIDER = {
    "openai_images": "natural_language_v2",
    "gemini_chat_image": "natural_language_v2",
    "gemini_nanobanana": "natural_language_v2",
    "novelai": "danbooru_tags_v2",
}

def validate_prompt_revision(request, revision):
    errors = []
    if revision.get("RequirementFingerprint") != request.get("RequirementFingerprint"):
        errors.append("prompt_revision_stale")
    variants = revision.get("Variants", {})
    for format_id in ("natural_language_v2", "danbooru_tags_v2"):
        if format_id not in variants:
            errors.append(f"prompt_variant_missing:{format_id}")
    return errors + _validate_constraint_mapping(request, variants)
```

- [ ] **Step 4: Implement immutable publish and active selection**

Publishing appends a deep copy, rejects conflicting duplicate IDs, computes `RevisionFingerprint`, and changes `ActivePromptRevisionID` only after validation succeeds. `unsupported` Variant requires non-empty `UnsupportedReason`; `ready` Variant requires complete ConstraintMapping.

- [ ] **Step 5: Implement provider-aware selection without semantic rewriting**

```python
def select_prompt_variant(request, prompt_revision_id="", prompt_format="auto", provider=""):
    revision = _find_revision(request, prompt_revision_id or request.get("ActivePromptRevisionID", ""))
    selected = PROMPT_FORMAT_BY_PROVIDER.get(provider, "natural_language_v2") if prompt_format == "auto" else prompt_format
    variant = revision["Variants"].get(selected)
    if not isinstance(variant, dict) or variant.get("Status") != "ready":
        raise ValueError(f"prompt_variant_not_ready:{selected}:{request.get('VisualID', '')}")
    return revision, selected, variant
```

- [ ] **Step 6: Run tests**

Run: `python -m unittest tools.美术工具.tests.test_art_prompt_revision -v`

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add -- tools/美术工具/art_prompt_revision.py tools/美术工具/tests/test_art_prompt_revision.py
git commit -m "feat: add immutable art prompt revisions"
```

---

### Task 3: Agent Authoring Export And Publish Tools

**Files:**
- Create: `tools/美术工具/export_art_prompt_authoring_package.py`
- Create: `tools/美术工具/Export-ArtPromptAuthoringPackage.ps1`
- Create: `tools/美术工具/publish_art_prompt_revision.py`
- Create: `tools/美术工具/Publish-ArtPromptRevision.ps1`
- Create: `tools/美术工具/tests/test_art_prompt_authoring_tools.py`

**Interfaces:**
- Export CLI: `--request-catalog`, repeated `--visual-id`, `--output-path`.
- Publish CLI: `--request-catalog`, `--revision-path`, optional `--no-activate`, `--dry-run`.
- Authoring package contains only contexts, contracts, resolved evidence paths and existing revision summaries; it never writes a Prompt.

- [ ] **Step 1: Write failing export/publish integration tests**

```python
def test_export_contains_context_but_no_legacy_prompt(self) -> None:
    package = export_authoring_package(self.catalog, ["doll_zero_cold"])
    item = package["Items"][0]
    self.assertIn("PromptAuthoringContext", item)
    self.assertNotIn("LegacyPromptVariants", item)

def test_publish_appends_and_activates_revision(self) -> None:
    result = publish_revision_file(self.catalog_path, self.revision_path, activate=True, dry_run=False)
    request = result["Catalog"]["Requests"][0]
    self.assertEqual(request["PromptAuthoringStatus"], "prompt_ready")
    self.assertEqual(request["ActivePromptRevisionID"], self.revision["PromptRevisionID"])
```

- [ ] **Step 2: Run failing tests**

Run: `python -m unittest tools.美术工具.tests.test_art_prompt_authoring_tools -v`

Expected: FAIL because modules are missing.

- [ ] **Step 3: Implement atomic export and publish Python CLIs**

Use existing UTF-8 `read_json`/`write_json` conventions. Publish writes a temporary sibling file and replaces the Catalog only after all revisions validate.

- [ ] **Step 4: Add PowerShell 5.1-safe wrappers**

Wrappers must pass empty default paths only when explicitly supplied, matching the fixed pattern in `Compile-ArtGenerationRequests.ps1` and `Run-CharacterPortraitSet.ps1`.

- [ ] **Step 5: Test Python and wrappers**

Run: `python -m unittest tools.美术工具.tests.test_art_prompt_authoring_tools -v`

Run: `powershell -NoProfile -File tools/美术工具/Export-ArtPromptAuthoringPackage.ps1 -RequestCatalogPath 美术文档/_generated/art_generation_requests.json -VisualID doll_zero_cold -OutputPath UnityClient/Logs/P3ArtProduction/prompt_authoring_dry_run.json -DryRun`

Expected: tests PASS; wrapper emits valid JSON without modifying Catalog.

- [ ] **Step 6: Commit**

```powershell
git add -- tools/美术工具/export_art_prompt_authoring_package.py tools/美术工具/Export-ArtPromptAuthoringPackage.ps1 tools/美术工具/publish_art_prompt_revision.py tools/美术工具/Publish-ArtPromptRevision.ps1 tools/美术工具/tests/test_art_prompt_authoring_tools.py
git commit -m "feat: add art prompt authoring tools"
```

---

### Task 4: Catalog v2 Strict Validator

**Files:**
- Modify: `tools/美术工具/validate_art_generation_requests.py`
- Modify: `tools/美术工具/tests/test_validate_art_generation_requests.py`

**Interfaces:**
- `validate_request_catalog(manifest, catalog, expected_catalog=None) -> dict` keeps its public signature.
- Validates RequirementFingerprint independently from RevisionFingerprint.
- Accepts legacy v1 only under `LegacyPromptVariants`; rejects it as active formal Prompt.

- [ ] **Step 1: Write failing v2 strict tests**

Add tests for valid authoring-required Request, valid active Revision, stale Revision, incomplete ConstraintMapping, invalid tag weight, missing unsupported reason, and active legacy v1 rejection.

- [ ] **Step 2: Run focused test**

Run: `python -m unittest tools.美术工具.tests.test_validate_art_generation_requests -v`

Expected: FAIL against the old RequestFingerprint/PromptVariants validator.

- [ ] **Step 3: Replace request-body hashing and Variant checks**

```python
def _requirement_body(request):
    return {key: request.get(key) for key in (
        "CompilerVersion", "PromptAuthoringContext", "TechnicalRequest", "PreservationContract"
    )}
```

Validate `sha256_json(_requirement_body(request)) == RequirementFingerprint`, then validate each PromptRevision through `validate_prompt_revision` and verify active ID membership.

- [ ] **Step 4: Preserve strict stale semantics**

Manifest runtime/lifecycle fields remain excluded. Changes to StyleRef, VisualIntent, Spec, AssetSet identity, SourceAssets or evidence references must stale the Requirement and all old Revisions.

- [ ] **Step 5: Run tests**

Run: `python -m unittest tools.美术工具.tests.test_validate_art_generation_requests -v`

Expected: PASS.

- [ ] **Step 6: Commit**

```powershell
git add -- tools/美术工具/validate_art_generation_requests.py tools/美术工具/tests/test_validate_art_generation_requests.py
git commit -m "feat: validate agent-authored art prompts"
```

---

### Task 5: Bottom-Level Generation Consumer v2

**Files:**
- Modify: `tools/美术工具/run_art_generation.py`
- Modify: `tools/美术工具/Run-ArtGeneration.ps1`
- Modify: `tools/美术工具/tests/test_run_art_generation_requests.py`

**Interfaces:**
- Replace `select_compiled_request` internals with `select_generation_request(catalog, entry, prompt_revision_id="", prompt_format="auto", provider="")`.
- `serialize_provider_prompt(variant, prompt_format)` supports only v2 formal formats and pure serialization.
- Evidence adds `RequirementSnapshot`, `PromptRevisionID`, `PromptRevisionFingerprint`, and `PromptRevisionSnapshot`.

- [ ] **Step 1: Write failing selection and no-rewrite tests**

```python
def test_natural_language_provider_request_equals_published_variant(self) -> None:
    request, revision, format_id, variant = select_generation_request(
        self.catalog, self.entry, provider="gemini_chat_image"
    )
    positive, negative = serialize_provider_prompt(variant, format_id)
    self.assertEqual(positive, variant["Positive"])
    self.assertEqual(negative, variant["Negative"])

def test_authoring_required_fails_before_provider_call(self) -> None:
    self.catalog["Requests"][0]["PromptRevisions"] = []
    with self.assertRaisesRegex(ValueError, "prompt_authoring_required"):
        select_generation_request(self.catalog, self.entry, provider="openai_images")
```

- [ ] **Step 2: Run focused tests**

Run: `python -m unittest tools.美术工具.tests.test_run_art_generation_requests -v`

Expected: FAIL because runtime consumes v1 PromptVariants.

- [ ] **Step 3: Implement v2 selection and serialization**

Natural language returns stored `Positive`/`Negative` byte-for-byte. Danbooru serialization only joins ordered tags and preserves structured weights using existing provider-neutral notation; no tag selection occurs here.

- [ ] **Step 4: Update request/evidence construction**

`make_request` receives the selected Revision and Variant. `build_generation_record` stores both snapshots and exact `ProviderRequest`; it must not record a reconstructed or shortened Prompt.

- [ ] **Step 5: Add explicit legacy escape hatch only for non-formal recovery**

Keep `--allow-legacy-prompt` disabled by default. When used, evidence must set `PromptFormat=legacy_unverified`; `Run-ArtProductionBatch` and `Run-CharacterPortraitSet` never pass it.

- [ ] **Step 6: Run tests**

Run: `python -m unittest tools.美术工具.tests.test_run_art_generation_requests -v`

Expected: PASS.

- [ ] **Step 7: Commit**

```powershell
git add -- tools/美术工具/run_art_generation.py tools/美术工具/Run-ArtGeneration.ps1 tools/美术工具/tests/test_run_art_generation_requests.py
git commit -m "feat: consume published art prompt revisions"
```

---

### Task 6: Standard Batch And Character Portrait Routes

**Files:**
- Modify: `tools/美术工具/run_art_production_batch.py`
- Modify: `tools/美术工具/Run-ArtProductionBatch.ps1`
- Modify: `tools/美术工具/run_character_portrait_set.py`
- Modify: `tools/美术工具/Run-CharacterPortraitSet.ps1`
- Modify: `tools/美术工具/tests/test_run_art_production_batch.py`
- Modify: `tools/美术工具/tests/test_run_character_portrait_set.py`

**Interfaces:**
- Batch Items consume `RequirementFingerprint`, optional `PromptRevisionID`, and `PromptFormat`.
- Batch groups include `PromptRevisionID`; mixed VisualIDs never share Prompt text.
- Portrait plan Items include active Revision identity, selected format, ReferenceAssets and PreservationContract.

- [ ] **Step 1: Write failing route tests**

Test standard batch blocks `prompt_authoring_required`, defaults OpenAI/Gemini to `natural_language_v2`, defaults NovelAI to `danbooru_tags_v2`, preserves `not_required` NineSlice routes, and groups by RevisionID. Test portrait plan orders master before difference and rejects missing/stale active Revision.

- [ ] **Step 2: Run route tests**

Run: `python -m unittest tools.美术工具.tests.test_run_art_production_batch tools.美术工具.tests.test_run_character_portrait_set -v`

Expected: FAIL because both routes consume RequestFingerprint/v1 formats.

- [ ] **Step 3: Update standard batch planning**

Resolve exact Request and Revision before grouping. A successful child exit with no current-batch decodable raw remains a generation failure. `character_portrait_set` remains hard-blocked from the standard runner.

- [ ] **Step 4: Update portrait set planning and wrapper ValidateSet**

```powershell
[ValidateSet("auto", "natural_language_v2", "danbooru_tags_v2")]
[string]$PromptFormat = "auto"
```

The portrait executor must pass the selected Revision without constructing “Preserve: ... Required change: ...” prefixes.

- [ ] **Step 5: Run tests and dry runs**

Run: `python -m unittest tools.美术工具.tests.test_run_art_production_batch tools.美术工具.tests.test_run_character_portrait_set -v`

Run: `.\tools\美术工具\Run-CharacterPortraitSet.ps1 -AssetSetID zero_dialogue_portrait_v1 -VisualID doll_zero_cold -DryRun`

Expected: tests PASS; real Catalog initially reports `prompt_authoring_required:doll_zero_cold` until Task 7 publishes the Pilot Revision.

- [ ] **Step 6: Commit**

```powershell
git add -- tools/美术工具/run_art_production_batch.py tools/美术工具/Run-ArtProductionBatch.ps1 tools/美术工具/run_character_portrait_set.py tools/美术工具/Run-CharacterPortraitSet.ps1 tools/美术工具/tests/test_run_art_production_batch.py tools/美术工具/tests/test_run_character_portrait_set.py
git commit -m "feat: route art batches through prompt revisions"
```

---

### Task 7: Catalog Migration And Pilot Agent Prompts

**Files:**
- Modify generated: `美术文档/_generated/art_generation_requests.json`
- Modify generated: `美术文档/_generated/art_manifest.json`
- Modify generated: `美术文档/_generated/art_generation_request_migration.json`
- Create run-scoped authoring/revision inputs under `UnityClient/Logs/P3ArtProduction/` only as evidence; do not commit ignored local evidence.

**Interfaces:**
- Pilot standard assets: `bg_combat_abyss`, `ui_icon_warning`.
- Pilot portrait difference: `doll_zero_cold` using `doll_zero_dialogue_neutral` as actual identity/reference source.
- Each Pilot gets independent Agent-authored `natural_language_v2` and `danbooru_tags_v2` slots; an honestly unsupported format records a reason.

- [ ] **Step 1: Compile Catalog v2 without publishing Prompt**

Run: `.\tools\美术工具\Compile-ArtGenerationRequests.ps1 -Overwrite`

Expected: 313 requirements preserved; active AI items become `prompt_authoring_required`; legacy Prompt evidence preserved; no asset status or Approved changes.

- [ ] **Step 2: Export Pilot authoring package**

Run: `.\tools\美术工具\Export-ArtPromptAuthoringPackage.ps1 -VisualID bg_combat_abyss,ui_icon_warning,doll_zero_cold -OutputPath UnityClient/Logs/P3ArtProduction/prompt_revision_pilot_20260726_01/authoring-package.json`

Expected: package includes contexts, source facts, reference paths and prior success/failure evidence.

- [ ] **Step 3: Agent authors three Revision JSON documents**

For `doll_zero_cold`, inspect the actual neutral selected image and old successful `generation.json`; write identity-first natural language and independently chosen Danbooru tags. For standard assets, write asset-class-specific prompts rather than a shared style string.

- [ ] **Step 4: Publish revisions**

Run:

```powershell
.\tools\美术工具\Publish-ArtPromptRevision.ps1 -RevisionPath UnityClient/Logs/P3ArtProduction/prompt_revision_pilot_20260726_01/bg_combat_abyss.prompt-revision.json
.\tools\美术工具\Publish-ArtPromptRevision.ps1 -RevisionPath UnityClient/Logs/P3ArtProduction/prompt_revision_pilot_20260726_01/ui_icon_warning.prompt-revision.json
.\tools\美术工具\Publish-ArtPromptRevision.ps1 -RevisionPath UnityClient/Logs/P3ArtProduction/prompt_revision_pilot_20260726_01/doll_zero_cold.prompt-revision.json
```

Expected: each target has `PromptAuthoringStatus=prompt_ready`, active immutable Revision and valid fingerprints.

- [ ] **Step 5: Strict validation and dry-run route checks**

Run: `python tools/美术工具/validate_art_generation_requests.py --manifest 美术文档/_generated/art_manifest.json --request-catalog 美术文档/_generated/art_generation_requests.json --strict`

Run: `.\tools\美术工具\Run-CharacterPortraitSet.ps1 -AssetSetID zero_dialogue_portrait_v1 -VisualID doll_zero_cold -DryRun`

Expected: strict PASS; portrait dry run selects exact active Revision and reference asset without generating an image.

- [ ] **Step 6: Verify migration invariants**

Compare pre/post VisualID set, OutputPath, Status, RegistryStatus, Approved hashes, `.meta` GUID and Registry hashes. Any change outside generated request fields fails the task.

- [ ] **Step 7: Commit generated Catalog migration and Pilot Revisions**

```powershell
git add -- 美术文档/_generated/art_generation_requests.json 美术文档/_generated/art_manifest.json 美术文档/_generated/art_generation_request_migration.json
git commit -m "feat: migrate art requests to agent prompts"
```

---

### Task 8: Workflow Documentation, Skills, Status And Full Verification

**Files:**
- Modify: `美术文档/00_美术流水线总览.md`
- Modify: `美术文档/01_Manifest规范.md`
- Modify: `美术文档/04_美术风格基准.md`
- Modify: `.codex/skills/p3-art-asset-production/SKILL.md`
- Modify: `.codex/skills/p3-generate-image/SKILL.md`
- Modify: `tools/美术工具/README.md`
- Modify: `agent_status/art.md`
- Modify: `PROJECT_STATUS.md`
- Modify generated: `docs_index.json`

**Interfaces:**
- Public workflow: requirement compile -> prompt_authoring_required -> Agent PromptRevision -> prompt_ready -> batch/portrait execution.
- Public Prompt formats: `natural_language_v2`, `danbooru_tags_v2`; v1 is legacy only.

- [ ] **Step 1: Update facts and skill boundaries**

Document that `p3-art-asset-production` owns requirement admission and Revision publication; `generate-image` consumes a published Variant and returns raw evidence. Remove statements saying the deterministic compiler writes final Prompt or that batch loops may append editing text.

- [ ] **Step 2: Update art/project status with evidence-limited claim**

Record implementation and dry-run validation. Do not claim new image quality or runtime art validation unless a real generation/visual review is performed.

- [ ] **Step 3: Run targeted and full art tool tests**

Run: `python -m unittest discover -s tools/美术工具/tests -p "test_*.py" -v`

Expected: all tests PASS.

- [ ] **Step 4: Run Catalog, docs and health validation**

Run: `python tools/美术工具/validate_art_generation_requests.py --manifest 美术文档/_generated/art_manifest.json --request-catalog 美术文档/_generated/art_generation_requests.json --strict`

Run: `.\tools\docs\Generate-DocsIndex.ps1`

Run: `.\tools\docs\Validate-Docs.ps1`

Run: `.\tools\agent\Invoke-AgentHealthCheck.ps1 -Strict`

Expected: all PASS after task files are committed and worktree is clean.

- [ ] **Step 5: Review diff and commit**

```powershell
git diff --check
git status --short
git add -- 美术文档/00_美术流水线总览.md 美术文档/01_Manifest规范.md 美术文档/04_美术风格基准.md .codex/skills/p3-art-asset-production/SKILL.md .codex/skills/p3-generate-image/SKILL.md tools/美术工具/README.md agent_status/art.md PROJECT_STATUS.md docs_index.json
git commit -m "docs: adopt agent-authored art prompts"
```

- [ ] **Step 6: Final mission review**

Confirm the compiler no longer creates formal Prompt text, both Pilot routes consume immutable dual-format Revisions, adapters do not rewrite semantics, old v1 remains non-executable legacy evidence, migration invariants hold, and all required status/fact sources are updated.
