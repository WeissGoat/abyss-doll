# -*- coding: utf-8 -*-

from __future__ import annotations

import copy
import sys
import unittest
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from art_prompt_revision import (  # noqa: E402
    compute_revision_fingerprint,
    publish_prompt_revision,
    select_prompt_variant,
    validate_prompt_revision,
)


class ArtPromptRevisionTests(unittest.TestCase):
    def setUp(self) -> None:
        self.request = {
            "RequestID": "doll_zero_cold@abc123",
            "VisualID": "doll_zero_cold",
            "RequirementFingerprint": "requirement-fingerprint",
            "RequirementStatus": "ready",
            "PromptAuthoringStatus": "prompt_authoring_required",
            "PromptAuthoringContext": {
                "HardConstraints": {
                    "Identity": [
                        {"ID": "brief:identity:0", "Text": "silver hair", "Source": "AssetSet"},
                        {"ID": "brief:identity:1", "Text": "white blindfold", "Source": "AssetSet"},
                    ],
                    "RequiredChanges": [
                        {"ID": "brief:requiredchanges:0", "Text": "cold posture", "Source": "VisualIntent"}
                    ],
                    "ForbiddenChanges": [
                        {"ID": "brief:forbidden:0", "Text": "visible eyes", "Source": "VisualIntent"}
                    ],
                    "Required": [],
                    "Technical": [
                        {"ID": "technical:alpharequired", "Text": "AlphaRequired=True", "Source": "Spec"}
                    ],
                }
            },
            "TechnicalRequest": {"AlphaRequired": True},
            "PreservationContract": {},
            "ActivePromptRevisionID": "",
            "PromptRevisions": [],
        }

    def make_revision(self) -> dict:
        hard_ids = {
            "brief:identity:0": ["preserve long silver hair"],
            "brief:identity:1": ["opaque white cloth covering both eyes"],
            "brief:requiredchanges:0": ["change only to a guarded cold posture"],
            "brief:forbidden:0": ["no eye or eyelid opening may be visible"],
            "technical:alpharequired": ["OutputContract.Background=transparent"],
        }
        return {
            "PromptRevisionID": "doll_zero_cold@abc123/prompt-001",
            "RequirementFingerprint": "requirement-fingerprint",
            "AuthoringMode": "agent_authored",
            "Status": "ready",
            "CommonStrategy": {
                "Operation": "character_difference",
                "Capability": "single_reference_image_edit",
                "ReferenceAssets": ["doll_zero_dialogue_neutral:selected/001.png"],
            },
            "Variants": {
                "natural_language_v2": {
                    "Format": "natural_language_v2",
                    "Status": "ready",
                    "CompatibleProviders": ["gemini_chat_image", "openai_images"],
                    "Positive": "Use the supplied portrait as the exact identity anchor. Change only the performance.",
                    "Negative": "visible eyes, back view",
                    "OutputContract": {"Background": "transparent"},
                    "ConstraintMapping": copy.deepcopy(hard_ids),
                },
                "danbooru_tags_v2": {
                    "Format": "danbooru_tags_v2",
                    "Status": "ready",
                    "CompatibleProviders": ["novelai"],
                    "PositiveTags": [
                        {"Tag": "silver hair", "Weight": 1.2},
                        {"Tag": "white blindfold", "Weight": 1.3},
                    ],
                    "NegativeTags": [{"Tag": "visible eyes", "Weight": 1.4}],
                    "ReferenceControls": {"PreserveReferenceIdentity": True},
                    "ConstraintMapping": copy.deepcopy(hard_ids),
                },
            },
            "AuthorNotes": ["Natural language and tags were authored independently."],
            "CreatedAt": "2026-07-26T12:00:00+08:00",
        }

    def test_publish_requires_independent_dual_variant_slots(self) -> None:
        revision = self.make_revision()
        revision["Variants"].pop("danbooru_tags_v2")

        self.assertIn(
            "prompt_variant_missing:danbooru_tags_v2",
            validate_prompt_revision(self.request, revision),
        )

    def test_ready_variant_requires_all_hard_constraint_ids(self) -> None:
        revision = self.make_revision()
        revision["Variants"]["natural_language_v2"]["ConstraintMapping"].pop("brief:identity:1")

        self.assertIn(
            "constraint_mapping_incomplete:natural_language_v2:brief:identity:1",
            validate_prompt_revision(self.request, revision),
        )

    def test_unsupported_variant_requires_reason_but_not_mapping(self) -> None:
        revision = self.make_revision()
        revision["Variants"]["danbooru_tags_v2"] = {
            "Format": "danbooru_tags_v2",
            "Status": "unsupported",
            "UnsupportedReason": ["Current tag vocabulary cannot express the required edit reliably."],
        }

        self.assertEqual(validate_prompt_revision(self.request, revision), [])

    def test_invalid_tag_weight_is_rejected(self) -> None:
        revision = self.make_revision()
        revision["Variants"]["danbooru_tags_v2"]["PositiveTags"][0]["Weight"] = 0

        self.assertIn(
            "prompt_tag_weight_invalid:danbooru_tags_v2:PositiveTags:0",
            validate_prompt_revision(self.request, revision),
        )

    def test_publish_rejects_duplicate_revision_id_with_changed_content(self) -> None:
        published = publish_prompt_revision(self.request, self.make_revision(), activate=True)
        changed = self.make_revision()
        changed["Variants"]["natural_language_v2"]["Positive"] = "changed"

        with self.assertRaisesRegex(ValueError, "prompt_revision_immutable"):
            publish_prompt_revision(published, changed, activate=True)

    def test_publish_is_idempotent_for_same_revision(self) -> None:
        revision = self.make_revision()
        first = publish_prompt_revision(self.request, revision, activate=True)
        second = publish_prompt_revision(first, revision, activate=True)

        self.assertEqual(len(second["PromptRevisions"]), 1)
        self.assertEqual(second["ActivePromptRevisionID"], revision["PromptRevisionID"])
        self.assertEqual(second["PromptAuthoringStatus"], "prompt_ready")

    def test_revision_fingerprint_ignores_existing_fingerprint_field(self) -> None:
        revision = self.make_revision()
        first = compute_revision_fingerprint(revision)
        revision["RevisionFingerprint"] = "stale-value"

        self.assertEqual(first, compute_revision_fingerprint(revision))

    def test_provider_aware_selection_uses_active_revision(self) -> None:
        request = publish_prompt_revision(self.request, self.make_revision(), activate=True)
        revision, format_id, variant = select_prompt_variant(request, provider="novelai")

        self.assertEqual(revision["PromptRevisionID"], request["ActivePromptRevisionID"])
        self.assertEqual(format_id, "danbooru_tags_v2")
        self.assertEqual(variant["Status"], "ready")

    def test_stale_requirement_is_rejected(self) -> None:
        revision = self.make_revision()
        revision["RequirementFingerprint"] = "old"

        self.assertIn("prompt_revision_stale", validate_prompt_revision(self.request, revision))


if __name__ == "__main__":
    unittest.main()
