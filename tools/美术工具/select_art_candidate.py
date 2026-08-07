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
from portrait_review_contract import DEFAULT_PROTECTED_DIMENSIONS, validate_portrait_review_item


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


def score_map(value: Any, *, label: str) -> dict[str, int]:
    if not isinstance(value, dict):
        raise ValueError(f"{label}_scores_missing")
    result: dict[str, int] = {}
    for key, score in value.items():
        try:
            result[str(key)] = int(score)
        except (TypeError, ValueError) as exc:
            raise ValueError(f"{label}_score_invalid:{key}") from exc
    if "Total" not in result:
        raise ValueError(f"{label}_total_missing")
    return result


def validate_replacement(
    *,
    item: dict[str, Any],
    target: Path,
    target_hash: str,
    source_hash: str,
    project_root: Path,
    is_portrait: bool,
) -> tuple[dict[str, Any], dict[str, Any]]:
    if item.get("SelectionMode") != "replacement":
        raise ValueError("replacement_baseline_missing")
    if not str(item.get("ReviewRubricVersion", "") or "").strip():
        raise ValueError("replacement_review_rubric_missing")
    baseline = item.get("ReplacementBaseline")
    policy = item.get("ReplacementPolicy")
    if not isinstance(baseline, dict) or not isinstance(policy, dict):
        raise ValueError("replacement_baseline_missing")

    baseline_path_value = str(baseline.get("SelectedPath", "") or "")
    if not baseline_path_value or resolve_path(baseline_path_value, project_root).resolve() != target.resolve():
        raise ValueError("replacement_baseline_path_mismatch")
    baseline_hash = str(baseline.get("SelectedSHA256", "") or "").lower()
    if not baseline_hash or baseline_hash != target_hash.lower():
        raise ValueError("replacement_baseline_stale")
    candidate_hash = str(item.get("CandidateSHA256", "") or "").lower()
    if not candidate_hash or candidate_hash != source_hash.lower():
        raise ValueError("replacement_candidate_sha_mismatch")

    candidate_scores = score_map(item.get("Scores"), label="replacement_candidate")
    baseline_scores = score_map(baseline.get("Scores"), label="replacement_baseline")
    baseline_dimension_evidence: dict[str, Any] = {}
    if is_portrait:
        try:
            baseline_review = validate_portrait_review_item(
                {
                    "ReviewRubricVersion": item.get("ReviewRubricVersion"),
                    "Scores": baseline.get("Scores"),
                    "DimensionEvidence": baseline.get("DimensionEvidence"),
                },
                require_selection_threshold=True,
            )
        except ValueError as exc:
            raise ValueError("replacement_baseline_review_required") from exc
        candidate_scores = score_map(item.get("Scores"), label="replacement_candidate")
        baseline_scores = baseline_review["Scores"]
        baseline_dimension_evidence = baseline_review["DimensionEvidence"]
    if policy.get("MustExceedExisting") is not True:
        raise ValueError("replacement_must_exceed_existing_required")
    try:
        minimum_score = int(policy.get("MinimumScore", 88))
        minimum_delta = int(policy.get("MinimumScoreDelta", 1))
    except (TypeError, ValueError) as exc:
        raise ValueError("replacement_policy_invalid") from exc
    if minimum_delta < 1:
        raise ValueError("replacement_minimum_delta_invalid")
    required_score = max(88, minimum_score, baseline_scores["Total"] + minimum_delta)
    if candidate_scores["Total"] < required_score:
        raise ValueError(
            f"replacement_not_better:candidate={candidate_scores['Total']}:required={required_score}"
        )

    protected = policy.get("ProtectedDimensions", [])
    if not isinstance(protected, list):
        raise ValueError("replacement_protected_dimensions_invalid")
    if is_portrait and any(dimension not in protected for dimension in DEFAULT_PROTECTED_DIMENSIONS):
        raise ValueError("replacement_protected_dimensions_required")
    for dimension in protected:
        name = str(dimension)
        if name not in candidate_scores or name not in baseline_scores:
            raise ValueError(f"replacement_protected_dimension_missing:{name}")
        if candidate_scores[name] < baseline_scores[name]:
            raise ValueError(
                f"replacement_protected_dimension_regression:{name}:"
                f"candidate={candidate_scores[name]}:baseline={baseline_scores[name]}"
            )

    previous_selected = {
        "Path": repo_path(target, project_root),
        "SHA256": target_hash,
        "Score": baseline_scores["Total"],
        "Scores": baseline_scores,
    }
    if is_portrait:
        previous_selected["DimensionEvidence"] = baseline_dimension_evidence
    policy_result = {
        "ReviewRubricVersion": item["ReviewRubricVersion"],
        "MinimumScorePassed": candidate_scores["Total"] >= max(88, minimum_score),
        "StrictlyBetterPassed": candidate_scores["Total"] >= baseline_scores["Total"] + minimum_delta,
        "ProtectedDimensionsPassed": True,
        "BaselineStillCurrent": True,
        "RequiredScore": required_score,
    }
    if is_portrait:
        policy_result["ProtectedDimensions"] = list(protected)
    return previous_selected, policy_result


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


def preserve_previous_selected(
    target: Path,
    review_path: Path,
    visual_id: str,
    target_hash: str,
) -> Path:
    destination = (
        review_path.parent
        / "replacement-baselines"
        / visual_id
        / f"{target_hash}{target.suffix.lower()}"
    )
    if destination.exists() and hashlib.sha256(destination.read_bytes()).hexdigest() != target_hash:
        raise ValueError("replacement_baseline_evidence_hash_mismatch")
    if not destination.exists():
        atomic_copy(target, destination)
    return destination


def find_run_summary(review_path: Path, production_run_id: str) -> Path | None:
    """Locate the batch summary even when the batch runner nests it under batch/<run>/."""
    direct = review_path.parent / "summary.json"
    candidates = [direct]
    batch_root = review_path.parent / "batch"
    if batch_root.is_dir():
        candidates.extend(sorted(batch_root.glob("*/summary.json")))
    matches: list[Path] = []
    for candidate in candidates:
        if not candidate.is_file():
            continue
        try:
            payload = read_json(candidate)
        except (OSError, json.JSONDecodeError):
            continue
        if not production_run_id or str(payload.get("ProductionRunID", "")) == production_run_id:
            matches.append(candidate)
    if len(matches) == 1:
        return matches[0]
    return direct if direct.is_file() else None


def update_run_summary(summary_path: Path | None, visual_id: str) -> None:
    if summary_path is None or not summary_path.exists():
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
    is_portrait = entry.get("ProductionProfile") == "character_portrait_set"
    if is_portrait:
        validate_portrait_review_item(item, require_selection_threshold=True)
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
    reviewed_hash = str(item.get("CandidateSHA256", "") or "").lower()
    if reviewed_hash and reviewed_hash != source_hash.lower():
        raise ValueError("reviewed_candidate_sha_mismatch")

    selection_mode = "initial"
    previous_selected: dict[str, Any] = {}
    policy_result: dict[str, Any] = {
        "MinimumScorePassed": score >= 88,
        "RequiredScore": 88,
    }
    state = "planned" if dry_run else "selected"
    if target.exists() and target_hash == source_hash:
        state = "already_selected"
        selection_mode = "idempotent"
        policy_result = {
            "CandidateStillCurrent": True,
            "CopyRequired": False,
        }
    elif target.exists():
        selection_mode = "replacement"
        previous_selected, policy_result = validate_replacement(
            item=item,
            target=target,
            target_hash=target_hash,
            source_hash=source_hash,
            project_root=project_root,
            is_portrait=is_portrait,
        )
        if not allow_selected_overwrite:
            raise PermissionError(f"selected overwrite authorization required: {repo_path(target, project_root)}")

    if selection_mode == "replacement" and not dry_run:
        evidence_path = preserve_previous_selected(target, review_path, visual_id, target_hash)
        previous_selected["EvidencePath"] = str(evidence_path.resolve())

    decision = {
        "ProductionRunID": str(review.get("ProductionRunID", "") or ""),
        "State": state,
        "VisualID": visual_id,
        "ProcessedRound": round_number,
        "Candidate": repo_path(reviewed_candidate, project_root),
        "SelectedPath": repo_path(target, project_root),
        "SelectionMode": selection_mode,
        "PreviousSelected": previous_selected,
        "PolicyResult": policy_result,
        "Score": score,
        "SHA256": source_hash,
        "ReviewPath": repo_path(review_path, project_root),
        "ApprovedChanged": False,
    }
    if dry_run:
        return decision

    if state != "already_selected":
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
            "SHA256": source_hash,
            "SelectionMode": selection_mode,
            "PreviousSelected": previous_selected,
            "PolicyResult": policy_result,
            "Reason": str(item.get("Reason") or "Agent review passed selected threshold."),
        }
    )
    selection_payload["Selected"] = sorted(selected_items, key=lambda value: str(value.get("VisualID", "")))
    write_json(selection_path, selection_payload)
    update_run_summary(
        find_run_summary(review_path, str(review.get("ProductionRunID", "") or "")),
        visual_id,
    )
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
