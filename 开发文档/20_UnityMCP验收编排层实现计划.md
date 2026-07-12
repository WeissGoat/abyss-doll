---
id: dev_20_unity_mcp_validation_implementation_plan
title: P3 程序与美术分离验收实现计划
type: dev
role: 程序
domain: test_automation
status: active
source_of_truth: true
related:
  - 开发文档/README.md
  - 开发文档/00_程序开发大纲.md
  - 开发文档/19_UnityMCP验收编排层设计.md
  - 开发文档/15_P0配置Validator与自动验收底座需求.md
  - 开发文档/14_Unity运行时美术自动验收方案.md
  - 开发文档/rules/04_自动化测试与验收流程规范.md
  - tools/agent/README.md
  - 知识库/views/program.md
last_verified: 2026-07-12
update_rule: 调整程序验收、美术迭代验收、发布聚合、文件边界、测试门禁或提交顺序时同步本文件。
---

# P3 Program and Art Validation Split Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace the unused mixed `p3-validation` entry with independent program automation, art iteration/acceptance, and release aggregation workflows sharing one domain-aware validation core.

**Architecture:** `P3ValidationCore` owns RunID paths, Editor readiness, Console deltas, JobState, artifact copying, `step-result@2`, and deterministic merging. `p3-program-validation` may run static validators and registered Smoke sets but never ArtAcceptance; `p3-art-validation` may inspect and modify allowlisted UGUI/VisualID presentation but never domain rules; `p3-release-validation` only combines completed ProgramRunID and ArtRunID evidence.

**Tech Stack:** Unity 2022.3.60f1, C# Editor scripts, MCP for Unity v10.0.0, Newtonsoft.Json, PowerShell 5.1+, pure UGUI, existing AutoTestDaemon / ArtAcceptanceRunner / T0ValidationFinalCaptureRunner.

## Global Constraints

- Runtime UI remains pure UGUI; do not introduce UI Toolkit, UXML, USS or `UIDocument`.
- Delete the old mixed Skill and Profiles directly; there is no compatibility or historical evidence migration requirement.
- Program validation must never start ArtAcceptance or claim visual completion.
- Art validation must never run full P0, modify domain rules, fabricate player state or approve AI assets.
- Art iteration may modify only registered UGUI presentation fields and Approved VisualID bindings.
- No project MCP tool may execute arbitrary C#, arbitrary reflection names, arbitrary menus, arbitrary filesystem paths or external processes.
- ProgramRunID, ArtRunID and ReleaseRunID use separate roots and reject cross-domain step results.
- Machine success never bypasses Owner or external review requirements.
- Preserve unrelated dirty-worktree changes and stage only task-owned files.

---

## Target File Structure

### Shared core

- `UnityClient/Assets/Editor/P3Validation/program_validation_profiles.json`
- `UnityClient/Assets/Editor/P3Validation/art_validation_profiles.json`
- `UnityClient/Assets/Editor/P3Validation/release_validation_profiles.json`
- `UnityClient/Assets/Scripts/Editor/P3ValidationCore/`
- `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/`
- `tools/agent/p3-validation-core/`

### Skills

- `.codex/skills/p3-program-validation/`
- `.codex/skills/p3-art-validation/`
- `.codex/skills/p3-release-validation/`

### Evidence roots

```text
UnityClient/Logs/P3Validation/program-runs/<ProgramRunID>/
UnityClient/Logs/P3Validation/art-runs/<ArtRunID>/
UnityClient/Logs/P3Validation/release-runs/<ReleaseRunID>/
```

---

### Task 1: Freeze Domain-Aware Profiles and `step-result@2`

**Files:**
- Create: `UnityClient/Assets/Editor/P3Validation/program_validation_profiles.json`
- Create: `UnityClient/Assets/Editor/P3Validation/art_validation_profiles.json`
- Create: `UnityClient/Assets/Editor/P3Validation/release_validation_profiles.json`
- Create: `tools/agent/p3-validation-core/schemas/step-result.schema.json`
- Create: `tools/agent/p3-validation-core/schemas/validation-summary.schema.json`
- Create: `tools/agent/p3-validation-core/fixtures/`
- Create: `tools/agent/p3-validation-core/Test-P3ValidationCore.ps1`

**Interfaces:**
- Produces program Profiles: `smoke_focus`, `t0_functional`, `p0_full`.
- Produces art Profiles: `art_focus`, `art_runtime`, `art_iteration`, `t0_art_seal`.
- Produces release Profiles: `vertical_slice_release`, `t0_release`.
- Produces domains: `program`, `art`, `release`, `infrastructure`.

- [ ] **Step 1: Write the failing contract test**

```powershell
$program = Get-Content $programProfilePath -Raw | ConvertFrom-Json
$art = Get-Content $artProfilePath -Raw | ConvertFrom-Json
if ($program.profiles.id -contains "art_runtime") { throw "art profile leaked into program registry" }
if ($art.profiles.id -contains "p0_full") { throw "program profile leaked into art registry" }
foreach ($fixture in Get-ChildItem $fixtureRoot -Filter *.json) {
    $data = Get-Content $fixture.FullName -Raw | ConvertFrom-Json
    if ($data.schema_version -ne "p3-validation/step-result@2") { throw "wrong schema" }
    if ($data.validation_domain -notin @("program","art","release","infrastructure")) { throw "wrong domain" }
}
```

- [ ] **Step 2: Run the test and verify it fails because the split registries do not exist**

Run: `./tools/agent/p3-validation-core/Test-P3ValidationCore.ps1`

Expected: non-zero exit with `program_validation_profiles.json` missing.

- [ ] **Step 3: Create the three registries**

Program Profile example:

```json
{
  "id": "p0_full",
  "version": "2",
  "validation_domain": "program",
  "static_steps": ["config_sync", "config_static_validate", "ui_spec_validate"],
  "smoke_set": "p0_core",
  "required_steps": ["config_sync", "config_static_validate", "ui_spec_validate", "compile_gate", "smoke", "console_delta"],
  "external_review_required": []
}
```

Art Profile example:

```json
{
  "id": "art_iteration",
  "version": "2",
  "validation_domain": "art",
  "mode": "iteration",
  "required_steps": ["readiness", "before_capture", "diagnosis", "allowlisted_changes", "after_capture", "console_delta"],
  "external_review_required": ["art"]
}
```

- [ ] **Step 4: Add success, failure, blocked, cross-domain and review-required fixtures**

Every artifact fixture must contain `path`, `source_path`, `sha256`, `size`, `captured_at` and `mime_type`.

- [ ] **Step 5: Run the contract test**

Expected: `[p3-validation-core] profiles and schemas passed`.

- [ ] **Step 6: Commit**

```powershell
git add -- UnityClient/Assets/Editor/P3Validation tools/agent/p3-validation-core
git commit -m "test: freeze split P3 validation contracts"
```

---

### Task 2: Refactor Shared C# Contracts, Paths and Profile Registry

**Files:**
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/P3ValidationDomain.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/P3ValidationContracts.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/P3ValidationProfileRegistry.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/P3ValidationEvidencePaths.cs`
- Test: `UnityClient/Assets/Scripts/Editor/Tests/P3ValidationCoreContractsSmokeTest.cs`

**Interfaces:**
- Produces `P3ValidationDomain.Program|Art|Release|Infrastructure`.
- Produces `P3ValidationProfileRegistry.GetProgram/GetArt/GetRelease`.
- Produces `P3ValidationEvidencePaths.ForProgramRun/ForArtRun/ForReleaseRun`.

- [ ] **Step 1: Write failing domain-isolation tests**

```csharp
var program = P3ValidationProfileRegistry.GetProgram("p0_full");
if (program.ValidationDomain != P3ValidationDomain.Program) throw new Exception("wrong domain");
AssertThrows(() => P3ValidationProfileRegistry.GetProgram("art_runtime"));
AssertThrows(() => P3ValidationEvidencePaths.ForProgramRun("20260712_art_runtime"));
```

- [ ] **Step 2: Run `P3ValidationCoreContractsSmokeTest.Run` and confirm compilation failure**

- [ ] **Step 3: Implement the domain enum and v2 contract**

```csharp
[JsonConverter(typeof(StringEnumConverter))]
public enum P3ValidationDomain { Program, Art, Release, Infrastructure }

public sealed class P3ValidationStepResult {
    [JsonProperty("schema_version")] public string SchemaVersion = "p3-validation/step-result@2";
    [JsonProperty("validation_domain")] public P3ValidationDomain ValidationDomain;
    [JsonProperty("run_id")] public string RunId;
    [JsonProperty("profile_id")] public string ProfileId;
    [JsonProperty("step_id")] public string StepId;
    [JsonProperty("required")] public bool Required;
    [JsonProperty("status")] public P3ValidationStepStatus Status;
}
```

- [ ] **Step 4: Implement separate registries and RunID roots**

Reject path separators, `..`, and a RunID prefix that does not match its requested domain.

- [ ] **Step 5: Run focused Unity smoke and Editor build**

Run:

```powershell
Set-Content UnityClient/Logs/.test_trigger "P3ValidationCoreContractsSmokeTest.Run"
dotnet build UnityClient/Assembly-CSharp-Editor.csproj --no-restore
```

Expected: smoke `PASSED`; build 0 errors.

- [ ] **Step 6: Commit**

```powershell
git add -- UnityClient/Assets/Scripts/Editor/P3ValidationCore UnityClient/Assets/Scripts/Editor/Tests/P3ValidationCoreContractsSmokeTest.cs*
git commit -m "refactor: split P3 validation core domains"
```

---

### Task 3: Implement Domain-Aware Run Creation and Merger

**Files:**
- Create: `tools/agent/p3-validation-core/New-P3ValidationRun.ps1`
- Create: `tools/agent/p3-validation-core/Merge-P3ValidationEvidence.ps1`
- Modify: `tools/agent/p3-validation-core/Test-P3ValidationCore.ps1`

**Interfaces:**
- Produces `New-P3ValidationRun -Domain program|art|release -ProfileId`.
- Produces summaries with domain-specific status fields.

- [ ] **Step 1: Add failing cross-domain and required-step tests**

```powershell
$programRun = & $newRun -Domain program -ProfileId p0_full -PassThru
Copy-Item $artFixture "$($programRun.EvidenceRoot)/steps/art/result.json"
{ & $merge -RunId $programRun.RunId -Domain program } | Should -Throw "cross-domain"
```

Also assert that Profile `required_steps` comes from JSON and is not duplicated in the merger.

- [ ] **Step 2: Implement domain roots and atomic request writes**

Map domain to `program-runs`, `art-runs`, or `release-runs`; reject completed duplicate RunIDs.

- [ ] **Step 3: Implement deterministic merge from `required_steps`**

```powershell
if ($required.status -contains "Failed") { $automation = "Failed" }
elseif ($missing.Count -gt 0 -or $required.status -contains "Blocked") { $automation = "Blocked" }
elseif ($required.status -contains "Limited") { $automation = "Limited" }
elseif ($required.status -contains "Cancelled") { $automation = "Cancelled" }
else { $automation = "Passed" }
```

Infer art external review from `external_review_required`; never default an art Profile to `NotRequired`.

- [ ] **Step 4: Validate artifact existence, size and SHA-256 before counting required evidence**

- [ ] **Step 5: Run all PowerShell tests**

Expected: cross-domain rejection, missing step, hash mismatch and review inference cases pass.

- [ ] **Step 6: Commit**

```powershell
git add -- tools/agent/p3-validation-core
git commit -m "feat: add domain-aware P3 validation merger"
```

---

### Task 4: Complete Shared Unity Readiness, Console and Job Infrastructure

**Files:**
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/P3ValidationEditorSnapshot.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/P3ValidationConsoleTracker.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/P3ValidationJobStore.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/P3ValidationInstanceLock.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ValidationReadinessTool.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ValidationCollectConsoleTool.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ValidationCollectEvidenceTool.cs`
- Test: `UnityClient/Assets/Scripts/Editor/Tests/P3ValidationInfrastructureSmokeTest.cs`

**Interfaces:**
- MCP: `p3_validation_readiness`, `p3_validation_collect_console`, `p3_validation_collect_evidence`.
- Job lock includes `validation_domain` and RunID.

- [ ] **Step 1: Write failing tests for dirty Scene, Console delta, lock collision and stale recovery**

- [ ] **Step 2: Capture PlayMode, pause, active Scene, selection, Prefab Stage and dirty state**

- [ ] **Step 3: Persist Console baseline/delta without clearing Console**

Fingerprint: `log_type + condition + stack_trace + occurrence_index`.

- [ ] **Step 4: Persist domain-aware JobState through `McpJobStateStore`**

- [ ] **Step 5: Run Unity infrastructure suite and build**

Expected: no Editor mutation from readiness; second RunID is blocked; Console delta excludes pre-baseline entries.

- [ ] **Step 6: Commit**

```powershell
git add -- UnityClient/Assets/Scripts/Editor/P3ValidationCore UnityClient/Assets/Scripts/Editor/Tests/P3ValidationInfrastructureSmokeTest.cs*
git commit -m "feat: add shared P3 validation infrastructure"
```

---

### Task 5: Build the Program Validation Lane

**Files:**
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Program/P3ProgramValidationOrchestrator.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Program/P3ProgramSmokeRegistry.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ProgramRunSmokeTool.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ProgramRunProfileTool.cs`
- Create: `tools/agent/p3-validation-core/Invoke-P3ProgramStaticValidation.ps1`
- Test: `UnityClient/Assets/Scripts/Editor/Tests/P3ProgramValidationSmokeTest.cs`

**Interfaces:**
- MCP: `p3_program_run_smoke`, `p3_program_run_profile`.
- Produces only `program` and `infrastructure` step results.

- [ ] **Step 1: Write a failing test that records whether `ArtAcceptanceRunner.BeginAutomatedRun` was invoked**

Assert it remains false for all three program Profiles.

- [ ] **Step 2: Implement allowlisted Smoke sets and compile gate**

No raw method names are accepted from MCP parameters.

- [ ] **Step 3: Implement static dispatch with a PowerShell `switch`**

Registered steps only: `config_sync`, `config_static_validate`, `ui_spec_validate`.

- [ ] **Step 4: Implement polling orchestration**

State sequence: `Preflight -> Compile -> Smoke -> Console -> Complete`.

- [ ] **Step 5: Run `smoke_focus`, `t0_functional`, and `p0_full` success/failure fixtures**

Expected: no screenshot requirement and no `ArtExternalReview` field.

- [ ] **Step 6: Commit**

```powershell
git add -- UnityClient/Assets/Scripts/Editor/P3ValidationCore/Program UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3Program* UnityClient/Assets/Scripts/Editor/Tests/P3ProgramValidationSmokeTest.cs* tools/agent/p3-validation-core
git commit -m "feat: add P3 program validation lane"
```

---

### Task 6: Create the Program Validation Skill

**Files:**
- Create: `.codex/skills/p3-program-validation/SKILL.md`
- Create: `.codex/skills/p3-program-validation/agents/openai.yaml`
- Create: `.codex/skills/p3-program-validation/references/profile-routing.md`
- Create: `.codex/skills/p3-program-validation/references/claims.md`

**Interfaces:**
- Triggers on P3 compile, Smoke, functional T0, P0, program regression and code acceptance requests.

- [ ] **Step 1: Initialize with `skill-creator/scripts/init_skill.py`**

- [ ] **Step 2: Write the exact program workflow**

```text
read program facts
-> select program Profile
-> create ProgramRunID
-> run static lane
-> pin Unity
-> run p3_program_run_profile
-> merge program evidence
-> report ProgramClaimCeiling
```

- [ ] **Step 3: Add explicit prohibition of ArtAcceptance, visual seal and art review claims**

- [ ] **Step 4: Run `quick_validate.py` and placeholder/link checks**

- [ ] **Step 5: Commit**

```powershell
git add -- .codex/skills/p3-program-validation
git commit -m "feat: add P3 program validation skill"
```

---

### Task 7: Build Art Diagnosis and Capture Without Mutation

**Files:**
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtTargetRegistry.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtCaptureService.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtAcceptanceAdapter.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtOpenTargetTool.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtCaptureTool.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtRunAcceptanceTool.cs`
- Test: `UnityClient/Assets/Scripts/Editor/Tests/P3ArtDiagnosisSmokeTest.cs`

**Interfaces:**
- MCP: `p3_art_open_target`, `p3_art_capture`, `p3_art_run_acceptance`.
- Accepts registered target ID or ScreenTag, never arbitrary state methods.

- [ ] **Step 1: Write failing tests for unknown target, stale report, missing screenshot and unreachable screen**

- [ ] **Step 2: Implement target registry and read-only target navigation**

Return `art_blocked:target_screen_unreachable` with program handoff evidence when navigation fails.

- [ ] **Step 3: Copy report, required screenshots, UI snapshot and Registry snapshot into ArtRunID**

- [ ] **Step 4: Validate timestamp, size, dimensions and SHA-256**

- [ ] **Step 5: Run art diagnosis success and controlled-failure tests**

- [ ] **Step 6: Commit**

```powershell
git add -- UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3Art* UnityClient/Assets/Scripts/Editor/Tests/P3ArtDiagnosisSmokeTest.cs*
git commit -m "feat: add P3 art diagnosis and capture"
```

---

### Task 8: Add Allowlisted Art Iteration

**Files:**
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtIterationContracts.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtIterationService.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtIterationRegistry.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtCompareIterationTool.cs`
- Test: `UnityClient/Assets/Scripts/Editor/Tests/P3ArtIterationSmokeTest.cs`

**Interfaces:**
- MCP: `p3_art_compare_iteration`.
- Mutation request contains `target_id`, `action`, and typed values only.

- [ ] **Step 1: Write rejection tests for arbitrary property, asset path, C#, menu and non-Approved VisualID**

- [ ] **Step 2: Define the mutation enum**

```csharp
public enum P3ArtIterationAction {
    SetAnchoredPosition, SetSizeDelta, SetAnchorMin, SetAnchorMax,
    SetSiblingIndex, SetColor, SetAlpha, SetTextStyle,
    SetCanvasGroup, SetRaycastTarget, BindApprovedVisualId
}
```

- [ ] **Step 3: Implement registry-scoped target resolution and Approved VisualID validation**

- [ ] **Step 4: Write `before.png`, `diagnosis.json`, `changes.json`, `after.png`, Console delta and result atomically**

- [ ] **Step 5: Run a successful layout iteration and all rejection tests**

- [ ] **Step 6: Commit**

```powershell
git add -- UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtCompareIterationTool.cs* UnityClient/Assets/Scripts/Editor/Tests/P3ArtIterationSmokeTest.cs*
git commit -m "feat: add allowlisted P3 art iteration"
```

---

### Task 9: Build the Art Profile Orchestrator and Skill

**Files:**
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtValidationOrchestrator.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtRunProfileTool.cs`
- Create: `.codex/skills/p3-art-validation/SKILL.md`
- Create: `.codex/skills/p3-art-validation/agents/openai.yaml`
- Create: `.codex/skills/p3-art-validation/references/profile-routing.md`
- Create: `.codex/skills/p3-art-validation/references/iteration-boundaries.md`
- Test: `UnityClient/Assets/Scripts/Editor/Tests/P3ArtValidationProfileSmokeTest.cs`

**Interfaces:**
- MCP: `p3_art_run_profile`.
- Supports `art_focus`, `art_runtime`, `art_iteration`, `t0_art_seal`.

- [ ] **Step 1: Write state tests for diagnosis-only, acceptance, iteration and T0 seal modes**

- [ ] **Step 2: Implement `exclusive_restore` PlayMode and Editor restoration**

- [ ] **Step 3: Ensure every art Profile defaults external review to `Required`**

- [ ] **Step 4: Initialize and write the Skill with exact MCP/script sequence and program handoff rule**

- [ ] **Step 5: Validate Skill and run four Profile success/failure fixtures**

- [ ] **Step 6: Commit**

```powershell
git add -- UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtRunProfileTool.cs* UnityClient/Assets/Scripts/Editor/Tests/P3ArtValidationProfileSmokeTest.cs* .codex/skills/p3-art-validation
git commit -m "feat: add P3 art validation skill and profiles"
```

---

### Task 10: Implement Release-Only Evidence Aggregation

**Files:**
- Create: `tools/agent/p3-validation-core/Merge-P3ReleaseEvidence.ps1`
- Create: `.codex/skills/p3-release-validation/SKILL.md`
- Create: `.codex/skills/p3-release-validation/agents/openai.yaml`
- Create: `.codex/skills/p3-release-validation/references/release-profiles.md`
- Modify: `tools/agent/p3-validation-core/Test-P3ValidationCore.ps1`

**Interfaces:**
- Consumes completed `ProgramRunID` and `ArtRunID`.
- Produces `p3-validation/release-summary@1`.

- [ ] **Step 1: Add failing release matrix tests**

Cover Program/Art pass/fail, external review required, fingerprint mismatch and missing RunID.

- [ ] **Step 2: Implement read-only aggregation**

```powershell
if ($program.AutomationStatus -eq "Failed" -or $art.AutomationStatus -eq "Failed") { $release = "Failed" }
elseif ($fingerprintsMatch -eq $false) { $release = "Blocked" }
elseif ($program.AutomationStatus -eq "Blocked" -or $art.AutomationStatus -eq "Blocked") { $release = "Blocked" }
elseif ($program.AutomationStatus -eq "Limited" -or $art.AutomationStatus -eq "Limited") { $release = "Limited" }
elseif ($art.ExternalReview -ne "Passed") { $release = "ReviewRequired" }
else { $release = "Passed" }
```

- [ ] **Step 3: Reject any parameter that requests test execution or Unity mutation**

- [ ] **Step 4: Create and validate the release Skill**

- [ ] **Step 5: Run the complete release matrix**

- [ ] **Step 6: Commit**

```powershell
git add -- tools/agent/p3-validation-core .codex/skills/p3-release-validation
git commit -m "feat: add P3 release evidence aggregation"
```

---

### Task 11: Remove the Old Mixed Entry and Test Evidence

**Files:**
- Delete: `.codex/skills/p3-validation/`
- Delete: `UnityClient/Assets/Editor/P3Validation/p3_validation_profiles.json*`
- Delete: `tools/agent/p3-validation/`
- Delete or move into core: `UnityClient/Assets/Scripts/Editor/P3Validation/`
- Delete runtime evidence: `UnityClient/Logs/P3Validation/runs/e2e_*`
- Modify: `UnityClient/Assets/Scripts/Editor/AutoTestDaemon.cs`

**Interfaces:**
- Leaves AutoTestDaemon compatibility triggers intact.
- Removes all new creation paths for mixed RunIDs.

- [ ] **Step 1: Add a repository scan test that fails while mixed entry names exist**

```powershell
$forbidden = @(".codex/skills/p3-validation", "p3_run_unity_profile", "p3_validation_profiles.json")
foreach ($item in $forbidden) { if (Test-Path $item -or (rg -l $item .)) { throw "legacy mixed entry remains: $item" } }
```

- [ ] **Step 2: Move reusable classes into `P3ValidationCore` and update namespaces/references**

- [ ] **Step 3: Delete old Skill, Profiles, scripts, tools and generated `e2e_*` evidence**

- [ ] **Step 4: Keep `.test_trigger`, existing P0 CLI and Runner compatibility paths functional**

- [ ] **Step 5: Run repository scan, builds and focused AutoTestDaemon parity test**

- [ ] **Step 6: Commit**

```powershell
git add -A -- .codex/skills/p3-validation UnityClient/Assets/Editor/P3Validation UnityClient/Assets/Scripts/Editor/P3Validation UnityClient/Assets/Scripts/Editor/P3ValidationCore tools/agent/p3-validation tools/agent/p3-validation-core UnityClient/Assets/Scripts/Editor/AutoTestDaemon.cs
git commit -m "refactor: remove mixed P3 validation entry"
```

---

### Task 12: End-to-End Gates, Documentation and Status Writeback

**Files:**
- Modify: `tools/agent/README.md`
- Modify: `开发文档/19_UnityMCP验收编排层设计.md`
- Modify: `开发文档/rules/04_自动化测试与验收流程规范.md`
- Modify: `agent_status/program.md`
- Modify: `agent_status/art.md`
- Modify: `知识库/views/program.md`
- Modify: `知识库/views/art.md`
- Generate: `DOCS_INDEX.md`
- Generate: `docs_index.json`

**Interfaces:**
- Produces one successful and one controlled-failure package per program/art Profile.
- Produces release matrix evidence.

- [ ] **Step 1: Run program Profile gates**

Expected: no ArtAcceptance process, screenshot requirement or art review field appears.

- [ ] **Step 2: Run art Profile gates**

Expected: no full P0 execution or domain mutation; iteration packages contain before/after evidence.

- [ ] **Step 3: Run release matrix and fingerprint mismatch injection**

- [ ] **Step 4: Verify project-scoped MCP discovery after reconnect**

Expected tools: three shared, two program and five art tools; old mixed tools absent.

- [ ] **Step 5: Update docs and both status pages with actual evidence paths and remaining limitations**

- [ ] **Step 6: Run final verification**

```powershell
.\tools\agent\p3-validation-core\Test-P3ValidationCore.ps1
dotnet build UnityClient/Assembly-CSharp.csproj --no-restore
dotnet build UnityClient/Assembly-CSharp-Editor.csproj --no-restore
python C:\Users\WhiteSheep\.codex\skills\.system\skill-creator\scripts\quick_validate.py .codex/skills/p3-program-validation
python C:\Users\WhiteSheep\.codex\skills\.system\skill-creator\scripts\quick_validate.py .codex/skills/p3-art-validation
python C:\Users\WhiteSheep\.codex\skills\.system\skill-creator\scripts\quick_validate.py .codex/skills/p3-release-validation
.\tools\docs\Generate-DocsIndex.ps1
.\tools\docs\Validate-Docs.ps1
.\tools\agent\Invoke-AgentHealthCheck.ps1 -Strict
```

Expected: contract tests and Skills pass; both builds have 0 errors; docs validation passes; health check contains no task-owned risk.

- [ ] **Step 7: Commit**

```powershell
git add -- tools/agent/README.md 开发文档/19_UnityMCP验收编排层设计.md 开发文档/rules/04_自动化测试与验收流程规范.md agent_status/program.md agent_status/art.md 知识库/views/program.md 知识库/views/art.md DOCS_INDEX.md docs_index.json
git commit -m "docs: switch P3 validation to role-specific entries"
```

---

## Plan Completion Gate

Implementation is complete only when:

- All 12 tasks have focused tests and commits.
- The old mixed Skill, Profiles, MCP tools and `e2e_*` evidence are absent.
- Program Profiles cannot start ArtAcceptance or claim visual completion.
- Art Profiles cannot run full P0 or mutate domain rules.
- `art_iteration` accepts only typed allowlisted UGUI/VisualID changes and records before/after evidence.
- Domain-aware mergers reject cross-domain steps and validate required artifact hashes.
- Release aggregation is read-only and enforces matching input fingerprints.
- Each program/art Profile has success and controlled-failure evidence.
- Program, art and release status pages and fact documents contain actual evidence paths.
