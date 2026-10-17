using System.Collections.Generic;
using UnityEngine;

// The content primitives (docs/specs/2026-10-17-content-expansion.md, step 1): statuses on enemies (poison, 破甲, 迟缓),
// thorns, AP back, charging a neighbour, growing with tagged neighbours, uses per fight, a reward on a kill and a cost
// paid by Zero. Each runs in a real fight with stand-in items built on the tactical blade (a 1×3 weapon).
public static class CombatPrimitivesTest {
    private static readonly List<string> Failures = new List<string>();

    public static void Run() {
        Debug.Log("=== Running Combat Primitives Test ===");
        Failures.Clear();
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        DollEntity doll = core.CurrentPlayer.ActiveDoll;

        // Poison: three stacks on the struck enemy; at its turn start it loses 3 HP and one stack fades.
        BackpackGrid grid = Fresh(doll);
        ItemEntity venom = Weapon(grid, 0, Tags("Toxic"), Effect("ApplyStatus", status: "Poison", value: 3));
        DollFighter zero = Fight(core, "mob_lost_miner_echo");
        FighterEntity enemy = core.Combat.EnemyFaction.Fighters[0];
        zero.Attack(enemy, venom);
        Check(CombatStatusService.Get(enemy, CombatStatusType.Poison) == 3, $"poison stacks {CombatStatusService.Get(enemy, CombatStatusType.Poison)}, expected 3");
        int hpBefore = enemy.RuntimeHP;
        core.Combat.EndPlayerTurn();
        Check(enemy.RuntimeHP == hpBefore - 3 && CombatStatusService.Get(enemy, CombatStatusType.Poison) == 2, $"poison tick: HP {hpBefore}->{enemy.RuntimeHP}, stacks {CombatStatusService.Get(enemy, CombatStatusType.Poison)}");
        Check(CombatStatusService.Describe(enemy) == "中毒 2", "status plate: " + CombatStatusService.Describe(enemy));

        // 破甲: the next strike lands ×1.5.
        grid = Fresh(doll);
        ItemEntity breaker = Weapon(grid, 0, Tags(), Effect("ApplyStatus", status: "Vulnerable", value: 1));
        ItemEntity plain = Weapon(grid, 1, Tags());
        zero = Fight(core, "mob_lost_miner_echo");
        enemy = Sturdy(core.Combat.EnemyFaction.Fighters[0]);
        zero.MaxAP = 10;
        zero.CurrentAP = 10;
        zero.Attack(enemy, breaker);
        hpBefore = enemy.RuntimeHP;
        zero.Attack(enemy, plain);
        Check(hpBefore - enemy.RuntimeHP == Mathf.RoundToInt(plain.Combat.RuntimeDamage * CombatStatusService.VulnerableMultiplier), $"vulnerable strike took {hpBefore - enemy.RuntimeHP}");

        // 迟缓: the monster's hits and its intent number drop to ×0.75.
        zero = Fight(core, "mob_lost_miner_echo");
        enemy = core.Combat.EnemyFaction.Fighters[0];
        int intentBefore = IntentDamage(core, (MonsterFighter)enemy);
        CombatStatusService.Apply(enemy, CombatStatusType.Weak, 1);
        int intentAfter = IntentDamage(core, (MonsterFighter)enemy);
        Check(intentBefore > 0 && intentAfter == Mathf.Max(1, Mathf.RoundToInt(intentBefore * CombatStatusService.WeakMultiplier)), $"weak intent {intentBefore} -> {intentAfter}");

        // Thorns: hit while shielded, the attacker takes damage back; unshielded, nothing.
        grid = Fresh(doll);
        Weapon(grid, 0, Tags("Defense"), Effect("Thorns", value: 4));
        zero = Fight(core, "mob_lost_miner_echo");
        MonsterFighter monster = (MonsterFighter)core.Combat.EnemyFaction.Fighters[0];
        zero.AddShield(10);
        hpBefore = monster.RuntimeHP;
        monster.DealActionDamage(zero, 3, "test");
        Check(monster.RuntimeHP == hpBefore - 4, $"thorns took {hpBefore - monster.RuntimeHP}, expected 4");
        zero.RuntimeShield = 0;
        hpBefore = monster.RuntimeHP;
        monster.DealActionDamage(zero, 3, "test");
        Check(monster.RuntimeHP == hpBefore, "thorns fired without a shield");

        // GainAP: on use, and when a heavy neighbour is used (the counterweight).
        grid = Fresh(doll);
        ItemEntity quick = Weapon(grid, 0, Tags(), Effect("GainAP", value: 1));
        ItemEntity hammer = Weapon(grid, 1, Tags("Heavy"));
        Weapon(grid, 2, Tags(), Effect("GainAP", value: 1, trigger: CombatItemEffects.NeighborUseTrigger, tag: "Heavy", target: "Left"));
        zero = Fight(core, "mob_lost_miner_echo");
        enemy = core.Combat.EnemyFaction.Fighters[0];
        zero.MaxAP = 10;
        zero.CurrentAP = 6;
        zero.Attack(enemy, quick);
        Check(zero.CurrentAP == 6 - quick.Combat.APCost + 1, $"AP after a GainAP strike {zero.CurrentAP}");
        int apBefore = zero.CurrentAP;
        zero.Attack(enemy, hammer);
        Check(zero.CurrentAP == apBefore - hammer.Combat.APCost + 1, $"AP after the heavy neighbour {zero.CurrentAP}");

        // ChargeNeighbor: the mechanical neighbour's next strike +50%, spent on that strike.
        grid = Fresh(doll);
        ItemEntity spring = Weapon(grid, 0, Tags(), Effect("ChargeNeighbor", value: 0.5f, tag: "Mechanical"));
        ItemEntity gearArm = Weapon(grid, 1, Tags("Mechanical"));
        zero = Fight(core, "mob_lost_miner_echo");
        enemy = Sturdy(core.Combat.EnemyFaction.Fighters[0]);
        zero.MaxAP = 10;
        zero.CurrentAP = 10;
        zero.Attack(enemy, spring);
        Check(Mathf.Approximately(CombatItemEffects.Charge(gearArm), 0.5f), $"charge {CombatItemEffects.Charge(gearArm)}");
        hpBefore = enemy.RuntimeHP;
        zero.Attack(enemy, gearArm);
        Check(hpBefore - enemy.RuntimeHP == Mathf.RoundToInt(gearArm.Combat.RuntimeDamage * 1.5f) && CombatItemEffects.Charge(gearArm) == 0f, $"charged strike took {hpBefore - enemy.RuntimeHP}");

        // PerAdjacentTag: +3 damage for each mechanical neighbour.
        grid = Fresh(doll);
        Weapon(grid, 0, Tags("Mechanical"));
        ItemEntity hub = Weapon(grid, 1, Tags(), Effect("PerAdjacentTag", value: 3f, tag: "Mechanical", target: "Self"));
        Weapon(grid, 2, Tags("Mechanical"));
        GridSolver.RecalculateAllEffects(doll);
        Check(Mathf.Approximately(hub.Combat.RuntimeDamage, hub.Combat.BaseValue + 6f), $"per-neighbour damage {hub.Combat.RuntimeDamage}, base {hub.Combat.BaseValue}");

        // UsesPerCombat: two uses, then held back; a new fight starts over.
        grid = Fresh(doll);
        ItemEntity knives = Weapon(grid, 0, Tags("Throwing"), Effect("UsesPerCombat", value: 2f));
        zero = Fight(core, "mob_lost_miner_echo");
        enemy = core.Combat.EnemyFaction.Fighters[0];
        zero.MaxAP = 10;
        zero.CurrentAP = 10;
        zero.Attack(enemy, knives);
        zero.Attack(enemy, knives);
        Check(!CombatItemEffects.CanUse(knives, out string spent) && spent.Contains("用完"), "a third throw was allowed: " + spent);
        hpBefore = enemy.RuntimeHP;
        int apLeft = zero.CurrentAP;
        zero.Attack(enemy, knives);
        Check(enemy.RuntimeHP == hpBefore && zero.CurrentAP == apLeft, "a spent item still struck or took AP");
        Check(ItemPresentationRules.BuildTooltip(knives).Body.Contains("还剩 0 次"), "tooltip does not show the uses left: " + ItemPresentationRules.BuildTooltip(knives).Body);
        Fight(core, "mob_lost_miner_echo");
        Check(CombatItemEffects.CanUse(knives, out _), "uses did not start over in a new fight");

        // OnKill: finishing an enemy heals Zero.
        grid = Fresh(doll);
        ItemEntity reaper = Weapon(grid, 0, Tags(), Effect("OnKill", value: 5f, resource: "HP"));
        zero = Fight(core, "mob_lost_miner_echo");
        enemy = core.Combat.EnemyFaction.Fighters[0];
        zero.RuntimeHP = 50;
        enemy.RuntimeHP = 1;
        zero.Attack(enemy, reaper);
        Check(zero.RuntimeHP == 55, $"HP after the kill {zero.RuntimeHP}, expected 55");

        // SelfCost: SAN paid per strike, a smudge brought home.
        grid = Fresh(doll);
        ItemEntity whisper = Weapon(grid, 0, Tags("Cursed"), Effect("SelfCost", value: 3f, resource: "SAN"), Effect("SelfCost", value: 1f, resource: "Grime"));
        zero = Fight(core, "mob_lost_miner_echo");
        enemy = core.Combat.EnemyFaction.Fighters[0];
        doll.Status.SAN_Current = 20;
        doll.Status.Grime = 0;
        zero.Attack(enemy, whisper);
        Check(doll.Status.SAN_Current == 17 && doll.Status.Grime == 1, $"self cost left SAN {doll.Status.SAN_Current}, grime {doll.Status.Grime}");

        // Every primitive describes itself in the tooltip.
        foreach (ItemEntity item in new[] { venom, breaker, quick, spring, hub, knives, reaper, whisper }) {
            Check(ItemPresentationRules.BuildTooltip(item).Body.Split('\n').Length >= 2, $"{item.Name} tooltip has no effect line: {ItemPresentationRules.BuildTooltip(item).Body}");
        }

        if (Failures.Count == 0) {
            Debug.Log("Combat Primitives PASSED.");
        } else {
            Debug.LogError("Combat Primitives FAILED: " + string.Join("; ", Failures));
        }

        VisualQueue.Clear();
        Debug.Log("=== Combat Primitives Test Finished ===");
    }

    private static void Check(bool condition, string message) {
        if (!condition) {
            Failures.Add(message);
        }
    }

    private static BackpackGrid Fresh(DollEntity doll) {
        doll.RuntimeGrid = new BackpackGrid(doll.Chassis);
        return (BackpackGrid)doll.RuntimeGrid;
    }

    private static DollFighter Fight(CoreBackend core, string monsterID) {
        GridSolver.RecalculateAllEffects(core.CurrentPlayer.ActiveDoll);
        core.Combat.StartCombat(new List<string> { monsterID });
        return (DollFighter)core.Combat.PlayerFaction.Fighters[0];
    }

    // A 1×3 tactical blade standing in column x, with the given tags and effects.
    private static ItemEntity Weapon(BackpackGrid grid, int x, List<string> tags, params EffectData[] effects) {
        ItemEntity item = ConfigManager.CreateItem("gear_tactical_blade");
        item.InstanceID = "primitive_" + x + "_" + System.Guid.NewGuid().ToString("N").Substring(0, 6);
        item.Tags = tags;
        item.Combat.Effects = new List<EffectData>(effects);
        grid.PlaceItem(item, x, 0, 0);
        return item;
    }

    // Enough HP that a strike is never cut short by the enemy falling.
    private static FighterEntity Sturdy(FighterEntity enemy) {
        enemy.RuntimeMaxHP = 999;
        enemy.RuntimeHP = 999;
        return enemy;
    }

    private static List<string> Tags(params string[] tags) {
        return new List<string>(tags);
    }

    private static EffectData Effect(string id, float value, string status = null, string tag = null, string target = null, string trigger = null, string resource = null) {
        return new EffectData { EffectID = id, Level = 0, Params = new[] { value }, Status = status, Tag = tag, Target = target, Trigger = trigger, Resource = resource };
    }

    private static int IntentDamage(CoreBackend core, MonsterFighter monster) {
        MonsterActionContext context = core.Combat.CreateMonsterActionContextForPreview(monster);
        foreach (MonsterActionConfig action in monster.DataRef.AI?.Actions ?? new List<MonsterActionConfig>()) {
            MonsterActionIntentPreview preview = MonsterIntentPreviewService.BuildActionPreview(monster, context, action);
            if (preview.IntentType == MonsterIntentType.Attack) {
                return preview.DamagePerHit;
            }
        }

        return 0;
    }
}
