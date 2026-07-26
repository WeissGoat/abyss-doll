# -*- coding: utf-8 -*-

from __future__ import annotations

import hashlib
import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from portrait_reference_resolver import resolve_portrait_references  # noqa: E402


def write_image(path: Path, *, size: tuple[int, int] = (32, 48), mode: str = "RGBA") -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    color = (220, 220, 220, 180) if mode == "RGBA" else (220, 220, 220)
    Image.new(mode, size, color).save(path, format="PNG")


def source_entry(**overrides: object) -> dict[str, object]:
    entry: dict[str, object] = {
        "VisualID": "doll_zero_dialogue_neutral",
        "ProductionProfile": "character_portrait_set",
        "AssetSetID": "zero_dialogue_portrait_v1",
        "AssetID": "zero_dialogue_neutral",
        "Status": "selected",
        "OutputPath": "UnityClient/Assets/Art/Approved/Dolls/doll_zero_dialogue_neutral.png",
        "ApprovedPath": "",
        "SelectedPath": "",
        "SourceAssets": [],
    }
    entry.update(overrides)
    return entry


def target_entry(**overrides: object) -> dict[str, object]:
    entry: dict[str, object] = {
        "VisualID": "doll_zero_cold",
        "ProductionProfile": "character_portrait_set",
        "AssetSetID": "zero_dialogue_portrait_v1",
        "AssetID": "zero_cold",
        "SourceAssets": [
            {"asset_id": "zero_dialogue_neutral", "role": "identity_and_pose_reference"}
        ],
    }
    entry.update(overrides)
    return entry


class PortraitReferenceResolverTests(unittest.TestCase):
    def test_approved_path_has_priority_over_selected(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            approved = root / "UnityClient/Assets/Art/Approved/Dolls/doll_zero_dialogue_neutral.png"
            selected = root / "UnityClient/Assets/Art/_IncomingAI/character_portraits/doll_zero_dialogue_neutral/selected/001.png"
            write_image(approved, size=(1024, 1536))
            write_image(selected, size=(512, 768))
            source = source_entry(
                Status="approved",
                ApprovedPath=str(approved.relative_to(root)).replace("\\", "/"),
                SelectedPath=str(selected.relative_to(root)).replace("\\", "/"),
            )

            result = resolve_portrait_references(
                {"Entries": [source, target_entry()]}, target_entry(), root
            )

            self.assertEqual(len(result), 1)
            self.assertEqual(result[0]["State"], "approved")
            self.assertEqual(result[0]["Path"], str(approved.relative_to(root)).replace("\\", "/"))
            self.assertEqual(result[0]["Width"], 1024)
            self.assertEqual(result[0]["Height"], 1536)
            self.assertEqual(result[0]["Mode"], "RGBA")
            self.assertEqual(result[0]["SHA256"], hashlib.sha256(approved.read_bytes()).hexdigest())

    def test_manifest_selected_path_is_used_when_approved_is_unavailable(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            selected = root / "custom/neutral-selected.png"
            write_image(selected)
            source = source_entry(SelectedPath="custom/neutral-selected.png")

            result = resolve_portrait_references(
                {"Entries": [source, target_entry()]}, target_entry(), root
            )

            self.assertEqual(result[0]["State"], "selected")
            self.assertEqual(result[0]["Path"], "custom/neutral-selected.png")

    def test_unique_workspace_selected_is_used_as_final_fallback(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            selected = root / "UnityClient/Assets/Art/_IncomingAI/character_portraits/doll_zero_dialogue_neutral/selected/001.png"
            write_image(selected)

            result = resolve_portrait_references(
                {"Entries": [source_entry(), target_entry()]}, target_entry(), root
            )

            self.assertEqual(result[0]["State"], "selected")
            self.assertEqual(result[0]["Path"], str(selected.relative_to(root)).replace("\\", "/"))

    def test_no_source_assets_returns_empty_list(self) -> None:
        self.assertEqual(
            resolve_portrait_references({"Entries": [source_entry()]}, target_entry(SourceAssets=[]), Path.cwd()),
            [],
        )

    def test_missing_or_duplicate_source_asset_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            with self.assertRaisesRegex(ValueError, "portrait_reference_asset_missing:zero_dialogue_neutral"):
                resolve_portrait_references({"Entries": [target_entry()]}, target_entry(), root)

            duplicate = source_entry(VisualID="duplicate_neutral")
            with self.assertRaisesRegex(ValueError, "portrait_reference_asset_duplicate:zero_dialogue_neutral"):
                resolve_portrait_references(
                    {"Entries": [source_entry(), duplicate, target_entry()]}, target_entry(), root
                )

    def test_missing_or_corrupt_selected_file_is_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            missing = source_entry(SelectedPath="missing.png")
            with self.assertRaisesRegex(ValueError, "portrait_reference_file_missing"):
                resolve_portrait_references({"Entries": [missing, target_entry()]}, target_entry(), root)

            corrupt_path = root / "corrupt.png"
            corrupt_path.write_bytes(b"not an image")
            corrupt = source_entry(SelectedPath="corrupt.png")
            with self.assertRaisesRegex(ValueError, "portrait_reference_image_invalid"):
                resolve_portrait_references({"Entries": [corrupt, target_entry()]}, target_entry(), root)

    def test_multiple_workspace_selected_files_are_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            selected_dir = root / "UnityClient/Assets/Art/_IncomingAI/character_portraits/doll_zero_dialogue_neutral/selected"
            write_image(selected_dir / "001.png")
            write_image(selected_dir / "002.png")

            with self.assertRaisesRegex(ValueError, "portrait_reference_selected_ambiguous"):
                resolve_portrait_references(
                    {"Entries": [source_entry(), target_entry()]}, target_entry(), root
                )

    def test_legacy_or_outside_project_paths_are_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir, tempfile.TemporaryDirectory() as outside_dir:
            root = Path(temp_dir)
            legacy = root / "UnityClient/Assets/Art/_IncomingAI/_legacy_runs/neutral.png"
            write_image(legacy)
            with self.assertRaisesRegex(ValueError, "portrait_reference_legacy_path_forbidden"):
                resolve_portrait_references(
                    {"Entries": [source_entry(SelectedPath=str(legacy.relative_to(root))), target_entry()]},
                    target_entry(),
                    root,
                )

            outside = Path(outside_dir) / "outside.png"
            write_image(outside)
            with self.assertRaisesRegex(ValueError, "portrait_reference_outside_project"):
                resolve_portrait_references(
                    {"Entries": [source_entry(SelectedPath=str(outside)), target_entry()]},
                    target_entry(),
                    root,
                )


if __name__ == "__main__":
    unittest.main()
