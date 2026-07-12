---
id: dev_20_unity_mcp_validation_implementation_plan
title: Unity MCP 验收编排层实现计划
type: dev
role: 程序
domain: test_automation
status: historical
source_of_truth: false
related:
  - 开发文档/README.md
  - 开发文档/00_程序开发大纲.md
  - 开发文档/19_UnityMCP验收编排层设计.md
  - 开发文档/15_P0配置Validator与自动验收底座需求.md
  - 开发文档/14_Unity运行时美术自动验收方案.md
  - tools/agent/README.md
  - 知识库/views/program.md
last_verified: 2026-07-12
update_rule: 调整 Unity MCP 验收编排实现任务、文件边界、测试命令、阶段门禁或提交顺序时同步本文档。
---

# Unity MCP Validation Orchestration Implementation Plan

> 历史说明：本计划对应旧的单一 `p3-validation` 混合入口，已完成首版实现，但不再作为后续重构执行依据。当前事实来源为 `开发文档/19_UnityMCP验收编排层设计.md` 中批准的“程序验收 / 美术迭代验收 / 发布聚合”直接拆分方案；新的实现计划需据此重新生成。

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build the `p3-validation` Codex Skill and project-scoped Unity MCP tools that combine deterministic P3 scripts with observable Unity validation jobs and RunID-scoped evidence.

**Architecture:** The Codex Skill is the only top-level agent entry. PowerShell keeps deterministic file validation and report merging; Unity Editor custom MCP tools own readiness, compilation, Console deltas, Smoke, ArtAcceptance and T0 capture. Both lanes write `p3-validation/step-result@1`, and only the merge script calculates the final status and `ClaimCeiling`.

**Tech Stack:** Unity 2022.3.60f1, C# Editor scripts, MCP for Unity v10.0.0, Newtonsoft.Json, PowerShell 5.1+, Codex project skills, existing P3 AutoTestDaemon / ArtAcceptance / T0 runners.

## Global Constraints

- Runtime UI remains pure UGUI; do not introduce UI Toolkit, UXML, USS or `UIDocument`.
- Do not change gameplay rules, domain services, configuration facts, Approved assets or active UI specifications.
- Unity MCP tools must not execute arbitrary C#, arbitrary reflection method names, arbitrary menu paths or external processes.
- All writable evidence stays under `UnityClient/Logs/P3Validation/`; persistent job state stays under `UnityClient/Library/`.
- Static automation continues through PowerShell / Python; Unity-dependent work goes through project-scoped MCP tools.
- Existing `AutoTestDaemon`, `ArtAcceptanceRunner` and `T0ValidationFinalCaptureRunner` remain the execution core until parity gates pass.
- A machine `Passed` result cannot exceed the report's `ClaimCeiling` or replace required external review.
- Preserve unrelated dirty workspace changes and stage only files owned by the current task.

---

## File Structure

### Codex orchestration

- Create `.codex/skills/p3-validation/SKILL.md`: routing, Profile selection, MCP/script sequencing and claim rules.
- Create `.codex/skills/p3-validation/agents/openai.yaml`: discovery metadata.
- Create `.codex/skills/p3-validation/references/profile-routing.md`: user intent to Profile mapping.
- Create `.codex/skills/p3-validation/references/evidence-and-claims.md`: report reading and `ClaimCeiling` rules.

### Shared Profile and schema source

- Create `UnityClient/Assets/Editor/P3Validation/p3_validation_profiles.json`: single Profile source read by Unity and PowerShell.
- Create `tools/agent/p3-validation/schemas/step-result.schema.json`.
- Create `tools/agent/p3-validation/schemas/validation-summary.schema.json`.
- Create `tools/agent/p3-validation/fixtures/`: golden success/failure reports.

### Deterministic scripts

- Create `tools/agent/p3-validation/New-P3ValidationRun.ps1`.
- Create `tools/agent/p3-validation/Invoke-P3StaticValidation.ps1`.
- Create `tools/agent/p3-validation/Merge-P3ValidationEvidence.ps1`.
- Create `tools/agent/p3-validation/Test-P3Validation.ps1`.

### Unity Editor validation module

- Create `UnityClient/Assets/Scripts/Editor/P3Validation/P3ValidationContracts.cs`.
- Create `UnityClient/Assets/Scripts/Editor/P3Validation/P3ValidationProfileRegistry.cs`.
- Create `UnityClient/Assets/Scripts/Editor/P3Validation/P3ValidationEvidencePaths.cs`.
- Create `UnityClient/Assets/Scripts/Editor/P3Validation/P3ValidationJobStore.cs`.
- Create `UnityClient/Assets/Scripts/Editor/P3Validation/P3ValidationInstanceLock.cs`.
- Create `UnityClient/Assets/Scripts/Editor/P3Validation/P3ValidationEditorSnapshot.cs`.
- Create `UnityClient/Assets/Scripts/Editor/P3Validation/P3ValidationConsoleTracker.cs`.
- Create `UnityClient/Assets/Scripts/Editor/P3Validation/P3SmokeExecutionService.cs`.
- Create `UnityClient/Assets/Scripts/Editor/P3Validation/P3ArtAcceptanceAdapter.cs`.
- Create `UnityClient/Assets/Scripts/Editor/P3Validation/P3T0CaptureAdapter.cs`.
- Create `UnityClient/Assets/Scripts/Editor/P3Validation/P3UnityValidationOrchestrator.cs`.

### Project-scoped MCP tools

- Create `UnityClient/Assets/Scripts/Editor/P3Validation/Tools/P3UnityReadinessTool.cs`.
- Create `UnityClient/Assets/Scripts/Editor/P3Validation/Tools/P3RunSmokeProfileTool.cs`.
- Create `UnityClient/Assets/Scripts/Editor/P3Validation/Tools/P3RunArtAcceptanceTool.cs`.
- Create `UnityClient/Assets/Scripts/Editor/P3Validation/Tools/P3CaptureT0Tool.cs`.
- Create `UnityClient/Assets/Scripts/Editor/P3Validation/Tools/P3CollectUnityEvidenceTool.cs`.
- Create `UnityClient/Assets/Scripts/Editor/P3Validation/Tools/P3RunUnityProfileTool.cs`.

### Tests and existing integrations

- Create `UnityClient/Assets/Scripts/Editor/Tests/P3ValidationContractsSmokeTest.cs`.
- Create `UnityClient/Assets/Scripts/Editor/Tests/P3ValidationProfileRegistrySmokeTest.cs`.
- Create `UnityClient/Assets/Scripts/Editor/Tests/P3ValidationJobStateSmokeTest.cs`.
- Create `UnityClient/Assets/Scripts/Editor/Tests/P3ValidationConsoleDeltaSmokeTest.cs`.
- Create `UnityClient/Assets/Scripts/Editor/Tests/P3ValidationMcpToolsSmokeTest.cs`.
- Modify `UnityClient/Assets/Scripts/Editor/AutoTestDaemon.cs`: delegate reflection execution to `P3SmokeExecutionService` while preserving trigger behavior.
- Modify `tools/agent/Invoke-P0Validation.ps1`: optional RunID/evidence adapter only; preserve current CLI behavior.

---

### Task 1: Freeze Profiles, Schemas and Golden Fixtures

**Files:**
- Create: `UnityClient/Assets/Editor/P3Validation/p3_validation_profiles.json`
- Create: `tools/agent/p3-validation/schemas/step-result.schema.json`
- Create: `tools/agent/p3-validation/schemas/validation-summary.schema.json`
- Create: `tools/agent/p3-validation/fixtures/step-passed.json`
- Create: `tools/agent/p3-validation/fixtures/step-failed.json`
- Create: `tools/agent/p3-validation/fixtures/step-blocked.json`
- Create: `tools/agent/p3-validation/fixtures/summary-limited.json`
- Create: `tools/agent/p3-validation/Test-P3Validation.ps1`

**Interfaces:**
- Produces: Profile IDs `smoke_focus`, `art_runtime`, `t0_seal`, `p0_full`.
- Produces: Schema identifiers `p3-validation/step-result@1` and `p3-validation/summary@1`.
- Consumed by: all later C#, PowerShell and Skill tasks.

- [ ] **Step 1: Write the failing schema/profile test**

Create `Test-P3Validation.ps1` with these exact assertions:

```powershell
param([string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).ProviderPath)
$ErrorActionPreference = "Stop"

$profilePath = Join-Path $RepoRoot "UnityClient/Assets/Editor/P3Validation/p3_validation_profiles.json"
$profiles = Get-Content -LiteralPath $profilePath -Raw -Encoding UTF8 | ConvertFrom-Json
$expected = @("smoke_focus", "art_runtime", "t0_seal", "p0_full")

foreach ($id in $expected) {
    if (-not ($profiles.profiles.id -contains $id)) { throw "missing profile: $id" }
}
foreach ($profile in $profiles.profiles) {
    if ([string]::IsNullOrWhiteSpace($profile.version)) { throw "profile version missing: $($profile.id)" }
    if ($profile.editor_control -ne "exclusive_restore") { throw "invalid editor_control: $($profile.id)" }
}

$fixtures = Get-ChildItem (Join-Path $PSScriptRoot "fixtures") -Filter "*.json"
foreach ($fixture in $fixtures) {
    $data = Get-Content $fixture.FullName -Raw -Encoding UTF8 | ConvertFrom-Json
    if ($data.schema_version -notmatch '^p3-validation/') { throw "schema missing: $($fixture.Name)" }
    if ([string]::IsNullOrWhiteSpace($data.run_id)) { throw "run_id missing: $($fixture.Name)" }
}
Write-Output "[p3-validation] profile/schema fixtures passed"
```

- [ ] **Step 2: Run the test and verify it fails**

Run:

```powershell
.\tools\agent\p3-validation\Test-P3Validation.ps1
```

Expected: FAIL because `p3_validation_profiles.json` and fixtures do not exist.

- [ ] **Step 3: Add the four Profile definitions**

Create JSON with this top-level structure and no arbitrary command fields:

```json
{
  "schema_version": "p3-validation/profiles@1",
  "profiles": [
    {
      "id": "smoke_focus",
      "version": "1",
      "static_steps": [],
      "unity_profile_id": "smoke_focus",
      "required_evidence": ["console_delta", "smoke_report"],
      "timeout_seconds": 300,
      "strict_warnings": false,
      "editor_control": "exclusive_restore",
      "external_review_required": []
    },
    {
      "id": "art_runtime",
      "version": "1",
      "static_steps": ["ui_spec_validate"],
      "unity_profile_id": "art_runtime",
      "required_evidence": ["console_delta", "art_acceptance_report", "screenshots"],
      "timeout_seconds": 600,
      "strict_warnings": false,
      "editor_control": "exclusive_restore",
      "external_review_required": ["art"]
    },
    {
      "id": "t0_seal",
      "version": "1",
      "static_steps": ["config_sync", "ui_spec_validate", "art_manifest_check"],
      "unity_profile_id": "t0_seal",
      "required_evidence": ["console_delta", "smoke_report", "t0_capture_report", "screenshots"],
      "timeout_seconds": 900,
      "strict_warnings": false,
      "editor_control": "exclusive_restore",
      "external_review_required": ["art", "director"]
    },
    {
      "id": "p0_full",
      "version": "1",
      "static_steps": ["config_sync", "config_static_validate", "ui_spec_validate"],
      "unity_profile_id": "p0_full",
      "required_evidence": ["config_report", "console_delta", "smoke_report", "art_acceptance_summary"],
      "timeout_seconds": 900,
      "strict_warnings": false,
      "editor_control": "exclusive_restore",
      "external_review_required": []
    }
  ]
}
```

- [ ] **Step 4: Add JSON schemas and golden fixtures**

Schemas must require the exact status enums from the design. Golden fixtures must include one artifact with `path`, `source_path`, `sha256`, `size`, `captured_at` and `mime_type`.

- [ ] **Step 5: Run the fixture test**

Run:

```powershell
.\tools\agent\p3-validation\Test-P3Validation.ps1
```

Expected: `[p3-validation] profile/schema fixtures passed` and exit code 0.

- [ ] **Step 6: Commit**

```powershell
git add -- UnityClient/Assets/Editor/P3Validation tools/agent/p3-validation
git commit -m "test: freeze P3 validation profiles and schemas"
```

---

### Task 2: Create RunID and Deterministic Evidence Merge Scripts

**Files:**
- Create: `tools/agent/p3-validation/New-P3ValidationRun.ps1`
- Create: `tools/agent/p3-validation/Merge-P3ValidationEvidence.ps1`
- Modify: `tools/agent/p3-validation/Test-P3Validation.ps1`

**Interfaces:**
- Produces: `New-P3ValidationRun -ProfileId <id>` returning `RunId`, `EvidenceRoot`, `RequestPath`.
- Produces: `Merge-P3ValidationEvidence -RunId <id>` writing `validation-summary.json` and `.md`.

- [ ] **Step 1: Add failing merge precedence tests**

Extend the PowerShell test to copy fixture steps into a temporary run and assert:

```powershell
$summary = & $mergeScript -RunId $runId -RepoRoot $RepoRoot -PassThru
if ($summary.AutomationStatus -ne "Failed") { throw "Failed must outrank Blocked/Limited" }
if ($summary.ClaimCeiling -ne "evidence_collected") { throw "failed run claim ceiling is too high" }
```

Add separate cases verifying `Blocked > Limited > Passed` when no required step failed.

- [ ] **Step 2: Run the tests and verify failure**

Expected: FAIL because run creation and merge scripts are absent.

- [ ] **Step 3: Implement run creation**

`New-P3ValidationRun.ps1` must:

```powershell
param(
    [Parameter(Mandatory)][string]$ProfileId,
    [string]$RunId = "",
    [string]$RepoRoot = (Resolve-Path (Join-Path $PSScriptRoot "..\..\..")).ProviderPath,
    [switch]$PassThru
)

if ([string]::IsNullOrWhiteSpace($RunId)) {
    $RunId = "{0}_{1}" -f (Get-Date -Format "yyyyMMdd_HHmmss"), $ProfileId
}
if ($RunId -notmatch '^[A-Za-z0-9_-]+$') { throw "invalid RunId: $RunId" }

$root = Join-Path $RepoRoot "UnityClient/Logs/P3Validation/runs/$RunId"
foreach ($child in @("steps", "unity", "screenshots", "source_reports")) {
    New-Item -ItemType Directory -Force -Path (Join-Path $root $child) | Out-Null
}
```

It must reject an already completed RunID and write `request.json` atomically through a temporary file and `Move-Item`.

- [ ] **Step 4: Implement deterministic status and claim merging**

`Merge-P3ValidationEvidence.ps1` must use this precedence:

```powershell
if ($required.Status -contains "Failed") { $automation = "Failed" }
elseif ($required.Status -contains "Blocked") { $automation = "Blocked" }
elseif ($required.Status -contains "Limited") { $automation = "Limited" }
elseif ($required.Status -contains "Cancelled") { $automation = "Cancelled" }
else { $automation = "Passed" }
```

`ClaimCeiling` must be derived only from `AutomationStatus`, `OwnerValidation` and `ExternalReview`; never from free text.

- [ ] **Step 5: Run tests**

Expected: all precedence, duplicate RunID and atomic write cases pass.

- [ ] **Step 6: Commit**

```powershell
git add -- tools/agent/p3-validation
git commit -m "feat: add P3 validation evidence merger"
```

---

### Task 3: Add Shared C# Contracts and Profile Registry

**Files:**
- Create: `UnityClient/Assets/Scripts/Editor/P3Validation/P3ValidationContracts.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3Validation/P3ValidationProfileRegistry.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3Validation/P3ValidationEvidencePaths.cs`
- Create: `UnityClient/Assets/Scripts/Editor/Tests/P3ValidationContractsSmokeTest.cs`
- Create: `UnityClient/Assets/Scripts/Editor/Tests/P3ValidationProfileRegistrySmokeTest.cs`

**Interfaces:**
- Produces: `P3ValidationStepStatus`, `P3ValidationStepResult`, `P3ValidationArtifact`, `P3ValidationProfile`.
- Produces: `P3ValidationProfileRegistry.GetRequired(string id)`.
- Produces: `P3ValidationEvidencePaths.ForRun(string runId)`.

- [ ] **Step 1: Write failing static Smoke tests**

```csharp
public static class P3ValidationContractsSmokeTest
{
    public static void Run()
    {
        var result = P3ValidationStepResult.Passed("run_1", "smoke_focus", "compile_gate", true);
        if (result.SchemaVersion != "p3-validation/step-result@1")
            UnityEngine.Debug.LogError("P3 Validation Contracts FAILED: schema");
        if (result.Status != P3ValidationStepStatus.Passed)
            UnityEngine.Debug.LogError("P3 Validation Contracts FAILED: status");
        UnityEngine.Debug.Log("P3 Validation Contracts PASSED");
    }
}
```

Registry test must assert the four IDs, version `1`, `exclusive_restore`, and rejection of an unknown ID.

- [ ] **Step 2: Trigger tests and confirm failure**

```powershell
Set-Content UnityClient/Logs/.test_trigger "P3ValidationContractsSmokeTest.Run"
```

Expected: compilation failure because contract types do not exist.

- [ ] **Step 3: Implement contracts**

Use serializable fields matching snake_case through `JsonProperty` attributes. Provide factory methods:

```csharp
public static P3ValidationStepResult Passed(string runId, string profileId, string stepId, bool required);
public static P3ValidationStepResult Failed(string runId, string profileId, string stepId, bool required, string code, string message);
public static P3ValidationStepResult Blocked(string runId, string profileId, string stepId, bool required, string limitationCode, string message);
```

- [ ] **Step 4: Implement profile loading and evidence path validation**

The registry must load only:

```text
Assets/Editor/P3Validation/p3_validation_profiles.json
```

`ForRun` must reject path separators and resolve only under `UnityClient/Logs/P3Validation/runs/`.

- [ ] **Step 5: Run focused tests and builds**

```powershell
Set-Content UnityClient/Logs/.test_trigger "P3ValidationContractsSmokeTest.Run"
Set-Content UnityClient/Logs/.test_trigger "P3ValidationProfileRegistrySmokeTest.Run"
dotnet build UnityClient/Assembly-CSharp-Editor.csproj --no-restore
```

Expected: both reports `PASSED`; build 0 errors.

- [ ] **Step 6: Commit**

```powershell
git add -- UnityClient/Assets/Editor/P3Validation UnityClient/Assets/Scripts/Editor/P3Validation UnityClient/Assets/Scripts/Editor/Tests
git commit -m "feat: add P3 validation contracts and profiles"
```

---

### Task 4: Extract Shared Smoke Execution Without Breaking AutoTestDaemon

**Files:**
- Create: `UnityClient/Assets/Scripts/Editor/P3Validation/P3SmokeExecutionService.cs`
- Modify: `UnityClient/Assets/Scripts/Editor/AutoTestDaemon.cs`
- Create: `UnityClient/Assets/Scripts/Editor/Tests/P3SmokeExecutionServiceSmokeTest.cs`

**Interfaces:**
- Produces: `P3SmokeExecutionResult Execute(IEnumerable<string> testNames)`.
- Consumed by: `AutoTestDaemon` and `p3_run_smoke_profile`.

- [ ] **Step 1: Write a characterization test around current reflection semantics**

The test must cover a passing method, a method logging `LogError`, a missing method and an exception. It must assert that only the passing method yields `Passed=true`.

- [ ] **Step 2: Run the characterization test before refactor**

Expected: FAIL because `P3SmokeExecutionService` does not exist.

- [ ] **Step 3: Move reflection execution into the service**

Expose:

```csharp
public sealed class P3SmokeExecutionResult
{
    public bool Passed;
    public List<string> Tests = new List<string>();
    public List<string> Logs = new List<string>();
    public List<string> Errors = new List<string>();
}

public static P3SmokeExecutionResult Execute(IEnumerable<string> testNames)
```

The service must subscribe/unsubscribe `Application.logMessageReceived` in `try/finally` and restore `GameRoot.Core` / global test state exactly as current tests require.

- [ ] **Step 4: Delegate AutoTestDaemon to the service**

Preserve `.test_trigger`, `RUN_ALL_TESTS` and `Logs/TestReport.json`. Do not change CLI output fields in this task.

- [ ] **Step 5: Run parity verification**

```powershell
Set-Content UnityClient/Logs/.test_trigger "RUN_ALL_TESTS"
```

Expected: same discovered test count and all previously passing tests remain passing.

- [ ] **Step 6: Commit**

```powershell
git add -- UnityClient/Assets/Scripts/Editor/AutoTestDaemon.cs UnityClient/Assets/Scripts/Editor/P3Validation UnityClient/Assets/Scripts/Editor/Tests
git commit -m "refactor: share P3 smoke execution service"
```

---

### Task 5: Implement Readiness, Editor Snapshot and Console Delta

**Files:**
- Create: `UnityClient/Assets/Scripts/Editor/P3Validation/P3ValidationEditorSnapshot.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3Validation/P3ValidationConsoleTracker.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3Validation/Tools/P3UnityReadinessTool.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3Validation/Tools/P3CollectUnityEvidenceTool.cs`
- Create: `UnityClient/Assets/Scripts/Editor/Tests/P3ValidationConsoleDeltaSmokeTest.cs`
- Create: `UnityClient/Assets/Scripts/Editor/Tests/P3ValidationMcpToolsSmokeTest.cs`

**Interfaces:**
- Produces: `P3ValidationEditorSnapshot Capture()`.
- Produces: `P3ConsoleMarker Begin(string runId)` and `P3ConsoleDelta Complete(P3ConsoleMarker marker)`.
- MCP tools: `p3_unity_readiness`, `p3_collect_unity_evidence`.

- [ ] **Step 1: Write failing Console delta test**

The test must create a baseline, log one warning and one error with unique tokens, complete the delta, and assert both new entries exist while a pre-baseline token does not.

- [ ] **Step 2: Implement snapshot and delta fingerprinting**

Fingerprint fields:

```text
log_type + condition + stack_trace + occurrence_index
```

Do not clear Unity Console. Persist baseline and delta JSON under the run's `unity/` directory.

- [ ] **Step 3: Implement readiness tool**

Use:

```csharp
[McpForUnityTool("p3_unity_readiness", Description = "Read-only P3 Unity validation readiness")]
public static class P3UnityReadinessTool
```

Return compile/update/play/prefab-stage/dirty-scene/job-lock fields and stable blocking codes.

- [ ] **Step 4: Run tests and inspect live MCP result**

Expected: static tests pass; MCP call returns `success=true` without changing PlayMode or selection.

- [ ] **Step 5: Commit**

```powershell
git add -- UnityClient/Assets/Scripts/Editor/P3Validation UnityClient/Assets/Scripts/Editor/Tests
git commit -m "feat: add P3 Unity readiness and console evidence"
```

---

### Task 6: Add Job Store, Instance Lock and Domain Reload Recovery

**Files:**
- Create: `UnityClient/Assets/Scripts/Editor/P3Validation/P3ValidationJobStore.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3Validation/P3ValidationInstanceLock.cs`
- Create: `UnityClient/Assets/Scripts/Editor/Tests/P3ValidationJobStateSmokeTest.cs`

**Interfaces:**
- Produces: `Load(runId)`, `Save(state)`, `Clear(runId)`.
- Produces: `TryAcquire(instanceId, runId, out activeRunId)` and `Release(instanceId, runId)`.

- [ ] **Step 1: Write failing persistence and collision tests**

Assert state survives serialize/load and that a second RunID cannot acquire the same instance lock.

- [ ] **Step 2: Implement RunID-scoped state on top of `McpJobStateStore`**

Use a sanitized key:

```csharp
private static string Key(string runId) => "p3_validation_" + runId;
```

State must include current step, progress, evidence root, original Editor state, source Runner ID, last error and restore action.

- [ ] **Step 3: Implement stale lock recovery**

A lock can be reclaimed only when its JobState is terminal or older than the configured infrastructure timeout. Record reclaim as a warning artifact.

- [ ] **Step 4: Run focused tests**

Expected: persistence, collision, owner-only release and stale recovery tests pass.

- [ ] **Step 5: Commit**

```powershell
git add -- UnityClient/Assets/Scripts/Editor/P3Validation UnityClient/Assets/Scripts/Editor/Tests
git commit -m "feat: persist P3 validation jobs and locks"
```

---

### Task 7: Add Atomic Smoke MCP Tool

**Files:**
- Create: `UnityClient/Assets/Scripts/Editor/P3Validation/Tools/P3RunSmokeProfileTool.cs`
- Modify: `UnityClient/Assets/Editor/P3Validation/p3_validation_profiles.json`
- Modify: `UnityClient/Assets/Scripts/Editor/Tests/P3ValidationMcpToolsSmokeTest.cs`

**Interfaces:**
- MCP tool: `p3_run_smoke_profile(run_id, smoke_profile_id, action=start|status|cancel)`.
- Consumes: `P3SmokeExecutionService`, Profile registry, Job store and evidence paths.

- [ ] **Step 1: Add a registered smoke set and rejection tests**

Profiles JSON must name exact registered sets such as `t0_core` and `p0_core`; the tool must reject a raw method name and unknown set.

- [ ] **Step 2: Implement polling tool attribute**

```csharp
[McpForUnityTool(
    "p3_run_smoke_profile",
    Description = "Run an allowlisted P3 smoke profile",
    RequiresPolling = true,
    PollAction = "status",
    MaxPollSeconds = 900)]
```

Return `PendingResponse` while queued/running and `_mcp_status=complete` only after the step result is atomically written.

- [ ] **Step 3: Verify passing and failing profiles**

Use a fixture Smoke method that intentionally logs an error. Expected: passing set returns `Passed`; failing set returns tool completion with step `Failed`, not transport error.

- [ ] **Step 4: Commit**

```powershell
git add -- UnityClient/Assets/Editor/P3Validation UnityClient/Assets/Scripts/Editor/P3Validation UnityClient/Assets/Scripts/Editor/Tests
git commit -m "feat: expose allowlisted smoke profiles over MCP"
```

---

### Task 8: Add ArtAcceptance and T0 Atomic Adapters

**Files:**
- Create: `UnityClient/Assets/Scripts/Editor/P3Validation/P3ArtAcceptanceAdapter.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3Validation/P3T0CaptureAdapter.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3Validation/Tools/P3RunArtAcceptanceTool.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3Validation/Tools/P3CaptureT0Tool.cs`
- Modify: `UnityClient/Assets/Editor/P3Validation/p3_validation_profiles.json`
- Modify: `UnityClient/Assets/Scripts/Editor/Tests/P3ValidationMcpToolsSmokeTest.cs`

**Interfaces:**
- MCP tool: `p3_run_art_acceptance(run_id, acceptance_profile_id, action)`.
- MCP tool: `p3_capture_t0(run_id, capture_profile_id, action)`.

- [ ] **Step 1: Write adapter tests with fixture report directories**

Cover: matching source RunID, stale `latest`, missing screenshot, semantic failure and atomic copy into EvidenceRoot.

- [ ] **Step 2: Implement ArtAcceptance adapter**

Start through `ArtAcceptanceRunner.BeginAutomatedRun`, poll `report.json` until `IsRunning=false`, validate required ScreenTags, then copy report, UI snapshot, Registry snapshot and required screenshots.

- [ ] **Step 3: Implement T0 adapter**

Start `T0ValidationFinalCaptureRunner.StartCapture()`, wait for a report written after Job start, parse `semantic_failed`, copy the report and registered screenshot set, and reject stale files.

- [ ] **Step 4: Verify real success and injected failure**

Expected: real run produces RunID-scoped artifacts; deleting one copied fixture screenshot makes the adapter step fail with a stable code.

- [ ] **Step 5: Commit**

```powershell
git add -- UnityClient/Assets/Editor/P3Validation UnityClient/Assets/Scripts/Editor/P3Validation UnityClient/Assets/Scripts/Editor/Tests
git commit -m "feat: adapt ArtAcceptance and T0 capture to MCP"
```

---

### Task 9: Build the Unity Profile Orchestrator

**Files:**
- Create: `UnityClient/Assets/Scripts/Editor/P3Validation/P3UnityValidationOrchestrator.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3Validation/Tools/P3RunUnityProfileTool.cs`
- Modify: `UnityClient/Assets/Scripts/Editor/Tests/P3ValidationMcpToolsSmokeTest.cs`

**Interfaces:**
- MCP tool: `p3_run_unity_profile(run_id, unity_profile_id, control_policy, action)`.
- Sequences readiness, baseline, refresh/compile, registered atomic steps, evidence collection and Editor restoration.

- [ ] **Step 1: Write state-machine and cancellation tests**

Test transitions:

```text
Created -> Preflight -> Prepare -> Execute -> CollectEvidence -> Complete
```

Also cover compile blocked, job collision, cancellation at a safe point and restore limitation.

- [ ] **Step 2: Implement `exclusive_restore` preflight**

Block on dirty Scene or open dirty Prefab Stage. Capture original PlayMode, active Scene and selection. Do not auto-save.

- [ ] **Step 3: Implement orchestration and status polling**

Infrastructure retries are bounded. A business `Failed` step is terminal and must not be rerun automatically.

- [ ] **Step 4: Run failure injection suite**

Expected codes include:

```text
blocked:unity_validation_job_active
validation_limited:UnityEditorStateStale
validation_limited:CompilationTimeout
validation_limited:editor_state_restore_failed
```

- [ ] **Step 5: Commit**

```powershell
git add -- UnityClient/Assets/Scripts/Editor/P3Validation UnityClient/Assets/Scripts/Editor/Tests
git commit -m "feat: orchestrate P3 Unity validation profiles"
```

---

### Task 10: Add Static Lane Adapter and P0 Integration

**Files:**
- Create: `tools/agent/p3-validation/Invoke-P3StaticValidation.ps1`
- Modify: `tools/agent/Invoke-P0Validation.ps1`
- Modify: `tools/agent/p3-validation/Test-P3Validation.ps1`

**Interfaces:**
- Produces: `Invoke-P3StaticValidation -RunId -ProfileId`.
- Adds optional parameters to P0 without changing defaults: `-ParentRunId`, `-EvidenceRoot`, `-StaticOnly`.

- [ ] **Step 1: Write compatibility tests**

Run current P0 command with no new parameters and assert its existing output paths and status fields remain unchanged.

- [ ] **Step 2: Implement Profile step dispatch**

Dispatch only registered step IDs with a PowerShell `switch`; never execute a command string from JSON.

- [ ] **Step 3: Convert child results to `step-result@1`**

Preserve original report paths as artifacts and write each converted result to `steps/<step-id>/result.json`.

- [ ] **Step 4: Run static profiles and compatibility command**

Expected: `t0_seal` static lane and existing `Invoke-P0Validation.ps1` both run; no default behavior regression.

- [ ] **Step 5: Commit**

```powershell
git add -- tools/agent/p3-validation tools/agent/Invoke-P0Validation.ps1
git commit -m "feat: add P3 static validation lane"
```

---

### Task 11: Create the `p3-validation` Codex Skill

**Files:**
- Create: `.codex/skills/p3-validation/SKILL.md`
- Create: `.codex/skills/p3-validation/agents/openai.yaml`
- Create: `.codex/skills/p3-validation/references/profile-routing.md`
- Create: `.codex/skills/p3-validation/references/evidence-and-claims.md`

**Interfaces:**
- User entry: requests containing P3 validation, smoke, ArtAcceptance, T0 capture, P0 validation or MCP validation orchestration.
- Consumes: PowerShell run/static/merge scripts and the six Unity MCP tools.

- [ ] **Step 1: Write the Skill routing contract**

`SKILL.md` must require this order:

```text
read P3 facts
-> select one registered Profile
-> pin exact Unity instance
-> create RunID
-> run static lane
-> run Unity profile
-> merge evidence
-> read ClaimCeiling
-> report without expanding claims
```

- [ ] **Step 2: Add hard safety rules**

The Skill must forbid `manage_ui`, `execute_code`, arbitrary `execute_menu_item`, direct asset changes and unregistered Smoke methods during validation.

- [ ] **Step 3: Add exact fallback behavior**

If custom MCP tools are unavailable, return `validation_limited:P3CustomMcpToolsUnavailable` and offer the existing script/menu path; do not silently substitute a weaker check and call it passed.

- [ ] **Step 4: Validate Skill discovery**

Restart/reload Codex project skills and confirm `p3-validation` appears with the intended description.

- [ ] **Step 5: Commit**

```powershell
git add -- .codex/skills/p3-validation
git commit -m "feat: add P3 validation orchestration skill"
```

---

### Task 12: End-to-End Profiles, Default Switch and Documentation

**Files:**
- Modify: `tools/agent/README.md`
- Modify: `开发文档/19_UnityMCP验收编排层设计.md`
- Modify: `开发文档/rules/04_自动化测试与验收流程规范.md`
- Modify: `agent_status/program.md`
- Modify: `知识库/views/program.md`
- Generate: `DOCS_INDEX.md`
- Generate: `docs_index.json`

**Interfaces:**
- Produces: verified evidence packages for all four Profiles, each with one success and one controlled failure sample.

- [ ] **Step 1: Run Phase gate G1**

Call readiness and Console collection 10 times. Expected: no Editor mutations and no evidence misassociation.

- [ ] **Step 2: Run Phase gate G2**

For Smoke, ArtAcceptance and T0, run the legacy entry and MCP adapter against the same code/input. Compare status, test/capture counts and semantic failures.

- [ ] **Step 3: Run Phase gate G3 failure injection**

Exercise stale state, compile error, timeout, Domain Reload, collision, dirty Scene, business failure, missing screenshot and cancel. Expected: exact `Failed/Blocked/Limited/Cancelled` results.

- [ ] **Step 4: Run all four Profiles end to end**

Expected evidence roots:

```text
UnityClient/Logs/P3Validation/runs/<run>_smoke_focus/
UnityClient/Logs/P3Validation/runs/<run>_art_runtime/
UnityClient/Logs/P3Validation/runs/<run>_t0_seal/
UnityClient/Logs/P3Validation/runs/<run>_p0_full/
```

- [ ] **Step 5: Verify ClaimCeiling**

`art_runtime` and `t0_seal` automated success must still show external review `Required`; the Skill must not say “美术封板完成” or “完整 T0 完成”.

- [ ] **Step 6: Update default workflow docs**

Document MCP Skill as the default agent entry. Keep file triggers and generic menus as automation/fallback paths, not the primary interactive path.

- [ ] **Step 7: Run final verification**

```powershell
.\tools\agent\p3-validation\Test-P3Validation.ps1
dotnet build UnityClient/Assembly-CSharp.csproj --no-restore
dotnet build UnityClient/Assembly-CSharp-Editor.csproj --no-restore
.\tools\docs\Generate-DocsIndex.ps1
.\tools\docs\Validate-Docs.ps1
.\tools\agent\Invoke-AgentHealthCheck.ps1 -Strict
```

Expected: schema/profile tests pass, both builds have 0 errors, docs validation passes, health check contains no task-owned risk.

- [ ] **Step 8: Commit**

```powershell
git add -- tools/agent/README.md 开发文档/19_UnityMCP验收编排层设计.md 开发文档/rules/04_自动化测试与验收流程规范.md agent_status/program.md 知识库/views/program.md DOCS_INDEX.md docs_index.json
git commit -m "docs: make MCP the default P3 validation entry"
```

---

## Plan Completion Gate

Implementation is complete only when:

- All 12 tasks have their focused tests and commits.
- Four Profiles each have success and controlled failure evidence.
- Legacy/MCP parity is proven before the default switch.
- No Unity MCP validation tool can mutate gameplay, UI assets, configuration or Approved art.
- `Blocked`, `Limited`, stale evidence and external review requirements are represented without false success.
- Program status and fact documents contain the actual evidence paths and remaining limitations.
