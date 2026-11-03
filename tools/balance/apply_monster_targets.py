"""Writes monster HP and hit sizes in 配置表(JSON)/Monsters from the targets in 配置表(JSON)/Balance/balance_targets.json.

Monster numbers are not tuned by hand: change the targets (or a monster's flavour in "Monsters") and run this, then
sync the configs and check with the fight lab. The model is in 设计文档/数值/02_战斗数值.md:

- monsters are sized against the kit a player has on arriving at the layer (Arrival: the good gear of the layers
  above), so what the layer itself drops makes it easier;
- a hit is sized by the shield it takes: ShieldAP x the arrival ShieldPerAP + the arrival Passive shield + Leak (what
  still gets through);
- a monster's attack gets the kind's Regular hit, or the Big hit when it has a cooldown of 2 turns or more (it comes
  every few turns, and the intent shows it coming);
- HP = middle of the kind's turn range x 2 AP x the arrival DamagePerAP x HPFactor (one AP of three goes to the
  shield on a typical turn).

Each monster keeps its character through its flavour: HP and Hit multipliers (default 1), and Keep, the attacks this
leaves alone (a whisper that is about SAN, not damage). Other numbers (junk,
weakening, self-destruct, armour) are left alone. Only the changed numbers are rewritten; formatting is kept.

    python tools/balance/apply_monster_targets.py          # show what would change
    python tools/balance/apply_monster_targets.py --write  # rewrite the monster files
"""
import argparse
import glob
import json
import os
import re
import sys

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CONFIG = os.path.join(ROOT, "配置表(JSON)")
BIG_COOLDOWN = 2


def load(path):
    with open(path, encoding="utf-8-sig") as f:
        return json.load(f)


def kind_of(monster_id):
    return "Boss" if monster_id.startswith("boss_") else "Elite" if monster_id.startswith("elite_") else "Normal"


def hit(arrival, spec):
    return spec["ShieldAP"] * arrival["ShieldPerAP"] + arrival["Passive"] + spec["Leak"]


def targets_for(monster, ruler, rows):
    layer = rows.get(monster.get("Layer"))
    if layer is None:
        return None
    kind = kind_of(monster["MonsterID"])
    fight = ruler["Fights"][kind]
    flavour = ruler.get("Monsters", {}).get(monster["MonsterID"], {})
    low, high = fight["Turns"]
    arrival = layer["Arrival"]
    hp = (low + high) / 2 * ruler["OffenseAPPerTurn"] * arrival["DamagePerAP"] * fight["HPFactor"] * flavour.get("HP", 1.0)
    regular = hit(arrival, fight["Hits"]["Regular"]) * flavour.get("Hit", 1.0)
    big = hit(arrival, fight["Hits"]["Big"]) * flavour.get("Hit", 1.0)
    return kind, int(round(hp / 5.0) * 5), int(round(regular)), int(round(big))


def rewrite(text, monster, hp, regular, big, keep):
    changes = []
    text, n = re.subn(r'("HP":\s*)\d+', lambda m: m.group(1) + str(hp), text, count=1)
    if monster["HP"] != hp:
        changes.append(f"HP {monster['HP']} -> {hp}")
    for action in monster["AI"]["Actions"]:
        if action.get("ActionType") != "DamageTarget" or action["ActionID"] in keep:
            continue
        damage = big if action.get("CooldownTurns", 0) >= BIG_COOLDOWN else regular
        repeat = max(1, int(action.get("Params", {}).get("RepeatCount", 1)))
        damage = max(1, int(round(damage / repeat)))
        start = text.find(f'"ActionID": "{action["ActionID"]}"')
        if start < 0:
            continue
        match = re.compile(r'("Damage":\s*)\d+').search(text, start)
        if match is None:
            continue
        old = int(action["Params"]["Damage"])
        text = text[:match.start()] + match.group(1) + str(damage) + text[match.end():]
        if old != damage:
            changes.append(f"{action['ActionID']} {old} -> {damage}" + (f"x{repeat}" if repeat > 1 else ""))
    # The intent list is only a design note (the screen reads the actions); keep its numbers in step.
    for intent_type, value in (("Attack", regular), ("Charge", big)):
        text = re.sub(r'("IntentType":\s*"' + intent_type + r'",\s*"PowerPreview":\s*)\d+', lambda m: m.group(1) + str(value), text)
    return text, changes


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--write", action="store_true", help="rewrite the monster files")
    args = parser.parse_args()
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")

    ruler = load(os.path.join(CONFIG, "Balance", "balance_targets.json"))
    rows = {row["Layer"]: row for row in ruler["Layers"]}
    for path in sorted(glob.glob(os.path.join(CONFIG, "Monsters", "*.json"))):
        monster = load(path)
        result = targets_for(monster, ruler, rows)
        if result is None:
            continue
        kind, hp, regular, big = result
        with open(path, encoding="utf-8-sig", newline="") as f:
            text = f.read()
        keep = ruler.get("Monsters", {}).get(monster["MonsterID"], {}).get("Keep", [])
        new_text, changes = rewrite(text, monster, hp, regular, big, keep)
        if changes:
            print(f"L{monster['Layer']} {kind:6s} {monster['MonsterID']}: " + "; ".join(changes))
        if args.write and new_text != text:
            with open(path, "w", encoding="utf-8", newline="") as f:
                f.write(new_text)
    return 0


if __name__ == "__main__":
    sys.exit(main())
