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

from sync_approved_art import choose_source, repo_path  # noqa: E402


class SyncApprovedSourceTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        self.workspace = Path(self.temp_dir.name) / "workspace"
        self.workspace.mkdir()

    def tearDown(self) -> None:
        self.temp_dir.cleanup()

    def write_selected(self, name: str, content: bytes) -> Path:
        selected = self.workspace / "selected"
        selected.mkdir(exist_ok=True)
        path = selected / name
        path.write_bytes(content)
        return path

    def write_round(self, number: int, state: str, candidates: list[tuple[str, str, str]]) -> Path:
        round_dir = self.workspace / "processed" / str(number)
        round_dir.mkdir(parents=True, exist_ok=True)
        records = []
        for name, status, input_path in candidates:
            path = round_dir / name
            Image.new("RGBA", (2, 2), "red").save(path)
            records.append(
                {
                    "File": name,
                    "Input": input_path,
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
        return round_dir

    def test_choose_source_prefers_manifest_selected_path(self) -> None:
        manifest_choice = self.write_selected("b.png", b"manifest")
        self.write_selected("a.png", b"legacy")
        source, kind = choose_source(
            self.workspace,
            {"SelectedPath": repo_path(manifest_choice)},
            candidate_batch=False,
        )
        self.assertEqual((source, kind), (manifest_choice, "manifest_selected"))

    def test_choose_source_uses_legacy_selected_directory(self) -> None:
        legacy = self.write_selected("a.png", b"legacy")
        source, kind = choose_source(self.workspace, {}, candidate_batch=False)
        self.assertEqual((source, kind), (legacy, "selected"))

    def test_choose_source_uses_latest_single_passed_round_without_flag(self) -> None:
        processed = self.write_round(2, "passed", [("001.png", "passed", "raw/source.png")])
        source, kind = choose_source(self.workspace, {}, candidate_batch=False)
        self.assertEqual((source, kind), (processed / "001.png", "processed_round"))

    def test_choose_source_rejects_latest_failed_round(self) -> None:
        self.write_round(2, "failed", [])
        source, kind = choose_source(self.workspace, {}, candidate_batch=False)
        self.assertIsNone(source)
        self.assertEqual(kind, "")

    def test_choose_source_rejects_multiple_passed_candidates(self) -> None:
        self.write_round(
            2,
            "passed",
            [("001.png", "passed", "raw/a.png"), ("002.png", "passed", "raw/b.png")],
        )
        source, kind = choose_source(self.workspace, {}, candidate_batch=False)
        self.assertIsNone(source)
        self.assertEqual(kind, "")

    def test_candidate_batch_filters_by_decision_input_path(self) -> None:
        raw_a = self.workspace / "raw" / "a.png"
        raw_b = self.workspace / "raw" / "b.png"
        raw_a.parent.mkdir(parents=True)
        raw_a.write_bytes(b"a")
        raw_b.write_bytes(b"b")
        processed = self.write_round(
            2,
            "passed",
            [
                ("001.png", "passed", repo_path(raw_a)),
                ("002.png", "passed", repo_path(raw_b)),
            ],
        )
        entry = {"CandidateRawFiles": [repo_path(raw_b)]}
        source, kind = choose_source(self.workspace, entry, candidate_batch=True)
        self.assertEqual((source, kind), (processed / "002.png", "processed_round"))


if __name__ == "__main__":
    unittest.main()
