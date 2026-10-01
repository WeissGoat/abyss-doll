# Prompt authoring and generation

## Persisted generation requests

Manifest prose fields are migration inputs, not the execution source. Before a formal generation run, compile and validate the persisted catalog:

```powershell
.\tools\美术工具\Compile-ArtGenerationRequests.ps1 -ManifestPath 美术文档/_generated/art_manifest.json
python tools/美术工具/validate_art_generation_requests.py `
  --manifest 美术文档/_generated/art_manifest.json `
  --request-catalog 美术文档/_generated/art_generation_requests.json `
  --strict
```

Each Manifest entry points to `CompiledRequest.RequestID`, `RequirementFingerprint`, `PromptAuthoringStatus`, and `ActivePromptRevisionID`. The compiler stores `PromptAuthoringContext`, `TechnicalRequest`, and `PreservationContract`; it never writes an executable final Prompt. If authoring is required, use `Export-ArtPromptAuthoringPackage.ps1`, let the Agent independently author `natural_language_v2` and `danbooru_tags_v2`, then publish with `Publish-ArtPromptRevision.ps1`. A ready Variant must map every hard constraint; an honestly unavailable format is `unsupported` with a reason. One revision file may carry a `Revisions` list for a whole batch.

In the fast lane, author only the format this Run's provider consumes and mark the other `unsupported` with a reason. Publish a new Revision if a later Run switches to the other provider family.

`Run-ArtProductionBatch.ps1`, `Run-CharacterPortraitSet.ps1`, and `Run-ArtGeneration.ps1` consume the exact active Revision and write `RequirementSnapshot`, `PromptRevisionID`, `PromptRevisionFingerprint`, `PromptRevisionSnapshot`, `PromptFormat`, and `ProviderRequest` to evidence. They fail closed on missing/stale Requirements, pointer mismatch, `prompt_authoring_required`, invalid Revision, unavailable Variant, or incomplete constraint mapping. Lifecycle and processing evidence is excluded from the Requirement fingerprint, so normal state transitions and numeric rounds do not stale an unchanged requirement; semantic contract changes do. Provider adapters may serialize only: they must not append quality phrases, rewrite natural language, duplicate Prompt text, or rebuild preservation/change instructions.

Formal generation evidence uses `EvidenceMode=formal_v2` and keeps the exact published `PromptRevisionSnapshot` plus `ProviderRequest`; it never copies a second prompt representation. Historical snapshots are read-only evidence, not an executable fallback. Generation evidence alone never lets the image capability advance `selected`, `Approved`, `registered`, or `runtime_validated`.

## Batch generation

- `Run-ArtProductionBatch.ps1` executes a Manifest batch plan (`缺图生成计划.json` or `FormalV2主动迭代计划.json`) up to the latest numeric processing round. Pass this Run's `AssetClass` to capability routes with `-Route`; the plan stays method-neutral. `-DryRun` prints the groups and commands without writing run evidence.
- Judge a run by its `summary.json` (`FinalState`, `Claims`) and `technical-review.json`, not by child exit codes: a child that exits 0 without decodable raw evidence from the current batch is still a generation failure.
- A `ui_skin` with `ProcessSpec.NineSlice.Enabled=true` needs an explicit capability route and the specialized adapter chosen for that Run before the common optimizer. Record the actual capability in generation and round evidence; keep the Manifest, Asset Contract, VisualID, and workspace method-neutral.
- The batch route rejects `character_portrait_set`. Portrait members are planned and ordered by `AssetSetID`, `SetRole`, and explicit `SourceAssets` in `Run-CharacterPortraitSet.ps1`; its evidence order, `PendingDecision` stops, and resume checks are in [state-machine.md](state-machine.md).
- To pick a provider or capability for a route, use `p3-generate-image` for backend availability and fallback, then record the choice in run evidence.
