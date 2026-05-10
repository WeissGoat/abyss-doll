using UnityEngine;

public class ExtraSanCostOnNodeEnterEffect : EffectBase {
    private int _extraSanCost;

    public override void Init(EffectData data) {
        base.Init(data);
        float baseCost = (data.Params != null && data.Params.Length > 0) ? data.Params[0] : 0f;
        _extraSanCost = Mathf.Max(0, Mathf.RoundToInt(baseCost));
    }

    public override int GetExtraSanCostOnNodeEnter(DungeonMoveCostContext context, ItemEntity provider) {
        if (context == null || provider == null || _extraSanCost <= 0) {
            return 0;
        }

        return _extraSanCost;
    }
}
