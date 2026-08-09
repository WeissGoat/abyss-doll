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
from art_catalog_integrity import recompute_catalog_summary  # noqa: E402
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
                    "OutputPath": "UnityClient/Assets/Art/Approved/UI/ui_button_primary.png",
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

    def _publish_revision(self, *, tags_ready: bool = False) -> tuple[dict, dict]:
        catalog = copy.deepcopy(self.catalog)
        manifest = copy.deepcopy(self.compiled_manifest)
        request = catalog["Requests"][0]
        hard_ids = []
        for values in request["PromptAuthoringContext"]["HardConstraints"].values():
            for item in values:
                hard_ids.append(item["ID"])
        mapping = {constraint_id: [f"covered:{constraint_id}"] for constraint_id in hard_ids}
        tags = {
            "Format": "danbooru_tags_v2",
            "Status": "ready",
            "PositiveTags": [{"Tag": "button", "Weight": 1.0}],
            "NegativeTags": [{"Tag": "text", "Weight": 1.0}],
            "ReferenceControls": {},
            "ConstraintMapping": copy.deepcopy(mapping),
        }
        if not tags_ready:
            tags = {
                "Format": "danbooru_tags_v2",
                "Status": "unsupported",
                "UnsupportedReason": ["No reliable tag-only route for this deterministic UI skin."],
            }
        revision = {
            "PromptRevisionID": f"{request['RequestID']}/prompt-001",
            "RequirementFingerprint": request["RequirementFingerprint"],
            "AuthoringMode": "agent_authored",
            "Status": "ready",
            "CommonStrategy": {"Operation": "standard_asset_generation"},
            "Variants": {
                "natural_language_v2": {
                    "Format": "natural_language_v2",
                    "Status": "ready",
                    "Positive": "Create a restrained crimson primary action button skin.",
                    "Negative": "text, blue-white wash",
                    "OutputContract": {},
                    "ConstraintMapping": copy.deepcopy(mapping),
                },
                "danbooru_tags_v2": tags,
            },
            "AuthorNotes": [],
            "CreatedAt": "2026-07-26T12:00:00+08:00",
        }
        catalog["Requests"][0] = publish_prompt_revision(request, revision, activate=True)
        catalog["Summary"] = recompute_catalog_summary(catalog["Requests"])
        pointer = manifest["Entries"][0]["CompiledRequest"]
        pointer["PromptAuthoringStatus"] = "prompt_ready"
        pointer["ActivePromptRevisionID"] = revision["PromptRevisionID"]
        return catalog, manifest

    def test_valid_authoring_required_catalog_passes(self) -> None:
        self.assertEqual(validate_request_catalog(self.catalog, self.compiled_manifest, strict=True), [])

    def test_valid_active_revision_passes(self) -> None:
        catalog, manifest = self._publish_revision()

        self.assertEqual(validate_request_catalog(catalog, manifest, strict=True), [])

    def test_missing_request_is_reported(self) -> None:
        manifest = copy.deepcopy(self.compiled_manifest)
        manifest["Entries"][0]["CompiledRequest"]["RequestID"] = "missing"
        errors = validate_request_catalog(self.catalog, manifest, strict=True)

        self.assertIn("compiled_request_missing:ui_button_primary", errors)

    def test_requirement_fingerprint_mismatch_is_reported(self) -> None:
        catalog = copy.deepcopy(self.catalog)
        catalog["Requests"][0]["RequirementFingerprint"] = "bad"
        errors = validate_request_catalog(catalog, self.compiled_manifest, strict=True)

        self.assertIn("compiled_request_fingerprint_mismatch:ui_button_primary", errors)
        self.assertIn("compiled_request_payload_hash_mismatch:ui_button_primary", errors)

    def test_incomplete_constraint_mapping_is_reported(self) -> None:
        catalog, manifest = self._publish_revision()
        revision = catalog["Requests"][0]["PromptRevisions"][0]
        missing_id = next(iter(revision["Variants"]["natural_language_v2"]["ConstraintMapping"]))
        revision["Variants"]["natural_language_v2"]["ConstraintMapping"].pop(missing_id)
        revision.pop("RevisionFingerprint", None)
        errors = validate_request_catalog(catalog, manifest, strict=True)

        self.assertIn(
            f"constraint_mapping_incomplete:natural_language_v2:{missing_id}:ui_button_primary",
            errors,
        )

    def test_invalid_tag_weight_is_reported(self) -> None:
        catalog, manifest = self._publish_revision(tags_ready=True)
        revision = catalog["Requests"][0]["PromptRevisions"][0]
        revision["Variants"]["danbooru_tags_v2"]["PositiveTags"][0]["Weight"] = 0
        revision.pop("RevisionFingerprint", None)
        errors = validate_request_catalog(catalog, manifest, strict=True)

        self.assertIn(
            "prompt_tag_weight_invalid:danbooru_tags_v2:PositiveTags:0:ui_button_primary",
            errors,
        )

    def test_stale_request_is_reported(self) -> None:
        manifest = copy.deepcopy(self.compiled_manifest)
        manifest["Entries"][0]["VisualIntent"]["AppearanceEN"] = ["changed semantic"]
        errors = validate_request_catalog(self.catalog, manifest, strict=True)

        self.assertIn("compiled_request_manifest_fingerprint_mismatch", errors)
        self.assertIn("compiled_request_stale:ui_button_primary", errors)

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

    def test_stale_catalog_summary_is_reported(self) -> None:
        catalog = copy.deepcopy(self.catalog)
        catalog["Summary"]["PromptReady"] = 99

        errors = validate_request_catalog(catalog, self.compiled_manifest, strict=True)

        self.assertIn("catalog_summary_stale", errors)

    def test_duplicate_manifest_ids_and_paths_are_reported(self) -> None:
        manifest = copy.deepcopy(self.compiled_manifest)
        duplicate = copy.deepcopy(manifest["Entries"][0])
        manifest["Entries"].append(duplicate)

        errors = validate_request_catalog(self.catalog, manifest, strict=True)

        self.assertIn("visual_id_duplicate:ui_button_primary", errors)
        self.assertIn(
            "output_path_duplicate:" + duplicate.get("OutputPath", ""),
            errors,
        )

    def test_active_missing_prompt_revision_is_rejected(self) -> None:
        catalog = copy.deepcopy(self.catalog)
        manifest = copy.deepcopy(self.compiled_manifest)
        request = catalog["Requests"][0]
        request["PromptAuthoringStatus"] = "prompt_ready"
        request["ActivePromptRevisionID"] = "missing-revision"
        pointer = manifest["Entries"][0]["CompiledRequest"]
        pointer["PromptAuthoringStatus"] = "prompt_ready"
        pointer["ActivePromptRevisionID"] = "missing-revision"
        errors = validate_request_catalog(catalog, manifest, strict=True)

        self.assertIn("prompt_revision_missing:ui_button_primary:missing-revision", errors)


if __name__ == "__main__":
    unittest.main()
