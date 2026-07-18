from __future__ import annotations

import dataclasses
import json
import sys
import tempfile
import unittest
from pathlib import Path
from unittest.mock import patch

from PIL import Image


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from gif_character_replace.encoder import encode_gif  # noqa: E402
from gif_character_replace.models import (  # noqa: E402
    FrameRecord,
    FrameStatus,
    RunConfig,
    RunStatus,
)
from gif_character_replace.store import RunStore, atomic_write_json  # noqa: E402
from gif_character_replace.timeline import extract_timeline  # noqa: E402
from gif_character_replace.workflow import GifReplacementWorkflow  # noqa: E402


class GifEncoderTests(unittest.TestCase):
    def _frames(self, root: Path, transparent: bool = False):
        frame_paths = []
        alpha_paths = []
        for index in range(8):
            frame = Image.new("RGBA", (48, 32), (index * 25, 60, 90, 255))
            frame.putpixel((index, index), (255, 255, 255, 255))
            generated = root / f"generated_{index}.png"
            frame.save(generated)
            original = frame.copy()
            if transparent:
                original.putpixel((0, 0), (0, 0, 0, 0))
            alpha = root / f"original_{index}.png"
            original.save(alpha)
            frame_paths.append(generated)
            alpha_paths.append(alpha)
        return frame_paths, alpha_paths

    def test_auto_uses_pillow_and_preserves_timeline(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            frames, alphas = self._frames(root)
            durations = [80, 90, 100, 110, 120, 130, 140, 150]
            with patch("gif_character_replace.encoder.shutil.which", return_value=None):
                result = encode_gif(
                    frames, alphas, durations, 0, root / "result.gif", "auto", True
                )
            self.assertEqual(result.encoder, "pillow")
            self.assertIn("ffmpeg_unavailable_using_pillow", result.warnings)
            with Image.open(result.path) as gif:
                self.assertEqual(gif.n_frames, 8)
                self.assertEqual(gif.size, (48, 32))
                self.assertEqual(gif.info.get("loop"), 0)
                decoded = []
                for index in range(gif.n_frames):
                    gif.seek(index)
                    decoded.append(gif.info.get("duration"))
                self.assertEqual(decoded, durations)

    def test_transparent_alpha_is_reapplied(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            frames, alphas = self._frames(root, transparent=True)
            with patch("gif_character_replace.encoder.shutil.which", return_value=None):
                result = encode_gif(
                    frames,
                    alphas,
                    [80] * 8,
                    0,
                    root / "transparent.gif",
                    "auto",
                    True,
                )
            with Image.open(result.path) as gif:
                gif.seek(0)
                rgba = gif.convert("RGBA")
                self.assertEqual(rgba.getpixel((0, 0))[3], 0)

    def test_ffmpeg_branch_writes_concat_and_palette_commands(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            frames, alphas = self._frames(root)
            durations = [80, 90, 100, 110, 120, 130, 140, 150]
            commands = []

            def fake_run(command, **kwargs):
                commands.append(command)
                destination = Path(command[-1])
                if "palettegen" in command:
                    Image.new("RGB", (16, 16), (255, 0, 0)).save(destination)
                else:
                    # Simulate FFmpeg output with Pillow so verification can run.
                    images = [Image.open(path).convert("P") for path in frames]
                    images[0].save(
                        destination,
                        save_all=True,
                        append_images=images[1:],
                        duration=durations,
                        loop=0,
                        disposal=2,
                        optimize=False,
                    )

            captured_concat = {}

            def fake_run_capture(command, **kwargs):
                concat = Path(command[command.index("-i") + 1])
                captured_concat["text"] = concat.read_text(encoding="utf-8")
                return fake_run(command, **kwargs)

            with patch("gif_character_replace.encoder.shutil.which", return_value="ffmpeg"), patch(
                "gif_character_replace.encoder.subprocess.run", side_effect=fake_run_capture
            ):
                result = encode_gif(
                    frames, alphas, durations, 0, root / "ffmpeg.gif", "auto", False
                )

            self.assertEqual(result.encoder, "ffmpeg")
            self.assertEqual(len(commands), 2)
            self.assertIn("palettegen", commands[0])
            self.assertIn("paletteuse=dither=sierra2_4a", commands[1])
            self.assertEqual(captured_concat["text"].count("duration "), 8)
            self.assertEqual(captured_concat["text"].count("file '"), 9)

    def test_repeated_encode_only_keeps_clean_run_ready_and_records_encoder(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            frames, _ = self._frames(root)
            source = root / "source.gif"
            images = [Image.open(path).convert("RGBA") for path in frames]
            images[0].save(
                source,
                save_all=True,
                append_images=images[1:],
                duration=[80] * 8,
                loop=0,
                disposal=2,
            )
            config = RunConfig.new(
                input_gif=source,
                output_root=root / "runs",
                prompt="character",
                references=(),
            )
            config = dataclasses.replace(config, encoder="pillow")
            store = RunStore.create(config)
            timeline = extract_timeline(source, store.paths.original_frames_dir, 8, 30)
            records = []
            for index, source_path in enumerate(timeline.frame_paths):
                output = store.paths.generated_raw_dir / f"frame_{index:04d}.png"
                output.parent.mkdir(parents=True, exist_ok=True)
                output.write_bytes(frames[index].read_bytes())
                records.append(
                    FrameRecord(
                        index,
                        str((store.paths.timeline_dir / source_path).resolve()),
                        str(output.resolve()),
                        FrameStatus.GENERATED,
                        1,
                        (),
                        (),
                        (),
                    )
                )
            review_path = store.paths.reports_dir / "frame_review.json"
            atomic_write_json(
                review_path,
                {
                    "frame_risks": [
                        {"frame_index": index, "codes": [], "hard_failure": False}
                        for index in range(8)
                    ],
                    "sequence_codes": [],
                    "hard_failure": False,
                    "contact_sheet_path": "",
                },
            )
            store.update_state(
                lambda state: dataclasses.replace(
                    state,
                    status=RunStatus.RUNNING,
                    timeline=timeline,
                    frames=tuple(records),
                    review_report=str(review_path.resolve()),
                )
            )
            workflow = GifReplacementWorkflow(object())

            first = workflow.encode_only(store.paths.root)
            second = workflow.encode_only(store.paths.root)

            self.assertEqual(first.status, RunStatus.READY)
            self.assertEqual(second.status, RunStatus.READY)
            summary = json.loads(
                (store.paths.reports_dir / "run_summary.json").read_text(encoding="utf-8")
            )
            self.assertEqual(summary["actual_encoder"], "pillow")


if __name__ == "__main__":
    unittest.main()
