"""Serializable contracts shared by the GIF replacement workflow."""

from __future__ import annotations

import json
import uuid
from dataclasses import asdict, dataclass
from datetime import datetime
from enum import Enum
from pathlib import Path
from typing import Any, Sequence


class RunStatus(str, Enum):
    PREFLIGHT = "preflight"
    AWAITING_IDENTITY_SELECTION = "awaiting_identity_selection"
    AWAITING_APPEARANCE_APPROVAL = "awaiting_appearance_approval"
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
        prompt = prompt.strip()
        if not prompt:
            raise ValueError("prompt must not be empty")
        if not input_gif.exists() or not input_gif.is_file():
            raise FileNotFoundError(input_gif)
        timestamp = datetime.now().strftime("%Y%m%d_%H%M%S")
        run_id = f"gif_replace_{timestamp}_{uuid.uuid4().hex[:8]}"
        return cls(
            run_id=run_id,
            input_gif=str(input_gif.resolve()),
            output_root=str(output_root.resolve()),
            prompt=prompt,
            references=tuple(str(path.resolve()) for path in references),
            config_path=str(config_path.resolve()) if config_path else "",
        )

    def to_dict(self) -> dict[str, Any]:
        payload = asdict(self)
        payload["references"] = list(self.references)
        return payload

    @classmethod
    def from_dict(cls, payload: dict[str, Any]) -> "RunConfig":
        data = dict(payload)
        data["references"] = tuple(data.get("references", ()))
        return cls(**data)


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

    @classmethod
    def for_config(cls, config: RunConfig) -> "RunPaths":
        root = Path(config.output_root) / "gif_character_replace" / config.run_id
        return cls(
            root=root,
            input_dir=root / "input",
            timeline_dir=root / "timeline",
            original_frames_dir=root / "timeline" / "frames_original",
            identity_dir=root / "identity",
            preview_dir=root / "preview",
            generated_raw_dir=root / "generated" / "raw",
            accepted_dir=root / "generated" / "accepted",
            rejected_dir=root / "generated" / "rejected",
            output_dir=root / "output",
            reports_dir=root / "reports",
        )

    def create_directories(self) -> None:
        for directory in (
            self.input_dir,
            self.timeline_dir,
            self.original_frames_dir,
            self.identity_dir,
            self.preview_dir,
            self.generated_raw_dir,
            self.accepted_dir,
            self.rejected_dir,
            self.output_dir,
            self.reports_dir,
        ):
            directory.mkdir(parents=True, exist_ok=True)


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

    def to_dict(self) -> dict[str, Any]:
        payload = asdict(self)
        payload["durations_ms"] = list(self.durations_ms)
        payload["disposal_methods"] = list(self.disposal_methods)
        payload["frame_paths"] = list(self.frame_paths)
        return payload

    @classmethod
    def from_dict(cls, payload: dict[str, Any]) -> "TimelineMetadata":
        data = dict(payload)
        for key in ("durations_ms", "disposal_methods", "frame_paths"):
            data[key] = tuple(data.get(key, ()))
        return cls(**data)


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

    def to_dict(self) -> dict[str, Any]:
        payload = asdict(self)
        payload["status"] = self.status.value
        payload["request_records"] = list(self.request_records)
        payload["risks"] = list(self.risks)
        payload["errors"] = list(self.errors)
        return payload

    @classmethod
    def from_dict(cls, payload: dict[str, Any]) -> "FrameRecord":
        data = dict(payload)
        data["status"] = FrameStatus(data["status"])
        data["request_records"] = tuple(data.get("request_records", ()))
        data["risks"] = tuple(data.get("risks", ()))
        data["errors"] = tuple(data.get("errors", ()))
        return cls(**data)


@dataclass(frozen=True)
class RunState:
    status: RunStatus
    timeline: TimelineMetadata | None
    selected_identity: str | None
    appearance_anchor: str | None
    appearance_anchor_sha256: str | None
    preview_indices: tuple[int, int] | None
    preview_approved: bool
    frames: tuple[FrameRecord, ...]
    review_report: str | None
    contact_sheet: str | None
    result_gif: str | None
    warnings: tuple[str, ...]
    errors: tuple[str, ...]

    @classmethod
    def initial(cls) -> "RunState":
        return cls(
            status=RunStatus.PREFLIGHT,
            timeline=None,
            selected_identity=None,
            appearance_anchor=None,
            appearance_anchor_sha256=None,
            preview_indices=None,
            preview_approved=False,
            frames=(),
            review_report=None,
            contact_sheet=None,
            result_gif=None,
            warnings=(),
            errors=(),
        )

    def to_dict(self) -> dict[str, Any]:
        return {
            "status": self.status.value,
            "timeline": self.timeline.to_dict() if self.timeline else None,
            "selected_identity": self.selected_identity,
            "appearance_anchor": self.appearance_anchor,
            "appearance_anchor_sha256": self.appearance_anchor_sha256,
            "preview_indices": list(self.preview_indices) if self.preview_indices else None,
            "preview_approved": self.preview_approved,
            "frames": [frame.to_dict() for frame in self.frames],
            "review_report": self.review_report,
            "contact_sheet": self.contact_sheet,
            "result_gif": self.result_gif,
            "warnings": list(self.warnings),
            "errors": list(self.errors),
        }

    @classmethod
    def from_dict(cls, payload: dict[str, Any]) -> "RunState":
        preview_indices = payload.get("preview_indices")
        return cls(
            status=RunStatus(payload["status"]),
            timeline=TimelineMetadata.from_dict(payload["timeline"]) if payload.get("timeline") else None,
            selected_identity=payload.get("selected_identity"),
            appearance_anchor=payload.get("appearance_anchor"),
            appearance_anchor_sha256=payload.get("appearance_anchor_sha256"),
            preview_indices=tuple(preview_indices) if preview_indices else None,
            preview_approved=bool(payload.get("preview_approved", False)),
            frames=tuple(FrameRecord.from_dict(frame) for frame in payload.get("frames", ())),
            review_report=payload.get("review_report"),
            contact_sheet=payload.get("contact_sheet"),
            result_gif=payload.get("result_gif"),
            warnings=tuple(payload.get("warnings", ())),
            errors=tuple(payload.get("errors", ())),
        )


def json_dumps(payload: dict[str, Any]) -> str:
    return json.dumps(payload, ensure_ascii=False, indent=2) + "\n"
