# -*- coding: utf-8 -*-

from __future__ import annotations

import copy
import sys
import unittest
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from compile_art_generation_requests import compile_manifest_requests  # noqa: E402
from validate_art_generation_requests import validate_request_catalog  # noqa: E402


class ValidateArtGenerationRequestsTests(unittest.TestCase):
    def setUp(self) -> None:
        self.manifest = {
            "Version": 1,
            "Entries": [
                {
                    "VisualID": "ui_button_primary",
                    "Domain": "ui",
                    "AssetType": "button",
                    "DisplayName": "主要行动按钮",
                    "ProductionProfile": "standard_asset",
                    "Status": "todo",
                    "Spec": {
                        "SourceSpec": {"Format": "png", "Width": 512, "Height": 160, "AlphaRequired": True},
                        "ProcessSpec": {"NineSlice": {"Enabled": True}},
                    },
                }
            ],
        }
        result = compile_manifest_requests(copy.deepcopy(self.manifest), project_root=Path.cwd())
        self.compiled_manifest = result["Manifest"]
        self.catalog = result["Catalog"]

    def test_valid_catalog_passes(self) -> None:
        self.assertEqual(validate_request_catalog(self.catalog, self.compiled_manifest, strict=True), [])

    def test_missing_request_is_reported(self) -> None:
        manifest = copy.deepcopy(self.compiled_manifest)
        manifest["Entries"][0]["CompiledRequest"]["RequestID"] = "missing"
        errors = validate_request_catalog(self.catalog, manifest, strict=True)
        self.assertIn("compiled_request_missing:ui_button_primary", errors)

    def test_fingerprint_mismatch_is_reported(self) -> None:
        catalog = copy.deepcopy(self.catalog)
        catalog["Requests"][0]["RequestFingerprint"] = "bad"
        errors = validate_request_catalog(catalog, self.compiled_manifest, strict=True)
        self.assertTrue(any(error.startswith("compiled_request_fingerprint_mismatch") for error in errors))

    def test_unsupported_variant_is_allowed_when_request_has_ready_variant(self) -> None:
        manifest = copy.deepcopy(self.manifest)
        manifest["Entries"][0]["VisualIntent"] = {
            "RequiredElements": ["unmapped impossible semantic"]
        }
        result = compile_manifest_requests(manifest, project_root=Path.cwd())
        self.assertEqual(
            result["Catalog"]["Requests"][0]["PromptVariants"]["natural_language_v1"]["CompileStatus"],
            "ready",
        )
        self.assertEqual(
            result["Catalog"]["Requests"][0]["PromptVariants"]["danbooru_tags_v1"]["CompileStatus"],
            "unsupported",
        )
        self.assertEqual(validate_request_catalog(result["Catalog"], result["Manifest"], strict=True), [])

    def test_invalid_tag_weight_is_reported(self) -> None:
        catalog = copy.deepcopy(self.catalog)
        catalog["Requests"][0]["PromptVariants"]["danbooru_tags_v1"]["PositiveTags"] = [
            {"Tag": "button", "Weight": 0}
        ]
        errors = validate_request_catalog(catalog, self.compiled_manifest, strict=True)
        self.assertIn("tag_weight_invalid:ui_button_primary", errors)

    def test_stale_request_is_reported(self) -> None:
        manifest = copy.deepcopy(self.compiled_manifest)
        manifest["Entries"][0]["VisualIntent"]["AppearanceEN"] = ["changed semantic"]
        errors = validate_request_catalog(self.catalog, manifest, strict=True)
        self.assertIn("compiled_request_manifest_fingerprint_mismatch", errors)

    def test_processing_state_changes_do_not_stale_request(self) -> None:
        manifest = copy.deepcopy(self.compiled_manifest)
        entry = manifest["Entries"][0]
        entry.update(
            {
                "Status": "registered",
                "CandidateBatchID": "formalv2_standard_stylepilot_20260725_01",
                "CandidateRawFiles": ["UnityClient/Assets/Art/_IncomingAI/standard_assets/ui_button_primary/raw/20260725_001.png"],
                "CandidateRawPath": "UnityClient/Assets/Art/_IncomingAI/standard_assets/ui_button_primary/raw",
                "ReplacementBatchID": "formalv2_standard_stylepilot_20260725_01",
                "QualityTier": "formal_ai_v2",
                "QualityUpdatedAt": "2026-07-25T00:00:00+08:00",
            }
        )
        self.assertEqual(validate_request_catalog(self.catalog, manifest, strict=True), [])


if __name__ == "__main__":
    unittest.main()
