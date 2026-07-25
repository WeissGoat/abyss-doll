from __future__ import annotations

import dataclasses
import json
import sys
import tempfile
import unittest
from pathlib import Path

import numpy as np
from PIL import Image


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from gif_character_replace.backend import GeneratedImage  # noqa: E402
from gif_character_replace.action_text import install_action_texts  # noqa: E402
from gif_character_replace.identity import (  # noqa: E402
    build_identity_contract,
    prepare_identity_inputs,
)
from gif_character_replace.models import RunConfig, RunStatus  # noqa: E402
from gif_character_replace.preview import (  # noqa: E402
    approve_appearance_anchor,
    approve_identity,
    approve_preview,
    generate_action_preview,
    generate_identity_candidates,
    generate_identity_preview,
    select_preview_frames,
)
from gif_character_replace.store import RunStore  # noqa: E402
from gif_character_replace.timeline import extract_timeline  # noqa: E402


class FakeBackend:
    def __init__(self, fail_frame_call: int | None = None) -> None:
        self.anchor_calls = 0
        self.frame_calls: list[bytes] = []
        self.identity_counts: list[int] = []
        self.identity_payloads: list[list[bytes]] = []
        self.prompts: list[str] = []
        self.fail_frame_call = fail_frame_call

    async def generate_anchor(self, prompt: str, width: int, height: int) -> GeneratedImage:
        self.anchor_calls += 1
        image = Image.new("RGBA", (width, height), (30 * self.anchor_calls, 60, 90, 255))
        return self._result(image, "openai_images")

    async def replace_frame(
        self, identity_images, frame: bytes, prompt: str, width: int, height: int
    ) -> GeneratedImage:
        self.frame_calls.append(frame)
        self.identity_counts.append(len(identity_images))
        self.identity_payloads.append(list(identity_images))
        self.prompts.append(prompt)
        if self.fail_frame_call == len(self.frame_calls):
            raise RuntimeError("synthetic preview failure")
        from io import BytesIO

        with Image.open(BytesIO(frame)) as source:
            image = source.convert("RGBA").copy()
        return self._result(image, "gemini_chat_image")

    @staticmethod
    def _result(image: Image.Image, provider: str) -> GeneratedImage:
        from io import BytesIO

        output = BytesIO()
        image.save(output, format="PNG")
        return GeneratedImage(output.getvalue(), provider, "fake", 1, {}, 0.0)


class GifPreviewTests(unittest.IsolatedAsyncioTestCase):
    def _gif(self, root: Path) -> Path:
        frames = []
        for index in range(8):
            array = np.zeros((32, 32, 4), dtype=np.uint8)
            array[:, :, 3] = 255
            if index in (1, 2, 3):
                checker = (np.indices((32, 32)).sum(axis=0) % 2) * 255
                array[:, :, :3] = checker[:, :, None]
            if index == 6:
                array[:, :, :3] = 255
            # Keep all eight frames distinct so GIF encoders cannot collapse
            # identical frames while retaining the intended scoring pattern.
            array[0:2, index * 3 : index * 3 + 2, :3] = (index * 27) % 256
            frames.append(Image.fromarray(array, mode="RGBA"))
        source = root / "source.gif"
        frames[0].save(
            source,
            save_all=True,
            append_images=frames[1:],
            duration=[80] * 8,
            loop=0,
            disposal=2,
        )
        return source

    def _store(self, root: Path, references=()) -> RunStore:
        source = self._gif(root)
        config = RunConfig.new(
            input_gif=source,
            output_root=root / "runs",
            prompt="silver-haired mechanic",
            references=references,
        )
        config = dataclasses.replace(config, delay_seconds=0)
        store = RunStore.create(config)
        timeline = extract_timeline(source, store.paths.original_frames_dir, 8, 30)
        store.update_state(lambda state: dataclasses.replace(state, timeline=timeline))
        return store

    def test_selects_sharp_stable_identity_and_largest_action(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            store = self._store(root)
            state = store.load_state()
            paths = [store.paths.timeline_dir / path for path in state.timeline.frame_paths]
            selection = select_preview_frames(paths)
            self.assertEqual(selection.identity_index, 2)
            self.assertEqual(selection.action_index, 6)

    async def test_zero_references_generate_three_candidates_and_require_selection(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            store = self._store(root)
            backend = FakeBackend()
            contract = build_identity_contract(store.config.prompt, [])

            state = await generate_identity_candidates(store, backend, contract)

            self.assertEqual(backend.anchor_calls, 3)
            self.assertEqual(state.status, RunStatus.AWAITING_IDENTITY_SELECTION)
            self.assertTrue(
                (store.paths.identity_dir / "anchor_candidates/anchor_02.png").is_file()
            )
            approved = approve_identity(store, 1)
            self.assertTrue(Path(approved.selected_identity or "").is_file())

    async def test_existing_reference_generates_identity_then_anchor_drives_action_preview(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            references = []
            for index in range(3):
                reference = root / f"reference_{index}.png"
                Image.new("RGBA", (32, 32), (200, 100 + index, 50, 255)).save(reference)
                references.append(reference)
            store = self._store(root, tuple(references))
            backend = FakeBackend()
            contract = build_identity_contract(store.config.prompt, references)

            identity_state = await generate_identity_candidates(store, backend, contract)
            inputs = prepare_identity_inputs(contract, store.paths.identity_dir)
            identity_preview = await generate_identity_preview(store, backend, inputs)

            self.assertEqual(backend.anchor_calls, 0)
            self.assertEqual(identity_state.status, RunStatus.PREFLIGHT)
            self.assertEqual(len(backend.frame_calls), 1)
            self.assertEqual(backend.identity_counts, [3])
            self.assertEqual(
                identity_preview.status, RunStatus.AWAITING_APPEARANCE_APPROVAL
            )

            action_index = identity_preview.preview_indices[1]
            action_map_path = root / "action_texts.json"
            action_map_path.write_text(
                json.dumps(
                    {
                        "source_sha256": identity_preview.timeline.source_sha256,
                        "frame_count": identity_preview.timeline.frame_count,
                        "actions": {
                            str(index): f"frame {index} action description"
                            for index in range(identity_preview.timeline.frame_count)
                        },
                    }
                ),
                encoding="utf-8",
            )
            install_action_texts(store, action_map_path)

            anchored = approve_appearance_anchor(store)
            anchor_path = Path(anchored.appearance_anchor or "")
            self.assertTrue(anchor_path.is_file())
            self.assertTrue(anchored.appearance_anchor_sha256)
            action_preview = await generate_action_preview(store, backend, inputs)

            self.assertEqual(len(backend.frame_calls), 2)
            self.assertEqual(backend.identity_counts, [3, 1])
            self.assertEqual(backend.identity_payloads[-1], [anchor_path.read_bytes()])
            self.assertIn("图一是唯一编辑底图", backend.prompts[-1])
            self.assertIn("动作和表情一律以图二为准", backend.prompts[-1])
            self.assertIn("silver-haired mechanic", backend.prompts[-1])
            self.assertIn(
                f"frame {action_index} action description", backend.prompts[-1]
            )
            action_metadata = json.loads(
                (
                    store.paths.preview_dir
                    / "action_frame"
                    / f"frame_{action_index:04d}.json"
                ).read_text(encoding="utf-8")
            )
            self.assertEqual(
                action_metadata["action_text"],
                f"frame {action_index} action description",
            )
            self.assertEqual(
                len((store.paths.reports_dir / "requests.jsonl").read_text(encoding="utf-8").splitlines()),
                2,
            )
            self.assertEqual(action_preview.status, RunStatus.AWAITING_PREVIEW_APPROVAL)
            self.assertFalse(action_preview.preview_approved)

    async def test_preview_approval_requires_valid_anchor_and_action_frame(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            reference = root / "reference.png"
            Image.new("RGBA", (32, 32), (200, 100, 50, 255)).save(reference)
            store = self._store(root, (reference,))
            backend = FakeBackend()
            contract = build_identity_contract(store.config.prompt, [reference])
            await generate_identity_candidates(store, backend, contract)
            inputs = prepare_identity_inputs(contract, store.paths.identity_dir)
            identity_state = await generate_identity_preview(store, backend, inputs)
            approve_appearance_anchor(store)
            state = await generate_action_preview(store, backend, inputs)
            action = store.paths.preview_dir / "action_frame" / f"frame_{state.preview_indices[1]:04d}.png"
            anchor = Path(state.appearance_anchor or "")
            anchor_bytes = anchor.read_bytes()
            action.unlink()
            with self.assertRaises(FileNotFoundError):
                approve_preview(store)
            Image.new("RGBA", (16, 16), (0, 0, 0, 255)).save(action)
            with self.assertRaisesRegex(ValueError, "does not match"):
                approve_preview(store)
            Image.new("RGBA", (32, 32), (0, 0, 0, 255)).save(action)
            anchor.write_bytes(b"modified")
            with self.assertRaisesRegex(ValueError, "hash"):
                approve_preview(store)
            anchor.write_bytes(anchor_bytes)
            approved = approve_preview(store)
            self.assertTrue(approved.preview_approved)
            self.assertEqual(approved.status, RunStatus.RUNNING)

    async def test_preview_failure_persists_request_evidence_and_failed_state(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            reference = root / "reference.png"
            Image.new("RGBA", (32, 32), (200, 100, 50, 255)).save(reference)
            store = self._store(root, (reference,))
            backend = FakeBackend(fail_frame_call=2)
            contract = build_identity_contract(store.config.prompt, [reference])
            await generate_identity_candidates(store, backend, contract)
            inputs = prepare_identity_inputs(contract, store.paths.identity_dir)

            await generate_identity_preview(store, backend, inputs)
            approve_appearance_anchor(store)
            with self.assertRaisesRegex(RuntimeError, "synthetic preview failure"):
                await generate_action_preview(store, backend, inputs)

            self.assertEqual(store.load_state().status, RunStatus.FAILED)
            records = [
                json.loads(line)
                for line in (store.paths.reports_dir / "requests.jsonl")
                .read_text(encoding="utf-8")
                .splitlines()
            ]
            self.assertEqual(len(records), 2)
            self.assertEqual(records[-1]["error"], "synthetic preview failure")
            self.assertIn("request_finished_at", records[-1])
            anchor = store.load_state().appearance_anchor
            self.assertEqual(records[-1]["identity_paths"], [anchor])
            self.assertEqual(
                records[-1]["appearance_anchor_sha256"],
                store.load_state().appearance_anchor_sha256,
            )


if __name__ == "__main__":
    unittest.main()
