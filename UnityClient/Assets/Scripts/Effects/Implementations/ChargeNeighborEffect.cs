using UnityEngine;

// After the item is used, the neighbours on its Target side (all four by default) that carry Tag get a charge: their
// next strike deals Params[0] more (0.3 = +30%). Charges add up and are spent on that strike.
public class ChargeNeighborEffect : EffectBase {
    private float _bonus;
    private string _tag;
    private string _target;

    public override void Init(EffectData data) {
        base.Init(data);
        _bonus = Mathf.Max(0f, data.Params != null && data.Params.Length > 0 ? data.Params[0] : 0f);
        _tag = data.Tag;
        _target = data.Target;
    }

    public override void OnItemUsed(ItemUseOutcome outcome, ItemEntity provider) {
        BackpackGrid grid = outcome.User?.DataRef?.RuntimeGrid as BackpackGrid;
        foreach (ItemEntity neighbour in GridSolver.Neighbors(provider, _target, grid)) {
            if (CombatItemEffects.HasTag(neighbour, _tag) && neighbour.Combat != null) {
                CombatItemEffects.AddCharge(neighbour, _bonus);
                GameEventBus.PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.Interfere, Actor = outcome.User, Label = $"{neighbour.Name} 充能 +{Mathf.RoundToInt(_bonus * 100f)}%" });
            }
        }
    }

    public override string Describe(ItemEntity provider, int rotation) {
        return $"使用后，{EffectText.Side(_target, rotation)}的{CombatItemEffects.TagLabel(_tag)}下一击伤害 +{Mathf.RoundToInt(_bonus * 100f)}%";
    }

    public override void ValidateConfig(ConfigValidationReport report, string ownerID, EffectData data) {
        if (data.Params == null || data.Params.Length == 0 || data.Params[0] <= 0f) {
            report.AddError($"[{ownerID}] ChargeNeighbor needs Params[0] > 0 (0.3 = +30%).");
        }
    }
}
