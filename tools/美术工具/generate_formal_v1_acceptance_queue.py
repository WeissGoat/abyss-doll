# -*- coding: utf-8 -*-
"""Generate the Formal V1 runtime art acceptance queue from active UI specs."""

from __future__ import annotations

import argparse
import json
import re
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]

DEFAULT_SCREENS = "美术文档/ui_design/screen_layouts.json"
DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_ACCEPTANCE_ROOT = "UnityClient/Logs/ArtAcceptance/latest"
DEFAULT_OUTPUT_JSON = "美术文档/ui_design/_generated/FormalV1验收队列.json"
DEFAULT_OUTPUT_MARKDOWN = "美术文档/ui_design/_generated/FormalV1验收队列.md"
DEFAULT_SNAPSHOT_DIR = "美术文档/ui_design/_generated/formal_v1_acceptance_snapshots"

APPROVED_STATUSES = {"approved", "registered", "validated"}
PRIORITY_ORDER = {"P0": 0, "P1": 1, "P2": 2, "P3": 3, "P4": 4}
QUEUE_ORDER = {
    "asset_blocked": 0,
    "program_register_visuals": 1,
    "capture_coverage_needed": 2,
    "rerun_acceptance_needed": 3,
    "review_previous_screenshot": 4,
    "art_review_ready": 5,
}


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


def as_list(value: Any) -> list[Any]:
    return value if isinstance(value, list) else []


def unique_strings(values: list[Any]) -> list[str]:
    seen: set[str] = set()
    result: list[str] = []
    for value in values:
        text = str(value or "").strip()
        if text and text not in seen:
            seen.add(text)
            result.append(text)
    return result


def screen_visual_ids(screen: dict[str, Any]) -> list[str]:
    visual_ids = unique_strings(as_list(screen.get("RequiredVisualIDs")))
    background = str(screen.get("BackgroundVisualID", "") or "").strip()
    if background and background not in visual_ids:
        visual_ids.append(background)
    return visual_ids


def manifest_by_visual_id(manifest: dict[str, Any]) -> dict[str, list[dict[str, Any]]]:
    result: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for entry in as_list(manifest.get("Entries")):
        if isinstance(entry, dict) and entry.get("VisualID"):
            result[str(entry["VisualID"])].append(entry)
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


def manifest_statuses(entries: list[dict[str, Any]]) -> list[str]:
    return sorted({str(entry.get("Status", "") or "") for entry in entries if str(entry.get("Status", "") or "")})


def manifest_quality_tiers(entries: list[dict[str, Any]]) -> list[str]:
    return sorted(
        {str(entry.get("QualityTier", "") or "") for entry in entries if str(entry.get("QualityTier", "") or "")}
    )


def inspect_visual(
    visual_id: str,
    manifest_entries: dict[str, list[dict[str, Any]]],
    registry_entries: dict[str, dict[str, Any]],
) -> dict[str, Any]:
    entries = manifest_entries.get(visual_id, [])
    approved_path = None
    approved_exists = False
    meta_exists = False
    for entry in entries:
        path = approved_path_for(entry)
        if path and path.exists():
            approved_path = path
            approved_exists = True
            meta_exists = Path(str(path) + ".meta").exists()
            break
        if path and approved_path is None:
            approved_path = path

    statuses = manifest_statuses(entries)
    is_approved_status = any(status in APPROVED_STATUSES for status in statuses)
    registry_entry = registry_entries.get(visual_id)
    registry_has_sprite = bool(registry_entry and registry_entry.get("HasSprite"))

    return {
        "VisualID": visual_id,
        "InManifest": bool(entries),
        "ManifestEntryCount": len(entries),
        "ManifestStatuses": statuses,
        "QualityTiers": manifest_quality_tiers(entries),
        "ApprovedPath": repo_path(approved_path),
        "ApprovedFileExists": approved_exists,
        "MetaFileExists": meta_exists,
        "IsApproved": is_approved_status and approved_exists,
        "RegistryHasSprite": registry_has_sprite,
    }


def screenshot_path_for(capture: dict[str, Any] | None, acceptance_root: Path, screen_id: str) -> Path:
    if capture and capture.get("File"):
        return acceptance_root / str(capture["File"])
    return acceptance_root / "screenshots" / f"{screen_id}.png"


def standard_review_checks(screen: dict[str, Any]) -> list[str]:
    checks = [
        "截图分辨率为 1920x1080，CanvasScaler 使用 1920x1080 参考分辨率。",
        "RequiredVisualIDs 在运行时不出现 missing sprite 或空 sprite。",
        "背景、面板、装饰和非交互图标不阻挡按钮、背包格或拖拽射线。",
        "所有数值、物品名、按钮文案、日期和说明由 Unity Text/TMP 渲染，不烘焙进图片。",
        "主操作、次级操作、警告和禁用态可读，长文本不溢出容器。",
    ]
    if screen.get("InventoryLayerPolicy"):
        checks.append("背包格保持 100x100，GridContainer 与 InventoryItemLayer 同锚点、同缩放。")
    if screen.get("BackgroundVisualID"):
        checks.append("背景使用 cover 适配，不因透明 alpha 或比例裁切产生黑块。")
    return checks


def required_actions(
    missing_approved: list[str],
    missing_meta: list[str],
    registry_gaps: list[str],
    has_capture: bool,
    capture_configured: bool,
    screenshot_stale: bool,
    capture_errors: list[Any],
) -> list[str]:
    actions: list[str] = []
    if missing_approved or missing_meta:
        actions.append("art_generate_or_sync_assets")
    if registry_gaps:
        actions.append("program_register_visuals")
    if not capture_configured:
        actions.append("program_add_art_acceptance_capture")
    elif not has_capture or screenshot_stale or registry_gaps:
        actions.append("program_rerun_art_acceptance")
    if capture_errors:
        actions.append("program_fix_capture_errors")
    if has_capture:
        actions.append("art_review_screenshot")
    return actions


def queue_bucket(actions: list[str], has_capture: bool, screenshot_stale: bool) -> str:
    if "art_generate_or_sync_assets" in actions:
        return "asset_blocked"
    if "program_register_visuals" in actions:
        return "program_register_visuals"
    if "program_add_art_acceptance_capture" in actions:
        return "capture_coverage_needed"
    if "program_rerun_art_acceptance" in actions:
        return "review_previous_screenshot" if has_capture and screenshot_stale else "rerun_acceptance_needed"
    return "art_review_ready"


def classify_screen(
    screen: dict[str, Any],
    capture: dict[str, Any] | None,
    acceptance_root: Path,
    spec_updated_at: datetime | None,
    acceptance_finished_at: datetime | None,
    manifest_entries: dict[str, list[dict[str, Any]]],
    registry_entries: dict[str, dict[str, Any]],
) -> dict[str, Any]:
    screen_id = str(screen.get("ScreenID", "") or "")
    visual_ids = screen_visual_ids(screen)
    visual_checks = [inspect_visual(visual_id, manifest_entries, registry_entries) for visual_id in visual_ids]
    missing_manifest = [item["VisualID"] for item in visual_checks if not item["InManifest"]]
    missing_approved = [item["VisualID"] for item in visual_checks if not item["IsApproved"]]
    missing_meta = [item["VisualID"] for item in visual_checks if item["ApprovedFileExists"] and not item["MetaFileExists"]]
    registry_gaps = [item["VisualID"] for item in visual_checks if item["IsApproved"] and not item["RegistryHasSprite"]]

    screenshot_path = screenshot_path_for(capture, acceptance_root, screen_id)
    has_capture_file = screenshot_path.exists()
    capture_configured = capture is not None
    capture_status = str(capture.get("Status", "") if capture else "")
    capture_warnings = as_list(capture.get("Warnings")) if capture else []
    capture_errors = as_list(capture.get("Errors")) if capture else []
    screenshot_stale = bool(
        has_capture_file
        and spec_updated_at
        and acceptance_finished_at
        and acceptance_finished_at < spec_updated_at
    )
    actions = required_actions(
        missing_approved,
        missing_meta,
        registry_gaps,
        has_capture_file,
        capture_configured,
        screenshot_stale,
        capture_errors,
    )
    bucket = queue_bucket(actions, has_capture_file, screenshot_stale)

    acceptance_criteria = [str(item) for item in as_list(screen.get("AcceptanceCriteria"))]
    return {
        "ScreenID": screen_id,
        "DisplayName": str(screen.get("DisplayName", "") or ""),
        "Priority": str(screen.get("Priority", "") or ""),
        "LayoutStatus": str(screen.get("LayoutStatus", "") or ""),
        "StructureVersion": str(screen.get("StructureVersion", "") or ""),
        "BackgroundVisualID": str(screen.get("BackgroundVisualID", "") or ""),
        "RequiredVisualCount": len(visual_ids),
        "RequiredComponentCount": len(as_list(screen.get("RequiredComponents"))),
        "ZoneCount": len(as_list(screen.get("Zones"))),
        "AcceptanceCriteriaCount": len(acceptance_criteria),
        "MissingManifestVisualIDs": missing_manifest,
        "MissingApprovedVisualIDs": missing_approved,
        "ApprovedButMissingMetaVisualIDs": missing_meta,
        "ApprovedButNotInLatestRegistryVisualIDs": registry_gaps,
        "LocalV0VisualIDs": [
            item["VisualID"] for item in visual_checks if "local_v0" in item.get("QualityTiers", [])
        ],
        "CaptureConfiguredInLatestTool": capture_configured,
        "HasScreenshot": has_capture_file,
        "ScreenshotPath": repo_path(screenshot_path) if has_capture_file else "",
        "CaptureStatus": capture_status,
        "CaptureWarnings": capture_warnings,
        "CaptureErrors": capture_errors,
        "ScreenshotStaleAgainstSpec": screenshot_stale,
        "QueueBucket": bucket,
        "RequiredActions": actions,
        "ReviewChecklist": standard_review_checks(screen) + acceptance_criteria,
    }


def priority_key(item: dict[str, Any]) -> tuple[int, int, str]:
    return (
        PRIORITY_ORDER.get(str(item.get("Priority", "")), 99),
        QUEUE_ORDER.get(str(item.get("QueueBucket", "")), 99),
        str(item.get("ScreenID", "")),
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
        "# Formal V1 运行时美术验收队列",
        "",
        "> Generated by `tools/美术工具/Generate-FormalV1AcceptanceQueue.ps1`.",
        "",
        "## Summary",
        "",
        f"* GeneratedAt: `{payload['GeneratedAt']}`",
        f"* Active Formal V1 screens: `{summary['FormalV1ScreenCount']}`",
        f"* Latest ArtAcceptance: `{summary['AcceptanceRunID']}` / `{summary['AcceptanceStatus']}` / `{summary['AcceptanceFinishedAt']}`",
        f"* Captured in latest: `{summary['CapturedScreenCount']}`",
        f"* Capture coverage needed: `{summary['BucketCounts'].get('capture_coverage_needed', 0)}`",
        f"* Program register visuals: `{summary['BucketCounts'].get('program_register_visuals', 0)}`",
        f"* Program add capture action: `{summary['ActionCounts'].get('program_add_art_acceptance_capture', 0)}`",
        f"* Program rerun action: `{summary['ActionCounts'].get('program_rerun_art_acceptance', 0)}`",
        f"* Art review action: `{summary['ActionCounts'].get('art_review_screenshot', 0)}`",
        f"* Rerun needed: `{summary['BucketCounts'].get('rerun_acceptance_needed', 0)}`",
        f"* Review previous screenshot: `{summary['BucketCounts'].get('review_previous_screenshot', 0)}`",
        f"* Art review ready: `{summary['BucketCounts'].get('art_review_ready', 0)}`",
        "",
    ]
    if summary.get("AcceptanceIsOlderThanSpec"):
        lines.extend(
            [
                "> Latest ArtAcceptance is older than active UI spec. Existing screenshots can be used for rough review, but current Formal V1 validation requires a rerun.",
                "",
            ]
        )

    lines.extend(
        [
            "## Queue",
            "",
            "| Priority | Screen | Bucket | Screenshot | Required actions | Registry gaps | Local V0 | Checklist items |",
            "|---|---|---|---|---|---|---|---:|",
        ]
    )
    for item in payload["Queue"]:
        screenshot = f"[open]({item['ScreenshotPath']})" if item.get("ScreenshotPath") else "-"
        lines.append(
            "| "
            + " | ".join(
                [
                    md_cell(item["Priority"]),
                    f"`{md_cell(item['ScreenID'])}`",
                    f"`{md_cell(item['QueueBucket'])}`",
                    screenshot,
                    md_cell(item["RequiredActions"]),
                    md_cell(item["ApprovedButNotInLatestRegistryVisualIDs"]),
                    str(len(item["LocalV0VisualIDs"])),
                    str(len(item["ReviewChecklist"])),
                ]
            )
            + " |"
        )

    lines.extend(["", "## Per-Screen Checklist", ""])
    for item in payload["Queue"]:
        lines.append(f"### `{item['ScreenID']}`")
        lines.append("")
        lines.append(f"* Bucket: `{item['QueueBucket']}`")
        lines.append(f"* Required actions: `{', '.join(item['RequiredActions']) if item['RequiredActions'] else 'none'}`")
        if item.get("ScreenshotPath"):
            lines.append(f"* Screenshot: `{item['ScreenshotPath']}`")
        if item["ApprovedButNotInLatestRegistryVisualIDs"]:
            lines.append(f"* Registry gaps: `{', '.join(item['ApprovedButNotInLatestRegistryVisualIDs'])}`")
        if item["MissingApprovedVisualIDs"]:
            lines.append(f"* Missing approved assets: `{', '.join(item['MissingApprovedVisualIDs'])}`")
        lines.append("")
        for check in item["ReviewChecklist"]:
            lines.append(f"- [ ] {check}")
        lines.append("")

    lines.extend(
        [
            "## Bucket Meanings",
            "",
            "- `asset_blocked`: 有 VisualID 尚未形成 Approved PNG 或 .meta，美术先补素材。",
            "- `program_register_visuals`: Approved 素材已具备，但 latest Registry / 截图还没反映，程序先登记并重跑验收。",
            "- `capture_coverage_needed`: active Formal V1 界面尚未被 ArtAcceptance latest 覆盖，需要程序补截图点或验收入口。",
            "- `rerun_acceptance_needed`: 截图缺失、过期或登记后需要重跑 ArtAcceptance。",
            "- `review_previous_screenshot`: 有旧截图可粗看，但必须重跑后才能判定当前 active 规格。",
            "- `art_review_ready`: 当前截图和数据足够进入正式美术验收。",
        ]
    )
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

    spec_updated_text = str(screens.get("UpdatedAt", "") or "")
    spec_updated_at = parse_time(spec_updated_text)
    acceptance_finished_text = str(report.get("FinishedAt", "") or "")
    acceptance_finished_at = parse_time(acceptance_finished_text)

    manifest_entries = manifest_by_visual_id(manifest)
    registry_entries = registry_by_visual_id(registry)
    captures = capture_by_screen(report)

    queue = [
        classify_screen(
            screen,
            captures.get(str(screen.get("ScreenID", "") or "")),
            acceptance_root,
            spec_updated_at,
            acceptance_finished_at,
            manifest_entries,
            registry_entries,
        )
        for screen in as_list(screens.get("Screens"))
        if isinstance(screen, dict)
        and str(screen.get("StructureVersion", "") or "") == "FormalV1"
        and str(screen.get("LayoutStatus", "") or "") in {"active_spec", "handoff", "integrated", "validated"}
    ]
    queue.sort(key=priority_key)

    bucket_counts = Counter(item["QueueBucket"] for item in queue)
    action_counts = Counter(action for item in queue for action in item["RequiredActions"])
    return {
        "GeneratedAt": timestamp_text(),
        "SnapshotTag": str(args.snapshot_tag or ""),
        "Inputs": {
            "ScreensPath": repo_path(screens_path),
            "ManifestPath": repo_path(manifest_path),
            "AcceptanceRoot": repo_path(acceptance_root),
            "ReportPath": repo_path(report_path),
            "RegistryPath": repo_path(registry_path),
        },
        "Summary": {
            "ScreenSpecUpdatedAt": spec_updated_text,
            "AcceptanceRunID": str(report.get("RunID", "") or ""),
            "AcceptanceStatus": str(report.get("Status", "") or ""),
            "AcceptanceFinishedAt": acceptance_finished_text,
            "AcceptanceIsOlderThanSpec": bool(
                spec_updated_at and acceptance_finished_at and acceptance_finished_at < spec_updated_at
            ),
            "FormalV1ScreenCount": len(queue),
            "CapturedScreenCount": sum(1 for item in queue if item["HasScreenshot"]),
            "BucketCounts": dict(sorted(bucket_counts.items())),
            "ActionCounts": dict(sorted(action_counts.items())),
        },
        "Queue": queue,
    }


def write_snapshot(args: argparse.Namespace, payload: dict[str, Any], markdown: str) -> tuple[Path, Path]:
    snapshot_dir = resolve_project_path(args.snapshot_dir)
    tag = safe_snapshot_tag(args.snapshot_tag)
    stem = timestamp_filename()
    if tag:
        stem = f"{stem}_{tag}"
    snapshot_json = snapshot_dir / f"{stem}.json"
    snapshot_markdown = snapshot_dir / f"{stem}.md"
    write_json(snapshot_json, payload)
    snapshot_markdown.parent.mkdir(parents=True, exist_ok=True)
    snapshot_markdown.write_text(markdown, encoding="utf-8")
    return snapshot_json, snapshot_markdown


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generate Formal V1 runtime art acceptance queue.")
    parser.add_argument("--screens-path", default=DEFAULT_SCREENS)
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--acceptance-root", default=DEFAULT_ACCEPTANCE_ROOT)
    parser.add_argument("--output-json", default=DEFAULT_OUTPUT_JSON)
    parser.add_argument("--output-markdown", default=DEFAULT_OUTPUT_MARKDOWN)
    parser.add_argument("--snapshot-dir", default=DEFAULT_SNAPSHOT_DIR)
    parser.add_argument("--snapshot-tag", default="")
    parser.add_argument("--snapshot", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    payload = build_payload(args)
    output_json = resolve_project_path(args.output_json)
    output_markdown = resolve_project_path(args.output_markdown)
    markdown = make_markdown(payload)
    write_json(output_json, payload)
    output_markdown.parent.mkdir(parents=True, exist_ok=True)
    output_markdown.write_text(markdown, encoding="utf-8")

    snapshot_paths: tuple[Path, Path] | None = None
    if args.snapshot:
        snapshot_paths = write_snapshot(args, payload, markdown)

    bucket_counts = payload["Summary"]["BucketCounts"]
    print(f"[OK] Formal V1 acceptance queue: {repo_path(output_markdown)}")
    if snapshot_paths is not None:
        _, snapshot_markdown = snapshot_paths
        print(f"[OK] Formal V1 acceptance snapshot: {repo_path(snapshot_markdown)}")
    print(
        "[OK] "
        f"screens={payload['Summary']['FormalV1ScreenCount']}, "
        f"captured={payload['Summary']['CapturedScreenCount']}, "
        f"register={bucket_counts.get('program_register_visuals', 0)}, "
        f"capture_needed={bucket_counts.get('capture_coverage_needed', 0)}, "
        f"review_ready={bucket_counts.get('art_review_ready', 0)}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
