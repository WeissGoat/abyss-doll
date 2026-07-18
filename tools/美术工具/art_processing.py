# -*- coding: utf-8 -*-
"""Shared processing-round allocation and candidate resolution for P3 art."""

from __future__ import annotations

import hashlib
import json
import re
import shutil
import uuid
from dataclasses import dataclass
from pathlib import Path
from typing import Any

from PIL import Image


IMAGE_EXTENSIONS = {".png", ".jpg", ".jpeg", ".webp"}
DECISION_STATES = {"passed", "decision_required", "failed", "legacy_unverified"}
REQUIRED_CANDIDATE_FIELDS = {"File", "Status", "SHA256", "Width", "Height", "Format"}
ASCII_DECIMAL = re.compile(r"^[0-9]+$")


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
    """Return only positive ASCII-integer round directories, sorted numerically."""

    if not processed_dir.exists() or not processed_dir.is_dir():
        return []

    rounds: list[tuple[int, Path]] = []
    for child in processed_dir.iterdir():
        if not child.is_dir() or not ASCII_DECIMAL.fullmatch(child.name):
            continue
        number = int(child.name)
        if number > 0:
            rounds.append((number, child))
    return sorted(rounds, key=lambda item: item[0])


def next_round_number(processed_dir: Path) -> int:
    rounds = numeric_round_directories(processed_dir)
    return rounds[-1][0] + 1 if rounds else 1


def reserve_round(processed_dir: Path) -> RoundReservation:
    """Reserve the next round using an exclusive directory lock."""

    processed_dir.mkdir(parents=True, exist_ok=True)
    lock_dir = processed_dir / ".processing.lock"
    lock_dir.mkdir()
    number = next_round_number(processed_dir)
    temp_dir = processed_dir / f".tmp-{number}-{uuid.uuid4().hex}"
    temp_dir.mkdir()
    return RoundReservation(
        number=number,
        processed_dir=processed_dir,
        temp_dir=temp_dir,
        final_dir=processed_dir / str(number),
        lock_dir=lock_dir,
    )


def publish_round(reservation: RoundReservation) -> Path:
    """Atomically publish a reserved temporary directory as its numeric round."""

    if not reservation.lock_dir.exists():
        raise RuntimeError("Processing round is not locked.")
    if not reservation.temp_dir.exists():
        raise FileNotFoundError(f"Temporary processing directory is missing: {reservation.temp_dir}")
    if reservation.final_dir.exists():
        raise FileExistsError(f"Processing round already exists: {reservation.final_dir}")

    reservation.temp_dir.replace(reservation.final_dir)
    reservation.lock_dir.rmdir()
    return reservation.final_dir


def abandon_round(reservation: RoundReservation) -> None:
    """Remove an unpublished reservation and release its lock."""

    if reservation.temp_dir.exists():
        shutil.rmtree(reservation.temp_dir)
    if reservation.lock_dir.exists():
        reservation.lock_dir.rmdir()


def load_round_decision(round_dir: Path) -> dict[str, Any]:
    """Load and validate a round decision contract."""

    decision_path = round_dir / "decision.json"
    data = json.loads(decision_path.read_text(encoding="utf-8-sig"))
    if not isinstance(data, dict):
        raise ValueError("Round decision must be a JSON object.")

    state = data.get("State")
    if state not in DECISION_STATES:
        raise ValueError(f"Round decision State must be one of {sorted(DECISION_STATES)}.")
    candidates = data.get("Candidates")
    if not isinstance(candidates, list):
        raise ValueError("Round decision Candidates must be a list.")

    for index, candidate in enumerate(candidates):
        if not isinstance(candidate, dict):
            raise ValueError(f"Round decision candidate {index} must be an object.")
        missing = REQUIRED_CANDIDATE_FIELDS.difference(candidate)
        if missing:
            raise ValueError(
                f"Round decision candidate {index} is missing fields: {', '.join(sorted(missing))}."
            )
    return data


def _normalize_format(value: str) -> str:
    normalized = str(value or "").strip().lower().lstrip(".")
    return {"jpg": "jpeg"}.get(normalized, normalized)


def _image_metadata(path: Path) -> dict[str, Any] | None:
    try:
        with Image.open(path) as image:
            image.load()
            width, height = image.size
            image_format = _normalize_format(str(image.format or ""))
    except (OSError, ValueError):
        return None
    return {
        "SHA256": hashlib.sha256(path.read_bytes()).hexdigest(),
        "Width": width,
        "Height": height,
        "Format": image_format,
    }


def _is_direct_child(round_dir: Path, file_value: Any) -> bool:
    if not isinstance(file_value, str) or not file_value.strip():
        return False
    relative = Path(file_value)
    if relative.name != file_value or relative.is_absolute() or relative.parts in ((), (".",)):
        return False
    if relative.name in {".", ".."} or relative.suffix.lower() not in IMAGE_EXTENSIONS:
        return False
    candidate = (round_dir / relative.name).resolve(strict=False)
    return candidate.parent == round_dir.resolve(strict=False)


def _candidate_matches(
    round_dir: Path,
    candidate: dict[str, Any],
    allowed_input_paths: set[str] | None,
) -> Path | None:
    if candidate.get("Status") != "passed":
        return None
    if allowed_input_paths is not None:
        input_value = candidate.get("Input")
        normalized_inputs = {value.replace("\\", "/") for value in allowed_input_paths}
        if not isinstance(input_value, str) or input_value.replace("\\", "/") not in normalized_inputs:
            return None
    if not _is_direct_child(round_dir, candidate.get("File")):
        return None

    path = round_dir / str(candidate["File"])
    actual = _image_metadata(path)
    if actual is None:
        return None
    try:
        expected_width = int(candidate["Width"])
        expected_height = int(candidate["Height"])
    except (TypeError, ValueError):
        return None
    expected_hash = str(candidate["SHA256"]).lower()
    expected_format = _normalize_format(str(candidate["Format"]))
    if expected_width <= 0 or expected_height <= 0:
        return None
    if (
        actual["SHA256"].lower() != expected_hash
        or actual["Width"] != expected_width
        or actual["Height"] != expected_height
        or actual["Format"] != expected_format
    ):
        return None
    return path


def resolve_latest_processed_candidate(
    workspace: Path,
    *,
    allowed_input_paths: set[str] | None = None,
) -> CandidateResolution:
    """Resolve only the latest numeric round; never fall back to an older round."""

    processed_dir = workspace / "processed"
    rounds = numeric_round_directories(processed_dir)
    if not rounds:
        return CandidateResolution(None, "processed_round", None, "missing", "no_processed_rounds")

    number, round_dir = rounds[-1]
    try:
        decision = load_round_decision(round_dir)
    except FileNotFoundError:
        return CandidateResolution(None, "processed_round", number, "missing", "decision_missing")
    except (OSError, json.JSONDecodeError, ValueError):
        return CandidateResolution(None, "processed_round", number, "invalid", "decision_invalid")

    state = str(decision["State"])
    if state != "passed":
        return CandidateResolution(None, "processed_round", number, state, f"round_state_{state}")

    valid_paths = [
        path
        for candidate in decision["Candidates"]
        if isinstance(candidate, dict)
        for path in [_candidate_matches(round_dir, candidate, allowed_input_paths)]
        if path is not None
    ]
    if len(valid_paths) == 1:
        return CandidateResolution(valid_paths[0], "processed_round", number, state, "")
    if len(valid_paths) > 1:
        return CandidateResolution(
            None,
            "processed_round",
            number,
            state,
            "multiple_passed_candidates_require_selection",
        )
    return CandidateResolution(None, "processed_round", number, state, "no_valid_passed_candidate")


def _safe_manifest_selected_path(workspace: Path, path: Path) -> Path | None:
    candidate = path if path.is_absolute() else workspace / path
    resolved = candidate.resolve(strict=False)
    allowed = [workspace.resolve(strict=False) / "selected"]
    allowed.extend(round_dir.resolve(strict=False) for _, round_dir in numeric_round_directories(workspace / "processed"))
    if not any(_is_path_within(resolved, root) for root in allowed):
        return None
    if not candidate.is_file() or candidate.suffix.lower() not in IMAGE_EXTENSIONS:
        return None
    return candidate


def _is_path_within(path: Path, directory: Path) -> bool:
    try:
        path.relative_to(directory)
        return True
    except ValueError:
        return False


def _first_selected_image(selected_dir: Path) -> Path | None:
    if not selected_dir.exists() or not selected_dir.is_dir():
        return None
    for path in sorted(selected_dir.iterdir(), key=lambda item: item.name.lower()):
        if path.is_file() and path.suffix.lower() in IMAGE_EXTENSIONS:
            return path
    return None


def resolve_selected_or_processed_candidate(
    workspace: Path,
    *,
    manifest_selected_path: Path | None = None,
    allowed_input_paths: set[str] | None = None,
) -> CandidateResolution:
    """Resolve Manifest selection, legacy selected/, or the latest processed round."""

    if manifest_selected_path is not None:
        selected = _safe_manifest_selected_path(workspace, manifest_selected_path)
        if selected is not None:
            return CandidateResolution(selected, "manifest_selected", None, "selected", "")

    legacy_selected = _first_selected_image(workspace / "selected")
    if legacy_selected is not None:
        return CandidateResolution(legacy_selected, "selected", None, "selected", "")

    return resolve_latest_processed_candidate(
        workspace,
        allowed_input_paths=allowed_input_paths,
    )
