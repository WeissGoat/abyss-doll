from __future__ import annotations

import dataclasses
import hashlib
import json
import sys
import tempfile
import unittest
from io import BytesIO
from pathlib import Path

from PIL import Image


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from gif_character_replace.backend import GeneratedImage  # noqa: E402
from gif_character_replace.action_text import install_action_texts  # noqa: E402
from gif_character_replace.models import FrameStatus, RunConfig, RunStatus  # noqa: E402
from gif_character_replace.preview import (  # noqa: E402
    approve_appearance_anchor,
    approve_preview,
)
from gif_character_replace.store import RunStore  # noqa: E402
from gif_character_replace.workflow import (  # noqa: E402
    STRICT_REPAIR_SENTENCE,
    GifReplacementWorkflow,
)


class RecordingBackend:
    def __init__(self, fail_hashes=()) -> None:
        self.anchor_calls = 0
        self.frame_hashes: list[str] = []
        self.identity_counts: list[int] = []
        self.identity_payloads: list[list[bytes]] = []
        self.prompts: list[str] = []
        self.fail_hashes = set(fail_hashes)

    @staticmethod
    def _png(width: int, height: int, color) -> bytes:
        output = BytesIO()
        Image.new("RGBA", (width, height), color).save(output, format="PNG")
        return output.getvalue()

    async def generate_anchor(self, prompt: str, width: int, height: int) -> GeneratedImage:
        self.anchor_calls += 1
        return GeneratedImage(
            self._png(width, height, (30 * self.anchor_calls, 50, 70, 255)),
            "openai_images",
            "fake",
            self.anchor_calls,
            {},
            0.0,
        )

    async def replace_frame(
        self, identity_images, frame: bytes, prompt: str, width: int, height: int
    ) -> GeneratedImage:
        digest = hashlib.sha256(frame).hexdigest()
        self.frame_hashes.append(digest)
        self.identity_counts.append(len(identity_images))
        self.identity_payloads.append(list(identity_images))
        self.prompts.append(prompt)
        if digest in self.fail_hashes:
            raise RuntimeError("synthetic frame failure")
        with Image.open(BytesIO(frame)) as source:
            image = source.convert("RGBA").copy()
        output = BytesIO()
        image.save(output, format="PNG")
        return GeneratedImage(
            output.getvalue(), "gemini_chat_image", "fake", len(self.frame_hashes), {}, 0.0
        )


class GifWorkflowTests(unittest.IsolatedAsyncioTestCase):
    def _input(self, root: Path, frame_count: int = 10) -> tuple[Path, Path]:
        frames = []
        for index in range(frame_count):
            image = Image.new("RGBA", (40, 32), (index * 19 % 255, 30, 80, 255))
            image.putpixel((index % 40, index % 32), (255, 255, 255, 255))
            frames.append(image)
        source = root / "source.gif"
        frames[0].save(
            source,
            save_all=True,
            append_images=frames[1:],
            duration=[70 + index for index in range(frame_count)],
            loop=0,
            disposal=2,
        )
        reference = root / "reference.png"
        Image.new("RGBA", (40, 32), (200, 100, 50, 255)).save(reference)
        return source, reference

    def _config(self, root: Path, references) -> RunConfig:
        source, _ = self._input(root)
        config = RunConfig.new(
            input_gif=source,
            output_root=root / "runs",
            prompt="replacement mechanic",
            references=references,
        )
        return dataclasses.replace(config, delay_seconds=0)

    async def _prepared_with_reference(self, root: Path, backend: RecordingBackend):
        source, reference = self._input(root)
        config = RunConfig.new(
            input_gif=source,
            output_root=root / "runs",
            prompt="replacement mechanic",
            references=(reference,),
        )
        config = dataclasses.replace(config, delay_seconds=0)
        workflow = GifReplacementWorkflow(backend)
        state = await workflow.prepare(config)
        store = RunStore.load(Path(config.output_root) / "gif_character_replace" / config.run_id)
        return workflow, store, state

    async def _approve_previews(
        self, workflow: GifReplacementWorkflow, store: RunStore
    ) -> None:
        approve_appearance_anchor(store)
        await workflow.generate_action_preview(store.paths.root)
        approve_preview(store)

    async def test_prepare_stops_at_identity_selection_without_references(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source, _ = self._input(root)
            config = RunConfig.new(
                input_gif=source,
                output_root=root / "runs",
                prompt="replacement mechanic",
                references=(),
            )
            config = dataclasses.replace(config, delay_seconds=0)
            backend = RecordingBackend()

            state = await GifReplacementWorkflow(backend).prepare(config)

            self.assertEqual(state.status, RunStatus.AWAITING_IDENTITY_SELECTION)
            self.assertEqual(backend.anchor_calls, 3)
            self.assertEqual(backend.frame_hashes, [])

    async def test_prepare_with_reference_stops_at_appearance_approval(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            backend = RecordingBackend()
            _, _, state = await self._prepared_with_reference(Path(temp), backend)
            self.assertEqual(state.status, RunStatus.AWAITING_APPEARANCE_APPROVAL)
            self.assertEqual(len(backend.frame_hashes), 1)

    async def test_selected_preview_frame_can_be_rerun_before_approval(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            backend = RecordingBackend()
            workflow, store, state = await self._prepared_with_reference(Path(temp), backend)
            identity_index = state.preview_indices[0]
            identity_path = (
                store.paths.preview_dir / "identity_frame" / f"frame_{identity_index:04d}.png"
            )
            previous_bytes = identity_path.read_bytes()
            rerun_backend = RecordingBackend()

            rerun_state = await GifReplacementWorkflow(rerun_backend).rerun_frames(
                store.paths.root, [identity_index], strict=True
            )

            self.assertEqual(rerun_state.status, RunStatus.AWAITING_APPEARANCE_APPROVAL)
            self.assertFalse(rerun_state.preview_approved)
            self.assertEqual(len(rerun_backend.frame_hashes), 1)
            self.assertIn(STRICT_REPAIR_SENTENCE, rerun_backend.prompts[0])
            self.assertTrue(identity_path.is_file())
            self.assertEqual(identity_path.read_bytes(), previous_bytes)

    async def test_resume_requires_approval_and_each_source_frame_is_used_once(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            backend = RecordingBackend()
            workflow, store, state = await self._prepared_with_reference(Path(temp), backend)
            with self.assertRaisesRegex(ValueError, "approval"):
                await workflow.resume_after_preview(store.paths.root)

            approve_appearance_anchor(store)
            await workflow.generate_action_preview(store.paths.root)
            Path(store.config.references[0]).unlink()
            approve_preview(store)
            completed = await workflow.resume_after_preview(store.paths.root)

            self.assertEqual(completed.status, RunStatus.REVIEW_REQUIRED)
            self.assertTrue(Path(completed.result_gif or "").is_file())
            self.assertEqual(len(backend.frame_hashes), len(completed.frames))
            source_hashes = {
                hashlib.sha256(Path(frame.source_path).read_bytes()).hexdigest()
                for frame in completed.frames
            }
            self.assertEqual(set(backend.frame_hashes), source_hashes)
            self.assertEqual(
                len((store.paths.reports_dir / "requests.jsonl").read_text(encoding="utf-8").splitlines()),
                len(completed.frames),
            )
            self.assertTrue(
                all(frame.status in (FrameStatus.GENERATED, FrameStatus.ACCEPTED) for frame in completed.frames)
            )
            generated_record = next(
                frame.request_records[-1]
                for frame in completed.frames
                if frame.status == FrameStatus.GENERATED
            )
            anchor_path = store.load_state().appearance_anchor
            self.assertEqual(generated_record["identity_paths"], [anchor_path])
            self.assertEqual(
                generated_record["appearance_anchor_sha256"],
                store.load_state().appearance_anchor_sha256,
            )
            self.assertTrue(all(count == 1 for count in backend.identity_counts[1:]))
            anchor_bytes = Path(anchor_path or "").read_bytes()
            self.assertTrue(
                all(payload == [anchor_bytes] for payload in backend.identity_payloads[1:])
            )

    async def test_action_text_sidecar_is_injected_per_frame(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            backend = RecordingBackend()
            workflow, store, state = await self._prepared_with_reference(root, backend)
            action_texts = {
                index: f"unique action description for frame {index}"
                for index in range(state.timeline.frame_count)
            }
            action_map = root / "action_texts.json"
            action_map.write_text(
                json.dumps(
                    {
                        "source_sha256": state.timeline.source_sha256,
                        "frame_count": state.timeline.frame_count,
                        "actions": {str(index): text for index, text in action_texts.items()},
                    }
                ),
                encoding="utf-8",
            )
            install_action_texts(store, action_map)
            await self._approve_previews(workflow, store)
            await workflow.resume_after_preview(store.paths.root)

            text_by_hash = {
                hashlib.sha256(Path(frame.source_path).read_bytes()).hexdigest(): action_texts[frame.index]
                for frame in store.load_state().frames
            }
            for frame_hash, prompt in zip(backend.frame_hashes[1:], backend.prompts[1:]):
                self.assertIn(text_by_hash[frame_hash], prompt)

    async def test_resume_skips_existing_outputs_after_interruption(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            backend = RecordingBackend()
            workflow, store, _ = await self._prepared_with_reference(Path(temp), backend)
            await self._approve_previews(workflow, store)
            first = await workflow.resume_after_preview(store.paths.root)
            replacement_backend = RecordingBackend()

            resumed = await GifReplacementWorkflow(replacement_backend).resume_after_preview(
                store.paths.root
            )

            self.assertEqual(replacement_backend.frame_hashes, [])
            self.assertEqual(resumed.frames, first.frames)

    async def test_rerun_targets_only_requested_frames_and_uses_strict_prompt(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            backend = RecordingBackend()
            workflow, store, _ = await self._prepared_with_reference(Path(temp), backend)
            await self._approve_previews(workflow, store)
            before = await workflow.resume_after_preview(store.paths.root)
            rerun_backend = RecordingBackend()

            after = await GifReplacementWorkflow(rerun_backend).rerun_frames(
                store.paths.root, [4, 9], strict=True
            )

            expected = [
                hashlib.sha256(Path(before.frames[index].source_path).read_bytes()).hexdigest()
                for index in (4, 9)
            ]
            self.assertEqual(rerun_backend.frame_hashes, expected)
            self.assertTrue(all(STRICT_REPAIR_SENTENCE in prompt for prompt in rerun_backend.prompts))
            self.assertEqual(after.frames[4].attempts, before.frames[4].attempts + 1)
            self.assertEqual(after.frames[9].attempts, before.frames[9].attempts + 1)

    async def test_failed_frame_becomes_manual_review_and_later_frames_continue(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            preview_backend = RecordingBackend()
            workflow, store, _ = await self._prepared_with_reference(Path(temp), preview_backend)
            await self._approve_previews(workflow, store)
            state = store.load_state()
            fail_index = next(
                index for index in range(len(state.frames)) if index not in state.preview_indices
            )
            fail_hash = hashlib.sha256(Path(state.frames[fail_index].source_path).read_bytes()).hexdigest()
            batch_backend = RecordingBackend({fail_hash})

            completed = await GifReplacementWorkflow(batch_backend).resume_after_preview(
                store.paths.root
            )

            self.assertEqual(completed.frames[fail_index].status, FrameStatus.MANUAL_REVIEW)
            failed_record = completed.frames[fail_index].request_records[-1]
            self.assertEqual(
                failed_record["identity_paths"],
                [completed.appearance_anchor],
            )
            self.assertEqual(
                failed_record["appearance_anchor_sha256"],
                completed.appearance_anchor_sha256,
            )
            self.assertGreater(len(batch_backend.frame_hashes), 1)
            later = [frame for frame in completed.frames[fail_index + 1 :] if frame.index not in state.preview_indices]
            self.assertTrue(any(frame.status == FrameStatus.GENERATED for frame in later))

    async def test_failed_targeted_rerun_clears_previous_result(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            backend = RecordingBackend()
            workflow, store, _ = await self._prepared_with_reference(root, backend)
            await self._approve_previews(workflow, store)
            completed = await workflow.resume_after_preview(store.paths.root)
            self.assertTrue(Path(completed.result_gif or "").is_file())
            target = 4
            fail_hash = hashlib.sha256(Path(completed.frames[target].source_path).read_bytes()).hexdigest()

            failed = await GifReplacementWorkflow(RecordingBackend({fail_hash})).rerun_frames(
                store.paths.root, [target], strict=True
            )

            self.assertEqual(failed.status, RunStatus.FAILED)
            self.assertIsNone(failed.result_gif)

    async def test_resume_and_rerun_reject_modified_appearance_anchor(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            backend = RecordingBackend()
            workflow, store, _ = await self._prepared_with_reference(Path(temp), backend)
            await self._approve_previews(workflow, store)
            anchor = Path(store.load_state().appearance_anchor or "")
            anchor.write_bytes(b"modified")

            with self.assertRaisesRegex(ValueError, "hash"):
                await workflow.resume_after_preview(store.paths.root)
            with self.assertRaisesRegex(ValueError, "hash"):
                await workflow.rerun_frames(store.paths.root, [4], strict=True)

    async def test_invalid_preflight_is_persisted(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source, reference = self._input(root, frame_count=7)
            config = RunConfig.new(
                input_gif=source,
                output_root=root / "runs",
                prompt="replacement mechanic",
                references=(reference,),
            )
            config = dataclasses.replace(config, delay_seconds=0)
            with self.assertRaisesRegex(ValueError, "outside"):
                await GifReplacementWorkflow(RecordingBackend()).prepare(config)
            run_root = Path(config.output_root) / "gif_character_replace" / config.run_id
            store = RunStore.load(run_root)
            self.assertEqual(store.load_state().status, RunStatus.FAILED)
            self.assertTrue((store.paths.reports_dir / "preflight.json").is_file())


if __name__ == "__main__":
    unittest.main()
