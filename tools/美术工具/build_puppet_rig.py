# -*- coding: utf-8 -*-
"""Head weights for a portrait puppet (UIPortraitPuppet): R = head, G = face, at half resolution.

  python tools/美术工具/build_puppet_rig.py <portrait.png> <out_puppet.png> --head cx,cy,r --neck x,y --face x,y,sx,sy
      [--preview <out.png>]

Coordinates are pixels of the full-size portrait. The head weight is 1 inside a circle around the head and fades out
past its edge and under the chin (the neck), so the head can tilt, dip and turn on its neck while the shoulders stay
put: pixels stretch across the fade instead of tearing. The face weight peaks on the face, so in a turn the face
slides further than the hair around it. Defaults are Zero's standing portrait (doll_proto_0_stand); the preview shows
the weights and the head tilted both ways, dipped and turned.
"""
import argparse

import cv2
import numpy as np
from PIL import Image


def smooth(e0, e1, x):
    t = np.clip((x - e0) / (e1 - e0), 0, 1)
    return t * t * (3 - 2 * t)


def build(shape, head, neck, face):
    h, w = shape
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
    cx, cy, r = head
    d = np.sqrt((xs - cx) ** 2 + (ys - cy) ** 2) / r
    head_w = 1 - smooth(0.85, 1.35, d)
    # Under the chin the head lets go of the neck and shoulders.
    head_w *= 1 - smooth(neck[1] - 65, neck[1] + 15, ys) * (1 - smooth(60, 150, np.abs(xs - cx)))
    head_w = cv2.GaussianBlur(head_w, (0, 0), 8)
    fx, fy, sx, sy = face
    face_w = np.exp(-(((xs - fx) / sx) ** 2 + ((ys - fy) / sy) ** 2)) * head_w
    return head_w, face_w


def deform(img, head_w, face_w, neck, theta, dip, look):
    h, w = head_w.shape
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)
    t = np.radians(theta) * head_w
    dx, dy = xs - neck[0], ys - neck[1]
    nx = neck[0] + np.cos(t) * dx - np.sin(t) * dy + look * (0.3 * head_w + 0.7 * face_w)
    ny = neck[1] + np.sin(t) * dx + np.cos(t) * dy + dip * head_w
    # Small moves: sampling at p - (f(p) - p) is close enough to the inverse for a preview.
    return cv2.remap(img, (2 * xs - nx).astype(np.float32), (2 * ys - ny).astype(np.float32), cv2.INTER_LINEAR)


def preview(img, head_w, face_w, neck, head, path):
    cx, cy, r = head
    box = (int(max(0, cx - 2 * r)), 0, int(min(img.shape[1], cx + 2 * r)), int(min(img.shape[0], neck[1] + 2.2 * r)))
    tiles = []
    weights = np.dstack([head_w, face_w, np.zeros_like(head_w)]) * 255
    alpha = img[..., 3:4] / 255.0
    tiles.append((weights * 0.7 + img[..., :3] * 0.3 * alpha).astype(np.uint8))
    for theta, dip, look in [(-8, 0, 0), (8, 0, 0), (0, 14, 0), (0, 0, 16)]:
        out = deform(img, head_w, face_w, neck, theta, dip, look).astype(np.float32)
        a = out[..., 3:4] / 255.0
        tiles.append((out[..., :3] * a + np.array([90, 90, 100]) * (1 - a)).astype(np.uint8))
    crops = [t[box[1]:box[3], box[0]:box[2]] for t in tiles]
    Image.fromarray(np.concatenate(crops, 1)).save(path)


def numbers(text, count):
    values = [float(v) for v in text.split(',')]
    if len(values) != count:
        raise SystemExit(f'expected {count} numbers, got {text}')
    return values


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('portrait')
    parser.add_argument('out')
    parser.add_argument('--head', default='530,165,150', help='head circle centre x,y and radius')
    parser.add_argument('--neck', default='525,320', help='the point the head turns on')
    parser.add_argument('--face', default='522,250,80,70', help='face centre x,y and spread x,y')
    parser.add_argument('--preview')
    args = parser.parse_args()

    img = np.asarray(Image.open(args.portrait).convert('RGBA')).astype(np.float32)
    head, neck, face = numbers(args.head, 3), numbers(args.neck, 2), numbers(args.face, 4)
    head_w, face_w = build(img.shape[:2], head, neck, face)
    h, w = head_w.shape
    rig = np.dstack([head_w, face_w, np.zeros_like(head_w)])
    small = cv2.resize(rig, (w // 2, h // 2), interpolation=cv2.INTER_AREA)
    Image.fromarray(np.clip(small * 255 + 0.5, 0, 255).astype(np.uint8), 'RGB').save(args.out)
    print('wrote', args.out, f'(neck pivot u={neck[0] / w:.3f} v={1 - neck[1] / h:.3f} from the bottom-left)')
    if args.preview:
        preview(img, head_w, face_w, neck, head, args.preview)


if __name__ == '__main__':
    main()
