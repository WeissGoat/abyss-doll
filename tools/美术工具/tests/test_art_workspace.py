# -*- coding: utf-8 -*-
from __future__ import annotations

import sys
import tempfile
import unittest
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from art_workspace import (  # noqa: E402
    CHARACTER_PORTRAIT_PROFILE,
    STANDARD_PROFILE,
    normalize_entry_workspace_paths,
    validate_workspace_file,
    workspace_path,
)


class ArtWorkspaceTests(unittest.TestCase):
    def test_missing_profile_defaults_to_standard_assets(self) -> None:
        root = Path("incoming")
        entry = {"VisualID": "ui_icon_test"}

        self.assertEqual(
            workspace_path(root, entry),
            root / "standard_assets" / "ui_icon_test",
        )
        self.assertEqual(entry.get("ProductionProfile"), None)

    def test_character_portrait_profile_uses_portrait_workspace(self) -> None:
        root = Path("incoming")
        entry = {
            "VisualID": "doll_zero_dialogue_confused",
            "ProductionProfile": CHARACTER_PORTRAIT_PROFILE,
        }

        self.assertEqual(
            workspace_path(root, entry),
            root / "character_portraits" / "doll_zero_dialogue_confused",
        )

    def test_unknown_profile_is_rejected(self) -> None:
        with self.assertRaisesRegex(ValueError, "Unsupported ProductionProfile"):
            workspace_path(
                Path("incoming"),
                {"VisualID": "ui_icon_test", "ProductionProfile": "unknown"},
            )

    def test_missing_visual_id_is_rejected(self) -> None:
        with self.assertRaisesRegex(ValueError, "VisualID"):
            workspace_path(Path("incoming"), {"ProductionProfile": STANDARD_PROFILE})

    def test_visual_id_must_be_a_single_path_segment(self) -> None:
        invalid_values = (
            "../_legacy_runs/foo",
            "folder/asset",
            "folder\\asset",
            ".",
            "..",
            "C:\\temp\\asset",
        )

        for value in invalid_values:
            with self.subTest(value=value):
                with self.assertRaisesRegex(ValueError, "single path segment"):
                    workspace_path(Path("incoming"), {"VisualID": value})

    def test_resolver_never_falls_back_to_legacy_flat_workspace(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            (root / "ui_icon_test").mkdir()

            resolved = workspace_path(root, {"VisualID": "ui_icon_test"})

            self.assertEqual(resolved, root / "standard_assets" / "ui_icon_test")
            self.assertFalse(resolved.exists())

    def test_normalize_entry_rewrites_active_legacy_paths(self) -> None:
        entry = {
            "VisualID": "ui_icon_test",
            "RawPath": "UnityClient/Assets/Art/_IncomingAI/ui_icon_test/raw",
            "SelectedPath": "UnityClient/Assets/Art/_IncomingAI/ui_icon_test/selected/a.png",
            "CandidateRawPath": "UnityClient/Assets/Art/_IncomingAI/ui_icon_test/raw",
            "CandidateRawFiles": [
                "UnityClient/Assets/Art/_IncomingAI/ui_icon_test/raw/a.png",
                "UnityClient/Assets/Art/Approved/UI/ui_icon_test.png",
            ],
        }

        normalized = normalize_entry_workspace_paths(entry)

        self.assertEqual(normalized["ProductionProfile"], STANDARD_PROFILE)
        self.assertEqual(
            normalized["RawPath"],
            "UnityClient/Assets/Art/_IncomingAI/standard_assets/ui_icon_test/raw",
        )
        self.assertEqual(
            normalized["SelectedPath"],
            "UnityClient/Assets/Art/_IncomingAI/standard_assets/ui_icon_test/selected/a.png",
        )
        self.assertEqual(
            normalized["CandidateRawFiles"],
            [
                "UnityClient/Assets/Art/_IncomingAI/standard_assets/ui_icon_test/raw/a.png",
                "UnityClient/Assets/Art/Approved/UI/ui_icon_test.png",
            ],
        )
        self.assertNotIn("ProductionProfile", entry)

    def test_normalize_character_entry_uses_character_portrait_path(self) -> None:
        entry = {
            "VisualID": "doll_zero_dialogue_confused",
            "ProductionProfile": CHARACTER_PORTRAIT_PROFILE,
            "RawPath": "UnityClient/Assets/Art/_IncomingAI/doll_zero_dialogue_confused/raw",
        }

        normalized = normalize_entry_workspace_paths(entry)

        self.assertEqual(
            normalized["RawPath"],
            "UnityClient/Assets/Art/_IncomingAI/character_portraits/doll_zero_dialogue_confused/raw",
        )

    def test_normalize_rewrites_previous_profile_path_when_profile_changes(self) -> None:
        entry = {
            "VisualID": "doll_proto_0_stand",
            "ProductionProfile": CHARACTER_PORTRAIT_PROFILE,
            "SelectedPath": "UnityClient/Assets/Art/_IncomingAI/standard_assets/doll_proto_0_stand/selected/a.png",
        }

        normalized = normalize_entry_workspace_paths(entry)

        self.assertEqual(
            normalized["SelectedPath"],
            "UnityClient/Assets/Art/_IncomingAI/character_portraits/doll_proto_0_stand/selected/a.png",
        )

    def test_validate_workspace_file_accepts_only_files_under_allowed_directory(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            raw = root / "standard_assets" / "ui_icon_test" / "raw"
            raw.mkdir(parents=True)
            valid = raw / "candidate.png"
            valid.write_bytes(b"png")

            self.assertEqual(validate_workspace_file(valid, raw), valid.resolve())

            legacy = root / "_legacy_runs" / "candidate.png"
            legacy.parent.mkdir(parents=True)
            legacy.write_bytes(b"png")
            with self.assertRaisesRegex(ValueError, "outside resolved workspace"):
                validate_workspace_file(legacy, raw)


if __name__ == "__main__":
    unittest.main()
