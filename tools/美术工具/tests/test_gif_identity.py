from __future__ import annotations

import json
import tempfile
import unittest
from pathlib import Path

from PIL import Image

TOOLS_DIR = Path(__file__).resolve().parents[1]
import sys

if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from gif_character_replace.identity import (  # noqa: E402
    build_identity_contract,
    prepare_identity_inputs,
)


class GifIdentityTests(unittest.TestCase):
    def _reference(self, root: Path, name: str, color: tuple[int, int, int]) -> Path:
        path = root / name
        Image.new("RGBA", (96, 64), color + (255,)).save(path)
        return path

    def test_prompt_priority_and_fixed_sections(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            reference = self._reference(root, "blue_coat_reference.png", (30, 50, 180))
            contract = build_identity_contract(
                "red coat, watercolor character rendering", [reference]
            )
            self.assertIn("User text has highest priority", contract.provider_prompt)
            self.assertIn("red coat", contract.provider_prompt)
            self.assertIn("preserve the original background exactly", contract.provider_prompt)
            self.assertIn(
                "apply watercolor rendering only to the replacement character",
                contract.provider_prompt,
            )
            headings = [
                "TASK",
                "USER TEXT - HIGHEST PRIORITY",
                "REFERENCE IMAGE ROLE",
                "MUST PRESERVE FROM CURRENT GIF FRAME",
                "MAY CHANGE",
                "MUST NOT CHANGE",
                "OUTPUT CONTRACT",
            ]
            positions = [contract.provider_prompt.index(heading) for heading in headings]
            self.assertEqual(positions, sorted(positions))

    def test_blank_prompt_and_missing_reference_are_rejected(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            with self.assertRaisesRegex(ValueError, "blank"):
                build_identity_contract("  ", [])
            with self.assertRaises(FileNotFoundError):
                build_identity_contract("character", [root / "missing.png"])

    def test_one_to_three_references_are_used_directly_and_contract_is_written(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            references = [
                self._reference(root, "one.png", (10, 20, 30)),
                self._reference(root, "two.png", (40, 50, 60)),
                self._reference(root, "three.png", (70, 80, 90)),
            ]
            contract = build_identity_contract("mechanic", references)
            inputs = prepare_identity_inputs(contract, root / "identity")
            self.assertEqual(inputs.provider_image_paths, tuple(str(p.resolve()) for p in references))
            self.assertIsNone(inputs.reference_board_path)
            payload = json.loads(Path(inputs.contract_path).read_text(encoding="utf-8"))
            self.assertEqual(payload["user_prompt"], "mechanic")
            self.assertEqual(payload["reference_paths"], [str(p.resolve()) for p in references])
            self.assertEqual(payload["provider_image_paths"], list(inputs.provider_image_paths))
            self.assertEqual(len(payload["reference_sha256"]), 3)
            self.assertIn("sections", payload)

    def test_four_references_create_two_column_labeled_board(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            references = [
                self._reference(root, f"reference_{index}.png", (index * 30, 80, 120))
                for index in range(4)
            ]
            contract = build_identity_contract("new character", references)
            inputs = prepare_identity_inputs(contract, root / "identity")
            self.assertEqual(len(inputs.provider_image_paths), 1)
            self.assertEqual(inputs.provider_image_paths[0], inputs.reference_board_path)
            board_path = Path(inputs.reference_board_path or "")
            self.assertTrue(board_path.is_file())
            with Image.open(board_path) as board:
                self.assertEqual(board.format, "PNG")
                self.assertEqual(board.mode, "RGB")
                self.assertEqual(board.getpixel((0, 0)), (128, 128, 128))
                self.assertGreater(board.width, 500)
                self.assertGreater(board.height, 500)
            board_bytes = board_path.read_bytes()
            for reference in references:
                self.assertNotIn(reference.name.encode("utf-8"), board_bytes)
            payload = json.loads(
                (root / "identity" / "identity_contract.json").read_text(encoding="utf-8")
            )
            self.assertEqual(payload["reference_board_path"], str(board_path.resolve()))


if __name__ == "__main__":
    unittest.main()
