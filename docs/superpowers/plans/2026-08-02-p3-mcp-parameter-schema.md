---
id: p3_mcp_parameter_schema_plan
title: P3 MCP 参数 Schema 实现计划
type: plan
role: 程序
domain: mcp_validation
status: implemented
source_of_truth: false
related:
  - docs/superpowers/specs/2026-08-02-p3-mcp-parameter-schema-design.md
last_verified: 2026-08-02
update_rule: 实现范围或验证口径变化时同步更新。
---

# P3 MCP Parameter Schema Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Make the 11 parameterized P3 Unity MCP tools expose usable direct-call schemas while keeping the no-argument readiness tool and all Handler behavior unchanged.

**Architecture:** Add the Unity MCP package's supported nested `Parameters` metadata class to each parameterized tool. Keep `HandleCommand(JObject)` as the runtime compatibility boundary. Add an Editor smoke test that reflects the P3 tool classes and verifies the discovery contract without modifying the third-party package.

**Tech Stack:** Unity 2022.3.60f1, C#, Newtonsoft.Json.Linq, unity-mcp `d49ae29535`, P3 Validation Core smoke scripts.

## Global Constraints

- Do not modify `UnityClient/Library/PackageCache` or the `tools/ai-image-gateway` submodule.
- Preserve pure UGUI and the existing program/art validation lane separation.
- Do not change Handler business behavior or P3 asset state transitions.
- `p3_validation_readiness` remains a valid zero-parameter tool.
- Direct MCP invocation, not `execute_custom_tool`, is required for final Schema acceptance.

### Task 1: Add the failing metadata contract smoke test

**Files:**
- Create: `UnityClient/Assets/Scripts/Editor/Tests/P3McpToolSchemaSmokeTest.cs`

**Interfaces:**
- Consumes the 12 P3 tool types and `MCPForUnity.Editor.Tools.ToolParameterAttribute`.
- Produces a reusable `P3McpToolSchemaSmokeTest.Run()` entry point for the existing smoke runner.

- [x] **Step 1: Write the reflection test**

  The test should enumerate the expected tool type/name pairs, read each nested `Parameters` type, and assert that each expected field has a `ToolParameterAttribute`. It must assert `p3_validation_readiness` has no parameters and must fail before the metadata classes exist.

- [x] **Step 2: Run the existing Editor smoke route**

  Run the repository's registered P3 program smoke route after Unity recompiles. Expected before implementation: the new schema smoke reports missing `Parameters` for the parameterized tools.

### Task 2: Add `Parameters` metadata to art tools

**Files:**
- Modify: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtRunProfileTool.cs`
- Modify: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtOpenTargetTool.cs`
- Modify: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtInspectTargetTool.cs`
- Modify: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtPrepareCaptureTool.cs`
- Modify: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtFinalizeCaptureTool.cs`
- Modify: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtCompareIterationTool.cs`
- Modify: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ArtRunRegressionTool.cs`

**Interfaces:**
- Produces the package-discoverable fields `action`, `run_id`, `profile_id`, `target_id`, `target_ids`, `instance_id`, `review`, `max_nodes`, `capture_role`, `iteration_id`, `capture_ticket_id`, and `change` with the names consumed by each existing Handler.

- [x] **Step 1: Add nested classes using `ToolParameterAttribute`**

  Use `string`, `string[]`, `int`, and `object` only. Mark action-independent identifiers required only where the Handler rejects missing input; mark defaults and action-specific fields optional. Do not change the `HandleCommand(JObject)` body.

- [x] **Step 2: Compile and run the schema smoke**

  Expected: all seven art tools expose a non-empty parameter list and the readiness exception remains intact.

### Task 3: Add `Parameters` metadata to program and infrastructure tools

**Files:**
- Modify: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ProgramRunProfileTool.cs`
- Modify: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ProgramRunSmokeTool.cs`
- Modify: `UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools/P3ValidationInfrastructureTools.cs`

**Interfaces:**
- Produces schemas for `p3_program_run_profile`, `p3_program_run_smoke`, `p3_validation_collect_console`, and `p3_validation_collect_evidence`.
- Leaves `p3_validation_readiness` without a nested `Parameters` type.

- [x] **Step 1: Add exact fields read by each Handler**

  Expose `run_id`, `profile_id`, `instance_id`, and `validation_domain` with the existing defaults and required behavior. Do not invent parameters for readiness.

- [x] **Step 2: Compile and run the schema smoke**

  Expected: all 11 parameterized tools pass the contract, readiness passes the zero-parameter assertion.

### Task 4: Verify Unity registration and direct MCP calls

**Files:**
- Modify: `docs/superpowers/plans/2026-08-02-p3-mcp-parameter-schema.md` to record completed checks only if needed.
- Evidence: `UnityClient/Logs/P3Validation/` and current MCP registration output.

**Interfaces:**
- Consumes the compiled Unity Editor and registered MCP connection.
- Produces direct-call evidence showing named parameters are accepted.

- [x] **Step 1: Run health check and Unity compile**

  Run `./tools/agent/Invoke-AgentHealthCheck.ps1`, then use the registered P3 program `smoke_focus` route. Record compile or validation limitations explicitly.

- [x] **Step 2: Reconnect or re-register Unity MCP**

  Confirm `p3_art_run_profile` and representative tools no longer declare `args: {}`; confirm readiness remains no-argument.

- [x] **Step 3: Invoke a real direct tool call**

  Call `p3_art_run_profile` with named `action` and `run_id` arguments. A successful response or a handler-level validation error proves the request crossed the Schema boundary; `Unexpected keyword argument` is failure.

- [x] **Step 4: Run strict health check and inspect git status**

  Run `./tools/agent/Invoke-AgentHealthCheck.ps1 -Strict` and `git status --short`. Only task files may be staged.

### Task 5: Commit the implementation

- [x] **Step 1: Commit implementation and test**

  ```powershell
  git add UnityClient/Assets/Scripts/Editor/P3ValidationCore/Tools UnityClient/Assets/Scripts/Editor/Tests/P3McpToolSchemaSmokeTest.cs
  git commit -m "fix: expose P3 MCP tool parameter schemas"
  ```
