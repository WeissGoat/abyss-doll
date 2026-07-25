# -*- coding: utf-8 -*-
"""Plan and verify the Approved -> Unity -> Registry art handoff."""

from __future__ import annotations

import argparse
import hashlib
import json
import re
import time
import uuid
from datetime import datetime
from pathlib import Path
from typing import Any

from art_processing import IMAGE_EXTENSIONS
from art_workspace import normalize_entry_workspace_paths, workspace_path
from sync_approved_art import choose_source


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]
DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_INCOMING_ROOT = "UnityClient/Assets/Art/_IncomingAI"
DEFAULT_APPROVED_ROOT = "UnityClient/Assets/Art/Approved"
DEFAULT_EVIDENCE_ROOT = "UnityClient/Logs/P3ArtImport"
DEFAULT_GENERATED_PATHS = [
    "美术文档/_generated/可接入素材清单.json",
    "美术文档/_generated/程序接入交接清单.json",
    "美术文档/_generated/VisualAssetRegistry登记缺口清单.json",
]

REQUEST_SCHEMA = "p3-art-import-request@1"
PLAN_SCHEMA = "p3-art-approved-plan@1"
SYNC_SCHEMA = "p3-art-approved-sync@1"
UNITY_IMPORT_SCHEMA = "p3-art-unity-import@1"
REGISTRY_SCHEMA = "p3-art-registry-result@1"
CONSOLE_SCHEMA = "p3-art-console-delta@1"
SUMMARY_SCHEMA = "p3-art-import-summary@1"


class ArtImportError(RuntimeError):
    """Controlled failure that can be resumed from a named ArtImport phase."""


def now_iso() -> str:
    return datetime.now().astimezone().isoformat(timespec="seconds")


def read_json(path: Path) -> dict[str, Any]:
    data = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(data, dict):
        raise ArtImportError(f"JSON document must be an object: {path}")
    return data


def write_json_atomic(path: Path, data: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(f".{path.name}.{uuid.uuid4().hex}.tmp")
    temporary.write_text(
        json.dumps(data, ensure_ascii=False, indent=2) + "\n",
        encoding="utf-8",
    )
    temporary.replace(path)


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def sha256_json(data: Any) -> str:
    payload = json.dumps(data, ensure_ascii=False, sort_keys=True, separators=(",", ":"))
    return hashlib.sha256(payload.encode("utf-8")).hexdigest()


def parse_meta_guid(meta_path: Path) -> str:
    if not meta_path.exists():
        raise ArtImportError(f"Unity meta is missing: {meta_path}")
    match = re.search(
        r"(?m)^guid:\s*([0-9a-fA-F]{32})\s*$",
        meta_path.read_text(encoding="utf-8-sig"),
    )
    if match is None:
        raise ArtImportError(f"Unity meta has no valid guid: {meta_path}")
    return match.group(1).lower()


def project_root_for_manifest(manifest_path: Path) -> Path:
    resolved = manifest_path.resolve(strict=False)
    if resolved.parent.name == "_generated" and resolved.parent.parent.name == "美术文档":
        return resolved.parent.parent.parent
    return PROJECT_ROOT


def resolve_path(value: str | Path, project_root: Path) -> Path:
    path = Path(value)
    return path.resolve(strict=False) if path.is_absolute() else (project_root / path).resolve(strict=False)


def repo_relative(path: Path, project_root: Path) -> str:
    try:
        return path.resolve(strict=False).relative_to(project_root.resolve(strict=False)).as_posix()
    except ValueError as exc:
        raise ArtImportError(f"Path is outside project root: {path}") from exc


def unity_asset_path(path: Path, project_root: Path) -> str:
    assets_root = (project_root / "UnityClient" / "Assets").resolve(strict=False)
    try:
        return f"Assets/{path.resolve(strict=False).relative_to(assets_root).as_posix()}"
    except ValueError as exc:
        raise ArtImportError(f"Approved target is outside Unity Assets: {path}") from exc


def run_dir(evidence_root: Path, art_import_run_id: str) -> Path:
    if not re.fullmatch(r"[A-Za-z0-9][A-Za-z0-9_.-]{2,127}", art_import_run_id):
        raise ArtImportError(f"invalid ArtImportRunID: {art_import_run_id}")
    return evidence_root / art_import_run_id


def load_run_document(run_path: Path, name: str, schema: str) -> dict[str, Any]:
    path = run_path / name
    data = read_json(path)
    if data.get("schema") != schema:
        raise ArtImportError(f"invalid schema for {name}: expected {schema}")
    return data


def expected_importer(entry: dict[str, Any]) -> dict[str, Any]:
    spec = entry.get("Spec")
    source_spec = spec.get("SourceSpec") if isinstance(spec, dict) else None
    if not isinstance(source_spec, dict):
        raise ArtImportError(f"Manifest entry lacks Spec.SourceSpec: {entry.get('VisualID', '')}")
    try:
        width = int(source_spec["Width"])
        height = int(source_spec["Height"])
    except (KeyError, TypeError, ValueError) as exc:
        raise ArtImportError(f"Manifest entry has invalid SourceSpec dimensions: {entry.get('VisualID', '')}") from exc
    max_dimension = max(width, height, 512)
    max_texture_size = 1 << (max_dimension - 1).bit_length()
    return {
        "texture_type": "Sprite",
        "sprite_import_mode": "Single",
        "alpha_is_transparency": bool(source_spec.get("AlphaRequired", True)),
        "max_texture_size": min(max_texture_size, 8192),
        "filter_mode": "Bilinear",
        "mipmap_enabled": False,
    }


def expected_source_dimensions(entry: dict[str, Any]) -> dict[str, int]:
    spec = entry.get("Spec")
    source_spec = spec.get("SourceSpec") if isinstance(spec, dict) else None
    if not isinstance(source_spec, dict):
        raise ArtImportError(f"Manifest entry lacks Spec.SourceSpec: {entry.get('VisualID', '')}")
    try:
        return {"width": int(source_spec["Width"]), "height": int(source_spec["Height"])}
    except (KeyError, TypeError, ValueError) as exc:
        raise ArtImportError(f"Manifest entry has invalid SourceSpec dimensions: {entry.get('VisualID', '')}") from exc


def find_approved_basename_collisions(approved_root: Path) -> dict[str, list[str]]:
    by_visual_id: dict[str, list[str]] = {}
    if not approved_root.exists():
        return {}
    for path in sorted(approved_root.rglob("*"), key=lambda item: item.as_posix().lower()):
        if path.is_file() and path.suffix.lower() in IMAGE_EXTENSIONS:
            by_visual_id.setdefault(path.stem, []).append(path.as_posix())
    return {key: values for key, values in by_visual_id.items() if len(values) > 1}


def _entry_source(
    workspace: Path,
    entry: dict[str, Any],
    project_root: Path,
) -> tuple[Path | None, str]:
    source_entry = dict(entry)
    selected_value = source_entry.get("SelectedPath")
    if isinstance(selected_value, str) and selected_value:
        selected_path = resolve_path(selected_value, project_root)
        if selected_path.exists():
            source_entry["SelectedPath"] = str(selected_path)
    return choose_source(workspace, source_entry, candidate_batch=False)


def _target_details(
    entry: dict[str, Any],
    approved_root: Path,
    project_root: Path,
) -> dict[str, Any]:
    visual_id = str(entry.get("VisualID", "")).strip()
    output_value = entry.get("OutputPath")
    if not visual_id or not isinstance(output_value, str) or not output_value.strip():
        raise ArtImportError("Manifest entry requires VisualID and OutputPath")
    target = resolve_path(output_value, project_root)
    try:
        target.relative_to(approved_root.resolve(strict=False))
    except ValueError as exc:
        raise ArtImportError(f"OutputPath is outside Approved root: {visual_id}") from exc
    meta_path = target.with_name(target.name + ".meta")
    meta_exists = meta_path.exists()
    return {
        "output_path": repo_relative(target, project_root),
        "unity_asset_path": unity_asset_path(target, project_root),
        "target_exists": target.exists(),
        "target_sha256": sha256_file(target) if target.exists() else "",
        "target_meta_sha256": sha256_file(meta_path) if meta_exists else "",
        "target_meta_guid": parse_meta_guid(meta_path) if meta_exists else "",
        "action": "overwrite" if target.exists() else "create",
    }


def _load_entries(manifest_path: Path) -> list[dict[str, Any]]:
    data = read_json(manifest_path)
    entries = data.get("Entries")
    if not isinstance(entries, list):
        raise ArtImportError("Manifest Entries must be a list")
    return [normalize_entry_workspace_paths(entry) for entry in entries if isinstance(entry, dict)]


def create_plan(
    *,
    manifest_path: Path,
    incoming_root: Path,
    approved_root: Path,
    evidence_root: Path,
    art_import_run_id: str,
    visual_ids: list[str],
    mode: str,
    unity_instance: str,
    permissions: dict[str, bool],
) -> dict[str, Any]:
    if mode not in {"interactive", "auto"}:
        raise ArtImportError(f"unsupported mode: {mode}")
    requested_ids = sorted({str(value).strip() for value in visual_ids if str(value).strip()})
    if not requested_ids:
        raise ArtImportError("at least one VisualID is required")
    manifest_path = manifest_path.resolve(strict=False)
    incoming_root = incoming_root.resolve(strict=False)
    approved_root = approved_root.resolve(strict=False)
    evidence_root = evidence_root.resolve(strict=False)
    project_root = project_root_for_manifest(manifest_path)
    entries = _load_entries(manifest_path)
    by_visual_id: dict[str, list[dict[str, Any]]] = {}
    for entry in entries:
        visual_id = str(entry.get("VisualID", "")).strip()
        if visual_id:
            by_visual_id.setdefault(visual_id, []).append(entry)

    run_path = run_dir(evidence_root, art_import_run_id)
    collisions = find_approved_basename_collisions(approved_root)
    blocking_errors: list[dict[str, str]] = []
    if collisions:
        blocking_errors.append({
            "code": "blocked:approved_visualid_collision",
            "details": json.dumps(collisions, ensure_ascii=False, sort_keys=True),
        })

    items: list[dict[str, Any]] = []
    for visual_id in requested_ids:
        matches = by_visual_id.get(visual_id, [])
        if len(matches) != 1:
            raise ArtImportError(f"duplicate Manifest VisualID: {visual_id}")
        entry = matches[0]
        workspace = workspace_path(incoming_root, entry)
        source, source_kind = _entry_source(workspace, entry, project_root)
        if source is None or not source.exists():
            raise ArtImportError(f"selected source is missing: {visual_id}")
        target = _target_details(entry, approved_root, project_root)
        action = target["action"]
        item = {
            "visual_id": visual_id,
            "production_profile": entry.get("ProductionProfile", "standard_asset"),
            "selected_source": repo_relative(source, project_root),
            "selected_source_kind": source_kind,
            "selected_sha256": sha256_file(source),
            "manifest_entry_sha256": sha256_json(entry),
            "expected_importer": expected_importer(entry),
            "expected_source_dimensions": expected_source_dimensions(entry),
            **target,
            "authorization_required": not bool(permissions.get("allow_approved_sync")) or action != "noop",
        }
        items.append(item)

    request = {
        "schema": REQUEST_SCHEMA,
        "art_import_run_id": art_import_run_id,
        "mode": mode,
        "unity_instance": unity_instance,
        "visual_ids": requested_ids,
        "permissions": {
            "allow_approved_sync": bool(permissions.get("allow_approved_sync")),
            "allow_existing_target_overwrite": bool(permissions.get("allow_existing_target_overwrite")),
            "allow_new_approved_target": bool(permissions.get("allow_new_approved_target")),
        },
        "scope": {
            "include": ["approved_sync", "unity_import", "sprite_registry"],
            "exclude": ["runtime_binding", "playmode", "art_acceptance", "program_regression"],
        },
        "manifest_path": repo_relative(manifest_path, project_root),
        "incoming_root": repo_relative(incoming_root, project_root),
        "approved_root": repo_relative(approved_root, project_root),
        "evidence_root": repo_relative(evidence_root, project_root),
        "created_at": now_iso(),
    }
    request_fingerprint = sha256_json({
        "manifest_path": request["manifest_path"],
        "visual_ids": request["visual_ids"],
        "unity_instance": request["unity_instance"],
        "permissions": request["permissions"],
        "items": items,
    })
    request["request_fingerprint"] = request_fingerprint
    plan = {
        "schema": PLAN_SCHEMA,
        "art_import_run_id": art_import_run_id,
        "request_fingerprint": request_fingerprint,
        "project_root": project_root.as_posix(),
        "items": items,
        "approved_basename_collisions": collisions,
        "blocking_errors": blocking_errors,
        "created_at": now_iso(),
    }
    run_path.mkdir(parents=True, exist_ok=True)
    request_path = run_path / "request.json"
    plan_path = run_path / "approved-plan.json"
    if request_path.exists() and plan_path.exists():
        old_request = read_json(request_path)
        if old_request.get("request_fingerprint") != request_fingerprint:
            raise ArtImportError("ArtImportRunID already exists with a different request fingerprint")
    write_json_atomic(request_path, request)
    write_json_atomic(plan_path, plan)
    return plan


def _run_project_root(plan: dict[str, Any]) -> Path:
    value = plan.get("project_root")
    if not isinstance(value, str) or not value:
        raise ArtImportError("approved-plan.json lacks project_root")
    return Path(value).resolve(strict=False)


def _manifest_entry_by_visual_id(manifest_path: Path, visual_id: str) -> dict[str, Any]:
    matches = [entry for entry in _load_entries(manifest_path) if entry.get("VisualID") == visual_id]
    if len(matches) != 1:
        raise ArtImportError(f"duplicate Manifest VisualID: {visual_id}")
    return matches[0]


def verify_sync_authorization(
    run_path: Path,
    *,
    authorize_approved_sync: bool,
    allow_existing_target_overwrite: bool = False,
    allow_new_approved_target: bool = False,
) -> dict[str, Any]:
    request = load_run_document(run_path, "request.json", REQUEST_SCHEMA)
    plan = load_run_document(run_path, "approved-plan.json", PLAN_SCHEMA)
    if request.get("request_fingerprint") != plan.get("request_fingerprint"):
        raise ArtImportError("failed:approved_sync request fingerprint mismatch")
    blocking_errors = plan.get("blocking_errors")
    if isinstance(blocking_errors, list) and blocking_errors:
        first = blocking_errors[0]
        code = first.get("code") if isinstance(first, dict) else str(first)
        raise ArtImportError(str(code))
    permissions = request.get("permissions")
    if not isinstance(permissions, dict):
        raise ArtImportError("request permissions are missing")
    if not authorize_approved_sync:
        raise ArtImportError("blocked:approved_authorization_required")

    project_root = _run_project_root(plan)
    manifest_path = resolve_path(str(request.get("manifest_path", "")), project_root)
    incoming_root = resolve_path(str(request.get("incoming_root", DEFAULT_INCOMING_ROOT)), project_root)
    for planned in plan.get("items", []):
        if not isinstance(planned, dict):
            raise ArtImportError("approved-plan items must be objects")
        visual_id = str(planned.get("visual_id", ""))
        action = planned.get("action")
        allow_new = bool(permissions.get("allow_new_approved_target")) or allow_new_approved_target
        allow_overwrite = bool(permissions.get("allow_existing_target_overwrite")) or allow_existing_target_overwrite
        if action == "create" and not allow_new:
            raise ArtImportError(f"blocked: new Approved target requires permission: {visual_id}")
        if action == "overwrite" and not allow_overwrite:
            raise ArtImportError(f"blocked: existing Approved target overwrite requires permission: {visual_id}")

        entry = _manifest_entry_by_visual_id(manifest_path, visual_id)
        if sha256_json(entry) != planned.get("manifest_entry_sha256"):
            raise ArtImportError(f"failed:approved_sync Manifest entry changed: {visual_id}")
        workspace = workspace_path(incoming_root, entry)
        source, _ = _entry_source(workspace, entry, project_root)
        if source is None or not source.exists() or sha256_file(source) != planned.get("selected_sha256"):
            raise ArtImportError(f"failed:approved_sync selected source changed: {visual_id}")

        target = resolve_path(str(planned.get("output_path", "")), project_root)
        if target.exists():
            current_hash = sha256_file(target)
            allowed_hashes = {str(planned.get("target_sha256", "")), str(planned.get("selected_sha256", ""))}
            if current_hash not in allowed_hashes:
                raise ArtImportError(f"failed:approved_sync Approved target changed: {visual_id}")
        meta_path = target.with_name(target.name + ".meta")
        planned_meta_hash = str(planned.get("target_meta_sha256", ""))
        if planned_meta_hash:
            if not meta_path.exists() or sha256_file(meta_path) != planned_meta_hash:
                raise ArtImportError(f"failed:approved_sync meta changed: {visual_id}")
            if parse_meta_guid(meta_path) != planned.get("target_meta_guid"):
                raise ArtImportError(f"failed:approved_sync GUID changed: {visual_id}")
    return plan


def record_approved_sync(run_path: Path) -> dict[str, Any]:
    request = load_run_document(run_path, "request.json", REQUEST_SCHEMA)
    plan = load_run_document(run_path, "approved-plan.json", PLAN_SCHEMA)
    project_root = _run_project_root(plan)
    items: list[dict[str, Any]] = []
    for planned in plan.get("items", []):
        if not isinstance(planned, dict):
            raise ArtImportError("approved-plan items must be objects")
        visual_id = str(planned.get("visual_id", ""))
        target = resolve_path(str(planned.get("output_path", "")), project_root)
        if not target.exists():
            raise ArtImportError(f"failed:approved_sync target missing: {visual_id}")
        approved_sha = sha256_file(target)
        if approved_sha != planned.get("selected_sha256"):
            raise ArtImportError(f"failed:approved_sync hash mismatch: {visual_id}")
        meta_path = target.with_name(target.name + ".meta")
        planned_meta_hash = str(planned.get("target_meta_sha256", ""))
        meta_guid = ""
        meta_status = "awaiting_unity_import"
        if meta_path.exists():
            meta_guid = parse_meta_guid(meta_path)
            meta_status = "generated" if not planned_meta_hash else "preserved"
        if planned_meta_hash:
            if not meta_path.exists() or sha256_file(meta_path) != planned_meta_hash:
                raise ArtImportError(f"failed:approved_sync meta changed: {visual_id}")
            if meta_guid != planned.get("target_meta_guid"):
                raise ArtImportError(f"failed:approved_sync GUID changed: {visual_id}")
        items.append({
            "visual_id": visual_id,
            "output_path": planned["output_path"],
            "unity_asset_path": planned["unity_asset_path"],
            "approved_sha256": approved_sha,
            "meta_sha256": sha256_file(meta_path) if meta_path.exists() else "",
            "meta_guid": meta_guid,
            "meta_status": meta_status,
            "status": "passed",
        })
    result = {
        "schema": SYNC_SCHEMA,
        "art_import_run_id": request["art_import_run_id"],
        "unity_instance": request["unity_instance"],
        "request_fingerprint": request["request_fingerprint"],
        "items": items,
        "completed_at": now_iso(),
    }
    write_json_atomic(run_path / "approved-sync.json", result)
    return result


def _parse_time(value: Any, label: str) -> datetime:
    if not isinstance(value, str) or not value:
        raise ArtImportError(f"{label} timestamp is missing")
    try:
        parsed = datetime.fromisoformat(value)
    except ValueError as exc:
        raise ArtImportError(f"{label} timestamp is invalid: {value}") from exc
    if parsed.tzinfo is None:
        raise ArtImportError(f"{label} timestamp must include a timezone")
    return parsed


def _document_items(document: dict[str, Any], expected_ids: set[str], label: str) -> dict[str, dict[str, Any]]:
    raw_items = document.get("items")
    if not isinstance(raw_items, list):
        raise ArtImportError(f"{label} items must be a list")
    items: dict[str, dict[str, Any]] = {}
    for item in raw_items:
        if not isinstance(item, dict):
            raise ArtImportError(f"{label} item must be an object")
        visual_id = str(item.get("visual_id", ""))
        if not visual_id or visual_id in items:
            raise ArtImportError(f"{label} contains a missing or duplicate VisualID")
        items[visual_id] = item
    if set(items) != expected_ids:
        raise ArtImportError(f"{label} VisualID set does not match the request")
    return items


def _validate_document_identity(
    document: dict[str, Any],
    request: dict[str, Any],
    label: str,
    minimum_time: datetime,
) -> None:
    if document.get("art_import_run_id") != request.get("art_import_run_id"):
        raise ArtImportError(f"{label} ArtImportRunID mismatch")
    if document.get("unity_instance") != request.get("unity_instance"):
        raise ArtImportError(f"{label} Unity instance mismatch")
    if _parse_time(document.get("observed_at"), label) < minimum_time:
        raise ArtImportError(f"{label} evidence is older than approved-sync")


def _validate_import_item(
    planned: dict[str, Any],
    imported: dict[str, Any],
    target_guid: str,
) -> None:
    visual_id = str(planned["visual_id"])
    if imported.get("status") != "passed":
        raise ArtImportError(f"failed:unity_import_mismatch status: {visual_id}")
    if imported.get("asset_path") != planned.get("unity_asset_path"):
        raise ArtImportError(f"failed:unity_import_mismatch path: {visual_id}")
    if str(imported.get("asset_database_guid", "")).lower() != target_guid:
        raise ArtImportError(f"failed:unity_import_mismatch GUID: {visual_id}")
    if imported.get("main_asset_type") != "UnityEngine.Texture2D" or imported.get("sprite_loaded") is not True:
        raise ArtImportError(f"failed:unity_import_mismatch asset type: {visual_id}")
    dimensions = planned.get("expected_source_dimensions")
    if not isinstance(dimensions, dict) or imported.get("source_width") != dimensions.get("width") or imported.get("source_height") != dimensions.get("height"):
        raise ArtImportError(f"failed:unity_import_mismatch source dimensions: {visual_id}")
    expected = planned.get("expected_importer")
    actual = imported.get("importer")
    if not isinstance(expected, dict) or not isinstance(actual, dict):
        raise ArtImportError(f"failed:unity_import_mismatch importer missing: {visual_id}")
    for key, value in expected.items():
        if actual.get(key) != value:
            raise ArtImportError(f"failed:unity_import_mismatch importer.{key}: {visual_id}")


def _validate_registry_item(
    planned: dict[str, Any],
    registered: dict[str, Any],
    target_guid: str,
) -> None:
    visual_id = str(planned["visual_id"])
    if registered.get("match_count") != 1:
        raise ArtImportError(f"failed:registry_duplicate: {visual_id}")
    if registered.get("try_get_entry") is not True:
        raise ArtImportError(f"failed:registry_missing: {visual_id}")
    if registered.get("sprite_loaded") is not True or registered.get("status") != "passed":
        raise ArtImportError(f"failed:registry_missing: {visual_id}")
    if registered.get("sprite_asset_path") != planned.get("unity_asset_path"):
        raise ArtImportError(f"failed:registry_asset_mismatch path: {visual_id}")
    if str(registered.get("sprite_guid", "")).lower() != target_guid:
        raise ArtImportError(f"failed:registry_asset_mismatch GUID: {visual_id}")


def mark_registry_status(manifest: dict[str, Any], visual_ids: set[str]) -> bool:
    entries = manifest.get("Entries")
    if not isinstance(entries, list):
        raise ArtImportError("Manifest Entries must be a list")
    counts = {visual_id: 0 for visual_id in visual_ids}
    changed = False
    for entry in entries:
        if not isinstance(entry, dict):
            continue
        visual_id = entry.get("VisualID")
        if visual_id not in visual_ids:
            continue
        counts[str(visual_id)] += 1
        if entry.get("Status") != "approved":
            raise ArtImportError(f"Manifest Status must be approved before registration: {visual_id}")
        if entry.get("RegistryStatus") != "registered":
            entry["RegistryStatus"] = "registered"
            changed = True
    missing = [visual_id for visual_id, count in counts.items() if count != 1]
    if missing:
        raise ArtImportError(f"Manifest VisualID set is missing or duplicated: {', '.join(sorted(missing))}")
    return changed


def stage_finalize(run_path: Path, *, manifest_path: Path) -> dict[str, Any]:
    request = load_run_document(run_path, "request.json", REQUEST_SCHEMA)
    plan = load_run_document(run_path, "approved-plan.json", PLAN_SCHEMA)
    sync = load_run_document(run_path, "approved-sync.json", SYNC_SCHEMA)
    unity_import = load_run_document(run_path, "unity-import.json", UNITY_IMPORT_SCHEMA)
    registry = load_run_document(run_path, "registry-result.json", REGISTRY_SCHEMA)
    console = load_run_document(run_path, "console-delta.json", CONSOLE_SCHEMA)
    if request.get("request_fingerprint") != plan.get("request_fingerprint") or sync.get("request_fingerprint") != request.get("request_fingerprint"):
        raise ArtImportError("Finalize request fingerprint mismatch")
    minimum_time = _parse_time(sync.get("completed_at"), "approved-sync")
    for document, label in (
        (unity_import, "unity-import"),
        (registry, "registry-result"),
        (console, "console-delta"),
    ):
        _validate_document_identity(document, request, label, minimum_time)

    expected_ids = set(str(value) for value in request.get("visual_ids", []))
    planned_items = _document_items(plan, expected_ids, "approved-plan")
    sync_items = _document_items(sync, expected_ids, "approved-sync")
    import_items = _document_items(unity_import, expected_ids, "unity-import")
    registry_items = _document_items(registry, expected_ids, "registry-result")
    if registry.get("registry_asset_path") != "Assets/Resources/VisualAssetRegistry.asset":
        raise ArtImportError("registry-result points to an unexpected Registry asset")
    if console.get("status") != "passed":
        raise ArtImportError("failed:console_delta status")
    errors = console.get("errors")
    target_warnings = console.get("target_warnings")
    if not isinstance(errors, list) or errors:
        raise ArtImportError("failed:console_delta errors")
    if not isinstance(target_warnings, list) or target_warnings:
        raise ArtImportError("failed:console_delta target warnings")

    project_root = _run_project_root(plan)
    for visual_id in sorted(expected_ids):
        planned = planned_items[visual_id]
        synced = sync_items[visual_id]
        target = resolve_path(str(planned.get("output_path", "")), project_root)
        meta_path = target.with_name(target.name + ".meta")
        if not target.exists() or not meta_path.exists():
            raise ArtImportError(f"failed:unity_import_mismatch asset or meta missing: {visual_id}")
        if sha256_file(target) != synced.get("approved_sha256"):
            raise ArtImportError(f"failed:unity_import_mismatch Approved hash changed: {visual_id}")
        target_guid = parse_meta_guid(meta_path)
        _validate_import_item(planned, import_items[visual_id], target_guid)
        _validate_registry_item(planned, registry_items[visual_id], target_guid)

    manifest_path = manifest_path.resolve(strict=False)
    manifest = read_json(manifest_path)
    manifest_before = sha256_file(manifest_path)
    manifest_changed = mark_registry_status(manifest, expected_ids)
    if manifest_changed:
        write_json_atomic(manifest_path, manifest)
    stage = {
        "schema": "p3-art-import-finalize-stage@1",
        "art_import_run_id": request["art_import_run_id"],
        "unity_instance": request["unity_instance"],
        "visual_ids": sorted(expected_ids),
        "manifest_path": repo_relative(manifest_path, project_root),
        "manifest_before_sha256": manifest_before,
        "manifest_after_sha256": sha256_file(manifest_path),
        "manifest_changed": manifest_changed,
        "staged_at": now_iso(),
        "staged_at_unix_ns": time.time_ns(),
        "status": "passed",
    }
    write_json_atomic(run_path / "finalize-stage.json", stage)
    return stage


def complete_finalize(run_path: Path, *, generated_paths: list[Path]) -> dict[str, Any]:
    request = load_run_document(run_path, "request.json", REQUEST_SCHEMA)
    stage = load_run_document(run_path, "finalize-stage.json", "p3-art-import-finalize-stage@1")
    staged_at_unix_ns = int(stage.get("staged_at_unix_ns", 0))
    outputs: list[dict[str, Any]] = []
    for path in generated_paths:
        resolved = path.resolve(strict=False)
        if not resolved.exists():
            raise ArtImportError(f"generated writeback is missing: {resolved}")
        if resolved.stat().st_mtime_ns < staged_at_unix_ns:
            raise ArtImportError(f"generated writeback is stale: {resolved}")
        outputs.append({
            "path": resolved.as_posix(),
            "sha256": sha256_file(resolved),
            "modified_at_unix_ns": resolved.stat().st_mtime_ns,
        })
    writeback = {
        "schema": "p3-art-generated-writeback@1",
        "art_import_run_id": request["art_import_run_id"],
        "outputs": outputs,
        "completed_at": now_iso(),
        "status": "passed",
    }
    write_json_atomic(run_path / "generated-writeback.json", writeback)
    summary = {
        "schema": SUMMARY_SCHEMA,
        "art_import_run_id": request["art_import_run_id"],
        "unity_instance": request["unity_instance"],
        "visual_ids": request["visual_ids"],
        "claim": "registered",
        "scope": request["scope"],
        "completed_at": now_iso(),
        "status": "passed",
    }
    write_json_atomic(run_path / "summary.json", summary)
    return summary


def build_parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description=__doc__)
    subparsers = parser.add_subparsers(dest="command", required=True)
    plan_parser = subparsers.add_parser("plan")
    plan_parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    plan_parser.add_argument("--incoming-root", default=DEFAULT_INCOMING_ROOT)
    plan_parser.add_argument("--approved-root", default=DEFAULT_APPROVED_ROOT)
    plan_parser.add_argument("--evidence-root", default=DEFAULT_EVIDENCE_ROOT)
    plan_parser.add_argument("--art-import-run-id", required=True)
    plan_parser.add_argument("--visual-id", action="append", required=True)
    plan_parser.add_argument("--mode", choices=["interactive", "auto"], default="interactive")
    plan_parser.add_argument("--unity-instance", default="")
    plan_parser.add_argument("--allow-approved-sync", action="store_true")
    plan_parser.add_argument("--allow-existing-target-overwrite", action="store_true")
    plan_parser.add_argument("--allow-new-approved-target", action="store_true")
    verify_parser = subparsers.add_parser("verify-sync")
    verify_parser.add_argument("--evidence-root", default=DEFAULT_EVIDENCE_ROOT)
    verify_parser.add_argument("--art-import-run-id", required=True)
    verify_parser.add_argument("--authorize-approved-sync", action="store_true")
    verify_parser.add_argument("--allow-existing-target-overwrite", action="store_true")
    verify_parser.add_argument("--allow-new-approved-target", action="store_true")
    record_parser = subparsers.add_parser("record-sync")
    record_parser.add_argument("--evidence-root", default=DEFAULT_EVIDENCE_ROOT)
    record_parser.add_argument("--art-import-run-id", required=True)
    stage_parser = subparsers.add_parser("stage-finalize")
    stage_parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    stage_parser.add_argument("--evidence-root", default=DEFAULT_EVIDENCE_ROOT)
    stage_parser.add_argument("--art-import-run-id", required=True)
    complete_parser = subparsers.add_parser("complete-finalize")
    complete_parser.add_argument("--evidence-root", default=DEFAULT_EVIDENCE_ROOT)
    complete_parser.add_argument("--art-import-run-id", required=True)
    complete_parser.add_argument("--generated-path", action="append")
    return parser


def main(argv: list[str] | None = None) -> int:
    args = build_parser().parse_args(argv)
    if args.command == "plan":
        plan = create_plan(
            manifest_path=resolve_path(args.manifest_path, PROJECT_ROOT),
            incoming_root=resolve_path(args.incoming_root, PROJECT_ROOT),
            approved_root=resolve_path(args.approved_root, PROJECT_ROOT),
            evidence_root=resolve_path(args.evidence_root, PROJECT_ROOT),
            art_import_run_id=args.art_import_run_id,
            visual_ids=args.visual_id,
            mode=args.mode,
            unity_instance=args.unity_instance,
            permissions={
                "allow_approved_sync": args.allow_approved_sync,
                "allow_existing_target_overwrite": args.allow_existing_target_overwrite,
                "allow_new_approved_target": args.allow_new_approved_target,
            },
        )
        print(json.dumps(plan, ensure_ascii=False, indent=2))
        return 0
    if args.command == "verify-sync":
        result = verify_sync_authorization(
            run_dir(resolve_path(args.evidence_root, PROJECT_ROOT), args.art_import_run_id),
            authorize_approved_sync=args.authorize_approved_sync,
            allow_existing_target_overwrite=args.allow_existing_target_overwrite,
            allow_new_approved_target=args.allow_new_approved_target,
        )
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return 0
    if args.command == "record-sync":
        result = record_approved_sync(
            run_dir(resolve_path(args.evidence_root, PROJECT_ROOT), args.art_import_run_id)
        )
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return 0
    if args.command == "stage-finalize":
        result = stage_finalize(
            run_dir(resolve_path(args.evidence_root, PROJECT_ROOT), args.art_import_run_id),
            manifest_path=resolve_path(args.manifest_path, PROJECT_ROOT),
        )
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return 0
    if args.command == "complete-finalize":
        result = complete_finalize(
            run_dir(resolve_path(args.evidence_root, PROJECT_ROOT), args.art_import_run_id),
            generated_paths=[
                resolve_path(value, PROJECT_ROOT)
                for value in (args.generated_path or DEFAULT_GENERATED_PATHS)
            ],
        )
        print(json.dumps(result, ensure_ascii=False, indent=2))
        return 0
    raise ArtImportError(f"unsupported command: {args.command}")


if __name__ == "__main__":
    raise SystemExit(main())
