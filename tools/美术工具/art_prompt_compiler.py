# -*- coding: utf-8 -*-
"""Compile Manifest art requirements into reusable prompt variants."""

from __future__ import annotations

import copy
import re
from typing import Any

from art_style_catalog import (
    resolve_entry_layers,
    sha256_json,
)


NATURAL_LANGUAGE_FORMAT = "natural_language_v1"
DANBOORU_TAGS_FORMAT = "danbooru_tags_v1"


TAG_MAP: dict[str, list[tuple[str, float]]] = {
    "modular 2d interface button sprite": [("button", 1.0), ("game ui", 1.0)],
    "single readable game asset": [("single object", 1.0), ("simple background", 0.8)],
    "clean interface silhouette with no baked text": [("simple background", 0.8), ("no text", 1.0)],
    "single horizontal button skin": [("button", 1.0), ("horizontal", 1.0)],
    "continuous outer frame": [("frame", 1.0), ("connected border", 1.0)],
    "clean stretchable center": [("empty center", 1.0), ("simple background", 0.8)],
    "crimson primary action surface": [("red", 1.0), ("crimson", 1.1)],
    "full-body anime game character portrait": [("1girl", 1.0), ("full body", 1.1)],
    "clear readable silhouette": [("simple background", 0.8)],
    "silver hair": [("silver hair", 1.2)],
    "white blindfold": [("blindfold", 1.3), ("white blindfold", 1.1)],
    "gray shawl": [("shawl", 1.1), ("grey clothes", 0.8)],
    "pale inner dress": [("dress", 1.0), ("pale clothes", 0.8)],
    "bare legs": [("bare legs", 1.0)],
    "bare feet": [("barefoot", 1.0)],
    "restrained confused expression": [("confused", 1.1), ("subtle expression", 1.0)],
    "slight uncertain head tilt": [("head tilt", 1.0)],
    "transparent background": [("transparent background", 1.1)],
    "single character": [("solo", 1.0)],
    "normal state no red glow": [("no glow", 1.0)],
}


def _list(value: Any) -> list[str]:
    if isinstance(value, list):
        return [str(item).strip() for item in value if str(item).strip()]
    if value in (None, ""):
        return []
    return [str(value).strip()]


def _dedupe(values: list[str]) -> list[str]:
    result: list[str] = []
    seen: set[str] = set()
    for value in values:
        key = re.sub(r"\s+", " ", value.strip()).lower()
        if key and key not in seen:
            result.append(re.sub(r"\s+", " ", value.strip()))
            seen.add(key)
    return result


def _node_positive(node: Any) -> list[str]:
    return _list(node.get("PositiveEN", [])) if isinstance(node, dict) else []


def _node_negative(node: Any) -> list[str]:
    return _list(node.get("NegativeEN", [])) if isinstance(node, dict) else []


def _source_spec(spec: Any) -> dict[str, Any]:
    if not isinstance(spec, dict):
        return {}
    source = spec.get("SourceSpec")
    return source if isinstance(source, dict) else spec


def _process_spec(spec: Any) -> dict[str, Any]:
    if not isinstance(spec, dict):
        return {}
    process = spec.get("ProcessSpec")
    return process if isinstance(process, dict) else {}


def _semantic_units(brief: dict[str, Any]) -> list[dict[str, Any]]:
    units: list[dict[str, Any]] = []
    for section in ("Subject", "Appearance", "Mood", "Composition", "Required", "Forbidden"):
        for index, text in enumerate(_list(brief.get(section))):
            units.append(
                {
                    "ID": f"brief:{section.lower()}:{index}",
                    "Text": text,
                    "Section": section,
                    "Required": section != "Forbidden",
                }
            )
    for section in ("Preserve", "RequiredChanges"):
        for index, text in enumerate(_list(brief.get(section))):
            units.append(
                {
                    "ID": f"brief:{section.lower()}:{index}",
                    "Text": text,
                    "Section": section,
                    "Required": True,
                    "Structured": True,
                }
            )
    return units


def build_canonical_visual_brief(
    entry: dict[str, Any],
    catalog: dict[str, Any],
    asset_sets: dict[str, Any],
) -> dict[str, Any]:
    resolved = resolve_entry_layers(entry, catalog, asset_sets)
    intent = entry.get("VisualIntent") if isinstance(entry.get("VisualIntent"), dict) else {}

    style_nodes = [
        resolved.get("Global", {}),
        resolved.get("Profile", {}),
        resolved.get("Family", {}),
        resolved.get("Role", {}),
        resolved.get("ContextAccent", {}),
    ]
    style_positive = _dedupe([text for node in style_nodes for text in _node_positive(node)])
    style_negative = _dedupe([text for node in style_nodes for text in _node_negative(node)])
    brief: dict[str, Any] = {
        "Style": style_positive,
        "StyleNegative": style_negative,
        "Subject": _list(intent.get("SubjectEN") or intent.get("SubjectCN") or entry.get("DisplayName")),
        "Appearance": _list(intent.get("AppearanceEN") or intent.get("AppearanceCN")),
        "Mood": _list(intent.get("MoodEN") or intent.get("MoodCN")),
        "Composition": _list(intent.get("CompositionEN") or intent.get("CompositionCN")),
        "Required": _list(intent.get("RequiredElements")),
        "Forbidden": _list(intent.get("ForbiddenElements")),
        "Preserve": _list(intent.get("Preserve")) + _list(resolved.get("IdentityLocks")),
        "RequiredChanges": _list(intent.get("RequiredChanges")),
        "ResolvedLayers": copy.deepcopy(resolved.get("ResolvedLayers", [])),
        "SetRole": resolved.get("SetRole", ""),
        "SourceAssets": copy.deepcopy(resolved.get("SourceAssets", [])),
        "IdentitySources": copy.deepcopy(resolved.get("IdentitySources", [])),
    }
    brief["Style"] = _dedupe(brief["Style"])
    brief["StyleNegative"] = _dedupe(brief["StyleNegative"])
    for field in ("Subject", "Appearance", "Mood", "Composition", "Required", "Forbidden", "Preserve", "RequiredChanges"):
        brief[field] = _dedupe(brief[field])
    brief["SemanticUnits"] = _semantic_units(brief)
    return brief


def _coverage(units: list[dict[str, Any]], unsupported: list[str]) -> dict[str, Any]:
    required = [unit for unit in units if unit.get("Required")]
    missing = _dedupe(unsupported)
    mapped = max(0, len(required) - len(missing))
    return {
        "Required": len(required),
        "Mapped": mapped,
        "Unmapped": missing,
        "Coverage": 1.0 if not required else round(mapped / len(required), 4),
    }


def serialize_natural_language(brief: dict[str, Any]) -> dict[str, Any]:
    positive = _dedupe(
        _list(brief.get("Style"))
        + _list(brief.get("Subject"))
        + _list(brief.get("Appearance"))
        + _list(brief.get("Mood"))
        + _list(brief.get("Composition"))
        + _list(brief.get("Required"))
        + _list(brief.get("Preserve"))
        + _list(brief.get("RequiredChanges"))
    )
    negative = _dedupe(_list(brief.get("StyleNegative")) + _list(brief.get("Forbidden")))
    coverage = _coverage(
        [unit for unit in brief.get("SemanticUnits", []) if isinstance(unit, dict)],
        [],
    )
    payload = {
        "Format": NATURAL_LANGUAGE_FORMAT,
        "CompileStatus": "ready",
        "Positive": ", ".join(positive),
        "Negative": ", ".join(negative),
        "SemanticCoverage": coverage,
    }
    payload["VariantFingerprint"] = sha256_json(payload)
    return payload


def _tag_entries(text: str) -> list[dict[str, Any]]:
    mapped = TAG_MAP.get(text.strip().lower())
    if mapped is None:
        return []
    return [{"Tag": tag, "Weight": weight} for tag, weight in mapped]


def _dedupe_tags(values: list[dict[str, Any]]) -> list[dict[str, Any]]:
    result: list[dict[str, Any]] = []
    seen: set[str] = set()
    for value in values:
        tag = str(value.get("Tag", "")).strip()
        if not tag or tag.lower() in seen:
            continue
        weight = float(value.get("Weight", 1.0))
        if weight <= 0:
            continue
        result.append({"Tag": tag, "Weight": weight})
        seen.add(tag.lower())
    return result


def serialize_danbooru_tags(brief: dict[str, Any]) -> dict[str, Any]:
    units = [unit for unit in brief.get("SemanticUnits", []) if isinstance(unit, dict)]
    positive_tags: list[dict[str, Any]] = []
    negative_tags: list[dict[str, Any]] = []
    unsupported: list[str] = []
    for unit in units:
        text = str(unit.get("Text", "")).strip()
        tags = _tag_entries(text)
        if unit.get("Section") == "Forbidden":
            if tags:
                negative_tags.extend(tags)
            elif unit.get("Required"):
                unsupported.append(text)
        elif unit.get("Structured"):
            # Identity preservation and edit intent remain structured fields.
            continue
        elif tags:
            positive_tags.extend(tags)
        elif unit.get("Required"):
            unsupported.append(text)

    positive_tags = _dedupe_tags(positive_tags)
    negative_tags = _dedupe_tags(negative_tags)
    coverage = _coverage(units, unsupported)
    payload: dict[str, Any] = {
        "Format": DANBOORU_TAGS_FORMAT,
        "CompileStatus": "ready" if not unsupported else "unsupported",
        "PositiveTags": positive_tags,
        "NegativeTags": negative_tags,
        "SemanticCoverage": coverage,
    }
    payload["VariantFingerprint"] = sha256_json(payload)
    return payload


def _technical_request(entry: dict[str, Any], brief: dict[str, Any]) -> dict[str, Any]:
    spec = entry.get("Spec") if isinstance(entry.get("Spec"), dict) else {}
    source = _source_spec(spec)
    process = _process_spec(spec)
    capabilities: list[str] = []
    nine_slice = process.get("NineSlice")
    if isinstance(nine_slice, dict) and nine_slice.get("Enabled"):
        capabilities.append("nine_slice_safe_frame")
    if source.get("AlphaRequired"):
        capabilities.append("transparent_asset")
    if entry.get("ProductionProfile") == "character_portrait_set":
        capabilities.append("transparent_character_portrait")
    return {
        "Width": source.get("Width"),
        "Height": source.get("Height"),
        "Format": source.get("Format", "png"),
        "AlphaRequired": bool(source.get("AlphaRequired")),
        "BackgroundPolicy": process.get("BackgroundPolicy", ""),
        "CapabilityRequirements": _dedupe(capabilities),
        "ReferenceAssets": copy.deepcopy(entry.get("SourceAssets", [])),
    }


def compile_generation_request(
    entry: dict[str, Any],
    catalog: dict[str, Any],
    asset_sets: dict[str, Any],
    *,
    compiler_version: int = 1,
) -> dict[str, Any]:
    brief = build_canonical_visual_brief(entry, catalog, asset_sets)
    natural = serialize_natural_language(brief)
    tags = serialize_danbooru_tags(brief)
    technical = _technical_request(entry, brief)
    preservation = {
        "Preserve": copy.deepcopy(brief.get("Preserve", [])),
        "RequiredChanges": copy.deepcopy(brief.get("RequiredChanges", [])),
        "IdentitySources": copy.deepcopy(brief.get("IdentitySources", [])),
    }
    input_payload = {
        "CompilerVersion": compiler_version,
        "CatalogFingerprint": catalog.get("CatalogFingerprint", ""),
        "VisualID": entry.get("VisualID", ""),
        "ProductionProfile": entry.get("ProductionProfile", "standard_asset"),
        "StyleRef": entry.get("StyleRef", {}),
        "VisualIntent": entry.get("VisualIntent", {}),
        "Spec": entry.get("Spec", {}),
        "AssetSetID": entry.get("AssetSetID", ""),
        "SetRole": entry.get("SetRole", ""),
        "SourceAssets": entry.get("SourceAssets", []),
    }
    input_fingerprint = sha256_json(input_payload)
    request_body = {
        "CompilerVersion": compiler_version,
        "InputFingerprint": input_fingerprint,
        "ResolvedLayers": brief.get("ResolvedLayers", []),
        "CanonicalVisualBrief": brief,
        "PromptVariants": {
            NATURAL_LANGUAGE_FORMAT: natural,
            DANBOORU_TAGS_FORMAT: tags,
        },
        "TechnicalRequest": technical,
        "PreservationContract": preservation,
    }
    request_fingerprint = sha256_json(request_body)
    compile_status = "ready" if any(
        variant.get("CompileStatus") == "ready"
        for variant in request_body["PromptVariants"].values()
    ) else "unsupported"
    visual_id = str(entry.get("VisualID", ""))
    return {
        "RequestID": f"{visual_id}@{request_fingerprint[:12]}",
        "VisualID": visual_id,
        "ProductionProfile": entry.get("ProductionProfile", "standard_asset"),
        "InputFingerprint": input_fingerprint,
        "RequestFingerprint": request_fingerprint,
        "CompileStatus": compile_status,
        **request_body,
    }
