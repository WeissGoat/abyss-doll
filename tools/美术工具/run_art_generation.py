# -*- coding: utf-8 -*-
"""Run Project P3 art generation from the visual asset manifest."""

from __future__ import annotations

import argparse
import asyncio
import copy
import io
import json
import re
import sys
from datetime import datetime
from pathlib import Path
from typing import Any

from PIL import Image

from art_prompt_revision import (
    DANBOORU_TAGS_FORMAT,
    NATURAL_LANGUAGE_FORMAT,
    select_prompt_variant,
)
from art_workspace import normalize_entry_workspace_paths, workspace_path


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]
GATEWAY_ROOT = PROJECT_ROOT / "tools" / "ai-image-gateway"
if str(GATEWAY_ROOT) not in sys.path:
    sys.path.insert(0, str(GATEWAY_ROOT))

from ai_image_gateway import GenerateRequest, ImageFormat, ImageService  # noqa: E402


DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
DEFAULT_OUT_ROOT = "UnityClient/Assets/Art/_IncomingAI"
DEFAULT_REQUEST_CATALOG = "美术文档/_generated/art_generation_requests.json"
IMAGE_EXTENSIONS = {".png", ".jpg", ".jpeg", ".webp"}


def repo_path(path: Path) -> str:
    try:
        return path.resolve().relative_to(PROJECT_ROOT.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def resolve_project_path(value: str | None, default: str | None = None) -> Path | None:
    raw = value or default
    if not raw:
        return None
    path = Path(raw)
    if path.is_absolute():
        return path
    return PROJECT_ROOT / path


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


def parse_extra(values: list[str]) -> dict[str, Any]:
    extra: dict[str, Any] = {}
    for value in values:
        if "=" not in value:
            raise ValueError(f"Invalid --extra value, expected key=value: {value}")
        key, raw = value.split("=", 1)
        key = key.strip()
        raw = raw.strip()
        if not key:
            raise ValueError(f"Invalid --extra key: {value}")
        extra[key] = parse_scalar(raw)
    return extra


def parse_scalar(raw: str) -> Any:
    lower = raw.lower()
    if lower == "true":
        return True
    if lower == "false":
        return False
    if lower == "null":
        return None
    try:
        if "." in raw:
            return float(raw)
        return int(raw)
    except ValueError:
        return raw


def source_spec(spec: Any) -> dict[str, Any]:
    if not isinstance(spec, dict):
        return {}
    nested = spec.get("SourceSpec")
    if isinstance(nested, dict):
        return nested
    return spec


def validate_entry(entry: dict[str, Any], *, require_legacy_prompt: bool = False) -> list[str]:
    errors: list[str] = []
    spec = entry.get("Spec")
    src = source_spec(spec)
    required_fields = ["VisualID"]
    if require_legacy_prompt:
        required_fields.extend(("PromptEN", "NegativePromptEN"))
    for field in required_fields:
        if not entry.get(field):
            errors.append(f"missing {field}")
    if not isinstance(spec, dict):
        errors.append("missing Spec object")
    else:
        for field in ("Width", "Height", "Format"):
            if not src.get(field):
                errors.append(f"missing Spec.SourceSpec.{field}")
    return errors


def select_generation_request(
    catalog: dict[str, Any],
    manifest_entry: dict[str, Any],
    *,
    request_id: str = "",
    prompt_revision_id: str = "",
    prompt_format: str = "auto",
    provider: str = "",
) -> tuple[dict[str, Any], dict[str, Any], str, dict[str, Any]]:
    pointer = manifest_entry.get("CompiledRequest")
    if not isinstance(pointer, dict) or not pointer.get("RequestID"):
        raise ValueError(f"compiled_request_missing:{manifest_entry.get('VisualID', '')}")
    selected_id = request_id or str(pointer["RequestID"])
    requests = catalog.get("Requests", []) if isinstance(catalog, dict) else []
    request = next(
        (item for item in requests if isinstance(item, dict) and item.get("RequestID") == selected_id),
        None,
    )
    visual_id = str(manifest_entry.get("VisualID", ""))
    if request is None:
        raise ValueError(f"compiled_request_missing:{visual_id}")
    if request.get("VisualID") != visual_id:
        raise ValueError(f"compiled_request_visual_id_mismatch:{visual_id}")
    if pointer.get("RequirementFingerprint") != request.get("RequirementFingerprint"):
        raise ValueError(f"compiled_request_fingerprint_mismatch:{visual_id}")
    if pointer.get("PromptAuthoringStatus") != request.get("PromptAuthoringStatus"):
        raise ValueError(f"prompt_authoring_status_mismatch:{visual_id}")
    if str(pointer.get("ActivePromptRevisionID", "") or "") != str(request.get("ActivePromptRevisionID", "") or ""):
        raise ValueError(f"prompt_revision_pointer_mismatch:{visual_id}")
    revision, selected_format, variant = select_prompt_variant(
        request,
        prompt_revision_id=prompt_revision_id,
        prompt_format=prompt_format,
        provider=provider,
    )
    return request, revision, selected_format, variant


def _serialize_weighted_tag(item: Any) -> str:
    if not isinstance(item, dict):
        return ""
    tag = str(item.get("Tag", "")).strip()
    if not tag:
        return ""
    weight = float(item.get("Weight", 1.0))
    if weight == 1.0:
        return tag
    return f"{weight:g}::{tag}::"


def serialize_provider_prompt(variant: dict[str, Any], prompt_format: str) -> tuple[str, str]:
    if prompt_format == NATURAL_LANGUAGE_FORMAT:
        return str(variant.get("Positive", "")), str(variant.get("Negative", ""))
    if prompt_format == DANBOORU_TAGS_FORMAT:
        positive = ", ".join(filter(None, (_serialize_weighted_tag(item) for item in variant.get("PositiveTags", []))))
        negative = ", ".join(filter(None, (_serialize_weighted_tag(item) for item in variant.get("NegativeTags", []))))
        return positive, negative
    raise ValueError(f"prompt_format_unsupported:{prompt_format}")


def select_entries(entries: list[dict[str, Any]], args: argparse.Namespace) -> tuple[list[dict[str, Any]], list[str]]:
    domains = split_filters(args.domain)
    visual_ids = split_filters(args.visual_id)
    config_ids = split_filters(args.config_id)
    priorities = split_filters(args.priority)
    selected: list[dict[str, Any]] = []
    skipped: list[str] = []

    for entry in entries:
        visual_id = str(entry.get("VisualID", ""))
        if entry.get("Status") != args.status:
            continue
        if domains and str(entry.get("Domain", "")) not in domains:
            continue
        if visual_ids and visual_id not in visual_ids:
            continue
        if config_ids and str(entry.get("ConfigID", "")) not in config_ids:
            continue
        if priorities and str(entry.get("Priority", "")) not in priorities:
            continue
        errors = validate_entry(entry, require_legacy_prompt=args.allow_legacy_prompt)
        if errors:
            skipped.append(f"{visual_id or '<missing VisualID>'}: {', '.join(errors)}")
            continue
        selected.append(entry)
        if args.limit and len(selected) >= args.limit:
            break

    return selected, skipped


def ensure_workspace(out_root: Path, entry: dict[str, Any]) -> dict[str, Path]:
    visual_id = str(entry["VisualID"])
    base = workspace_path(out_root, entry)
    paths = {
        "base": base,
        "raw": base / "raw",
        "processed": base / "processed",
        "selected": base / "selected",
        "contact_sheet": base / "contact_sheet",
    }
    for path in paths.values():
        path.mkdir(parents=True, exist_ok=True)
    notes = base / "notes.md"
    if not notes.exists():
        notes.write_text(
            f"# {visual_id}\n\nGenerated candidates enter `raw/`; processing outputs are published only as decision-backed numeric `processed/<round>/` directories.\n",
            encoding="utf-8",
        )
    return paths


def next_output_paths(raw_dir: Path, date_prefix: str, ext: str, count: int, overwrite: bool) -> list[Path]:
    if overwrite:
        start = 1
    else:
        pattern = re.compile(rf"^{re.escape(date_prefix)}_(\d{{3}}){re.escape(ext)}$", re.IGNORECASE)
        existing = []
        for path in raw_dir.glob(f"{date_prefix}_*{ext}"):
            match = pattern.match(path.name)
            if match:
                existing.append(int(match.group(1)))
        start = max(existing, default=0) + 1
    return [raw_dir / f"{date_prefix}_{index:03d}{ext}" for index in range(start, start + count)]


def image_size(data: bytes) -> tuple[int | None, int | None]:
    try:
        with Image.open(io.BytesIO(data)) as img:
            return img.size
    except Exception:
        return None, None


def append_note(entry: dict[str, Any], message: str) -> None:
    existing = str(entry.get("Notes", "") or "").strip()
    entry["Notes"] = f"{existing}\n{message}".strip() if existing else message


def make_request(
    entry: dict[str, Any],
    args: argparse.Namespace,
    extra: dict[str, Any],
    seed: int | None,
    *,
    count: int,
    requirement_request: dict[str, Any] | None = None,
    prompt_revision: dict[str, Any] | None = None,
    prompt_variant: dict[str, Any] | None = None,
    prompt_format: str = "legacy_unverified",
) -> GenerateRequest:
    spec = source_spec(entry["Spec"])
    fmt = str(spec.get("Format", "png")).lower()
    try:
        output_format = ImageFormat(fmt)
    except ValueError:
        output_format = ImageFormat.PNG

    prompt = entry.get("PromptEN", "")
    negative_prompt = entry.get("NegativePromptEN", "")
    if requirement_request is not None and prompt_revision is not None and prompt_variant is not None:
        prompt, negative_prompt = serialize_provider_prompt(prompt_variant, prompt_format)
        technical = requirement_request.get("TechnicalRequest", {})
        if isinstance(technical, dict):
            spec = {**spec, **{key: technical.get(key) for key in ("Width", "Height", "Format") if technical.get(key)}}
    request_extra = {
        "domain": entry.get("Domain", ""),
        "visual_id": entry.get("VisualID", ""),
        "asset_type": entry.get("AssetType", ""),
        "prompt_format": prompt_format,
        "request_id": requirement_request.get("RequestID", "") if requirement_request else "",
        "prompt_revision_id": prompt_revision.get("PromptRevisionID", "") if prompt_revision else "",
        **extra,
    }
    return GenerateRequest(
        prompt=prompt,
        negative_prompt=negative_prompt,
        width=int(spec["Width"]),
        height=int(spec["Height"]),
        count=count,
        seed=seed,
        provider=args.provider or None,
        output_format=output_format,
        extra=request_extra,
    )


def build_generation_record(
    *,
    entry: dict[str, Any],
    batch_id: str,
    request_ids: list[str],
    provider: str,
    model: str,
    requested_width: int,
    requested_height: int,
    requested_count: int,
    outputs: list[dict[str, Any]],
    errors: list[str],
    created_at: str,
    requirement_request: dict[str, Any] | None = None,
    prompt_revision: dict[str, Any] | None = None,
    prompt_format: str = "",
    provider_request: dict[str, Any] | None = None,
) -> dict[str, Any]:
    return {
        "VisualID": entry["VisualID"],
        "BatchID": batch_id,
        "RequestID": request_ids[0] if request_ids else "",
        "RequestIDs": request_ids,
        "Provider": provider,
        "Model": model,
        "PromptEN": entry.get("PromptEN", ""),
        "NegativePromptEN": entry.get("NegativePromptEN", ""),
        "RequirementRequestID": requirement_request.get("RequestID", "") if requirement_request else "",
        "RequirementFingerprint": requirement_request.get("RequirementFingerprint", "") if requirement_request else "",
        "PromptRevisionID": prompt_revision.get("PromptRevisionID", "") if prompt_revision else "",
        "PromptRevisionFingerprint": prompt_revision.get("RevisionFingerprint", "") if prompt_revision else "",
        "PromptFormat": prompt_format,
        "RequirementSnapshot": copy.deepcopy(requirement_request) if requirement_request else {},
        "PromptRevisionSnapshot": copy.deepcopy(prompt_revision) if prompt_revision else {},
        "ProviderRequest": provider_request or {},
        "Spec": entry.get("Spec", {}),
        "Requested": {
            "Width": requested_width,
            "Height": requested_height,
            "Count": requested_count,
        },
        "Outputs": outputs,
        "Errors": errors,
        "CreatedAt": created_at,
    }


async def run_generation(args: argparse.Namespace) -> int:
    manifest_path = resolve_project_path(args.manifest_path, DEFAULT_MANIFEST)
    out_root = resolve_project_path(args.out_root, DEFAULT_OUT_ROOT)
    config_path = resolve_project_path(args.config, None)
    if manifest_path is None or out_root is None:
        raise RuntimeError("Manifest path and output root are required.")
    if not manifest_path.exists():
        raise FileNotFoundError(f"Manifest not found: {manifest_path}")
    if config_path is not None and not config_path.exists():
        raise FileNotFoundError(f"Gateway config not found: {config_path}")

    manifest = read_json(manifest_path)
    request_catalog = None
    if args.allow_legacy_prompt:
        if args.request_id or args.prompt_revision_id:
            raise ValueError("legacy_prompt_cannot_select_request_or_revision")
    elif args.request_catalog:
        request_catalog_path = resolve_project_path(args.request_catalog, DEFAULT_REQUEST_CATALOG)
        if request_catalog_path is None or not request_catalog_path.exists():
            raise FileNotFoundError(f"Request catalog not found: {request_catalog_path}")
        request_catalog = read_json(request_catalog_path)
    else:
        raise ValueError("request_catalog_required: use --allow-legacy-prompt only for non-formal recovery")
    entries = manifest.get("Entries", [])
    if not isinstance(entries, list):
        raise ValueError("Manifest Entries must be a list.")
    entries = [normalize_entry_workspace_paths(entry) for entry in entries if isinstance(entry, dict)]
    manifest["Entries"] = entries

    selected, skipped = select_entries(entries, args)
    compiled_by_visual: dict[str, tuple[dict[str, Any], dict[str, Any], str, dict[str, Any]]] = {}
    if request_catalog is not None:
        ready_entries: list[dict[str, Any]] = []
        for entry in selected:
            try:
                compiled_by_visual[entry["VisualID"]] = select_generation_request(
                    request_catalog,
                    entry,
                    request_id=args.request_id,
                    prompt_revision_id=args.prompt_revision_id,
                    prompt_format=args.prompt_format,
                    provider=args.provider,
                )
            except ValueError as exc:
                skipped.append(f"{entry.get('VisualID', '')}: {exc}")
                continue
            ready_entries.append(entry)
        selected = ready_entries
    batch_id = args.batch_id or f"ai_{datetime.now().strftime('%Y%m%d_%H%M%S')}_{args.provider or 'default'}"
    extra = parse_extra(args.extra)

    print(
        f"[PLAN] status={args.status} entries={len(entries)} selected={len(selected)} "
        f"provider={args.provider or 'default'} variants={args.variants} batch={batch_id}"
    )
    for item in skipped:
        print(f"[SKIP] {item}")
    for entry in selected:
        spec = source_spec(entry["Spec"])
        print(
            f"[ITEM] {entry['VisualID']} config={entry.get('ConfigID', '')} domain={entry.get('Domain', '')} "
            f"size={spec['Width']}x{spec['Height']} format={spec.get('Format', 'png')}"
        )
    if args.dry_run:
        print("[DONE] dry-run only; no files changed.")
        return 0
    if not selected:
        print("[DONE] no entries selected.")
        return 0

    success_entries = 0
    failed_entries = 0
    image_count = 0
    created_at = datetime.now().astimezone().isoformat(timespec="seconds")
    date_prefix = datetime.now().strftime("%Y%m%d")
    total_requests = len(selected) * args.variants
    current_request = 0

    if args.concurrency != 1:
        print("[WARN] --concurrency is ignored by this adapter; generation is serialized one image at a time.")

    async with ImageService(str(config_path) if config_path else None) as service:
        for entry_index, entry in enumerate(selected):
            visual_id = entry["VisualID"]
            workspace = ensure_workspace(out_root, entry)
            write_json(workspace["base"] / "manifest_snapshot.json", entry)

            spec = source_spec(entry["Spec"])
            ext = f".{str(spec.get('Format', 'png')).lower()}"
            if ext not in IMAGE_EXTENSIONS:
                ext = ".png"
            output_paths = next_output_paths(workspace["raw"], date_prefix, ext, args.variants, args.overwrite)

            outputs: list[dict[str, Any]] = []
            errors: list[str] = []
            request_ids: list[str] = []
            provider = args.provider or "unknown"
            model = "unknown"
            requested_width = int(spec["Width"])
            requested_height = int(spec["Height"])
            requirement_request, prompt_revision, prompt_format, prompt_variant = compiled_by_visual.get(
                visual_id,
                (None, None, "legacy_unverified", None),
            )
            provider_request_snapshot: dict[str, Any] = {}

            for variant_index in range(args.variants):
                if current_request > 0 and args.delay_seconds > 0:
                    await asyncio.sleep(args.delay_seconds)
                seed = (
                    args.seed + entry_index * args.variants + variant_index
                    if args.seed is not None
                    else None
                )
                request = make_request(
                    entry,
                    args,
                    extra,
                    seed,
                    count=1,
                    requirement_request=requirement_request,
                    prompt_revision=prompt_revision,
                    prompt_variant=prompt_variant,
                    prompt_format=prompt_format,
                )
                provider_request_snapshot = {
                    "Prompt": request.prompt,
                    "NegativePrompt": request.negative_prompt,
                    "Width": request.width,
                    "Height": request.height,
                    "Count": request.count,
                    "Seed": request.seed,
                    "Provider": request.provider,
                    "OutputFormat": request.output_format.value,
                    "Extra": request.extra,
                }
                requested_width = request.width
                requested_height = request.height
                batch = await service.generate(request)
                request_ids.append(batch.request_id)
                current_request += 1
                print(
                    f"[PROGRESS] {current_request}/{total_requests} {visual_id} "
                    f"variant={variant_index + 1}/{args.variants} success={batch.success_count} errors={len(batch.errors)}"
                )
                errors.extend(batch.errors)

                for image_result in batch.results:
                    if len(outputs) >= len(output_paths):
                        break
                    output_path = output_paths[len(outputs)]
                    if output_path.exists() and not args.overwrite:
                        raise FileExistsError(f"Output already exists: {output_path}")
                    output_path.write_bytes(image_result.image_bytes)
                    width, height = image_size(image_result.image_bytes)
                    provider = image_result.provider_name or provider
                    model = image_result.model_name or model
                    outputs.append(
                        {
                            "File": f"raw/{output_path.name}",
                            "RepoPath": repo_path(output_path),
                            "Seed": image_result.seed,
                            "Width": width,
                            "Height": height,
                            "Params": image_result.generation_params,
                        }
                    )

            if not outputs and not errors:
                errors.append("no images returned")

            record = build_generation_record(
                entry=entry,
                batch_id=batch_id,
                request_ids=request_ids,
                provider=provider,
                model=model,
                requested_width=requested_width,
                requested_height=requested_height,
                requested_count=args.variants,
                outputs=outputs,
                errors=errors,
                created_at=created_at,
                requirement_request=requirement_request,
                prompt_revision=prompt_revision,
                prompt_format=prompt_format,
                provider_request=provider_request_snapshot,
            )
            write_json(workspace["base"] / "generation.json", record)

            if outputs:
                if args.preserve_status:
                    entry["CandidateBatchID"] = batch_id
                    entry["CandidateRawFiles"] = [item["RepoPath"] for item in outputs if item.get("RepoPath")]
                    append_note(entry, f"[{created_at}] generated candidate batch {batch_id}; preserved Status={entry.get('Status', '')}.")
                else:
                    entry["Status"] = "generated"
                    entry["BatchID"] = batch_id
                entry["RawPath"] = repo_path(workspace["raw"])
                if errors:
                    append_note(entry, f"[{created_at}] generation partial errors: {'; '.join(errors)}")
                success_entries += 1
                image_count += len(outputs)
                print(f"[OK] {visual_id} raw={len(outputs)} path={entry['RawPath']}")
            else:
                failed_entries += 1
                append_note(entry, f"[{created_at}] generation failed: {'; '.join(errors)}")
                print(f"[WARN] {visual_id} raw=0 errors={errors}")

    write_json(manifest_path, manifest)
    print(f"[DONE] success_entries={success_entries} failed_entries={failed_entries} images={image_count}")
    print(f"[MANIFEST] {repo_path(manifest_path)}")
    return 0


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generate Project P3 art assets through ai-image-gateway.")
    parser.add_argument("--config", default="")
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--out-root", default=DEFAULT_OUT_ROOT)
    parser.add_argument("--request-catalog", default=DEFAULT_REQUEST_CATALOG)
    parser.add_argument("--request-id", default="")
    parser.add_argument("--prompt-revision-id", default="")
    parser.add_argument("--prompt-format", choices=["auto", NATURAL_LANGUAGE_FORMAT, DANBOORU_TAGS_FORMAT], default="auto")
    parser.add_argument("--allow-legacy-prompt", action="store_true")
    parser.add_argument("--provider", default="")
    parser.add_argument("--status", default="prompted")
    parser.add_argument("--domain", action="append", default=[])
    parser.add_argument("--visual-id", action="append", default=[])
    parser.add_argument("--config-id", action="append", default=[])
    parser.add_argument("--priority", action="append", default=[])
    parser.add_argument("--limit", type=int, default=0)
    parser.add_argument("--variants", type=int, default=4)
    parser.add_argument("--seed", type=int, default=None)
    parser.add_argument("--concurrency", type=int, default=1)
    parser.add_argument("--delay-seconds", type=float, default=1.0)
    parser.add_argument("--batch-id", default="")
    parser.add_argument("--extra", action="append", default=[])
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--overwrite", action="store_true")
    parser.add_argument("--preserve-status", action="store_true")
    args = parser.parse_args()
    if args.variants < 1 or args.variants > 16:
        raise ValueError("--variants must be between 1 and 16.")
    if args.concurrency < 1:
        raise ValueError("--concurrency must be at least 1.")
    return args


def main() -> int:
    return asyncio.run(run_generation(parse_args()))


if __name__ == "__main__":
    raise SystemExit(main())
