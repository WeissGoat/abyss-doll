"""CLI for the resumable GIF character replacement workflow."""

from __future__ import annotations

import argparse
import asyncio
import json
from dataclasses import replace
from contextlib import AbstractAsyncContextManager
from pathlib import Path
from typing import Callable, Sequence

from gif_character_replace.backend import GatewayImageBackend, ImageBackend
from gif_character_replace.models import RunConfig
from gif_character_replace.preview import approve_identity, approve_preview
from gif_character_replace.preview import generate_preview
from gif_character_replace.identity import IdentityInputs
from gif_character_replace.store import RunStore
from gif_character_replace.store import atomic_write_json
from gif_character_replace.models import RunStatus
from gif_character_replace.timeline import extract_timeline
from gif_character_replace.workflow import GifReplacementWorkflow


BackendFactory = Callable[[RunConfig], AbstractAsyncContextManager[ImageBackend]]


def _parser() -> argparse.ArgumentParser:
    parser = argparse.ArgumentParser(description="Replace a GIF character frame by frame")
    parser.add_argument("--input-gif")
    prompt = parser.add_mutually_exclusive_group()
    prompt.add_argument("--prompt")
    prompt.add_argument("--prompt-file")
    parser.add_argument("--reference", action="append", default=[])
    parser.add_argument("--output-root", default=".")
    parser.add_argument("--config")
    parser.add_argument("--provider", default="gemini_chat_image")
    parser.add_argument("--max-frames", type=int, default=30)
    parser.add_argument("--delay-seconds", type=float, default=2.0)
    parser.add_argument("--retry-count", type=int, default=3)
    parser.add_argument("--visual-retry-count", type=int, default=2)
    parser.add_argument("--preserve-transparency", action=argparse.BooleanOptionalAction, default=True)
    parser.add_argument("--encoder", choices=("auto", "ffmpeg", "pillow"), default="auto")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--run-id")
    parser.add_argument("--select-identity", type=int)
    parser.add_argument("--approve-preview", action="store_true")
    parser.add_argument("--resume", action="store_true")
    parser.add_argument("--rerun-frame", action="append", type=int, default=[])
    parser.add_argument("--repair-mode", choices=("normal", "strict"), default="normal")
    parser.add_argument("--encode-only", action="store_true")
    return parser


def _print_state(state) -> None:
    print(json.dumps(state.to_dict(), ensure_ascii=False, indent=2))


def _read_new_prompt(args: argparse.Namespace) -> str:
    if args.prompt is not None:
        return args.prompt
    if args.prompt_file:
        return Path(args.prompt_file).read_text(encoding="utf-8")
    raise ValueError("one of --prompt or --prompt-file is required for a new run")


def _run_root(args: argparse.Namespace) -> Path:
    if not args.run_id:
        raise ValueError("--run-id is required for an existing run action")
    return Path(args.output_root).resolve() / "gif_character_replace" / args.run_id


def _production_backend_factory(config: RunConfig):
    return GatewayImageBackend(
        config_path=config.config_path or None,
        retry_count=config.retry_count,
        delay_seconds=config.delay_seconds,
        frame_provider=config.frame_provider,
        anchor_provider=config.anchor_provider,
    )


def _dry_run(config: RunConfig):
    store = RunStore.create(config)
    try:
        timeline = extract_timeline(
            Path(config.input_gif),
            store.paths.original_frames_dir,
            config.min_frames,
            config.max_frames,
        )
    except Exception as exc:
        atomic_write_json(
            store.paths.reports_dir / "preflight.json",
            {"status": "failed", "error": str(exc), "input_gif": config.input_gif},
        )
        store.update_state(
            lambda current: replace(
                current,
                status=RunStatus.FAILED,
                errors=(*current.errors, str(exc)),
            )
        )
        raise
    state = store.update_state(
        lambda current: replace(
            current,
            timeline=timeline,
            warnings=(*current.warnings, "dry_run_no_provider_calls"),
        )
    )
    return store, state


async def _main_async(args: argparse.Namespace, backend_factory: BackendFactory | None) -> int:
    existing_action = bool(
        args.run_id
        or args.select_identity is not None
        or args.approve_preview
        or args.resume
        or args.rerun_frame
        or args.encode_only
    )
    if existing_action:
        if args.input_gif or args.prompt or args.prompt_file or args.reference:
            raise ValueError("existing-run actions cannot mutate input GIF, prompt, or references")
        run_root = _run_root(args)
        store = RunStore.load(run_root)
        if args.select_identity is not None:
            approve_identity(store, args.select_identity)
            config = store.load_config()
            contract_path = store.paths.identity_dir / "identity_contract.json"
            payload = json.loads(contract_path.read_text(encoding="utf-8"))
            inputs = IdentityInputs(
                contract_path=str(contract_path.resolve()),
                provider_image_paths=tuple(payload.get("provider_image_paths", ())),
                reference_board_path=payload.get("reference_board_path"),
            )
            factory = backend_factory or _production_backend_factory
            async with factory(config) as backend:
                preview_state = await generate_preview(store, backend, inputs)
            _print_state(preview_state)
        if args.approve_preview:
            _print_state(approve_preview(store))
        if args.resume or args.rerun_frame:
            config = store.load_config()
            factory = backend_factory or _production_backend_factory
            async with factory(config) as backend:
                workflow = GifReplacementWorkflow(backend)
                if args.resume:
                    state = await workflow.resume_after_preview(run_root)
                else:
                    state = await workflow.rerun_frames(
                        run_root, args.rerun_frame, strict=args.repair_mode == "strict"
                    )
            _print_state(state)
        if args.encode_only:
            state = GifReplacementWorkflow(object()).encode_only(run_root)
            _print_state(state)
        return 0

    if not args.input_gif:
        raise ValueError("--input-gif is required for a new run")
    prompt = _read_new_prompt(args)
    references = tuple(Path(path) for path in args.reference)
    config = RunConfig.new(
        input_gif=Path(args.input_gif),
        output_root=Path(args.output_root),
        prompt=prompt,
        references=references,
        config_path=Path(args.config).resolve() if args.config else None,
    )
    config = replace(
        config,
        frame_provider=args.provider,
        max_frames=args.max_frames,
        delay_seconds=args.delay_seconds,
        retry_count=args.retry_count,
        visual_retry_count=args.visual_retry_count,
        preserve_transparency=args.preserve_transparency,
        encoder=args.encoder,
    )
    if args.dry_run:
        _, state = _dry_run(config)
        _print_state(state)
        return 0
    factory = backend_factory or _production_backend_factory
    async with factory(config) as backend:
        state = await GifReplacementWorkflow(backend).prepare(config)
    _print_state(state)
    return 0


def main(
    argv: Sequence[str] | None = None,
    backend_factory: BackendFactory | None = None,
) -> int:
    parser = _parser()
    args = parser.parse_args(argv)
    try:
        return asyncio.run(_main_async(args, backend_factory))
    except (ValueError, FileNotFoundError) as exc:
        parser.error(str(exc))
        return 2


if __name__ == "__main__":
    raise SystemExit(main())
