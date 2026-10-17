using UnityEngine;

// The item grows with its neighbours: for each item touching it (all four sides) that carries Tag, its own damage
// rises by Params[0] (Operation AddPercent: 0.1 = +10% each; otherwise flat points each). Set Target to Self.
public class PerAdjacentTagEffect : EffectBase {
    private float _perNeighbour;
    private string _tag;
    private bool _percent;

    public override void Init(EffectData data) {
        base.Init(data);
        _perNeighbour = data.Params != null && data.Params.Length > 0 ? data.Params[0] : 0f;
        _tag = data.Tag;
        _percent = data.Operation == nameof(EffectModifierOperation.AddPercent);
    }

    public override void ApplyInGrid(ItemEntity provider, ItemEntity target, BackpackGrid grid) {
        if (target != provider || target?.Combat == null) {
            return;
        }

        int count = CountNeighbours(provider, grid);
        if (count <= 0) {
            return;
        }

        target.Combat.RuntimeDamage = _percent
            ? target.Combat.RuntimeDamage * (1f + _perNeighbour * count)
            : target.Combat.RuntimeDamage + _perNeighbour * count;
    }

    public int CountNeighbours(ItemEntity provider, BackpackGrid grid) {
        int count = 0;
        foreach (ItemEntity neighbour in GridSolver.Neighbors(provider, TargetDirection.AllAdjacent.ToString(), grid)) {
            if (CombatItemEffects.HasTag(neighbour, _tag)) {
                count++;
            }
        }

        return count;
    }

    public override string Describe(ItemEntity provider, int rotation) {
        string each = _percent ? $"+{Mathf.RoundToInt(_perNeighbour * 100f)}%" : $"+{Mathf.RoundToInt(_perNeighbour)}";
        return $"四周每有一件{CombatItemEffects.TagLabel(_tag)}，自身伤害 {each}";
    }

    public override void ValidateConfig(ConfigValidationReport report, string ownerID, EffectData data) {
        if (data.Params == null || data.Params.Length == 0 || data.Params[0] <= 0f) {
            report.AddError($"[{ownerID}] PerAdjacentTag needs Params[0] > 0.");
        }

        if (string.IsNullOrEmpty(data.Tag)) {
            report.AddWarning($"[{ownerID}] PerAdjacentTag without Tag counts every neighbour.");
        }

        if (data.Target != TargetDirection.Self.ToString()) {
            report.AddError($"[{ownerID}] PerAdjacentTag needs Target Self (it counts its own neighbours).");
        }
    }
}
