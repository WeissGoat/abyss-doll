---
name: p3-art-asset-production
description: Use when Project P3 needs formal production or replacement of runtime art assets, including Manifest-driven standard assets, character portrait sets, portrait differences, Approved admission, and Unity / Registry integration. Runtime UI or art validation itself belongs to p3-art-validation.
---

# P3 Art Asset Production

## Purpose

Turn an existing, detailed P3 art requirement into a runtime asset through the project's scripts. This skill owns orchestration only. Art facts stay in `美术文档/`; image creation and editing go to `p3-generate-image`; narrative pages also follow `p3-narrative-cg-comic`; runtime diagnosis and evidence go to `p3-art-validation`. A production profile selects workspace, interaction, and acceptance behavior; it never fixes the generation method, provider, or tool.

## Lanes

Choose the lane first. The lane definitions are facts in section 2 of `美术文档/00_美术流水线总览.md`; when unsure, take the full lane.

- **Fast lane**: `standard_asset` icons, backgrounds, and ordinary UI images that are not a NineSlice UI skin, not a first-time style anchor, and do not replace an asset a game screen already uses. Claim ceiling `registered`.
- **Full lane**: `character_portrait_set`, narrative CG and comic panels, NineSlice UI skins, first-time anchors, and replacements of assets a game screen already uses. Every phase and gate applies.

Fast lane, one batch at a time:

1. Check that every VisualID has a Manifest entry and a detailed source. Compile the batch, author only the prompt format this Run's provider consumes, and publish one revision file for the batch.
2. Run the batch plan through `Run-ArtProductionBatch.ps1` and read its `summary.json`.
3. Review every VisualID's contact sheet and small-size preview in one pass, write one `visual-review.json` whose `Items` cover the batch, and run `Select-ArtCandidate.ps1` per VisualID with that file.
4. Take the batch through Approved, Unity, and Registry under one `ArtImportRunID`. Approved sync needs the user's go-ahead for the batch unless it was granted up front.
5. Stop only for a hard failure with no valid candidate, a fact conflict, or missing authority. A failing VisualID leaves the batch and the rest continue; repairs stay within the default limits (3 rounds, 2 provider switches).
6. Leave runtime validation to the task in which a game screen first consumes the asset.

## Reading by phase

Read only what the current phase needs, after the `AGENTS.md` startup reads; skip anything already read this session. Before any pause or user question, read [interaction-gates.md](references/interaction-gates.md).

| Phase (state-machine states) | Fast lane | Full lane also reads |
|---|---|---|
| Start | Section 2 of `美术文档/00_美术流水线总览.md`; the target asset's source | The rest of `美术文档/00_美术流水线总览.md` |
| Intake: `SOURCE_AUDIT` → `PRODUCTION_PLAN` | `美术文档/01_Manifest规范.md` only when a VisualID still needs admission | [modes-and-input.md](references/modes-and-input.md), [state-machine.md](references/state-machine.md), [workspace-profiles-and-character-portraits.md](references/workspace-profiles-and-character-portraits.md); `美术文档/01_Manifest规范.md` when admitting a requirement or editing the Manifest / seed; `美术文档/02_资源规格与接入规范.md` when a new asset needs output or display specs |
| Prompt and generation: `BACKEND_PREFLIGHT` → `OUTPUT_CONTRACT_AUDIT` | [prompt-and-generation.md](references/prompt-and-generation.md), `美术文档/04_美术风格基准.md` | `p3-generate-image` |
| Processing, review, and selection: `PREPROCESS` → `REPAIR_OR_REGENERATE` | [candidate-evaluation.md](references/candidate-evaluation.md) | `美术文档/03_AI生成与筛选规范.md` |
| Approved, Unity, and Registry: `APPROVED_GATE` → `REGISTRY_INTEGRATION` | [approved-and-unity.md](references/approved-and-unity.md) | `美术文档/02_资源规格与接入规范.md` |
| Runtime validation: `RUNTIME_VALIDATION` | Deferred | `p3-art-validation` |
| Resume, evidence, and writeback: any `-Resume`, `WRITEBACK` | [evidence-and-writeback.md](references/evidence-and-writeback.md) | Same |

## Commands by phase

Scripts live in `tools/美术工具/`; parameters and examples are in its `README.md`. The scripts enforce the gates, so read their refusals instead of re-checking by hand.

| Phase | Commands |
|---|---|
| Requirements | `Compile-ArtGenerationRequests.ps1 -VisualID <ids>`, then `validate_art_generation_requests.py --strict` |
| Prompt authoring | `Export-ArtPromptAuthoringPackage.ps1 -VisualID <ids>`, author, then `Publish-ArtPromptRevision.ps1 -RevisionPath <file>` |
| Batch plan | `Generate-ArtBatchPlan.ps1` for missing art; `Generate-FormalV2ReplacementPlan.ps1` for same-VisualID replacement |
| Generate and process, standard | `Run-ArtProductionBatch.ps1 -PlanPath <plan> -VisualID <ids> -Route <class>=<capability>`; reprocess with `Optimize-ArtAssets.ps1 -VisualID <ids>` |
| Generate and process, portrait | `Run-CharacterPortraitSet.ps1` (continue with `-Resume`); staged rounds through `Register-ArtProcessingRound.ps1` |
| Select | `Select-ArtCandidate.ps1 -VisualID <id> -ReviewPath <review>` |
| Portrait set gate | `Invoke-PortraitSetGate.ps1 -Phase Prepare`, `Finalize`, or `Check` |
| Approved, Unity, and Registry | `Invoke-ArtApprovedUnityRegistration.ps1 -Phase Plan`, `SyncApproved`, then `Finalize`, with one `-ArtImportRunID` and `-VisualID <ids>` |
| Runtime validation | `p3-art-validation` light check; its `t0_art_seal` and `art_regression` profiles for seals and full regression |

## Modes

`interactive` is the default: continue through deterministic, high-confidence steps and pause only at a gate in [interaction-gates.md](references/interaction-gates.md). `auto` needs the user's explicit authorization and stays inside the granted scope. Neither mode authorizes inventing missing requirements, resolving contradictory facts by preference, changing active art direction, bypassing `.meta` / GUID guards, overwriting unrelated assets, or changing gameplay or domain rules.

## Hard rules

- Formal production needs a detailed source. A chat idea or an unreviewed requirement-candidate report may produce exploration candidates only.
- Use the existing scripts and MCP tools. Never create a second Manifest, Registry, progress table, or acceptance system.
- Only the scripts write numeric `processed/` rounds, `selected/`, Approved, the Manifest, and the Registry. Never hand-copy a candidate, overwrite a published round, fall back to an older round, or promote a hard-failed or below-threshold candidate.
- A handwritten `decision.json`, a `technical_override.json`, or a stale `visual-review.json` is not evidence by itself; the Registrar recomputation, SHA checks, and explicit override authorization decide.
- Same-VisualID replacement keeps the VisualID, Approved path, DisplaySpec, `.meta`, GUID, and runtime binding.
- Keep requirements, Asset Contracts, IDs, and directories method-neutral. Record the tool actually used in run evidence, and never reject a useful tool because this skill doesn't list it.
- Prefabs, the Registry, and runtime code never reference `_IncomingAI`.
- `raw`, `processed`, `selected`, `approved`, `registered`, `runtime_validated`, `player_path_verified`, and `regression_passed` are separate claims. A successful menu run is not import or Registry completion, and a Runner screenshot doesn't prove the player path.
- Never run full P0 or change domain rules to make an art target reachable.
