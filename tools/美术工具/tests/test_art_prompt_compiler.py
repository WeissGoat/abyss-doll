# -*- coding: utf-8 -*-

from __future__ import annotations

import copy
import json
import sys
import unittest
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from art_prompt_compiler import (  # noqa: E402
    build_prompt_authoring_context,
    compile_requirement_request,
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

    def test_button_compiles_authoring_context_without_executable_prompt(self) -> None:
        request = compile_requirement_request(self.button_entry, self.catalog, {})

        self.assertEqual(request["RequirementStatus"], "ready")
        self.assertEqual(request["PromptAuthoringStatus"], "prompt_authoring_required")
        self.assertNotIn('"Positive"', json.dumps(request, ensure_ascii=False))
        guidance = request["PromptAuthoringContext"]["Guidance"]
        self.assertTrue(any(item["Text"] == "crimson primary action surface" for item in guidance["Appearance"]))
        self.assertIn("nine_slice_safe_frame", request["TechnicalRequest"]["CapabilityRequirements"])

    def test_portrait_preserve_and_required_changes_are_structured(self) -> None:
        entry = {
            "VisualID": "doll_zero_dialogue_confused",
            "ProductionProfile": "character_portrait_set",
            "AssetSetID": "zero_dialogue_portrait_v1",
            "PresentationGroup": "dialogue_standing",
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
                "IdentitySources": ["character.md"],
                "ProductionProfile": "character_portrait_set",
                "IdentityContract": {
                    "Version": 1,
                    "Required": ["silver loose hair", "white cloth blindfold"],
                    "Forbidden": ["tied hair", "black blindfold"],
                    "Conditional": ["red glow only when required by the target state"],
                },
            }
        }

        request = compile_requirement_request(entry, self.catalog, asset_sets)

        self.assertEqual(
            [item["Text"] for item in request["PreservationContract"]["RequiredChanges"]],
            ["restrained confused expression"],
        )
        self.assertIn("silver loose hair", [item["Text"] for item in request["PreservationContract"]["Preserve"]])
        self.assertEqual(request["TechnicalRequest"]["ReferenceAssets"], entry["SourceAssets"])
        hard = request["PromptAuthoringContext"]["HardConstraints"]
        self.assertTrue(any(item["Text"] == "white blindfold" for item in hard["Identity"]))
        self.assertTrue(any(item["Text"] == "restrained confused expression" for item in hard["RequiredChanges"]))
        self.assertEqual([item["ID"] for item in hard["Conditional"]], ["brief:conditional:0"])
        self.assertEqual(request["PromptAuthoringContext"]["PresentationGroup"], "dialogue_standing")

    def test_context_contains_stable_constraint_ids(self) -> None:
        context = build_prompt_authoring_context(self.button_entry, self.catalog, {})
        required = context["HardConstraints"]["Required"]

        self.assertEqual(required[0]["ID"], "brief:required:0")
        self.assertEqual(required[0]["Text"], "continuous outer frame")

    def test_same_input_produces_same_requirement_fingerprint(self) -> None:
        first = compile_requirement_request(self.button_entry, self.catalog, {})
        second = compile_requirement_request(copy.deepcopy(self.button_entry), copy.deepcopy(self.catalog), {})

        self.assertEqual(first["RequestID"], second["RequestID"])
        self.assertEqual(first["RequirementFingerprint"], second["RequirementFingerprint"])


if __name__ == "__main__":
    unittest.main()
