# -*- coding: utf-8 -*-

from __future__ import annotations

import copy
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from art_prompt_revision import publish_prompt_revision  # noqa: E402
from compile_art_generation_requests import compile_manifest_requests  # noqa: E402
from run_character_portrait_set import (  # noqa: E402
    build_portrait_set_plan,
    load_asset_set,
    order_portrait_members,
)


class CharacterPortraitSetTests(unittest.TestCase):
    def setUp(self) -> None:
        self.manifest = {
            "Version": 1,
            "Entries": [
                {
                    "VisualID": "doll_zero_dialogue_neutral",
                    "Domain": "doll",
                    "AssetType": "portrait",
                    "DisplayName": "零号中性",
                    "ProductionProfile": "character_portrait_set",
                    "AssetSetID": "zero_dialogue_portrait_v1",
                    "AssetID": "zero_dialogue_neutral",
                    "SetRole": "neutral_dialogue_master",
                    "SourceAssets": [],
                    "Status": "approved",
                    "Spec": {"SourceSpec": {"Format": "png", "Width": 1024, "Height": 1536, "AlphaRequired": True}},
                },
                {
                    "VisualID": "doll_zero_dialogue_confused",
                    "Domain": "doll",
                    "AssetType": "portrait",
                    "DisplayName": "零号疑惑",
                    "ProductionProfile": "character_portrait_set",
                    "AssetSetID": "zero_dialogue_portrait_v1",
                    "AssetID": "zero_dialogue_confused",
                    "SetRole": "confused_expression_difference",
                    "SourceAssets": [{"asset_id": "zero_dialogue_neutral", "role": "identity_reference"}],
                    "Status": "selected",
                    "Spec": {"SourceSpec": {"Format": "png", "Width": 1024, "Height": 1536, "AlphaRequired": True}},
                },
            ],
        }
        compiled = compile_manifest_requests(copy.deepcopy(self.manifest), project_root=Path.cwd())
        self.compiled_manifest = compiled["Manifest"]
        self.catalog = compiled["Catalog"]
        for index, request in enumerate(list(self.catalog["Requests"])):
            hard_ids = [
                item["ID"]
                for values in request["PromptAuthoringContext"]["HardConstraints"].values()
                for item in values
            ]
            mapping = {constraint_id: [f"covered:{constraint_id}"] for constraint_id in hard_ids}
            revision = {
                "PromptRevisionID": f"{request['RequestID']}/prompt-001",
                "RequirementFingerprint": request["RequirementFingerprint"],
                "AuthoringMode": "agent_authored",
                "Status": "ready",
                "CommonStrategy": {"Operation": "character_portrait_member"},
                "Variants": {
                    "natural_language_v2": {
                        "Format": "natural_language_v2",
                        "Status": "ready",
                        "Positive": f"Create {request['VisualID']} while preserving the locked identity.",
                        "Negative": "identity drift",
                        "OutputContract": {},
                        "ConstraintMapping": copy.deepcopy(mapping),
                    },
                    "danbooru_tags_v2": {
                        "Format": "danbooru_tags_v2",
                        "Status": "ready",
                        "PositiveTags": [{"Tag": "1girl", "Weight": 1.0}],
                        "NegativeTags": [{"Tag": "identity drift", "Weight": 1.0}],
                        "ReferenceControls": {},
                        "ConstraintMapping": copy.deepcopy(mapping),
                    },
                },
            }
            published = publish_prompt_revision(request, revision, activate=True)
            self.catalog["Requests"][index] = published
            pointer = next(
                entry["CompiledRequest"]
                for entry in self.compiled_manifest["Entries"]
                if entry["VisualID"] == request["VisualID"]
            )
            pointer["PromptAuthoringStatus"] = "prompt_ready"
            pointer["ActivePromptRevisionID"] = revision["PromptRevisionID"]

    def test_powershell_wrapper_dry_run_uses_python_utf8_defaults(self) -> None:
        wrapper = TOOLS_DIR / "Run-CharacterPortraitSet.ps1"
        with tempfile.TemporaryDirectory() as temp_dir:
            manifest_path = Path(temp_dir) / "manifest.json"
            catalog_path = Path(temp_dir) / "catalog.json"
            manifest_path.write_text(__import__("json").dumps(self.compiled_manifest), encoding="utf-8")
            catalog_path.write_text(__import__("json").dumps(self.catalog), encoding="utf-8")
            completed = subprocess.run(
                [
                    "powershell", "-NoProfile", "-ExecutionPolicy", "Bypass", "-File", str(wrapper),
                    "-AssetSetID", "zero_dialogue_portrait_v1",
                    "-ManifestPath", str(manifest_path),
                    "-RequestCatalogPath", str(catalog_path),
                    "-VisualID", "doll_zero_dialogue_confused",
                    "-PromptFormat", "natural_language_v2",
                    "-DryRun",
                ],
                cwd=TOOLS_DIR.parents[1], capture_output=True, text=True, encoding="utf-8",
                errors="replace", timeout=30, check=False,
            )

        self.assertEqual(completed.returncode, 0, completed.stderr or completed.stdout)
        self.assertIn('"VisualID": "doll_zero_dialogue_confused"', completed.stdout)

    def test_missing_asset_set_is_rejected(self) -> None:
        with self.assertRaisesRegex(ValueError, "asset_set_missing"):
            load_asset_set(self.compiled_manifest, "missing")

    def test_master_is_ordered_before_difference(self) -> None:
        asset_set = load_asset_set(self.compiled_manifest, "zero_dialogue_portrait_v1")
        ordered = order_portrait_members(asset_set, list(reversed(self.compiled_manifest["Entries"])))
        self.assertEqual(ordered[0]["AssetID"], "zero_dialogue_neutral")
        self.assertEqual(ordered[1]["AssetID"], "zero_dialogue_confused")

    def test_missing_source_is_rejected(self) -> None:
        entries = copy.deepcopy(self.compiled_manifest["Entries"])
        entries[1]["SourceAssets"] = [{"asset_id": "missing", "role": "identity_reference"}]
        with self.assertRaisesRegex(ValueError, "asset_set_source_missing"):
            order_portrait_members(load_asset_set(self.compiled_manifest, "zero_dialogue_portrait_v1"), entries)

    def test_dependency_cycle_is_rejected(self) -> None:
        entries = copy.deepcopy(self.compiled_manifest["Entries"])
        entries[0]["SourceAssets"] = [{"asset_id": "zero_dialogue_confused", "role": "identity_reference"}]
        with self.assertRaisesRegex(ValueError, "asset_set_dependency_cycle"):
            order_portrait_members(load_asset_set(self.compiled_manifest, "zero_dialogue_portrait_v1"), entries)

    def test_valid_set_plan_contains_request_and_preservation_contract(self) -> None:
        plan = build_portrait_set_plan(
            self.compiled_manifest,
            self.catalog,
            "zero_dialogue_portrait_v1",
        )
        self.assertEqual(plan["State"], "ready")
        self.assertEqual(plan["Items"][0]["VisualID"], "doll_zero_dialogue_neutral")
        self.assertTrue(plan["Items"][1]["RequestID"])
        self.assertTrue(plan["Items"][1]["PromptRevisionID"])
        self.assertEqual(plan["Items"][1]["PromptFormat"], "natural_language_v2")
        self.assertIn("Preserve", plan["Items"][1]["PreservationContract"])
        self.assertIn("ReferenceAssets", plan["Items"][1])

    def test_novelai_selects_danbooru_revision_variant(self) -> None:
        plan = build_portrait_set_plan(
            self.compiled_manifest,
            self.catalog,
            "zero_dialogue_portrait_v1",
            provider="novelai",
        )
        self.assertEqual(plan["Items"][0]["PromptFormat"], "danbooru_tags_v2")

    def test_missing_active_revision_is_rejected(self) -> None:
        catalog = copy.deepcopy(self.catalog)
        request = catalog["Requests"][0]
        request["PromptAuthoringStatus"] = "prompt_authoring_required"
        request["ActivePromptRevisionID"] = ""
        request["PromptRevisions"] = []
        manifest = copy.deepcopy(self.compiled_manifest)
        pointer = next(
            entry["CompiledRequest"]
            for entry in manifest["Entries"]
            if entry["VisualID"] == request["VisualID"]
        )
        pointer["PromptAuthoringStatus"] = "prompt_authoring_required"
        pointer["ActivePromptRevisionID"] = ""

        plan = build_portrait_set_plan(manifest, catalog, "zero_dialogue_portrait_v1")
        self.assertIn(f"prompt_authoring_required:{request['VisualID']}", plan["Errors"])


if __name__ == "__main__":
    unittest.main()
