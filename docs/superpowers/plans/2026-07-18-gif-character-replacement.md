---
id: plan_gif_character_replacement_implementation
title: GIF 小循环人物替换实现计划
type: plan
role: 美术
domain: gif_character_replacement
status: planned
source_of_truth: false
related:
  - 美术文档/20_GIF小循环人物替换工作流.md
last_verified: 2026-07-18
update_rule: 修改 GIF 人物替换实现文件结构、接口、任务顺序、测试命令、提交边界或验证口径时同步本文件。
---

# GIF Character Replacement Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a resumable Project P3 art tool that accepts an 8-30 frame GIF, required character/style text, and zero or more references, replaces the character independently in every frame through the existing image gateway, reviews risks, and re-encodes the original timeline.

**Architecture:** Keep all P3 orchestration in `tools/美术工具/gif_character_replace/` and treat `tools/ai-image-gateway` as an unchanged provider facade. A persistent run store owns immutable configuration, timeline metadata, frame state, and evidence; small modules handle extraction, identity inputs, gateway calls, preview gates, quality review, and encoding. The PowerShell entry point only translates parameters to the Python CLI.

**Tech Stack:** Python 3.10, standard-library `dataclasses` / `enum` / `json` / `hashlib` / `subprocess`, Pillow 12+, NumPy 2+, existing `ai_image_gateway.ImageService`, PowerShell, `unittest`.

## Global Constraints

- Accept only GIF inputs with `8-30` frames; invalid or damaged inputs stop after preflight without AI calls.
- Conflict priority is `user text > user references > original GIF character appearance`.
- Preserve original action, subject position, background, camera, composition, canvas size, frame durations, order, and loop count.
- User style text applies to the replacement character only; it must not implicitly redraw the background.
- Generate every animation frame independently. Never use a previous generated frame as an input reference.
- Default frame editor is `gemini_chat_image.image_to_image`; zero-reference identity anchors use three serial `openai_images` requests.
- Every provider request uses `count=1`, concurrency `1`, and a configurable delay.
- Do not require a mask in the normal path. Strict repair may use a local composite or optional mask only after drift is detected.
- Do not modify `tools/ai-image-gateway`, Manifest, Approved, `.meta`, Registry, or Unity.
- Raw provider output and run evidence stay in the user-selected run directory and are not formal P3 assets.
- FFmpeg is optional and currently absent on the workspace machine; Pillow fallback must preserve frame count, durations, loop count, and a shared palette.
- Tests must not call real image providers. Real provider smoke is a separate final validation step and must write outside tracked paths.

## File Map

Create:

- `tools/美术工具/gif_character_replace/__init__.py`: stable public exports.
- `tools/美术工具/gif_character_replace/models.py`: enums and serialized data contracts.
- `tools/美术工具/gif_character_replace/store.py`: immutable run config and atomic state/evidence persistence.
- `tools/美术工具/gif_character_replace/timeline.py`: GIF preflight, disposal-aware full-frame extraction, timeline metadata.
- `tools/美术工具/gif_character_replace/identity.py`: text contract, reference selection, reference board.
- `tools/美术工具/gif_character_replace/backend.py`: gateway adapter, single-call retries, anchor and frame generation.
- `tools/美术工具/gif_character_replace/preview.py`: deterministic preview-frame selection and approval gate.
- `tools/美术工具/gif_character_replace/review.py`: hard checks, drift/flicker/seam heuristics, heatmaps and contact sheet.
- `tools/美术工具/gif_character_replace/encoder.py`: FFmpeg primary encoder and Pillow shared-palette fallback.
- `tools/美术工具/gif_character_replace/workflow.py`: state machine, resume, rerun, encode-only orchestration.
- `tools/美术工具/gif_character_replace_cli.py`: Python CLI kept separate from the same-named package.
- `tools/美术工具/Invoke-GifCharacterReplace.ps1`: PowerShell parameter wrapper.
- `tools/美术工具/tests/test_gif_models_store.py`
- `tools/美术工具/tests/test_gif_timeline.py`
- `tools/美术工具/tests/test_gif_identity.py`
- `tools/美术工具/tests/test_gif_backend.py`
- `tools/美术工具/tests/test_gif_preview.py`
- `tools/美术工具/tests/test_gif_workflow.py`
- `tools/美术工具/tests/test_gif_review.py`
- `tools/美术工具/tests/test_gif_encoder.py`
- `tools/美术工具/tests/test_gif_cli.py`

Modify after implementation evidence exists:

- `tools/美术工具/README.md`: actual commands, states, outputs, and limitations.
- `美术文档/20_GIF小循环人物替换工作流.md`: implementation status and verified evidence boundary.
- `agent_status/art.md`: recent completion, current focus, next step, limitations, evidence paths.
- `DOCS_INDEX.md` and `docs_index.json`: regenerate through the docs tooling.

---

### Task 1: Run Models And Atomic Persistence

**Files:**
- Create: `tools/美术工具/gif_character_replace/__init__.py`
- Create: `tools/美术工具/gif_character_replace/models.py`
- Create: `tools/美术工具/gif_character_replace/store.py`
- Test: `tools/美术工具/tests/test_gif_models_store.py`

**Interfaces:**
- Produces: `RunStatus`, `FrameStatus`, `RunConfig`, `TimelineMetadata`, `FrameRecord`, `RunState`, `RunPaths`, `RunStore`.
- `RunStore.create(config: RunConfig) -> RunStore` writes immutable `run_config.json` and initial `run_state.json`.
- `RunStore.load(run_root: Path) -> RunStore` restores an existing run.
- `RunStore.update_state(mutator: Callable[[RunState], RunState]) -> RunState` atomically replaces state.

- [ ] **Step 1: Write failing model and store tests**

```python
class RunStoreTests(unittest.TestCase):
    def test_create_writes_immutable_config_and_initial_state(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            config = RunConfig.new(
                input_gif=Path(temp) / "source.gif",
                output_root=Path(temp),
                prompt="silver-haired mechanic",
                references=(),
            )
            store = RunStore.create(config)
            self.assertEqual(store.load_state().status, RunStatus.PREFLIGHT)
            self.assertEqual(store.load_config(), config)

    def test_create_rejects_changed_existing_config(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            first = RunConfig.new(Path(temp) / "a.gif", Path(temp), "first", ())
            store = RunStore.create(first)
            changed = dataclasses.replace(first, prompt="changed")
            with self.assertRaisesRegex(ValueError, "immutable"):
                RunStore.create(changed, run_root=store.paths.root)
```

- [ ] **Step 2: Run the focused test and confirm failure**

Run:

```powershell
python -m unittest discover -s tools/美术工具/tests -p "test_gif_models_store.py" -v
```

Expected: FAIL because `gif_character_replace.models` and `RunStore` do not exist.

- [ ] **Step 3: Implement enums, dataclasses, JSON conversion, and atomic writes**

Use these exact enum values:

```python
class RunStatus(str, Enum):
    PREFLIGHT = "preflight"
    AWAITING_IDENTITY_SELECTION = "awaiting_identity_selection"
    AWAITING_PREVIEW_APPROVAL = "awaiting_preview_approval"
    RUNNING = "running"
    READY = "ready"
    REVIEW_REQUIRED = "review_required"
    FAILED = "failed"

class FrameStatus(str, Enum):
    PENDING = "pending"
    GENERATED = "generated"
    ACCEPTED = "accepted"
    REJECTED = "rejected"
    MANUAL_REVIEW = "manual_review"
```

`RunConfig` must include `run_id`, input/output paths, prompt, references, providers, `min_frames=8`, `max_frames=30`, `delay_seconds=2.0`, `retry_count=3`, `visual_retry_count=2`, `preserve_transparency=True`, `encoder="auto"`, and quality thresholds used in Task 7. Serialize paths as absolute strings and tuples as JSON arrays.

Use these exact public data shapes so later tasks do not invent incompatible fields:

```python
@dataclass(frozen=True)
class RunConfig:
    run_id: str
    input_gif: str
    output_root: str
    prompt: str
    references: tuple[str, ...]
    config_path: str
    frame_provider: str = "gemini_chat_image"
    anchor_provider: str = "openai_images"
    min_frames: int = 8
    max_frames: int = 30
    delay_seconds: float = 2.0
    retry_count: int = 3
    visual_retry_count: int = 2
    preserve_transparency: bool = True
    encoder: str = "auto"
    min_image_stddev: float = 2.0
    pixel_diff_threshold: float = 24.0
    stable_pixel_stddev: float = 4.0
    edge_fraction: float = 0.12
    background_changed_ratio: float = 0.08
    flicker_ratio: float = 2.5
    flicker_absolute_delta: float = 12.0
    loop_seam_ratio: float = 2.0
    identity_histogram_distance: float = 0.55
    identity_area_change_ratio: float = 0.35

    @classmethod
    def new(
        cls,
        *,
        input_gif: Path,
        output_root: Path,
        prompt: str,
        references: Sequence[Path],
        config_path: Path | None = None,
    ) -> "RunConfig":
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        run_id = f"gif_replace_{timestamp}_{uuid.uuid4().hex[:8]}"
        return cls(
            run_id=run_id,
            input_gif=str(input_gif.resolve()),
            output_root=str(output_root.resolve()),
            prompt=prompt.strip(),
            references=tuple(str(path.resolve()) for path in references),
            config_path=str(config_path.resolve()) if config_path else "",
        )

@dataclass(frozen=True)
class TimelineMetadata:
    width: int
    height: int
    frame_count: int
    durations_ms: tuple[int, ...]
    loop: int
    has_transparency: bool
    disposal_methods: tuple[int, ...]
    source_sha256: str
    frame_paths: tuple[str, ...]

@dataclass(frozen=True)
class FrameRecord:
    index: int
    source_path: str
    output_path: str | None
    status: FrameStatus
    attempts: int
    request_records: tuple[dict[str, Any], ...]
    risks: tuple[str, ...]
    errors: tuple[str, ...]

@dataclass(frozen=True)
class RunState:
    status: RunStatus
    timeline: TimelineMetadata | None
    selected_identity: str | None
    preview_indices: tuple[int, int] | None
    preview_approved: bool
    frames: tuple[FrameRecord, ...]
    review_report: str | None
    contact_sheet: str | None
    result_gif: str | None
    warnings: tuple[str, ...]
    errors: tuple[str, ...]

@dataclass(frozen=True)
class RunPaths:
    root: Path
    input_dir: Path
    timeline_dir: Path
    original_frames_dir: Path
    identity_dir: Path
    preview_dir: Path
    generated_raw_dir: Path
    accepted_dir: Path
    rejected_dir: Path
    output_dir: Path
    reports_dir: Path
```

Import `datetime` and `uuid` for the generated `gif_replace_YYYYMMDD_HHMMSS_<8 hex chars>` ID. Reject an empty stripped prompt before returning the config.

`RunPaths.for_config(config)` must place the run at `Path(config.output_root) / "gif_character_replace" / config.run_id` and expose the exact directories from the design. `RunStore.create` copies `source.gif`, `character_prompt.txt`, and reference files into `input/` before writing state; config retains the original absolute paths and evidence records the copied paths.

Write JSON atomically with a sibling temporary file and `Path.replace()`:

```python
def atomic_write_json(path: Path, payload: dict[str, Any]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + ".tmp")
    temporary.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    temporary.replace(path)
```

- [ ] **Step 4: Run focused tests**

Run the command from Step 2. Expected: all `RunStoreTests` pass.

- [ ] **Step 5: Commit Task 1**

```powershell
git add tools/美术工具/gif_character_replace tools/美术工具/tests/test_gif_models_store.py
git commit -m "feat: add gif replacement run state"
```

### Task 2: Disposal-Aware GIF Preflight And Extraction

**Files:**
- Create: `tools/美术工具/gif_character_replace/timeline.py`
- Test: `tools/美术工具/tests/test_gif_timeline.py`

**Interfaces:**
- Consumes: `RunConfig`, `TimelineMetadata`, `RunStore`.
- Produces: `extract_timeline(input_gif: Path, frames_dir: Path, min_frames: int, max_frames: int) -> TimelineMetadata`.
- Writes: `timeline/source_metadata.json` and `timeline/frames_original/frame_0000.png` through the caller.

- [ ] **Step 1: Write failing extraction tests with synthetic GIFs**

Create test helpers that save RGBA frames with exact durations `[80, 90, 100, 110, 120, 130, 140, 150]`, `loop=0`, `disposal=2`. Assert:

```python
metadata = extract_timeline(source, output, min_frames=8, max_frames=30)
self.assertEqual(metadata.frame_count, 8)
self.assertEqual(metadata.durations_ms, (80, 90, 100, 110, 120, 130, 140, 150))
self.assertEqual(metadata.loop, 0)
self.assertEqual(Image.open(output / "frame_0007.png").mode, "RGBA")
self.assertEqual(Image.open(output / "frame_0007.png").size, (64, 64))
```

Add separate tests that 7-frame and 31-frame GIFs raise `PreflightError` before writing frame PNGs, and a transparent GIF sets `has_transparency=True`.

- [ ] **Step 2: Run and confirm failure**

```powershell
python -m unittest discover -s tools/美术工具/tests -p "test_gif_timeline.py" -v
```

Expected: FAIL because `timeline.py` is absent.

- [ ] **Step 3: Implement full-frame extraction**

For every frame, call `gif.seek(index)`, then `gif.convert("RGBA").copy()` before saving. Record `duration`, `getattr(gif, "disposal_method", 0)`, source SHA-256, loop, dimensions, transparency, and the relative PNG path. Reject non-GIF input by checking `gif.format == "GIF"`.

Do not use `ImageSequence.Iterator` as the sole source of frame pixels; the explicit `seek()` + copied RGBA canvas is required so disposal is applied before the provider sees a frame.

- [ ] **Step 4: Run timeline tests**

Expected: all extraction, bounds, duration, loop, transparency, and hash tests pass.

- [ ] **Step 5: Commit Task 2**

```powershell
git add tools/美术工具/gif_character_replace/timeline.py tools/美术工具/tests/test_gif_timeline.py
git commit -m "feat: extract gif replacement timeline"
```

### Task 3: Identity Contract And Reference Board

**Files:**
- Create: `tools/美术工具/gif_character_replace/identity.py`
- Test: `tools/美术工具/tests/test_gif_identity.py`

**Interfaces:**
- Produces: `IdentityContract`, `IdentityInputs`.
- `build_identity_contract(user_prompt: str, reference_paths: Sequence[Path]) -> IdentityContract`.
- `prepare_identity_inputs(contract: IdentityContract, identity_dir: Path) -> IdentityInputs`.
- `IdentityInputs.provider_images` contains zero, one to three original references, or one generated reference board for four or more references.

Use these exact shapes:

```python
@dataclass(frozen=True)
class IdentityContract:
    user_prompt: str
    provider_prompt: str
    must_have: tuple[str, ...]
    may_change: tuple[str, ...]
    must_preserve: tuple[str, ...]
    must_not_have: tuple[str, ...]
    reference_paths: tuple[str, ...]
    reference_sha256: tuple[str, ...]

@dataclass(frozen=True)
class IdentityInputs:
    contract_path: str
    provider_image_paths: tuple[str, ...]
    reference_board_path: str | None
```

- [ ] **Step 1: Write failing contract-priority tests**

```python
contract = build_identity_contract(
    "red coat, watercolor character rendering",
    [Path("blue_coat_reference.png")],
)
self.assertIn("User text has highest priority", contract.provider_prompt)
self.assertIn("red coat", contract.provider_prompt)
self.assertIn("preserve the original background exactly", contract.provider_prompt)
self.assertIn("apply watercolor rendering only to the replacement character", contract.provider_prompt)
```

Add tests for blank prompt rejection, missing reference rejection, direct use of 1-3 references, and creation of one labeled PNG board for four references.

- [ ] **Step 2: Run and confirm failure**

```powershell
python -m unittest discover -s tools/美术工具/tests -p "test_gif_identity.py" -v
```

- [ ] **Step 3: Implement the fixed prompt contract**

The provider prompt must contain these sections in this order:

```text
TASK
USER TEXT - HIGHEST PRIORITY
REFERENCE IMAGE ROLE
MUST PRESERVE FROM CURRENT GIF FRAME
MAY CHANGE
MUST NOT CHANGE
OUTPUT CONTRACT
```

The reference board uses a neutral gray background, `2` columns, contained thumbnails, and filenames as labels. Save `identity_contract.json` with the original prompt, structured sections, reference absolute paths, SHA-256 hashes, and chosen provider input paths.

- [ ] **Step 4: Run identity tests**

Expected: all priority, missing-file, direct-reference, and board tests pass.

- [ ] **Step 5: Commit Task 3**

```powershell
git add tools/美术工具/gif_character_replace/identity.py tools/美术工具/tests/test_gif_identity.py
git commit -m "feat: add gif replacement identity contract"
```

### Task 4: Gateway Adapter, Single-Image Calls, And Retry Policy

**Files:**
- Create: `tools/美术工具/gif_character_replace/backend.py`
- Test: `tools/美术工具/tests/test_gif_backend.py`

**Interfaces:**
- Produces: `GeneratedImage`, `ImageBackend` protocol, `GatewayImageBackend`.
- `generate_anchor(prompt: str, width: int, height: int) -> GeneratedImage` uses `GenerateRequest(provider="openai_images", count=1)`.
- `replace_frame(identity_images: Sequence[bytes], frame: bytes, prompt: str, width: int, height: int) -> GeneratedImage` uses `ImageToImageRequest(provider="gemini_chat_image", count=1)`.
- `call_with_retry(operation, retry_count, delay_seconds) -> GeneratedImage` retries only transient failures.

```python
@dataclass(frozen=True)
class GeneratedImage:
    image_bytes: bytes
    provider: str
    model: str
    seed: int | None
    generation_params: dict[str, Any]
    cost: float

class ImageBackend(Protocol):
    async def generate_anchor(self, prompt: str, width: int, height: int) -> GeneratedImage:
        raise NotImplementedError

    async def replace_frame(
        self,
        identity_images: Sequence[bytes],
        frame: bytes,
        prompt: str,
        width: int,
        height: int,
    ) -> GeneratedImage:
        raise NotImplementedError
```

- [ ] **Step 1: Write failing adapter tests with a fake service**

The fake service records requests and returns `BatchResult`. Assert three anchor calls produce three separate `GenerateRequest` objects with `count=1`; frame replacement sends identity images first and the current source frame last; no previous output is present.

Add retry tests:

```python
service.responses = [
    BatchResult(errors=["HTTP 524"]),
    BatchResult(results=[fake_image_result()]),
]
result = await backend.generate_anchor("prompt", 512, 768)
self.assertEqual(service.generate_call_count, 2)
self.assertEqual(result.provider, "openai_images")
```

Assert `401` and `403` errors stop after one call. Assert exhausted transient errors raise `BackendCallError` with all error messages.

- [ ] **Step 2: Run and confirm failure**

```powershell
python -m unittest discover -s tools/美术工具/tests -p "test_gif_backend.py" -v
```

- [ ] **Step 3: Implement the adapter without changing the submodule**

Insert `tools/ai-image-gateway` into `sys.path` exactly as existing art tools do. `GatewayImageBackend` owns an `ImageService` supplied by an async context manager or injected fake. Treat error strings containing `429`, `502`, `503`, `504`, or `524` as transient; `401` and `403` are terminal.

Persist provider/model/seed/generation params/cost/byte length from `ImageResult` in `GeneratedImage`.

- [ ] **Step 4: Run backend tests**

Expected: all call-shape and retry tests pass without network access.

- [ ] **Step 5: Commit Task 4**

```powershell
git add tools/美术工具/gif_character_replace/backend.py tools/美术工具/tests/test_gif_backend.py
git commit -m "feat: add gif replacement image backend"
```

### Task 5: Identity Selection And Two-Frame Preview Gate

**Files:**
- Create: `tools/美术工具/gif_character_replace/preview.py`
- Test: `tools/美术工具/tests/test_gif_preview.py`

**Interfaces:**
- Produces: `PreviewSelection(identity_index: int, action_index: int)`.
- `select_preview_frames(frame_paths: Sequence[Path]) -> PreviewSelection`.
- `generate_identity_candidates(store, backend, contract) -> RunState`.
- `generate_preview(store, backend, identity_inputs) -> RunState`.
- `approve_identity(store, candidate_index: int) -> RunState` and `approve_preview(store) -> RunState`.

- [ ] **Step 1: Write failing deterministic selection and gate tests**

Use eight synthetic frames where frame `2` is sharp and nearly static and frame `6` has the largest motion delta. Assert selection returns `(2, 6)`.

For zero references, assert `generate_identity_candidates` makes exactly three backend calls, saves three candidates, and sets `AWAITING_IDENTITY_SELECTION`. For existing references, assert it skips anchor generation. Assert preview approval fails unless both preview output files exist and decode at the expected size.

- [ ] **Step 2: Run and confirm failure**

```powershell
python -m unittest discover -s tools/美术工具/tests -p "test_gif_preview.py" -v
```

- [ ] **Step 3: Implement deterministic preview scoring**

Compute identity score as:

```python
edge_variance = np.asarray(frame.convert("L").filter(ImageFilter.FIND_EDGES), dtype=np.float32).var()
neighbor_motion = mean(delta(previous, frame), delta(frame, next_frame))
identity_score = edge_variance - 0.5 * neighbor_motion
```

Choose the highest score, breaking ties by lowest index. Choose the action frame by maximum RGB mean absolute delta from the identity frame, excluding the identity index.

Write preview raw image and metadata under `preview/identity_frame/` and `preview/action_frame/`. On success set `AWAITING_PREVIEW_APPROVAL`; do not process other frames.

- [ ] **Step 4: Run preview tests**

Expected: selection, zero-reference gate, existing-reference path, and approval validation pass.

- [ ] **Step 5: Commit Task 5**

```powershell
git add tools/美术工具/gif_character_replace/preview.py tools/美术工具/tests/test_gif_preview.py
git commit -m "feat: add gif replacement preview gate"
```

### Task 6: Independent Batch Generation, Resume, And Targeted Reruns

**Files:**
- Create: `tools/美术工具/gif_character_replace/workflow.py`
- Test: `tools/美术工具/tests/test_gif_workflow.py`

**Interfaces:**
- Produces: `GifReplacementWorkflow`.
- `prepare(config: RunConfig) -> RunState` performs preflight, identity preparation, and the appropriate gate.
- `resume_after_preview(run_root: Path) -> RunState` processes pending frames independently.
- `rerun_frames(run_root: Path, indices: Sequence[int], strict: bool) -> RunState` replaces only requested records.

- [ ] **Step 1: Write failing state-machine tests**

Use a fake backend that returns deterministic PNG bytes and records source-frame hashes. Assert:

- `prepare` stops at identity selection when references are absent.
- `prepare` stops at preview approval when references exist.
- `resume_after_preview` rejects unapproved runs.
- Approved resume calls each non-preview source frame once and never supplies a generated frame as input.
- A pre-existing accepted frame is skipped after simulated interruption.
- `rerun_frames([4, 9])` calls only frames 4 and 9 and increments their attempt count.
- One failed frame becomes `MANUAL_REVIEW` while later frames continue.

- [ ] **Step 2: Run and confirm failure**

```powershell
python -m unittest discover -s tools/美术工具/tests -p "test_gif_workflow.py" -v
```

- [ ] **Step 3: Implement the workflow state machine**

The workflow must save a `FrameRecord` immediately after every provider call. Preview outputs may be promoted to their matching frame records after approval so those source frames are not called twice. After all possible frames are generated, leave status as `RUNNING`; Task 7 adds review and Task 8 adds final encoding/status transitions.

Strict rerun appends this sentence to the fixed prompt without changing immutable config:

```text
STRICT REPAIR: preserve every non-character pixel, background edge, camera crop, and object placement from the current source frame.
```

Store the effective repair prompt in the attempt record, not `run_config.json`.

- [ ] **Step 4: Run workflow tests**

Expected: all gates, independent inputs, resume behavior, reruns, and partial-failure behavior pass.

- [ ] **Step 5: Commit Task 6**

```powershell
git add tools/美术工具/gif_character_replace/workflow.py tools/美术工具/tests/test_gif_workflow.py
git commit -m "feat: orchestrate gif frame replacement"
```

### Task 7: Technical Validation And Visual Risk Review

**Files:**
- Create: `tools/美术工具/gif_character_replace/review.py`
- Modify: `tools/美术工具/gif_character_replace/workflow.py`
- Test: `tools/美术工具/tests/test_gif_review.py`

**Interfaces:**
- Produces: `FrameRisk`, `SequenceReview`.
- `review_sequence(config: RunConfig, timeline: TimelineMetadata, source_paths: Sequence[Path], output_paths: Sequence[Path], reports_dir: Path, output_dir: Path) -> SequenceReview`.
- Writes: `reports/frame_review.json`, per-frame heatmaps, and `output/preview_contact_sheet.png`.
- Consumes quality thresholds from immutable `RunConfig`.

```python
@dataclass(frozen=True)
class FrameRisk:
    frame_index: int
    codes: tuple[str, ...]
    hard_failure: bool
    metrics: dict[str, float]
    heatmap_path: str | None

@dataclass(frozen=True)
class SequenceReview:
    frame_risks: tuple[FrameRisk, ...]
    sequence_codes: tuple[str, ...]
    hard_failure: bool
    contact_sheet_path: str
```

- [ ] **Step 1: Write failing hard-check and heuristic tests**

Create synthetic source/output sequences to assert:

- wrong size, corrupt bytes, solid-color output, and missing output are hard failures;
- changing only the center subject does not flag `background_drift`;
- changing stable edge pixels above threshold flags `background_drift`;
- one output jump at least `2.5x` its source motion and above absolute delta `12` flags `temporal_flicker`;
- first/last output jump at least `2.0x` source loop seam flags `loop_seam`;
- unreliable changed-region extraction emits `identity_check_limited`;
- reliable changed-region histogram distance over `0.55` or area change over `0.35` emits `identity_drift`.
- every source timeline with transparency emits `transparent_silhouette_limited`, because the fallback reapplies original alpha and cannot prove that a new character silhouette fits inside it.

- [ ] **Step 2: Run and confirm failure**

```powershell
python -m unittest discover -s tools/美术工具/tests -p "test_gif_review.py" -v
```

- [ ] **Step 3: Implement exact review algorithms**

Use these defaults in `RunConfig`:

```python
pixel_diff_threshold = 24.0
stable_pixel_stddev = 4.0
edge_fraction = 0.12
background_changed_ratio = 0.08
flicker_ratio = 2.5
flicker_absolute_delta = 12.0
loop_seam_ratio = 2.0
identity_histogram_distance = 0.55
identity_area_change_ratio = 0.35
```

Build the stable background mask from original-frame per-pixel standard deviation and intersect it with the outer edge band. Build the changed-region mask from source/output mean RGB delta. Treat changed ratios outside `0.02-0.65` or boxes touching more than two canvas edges as unreliable.

When `TimelineMetadata.has_transparency` is true, add `transparent_silhouette_limited` to the sequence codes before encoding. This is a declared capability limit rather than a heuristic pass/fail judgment.

The contact sheet must show original, generated, and heatmap columns with frame index and risk labels. Update `GifReplacementWorkflow.resume_after_preview` and `rerun_frames` to invoke review after generation. Hard failures set `FAILED`; otherwise persist the report/contact-sheet paths and keep status `RUNNING` until Task 8 encodes the GIF.

- [ ] **Step 4: Run review tests**

Expected: all technical and risk-classification tests pass.

- [ ] **Step 5: Commit Task 7**

```powershell
git add tools/美术工具/gif_character_replace/review.py tools/美术工具/gif_character_replace/workflow.py tools/美术工具/tests/test_gif_review.py
git commit -m "feat: review gif replacement risks"
```

### Task 8: Variable-Duration GIF Encoding With FFmpeg Fallback

**Files:**
- Create: `tools/美术工具/gif_character_replace/encoder.py`
- Modify: `tools/美术工具/gif_character_replace/workflow.py`
- Test: `tools/美术工具/tests/test_gif_encoder.py`

**Interfaces:**
- Produces: `EncodeResult(path: Path, encoder: str, warnings: tuple[str, ...])`.
- `encode_gif(frame_paths, original_alpha_paths, durations_ms, loop, output_path, encoder, preserve_transparency) -> EncodeResult`.
- `encoder="auto"` selects FFmpeg when `shutil.which("ffmpeg")` succeeds, otherwise Pillow.

- [ ] **Step 1: Write failing Pillow fallback and FFmpeg command tests**

Since FFmpeg is absent in the current environment, the integration test must assert `auto` selects Pillow and the decoded result preserves `8` frames, exact durations, loop `0`, and canvas size.

Mock `shutil.which` and `subprocess.run` to assert the FFmpeg branch creates a concat timeline with per-frame `duration` lines, runs `palettegen`, then runs `paletteuse`.

Add a transparent sample where source alpha is reapplied and the output retains transparent pixels.

- [ ] **Step 2: Run and confirm failure**

```powershell
python -m unittest discover -s tools/美术工具/tests -p "test_gif_encoder.py" -v
```

- [ ] **Step 3: Implement both encoders**

For Pillow fallback, make one shared palette from an atlas of copies resized to at most `256x256`, quantized to `255` colors. Quantize every full-size frame against that palette. Reserve palette index `255` for transparency when alpha is preserved.

For FFmpeg, write a concat file with each PNG and duration in seconds, repeat the final file once for concat duration semantics, generate one palette, then apply it with `paletteuse=dither=sierra2_4a`. Write all temporary files beneath the run directory and remove them after successful output verification.

Update workflow completion rules: after successful encoding, set `READY` when review has no visual risk codes and `REVIEW_REQUIRED` when any visual risk or `identity_check_limited` exists. Add `encode_only(run_root: Path) -> RunState`; it refuses missing/hard-failed frames and otherwise reuses saved timeline/review data without provider calls.

- [ ] **Step 4: Run encoder tests**

Expected: Pillow integration passes on this machine; mocked FFmpeg command tests pass; transparency and metadata tests pass.

- [ ] **Step 5: Commit Task 8**

```powershell
git add tools/美术工具/gif_character_replace/encoder.py tools/美术工具/gif_character_replace/workflow.py tools/美术工具/tests/test_gif_encoder.py
git commit -m "feat: encode gif replacement output"
```

### Task 9: CLI, PowerShell Wrapper, And Offline End-To-End Test

**Files:**
- Create: `tools/美术工具/gif_character_replace_cli.py`
- Create: `tools/美术工具/Invoke-GifCharacterReplace.ps1`
- Test: `tools/美术工具/tests/test_gif_cli.py`
- Modify: `tools/美术工具/gif_character_replace/workflow.py`

**Interfaces:**
- `gif_character_replace_cli.main(argv: Sequence[str] | None = None, backend_factory: BackendFactory | None = None) -> int`.
- `BackendFactory = Callable[[RunConfig], AsyncContextManager[ImageBackend]]`; production creates `GatewayImageBackend`, tests inject a fake async context manager.
- New-run arguments: `--input-gif`, `--prompt` / `--prompt-file`, repeated `--reference`, `--output-root`, `--config`, `--provider`, `--dry-run`.
- Existing-run actions: `--run-id`, `--select-identity`, `--approve-preview`, `--resume`, repeated `--rerun-frame`, `--repair-mode`, `--encode-only`.

- [ ] **Step 1: Write failing parser and offline end-to-end tests**

Assert prompt and prompt-file are mutually exclusive but one is required for a new run. Assert existing-run actions reject `--input-gif` mutations.

The end-to-end test uses an 8-frame synthetic GIF, one reference image, injected fake backend, and Pillow encoder:

```python
first_exit = main(new_run_args, backend_factory=fake_factory)
self.assertEqual(first_exit, 0)
self.assertEqual(load_state(run_root).status, RunStatus.AWAITING_PREVIEW_APPROVAL)
self.assertFalse((run_root / "output" / "result.gif").exists())

second_exit = main(["--run-id", run_id, "--approve-preview", "--resume"], backend_factory=fake_factory)
self.assertEqual(second_exit, 0)
self.assertTrue((run_root / "output" / "result.gif").exists())
```

Also assert `--dry-run` writes the plan and timeline but makes zero backend calls.

- [ ] **Step 2: Run and confirm failure**

```powershell
python -m unittest discover -s tools/美术工具/tests -p "test_gif_cli.py" -v
```

- [ ] **Step 3: Implement CLI and PowerShell parameter sets**

PowerShell must build a Python argument list without string concatenation. Expose `InputGif`, `Prompt`, `PromptFile`, `Reference`, `OutputRoot`, `ConfigPath`, `Provider`, `RunID`, `SelectIdentity`, `ApprovePreview`, `Resume`, `RerunFrame`, `RepairMode`, `EncodeOnly`, `Encoder`, and `DryRun`. Use `ValidateSet("Auto", "FFmpeg", "Pillow")` for `Encoder` and `ValidateSet("Normal", "Strict")` for `RepairMode`. Repeated references and rerun indices must emit repeated CLI flags.

The CLI prints one JSON summary containing `run_id`, `run_root`, `status`, `frame_count`, `accepted_count`, `manual_review_frames`, `result_gif`, and `report`.

- [ ] **Step 4: Run the complete offline GIF test suite**

```powershell
python -m unittest discover -s tools/美术工具/tests -p "test_gif_*.py" -v
```

Expected: all tests pass with zero network calls.

- [ ] **Step 5: Commit Task 9**

```powershell
git add tools/美术工具/gif_character_replace_cli.py tools/美术工具/Invoke-GifCharacterReplace.ps1 tools/美术工具/gif_character_replace/workflow.py tools/美术工具/tests/test_gif_cli.py
git commit -m "feat: add gif replacement command"
```

### Task 10: Provider Smoke, Documentation, And Completion Evidence

**Files:**
- Modify: `tools/美术工具/README.md`
- Modify: `美术文档/20_GIF小循环人物替换工作流.md`
- Modify: `agent_status/art.md`
- Regenerate: `DOCS_INDEX.md`
- Regenerate: `docs_index.json`

**Interfaces:**
- Uses the public PowerShell command only; does not import internal modules for smoke.
- Produces a temporary smoke run and a repository status writeback, but no raw provider images in Git.

- [ ] **Step 1: Run all offline tests and syntax checks**

```powershell
python -m unittest discover -s tools/美术工具/tests -p "test_gif_*.py" -v
python -m py_compile tools/美术工具/gif_character_replace_cli.py tools/美术工具/gif_character_replace/*.py
```

Expected: zero failures and zero syntax errors.

- [ ] **Step 2: Check provider configuration without exposing credentials**

```powershell
.\tools\美术工具\Test-AIImageBackends.ps1 -CheckConfigOnly
```

If Gemini configuration is unavailable, record `validation_limited:gif_gemini_config_unavailable` and do not claim real-backend success.

- [ ] **Step 3: Run a bounded two-frame real preview smoke when configured**

Create an 8-frame temporary GIF and one temporary reference PNG outside the repository. Run:

```powershell
.\tools\美术工具\Invoke-GifCharacterReplace.ps1 `
  -InputGif $sourceGif `
  -Prompt "replace the simple figure with a silver-haired workshop mechanic; preserve the flat background exactly" `
  -Reference $referencePng `
  -OutputRoot $smokeRoot
```

Expected: status `awaiting_preview_approval`, exactly two successful Gemini preview requests, no full-batch output GIF, and complete request metadata. Do not approve the preview during smoke, avoiding another six provider calls.

- [ ] **Step 4: Update documentation and status with the evidence boundary**

Add commands and state descriptions to `tools/美术工具/README.md`. Change the workflow document status to `implemented_preview_smoke_passed` only if Step 3 succeeds; otherwise use `implemented_offline_validated` plus the exact `validation_limited:*` reason. Update `agent_status/art.md` with recent completion, current focus, next step, limitations, test command, and smoke evidence path.

- [ ] **Step 5: Regenerate and validate documentation**

```powershell
.\tools\docs\Generate-DocsIndex.ps1
.\tools\docs\Validate-Docs.ps1
git diff --check
```

Expected: validation passes with zero missing metadata or broken relations.

- [ ] **Step 6: Review scope and commit completion evidence**

```powershell
git status --short
git diff -- tools/美术工具/README.md 美术文档/20_GIF小循环人物替换工作流.md agent_status/art.md DOCS_INDEX.md docs_index.json
git add tools/美术工具/README.md 美术文档/20_GIF小循环人物替换工作流.md agent_status/art.md DOCS_INDEX.md docs_index.json
git commit -m "docs: record gif replacement validation"
```

Before committing, explicitly exclude temporary smoke images, `_IncomingAI`, generated candidates, unrelated art workspace migration changes, and `tools/ai-image-gateway` submodule changes.

## Consistency Optimization Addendum (2026-07-18)

The independent-frame design now uses a fixed generated appearance anchor after preview approval:

- Add `RunStatus.AWAITING_APPEARANCE_APPROVAL`, `RunState.appearance_anchor`, and `RunState.appearance_anchor_sha256`, with legacy state keys defaulting to `None`.
- Split preview generation into identity preview, `approve_appearance_anchor`, and anchor-only action preview stages.
- Persist the approved frame at `identity/appearance_anchor.png` with `identity/appearance_anchor.json`; reject missing or modified anchors by SHA-256 before resume, approval, or targeted rerun.
- Use user references only for the identity preview. Action preview and full-batch requests send exactly `[appearance_anchor, current_original_frame]` and use the anchor-specific prompt.
- Add `--approve-appearance-anchor` and `-ApproveAppearanceAnchor`; keep `--approve-preview` for the action preview gate.
- Record `identity_paths` and `appearance_anchor_sha256` in preview and frame request evidence.

Focused verification: `python -m pytest 'tools/美术工具/tests' -k gif -q`.

## Action Transfer Prompt Correction Addendum (2026-07-19)

**Goal:** Replace the post-approval frame prompt with the compact two-image contract proven by the Illya action-transfer experiments, without adding a second gateway or sending user reference images after the appearance anchor is approved.

**Architecture:** `appearance_anchor_prompt` becomes a stage-specific prompt: image one remains the sole editable base and appearance source; image two supplies action, pose, expression, gaze, hands, framing, and occlusion. The original character-replacement text remains authoritative during identity-frame generation, while the second-stage highest-priority instruction is the user's approved action request, `把图一修改成图二的动作`. No per-frame text analyzer is added because the configured Gemini image route did not return text in the capability smoke.

### Task 11: Compact Anchor-Base Action Prompt

**Files:**
- Modify: `tools/美术工具/gif_character_replace/identity.py`
- Modify: `tools/美术工具/tests/test_gif_identity.py`
- Verify: `tools/美术工具/tests/test_gif_preview.py`
- Verify: `tools/美术工具/tests/test_gif_workflow.py`

**Interfaces:**
- Produces: `appearance_anchor_prompt(user_prompt: str) -> str`, retaining the existing signature for preview, resume, and targeted rerun callers.
- Preserves: request image order `[appearance_anchor, current_original_frame]`, `count=1`, `stream=true`, anchor SHA-256 checks, and independent-frame generation.

- [ ] **Step 1: Tighten the prompt contract test**

Assert that the prompt starts with `USER TEXT - HIGHEST PRIORITY\n把图一修改成图二的动作。`, assigns image one as the sole edit base, assigns image two as action-only guidance, explicitly replaces the anchor action and expression, excludes the original appearance request from the second-stage prompt, and remains below 500 characters.

- [ ] **Step 2: Run the focused test and confirm the old prompt fails**

```powershell
python -m pytest "tools/美术工具/tests/test_gif_identity.py" -q
```

Expected: the action-prompt test fails because the old implementation starts with the original appearance request and describes transferring appearance onto the current frame.

- [ ] **Step 3: Implement the compact prompt**

Return a compact natural-language contract with exactly these responsibilities: image one is the sole editable base and supplies identity, face, hair, clothing, colors, character rendering, and background; image two supplies only action, pose, expression, head angle, gaze, arms, hands, fingers, character position, framing, and occlusion; conflicting action/expression always follows image two; image-two appearance and background must not be copied; output exactly one image with no text.

- [ ] **Step 4: Run focused and full offline GIF tests**

```powershell
python -m pytest "tools/美术工具/tests/test_gif_identity.py" "tools/美术工具/tests/test_gif_preview.py" "tools/美术工具/tests/test_gif_workflow.py" -q
python -m pytest "tools/美术工具/tests" -k gif -q
```

Expected: all tests pass with zero network calls.

- [ ] **Step 5: Regenerate the existing Illya action preview**

Use the approved anchor and original `frame_0000.png` from run `gif_replace_20260718_231923_234e76a6`, persist a new experimental result and request evidence in the run's temporary experiment directory, and compare it against the source action frame before approving or generating remaining frames.

## Final Acceptance Checklist

- [ ] 8-30 frame preflight and disposal-aware full-frame extraction are proven by tests.
- [ ] Zero-reference and multi-reference identity paths are proven by tests.
- [ ] User text priority and background preservation wording are persisted in evidence.
- [ ] Two-frame preview approval blocks the full batch.
- [ ] Every provider request uses `count=1`, serial execution, and independent source-frame input.
- [ ] Resume and targeted rerun do not repeat accepted frames.
- [ ] Hard failures and visual risks produce distinct statuses and reports.
- [ ] Pillow fallback preserves frame count, durations, loop, canvas, and shared palette without FFmpeg.
- [ ] Offline end-to-end test produces a GIF without network calls.
- [ ] Bounded Gemini preview smoke passes or is recorded as `validation_limited:*`.
- [ ] Manifest, Approved, Registry, Unity, and the gateway submodule remain untouched.
