"""Identity selection and deterministic two-frame preview gates."""

from __future__ import annotations

import asyncio
import hashlib
import json
from dataclasses import dataclass, replace
from datetime import datetime, timezone
from pathlib import Path
from statistics import mean
from typing import Sequence

import numpy as np
from PIL import Image, ImageFilter

from .action_text import action_text_for_frame
from .backend import ImageBackend
from .identity import (
    IdentityContract,
    IdentityInputs,
    appearance_anchor_prompt,
    prepare_identity_inputs,
)
from .models import RunState, RunStatus
from .store import RunStore, append_jsonl, atomic_write_json


@dataclass(frozen=True)
class PreviewSelection:
    identity_index: int
    action_index: int


def _rgb_delta(left: Image.Image, right: Image.Image) -> float:
    left_array = np.asarray(left.convert("RGB"), dtype=np.float32)
    right_array = np.asarray(right.convert("RGB"), dtype=np.float32)
    if left_array.shape != right_array.shape:
        raise ValueError("preview frames must share one canvas size")
    return float(np.abs(left_array - right_array).mean())


def select_preview_frames(frame_paths: Sequence[Path]) -> PreviewSelection:
    """Choose the clearest stable identity frame and the most different action frame."""

    if len(frame_paths) < 2:
        raise ValueError("at least two frames are required for preview selection")
    frames: list[Image.Image] = []
    for path in frame_paths:
        with Image.open(path) as image:
            frames.append(image.convert("RGBA").copy())

    scores: list[float] = []
    for index, frame in enumerate(frames):
        edge_variance = float(
            np.asarray(
                frame.convert("L").filter(ImageFilter.FIND_EDGES), dtype=np.float32
            ).var()
        )
        neighbor_deltas: list[float] = []
        if index > 0:
            neighbor_deltas.append(_rgb_delta(frames[index - 1], frame))
        if index + 1 < len(frames):
            neighbor_deltas.append(_rgb_delta(frame, frames[index + 1]))
        neighbor_motion = mean(neighbor_deltas) if neighbor_deltas else 0.0
        scores.append(edge_variance - 0.5 * neighbor_motion)

    identity_index = max(range(len(scores)), key=lambda index: (scores[index], -index))
    action_candidates = [index for index in range(len(frames)) if index != identity_index]
    action_index = max(
        action_candidates,
        key=lambda index: (_rgb_delta(frames[identity_index], frames[index]), -index),
    )
    return PreviewSelection(identity_index, action_index)


def _utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


def _write_generated(
    path: Path,
    generated,
    source_index: int,
    request_started_at: str,
    request_finished_at: str,
    *,
    identity_paths: Sequence[str] = (),
    appearance_anchor_sha256: str | None = None,
    action_text: str | None = None,
) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(generated.image_bytes)
    payload = {
        "source_index": source_index,
        "provider": generated.provider,
        "model": generated.model,
        "seed": generated.seed,
        "generation_params": generated.generation_params,
        "cost": generated.cost,
        "byte_length": len(generated.image_bytes),
        "request_started_at": request_started_at,
        "request_finished_at": request_finished_at,
        "identity_paths": list(identity_paths),
    }
    if appearance_anchor_sha256:
        payload["appearance_anchor_sha256"] = appearance_anchor_sha256
    if action_text:
        payload["action_text"] = action_text
    atomic_write_json(
        path.with_suffix(".json"),
        payload,
    )


def _failure_record(
    *,
    source_index: int,
    provider: str,
    request_started_at: str,
    exc: Exception,
    identity_paths: Sequence[str] = (),
    appearance_anchor_sha256: str | None = None,
    action_text: str | None = None,
) -> dict:
    payload = {
        "source_index": source_index,
        "provider": provider,
        "error": str(exc),
        "gateway_attempt_count": int(getattr(exc, "attempts", 1)),
        "gateway_errors": list(getattr(exc, "errors", (str(exc),))),
        "request_started_at": request_started_at,
        "request_finished_at": _utc_now(),
        "identity_paths": list(identity_paths),
    }
    if appearance_anchor_sha256:
        payload["appearance_anchor_sha256"] = appearance_anchor_sha256
    if action_text:
        payload["action_text"] = action_text
    return payload


def _persist_request_failure(store: RunStore, metadata_path: Path, payload: dict) -> None:
    atomic_write_json(metadata_path, payload)
    append_jsonl(store.paths.reports_dir / "requests.jsonl", payload)
    store.update_state(
        lambda state: replace(
            state,
            status=RunStatus.FAILED,
            errors=(*state.errors, str(payload["error"])),
        )
    )


async def generate_identity_candidates(
    store: RunStore,
    backend: ImageBackend,
    contract: IdentityContract,
) -> RunState:
    """Generate three serial anchors only when the user supplied no references."""

    inputs = prepare_identity_inputs(contract, store.paths.identity_dir)
    if inputs.provider_image_paths:
        return store.update_state(
            lambda state: replace(
                state,
                selected_identity=None,
                status=RunStatus.PREFLIGHT,
            )
        )

    state = store.load_state()
    if state.timeline is None:
        raise ValueError("timeline is required before identity generation")
    candidate_dir = store.paths.identity_dir / "anchor_candidates"
    for index in range(3):
        if index > 0 and store.config.delay_seconds > 0:
            await asyncio.sleep(store.config.delay_seconds)
        request_started_at = _utc_now()
        try:
            generated = await backend.generate_anchor(
                contract.provider_prompt, state.timeline.width, state.timeline.height
            )
        except Exception as exc:
            payload = _failure_record(
                source_index=-1,
                provider=store.config.anchor_provider,
                request_started_at=request_started_at,
                exc=exc,
            )
            _persist_request_failure(
                store, candidate_dir / f"anchor_{index:02d}.json", payload
            )
            raise
        _write_generated(
            candidate_dir / f"anchor_{index:02d}.png",
            generated,
            -1,
            request_started_at,
            _utc_now(),
        )
        append_jsonl(
            store.paths.reports_dir / "requests.jsonl",
            json.loads((candidate_dir / f"anchor_{index:02d}.json").read_text(encoding="utf-8")),
        )
    return store.update_state(
        lambda current: replace(
            current,
            status=RunStatus.AWAITING_IDENTITY_SELECTION,
            selected_identity=None,
        )
    )


def approve_identity(store: RunStore, candidate_index: int) -> RunState:
    candidate = store.paths.identity_dir / "anchor_candidates" / f"anchor_{candidate_index:02d}.png"
    if not candidate.is_file():
        raise FileNotFoundError(candidate)
    with Image.open(candidate) as image:
        image.verify()
    return store.update_state(
        lambda state: replace(
            state,
            selected_identity=str(candidate.resolve()),
            status=RunStatus.PREFLIGHT,
        )
    )


def _contract_prompt(inputs: IdentityInputs) -> str:
    payload = json.loads(Path(inputs.contract_path).read_text(encoding="utf-8"))
    return str(payload["provider_prompt"])


def _anchor_prompt(inputs: IdentityInputs, action_text: str | None = None) -> str:
    payload = json.loads(Path(inputs.contract_path).read_text(encoding="utf-8"))
    return appearance_anchor_prompt(str(payload["user_prompt"]), action_text)


def _file_sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _validate_preview_image(path: Path, expected_size: tuple[int, int]) -> None:
    if not path.is_file():
        raise FileNotFoundError(path)
    try:
        with Image.open(path) as image:
            image.load()
            if image.size != expected_size:
                raise ValueError(
                    f"preview size {image.size} does not match timeline {expected_size}: {path}"
                )
    except OSError as exc:
        raise ValueError(f"preview is not decodable: {path}") from exc


def validate_appearance_anchor(store: RunStore, state: RunState | None = None) -> Path:
    current = state or store.load_state()
    if not current.appearance_anchor or not current.appearance_anchor_sha256:
        raise ValueError("appearance anchor approval is required")
    anchor = Path(current.appearance_anchor)
    if not anchor.is_file():
        raise FileNotFoundError(anchor)
    actual_sha256 = _file_sha256(anchor)
    if actual_sha256 != current.appearance_anchor_sha256:
        raise ValueError(
            "appearance anchor hash mismatch: "
            f"expected {current.appearance_anchor_sha256}, received {actual_sha256}"
        )
    if current.timeline is not None:
        _validate_preview_image(anchor, (current.timeline.width, current.timeline.height))
    return anchor


async def generate_identity_preview(
    store: RunStore,
    backend: ImageBackend,
    identity_inputs: IdentityInputs,
) -> RunState:
    """Generate the identity preview from user references and one source frame."""

    state = store.load_state()
    if state.timeline is None:
        raise ValueError("timeline is required before preview generation")
    if state.appearance_anchor:
        raise ValueError("appearance anchor is already approved; start a new run")
    frame_paths = [store.paths.timeline_dir / path for path in state.timeline.frame_paths]
    selection = select_preview_frames(frame_paths)
    store.update_state(
        lambda current: replace(
            current,
            preview_indices=(selection.identity_index, selection.action_index),
            preview_approved=False,
            appearance_anchor=None,
            appearance_anchor_sha256=None,
        )
    )

    if state.selected_identity and Path(state.selected_identity).is_file():
        identity_paths = (Path(state.selected_identity),)
    else:
        identity_paths = tuple(Path(path) for path in identity_inputs.provider_image_paths)
    if not identity_paths:
        raise ValueError("an approved identity or reference image is required")
    identity_images = [path.read_bytes() for path in identity_paths]
    prompt = _contract_prompt(identity_inputs)

    index = selection.identity_index
    output_path = store.paths.preview_dir / "identity_frame" / f"frame_{index:04d}.png"
    request_started_at = _utc_now()
    try:
        generated = await backend.replace_frame(
            identity_images,
            frame_paths[index].read_bytes(),
            prompt,
            state.timeline.width,
            state.timeline.height,
        )
    except Exception as exc:
        payload = _failure_record(
            source_index=index,
            provider=store.config.frame_provider,
            request_started_at=request_started_at,
            exc=exc,
            identity_paths=[str(path.resolve()) for path in identity_paths],
        )
        _persist_request_failure(store, output_path.with_suffix(".json"), payload)
        raise
    _write_generated(
        output_path,
        generated,
        index,
        request_started_at,
        _utc_now(),
        identity_paths=[str(path.resolve()) for path in identity_paths],
    )
    append_jsonl(
        store.paths.reports_dir / "requests.jsonl",
        json.loads(output_path.with_suffix(".json").read_text(encoding="utf-8")),
    )

    return store.update_state(
        lambda current: replace(
            current,
            status=RunStatus.AWAITING_APPEARANCE_APPROVAL,
            preview_indices=(selection.identity_index, selection.action_index),
            preview_approved=False,
            errors=(),
        )
    )


async def generate_preview(
    store: RunStore,
    backend: ImageBackend,
    identity_inputs: IdentityInputs,
) -> RunState:
    """Compatibility entry point for the first, identity-only preview stage."""

    return await generate_identity_preview(store, backend, identity_inputs)


def approve_appearance_anchor(store: RunStore) -> RunState:
    state = store.load_state()
    if state.timeline is None or state.preview_indices is None:
        raise ValueError("identity preview has not been generated")
    if state.appearance_anchor:
        validate_appearance_anchor(store, state)
        if state.status not in (
            RunStatus.PREFLIGHT,
            RunStatus.AWAITING_PREVIEW_APPROVAL,
        ):
            raise ValueError(
                f"appearance anchor cannot be re-approved from status {state.status.value}"
            )
        return state
    if state.status != RunStatus.AWAITING_APPEARANCE_APPROVAL:
        raise ValueError(
            "appearance anchor approval requires awaiting_appearance_approval status"
        )
    expected_size = (state.timeline.width, state.timeline.height)
    source = (
        store.paths.preview_dir
        / "identity_frame"
        / f"frame_{state.preview_indices[0]:04d}.png"
    )
    _validate_preview_image(source, expected_size)
    destination = store.paths.identity_dir / "appearance_anchor.png"
    temporary = store.paths.identity_dir / "appearance_anchor.tmp"
    temporary.write_bytes(source.read_bytes())
    temporary.replace(destination)
    sha256 = _file_sha256(destination)
    metadata_path = store.paths.identity_dir / "appearance_anchor.json"
    atomic_write_json(
        metadata_path,
        {
            "path": str(destination.resolve()),
            "sha256": sha256,
            "source_preview": str(source.resolve()),
            "source_index": state.preview_indices[0],
            "approved_at": _utc_now(),
        },
    )
    return store.update_state(
        lambda current: replace(
            current,
            status=RunStatus.PREFLIGHT,
            appearance_anchor=str(destination.resolve()),
            appearance_anchor_sha256=sha256,
            preview_approved=False,
            errors=(),
        )
    )


async def generate_action_preview(
    store: RunStore,
    backend: ImageBackend,
    identity_inputs: IdentityInputs,
) -> RunState:
    """Generate the action preview from the approved appearance anchor only."""

    state = store.load_state()
    if state.timeline is None or state.preview_indices is None:
        raise ValueError("preview selection is not available")
    if state.status != RunStatus.PREFLIGHT:
        raise ValueError(
            f"action preview requires preflight after anchor approval, received {state.status.value}"
        )
    anchor = validate_appearance_anchor(store, state)
    index = state.preview_indices[1]
    action_text = action_text_for_frame(store, index)
    frame_path = store.paths.timeline_dir / state.timeline.frame_paths[index]
    output_path = store.paths.preview_dir / "action_frame" / f"frame_{index:04d}.png"
    request_started_at = _utc_now()
    try:
        generated = await backend.replace_frame(
            [anchor.read_bytes()],
            frame_path.read_bytes(),
            _anchor_prompt(identity_inputs, action_text),
            state.timeline.width,
            state.timeline.height,
        )
    except Exception as exc:
        payload = _failure_record(
            source_index=index,
            provider=store.config.frame_provider,
            request_started_at=request_started_at,
            exc=exc,
            identity_paths=[str(anchor.resolve())],
            appearance_anchor_sha256=state.appearance_anchor_sha256,
            action_text=action_text,
        )
        _persist_request_failure(store, output_path.with_suffix(".json"), payload)
        raise
    _write_generated(
        output_path,
        generated,
        index,
        request_started_at,
        _utc_now(),
        identity_paths=[str(anchor.resolve())],
        appearance_anchor_sha256=state.appearance_anchor_sha256,
        action_text=action_text,
    )
    append_jsonl(
        store.paths.reports_dir / "requests.jsonl",
        json.loads(output_path.with_suffix(".json").read_text(encoding="utf-8")),
    )
    return store.update_state(
        lambda current: replace(
            current,
            status=RunStatus.AWAITING_PREVIEW_APPROVAL,
            preview_approved=False,
            errors=(),
        )
    )


async def rerun_preview_frame(
    store: RunStore,
    backend: ImageBackend,
    identity_inputs: IdentityInputs,
    frame_index: int,
    extra_prompt: str = "",
) -> RunState:
    """Replace one selected preview frame without approving or running the batch."""

    state = store.load_state()
    if state.timeline is None or state.preview_indices is None:
        raise ValueError("preview selection is not available")
    if frame_index not in state.preview_indices:
        raise ValueError(f"frame {frame_index} is not one of the selected preview frames")
    is_identity_frame = frame_index == state.preview_indices[0]
    label = "identity_frame" if is_identity_frame else "action_frame"
    if is_identity_frame and state.appearance_anchor:
        raise ValueError("identity preview cannot be rerun after appearance anchor approval")
    if is_identity_frame and state.selected_identity and Path(state.selected_identity).is_file():
        identity_paths = (Path(state.selected_identity),)
        prompt = _contract_prompt(identity_inputs)
    elif is_identity_frame:
        identity_paths = tuple(Path(path) for path in identity_inputs.provider_image_paths)
        prompt = _contract_prompt(identity_inputs)
    else:
        identity_paths = (validate_appearance_anchor(store, state),)
        action_text = action_text_for_frame(store, frame_index)
        prompt = _anchor_prompt(identity_inputs, action_text)
    if not identity_paths:
        raise ValueError("an approved identity or reference image is required")
    if extra_prompt:
        prompt = f"{prompt}\n\n{extra_prompt}"
    frame_path = store.paths.timeline_dir / state.timeline.frame_paths[frame_index]
    request_started_at = _utc_now()
    try:
        generated = await backend.replace_frame(
            [path.read_bytes() for path in identity_paths],
            frame_path.read_bytes(),
            prompt,
            state.timeline.width,
            state.timeline.height,
        )
    except Exception as exc:
        payload = _failure_record(
            source_index=frame_index,
            provider=store.config.frame_provider,
            request_started_at=request_started_at,
            exc=exc,
            identity_paths=[str(path.resolve()) for path in identity_paths],
            appearance_anchor_sha256=(
                state.appearance_anchor_sha256 if not is_identity_frame else None
            ),
            action_text=action_text if not is_identity_frame else None,
        )
        _persist_request_failure(
            store,
            store.paths.preview_dir / label / f"frame_{frame_index:04d}.json",
            payload,
        )
        raise
    output_path = store.paths.preview_dir / label / f"frame_{frame_index:04d}.png"
    _write_generated(
        output_path,
        generated,
        frame_index,
        request_started_at,
        _utc_now(),
        identity_paths=[str(path.resolve()) for path in identity_paths],
        appearance_anchor_sha256=(
            state.appearance_anchor_sha256 if not is_identity_frame else None
        ),
        action_text=action_text if not is_identity_frame else None,
    )
    append_jsonl(
        store.paths.reports_dir / "requests.jsonl",
        json.loads(output_path.with_suffix(".json").read_text(encoding="utf-8")),
    )
    return store.update_state(
        lambda current: replace(
            current,
            status=(
                RunStatus.AWAITING_APPEARANCE_APPROVAL
                if is_identity_frame
                else RunStatus.AWAITING_PREVIEW_APPROVAL
            ),
            preview_approved=False,
            errors=(),
        )
    )


def approve_preview(store: RunStore) -> RunState:
    state = store.load_state()
    if state.timeline is None or state.preview_indices is None:
        raise ValueError("preview has not been generated")
    if state.status != RunStatus.AWAITING_PREVIEW_APPROVAL:
        raise ValueError(
            f"action preview approval requires awaiting_preview_approval status, received {state.status.value}"
        )
    validate_appearance_anchor(store, state)
    expected_size = (state.timeline.width, state.timeline.height)
    action_path = (
        store.paths.preview_dir
        / "action_frame"
        / f"frame_{state.preview_indices[1]:04d}.png"
    )
    _validate_preview_image(action_path, expected_size)
    return store.update_state(
        lambda current: replace(
            current,
            status=RunStatus.RUNNING,
            preview_approved=True,
        )
    )
