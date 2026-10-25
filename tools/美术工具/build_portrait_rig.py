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


def build(portrait, strands=0, skirt_from=0.52):
    f = portrait.astype(np.float32)
    r, b, a = f[..., 0], f[..., 2], f[..., 3]
    h, w = a.shape
    ys = np.arange(h)[:, None] / h
    xs = np.arange(w)[None, :] / w
    opaque = a > 40
    warm = (((r - b) > 12) & opaque).astype(np.uint8)
    # Closing joins the body across small gaps; with thin strands a wide close also swallows the hair between her arms
    # and the cape, which then stands still in blocks while the hair around it sways.
    close = 5 if strands else 15
    body = cv2.morphologyEx(warm, cv2.MORPH_CLOSE, np.ones((close, close), np.uint8))
    body = cv2.morphologyEx(body, cv2.MORPH_OPEN, np.ones((5, 5), np.uint8))
    count, labels, stats, _ = cv2.connectedComponentsWithStats(body)
    big = np.zeros_like(body)
    for i in range(1, count):
        if stats[i, cv2.CC_STAT_AREA] > 5000:
            big[labels == i] = 1
    body = cv2.dilate(big, np.ones((3, 3), np.uint8)) > 0

    loose = (opaque & ~body).astype(np.uint8)
    if strands:
        # Hair drawn as thin separate strands with gaps between them: weight the whole zone they hang in (the figure
        # spread by `strands` px, minus the body), not each strand, or the blur thins the outer strands to nothing
        # and the gaps tear. The gaps are transparent, so their weight costs nothing.
        zone = cv2.dilate(opaque.astype(np.uint8), cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (strands, strands)))
        loose = zone & ~cv2.dilate(body.astype(np.uint8), np.ones((5, 5), np.uint8)).astype(bool)
    hair = np.clip(cv2.GaussianBlur(loose.astype(np.float32), (0, 0), 10 if strands else 6) * 1.4, 0, 1)
    if strands:
        # The zone also hugs her legs; the hair ends above the hem.
        hair *= 1 - smooth(0.64, 0.70, ys)
        # The blur reaches back over her arms beside the hair; arms that bend with the hair come out one wider than
        # the other. Only what is not her body sways, and the hair within about one mesh cell (UIPortraitSway puts a
        # vertex every 1/64 of the width) of her body stays nearly still, or the cell drags the arm's edge along.
        held = cv2.dilate(body.astype(np.uint8), cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (w // 64 + 1,) * 2))
        hair *= 1 - np.clip(cv2.GaussianBlur(held.astype(np.float32), (0, 0), w / 256) * 1.3, 0, 1)
    column = smooth(0.08, 0.13, np.abs(xs - 0.49))
    hair_w = hair * smooth(0.17, 0.42, ys) * np.maximum(column, 1 - smooth(0.25, 0.32, ys))
    hair_w = keep_main_masses(hair_w)
    skirt = (body & (ys > 0.45) & (ys < 0.70) & (r > 150)).astype(np.float32)
    # Blurred for a soft bend, but held inside the dress (a little past it) so the knees below the hem stay put.
    inside = cv2.GaussianBlur(cv2.dilate(skirt, np.ones((9, 9), np.uint8)), (0, 0), 3)
    skirt_w = np.clip(cv2.GaussianBlur(skirt, (0, 0), 10), 0, 1) * smooth(skirt_from, 0.67, ys) * np.clip(inside * 1.5, 0, 1)
    return hair_w, skirt_w


def keep_main_masses(weight):
    """Drops small islands of weight (wounds, bandages, a hand in front of the cape) that are not the hanging hair."""
    labels_count, labels, stats, _ = cv2.connectedComponentsWithStats((weight > 0.2).astype(np.uint8))
    if labels_count <= 1:
        return weight
    largest = stats[1:, cv2.CC_STAT_AREA].max()
    keep = np.zeros(weight.shape, np.float32)
    for i in range(1, labels_count):
        if stats[i, cv2.CC_STAT_AREA] >= largest * 0.15:
            keep[labels == i] = 1
    keep = cv2.GaussianBlur(cv2.dilate(keep, np.ones((15, 15), np.uint8)), (0, 0), 6)
    return weight * np.clip(keep, 0, 1)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("portrait")
    parser.add_argument("out")
    parser.add_argument("--preview")
    parser.add_argument("--strands", type=int, default=0, help="close gaps of this many px between thin hair strands")
    parser.add_argument("--skirt-from", type=float, default=0.52,
                        help="share of the height where the skirt starts to sway; below her hands, or they stretch")
    args = parser.parse_args()
    portrait = np.array(Image.open(args.portrait).convert("RGBA"))
    hair_w, skirt_w = build(portrait, args.strands, args.skirt_from)
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
