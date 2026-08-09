# -*- coding: utf-8 -*-
"""Method-neutral defaults for incomplete P3 art requirements.

This module intentionally produces requirement facts only. Final provider text is
authored in a PromptRevision and must never be reconstructed here.
"""

from __future__ import annotations

import copy
from typing import Any


def _source_spec(
    width: int,
    height: int,
    background: str,
    alpha_required: bool,
) -> dict[str, Any]:
    return {
        "Format": "png",
        "Width": width,
        "Height": height,
        "Background": background,
        "AlphaRequired": alpha_required,
    }


def default_spec_for(entry: dict[str, Any]) -> dict[str, Any]:
    """Return a conservative production contract when a source has none."""
    domain = str(entry.get("Domain", ""))
    asset_type = str(entry.get("AssetType", "asset"))
    visual_id = str(entry.get("VisualID", ""))

    width, height, background, alpha_required = 512, 512, "transparent", True
    display_width, display_height, fit_mode, pivot = 64, 64, "contain", "center"
    safe_padding, subject_min, subject_max, anchor = 10, 0.70, 0.86, "center"
    post_process = ["resize", "trim_transparent_edges", "fit_safe_padding"]
    background_policy = "auto_simple"

    if domain in {"background", "narrative_cg"}:
        width, height, background, alpha_required = 1920, 1080, "opaque_environment", False
        display_width, display_height, fit_mode = 1920, 1080, "cover"
        safe_padding, subject_min, subject_max = 0, 0.0, 1.0
        post_process, background_policy = ["crop_16_9", "resize"], "preserve"
    elif domain == "doll" or entry.get("ProductionProfile") == "character_portrait_set":
        width, height, background, alpha_required = 1024, 1536, "transparent", True
        display_width, display_height, pivot, anchor = 420, 720, "bottom_center", "bottom_center"
        safe_padding, subject_min, subject_max = 6, 0.86, 0.94
        post_process, background_policy = ["crop_portrait", "resize", "fit_safe_padding"], "agent_required"
    elif domain == "monster" and visual_id.endswith("_combat"):
        width, height = 1024, 1024
        display_width, display_height, pivot, anchor = 360, 420, "bottom_center", "bottom_center"
        safe_padding, subject_min, subject_max = 6, 0.74, 0.90
        post_process, background_policy = ["trim_transparent_edges", "resize", "fit_safe_padding"], "preserve"
    elif domain == "monster":
        width, height, background, alpha_required = 1024, 1024, "transparent_or_simple_dark", False
        display_width, display_height = 320, 320
        safe_padding, subject_min, subject_max = 8, 0.78, 0.92
        post_process, background_policy = ["crop_square", "resize"], "preserve"
    elif domain == "ui" and asset_type in {"panel", "button", "slot", "bar", "frame", "divider"}:
        width, height = (1024, 768) if asset_type in {"panel", "frame"} else (512, 160)
        display_width, display_height = (960, 640) if width == 1024 else (220, 64)
        fit_mode = "stretch"
        safe_padding, subject_min, subject_max = 5, 0.88, 0.98
        post_process, background_policy = ["resize", "preserve_transparency", "check_nine_slice_edges"], "already_transparent"

    composition = "centered single readable asset"
    if domain in {"background", "narrative_cg"}:
        composition = "wide environment with a protected central safe area"
    elif domain == "doll" or entry.get("ProductionProfile") == "character_portrait_set":
        composition = "full-body portrait with a bottom anchor and readable pose silhouette"
    elif domain == "ui":
        composition = "clean interface shape with no baked text"

    result: dict[str, Any] = {
        "SourceSpec": _source_spec(width, height, background, alpha_required),
        "DisplaySpec": {
            "ReferenceResolution": "1920x1080",
            "DisplayWidth": display_width,
            "DisplayHeight": display_height,
            "Unit": "ui_px",
            "FitMode": fit_mode,
            "Pivot": pivot,
        },
        "CompositionSpec": {
            "SafePaddingPercent": safe_padding,
            "SubjectOccupancyMin": subject_min,
            "SubjectOccupancyMax": subject_max,
            "Anchor": anchor,
            "Composition": composition,
        },
        "ProcessSpec": {
            "PostProcess": post_process,
            "PreviewSize": min(max(display_width, display_height), 320),
            "BackgroundPolicy": background_policy,
        },
    }
    if domain in {"background", "narrative_cg"}:
        result["CompositionSpec"]["SafeArea"] = "center_4_3"
    if pivot == "bottom_center":
        result["CompositionSpec"]["BaselinePercent"] = 94 if domain == "doll" else 92
    return result


def visual_intent_for(entry: dict[str, Any]) -> dict[str, Any]:
    """Derive a minimal, method-neutral brief from source facts and role data."""
    domain = str(entry.get("Domain", ""))
    asset_type = str(entry.get("AssetType", "asset"))
    visual_id = str(entry.get("VisualID", ""))
    subject = {
        "background": "environment background",
        "monster": "full-body creature illustration" if visual_id.endswith("_combat") else "creature portrait",
        "doll": "full-body character portrait",
        "narrative_cg": "cinematic narrative panel",
        "node": "map node icon",
        "ui": f"modular interface {asset_type} sprite",
        "item": "game item icon",
        "prosthetic": "prosthetic module icon",
        "chassis": "mechanical chassis asset",
    }.get(domain, f"single {asset_type} game asset")
    composition = {
        "background": "wide composition with readable negative space",
        "monster": "full figure and clear silhouette" if visual_id.endswith("_combat") else "front or three-quarter portrait with a clear silhouette",
        "doll": "full-body portrait with a readable pose silhouette",
        "narrative_cg": "one clear focal point inside the safe area",
        "node": "centered high-contrast symbol",
        "ui": "clean interface silhouette with no baked text",
    }.get(domain, "centered readable asset")
    source_facts = str(entry.get("SourceFactsCN", "")).strip()
    pose_spec = entry.get("PoseSpec") if isinstance(entry.get("PoseSpec"), dict) else {}
    required_changes: list[str] = []
    forbidden: list[str] = []
    if entry.get("ProductionProfile") == "character_portrait_set" or domain == "doll":
        if source_facts and str(entry.get("SetRole", "")) not in {"", "neutral_dialogue_master"}:
            required_changes.append(source_facts)
        for key, label in (
            ("PoseClass", "Pose class"),
            ("BodyAction", "Body action"),
            ("Gesture", "Arm and hand action"),
            ("WeightShift", "Weight shift"),
            ("HeadShoulder", "Head and shoulder relation"),
            ("SilhouetteChange", "Silhouette change"),
        ):
            value = str(pose_spec.get(key, "")).strip()
            if value:
                required_changes.append(f"{label}: {value}")
        for marker, value in (
            ("无血", "blood or gore"),
            ("不画血", "blood or gore"),
            ("无断肢", "dismemberment"),
            ("不露完整眼睛", "fully visible uncovered eyes"),
            ("核心仓隐藏", "exposed core chamber"),
            ("无怪物化", "monster transformation"),
            ("非色情视角", "sexualized camera angle"),
        ):
            if marker in source_facts:
                forbidden.append(value)
    return {
        "SubjectEN": [subject],
        "SubjectCN": [str(entry.get("DisplayName", "单项美术资产"))],
        "AppearanceEN": [source_facts] if source_facts else [],
        "AppearanceCN": [source_facts] if source_facts else [],
        "CompositionEN": [composition],
        "CompositionCN": [composition],
        "RequiredElements": [],
        "ForbiddenElements": forbidden,
        "RequiredChanges": required_changes,
        "PoseSpec": copy.deepcopy(pose_spec),
    }
