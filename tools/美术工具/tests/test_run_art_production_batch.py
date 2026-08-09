# -*- coding: utf-8 -*-
from __future__ import annotations

import json
import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from run_art_production_batch import (  # noqa: E402
    build_execution_plan,
    collect_generation_results,
    collect_processing_results,
    parse_routes,
)


class ArtProductionBatchPlanTests(unittest.TestCase):
    def setUp(self) -> None:
        self.manifest = {
            "Entries": [
                {
                    "VisualID": "bg_workshop_day",
                    "Status": "approved",
                    "Domain": "background",
                    "AssetType": "background",
                    "ProductionProfile": "standard_asset",
                },
                {
                    "VisualID": "ui_button_primary",
                    "Status": "approved",
                    "Domain": "ui",
                    "AssetType": "button",
                    "ProductionProfile": "standard_asset",
                },
                {
                    "VisualID": "item_new_icon",
                    "Status": "todo",
                    "Domain": "item",
                    "AssetType": "icon",
                    "ProductionProfile": "standard_asset",
                },
            ]
        }

    def _attach_ready_request(self, visual_id: str, *, tags_ready: bool = True) -> dict:
        entry = next(item for item in self.manifest["Entries"] if item["VisualID"] == visual_id)
        request_id = f"{visual_id}@requirement"
        requirement_fingerprint = f"requirement-{visual_id}"
        revision_id = f"{request_id}/prompt-001"
        tags = {
            "Format": "danbooru_tags_v2",
            "Status": "ready",
            "PositiveTags": [{"Tag": visual_id, "Weight": 1.0}],
            "NegativeTags": [],
            "ReferenceControls": {},
            "ConstraintMapping": {},
        }
        if not tags_ready:
            tags = {
                "Format": "danbooru_tags_v2",
                "Status": "unsupported",
                "UnsupportedReason": ["This route requires a natural-language provider."],
            }
        revision = {
            "PromptRevisionID": revision_id,
            "RequirementFingerprint": requirement_fingerprint,
            "AuthoringMode": "agent_authored",
            "Status": "ready",
            "CommonStrategy": {},
            "Variants": {
                "natural_language_v2": {
                    "Format": "natural_language_v2",
                    "Status": "ready",
                    "Positive": f"Create {visual_id}.",
                    "Negative": "text",
                    "OutputContract": {},
                    "ConstraintMapping": {},
                },
                "danbooru_tags_v2": tags,
            },
        }
        request = {
            "RequestID": request_id,
            "VisualID": visual_id,
            "RequirementFingerprint": requirement_fingerprint,
            "RequirementStatus": "ready",
            "PromptAuthoringStatus": "prompt_ready",
            "PromptAuthoringContext": {"HardConstraints": {}},
            "ActivePromptRevisionID": revision_id,
            "PromptRevisions": [revision],
        }
        entry["CompiledRequest"] = {
            "RequestID": request_id,
            "RequirementFingerprint": requirement_fingerprint,
            "PromptAuthoringStatus": "prompt_ready",
            "ActivePromptRevisionID": revision_id,
        }
        return request

    def test_parse_routes_requires_asset_class_and_provider(self) -> None:
        self.assertEqual(
            parse_routes(["background=openai_images", "icon=novelai"]),
            {"background": "openai_images", "icon": "novelai"},
        )
        with self.assertRaisesRegex(ValueError, "asset_class=provider"):
            parse_routes(["openai_images"])

    def test_replacement_plan_preserves_status_and_blocks_unrouted_classes(self) -> None:
        payload = {
            "RunConfig": {
                "Action": "visual_v2_replace",
                "BatchID": "formalv2_replace_01",
                "Provider": "agent_selected",
                "Variants": 2,
                "DelaySeconds": 1.0,
            },
            "Items": [
                {"VisualID": "bg_workshop_day", "AssetClass": "background", "PromptReady": True},
                {"VisualID": "ui_button_primary", "AssetClass": "ui_skin", "PromptReady": True},
            ],
        }

        result = build_execution_plan(
            payload,
            self.manifest,
            routes={"background": "openai_images"},
            provider_override="",
            visual_ids=set(),
            asset_classes=set(),
            limit=0,
            request_catalog={"Requests": [self._attach_ready_request("bg_workshop_day")]},
        )

        self.assertEqual([item["VisualID"] for item in result["Blocked"]], ["ui_button_primary"])
        self.assertEqual(len(result["Groups"]), 1)
        group = result["Groups"][0]
        self.assertTrue(group["PreserveStatus"])
        self.assertEqual(group["Status"], "approved")
        self.assertEqual(group["Provider"], "openai_images")
        self.assertEqual(group["VisualIDs"], ["bg_workshop_day"])
        self.assertIn("--preserve-status", group["GenerationCommand"])
        self.assertIn("--candidate-batch-id", group["ProcessingCommand"])

    def test_missing_asset_plan_advances_todo_entries_without_preserve_status(self) -> None:
        payload = {
            "RunConfig": {
                "ActionFilter": "generate_needed",
                "Status": "todo",
                "BatchID": "missing_01",
                "Provider": "novelai",
                "Variants": 4,
                "DelaySeconds": 1.0,
            },
            "Items": [
                {"VisualID": "item_new_icon", "PromptReady": True, "Domain": "item", "AssetType": "icon"},
            ],
        }

        result = build_execution_plan(
            payload,
            self.manifest,
            routes={},
            provider_override="",
            visual_ids=set(),
            asset_classes=set(),
            limit=0,
            request_catalog={"Requests": [self._attach_ready_request("item_new_icon")]},
        )

        self.assertEqual(result["Blocked"], [])
        group = result["Groups"][0]
        self.assertFalse(group["PreserveStatus"])
        self.assertEqual(group["Status"], "todo")
        self.assertEqual(group["Provider"], "novelai")
        self.assertEqual(group["PromptFormat"], "danbooru_tags_v2")
        self.assertNotIn("--preserve-status", group["GenerationCommand"])
        self.assertIn("--batch-id", group["ProcessingCommand"])
        self.assertNotIn("--candidate-batch-id", group["ProcessingCommand"])

    def test_prompt_authoring_required_item_never_enters_a_group(self) -> None:
        payload = {
            "RunConfig": {
                "Action": "visual_v2_replace",
                "BatchID": "formalv2_replace_01",
                "Provider": "agent_selected",
            },
            "Items": [
                {"VisualID": "bg_workshop_day", "AssetClass": "background"},
            ],
        }
        request = self._attach_ready_request("bg_workshop_day")
        request["PromptAuthoringStatus"] = "prompt_authoring_required"
        request["ActivePromptRevisionID"] = ""
        request["PromptRevisions"] = []
        self.manifest["Entries"][0]["CompiledRequest"]["PromptAuthoringStatus"] = "prompt_authoring_required"
        self.manifest["Entries"][0]["CompiledRequest"]["ActivePromptRevisionID"] = ""

        result = build_execution_plan(
            payload,
            self.manifest,
            routes={"background": "openai_images"},
            provider_override="",
            visual_ids=set(),
            asset_classes=set(),
            limit=0,
            request_catalog={"Requests": [request]},
        )

        self.assertEqual(result["Groups"], [])
        self.assertEqual(result["Blocked"][0]["Reason"], "prompt_authoring_required:bg_workshop_day")

    def test_ui_skin_capability_route_uses_specialized_adapter(self) -> None:
        payload = {
            "RunConfig": {
                "Action": "visual_v2_replace",
                "BatchID": "formalv2_replace_01",
                "Provider": "agent_selected",
                "Variants": 2,
                "DelaySeconds": 1.0,
            },
            "Items": [
                {"VisualID": "ui_button_primary", "AssetClass": "ui_skin", "PromptReady": True},
            ],
        }

        result = build_execution_plan(
            payload,
            self.manifest,
            routes={"ui_skin": "deterministic_template"},
            provider_override="",
            visual_ids=set(),
            asset_classes=set(),
            limit=0,
        )

        self.assertEqual(result["Blocked"], [])
        group = result["Groups"][0]
        self.assertEqual(group["RouteKind"], "ui_skin_capability")
        self.assertEqual(group["Capability"], "deterministic_template")
        self.assertEqual(group["PromptFormat"], "not_required")
        self.assertEqual(group["PromptRevisionID"], "")
        self.assertIn("generate_ui_skin_candidate.py", " ".join(group["GenerationCommand"]))
        self.assertIn("--capability", group["GenerationCommand"])
        self.assertNotIn("--provider", group["GenerationCommand"])

    def test_character_portrait_profile_is_hard_blocked(self) -> None:
        manifest = {
            "Entries": [
                {
                    "VisualID": "doll_zero_dialogue_confused",
                    "Status": "approved",
                    "ProductionProfile": "character_portrait_set",
                    "Domain": "doll",
                    "AssetType": "portrait",
                }
            ]
        }
        payload = {
            "RunConfig": {"Action": "visual_v2_replace", "BatchID": "portrait_block_01", "Variants": 1},
            "Items": [{"VisualID": "doll_zero_dialogue_confused", "PromptReady": True}],
        }

        result = build_execution_plan(
            payload,
            manifest,
            routes={"standard_asset": "openai_images"},
            provider_override="",
            visual_ids=set(),
            asset_classes=set(),
            limit=0,
        )

        self.assertEqual(result["Groups"], [])
        self.assertIn("route_mismatch:character_portrait_set", result["Blocked"][0]["Reason"])

    def test_compiled_request_controls_prompt_format_and_command(self) -> None:
        manifest = {
            "Entries": [
                {
                    "VisualID": "bg_workshop_day",
                    "Status": "approved",
                    "ProductionProfile": "standard_asset",
                    "Domain": "background",
                    "AssetType": "background",
                    "CompiledRequest": {
                        "RequestID": "bg@abc",
                        "RequirementFingerprint": "fingerprint",
                        "PromptAuthoringStatus": "prompt_ready",
                        "ActivePromptRevisionID": "bg@abc/prompt-001",
                    },
                }
            ]
        }
        payload = {
            "RunConfig": {"Action": "visual_v2_replace", "BatchID": "compiled_01", "Variants": 1},
            "Items": [{"VisualID": "bg_workshop_day", "AssetClass": "background", "PromptReady": True}],
        }
        request_catalog = {
            "Requests": [
                {
                    "RequestID": "bg@abc",
                    "VisualID": "bg_workshop_day",
                    "RequirementFingerprint": "fingerprint",
                    "RequirementStatus": "ready",
                    "PromptAuthoringStatus": "prompt_ready",
                    "PromptAuthoringContext": {"HardConstraints": {}},
                    "ActivePromptRevisionID": "bg@abc/prompt-001",
                    "PromptRevisions": [
                        {
                            "PromptRevisionID": "bg@abc/prompt-001",
                            "RequirementFingerprint": "fingerprint",
                            "AuthoringMode": "agent_authored",
                            "Status": "ready",
                            "CommonStrategy": {},
                            "Variants": {
                                "natural_language_v2": {
                                    "Format": "natural_language_v2",
                                    "Status": "ready",
                                    "Positive": "Create a workshop background.",
                                    "Negative": "text",
                                    "OutputContract": {},
                                    "ConstraintMapping": {},
                                },
                                "danbooru_tags_v2": {
                                    "Format": "danbooru_tags_v2",
                                    "Status": "unsupported",
                                    "UnsupportedReason": ["Natural language required."],
                                },
                            },
                        }
                    ],
                }
            ]
        }

        result = build_execution_plan(
            payload,
            manifest,
            routes={"background": "openai_images"},
            provider_override="",
            visual_ids=set(),
            asset_classes=set(),
            limit=0,
            request_catalog=request_catalog,
            request_catalog_path="requests.json",
        )

        group = result["Groups"][0]
        self.assertEqual(group["PromptFormat"], "natural_language_v2")
        self.assertEqual(group["PromptRevisionID"], "bg@abc/prompt-001")
        self.assertIn("--request-catalog", group["GenerationCommand"])
        self.assertIn("--prompt-format", group["GenerationCommand"])
        self.assertIn("--prompt-revision-id", group["GenerationCommand"])

    def test_distinct_prompt_revisions_are_not_grouped_together(self) -> None:
        self.manifest["Entries"].append(
            {
                "VisualID": "bg_combat_abyss",
                "Status": "approved",
                "Domain": "background",
                "AssetType": "background",
                "ProductionProfile": "standard_asset",
            }
        )
        catalog = {
            "Requests": [
                self._attach_ready_request("bg_workshop_day"),
                self._attach_ready_request("bg_combat_abyss"),
            ]
        }
        payload = {
            "RunConfig": {
                "Action": "visual_v2_replace",
                "BatchID": "separate_revisions_01",
                "Provider": "openai_images",
                "Variants": 1,
            },
            "Items": [
                {"VisualID": "bg_workshop_day", "AssetClass": "background"},
                {"VisualID": "bg_combat_abyss", "AssetClass": "background"},
            ],
        }

        result = build_execution_plan(
            payload,
            self.manifest,
            routes={},
            provider_override="",
            visual_ids=set(),
            asset_classes=set(),
            limit=0,
            request_catalog=catalog,
        )

        self.assertEqual(result["Blocked"], [])
        self.assertEqual(len(result["Groups"]), 2)
        self.assertEqual({len(group["VisualIDs"]) for group in result["Groups"]}, {1})
        self.assertEqual(len({group["PromptRevisionID"] for group in result["Groups"]}), 2)

    def test_zero_output_generation_never_reuses_an_old_processing_report(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            incoming = root / "incoming"
            manifest_path = root / "manifest.json"
            workspace = incoming / "standard_assets" / "bg_workshop_day"
            workspace.mkdir(parents=True)
            manifest_path.write_text(
                json.dumps(
                    {
                        "Entries": [
                            {
                                "VisualID": "bg_workshop_day",
                                "Status": "approved",
                                "ProductionProfile": "standard_asset",
                            }
                        ]
                    }
                ),
                encoding="utf-8",
            )
            (workspace / "generation.json").write_text(
                json.dumps(
                    {
                        "BatchID": "replace_background",
                        "Outputs": [],
                        "Errors": ["provider unavailable"],
                    }
                ),
                encoding="utf-8",
            )
            (workspace / "process_report.json").write_text(
                json.dumps({"LatestRound": 9, "LatestState": "passed"}),
                encoding="utf-8",
            )
            execution = {
                "Groups": [
                    {
                        "AssetClass": "background",
                        "Provider": "openai_images",
                        "BatchID": "replace_background",
                        "VisualIDs": ["bg_workshop_day"],
                    }
                ]
            }

            generation = collect_generation_results(
                execution["Groups"][0], manifest_path, incoming, project_root=root
            )
            technical = collect_processing_results(
                execution,
                manifest_path,
                incoming,
                generation_results={item["VisualID"]: item for item in generation},
            )

            self.assertEqual(generation[0]["State"], "failed")
            self.assertEqual(technical[0]["Claim"], "raw_failed")
            self.assertIsNone(technical[0]["LatestRound"])

    def test_generation_result_requires_decodable_output_from_current_batch(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            root = Path(temp_dir)
            incoming = root / "incoming"
            manifest_path = root / "manifest.json"
            workspace = incoming / "standard_assets" / "bg_workshop_day"
            raw = workspace / "raw"
            raw.mkdir(parents=True)
            image_path = raw / "001.png"
            Image.new("RGBA", (2, 2), "red").save(image_path)
            manifest_path.write_text(
                json.dumps(
                    {
                        "Entries": [
                            {
                                "VisualID": "bg_workshop_day",
                                "Status": "approved",
                                "ProductionProfile": "standard_asset",
                            }
                        ]
                    }
                ),
                encoding="utf-8",
            )
            (workspace / "generation.json").write_text(
                json.dumps(
                    {
                        "BatchID": "replace_background",
                        "Outputs": [{"RepoPath": image_path.relative_to(root).as_posix()}],
                        "Errors": [],
                    }
                ),
                encoding="utf-8",
            )
            group = {
                "BatchID": "replace_background",
                "VisualIDs": ["bg_workshop_day"],
            }

            result = collect_generation_results(group, manifest_path, incoming, project_root=root)

            self.assertEqual(result[0]["State"], "succeeded")
            self.assertEqual(result[0]["OutputCount"], 1)


if __name__ == "__main__":
    unittest.main()
