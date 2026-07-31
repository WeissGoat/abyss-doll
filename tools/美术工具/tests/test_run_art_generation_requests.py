# -*- coding: utf-8 -*-

from __future__ import annotations

import asyncio
import copy
import hashlib
import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from art_prompt_revision import publish_prompt_revision  # noqa: E402
from compile_art_generation_requests import compile_manifest_requests  # noqa: E402
from run_art_generation import (  # noqa: E402
    GenerateRequest,
    ImageFormat,
    build_generation_record,
    resolve_reference_images,
    select_generation_request,
    serialize_provider_prompt,
    submit_generation_request,
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
            reference_images=[
                {
                    "Path": "UnityClient/Assets/Art/Approved/Dolls/reference.png",
                    "Role": "identity_reference",
                    "SHA256": "abc123",
                    "Width": 1024,
                    "Height": 1536,
                    "Mode": "RGBA",
                }
            ],
        )

        self.assertEqual(record["EvidenceMode"], "formal_v2")
        self.assertNotIn("PromptEN", record)
        self.assertNotIn("NegativePromptEN", record)
        self.assertNotIn("LegacyPromptInput", record)
        self.assertEqual(record["RequirementFingerprint"], request["RequirementFingerprint"])
        self.assertEqual(record["PromptRevisionID"], revision["PromptRevisionID"])
        self.assertEqual(record["PromptRevisionFingerprint"], revision["RevisionFingerprint"])
        self.assertEqual(record["RequirementSnapshot"], request)
        self.assertEqual(record["PromptRevisionSnapshot"], revision)
        self.assertEqual(record["ProviderRequest"]["Prompt"], variant["Positive"])
        self.assertEqual(record["ReferenceImages"][0]["Role"], "identity_reference")

    def test_legacy_generation_record_is_explicitly_isolated(self) -> None:
        entry = copy.deepcopy(self.entry)
        entry["PromptEN"] = "legacy positive"
        entry["NegativePromptEN"] = "legacy negative"

        record = build_generation_record(
            entry=entry,
            batch_id="legacy-001",
            request_ids=[],
            provider="openai_images",
            model="legacy",
            requested_width=256,
            requested_height=256,
            requested_count=1,
            outputs=[],
            errors=[],
            created_at="2026-07-31T12:00:00+08:00",
        )

        self.assertEqual(record["EvidenceMode"], "legacy_unverified")
        self.assertEqual(
            record["LegacyPromptInput"],
            {
                "PromptEN": "legacy positive",
                "NegativePromptEN": "legacy negative",
            },
        )
        self.assertNotIn("PromptEN", record)
        self.assertNotIn("NegativePromptEN", record)

    def test_reference_images_select_image_to_image_without_rewriting_prompt(self) -> None:
        class FakeService:
            def __init__(self) -> None:
                self.generate_requests = []
                self.image_to_image_requests = []

            async def generate(self, request):
                self.generate_requests.append(request)
                return "generated"

            async def image_to_image(self, request):
                self.image_to_image_requests.append(request)
                return "edited"

        with tempfile.TemporaryDirectory() as temp_dir:
            reference_path = Path(temp_dir) / "reference.png"
            Image.new("RGBA", (32, 48), (10, 20, 30, 255)).save(reference_path)
            expected_hash = hashlib.sha256(reference_path.read_bytes()).hexdigest()
            records, image_bytes = resolve_reference_images(
                [str(reference_path)],
                expected_hashes=[expected_hash],
                roles=["identity_reference"],
            )

        request = GenerateRequest(
            prompt="Exact published natural-language prompt.",
            negative_prompt="Exact published negative prompt.",
            width=1024,
            height=1536,
            count=1,
            provider="gemini_chat_image",
            output_format=ImageFormat.PNG,
        )
        service = FakeService()
        result = asyncio.run(submit_generation_request(service, request, image_bytes))

        self.assertEqual(result, "edited")
        self.assertEqual(service.generate_requests, [])
        self.assertEqual(len(service.image_to_image_requests), 1)
        image_request = service.image_to_image_requests[0]
        self.assertEqual(image_request.prompt, request.prompt)
        self.assertEqual(image_request.negative_prompt, request.negative_prompt)
        self.assertEqual(image_request.images, image_bytes)
        self.assertEqual(records[0]["SHA256"], expected_hash)
        self.assertEqual(records[0]["Role"], "identity_reference")

    def test_no_reference_images_keep_generate_route(self) -> None:
        class FakeService:
            def __init__(self) -> None:
                self.generate_requests = []
                self.image_to_image_requests = []

            async def generate(self, request):
                self.generate_requests.append(request)
                return "generated"

            async def image_to_image(self, request):
                self.image_to_image_requests.append(request)
                return "edited"

        request = GenerateRequest(prompt="Exact prompt.", count=1)
        service = FakeService()
        result = asyncio.run(submit_generation_request(service, request, []))

        self.assertEqual(result, "generated")
        self.assertEqual(service.generate_requests, [request])
        self.assertEqual(service.image_to_image_requests, [])

    def test_changed_reference_hash_fails_before_provider_call(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            reference_path = Path(temp_dir) / "reference.png"
            Image.new("RGBA", (16, 16), (1, 2, 3, 255)).save(reference_path)
            expected_hash = hashlib.sha256(reference_path.read_bytes()).hexdigest()
            Image.new("RGBA", (16, 16), (4, 5, 6, 255)).save(reference_path)

            with self.assertRaisesRegex(ValueError, "reference_image_hash_mismatch"):
                resolve_reference_images(
                    [str(reference_path)],
                    expected_hashes=[expected_hash],
                    roles=["identity_reference"],
                )


if __name__ == "__main__":
    unittest.main()
