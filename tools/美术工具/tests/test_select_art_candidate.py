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

from select_art_candidate import select_art_candidate  # noqa: E402


def repo_path(path: Path, root: Path) -> str:
    return path.resolve().relative_to(root.resolve()).as_posix()


class SelectArtCandidateTests(unittest.TestCase):
    def setUp(self) -> None:
        self.temp_dir = tempfile.TemporaryDirectory()
        self.root = Path(self.temp_dir.name)
        self.manifest_path = self.root / "manifest.json"
        self.incoming_root = self.root / "incoming"
        self.workspace = self.incoming_root / "standard_assets" / "bg_workshop_day"
        self.round_dir = self.workspace / "processed" / "2"
        self.round_dir.mkdir(parents=True)
        self.candidate = self.round_dir / "001.png"
        Image.new("RGBA", (8, 4), (10, 20, 30, 255)).save(self.candidate)
        candidate_record = {
            "File": self.candidate.name,
            "Input": "incoming/standard_assets/bg_workshop_day/raw/001.png",
            "Status": "passed",
            "SHA256": hashlib.sha256(self.candidate.read_bytes()).hexdigest(),
            "Width": 8,
            "Height": 4,
            "Format": "png",
            "Reasons": [],
        }
        (self.round_dir / "decision.json").write_text(
            json.dumps({"State": "passed", "Candidates": [candidate_record]}),
            encoding="utf-8",
        )
        self.review_path = self.root / "visual-review.json"
        self.write_review(score=92)

    def tearDown(self) -> None:
        self.temp_dir.cleanup()

    def write_manifest(self, status: str = "generated", selected_path: str = "") -> None:
        self.manifest_path.write_text(
            json.dumps(
                {
                    "Entries": [
                        {
                            "VisualID": "bg_workshop_day",
                            "ProductionProfile": "standard_asset",
                            "Status": status,
                            "SelectedPath": selected_path,
                        }
                    ]
                }
            ),
            encoding="utf-8",
        )

    def write_review(
        self,
        score: int,
        candidate: Path | None = None,
        *,
        replacement_target: Path | None = None,
        baseline_score: int = 90,
        candidate_dimensions: dict[str, int] | None = None,
        baseline_dimensions: dict[str, int] | None = None,
        protected_dimensions: list[str] | None = None,
    ) -> None:
        selected = candidate or self.candidate
        scores = {"Total": score, **(candidate_dimensions or {})}
        item = {
            "VisualID": "bg_workshop_day",
            "Candidate": str(selected),
            "CandidateSHA256": hashlib.sha256(selected.read_bytes()).hexdigest(),
            "HardGate": "passed",
            "Scores": scores,
            "RecommendedAction": "select",
        }
        if replacement_target is not None:
            item.update(
                {
                    "SelectionMode": "replacement",
                    "ReviewRubricVersion": "standard_asset_review_002",
                    "ReplacementBaseline": {
                        "SelectedPath": str(replacement_target),
                        "SelectedSHA256": hashlib.sha256(replacement_target.read_bytes()).hexdigest(),
                        "Scores": {"Total": baseline_score, **(baseline_dimensions or {})},
                    },
                    "ReplacementPolicy": {
                        "MinimumScore": 88,
                        "MustExceedExisting": True,
                        "MinimumScoreDelta": 1,
                        "ProtectedDimensions": protected_dimensions or [],
                    },
                }
            )
        self.review_path.write_text(
            json.dumps(
                {
                    "ProductionRunID": "formalv2_batch_01",
                    "Items": [item],
                }
            ),
            encoding="utf-8",
        )

    def test_selects_latest_passed_candidate_and_updates_manifest(self) -> None:
        self.write_manifest()
        (self.root / "summary.json").write_text(
            json.dumps(
                {
                    "ProductionRunID": "formalv2_batch_01",
                    "FinalState": "review_required",
                    "Claims": {"bg_workshop_day": "review_required"},
                    "ApprovedChanged": False,
                }
            ),
            encoding="utf-8",
        )

        result = select_art_candidate(
            project_root=self.root,
            manifest_path=self.manifest_path,
            incoming_root=self.incoming_root,
            visual_id="bg_workshop_day",
            review_path=self.review_path,
            allow_selected_overwrite=False,
            dry_run=False,
        )

        target = self.workspace / "selected" / "bg_workshop_day.png"
        self.assertTrue(target.exists())
        self.assertEqual(target.read_bytes(), self.candidate.read_bytes())
        manifest = json.loads(self.manifest_path.read_text(encoding="utf-8"))
        self.assertEqual(manifest["Entries"][0]["Status"], "selected")
        self.assertEqual(manifest["Entries"][0]["SelectedPath"], repo_path(target, self.root))
        self.assertEqual(result["ProcessedRound"], 2)
        self.assertTrue((self.workspace / "production_decision.json").exists())
        summary = json.loads((self.root / "summary.json").read_text(encoding="utf-8"))
        self.assertEqual(summary["FinalState"], "selection_complete")
        self.assertEqual(summary["Claims"]["bg_workshop_day"], "selected")

    def test_approved_replacement_preserves_status_and_requires_overwrite_permission(self) -> None:
        target = self.workspace / "selected" / "bg_workshop_day.png"
        target.parent.mkdir(parents=True)
        Image.new("RGBA", (8, 4), "red").save(target)
        self.write_manifest(status="approved", selected_path=repo_path(target, self.root))
        self.write_review(score=92, replacement_target=target, baseline_score=90)

        with self.assertRaisesRegex(PermissionError, "selected overwrite"):
            select_art_candidate(
                project_root=self.root,
                manifest_path=self.manifest_path,
                incoming_root=self.incoming_root,
                visual_id="bg_workshop_day",
                review_path=self.review_path,
                allow_selected_overwrite=False,
                dry_run=False,
            )

        result = select_art_candidate(
            project_root=self.root,
            manifest_path=self.manifest_path,
            incoming_root=self.incoming_root,
            visual_id="bg_workshop_day",
            review_path=self.review_path,
            allow_selected_overwrite=True,
            dry_run=False,
        )
        manifest = json.loads(self.manifest_path.read_text(encoding="utf-8"))
        self.assertEqual(manifest["Entries"][0]["Status"], "approved")
        self.assertEqual(target.read_bytes(), self.candidate.read_bytes())
        self.assertEqual(result["SelectionMode"], "replacement")
        self.assertEqual(result["PreviousSelected"]["Score"], 90)
        self.assertTrue(result["PolicyResult"]["StrictlyBetterPassed"])
        selection = json.loads((self.root / "selection-decision.json").read_text(encoding="utf-8"))
        selected = selection["Selected"][0]
        self.assertEqual(selected["SelectionMode"], "replacement")
        self.assertEqual(selected["PreviousSelected"]["SHA256"], result["PreviousSelected"]["SHA256"])
        self.assertEqual(selected["PolicyResult"], result["PolicyResult"])

    def test_updates_nested_batch_summary_by_production_run_id(self) -> None:
        self.write_manifest()
        nested_summary = self.root / "batch" / "formalv2_batch_01" / "summary.json"
        nested_summary.parent.mkdir(parents=True)
        nested_summary.write_text(
            json.dumps(
                {
                    "ProductionRunID": "formalv2_batch_01",
                    "FinalState": "review_required",
                    "Claims": {"bg_workshop_day": "review_required"},
                    "ApprovedChanged": False,
                }
            ),
            encoding="utf-8",
        )
        self.review_path.write_text(
            json.dumps(
                {
                    "ProductionRunID": "formalv2_batch_01",
                    "Items": [json.loads(self.review_path.read_text(encoding="utf-8"))["Items"][0]],
                }
            ),
            encoding="utf-8",
        )

        select_art_candidate(
            project_root=self.root,
            manifest_path=self.manifest_path,
            incoming_root=self.incoming_root,
            visual_id="bg_workshop_day",
            review_path=self.review_path,
            allow_selected_overwrite=False,
            dry_run=False,
        )

        summary = json.loads(nested_summary.read_text(encoding="utf-8"))
        self.assertEqual(summary["FinalState"], "selection_complete")
        self.assertEqual(summary["Claims"]["bg_workshop_day"], "selected")

    def test_rejects_below_threshold_or_older_round_candidate(self) -> None:
        self.write_manifest()
        self.write_review(score=87)
        with self.assertRaisesRegex(ValueError, "score"):
            select_art_candidate(
                project_root=self.root,
                manifest_path=self.manifest_path,
                incoming_root=self.incoming_root,
                visual_id="bg_workshop_day",
                review_path=self.review_path,
                allow_selected_overwrite=False,
                dry_run=False,
            )

        older = self.workspace / "processed" / "1" / "001.png"
        older.parent.mkdir(parents=True)
        Image.new("RGBA", (8, 4), "blue").save(older)
        self.write_review(score=92, candidate=older)
        with self.assertRaisesRegex(ValueError, "latest processed round"):
            select_art_candidate(
                project_root=self.root,
                manifest_path=self.manifest_path,
                incoming_root=self.incoming_root,
                visual_id="bg_workshop_day",
                review_path=self.review_path,
                allow_selected_overwrite=False,
                dry_run=False,
            )

    def test_dry_run_writes_nothing(self) -> None:
        self.write_manifest()
        before = self.manifest_path.read_bytes()
        result = select_art_candidate(
            project_root=self.root,
            manifest_path=self.manifest_path,
            incoming_root=self.incoming_root,
            visual_id="bg_workshop_day",
            review_path=self.review_path,
            allow_selected_overwrite=False,
            dry_run=True,
        )
        self.assertEqual(result["State"], "planned")
        self.assertEqual(self.manifest_path.read_bytes(), before)
        self.assertFalse((self.workspace / "selected").exists())

    def test_reuses_unique_legacy_selected_image_when_manifest_path_is_empty(self) -> None:
        target = self.workspace / "selected" / "001.png"
        target.parent.mkdir(parents=True)
        Image.new("RGBA", (8, 4), "red").save(target)
        self.write_manifest(selected_path="")
        self.write_review(score=92, replacement_target=target, baseline_score=90)

        result = select_art_candidate(
            project_root=self.root,
            manifest_path=self.manifest_path,
            incoming_root=self.incoming_root,
            visual_id="bg_workshop_day",
            review_path=self.review_path,
            allow_selected_overwrite=True,
            dry_run=False,
        )

        self.assertEqual(result["SelectedPath"], repo_path(target, self.root))
        self.assertEqual(target.read_bytes(), self.candidate.read_bytes())
        self.assertFalse((target.parent / "bg_workshop_day.png").exists())
        manifest = json.loads(self.manifest_path.read_text(encoding="utf-8"))
        self.assertEqual(manifest["Entries"][0]["SelectedPath"], repo_path(target, self.root))

    def test_rejects_ambiguous_legacy_selected_images_when_manifest_path_is_empty(self) -> None:
        selected_dir = self.workspace / "selected"
        selected_dir.mkdir(parents=True)
        Image.new("RGBA", (8, 4), "red").save(selected_dir / "001.png")
        Image.new("RGBA", (8, 4), "blue").save(selected_dir / "002.webp")
        self.write_manifest(selected_path="")

        with self.assertRaisesRegex(ValueError, "selected_target_ambiguous"):
            select_art_candidate(
                project_root=self.root,
                manifest_path=self.manifest_path,
                incoming_root=self.incoming_root,
                visual_id="bg_workshop_day",
                review_path=self.review_path,
                allow_selected_overwrite=True,
                dry_run=True,
            )

    def test_replacement_requires_strictly_better_same_run_baseline(self) -> None:
        target = self.workspace / "selected" / "bg_workshop_day.png"
        target.parent.mkdir(parents=True)
        Image.new("RGBA", (8, 4), "red").save(target)
        self.write_manifest(selected_path=repo_path(target, self.root))
        self.write_review(score=90, replacement_target=target, baseline_score=90)

        with self.assertRaisesRegex(ValueError, "replacement_not_better"):
            select_art_candidate(
                project_root=self.root,
                manifest_path=self.manifest_path,
                incoming_root=self.incoming_root,
                visual_id="bg_workshop_day",
                review_path=self.review_path,
                allow_selected_overwrite=True,
                dry_run=False,
            )

    def test_replacement_rejects_protected_dimension_regression(self) -> None:
        target = self.workspace / "selected" / "bg_workshop_day.png"
        target.parent.mkdir(parents=True)
        Image.new("RGBA", (8, 4), "red").save(target)
        self.write_manifest(selected_path=repo_path(target, self.root))
        self.write_review(
            score=92,
            replacement_target=target,
            baseline_score=90,
            candidate_dimensions={"SemanticFit": 89},
            baseline_dimensions={"SemanticFit": 90},
            protected_dimensions=["SemanticFit"],
        )

        with self.assertRaisesRegex(ValueError, "replacement_protected_dimension_regression"):
            select_art_candidate(
                project_root=self.root,
                manifest_path=self.manifest_path,
                incoming_root=self.incoming_root,
                visual_id="bg_workshop_day",
                review_path=self.review_path,
                allow_selected_overwrite=True,
                dry_run=False,
            )

    def test_replacement_rejects_stale_selected_sha(self) -> None:
        target = self.workspace / "selected" / "bg_workshop_day.png"
        target.parent.mkdir(parents=True)
        Image.new("RGBA", (8, 4), "red").save(target)
        self.write_manifest(selected_path=repo_path(target, self.root))
        self.write_review(score=92, replacement_target=target, baseline_score=90)
        Image.new("RGBA", (8, 4), "blue").save(target)

        with self.assertRaisesRegex(ValueError, "replacement_baseline_stale"):
            select_art_candidate(
                project_root=self.root,
                manifest_path=self.manifest_path,
                incoming_root=self.incoming_root,
                visual_id="bg_workshop_day",
                review_path=self.review_path,
                allow_selected_overwrite=True,
                dry_run=False,
            )

    def test_identical_selected_sha_is_idempotent(self) -> None:
        target = self.workspace / "selected" / "bg_workshop_day.png"
        target.parent.mkdir(parents=True)
        target.write_bytes(self.candidate.read_bytes())
        self.write_manifest(status="approved", selected_path=repo_path(target, self.root))

        result = select_art_candidate(
            project_root=self.root,
            manifest_path=self.manifest_path,
            incoming_root=self.incoming_root,
            visual_id="bg_workshop_day",
            review_path=self.review_path,
            allow_selected_overwrite=False,
            dry_run=False,
        )

        self.assertEqual(result["State"], "already_selected")
        self.assertEqual(result["SelectionMode"], "idempotent")
        manifest = json.loads(self.manifest_path.read_text(encoding="utf-8"))
        self.assertEqual(manifest["Entries"][0]["Status"], "approved")

    def test_rejects_stale_reviewed_candidate_sha(self) -> None:
        self.write_manifest()
        review = json.loads(self.review_path.read_text(encoding="utf-8"))
        review["Items"][0]["CandidateSHA256"] = "0" * 64
        self.review_path.write_text(json.dumps(review), encoding="utf-8")

        with self.assertRaisesRegex(ValueError, "reviewed_candidate_sha_mismatch"):
            select_art_candidate(
                project_root=self.root,
                manifest_path=self.manifest_path,
                incoming_root=self.incoming_root,
                visual_id="bg_workshop_day",
                review_path=self.review_path,
                allow_selected_overwrite=False,
                dry_run=False,
            )

    def test_replacement_requires_baseline_evidence(self) -> None:
        target = self.workspace / "selected" / "bg_workshop_day.png"
        target.parent.mkdir(parents=True)
        Image.new("RGBA", (8, 4), "red").save(target)
        self.write_manifest(selected_path=repo_path(target, self.root))

        with self.assertRaisesRegex(ValueError, "replacement_baseline_missing"):
            select_art_candidate(
                project_root=self.root,
                manifest_path=self.manifest_path,
                incoming_root=self.incoming_root,
                visual_id="bg_workshop_day",
                review_path=self.review_path,
                allow_selected_overwrite=True,
                dry_run=False,
            )


if __name__ == "__main__":
    unittest.main()
