using UnityEngine;

// Thorns: while Zero has shield up, a monster that hits her takes Params[0] damage back (once per hit).
public class ThornsEffect : EffectBase {
    private int _damage;

    public override void Init(EffectData data) {
        base.Init(data);
        _damage = Mathf.Max(0, Mathf.RoundToInt(data.Params != null && data.Params.Length > 0 ? data.Params[0] : 0f));
    }

    public override void OnOwnerHit(DollFighter owner, FighterEntity attacker, int shieldBefore, ItemEntity provider) {
        if (shieldBefore <= 0 || _damage <= 0 || attacker == null || attacker.RuntimeHP <= 0) {
            return;
        }

        GameEventBus.PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.Interfere, Actor = owner, Label = $"{provider?.Name} 反伤 {_damage}" });
        attacker.TakeDamage(_damage);
    }

    public override string Describe(ItemEntity provider, int rotation) {
        return $"有护盾时被攻击，反伤 {_damage}";
    }

    public override void ValidateConfig(ConfigValidationReport report, string ownerID, EffectData data) {
        if (data.Params == null || data.Params.Length == 0 || data.Params[0] < 1f) {
            report.AddError($"[{ownerID}] Thorns needs Params[0] >= 1 (damage back).");
        }
    }
}
