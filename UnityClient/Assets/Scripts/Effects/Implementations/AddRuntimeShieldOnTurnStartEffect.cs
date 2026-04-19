using UnityEngine;

public class AddRuntimeShieldOnTurnStartEffect : EffectBase {
    private float _shieldAmount;
    
    public override void Init(EffectData data) {
        base.Init(data);
        float baseAmount = (data.Params != null && data.Params.Length > 0) ? data.Params[0] : 0f;
        _shieldAmount = baseAmount + (Level * 2.0f);
    }

    public override void Apply(ItemEntity provider, ItemEntity target) {
        // Will be triggered passively
        Debug.Log($"[Effect] Registered passive turn-start shield for {provider?.Name}. Amount: {_shieldAmount}");
    }
    
    public override void Remove(ItemEntity provider, ItemEntity target) { }
}
