# -*- coding: utf-8 -*-

from __future__ import annotations

import copy
import sys
import unittest
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))


CATALOG = {
    "Version": 1,
    "GlobalStyle": {
        "ID": "p3_global_v1",
        "PositiveEN": ["Japanese anime-inspired 2D game art"],
    },
    "Profiles": {
        "ui_v1": {"PositiveEN": ["clean hand-painted fantasy game UI"]},
        "character_portrait_v1": {
            "PositiveEN": ["full-body anime game character portrait"]
        },
    },
    "Families": {
        "ui_button_core_v1": {
            "Profile": "ui_v1",
            "PositiveEN": ["single horizontal button skin"],
            "Roles": {
                "primary": {"PositiveEN": ["crimson primary action surface"]}
            },
        }
    },
    "ContextAccents": {"none": {"PositiveEN": []}},
}


class ArtStyleCatalogTests(unittest.TestCase):
    def test_primary_role_resolves_to_crimson_semantic(self) -> None:
        from art_style_catalog import resolve_style_ref

        resolved = resolve_style_ref(
            CATALOG,
            {"Profile": "ui_v1", "Family": "ui_button_core_v1", "Role": "primary", "ContextAccent": "none"},
        )

        self.assertEqual(resolved["ResolvedLayers"][-2], "role:primary")
        self.assertEqual(resolved["Role"]["PositiveEN"], ["crimson primary action surface"])

    def test_visual_id_does_not_infer_role(self) -> None:
        from art_style_catalog import resolve_style_ref

        with self.assertRaisesRegex(ValueError, "style_role_missing"):
            resolve_style_ref(CATALOG, {"Profile": "ui_v1", "Family": "ui_button_core_v1"})

    def test_family_role_mismatch_is_blocked(self) -> None:
        from art_style_catalog import resolve_style_ref

        catalog = copy.deepcopy(CATALOG)
        catalog["Profiles"]["character_portrait_v1"]["Families"] = []
        with self.assertRaisesRegex(ValueError, "style_family_profile_mismatch"):
            resolve_style_ref(
                catalog,
                {"Profile": "character_portrait_v1", "Family": "ui_button_core_v1", "Role": "primary"},
            )

    def test_portrait_layers_include_identity_locks(self) -> None:
        from art_style_catalog import resolve_entry_layers

        entry = {
            "VisualID": "doll_zero_dialogue_confused",
            "ProductionProfile": "character_portrait_set",
            "AssetSetID": "zero_dialogue_portrait_v1",
            "SetRole": "confused_expression_difference",
            "SourceAssets": [{"AssetID": "zero_dialogue_neutral", "Role": "identity_reference"}],
            "VisualIntent": {"RequiredChanges": ["restrained confused expression"]},
        }
        asset_sets = {
            "zero_dialogue_portrait_v1": {
                "StyleRef": {"Profile": "character_portrait_v1"},
                "IdentityLocks": ["silver hair", "white blindfold"],
            }
        }

        resolved = resolve_entry_layers(entry, CATALOG, asset_sets)

        self.assertEqual(
            resolved["Profile"]["PositiveEN"],
            ["full-body anime game character portrait"],
        )
        self.assertEqual(resolved["IdentityLocks"], ["silver hair", "white blindfold"])
        self.assertEqual(resolved["SetRole"], "confused_expression_difference")

    def test_canonical_hash_is_stable_for_key_order(self) -> None:
        from art_style_catalog import sha256_json

        first = {"Version": 1, "Value": {"b": 2, "a": 1}}
        second = {"Value": {"a": 1, "b": 2}, "Version": 1}

        self.assertEqual(sha256_json(first), sha256_json(second))


if __name__ == "__main__":
    unittest.main()
