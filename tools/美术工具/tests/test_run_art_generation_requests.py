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
from run_art_generation import select_compiled_request, serialize_provider_prompt  # noqa: E402


class RunArtGenerationRequestTests(unittest.TestCase):
    def setUp(self) -> None:
        manifest = {
            "Version": 1,
            "Entries": [
                {
                    "VisualID": "ui_button_primary",
                    "Domain": "ui",
                    "AssetType": "button",
                    "DisplayName": "主要行动按钮",
                    "ProductionProfile": "standard_asset",
                    "Status": "prompted",
                    "Spec": {
                        "SourceSpec": {"Format": "png", "Width": 512, "Height": 160, "AlphaRequired": True},
                        "ProcessSpec": {"NineSlice": {"Enabled": True}},
                    },
                }
            ],
        }
        compiled = compile_manifest_requests(copy.deepcopy(manifest), project_root=Path.cwd())
        self.entry = compiled["Manifest"]["Entries"][0]
        self.catalog = compiled["Catalog"]

    def test_explicit_natural_language_variant_is_selected(self) -> None:
        request, prompt_format = select_compiled_request(
            self.catalog,
            self.entry,
            prompt_format="natural_language_v1",
        )

        self.assertEqual(prompt_format, "natural_language_v1")
        self.assertEqual(request["RequestID"], self.entry["CompiledRequest"]["RequestID"])

    def test_tags_are_serialized_without_provider_weight_syntax(self) -> None:
        request, _ = select_compiled_request(self.catalog, self.entry, prompt_format="danbooru_tags_v1")
        positive, negative = serialize_provider_prompt(request["PromptVariants"]["danbooru_tags_v1"], "danbooru_tags_v1")

        self.assertIn("button", positive)
        self.assertNotIn("::", positive + negative)

    def test_stale_request_is_rejected(self) -> None:
        entry = copy.deepcopy(self.entry)
        entry["CompiledRequest"]["RequestFingerprint"] = "stale"

        with self.assertRaisesRegex(ValueError, "compiled_request_fingerprint_mismatch"):
            select_compiled_request(self.catalog, entry, prompt_format="natural_language_v1")

    def test_missing_request_is_rejected(self) -> None:
        entry = copy.deepcopy(self.entry)
        entry["CompiledRequest"]["RequestID"] = "missing"

        with self.assertRaisesRegex(ValueError, "compiled_request_missing"):
            select_compiled_request(self.catalog, entry, prompt_format="natural_language_v1")


if __name__ == "__main__":
    unittest.main()
