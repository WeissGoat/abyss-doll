"""Measures what the abyss pays against what the town costs (配置表(JSON)/Balance/balance_targets.json, "Economy").

The model: 设计文档/数值/01_经济数值.md. Reads the drop tables (Rewards), the layers' node pools and the monsters'
RewardIDs, and prints for each layer the expected haul of one node, one segment, a whole layer with its boss, and the
cells that haul takes in the bag. Everything is valued at the dump-box price (BaseValue x DumpBoxMultiplier), the
price a player gets without arranging the showcase. Then it compares a typical week of dives with the rent and the
growth the week is meant to pay for. Nothing is blocked; --strict exits 1 when a layer is outside its band.

    python tools/balance/check_economy.py           # one table per layer plus the week
    python tools/balance/check_economy.py --rewards # also every reward table's expected value
"""
import argparse
import glob
import json
import os
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CONFIG = os.path.join(ROOT, "配置表(JSON)")


def load(path):
    with open(path, encoding="utf-8-sig") as f:
        return json.load(f)


def load_dir(name, key):
    table = {}
    for path in glob.glob(os.path.join(CONFIG, name, "*.json")):
        data = load(path)
        if isinstance(data, dict) and key in data:
            table[data[key]] = data
    return table


ITEMS = load_dir("Items", "ConfigID")
REWARDS = load_dir("Rewards", "RewardID")
MONSTERS = load_dir("Monsters", "MonsterID")
ECONOMY = load(os.path.join(CONFIG, "Economy", "economy_town_v1.json"))
TARGETS = load(os.path.join(CONFIG, "Balance", "balance_targets.json"))


class Haul:
    """Expected gold (at the dump-box price) and bag cells of a drop."""

    def __init__(self, gold=0.0, cells=0.0):
        self.gold = gold
        self.cells = cells

    def __add__(self, other):
        return Haul(self.gold + other.gold, self.cells + other.cells)

    def __mul__(self, k):
        return Haul(self.gold * k, self.cells * k)


def item_haul(item_id, count=1):
    item = ITEMS.get(item_id)
    # Relics are worth nothing in town and crumble at the exit, so they are not haul.
    if item is None or item.get("ItemType") == "Relic":
        return Haul()
    sellable = item.get("BaseValue", 0) > 0 and item.get("ItemType") != "QuestItem"
    gold = item.get("BaseValue", 0) * ECONOMY["DumpBoxMultiplier"] if sellable else 0
    return Haul(gold, len(item["Grid"]["Shape"])) * count


def entry_haul(entry, seen):
    kind = entry.get("Type")
    count = entry.get("Count", 1)
    if kind == "Item":
        return item_haul(entry["ItemID"], count)
    if kind == "Money":
        return Haul(entry.get("Money", 0) * count, 0)
    if kind == "RewardRef":
        return reward_haul(entry.get("RewardID"), seen) * count
    return Haul()


def reward_haul(reward_id, seen=()):
    reward = REWARDS.get(reward_id)
    if reward is None or reward_id in seen:
        return Haul()
    seen = seen + (reward_id,)
    total = Haul()
    for entry in reward.get("Guaranteed", []):
        total = total + entry_haul(entry, seen)
    for pool in reward.get("WeightedPools", []):
        entries = pool.get("Entries", [])
        weight = sum(e.get("Weight", 0) for e in entries)
        if weight <= 0:
            continue
        one = Haul()
        for entry in entries:
            one = one + entry_haul(entry, seen) * (entry.get("Weight", 0) / weight)
        total = total + one * pool.get("RollCount", 1)
    return total


def monster_haul(monster_id):
    monster = MONSTERS.get(monster_id)
    return reward_haul(monster.get("RewardID")) if monster else Haul()


def is_elite(entry):
    return any(m.startswith("elite_") for m in entry.get("MonsterIDs", []))


def node_haul(entry):
    if entry.get("NodeType") == "CombatNode":
        total = Haul()
        for monster_id in entry.get("MonsterIDs", []):
            total = total + monster_haul(monster_id)
        return total
    if entry.get("RewardID"):
        return reward_haul(entry["RewardID"])
    return Haul()


def pool_average(entries):
    weight = sum(e.get("Weight", 0) for e in entries)
    total = Haul()
    for entry in entries:
        total = total + node_haul(entry) * (entry.get("Weight", 0) / weight)
    return total


def layer_numbers(layer_id):
    layer = load(os.path.join(CONFIG, "Dungeons", f"layer_{layer_id}.json"))
    pool = layer["NodePool"]
    first = pool_average([e for e in pool if not is_elite(e)])
    later = pool_average(pool)
    segments = layer.get("Segments") or [layer.get("RowCount", 0)]
    boss = monster_haul(layer["BossNode"])
    by_segment = [first * segments[0]] + [later * n for n in segments[1:]]
    return {
        "layer": layer,
        "node_first": first,
        "node_later": later,
        "segments": segments,
        "by_segment": by_segment,
        "boss": boss,
    }


def in_band(value, lo_hi):
    lo, hi = lo_hi
    return "达标" if lo <= value <= hi else f"目标 {lo}–{hi}"


def round_rent(value):
    return int(round(value / 100.0) * 100)


def main():
    parser = argparse.ArgumentParser()
    parser.add_argument("--rewards", action="store_true")
    parser.add_argument("--strict", action="store_true")
    args = parser.parse_args()

    economy = TARGETS["Economy"]
    week = economy["Week"]
    problems = 0
    gold_per_cell = {}
    full_cells = {}
    print(f"按垃圾箱价算（BaseValue × {ECONOMY['DumpBoxMultiplier']}），橱窗摆好能多卖。")
    for layer_id in (1, 2, 3):
        n = layer_numbers(layer_id)
        whole = n["boss"]
        for h in n["by_segment"]:
            whole = whole + h
        later = n["node_later"]
        gold_per_cell[layer_id] = whole.gold / whole.cells if whole.cells else 0
        full_cells[layer_id] = whole.cells
        segs = " + ".join(f"{h.gold:.0f}G/{h.cells:.0f}格" for h in n["by_segment"])
        cells_verdict = in_band(later.cells, economy["CellsPerNode"][str(layer_id)])
        problems += cells_verdict != "达标"
        print()
        print(f"== 第 {layer_id} 层（{' + '.join(map(str, n['segments']))} 格）==")
        print(f"  每格：第 1 段 {n['node_first'].gold:.0f}G / {n['node_first'].cells:.1f} 格，之后 {later.gold:.0f}G / {later.cells:.1f} 格（每格掉落格数 {cells_verdict}）")
        print(f"  每段：{segs}；首领 {n['boss'].gold:.0f}G / {n['boss'].cells:.0f} 格")
        print(f"  整层加首领：{whole.gold:.0f}G / {whole.cells:.0f} 格，平均每格战利品 {gold_per_cell[layer_id]:.0f}G")

    print()
    print("== 一周的房租 ==")
    rents = {r["Period"]: r["BaseRent"] for r in ECONOMY["RentCurve"]}
    for row in week["Weeks"]:
        layers = row["Frontier"] if isinstance(row["Frontier"], list) else [row["Frontier"]]
        income = 0.0
        for layer_id in layers:
            cells = min(week["CellsHome"][str(layer_id)], full_cells[layer_id])
            income += week["DivesPerWeek"] * week["SuccessRate"] * cells * gold_per_cell[layer_id] / len(layers)
        layer_id = "、".join(map(str, layers))
        rent = rents.get(row["Week"], 0)
        share = rent / income if income else 0
        lo = row["RentShare"] * (1 - week["Tolerance"])
        hi = row["RentShare"] * (1 + week["Tolerance"])
        verdict = "达标" if lo <= share <= hi else f"目标 {row['RentShare']:.0%}，建议房租 {round_rent(income * row['RentShare'])}"
        problems += not (lo <= share <= hi)
        print(f"  第 {row['Week']} 周（前线第 {layer_id} 层）：收入约 {income:.0f}G，房租 {rent}，占 {share:.0%}：{verdict}")

    if args.rewards:
        print()
        print("== 奖励表期望 ==")
        for reward_id in sorted(REWARDS):
            h = reward_haul(reward_id)
            print(f"  {reward_id:48s} {h.gold:6.0f}G {h.cells:4.1f} 格")

    if args.strict and problems:
        sys.exit(1)


if __name__ == "__main__":
    main()
