"""Item icons against their bag footprint.

Item icons are drawn on square canvases, and the bag fits each icon into its footprint keeping its aspect, so on a
square canvas every multi-cell item looked one cell big. This crops the icons of multi-cell items to what is drawn
(plus a margin), so a long blade spans its cells, and reports the items whose drawing still covers little of its
footprint (drawn on the diagonal, or the wrong way round): those need the art redrawn to the footprint's shape.

  python tools/美术工具/item_icon_footprint.py            report only
  python tools/美术工具/item_icon_footprint.py --apply    crop in place (keeps the .meta and its GUID)
"""
import glob
import json
import os
import sys
from pathlib import Path

from PIL import Image

REPO = Path(__file__).resolve().parents[2]
ITEMS = REPO / "配置表(JSON)" / "Items"
APPROVED = REPO / "UnityClient" / "Assets" / "Art" / "Approved"
MARGIN = 0.04          # of the drawn content's longer side
ALPHA = 40             # alpha above this counts as drawn
POOR_SPAN = 0.6        # reaching less than this far along the footprint needs the art redrawn


def footprint(item):
    shape = item.get("Grid", {}).get("Shape") or [[0, 0]]
    return len({c[0] for c in shape}), len({c[1] for c in shape})


def span(content_w, content_h, cols, rows):
    # The icon fitted into the cols x rows footprint keeping its aspect: how far it reaches along the footprint's
    # long side (on a square footprint, along its shorter reach), as a share of that side.
    scale = min(cols / content_w, rows / content_h)
    across, down = content_w * scale / cols, content_h * scale / rows
    if cols == rows:
        return min(across, down)
    return down if rows > cols else across


def main():
    apply = "--apply" in sys.argv
    icons = {Path(p).stem: Path(p) for p in glob.glob(str(APPROVED / "**" / "*.png"), recursive=True)}
    cropped, poor = [], []
    for path in sorted(ITEMS.glob("*.json")):
        item = json.loads(path.read_text(encoding="utf-8-sig"))
        cols, rows = footprint(item)
        if cols * rows == 1:
            continue
        icon = icons.get(item.get("IconID", ""))
        if icon is None:
            continue
        image = Image.open(icon).convert("RGBA")
        box = image.getchannel("A").point(lambda a: 255 if a > ALPHA else 0).getbbox()
        if box is None:
            continue
        w, h = box[2] - box[0], box[3] - box[1]
        margin = round(max(w, h) * MARGIN)
        box = (max(0, box[0] - margin), max(0, box[1] - margin), min(image.width, box[2] + margin), min(image.height, box[3] + margin))
        before = span(image.width, image.height, cols, rows)
        after = span(box[2] - box[0], box[3] - box[1], cols, rows)
        name = f"{item['ConfigID']} {item.get('Name', '')} {cols}x{rows}"
        if after < POOR_SPAN:
            poor.append(f"{name}: reaches {after:.0%} along its cells (drawn {w}x{h})")
        if box != (0, 0, image.width, image.height) and after > before + 0.05:
            cropped.append(f"{name}: {before:.0%} -> {after:.0%}")
            if apply:
                Image.open(icon).crop(box).save(icon)

    print(("Cropped" if apply else "Would crop") + f" {len(cropped)} icon(s):")
    print("\n".join("  " + line for line in cropped))
    print(f"Needs the art redrawn to its footprint ({len(poor)}):")
    print("\n".join("  " + line for line in poor))


if __name__ == "__main__":
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")
    main()
