# -*- coding: utf-8 -*-
"""Post-process generated art assets into processed candidates and contact sheets."""

from __future__ import annotations

import argparse
import hashlib
import json
import math
from datetime import datetime
from pathlib import Path
from typing import Any

from PIL import Image, ImageDraw, ImageFont

from art_background import background_policy, process_background, review_candidate
from art_processing import numeric_round_directories, next_round_number, publish_round, reserve_round
from art_workspace import normalize_entry_workspace_paths, validate_workspace_file, workspace_path


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
    return json.loads(path.read_text(encoding="utf-8-sig"))


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


def source_spec(spec: Any) -> dict[str, Any]:
    if not isinstance(spec, dict):
        return {}
    nested = spec.get("SourceSpec")
    if isinstance(nested, dict):
        return nested
    return spec


def composition_spec(spec: Any) -> dict[str, Any]:
    if not isinstance(spec, dict):
        return {}
    nested = spec.get("CompositionSpec")
    if isinstance(nested, dict):
        return nested
    return spec


def process_spec(spec: Any) -> dict[str, Any]:
    if not isinstance(spec, dict):
        return {}
    nested = spec.get("ProcessSpec")
    if isinstance(nested, dict):
        return nested
    return spec


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
        if args.candidate_batch_id and entry.get("CandidateBatchID") != args.candidate_batch_id:
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


def ensure_workspace(in_root: Path, entry: dict[str, Any]) -> dict[str, Path]:
    base = workspace_path(in_root, entry)
    paths = {
        "base": base,
        "raw": base / "raw",
        "processed": base / "processed",
        "contact_sheet": base / "contact_sheet",
    }
    for key, path in paths.items():
        if key == "processed":
            continue
        path.mkdir(parents=True, exist_ok=True)
    return paths


def list_raw_images(raw_dir: Path) -> list[Path]:
    if not raw_dir.exists():
        return []
    return sorted(path for path in raw_dir.iterdir() if path.is_file() and path.suffix.lower() in IMAGE_EXTENSIONS)


def candidate_raw_images(entry: dict[str, Any], raw_dir: Path, args: argparse.Namespace) -> tuple[list[Path], list[str]]:
    if not args.candidate_batch_id:
        return list_raw_images(raw_dir), []

    raw_files = entry.get("CandidateRawFiles")
    if not isinstance(raw_files, list):
        return [], [f"CandidateBatchID={args.candidate_batch_id} has no CandidateRawFiles"]

    images: list[Path] = []
    warnings: list[str] = []
    for value in raw_files:
        if not isinstance(value, str) or not value.strip():
            continue
        path = validate_workspace_file(resolve_project_path(value, value), raw_dir)
        if not path.exists():
            warnings.append(f"candidate raw file missing: {value}")
            continue
        if path.suffix.lower() not in IMAGE_EXTENSIONS:
            warnings.append(f"candidate raw file has unsupported extension: {value}")
            continue
        images.append(path)
    return sorted(images), warnings


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


def process_spec_background_policy(spec: Any) -> str:
    return background_policy(spec)


def round_candidate_name(index: int) -> str:
    return f"{index:03d}.png"


def process_image(image_or_path: Image.Image | Path, spec: dict[str, Any]) -> Image.Image:
    image = Image.open(image_or_path) if isinstance(image_or_path, Path) else image_or_path.copy()
    src = source_spec(spec)
    comp = composition_spec(spec)
    proc = process_spec(spec)
    target_width = int(src.get("Width", image.width))
    target_height = int(src.get("Height", image.height))
    post_process = proc.get("PostProcess", [])
    if not isinstance(post_process, list):
        post_process = []

    if "crop_16_9" in post_process:
        image = crop_to_aspect(image, 16 / 9)

    if image.mode not in ("RGB", "RGBA"):
        image = image.convert("RGB")

    if "trim_transparent_edges" in post_process and image.mode == "RGBA":
        image = trim_transparent(image)

    if "fit_safe_padding" in post_process:
        return fit_safe_padding(image, target_width, target_height, float(comp.get("SafePaddingPercent", 0)))

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
    workspace = ensure_workspace(in_root, entry)
    raw_images, warnings = candidate_raw_images(entry, workspace["raw"], args)
    if not raw_images:
        warnings.append("no raw images found")
    policy = process_spec_background_policy(entry["Spec"])
    if policy == "agent_required":
        report = {
            "VisualID": visual_id,
            "CandidateBatchID": str(args.candidate_batch_id or ""),
            "ProcessedPath": repo_path(workspace["processed"]),
            "ContactSheet": "",
            "Inputs": [repo_path(path) for path in raw_images],
            "Outputs": [],
            "Warnings": warnings,
            "LatestRound": None,
            "LatestState": "decision_required",
            "Reason": "agent_processing_required",
            "Policy": policy,
        }
        write_json(workspace["base"] / "process_report.json", report)
        return report

    if not raw_images:
        report = {
            "VisualID": visual_id,
            "CandidateBatchID": str(args.candidate_batch_id or ""),
            "ProcessedPath": repo_path(workspace["processed"]),
            "ContactSheet": "",
            "Inputs": [],
            "Outputs": [],
            "Warnings": warnings,
            "LatestRound": None,
            "LatestState": "failed",
            "Reason": "no_raw_images",
            "Policy": policy,
        }
        write_json(workspace["base"] / "process_report.json", report)
        return report

    reservation = reserve_round(workspace["processed"])
    output_records: list[dict[str, Any]] = []
    candidate_records: list[dict[str, Any]] = []
    technical_reviews: list[dict[str, Any]] = []
    round_blockers: list[str] = []

    try:
        for index, raw_path in enumerate(raw_images, start=1):
            with Image.open(raw_path) as source_image:
                background_result = process_background(
                    source_image,
                    policy,
                    threshold=args.background_threshold,
                )
                if background_result.state != "passed" or background_result.image is None:
                    round_blockers.extend(background_result.reasons or [background_result.state])
                    output_records.append(
                        {
                            "Input": repo_path(raw_path),
                            "Output": "",
                            "Skipped": True,
                            "State": background_result.state,
                            "Reasons": background_result.reasons,
                        }
                    )
                    continue

                processed = process_image(background_result.image, entry["Spec"])
            output_path = reservation.temp_dir / round_candidate_name(index)
            processed.save(output_path, format="PNG")
            review = review_candidate(
                processed,
                source_spec=source_spec(entry["Spec"]),
                composition_spec=composition_spec(entry["Spec"]),
                production_profile=str(entry.get("ProductionProfile", "standard_asset")),
                saved_path=output_path,
            )
            technical_reviews.append({"File": output_path.name, "Input": repo_path(raw_path), **review})
            with Image.open(output_path) as saved_image:
                width, height = saved_image.size
                image_format = str(saved_image.format or "png").lower()
            candidate_records.append(
                {
                    "File": output_path.name,
                    "Input": repo_path(raw_path),
                    "Status": review["Status"],
                    "SHA256": hashlib.sha256(output_path.read_bytes()).hexdigest(),
                    "Width": width,
                    "Height": height,
                    "Format": image_format,
                    "Reasons": review["Reasons"],
                }
            )
            output_records.append(
                {
                    "Input": repo_path(raw_path),
                    "Output": repo_path(workspace["processed"] / str(reservation.number) / output_path.name),
                    "Skipped": False,
                    "Status": review["Status"],
                    "Reasons": review["Reasons"],
                }
            )

        passed_count = sum(1 for candidate in candidate_records if candidate["Status"] == "passed")
        round_state = "passed" if passed_count and not round_blockers else "failed"
        decision = {"State": round_state, "Candidates": candidate_records, "Reasons": sorted(set(round_blockers))}
        write_json(reservation.temp_dir / "technical_review.json", {"Candidates": technical_reviews})
        write_json(reservation.temp_dir / "process_report.json", {"VisualID": visual_id, "Policy": policy, "Outputs": output_records})
        write_json(reservation.temp_dir / "decision.json", decision)

        contact_path = reservation.temp_dir / "contact_sheet.png"
        contact_inputs = sorted(path for path in reservation.temp_dir.glob("*.png") if path.name != contact_path.name)
        if contact_inputs:
            make_contact_sheet(visual_id, contact_inputs, contact_path, args.contact_size)
        final_dir = publish_round(reservation)
    except Exception:
        from art_processing import abandon_round

        abandon_round(reservation)
        raise

    published_contact = final_dir / "contact_sheet.png"
    compatibility_contact = workspace["contact_sheet"] / f"{visual_id}_contact_sheet.png"
    if published_contact.exists():
        compatibility_contact.write_bytes(published_contact.read_bytes())

    rounds = numeric_round_directories(workspace["processed"])
    report = {
        "VisualID": visual_id,
        "CandidateBatchID": str(args.candidate_batch_id or ""),
        "ProcessedPath": repo_path(workspace["processed"]),
        "ContactSheet": repo_path(compatibility_contact) if compatibility_contact.exists() else "",
        "Inputs": [repo_path(path) for path in raw_images],
        "Outputs": output_records,
        "Warnings": warnings,
        "LatestRound": reservation.number,
        "LatestState": round_state,
        "Reason": ";".join(sorted(set(round_blockers))),
        "Policy": policy,
        "Rounds": [number for number, _ in rounds],
    }
    write_json(workspace["base"] / "process_report.json", report)
    return report


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Optimize generated Project P3 art assets.")
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--in-root", default=DEFAULT_IN_ROOT)
    parser.add_argument("--status", default="generated")
    parser.add_argument("--domain", action="append", default=[])
    parser.add_argument("--visual-id", action="append", default=[])
    parser.add_argument("--priority", action="append", default=[])
    parser.add_argument("--batch-id", default="")
    parser.add_argument("--candidate-batch-id", default="")
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
    entries = [normalize_entry_workspace_paths(entry) for entry in entries if isinstance(entry, dict)]
    manifest["Entries"] = entries

    selected = select_entries(entries, args)
    print(f"[PLAN] status={args.status} selected={len(selected)} batch={args.batch_id or '<any>'}")
    for entry in selected:
        workspace = workspace_path(in_root, entry)
        raw_count = len(list_raw_images(workspace / "raw"))
        src = source_spec(entry.get("Spec"))
        policy = process_spec_background_policy(entry["Spec"])
        next_round = next_round_number(workspace / "processed")
        print(
            f"[ITEM] {entry['VisualID']} policy={policy} raw={raw_count} "
            f"next_round={next_round} spec={src.get('Width')}x{src.get('Height')}"
        )

    if args.dry_run:
        print("[DONE] dry-run only; no files changed.")
        return 0

    created_at = datetime.now().astimezone().isoformat(timespec="seconds")
    success = 0
    reports = []
    for entry in selected:
        report = optimize_entry(entry, args, in_root)
        reports.append(report)
        report_path = workspace_path(in_root, entry) / "process_report.json"
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
