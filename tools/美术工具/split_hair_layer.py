# -*- coding: utf-8 -*-
"""Cuts a standing portrait's long hanging hair into a layer behind her body, so the hair can sway and follow her head
(UIPortraitPuppet: the image shows "<visualID>_hair", the body layer "<visualID>_body" is drawn over it) without
dragging the edge of her dress along. Also writes the sway rig the hair layer bends by.

  python tools/美术工具/split_hair_layer.py <portrait.png> <out_dir> --id <visualID> --rig <out_rig.png>
      [--dress-left x0,y0,x1,y1] [--dress-right x0,y0,x1,y1] [--dress-rows top,bottom] [--hair-top y]
      [--hair-mask <mask.png>] [--preview <out.png>]

Coordinates are pixels of the full-size portrait; the defaults are Zero's standing portrait (doll_proto_0_stand).

- Body: the warm pixels (cape, skin, dress) joined up, plus the dress itself. The dress's shaded edge is as grey as the
  hair, so its outline is traced row by row: from a rough line along each side (--dress-left / --dress-right, two
  points each, between --dress-rows) the scan walks outward to the dark outline and keeps its outer end.
- Hair: everything else below --hair-top in the zone the hanging hair takes (build_portrait_rig.py's hair weight,
  spread out). The hair by her face stays in the body layer and turns with her head.
- --hair-mask: for a portrait the colours cannot split (dark hair on dark clothes, hair over the body), paint the
  hanging hair white on black at the portrait's size in any image editor; it replaces the two steps above. Paint
  only hair that hangs free beside or behind her, nothing in front of her body or by her face.
- Under her body the hair layer carries on: each row repeats the hair at the body's edge, smoothed down the hair's
  length, so hair swinging out from behind her shows hair instead of a gap.
- Rig: R = how far the hair layer sways (soft all over the hair, nothing above her shoulders); G and B are 0. Half
  resolution, like build_portrait_rig.py.

Both layers are on the portrait's canvas. The preview shows the hair moved both ways behind the body.
"""
import argparse
import os
import sys

import cv2
import numpy as np
from PIL import Image

sys.path.insert(0, os.path.dirname(os.path.abspath(__file__)))
from build_portrait_rig import build as build_sway_rig  # noqa: E402
from build_portrait_rig import smooth  # noqa: E402

DARK = 80          # outline luminance
SCAN_IN = 30       # the scan starts this far inside the rough line
SCAN_OUT = 70      # and looks this far for the outline
EXTEND = 45        # how far the hair carries on under her body


def trace_side(lum, line, rows, side):
    (x0, y0, x1, y1) = line
    points = []
    for y in range(int(rows[0]), int(rows[1]), 2):
        x = int(x0 + (x1 - x0) * (y - y0) / (y1 - y0)) - side * SCAN_IN
        for k in range(SCAN_OUT):
            xx = x + side * k
            if lum[y, xx] < DARK:
                n = 0
                while lum[y, xx + side] < DARK and n < 8:
                    xx += side
                    n += 1
                points.append((xx, y))
                break
    return points


def find_hair(img, left, right, rows, hair_top):
    """The hanging hair and her body, from the colours and the traced dress outline."""
    f = img.astype(np.float32)
    r, b, a = f[..., 0], f[..., 2], f[..., 3]
    h, w = a.shape
    ys = np.arange(h)[:, None].repeat(w, 1)
    opaque = a > 40
    warm = (((r - b) > 12) & opaque).astype(np.uint8)
    body = cv2.morphologyEx(warm, cv2.MORPH_CLOSE, np.ones((5, 5), np.uint8))
    body = cv2.morphologyEx(body, cv2.MORPH_OPEN, np.ones((5, 5), np.uint8))
    count, labels, stats, _ = cv2.connectedComponentsWithStats(body)
    big = np.zeros_like(body)
    for i in range(1, count):
        if stats[i, cv2.CC_STAT_AREA] > 5000:
            big[labels == i] = 1
    # Her outline belongs to the body, but light grey strands touching it are hair, or a tip stays on the dress.
    body = cv2.dilate(big, cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (7, 7))) > 0
    lum_rgb = f[..., :3].mean(-1)
    ring = body & ~(cv2.dilate(big, np.ones((3, 3), np.uint8)) > 0)
    body &= ~(ring & (warm == 0) & (lum_rgb > 105))

    lum = np.where(a > 128, lum_rgb, 255)
    lefts = trace_side(lum, left, rows, -1)
    rights = trace_side(lum, right, rows, 1)
    dress = np.zeros((h, w), np.uint8)
    hem = int(rows[1]) + 10
    poly = lefts + [(lefts[-1][0] + 20, hem), (rights[-1][0] - 20, hem)] + rights[::-1]
    cv2.fillPoly(dress, [np.array(poly, np.int32)], 1)
    body |= cv2.dilate(dress, np.ones((3, 3), np.uint8)) > 0
    # Beside the traced edges nothing is her body: a strand crossing the outline would otherwise stay on the dress.
    for x, y in lefts:
        body[y:y + 2, :max(0, x - 1)] = False
    for x, y in rights:
        body[y:y + 2, x + 2:] = False

    sway_hair, _ = build_sway_rig(img, strands=31, skirt_from=0.6)
    zone = cv2.dilate((sway_hair > 0.02).astype(np.uint8), cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (41, 41))) > 0
    zone &= ys > hair_top
    hair = (a > 0) & ~body & zone
    return hair, body


def split(img, hair, body, hair_top):
    """The hair layer (carried on under her body), the body layer and the hair layer's sway weight."""
    f = img.astype(np.float32)
    a = f[..., 3]
    h, w = a.shape
    ys = np.arange(h)[:, None].repeat(w, 1)
    under = body & (cv2.dilate(hair.astype(np.uint8), cv2.getStructuringElement(cv2.MORPH_ELLIPSE, (2 * EXTEND - 9,) * 2)) > 0)
    hair_layer = f.copy()
    hair_layer[..., 3] = np.where(hair, a, 0)
    filled = hair.copy()
    for _ in range(EXTEND):
        for d in (1, -1):
            take = under & ~filled & np.roll(filled, d, axis=1)
            if take.any():
                hair_layer[take] = np.roll(hair_layer, d, axis=1)[take]
                filled |= take
    carried = under & ~hair
    blurred = cv2.GaussianBlur(hair_layer, (0, 0), sigmaX=0.6, sigmaY=7)
    hair_layer[carried] = blurred[carried]
    hair_layer[~(hair | carried)] = 0

    body_layer = f.copy()
    body_layer[..., 3] = np.where(hair, 0, a)

    weight = cv2.GaussianBlur((hair | under).astype(np.float32), (0, 0), 14)
    # Nothing at --hair-top, whole a little below her shoulders.
    weight = np.clip(weight * 1.6, 0, 1) * smooth(hair_top / h, hair_top / h + 0.18, ys / h)
    return hair_layer, body_layer, weight


def over(dst, src):
    sa = src[..., 3:4] / 255
    da = dst[..., 3:4] / 255
    oa = sa + da * (1 - sa)
    rgb = (src[..., :3] * sa + dst[..., :3] * da * (1 - sa)) / np.maximum(oa, 1e-6)
    return np.dstack([rgb, oa * 255])


def numbers(text, count):
    values = [float(v) for v in text.split(',')]
    if len(values) != count:
        raise SystemExit(f'expected {count} numbers, got {text}')
    return values


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('portrait')
    parser.add_argument('out_dir')
    parser.add_argument('--id', required=True)
    parser.add_argument('--rig', required=True, help='the sway rig to write for the hair layer')
    parser.add_argument('--dress-left', default='383,680,318,985', help='two points on the left edge of the dress')
    parser.add_argument('--dress-right', default='655,680,733,985', help='two points on the right edge of the dress')
    parser.add_argument('--dress-rows', default='600,1030', help='the rows to trace the dress edges over')
    parser.add_argument('--hair-top', type=float, default=330, help='hair above this row turns with the head')
    parser.add_argument('--hair-mask', help='painted mask of the hanging hair (white), instead of finding it')
    parser.add_argument('--preview')
    args = parser.parse_args()
    img = np.array(Image.open(args.portrait).convert('RGBA'))
    if args.hair_mask:
        mask = np.array(Image.open(args.hair_mask).convert('L'))
        if mask.shape != img.shape[:2]:
            raise SystemExit(f'the mask is {mask.shape[1]}x{mask.shape[0]}, the portrait {img.shape[1]}x{img.shape[0]}')
        hair_mask = (mask > 127) & (img[..., 3] > 0)
        body_mask = (img[..., 3] > 0) & ~hair_mask
    else:
        hair_mask, body_mask = find_hair(img, numbers(args.dress_left, 4), numbers(args.dress_right, 4),
                                         numbers(args.dress_rows, 2), args.hair_top)
    hair, body, weight = split(img, hair_mask, body_mask, args.hair_top)
    os.makedirs(args.out_dir, exist_ok=True)
    for suffix, layer in (('_hair', hair), ('_body', body)):
        path = os.path.join(args.out_dir, args.id + suffix + '.png')
        Image.fromarray(layer.clip(0, 255).astype(np.uint8), 'RGBA').save(path)
        print('wrote', path)

    h, w = weight.shape
    zero = np.zeros((h, w), np.uint8)
    rig = np.dstack([(weight * 255 + 0.5).astype(np.uint8), zero, zero])
    Image.fromarray(rig).resize((w // 2, h // 2), Image.BILINEAR).save(args.rig)
    print('wrote', args.rig)

    if args.preview:
        ground = np.zeros_like(body)
        ground[..., :3] = (46, 40, 38)
        ground[..., 3] = 255
        tiles = []
        for dx in (0, -16, 16):
            m = np.float32([[1, 0, dx], [0, 1, 0]])
            moved = cv2.warpAffine(hair * (weight[..., None] > 0.05), m, (w, h), borderValue=(0, 0, 0, 0))
            rest = hair * (weight[..., None] <= 0.05)
            comp = over(over(over(ground, rest), moved), body)[..., :3]
            tiles.append(comp[400:1100, 150:880])
        Image.fromarray(np.concatenate(tiles, 1).clip(0, 255).astype(np.uint8)).save(args.preview)


if __name__ == '__main__':
    main()
