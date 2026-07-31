# -*- coding: utf-8 -*-
"""Register Agent-produced processing evidence as an immutable numeric round."""

from __future__ import annotations

import argparse
import copy
import hashlib
import json
import shutil
from pathlib import Path
from typing import Any

from PIL import Image

from art_background import review_candidate
from art_processing import load_round_decision, next_round_number, publish_round, reserve_round
from art_workspace import normalize_entry_workspace_paths, workspace_path


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]
DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_INCOMING_ROOT = "UnityClient/Assets/Art/_IncomingAI"
IMAGE_EXTENSIONS = {".png", ".jpg", ".jpeg", ".webp"}
FORBIDDEN_KEYS = {"selectedpath", "approvedpath", "registrystatus", "runtimepath", "runtimestate"}
TECHNICAL_REVIEW_SCHEMA = "technical_review_v2"
TECHNICAL_RULESET_VERSION = "p3_art_technical_rules_002"
TECHNICAL_OVERRIDE_SCHEMA = "technical_override_v1"
OVERRIDABLE_RULES = {"subject_outside_safe_canvas": "accept_as_warning"}


def resolve_project_path(value: str | Path) -> Path:
    path = Path(value)
    return path if path.is_absolute() else PROJECT_ROOT / path


def read_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, data: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def _technical_review_payload(review: dict[str, Any]) -> dict[str, Any]:
    payload = {
        key: copy.deepcopy(review[key])
        for key in ("File", "SHA256", "Status", "Reasons", "Warnings", "Metrics", "NineSliceMetrics")
        if key in review
    }
    return payload


def review_fingerprint(review: dict[str, Any]) -> str:
    encoded = json.dumps(
        _technical_review_payload(review),
        ensure_ascii=False,
        sort_keys=True,
        separators=(",", ":"),
    )
    return hashlib.sha256(encoded.encode("utf-8")).hexdigest()


def _spec_section(entry: dict[str, Any], name: str) -> dict[str, Any]:
    spec = entry.get("Spec") if isinstance(entry.get("Spec"), dict) else {}
    value = spec.get(name)
    return value if isinstance(value, dict) else {}


def recalculate_candidate_review(entry: dict[str, Any], path: Path) -> dict[str, Any]:
    with Image.open(path) as image:
        image.load()
        review = review_candidate(
            image,
            source_spec=_spec_section(entry, "SourceSpec"),
            composition_spec=_spec_section(entry, "CompositionSpec"),
            process_spec=_spec_section(entry, "ProcessSpec"),
            asset_type=str(entry.get("AssetType", "") or ""),
            production_profile=str(entry.get("ProductionProfile", "standard_asset") or "standard_asset"),
            saved_path=path,
        )
    record = {
        "File": path.name,
        "SHA256": hashlib.sha256(path.read_bytes()).hexdigest(),
        **review,
    }
    record["ReviewFingerprint"] = review_fingerprint(record)
    return record


def _technical_review_map(report: dict[str, Any], visual_id: str, production_profile: str) -> dict[str, dict[str, Any]]:
    if report.get("SchemaVersion") != TECHNICAL_REVIEW_SCHEMA:
        raise ValueError("technical_review_schema_invalid")
    if report.get("RuleSetVersion") != TECHNICAL_RULESET_VERSION:
        raise ValueError("technical_review_ruleset_invalid")
    if report.get("VisualID") != visual_id:
        raise ValueError("technical_review_visual_id_mismatch")
    if report.get("ProductionProfile") != production_profile:
        raise ValueError("technical_review_profile_mismatch")
    candidates = report.get("Candidates")
    if not isinstance(candidates, list):
        raise ValueError("technical_review_candidates_invalid")
    result: dict[str, dict[str, Any]] = {}
    for item in candidates:
        if not isinstance(item, dict) or not str(item.get("File", "")):
            raise ValueError("technical_review_candidate_invalid")
        filename = str(item["File"])
        if filename in result:
            raise ValueError(f"technical_review_candidate_duplicate:{filename}")
        result[filename] = item
    return result


def _load_technical_override(staging_dir: Path) -> dict[str, Any] | None:
    path = staging_dir / "technical_override.json"
    if not path.exists():
        return None
    payload = read_json(path)
    if not isinstance(payload, dict) or payload.get("SchemaVersion") != TECHNICAL_OVERRIDE_SCHEMA:
        raise ValueError("technical_override_schema_invalid")
    for item in payload.get("Overrides", []):
        if not isinstance(item, dict):
            continue
        for value in item.get("Evidence", []):
            evidence_path = Path(str(value))
            if evidence_path.name != str(value) or not (staging_dir / evidence_path.name).is_file():
                raise ValueError(f"technical_override_evidence_invalid:{value}")
    return payload


def _apply_technical_override(
    *,
    automatic: dict[str, Any],
    override: dict[str, Any] | None,
    visual_id: str,
    allowed_override_rules: set[str],
) -> tuple[str, list[str], list[str]]:
    reasons = [str(value) for value in automatic.get("Reasons", [])]
    if override is None:
        return str(automatic.get("Status", "failed")), [], reasons
    if override.get("VisualID") != visual_id or override.get("Candidate") != automatic.get("File"):
        raise ValueError("technical_override_candidate_mismatch")
    if str(override.get("CandidateSHA256", "")).lower() != str(automatic.get("SHA256", "")).lower():
        raise ValueError("technical_override_candidate_sha_mismatch")
    if override.get("BaseReviewFingerprint") != automatic.get("ReviewFingerprint"):
        raise ValueError("technical_override_base_review_mismatch")
    overrides = override.get("Overrides")
    if not isinstance(overrides, list) or not overrides:
        raise ValueError("technical_override_rules_missing")

    applied: list[str] = []
    remaining = list(reasons)
    for item in overrides:
        if not isinstance(item, dict):
            raise ValueError("technical_override_rule_invalid")
        rule_id = str(item.get("RuleID", "") or "")
        action = str(item.get("Action", "") or "")
        if OVERRIDABLE_RULES.get(rule_id) != action:
            raise ValueError(f"technical_override_not_allowed:{rule_id}:{action}")
        if rule_id not in allowed_override_rules:
            raise PermissionError(f"technical_override_authorization_required:{rule_id}")
        if rule_id not in remaining:
            raise ValueError(f"technical_override_rule_not_failed:{rule_id}")
        if not str(item.get("Reason", "") or "").strip():
            raise ValueError(f"technical_override_reason_missing:{rule_id}")
        evidence = item.get("Evidence")
        if not isinstance(evidence, list) or not any(str(value).strip() for value in evidence):
            raise ValueError(f"technical_override_evidence_missing:{rule_id}")
        remaining.remove(rule_id)
        applied.append(rule_id)

    status = "failed" if remaining else "passed"
    return status, applied, remaining


def validate_technical_evidence(
    *,
    staging_dir: Path,
    entry: dict[str, Any],
    decision: dict[str, Any],
    allowed_override_rules: set[str],
) -> tuple[dict[str, Any], dict[str, Any]]:
    visual_id = str(entry.get("VisualID", "") or "")
    production_profile = str(entry.get("ProductionProfile", "standard_asset") or "standard_asset")
    submitted_report = read_json(staging_dir / "technical_review.json")
    submitted_by_file = _technical_review_map(submitted_report, visual_id, production_profile)
    override = _load_technical_override(staging_dir)
    if override is not None and len(decision["Candidates"]) != 1:
        raise ValueError("technical_override_requires_single_candidate")
    decision_files = {str(candidate.get("File", "")) for candidate in decision["Candidates"]}
    if set(submitted_by_file) != decision_files:
        raise ValueError("technical_review_candidate_set_mismatch")

    canonical_report = {
        "SchemaVersion": TECHNICAL_REVIEW_SCHEMA,
        "RuleSetVersion": TECHNICAL_RULESET_VERSION,
        "VisualID": visual_id,
        "ProductionProfile": production_profile,
        "Candidates": [],
    }
    canonical_decision = copy.deepcopy(decision)
    has_visual_review = (staging_dir / "visual_review.json").is_file()
    effective_statuses: list[str] = []
    for candidate in canonical_decision["Candidates"]:
        filename = str(candidate["File"])
        submitted = submitted_by_file.get(filename)
        if submitted is None:
            raise ValueError(f"technical_review_candidate_missing:{filename}")
        automatic = recalculate_candidate_review(entry, staging_dir / filename)
        if (
            _technical_review_payload(submitted) != _technical_review_payload(automatic)
            or submitted.get("ReviewFingerprint") != automatic.get("ReviewFingerprint")
        ):
            raise ValueError(f"technical_review_mismatch:{filename}")

        effective_status, applied, remaining_reasons = _apply_technical_override(
            automatic=automatic,
            override=override,
            visual_id=visual_id,
            allowed_override_rules=allowed_override_rules,
        )
        requested_status = str(candidate.get("Status", "") or "")
        if automatic["Status"] == "warning" and requested_status == "passed":
            if not has_visual_review:
                raise ValueError(f"technical_warning_visual_review_required:{filename}")
            effective_status = "passed"
        if requested_status != effective_status:
            raise ValueError(
                f"technical_decision_mismatch:{filename}:requested={requested_status}:effective={effective_status}"
            )
        candidate["AutomaticStatus"] = automatic["Status"]
        candidate["AppliedOverrides"] = applied
        candidate["Reasons"] = remaining_reasons
        candidate["Warnings"] = automatic.get("Warnings", [])
        candidate["Status"] = effective_status
        effective_statuses.append(effective_status)
        canonical_report["Candidates"].append(automatic)

    round_reasons = canonical_decision.get("Reasons")
    has_round_blockers = isinstance(round_reasons, list) and any(str(value).strip() for value in round_reasons)
    expected_state = (
        "failed"
        if has_round_blockers
        else "passed"
        if "passed" in effective_statuses
        else "decision_required"
        if "warning" in effective_statuses
        else "failed"
    )
    if canonical_decision.get("State") != expected_state:
        raise ValueError(
            f"technical_round_state_mismatch:requested={canonical_decision.get('State')}:effective={expected_state}"
        )
    canonical_decision["State"] = expected_state
    return canonical_decision, canonical_report


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
    allowed_override_rules: set[str] | None = None,
) -> dict[str, Any]:
    entry = require_unique_manifest_entry(manifest_path, visual_id)
    workspace = workspace_path(incoming_root, entry)
    decision = load_round_decision(staging_dir)
    validate_staging_files(staging_dir, decision, str(entry.get("ProductionProfile", "standard_asset")))
    canonical_decision, canonical_review = validate_technical_evidence(
        staging_dir=staging_dir,
        entry=entry,
        decision=decision,
        allowed_override_rules=allowed_override_rules or set(),
    )
    round_number = next_round_number(workspace / "processed")
    if dry_run:
        return {
            "VisualID": visual_id,
            "RoundNumber": round_number,
            "State": canonical_decision["State"],
            "DryRun": True,
        }

    reservation = reserve_round(workspace / "processed")
    try:
        copy_staging_files(staging_dir, reservation.temp_dir)
        write_json(reservation.temp_dir / "technical_review.json", canonical_review)
        write_json(reservation.temp_dir / "decision.json", canonical_decision)
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
    parser.add_argument("--allow-technical-override", action="append", default=[])
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
        allowed_override_rules={str(value).strip() for value in args.allow_technical_override if str(value).strip()},
    )
    print(f"[OK] VisualID={result['VisualID']} round={result['RoundNumber']} state={result['State']} dry_run={result['DryRun']}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
