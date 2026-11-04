"""Writes the BaseValue of loot and gear in 配置表(JSON)/Items from the economy targets (balance_targets.json, "Economy").

The model: 设计文档/数值/01_经济数值.md. One cell of plain Common loot found on layer L is worth CellValue[L]; an item is
worth its cells times that, times its rarity, its shape (awkward shapes are worth more per cell, they are harder to
carry), a premium when carrying it costs SAN or corrodes, and GearShare for weapons, armour and anchors (gear is carried
to fight; selling it is the side door). Consumables and quest items are priced by hand and left alone. Per-item
flavour goes in Economy.Items ({"Value": 1.1} or {"Keep": true}). Only the top-level BaseValue changes; the shop sells
at BaseValue x ShopPriceMultiplier, so shop prices follow.

    python tools/balance/apply_item_values.py          # dry run: what would change
    python tools/balance/apply_item_values.py --write  # write the files (then run tools/config/Sync-Configs.ps1)
"""
import argparse
import glob
import json
import os
import re

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CONFIG = os.path.join(ROOT, "配置表(JSON)")
PRICED_TYPES = ("Loot", "Weapon", "Armor", "Anchor")
GEAR_TYPES = ("Weapon", "Armor", "Anchor")
NO_RISK = ("", "None", None)


def load(path):
    with open(path, encoding="utf-8-sig") as f:
        return json.load(f)


def item_layer(item):
    for tag in item.get("LayerTags", []):
        if tag.startswith("Layer") and tag[5:].isdigit():
            return int(tag[5:])
    return None


def shape_kind(shape):
    xs = [c[0] for c in shape]
    ys = [c[1] for c in shape]
    box = (max(xs) - min(xs) + 1) * (max(ys) - min(ys) + 1)
    if box != len(shape):
        return "Irregular"
    return "Large" if len(shape) >= 4 else "Compact"


def risky(item):
    # Carrying it costs something on the way (SAN, pollution of its neighbours); a maintenance note is not a risk.
    risk = item.get("Risk")
    return isinstance(risk, dict) and risk.get("PollutionProfile") not in NO_RISK


def round_value(value):
    step = 5 if value < 200 else 10
    return int(round(value / step) * step)


def target_value(item, economy):
    """(value, how) for a priced item, or None when it is priced by hand."""
    if item.get("ItemType") not in PRICED_TYPES:
        return None
    flavour = economy.get("Items", {}).get(item["ConfigID"], {})
    if flavour.get("Keep"):
        return None
    layer = item_layer(item)
    if layer is None or str(layer) not in economy["CellValue"]:
        return None

    shape = item["Grid"]["Shape"]
    cells = len(shape)
    parts = [f"L{layer}", f"{cells} 格", item.get("Rarity", "?")]
    if item["ItemType"] in GEAR_TYPES:
        # Gear is worth what it does, not the room it takes: small gear packs its power into fewer cells, and its
        # drawbacks (self-damage, corrosion) are paid for by its power, not by a premium.
        value = economy["CellValue"][str(layer)] * max(cells, economy["GearMinCells"]) * economy["GearShare"]
        parts.append("装备")
    else:
        kind = shape_kind(shape)
        value = economy["CellValue"][str(layer)] * cells * economy["Shape"][kind]
        parts.append(kind)
        if risky(item):
            value *= economy["RiskPremium"]
            parts.append("有风险")
    value *= economy["Rarity"].get(item.get("Rarity"), 1.0)
    value *= flavour.get("Value", 1.0)
    return round_value(value), "，".join(parts)


def write_value(path, value):
    with open(path, "rb") as f:
        raw = f.read()
    bom = raw.startswith(b"\xef\xbb\xbf")
    text = raw.decode("utf-8-sig")
    # The item's own BaseValue is the least indented one (Combat.BaseValue is damage).
    matches = list(re.finditer(r'(?m)^([ \t]*)"BaseValue"(\s*:\s*)(-?\d+)', text))
    top = min(matches, key=lambda m: len(m.group(1)))
    text = text[:top.start(3)] + str(value) + text[top.end(3):]
    with open(path, "wb") as f:
        f.write((b"\xef\xbb\xbf" if bom else b"") + text.encode("utf-8"))


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--write", action="store_true")
    args = parser.parse_args()

    economy = load(os.path.join(CONFIG, "Balance", "balance_targets.json"))["Economy"]
    changed = 0
    for path in sorted(glob.glob(os.path.join(CONFIG, "Items", "*.json"))):
        item = load(path)
        result = target_value(item, economy)
        if result is None:
            continue
        value, how = result
        old = item.get("BaseValue", 0)
        if value == old:
            continue
        changed += 1
        print(f"  {item['ConfigID']:30s} {old:5d} -> {value:5d}  （{how}）")
        if args.write:
            write_value(path, value)
    print(f"{'已写入' if args.write else '会改'} {changed} 件。")


if __name__ == "__main__":
    main()
