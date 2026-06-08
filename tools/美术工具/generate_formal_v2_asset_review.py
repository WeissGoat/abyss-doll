# -*- coding: utf-8 -*-
"""Generate a pre-integration review package for Formal V2 Approved assets."""

from __future__ import annotations

import argparse
import json
import math
import re
import shutil
from collections import Counter, defaultdict
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from PIL import Image, ImageDraw, ImageFont


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]

DEFAULT_OUTPUT_DIR = ""
DEFAULT_SNAPSHOT_DIR = ""

PRIORITY_ORDER = {"P0": 0, "P1": 1, "P2": 2, "P3": 3, "P4": 4}
FORMAL_TIERS = {"formal_ai_v2", "final", "production"}


def timestamp_text() -> str:
    return datetime.now(timezone.utc).astimezone().isoformat(timespec="seconds")


def timestamp_filename() -> str:
    return datetime.now(timezone.utc).astimezone().strftime("%Y%m%d_%H%M%S")


def safe_snapshot_tag(value: str) -> str:
    text = re.sub(r"[^A-Za-z0-9_.-]+", "_", value.strip())
    text = re.sub(r"_+", "_", text).strip("._-")
    return text[:80]


def resolve_project_path(value: str | None) -> Path | None:
    if not value:
        return None
    path = Path(value)
    return path if path.is_absolute() else PROJECT_ROOT / path


def repo_path(path: Path | None) -> str:
    if path is None:
        return ""
    try:
        return path.resolve().relative_to(PROJECT_ROOT.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def read_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def find_art_generated_dir() -> Path:
    for path in PROJECT_ROOT.iterdir():
        generated = path / "_generated"
        if generated.is_dir() and (generated / "art_manifest.json").exists():
            return generated
    raise FileNotFoundError("Cannot find art _generated directory with art_manifest.json.")


def find_json_with_key(generated_dir: Path, key: str) -> Path:
    for path in sorted(generated_dir.glob("*.json")):
        try:
            data = read_json(path)
        except Exception:
            continue
        if isinstance(data, dict) and key in data:
            return path
    raise FileNotFoundError(f"Cannot find JSON containing key: {key}")


def default_handoff_path() -> Path:
    return find_json_with_key(find_art_generated_dir(), "ProgramIntegrateVisuals")


def default_manifest_path() -> Path:
    return find_art_generated_dir() / "art_manifest.json"


def default_output_dir() -> Path:
    return find_art_generated_dir() / "formal_v2_asset_review"


def default_snapshot_dir() -> Path:
    return find_art_generated_dir() / "formal_v2_asset_review_snapshots"


def as_list(value: Any) -> list[Any]:
    return value if isinstance(value, list) else []


def source_spec(entry: dict[str, Any]) -> dict[str, Any]:
    spec = entry.get("Spec")
    if not isinstance(spec, dict):
        return {}
    nested = spec.get("SourceSpec")
    return nested if isinstance(nested, dict) else spec


def manifest_entries_by_visual_id(manifest: dict[str, Any]) -> dict[str, list[dict[str, Any]]]:
    grouped: dict[str, list[dict[str, Any]]] = defaultdict(list)
    for entry in as_list(manifest.get("Entries")):
        if isinstance(entry, dict) and entry.get("VisualID"):
            grouped[str(entry["VisualID"])].append(entry)
    return grouped


def image_facts(path: Path | None) -> dict[str, Any]:
    if path is None or not path.exists():
        return {
            "Exists": False,
            "Width": 0,
            "Height": 0,
            "Mode": "",
            "HasAlpha": False,
            "AlphaMin": 255,
            "AlphaMax": 255,
            "TransparentPixelCount": 0,
            "SemiTransparentPixelCount": 0,
        }
    with Image.open(path) as image:
        mode = image.mode
        width, height = image.size
        has_alpha = mode in {"RGBA", "LA"} or (mode == "P" and "transparency" in image.info)
        alpha_min = 255
        alpha_max = 255
        transparent = 0
        semi = 0
        if has_alpha:
            alpha = image.convert("RGBA").getchannel("A")
            hist = alpha.histogram()
            alpha_min = next((index for index, value in enumerate(hist) if value), 255)
            alpha_max = next((index for index in range(255, -1, -1) if hist[index]), 255)
            transparent = hist[0]
            semi = sum(hist[1:255])
        return {
            "Exists": True,
            "Width": width,
            "Height": height,
            "Mode": mode,
            "HasAlpha": has_alpha,
            "AlphaMin": alpha_min,
            "AlphaMax": alpha_max,
            "TransparentPixelCount": transparent,
            "SemiTransparentPixelCount": semi,
        }


def expected_spec(entries: list[dict[str, Any]]) -> dict[str, Any]:
    for entry in entries:
        spec = source_spec(entry)
        if spec:
            return {
                "Format": str(spec.get("Format", "") or ""),
                "Width": int(spec.get("Width") or 0),
                "Height": int(spec.get("Height") or 0),
                "AlphaRequired": bool(spec.get("AlphaRequired", False)),
                "Background": str(spec.get("Background", "") or ""),
            }
    return {"Format": "", "Width": 0, "Height": 0, "AlphaRequired": False, "Background": ""}


def review_item(item: dict[str, Any], manifest_entries: dict[str, list[dict[str, Any]]]) -> dict[str, Any]:
    visual_id = str(item.get("VisualID", "") or "")
    approved_path = resolve_project_path(str(item.get("ApprovedPath", "") or ""))
    meta_path = Path(str(approved_path) + ".meta") if approved_path else None
    entries = manifest_entries.get(visual_id, [])
    spec = expected_spec(entries)
    facts = image_facts(approved_path)
    quality_tiers = [str(value) for value in as_list(item.get("QualityTiers"))]
    issues: list[str] = []
    warnings: list[str] = []

    if not facts["Exists"]:
        issues.append("approved_png_missing")
    if not meta_path or not meta_path.exists():
        issues.append("unity_meta_missing")
    if not any(tier in FORMAL_TIERS for tier in quality_tiers):
        issues.append("quality_tier_not_formal")
    if spec["Format"] and approved_path and approved_path.suffix.lower().lstrip(".") != spec["Format"].lower():
        issues.append("format_mismatch")
    if spec["Width"] and facts["Width"] and facts["Width"] != spec["Width"]:
        issues.append("width_mismatch")
    if spec["Height"] and facts["Height"] and facts["Height"] != spec["Height"]:
        issues.append("height_mismatch")
    if not spec["AlphaRequired"] and (facts["TransparentPixelCount"] or facts["SemiTransparentPixelCount"]):
        issues.append("unexpected_transparency")
    if spec["AlphaRequired"] and not facts["HasAlpha"]:
        warnings.append("alpha_expected_but_not_present")
    if item.get("RegistryHasSprite"):
        warnings.append("already_registered_in_latest_registry")

    if issues:
        status = "fail"
    elif warnings:
        status = "warn"
    else:
        status = "pass"

    return {
        "VisualID": visual_id,
        "Priority": str(item.get("Priority", "") or ""),
        "Domain": str(item.get("Domain", "") or ""),
        "AssetType": str(item.get("AssetType", "") or ""),
        "DisplayName": str(item.get("DisplayName", "") or ""),
        "ApprovedPath": repo_path(approved_path),
        "MetaPath": repo_path(meta_path),
        "MetaFileExists": bool(meta_path and meta_path.exists()),
        "QualityTiers": quality_tiers,
        "ManifestStatuses": as_list(item.get("ManifestStatuses")),
        "RegistryHasSprite": bool(item.get("RegistryHasSprite")),
        "ExpectedSpec": spec,
        "ImageFacts": facts,
        "ReferencedScreens": as_list(item.get("ReferencedScreens")),
        "OtherReferences": as_list(item.get("OtherReferences")),
        "ReviewStatus": status,
        "Issues": issues,
        "Warnings": warnings,
    }


def sort_key(item: dict[str, Any]) -> tuple[int, str, str, str]:
    return (
        PRIORITY_ORDER.get(str(item.get("Priority", "")), 99),
        str(item.get("Domain", "")),
        str(item.get("AssetType", "")),
        str(item.get("VisualID", "")),
    )


def fit_image(image: Image.Image, size: tuple[int, int]) -> Image.Image:
    thumb = image.convert("RGBA")
    thumb.thumbnail(size, Image.Resampling.LANCZOS)
    canvas = Image.new("RGBA", size, (246, 244, 238, 255))
    x = (size[0] - thumb.width) // 2
    y = (size[1] - thumb.height) // 2
    canvas.alpha_composite(thumb, (x, y))
    return canvas.convert("RGB")


def draw_wrapped(draw: ImageDraw.ImageDraw, xy: tuple[int, int], text: str, font: ImageFont.ImageFont, width: int) -> None:
    words = re.split(r"([_\-/])", text)
    lines: list[str] = []
    current = ""
    for word in words:
        test = current + word
        bbox = draw.textbbox((0, 0), test, font=font)
        if bbox[2] - bbox[0] <= width or not current:
            current = test
        else:
            lines.append(current)
            current = word
    if current:
        lines.append(current)
    x, y = xy
    for line in lines[:3]:
        draw.text((x, y), line, fill=(34, 32, 29), font=font)
        y += 13


def make_contact_sheet(items: list[dict[str, Any]], output_path: Path, title: str) -> None:
    if not items:
        return
    cell_w, cell_h = 220, 250
    image_box = (184, 168)
    cols = min(5, max(1, math.ceil(math.sqrt(len(items)))))
    rows = math.ceil(len(items) / cols)
    header_h = 54
    sheet = Image.new("RGB", (cols * cell_w, rows * cell_h + header_h), (232, 227, 216))
    draw = ImageDraw.Draw(sheet)
    font = ImageFont.load_default()
    draw.rectangle((0, 0, sheet.width, header_h), fill=(47, 44, 38))
    draw.text((16, 18), title, fill=(248, 245, 236), font=font)
    for index, item in enumerate(items):
        col = index % cols
        row = index // cols
        x = col * cell_w
        y = header_h + row * cell_h
        draw.rectangle((x + 8, y + 8, x + cell_w - 8, y + cell_h - 8), fill=(248, 246, 240), outline=(190, 181, 164))
        path = resolve_project_path(item.get("ApprovedPath"))
        if path and path.exists():
            with Image.open(path) as image:
                thumb = fit_image(image, image_box)
            sheet.paste(thumb, (x + 18, y + 18))
        else:
            draw.rectangle((x + 18, y + 18, x + 18 + image_box[0], y + 18 + image_box[1]), fill=(80, 42, 42))
            draw.text((x + 28, y + 88), "missing", fill=(255, 230, 220), font=font)
        status = str(item.get("ReviewStatus", ""))
        color = {"pass": (57, 120, 72), "warn": (178, 119, 30), "fail": (170, 50, 46)}.get(status, (80, 80, 80))
        draw.rectangle((x + 18, y + 188, x + 72, y + 207), fill=color)
        draw.text((x + 24, y + 193), status, fill=(255, 255, 255), font=font)
        draw.text((x + 82, y + 192), f"{item.get('Domain','')}/{item.get('AssetType','')}", fill=(70, 65, 58), font=font)
        draw_wrapped(draw, (x + 18, y + 215), str(item.get("VisualID", "")), font, cell_w - 36)
    output_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(output_path)


def md_cell(value: Any) -> str:
    if isinstance(value, list):
        text = ", ".join(str(item) for item in value) if value else "-"
    elif isinstance(value, bool):
        text = "yes" if value else "no"
    else:
        text = "" if value is None else str(value)
    return text.replace("|", "\\|").replace("\n", " ")


def build_payload(args: argparse.Namespace) -> tuple[dict[str, Any], str]:
    handoff_path = resolve_project_path(args.handoff_path) or default_handoff_path()
    manifest_path = resolve_project_path(args.manifest_path) or default_manifest_path()
    output_dir = resolve_project_path(args.output_dir) or default_output_dir()

    handoff = read_json(handoff_path)
    manifest = read_json(manifest_path)
    manifest_entries = manifest_entries_by_visual_id(manifest)
    items = [
        review_item(item, manifest_entries)
        for item in as_list(handoff.get("ProgramIntegrateVisuals"))
        if isinstance(item, dict)
    ]
    items.sort(key=sort_key)

    status_counts = Counter(item["ReviewStatus"] for item in items)
    domain_counts = Counter(item["Domain"] for item in items)
    asset_type_counts = Counter(item["AssetType"] for item in items)
    issue_counts = Counter(issue for item in items for issue in item["Issues"])
    warning_counts = Counter(warning for item in items for warning in item["Warnings"])

    contact_dir = output_dir / "contact_sheets"
    contact_sheets: dict[str, str] = {}
    make_contact_sheet(items, contact_dir / "all.png", "FormalV2 pending program integration - all")
    contact_sheets["all"] = repo_path(contact_dir / "all.png")
    for domain in sorted(domain_counts):
        domain_items = [item for item in items if item["Domain"] == domain]
        sheet_path = contact_dir / f"{domain}.png"
        make_contact_sheet(domain_items, sheet_path, f"FormalV2 pending program integration - {domain}")
        contact_sheets[domain] = repo_path(sheet_path)

    payload = {
        "GeneratedAt": timestamp_text(),
        "SnapshotTag": str(args.snapshot_tag or ""),
        "Inputs": {
            "HandoffPath": repo_path(handoff_path),
            "ManifestPath": repo_path(manifest_path),
        },
        "Outputs": {
            "OutputDir": repo_path(output_dir),
            "ContactSheets": contact_sheets,
        },
        "Summary": {
            "ReviewedVisualCount": len(items),
            "ReviewStatusCounts": dict(sorted(status_counts.items())),
            "DomainCounts": dict(sorted(domain_counts.items())),
            "AssetTypeCounts": dict(sorted(asset_type_counts.items())),
            "IssueCounts": dict(sorted(issue_counts.items())),
            "WarningCounts": dict(sorted(warning_counts.items())),
            "AllPass": len(items) > 0 and status_counts.get("fail", 0) == 0 and status_counts.get("warn", 0) == 0,
            "ProgramIntegrateVisualCountFromHandoff": handoff.get("Summary", {}).get("ProgramIntegrateVisualCount", 0),
        },
        "Items": items,
    }
    return payload, make_markdown(payload)


def make_markdown(payload: dict[str, Any]) -> str:
    summary = payload["Summary"]
    lines = [
        "# Formal V2 Approved 资产预验收清单",
        "",
        f"生成时间：`{payload['GeneratedAt']}`",
        "",
        "## 摘要",
        "",
        f"- 本次预验收 VisualID：`{summary['ReviewedVisualCount']}`",
        f"- 状态统计：`{summary['ReviewStatusCounts']}`",
        f"- 领域统计：`{summary['DomainCounts']}`",
        f"- 类型统计：`{summary['AssetTypeCounts']}`",
        f"- 问题统计：`{summary['IssueCounts']}`",
        f"- 警告统计：`{summary['WarningCounts']}`",
        "",
        "## Contact Sheets",
        "",
    ]
    for key, path in payload["Outputs"]["ContactSheets"].items():
        lines.append(f"- `{key}`: `{path}`")
    lines.extend([
        "",
        "## 结论口径",
        "",
        "- `pass`：Approved PNG、Unity `.meta`、formal quality tier、尺寸和 alpha 均符合 Manifest SourceSpec。",
        "- `warn`：可以继续交接，但存在需要运行时截图复核的小风险。",
        "- `fail`：不建议交给程序接入，需先由美术侧修复。",
        "",
        "## 明细",
        "",
        "| Status | Priority | VisualID | Domain | Type | Size | Expected | Alpha | Issues | Warnings | Path |",
        "|---|---|---|---|---|---|---|---|---|---|---|",
    ])
    for item in payload["Items"]:
        facts = item["ImageFacts"]
        spec = item["ExpectedSpec"]
        size = f"{facts['Width']}x{facts['Height']}"
        expected = f"{spec['Width']}x{spec['Height']}" if spec["Width"] and spec["Height"] else "-"
        alpha = f"{facts['AlphaMin']}-{facts['AlphaMax']}"
        lines.append(
            "| "
            + " | ".join(
                [
                    md_cell(item["ReviewStatus"]),
                    md_cell(item["Priority"]),
                    f"`{md_cell(item['VisualID'])}`",
                    md_cell(item["Domain"]),
                    md_cell(item["AssetType"]),
                    md_cell(size),
                    md_cell(expected),
                    md_cell(alpha),
                    md_cell(item["Issues"]),
                    md_cell(item["Warnings"]),
                    md_cell(item["ApprovedPath"]),
                ]
            )
            + " |"
        )
    lines.extend([
        "",
        "## Source Files",
        "",
        f"- Program handoff: `{payload['Inputs']['HandoffPath']}`",
        f"- Manifest: `{payload['Inputs']['ManifestPath']}`",
    ])
    return "\n".join(lines) + "\n"


def write_outputs(args: argparse.Namespace, payload: dict[str, Any], markdown: str) -> tuple[Path, Path]:
    output_dir = resolve_project_path(args.output_dir) or default_output_dir()
    json_path = output_dir / "formal_v2_asset_review.json"
    md_path = output_dir / "formal_v2_asset_review.md"
    write_json(json_path, payload)
    md_path.parent.mkdir(parents=True, exist_ok=True)
    md_path.write_text(markdown, encoding="utf-8")
    return json_path, md_path


def write_snapshot(args: argparse.Namespace, output_json: Path, output_md: Path, payload: dict[str, Any]) -> Path:
    snapshot_dir = resolve_project_path(args.snapshot_dir) or default_snapshot_dir()
    tag = safe_snapshot_tag(args.snapshot_tag)
    stem = timestamp_filename()
    if tag:
        stem = f"{stem}_{tag}"
    target = snapshot_dir / stem
    target.mkdir(parents=True, exist_ok=True)
    shutil.copy2(output_json, target / output_json.name)
    shutil.copy2(output_md, target / output_md.name)
    output_dir = output_json.parent
    contact_dir = output_dir / "contact_sheets"
    if contact_dir.exists():
        shutil.copytree(contact_dir, target / "contact_sheets", dirs_exist_ok=True)
    return target


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generate Formal V2 Approved asset pre-review package.")
    parser.add_argument("--handoff-path", default="")
    parser.add_argument("--manifest-path", default="")
    parser.add_argument("--output-dir", default=DEFAULT_OUTPUT_DIR)
    parser.add_argument("--snapshot-dir", default=DEFAULT_SNAPSHOT_DIR)
    parser.add_argument("--snapshot-tag", default="")
    parser.add_argument("--snapshot", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    payload, markdown = build_payload(args)
    output_json, output_md = write_outputs(args, payload, markdown)
    snapshot_path: Path | None = None
    if args.snapshot:
        snapshot_path = write_snapshot(args, output_json, output_md, payload)
    summary = payload["Summary"]
    print(f"[OK] Formal V2 asset review: {repo_path(output_md)}")
    print(f"[OK] reviewed={summary['ReviewedVisualCount']}, status={summary['ReviewStatusCounts']}")
    if snapshot_path:
        print(f"[OK] snapshot: {repo_path(snapshot_path)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
