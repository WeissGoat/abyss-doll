# -*- coding: utf-8 -*-

from __future__ import annotations

import hashlib
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from prepare_art_background_candidate import prepare_background_candidate  # noqa: E402


class PrepareArtBackgroundCandidateTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        self.root = Path(self.temp_dir.name)

    def tearDown(self) -> None:
        self.temp_dir.cleanup()

    def make_image(self, name: str, image: Image.Image) -> Path:
        path = self.root / name
        image.save(path, format="PNG")
        return path

    @staticmethod
    def digest(path: Path) -> str:
        return hashlib.sha256(path.read_bytes()).hexdigest()

    def test_alpha_passthrough_stages_existing_transparency(self) -> None:
        source = self.make_image("alpha.png", Image.new("RGBA", (8, 8), (10, 20, 30, 0)))
        image = Image.open(source).convert("RGBA")
        for x in range(2, 6):
            for y in range(1, 7):
                image.putpixel((x, y), (200, 180, 160, 255))
        image.save(source)
        staging = self.root / "processing_candidates" / "run_alpha"

        result = prepare_background_candidate(
            input_path=source,
            staging_dir=staging,
            method="alpha_passthrough",
            expected_input_sha256=self.digest(source),
        )

        self.assertEqual(result["Method"], "alpha_passthrough")
        self.assertTrue((staging / "candidate.png").is_file())
        self.assertTrue((staging / "background-processing.json").is_file())
        evidence = json.loads((staging / "background-processing.json").read_text(encoding="utf-8"))
        self.assertEqual(evidence["Output"]["SHA256"], self.digest(staging / "candidate.png"))

    def test_connected_border_removes_only_border_connected_background(self) -> None:
        image = Image.new("RGB", (10, 10), "white")
        for x in range(3, 7):
            for y in range(2, 8):
                image.putpixel((x, y), (80, 30, 30))
        source = self.make_image("matte.png", image)
        staging = self.root / "processing_candidates" / "run_border"

        prepare_background_candidate(
            input_path=source,
            staging_dir=staging,
            method="connected_border",
            expected_input_sha256=self.digest(source),
            threshold=20,
        )

        with Image.open(staging / "candidate.png") as candidate:
            alpha = candidate.convert("RGBA").getchannel("A")
            self.assertEqual(alpha.getpixel((0, 0)), 0)
            self.assertEqual(alpha.getpixel((5, 5)), 255)

    def test_explicit_mask_sets_candidate_alpha(self) -> None:
        source = self.make_image("source.png", Image.new("RGB", (6, 8), (120, 100, 90)))
        mask = Image.new("L", (6, 8), 0)
        for x in range(1, 5):
            for y in range(2, 7):
                mask.putpixel((x, y), 255)
        mask_path = self.make_image("mask.png", mask)
        staging = self.root / "processing_candidates" / "run_mask"

        result = prepare_background_candidate(
            input_path=source,
            staging_dir=staging,
            method="explicit_mask",
            mask_path=mask_path,
            expected_input_sha256=self.digest(source),
        )

        with Image.open(staging / "candidate.png") as candidate:
            self.assertEqual(candidate.convert("RGBA").getchannel("A").getextrema(), (0, 255))
        self.assertEqual(result["Mask"]["Path"], mask_path.resolve().as_posix())

    def test_explicit_mask_rejects_size_mismatch_and_constant_masks(self) -> None:
        source = self.make_image("source.png", Image.new("RGB", (8, 8), "red"))
        mismatch = self.make_image("mismatch.png", Image.new("L", (4, 4), 128))
        black = self.make_image("black.png", Image.new("L", (8, 8), 0))
        white = self.make_image("white.png", Image.new("L", (8, 8), 255))

        with self.assertRaisesRegex(ValueError, "background_mask_size_mismatch"):
            prepare_background_candidate(
                input_path=source,
                staging_dir=self.root / "processing_candidates" / "mismatch",
                method="explicit_mask",
                mask_path=mismatch,
                expected_input_sha256=self.digest(source),
            )
        for mask_path in (black, white):
            with self.assertRaisesRegex(ValueError, "background_mask_constant"):
                prepare_background_candidate(
                    input_path=source,
                    staging_dir=self.root / "processing_candidates" / mask_path.stem,
                    method="explicit_mask",
                    mask_path=mask_path,
                    expected_input_sha256=self.digest(source),
                )

    def test_input_hash_mismatch_is_rejected(self) -> None:
        source = self.make_image("source.png", Image.new("RGBA", (8, 8), (1, 2, 3, 128)))
        with self.assertRaisesRegex(ValueError, "background_input_hash_mismatch"):
            prepare_background_candidate(
                input_path=source,
                staging_dir=self.root / "processing_candidates" / "hash",
                method="alpha_passthrough",
                expected_input_sha256="0" * 64,
            )

    def test_non_empty_staging_and_formal_output_directories_are_rejected(self) -> None:
        source = self.make_image("source.png", Image.new("RGBA", (8, 8), (1, 2, 3, 128)))
        staging = self.root / "processing_candidates" / "existing"
        staging.mkdir(parents=True)
        (staging / "old.txt").write_text("immutable", encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "background_staging_not_empty"):
            prepare_background_candidate(
                input_path=source,
                staging_dir=staging,
                method="alpha_passthrough",
                expected_input_sha256=self.digest(source),
            )

        for forbidden in ("processed", "selected", "Approved"):
            with self.assertRaisesRegex(ValueError, "background_staging_forbidden"):
                prepare_background_candidate(
                    input_path=source,
                    staging_dir=self.root / forbidden / "run",
                    method="alpha_passthrough",
                    expected_input_sha256=self.digest(source),
                )

    def test_powershell_dry_run_writes_nothing(self) -> None:
        source = self.make_image("source.png", Image.new("RGBA", (8, 8), (1, 2, 3, 128)))
        staging = self.root / "processing_candidates" / "wrapper_dry_run"
        wrapper = TOOLS_DIR / "Prepare-ArtBackgroundCandidate.ps1"

        completed = subprocess.run(
            [
                "powershell",
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                str(wrapper),
                "-InputPath",
                str(source),
                "-StagingDirectory",
                str(staging),
                "-Method",
                "alpha_passthrough",
                "-ExpectedInputSHA256",
                self.digest(source),
                "-DryRun",
            ],
            cwd=TOOLS_DIR.parents[1],
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=30,
            check=False,
        )

        self.assertEqual(completed.returncode, 0, completed.stderr or completed.stdout)
        self.assertIn("dry_run=True", completed.stdout)
        self.assertFalse(staging.exists())


if __name__ == "__main__":
    unittest.main()
