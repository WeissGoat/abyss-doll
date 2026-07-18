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

from art_processing import (  # noqa: E402
    abandon_round,
    load_round_decision,
    next_round_number,
    numeric_round_directories,
    publish_round,
    reserve_round,
    resolve_latest_processed_candidate,
    resolve_selected_or_processed_candidate,
)


def image_metadata(path: Path) -> dict[str, object]:
    with Image.open(path) as image:
        width, height = image.size
        image_format = str(image.format or "").lower()
    return {
        "SHA256": hashlib.sha256(path.read_bytes()).hexdigest(),
        "Width": width,
        "Height": height,
        "Format": image_format,
    }


def write_decision(round_dir: Path, state: str, candidates: list[dict[str, object]]) -> None:
    (round_dir / "decision.json").write_text(
        json.dumps({"State": state, "Candidates": candidates}),
        encoding="utf-8",
    )


class ArtProcessingRoundTests(unittest.TestCase):
    def test_numeric_rounds_sort_as_integers_and_ignore_other_names(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            processed = Path(temp_dir)
            for name in ["10", "2", "1", "round_3", ".tmp-4"]:
                (processed / name).mkdir()

            rounds = numeric_round_directories(processed)

            self.assertEqual([number for number, _ in rounds], [1, 2, 10])
            self.assertEqual(next_round_number(processed), 11)

    def test_empty_processed_directory_allocates_round_one(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            self.assertEqual(next_round_number(Path(temp_dir)), 1)

    def test_reservation_publish_and_abandon(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            processed = Path(temp_dir)
            reservation = reserve_round(processed)
            self.assertEqual(reservation.number, 1)
            self.assertTrue(reservation.temp_dir.exists())
            self.assertTrue(reservation.lock_dir.exists())

            with self.assertRaises(FileExistsError):
                reserve_round(processed)

            (reservation.temp_dir / "decision.json").write_text("{}", encoding="utf-8")
            published = publish_round(reservation)
            self.assertEqual(published, processed / "1")
            self.assertTrue(published.exists())
            self.assertFalse(reservation.temp_dir.exists())
            self.assertFalse(reservation.lock_dir.exists())

            second = reserve_round(processed)
            abandon_round(second)
            self.assertFalse(second.temp_dir.exists())
            self.assertFalse(second.lock_dir.exists())


class ArtProcessingResolverTests(unittest.TestCase):
    def test_latest_single_passed_candidate_is_resolved(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            workspace = Path(temp_dir)
            round_dir = workspace / "processed" / "2"
            round_dir.mkdir(parents=True)
            candidate = round_dir / "001.png"
            Image.new("RGBA", (2, 2), (255, 0, 0, 255)).save(candidate)
            metadata = image_metadata(candidate)
            write_decision(
                round_dir,
                "passed",
                [{"File": "001.png", "Status": "passed", **metadata}],
            )

            result = resolve_latest_processed_candidate(workspace)

            self.assertEqual(result.path, candidate)
            self.assertEqual(result.source_kind, "processed_round")
            self.assertEqual(result.round_number, 2)
            self.assertEqual(result.processing_state, "passed")

    def test_latest_failed_round_does_not_fall_back_to_earlier_passed_round(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            workspace = Path(temp_dir)
            first = workspace / "processed" / "1"
            latest = workspace / "processed" / "2"
            first.mkdir(parents=True)
            latest.mkdir()
            first_file = first / "001.png"
            Image.new("RGBA", (2, 2), (0, 255, 0, 255)).save(first_file)
            write_decision(first, "passed", [{"File": "001.png", "Status": "passed", **image_metadata(first_file)}])
            write_decision(latest, "failed", [])

            result = resolve_latest_processed_candidate(workspace)

            self.assertIsNone(result.path)
            self.assertEqual(result.round_number, 2)
            self.assertEqual(result.processing_state, "failed")

    def test_multiple_passed_candidates_require_selected(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            workspace = Path(temp_dir)
            round_dir = workspace / "processed" / "1"
            round_dir.mkdir(parents=True)
            candidates = []
            for name in ["001.png", "002.png"]:
                path = round_dir / name
                Image.new("RGBA", (2, 2), (0, 0, 255, 255)).save(path)
                candidates.append({"File": name, "Status": "passed", **image_metadata(path)})
            write_decision(round_dir, "passed", candidates)

            result = resolve_latest_processed_candidate(workspace)

            self.assertIsNone(result.path)
            self.assertEqual(result.reason, "multiple_passed_candidates_require_selection")

    def test_invalid_direct_child_filename_and_hash_are_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            workspace = Path(temp_dir)
            round_dir = workspace / "processed" / "1"
            round_dir.mkdir(parents=True)
            candidate = round_dir / "001.png"
            Image.new("RGBA", (2, 2), "red").save(candidate)
            write_decision(
                round_dir,
                "passed",
                [{"File": "sub/001.png", "Status": "passed", "SHA256": "bad", "Width": 2, "Height": 2, "Format": "png"}],
            )

            result = resolve_latest_processed_candidate(workspace)

            self.assertIsNone(result.path)
            self.assertEqual(result.reason, "no_valid_passed_candidate")

    def test_temporary_directory_is_not_scanned(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            workspace = Path(temp_dir)
            temp_round = workspace / "processed" / ".tmp-1-abcd"
            temp_round.mkdir(parents=True)
            candidate = temp_round / "001.png"
            Image.new("RGBA", (2, 2), "red").save(candidate)
            write_decision(temp_round, "passed", [{"File": "001.png", "Status": "passed", **image_metadata(candidate)}])

            result = resolve_latest_processed_candidate(workspace)

            self.assertIsNone(result.path)
            self.assertEqual(result.reason, "no_processed_rounds")

    def test_manifest_selected_path_precedes_selected_directory(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            workspace = Path(temp_dir)
            selected = workspace / "selected"
            selected.mkdir()
            manifest_choice = selected / "b.png"
            legacy_first = selected / "a.png"
            manifest_choice.write_bytes(b"b")
            legacy_first.write_bytes(b"a")

            result = resolve_selected_or_processed_candidate(workspace, manifest_selected_path=manifest_choice)

            self.assertEqual(result.path, manifest_choice)
            self.assertEqual(result.source_kind, "manifest_selected")

    def test_selected_directory_precedes_processed_round(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            workspace = Path(temp_dir)
            selected = workspace / "selected"
            selected.mkdir()
            selected_file = selected / "a.png"
            selected_file.write_bytes(b"selected")

            result = resolve_selected_or_processed_candidate(workspace)

            self.assertEqual(result.path, selected_file)
            self.assertEqual(result.source_kind, "selected")


class ArtProcessingDecisionValidationTests(unittest.TestCase):
    def test_load_round_decision_requires_supported_state_and_candidates(self) -> None:
        with tempfile.TemporaryDirectory() as temp_dir:
            round_dir = Path(temp_dir)
            (round_dir / "decision.json").write_text(json.dumps({"State": "unknown"}), encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "State"):
                load_round_decision(round_dir)

            (round_dir / "decision.json").write_text(json.dumps({"State": "passed", "Candidates": {}}), encoding="utf-8")
            with self.assertRaisesRegex(ValueError, "Candidates"):
                load_round_decision(round_dir)


if __name__ == "__main__":
    unittest.main()
