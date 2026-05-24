# -*- coding: utf-8 -*-
"""Create deterministic local_v0 art assets for already specified Manifest items.

This tool is intentionally not an AI-image fallback. It creates simple readable
runtime sprites so VisualID wiring, Registry work, and ArtAcceptance can proceed
while formal AI art remains tracked by the Visual V2 replacement backlog.
"""

from __future__ import annotations

import argparse
import json
import math
import random
import re
import uuid
from datetime import datetime, timezone
from pathlib import Path
from typing import Any

from PIL import Image, ImageDraw, ImageFilter


SCRIPT_DIR = Path(__file__).resolve().parent
PROJECT_ROOT = SCRIPT_DIR.parents[1]
DEFAULT_PLAN = "美术文档/_generated/缺图生成计划.json"
DEFAULT_MANIFEST = "美术文档/_generated/art_manifest.json"
APPROVED_STATUSES = {"approved", "registered", "validated"}


PALETTE = {
    "brass": (196, 145, 64, 255),
    "brass_light": (242, 190, 99, 255),
    "brass_dark": (96, 65, 34, 255),
    "metal": (45, 49, 58, 245),
    "metal_light": (95, 101, 112, 255),
    "shadow": (4, 6, 10, 120),
    "red": (211, 70, 52, 235),
    "orange": (248, 137, 60, 245),
    "green": (89, 205, 126, 235),
    "acid": (118, 235, 99, 230),
    "blue": (90, 178, 235, 235),
    "purple": (156, 88, 220, 235),
    "cream": (224, 205, 157, 240),
}


def resolve_project_path(value: str) -> Path:
    path = Path(value)
    return path if path.is_absolute() else PROJECT_ROOT / path


def repo_path(path: Path) -> str:
    try:
        return path.resolve().relative_to(PROJECT_ROOT.resolve()).as_posix()
    except ValueError:
        return path.as_posix()


def read_json(path: Path) -> Any:
    return json.loads(path.read_text(encoding="utf-8-sig"))


def write_json(path: Path, data: Any) -> None:
    path.parent.mkdir(parents=True, exist_ok=True)
    path.write_text(json.dumps(data, ensure_ascii=False, indent=2) + "\n", encoding="utf-8")


def split_filters(values: list[str]) -> set[str]:
    result: set[str] = set()
    for value in values:
        for part in value.split(","):
            part = part.strip()
            if part:
                result.add(part)
    return result


def source_spec(entry: dict[str, Any]) -> dict[str, Any]:
    spec = entry.get("Spec")
    if not isinstance(spec, dict):
        return {}
    nested = spec.get("SourceSpec")
    return nested if isinstance(nested, dict) else spec


def expected_size(entry: dict[str, Any]) -> tuple[int, int]:
    src = source_spec(entry)
    try:
        width = int(src.get("Width", 512) or 512)
        height = int(src.get("Height", 512) or 512)
    except (TypeError, ValueError):
        return (512, 512)
    return (max(16, width), max(16, height))


def append_note(entry: dict[str, Any], message: str) -> None:
    existing = str(entry.get("Notes", "") or "").strip()
    entry["Notes"] = f"{existing}\n{message}".strip() if existing else message


def select_plan_items(plan: dict[str, Any], visual_ids: set[str]) -> list[dict[str, Any]]:
    items = [item for item in plan.get("Items", []) if isinstance(item, dict)]
    result: list[dict[str, Any]] = []
    for item in items:
        visual_id = str(item.get("VisualID", "") or "").strip()
        if not visual_id:
            continue
        if visual_ids and visual_id not in visual_ids:
            continue
        if str(item.get("Status", "") or "") in APPROVED_STATUSES:
            continue
        if not item.get("PromptReady", False):
            continue
        result.append(item)
    return result


def make_manifest_map(manifest: dict[str, Any]) -> dict[str, dict[str, Any]]:
    return {
        str(entry.get("VisualID", "")): entry
        for entry in manifest.get("Entries", [])
        if isinstance(entry, dict) and entry.get("VisualID")
    }


def rgba(color: str, alpha: int | None = None) -> tuple[int, int, int, int]:
    value = PALETTE[color]
    if alpha is None:
        return value
    return (value[0], value[1], value[2], alpha)


class Canvas:
    def __init__(self, width: int, height: int, scale: int = 4) -> None:
        self.width = width
        self.height = height
        self.scale = scale
        self.image = Image.new("RGBA", (width * scale, height * scale), (0, 0, 0, 0))
        self.draw = ImageDraw.Draw(self.image, "RGBA")

    def p(self, x: float, y: float) -> tuple[int, int]:
        return (round(x * self.width * self.scale), round(y * self.height * self.scale))

    def box(self, x0: float, y0: float, x1: float, y1: float) -> tuple[int, int, int, int]:
        return (*self.p(x0, y0), *self.p(x1, y1))

    def w(self, value: float) -> int:
        return max(1, round(value * min(self.width, self.height) * self.scale))

    def line(self, points: list[tuple[float, float]], fill: tuple[int, int, int, int], width: float) -> None:
        self.draw.line([self.p(x, y) for x, y in points], fill=fill, width=self.w(width), joint="curve")

    def polygon(self, points: list[tuple[float, float]], fill: tuple[int, int, int, int], outline=None) -> None:
        self.draw.polygon([self.p(x, y) for x, y in points], fill=fill, outline=outline)

    def ellipse(self, box: tuple[float, float, float, float], fill, outline=None, width: float = 0.0) -> None:
        self.draw.ellipse(self.box(*box), fill=fill, outline=outline, width=self.w(width) if width else 1)

    def rect(self, box: tuple[float, float, float, float], fill, outline=None, width: float = 0.0) -> None:
        self.draw.rectangle(self.box(*box), fill=fill, outline=outline, width=self.w(width) if width else 1)

    def round_rect(self, box: tuple[float, float, float, float], radius: float, fill, outline=None, width: float = 0.0) -> None:
        self.draw.rounded_rectangle(
            self.box(*box),
            radius=self.w(radius),
            fill=fill,
            outline=outline,
            width=self.w(width) if width else 1,
        )

    def arc(self, box: tuple[float, float, float, float], start: float, end: float, fill, width: float) -> None:
        self.draw.arc(self.box(*box), start=start, end=end, fill=fill, width=self.w(width))

    def glow(self, box: tuple[float, float, float, float], color, blur: float = 0.025) -> None:
        layer = Image.new("RGBA", self.image.size, (0, 0, 0, 0))
        draw = ImageDraw.Draw(layer, "RGBA")
        draw.ellipse(self.box(*box), fill=color)
        self.image.alpha_composite(layer.filter(ImageFilter.GaussianBlur(self.w(blur))))
        self.draw = ImageDraw.Draw(self.image, "RGBA")

    def finish(self) -> Image.Image:
        if self.scale == 1:
            return self.image
        return self.image.resize((self.width, self.height), Image.Resampling.LANCZOS)


def draw_badge(c: Canvas, glow_color=rgba("brass", 60)) -> None:
    c.glow((0.16, 0.16, 0.84, 0.84), glow_color, 0.035)
    c.ellipse((0.18, 0.18, 0.82, 0.82), rgba("shadow"), None)
    c.ellipse((0.20, 0.20, 0.80, 0.80), rgba("metal"), rgba("brass_light"), 0.018)
    c.ellipse((0.28, 0.28, 0.72, 0.72), (12, 15, 20, 210), rgba("brass_dark"), 0.01)


def draw_node_event(c: Canvas) -> None:
    draw_badge(c)
    c.round_rect((0.34, 0.38, 0.66, 0.62), 0.035, rgba("cream"), rgba("brass_dark"), 0.012)
    c.ellipse((0.28, 0.38, 0.38, 0.62), rgba("cream"), rgba("brass_dark"), 0.01)
    c.ellipse((0.62, 0.38, 0.72, 0.62), rgba("cream"), rgba("brass_dark"), 0.01)
    c.line([(0.43, 0.46), (0.57, 0.46)], rgba("brass_dark", 180), 0.012)
    c.line([(0.43, 0.54), (0.54, 0.54)], rgba("brass_dark", 150), 0.01)
    c.ellipse((0.58, 0.25, 0.66, 0.33), rgba("blue", 220), None)
    c.ellipse((0.40, 0.67, 0.46, 0.73), rgba("brass_light", 230), None)


def draw_node_hazard(c: Canvas) -> None:
    draw_badge(c, rgba("red", 70))
    c.polygon([(0.50, 0.24), (0.76, 0.70), (0.24, 0.70)], rgba("orange"), rgba("brass_dark"))
    c.polygon([(0.50, 0.33), (0.63, 0.62), (0.37, 0.62)], rgba("metal"), None)
    c.polygon([(0.50, 0.38), (0.58, 0.56), (0.48, 0.65), (0.40, 0.52)], rgba("acid"), None)
    c.line([(0.50, 0.31), (0.50, 0.48)], rgba("brass_light"), 0.018)
    c.ellipse((0.47, 0.56, 0.53, 0.62), rgba("brass_light"), None)


def draw_node_rest(c: Canvas) -> None:
    draw_badge(c, rgba("green", 60))
    c.glow((0.38, 0.32, 0.62, 0.63), rgba("brass_light", 95), 0.03)
    c.round_rect((0.39, 0.35, 0.61, 0.62), 0.035, rgba("brass"), rgba("brass_light"), 0.012)
    c.round_rect((0.43, 0.40, 0.57, 0.58), 0.02, (255, 197, 88, 155), None)
    c.arc((0.38, 0.26, 0.62, 0.48), 200, 340, rgba("brass_light"), 0.018)
    c.round_rect((0.31, 0.64, 0.69, 0.74), 0.035, rgba("green", 200), rgba("brass_dark"), 0.01)
    c.line([(0.35, 0.69), (0.65, 0.69)], rgba("cream", 180), 0.008)


def draw_node_treasure(c: Canvas) -> None:
    draw_badge(c, rgba("brass_light", 80))
    c.glow((0.30, 0.28, 0.70, 0.72), rgba("brass_light", 100), 0.035)
    c.round_rect((0.28, 0.42, 0.72, 0.68), 0.035, rgba("brass"), rgba("brass_light"), 0.014)
    c.round_rect((0.31, 0.32, 0.69, 0.51), 0.05, rgba("brass_dark"), rgba("brass_light"), 0.014)
    c.line([(0.28, 0.51), (0.72, 0.51)], rgba("brass_light"), 0.018)
    c.round_rect((0.46, 0.49, 0.54, 0.60), 0.014, rgba("metal"), rgba("brass_light"), 0.008)
    c.ellipse((0.43, 0.22, 0.48, 0.27), rgba("brass_light", 210), None)
    c.ellipse((0.57, 0.25, 0.63, 0.31), rgba("brass_light", 180), None)


def draw_hit_feedback(c: Canvas) -> None:
    center = (0.5, 0.5)
    points = []
    for index in range(18):
        radius = 0.42 if index % 2 == 0 else 0.18
        angle = -math.pi / 2 + index * math.tau / 18
        points.append((center[0] + math.cos(angle) * radius, center[1] + math.sin(angle) * radius))
    c.glow((0.19, 0.19, 0.81, 0.81), rgba("red", 80), 0.035)
    c.polygon(points, rgba("red"), None)
    c.polygon([(0.5, 0.25), (0.62, 0.50), (0.5, 0.75), (0.38, 0.50)], rgba("orange", 235), None)
    for angle in [0.1, 1.2, 2.4, 3.7, 5.0]:
        c.line(
            [(0.5, 0.5), (0.5 + math.cos(angle) * 0.45, 0.5 + math.sin(angle) * 0.45)],
            rgba("brass_light", 180),
            0.008,
        )


def draw_shield_break(c: Canvas) -> None:
    c.glow((0.20, 0.18, 0.80, 0.84), rgba("blue", 80), 0.035)
    c.polygon([(0.50, 0.18), (0.76, 0.30), (0.70, 0.64), (0.50, 0.82), (0.30, 0.64), (0.24, 0.30)], rgba("blue", 130), rgba("brass_light"))
    c.line([(0.49, 0.20), (0.43, 0.44), (0.54, 0.50), (0.45, 0.72)], rgba("cream"), 0.017)
    c.line([(0.58, 0.32), (0.51, 0.48), (0.64, 0.60)], rgba("cream", 210), 0.013)
    c.polygon([(0.66, 0.25), (0.82, 0.18), (0.76, 0.38)], rgba("blue", 180), None)
    c.polygon([(0.27, 0.56), (0.12, 0.66), (0.33, 0.72)], rgba("blue", 170), None)


def draw_square_marker(c: Canvas, mode: str) -> None:
    color = rgba("red") if mode == "lock" else rgba("acid")
    c.glow((0.08, 0.08, 0.92, 0.92), (*color[:3], 75), 0.025)
    c.round_rect((0.12, 0.12, 0.88, 0.88), 0.035, (0, 0, 0, 0), color, 0.025)
    c.round_rect((0.22, 0.22, 0.78, 0.78), 0.02, (0, 0, 0, 0), rgba("brass", 150), 0.01)
    if mode == "lock":
        c.line([(0.22, 0.78), (0.78, 0.22)], rgba("red", 220), 0.035)
        c.arc((0.38, 0.28, 0.62, 0.54), 180, 360, rgba("brass_light"), 0.018)
        c.round_rect((0.36, 0.48, 0.64, 0.68), 0.018, rgba("metal"), rgba("brass_light"), 0.01)
    else:
        c.ellipse((0.30, 0.34, 0.50, 0.54), rgba("acid", 120), None)
        c.polygon([(0.56, 0.35), (0.70, 0.42), (0.64, 0.58), (0.51, 0.50)], rgba("metal_light", 180), None)
        c.ellipse((0.57, 0.60, 0.67, 0.70), rgba("acid", 150), None)


def draw_intent_attack(c: Canvas) -> None:
    draw_badge(c, rgba("red", 65))
    c.polygon([(0.28, 0.69), (0.65, 0.25), (0.73, 0.32), (0.39, 0.76)], rgba("metal_light"), rgba("brass_light"))
    c.polygon([(0.63, 0.22), (0.80, 0.19), (0.75, 0.36)], rgba("cream"), None)
    c.round_rect((0.25, 0.66, 0.39, 0.80), 0.02, rgba("brass"), rgba("brass_light"), 0.008)


def draw_intent_defend(c: Canvas) -> None:
    draw_badge(c, rgba("blue", 65))
    c.polygon([(0.50, 0.23), (0.72, 0.32), (0.68, 0.62), (0.50, 0.78), (0.32, 0.62), (0.28, 0.32)], rgba("metal_light"), rgba("brass_light"))
    c.line([(0.50, 0.30), (0.50, 0.72)], rgba("brass_light", 210), 0.012)
    c.line([(0.34, 0.43), (0.66, 0.43)], rgba("brass", 210), 0.012)


def draw_intent_buff(c: Canvas) -> None:
    draw_badge(c, rgba("green", 70))
    c.polygon([(0.50, 0.22), (0.70, 0.50), (0.58, 0.50), (0.58, 0.74), (0.42, 0.74), (0.42, 0.50), (0.30, 0.50)], rgba("green"), rgba("brass_light"))
    c.line([(0.38, 0.62), (0.62, 0.62)], rgba("brass_light", 180), 0.012)
    c.ellipse((0.47, 0.14, 0.53, 0.20), rgba("green", 180), None)


def draw_intent_charge(c: Canvas) -> None:
    draw_badge(c, rgba("orange", 70))
    c.ellipse((0.33, 0.33, 0.67, 0.67), rgba("metal"), rgba("brass_light"), 0.018)
    for angle in range(0, 360, 45):
        x = 0.5 + math.cos(math.radians(angle)) * 0.25
        y = 0.5 + math.sin(math.radians(angle)) * 0.25
        c.line([(0.5, 0.5), (x, y)], rgba("brass"), 0.009)
    c.arc((0.37, 0.37, 0.63, 0.63), 20, 320, rgba("orange"), 0.026)
    c.line([(0.62, 0.27), (0.74, 0.18), (0.70, 0.34)], rgba("brass_light"), 0.014)


def draw_intent_debuff(c: Canvas) -> None:
    draw_badge(c, rgba("purple", 70))
    c.polygon([(0.50, 0.78), (0.30, 0.50), (0.42, 0.50), (0.42, 0.27), (0.58, 0.27), (0.58, 0.50), (0.70, 0.50)], rgba("purple"), rgba("brass_light"))
    c.line([(0.35, 0.38), (0.65, 0.63)], rgba("cream", 190), 0.011)
    c.line([(0.38, 0.64), (0.62, 0.36)], rgba("cream", 160), 0.009)


def draw_intent_grid_lock(c: Canvas) -> None:
    draw_badge(c, rgba("red", 60))
    c.round_rect((0.29, 0.28, 0.71, 0.70), 0.02, (0, 0, 0, 0), rgba("brass_light"), 0.014)
    for value in [0.43, 0.57]:
        c.line([(value, 0.28), (value, 0.70)], rgba("brass", 210), 0.008)
        c.line([(0.29, value), (0.71, value)], rgba("brass", 210), 0.008)
    c.arc((0.39, 0.33, 0.61, 0.58), 180, 360, rgba("cream"), 0.014)
    c.round_rect((0.37, 0.50, 0.63, 0.67), 0.016, rgba("red"), rgba("brass_light"), 0.008)


def draw_intent_move(c: Canvas) -> None:
    draw_badge(c, rgba("blue", 60))
    c.round_rect((0.38, 0.38, 0.62, 0.62), 0.025, rgba("brass"), rgba("brass_light"), 0.01)
    c.polygon([(0.50, 0.18), (0.42, 0.30), (0.58, 0.30)], rgba("blue"), None)
    c.polygon([(0.50, 0.82), (0.42, 0.70), (0.58, 0.70)], rgba("blue"), None)
    c.polygon([(0.18, 0.50), (0.30, 0.42), (0.30, 0.58)], rgba("blue"), None)
    c.polygon([(0.82, 0.50), (0.70, 0.42), (0.70, 0.58)], rgba("blue"), None)


def draw_intent_san(c: Canvas) -> None:
    draw_badge(c, rgba("purple", 70))
    c.ellipse((0.25, 0.36, 0.75, 0.62), rgba("metal_light"), rgba("brass_light"), 0.012)
    c.ellipse((0.42, 0.38, 0.58, 0.58), rgba("purple"), rgba("cream"), 0.008)
    c.ellipse((0.47, 0.43, 0.53, 0.53), (8, 10, 18, 255), None)
    c.polygon([(0.55, 0.61), (0.64, 0.77), (0.47, 0.77)], rgba("blue", 190), None)


def draw_intent_unknown(c: Canvas) -> None:
    draw_badge(c, rgba("purple", 45))
    c.ellipse((0.27, 0.38, 0.73, 0.61), rgba("metal_light", 180), rgba("brass", 170), 0.01)
    c.polygon([(0.27, 0.28), (0.76, 0.39), (0.68, 0.73), (0.20, 0.63)], rgba("purple", 185), rgba("brass_dark"))
    c.line([(0.32, 0.50), (0.68, 0.50)], rgba("cream", 120), 0.012)


def draw_intent_add_junk(c: Canvas) -> None:
    draw_badge(c, rgba("acid", 55))
    c.round_rect((0.34, 0.50, 0.66, 0.72), 0.03, rgba("metal"), rgba("brass_light"), 0.012)
    c.arc((0.32, 0.34, 0.68, 0.64), 200, 340, rgba("brass_light"), 0.018)
    random.seed(17)
    for _ in range(7):
        x = random.uniform(0.30, 0.70)
        y = random.uniform(0.25, 0.50)
        size = random.uniform(0.025, 0.055)
        c.polygon([(x, y), (x + size, y + size * 0.4), (x + size * 0.35, y + size)], rgba("metal_light"), rgba("acid", 150))


def draw_status_corrosion(c: Canvas) -> None:
    draw_badge(c, rgba("acid", 65))
    c.polygon([(0.50, 0.23), (0.66, 0.53), (0.55, 0.75), (0.38, 0.72), (0.33, 0.53)], rgba("acid"), rgba("brass_light"))
    c.line([(0.32, 0.65), (0.68, 0.38)], rgba("green", 200), 0.018)
    c.ellipse((0.61, 0.63, 0.72, 0.74), rgba("acid", 160), None)


def draw_status_curse(c: Canvas) -> None:
    draw_badge(c, rgba("purple", 70))
    c.polygon([(0.35, 0.25), (0.68, 0.31), (0.63, 0.75), (0.30, 0.68)], rgba("purple"), rgba("brass_light"))
    c.line([(0.42, 0.33), (0.58, 0.67)], rgba("cream", 160), 0.011)
    c.line([(0.58, 0.35), (0.43, 0.56), (0.55, 0.58)], rgba("brass_light", 180), 0.009)
    c.arc((0.24, 0.27, 0.74, 0.73), 20, 330, rgba("brass", 170), 0.009)


def draw_status_stun(c: Canvas) -> None:
    draw_badge(c, rgba("blue", 70))
    c.ellipse((0.34, 0.34, 0.66, 0.66), rgba("brass"), rgba("brass_light"), 0.014)
    c.ellipse((0.43, 0.43, 0.57, 0.57), rgba("metal"), None)
    for angle in range(0, 360, 60):
        x0 = 0.5 + math.cos(math.radians(angle)) * 0.18
        y0 = 0.5 + math.sin(math.radians(angle)) * 0.18
        x1 = 0.5 + math.cos(math.radians(angle)) * 0.30
        y1 = 0.5 + math.sin(math.radians(angle)) * 0.30
        c.line([(x0, y0), (x1, y1)], rgba("brass_light"), 0.012)
    c.line([(0.25, 0.31), (0.36, 0.18), (0.38, 0.34)], rgba("blue"), 0.014)
    c.line([(0.66, 0.72), (0.77, 0.61), (0.75, 0.78)], rgba("blue"), 0.014)


def draw_icon_chassis_upgrade(c: Canvas) -> None:
    draw_badge(c, rgba("green", 65))
    c.round_rect((0.28, 0.50, 0.72, 0.72), 0.03, rgba("metal"), rgba("brass_light"), 0.012)
    c.round_rect((0.34, 0.56, 0.66, 0.66), 0.018, (0, 0, 0, 0), rgba("brass", 190), 0.008)
    c.polygon([(0.50, 0.20), (0.68, 0.43), (0.57, 0.43), (0.57, 0.57), (0.43, 0.57), (0.43, 0.43), (0.32, 0.43)], rgba("green"), rgba("brass_light"))
    c.line([(0.34, 0.76), (0.66, 0.76)], rgba("brass_light", 180), 0.012)
    for x in (0.33, 0.67):
        c.ellipse((x - 0.035, 0.47, x + 0.035, 0.54), rgba("brass_light", 210), None)


def draw_icon_blueprint(c: Canvas) -> None:
    draw_badge(c, rgba("blue", 65))
    c.round_rect((0.28, 0.28, 0.72, 0.72), 0.035, rgba("blue", 165), rgba("brass_light"), 0.012)
    c.line([(0.36, 0.38), (0.64, 0.38)], rgba("cream", 180), 0.008)
    c.line([(0.36, 0.50), (0.58, 0.50)], rgba("cream", 170), 0.008)
    c.line([(0.36, 0.62), (0.66, 0.62)], rgba("cream", 160), 0.008)
    c.round_rect((0.50, 0.43, 0.66, 0.56), 0.016, (0, 0, 0, 0), rgba("cream", 190), 0.007)
    c.ellipse((0.61, 0.23, 0.70, 0.32), rgba("brass_light", 230), rgba("brass_dark"), 0.006)


def draw_icon_material_need(c: Canvas) -> None:
    draw_badge(c, rgba("orange", 70))
    c.round_rect((0.29, 0.42, 0.70, 0.70), 0.035, rgba("brass"), rgba("brass_light"), 0.012)
    c.round_rect((0.34, 0.34, 0.65, 0.48), 0.03, rgba("brass_dark"), rgba("brass_light"), 0.01)
    c.line([(0.39, 0.42), (0.49, 0.56), (0.43, 0.68)], rgba("metal"), 0.018)
    c.polygon([(0.70, 0.24), (0.84, 0.50), (0.56, 0.50)], rgba("orange"), rgba("brass_dark"))
    c.line([(0.70, 0.31), (0.70, 0.42)], rgba("cream"), 0.01)
    c.ellipse((0.675, 0.44, 0.725, 0.49), rgba("cream"), None)


def draw_icon_customer(c: Canvas) -> None:
    draw_badge(c, rgba("brass_light", 70))
    c.ellipse((0.40, 0.24, 0.60, 0.44), rgba("cream"), rgba("brass_dark"), 0.009)
    c.round_rect((0.32, 0.46, 0.68, 0.72), 0.06, rgba("metal_light"), rgba("brass_light"), 0.012)
    c.round_rect((0.24, 0.62, 0.76, 0.76), 0.035, rgba("brass"), rgba("brass_light"), 0.012)
    c.line([(0.30, 0.62), (0.70, 0.62)], rgba("brass_dark", 180), 0.008)
    c.ellipse((0.36, 0.32, 0.42, 0.38), rgba("brass_dark"), None)
    c.ellipse((0.58, 0.32, 0.64, 0.38), rgba("brass_dark"), None)


def draw_icon_sale_spark(c: Canvas) -> None:
    c.glow((0.18, 0.18, 0.82, 0.82), rgba("brass_light", 105), 0.04)
    for angle in range(0, 360, 30):
        inner = 0.18 if angle % 60 == 0 else 0.24
        outer = 0.42 if angle % 60 == 0 else 0.34
        x0 = 0.5 + math.cos(math.radians(angle)) * inner
        y0 = 0.5 + math.sin(math.radians(angle)) * inner
        x1 = 0.5 + math.cos(math.radians(angle)) * outer
        y1 = 0.5 + math.sin(math.radians(angle)) * outer
        c.line([(x0, y0), (x1, y1)], rgba("brass_light", 220), 0.012)
    c.ellipse((0.30, 0.30, 0.70, 0.70), rgba("orange", 190), rgba("brass_light"), 0.014)
    c.ellipse((0.39, 0.39, 0.61, 0.61), rgba("brass_light"), rgba("brass_dark"), 0.01)
    c.line([(0.43, 0.50), (0.57, 0.50)], rgba("brass_dark", 190), 0.012)
    c.line([(0.50, 0.43), (0.50, 0.57)], rgba("brass_dark", 170), 0.009)


def draw_icon_business_settlement(c: Canvas) -> None:
    draw_badge(c, rgba("green", 55))
    c.round_rect((0.25, 0.48, 0.75, 0.70), 0.035, rgba("brass"), rgba("brass_light"), 0.012)
    c.round_rect((0.30, 0.36, 0.70, 0.53), 0.03, rgba("metal"), rgba("brass_light"), 0.01)
    c.line([(0.33, 0.36), (0.40, 0.25), (0.60, 0.25), (0.67, 0.36)], rgba("brass_light"), 0.012)
    c.ellipse((0.34, 0.58, 0.45, 0.69), rgba("brass_light"), rgba("brass_dark"), 0.006)
    c.ellipse((0.46, 0.57, 0.57, 0.68), rgba("brass_light", 220), rgba("brass_dark"), 0.006)
    c.ellipse((0.58, 0.58, 0.69, 0.69), rgba("brass_light", 200), rgba("brass_dark"), 0.006)
    c.line([(0.32, 0.76), (0.68, 0.76)], rgba("brass_light", 180), 0.012)


DRAWERS = {
    "node_eventnode_icon": draw_node_event,
    "node_hazardnode_icon": draw_node_hazard,
    "node_reststopnode_icon": draw_node_rest,
    "node_treasurenode_icon": draw_node_treasure,
    "ui_combat_feedback_hit": draw_hit_feedback,
    "ui_combat_feedback_shield_break": draw_shield_break,
    "ui_combat_grid_lock_marker": lambda c: draw_square_marker(c, "lock"),
    "ui_combat_junk_preview_marker": lambda c: draw_square_marker(c, "junk"),
    "ui_combat_intent_add_junk": draw_intent_add_junk,
    "ui_combat_intent_attack": draw_intent_attack,
    "ui_combat_intent_buff": draw_intent_buff,
    "ui_combat_intent_charge": draw_intent_charge,
    "ui_combat_intent_debuff": draw_intent_debuff,
    "ui_combat_intent_defend": draw_intent_defend,
    "ui_combat_intent_grid_lock": draw_intent_grid_lock,
    "ui_combat_intent_move_item": draw_intent_move,
    "ui_combat_intent_san_pressure": draw_intent_san,
    "ui_combat_intent_unknown": draw_intent_unknown,
    "ui_combat_status_corrosion": draw_status_corrosion,
    "ui_combat_status_curse": draw_status_curse,
    "ui_combat_status_stun": draw_status_stun,
    "ui_icon_blueprint": draw_icon_blueprint,
    "ui_icon_chassis_upgrade": draw_icon_chassis_upgrade,
    "ui_icon_business_settlement": draw_icon_business_settlement,
    "ui_icon_customer": draw_icon_customer,
    "ui_icon_material_need": draw_icon_material_need,
    "ui_icon_sale_spark": draw_icon_sale_spark,
}


def draw_fallback(c: Canvas, visual_id: str) -> None:
    draw_badge(c)
    digest = uuid.uuid5(uuid.NAMESPACE_URL, visual_id).hex
    color_keys = ["brass", "green", "blue", "purple", "orange"]
    color = rgba(color_keys[int(digest[0], 16) % len(color_keys)])
    for index in range(5):
        angle = index * math.tau / 5 - math.pi / 2
        next_angle = (index + 2) * math.tau / 5 - math.pi / 2
        c.line(
            [
                (0.5 + math.cos(angle) * 0.25, 0.5 + math.sin(angle) * 0.25),
                (0.5 + math.cos(next_angle) * 0.25, 0.5 + math.sin(next_angle) * 0.25),
            ],
            color,
            0.018,
        )


def render_asset(visual_id: str, width: int, height: int) -> Image.Image:
    canvas = Canvas(width, height)
    drawer = DRAWERS.get(visual_id)
    if drawer:
        drawer(canvas)
    else:
        draw_fallback(canvas, visual_id)
    return canvas.finish()


def meta_text(max_size: int) -> str:
    guid = uuid.uuid4().hex
    sprite_id = uuid.uuid4().hex
    return f"""fileFormatVersion: 2
guid: {guid}
TextureImporter:
  internalIDToNameTable: []
  externalObjects: {{}}
  serializedVersion: 13
  mipmaps:
    mipMapMode: 0
    enableMipMap: 0
    sRGBTexture: 1
    linearTexture: 0
    fadeOut: 0
    borderMipMap: 0
    mipMapsPreserveCoverage: 0
    alphaTestReferenceValue: 0.5
    mipMapFadeDistanceStart: 1
    mipMapFadeDistanceEnd: 3
  bumpmap:
    convertToNormalMap: 0
    externalNormalMap: 0
    heightScale: 0.25
    normalMapFilter: 0
    flipGreenChannel: 0
  isReadable: 0
  streamingMipmaps: 0
  streamingMipmapsPriority: 0
  vTOnly: 0
  ignoreMipmapLimit: 0
  grayScaleToAlpha: 0
  generateCubemap: 6
  cubemapConvolution: 0
  seamlessCubemap: 0
  textureFormat: 1
  maxTextureSize: 2048
  textureSettings:
    serializedVersion: 2
    filterMode: 1
    aniso: 1
    mipBias: 0
    wrapU: 1
    wrapV: 1
    wrapW: 1
  nPOTScale: 0
  lightmap: 0
  compressionQuality: 50
  spriteMode: 1
  spriteExtrude: 1
  spriteMeshType: 1
  alignment: 0
  spritePivot: {{x: 0.5, y: 0.5}}
  spritePixelsToUnits: 100
  spriteBorder: {{x: 0, y: 0, z: 0, w: 0}}
  spriteGenerateFallbackPhysicsShape: 1
  alphaUsage: 1
  alphaIsTransparency: 1
  spriteTessellationDetail: -1
  textureType: 8
  textureShape: 1
  singleChannelComponent: 0
  flipbookRows: 1
  flipbookColumns: 1
  maxTextureSizeSet: 0
  compressionQualitySet: 0
  textureFormatSet: 0
  ignorePngGamma: 0
  applyGammaDecoding: 0
  swizzle: 50462976
  cookieLightType: 0
  platformSettings:
  - serializedVersion: 3
    buildTarget: DefaultTexturePlatform
    maxTextureSize: {max_size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  - serializedVersion: 3
    buildTarget: Standalone
    maxTextureSize: {max_size}
    resizeAlgorithm: 0
    textureFormat: -1
    textureCompression: 1
    compressionQuality: 50
    crunchedCompression: 0
    allowsAlphaSplitting: 0
    overridden: 0
    ignorePlatformSupport: 0
    androidETC2FallbackOverride: 0
    forceMaximumCompressionQuality_BC6H_BC7: 0
  spriteSheet:
    serializedVersion: 2
    sprites: []
    outline: []
    physicsShape: []
    bones: []
    spriteID: {sprite_id}
    internalID: 0
    vertices: []
    indices:
    edges: []
    weights: []
    secondaryTextures: []
    nameFileIdTable: {{}}
  mipmapLimitGroupName:
  pSDRemoveMatte: 0
  userData:
  assetBundleName:
  assetBundleVariant:
"""


def update_manifest_entry(entry: dict[str, Any], item: dict[str, Any], target: Path, batch_id: str, created_at: str) -> None:
    entry["Status"] = "approved"
    entry["BatchID"] = batch_id
    entry["ApprovedPath"] = repo_path(target)
    entry["SelectedPath"] = repo_path(target)
    entry["QualityTier"] = "local_v0"
    entry["QualityUpdatedAt"] = created_at
    append_note(
        entry,
        f"[{created_at}] local_v0 approved directly in Approved: {repo_path(target)}; formal AI replacement remains required.",
    )


def parse_args() -> argparse.Namespace:
    parser = argparse.ArgumentParser(description="Generate local_v0 art for planned missing Manifest assets.")
    parser.add_argument("--batch-plan-path", default=DEFAULT_PLAN)
    parser.add_argument("--manifest-path", default=DEFAULT_MANIFEST)
    parser.add_argument("--visual-id", action="append", default=[])
    parser.add_argument("--batch-id", default="local_v0_missing_assets")
    parser.add_argument("--dry-run", action="store_true")
    parser.add_argument("--overwrite", action="store_true")
    return parser.parse_args()


def main() -> int:
    args = parse_args()
    plan_path = resolve_project_path(args.batch_plan_path)
    manifest_path = resolve_project_path(args.manifest_path)
    visual_ids = split_filters(args.visual_id)

    plan = read_json(plan_path)
    manifest = read_json(manifest_path)
    manifest_map = make_manifest_map(manifest)
    items = select_plan_items(plan, visual_ids)
    created_at = datetime.now(timezone.utc).astimezone().isoformat(timespec="seconds")

    print(f"[PLAN] local_v0 items={len(items)} batch={args.batch_id}")
    generated = 0
    skipped = 0
    for item in items:
        visual_id = str(item["VisualID"])
        entry = manifest_map.get(visual_id)
        if entry is None:
            print(f"[SKIP] {visual_id} missing manifest entry")
            skipped += 1
            continue
        target_value = str(item.get("ApprovedPath") or entry.get("OutputPath") or "")
        if not target_value:
            print(f"[SKIP] {visual_id} missing approved path")
            skipped += 1
            continue
        target = resolve_project_path(target_value)
        width, height = expected_size(entry)
        print(f"[ITEM] {visual_id} {width}x{height} -> {repo_path(target)}")
        if args.dry_run:
            continue
        if target.exists() and not args.overwrite:
            print(f"[SKIP] {visual_id} target exists; use --overwrite")
            skipped += 1
            continue
        target.parent.mkdir(parents=True, exist_ok=True)
        render_asset(visual_id, width, height).save(target)
        meta_path = target.with_suffix(target.suffix + ".meta")
        if not meta_path.exists() or args.overwrite:
            meta_path.write_text(meta_text(max(512, width, height)), encoding="utf-8", newline="\n")
        update_manifest_entry(entry, item, target, args.batch_id, created_at)
        print(f"[OK] {visual_id}")
        generated += 1

    if not args.dry_run:
        write_json(manifest_path, manifest)
    print(f"[DONE] generated={generated} skipped={skipped} dry_run={args.dry_run}")
    return 0


if __name__ == "__main__":
    raise SystemExit(main())
