# -*- coding: utf-8 -*-
"""Register Agent-produced processing evidence as an immutable numeric round."""

from __future__ import annotations

import argparse
import hashlib
import json
import shutil
from pathlib import Path
from typing import Any

from PIL import Image

from art_processing import load_round_decision, next_round_number, publish_round, reserve_round
from art_workspace import normalize_entry_workspace_paths, workspace_path


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]
DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_INCOMING_ROOT = "UnityClient/Assets/Art/_IncomingAI"
IMAGE_EXTENSIONS = {".png", ".jpg", ".jpeg", ".webp"}
FORBIDDEN_KEYS = {"selectedpath", "approvedpath", "registrystatus", "runtimepath", "runtimestate"}


def resolve_project_path(value: str | Path) -> Path:
    path = Path(value)
    return path if path.is_absolute() else PROJECT_ROOT / path


def read_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, data: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def require_unique_manifest_entry(manifest_path: Path, visual_id: str) -> dict[str, Any]:
    payload = read_json(manifest_path)
    entries = payload.get("Entries")
    if not isinstance(entries, list):
        raise ValueError("Manifest Entries must be a list.")
    matches = [entry for entry in entries if isinstance(entry, dict) and entry.get("VisualID") == visual_id]
    if len(matches) != 1:
        raise ValueError(f"Expected exactly one Manifest entry for VisualID={visual_id}, found {len(matches)}.")
    return normalize_entry_workspace_paths(matches[0])


def _is_within(path: Path, directory: Path) -> bool:
    try:
        path.resolve(strict=False).relative_to(directory.resolve(strict=False))
        return True
    except ValueError:
        return False


def _scan_forbidden_keys(value: Any) -> str | None:
    if isinstance(value, dict):
        for key, nested in value.items():
            if str(key).lower() in FORBIDDEN_KEYS:
                return str(key)
            result = _scan_forbidden_keys(nested)
            if result:
                return result
    elif isinstance(value, list):
        for nested in value:
            result = _scan_forbidden_keys(nested)
            if result:
                return result
    return None


def _validate_report_paths(value: Any, staging_dir: Path, key: str = "") -> None:
    if isinstance(value, dict):
        for nested_key, nested in value.items():
            _validate_report_paths(nested, staging_dir, str(nested_key))
    elif isinstance(value, list):
        for nested in value:
            _validate_report_paths(nested, staging_dir, key)
    elif isinstance(value, str) and (key.lower().endswith("path") or key.lower().endswith("paths")):
        path = Path(value)
        if path.is_absolute() and not _is_within(path, staging_dir):
            raise ValueError(f"Report path escapes staging directory: {value}")
        if not path.is_absolute() and not _is_within(staging_dir / path, staging_dir):
            raise ValueError(f"Report path escapes staging directory: {value}")


def validate_staging_files(staging_dir: Path, decision: dict[str, Any], production_profile: str) -> None:
    if not staging_dir.exists() or not staging_dir.is_dir():
        raise FileNotFoundError(f"Staging directory does not exist: {staging_dir}")
    forbidden = _scan_forbidden_keys(decision)
    if forbidden:
        raise ValueError(f"Staging decision contains forbidden {forbidden}")

    for required in ("process_report.json", "technical_review.json"):
        if not (staging_dir / required).is_file():
            raise ValueError(f"Missing required staging evidence: {required}")
    if decision.get("State") == "passed" and production_profile == "character_portrait_set":
        if not (staging_dir / "visual_review.json").is_file():
            raise ValueError("Passed character_portrait_set registration requires visual_review.json")

    for report_name in ("process_report.json", "technical_review.json", "visual_review.json"):
        report_path = staging_dir / report_name
        if report_path.exists():
            _validate_report_paths(read_json(report_path), staging_dir)

    for candidate in decision["Candidates"]:
        file_value = candidate.get("File")
        if not isinstance(file_value, str) or Path(file_value).name != file_value:
            raise ValueError(f"Candidate File must be a direct child: {file_value}")
        path = staging_dir / file_value
        if not path.is_file() or path.suffix.lower() not in IMAGE_EXTENSIONS:
            raise ValueError(f"Candidate file missing or unsupported: {file_value}")
        try:
            with Image.open(path) as image:
                image.load()
                width, height = image.size
                image_format = str(image.format or "").lower()
        except OSError as exc:
            raise ValueError(f"Candidate image is not decodable: {file_value}") from exc
        digest = hashlib.sha256(path.read_bytes()).hexdigest()
        if digest.lower() != str(candidate.get("SHA256", "")).lower():
            raise ValueError(f"Candidate SHA256 mismatch: {file_value}")
        if width != int(candidate.get("Width", -1)) or height != int(candidate.get("Height", -1)):
            raise ValueError(f"Candidate dimensions mismatch: {file_value}")
        expected_format = str(candidate.get("Format", "")).lower().lstrip(".")
        if expected_format == "jpg":
            expected_format = "jpeg"
        if image_format == "jpg":
            image_format = "jpeg"
        if image_format != expected_format:
            raise ValueError(f"Candidate Format mismatch: {file_value}")


def copy_staging_files(staging_dir: Path, destination: Path) -> None:
    destination.mkdir(parents=True, exist_ok=True)
    for source in staging_dir.iterdir():
        if source.is_dir():
            raise ValueError(f"Staging evidence must be direct files: {source.name}")
        shutil.copy2(source, destination / source.name)


def refresh_root_process_report(workspace: Path) -> None:
    processed = workspace / "processed"
    rounds = []
    for number in sorted(
        (int(path.name) for path in processed.iterdir() if path.is_dir() and path.name.isascii() and path.name.isdecimal() and int(path.name) > 0),
    ) if processed.exists() else []:
        round_dir = processed / str(number)
        decision = read_json(round_dir / "decision.json") if (round_dir / "decision.json").exists() else {}
        rounds.append({"Number": number, "State": decision.get("State", "invalid")})
    latest = rounds[-1] if rounds else {"Number": None, "State": "missing"}
    write_json(
        workspace / "process_report.json",
        {"LatestRound": latest["Number"], "LatestState": latest["State"], "Rounds": rounds},
    )


def refresh_latest_contact_sheet(workspace: Path, final_dir: Path) -> None:
    source = final_dir / "contact_sheet.png"
    if not source.exists():
        return
    contact_dir = workspace / "contact_sheet"
    contact_dir.mkdir(parents=True, exist_ok=True)
    visual_id = workspace.name
    shutil.copy2(source, contact_dir / f"{visual_id}_contact_sheet.png")


def register_processing_round(
    *,
    manifest_path: Path,
    incoming_root: Path,
    visual_id: str,
    staging_dir: Path,
    dry_run: bool,
) -> dict[str, Any]:
    entry = require_unique_manifest_entry(manifest_path, visual_id)
    workspace = workspace_path(incoming_root, entry)
    decision = load_round_decision(staging_dir)
    validate_staging_files(staging_dir, decision, str(entry.get("ProductionProfile", "standard_asset")))
    round_number = next_round_number(workspace / "processed")
    if dry_run:
        return {"VisualID": visual_id, "RoundNumber": round_number, "State": decision["State"], "DryRun": True}

    reservation = reserve_round(workspace / "processed")
    try:
        copy_staging_files(staging_dir, reservation.temp_dir)
        final_dir = publish_round(reservation)
    except Exception:
        from art_processing import abandon_round

        abandon_round(reservation)
        raise
    refresh_root_process_report(workspace)
    refresh_latest_contact_sheet(workspace, final_dir)
    return {"VisualID": visual_id, "RoundNumber": reservation.number, "State": decision["State"], "DryRun": False}


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Register an Agent-produced art processing round.")
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--incoming-root", default=DEFAULT_INCOMING_ROOT)
    parser.add_argument("--visual-id", required=True)
    parser.add_argument("--staging-directory", required=True)
    parser.add_argument("--dry-run", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    result = register_processing_round(
        manifest_path=resolve_project_path(args.manifest_path),
        incoming_root=resolve_project_path(args.incoming_root),
        visual_id=args.visual_id,
        staging_dir=resolve_project_path(args.staging_directory),
        dry_run=args.dry_run,
    )
    print(f"[OK] VisualID={result['VisualID']} round={result['RoundNumber']} state={result['State']} dry_run={result['DryRun']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
