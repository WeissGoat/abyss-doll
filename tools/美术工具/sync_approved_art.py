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
    return json.loads(path.read_text(encoding="utf-8-sig"))


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
        if args.candidate_batch_id and entry.get("CandidateBatchID") != args.candidate_batch_id:
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


def first_candidate_processed(workspace: Path, entry: dict[str, Any]) -> Path | None:
    candidate_stems = candidate_raw_stems(entry)
    if not candidate_stems:
        return None
    candidates = [
        path
        for path in (workspace / "processed").iterdir()
        if path.is_file() and path.suffix.lower() in IMAGE_EXTENSIONS and path.stem in candidate_stems
    ] if (workspace / "processed").exists() else []
    return sorted(candidates)[0] if candidates else None


def first_candidate_selected(workspace: Path, entry: dict[str, Any]) -> Path | None:
    candidate_stems = candidate_raw_stems(entry)
    if not candidate_stems:
        return None
    selected_dir = workspace / "selected"
    if not selected_dir.exists():
        return None
    candidates = [
        path
        for path in selected_dir.iterdir()
        if path.is_file() and path.suffix.lower() in IMAGE_EXTENSIONS and path.stem in candidate_stems
    ]
    return sorted(candidates)[0] if candidates else None


def candidate_raw_stems(entry: dict[str, Any]) -> set[str]:
    raw_files = entry.get("CandidateRawFiles")
    if not isinstance(raw_files, list):
        return set()
    stems: set[str] = set()
    for value in raw_files:
        if not isinstance(value, str) or not value.strip():
            continue
        raw_path = resolve_project_path(value, value)
        stems.add(raw_path.stem)
    return stems


def choose_candidate_source(
    workspace: Path,
    entry: dict[str, Any],
    allow_processed_fallback: bool,
) -> tuple[Path | None, str]:
    selected = first_candidate_selected(workspace, entry)
    if selected:
        return selected, "selected"
    if allow_processed_fallback:
        processed = first_candidate_processed(workspace, entry)
        if processed:
            return processed, "candidate_processed"
    return None, ""


def unity_meta_path(asset_path: Path) -> Path:
    return asset_path.with_name(asset_path.name + ".meta")


def read_meta_guard(target: Path, require_existing: bool) -> bytes | None:
    meta_path = unity_meta_path(target)
    if require_existing and not target.exists():
        raise FileNotFoundError(
            f"Visual V2 replacement target does not exist; refusing to create a new Unity asset path: {repo_path(target)}"
        )
    if require_existing and not meta_path.exists():
        raise FileNotFoundError(
            f"Visual V2 replacement target has no Unity .meta; refusing to replace without GUID guard: {repo_path(meta_path)}"
        )
    return meta_path.read_bytes() if meta_path.exists() else None


def verify_meta_guard(target: Path, before: bytes | None) -> None:
    if before is None:
        return
    meta_path = unity_meta_path(target)
    if not meta_path.exists():
        raise FileNotFoundError(f"Unity .meta disappeared during sync: {repo_path(meta_path)}")
    after = meta_path.read_bytes()
    if after != before:
        raise RuntimeError(f"Unity .meta changed during sync; GUID guard failed: {repo_path(meta_path)}")


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Sync selected art candidates to Approved output paths.")
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--in-root", default=DEFAULT_IN_ROOT)
    parser.add_argument("--status", default="")
    parser.add_argument("--domain", action="append", default=[])
    parser.add_argument("--visual-id", action="append", default=[])
    parser.add_argument("--priority", action="append", default=[])
    parser.add_argument("--batch-id", default="")
    parser.add_argument("--candidate-batch-id", default="")
    parser.add_argument("--quality-tier", default="")
    parser.add_argument("--limit", type=int, default=0)
    parser.add_argument("--allow-processed-fallback", action="store_true")
    parser.add_argument("--clear-candidate", action="store_true")
    parser.add_argument(
        "--allow-new-target-with-candidate",
        action="store_true",
        help="Allow CandidateBatchID sync to create a new target asset. Default is strict replacement with existing .meta.",
    )
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
    plan: list[tuple[dict[str, Any], Path, Path, str, bytes | None]] = []
    for entry in selected:
        workspace = in_root / entry["VisualID"]
        if args.candidate_batch_id:
            source, source_kind = choose_candidate_source(workspace, entry, args.allow_processed_fallback)
        else:
            source, source_kind = choose_source(workspace, args.allow_processed_fallback)
        if source is None:
            print(f"[SKIP] {entry['VisualID']} no selected image")
            continue
        target = resolve_project_path(entry["OutputPath"], entry["OutputPath"])
        require_existing_meta = bool(args.candidate_batch_id and not args.allow_new_target_with_candidate)
        meta_before = read_meta_guard(target, require_existing_meta)
        meta_note = "meta_guard=strict" if require_existing_meta else ("meta_guard=preserve" if meta_before else "meta_guard=none")
        print(f"[ITEM] {entry['VisualID']} {source_kind}={repo_path(source)} -> {repo_path(target)} {meta_note}")
        plan.append((entry, source, target, source_kind, meta_before))

    if args.dry_run:
        print("[DONE] dry-run only; no files changed.")
        return 0

    created_at = datetime.now().astimezone().isoformat(timespec="seconds")
    synced = 0
    for entry, source, target, source_kind, meta_before in plan:
        if target.exists() and not args.overwrite:
            print(f"[SKIP] {entry['VisualID']} target exists; use --overwrite")
            continue
        target.parent.mkdir(parents=True, exist_ok=True)
        shutil.copy2(source, target)
        verify_meta_guard(target, meta_before)
        entry["SelectedPath"] = repo_path(source)
        entry["ApprovedPath"] = repo_path(target)
        entry["Status"] = "approved"
        if args.quality_tier:
            entry["QualityTier"] = args.quality_tier
            entry["QualityUpdatedAt"] = created_at
            entry["ReplacementBatchID"] = args.candidate_batch_id or args.batch_id or str(entry.get("BatchID", "") or "")
        if args.clear_candidate:
            entry.pop("CandidateBatchID", None)
            entry.pop("CandidateRawFiles", None)
        append_note(entry, f"[{created_at}] approved from {source_kind}: {repo_path(source)}")
        synced += 1
        print(f"[OK] {entry['VisualID']} approved={repo_path(target)}")

    write_json(manifest_path, manifest)
    print(f"[DONE] synced={synced}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
