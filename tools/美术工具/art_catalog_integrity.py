# -*- coding: utf-8 -*-
"""Shared integrity rules for the formal P3 art Manifest and Request Catalog."""

from __future__ import annotations

import copy
import json
from typing import Any


SUMMARY_KEYS = (
    "Ready",
    "StyleResolutionRequired",
    "Unsupported",
    "Invalid",
    "UnchangedPublished",
    "PromptAuthoringRequired",
    "PromptReady",
)

PUBLISHED_STATUSES = {"approved", "registered", "runtime_validated", "validated"}


class CatalogIntegrityError(ValueError):
    """A stable, fail-closed Catalog or Manifest integrity error."""


def primary_source_rank(entry: dict[str, Any]) -> tuple[int, str]:
    config_id = str(entry.get("ConfigID", ""))
    visual_id = str(entry.get("VisualID", ""))
    return (0 if f"item_{config_id}_icon" == visual_id else 1, config_id)


def requirement_source_rank(source: dict[str, Any], visual_id: str) -> tuple[int, str]:
    config_id = str(source.get("ConfigID", ""))
    return (0 if f"item_{config_id}_icon" == visual_id else 1, config_id)


def _structural_contract(entry: dict[str, Any]) -> str:
    return json.dumps(
        {
            "Domain": entry.get("Domain"),
            "AssetType": entry.get("AssetType"),
            "ProductionProfile": entry.get("ProductionProfile"),
            "OutputPath": entry.get("OutputPath"),
            "Spec": entry.get("Spec", {}),
        },
        ensure_ascii=False,
        sort_keys=True,
        separators=(",", ":"),
    )


def canonicalize_manifest_entries(entries: list[dict[str, Any]]) -> list[dict[str, Any]]:
    """Merge only source-proven shared visuals and reject every other collision."""
    by_visual: dict[str, list[dict[str, Any]]] = {}
    for entry in entries:
        visual_id = str(entry.get("VisualID", ""))
        if not visual_id:
            raise CatalogIntegrityError("visual_id_missing")
        by_visual.setdefault(visual_id, []).append(copy.deepcopy(entry))

    result: list[dict[str, Any]] = []
    for visual_id, group in sorted(by_visual.items()):
        if len(group) == 1:
            result.append(group[0])
            continue

        sources = [source for entry in group for source in entry.get("RequirementSources", [])]
        if len(sources) != len(group) or not all(
            isinstance(source, dict) and source.get("ExplicitVisualID") is True
            for source in sources
        ):
            raise CatalogIntegrityError(f"visual_id_collision_conflict:{visual_id}")
        if len({_structural_contract(entry) for entry in group}) != 1:
            raise CatalogIntegrityError(f"visual_id_collision_contract_mismatch:{visual_id}")

        primary = min(group, key=primary_source_rank)
        primary["RequirementSources"] = sorted(
            sources,
            key=lambda source: requirement_source_rank(source, visual_id),
        )
        primary["VisualReusePolicy"] = {
            "Mode": "shared_visual",
            "DecisionSource": "config_explicit_visual_id",
            "Reason": "Multiple active config records explicitly reference the same visual field value.",
        }
        result.append(primary)
    return sorted(
        result,
        key=lambda item: (str(item.get("Domain", "")), str(item.get("VisualID", ""))),
    )


def validate_manifest_uniqueness(entries: list[dict[str, Any]]) -> None:
    visual_ids: set[str] = set()
    output_paths: set[str] = set()
    for entry in entries:
        visual_id = str(entry.get("VisualID", ""))
        if not visual_id:
            raise CatalogIntegrityError("visual_id_missing")
        if visual_id in visual_ids:
            raise CatalogIntegrityError(f"visual_id_duplicate:{visual_id}")
        visual_ids.add(visual_id)

        output_path = str(entry.get("OutputPath", "") or "")
        if output_path:
            if output_path in output_paths:
                raise CatalogIntegrityError(f"output_path_duplicate:{output_path}")
            output_paths.add(output_path)


def recompute_catalog_summary(requests: list[dict[str, Any]]) -> dict[str, int]:
    summary = {key: 0 for key in SUMMARY_KEYS}
    for request in requests:
        if not isinstance(request, dict):
            continue
        requirement_status = str(request.get("RequirementStatus", ""))
        if requirement_status == "ready":
            summary["Ready"] += 1
        elif requirement_status == "style_resolution_required":
            summary["StyleResolutionRequired"] += 1
        elif requirement_status == "unsupported":
            summary["Unsupported"] += 1
        else:
            summary["Invalid"] += 1

        prompt_status = str(request.get("PromptAuthoringStatus", ""))
        if prompt_status == "prompt_ready":
            summary["PromptReady"] += 1
        elif prompt_status == "prompt_authoring_required":
            summary["PromptAuthoringRequired"] += 1

        if str(request.get("PublicationStatus", "")) in PUBLISHED_STATUSES:
            summary["UnchangedPublished"] += 1
    return summary


def _unique_request_map(requests: list[dict[str, Any]], *, label: str) -> dict[str, dict[str, Any]]:
    by_visual: dict[str, dict[str, Any]] = {}
    request_ids: set[str] = set()
    for request in requests:
        if not isinstance(request, dict):
            raise CatalogIntegrityError(f"{label}_request_invalid")
        visual_id = str(request.get("VisualID", ""))
        request_id = str(request.get("RequestID", ""))
        if not visual_id:
            raise CatalogIntegrityError(f"{label}_visual_id_missing")
        if not request_id:
            raise CatalogIntegrityError(f"{label}_request_id_missing:{visual_id}")
        if visual_id in by_visual:
            raise CatalogIntegrityError(f"{label}_visual_id_duplicate:{visual_id}")
        if request_id in request_ids:
            raise CatalogIntegrityError(f"{label}_request_id_duplicate:{request_id}")
        by_visual[visual_id] = copy.deepcopy(request)
        request_ids.add(request_id)
    return by_visual


def merge_request_catalog(
    previous_requests: list[dict[str, Any]],
    compiled_requests: list[dict[str, Any]],
    selected_visual_ids: set[str],
) -> list[dict[str, Any]]:
    """Replace only compiled VisualIDs while retaining every unselected Request."""
    previous = _unique_request_map(previous_requests, label="previous_catalog")
    compiled = _unique_request_map(compiled_requests, label="compiled_catalog")
    if set(compiled) != set(selected_visual_ids):
        missing = sorted(selected_visual_ids - set(compiled))
        unexpected = sorted(set(compiled) - set(selected_visual_ids))
        if missing:
            raise CatalogIntegrityError(f"compiled_catalog_selected_missing:{','.join(missing)}")
        raise CatalogIntegrityError(f"compiled_catalog_selected_unexpected:{','.join(unexpected)}")

    merged = {
        visual_id: request
        for visual_id, request in previous.items()
        if visual_id not in selected_visual_ids
    }
    merged.update(compiled)
    result = [merged[visual_id] for visual_id in sorted(merged)]
    _unique_request_map(result, label="merged_catalog")
    return result
