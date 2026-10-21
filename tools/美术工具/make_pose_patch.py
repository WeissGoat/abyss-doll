# -*- coding: utf-8 -*-
"""A pose change as a patch over the portrait: only the part of an image-to-image edit that differs (an arm reaching
out), on the portrait's canvas, so a puppet can fade it in over her own (UIPortraitPuppet "_hold").

  python tools/美术工具/make_pose_patch.py --base <portrait.png> --edit <edit on green> --out <visualID>_hold.png
      [--below 0.38] [--cover x0,y0,x1,y1 ...] [--preview <out.png>]

The edit is aligned back onto the portrait (features matched on the portrait laid on green, similarity transform),
keyed off the green, and kept only where it differs from the portrait, grown and feathered so the change has room.
--below is the share of the height above which nothing is taken (her head and shoulders stay the portrait's own, so
head motion still lines up). --cover adds boxes (portrait pixels) that are always taken, for what the edit removed
but did not visibly change around (her old clasped hands on the dress). The preview shows portrait, patch and the two laid together.
"""
import argparse

import cv2
import numpy as np
from PIL import Image

from fit_pose_set import key_green


def align(edit, ref):
    g1 = cv2.cvtColor(edit, cv2.COLOR_RGB2GRAY)
    g2 = cv2.cvtColor(ref, cv2.COLOR_RGB2GRAY)
    sift = cv2.SIFT_create(6000)
    k1, d1 = sift.detectAndCompute(g1, None)
    k2, d2 = sift.detectAndCompute(g2, None)
    good = [m for m, n in cv2.BFMatcher().knnMatch(d1, d2, k=2) if m.distance < 0.75 * n.distance]
    src = np.float32([k1[m.queryIdx].pt for m in good])
    dst = np.float32([k2[m.trainIdx].pt for m in good])
    matrix, inliers = cv2.estimateAffinePartial2D(src, dst, method=cv2.RANSAC, ransacReprojThreshold=3)
    if matrix is None:
        raise SystemExit('could not align the edit to the portrait')
    return matrix, int(inliers.sum()), len(good)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('--base', required=True)
    parser.add_argument('--edit', required=True)
    parser.add_argument('--out', required=True)
    parser.add_argument('--below', type=float, default=0.38)
    parser.add_argument('--cover', action='append', default=[])
    parser.add_argument('--preview')
    args = parser.parse_args()

    base = np.asarray(Image.open(args.base).convert('RGBA')).astype(np.float32)
    h, w = base.shape[:2]
    green = base[..., :3] * base[..., 3:4] / 255 + np.array([0, 255, 0]) * (1 - base[..., 3:4] / 255)
    edit = np.asarray(Image.open(args.edit).convert('RGB'))
    scale = h / edit.shape[0]
    edit = cv2.resize(edit, (round(edit.shape[1] * scale), h), interpolation=cv2.INTER_AREA)
    matrix, inliers, good = align(edit, green.astype(np.uint8))
    warped = cv2.warpAffine(edit, matrix, (w, h), flags=cv2.INTER_LANCZOS4, borderValue=(0, 255, 0))
    keyed = np.asarray(key_green(Image.fromarray(warped))).astype(np.float32)

    # Where the edit differs from the portrait (colour on the shared ground, or one has her and the other not).
    a_e, a_b = keyed[..., 3] / 255, base[..., 3] / 255
    on_e = keyed[..., :3] * a_e[..., None] + 128 * (1 - a_e[..., None])
    on_b = base[..., :3] * a_b[..., None] + 128 * (1 - a_b[..., None])
    diff = (np.abs(on_e - on_b).max(-1) > 40).astype(np.uint8)
    diff = cv2.morphologyEx(diff, cv2.MORPH_OPEN, np.ones((5, 5), np.uint8))
    n, labels, stats, _ = cv2.connectedComponentsWithStats(diff)
    keep = np.zeros_like(diff)
    for i in range(1, n):
        if stats[i, cv2.CC_STAT_AREA] > w * h * 0.0005:
            keep[labels == i] = 1
    for box in args.cover:
        x0, y0, x1, y1 = [int(v) for v in box.split(',')]
        keep[y0:y1, x0:x1] = 1
    grown = cv2.dilate(keep, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (41, 41))).astype(np.float32)
    mask = cv2.GaussianBlur(grown, (0, 0), 8)
    ys = np.arange(h, dtype=np.float32)[:, None] / h
    mask *= np.clip((ys - args.below) / 0.03, 0, 1)
    # Inside the change the edit replaces her outright (also where her old hands were and the edit shows the dress).
    alpha = np.clip(mask * np.maximum(a_e, (mask > 0.5) * a_b), 0, 1)
    patch = np.dstack([keyed[..., :3], alpha * 255]).astype(np.uint8)
    Image.fromarray(patch, 'RGBA').save(args.out)
    print(f'wrote {args.out}: {inliers}/{good} matches, scale {np.hypot(*matrix[0, :2]):.3f}, patch covers {(alpha > 0.5).mean():.1%}')

    if args.preview:
        ground = np.array([48, 46, 58], np.float32)
        lay = lambda rgb, a: rgb * a[..., None] + ground * (1 - a[..., None])
        under = lay(base[..., :3], a_b)
        together = patch[..., :3] * alpha[..., None] + under * (1 - alpha[..., None])
        tiles = [under, lay(patch[..., :3].astype(np.float32), alpha), together]
        Image.fromarray(np.concatenate(tiles, 1).clip(0, 255).astype(np.uint8)).resize((w * 3 // 2, h // 2)).save(args.preview)


if __name__ == '__main__':
    main()
