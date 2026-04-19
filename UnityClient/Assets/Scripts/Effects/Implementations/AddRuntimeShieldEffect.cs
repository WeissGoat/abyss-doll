using UnityEngine;

public class AddRuntimeShieldEffect : EffectBase {
    private float _shieldAmount;
    
    public override void Init(EffectData data) {
        base.Init(data);
        float baseAmount = (data.Params != null && data.Params.Length > 0) ? data.Params[0] : 0f;
        _shieldAmount = baseAmount + (Level * 5.0f);
    }

    public override void Apply(ItemEntity provider, ItemEntity target) {
        // For MVP, just log it. The CombatSystem or the DollFighter will read this buffer when an action is executed.
        Debug.Log($"[Effect] Active AddRuntimeShield buffer applied. Amount: {_shieldAmount}");
    }
    
    public override void Remove(ItemEntity provider, ItemEntity target) { }
}
