# -*- coding: utf-8 -*-
"""A large portrait with its faces: one image per face, the base with that face's patch laid on, keyed off the green.

  python tools/美术工具/make_bust_set.py --base <bust on green> --crop x0,y0,x1,y1 --center x,y --width 1024
      --out <dir> --id doll_zero_bust [--preview <out.png>] smile=<patch.png> sad=<patch.png> ...

The patches are cut_face_patch.py output for the same --base (full-size, placed at --center). Writes <id>.png (the
base, neutral) and <id>_<face>.png, cropped to --crop and scaled to --width; the preview shows them side by side on a
dark ground, with the faces enlarged underneath.
"""
import argparse
import os

import numpy as np
from PIL import Image

from fit_pose_set import key_green


def numbers(text, count):
    values = [int(round(float(v))) for v in text.split(',')]
    if len(values) != count:
        raise SystemExit(f'expected {count} numbers, got {text}')
    return values


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--base', required=True)
    parser.add_argument('--crop', required=True)
    parser.add_argument('--center', required=True)
    parser.add_argument('--width', type=int, default=1024)
    parser.add_argument('--out', required=True)
    parser.add_argument('--id', required=True)
    parser.add_argument('--preview')
    parser.add_argument('faces', nargs='*', help='face=patch png')
    args = parser.parse_args()

    base = Image.open(args.base).convert('RGB')
    x0, y0, x1, y1 = numbers(args.crop, 4)
    cx, cy = numbers(args.center, 2)
    os.makedirs(args.out, exist_ok=True)
    made = []
    for item in [None] + args.faces:
        image = base.copy()
        name = args.id
        if item:
            face, path = item.split('=', 1)
            patch = Image.open(path).convert('RGBA')
            image = image.convert('RGBA')
            image.alpha_composite(patch, (cx - patch.width // 2, cy - patch.height // 2))
            name = f'{args.id}_{face}'
        keyed = key_green(image.convert('RGB')).crop((x0, y0, x1, y1))
        height = round(keyed.height * args.width / keyed.width)
        keyed = keyed.resize((args.width, height), Image.LANCZOS)
        out = os.path.join(args.out, name + '.png')
        keyed.save(out)
        made.append((name, keyed))
        print('wrote', out, keyed.size)

    if args.preview:
        ground = (48, 46, 58, 255)
        small = []
        faces = []
        k = args.width / (x1 - x0)
        fx, fy = (cx - x0) * k, (cy - y0) * k
        for _, im in made:
            bg = Image.new('RGBA', im.size, ground)
            bg.alpha_composite(im)
            small.append(bg.convert('RGB').resize((im.width // 3, im.height // 3)))
            r = 0.16 * im.width
            faces.append(bg.convert('RGB').crop((int(fx - r), int(fy - r * 0.8), int(fx + r), int(fy + r * 0.6))).resize((small[-1].width, int(small[-1].width * 0.7))))
        w = sum(i.width for i in small)
        sheet = Image.new('RGB', (w, small[0].height + faces[0].height), ground[:3])
        x = 0
        for a, b in zip(small, faces):
            sheet.paste(a, (x, 0))
            sheet.paste(b, (x, small[0].height))
            x += a.width
        sheet.save(args.preview)


if __name__ == '__main__':
    main()
