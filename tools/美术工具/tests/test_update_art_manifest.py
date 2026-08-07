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

from art_catalog_integrity import CatalogIntegrityError, canonicalize_manifest_entries  # noqa: E402
from update_art_manifest import (  # noqa: E402
    add_preset_assets,
    merge_seed_asset_sets,
    scan_items,
    validate_seed_asset_sets,
)


class UpdateArtManifestProfileTests(unittest.TestCase):
    def test_seed_asset_sets_replace_legacy_values_and_preserve_latest_review(self) -> None:
        seed_sets = {
            "demo_portraits": {
                "ProductionProfile": "character_portrait_set",
                "StyleRef": {"Profile": "character_portrait_v1"},
                "IdentitySources": ["character.md"],
                "IdentityContract": {
                    "Version": 1,
                    "Required": ["silver loose hair"],
                    "Forbidden": ["tied hair"],
                    "Conditional": ["red glow only when the target state requires it"],
                },
                "ConsistencyRules": ["preserve character identity across presentation groups"],
            }
        }
        existing_sets = {
            "demo_portraits": {
                "IdentityLocks": ["legacy compiler-owned value"],
                "LatestConsistencyReview": {
                    "State": "passed",
                    "SetSnapshotFingerprint": "old-fingerprint",
                    "ProductionRunID": "old-run",
                    "EvidencePath": "old/review.json",
                },
            }
        }

        merged = merge_seed_asset_sets(validate_seed_asset_sets(seed_sets), existing_sets)

        self.assertEqual(merged["demo_portraits"]["IdentityContract"], seed_sets["demo_portraits"]["IdentityContract"])
        self.assertNotIn("IdentityLocks", merged["demo_portraits"])
        self.assertEqual(
            merged["demo_portraits"]["LatestConsistencyReview"],
            existing_sets["demo_portraits"]["LatestConsistencyReview"],
        )

    def test_seed_asset_set_requires_all_identity_contract_lists(self) -> None:
        with self.assertRaisesRegex(ValueError, "asset_set_identity_contract_invalid:demo_portraits"):
            validate_seed_asset_sets(
                {
                    "demo_portraits": {
                        "IdentityContract": {"Version": 1, "Required": [], "Forbidden": []},
                    }
                }
            )

    def test_explicit_presentation_group_overrides_existing_manifest_value(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            seed_path = root / "art_requirements_seed.json"
            seed_path.write_text(
                json.dumps({"Entries": [{"VisualID": "demo_portrait", "PresentationGroup": "dialogue_standing"}]}),
                encoding="utf-8",
            )
            entries: list[dict[str, object]] = []

            add_preset_assets(root, seed_path, {"demo_portrait": {"VisualID": "demo_portrait", "PresentationGroup": "legacy"}}, entries)

            self.assertEqual(entries[0]["PresentationGroup"], "dialogue_standing")

    def test_explicit_shared_item_icon_keeps_both_requirement_sources(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            item_dir = root / "Configs" / "Items"
            item_dir.mkdir(parents=True)
            for config_id in ("gear_iron_armor", "gear_cracked_iron_armor"):
                (item_dir / f"{config_id}.json").write_text(
                    json.dumps(
                        {
                            "ConfigID": config_id,
                            "Name": config_id,
                            "ItemType": "Gear",
                            "Rarity": "Common",
                            "IconID": "item_gear_iron_armor_icon",
                        }
                    ),
                    encoding="utf-8",
                )

            entries: list[dict[str, object]] = []
            scan_items(root / "Configs", root, {}, entries)
            canonical = canonicalize_manifest_entries(entries)

            self.assertEqual(len(canonical), 1)
            self.assertEqual(canonical[0]["ConfigID"], "gear_iron_armor")
            self.assertEqual(
                [source["ConfigID"] for source in canonical[0]["RequirementSources"]],
                ["gear_iron_armor", "gear_cracked_iron_armor"],
            )

    def test_non_explicit_collision_remains_an_error(self) -> None:
        entries = [
            {
                "Domain": "item",
                "ConfigID": "item_a",
                "VisualID": "item_demo_icon",
                "OutputPath": "Assets/a.png",
                "AssetType": "icon",
                "ProductionProfile": "standard_asset",
                "RequirementSources": [],
            },
            {
                "Domain": "item",
                "ConfigID": "item_b",
                "VisualID": "item_demo_icon",
                "OutputPath": "Assets/a.png",
                "AssetType": "icon",
                "ProductionProfile": "standard_asset",
                "RequirementSources": [],
            },
        ]
        with self.assertRaisesRegex(CatalogIntegrityError, "visual_id_collision_conflict:item_demo_icon"):
            canonicalize_manifest_entries(entries)

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
