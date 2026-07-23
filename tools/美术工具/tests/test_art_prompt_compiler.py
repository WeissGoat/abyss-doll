# -*- coding: utf-8 -*-

from __future__ import annotations

import copy
import sys
import unittest
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from art_prompt_compiler import (  # noqa: E402
    DANBOORU_TAGS_FORMAT,
    NATURAL_LANGUAGE_FORMAT,
    build_canonical_visual_brief,
    compile_generation_request,
    serialize_danbooru_tags,
    serialize_natural_language,
)
from art_style_catalog import DEFAULT_CATALOG, build_catalog_snapshot  # noqa: E402


class ArtPromptCompilerTests(unittest.TestCase):
    def setUp(self) -> None:
        self.catalog = build_catalog_snapshot(Path.cwd(), {"ArtStyleCatalog": copy.deepcopy(DEFAULT_CATALOG)})
        self.button_entry = {
            "VisualID": "ui_button_primary",
            "ProductionProfile": "standard_asset",
            "AssetType": "button",
            "StyleRef": {
                "Profile": "ui_v1",
                "Family": "ui_button_core_v1",
                "Role": "primary",
                "ContextAccent": "none",
            },
            "VisualIntent": {
                "SubjectEN": "single horizontal button skin",
                "AppearanceEN": "crimson primary action surface",
                "CompositionEN": "clean stretchable center",
                "RequiredElements": ["continuous outer frame"],
                "ForbiddenElements": ["baked text"],
            },
            "Spec": {
                "SourceSpec": {"Format": "png", "Width": 512, "Height": 160, "AlphaRequired": True},
                "ProcessSpec": {"NineSlice": {"Enabled": True}},
            },
        }

    def test_button_compiles_natural_language_and_tags(self) -> None:
        request = compile_generation_request(self.button_entry, self.catalog, {})

        self.assertEqual(request["CompileStatus"], "ready")
        self.assertEqual(
            request["PromptVariants"][NATURAL_LANGUAGE_FORMAT]["CompileStatus"],
            "ready",
        )
        tags = request["PromptVariants"][DANBOORU_TAGS_FORMAT]
        self.assertIn({"Tag": "crimson", "Weight": 1.1}, tags["PositiveTags"])
        self.assertIn("nine_slice_safe_frame", request["TechnicalRequest"]["CapabilityRequirements"])

    def test_portrait_preserve_and_required_changes_are_structured(self) -> None:
        entry = {
            "VisualID": "doll_zero_dialogue_confused",
            "ProductionProfile": "character_portrait_set",
            "AssetSetID": "zero_dialogue_portrait_v1",
            "SetRole": "confused_expression_difference",
            "SourceAssets": [{"AssetID": "zero_dialogue_neutral", "Role": "identity_reference"}],
            "VisualIntent": {
                "SubjectEN": "full-body anime game character portrait",
                "Preserve": ["silver hair", "white blindfold"],
                "RequiredChanges": ["restrained confused expression"],
            },
            "Spec": {"SourceSpec": {"Format": "png", "Width": 1024, "Height": 1536, "AlphaRequired": True}},
        }
        asset_sets = {
            "zero_dialogue_portrait_v1": {
                "StyleRef": {"Profile": "character_portrait_v1"},
                "IdentityLocks": ["silver hair", "white blindfold"],
                "IdentitySources": ["character.md"],
            }
        }

        request = compile_generation_request(entry, self.catalog, asset_sets)

        self.assertEqual(request["PreservationContract"]["RequiredChanges"], ["restrained confused expression"])
        self.assertIn("silver hair", request["PreservationContract"]["Preserve"])
        self.assertEqual(request["TechnicalRequest"]["ReferenceAssets"], entry["SourceAssets"])

    def test_tag_weights_are_structured_not_provider_syntax(self) -> None:
        variant = serialize_danbooru_tags(
            {"SemanticUnits": [{"Text": "silver hair", "Section": "Appearance", "Required": True}]}
        )

        self.assertEqual(variant["PositiveTags"], [{"Tag": "silver hair", "Weight": 1.2}])
        self.assertNotIn("::", str(variant))

    def test_unsupported_semantic_marks_variant_not_ready(self) -> None:
        variant = serialize_danbooru_tags(
            {"SemanticUnits": [{"Text": "unmapped impossible semantic", "Section": "Required", "Required": True}]}
        )

        self.assertEqual(variant["CompileStatus"], "unsupported")
        self.assertEqual(variant["SemanticCoverage"]["Unmapped"], ["unmapped impossible semantic"])

    def test_same_input_produces_same_variant_fingerprint(self) -> None:
        first = compile_generation_request(self.button_entry, self.catalog, {})
        second = compile_generation_request(copy.deepcopy(self.button_entry), copy.deepcopy(self.catalog), {})

        self.assertEqual(first["RequestID"], second["RequestID"])
        self.assertEqual(first["RequestFingerprint"], second["RequestFingerprint"])


if __name__ == "__main__":
    unittest.main()
