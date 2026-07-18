"""Resumable state machine for independent GIF frame replacement."""

from __future__ import annotations

import asyncio
import json
import shutil
from dataclasses import replace
from datetime import datetime, timezone
from pathlib import Path
from typing import Sequence

from .backend import ImageBackend
from .identity import (
    IdentityInputs,
    build_identity_contract,
    prepare_identity_inputs,
)
from .models import FrameRecord, FrameStatus, RunConfig, RunState, RunStatus
from .preview import generate_identity_candidates, generate_preview, rerun_preview_frame
from .store import RunStore, append_jsonl, atomic_write_json
from .timeline import extract_timeline


STRICT_REPAIR_SENTENCE = (
    "STRICT REPAIR: preserve every non-character pixel, background edge, camera crop, "
    "and object placement from the current source frame."
)


def _utc_now() -> str:
    return datetime.now(timezone.utc).isoformat()


class GifReplacementWorkflow:
    def __init__(self, backend: ImageBackend) -> None:
        self.backend = backend

    async def prepare(self, config: RunConfig) -> RunState:
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
                lambda state: replace(
                    state,
                    status=RunStatus.FAILED,
                    errors=(*state.errors, str(exc)),
                )
            )
            raise
        frames = tuple(
            FrameRecord(
                index=index,
                source_path=str((store.paths.timeline_dir / source_path).resolve()),
                output_path=None,
                status=FrameStatus.PENDING,
                attempts=0,
                request_records=(),
                risks=(),
                errors=(),
            )
            for index, source_path in enumerate(timeline.frame_paths)
        )
        store.update_state(
            lambda state: replace(
                state,
                status=RunStatus.PREFLIGHT,
                timeline=timeline,
                frames=frames,
            )
        )
        contract = build_identity_contract(
            config.prompt, store.copied_reference_paths()
        )
        identity_state = await generate_identity_candidates(store, self.backend, contract)
        if identity_state.status == RunStatus.AWAITING_IDENTITY_SELECTION:
            return identity_state
        inputs = prepare_identity_inputs(contract, store.paths.identity_dir)
        return await generate_preview(store, self.backend, inputs)

    async def resume_after_preview(self, run_root: Path) -> RunState:
        store = RunStore.load(run_root)
        state = store.load_state()
        if not state.preview_approved:
            raise ValueError("preview approval is required before full batch generation")
        self._promote_preview_frames(store)
        await self._generate_indices(store, range(len(store.load_state().frames)), strict=False)
        reviewed = self._review(store)
        if reviewed.status == RunStatus.FAILED:
            return reviewed
        return self.encode_only(store.paths.root)

    async def rerun_frames(
        self,
        run_root: Path,
        indices: Sequence[int],
        strict: bool,
    ) -> RunState:
        store = RunStore.load(run_root)
        state = store.load_state()
        unique_indices = tuple(dict.fromkeys(indices))
        for index in unique_indices:
            if index < 0 or index >= len(state.frames):
                raise IndexError(f"frame index out of range: {index}")
        if not state.preview_approved:
            if state.preview_indices is None or any(
                index not in state.preview_indices for index in unique_indices
            ):
                raise ValueError(
                    "before approval, only selected preview frames can be rerun"
                )
            inputs, _ = self._load_identity_inputs(store)
            latest = state
            for index in unique_indices:
                latest = await rerun_preview_frame(
                    store,
                    self.backend,
                    inputs,
                    index,
                    STRICT_REPAIR_SENTENCE if strict else "",
                )
            return latest
        store.update_state(lambda current: replace(current, result_gif=None))
        await self._generate_indices(store, unique_indices, strict=strict, force=True)
        reviewed = self._review(store)
        if reviewed.status == RunStatus.FAILED:
            return reviewed
        return self.encode_only(store.paths.root)

    def encode_only(self, run_root: Path) -> RunState:
        from .encoder import encode_gif

        store = RunStore.load(run_root)
        state = store.load_state()
        if state.timeline is None:
            raise ValueError("timeline is missing")
        if state.status == RunStatus.FAILED:
            raise ValueError("hard-failed runs cannot be encoded")
        if any(
            frame.status not in (FrameStatus.GENERATED, FrameStatus.ACCEPTED)
            or not frame.output_path
            or not Path(frame.output_path).is_file()
            for frame in state.frames
        ):
            raise ValueError("all frames must have valid generated outputs before encoding")
        if not state.review_report or not Path(state.review_report).is_file():
            state = self._review(store)
            if state.status == RunStatus.FAILED:
                raise ValueError("review found hard failures; encoding refused")
        frame_paths = [Path(frame.output_path or "") for frame in state.frames]
        alpha_paths = [Path(frame.source_path) for frame in state.frames]
        result = encode_gif(
            frame_paths,
            alpha_paths,
            state.timeline.durations_ms,
            state.timeline.loop,
            store.paths.output_dir / "result.gif",
            store.config.encoder,
            store.config.preserve_transparency,
        )
        review_payload = json.loads(Path(state.review_report).read_text(encoding="utf-8"))
        visual_risks = set(review_payload.get("sequence_codes", ()))
        for frame_risk in review_payload.get("frame_risks", ()):
            if not frame_risk.get("hard_failure", False):
                visual_risks.update(frame_risk.get("codes", ()))
        final_status = RunStatus.REVIEW_REQUIRED if visual_risks else RunStatus.READY
        final_state = store.update_state(
            lambda current: replace(
                current,
                status=final_status,
                result_gif=str(result.path.resolve()),
                warnings=tuple(dict.fromkeys((*current.warnings, *result.warnings))),
            )
        )
        self._write_run_summary(store, result.encoder)
        return final_state

    @staticmethod
    def _review(store: RunStore) -> RunState:
        from .review import review_sequence

        state = store.load_state()
        if state.timeline is None:
            raise ValueError("timeline is missing")
        source_paths = [Path(frame.source_path) for frame in state.frames]
        output_paths = [
            Path(frame.output_path)
            if frame.output_path
            else store.paths.generated_raw_dir / f"missing_{frame.index:04d}.png"
            for frame in state.frames
        ]
        review = review_sequence(
            store.config,
            state.timeline,
            source_paths,
            output_paths,
            store.paths.reports_dir,
            store.paths.output_dir,
        )
        report_path = store.paths.reports_dir / "frame_review.json"
        risks_by_index = {risk.frame_index: risk.codes for risk in review.frame_risks}

        def mutator(current: RunState) -> RunState:
            frames = tuple(
                replace(frame, risks=risks_by_index.get(frame.index, ()))
                for frame in current.frames
            )
            return replace(
                current,
                status=RunStatus.FAILED if review.hard_failure else RunStatus.RUNNING,
                frames=frames,
                review_report=str(report_path.resolve()),
                contact_sheet=review.contact_sheet_path,
                warnings=review.sequence_codes,
                result_gif=None if review.hard_failure else current.result_gif,
            )

        reviewed = store.update_state(mutator)
        if review.hard_failure:
            GifReplacementWorkflow._write_run_summary(store, None)
        return reviewed

    @staticmethod
    def _write_run_summary(store: RunStore, actual_encoder: str | None) -> None:
        state = store.load_state()
        atomic_write_json(
            store.paths.reports_dir / "run_summary.json",
            {
                "run_id": store.config.run_id,
                "status": state.status.value,
                "frame_count": state.timeline.frame_count if state.timeline else 0,
                "frame_status_counts": {
                    status.value: sum(1 for frame in state.frames if frame.status == status)
                    for status in FrameStatus
                },
                "actual_encoder": actual_encoder,
                "result_gif": state.result_gif,
                "review_report": state.review_report,
                "contact_sheet": state.contact_sheet,
                "warnings": list(state.warnings),
                "errors": list(state.errors),
                "updated_at": _utc_now(),
            },
        )

    def _load_identity_inputs(self, store: RunStore) -> tuple[IdentityInputs, str]:
        contract_path = store.paths.identity_dir / "identity_contract.json"
        payload = json.loads(contract_path.read_text(encoding="utf-8"))
        inputs = IdentityInputs(
            contract_path=str(contract_path.resolve()),
            provider_image_paths=tuple(payload.get("provider_image_paths", ())),
            reference_board_path=payload.get("reference_board_path"),
        )
        return inputs, str(payload["provider_prompt"])

    @staticmethod
    def _identity_bytes(store: RunStore, inputs: IdentityInputs) -> list[bytes]:
        state = store.load_state()
        if state.selected_identity:
            paths = (Path(state.selected_identity),)
        else:
            paths = tuple(Path(path) for path in inputs.provider_image_paths)
        if not paths:
            raise ValueError("no selected identity or reference images are available")
        return [path.read_bytes() for path in paths]

    @staticmethod
    def _replace_record(store: RunStore, updated: FrameRecord) -> RunState:
        def mutator(state: RunState) -> RunState:
            records = list(state.frames)
            records[updated.index] = updated
            return replace(state, frames=tuple(records), status=RunStatus.RUNNING)

        return store.update_state(mutator)

    def _promote_preview_frames(self, store: RunStore) -> None:
        state = store.load_state()
        if state.preview_indices is None:
            return
        preview_locations = (
            store.paths.preview_dir
            / "identity_frame"
            / f"frame_{state.preview_indices[0]:04d}.png",
            store.paths.preview_dir
            / "action_frame"
            / f"frame_{state.preview_indices[1]:04d}.png",
        )
        for index, source in zip(state.preview_indices, preview_locations):
            current = store.load_state().frames[index]
            if current.output_path and Path(current.output_path).is_file():
                continue
            destination = store.paths.generated_raw_dir / f"frame_{index:04d}.png"
            destination.parent.mkdir(parents=True, exist_ok=True)
            shutil.copy2(source, destination)
            metadata_path = source.with_suffix(".json")
            metadata = (
                json.loads(metadata_path.read_text(encoding="utf-8"))
                if metadata_path.is_file()
                else {"source_index": index, "promoted_from_preview": True}
            )
            metadata["promoted_from_preview"] = True
            updated = replace(
                current,
                output_path=str(destination.resolve()),
                status=FrameStatus.ACCEPTED,
                attempts=max(1, current.attempts),
                request_records=(*current.request_records, metadata),
            )
            self._replace_record(store, updated)

    async def _generate_indices(
        self,
        store: RunStore,
        indices: Sequence[int],
        *,
        strict: bool,
        force: bool = False,
    ) -> RunState:
        state = store.load_state()
        if state.timeline is None:
            raise ValueError("timeline is missing")
        inputs, base_prompt = self._load_identity_inputs(store)
        prompt = f"{base_prompt}\n\n{STRICT_REPAIR_SENTENCE}" if strict else base_prompt
        identity_images = self._identity_bytes(store, inputs)
        request_number = 0

        for index in indices:
            current = store.load_state().frames[index]
            if (
                not force
                and current.status in (FrameStatus.GENERATED, FrameStatus.ACCEPTED)
                and current.output_path
                and Path(current.output_path).is_file()
            ):
                continue
            if request_number > 0 and store.config.delay_seconds > 0:
                await asyncio.sleep(store.config.delay_seconds)
            request_number += 1
            source_path = Path(current.source_path)
            destination = store.paths.generated_raw_dir / f"frame_{index:04d}.png"
            request_started_at = _utc_now()
            try:
                generated = await self.backend.replace_frame(
                    identity_images,
                    source_path.read_bytes(),
                    prompt,
                    state.timeline.width,
                    state.timeline.height,
                )
                destination.parent.mkdir(parents=True, exist_ok=True)
                destination.write_bytes(generated.image_bytes)
                identity_path_records = (
                    [state.selected_identity]
                    if state.selected_identity
                    else list(inputs.provider_image_paths)
                )
                request_record = {
                    "frame_index": index,
                    "prompt": prompt,
                    "provider": generated.provider,
                    "model": generated.model,
                    "seed": generated.seed,
                    "generation_params": generated.generation_params,
                    "gateway_attempt_count": int(
                        generated.generation_params.get("gateway_attempt_count", 1)
                    ),
                    "gateway_retry_errors": list(
                        generated.generation_params.get("gateway_retry_errors", ())
                    ),
                    "cost": generated.cost,
                    "byte_length": len(generated.image_bytes),
                    "request_started_at": request_started_at,
                    "request_finished_at": _utc_now(),
                    "identity_paths": identity_path_records,
                    "source_path": str(source_path.resolve()),
                }
                atomic_write_json(destination.with_suffix(".json"), request_record)
                append_jsonl(store.paths.reports_dir / "requests.jsonl", request_record)
                updated = replace(
                    current,
                    output_path=str(destination.resolve()),
                    status=FrameStatus.GENERATED,
                    attempts=current.attempts + 1,
                    request_records=(*current.request_records, request_record),
                )
            except Exception as exc:
                request_record = {
                    "frame_index": index,
                    "prompt": prompt,
                    "source_path": str(source_path.resolve()),
                    "error": str(exc),
                    "request_started_at": request_started_at,
                    "request_finished_at": _utc_now(),
                    "gateway_attempt_count": int(getattr(exc, "attempts", 1)),
                    "gateway_errors": list(getattr(exc, "errors", (str(exc),))),
                }
                updated = replace(
                    current,
                    output_path=None,
                    status=FrameStatus.MANUAL_REVIEW,
                    attempts=current.attempts + 1,
                    request_records=(*current.request_records, request_record),
                    errors=(*current.errors, str(exc)),
                )
                append_jsonl(store.paths.reports_dir / "requests.jsonl", request_record)
            self._replace_record(store, updated)

        return store.update_state(lambda current: replace(current, status=RunStatus.RUNNING))
