---
id: plan_art_production_evidence_gates_and_resume
title: 美术生产证据、质量门禁与人物立绘恢复实施计划
type: plan
role: 美术
domain: art_asset_production
status: active
source_of_truth: false
related: []
last_verified: 2026-07-31
update_rule: 实现文件边界、测试命令、兼容策略或阶段提交发生变化时更新本文档。
---

# 美术生产证据、质量门禁与人物立绘恢复 Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 在不新增平行工作流入口的前提下，收口正式 Prompt 执行证据、技术门禁、selected 替换质量保护，并让现有角色立绘 route 支持后半段证据和安全 Resume。

**Architecture:** 保留 `Run-ArtProductionBatch.ps1`、`Run-ArtGeneration.ps1` 和 `Run-CharacterPortraitSet.ps1`。确定性校验继续由现有 Python 核心完成，Agent 负责 Prompt 创作、生成能力选择、背景处理决策和视觉评审；人物立绘只增加 run-scoped checkpoint 和对既有处理/选择工具的续跑调用。

**Tech Stack:** Python 3.10、PowerShell 5.1/7、Pillow、`unittest`、JSON evidence、现有 P3 美术工具与 Skill。

## Global Constraints

- 当前分支为 `main`；不创建新分支或 worktree。
- 不新增第三个美术工作流入口或通用工作流引擎。
- formal v2 执行源只能是 active immutable PromptRevision；旧 Prompt 只允许显式 `legacy_unverified`。
- `processed/<正整数>` 保持不可变；修复发布下一数字轮次。
- override 不能修改原始技术结论，且不能绕过解码、SHA、尺寸、格式、Alpha 合同或 nine-slice 结构硬门禁。
- selected replacement 默认严格优于当前 selected，且保护维度不得倒退。
- 人物立绘 route 保持方法中立；脚本记录 Agent 选择，不固定文生图、图生图、编辑或 provider。
- 本轮真实 Pilot claim ceiling 为 `selected`；不修改 Approved、Unity、Registry 或 runtime state。
- `tools/ai-image-gateway` 是 submodule，本计划不提交其内部改动。
- 每个任务只暂存本任务文件并独立提交。

---

## File Structure

- Modify `tools/美术工具/run_art_generation.py`: formal/legacy evidence mode 和精确 ProviderRequest。
- Modify `tools/美术工具/tests/test_run_art_generation_requests.py`: typed Prompt evidence 和 legacy 隔离测试。
- Modify `tools/美术工具/art_background.py`: 分离人物负空间 warning 与真实透明洞 failure。
- Modify `tools/美术工具/register_art_processing_round.py`: Registrar 重算、fingerprint、override 校验和 effective decision。
- Modify `tools/美术工具/Register-ArtProcessingRound.ps1`: 显式传入本次允许的 override RuleID。
- Modify `tools/美术工具/tests/test_art_background.py`: 人物透明度规则回归。
- Modify `tools/美术工具/tests/test_register_art_processing_round.py`: 伪造报告、合法/非法 override 和不可 override 规则测试。
- Modify `tools/美术工具/select_art_candidate.py`: replacement baseline、分数 delta、保护维度和 stale SHA 门禁。
- Modify `tools/美术工具/tests/test_select_art_candidate.py`: 首选、替换、幂等和质量回退测试。
- Modify `tools/美术工具/Run-CharacterPortraitSet.ps1`: `ExecutionMode`、`Resume`、决策/评审输入和覆盖授权参数。
- Modify `tools/美术工具/run_character_portrait_set.py`: run state、generation snapshot、阶段恢复、依赖阻塞和 summary。
- Modify `tools/美术工具/tests/test_run_character_portrait_set.py`: Resume 幂等、stale 证据和依赖测试。
- Modify `tools/美术工具/README.md`: CLI、evidence 和恢复协议。
- Modify `.codex/skills/p3-art-asset-production/SKILL.md` and relevant references: 两条 route 的新门禁和恢复规则。
- Modify `.codex/skills/p3-generate-image/SKILL.md`: formal/legacy generation evidence 边界。
- Modify `美术文档/00_美术流水线总览.md`, `美术文档/01_Manifest规范.md`, `agent_status/art.md`: 稳定事实与实现状态。
- Regenerate `DOCS_INDEX.md` and `docs_index.json` through `Generate-DocsIndex.ps1`。

---

### Task 1: Formal Prompt Execution Evidence

**Files:**
- Modify: `tools/美术工具/run_art_generation.py`
- Test: `tools/美术工具/tests/test_run_art_generation_requests.py`

**Interfaces:**
- Consumes: `build_generation_record(... requirement_request, prompt_revision, prompt_format, provider_request ...)` existing callers.
- Produces: `EvidenceMode=formal_v2|legacy_unverified`; formal records omit top-level legacy fields; legacy records use `LegacyPromptInput`.

- [ ] **Step 1: Write failing formal and legacy evidence tests**

Add assertions to the existing formal record test:

```python
self.assertEqual(record["EvidenceMode"], "formal_v2")
self.assertNotIn("PromptEN", record)
self.assertNotIn("NegativePromptEN", record)
self.assertNotIn("LegacyPromptInput", record)
self.assertEqual(record["ProviderRequest"]["Prompt"], variant["Positive"])
```

Add a legacy record test:

```python
def test_legacy_generation_record_is_explicitly_isolated(self) -> None:
    entry = copy.deepcopy(self.entry)
    entry["PromptEN"] = "legacy positive"
    entry["NegativePromptEN"] = "legacy negative"
    record = build_generation_record(
        entry=entry,
        batch_id="legacy-001",
        request_ids=[],
        provider="openai_images",
        model="legacy",
        requested_width=256,
        requested_height=256,
        requested_count=1,
        outputs=[],
        errors=[],
        created_at="2026-07-31T12:00:00+08:00",
    )
    self.assertEqual(record["EvidenceMode"], "legacy_unverified")
    self.assertEqual(
        record["LegacyPromptInput"],
        {"PromptEN": "legacy positive", "NegativePromptEN": "legacy negative"},
    )
    self.assertNotIn("PromptEN", record)
```

- [ ] **Step 2: Run the focused tests and verify RED**

Run:

```powershell
python -m unittest tools/美术工具/tests/test_run_art_generation_requests.py -v
```

Expected: formal test fails because old fields remain and `EvidenceMode` is missing; legacy test fails because `LegacyPromptInput` is missing.

- [ ] **Step 3: Implement mutually exclusive evidence modes**

In `build_generation_record`, build common evidence first, then conditionally add legacy input:

```python
formal_v2 = requirement_request is not None and prompt_revision is not None
record = {
    "EvidenceMode": "formal_v2" if formal_v2 else "legacy_unverified",
    "VisualID": entry["VisualID"],
    # existing non-prompt evidence fields remain unchanged
    "RequirementRequestID": requirement_request.get("RequestID", "") if requirement_request else "",
    "PromptRevisionID": prompt_revision.get("PromptRevisionID", "") if prompt_revision else "",
    "PromptFormat": prompt_format,
    "RequirementSnapshot": copy.deepcopy(requirement_request) if requirement_request else {},
    "PromptRevisionSnapshot": copy.deepcopy(prompt_revision) if prompt_revision else {},
    "ProviderRequest": copy.deepcopy(provider_request or {}),
}
if not formal_v2:
    record["LegacyPromptInput"] = {
        "PromptEN": str(entry.get("PromptEN", "") or ""),
        "NegativePromptEN": str(entry.get("NegativePromptEN", "") or ""),
    }
return record
```

Do not add a second `ExecutedPrompt` field. The selected Variant remains in `PromptRevisionSnapshot`; actual serialized content remains in `ProviderRequest`.

- [ ] **Step 4: Run focused and adjacent tests**

```powershell
python -m unittest tools/美术工具/tests/test_run_art_generation_requests.py -v
```

Expected: all tests pass.

- [ ] **Step 5: Commit Task 1**

```powershell
git add -- tools/美术工具/run_art_generation.py tools/美术工具/tests/test_run_art_generation_requests.py
git commit -m "fix: isolate formal art prompt evidence"
```

---

### Task 2: Registrar Technical Recalculation and Overrides

**Files:**
- Modify: `tools/美术工具/art_background.py`
- Modify: `tools/美术工具/register_art_processing_round.py`
- Modify: `tools/美术工具/Register-ArtProcessingRound.ps1`
- Test: `tools/美术工具/tests/test_art_background.py`
- Test: `tools/美术工具/tests/test_register_art_processing_round.py`

**Interfaces:**
- Produces: `technical_review_v2` candidate review fingerprint; `technical_override_v1`; decision candidate `AutomaticStatus` and `AppliedOverrides`.
- CLI: repeatable `--allow-technical-override RULE_ID`; PowerShell `[string[]]$AllowTechnicalOverride`.

- [ ] **Step 1: Write failing transparency classification tests**

Update the current internal-hole assertion to expect `unexpected_transparent_holes`. Add a silhouette with high occupied-box transparency but no enclosed hole:

```python
def test_character_negative_space_is_warning_not_hard_failure(self) -> None:
    image = Image.new("RGBA", (100, 100), (0, 0, 0, 0))
    draw = ImageDraw.Draw(image)
    draw.rectangle((10, 5, 22, 95), fill=(255, 255, 255, 255))
    draw.rectangle((78, 5, 90, 95), fill=(255, 255, 255, 255))
    review = review_candidate(
        image,
        source_spec={"Width": 100, "Height": 100, "AlphaRequired": True},
        composition_spec={"SafePaddingPercent": 0},
        production_profile="character_portrait_set",
    )
    self.assertNotEqual(review["Status"], "failed")
    self.assertIn("high_occupied_bbox_transparency", review["Warnings"])
```

- [ ] **Step 2: Write failing Registrar forgery and override tests**

Refactor the test fixture Manifest entry to include a complete 2x2 SourceSpec and BackgroundPolicy. Generate `technical_review.json` with per-candidate automatic results. Add tests for:

```python
def test_registration_rejects_forged_passed_technical_review(self) -> None:
    staging = self.make_staging(state="passed", candidates=[("001.png", "passed", "red")])
    review = json.loads((staging / "technical_review.json").read_text(encoding="utf-8"))
    review["Candidates"][0]["Status"] = "passed"
    review["Candidates"][0]["ReviewFingerprint"] = "forged"
    self.write_json(staging / "technical_review.json", review)
    with self.assertRaisesRegex(ValueError, "technical_review_mismatch"):
        self.register(staging)
```

Also cover:

- valid automatic passed registration;
- non-whitelisted override rejected;
- override CandidateSHA or BaseReviewFingerprint mismatch rejected;
- authorized `subject_outside_safe_canvas -> accept_as_warning` produces effective passed with `AutomaticStatus=failed`;
- `wrong_dimensions`, `required_alpha_missing`, `nine_slice_*`, decode/hash/format mismatch remain non-overridable.

- [ ] **Step 3: Run focused tests and verify RED**

```powershell
python -m unittest `
  tools/美术工具/tests/test_art_background.py `
  tools/美术工具/tests/test_register_art_processing_round.py -v
```

Expected: new reason/warning and Registrar validation tests fail.

- [ ] **Step 4: Split portrait transparency reasons**

Replace the combined branch in `review_candidate`:

```python
if production_profile == "character_portrait_set":
    if float(metrics["OccupiedBBoxTransparency"]) > 0.45:
        warnings.append("high_occupied_bbox_transparency")
    if float(metrics["TransparentHoleRatio"]) > 0.05:
        reasons.append("unexpected_transparent_holes")
```

- [ ] **Step 5: Add deterministic review fingerprint and recomputation**

In `register_art_processing_round.py`, import the existing review helpers and define:

```python
TECHNICAL_REVIEW_SCHEMA = "technical_review_v2"
TECHNICAL_OVERRIDE_SCHEMA = "technical_override_v1"
OVERRIDABLE_RULES = {"subject_outside_safe_canvas": "accept_as_warning"}
NON_OVERRIDABLE_RULES = {
    "wrong_dimensions",
    "empty_alpha",
    "required_alpha_missing",
    "nine_slice_many_components",
    "nine_slice_edge_coverage_low",
}

def review_fingerprint(review: dict[str, Any]) -> str:
    payload = copy.deepcopy(review)
    payload.pop("ReviewFingerprint", None)
    encoded = json.dumps(payload, ensure_ascii=False, sort_keys=True, separators=(",", ":"))
    return hashlib.sha256(encoded.encode("utf-8")).hexdigest()
```

Add `recalculate_candidate_review(entry, candidate_path)` using `review_candidate(...)` and candidate SHA. Compare every submitted automatic review to recalculated status, reasons, warnings, metrics and fingerprint before applying override.

- [ ] **Step 6: Apply only explicitly authorized overrides**

Extend `register_processing_round(... allowed_override_rules: set[str])`. Load optional staging `technical_override.json`, validate schema, VisualID, candidate SHA and base fingerprint, and accept only:

```python
if rule_id not in allowed_override_rules:
    raise PermissionError(f"technical_override_authorization_required:{rule_id}")
if OVERRIDABLE_RULES.get(rule_id) != action:
    raise ValueError(f"technical_override_not_allowed:{rule_id}:{action}")
```

Write compatible decision candidates with:

```python
candidate["AutomaticStatus"] = automatic_status
candidate["AppliedOverrides"] = applied_rule_ids
candidate["Status"] = effective_status
```

- [ ] **Step 7: Expose repeatable PowerShell authorization**

Add:

```powershell
[string[]]$AllowTechnicalOverride = @()
```

and serialize each value as `--allow-technical-override <RuleID>`. JSON alone must never grant permission.

- [ ] **Step 8: Run technical, optimizer, resolver and wrapper tests**

```powershell
python -m unittest `
  tools/美术工具/tests/test_art_background.py `
  tools/美术工具/tests/test_register_art_processing_round.py `
  tools/美术工具/tests/test_optimize_art_assets.py `
  tools/美术工具/tests/test_art_processing.py -v
```

Expected: all tests pass; automatic optimizer behavior remains compatible.

- [ ] **Step 9: Commit Task 2**

```powershell
git add -- `
  tools/美术工具/art_background.py `
  tools/美术工具/register_art_processing_round.py `
  tools/美术工具/Register-ArtProcessingRound.ps1 `
  tools/美术工具/tests/test_art_background.py `
  tools/美术工具/tests/test_register_art_processing_round.py
git commit -m "fix: enforce art processing technical gates"
```

---

### Task 3: Guarded Selected Replacement

**Files:**
- Modify: `tools/美术工具/select_art_candidate.py`
- Test: `tools/美术工具/tests/test_select_art_candidate.py`

**Interfaces:**
- Consumes: existing visual review item; replacement adds `SelectionMode`, `CandidateSHA256`, `ReviewRubricVersion`, `ReplacementBaseline`, `ReplacementPolicy`.
- Produces: `already_selected`, strict replacement decision, `PreviousSelected`, `PolicyResult`.

- [ ] **Step 1: Write failing replacement tests**

Add fixture support for old selected bytes and a replacement review. Cover:

```python
def test_replacement_requires_strictly_better_same_run_baseline(self) -> None:
    self.create_selected(score=90)
    self.write_replacement_review(candidate_score=90, baseline_score=90)
    with self.assertRaisesRegex(ValueError, "replacement_not_better"):
        self.select(allow_selected_overwrite=True)
```

Also test candidate 92 vs baseline 90 succeeds, protected dimension regression fails, baseline SHA change fails, missing baseline fails, and identical source/target SHA returns `already_selected` without copy.

- [ ] **Step 2: Run focused tests and verify RED**

```powershell
python -m unittest tools/美术工具/tests/test_select_art_candidate.py -v
```

- [ ] **Step 3: Add typed score and replacement validation helpers**

Implement:

```python
def score_map(value: Any, *, label: str) -> dict[str, int]:
    if not isinstance(value, dict):
        raise ValueError(f"{label}_scores_missing")
    result = {str(key): int(score) for key, score in value.items()}
    if "Total" not in result:
        raise ValueError(f"{label}_total_missing")
    return result

def validate_replacement(item: dict[str, Any], target: Path, target_hash: str) -> dict[str, Any]:
    baseline = item.get("ReplacementBaseline")
    policy = item.get("ReplacementPolicy")
    # validate selected path/hash, delta and protected dimensions
```

Rules:

- target exists with different SHA requires `SelectionMode=replacement`;
- baseline `SelectedPath` resolves to the same target;
- baseline SHA equals execution-time target SHA;
- candidate SHA equals latest processed candidate SHA;
- `new_total >= max(MinimumScore, old_total + MinimumScoreDelta)`;
- each named protected dimension exists in both maps and new >= old.

- [ ] **Step 4: Preserve initial selection and idempotency**

Before overwrite authorization:

```python
if target.exists() and target_hash == source_hash:
    return {
        "State": "already_selected",
        "VisualID": visual_id,
        "Candidate": repo_path(reviewed_candidate, project_root),
        "SelectedPath": repo_path(target, project_root),
        "SHA256": source_hash,
        "ApprovedChanged": False,
    }
```

Only a genuinely different replacement requires both quality validation and `allow_selected_overwrite`.

- [ ] **Step 5: Record complete replacement evidence**

Add to decision and `selection-decision.json`:

```python
"SelectionMode": selection_mode,
"PreviousSelected": previous_selected,
"PolicyResult": policy_result,
```

Keep `ApprovedChanged=False` and preserve Manifest main status for approved/registered/validated assets.

- [ ] **Step 6: Run selector and production batch tests**

```powershell
python -m unittest `
  tools/美术工具/tests/test_select_art_candidate.py `
  tools/美术工具/tests/test_run_art_production_batch.py -v
```

- [ ] **Step 7: Commit Task 3**

```powershell
git add -- tools/美术工具/select_art_candidate.py tools/美术工具/tests/test_select_art_candidate.py
git commit -m "feat: guard selected art replacements"
```

---

### Task 4: Character Portrait Run State and Resume

**Files:**
- Modify: `tools/美术工具/Run-CharacterPortraitSet.ps1`
- Modify: `tools/美术工具/run_character_portrait_set.py`
- Test: `tools/美术工具/tests/test_run_character_portrait_set.py`

**Interfaces:**
- New CLI: `--execution-mode interactive|automatic`, `--resume`, `--processing-decisions PATH`, `--visual-review PATH`, `--allow-selected-overwrite`, repeatable `--allow-technical-override`.
- Produces: `portrait-set-run.json`, per-item immutable generation snapshot, truthful `summary.json`.

- [ ] **Step 1: Write failing run-state and Resume tests**

Use mocked `subprocess.run` and temporary manifests to cover:

```python
def test_resume_skips_generation_when_snapshot_is_current(self) -> None:
    state = self.create_generated_run_state()
    completed = resume_portrait_set_run(...)
    self.assertEqual(completed["Items"][0]["Stage"], "processing_decision")
    self.assertEqual(self.subprocess_calls, [])
```

Also cover:

- existing run without `--resume` fails;
- Resume with changed PromptRevision fingerprint marks `prompt_stale` and does not reuse generation;
- changed reference SHA marks item and explicit dependents `reference_stale`;
- processed round evidence prevents duplicate registration;
- selected evidence prevents duplicate selection;
- one independent difference failure does not block unrelated member;
- master failure blocks explicit downstream member;
- summary never reports full completion while any item is pending/failed.

- [ ] **Step 2: Run focused tests and verify RED**

```powershell
python -m unittest tools/美术工具/tests/test_run_character_portrait_set.py -v
```

- [ ] **Step 3: Add run-state schema and atomic persistence**

Inside `run_character_portrait_set.py`, add:

```python
RUN_SCHEMA = "portrait_set_run_v1"
ITEM_STAGES = {
    "source_audit",
    "prompt_resolution",
    "generation",
    "processing_decision",
    "technical_gate",
    "processed_registration",
    "visual_review",
    "guarded_selection",
    "complete",
}

def write_json_atomic(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(f".{path.name}.tmp")
    temporary.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    temporary.replace(path)
```

Initialize `portrait-set-run.json` from the existing plan. Store Requirement/Revision/reference fingerprints in each item; do not copy lifecycle states into the run file.

- [ ] **Step 4: Snapshot generation evidence per item**

After each successful child generation:

1. Load workspace `generation.json`.
2. Validate BatchID, PromptRevisionID, PromptFormat, reference SHA and at least one decodable output.
3. Copy the JSON payload to `run_dir/generation/<VisualID>.json` with atomic write.
4. Record its SHA and output SHA list in run state.
5. Advance to `processing_decision`.

Resume validates the snapshot before skipping provider execution. A stale snapshot starts a new attempt without deleting old raw.

- [ ] **Step 5: Consume explicit processing decisions**

Define decision input:

```json
{
  "ProductionRunID": "portrait-zero-001",
  "Items": [
    {
      "VisualID": "doll_zero_cold",
      "Action": "background_processing_required",
      "InputPath": ".../raw/file.png",
      "InputSHA256": "...",
      "Method": "explicit_mask",
      "MaskPath": ".../mask.png"
    }
  ]
}
```

Supported actions are exactly `already_usable`, `background_processing_required`, `manual_edit_required`, and `regenerate_required`. For the first two, call existing `prepare_art_background_candidate.py` as needed, ensure staging has canonical process/technical/decision evidence, then call `register_art_processing_round.py`. The Registrar remains the only publisher of Agent-produced numeric rounds.

- [ ] **Step 6: Consume visual review and guarded selection**

When `--visual-review` is supplied, validate that its Candidate SHA belongs to the latest registered round, then call existing `select_art_candidate.py`. Propagate `--allow-selected-overwrite`; replacement quality remains enforced by Task 3.

Interactive mode with missing decision/review writes `PendingDecision` and exits with code 2. Automatic mode may continue only when the current Agent has already provided valid decision/review evidence; it never fabricates an empty review.

- [ ] **Step 7: Add dependency-aware summary**

Map final item results to:

```text
selected
kept_existing
review_required
generation_failed
technical_failed
blocked_by_dependency
```

Compute set `FinalState=selection_complete` only when all selected members are `selected` or `kept_existing`; otherwise use `selection_in_progress` or `selection_complete_with_failures`.

- [ ] **Step 8: Expose wrapper parameters**

Add PowerShell parameters and serialize them without changing existing defaults:

```powershell
[ValidateSet("Interactive", "Automatic")][string]$ExecutionMode = "Interactive",
[switch]$Resume,
[string]$ProcessingDecisions = "",
[string]$VisualReview = "",
[switch]$AllowSelectedOverwrite,
[string[]]$AllowTechnicalOverride = @()
```

- [ ] **Step 9: Run portrait, background, Registrar and selector suites**

```powershell
python -m unittest `
  tools/美术工具/tests/test_run_character_portrait_set.py `
  tools/美术工具/tests/test_prepare_art_background_candidate.py `
  tools/美术工具/tests/test_register_art_processing_round.py `
  tools/美术工具/tests/test_select_art_candidate.py -v
```

- [ ] **Step 10: Commit Task 4**

```powershell
git add -- `
  tools/美术工具/Run-CharacterPortraitSet.ps1 `
  tools/美术工具/run_character_portrait_set.py `
  tools/美术工具/tests/test_run_character_portrait_set.py
git commit -m "feat: resume character portrait production"
```

---

### Task 5: Skill, Facts, Full Regression and Pilots

**Files:**
- Modify: `tools/美术工具/README.md`
- Modify: `.codex/skills/p3-art-asset-production/SKILL.md`
- Modify: `.codex/skills/p3-art-asset-production/references/state-machine.md`
- Modify: `.codex/skills/p3-art-asset-production/references/candidate-evaluation.md`
- Modify: `.codex/skills/p3-art-asset-production/references/interaction-gates.md`
- Modify: `.codex/skills/p3-art-asset-production/references/evidence-and-writeback.md`
- Modify: `.codex/skills/p3-generate-image/SKILL.md`
- Modify: `美术文档/00_美术流水线总览.md`
- Modify: `美术文档/01_Manifest规范.md`
- Modify: `agent_status/art.md`
- Regenerate: `DOCS_INDEX.md`, `docs_index.json`

**Interfaces:**
- Documents the exact implemented CLI/schema only; no aspirational fields absent from code.
- Does not change PROJECT_STATUS unless implementation creates a new cross-role handoff or blocker.

- [ ] **Step 1: Update tools and Skill contracts**

Document:

- formal v2 vs `legacy_unverified` evidence;
- Registrar recomputation and explicit override authorization;
- selected replacement baseline and strict delta;
- `Run-CharacterPortraitSet -Resume` stages and decision inputs;
- Interactive/Automatic pause behavior;
- claim ceiling remains selected for production pilots.

- [ ] **Step 2: Run the complete art tooling suite**

```powershell
python -m unittest discover -s tools/美术工具/tests -p "test_*.py" -v
```

Expected: zero failures and zero errors.

- [ ] **Step 3: Run Request Catalog and document validation**

```powershell
python tools/美术工具/validate_art_generation_requests.py `
  --manifest 美术文档/_generated/art_manifest.json `
  --request-catalog 美术文档/_generated/art_generation_requests.json `
  --strict
.\tools\docs\Generate-DocsIndex.ps1
.\tools\docs\Validate-Docs.ps1
```

- [ ] **Step 4: Run dry-run regression for both routes**

Use one current standard asset plan and `zero_dialogue_portrait_v1` with an existing ready member. Dry-run must prove:

- standard route resolves exact PromptRevision and does not accept portrait profile;
- portrait route resolves exact references and PromptRevision;
- `-Resume` on synthetic/fake-provider evidence performs no duplicate generation or numeric round publication.

- [ ] **Step 5: Run bounded real pilots only if providers remain available**

Create one new ProductionRunID for a standard asset and one character difference. Stop at selected. If a real provider is unavailable, record `validation_limited:provider_unavailable` and retain fake-provider/integration evidence; do not claim live generation.

- [ ] **Step 6: Update status and regenerate indexes**

Update `agent_status/art.md` with exact tests, Pilot claim ceiling, remaining Approved boundary and any limited validation. Regenerate indexes through the project scripts; do not hand-edit generated files.

- [ ] **Step 7: Run strict health check**

```powershell
.\tools\agent\Invoke-AgentHealthCheck.ps1 -Strict
```

If strict mode reports only the intended staged task files, confirm the exact staged list before committing. Any docs, submodule or unrelated-file error must be resolved or reported before completion.

- [ ] **Step 8: Commit Task 5**

```powershell
git add -- `
  tools/美术工具/README.md `
  .codex/skills/p3-art-asset-production `
  .codex/skills/p3-generate-image/SKILL.md `
  美术文档/00_美术流水线总览.md `
  美术文档/01_Manifest规范.md `
  agent_status/art.md `
  DOCS_INDEX.md docs_index.json
git commit -m "docs: document hardened art production workflows"
```

## Final Verification

Run fresh after all commits:

```powershell
python -m unittest discover -s tools/美术工具/tests -p "test_*.py" -v
python tools/美术工具/validate_art_generation_requests.py `
  --manifest 美术文档/_generated/art_manifest.json `
  --request-catalog 美术文档/_generated/art_generation_requests.json `
  --strict
.\tools\docs\Validate-Docs.ps1
.\tools\agent\Invoke-AgentHealthCheck.ps1 -Strict
git status --short
```

Completion requires a clean worktree, no unrelated commit content, and evidence that both existing route entrypoints remain compatible.
