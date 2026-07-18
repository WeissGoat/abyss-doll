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

from generate_art_integration_candidates import classify_entry  # noqa: E402


class IntegrationCandidateRoundTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        self.incoming_root = Path(self.temp_dir.name) / "incoming"
        self.workspace = self.incoming_root / "standard_assets" / "ui_icon_test"
        self.entry = {
            "VisualID": "ui_icon_test",
            "ProductionProfile": "standard_asset",
            "Status": "generated",
            "Priority": "P1",
            "Domain": "item",
            "AssetType": "icon",
        }

    def tearDown(self) -> None:
        self.temp_dir.cleanup()

    def classify(self) -> dict[str, object]:
        return classify_entry(self.entry, {}, {}, self.incoming_root)

    def write_raw(self, name: str) -> None:
        raw = self.workspace / "raw"
        raw.mkdir(parents=True, exist_ok=True)
        Image.new("RGBA", (2, 2), "red").save(raw / name)

    def write_selected(self, name: str) -> None:
        selected = self.workspace / "selected"
        selected.mkdir(parents=True, exist_ok=True)
        Image.new("RGBA", (2, 2), "red").save(selected / name)

    def write_round(self, number: int, state: str, candidates: list[tuple[str, str]]) -> None:
        round_dir = self.workspace / "processed" / str(number)
        round_dir.mkdir(parents=True, exist_ok=True)
        records = []
        for name, status in candidates:
            path = round_dir / name
            Image.new("RGBA", (2, 2), "red").save(path)
            records.append(
                {
                    "File": name,
                    "Status": status,
                    "SHA256": hashlib.sha256(path.read_bytes()).hexdigest(),
                    "Width": 2,
                    "Height": 2,
                    "Format": "png",
                }
            )
        (round_dir / "decision.json").write_text(
            json.dumps({"State": state, "Candidates": records}),
            encoding="utf-8",
        )

    def test_latest_passed_single_candidate_routes_art_select(self) -> None:
        self.write_round(2, "passed", [("001.png", "passed")])
        result = self.classify()
        self.assertEqual(result["Action"], "art_select")
        self.assertEqual(result["ProcessedRound"], 2)
        self.assertEqual(result["ProcessingState"], "passed")
        self.assertTrue(str(result["ProcessedCandidate"]).endswith("processed/2/001.png"))

    def test_latest_failed_round_routes_art_select_with_failure_reason(self) -> None:
        self.write_round(2, "failed", [])
        result = self.classify()
        self.assertEqual(result["Action"], "art_select")
        self.assertEqual(result["ProcessedRound"], 2)
        self.assertEqual(result["ProcessingState"], "failed")
        self.assertEqual(result["ProcessedCandidate"], "")
        self.assertIn("failed", str(result["Reason"]))

    def test_raw_without_round_routes_art_process(self) -> None:
        self.write_raw("001.png")
        result = self.classify()
        self.assertEqual(result["Action"], "art_process")

    def test_selected_still_routes_art_approve(self) -> None:
        self.write_selected("001.png")
        result = self.classify()
        self.assertEqual(result["Action"], "art_approve")


if __name__ == "__main__":
    unittest.main()
