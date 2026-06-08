# -*- coding: utf-8 -*-
"""Run NovelAI generation for Formal V2 prompt-ready program-integrate assets."""

from __future__ import annotations

import argparse
import json
import os
import subprocess
from pathlib import Path
from typing import Any


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]


def read_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def find_art_generated_dir() -> Path:
    for path in PROJECT_ROOT.iterdir():
        generated = path / "_generated"
        if generated.is_dir() and (generated / "art_manifest.json").exists():
            return generated
    raise FileNotFoundError("Cannot find art _generated directory with art_manifest.json.")


def default_readiness_path() -> Path:
    return find_art_generated_dir() / "formal_v2_prompt_readiness" / "formal_v2_prompt_readiness.json"


def repo_path(path: Path) -> str:
    try:
        return path.resolve().relative_to(PROJECT_ROOT.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def parse_csv(values: list[str]) -> set[str]:
    result: set[str] = set()
    for value in values:
        for part in value.split(","):
            part = part.strip()
            if part:
                result.add(part)
    return result


def require_prompt_ready(items: list[dict[str, Any]]) -> None:
    blocked = [item for item in items if not item.get("PromptReady")]
    if blocked:
        ids = ", ".join(str(item.get("VisualID", "")) for item in blocked[:20])
        raise RuntimeError(f"Prompt readiness blocked for {len(blocked)} item(s): {ids}")


def selected_items(payload: dict[str, Any], domains: set[str], visual_ids: set[str], limit: int) -> list[dict[str, Any]]:
    items = [item for item in payload.get("Items", []) if isinstance(item, dict)]
    if domains:
        items = [item for item in items if str(item.get("Domain", "")) in domains]
    if visual_ids:
        items = [item for item in items if str(item.get("VisualID", "")) in visual_ids]
    items.sort(key=lambda item: (str(item.get("Priority", "")), str(item.get("Domain", "")), str(item.get("AssetType", "")), str(item.get("VisualID", ""))))
    if limit > 0:
        items = items[:limit]
    return items


def group_by_domain(items: list[dict[str, Any]]) -> dict[str, list[dict[str, Any]]]:
    result: dict[str, list[dict[str, Any]]] = {}
    for item in items:
        domain = str(item.get("Domain", "") or "unknown")
        result.setdefault(domain, []).append(item)
    return result


def run_generation_for_group(
    *,
    run_script: Path,
    config_path: Path,
    domain: str,
    items: list[dict[str, Any]],
    batch_id: str,
    variants: int,
    delay_seconds: float,
    dry_run: bool,
    extra_args: list[str],
) -> None:
    ids = ",".join(str(item["VisualID"]) for item in items)
    group_batch = f"{batch_id}_{domain}" if batch_id else f"formalv2_prompt_ready_{domain}"
    command = [
        "powershell",
        "-NoProfile",
        "-ExecutionPolicy",
        "Bypass",
        "-File",
        str(run_script),
        "-Config",
        str(config_path),
        "-Provider",
        "novelai",
        "-Status",
        "approved",
        "-VisualID",
        ids,
        "-Variants",
        str(variants),
        "-Concurrency",
        "1",
        "-DelaySeconds",
        str(delay_seconds),
        "-BatchID",
        group_batch,
        "-PreserveStatus",
    ]
    if dry_run:
        command.append("-DryRun")
    command.extend(extra_args)
    print(f"[RUN] domain={domain} count={len(items)} batch={group_batch} dry_run={dry_run}")
    print("[CMD] " + " ".join(command))
    completed = subprocess.run(command, cwd=PROJECT_ROOT)
    if completed.returncode != 0:
        raise RuntimeError(f"Run-ArtGeneration failed for domain={domain} with exit code {completed.returncode}")


def main() -> int:
    parser = argparse.ArgumentParser(description="Run Formal V2 prompt-ready generation in serial domain batches.")
    parser.add_argument("--readiness-path", default="")
    parser.add_argument("--run-script", default="")
    parser.add_argument("--config", default="")
    parser.add_argument("--domain", action="append", default=[])
    parser.add_argument("--visual-id", action="append", default=[])
    parser.add_argument("--limit", type=int, default=0)
    parser.add_argument("--variants", type=int, default=1)
    parser.add_argument("--delay-seconds", type=float, default=1.0)
    parser.add_argument("--batch-id", default="nai_formalv2_program_integrate_prompt_specific_20260609_01")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--require-token", action="store_true")
    parser.add_argument("--extra", action="append", default=[])
    args = parser.parse_args()

    readiness_path = Path(args.readiness_path) if args.readiness_path else default_readiness_path()
    run_script = Path(args.run_script) if args.run_script else SCRIPT_DIR / "Run-ArtGeneration.ps1"
    config_path = Path(args.config) if args.config else SCRIPT_DIR / "ai_image_gateway.local.yaml"
    if not readiness_path.is_absolute():
        readiness_path = PROJECT_ROOT / readiness_path
    if not run_script.is_absolute():
        run_script = PROJECT_ROOT / run_script
    if not config_path.is_absolute():
        config_path = PROJECT_ROOT / config_path

    if not readiness_path.exists():
        raise FileNotFoundError(readiness_path)
    if not run_script.exists():
        raise FileNotFoundError(run_script)
    if not config_path.exists():
        raise FileNotFoundError(config_path)
    if args.require_token and not os.environ.get("NAI_ACCESS_TOKEN"):
        raise RuntimeError("NAI_ACCESS_TOKEN is not set.")

    payload = read_json(readiness_path)
    domains = parse_csv(args.domain)
    visual_ids = parse_csv(args.visual_id)
    items = selected_items(payload, domains, visual_ids, args.limit)
    require_prompt_ready(items)
    if not items:
        print("[DONE] no matching prompt-ready items.")
        return 0

    summary = payload.get("Summary", {})
    print(
        "[READY] "
        f"source={repo_path(readiness_path)} "
        f"program_integrate={summary.get('ProgramIntegrateVisualCount')} "
        f"prompt_ready={summary.get('PromptReadyCount')} "
        f"selected={len(items)}"
    )
    extra_args: list[str] = []
    for extra in args.extra:
        if extra.strip():
            extra_args.extend(extra.split())
    for domain, group in group_by_domain(items).items():
        run_generation_for_group(
            run_script=run_script,
            config_path=config_path,
            domain=domain,
            items=group,
            batch_id=args.batch_id,
            variants=args.variants,
            delay_seconds=args.delay_seconds,
            dry_run=args.dry_run,
            extra_args=extra_args,
        )
    print("[DONE] Formal V2 prompt-ready generation command sequence complete.")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
