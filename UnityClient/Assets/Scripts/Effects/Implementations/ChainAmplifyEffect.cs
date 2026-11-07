using UnityEngine;

// A conduit that strengthens the chain (增幅线圈): modules fired after it in the same chain deal and shield Params[0]
// more (0.3 = +30%). TriggerChain.Plan reads it; it does nothing outside a chain.
public class ChainAmplifyEffect : EffectBase {
    private float _bonus;

    public override void Init(EffectData data) {
        base.Init(data);
        _bonus = data.Params != null && data.Params.Length > 0 ? data.Params[0] : 0f;
    }

    public override string Describe(ItemEntity provider, int rotation) {
        return $"链路经过时：之后带动的模块伤害、护盾 +{Mathf.RoundToInt(_bonus * 100f)}%";
    }

    public override void ValidateConfig(ConfigValidationReport report, string ownerID, EffectData data) {
        if (data.Params == null || data.Params.Length == 0 || data.Params[0] <= 0f) {
            report.AddError($"[{ownerID}] ChainAmplify needs Params[0] > 0 (0.3 = +30%).");
        }
    }
}
