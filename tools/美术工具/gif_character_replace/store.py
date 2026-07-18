"""Run directory creation and atomic persistence."""

from __future__ import annotations

import json
import shutil
from dataclasses import replace
from pathlib import Path
from typing import Callable

from .models import RunConfig, RunPaths, RunState, json_dumps


def atomic_write_json(path: Path, payload: dict) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    temporary = path.with_suffix(path.suffix + ".tmp")
    temporary.write_text(json_dumps(payload), encoding="utf-8")
    temporary.replace(path)


class RunStore:
    def __init__(self, config: RunConfig, paths: RunPaths) -> None:
        self.config = config
        self.paths = paths
        self.config_path = paths.root / "run_config.json"
        self.state_path = paths.root / "run_state.json"

    @classmethod
    def create(cls, config: RunConfig, *, run_root: Path | None = None) -> "RunStore":
        paths = RunPaths.for_config(config)
        if run_root is not None:
            root = run_root.resolve()
            paths = replace(
                paths,
                root=root,
                input_dir=root / "input",
                timeline_dir=root / "timeline",
                original_frames_dir=root / "timeline" / "frames_original",
                identity_dir=root / "identity",
                preview_dir=root / "preview",
                generated_raw_dir=root / "generated" / "raw",
                accepted_dir=root / "generated" / "accepted",
                rejected_dir=root / "generated" / "rejected",
                output_dir=root / "output",
                reports_dir=root / "reports",
            )
        config_path = paths.root / "run_config.json"
        if config_path.exists():
            existing = json.loads(config_path.read_text(encoding="utf-8"))
            if existing != config.to_dict():
                raise ValueError("run configuration is immutable")
            return cls(config, paths)
        if paths.root.exists() and any(paths.root.iterdir()):
            raise ValueError(f"run directory is not empty: {paths.root}")
        if not Path(config.input_gif).is_file():
            raise FileNotFoundError(config.input_gif)
        for reference in config.references:
            if not Path(reference).is_file():
                raise FileNotFoundError(reference)
        paths.create_directories()
        shutil.copy2(config.input_gif, paths.input_dir / "source.gif")
        (paths.input_dir / "character_prompt.txt").write_text(config.prompt + "\n", encoding="utf-8")
        references_dir = paths.input_dir / "references"
        references_dir.mkdir(parents=True, exist_ok=True)
        for index, reference in enumerate(config.references):
            source = Path(reference)
            shutil.copy2(source, references_dir / f"reference_{index:02d}{source.suffix.lower()}")
        atomic_write_json(config_path, config.to_dict())
        atomic_write_json(paths.root / "run_state.json", RunState.initial().to_dict())
        return cls(config, paths)

    @classmethod
    def load(cls, run_root: Path) -> "RunStore":
        config_path = run_root / "run_config.json"
        if not config_path.exists():
            raise FileNotFoundError(config_path)
        config = RunConfig.from_dict(json.loads(config_path.read_text(encoding="utf-8")))
        paths = RunPaths.for_config(config)
        root = run_root.resolve()
        paths = replace(
            paths,
            root=root,
            input_dir=root / "input",
            timeline_dir=root / "timeline",
            original_frames_dir=root / "timeline" / "frames_original",
            identity_dir=root / "identity",
            preview_dir=root / "preview",
            generated_raw_dir=root / "generated" / "raw",
            accepted_dir=root / "generated" / "accepted",
            rejected_dir=root / "generated" / "rejected",
            output_dir=root / "output",
            reports_dir=root / "reports",
        )
        return cls(config, paths)

    def load_config(self) -> RunConfig:
        return RunConfig.from_dict(json.loads(self.config_path.read_text(encoding="utf-8")))

    def load_state(self) -> RunState:
        return RunState.from_dict(json.loads(self.state_path.read_text(encoding="utf-8")))

    def write_state(self, state: RunState) -> RunState:
        atomic_write_json(self.state_path, state.to_dict())
        return state

    def update_state(self, mutator: Callable[[RunState], RunState]) -> RunState:
        return self.write_state(mutator(self.load_state()))
