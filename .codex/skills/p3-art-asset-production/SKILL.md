---
name: p3-art-asset-production
description: Use when Project P3 needs formal production or replacement of runtime art assets, including Manifest-driven standard assets, character portrait sets, portrait differences, Approved admission, and Unity / Registry integration. Runtime UI or art validation itself belongs to p3-art-validation.
---

# P3 Art Asset Production

## Purpose

Turn an existing, sufficiently detailed P3 art requirement into a validated runtime asset. This skill owns the production workflow; it does not replace project art facts, image-generation capabilities, UI design, narrative design, or runtime validation facts.

Capability boundaries:

- Image creation or editing is delegated to `p3-generate-image` without moving production responsibilities into it.
- Narrative comic-page production additionally follows `.codex/skills/p3-narrative-cg-comic/SKILL.md`.
- Runtime art diagnosis and evidence use `.codex/skills/p3-art-validation/SKILL.md`.
- A production profile selects workspace layout, orchestration, interaction, and acceptance behavior. It never fixes the generation method, provider, or tool.

## Required reading

Read in layers instead of front-loading every document. The Core workflow, Execution rules, and Never lists in this file apply in every phase.

1. Before starting: `AGENTS.md`, `PROJECT_STATUS.md`, `agent_status/art.md`, `美术文档/00_美术流水线总览.md`, and the target asset's active character, UI, CG, configuration, or implementation source.
2. Before entering a phase, read what it needs; skip anything already read in this session:

| Phase (state-machine states) | Read before entering |
|---|---|
| Intake: `SOURCE_AUDIT` → `PRODUCTION_PLAN` | [modes-and-input.md](references/modes-and-input.md), [state-machine.md](references/state-machine.md), [workspace-profiles-and-character-portraits.md](references/workspace-profiles-and-character-portraits.md); `美术文档/01_Manifest规范.md` when admitting a new requirement or editing the Manifest / seed; `美术文档/02_资源规格与接入规范.md` when a new asset needs output or display specs |
| Prompt authoring and generation: `BACKEND_PREFLIGHT` → `OUTPUT_CONTRACT_AUDIT` | `美术文档/04_美术风格基准.md`, the "Persisted generation requests" section below, and `p3-generate-image` |
| Processing, review, and selection: `PREPROCESS` → `REPAIR_OR_REGENERATE` | `美术文档/03_AI生成与筛选规范.md`, `美术文档/04_美术风格基准.md`, [candidate-evaluation.md](references/candidate-evaluation.md), [interaction-gates.md](references/interaction-gates.md) |
| Approved, Unity, and Registry: `APPROVED_GATE` → `REGISTRY_INTEGRATION` | `美术文档/02_资源规格与接入规范.md`, [approved-and-unity.md](references/approved-and-unity.md) |
| Runtime validation: `RUNTIME_VALIDATION` | `p3-art-validation` |
| Resume, evidence, and writeback: any `-Resume`, `WRITEBACK` | [evidence-and-writeback.md](references/evidence-and-writeback.md) |

3. Before any pause or user question, whatever the phase, read [interaction-gates.md](references/interaction-gates.md).

## Modes

- `interactive` is the default. Continue automatically through deterministic and high-confidence decisions; pause only at a configured decision gate or genuine blocker.
- `auto` is enabled only when the user explicitly authorizes fully automatic execution. It may automatically generate, repair, select, sync Approved, integrate, validate, and write back within the locked facts and granted scope.
- The PowerShell portrait wrapper exposes this same mode as `-ExecutionMode Automatic`; the Python entry point uses `--execution-mode automatic`. Both still stop when required evidence or authority is missing.

Full automation never authorizes inventing missing requirements, resolving contradictory facts by preference, changing active art direction, bypassing `.meta` or GUID guards, overwriting unrelated assets, or modifying gameplay/domain rules.

## Core workflow

1. Normalize the request, production profile, mode, scope, permissions, limits, and source references.
2. Audit existing facts and state; reject duplicate work and stale evidence.
3. Admit the requirement and lock the VisualID, operation, output path, method-neutral Asset Contract, quality tier, and claim ceiling.
4. Resolve the profile workspace and create a bounded, run-scoped production plan from the currently available tools and evidence.
5. Compile the deterministic Requirement. When prompt authoring is required, export the authoring package and let the Agent publish an immutable dual-format PromptRevision before any provider call.
6. Delegate image creation, editing, imported-source handling, or deterministic processing to the appropriate current capability without hard-coding a method in the requirement.
7. Preprocess candidates and apply deterministic technical gates.
8. Inspect and score valid candidates; select, adjust, retry with another current capability, request a decision, or block.
9. Pass the Approved gate before copying or replacing any formal asset.
10. Refresh Unity, validate import state, rebuild or inspect the appropriate Registry path, and check Console delta.
11. Run focused runtime art validation; use full regression only for broad changes or release evidence.
12. Refresh generated handoffs and write all affected art/program status and evidence entries.

## Persisted generation requests

Manifest prose fields are migration inputs, not the execution source. Before a formal generation run, compile and validate the persisted catalog:

```powershell
.\tools\美术工具\Compile-ArtGenerationRequests.ps1 -ManifestPath 美术文档/_generated/art_manifest.json
python tools/美术工具/validate_art_generation_requests.py `
  --manifest 美术文档/_generated/art_manifest.json `
  --request-catalog 美术文档/_generated/art_generation_requests.json `
  --strict
```

Each Manifest entry points to `CompiledRequest.RequestID`, `RequirementFingerprint`, `PromptAuthoringStatus`, and `ActivePromptRevisionID`. The compiler stores `PromptAuthoringContext`, `TechnicalRequest`, and `PreservationContract`; it never writes an executable final Prompt. If authoring is required, use `Export-ArtPromptAuthoringPackage.ps1`, let the Agent independently author `natural_language_v2` and `danbooru_tags_v2`, then publish with `Publish-ArtPromptRevision.ps1`. A ready Variant must map every hard constraint; an honestly unavailable format is `unsupported` with a reason.

`Run-ArtProductionBatch.ps1`, `Run-CharacterPortraitSet.ps1`, and `Run-ArtGeneration.ps1` consume the exact active Revision and write `RequirementSnapshot`, `PromptRevisionID`, `PromptRevisionFingerprint`, `PromptRevisionSnapshot`, `PromptFormat`, and `ProviderRequest` to evidence. They fail closed on missing/stale Requirements, pointer mismatch, `prompt_authoring_required`, invalid Revision, unavailable Variant, or incomplete constraint mapping. Lifecycle and processing evidence is excluded from the Requirement fingerprint, so normal state transitions and numeric rounds do not stale an unchanged requirement; semantic contract changes do. Provider adapters may serialize only: they must not append quality phrases, rewrite natural language, duplicate Prompt text, or rebuild preservation/change instructions.

Formal generation evidence uses `EvidenceMode=formal_v2` and keeps the exact published `PromptRevisionSnapshot` plus `ProviderRequest`; it never copies a second prompt representation. Historical snapshots are read-only evidence, not an executable fallback. Generation evidence alone never lets the image capability advance `selected`, `Approved`, `registered`, or `runtime_validated`.

The standard batch route rejects `character_portrait_set`; portrait members are planned and ordered by `AssetSetID`, `SetRole`, and explicit `SourceAssets` in the independent portrait-set executor. The profile remains method-neutral: the Agent chooses the current image capability after reading the compiled brief and records that choice in run evidence.

For a resumable portrait run, `Run-CharacterPortraitSet.ps1` persists `portrait-set-run.json` and immutable per-member generation snapshots. The effective portrait order is:

```text
generation snapshot
  -> processing decision
  -> visual-review evidence gate
  -> Registrar technical recomputation and next processed/<n> round
  -> guarded selection
```

The portrait Registrar intentionally requires `visual_review.json` before publishing a passed `character_portrait_set` round. Missing processing decisions or review evidence write `PendingDecision` and stop; `Automatic` does not invent a processing method, visual score, or review. Resume revalidates PromptRevision, reference, raw, processed, and selected hashes before skipping any child operation.

## Execution rules

- Prefer existing project scripts and MCP tools; do not create a second Manifest, Registry, progress table, or acceptance system.
- For Manifest batch plans, use `Run-ArtProductionBatch.ps1` to isolate runtime provider routes and advance only through the latest numeric processing round. The source plan remains method-neutral; a successful child exit code without current-batch decodable raw evidence is still a generation failure.
- When a batch item is `ui_skin` with `ProcessSpec.NineSlice.Enabled=true`, require an explicit runtime capability route and use the specialized adapter selected for that Run before the common optimizer. Record the actual capability in generation/round evidence; keep the Manifest, Asset Contract, VisualID, and workspace method-neutral.
- After Agent visual review, use `Select-ArtCandidate.ps1` for guarded promotion into `selected/`. Do not manually copy candidates around latest-round, hash, score, overwrite-permission, or status-preservation checks.
- Treat Registrar output as canonical technical evidence: it recomputes `technical_review_v2` from the real candidate and Manifest Spec, verifies `ReviewFingerprint`, and records `AutomaticStatus` plus `AppliedOverrides`. A `technical_override.json` never grants itself permission; each allowed RuleID must come from explicit user/calling-workflow authorization and also be passed through `-AllowTechnicalOverride` / `--allow-technical-override`.
- For an existing `selected/` target, require the review's current `ReplacementBaseline` and `ReplacementPolicy`. The candidate must clear the selected threshold, strictly exceed the baseline by the configured delta, and not regress protected dimensions before `-AllowSelectedOverwrite` can authorize the copy. Same-SHA input is idempotent and returns `already_selected`.
- A `character_portrait_set` replacement additionally uses `character_portrait_v3`: independently evidenced `Identity`, `Costume`, `Proportion`, `Framing`, `Technical`, and `TargetFit`. Legacy or incomplete baselines stop at `replacement_baseline_review_required`; all five default protected dimensions are mandatory, and the previous selected bytes are preserved under the review evidence before overwrite.
- Before a character portrait member can sync to Approved, run `Invoke-PortraitSetGate.ps1 -Phase Prepare`, review the exact `SetSnapshotFingerprint`, then Finalize only a genuinely passed set review. A stale, missing, or failed set review blocks Approved/Unity planning; standard assets are unaffected. A retrospective set failure retains `registered` but cannot claim `runtime_validated`.
- Require a detailed source before formal production. A chat-only idea may produce exploration candidates, but not a formal Manifest/Approved asset.
- Route working files through exactly one active profile: `standard_asset` -> `_IncomingAI/standard_assets/<VisualID>/`, or `character_portrait_set` -> `_IncomingAI/character_portraits/<VisualID>/`. Keep non-VisualID historical work only under `_IncomingAI/_legacy_runs/` and never scan it as production input.
- Treat `character_portrait_set` as a requirement, identity, interaction, set-consistency, and acceptance profile. Do not define it by text-to-image, image-to-image, inpaint, any provider, or any closed list of methods.
- Keep requirement and Asset Contract fields method-neutral. Let the Agent inspect current tools and evidence, choose or combine appropriate capabilities per run and round, and record what actually happened in run evidence.
- Use stable `AssetID` for production/design members, `VisualID` only for runtime-consumed assets, `AssetSetID` for related members, and `ProductionRunID` for one execution. Do not create a separate `AnchorID`; use `AssetID` with an anchor role for non-runtime design masters.
- Treat `raw`, `processed`, `selected`, `approved`, `registered`, `runtime_validated`, `player_path_verified`, and `regression_passed` as different public claims. Runtime binding remains an internal ArtRun check and is never exposed as a separate production state.
- Treat `processed/<positive integer>/` as an immutable processing round. Read only the latest numeric round for processing state; repairs and complex edits must publish the next integer through `Optimize-ArtAssets.ps1` or `Register-ArtProcessingRound.ps1`.
- Require an explicit `BackgroundPolicy`. `AlphaRequired=true` never authorizes implicit background removal.
- For RGB provider output with a baked checkerboard, prefer an explicit mask or the optional `segmentation` route; threshold-based `connected_border` processing is not a substitute when it can leave checker pixels or delete protected clothing/hair.
- Stop on a latest `failed`, `decision_required`, `legacy_unverified`, or multi-pass-candidate round. Never fall back to an older round or write complex edits directly into `selected/` or Approved.
- For `character_portrait_set`, stop before Registrar when the latest member lacks a valid processing decision or visual-review evidence; do not publish a passed round first and ask for review afterward.
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
- Treat a `technical_override.json`, handwritten `decision.json`, or stale `visual-review.json` as sufficient evidence without the Registrar recomputation and SHA checks.
- Treat a Runner-created screenshot as proof that the normal player path is reachable.
- Run full P0 as part of an art-production task or modify domain rules to make an art target reachable.
