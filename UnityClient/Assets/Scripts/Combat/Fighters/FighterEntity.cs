using UnityEngine;

public abstract class FighterEntity {
    public string Name;
    public CombatFaction ParentFaction;
    public int RuntimeHP;
    public int RuntimeMaxHP;
    public int RuntimeShield;

    public int CurrentAP;
    public int MaxAP;

    public FighterEntity() {
        CombatEventBus.OnCombatPhase += HandleCombatPhase;
    }

    public virtual void Cleanup() {
        CombatEventBus.OnCombatPhase -= HandleCombatPhase;
    }

    private void HandleCombatPhase(CombatEventType phase, CombatFaction activeFaction) {
        if (ParentFaction != activeFaction) {
            return;
        }

        if (phase == CombatEventType.OnTurnStart) {
            RuntimeShield = 0;
            GameEventBus.PublishShieldChanged(Name, RuntimeShield);
        }

        ProcessEffects(phase);
    }

    protected abstract void ProcessEffects(CombatEventType phase);

    public virtual void TakeDamage(int damage) {
        if (damage <= 0) {
            return;
        }

        int originalDamage = damage;

        if (RuntimeShield > 0) {
            if (RuntimeShield >= damage) {
                RuntimeShield -= damage;
                damage = 0;
            } else {
                damage -= RuntimeShield;
                RuntimeShield = 0;
            }
        }

        if (damage > 0) {
            RuntimeHP -= damage;
        }

        GameEventBus.PublishShieldChanged(Name, RuntimeShield);
        GameEventBus.PublishHPChanged(Name, RuntimeHP, RuntimeMaxHP);

        Debug.Log($"[{Name}] Took {originalDamage} damage! Shield absorbed {originalDamage - damage}, HP reduced by {damage}. Current HP: {RuntimeHP}, Shield: {RuntimeShield}");

        if (RuntimeHP <= 0) {
            RuntimeHP = 0;
            Debug.Log($"[{Name}] has been defeated!");
        }
    }

    public virtual void AddShield(int amount) {
        RuntimeShield += amount;
        GameEventBus.PublishShieldChanged(Name, RuntimeShield);
        Debug.Log($"[{Name}] Gained {amount} shield. Total Shield: {RuntimeShield}");
    }

    public virtual void Heal(int amount) {
        if (amount <= 0) {
            return;
        }

        int before = RuntimeHP;
        RuntimeHP = Mathf.Min(RuntimeHP + amount, RuntimeMaxHP);
        int actualHeal = RuntimeHP - before;
        GameEventBus.PublishHPChanged(Name, RuntimeHP, RuntimeMaxHP);
        Debug.Log($"[{Name}] Recovered {actualHeal} HP. Current HP: {RuntimeHP}/{RuntimeMaxHP}");
    }

    public abstract void Attack(FighterEntity target, ItemEntity weaponSource = null);
}
