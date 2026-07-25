# -*- coding: utf-8 -*-

from __future__ import annotations

import json
import sys
import tempfile
import unittest
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from update_art_manifest import add_preset_assets  # noqa: E402


class UpdateArtManifestProfileTests(unittest.TestCase):
    def test_explicit_preset_profile_and_set_fields_override_existing_manifest_values(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            seed_path = root / "art_requirements_seed.json"
            seed_path.write_text(
                json.dumps(
                    {
                        "Entries": [
                            {
                                "VisualID": "doll_zero_dialogue_confused",
                                "ProductionProfile": "character_portrait_set",
                                "AssetSetID": "zero_dialogue_portrait_v1",
                                "AssetID": "zero_dialogue_confused",
                                "SetRole": "expression_difference",
                                "SourceAssets": [],
                            }
                        ]
                    }
                ),
                encoding="utf-8",
            )
            existing = {
                "VisualID": "doll_zero_dialogue_confused",
                "ProductionProfile": "standard_asset",
                "AssetSetID": "old_set",
                "AssetID": "old_asset",
                "SetRole": "old_role",
                "SourceAssets": [{"asset_id": "old_source"}],
                "Status": "todo",
            }
            entries: list[dict[str, object]] = []

            add_preset_assets(
                root,
                seed_path,
                {existing["VisualID"]: existing},
                entries,
            )

            self.assertEqual(entries[0]["ProductionProfile"], "character_portrait_set")
            self.assertEqual(entries[0]["AssetSetID"], "zero_dialogue_portrait_v1")
            self.assertEqual(entries[0]["AssetID"], "zero_dialogue_confused")
            self.assertEqual(entries[0]["SetRole"], "expression_difference")
            self.assertEqual(entries[0]["SourceAssets"], [])

    def test_omitted_preset_profile_preserves_existing_character_profile(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            seed_path = root / "art_requirements_seed.json"
            seed_path.write_text(
                json.dumps({"Entries": [{"VisualID": "doll_zero_dialogue_confused"}]}),
                encoding="utf-8",
            )
            existing = {
                "VisualID": "doll_zero_dialogue_confused",
                "ProductionProfile": "character_portrait_set",
                "Status": "todo",
            }
            entries: list[dict[str, object]] = []

            add_preset_assets(
                root,
                seed_path,
                {existing["VisualID"]: existing},
                entries,
            )

            self.assertEqual(entries[0]["ProductionProfile"], "character_portrait_set")


if __name__ == "__main__":
    unittest.main()
