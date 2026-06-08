# -*- coding: utf-8 -*-
"""Force opaque alpha for Approved PNGs listed as technical_fix.

This tool is intentionally narrow: it only edits PNG pixel alpha for entries whose
quality backlog says SourceSpec does not require transparency. RGB channels,
image size, file path, and Unity .meta files are left untouched.
"""

from __future__ import annotations

import argparse
import json
from pathlib import Path
from typing import Any

from PIL import Image


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]

DEFAULT_BACKLOG = "美术文档/_generated/素材质量替换清单.json"


def resolve_project_path(value: str) -> Path:
    path = Path(value)
    return path if path.is_absolute() else PROJECT_ROOT / path


def read_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def force_opaque(path: Path, dry_run: bool) -> dict[str, Any]:
    with Image.open(path) as image:
        rgba = image.convert("RGBA")
        alpha = rgba.getchannel("A")
        histogram = alpha.histogram()
        transparent = sum(histogram[:1])
        semi = sum(histogram[1:255])
        before_min = next((index for index, count in enumerate(histogram) if count), 255)
        before_max = next((index for index in range(255, -1, -1) if histogram[index]), 255)

        if transparent == 0 and semi == 0 and before_min == 255 and before_max == 255:
            return {
                "changed": False,
                "transparent": transparent,
                "semi": semi,
                "alpha_min": before_min,
                "alpha_max": before_max,
            }

        if not dry_run:
            opaque = Image.new("L", rgba.size, 255)
            rgba.putalpha(opaque)
            rgba.save(path)

    return {
        "changed": True,
        "transparent": transparent,
        "semi": semi,
        "alpha_min": before_min,
        "alpha_max": before_max,
    }


def main() -> int:
    parser = argparse.ArgumentParser(description="Fix opaque PNG alpha from art quality backlog.")
    parser.add_argument("--backlog", default=DEFAULT_BACKLOG)
    parser.add_argument("--visual-id", action="append", default=[])
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()

    backlog_path = resolve_project_path(args.backlog)
    payload = read_json(backlog_path)
    selected = set(args.visual_id)
    items = []
    for item in payload.get("Items", []):
        if item.get("Action") != "technical_fix":
            continue
        if item.get("AlphaRequired") is not False:
            continue
        visual_id = str(item.get("VisualID", ""))
        if selected and visual_id not in selected:
            continue
        items.append(item)

    changed = 0
    skipped = 0
    for item in items:
        visual_id = str(item.get("VisualID", ""))
        approved_path = resolve_project_path(str(item.get("ApprovedPath", "")))
        if not approved_path.exists():
            print(f"MISSING {visual_id}: {approved_path}")
            skipped += 1
            continue
        result = force_opaque(approved_path, args.dry_run)
        if result["changed"]:
            changed += 1
            action = "DRY" if args.dry_run else "FIXED"
        else:
            skipped += 1
            action = "SKIP"
        print(
            f"{action} {visual_id}: transparent={result['transparent']} "
            f"semi={result['semi']} min={result['alpha_min']} max={result['alpha_max']}"
        )

    print(f"summary: scanned={len(items)} changed={changed} skipped={skipped} dry_run={args.dry_run}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
