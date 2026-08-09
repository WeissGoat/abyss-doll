# -*- coding: utf-8 -*-

from __future__ import annotations

import sys
import unittest
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from art_requirement_defaults import default_spec_for, visual_intent_for  # noqa: E402


class ArtRequirementDefaultsTests(unittest.TestCase):
    def test_portrait_defaults_are_method_neutral(self) -> None:
        entry = {
            "VisualID": "doll_demo_hurt",
            "Domain": "doll",
            "AssetType": "portrait",
            "DisplayName": "演示角色受伤差分",
            "ProductionProfile": "character_portrait_set",
            "SetRole": "hurt_difference",
            "SourceFactsCN": "无血、无断肢。",
            "PoseSpec": {"BodyAction": "recoils backward"},
        }

        intent = visual_intent_for(entry)
        spec = default_spec_for(entry)

        self.assertIn("Body action: recoils backward", intent["RequiredChanges"])
        self.assertIn("blood or gore", intent["ForbiddenElements"])
        self.assertEqual(spec["SourceSpec"]["Width"], 1024)
        self.assertEqual(spec["ProcessSpec"]["BackgroundPolicy"], "agent_required")

    def test_ui_skin_defaults_require_nine_slice_processing_without_prompt_text(self) -> None:
        spec = default_spec_for({"Domain": "ui", "AssetType": "button", "VisualID": "ui_demo_button"})

        self.assertEqual(spec["DisplaySpec"]["FitMode"], "stretch")
        self.assertIn("check_nine_slice_edges", spec["ProcessSpec"]["PostProcess"])


if __name__ == "__main__":
    unittest.main()
