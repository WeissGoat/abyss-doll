---
id: dev_20_unity_mcp_validation_implementation_plan
title: p3-art-validation V2 MCP 实时优先实现计划
type: dev
role: 程序
domain: runtime_art_validation
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
update_rule: 调整 MCP 实时查看、截图票据、Art Profile、表现迭代、全量回归适配或验收证据时同步本文件。
---

# p3-art-validation V2 MCP 实时优先实现计划

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** 将 `p3-art-validation` 重构为 MCP 直接查看 Game View、按结论截图、受控表现迭代和外部主美复核的日常美术验收入口，并把旧 `ArtAcceptanceRunner` 降为 `art_regression` 全量回归后端。

**Architecture:** Skill 负责调用标准 Unity MCP：锁定实例、控制 PlayMode、读取层级/组件、使用 `manage_camera(action="screenshot", capture_source="game_view")` 直接查看和截图。项目自定义工具只负责注册目标、截图票据、ArtRunID 证据归档、受控修改、回归导入和确定性汇总，不尝试从 C# 内部调用 MCP server。

**Tech Stack:** Unity 2022.3.60f1、MCP for Unity v10、C# Editor scripts、Newtonsoft.Json、PowerShell 5.1+、纯 UGUI、现有 ArtAcceptanceRunner、Codex Skill。

## Global Constraints

- 日常验收采用 `live-first, capture-on-decision`，不默认执行完整 ArtAcceptance。
- 正式截图必须由当前锁定 Unity 实例的标准 Unity MCP `manage_camera` 获取；省略 `camera`，确保 Screen Space Overlay UGUI 被捕获。
- 项目 MCP 工具不得接受任意截图源路径、任意 C#、任意菜单、任意反射方法或任意序列化属性路径。
- `p3_art_capture` 旧 `source_path` 参数直接删除，不提供兼容层。
- Art Profile 不运行完整 P0、不修改领域规则、不伪造玩家状态。
- PlayMode 临时修改只能作为 preview；正式通过必须验证持久化后重新进入 PlayMode 的画面。
- 所有正式 Art Profile 继续要求外部美术复核。
- 旧 `.art_acceptance_trigger` 保留为人工/CI 兼容入口，但 Skill 默认不使用 watcher。
- 保留用户和其他 agent 的无关脏文件；每个任务只暂存本任务路径。

## Target File Structure

```text
UnityClient/Assets/Editor/P3Validation/
  art_validation_profiles.json
  art_validation_targets.json

UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/
  P3ArtTargetContracts.cs
  P3ArtTargetRegistry.cs
  P3ArtCaptureContracts.cs
  P3ArtCaptureTicketStore.cs
  P3ArtEvidenceService.cs
  P3ArtInspectionContracts.cs
  P3ArtLiveInspectionService.cs
  P3ArtIterationContracts.cs
  P3ArtIterationService.cs
  P3ArtValidationOrchestrator.cs
  P3ArtAcceptanceAdapter.cs

UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/
  P3ArtOpenTargetTool.cs
  P3ArtInspectTargetTool.cs
  P3ArtPrepareCaptureTool.cs
  P3ArtFinalizeCaptureTool.cs
  P3ArtCompareIterationTool.cs
  P3ArtRunProfileTool.cs
  P3ArtRunRegressionTool.cs
```

---

### Task 1: Freeze V2 Profiles and Registered Target Contract

**Files:**
- Modify: `UnityClient/Assets/Editor/P3Validation/art_validation_profiles.json`
- Create: `UnityClient/Assets/Editor/P3Validation/art_validation_targets.json`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtTargetContracts.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtTargetRegistry.cs`
- Test: `UnityClient/Assets/Scripts/Editor/Tests/P3ArtTargetRegistrySmokeTest.cs`
- Modify: `tools/agent/p3-validation-core/Test-P3ValidationCore.ps1`

**Interfaces:**
- Produces `ArtTargetDefinition`, `ArtTargetRegistry.Get(string targetId)`, `ArtCaptureRole`.
- Adds Profile `art_regression`.
- Makes `final_capture` required for formal focus/runtime runs and removes generic `screenshots` steps.

- [ ] **Step 1: Write the failing registry test**

```csharp
public static class P3ArtTargetRegistrySmokeTest {
    public static void Run() {
        var target = P3.Validation.ArtTargetRegistry.Get("workshop_main");
        if (target.ScreenTag != "WorkshopMain") throw new Exception("wrong screen tag");
        if (target.ExpectedRoots.Count == 0) throw new Exception("expected root missing");
        AssertThrows(() => P3.Validation.ArtTargetRegistry.Get("../raw"));
        AssertThrows(() => P3.Validation.ArtTargetRegistry.Get("unknown"));
    }
}
```

- [ ] **Step 2: Run the focused Editor build and confirm missing types**

Run: `dotnet build UnityClient/Assembly-CSharp-Editor.csproj --no-restore`

Expected: non-zero with `ArtTargetRegistry` or `ArtTargetDefinition` missing.

- [ ] **Step 3: Add the target contract**

```csharp
public enum ArtCaptureRole { Issue, Before, After, Final, Seal, Regression }

[Serializable]
public sealed class ArtTargetDefinition {
    [JsonProperty("target_id")] public string TargetId;
    [JsonProperty("screen_tag")] public string ScreenTag;
    [JsonProperty("expected_roots")] public List<string> ExpectedRoots = new List<string>();
    [JsonProperty("required_visual_ids")] public List<string> RequiredVisualIds = new List<string>();
    [JsonProperty("reference_width")] public int ReferenceWidth = 1920;
    [JsonProperty("reference_height")] public int ReferenceHeight = 1080;
    [JsonProperty("persist_mode")] public string PersistMode;
    [JsonProperty("prefab_asset_path")] public string PrefabAssetPath;
}
```

- [ ] **Step 4: Add registered target JSON**

```json
{
  "schema_version": "p3-validation/art-targets@1",
  "targets": [
    {
      "target_id": "workshop_main",
      "screen_tag": "WorkshopMain",
      "expected_roots": ["WorkshopUI"],
      "required_visual_ids": [],
      "reference_width": 1920,
      "reference_height": 1080,
      "persist_mode": "registered_adapter",
      "prefab_asset_path": ""
    },
    {
      "target_id": "dungeon_map",
      "screen_tag": "DungeonMap",
      "expected_roots": ["DungeonMapUI"],
      "required_visual_ids": [],
      "reference_width": 1920,
      "reference_height": 1080,
      "persist_mode": "registered_adapter",
      "prefab_asset_path": ""
    },
    {
      "target_id": "dialogue_overlay",
      "screen_tag": "DialogueOverlay",
      "expected_roots": ["DialogueOverlay"],
      "required_visual_ids": [],
      "reference_width": 1920,
      "reference_height": 1080,
      "persist_mode": "registered_adapter",
      "prefab_asset_path": ""
    },
    {
      "target_id": "t0_prologue",
      "screen_tag": "T0Prologue",
      "expected_roots": ["PrologueFirstDive"],
      "required_visual_ids": [],
      "reference_width": 1920,
      "reference_height": 1080,
      "persist_mode": "registered_adapter",
      "prefab_asset_path": ""
    }
  ]
}
```

- [ ] **Step 5: Replace art Profiles**

```json
{
  "schema_version": "p3-validation/art-profiles@3",
  "profiles": [
    {
      "id": "art_focus",
      "version": "3",
      "validation_domain": "art",
      "mode": "live_focus",
      "required_steps": ["readiness", "target_reachability", "live_inspection", "final_capture", "console_delta"],
      "external_review_required": ["art"]
    },
    {
      "id": "art_runtime",
      "version": "3",
      "validation_domain": "art",
      "mode": "live_set",
      "required_steps": ["readiness", "target_reachability", "live_inspection", "final_capture", "console_delta"],
      "external_review_required": ["art"]
    },
    {
      "id": "art_iteration",
      "version": "3",
      "validation_domain": "art",
      "mode": "iteration",
      "required_steps": ["readiness", "before_capture", "diagnosis", "persisted_changes", "after_capture", "console_delta"],
      "external_review_required": ["art"]
    },
    {
      "id": "t0_art_seal",
      "version": "3",
      "validation_domain": "art",
      "mode": "seal",
      "required_steps": ["readiness", "target_reachability", "live_inspection", "seal_capture", "console_delta"],
      "external_review_required": ["art", "director"]
    },
    {
      "id": "art_regression",
      "version": "3",
      "validation_domain": "art",
      "mode": "regression",
      "required_steps": ["readiness", "art_acceptance", "regression_import", "console_delta"],
      "external_review_required": ["art"]
    }
  ]
}
```

- [ ] **Step 6: Run contract tests and commit**

Run:

```powershell
.\tools\agent\p3-validation-core\Test-P3ValidationCore.ps1
dotnet build UnityClient/Assembly-CSharp-Editor.csproj --no-restore
```

Expected: Profile/target tests pass; build has 0 errors.

Commit:

```powershell
git add -- UnityClient/Assets/Editor/P3Validation UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtTarget* UnityClient/Assets/Scripts/Editor/Tests/P3ArtTargetRegistrySmokeTest.cs tools/agent/p3-validation-core/Test-P3ValidationCore.ps1
git commit -m "test: freeze MCP live-first art validation contracts"
```

---

### Task 2: Replace Arbitrary Screenshot Paths with Capture Tickets

**Files:**
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtCaptureContracts.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtCaptureTicketStore.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtEvidenceService.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtPrepareCaptureTool.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtFinalizeCaptureTool.cs`
- Delete: old `p3_art_capture` implementation from `P3ArtTools.cs`
- Test: `UnityClient/Assets/Scripts/Editor/Tests/P3ArtCaptureTicketSmokeTest.cs`

**Interfaces:**
- `PrepareCapture(runId, profileId, targetId, role, iterationId)` returns an exact MCP screenshot command envelope.
- Skill calls standard Unity MCP `manage_camera(action="screenshot")` with the returned folder and filename.
- `FinalizeCapture(ticketId)` validates and imports only the exact ticket path.

- [ ] **Step 1: Write rejection tests**

```csharp
var ticket = ArtCaptureTicketStore.Prepare(runId, "art_focus", "workshop_main", ArtCaptureRole.Final, null);
if (ticket.CaptureSource != "game_view") throw new Exception("wrong source");
if (ticket.Camera != null) throw new Exception("camera must be omitted for overlay UI");
AssertThrows(() => ArtEvidenceService.Finalize("../raw"));
AssertThrows(() => ArtEvidenceService.Finalize("unknown-ticket"));
```

- [ ] **Step 2: Define the capture ticket**

```csharp
[Serializable]
public sealed class ArtCaptureTicket {
    public string TicketId;
    public string RunId;
    public string ProfileId;
    public string TargetId;
    public ArtCaptureRole Role;
    public string IterationId;
    public string OutputFolder;
    public string ScreenshotFileName;
    public string ExpectedAbsolutePath;
    public string CaptureSource = "game_view";
    public string Camera;
    public string CreatedAt;
    public string Status;
}
```

- [ ] **Step 3: Implement prepare response**

```csharp
return new {
    capture_ticket_id = ticket.TicketId,
    mcp_tool = "manage_camera",
    mcp_arguments = new {
        action = "screenshot",
        capture_source = "game_view",
        include_image = true,
        max_resolution = 1280,
        output_folder = ticket.OutputFolder,
        screenshot_file_name = ticket.ScreenshotFileName,
        screenshot_super_size = 1
    }
};
```

Do not include a `camera` field. The standard MCP ScreenCapture route is required so Screen Space Overlay UI remains visible.

- [ ] **Step 4: Implement finalize validation**

```csharp
if (!File.Exists(ticket.ExpectedAbsolutePath)) throw new FileNotFoundException("art_blocked:mcp_capture_missing");
var bytes = File.ReadAllBytes(ticket.ExpectedAbsolutePath);
if (bytes.Length == 0) throw new InvalidDataException("art_blocked:mcp_capture_empty");
var texture = new Texture2D(2, 2);
if (!texture.LoadImage(bytes)) throw new InvalidDataException("art_failed:invalid_png");
if (texture.width != target.ReferenceWidth || texture.height != target.ReferenceHeight)
    throw new InvalidDataException("art_failed:unexpected_capture_dimensions");
```

Finalize moves the file into `art-runs/<RunID>/screenshots/<TargetID>/<role>.png`, calculates SHA-256, writes the artifact to the step result, marks the ticket complete, and removes the empty staging directory.

- [ ] **Step 5: Test the real MCP sequence**

```text
p3_art_prepare_capture
-> mcp__unityMCP__manage_camera(action=screenshot, capture_source=game_view, camera omitted)
-> p3_art_finalize_capture
```

Expected: final artifact has 1920x1080 dimensions, non-zero size, SHA-256, target ID, role and current Unity instance ID.

- [ ] **Step 6: Run tests and commit**

```powershell
dotnet build UnityClient/Assembly-CSharp-Editor.csproj --no-restore
rg -n '"source_path"|before_path|after_path' UnityClient/Assets/Scripts/Editor/P3ValidationCore
```

Expected: build 0 errors; search returns no MCP request parameters.

Commit:

```powershell
git add -- UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtCapture* UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtEvidenceService.cs UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3Art*CaptureTool.cs UnityClient/Assets/Scripts/Editor/Tests/P3ArtCaptureTicketSmokeTest.cs UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtTools.cs
git commit -m "feat: capture P3 art evidence through MCP tickets"
```

---

### Task 3: Add Live Target Inspection and Structured Diagnosis

**Files:**
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtInspectionContracts.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtLiveInspectionService.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtInspectTargetTool.cs`
- Refactor: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtOpenTargetTool.cs`
- Test: `UnityClient/Assets/Scripts/Editor/Tests/P3ArtLiveInspectionSmokeTest.cs`

**Interfaces:**
- `p3_art_open_target` verifies that the expected registered root is currently reachable; it does not fabricate state.
- `p3_art_inspect_target` returns a bounded component snapshot and a list of machine-detectable issues.
- Skill combines this structure with the inline MCP Game View image for visual judgment.

- [ ] **Step 1: Write failing reachability and bounded-payload tests**

```csharp
var result = ArtLiveInspectionService.Inspect("workshop_main", maxNodes: 200);
if (!result.Reachable) throw new Exception("registered root not reachable");
if (result.Nodes.Count > 200) throw new Exception("inspection payload unbounded");
AssertThrows(() => ArtLiveInspectionService.Inspect("unknown", 200));
```

- [ ] **Step 2: Define diagnosis types**

```csharp
public enum ArtIssueCategory {
    Layout, Hierarchy, Typography, Color, SpriteBinding, VisualId,
    Mask, Raycast, RuntimeState, ProgramReachability, SemanticVisual, CommercialPolish
}

[Serializable]
public sealed class ArtInspectionIssue {
    public string IssueId;
    public string TargetId;
    public ArtIssueCategory Category;
    public string Severity;
    public string Observation;
    public List<string> SuspectedComponents = new List<string>();
    public bool Blocking;
}
```

- [ ] **Step 3: Inspect only relevant UGUI components**

For each active descendant under the registered root, collect:

```csharp
new ArtUiNodeSnapshot {
    Path = BuildPath(transform),
    Active = gameObject.activeInHierarchy,
    Rect = Snapshot(rectTransform),
    Graphic = SnapshotGraphic(graphic),
    CanvasGroup = SnapshotCanvasGroup(canvasGroup),
    RaycastTarget = graphic != null && graphic.raycastTarget
};
```

Do not serialize arbitrary MonoBehaviour fields.

- [ ] **Step 4: Add deterministic technical checks**

Generate issues for:

```text
missing expected root
missing required VisualID
CanvasScaler not 1920x1080 ScaleWithScreenSize
visible Graphic with missing sprite
alpha <= 0.01 on expected visible root
full-screen CanvasGroup blocking raycasts unexpectedly
RectTransform with NaN or infinite values
```

Semantic layout and commercial polish remain agent/main-art judgments from the live image, not machine pass/fail rules.

- [ ] **Step 5: Run tests and commit**

```powershell
dotnet build UnityClient/Assembly-CSharp-Editor.csproj --no-restore
Set-Content UnityClient/Logs/.test_trigger "P3ArtLiveInspectionSmokeTest.Run"
```

Expected: build 0 errors and focused smoke `PASSED`.

Commit:

```powershell
git add -- UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtInspection* UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtLiveInspectionService.cs UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtOpenTargetTool.cs UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtInspectTargetTool.cs UnityClient/Assets/Scripts/Editor/Tests/P3ArtLiveInspectionSmokeTest.cs
git commit -m "feat: inspect P3 runtime art targets live"
```

---

### Task 4: Rebuild the Art Profile State Machine

**Files:**
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtValidationOrchestrator.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtRunProfileTool.cs`
- Delete: `ArtValidationOrchestrator` from monolithic `P3ArtValidation.cs`
- Test: `UnityClient/Assets/Scripts/Editor/Tests/P3ArtProfileStateMachineSmokeTest.cs`

**Interfaces:**
- Produces state sequence `Created -> Preflight -> TargetNavigation -> LiveInspection -> CaptureDecision -> FinalCapture -> EvidenceValidation -> ExternalReviewRequired -> Complete`.
- The orchestrator coordinates project steps; the Skill performs the standard MCP Game View image call between polling states.

- [ ] **Step 1: Write state transition tests**

```csharp
var job = ArtValidationOrchestrator.Start("art_run", "art_focus", new[] { "workshop_main" }, "instance");
AssertEqual("AwaitingLiveInspection", job.Status);
ArtValidationOrchestrator.RecordInspection("art_run", inspection);
AssertEqual("AwaitingCaptureDecision", JobStore.Load("art_run").Status);
AssertThrows(() => ArtValidationOrchestrator.Complete("art_run"));
```

- [ ] **Step 2: Define explicit polling actions**

```text
start
status
record_inspection
record_diagnosis
record_capture
complete
cancel
```

No action accepts arbitrary C#, file path or state method.

- [ ] **Step 3: Implement capture decision rules**

```csharp
if (diagnosis.HasPersistedChanges) Require(ArtCaptureRole.Before, ArtCaptureRole.After);
else if (diagnosis.HasBlockingIssue) Require(ArtCaptureRole.Issue);
else Require(profile.Mode == "seal" ? ArtCaptureRole.Seal : ArtCaptureRole.Final);
```

- [ ] **Step 4: Enforce external review**

```csharp
job.Status = "Complete";
job.AutomationStatus = "Passed";
job.ExternalReview = "Required";
job.ClaimCeiling = "evidence_collected";
```

- [ ] **Step 5: Run success and controlled-failure fixtures**

Cover:

```text
art_focus final-only success
art_focus unreachable -> ProgramHandoff
art_runtime one issue and one final
t0_art_seal missing seal capture -> Blocked
completion attempted before capture -> rejected
```

- [ ] **Step 6: Commit**

```powershell
git add -- UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtValidationOrchestrator.cs UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtRunProfileTool.cs UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtValidation.cs UnityClient/Assets/Scripts/Editor/Tests/P3ArtProfileStateMachineSmokeTest.cs
git commit -m "feat: orchestrate live-first P3 art profiles"
```

---

### Task 5: Separate Iteration Preview from Persisted Validation

**Files:**
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtIterationContracts.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtIterationService.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtPersistAdapterRegistry.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtCompareIterationTool.cs`
- Delete iteration implementation from `P3ArtValidation.cs`
- Test: `UnityClient/Assets/Scripts/Editor/Tests/P3ArtIterationV2SmokeTest.cs`

**Interfaces:**
- `preview` mutates only the current registered runtime object and never satisfies `persisted_changes`.
- `persist` routes through a registered target adapter and records the durable source changed.
- An iteration can complete only after PlayMode reload and an MCP `after` capture ticket is finalized.

- [ ] **Step 1: Write preview/persist separation tests**

```csharp
var preview = ArtIterationService.Preview(request);
if (preview.SatisfiesFormalValidation) throw new Exception("preview claimed formal validation");
AssertThrows(() => ArtIterationService.Persist(requestWithoutAdapter));
AssertThrows(() => ArtIterationService.Complete(iterationIdWithoutAfterCapture));
```

- [ ] **Step 2: Define typed request**

```csharp
[Serializable]
public sealed class ArtIterationRequest {
    public string RunId;
    public string IterationId;
    public string TargetId;
    public string ComponentPath;
    public ArtIterationAction Action;
    public Vector2 Vector2Value;
    public Color ColorValue;
    public float FloatValue;
    public int IntValue;
    public bool BoolValue;
    public string VisualId;
}
```

- [ ] **Step 3: Implement registered persist adapters**

```csharp
public interface IArtPersistAdapter {
    string TargetId { get; }
    ArtPersistResult Apply(ArtIterationRequest request);
}
```

`ArtPersistAdapterRegistry.Get(targetId)` must reject targets with no adapter using `art_blocked:persist_adapter_missing`. Adapters may edit only a registry-owned Prefab or builder source and must return its exact project-relative path.

- [ ] **Step 4: Validate Approved VisualID**

```csharp
var registry = Resources.Load<VisualAssetRegistry>("VisualAssetRegistry");
if (registry == null || !registry.Entries.Exists(x => x.VisualID == request.VisualId))
    throw new InvalidOperationException("art_blocked:visual_id_not_registered");
```

Do not accept asset paths in the request.

- [ ] **Step 5: Enforce the formal iteration sequence**

```text
finalize before ticket
-> optional preview
-> persist through adapter
-> exit/re-enter PlayMode
-> inspect target again
-> finalize after ticket
-> collect Console delta
-> complete iteration
```

- [ ] **Step 6: Run tests and commit**

```powershell
dotnet build UnityClient/Assembly-CSharp-Editor.csproj --no-restore
rg -n 'source_path|before_path|after_path|AssetDatabase.LoadAssetAtPath.*request' UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art
```

Expected: build 0 errors and no arbitrary path consumption.

Commit:

```powershell
git add -- UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtIteration* UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtPersistAdapterRegistry.cs UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtCompareIterationTool.cs UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtValidation.cs UnityClient/Assets/Scripts/Editor/Tests/P3ArtIterationV2SmokeTest.cs
git commit -m "feat: separate P3 art preview from persisted validation"
```

---

### Task 6: Add art_regression Adapter for the Legacy Runner

**Files:**
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtAcceptanceAdapter.cs`
- Create: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtRunRegressionTool.cs`
- Modify: `UnityClient/Assets/Scripts/ArtAcceptance/ArtAcceptanceRunner.cs`
- Test: `UnityClient/Assets/Scripts/Editor/Tests/P3ArtRegressionAdapterSmokeTest.cs`

**Interfaces:**
- Starts `ArtAcceptanceRunner` directly through a registered adapter, not `.art_acceptance_trigger`.
- Waits for an exact source RunID and imports its report, UI snapshot, Registry snapshot and screenshots into `art-runs/<ArtRunID>/regression`.

- [ ] **Step 1: Add a completion callback contract**

```csharp
public static event Action<string, string> AutomatedRunCompleted;
```

Invoke it after outputs are fully written:

```csharp
AutomatedRunCompleted?.Invoke(_runID, _outputRoot);
```

- [ ] **Step 2: Write stale-run rejection tests**

```csharp
adapter.Start(artRunId);
adapter.OnRunnerCompleted("different-run", oldRoot);
AssertEqual("Running", JobStore.Load(artRunId).Status);
adapter.OnRunnerCompleted(expectedSourceRun, expectedRoot);
AssertEqual("Importing", JobStore.Load(artRunId).Status);
```

- [ ] **Step 3: Import exact artifacts**

Require:

```text
report.json
ui_snapshot.json
registry_snapshot.json
acceptance_checklist.md
at least one non-empty PNG
```

Validate timestamp, dimensions, size and SHA-256 before writing `regression_import/result.json`.

- [ ] **Step 4: Preserve compatibility**

Do not delete:

```text
UnityClient/Logs/.art_acceptance_trigger
ArtAcceptanceEditorDaemon
existing menu items
```

The Skill and `art_regression` use the direct adapter; human/CI callers may continue using the old trigger.

- [ ] **Step 5: Run adapter tests and commit**

```powershell
dotnet build UnityClient/Assembly-CSharp-Editor.csproj --no-restore
Set-Content UnityClient/Logs/.test_trigger "P3ArtRegressionAdapterSmokeTest.Run"
```

Commit:

```powershell
git add -- UnityClient/Assets/Scripts/Editor/P3ValidationCore/Art/P3ArtAcceptanceAdapter.cs UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtRunRegressionTool.cs UnityClient/Assets/Scripts/ArtAcceptance/ArtAcceptanceRunner.cs UnityClient/Assets/Scripts/Editor/Tests/P3ArtRegressionAdapterSmokeTest.cs
git commit -m "feat: adapt legacy ArtAcceptance as P3 art regression"
```

---

### Task 7: Rewrite the p3-art-validation Skill for Live MCP Inspection

**Files:**
- Modify: `.codex/skills/p3-art-validation/SKILL.md`
- Modify: `.codex/skills/p3-art-validation/references/profile-routing.md`
- Modify: `.codex/skills/p3-art-validation/references/iteration-boundaries.md`
- Create: `.codex/skills/p3-art-validation/references/mcp-live-inspection.md`
- Create: `.codex/skills/p3-art-validation/references/evidence-policy.md`

**Interfaces:**
- Formalizes the exact standard-MCP/project-MCP sequence.
- Makes `manage_camera` live view and capture the default.
- Makes `art_regression` the only Profile that starts the legacy runner.

- [ ] **Step 1: Add pressure scenarios**

Validate that the Skill rejects these requests:

```text
"Run full ArtAcceptance after this one button color change."
"Use a screenshot from C:\temp as the final evidence."
"The MCP image looks fine, mark art externally approved."
"The target is unreachable; fabricate player state and open it."
```

- [ ] **Step 2: Write the exact live inspection sequence**

```text
read art facts
-> select Profile and registered TargetID
-> create ArtRunID
-> set_active_instance
-> p3_validation_readiness
-> p3_art_open_target
-> p3_art_inspect_target
-> manage_camera(action=screenshot, capture_source=game_view, include_image=true)
-> diagnose from image plus structured snapshot
-> p3_art_prepare_capture when a decision artifact is needed
-> manage_camera with returned exact folder/name
-> p3_art_finalize_capture
-> p3_art_run_profile complete
-> merge ArtRunID
-> request external review
```

- [ ] **Step 3: State screenshot policy**

```text
art_focus pass: final only
issue without modification: issue only
persisted modification: before and after
t0_art_seal: every registered seal target
art_regression: complete runner evidence
```

- [ ] **Step 4: Validate Skill**

```powershell
python C:\Users\WhiteSheep\.codex\skills\.system\skill-creator\scripts\quick_validate.py .codex/skills/p3-art-validation
rg -n 'source_path|before_path|after_path|full ArtAcceptance.*default' .codex/skills/p3-art-validation
```

Expected: Skill valid; no arbitrary screenshot path or default full-run instruction.

- [ ] **Step 5: Commit**

```powershell
git add -- .codex/skills/p3-art-validation
git commit -m "docs: make P3 art validation MCP live-first"
```

---

### Task 8: End-to-End Evidence, Documentation and Status Writeback

**Files:**
- Modify: `tools/agent/p3-validation-core/Test-P3ValidationCore.ps1`
- Modify: `开发文档/19_UnityMCP验收编排层设计.md`
- Modify: `开发文档/14_Unity运行时美术自动验收方案.md`
- Modify: `开发文档/rules/04_自动化测试与验收流程规范.md`
- Modify: `tools/agent/README.md`
- Modify: `agent_status/program.md`
- Modify: `agent_status/art.md`
- Modify: `知识库/views/program.md`
- Modify: `知识库/views/art.md`
- Generate: `DOCS_INDEX.md`
- Generate: `docs_index.json`

**Interfaces:**
- Produces one real or explicitly limited ArtRunID for each V2 Profile.
- Demonstrates that日常 Profile never invokes the legacy runner and formal captures come from MCP tickets.

- [ ] **Step 1: Run art_focus live-first success**

```text
open registered target
-> inspect target
-> view inline MCP game_view image
-> prepare final ticket
-> manage_camera screenshot
-> finalize ticket
-> complete Profile
```

Expected: one `final.png`, no regression folder, `ExternalReview=Required`.

- [ ] **Step 2: Run controlled failure cases**

Cover:

```text
unknown target -> Blocked
unreachable target -> ProgramHandoff
arbitrary source path -> rejected
missing MCP output -> Blocked
wrong dimensions -> Failed
complete without final/issue evidence -> rejected
```

- [ ] **Step 3: Run art_iteration**

Expected evidence:

```text
before.png
diagnosis.json
preview-changes.json
persisted-changes.json
after.png
console-delta.json
result.json
```

Confirm preview alone cannot complete the Profile.

- [ ] **Step 4: Run art_regression**

Expected: direct adapter invokes `ArtAcceptanceRunner`, imports exact source RunID evidence and does not touch ProgramRunID.

- [ ] **Step 5: Verify MCP discovery**

Expected project tools:

```text
p3_art_open_target
p3_art_inspect_target
p3_art_prepare_capture
p3_art_finalize_capture
p3_art_compare_iteration
p3_art_run_profile
p3_art_run_regression
```

Expected standard tool used for images:

```text
mcp__unityMCP__manage_camera action=screenshot capture_source=game_view camera omitted
```

- [ ] **Step 6: Run final verification**

```powershell
.\tools\agent\p3-validation-core\Test-P3ValidationCore.ps1
dotnet build UnityClient/Assembly-CSharp.csproj --no-restore
dotnet build UnityClient/Assembly-CSharp-Editor.csproj --no-restore
python C:\Users\WhiteSheep\.codex\skills\.system\skill-creator\scripts\quick_validate.py .codex/skills/p3-art-validation
rg -n '"source_path"|before_path|after_path' UnityClient/Assets/Scripts/Editor/P3ValidationCore .codex/skills/p3-art-validation
.\tools\docs\Generate-DocsIndex.ps1
.\tools\docs\Validate-Docs.ps1
.\tools\agent\Invoke-AgentHealthCheck.ps1 -Strict
```

Expected: core tests and Skill pass; both builds have 0 errors; forbidden path parameters absent; docs pass. Pre-existing unrelated dirty-worktree warnings must be recorded but must not be staged.

- [ ] **Step 7: Update status and commit**

Status writeback must state:

```text
日常美术验收默认 MCP live-first
正式截图由 manage_camera + capture ticket 产生
旧 ArtAcceptanceRunner 只用于 art_regression
机器通过仍需要主美外部复核
真实 ArtRunID 证据路径和任何 validation_limited
```

Commit:

```powershell
git add -- tools/agent/p3-validation-core/Test-P3ValidationCore.ps1 开发文档/19_UnityMCP验收编排层设计.md 开发文档/14_Unity运行时美术自动验收方案.md 开发文档/rules/04_自动化测试与验收流程规范.md tools/agent/README.md agent_status/program.md agent_status/art.md 知识库/views/program.md 知识库/views/art.md DOCS_INDEX.md docs_index.json
git commit -m "docs: switch P3 art validation to MCP live-first"
```

---

## Plan Completion Gate

Implementation is complete only when:

- `art_focus` and `art_runtime` use MCP live inspection rather than the legacy full runner.
- Formal screenshots use standard Unity MCP `manage_camera` with `capture_source=game_view` and omitted `camera`.
- Project tools use prepare/finalize capture tickets and accept no arbitrary screenshot paths.
- Normal single-target pass produces only one final image.
- Persisted iteration requires before/after and a PlayMode reload; preview alone cannot pass.
- Target reachability failures generate program handoff evidence.
- `art_regression` imports the exact legacy runner output into ArtRunID.
- All formal Profile summaries keep `ExternalReview=Required` until actual main-art review.
- Focused tests, both builds, Skill validation, document validation and scoped status writeback pass.
