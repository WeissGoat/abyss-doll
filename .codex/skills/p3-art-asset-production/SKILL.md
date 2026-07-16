---
name: p3-art-asset-production
description: Use when Project P3 needs to turn an admitted art requirement into a production-ready runtime asset through Manifest planning, image generation delegation, preprocessing, agent candidate review, interactive or automatic selection, Approved synchronization, Unity import, Registry integration, runtime validation, evidence collection, and status writeback.
---

# P3 Art Asset Production

## Purpose

Turn an existing, sufficiently detailed P3 art requirement into a validated runtime asset. This skill owns the production workflow; it does not replace project art facts, image-generation capabilities, UI design, narrative design, or runtime validation facts.

Capability boundaries:

- Image creation or editing is delegated to `generate-image` without moving production responsibilities into it.
- Narrative comic-page production additionally follows `.codex/skills/p3-narrative-cg-comic/SKILL.md`.
- Runtime art diagnosis and evidence use `.codex/skills/p3-art-validation/SKILL.md`.

## Required reading

1. Read `AGENTS.md`, `PROJECT_STATUS.md`, and `agent_status/art.md`.
2. Read `美术文档/00_美术流水线总览.md`, `01_Manifest规范.md`, `02_资源规格与接入规范.md`, `03_AI生成与筛选规范.md`, and `04_美术风格基准.md`.
3. Read the target asset's active character, UI, CG, configuration, or implementation source.
4. Read all references in this skill before executing a production run:
   - [modes-and-input.md](references/modes-and-input.md)
   - [state-machine.md](references/state-machine.md)
   - [candidate-evaluation.md](references/candidate-evaluation.md)
   - [interaction-gates.md](references/interaction-gates.md)
   - [approved-and-unity.md](references/approved-and-unity.md)
   - [evidence-and-writeback.md](references/evidence-and-writeback.md)

## Modes

- `interactive` is the default. Continue automatically through deterministic and high-confidence decisions; pause only at a configured decision gate or genuine blocker.
- `auto` is enabled only when the user explicitly authorizes fully automatic execution. It may automatically generate, repair, select, sync Approved, integrate, validate, and write back within the locked facts and granted scope.

Full automation never authorizes inventing missing requirements, resolving contradictory facts by preference, changing active art direction, bypassing `.meta` or GUID guards, overwriting unrelated assets, or modifying gameplay/domain rules.

## Core workflow

1. Normalize the request, mode, scope, permissions, limits, and source references.
2. Audit existing facts and state; reject duplicate work and stale evidence.
3. Admit the requirement and lock the VisualID, operation, output path, Asset Contract, quality tier, and claim ceiling.
4. Create a bounded production plan and backend policy.
5. Delegate raw image generation or editing to the image-generation capability skill.
6. Preprocess candidates and apply deterministic technical gates.
7. Inspect and score valid candidates; select, repair, regenerate, switch provider, request a decision, or block.
8. Pass the Approved gate before copying or replacing any formal asset.
9. Refresh Unity, validate import state, rebuild or inspect the appropriate Registry path, and check Console delta.
10. Run focused runtime art validation; use full regression only for broad changes or release evidence.
11. Refresh generated handoffs and write all affected art/program status and evidence entries.

## Execution rules

- Prefer existing project scripts and MCP tools; do not create a second Manifest, Registry, progress table, or acceptance system.
- Require a detailed source before formal production. A chat-only idea may produce exploration candidates, but not a formal Manifest/Approved asset.
- Treat `raw`, `processed`, `selected`, `approved`, `registered`, `runtime_bound`, `player_path_verified`, and `regression_passed` as different states.
- Limit repair/regeneration loops according to the request. Default: 3 rounds, 4 initial variants, 2 repair variants, 2 provider switches.
- In interactive mode, ask a concrete decision question with recommendation, evidence, differences, risks, and resume state. Do not ask the user to repeat facts already available in project sources.
- In auto mode, prefer fact consistency, identity, semantic correctness, engineering safety, style consistency, composition, then decoration.

## Never

- Generate directly from an unreviewed requirement-candidate report.
- Let `_IncomingAI` be referenced by Prefabs, Registry, or runtime code.
- Promote a hard-failed or below-threshold candidate to Approved.
- Change a VisualID, Approved output path, DisplaySpec, `.meta`, GUID, or runtime binding during same-VisualID replacement.
- Treat menu execution success as Registry, import, or acceptance completion.
- Treat a Runner-created screenshot as proof that the normal player path is reachable.
- Run full P0 as part of an art-production task or modify domain rules to make an art target reachable.
