# -*- coding: utf-8 -*-
from __future__ import annotations

import argparse
import sys
import unittest
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from generate_art_batch_plan import build_item, make_markdown  # noqa: E402


class ArtBatchPlanTests(unittest.TestCase):
    def setUp(self) -> None:
        self.args = argparse.Namespace(
            provider="openai_images",
            request_catalog="requests.json",
            variants=2,
            delay_seconds=1.0,
            batch_id="batch_01",
        )
        self.entry = {
            "VisualID": "bg_workshop_day",
            "Status": "todo",
            "Domain": "background",
            "AssetType": "background",
            "CompiledRequest": {
                "RequestID": "bg_workshop_day@requirement",
                "RequirementFingerprint": "requirement-fingerprint",
                "PromptAuthoringStatus": "prompt_ready",
                "ActivePromptRevisionID": "bg_workshop_day@requirement/prompt-001",
            },
            "Spec": {"Width": 1024, "Height": 1024},
        }
        self.request = {
            "RequestID": "bg_workshop_day@requirement",
            "VisualID": "bg_workshop_day",
            "RequirementFingerprint": "requirement-fingerprint",
            "RequirementStatus": "ready",
            "PromptAuthoringStatus": "prompt_ready",
            "ActivePromptRevisionID": "bg_workshop_day@requirement/prompt-001",
            "PromptRevisions": [
                {
                    "PromptRevisionID": "bg_workshop_day@requirement/prompt-001",
                    "RequirementFingerprint": "requirement-fingerprint",
                    "Status": "ready",
                    "Variants": {
                        "natural_language_v2": {"Format": "natural_language_v2", "Status": "ready", "Positive": "Workshop."},
                    },
                }
            ],
        }

    def test_plan_item_uses_revision_pointer_without_prompt_text(self) -> None:
        item = build_item({"VisualID": "bg_workshop_day"}, self.entry, self.request, self.args)

        self.assertTrue(item["PromptReady"])
        self.assertEqual(item["PromptRevisionID"], "bg_workshop_day@requirement/prompt-001")
        self.assertIn("-PromptRevisionID bg_workshop_day@requirement/prompt-001", item["Commands"]["RunGeneration"])

    def test_plan_item_blocks_without_published_revision(self) -> None:
        self.request["PromptAuthoringStatus"] = "prompt_authoring_required"
        self.request["ActivePromptRevisionID"] = ""
        self.request["PromptRevisions"] = []
        self.entry["CompiledRequest"]["PromptAuthoringStatus"] = "prompt_authoring_required"
        self.entry["CompiledRequest"]["ActivePromptRevisionID"] = ""

        item = build_item({"VisualID": "bg_workshop_day"}, self.entry, self.request, self.args)

        self.assertFalse(item["PromptReady"])
        self.assertEqual(item["Commands"]["RunGeneration"], "")
        self.assertEqual(item["PromptBlockReason"], "prompt_authoring_required")

    def test_markdown_uses_action_after_status_filter_removal(self) -> None:
        markdown = make_markdown(
            {
                "GeneratedAt": "2026-08-08T00:00:00+08:00",
                "Summary": {"PlannedItems": 0, "PromptReadyItems": 0, "PriorityCounts": {}, "DomainCounts": {}, "SizeCounts": {}},
                "RunConfig": {"Provider": "openai_images", "BatchID": "batch_01", "Action": "generate_needed", "Variants": 2, "DelaySeconds": 1.0, "SerialRequired": True},
                "BatchCommands": {"RunProductionBatch": "run", "RefreshIntegration": "integration", "RefreshQuality": "quality", "RefreshPlan": "plan"},
                "Items": [],
            }
        )

        self.assertIn("Action: `generate_needed`", markdown)


if __name__ == "__main__":
    unittest.main()
