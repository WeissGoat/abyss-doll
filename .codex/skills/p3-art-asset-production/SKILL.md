---
name: p3-art-asset-production
description: Use when Project P3 needs formal production or replacement of runtime art assets, including Manifest-driven standard assets, character portrait sets, portrait differences, Approved admission, Unity integration, or runtime art validation.
---

# P3 Art Asset Production

## Purpose

Turn an existing, sufficiently detailed P3 art requirement into a validated runtime asset. This skill owns the production workflow; it does not replace project art facts, image-generation capabilities, UI design, narrative design, or runtime validation facts.

Capability boundaries:

- Image creation or editing is delegated to `generate-image` without moving production responsibilities into it.
- Narrative comic-page production additionally follows `.codex/skills/p3-narrative-cg-comic/SKILL.md`.
- Runtime art diagnosis and evidence use `.codex/skills/p3-art-validation/SKILL.md`.
- A production profile selects workspace layout, orchestration, interaction, and acceptance behavior. It never fixes the generation method, provider, or tool.

## Required reading

1. Read `AGENTS.md`, `PROJECT_STATUS.md`, and `agent_status/art.md`.
2. Read `美术文档/00_美术流水线总览.md`, `01_Manifest规范.md`, `02_资源规格与接入规范.md`, `03_AI生成与筛选规范.md`, and `04_美术风格基准.md`.
3. Read the target asset's active character, UI, CG, configuration, or implementation source.
4. Read all references in this skill before executing a production run:
   - [modes-and-input.md](references/modes-and-input.md)
   - [state-machine.md](references/state-machine.md)
   - [candidate-evaluation.md](references/candidate-evaluation.md)
   - [interaction-gates.md](references/interaction-gates.md)
   - [workspace-profiles-and-character-portraits.md](references/workspace-profiles-and-character-portraits.md)
   - [approved-and-unity.md](references/approved-and-unity.md)
   - [evidence-and-writeback.md](references/evidence-and-writeback.md)

## Modes

- `interactive` is the default. Continue automatically through deterministic and high-confidence decisions; pause only at a configured decision gate or genuine blocker.
- `auto` is enabled only when the user explicitly authorizes fully automatic execution. It may automatically generate, repair, select, sync Approved, integrate, validate, and write back within the locked facts and granted scope.

Full automation never authorizes inventing missing requirements, resolving contradictory facts by preference, changing active art direction, bypassing `.meta` or GUID guards, overwriting unrelated assets, or modifying gameplay/domain rules.

## Core workflow

1. Normalize the request, production profile, mode, scope, permissions, limits, and source references.
2. Audit existing facts and state; reject duplicate work and stale evidence.
3. Admit the requirement and lock the VisualID, operation, output path, method-neutral Asset Contract, quality tier, and claim ceiling.
4. Resolve the profile workspace and create a bounded, run-scoped production plan from the currently available tools and evidence.
5. Delegate image creation, editing, imported-source handling, or deterministic processing to the appropriate current capability without hard-coding a method in the requirement.
6. Preprocess candidates and apply deterministic technical gates.
7. Inspect and score valid candidates; select, adjust, retry with another current capability, request a decision, or block.
8. Pass the Approved gate before copying or replacing any formal asset.
9. Refresh Unity, validate import state, rebuild or inspect the appropriate Registry path, and check Console delta.
10. Run focused runtime art validation; use full regression only for broad changes or release evidence.
11. Refresh generated handoffs and write all affected art/program status and evidence entries.

## Execution rules

- Prefer existing project scripts and MCP tools; do not create a second Manifest, Registry, progress table, or acceptance system.
- Require a detailed source before formal production. A chat-only idea may produce exploration candidates, but not a formal Manifest/Approved asset.
- Route working files through exactly one active profile: `standard_asset` -> `_IncomingAI/standard_assets/<VisualID>/`, or `character_portrait_set` -> `_IncomingAI/character_portraits/<VisualID>/`. Keep non-VisualID historical work only under `_IncomingAI/_legacy_runs/` and never scan it as production input.
- Treat `character_portrait_set` as a requirement, identity, interaction, set-consistency, and acceptance profile. Do not define it by text-to-image, image-to-image, inpaint, any provider, or any closed list of methods.
- Keep requirement and Asset Contract fields method-neutral. Let the Agent inspect current tools and evidence, choose or combine appropriate capabilities per run and round, and record what actually happened in run evidence.
- Use stable `AssetID` for production/design members, `VisualID` only for runtime-consumed assets, `AssetSetID` for related members, and `ProductionRunID` for one execution. Do not create a separate `AnchorID`; use `AssetID` with an anchor role for non-runtime design masters.
- Treat `raw`, `processed`, `selected`, `approved`, `registered`, `runtime_bound`, `player_path_verified`, and `regression_passed` as different states.
- Treat `processed/<positive integer>/` as an immutable processing round. Read only the latest numeric round for processing state; repairs and complex edits must publish the next integer through `Optimize-ArtAssets.ps1` or `Register-ArtProcessingRound.ps1`.
- Require an explicit `BackgroundPolicy`. `AlphaRequired=true` never authorizes implicit background removal.
- Stop on a latest `failed`, `decision_required`, `legacy_unverified`, or multi-pass-candidate round. Never fall back to an older round or write complex edits directly into `selected/` or Approved.
- Limit repair/regeneration loops according to the request. Default: 3 rounds, 4 initial variants, 2 repair variants, 2 provider switches.
- In interactive mode, ask a concrete decision question with recommendation, evidence, differences, risks, and resume state. Do not ask the user to repeat facts already available in project sources.
- In auto mode, prefer fact consistency, identity, semantic correctness, engineering safety, style consistency, composition, then decoration.

## Never

- Generate directly from an unreviewed requirement-candidate report.
- Use the legacy flat `_IncomingAI/<VisualID>/` path after the workspace-profile migration is declared complete, or silently fall back between old and new workspace layouts.
- Put generation method names into stable IDs or directory contracts, or reject a useful current/future tool merely because it is not listed in this skill.
- Let `_IncomingAI` be referenced by Prefabs, Registry, or runtime code.
- Promote a hard-failed or below-threshold candidate to Approved.
- Overwrite a published numeric processing round, or bypass its decision/hash evidence.
- Change a VisualID, Approved output path, DisplaySpec, `.meta`, GUID, or runtime binding during same-VisualID replacement.
- Treat menu execution success as Registry, import, or acceptance completion.
- Treat a Runner-created screenshot as proof that the normal player path is reachable.
- Run full P0 as part of an art-production task or modify domain rules to make an art target reachable.
