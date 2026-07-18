# -*- coding: utf-8 -*-

from __future__ import annotations

import sys
import tempfile
import unittest
from argparse import Namespace
from pathlib import Path
from types import SimpleNamespace

from PIL import Image


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from art_workspace import workspace_path  # noqa: E402
from optimize_art_assets import candidate_raw_images, optimize_entry  # noqa: E402


def make_args(**overrides: object) -> Namespace:
    values = {
        "candidate_batch_id": "",
        "batch_id": "",
        "domain": [],
        "visual_id": [],
        "priority": [],
        "status": "generated",
        "limit": 0,
        "contact_size": 64,
        "background_threshold": 34,
        "dry_run": False,
        "overwrite": False,
    }
    values.update(overrides)
    return Namespace(**values)


def make_entry(background_policy: str = "auto_simple") -> dict[str, object]:
    return {
        "Domain": "item",
        "ConfigID": "loot_gear_scrap",
        "AssetType": "icon",
        "VisualID": "ui_icon_test",
        "ProductionProfile": "standard_asset",
        "Status": "generated",
        "Spec": {
            "SourceSpec": {
                "Width": 32,
                "Height": 32,
                "Background": "transparent",
                "AlphaRequired": True,
            },
            "CompositionSpec": {"SafePaddingPercent": 5},
            "ProcessSpec": {
                "BackgroundPolicy": background_policy,
                "PostProcess": ["resize", "fit_safe_padding"],
            },
        },
    }


class OptimizeArtAssetsWorkspaceTests(unittest.TestCase):
    def test_candidate_raw_files_rejects_file_outside_resolved_raw_workspace(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            raw = root / "standard_assets" / "ui_icon_test" / "raw"
            raw.mkdir(parents=True)
            legacy = root / "_legacy_runs" / "old.png"
            legacy.parent.mkdir(parents=True)
            legacy.write_bytes(b"png")
            entry = {"CandidateRawFiles": [str(legacy)]}
            args = SimpleNamespace(candidate_batch_id="candidate_batch")

            with self.assertRaisesRegex(ValueError, "outside resolved workspace"):
                candidate_raw_images(entry, raw, args)


class OptimizeArtAssetsRoundTests(unittest.TestCase):
    def test_processing_publishes_numeric_round_and_never_overwrites_previous_round(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            incoming_root = Path(temp_dir) / "incoming"
            entry = make_entry()
            workspace = workspace_path(incoming_root, entry)
            workspace.joinpath("raw").mkdir(parents=True)
            Image.new("RGB", (32, 32), "white").save(workspace / "raw" / "r01_001.png")

            first = optimize_entry(entry, make_args(), incoming_root)
            self.assertTrue((workspace / "processed" / "1" / "001.png").exists())
            self.assertTrue((workspace / "processed" / "1" / "decision.json").exists())
            self.assertFalse((workspace / "processed" / "r01_001.png").exists())
            self.assertEqual(first["LatestRound"], 1)
            self.assertIn(first["LatestState"], {"passed", "failed", "decision_required"})

            second = optimize_entry(entry, make_args(overwrite=True), incoming_root)
            self.assertTrue((workspace / "processed" / "2" / "001.png").exists())
            self.assertTrue((workspace / "processed" / "1" / "001.png").exists())
            self.assertEqual(second["LatestRound"], 2)

    def test_agent_required_does_not_create_a_round(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            incoming_root = Path(temp_dir) / "incoming"
            entry = make_entry(background_policy="agent_required")
            workspace = workspace_path(incoming_root, entry)
            workspace.joinpath("raw").mkdir(parents=True)
            Image.new("RGB", (32, 32), "white").save(workspace / "raw" / "r01_001.png")

            report = optimize_entry(entry, make_args(), incoming_root)

            self.assertFalse((workspace / "processed").exists())
            self.assertEqual(report["LatestState"], "decision_required")
            self.assertEqual(report["Reason"], "agent_processing_required")


if __name__ == "__main__":
    unittest.main()
