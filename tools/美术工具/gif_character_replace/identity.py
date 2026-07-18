"""Identity contract and reference-image preparation for GIF replacement."""

from __future__ import annotations

import hashlib
import json
import math
from dataclasses import asdict, dataclass
from pathlib import Path
from typing import Any, Sequence

from PIL import Image, ImageDraw, ImageOps


@dataclass(frozen=True)
class IdentityContract:
    user_prompt: str
    provider_prompt: str
    must_have: tuple[str, ...]
    may_change: tuple[str, ...]
    must_preserve: tuple[str, ...]
    must_not_have: tuple[str, ...]
    reference_paths: tuple[str, ...]
    reference_sha256: tuple[str, ...]


@dataclass(frozen=True)
class IdentityInputs:
    contract_path: str
    provider_image_paths: tuple[str, ...]
    reference_board_path: str | None

    @property
    def provider_images(self) -> tuple[str, ...]:
        """Compatibility alias used by early callers of the design."""

        return self.provider_image_paths


def _sha256(path: Path) -> str:
    digest = hashlib.sha256()
    with path.open("rb") as handle:
        for chunk in iter(lambda: handle.read(1024 * 1024), b""):
            digest.update(chunk)
    return digest.hexdigest()


def _validate_references(reference_paths: Sequence[Path]) -> tuple[Path, ...]:
    normalized: list[Path] = []
    for path in reference_paths:
        candidate = Path(path).expanduser().resolve()
        if not candidate.is_file():
            raise FileNotFoundError(candidate)
        normalized.append(candidate)
    return tuple(normalized)


def _prompt_sections(user_prompt: str) -> dict[str, str]:
    style_line = (
        "apply watercolor rendering only to the replacement character."
        if "watercolor" in user_prompt.lower()
        else "apply any requested rendering style only to the replacement character."
    )
    return {
        "TASK": (
            "Replace the character in the current GIF frame while preserving the original "
            "animation frame and scene."
        ),
        "USER TEXT - HIGHEST PRIORITY": (
            f"User text has highest priority: {user_prompt}. Resolve any conflict in favor "
            "of this text."
        ),
        "REFERENCE IMAGE ROLE": (
            "Reference images are identity guidance only and have lower priority than user "
            "text. Use them for character details; do not copy their background, pose, camera, "
            "or composition."
        ),
        "MUST PRESERVE FROM CURRENT GIF FRAME": (
            "Preserve the original action, character position, preserve the original background "
            "exactly, camera, composition, canvas size, frame timing, frame order, and loop "
            "count."
        ),
        "MAY CHANGE": (
            "May change only the replacement character identity and the style details explicitly "
            f"requested by the user. {style_line}"
        ),
        "MUST NOT CHANGE": (
            "Do not add extra people, text, watermarks, costume drift, background replacement, "
            "camera or framing changes, or canvas-size changes."
        ),
        "OUTPUT CONTRACT": (
            "Output exactly one image at the original canvas size, with one replacement character "
            "and no border, labels, or contact-sheet layout."
        ),
    }


def build_identity_contract(
    user_prompt: str, reference_paths: Sequence[Path]
) -> IdentityContract:
    """Build the immutable identity contract used for every frame request."""

    prompt = user_prompt.strip()
    if not prompt:
        raise ValueError("user_prompt must not be blank")
    references = _validate_references(reference_paths)
    sections = _prompt_sections(prompt)
    provider_prompt = "\n\n".join(
        f"{heading}\n{body}" for heading, body in sections.items()
    )
    return IdentityContract(
        user_prompt=prompt,
        provider_prompt=provider_prompt,
        must_have=(prompt,),
        may_change=("replacement character identity and explicitly requested rendering style",),
        must_preserve=(
            "original action",
            "character position",
            "background",
            "camera",
            "composition",
            "canvas size",
            "frame timing",
            "frame order",
            "loop count",
        ),
        must_not_have=(
            "extra characters",
            "text",
            "watermarks",
            "costume drift",
            "background replacement",
            "camera or framing changes",
            "canvas-size changes",
        ),
        reference_paths=tuple(str(path) for path in references),
        reference_sha256=tuple(_sha256(path) for path in references),
    )


def _draw_reference_board(reference_paths: Sequence[Path], destination: Path) -> None:
    """Render contained thumbnails in a neutral two-column labeled board."""

    columns = 2
    cell_width = 320
    thumb_height = 260
    label_height = 42
    margin = 24
    gap = 16
    rows = math.ceil(len(reference_paths) / columns)
    board_width = margin * 2 + columns * cell_width + (columns - 1) * gap
    board_height = margin * 2 + rows * (thumb_height + label_height) + (rows - 1) * gap
    board = Image.new("RGB", (board_width, board_height), (128, 128, 128))
    draw = ImageDraw.Draw(board)

    for index, path in enumerate(reference_paths):
        row, column = divmod(index, columns)
        left = margin + column * (cell_width + gap)
        top = margin + row * (thumb_height + label_height + gap)
        panel = Image.new("RGB", (cell_width, thumb_height), (218, 218, 218))
        with Image.open(path) as source:
            image = source.convert("RGBA")
            contained = ImageOps.contain(image, (cell_width - 12, thumb_height - 12))
            if contained.mode == "RGBA":
                panel.paste(contained, ((cell_width - contained.width) // 2, (thumb_height - contained.height) // 2), contained)
            else:
                panel.paste(contained, ((cell_width - contained.width) // 2, (thumb_height - contained.height) // 2))
        board.paste(panel, (left, top))
        label = path.name
        draw.rectangle((left, top + thumb_height, left + cell_width, top + thumb_height + label_height), fill=(82, 82, 82))
        draw.text((left + 8, top + thumb_height + 11), label, fill=(255, 255, 255))

    destination.parent.mkdir(parents=True, exist_ok=True)
    board.save(destination, format="PNG")


def prepare_identity_inputs(
    contract: IdentityContract, identity_dir: Path
) -> IdentityInputs:
    """Persist the contract and select direct references or a reference board."""

    identity_dir = Path(identity_dir)
    identity_dir.mkdir(parents=True, exist_ok=True)
    references = tuple(Path(path) for path in contract.reference_paths)
    board_path: Path | None = None
    if len(references) >= 4:
        board_path = identity_dir / "reference_board.png"
        _draw_reference_board(references, board_path)
        provider_paths = (str(board_path.resolve()),)
    else:
        provider_paths = tuple(str(path.resolve()) for path in references)

    contract_path = identity_dir / "identity_contract.json"
    payload: dict[str, Any] = asdict(contract)
    payload["must_have"] = list(contract.must_have)
    payload["may_change"] = list(contract.may_change)
    payload["must_preserve"] = list(contract.must_preserve)
    payload["must_not_have"] = list(contract.must_not_have)
    payload["reference_paths"] = list(contract.reference_paths)
    payload["reference_sha256"] = list(contract.reference_sha256)
    payload["sections"] = {
        heading: body
        for heading, body in _prompt_sections(contract.user_prompt).items()
    }
    payload["provider_image_paths"] = list(provider_paths)
    payload["reference_board_path"] = str(board_path.resolve()) if board_path else None
    contract_path.write_text(
        json.dumps(payload, ensure_ascii=False, indent=2) + "\n", encoding="utf-8"
    )
    return IdentityInputs(
        contract_path=str(contract_path.resolve()),
        provider_image_paths=provider_paths,
        reference_board_path=str(board_path.resolve()) if board_path else None,
    )
