# -*- coding: utf-8 -*-
"""Execute a generated P3 art plan through raw generation and processing."""

from __future__ import annotations

import argparse
import json
import re
import subprocess
import sys
from collections import defaultdict
from datetime import datetime
from pathlib import Path
from typing import Any

from PIL import Image

from art_prompt_revision import select_prompt_variant
from art_workspace import normalize_entry_workspace_paths, workspace_path


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]
DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_INCOMING_ROOT = "UnityClient/Assets/Art/_IncomingAI"
DEFAULT_LOG_ROOT = "UnityClient/Logs/P3ArtProduction"
DEFAULT_GATEWAY_CONFIG = "tools/ai-image-gateway/config.local.yaml"
DEFAULT_REQUEST_CATALOG = "美术文档/_generated/art_generation_requests.json"
SUPPORTED_UI_SKIN_CAPABILITIES = {"deterministic_template"}


def read_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(f".{path.name}.tmp")
    temporary.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    temporary.replace(path)


def resolve_project_path(value: str, project_root: Path = PROJECT_ROOT) -> Path:
    path = Path(value)
    return path if path.is_absolute() else project_root / path


def repo_path(path: Path, project_root: Path = PROJECT_ROOT) -> str:
    try:
        return path.resolve().relative_to(project_root.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def parse_csv(values: list[str]) -> set[str]:
    return {
        part.strip()
        for value in values
        for part in value.split(",")
        if part.strip()
    }


def parse_routes(values: list[str]) -> dict[str, str]:
    routes: dict[str, str] = {}
    for value in values:
        if "=" not in value:
            raise ValueError(f"Invalid route; expected asset_class=provider: {value}")
        asset_class, provider = (part.strip() for part in value.split("=", 1))
        if not asset_class or not provider:
            raise ValueError(f"Invalid route; expected asset_class=provider: {value}")
        routes[asset_class] = provider
    return routes


def safe_id(value: str) -> str:
    return re.sub(r"_+", "_", re.sub(r"[^A-Za-z0-9_.-]+", "_", value)).strip("._-")


def classify_item(item: dict[str, Any], entry: dict[str, Any]) -> str:
    explicit = str(item.get("AssetClass", "") or "")
    if explicit:
        return explicit
    spec = entry.get("Spec") if isinstance(entry.get("Spec"), dict) else {}
    process_spec = spec.get("ProcessSpec") if isinstance(spec.get("ProcessSpec"), dict) else {}
    nine_slice = process_spec.get("NineSlice") if isinstance(process_spec.get("NineSlice"), dict) else {}
    asset_type = str(item.get("AssetType") or entry.get("AssetType") or "")
    domain = str(item.get("Domain") or entry.get("Domain") or "")
    visual_id = str(item.get("VisualID", ""))
    if bool(nine_slice.get("Enabled")):
        return "ui_skin"
    if asset_type == "icon":
        return "icon"
    if visual_id.startswith("bg_") or domain == "background" or asset_type == "background":
        return "background"
    if visual_id.startswith(("fx_", "vfx_")):
        return "vfx"
    return "standard_asset"


def _command_paths() -> tuple[str, str]:
    return str(SCRIPT_DIR / "run_art_generation.py"), str(SCRIPT_DIR / "optimize_art_assets.py")


def build_execution_plan(
    payload: dict[str, Any],
    manifest: dict[str, Any],
    *,
    routes: dict[str, str],
    provider_override: str,
    visual_ids: set[str],
    asset_classes: set[str],
    limit: int,
    manifest_path: str = DEFAULT_MANIFEST,
    incoming_root: str = DEFAULT_INCOMING_ROOT,
    config_path: str = "",
    request_catalog: dict[str, Any] | None = None,
    request_catalog_path: str = DEFAULT_REQUEST_CATALOG,
) -> dict[str, Any]:
    run_config = payload.get("RunConfig") if isinstance(payload.get("RunConfig"), dict) else {}
    action = str(run_config.get("Action") or run_config.get("ActionFilter") or "")
    if action not in {"generate_needed", "visual_v2_replace"}:
        raise ValueError(f"Unsupported art batch action: {action or '<missing>'}")
    replacement = action == "visual_v2_replace"
    base_batch_id = str(run_config.get("BatchID") or f"art_batch_{datetime.now().strftime('%Y%m%d_%H%M%S')}")
    default_provider = str(provider_override or run_config.get("Provider") or "")
    if default_provider == "agent_selected":
        default_provider = ""
    default_status = str(run_config.get("Status") or "todo")
    default_variants = int(run_config.get("Variants") or 4)
    default_delay = float(run_config.get("DelaySeconds") or 1.0)

    entries = {
        str(entry.get("VisualID", "")): normalize_entry_workspace_paths(entry)
        for entry in manifest.get("Entries", [])
        if isinstance(entry, dict) and entry.get("VisualID")
    }
    request_map = {
        str(request.get("RequestID", "")): request
        for request in (request_catalog or {}).get("Requests", [])
        if isinstance(request, dict) and request.get("RequestID")
    }
    requested: list[dict[str, Any]] = []
    blocked: list[dict[str, str]] = []
    seen: set[str] = set()
    items = [item for item in payload.get("Items", []) if isinstance(item, dict)]
    items.sort(key=lambda item: (int(item.get("Order") or 0), str(item.get("Priority", "")), str(item.get("VisualID", ""))))
    for item in items:
        visual_id = str(item.get("VisualID", "") or "")
        if not visual_id or visual_id in seen:
            if visual_id:
                blocked.append({"VisualID": visual_id, "Reason": "duplicate_visual_id"})
            continue
        seen.add(visual_id)
        if visual_ids and visual_id not in visual_ids:
            continue
        entry = entries.get(visual_id)
        if entry is None:
            blocked.append({"VisualID": visual_id, "Reason": "manifest_missing"})
            continue
        asset_class = classify_item(item, entry)
        if str(entry.get("ProductionProfile", "standard_asset")) == "character_portrait_set":
            blocked.append({"VisualID": visual_id, "Reason": "route_mismatch:character_portrait_set_requires_portrait_set_orchestration"})
            continue
        if asset_classes and asset_class not in asset_classes:
            continue
        route_target = str(provider_override or routes.get(asset_class) or default_provider)
        if not route_target:
            blocked.append({"VisualID": visual_id, "Reason": f"route_required:{asset_class}"})
            continue
        route_kind = "image_provider"
        provider = route_target
        capability = ""
        if asset_class == "ui_skin":
            if route_target not in SUPPORTED_UI_SKIN_CAPABILITIES:
                blocked.append({"VisualID": visual_id, "Reason": f"unsupported_ui_skin_capability:{route_target}"})
                continue
            route_kind = "ui_skin_capability"
            provider = ""
            capability = route_target
        pointer = entry.get("CompiledRequest") if isinstance(entry.get("CompiledRequest"), dict) else {}
        request_id = str(item.get("RequestID") or pointer.get("RequestID", ""))
        requirement_fingerprint = str(
            item.get("RequirementFingerprint") or pointer.get("RequirementFingerprint", "")
        )
        prompt_revision_id = ""
        revision_fingerprint = ""
        if route_kind == "ui_skin_capability":
            prompt_format = "not_required"
        else:
            request = request_map.get(request_id)
            if request is None:
                blocked.append({"VisualID": visual_id, "Reason": "compiled_request_missing"})
                continue
            if request.get("VisualID") != visual_id:
                blocked.append({"VisualID": visual_id, "Reason": "compiled_request_visual_id_mismatch"})
                continue
            if request.get("RequirementFingerprint") != requirement_fingerprint:
                blocked.append({"VisualID": visual_id, "Reason": "compiled_request_fingerprint_mismatch"})
                continue
            if pointer.get("PromptAuthoringStatus") != request.get("PromptAuthoringStatus"):
                blocked.append({"VisualID": visual_id, "Reason": "prompt_authoring_status_mismatch"})
                continue
            if str(pointer.get("ActivePromptRevisionID", "") or "") != str(request.get("ActivePromptRevisionID", "") or ""):
                blocked.append({"VisualID": visual_id, "Reason": "prompt_revision_pointer_mismatch"})
                continue
            try:
                revision, prompt_format, _ = select_prompt_variant(
                    request,
                    prompt_revision_id=str(item.get("PromptRevisionID", "") or ""),
                    prompt_format=str(item.get("PromptFormat", "auto") or "auto"),
                    provider=provider,
                )
            except ValueError as exc:
                blocked.append({"VisualID": visual_id, "Reason": str(exc)})
                continue
            prompt_revision_id = str(revision.get("PromptRevisionID", ""))
            revision_fingerprint = str(revision.get("RevisionFingerprint", ""))
        status = str(entry.get("Status", "")) if replacement else default_status
        if replacement and status not in {"approved", "registered", "runtime_validated"}:
            blocked.append({"VisualID": visual_id, "Reason": f"replacement_status_{status or 'missing'}"})
            continue
        item_run = item.get("RunConfig") if isinstance(item.get("RunConfig"), dict) else {}
        requested.append(
            {
                "VisualID": visual_id,
                "AssetClass": asset_class,
                "RouteKind": route_kind,
                "Provider": provider,
                "Capability": capability,
                "Status": status,
                "Variants": int(item_run.get("Variants") or default_variants),
                "DelaySeconds": float(item_run.get("DelaySeconds") or default_delay),
                "PreserveStatus": replacement,
                "RequestID": request_id,
                "RequirementFingerprint": requirement_fingerprint,
                "PromptRevisionID": prompt_revision_id,
                "PromptRevisionFingerprint": revision_fingerprint,
                "PromptFormat": prompt_format,
            }
        )
        if limit > 0 and len(requested) >= limit:
            break

    grouped: dict[tuple[Any, ...], list[dict[str, Any]]] = defaultdict(list)
    for item in requested:
        key = (
            item["AssetClass"],
            item["RouteKind"],
            item["Provider"],
            item["Capability"],
            item["Status"],
            item["Variants"],
            item["DelaySeconds"],
            item["PreserveStatus"],
            item["PromptFormat"],
            item["PromptRevisionID"],
        )
        grouped[key].append(item)

    generation_script, processing_script = _command_paths()
    ui_skin_script = str(SCRIPT_DIR / "generate_ui_skin_candidate.py")
    groups: list[dict[str, Any]] = []
    for key, group_items in sorted(grouped.items(), key=lambda pair: tuple(str(value) for value in pair[0])):
        (
            asset_class,
            route_kind,
            provider,
            capability,
            status,
            variants,
            delay_seconds,
            preserve_status,
            prompt_format,
            prompt_revision_id,
        ) = key
        visual_id_values = sorted(item["VisualID"] for item in group_items)
        route_name = capability or provider
        suffix = safe_id(f"{asset_class}_{route_name}_{status}")
        group_batch_id = base_batch_id if len(grouped) == 1 else f"{base_batch_id}_{suffix}"
        if route_kind == "ui_skin_capability":
            generation_command = [
                sys.executable,
                ui_skin_script,
                "--manifest-path",
                manifest_path,
                "--incoming-root",
                incoming_root,
                "--visual-id",
                ",".join(visual_id_values),
                "--variants",
                str(variants),
                "--batch-id",
                group_batch_id,
                "--capability",
                str(capability),
            ]
        else:
            generation_command = [
                sys.executable,
                generation_script,
                "--manifest-path",
                manifest_path,
                "--out-root",
                incoming_root,
                "--provider",
                str(provider),
                "--status",
                str(status),
                "--visual-id",
                ",".join(visual_id_values),
                "--variants",
                str(variants),
                "--delay-seconds",
                str(delay_seconds),
                "--batch-id",
                group_batch_id,
                "--request-catalog",
                request_catalog_path,
                "--prompt-format",
                prompt_format,
            ]
            if prompt_revision_id:
                generation_command.extend(["--prompt-revision-id", prompt_revision_id])
            if config_path:
                generation_command.extend(["--config", config_path])
            if preserve_status:
                generation_command.append("--preserve-status")

        processing_status = str(status) if preserve_status else "generated"
        processing_command = [
            sys.executable,
            processing_script,
            "--manifest-path",
            manifest_path,
            "--in-root",
            incoming_root,
            "--status",
            processing_status,
            "--visual-id",
            ",".join(visual_id_values),
        ]
        if preserve_status:
            processing_command.extend(["--candidate-batch-id", group_batch_id])
        else:
            processing_command.extend(["--batch-id", group_batch_id])
        groups.append(
            {
                "AssetClass": asset_class,
                "RouteKind": route_kind,
                "Provider": provider,
                "Capability": capability,
                "Status": status,
                "Variants": variants,
                "DelaySeconds": delay_seconds,
                "PreserveStatus": preserve_status,
                "PromptFormat": prompt_format,
                "PromptRevisionID": prompt_revision_id,
                "PromptRevisionFingerprint": next(
                    (item["PromptRevisionFingerprint"] for item in group_items if item["PromptRevisionFingerprint"]),
                    "",
                ),
                "RequestIDs": sorted({item["RequestID"] for item in group_items if item["RequestID"]}),
                "BatchID": group_batch_id,
                "VisualIDs": visual_id_values,
                "GenerationCommand": generation_command,
                "ProcessingCommand": processing_command,
            }
        )
    return {"Action": action, "BatchID": base_batch_id, "Groups": groups, "Blocked": blocked}


def decodable_image(path: Path) -> bool:
    try:
        with Image.open(path) as image:
            image.load()
        return True
    except (OSError, ValueError):
        return False


def collect_generation_results(
    group: dict[str, Any],
    manifest_path: Path,
    incoming_root: Path,
    *,
    project_root: Path = PROJECT_ROOT,
) -> list[dict[str, Any]]:
    manifest = read_json(manifest_path)
    entries = {
        str(entry.get("VisualID", "")): normalize_entry_workspace_paths(entry)
        for entry in manifest.get("Entries", [])
        if isinstance(entry, dict) and entry.get("VisualID")
    }
    results: list[dict[str, Any]] = []
    for visual_id in group["VisualIDs"]:
        entry = entries.get(visual_id)
        if entry is None:
            results.append({"VisualID": visual_id, "State": "failed", "OutputCount": 0, "Errors": ["manifest_missing_after_generation"]})
            continue
        workspace = workspace_path(incoming_root, entry)
        record_path = workspace / "generation.json"
        if not record_path.exists():
            results.append({"VisualID": visual_id, "State": "failed", "OutputCount": 0, "Errors": ["generation_record_missing"]})
            continue
        record = read_json(record_path)
        if str(record.get("BatchID", "")) != str(group["BatchID"]):
            results.append({"VisualID": visual_id, "State": "failed", "OutputCount": 0, "Errors": ["generation_batch_mismatch"]})
            continue
        valid_outputs: list[str] = []
        invalid_outputs: list[str] = []
        for output in record.get("Outputs", []):
            if not isinstance(output, dict) or not output.get("RepoPath"):
                continue
            path = resolve_project_path(str(output["RepoPath"]), project_root)
            if path.exists() and decodable_image(path):
                valid_outputs.append(repo_path(path, project_root))
            else:
                invalid_outputs.append(str(output.get("RepoPath", "")))
        errors = [str(value) for value in record.get("Errors", [])]
        if invalid_outputs:
            errors.append("invalid_outputs:" + ",".join(invalid_outputs))
        results.append(
            {
                "VisualID": visual_id,
                "State": "succeeded" if valid_outputs else "failed",
                "OutputCount": len(valid_outputs),
                "Outputs": valid_outputs,
                "Errors": errors,
            }
        )
    return results


def command_with_visual_ids(command: list[str], visual_ids: list[str]) -> list[str]:
    updated = list(command)
    index = updated.index("--visual-id")
    updated[index + 1] = ",".join(visual_ids)
    return updated


def collect_processing_results(
    execution_plan: dict[str, Any],
    manifest_path: Path,
    incoming_root: Path,
    *,
    generation_results: dict[str, dict[str, Any]] | None = None,
) -> list[dict[str, Any]]:
    manifest = read_json(manifest_path)
    entries = {
        str(entry.get("VisualID", "")): normalize_entry_workspace_paths(entry)
        for entry in manifest.get("Entries", [])
        if isinstance(entry, dict) and entry.get("VisualID")
    }
    results: list[dict[str, Any]] = []
    for group in execution_plan["Groups"]:
        for visual_id in group["VisualIDs"]:
            generation = (generation_results or {}).get(visual_id)
            if generation is not None and generation.get("State") != "succeeded":
                results.append(
                    {
                        "VisualID": visual_id,
                        "AssetClass": group["AssetClass"],
                        "Provider": group["Provider"],
                        "Capability": group.get("Capability", ""),
                        "CandidateBatchID": group["BatchID"],
                        "LatestRound": None,
                        "State": "generation_failed",
                        "Claim": "raw_failed",
                        "Reason": ";".join(generation.get("Errors", [])),
                        "ContactSheet": "",
                    }
                )
                continue
            entry = entries.get(visual_id)
            if entry is None:
                results.append({"VisualID": visual_id, "State": "failed", "Reason": "manifest_missing_after_run"})
                continue
            workspace = workspace_path(incoming_root, entry)
            report_path = workspace / "process_report.json"
            report = read_json(report_path) if report_path.exists() else {}
            latest_state = str(report.get("LatestState") or "missing")
            report_batch = str(report.get("CandidateBatchID") or "")
            if group.get("PreserveStatus") and report_batch != str(group["BatchID"]):
                latest_state = "missing"
                report = {"Reason": "processing_batch_mismatch"}
            results.append(
                {
                    "VisualID": visual_id,
                    "AssetClass": group["AssetClass"],
                    "Provider": group["Provider"],
                    "Capability": group.get("Capability", ""),
                    "CandidateBatchID": group["BatchID"],
                    "LatestRound": report.get("LatestRound"),
                    "State": latest_state,
                    "Claim": "review_required" if latest_state == "passed" else f"processed_{latest_state}",
                    "Reason": str(report.get("Reason") or ""),
                    "ContactSheet": str(report.get("ContactSheet") or ""),
                }
            )
    return results


def main() -> int:
    parser = argparse.ArgumentParser(description="Run a P3 art production plan through generation and processing.")
    parser.add_argument("--plan-path", required=True)
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--request-catalog", default=DEFAULT_REQUEST_CATALOG)
    parser.add_argument("--incoming-root", default=DEFAULT_INCOMING_ROOT)
    parser.add_argument("--log-root", default=DEFAULT_LOG_ROOT)
    parser.add_argument("--config", default=DEFAULT_GATEWAY_CONFIG)
    parser.add_argument("--provider", default="")
    parser.add_argument("--route", action="append", default=[])
    parser.add_argument("--visual-id", action="append", default=[])
    parser.add_argument("--asset-class", action="append", default=[])
    parser.add_argument("--limit", type=int, default=0)
    parser.add_argument("--production-run-id", default="")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--allow-blocked", action="store_true")
    args = parser.parse_args()

    plan_path = resolve_project_path(args.plan_path)
    manifest_path = resolve_project_path(args.manifest_path)
    incoming_root = resolve_project_path(args.incoming_root)
    log_root = resolve_project_path(args.log_root)
    config_path = resolve_project_path(args.config) if args.config else None
    if not plan_path.exists():
        raise FileNotFoundError(plan_path)
    if not manifest_path.exists():
        raise FileNotFoundError(manifest_path)
    request_catalog_path = resolve_project_path(args.request_catalog)
    request_catalog = None
    if request_catalog_path is not None and request_catalog_path.exists():
        request_catalog = read_json(request_catalog_path)
    if config_path is not None and not config_path.exists():
        raise FileNotFoundError(config_path)

    execution_plan = build_execution_plan(
        read_json(plan_path),
        read_json(manifest_path),
        routes=parse_routes(args.route),
        provider_override=args.provider,
        visual_ids=parse_csv(args.visual_id),
        asset_classes=parse_csv(args.asset_class),
        limit=args.limit,
        manifest_path=str(manifest_path),
        incoming_root=str(incoming_root),
        config_path=str(config_path) if config_path is not None else "",
        request_catalog=request_catalog,
        request_catalog_path=str(request_catalog_path) if request_catalog_path is not None else DEFAULT_REQUEST_CATALOG,
    )
    print(
        f"[PLAN] action={execution_plan['Action']} groups={len(execution_plan['Groups'])} "
        f"blocked={len(execution_plan['Blocked'])}"
    )
    for item in execution_plan["Blocked"]:
        print(f"[BLOCKED] {item['VisualID']} {item['Reason']}")
    for group in execution_plan["Groups"]:
        print(
            f"[GROUP] class={group['AssetClass']} route={group.get('Capability') or group['Provider']} "
            f"count={len(group['VisualIDs'])} batch={group['BatchID']}"
        )
        print("[GENERATE] " + subprocess.list2cmdline(group["GenerationCommand"]))
        print("[PROCESS] " + subprocess.list2cmdline(group["ProcessingCommand"]))
    if args.dry_run:
        print("[DONE] dry-run only; no files changed.")
        return 0 if args.allow_blocked or not execution_plan["Blocked"] else 2
    if not execution_plan["Groups"]:
        raise RuntimeError("No executable art batch groups.")
    if execution_plan["Blocked"] and not args.allow_blocked:
        raise RuntimeError("Batch has blocked items; filter them or pass --allow-blocked.")

    production_run_id = args.production_run_id or execution_plan["BatchID"]
    run_dir = log_root / production_run_id
    if run_dir.exists():
        raise FileExistsError(f"Production run already exists: {run_dir}")
    write_json(
        run_dir / "request.json",
        {
            "ProductionRunID": production_run_id,
            "PlanPath": repo_path(plan_path),
            "ManifestPath": repo_path(manifest_path),
            "ClaimCeiling": "selected",
            "ApprovedAllowed": False,
        },
    )
    write_json(run_dir / "production-plan.json", execution_plan)

    group_results: list[dict[str, Any]] = []
    generation_results_by_id: dict[str, dict[str, Any]] = {}
    for group in execution_plan["Groups"]:
        generation = subprocess.run(group["GenerationCommand"], cwd=PROJECT_ROOT)
        processing_return_code = None
        item_generation = collect_generation_results(group, manifest_path, incoming_root)
        generation_results_by_id.update({item["VisualID"]: item for item in item_generation})
        successful_ids = [item["VisualID"] for item in item_generation if item["State"] == "succeeded"]
        if generation.returncode == 0 and successful_ids:
            processing_command = command_with_visual_ids(group["ProcessingCommand"], successful_ids)
            processing = subprocess.run(processing_command, cwd=PROJECT_ROOT)
            processing_return_code = processing.returncode
        group_results.append(
            {
                "BatchID": group["BatchID"],
                "VisualIDs": group["VisualIDs"],
                "GenerationExitCode": generation.returncode,
                "ProcessingExitCode": processing_return_code,
                "GenerationItems": item_generation,
            }
        )
    write_json(run_dir / "generation-summary.json", {"Groups": group_results})
    technical_items = collect_processing_results(
        execution_plan,
        manifest_path,
        incoming_root,
        generation_results=generation_results_by_id,
    )
    write_json(run_dir / "technical-review.json", {"ProductionRunID": production_run_id, "Items": technical_items})
    failed_groups = [
        item
        for item in group_results
        if item["GenerationExitCode"] != 0
        or any(result["State"] != "succeeded" for result in item["GenerationItems"])
        or (any(result["State"] == "succeeded" for result in item["GenerationItems"]) and item["ProcessingExitCode"] != 0)
    ]
    review_ready = any(item["Claim"] == "review_required" for item in technical_items)
    summary = {
        "ProductionRunID": production_run_id,
        "FinalState": (
            "review_required"
            if not failed_groups
            else "review_required_with_failures"
            if review_ready
            else "generation_or_processing_failed"
        ),
        "Claims": {item["VisualID"]: item["Claim"] for item in technical_items},
        "Blocked": execution_plan["Blocked"],
        "ApprovedChanged": False,
        "UnityChanged": False,
        "RegistryChanged": False,
        "NextAction": "Agent reviews latest processed rounds and writes visual-review.json before guarded selection.",
    }
    write_json(run_dir / "summary.json", summary)
    print(f"[DONE] run={production_run_id} state={summary['FinalState']}")
    return 1 if failed_groups else 0


if __name__ == "__main__":
    raise SystemExit(main())
