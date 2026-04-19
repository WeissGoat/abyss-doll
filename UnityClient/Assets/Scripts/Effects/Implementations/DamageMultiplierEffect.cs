using UnityEngine;

public class DamageMultiplierEffect : EffectBase {
    private float _multiplier;
    
    public override void Init(EffectData data) {
        base.Init(data);
        // Safely extract the base multiplier from the Params array
        float baseMultiplier = (data.Params != null && data.Params.Length > 0) ? data.Params[0] : 0f;
        _multiplier = baseMultiplier + (Level * 0.05f); // Example of level scaling
    }

    public override void Apply(ItemEntity provider, ItemEntity target) {
        if (target != null && target.Combat != null) {
            target.Combat.RuntimeDamage *= (1.0f + _multiplier);
            Debug.Log($"[Effect] Applied DamageMultiplier (+{_multiplier * 100}%) to {target.Name}. New Dmg: {target.Combat.RuntimeDamage}");
        }
    }
    
    public override void Remove(ItemEntity provider, ItemEntity target) {
        // Handled via full recalculation normally
    }
}
