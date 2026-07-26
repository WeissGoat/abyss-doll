# -*- coding: utf-8 -*-
"""Resolve character portrait SourceAssets into immutable image evidence."""

from __future__ import annotations

import hashlib
from pathlib import Path
from typing import Any, Mapping

from PIL import Image, UnidentifiedImageError

from art_workspace import INCOMING_ROOT_REPO, workspace_path


SUPPORTED_IMAGE_SUFFIXES = {".png", ".jpg", ".jpeg", ".webp"}


def _source_value(source: Mapping[str, Any], snake: str, pascal: str) -> str:
    return str(source.get(snake, source.get(pascal, "")) or "").strip()


def _project_path(project_root: Path, value: str, *, asset_id: str) -> Path:
    raw = Path(value)
    candidate = (raw if raw.is_absolute() else project_root / raw).resolve(strict=False)
    root = project_root.resolve(strict=False)
    try:
        relative = candidate.relative_to(root)
    except ValueError as exc:
        raise ValueError(f"portrait_reference_outside_project:{asset_id}:{candidate}") from exc
    if "_legacy_runs" in relative.parts:
        raise ValueError(f"portrait_reference_legacy_path_forbidden:{asset_id}:{candidate}")
    return candidate


def _read_image_evidence(path: Path, *, asset_id: str, project_root: Path, state: str) -> dict[str, Any]:
    if not path.is_file():
        raise ValueError(f"portrait_reference_file_missing:{asset_id}:{path}")
    try:
        with Image.open(path) as image:
            image.load()
            width, height = image.size
            mode = image.mode
    except (OSError, UnidentifiedImageError, ValueError) as exc:
        raise ValueError(f"portrait_reference_image_invalid:{asset_id}:{path}") from exc

    relative = path.resolve().relative_to(project_root.resolve())
    return {
        "Path": relative.as_posix(),
        "State": state,
        "SHA256": hashlib.sha256(path.read_bytes()).hexdigest(),
        "Width": width,
        "Height": height,
        "Mode": mode,
    }


def _matching_source_entries(
    manifest: Mapping[str, Any], target_entry: Mapping[str, Any], asset_id: str
) -> list[Mapping[str, Any]]:
    asset_set_id = str(target_entry.get("AssetSetID", "") or "")
    return [
        entry
        for entry in manifest.get("Entries", [])
        if isinstance(entry, Mapping)
        and str(entry.get("AssetID", "") or "") == asset_id
        and str(entry.get("AssetSetID", "") or "") == asset_set_id
    ]


def _resolve_source_path(
    source_entry: Mapping[str, Any], *, asset_id: str, project_root: Path
) -> tuple[Path, str]:
    status = str(source_entry.get("Status", "") or "")
    if status == "approved":
        for field in ("ApprovedPath", "OutputPath"):
            value = str(source_entry.get(field, "") or "").strip()
            if not value:
                continue
            candidate = _project_path(project_root, value, asset_id=asset_id)
            if candidate.is_file():
                return candidate, "approved"

    selected_value = str(source_entry.get("SelectedPath", "") or "").strip()
    if selected_value:
        selected = _project_path(project_root, selected_value, asset_id=asset_id)
        if not selected.is_file():
            raise ValueError(f"portrait_reference_file_missing:{asset_id}:{selected}")
        return selected, "selected"

    incoming_root = project_root / INCOMING_ROOT_REPO
    selected_dir = workspace_path(incoming_root, source_entry) / "selected"
    candidates = []
    if selected_dir.is_dir():
        candidates = sorted(
            path
            for path in selected_dir.iterdir()
            if path.is_file() and path.suffix.lower() in SUPPORTED_IMAGE_SUFFIXES
        )
    if len(candidates) > 1:
        raise ValueError(f"portrait_reference_selected_ambiguous:{asset_id}:{selected_dir}")
    if len(candidates) == 1:
        return candidates[0].resolve(), "selected"
    raise ValueError(f"portrait_reference_file_missing:{asset_id}:{selected_dir}")


def resolve_portrait_references(
    manifest: Mapping[str, Any], target_entry: Mapping[str, Any], project_root: Path
) -> list[dict[str, Any]]:
    """Resolve ordered logical SourceAssets without mutating project state."""

    sources = target_entry.get("SourceAssets", [])
    if not isinstance(sources, list):
        raise ValueError("portrait_reference_sources_invalid")
    if not sources:
        return []

    resolved: list[dict[str, Any]] = []
    for source in sources:
        if not isinstance(source, Mapping):
            raise ValueError("portrait_reference_source_invalid")
        asset_id = _source_value(source, "asset_id", "AssetID")
        role = _source_value(source, "role", "Role")
        if not asset_id:
            raise ValueError("portrait_reference_asset_id_missing")
        matches = _matching_source_entries(manifest, target_entry, asset_id)
        if not matches:
            raise ValueError(f"portrait_reference_asset_missing:{asset_id}")
        if len(matches) > 1:
            raise ValueError(f"portrait_reference_asset_duplicate:{asset_id}")

        source_entry = matches[0]
        path, state = _resolve_source_path(source_entry, asset_id=asset_id, project_root=project_root)
        evidence = _read_image_evidence(path, asset_id=asset_id, project_root=project_root, state=state)
        resolved.append(
            {
                "AssetID": asset_id,
                "VisualID": str(source_entry.get("VisualID", "") or ""),
                "Role": role,
                **evidence,
            }
        )
    return resolved
