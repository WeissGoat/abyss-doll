# -*- coding: utf-8 -*-
from __future__ import annotations

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

from art_background import review_candidate  # noqa: E402
from register_art_processing_round import register_processing_round, review_fingerprint  # noqa: E402


class RegisterArtProcessingRoundTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        root = Path(self.temp_dir.name)
        self.manifest_path = root / "manifest.json"
        self.incoming_root = root / "incoming"
        self.manifest_path.write_text(
            json.dumps(
                {
                    "Entries": [
                        {
                            "VisualID": "doll_zero_dialogue_neutral",
                            "ProductionProfile": "character_portrait_set",
                            "AssetType": "portrait",
                            "Status": "generated",
                            "Spec": {
                                "SourceSpec": {
                                    "Format": "png",
                                    "Width": 8,
                                    "Height": 8,
                                    "AlphaRequired": False,
                                },
                                "CompositionSpec": {"SafePaddingPercent": 0},
                                "ProcessSpec": {"BackgroundPolicy": "already_transparent"},
                            },
                        }
                    ]
                }
            ),
            encoding="utf-8",
        )
        self.workspace = self.incoming_root / "character_portraits" / "doll_zero_dialogue_neutral"

    def tearDown(self) -> None:
        self.temp_dir.cleanup()

    def read_manifest_entry(self) -> dict[str, object]:
        payload = json.loads(self.manifest_path.read_text(encoding="utf-8"))
        return payload["Entries"][0]

    def write_manifest_entry(self, entry: dict[str, object]) -> None:
        self.manifest_path.write_text(json.dumps({"Entries": [entry]}), encoding="utf-8")

    def register(
        self,
        staging: Path,
        *,
        dry_run: bool = False,
        allowed_override_rules: set[str] | None = None,
    ) -> dict[str, object]:
        return register_processing_round(
            manifest_path=self.manifest_path,
            incoming_root=self.incoming_root,
            visual_id="doll_zero_dialogue_neutral",
            staging_dir=staging,
            dry_run=dry_run,
            allowed_override_rules=allowed_override_rules or set(),
        )

    def make_staging(
        self,
        *,
        state: str,
        candidates: list[tuple[str, str, str]],
    ) -> Path:
        staging = Path(self.temp_dir.name) / f"staging_{len(list(Path(self.temp_dir.name).glob('staging_*')))}"
        staging.mkdir()
        decision_candidates = []
        technical_candidates = []
        entry = self.read_manifest_entry()
        spec = entry["Spec"]
        for filename, status, color in candidates:
            path = staging / filename
            Image.new("RGBA", (8, 8), color).save(path)
            technical = review_candidate(
                Image.open(path),
                source_spec=spec["SourceSpec"],
                composition_spec=spec["CompositionSpec"],
                process_spec=spec["ProcessSpec"],
                asset_type=str(entry.get("AssetType", "")),
                production_profile=str(entry["ProductionProfile"]),
                saved_path=path,
            )
            technical_record = {
                "File": filename,
                "SHA256": hashlib.sha256(path.read_bytes()).hexdigest(),
                **technical,
            }
            technical_record["ReviewFingerprint"] = review_fingerprint(technical_record)
            technical_candidates.append(technical_record)
            decision_candidates.append(
                {
                    "File": filename,
                    "Status": status,
                    "SHA256": hashlib.sha256(path.read_bytes()).hexdigest(),
                    "Width": 8,
                    "Height": 8,
                    "Format": "png",
                    "Reasons": [],
                }
            )
        (staging / "decision.json").write_text(
            json.dumps({"State": state, "Candidates": decision_candidates}),
            encoding="utf-8",
        )
        (staging / "process_report.json").write_text(json.dumps({"State": state}), encoding="utf-8")
        (staging / "technical_review.json").write_text(
            json.dumps(
                {
                    "SchemaVersion": "technical_review_v2",
                    "RuleSetVersion": "p3_art_technical_rules_002",
                    "VisualID": "doll_zero_dialogue_neutral",
                    "ProductionProfile": "character_portrait_set",
                    "Candidates": technical_candidates,
                }
            ),
            encoding="utf-8",
        )
        if state == "passed" and decision_candidates:
            (staging / "visual_review.json").write_text(
                json.dumps(
                    {
                        "VisualID": "doll_zero_dialogue_neutral",
                        "Candidate": decision_candidates[0]["File"],
                        "CandidateSHA256": decision_candidates[0]["SHA256"],
                        "HardGate": "passed",
                        "Scores": {"Total": 92},
                        "RecommendedAction": "select",
                    }
                ),
                encoding="utf-8",
            )
        else:
            (staging / "visual_review.json").write_text(json.dumps({"Status": state}), encoding="utf-8")
        return staging

    def test_registration_publishes_staging_as_next_numeric_round(self) -> None:
        staging = self.make_staging(state="passed", candidates=[("001.png", "passed", "red")])
        result = self.register(staging)
        self.assertEqual(result["RoundNumber"], 1)
        self.assertTrue((self.workspace / "processed" / "1" / "001.png").exists())

    def test_registration_rejects_hash_mismatch(self) -> None:
        staging = self.make_staging(state="passed", candidates=[("001.png", "passed", "red")])
        decision = json.loads((staging / "decision.json").read_text(encoding="utf-8"))
        decision["Candidates"][0]["SHA256"] = "0" * 64
        (staging / "decision.json").write_text(json.dumps(decision), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "SHA256"):
            self.register(staging)

    def test_registration_rejects_selected_or_approved_paths(self) -> None:
        staging = self.make_staging(state="passed", candidates=[("001.png", "passed", "red")])
        decision = json.loads((staging / "decision.json").read_text(encoding="utf-8"))
        decision["SelectedPath"] = "selected/001.png"
        (staging / "decision.json").write_text(json.dumps(decision), encoding="utf-8")
        with self.assertRaisesRegex(ValueError, "SelectedPath"):
            self.register(staging)

    def test_registration_rejects_forged_passed_technical_review(self) -> None:
        staging = self.make_staging(state="passed", candidates=[("001.png", "passed", "red")])
        review = json.loads((staging / "technical_review.json").read_text(encoding="utf-8"))
        review["Candidates"][0]["ReviewFingerprint"] = "forged"
        (staging / "technical_review.json").write_text(json.dumps(review), encoding="utf-8")

        with self.assertRaisesRegex(ValueError, "technical_review_mismatch"):
            self.register(staging)

    def test_registration_rejects_incomplete_portrait_visual_review(self) -> None:
        staging = self.make_staging(state="passed", candidates=[("001.png", "passed", "red")])
        (staging / "visual_review.json").write_text(
            json.dumps(
                {
                    "VisualID": "doll_zero_dialogue_neutral",
                    "Candidate": "001.png",
                    "HardGate": "passed",
                    "Scores": {"Total": 92},
                    "RecommendedAction": "select",
                }
            ),
            encoding="utf-8",
        )

        with self.assertRaisesRegex(ValueError, "visual_review_candidate_sha_missing"):
            self.register(staging)

    def test_registration_requires_explicit_override_authorization(self) -> None:
        entry = self.read_manifest_entry()
        entry["Spec"]["CompositionSpec"]["SafePaddingPercent"] = 25
        self.write_manifest_entry(entry)
        staging = self.make_staging(state="passed", candidates=[("001.png", "passed", "red")])
        review = json.loads((staging / "technical_review.json").read_text(encoding="utf-8"))
        candidate = review["Candidates"][0]
        self.assertIn("subject_outside_safe_canvas", candidate["Reasons"])
        override = {
            "SchemaVersion": "technical_override_v1",
            "VisualID": "doll_zero_dialogue_neutral",
            "Candidate": "001.png",
            "CandidateSHA256": candidate["SHA256"],
            "BaseReviewFingerprint": candidate["ReviewFingerprint"],
            "Overrides": [
                {
                    "RuleID": "subject_outside_safe_canvas",
                    "Action": "accept_as_warning",
                    "Reason": "Intentional full-canvas portrait crop.",
                    "Evidence": ["visual_review.json"],
                }
            ],
        }
        (staging / "technical_override.json").write_text(json.dumps(override), encoding="utf-8")

        with self.assertRaisesRegex(PermissionError, "technical_override_authorization_required"):
            self.register(staging)

        result = self.register(staging, allowed_override_rules={"subject_outside_safe_canvas"})
        decision = json.loads(
            (self.workspace / "processed" / str(result["RoundNumber"]) / "decision.json").read_text(encoding="utf-8")
        )
        self.assertEqual(decision["Candidates"][0]["AutomaticStatus"], "failed")
        self.assertEqual(decision["Candidates"][0]["Status"], "passed")
        self.assertEqual(decision["Candidates"][0]["AppliedOverrides"], ["subject_outside_safe_canvas"])

    def test_registration_rejects_non_overridable_dimension_failure(self) -> None:
        staging = self.make_staging(state="passed", candidates=[("001.png", "passed", "red")])
        Image.new("RGBA", (4, 8), "red").save(staging / "001.png")
        review = json.loads((staging / "technical_review.json").read_text(encoding="utf-8"))
        entry = self.read_manifest_entry()
        path = staging / "001.png"
        with Image.open(path) as image:
            image.load()
            technical = review_candidate(
                image,
                source_spec=entry["Spec"]["SourceSpec"],
                composition_spec=entry["Spec"]["CompositionSpec"],
                process_spec=entry["Spec"]["ProcessSpec"],
                asset_type=str(entry.get("AssetType", "")),
                production_profile=str(entry["ProductionProfile"]),
                saved_path=path,
            )
        candidate = {
            "File": "001.png",
            "SHA256": hashlib.sha256(path.read_bytes()).hexdigest(),
            **technical,
        }
        candidate["ReviewFingerprint"] = review_fingerprint(candidate)
        review["Candidates"][0] = candidate
        (staging / "technical_review.json").write_text(json.dumps(review), encoding="utf-8")
        decision = json.loads((staging / "decision.json").read_text(encoding="utf-8"))
        decision["Candidates"][0].update({"SHA256": candidate["SHA256"], "Width": 4})
        (staging / "decision.json").write_text(json.dumps(decision), encoding="utf-8")
        visual_review = json.loads((staging / "visual_review.json").read_text(encoding="utf-8"))
        visual_review["CandidateSHA256"] = candidate["SHA256"]
        (staging / "visual_review.json").write_text(json.dumps(visual_review), encoding="utf-8")
        (staging / "technical_override.json").write_text(
            json.dumps(
                {
                    "SchemaVersion": "technical_override_v1",
                    "VisualID": "doll_zero_dialogue_neutral",
                    "Candidate": "001.png",
                    "CandidateSHA256": candidate["SHA256"],
                    "BaseReviewFingerprint": candidate["ReviewFingerprint"],
                    "Overrides": [
                        {
                            "RuleID": "wrong_dimensions",
                            "Action": "accept_as_warning",
                            "Reason": "Invalid attempted override.",
                            "Evidence": ["visual_review.json"],
                        }
                    ],
                }
            ),
            encoding="utf-8",
        )

        with self.assertRaisesRegex(ValueError, "technical_override_not_allowed"):
            self.register(staging, allowed_override_rules={"wrong_dimensions"})

    def test_dry_run_writes_nothing(self) -> None:
        staging = self.make_staging(state="passed", candidates=[("001.png", "passed", "red")])
        result = self.register(staging, dry_run=True)
        self.assertEqual(result["RoundNumber"], 1)
        self.assertFalse((self.workspace / "processed").exists())


if __name__ == "__main__":
    unittest.main()
