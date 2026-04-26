using UnityEngine;

public class MonsterFighter : FighterEntity {
    public MonsterEntity DataRef;
    
    public MonsterFighter(MonsterEntity monster, CombatFaction faction) : base() {
        ParentFaction = faction;
        DataRef = monster;
        Name = monster.Name;
        RuntimeHP = monster.HP;
        RuntimeMaxHP = monster.HP;
    }
    
    protected override void ProcessEffects(CombatEventType phase) {
        // MVP阶段怪物没有复杂的背包Effect，暂时留空
    }
    
    public override void Attack(FighterEntity target, ItemEntity weaponSource = null) {
        GameEventBus.PublishAttackAction(Name, target.Name, "普通攻击");
        GameEventBus.PublishDamageDealt(Name, target.Name, DataRef.DamageValue);
        
        Debug.Log($"[{Name}] attacks [{target.Name}] for {DataRef.DamageValue} damage!");
        target.TakeDamage(DataRef.DamageValue);
    }
}
