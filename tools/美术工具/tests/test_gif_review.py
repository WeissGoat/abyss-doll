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

from gif_character_replace.models import RunConfig, TimelineMetadata  # noqa: E402
from gif_character_replace.review import review_sequence  # noqa: E402


class GifReviewTests(unittest.TestCase):
    def _config(self, root: Path) -> RunConfig:
        source = root / "source.gif"
        source.write_bytes(b"GIF89a")
        return RunConfig.new(
            input_gif=source,
            output_root=root / "runs",
            prompt="character",
            references=(),
        )

    def _sequence(self, root: Path, count: int = 8, transparent: bool = False):
        source_paths = []
        output_paths = []
        for index in range(count):
            base = np.full((32, 32, 4), (20, 30, 40, 0 if transparent else 255), dtype=np.uint8)
            base[10:22, 12:20, :3] = (80 + index, 100, 120)
            base[10:22, 12:20, 3] = 255
            source = root / f"source_{index:02d}.png"
            output = root / f"output_{index:02d}.png"
            Image.fromarray(base, mode="RGBA").save(source)
            Image.fromarray(base, mode="RGBA").save(output)
            source_paths.append(source)
            output_paths.append(output)
        timeline = TimelineMetadata(
            32,
            32,
            count,
            tuple([80] * count),
            0,
            transparent,
            tuple([2] * count),
            "hash",
            tuple(path.name for path in source_paths),
        )
        return timeline, source_paths, output_paths

    def _review(self, root: Path, timeline, sources, outputs):
        return review_sequence(
            self._config(root),
            timeline,
            sources,
            outputs,
            root / "reports",
            root / "output",
        )

    def test_missing_corrupt_wrong_size_and_solid_outputs_are_hard_failures(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            timeline, sources, outputs = self._sequence(root)
            outputs[0].unlink()
            outputs[1].write_bytes(b"not an image")
            Image.new("RGB", (16, 16), (50, 60, 70)).save(outputs[2])
            Image.new("RGB", (32, 32), (50, 50, 50)).save(outputs[3])

            review = self._review(root, timeline, sources, outputs)

            self.assertTrue(review.hard_failure)
            self.assertIn("missing_output", review.frame_risks[0].codes)
            self.assertIn("corrupt_output", review.frame_risks[1].codes)
            self.assertIn("wrong_size", review.frame_risks[2].codes)
            self.assertIn("solid_color_output", review.frame_risks[3].codes)

    def test_center_change_does_not_flag_background_but_edge_change_does(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            timeline, sources, outputs = self._sequence(root)
            center = np.asarray(Image.open(outputs[0]).convert("RGBA")).copy()
            center[12:20, 13:19, :3] = 240
            Image.fromarray(center).save(outputs[0])
            edge = np.asarray(Image.open(outputs[1]).convert("RGBA")).copy()
            edge[:8, :, :3] = 240
            Image.fromarray(edge).save(outputs[1])

            review = self._review(root, timeline, sources, outputs)

            self.assertNotIn("background_drift", review.frame_risks[0].codes)
            self.assertIn("background_drift", review.frame_risks[1].codes)

    def test_temporal_flicker_and_loop_seam_are_sequence_risks(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            timeline, sources, outputs = self._sequence(root)
            jump = np.asarray(Image.open(outputs[4]).convert("RGBA")).copy()
            jump[:, :, :3] = 220
            jump[0, 0, :3] = 0
            Image.fromarray(jump).save(outputs[4])
            seam = np.asarray(Image.open(outputs[-1]).convert("RGBA")).copy()
            seam[:, :, :3] = 180
            seam[0, 0, :3] = 0
            Image.fromarray(seam).save(outputs[-1])

            review = self._review(root, timeline, sources, outputs)

            self.assertIn("temporal_flicker", review.sequence_codes)
            self.assertIn("loop_seam", review.sequence_codes)

    def test_identity_limited_and_identity_drift_are_distinguished(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            timeline, sources, outputs = self._sequence(root)
            # Identical output has no reliable changed region.
            drift = np.asarray(Image.open(outputs[1]).convert("RGBA")).copy()
            drift[10:22, 12:20, :3] = (250, 10, 10)
            Image.fromarray(drift).save(outputs[1])

            review = self._review(root, timeline, sources, outputs)

            self.assertIn("identity_check_limited", review.frame_risks[0].codes)
            self.assertIn("identity_drift", review.frame_risks[1].codes)

    def test_transparency_always_records_silhouette_limitation_and_artifacts(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            timeline, sources, outputs = self._sequence(root, transparent=True)

            review = self._review(root, timeline, sources, outputs)

            self.assertIn("transparent_silhouette_limited", review.sequence_codes)
            self.assertTrue(Path(review.contact_sheet_path).is_file())
            self.assertTrue((root / "reports/frame_review.json").is_file())


if __name__ == "__main__":
    unittest.main()
