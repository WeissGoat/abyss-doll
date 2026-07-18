# -*- coding: utf-8 -*-
"""Generate art integration candidates from Manifest, Approved art, and Unity registry."""

from __future__ import annotations

import argparse
import json
import re
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from art_processing import resolve_latest_processed_candidate
from art_workspace import workspace_path


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]

DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_SCREENS = "美术文档/ui_design/screen_layouts.json"
DEFAULT_REGISTRY = "UnityClient/Assets/Resources/VisualAssetRegistry.asset"
DEFAULT_INCOMING_ROOT = "UnityClient/Assets/Art/_IncomingAI"
DEFAULT_OUTPUT_JSON = "美术文档/_generated/可接入素材清单.json"
DEFAULT_OUTPUT_MARKDOWN = "美术文档/_generated/可接入素材清单.md"
DEFAULT_SNAPSHOT_DIR = "美术文档/_generated/art_integration_snapshots"

IMAGE_EXTENSIONS = {".png", ".jpg", ".jpeg", ".webp"}
APPROVED_STATUSES = {"approved", "registered", "validated"}
DONE_STATUSES = {"validated", "deprecated", "rejected"}
PRIORITY_ORDER = {"P0": 0, "P1": 1, "P2": 2, "P3": 3}
ACTION_ORDER = {
    "program_integrate": 0,
    "acceptance_needed": 1,
    "art_approve": 2,
    "art_select": 3,
    "art_process": 4,
    "generate_needed": 5,
    "validated": 6,
    "deprecated": 7,
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


def as_list(value: Any) -> list[Any]:
    return value if isinstance(value, list) else []


def first_image(path: Path) -> Path | None:
    if not path.exists() or not path.is_dir():
        return None
    images = sorted(item for item in path.iterdir() if item.is_file() and item.suffix.lower() in IMAGE_EXTENSIONS)
    return images[0] if images else None


def approved_path_for(entry: dict[str, Any]) -> Path | None:
    value = entry.get("ApprovedPath") or entry.get("OutputPath")
    if not value:
        return None
    return resolve_project_path(str(value))


def collect_screen_refs(screens: dict[str, Any]) -> dict[str, list[str]]:
    refs: dict[str, list[str]] = defaultdict(list)
    for screen in as_list(screens.get("Screens")):
        if not isinstance(screen, dict):
            continue
        screen_id = str(screen.get("ScreenID", "") or "").strip()
        if not screen_id:
            continue
        visual_ids = [str(item).strip() for item in as_list(screen.get("RequiredVisualIDs")) if str(item).strip()]
        background = str(screen.get("BackgroundVisualID", "") or "").strip()
        if background:
            visual_ids.append(background)
        for visual_id in sorted(set(visual_ids)):
            refs[visual_id].append(f"screen:{screen_id}")
    return refs


def parse_registry_asset(path: Path) -> dict[str, dict[str, Any]]:
    entries: dict[str, dict[str, Any]] = {}
    if not path.exists():
        return entries
    current: dict[str, Any] | None = None
    visual_re = re.compile(r"^\s*-\s+VisualID:\s*(.*?)\s*$")
    sprite_re = re.compile(r"^\s*Sprite:\s*\{fileID:\s*([^,}]+)")
    for line in path.read_text(encoding="utf-8-sig").splitlines():
        visual_match = visual_re.match(line)
        if visual_match:
            visual_id = visual_match.group(1).strip().strip("'\"")
            current = {
                "VisualID": visual_id,
                "HasSprite": False,
                "SpriteFileID": "",
            }
            entries[visual_id] = current
            continue
        if current is None:
            continue
        sprite_match = sprite_re.match(line)
        if sprite_match:
            file_id = sprite_match.group(1).strip()
            current["SpriteFileID"] = file_id
            current["HasSprite"] = file_id != "0"
    return entries


def source_refs_for(entry: dict[str, Any]) -> list[str]:
    refs: list[str] = []
    source_type = str(entry.get("SourceType", "") or "").strip()
    config_id = str(entry.get("ConfigID", "") or "").strip()
    config_source = str(entry.get("ConfigSource", "") or "").strip()
    if config_source:
        refs.append(f"{source_type or 'source'}:{config_source}")
    elif source_type and config_id:
        refs.append(f"{source_type}:{config_id}")
    elif config_id:
        refs.append(f"config:{config_id}")
    return refs


def classify_entry(
    entry: dict[str, Any],
    screen_refs: dict[str, list[str]],
    registry_entries: dict[str, dict[str, Any]],
    incoming_root: Path,
) -> dict[str, Any]:
    visual_id = str(entry.get("VisualID", "") or "").strip()
    status = str(entry.get("Status", "") or "").strip()
    registry_status = str(entry.get("RegistryStatus", "") or "").strip()
    priority = str(entry.get("Priority", "") or "").strip()
    approved_path = approved_path_for(entry)
    approved_exists = bool(approved_path and approved_path.exists())
    meta_path = Path(str(approved_path) + ".meta") if approved_path else None
    meta_exists = bool(meta_path and meta_path.exists())
    registry_entry = registry_entries.get(visual_id)
    registry_has_sprite = bool(registry_entry and registry_entry.get("HasSprite"))
    workspace = workspace_path(incoming_root, entry)
    selected_image = first_image(workspace / "selected")
    latest_processed = resolve_latest_processed_candidate(workspace)
    processed_image = latest_processed.path
    raw_image = first_image(workspace / "raw")

    if status == "validated":
        action = "validated"
        reason = "Manifest 已标记 validated。"
    elif status in {"deprecated", "rejected"}:
        action = status
        reason = "资产已废弃或被拒绝，不进入接入队列。"
    elif status in APPROVED_STATUSES and approved_exists and meta_exists and not registry_has_sprite:
        action = "program_integrate"
        reason = "Approved PNG 和 .meta 已存在，但 VisualAssetRegistry 尚未登记。"
    elif status in APPROVED_STATUSES and approved_exists and not meta_exists:
        action = "program_integrate"
        reason = "Approved PNG 已存在，但缺少 Unity .meta，需要先导入或补齐。"
    elif status in APPROVED_STATUSES and approved_exists and registry_has_sprite:
        action = "acceptance_needed"
        reason = "素材已入库且 Registry 已登记，等待运行时截图验收或 Manifest 状态回填。"
    elif selected_image is not None:
        action = "art_approve"
        reason = "Incoming selected 已有候选，等待同步到 Approved。"
    elif latest_processed.processing_state in {"failed", "decision_required", "legacy_unverified"}:
        action = "art_select"
        reason = f"processed 最新轮次状态为 {latest_processed.processing_state}，需要美术决策或修复。"
    elif processed_image is not None:
        action = "art_select"
        reason = "Incoming processed 最新数字轮次已有唯一通过候选，等待美术筛选 selected。"
    elif raw_image is not None:
        action = "art_process"
        reason = "Incoming raw 已有候选，等待预处理和 contact sheet。"
    elif status in {"todo", "prompted", "generated", "selected"}:
        action = "generate_needed"
        reason = "Manifest 有需求但尚未形成可接入 Approved 素材。"
    else:
        action = "generate_needed"
        reason = "资产状态无法进入接入流程，需要先补齐生产链路。"

    referenced_by = []
    referenced_by.extend(screen_refs.get(visual_id, []))
    referenced_by.extend(source_refs_for(entry))
    referenced_by = sorted(set(ref for ref in referenced_by if ref))

    if action == "program_integrate" and approved_exists and meta_exists and referenced_by:
        confidence = "high"
    elif action in {"program_integrate", "acceptance_needed", "art_approve"}:
        confidence = "medium"
    else:
        confidence = "low"

    return {
        "VisualID": visual_id,
        "Action": action,
        "Reason": reason,
        "Confidence": confidence,
        "Priority": priority,
        "Domain": str(entry.get("Domain", "") or ""),
        "AssetType": str(entry.get("AssetType", "") or ""),
        "ConfigID": str(entry.get("ConfigID", "") or ""),
        "DisplayName": str(entry.get("DisplayName", "") or ""),
        "Status": status,
        "RegistryStatus": registry_status,
        "RegistryHasSprite": registry_has_sprite,
        "ApprovedPath": repo_path(approved_path),
        "ApprovedFileExists": approved_exists,
        "MetaFileExists": meta_exists,
        "SelectedCandidate": repo_path(selected_image),
        "ProcessedCandidate": repo_path(processed_image),
        "ProcessedRound": latest_processed.round_number,
        "ProcessingState": latest_processed.processing_state,
        "RawCandidate": repo_path(raw_image),
        "ReferencedBy": referenced_by,
    }


def sort_key(item: dict[str, Any]) -> tuple[int, int, str, str]:
    return (
        ACTION_ORDER.get(str(item.get("Action", "")), 99),
        PRIORITY_ORDER.get(str(item.get("Priority", "")), 99),
        str(item.get("Domain", "")),
        str(item.get("VisualID", "")),
    )


def best_confidence(values: list[str]) -> str:
    order = {"high": 0, "medium": 1, "low": 2}
    clean = [value for value in values if value]
    if not clean:
        return "low"
    return sorted(clean, key=lambda value: order.get(value, 99))[0]


def first_non_empty(values: list[Any]) -> str:
    for value in values:
        text = str(value or "").strip()
        if text:
            return text
    return ""


def join_unique(values: list[Any]) -> str:
    unique = sorted({str(value).strip() for value in values if str(value or "").strip()})
    return ", ".join(unique)


def merge_candidate_group(items: list[dict[str, Any]]) -> dict[str, Any]:
    base = sorted(items, key=sort_key)[0].copy()
    referenced_by: list[str] = []
    for item in items:
        referenced_by.extend(str(ref) for ref in as_list(item.get("ReferencedBy")))

    base["ReferencedBy"] = sorted(set(ref for ref in referenced_by if ref))
    base["ConfigID"] = join_unique([item.get("ConfigID") for item in items])
    base["DisplayName"] = " / ".join(
        sorted({str(item.get("DisplayName", "")).strip() for item in items if str(item.get("DisplayName", "")).strip()})
    )
    base["Status"] = join_unique([item.get("Status") for item in items])
    base["RegistryStatus"] = join_unique([item.get("RegistryStatus") for item in items])
    base["ApprovedFileExists"] = any(bool(item.get("ApprovedFileExists")) for item in items)
    base["MetaFileExists"] = any(bool(item.get("MetaFileExists")) for item in items)
    base["RegistryHasSprite"] = any(bool(item.get("RegistryHasSprite")) for item in items)
    base["ApprovedPath"] = first_non_empty([item.get("ApprovedPath") for item in items])
    base["SelectedCandidate"] = first_non_empty([item.get("SelectedCandidate") for item in items])
    base["ProcessedCandidate"] = first_non_empty([item.get("ProcessedCandidate") for item in items])
    base["ProcessedRound"] = next(
        (item.get("ProcessedRound") for item in items if item.get("ProcessedRound") is not None),
        None,
    )
    base["ProcessingState"] = first_non_empty([item.get("ProcessingState") for item in items])
    base["RawCandidate"] = first_non_empty([item.get("RawCandidate") for item in items])
    base["Confidence"] = best_confidence([str(item.get("Confidence", "")) for item in items])
    base["ManifestEntryCount"] = len(items)
    if len(items) > 1:
        base["Reason"] = f"{base['Reason']} 已合并 {len(items)} 个 Manifest 来源。"
    return base


def merge_candidates_by_visual_id(candidates: list[dict[str, Any]]) -> list[dict[str, Any]]:
    grouped: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for item in candidates:
        grouped[str(item.get("VisualID", ""))].append(item)
    merged = [merge_candidate_group(items) for _, items in grouped.items()]
    merged.sort(key=sort_key)
    return merged


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
        "# 可接入素材清单",
        "",
        "> Generated by `tools/美术工具/Generate-ArtIntegrationCandidates.ps1`.",
        "",
        "## Summary",
        "",
        f"* GeneratedAt: `{payload['GeneratedAt']}`",
        f"* Manifest entries: `{summary['TotalManifestEntries']}`",
        f"* Unique VisualIDs: `{summary['TotalVisualIDs']}`",
        f"* Candidate entries: `{summary['TotalCandidates']}`",
        f"* Program integrate: `{summary['ActionCounts'].get('program_integrate', 0)}`",
        f"* Acceptance needed: `{summary['ActionCounts'].get('acceptance_needed', 0)}`",
        f"* Art approve: `{summary['ActionCounts'].get('art_approve', 0)}`",
        f"* Art select: `{summary['ActionCounts'].get('art_select', 0)}`",
        f"* Art process: `{summary['ActionCounts'].get('art_process', 0)}`",
        f"* Generate needed: `{summary['ActionCounts'].get('generate_needed', 0)}`",
        "",
        "## Program Integrate",
        "",
    ]

    program_items = [item for item in payload["Candidates"] if item["Action"] == "program_integrate"]
    if program_items:
        lines.extend(
            [
                "| Priority | VisualID | Domain | Name | ReferencedBy | Approved | Registry | Reason |",
                "|---|---|---|---|---|---|---|---|",
            ]
        )
        for item in program_items:
            lines.append(
                "| "
                + " | ".join(
                    [
                        md_cell(item["Priority"]),
                        f"`{md_cell(item['VisualID'])}`",
                        md_cell(item["Domain"]),
                        md_cell(item["DisplayName"]),
                        md_cell(item["ReferencedBy"]),
                        md_cell(item["ApprovedPath"]),
                        "registered" if item["RegistryHasSprite"] else "missing",
                        md_cell(item["Reason"]),
                    ]
                )
                + " |"
            )
    else:
        lines.append("- 当前没有等待程序登记的 Approved 素材。")

    lines.extend(["", "## Other Actions", ""])
    other_items = [item for item in payload["Candidates"] if item["Action"] != "program_integrate"]
    if other_items:
        lines.extend(
            [
                "| Action | Priority | VisualID | Status | Processing | Registry | Candidate | Reason |",
                "|---|---|---|---|---|---|---|---|",
            ]
        )
        for item in other_items:
            candidate = item["SelectedCandidate"] or item["ProcessedCandidate"] or item["RawCandidate"] or item["ApprovedPath"] or "-"
            lines.append(
                "| "
                + " | ".join(
                    [
                        f"`{md_cell(item['Action'])}`",
                        md_cell(item["Priority"]),
                        f"`{md_cell(item['VisualID'])}`",
                        f"`{md_cell(item['Status'])}`",
                        f"{md_cell(item.get('ProcessedRound', '-'))} / {md_cell(item.get('ProcessingState', '-'))}",
                        "registered" if item["RegistryHasSprite"] else "missing",
                        md_cell(candidate),
                        md_cell(item["Reason"]),
                    ]
                )
                + " |"
            )
    else:
        lines.append("- None.")

    lines.extend(
        [
            "",
            "## Action Meanings",
            "",
            "- `program_integrate`: Approved PNG 已可用，程序侧应导入 / 重建 `VisualAssetRegistry` 并接入对应 UI 或配置引用。",
            "- `acceptance_needed`: Registry 已能找到素材，下一步是运行时截图验收或回填 Manifest 状态。",
            "- `art_approve`: 当前 Manifest entry 的 Profile 工作区 `selected/` 已有候选，等待同步到 Approved。",
            "- `art_select`: processed 已有候选，等待美术筛选。",
            "- `art_process`: raw 已有候选，等待预处理、contact sheet 和筛选。",
            "- `generate_needed`: Manifest 有需求，但尚未生成可接入素材。",
        ]
    )
    return "\n".join(lines) + "\n"


def build_payload(args: argparse.Namespace) -> dict[str, Any]:
    manifest_path = resolve_project_path(args.manifest_path)
    screens_path = resolve_project_path(args.screens_path)
    registry_path = resolve_project_path(args.registry_path)
    registry_display_path = str(args.registry_display_path or "").strip()
    incoming_root = resolve_project_path(args.incoming_root)
    manifest = read_json(manifest_path, {})
    screens = read_json(screens_path, {})
    registry_entries = parse_registry_asset(registry_path)
    screen_refs = collect_screen_refs(screens)

    candidates = []
    entries = [entry for entry in as_list(manifest.get("Entries")) if isinstance(entry, dict) and entry.get("VisualID")]
    for entry in entries:
        item = classify_entry(entry, screen_refs, registry_entries, incoming_root)
        if args.include_done or item["Action"] not in DONE_STATUSES:
            candidates.append(item)
    candidates = merge_candidates_by_visual_id(candidates)

    action_counts = Counter(item["Action"] for item in candidates)
    domain_counts = Counter(item["Domain"] for item in candidates)
    priority_counts = Counter(item["Priority"] for item in candidates)
    return {
        "GeneratedAt": timestamp_text(),
        "SnapshotTag": str(args.snapshot_tag or ""),
        "Inputs": {
            "ManifestPath": repo_path(manifest_path),
            "ScreensPath": repo_path(screens_path),
            "RegistryPath": registry_display_path or repo_path(registry_path),
            "IncomingRoot": repo_path(incoming_root),
        },
        "Summary": {
            "TotalManifestEntries": len(entries),
            "TotalVisualIDs": len({str(entry.get("VisualID", "")) for entry in entries}),
            "TotalCandidates": len(candidates),
            "ActionCounts": dict(sorted(action_counts.items())),
            "DomainCounts": dict(sorted(domain_counts.items())),
            "PriorityCounts": dict(sorted(priority_counts.items())),
        },
        "Candidates": candidates,
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generate art integration candidates.")
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--screens-path", default=DEFAULT_SCREENS)
    parser.add_argument("--registry-path", default=DEFAULT_REGISTRY)
    parser.add_argument("--registry-display-path", default="")
    parser.add_argument("--incoming-root", default=DEFAULT_INCOMING_ROOT)
    parser.add_argument("--output-json", default=DEFAULT_OUTPUT_JSON)
    parser.add_argument("--output-markdown", default=DEFAULT_OUTPUT_MARKDOWN)
    parser.add_argument("--snapshot-dir", default=DEFAULT_SNAPSHOT_DIR)
    parser.add_argument("--snapshot-tag", default="")
    parser.add_argument("--snapshot", action="store_true")
    parser.add_argument("--include-done", action="store_true")
    return parser.parse_args()


def write_snapshot(args: argparse.Namespace, payload: dict[str, Any], markdown: str) -> tuple[Path, Path]:
    snapshot_dir = resolve_project_path(args.snapshot_dir)
    tag = safe_snapshot_tag(args.snapshot_tag)
    stem = timestamp_filename()
    if tag:
        stem = f"{stem}_{tag}"
    snapshot_json = snapshot_dir / f"{stem}.json"
    snapshot_markdown = snapshot_dir / f"{stem}.md"
    write_json(snapshot_json, payload)
    snapshot_markdown.write_text(markdown, encoding="utf-8")
    return snapshot_json, snapshot_markdown


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
    counts = payload["Summary"]["ActionCounts"]
    print(f"[OK] Art integration candidates: {repo_path(output_markdown)}")
    if snapshot_paths is not None:
        _, snapshot_markdown = snapshot_paths
        print(f"[OK] Art integration snapshot: {repo_path(snapshot_markdown)}")
    print(
        "[OK] "
        f"program_integrate={counts.get('program_integrate', 0)}, "
        f"acceptance_needed={counts.get('acceptance_needed', 0)}, "
        f"art_approve={counts.get('art_approve', 0)}, "
        f"art_select={counts.get('art_select', 0)}, "
        f"art_process={counts.get('art_process', 0)}, "
        f"generate_needed={counts.get('generate_needed', 0)}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
