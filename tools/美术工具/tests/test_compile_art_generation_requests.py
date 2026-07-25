# -*- coding: utf-8 -*-

from __future__ import annotations

import copy
import json
import subprocess
import sys
import tempfile
import unittest
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from compile_art_generation_requests import compile_manifest_requests  # noqa: E402


class CompileArtGenerationRequestsTests(unittest.TestCase):
    def test_powershell_wrapper_dry_run_uses_python_utf8_defaults(self) -> None:
        wrapper = TOOLS_DIR / "Compile-ArtGenerationRequests.ps1"
        completed = subprocess.run(
            [
                "powershell",
                "-NoProfile",
                "-ExecutionPolicy",
                "Bypass",
                "-File",
                str(wrapper),
                "-DryRun",
            ],
            cwd=TOOLS_DIR.parents[1],
            capture_output=True,
            text=True,
            encoding="utf-8",
            errors="replace",
            timeout=30,
            check=False,
        )

        self.assertEqual(completed.returncode, 0, completed.stderr or completed.stdout)
        self.assertIn('"Ready": 313', completed.stdout)

    def test_refresh_flags_replace_catalog_and_selected_visual_intent_only(self) -> None:
        manifest = {
            "Version": 3,
            "ArtStyleCatalog": {
                "GlobalStyle": {"ID": "old", "PositiveEN": ["old style"]},
                "Profiles": {},
                "Families": {},
                "ContextAccents": {},
            },
            "Entries": [
                {
                    "VisualID": "ui_button_primary",
                    "Domain": "ui",
                    "ConfigID": "button_primary",
                    "AssetType": "button",
                    "DisplayName": "主要行动按钮",
                    "ProductionProfile": "standard_asset",
                    "Status": "approved",
                    "Spec": {"SourceSpec": {"Format": "png", "Width": 512, "Height": 160}},
                    "VisualIntent": {
                        "SubjectEN": ["old subject"],
                        "AppearanceEN": ["bright cool highlight"],
                        "CompositionEN": ["old composition"],
                    },
                },
                {
                    "VisualID": "item_demo_icon",
                    "Domain": "item",
                    "ConfigID": "loot_gear_scrap",
                    "AssetType": "icon",
                    "DisplayName": "演示物品",
                    "ProductionProfile": "standard_asset",
                    "Status": "todo",
                    "Spec": {"SourceSpec": {"Format": "png", "Width": 128, "Height": 128}},
                    "VisualIntent": {"AppearanceEN": ["keep this intent"]},
                },
            ],
        }

        result = compile_manifest_requests(
            copy.deepcopy(manifest),
            project_root=Path.cwd(),
            refresh_style_catalog=True,
            refresh_visual_intent_ids={"ui_button_primary"},
        )

        compiled = {entry["VisualID"]: entry for entry in result["Manifest"]["Entries"]}
        self.assertEqual(result["Manifest"]["ArtStyleCatalog"]["GlobalStyle"]["ID"], "p3_global_v1")
        self.assertIn(
            "warm moon-white rim",
            " | ".join(compiled["ui_button_primary"]["VisualIntent"]["AppearanceEN"]),
        )
        self.assertEqual(compiled["item_demo_icon"]["VisualIntent"]["AppearanceEN"], ["keep this intent"])

    def test_compiles_standard_and_portrait_entries_without_changing_state(self) -> None:
        manifest = {
            "Version": 1,
            "Entries": [
                {
                    "VisualID": "ui_button_primary",
                    "Domain": "ui",
                    "AssetType": "button",
                    "DisplayName": "主要行动按钮",
                    "ProductionProfile": "standard_asset",
                    "Status": "approved",
                    "ApprovedPath": "UnityClient/Assets/Art/Approved/UI/ui_button_primary.png",
                    "RegistryStatus": "registered",
                    "Spec": {
                        "SourceSpec": {"Format": "png", "Width": 512, "Height": 160, "AlphaRequired": True},
                        "ProcessSpec": {"NineSlice": {"Enabled": True}},
                    },
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
                    "SourceFactsCN": "基于中性立绘，嘴角轻微收紧；无血、无断肢。",
                    "Status": "selected",
                    "SelectedPath": "UnityClient/Assets/Art/_IncomingAI/character_portraits/doll_zero_dialogue_confused/selected/001.png",
                    "Spec": {"SourceSpec": {"Format": "png", "Width": 1024, "Height": 1536, "AlphaRequired": True}},
                },
            ],
        }

        with tempfile.TemporaryDirectory() as temp_dir:
            result = compile_manifest_requests(copy.deepcopy(manifest), project_root=Path(temp_dir))

        compiled = {entry["VisualID"]: entry for entry in result["Manifest"]["Entries"]}
        self.assertEqual(result["Manifest"]["Version"], 3)
        self.assertEqual(compiled["ui_button_primary"]["Status"], "approved")
        self.assertEqual(compiled["ui_button_primary"]["RegistryStatus"], "registered")
        self.assertEqual(compiled["doll_zero_dialogue_confused"]["SelectedPath"], manifest["Entries"][1]["SelectedPath"])
        self.assertEqual(result["Catalog"]["Summary"]["Ready"], 2)
        self.assertEqual(
            result["Manifest"]["AssetSets"]["zero_dialogue_portrait_v1"]["IdentityLocks"][0],
            "silver hair",
        )
        portrait_intent = compiled["doll_zero_dialogue_confused"]["VisualIntent"]
        self.assertIn("嘴角轻微收紧", portrait_intent["RequiredChanges"][0])
        self.assertIn("blood or gore", portrait_intent["ForbiddenElements"])

    def test_compile_report_is_json_serializable_and_request_ids_are_stable(self) -> None:
        manifest = {
            "Version": 1,
            "Entries": [
                {
                    "VisualID": "item_demo_icon",
                    "Domain": "item",
                    "AssetType": "icon",
                    "DisplayName": "演示物品",
                    "ProductionProfile": "standard_asset",
                    "Status": "todo",
                    "Spec": {"SourceSpec": {"Format": "png", "Width": 128, "Height": 128}},
                }
            ],
        }
        first = compile_manifest_requests(copy.deepcopy(manifest), project_root=Path.cwd())
        second = compile_manifest_requests(copy.deepcopy(manifest), project_root=Path.cwd())

        self.assertEqual(first["Catalog"]["Requests"][0]["RequestID"], second["Catalog"]["Requests"][0]["RequestID"])
        json.dumps(first, ensure_ascii=False)


if __name__ == "__main__":
    unittest.main()
