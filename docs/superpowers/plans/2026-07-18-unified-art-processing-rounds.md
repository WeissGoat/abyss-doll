# Unified Art Processing Rounds Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Replace flat, implicitly trusted `processed/*.png` outputs with unified numeric processing rounds, explicit background policies, safe candidate resolution, and a guarded one-time migration for Project P3 art workspaces.

**Architecture:** A new `art_processing.py` module owns numeric round allocation, decision validation, selected/processed candidate resolution, and atomic publication. A separate `art_background.py` module owns background-policy interpretation, border-connected simple-background removal, and deterministic technical review. Existing PowerShell entry points remain stable while `Optimize-ArtAssets`, Approved sync, integration reports, migration, and P3 art documentation adopt `processed/<positive integer>/`.

**Tech Stack:** Python 3.10, Pillow, JSON, `unittest`, PowerShell wrappers, Project P3 Manifest/Profile workspaces.

## Global Constraints

- The approved design is `docs/superpowers/specs/2026-07-18-unified-art-processing-rounds-design.md`.
- Keep `raw/`, `processed/`, and `selected/` as the stable top-level production directories.
- Processing round directories are positive decimal integers: `processed/1/`, `processed/2/`, `processed/10/`.
- Do not create or use `iterations/` or `round_` directory names.
- New processing rounds recommend `001.png`, `002.png`; migrated files preserve existing filenames.
- Manifest main status remains `todo -> prompted -> generated -> selected -> approved`.
- `AlphaRequired=true` never authorizes background removal by itself.
- `BackgroundPolicy` is exactly one of `preserve`, `already_transparent`, `auto_simple`, or `agent_required`.
- Approved source priority is `Manifest.SelectedPath -> selected/ -> latest numeric processed round with exactly one passed candidate`.
- A failed, decision-required, legacy-unverified, incomplete, or multi-pass-candidate latest round cannot fall back to an earlier round or Approved.
- Preserve Approved files, Registry, runtime bindings, and Unity `.meta`/GUID state.
- The current dirty worktree contains user and prior Agent changes. Stage and commit only each task's listed files.
- `tools/ai-image-gateway` is out of scope.
- Actual image editing for an `agent_required` round remains delegated through `p3-art-asset-production` and `generate-image`; this implementation only provides the safe round registration and resolution infrastructure.

---

### Task 1: Add the numeric processing-round model and shared candidate Resolver

**Files:**
- Create: `tools/美术工具/art_processing.py`
- Create: `tools/美术工具/tests/test_art_processing.py`

**Interfaces:**
- Produces: `RoundReservation`, `CandidateResolution`, `numeric_round_directories()`, `next_round_number()`, `reserve_round()`, `publish_round()`, `load_round_decision()`, `resolve_latest_processed_candidate()`, and `resolve_selected_or_processed_candidate()`.
- Consumes later: Tasks 4-7 import this module rather than scanning `processed` independently.

- [ ] **Step 1: Write failing tests for numeric ordering and next-round allocation**

```python
class ArtProcessingRoundTests(unittest.TestCase):
    def test_numeric_rounds_sort_as_integers_and_ignore_other_names(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            processed = Path(temp_dir)
            for name in ["10", "2", "1", "round_3", ".tmp-4"]:
                (processed / name).mkdir()

            rounds = numeric_round_directories(processed)

            self.assertEqual([number for number, _ in rounds], [1, 2, 10])
            self.assertEqual(next_round_number(processed), 11)

    def test_empty_processed_directory_allocates_round_one(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            self.assertEqual(next_round_number(Path(temp_dir)), 1)
```

- [ ] **Step 2: Write failing tests for safe processed fallback**

```python
def write_decision(round_dir: Path, state: str, candidates: list[dict[str, str]]) -> None:
    (round_dir / "decision.json").write_text(
        json.dumps({"State": state, "Candidates": candidates}),
        encoding="utf-8",
    )


class ArtProcessingResolverTests(unittest.TestCase):
    def test_latest_single_passed_candidate_is_resolved(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            workspace = Path(temp_dir)
            round_dir = workspace / "processed" / "2"
            round_dir.mkdir(parents=True)
            candidate = round_dir / "001.png"
            Image.new("RGBA", (2, 2), (255, 0, 0, 255)).save(candidate)
            digest = hashlib.sha256(candidate.read_bytes()).hexdigest()
            write_decision(
                round_dir,
                "passed",
                [{
                    "File": "001.png",
                    "Status": "passed",
                    "SHA256": digest,
                    "Width": 2,
                    "Height": 2,
                    "Format": "png",
                }],
            )

            result = resolve_latest_processed_candidate(workspace)

            self.assertEqual(result.path, candidate)
            self.assertEqual(result.source_kind, "processed_round")
            self.assertEqual(result.round_number, 2)
            self.assertEqual(result.processing_state, "passed")

    def test_latest_failed_round_does_not_fall_back_to_earlier_passed_round(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            workspace = Path(temp_dir)
            first = workspace / "processed" / "1"
            latest = workspace / "processed" / "2"
            first.mkdir(parents=True)
            latest.mkdir()
            first_file = first / "001.png"
            Image.new("RGBA", (2, 2), (0, 255, 0, 255)).save(first_file)
            write_decision(
                first,
                "passed",
                [{
                    "File": "001.png",
                    "Status": "passed",
                    "SHA256": hashlib.sha256(first_file.read_bytes()).hexdigest(),
                    "Width": 2,
                    "Height": 2,
                    "Format": "png",
                }],
            )
            write_decision(latest, "failed", [])

            result = resolve_latest_processed_candidate(workspace)

            self.assertIsNone(result.path)
            self.assertEqual(result.round_number, 2)
            self.assertEqual(result.processing_state, "failed")

    def test_multiple_passed_candidates_require_selected(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            workspace = Path(temp_dir)
            round_dir = workspace / "processed" / "1"
            round_dir.mkdir(parents=True)
            candidates = []
            for name in ["001.png", "002.png"]:
                path = round_dir / name
                Image.new("RGBA", (2, 2), (0, 0, 255, 255)).save(path)
                candidates.append({
                    "File": name,
                    "Status": "passed",
                    "SHA256": hashlib.sha256(path.read_bytes()).hexdigest(),
                    "Width": 2,
                    "Height": 2,
                    "Format": "png",
                })
            write_decision(round_dir, "passed", candidates)

            result = resolve_latest_processed_candidate(workspace)

            self.assertIsNone(result.path)
            self.assertEqual(result.reason, "multiple_passed_candidates_require_selection")
```

- [ ] **Step 3: Write failing tests for SelectedPath and legacy selected priority**

```python
class ArtProcessingSelectionPriorityTests(unittest.TestCase):
    def test_manifest_selected_path_precedes_selected_directory(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            workspace = Path(temp_dir)
            selected = workspace / "selected"
            selected.mkdir()
            manifest_choice = selected / "b.png"
            legacy_first = selected / "a.png"
            manifest_choice.write_bytes(b"b")
            legacy_first.write_bytes(b"a")

            result = resolve_selected_or_processed_candidate(
                workspace,
                manifest_selected_path=manifest_choice,
            )

            self.assertEqual(result.path, manifest_choice)
            self.assertEqual(result.source_kind, "manifest_selected")

    def test_selected_directory_precedes_processed_round(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            workspace = Path(temp_dir)
            selected = workspace / "selected"
            selected.mkdir()
            selected_file = selected / "a.png"
            selected_file.write_bytes(b"selected")

            result = resolve_selected_or_processed_candidate(workspace)

            self.assertEqual(result.path, selected_file)
            self.assertEqual(result.source_kind, "selected")
```

- [ ] **Step 4: Run the new tests to verify RED**

Run:

```powershell
python -X utf8 -m unittest tools.美术工具.tests.test_art_processing -v
```

Expected: import failure because `art_processing.py` does not exist.

- [ ] **Step 5: Implement the public data types and pure Resolver functions**

```python
@dataclass(frozen=True)
class CandidateResolution:
    path: Path | None
    source_kind: str
    round_number: int | None
    processing_state: str
    reason: str


@dataclass(frozen=True)
class RoundReservation:
    number: int
    processed_dir: Path
    temp_dir: Path
    final_dir: Path
    lock_dir: Path


def numeric_round_directories(processed_dir: Path) -> list[tuple[int, Path]]:
    if not processed_dir.exists():
        return []
    rounds = []
    for child in processed_dir.iterdir():
        if child.is_dir() and child.name.isdecimal() and int(child.name) > 0:
            rounds.append((int(child.name), child))
    return sorted(rounds, key=lambda item: item[0])


def next_round_number(processed_dir: Path) -> int:
    rounds = numeric_round_directories(processed_dir)
    return rounds[-1][0] + 1 if rounds else 1
```

Implement `load_round_decision()` to require a JSON object with `State` in `passed`, `decision_required`, `failed`, or `legacy_unverified`, and a list-valued `Candidates` field. Each candidate must contain `File`, `Status`, `SHA256`, `Width`, `Height`, and `Format`, with optional `Input` and `Reasons`. `resolve_latest_processed_candidate()` must inspect only the maximum numeric round, validate each passed candidate's direct-child filename, decodability, dimensions, format, and SHA-256, and return no path unless exactly one candidate is passed.

Implement `resolve_selected_or_processed_candidate()` with this signature:

```python
def resolve_selected_or_processed_candidate(
    workspace: Path,
    *,
    manifest_selected_path: Path | None = None,
    allowed_input_paths: set[str] | None = None,
) -> CandidateResolution:
    if manifest_selected_path is not None:
        selected = validate_manifest_selected_path(workspace, manifest_selected_path)
        if selected.exists():
            return CandidateResolution(selected, "manifest_selected", None, "selected", "")
    legacy_selected = first_image(workspace / "selected")
    if legacy_selected is not None:
        return CandidateResolution(legacy_selected, "selected", None, "selected", "")
    return resolve_latest_processed_candidate(
        workspace,
        allowed_input_paths=allowed_input_paths,
    )
```

When `allowed_input_paths` is provided, only decision candidates whose optional `Input` field is in that repo-path set may pass CandidateBatch resolution.

- [ ] **Step 6: Implement lock, reservation, and atomic publication**

```python
def reserve_round(processed_dir: Path) -> RoundReservation:
    processed_dir.mkdir(parents=True, exist_ok=True)
    lock_dir = processed_dir / ".processing.lock"
    lock_dir.mkdir()
    number = next_round_number(processed_dir)
    temp_dir = processed_dir / f".tmp-{number}-{uuid.uuid4().hex}"
    temp_dir.mkdir()
    return RoundReservation(number, processed_dir, temp_dir, processed_dir / str(number), lock_dir)


def publish_round(reservation: RoundReservation) -> Path:
    if reservation.final_dir.exists():
        raise FileExistsError(f"Processing round already exists: {reservation.final_dir}")
    reservation.temp_dir.replace(reservation.final_dir)
    reservation.lock_dir.rmdir()
    return reservation.final_dir


def abandon_round(reservation: RoundReservation) -> None:
    if reservation.temp_dir.exists():
        shutil.rmtree(reservation.temp_dir)
    if reservation.lock_dir.exists():
        reservation.lock_dir.rmdir()
```

Add tests proving a second reservation fails while the lock exists, publication renames the temporary directory to the numeric round, and abandoned temporary directories are ignored by the Resolver.

- [ ] **Step 7: Run focused tests and compile**

Run:

```powershell
python -X utf8 -m unittest tools.美术工具.tests.test_art_processing -v
python -X utf8 -m py_compile tools/美术工具/art_processing.py
```

Expected: all focused tests pass; compile exits `0`.

- [ ] **Step 8: Commit Task 1**

```powershell
git add -- tools/美术工具/art_processing.py tools/美术工具/tests/test_art_processing.py
git commit -m "feat: add art processing round resolver"
```

---

### Task 2: Make BackgroundPolicy explicit in generated Specs

**Files:**
- Modify: `tools/美术工具/generate_art_prompts.py`
- Modify: `tools/美术工具/Generate-ArtPrompts.ps1`
- Modify: `tools/美术工具/tests/test_generate_art_prompts.py`

**Interfaces:**
- Produces: every generated `Spec.ProcessSpec.BackgroundPolicy` is explicit.
- Consumes later: Task 4 reads the policy; migration and docs assume the field exists after refresh.

- [ ] **Step 1: Add failing policy tests**

```python
class GenerateArtPromptBackgroundPolicyTests(unittest.TestCase):
    def test_character_portrait_uses_agent_required(self) -> None:
        _, _, _, spec = generate_art_prompts.prompt_for({
            "Domain": "doll",
            "ConfigID": "zero_dialogue_neutral",
            "AssetType": "portrait",
            "VisualID": "doll_zero_dialogue_neutral",
        })
        self.assertEqual(spec["ProcessSpec"]["BackgroundPolicy"], "agent_required")

    def test_item_uses_auto_simple(self) -> None:
        _, _, _, spec = generate_art_prompts.prompt_for({
            "Domain": "item",
            "ConfigID": "loot_gear_scrap",
            "AssetType": "icon",
            "VisualID": "item_loot_gear_scrap_icon",
        })
        self.assertEqual(spec["ProcessSpec"]["BackgroundPolicy"], "auto_simple")

    def test_background_and_narrative_cg_preserve_full_frame(self) -> None:
        for domain, config_id in [
            ("background", "bg_workshop_day"),
            ("narrative_cg", "t0_01a_p02_panel01_workshop_wide"),
        ]:
            _, _, _, spec = generate_art_prompts.prompt_for({
                "Domain": domain,
                "ConfigID": config_id,
                "AssetType": "background",
                "VisualID": f"test_{domain}",
            })
            self.assertEqual(spec["ProcessSpec"]["BackgroundPolicy"], "preserve")

    def test_refresh_spec_only_preserves_prompt_status_and_other_fields(self) -> None:
        entry = {
            "Domain": "item",
            "ConfigID": "loot_gear_scrap",
            "AssetType": "icon",
            "VisualID": "item_loot_gear_scrap_icon",
            "Status": "generated",
            "PromptEN": "existing prompt",
            "PromptCN": "existing review text",
            "Notes": "preserve this",
        }
        result = generate_art_prompts.refresh_spec_only(entry)
        self.assertEqual(result["Status"], "generated")
        self.assertEqual(result["PromptEN"], "existing prompt")
        self.assertEqual(result["PromptCN"], "existing review text")
        self.assertEqual(result["Notes"], "preserve this")
        self.assertEqual(result["Spec"]["ProcessSpec"]["BackgroundPolicy"], "auto_simple")
```

- [ ] **Step 2: Run focused tests to verify RED**

Run:

```powershell
python -X utf8 -m unittest tools.美术工具.tests.test_generate_art_prompts -v
```

Expected: failures with missing `BackgroundPolicy`.

- [ ] **Step 3: Make the policy a required `make_spec()` argument**

Change the signature and ProcessSpec construction:

```python
BACKGROUND_POLICIES = {
    "preserve",
    "already_transparent",
    "auto_simple",
    "agent_required",
}


def make_spec(
    *,
    width: int,
    height: int,
    background: str,
    alpha_required: bool,
    background_policy: str,
    display_width: int,
    display_height: int,
    safe_padding: int,
    subject_min: float,
    subject_max: float,
    post_process: list[str],
    preview_size: int,
    fit_mode: str = "contain",
    pivot: str = "center",
    anchor: str = "center",
    composition: str = "",
    safe_area: str | None = None,
    baseline_percent: int | None = None,
    nine_slice: Dict[str, Any] | None = None,
) -> Dict[str, Any]:
    if background_policy not in BACKGROUND_POLICIES:
        raise ValueError(f"Unsupported BackgroundPolicy: {background_policy}")
    process_spec: Dict[str, Any] = {
        "PostProcess": post_process,
        "PreviewSize": preview_size,
        "BackgroundPolicy": background_policy,
    }
```

Set the stable mappings:

```text
item, node, prosthetic, chassis, chassis_icon, memento, rumor, faction, order, ui, monster_combat -> auto_simple
monster -> preserve
background, narrative_cg -> preserve
doll -> agent_required
```

Do not derive these values from `AlphaRequired` inside `make_spec()`.

- [ ] **Step 4: Add a policy-only refresh path**

Add `--refresh-spec-only` to `generate_art_prompts.py` and `-RefreshSpecOnly` to `Generate-ArtPrompts.ps1`. When selected, call `refresh_spec_only(entry)` for every matching non-deprecated entry, update only `Spec`, and preserve PromptCN, PromptEN, NegativePromptEN, Status, Notes, BatchID, RawPath, SelectedPath, and ApprovedPath byte-for-byte in the JSON values.

- [ ] **Step 5: Run focused and full prompt tests**

Run:

```powershell
python -X utf8 -m unittest tools.美术工具.tests.test_generate_art_prompts -v
python -X utf8 -m py_compile tools/美术工具/generate_art_prompts.py
```

Expected: all tests pass.

- [ ] **Step 6: Commit Task 2**

```powershell
git add -- tools/美术工具/generate_art_prompts.py tools/美术工具/Generate-ArtPrompts.ps1 tools/美术工具/tests/test_generate_art_prompts.py
git commit -m "feat: add explicit art background policies"
```

---

### Task 3: Add safe connected-background processing and technical review

**Files:**
- Create: `tools/美术工具/art_background.py`
- Create: `tools/美术工具/tests/test_art_background.py`

**Interfaces:**
- Produces: `background_policy()`, `remove_connected_background()`, `process_background()`, `measure_candidate()`, and `review_candidate()`.
- Consumes: Task 4 uses these functions instead of `remove_solid_background()`.

- [ ] **Step 1: Write a synthetic white-foreground regression test**

```python
class ConnectedBackgroundRemovalTests(unittest.TestCase):
    def test_enclosed_white_foreground_is_not_removed_with_white_background(self) -> None:
        image = Image.new("RGB", (64, 64), "white")
        draw = ImageDraw.Draw(image)
        draw.rectangle((15, 15, 48, 48), outline="black", width=3, fill="white")

        result = remove_connected_background(image, threshold=34)

        self.assertEqual(result.getpixel((0, 0))[3], 0)
        self.assertEqual(result.getpixel((32, 32))[3], 255)
        self.assertGreater(result.getpixel((15, 32))[3], 0)
```

- [ ] **Step 2: Write failing policy and technical-review tests**

```python
class BackgroundPolicyTests(unittest.TestCase):
    def test_alpha_required_does_not_imply_auto_simple(self) -> None:
        spec = {
            "SourceSpec": {"AlphaRequired": True},
            "ProcessSpec": {},
        }
        with self.assertRaisesRegex(ValueError, "BackgroundPolicy"):
            background_policy(spec)

    def test_agent_required_returns_decision_required_without_processing(self) -> None:
        image = Image.new("RGB", (32, 32), "white")
        result = process_background(image, "agent_required", threshold=34)
        self.assertEqual(result.state, "decision_required")
        self.assertIsNone(result.image)


class CandidateTechnicalReviewTests(unittest.TestCase):
    def test_large_internal_transparency_fails_character_review(self) -> None:
        image = Image.new("RGBA", (100, 100), (255, 255, 255, 0))
        draw = ImageDraw.Draw(image)
        draw.rectangle((10, 5, 90, 95), fill=(100, 100, 100, 255))
        draw.rectangle((25, 20, 75, 80), fill=(255, 255, 255, 0))

        review = review_candidate(
            image,
            source_spec={"Width": 100, "Height": 100, "AlphaRequired": True},
            composition_spec={"SafePaddingPercent": 5},
            production_profile="character_portrait_set",
        )

        self.assertEqual(review["Status"], "failed")
        self.assertIn("transparent_holes_detected", review["Reasons"])
```

- [ ] **Step 3: Run tests to verify RED**

Run:

```powershell
python -X utf8 -m unittest tools.美术工具.tests.test_art_background -v
```

Expected: import failure because `art_background.py` does not exist.

- [ ] **Step 4: Implement border-connected removal**

Use a queue-based flood fill from every border pixel whose RGB distance from the sampled border background is within `threshold`. Only visited, border-connected pixels become transparent. Apply feather alpha only to non-visited pixels immediately adjacent to visited background pixels; do not perform a global color replacement.

Public result type:

```python
@dataclass(frozen=True)
class BackgroundProcessResult:
    state: str
    image: Image.Image | None
    policy: str
    reasons: list[str]
    metrics: dict[str, Any]
```

Implement exact policy behavior:

```python
def process_background(
    image: Image.Image,
    policy: str,
    *,
    threshold: int,
) -> BackgroundProcessResult:
    if policy == "preserve":
        return BackgroundProcessResult("passed", image.copy(), policy, [], {})
    if policy == "already_transparent":
        rgba = image.convert("RGBA")
        if rgba.getchannel("A").getextrema() == (255, 255):
            return BackgroundProcessResult(
                "failed", None, policy, ["expected_existing_alpha"], {}
            )
        return BackgroundProcessResult("passed", rgba, policy, [], {})
    if policy == "agent_required":
        return BackgroundProcessResult(
            "decision_required", None, policy, ["agent_processing_required"], {}
        )
    if policy == "auto_simple":
        return BackgroundProcessResult(
            "passed", remove_connected_background(image, threshold), policy, [], {}
        )
    raise ValueError(f"Unsupported BackgroundPolicy: {policy}")
```

- [ ] **Step 5: Implement deterministic candidate metrics and conservative review**

`measure_candidate()` must record dimensions, mode, alpha extrema, alpha bbox, transparent/opaque/partial ratios, occupied-bbox transparency, top/bottom/left/right margins, and SHA-256 after saving.

`review_candidate()` must fail on corrupt/empty alpha, wrong dimensions, missing required alpha, subject outside the safe canvas, or character-profile occupied-bbox transparency above `0.45`. It must return `warning` rather than fail for metrics that are not universally invalid, such as many connected components on an icon.

- [ ] **Step 6: Run focused tests and compile**

Run:

```powershell
python -X utf8 -m unittest tools.美术工具.tests.test_art_background -v
python -X utf8 -m py_compile tools/美术工具/art_background.py
```

Expected: all tests pass.

- [ ] **Step 7: Commit Task 3**

```powershell
git add -- tools/美术工具/art_background.py tools/美术工具/tests/test_art_background.py
git commit -m "feat: add safe art background processing"
```

---

### Task 4: Refactor Optimize-ArtAssets to publish numeric rounds

**Files:**
- Modify: `tools/美术工具/optimize_art_assets.py`
- Modify: `tools/美术工具/Optimize-ArtAssets.ps1`
- Modify: `tools/美术工具/tests/test_optimize_art_assets.py`

**Interfaces:**
- Consumes: `art_processing.reserve_round/publish_round` and `art_background.process_background/review_candidate`.
- Produces: `processed/<number>/`, per-round reports, root `process_report.json`, and latest contact-sheet compatibility output.

- [ ] **Step 1: Write failing end-to-end round tests**

Create a temporary Manifest with one `standard_asset`, one raw white-background image, and `BackgroundPolicy=auto_simple`. Call `optimize_entry(entry: dict[str, Any], args: argparse.Namespace, in_root: Path) -> dict[str, Any]` directly.

Assert:

```python
self.assertTrue((workspace / "processed" / "1" / "001.png").exists())
self.assertTrue((workspace / "processed" / "1" / "decision.json").exists())
self.assertFalse((workspace / "processed" / "r01_001.png").exists())
self.assertEqual(report["LatestRound"], 1)
self.assertIn(report["LatestState"], {"passed", "failed", "decision_required"})
```

Add a second call and assert it creates `processed/2/`, even when `args.overwrite=True`; `--overwrite` is compatibility syntax for force-reprocessing into a new round and never overwrites `processed/1/`.

- [ ] **Step 2: Write a failing `agent_required` no-implicit-processing test**

```python
def test_agent_required_does_not_create_a_round(self) -> None:
    entry = make_entry(background_policy="agent_required")
    report = optimize_entry(entry, make_args(), incoming_root)

    self.assertFalse((workspace / "processed").exists())
    self.assertEqual(report["LatestState"], "decision_required")
    self.assertEqual(report["Reason"], "agent_processing_required")
```

- [ ] **Step 3: Run focused tests to verify RED**

Run:

```powershell
python -X utf8 -m unittest tools.美术工具.tests.test_optimize_art_assets -v
```

Expected: failures because outputs are still flat and AlphaRequired still triggers old removal.

- [ ] **Step 4: Remove `remove_solid_background()` and delegate background handling**

Delete the global color-replacement implementation. `process_image()` must accept an already background-processed image and perform only deterministic crop, trim, resize, safe-padding, and format operations.

Add:

```python
def round_candidate_name(index: int) -> str:
    return f"{index:03d}.png"


def process_spec_background_policy(spec: Any) -> str:
    return background_policy(spec)
```

- [ ] **Step 5: Implement per-round output and decisions**

For each eligible entry:

1. Read raw inputs.
2. If policy is `agent_required`, return `decision_required` without allocating a round.
3. Reserve the next round.
4. Write candidates to the temporary round as `001.png`, `002.png`, preserving each source path in decision candidate `Input`.
5. Write `technical_review.json`, `process_report.json`, `decision.json`, and `contact_sheet.png`.
6. Publish atomically.
7. Refresh root `process_report.json` with all numeric rounds and latest state.
8. Copy the latest round contact sheet to the existing top-level `contact_sheet/<VisualID>_contact_sheet.png` compatibility path.

Decision candidate shape:

```python
{
    "File": output_path.name,
    "Input": repo_path(raw_path),
    "Status": review["Status"],
    "SHA256": sha256_file(output_path),
    "Width": width,
    "Height": height,
    "Format": "png",
    "Reasons": review["Reasons"],
}
```

Round state is `passed` when at least one candidate passes and no round-level blocker exists; `failed` when none pass.

- [ ] **Step 6: Update dry-run and wrapper compatibility**

Dry-run must print:

```text
[ITEM] <VisualID> policy=<policy> raw=<count> next_round=<number>
```

Keep `-Overwrite` and `--overwrite` accepted, but document and implement them as “create another numeric processing round even if inputs were processed before.” They must never overwrite a published round.

- [ ] **Step 7: Run focused and full tool tests**

Run:

```powershell
python -X utf8 -m unittest tools.美术工具.tests.test_art_processing tools.美术工具.tests.test_art_background tools.美术工具.tests.test_optimize_art_assets -v
python -X utf8 -m unittest discover -s tools/美术工具/tests -v
python -X utf8 -m py_compile tools/美术工具/optimize_art_assets.py
```

Expected: all tests pass.

- [ ] **Step 8: Commit Task 4**

```powershell
git add -- tools/美术工具/optimize_art_assets.py tools/美术工具/Optimize-ArtAssets.ps1 tools/美术工具/tests/test_optimize_art_assets.py
git commit -m "feat: publish processed art in numeric rounds"
```

---

### Task 5: Add a safe registration entry for Agent-produced processing rounds

**Files:**
- Create: `tools/美术工具/register_art_processing_round.py`
- Create: `tools/美术工具/Register-ArtProcessingRound.ps1`
- Create: `tools/美术工具/tests/test_register_art_processing_round.py`
- Modify: `tools/美术工具/README.md`

**Interfaces:**
- Consumes: a staging directory containing direct-child candidate images, `process_report.json`, `technical_review.json`, optional `visual_review.json`, and `decision.json`.
- Produces: the next immutable `processed/<number>/` through Task 1 reservation/publication functions.

- [ ] **Step 1: Write failing registration tests**

```python
class RegisterArtProcessingRoundTests(unittest.TestCase):
    def test_registration_publishes_staging_as_next_numeric_round(self) -> None:
        staging = self.make_staging(
            state="passed",
            candidates=[("001.png", "passed", "red")],
        )
        result = register_processing_round(
            manifest_path=self.manifest_path,
            incoming_root=self.incoming_root,
            visual_id="doll_zero_dialogue_neutral",
            staging_dir=staging,
            dry_run=False,
        )
        self.assertEqual(result["RoundNumber"], 1)
        self.assertTrue((self.workspace / "processed" / "1" / "001.png").exists())

    def test_registration_rejects_hash_mismatch(self) -> None:
        staging = self.make_staging(
            state="passed",
            candidates=[("001.png", "passed", "red")],
        )
        decision = json.loads((staging / "decision.json").read_text(encoding="utf-8"))
        decision["Candidates"][0]["SHA256"] = "0" * 64
        (staging / "decision.json").write_text(json.dumps(decision), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "SHA256"):
            register_processing_round(
                manifest_path=self.manifest_path,
                incoming_root=self.incoming_root,
                visual_id="doll_zero_dialogue_neutral",
                staging_dir=staging,
                dry_run=False,
            )

    def test_registration_rejects_selected_or_approved_paths(self) -> None:
        staging = self.make_staging(
            state="passed",
            candidates=[("001.png", "passed", "red")],
        )
        decision = json.loads((staging / "decision.json").read_text(encoding="utf-8"))
        decision["SelectedPath"] = "selected/001.png"
        (staging / "decision.json").write_text(json.dumps(decision), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "SelectedPath"):
            register_processing_round(
                manifest_path=self.manifest_path,
                incoming_root=self.incoming_root,
                visual_id="doll_zero_dialogue_neutral",
                staging_dir=staging,
                dry_run=False,
            )

    def test_dry_run_writes_nothing(self) -> None:
        staging = self.make_staging(
            state="decision_required",
            candidates=[("001.png", "warning", "red")],
        )
        result = register_processing_round(
            manifest_path=self.manifest_path,
            incoming_root=self.incoming_root,
            visual_id="doll_zero_dialogue_neutral",
            staging_dir=staging,
            dry_run=True,
        )
        self.assertEqual(result["RoundNumber"], 1)
        self.assertFalse((self.workspace / "processed").exists())
```

`make_staging()` must create a temporary directory, save each named candidate as a valid 2x2 RGBA PNG using the requested color, compute each hash, and write `decision.json` with `Width=2`, `Height=2`, and `Format="png"`.

- [ ] **Step 2: Run tests to verify RED**

Run:

```powershell
python -X utf8 -m unittest tools.美术工具.tests.test_register_art_processing_round -v
```

Expected: import failure because the registration module does not exist.

- [ ] **Step 3: Implement staging validation and publication**

```python
def register_processing_round(
    *,
    manifest_path: Path,
    incoming_root: Path,
    visual_id: str,
    staging_dir: Path,
    dry_run: bool,
) -> dict[str, Any]:
    entry = require_unique_manifest_entry(manifest_path, visual_id)
    workspace = workspace_path(incoming_root, entry)
    decision = load_round_decision(staging_dir)
    validate_staging_files(staging_dir, decision)
    round_number = next_round_number(workspace / "processed")
    if dry_run:
        return {"VisualID": visual_id, "RoundNumber": round_number, "State": decision["State"]}
    reservation = reserve_round(workspace / "processed")
    try:
        copy_staging_files(staging_dir, reservation.temp_dir)
        final_dir = publish_round(reservation)
    except Exception:
        abandon_round(reservation)
        raise
    refresh_root_process_report(workspace)
    refresh_latest_contact_sheet(workspace, final_dir)
    return {"VisualID": visual_id, "RoundNumber": reservation.number, "State": decision["State"]}
```

Validation rules:

- staging candidates are direct child images;
- every decision candidate file exists and matches SHA-256;
- a `passed` staging decision for `character_portrait_set` requires `technical_review.json` and `visual_review.json`;
- report paths cannot escape staging;
- `SelectedPath`, `ApprovedPath`, Registry state, and runtime state are forbidden in staging decision data;
- registration does not modify Manifest main status or selected/Approved files.

- [ ] **Step 4: Add the PowerShell wrapper and README usage**

Wrapper parameters:

```powershell
param(
  [string]$ManifestPath = "",
  [string]$IncomingRoot = "",
  [Parameter(Mandatory=$true)][string]$VisualID,
  [Parameter(Mandatory=$true)][string]$StagingDirectory,
  [switch]$DryRun
)
```

Document that `p3-art-asset-production` prepares staging evidence after using a current image capability, runs `-DryRun`, and then registers the round without writing selected or Approved.

- [ ] **Step 5: Run focused/full tests and compile**

Run:

```powershell
python -X utf8 -m unittest tools.美术工具.tests.test_register_art_processing_round -v
python -X utf8 -m unittest discover -s tools/美术工具/tests -v
python -X utf8 -m py_compile tools/美术工具/register_art_processing_round.py
```

Expected: all tests pass.

- [ ] **Step 6: Commit Task 5**

```powershell
git add -- tools/美术工具/register_art_processing_round.py tools/美术工具/Register-ArtProcessingRound.ps1 tools/美术工具/tests/test_register_art_processing_round.py tools/美术工具/README.md
git commit -m "feat: register agent art processing rounds"
```

---

### Task 6: Use the shared Resolver for Approved sync and CandidateBatch

**Files:**
- Modify: `tools/美术工具/sync_approved_art.py`
- Modify: `tools/美术工具/Sync-ApprovedArt.ps1`
- Create: `tools/美术工具/tests/test_sync_approved_art.py`

**Interfaces:**
- Consumes: `resolve_selected_or_processed_candidate()`.
- Produces: guarded Approved source selection for ordinary and CandidateBatch flows.

- [ ] **Step 1: Write failing priority and fallback tests**

Test these exact cases with temporary workspaces:

```python
def test_choose_source_prefers_manifest_selected_path(self) -> None:
    manifest_choice = self.write_selected("b.png", b"manifest")
    self.write_selected("a.png", b"legacy")
    source, kind = choose_source(
        self.workspace,
        {"SelectedPath": repo_path(manifest_choice)},
        candidate_batch=False,
    )
    self.assertEqual((source, kind), (manifest_choice, "manifest_selected"))

def test_choose_source_uses_legacy_selected_directory(self) -> None:
    legacy = self.write_selected("a.png", b"legacy")
    source, kind = choose_source(self.workspace, {}, candidate_batch=False)
    self.assertEqual((source, kind), (legacy, "selected"))

def test_choose_source_uses_latest_single_passed_round_without_flag(self) -> None:
    processed = self.write_round(
        2,
        state="passed",
        candidates=[("001.png", "passed", "raw/source.png")],
    )
    source, kind = choose_source(self.workspace, {}, candidate_batch=False)
    self.assertEqual((source, kind), (processed / "001.png", "processed_round"))

def test_choose_source_rejects_latest_failed_round(self) -> None:
    self.write_round(2, state="failed", candidates=[])
    source, kind = choose_source(self.workspace, {}, candidate_batch=False)
    self.assertIsNone(source)
    self.assertEqual(kind, "")

def test_choose_source_rejects_multiple_passed_candidates(self) -> None:
    self.write_round(
        2,
        state="passed",
        candidates=[
            ("001.png", "passed", "raw/a.png"),
            ("002.png", "passed", "raw/b.png"),
        ],
    )
    source, kind = choose_source(self.workspace, {}, candidate_batch=False)
    self.assertIsNone(source)
    self.assertEqual(kind, "")

def test_candidate_batch_filters_by_decision_input_path(self) -> None:
    processed = self.write_round(
        2,
        state="passed",
        candidates=[
            ("001.png", "passed", "raw/a.png"),
            ("002.png", "passed", "raw/b.png"),
        ],
    )
    entry = {"CandidateRawFiles": [repo_path(self.workspace / "raw" / "b.png")]}
    source, kind = choose_source(self.workspace, entry, candidate_batch=True)
    self.assertEqual((source, kind), (processed / "002.png", "processed_round"))
```

Implement `setUp()`, `write_selected()`, and `write_round()` in the test class. `write_round()` must create a valid 2x2 RGBA PNG with Pillow, compute SHA-256, read width/height/format from Pillow, and write the exact `decision.json` schema from Task 1.

The processed fallback test must call the source-selection function with `allow_processed_fallback=False` and still resolve a safe numeric round. This proves the deprecated flag is no longer required for evidence-backed rounds.

- [ ] **Step 2: Run focused tests to verify RED**

Run:

```powershell
python -X utf8 -m unittest tools.美术工具.tests.test_sync_approved_art -v
```

Expected: import/test failure because the current flat-directory helpers do not support numeric rounds.

- [ ] **Step 3: Replace flat helpers with one entry-aware source function**

Remove `first_candidate_processed()` and flat `first_image(workspace / "processed")` fallback. Add:

```python
def manifest_selected_path(entry: dict[str, Any]) -> Path | None:
    value = str(entry.get("SelectedPath", "") or "").strip()
    return resolve_project_path(value, value) if value else None


def choose_source(
    workspace: Path,
    entry: dict[str, Any],
    *,
    candidate_batch: bool,
) -> tuple[Path | None, str]:
    allowed_inputs = None
    if candidate_batch:
        allowed_inputs = {
            repo_path(resolve_project_path(value, value))
            for value in entry.get("CandidateRawFiles", [])
            if isinstance(value, str) and value.strip()
        }
    result = resolve_selected_or_processed_candidate(
        workspace,
        manifest_selected_path=manifest_selected_path(entry),
        allowed_input_paths=allowed_inputs,
    )
    return result.path, result.source_kind
```

The Resolver must validate that Manifest.SelectedPath stays under the resolved workspace's `selected/` or numeric `processed/` tree.

- [ ] **Step 4: Preserve CLI compatibility**

Keep `--allow-processed-fallback` and `-AllowProcessedFallback` accepted. Print one deprecation warning when supplied:

```text
[WARN] --allow-processed-fallback is deprecated; safe numeric-round fallback is automatic.
```

Do not use the flag to bypass missing/failed decision evidence.

- [ ] **Step 5: Verify Manifest writeback**

When Approved uses `processed/<N>/<file>`, assert the tool writes that exact path into `SelectedPath` before `ApprovedPath` and `Status=approved`. Preserve existing `.meta` guard behavior byte-for-byte.

- [ ] **Step 6: Run focused/full tests and compile**

Run:

```powershell
python -X utf8 -m unittest tools.美术工具.tests.test_sync_approved_art -v
python -X utf8 -m unittest discover -s tools/美术工具/tests -v
python -X utf8 -m py_compile tools/美术工具/sync_approved_art.py
```

Expected: all tests pass.

- [ ] **Step 7: Commit Task 6**

```powershell
git add -- tools/美术工具/sync_approved_art.py tools/美术工具/Sync-ApprovedArt.ps1 tools/美术工具/tests/test_sync_approved_art.py
git commit -m "feat: resolve approved art from numeric rounds"
```

---

### Task 7: Update integration reporting and remaining processed consumers

**Files:**
- Modify: `tools/美术工具/generate_art_integration_candidates.py`
- Modify: `tools/美术工具/generate_art_batch_plan.py`
- Modify: `tools/美术工具/run_art_generation.py`
- Create: `tools/美术工具/tests/test_generate_art_integration_candidates.py`

**Interfaces:**
- Consumes: shared Resolver and numeric round metadata.
- Produces: generated candidates with `ProcessedRound`, `ProcessingState`, and safe routing.

- [ ] **Step 1: Write failing classification tests**

Build temporary workspaces and call `classify_entry()` for the following exact assertions:

```python
def test_latest_passed_single_candidate_routes_art_select(self) -> None:
    self.write_round(2, "passed", [("001.png", "passed")])
    result = self.classify()
    self.assertEqual(result["Action"], "art_select")
    self.assertEqual(result["ProcessedRound"], 2)
    self.assertEqual(result["ProcessingState"], "passed")
    self.assertTrue(result["ProcessedCandidate"].endswith("processed/2/001.png"))

def test_latest_failed_round_routes_art_select_with_failure_reason(self) -> None:
    self.write_round(2, "failed", [])
    result = self.classify()
    self.assertEqual(result["Action"], "art_select")
    self.assertEqual(result["ProcessedRound"], 2)
    self.assertEqual(result["ProcessingState"], "failed")
    self.assertEqual(result["ProcessedCandidate"], "")
    self.assertIn("failed", result["Reason"])

def test_raw_without_round_routes_art_process(self) -> None:
    self.write_raw("001.png")
    result = self.classify()
    self.assertEqual(result["Action"], "art_process")

def test_selected_still_routes_art_approve(self) -> None:
    self.write_selected("001.png")
    result = self.classify()
    self.assertEqual(result["Action"], "art_approve")
```

Implement the fixture helpers inside the test class using a minimal Manifest entry, empty registry map, and empty screen references.

Expected payload fields:

```python
self.assertEqual(result["ProcessedRound"], 2)
self.assertEqual(result["ProcessingState"], "failed")
self.assertEqual(result["ProcessedCandidate"], "")
self.assertEqual(result["Action"], "art_select")
self.assertIn("failed", result["Reason"])
```

- [ ] **Step 2: Run focused test to verify RED**

Run:

```powershell
python -X utf8 -m unittest tools.美术工具.tests.test_generate_art_integration_candidates -v
```

Expected: failures because the current implementation scans flat `processed` images and lacks processing metadata.

- [ ] **Step 3: Replace flat processed scanning in classification**

Use both `resolve_selected_or_processed_candidate()` and `resolve_latest_processed_candidate()`. Keep selected/Approved routing ahead of processing state. Add these output fields to JSON and Markdown:

```text
ProcessedRound
ProcessingState
ProcessedCandidate
```

Routing rules:

```python
if selected_image is not None:
    action = "art_approve"
elif latest.processing_state in {"failed", "decision_required", "legacy_unverified"}:
    action = "art_select"
elif latest.path is not None:
    action = "art_select"
elif raw_image is not None:
    action = "art_process"
```

- [ ] **Step 4: Update remaining path producers**

`generate_art_batch_plan.py` may continue exposing the workspace `processed` root, but its instructions must say that candidates live under numeric child directories and only decision-backed candidates are eligible.

`run_art_generation.py` may create the empty `processed` root but must not create flat files or imply a round exists.

- [ ] **Step 5: Run focused/full tests and compile**

Run:

```powershell
python -X utf8 -m unittest tools.美术工具.tests.test_generate_art_integration_candidates -v
python -X utf8 -m unittest discover -s tools/美术工具/tests -v
python -X utf8 -m py_compile tools/美术工具/generate_art_integration_candidates.py tools/美术工具/generate_art_batch_plan.py tools/美术工具/run_art_generation.py
```

Expected: all tests pass.

- [ ] **Step 6: Commit Task 7**

```powershell
git add -- tools/美术工具/generate_art_integration_candidates.py tools/美术工具/generate_art_batch_plan.py tools/美术工具/run_art_generation.py tools/美术工具/tests/test_generate_art_integration_candidates.py
git commit -m "feat: report numeric art processing state"
```

---

### Task 8: Add the guarded one-time processed-round migration tool

**Files:**
- Create: `tools/美术工具/migrate_art_processed_rounds.py`
- Create: `tools/美术工具/Migrate-ArtProcessedRounds.ps1`
- Create: `tools/美术工具/tests/test_migrate_art_processed_rounds.py`
- Modify: `tools/美术工具/README.md`

**Interfaces:**
- Consumes: Manifest entries and Profile workspace resolver.
- Produces: dry-run migration plans, `processed/1/` moves, legacy decisions, SelectedPath rewrites, and hash evidence.

- [ ] **Step 1: Write failing migration-plan tests**

Cover these exact tests:

```python
def test_plan_moves_flat_processed_files_to_round_one_preserving_names(self) -> None:
    source = self.write_flat_processed("legacy_name.png", b"image")
    _, migrations = self.plan()
    move = migrations[0].moves[0]
    self.assertEqual(move.source, source)
    self.assertEqual(move.destination, source.parent / "1" / "legacy_name.png")

def test_plan_moves_adjacent_meta_with_image(self) -> None:
    source = self.write_flat_processed("legacy.png", b"image")
    meta = source.with_name(source.name + ".meta")
    meta.write_bytes(b"guid")
    _, migrations = self.plan()
    destinations = {move.destination.name for move in migrations[0].moves}
    self.assertEqual(destinations, {"legacy.png", "legacy.png.meta"})

def test_plan_blocks_when_flat_files_and_round_one_both_exist(self) -> None:
    self.write_flat_processed("legacy.png", b"image")
    (self.workspace / "processed" / "1").mkdir()
    with self.assertRaisesRegex(ValueError, "round 1 already exists"):
        self.plan()

def test_plan_rewrites_selected_path_only_when_it_points_to_moved_file(self) -> None:
    source = self.write_flat_processed("legacy.png", b"image")
    self.entry["SelectedPath"] = repo_path(source)
    _, migrations = self.plan()
    self.assertEqual(
        migrations[0].selected_path_after,
        repo_path(source.parent / "1" / "legacy.png"),
    )

def test_dry_run_writes_nothing(self) -> None:
    source = self.write_flat_processed("legacy.png", b"image")
    _, migrations = self.plan()
    self.assertTrue(source.exists())
    self.assertFalse((source.parent / "1").exists())
    self.assertEqual(len(migrations), 1)

def test_existing_failed_production_decision_marks_round_failed(self) -> None:
    self.write_flat_processed("legacy.png", b"image")
    (self.workspace / "production_decision.json").write_text(
        json.dumps({
            "state": "decision_required",
            "hard_gate": {"status": "failed", "code": "transparent_background_contract_failed"},
        }),
        encoding="utf-8",
    )
    _, migrations = self.plan()
    self.assertEqual(migrations[0].round_state, "failed")
```

Implement a shared fixture that creates one temporary Manifest entry and resolves its workspace through `art_workspace.workspace_path()`.

`write_flat_processed()` must save a valid 2x2 PNG with Pillow, not arbitrary bytes, so the migration test exercises the same decodable-image boundary as production.

The selected-path test must assert identical SHA-256 before/after and the new path `processed/1/<original-name>`.

- [ ] **Step 2: Run focused tests to verify RED**

Run:

```powershell
python -X utf8 -m unittest tools.美术工具.tests.test_migrate_art_processed_rounds -v
```

Expected: import failure because the migration module does not exist.

- [ ] **Step 3: Implement a structured plan**

```python
@dataclass(frozen=True)
class FileMove:
    source: Path
    destination: Path
    sha256: str


@dataclass(frozen=True)
class WorkspaceMigration:
    visual_id: str
    workspace: Path
    moves: list[FileMove]
    round_state: str
    selected_path_before: str
    selected_path_after: str
```

Public functions:

Implement these exact public signatures:

```python
def plan_migration(
    *,
    manifest_path: Path,
    incoming_root: Path,
) -> tuple[dict[str, Any], list[WorkspaceMigration]]:
    manifest = read_json(manifest_path)
    migrations = collect_workspace_migrations(manifest, incoming_root)
    return manifest, migrations


def execute_migration(
    *,
    manifest_path: Path,
    manifest: dict[str, Any],
    migrations: list[WorkspaceMigration],
) -> dict[str, Any]:
    summary = apply_workspace_migrations(manifest, migrations)
    write_json(manifest_path, manifest)
    return summary
```

`collect_workspace_migrations()` and `apply_workspace_migrations()` are private helpers in the same module. The former is read-only; the latter performs validated moves, writes round evidence, verifies hashes, and updates only matching SelectedPath values.

For each workspace, scan only direct image children of `processed/`. Preserve filenames. Include direct-child `.meta` files in moves. Block if `processed/1/` already exists alongside flat images.

- [ ] **Step 4: Generate migration decisions and root reports**

Default migrated state is `legacy_unverified`. If an existing `production_decision.json` records a failed hard gate for the flat candidate, write round `State=failed` and candidate `Status=failed` with that reason.

Write `processed/1/decision.json`, `processed/1/process_report.json`, and root `process_report.json`. Do not claim technical pass without existing evidence.

- [ ] **Step 5: Make execution opt-in and verify paths before moving**

Python CLI:

```text
--manifest-path
--incoming-root
--execute
```

Without `--execute`, print JSON/text summary and write nothing. The PowerShell wrapper exposes `-Execute`; default invocation is dry-run. Resolve every source and target under the expected workspace before using `Move-Item`/`Path.replace()`. Compare SHA-256 after each move.

- [ ] **Step 6: Document usage**

README commands:

```powershell
.\tools\美术工具\Migrate-ArtProcessedRounds.ps1
.\tools\美术工具\Migrate-ArtProcessedRounds.ps1 -Execute
```

State that the first command is mandatory before execution and that Approved/Registry are never touched.

- [ ] **Step 7: Run focused/full tests and compile**

Run:

```powershell
python -X utf8 -m unittest tools.美术工具.tests.test_migrate_art_processed_rounds -v
python -X utf8 -m unittest discover -s tools/美术工具/tests -v
python -X utf8 -m py_compile tools/美术工具/migrate_art_processed_rounds.py
```

Expected: all tests pass.

- [ ] **Step 8: Commit Task 8**

```powershell
git add -- tools/美术工具/migrate_art_processed_rounds.py tools/美术工具/Migrate-ArtProcessedRounds.ps1 tools/美术工具/tests/test_migrate_art_processed_rounds.py tools/美术工具/README.md
git commit -m "feat: add processed round migration tool"
```

---

### Task 9: Execute the migration and convert the Zero failure into round evidence

**Files:**
- Modify generated: `美术文档/_generated/art_manifest.json`
- Modify generated: `美术文档/_generated/AI绘图提示词清单.md`
- Modify ignored workspaces: `UnityClient/Assets/Art/_IncomingAI/standard_assets/*/processed/`
- Modify ignored workspace: `UnityClient/Assets/Art/_IncomingAI/character_portraits/doll_zero_dialogue_neutral/processed/`
- Modify ignored evidence: `UnityClient/Assets/Art/_IncomingAI/**/process_report.json`

**Interfaces:**
- Consumes: Tasks 1-8.
- Produces: no flat processed image children, explicit BackgroundPolicy, and Zero `processed/1` failed evidence.

- [ ] **Step 1: Save protected hashes**

Save SHA-256 for:

```text
UnityClient/Assets/Art/Approved/**
UnityClient/Assets/Resources/VisualAssetRegistry.asset
美术文档/_generated/art_manifest.json
美术文档/人设/AI出图/zero_dialogue_differences_20260712_01/selected/zero_dialogue_neutral.png
```

Store the snapshot under `$env:TEMP/P3ProcessedRoundMigration/protected-before.json`.

- [ ] **Step 2: Refresh BackgroundPolicy only through the owning prompt generator**

Run:

```powershell
.\tools\美术工具\Generate-ArtPrompts.ps1 -RefreshSpecOnly
```

Assert all non-deprecated Manifest entries have a legal `Spec.ProcessSpec.BackgroundPolicy`. Preserve existing statuses. If this command updates unrelated Prompt text, stop and add a policy-only refresh option before continuing rather than accepting broad prompt churn.

- [ ] **Step 3: Run migration dry-run and audit every conflict**

Run:

```powershell
.\tools\美术工具\Migrate-ArtProcessedRounds.ps1
```

Expected summary includes workspace count, flat file count, `.meta` count, SelectedPath rewrite count, legacy-unverified count, failed count, and zero conflicts. Save the summary under `$env:TEMP/P3ProcessedRoundMigration/dry-run.json`.

- [ ] **Step 4: Execute migration**

Run only after the dry-run reports zero conflicts:

```powershell
.\tools\美术工具\Migrate-ArtProcessedRounds.ps1 -Execute
```

Expected: all flat processed images move into `processed/1/`; existing numeric rounds are untouched; each migrated workspace receives decision/index evidence.

- [ ] **Step 5: Verify Zero's exact state**

Assert:

```text
character_portraits/doll_zero_dialogue_neutral/processed/r01_001.png does not exist
character_portraits/doll_zero_dialogue_neutral/processed/1/r01_001.png exists
processed/1/decision.json State=failed
Manifest Status=generated
SelectedPath=""
ApprovedPath=""
RegistryStatus=unregistered
```

- [ ] **Step 6: Verify migration boundaries**

Compare all protected hashes except Manifest, which is expected to change only for BackgroundPolicy and any necessary migrated SelectedPath. Assert Approved, Registry, source candidate, and all moved `.meta` contents are byte-identical.

- [ ] **Step 7: Refresh generated art reports**

Run:

```powershell
.\tools\美术工具\Generate-ArtIntegrationCandidates.ps1
.\tools\美术工具\Generate-ArtQualityBacklog.ps1
.\tools\美术工具\Generate-ArtProgramHandoff.ps1
```

Assert Zero reports `ProcessedRound=1`, `ProcessingState=failed`, no `ProcessedCandidate`, and never `program_integrate`.

- [ ] **Step 8: Commit only tracked migration outputs**

Do not attempt to add ignored `_IncomingAI` files. Stage only the Manifest/prompt generated files that changed through owning scripts and are required by the migration:

```powershell
git add -- 美术文档/_generated/art_manifest.json 美术文档/_generated/AI绘图提示词清单.md
git commit -m "art: migrate processing specs to numeric rounds"
```

If those files contain unrelated pre-existing changes, do not commit them in this step; document the verified local migration state and leave them unstaged.

---

### Task 10: Update Project P3 facts, Skill instructions, and final validation

**Files:**
- Modify: `美术文档/00_美术流水线总览.md`
- Modify: `美术文档/01_Manifest规范.md`
- Modify: `美术文档/02_资源规格与接入规范.md`
- Modify: `美术文档/03_AI生成与筛选规范.md`
- Modify: `.codex/skills/p3-art-asset-production/SKILL.md`
- Modify: `.codex/skills/p3-art-asset-production/references/state-machine.md`
- Modify: `.codex/skills/p3-art-asset-production/references/candidate-evaluation.md`
- Modify: `.codex/skills/p3-art-asset-production/references/evidence-and-writeback.md`
- Modify: `.codex/skills/p3-art-asset-production/references/workspace-profiles-and-character-portraits.md`
- Modify: `docs/superpowers/specs/2026-07-18-zero-dialogue-neutral-character-portrait-pilot-design.md`
- Modify: `agent_status/art.md`
- Generated: `DOCS_INDEX.md`
- Generated: `docs_index.json`

**Interfaces:**
- Consumes: verified implementation and migration evidence.
- Produces: one consistent active description of numeric processing rounds and the Zero resume state.

- [ ] **Step 1: Update active art facts**

Document exactly:

```text
raw -> processed/<number> -> selected -> Approved
```

Remove active `iterations/` structure. Define BackgroundPolicy, numeric sorting, per-round decisions, single-candidate fallback, multi-candidate selection, and latest-failed-no-fallback behavior. State that standard batch remains one external command while character processing can be Agent-driven.

- [ ] **Step 2: Update the production Skill and references**

Require the Agent to:

- read the latest numeric round;
- record actual processing capability in round evidence;
- never write complex edits directly to selected/Approved;
- stop on failed/decision-required/latest multi-candidate rounds;
- use `processed/<next integer>/` for repairs;
- preserve the method-neutral Asset Contract.

- [ ] **Step 3: Correct the historical pilot design**

Add a prominent note that its flat `processed/r01_001.png` expectation was superseded by the unified numeric-round design. Do not rewrite its historical scope or pretend the pilot passed.

- [ ] **Step 4: Update art status**

Record:

- unified numeric processing implementation status;
- migration counts and hash evidence;
- Zero is `processed/1 failed`, with the next safe attempt allocated as round `2`;
- no Approved/Registry/Unity changes;
- next action is Agent-owned foreground-preserving processing under the new workflow.

- [ ] **Step 5: Refresh and validate documentation**

Run:

```powershell
.\tools\docs\Generate-DocsIndex.ps1
.\tools\docs\Validate-Docs.ps1
```

Expected: docs validation passes.

- [ ] **Step 6: Run the complete implementation verification**

Run:

```powershell
python -X utf8 -m unittest discover -s tools/美术工具/tests -v
python -X utf8 -m py_compile `
  tools/美术工具/art_processing.py `
  tools/美术工具/art_background.py `
  tools/美术工具/optimize_art_assets.py `
  tools/美术工具/sync_approved_art.py `
  tools/美术工具/generate_art_integration_candidates.py `
  tools/美术工具/migrate_art_processed_rounds.py
.\tools\美术工具\Optimize-ArtAssets.ps1 -VisualID doll_zero_dialogue_neutral -Status generated -Limit 1 -DryRun -SkipIntegrationCandidates
.\tools\美术工具\Sync-ApprovedArt.ps1 -VisualID doll_zero_dialogue_neutral -DryRun -SkipIntegrationCandidates
git diff --check
.\tools\agent\Invoke-AgentHealthCheck.ps1
```

Expected:

- tests have zero failures;
- compile exits `0`;
- Zero optimize dry-run reports `BackgroundPolicy=agent_required` and next round `2` without writing;
- Zero Approved dry-run skips because latest round failed and there is no selected candidate;
- diff check has no errors;
- health check has no new error beyond known dirty-worktree warnings.

- [ ] **Step 7: Re-run protected hash comparison**

Assert Approved, Registry, original Zero source, and all guarded `.meta` files match the pre-migration snapshot. Record `validation_limited:unity_not_run` because this task intentionally does not modify or inspect live Unity.

- [ ] **Step 8: Commit documentation and status**

```powershell
git add -- `
  美术文档/00_美术流水线总览.md `
  美术文档/01_Manifest规范.md `
  美术文档/02_资源规格与接入规范.md `
  美术文档/03_AI生成与筛选规范.md `
  .codex/skills/p3-art-asset-production/SKILL.md `
  .codex/skills/p3-art-asset-production/references/state-machine.md `
  .codex/skills/p3-art-asset-production/references/candidate-evaluation.md `
  .codex/skills/p3-art-asset-production/references/evidence-and-writeback.md `
  .codex/skills/p3-art-asset-production/references/workspace-profiles-and-character-portraits.md `
  docs/superpowers/specs/2026-07-18-zero-dialogue-neutral-character-portrait-pilot-design.md `
  agent_status/art.md `
  DOCS_INDEX.md `
  docs_index.json
git commit -m "docs: adopt numeric art processing rounds"
```

Stage only files whose task-related diff has been reviewed. Leave unrelated pre-existing changes unstaged.

---

## Execution Completion Gate

Before reporting implementation complete, verify every design requirement maps to evidence:

```text
Numeric round model                  -> Task 1 tests
Explicit BackgroundPolicy           -> Task 2 tests and refreshed Manifest
Foreground-preserving auto_simple   -> Task 3 synthetic regression tests
One-command standard batch          -> Task 4 integration test
Agent-required no implicit removal  -> Task 4 test and Zero dry-run
Agent round registration            -> Task 5 tests
Approved source precedence          -> Task 6 tests
Integration report compatibility    -> Task 7 tests and generated report
One-time migration and .meta guard  -> Task 8 tests + Task 9 hashes
Zero failed round restoration       -> Task 9 evidence
Facts/Skill/status consistency       -> Task 10 docs validation
```

Do not begin Zero round `2` image editing as part of this implementation plan. After the infrastructure and migration are verified, resume it as a new `p3-art-asset-production` execution using the new `agent_required` flow.
