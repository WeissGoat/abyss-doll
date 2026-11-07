using UnityEngine;

// Empathy (八音盒, GDD_01 §5.2): while Zero is badly hurt (HP at or below Params[2] of her max) or deep in corruption
// (at least Params[3]), the items touching it deal and shield Params[0] more, plus Params[1] per bond level. Read at
// the moment of each strike or shield, so it switches on the instant she drops low.
public class BondResonanceEffect : EffectBase {
    private float _base;
    private float _perBond;
    private float _hpRatio;
    private float _corruption;

    public override void Init(EffectData data) {
        base.Init(data);
        float[] p = data.Params ?? new float[0];
        _base = p.Length > 0 ? p[0] : 0.3f;
        _perBond = p.Length > 1 ? p[1] : 0.1f;
        _hpRatio = p.Length > 2 ? p[2] : 0.4f;
        _corruption = p.Length > 3 ? p[3] : 60f;
    }

    private static DollEntity Doll => GameRoot.Core?.CurrentPlayer?.ActiveDoll;

    public bool Active() {
        DollEntity doll = Doll;
        if (doll?.Status == null) {
            return false;
        }

        int hp = doll.Status.HP_Current;
        CombatSystem combat = GameRoot.Core?.Combat;
        if (combat != null && combat.CurrentState != CombatState.End && combat.PlayerFaction?.Fighters?.Count > 0) {
            hp = combat.PlayerFaction.Fighters[0].RuntimeHP;
        }

        return (doll.Status.HP_Max > 0 && hp <= doll.Status.HP_Max * _hpRatio) || doll.Status.Corruption >= _corruption;
    }

    public float Bonus() {
        return _base + _perBond * Mathf.Max(0, Doll?.Bond?.AffectionLevel ?? 0);
    }

    public override float OutputBonus(ItemEntity provider, ItemEntity target, BackpackGrid grid) {
        if (target == provider || !Active()) {
            return 0f;
        }

        return GridSolver.Neighbors(provider, TargetDirection.AllAdjacent.ToString(), grid).Contains(target) ? Bonus() : 0f;
    }

    public override string Describe(ItemEntity provider, int rotation) {
        string state = Doll != null && Active() ? "（共鸣中）" : string.Empty;
        return $"零号 HP 不到 {Mathf.RoundToInt(_hpRatio * 100f)}% 或侵蚀 {Mathf.RoundToInt(_corruption)} 以上时，四周紧贴的物品伤害和护盾 +{Mathf.RoundToInt(Bonus() * 100f)}%（羁绊越深越强）{state}";
    }
}
