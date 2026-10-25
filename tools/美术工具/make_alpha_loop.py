"""Turns a green-screen idle video (H3 image-to-video on a flat #00FF00 background) into a seamless, transparent loop.

  python tools/美术工具/make_alpha_loop.py <in.mp4> <out.webm> [--fade 12] [--grey-edges] [--preview out_sheet.png]

- Loop: the first `fade` frames are cut off and blended into the tail, so the last frame flows into the first.
  Works when the clip ends near its starting pose (prompt it to return to the first-frame pose).
- Key: alpha from how much greener than red/blue a pixel is, then green spill clamped to max(red, blue).
  --grey-edges: thin see-through parts (fine hair strands) lose their colour in proportion to how see-through they
  are, and no pixel keeps more green than its red or blue. The video's halved colour resolution smears green into a
  2-3 px strand, and unmixing leaves it olive.
- Output: VP8 WebM with alpha (yuva420p), no audio. Import it in Unity with "Keep Alpha" on.
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


def find_ffmpeg():
    exe = shutil.which('ffmpeg')
    if exe:
        return exe
    import imageio_ffmpeg
    return imageio_ffmpeg.get_ffmpeg_exe()


def read_frames(path):
    cap = cv2.VideoCapture(path)
    fps = cap.get(cv2.CAP_PROP_FPS) or 24.0
    frames = []
    ok, frame = cap.read()
    while ok:
        frames.append(cv2.cvtColor(frame, cv2.COLOR_BGR2RGB).astype(np.float32))
        ok, frame = cap.read()
    return frames, fps


def loop(frames, fade):
    n = len(frames)
    if fade <= 0 or fade * 2 >= n:
        return frames
    out = frames[fade:]
    for j in range(fade):
        w = (j + 1) / (fade + 1)
        tail = n - fade + j - fade
        out[tail] = frames[n - fade + j] * (1 - w) + frames[j] * w
    return out


def background_colour(rgb):
    # The median of the four corner patches: H3 drifts the green a little, so it is measured per clip.
    h, w = rgb.shape[:2]
    s = max(4, min(h, w) // 20)
    patches = [rgb[:s, :s], rgb[:s, -s:], rgb[-s:, :s], rgb[-s:, -s:]]
    return np.median(np.concatenate([p.reshape(-1, 3) for p in patches]), axis=0)


def key(rgb, bg, noise=12.0, grey_edges=False):
    # Unmix against the known background: a pixel is alpha * foreground + (1 - alpha) * bg. Alpha comes from how
    # much of the background's green excess the pixel still has, and the foreground colour is solved back out, so
    # semi-transparent hair keeps its own colour instead of turning into green-white blotches.
    r, g, b = rgb[..., 0], rgb[..., 1], rgb[..., 2]
    spill = g - np.maximum(r, b)
    bg_spill = max(1.0, bg[1] - max(bg[0], bg[2]))
    alpha = 1.0 - np.clip((spill - noise) / (bg_spill - 2 * noise), 0, 1)
    a = np.maximum(alpha, 0.05)[..., None]
    fg = (rgb - (1.0 - a) * bg) / a
    fg[..., 1] = np.minimum(fg[..., 1], np.maximum(fg[..., 0], fg[..., 2]) + (0.0 if grey_edges else 6.0))
    if grey_edges:
        keep = a ** 8
        fg = fg * keep + fg.mean(axis=2, keepdims=True) * (1.0 - keep)
    return np.dstack([fg, alpha[..., None] * 255.0]).clip(0, 255).astype(np.uint8)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument('src')
    parser.add_argument('dst')
    parser.add_argument('--fade', type=int, default=12)
    parser.add_argument('--bitrate', default='3M')
    parser.add_argument('--preview')
    parser.add_argument('--grey-edges', action='store_true', help='take the colour out of see-through strands')
    args = parser.parse_args()

    frames, fps = read_frames(args.src)
    frames = loop(frames, args.fade)
    tmp = tempfile.mkdtemp(prefix='alpha_loop_')
    try:
        keyed = []
        bg = background_colour(frames[0])
        for i, frame in enumerate(frames):
            rgba = key(frame, bg, grey_edges=args.grey_edges)
            Image.fromarray(rgba, 'RGBA').save(os.path.join(tmp, f'{i:05d}.png'))
            if args.preview and i in (0, len(frames) // 3, 2 * len(frames) // 3, len(frames) - 1):
                keyed.append(rgba)
        cmd = [find_ffmpeg(), '-y', '-loglevel', 'error', '-framerate', f'{fps:g}', '-i', os.path.join(tmp, '%05d.png'),
               '-c:v', 'libvpx', '-pix_fmt', 'yuva420p', '-auto-alt-ref', '0', '-b:v', args.bitrate, '-an', args.dst]
        subprocess.run(cmd, check=True)
    finally:
        shutil.rmtree(tmp, ignore_errors=True)

    if args.preview and keyed:
        h, w = keyed[0].shape[:2]
        sheet = Image.new('RGBA', (w * len(keyed), h), (36, 30, 28, 255))
        for i, rgba in enumerate(keyed):
            sheet.alpha_composite(Image.fromarray(rgba, 'RGBA'), (i * w, 0))
        sheet.convert('RGB').resize((w * len(keyed) // 2, h // 2)).save(args.preview)
    print(f'{len(frames)} frames at {fps:g} fps -> {args.dst} ({os.path.getsize(args.dst) / 1e6:.1f} MB)')


if __name__ == '__main__':
    main()
