"""Turns a green-screen idle video into a transparent loop that keeps the portrait's own lines: the motion is tracked
in the clip (optical flow from each frame back to the first) and the crisp portrait is moved with it, instead of keying
the clip's own pixels. H3 redraws thin lineart soft and makes thin hair strands shimmer; this keeps only its motion.

  python tools/美术工具/make_flow_loop.py <in.mp4> <portrait.png> <out.webm> [--fade 12] [--smooth 12] [--max-move 16] [--steady 2.5] [--preview out.png]

- The clip must start on the portrait itself (image-to-video with the portrait on green as the first frame).
- Flow: DIS on the figure (the green ground flattened), each move capped at --max-move px (longer ones are tracking
  errors around swinging strands) and blurred by --smooth px so the motion stays one smooth bend; less smoothing
  follows small strands but tears where the clip has no texture.
- Steady: the tracker errs a little differently on every frame, which shows as jitter; the flows are blurred over
  time by --steady frames (around the loop, so the seam stays smooth). The printed jitter is the mean frame-to-frame
  change in speed in px over the whole frame, before and after (the motion's own acceleration is part of it);
  compare clips and settings with it, then look at the result.
- Loop: the flows of the last `fade` frames blend into the first ones (the motion loops, the lines never ghost).
- Output: VP8 WebM with alpha at the clip's size (the portrait is scaled down to it with Lanczos first), no audio.
Needs numpy, OpenCV, Pillow and an ffmpeg with libvpx (imageio-ffmpeg's bundled one works).
"""
import argparse
import os
import shutil
import subprocess
import tempfile

import cv2
import numpy as np
from PIL import Image

from make_alpha_loop import find_ffmpeg, loop


def figure_grey(frame):
    grey = cv2.cvtColor(frame, cv2.COLOR_BGR2GRAY)
    green = (frame[..., 1].astype(int) - np.maximum(frame[..., 0], frame[..., 2]).astype(int)) > 80
    grey[green] = 0
    return grey


def smooth_flow(flow, sigma, max_move):
    # Where the clip swings a strand far or the tracker loses texture, the flow jumps; cap the length first, then blur
    # so the cap does not leave a seam.
    length = np.linalg.norm(flow, axis=2, keepdims=True)
    flow = flow * np.minimum(1.0, max_move / np.maximum(length, 1e-6))
    return cv2.GaussianBlur(flow, (0, 0), sigma)


def steady(stack, sigma):
    # Gaussian over time, wrapping around: the clip is a loop, so frame 0 follows the last frame.
    radius = int(3 * sigma)
    weights = np.exp(-0.5 * (np.arange(-radius, radius + 1) / sigma) ** 2)
    weights /= weights.sum()
    n = len(stack)
    out = np.zeros_like(stack)
    for k, wgt in zip(range(-radius, radius + 1), weights):
        out += wgt * stack[(np.arange(n) + k) % n]
    return out


def jitter(stack):
    accel = stack - 0.5 * (np.roll(stack, 1, axis=0) + np.roll(stack, -1, axis=0))
    return float(np.abs(accel).mean())


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('src')
    parser.add_argument('portrait')
    parser.add_argument('dst')
    parser.add_argument('--fade', type=int, default=12)
    parser.add_argument('--smooth', type=float, default=12.0)
    parser.add_argument('--max-move', type=float, default=16.0, help='largest move in px; more is a tracking error')
    parser.add_argument('--steady', type=float, default=2.5, help='blur the motion over this many frames')
    parser.add_argument('--bitrate', default='4M')
    parser.add_argument('--preview')
    args = parser.parse_args()

    cap = cv2.VideoCapture(args.src)
    fps = cap.get(cv2.CAP_PROP_FPS) or 24.0
    frames = []
    ok, frame = cap.read()
    while ok:
        frames.append(frame)
        ok, frame = cap.read()
    h, w = frames[0].shape[:2]
    portrait = np.array(Image.open(args.portrait).convert('RGBA').resize((w, h), Image.LANCZOS)).astype(np.float32)

    dis = cv2.DISOpticalFlow_create(cv2.DISOPTICAL_FLOW_PRESET_MEDIUM)
    first = figure_grey(frames[0])
    # Backward flow: for each pixel of frame i, where it was in the first frame (so the portrait can be sampled there).
    # Kept at a quarter size: the flow is smooth, and 112 full-size flows would take about 1 GB.
    small = (w // 4, h // 4)
    flows = [cv2.resize(smooth_flow(dis.calc(figure_grey(f), first, None), args.smooth, args.max_move), small,
                        interpolation=cv2.INTER_AREA) for f in frames]
    flows = loop(flows, args.fade)
    stack = np.stack(flows)
    raw_jitter = jitter(stack)
    if args.steady > 0:
        stack = steady(stack, args.steady)
    flows = [cv2.resize(f, (w, h), interpolation=cv2.INTER_CUBIC) for f in stack]
    ys, xs = np.mgrid[0:h, 0:w].astype(np.float32)

    tmp = tempfile.mkdtemp(prefix='flow_loop_')
    try:
        shown = []
        for i, flow in enumerate(flows):
            moved = cv2.remap(portrait, xs + flow[..., 0], ys + flow[..., 1], cv2.INTER_CUBIC, borderMode=cv2.BORDER_CONSTANT)
            rgba = moved.clip(0, 255).astype(np.uint8)
            Image.fromarray(rgba, 'RGBA').save(os.path.join(tmp, f'{i:05d}.png'))
            if args.preview and i in (0, len(flows) // 3, 2 * len(flows) // 3, len(flows) - 1):
                shown.append(rgba)
        cmd = [find_ffmpeg(), '-y', '-loglevel', 'error', '-framerate', f'{fps:g}', '-i', os.path.join(tmp, '%05d.png'),
               '-c:v', 'libvpx', '-pix_fmt', 'yuva420p', '-auto-alt-ref', '0', '-b:v', args.bitrate, '-an', args.dst]
        subprocess.run(cmd, check=True)
    finally:
        shutil.rmtree(tmp, ignore_errors=True)

    largest = max(float(np.abs(f).max()) for f in flows)
    print(f'jitter {raw_jitter:.3f} -> {jitter(np.stack([cv2.resize(f, small) for f in flows])):.3f}')
    print(f'{len(flows)} frames at {fps:g} fps, {w}x{h}, largest move {largest:.1f} px -> {args.dst} '
          f'({os.path.getsize(args.dst) / 1e6:.1f} MB)')
    if args.preview:
        sheet = Image.new('RGBA', (w * len(shown), h), (40, 40, 48, 255))
        for i, rgba in enumerate(shown):
            sheet.alpha_composite(Image.fromarray(rgba, 'RGBA'), (i * w, 0))
        sheet.convert('RGB').save(args.preview)


if __name__ == '__main__':
    main()
