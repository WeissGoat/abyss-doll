---
id: gemini_portrait_output_contract_plan
title: Gemini 角色立绘输出契约实施计划
type: plan
role: Owner
domain: ai_image_gateway_art_pipeline
status: active
source_of_truth: false
related:
  - docs/specs/2026-08-08-gemini-portrait-output-contract-design.md
last_verified: 2026-08-08
update_rule: 实施文件、测试、真实 smoke、PromptRevision 或 Zero hurt 修复步骤变化时更新本计划。
---

# Gemini Portrait Output Contract Implementation Plan

**Goal:** Move Gemini portrait aspect ratio out of Prompt text into provider-specific request parameters, publish a green-matte `prompt-003`, and safely rerun `doll_zero_hurt` through existing segmentation and replacement gates.

**Architecture:** `OpenAIChatImageProvider` exposes a no-op internal image-parameter hook; `GeminiChatImageProvider` alone serializes a reduced aspect ratio into the relay payload and result evidence. P3 keeps exact PromptRevision text immutable, records provider parameters in generation evidence, then reuses the existing segmentation/rembg and Registrar path.

**Tech Stack:** Python 3.10+, httpx, Pydantic, pytest, unittest, PowerShell 5.1, Pillow, P3 PromptRevision Catalog, P3 portrait Registrar.

## Global Constraints

- Work directly on `main` because the user explicitly requested inline execution and no branch unless requested.
- Preserve unrelated dirty files and stage only files owned by this task.
- Do not expose or commit `tools/ai-image-gateway/config.local.yaml`.
- Do not append provider-specific quality prose or dimensions to published Prompt text.
- Do not add Gemini image parameters to OpenAI or Grok chat providers.
- Do not overwrite immutable PromptRevisions or numeric processing rounds.
- Do not change selected, Approved, `.meta`, GUID, Registry, or `registered` before all replacement gates pass.
- Real provider calls use `count=1`; a second variant is a second request.

---

### Task 1: Add Gemini-Specific Aspect Ratio Serialization

**Files:**
- Modify: `tools/ai-image-gateway/ai_image_gateway/providers/openai_compatible.py`
- Modify: `tools/ai-image-gateway/tests/test_openai_compatible_provider.py`

**Interfaces:**
- Consumes: `GenerateRequest | ImageToImageRequest` width and height.
- Produces: `_provider_image_parameters(request) -> dict[str, Any]` and Flow2API `generationConfig.imageConfig` only for Gemini.

- [ ] **Step 1: Write failing provider tests**

Add tests that instantiate `GeminiChatImageProvider`, submit `1024x1536`, and assert:

```python
assert payload["generationConfig"]["imageConfig"] == {
    "aspectRatio": "three-four",
    "imageSize": "2k",
}
assert "Target size:" not in payload["messages"][-1]["content"][0]["text"]
assert results[0].generation_params["provider_image_parameters"] == {
    "generationConfig": {
        "responseModalities": ["IMAGE"],
        "imageConfig": {"aspectRatio": "three-four", "imageSize": "2k"},
    }
}
```

Add a compatibility test asserting `OpenAIChatImageProvider` still contains `Target size: 768x1024.` and has no `generationConfig`.

- [ ] **Step 2: Run RED tests**

Run:

```powershell
python -m pytest tests/test_openai_compatible_provider.py -q
```

Expected: Gemini assertions fail because the alias has no provider-specific behavior.

- [ ] **Step 3: Implement the minimal hook**

Add a nearest-supported-aspect helper, a default no-op `_provider_image_parameters`, and Gemini override:

```python
def _gemini_aspect_ratio(width: int | None, height: int | None) -> str | None:
    if not width or not height or width <= 0 or height <= 0:
        return None
    requested_ratio = width / height
    supported = (
        ("landscape", 16 / 9),
        ("portrait", 9 / 16),
        ("square", 1.0),
        ("four-three", 4 / 3),
        ("three-four", 3 / 4),
    )
    return min(supported, key=lambda item: abs(requested_ratio - item[1]))[0]

class GeminiChatImageProvider(OpenAIChatImageProvider):
    def _provider_image_parameters(self, request):
        aspect_ratio = _gemini_aspect_ratio(request.width, request.height)
        if aspect_ratio is None:
            return {}
        return {
            "generationConfig": {
                "responseModalities": ["IMAGE"],
                "imageConfig": {"aspectRatio": aspect_ratio, "imageSize": "2k"},
            }
        }
```

Merge the returned dictionary after ordinary chat passthrough. Gemini overrides `_build_user_content` and `_build_image_to_image_content` only to omit the inherited `Target size` sentence; OpenAI/Grok retain existing Prompt behavior.

- [ ] **Step 4: Run GREEN tests**

Run the same pytest command. Expected: all provider tests pass.

### Task 2: Record Provider Parameters In P3 Evidence

**Files:**
- Modify: `tools/美术工具/run_art_generation.py`
- Modify: `tools/美术工具/tests/test_run_art_generation_requests.py`

**Interfaces:**
- Consumes: first successful `ImageResult.generation_params.provider_image_parameters`.
- Produces: `ProviderRequest.ProviderImageParameters` in formal generation evidence.

- [ ] **Step 1: Write a failing evidence test**

Make the fake image result contain:

```python
generation_params={
    "provider_image_parameters": {
        "generationConfig": {
            "responseModalities": ["IMAGE"],
            "imageConfig": {"aspectRatio": "three-four", "imageSize": "2k"},
        }
    }
}
```

Assert the final generation record contains the same value at `ProviderRequest.ProviderImageParameters`.

- [ ] **Step 2: Run RED test**

Run:

```powershell
python -m unittest tools.美术工具.tests.test_run_art_generation_requests -v
```

Expected: the new evidence assertion fails.

- [ ] **Step 3: Implement evidence propagation**

After each successful batch, copy the first non-empty provider parameter snapshot into the request record without mutating `request.extra`:

```python
provider_params = image_result.generation_params.get("provider_image_parameters")
if provider_params and "ProviderImageParameters" not in provider_request_snapshot:
    provider_request_snapshot["ProviderImageParameters"] = copy.deepcopy(provider_params)
```

- [ ] **Step 4: Run GREEN test**

Run the same unittest command. Expected: all tests pass.

### Task 3: Document And Smoke The Relay Contract

**Files:**
- Modify: `tools/ai-image-gateway/config.example.yaml`
- Modify: `tools/ai-image-gateway/docs/openai_compatible_relay_integration.md`
- Modify: `tools/ai-image-gateway/README.md`

**Interfaces:**
- Consumes: tested payload shape.
- Produces: documented Gemini-only `generationConfig.imageConfig` behavior and validation boundary.

- [ ] **Step 1: Update examples and provider facts**

Document that width/height select the nearest Flow2API aspect alias and `2k/4k` size tier for Gemini chat image requests, exact pixels remain a post-processing responsibility, and Prompt text is not used for dimensions.

- [ ] **Step 2: Run targeted and full gateway tests**

```powershell
python -m pytest tests/test_openai_compatible_provider.py -q
python -m pytest tests -q
python -m compileall ai_image_gateway
```

- [ ] **Step 3: Run one bounded real Gemini smoke**

Use the ignored local config and one image-to-image request with `1024x1536`. Record whether `generationConfig.imageConfig={aspectRatio:three-four,imageSize:2k}` is accepted, actual output dimensions, response mode, and errors. Do not print credentials or config contents.

### Task 4: Publish `doll_zero_hurt` PromptRevision 003

**Files:**
- Generated through tool: `美术文档/_generated/art_generation_requests.json`
- Generated through tool: `美术文档/_generated/art_manifest.json`
- Runtime authoring evidence: `UnityClient/Logs/P3ArtProduction/zero_hurt_repair_20260808_07_gemini_contract/`

**Interfaces:**
- Consumes: current Requirement `doll_zero_hurt@609744e4ff67` and prompt-002.
- Produces: immutable active `doll_zero_hurt@609744e4ff67/prompt-003`.

- [ ] **Step 1: Export the current authoring package**

Run `Export-ArtPromptAuthoringPackage.ps1` for `doll_zero_hurt` into the new ProductionRun evidence directory.

- [ ] **Step 2: Author prompt-003**

Keep the existing identity, costume, posture, framing and injury intent. Remove every Alpha/transparency request and pixel dimension. Require a single uniform saturated green matte with no floor, shadow, scenery, gradient, texture or checkerboard. Update both natural-language and Danbooru Variants and map technical width/height/alpha to provider parameters plus post-processing.

- [ ] **Step 3: Publish and strictly validate**

```powershell
.\tools\美术工具\Publish-ArtPromptRevision.ps1 -RevisionPath <prompt-003.json>
python tools/美术工具/validate_art_generation_requests.py --manifest 美术文档/_generated/art_manifest.json --request-catalog 美术文档/_generated/art_generation_requests.json --strict
```

Expected: prompt-003 is active, prompt-001/002 remain byte-preserved, and strict validation passes.

### Task 5: Rerun Zero Hurt And Process Safely

**Files:**
- Runtime evidence: `UnityClient/Logs/P3ArtProduction/zero_hurt_repair_20260808_07_gemini_contract/`
- Ignored workspace: `UnityClient/Assets/Art/_IncomingAI/character_portraits/doll_zero_hurt/`

**Interfaces:**
- Consumes: prompt-003, neutral Approved reference, Gemini provider, existing selected baseline.
- Produces: raw candidate plus either no new processed round or the next guarded round and selection decision.

- [ ] **Step 1: Record baseline hashes and current latest round**

Record selected, Approved, `.meta`, Registry, current processed round and replacement baseline before generation.

- [ ] **Step 2: Generate two variants serially**

Run the existing portrait route with Gemini, `Variants=2`, `count=1` per request, and exact prompt-003. Stop provider switching; this task intentionally tests Gemini.

- [ ] **Step 3: Audit raw outputs**

Check decodability, actual dimensions, RGB/RGBA mode, background uniformity, identity, anatomy, pose, costume and framing. Reject baked checkerboard, complex scenery, duplicate figures or unrecoverable anatomy before segmentation.

- [ ] **Step 4: Reuse existing segmentation/rembg**

For the best eligible raw, generate and retain the explicit mask, normalize to `1024x1536 RGBA`, and optionally apply bounded green despill. Write only to Run staging.

- [ ] **Step 5: Publish and review only when eligible**

Write SHA-bound visual review, run the Registrar to publish the next numeric round, then perform six-dimension replacement review. Use guarded selection only if the new candidate is strictly better and all protected dimensions pass.

### Task 6: Status Writeback And Final Verification

**Files:**
- Modify: `agent_status/art.md`
- Modify: `美术文档/05_AI图片网关接入方案.md` only if the stable P3 gateway contract changes.
- Generated: `docs_index.json`

**Interfaces:**
- Consumes: tests, smoke, PromptRevision and portrait evidence.
- Produces: evidence-bounded art status and verified repository state.

- [ ] **Step 1: Update durable facts**

Record the actual accepted Gemini parameter, prompt-003 policy, generation result, processing result and exact maximum claim. Do not rewrite PROJECT_STATUS unless the project-level blocker changes.

- [ ] **Step 2: Run verification**

```powershell
python -m pytest tools/ai-image-gateway/tests -q
python -m unittest tools.美术工具.tests.test_run_art_generation_requests -v
python tools/美术工具/validate_art_generation_requests.py --manifest 美术文档/_generated/art_manifest.json --request-catalog 美术文档/_generated/art_generation_requests.json --strict
.\tools\docs\Generate-DocsIndex.ps1
.\tools\docs\Validate-Docs.ps1
.\tools\agent\Invoke-AgentHealthCheck.ps1 -Strict
git diff --check
git status --short
```

- [ ] **Step 3: Verify protected state**

Confirm selected, Approved, `.meta`, GUID and Registry hashes are unchanged unless guarded selection explicitly succeeded. Record any provider timeout as `validation_limited:provider_unavailable:gemini_chat_image` without overstating completion.
