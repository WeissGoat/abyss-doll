"""Draw the M4 linen UI kit (stitched linen panels, leather edging, brass accents).

The sprites overwrite the existing UI frame PNGs in UnityClient/Assets/Art/Approved/UI under the same file names, so
their .meta files (GUIDs) stay and every screen that already uses those VisualIDs picks up the new look. 9-slice
borders are written into the .meta files to match what is drawn here.

usage: python tools/美术工具/generate_linen_ui_kit.py [--preview out.png]
Colours follow 美术文档/04_美术风格基准.md section 1.2 and 美术文档/ui_design/design_tokens.json.
"""
import argparse
import random
import re
from pathlib import Path

from PIL import Image, ImageChops, ImageDraw, ImageFilter

ROOT = Path(__file__).resolve().parents[2]
UI_DIR = ROOT / "UnityClient" / "Assets" / "Art" / "Approved" / "UI"
SS = 2  # supersampling factor for smooth edges

LINEN = (233, 220, 195)
LINEN_DARK = (220, 203, 170)
LINEN_SEAM = (196, 176, 140)
LEATHER = (122, 82, 54)
LEATHER_DARK = (74, 48, 34)
WALNUT = (74, 52, 38)
CREAM = (244, 230, 204)
STITCH_ON_LINEN = (154, 122, 85)
BRASS = (198, 154, 74)
BRASS_DARK = (128, 92, 40)
MADDER = (181, 64, 47)
MADDER_DARK = (126, 42, 32)
HONEY = (232, 160, 74)
SAGE = (143, 168, 118)
TERRACOTTA = (197, 98, 63)
AP_BLUE = (63, 167, 214)


def rgba(c, a=255):
    return (c[0], c[1], c[2], a)


def weave(size, base, strength=10, seed=1):
    """Linen-like cloth: fine crossing threads plus speckle, around a base colour."""
    w, h = size
    rnd = random.Random(seed)
    img = Image.new("RGB", size, base)
    px = img.load()
    rows = [rnd.uniform(-1, 1) for _ in range(h)]
    cols = [rnd.uniform(-1, 1) for _ in range(w)]
    for y in range(h):
        for x in range(w):
            n = (rows[y] * 0.6 + cols[x] * 0.6 + rnd.uniform(-1, 1) * 0.5) * strength
            if (x + y) % 4 == 0:
                n -= strength * 0.25
            px[x, y] = tuple(max(0, min(255, int(c + n))) for c in base)
    return img


def leather(size, base, seed=2):
    img = weave(size, base, strength=7, seed=seed)
    return img.filter(ImageFilter.GaussianBlur(0.6))


def rounded_mask(size, radius, inset=0):
    m = Image.new("L", size, 0)
    ImageDraw.Draw(m).rounded_rectangle((inset, inset, size[0] - 1 - inset, size[1] - 1 - inset), radius=max(0, radius - inset), fill=255)
    return m


def dashed_rect(draw, box, radius, color, width, dash, gap):
    """Dashed stitching along a rounded rectangle."""
    x0, y0, x1, y1 = box
    pts = []
    step = 2
    # walk the perimeter of the rounded rectangle as a polyline
    import math
    r = radius
    segs = [((x0 + r, y0), (x1 - r, y0)), ((x1, y0 + r), (x1, y1 - r)), ((x1 - r, y1), (x0 + r, y1)), ((x0, y1 - r), (x0, y0 + r))]
    corners = [((x1 - r, y0 + r), -90, 0), ((x1 - r, y1 - r), 0, 90), ((x0 + r, y1 - r), 90, 180), ((x0 + r, y0 + r), 180, 270)]
    for i in range(4):
        (ax, ay), (bx, by) = segs[i]
        length = max(abs(bx - ax), abs(by - ay))
        n = max(1, int(length / step))
        for k in range(n):
            t = k / n
            pts.append((ax + (bx - ax) * t, ay + (by - ay) * t))
        (cx, cy), a0, a1 = corners[i]
        n = max(2, int(r * math.pi / 2 / step))
        for k in range(n):
            a = math.radians(a0 + (a1 - a0) * k / n)
            pts.append((cx + r * math.cos(a), cy + r * math.sin(a)))
    travelled, on, run = 0.0, True, []
    for i in range(1, len(pts)):
        (ax, ay), (bx, by) = pts[i - 1], pts[i]
        d = ((bx - ax) ** 2 + (by - ay) ** 2) ** 0.5
        travelled += d
        if on:
            run.append((ax, ay))
        limit = dash if on else gap
        if travelled >= limit:
            if on and run:
                run.append((bx, by))
                draw.line(run, fill=color, width=width)
            run, on, travelled = [], not on, 0.0
    if on and len(run) > 1:
        draw.line(run, fill=color, width=width)


def dashed_line(draw, a, b, color, width, dash, gap):
    (ax, ay), (bx, by) = a, b
    length = ((bx - ax) ** 2 + (by - ay) ** 2) ** 0.5
    t = 0.0
    while t < length:
        e = min(length, t + dash)
        draw.line((ax + (bx - ax) * t / length, ay + (by - ay) * t / length, ax + (bx - ax) * e / length, ay + (by - ay) * e / length), fill=color, width=width)
        t += dash + gap


def rivet(img, cx, cy, r):
    d = ImageDraw.Draw(img)
    d.ellipse((cx - r, cy - r, cx + r, cy + r), fill=rgba(BRASS_DARK))
    d.ellipse((cx - r + 2 * SS, cy - r + 2 * SS, cx + r - 2 * SS, cy + r - 2 * SS), fill=rgba(BRASS))
    hr = max(1, r // 3)
    d.ellipse((cx - r // 2 - hr, cy - r // 2 - hr, cx - r // 2 + hr, cy - r // 2 + hr), fill=(255, 236, 190, 220))


def panel(size, band=20, radius=18, fill=LINEN, edge=LEATHER, edge_dark=LEATHER_DARK, stitch=CREAM, inner_stitch=STITCH_ON_LINEN, rivets=True, seed=3, fill_strength=10):
    """Linen board with a leather edge, cream stitching on the leather and a fine seam stitch on the linen."""
    W, H = size[0] * SS, size[1] * SS
    b, r = band * SS, radius * SS
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    edge_img = leather((W, H), edge, seed=seed).convert("RGBA")
    out.paste(edge_img, (0, 0), rounded_mask((W, H), r))
    d = ImageDraw.Draw(out)
    d.rounded_rectangle((0, 0, W - 1, H - 1), radius=r, outline=rgba(edge_dark), width=2 * SS)
    fill_img = weave((W - 2 * b, H - 2 * b), fill, strength=fill_strength, seed=seed + 1).convert("RGBA")
    inner_r = max(2 * SS, r - b // 2)
    out.paste(fill_img, (b, b), rounded_mask(fill_img.size, inner_r))
    d.rounded_rectangle((b, b, W - 1 - b, H - 1 - b), radius=inner_r, outline=rgba(edge_dark, 160), width=SS)
    half = b // 2
    dashed_rect(d, (half, half, W - 1 - half, H - 1 - half), max(SS, r - half), rgba(stitch), max(2, int(1.6 * SS)), 9 * SS, 6 * SS)
    if inner_stitch is not None:
        o = b + 7 * SS
        dashed_rect(d, (o, o, W - 1 - o, H - 1 - o), max(SS, inner_r - 7 * SS), rgba(inner_stitch, 170), SS, 6 * SS, 5 * SS)
    if rivets:
        rr = int(b * 0.42)
        for cx, cy in ((half + r // 3, half + r // 3), (W - 1 - half - r // 3, half + r // 3), (half + r // 3, H - 1 - half - r // 3), (W - 1 - half - r // 3, H - 1 - half - r // 3)):
            rivet(out, int(cx), int(cy), rr)
    return out.resize(size, Image.LANCZOS)


def cloth_button(size, base, dark, stitch, radius=20, seed=7):
    W, H = size[0] * SS, size[1] * SS
    r = radius * SS
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    body = weave((W, H), base, strength=8, seed=seed).convert("RGBA")
    # soft top highlight and bottom shade so the button reads as raised cloth
    grad = Image.new("L", (1, H))
    for y in range(H):
        grad.putpixel((0, y), int(40 - 80 * y / H) + 128)
    grad = grad.resize((W, H))
    light = Image.new("RGBA", (W, H), (255, 255, 255, 0))
    shade = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    lp, sp = light.load(), shade.load()
    gp = grad.load()
    for y in range(H):
        v = gp[0, y] - 128
        for x in range(0, W, 1):
            if v > 0:
                lp[x, y] = (255, 255, 255, v)
            else:
                sp[x, y] = (70, 44, 24, -v // 2)
    body = Image.alpha_composite(Image.alpha_composite(body, light), shade)
    out.paste(body, (0, 0), rounded_mask((W, H), r))
    d = ImageDraw.Draw(out)
    d.rounded_rectangle((0, 0, W - 1, H - 1), radius=r, outline=rgba(dark), width=3 * SS)
    o = 9 * SS
    dashed_rect(d, (o, o, W - 1 - o, H - 1 - o), r - o // 2, rgba(stitch, 230), max(2, int(1.6 * SS)), 8 * SS, 6 * SS)
    return out.resize(size, Image.LANCZOS)


def pocket(size, fill, outline, stitch, radius=22, outline_width=4, tint=None, seed=11, cross=False):
    """Inventory slot: a sunken linen pocket with stitched rim."""
    W, H = size[0] * SS, size[1] * SS
    r = radius * SS
    m = 10 * SS
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    body = weave((W - 2 * m, H - 2 * m), fill, strength=9, seed=seed).convert("RGBA")
    if tint is not None:
        body = Image.alpha_composite(body, Image.new("RGBA", body.size, tint))
    # inner shadow at the top-left: the pocket is sunk into the bag
    sh = Image.new("L", body.size, 0)
    ImageDraw.Draw(sh).rounded_rectangle((0, 0, body.size[0] - 1, body.size[1] - 1), radius=r, outline=110, width=10 * SS)
    sh = sh.filter(ImageFilter.GaussianBlur(6 * SS))
    shade = Image.new("RGBA", body.size, (40, 25, 15, 0))
    shade.putalpha(ImageChops.multiply(sh, rounded_mask(body.size, r)))
    body = Image.alpha_composite(body, shade)
    out.paste(body, (m, m), rounded_mask(body.size, r))
    d = ImageDraw.Draw(out)
    d.rounded_rectangle((m, m, W - 1 - m, H - 1 - m), radius=r, outline=rgba(outline), width=outline_width * SS)
    o = m + 12 * SS
    dashed_rect(d, (o, o, W - 1 - o, H - 1 - o), max(SS, r - 12 * SS), rgba(stitch, 200), max(2, int(1.5 * SS)), 7 * SS, 6 * SS)
    if cross:
        c = rgba(stitch, 200)
        dashed_line(d, (o + 14 * SS, o + 14 * SS), (W - 1 - o - 14 * SS, H - 1 - o - 14 * SS), c, 2 * SS, 7 * SS, 6 * SS)
        dashed_line(d, (W - 1 - o - 14 * SS, o + 14 * SS), (o + 14 * SS, H - 1 - o - 14 * SS), c, 2 * SS, 7 * SS, 6 * SS)
    return out.resize(size, Image.LANCZOS)


def list_row(size, selected):
    W, H = size[0] * SS, size[1] * SS
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(out)
    if selected:
        body = weave((W, H - 8 * SS), (242, 217, 166), strength=8, seed=21).convert("RGBA")
        out.paste(body, (0, 4 * SS), rounded_mask(body.size, 10 * SS))
        d.rounded_rectangle((0, 4 * SS, W - 1, H - 1 - 4 * SS), radius=10 * SS, outline=rgba(HONEY), width=3 * SS)
        tab = leather((14 * SS, H - 8 * SS), LEATHER, seed=22).convert("RGBA")
        out.paste(tab, (0, 4 * SS), rounded_mask(tab.size, 6 * SS))
    else:
        body = Image.new("RGBA", (W, H - 8 * SS), rgba(LINEN_DARK, 120))
        out.paste(body, (0, 4 * SS), rounded_mask(body.size, 10 * SS))
    dashed_line(d, (18 * SS, H - 6 * SS), (W - 18 * SS, H - 6 * SS), rgba(STITCH_ON_LINEN, 200), 2 * SS, 8 * SS, 6 * SS)
    return out.resize(size, Image.LANCZOS)


def strap(size, radius=24):
    """Leather strap banner (turn banner): leather with cream stitching, light text goes on it."""
    W, H = size[0] * SS, size[1] * SS
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    body = leather((W, H), LEATHER, seed=31).convert("RGBA")
    out.paste(body, (0, 0), rounded_mask((W, H), radius * SS))
    d = ImageDraw.Draw(out)
    d.rounded_rectangle((0, 0, W - 1, H - 1), radius=radius * SS, outline=rgba(LEATHER_DARK), width=3 * SS)
    o = 12 * SS
    dashed_rect(d, (o, o, W - 1 - o, H - 1 - o), (radius - 8) * SS, rgba(CREAM), 2 * SS, 9 * SS, 6 * SS)
    return out.resize(size, Image.LANCZOS)


def node_plate(size):
    W, H = size[0] * SS, size[1] * SS
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    m = 24 * SS
    ring = leather((W - 2 * m, H - 2 * m), LEATHER, seed=41).convert("RGBA")
    rm = Image.new("L", ring.size, 0)
    ImageDraw.Draw(rm).ellipse((0, 0, ring.size[0] - 1, ring.size[1] - 1), fill=255)
    out.paste(ring, (m, m), rm)
    b = 34 * SS
    face = weave((W - 2 * (m + b), H - 2 * (m + b)), LINEN, strength=10, seed=42).convert("RGBA")
    fm = Image.new("L", face.size, 0)
    ImageDraw.Draw(fm).ellipse((0, 0, face.size[0] - 1, face.size[1] - 1), fill=255)
    out.paste(face, (m + b, m + b), fm)
    d = ImageDraw.Draw(out)
    d.ellipse((m, m, W - 1 - m, H - 1 - m), outline=rgba(LEATHER_DARK), width=4 * SS)
    import math
    cx, cy = W / 2, H / 2
    rr = (W - 2 * m) / 2 - b / 2
    n = 40
    for k in range(n):
        a0, a1 = 2 * math.pi * k / n, 2 * math.pi * (k + 0.55) / n
        d.line((cx + rr * math.cos(a0), cy + rr * math.sin(a0), cx + rr * math.cos(a1), cy + rr * math.sin(a1)), fill=rgba(CREAM), width=3 * SS)
    return out.resize(size, Image.LANCZOS)


def divider(size):
    W, H = size[0] * SS, size[1] * SS
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(out)
    cy = H // 2
    dashed_line(d, (40 * SS, cy), (W // 2 - 40 * SS, cy), rgba(LEATHER), 4 * SS, 14 * SS, 9 * SS)
    dashed_line(d, (W // 2 + 40 * SS, cy), (W - 40 * SS, cy), rgba(LEATHER), 4 * SS, 14 * SS, 9 * SS)
    s = 18 * SS
    d.polygon([(W // 2, cy - s), (W // 2 + s, cy), (W // 2, cy + s), (W // 2 - s, cy)], fill=rgba(MADDER), outline=rgba(MADDER_DARK))
    return out.resize(size, Image.LANCZOS)


def ap_gem(size):
    W, H = size[0] * SS, size[1] * SS
    out = Image.new("RGBA", (W, H), (0, 0, 0, 0))
    d = ImageDraw.Draw(out)
    m = 22 * SS
    d.ellipse((m, m, W - 1 - m, H - 1 - m), fill=rgba(BRASS_DARK))
    d.ellipse((m + 8 * SS, m + 8 * SS, W - 1 - m - 8 * SS, H - 1 - m - 8 * SS), fill=rgba(BRASS))
    g = m + 26 * SS
    d.ellipse((g, g, W - 1 - g, H - 1 - g), fill=rgba((30, 90, 130)))
    d.ellipse((g + 6 * SS, g + 6 * SS, W - 1 - g - 6 * SS, H - 1 - g - 6 * SS), fill=rgba(AP_BLUE))
    hl = g + 22 * SS
    d.ellipse((hl, hl, hl + 34 * SS, hl + 26 * SS), fill=(230, 248, 255, 230))
    return out.resize(size, Image.LANCZOS)


def kit():
    """VisualID -> (image, 9-slice border L,B,R,T or None for simple sprites)."""
    k = {}
    k["ui_panel_main"] = (panel((1024, 768), band=22, radius=20), (48, 48, 48, 48))
    k["ui_panel_info"] = (panel((768, 384), band=16, radius=16, rivets=False, seed=5), (36, 36, 36, 36))
    k["ui_loot_pickup_panel"] = (panel((1024, 768), band=22, radius=20, seed=8), (48, 48, 48, 48))
    k["ui_loot_drop_zone"] = (panel((768, 512), band=10, radius=16, fill=LINEN_DARK, edge=LINEN_SEAM, edge_dark=STITCH_ON_LINEN, stitch=STITCH_ON_LINEN, inner_stitch=None, rivets=False, seed=9), (32, 32, 32, 32))
    k["ui_settlement_victory_panel"] = (panel((1024, 768), band=22, radius=20, seed=12), (48, 48, 48, 48))
    k["ui_settlement_defeat_panel"] = (panel((1024, 768), band=22, radius=20, fill=(214, 198, 168), edge=WALNUT, seed=13), (48, 48, 48, 48))
    k["ui_inventory_chassis_panel"] = (panel((1024, 1024), band=26, radius=28, fill=(138, 94, 60), edge=LEATHER_DARK, edge_dark=(50, 32, 22), stitch=CREAM, inner_stitch=CREAM, seed=14, fill_strength=7), (56, 56, 56, 56))
    k["ui_combat_enemy_card"] = (panel((768, 768), band=20, radius=20, seed=15), (48, 48, 48, 48))
    k["ui_combat_enemy_card_selected"] = (panel((768, 768), band=20, radius=20, edge=HONEY, edge_dark=(150, 96, 36), seed=16), (48, 48, 48, 48))
    k["ui_combat_turn_banner"] = (strap((1024, 256)), (40, 40, 40, 40))
    k["ui_button_primary"] = (cloth_button((512, 160), MADDER, MADDER_DARK, CREAM), (40, 40, 40, 40))
    k["ui_button_secondary"] = (cloth_button((512, 160), (227, 211, 180), LEATHER, LEATHER, seed=8), (40, 40, 40, 40))
    k["ui_button_danger"] = (cloth_button((512, 160), WALNUT, (40, 28, 20), TERRACOTTA, seed=9), (40, 40, 40, 40))
    k["ui_list_row_normal"] = (list_row((1024, 128), False), (24, 24, 24, 24))
    k["ui_list_row_selected"] = (list_row((1024, 128), True), (24, 24, 24, 24))
    k["ui_inventory_slot_available"] = (pocket((256, 256), (226, 210, 180), LEATHER_DARK, STITCH_ON_LINEN), None)
    k["ui_inventory_slot_hover"] = (pocket((256, 256), (238, 222, 190), HONEY, (180, 110, 40), outline_width=7), None)
    k["ui_inventory_slot_valid"] = (pocket((256, 256), (226, 210, 180), (96, 130, 78), (96, 130, 78), outline_width=7, tint=rgba(SAGE, 70)), None)
    k["ui_inventory_slot_invalid"] = (pocket((256, 256), (226, 210, 180), MADDER_DARK, MADDER, outline_width=7, tint=rgba(TERRACOTTA, 80)), None)
    k["ui_inventory_slot_locked"] = (pocket((256, 256), (96, 68, 48), (50, 32, 22), (150, 120, 90), cross=True), None)
    k["ui_dungeon_node_plate"] = (node_plate((512, 512)), None)
    k["ui_title_divider"] = (divider((1024, 128)), None)
    k["ui_combat_ap_pip"] = (ap_gem((256, 256)), None)
    return k


def set_border(meta_path: Path, border):
    text = meta_path.read_text(encoding="utf-8")
    l, b, r, t = border if border else (0, 0, 0, 0)
    new = re.sub(r"spriteBorder: \{x: [^,]+, y: [^,]+, z: [^,]+, w: [^}]+\}", f"spriteBorder: {{x: {l}, y: {b}, z: {r}, w: {t}}}", text, count=1)
    if new != text:
        newline = "\r\n" if "\r\n" in text else "\n"
        meta_path.write_bytes(new.replace("\r\n", "\n").replace("\n", newline).encode("utf-8"))


def main():
    ap = argparse.ArgumentParser()
    ap.add_argument("--preview", help="also write a contact sheet here")
    ap.add_argument("--dry-run", action="store_true", help="only write the preview")
    args = ap.parse_args()
    items = kit()
    if not args.dry_run:
        for vid, (img, border) in items.items():
            png = UI_DIR / f"{vid}.png"
            meta = UI_DIR / f"{vid}.png.meta"
            assert png.exists() and meta.exists(), f"missing {png} or its .meta"
            img.save(png)
            set_border(meta, border)
            print("wrote", vid, img.size, border)
    if args.preview:
        cell = 300
        cols = 6
        rows = (len(items) + cols - 1) // cols
        sheet = Image.new("RGB", (cols * cell, rows * cell), (60, 44, 32))
        for i, (vid, (img, _)) in enumerate(items.items()):
            t = img.copy()
            t.thumbnail((cell - 20, cell - 40))
            sheet.paste(t, ((i % cols) * cell + 10, (i // cols) * cell + 30), t)
            ImageDraw.Draw(sheet).text(((i % cols) * cell + 10, (i // cols) * cell + 8), vid[3:], fill=(244, 230, 204))
        sheet.save(args.preview)


if __name__ == "__main__":
    main()
