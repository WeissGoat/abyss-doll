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

from art_workspace import workspace_path  # noqa: E402
from migrate_art_processed_rounds import plan_migration, repo_path  # noqa: E402


class MigrateArtProcessedRoundsTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        root = Path(self.temp_dir.name)
        self.manifest_path = root / "manifest.json"
        self.incoming_root = root / "incoming"
        self.entry = {
            "VisualID": "ui_icon_test",
            "ProductionProfile": "standard_asset",
            "Status": "generated",
            "SelectedPath": "",
        }
        self.workspace = workspace_path(self.incoming_root, self.entry)
        self.write_manifest()

    def tearDown(self) -> None:
        self.temp_dir.cleanup()

    def write_manifest(self) -> None:
        self.manifest_path.write_text(json.dumps({"Entries": [self.entry]}), encoding="utf-8")

    def write_flat_processed(self, name: str, content: bytes) -> Path:
        processed = self.workspace / "processed"
        processed.mkdir(parents=True, exist_ok=True)
        path = processed / name
        color = (content[0] if content else 1, 0, 0, 255)
        Image.new("RGBA", (2, 2), color).save(path)
        return path

    def plan(self):
        self.write_manifest()
        return plan_migration(manifest_path=self.manifest_path, incoming_root=self.incoming_root)

    def test_plan_moves_flat_processed_files_to_round_one_preserving_names(self) -> None:
        source = self.write_flat_processed("legacy_name.png", b"image")
        _, migrations = self.plan()
        move = migrations[0].moves[0]
        self.assertEqual(move.source, source)
        self.assertEqual(move.destination, source.parent / "1" / "legacy_name.png")

    def test_plan_moves_adjacent_meta_with_image(self) -> None:
        source = self.write_flat_processed("legacy.png", b"image")
        meta = source.with_name(source.name + ".meta")
        meta.write_bytes(b"guid")
        _, migrations = self.plan()
        destinations = {move.destination.name for move in migrations[0].moves}
        self.assertEqual(destinations, {"legacy.png", "legacy.png.meta"})

    def test_plan_blocks_when_flat_files_and_round_one_both_exist(self) -> None:
        self.write_flat_processed("legacy.png", b"image")
        (self.workspace / "processed" / "1").mkdir()
        with self.assertRaisesRegex(ValueError, "round 1 already exists"):
            self.plan()

    def test_plan_rewrites_selected_path_only_when_it_points_to_moved_file(self) -> None:
        source = self.write_flat_processed("legacy.png", b"image")
        before_hash = hashlib.sha256(source.read_bytes()).hexdigest()
        self.entry["SelectedPath"] = repo_path(source)
        _, migrations = self.plan()
        self.assertEqual(migrations[0].selected_path_after, repo_path(source.parent / "1" / "legacy.png"))
        self.assertEqual(migrations[0].moves[0].sha256, before_hash)

    def test_dry_run_writes_nothing(self) -> None:
        source = self.write_flat_processed("legacy.png", b"image")
        _, migrations = self.plan()
        self.assertTrue(source.exists())
        self.assertFalse((source.parent / "1").exists())
        self.assertEqual(len(migrations), 1)

    def test_existing_failed_production_decision_marks_round_failed(self) -> None:
        self.write_flat_processed("legacy.png", b"image")
        (self.workspace / "production_decision.json").write_text(
            json.dumps(
                {
                    "state": "decision_required",
                    "hard_gate": {"status": "failed", "code": "transparent_background_contract_failed"},
                }
            ),
            encoding="utf-8",
        )
        _, migrations = self.plan()
        self.assertEqual(migrations[0].round_state, "failed")


if __name__ == "__main__":
    unittest.main()
