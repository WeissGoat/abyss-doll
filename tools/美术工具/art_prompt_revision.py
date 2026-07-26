# -*- coding: utf-8 -*-
"""Validate, publish and select immutable Agent-authored art prompts."""

from __future__ import annotations

import copy
from typing import Any

from art_style_catalog import sha256_json


NATURAL_LANGUAGE_FORMAT = "natural_language_v2"
DANBOORU_TAGS_FORMAT = "danbooru_tags_v2"
PROMPT_FORMATS = (NATURAL_LANGUAGE_FORMAT, DANBOORU_TAGS_FORMAT)
PROMPT_FORMAT_BY_PROVIDER = {
    "openai_images": NATURAL_LANGUAGE_FORMAT,
    "gemini_chat_image": NATURAL_LANGUAGE_FORMAT,
    "gemini_nanobanana": NATURAL_LANGUAGE_FORMAT,
    "novelai": DANBOORU_TAGS_FORMAT,
}
VARIANT_STATES = {"ready", "unsupported", "invalid"}


def compute_revision_fingerprint(revision: dict[str, Any]) -> str:
    payload = copy.deepcopy(revision)
    payload.pop("RevisionFingerprint", None)
    return sha256_json(payload)


def _hard_constraint_ids(request: dict[str, Any]) -> list[str]:
    context = request.get("PromptAuthoringContext", {})
    hard = context.get("HardConstraints", {}) if isinstance(context, dict) else {}
    result: list[str] = []
    if not isinstance(hard, dict):
        return result
    for values in hard.values():
        if not isinstance(values, list):
            continue
        for item in values:
            if not isinstance(item, dict):
                continue
            constraint_id = str(item.get("ID", "")).strip()
            if constraint_id and constraint_id not in result:
                result.append(constraint_id)
    return result


def _nonempty_reason(value: Any) -> bool:
    if isinstance(value, list):
        return any(str(item).strip() for item in value)
    return bool(str(value or "").strip())


def _validate_mapping(
    variant: dict[str, Any],
    format_id: str,
    constraint_ids: list[str],
    errors: list[str],
) -> None:
    mapping = variant.get("ConstraintMapping")
    if not isinstance(mapping, dict):
        errors.append(f"constraint_mapping_missing:{format_id}")
        return
    for constraint_id in constraint_ids:
        value = mapping.get(constraint_id)
        if isinstance(value, list):
            covered = any(str(item).strip() for item in value)
        else:
            covered = bool(str(value or "").strip())
        if not covered:
            errors.append(f"constraint_mapping_incomplete:{format_id}:{constraint_id}")


def _validate_tags(
    variant: dict[str, Any],
    format_id: str,
    field: str,
    errors: list[str],
) -> None:
    values = variant.get(field)
    if not isinstance(values, list):
        errors.append(f"prompt_tags_invalid:{format_id}:{field}")
        return
    seen: set[str] = set()
    for index, item in enumerate(values):
        if not isinstance(item, dict) or not str(item.get("Tag", "")).strip():
            errors.append(f"prompt_tag_invalid:{format_id}:{field}:{index}")
            continue
        tag = str(item["Tag"]).strip().lower()
        if tag in seen:
            errors.append(f"prompt_tag_duplicate:{format_id}:{field}:{index}")
        seen.add(tag)
        try:
            weight = float(item.get("Weight", 1.0))
        except (TypeError, ValueError):
            weight = 0.0
        if weight <= 0:
            errors.append(f"prompt_tag_weight_invalid:{format_id}:{field}:{index}")


def _validate_variant(
    variant: Any,
    format_id: str,
    constraint_ids: list[str],
    errors: list[str],
) -> None:
    if not isinstance(variant, dict):
        errors.append(f"prompt_variant_missing:{format_id}")
        return
    if variant.get("Format") != format_id:
        errors.append(f"prompt_variant_format_mismatch:{format_id}")
    state = str(variant.get("Status", ""))
    if state not in VARIANT_STATES:
        errors.append(f"prompt_variant_status_invalid:{format_id}")
        return
    if state == "unsupported":
        if not _nonempty_reason(variant.get("UnsupportedReason")):
            errors.append(f"prompt_variant_unsupported_reason_missing:{format_id}")
        return
    if state == "invalid":
        return

    if format_id == NATURAL_LANGUAGE_FORMAT:
        if not str(variant.get("Positive", "")).strip():
            errors.append(f"prompt_positive_missing:{format_id}")
        if not isinstance(variant.get("Negative", ""), str):
            errors.append(f"prompt_negative_invalid:{format_id}")
        if not isinstance(variant.get("OutputContract"), dict):
            errors.append(f"prompt_output_contract_missing:{format_id}")
    else:
        _validate_tags(variant, format_id, "PositiveTags", errors)
        _validate_tags(variant, format_id, "NegativeTags", errors)
        if not isinstance(variant.get("ReferenceControls", {}), dict):
            errors.append(f"prompt_reference_controls_invalid:{format_id}")
    _validate_mapping(variant, format_id, constraint_ids, errors)


def validate_prompt_revision(request: dict[str, Any], revision: Any) -> list[str]:
    errors: list[str] = []
    if not isinstance(revision, dict):
        return ["prompt_revision_invalid"]
    request_id = str(request.get("RequestID", ""))
    revision_id = str(revision.get("PromptRevisionID", ""))
    if not revision_id or not revision_id.startswith(f"{request_id}/prompt-"):
        errors.append("prompt_revision_id_invalid")
    if revision.get("RequirementFingerprint") != request.get("RequirementFingerprint"):
        errors.append("prompt_revision_stale")
    if revision.get("AuthoringMode") != "agent_authored":
        errors.append("prompt_revision_authoring_mode_invalid")
    if revision.get("Status") != "ready":
        errors.append("prompt_revision_status_invalid")
    if not isinstance(revision.get("CommonStrategy"), dict):
        errors.append("prompt_revision_strategy_missing")

    variants = revision.get("Variants")
    constraint_ids = _hard_constraint_ids(request)
    ready_count = 0
    if not isinstance(variants, dict):
        errors.extend(f"prompt_variant_missing:{format_id}" for format_id in PROMPT_FORMATS)
    else:
        for format_id in PROMPT_FORMATS:
            variant = variants.get(format_id)
            _validate_variant(variant, format_id, constraint_ids, errors)
            if isinstance(variant, dict) and variant.get("Status") == "ready":
                ready_count += 1
    if ready_count == 0:
        errors.append("prompt_revision_no_ready_variant")

    supplied_fingerprint = str(revision.get("RevisionFingerprint", "") or "")
    if supplied_fingerprint and supplied_fingerprint != compute_revision_fingerprint(revision):
        errors.append("prompt_revision_fingerprint_mismatch")
    return errors


def publish_prompt_revision(
    request: dict[str, Any],
    revision: dict[str, Any],
    *,
    activate: bool = True,
) -> dict[str, Any]:
    result = copy.deepcopy(request)
    candidate = copy.deepcopy(revision)
    errors = validate_prompt_revision(result, candidate)
    if errors:
        raise ValueError(";".join(errors))
    candidate["RevisionFingerprint"] = compute_revision_fingerprint(candidate)

    revisions = result.get("PromptRevisions")
    if not isinstance(revisions, list):
        revisions = []
    revision_id = candidate["PromptRevisionID"]
    existing = next(
        (
            item
            for item in revisions
            if isinstance(item, dict) and item.get("PromptRevisionID") == revision_id
        ),
        None,
    )
    if existing is not None:
        if compute_revision_fingerprint(existing) != candidate["RevisionFingerprint"]:
            raise ValueError(f"prompt_revision_immutable:{revision_id}")
    else:
        revisions.append(candidate)
    result["PromptRevisions"] = revisions
    if activate:
        result["ActivePromptRevisionID"] = revision_id
        result["PromptAuthoringStatus"] = "prompt_ready"
    return result


def _find_revision(request: dict[str, Any], revision_id: str) -> dict[str, Any]:
    for revision in request.get("PromptRevisions", []):
        if isinstance(revision, dict) and revision.get("PromptRevisionID") == revision_id:
            return revision
    raise ValueError(f"prompt_revision_missing:{request.get('VisualID', '')}:{revision_id}")


def select_prompt_variant(
    request: dict[str, Any],
    *,
    prompt_revision_id: str = "",
    prompt_format: str = "auto",
    provider: str = "",
) -> tuple[dict[str, Any], str, dict[str, Any]]:
    if request.get("RequirementStatus") != "ready":
        raise ValueError(f"compiled_request_not_ready:{request.get('VisualID', '')}")
    revision_id = prompt_revision_id or str(request.get("ActivePromptRevisionID", ""))
    if not revision_id or request.get("PromptAuthoringStatus") != "prompt_ready":
        raise ValueError(f"prompt_authoring_required:{request.get('VisualID', '')}")
    revision = _find_revision(request, revision_id)
    errors = validate_prompt_revision(request, revision)
    if errors:
        raise ValueError(";".join(errors))

    selected_format = prompt_format
    if selected_format == "auto":
        selected_format = PROMPT_FORMAT_BY_PROVIDER.get(provider, NATURAL_LANGUAGE_FORMAT)
    if selected_format not in PROMPT_FORMATS:
        raise ValueError(f"prompt_format_unsupported:{selected_format}")
    variant = revision.get("Variants", {}).get(selected_format)
    if not isinstance(variant, dict) or variant.get("Status") != "ready":
        raise ValueError(f"prompt_variant_not_ready:{selected_format}:{request.get('VisualID', '')}")
    providers = variant.get("CompatibleProviders")
    if provider and isinstance(providers, list) and providers and provider not in providers:
        raise ValueError(f"provider_capability_mismatch:{selected_format}:{provider}")
    return revision, selected_format, variant
