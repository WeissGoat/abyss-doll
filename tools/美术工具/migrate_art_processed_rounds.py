# -*- coding: utf-8 -*-
"""Guarded one-time migration from flat processed files to processed/1/."""

from __future__ import annotations

import argparse
import hashlib
import json
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Any

from PIL import Image

from art_workspace import normalize_entry_workspace_paths, workspace_path


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]
DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_INCOMING_ROOT = "UnityClient/Assets/Art/_IncomingAI"
IMAGE_EXTENSIONS = {".png", ".jpg", ".jpeg", ".webp"}


@dataclass(frozen=True)
class FileMove:
    source: Path
    destination: Path
    sha256: str


@dataclass(frozen=True)
class WorkspaceMigration:
    visual_id: str
    workspace: Path
    moves: list[FileMove]
    round_state: str
    selected_path_before: str
    selected_path_after: str


def resolve_project_path(value: str | Path) -> Path:
    path = Path(value)
    return path if path.is_absolute() else PROJECT_ROOT / path


def repo_path(path: Path) -> str:
    resolved = path.resolve(strict=False)
    try:
        return resolved.relative_to(PROJECT_ROOT.resolve()).as_posix()
    except ValueError:
        return resolved.as_posix()


def read_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def sha256_file(path: Path) -> str:
    return hashlib.sha256(path.read_bytes()).hexdigest()


def _is_within(path: Path, directory: Path) -> bool:
    try:
        path.resolve(strict=False).relative_to(directory.resolve(strict=False))
        return True
    except ValueError:
        return False


def _legacy_round_state(workspace: Path) -> tuple[str, list[str]]:
    decision_path = workspace / "production_decision.json"
    if not decision_path.exists():
        return "legacy_unverified", ["legacy_candidate_requires_review"]
    try:
        decision = read_json(decision_path)
    except (OSError, json.JSONDecodeError):
        return "legacy_unverified", ["legacy_production_decision_invalid"]
    hard_gate = decision.get("hard_gate")
    if isinstance(hard_gate, dict) and hard_gate.get("status") == "failed":
        code = str(hard_gate.get("code", "legacy_hard_gate_failed") or "legacy_hard_gate_failed")
        return "failed", [code]
    return "legacy_unverified", ["legacy_candidate_requires_review"]


def collect_workspace_migrations(
    manifest: dict[str, Any],
    incoming_root: Path,
) -> list[WorkspaceMigration]:
    entries = manifest.get("Entries")
    if not isinstance(entries, list):
        raise ValueError("Manifest Entries must be a list.")
    migrations: list[WorkspaceMigration] = []
    seen_workspaces: set[Path] = set()
    for raw_entry in entries:
        if not isinstance(raw_entry, dict):
            continue
        entry = normalize_entry_workspace_paths(raw_entry)
        workspace = workspace_path(incoming_root, entry)
        workspace_key = workspace.resolve(strict=False)
        if workspace_key in seen_workspaces:
            continue
        seen_workspaces.add(workspace_key)
        processed = workspace / "processed"
        if not processed.exists():
            continue
        flat_images = sorted(
            path for path in processed.iterdir() if path.is_file() and path.suffix.lower() in IMAGE_EXTENSIONS
        )
        if not flat_images:
            continue
        round_one = processed / "1"
        if round_one.exists():
            raise ValueError(f"Cannot migrate {entry['VisualID']}: round 1 already exists alongside flat files.")

        moves: list[FileMove] = []
        image_sources: set[Path] = set()
        for source in flat_images:
            with Image.open(source) as image:
                image.verify()
            image_sources.add(source.resolve(strict=False))
            moves.append(FileMove(source, round_one / source.name, sha256_file(source)))
            meta = source.with_name(source.name + ".meta")
            if meta.exists() and meta.is_file():
                moves.append(FileMove(meta, round_one / meta.name, sha256_file(meta)))

        selected_before = str(entry.get("SelectedPath", "") or "")
        selected_after = selected_before
        if selected_before:
            selected_path = resolve_project_path(selected_before).resolve(strict=False)
            if selected_path in image_sources:
                selected_after = repo_path(round_one / selected_path.name)

        round_state, _ = _legacy_round_state(workspace)
        migrations.append(
            WorkspaceMigration(
                visual_id=str(entry["VisualID"]),
                workspace=workspace,
                moves=moves,
                round_state=round_state,
                selected_path_before=selected_before,
                selected_path_after=selected_after,
            )
        )
    return migrations


def plan_migration(
    *,
    manifest_path: Path,
    incoming_root: Path,
) -> tuple[dict[str, Any], list[WorkspaceMigration]]:
    manifest = read_json(manifest_path)
    migrations = collect_workspace_migrations(manifest, incoming_root)
    return manifest, migrations


def _validate_migration(migration: WorkspaceMigration) -> None:
    round_one = migration.workspace / "processed" / "1"
    if round_one.exists():
        raise ValueError(f"Cannot migrate {migration.visual_id}: round 1 already exists.")
    for move in migration.moves:
        if not _is_within(move.source, migration.workspace / "processed"):
            raise ValueError(f"Migration source escapes workspace: {move.source}")
        if not _is_within(move.destination, round_one):
            raise ValueError(f"Migration destination escapes round 1: {move.destination}")
        if not move.source.is_file() or sha256_file(move.source) != move.sha256:
            raise ValueError(f"Migration source hash changed: {move.source}")


def apply_workspace_migrations(
    manifest: dict[str, Any],
    migrations: list[WorkspaceMigration],
) -> dict[str, Any]:
    for migration in migrations:
        _validate_migration(migration)

    entries = manifest.get("Entries", [])
    selected_rewrites = 0
    moved_files = 0
    moved_meta = 0
    for migration in migrations:
        round_one = migration.workspace / "processed" / "1"
        round_one.mkdir(parents=True)
        for move in migration.moves:
            move.source.replace(move.destination)
            if sha256_file(move.destination) != move.sha256:
                raise RuntimeError(f"Migration destination hash mismatch: {move.destination}")
            moved_files += 1
            if move.destination.name.endswith(".meta"):
                moved_meta += 1

        state, reasons = _legacy_round_state(migration.workspace)
        candidate_status = "failed" if state == "failed" else "legacy_unverified"
        candidates = []
        for move in migration.moves:
            if move.destination.suffix.lower() not in IMAGE_EXTENSIONS:
                continue
            with Image.open(move.destination) as image:
                width, height = image.size
                image_format = str(image.format or move.destination.suffix.lstrip(".")).lower()
            candidates.append(
                {
                    "File": move.destination.name,
                    "Status": candidate_status,
                    "SHA256": move.sha256,
                    "Width": width,
                    "Height": height,
                    "Format": image_format,
                    "Reasons": reasons,
                }
            )
        write_json(round_one / "decision.json", {"State": state, "Candidates": candidates, "Reasons": reasons})
        write_json(
            round_one / "process_report.json",
            {"VisualID": migration.visual_id, "MigratedFromFlatProcessed": True, "State": state},
        )
        write_json(
            migration.workspace / "process_report.json",
            {"LatestRound": 1, "LatestState": state, "Rounds": [{"Number": 1, "State": state}]},
        )

        if migration.selected_path_after != migration.selected_path_before:
            for entry in entries:
                if isinstance(entry, dict) and entry.get("VisualID") == migration.visual_id:
                    if str(entry.get("SelectedPath", "") or "") == migration.selected_path_before:
                        entry["SelectedPath"] = migration.selected_path_after
                        selected_rewrites += 1
                    break

    return {
        "WorkspaceCount": len(migrations),
        "MovedFileCount": moved_files,
        "MovedMetaCount": moved_meta,
        "SelectedPathRewriteCount": selected_rewrites,
        "LegacyUnverifiedCount": sum(1 for item in migrations if item.round_state == "legacy_unverified"),
        "FailedCount": sum(1 for item in migrations if item.round_state == "failed"),
        "ConflictCount": 0,
    }


def execute_migration(
    *,
    manifest_path: Path,
    manifest: dict[str, Any],
    migrations: list[WorkspaceMigration],
) -> dict[str, Any]:
    summary = apply_workspace_migrations(manifest, migrations)
    write_json(manifest_path, manifest)
    return summary


def migration_summary(migrations: list[WorkspaceMigration]) -> dict[str, Any]:
    return {
        "WorkspaceCount": len(migrations),
        "FlatImageCount": sum(
            1 for migration in migrations for move in migration.moves if move.destination.suffix.lower() in IMAGE_EXTENSIONS
        ),
        "MetaCount": sum(1 for migration in migrations for move in migration.moves if move.destination.name.endswith(".meta")),
        "SelectedPathRewriteCount": sum(
            1 for migration in migrations if migration.selected_path_after != migration.selected_path_before
        ),
        "LegacyUnverifiedCount": sum(1 for item in migrations if item.round_state == "legacy_unverified"),
        "FailedCount": sum(1 for item in migrations if item.round_state == "failed"),
        "ConflictCount": 0,
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Migrate flat P3 processed art into numeric round 1.")
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--incoming-root", default=DEFAULT_INCOMING_ROOT)
    parser.add_argument("--execute", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    manifest_path = resolve_project_path(args.manifest_path)
    incoming_root = resolve_project_path(args.incoming_root)
    manifest, migrations = plan_migration(manifest_path=manifest_path, incoming_root=incoming_root)
    summary = execute_migration(manifest_path=manifest_path, manifest=manifest, migrations=migrations) if args.execute else migration_summary(migrations)
    print(json.dumps({"Execute": args.execute, "Summary": summary}, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
