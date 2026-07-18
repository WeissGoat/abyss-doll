from __future__ import annotations

import dataclasses
import tempfile
import unittest
from pathlib import Path

from PIL import Image

TOOLS_DIR = Path(__file__).resolve().parents[1]
import sys

if str(TOOLS_DIR) not in sys.path:
    sys.path.insert(0, str(TOOLS_DIR))

from gif_character_replace.models import RunConfig, RunStatus  # noqa: E402
from gif_character_replace.store import RunStore  # noqa: E402


class RunStoreTests(unittest.TestCase):
    def _source_gif(self, root: Path) -> Path:
        source = root / "source.gif"
        Image.new("RGBA", (8, 8), (20, 30, 40, 255)).save(source, format="GIF")
        return source

    def test_create_writes_immutable_config_and_initial_state(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = self._source_gif(root)
            reference = root / "reference.png"
            Image.new("RGBA", (8, 8), (200, 100, 50, 255)).save(reference)
            config = RunConfig.new(
                input_gif=source,
                output_root=root / "output",
                prompt="silver-haired workshop mechanic",
                references=(reference,),
            )
            store = RunStore.create(config)
            self.assertEqual(store.load_state().status, RunStatus.PREFLIGHT)
            self.assertEqual(store.load_config(), config)
            self.assertTrue((store.paths.input_dir / "source.gif").exists())
            self.assertTrue((store.paths.input_dir / "character_prompt.txt").exists())
            self.assertTrue((store.paths.input_dir / "references/reference_00.png").exists())

    def test_create_rejects_changed_existing_config(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = self._source_gif(root)
            config = RunConfig.new(
                input_gif=source,
                output_root=root / "output",
                prompt="first",
                references=(),
            )
            store = RunStore.create(config)
            changed = dataclasses.replace(config, prompt="changed")
            with self.assertRaisesRegex(ValueError, "immutable"):
                RunStore.create(changed, run_root=store.paths.root)

    def test_create_recovers_missing_initial_evidence_after_interruption(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = self._source_gif(root)
            reference = root / "reference.png"
            Image.new("RGBA", (8, 8), (200, 100, 50, 255)).save(reference)
            config = RunConfig.new(
                input_gif=source,
                output_root=root / "output",
                prompt="restored prompt",
                references=(reference,),
            )
            store = RunStore.create(config)
            (store.paths.input_dir / "source.gif").unlink()
            (store.paths.input_dir / "character_prompt.txt").unlink()
            (store.paths.input_dir / "references/reference_00.png").unlink()
            store.state_path.unlink()

            recovered = RunStore.create(config, run_root=store.paths.root)

            self.assertEqual(recovered.load_state(), recovered.load_state().initial())
            self.assertEqual(
                (recovered.paths.input_dir / "character_prompt.txt").read_text(encoding="utf-8"),
                "restored prompt\n",
            )
            self.assertEqual(
                (recovered.paths.input_dir / "source.gif").read_bytes(),
                source.read_bytes(),
            )
            self.assertEqual(
                (recovered.paths.input_dir / "references/reference_00.png").read_bytes(),
                reference.read_bytes(),
            )

    def test_create_preserves_existing_state_while_restoring_input_evidence(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = self._source_gif(root)
            config = RunConfig.new(
                input_gif=source,
                output_root=root / "output",
                prompt="figure",
                references=(),
            )
            store = RunStore.create(config)
            store.update_state(lambda state: dataclasses.replace(state, status=RunStatus.RUNNING))
            (store.paths.input_dir / "source.gif").unlink()

            recovered = RunStore.create(config, run_root=store.paths.root)

            self.assertEqual(recovered.load_state().status, RunStatus.RUNNING)
            self.assertTrue((recovered.paths.input_dir / "source.gif").exists())

    def test_update_state_is_atomic_and_reloadable(self) -> None:
        with tempfile.TemporaryDirectory() as temp:
            root = Path(temp)
            source = self._source_gif(root)
            config = RunConfig.new(
                input_gif=source,
                output_root=root / "output",
                prompt="figure",
                references=(),
            )
            store = RunStore.create(config)
            updated = store.update_state(
                lambda state: dataclasses.replace(state, status=RunStatus.RUNNING)
            )
            restored = RunStore.load(store.paths.root)
            self.assertEqual(updated.status, RunStatus.RUNNING)
            self.assertEqual(restored.load_state().status, RunStatus.RUNNING)


if __name__ == "__main__":
    unittest.main()
