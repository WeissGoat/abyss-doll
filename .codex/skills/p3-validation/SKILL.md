---
name: p3-validation
description: Run Project P3 validation through deterministic PowerShell automation plus project-scoped Unity MCP tools, producing RunID-scoped evidence and bounded claims. Use for P3 smoke tests, P0 validation, ArtAcceptance, T0 capture, Unity Console/readiness checks, runtime screenshots, or MCP-based Unity acceptance orchestration.
---

# P3 Validation

Use one registered Profile and preserve its evidence and claim boundaries.

## Required order

1. Read `AGENTS.md`, `PROJECT_STATUS.md`, `agent_status/program.md`, and affected role status pages.
2. Select exactly one Profile using [profile-routing.md](references/profile-routing.md).
3. Pin the intended Unity instance; never rely on an ambiguous active instance.
4. Create the RunID with `tools/agent/p3-validation/New-P3ValidationRun.ps1`.
5. Run `Invoke-P3StaticValidation.ps1` for registered static steps.
6. Call `p3_unity_readiness`; stop on a dirty Scene or dirty Prefab Stage.
7. Call `p3_run_unity_profile` or the registered atomic MCP tool. Poll with the same RunID.
8. Call `p3_collect_unity_evidence`.
9. Run `Merge-P3ValidationEvidence.ps1`.
10. Read `validation-summary.json` and report no claim above `claim_ceiling`.

Read [evidence-and-claims.md](references/evidence-and-claims.md) before reporting completion.

## Safety

- Use only registered Profile, smoke-set, and capture IDs.
- Never use `manage_ui`; P3 runtime UI is pure UGUI.
- Never use `execute_code`, arbitrary reflection names, arbitrary `execute_menu_item`, arbitrary filesystem paths, or external processes for validation.
- Do not change gameplay, configuration facts, UI assets, Approved art, scenes, or Prefabs while validating.
- Do not clear the Unity Console. Use RunID baseline/delta evidence.
- Do not auto-save dirty scenes or Prefab Stages.
- Treat a business `Failed` result as terminal; do not automatically rerun it.

## Fallback

If project-scoped MCP tools are unavailable, record `validation_limited:P3CustomMcpToolsUnavailable`. Offer the existing script, trigger, or menu path as a fallback, but never label the weaker substitute as the MCP Profile passing.
