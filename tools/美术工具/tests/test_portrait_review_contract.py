# -*- coding: utf-8 -*-

from __future__ import annotations

import sys
import unittest
from pathlib import Path

TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from portrait_review_contract import validate_portrait_review_item  # noqa: E402


def valid_item() -> dict:
    scores = {"Identity": 94, "Costume": 92, "Proportion": 91, "Framing": 90, "Technical": 94, "TargetFit": 95}
    scores["Total"] = round(sum(scores.values()) / 6)
    return {
        "VisualID": "doll_demo_hurt",
        "ReviewRubricVersion": "character_portrait_v3",
        "Scores": scores,
        "DimensionEvidence": {name: {"Finding": f"independent finding for {name}", "Evidence": [f"observation for {name}"]} for name in scores if name != "Total"},
    }


class PortraitReviewContractTests(unittest.TestCase):
    def test_validates_six_dimensions_and_total(self) -> None:
        self.assertEqual(validate_portrait_review_item(valid_item())["Scores"]["Total"], 93)

    def test_rejects_missing_score_and_copied_findings(self) -> None:
        missing = valid_item()
        missing["Scores"].pop("Costume")
        with self.assertRaisesRegex(ValueError, "portrait_score_missing:Costume"):
            validate_portrait_review_item(missing)
        duplicated = valid_item()
        for evidence in duplicated["DimensionEvidence"].values():
            evidence["Finding"] = "same copied sentence"
        with self.assertRaisesRegex(ValueError, "portrait_dimension_evidence_not_independent"):
            validate_portrait_review_item(duplicated)

    def test_rejects_invalid_total_evidence_and_selection_threshold(self) -> None:
        invalid = valid_item()
        invalid["Scores"]["Total"] = 99
        with self.assertRaisesRegex(ValueError, "portrait_score_total_mismatch"):
            validate_portrait_review_item(invalid)
        invalid = valid_item()
        invalid["DimensionEvidence"].pop("Technical")
        with self.assertRaisesRegex(ValueError, "portrait_dimension_evidence_missing:Technical"):
            validate_portrait_review_item(invalid)
        invalid = valid_item()
        invalid["Scores"]["TargetFit"] = 87
        invalid["Scores"]["Total"] = round(sum(invalid["Scores"][name] for name in invalid["Scores"] if name != "Total") / 6)
        with self.assertRaisesRegex(ValueError, "portrait_target_fit_below_selection_threshold"):
            validate_portrait_review_item(invalid, require_selection_threshold=True)


if __name__ == "__main__":
    unittest.main()
