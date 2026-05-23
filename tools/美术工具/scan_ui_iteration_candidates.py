# -*- coding: utf-8 -*-
"""Scan UI specs, art assets, and ArtAcceptance output for UI iteration candidates."""

from __future__ import annotations

import argparse
import json
import re
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]

DEFAULT_SCREENS = "美术文档/ui_design/screen_layouts.json"
DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_ACCEPTANCE_ROOT = "UnityClient/Logs/ArtAcceptance/latest"
DEFAULT_OUTPUT_JSON = "美术文档/ui_design/_generated/ui_iteration_candidates.json"
DEFAULT_OUTPUT_MARKDOWN = "美术文档/ui_design/_generated/ui_iteration_candidates.md"

APPROVED_STATUSES = {"approved", "registered", "validated"}
REGISTERED_STATUSES = {"registered", "validated"}
STATUS_ORDER = {
    "P0": 0,
    "P1": 1,
    "P2": 2,
    "P3": 3,
}


def resolve_project_path(value: str) -> Path:
    path = Path(value)
    return path if path.is_absolute() else PROJECT_ROOT / path


def repo_path(path: Path) -> str:
    try:
        return path.resolve().relative_to(PROJECT_ROOT.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def read_json(path: Path, default: Any) -> Any:
    if not path.exists():
        return default
    return json.loads(path.read_text(encoding="utf-8-sig"))


def parse_time(value: Any) -> datetime | None:
    if not value:
        return None
    text = str(value).strip()
    if not text:
        return None
    if "T" not in text:
        text = f"{text}T00:00:00"
    text = text.replace("Z", "+00:00")
    fractional_match = re.match(r"^(.*T\d{2}:\d{2}:\d{2})\.(\d+)(.*)$", text)
    if fractional_match:
        prefix, fraction, suffix = fractional_match.groups()
        text = f"{prefix}.{fraction[:6]}{suffix}"
    try:
        result = datetime.fromisoformat(text)
    except ValueError:
        return None
    if result.tzinfo is None:
        result = result.replace(tzinfo=datetime.now().astimezone().tzinfo)
    return result


def timestamp_text() -> str:
    return datetime.now(timezone.utc).astimezone().isoformat(timespec="seconds")


def as_list(value: Any) -> list[Any]:
    return value if isinstance(value, list) else []


def unique_strings(values: list[Any]) -> list[str]:
    seen: set[str] = set()
    result: list[str] = []
    for value in values:
        text = str(value).strip()
        if text and text not in seen:
            seen.add(text)
            result.append(text)
    return result


def screen_visual_ids(screen: dict[str, Any]) -> list[str]:
    ids = unique_strings(as_list(screen.get("RequiredVisualIDs")))
    background = str(screen.get("BackgroundVisualID", "") or "").strip()
    if background and background not in ids:
        ids.append(background)
    return ids


def manifest_by_visual_id(manifest: dict[str, Any]) -> dict[str, dict[str, Any]]:
    result: dict[str, dict[str, Any]] = {}
    for entry in as_list(manifest.get("Entries")):
        if isinstance(entry, dict) and entry.get("VisualID"):
            result[str(entry["VisualID"])] = entry
    return result


def registry_by_visual_id(registry: dict[str, Any]) -> dict[str, dict[str, Any]]:
    result: dict[str, dict[str, Any]] = {}
    for entry in as_list(registry.get("Entries")):
        if isinstance(entry, dict) and entry.get("VisualID"):
            result[str(entry["VisualID"])] = entry
    return result


def capture_by_screen(report: dict[str, Any]) -> dict[str, dict[str, Any]]:
    result: dict[str, dict[str, Any]] = {}
    for capture in as_list(report.get("Captures")):
        if isinstance(capture, dict) and capture.get("ScreenTag"):
            result[str(capture["ScreenTag"])] = capture
    return result


def approved_path_for(entry: dict[str, Any]) -> Path | None:
    value = entry.get("ApprovedPath") or entry.get("OutputPath")
    if not value:
        return None
    return resolve_project_path(str(value))


def inspect_visual(
    visual_id: str,
    manifest_entries: dict[str, dict[str, Any]],
    registry_entries: dict[str, dict[str, Any]],
) -> dict[str, Any]:
    entry = manifest_entries.get(visual_id, {})
    status = str(entry.get("Status", "") or "")
    approved_path = approved_path_for(entry) if entry else None
    approved_exists = bool(approved_path and approved_path.exists())
    meta_exists = bool(approved_path and Path(str(approved_path) + ".meta").exists())
    registry_entry = registry_entries.get(visual_id)
    registry_has_sprite = bool(registry_entry and registry_entry.get("HasSprite"))
    return {
        "VisualID": visual_id,
        "ManifestStatus": status,
        "InManifest": bool(entry),
        "ApprovedPath": repo_path(approved_path) if approved_path else "",
        "ApprovedFileExists": approved_exists,
        "MetaFileExists": meta_exists,
        "IsApproved": status in APPROVED_STATUSES and approved_exists,
        "RegistryHasSprite": registry_has_sprite,
        "RegistryStatus": str(entry.get("RegistryStatus", "") or ""),
        "NeedsRegistryRefresh": status in APPROVED_STATUSES and approved_exists and not registry_has_sprite,
    }


def screenshot_path_for(capture: dict[str, Any] | None, acceptance_root: Path, screen_id: str) -> Path:
    if capture and capture.get("File"):
        return acceptance_root / str(capture["File"])
    return acceptance_root / "screenshots" / f"{screen_id}.png"


def classify_screen(
    screen: dict[str, Any],
    capture: dict[str, Any] | None,
    report: dict[str, Any],
    acceptance_root: Path,
    spec_updated_at: datetime | None,
    acceptance_finished_at: datetime | None,
    manifest_entries: dict[str, dict[str, Any]],
    registry_entries: dict[str, dict[str, Any]],
) -> dict[str, Any]:
    screen_id = str(screen.get("ScreenID", ""))
    visual_ids = screen_visual_ids(screen)
    visual_checks = [inspect_visual(visual_id, manifest_entries, registry_entries) for visual_id in visual_ids]
    missing_manifest = [item["VisualID"] for item in visual_checks if not item["InManifest"]]
    missing_approved = [item["VisualID"] for item in visual_checks if not item["IsApproved"]]
    missing_meta = [item["VisualID"] for item in visual_checks if item["IsApproved"] and not item["MetaFileExists"]]
    registry_gap = [item["VisualID"] for item in visual_checks if item["NeedsRegistryRefresh"]]

    screenshot_path = screenshot_path_for(capture, acceptance_root, screen_id)
    has_screenshot = screenshot_path.exists()
    capture_status = str(capture.get("Status", "") if capture else "")
    capture_warnings = as_list(capture.get("Warnings")) if capture else []
    capture_errors = as_list(capture.get("Errors")) if capture else []
    screenshot_stale = bool(
        has_screenshot
        and spec_updated_at
        and acceptance_finished_at
        and acceptance_finished_at < spec_updated_at
    )

    layout_status = str(screen.get("LayoutStatus", "") or "")
    structure_version = str(screen.get("StructureVersion", "") or "")
    priority = str(screen.get("Priority", "") or "")

    blockers: list[str] = []
    if missing_manifest:
        blockers.append("missing_manifest")
    if missing_approved:
        blockers.append("missing_approved")
    if missing_meta:
        blockers.append("missing_meta")
    if capture_errors:
        blockers.append("capture_errors")

    if missing_approved:
        suggested_action = "generate_or_approve_assets"
        iteration_bucket = "asset_blocked"
    elif registry_gap:
        suggested_action = "import_or_rerun_art_acceptance"
        iteration_bucket = "registry_or_acceptance_gap"
    elif not has_screenshot:
        suggested_action = "run_art_acceptance"
        iteration_bucket = "needs_runtime_screenshot"
    elif screenshot_stale:
        suggested_action = "review_existing_screenshot_then_rerun_art_acceptance"
        iteration_bucket = "viewable_but_stale"
    elif layout_status in {"active_spec", "handoff"}:
        suggested_action = "program_integrate_then_art_review"
        iteration_bucket = "program_handoff_ready"
    elif layout_status == "draft":
        suggested_action = "ui_design_iteration_review"
        iteration_bucket = "review_ready"
    elif layout_status == "validated" and structure_version != "FormalV1":
        suggested_action = "formal_v1_structure_review"
        iteration_bucket = "mvp_baseline_review_ready"
    else:
        suggested_action = "art_review"
        iteration_bucket = "review_ready"

    return {
        "ScreenID": screen_id,
        "DisplayName": str(screen.get("DisplayName", "") or ""),
        "Priority": priority,
        "LayoutStatus": layout_status,
        "StructureVersion": structure_version,
        "RequiredVisualCount": len(visual_ids),
        "MissingManifestVisualIDs": missing_manifest,
        "MissingApprovedVisualIDs": missing_approved,
        "ApprovedButMissingMetaVisualIDs": missing_meta,
        "ApprovedButNotInLatestRegistryVisualIDs": registry_gap,
        "HasScreenshot": has_screenshot,
        "ScreenshotPath": repo_path(screenshot_path) if has_screenshot else "",
        "CaptureStatus": capture_status,
        "CaptureWarnings": capture_warnings,
        "CaptureErrors": capture_errors,
        "ScreenshotStaleAgainstSpec": screenshot_stale,
        "CanReviewScreenshotNow": has_screenshot,
        "CanReviewCurrentSpecNow": has_screenshot and not screenshot_stale and not blockers and not registry_gap,
        "IterationBucket": iteration_bucket,
        "SuggestedAction": suggested_action,
        "Blockers": blockers,
    }


def classify_runtime_only_capture(
    screen_id: str,
    capture: dict[str, Any],
    acceptance_root: Path,
    spec_updated_at: datetime | None,
    acceptance_finished_at: datetime | None,
) -> dict[str, Any]:
    screenshot_path = screenshot_path_for(capture, acceptance_root, screen_id)
    has_screenshot = screenshot_path.exists()
    screenshot_stale = bool(
        has_screenshot
        and spec_updated_at
        and acceptance_finished_at
        and acceptance_finished_at < spec_updated_at
    )
    return {
        "ScreenID": screen_id,
        "HasScreenshot": has_screenshot,
        "ScreenshotPath": repo_path(screenshot_path) if has_screenshot else "",
        "CaptureStatus": str(capture.get("Status", "") or ""),
        "ScreenshotStaleAgainstSpec": screenshot_stale,
        "SuggestedAction": "add_to_ui_design_or_runtime_validation_notes",
    }


def priority_key(candidate: dict[str, Any]) -> tuple[int, str, str]:
    bucket_order = {
        "asset_blocked": 0,
        "registry_or_acceptance_gap": 1,
        "viewable_but_stale": 2,
        "program_handoff_ready": 3,
        "mvp_baseline_review_ready": 4,
        "review_ready": 5,
        "needs_runtime_screenshot": 6,
    }
    return (
        STATUS_ORDER.get(str(candidate.get("Priority", "")), 9),
        str(bucket_order.get(str(candidate.get("IterationBucket", "")), 9)),
        str(candidate.get("ScreenID", "")),
    )


def md_cell(value: Any) -> str:
    if isinstance(value, list):
        text = ", ".join(str(item) for item in value) if value else "-"
    elif isinstance(value, bool):
        text = "yes" if value else "no"
    else:
        text = "" if value is None else str(value)
    return text.replace("|", "\\|").replace("\n", " ")


def make_markdown(payload: dict[str, Any]) -> str:
    summary = payload["Summary"]
    lines = [
        "# UI Iteration Candidates",
        "",
        "> Generated by `tools/美术工具/Scan-UIIterationCandidates.ps1`.",
        "",
        "## Summary",
        "",
        f"* GeneratedAt: `{payload['GeneratedAt']}`",
        f"* Screen spec updated: `{summary.get('ScreenSpecUpdatedAt', '')}`",
        f"* Latest ArtAcceptance: `{summary.get('AcceptanceRunID', '')}` / `{summary.get('AcceptanceStatus', '')}` / `{summary.get('AcceptanceFinishedAt', '')}`",
        f"* Screens in active spec: `{summary.get('ScreenCount', 0)}`",
        f"* Screens with screenshots: `{summary.get('ScreensWithScreenshots', 0)}`",
        f"* Current-spec review ready: `{summary.get('CurrentSpecReviewReadyCount', 0)}`",
        f"* Viewable but stale: `{summary.get('ViewableButStaleCount', 0)}`",
        f"* Asset blocked: `{summary.get('AssetBlockedCount', 0)}`",
        "",
    ]

    if summary.get("AcceptanceIsOlderThanSpec"):
        lines.extend(
            [
                "> Warning: latest ArtAcceptance output is older than the current UI spec. Screenshots are still useful for visual review, but current-spec validation needs a rerun.",
                "",
            ]
        )

    lines.extend(
        [
            "## Candidates",
            "",
            "| Priority | Screen | Status | Screenshot | Fresh | Asset gaps | Registry gaps | Bucket | Suggested action |",
            "|---|---|---|---|---|---|---|---|---|",
        ]
    )
    for item in payload["Candidates"]:
        screenshot = f"[open]({item['ScreenshotPath']})" if item.get("ScreenshotPath") else "-"
        lines.append(
            "| "
            + " | ".join(
                [
                    md_cell(item.get("Priority")),
                    f"`{md_cell(item.get('ScreenID'))}`",
                    f"`{md_cell(item.get('LayoutStatus'))}`",
                    screenshot,
                    md_cell(not item.get("ScreenshotStaleAgainstSpec")),
                    md_cell(item.get("MissingApprovedVisualIDs")),
                    md_cell(item.get("ApprovedButNotInLatestRegistryVisualIDs")),
                    f"`{md_cell(item.get('IterationBucket'))}`",
                    f"`{md_cell(item.get('SuggestedAction'))}`",
                ]
            )
            + " |"
        )

    runtime_only = payload.get("RuntimeOnlyCaptures", [])
    lines.extend(["", "## Runtime-Only Captures", ""])
    if runtime_only:
        lines.extend(["| Screen | Screenshot | Fresh | Suggested action |", "|---|---|---|---|"])
        for item in runtime_only:
            screenshot = f"[open]({item['ScreenshotPath']})" if item.get("ScreenshotPath") else "-"
            lines.append(
                f"| `{md_cell(item.get('ScreenID'))}` | {screenshot} | "
                f"{md_cell(not item.get('ScreenshotStaleAgainstSpec'))} | "
                f"`{md_cell(item.get('SuggestedAction'))}` |"
            )
    else:
        lines.append("- None.")

    lines.extend(["", "## Suggested Reading", ""])
    lines.append("- `viewable_but_stale`: 可以先看现有截图判断大方向，但当前规格验收前需要重跑 ArtAcceptance。")
    lines.append("- `registry_or_acceptance_gap`: Approved 素材已具备，但最新运行时 registry / 截图还没反映出来。")
    lines.append("- `mvp_baseline_review_ready`: MVP 骨架可看，适合进入 Formal V1 结构审查。")
    lines.append("- `review_ready`: 当前截图可直接进入 UI 设计或视觉精修判断。")
    return "\n".join(lines) + "\n"


def build_payload(args: argparse.Namespace) -> dict[str, Any]:
    screens_path = resolve_project_path(args.screens_path)
    manifest_path = resolve_project_path(args.manifest_path)
    acceptance_root = resolve_project_path(args.acceptance_root)
    report_path = acceptance_root / "report.json"
    registry_path = acceptance_root / "registry_snapshot.json"

    screens = read_json(screens_path, {})
    manifest = read_json(manifest_path, {})
    report = read_json(report_path, {})
    registry = read_json(registry_path, {})

    spec_updated_at_text = str(screens.get("UpdatedAt", "") or "")
    spec_updated_at = parse_time(spec_updated_at_text)
    acceptance_finished_at_text = str(report.get("FinishedAt", "") or "")
    acceptance_finished_at = parse_time(acceptance_finished_at_text)

    manifest_entries = manifest_by_visual_id(manifest)
    registry_entries = registry_by_visual_id(registry)
    captures = capture_by_screen(report)

    candidates = [
        classify_screen(
            screen,
            captures.get(str(screen.get("ScreenID", ""))),
            report,
            acceptance_root,
            spec_updated_at,
            acceptance_finished_at,
            manifest_entries,
            registry_entries,
        )
        for screen in as_list(screens.get("Screens"))
        if isinstance(screen, dict)
    ]
    candidates.sort(key=priority_key)

    known_screen_ids = {item["ScreenID"] for item in candidates}
    runtime_only = [
        classify_runtime_only_capture(screen_id, capture, acceptance_root, spec_updated_at, acceptance_finished_at)
        for screen_id, capture in sorted(captures.items())
        if screen_id not in known_screen_ids
    ]

    summary = {
        "ScreenSpecUpdatedAt": spec_updated_at_text,
        "AcceptanceRunID": str(report.get("RunID", "") or ""),
        "AcceptanceStatus": str(report.get("Status", "") or ""),
        "AcceptanceFinishedAt": acceptance_finished_at_text,
        "AcceptanceIsOlderThanSpec": bool(
            spec_updated_at and acceptance_finished_at and acceptance_finished_at < spec_updated_at
        ),
        "ScreenCount": len(candidates),
        "ScreensWithScreenshots": sum(1 for item in candidates if item["HasScreenshot"]),
        "CurrentSpecReviewReadyCount": sum(1 for item in candidates if item["CanReviewCurrentSpecNow"]),
        "ViewableButStaleCount": sum(1 for item in candidates if item["IterationBucket"] == "viewable_but_stale"),
        "AssetBlockedCount": sum(1 for item in candidates if item["IterationBucket"] == "asset_blocked"),
        "RegistryOrAcceptanceGapCount": sum(
            1 for item in candidates if item["IterationBucket"] == "registry_or_acceptance_gap"
        ),
        "RuntimeOnlyCaptureCount": len(runtime_only),
    }

    return {
        "GeneratedAt": timestamp_text(),
        "Inputs": {
            "ScreensPath": repo_path(screens_path),
            "ManifestPath": repo_path(manifest_path),
            "AcceptanceRoot": repo_path(acceptance_root),
            "ReportPath": repo_path(report_path),
            "RegistryPath": repo_path(registry_path),
        },
        "Summary": summary,
        "Candidates": candidates,
        "RuntimeOnlyCaptures": runtime_only,
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Scan UI iteration candidates from art specs and runtime captures.")
    parser.add_argument("--screens-path", default=DEFAULT_SCREENS)
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--acceptance-root", default=DEFAULT_ACCEPTANCE_ROOT)
    parser.add_argument("--output-json", default=DEFAULT_OUTPUT_JSON)
    parser.add_argument("--output-markdown", default=DEFAULT_OUTPUT_MARKDOWN)
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    payload = build_payload(args)
    output_json = resolve_project_path(args.output_json)
    output_markdown = resolve_project_path(args.output_markdown)
    output_json.parent.mkdir(parents=True, exist_ok=True)
    output_json.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    output_markdown.write_text(make_markdown(payload), encoding="utf-8")

    print(f"[OK] UI iteration candidates: {repo_path(output_markdown)}")
    print(
        "[OK] "
        f"screens={payload['Summary']['ScreenCount']}, "
        f"screenshots={payload['Summary']['ScreensWithScreenshots']}, "
        f"current_ready={payload['Summary']['CurrentSpecReviewReadyCount']}, "
        f"stale={payload['Summary']['ViewableButStaleCount']}, "
        f"asset_blocked={payload['Summary']['AssetBlockedCount']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
