# -*- coding: utf-8 -*-
"""Post-process generated art assets into processed candidates and contact sheets."""

from __future__ import annotations

import argparse
import json
import math
from datetime import datetime
from pathlib import Path
from typing import Any

from PIL import Image, ImageDraw, ImageFont


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]
DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_IN_ROOT = "UnityClient/Assets/Art/_IncomingAI"
IMAGE_EXTENSIONS = {".png", ".jpg", ".jpeg", ".webp"}


def repo_path(path: Path) -> str:
    try:
        return path.resolve().relative_to(PROJECT_ROOT.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def resolve_project_path(value: str | None, default: str) -> Path:
    raw = value or default
    path = Path(raw)
    return path if path.is_absolute() else PROJECT_ROOT / path


def read_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8"))


def write_json(path: Path, data: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def split_filters(values: list[str]) -> set[str]:
    result: set[str] = set()
    for value in values:
        for part in value.split(","):
            part = part.strip()
            if part:
                result.add(part)
    return result


def select_entries(entries: list[dict[str, Any]], args: argparse.Namespace) -> list[dict[str, Any]]:
    domains = split_filters(args.domain)
    visual_ids = split_filters(args.visual_id)
    priorities = split_filters(args.priority)
    selected: list[dict[str, Any]] = []
    for entry in entries:
        if args.status and entry.get("Status") != args.status:
            continue
        if args.batch_id and entry.get("BatchID") != args.batch_id:
            continue
        if domains and str(entry.get("Domain", "")) not in domains:
            continue
        if visual_ids and str(entry.get("VisualID", "")) not in visual_ids:
            continue
        if priorities and str(entry.get("Priority", "")) not in priorities:
            continue
        if not isinstance(entry.get("Spec"), dict):
            continue
        selected.append(entry)
        if args.limit and len(selected) >= args.limit:
            break
    return selected


def append_note(entry: dict[str, Any], message: str) -> None:
    existing = str(entry.get("Notes", "") or "").strip()
    entry["Notes"] = f"{existing}\n{message}".strip() if existing else message


def ensure_workspace(in_root: Path, visual_id: str) -> dict[str, Path]:
    base = in_root / visual_id
    paths = {
        "base": base,
        "raw": base / "raw",
        "processed": base / "processed",
        "contact_sheet": base / "contact_sheet",
    }
    for path in paths.values():
        path.mkdir(parents=True, exist_ok=True)
    return paths


def list_raw_images(raw_dir: Path) -> list[Path]:
    if not raw_dir.exists():
        return []
    return sorted(path for path in raw_dir.iterdir() if path.is_file() and path.suffix.lower() in IMAGE_EXTENSIONS)


def crop_to_aspect(image: Image.Image, target_ratio: float) -> Image.Image:
    width, height = image.size
    current_ratio = width / height
    if abs(current_ratio - target_ratio) < 0.01:
        return image
    if current_ratio > target_ratio:
        new_width = int(height * target_ratio)
        offset = (width - new_width) // 2
        return image.crop((offset, 0, offset + new_width, height))
    new_height = int(width / target_ratio)
    offset = (height - new_height) // 2
    return image.crop((0, offset, width, offset + new_height))


def trim_transparent(image: Image.Image) -> Image.Image:
    rgba = image.convert("RGBA")
    alpha = rgba.getchannel("A")
    bbox = alpha.getbbox()
    if bbox is None:
        return rgba
    return rgba.crop(bbox)


def fit_safe_padding(image: Image.Image, width: int, height: int, padding_percent: float) -> Image.Image:
    rgba = trim_transparent(image)
    canvas = Image.new("RGBA", (width, height), (0, 0, 0, 0))
    safe_width = max(1, int(width * (100 - 2 * padding_percent) / 100))
    safe_height = max(1, int(height * (100 - 2 * padding_percent) / 100))
    source_width, source_height = rgba.size
    scale = min(safe_width / source_width, safe_height / source_height)
    resized_width = max(1, int(source_width * scale))
    resized_height = max(1, int(source_height * scale))
    resized = rgba.resize((resized_width, resized_height), Image.Resampling.LANCZOS)
    offset = ((width - resized_width) // 2, (height - resized_height) // 2)
    canvas.paste(resized, offset, resized)
    return canvas


def sample_corner_color(image: Image.Image) -> tuple[int, int, int]:
    rgb = image.convert("RGB")
    width, height = rgb.size
    points = [(0, 0), (width - 1, 0), (0, height - 1), (width - 1, height - 1)]
    colors = [rgb.getpixel(point) for point in points]
    return tuple(sorted(channel)[len(channel) // 2] for channel in zip(*colors))


def remove_solid_background(image: Image.Image, threshold: int) -> Image.Image:
    rgba = image.convert("RGBA")
    background = sample_corner_color(rgba)
    pixels = []
    feather = max(8, threshold // 2)
    pixel_source = rgba.get_flattened_data() if hasattr(rgba, "get_flattened_data") else rgba.getdata()
    for red, green, blue, alpha in pixel_source:
        distance = math.sqrt(
            (red - background[0]) ** 2
            + (green - background[1]) ** 2
            + (blue - background[2]) ** 2
        )
        if distance <= threshold:
            pixels.append((red, green, blue, 0))
        elif distance <= threshold + feather:
            factor = (distance - threshold) / feather
            pixels.append((red, green, blue, int(alpha * factor)))
        else:
            pixels.append((red, green, blue, alpha))
    rgba.putdata(pixels)
    return rgba


def process_image(raw_path: Path, spec: dict[str, Any], args: argparse.Namespace) -> Image.Image:
    image = Image.open(raw_path)
    target_width = int(spec.get("Width", image.width))
    target_height = int(spec.get("Height", image.height))
    post_process = spec.get("PostProcess", [])
    if not isinstance(post_process, list):
        post_process = []

    if "crop_16_9" in post_process:
        image = crop_to_aspect(image, 16 / 9)

    alpha_required = bool(spec.get("AlphaRequired", False))
    background = str(spec.get("Background", ""))
    if alpha_required or background == "transparent":
        image = remove_solid_background(image, args.background_threshold)
    elif image.mode not in ("RGB", "RGBA"):
        image = image.convert("RGB")

    if "trim_transparent_edges" in post_process and (alpha_required or image.mode == "RGBA"):
        image = trim_transparent(image)

    if "fit_safe_padding" in post_process:
        return fit_safe_padding(image, target_width, target_height, float(spec.get("SafePaddingPercent", 0)))

    return image.resize((target_width, target_height), Image.Resampling.LANCZOS)


def checkerboard(size: tuple[int, int], cell: int = 16) -> Image.Image:
    width, height = size
    image = Image.new("RGB", size, "#d8d8d8")
    draw = ImageDraw.Draw(image)
    for y in range(0, height, cell):
        for x in range(0, width, cell):
            if (x // cell + y // cell) % 2:
                draw.rectangle((x, y, x + cell - 1, y + cell - 1), fill="#f4f4f4")
    return image


def make_contact_sheet(visual_id: str, images: list[Path], out_path: Path, contact_size: int) -> None:
    if not images:
        return
    columns = min(4, len(images))
    rows = math.ceil(len(images) / columns)
    label_height = 28
    padding = 12
    tile_width = contact_size
    tile_height = contact_size + label_height
    sheet = Image.new(
        "RGB",
        (columns * tile_width + (columns + 1) * padding, rows * tile_height + (rows + 1) * padding + 30),
        "#20232d",
    )
    draw = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("arial.ttf", 14)
        title_font = ImageFont.truetype("arial.ttf", 16)
    except OSError:
        font = ImageFont.load_default()
        title_font = font
    draw.text((padding, 8), visual_id, fill="#f1d27a", font=title_font)

    for index, path in enumerate(images):
        column = index % columns
        row = index // columns
        x = padding + column * (tile_width + padding)
        y = padding + 30 + row * (tile_height + padding)
        thumb = Image.open(path).convert("RGBA")
        thumb.thumbnail((contact_size, contact_size), Image.Resampling.LANCZOS)
        bg = checkerboard((contact_size, contact_size), 12).convert("RGBA")
        offset = ((contact_size - thumb.width) // 2, (contact_size - thumb.height) // 2)
        bg.alpha_composite(thumb, offset)
        sheet.paste(bg.convert("RGB"), (x, y))
        draw.text((x, y + contact_size + 6), path.name, fill="#f0f0f0", font=font)

    out_path.parent.mkdir(parents=True, exist_ok=True)
    sheet.save(out_path, format="PNG")


def optimize_entry(entry: dict[str, Any], args: argparse.Namespace, in_root: Path) -> dict[str, Any]:
    visual_id = entry["VisualID"]
    workspace = ensure_workspace(in_root, visual_id)
    raw_images = list_raw_images(workspace["raw"])
    processed_outputs: list[dict[str, Any]] = []
    warnings: list[str] = []
    if not raw_images:
        warnings.append("no raw images found")

    for raw_path in raw_images:
        out_path = workspace["processed"] / f"{raw_path.stem}.png"
        if out_path.exists() and not args.overwrite:
            processed_outputs.append({"Input": repo_path(raw_path), "Output": repo_path(out_path), "Skipped": True})
            continue
        processed = process_image(raw_path, entry["Spec"], args)
        processed.save(out_path, format="PNG")
        processed_outputs.append({"Input": repo_path(raw_path), "Output": repo_path(out_path), "Skipped": False})

    processed_paths = [Path(item["Output"]) for item in processed_outputs if not item.get("Skipped")]
    actual_processed = sorted(workspace["processed"].glob("*.png"))
    contact_path = workspace["contact_sheet"] / f"{visual_id}_contact_sheet.png"
    if actual_processed:
        make_contact_sheet(visual_id, actual_processed, contact_path, args.contact_size)

    return {
        "VisualID": visual_id,
        "ProcessedPath": repo_path(workspace["processed"]),
        "ContactSheet": repo_path(contact_path) if contact_path.exists() else "",
        "Inputs": [repo_path(path) for path in raw_images],
        "Outputs": processed_outputs,
        "Warnings": warnings,
    }


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Optimize generated Project P3 art assets.")
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--in-root", default=DEFAULT_IN_ROOT)
    parser.add_argument("--status", default="generated")
    parser.add_argument("--domain", action="append", default=[])
    parser.add_argument("--visual-id", action="append", default=[])
    parser.add_argument("--priority", action="append", default=[])
    parser.add_argument("--batch-id", default="")
    parser.add_argument("--limit", type=int, default=0)
    parser.add_argument("--contact-size", type=int, default=160)
    parser.add_argument("--background-threshold", type=int, default=34)
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--overwrite", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    manifest_path = resolve_project_path(args.manifest_path, DEFAULT_MANIFEST)
    in_root = resolve_project_path(args.in_root, DEFAULT_IN_ROOT)
    manifest = read_json(manifest_path)
    entries = manifest.get("Entries", [])
    if not isinstance(entries, list):
        raise ValueError("Manifest Entries must be a list.")

    selected = select_entries(entries, args)
    print(f"[PLAN] status={args.status} selected={len(selected)} batch={args.batch_id or '<any>'}")
    for entry in selected:
        workspace = in_root / entry["VisualID"]
        raw_count = len(list_raw_images(workspace / "raw"))
        print(f"[ITEM] {entry['VisualID']} raw={raw_count} spec={entry['Spec'].get('Width')}x{entry['Spec'].get('Height')}")

    if args.dry_run:
        print("[DONE] dry-run only; no files changed.")
        return 0

    created_at = datetime.now().astimezone().isoformat(timespec="seconds")
    success = 0
    reports = []
    for entry in selected:
        report = optimize_entry(entry, args, in_root)
        reports.append(report)
        report_path = in_root / entry["VisualID"] / "process_report.json"
        write_json(report_path, {"CreatedAt": created_at, **report, "Spec": entry.get("Spec", {})})
        if report["Outputs"]:
            success += 1
            append_note(entry, f"[{created_at}] processed {len(report['Outputs'])} raw images; contact sheet: {report['ContactSheet']}")
        print(
            f"[OK] {entry['VisualID']} processed={len(report['Outputs'])} "
            f"contact={report['ContactSheet'] or '<none>'}"
        )

    write_json(manifest_path, manifest)
    print(f"[DONE] processed_entries={success} reports={len(reports)}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
