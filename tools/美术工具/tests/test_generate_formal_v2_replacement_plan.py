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

    def entry(self, visual_id: str) -> dict:
        manifest = json.loads(self.manifest.read_text(encoding="utf-8"))
        return next(item for item in manifest["Entries"] if item["VisualID"] == visual_id)

    def write_entry(self, replacement: dict) -> None:
        manifest = json.loads(self.manifest.read_text(encoding="utf-8"))
        manifest["Entries"] = [
            replacement if item["VisualID"] == replacement["VisualID"] else item
            for item in manifest["Entries"]
        ]
        self.manifest.write_text(json.dumps(manifest), encoding="utf-8")

    def attach_catalog_v2_request(
        self,
        visual_id: str,
        *,
        ready_formats: tuple[str, ...] = ("natural_language_v2", "danbooru_tags_v2"),
    ) -> dict:
        entry = self.entry(visual_id)
        request_id = f"{visual_id}@requirement"
        requirement_fingerprint = f"requirement-{visual_id}"
        revision_id = f"{request_id}/prompt-001"
        request = {
            "VisualID": visual_id,
            "RequestID": request_id,
            "RequirementStatus": "ready",
            "RequirementFingerprint": requirement_fingerprint,
            "PromptAuthoringStatus": "prompt_ready",
            "ActivePromptRevisionID": revision_id,
            "PromptRevisions": [
                {
                    "PromptRevisionID": revision_id,
                    "RequirementFingerprint": requirement_fingerprint,
                    "Status": "ready",
                    "RevisionFingerprint": f"revision-{visual_id}",
                    "Variants": {
                        format_id: {"Format": format_id, "Status": "ready"}
                        for format_id in ready_formats
                    },
                }
            ],
        }
        entry["CompiledRequest"] = {
            "RequestID": request_id,
            "RequirementFingerprint": requirement_fingerprint,
            "PromptAuthoringStatus": "prompt_ready",
            "ActivePromptRevisionID": revision_id,
        }
        self.write_entry(entry)
        return request

    def write_catalog(self, requests: list[dict]) -> Path:
        catalog = self.root / "requests.json"
        catalog.write_text(json.dumps({"Requests": requests}), encoding="utf-8")
        return catalog

    def build_single_item(self, request: dict | None = None) -> dict:
        catalog = self.write_catalog([request]) if request is not None else None
        payload = build_payload(
            overview_path=self.overview,
            manifest_path=self.manifest,
            batch_id="formalv2_catalog_v2_test",
            variants=2,
            delay_seconds=1.0,
            visual_ids={"ui_button_primary"},
            request_catalog_path=catalog,
        )
        return payload["Items"][0]

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

    def test_payload_persists_exact_active_catalog_v2_evidence(self) -> None:
        request = self.attach_catalog_v2_request("ui_button_primary")

        item = self.build_single_item(request)

        self.assertEqual(item["RequestID"], "ui_button_primary@requirement")
        self.assertEqual(item["RequirementFingerprint"], "requirement-ui_button_primary")
        self.assertEqual(item["PromptAuthoringStatus"], "prompt_ready")
        self.assertEqual(item["PromptRevisionID"], "ui_button_primary@requirement/prompt-001")
        self.assertEqual(item["PromptRevisionFingerprint"], "revision-ui_button_primary")
        self.assertEqual(set(item["PromptFormats"]), {"natural_language_v2", "danbooru_tags_v2"})
        self.assertTrue(item["PromptReady"])
        self.assertEqual(item["PromptBlockReason"], "")
        self.assertNotIn("RequestFingerprint", item)

    def test_payload_accepts_danbooru_only_active_revision(self) -> None:
        request = self.attach_catalog_v2_request(
            "ui_button_primary",
            ready_formats=("danbooru_tags_v2",),
        )

        item = self.build_single_item(request)

        self.assertTrue(item["PromptReady"])
        self.assertEqual(item["PromptFormats"], ["danbooru_tags_v2"])

    def test_payload_reports_catalog_v2_block_reasons(self) -> None:
        base_request = self.attach_catalog_v2_request("ui_button_primary")
        cases = []

        request = json.loads(json.dumps(base_request))
        request["RequirementStatus"] = "invalid"
        cases.append(("requirement_not_ready", request, None))

        request = json.loads(json.dumps(base_request))
        request["PromptAuthoringStatus"] = "prompt_authoring_required"
        request["ActivePromptRevisionID"] = ""
        request["PromptRevisions"] = []
        cases.append(("prompt_authoring_required", request, {
            "PromptAuthoringStatus": "prompt_authoring_required",
            "ActivePromptRevisionID": "",
        }))

        for field in (
            "RequestID",
            "RequirementFingerprint",
            "PromptAuthoringStatus",
            "ActivePromptRevisionID",
        ):
            cases.append((f"manifest_pointer_mismatch:{field}", base_request, {field: "mismatch"}))

        request = json.loads(json.dumps(base_request))
        request["PromptRevisions"] = []
        cases.append(("active_prompt_revision_missing", request, None))

        request = json.loads(json.dumps(base_request))
        request["PromptRevisions"][0]["RequirementFingerprint"] = "stale"
        cases.append(("prompt_revision_stale", request, None))

        request = json.loads(json.dumps(base_request))
        request["PromptRevisions"][0]["Status"] = "invalid"
        cases.append(("prompt_revision_not_ready", request, None))

        request = json.loads(json.dumps(base_request))
        request["PromptRevisions"][0]["Variants"] = {
            "natural_language_v2": {"Format": "natural_language_v2", "Status": "unsupported"},
            "danbooru_tags_v2": {"Format": "danbooru_tags_v2", "Status": "invalid"},
        }
        cases.append(("prompt_revision_no_ready_v2_variant", request, None))

        for expected_reason, request, compiled_changes in cases:
            with self.subTest(expected_reason=expected_reason):
                entry = self.entry("ui_button_primary")
                entry["CompiledRequest"] = {
                    "RequestID": base_request["RequestID"],
                    "RequirementFingerprint": base_request["RequirementFingerprint"],
                    "PromptAuthoringStatus": base_request["PromptAuthoringStatus"],
                    "ActivePromptRevisionID": base_request["ActivePromptRevisionID"],
                }
                if compiled_changes:
                    entry["CompiledRequest"].update(compiled_changes)
                self.write_entry(entry)

                item = self.build_single_item(request)

                self.assertFalse(item["PromptReady"])
                self.assertEqual(item["PromptBlockReason"], expected_reason)

    def test_payload_blocks_when_catalog_request_is_missing(self) -> None:
        item = self.build_single_item()

        self.assertFalse(item["PromptReady"])
        self.assertEqual(item["PromptBlockReason"], "catalog_request_missing")
        self.assertEqual(item["PromptFormats"], [])

    def test_unrecognized_prompt_payload_is_not_formal_input(self) -> None:
        entry = self.entry("ui_button_primary")
        entry["CompiledRequest"] = {
            "RequestID": "button@legacy",
            "RequestFingerprint": "legacy-fingerprint",
        }
        self.write_entry(entry)
        request = {
            "VisualID": "ui_button_primary",
            "RequestID": "button@legacy",
            "RequestFingerprint": "legacy-fingerprint",
            "UnsupportedPayload": {"status": "ready"},
        }

        item = self.build_single_item(request)

        self.assertFalse(item["PromptReady"])
        self.assertEqual(item["PromptFormats"], [])
        self.assertEqual(item["PromptBlockReason"], "requirement_not_ready")
        self.assertNotIn("UnsupportedPayload", json.dumps(item))


if __name__ == "__main__":
    unittest.main()
