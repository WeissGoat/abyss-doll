"""Technical validation and bounded visual-risk heuristics."""

from __future__ import annotations

from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Sequence

import numpy as np
from PIL import Image, ImageDraw, ImageOps, UnidentifiedImageError

from .models import RunConfig, TimelineMetadata
from .store import atomic_write_json


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


def _rgb_array(image: Image.Image) -> np.ndarray:
    return np.asarray(image.convert("RGB"), dtype=np.float32)


def _mean_delta(left: np.ndarray, right: np.ndarray) -> float:
    return float(np.abs(left - right).mean())


def _histogram_distance(left: np.ndarray, right: np.ndarray) -> float:
    distances = []
    for channel in range(3):
        left_hist, _ = np.histogram(left[:, channel], bins=16, range=(0, 256))
        right_hist, _ = np.histogram(right[:, channel], bins=16, range=(0, 256))
        left_hist = left_hist.astype(np.float64) / max(1, left_hist.sum())
        right_hist = right_hist.astype(np.float64) / max(1, right_hist.sum())
        distances.append(float(np.abs(left_hist - right_hist).sum() / 2.0))
    return float(np.mean(distances))


def _foreground_area(array: np.ndarray, edge_mask: np.ndarray, threshold: float) -> int:
    background_color = np.median(array[edge_mask], axis=0)
    distance = np.abs(array - background_color).mean(axis=2)
    return int((distance > threshold).sum())


def _edge_mask(width: int, height: int, fraction: float) -> np.ndarray:
    x_band = max(1, int(round(width * fraction)))
    y_band = max(1, int(round(height * fraction)))
    mask = np.zeros((height, width), dtype=bool)
    mask[:y_band, :] = True
    mask[-y_band:, :] = True
    mask[:, :x_band] = True
    mask[:, -x_band:] = True
    return mask


def _heatmap(delta: np.ndarray, destination: Path) -> None:
    normalized = np.clip(delta / max(1.0, float(delta.max())) * 255.0, 0, 255).astype(
        np.uint8
    )
    rgba = np.zeros((*normalized.shape, 4), dtype=np.uint8)
    rgba[:, :, 0] = normalized
    rgba[:, :, 1] = normalized // 5
    rgba[:, :, 3] = 255
    destination.parent.mkdir(parents=True, exist_ok=True)
    Image.fromarray(rgba, mode="RGBA").save(destination, format="PNG")


def _contact_sheet(
    source_images: Sequence[Image.Image],
    output_images: Sequence[Image.Image | None],
    risks: Sequence[FrameRisk],
    destination: Path,
) -> None:
    thumb_width = 220
    thumb_height = 160
    label_height = 34
    row_height = thumb_height + label_height
    sheet = Image.new("RGB", (thumb_width * 3, row_height * len(risks)), (40, 40, 40))
    draw = ImageDraw.Draw(sheet)
    for row, risk in enumerate(risks):
        top = row * row_height
        original = ImageOps.contain(source_images[row].convert("RGB"), (thumb_width, thumb_height))
        generated_source = output_images[row]
        generated = (
            ImageOps.contain(generated_source.convert("RGB"), (thumb_width, thumb_height))
            if generated_source is not None
            else Image.new("RGB", (thumb_width, thumb_height), (90, 20, 20))
        )
        heatmap = (
            Image.open(risk.heatmap_path).convert("RGB")
            if risk.heatmap_path
            else Image.new("RGB", (thumb_width, thumb_height), (20, 20, 20))
        )
        heatmap = ImageOps.contain(heatmap, (thumb_width, thumb_height))
        for column, image in enumerate((original, generated, heatmap)):
            left = column * thumb_width
            sheet.paste(
                image,
                (left + (thumb_width - image.width) // 2, top + (thumb_height - image.height) // 2),
            )
        label = f"frame {risk.frame_index:04d}: {', '.join(risk.codes) or 'no risk'}"
        draw.rectangle((0, top + thumb_height, sheet.width, top + row_height), fill=(60, 60, 60))
        draw.text((8, top + thumb_height + 9), label, fill=(255, 255, 255))
    destination.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(destination, format="PNG")


def review_sequence(
    config: RunConfig,
    timeline: TimelineMetadata,
    source_paths: Sequence[Path],
    output_paths: Sequence[Path],
    reports_dir: Path,
    output_dir: Path,
) -> SequenceReview:
    """Validate output frames and label heuristic risks without claiming art approval."""

    if len(source_paths) != timeline.frame_count or len(output_paths) != timeline.frame_count:
        raise ValueError("review path counts must match the timeline")
    source_images: list[Image.Image] = []
    source_arrays: list[np.ndarray] = []
    for path in source_paths:
        with Image.open(path) as image:
            copied = image.convert("RGBA").copy()
        source_images.append(copied)
        source_arrays.append(_rgb_array(copied))

    source_stack = np.stack(source_arrays)
    stable_mask = source_stack.std(axis=0).mean(axis=2) <= config.stable_pixel_stddev
    edge_mask = _edge_mask(timeline.width, timeline.height, config.edge_fraction)
    stable_edge = stable_mask & edge_mask

    output_images: list[Image.Image | None] = []
    output_arrays: list[np.ndarray | None] = []
    frame_data: list[dict] = []
    heatmaps_dir = reports_dir / "heatmaps"

    for index, (source_array, output_path) in enumerate(zip(source_arrays, output_paths)):
        codes: list[str] = []
        metrics: dict[str, float] = {}
        hard_failure = False
        output_image: Image.Image | None = None
        output_array: np.ndarray | None = None
        if not output_path.is_file():
            codes.append("missing_output")
            hard_failure = True
        else:
            try:
                with Image.open(output_path) as image:
                    image.load()
                    output_image = image.convert("RGBA").copy()
            except (OSError, UnidentifiedImageError):
                codes.append("corrupt_output")
                hard_failure = True
            if output_image is not None and output_image.size != (timeline.width, timeline.height):
                codes.append("wrong_size")
                hard_failure = True
            if output_image is not None and output_image.size == (timeline.width, timeline.height):
                output_array = _rgb_array(output_image)
                stddev = float(output_array.std())
                metrics["image_stddev"] = stddev
                if stddev < config.min_image_stddev:
                    codes.append("solid_color_output")
                    hard_failure = True

        heatmap_path: Path | None = None
        if output_array is not None and output_array.shape == source_array.shape:
            delta = np.abs(source_array - output_array).mean(axis=2)
            changed = delta > config.pixel_diff_threshold
            heatmap_path = heatmaps_dir / f"frame_{index:04d}.png"
            _heatmap(delta, heatmap_path)

            background_ratio = float((changed & stable_edge).sum() / max(1, stable_edge.sum()))
            metrics["background_changed_ratio"] = background_ratio
            if background_ratio > config.background_changed_ratio:
                codes.append("background_drift")

            changed_ratio = float(changed.mean())
            metrics["changed_ratio"] = changed_ratio
            ys, xs = np.where(changed)
            touching_edges = 0
            if len(xs):
                touching_edges = sum(
                    (
                        int(xs.min() == 0),
                        int(xs.max() == timeline.width - 1),
                        int(ys.min() == 0),
                        int(ys.max() == timeline.height - 1),
                    )
                )
            metrics["changed_box_touching_edges"] = float(touching_edges)
            reliable = 0.02 <= changed_ratio <= 0.65 and touching_edges <= 2
            if not reliable:
                codes.append("identity_check_limited")
            else:
                source_pixels = source_array[changed]
                output_pixels = output_array[changed]
                histogram_distance = _histogram_distance(source_pixels, output_pixels)
                source_area = _foreground_area(
                    source_array, edge_mask, config.pixel_diff_threshold
                )
                output_area = _foreground_area(
                    output_array, edge_mask, config.pixel_diff_threshold
                )
                area_change = float(abs(output_area - source_area) / max(1, source_area))
                metrics["identity_histogram_distance"] = histogram_distance
                metrics["identity_area_change_ratio"] = area_change
                if (
                    histogram_distance > config.identity_histogram_distance
                    or area_change > config.identity_area_change_ratio
                ):
                    codes.append("identity_drift")

        output_images.append(output_image)
        output_arrays.append(output_array)
        frame_data.append(
            {
                "index": index,
                "codes": codes,
                "hard_failure": hard_failure,
                "metrics": metrics,
                "heatmap_path": str(heatmap_path.resolve()) if heatmap_path else None,
            }
        )

    sequence_codes: list[str] = []
    for index in range(1, timeline.frame_count):
        left = output_arrays[index - 1]
        right = output_arrays[index]
        if left is None or right is None:
            continue
        source_motion = _mean_delta(source_arrays[index - 1], source_arrays[index])
        output_motion = _mean_delta(left, right)
        if (
            output_motion >= max(0.001, source_motion) * config.flicker_ratio
            and output_motion >= config.flicker_absolute_delta
        ):
            if "temporal_flicker" not in sequence_codes:
                sequence_codes.append("temporal_flicker")
            frame_data[index]["codes"].append("temporal_flicker")
            frame_data[index]["metrics"]["source_motion"] = source_motion
            frame_data[index]["metrics"]["output_motion"] = output_motion

    first_output = output_arrays[0]
    last_output = output_arrays[-1]
    if first_output is not None and last_output is not None:
        source_seam = _mean_delta(source_arrays[-1], source_arrays[0])
        output_seam = _mean_delta(last_output, first_output)
        if output_seam >= max(0.001, source_seam) * config.loop_seam_ratio:
            sequence_codes.append("loop_seam")

    if timeline.has_transparency:
        sequence_codes.append("transparent_silhouette_limited")

    risks = tuple(
        FrameRisk(
            frame_index=item["index"],
            codes=tuple(dict.fromkeys(item["codes"])),
            hard_failure=bool(item["hard_failure"]),
            metrics=item["metrics"],
            heatmap_path=item["heatmap_path"],
        )
        for item in frame_data
    )
    contact_sheet_path = output_dir / "preview_contact_sheet.png"
    _contact_sheet(source_images, output_images, risks, contact_sheet_path)
    hard_failure = any(risk.hard_failure for risk in risks)
    report_path = reports_dir / "frame_review.json"
    atomic_write_json(
        report_path,
        {
            "frame_risks": [asdict(risk) for risk in risks],
            "sequence_codes": sequence_codes,
            "hard_failure": hard_failure,
            "contact_sheet_path": str(contact_sheet_path.resolve()),
        },
    )
    return SequenceReview(
        frame_risks=risks,
        sequence_codes=tuple(dict.fromkeys(sequence_codes)),
        hard_failure=hard_failure,
        contact_sheet_path=str(contact_sheet_path.resolve()),
    )
