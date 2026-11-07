using UnityEngine;

// The item heats Zero (赤热刃): each use, by hand or fired by a chain, adds Params[0] overload. Over capacity after the
// use, she jams next turn like after a chain (TriggerChain.CheckOverflow).
public class HeatOnUseEffect : EffectBase {
    private int _heat;

    public override void Init(EffectData data) {
        base.Init(data);
        _heat = Mathf.RoundToInt(data.Params != null && data.Params.Length > 0 ? data.Params[0] : 1f);
    }

    public int Heat => _heat;

    public override void OnItemUsed(ItemUseOutcome outcome, ItemEntity provider) {
        ChainState state = TriggerChain.State;
        if (state == null || _heat == 0) {
            return;
        }

        state.Add(_heat);
        GameEventBus.PublishOverloadChanged(state.Overload, state.Capacity, state.Jammed);
    }

    public override string Describe(ItemEntity provider, int rotation) {
        return $"每用一次，过载 +{_heat}";
    }

    public override void ValidateConfig(ConfigValidationReport report, string ownerID, EffectData data) {
        if (data.Params == null || data.Params.Length == 0 || data.Params[0] == 0f) {
            report.AddError($"[{ownerID}] HeatOnUse needs Params[0] (overload per use).");
        }
    }
}
