# -*- coding: utf-8 -*-

from __future__ import annotations

import sys
import unittest
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from art_catalog_integrity import (  # noqa: E402
    CatalogIntegrityError,
    canonicalize_manifest_entries,
    merge_request_catalog,
    recompute_catalog_summary,
)


def source(config_id: str, visual_id: str, *, explicit: bool = True) -> dict:
    return {
        "Domain": "item",
        "SourceType": "config",
        "ConfigID": config_id,
        "ConfigSource": f"配置表(JSON)/Items/{config_id}.json",
        "DisplayName": config_id,
        "AssetType": "icon",
        "VisualID": visual_id,
        "OutputPath": f"UnityClient/Assets/Art/Approved/Items/Icons/{visual_id}.png",
        "ProductionProfile": "standard_asset",
        "Spec": {"SourceSpec": {"Format": "png", "Width": 128, "Height": 128}},
        "RequirementSources": [
            {
                "SourceType": "config",
                "ConfigID": config_id,
                "ConfigSource": f"配置表(JSON)/Items/{config_id}.json",
                "DisplayName": config_id,
                "VisualIDField": "IconID" if explicit else "derived",
                "ExplicitVisualID": explicit,
            }
        ],
    }


class ArtCatalogIntegrityTests(unittest.TestCase):
    def test_explicit_shared_icon_becomes_one_entry(self) -> None:
        entries = canonicalize_manifest_entries([
            source("gear_iron_armor", "item_gear_iron_armor_icon"),
            source("gear_cracked_iron_armor", "item_gear_iron_armor_icon"),
        ])

        self.assertEqual(len(entries), 1)
        self.assertEqual(entries[0]["ConfigID"], "gear_iron_armor")
        self.assertEqual(
            [item["ConfigID"] for item in entries[0]["RequirementSources"]],
            ["gear_iron_armor", "gear_cracked_iron_armor"],
        )
        self.assertEqual(entries[0]["VisualReusePolicy"]["Mode"], "shared_visual")

    def test_derived_duplicate_visual_id_fails(self) -> None:
        with self.assertRaisesRegex(
            CatalogIntegrityError,
            "visual_id_collision_conflict:item_demo_icon",
        ):
            canonicalize_manifest_entries([
                source("item_a", "item_demo_icon", explicit=False),
                source("item_b", "item_demo_icon", explicit=False),
            ])

    def test_summary_is_derived_from_requests(self) -> None:
        requests = [
            {"RequirementStatus": "ready", "PromptAuthoringStatus": "prompt_ready"},
            {"RequirementStatus": "ready", "PromptAuthoringStatus": "prompt_authoring_required"},
            {"RequirementStatus": "invalid", "PromptAuthoringStatus": "prompt_invalid"},
        ]

        self.assertEqual(
            recompute_catalog_summary(requests),
            {
                "Ready": 2,
                "StyleResolutionRequired": 0,
                "Unsupported": 0,
                "Invalid": 1,
                "UnchangedPublished": 0,
                "PromptAuthoringRequired": 1,
                "PromptReady": 1,
            },
        )

    def test_scoped_merge_preserves_uncompiled_requests(self) -> None:
        previous = [
            {"VisualID": "keep", "RequestID": "keep@1"},
            {"VisualID": "replace", "RequestID": "replace@1"},
        ]
        compiled = [{"VisualID": "replace", "RequestID": "replace@2"}]

        self.assertEqual(
            [item["RequestID"] for item in merge_request_catalog(previous, compiled, {"replace"})],
            ["keep@1", "replace@2"],
        )


if __name__ == "__main__":
    unittest.main()
