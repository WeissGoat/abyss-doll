using UnityEngine;

// Heat into shield (泄压阀): using it clears Zero's overload and gives Params[0] shield per point vented. Needs some
// overload to vent.
public class VentOverloadEffect : EffectBase {
    private float _shieldPerPoint;

    public override void Init(EffectData data) {
        base.Init(data);
        _shieldPerPoint = data.Params != null && data.Params.Length > 0 ? data.Params[0] : 0f;
    }

    public override bool AllowsUse(ItemEntity provider, out string reason) {
        bool any = (TriggerChain.State?.Overload ?? 0) > 0;
        reason = any ? string.Empty : $"[{provider?.Name}] 现在没有过载可泄。";
        return any;
    }

    public override void ApplyOnUse(ItemUseContext context, ItemEntity provider) {
        ChainState state = TriggerChain.State;
        if (state == null || context?.UserFighter == null || state.Overload <= 0) {
            return;
        }

        int vented = state.Overload;
        state.Overload = 0;
        int shield = Mathf.RoundToInt(vented * _shieldPerPoint * CombatItemEffects.OutputScale);
        context.UserFighter.AddShield(shield);
        GameEventBus.PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.Interfere, Actor = context.UserFighter, Label = $"泄掉 {vented} 点过载，护盾 +{shield}" });
        GameEventBus.PublishOverloadChanged(state.Overload, state.Capacity, state.Jammed);
    }

    public override string Describe(ItemEntity provider, int rotation) {
        return $"使用：过载清零，每泄掉 1 点护盾 +{Mathf.RoundToInt(_shieldPerPoint)}";
    }

    public override void ValidateConfig(ConfigValidationReport report, string ownerID, EffectData data) {
        if (data.Params == null || data.Params.Length == 0 || data.Params[0] <= 0f) {
            report.AddError($"[{ownerID}] VentOverload needs Params[0] > 0 (shield per point).");
        }
    }
}
