# -*- coding: utf-8 -*-
"""Compile Manifest art requirements into persisted authoring context."""

from __future__ import annotations

import copy
import re
from typing import Any

from art_style_catalog import (
    resolve_entry_layers,
    sha256_json,
)


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
    for section in ("Preserve", "RequiredChanges", "Conditional"):
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
    contract = resolved.get("IdentityContract", {})
    brief: dict[str, Any] = {
        "Style": style_positive,
        "StyleNegative": style_negative,
        "Subject": _list(intent.get("SubjectEN") or intent.get("SubjectCN") or entry.get("DisplayName")),
        "Appearance": _list(intent.get("AppearanceEN") or intent.get("AppearanceCN")),
        "Mood": _list(intent.get("MoodEN") or intent.get("MoodCN")),
        "Composition": _list(intent.get("CompositionEN") or intent.get("CompositionCN")),
        "Required": _list(intent.get("RequiredElements")),
        "Forbidden": _list(intent.get("ForbiddenElements")) + _list(contract.get("Forbidden")),
        "Preserve": _list(intent.get("Preserve")) + _list(contract.get("Required")),
        "Conditional": _list(contract.get("Conditional")),
        "RequiredChanges": _list(intent.get("RequiredChanges")),
        "PoseSpec": copy.deepcopy(intent.get("PoseSpec") or entry.get("PoseSpec") or {}),
        "ResolvedLayers": copy.deepcopy(resolved.get("ResolvedLayers", [])),
        "SetRole": resolved.get("SetRole", ""),
        "SourceAssets": copy.deepcopy(resolved.get("SourceAssets", [])),
        "IdentitySources": copy.deepcopy(resolved.get("IdentitySources", [])),
        "PresentationGroup": resolved.get("PresentationGroup", ""),
    }
    brief["Style"] = _dedupe(brief["Style"])
    brief["StyleNegative"] = _dedupe(brief["StyleNegative"])
    for field in ("Subject", "Appearance", "Mood", "Composition", "Required", "Forbidden", "Preserve", "RequiredChanges", "Conditional"):
        brief[field] = _dedupe(brief[field])
    brief["SemanticUnits"] = _semantic_units(brief)
    return brief


def _context_items(values: Any, section: str, *, source: str) -> list[dict[str, Any]]:
    return [
        {
            "ID": f"brief:{section.lower()}:{index}",
            "Text": text,
            "Source": source,
        }
        for index, text in enumerate(_dedupe(_list(values)))
    ]


def build_prompt_authoring_context(
    entry: dict[str, Any],
    catalog: dict[str, Any],
    asset_sets: dict[str, Any],
) -> dict[str, Any]:
    """Build the deterministic context an Agent uses to author prompts.

    This function intentionally produces no executable Positive/Negative prompt
    and no generated Danbooru tags. It only resolves facts, constraints and
    evidence references.
    """

    brief = build_canonical_visual_brief(entry, catalog, asset_sets)
    evidence = entry.get("PromptEvidence") if isinstance(entry.get("PromptEvidence"), dict) else {}
    return {
        "HardConstraints": {
            "Identity": _context_items(
                brief.get("Preserve"),
                "identity",
                source="VisualIntent.Preserve+AssetSet.IdentityContract.Required",
            ),
            "RequiredChanges": _context_items(
                brief.get("RequiredChanges"),
                "requiredchanges",
                source="VisualIntent.RequiredChanges",
            ),
            "PoseSpec": copy.deepcopy(brief.get("PoseSpec") or {}),
            "ForbiddenChanges": _context_items(
                brief.get("Forbidden"),
                "forbidden",
                source="VisualIntent.ForbiddenElements",
            ),
            "Required": _context_items(
                brief.get("Required"),
                "required",
                source="VisualIntent.RequiredElements",
            ),
            "Conditional": _context_items(
                brief.get("Conditional"),
                "conditional",
                source="AssetSet.IdentityContract.Conditional",
            ),
            "Technical": [],
        },
        "Guidance": {
            "Style": _context_items(brief.get("Style"), "style", source="StyleRef"),
            "StyleNegative": _context_items(
                brief.get("StyleNegative"),
                "stylenegative",
                source="StyleRef.Negative",
            ),
            "Subject": _context_items(brief.get("Subject"), "subject", source="VisualIntent.Subject"),
            "Appearance": _context_items(
                brief.get("Appearance"),
                "appearance",
                source="VisualIntent.Appearance",
            ),
            "Mood": _context_items(brief.get("Mood"), "mood", source="VisualIntent.Mood"),
            "Composition": _context_items(
                brief.get("Composition"),
                "composition",
                source="VisualIntent.Composition",
            ),
        },
        "Evidence": {
            "IdentityDocuments": copy.deepcopy(brief.get("IdentitySources", [])),
            "ReferenceAssets": copy.deepcopy(brief.get("SourceAssets", [])),
            "PreviousSuccessfulPrompts": copy.deepcopy(evidence.get("PreviousSuccessfulPrompts", [])),
            "PreviousFailures": copy.deepcopy(evidence.get("PreviousFailures", [])),
        },
        "ResolvedLayers": copy.deepcopy(brief.get("ResolvedLayers", [])),
        "SetRole": brief.get("SetRole", ""),
        "PresentationGroup": brief.get("PresentationGroup", ""),
    }


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


def _technical_context_items(technical: dict[str, Any]) -> list[dict[str, Any]]:
    items: list[dict[str, Any]] = []
    for key in ("Width", "Height", "Format", "AlphaRequired", "BackgroundPolicy"):
        value = technical.get(key)
        if value not in (None, ""):
            items.append({"ID": f"technical:{key.lower()}", "Text": f"{key}={value}", "Source": "Spec"})
    for index, capability in enumerate(_list(technical.get("CapabilityRequirements"))):
        items.append(
            {
                "ID": f"technical:capability:{index}",
                "Text": capability,
                "Source": "Spec.CapabilityRequirements",
            }
        )
    return items


def requirement_request_body(request: dict[str, Any]) -> dict[str, Any]:
    return {
        "CompilerVersion": request.get("CompilerVersion"),
        "CatalogFingerprint": request.get("CatalogFingerprint", ""),
        "VisualID": request.get("VisualID", ""),
        "ProductionProfile": request.get("ProductionProfile", "standard_asset"),
        "PromptAuthoringContext": copy.deepcopy(request.get("PromptAuthoringContext", {})),
        "TechnicalRequest": copy.deepcopy(request.get("TechnicalRequest", {})),
        "PreservationContract": copy.deepcopy(request.get("PreservationContract", {})),
    }


def compile_requirement_request(
    entry: dict[str, Any],
    catalog: dict[str, Any],
    asset_sets: dict[str, Any],
    *,
    compiler_version: int = 2,
) -> dict[str, Any]:
    context = build_prompt_authoring_context(entry, catalog, asset_sets)
    technical = _technical_request(entry, context)
    context["HardConstraints"]["Technical"] = _technical_context_items(technical)
    hard = context["HardConstraints"]
    preservation = {
        "Preserve": copy.deepcopy(hard.get("Identity", [])),
        "RequiredChanges": copy.deepcopy(hard.get("RequiredChanges", [])),
        "Conditional": copy.deepcopy(hard.get("Conditional", [])),
        "PoseSpec": copy.deepcopy(hard.get("PoseSpec", {})),
        "ForbiddenChanges": copy.deepcopy(hard.get("ForbiddenChanges", [])),
        "Required": copy.deepcopy(hard.get("Required", [])),
        "IdentitySources": copy.deepcopy(context.get("Evidence", {}).get("IdentityDocuments", [])),
    }
    visual_id = str(entry.get("VisualID", ""))
    request: dict[str, Any] = {
        "CompilerVersion": compiler_version,
        "CatalogFingerprint": catalog.get("CatalogFingerprint", ""),
        "VisualID": visual_id,
        "ProductionProfile": entry.get("ProductionProfile", "standard_asset"),
        "RequirementStatus": "ready",
        "PromptAuthoringStatus": "prompt_authoring_required",
        "PromptAuthoringContext": context,
        "TechnicalRequest": technical,
        "PreservationContract": preservation,
        "ActivePromptRevisionID": "",
        "PromptRevisions": [],
    }
    requirement_fingerprint = sha256_json(requirement_request_body(request))
    request["RequirementFingerprint"] = requirement_fingerprint
    request["RequestID"] = f"{visual_id}@{requirement_fingerprint[:12]}"
    return request
