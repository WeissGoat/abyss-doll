"""Generate individual Zero prototype pose/action character references."""

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
WIDTH = 1024
HEIGHT = 1536


POSES: list[dict[str, str]] = [
    {
        "id": "pose_01_front_idle",
        "name": "front idle",
        "prompt": "front-facing neutral standing pose, quiet command-ready posture, arms relaxed at sides, full body centered",
        "nai": "front view, standing, arms at sides, full body",
    },
    {
        "id": "pose_02_three_quarter",
        "name": "three-quarter view",
        "prompt": "one single three-quarter front view only, body turned slightly left, head subtly tilted toward the viewer, cloak silhouette readable, no duplicate body and no extra view",
        "nai": "three-quarter view, slight head tilt, standing, cloak silhouette",
    },
    {
        "id": "pose_03_side_walk",
        "name": "side walking",
        "prompt": "clean side view walking pose, one bare foot stepping forward, long loose hair trailing behind, calm balance",
        "nai": "side view, walking, one foot forward, hair trailing behind",
    },
    {
        "id": "pose_04_back_turn",
        "name": "back turn",
        "prompt": "back view with a slight over-the-shoulder turn, long loose hair falling down the back, gray cloak back hem visible",
        "nai": "back view, looking back, over shoulder, long hair down back, cloak back",
    },
    {
        "id": "pose_05_maintenance_sit",
        "name": "maintenance seated",
        "prompt": "non-sexualized maintenance seated pose on a plain invisible surface, knees together, hands resting on cloak edge, fragile but calm",
        "nai": "sitting, knees together, hands on cloak, maintenance pose, calm",
    },
    {
        "id": "pose_06_activation_ready",
        "name": "activation ready",
        "prompt": "activation-ready pose, feet planted, one hand lightly touching the white blindfold, a clear but subtle red glow visible behind the cloth",
        "nai": "activation pose, hand near blindfold, faint red glow behind blindfold",
    },
    {
        "id": "pose_07_low_san_red_eye",
        "name": "low SAN red-eye",
        "prompt": "low stability state pose, body slightly hunched, blindfold still covering the eyes, visible red eye glow leaking through the white cloth, no blood",
        "nai": "unstable pose, hunched, red glow behind blindfold, no blood",
    },
    {
        "id": "pose_08_damaged_kneel",
        "name": "damaged kneel",
        "prompt": "damage-state kneeling support pose, one hand touching the ground, cloak slightly worn and dusty, red eyes hidden behind the blindfold with a dim red glow, no blood",
        "nai": "kneeling, one hand on ground, dusty cloak, dim red glow behind blindfold, no blood",
    },
]


BACKENDS: dict[str, dict[str, Any]] = {
    "gemini_nanobanana": {
        "provider": "gemini_chat_image",
        "width": WIDTH,
        "height": HEIGHT,
        "extra": {},
        "negative_prompt": "",
    },
    "novelai": {
        "provider": "novelai",
        "width": WIDTH,
        "height": HEIGHT,
        "extra": {
            "steps": 28,
            "cfg": 5.5,
            "sampler": "k_euler",
            "scheduler": "native",
            "model": "nai-diffusion-4-5-full",
        },
        "negative_prompt": """
lowres, bad anatomy, bad hands, extra fingers, missing fingers, extra legs, extra arms, deformed, disfigured, toddler, child, explicit loli, chibi, old, mature female, adult woman, tall woman, sexy, erotic, pinup, male, multiple girls, black blindfold, visible bare eyes in normal pose, blue eyes, 2b, yorha, nier automata, cosplay, ponytail, twintails, braid, braided hair, bun, tied hair, short hair, heavy curls, pink cape, purple dress, begging sign, signboard, chest tag, necklace, pendant, large chest ornament, exposed chest core, visible core chamber, bare shoulder, one shoulder exposed, off shoulder, asymmetrical cloak, armor, mecha, robot body, mechanical arms, boots, shoes, high heels, gothic lolita, maid, military uniform, complex background, room, furniture, blood, gore, injury fetish, smile, open mouth, text, watermark, logo, multi-view, turnaround sheet, split view
""".strip(),
    },
    "chatgpt": {
        "provider": "openai_images",
        "width": 832,
        "height": 1216,
        "extra": {"quality": "medium"},
        "negative_prompt": "",
    },
}

BACKEND_ORDER = ["chatgpt", "gemini_nanobanana", "novelai"]


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


def _base_prompt(pose: dict[str, str]) -> str:
    return f"""
Create one single full-body anime game character reference image of Project P3 character "Zero".

This must be one image with exactly one full-body character only. The character should fill most of the vertical canvas while keeping the whole body visible. Do not draw duplicate bodies, alternate views, a turnaround sheet, multiple views, or a collage. Clean white background. No text, no labels, no UI, no props, no room scene.

Pose and camera: {pose["prompt"]}.

Character identity: Zero is a quiet compact doll-like heroine with a slim non-sexualized model-sheet body. She has silver-white long loose hair worn down naturally, unbound and lightly messy, falling around her shoulders and down her back. Her eyes are normally hidden by a soft white cloth blindfold. Behind the blindfold her real eyes are red; in normal poses they stay covered, and in activation or unstable poses only a faint red glow may show through the cloth.

Costume and body: complete plain gray shoulder cloak covering both shoulders, chest, upper arms, and the hidden core chamber; very simple short inner underdress with no decoration; uncovered legs and unshod feet drawn as neutral character design reference; subtle doll-joint seams at wrists, knees, and ankles; light dust, small scratches, and worn fabric edges. The core chamber stays hidden under the cloak in this image.

Mood and style: cold, restrained, emotionless, kuudere, quiet beauty; clean thin linework, soft cel shading, gentle gray-white palette, readable silhouette, elegant but not ornate.

Avoid: multiple characters, duplicate bodies, alternate views, multiple views, turnaround sheet, collage, complex background, room, furniture, black blindfold, visible exposed eyes unless red glow through cloth is requested by the pose, blue eyes, 2B outfit, pink cape, purple dress, begging sign, chest tag, necklace, visible chest core, bare shoulder, half-exposed shoulder, mature tall body, pinup pose, erotic pose, boots, shoes, high heels, armor, heavy machinery, blood, gore, text, watermark.
""".strip()


def _chatgpt_prompt(pose: dict[str, str]) -> str:
    return f"""
Create one single full-body Japanese anime game character reference on a pure white background. Exactly one character, no alternate views, no collage, no text.

Zero is a petite but non-childlike doll heroine with long loose silver-white hair, a soft white cloth blindfold, and real red eyes hidden behind it. She wears a complete plain gray shoulder cloak covering both shoulders, chest and upper arms, a simple short ivory-gray inner dress, uncovered legs, unshod feet, and subtle doll joints at wrists, knees and ankles. The chest core stays hidden under the cloak.

Pose: {pose["prompt"]}.

Use clean linework, soft cel shading, low visual noise and a restrained cold expression. Keep the entire body large and centered. No exposed eyes; for activation or unstable states, show only red light through the white cloth. No bare shoulder, boots, shoes, armor, machinery, blood, gore, pinup pose or watermark.
""".strip()


def _novelai_prompt(pose: dict[str, str]) -> str:
    if pose["id"] == "pose_06_activation_ready":
        eye_state = "red eyes hidden behind blindfold, clear subtle red glow through white cloth"
    elif pose["id"] == "pose_07_low_san_red_eye":
        eye_state = "red eyes hidden behind blindfold, unstable visible red glow through white cloth"
    elif pose["id"] == "pose_08_damaged_kneel":
        eye_state = "red eyes hidden behind blindfold, dim red glow through white cloth"
    else:
        eye_state = "red eyes fully hidden behind blindfold, no visible eye glow"
    return f"""
1girl, solo, single character, full body, {pose["nai"]}, anime game character design, petite, small frame, quiet doll girl, expressionless, kuudere, emotionless, long hair, very long hair, loose hair, hair down, unbound hair, pale silver hair, white hair, white cloth blindfold, blindfold over eyes, {eye_state}, complete gray cloak, gray shoulder cloak, cloak covering both shoulders, covered shoulders, covered chest, covered upper arms, simple short inner dress, uncovered legs, unshod feet, subtle doll joints, wrist joints, knee joints, ankle joints, light dust, worn fabric, small scratches, fragile body, clean white background, character reference, clean lineart, soft cel shading, delicate silhouette, muted gray palette, no text
""".strip()


async def run_pose_backend(
    service: ImageService,
    run_dir: Path,
    backend_id: str,
    pose: dict[str, str],
    count: int,
) -> dict[str, Any]:
    backend = BACKENDS[backend_id]
    pose_dir = run_dir / pose["id"] / backend_id
    pose_dir.mkdir(parents=True, exist_ok=True)
    start_index = len(_image_files(pose_dir))

    if backend_id == "novelai":
        prompt = _novelai_prompt(pose)
    elif backend_id == "chatgpt":
        prompt = _chatgpt_prompt(pose)
    else:
        prompt = _base_prompt(pose)
    request = GenerateRequest(
        provider=backend["provider"],
        prompt=prompt,
        negative_prompt=backend.get("negative_prompt", ""),
        width=int(backend["width"]),
        height=int(backend["height"]),
        count=count,
        extra=backend.get("extra", {}),
    )

    batch = await service.generate(request)
    files: list[dict[str, Any]] = []
    for index, result in enumerate(batch.results):
        output_index = start_index + index
        ext = _image_extension(result.image_bytes)
        image_path = pose_dir / f"{pose['id']}_{backend_id}_{output_index:02d}{ext}"
        meta_path = pose_dir / f"{pose['id']}_{backend_id}_{output_index:02d}.json"
        image_path.write_bytes(result.image_bytes)
        metadata = {
            "pose_id": pose["id"],
            "pose_name": pose["name"],
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

    manifest = {
        "pose_id": pose["id"],
        "pose_name": pose["name"],
        "backend_id": backend_id,
        "provider": backend["provider"],
        "prompt": prompt,
        "negative_prompt": backend.get("negative_prompt", ""),
        "width": int(backend["width"]),
        "height": int(backend["height"]),
        "count": count,
        "success_count": batch.success_count,
        "errors": batch.errors,
        "files": files,
    }
    (pose_dir / "manifest.json").write_text(
        json.dumps(manifest, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    return manifest


def build_contact_sheet(run_dir: Path, pose_ids: list[str], backend_ids: list[str]) -> Path:
    thumb_w, thumb_h = 220, 330
    label_h = 34
    pad = 12
    cols = len(backend_ids)
    rows = len(pose_ids)
    sheet_w = cols * thumb_w + (cols + 1) * pad
    sheet_h = rows * (label_h + thumb_h) + (rows + 1) * pad
    sheet = Image.new("RGB", (sheet_w, sheet_h), "#f4f4f4")
    draw = ImageDraw.Draw(sheet)
    try:
        font = ImageFont.truetype("arial.ttf", 12)
    except OSError:
        font = ImageFont.load_default()

    for row, pose_id in enumerate(pose_ids):
        for col, backend_id in enumerate(backend_ids):
            x = pad + col * (thumb_w + pad)
            y = pad + row * (thumb_h + label_h + pad)
            files = _image_files(run_dir / pose_id / backend_id)
            label = f"{pose_id}/{backend_id}"
            draw.text((x + 4, y + 7), label, fill="#111111", font=font)
            box_y = y + label_h
            draw.rectangle([x, box_y, x + thumb_w, box_y + thumb_h], fill="#ffffff")
            if files:
                with Image.open(files[-1]) as image:
                    image = image.convert("RGB")
                    image.thumbnail((thumb_w, thumb_h), Image.Resampling.LANCZOS)
                    ox = x + (thumb_w - image.width) // 2
                    oy = box_y + (thumb_h - image.height) // 2
                    sheet.paste(image, (ox, oy))
            else:
                draw.text((x + 26, box_y + thumb_h // 2 - 8), "missing image", fill="#555555", font=font)

    output = run_dir / "contact_sheet_zero_v1_pose_actions.jpg"
    sheet.save(output, quality=92)
    return output


def read_existing_manifest(path: Path) -> dict[str, Any]:
    if path.exists():
        return json.loads(path.read_text(encoding="utf-8"))
    return {}


async def main() -> int:
    parser = argparse.ArgumentParser()
    parser.add_argument("--config", default=str(GATEWAY_ROOT / "config.local.yaml"))
    parser.add_argument("--backend", choices=sorted(BACKENDS.keys()), nargs="*", default=["gemini_nanobanana"])
    parser.add_argument("--pose", choices=[pose["id"] for pose in POSES], nargs="*", default=None)
    parser.add_argument("--count", type=int, default=1)
    parser.add_argument("--output-dir", default=None)
    parser.add_argument("--refresh-only", action="store_true")
    args = parser.parse_args()

    run_id = datetime.now().strftime("%Y%m%d_%H%M%S")
    run_dir = Path(args.output_dir) if args.output_dir else OUTPUT_ROOT / f"zero_v1_pose_actions_{run_id}"
    run_dir.mkdir(parents=True, exist_ok=True)
    selected_poses = [pose for pose in POSES if args.pose is None or pose["id"] in args.pose]
    selected_backend_ids = list(args.backend)

    manifests: dict[str, Any] = {}
    if not args.refresh_only:
        async with ImageService(args.config) as service:
            for pose in selected_poses:
                for backend_id in selected_backend_ids:
                    print(f"[zero-pose] generating {pose['id']} / {backend_id} ...", flush=True)
                    manifests[f"{pose['id']}:{backend_id}"] = await run_pose_backend(
                        service,
                        run_dir,
                        backend_id,
                        pose,
                        args.count,
                    )

    summary_backend_ids = BACKEND_ORDER
    summary_poses = POSES
    pose_ids = [pose["id"] for pose in summary_poses]
    contact_sheet = build_contact_sheet(run_dir, pose_ids, summary_backend_ids)
    summary: dict[str, Any] = {
        "run_dir": str(run_dir),
        "source_design_doc": "美术文档/人设/03_零号初版人设方案.md",
        "pose_ids": pose_ids,
        "backend_ids": summary_backend_ids,
        "contact_sheet": str(contact_sheet),
        "poses": {},
    }
    for pose in summary_poses:
        summary["poses"][pose["id"]] = {"name": pose["name"], "backends": {}}
        for backend_id in summary_backend_ids:
            pose_dir = run_dir / pose["id"] / backend_id
            manifest = manifests.get(f"{pose['id']}:{backend_id}") or read_existing_manifest(pose_dir / "manifest.json")
            summary["poses"][pose["id"]]["backends"][backend_id] = {
                "count": len(_image_files(pose_dir)),
                "files": [str(path) for path in _image_files(pose_dir)],
                "errors": manifest.get("errors", []),
            }
    (run_dir / "final_summary.json").write_text(
        json.dumps(summary, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(str(run_dir))
    return 0


if __name__ == "__main__":
    raise SystemExit(asyncio.run(main()))
