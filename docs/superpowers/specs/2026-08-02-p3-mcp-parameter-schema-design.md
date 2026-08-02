---
id: p3_mcp_parameter_schema_design
title: P3 MCP 参数 Schema 设计
type: design
role: 程序
domain: mcp_validation
status: implemented
source_of_truth: false
related:
  - docs/superpowers/plans/2026-08-02-p3-mcp-parameter-schema.md
last_verified: 2026-08-02
update_rule: MCP 工具发现契约变化时同步更新，并通过 P3 MCP Schema Smoke 验证。
---

# P3 MCP Parameter Schema Design

## Goal

修复 P3 Unity MCP 工具的参数 Schema 为空问题，使 11 个有参数的工具可以被 Codex 直接发现和调用；无参数的 `p3_validation_readiness` 保持空参数 Schema。

## Root Cause

项目使用的 `com.coplaydev.unity-mcp@d49ae29535` / `mcpforunityserver==10.0.0` 的 `ToolDiscoveryService` 只读取工具类内部名为 `Parameters` 的嵌套类型，并导出其中带 `[ToolParameter]` 的公开成员。P3 工具目前只有 `HandleCommand(JObject)`，所以工具名称和描述能注册，但参数数组为空，Codex 端最终得到 `args: {}`。

## Chosen Design

为每个有参数的 `[McpForUnityTool]` P3 工具增加公开嵌套 `Parameters` 类，并为每个实际读取的字段添加 `[ToolParameter]`。现有 `HandleCommand(JObject)` 保持不变，继续负责兼容动态请求和业务校验。参数元数据只描述现有接口，不引入新的业务行为，也不修改第三方 `Library/PackageCache`。

`p3_validation_readiness` 不增加虚假参数；它不读取请求字段，保持无参数工具。

## Parameter Contract

| Tool | Parameters |
|---|---|
| `p3_art_run_profile` | `action`, `run_id`, `profile_id`, `target_id`, `target_ids`, `instance_id`, `review` |
| `p3_art_open_target` | `target_id` |
| `p3_art_inspect_target` | `run_id`, `target_id`, `max_nodes` |
| `p3_art_prepare_capture` | `run_id`, `profile_id`, `target_id`, `capture_role`, `iteration_id` |
| `p3_art_finalize_capture` | `run_id`, `capture_ticket_id` |
| `p3_art_compare_iteration` | `action`, `change` |
| `p3_art_run_regression` | `run_id`, `instance_id` |
| `p3_program_run_profile` | `run_id`, `profile_id`, `instance_id` |
| `p3_program_run_smoke` | `run_id`, `profile_id` |
| `p3_validation_collect_console` | `run_id`, `validation_domain` |
| `p3_validation_collect_evidence` | `run_id`, `validation_domain` |

Object-shaped fields such as `review` and `change` are exposed as `object` because the installed MCP discovery API does not provide nested property schema declarations. Their existing JSON deserialization remains the runtime authority.

Required flags follow current behavior: fields that are explicitly required by the handler are marked required; optional fallbacks (`profile_id`, `instance_id`, `target_ids`, `target_id` alternatives, `max_nodes`, `iteration_id`, and action-specific objects) are marked optional. The implementation must not make a currently optional field mandatory merely to improve prompting.

## Testing and Acceptance

1. Add an Editor-side reflection/discovery regression test or equivalent validation helper that inspects all P3 `[McpForUnityTool]` classes.
2. Assert that the 11 parameterized tools expose the expected parameter names and primitive/object types.
3. Assert that `p3_validation_readiness` remains a valid zero-parameter tool.
4. Compile the Unity project and run the P3 program `smoke_focus` validation lane.
5. Reconnect or re-register Unity MCP and verify the direct tool declaration is no longer `args: {}`.
6. Execute a real direct call such as `p3_art_run_profile(action="status", run_id="...")`; `execute_custom_tool` is only a diagnostic fallback and cannot be the final acceptance evidence.

## Non-Goals

- Do not modify `com.coplaydev.unity-mcp` or `Library/PackageCache`.
- Do not replace the individual P3 tools with a single generic router.
- Do not change art/program validation state transitions or Handler behavior.
- Do not change the separate asset status `registered` terminology.
