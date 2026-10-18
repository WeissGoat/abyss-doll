using System.Collections.Generic;
using UnityEngine;

public abstract class FighterEntity {
    public string Name;
    public CombatFaction ParentFaction;
    public int RuntimeHP;
    public int RuntimeMaxHP;
    public int RuntimeShield;

    public int CurrentAP;
    public int MaxAP;
    // 中毒、破甲、迟缓 stacks for this fight (CombatStatusService).
    public readonly Dictionary<CombatStatusType, int> Statuses = new Dictionary<CombatStatusType, int>();

    // Flat reduction on every hit this fighter takes from an attack (CombatStatusService.ModifyDamage).
    public virtual int Armor => 0;

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
            CombatStatusService.TickTurnStart(this);
        }

        ProcessEffects(phase);
        if (phase == CombatEventType.OnTurnEnd) {
            CombatStatusService.TickTurnEnd(this);
        }
    }

    protected abstract void ProcessEffects(CombatEventType phase);

    // ignoreShield: the hit goes straight to HP and leaves the shield standing (a monster blowing itself up).
    public virtual void TakeDamage(int damage, bool ignoreShield = false) {
        if (damage <= 0) {
            return;
        }

        int originalDamage = damage;
        int hpBefore = RuntimeHP;
        int shieldBefore = RuntimeShield;

        if (RuntimeShield > 0 && !ignoreShield) {
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

        if (RuntimeHP < 0) {
            RuntimeHP = 0;
        }

        GameEventBus.PublishCombatBeat(new CombatBeat {
            Kind = CombatBeatKind.Hit,
            Target = this,
            Amount = originalDamage,
            HpLoss = hpBefore - RuntimeHP,
            ShieldLoss = shieldBefore - RuntimeShield,
            Defeated = RuntimeHP <= 0
        });
        GameEventBus.PublishShieldChanged(Name, RuntimeShield);
        GameEventBus.PublishHPChanged(Name, RuntimeHP, RuntimeMaxHP);

        Debug.Log($"[{Name}] Took {originalDamage} damage! Shield absorbed {originalDamage - damage}, HP reduced by {damage}. Current HP: {RuntimeHP}, Shield: {RuntimeShield}");

        if (RuntimeHP <= 0) {
            Debug.Log($"[{Name}] has been defeated!");
        }
    }

    public virtual void AddShield(int amount) {
        RuntimeShield += amount;
        if (amount > 0) {
            GameEventBus.PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.Shield, Target = this, Amount = amount });
        }
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
        if (actualHeal > 0) {
            GameEventBus.PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.Heal, Target = this, Amount = actualHeal });
        }
        GameEventBus.PublishHPChanged(Name, RuntimeHP, RuntimeMaxHP);
        Debug.Log($"[{Name}] Recovered {actualHeal} HP. Current HP: {RuntimeHP}/{RuntimeMaxHP}");
    }

    public abstract void Attack(FighterEntity target, ItemEntity weaponSource = null);
}
