using UnityEngine;

public class AddRuntimeShieldOnTurnStartEffect : EffectBase {
    private float _shieldAmount;
    
    public override CombatEventType ListenEvent => CombatEventType.OnTurnStart;
    
    public override void Init(EffectData data) {
        base.Init(data);
        float baseAmount = (data.Params != null && data.Params.Length > 0) ? data.Params[0] : 0f;
        _shieldAmount = baseAmount + (Level * 2.0f);
    }

    public override void ApplyToFighter(FighterEntity fighter, ItemEntity provider) {
        fighter.AddShield((int)_shieldAmount);
        Debug.Log($"[Effect] {provider?.Name} passive triggered. Added {_shieldAmount} shield to {fighter.Name}.");
    }
}
