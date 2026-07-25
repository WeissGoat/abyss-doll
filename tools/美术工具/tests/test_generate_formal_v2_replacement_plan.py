# -*- coding: utf-8 -*-
from __future__ import annotations

import json
import sys
import tempfile
import unittest
from pathlib import Path


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from generate_formal_v2_replacement_plan import (  # noqa: E402
    build_payload,
    parse_formal_v2_overview,
)


class FormalV2ReplacementPlanTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        self.root = Path(self.temp_dir.name)
        self.overview = self.root / "formal_v2.md"
        self.manifest = self.root / "manifest.json"
        self.overview.write_text(
            """
### 4.4 Formal V2 UI Skin 资产清单
| 资产组 | 代表 VisualID / 组件 | Domain | Type | 状态 |
|---|---|---|---|---|
| 面板 | `ui_panel_main`、`ui_button_primary`、`ui_icon_money` | ui | panel | V2-A active |

### 4.5 Formal V2 场景 / 背景资产清单
| 场景组 | ScreenID | Runtime VisualID | 尺寸 | 状态 |
|---|---|---|---|---|
| 工坊 | `workshop_main` | `bg_workshop_day` | 1920x1080 | V2-A active |
| 草案 | `draft_screen` | `bg_draft_only` | 1920x1080 | draft planning |
""".strip(),
            encoding="utf-8",
        )
        approved = self.root / "approved"
        approved.mkdir()
        entries = []
        for visual_id, domain, asset_type in (
            ("ui_panel_main", "ui", "panel"),
            ("ui_button_primary", "ui", "button"),
            ("ui_icon_money", "ui", "icon"),
            ("bg_workshop_day", "background", "background"),
            ("bg_draft_only", "background", "background"),
        ):
            path = approved / f"{visual_id}.png"
            path.write_bytes(b"placeholder")
            entries.append(
                {
                    "VisualID": visual_id,
                    "Domain": domain,
                    "AssetType": asset_type,
                    "Status": "approved",
                    "Priority": "P0",
                    "ApprovedPath": str(path),
                    "PromptEN": "a specific prompt for this runtime asset",
                    "NegativePromptEN": "text, watermark",
                    "Spec": {
                        "SourceSpec": {"Format": "png", "Width": 64, "Height": 64},
                        "ProcessSpec": {
                            "BackgroundPolicy": "preserve",
                            "NineSlice": {"Enabled": asset_type in {"panel", "button"}},
                        },
                    },
                }
            )
        self.manifest.write_text(json.dumps({"Entries": entries}), encoding="utf-8")

    def tearDown(self) -> None:
        self.temp_dir.cleanup()

    def test_parser_excludes_draft_rows_and_returns_asset_classes(self) -> None:
        result = parse_formal_v2_overview(self.overview)
        self.assertEqual(
            set(result),
            {"ui_panel_main", "ui_button_primary", "ui_icon_money", "bg_workshop_day"},
        )
        self.assertEqual(result["ui_panel_main"], "ui_skin")
        self.assertEqual(result["bg_workshop_day"], "background")

    def test_payload_emits_method_neutral_replacement_items(self) -> None:
        payload = build_payload(
            overview_path=self.overview,
            manifest_path=self.manifest,
            batch_id="formalv2_replacement_test",
            variants=2,
            delay_seconds=1.0,
        )
        self.assertEqual(payload["Summary"]["PlannedItems"], 4)
        self.assertEqual(
            {item["VisualID"] for item in payload["Items"]},
            {"ui_panel_main", "ui_button_primary", "ui_icon_money", "bg_workshop_day"},
        )
        self.assertTrue(all(item["Action"] == "visual_v2_replace" for item in payload["Items"]))
        self.assertEqual(
            {item["AssetClass"] for item in payload["Items"]},
            {"ui_skin", "icon", "background"},
        )
        icon = next(item for item in payload["Items"] if item["VisualID"] == "ui_icon_money")
        self.assertEqual(icon["AssetClass"], "icon")
        self.assertNotIn("Provider", payload["Items"][0])

    def test_payload_can_be_bounded_to_one_visual_id(self) -> None:
        payload = build_payload(
            overview_path=self.overview,
            manifest_path=self.manifest,
            batch_id="formalv2_button_repair",
            variants=2,
            delay_seconds=1.0,
            visual_ids={"ui_button_primary"},
        )
        self.assertEqual(payload["Summary"]["RequestedVisualIDs"], ["ui_button_primary"])
        self.assertEqual(payload["Summary"]["PlannedItems"], 1)
        self.assertEqual(payload["Items"][0]["VisualID"], "ui_button_primary")

    def test_payload_includes_compiled_request_pointer_when_catalog_is_provided(self) -> None:
        catalog = self.root / "requests.json"
        catalog.write_text(
            json.dumps(
                {
                    "Requests": [
                        {
                            "VisualID": "ui_button_primary",
                            "RequestID": "button@abc",
                            "RequestFingerprint": "fingerprint",
                            "CompileStatus": "ready",
                            "PromptVariants": {
                                "natural_language_v1": {"CompileStatus": "ready"},
                                "danbooru_tags_v1": {"CompileStatus": "ready"},
                            },
                        }
                    ]
                }
            ),
            encoding="utf-8",
        )
        manifest = json.loads(self.manifest.read_text(encoding="utf-8"))
        button = next(item for item in manifest["Entries"] if item["VisualID"] == "ui_button_primary")
        button["CompiledRequest"] = {"RequestID": "button@abc", "RequestFingerprint": "fingerprint"}
        self.manifest.write_text(json.dumps(manifest), encoding="utf-8")

        payload = build_payload(
            overview_path=self.overview,
            manifest_path=self.manifest,
            batch_id="formalv2_compiled_test",
            variants=2,
            delay_seconds=1.0,
            visual_ids={"ui_button_primary"},
            request_catalog_path=catalog,
        )

        item = payload["Items"][0]
        self.assertEqual(item["RequestID"], "button@abc")
        self.assertEqual(item["RequestFingerprint"], "fingerprint")
        self.assertEqual(set(item["PromptFormats"]), {"natural_language_v1", "danbooru_tags_v1"})


if __name__ == "__main__":
    unittest.main()
