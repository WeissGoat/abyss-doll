"""Turns green-screen pose candidates into a pose set that swaps cleanly with a base image in combat.

  python tools/美术工具/fit_pose_set.py --base <base.png> --set <poseSetID> --out <dir> attack=a.jpeg guard=g.jpeg ...
      [--area kneel=0.8 --area windup=0.95] [--preview <background.png>]

For each pose: key out the green background (unmixed against the measured background colour, so semi-transparent
hair and steam keep their own colour), scale it so its visible area matches the base figure's (poses then neither
grow nor shrink when they swap), put its lowest point on the base figure's baseline and centre it on the base's
centre, on a canvas the size of the base. Writes <out>/<poseSetID>_<pose>.png and, with --preview, _preview.png
(every pose over the background). --area scales the target area for poses that show less of the body (a kneel).
The base image itself is not changed; a matching standing pose goes in as "idle".
"""
import argparse
import os

import numpy as np
from PIL import Image


def background_colour(rgb):
    h, w = rgb.shape[:2]
    s = max(4, min(h, w) // 20)
    patches = [rgb[:s, :s], rgb[:s, -s:], rgb[-s:, :s], rgb[-s:, -s:]]
    return np.median(np.concatenate([p.reshape(-1, 3) for p in patches]), axis=0)


def key_green(img, noise=12.0):
    # Keys whichever pure colour the background is (green by default; blue for a green creature).
    rgb = np.asarray(img.convert('RGB')).astype(np.float32)
    bg = background_colour(rgb)
    c = int(np.argmax(bg))
    o1, o2 = [k for k in range(3) if k != c]
    spill = rgb[..., c] - np.maximum(rgb[..., o1], rgb[..., o2])
    bg_spill = max(1.0, bg[c] - max(bg[o1], bg[o2]))
    alpha = 1.0 - np.clip((spill - noise) / (bg_spill - 2 * noise), 0, 1)
    a = np.maximum(alpha, 0.05)[..., None]
    fg = (rgb - (1.0 - a) * bg) / a
    fg[..., c] = np.minimum(fg[..., c], np.maximum(fg[..., o1], fg[..., o2]) + 6.0)
    return Image.fromarray(np.dstack([fg, alpha[..., None] * 255.0]).clip(0, 255).astype(np.uint8), 'RGBA')


def stats(im):
    opaque = np.asarray(im)[..., 3] > 128
    ys, xs = np.nonzero(opaque)
    return opaque.sum(), xs.min(), xs.max(), ys.min(), ys.max()


def fit(pose, base, factor):
    area_b, x0b, x1b, _, y1b = stats(base)
    area_p, x0, x1, y0, y1 = stats(pose)
    s = (area_b * factor / area_p) ** 0.5
    crop = pose.crop((x0, y0, x1 + 1, y1 + 1))
    w, h = max(1, round(crop.width * s)), max(1, round(crop.height * s))
    W, H = base.size
    if h > H - 8 or w > W - 8:
        k = min((H - 8) / h, (W - 8) / w)
        w, h = round(w * k), round(h * k)
    crop = crop.resize((w, h), Image.LANCZOS)
    canvas = Image.new('RGBA', base.size, (0, 0, 0, 0))
    x = int(min(max((x0b + x1b) / 2 - w / 2, 4), W - w - 4))
    y = max(4, int(min(y1b + 1 - h, H - h)))
    canvas.alpha_composite(crop, (x, y))
    return canvas


def preview(results, background, path):
    bg = Image.open(background).convert('RGBA').resize((480, 270))
    cols = 3
    rows = (len(results) + cols - 1) // cols
    sheet = Image.new('RGBA', (480 * cols, 270 * rows), (40, 40, 40, 255))
    for i, (_, im) in enumerate(results):
        tile = bg.copy()
        thumb = im.copy()
        thumb.thumbnail((250, 250))
        tile.alpha_composite(thumb, ((480 - thumb.width) // 2, 270 - thumb.height - 6))
        sheet.alpha_composite(tile, ((i % cols) * 480, (i // cols) * 270))
    sheet.convert('RGB').save(path)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--base', required=True, help='the image the poses swap with (its canvas size and baseline)')
    parser.add_argument('--set', required=True, help='pose set ID, e.g. doll_zero_combat or a CombatVisualID')
    parser.add_argument('--out', required=True)
    parser.add_argument('--area', action='append', default=[], help='pose=factor, e.g. kneel=0.8')
    parser.add_argument('--preview', help='background image for the preview sheet')
    parser.add_argument('poses', nargs='+', help='pose=candidate image on green')
    args = parser.parse_args()

    areas = dict((k, float(v)) for k, v in (a.split('=', 1) for a in args.area))
    base = Image.open(args.base).convert('RGBA')
    os.makedirs(args.out, exist_ok=True)
    results = []
    for item in args.poses:
        pose, path = item.split('=', 1)
        fitted = fit(key_green(Image.open(path)), base, areas.get(pose, 1.0))
        name = f'{args.set}_{pose}'
        fitted.save(os.path.join(args.out, name + '.png'))
        results.append((name, fitted))
        print('wrote', name)
    if args.preview:
        preview(results, args.preview, os.path.join(args.out, '_preview.png'))


if __name__ == '__main__':
    main()
