# -*- coding: utf-8 -*-
"""Guarded promotion from the latest processed round into selected/."""

from __future__ import annotations

import argparse
import hashlib
import json
import shutil
import uuid
from pathlib import Path
from typing import Any

from art_processing import IMAGE_EXTENSIONS, numeric_round_directories, resolve_latest_processed_candidate_file
from art_workspace import normalize_entry_workspace_paths, workspace_path


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]
DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_INCOMING_ROOT = "UnityClient/Assets/Art/_IncomingAI"
PRESERVED_MAIN_STATUSES = {"approved", "registered", "validated"}


def read_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(f".{path.name}.{uuid.uuid4().hex}.tmp")
    temporary.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    temporary.replace(path)


def repo_path(path: Path, project_root: Path) -> str:
    try:
        return path.resolve().relative_to(project_root.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def resolve_path(value: str, project_root: Path) -> Path:
    path = Path(value)
    return path if path.is_absolute() else project_root / path


def is_within(path: Path, directory: Path) -> bool:
    try:
        path.resolve().relative_to(directory.resolve())
        return True
    except ValueError:
        return False


def find_review_item(payload: dict[str, Any], visual_id: str) -> dict[str, Any]:
    items = payload.get("Items")
    if not isinstance(items, list):
        raise ValueError("visual review must contain an Items list")
    matches = [item for item in items if isinstance(item, dict) and item.get("VisualID") == visual_id]
    if len(matches) != 1:
        raise ValueError(f"visual review must contain exactly one item for {visual_id}")
    return matches[0]


def review_score(item: dict[str, Any]) -> int:
    scores = item.get("Scores") if isinstance(item.get("Scores"), dict) else {}
    value = scores.get("Total", item.get("Score"))
    try:
        return int(value)
    except (TypeError, ValueError) as exc:
        raise ValueError("visual review score is missing") from exc


def selected_target(entry: dict[str, Any], workspace: Path, candidate: Path, project_root: Path) -> Path:
    selected_dir = workspace / "selected"
    existing_value = str(entry.get("SelectedPath", "") or "")
    if existing_value:
        existing = resolve_path(existing_value, project_root)
        if is_within(existing, selected_dir):
            return existing

    legacy_images = sorted(
        path
        for path in selected_dir.iterdir()
        if path.is_file() and path.suffix.lower() in IMAGE_EXTENSIONS
    ) if selected_dir.is_dir() else []
    if len(legacy_images) == 1:
        return legacy_images[0]
    if len(legacy_images) > 1:
        candidates = ", ".join(path.name for path in legacy_images)
        raise ValueError(f"selected_target_ambiguous: {candidates}")
    return selected_dir / f"{entry['VisualID']}{candidate.suffix.lower()}"


def atomic_copy(source: Path, target: Path) -> None:
    target.parent.mkdir(parents=True, exist_ok=True)
    temporary = target.with_name(f".{target.name}.{uuid.uuid4().hex}.tmp")
    shutil.copy2(source, temporary)
    temporary.replace(target)


def update_run_summary(summary_path: Path, visual_id: str) -> None:
    if not summary_path.exists():
        return
    summary = read_json(summary_path)
    claims = summary.get("Claims") if isinstance(summary.get("Claims"), dict) else {}
    claims[visual_id] = "selected"
    summary["Claims"] = claims
    values = set(str(value) for value in claims.values())
    if values and values <= {"selected"}:
        summary["FinalState"] = "selection_complete"
        summary["NextAction"] = "Selected batch is complete; Approved synchronization requires a separate authorization."
    elif "review_required" in values:
        summary["FinalState"] = "selection_in_progress"
    else:
        summary["FinalState"] = "selection_complete_with_failures"
    summary["ApprovedChanged"] = False
    write_json(summary_path, summary)


def select_art_candidate(
    *,
    project_root: Path,
    manifest_path: Path,
    incoming_root: Path,
    visual_id: str,
    review_path: Path,
    allow_selected_overwrite: bool,
    dry_run: bool,
) -> dict[str, Any]:
    manifest = read_json(manifest_path)
    entries = [entry for entry in manifest.get("Entries", []) if isinstance(entry, dict)]
    matches = [entry for entry in entries if entry.get("VisualID") == visual_id]
    if len(matches) != 1:
        raise ValueError(f"Manifest must contain exactly one entry for {visual_id}")
    entry = normalize_entry_workspace_paths(matches[0])
    index = entries.index(matches[0])
    entries[index] = entry
    manifest["Entries"] = entries
    workspace = workspace_path(incoming_root, entry)

    review = read_json(review_path)
    item = find_review_item(review, visual_id)
    if str(item.get("HardGate", "")) != "passed":
        raise ValueError("visual review hard gate did not pass")
    if str(item.get("RecommendedAction", "")) not in {"select", "auto_select"}:
        raise ValueError("visual review does not recommend selection")
    score = review_score(item)
    if score < 88:
        raise ValueError(f"visual review score {score} is below selected threshold 88")
    candidate_value = str(item.get("Candidate", "") or "")
    if not candidate_value:
        raise ValueError("visual review candidate is missing")
    reviewed_candidate = resolve_path(candidate_value, project_root).resolve()

    rounds = numeric_round_directories(workspace / "processed")
    if not rounds:
        raise ValueError("latest processed round is missing")
    round_number, round_dir = rounds[-1]
    if reviewed_candidate.parent != round_dir.resolve():
        raise ValueError("reviewed candidate is not from the latest processed round")
    resolution = resolve_latest_processed_candidate_file(workspace, reviewed_candidate.name)
    if resolution.path is None or resolution.path.resolve() != reviewed_candidate:
        raise ValueError(f"reviewed candidate failed processing evidence validation: {resolution.reason}")

    target = selected_target(entry, workspace, reviewed_candidate, project_root)
    source_hash = hashlib.sha256(reviewed_candidate.read_bytes()).hexdigest()
    target_hash = hashlib.sha256(target.read_bytes()).hexdigest() if target.exists() else ""
    if target.exists() and target_hash != source_hash and not allow_selected_overwrite:
        raise PermissionError(f"selected overwrite authorization required: {repo_path(target, project_root)}")

    decision = {
        "ProductionRunID": str(review.get("ProductionRunID", "") or ""),
        "State": "planned" if dry_run else "selected",
        "VisualID": visual_id,
        "ProcessedRound": round_number,
        "Candidate": repo_path(reviewed_candidate, project_root),
        "SelectedPath": repo_path(target, project_root),
        "Score": score,
        "SHA256": source_hash,
        "ReviewPath": repo_path(review_path, project_root),
        "ApprovedChanged": False,
    }
    if dry_run:
        return decision

    if not target.exists() or target_hash != source_hash:
        atomic_copy(reviewed_candidate, target)
    entry["SelectedPath"] = repo_path(target, project_root)
    if str(entry.get("Status", "")) not in PRESERVED_MAIN_STATUSES:
        entry["Status"] = "selected"
    write_json(manifest_path, manifest)
    write_json(workspace / "production_decision.json", decision)

    selection_path = review_path.parent / "selection-decision.json"
    selection_payload = read_json(selection_path) if selection_path.exists() else {
        "ProductionRunID": decision["ProductionRunID"],
        "Selected": [],
        "Rejected": [],
    }
    selected_items = [
        existing
        for existing in selection_payload.get("Selected", [])
        if isinstance(existing, dict) and existing.get("VisualID") != visual_id
    ]
    selected_items.append(
        {
            "VisualID": visual_id,
            "Source": decision["Candidate"],
            "Target": decision["SelectedPath"],
            "Score": score,
            "Reason": str(item.get("Reason") or "Agent review passed selected threshold."),
        }
    )
    selection_payload["Selected"] = sorted(selected_items, key=lambda value: str(value.get("VisualID", "")))
    write_json(selection_path, selection_payload)
    update_run_summary(review_path.parent / "summary.json", visual_id)
    return decision


def main() -> int:
    parser = argparse.ArgumentParser(description="Promote one reviewed latest-round candidate into selected/.")
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--incoming-root", default=DEFAULT_INCOMING_ROOT)
    parser.add_argument("--visual-id", required=True)
    parser.add_argument("--review-path", required=True)
    parser.add_argument("--allow-selected-overwrite", action="store_true")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    manifest_path = resolve_path(args.manifest_path, PROJECT_ROOT)
    incoming_root = resolve_path(args.incoming_root, PROJECT_ROOT)
    review_path = resolve_path(args.review_path, PROJECT_ROOT)
    decision = select_art_candidate(
        project_root=PROJECT_ROOT,
        manifest_path=manifest_path,
        incoming_root=incoming_root,
        visual_id=args.visual_id,
        review_path=review_path,
        allow_selected_overwrite=args.allow_selected_overwrite,
        dry_run=args.dry_run,
    )
    print(json.dumps(decision, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
