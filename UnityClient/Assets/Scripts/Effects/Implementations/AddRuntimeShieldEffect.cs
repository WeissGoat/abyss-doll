using UnityEngine;

public class AddRuntimeShieldEffect : EffectBase {
    private float _shieldAmount;
    
    public override void Init(EffectData data) {
        base.Init(data);
        float baseAmount = (data.Params != null && data.Params.Length > 0) ? data.Params[0] : 0f;
        _shieldAmount = baseAmount + (Level * 5.0f);
    }

    public override void ApplyToFighter(FighterEntity fighter, ItemEntity provider) {
        // A trigger chain's amplifiers raise it (CombatItemEffects.OutputScale is 1 otherwise).
        fighter.AddShield(Mathf.RoundToInt(_shieldAmount * CombatItemEffects.OutputScale));
        Debug.Log($"[Effect] Active shield used from {provider?.Name}. Added {_shieldAmount} shield to {fighter.Name}.");
    }
}
