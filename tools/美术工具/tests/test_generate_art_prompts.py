# -*- coding: utf-8 -*-

from __future__ import annotations

import json
import sys
import tempfile
import unittest
from pathlib import Path
from unittest import mock


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

import generate_art_prompts  # noqa: E402
from generate_art_prompts import entry_matches_visual_ids, split_filters  # noqa: E402


class GenerateArtPromptsFilterTests(unittest.TestCase):
    def test_visual_id_filter_accepts_repeat_and_comma_separated_values(self) -> None:
        selected = split_filters(["doll_a,doll_b", "doll_c"])

        self.assertEqual(selected, {"doll_a", "doll_b", "doll_c"})
        self.assertTrue(entry_matches_visual_ids({"VisualID": "doll_b"}, selected))
        self.assertFalse(entry_matches_visual_ids({"VisualID": "ui_icon"}, selected))

    def test_empty_filter_matches_all_entries(self) -> None:
        self.assertTrue(entry_matches_visual_ids({"VisualID": "anything"}, set()))

    def test_main_updates_only_selected_visual_id(self) -> None:
        entries = [
            {
                "Domain": "doll",
                "ConfigID": "zero_dialogue_neutral",
                "AssetType": "portrait",
                "VisualID": "doll_zero_dialogue_neutral",
                "Status": "todo",
            },
            {
                "Domain": "doll",
                "ConfigID": "unrelated_doll",
                "AssetType": "portrait",
                "VisualID": "doll_unrelated",
                "Status": "todo",
            },
        ]
        with tempfile.TemporaryDirectory(dir=Path.cwd()) as temp_dir:
            root = Path(temp_dir)
            manifest_path = root / "manifest.json"
            markdown_path = root / "prompts.md"
            manifest_path.write_text(json.dumps({"Entries": entries}), encoding="utf-8")

            argv = [
                "generate_art_prompts.py",
                "--manifest-path",
                str(manifest_path),
                "--prompt-markdown-path",
                str(markdown_path),
                "--visual-id",
                "doll_zero_dialogue_neutral",
            ]
            with mock.patch.object(sys, "argv", argv):
                self.assertEqual(generate_art_prompts.main(), 0)

            result = json.loads(manifest_path.read_text(encoding="utf-8"))
            self.assertEqual(result["Entries"][0]["Status"], "prompted")
            self.assertIn("white cloth blindfold", result["Entries"][0]["PromptEN"])
            self.assertEqual(result["Entries"][1], entries[1])


class GenerateArtPromptBackgroundPolicyTests(unittest.TestCase):
    def test_character_portrait_uses_agent_required(self) -> None:
        _, _, _, spec = generate_art_prompts.prompt_for(
            {
                "Domain": "doll",
                "ConfigID": "zero_dialogue_neutral",
                "AssetType": "portrait",
                "VisualID": "doll_zero_dialogue_neutral",
            }
        )
        self.assertEqual(spec["ProcessSpec"]["BackgroundPolicy"], "agent_required")

    def test_item_uses_auto_simple(self) -> None:
        _, _, _, spec = generate_art_prompts.prompt_for(
            {
                "Domain": "item",
                "ConfigID": "loot_gear_scrap",
                "AssetType": "icon",
                "VisualID": "item_loot_gear_scrap_icon",
            }
        )
        self.assertEqual(spec["ProcessSpec"]["BackgroundPolicy"], "auto_simple")

    def test_background_and_narrative_cg_preserve_full_frame(self) -> None:
        for domain, config_id in [
            ("background", "workshop"),
            ("narrative_cg", "t0_01a_p02_panel01_workshop_wide"),
        ]:
            with self.subTest(domain=domain):
                _, _, _, spec = generate_art_prompts.prompt_for(
                    {
                        "Domain": domain,
                        "ConfigID": config_id,
                        "AssetType": "background",
                        "VisualID": f"test_{domain}",
                    }
                )
                self.assertEqual(spec["ProcessSpec"]["BackgroundPolicy"], "preserve")

    def test_refresh_spec_only_preserves_prompt_status_and_other_fields(self) -> None:
        entry = {
            "Domain": "item",
            "ConfigID": "loot_gear_scrap",
            "AssetType": "icon",
            "VisualID": "item_loot_gear_scrap_icon",
            "Status": "generated",
            "PromptEN": "existing prompt",
            "PromptCN": "existing review text",
            "NegativePromptEN": "existing negative",
            "Notes": "preserve this",
        }
        result = generate_art_prompts.refresh_spec_only(entry)
        self.assertEqual(result["Status"], "generated")
        self.assertEqual(result["PromptEN"], "existing prompt")
        self.assertEqual(result["PromptCN"], "existing review text")
        self.assertEqual(result["NegativePromptEN"], "existing negative")
        self.assertEqual(result["Notes"], "preserve this")
        self.assertEqual(result["Spec"]["ProcessSpec"]["BackgroundPolicy"], "auto_simple")


if __name__ == "__main__":
    unittest.main()
