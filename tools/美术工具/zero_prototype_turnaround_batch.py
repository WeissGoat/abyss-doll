"""Generate Zero prototype turnaround sheets across configured image backends."""

from __future__ import annotations

import argparse
import asyncio
import json
import sys
from datetime import datetime
from pathlib import Path
from typing import Any

from PIL import Image, ImageDraw, ImageFont


PROJECT_ROOT = Path(__file__).resolve().parents[2]
GATEWAY_ROOT = PROJECT_ROOT / "tools" / "ai-image-gateway"
if str(GATEWAY_ROOT) not in sys.path:
    sys.path.insert(0, str(GATEWAY_ROOT))

from ai_image_gateway import GenerateRequest, ImageService  # noqa: E402


OUTPUT_ROOT = PROJECT_ROOT / "美术文档" / "人设" / "AI出图"
DEFAULT_COUNT = 3


PROMPTS: dict[str, dict[str, Any]] = {
    "chatgpt": {
        "provider": "openai_images",
        "width": 1536,
        "height": 1024,
        "prompt": """
Create a polished anime game character turnaround sheet for Project P3 character "Zero".

Canvas and layout: one clean white-background model sheet, landscape composition, three full-body views of the same character aligned left to right: front view, side view, back view. Keep the same body scale, same costume, same hair length, same cloak shape, and same proportions across all three views. No text labels, no UI, no room, no props.

Character identity: Zero is a petite cute quiet doll girl, small-frame but not childlike, with a cold emotionless kuudere expression. She has pale silver-white long loose hair worn down naturally, unbound and scattered softly around the shoulders and back. The hair is long, straight to lightly messy, with separate front and back hair masses readable in the side and back views. Do not tie the hair.

Costume: soft white cloth blindfold covering the eyes and wrapping around the head; complete plain gray shoulder cloak covering both shoulders, chest, upper arms, and the hidden core chamber; very simple ultra-short inner dress under the cloak; bare legs and bare feet. Add subtle small doll joints at wrists, knees, and ankles, plus light dust, tiny scratches, and slightly worn cloth edges. The chest core chamber exists under the cloak but must not be visible in this normal turnaround.

Turnaround requirements: front view must clearly show the white blindfold, gray cloak mass, short inner dress, bare legs, bare feet, and doll joints. Side view must show cloak thickness, blindfold wrapping around the head, long loose hair falling behind the shoulder, and ankle/knee joint seams. Back view must show the long loose hair falling down the back, the back hem of the gray cloak, blindfold wrap or knot, and subtle back-body seam hints only if naturally visible.

Art direction: refined Japanese anime 2D game character design, clean linework, soft cel-shaded rendering, delicate doll silhouette, understated subterranean fantasy, low visual noise, production reference clarity.

Must avoid: black blindfold, tied hair, ponytail, twintails, braids, buns, short hair, heavy curls, 2B cosplay, NieR Automata outfit, pink cape, purple dress, begging sign, chest pendant, necklace, large chest ornament, visible exposed core on chest, half-exposed shoulder, one bare shoulder, off-shoulder cloak, mature tall woman, adult sexy body, erotic pose, toddler or child proportions, boots, high heels, heavy armor, mecha limbs, gothic lolita, maid dress, military uniform, complex background, room background, text, watermark.
""".strip(),
        "negative_prompt": "",
        "extra": {"quality": "high"},
    },
    "gemini_nanobanana": {
        "provider": "gemini_chat_image",
        "width": 1536,
        "height": 1024,
        "prompt": """
Design a clean anime game character turnaround sheet for Zero on a pure white background.

Output a single landscape production model sheet containing exactly three aligned full-body views of the same stylized doll heroine: front, side, and back. Keep the character large in frame. All three views must share the same height, costume, hair, cloak shape, and proportions. No environment, no furniture, no workshop, no props, no captions, no labels, no text.

Zero is a quiet compact doll-like heroine with a slim non-sexualized model-sheet body. She has long loose silver-white hair worn down naturally; the hair is unbound, lightly scattered, and falls around the shoulders and down the back. No ponytail, no twintails, no braid, no bun, no short hair. Her eyes are hidden by a soft white cloth blindfold that wraps around her head.

She wears a complete plain gray cloak that covers both shoulders, chest, upper arms, and the hidden core chamber. Do not expose either shoulder. Beneath the cloak is a very simple short inner underdress with no decoration. The lower body is sparse and neutral, with uncovered legs and unshod feet drawn like a plain character model reference, not a pinup. Add subtle doll-joint seams at wrists, knees, and ankles with a little dust and worn fabric edges. The hidden core chamber must remain covered by the cloak and must not be visible.

Mood and style: cold, restrained, emotionless, quiet beauty; clean thin linework, soft cel shading, gentle gray-white palette, readable silhouette, elegant but not ornate.

Avoid: complex background, room, furniture, extra characters, black blindfold, 2B outfit, pink cape, purple dress, begging sign, chest tag, necklace, visible chest core, bare shoulder, half-exposed shoulder, mature tall body, pinup pose, erotic pose, boots, shoes, high heels, armor, heavy machinery, gore, text, watermark.
""".strip(),
        "negative_prompt": "",
        "extra": {},
    },
    "novelai": {
        "provider": "novelai",
        "width": 1536,
        "height": 1024,
        "prompt": """
1girl turnaround sheet, character sheet, multiple views, front view, side view, back view, same character, three views, full body, anime game character design, petite, small frame, cute doll girl, quiet doll girl, expressionless, kuudere, emotionless, long hair, very long hair, loose hair, hair down, unbound hair, pale silver hair, white hair, white cloth blindfold, blindfold over eyes, blindfold wrap, complete gray cloak, gray shoulder cloak, cloak covering both shoulders, covered shoulders, covered chest, covered upper arms, simple ultra short inner dress, bare legs, bare feet, subtle doll joints, wrist joints, knee joints, ankle joints, light dust, worn fabric, small scratches, fragile body, clean white background, landscape model sheet, clean lineart, soft cel shading, delicate silhouette, muted gray palette, no text
""".strip(),
        "negative_prompt": """
lowres, bad anatomy, bad hands, extra fingers, missing fingers, extra legs, extra arms, deformed, disfigured, inconsistent views, different outfits, different character, toddler, child, explicit loli, chibi, old, mature female, adult woman, tall woman, sexy, erotic, male, black blindfold, 2b, yorha, nier automata, cosplay, ponytail, twintails, braid, braided hair, bun, tied hair, short hair, drill hair, heavy curls, pink cape, purple dress, begging sign, signboard, chest tag, necklace, pendant, large chest ornament, exposed chest core, visible core chamber, bare shoulder, one shoulder exposed, off shoulder, asymmetrical cloak, armor, mecha, robot body, mechanical arms, boots, shoes, high heels, gothic lolita, maid, military uniform, complex background, room, furniture, blood, gore, injury fetish, crying, smile, open mouth, text, watermark, logo
""".strip(),
        "extra": {
            "steps": 28,
            "cfg": 5.5,
            "sampler": "k_euler",
            "scheduler": "native",
            "model": "nai-diffusion-4-5-full",
        },
    },
}


def _image_extension(image_bytes: bytes) -> str:
    if image_bytes.startswith(b"\x89PNG\r\n\x1a\n"):
        return ".png"
    if image_bytes.startswith(b"\xff\xd8\xff"):
        return ".jpg"
    if image_bytes.startswith(b"RIFF") and image_bytes[8:12] == b"WEBP":
        return ".webp"
    return ".png"


def _image_files(path: Path) -> list[Path]:
    files: list[Path] = []
    for pattern in ("*.png", "*.jpg", "*.jpeg", "*.webp"):
        files.extend(sorted(path.glob(pattern)))
    return sorted(files)


async def run_backend(
    service: ImageService,
    run_dir: Path,
    backend_id: str,
    spec: dict[str, Any],
    count: int,
) -> dict[str, Any]:
    backend_dir = run_dir / backend_id
    backend_dir.mkdir(parents=True, exist_ok=True)
    start_index = len(_image_files(backend_dir))
    width = int(spec["width"])
    height = int(spec["height"])

    request = GenerateRequest(
        provider=spec["provider"],
        prompt=spec["prompt"],
        negative_prompt=spec.get("negative_prompt", ""),
        width=width,
        height=height,
        count=count,
        extra=spec.get("extra", {}),
    )

    batch = await service.generate(request)
    files: list[dict[str, Any]] = []

    for index, result in enumerate(batch.results):
        ext = _image_extension(result.image_bytes)
        output_index = start_index + index
        image_path = backend_dir / f"{backend_id}_{output_index:02d}{ext}"
        meta_path = backend_dir / f"{backend_id}_{output_index:02d}.json"
        image_path.write_bytes(result.image_bytes)
        metadata = {
            "backend_id": backend_id,
            "provider": result.provider_name,
            "model": result.model_name,
            "seed": result.seed,
            "generation_params": result.generation_params,
            "cost": result.cost,
            "bytes": len(result.image_bytes),
        }
        meta_path.write_text(json.dumps(metadata, ensure_ascii=False, indent=2), encoding="utf-8")
        files.append({"image": str(image_path), "metadata": str(meta_path)})

    backend_manifest = {
        "backend_id": backend_id,
        "provider": spec["provider"],
        "prompt": spec["prompt"],
        "negative_prompt": spec.get("negative_prompt", ""),
        "width": width,
        "height": height,
        "count": count,
        "success_count": batch.success_count,
        "errors": batch.errors,
        "files": files,
    }
    (backend_dir / "manifest.json").write_text(
        json.dumps(backend_manifest, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    return backend_manifest


def build_contact_sheet(run_dir: Path, selected: list[str]) -> Path:
    thumb_w, thumb_h = 360, 240
    label_h = 30
    pad = 14
    cols = 3
    rows = len(selected)
    sheet_w = cols * thumb_w + (cols + 1) * pad
    sheet_h = rows * (label_h + thumb_h) + (rows + 1) * pad
    sheet = Image.new("RGB", (sheet_w, sheet_h), "#f4f4f4")
    draw = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("arial.ttf", 13)
    except OSError:
        font = ImageFont.load_default()

    for row, backend_id in enumerate(selected):
        files = _image_files(run_dir / backend_id)[:cols]
        for col in range(cols):
            x = pad + col * (thumb_w + pad)
            y = pad + row * (thumb_h + label_h + pad)
            label = f"{backend_id}/{files[col].stem if col < len(files) else 'missing'}"
            draw.text((x + 4, y + 7), label, fill="#111111", font=font)
            box_y = y + label_h
            draw.rectangle([x, box_y, x + thumb_w, box_y + thumb_h], fill="#ffffff")
            if col < len(files):
                with Image.open(files[col]) as image:
                    image = image.convert("RGB")
                    image.thumbnail((thumb_w, thumb_h), Image.Resampling.LANCZOS)
                    ox = x + (thumb_w - image.width) // 2
                    oy = box_y + (thumb_h - image.height) // 2
                    sheet.paste(image, (ox, oy))
            else:
                draw.text((x + 32, box_y + thumb_h // 2 - 8), "missing image", fill="#555555", font=font)

    output = run_dir / "contact_sheet_zero_v1_turnaround.jpg"
    sheet.save(output, quality=92)
    return output


def build_final_summary(run_dir: Path, selected: list[str], contact_sheet: Path) -> Path:
    backends = {}
    for backend_id in selected:
        backend_manifest_path = run_dir / backend_id / "manifest.json"
        backend_manifest = {}
        if backend_manifest_path.exists():
            backend_manifest = json.loads(backend_manifest_path.read_text(encoding="utf-8"))
        backends[backend_id] = {
            "count": len(_image_files(run_dir / backend_id)),
            "files": [str(path) for path in _image_files(run_dir / backend_id)],
            "errors": backend_manifest.get("errors", []),
        }
    summary = {
        "run_dir": str(run_dir),
        "source_design_doc": "美术文档/人设/03_零号初版人设方案.md",
        "prompt_doc": "美术文档/人设/04_零号AI后端出图提示词对比.md",
        "contact_sheet": str(contact_sheet),
        "backends": backends,
        "notes": {
            "chatgpt": "Three-view prompt emphasizes polished concept-art consistency, long loose hair, gray cloak structure, and production model-sheet clarity.",
            "gemini_nanobanana": "Three-view prompt emphasizes pure white background, large readable character, consistent front/side/back model-sheet layout, and no scene elements.",
            "novelai": "Tag prompt emphasizes multiple views, loose long hair, white blindfold, covered shoulders, doll joints, and negative tags against tied hair or exposed shoulders.",
        },
    }
    output = run_dir / "final_summary.json"
    output.write_text(json.dumps(summary, ensure_ascii=False, indent=2), encoding="utf-8")
    return output


async def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument(
        "--config",
        default=str(GATEWAY_ROOT / "config.local.yaml"),
        help="Path to ai-image-gateway config.",
    )
    parser.add_argument(
        "--only",
        choices=sorted(PROMPTS.keys()),
        nargs="*",
        default=None,
        help="Optional subset of backends to run.",
    )
    parser.add_argument(
        "--count",
        type=int,
        default=DEFAULT_COUNT,
        help="Number of turnaround candidates requested per selected backend.",
    )
    parser.add_argument(
        "--output-dir",
        default=None,
        help="Existing or explicit output directory. Defaults to timestamped folder under 美术文档/人设/AI出图.",
    )
    parser.add_argument(
        "--refresh-only",
        action="store_true",
        help="Do not generate images; rebuild manifest summary and contact sheet from existing backend folders.",
    )
    args = parser.parse_args()

    run_id = datetime.now().strftime("%Y%m%d_%H%M%S")
    run_dir = Path(args.output_dir) if args.output_dir else OUTPUT_ROOT / f"zero_v1_turnaround_{run_id}"
    run_dir.mkdir(parents=True, exist_ok=True)

    selected = args.only or list(PROMPTS.keys())
    manifests: dict[str, Any] = {}
    if not args.refresh_only:
        async with ImageService(args.config) as service:
            for backend_id in selected:
                print(f"[zero-turnaround] generating {backend_id} ...", flush=True)
                manifests[backend_id] = await run_backend(
                    service,
                    run_dir,
                    backend_id,
                    PROMPTS[backend_id],
                    args.count,
                )

    for backend_id in PROMPTS:
        if backend_id in manifests:
            continue
        manifest_path = run_dir / backend_id / "manifest.json"
        if manifest_path.exists():
            manifests[backend_id] = json.loads(manifest_path.read_text(encoding="utf-8"))

    root_manifest = {
        "run_id": run_id,
        "task": "zero_prototype_turnaround",
        "source_design_doc": "美术文档/人设/03_零号初版人设方案.md",
        "config": args.config,
        "count_per_backend": args.count,
        "backends": manifests,
    }
    (run_dir / "manifest.json").write_text(
        json.dumps(root_manifest, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    summary_backends = list(PROMPTS.keys())
    contact_sheet = build_contact_sheet(run_dir, summary_backends)
    build_final_summary(run_dir, summary_backends, contact_sheet)
    print(str(run_dir))
    return 0


if __name__ == "__main__":
    raise SystemExit(asyncio.run(main()))
