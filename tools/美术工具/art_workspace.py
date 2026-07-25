# -*- coding: utf-8 -*-
"""Resolve Project P3 art production workspaces from Manifest entries."""

from __future__ import annotations

from copy import deepcopy
from pathlib import Path
from typing import Any, Mapping


STANDARD_PROFILE = "standard_asset"
CHARACTER_PORTRAIT_PROFILE = "character_portrait_set"
PROFILE_DIRECTORIES = {
    STANDARD_PROFILE: "standard_assets",
    CHARACTER_PORTRAIT_PROFILE: "character_portraits",
}
INCOMING_ROOT_REPO = "UnityClient/Assets/Art/_IncomingAI"
WORKSPACE_PATH_FIELDS = ("RawPath", "SelectedPath", "CandidateRawPath")


def production_profile(entry: Mapping[str, Any]) -> str:
    value = str(entry.get("ProductionProfile", "") or "").strip()
    profile = value or STANDARD_PROFILE
    if profile not in PROFILE_DIRECTORIES:
        raise ValueError(f"Unsupported ProductionProfile: {profile}")
    return profile


def visual_id(entry: Mapping[str, Any]) -> str:
    value = str(entry.get("VisualID", "") or "").strip()
    if not value:
        raise ValueError("Manifest entry is missing VisualID.")
    path = Path(value)
    if (
        value in {".", ".."}
        or "/" in value
        or "\\" in value
        or "\x00" in value
        or path.is_absolute()
        or bool(path.drive)
        or len(path.parts) != 1
    ):
        raise ValueError(f"VisualID must be a single path segment: {value}")
    return value


def workspace_path(incoming_root: Path, entry: Mapping[str, Any]) -> Path:
    profile = production_profile(entry)
    return incoming_root / PROFILE_DIRECTORIES[profile] / visual_id(entry)


def validate_workspace_file(path: Path, allowed_directory: Path) -> Path:
    candidate = path.resolve(strict=False)
    allowed = allowed_directory.resolve(strict=False)
    try:
        candidate.relative_to(allowed)
    except ValueError as exc:
        raise ValueError(
            f"Workspace file is outside resolved workspace: {candidate} not under {allowed}"
        ) from exc
    return candidate


def _rewrite_workspace_value(value: Any, visual: str, profile: str) -> Any:
    if not isinstance(value, str) or not value:
        return value

    normalized = value.replace("\\", "/")
    target_prefix = f"{INCOMING_ROOT_REPO}/{PROFILE_DIRECTORIES[profile]}/{visual}"
    source_prefixes = (
        f"{INCOMING_ROOT_REPO}/{visual}",
        f"{INCOMING_ROOT_REPO}/standard_assets/{visual}",
        f"{INCOMING_ROOT_REPO}/character_portraits/{visual}",
    )
    for source_prefix in source_prefixes:
        if normalized == source_prefix:
            return target_prefix
        if normalized.startswith(f"{source_prefix}/"):
            return f"{target_prefix}{normalized[len(source_prefix):]}"
    return value


def normalize_entry_workspace_paths(entry: Mapping[str, Any]) -> dict[str, Any]:
    normalized = deepcopy(dict(entry))
    profile = production_profile(normalized)
    visual = visual_id(normalized)
    normalized["ProductionProfile"] = profile

    for field in WORKSPACE_PATH_FIELDS:
        if field in normalized:
            normalized[field] = _rewrite_workspace_value(normalized[field], visual, profile)

    raw_files = normalized.get("CandidateRawFiles")
    if isinstance(raw_files, list):
        normalized["CandidateRawFiles"] = [
            _rewrite_workspace_value(value, visual, profile) for value in raw_files
        ]
    return normalized
