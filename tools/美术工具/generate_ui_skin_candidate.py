# -*- coding: utf-8 -*-
"""Generate engineering-safe UI Skin candidates without touching Approved."""

from __future__ import annotations

import argparse
import hashlib
import json
import math
import re
import uuid
from datetime import datetime
from pathlib import Path
from typing import Any

from PIL import Image, ImageDraw

from art_workspace import normalize_entry_workspace_paths, workspace_path


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]
DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_INCOMING_ROOT = "UnityClient/Assets/Art/_IncomingAI"
SUPPORTED_CAPABILITIES = {"deterministic_template"}


def read_json(path: Path) -> dict[str, Any]:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, payload: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_name(f".{path.name}.{uuid.uuid4().hex}.tmp")
    temporary.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    temporary.replace(path)


def resolve_path(value: str, project_root: Path) -> Path:
    path = Path(value)
    return path if path.is_absolute() else project_root / path


def repo_path(path: Path, project_root: Path) -> str:
    try:
        return path.resolve().relative_to(project_root.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def parse_csv(values: list[str]) -> set[str]:
    return {part.strip() for value in values for part in value.split(",") if part.strip()}


def source_spec(entry: dict[str, Any]) -> dict[str, Any]:
    spec = entry.get("Spec") if isinstance(entry.get("Spec"), dict) else {}
    nested = spec.get("SourceSpec") if isinstance(spec.get("SourceSpec"), dict) else spec
    return nested


def composition_spec(entry: dict[str, Any]) -> dict[str, Any]:
    spec = entry.get("Spec") if isinstance(entry.get("Spec"), dict) else {}
    return spec.get("CompositionSpec") if isinstance(spec.get("CompositionSpec"), dict) else {}


def process_spec(entry: dict[str, Any]) -> dict[str, Any]:
    spec = entry.get("Spec") if isinstance(entry.get("Spec"), dict) else {}
    return spec.get("ProcessSpec") if isinstance(spec.get("ProcessSpec"), dict) else {}


def nine_slice_contract(entry: dict[str, Any]) -> dict[str, Any]:
    nine_slice = process_spec(entry).get("NineSlice")
    if not isinstance(nine_slice, dict) or not bool(nine_slice.get("Enabled")):
        raise ValueError(f"{entry.get('VisualID', '<missing>')} requires ProcessSpec.NineSlice.Enabled=true")
    border = nine_slice.get("Border")
    if not isinstance(border, dict):
        raise ValueError(f"{entry.get('VisualID', '<missing>')} NineSlice Border is missing")
    for side in ("Left", "Right", "Top", "Bottom"):
        try:
            value = int(border.get(side, 0))
        except (TypeError, ValueError) as exc:
            raise ValueError(f"{entry.get('VisualID', '<missing>')} NineSlice Border.{side} is invalid") from exc
        if value <= 0:
            raise ValueError(f"{entry.get('VisualID', '<missing>')} NineSlice Border.{side} must be positive")
    return nine_slice


def accent_family(visual_id: str, variant: int) -> tuple[tuple[int, int, int, int], tuple[int, int, int, int]]:
    value = visual_id.lower()
    if any(token in value for token in ("danger", "defeat", "_hp")):
        base, light = (207, 76, 92, 255), (255, 142, 132, 255)
    elif "victory" in value:
        base, light = (67, 181, 145, 255), (146, 246, 192, 255)
    elif "shield" in value:
        base, light = (58, 139, 217, 255), (126, 220, 255, 255)
    elif any(token in value for token in ("primary", "selected")):
        base, light = (54, 166, 202, 255), (137, 238, 242, 255)
    else:
        base, light = (87, 128, 174, 255), (157, 204, 235, 255)
    if variant % 2:
        base = (min(255, base[0] + 24), max(0, base[1] - 16), min(255, base[2] + 20), 255)
        light = (min(255, light[0] + 18), max(0, light[1] - 12), min(255, light[2] + 8), 255)
    return base, light


def scaled_box(box: tuple[int, int, int, int], scale: int) -> tuple[int, int, int, int]:
    return (box[0] * scale, box[1] * scale, box[2] * scale, box[3] * scale)


def render_deterministic_template(entry: dict[str, Any], variant: int) -> Image.Image:
    src = source_spec(entry)
    comp = composition_spec(entry)
    nine_slice = nine_slice_contract(entry)
    width, height = int(src.get("Width", 0) or 0), int(src.get("Height", 0) or 0)
    if width <= 0 or height <= 0:
        raise ValueError(f"{entry.get('VisualID', '<missing>')} SourceSpec dimensions are invalid")
    if str(src.get("Format", "png")).lower() != "png" or not bool(src.get("AlphaRequired", False)):
        raise ValueError(f"{entry.get('VisualID', '<missing>')} UI Skin requires transparent PNG SourceSpec")

    safe = float(comp.get("SafePaddingPercent", 0) or 0)
    # Leave room for the high-resolution downsample kernel so alpha cannot bleed outside the contract safe area.
    margin_x = max(6, math.ceil(width * safe / 100) + 4)
    margin_y = max(6, math.ceil(height * safe / 100) + 4)
    outer = (margin_x, margin_y, width - margin_x - 1, height - margin_y - 1)
    border = nine_slice["Border"]
    minimum_border = min(int(border[side]) for side in ("Left", "Right", "Top", "Bottom"))
    stroke = max(2, min(width, height) // 50)
    inset = max(stroke * 3, minimum_border // 5)
    inner = (outer[0] + inset, outer[1] + inset, outer[2] - inset, outer[3] - inset)
    radius = max(stroke * 2, min((outer[3] - outer[1]) // 3, minimum_border // 2))
    inner_radius = max(stroke, radius - inset // 2)
    scale = 2
    canvas = Image.new("RGBA", (width * scale, height * scale), (0, 0, 0, 0))
    draw = ImageDraw.Draw(canvas, "RGBA")
    base, light = accent_family(str(entry["VisualID"]), variant)
    asset_type = str(entry.get("AssetType", "") or "")

    draw.rounded_rectangle(
        scaled_box(outer, scale), radius=radius * scale, fill=(8, 16, 29, 246),
        outline=base, width=stroke * 3 * scale,
    )
    draw.rounded_rectangle(
        scaled_box(inner, scale), radius=inner_radius * scale,
        fill=(13 + variant * 2, 24, 39 + variant * 4, 238), outline=light, width=stroke * scale,
    )
    highlight_y = outer[1] + max(stroke * 2, inset // 2)
    draw.line(
        [(inner[0] * scale, highlight_y * scale), (inner[2] * scale, highlight_y * scale)],
        fill=(*light[:3], 125), width=stroke * scale,
    )
    if asset_type in {"panel", "frame"}:
        cap = max(stroke * 3, minimum_border // 4)
        for x, y in (
            (outer[0], outer[1]), (outer[2] - cap, outer[1]),
            (outer[0], outer[3] - cap), (outer[2] - cap, outer[3] - cap),
        ):
            draw.rounded_rectangle(
                scaled_box((x, y, x + cap, y + cap), scale),
                radius=stroke * scale, fill=(*light[:3], 210),
            )
    elif asset_type == "bar":
        groove_y = (inner[1] + inner[3]) // 2
        draw.line(
            [(inner[0] * scale, groove_y * scale), (inner[2] * scale, groove_y * scale)],
            fill=(*base[:3], 170), width=max(stroke, (inner[3] - inner[1]) // 4) * scale,
        )
    result = canvas.resize((width, height), Image.Resampling.LANCZOS)
    alpha = result.getchannel("A").point(lambda value: 0 if value < 16 else value)
    result.putalpha(alpha)
    return result


def next_output_paths(raw_dir: Path, count: int) -> list[Path]:
    date_prefix = datetime.now().strftime("%Y%m%d")
    pattern = re.compile(rf"^{date_prefix}_(\d{{3}})\.png$", re.IGNORECASE)
    existing = [int(match.group(1)) for path in raw_dir.glob(f"{date_prefix}_*.png") if (match := pattern.match(path.name))]
    start = max(existing, default=0) + 1
    return [raw_dir / f"{date_prefix}_{index:03d}.png" for index in range(start, start + count)]


def append_note(entry: dict[str, Any], message: str) -> None:
    existing = str(entry.get("Notes", "") or "").strip()
    entry["Notes"] = f"{existing}\n{message}".strip() if existing else message


def generate_ui_skin_candidates(
    *, project_root: Path, manifest_path: Path, incoming_root: Path,
    visual_ids: set[str], batch_id: str, variants: int, capability: str, dry_run: bool,
) -> dict[str, Any]:
    if capability not in SUPPORTED_CAPABILITIES:
        raise ValueError(f"Unsupported UI Skin capability: {capability}")
    if variants < 1 or variants > 8:
        raise ValueError("variants must be between 1 and 8")
    manifest = read_json(manifest_path)
    entries = [normalize_entry_workspace_paths(entry) for entry in manifest.get("Entries", []) if isinstance(entry, dict)]
    manifest["Entries"] = entries
    selected = [entry for entry in entries if not visual_ids or str(entry.get("VisualID", "")) in visual_ids]
    if visual_ids:
        found = {str(entry.get("VisualID", "")) for entry in selected}
        missing = sorted(visual_ids.difference(found))
        if missing:
            raise ValueError(f"Manifest UI Skin entries missing: {', '.join(missing)}")
    for entry in selected:
        nine_slice_contract(entry)
    print(f"[PLAN] ui_skin capability={capability} selected={len(selected)} variants={variants} batch={batch_id}")
    for entry in selected:
        src = source_spec(entry)
        print(f"[ITEM] {entry['VisualID']} {src.get('Width')}x{src.get('Height')} type={entry.get('AssetType', '')}")
    if dry_run:
        return {"Planned": len(selected), "Generated": 0, "BatchID": batch_id, "Capability": capability}

    created_at = datetime.now().astimezone().isoformat(timespec="seconds")
    for entry in selected:
        workspace = workspace_path(incoming_root, entry)
        raw_dir = workspace / "raw"
        raw_dir.mkdir(parents=True, exist_ok=True)
        outputs: list[dict[str, Any]] = []
        for index, output_path in enumerate(next_output_paths(raw_dir, variants)):
            image = render_deterministic_template(entry, index)
            image.save(output_path, format="PNG")
            outputs.append({
                "File": f"raw/{output_path.name}", "RepoPath": repo_path(output_path, project_root),
                "Variant": index + 1, "Width": image.width, "Height": image.height,
                "SHA256": hashlib.sha256(output_path.read_bytes()).hexdigest(),
            })
        write_json(workspace / "generation.json", {
            "VisualID": entry["VisualID"], "BatchID": batch_id, "Capability": capability,
            "Provider": "", "Model": "", "Requested": {"Count": variants, "Width": outputs[0]["Width"], "Height": outputs[0]["Height"]},
            "Outputs": outputs, "Errors": [], "CreatedAt": created_at,
            "MethodEvidence": {"NineSlice": process_spec(entry).get("NineSlice"), "AssetType": entry.get("AssetType", ""), "NoBakedText": True},
        })
        write_json(workspace / "manifest_snapshot.json", entry)
        entry["CandidateBatchID"] = batch_id
        entry["CandidateRawFiles"] = [output["RepoPath"] for output in outputs]
        entry["RawPath"] = repo_path(raw_dir, project_root)
        append_note(entry, f"[{created_at}] generated UI Skin candidate batch {batch_id} with capability={capability}; preserved Status={entry.get('Status', '')}.")
        print(f"[OK] {entry['VisualID']} raw={len(outputs)} path={repo_path(raw_dir, project_root)}")
    write_json(manifest_path, manifest)
    return {"Planned": len(selected), "Generated": len(selected), "BatchID": batch_id, "Capability": capability}


def main() -> int:
    parser = argparse.ArgumentParser(description="Generate P3 NineSlice UI Skin candidates.")
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--incoming-root", default=DEFAULT_INCOMING_ROOT)
    parser.add_argument("--visual-id", action="append", default=[])
    parser.add_argument("--batch-id", required=True)
    parser.add_argument("--variants", type=int, default=2)
    parser.add_argument("--capability", default="deterministic_template")
    parser.add_argument("--dry-run", action="store_true")
    args = parser.parse_args()
    result = generate_ui_skin_candidates(
        project_root=PROJECT_ROOT,
        manifest_path=resolve_path(args.manifest_path, PROJECT_ROOT),
        incoming_root=resolve_path(args.incoming_root, PROJECT_ROOT),
        visual_ids=parse_csv(args.visual_id), batch_id=args.batch_id, variants=args.variants,
        capability=args.capability, dry_run=args.dry_run,
    )
    print(json.dumps(result, ensure_ascii=False, indent=2))
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
