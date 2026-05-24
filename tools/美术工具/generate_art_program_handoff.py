# -*- coding: utf-8 -*-
"""Generate a compact program-facing art handoff from current art queues."""

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

DEFAULT_INTEGRATION_CANDIDATES = "美术文档/_generated/可接入素材清单.json"
DEFAULT_ACCEPTANCE_QUEUE = "美术文档/ui_design/_generated/FormalV1验收队列.json"
DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_OUTPUT_JSON = "美术文档/_generated/程序接入交接清单.json"
DEFAULT_OUTPUT_MARKDOWN = "美术文档/_generated/程序接入交接清单.md"
DEFAULT_SNAPSHOT_DIR = "美术文档/_generated/art_program_handoff_snapshots"

PRIORITY_ORDER = {"P0": 0, "P1": 1, "P2": 2, "P3": 3, "P4": 4}


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


def md_cell(value: Any) -> str:
    if isinstance(value, list):
        text = ", ".join(str(item) for item in value) if value else "-"
    elif isinstance(value, bool):
        text = "yes" if value else "no"
    else:
        text = "" if value is None else str(value)
    return text.replace("|", "\\|").replace("\n", " ")


def unique_strings(values: list[Any]) -> list[str]:
    seen: set[str] = set()
    result: list[str] = []
    for value in values:
        text = str(value or "").strip()
        if text and text not in seen:
            seen.add(text)
            result.append(text)
    return result


def manifest_by_visual_id(manifest: dict[str, Any]) -> dict[str, list[dict[str, Any]]]:
    grouped: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for entry in as_list(manifest.get("Entries")):
        if isinstance(entry, dict) and entry.get("VisualID"):
            grouped[str(entry["VisualID"])].append(entry)
    return grouped


def manifest_values(entries: list[dict[str, Any]], key: str) -> list[str]:
    return sorted({str(entry.get(key, "") or "") for entry in entries if str(entry.get(key, "") or "")})


def split_refs(refs: list[Any]) -> tuple[list[str], list[str]]:
    screen_refs: list[str] = []
    other_refs: list[str] = []
    for ref in unique_strings(refs):
        if ref.startswith("screen:"):
            screen_refs.append(ref.removeprefix("screen:"))
        else:
            other_refs.append(ref)
    return screen_refs, other_refs


def normalize_program_visual(
    item: dict[str, Any],
    manifest_entries: dict[str, list[dict[str, Any]]],
) -> dict[str, Any]:
    visual_id = str(item.get("VisualID", "") or "").strip()
    screen_refs, other_refs = split_refs(as_list(item.get("ReferencedBy")))
    entries = manifest_entries.get(visual_id, [])
    quality_tiers = manifest_values(entries, "QualityTier")
    statuses = manifest_values(entries, "Status")
    return {
        "VisualID": visual_id,
        "Priority": str(item.get("Priority", "") or ""),
        "Domain": str(item.get("Domain", "") or ""),
        "AssetType": str(item.get("AssetType", "") or ""),
        "DisplayName": str(item.get("DisplayName", "") or ""),
        "QualityTiers": quality_tiers,
        "ManifestStatuses": statuses,
        "IsLocalV0": "local_v0" in quality_tiers,
        "ApprovedPath": str(item.get("ApprovedPath", "") or ""),
        "MetaFileExists": bool(item.get("MetaFileExists")),
        "RegistryHasSprite": bool(item.get("RegistryHasSprite")),
        "ReferencedScreens": screen_refs,
        "OtherReferences": other_refs,
        "Reason": str(item.get("Reason", "") or ""),
    }


def visual_sort_key(item: dict[str, Any]) -> tuple[int, str, str, str]:
    refs = as_list(item.get("ReferencedScreens"))
    return (
        PRIORITY_ORDER.get(str(item.get("Priority", "")), 99),
        str(refs[0] if refs else "~"),
        str(item.get("Domain", "")),
        str(item.get("VisualID", "")),
    )


def screen_sort_key(item: dict[str, Any]) -> tuple[int, str]:
    return (
        PRIORITY_ORDER.get(str(item.get("Priority", "")), 99),
        str(item.get("ScreenID", "")),
    )


def normalize_acceptance_screen(item: dict[str, Any]) -> dict[str, Any]:
    return {
        "ScreenID": str(item.get("ScreenID", "") or ""),
        "DisplayName": str(item.get("DisplayName", "") or ""),
        "Priority": str(item.get("Priority", "") or ""),
        "QueueBucket": str(item.get("QueueBucket", "") or ""),
        "RequiredActions": unique_strings(as_list(item.get("RequiredActions"))),
        "RegistryGaps": unique_strings(as_list(item.get("ApprovedButNotInLatestRegistryVisualIDs"))),
        "LocalV0VisualIDs": unique_strings(as_list(item.get("LocalV0VisualIDs"))),
        "CaptureConfiguredInLatestTool": bool(item.get("CaptureConfiguredInLatestTool")),
        "HasScreenshot": bool(item.get("HasScreenshot")),
        "ScreenshotPath": str(item.get("ScreenshotPath", "") or ""),
        "ScreenshotStaleAgainstSpec": bool(item.get("ScreenshotStaleAgainstSpec")),
    }


def build_payload(args: argparse.Namespace) -> dict[str, Any]:
    integration_path = resolve_project_path(args.integration_candidates_path)
    acceptance_path = resolve_project_path(args.acceptance_queue_path)
    manifest_path = resolve_project_path(args.manifest_path)

    integration = read_json(integration_path, {})
    acceptance = read_json(acceptance_path, {})
    manifest = read_json(manifest_path, {})
    manifest_entries = manifest_by_visual_id(manifest)

    program_visuals = [
        normalize_program_visual(item, manifest_entries)
        for item in as_list(integration.get("Candidates"))
        if isinstance(item, dict) and item.get("Action") == "program_integrate"
    ]
    program_visuals.sort(key=visual_sort_key)

    acceptance_screens = [
        normalize_acceptance_screen(item)
        for item in as_list(acceptance.get("Queue"))
        if isinstance(item, dict) and item.get("ScreenID")
    ]
    acceptance_screens.sort(key=screen_sort_key)

    register_screens = [
        item for item in acceptance_screens if "program_register_visuals" in item["RequiredActions"]
    ]
    add_capture_screens = [
        item for item in acceptance_screens if "program_add_art_acceptance_capture" in item["RequiredActions"]
    ]
    rerun_screens = [
        item for item in acceptance_screens if "program_rerun_art_acceptance" in item["RequiredActions"]
    ]
    art_review_screens = [
        item for item in acceptance_screens if "art_review_screenshot" in item["RequiredActions"]
    ]

    visual_priority_counts = Counter(item["Priority"] for item in program_visuals)
    visual_domain_counts = Counter(item["Domain"] for item in program_visuals)
    screen_bucket_counts = Counter(item["QueueBucket"] for item in acceptance_screens)
    screen_action_counts = Counter(action for item in acceptance_screens for action in item["RequiredActions"])

    return {
        "GeneratedAt": timestamp_text(),
        "SnapshotTag": str(args.snapshot_tag or ""),
        "Inputs": {
            "IntegrationCandidatesPath": repo_path(integration_path),
            "AcceptanceQueuePath": repo_path(acceptance_path),
            "ManifestPath": repo_path(manifest_path),
        },
        "SourceSummaries": {
            "IntegrationGeneratedAt": str(integration.get("GeneratedAt", "") or ""),
            "IntegrationActionCounts": integration.get("Summary", {}).get("ActionCounts", {}),
            "AcceptanceGeneratedAt": str(acceptance.get("GeneratedAt", "") or ""),
            "AcceptanceRunID": acceptance.get("Summary", {}).get("AcceptanceRunID", ""),
            "AcceptanceStatus": acceptance.get("Summary", {}).get("AcceptanceStatus", ""),
            "AcceptanceFinishedAt": acceptance.get("Summary", {}).get("AcceptanceFinishedAt", ""),
            "AcceptanceIsOlderThanSpec": bool(
                acceptance.get("Summary", {}).get("AcceptanceIsOlderThanSpec", False)
            ),
        },
        "Summary": {
            "ProgramIntegrateVisualCount": len(program_visuals),
            "ProgramIntegrateLocalV0Count": sum(1 for item in program_visuals if item["IsLocalV0"]),
            "ProgramIntegratePriorityCounts": dict(sorted(visual_priority_counts.items())),
            "ProgramIntegrateDomainCounts": dict(sorted(visual_domain_counts.items())),
            "FormalV1ScreenCount": len(acceptance_screens),
            "ProgramRegisterScreenCount": len(register_screens),
            "ProgramAddCaptureScreenCount": len(add_capture_screens),
            "ProgramRerunAcceptanceScreenCount": len(rerun_screens),
            "ArtReviewScreenshotScreenCount": len(art_review_screens),
            "AcceptanceQueueBucketCounts": dict(sorted(screen_bucket_counts.items())),
            "AcceptanceActionCounts": dict(sorted(screen_action_counts.items())),
        },
        "ProgramActions": {
            "RegisterVisualIDs": [item["VisualID"] for item in program_visuals],
            "AddArtAcceptanceCaptureScreens": [item["ScreenID"] for item in add_capture_screens],
            "RerunArtAcceptanceScreens": [item["ScreenID"] for item in rerun_screens],
            "ArtReviewAfterRerunScreens": [item["ScreenID"] for item in art_review_screens],
        },
        "ProgramIntegrateVisuals": program_visuals,
        "FormalV1AcceptanceActions": acceptance_screens,
    }


def make_markdown(payload: dict[str, Any]) -> str:
    summary = payload["Summary"]
    sources = payload["SourceSummaries"]
    actions = payload["ProgramActions"]
    lines = [
        "# 程序接入交接清单",
        "",
        "> Generated by `tools/美术工具/Generate-ArtProgramHandoff.ps1`.",
        "",
        "## Summary",
        "",
        f"* GeneratedAt: `{payload['GeneratedAt']}`",
        f"* Integration source: `{payload['Inputs']['IntegrationCandidatesPath']}` / `{sources['IntegrationGeneratedAt']}`",
        f"* Acceptance source: `{payload['Inputs']['AcceptanceQueuePath']}` / `{sources['AcceptanceGeneratedAt']}`",
        f"* Latest ArtAcceptance: `{sources['AcceptanceRunID']}` / `{sources['AcceptanceStatus']}` / `{sources['AcceptanceFinishedAt']}`",
        f"* Acceptance older than active spec: `{sources['AcceptanceIsOlderThanSpec']}`",
        f"* VisualAssetRegistry register count: `{summary['ProgramIntegrateVisualCount']}`",
        f"* Local V0 among register count: `{summary['ProgramIntegrateLocalV0Count']}`",
        f"* Formal V1 screens: `{summary['FormalV1ScreenCount']}`",
        f"* Screens needing VisualID registration: `{summary['ProgramRegisterScreenCount']}`",
        f"* Screens needing ArtAcceptance capture coverage: `{summary['ProgramAddCaptureScreenCount']}`",
        f"* Screens needing ArtAcceptance rerun: `{summary['ProgramRerunAcceptanceScreenCount']}`",
        "",
        "## Program Next Actions",
        "",
        "1. Register the `VisualAssetRegistry` entries listed in **VisualAssetRegistry Queue**.",
        "2. Add ArtAcceptance screenshot coverage for screens listed in **Capture Coverage Queue**.",
        "3. Rerun ArtAcceptance for screens listed in **Rerun Queue** after registry and capture updates.",
        "4. Return the latest screenshots and `registry_snapshot.json`; art side will review the screenshot queue after rerun.",
        "",
        "> `local_v0` assets are allowed for current program integration and layout validation. They are not final visual quality and remain in the Visual V2 replacement queue.",
        "",
        "## VisualAssetRegistry Queue",
        "",
    ]

    visuals = payload["ProgramIntegrateVisuals"]
    if visuals:
        lines.extend(
            [
                "| Priority | VisualID | Domain | Quality | Screens / refs | ApprovedPath |",
                "|---|---|---|---|---|---|",
            ]
        )
        for item in visuals:
            refs = item["ReferencedScreens"] or item["OtherReferences"]
            quality = ", ".join(item["QualityTiers"]) if item["QualityTiers"] else "-"
            lines.append(
                "| "
                + " | ".join(
                    [
                        md_cell(item["Priority"]),
                        f"`{md_cell(item['VisualID'])}`",
                        md_cell(item["Domain"]),
                        md_cell(quality),
                        md_cell(refs),
                        md_cell(item["ApprovedPath"]),
                    ]
                )
                + " |"
            )
    else:
        lines.append("- 当前没有需要程序登记的 Approved VisualID。")

    add_capture = [
        item
        for item in payload["FormalV1AcceptanceActions"]
        if "program_add_art_acceptance_capture" in item["RequiredActions"]
    ]
    lines.extend(["", "## Capture Coverage Queue", ""])
    if add_capture:
        lines.extend(
            [
                "| Priority | ScreenID | Registry gaps | Required actions |",
                "|---|---|---|---|",
            ]
        )
        for item in add_capture:
            lines.append(
                "| "
                + " | ".join(
                    [
                        md_cell(item["Priority"]),
                        f"`{md_cell(item['ScreenID'])}`",
                        md_cell(item["RegistryGaps"]),
                        md_cell(item["RequiredActions"]),
                    ]
                )
                + " |"
            )
    else:
        lines.append("- 当前没有新增截图覆盖需求。")

    rerun = [
        item
        for item in payload["FormalV1AcceptanceActions"]
        if "program_rerun_art_acceptance" in item["RequiredActions"]
    ]
    lines.extend(["", "## Rerun Queue", ""])
    if rerun:
        lines.extend(
            [
                "| Priority | ScreenID | Bucket | Screenshot | Registry gaps |",
                "|---|---|---|---|---|",
            ]
        )
        for item in rerun:
            screenshot = item["ScreenshotPath"] or "-"
            lines.append(
                "| "
                + " | ".join(
                    [
                        md_cell(item["Priority"]),
                        f"`{md_cell(item['ScreenID'])}`",
                        f"`{md_cell(item['QueueBucket'])}`",
                        md_cell(screenshot),
                        md_cell(item["RegistryGaps"]),
                    ]
                )
                + " |"
            )
    else:
        lines.append("- 当前没有需要程序重跑的 Formal V1 截图。")

    lines.extend(
        [
            "",
            "## Source Files",
            "",
            f"* Art integration candidates: `{payload['Inputs']['IntegrationCandidatesPath']}`",
            f"* Formal V1 acceptance queue: `{payload['Inputs']['AcceptanceQueuePath']}`",
            f"* Manifest: `{payload['Inputs']['ManifestPath']}`",
            "",
            "## Action Meanings",
            "",
            "- `RegisterVisualIDs`: Approved PNG 和 Unity `.meta` 已存在，程序侧登记到 `VisualAssetRegistry`。",
            "- `AddArtAcceptanceCaptureScreens`: active Formal V1 界面还没有 latest 截图点，程序侧补 ArtAcceptance 捕获。",
            "- `RerunArtAcceptanceScreens`: Registry 或 active 规格已变化，需要重跑截图验收。",
            "- `ArtReviewAfterRerunScreens`: 重跑后交给美术侧验收结构、缺图、黑块、射线遮挡和可读性。",
        ]
    )
    return "\n".join(lines) + "\n"


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
    parser = argparse.ArgumentParser(description="Generate a program-facing art handoff.")
    parser.add_argument("--integration-candidates-path", default=DEFAULT_INTEGRATION_CANDIDATES)
    parser.add_argument("--acceptance-queue-path", default=DEFAULT_ACCEPTANCE_QUEUE)
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--output-json", default=DEFAULT_OUTPUT_JSON)
    parser.add_argument("--output-markdown", default=DEFAULT_OUTPUT_MARKDOWN)
    parser.add_argument("--snapshot-dir", default=DEFAULT_SNAPSHOT_DIR)
    parser.add_argument("--snapshot-tag", default="")
    parser.add_argument("--snapshot", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    payload = build_payload(args)
    markdown = make_markdown(payload)
    output_json = resolve_project_path(args.output_json)
    output_markdown = resolve_project_path(args.output_markdown)
    write_json(output_json, payload)
    output_markdown.parent.mkdir(parents=True, exist_ok=True)
    output_markdown.write_text(markdown, encoding="utf-8")

    snapshot_paths: tuple[Path, Path] | None = None
    if args.snapshot:
        snapshot_paths = write_snapshot(args, payload, markdown)

    print(f"[OK] Art program handoff: {repo_path(output_markdown)}")
    if snapshot_paths is not None:
        _, snapshot_markdown = snapshot_paths
        print(f"[OK] Art program handoff snapshot: {repo_path(snapshot_markdown)}")
    summary = payload["Summary"]
    print(
        "[OK] "
        f"program_integrate={summary['ProgramIntegrateVisualCount']}, "
        f"add_capture={summary['ProgramAddCaptureScreenCount']}, "
        f"rerun_acceptance={summary['ProgramRerunAcceptanceScreenCount']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
