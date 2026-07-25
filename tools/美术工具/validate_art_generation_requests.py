# -*- coding: utf-8 -*-
"""Validate persisted P3 art generation requests against a Manifest."""

from __future__ import annotations

import copy
from typing import Any

from art_prompt_compiler import compile_generation_request
from art_style_catalog import sha256_json
from compile_art_generation_requests import _input_manifest


VALID_COMPILE_STATES = {"ready", "style_resolution_required", "unsupported", "invalid", "stale"}
VALID_VARIANT_STATES = {"ready", "unsupported", "invalid"}


def _request_body(request: dict[str, Any]) -> dict[str, Any]:
    return {
        key: copy.deepcopy(request.get(key))
        for key in (
            "CompilerVersion",
            "InputFingerprint",
            "ResolvedLayers",
            "CanonicalVisualBrief",
            "PromptVariants",
            "TechnicalRequest",
            "PreservationContract",
        )
    }


def _validate_variant(variant: Any, expected_format: str, errors: list[str], visual_id: str) -> None:
    if not isinstance(variant, dict):
        errors.append(f"prompt_variant_missing:{expected_format}:{visual_id}")
        return
    state = str(variant.get("CompileStatus", ""))
    if state not in VALID_VARIANT_STATES:
        errors.append(f"prompt_variant_status_invalid:{expected_format}:{visual_id}")
    coverage = variant.get("SemanticCoverage")
    if not isinstance(coverage, dict):
        errors.append(f"semantic_coverage_missing:{expected_format}:{visual_id}")
    elif state == "ready" and (coverage.get("Coverage") != 1.0 or coverage.get("Unmapped")):
        errors.append(f"semantic_coverage_incomplete:{expected_format}:{visual_id}")
    if expected_format == "natural_language_v1" and state == "ready":
        if not str(variant.get("Positive", "")).strip():
            errors.append(f"prompt_positive_missing:{visual_id}")
    if expected_format == "danbooru_tags_v1":
        for field in ("PositiveTags", "NegativeTags"):
            tags = variant.get(field, [])
            if not isinstance(tags, list):
                errors.append(f"tag_list_invalid:{field}:{visual_id}")
                continue
            for item in tags:
                if not isinstance(item, dict) or not str(item.get("Tag", "")).strip():
                    errors.append(f"tag_item_invalid:{visual_id}")
                elif float(item.get("Weight", 0)) <= 0:
                    errors.append(f"tag_weight_invalid:{visual_id}")


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
    for request in requests:
        if not isinstance(request, dict) or not request.get("RequestID"):
            errors.append("compiled_request_request_id_missing")
            continue
        request_id = str(request["RequestID"])
        if request_id in request_map:
            errors.append(f"compiled_request_duplicate:{request_id}")
        request_map[request_id] = request

    manifest_catalog = manifest.get("ArtStyleCatalog") if isinstance(manifest, dict) else None
    if not isinstance(manifest_catalog, dict):
        errors.append("style_catalog_missing")
    elif manifest_catalog.get("CatalogFingerprint") != catalog.get("ArtStyleCatalogFingerprint"):
        errors.append("style_catalog_fingerprint_mismatch")

    expected_manifest_fingerprint = sha256_json(_input_manifest(manifest))
    if catalog.get("ManifestFingerprint") != expected_manifest_fingerprint:
        errors.append("compiled_request_manifest_fingerprint_mismatch")

    asset_sets = manifest.get("AssetSets", {}) if isinstance(manifest.get("AssetSets"), dict) else {}
    for entry in manifest.get("Entries", []):
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
        if pointer.get("RequestFingerprint") != request.get("RequestFingerprint"):
            errors.append(f"compiled_request_fingerprint_mismatch:{visual_id}")
        state = str(request.get("CompileStatus", ""))
        if state not in VALID_COMPILE_STATES:
            errors.append(f"compiled_request_status_invalid:{visual_id}")
        expected_request_hash = sha256_json(_request_body(request)) if request.get("RequestFingerprint") else ""
        if expected_request_hash and expected_request_hash != request.get("RequestFingerprint"):
            errors.append(f"compiled_request_payload_hash_mismatch:{visual_id}")
        variants = request.get("PromptVariants", {})
        if not isinstance(variants, dict):
            errors.append(f"prompt_variants_missing:{visual_id}")
            continue
        for format_id in ("natural_language_v1", "danbooru_tags_v1"):
            _validate_variant(variants.get(format_id), format_id, errors, visual_id)
        if state == "ready" and not any(
            isinstance(variants.get(format_id), dict) and variants[format_id].get("CompileStatus") == "ready"
            for format_id in ("natural_language_v1", "danbooru_tags_v1")
        ):
            errors.append(f"compiled_request_ready_without_variant:{visual_id}")
        if state == "ready" and manifest_catalog:
            try:
                expected = compile_generation_request(entry, manifest_catalog, asset_sets)
            except ValueError as exc:
                errors.append(f"compiled_request_recompile_failed:{visual_id}:{exc}")
            else:
                if expected.get("RequestFingerprint") != request.get("RequestFingerprint"):
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
