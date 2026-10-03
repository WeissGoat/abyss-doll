# 正式资产生产流程（原 p3-art-asset-production）

## Purpose

Turn an existing, detailed P3 art requirement into a runtime asset through the project's scripts. This flow owns orchestration only. Art facts stay in `美术文档/`; image creation and editing follow [image-generation.md](image-generation.md); narrative pages also follow [narrative-cg.md](narrative-cg.md); runtime diagnosis and evidence go to `p3-playtest`. A production profile selects workspace, interaction, and acceptance behavior; it never fixes the generation method, provider, or tool.

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

Read only what the current phase needs, after the `AGENTS.md` startup reads; skip anything already read this session. Before any pause or user question, read [interaction-gates.md](interaction-gates.md).

| Phase (state-machine states) | Fast lane | Full lane also reads |
|---|---|---|
| Start | Section 2 of `美术文档/00_美术流水线总览.md`; the target asset's source | The rest of `美术文档/00_美术流水线总览.md` |
| Intake: `SOURCE_AUDIT` → `PRODUCTION_PLAN` | `美术文档/01_Manifest规范.md` only when a VisualID still needs admission | [modes-and-input.md](modes-and-input.md), [state-machine.md](state-machine.md), [workspace-profiles-and-character-portraits.md](workspace-profiles-and-character-portraits.md); `美术文档/01_Manifest规范.md` when admitting a requirement or editing the Manifest / seed; `美术文档/02_资源规格与接入规范.md` when a new asset needs output or display specs |
| Prompt and generation: `BACKEND_PREFLIGHT` → `OUTPUT_CONTRACT_AUDIT` | [prompt-and-generation.md](prompt-and-generation.md), `美术文档/04_美术风格基准.md` | `p3-art` |
| Processing, review, and selection: `PREPROCESS` → `REPAIR_OR_REGENERATE` | [candidate-evaluation.md](candidate-evaluation.md) | `美术文档/03_AI生成与筛选规范.md` |
| Approved, Unity, and Registry: `APPROVED_GATE` → `REGISTRY_INTEGRATION` | [approved-and-unity.md](approved-and-unity.md) | `美术文档/02_资源规格与接入规范.md` |
| Runtime validation: `RUNTIME_VALIDATION` | Deferred | `p3-playtest` |
| Resume, evidence, and writeback: any `-Resume`, `WRITEBACK` | [evidence-and-writeback.md](evidence-and-writeback.md) | Same |

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
| Runtime validation | `p3-playtest` light check (the old `t0_art_seal` / `art_regression` ArtRun profiles are retired with the old validation framework) |

## Modes

`interactive` is the default: continue through deterministic, high-confidence steps and pause only at a gate in [interaction-gates.md](interaction-gates.md). `auto` needs the user's explicit authorization and stays inside the granted scope. Neither mode authorizes inventing missing requirements, resolving contradictory facts by preference, changing active art direction, bypassing `.meta` / GUID guards, overwriting unrelated assets, or changing gameplay or domain rules.

## Hard rules

- Formal production needs a detailed source. A chat idea or an unreviewed requirement-candidate report may produce exploration candidates only. Why: a formal asset locks a VisualID and an art direction, and guessed requirements turn into rework.
- Use the existing scripts and MCP tools. Never create a second Manifest, Registry, progress table, or acceptance system. Why: two sources of truth drift apart.
- Only the scripts write numeric `processed/` rounds, `selected/`, Approved, the Manifest, and the Registry. Never hand-copy a candidate, overwrite a published round, fall back to an older round, or promote a hard-failed candidate. Why: the scripts keep hashes, `.meta` files and evidence in step, and a hand copy breaks them silently.
- A handwritten `decision.json`, a `technical_override.json`, or a stale `visual-review.json` is not evidence by itself; the Registrar recomputation, SHA checks, and explicit override authorization decide. Why: evidence has to match the bytes of the actual file.
- Same-VisualID replacement keeps the VisualID, Approved path, DisplaySpec, `.meta`, GUID, and runtime binding. Why: prefabs and the Registry reference the GUID, so a new one breaks every screen that uses the asset.
- Keep requirements, Asset Contracts, IDs, and directories method-neutral. Record the tool actually used in run evidence, and never reject a useful tool because this skill doesn't list it. Why: tools change much faster than requirements.
- Prefabs, the Registry, and runtime code never reference `_IncomingAI`. Why: `_IncomingAI` isn't in git, so those references break on other machines and in builds.
- `raw`, `processed`, `selected`, `approved`, `registered`, `runtime_validated`, `player_path_verified`, and `regression_passed` are separate claims. A successful menu run is not import or Registry completion, and a Runner screenshot doesn't prove the player path. Why: each claim needs different evidence, and merging them overstates progress.
- Never run full P0 or change domain rules to make an art target reachable. Why: art work must not change gameplay, and full P0 belongs to program validation.
