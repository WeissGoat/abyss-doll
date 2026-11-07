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
            // Trigger chains (GDD_01 §5.1A): the overload cools, or a jam from last turn takes hold and costs an AP.
            ChainState chain = TriggerChain.State;
            if (chain != null) {
                chain.OnTurnStart();
                if (chain.Jammed) {
                    CurrentAP = Mathf.Max(0, CurrentAP - ChainState.JamAPPenalty);
                    GameEventBus.PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.Interfere, Actor = this, Label = $"卡壳：这回合链路不通，AP -{ChainState.JamAPPenalty}" });
                }

                GameEventBus.PublishOverloadChanged(chain.Overload, chain.Capacity, chain.Jammed);
            }
            // 精神好: cared for and talked to last night, the dive's first turn gets one more (it may pass the cap).
            if (HomeEveningService.TakeSpirit()) {
                CurrentAP += HomeEveningService.SpiritAP;
                GameEventBus.PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.Interfere, Actor = this, Label = $"零号精神很好：AP +{HomeEveningService.SpiritAP}" });
            }
            Debug.Log($"[{Name}] Turn Started. AP restored to {CurrentAP}/{MaxAP}.");
            // Spore pollution creeps on (GDD_01 §6.2).
            CombatItemEffects.SpreadPollution(this);
            GridSolver.RecalculateAllEffects(DataRef); // 重新计算网格连结
            
            // 【关键修复】回合开始时，必须主动向 VisualQueue 和 UI 广播 AP 恢复事件
            GameEventBus.PublishAPChanged(Name, CurrentAP, MaxAP);
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
                if (prosConfig.Effects == null) {
                    continue;
                }

                foreach (var effectData in prosConfig.Effects) {
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

    public override void TakeDamage(int damage, bool ignoreShield = false) {
        // Bond 3: once a dive, a lethal blow leaves her standing at 1 HP.
        int absorbed = ignoreShield ? 0 : RuntimeShield;
        if (damage > 0 && RuntimeHP > 1 && damage >= absorbed + RuntimeHP && HomeLoop.TryHoldOn(DataRef)) {
            damage = absorbed + RuntimeHP - 1;
            GameEventBus.PublishCombatBeat(new CombatBeat { Kind = CombatBeatKind.Interfere, Actor = this, Label = "零号咬牙撑住了", Pose = CombatPose.Kneel });
        }

        int hpBefore = RuntimeHP;
        base.TakeDamage(damage, ignoreShield);
        DollStrainService.AddWearFromHPLoss(DataRef, hpBefore - RuntimeHP);
        SyncRuntimeHPToDoll();
    }

    public override void Heal(int amount) {
        base.Heal(amount);
        SyncRuntimeHPToDoll();
    }

    public bool TrySpendAP(int amount, string sourceName) {
        if (amount <= 0) {
            return true;
        }

        if (CurrentAP < amount) {
            Debug.LogWarning($"[{Name}] Not enough AP to use {sourceName}. Cost: {amount}, Current: {CurrentAP}");
            return false;
        }

        CurrentAP -= amount;
        GameEventBus.PublishAPChanged(Name, CurrentAP, MaxAP);
        return true;
    }
    
    public override void Attack(FighterEntity target, ItemEntity weaponSource) {
        if (weaponSource == null || weaponSource.Combat == null) return;

        if (!CombatItemEffects.CanUse(weaponSource, out string blocked)) {
            Debug.LogWarning($"[{Name}] Cannot use {weaponSource.Name}: {blocked}");
            return;
        }

        if (!TrySpendAP(weaponSource.Combat.APCost, weaponSource.Name)) {
            return;
        }

        ItemUseOutcome outcome = new ItemUseOutcome { User = this, Item = weaponSource, Target = target };
        Strike(target, weaponSource, outcome, 1f);
        CombatItemEffects.AfterUse(outcome);
    }

    // The item's payload without the AP: a strike on the target, or a shield on herself. A trigger chain fires modules
    // this way, with `scale` from the amplifiers before them; a use by hand comes through Attack.
    public void Strike(FighterEntity target, ItemEntity weaponSource, ItemUseOutcome outcome, float scale) {
        CombatItemEffects.OutputScale = scale;
        try {
            StrikeUnscaled(target, weaponSource, outcome);
        } finally {
            CombatItemEffects.OutputScale = 1f;
        }
    }

    private void StrikeUnscaled(FighterEntity target, ItemEntity weaponSource, ItemUseOutcome outcome) {
        if (weaponSource.Combat.DamageType == DamageType.Physical.ToString() || weaponSource.Combat.DamageType == DamageType.Energy.ToString()) {
            if (target == null) {
                return;
            }

            // A neighbour's charge raises this strike once; 迟缓 on Zero and 破甲 on the target change it too.
            int dmg = CombatStatusService.ModifyDamage(this, target, CombatItemEffects.StrikeDamage(weaponSource));
            string pose = weaponSource.ConfigID == ItemUseService.UnarmedStrikeConfigID ? CombatPose.Punch : null;
            GameEventBus.PublishAttackAction(this, target, weaponSource.Name, weaponSource.IconID, pose, weaponSource);
            GameEventBus.PublishDamageDealt(Name, target.Name, dmg);
            Debug.Log($"[{Name}] attacks [{target.Name}] with {weaponSource.Name} for {dmg} damage! (AP Left: {CurrentAP})");
            int targetHPBefore = target.RuntimeHP;
            int targetShieldBefore = target.RuntimeShield;
            target.TakeDamage(dmg);
            GameRoot.Core?.Combat?.Timeline?.RecordDamage(
                ParentFaction != null ? ParentFaction.Type : FactionType.Player,
                Name,
                target.Name,
                weaponSource.Name,
                dmg,
                targetHPBefore,
                target.RuntimeHP,
                targetShieldBefore,
                target.RuntimeShield);
            outcome.Damage = dmg;
            outcome.Killed = targetHPBefore > 0 && target.RuntimeHP <= 0;
        }
        else if (weaponSource.Combat.DamageType == DamageType.Shield.ToString()) {
            GameEventBus.PublishAttackAction(this, this, weaponSource.Name, weaponSource.IconID, null, weaponSource);
            foreach (var effectData in weaponSource.Combat.Effects) {
                EffectBase effect = EffectFactory.CreateEffect(effectData);
                // Turn-start effects on a shield wait for the turn start.
                if (effect != null && effect.ListenEvent == CombatEventType.None) {
                    effect.ApplyToFighter(this, weaponSource);
                }
            }
        }
    }

    private void SyncRuntimeHPToDoll() {
        if (DataRef != null) {
            DataRef.Status.HP_Current = RuntimeHP;
        }
    }
}
