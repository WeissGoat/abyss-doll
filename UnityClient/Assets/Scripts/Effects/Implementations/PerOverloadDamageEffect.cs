using UnityEngine;

// Heat as a resource (赤热刃, GDD_01 §5.1 流派 C): each point of Zero's overload adds Params[0] damage to this weapon's
// strikes, read at the moment it strikes.
public class PerOverloadDamageEffect : EffectBase {
    private float _perPoint;

    public override void Init(EffectData data) {
        base.Init(data);
        _perPoint = data.Params != null && data.Params.Length > 0 ? data.Params[0] : 0f;
    }

    public override float StrikeBonus(ItemEntity provider) {
        return _perPoint * (TriggerChain.State?.Overload ?? 0);
    }

    public override string Describe(ItemEntity provider, int rotation) {
        int overload = TriggerChain.State?.Overload ?? 0;
        bool inFight = GameRoot.Core?.Combat != null && GameRoot.Core.Combat.CurrentState != CombatState.End;
        string now = inFight ? $"（现在 +{Mathf.RoundToInt(_perPoint * overload)}）" : string.Empty;
        return $"每点过载，伤害 +{Mathf.RoundToInt(_perPoint)}{now}";
    }

    public override void ValidateConfig(ConfigValidationReport report, string ownerID, EffectData data) {
        if (data.Params == null || data.Params.Length == 0 || data.Params[0] <= 0f) {
            report.AddError($"[{ownerID}] PerOverloadDamage needs Params[0] > 0.");
        }
    }
}
