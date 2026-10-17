using System.Collections.Generic;
using UnityEngine;

// What one fight remembers about each backpack item: how often it was used and the charge a neighbour put on it.
public class CombatItemLedger {
    private readonly Dictionary<string, int> _uses = new Dictionary<string, int>();
    private readonly Dictionary<string, float> _charge = new Dictionary<string, float>();

    public void Reset() {
        _uses.Clear();
        _charge.Clear();
    }

    public int Uses(ItemEntity item) {
        return item != null && _uses.TryGetValue(item.InstanceID ?? string.Empty, out int uses) ? uses : 0;
    }

    public void CountUse(ItemEntity item) {
        if (item != null) {
            _uses[item.InstanceID ?? string.Empty] = Uses(item) + 1;
        }
    }

    public float Charge(ItemEntity item) {
        return item != null && _charge.TryGetValue(item.InstanceID ?? string.Empty, out float charge) ? charge : 0f;
    }

    public void AddCharge(ItemEntity item, float amount) {
        if (item != null && amount > 0f) {
            _charge[item.InstanceID ?? string.Empty] = Charge(item) + amount;
        }
    }

    public float TakeCharge(ItemEntity item) {
        float charge = Charge(item);
        if (item != null) {
            _charge.Remove(item.InstanceID ?? string.Empty);
        }

        return charge;
    }
}

// The fight-time side of backpack effects (docs/specs/2026-10-17-content-expansion.md): whether an item may be used
// now, what happens after a use (its own effects, then the neighbours that answer a use), a hit taken by Zero, and the
// charge a neighbour left on a weapon. Effects say what they do; this only calls them in order.
public static class CombatItemEffects {
    public const string NeighborUseTrigger = "NeighborUse";

    private static CombatItemLedger Ledger => GameRoot.Core?.Combat?.Items;

    public static int Uses(ItemEntity item) {
        return Ledger?.Uses(item) ?? 0;
    }

    public static float Charge(ItemEntity item) {
        return Ledger?.Charge(item) ?? 0f;
    }

    public static void AddCharge(ItemEntity item, float amount) {
        Ledger?.AddCharge(item, amount);
    }

    // A weapon's damage for this strike: its runtime damage, raised by a charge (which is spent).
    public static int StrikeDamage(ItemEntity weapon) {
        float charge = Ledger?.TakeCharge(weapon) ?? 0f;
        float damage = weapon?.Combat?.RuntimeDamage ?? 0f;
        return Mathf.RoundToInt(damage * (1f + charge));
    }

    public static bool CanUse(ItemEntity item, out string reason) {
        reason = string.Empty;
        foreach (EffectData data in item?.Combat?.Effects ?? new List<EffectData>()) {
            EffectBase effect = EffectFactory.CreateEffect(data);
            if (effect != null && !effect.AllowsUse(item, out reason)) {
                return false;
            }
        }

        return true;
    }

    // After the item's damage or payload: count the use, run its own effects, then any neighbour that answers a use.
    public static void AfterUse(ItemUseOutcome outcome) {
        if (outcome?.Item == null || outcome.User == null) {
            return;
        }

        Ledger?.CountUse(outcome.Item);
        foreach (EffectData data in outcome.Item.Combat?.Effects ?? new List<EffectData>()) {
            EffectFactory.CreateEffect(data)?.OnItemUsed(outcome, outcome.Item);
        }

        BackpackGrid grid = outcome.User.DataRef?.RuntimeGrid as BackpackGrid;
        if (grid == null || !grid.ContainedItems.Contains(outcome.Item)) {
            return;
        }

        foreach (ItemEntity neighbour in new List<ItemEntity>(grid.ContainedItems)) {
            if (neighbour == outcome.Item || neighbour?.Combat?.Effects == null) {
                continue;
            }

            foreach (EffectData data in neighbour.Combat.Effects) {
                if (data == null || data.Trigger != NeighborUseTrigger || !HasTag(outcome.Item, data.Tag)) {
                    continue;
                }

                if (GridSolver.Neighbors(neighbour, data.Target, grid).Contains(outcome.Item)) {
                    EffectFactory.CreateEffect(data)?.OnNeighborUsed(outcome, neighbour);
                }
            }
        }
    }

    // A monster hit Zero: backpack effects that answer a hit (thorns).
    public static void OnOwnerHit(DollFighter owner, FighterEntity attacker, int shieldBefore) {
        BackpackGrid grid = owner?.DataRef?.RuntimeGrid as BackpackGrid;
        if (grid == null || attacker == null) {
            return;
        }

        foreach (ItemEntity item in new List<ItemEntity>(grid.ContainedItems)) {
            foreach (EffectData data in item?.Combat?.Effects ?? new List<EffectData>()) {
                EffectFactory.CreateEffect(data)?.OnOwnerHit(owner, attacker, shieldBefore, item);
            }
        }
    }

    public static bool HasTag(ItemEntity item, string tag) {
        return string.IsNullOrEmpty(tag) || (item?.Tags != null && item.Tags.Exists(t => string.Equals(t, tag, System.StringComparison.OrdinalIgnoreCase)));
    }

    // The living fighters on the other side of the given one.
    public static List<FighterEntity> Opponents(FighterEntity fighter) {
        CombatSystem combat = GameRoot.Core?.Combat;
        CombatFaction other = fighter?.ParentFaction == combat?.PlayerFaction ? combat?.EnemyFaction : combat?.PlayerFaction;
        return other?.Fighters?.FindAll(f => f != null && f.RuntimeHP > 0) ?? new List<FighterEntity>();
    }

    public static string TagLabel(string tag) {
        return string.IsNullOrEmpty(tag) ? "物品" : $"「{ItemTagLabels.Label(tag)}」物品";
    }
}

// Player-facing names of item tags (配置表(JSON)/Effects/TagLabels.json); an unknown tag shows as written.
public static class ItemTagLabels {
    private static readonly Dictionary<string, string> Labels = new Dictionary<string, string>(System.StringComparer.OrdinalIgnoreCase);

    private class TagLabelFile {
        public Dictionary<string, string> Labels;
    }

    public static void Load(string path) {
        Labels.Clear();
        if (!System.IO.File.Exists(path)) {
            return;
        }

        try {
            TagLabelFile file = Newtonsoft.Json.JsonConvert.DeserializeObject<TagLabelFile>(System.IO.File.ReadAllText(path));
            foreach (KeyValuePair<string, string> pair in file?.Labels ?? new Dictionary<string, string>()) {
                Labels[pair.Key] = pair.Value;
            }
        } catch (System.Exception e) {
            Debug.LogError($"[ItemTagLabels] Could not read {path}: {e.Message}");
        }
    }

    public static string Label(string tag) {
        return !string.IsNullOrEmpty(tag) && Labels.TryGetValue(tag, out string label) ? label : tag ?? string.Empty;
    }
}

// Shared wording for effect tooltips.
public static class EffectText {
    public static string Resource(string resource) {
        switch (resource) {
            case "HP": return "HP";
            case "SAN": return "SAN";
            case "Shield": return "护盾";
            case "AP": return "AP";
            case "Corruption": return "侵蚀";
            case "Grime": return "污渍";
            default: return resource ?? string.Empty;
        }
    }

    // Which side, as it reads at the item's current rotation; all four when the direction is not set.
    public static string Side(string target, int rotation) {
        if (!System.Enum.TryParse(target, out TargetDirection direction)) {
            return "四周紧贴";
        }

        switch (BackpackGrid.RotateDirection(direction, rotation)) {
            case TargetDirection.Right: return "紧贴右侧";
            case TargetDirection.Left: return "紧贴左侧";
            case TargetDirection.Up: return "紧贴上方";
            case TargetDirection.Down: return "紧贴下方";
            default: return "四周紧贴";
        }
    }
}
