using UnityEngine;

public class RestoreSANOnCombatEndEffect : EffectBase {
    private int _restoreAmount;

    public override CombatEventType ListenEvent => CombatEventType.OnCombatEnd;

    public override void Init(EffectData data) {
        base.Init(data);
        float baseAmount = (data.Params != null && data.Params.Length > 0) ? data.Params[0] : 0f;
        _restoreAmount = Mathf.Max(0, Mathf.RoundToInt(baseAmount));
    }

    public override void ApplyToFighter(FighterEntity fighter, ItemEntity provider) {
        DollFighter dollFighter = fighter as DollFighter;
        DollEntity doll = dollFighter?.DataRef;
        if (doll == null || _restoreAmount <= 0) {
            return;
        }

        int before = doll.Status.SAN_Current;
        doll.Status.SAN_Current = Mathf.Min(doll.Status.SAN_Current + _restoreAmount, doll.Status.SAN_Max);
        int restored = doll.Status.SAN_Current - before;
        GameEventBus.PublishSANChanged(doll.Name, doll.Status.SAN_Current, doll.Status.SAN_Max);
        Debug.Log($"[Effect] RestoreSANOnCombatEnd restored {restored} SAN for {doll.Name}.");
    }
}
