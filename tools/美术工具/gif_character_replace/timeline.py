"""GIF preflight and disposal-aware full-frame extraction."""

from __future__ import annotations

import hashlib
from pathlib import Path

from PIL import Image, UnidentifiedImageError

from .models import TimelineMetadata
from .store import atomic_write_json


class PreflightError(ValueError):
    """Raised when an input cannot enter the replacement workflow."""


def _sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as source:
        for chunk in iter(lambda: source.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def extract_timeline(
    input_gif: Path,
    frames_dir: Path,
    min_frames: int,
    max_frames: int,
) -> TimelineMetadata:
    """Validate and extract a GIF as disposal-composited RGBA PNG frames."""

    input_gif = input_gif.resolve()
    frames_dir = frames_dir.resolve()
    if min_frames < 1 or max_frames < min_frames:
        raise ValueError("invalid frame bounds")
    if not input_gif.is_file():
        raise PreflightError(f"input GIF does not exist: {input_gif}")

    try:
        with Image.open(input_gif) as gif:
            if gif.format != "GIF":
                raise PreflightError(f"input is not a GIF: {input_gif}")

            frame_count = int(getattr(gif, "n_frames", 1))
            if not min_frames <= frame_count <= max_frames:
                raise PreflightError(
                    f"GIF frame count {frame_count} is outside "
                    f"the supported range {min_frames}-{max_frames}"
                )

            width, height = gif.size
            loop = int(gif.info.get("loop", 0) or 0)
            has_transparency = "transparency" in gif.info
            durations: list[int] = []
            disposal_methods: list[int] = []
            frames: list[Image.Image] = []

            # Explicit seek and copy are required so Pillow applies each GIF
            # disposal operation before a frame is handed to image generation.
            for index in range(frame_count):
                gif.seek(index)
                frame = gif.convert("RGBA").copy()
                frames.append(frame)
                durations.append(int(gif.info.get("duration", 0) or 0))
                disposal_methods.append(int(getattr(gif, "disposal_method", 0) or 0))
                if not has_transparency:
                    alpha_min, _ = frame.getchannel("A").getextrema()
                    has_transparency = alpha_min < 255
    except PreflightError:
        raise
    except (EOFError, OSError, UnidentifiedImageError) as exc:
        raise PreflightError(f"could not decode GIF: {input_gif}") from exc

    frames_dir.mkdir(parents=True, exist_ok=True)
    frame_paths: list[str] = []
    for index, frame in enumerate(frames):
        frame_path = frames_dir / f"frame_{index:04d}.png"
        frame.save(frame_path, format="PNG")
        frame_paths.append(frame_path.relative_to(frames_dir.parent).as_posix())

    metadata = TimelineMetadata(
        width=width,
        height=height,
        frame_count=frame_count,
        durations_ms=tuple(durations),
        loop=loop,
        has_transparency=has_transparency,
        disposal_methods=tuple(disposal_methods),
        source_sha256=_sha256(input_gif),
        frame_paths=tuple(frame_paths),
    )
    atomic_write_json(frames_dir.parent / "source_metadata.json", metadata.to_dict())
    return metadata
