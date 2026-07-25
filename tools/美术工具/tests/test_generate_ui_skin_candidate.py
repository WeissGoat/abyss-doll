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

from art_background import review_candidate  # noqa: E402
from generate_ui_skin_candidate import generate_ui_skin_candidates  # noqa: E402


def button_entry() -> dict[str, object]:
    return {
        "VisualID": "ui_button_primary",
        "Status": "approved",
        "ProductionProfile": "standard_asset",
        "AssetType": "button",
        "Domain": "ui",
        "Spec": {
            "SourceSpec": {
                "Format": "png",
                "Width": 512,
                "Height": 160,
                "Background": "transparent",
                "AlphaRequired": True,
            },
            "CompositionSpec": {
                "SafePaddingPercent": 4,
                "SubjectOccupancyMin": 0.88,
                "SubjectOccupancyMax": 0.98,
                "Anchor": "center",
            },
            "ProcessSpec": {
                "BackgroundPolicy": "auto_simple",
                "NineSlice": {
                    "Enabled": True,
                    "Border": {"Left": 72, "Right": 72, "Top": 48, "Bottom": 48},
                },
            },
        },
    }


class GenerateUISkinCandidateTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        self.root = Path(self.temp_dir.name)
        self.manifest_path = self.root / "manifest.json"
        self.incoming_root = self.root / "incoming"
        self.manifest_path.write_text(
            json.dumps({"Entries": [button_entry()]}),
            encoding="utf-8",
        )

    def tearDown(self) -> None:
        self.temp_dir.cleanup()

    def test_generates_two_decodable_contract_safe_candidates_and_preserves_status(self) -> None:
        result = generate_ui_skin_candidates(
            project_root=self.root,
            manifest_path=self.manifest_path,
            incoming_root=self.incoming_root,
            visual_ids={"ui_button_primary"},
            batch_id="ui_skin_button_01",
            variants=2,
            capability="deterministic_template",
            dry_run=False,
        )

        self.assertEqual(result["Generated"], 1)
        workspace = self.incoming_root / "standard_assets" / "ui_button_primary"
        raw_files = sorted((workspace / "raw").glob("*.png"))
        self.assertEqual(len(raw_files), 2)
        entry = json.loads(self.manifest_path.read_text(encoding="utf-8"))["Entries"][0]
        self.assertEqual(entry["Status"], "approved")
        self.assertEqual(entry["CandidateBatchID"], "ui_skin_button_01")
        self.assertEqual(len(entry["CandidateRawFiles"]), 2)
        generation = json.loads((workspace / "generation.json").read_text(encoding="utf-8"))
        self.assertEqual(generation["Capability"], "deterministic_template")
        self.assertEqual(generation["BatchID"], "ui_skin_button_01")

        for path in raw_files:
            with Image.open(path) as image:
                image.load()
                self.assertEqual(image.size, (512, 160))
                self.assertEqual(image.mode, "RGBA")
                review = review_candidate(
                    image,
                    source_spec=entry["Spec"]["SourceSpec"],
                    composition_spec=entry["Spec"]["CompositionSpec"],
                    process_spec=entry["Spec"]["ProcessSpec"],
                    asset_type="button",
                    production_profile="standard_asset",
                    saved_path=path,
                )
            self.assertEqual(review["Status"], "passed", review)

    def test_rejects_non_nine_slice_entry_or_unknown_capability(self) -> None:
        manifest = json.loads(self.manifest_path.read_text(encoding="utf-8"))
        manifest["Entries"][0]["Spec"]["ProcessSpec"]["NineSlice"]["Enabled"] = False
        self.manifest_path.write_text(json.dumps(manifest), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "NineSlice"):
            generate_ui_skin_candidates(
                project_root=self.root,
                manifest_path=self.manifest_path,
                incoming_root=self.incoming_root,
                visual_ids={"ui_button_primary"},
                batch_id="ui_skin_button_01",
                variants=1,
                capability="deterministic_template",
                dry_run=False,
            )

        with self.assertRaisesRegex(ValueError, "Unsupported UI Skin capability"):
            generate_ui_skin_candidates(
                project_root=self.root,
                manifest_path=self.manifest_path,
                incoming_root=self.incoming_root,
                visual_ids={"ui_button_primary"},
                batch_id="ui_skin_button_01",
                variants=1,
                capability="unknown",
                dry_run=True,
            )

    def test_dry_run_writes_nothing(self) -> None:
        before = self.manifest_path.read_bytes()
        result = generate_ui_skin_candidates(
            project_root=self.root,
            manifest_path=self.manifest_path,
            incoming_root=self.incoming_root,
            visual_ids={"ui_button_primary"},
            batch_id="ui_skin_button_01",
            variants=2,
            capability="deterministic_template",
            dry_run=True,
        )
        self.assertEqual(result["Planned"], 1)
        self.assertEqual(self.manifest_path.read_bytes(), before)
        self.assertFalse(self.incoming_root.exists())


if __name__ == "__main__":
    unittest.main()
