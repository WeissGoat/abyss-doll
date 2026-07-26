# -*- coding: utf-8 -*-

from __future__ import annotations

import copy
import sys
import unittest
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from art_prompt_revision import publish_prompt_revision  # noqa: E402
from compile_art_generation_requests import compile_manifest_requests  # noqa: E402
from run_art_generation import (  # noqa: E402
    build_generation_record,
    select_generation_request,
    serialize_provider_prompt,
)


class RunArtGenerationRequestTests(unittest.TestCase):
    def setUp(self) -> None:
        manifest = {
            "Version": 1,
            "Entries": [
                {
                    "VisualID": "ui_icon_warning",
                    "Domain": "ui",
                    "AssetType": "icon",
                    "DisplayName": "危险警告图标",
                    "ProductionProfile": "standard_asset",
                    "Status": "prompted",
                    "VisualIntent": {
                        "RequiredElements": ["single warning symbol"],
                        "ForbiddenElements": ["text"],
                    },
                    "Spec": {
                        "SourceSpec": {"Format": "png", "Width": 256, "Height": 256, "AlphaRequired": True},
                    },
                }
            ],
        }
        compiled = compile_manifest_requests(copy.deepcopy(manifest), project_root=Path.cwd())
        self.entry = compiled["Manifest"]["Entries"][0]
        self.catalog = compiled["Catalog"]
        request = self.catalog["Requests"][0]
        hard_ids = [
            item["ID"]
            for values in request["PromptAuthoringContext"]["HardConstraints"].values()
            for item in values
        ]
        mapping = {constraint_id: [f"covered:{constraint_id}"] for constraint_id in hard_ids}
        self.revision = {
            "PromptRevisionID": f"{request['RequestID']}/prompt-001",
            "RequirementFingerprint": request["RequirementFingerprint"],
            "AuthoringMode": "agent_authored",
            "Status": "ready",
            "CommonStrategy": {"Operation": "standard_asset_generation"},
            "Variants": {
                "natural_language_v2": {
                    "Format": "natural_language_v2",
                    "Status": "ready",
                    "Positive": "Create one compact crimson warning icon with a readable angular silhouette.",
                    "Negative": "text, letters, blue-white palette",
                    "OutputContract": {"Background": "transparent"},
                    "ConstraintMapping": copy.deepcopy(mapping),
                },
                "danbooru_tags_v2": {
                    "Format": "danbooru_tags_v2",
                    "Status": "ready",
                    "PositiveTags": [
                        {"Tag": "warning symbol", "Weight": 1.0},
                        {"Tag": "crimson", "Weight": 1.2},
                    ],
                    "NegativeTags": [{"Tag": "text", "Weight": 1.3}],
                    "ReferenceControls": {},
                    "ConstraintMapping": copy.deepcopy(mapping),
                },
            },
            "AuthorNotes": [],
            "CreatedAt": "2026-07-26T12:00:00+08:00",
        }
        self.catalog["Requests"][0] = publish_prompt_revision(request, self.revision, activate=True)
        pointer = self.entry["CompiledRequest"]
        pointer["PromptAuthoringStatus"] = "prompt_ready"
        pointer["ActivePromptRevisionID"] = self.revision["PromptRevisionID"]

    def test_natural_language_provider_request_equals_published_variant(self) -> None:
        request, revision, prompt_format, variant = select_generation_request(
            self.catalog,
            self.entry,
            provider="gemini_chat_image",
        )

        positive, negative = serialize_provider_prompt(variant, prompt_format)
        self.assertEqual(prompt_format, "natural_language_v2")
        self.assertEqual(positive, variant["Positive"])
        self.assertEqual(negative, variant["Negative"])
        self.assertEqual(revision["PromptRevisionID"], request["ActivePromptRevisionID"])

    def test_novelai_auto_selects_tags_and_serializes_weights(self) -> None:
        _, _, prompt_format, variant = select_generation_request(
            self.catalog,
            self.entry,
            provider="novelai",
        )

        positive, negative = serialize_provider_prompt(variant, prompt_format)
        self.assertEqual(prompt_format, "danbooru_tags_v2")
        self.assertEqual(positive, "warning symbol, 1.2::crimson::")
        self.assertEqual(negative, "1.3::text::")

    def test_authoring_required_fails_before_provider_call(self) -> None:
        catalog = copy.deepcopy(self.catalog)
        request = catalog["Requests"][0]
        request["PromptRevisions"] = []
        request["ActivePromptRevisionID"] = ""
        request["PromptAuthoringStatus"] = "prompt_authoring_required"
        entry = copy.deepcopy(self.entry)
        entry["CompiledRequest"]["ActivePromptRevisionID"] = ""
        entry["CompiledRequest"]["PromptAuthoringStatus"] = "prompt_authoring_required"

        with self.assertRaisesRegex(ValueError, "prompt_authoring_required"):
            select_generation_request(catalog, entry, provider="openai_images")

    def test_stale_requirement_pointer_is_rejected(self) -> None:
        entry = copy.deepcopy(self.entry)
        entry["CompiledRequest"]["RequirementFingerprint"] = "stale"

        with self.assertRaisesRegex(ValueError, "compiled_request_fingerprint_mismatch"):
            select_generation_request(self.catalog, entry, provider="openai_images")

    def test_missing_request_is_rejected(self) -> None:
        entry = copy.deepcopy(self.entry)
        entry["CompiledRequest"]["RequestID"] = "missing"

        with self.assertRaisesRegex(ValueError, "compiled_request_missing"):
            select_generation_request(self.catalog, entry, provider="openai_images")

    def test_generation_record_preserves_requirement_and_revision_evidence(self) -> None:
        request, revision, prompt_format, variant = select_generation_request(
            self.catalog,
            self.entry,
            provider="openai_images",
        )
        positive, negative = serialize_provider_prompt(variant, prompt_format)
        record = build_generation_record(
            entry=self.entry,
            batch_id="batch-001",
            request_ids=["provider-request-001"],
            provider="openai_images",
            model="gpt-image-2",
            requested_width=256,
            requested_height=256,
            requested_count=1,
            outputs=[],
            errors=[],
            created_at="2026-07-26T12:00:00+08:00",
            requirement_request=request,
            prompt_revision=revision,
            prompt_format=prompt_format,
            provider_request={"Prompt": positive, "NegativePrompt": negative},
        )

        self.assertEqual(record["RequirementFingerprint"], request["RequirementFingerprint"])
        self.assertEqual(record["PromptRevisionID"], revision["PromptRevisionID"])
        self.assertEqual(record["PromptRevisionFingerprint"], revision["RevisionFingerprint"])
        self.assertEqual(record["RequirementSnapshot"], request)
        self.assertEqual(record["PromptRevisionSnapshot"], revision)
        self.assertEqual(record["ProviderRequest"]["Prompt"], variant["Positive"])


if __name__ == "__main__":
    unittest.main()
