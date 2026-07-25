# Art Runtime Validated State Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task with verification checkpoints.

**Goal:** Implement the approved public state transition `registered -> runtime_validated` inside the existing P3 art validation flow, with binding, display, console, and Agent review retained as internal ArtRun checks.

**Architecture:** Extend the existing `P3.Validation.ArtValidationOrchestrator` session rather than adding a second runtime status store. Live inspection derives actual Sprite path/GUID/VisualID from registered UGUI nodes; a new explicit review/finalize action validates all checks and writes one ArtRun runtime-validation evidence file with `claim=runtime_validated`. No Manifest runtime status or automatic code/Prefab mutation is added.

**Tech Stack:** Unity 2022.3 C# Editor MCP tools, Newtonsoft.Json, existing P3ValidationCore ArtRun services, PowerShell validation scripts, Unity Editor smoke tests.

## Global Constraints

- Public art state is `registered -> runtime_validated -> player_path_verified -> regression_passed`; do not expose `runtime_bound`.
- `runtime_bound` is not added to Manifest, RegistryStatus, or any manual checkbox.
- Runtime validation requires a registered TargetID, a non-empty `required_visual_ids` contract, live binding evidence, final capture, no blocking issues, and explicit Agent review approval.
- Binding checks never modify code, Prefabs, config source, domain rules, or runtime assets.
- `runtime_validated` is target-scoped; aggregation across VisualID/TargetID requires explicit required-target facts.
- Do not enable or invoke legacy ArtAcceptance except through the existing `art_regression` profile.
- Preserve all unrelated worktree changes and never modify `tools/ai-image-gateway`.

---

### Task 1: Extend ArtRun contracts and finalization state

**Files:**
- Modify: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtInspectionContracts.cs`
- Modify: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtValidationOrchestrator.cs`
- Modify: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtRunProfileTool.cs`
- Test: `UnityClient/Assets/Scripts/Editor/Tests/P3ArtRuntimeValidationSmokeTest.cs`

**Interfaces:**
- Add `ArtBindingEvidence` with `VisualId`, `ComponentPath`, `AssetPath`, `AssetGuid`, `Status`, and `Observation`.
- Add `ArtValidationReview` with `Decision`, `Reviewer`, `ObservedAt`, and `Notes`.
- Extend `ArtInspectionResult` with `BindingStatus` and `Bindings`.
- Extend `ArtValidationSession` with `BindingStatus`, `HasAgentReview`, `AgentReviewPassed`, `Claim`, and `RuntimeValidationEvidencePath`.
- Add `ArtValidationOrchestrator.RecordReview(string run, ArtValidationReview review)`.
- Add `ArtValidationOrchestrator.FinalizeRuntimeValidated(string run)`.
- Add MCP actions `record_review` and `finalize_runtime_validated` to `p3_art_run_profile`.

- [ ] **Step 1: Write the failing Unity smoke test**

The smoke test must create an `art_focus` session, record an inspection with a passed binding and no blocking issue, prove finalization fails before review, record an approved review, finalize successfully, and assert `Claim == "runtime_validated"` plus the evidence file exists. Add a second case where `BindingStatus="failed"` and assert `art_blocked:runtime_binding_missing`.

- [ ] **Step 2: Run the focused Unity test and confirm it fails**

Run the existing Unity MCP editor test runner for `P3ArtRuntimeValidationSmokeTest.Run`. Expected failure: missing review/finalization APIs or missing claim behavior.

- [ ] **Step 3: Implement the minimal contracts and session fields**

Keep all new fields serializable with the existing Newtonsoft.Json session format. Do not rename existing fields used by iteration/regression profiles.

- [ ] **Step 4: Implement review and finalization guards**

`FinalizeRuntimeValidated` must reject, in order:

```text
unknown run
non-art-regression profile
inspection missing
binding status != passed
blocking issue present
required final/seal/after capture missing
Agent review missing or decision != passed
```

On success write `runtime-validation.json` under the existing ArtRun root with `claim=runtime_validated`, checks, target IDs, VisualIDs, and binding evidence. Set session `Claim` and `RuntimeValidationEvidencePath`; do not modify Manifest.

- [ ] **Step 5: Add MCP actions**

`record_review` converts the JSON payload to `ArtValidationReview` and persists it. `finalize_runtime_validated` calls the finalizer and returns the session/evidence path. Keep the existing `complete` action behavior for legacy profile state-machine compatibility.

- [ ] **Step 6: Run focused smoke and C# compile validation**

Expected: the new smoke passes and existing `P3ArtProfileStateMachineSmokeTest` and `P3ArtIterationV2SmokeTest` remain green.

### Task 2: Make live inspection derive binding evidence

**Files:**
- Modify: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtInspectionContracts.cs`
- Modify: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtLiveInspectionService.cs`
- Modify: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtTargetContracts.cs`
- Modify: `UnityClient/Assets/Editor/P3Validation/art_validation_targets.json`
- Test: `UnityClient/Assets/Scripts/Editor/Tests/P3ArtRuntimeValidationSmokeTest.cs`
- Test: `tools/agent/p3-validation-core/Test-P3ValidationCore.ps1`

**Interfaces:**
- Extend `ArtUiNodeSnapshot` with `VisualId`, `SpriteAssetPath`, and `SpriteGuid`.
- Extend `ArtTargetDefinition` with the existing `required_visual_ids` contract usage; no new target-level status field.
- Add a pure helper in `ArtLiveInspectionService` that resolves binding evidence from nodes and required VisualIDs.

- [ ] **Step 1: Add failing binding-resolution assertions**

Use a pure helper test path or test-only node records to assert:

```text
required VisualID present with Approved path/GUID -> passed
required VisualID absent -> failed
required list empty -> not_configured, never eligible for runtime_validated
_IncomingAI path -> failed
duplicate required VisualID matches -> failed
```

- [ ] **Step 2: Implement live node asset identity**

For each `Image` node, use `AssetDatabase.GetAssetPath(image.sprite)` and `AssetDatabase.AssetPathToGUID`. Derive `VisualId` from the filename stem only after confirming the path is under `Assets/Art/Approved/`; never infer runtime identity from `_IncomingAI` or an arbitrary sprite name.

- [ ] **Step 3: Implement required VisualID matching**

For each `required_visual_ids` value, emit exactly one binding record. A valid record requires a live node match, Approved asset path, non-empty GUID, and no duplicate matches. Set `BindingStatus` to `passed`, `failed`, or `not_configured`; `not_configured` is a blocking issue for runtime finalization but remains compatible with ordinary diagnosis.

- [ ] **Step 4: Add a neutral pilot target contract without claiming runtime completion**

Add a dedicated target entry for the future neutral dialogue target only if an existing registered production root/component is available. If no production target exists, leave `required_visual_ids` empty and ensure the validator reports `art_blocked:runtime_binding_contract_missing`; do not create a fake target or bind the asset automatically.

- [ ] **Step 5: Run profile/schema validation and focused tests**

Run `tools/agent/p3-validation-core/Test-P3ValidationCore.ps1` and the Unity editor smoke suite. Expected: target/profile schema passes; existing diagnosis profiles remain usable; runtime finalization cannot pass with an empty binding contract.

### Task 3: Update art validation documentation and generated routing

**Files:**
- Modify: `.codex/skills/p3-art-validation/SKILL.md`
- Modify: `.codex/skills/p3-art-validation/references/evidence-policy.md`
- Modify: `.codex/skills/p3-art-validation/references/mcp-live-inspection.md`
- Modify: `美术文档/00_美术流水线总览.md`
- Modify: `美术文档/01_Manifest规范.md`
- Modify: `美术文档/02_资源规格与接入规范.md`
- Modify: `开发文档/rules/03_视觉资源系统程序开发规范.md`
- Modify: `agent_status/art.md`
- Modify: `agent_status/program.md`

**Interfaces:**
- Document `registered -> runtime_validated` and the internal checks.
- Document `record_review` / `finalize_runtime_validated` MCP actions and failure routing.
- Keep `player_path_verified` and `regression_passed` separate.

- [ ] **Step 1: Update skill state and evidence rules**

Remove any instruction that treats `runtime_bound` as a user-facing state. State that binding evidence is internal to ArtRun and final claim is written only after Agent review and final capture.

- [ ] **Step 2: Update program contract**

Clarify that `program_integrate=0` means no Registry registration gap, while `runtime_validated` requires a real runtime target and art validation evidence. Program remains responsible for code/Prefab/config binding.

- [ ] **Step 3: Update status pages and pipeline overview**

Record the new state model and implementation status; do not claim the neutral pilot is runtime validated before a real production TargetID exists and passes.

- [ ] **Step 4: Regenerate and validate docs**

Run `Generate-DocsIndex.ps1` and `Validate-Docs.ps1`; inspect generated diffs and do not stage unrelated pre-existing changes.

### Task 4: End-to-end verification and handoff

**Files:**
- Test: `UnityClient/Assets/Scripts/Editor/Tests/P3ArtRuntimeValidationSmokeTest.cs`
- Test: `tools/agent/p3-validation-core/Test-P3ValidationCore.ps1`
- Evidence: one bounded ArtRun fixture under Unity validation evidence, only if a real target is available.

- [ ] **Step 1: Run Python and PowerShell regression checks**

Run the existing art-processing/Approved/Registry tests, `Test-P3ValidationCore.ps1`, Python compile, PowerShell parser checks, and `git diff --check`.

- [ ] **Step 2: Run Unity compile and smoke checks**

Confirm the new runtime smoke, existing art state-machine smoke, iteration smoke, and Registry smoke compile and pass. Do not run full ArtAcceptance or P0 as part of this feature.

- [ ] **Step 3: Perform live MCP pilot only when a production TargetID exists**

Use `p3_validation_readiness -> p3_art_open_target -> p3_art_inspect_target -> capture -> record_review -> finalize_runtime_validated`. If the target is not reachable or not configured, record the bounded failure and do not claim `runtime_validated`.

- [ ] **Step 4: Final evidence and worktree review**

Verify no `runtime_bound` public field was added, `runtime-validation.json` is schema-valid, docs validate, `scripting_ext` is restored off, and only task files are staged.
