# -*- coding: utf-8 -*-
"""Plan and optionally execute a bounded P3 character portrait AssetSet."""

from __future__ import annotations

import argparse
import json
import subprocess
import sys
from datetime import datetime
from pathlib import Path
from typing import Any


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]
DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_REQUEST_CATALOG = "美术文档/_generated/art_generation_requests.json"
DEFAULT_LOG_ROOT = "UnityClient/Logs/P3ArtProduction"


def read_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


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


def _select_format(request: dict[str, Any], prompt_format: str) -> str:
    variants = request.get("PromptVariants", {}) if isinstance(request.get("PromptVariants"), dict) else {}
    if prompt_format != "auto":
        variant = variants.get(prompt_format)
        if not isinstance(variant, dict) or variant.get("CompileStatus") != "ready":
            raise ValueError(f"prompt_variant_not_ready:{prompt_format}:{request.get('VisualID', '')}")
        return prompt_format
    for format_id in ("natural_language_v1", "danbooru_tags_v1"):
        variant = variants.get(format_id)
        if isinstance(variant, dict) and variant.get("CompileStatus") == "ready":
            return format_id
    raise ValueError(f"prompt_variant_missing:{request.get('VisualID', '')}")


def build_portrait_set_plan(
    manifest: dict[str, Any],
    request_catalog: dict[str, Any],
    asset_set_id: str,
    *,
    prompt_format: str = "auto",
    visual_ids: set[str] | None = None,
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
        if request.get("RequestFingerprint") != pointer.get("RequestFingerprint"):
            errors.append(f"compiled_request_fingerprint_mismatch:{visual_id}")
            continue
        if request.get("CompileStatus") != "ready":
            errors.append(f"compiled_request_not_ready:{visual_id}")
            continue
        try:
            selected_format = _select_format(request, prompt_format)
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
                "RequestID": request["RequestID"],
                "RequestFingerprint": request["RequestFingerprint"],
                "PromptFormat": selected_format,
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


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Plan or execute a bounded P3 character portrait set.")
    parser.add_argument("--asset-set-id", required=True)
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--request-catalog-path", default=DEFAULT_REQUEST_CATALOG)
    parser.add_argument("--prompt-format", choices=["auto", "natural_language_v1", "danbooru_tags_v1"], default="auto")
    parser.add_argument("--visual-id", action="append", default=[])
    parser.add_argument("--provider", default="")
    parser.add_argument("--config", default="")
    parser.add_argument("--variants", type=int, default=2)
    parser.add_argument("--execute-limit", type=int, default=1)
    parser.add_argument("--production-run-id", default="")
    parser.add_argument("--dry-run", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    manifest_path = resolve_project_path(args.manifest_path)
    catalog_path = resolve_project_path(args.request_catalog_path)
    manifest = read_json(manifest_path)
    catalog = read_json(catalog_path)
    visual_ids = {part.strip() for value in args.visual_id for part in value.split(",") if part.strip()}
    plan = build_portrait_set_plan(
        manifest,
        catalog,
        args.asset_set_id,
        prompt_format=args.prompt_format,
        visual_ids=visual_ids,
    )
    print(json.dumps(plan, ensure_ascii=False, indent=2))
    if args.dry_run:
        return 0 if plan["State"] == "ready" else 2
    if plan["State"] != "ready":
        return 2
    if not args.provider:
        print("[BLOCKED] provider_required_for_execution")
        return 2
    run_id = args.production_run_id or f"portrait_set_{args.asset_set_id}_{datetime.now().strftime('%Y%m%d_%H%M%S')}"
    run_dir = resolve_project_path(DEFAULT_LOG_ROOT) / run_id
    if run_dir.exists():
        raise FileExistsError(run_dir)
    write_json(run_dir / "portrait-set-plan.json", plan)
    for item in plan["Items"][: max(1, args.execute_limit)]:
        command = [
            sys.executable,
            str(SCRIPT_DIR / "run_art_generation.py"),
            "--manifest-path",
            str(manifest_path),
            "--request-catalog",
            str(catalog_path),
            "--provider",
            args.provider,
            "--prompt-format",
            item["PromptFormat"],
            "--visual-id",
            item["VisualID"],
            "--status",
            item["Status"],
            "--variants",
            str(args.variants),
            "--batch-id",
            run_id,
        ]
        if args.config:
            command.extend(["--config", args.config])
        if item["Status"] in {"approved", "registered", "validated"}:
            command.append("--preserve-status")
        completed = subprocess.run(command, cwd=PROJECT_ROOT)
        if completed.returncode != 0:
            return completed.returncode
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
