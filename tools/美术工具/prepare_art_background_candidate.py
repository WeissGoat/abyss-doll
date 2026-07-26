# -*- coding: utf-8 -*-
"""Prepare one explicit background-processing candidate in run-scoped staging."""

from __future__ import annotations

import argparse
import hashlib
import json
from datetime import datetime
from pathlib import Path
from typing import Any

from PIL import Image, UnidentifiedImageError

from art_background import measure_candidate, process_background


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]
METHODS = {"alpha_passthrough", "connected_border", "explicit_mask"}
FORBIDDEN_STAGING_PARTS = {"approved", "processed", "selected"}


def resolve_project_path(value: str | Path) -> Path:
    path = Path(value)
    return path if path.is_absolute() else PROJECT_ROOT / path


def _validate_staging(staging_dir: Path) -> None:
    if any(part.lower() in FORBIDDEN_STAGING_PARTS for part in staging_dir.parts):
        raise ValueError(f"background_staging_forbidden:{staging_dir}")
    if staging_dir.exists():
        if not staging_dir.is_dir():
            raise ValueError(f"background_staging_not_directory:{staging_dir}")
        if any(staging_dir.iterdir()):
            raise ValueError(f"background_staging_not_empty:{staging_dir}")


def _load_image(path: Path, *, label: str) -> tuple[Image.Image, bytes, str]:
    if not path.is_file():
        raise ValueError(f"background_{label}_missing:{path}")
    data = path.read_bytes()
    try:
        with Image.open(path) as image:
            image.load()
            loaded = image.copy()
    except (OSError, UnidentifiedImageError, ValueError) as exc:
        raise ValueError(f"background_{label}_invalid:{path}") from exc
    return loaded, data, hashlib.sha256(data).hexdigest()


def _apply_explicit_mask(image: Image.Image, mask: Image.Image) -> Image.Image:
    alpha = mask.convert("L")
    if alpha.size != image.size:
        raise ValueError("background_mask_size_mismatch")
    if alpha.getextrema()[0] == alpha.getextrema()[1]:
        raise ValueError("background_mask_constant")
    output = image.convert("RGBA")
    output.putalpha(alpha)
    return output


def _write_json_atomic(path: Path, payload: dict[str, Any]) -> None:
    temporary = path.with_name(f".{path.name}.tmp")
    temporary.write_text(json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")
    temporary.replace(path)


def prepare_background_candidate(
    *,
    input_path: Path,
    staging_dir: Path,
    method: str,
    expected_input_sha256: str,
    mask_path: Path | None = None,
    threshold: int = 34,
    dry_run: bool = False,
) -> dict[str, Any]:
    input_path = input_path.resolve()
    staging_dir = staging_dir.resolve()
    mask_path = mask_path.resolve() if mask_path is not None else None
    if method not in METHODS:
        raise ValueError(f"background_method_unsupported:{method}")
    _validate_staging(staging_dir)

    source, source_bytes, source_hash = _load_image(input_path, label="input")
    if source_hash != expected_input_sha256.strip().lower():
        raise ValueError(f"background_input_hash_mismatch:{input_path}")

    mask_evidence: dict[str, Any] = {}
    if method == "alpha_passthrough":
        result = process_background(source, "already_transparent", threshold=threshold)
        if result.state != "passed" or result.image is None:
            raise ValueError(f"background_processing_failed:{','.join(result.reasons)}")
        output = result.image
    elif method == "connected_border":
        result = process_background(source, "auto_simple", threshold=threshold)
        if result.state != "passed" or result.image is None:
            raise ValueError(f"background_processing_failed:{','.join(result.reasons)}")
        output = result.image
    else:
        if mask_path is None:
            raise ValueError("background_mask_required")
        mask, mask_bytes, mask_hash = _load_image(mask_path, label="mask")
        output = _apply_explicit_mask(source, mask)
        mask_evidence = {
            "Path": mask_path.as_posix(),
            "SHA256": mask_hash,
            "Width": mask.width,
            "Height": mask.height,
            "Mode": mask.mode,
            "ByteLength": len(mask_bytes),
        }

    preview_metrics = measure_candidate(output)
    evidence: dict[str, Any] = {
        "State": "planned" if dry_run else "prepared",
        "DryRun": dry_run,
        "Method": method,
        "Threshold": threshold,
        "Input": {
            "Path": input_path.as_posix(),
            "SHA256": source_hash,
            "Width": source.width,
            "Height": source.height,
            "Mode": source.mode,
            "ByteLength": len(source_bytes),
        },
        "Mask": mask_evidence,
        "Output": {
            "Path": "candidate.png",
            **preview_metrics,
        },
        "CreatedAt": datetime.now().astimezone().isoformat(timespec="seconds"),
    }
    if dry_run:
        return evidence

    staging_dir.mkdir(parents=True, exist_ok=True)
    temporary_candidate = staging_dir / ".candidate.png.tmp"
    candidate_path = staging_dir / "candidate.png"
    try:
        output.save(temporary_candidate, format="PNG")
        with Image.open(temporary_candidate) as decoded:
            decoded.load()
        temporary_candidate.replace(candidate_path)
        evidence["Output"] = {
            "Path": candidate_path.name,
            **measure_candidate(output, candidate_path),
        }
        _write_json_atomic(staging_dir / "background-processing.json", evidence)
    except Exception:
        if temporary_candidate.exists():
            temporary_candidate.unlink()
        if candidate_path.exists() and not (staging_dir / "background-processing.json").exists():
            candidate_path.unlink()
        raise
    return evidence


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Prepare a guarded art background candidate in staging.")
    parser.add_argument("--input", required=True)
    parser.add_argument("--staging-dir", required=True)
    parser.add_argument("--method", choices=sorted(METHODS), required=True)
    parser.add_argument("--expected-input-sha256", required=True)
    parser.add_argument("--mask", default="")
    parser.add_argument("--threshold", type=int, default=34)
    parser.add_argument("--dry-run", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    result = prepare_background_candidate(
        input_path=resolve_project_path(args.input),
        staging_dir=resolve_project_path(args.staging_dir),
        method=args.method,
        expected_input_sha256=args.expected_input_sha256,
        mask_path=resolve_project_path(args.mask) if args.mask else None,
        threshold=args.threshold,
        dry_run=args.dry_run,
    )
    print(
        f"[OK] method={result['Method']} state={result['State']} dry_run={result['DryRun']} "
        f"input_sha256={result['Input']['SHA256']} output_sha256={result['Output']['SHA256']}"
    )
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
