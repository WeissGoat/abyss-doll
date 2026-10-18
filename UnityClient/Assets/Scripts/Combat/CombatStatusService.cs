using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public enum CombatStatusType {
    // Loses its stacks in HP at the start of its own turn, then one stack fades.
    Poison = 0,
    // Takes more damage while it lasts; one stack fades at the end of its own turn.
    Vulnerable = 1,
    // Deals less damage while it lasts; one stack fades at the end of its own turn.
    Weak = 2
}

// Stacks of a status on a fighter during one fight (docs/specs/2026-10-17-content-expansion.md, ApplyStatus). Items put
// them on enemies; the rules here also work on Zero, should a monster ever put one on her. Damage between fighters goes
// through ModifyDamage so the intents on screen read the same numbers the turn will deal.
public static class CombatStatusService {
    public const float VulnerableMultiplier = 1.5f;
    public const float WeakMultiplier = 0.75f;

    public static int Get(FighterEntity fighter, CombatStatusType status) {
        return fighter != null && fighter.Statuses.TryGetValue(status, out int stacks) ? stacks : 0;
    }

    public static void Apply(FighterEntity target, CombatStatusType status, int stacks, string source = "") {
        if (target == null || stacks <= 0 || target.RuntimeHP <= 0) {
            return;
        }

        target.Statuses[status] = Get(target, status) + stacks;
        GameEventBus.PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.Interfere, Actor = target, Label = $"{Label(status)} +{stacks}" });
        Debug.Log($"[CombatStatus] {source} → {target.Name}: {status} +{stacks} (now {Get(target, status)}).");
    }

    // Damage one fighter deals another, after the attacker's 迟缓, the target's 破甲, then the target's armour.
    public static int ModifyDamage(FighterEntity attacker, FighterEntity target, int damage) {
        if (damage <= 0) {
            return damage;
        }

        float value = damage;
        if (Get(attacker, CombatStatusType.Weak) > 0) {
            value *= WeakMultiplier;
        }

        if (Get(target, CombatStatusType.Vulnerable) > 0) {
            value *= VulnerableMultiplier;
        }

        int armor = target != null ? target.Armor : 0;
        return Mathf.Max(1, Mathf.RoundToInt(value) - armor);
    }

    // Takes every stack of a status off (a monster cleansing itself). Returns how many stacks went.
    public static int Clear(FighterEntity fighter, CombatStatusType status) {
        int stacks = Get(fighter, status);
        if (stacks > 0) {
            fighter.Statuses.Remove(status);
        }

        return stacks;
    }

    // The fighter's own turn begins: poison bites, then fades by one.
    public static void TickTurnStart(FighterEntity fighter) {
        int poison = Get(fighter, CombatStatusType.Poison);
        if (poison <= 0 || fighter.RuntimeHP <= 0) {
            return;
        }

        GameEventBus.PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.Interfere, Actor = fighter, Label = $"中毒 -{poison} HP" });
        fighter.TakeDamage(poison);
        Fade(fighter, CombatStatusType.Poison);
    }

    // The fighter's own turn ends: 破甲 and 迟缓 fade by one.
    public static void TickTurnEnd(FighterEntity fighter) {
        Fade(fighter, CombatStatusType.Vulnerable);
        Fade(fighter, CombatStatusType.Weak);
    }

    // "护甲 3 · 中毒 3 · 破甲 1" for the enemy's name plate; empty when nothing is on it.
    public static string Describe(FighterEntity fighter) {
        if (fighter == null) {
            return string.Empty;
        }

        IEnumerable<string> parts = fighter.Statuses.Where(pair => pair.Value > 0).OrderBy(pair => pair.Key).Select(pair => $"{Label(pair.Key)} {pair.Value}");
        if (fighter.Armor > 0) {
            parts = new[] { $"护甲 {fighter.Armor}" }.Concat(parts);
        }

        return string.Join(" · ", parts);
    }

    public static string Label(CombatStatusType status) {
        switch (status) {
            case CombatStatusType.Poison: return "中毒";
            case CombatStatusType.Vulnerable: return "破甲";
            default: return "迟缓";
        }
    }

    // What one stack does, for item tooltips.
    public static string Explain(CombatStatusType status) {
        switch (status) {
            case CombatStatusType.Poison: return "它的回合开始时按层数掉血，然后少一层";
            case CombatStatusType.Vulnerable: return $"受到的伤害 ×{VulnerableMultiplier:0.#}，它的回合结束时少一层";
            default: return $"造成的伤害 ×{WeakMultiplier:0.##}，它的回合结束时少一层";
        }
    }

    private static void Fade(FighterEntity fighter, CombatStatusType status) {
        int stacks = Get(fighter, status);
        if (stacks <= 1) {
            fighter.Statuses.Remove(status);
        } else {
            fighter.Statuses[status] = stacks - 1;
        }
    }
}
