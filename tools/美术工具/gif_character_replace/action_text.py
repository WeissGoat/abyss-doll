"""Validated per-frame action descriptions for anchor-based GIF editing."""

from __future__ import annotations

import json
from dataclasses import dataclass
from pathlib import Path

from .store import RunStore, atomic_write_json


ACTION_TEXT_FILENAME = "frame_action_texts.json"
MAX_ACTION_TEXT_LENGTH = 500


@dataclass(frozen=True)
class FrameActionTexts:
    source_sha256: str
    frame_count: int
    actions: dict[int, str]

    def to_dict(self) -> dict:
        return {
            "source_sha256": self.source_sha256,
            "frame_count": self.frame_count,
            "actions": {str(index): self.actions[index] for index in sorted(self.actions)},
        }


def _parse_payload(store: RunStore, payload: dict) -> FrameActionTexts:
    state = store.load_state()
    if state.timeline is None:
        raise ValueError("timeline is required before installing action texts")
    if not isinstance(payload, dict):
        raise ValueError("action text sidecar must contain one JSON object")

    source_sha256 = str(payload.get("source_sha256", "")).strip()
    if source_sha256 != state.timeline.source_sha256:
        raise ValueError(
            "source_sha256 mismatch: "
            f"expected {state.timeline.source_sha256}, received {source_sha256 or '<blank>'}"
        )
    frame_count = payload.get("frame_count")
    if frame_count != state.timeline.frame_count:
        raise ValueError(
            "frame_count mismatch: "
            f"expected {state.timeline.frame_count}, received {frame_count}"
        )
    raw_actions = payload.get("actions")
    if not isinstance(raw_actions, dict):
        raise ValueError("actions must be a JSON object keyed by frame index")

    actions: dict[int, str] = {}
    for raw_index, raw_text in raw_actions.items():
        try:
            index = int(raw_index)
        except (TypeError, ValueError) as exc:
            raise ValueError(f"action indices must be integers: {raw_index!r}") from exc
        text = str(raw_text).strip()
        if not text:
            raise ValueError(f"action text must not be blank for frame {index}")
        if len(text) > MAX_ACTION_TEXT_LENGTH:
            raise ValueError(
                f"action text exceeds {MAX_ACTION_TEXT_LENGTH} characters for frame {index}"
            )
        actions[index] = text

    expected_indices = set(range(state.timeline.frame_count))
    actual_indices = set(actions)
    if actual_indices != expected_indices:
        missing = sorted(expected_indices - actual_indices)
        extra = sorted(actual_indices - expected_indices)
        raise ValueError(f"action indices mismatch: missing={missing}, extra={extra}")
    return FrameActionTexts(source_sha256, state.timeline.frame_count, actions)


def install_action_texts(store: RunStore, source_path: Path) -> Path:
    if store.load_state().preview_approved:
        raise ValueError("action texts cannot change after preview approval")
    source = Path(source_path).expanduser().resolve()
    if not source.is_file():
        raise FileNotFoundError(source)
    try:
        payload = json.loads(source.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        raise ValueError(f"invalid action text JSON: {source}") from exc
    action_texts = _parse_payload(store, payload)
    destination = store.paths.identity_dir / ACTION_TEXT_FILENAME
    atomic_write_json(destination, action_texts.to_dict())
    return destination


def load_action_texts(store: RunStore) -> FrameActionTexts | None:
    path = store.paths.identity_dir / ACTION_TEXT_FILENAME
    if not path.is_file():
        return None
    try:
        payload = json.loads(path.read_text(encoding="utf-8"))
    except json.JSONDecodeError as exc:
        raise ValueError(f"invalid stored action text JSON: {path}") from exc
    return _parse_payload(store, payload)


def action_text_for_frame(store: RunStore, frame_index: int) -> str | None:
    action_texts = load_action_texts(store)
    if action_texts is None:
        return None
    if frame_index not in action_texts.actions:
        raise IndexError(f"frame index out of action text range: {frame_index}")
    return action_texts.actions[frame_index]
