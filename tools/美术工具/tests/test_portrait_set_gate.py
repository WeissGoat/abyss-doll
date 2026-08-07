# -*- coding: utf-8 -*-
from __future__ import annotations

import copy
import hashlib
import json
import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from portrait_set_gate import (  # noqa: E402
    build_set_snapshot,
    finalize_set_review,
    prepare_set_review,
    require_current_set_review,
)


MEMBER_CHECKS = ("Identity", "Costume", "Proportion", "Framing", "Technical", "TargetFit", "RapidSwitch")
CROSS_GROUP_CHECKS = ("Identity", "Costume", "Proportion", "RenderingDirection")


class PortraitSetGateTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        self.project_root = Path(self.temp_dir.name)
        self.manifest_path = self.project_root / "manifest.json"
        self.incoming_root = self.project_root / "incoming"
        self.evidence_root = self.project_root / "evidence"
        self.write_manifest()

    def tearDown(self) -> None:
        self.temp_dir.cleanup()

    def write_manifest(self) -> None:
        members = [
            ("portrait_a", "dialogue_standing", "red"),
            ("portrait_b", "dialogue_standing", "blue"),
            ("portrait_sit", "maintenance_seated", "green"),
        ]
        entries = []
        for visual_id, group, color in members:
            selected = self.incoming_root / "character_portraits" / visual_id / "selected" / "001.png"
            selected.parent.mkdir(parents=True, exist_ok=True)
            Image.new("RGBA", (16, 24), color).save(selected)
            entries.append(
                {
                    "VisualID": visual_id,
                    "ProductionProfile": "character_portrait_set",
                    "AssetSetID": "demo_portraits",
                    "PresentationGroup": group,
                    "SelectedPath": selected.relative_to(self.project_root).as_posix(),
                }
            )
        self.manifest_path.write_text(
            json.dumps(
                {
                    "Entries": entries,
                    "AssetSets": {
                        "demo_portraits": {
                            "IdentityContract": {
                                "Version": "1",
                                "Required": ["silver hair"],
                                "Forbidden": ["red glow"],
                                "Conditional": ["sitting pose may use seated framing"],
                            }
                        }
                    },
                }
            ),
            encoding="utf-8",
        )

    def load_manifest(self) -> dict:
        return json.loads(self.manifest_path.read_text(encoding="utf-8"))

    def prepare(self) -> dict:
        return prepare_set_review(
            manifest_path=self.manifest_path,
            incoming_root=self.incoming_root,
            evidence_root=self.evidence_root,
            asset_set_id="demo_portraits",
            production_run_id="portrait_set_test_01",
        )

    def valid_set_review(self, prepared: dict) -> dict:
        groups = {}
        for group in sorted({member["PresentationGroup"] for member in prepared["Members"]}):
            groups[group] = {
                "Checks": {
                    name: {"Status": "passed", "Finding": f"{group} {name} conclusion."}
                    for name in MEMBER_CHECKS
                }
            }
        return {
            "Schema": "p3-portrait-set-review@1",
            "ProductionRunID": "portrait_set_test_01",
            "AssetSetID": "demo_portraits",
            "SetSnapshotFingerprint": prepared["SetSnapshotFingerprint"],
            "State": "passed",
            "Members": [
                {
                    "VisualID": item["VisualID"],
                    "SelectedSHA256": item["SelectedSHA256"],
                    "PresentationGroup": item["PresentationGroup"],
                    "Status": "passed",
                    "Finding": f"{item['VisualID']} is compatible with the set.",
                }
                for item in prepared["Members"]
            ],
            "Groups": groups,
            "CrossGroupChecks": {
                name: {"Status": "passed", "Finding": f"Cross-group {name} conclusion."}
                for name in CROSS_GROUP_CHECKS
            },
            "FailedMembers": [],
        }

    def write_review(self, review: dict) -> Path:
        path = self.evidence_root / "portrait_set_test_01" / "portrait-set-gate" / "consistency-review.json"
        path.parent.mkdir(parents=True, exist_ok=True)
        path.write_text(json.dumps(review, ensure_ascii=False), encoding="utf-8")
        return path

    def finalize(self, review_path: Path) -> dict:
        return finalize_set_review(
            manifest_path=self.manifest_path,
            incoming_root=self.incoming_root,
            asset_set_id="demo_portraits",
            production_run_id="portrait_set_test_01",
            review_path=review_path,
        )

    def test_build_prepare_and_finalize_current_snapshot(self) -> None:
        manifest = self.load_manifest()
        snapshot = build_set_snapshot(manifest, "demo_portraits", self.incoming_root, self.project_root)
        self.assertEqual([item["VisualID"] for item in snapshot["Members"]], ["portrait_a", "portrait_b", "portrait_sit"])
        self.assertEqual(len(snapshot["SetSnapshotFingerprint"]), 64)

        prepared = self.prepare()
        self.assertTrue(Path(prepared["Evidence"]["ContactSheet"]).is_file())
        self.assertTrue(Path(prepared["Evidence"]["SmallSizeStrip"]).is_file())
        review_path = self.write_review(self.valid_set_review(prepared))
        finalized = self.finalize(review_path)

        self.assertEqual(finalized["State"], "passed")
        latest = self.load_manifest()["AssetSets"]["demo_portraits"]["LatestConsistencyReview"]
        self.assertEqual(latest["SetSnapshotFingerprint"], prepared["SetSnapshotFingerprint"])
        self.assertEqual(
            require_current_set_review(
                manifest=self.load_manifest(),
                asset_set_id="demo_portraits",
                incoming_root=self.incoming_root,
                project_root=self.project_root,
            )["State"],
            "passed",
        )

    def test_current_review_becomes_stale_when_selected_bytes_change(self) -> None:
        prepared = self.prepare()
        review_path = self.write_review(self.valid_set_review(prepared))
        selected = self.incoming_root / "character_portraits" / "portrait_a" / "selected" / "001.png"
        Image.new("RGBA", (16, 24), "purple").save(selected)
        with self.assertRaisesRegex(ValueError, "portrait_set_review_stale"):
            self.finalize(review_path)

    def test_snapshot_rejects_missing_selected_member(self) -> None:
        selected = self.incoming_root / "character_portraits" / "portrait_sit" / "selected" / "001.png"
        selected.unlink()
        with self.assertRaisesRegex(ValueError, "portrait_set_member_selected_missing:portrait_sit"):
            build_set_snapshot(self.load_manifest(), "demo_portraits", self.incoming_root, self.project_root)

    def test_finalize_requires_exact_member_and_check_coverage(self) -> None:
        prepared = self.prepare()
        review = self.valid_set_review(prepared)
        review["Members"].pop()
        with self.assertRaisesRegex(ValueError, "portrait_set_review_members_mismatch"):
            self.finalize(self.write_review(review))

        review = self.valid_set_review(prepared)
        review["Members"][0]["SelectedSHA256"] = "0" * 64
        with self.assertRaisesRegex(ValueError, "portrait_set_review_members_mismatch"):
            self.finalize(self.write_review(review))

        review = self.valid_set_review(prepared)
        review["Groups"]["dialogue_standing"]["Checks"].pop("RapidSwitch")
        with self.assertRaisesRegex(ValueError, "portrait_set_review_group_checks_missing"):
            self.finalize(self.write_review(review))

        review = self.valid_set_review(prepared)
        review["Members"][0]["Status"] = "failed"
        with self.assertRaisesRegex(ValueError, "portrait_set_review_members_not_passed"):
            self.finalize(self.write_review(review))

    def test_identity_and_presentation_group_change_fingerprint_and_stale_review(self) -> None:
        prepared = self.prepare()
        original = build_set_snapshot(self.load_manifest(), "demo_portraits", self.incoming_root, self.project_root)
        changed_identity = self.load_manifest()
        changed_identity["AssetSets"]["demo_portraits"]["IdentityContract"]["Required"].append("white blindfold")
        identity_snapshot = build_set_snapshot(changed_identity, "demo_portraits", self.incoming_root, self.project_root)
        self.assertNotEqual(original["SetSnapshotFingerprint"], identity_snapshot["SetSnapshotFingerprint"])

        changed_group = copy.deepcopy(self.load_manifest())
        changed_group["Entries"][0]["PresentationGroup"] = "front_standing"
        group_snapshot = build_set_snapshot(changed_group, "demo_portraits", self.incoming_root, self.project_root)
        self.assertNotEqual(original["SetSnapshotFingerprint"], group_snapshot["SetSnapshotFingerprint"])

        review_path = self.write_review(self.valid_set_review(prepared))
        self.finalize(review_path)
        manifest = self.load_manifest()
        manifest["Entries"][0]["PresentationGroup"] = "front_standing"
        self.manifest_path.write_text(json.dumps(manifest), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "portrait_set_review_stale"):
            require_current_set_review(
                manifest=self.load_manifest(),
                asset_set_id="demo_portraits",
                incoming_root=self.incoming_root,
                project_root=self.project_root,
            )


if __name__ == "__main__":
    unittest.main()
