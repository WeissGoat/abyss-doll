"""Identity selection and deterministic two-frame preview gates."""

from __future__ import annotations

import asyncio
import json
from dataclasses import dataclass, replace
from datetime import datetime, timezone
from pathlib import Path
from statistics import mean
from typing import Sequence

import numpy as np
from PIL import Image, ImageFilter

from .backend import ImageBackend
from .identity import IdentityContract, IdentityInputs, prepare_identity_inputs
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
) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_bytes(generated.image_bytes)
    atomic_write_json(
        path.with_suffix(".json"),
        {
            "source_index": source_index,
            "provider": generated.provider,
            "model": generated.model,
            "seed": generated.seed,
            "generation_params": generated.generation_params,
            "cost": generated.cost,
            "byte_length": len(generated.image_bytes),
            "request_started_at": request_started_at,
            "request_finished_at": request_finished_at,
        },
    )


def _failure_record(
    *,
    source_index: int,
    provider: str,
    request_started_at: str,
    exc: Exception,
) -> dict:
    return {
        "source_index": source_index,
        "provider": provider,
        "error": str(exc),
        "gateway_attempt_count": int(getattr(exc, "attempts", 1)),
        "gateway_errors": list(getattr(exc, "errors", (str(exc),))),
        "request_started_at": request_started_at,
        "request_finished_at": _utc_now(),
    }


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


async def generate_preview(
    store: RunStore,
    backend: ImageBackend,
    identity_inputs: IdentityInputs,
) -> RunState:
    """Generate only the identity and action preview frames."""

    state = store.load_state()
    if state.timeline is None:
        raise ValueError("timeline is required before preview generation")
    frame_paths = [store.paths.timeline_dir / path for path in state.timeline.frame_paths]
    selection = select_preview_frames(frame_paths)

    if state.selected_identity and Path(state.selected_identity).is_file():
        identity_paths = (Path(state.selected_identity),)
    else:
        identity_paths = tuple(Path(path) for path in identity_inputs.provider_image_paths)
    if not identity_paths:
        raise ValueError("an approved identity or reference image is required")
    identity_images = [path.read_bytes() for path in identity_paths]
    prompt = _contract_prompt(identity_inputs)

    for request_index, (label, index) in enumerate((
        ("identity_frame", selection.identity_index),
        ("action_frame", selection.action_index),
    )):
        if request_index > 0 and store.config.delay_seconds > 0:
            await asyncio.sleep(store.config.delay_seconds)
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
            )
            _persist_request_failure(
                store,
                store.paths.preview_dir / label / f"frame_{index:04d}.json",
                payload,
            )
            raise
        _write_generated(
            store.paths.preview_dir / label / f"frame_{index:04d}.png",
            generated,
            index,
            request_started_at,
            _utc_now(),
        )
        append_jsonl(
            store.paths.reports_dir / "requests.jsonl",
            json.loads(
                (
                    store.paths.preview_dir / label / f"frame_{index:04d}.json"
                ).read_text(encoding="utf-8")
            ),
        )

    return store.update_state(
        lambda current: replace(
            current,
            status=RunStatus.AWAITING_PREVIEW_APPROVAL,
            preview_indices=(selection.identity_index, selection.action_index),
            preview_approved=False,
        )
    )


def approve_preview(store: RunStore) -> RunState:
    state = store.load_state()
    if state.timeline is None or state.preview_indices is None:
        raise ValueError("preview has not been generated")
    expected_size = (state.timeline.width, state.timeline.height)
    locations = (
        store.paths.preview_dir / "identity_frame" / f"frame_{state.preview_indices[0]:04d}.png",
        store.paths.preview_dir / "action_frame" / f"frame_{state.preview_indices[1]:04d}.png",
    )
    for path in locations:
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
    return store.update_state(
        lambda current: replace(
            current,
            status=RunStatus.RUNNING,
            preview_approved=True,
        )
    )
