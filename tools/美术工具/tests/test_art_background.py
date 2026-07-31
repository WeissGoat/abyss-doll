# -*- coding: utf-8 -*-
from __future__ import annotations

import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image, ImageDraw


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from art_background import (  # noqa: E402
    background_policy,
    process_background,
    remove_connected_background,
    review_candidate,
)


class ConnectedBackgroundRemovalTests(unittest.TestCase):
    def test_enclosed_white_foreground_is_not_removed_with_white_background(self) -> None:
        image = Image.new("RGB", (64, 64), "white")
        draw = ImageDraw.Draw(image)
        draw.rectangle((15, 15, 48, 48), outline="black", width=3, fill="white")

        result = remove_connected_background(image, threshold=34)

        self.assertEqual(result.getpixel((0, 0))[3], 0)
        self.assertEqual(result.getpixel((32, 32))[3], 255)
        self.assertGreater(result.getpixel((15, 32))[3], 0)


class BackgroundPolicyTests(unittest.TestCase):
    def test_alpha_required_does_not_imply_auto_simple(self) -> None:
        spec = {
            "SourceSpec": {"AlphaRequired": True},
            "ProcessSpec": {},
        }
        with self.assertRaisesRegex(ValueError, "BackgroundPolicy"):
            background_policy(spec)

    def test_agent_required_returns_decision_required_without_processing(self) -> None:
        image = Image.new("RGB", (32, 32), "white")
        result = process_background(image, "agent_required", threshold=34)
        self.assertEqual(result.state, "decision_required")
        self.assertIsNone(result.image)


class CandidateTechnicalReviewTests(unittest.TestCase):
    def test_large_internal_transparency_fails_character_review(self) -> None:
        image = Image.new("RGBA", (100, 100), (255, 255, 255, 0))
        draw = ImageDraw.Draw(image)
        draw.rectangle((10, 5, 90, 95), fill=(100, 100, 100, 255))
        draw.rectangle((25, 20, 75, 80), fill=(255, 255, 255, 0))

        review = review_candidate(
            image,
            source_spec={"Width": 100, "Height": 100, "AlphaRequired": True},
            composition_spec={"SafePaddingPercent": 5},
            production_profile="character_portrait_set",
        )

        self.assertEqual(review["Status"], "failed")
        self.assertIn("unexpected_transparent_holes", review["Reasons"])

    def test_character_negative_space_is_warning_not_hard_failure(self) -> None:
        image = Image.new("RGBA", (100, 100), (0, 0, 0, 0))
        draw = ImageDraw.Draw(image)
        draw.rectangle((10, 5, 22, 95), fill=(255, 255, 255, 255))
        draw.rectangle((78, 5, 90, 95), fill=(255, 255, 255, 255))
        draw.rectangle((10, 5, 90, 16), fill=(255, 255, 255, 255))

        review = review_candidate(
            image,
            source_spec={"Width": 100, "Height": 100, "AlphaRequired": True},
            composition_spec={"SafePaddingPercent": 0},
            production_profile="character_portrait_set",
        )

        self.assertEqual(review["Status"], "warning")
        self.assertIn("high_occupied_bbox_transparency", review["Warnings"])
        self.assertNotIn("unexpected_transparent_holes", review["Reasons"])

    def test_agent_review_can_measure_a_valid_transparent_candidate(self) -> None:
        image = Image.new("RGBA", (32, 32), (0, 0, 0, 0))
        ImageDraw.Draw(image).rectangle((8, 4, 24, 28), fill=(255, 0, 0, 255))
        review = review_candidate(
            image,
            source_spec={"Width": 32, "Height": 32, "AlphaRequired": True},
            composition_spec={"SafePaddingPercent": 5},
            production_profile="standard_asset",
        )
        self.assertEqual(review["Status"], "passed")
        self.assertIn("Width", review["Metrics"])

    def test_nine_slice_rejects_fragmented_decoration(self) -> None:
        image = Image.new("RGBA", (100, 40), (0, 0, 0, 0))
        draw = ImageDraw.Draw(image)
        draw.rectangle((5, 5, 94, 34), outline=(255, 255, 255, 255), width=3)
        for index in range(30):
            x = 2 + (index * 3) % 96
            y = 2 if index % 2 == 0 else 37
            draw.point((x, y), fill=(255, 255, 255, 255))

        review = review_candidate(
            image,
            source_spec={"Width": 100, "Height": 40, "AlphaRequired": True},
            composition_spec={"SafePaddingPercent": 0},
            process_spec={
                "NineSlice": {
                    "Enabled": True,
                    "Border": {"Left": 10, "Right": 10, "Top": 8, "Bottom": 8},
                }
            },
            asset_type="button",
            production_profile="standard_asset",
        )

        self.assertEqual(review["Status"], "failed")
        self.assertIn("nine_slice_many_components", review["Reasons"])
        self.assertIn("NineSliceMetrics", review)

    def test_nine_slice_rejects_empty_border_band(self) -> None:
        image = Image.new("RGBA", (100, 40), (0, 0, 0, 0))
        ImageDraw.Draw(image).rectangle((30, 12, 70, 28), fill=(255, 255, 255, 255))

        review = review_candidate(
            image,
            source_spec={"Width": 100, "Height": 40, "AlphaRequired": True},
            composition_spec={"SafePaddingPercent": 0},
            process_spec={
                "NineSlice": {
                    "Enabled": True,
                    "Border": {"Left": 10, "Right": 10, "Top": 8, "Bottom": 8},
                }
            },
            asset_type="button",
            production_profile="standard_asset",
        )

        self.assertEqual(review["Status"], "failed")
        self.assertIn("nine_slice_edge_coverage_low", review["Reasons"])


if __name__ == "__main__":
    unittest.main()
