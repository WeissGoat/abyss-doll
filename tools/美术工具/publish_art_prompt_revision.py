# -*- coding: utf-8 -*-
"""Publish immutable Agent-authored PromptRevisions into the Request Catalog."""

from __future__ import annotations

import argparse
import copy
import json
from pathlib import Path
from typing import Any

from art_prompt_revision import publish_prompt_revision


DEFAULT_CATALOG = "美术文档/_generated/art_generation_requests.json"


def read_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(f".{path.name}.tmp")
    temporary.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    temporary.replace(path)


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
    if not dry_run:
        write_json(catalog_path, updated)
    return {"Catalog": updated, "Published": published, "Activated": activate, "DryRun": dry_run}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Publish P3 Agent-authored art PromptRevisions.")
    parser.add_argument("--request-catalog", default=DEFAULT_CATALOG)
    parser.add_argument("--revision-path", required=True)
    parser.add_argument("--no-activate", action="store_true")
    parser.add_argument("--dry-run", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    result = publish_revision_file(
        Path(args.request_catalog).resolve(),
        Path(args.revision_path).resolve(),
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
