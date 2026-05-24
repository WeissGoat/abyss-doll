# -*- coding: utf-8 -*-
"""Generate Visual V2 art quality backlog from the current art Manifest."""

from __future__ import annotations

import argparse
import json
import re
from collections import Counter
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from PIL import Image


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]

DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_OUTPUT_JSON = "美术文档/_generated/素材质量替换清单.json"
DEFAULT_OUTPUT_MARKDOWN = "美术文档/_generated/素材质量替换清单.md"
DEFAULT_SNAPSHOT_DIR = "美术文档/_generated/art_quality_snapshots"

APPROVED_STATUSES = {"approved", "registered", "validated"}
PRIORITY_ORDER = {"P0": 0, "P1": 1, "P2": 2, "P3": 3}
ACTION_ORDER = {
    "technical_fix": 0,
    "visual_v2_replace": 1,
    "spec_review": 2,
}
LOCAL_QUALITY_PATTERNS = (
    "local_v0",
    "placeholder",
    "temporary",
    "fallback",
)


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


def display_spec(entry: dict[str, Any]) -> dict[str, Any]:
    spec = entry.get("Spec")
    if not isinstance(spec, dict):
        return {}
    nested = spec.get("DisplaySpec")
    return nested if isinstance(nested, dict) else {}


def approved_path_for(entry: dict[str, Any]) -> Path | None:
    value = entry.get("ApprovedPath") or entry.get("OutputPath")
    if not value:
        return None
    return resolve_project_path(str(value))


def has_local_quality_marker(entry: dict[str, Any]) -> bool:
    fields = [
        str(entry.get("BatchID", "") or ""),
        str(entry.get("Notes", "") or ""),
        str(entry.get("SelectedPath", "") or ""),
        str(entry.get("ApprovedPath", "") or ""),
    ]
    text = "\n".join(fields).lower()
    return any(pattern in text for pattern in LOCAL_QUALITY_PATTERNS)


def read_image_facts(path: Path | None) -> dict[str, Any]:
    if path is None or not path.exists():
        return {
            "Exists": False,
            "Width": 0,
            "Height": 0,
            "Mode": "",
            "HasAlpha": False,
            "TransparentPixelCount": 0,
            "SemiTransparentPixelCount": 0,
            "AlphaMin": 255,
            "AlphaMax": 255,
        }
    with Image.open(path) as image:
        facts: dict[str, Any] = {
            "Exists": True,
            "Width": image.width,
            "Height": image.height,
            "Mode": image.mode,
            "HasAlpha": image.mode in ("RGBA", "LA") or "transparency" in image.info,
            "TransparentPixelCount": 0,
            "SemiTransparentPixelCount": 0,
            "AlphaMin": 255,
            "AlphaMax": 255,
        }
        if facts["HasAlpha"]:
            alpha = image.convert("RGBA").getchannel("A")
            histogram = alpha.histogram()
            facts["TransparentPixelCount"] = histogram[0]
            facts["SemiTransparentPixelCount"] = sum(histogram[1:255])
            facts["AlphaMin"] = next((index for index, count in enumerate(histogram) if count), 255)
            facts["AlphaMax"] = next((index for index in range(255, -1, -1) if histogram[index]), 255)
        return facts


def expected_size(entry: dict[str, Any]) -> tuple[int, int]:
    src = source_spec(entry)
    try:
        width = int(src.get("Width", 0) or 0)
        height = int(src.get("Height", 0) or 0)
    except (TypeError, ValueError):
        return (0, 0)
    return (width, height)


def alpha_required(entry: dict[str, Any]) -> bool:
    src = source_spec(entry)
    return bool(src.get("AlphaRequired", False))


def background_kind(entry: dict[str, Any]) -> str:
    src = source_spec(entry)
    return str(src.get("Background", "") or "")


def build_item(entry: dict[str, Any]) -> dict[str, Any] | None:
    status = str(entry.get("Status", "") or "").strip()
    if status not in APPROVED_STATUSES:
        return None

    visual_id = str(entry.get("VisualID", "") or "").strip()
    if not visual_id:
        return None

    approved_path = approved_path_for(entry)
    image_facts = read_image_facts(approved_path)
    expected_width, expected_height = expected_size(entry)
    reasons: list[str] = []
    actions: set[str] = set()
    current_quality = "approved"

    local_quality = has_local_quality_marker(entry)
    if local_quality:
        actions.add("visual_v2_replace")
        current_quality = "local_v0"
        reasons.append("Manifest 标记为 local_v0 / placeholder，可用于接入但不是正式视觉质量。")

    if image_facts["Exists"] and expected_width and expected_height:
        if image_facts["Width"] != expected_width or image_facts["Height"] != expected_height:
            actions.add("spec_review")
            reasons.append(
                f"实际尺寸 {image_facts['Width']}x{image_facts['Height']} 与 SourceSpec {expected_width}x{expected_height} 不一致。"
            )

    if image_facts["Exists"] and not alpha_required(entry) and image_facts["HasAlpha"]:
        if image_facts["TransparentPixelCount"] or image_facts["SemiTransparentPixelCount"]:
            actions.add("technical_fix")
            current_quality = "technical_risk"
            reasons.append("SourceSpec 要求不透明，但 PNG 存在透明或半透明像素。")

    if not actions:
        return None

    action = sorted(actions, key=lambda value: ACTION_ORDER.get(value, 99))[0]
    target_quality = "formal_ai_v2"
    if action == "technical_fix":
        target_quality = "opaque_spec_fix"
    elif action == "spec_review":
        target_quality = "spec_aligned_asset"

    src = source_spec(entry)
    display = display_spec(entry)
    prompt_ready = bool(str(entry.get("PromptEN", "") or "").strip() and isinstance(entry.get("Spec"), dict))

    return {
        "VisualID": visual_id,
        "Action": action,
        "Priority": str(entry.get("Priority", "") or ""),
        "Domain": str(entry.get("Domain", "") or ""),
        "AssetType": str(entry.get("AssetType", "") or ""),
        "ConfigID": str(entry.get("ConfigID", "") or ""),
        "DisplayName": str(entry.get("DisplayName", "") or ""),
        "Status": status,
        "CurrentQuality": current_quality,
        "TargetQuality": target_quality,
        "ProgramCanUseCurrent": action != "technical_fix",
        "ReplaceWithoutProgramChange": True,
        "PromptReady": prompt_ready,
        "ApprovedPath": repo_path(approved_path),
        "ApprovedFileExists": bool(image_facts["Exists"]),
        "ExpectedSize": f"{expected_width}x{expected_height}" if expected_width and expected_height else "",
        "ActualSize": f"{image_facts['Width']}x{image_facts['Height']}" if image_facts["Exists"] else "",
        "SourceBackground": background_kind(entry),
        "AlphaRequired": alpha_required(entry),
        "ImageAlpha": {
            "HasAlpha": bool(image_facts["HasAlpha"]),
            "TransparentPixelCount": int(image_facts["TransparentPixelCount"]),
            "SemiTransparentPixelCount": int(image_facts["SemiTransparentPixelCount"]),
            "AlphaMin": int(image_facts["AlphaMin"]),
            "AlphaMax": int(image_facts["AlphaMax"]),
        },
        "DisplaySize": (
            f"{display.get('DisplayWidth', '')}x{display.get('DisplayHeight', '')}"
            if display.get("DisplayWidth") and display.get("DisplayHeight")
            else ""
        ),
        "Reason": " ".join(reasons),
        "BatchID": str(entry.get("BatchID", "") or ""),
        "Notes": str(entry.get("Notes", "") or ""),
    }


def sort_key(item: dict[str, Any]) -> tuple[int, int, str, str]:
    return (
        ACTION_ORDER.get(str(item.get("Action", "")), 99),
        PRIORITY_ORDER.get(str(item.get("Priority", "")), 99),
        str(item.get("Domain", "")),
        str(item.get("VisualID", "")),
    )


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
    lines = [
        "# 素材质量替换清单",
        "",
        "> Generated by `tools/美术工具/Generate-ArtQualityBacklog.ps1`.",
        "",
        "## Summary",
        "",
        f"* GeneratedAt: `{payload['GeneratedAt']}`",
        f"* Manifest entries: `{summary['TotalManifestEntries']}`",
        f"* Backlog items: `{summary['TotalBacklogItems']}`",
        f"* Technical fix: `{summary['ActionCounts'].get('technical_fix', 0)}`",
        f"* Visual V2 replace: `{summary['ActionCounts'].get('visual_v2_replace', 0)}`",
        f"* Spec review: `{summary['ActionCounts'].get('spec_review', 0)}`",
        "",
        "## Technical Fix",
        "",
    ]

    technical_items = [item for item in payload["Items"] if item["Action"] == "technical_fix"]
    if technical_items:
        lines.extend(
            [
                "| Priority | VisualID | Name | Expected | Actual | Alpha | Approved | Reason |",
                "|---|---|---|---|---|---|---|---|",
            ]
        )
        for item in technical_items:
            alpha = item["ImageAlpha"]
            alpha_text = (
                f"transparent={alpha['TransparentPixelCount']}, "
                f"semi={alpha['SemiTransparentPixelCount']}, "
                f"min={alpha['AlphaMin']}"
            )
            lines.append(
                "| "
                + " | ".join(
                    [
                        md_cell(item["Priority"]),
                        f"`{md_cell(item['VisualID'])}`",
                        md_cell(item["DisplayName"]),
                        md_cell(item["ExpectedSize"]),
                        md_cell(item["ActualSize"]),
                        md_cell(alpha_text),
                        md_cell(item["ApprovedPath"]),
                        md_cell(item["Reason"]),
                    ]
                )
                + " |"
            )
    else:
        lines.append("- 当前没有需要优先技术修复的素材。")

    lines.extend(["", "## Visual V2 Replacement", ""])
    replace_items = [item for item in payload["Items"] if item["Action"] == "visual_v2_replace"]
    if replace_items:
        lines.extend(
            [
                "| Priority | VisualID | Domain | Type | Name | Current | Program can use | Prompt | Approved | Reason |",
                "|---|---|---|---|---|---|---|---|---|---|",
            ]
        )
        for item in replace_items:
            lines.append(
                "| "
                + " | ".join(
                    [
                        md_cell(item["Priority"]),
                        f"`{md_cell(item['VisualID'])}`",
                        md_cell(item["Domain"]),
                        md_cell(item["AssetType"]),
                        md_cell(item["DisplayName"]),
                        md_cell(item["CurrentQuality"]),
                        md_cell(item["ProgramCanUseCurrent"]),
                        "ready" if item["PromptReady"] else "missing",
                        md_cell(item["ApprovedPath"]),
                        md_cell(item["Reason"]),
                    ]
                )
                + " |"
            )
    else:
        lines.append("- 当前没有 local_v0 / placeholder 质量替换项。")

    spec_items = [item for item in payload["Items"] if item["Action"] == "spec_review"]
    lines.extend(["", "## Spec Review", ""])
    if spec_items:
        lines.extend(
            [
                "| Priority | VisualID | Expected | Actual | Approved | Reason |",
                "|---|---|---|---|---|---|",
            ]
        )
        for item in spec_items:
            lines.append(
                "| "
                + " | ".join(
                    [
                        md_cell(item["Priority"]),
                        f"`{md_cell(item['VisualID'])}`",
                        md_cell(item["ExpectedSize"]),
                        md_cell(item["ActualSize"]),
                        md_cell(item["ApprovedPath"]),
                        md_cell(item["Reason"]),
                    ]
                )
                + " |"
            )
    else:
        lines.append("- 当前没有尺寸规格复核项。")

    lines.extend(
        [
            "",
            "## Action Meanings",
            "",
            "- `technical_fix`: 当前 Approved 文件存在技术风险，例如应不透明却含透明像素；修复优先级高于美术精修。",
            "- `visual_v2_replace`: 当前图可用于程序接入和验收，但视觉质量只是 local_v0 / placeholder，后续用同名 VisualID 替换正式版。",
            "- `spec_review`: 当前图尺寸与 Manifest SourceSpec 不一致，需要确认是素材错误还是 Spec 需要调整。",
            "",
            "## Program Contract",
            "",
            "- `ProgramCanUseCurrent=yes` 的条目不阻塞程序接入；程序继续按 `可接入素材清单.md` 的 `program_integrate` 处理。",
            "- Visual V2 替换不得改变 `VisualID`、Approved 目标路径、DisplaySpec 或 UI 绑定。",
            "- 完成替换后重新运行 `Generate-ArtIntegrationCandidates.ps1` 和本脚本，保留 snapshot。",
        ]
    )
    return "\n".join(lines) + "\n"


def build_payload(args: argparse.Namespace) -> dict[str, Any]:
    manifest_path = resolve_project_path(args.manifest_path)
    manifest = read_json(manifest_path, {})
    entries = [entry for entry in as_list(manifest.get("Entries")) if isinstance(entry, dict)]
    items = [item for entry in entries if (item := build_item(entry)) is not None]
    items.sort(key=sort_key)

    action_counts = Counter(item["Action"] for item in items)
    domain_counts = Counter(item["Domain"] for item in items)
    priority_counts = Counter(item["Priority"] for item in items)
    return {
        "GeneratedAt": timestamp_text(),
        "SnapshotTag": str(args.snapshot_tag or ""),
        "Inputs": {
            "ManifestPath": repo_path(manifest_path),
        },
        "Summary": {
            "TotalManifestEntries": len(entries),
            "TotalBacklogItems": len(items),
            "ActionCounts": dict(sorted(action_counts.items())),
            "DomainCounts": dict(sorted(domain_counts.items())),
            "PriorityCounts": dict(sorted(priority_counts.items())),
        },
        "Items": items,
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generate Visual V2 art quality backlog.")
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--output-json", default=DEFAULT_OUTPUT_JSON)
    parser.add_argument("--output-markdown", default=DEFAULT_OUTPUT_MARKDOWN)
    parser.add_argument("--snapshot-dir", default=DEFAULT_SNAPSHOT_DIR)
    parser.add_argument("--snapshot-tag", default="")
    parser.add_argument("--snapshot", action="store_true")
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
    markdown = make_markdown(payload)

    output_json = resolve_project_path(args.output_json)
    output_markdown = resolve_project_path(args.output_markdown)
    write_json(output_json, payload)
    output_markdown.parent.mkdir(parents=True, exist_ok=True)
    output_markdown.write_text(markdown, encoding="utf-8")

    snapshot_paths: tuple[Path, Path] | None = None
    if args.snapshot:
        snapshot_paths = write_snapshot(args, payload, markdown)

    counts = payload["Summary"]["ActionCounts"]
    print(f"[OK] Art quality backlog: {repo_path(output_markdown)}")
    if snapshot_paths is not None:
        _, snapshot_markdown = snapshot_paths
        print(f"[OK] Art quality snapshot: {repo_path(snapshot_markdown)}")
    print(
        "[OK] "
        f"technical_fix={counts.get('technical_fix', 0)}, "
        f"visual_v2_replace={counts.get('visual_v2_replace', 0)}, "
        f"spec_review={counts.get('spec_review', 0)}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
