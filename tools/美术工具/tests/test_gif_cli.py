from __future__ import annotations

import asyncio
import dataclasses
import json
import sys
import tempfile
import unittest
from contextlib import asynccontextmanager
from io import BytesIO
from pathlib import Path

from PIL import Image


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from gif_character_replace.backend import GeneratedImage  # noqa: E402
from gif_character_replace.models import RunConfig  # noqa: E402
from gif_character_replace_cli import main  # noqa: E402


class FakeBackend:
    def __init__(self) -> None:
        self.calls = 0

    async def generate_anchor(self, prompt, width, height):
        self.calls += 1
        output = BytesIO()
        Image.new("RGBA", (width, height), (100, 100, 100, 255)).save(output, format="PNG")
        return GeneratedImage(output.getvalue(), "openai_images", "fake", self.calls, {}, 0)

    async def replace_frame(self, identity_images, frame, prompt, width, height):
        self.calls += 1
        with Image.open(BytesIO(frame)) as source:
            image = source.convert("RGBA").copy()
        output = BytesIO()
        image.save(output, format="PNG")
        return GeneratedImage(output.getvalue(), "gemini_chat_image", "fake", self.calls, {}, 0)


def factory_for(backend: FakeBackend):
    @asynccontextmanager
    async def factory(config: RunConfig):
        yield backend

    return factory


class GifCliTests(unittest.TestCase):
    def _gif(self, root: Path) -> Path:
        frames = [Image.new("RGBA", (32, 24), (index * 25, 50, 80, 255)) for index in range(8)]
        for index, image in enumerate(frames):
            image.putpixel((index, index), (255, 255, 255, 255))
        source = root / "source.gif"
        frames[0].save(source, save_all=True, append_images=frames[1:], duration=[80] * 8, loop=0, disposal=2)
        return source

    def test_prompt_and_prompt_file_are_mutually_exclusive(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = self._gif(root)
            prompt_file = root / "prompt.txt"
            prompt_file.write_text("character", encoding="utf-8")
            with self.assertRaises(SystemExit):
                main(
                    [
                        "--input-gif",
                        str(source),
                        "--prompt",
                        "one",
                        "--prompt-file",
                        str(prompt_file),
                    ]
                )

    def test_dry_run_does_not_call_provider_and_writes_timeline(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = self._gif(root)
            backend = FakeBackend()
            code = main(
                [
                    "--input-gif",
                    str(source),
                    "--prompt",
                    "replacement",
                    "--output-root",
                    str(root / "out"),
                    "--dry-run",
                ],
                factory_for(backend),
            )
            self.assertEqual(code, 0)
            self.assertEqual(backend.calls, 0)
            run_dirs = list((root / "out" / "gif_character_replace").iterdir())
            self.assertEqual(len(run_dirs), 1)
            self.assertTrue((run_dirs[0] / "timeline/source_metadata.json").is_file())

    def test_existing_run_rejects_input_mutation(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = self._gif(root)
            backend = FakeBackend()
            main(
                [
                    "--input-gif",
                    str(source),
                    "--prompt",
                    "replacement",
                    "--output-root",
                    str(root / "out"),
                    "--dry-run",
                ],
                factory_for(backend),
            )
            run_id = next((root / "out" / "gif_character_replace").iterdir()).name
            with self.assertRaises(SystemExit):
                main(
                    [
                        "--run-id",
                        run_id,
                        "--output-root",
                        str(root / "out"),
                        "--prompt",
                        "mutated",
                    ],
                    factory_for(backend),
                )

    def test_offline_end_to_end_reaches_encoded_result(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = self._gif(root)
            reference = root / "reference.png"
            Image.new("RGBA", (32, 24), (180, 90, 40, 255)).save(reference)
            backend = FakeBackend()
            output_root = root / "out"

            self.assertEqual(
                main(
                    [
                        "--input-gif",
                        str(source),
                        "--prompt",
                        "replacement",
                        "--reference",
                        str(reference),
                        "--output-root",
                        str(output_root),
                        "--delay-seconds",
                        "0",
                        "--encoder",
                        "pillow",
                    ],
                    factory_for(backend),
                ),
                0,
            )
            run_dir = next((output_root / "gif_character_replace").iterdir())
            self.assertEqual(
                main(
                    [
                        "--run-id",
                        run_dir.name,
                        "--output-root",
                        str(output_root),
                        "--approve-appearance-anchor",
                    ],
                    factory_for(backend),
                ),
                0,
            )
            appearance_state = json.loads(
                (run_dir / "run_state.json").read_text(encoding="utf-8")
            )
            self.assertEqual(appearance_state["status"], "awaiting_preview_approval")
            self.assertTrue((run_dir / "identity/appearance_anchor.png").is_file())
            calls_after_action_preview = backend.calls
            self.assertEqual(
                main(
                    [
                        "--run-id",
                        run_dir.name,
                        "--output-root",
                        str(output_root),
                        "--approve-appearance-anchor",
                    ],
                    factory_for(backend),
                ),
                0,
            )
            self.assertEqual(backend.calls, calls_after_action_preview)
            self.assertEqual(
                main(
                    [
                        "--run-id",
                        run_dir.name,
                        "--output-root",
                        str(output_root),
                        "--approve-preview",
                    ],
                    factory_for(backend),
                ),
                0,
            )
            self.assertEqual(
                main(
                    [
                        "--run-id",
                        run_dir.name,
                        "--output-root",
                        str(output_root),
                        "--resume",
                    ],
                    factory_for(backend),
                ),
                0,
            )
            result = run_dir / "output/result.gif"
            self.assertTrue(result.is_file())
            with Image.open(result) as gif:
                self.assertEqual(gif.n_frames, 8)
                self.assertEqual(gif.size, (32, 24))
                self.assertEqual(gif.info.get("loop"), 0)
            completed_state = (run_dir / "run_state.json").read_bytes()
            with self.assertRaises(SystemExit):
                main(
                    [
                        "--run-id",
                        run_dir.name,
                        "--output-root",
                        str(output_root),
                        "--approve-appearance-anchor",
                    ],
                    factory_for(backend),
                )
            self.assertEqual((run_dir / "run_state.json").read_bytes(), completed_state)

    def test_existing_run_imports_valid_action_text_sidecar(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = self._gif(root)
            backend = FakeBackend()
            output_root = root / "out"
            self.assertEqual(
                main(
                    [
                        "--input-gif",
                        str(source),
                        "--prompt",
                        "replacement",
                        "--output-root",
                        str(output_root),
                        "--dry-run",
                    ],
                    factory_for(backend),
                ),
                0,
            )
            run_dir = next((output_root / "gif_character_replace").iterdir())
            state = json.loads((run_dir / "run_state.json").read_text(encoding="utf-8"))
            sidecar = root / "action_texts.json"
            sidecar.write_text(
                json.dumps(
                    {
                        "source_sha256": state["timeline"]["source_sha256"],
                        "frame_count": state["timeline"]["frame_count"],
                        "actions": {
                            str(index): f"action {index}"
                            for index in range(state["timeline"]["frame_count"])
                        },
                    }
                ),
                encoding="utf-8",
            )
            self.assertEqual(
                main(
                    [
                        "--run-id",
                        run_dir.name,
                        "--output-root",
                        str(output_root),
                        "--set-action-texts",
                        str(sidecar),
                    ],
                    factory_for(backend),
                ),
                0,
            )
            stored = json.loads(
                (run_dir / "identity/frame_action_texts.json").read_text(encoding="utf-8")
            )
            self.assertEqual(stored["actions"]["0"], "action 0")

    def test_invalid_dry_run_persists_preflight_failure(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            frames = [Image.new("RGBA", (16, 16), (index * 30, 0, 0, 255)) for index in range(7)]
            source = root / "short.gif"
            frames[0].save(
                source,
                save_all=True,
                append_images=frames[1:],
                duration=[80] * 7,
                loop=0,
                disposal=2,
            )
            output_root = root / "out"
            with self.assertRaises(SystemExit):
                main(
                    [
                        "--input-gif",
                        str(source),
                        "--prompt",
                        "replacement",
                        "--output-root",
                        str(output_root),
                        "--dry-run",
                    ]
                )
            run_dir = next((output_root / "gif_character_replace").iterdir())
            self.assertTrue((run_dir / "reports/preflight.json").is_file())
            state = json.loads((run_dir / "run_state.json").read_text(encoding="utf-8"))
            self.assertEqual(state["status"], "failed")


if __name__ == "__main__":
    unittest.main()
