# -*- coding: utf-8 -*-
"""Validate persisted P3 art generation requests against a Manifest."""

from __future__ import annotations

from typing import Any

from art_catalog_integrity import (
    manifest_uniqueness_errors,
    recompute_catalog_summary,
)
from art_prompt_compiler import compile_requirement_request, requirement_request_body
from art_prompt_revision import validate_prompt_revision
from art_style_catalog import sha256_json
from compile_art_generation_requests import _input_manifest


VALID_REQUIREMENT_STATES = {"ready", "style_resolution_required", "invalid", "stale"}
VALID_AUTHORING_STATES = {
    "prompt_authoring_required",
    "prompt_ready",
    "prompt_stale",
    "prompt_invalid",
    "not_required",
}


def _append_revision_errors(errors: list[str], revision_errors: list[str], visual_id: str) -> None:
    errors.extend(f"{error}:{visual_id}" for error in revision_errors)


def validate_request_catalog(
    catalog: dict[str, Any],
    manifest: dict[str, Any],
    *,
    strict: bool = False,
) -> list[str]:
    errors: list[str] = []
    if not isinstance(catalog, dict):
        return ["compiled_request_catalog_invalid"]
    requests = catalog.get("Requests")
    if not isinstance(requests, list):
        return ["compiled_request_catalog_requests_missing"]
    request_map: dict[str, dict[str, Any]] = {}
    request_visual_ids: set[str] = set()
    for request in requests:
        if not isinstance(request, dict) or not request.get("RequestID"):
            errors.append("compiled_request_request_id_missing")
            continue
        request_id = str(request["RequestID"])
        if request_id in request_map:
            errors.append(f"compiled_request_duplicate:{request_id}")
        visual_id = str(request.get("VisualID", ""))
        if visual_id in request_visual_ids:
            errors.append(f"compiled_request_visual_id_duplicate:{visual_id}")
        request_visual_ids.add(visual_id)
        request_map[request_id] = request

    expected_summary = recompute_catalog_summary(requests)
    if catalog.get("Summary") != expected_summary:
        errors.append("catalog_summary_stale")

    manifest_entries = manifest.get("Entries", []) if isinstance(manifest, dict) else []
    if not isinstance(manifest_entries, list):
        errors.append("manifest_entries_invalid")
        manifest_entries = []
    else:
        errors.extend(manifest_uniqueness_errors([entry for entry in manifest_entries if isinstance(entry, dict)]))

    manifest_catalog = manifest.get("ArtStyleCatalog") if isinstance(manifest, dict) else None
    if not isinstance(manifest_catalog, dict):
        errors.append("style_catalog_missing")
    elif manifest_catalog.get("CatalogFingerprint") != catalog.get("ArtStyleCatalogFingerprint"):
        errors.append("style_catalog_fingerprint_mismatch")

    expected_manifest_fingerprint = sha256_json(_input_manifest(manifest))
    if catalog.get("ManifestFingerprint") != expected_manifest_fingerprint:
        errors.append("compiled_request_manifest_fingerprint_mismatch")

    asset_sets = manifest.get("AssetSets", {}) if isinstance(manifest.get("AssetSets"), dict) else {}
    for entry in manifest_entries:
        if not isinstance(entry, dict):
            continue
        visual_id = str(entry.get("VisualID", ""))
        pointer = entry.get("CompiledRequest")
        if not isinstance(pointer, dict) or not pointer.get("RequestID"):
            errors.append(f"compiled_request_missing:{visual_id}")
            continue
        request_id = str(pointer["RequestID"])
        request = request_map.get(request_id)
        if request is None:
            errors.append(f"compiled_request_missing:{visual_id}")
            continue
        if request.get("VisualID") != visual_id:
            errors.append(f"compiled_request_visual_id_mismatch:{visual_id}")
        if pointer.get("RequirementFingerprint") != request.get("RequirementFingerprint"):
            errors.append(f"compiled_request_fingerprint_mismatch:{visual_id}")
        requirement_state = str(request.get("RequirementStatus", ""))
        if requirement_state not in VALID_REQUIREMENT_STATES:
            errors.append(f"compiled_request_status_invalid:{visual_id}")
        authoring_state = str(request.get("PromptAuthoringStatus", ""))
        if authoring_state not in VALID_AUTHORING_STATES:
            errors.append(f"prompt_authoring_status_invalid:{visual_id}")
        if pointer.get("PromptAuthoringStatus") != authoring_state:
            errors.append(f"prompt_authoring_status_mismatch:{visual_id}")
        active_revision_id = str(request.get("ActivePromptRevisionID", "") or "")
        if str(pointer.get("ActivePromptRevisionID", "") or "") != active_revision_id:
            errors.append(f"prompt_revision_pointer_mismatch:{visual_id}")

        expected_request_hash = (
            sha256_json(requirement_request_body(request))
            if request.get("RequirementFingerprint")
            else ""
        )
        if expected_request_hash and expected_request_hash != request.get("RequirementFingerprint"):
            errors.append(f"compiled_request_payload_hash_mismatch:{visual_id}")

        revisions = request.get("PromptRevisions")
        if not isinstance(revisions, list):
            errors.append(f"prompt_revisions_missing:{visual_id}")
            revisions = []
        revision_ids: set[str] = set()
        for revision in revisions:
            if isinstance(revision, dict):
                revision_id = str(revision.get("PromptRevisionID", "") or "")
                if revision_id in revision_ids:
                    errors.append(f"prompt_revision_duplicate:{visual_id}:{revision_id}")
                revision_ids.add(revision_id)
            _append_revision_errors(errors, validate_prompt_revision(request, revision), visual_id)

        if authoring_state == "prompt_ready":
            if not active_revision_id or active_revision_id not in revision_ids:
                errors.append(f"prompt_revision_missing:{visual_id}:{active_revision_id}")
        elif active_revision_id:
            errors.append(f"prompt_revision_active_without_ready_status:{visual_id}")

        if requirement_state == "ready" and manifest_catalog:
            try:
                expected = compile_requirement_request(entry, manifest_catalog, asset_sets)
            except ValueError as exc:
                errors.append(f"compiled_request_recompile_failed:{visual_id}:{exc}")
            else:
                if expected.get("RequirementFingerprint") != request.get("RequirementFingerprint"):
                    errors.append(f"compiled_request_stale:{visual_id}")

    if strict and not requests:
        errors.append("compiled_request_catalog_empty")
    return errors


def main() -> int:
    import argparse
    import json
    from pathlib import Path

    parser = argparse.ArgumentParser(description="Validate P3 persisted art generation requests.")
    parser.add_argument("--manifest", required=True)
    parser.add_argument("--request-catalog", required=True)
    parser.add_argument("--strict", action="store_true")
    args = parser.parse_args()
    manifest = json.loads(Path(args.manifest).read_text(encoding="utf-8-sig"))
    catalog = json.loads(Path(args.request_catalog).read_text(encoding="utf-8-sig"))
    errors = validate_request_catalog(catalog, manifest, strict=args.strict)
    if errors:
        for error in errors:
            print(f"[FAIL] {error}")
        return 1
    print("[OK] compiled art generation request catalog")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
