"""Checks items and monsters in 配置表(JSON) against the numeric targets (配置表(JSON)/Balance/balance_targets.json).

The model and where each number comes from: 设计文档/数值/02_战斗数值.md. Items are priced by formula (weapons by net
damage per AP, active shields by shield per AP, passive shields by a share of one AP of shield); monsters are compared
with what tools/balance/apply_monster_targets.py would write, so a hand edit shows up here. How a fight actually goes is
measured by the fight lab (FightLabTest). Prints one line per item or monster outside its budget; nothing is blocked.
--strict exits 1 when anything is over budget.

    python tools/balance/check_budget.py            # over / under budget only
    python tools/balance/check_budget.py --all      # every priced item and monster
"""
import argparse
import glob
import json
import os
import sys

import apply_monster_targets

ROOT = os.path.dirname(os.path.dirname(os.path.dirname(os.path.abspath(__file__))))
CONFIG = os.path.join(ROOT, "配置表(JSON)")


def load(path):
    with open(path, encoding="utf-8-sig") as f:
        return json.load(f)


def item_layer(item):
    for tag in item.get("LayerTags", []):
        if tag.startswith("Layer") and tag[5:].isdigit():
            return int(tag[5:])
    return None


def effects(item, effect_id):
    return [e for e in (item.get("Combat") or {}).get("Effects", []) if e.get("EffectID") == effect_id]


def first_param(effect, default=0.0):
    params = effect.get("Params") or []
    return float(params[0]) if params else default


def check_weapon(item, layer, ruler, row):
    combat = item["Combat"]
    weapon = ruler["Weapon"]
    ap = max(1, combat.get("APCost", 1))
    self_cost = sum(first_param(e) for e in effects(item, "SelfCost"))
    damage = combat.get("BaseValue", 0) - self_cost * ruler["SelfDamageWeight"]
    per_ap = damage / ap
    cells = len(item["Grid"]["Shape"])
    allowance = 1 + weapon["AllowancePerExtraCell"] * (cells - weapon["BaselineCells"])
    allowance *= ruler["RarityAllowance"].get(item.get("Rarity"), 1.0)
    uses = [int(first_param(e)) for e in effects(item, "UsesPerCombat")]
    note = ""
    if uses and uses[0] <= weapon["BurstUsesPerCombat"]:
        allowance *= weapon["BurstAllowance"]
        note = f"，每场限 {uses[0]} 次"
    budget = row["DamagePerAP"] * allowance
    ratio = per_ap / budget if budget else 0
    detail = f"每 AP {per_ap:.1f}（伤害 {combat.get('BaseValue', 0)}，{ap} AP" + (f"，自伤 {self_cost:g}" if self_cost else "") + f"{note}），预算 {budget:.1f}"
    return ratio, weapon["Over"], weapon["Under"], detail


def check_active_shield(item, layer, ruler, row):
    combat = item["Combat"]
    rule = ruler["ActiveShield"]
    ap = max(1, combat.get("APCost", 1))
    shield = sum(first_param(e) for e in effects(item, "AddRuntimeShield"))
    thorns = sum(first_param(e) for e in effects(item, "Thorns"))
    per_ap = (shield + thorns * rule["ThornsValue"]) / ap
    cells = len(item["Grid"]["Shape"])
    allowance = (1 + rule["AllowancePerExtraCell"] * (cells - rule["BaselineCells"])) * ruler["RarityAllowance"].get(item.get("Rarity"), 1.0)
    budget = row["ShieldPerAP"] * allowance
    detail = f"每 AP 护盾 {per_ap:.1f}（护盾 {shield:g}" + (f"，反伤 {thorns:g}" if thorns else "") + f"，{ap} AP），预算 {budget:.1f}"
    return per_ap / budget if budget else 0, rule["Over"], rule["Under"], detail


def check_passive_shield(item, layer, ruler, row):
    shield = sum(first_param(e) for e in effects(item, "AddRuntimeShieldOnTurnStart"))
    cells = len(item["Grid"]["Shape"])
    rule = ruler["PassiveShield"]
    share = min(rule["MaxShare"], rule["BaseShare"] + rule["SharePerCell"] * cells)
    budget = row["ShieldPerAP"] * share * ruler["RarityAllowance"].get(item.get("Rarity"), 1.0)
    detail = f"每回合白送护盾 {shield:g}，预算 {budget:.1f}（本层 1 AP 盾 {row['ShieldPerAP']} 的 {share:.0%}）"
    return shield / budget if budget else 0, rule["Over"], 0, detail


def check_monster(monster, ruler, rows):
    result = apply_monster_targets.targets_for(monster, ruler, rows)
    if result is None:
        return None
    kind, hp, regular, big = result
    keep = ruler.get("Monsters", {}).get(monster["MonsterID"], {}).get("Keep", [])
    problems = []
    if abs(monster.get("HP", 0) - hp) > max(5, hp * 0.05):
        problems.append(f"HP {monster.get('HP')}，目标 {hp}")
    hits = []
    for action in (monster.get("AI") or {}).get("Actions", []):
        if action.get("ActionType") != "DamageTarget":
            continue
        params = action.get("Params") or {}
        damage = params.get("Damage", 0) * max(1, params.get("RepeatCount", 1))
        big_hit = action.get("CooldownTurns", 0) >= apply_monster_targets.BIG_COOLDOWN
        hits.append(f"{damage}{'（重击）' if big_hit else ''}")
        if action["ActionID"] in keep:
            continue
        target = big if big_hit else regular
        if abs(damage - target) > max(2, target * 0.1):
            problems.append(f"{action['ActionID']} 出手 {damage}，目标 {target}")
    summary = f"HP {monster.get('HP')}，出手 {' / '.join(hits) or '无'}"
    return kind, monster.get("Layer"), summary, problems


def main():
    parser = argparse.ArgumentParser(description=__doc__, formatter_class=argparse.RawDescriptionHelpFormatter)
    parser.add_argument("--all", action="store_true", help="also list what is within budget")
    parser.add_argument("--strict", action="store_true", help="exit 1 when anything is over budget")
    args = parser.parse_args()
    if hasattr(sys.stdout, "reconfigure"):
        sys.stdout.reconfigure(encoding="utf-8")

    ruler = load(os.path.join(CONFIG, "Balance", "balance_targets.json"))
    rows = {row["Layer"]: row for row in ruler["Layers"]}
    over = 0
    unpriced = []

    print("== 物品 ==")
    for path in sorted(glob.glob(os.path.join(CONFIG, "Items", "*.json"))):
        item = load(path)
        combat = item.get("Combat") or {}
        layer = item_layer(item)
        if combat.get("TriggerType") == "Manual" and combat.get("DamageType") in ("Physical", "Energy"):
            check = check_weapon
        elif combat.get("TriggerType") == "Manual" and effects(item, "AddRuntimeShield"):
            check = check_active_shield
        elif effects(item, "AddRuntimeShieldOnTurnStart"):
            check = check_passive_shield
        else:
            if combat and combat.get("TriggerType") == "Passive" and combat.get("DamageType") == "Shield" and not combat.get("Effects"):
                print(f"  无效   {item['ConfigID']}（{item['Name']}）：被动护盾物品没有任何效果，战斗里什么也不给")
            elif combat.get("Effects") or combat.get("BaseValue"):
                unpriced.append(item["ConfigID"])
            continue
        if layer not in rows:
            unpriced.append(item["ConfigID"] + "（没有层标签）")
            continue
        ratio, over_at, under_at, detail = check(item, layer, ruler, rows[layer])
        label = "超预算" if ratio > over_at else "偏弱" if under_at and ratio < under_at else "正常"
        if label == "偏弱" and effects(item, "ApplyStatus"):
            label = "状态未计"
        if label == "超预算":
            over += 1
        if label != "正常" or args.all:
            print(f"  {label:4s} L{layer} {item['ConfigID']}（{item['Name']}）：{ratio:.2f} 倍。{detail}")

    print("\n== 怪物 ==")
    for path in sorted(glob.glob(os.path.join(CONFIG, "Monsters", "*.json"))):
        monster = load(path)
        result = check_monster(monster, ruler, rows)
        if result is None:
            continue
        kind, layer, summary, problems = result
        if problems or args.all:
            print(f"  L{layer} {kind:6s} {monster['MonsterID']}（{monster['Name']}）：{summary}" + ("。" + "；".join(problems) if problems else ""))

    print(f"\n还没有定价规则的物品（{len(unpriced)}）：{', '.join(unpriced)}")
    print(f"超预算物品 {over} 件。")
    return 1 if args.strict and over else 0


if __name__ == "__main__":
    sys.exit(main())
