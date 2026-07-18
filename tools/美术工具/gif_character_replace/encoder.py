"""Variable-duration GIF encoding with FFmpeg and Pillow paths."""

from __future__ import annotations

import shutil
import subprocess
import tempfile
from dataclasses import dataclass
from pathlib import Path
from typing import Sequence

from PIL import Image


@dataclass(frozen=True)
class EncodeResult:
    path: Path
    encoder: str
    warnings: tuple[str, ...]


def _load_frames(
    frame_paths: Sequence[Path],
    original_alpha_paths: Sequence[Path],
    preserve_transparency: bool,
) -> list[Image.Image]:
    frames: list[Image.Image] = []
    if original_alpha_paths and len(original_alpha_paths) != len(frame_paths):
        raise ValueError("original alpha paths must match frame count")
    for index, path in enumerate(frame_paths):
        with Image.open(path) as image:
            frame = image.convert("RGBA").copy()
        if preserve_transparency and original_alpha_paths:
            with Image.open(original_alpha_paths[index]) as original:
                alpha = original.convert("RGBA").getchannel("A")
            if alpha.size != frame.size:
                raise ValueError("original alpha size must match generated frame")
            frame.putalpha(alpha)
        frames.append(frame)
    if not frames:
        raise ValueError("at least one frame is required")
    size = frames[0].size
    if any(frame.size != size for frame in frames):
        raise ValueError("all encoded frames must share one canvas size")
    return frames


def _shared_palette(frames: Sequence[Image.Image]) -> Image.Image:
    thumbnails = []
    for frame in frames:
        thumbnail = frame.convert("RGB").copy()
        thumbnail.thumbnail((256, 256), Image.Resampling.LANCZOS)
        thumbnails.append(thumbnail)
    atlas_width = max(image.width for image in thumbnails)
    atlas_height = sum(image.height for image in thumbnails)
    atlas = Image.new("RGB", (atlas_width, atlas_height), (0, 0, 0))
    top = 0
    for image in thumbnails:
        atlas.paste(image, (0, top))
        top += image.height
    return atlas.quantize(colors=255, method=Image.Quantize.MEDIANCUT)


def _encode_pillow(
    frames: Sequence[Image.Image],
    durations_ms: Sequence[int],
    loop: int,
    output_path: Path,
    preserve_transparency: bool,
) -> EncodeResult:
    palette = _shared_palette(frames)
    encoded: list[Image.Image] = []
    has_transparency = False
    for frame in frames:
        quantized = frame.convert("RGB").quantize(palette=palette, dither=Image.Dither.FLOYDSTEINBERG)
        if preserve_transparency:
            alpha = frame.getchannel("A")
            transparent_mask = alpha.point(lambda value: 255 if value < 128 else 0)
            if transparent_mask.getbbox():
                has_transparency = True
                quantized.paste(255, mask=transparent_mask)
                quantized.info["transparency"] = 255
        encoded.append(quantized)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    save_args = {
        "format": "GIF",
        "save_all": True,
        "append_images": encoded[1:],
        "duration": list(durations_ms),
        "loop": loop,
        "disposal": 2,
        "optimize": False,
    }
    if has_transparency:
        save_args["transparency"] = 255
    encoded[0].save(output_path, **save_args)
    _verify_output(output_path, len(frames), durations_ms, loop, frames[0].size)
    return EncodeResult(output_path, "pillow", ())


def _ffmpeg_escape(path: Path) -> str:
    return str(path.resolve()).replace("'", "'\\''")


def _encode_ffmpeg(
    ffmpeg: str,
    frames: Sequence[Image.Image],
    durations_ms: Sequence[int],
    loop: int,
    output_path: Path,
    temp_root: Path,
) -> EncodeResult:
    temp_root.mkdir(parents=True, exist_ok=True)
    with tempfile.TemporaryDirectory(prefix="gif_encode_", dir=temp_root) as temporary:
        work = Path(temporary)
        png_paths = []
        for index, frame in enumerate(frames):
            path = work / f"frame_{index:04d}.png"
            frame.save(path, format="PNG")
            png_paths.append(path)
        concat_path = work / "timeline.txt"
        lines = []
        for path, duration in zip(png_paths, durations_ms):
            lines.append(f"file '{_ffmpeg_escape(path)}'")
            lines.append(f"duration {duration / 1000:.6f}")
        lines.append(f"file '{_ffmpeg_escape(png_paths[-1])}'")
        concat_path.write_text("\n".join(lines) + "\n", encoding="utf-8")
        palette_path = work / "palette.png"
        subprocess.run(
            [
                ffmpeg,
                "-y",
                "-f",
                "concat",
                "-safe",
                "0",
                "-i",
                str(concat_path),
                "-vf",
                "palettegen",
                str(palette_path),
            ],
            check=True,
            capture_output=True,
        )
        output_path.parent.mkdir(parents=True, exist_ok=True)
        subprocess.run(
            [
                ffmpeg,
                "-y",
                "-f",
                "concat",
                "-safe",
                "0",
                "-i",
                str(concat_path),
                "-i",
                str(palette_path),
                "-lavfi",
                "paletteuse=dither=sierra2_4a",
                "-loop",
                str(loop),
                str(output_path),
            ],
            check=True,
            capture_output=True,
        )
    _verify_output(output_path, len(frames), durations_ms, loop, frames[0].size)
    return EncodeResult(output_path, "ffmpeg", ())


def _verify_output(
    output_path: Path,
    frame_count: int,
    durations_ms: Sequence[int],
    loop: int,
    size: tuple[int, int],
) -> None:
    with Image.open(output_path) as gif:
        if gif.format != "GIF":
            raise ValueError("encoder output is not a GIF")
        if int(getattr(gif, "n_frames", 1)) != frame_count:
            raise ValueError("encoded GIF frame count does not match")
        if gif.size != size:
            raise ValueError("encoded GIF canvas size does not match")
        if int(gif.info.get("loop", 0) or 0) != loop:
            raise ValueError("encoded GIF loop does not match")
        decoded_durations = []
        for index in range(frame_count):
            gif.seek(index)
            decoded_durations.append(int(gif.info.get("duration", 0) or 0))
        # GIF stores centiseconds; compare against the representable timeline.
        expected = [int(round(duration / 10.0) * 10) for duration in durations_ms]
        if decoded_durations != expected:
            raise ValueError(
                f"encoded GIF durations do not match: {decoded_durations} != {expected}"
            )


def encode_gif(
    frame_paths: Sequence[Path],
    original_alpha_paths: Sequence[Path],
    durations_ms: Sequence[int],
    loop: int,
    output_path: Path,
    encoder: str,
    preserve_transparency: bool,
) -> EncodeResult:
    if len(frame_paths) != len(durations_ms):
        raise ValueError("frame paths and durations must have equal length")
    frames = _load_frames(frame_paths, original_alpha_paths, preserve_transparency)
    selected = encoder.lower()
    if selected not in ("auto", "ffmpeg", "pillow"):
        raise ValueError(f"unsupported encoder: {encoder}")
    ffmpeg = shutil.which("ffmpeg")
    if selected == "auto":
        selected = "ffmpeg" if ffmpeg else "pillow"
    if selected == "ffmpeg":
        if not ffmpeg:
            raise RuntimeError("ffmpeg encoder requested but ffmpeg is unavailable")
        return _encode_ffmpeg(
            ffmpeg,
            frames,
            durations_ms,
            loop,
            output_path,
            output_path.parent / ".encode_tmp",
        )
    warnings = ("ffmpeg_unavailable_using_pillow",) if encoder.lower() == "auto" and not ffmpeg else ()
    result = _encode_pillow(frames, durations_ms, loop, output_path, preserve_transparency)
    return EncodeResult(result.path, result.encoder, warnings)
