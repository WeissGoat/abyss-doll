from __future__ import annotations

import dataclasses
import json
import sys
import tempfile
import unittest
from pathlib import Path

from PIL import Image


TOOLS_DIR = Path(__file__).resolve().parents[1]
if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from gif_character_replace.action_text import (  # noqa: E402
    action_text_for_frame,
    install_action_texts,
    load_action_texts,
)
from gif_character_replace.models import RunConfig, TimelineMetadata  # noqa: E402
from gif_character_replace.store import RunStore  # noqa: E402


class GifActionTextTests(unittest.TestCase):
    def _store(self, root: Path, frame_count: int = 3) -> RunStore:
        frames = [Image.new("RGBA", (32, 24), (index * 40, 20, 80, 255)) for index in range(frame_count)]
        source = root / "source.gif"
        frames[0].save(
            source,
            save_all=True,
            append_images=frames[1:],
            duration=[80] * frame_count,
            loop=0,
        )
        config = RunConfig.new(
            input_gif=source,
            output_root=root / "runs",
            prompt="replacement character",
            references=(),
        )
        store = RunStore.create(config)
        timeline = TimelineMetadata(
            width=32,
            height=24,
            frame_count=frame_count,
            durations_ms=tuple(80 for _ in range(frame_count)),
            loop=0,
            has_transparency=False,
            disposal_methods=tuple(1 for _ in range(frame_count)),
            source_sha256="source-hash",
            frame_paths=tuple(f"frames_original/frame_{index:04d}.png" for index in range(frame_count)),
        )
        store.update_state(lambda state: dataclasses.replace(state, timeline=timeline))
        return store

    def _write_map(self, root: Path, payload: dict) -> Path:
        path = root / "action_texts.json"
        path.write_text(json.dumps(payload, ensure_ascii=False), encoding="utf-8")
        return path

    def test_valid_complete_map_is_normalized_persisted_and_loaded(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            store = self._store(root)
            source = self._write_map(
                root,
                {
                    "source_sha256": "source-hash",
                    "frame_count": 3,
                    "actions": {
                        "2": " head tilted right ",
                        "0": "finger near lips",
                        "1": "hands below chin",
                    },
                },
            )

            destination = install_action_texts(store, source)
            loaded = load_action_texts(store)

            self.assertEqual(destination, store.paths.identity_dir / "frame_action_texts.json")
            self.assertIsNotNone(loaded)
            self.assertEqual(loaded.actions, {
                0: "finger near lips",
                1: "hands below chin",
                2: "head tilted right",
            })
            self.assertEqual(action_text_for_frame(store, 1), "hands below chin")
            payload = json.loads(destination.read_text(encoding="utf-8"))
            self.assertEqual(list(payload["actions"]), ["0", "1", "2"])

    def test_missing_sidecar_keeps_legacy_generic_prompt_path(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            store = self._store(Path(temp))

            self.assertIsNone(load_action_texts(store))
            self.assertIsNone(action_text_for_frame(store, 0))

    def test_rejects_wrong_hash_count_indices_missing_or_blank_actions(self) -> None:
        cases = (
            ({"source_sha256": "wrong", "frame_count": 3, "actions": {"0": "a", "1": "b", "2": "c"}}, "source_sha256"),
            ({"source_sha256": "source-hash", "frame_count": 2, "actions": {"0": "a", "1": "b"}}, "frame_count"),
            ({"source_sha256": "source-hash", "frame_count": 3, "actions": {"0": "a", "1": "b", "3": "c"}}, "indices"),
            ({"source_sha256": "source-hash", "frame_count": 3, "actions": {"0": "a", "2": "c"}}, "indices"),
            ({"source_sha256": "source-hash", "frame_count": 3, "actions": {"0": "a", "1": " ", "2": "c"}}, "blank"),
        )
        for payload, message in cases:
            with self.subTest(message=message), tempfile.TemporaryDirectory() as temp:
                root = Path(temp)
                store = self._store(root)
                source = self._write_map(root, payload)
                with self.assertRaisesRegex(ValueError, message):
                    install_action_texts(store, source)


if __name__ == "__main__":
    unittest.main()
