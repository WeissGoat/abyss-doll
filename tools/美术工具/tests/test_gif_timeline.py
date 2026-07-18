from __future__ import annotations

import hashlib
import json
import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image, ImageDraw

TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from gif_character_replace.timeline import PreflightError, extract_timeline  # noqa: E402


class GifTimelineTests(unittest.TestCase):
    def _write_gif(
        self,
        path: Path,
        frame_count: int,
        *,
        durations: list[int] | None = None,
        transparent: bool = False,
    ) -> None:
        frames: list[Image.Image] = []
        for index in range(frame_count):
            background = (0, 0, 0, 0) if transparent else (30, 40, 50, 255)
            frame = Image.new("RGBA", (64, 64), background)
            draw = ImageDraw.Draw(frame)
            draw.rectangle(
                (4 + index, 12, 23 + index, 45),
                fill=(80 + index * 4, 120, 200, 255),
            )
            frames.append(frame)
        frame_durations = durations or [100] * frame_count
        frames[0].save(
            path,
            format="GIF",
            save_all=True,
            append_images=frames[1:],
            duration=frame_durations,
            loop=0,
            disposal=2,
            transparency=0 if transparent else None,
            optimize=False,
        )

    def test_extracts_rgba_frames_and_preserves_timeline(self) -> None:
        durations = [80, 90, 100, 110, 120, 130, 140, 150]
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = root / "source.gif"
            output = root / "timeline" / "frames_original"
            self._write_gif(source, 8, durations=durations)

            metadata = extract_timeline(source, output, min_frames=8, max_frames=30)

            self.assertEqual(metadata.frame_count, 8)
            self.assertEqual(metadata.durations_ms, tuple(durations))
            self.assertEqual(metadata.loop, 0)
            self.assertEqual(metadata.disposal_methods, (2,) * 8)
            self.assertEqual(metadata.frame_paths[0], "frames_original/frame_0000.png")
            self.assertEqual(metadata.source_sha256, hashlib.sha256(source.read_bytes()).hexdigest())
            with Image.open(output / "frame_0007.png") as frame:
                self.assertEqual(frame.mode, "RGBA")
                self.assertEqual(frame.size, (64, 64))

            metadata_path = output.parent / "source_metadata.json"
            persisted = json.loads(metadata_path.read_text(encoding="utf-8"))
            self.assertEqual(persisted, metadata.to_dict())

    def test_rejects_too_few_frames_before_writing_pngs(self) -> None:
        self._assert_frame_bound_rejected(7)

    def test_rejects_too_many_frames_before_writing_pngs(self) -> None:
        self._assert_frame_bound_rejected(31)

    def _assert_frame_bound_rejected(self, frame_count: int) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = root / "source.gif"
            output = root / "timeline" / "frames_original"
            self._write_gif(source, frame_count)

            with self.assertRaisesRegex(PreflightError, "frame count"):
                extract_timeline(source, output, min_frames=8, max_frames=30)

            self.assertFalse(output.exists())
            self.assertFalse((output.parent / "source_metadata.json").exists())

    def test_transparent_gif_sets_transparency_flag(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = root / "transparent.gif"
            output = root / "timeline" / "frames_original"
            self._write_gif(source, 8, transparent=True)

            metadata = extract_timeline(source, output, min_frames=8, max_frames=30)

            self.assertTrue(metadata.has_transparency)
            with Image.open(output / "frame_0000.png") as frame:
                self.assertLess(frame.getchannel("A").getextrema()[0], 255)

    def test_rejects_non_gif_without_writing_frames(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = root / "source.png"
            output = root / "timeline" / "frames_original"
            Image.new("RGBA", (64, 64), (1, 2, 3, 255)).save(source)

            with self.assertRaisesRegex(PreflightError, "not a GIF"):
                extract_timeline(source, output, min_frames=8, max_frames=30)

            self.assertFalse(output.exists())


if __name__ == "__main__":
    unittest.main()
