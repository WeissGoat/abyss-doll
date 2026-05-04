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
        // 在构造时注册全局战斗阶段事件
        CombatEventBus.OnCombatPhase += HandleCombatPhase;
    }
    
    public virtual void Cleanup() {
        // 战斗结束时必须注销，防止内存泄漏
        CombatEventBus.OnCombatPhase -= HandleCombatPhase;
    }
    
    private void HandleCombatPhase(CombatEventType phase, CombatFaction activeFaction) {
        // 大部分阶段事件（如回合开始/结束）只对当前活跃阵营生效
        if (this.ParentFaction == activeFaction) {
            if (phase == CombatEventType.OnTurnStart) {
                RuntimeShield = 0; // 回合初清理护盾
            }
            // 回调：遍历自己身上的 Effect 去处理
            ProcessEffects(phase);
        }
    }
    
    protected abstract void ProcessEffects(CombatEventType phase);
    
    public virtual void TakeDamage(int damage) {
        if (damage <= 0) return;
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
        if (amount <= 0) return;

        int before = RuntimeHP;
        RuntimeHP = Mathf.Min(RuntimeHP + amount, RuntimeMaxHP);
        int actualHeal = RuntimeHP - before;
        GameEventBus.PublishHPChanged(Name, RuntimeHP, RuntimeMaxHP);
        Debug.Log($"[{Name}] Recovered {actualHeal} HP. Current HP: {RuntimeHP}/{RuntimeMaxHP}");
    }
    
    public abstract void Attack(FighterEntity target, ItemEntity weaponSource = null);
}
