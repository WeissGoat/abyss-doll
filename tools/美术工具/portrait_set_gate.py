# -*- coding: utf-8 -*-
"""Current-snapshot gates for character portrait sets."""

from __future__ import annotations

import argparse
import copy
import hashlib
import json
import uuid
from pathlib import Path
from typing import Any

from PIL import Image, ImageDraw

from art_processing import resolve_selected_or_processed_candidate
from art_workspace import normalize_entry_workspace_paths, workspace_path


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]
REVIEW_SCHEMA = "p3-portrait-set-review@1"
MEMBER_CHECKS = ("Identity", "Costume", "Proportion", "Framing", "Technical", "TargetFit", "RapidSwitch")
CROSS_GROUP_CHECKS = ("Identity", "Costume", "Proportion", "RenderingDirection")
PASSING_CHECK_STATUSES = {"passed", "passed_with_risk"}


def canonical_json(value: Any) -> str:
    return json.dumps(value, ensure_ascii=False, sort_keys=True, separators=(",", ":"))


def read_json(path: Path) -> dict[str, Any]:
    payload = json.loads(path.read_text(encoding="utf-8-sig"))
    if not isinstance(payload, dict):
        raise ValueError(f"JSON object required: {path}")
    return payload


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(f".{path.name}.{uuid.uuid4().hex}.tmp")
    temporary.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    temporary.replace(path)


def repo_path(path: Path, project_root: Path) -> str:
    try:
        return path.resolve().relative_to(project_root.resolve()).as_posix()
    except ValueError:
        return str(path.resolve())


def resolve_path(value: str, project_root: Path) -> Path:
    path = Path(value)
    return path if path.is_absolute() else project_root / path


def inferred_project_root(manifest_path: Path, incoming_root: Path) -> Path:
    for parent in (incoming_root, *incoming_root.parents):
        if parent.name == "UnityClient":
            return parent.parent
    return manifest_path.parent


def selected_member_path(entry: dict[str, Any], incoming_root: Path, project_root: Path) -> Path | None:
    workspace = workspace_path(incoming_root, entry)
    selected_value = str(entry.get("SelectedPath", "") or "").strip()
    manifest_selected = resolve_path(selected_value, project_root) if selected_value else None
    resolution = resolve_selected_or_processed_candidate(
        workspace,
        manifest_selected_path=manifest_selected,
    )
    if resolution.path is None or resolution.source_kind not in {"manifest_selected", "selected"}:
        return None
    return resolution.path


def build_set_snapshot(
    manifest: dict[str, Any],
    asset_set_id: str,
    incoming_root: Path,
    project_root: Path,
) -> dict[str, Any]:
    asset_sets = manifest.get("AssetSets")
    if not isinstance(asset_sets, dict) or not isinstance(asset_sets.get(asset_set_id), dict):
        raise ValueError(f"portrait_set_missing:{asset_set_id}")
    asset_set = asset_sets[asset_set_id]
    identity_contract = asset_set.get("IdentityContract")
    if not isinstance(identity_contract, dict):
        raise ValueError(f"portrait_set_identity_contract_missing:{asset_set_id}")

    entries = manifest.get("Entries")
    if not isinstance(entries, list):
        raise ValueError("portrait_set_manifest_entries_invalid")
    members: list[dict[str, Any]] = []
    known_visual_ids: set[str] = set()
    for source_entry in entries:
        if not isinstance(source_entry, dict) or source_entry.get("AssetSetID") != asset_set_id:
            continue
        entry = normalize_entry_workspace_paths(source_entry)
        if entry.get("ProductionProfile") != "character_portrait_set":
            continue
        visual_id = str(entry.get("VisualID", "") or "").strip()
        group = str(entry.get("PresentationGroup", "") or "").strip()
        if not visual_id or not group or visual_id in known_visual_ids:
            raise ValueError(f"portrait_set_member_invalid:{asset_set_id}")
        known_visual_ids.add(visual_id)
        selected = selected_member_path(entry, incoming_root, project_root)
        if selected is None:
            raise ValueError(f"portrait_set_member_selected_missing:{visual_id}")
        members.append(
            {
                "VisualID": visual_id,
                "SelectedPath": str(selected.resolve()),
                "SelectedSHA256": hashlib.sha256(selected.read_bytes()).hexdigest(),
                "PresentationGroup": group,
            }
        )
    if not members:
        raise ValueError(f"portrait_set_members_missing:{asset_set_id}")

    members.sort(key=lambda item: item["VisualID"])
    fingerprint_payload = {
        "AssetSetID": asset_set_id,
        "IdentityContract": copy.deepcopy(identity_contract),
        "Members": [
            {
                "VisualID": item["VisualID"],
                "SelectedSHA256": item["SelectedSHA256"],
                "PresentationGroup": item["PresentationGroup"],
            }
            for item in members
        ],
    }
    return {
        "AssetSetID": asset_set_id,
        "IdentityContract": copy.deepcopy(identity_contract),
        "Members": members,
        "PresentationGroups": sorted({item["PresentationGroup"] for item in members}),
        "SetSnapshotFingerprint": hashlib.sha256(canonical_json(fingerprint_payload).encode("utf-8")).hexdigest(),
    }


def draw_member_grid(members: list[dict[str, Any]], output_path: Path, *, cell_size: tuple[int, int]) -> None:
    cell_width, cell_height = cell_size
    label_height = 36
    columns = min(4, len(members))
    rows = (len(members) + columns - 1) // columns
    canvas = Image.new("RGBA", (columns * cell_width, rows * (cell_height + label_height)), (28, 31, 39, 255))
    draw = ImageDraw.Draw(canvas)
    for index, member in enumerate(members):
        x = (index % columns) * cell_width
        y = (index // columns) * (cell_height + label_height)
        with Image.open(member["SelectedPath"]) as source:
            image = source.convert("RGBA")
            image.thumbnail((cell_width - 12, cell_height - 12))
            offset = (x + (cell_width - image.width) // 2, y + (cell_height - image.height) // 2)
            canvas.alpha_composite(image, offset)
        draw.text((x + 4, y + cell_height + 4), f"{member['VisualID']} [{member['PresentationGroup']}]", fill=(240, 240, 240, 255))
    output_path.parent.mkdir(parents=True, exist_ok=True)
    canvas.convert("RGB").save(output_path)


def baseline_paths(snapshot: dict[str, Any], baseline_map_path: Path | None, project_root: Path, incoming_root: Path) -> dict[str, Path]:
    values: dict[str, str] = {}
    if baseline_map_path is not None:
        loaded = read_json(baseline_map_path)
        values.update({str(key): str(value) for key, value in loaded.items() if isinstance(value, str)})
    for member in snapshot["Members"]:
        workspace = incoming_root / "character_portraits" / member["VisualID"]
        decision_path = workspace / "production_decision.json"
        if not decision_path.is_file():
            continue
        try:
            decision = read_json(decision_path)
            previous = decision.get("PreviousSelected")
            evidence_path = previous.get("EvidencePath") if isinstance(previous, dict) else None
            if isinstance(evidence_path, str) and evidence_path.strip():
                values.setdefault(member["VisualID"], evidence_path)
        except (OSError, json.JSONDecodeError, ValueError):
            continue
    return {
        visual_id: path
        for visual_id, value in values.items()
        for path in [resolve_path(value, project_root)]
        if path.is_file()
    }


def draw_replacement_comparisons(snapshot: dict[str, Any], baselines: dict[str, Path], output_path: Path) -> bool:
    pairs = [member for member in snapshot["Members"] if member["VisualID"] in baselines]
    if not pairs:
        return False
    cell_width, cell_height, label_height = 180, 220, 28
    canvas = Image.new("RGBA", (cell_width * 2, len(pairs) * (cell_height + label_height)), (28, 31, 39, 255))
    draw = ImageDraw.Draw(canvas)
    for index, member in enumerate(pairs):
        y = index * (cell_height + label_height)
        for column, path in enumerate((baselines[member["VisualID"]], Path(member["SelectedPath"]))):
            with Image.open(path) as source:
                image = source.convert("RGBA")
                image.thumbnail((cell_width - 12, cell_height - 12))
                canvas.alpha_composite(image, (column * cell_width + (cell_width - image.width) // 2, y + (cell_height - image.height) // 2))
        draw.text((4, y + cell_height + 4), f"{member['VisualID']} previous / current", fill=(240, 240, 240, 255))
    output_path.parent.mkdir(parents=True, exist_ok=True)
    canvas.convert("RGB").save(output_path)
    return True


def prepare_set_review(
    *,
    manifest_path: Path,
    incoming_root: Path,
    evidence_root: Path,
    asset_set_id: str,
    production_run_id: str,
    baseline_map_path: Path | None = None,
) -> dict[str, Any]:
    manifest = read_json(manifest_path)
    project_root = inferred_project_root(manifest_path, incoming_root)
    snapshot = build_set_snapshot(manifest, asset_set_id, incoming_root, project_root)
    gate_root = evidence_root / production_run_id / "portrait-set-gate"
    contact_sheet = gate_root / "set-contact-sheet.png"
    small_strip = gate_root / "small-size-strip.png"
    draw_member_grid(snapshot["Members"], contact_sheet, cell_size=(240, 320))
    draw_member_grid(snapshot["Members"], small_strip, cell_size=(96, 128))
    evidence = {
        "ContactSheet": str(contact_sheet.resolve()),
        "SmallSizeStrip": str(small_strip.resolve()),
    }
    comparisons = gate_root / "replacement-comparisons.png"
    baselines = baseline_paths(snapshot, baseline_map_path, project_root, incoming_root)
    if draw_replacement_comparisons(snapshot, baselines, comparisons):
        evidence["ReplacementComparisons"] = str(comparisons.resolve())
    prepared = {
        "Schema": "p3-portrait-set-prepare@1",
        "ProductionRunID": production_run_id,
        "State": "review_required",
        **snapshot,
        "Evidence": evidence,
    }
    write_json(gate_root / "prepare.json", prepared)
    return prepared


def validate_review(snapshot: dict[str, Any], review: dict[str, Any], asset_set_id: str, production_run_id: str) -> None:
    if review.get("Schema") != REVIEW_SCHEMA:
        raise ValueError("portrait_set_review_schema_invalid")
    if review.get("ProductionRunID") != production_run_id or review.get("AssetSetID") != asset_set_id:
        raise ValueError("portrait_set_review_scope_mismatch")
    if review.get("SetSnapshotFingerprint") != snapshot["SetSnapshotFingerprint"]:
        raise ValueError("portrait_set_review_stale")
    if review.get("State") != "passed":
        raise ValueError("portrait_set_review_not_passed")
    if review.get("FailedMembers") != []:
        raise ValueError("portrait_set_review_failed_members_present")

    expected_members = {
        (item["VisualID"], item["SelectedSHA256"], item["PresentationGroup"])
        for item in snapshot["Members"]
    }
    members = review.get("Members")
    if not isinstance(members, list):
        raise ValueError("portrait_set_review_members_invalid")
    actual_members = {
        (str(item.get("VisualID", "")), str(item.get("SelectedSHA256", "")), str(item.get("PresentationGroup", "")))
        for item in members
        if isinstance(item, dict)
    }
    if len(members) != len(expected_members) or actual_members != expected_members:
        raise ValueError("portrait_set_review_members_mismatch")
    if any(item.get("Status") != "passed" or not str(item.get("Finding", "")).strip() for item in members if isinstance(item, dict)):
        raise ValueError("portrait_set_review_members_not_passed")

    groups = review.get("Groups")
    if not isinstance(groups, dict):
        raise ValueError("portrait_set_review_groups_invalid")
    for group in snapshot["PresentationGroups"]:
        checks = groups.get(group, {}).get("Checks") if isinstance(groups.get(group), dict) else None
        if not isinstance(checks, dict) or any(name not in checks for name in MEMBER_CHECKS):
            raise ValueError("portrait_set_review_group_checks_missing")
        for name in MEMBER_CHECKS:
            check = checks[name]
            if not isinstance(check, dict) or check.get("Status") not in PASSING_CHECK_STATUSES or not str(check.get("Finding", "")).strip():
                raise ValueError("portrait_set_review_group_check_invalid")

    cross_group_checks = review.get("CrossGroupChecks")
    if not isinstance(cross_group_checks, dict) or any(name not in cross_group_checks for name in CROSS_GROUP_CHECKS):
        raise ValueError("portrait_set_review_cross_group_checks_missing")
    for name in CROSS_GROUP_CHECKS:
        check = cross_group_checks[name]
        if not isinstance(check, dict) or check.get("Status") not in PASSING_CHECK_STATUSES or not str(check.get("Finding", "")).strip():
            raise ValueError("portrait_set_review_cross_group_check_invalid")


def finalize_set_review(
    *,
    manifest_path: Path,
    incoming_root: Path,
    asset_set_id: str,
    production_run_id: str,
    review_path: Path,
) -> dict[str, Any]:
    manifest = read_json(manifest_path)
    project_root = inferred_project_root(manifest_path, incoming_root)
    snapshot = build_set_snapshot(manifest, asset_set_id, incoming_root, project_root)
    review = read_json(review_path)
    validate_review(snapshot, review, asset_set_id, production_run_id)
    manifest["AssetSets"][asset_set_id]["LatestConsistencyReview"] = {
        "State": "passed",
        "SetSnapshotFingerprint": snapshot["SetSnapshotFingerprint"],
        "ProductionRunID": production_run_id,
        "EvidencePath": repo_path(review_path, project_root),
    }
    write_json(manifest_path, manifest)
    return manifest["AssetSets"][asset_set_id]["LatestConsistencyReview"]


def require_current_set_review(
    *,
    manifest: dict[str, Any],
    asset_set_id: str,
    incoming_root: Path,
    project_root: Path,
) -> dict[str, Any]:
    snapshot = build_set_snapshot(manifest, asset_set_id, incoming_root, project_root)
    latest = manifest["AssetSets"][asset_set_id].get("LatestConsistencyReview")
    if not isinstance(latest, dict) or latest.get("State") != "passed":
        raise ValueError(f"portrait_set_review_missing:{asset_set_id}")
    if latest.get("SetSnapshotFingerprint") != snapshot["SetSnapshotFingerprint"]:
        raise ValueError(f"portrait_set_review_stale:{asset_set_id}")
    return copy.deepcopy(latest)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Prepare, finalize, or check a current character portrait set review.")
    parser.add_argument("--phase", choices=("prepare", "finalize", "check"), required=True)
    parser.add_argument("--manifest-path", required=True)
    parser.add_argument("--incoming-root", required=True)
    parser.add_argument("--evidence-root", default="UnityClient/Logs/P3ArtProduction")
    parser.add_argument("--asset-set-id", required=True)
    parser.add_argument("--production-run-id", default="")
    parser.add_argument("--review-path", default="")
    parser.add_argument("--baseline-map", default="")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    manifest_path = resolve_path(args.manifest_path, PROJECT_ROOT)
    incoming_root = resolve_path(args.incoming_root, PROJECT_ROOT)
    if args.phase == "prepare":
        if not args.production_run_id:
            raise ValueError("production_run_id_required")
        result = prepare_set_review(
            manifest_path=manifest_path,
            incoming_root=incoming_root,
            evidence_root=resolve_path(args.evidence_root, PROJECT_ROOT),
            asset_set_id=args.asset_set_id,
            production_run_id=args.production_run_id,
            baseline_map_path=resolve_path(args.baseline_map, PROJECT_ROOT) if args.baseline_map else None,
        )
    elif args.phase == "finalize":
        if not args.production_run_id or not args.review_path:
            raise ValueError("production_run_id_and_review_path_required")
        result = finalize_set_review(
            manifest_path=manifest_path,
            incoming_root=incoming_root,
            asset_set_id=args.asset_set_id,
            production_run_id=args.production_run_id,
            review_path=resolve_path(args.review_path, PROJECT_ROOT),
        )
    else:
        manifest = read_json(manifest_path)
        result = require_current_set_review(
            manifest=manifest,
            asset_set_id=args.asset_set_id,
            incoming_root=incoming_root,
            project_root=inferred_project_root(manifest_path, incoming_root),
        )
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
