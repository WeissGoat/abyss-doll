# -*- coding: utf-8 -*-
"""Compile Manifest entries into a persisted, multi-format Request Catalog."""

from __future__ import annotations

import argparse
import copy
import json
from datetime import datetime
from pathlib import Path
from typing import Any

from art_catalog_integrity import (
    CatalogIntegrityError,
    merge_request_catalog,
    recompute_catalog_summary,
    validate_manifest_uniqueness,
)
from art_prompt_compiler import compile_requirement_request
from art_style_catalog import build_catalog_snapshot, sha256_json
from generate_art_prompts import visual_intent_for


DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_OUTPUT = "美术文档/_generated/art_generation_requests.json"
DEFAULT_REPORT = "美术文档/_generated/art_generation_request_migration.json"
COMPILER_VERSION = 2

STYLE_REF_OVERRIDES: dict[str, dict[str, Any]] = {
    "ui_button_primary": {
        "Profile": "ui_v1",
        "Family": "ui_button_core_v1",
        "Role": "primary",
        "ContextAccent": "none",
    },
}

ZERO_IDENTITY_LOCKS = [
    "silver hair",
    "white blindfold",
    "gray shawl",
    "pale inner dress",
    "bare legs",
    "bare feet",
    "normal state no red glow",
]


def read_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(f".{path.name}.tmp")
    temporary.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    temporary.replace(path)


def _profile_for_entry(entry: dict[str, Any]) -> str:
    if entry.get("ProductionProfile") == "character_portrait_set":
        return "character_portrait_v1"
    if entry.get("Domain") == "ui":
        return "ui_v1"
    if entry.get("Domain") in {"background", "narrative_cg"}:
        return "environment_v1"
    return "icon_v1"


def _style_ref_for_entry(entry: dict[str, Any]) -> dict[str, Any]:
    visual_id = str(entry.get("VisualID", ""))
    if isinstance(entry.get("StyleRef"), dict) and entry["StyleRef"]:
        return copy.deepcopy(entry["StyleRef"])
    if visual_id in STYLE_REF_OVERRIDES:
        return copy.deepcopy(STYLE_REF_OVERRIDES[visual_id])
    return {"Profile": _profile_for_entry(entry), "ContextAccent": "none"}


def _asset_sets(manifest: dict[str, Any]) -> dict[str, Any]:
    result = copy.deepcopy(manifest.get("AssetSets", {})) if isinstance(manifest.get("AssetSets"), dict) else {}
    members: dict[str, list[dict[str, Any]]] = {}
    for entry in manifest.get("Entries", []):
        if not isinstance(entry, dict):
            continue
        asset_set_id = str(entry.get("AssetSetID", ""))
        if not asset_set_id:
            continue
        members.setdefault(asset_set_id, []).append(
            {
                "AssetID": entry.get("AssetID", ""),
                "VisualID": entry.get("VisualID", ""),
                "SetRole": entry.get("SetRole", ""),
                "SourceAssets": copy.deepcopy(entry.get("SourceAssets", [])),
            }
        )
    for asset_set_id, member_list in members.items():
        asset_set = result.setdefault(asset_set_id, {})
        asset_set.setdefault("ProductionProfile", "character_portrait_set")
        asset_set.setdefault("StyleRef", {"Profile": "character_portrait_v1"})
        asset_set.setdefault("IdentitySources", [])
        asset_set.setdefault("IdentityLocks", [])
        asset_set.setdefault("ConsistencyRules", ["preserve identity and canvas baseline across members"])
        if asset_set_id == "zero_dialogue_portrait_v1" and not asset_set["IdentityLocks"]:
            asset_set["IdentityLocks"] = copy.deepcopy(ZERO_IDENTITY_LOCKS)
            asset_set["IdentitySources"] = [
                "美术文档/人设/03_零号初版人设方案.md",
                "美术文档/人设/05_零号立绘素材设计与交付清单.md",
            ]
        asset_set["Members"] = sorted(member_list, key=lambda item: (str(item.get("SetRole", "")), str(item.get("VisualID", ""))))
    return result


def _input_manifest(manifest: dict[str, Any]) -> dict[str, Any]:
    snapshot = copy.deepcopy(manifest)
    snapshot.pop("ArtStyleCatalog", None)
    snapshot.pop("AssetSets", None)
    entries = []
    for entry in snapshot.get("Entries", []):
        if not isinstance(entry, dict):
            continue
        entry = copy.deepcopy(entry)
        for field in (
            # Manifest lifecycle and processing fields are mutable evidence. They
            # must not invalidate a persisted request whose semantic contract is
            # unchanged (for example approved -> registered or a new candidate round).
            "Status",
            "PromptCN",
            "PromptEN",
            "NegativePromptEN",
            "CompiledRequest",
            "Notes",
            "BatchID",
            "RawPath",
            "SelectedPath",
            "ApprovedPath",
            "RegistryStatus",
            "CandidateBatchID",
            "CandidateRawFiles",
            "CandidateRawPath",
            "ReplacementBatchID",
            "QualityTier",
            "QualityUpdatedAt",
        ):
            entry.pop(field, None)
        entries.append(entry)
    snapshot["Entries"] = entries
    return snapshot


def _prompt_cn(entry: dict[str, Any], intent: dict[str, Any]) -> str:
    existing = str(entry.get("PromptCN", "") or "").strip()
    if existing:
        return existing
    values = []
    for key in ("SubjectCN", "AppearanceCN", "CompositionCN"):
        value = intent.get(key, [])
        values.extend(value if isinstance(value, list) else [value])
    return "".join(str(value).strip() for value in values if str(value).strip())


def _requests_by_visual(catalog: dict[str, Any] | None) -> dict[str, dict[str, Any]]:
    if not isinstance(catalog, dict):
        return {}
    return {
        str(request.get("VisualID", "")): request
        for request in catalog.get("Requests", [])
        if isinstance(request, dict) and str(request.get("VisualID", ""))
    }


def _canonical_previous_catalog_for_manifest(
    previous_catalog: dict[str, Any],
    manifest: dict[str, Any],
) -> dict[str, Any]:
    """Recover one previous Request per canonical Entry during legacy migration."""
    previous_requests = previous_catalog.get("Requests", [])
    if not isinstance(previous_requests, list):
        return copy.deepcopy(previous_catalog)
    result = copy.deepcopy(previous_catalog)
    selected: list[dict[str, Any]] = []
    for entry in manifest.get("Entries", []):
        if not isinstance(entry, dict):
            continue
        visual_id = str(entry.get("VisualID", ""))
        if not visual_id:
            continue
        pointer = entry.get("CompiledRequest") if isinstance(entry.get("CompiledRequest"), dict) else {}
        request_id = str(pointer.get("RequestID", ""))
        matches = [
            request
            for request in previous_requests
            if isinstance(request, dict)
            and request.get("VisualID") == visual_id
            and (not request_id or request.get("RequestID") == request_id)
        ]
        if not matches and request_id:
            matches = [
                request
                for request in previous_requests
                if isinstance(request, dict) and request.get("VisualID") == visual_id
            ]
        if matches:
            serialized = {
                json.dumps(request, ensure_ascii=False, sort_keys=True, separators=(",", ":"))
                for request in matches
            }
            if len(serialized) == 1:
                selected.append(copy.deepcopy(matches[0]))
    result["Requests"] = selected
    return result


def _legacy_prompt_variants(entry: dict[str, Any], previous: dict[str, Any] | None) -> dict[str, Any]:
    legacy = copy.deepcopy(previous.get("LegacyPromptVariants", {})) if isinstance(previous, dict) else {}
    prompt = str(entry.get("PromptEN", "") or "").strip()
    negative = str(entry.get("NegativePromptEN", "") or "").strip()
    if prompt and "natural_language_v1" not in legacy:
        legacy["natural_language_v1"] = {
            "Format": "natural_language_v1",
            "Status": "legacy_compiled",
            "Positive": prompt,
            "Negative": negative,
        }
    if isinstance(previous, dict):
        old_variants = previous.get("PromptVariants", {})
        if isinstance(old_variants, dict):
            for format_id, variant in old_variants.items():
                if not isinstance(variant, dict) or format_id in legacy:
                    continue
                migrated = copy.deepcopy(variant)
                migrated["Status"] = "legacy_compiled"
                migrated.pop("CompileStatus", None)
                legacy[format_id] = migrated
    return legacy


def carry_forward_prompt_revisions(
    request: dict[str, Any],
    previous: dict[str, Any] | None,
) -> dict[str, Any]:
    result = copy.deepcopy(request)
    if not isinstance(previous, dict):
        return result
    if previous.get("RequirementFingerprint") != result.get("RequirementFingerprint"):
        return result
    revisions = copy.deepcopy(previous.get("PromptRevisions", []))
    active = str(previous.get("ActivePromptRevisionID", "") or "")
    result["PromptRevisions"] = revisions
    result["ActivePromptRevisionID"] = active
    if revisions and active:
        result["PromptAuthoringStatus"] = "prompt_ready"
    return result


def compile_manifest_requests(
    manifest: dict[str, Any],
    *,
    project_root: Path,
    compiler_version: int = COMPILER_VERSION,
    visual_ids: set[str] | None = None,
    refresh_style_catalog: bool = False,
    refresh_visual_intent_ids: set[str] | None = None,
    previous_catalog: dict[str, Any] | None = None,
) -> dict[str, Any]:
    working = copy.deepcopy(manifest)
    working["Version"] = max(3, int(working.get("Version", 1) or 1))
    entries = [entry for entry in working.get("Entries", []) if isinstance(entry, dict)]
    validate_manifest_uniqueness(entries)
    if refresh_style_catalog:
        working.pop("ArtStyleCatalog", None)
    catalog = build_catalog_snapshot(project_root, working, refresh=refresh_style_catalog)
    working["ArtStyleCatalog"] = catalog
    asset_sets = _asset_sets(working)
    working["AssetSets"] = asset_sets
    selected = visual_ids or set()
    compiled_requests: list[dict[str, Any]] = []
    previous_by_visual = _requests_by_visual(previous_catalog)
    report = {
        "CompilerVersion": compiler_version,
        "Ready": 0,
        "StyleResolutionRequired": 0,
        "Unsupported": 0,
        "Invalid": 0,
        "UnchangedPublished": 0,
        "PromptAuthoringRequired": 0,
        "PromptReady": 0,
        "Items": [],
    }

    for original in working.get("Entries", []):
        if not isinstance(original, dict):
            continue
        visual_id = str(original.get("VisualID", ""))
        if selected and visual_id not in selected:
            continue
        entry = original
        if refresh_visual_intent_ids and visual_id in refresh_visual_intent_ids:
            entry["VisualIntent"] = visual_intent_for(entry)
        if not isinstance(entry.get("StyleRef"), dict) or not entry.get("StyleRef"):
            entry["StyleRef"] = _style_ref_for_entry(entry)
        if not isinstance(entry.get("VisualIntent"), dict) or not entry.get("VisualIntent"):
            entry["VisualIntent"] = visual_intent_for(entry)
        elif entry.get("ProductionProfile") == "character_portrait_set":
            derived_intent = visual_intent_for(entry)
            if entry.get("PoseSpec"):
                for field in ("CompositionEN", "CompositionCN", "RequiredChanges", "PoseSpec"):
                    if derived_intent.get(field):
                        entry["VisualIntent"][field] = copy.deepcopy(derived_intent[field])
            for field in ("RequiredChanges", "ForbiddenElements"):
                if not entry["VisualIntent"].get(field) and derived_intent.get(field):
                    entry["VisualIntent"][field] = copy.deepcopy(derived_intent[field])
        try:
            request = compile_requirement_request(entry, catalog, asset_sets, compiler_version=compiler_version)
            previous_request = previous_by_visual.get(visual_id)
            request = carry_forward_prompt_revisions(request, previous_request)
            legacy = _legacy_prompt_variants(entry, previous_request)
            if legacy:
                request["LegacyPromptVariants"] = legacy
        except ValueError as exc:
            code = str(exc).split(":", 1)[0]
            request = {
                "RequestID": f"{visual_id}@invalid",
                "VisualID": visual_id,
                "ProductionProfile": entry.get("ProductionProfile", "standard_asset"),
                "RequirementFingerprint": "",
                "RequirementStatus": "style_resolution_required" if code.startswith("style_") else "invalid",
                "PromptAuthoringStatus": "prompt_invalid",
                "ActivePromptRevisionID": "",
                "PromptRevisions": [],
                "Error": str(exc),
            }
        request["PublicationStatus"] = str(entry.get("Status", "") or "")
        entry["CompiledRequest"] = {
            "RequestID": request["RequestID"],
            "RequirementFingerprint": request.get("RequirementFingerprint", ""),
            "RequirementStatus": request["RequirementStatus"],
            "PromptAuthoringStatus": request.get("PromptAuthoringStatus", "prompt_invalid"),
            "ActivePromptRevisionID": request.get("ActivePromptRevisionID", ""),
        }
        if request["RequirementStatus"] == "ready":
            report["Ready"] += 1
            if request.get("PromptAuthoringStatus") == "prompt_ready":
                report["PromptReady"] += 1
            elif request.get("PromptAuthoringStatus") == "prompt_authoring_required":
                report["PromptAuthoringRequired"] += 1
        elif request["RequirementStatus"] == "style_resolution_required":
            report["StyleResolutionRequired"] += 1
        else:
            report["Invalid"] += 1
        if str(entry.get("Status", "")) in {"approved", "registered", "validated"}:
            report["UnchangedPublished"] += 1
        report["Items"].append(
            {
                "VisualID": visual_id,
                "RequestID": request["RequestID"],
                "RequirementStatus": request["RequirementStatus"],
                "PromptAuthoringStatus": request.get("PromptAuthoringStatus", "prompt_invalid"),
                "Error": request.get("Error", ""),
            }
        )
        compiled_requests.append(request)

    selected_visual_ids = selected or {
        str(entry.get("VisualID", ""))
        for entry in entries
        if str(entry.get("VisualID", ""))
    }
    if selected and previous_catalog is None:
        raise CatalogIntegrityError("scoped_compile_previous_catalog_missing")
    previous_requests = (
        previous_catalog.get("Requests", [])
        if isinstance(previous_catalog, dict) and isinstance(previous_catalog.get("Requests", []), list)
        else []
    )
    requests = merge_request_catalog(previous_requests, compiled_requests, selected_visual_ids)
    summary = recompute_catalog_summary(requests)

    manifest_fingerprint = sha256_json(_input_manifest(working))
    request_catalog = {
        "Version": 2,
        "GeneratedAt": datetime.now().astimezone().isoformat(timespec="seconds"),
        "CompilerVersion": compiler_version,
        "ManifestFingerprint": manifest_fingerprint,
        "ArtStyleCatalogFingerprint": catalog["CatalogFingerprint"],
        "Requests": requests,
        "Summary": summary,
    }
    return {"Manifest": working, "Catalog": request_catalog, "Report": report}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Compile persisted multi-format P3 art generation requests.")
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--output-path", default=DEFAULT_OUTPUT)
    parser.add_argument("--report-path", default=DEFAULT_REPORT)
    parser.add_argument("--visual-id", action="append", default=[])
    parser.add_argument(
        "--refresh-visual-intent-id",
        action="append",
        default=[],
        help="Regenerate VisualIntent only for the listed VisualIDs.",
    )
    parser.add_argument(
        "--refresh-style-catalog",
        action="store_true",
        help="Discard the embedded catalog snapshot and rebuild it from the current source catalog.",
    )
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--overwrite", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    root = Path.cwd()
    manifest_path = (root / args.manifest_path).resolve()
    output_path = (root / args.output_path).resolve()
    report_path = (root / args.report_path).resolve()
    manifest = read_json(manifest_path)
    previous_catalog = read_json(output_path) if output_path.exists() else None
    visual_ids = {part.strip() for value in args.visual_id for part in value.split(",") if part.strip()}
    refresh_visual_intent_ids = {
        part.strip()
        for value in args.refresh_visual_intent_id
        for part in value.split(",")
        if part.strip()
    }
    if previous_catalog is not None and not visual_ids:
        previous_requests = previous_catalog.get("Requests", [])
        try:
            merge_request_catalog(previous_requests, [], set())
        except CatalogIntegrityError:
            previous_catalog = _canonical_previous_catalog_for_manifest(previous_catalog, manifest)
    result = compile_manifest_requests(
        manifest,
        project_root=root,
        visual_ids=visual_ids,
        refresh_style_catalog=args.refresh_style_catalog,
        refresh_visual_intent_ids=refresh_visual_intent_ids,
        previous_catalog=previous_catalog,
    )
    if args.dry_run:
        print(json.dumps(result["Report"], ensure_ascii=False, indent=2))
        print("No files changed.")
        return 0
    write_json(manifest_path, result["Manifest"])
    write_json(output_path, result["Catalog"])
    write_json(report_path, result["Report"])
    print(json.dumps(result["Report"], ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
