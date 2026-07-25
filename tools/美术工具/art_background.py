# -*- coding: utf-8 -*-
"""Explicit background processing and deterministic candidate review."""

from __future__ import annotations

import hashlib
import io
import math
from collections import deque
from dataclasses import dataclass
from pathlib import Path
from typing import Any, Mapping

from PIL import Image


BACKGROUND_POLICIES = {
    "preserve",
    "already_transparent",
    "auto_simple",
    "agent_required",
}


@dataclass(frozen=True)
class BackgroundProcessResult:
    state: str
    image: Image.Image | None
    policy: str
    reasons: list[str]
    metrics: dict[str, Any]


def background_policy(spec: Mapping[str, Any]) -> str:
    process_spec = spec.get("ProcessSpec")
    if not isinstance(process_spec, Mapping):
        raise ValueError("Spec is missing ProcessSpec.BackgroundPolicy.")
    policy = process_spec.get("BackgroundPolicy")
    if policy not in BACKGROUND_POLICIES:
        if policy is None or str(policy).strip() == "":
            raise ValueError("Spec is missing ProcessSpec.BackgroundPolicy.")
        raise ValueError(f"Unsupported BackgroundPolicy: {policy}")
    return str(policy)


def _sample_border_color(image: Image.Image) -> tuple[int, int, int]:
    rgb = image.convert("RGB")
    width, height = rgb.size
    points = [(0, 0), (width - 1, 0), (0, height - 1), (width - 1, height - 1)]
    colors = [rgb.getpixel(point) for point in points]
    return tuple(sorted(channel)[len(channel) // 2] for channel in zip(*colors))


def _rgb_distance(left: tuple[int, int, int], right: tuple[int, int, int]) -> float:
    return math.sqrt(sum((left[index] - right[index]) ** 2 for index in range(3)))


def remove_connected_background(image: Image.Image, threshold: int) -> Image.Image:
    """Remove only background-colored pixels connected to the image border."""

    if threshold < 0:
        raise ValueError("Background threshold must be non-negative.")
    rgba = image.convert("RGBA")
    width, height = rgba.size
    if width == 0 or height == 0:
        return rgba

    background = _sample_border_color(rgba)
    pixels = rgba.load()
    visited: set[tuple[int, int]] = set()
    queue: deque[tuple[int, int]] = deque()

    def enqueue_if_background(x: int, y: int) -> None:
        if (x, y) in visited:
            return
        red, green, blue, _ = pixels[x, y]
        if _rgb_distance((red, green, blue), background) <= threshold:
            visited.add((x, y))
            queue.append((x, y))

    for x in range(width):
        enqueue_if_background(x, 0)
        enqueue_if_background(x, height - 1)
    for y in range(height):
        enqueue_if_background(0, y)
        enqueue_if_background(width - 1, y)

    while queue:
        x, y = queue.popleft()
        for next_x, next_y in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= next_x < width and 0 <= next_y < height:
                enqueue_if_background(next_x, next_y)

    output = rgba.copy()
    output_pixels = output.load()
    for x, y in visited:
        red, green, blue, _ = output_pixels[x, y]
        output_pixels[x, y] = (red, green, blue, 0)

    feather = max(8, threshold // 2)
    boundary_neighbors: set[tuple[int, int]] = set()
    for x, y in visited:
        for next_x, next_y in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= next_x < width and 0 <= next_y < height and (next_x, next_y) not in visited:
                boundary_neighbors.add((next_x, next_y))
    for x, y in boundary_neighbors:
        red, green, blue, alpha = output_pixels[x, y]
        distance = _rgb_distance((red, green, blue), background)
        if distance <= threshold + feather:
            factor = max(0.0, min(1.0, (distance - threshold) / feather))
            output_pixels[x, y] = (red, green, blue, int(alpha * factor))
    return output


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
            return BackgroundProcessResult("failed", None, policy, ["expected_existing_alpha"], {})
        return BackgroundProcessResult("passed", rgba, policy, [], {})
    if policy == "agent_required":
        return BackgroundProcessResult(
            "decision_required",
            None,
            policy,
            ["agent_processing_required"],
            {},
        )
    if policy == "auto_simple":
        return BackgroundProcessResult(
            "passed",
            remove_connected_background(image, threshold),
            policy,
            [],
            {},
        )
    raise ValueError(f"Unsupported BackgroundPolicy: {policy}")


def _encoded_sha256(image: Image.Image) -> str:
    buffer = io.BytesIO()
    image.save(buffer, format="PNG")
    return hashlib.sha256(buffer.getvalue()).hexdigest()


def _component_count(alpha: Image.Image) -> int:
    width, height = alpha.size
    pixels = alpha.load()
    remaining = {(x, y) for y in range(height) for x in range(width) if pixels[x, y] > 0}
    count = 0
    while remaining:
        count += 1
        queue = deque([remaining.pop()])
        while queue:
            x, y = queue.popleft()
            for neighbor in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
                if neighbor in remaining:
                    remaining.remove(neighbor)
                    queue.append(neighbor)
    return count


def _transparent_hole_ratio(alpha: Image.Image, bbox: tuple[int, int, int, int] | None) -> float:
    if bbox is None:
        return 0.0
    cropped = alpha.crop(bbox)
    width, height = cropped.size
    pixels = cropped.load()
    exterior: set[tuple[int, int]] = set()
    queue: deque[tuple[int, int]] = deque()

    def enqueue_if_transparent(x: int, y: int) -> None:
        point = (x, y)
        if point not in exterior and pixels[x, y] == 0:
            exterior.add(point)
            queue.append(point)

    for x in range(width):
        enqueue_if_transparent(x, 0)
        enqueue_if_transparent(x, height - 1)
    for y in range(height):
        enqueue_if_transparent(0, y)
        enqueue_if_transparent(width - 1, y)
    while queue:
        x, y = queue.popleft()
        for next_x, next_y in ((x - 1, y), (x + 1, y), (x, y - 1), (x, y + 1)):
            if 0 <= next_x < width and 0 <= next_y < height:
                enqueue_if_transparent(next_x, next_y)

    transparent = sum(1 for y in range(height) for x in range(width) if pixels[x, y] == 0)
    holes = transparent - len(exterior)
    return holes / max(1, width * height)


def measure_candidate(image: Image.Image, saved_path: Path | None = None) -> dict[str, Any]:
    rgba = image.convert("RGBA")
    alpha = rgba.getchannel("A")
    histogram = alpha.histogram()
    total = max(1, rgba.width * rgba.height)
    transparent = histogram[0]
    opaque = histogram[255]
    partial = total - transparent - opaque
    bbox = alpha.getbbox()
    if bbox is None:
        margins = {"Top": rgba.height, "Bottom": rgba.height, "Left": rgba.width, "Right": rgba.width}
        occupied_transparency = 1.0
    else:
        left, top, right, bottom = bbox
        margins = {
            "Top": top,
            "Bottom": rgba.height - bottom,
            "Left": left,
            "Right": rgba.width - right,
        }
        occupied_alpha = alpha.crop(bbox)
        occupied_histogram = occupied_alpha.histogram()
        occupied_transparency = occupied_histogram[0] / max(1, occupied_alpha.width * occupied_alpha.height)

    digest = hashlib.sha256(saved_path.read_bytes()).hexdigest() if saved_path is not None else _encoded_sha256(rgba)
    return {
        "Width": rgba.width,
        "Height": rgba.height,
        "Mode": image.mode,
        "AlphaExtrema": list(alpha.getextrema()),
        "AlphaBBox": list(bbox) if bbox is not None else None,
        "TransparentRatio": transparent / total,
        "OpaqueRatio": opaque / total,
        "PartialRatio": partial / total,
        "OccupiedBBoxTransparency": occupied_transparency,
        "TransparentHoleRatio": _transparent_hole_ratio(alpha, bbox),
        "Margins": margins,
        "ConnectedComponents": _component_count(alpha),
        "SHA256": digest,
        "Format": "png",
    }


def _edge_band_coverage(alpha: Image.Image, border: Mapping[str, Any]) -> dict[str, float]:
    width, height = alpha.size
    pixels = alpha.load()
    left = max(1, min(width, int(border.get("Left", 1) or 1)))
    right = max(1, min(width, int(border.get("Right", 1) or 1)))
    top = max(1, min(height, int(border.get("Top", 1) or 1)))
    bottom = max(1, min(height, int(border.get("Bottom", 1) or 1)))

    top_columns = sum(1 for x in range(width) if any(pixels[x, y] > 0 for y in range(top)))
    bottom_columns = sum(
        1 for x in range(width) if any(pixels[x, y] > 0 for y in range(height - bottom, height))
    )
    left_rows = sum(1 for y in range(height) if any(pixels[x, y] > 0 for x in range(left)))
    right_rows = sum(
        1 for y in range(height) if any(pixels[x, y] > 0 for x in range(width - right, width))
    )
    return {
        "Top": top_columns / max(1, width),
        "Bottom": bottom_columns / max(1, width),
        "Left": left_rows / max(1, height),
        "Right": right_rows / max(1, height),
    }


def review_candidate(
    image: Image.Image,
    *,
    source_spec: Mapping[str, Any],
    composition_spec: Mapping[str, Any],
    process_spec: Mapping[str, Any] | None = None,
    asset_type: str = "",
    production_profile: str,
    saved_path: Path | None = None,
) -> dict[str, Any]:
    metrics = measure_candidate(image, saved_path)
    reasons: list[str] = []
    warnings: list[str] = []

    expected_width = int(source_spec.get("Width", image.width))
    expected_height = int(source_spec.get("Height", image.height))
    if image.size != (expected_width, expected_height):
        reasons.append("wrong_dimensions")

    alpha_extrema = tuple(metrics["AlphaExtrema"])
    if metrics["AlphaBBox"] is None:
        reasons.append("empty_alpha")
    if bool(source_spec.get("AlphaRequired", False)) and alpha_extrema == (255, 255):
        reasons.append("required_alpha_missing")

    bbox = metrics["AlphaBBox"]
    safe_padding = float(composition_spec.get("SafePaddingPercent", 0) or 0)
    if bbox is not None and safe_padding > 0:
        minimum_x = math.floor(image.width * safe_padding / 100)
        minimum_y = math.floor(image.height * safe_padding / 100)
        left, top, right, bottom = bbox
        if left < minimum_x or top < minimum_y or right > image.width - minimum_x or bottom > image.height - minimum_y:
            reasons.append("subject_outside_safe_canvas")

    process_spec = process_spec or {}
    nine_slice = process_spec.get("NineSlice")
    nine_slice_metrics: dict[str, Any] | None = None
    if isinstance(nine_slice, Mapping) and bool(nine_slice.get("Enabled")):
        border = nine_slice.get("Border") if isinstance(nine_slice.get("Border"), Mapping) else {}
        max_components = int(nine_slice.get("MaxConnectedComponents", 24) or 24)
        minimum_edge_coverage = float(nine_slice.get("MinEdgeCoveragePercent", 35) or 35) / 100
        edge_coverage = _edge_band_coverage(image.convert("RGBA").getchannel("A"), border)
        nine_slice_metrics = {
            "AssetType": asset_type,
            "Border": dict(border),
            "MaxConnectedComponents": max_components,
            "MinEdgeCoveragePercent": minimum_edge_coverage * 100,
            "EdgeCoverage": edge_coverage,
        }
        if int(metrics["ConnectedComponents"]) > max_components:
            reasons.append("nine_slice_many_components")
        if any(value < minimum_edge_coverage for value in edge_coverage.values()):
            reasons.append("nine_slice_edge_coverage_low")

    if production_profile == "character_portrait_set":
        if (
            float(metrics["OccupiedBBoxTransparency"]) > 0.45
            or float(metrics["TransparentHoleRatio"]) > 0.05
        ):
            reasons.append("transparent_holes_detected")
    elif nine_slice_metrics is None and int(metrics["ConnectedComponents"]) > 12:
        warnings.append("many_connected_components")

    status = "failed" if reasons else "warning" if warnings else "passed"
    result = {
        "Status": status,
        "Reasons": reasons,
        "Warnings": warnings,
        "Metrics": metrics,
    }
    if nine_slice_metrics is not None:
        result["NineSliceMetrics"] = nine_slice_metrics
    return result
