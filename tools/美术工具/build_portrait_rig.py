# -*- coding: utf-8 -*-
"""Weight map for a portrait's sway (UIPortraitSway): R = hair, G = skirt hem, at half resolution.

  python tools/美术工具/build_portrait_rig.py <portrait.png> <out_rig.png> [--preview <out.gif>]

Built for Zero's standing portrait (doll_proto_0_stand): long cool-grey hair hanging on both sides of a warm body
(cape, dress, skin). Hair = opaque pixels outside the warm body, fading in below the head and out in a central column
(hands, legs); skirt = the dress band above the hem. Both are blurred so the mesh bends instead of tearing. Tuned by
eye on the preview GIF; another portrait needs its own numbers.
"""
import argparse

import cv2
import numpy as np
from PIL import Image


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def build(portrait):
    f = portrait.astype(np.float32)
    r, b, a = f[..., 0], f[..., 2], f[..., 3]
    h, w = a.shape
    ys = np.arange(h)[:, None] / h
    xs = np.arange(w)[None, :] / w
    opaque = a > 40
    warm = (((r - b) > 12) & opaque).astype(np.uint8)
    body = cv2.morphologyEx(warm, cv2.MORPH_CLOSE, np.ones((15, 15), np.uint8))
    body = cv2.morphologyEx(body, cv2.MORPH_OPEN, np.ones((5, 5), np.uint8))
    count, labels, stats, _ = cv2.connectedComponentsWithStats(body)
    big = np.zeros_like(body)
    for i in range(1, count):
        if stats[i, cv2.CC_STAT_AREA] > 5000:
            big[labels == i] = 1
    body = cv2.dilate(big, np.ones((3, 3), np.uint8)) > 0

    hair = np.clip(cv2.GaussianBlur((opaque & ~body).astype(np.float32), (0, 0), 6) * 1.4, 0, 1)
    column = smooth(0.08, 0.13, np.abs(xs - 0.49))
    hair_w = hair * smooth(0.17, 0.42, ys) * np.maximum(column, 1 - smooth(0.25, 0.32, ys))
    skirt = (body & (ys > 0.45) & (ys < 0.70) & (r > 150)).astype(np.float32)
    skirt_w = np.clip(cv2.GaussianBlur(skirt, (0, 0), 10), 0, 1) * smooth(0.52, 0.67, ys)
    return hair_w, skirt_w


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("portrait")
    parser.add_argument("out")
    parser.add_argument("--preview")
    args = parser.parse_args()
    portrait = np.array(Image.open(args.portrait).convert("RGBA"))
    hair_w, skirt_w = build(portrait)
    h, w = hair_w.shape
    rig = np.dstack([(hair_w * 255).astype(np.uint8), (skirt_w * 255).astype(np.uint8), np.zeros((h, w), np.uint8)])
    Image.fromarray(rig).resize((w // 2, h // 2), Image.BILINEAR).save(args.out)
    print(f"{args.out}: {w // 2}x{h // 2}, hair max {hair_w.max():.2f}, skirt max {skirt_w.max():.2f}")
    if args.preview:
        gx, gy = np.meshgrid(np.arange(w, dtype=np.float32), np.arange(h, dtype=np.float32))
        ys = np.arange(h)[:, None] / h
        frames = []
        for k in range(16):
            phase = k / 16 * 2 * np.pi
            # Same bend as UIPortraitSway (HairAmount 0.024, SkirtAmount 0.007 of the width).
            dx = (w / 1024) * (24 * hair_w * np.sin(phase - 3.0 * ys) + 7 * skirt_w * np.sin(1.3 * phase + 1.0))
            out = cv2.remap(portrait, gx - dx.astype(np.float32), gy, cv2.INTER_LINEAR, borderMode=cv2.BORDER_CONSTANT)
            alpha = out[..., 3:4] / 255.0
            comp = (out[..., :3] * alpha + 50 * (1 - alpha)).astype(np.uint8)
            frames.append(Image.fromarray(comp).resize((w // 3, h // 3)))
        frames[0].save(args.preview, save_all=True, append_images=frames[1:], duration=120, loop=0)


if __name__ == "__main__":
    main()
