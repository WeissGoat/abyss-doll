using UnityEngine;
using System.Collections.Generic;

public class DollFighter : FighterEntity {
    public DollEntity DataRef;
    
    public DollFighter(DollEntity doll, CombatFaction faction) : base() {
        ParentFaction = faction;
        DataRef = doll;
        Name = doll.Name;
        RuntimeHP = doll.Status.HP_Current > 0 ? doll.Status.HP_Current : doll.Status.HP_Max;
        RuntimeMaxHP = doll.Status.HP_Max;
        MaxAP = doll.Stats.MaxAP;
    }
    
    protected override void ProcessEffects(CombatEventType phase) {
        if (phase == CombatEventType.OnTurnStart) {
            CurrentAP = Mathf.Min(CurrentAP + DataRef.Stats.APRegen, MaxAP);
            Debug.Log($"[{Name}] Turn Started. AP restored to {CurrentAP}/{MaxAP}.");
            GridSolver.RecalculateAllEffects(DataRef); // 重新计算网格连结
        }

        if (DataRef.RuntimeGrid == null) return;
        BackpackGrid grid = DataRef.RuntimeGrid as BackpackGrid;
        if (grid == null) return;

        // 遍历包内物品，触发匹配阶段的 Effect
        foreach (var item in grid.ContainedItems) {
            if (item.Combat != null && item.Combat.Effects != null) {
                foreach (var effectData in item.Combat.Effects) {
                    EffectBase effect = EffectFactory.CreateEffect(effectData);
                    if (effect != null && effect.ListenEvent == phase) {
                        effect.ApplyToFighter(this, item);
                    }
                }
            }
        }
        
        // 遍历局外义体
        foreach (string prosID in DataRef.EquippedProsthetics) {
            if (ConfigManager.Prosthetics.TryGetValue(prosID, out var prosConfig)) {
                foreach (var effectData in prosConfig.PassiveEffects) {
                    EffectBase effect = EffectFactory.CreateEffect(effectData);
                    if (effect != null && effect.ListenEvent == phase) {
                        effect.ApplyToFighter(this, null);
                    }
                }
            }
        }
    }
    
    public void SyncDataBack() {
        DataRef.Status.HP_Current = RuntimeHP;
        Debug.Log($"[{Name}] Synced data back to DollEntity. HP: {DataRef.Status.HP_Current}");
    }
    
    public override void Attack(FighterEntity target, ItemEntity weaponSource) {
        if (weaponSource == null || weaponSource.Combat == null) return;
        
        if (CurrentAP < weaponSource.Combat.APCost) {
            Debug.LogWarning($"[{Name}] Not enough AP to use {weaponSource.Name}. Cost: {weaponSource.Combat.APCost}, Current: {CurrentAP}");
            return;
        }
        
        CurrentAP -= weaponSource.Combat.APCost;
        GameEventBus.PublishAPChanged(Name, CurrentAP, MaxAP);
        
        if (weaponSource.Combat.DamageType == DamageType.Physical.ToString() || weaponSource.Combat.DamageType == DamageType.Energy.ToString()) {
            int dmg = (int)weaponSource.Combat.RuntimeDamage;
            GameEventBus.PublishAttackAction(Name, target.Name, weaponSource.Name);
            GameEventBus.PublishDamageDealt(Name, target.Name, dmg);
            Debug.Log($"[{Name}] attacks [{target.Name}] with {weaponSource.Name} for {dmg} damage! (AP Left: {CurrentAP})");
            target.TakeDamage(dmg);
        }
        else if (weaponSource.Combat.DamageType == DamageType.Shield.ToString()) {
            GameEventBus.PublishAttackAction(Name, Name, weaponSource.Name);
            foreach (var effectData in weaponSource.Combat.Effects) {
                EffectBase effect = EffectFactory.CreateEffect(effectData);
                if (effect != null) {
                    effect.ApplyToFighter(this, weaponSource);
                }
            }
        }
    }
}
