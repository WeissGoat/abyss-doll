# -*- coding: utf-8 -*-
"""Sync selected art candidates into Approved paths from the manifest."""

from __future__ import annotations

import argparse
import json
import shutil
from datetime import datetime
from pathlib import Path
from typing import Any


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]
DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_IN_ROOT = "UnityClient/Assets/Art/_IncomingAI"
IMAGE_EXTENSIONS = {".png", ".jpg", ".jpeg", ".webp"}


def repo_path(path: Path) -> str:
    try:
        return path.resolve().relative_to(PROJECT_ROOT.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def resolve_project_path(value: str | None, default: str) -> Path:
    raw = value or default
    path = Path(raw)
    return path if path.is_absolute() else PROJECT_ROOT / path


def read_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, data: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def split_filters(values: list[str]) -> set[str]:
    result: set[str] = set()
    for value in values:
        for part in value.split(","):
            part = part.strip()
            if part:
                result.add(part)
    return result


def select_entries(entries: list[dict[str, Any]], args: argparse.Namespace) -> list[dict[str, Any]]:
    domains = split_filters(args.domain)
    visual_ids = split_filters(args.visual_id)
    priorities = split_filters(args.priority)
    selected: list[dict[str, Any]] = []
    for entry in entries:
        if args.status and entry.get("Status") != args.status:
            continue
        if args.batch_id and entry.get("BatchID") != args.batch_id:
            continue
        if domains and str(entry.get("Domain", "")) not in domains:
            continue
        if visual_ids and str(entry.get("VisualID", "")) not in visual_ids:
            continue
        if priorities and str(entry.get("Priority", "")) not in priorities:
            continue
        if not entry.get("VisualID") or not entry.get("OutputPath"):
            continue
        selected.append(entry)
        if args.limit and len(selected) >= args.limit:
            break
    return selected


def append_note(entry: dict[str, Any], message: str) -> None:
    existing = str(entry.get("Notes", "") or "").strip()
    entry["Notes"] = f"{existing}\n{message}".strip() if existing else message


def first_image(path: Path) -> Path | None:
    if not path.exists():
        return None
    images = sorted(item for item in path.iterdir() if item.is_file() and item.suffix.lower() in IMAGE_EXTENSIONS)
    return images[0] if images else None


def choose_source(workspace: Path, allow_processed_fallback: bool) -> tuple[Path | None, str]:
    selected = first_image(workspace / "selected")
    if selected:
        return selected, "selected"
    if allow_processed_fallback:
        processed = first_image(workspace / "processed")
        if processed:
            return processed, "processed"
    return None, ""


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Sync selected art candidates to Approved output paths.")
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--in-root", default=DEFAULT_IN_ROOT)
    parser.add_argument("--status", default="")
    parser.add_argument("--domain", action="append", default=[])
    parser.add_argument("--visual-id", action="append", default=[])
    parser.add_argument("--priority", action="append", default=[])
    parser.add_argument("--batch-id", default="")
    parser.add_argument("--limit", type=int, default=0)
    parser.add_argument("--allow-processed-fallback", action="store_true")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--overwrite", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    manifest_path = resolve_project_path(args.manifest_path, DEFAULT_MANIFEST)
    in_root = resolve_project_path(args.in_root, DEFAULT_IN_ROOT)
    manifest = read_json(manifest_path)
    entries = manifest.get("Entries", [])
    if not isinstance(entries, list):
        raise ValueError("Manifest Entries must be a list.")

    selected = select_entries(entries, args)
    print(f"[PLAN] selected_entries={len(selected)} batch={args.batch_id or '<any>'} status={args.status or '<any>'}")
    plan: list[tuple[dict[str, Any], Path, Path, str]] = []
    for entry in selected:
        workspace = in_root / entry["VisualID"]
        source, source_kind = choose_source(workspace, args.allow_processed_fallback)
        if source is None:
            print(f"[SKIP] {entry['VisualID']} no selected image")
            continue
        target = resolve_project_path(entry["OutputPath"], entry["OutputPath"])
        print(f"[ITEM] {entry['VisualID']} {source_kind}={repo_path(source)} -> {repo_path(target)}")
        plan.append((entry, source, target, source_kind))

    if args.dry_run:
        print("[DONE] dry-run only; no files changed.")
        return 0

    created_at = datetime.now().astimezone().isoformat(timespec="seconds")
    synced = 0
    for entry, source, target, source_kind in plan:
        if target.exists() and not args.overwrite:
            print(f"[SKIP] {entry['VisualID']} target exists; use --overwrite")
            continue
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)
        entry["SelectedPath"] = repo_path(source)
        entry["ApprovedPath"] = repo_path(target)
        entry["Status"] = "approved"
        append_note(entry, f"[{created_at}] approved from {source_kind}: {repo_path(source)}")
        synced += 1
        print(f"[OK] {entry['VisualID']} approved={repo_path(target)}")

    write_json(manifest_path, manifest)
    print(f"[DONE] synced={synced}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
