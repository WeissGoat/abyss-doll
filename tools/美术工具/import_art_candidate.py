# -*- coding: utf-8 -*-
"""Import an existing image into a Manifest-owned P3 art workspace."""

from __future__ import annotations

import argparse
import hashlib
import json
import shutil
from datetime import datetime
from pathlib import Path
from typing import Any

from PIL import Image

from art_workspace import normalize_entry_workspace_paths, workspace_path


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]
DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_INCOMING_ROOT = "UnityClient/Assets/Art/_IncomingAI"
IMAGE_EXTENSIONS = {".png", ".jpg", ".jpeg", ".webp"}


def resolve_project_path(value: str | Path) -> Path:
    path = Path(value)
    return path if path.is_absolute() else PROJECT_ROOT / path


def repo_path(path: Path) -> str:
    resolved = path.resolve()
    try:
        return resolved.relative_to(PROJECT_ROOT.resolve()).as_posix()
    except ValueError:
        return resolved.as_posix()


def read_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, data: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def sha256_file(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as stream:
        for chunk in iter(lambda: stream.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def validate_destination_name(value: str) -> str:
    name = value.strip()
    path = Path(name)
    if (
        not name
        or name in {".", ".."}
        or "/" in name
        or "\\" in name
        or path.is_absolute()
        or bool(path.drive)
        or len(path.parts) != 1
    ):
        raise ValueError(f"Destination name must be a single filename: {value}")
    if path.suffix.lower() not in IMAGE_EXTENSIONS:
        raise ValueError(f"Unsupported destination image extension: {value}")
    return name


def is_within(path: Path, directory: Path) -> bool:
    try:
        path.resolve().relative_to(directory.resolve())
        return True
    except ValueError:
        return False


def append_note(entry: dict[str, Any], message: str) -> None:
    existing = str(entry.get("Notes", "") or "").strip()
    entry["Notes"] = f"{existing}\n{message}".strip() if existing else message


def import_candidate(
    *,
    manifest_path: Path,
    incoming_root: Path,
    visual_id: str,
    source_path: Path,
    destination_name: str,
    batch_id: str,
    source_review: str,
    dry_run: bool = False,
) -> dict[str, object]:
    manifest_path = manifest_path.resolve()
    incoming_root = incoming_root.resolve()
    source_path = source_path.resolve()
    destination_name = validate_destination_name(destination_name)

    manifest = read_json(manifest_path)
    entries = manifest.get("Entries")
    if not isinstance(entries, list):
        raise ValueError("Manifest Entries must be a list.")
    matches = [
        (index, entry)
        for index, entry in enumerate(entries)
        if isinstance(entry, dict) and str(entry.get("VisualID", "") or "") == visual_id
    ]
    if len(matches) != 1:
        raise ValueError(
            f"Expected exactly one Manifest entry for VisualID={visual_id}, found {len(matches)}."
        )
    index, entry = matches[0]
    normalized_entry = normalize_entry_workspace_paths(entry)

    if not source_path.exists() or not source_path.is_file():
        raise FileNotFoundError(f"Source image does not exist: {source_path}")
    if source_path.suffix.lower() not in IMAGE_EXTENSIONS:
        raise ValueError(f"Unsupported source image extension: {source_path}")
    legacy_root = incoming_root / "_legacy_runs"
    if is_within(source_path, legacy_root):
        raise ValueError(f"Refusing to import source from _legacy_runs: {source_path}")

    with Image.open(source_path) as image:
        image.verify()
    with Image.open(source_path) as image:
        width, height = image.size
        mode = image.mode
        image_format = str(image.format or source_path.suffix.lstrip(".")).lower()

    workspace = workspace_path(incoming_root, normalized_entry).resolve()
    raw_dir = workspace / "raw"
    destination = (raw_dir / destination_name).resolve()
    if not is_within(destination, raw_dir):
        raise ValueError(f"Destination escapes resolved raw workspace: {destination}")

    source_hash = sha256_file(source_path)
    created_at = datetime.now().astimezone().isoformat(timespec="seconds")
    result: dict[str, object] = {
        "dry_run": dry_run,
        "visual_id": visual_id,
        "production_profile": normalized_entry["ProductionProfile"],
        "source": source_path,
        "destination": destination,
        "sha256": source_hash,
        "width": width,
        "height": height,
        "mode": mode,
        "format": image_format,
        "batch_id": batch_id,
    }
    if dry_run:
        return result

    raw_dir.mkdir(parents=True, exist_ok=True)
    shutil.copy2(source_path, destination)
    if sha256_file(destination) != source_hash:
        raise RuntimeError(f"Imported candidate hash mismatch: {destination}")

    reference_inputs = {
        "CreatedAt": created_at,
        "VisualID": visual_id,
        "ProductionProfile": normalized_entry["ProductionProfile"],
        "Inputs": [
            {
                "Role": "imported_candidate",
                "SourcePath": repo_path(source_path),
                "DestinationPath": repo_path(destination),
                "SHA256": source_hash,
                "Width": width,
                "Height": height,
                "Mode": mode,
                "Format": image_format,
                "SourceReview": source_review,
            }
        ],
    }
    generation = {
        "CreatedAt": created_at,
        "VisualID": visual_id,
        "BatchID": batch_id,
        "Capability": "import_existing_candidate",
        "Provider": "",
        "Model": "",
        "Outputs": [repo_path(destination)],
        "SourceReview": source_review,
    }
    write_json(workspace / "reference_inputs.json", reference_inputs)
    write_json(workspace / "generation.json", generation)

    normalized_entry["BatchID"] = batch_id
    normalized_entry["RawPath"] = repo_path(raw_dir)
    normalized_entry["Status"] = "generated"
    append_note(
        normalized_entry,
        f"[{created_at}] imported candidate from {repo_path(source_path)} to {repo_path(destination)}",
    )
    entries[index] = normalized_entry
    manifest["Entries"] = entries
    write_json(manifest_path, manifest)
    return result


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Import an existing image into a P3 art workspace.")
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--incoming-root", default=DEFAULT_INCOMING_ROOT)
    parser.add_argument("--visual-id", required=True)
    parser.add_argument("--source-path", required=True)
    parser.add_argument("--destination-name", default="r01_001.png")
    parser.add_argument("--batch-id", required=True)
    parser.add_argument("--source-review", default="")
    parser.add_argument("--dry-run", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    result = import_candidate(
        manifest_path=resolve_project_path(args.manifest_path),
        incoming_root=resolve_project_path(args.incoming_root),
        visual_id=args.visual_id,
        source_path=resolve_project_path(args.source_path),
        destination_name=args.destination_name,
        batch_id=args.batch_id,
        source_review=args.source_review,
        dry_run=args.dry_run,
    )
    prefix = "[DRY-RUN]" if args.dry_run else "[OK]"
    print(
        f"{prefix} VisualID={result['visual_id']} profile={result['production_profile']} "
        f"source={repo_path(Path(result['source']))} destination={repo_path(Path(result['destination']))} "
        f"sha256={result['sha256']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
