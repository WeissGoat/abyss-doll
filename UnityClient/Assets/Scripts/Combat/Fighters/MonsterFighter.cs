using UnityEngine;

public class MonsterFighter : FighterEntity {
    public MonsterEntity DataRef;
    public string RuntimeID { get; private set; }

    public MonsterFighter(MonsterEntity monster, CombatFaction faction) : base() {
        ParentFaction = faction;
        DataRef = monster;
        RuntimeID = System.Guid.NewGuid().ToString();
        Name = monster.Name;
        RuntimeHP = monster.HP;
        RuntimeMaxHP = monster.HP;
    }

    protected override void ProcessEffects(CombatEventType phase) {
        // Monster turn logic is owned by MonsterActionAI; per-phase monster effects can be added later.
    }

    public override void Attack(FighterEntity target, ItemEntity weaponSource = null) {
        Debug.LogWarning($"[MonsterFighter] Direct Attack() is disabled. Monster behavior must use MonsterActionRunner. Monster=[{Name}]");
    }

    public void DealActionDamage(FighterEntity target, int damage, string actionID) {
        if (target == null || damage <= 0) {
            return;
        }

        string actionName = string.IsNullOrEmpty(actionID) ? "MonsterAction" : actionID;
        GameEventBus.PublishAttackAction(Name, target.Name, actionName);
        GameEventBus.PublishDamageDealt(Name, target.Name, damage);

        Debug.Log($"[{Name}] uses [{actionName}] on [{target.Name}] for {damage} damage.");
        target.TakeDamage(damage);
    }
}
