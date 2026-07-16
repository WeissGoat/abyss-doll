"""Smoke-test configured AI image backends with one tiny real generation."""

from __future__ import annotations

import argparse
import asyncio
import json
import os
import sys
from datetime import datetime
from pathlib import Path
from typing import Any

from loguru import logger


PROJECT_ROOT = Path(__file__).resolve().parents[2]
GATEWAY_ROOT = PROJECT_ROOT / "tools" / "ai-image-gateway"
if str(GATEWAY_ROOT) not in sys.path:
    sys.path.insert(0, str(GATEWAY_ROOT))

from ai_image_gateway import GenerateRequest, ImageService  # noqa: E402
from ai_image_gateway.config import load_config  # noqa: E402


DEFAULT_BACKENDS: dict[str, dict[str, Any]] = {
    "chatgpt": {
        "provider": "openai_images",
        "prompt": "A tiny clean Project P3 backend connectivity test icon: one blue crystal dot on a plain white background. No text.",
        "negative_prompt": "",
        "extra": {"quality": "low"},
    },
    "gemini": {
        "provider": "gemini_chat_image",
        "prompt": "Generate a tiny clean backend connectivity test image: one blue crystal dot on a plain white background. No text.",
        "negative_prompt": "",
        "extra": {},
    },
    "novelai": {
        "provider": "novelai",
        "prompt": "simple blue crystal dot, white background, tiny icon, clean, no text",
        "negative_prompt": "text, watermark, logo, complex background, character, person",
        "extra": {
            "steps": 16,
            "cfg": 4.5,
            "sampler": "k_euler",
            "scheduler": "native",
            "model": "nai-diffusion-4-5-full",
        },
    },
}


IMAGE_SUFFIXES = (".png", ".jpg", ".jpeg", ".webp")


def _image_extension(image_bytes: bytes) -> str:
    if image_bytes.startswith(b"\x89PNG\r\n\x1a\n"):
        return ".png"
    if image_bytes.startswith(b"\xff\xd8\xff"):
        return ".jpg"
    if image_bytes.startswith(b"RIFF") and image_bytes[8:12] == b"WEBP":
        return ".webp"
    return ".png"


def _default_output_dir() -> Path:
    temp_root = Path(os.environ.get("TEMP") or os.environ.get("TMP") or PROJECT_ROOT / "Temp")
    stamp = datetime.now().strftime("%Y%m%d_%H%M%S")
    return temp_root / "P3BackendSmoke" / stamp


def _redacted_auth_status(auth: dict[str, Any]) -> dict[str, str]:
    return {
        str(key): "SET" if value not in (None, "", "${OPENAI_API_KEY}", "${AI_IMAGE_PROXY_KEY}") else "MISSING"
        for key, value in auth.items()
    }


def _provider_config_summary(config_path: Path, backend_ids: list[str]) -> dict[str, Any]:
    config = load_config(config_path)
    providers: dict[str, Any] = {}
    for backend_id in backend_ids:
        spec = DEFAULT_BACKENDS[backend_id]
        provider_name = spec["provider"]
        provider_config = config.providers.get(provider_name)
        if provider_config is None:
            providers[backend_id] = {
                "provider": provider_name,
                "configured": False,
                "enabled": False,
                "auth": {},
                "model": "",
            }
            continue
        providers[backend_id] = {
            "provider": provider_name,
            "configured": True,
            "enabled": bool(provider_config.enabled),
            "auth": _redacted_auth_status(provider_config.auth),
            "model": str(provider_config.settings.get("model", "")),
            "base_url": str(provider_config.settings.get("base_url", "")),
            "endpoint": str(provider_config.settings.get("endpoint", "")),
        }
    return {
        "config": str(config_path),
        "default_provider": config.default_provider.model_dump(),
        "providers": providers,
    }


async def _run_attempt(
    service: ImageService,
    *,
    backend_id: str,
    spec: dict[str, Any],
    attempt: int,
    output_dir: Path,
    width: int,
    height: int,
    seed: int,
) -> dict[str, Any]:
    started = datetime.now()
    request = GenerateRequest(
        provider=spec["provider"],
        prompt=spec["prompt"],
        negative_prompt=spec.get("negative_prompt", ""),
        width=width,
        height=height,
        count=1,
        seed=seed,
        extra=spec.get("extra", {}),
    )
    batch = await service.generate(request)
    elapsed_s = round((datetime.now() - started).total_seconds(), 2)
    record: dict[str, Any] = {
        "attempt": attempt,
        "elapsed_s": elapsed_s,
        "success_count": batch.success_count,
        "errors": batch.errors,
    }
    if not batch.results:
        record["status"] = "fail"
        record["error"] = "No image result returned"
        return record

    result = batch.results[0]
    ext = _image_extension(result.image_bytes)
    image_path = output_dir / f"{backend_id}_attempt{attempt}{ext}"
    meta_path = output_dir / f"{backend_id}_attempt{attempt}.json"
    image_path.write_bytes(result.image_bytes)
    meta_path.write_text(
        json.dumps(
            {
                "backend_id": backend_id,
                "provider": result.provider_name,
                "model": result.model_name,
                "seed": result.seed,
                "generation_params": result.generation_params,
                "cost": result.cost,
                "bytes": len(result.image_bytes),
            },
            ensure_ascii=False,
            indent=2,
        ),
        encoding="utf-8",
    )
    record.update(
        {
            "status": "ok",
            "provider_name": result.provider_name,
            "model_name": result.model_name,
            "seed": result.seed,
            "bytes": len(result.image_bytes),
            "image_path": str(image_path),
            "meta_path": str(meta_path),
        }
    )
    return record


async def _run_backend(
    service: ImageService,
    *,
    backend_id: str,
    spec: dict[str, Any],
    attempts: int,
    retry_delay_seconds: float,
    output_dir: Path,
    width: int,
    height: int,
    seed: int,
) -> dict[str, Any]:
    attempts_log: list[dict[str, Any]] = []
    for attempt in range(1, attempts + 1):
        print(f"[backend-smoke] {backend_id}: attempt {attempt}/{attempts}", flush=True)
        record = await _run_attempt(
            service,
            backend_id=backend_id,
            spec=spec,
            attempt=attempt,
            output_dir=output_dir,
            width=width,
            height=height,
            seed=seed + attempt,
        )
        attempts_log.append(record)
        if record["status"] == "ok":
            break
        if attempt < attempts and retry_delay_seconds > 0:
            await asyncio.sleep(retry_delay_seconds)
    final_status = "ok" if any(item["status"] == "ok" for item in attempts_log) else "fail"
    return {
        "label": backend_id,
        "provider": spec["provider"],
        "status": final_status,
        "attempts": attempts_log,
    }


def _print_summary(records: list[dict[str, Any]], output_dir: Path) -> None:
    print("")
    print("[backend-smoke] summary")
    for record in records:
        ok_attempt = next((item for item in record["attempts"] if item["status"] == "ok"), None)
        if ok_attempt:
            print(
                f"  OK   {record['label']} ({record['provider']}): "
                f"{ok_attempt.get('model_name', '')} bytes={ok_attempt.get('bytes', 0)}"
            )
        else:
            last = record["attempts"][-1] if record["attempts"] else {}
            error = last.get("error") or "; ".join(last.get("errors", [])) or "unknown"
            print(f"  FAIL {record['label']} ({record['provider']}): {error}")
    print(f"[backend-smoke] output: {output_dir}")
    print(f"[backend-smoke] provider log: {output_dir / 'provider.log'}")


async def main() -> int:
    parser = argparse.ArgumentParser(description="Smoke-test configured Project P3 AI image backends.")
    parser.add_argument("--config", default=str(GATEWAY_ROOT / "config.local.yaml"))
    parser.add_argument("--backend", choices=sorted(DEFAULT_BACKENDS.keys()), nargs="*", default=None)
    parser.add_argument("--attempts", type=int, default=2)
    parser.add_argument("--retry-delay-seconds", type=float, default=5.0)
    parser.add_argument("--width", type=int, default=512)
    parser.add_argument("--height", type=int, default=512)
    parser.add_argument("--seed", type=int, default=12345)
    parser.add_argument("--output-dir", default=None)
    parser.add_argument(
        "--check-config-only",
        action="store_true",
        help="Resolve config and print redacted provider status without making API calls.",
    )
    args = parser.parse_args()

    if args.attempts < 1:
        raise SystemExit("--attempts must be >= 1")

    config_path = Path(args.config)
    backend_ids = args.backend or list(DEFAULT_BACKENDS.keys())
    output_dir = Path(args.output_dir) if args.output_dir else _default_output_dir()
    output_dir.mkdir(parents=True, exist_ok=True)
    logger.add(output_dir / "provider.log", encoding="utf-8", level="INFO")

    config_summary = _provider_config_summary(config_path, backend_ids)
    (output_dir / "config_summary.json").write_text(
        json.dumps(config_summary, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )

    if args.check_config_only:
        print(json.dumps(config_summary, ensure_ascii=False, indent=2))
        print(f"[backend-smoke] config summary: {output_dir / 'config_summary.json'}")
        return 0

    records: list[dict[str, Any]] = []
    async with ImageService(config_path) as service:
        for backend_id in backend_ids:
            records.append(
                await _run_backend(
                    service,
                    backend_id=backend_id,
                    spec=DEFAULT_BACKENDS[backend_id],
                    attempts=args.attempts,
                    retry_delay_seconds=args.retry_delay_seconds,
                    output_dir=output_dir,
                    width=args.width,
                    height=args.height,
                    seed=args.seed,
                )
            )

    summary = {
        "output_dir": str(output_dir),
        "config_summary": str(output_dir / "config_summary.json"),
        "provider_log": str(output_dir / "provider.log"),
        "width": args.width,
        "height": args.height,
        "attempts": args.attempts,
        "retry_delay_seconds": args.retry_delay_seconds,
        "records": records,
    }
    (output_dir / "summary.json").write_text(
        json.dumps(summary, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    _print_summary(records, output_dir)
    return 0 if all(record["status"] == "ok" for record in records) else 1


if __name__ == "__main__":
    raise SystemExit(asyncio.run(main()))
