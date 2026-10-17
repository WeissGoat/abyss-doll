using UnityEngine;

// Only Params[0] uses per fight (throwing knives, a one-shot trick); the count starts over in every fight.
public class UsesPerCombatEffect : EffectBase {
    private int _uses;

    public override void Init(EffectData data) {
        base.Init(data);
        _uses = Mathf.Max(1, Mathf.RoundToInt(data.Params != null && data.Params.Length > 0 ? data.Params[0] : 1f));
    }

    public override bool AllowsUse(ItemEntity provider, out string reason) {
        int left = UsesLeft(provider);
        reason = left > 0 ? string.Empty : $"[{provider?.Name}] 这场战斗已经用完了（每场 {_uses} 次）。";
        return left > 0;
    }

    public int UsesLeft(ItemEntity provider) {
        return Mathf.Max(0, _uses - CombatItemEffects.Uses(provider));
    }

    public override string Describe(ItemEntity provider, int rotation) {
        bool inFight = GameRoot.Core?.Combat != null && GameRoot.Core.Combat.CurrentState != CombatState.End;
        return inFight ? $"每场战斗限用 {_uses} 次（还剩 {UsesLeft(provider)} 次）" : $"每场战斗限用 {_uses} 次";
    }

    public override void ValidateConfig(ConfigValidationReport report, string ownerID, EffectData data) {
        if (data.Params == null || data.Params.Length == 0 || data.Params[0] < 1f) {
            report.AddError($"[{ownerID}] UsesPerCombat needs Params[0] >= 1 (uses per fight).");
        }
    }
}
