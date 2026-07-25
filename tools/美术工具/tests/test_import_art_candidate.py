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

from import_art_candidate import import_candidate  # noqa: E402


def write_manifest(path: Path, entries: list[dict[str, object]]) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps({"Entries": entries}, ensure_ascii=False), encoding="utf-8")


def write_image(path: Path) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    Image.new("RGB", (32, 48), "white").save(path, format="PNG")


class ImportArtCandidateTests(unittest.TestCase):
    def test_import_uses_character_profile_workspace_and_updates_only_target_entry(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            manifest_path = root / "manifest.json"
            incoming_root = root / "_IncomingAI"
            source_path = root / "source.png"
            write_image(source_path)
            entries = [
                {
                    "VisualID": "doll_zero_dialogue_neutral",
                    "ProductionProfile": "character_portrait_set",
                    "Status": "prompted",
                    "BatchID": "",
                    "RawPath": "",
                    "SelectedPath": "",
                    "ApprovedPath": "",
                    "Notes": "",
                },
                {
                    "VisualID": "ui_icon_test",
                    "ProductionProfile": "standard_asset",
                    "Status": "approved",
                },
            ]
            write_manifest(manifest_path, entries)

            result = import_candidate(
                manifest_path=manifest_path,
                incoming_root=incoming_root,
                visual_id="doll_zero_dialogue_neutral",
                source_path=source_path,
                destination_name="r01_001.png",
                batch_id="imported_zero_dialogue_neutral_20260718_01",
                source_review="selection_review.md",
            )

            destination = (
                incoming_root
                / "character_portraits"
                / "doll_zero_dialogue_neutral"
                / "raw"
                / "r01_001.png"
            )
            self.assertEqual(Path(result["destination"]), destination.resolve())
            self.assertTrue(destination.exists())
            self.assertEqual(
                hashlib.sha256(source_path.read_bytes()).hexdigest(),
                hashlib.sha256(destination.read_bytes()).hexdigest(),
            )
            updated = json.loads(manifest_path.read_text(encoding="utf-8"))
            target = updated["Entries"][0]
            self.assertEqual(target["Status"], "generated")
            self.assertEqual(target["BatchID"], "imported_zero_dialogue_neutral_20260718_01")
            self.assertEqual(
                target["RawPath"],
                str(destination.parent.resolve()).replace("\\", "/"),
            )
            self.assertEqual(target["SelectedPath"], "")
            self.assertEqual(target["ApprovedPath"], "")
            self.assertEqual(updated["Entries"][1], entries[1])

    def test_import_rejects_missing_or_duplicate_visual_id(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            source_path = root / "source.png"
            write_image(source_path)
            for entries in ([], [{"VisualID": "duplicate"}, {"VisualID": "duplicate"}]):
                manifest_path = root / f"manifest_{len(entries)}.json"
                write_manifest(manifest_path, entries)
                with self.assertRaisesRegex(ValueError, "exactly one Manifest entry"):
                    import_candidate(
                        manifest_path=manifest_path,
                        incoming_root=root / "_IncomingAI",
                        visual_id="duplicate",
                        source_path=source_path,
                        destination_name="r01_001.png",
                        batch_id="batch",
                        source_review="review.md",
                    )

    def test_import_rejects_source_inside_legacy_runs(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            manifest_path = root / "manifest.json"
            incoming_root = root / "_IncomingAI"
            source_path = incoming_root / "_legacy_runs" / "old.png"
            write_image(source_path)
            write_manifest(
                manifest_path,
                [{"VisualID": "portrait", "ProductionProfile": "character_portrait_set"}],
            )

            with self.assertRaisesRegex(ValueError, "_legacy_runs"):
                import_candidate(
                    manifest_path=manifest_path,
                    incoming_root=incoming_root,
                    visual_id="portrait",
                    source_path=source_path,
                    destination_name="r01_001.png",
                    batch_id="batch",
                    source_review="review.md",
                )

    def test_dry_run_writes_nothing(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            manifest_path = root / "manifest.json"
            incoming_root = root / "_IncomingAI"
            source_path = root / "source.png"
            write_image(source_path)
            write_manifest(
                manifest_path,
                [{"VisualID": "portrait", "ProductionProfile": "character_portrait_set"}],
            )
            before = manifest_path.read_bytes()

            result = import_candidate(
                manifest_path=manifest_path,
                incoming_root=incoming_root,
                visual_id="portrait",
                source_path=source_path,
                destination_name="r01_001.png",
                batch_id="batch",
                source_review="review.md",
                dry_run=True,
            )

            self.assertTrue(result["dry_run"])
            self.assertEqual(manifest_path.read_bytes(), before)
            self.assertFalse(incoming_root.exists())


if __name__ == "__main__":
    unittest.main()
