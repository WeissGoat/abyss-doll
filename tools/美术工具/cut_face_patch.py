# -*- coding: utf-8 -*-
"""Cuts small face parts (a smiling mouth, a wince) out of image-to-image edits, as patches laid over the portrait.

  python tools/美术工具/cut_face_patch.py --base <portrait.png> --crop x0,y0,x1,y1 --center x,y --size w,h
      --out <dir> --prefix <visualID>_ [--preview <out.png>] smile=edit1.jpeg happy=edit2.jpeg ...

--crop is the box of the portrait that was sent for editing (full-size pixels), --center/--size the part to cut (a
box around the mouth). Each edit is aligned back onto that box (the editor may return another size and framing:
features are matched and a similarity transform fitted), its colours are matched to the portrait on a ring around
the part, and an elliptical patch with a soft edge is written as <prefix><name>.png at full-size resolution. The
patch replaces the part when drawn at --center (UIPortraitPuppet places it on her head).
"""
import argparse
import os

import cv2
import numpy as np
from PIL import Image


def numbers(text, count):
    values = [int(round(float(v))) for v in text.split(',')]
    if len(values) != count:
        raise SystemExit(f'expected {count} numbers, got {text}')
    return values


def align(edit, ref):
    """Similarity transform taking the edit onto the reference crop."""
    g1 = cv2.cvtColor(edit, cv2.COLOR_RGB2GRAY)
    g2 = cv2.cvtColor(ref, cv2.COLOR_RGB2GRAY)
    sift = cv2.SIFT_create(4000)
    k1, d1 = sift.detectAndCompute(g1, None)
    k2, d2 = sift.detectAndCompute(g2, None)
    matches = cv2.BFMatcher().knnMatch(d1, d2, k=2)
    good = [m for m, n in matches if m.distance < 0.75 * n.distance]
    src = np.float32([k1[m.queryIdx].pt for m in good])
    dst = np.float32([k2[m.trainIdx].pt for m in good])
    matrix, inliers = cv2.estimateAffinePartial2D(src, dst, method=cv2.RANSAC, ransacReprojThreshold=3)
    if matrix is None:
        raise SystemExit('could not align the edit to the crop')
    return matrix, int(inliers.sum()), len(good)


def ellipse(w, h, inner, outer):
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
    d = np.sqrt(((xs + 0.5 - w / 2) / (w / 2)) ** 2 + ((ys + 0.5 - h / 2) / (h / 2)) ** 2)
    t = np.clip((outer - d) / (outer - inner), 0, 1)
    return t * t * (3 - 2 * t)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--base', required=True)
    parser.add_argument('--crop', required=True)
    parser.add_argument('--center', required=True)
    parser.add_argument('--size', required=True)
    parser.add_argument('--out', required=True)
    parser.add_argument('--prefix', default='')
    parser.add_argument('--preview')
    parser.add_argument('parts', nargs='+', help='name=edited image')
    args = parser.parse_args()

    base = np.asarray(Image.open(args.base).convert('RGBA')).astype(np.float32)
    x0, y0, x1, y1 = numbers(args.crop, 4)
    cx, cy = numbers(args.center, 2)
    pw, ph = numbers(args.size, 2)
    white = base[..., :3] * base[..., 3:4] / 255 + 255 * (1 - base[..., 3:4] / 255)
    ref = white[y0:y1, x0:x1].astype(np.uint8)
    px0, py0 = cx - pw // 2, cy - ph // 2
    original = white[py0:py0 + ph, px0:px0 + pw]
    alpha = ellipse(pw, ph, 0.55, 0.95)
    ring = (ellipse(pw, ph, 0.7, 1.0) > 0) & (ellipse(pw, ph, 0.45, 0.6) < 1)
    os.makedirs(args.out, exist_ok=True)
    tiles = []
    for item in args.parts:
        name, path = item.split('=', 1)
        edit = np.asarray(Image.open(path).convert('RGB'))
        # Align at the reference crop's scale first, so feature sizes are comparable.
        scale = ref.shape[0] / edit.shape[0]
        small = cv2.resize(edit, (round(edit.shape[1] * scale), ref.shape[0]), interpolation=cv2.INTER_AREA)
        matrix, inliers, good = align(small, ref)
        warped = cv2.warpAffine(small, matrix, (ref.shape[1], ref.shape[0]), flags=cv2.INTER_LANCZOS4).astype(np.float32)
        part = warped[py0 - y0:py0 - y0 + ph, px0 - x0:px0 - x0 + pw]
        # Same colours as the portrait around the part (the editor shifts skin tone a little).
        gain = original[ring].mean(0) / np.maximum(part[ring].mean(0), 1)
        part = np.clip(part * gain, 0, 255)
        patch = np.dstack([part, alpha * 255]).astype(np.uint8)
        out = os.path.join(args.out, f'{args.prefix}{name}.png')
        Image.fromarray(patch, 'RGBA').save(out)
        err = np.abs(part - original)[ring].mean()
        print(f'wrote {out}: {inliers}/{good} matches, scale {np.hypot(*matrix[0, :2]):.3f}, ring error {err:.1f}')
        composite = original * (1 - alpha[..., None]) + part * alpha[..., None]
        tiles.append(composite)
    if args.preview:
        row = [original] + tiles
        big = [cv2.resize(t.astype(np.uint8), (pw * 4, ph * 4), interpolation=cv2.INTER_NEAREST) for t in row]
        Image.fromarray(np.concatenate(big, 1)).save(args.preview)


if __name__ == '__main__':
    main()
