# -*- coding: utf-8 -*-
"""Generate an executable Visual V2 replacement plan from the art quality backlog."""

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

DEFAULT_QUALITY_BACKLOG = "美术文档/_generated/素材质量替换清单.json"
DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_OUTPUT_JSON = "美术文档/_generated/VisualV2生成计划.json"
DEFAULT_OUTPUT_MARKDOWN = "美术文档/_generated/VisualV2生成计划.md"
DEFAULT_SNAPSHOT_DIR = "美术文档/_generated/visual_v2_plan_snapshots"
DEFAULT_BATCH_ID = "nai_visual_v2_20260525_01"

DOMAIN_ORDER = {
    "ui": 0,
    "order": 1,
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


def ps_single_quote(value: str) -> str:
    return "'" + value.replace("'", "''") + "'"


def comma_visual_ids(items: list[dict[str, Any]]) -> str:
    return ",".join(str(item["VisualID"]) for item in items)


def observed_anlas_note(size: str, provider: str) -> str:
    if provider != "novelai":
        return ""
    if size == "512x512":
        return "Observed probe on 2026-05-25: NovelAI required 5 Anlas for one 512x512 image."
    return ""


def replacement_batch_key(item: dict[str, Any]) -> str:
    visual_id = str(item.get("VisualID", "") or "")
    domain = str(item.get("Domain", "") or "")
    asset_type = str(item.get("AssetType", "") or "")
    priority = str(item.get("Priority", "") or "")
    if priority == "P1" and visual_id.startswith("ui_combat_"):
        return "p1_combat_readability"
    if priority == "P1" and domain == "background":
        return "p1_core_backgrounds"
    if domain == "ui" and (
        asset_type in {"panel", "divider", "frame", "slot", "marker", "effect_overlay"}
        or visual_id.startswith("ui_settlement_")
        or visual_id in {"ui_panel_main", "ui_list_row_normal", "ui_list_row_selected", "ui_title_divider"}
    ):
        return "p1_p2_ui_skin_core"
    if domain == "ui":
        return "p2_shared_ui_icons"
    if domain == "background":
        return "p2_scene_backgrounds"
    if domain in {"order", "rumor", "faction"}:
        return "p2_economy_social_icons"
    if domain in {"prosthetic", "chassis", "memento"}:
        return "p2_growth_room_assets"
    return "p2_misc_runtime_assets"


BATCH_TITLES = {
    "p1_combat_readability": "P1 combat readability icons and markers",
    "p1_core_backgrounds": "P1 safe room and stairs room backgrounds",
    "p1_p2_ui_skin_core": "P1/P2 reusable UI skin core",
    "p2_shared_ui_icons": "P2 shared UI icons",
    "p2_scene_backgrounds": "P2 scene backgrounds",
    "p2_economy_social_icons": "P2 economy, order, rumor, and faction icons",
    "p2_growth_room_assets": "P2 growth, chassis, prosthetic, and room memento assets",
    "p2_misc_runtime_assets": "P2 miscellaneous runtime assets",
}


BATCH_ORDER = list(BATCH_TITLES.keys())


def build_recommended_batches(plan_items: list[dict[str, Any]], args: argparse.Namespace) -> list[dict[str, Any]]:
    grouped: dict[str, list[dict[str, Any]]] = {key: [] for key in BATCH_ORDER}
    for item in plan_items:
        grouped.setdefault(replacement_batch_key(item), []).append(item)

    batches: list[dict[str, Any]] = []
    for key in BATCH_ORDER:
        items = grouped.get(key, [])
        if not items:
            continue
        batch_id = f"{args.batch_id}_{key}"
        visual_ids = comma_visual_ids(items)
        domain_counts = Counter(item["Domain"] for item in items)
        size_counts = Counter(item["ExpectedSize"] for item in items)
        ready_count = sum(1 for item in items if item["PromptReady"])
        batches.append(
            {
                "Key": key,
                "Title": BATCH_TITLES.get(key, key),
                "BatchID": batch_id,
                "ItemCount": len(items),
                "PromptReadyItems": ready_count,
                "DomainCounts": dict(sorted(domain_counts.items())),
                "SizeCounts": dict(sorted(size_counts.items())),
                "VisualIDs": [str(item["VisualID"]) for item in items],
                "Commands": {
                    "RunGeneration": (
                        f".\\tools\\缇庢湳宸ュ叿\\Run-ArtGeneration.ps1 -Config .\\tools\\缇庢湳宸ュ叿\\ai_image_gateway.local.yaml "
                        f"-Provider {args.provider} -Status approved -VisualID {visual_ids} "
                        f"-Variants {args.variants} -DelaySeconds {args.delay_seconds:g} "
                        f"-BatchID {batch_id} -PreserveStatus"
                    ),
                    "Optimize": f".\\tools\\缇庢湳宸ュ叿\\Optimize-ArtAssets.ps1 -Status approved -CandidateBatchID {batch_id} -Overwrite",
                    "RefreshIntegration": f".\\tools\\缇庢湳宸ュ叿\\Generate-ArtIntegrationCandidates.ps1 -Snapshot -SnapshotTag {batch_id}",
                    "RefreshQuality": f".\\tools\\缇庢湳宸ュ叿\\Generate-ArtQualityBacklog.ps1 -Snapshot -SnapshotTag {batch_id}",
                    "RefreshPlan": f".\\tools\\缇庢湳宸ュ叿\\Generate-VisualV2Plan.ps1 -Snapshot -SnapshotTag {batch_id}_after -BatchID {args.batch_id}",
                },
            }
        )
    return batches


def build_item(
    backlog_item: dict[str, Any],
    manifest_entry: dict[str, Any],
    args: argparse.Namespace,
) -> dict[str, Any]:
    visual_id = str(backlog_item.get("VisualID", "") or "")
    size = expected_size_from_manifest(manifest_entry) or str(backlog_item.get("ExpectedSize", "") or "")
    prompt_ready = bool(
        str(manifest_entry.get("PromptEN", "") or "").strip()
        and str(manifest_entry.get("NegativePromptEN", "") or "").strip()
        and isinstance(manifest_entry.get("Spec"), dict)
    )
    command_base = (
        f".\\tools\\美术工具\\Run-ArtGeneration.ps1 -Config .\\tools\\美术工具\\ai_image_gateway.local.yaml "
        f"-Provider {args.provider} -Status approved -VisualID {visual_id} "
        f"-Variants {args.variants} -DelaySeconds {args.delay_seconds:g} "
        f"-BatchID {args.batch_id} -PreserveStatus"
    )
    optimize_command = (
        f".\\tools\\美术工具\\Optimize-ArtAssets.ps1 -Status approved -VisualID {visual_id} "
        f"-CandidateBatchID {args.batch_id} -Overwrite"
    )
    sync_command = (
        f".\\tools\\美术工具\\Sync-ApprovedArt.ps1 -Status approved -VisualID {visual_id} "
        f"-CandidateBatchID {args.batch_id} -AllowProcessedFallback -Overwrite "
        f"-QualityTier formal_ai_v2 -ClearCandidate"
    )
    return {
        "VisualID": visual_id,
        "Order": 0,
        "Priority": str(backlog_item.get("Priority", "") or ""),
        "Domain": str(backlog_item.get("Domain", "") or ""),
        "AssetType": str(backlog_item.get("AssetType", "") or ""),
        "DisplayName": str(backlog_item.get("DisplayName", "") or ""),
        "Status": str(backlog_item.get("Status", "") or ""),
        "CurrentQuality": str(backlog_item.get("CurrentQuality", "") or ""),
        "TargetQuality": str(backlog_item.get("TargetQuality", "formal_ai_v2") or "formal_ai_v2"),
        "PromptReady": prompt_ready,
        "ExpectedSize": size,
        "ApprovedPath": str(backlog_item.get("ApprovedPath", "") or ""),
        "ReplaceWithoutProgramChange": bool(backlog_item.get("ReplaceWithoutProgramChange", True)),
        "ProgramCanUseCurrent": bool(backlog_item.get("ProgramCanUseCurrent", True)),
        "AnlasNote": observed_anlas_note(size, args.provider),
        "Commands": {
            "RunGeneration": command_base,
            "Optimize": optimize_command,
            "SyncApproved": sync_command,
        },
    }


def build_payload(args: argparse.Namespace) -> dict[str, Any]:
    backlog_path = resolve_project_path(args.quality_backlog_path)
    manifest_path = resolve_project_path(args.manifest_path)
    backlog = read_json(backlog_path, {})
    manifest = read_json(manifest_path, {})
    manifest_entries = {
        str(entry.get("VisualID", "") or ""): entry
        for entry in as_list(manifest.get("Entries"))
        if isinstance(entry, dict)
    }

    raw_items = [
        item
        for item in as_list(backlog.get("Items"))
        if isinstance(item, dict) and item.get("Action") == "visual_v2_replace"
    ]
    raw_items.sort(key=item_sort_key)
    plan_items: list[dict[str, Any]] = []
    missing_manifest: list[str] = []
    for index, item in enumerate(raw_items, start=1):
        visual_id = str(item.get("VisualID", "") or "")
        manifest_entry = manifest_entries.get(visual_id)
        if not manifest_entry:
            missing_manifest.append(visual_id)
            continue
        plan_item = build_item(item, manifest_entry, args)
        plan_item["Order"] = index
        plan_items.append(plan_item)

    domain_counts = Counter(item["Domain"] for item in plan_items)
    size_counts = Counter(item["ExpectedSize"] for item in plan_items)
    ready_count = sum(1 for item in plan_items if item["PromptReady"])
    all_ids = comma_visual_ids(plan_items)
    batch_generation_command = (
        f".\\tools\\美术工具\\Run-ArtGeneration.ps1 -Config .\\tools\\美术工具\\ai_image_gateway.local.yaml "
        f"-Provider {args.provider} -Status approved -VisualID {all_ids} "
        f"-Variants {args.variants} -DelaySeconds {args.delay_seconds:g} "
        f"-BatchID {args.batch_id} -PreserveStatus"
        if all_ids
        else ""
    )

    recommended_batches = build_recommended_batches(plan_items, args)
    return {
        "GeneratedAt": timestamp_text(),
        "SnapshotTag": str(args.snapshot_tag or ""),
        "Inputs": {
            "QualityBacklogPath": repo_path(backlog_path),
            "ManifestPath": repo_path(manifest_path),
        },
        "RunConfig": {
            "Provider": args.provider,
            "BatchID": args.batch_id,
            "Variants": args.variants,
            "DelaySeconds": args.delay_seconds,
            "QualityTierAfterSync": "formal_ai_v2",
            "LastProbeBatchID": args.last_probe_batch_id,
            "LastProbeNote": args.last_probe_note,
        },
        "Summary": {
            "TotalBacklogItems": len(raw_items),
            "PlannedItems": len(plan_items),
            "PromptReadyItems": ready_count,
            "MissingManifestEntries": missing_manifest,
            "DomainCounts": dict(sorted(domain_counts.items())),
            "SizeCounts": dict(sorted(size_counts.items())),
        },
        "BatchCommands": {
            "RunAllGeneration": batch_generation_command,
            "OptimizeAllAfterGeneration": f".\\tools\\美术工具\\Optimize-ArtAssets.ps1 -Status approved -CandidateBatchID {args.batch_id} -Overwrite",
            "RefreshIntegration": f".\\tools\\美术工具\\Generate-ArtIntegrationCandidates.ps1 -Snapshot -SnapshotTag {args.batch_id}",
            "RefreshQuality": f".\\tools\\美术工具\\Generate-ArtQualityBacklog.ps1 -Snapshot -SnapshotTag {args.batch_id}",
            "RefreshPlan": f".\\tools\\美术工具\\Generate-VisualV2Plan.ps1 -Snapshot -SnapshotTag {args.batch_id}_after -BatchID {args.batch_id}",
        },
        "RecommendedBatches": recommended_batches,
        "Items": plan_items,
    }


def md_cell(value: Any) -> str:
    if isinstance(value, bool):
        text = "yes" if value else "no"
    elif value is None:
        text = ""
    else:
        text = str(value)
    return text.replace("|", "\\|").replace("\n", " ")


def make_markdown(payload: dict[str, Any]) -> str:
    summary = payload["Summary"]
    run = payload["RunConfig"]
    lines = [
        "# Visual V2 生成计划",
        "",
        "> Generated by `tools/美术工具/Generate-VisualV2Plan.ps1`.",
        "",
        "## Summary",
        "",
        f"* GeneratedAt: `{payload['GeneratedAt']}`",
        f"* Provider: `{run['Provider']}`",
        f"* BatchID: `{run['BatchID']}`",
        f"* Variants per asset: `{run['Variants']}`",
        f"* Planned items: `{summary['PlannedItems']}`",
        f"* Prompt ready: `{summary['PromptReadyItems']}`",
        f"* Domain counts: `{json.dumps(summary['DomainCounts'], ensure_ascii=False)}`",
        f"* Size counts: `{json.dumps(summary['SizeCounts'], ensure_ascii=False)}`",
        "",
    ]
    if run.get("LastProbeNote"):
        lines.extend(["## Last Probe", "", f"* Batch: `{run.get('LastProbeBatchID', '')}`", f"* Result: {run['LastProbeNote']}", ""])

    lines.extend(
        [
            "## Preconditions",
            "",
            "- `NAI_ACCESS_TOKEN` must be set locally.",
            "- NovelAI Anlas must be sufficient. The latest probe showed that one 512x512 image required 5 Anlas.",
            "- Generation must remain serial; current script already sends one image per request and uses `DelaySeconds` between requests.",
            "- Do not use mock outputs as `formal_ai_v2`.",
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
            "# Review contact sheets under UnityClient/Assets/Art/_IncomingAI/<VisualID>/contact_sheet before syncing.",
            "# Then sync each accepted VisualID with its per-item SyncApproved command below.",
            payload["BatchCommands"]["RefreshIntegration"],
            payload["BatchCommands"]["RefreshQuality"],
            payload["BatchCommands"]["RefreshPlan"],
            "```",
            "",
            "## Recommended Execution Batches",
            "",
            "These batches are the preferred execution order for serial NovelAI replacement work. Keep missing-art generation separate from same-path quality replacement, review contact sheets per batch, and sync with the per-item meta guard commands after selection.",
            "",
            "| Order | BatchID | Title | Items | Prompt Ready | Domains | Sizes |",
            "|---:|---|---|---:|---:|---|---|",
        ]
    )
    for index, batch in enumerate(payload.get("RecommendedBatches", []), start=1):
        lines.append(
            "| "
            + " | ".join(
                [
                    md_cell(index),
                    f"`{md_cell(batch['BatchID'])}`",
                    md_cell(batch["Title"]),
                    md_cell(batch["ItemCount"]),
                    md_cell(batch["PromptReadyItems"]),
                    md_cell(json.dumps(batch["DomainCounts"], ensure_ascii=False)),
                    md_cell(json.dumps(batch["SizeCounts"], ensure_ascii=False)),
                ]
            )
            + " |"
        )
    lines.extend(
        [
            "",
            "Per-batch command examples:",
            "",
        ]
    )
    for batch in payload.get("RecommendedBatches", []):
        commands = batch["Commands"]
        lines.extend(
            [
                f"### `{batch['BatchID']}`",
                "",
                "```powershell",
                commands["RunGeneration"],
                commands["Optimize"],
                "# Review selected/contact_sheet before syncing accepted VisualIDs with per-item SyncApproved commands below.",
                commands["RefreshIntegration"],
                commands["RefreshQuality"],
                commands["RefreshPlan"],
                "```",
                "",
            ]
        )
    lines.extend(
        [
            "## Planned Items",
            "",
            "| Order | VisualID | Domain | Type | Size | Prompt | Current | Approved |",
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
                    md_cell(item["Domain"]),
                    md_cell(item["AssetType"]),
                    md_cell(item["ExpectedSize"]),
                    "ready" if item["PromptReady"] else "missing",
                    md_cell(item["CurrentQuality"]),
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
    parser = argparse.ArgumentParser(description="Generate Visual V2 replacement execution plan.")
    parser.add_argument("--quality-backlog-path", default=DEFAULT_QUALITY_BACKLOG)
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--output-json", default=DEFAULT_OUTPUT_JSON)
    parser.add_argument("--output-markdown", default=DEFAULT_OUTPUT_MARKDOWN)
    parser.add_argument("--snapshot-dir", default=DEFAULT_SNAPSHOT_DIR)
    parser.add_argument("--snapshot-tag", default="")
    parser.add_argument("--batch-id", default=DEFAULT_BATCH_ID)
    parser.add_argument("--provider", default="novelai")
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

    print(f"[OK] Visual V2 plan: {repo_path(output_markdown)}")
    if snapshot_paths is not None:
        _, snapshot_markdown = snapshot_paths
        print(f"[OK] Visual V2 plan snapshot: {repo_path(snapshot_markdown)}")
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
