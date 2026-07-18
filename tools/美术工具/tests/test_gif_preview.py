from __future__ import annotations

import dataclasses
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
from gif_character_replace.identity import (  # noqa: E402
    build_identity_contract,
    prepare_identity_inputs,
)
from gif_character_replace.models import RunConfig, RunStatus  # noqa: E402
from gif_character_replace.preview import (  # noqa: E402
    approve_identity,
    approve_preview,
    generate_identity_candidates,
    generate_preview,
    select_preview_frames,
)
from gif_character_replace.store import RunStore  # noqa: E402
from gif_character_replace.timeline import extract_timeline  # noqa: E402


class FakeBackend:
    def __init__(self) -> None:
        self.anchor_calls = 0
        self.frame_calls: list[bytes] = []
        self.identity_counts: list[int] = []

    async def generate_anchor(self, prompt: str, width: int, height: int) -> GeneratedImage:
        self.anchor_calls += 1
        image = Image.new("RGBA", (width, height), (30 * self.anchor_calls, 60, 90, 255))
        return self._result(image, "openai_images")

    async def replace_frame(
        self, identity_images, frame: bytes, prompt: str, width: int, height: int
    ) -> GeneratedImage:
        self.frame_calls.append(frame)
        self.identity_counts.append(len(identity_images))
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

    async def test_existing_reference_skips_anchor_and_generates_only_two_previews(self) -> None:
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
            preview_state = await generate_preview(store, backend, inputs)

            self.assertEqual(backend.anchor_calls, 0)
            self.assertEqual(identity_state.status, RunStatus.PREFLIGHT)
            self.assertEqual(len(backend.frame_calls), 2)
            self.assertEqual(backend.identity_counts, [3, 3])
            self.assertEqual(preview_state.status, RunStatus.AWAITING_PREVIEW_APPROVAL)
            self.assertFalse(preview_state.preview_approved)

    async def test_preview_approval_requires_both_decodable_expected_size_files(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            reference = root / "reference.png"
            Image.new("RGBA", (32, 32), (200, 100, 50, 255)).save(reference)
            store = self._store(root, (reference,))
            backend = FakeBackend()
            contract = build_identity_contract(store.config.prompt, [reference])
            await generate_identity_candidates(store, backend, contract)
            inputs = prepare_identity_inputs(contract, store.paths.identity_dir)
            state = await generate_preview(store, backend, inputs)
            action = store.paths.preview_dir / "action_frame" / f"frame_{state.preview_indices[1]:04d}.png"
            action.unlink()
            with self.assertRaises(FileNotFoundError):
                approve_preview(store)
            Image.new("RGBA", (16, 16), (0, 0, 0, 255)).save(action)
            with self.assertRaisesRegex(ValueError, "does not match"):
                approve_preview(store)
            Image.new("RGBA", (32, 32), (0, 0, 0, 255)).save(action)
            approved = approve_preview(store)
            self.assertTrue(approved.preview_approved)
            self.assertEqual(approved.status, RunStatus.RUNNING)


if __name__ == "__main__":
    unittest.main()
