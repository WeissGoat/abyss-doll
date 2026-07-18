# -*- coding: utf-8 -*-
from __future__ import annotations

import hashlib
import json
import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from register_art_processing_round import register_processing_round  # noqa: E402


class RegisterArtProcessingRoundTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        root = Path(self.temp_dir.name)
        self.manifest_path = root / "manifest.json"
        self.incoming_root = root / "incoming"
        self.manifest_path.write_text(
            json.dumps(
                {
                    "Entries": [
                        {
                            "VisualID": "doll_zero_dialogue_neutral",
                            "ProductionProfile": "character_portrait_set",
                            "Status": "generated",
                        }
                    ]
                }
            ),
            encoding="utf-8",
        )
        self.workspace = self.incoming_root / "character_portraits" / "doll_zero_dialogue_neutral"

    def tearDown(self) -> None:
        self.temp_dir.cleanup()

    def make_staging(
        self,
        *,
        state: str,
        candidates: list[tuple[str, str, str]],
    ) -> Path:
        staging = Path(self.temp_dir.name) / f"staging_{len(list(Path(self.temp_dir.name).glob('staging_*')))}"
        staging.mkdir()
        decision_candidates = []
        for filename, status, color in candidates:
            path = staging / filename
            Image.new("RGBA", (2, 2), color).save(path)
            decision_candidates.append(
                {
                    "File": filename,
                    "Status": status,
                    "SHA256": hashlib.sha256(path.read_bytes()).hexdigest(),
                    "Width": 2,
                    "Height": 2,
                    "Format": "png",
                    "Reasons": [],
                }
            )
        (staging / "decision.json").write_text(
            json.dumps({"State": state, "Candidates": decision_candidates}),
            encoding="utf-8",
        )
        (staging / "process_report.json").write_text(json.dumps({"State": state}), encoding="utf-8")
        (staging / "technical_review.json").write_text(json.dumps({"Status": state}), encoding="utf-8")
        (staging / "visual_review.json").write_text(json.dumps({"Status": state}), encoding="utf-8")
        return staging

    def test_registration_publishes_staging_as_next_numeric_round(self) -> None:
        staging = self.make_staging(state="passed", candidates=[("001.png", "passed", "red")])
        result = register_processing_round(
            manifest_path=self.manifest_path,
            incoming_root=self.incoming_root,
            visual_id="doll_zero_dialogue_neutral",
            staging_dir=staging,
            dry_run=False,
        )
        self.assertEqual(result["RoundNumber"], 1)
        self.assertTrue((self.workspace / "processed" / "1" / "001.png").exists())

    def test_registration_rejects_hash_mismatch(self) -> None:
        staging = self.make_staging(state="passed", candidates=[("001.png", "passed", "red")])
        decision = json.loads((staging / "decision.json").read_text(encoding="utf-8"))
        decision["Candidates"][0]["SHA256"] = "0" * 64
        (staging / "decision.json").write_text(json.dumps(decision), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "SHA256"):
            register_processing_round(
                manifest_path=self.manifest_path,
                incoming_root=self.incoming_root,
                visual_id="doll_zero_dialogue_neutral",
                staging_dir=staging,
                dry_run=False,
            )

    def test_registration_rejects_selected_or_approved_paths(self) -> None:
        staging = self.make_staging(state="passed", candidates=[("001.png", "passed", "red")])
        decision = json.loads((staging / "decision.json").read_text(encoding="utf-8"))
        decision["SelectedPath"] = "selected/001.png"
        (staging / "decision.json").write_text(json.dumps(decision), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "SelectedPath"):
            register_processing_round(
                manifest_path=self.manifest_path,
                incoming_root=self.incoming_root,
                visual_id="doll_zero_dialogue_neutral",
                staging_dir=staging,
                dry_run=False,
            )

    def test_dry_run_writes_nothing(self) -> None:
        staging = self.make_staging(state="decision_required", candidates=[("001.png", "warning", "red")])
        result = register_processing_round(
            manifest_path=self.manifest_path,
            incoming_root=self.incoming_root,
            visual_id="doll_zero_dialogue_neutral",
            staging_dir=staging,
            dry_run=True,
        )
        self.assertEqual(result["RoundNumber"], 1)
        self.assertFalse((self.workspace / "processed").exists())


if __name__ == "__main__":
    unittest.main()
