using System.Collections.Generic;
using System.Linq;
using UnityEngine;

// Plays Zero's side of a fight the way a careful player would (phase 2 M1, docs/specs/2026-11-04-fight-lab.md). Each
// turn it reads the intents on screen and tries every way to split the AP between striking and shielding: shielding
// saves HP now, striking ends the fight sooner and saves the hits still to come. It takes the split that loses the
// least HP. Strikes go to the enemy whose death removes the most incoming damage. The fight lab and the campaign bots
// both use it, so the numbers they report come from the same play.
public static class CombatBot {
    // How much of an average enemy turn still lands in later turns: the bot will shield then too.
    private const float FutureHitShare = 0.6f;

    public static void PlayTurn(CombatSystem combat, DollFighter player) {
        BackpackGrid grid = player?.DataRef?.RuntimeGrid as BackpackGrid;
        if (combat == null || grid == null || combat.CurrentState != CombatState.PlayerTurn) {
            return;
        }

        ItemEntity shield = BestShield(grid, player.CurrentAP);
        int shieldUses = shield == null ? 0 : ShieldUsesPlanned(combat, player, grid, shield);
        Strike(combat, player, grid, player.CurrentAP - shieldUses * (shield?.Combat.APCost ?? 0));

        // Re-check after the strikes: a kill may have removed the hit the shield was for, and then the AP strikes.
        int guard = 0;
        while (combat.CurrentState == CombatState.PlayerTurn && shield != null && guard++ < 6
            && player.CurrentAP >= shield.Combat.APCost && CombatItemEffects.CanUse(shield, out _)
            && Unblocked(combat) > 0) {
            player.Attack(player, shield);
        }

        Strike(combat, player, grid, player.CurrentAP);
        Vent(combat, player, grid);
    }

    // Heat into shield: with AP left and a hit coming, vent the overload (泄压阀).
    private static void Vent(CombatSystem combat, DollFighter player, BackpackGrid grid) {
        ItemEntity vent = grid.ContainedItems.FirstOrDefault(item => item?.Combat?.Effects != null
            && item.Combat.Effects.Exists(effect => effect?.EffectID == "VentOverload")
            && item.Combat.APCost <= player.CurrentAP && CombatItemEffects.CanUse(item, out _));
        if (vent != null && combat.CurrentState == CombatState.PlayerTurn && Unblocked(combat) > 0) {
            ItemUseService.TryUseItem(vent, out _);
        }
    }

    private static void Strike(CombatSystem combat, DollFighter player, BackpackGrid grid, int ap) {
        int guard = 0;
        while (combat.CurrentState == CombatState.PlayerTurn && guard++ < 12) {
            ItemEntity weapon = BestWeapon(grid, ap);
            FighterEntity target = PickTarget(combat, weapon, grid);
            if (weapon == null || target == null) {
                return;
            }

            int before = player.CurrentAP;
            player.Attack(target, weapon);
            if (player.CurrentAP == before) {
                return;
            }

            ap -= before - player.CurrentAP;
            combat.TryResolveVictoryAfterPlayerAction();
        }
    }

    public static bool IsWeapon(ItemEntity item) {
        return item?.Combat != null
            && item.ItemType == nameof(ItemType.Weapon)
            && item.Combat.TriggerType == TriggerType.Manual.ToString()
            && (item.Combat.DamageType == DamageType.Physical.ToString() || item.Combat.DamageType == DamageType.Energy.ToString());
    }

    // A trigger-chain start the bot will use: its line holds a strike and firing it now stays within the overload
    // capacity (the bot does not jam itself on purpose).
    public static bool IsChainStrike(ItemEntity item, BackpackGrid grid) {
        ChainState state = TriggerChain.State;
        return item?.Combat != null
            && item.Combat.TriggerType == TriggerType.Manual.ToString()
            && item.ItemType != nameof(ItemType.Consumable)
            && TriggerChain.HasOutPort(item)
            && state != null && !state.Jammed
            && state.Overload + TriggerChain.PlannedLoad(item, grid) <= state.Capacity
            && TriggerChain.ChainHasStrike(item, grid);
    }

    // Using a chain start always fires its line, and a hot item heats Zero; the bot skips a use that would push the
    // overload over capacity (it does not jam itself on purpose).
    public static bool WouldJam(ItemEntity item, BackpackGrid grid) {
        ChainState state = TriggerChain.State;
        return state != null && state.Overload + TriggerChain.UseLoad(item, grid) > state.Capacity;
    }

    // A use's worth: the item's own strike and, for a chain start within capacity, the strikes down its line.
    public static float StrikeValue(ItemEntity item, BackpackGrid grid) {
        float value = IsWeapon(item) ? EffectiveDamage(item) : 0f;
        if (grid != null && IsChainStrike(item, grid)) {
            foreach (ChainStep step in TriggerChain.Plan(item, grid)) {
                if (TriggerChain.IsStrike(step.Item) && TriggerChain.CanFire(step.Item, out _)) {
                    value += EffectiveDamage(step.Item) * (1f + step.Bonus);
                }
            }
        }

        return value;
    }

    // A strike's worth: its damage, plus poison as the HP it takes over the next turns and 破甲 as half a strike more.
    public static float EffectiveDamage(ItemEntity item) {
        if (item?.Combat == null) {
            return 0f;
        }

        float damage = (item.Combat.RuntimeDamage > 0f ? item.Combat.RuntimeDamage : item.Combat.BaseValue) + CombatItemEffects.StrikeBonus(item);
        foreach (EffectData effect in item.Combat.Effects ?? new List<EffectData>()) {
            float value = effect?.Params != null && effect.Params.Length > 0 ? effect.Params[0] : 0f;
            if (effect?.EffectID == "ApplyStatus" && effect.Trigger != "TurnStart") {
                if (effect.Status == "Poison") damage += value * 2f;
                if (effect.Status == "Vulnerable") damage += 8f * value;
            }
        }

        return damage;
    }

    public static ItemEntity BestWeapon(BackpackGrid grid, int ap) {
        return grid?.ContainedItems
            .Where(item => (IsWeapon(item) || IsChainStrike(item, grid)) && item.Combat.APCost <= ap && CombatItemEffects.CanUse(item, out _) && !WouldJam(item, grid))
            .OrderByDescending(item => StrikeValue(item, grid) / Mathf.Max(1f, item.Combat.APCost))
            .ThenByDescending(item => StrikeValue(item, grid))
            .FirstOrDefault();
    }

    // An enemy's average damage per turn over its whole move set (what to expect after the shown intent).
    public static float AverageHit(MonsterFighter monster) {
        List<MonsterActionConfig> actions = monster?.DataRef?.AI?.Actions;
        if (actions == null || actions.Count == 0) {
            return 0f;
        }

        float total = Mathf.Max(1, actions.Sum(action => action.Weight));
        float hit = 0f;
        foreach (MonsterActionConfig action in actions) {
            if (action.ActionType != "DamageTarget") {
                continue;
            }

            float damage = Param(action, "Damage") * Mathf.Max(1f, Param(action, "RepeatCount"));
            float share = action.Weight / total;
            if (action.CooldownTurns > 0) {
                share = Mathf.Min(share, 1f / (action.CooldownTurns + 1));
            }

            hit += share * damage;
        }

        return hit;
    }

    private static float Param(MonsterActionConfig action, string key) {
        return action.Params != null && action.Params.TryGetValue(key, out var token) && token != null ? (float)token : 0f;
    }

    private static ItemEntity BestShield(BackpackGrid grid, int ap) {
        return grid.ContainedItems
            .Where(item => item?.Combat != null
                && item.Combat.TriggerType == TriggerType.Manual.ToString()
                && item.Combat.DamageType == DamageType.Shield.ToString()
                && item.Combat.APCost <= ap
                && CombatItemEffects.CanUse(item, out _))
            .OrderByDescending(item => ShieldAmount(item) / Mathf.Max(1f, item.Combat.APCost))
            .FirstOrDefault();
    }

    private static float ShieldAmount(ItemEntity item) {
        return (item.Combat.Effects ?? new List<EffectData>())
            .Where(effect => effect?.EffectID == "AddRuntimeShield" && effect.Params != null && effect.Params.Length > 0)
            .Sum(effect => effect.Params[0] + effect.Level * 5f);
    }

    // Tries 0, 1, 2... shield uses and keeps the count that loses the least HP: this turn's unblocked hit from the
    // enemies left standing, plus their average hits over the turns it will still take to finish them.
    private static int ShieldUsesPlanned(CombatSystem combat, DollFighter player, BackpackGrid grid, ItemEntity shield) {
        List<EnemyView> enemies = Enemies(combat);
        if (enemies.Count == 0) {
            return 0;
        }

        ItemEntity mainWeapon = BestWeapon(grid, player.MaxAP);
        float perAP = mainWeapon == null ? 0f : StrikeValue(mainWeapon, grid) / Mathf.Max(1f, mainWeapon.Combat.APCost);
        float perTurn = Mathf.Max(1f, perAP * player.MaxAP);
        float shieldAmount = ShieldAmount(shield);
        int maxUses = player.CurrentAP / Mathf.Max(1, shield.Combat.APCost);

        int cheapestStrike = grid.ContainedItems.Where(IsWeapon).Select(item => item.Combat.APCost).DefaultIfEmpty(int.MaxValue).Min();

        int bestUses = 0;
        float bestLoss = float.MaxValue;
        float previousLossNow = float.MaxValue;
        for (int uses = 0; uses <= maxUses; uses++) {
            int strikeAP = player.CurrentAP - uses * shield.Combat.APCost;
            List<EnemyView> left = SimulateStrikes(enemies, grid, strikeAP);
            int incoming = left.Sum(enemy => enemy.Incoming);
            int unblockable = left.Sum(enemy => enemy.Unblockable);
            int blocked = Mathf.Min(incoming - unblockable, player.RuntimeShield + Mathf.RoundToInt(uses * shieldAmount));
            float lossNow = incoming - blocked;
            // A turn without a strike only to brace for a blow that would take a big piece of her: a fight is never
            // won by shielding, and a player does not wait out a monster that has nothing big coming.
            bool noStrike = strikeAP < cheapestStrike && cheapestStrike <= player.CurrentAP;
            if (noStrike && previousLossNow < player.RuntimeHP * 0.4f) {
                continue;
            }

            previousLossNow = lossNow;
            float hpLeft = left.Sum(enemy => enemy.HP);
            float futureTurns = hpLeft / perTurn;
            float futureLoss = left.Sum(enemy => enemy.AverageHit) * futureTurns * FutureHitShare;
            float loss = lossNow + futureLoss;
            if (loss < bestLoss - 0.01f) {
                bestLoss = loss;
                bestUses = uses;
            }
        }

        return bestUses;
    }

    private static List<EnemyView> SimulateStrikes(List<EnemyView> enemies, BackpackGrid grid, int ap) {
        List<EnemyView> left = enemies.Select(enemy => enemy.Copy()).ToList();
        int guard = 0;
        while (ap > 0 && left.Count > 0 && guard++ < 12) {
            ItemEntity weapon = BestWeapon(grid, ap);
            if (weapon == null) {
                break;
            }

            int damage = Mathf.RoundToInt(StrikeValue(weapon, grid));
            EnemyView target = Choose(left, damage);
            target.HP -= Mathf.Max(1, damage - target.Armor);
            if (target.HP <= 0) {
                left.Remove(target);
            }

            ap -= Mathf.Max(1, weapon.Combat.APCost);
        }

        return left;
    }

    private static FighterEntity PickTarget(CombatSystem combat, ItemEntity weapon, BackpackGrid grid) {
        List<EnemyView> enemies = Enemies(combat);
        if (enemies.Count == 0 || weapon == null) {
            return null;
        }

        return Choose(enemies, Mathf.RoundToInt(StrikeValue(weapon, grid))).Fighter;
    }

    // Finish an enemy this strike kills (the most dangerous first); otherwise hit the one whose hits per HP left are
    // the worst.
    private static EnemyView Choose(List<EnemyView> enemies, int damage) {
        EnemyView killable = enemies
            .Where(enemy => enemy.HP <= Mathf.Max(1, damage - enemy.Armor))
            .OrderByDescending(enemy => enemy.Incoming + enemy.AverageHit)
            .FirstOrDefault();
        return killable ?? enemies
            .OrderByDescending(enemy => (enemy.Incoming + enemy.AverageHit) / Mathf.Max(1f, enemy.HP))
            .First();
    }

    private static int Unblocked(CombatSystem combat) {
        IncomingDamagePreview preview = CombatReadabilityTextService.BuildIncomingDamagePreview(combat);
        return preview == null ? 0 : preview.HPLoss;
    }

    private static List<EnemyView> Enemies(CombatSystem combat) {
        List<EnemyView> enemies = new List<EnemyView>();
        MonsterIntentReport report = MonsterIntentPreviewService.BuildReport(combat);
        foreach (FighterEntity fighter in combat.EnemyFaction?.Fighters ?? new List<FighterEntity>()) {
            MonsterFighter monster = fighter as MonsterFighter;
            if (monster == null || monster.RuntimeHP <= 0) {
                continue;
            }

            MonsterActionIntentPreview intent = report?.Monsters?.FirstOrDefault(card => card.RuntimeID == monster.RuntimeID)?.SelectedIntent;
            bool attacks = intent != null && intent.CanExecute
                && (intent.IntentType == MonsterIntentType.Attack || intent.IntentType == MonsterIntentType.SelfDestruct);
            int incoming = attacks ? intent.EstimatedTotalDamage : 0;
            enemies.Add(new EnemyView {
                Fighter = monster,
                HP = monster.RuntimeHP + monster.RuntimeShield,
                Armor = monster.Armor,
                Incoming = incoming,
                Unblockable = attacks && intent.IntentType == MonsterIntentType.SelfDestruct ? incoming : 0,
                AverageHit = AverageHit(monster)
            });
        }

        return enemies;
    }

    private class EnemyView {
        public FighterEntity Fighter;
        public int HP;
        public int Armor;
        public int Incoming;
        public int Unblockable;
        public float AverageHit;

        public EnemyView Copy() {
            return (EnemyView)MemberwiseClone();
        }
    }
}
