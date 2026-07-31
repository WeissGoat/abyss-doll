# -*- coding: utf-8 -*-
"""Plan and optionally execute a bounded P3 character portrait AssetSet."""

from __future__ import annotations

import argparse
import copy
import hashlib
import json
import shutil
import subprocess
import sys
from datetime import datetime
from pathlib import Path
from typing import Any

from art_prompt_revision import DANBOORU_TAGS_FORMAT, NATURAL_LANGUAGE_FORMAT, select_prompt_variant
from register_art_processing_round import register_processing_round
from select_art_candidate import select_art_candidate
from prepare_art_background_candidate import prepare_background_candidate
from portrait_reference_resolver import resolve_portrait_references

SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]
DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_REQUEST_CATALOG = "美术文档/_generated/art_generation_requests.json"
DEFAULT_LOG_ROOT = "UnityClient/Logs/P3ArtProduction"
RUN_SCHEMA = "portrait_set_run_v1"
ITEM_STAGES = {
    "source_audit",
    "prompt_resolution",
    "generation",
    "output_contract_audit",
    "processing_decision",
    "technical_gate",
    "processed_registration",
    "visual_review",
    "guarded_selection",
    "complete",
}
TERMINAL_RESULTS = {"selected", "kept_existing"}
FAILURE_RESULTS = {"generation_failed", "generation_stale", "technical_failed", "prompt_stale", "reference_stale", "blocked_by_dependency"}


def read_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def write_json_atomic(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(f".{path.name}.tmp")
    temporary.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    temporary.replace(path)


def payload_fingerprint(payload: Any) -> str:
    encoded = json.dumps(payload, ensure_ascii=False, sort_keys=True, separators=(",", ":"))
    return hashlib.sha256(encoded.encode("utf-8")).hexdigest()


def file_sha256(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def reference_fingerprint(references: list[dict[str, Any]]) -> str:
    return payload_fingerprint(
        [
            {
                "AssetID": str(reference.get("AssetID", "")),
                "Role": str(reference.get("Role", "")),
                "Path": str(reference.get("Path", "")),
                "SHA256": str(reference.get("SHA256", "")).lower(),
            }
            for reference in references
        ]
    )


def resolve_project_path(value: str) -> Path:
    path = Path(value)
    return path if path.is_absolute() else PROJECT_ROOT / path


def load_asset_set(manifest: dict[str, Any], asset_set_id: str) -> dict[str, Any]:
    asset_sets = manifest.get("AssetSets") if isinstance(manifest.get("AssetSets"), dict) else {}
    asset_set = asset_sets.get(asset_set_id)
    if not isinstance(asset_set, dict):
        raise ValueError(f"asset_set_missing:{asset_set_id}")
    if asset_set.get("ProductionProfile") != "character_portrait_set":
        raise ValueError(f"route_mismatch:{asset_set_id}")
    if not asset_set.get("IdentitySources") or not asset_set.get("IdentityLocks"):
        raise ValueError(f"identity_contract_missing:{asset_set_id}")
    return asset_set


def _source_asset_id(source: Any) -> str:
    if not isinstance(source, dict):
        return ""
    return str(source.get("AssetID") or source.get("asset_id") or "")


def _role_rank(entry: dict[str, Any]) -> int:
    role = str(entry.get("SetRole", ""))
    if "master" in role:
        return 0
    if "anchor" in role:
        return 1
    return 2


def order_portrait_members(
    asset_set: dict[str, Any],
    entries: list[dict[str, Any]],
) -> list[dict[str, Any]]:
    del asset_set  # The validated set contract is consumed by the caller.
    by_asset: dict[str, dict[str, Any]] = {}
    by_visual: set[str] = set()
    for entry in entries:
        asset_id = str(entry.get("AssetID", ""))
        visual_id = str(entry.get("VisualID", ""))
        if not asset_id or asset_id in by_asset:
            raise ValueError(f"asset_set_duplicate_member:{asset_id or visual_id}")
        if not visual_id or visual_id in by_visual:
            raise ValueError(f"asset_set_duplicate_visual:{visual_id}")
        if not entry.get("SetRole"):
            raise ValueError(f"asset_set_role_missing:{visual_id}")
        by_asset[asset_id] = entry
        by_visual.add(visual_id)

    dependencies: dict[str, set[str]] = {asset_id: set() for asset_id in by_asset}
    for asset_id, entry in by_asset.items():
        for source in entry.get("SourceAssets", []):
            source_id = _source_asset_id(source)
            if source_id and source_id not in by_asset:
                raise ValueError(f"asset_set_source_missing:{asset_id}:{source_id}")
            if source_id:
                dependencies[asset_id].add(source_id)

    ordered: list[dict[str, Any]] = []
    remaining = set(by_asset)
    while remaining:
        ready = sorted(
            [asset_id for asset_id in remaining if not (dependencies[asset_id] & remaining)],
            key=lambda asset_id: (_role_rank(by_asset[asset_id]), str(by_asset[asset_id].get("VisualID", ""))),
        )
        if not ready:
            raise ValueError("asset_set_dependency_cycle")
        for asset_id in ready:
            ordered.append(by_asset[asset_id])
            remaining.remove(asset_id)
    return ordered


def build_portrait_set_plan(
    manifest: dict[str, Any],
    request_catalog: dict[str, Any],
    asset_set_id: str,
    *,
    prompt_format: str = "auto",
    provider: str = "",
    visual_ids: set[str] | None = None,
    project_root: Path = PROJECT_ROOT,
) -> dict[str, Any]:
    asset_set = load_asset_set(manifest, asset_set_id)
    members = [
        entry
        for entry in manifest.get("Entries", [])
        if isinstance(entry, dict)
        and entry.get("AssetSetID") == asset_set_id
        and entry.get("ProductionProfile") == "character_portrait_set"
    ]
    ordered = order_portrait_members(asset_set, members)
    request_map = {
        str(request.get("RequestID", "")): request
        for request in request_catalog.get("Requests", [])
        if isinstance(request, dict) and request.get("RequestID")
    }
    selected = visual_ids or set()
    items: list[dict[str, Any]] = []
    errors: list[str] = []
    for order, entry in enumerate(ordered, start=1):
        visual_id = str(entry.get("VisualID", ""))
        if selected and visual_id not in selected:
            continue
        pointer = entry.get("CompiledRequest") if isinstance(entry.get("CompiledRequest"), dict) else {}
        request = request_map.get(str(pointer.get("RequestID", "")))
        if request is None:
            errors.append(f"compiled_request_missing:{visual_id}")
            continue
        if request.get("VisualID") != visual_id:
            errors.append(f"compiled_request_visual_id_mismatch:{visual_id}")
            continue
        if request.get("RequirementFingerprint") != pointer.get("RequirementFingerprint"):
            errors.append(f"compiled_request_fingerprint_mismatch:{visual_id}")
            continue
        if request.get("PromptAuthoringStatus") != pointer.get("PromptAuthoringStatus"):
            errors.append(f"prompt_authoring_status_mismatch:{visual_id}")
            continue
        if str(request.get("ActivePromptRevisionID", "") or "") != str(pointer.get("ActivePromptRevisionID", "") or ""):
            errors.append(f"prompt_revision_pointer_mismatch:{visual_id}")
            continue
        try:
            revision, selected_format, selected_variant = select_prompt_variant(
                request,
                prompt_format=prompt_format,
                provider=provider,
            )
        except ValueError as exc:
            errors.append(str(exc))
            continue
        try:
            resolved_references = resolve_portrait_references(manifest, entry, project_root)
        except ValueError as exc:
            errors.append(str(exc))
            continue
        items.append(
            {
                "Order": order,
                "AssetID": entry.get("AssetID", ""),
                "VisualID": visual_id,
                "SetRole": entry.get("SetRole", ""),
                "SourceAssets": entry.get("SourceAssets", []),
                "ReferenceAssets": entry.get("SourceAssets", []),
                "ResolvedReferenceAssets": resolved_references,
                "RequestID": request["RequestID"],
                "RequirementFingerprint": request["RequirementFingerprint"],
                "PromptRevisionID": revision["PromptRevisionID"],
                "PromptRevisionFingerprint": revision.get("RevisionFingerprint", ""),
                "PromptFormat": selected_format,
                "PromptRevisionSnapshot": copy.deepcopy(revision),
                "PromptVariantSnapshot": copy.deepcopy(selected_variant),
                "PreservationContract": request.get("PreservationContract", {}),
                "Workspace": f"UnityClient/Assets/Art/_IncomingAI/character_portraits/{visual_id}",
                "Status": entry.get("Status", ""),
            }
        )
    return {
        "AssetSetID": asset_set_id,
        "ProductionProfile": "character_portrait_set",
        "State": "ready" if items and not errors else "decision_required",
        "IdentitySources": asset_set.get("IdentitySources", []),
        "IdentityLocks": asset_set.get("IdentityLocks", []),
        "Items": items,
        "Errors": errors,
    }


def reference_cli_arguments(references: list[dict[str, Any]]) -> list[str]:
    arguments: list[str] = []
    for reference in references:
        path = str(reference.get("Path", "") or "")
        sha256 = str(reference.get("SHA256", "") or "")
        role = str(reference.get("Role", "") or "")
        if not path or not sha256:
            raise ValueError("resolved_reference_evidence_incomplete")
        arguments.extend(
            [
                "--reference-image",
                path,
                "--reference-image-sha256",
                sha256,
                "--reference-image-role",
                role,
            ]
        )
    return arguments


def _item_dependencies(item: dict[str, Any], by_asset: dict[str, dict[str, Any]]) -> list[str]:
    dependencies: list[str] = []
    for source in item.get("SourceAssets", []):
        source_id = _source_asset_id(source)
        if source_id and source_id in by_asset:
            dependencies.append(str(by_asset[source_id].get("VisualID", "")))
    return dependencies


def _generation_snapshot_is_current(item: dict[str, Any], snapshot: dict[str, Any], run_id: str) -> tuple[bool, str]:
    if snapshot.get("VisualID") != item.get("VisualID") or snapshot.get("BatchID") != run_id:
        return False, "generation_snapshot_mismatch"
    for key in ("RequirementFingerprint", "PromptRevisionID", "PromptRevisionFingerprint", "PromptFormat"):
        if str(snapshot.get(key, "")) != str(item.get(key, "")):
            return False, "prompt_stale"
    expected_references = reference_fingerprint(item.get("ResolvedReferenceAssets", []))
    actual_references = reference_fingerprint(snapshot.get("ReferenceImages", []))
    if expected_references != actual_references:
        return False, "reference_stale"
    outputs = snapshot.get("Outputs")
    if not isinstance(outputs, list) or not outputs:
        return False, "generation_outputs_missing"
    return True, ""


def _validate_generation_evidence(
    *,
    item: dict[str, Any],
    generation: dict[str, Any],
    project_root: Path,
    run_id: str,
) -> dict[str, Any]:
    current, reason = _generation_snapshot_is_current(item, generation, run_id)
    if not current:
        raise ValueError(reason)
    output_hashes: list[str] = []
    for output in generation["Outputs"]:
        if not isinstance(output, dict):
            raise ValueError("generation_output_record_invalid")
        value = str(output.get("RepoPath", "") or "")
        if not value:
            raise ValueError("generation_output_path_missing")
        path = Path(value)
        path = path if path.is_absolute() else project_root / path
        if not path.is_file():
            raise ValueError(f"generation_output_missing:{value}")
        try:
            from PIL import Image

            with Image.open(path) as image:
                image.load()
        except Exception as exc:  # pragma: no cover - provider evidence is environment-dependent
            raise ValueError(f"generation_output_undecodable:{value}") from exc
        output_hashes.append(file_sha256(path))
    return {
        "BatchID": generation["BatchID"],
        "EvidencePath": "generation.json",
        "GenerationSHA256": payload_fingerprint(generation),
        "OutputSHA256": output_hashes,
        "ReferenceFingerprint": reference_fingerprint(generation.get("ReferenceImages", [])),
    }


def _generation_record_matches(item: dict[str, Any], evidence: dict[str, Any]) -> bool:
    previous = item.get("Generation") if isinstance(item.get("Generation"), dict) else {}
    expected = [str(value).lower() for value in previous.get("OutputSHA256", [])]
    actual = [str(value).lower() for value in evidence.get("OutputSHA256", [])]
    return not expected or expected == actual


def _write_generation_snapshot(
    *,
    generation_dir: Path,
    visual_id: str,
    generation: dict[str, Any],
    previous: dict[str, Any] | None,
    project_root: Path,
) -> Path:
    generation_dir.mkdir(parents=True, exist_ok=True)
    content_fingerprint = payload_fingerprint(generation)
    previous_path = str(previous.get("SnapshotPath", "") or "") if isinstance(previous, dict) else ""
    if previous_path:
        candidate = Path(previous_path)
        if not candidate.is_absolute():
            candidate = project_root / previous_path
        if candidate.is_file():
            try:
                if payload_fingerprint(read_json(candidate)) == content_fingerprint:
                    return candidate
            except (OSError, ValueError, json.JSONDecodeError):
                pass
    base = generation_dir / f"{visual_id}.json"
    if not base.exists():
        write_json_atomic(base, generation)
        return base
    try:
        if payload_fingerprint(read_json(base)) == content_fingerprint:
            return base
    except (OSError, ValueError, json.JSONDecodeError):
        pass
    attempt = 2
    while True:
        candidate = generation_dir / f"{visual_id}.attempt-{attempt}.json"
        if not candidate.exists():
            write_json_atomic(candidate, generation)
            return candidate
        attempt += 1


def _load_decisions(path: Path | None) -> dict[str, dict[str, Any]]:
    if path is None or not path.exists():
        return {}
    payload = read_json(path)
    values = payload.get("Items") if isinstance(payload, dict) else None
    if not isinstance(values, list):
        raise ValueError("processing_decisions_items_invalid")
    result: dict[str, dict[str, Any]] = {}
    allowed = {"already_usable", "background_processing_required", "manual_edit_required", "regenerate_required"}
    for item in values:
        if not isinstance(item, dict) or not str(item.get("VisualID", "")):
            raise ValueError("processing_decision_item_invalid")
        visual_id = str(item["VisualID"])
        if visual_id in result:
            raise ValueError(f"processing_decision_duplicate:{visual_id}")
        action = str(item.get("Action", "") or "")
        if action not in allowed:
            raise ValueError(f"processing_decision_action_invalid:{visual_id}:{action}")
        result[visual_id] = item
    return result


def _load_visual_review(path: Path | None) -> dict[str, dict[str, Any]]:
    if path is None or not path.exists():
        return {}
    payload = read_json(path)
    values = payload.get("Items") if isinstance(payload, dict) else None
    if not isinstance(values, list):
        raise ValueError("visual_review_items_invalid")
    result: dict[str, dict[str, Any]] = {}
    for item in values:
        if isinstance(item, dict) and str(item.get("VisualID", "")):
            result[str(item["VisualID"])] = item
    return result


def _entry_for_visual(manifest_path: Path, visual_id: str) -> dict[str, Any]:
    manifest = read_json(manifest_path)
    entries = [entry for entry in manifest.get("Entries", []) if isinstance(entry, dict) and entry.get("VisualID") == visual_id]
    if len(entries) != 1:
        raise ValueError(f"manifest_visual_id_invalid:{visual_id}")
    return entries[0]


def _make_processing_staging(
    *,
    item: dict[str, Any],
    decision: dict[str, Any],
    generation: dict[str, Any],
    run_dir: Path,
    manifest_path: Path,
    project_root: Path,
    visual_review: dict[str, Any] | None,
) -> Path:
    visual_id = str(item["VisualID"])
    staging = run_dir / "processing" / visual_id
    if staging.exists() and any(staging.iterdir()):
        return staging
    staging.mkdir(parents=True, exist_ok=True)
    outputs = generation.get("Outputs", [])
    if not outputs:
        raise ValueError(f"generation_outputs_missing:{visual_id}")
    source_value = str(outputs[0].get("RepoPath", "") or "")
    source = Path(source_value)
    source = source if source.is_absolute() else project_root / source
    if not source.is_file():
        raise ValueError(f"generation_output_missing:{visual_id}")
    decision_input = str(decision.get("InputPath", "") or "")
    if decision_input:
        expected_input = Path(decision_input)
        expected_input = expected_input if expected_input.is_absolute() else project_root / expected_input
        if expected_input.resolve() != source.resolve():
            raise ValueError(f"processing_input_path_mismatch:{visual_id}")
    input_hash = file_sha256(source)
    expected_hash = str(decision.get("InputSHA256", "") or "").lower()
    if expected_hash and expected_hash != input_hash.lower():
        raise ValueError(f"processing_input_hash_mismatch:{visual_id}")

    action = str(decision.get("Action", ""))
    candidate = staging / "candidate.png"
    if action == "background_processing_required":
        method = str(decision.get("Method", "") or "")
        mask_value = str(decision.get("MaskPath", "") or "")
        prepare_background_candidate(
            input_path=source,
            staging_dir=staging,
            method=method,
            expected_input_sha256=input_hash,
            mask_path=(project_root / mask_value if mask_value and not Path(mask_value).is_absolute() else Path(mask_value) if mask_value else None),
        )
    elif action == "already_usable":
        shutil.copy2(source, candidate)
    else:
        raise ValueError(f"processing_decision_not_registrable:{visual_id}:{action}")

    from register_art_processing_round import recalculate_candidate_review

    entry = _entry_for_visual(manifest_path, visual_id)
    technical = recalculate_candidate_review(entry, candidate)
    candidate_status = str(technical.get("Status", "failed"))
    requested_status = "passed" if candidate_status == "warning" and visual_review is not None else candidate_status
    state = "passed" if requested_status == "passed" else "decision_required" if requested_status == "warning" else "failed"
    (staging / "process_report.json").write_text(
        json.dumps(
            {
                "VisualID": visual_id,
                "Action": action,
                "Input": source.as_posix(),
                "InputSHA256": input_hash,
                "Output": candidate.name,
                "OutputSHA256": file_sha256(candidate),
            },
            ensure_ascii=False,
        ),
        encoding="utf-8",
    )
    (staging / "technical_review.json").write_text(
        json.dumps(
            {
                "SchemaVersion": "technical_review_v2",
                "RuleSetVersion": "p3_art_technical_rules_002",
                "VisualID": visual_id,
                "ProductionProfile": "character_portrait_set",
                "Candidates": [technical],
            },
            ensure_ascii=False,
        ),
        encoding="utf-8",
    )
    decision_payload = {
        "State": state,
        "Candidates": [
            {
                "File": candidate.name,
                "Status": requested_status,
                "SHA256": file_sha256(candidate),
                "Width": technical.get("Metrics", {}).get("Width", 0),
                "Height": technical.get("Metrics", {}).get("Height", 0),
                "Format": "png",
                "Reasons": technical.get("Reasons", []),
            }
        ],
    }
    (staging / "decision.json").write_text(json.dumps(decision_payload, ensure_ascii=False), encoding="utf-8")
    if visual_review is not None:
        review_copy = copy.deepcopy(visual_review)
        review_hash = str(review_copy.get("CandidateSHA256", "") or "").lower()
        candidate_hash = file_sha256(candidate).lower()
        if review_hash and review_hash != candidate_hash:
            raise ValueError(f"processing_visual_review_sha_mismatch:{visual_id}")
        review_copy["Candidate"] = candidate.name
        review_copy["CandidateSHA256"] = candidate_hash
        (staging / "visual_review.json").write_text(json.dumps(review_copy, ensure_ascii=False), encoding="utf-8")
    override = decision.get("TechnicalOverride")
    if isinstance(override, dict):
        (staging / "technical_override.json").write_text(json.dumps(override, ensure_ascii=False), encoding="utf-8")
    override_path = str(decision.get("TechnicalOverridePath", "") or "")
    if override_path:
        source_override = Path(override_path)
        source_override = source_override if source_override.is_absolute() else project_root / source_override
        if not source_override.is_file():
            raise ValueError(f"processing_technical_override_missing:{visual_id}")
        shutil.copy2(source_override, staging / "technical_override.json")
    return staging


def _summary_state(items: list[dict[str, Any]]) -> str:
    results = {str(item.get("Result", "")) for item in items}
    if items and results <= TERMINAL_RESULTS:
        return "selection_complete"
    if results & FAILURE_RESULTS:
        return "selection_complete_with_failures"
    return "selection_in_progress"


def _processed_evidence_current(item: dict[str, Any], project_root: Path) -> dict[str, Any] | None:
    recorded = item.get("Processed")
    if isinstance(recorded, dict):
        candidate_value = str(recorded.get("CandidatePath", "") or "")
        candidate = project_root / candidate_value if candidate_value and not Path(candidate_value).is_absolute() else Path(candidate_value)
        if candidate.is_file() and str(recorded.get("CandidateSHA256", "")).lower() == file_sha256(candidate).lower():
            return recorded

    generation = item.get("Generation") if isinstance(item.get("Generation"), dict) else {}
    expected_hashes = {str(value).lower() for value in generation.get("OutputSHA256", [])}
    workspace = project_root / str(item.get("Workspace", ""))
    processed = workspace / "processed"
    rounds = sorted(
        [path for path in processed.iterdir() if path.is_dir() and path.name.isdecimal() and int(path.name) > 0],
        key=lambda path: int(path.name),
    ) if processed.is_dir() else []
    for round_dir in reversed(rounds):
        decision_path = round_dir / "decision.json"
        if not decision_path.is_file():
            continue
        decision = read_json(decision_path)
        if decision.get("State") not in {"passed", "warning"}:
            continue
        process_report = read_json(round_dir / "process_report.json") if (round_dir / "process_report.json").is_file() else {}
        input_hash = str(process_report.get("InputSHA256", "") or "").lower()
        output_hashes = set()
        if process_report.get("OutputSHA256"):
            output_hashes.add(str(process_report["OutputSHA256"]).lower())
        output_value = process_report.get("Output")
        if isinstance(output_value, dict) and output_value.get("SHA256"):
            output_hashes.add(str(output_value["SHA256"]).lower())
        for candidate in decision.get("Candidates", []):
            if not isinstance(candidate, dict):
                continue
            filename = str(candidate.get("File", "") or "")
            path = round_dir / filename
            if path.is_file() and (
                (expected_hashes and file_sha256(path).lower() in expected_hashes)
                or (input_hash and input_hash in expected_hashes)
                or (output_hashes and file_sha256(path).lower() in output_hashes)
                or (not expected_hashes and str(process_report.get("VisualID", "")) == str(item.get("VisualID", "")))
            ):
                return {
                    "VisualID": item.get("VisualID", ""),
                    "RoundNumber": int(round_dir.name),
                    "State": decision.get("State"),
                    "CandidatePath": path.relative_to(project_root).as_posix(),
                    "CandidateSHA256": file_sha256(path),
                }
    return None


def _selection_evidence_current(item: dict[str, Any], project_root: Path) -> dict[str, Any] | None:
    recorded = item.get("Selection")
    if not isinstance(recorded, dict):
        return None
    selected_value = str(recorded.get("SelectedPath", "") or "")
    selected = project_root / selected_value if selected_value and not Path(selected_value).is_absolute() else Path(selected_value)
    expected_hash = str(recorded.get("SHA256", "") or "").lower()
    if selected.is_file() and expected_hash and file_sha256(selected).lower() == expected_hash:
        return recorded
    return None


def _record_processed_evidence(processed: dict[str, Any], item: dict[str, Any], project_root: Path) -> dict[str, Any]:
    result = copy.deepcopy(processed)
    workspace = project_root / str(item.get("Workspace", ""))
    round_dir = workspace / "processed" / str(processed.get("RoundNumber", ""))
    decision_path = round_dir / "decision.json"
    if decision_path.is_file():
        decision = read_json(decision_path)
        for candidate in decision.get("Candidates", []):
            if isinstance(candidate, dict) and str(candidate.get("Status", "")) in {"passed", "warning"}:
                path = round_dir / str(candidate.get("File", ""))
                if path.is_file():
                    result["CandidatePath"] = path.relative_to(project_root).as_posix()
                    result["CandidateSHA256"] = file_sha256(path)
                    break
    return result


def execute_portrait_set_run(
    *,
    plan: dict[str, Any],
    manifest_path: Path,
    catalog_path: Path,
    incoming_root: Path,
    run_dir: Path,
    production_run_id: str,
    execution_mode: str,
    resume: bool,
    provider: str,
    config: str,
    variants: int,
    execute_limit: int,
    processing_decisions_path: Path | None,
    visual_review_path: Path | None,
    allow_selected_overwrite: bool,
    allowed_technical_overrides: set[str],
    project_root: Path = PROJECT_ROOT,
    runner: Any = subprocess.run,
) -> dict[str, Any]:
    state_path = run_dir / "portrait-set-run.json"
    if run_dir.exists() and not resume:
        raise FileExistsError(f"resume_required:{run_dir}")
    previous = read_json(state_path) if resume and state_path.exists() else None
    if previous is not None and previous.get("SchemaVersion") != RUN_SCHEMA:
        raise ValueError("portrait_set_run_schema_invalid")

    previous_items = {
        str(item.get("VisualID")): item
        for item in (previous.get("Items", []) if isinstance(previous, dict) else [])
        if isinstance(item, dict)
    }
    items: list[dict[str, Any]] = []
    for source in plan.get("Items", []):
        visual_id = str(source.get("VisualID", ""))
        old = copy.deepcopy(previous_items.get(visual_id, {}))
        current = copy.deepcopy(source)
        current["Dependencies"] = _item_dependencies(source, {str(i.get("AssetID")): i for i in plan.get("Items", [])})
        current["Stage"] = old.get("Stage", "source_audit")
        current["Result"] = old.get("Result", "")
        for key in ("Generation", "Processed", "Selection", "PendingDecision"):
            if key in old:
                current[key] = old[key]
        items.append(current)

    state: dict[str, Any] = {
        "SchemaVersion": RUN_SCHEMA,
        "ProductionRunID": production_run_id,
        "AssetSetID": plan.get("AssetSetID", ""),
        "ExecutionMode": execution_mode.lower(),
        "PlanFingerprint": payload_fingerprint(plan),
        "Items": items,
        "PendingDecision": [],
        "FinalState": "selection_in_progress",
    }
    if not resume:
        write_json_atomic(run_dir / "portrait-set-plan.json", plan)
    elif not (run_dir / "portrait-set-plan.json").exists():
        write_json_atomic(run_dir / "portrait-set-plan.json", plan)
    write_json_atomic(state_path, state)
    decisions = _load_decisions(processing_decisions_path)
    reviews = _load_visual_review(visual_review_path)
    by_visual = {str(item.get("VisualID")): item for item in items}
    generation_dir = run_dir / "generation"
    for item in items[: max(1, execute_limit)]:
        visual_id = str(item["VisualID"])
        existing_selection = _selection_evidence_current(item, project_root)
        if existing_selection is not None:
            item["Selection"] = existing_selection
            item["Stage"] = "complete"
            item["Result"] = "selected"
            continue
        if any(by_visual.get(dep, {}).get("Result") in FAILURE_RESULTS for dep in item.get("Dependencies", [])):
            item["Stage"] = "complete"
            item["Result"] = "blocked_by_dependency"
            continue

        workspace = project_root / str(item["Workspace"])
        generation_path = workspace / "generation.json"
        generation = read_json(generation_path) if generation_path.exists() else None
        if generation is not None:
            try:
                evidence = _validate_generation_evidence(item=item, generation=generation, project_root=project_root, run_id=production_run_id)
            except ValueError as exc:
                reason = str(exc)
                item["Stage"] = "prompt_resolution" if reason in {"prompt_stale", "reference_stale"} else "generation"
                item["Result"] = reason
                write_json_atomic(state_path, state)
                continue
            if item.get("Generation") and not _generation_record_matches(item, evidence):
                item["Stage"] = "output_contract_audit"
                item["Result"] = "generation_stale"
                write_json_atomic(state_path, state)
                continue
            snapshot_path = _write_generation_snapshot(
                generation_dir=generation_dir,
                visual_id=visual_id,
                generation=generation,
                previous=item.get("Generation"),
                project_root=project_root,
            )
            item["Generation"] = {**evidence, "SnapshotPath": snapshot_path.relative_to(project_root).as_posix()}
            item["Stage"] = "processing_decision"
            item["Result"] = "review_required"
        elif item.get("Generation"):
            snapshot_path = project_root / str(item["Generation"].get("SnapshotPath", ""))
            if snapshot_path.exists():
                generation = read_json(snapshot_path)
                try:
                    _validate_generation_evidence(item=item, generation=generation, project_root=project_root, run_id=production_run_id)
                except ValueError as exc:
                    item["Stage"] = "prompt_resolution"
                    item["Result"] = str(exc)
                    write_json_atomic(state_path, state)
                    continue
                item["Stage"] = "processing_decision"
                item["Result"] = "review_required"

        if generation is None:
            if not provider:
                item["Stage"] = "generation"
                item["Result"] = "generation_failed"
                continue
            command = [
                sys.executable,
                str(SCRIPT_DIR / "run_art_generation.py"),
                "--manifest-path", str(manifest_path),
                "--request-catalog", str(catalog_path),
                "--provider", provider,
                "--prompt-format", item["PromptFormat"],
                "--prompt-revision-id", item["PromptRevisionID"],
                "--visual-id", visual_id,
                "--status", str(item.get("Status", "")),
                "--variants", str(variants),
                "--batch-id", production_run_id,
            ]
            if config:
                command.extend(["--config", config])
            command.extend(reference_cli_arguments(item.get("ResolvedReferenceAssets", [])))
            if str(item.get("Status", "")) in {"approved", "registered", "runtime_validated"}:
                command.append("--preserve-status")
            completed = runner(command, cwd=project_root)
            if completed.returncode != 0:
                item["Stage"] = "generation"
                item["Result"] = "generation_failed"
                continue
            if not generation_path.exists():
                item["Stage"] = "generation"
                item["Result"] = "generation_failed"
                continue
            generation = read_json(generation_path)
            try:
                evidence = _validate_generation_evidence(item=item, generation=generation, project_root=project_root, run_id=production_run_id)
            except ValueError as exc:
                item["Stage"] = "generation"
                item["Result"] = str(exc)
                continue
            snapshot_path = _write_generation_snapshot(
                generation_dir=generation_dir,
                visual_id=visual_id,
                generation=generation,
                previous=item.get("Generation"),
                project_root=project_root,
            )
            item["Generation"] = {**evidence, "SnapshotPath": snapshot_path.relative_to(project_root).as_posix()}
            item["Stage"] = "processing_decision"
            item["Result"] = "review_required"

        decision = decisions.get(visual_id)
        if decision is None:
            item["PendingDecision"] = "processing_decision"
            continue
        item.pop("PendingDecision", None)
        action = str(decision.get("Action", ""))
        if action in {"manual_edit_required", "regenerate_required"}:
            item["Stage"] = "processing_decision"
            item["Result"] = "review_required"
            item["PendingDecision"] = action
            continue
        if visual_id not in reviews:
            item["Stage"] = "visual_review"
            item["Result"] = "review_required"
            item["PendingDecision"] = "visual_review"
            continue
        existing_processed = _processed_evidence_current(item, project_root)
        if existing_processed is not None:
            item["Processed"] = existing_processed
            item["Stage"] = "visual_review"
            item["Result"] = "review_required"
        else:
            try:
                staging = _make_processing_staging(
                    item=item,
                    decision=decision,
                    generation=generation,
                    run_dir=run_dir,
                    manifest_path=manifest_path,
                    project_root=project_root,
                    visual_review=reviews.get(visual_id),
                )
                processed = register_processing_round(
                    manifest_path=manifest_path,
                    incoming_root=incoming_root,
                    visual_id=visual_id,
                    staging_dir=staging,
                    dry_run=False,
                    allowed_override_rules=allowed_technical_overrides,
                )
            except Exception as exc:
                item["Stage"] = "technical_gate"
                item["Result"] = "technical_failed"
                item["Error"] = str(exc)
                continue
            item["Processed"] = _record_processed_evidence(processed, item, project_root)
            item["Stage"] = "visual_review"
            item["Result"] = "review_required"
        review_item = reviews.get(visual_id)
        item.pop("PendingDecision", None)
        processed_candidate_value = str(item.get("Processed", {}).get("CandidatePath", "") or "")
        processed_candidate = project_root / processed_candidate_value if processed_candidate_value and not Path(processed_candidate_value).is_absolute() else Path(processed_candidate_value)
        if not processed_candidate.is_file():
            item["Stage"] = "visual_review"
            item["Result"] = "review_required"
            item["Error"] = "processed_candidate_missing"
            continue
        normalized_review = copy.deepcopy(review_item)
        review_hash = str(normalized_review.get("CandidateSHA256", "") or "").lower()
        processed_hash = file_sha256(processed_candidate).lower()
        if review_hash and review_hash != processed_hash:
            item["Stage"] = "visual_review"
            item["Result"] = "review_required"
            item["Error"] = "visual_review_candidate_sha_mismatch"
            continue
        normalized_review["Candidate"] = processed_candidate.relative_to(project_root).as_posix()
        normalized_review["CandidateSHA256"] = processed_hash
        review_payload = {"ProductionRunID": production_run_id, "Items": [normalized_review]}
        review_file = run_dir / "visual-review" / f"{visual_id}.json"
        write_json_atomic(review_file, review_payload)
        try:
            selected = select_art_candidate(
                project_root=project_root,
                manifest_path=manifest_path,
                incoming_root=incoming_root,
                visual_id=visual_id,
                review_path=review_file,
                allow_selected_overwrite=allow_selected_overwrite,
                dry_run=False,
            )
        except Exception as exc:
            item["Stage"] = "guarded_selection"
            item["Result"] = "review_required"
            item["Error"] = str(exc)
            continue
        item["Selection"] = selected
        item["Stage"] = "complete"
        item["Result"] = "selected" if selected.get("State") == "selected" else "kept_existing"

    for item in items:
        if item.get("Result") == "" and item.get("Stage") != "complete":
            item["Result"] = "review_required"
    state["PendingDecision"] = [
        {"VisualID": item["VisualID"], "Stage": item.get("Stage"), "Action": item.get("PendingDecision")}
        for item in items
        if item.get("PendingDecision")
    ]
    state["FinalState"] = _summary_state(items)
    write_json_atomic(state_path, state)
    summary = {
        "ProductionRunID": production_run_id,
        "AssetSetID": plan.get("AssetSetID", ""),
        "FinalState": state["FinalState"],
        "Claims": {str(item["VisualID"]): item.get("Result", "review_required") for item in items},
        "PendingDecision": state["PendingDecision"],
        "ApprovedChanged": False,
    }
    write_json_atomic(run_dir / "summary.json", summary)
    return state


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Plan or execute a bounded P3 character portrait set.")
    parser.add_argument("--asset-set-id", required=True)
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--request-catalog-path", default=DEFAULT_REQUEST_CATALOG)
    parser.add_argument("--prompt-format", choices=["auto", NATURAL_LANGUAGE_FORMAT, DANBOORU_TAGS_FORMAT], default="auto")
    parser.add_argument("--visual-id", action="append", default=[])
    parser.add_argument("--provider", default="")
    parser.add_argument("--config", default="")
    parser.add_argument("--variants", type=int, default=2)
    parser.add_argument("--execute-limit", type=int, default=1)
    parser.add_argument("--production-run-id", default="")
    parser.add_argument("--execution-mode", choices=["interactive", "automatic"], default="interactive")
    parser.add_argument("--resume", action="store_true")
    parser.add_argument("--processing-decisions", default="")
    parser.add_argument("--visual-review", default="")
    parser.add_argument("--allow-selected-overwrite", action="store_true")
    parser.add_argument("--allow-technical-override", action="append", default=[])
    parser.add_argument("--dry-run", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    manifest_path = resolve_project_path(args.manifest_path)
    catalog_path = resolve_project_path(args.request_catalog_path)
    run_id = args.production_run_id or f"portrait_set_{args.asset_set_id}_{datetime.now().strftime('%Y%m%d_%H%M%S')}"
    run_dir = resolve_project_path(DEFAULT_LOG_ROOT) / run_id
    manifest = read_json(manifest_path)
    catalog = read_json(catalog_path)
    visual_ids = {part.strip() for value in args.visual_id for part in value.split(",") if part.strip()}
    plan_path = run_dir / "portrait-set-plan.json"
    if args.resume and plan_path.is_file():
        plan = read_json(plan_path)
    else:
        plan = build_portrait_set_plan(
            manifest,
            catalog,
            args.asset_set_id,
            prompt_format=args.prompt_format,
            provider=args.provider,
            visual_ids=visual_ids,
        )
    print(json.dumps(plan, ensure_ascii=False, indent=2))
    if args.dry_run:
        return 0 if plan["State"] == "ready" else 2
    if plan["State"] != "ready":
        return 2
    if not args.provider and not args.resume:
        print("[BLOCKED] provider_required_for_execution")
        return 2
    processing_path = resolve_project_path(args.processing_decisions) if args.processing_decisions else None
    review_path = resolve_project_path(args.visual_review) if args.visual_review else None
    try:
        state = execute_portrait_set_run(
            plan=plan,
            manifest_path=manifest_path,
            catalog_path=catalog_path,
            incoming_root=resolve_project_path("UnityClient/Assets/Art/_IncomingAI"),
            run_dir=run_dir,
            production_run_id=run_id,
            execution_mode=args.execution_mode,
            resume=args.resume,
            provider=args.provider,
            config=args.config,
            variants=args.variants,
            execute_limit=args.execute_limit,
            processing_decisions_path=processing_path,
            visual_review_path=review_path,
            allow_selected_overwrite=args.allow_selected_overwrite,
            allowed_technical_overrides={str(value).strip() for value in args.allow_technical_override if str(value).strip()},
        )
    except FileExistsError as exc:
        print(f"[BLOCKED] {exc}")
        return 2
    except ValueError as exc:
        print(f"[BLOCKED] {exc}")
        return 2
    print(json.dumps(state, ensure_ascii=False, indent=2))
    return 2 if state.get("PendingDecision") else 0


if __name__ == "__main__":
    raise SystemExit(main())
