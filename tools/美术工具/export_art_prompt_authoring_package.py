# -*- coding: utf-8 -*-
"""Export deterministic art requirement contexts for Agent prompt authoring."""

from __future__ import annotations

import argparse
import copy
import json
from datetime import datetime
from pathlib import Path
from typing import Any


DEFAULT_CATALOG = "美术文档/_generated/art_generation_requests.json"


def read_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(f".{path.name}.tmp")
    temporary.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    temporary.replace(path)


def _revision_summaries(request: dict[str, Any]) -> list[dict[str, Any]]:
    result = []
    for revision in request.get("PromptRevisions", []):
        if not isinstance(revision, dict):
            continue
        variants = revision.get("Variants", {}) if isinstance(revision.get("Variants"), dict) else {}
        result.append(
            {
                "PromptRevisionID": revision.get("PromptRevisionID", ""),
                "RevisionFingerprint": revision.get("RevisionFingerprint", ""),
                "Status": revision.get("Status", ""),
                "Formats": {
                    format_id: variant.get("Status", "")
                    for format_id, variant in variants.items()
                    if isinstance(variant, dict)
                },
                "CreatedAt": revision.get("CreatedAt", ""),
            }
        )
    return result


def export_authoring_package(
    catalog: dict[str, Any],
    visual_ids: list[str] | None = None,
) -> dict[str, Any]:
    by_visual = {
        str(request.get("VisualID", "")): request
        for request in catalog.get("Requests", [])
        if isinstance(request, dict) and str(request.get("VisualID", ""))
    }
    selected = [item for item in (visual_ids or []) if item]
    if not selected:
        selected = [
            visual_id
            for visual_id, request in by_visual.items()
            if request.get("RequirementStatus") == "ready"
            and request.get("PromptAuthoringStatus") == "prompt_authoring_required"
        ]

    items = []
    for visual_id in selected:
        request = by_visual.get(visual_id)
        if not isinstance(request, dict):
            raise ValueError(f"compiled_request_missing:{visual_id}")
        items.append(
            {
                "RequestID": request.get("RequestID", ""),
                "VisualID": visual_id,
                "ProductionProfile": request.get("ProductionProfile", "standard_asset"),
                "RequirementFingerprint": request.get("RequirementFingerprint", ""),
                "RequirementStatus": request.get("RequirementStatus", ""),
                "PromptAuthoringStatus": request.get("PromptAuthoringStatus", ""),
                "PromptAuthoringContext": copy.deepcopy(request.get("PromptAuthoringContext", {})),
                "TechnicalRequest": copy.deepcopy(request.get("TechnicalRequest", {})),
                "PreservationContract": copy.deepcopy(request.get("PreservationContract", {})),
                "ActivePromptRevisionID": request.get("ActivePromptRevisionID", ""),
                "ExistingPromptRevisions": _revision_summaries(request),
            }
        )
    return {
        "Version": 1,
        "GeneratedAt": datetime.now().astimezone().isoformat(timespec="seconds"),
        "CatalogVersion": catalog.get("Version"),
        "CompilerVersion": catalog.get("CompilerVersion"),
        "Items": items,
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Export P3 art PromptRevision authoring contexts.")
    parser.add_argument("--request-catalog", default=DEFAULT_CATALOG)
    parser.add_argument("--visual-id", action="append", default=[])
    parser.add_argument("--output-path", default="")
    parser.add_argument("--dry-run", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    catalog_path = Path(args.request_catalog).resolve()
    visual_ids = [part.strip() for value in args.visual_id for part in value.split(",") if part.strip()]
    package = export_authoring_package(read_json(catalog_path), visual_ids)
    if args.dry_run or not args.output_path:
        print(json.dumps(package, ensure_ascii=False, indent=2))
        if args.dry_run:
            print("No files changed.")
        return 0
    output_path = Path(args.output_path).resolve()
    write_json(output_path, package)
    print(json.dumps({"OutputPath": str(output_path), "Items": len(package["Items"])}, ensure_ascii=False))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
