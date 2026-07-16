"""Generate Zero prototype character drafts across configured image backends."""

from __future__ import annotations

import argparse
import asyncio
import json
import sys
from datetime import datetime
from pathlib import Path
from typing import Any


PROJECT_ROOT = Path(__file__).resolve().parents[2]
GATEWAY_ROOT = PROJECT_ROOT / "tools" / "ai-image-gateway"
if str(GATEWAY_ROOT) not in sys.path:
    sys.path.insert(0, str(GATEWAY_ROOT))

from ai_image_gateway import GenerateRequest, ImageService  # noqa: E402


OUTPUT_ROOT = (
    PROJECT_ROOT
    / "UnityClient"
    / "Assets"
    / "Art"
    / "_IncomingAI"
    / "CharacterDesign"
    / "zero_prototype_ai_backend_compare"
)

WIDTH = 832
HEIGHT = 1216
DEFAULT_COUNT = 3


PROMPTS: dict[str, dict[str, Any]] = {
    "chatgpt": {
        "provider": "openai_images",
        "prompt": """
Create a polished full-body anime game character concept sheet for Project P3 character "Zero", using the previous ChatGPT concept-art direction as the visual baseline: refined concept-art polish, gray cloak mass, white cloth blindfold, quiet underground-doll atmosphere.

Subject: a petite cute quiet doll girl, small-frame but not childlike, newly awakened as a prototype doll. She has a white cloth blindfold covering the eyes, pale silver long hair, a complete plain gray shoulder cloak that covers both shoulders, chest, upper arms, and the hidden core chamber. Under the cloak is a very simple ultra-short inner dress. She has bare legs and bare feet, subtle small doll joints at wrists, knees and ankles, light dust and small scratches from long storage. The chest core chamber exists under the cloak but must not be visible in this normal state.

Personality read: initial kuudere / emotionless / mission-ready, restrained like an android soldier, calm and cold rather than sweet. No smile, no crying, no begging. The pose is quiet and slightly unstable, as if she just stepped off a repair table.

Art direction: Japanese anime-inspired 2D game character design, clean linework, soft cel-shaded rendering, delicate body silhouette, understated subterranean fantasy doll design, low visual noise, design-reference clarity.

Composition: vertical portrait, near full-body character centered, clean white or very light neutral background, no room scene, no props, enough empty space around limbs, no text.

Must include: white cloth blindfold, complete gray cloak covering both shoulders, ultra-short simple inner dress, bare legs, bare feet, subtle doll joints, light dust and wear, petite cute proportions.

Must avoid: black blindfold, 2B cosplay, NieR Automata outfit, pink cape, purple dress, begging sign, chest pendant, necklace, large chest ornament, visible exposed core on chest, half-exposed shoulder, one bare shoulder, asymmetrical off-shoulder cloak, mature tall woman, adult sexy body, erotic pose, heavy armor, mecha arms, boots, high heels, gothic lolita, maid dress, military uniform, complex background, room background, blood, gore, toddler or child proportions, copied anime screenshot, watermark, readable text.
""".strip(),
        "negative_prompt": "",
        "extra": {"quality": "high"},
    },
    "gemini_nanobanana": {
        "provider": "gemini_chat_image",
        "prompt": """
Design a vertical anime character design sheet for a game heroine named Zero on a pure white background.

The image must be a clean white-background character reference, not an environment illustration. One near full-body girl only, readable silhouette, no UI, no captions, no room, no furniture, no workshop background. Zero is a petite cute doll-like girl, small-frame but not childlike. Her eyes are hidden by a soft white cloth blindfold. She wears a complete plain gray cloak that covers both shoulders, chest, upper arms, and the hidden core chamber; do not expose one shoulder. Beneath the cloak is a minimal ultra-short inner dress. Her legs are bare and her feet are bare, leaving the lower body visually sparse and fragile. Add only subtle doll-joint seams on wrists, knees and ankles, with a little dust and worn fabric edges. Her hidden core chamber is covered by the cloak and should not be visible.

Mood: cold, restrained, emotionless, kuudere, mission-ready. She should not look warm, smiling, pleading, crying, or helpless. The appeal comes from quiet beauty, controlled posture, and the contrast between covered upper body and empty lower silhouette.

Visual style: Japanese anime game character sheet, clean thin linework, soft cel shading, gentle gray-white palette with restrained accents, elegant but not ornate, white background.

Avoid all direct cosplay or source copying: no black blindfold, no 2B outfit, no pink cape, no begging sign, no purple dress, no chest tag, no necklace, no visible chest core, no bare shoulder, no half-exposed shoulder, no mature tall body, no erotic pose, no boots, no high heels, no heavy machinery, no armor, no background scene, no furniture, no gore, no text, no watermark.
""".strip(),
        "negative_prompt": "",
        "extra": {},
    },
    "novelai": {
        "provider": "novelai",
        "prompt": """
1girl, solo, full body, anime game character design, petite, small frame, cute doll girl, quiet doll girl, expressionless, kuudere, emotionless, white cloth blindfold, blindfold over eyes, pale silver long hair, complete gray cloak, cloak covering both shoulders, covered shoulders, covered chest, shoulder cloak, simple ultra short inner dress, bare legs, bare feet, subtle doll joints, wrist joints, knee joints, ankle joints, light dust, worn fabric, small scratches, fragile body, clean white background, concept art, character sheet, centered composition, clean lineart, soft cel shading, delicate silhouette, muted gray palette, no text
""".strip(),
        "negative_prompt": """
lowres, bad anatomy, bad hands, extra fingers, missing fingers, extra legs, extra arms, deformed, disfigured, toddler, child, explicit loli, chibi, old, mature female, adult woman, tall woman, sexy, erotic, male, multiple girls, black blindfold, 2b, yorha, nier automata, cosplay, pink cape, purple dress, begging sign, signboard, chest tag, necklace, pendant, large chest ornament, exposed chest core, visible core chamber, bare shoulder, one shoulder exposed, off shoulder, asymmetrical cloak, armor, mecha, robot body, mechanical arms, boots, shoes, high heels, gothic lolita, maid, military uniform, complex background, room, furniture, blood, gore, injury fetish, crying, smile, open mouth, text, watermark, logo
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


async def run_backend(
    service: ImageService,
    run_dir: Path,
    backend_id: str,
    spec: dict[str, Any],
    count: int,
) -> dict[str, Any]:
    backend_dir = run_dir / backend_id
    backend_dir.mkdir(parents=True, exist_ok=True)
    start_index = len([
        path for path in backend_dir.iterdir()
        if path.is_file() and path.suffix.lower() in {".png", ".jpg", ".jpeg", ".webp"}
    ])

    request = GenerateRequest(
        provider=spec["provider"],
        prompt=spec["prompt"],
        negative_prompt=spec.get("negative_prompt", ""),
        width=WIDTH,
        height=HEIGHT,
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
        "width": WIDTH,
        "height": HEIGHT,
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
        help="Number of candidates requested per selected backend.",
    )
    parser.add_argument(
        "--output-dir",
        default=None,
        help="Existing or explicit output directory. Defaults to timestamped folder under OUTPUT_ROOT.",
    )
    args = parser.parse_args()

    run_id = datetime.now().strftime("%Y%m%d_%H%M%S")
    run_dir = Path(args.output_dir) if args.output_dir else OUTPUT_ROOT / f"zero_v1_backend_compare_{run_id}"
    run_dir.mkdir(parents=True, exist_ok=True)

    selected = args.only or list(PROMPTS.keys())
    manifests: dict[str, Any] = {}
    async with ImageService(args.config) as service:
        for backend_id in selected:
            print(f"[zero] generating {backend_id} ...", flush=True)
            manifests[backend_id] = await run_backend(
                service,
                run_dir,
                backend_id,
                PROMPTS[backend_id],
                args.count,
            )

    root_manifest = {
        "run_id": run_id,
        "task": "zero_prototype_ai_backend_compare",
        "source_design_doc": "美术文档/人设/03_零号初版人设方案.md",
        "config": args.config,
        "width": WIDTH,
        "height": HEIGHT,
        "count_per_backend": args.count,
        "backends": manifests,
    }
    (run_dir / "manifest.json").write_text(
        json.dumps(root_manifest, ensure_ascii=False, indent=2),
        encoding="utf-8",
    )
    print(str(run_dir))
    return 0


if __name__ == "__main__":
    raise SystemExit(asyncio.run(main()))
