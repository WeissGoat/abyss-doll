# -*- coding: utf-8 -*-
"""Generate FormalV2 runtime art acceptance gate status."""

from __future__ import annotations

import argparse
import json
import re
import sys
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]

BASELINE_RUN_ID = "20260606_230523"
DEFAULT_REPORT = "UnityClient/Logs/ArtAcceptance/latest/report.json"
DEFAULT_REGISTRY_GAP = "美术文档/_generated/VisualAssetRegistry登记缺口清单.json"
DEFAULT_HANDOFF = "美术文档/_generated/程序接入交接清单.json"
DEFAULT_OUTPUT_JSON = "美术文档/_generated/FormalV2运行时验收状态.json"
DEFAULT_OUTPUT_MARKDOWN = "美术文档/_generated/FormalV2运行时验收状态.md"
DEFAULT_SNAPSHOT_DIR = "美术文档/_generated/formal_v2_runtime_acceptance_snapshots"


def resolve_project_path(value: str) -> Path:
    path = Path(value)
    return path if path.is_absolute() else PROJECT_ROOT / path


def repo_path(path: Path | None) -> str:
    if path is None:
        return ""
    try:
        return path.resolve().relative_to(PROJECT_ROOT.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def read_json(path: Path, default: Any) -> Any:
    if not path.exists():
        return default
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def timestamp_text() -> str:
    return datetime.now(timezone.utc).astimezone().isoformat(timespec="seconds")


def timestamp_filename() -> str:
    return datetime.now(timezone.utc).astimezone().strftime("%Y%m%d_%H%M%S")


def safe_snapshot_tag(value: str) -> str:
    text = re.sub(r"[^A-Za-z0-9_.-]+", "_", value.strip())
    text = re.sub(r"_+", "_", text).strip("._-")
    return text[:80]


def as_list(value: Any) -> list[Any]:
    return value if isinstance(value, list) else []


def count_existing_screenshots(screen_ids: list[str]) -> int:
    screenshot_dir = PROJECT_ROOT / "UnityClient/Logs/ArtAcceptance/latest/screenshots"
    return sum(1 for screen_id in screen_ids if (screenshot_dir / f"{screen_id}.png").exists())


def is_newer_run_id(run_id: str, baseline: str) -> bool:
    return bool(run_id) and run_id > baseline


def determine_gate(report: dict[str, Any], registry_gap: dict[str, Any], handoff: dict[str, Any]) -> tuple[str, list[str]]:
    reasons: list[str] = []
    gap_summary = registry_gap.get("Summary", {}) if isinstance(registry_gap, dict) else {}
    handoff_summary = handoff.get("Summary", {}) if isinstance(handoff, dict) else {}
    report_registry = report.get("Registry", {}) if isinstance(report, dict) else {}
    run_id = str(report.get("RunID", "") or report.get("RunId", "") or "") if isinstance(report, dict) else ""
    report_status = str(report.get("Status", "") or "") if isinstance(report, dict) else ""
    missing_registry = int(gap_summary.get("MissingRegistryCount", 0) or 0)
    program_integrate = int(handoff_summary.get("ProgramIntegrateVisualCount", 0) or gap_summary.get("ProgramIntegrateVisualCount", 0) or 0)
    missing_required = len(as_list(report_registry.get("MissingRequiredVisualIDs")))

    if program_integrate <= 0:
        return "no_program_integrate", ["No current FormalV2 program_integrate queue remains."]
    if missing_registry > 0:
        reasons.append(f"VisualAssetRegistry still misses {missing_registry} handed-off VisualIDs.")
        return "waiting_registry", reasons
    if not is_newer_run_id(run_id, BASELINE_RUN_ID):
        reasons.append(f"ArtAcceptance RunID is `{run_id or 'missing'}`, not newer than `{BASELINE_RUN_ID}`.")
        return "waiting_art_acceptance_rerun", reasons
    if report_status != "PASSED" or missing_required > 0:
        reasons.append(f"ArtAcceptance status is `{report_status}` with MissingRequiredVisualIDs={missing_required}.")
        return "runtime_failed_needs_fix", reasons
    return "ready_for_art_review", ["New ArtAcceptance evidence exists and registry gate is clear."]


def build_payload(report_path: Path, registry_gap_path: Path, handoff_path: Path) -> dict[str, Any]:
    report = read_json(report_path, {})
    registry_gap = read_json(registry_gap_path, {})
    handoff = read_json(handoff_path, {})
    gate, reasons = determine_gate(report, registry_gap, handoff)
    report_registry = report.get("Registry", {}) if isinstance(report, dict) else {}
    handoff_summary = handoff.get("Summary", {}) if isinstance(handoff, dict) else {}
    gap_summary = registry_gap.get("Summary", {}) if isinstance(registry_gap, dict) else {}
    rerun_screens = as_list(handoff.get("ProgramActions", {}).get("RerunArtAcceptanceScreens")) if isinstance(handoff, dict) else []

    return {
        "GeneratedAt": timestamp_text(),
        "Gate": gate,
        "GateReasons": reasons,
        "Inputs": {
            "ReportPath": repo_path(report_path),
            "RegistryGapPath": repo_path(registry_gap_path),
            "HandoffPath": repo_path(handoff_path),
            "BaselineRunID": BASELINE_RUN_ID,
        },
        "Summary": {
            "ProgramIntegrateVisualCount": int(handoff_summary.get("ProgramIntegrateVisualCount", 0) or gap_summary.get("ProgramIntegrateVisualCount", 0) or 0),
            "MissingRegistryCount": int(gap_summary.get("MissingRegistryCount", 0) or 0),
            "MissingApprovedFileCount": int(gap_summary.get("MissingApprovedFileCount", 0) or 0),
            "MissingMetaFileCount": int(gap_summary.get("MissingMetaFileCount", 0) or 0),
            "RerunAcceptanceScreenCount": int(handoff_summary.get("ProgramRerunAcceptanceScreenCount", 0) or 0),
            "ExistingRerunScreenshotCount": count_existing_screenshots([str(item) for item in rerun_screens]),
            "ReportRunID": str(report.get("RunID", "") or report.get("RunId", "") or "") if isinstance(report, dict) else "",
            "ReportStatus": str(report.get("Status", "") or "") if isinstance(report, dict) else "",
            "ReportEntryCount": int(report_registry.get("EntryCount", 0) or 0),
            "ReportMissingRequiredVisualIDCount": len(as_list(report_registry.get("MissingRequiredVisualIDs"))),
        },
        "RequiredNextAction": next_action_for_gate(gate),
        "RerunAcceptanceScreens": [str(item) for item in rerun_screens],
    }


def next_action_for_gate(gate: str) -> str:
    actions = {
        "waiting_registry": "Program: run Tools/P3 Art/Rebuild Approved Sprite Registry in Unity, save VisualAssetRegistry.asset, then rerun ArtAcceptance.",
        "waiting_art_acceptance_rerun": "Program: rerun ArtAcceptance / VisualAsset validation and provide a new latest RunID.",
        "runtime_failed_needs_fix": "Program/art: inspect latest ArtAcceptance errors and screenshots, then fix missing registry or runtime presentation issues.",
        "ready_for_art_review": "Art: review latest screenshots/contact sheet and update runtime art acceptance record.",
        "no_program_integrate": "Art/program: no pending program_integrate queue; inspect acceptance_needed or runtime screenshots if visual validation is still required.",
    }
    return actions.get(gate, "Inspect generated reports.")


def make_markdown(payload: dict[str, Any]) -> str:
    summary = payload["Summary"]
    lines = [
        "# FormalV2 运行时验收状态",
        "",
        "> 美术侧自动汇总门禁。它不替代人工看图，只判断当前是否具备进入 FormalV2 运行时美术验收的证据。",
        "",
        f"- GeneratedAt: `{payload['GeneratedAt']}`",
        f"- Gate: `{payload['Gate']}`",
        f"- RequiredNextAction: {payload['RequiredNextAction']}",
        "",
        "## Summary",
        "",
    ]
    for key, value in summary.items():
        lines.append(f"- {key}: `{value}`")
    lines.extend(["", "## Gate Reasons", ""])
    for reason in payload["GateReasons"]:
        lines.append(f"- {reason}")
    lines.extend(["", "## Rerun Acceptance Screens", ""])
    for screen in payload["RerunAcceptanceScreens"]:
        lines.append(f"- `{screen}`")
    return "\n".join(lines) + "\n"


def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--report-path", default=DEFAULT_REPORT)
    parser.add_argument("--registry-gap-path", default=DEFAULT_REGISTRY_GAP)
    parser.add_argument("--handoff-path", default=DEFAULT_HANDOFF)
    parser.add_argument("--output-json", default=DEFAULT_OUTPUT_JSON)
    parser.add_argument("--output-markdown", default=DEFAULT_OUTPUT_MARKDOWN)
    parser.add_argument("--snapshot-dir", default=DEFAULT_SNAPSHOT_DIR)
    parser.add_argument("--snapshot-tag", default="")
    parser.add_argument("--snapshot", action="store_true")
    args = parser.parse_args()

    report_path = resolve_project_path(args.report_path)
    registry_gap_path = resolve_project_path(args.registry_gap_path)
    handoff_path = resolve_project_path(args.handoff_path)
    output_json = resolve_project_path(args.output_json)
    output_markdown = resolve_project_path(args.output_markdown)

    payload = build_payload(report_path, registry_gap_path, handoff_path)
    markdown = make_markdown(payload)
    write_json(output_json, payload)
    output_markdown.parent.mkdir(parents=True, exist_ok=True)
    output_markdown.write_text(markdown, encoding="utf-8")

    if args.snapshot:
        snapshot_dir = resolve_project_path(args.snapshot_dir)
        tag = safe_snapshot_tag(args.snapshot_tag) or "formalv2_runtime_acceptance"
        stamp = timestamp_filename()
        write_json(snapshot_dir / f"{stamp}_{tag}.json", payload)
        (snapshot_dir / f"{stamp}_{tag}.md").write_text(markdown, encoding="utf-8")
        print(f"[OK] FormalV2 runtime acceptance snapshot: {repo_path(snapshot_dir / f'{stamp}_{tag}.md')}")

    summary = payload["Summary"]
    print(f"[OK] FormalV2 runtime acceptance status: {repo_path(output_markdown)}")
    print(
        "[OK] "
        f"gate={payload['Gate']}, "
        f"program_integrate={summary['ProgramIntegrateVisualCount']}, "
        f"missing_registry={summary['MissingRegistryCount']}, "
        f"run_id={summary['ReportRunID']}, "
        f"status={summary['ReportStatus']}"
    )
    return 0


if __name__ == "__main__":
    sys.exit(main())
