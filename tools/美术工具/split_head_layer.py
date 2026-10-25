# -*- coding: utf-8 -*-
"""Cuts a standing portrait into a head layer and a body layer, so the head can move as one piece (UIPortraitPuppet,
"<visualID>_head" over "<visualID>_body") instead of the mesh stretching it.

  python tools/美术工具/split_head_layer.py <portrait.png> <out_dir> --id <visualID> --head cx,cy,r --neck x,y
      --jaw y [--preview <out.png>]

Coordinates are pixels of the full-size portrait (the same --head and --neck as build_puppet_rig.py). The head layer is
the round of the head with the loose strands around it (out to 1.25 r) and the face, cut off softly under the chin at
the neck. The body layer is the portrait with that area taken out: above the jaw nothing is behind her head, so it is
transparent and a moving head shows the room; from the jaw down to the neck the collar and the hair behind are filled
in from around the hole. Both are on the portrait's canvas. The preview shows the head moved left, right, tilted and
dipped over the body layer, on a plain ground.
"""
import argparse
import os

import cv2
import numpy as np
from PIL import Image


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def split(img, head, neck, jaw):
    h, w = img.shape[:2]
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
    cx, cy, r = head
    d = np.sqrt((xs - cx) ** 2 + (ys - cy) ** 2) / r
    mask = (1 - smooth(1.05, 1.25, d)) * (1 - smooth(neck[1] - 20, neck[1] + 5, ys))
    mask = cv2.GaussianBlur(mask, (0, 0), 2)
    hole = (mask > 0.02).astype(np.uint8) * 255
    rgb = np.ascontiguousarray(img[..., 2::-1].astype(np.uint8))
    alpha = img[..., 3].astype(np.uint8)
    body_rgb = cv2.inpaint(rgb, hole, 9, cv2.INPAINT_NS)[..., ::-1]
    body_a = cv2.inpaint(alpha, hole, 9, cv2.INPAINT_NS).astype(np.float32)
    inside = mask > 0.02
    # Half-filled alpha shows as dots around her head: inside the fill it is all or nothing, and nothing above the jaw.
    body_a = np.where(inside, (body_a > 128) * 255.0 * smooth(jaw - 25, jaw + 25, ys), body_a)
    body = np.dstack([body_rgb, body_a]).astype(np.float32)
    head_layer = img.astype(np.float32).copy()
    head_layer[..., 3] *= mask
    return head_layer, body


def over(dst, src):
    sa = src[..., 3:4] / 255
    da = dst[..., 3:4] / 255
    oa = sa + da * (1 - sa)
    rgb = (src[..., :3] * sa + dst[..., :3] * da * (1 - sa)) / np.maximum(oa, 1e-6)
    return np.dstack([rgb, oa * 255])


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('portrait')
    parser.add_argument('out_dir')
    parser.add_argument('--id', required=True)
    parser.add_argument('--head', required=True)
    parser.add_argument('--neck', required=True)
    parser.add_argument('--jaw', type=float, required=True)
    parser.add_argument('--preview')
    args = parser.parse_args()
    img = np.array(Image.open(args.portrait).convert('RGBA')).astype(np.float32)
    head = tuple(float(v) for v in args.head.split(','))
    neck = tuple(float(v) for v in args.neck.split(','))
    head_layer, body = split(img, head, neck, args.jaw)
    os.makedirs(args.out_dir, exist_ok=True)
    for suffix, layer in (('_head', head_layer), ('_body', body)):
        path = os.path.join(args.out_dir, args.id + suffix + '.png')
        Image.fromarray(layer.clip(0, 255).astype(np.uint8), 'RGBA').save(path)
        print('wrote', path)

    if args.preview:
        h, w = img.shape[:2]
        cx, cy, r = head
        box = (int(max(0, cx - 1.6 * r)), 0, int(min(w, cx + 1.6 * r)), int(min(h, neck[1] + 1.2 * r)))
        tiles = []
        for theta, dx, dy in ((0, 0, 0), (0, -14, 0), (0, 14, 0), (5, 0, 0), (0, 0, 16)):
            m = cv2.getRotationMatrix2D(neck, -theta, 1.0)
            m[0, 2] += dx
            m[1, 2] += dy
            moved = cv2.warpAffine(head_layer, m, (w, h), flags=cv2.INTER_CUBIC, borderValue=(0, 0, 0, 0))
            ground = np.zeros_like(img)
            ground[..., :3] = (205, 180, 140)
            ground[..., 3] = 255
            comp = over(over(ground, body), moved)[..., :3]
            tiles.append(Image.fromarray(comp.clip(0, 255).astype(np.uint8)).crop(box))
        sheet = Image.new('RGB', (tiles[0].width * len(tiles), tiles[0].height))
        for i, tile in enumerate(tiles):
            sheet.paste(tile, (i * tile.width, 0))
        sheet.save(args.preview)


if __name__ == '__main__':
    main()
