# -*- coding: utf-8 -*-
"""Generate an executable image generation plan for currently missing art assets."""

from __future__ import annotations

import argparse
import json
import re
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]

DEFAULT_INTEGRATION_CANDIDATES = "美术文档/_generated/可接入素材清单.json"
DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_OUTPUT_JSON = "美术文档/_generated/缺图生成计划.json"
DEFAULT_OUTPUT_MARKDOWN = "美术文档/_generated/缺图生成计划.md"
DEFAULT_SNAPSHOT_DIR = "美术文档/_generated/art_generation_plan_snapshots"
DEFAULT_BATCH_ID = "nai_missing_assets_20260525_01"

DOMAIN_ORDER = {
    "node": 0,
    "ui": 1,
    "item": 2,
    "prosthetic": 3,
    "monster": 4,
    "background": 5,
}
PRIORITY_ORDER = {"P0": 0, "P1": 1, "P2": 2, "P3": 3}


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


def source_spec(entry: dict[str, Any]) -> dict[str, Any]:
    spec = entry.get("Spec")
    if not isinstance(spec, dict):
        return {}
    nested = spec.get("SourceSpec")
    return nested if isinstance(nested, dict) else spec


def expected_size_from_manifest(entry: dict[str, Any]) -> str:
    src = source_spec(entry)
    width = src.get("Width", "")
    height = src.get("Height", "")
    return f"{width}x{height}" if width and height else ""


def item_sort_key(item: dict[str, Any]) -> tuple[int, int, str]:
    return (
        PRIORITY_ORDER.get(str(item.get("Priority", "")), 99),
        DOMAIN_ORDER.get(str(item.get("Domain", "")), 99),
        str(item.get("VisualID", "")),
    )


def comma_visual_ids(items: list[dict[str, Any]]) -> str:
    return ",".join(str(item["VisualID"]) for item in items)


def observed_anlas_note(size: str, provider: str) -> str:
    if provider != "novelai":
        return ""
    if size == "512x512":
        return "Observed probe on 2026-05-25: NovelAI required 5 Anlas for one 512x512 image."
    return ""


def command_prefix(args: argparse.Namespace) -> str:
    return ".\\tools\\美术工具"


def build_item(
    candidate: dict[str, Any],
    manifest_entry: dict[str, Any],
    args: argparse.Namespace,
) -> dict[str, Any]:
    visual_id = str(candidate.get("VisualID", "") or "")
    size = expected_size_from_manifest(manifest_entry)
    incoming_base = resolve_project_path(f"UnityClient/Assets/Art/_IncomingAI/{visual_id}")
    prompt_ready = bool(
        str(manifest_entry.get("PromptEN", "") or "").strip()
        and str(manifest_entry.get("NegativePromptEN", "") or "").strip()
        and isinstance(manifest_entry.get("Spec"), dict)
    )
    tools = command_prefix(args)
    run_command = (
        f"{tools}\\Run-ArtGeneration.ps1 -Config .\\tools\\美术工具\\ai_image_gateway.local.yaml "
        f"-Provider {args.provider} -Status {args.status} -VisualID {visual_id} "
        f"-Variants {args.variants} -DelaySeconds {args.delay_seconds:g} "
        f"-BatchID {args.batch_id}"
    )
    optimize_command = (
        f"{tools}\\Optimize-ArtAssets.ps1 -Status generated -VisualID {visual_id} "
        f"-BatchID {args.batch_id} -Overwrite"
    )
    sync_command = (
        f"{tools}\\Sync-ApprovedArt.ps1 -Status generated -VisualID {visual_id} "
        f"-BatchID {args.batch_id} -AllowProcessedFallback -Overwrite"
    )
    return {
        "VisualID": visual_id,
        "Order": 0,
        "Priority": str(candidate.get("Priority", "") or manifest_entry.get("Priority", "") or ""),
        "Domain": str(candidate.get("Domain", "") or manifest_entry.get("Domain", "") or ""),
        "AssetType": str(candidate.get("AssetType", "") or manifest_entry.get("AssetType", "") or ""),
        "DisplayName": str(candidate.get("DisplayName", "") or manifest_entry.get("DisplayName", "") or ""),
        "ConfigID": str(candidate.get("ConfigID", "") or manifest_entry.get("ConfigID", "") or ""),
        "Status": str(candidate.get("Status", "") or manifest_entry.get("Status", "") or ""),
        "PromptReady": prompt_ready,
        "ExpectedSize": size,
        "AnlasNote": observed_anlas_note(size, args.provider),
        "PromptCN": str(manifest_entry.get("PromptCN", "") or ""),
        "PromptEN": str(manifest_entry.get("PromptEN", "") or ""),
        "NegativePromptEN": str(manifest_entry.get("NegativePromptEN", "") or ""),
        "Spec": manifest_entry.get("Spec", {}),
        "ApprovedPath": str(candidate.get("ApprovedPath", "") or manifest_entry.get("OutputPath", "") or ""),
        "Workspace": {
            "Base": repo_path(incoming_base),
            "Raw": repo_path(incoming_base / "raw"),
            "Processed": repo_path(incoming_base / "processed"),
            "Selected": repo_path(incoming_base / "selected"),
            "ContactSheet": repo_path(incoming_base / "contact_sheet"),
        },
        "ReferencedBy": as_list(candidate.get("ReferencedBy")),
        "Commands": {
            "RunGeneration": run_command,
            "Optimize": optimize_command,
            "SyncApproved": sync_command,
        },
    }


def build_payload(args: argparse.Namespace) -> dict[str, Any]:
    candidates_path = resolve_project_path(args.integration_candidates_path)
    manifest_path = resolve_project_path(args.manifest_path)
    candidates_payload = read_json(candidates_path, {})
    manifest = read_json(manifest_path, {})
    manifest_entries = {
        str(entry.get("VisualID", "") or ""): entry
        for entry in as_list(manifest.get("Entries"))
        if isinstance(entry, dict)
    }

    raw_candidates = [
        item
        for item in as_list(candidates_payload.get("Candidates"))
        if isinstance(item, dict) and item.get("Action") == args.action
    ]
    raw_candidates.sort(key=item_sort_key)

    plan_items: list[dict[str, Any]] = []
    missing_manifest: list[str] = []
    for index, candidate in enumerate(raw_candidates, start=1):
        visual_id = str(candidate.get("VisualID", "") or "")
        manifest_entry = manifest_entries.get(visual_id)
        if not manifest_entry:
            missing_manifest.append(visual_id)
            continue
        plan_item = build_item(candidate, manifest_entry, args)
        plan_item["Order"] = index
        plan_items.append(plan_item)

    domain_counts = Counter(item["Domain"] for item in plan_items)
    priority_counts = Counter(item["Priority"] for item in plan_items)
    size_counts = Counter(item["ExpectedSize"] for item in plan_items)
    prompt_ready_count = sum(1 for item in plan_items if item["PromptReady"])
    visual_ids = comma_visual_ids(plan_items)
    tools = command_prefix(args)
    run_all = (
        f"{tools}\\Run-ArtGeneration.ps1 -Config .\\tools\\美术工具\\ai_image_gateway.local.yaml "
        f"-Provider {args.provider} -Status {args.status} -VisualID {visual_ids} "
        f"-Variants {args.variants} -DelaySeconds {args.delay_seconds:g} "
        f"-BatchID {args.batch_id}"
        if visual_ids
        else ""
    )

    return {
        "GeneratedAt": timestamp_text(),
        "SnapshotTag": str(args.snapshot_tag or ""),
        "Inputs": {
            "IntegrationCandidatesPath": repo_path(candidates_path),
            "ManifestPath": repo_path(manifest_path),
        },
        "RunConfig": {
            "Provider": args.provider,
            "BatchID": args.batch_id,
            "StatusFilter": args.status,
            "ActionFilter": args.action,
            "Variants": args.variants,
            "DelaySeconds": args.delay_seconds,
            "SerialRequired": True,
            "LastProbeBatchID": args.last_probe_batch_id,
            "LastProbeNote": args.last_probe_note,
        },
        "Summary": {
            "TotalCandidateItems": len(raw_candidates),
            "PlannedItems": len(plan_items),
            "PromptReadyItems": prompt_ready_count,
            "MissingManifestEntries": missing_manifest,
            "PriorityCounts": dict(sorted(priority_counts.items())),
            "DomainCounts": dict(sorted(domain_counts.items())),
            "SizeCounts": dict(sorted(size_counts.items())),
        },
        "BatchCommands": {
            "RunAllGeneration": run_all,
            "OptimizeAllAfterGeneration": (
                f"{tools}\\Optimize-ArtAssets.ps1 -Status generated "
                f"-BatchID {args.batch_id} -Overwrite"
            ),
            "SyncAllAfterReviewOrFallback": (
                f"{tools}\\Sync-ApprovedArt.ps1 -Status generated "
                f"-BatchID {args.batch_id} -AllowProcessedFallback -Overwrite"
            ),
            "RefreshIntegration": (
                f"{tools}\\Generate-ArtIntegrationCandidates.ps1 -Snapshot "
                f"-SnapshotTag {args.batch_id}"
            ),
            "RefreshQuality": (
                f"{tools}\\Generate-ArtQualityBacklog.ps1 -Snapshot "
                f"-SnapshotTag {args.batch_id}"
            ),
            "RefreshPlan": (
                f"{tools}\\Generate-ArtBatchPlan.ps1 -Snapshot "
                f"-SnapshotTag {args.batch_id}_after -BatchID {args.batch_id}"
            ),
        },
        "Items": plan_items,
    }


def md_cell(value: Any) -> str:
    if isinstance(value, bool):
        text = "yes" if value else "no"
    elif isinstance(value, list):
        text = ", ".join(str(item) for item in value) if value else ""
    elif value is None:
        text = ""
    else:
        text = str(value)
    return text.replace("|", "\\|").replace("\n", " ")


def make_markdown(payload: dict[str, Any]) -> str:
    summary = payload["Summary"]
    run = payload["RunConfig"]
    lines = [
        "# 缺图生成计划",
        "",
        "> Generated by `tools/美术工具/Generate-ArtBatchPlan.ps1`.",
        "",
        "## Summary",
        "",
        f"* GeneratedAt: `{payload['GeneratedAt']}`",
        f"* Provider: `{run['Provider']}`",
        f"* BatchID: `{run['BatchID']}`",
        f"* Action filter: `{run['ActionFilter']}`",
        f"* Status filter: `{run['StatusFilter']}`",
        f"* Variants per asset: `{run['Variants']}`",
        f"* Delay seconds: `{run['DelaySeconds']}`",
        f"* Serial required: `{run['SerialRequired']}`",
        f"* Planned items: `{summary['PlannedItems']}`",
        f"* Prompt ready: `{summary['PromptReadyItems']}`",
        f"* Priority counts: `{json.dumps(summary['PriorityCounts'], ensure_ascii=False)}`",
        f"* Domain counts: `{json.dumps(summary['DomainCounts'], ensure_ascii=False)}`",
        f"* Size counts: `{json.dumps(summary['SizeCounts'], ensure_ascii=False)}`",
        "",
    ]
    if run.get("LastProbeNote"):
        lines.extend(
            [
                "## Last Probe",
                "",
                f"* Batch: `{run.get('LastProbeBatchID', '')}`",
                f"* Result: {run['LastProbeNote']}",
                "",
            ]
        )

    lines.extend(
        [
            "## Preconditions",
            "",
            "- `NAI_ACCESS_TOKEN` must be set locally.",
            "- NovelAI Anlas must be sufficient. The latest probe showed that one 512x512 image required 5 Anlas.",
            "- Generation must remain serial. Use `DelaySeconds=1`; do not run multiple generation jobs in parallel.",
            "- Do not use mock outputs as Approved or formal assets.",
            "",
            "## Batch Commands",
            "",
            "Run all planned generation:",
            "",
            "```powershell",
            "$env:NAI_ACCESS_TOKEN = '<set locally>'",
            payload["BatchCommands"]["RunAllGeneration"],
            "```",
            "",
            "After generation succeeds:",
            "",
            "```powershell",
            payload["BatchCommands"]["OptimizeAllAfterGeneration"],
            "# Review contact sheets under UnityClient/Assets/Art/_IncomingAI/<VisualID>/contact_sheet.",
            "# Put accepted image into selected/, or use processed fallback only for explicitly accepted first candidates.",
            payload["BatchCommands"]["SyncAllAfterReviewOrFallback"],
            payload["BatchCommands"]["RefreshIntegration"],
            payload["BatchCommands"]["RefreshQuality"],
            payload["BatchCommands"]["RefreshPlan"],
            "```",
            "",
            "## Planned Items",
            "",
            "| Order | VisualID | Priority | Domain | Type | Size | Prompt | Approved Path |",
            "|---:|---|---|---|---|---|---|---|",
        ]
    )
    for item in payload["Items"]:
        lines.append(
            "| "
            + " | ".join(
                [
                    md_cell(item["Order"]),
                    f"`{md_cell(item['VisualID'])}`",
                    md_cell(item["Priority"]),
                    md_cell(item["Domain"]),
                    md_cell(item["AssetType"]),
                    md_cell(item["ExpectedSize"]),
                    "ready" if item["PromptReady"] else "missing",
                    md_cell(item["ApprovedPath"]),
                ]
            )
            + " |"
        )

    lines.extend(["", "## Per-Item Commands", ""])
    for item in payload["Items"]:
        commands = item["Commands"]
        lines.extend(
            [
                f"### {item['Order']}. `{item['VisualID']}`",
                "",
                "```powershell",
                commands["RunGeneration"],
                commands["Optimize"],
                "# Review selected/contact_sheet before syncing.",
                commands["SyncApproved"],
                "```",
                "",
            ]
        )

    if summary.get("MissingManifestEntries"):
        lines.extend(["## Missing Manifest Entries", ""])
        for visual_id in summary["MissingManifestEntries"]:
            lines.append(f"- `{visual_id}`")
        lines.append("")

    return "\n".join(lines)


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generate execution plan for missing art assets.")
    parser.add_argument("--integration-candidates-path", default=DEFAULT_INTEGRATION_CANDIDATES)
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--output-json", default=DEFAULT_OUTPUT_JSON)
    parser.add_argument("--output-markdown", default=DEFAULT_OUTPUT_MARKDOWN)
    parser.add_argument("--snapshot-dir", default=DEFAULT_SNAPSHOT_DIR)
    parser.add_argument("--snapshot-tag", default="")
    parser.add_argument("--batch-id", default=DEFAULT_BATCH_ID)
    parser.add_argument("--provider", default="novelai")
    parser.add_argument("--status", default="prompted")
    parser.add_argument("--action", default="generate_needed")
    parser.add_argument("--variants", type=int, default=4)
    parser.add_argument("--delay-seconds", type=float, default=1.0)
    parser.add_argument("--last-probe-note", default="")
    parser.add_argument("--last-probe-batch-id", default="")
    parser.add_argument("--snapshot", action="store_true")
    return parser.parse_args()


def write_snapshot(args: argparse.Namespace, payload: dict[str, Any], markdown: str) -> tuple[Path, Path]:
    snapshot_dir = resolve_project_path(args.snapshot_dir)
    tag = safe_snapshot_tag(args.snapshot_tag or args.batch_id)
    stem = timestamp_filename()
    if tag:
        stem = f"{stem}_{tag}"
    snapshot_json = snapshot_dir / f"{stem}.json"
    snapshot_markdown = snapshot_dir / f"{stem}.md"
    write_json(snapshot_json, payload)
    snapshot_markdown.parent.mkdir(parents=True, exist_ok=True)
    snapshot_markdown.write_text(markdown + "\n", encoding="utf-8")
    return snapshot_json, snapshot_markdown


def main() -> int:
    args = parse_args()
    payload = build_payload(args)
    markdown = make_markdown(payload)

    output_json = resolve_project_path(args.output_json)
    output_markdown = resolve_project_path(args.output_markdown)
    write_json(output_json, payload)
    output_markdown.parent.mkdir(parents=True, exist_ok=True)
    output_markdown.write_text(markdown + "\n", encoding="utf-8")

    snapshot_paths: tuple[Path, Path] | None = None
    if args.snapshot:
        snapshot_paths = write_snapshot(args, payload, markdown)

    print(f"[OK] Art batch plan: {repo_path(output_markdown)}")
    if snapshot_paths is not None:
        _, snapshot_markdown = snapshot_paths
        print(f"[OK] Art batch plan snapshot: {repo_path(snapshot_markdown)}")
    print(
        "[OK] "
        f"planned={payload['Summary']['PlannedItems']}, "
        f"prompt_ready={payload['Summary']['PromptReadyItems']}, "
        f"provider={payload['RunConfig']['Provider']}, "
        f"batch={payload['RunConfig']['BatchID']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
