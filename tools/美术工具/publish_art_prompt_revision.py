# -*- coding: utf-8 -*-
"""Publish immutable Agent-authored PromptRevisions into the Request Catalog."""

from __future__ import annotations

import argparse
import copy
import json
from pathlib import Path
from typing import Any

from art_catalog_integrity import recompute_catalog_summary
from art_prompt_revision import publish_prompt_revision


DEFAULT_CATALOG = "美术文档/_generated/art_generation_requests.json"
DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"


def read_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(f".{path.name}.tmp")
    temporary.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    temporary.replace(path)


def _synchronize_manifest(
    manifest: dict[str, Any],
    catalog: dict[str, Any],
    published_ids: list[str],
) -> dict[str, Any]:
    updated = copy.deepcopy(manifest)
    requests = {
        str(request.get("RequestID", "")): request
        for request in catalog.get("Requests", [])
        if isinstance(request, dict)
    }
    published_request_ids = {revision_id.rsplit("/prompt-", 1)[0] for revision_id in published_ids}
    entries = {
        str(entry.get("VisualID", "")): entry
        for entry in updated.get("Entries", [])
        if isinstance(entry, dict)
    }
    for request_id in published_request_ids:
        request = requests.get(request_id)
        if not isinstance(request, dict):
            raise ValueError(f"compiled_request_missing:{request_id}")
        visual_id = str(request.get("VisualID", ""))
        entry = entries.get(visual_id)
        if not isinstance(entry, dict):
            raise ValueError(f"manifest_entry_missing:{visual_id}")
        pointer = entry.get("CompiledRequest")
        if not isinstance(pointer, dict) or pointer.get("RequestID") != request_id:
            raise ValueError(f"compiled_request_pointer_mismatch:{visual_id}")
        if pointer.get("RequirementFingerprint") != request.get("RequirementFingerprint"):
            raise ValueError(f"compiled_request_fingerprint_mismatch:{visual_id}")
        pointer["PromptAuthoringStatus"] = request.get("PromptAuthoringStatus", "")
        pointer["ActivePromptRevisionID"] = request.get("ActivePromptRevisionID", "")
    return updated


def _revision_list(payload: dict[str, Any]) -> list[dict[str, Any]]:
    revisions = payload.get("Revisions")
    if isinstance(revisions, list):
        return [item for item in revisions if isinstance(item, dict)]
    return [payload]


def _request_id_for_revision(revision: dict[str, Any]) -> str:
    revision_id = str(revision.get("PromptRevisionID", ""))
    marker = "/prompt-"
    if marker not in revision_id:
        raise ValueError("prompt_revision_id_invalid")
    return revision_id.rsplit(marker, 1)[0]


def publish_revision_file(
    catalog_path: Path,
    revision_path: Path,
    *,
    manifest_path: Path | None = None,
    activate: bool = True,
    dry_run: bool = False,
) -> dict[str, Any]:
    catalog = read_json(catalog_path)
    updated = copy.deepcopy(catalog)
    revisions = _revision_list(read_json(revision_path))
    published = []
    for revision in revisions:
        request_id = _request_id_for_revision(revision)
        request_index = next(
            (
                index
                for index, request in enumerate(updated.get("Requests", []))
                if isinstance(request, dict) and request.get("RequestID") == request_id
            ),
            None,
        )
        if request_index is None:
            raise ValueError(f"compiled_request_missing:{request_id}")
        updated["Requests"][request_index] = publish_prompt_revision(
            updated["Requests"][request_index],
            revision,
            activate=activate,
        )
        published.append(revision["PromptRevisionID"])
    updated["Summary"] = recompute_catalog_summary(updated.get("Requests", []))
    updated_manifest = None
    if manifest_path is not None:
        updated_manifest = _synchronize_manifest(read_json(manifest_path), updated, published)
        if isinstance(updated.get("ManifestFingerprint"), str) and isinstance(
            updated_manifest.get("ArtStyleCatalog"), dict
        ):
            from validate_art_generation_requests import validate_request_catalog

            errors = validate_request_catalog(updated, updated_manifest, strict=True)
            if errors:
                raise ValueError(";".join(errors))
    if not dry_run:
        if manifest_path is not None and updated_manifest is not None:
            write_json(manifest_path, updated_manifest)
        write_json(catalog_path, updated)
    return {
        "Catalog": updated,
        "Manifest": updated_manifest,
        "Published": published,
        "Activated": activate,
        "DryRun": dry_run,
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Publish P3 Agent-authored art PromptRevisions.")
    parser.add_argument("--request-catalog", default=DEFAULT_CATALOG)
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--revision-path", required=True)
    parser.add_argument("--no-activate", action="store_true")
    parser.add_argument("--dry-run", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    result = publish_revision_file(
        Path(args.request_catalog).resolve(),
        Path(args.revision_path).resolve(),
        manifest_path=Path(args.manifest_path).resolve(),
        activate=not args.no_activate,
        dry_run=args.dry_run,
    )
    print(
        json.dumps(
            {
                "Published": result["Published"],
                "Activated": result["Activated"],
                "DryRun": result["DryRun"],
            },
            ensure_ascii=False,
            indent=2,
        )
    )
    if args.dry_run:
        print("No files changed.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
