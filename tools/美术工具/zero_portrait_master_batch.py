"""Generate Gemini-guided production candidates for Zero portrait masters."""

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

from ai_image_gateway import ImageService, ImageToImageRequest  # noqa: E402


OUTPUT_ROOT = PROJECT_ROOT / "美术文档" / "人设" / "AI出图"
PROVIDER = "gemini_chat_image"
WIDTH = 1024
HEIGHT = 1536


ASSETS: dict[str, dict[str, str]] = {
    "zero_stand_neutral": {
        "name": "front neutral portrait master",
        "pose": (
            "strict front-facing orthographic full-body standing pose, feet parallel, "
            "arms relaxed naturally at both sides, quiet command-ready posture"
        ),
    },
    "zero_dialogue_neutral": {
        "name": "three-quarter dialogue portrait master",
        "pose": (
            "single three-quarter front full-body standing pose, torso turned about 25 degrees, "
            "head gently oriented toward the viewer, restrained conversational posture"
        ),
    },
    "zero_maintenance_sit": {
        "name": "maintenance seated portrait master",
        "pose": (
            "single compact seated maintenance pose, knees kept together and angled slightly to one side, "
            "bare feet fully visible, hands resting calmly near the cloak edges, non-sexualized"
        ),
    },
}


def build_prompt(asset_id: str) -> str:
    asset = ASSETS[asset_id]
    return f"""
Use the supplied images only as identity, costume, hairstyle, and pose references. Redraw one polished production-ready Japanese anime game character portrait of Project P3 character Zero. Preserve the same recognizable character identity across the batch.

Output contract: exactly one character, one view, vertical 2:3 canvas, clean perfectly white background, no floor line, no cast shadow, no scenery, no props, no text, no border, no UI. Keep the complete body visible from hair tip to bare toes. The character must occupy 85 to 90 percent of the canvas height and be centered with narrow even margins.

Pose: {asset['pose']}.

Identity: a petite and cute but clearly non-childlike young doll heroine, slim compact proportions, quiet restrained kuudere expression, small closed mouth. Long loose silver-white hair worn fully down and unbound, naturally scattered, extending below the waist toward the upper thighs. Keep the face shape and understated anime design close to the supplied Gemini reference.

Eyes and blindfold: both real eyes are red but completely hidden in this normal state. Cover the eye area with one soft aged white cloth blindfold. No visible pupils, no red glow, no colored eye slit, no metal mask, no black blindfold.

Costume: one independent plain medium-gray shoulder cape, clearly separate from the inner dress. The cape fully covers both shoulders, upper chest, hidden core chamber, and upper arms. It must read as a short cape with two front panels, not a poncho dress and not a long cloak. Under it is a minimal light gray-white ultra-short inner dress with a simple loose silhouette and no ornament. Bare legs and fully bare feet. No shoes, socks, stockings, anklets, gloves, necklace, brooch, chest tag, chest ornament, armor, or exposed core.

Doll details: only tiny subtle seam lines at wrists, knees, and ankles. Add light dust, faint scratches, and slightly worn fabric edges. Do not use large ball joints, skeletal robot limbs, exposed machinery, blood, wounds, or gore.

Rendering: refined 2D anime game character concept art, clean thin linework, soft cel shading, muted gray-white palette with natural pale skin, low visual noise, readable silhouette, professional standing-portrait finish. Neutral eye-level camera. Avoid pinup framing, erotic emphasis, exaggerated breasts, exaggerated thighs, mature tall fashion-model proportions, chibi proportions, toddler traits, or child coding.

Critical invariants: complete gray cape covering both shoulders; white cloth blindfold; long loose hair; normal state has no red light; no chest ornament; core hidden; bare legs; bare feet; one character only; portrait orientation; character fills most of the canvas.
""".strip()


def image_extension(image_bytes: bytes) -> str:
    if image_bytes.startswith(b"\x89PNG\r\n\x1a\n"):
        return ".png"
    if image_bytes.startswith(b"\xff\xd8\xff"):
        return ".jpg"
    if image_bytes.startswith(b"RIFF") and image_bytes[8:12] == b"WEBP":
        return ".webp"
    return ".png"


def image_files(path: Path) -> list[Path]:
    files: list[Path] = []
    for pattern in ("*.png", "*.jpg", "*.jpeg", "*.webp"):
        files.extend(path.glob(pattern))
    return sorted(files)


def build_contact_sheet(output_dir: Path, asset_id: str) -> Path:
    files = image_files(output_dir)
    thumb_w, thumb_h = 360, 540
    pad, label_h = 16, 42
    columns = max(1, min(3, len(files)))
    rows = max(1, (len(files) + columns - 1) // columns)
    sheet = Image.new(
        "RGB",
        (columns * thumb_w + (columns + 1) * pad, rows * (thumb_h + label_h) + (rows + 1) * pad),
        "#eeeeee",
    )
    draw = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("arial.ttf", 18)
    except OSError:
        font = ImageFont.load_default()

    for index, path in enumerate(files):
        row, column = divmod(index, columns)
        x = pad + column * (thumb_w + pad)
        y = pad + row * (thumb_h + label_h + pad)
        draw.text((x + 4, y + 8), path.name, fill="#111111", font=font)
        with Image.open(path) as source:
            image = source.convert("RGB")
            image.thumbnail((thumb_w, thumb_h), Image.Resampling.LANCZOS)
            ox = x + (thumb_w - image.width) // 2
            oy = y + label_h + (thumb_h - image.height) // 2
            sheet.paste(image, (ox, oy))

    contact_path = output_dir / f"contact_sheet_{asset_id}.jpg"
    sheet.save(contact_path, quality=92)
    return contact_path


async def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--config", default=str(GATEWAY_ROOT / "config.local.yaml"))
    parser.add_argument("--asset", choices=sorted(ASSETS), required=True)
    parser.add_argument("--reference", action="append", required=True)
    parser.add_argument("--count", type=int, default=1)
    parser.add_argument("--output-dir")
    args = parser.parse_args()

    references = [Path(path).resolve() for path in args.reference]
    missing = [str(path) for path in references if not path.exists()]
    if missing:
        raise FileNotFoundError(f"Missing reference images: {missing}")

    run_id = datetime.now().strftime("%Y%m%d_%H%M%S")
    root = Path(args.output_dir) if args.output_dir else OUTPUT_ROOT / f"zero_v1_portrait_masters_{run_id}"
    output_dir = root / args.asset / "gemini_nanobanana"
    output_dir.mkdir(parents=True, exist_ok=True)
    start_index = len(image_files(output_dir))
    prompt = build_prompt(args.asset)
    request = ImageToImageRequest(
        provider=PROVIDER,
        images=[path.read_bytes() for path in references],
        prompt=prompt,
        width=WIDTH,
        height=HEIGHT,
        count=args.count,
    )

    async with ImageService(args.config) as service:
        batch = await service.image_to_image(request)

    outputs: list[dict[str, Any]] = []
    for index, result in enumerate(batch.results):
        output_index = start_index + index
        extension = image_extension(result.image_bytes)
        image_path = output_dir / f"{args.asset}_gemini_nanobanana_{output_index:02d}{extension}"
        metadata_path = output_dir / f"{args.asset}_gemini_nanobanana_{output_index:02d}.json"
        image_path.write_bytes(result.image_bytes)
        metadata = {
            "asset_id": args.asset,
            "provider": result.provider_name,
            "model": result.model_name,
            "seed": result.seed,
            "generation_params": result.generation_params,
            "cost": result.cost,
            "bytes": len(result.image_bytes),
            "references": [str(path) for path in references],
        }
        metadata_path.write_text(json.dumps(metadata, ensure_ascii=False, indent=2), encoding="utf-8")
        outputs.append({"image": str(image_path), "metadata": str(metadata_path)})

    manifest = {
        "asset_id": args.asset,
        "asset_name": ASSETS[args.asset]["name"],
        "provider": PROVIDER,
        "mode": "image_to_image",
        "width": WIDTH,
        "height": HEIGHT,
        "count": args.count,
        "success_count": batch.success_count,
        "errors": batch.errors,
        "references": [str(path) for path in references],
        "prompt": prompt,
        "outputs": outputs,
    }
    (output_dir / "manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2), encoding="utf-8"
    )
    contact_sheet = build_contact_sheet(output_dir, args.asset)
    print(json.dumps({"output_dir": str(output_dir), "contact_sheet": str(contact_sheet), **manifest}, ensure_ascii=False))
    return 0 if batch.success_count > 0 else 1


if __name__ == "__main__":
    raise SystemExit(asyncio.run(main()))
