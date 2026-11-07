using System.Collections.Generic;
using UnityEngine;

// Trigger chains (GDD_01 §5.1A, docs/specs/2026-11-07-trigger-chains.md): ports turn with the item, only port facing
// port links, a use fires the line once each in order, splitters branch, overload cools or jams Zero, a spent module
// cuts the line, a start with a weapon on its line asks for a target, and the heat items' numbers. Real fights on the
// level-2 chassis (5×5, overload capacity 5, cooling 2) with the chain items from config.
public static class TriggerChainTest {
    private static readonly List<string> Failures = new List<string>();

    public static void Run() {
        Debug.Log("=== Running Trigger Chain Test ===");
        Failures.Clear();
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        doll.Chassis = ConfigManager.Chassis["chassis_lv2_expanded"];

        // Ports turn with the item: a conduit stood up has its in port on top and its out port below.
        BackpackGrid grid = Fresh(doll);
        ItemEntity conduit = Place(grid, "gear_copper_conduit", 0, 0, 90);
        List<PlacedPort> ports = TriggerChain.Ports(conduit, grid);
        Check(ports.Count == 2, $"conduit ports {ports.Count}");
        Check(ports.Exists(p => !p.IsOut && p.X == 0 && p.Y == 0 && p.Side == TargetDirection.Up), "turned conduit: in port should be on top of (0,0)");
        Check(ports.Exists(p => p.IsOut && p.X == 0 && p.Y == 1 && p.Side == TargetDirection.Down), "turned conduit: out port should be below (0,1)");

        // Battery → rivet driver → steam cannon on row 1: one use fires both, 10 + 36, overload +5 (1 + 4), one AP.
        grid = Fresh(doll);
        ItemEntity battery = Place(grid, "gear_spark_cell", 0, 1, 0);
        ItemEntity rivet = Place(grid, "gear_rivet_driver", 1, 1, 0);
        ItemEntity cannon = Place(grid, "gear_steam_cannon", 3, 0, 0);
        List<ChainStep> plan = TriggerChain.Plan(battery, grid);
        Check(plan.Count == 2 && plan[0].Item == rivet && plan[1].Item == cannon, $"plan {Names(plan)}, expected rivet then cannon");
        Check(TriggerChain.PlannedLoad(battery, grid) == 5, $"planned load {TriggerChain.PlannedLoad(battery, grid)}");
        DollFighter zero = Fight(core);
        FighterEntity enemy = Sturdy(core.Combat.EnemyFaction.Fighters[0]);
        int ap = zero.CurrentAP;
        int hp = enemy.RuntimeHP;
        zero.Attack(enemy, battery);
        Check(hp - enemy.RuntimeHP == 46, $"chain dealt {hp - enemy.RuntimeHP}, expected 10 + 36");
        Check(zero.CurrentAP == ap - 1, $"AP {ap} -> {zero.CurrentAP}, expected one spent");
        Check(core.Combat.Chain.Overload == 5 && !core.Combat.Chain.JamNextTurn, $"overload {core.Combat.Chain.Overload}, expected 5 and no jam at capacity");
        Check(CombatItemEffects.Uses(cannon) == 1, "the cannon's use was not counted");

        // Touching is not linking: the cannon one row down touches the rivet driver, but no in port faces its out port.
        grid = Fresh(doll);
        battery = Place(grid, "gear_spark_cell", 0, 1, 0);
        Place(grid, "gear_rivet_driver", 1, 1, 0);
        Place(grid, "gear_steam_cannon", 3, 1, 0);
        Check(TriggerChain.Plan(battery, grid).Count == 1, $"the shifted cannon should not link: {Names(TriggerChain.Plan(battery, grid))}");

        // A splitter branches: battery → valve, up into a stood-up rivet driver, down into a stood-up shield projector.
        grid = Fresh(doll);
        battery = Place(grid, "gear_spark_cell", 0, 2, 0);
        Place(grid, "gear_splitter_valve", 1, 2, 0);
        rivet = Place(grid, "gear_rivet_driver", 1, 0, 270);
        ItemEntity projector = Place(grid, "gear_shield_projector", 1, 3, 90);
        plan = TriggerChain.Plan(battery, grid);
        Check(plan.Exists(step => step.Item == rivet) && plan.Exists(step => step.Item == projector), $"splitter plan {Names(plan)}");
        zero = Fight(core);
        enemy = Sturdy(core.Combat.EnemyFaction.Fighters[0]);
        hp = enemy.RuntimeHP;
        zero.Attack(enemy, battery);
        Check(hp - enemy.RuntimeHP == 10 && zero.RuntimeShield == 5, $"branches: damage {hp - enemy.RuntimeHP}, shield {zero.RuntimeShield}");
        Check(core.Combat.Chain.Overload == 2, $"branch overload {core.Combat.Chain.Overload}, expected 2 (the valve adds none)");

        // Overload: twice in a turn (5 + 5) overflows capacity 5, the next turn is jammed (no chain, one AP less, overload 0),
        // the turn after cools normally.
        grid = Fresh(doll);
        battery = Place(grid, "gear_spark_cell", 0, 1, 0);
        Place(grid, "gear_rivet_driver", 1, 1, 0);
        Place(grid, "gear_steam_cannon", 3, 0, 0);
        zero = Fight(core);
        enemy = Sturdy(core.Combat.EnemyFaction.Fighters[0]);
        Tough(zero);
        zero.Attack(enemy, battery);
        zero.Attack(enemy, battery);
        Check(core.Combat.Chain.Overload == 10 && core.Combat.Chain.JamNextTurn, $"two uses: overload {core.Combat.Chain.Overload}, jam pending {core.Combat.Chain.JamNextTurn}");
        NextTurn(core);
        Check(core.Combat.Chain.Jammed && core.Combat.Chain.Overload == 0, $"jammed turn: jammed {core.Combat.Chain.Jammed}, overload {core.Combat.Chain.Overload}");
        Check(zero.CurrentAP == zero.MaxAP - ChainState.JamAPPenalty, $"jammed AP {zero.CurrentAP}, expected {zero.MaxAP - ChainState.JamAPPenalty}");
        Check(!CombatItemEffects.CanUse(battery, out _), "the battery should not start a chain while jammed");
        NextTurn(core);
        Check(!core.Combat.Chain.Jammed && zero.CurrentAP == zero.MaxAP, $"turn after the jam: jammed {core.Combat.Chain.Jammed}, AP {zero.CurrentAP}");
        zero.Attack(enemy, battery);
        NextTurn(core);
        Check(core.Combat.Chain.Overload == 3, $"overload 5 should cool by 2 to 3, got {core.Combat.Chain.Overload}");

        // A module spent for this fight cuts the line behind it.
        grid = Fresh(doll);
        battery = Place(grid, "gear_spark_cell", 0, 1, 0);
        rivet = Place(grid, "gear_rivet_driver", 1, 1, 0);
        rivet.Combat.Effects.Add(new EffectData { EffectID = "UsesPerCombat", Params = new[] { 1f } });
        Place(grid, "gear_steam_cannon", 3, 0, 0);
        zero = Fight(core);
        enemy = Sturdy(core.Combat.EnemyFaction.Fighters[0]);
        zero.MaxAP = 10;
        zero.CurrentAP = 10;
        zero.Attack(enemy, battery);
        hp = enemy.RuntimeHP;
        int overload = core.Combat.Chain.Overload;
        Check(!CombatItemEffects.CanUse(battery, out _), "with the rivet driver spent nothing can fire, so the battery should be blocked");
        zero.Attack(enemy, battery);
        Check(enemy.RuntimeHP == hp && core.Combat.Chain.Overload == overload, $"spent line: damage {hp - enemy.RuntimeHP}, overload {overload} -> {core.Combat.Chain.Overload}");

        // A start with a weapon on its line asks for a target; with only a shield it is used at once.
        grid = Fresh(doll);
        battery = Place(grid, "gear_spark_cell", 0, 1, 0);
        Place(grid, "gear_rivet_driver", 1, 1, 0);
        Fight(core);
        Check(ItemUseService.TryUseItem(battery, out string reason) && ItemUseService.HasPendingEnemyTargetSelection, "battery with a weapon on its line should wait for a target: " + reason);
        ItemUseService.ClearPendingTargetSelection();
        grid = Fresh(doll);
        battery = Place(grid, "gear_spark_cell", 0, 1, 0);
        Place(grid, "gear_shield_projector", 1, 1, 0);
        zero = Fight(core);
        Check(ItemUseService.TryUseItem(battery, out reason) && !ItemUseService.HasPendingEnemyTargetSelection && zero.RuntimeShield == 5, $"battery with a shield: used {reason}, shield {zero.RuntimeShield}");

        // Amplifier: +30% to what comes after it (rivet 10 -> 13); a heat sink takes 2 off the chain's load.
        grid = Fresh(doll);
        battery = Place(grid, "gear_spark_cell", 0, 1, 0);
        Place(grid, "gear_amp_coil", 1, 1, 0);
        Place(grid, "gear_rivet_driver", 2, 1, 0);
        zero = Fight(core);
        enemy = Sturdy(core.Combat.EnemyFaction.Fighters[0]);
        hp = enemy.RuntimeHP;
        zero.Attack(enemy, battery);
        Check(hp - enemy.RuntimeHP == 13, $"amplified rivet {hp - enemy.RuntimeHP}, expected 13");
        grid = Fresh(doll);
        battery = Place(grid, "gear_spark_cell", 0, 1, 0);
        Place(grid, "gear_heat_sink", 1, 1, 0);
        Place(grid, "gear_rivet_driver", 3, 1, 0);
        zero = Fight(core);
        enemy = Sturdy(core.Combat.EnemyFaction.Fighters[0]);
        core.Combat.Chain.Overload = 3;
        zero.Attack(enemy, battery);
        Check(core.Combat.Chain.Overload == 2, $"heat sink chain: overload 3 -> {core.Combat.Chain.Overload}, expected 2");

        // Heat: the red-hot blade 16, +3 per point of overload, and each use heats Zero by 1; the vent valve clears it for 3 shield a point.
        grid = Fresh(doll);
        ItemEntity blade = Place(grid, "gear_redhot_blade", 0, 0, 0);
        ItemEntity vent = Place(grid, "gear_vent_valve", 0, 2, 0);
        zero = Fight(core);
        enemy = Sturdy(core.Combat.EnemyFaction.Fighters[0]);
        core.Combat.Chain.Overload = 3;
        hp = enemy.RuntimeHP;
        zero.Attack(enemy, blade);
        Check(hp - enemy.RuntimeHP == 25 && core.Combat.Chain.Overload == 4, $"red-hot blade at overload 3 dealt {hp - enemy.RuntimeHP} (expected 25), overload after {core.Combat.Chain.Overload} (expected 4)");
        core.Combat.Chain.Overload = 4;
        Check(ItemUseService.TryUseItem(vent, out reason), "vent valve: " + reason);
        Check(core.Combat.Chain.Overload == 0 && zero.RuntimeShield == 12, $"vent: overload {core.Combat.Chain.Overload}, shield {zero.RuntimeShield}");
        Check(!CombatItemEffects.CanUse(vent, out _), "the vent valve needs overload to vent");
        core.Combat.Chain.Overload = 5;
        zero.CurrentAP = 3;
        zero.Attack(enemy, blade);
        Check(core.Combat.Chain.JamNextTurn, "a hot blade use past capacity should jam Zero next turn");

        // The tooltip of a start shows what a use would set off.
        grid = Fresh(doll);
        battery = Place(grid, "gear_spark_cell", 0, 1, 0);
        Place(grid, "gear_rivet_driver", 1, 1, 0);
        Place(grid, "gear_steam_cannon", 3, 0, 0);
        Fight(core);
        core.Combat.Chain.Overload = 3;
        string body = ItemPresentationRules.BuildTooltip(battery).Body;
        Check(body.Contains("带动 2 件") && body.Contains("会卡壳"), "start tooltip: " + body);

        core.Combat.CurrentState = CombatState.End;
        VisualQueue.Clear();
        if (Failures.Count > 0) {
            foreach (string failure in Failures) {
                Debug.LogError("[TriggerChainTest] " + failure);
            }

            throw new System.Exception($"TriggerChainTest: {Failures.Count} failure(s), first: {Failures[0]}");
        }

        Debug.Log("=== Trigger Chain Test Passed ===");
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

    private static ItemEntity Place(BackpackGrid grid, string configID, int x, int y, int rotation) {
        ItemEntity item = ConfigManager.CreateItem(configID);
        if (!grid.PlaceItem(item, x, y, rotation)) {
            Failures.Add($"could not place {configID} at ({x},{y}) turned {rotation}");
        }

        return item;
    }

    private static DollFighter Fight(CoreBackend core) {
        GridSolver.RecalculateAllEffects(core.CurrentPlayer.ActiveDoll);
        core.Combat.StartCombat(new List<string> { "mob_lost_miner_echo" });
        return (DollFighter)core.Combat.PlayerFaction.Fighters[0];
    }

    private static FighterEntity Sturdy(FighterEntity enemy) {
        enemy.RuntimeMaxHP = 999;
        enemy.RuntimeHP = 999;
        return enemy;
    }

    private static void Tough(DollFighter zero) {
        zero.RuntimeMaxHP = 999;
        zero.RuntimeHP = 999;
    }

    private static void NextTurn(CoreBackend core) {
        core.Combat.EndPlayerTurn();
        VisualQueue.Clear();
    }

    private static string Names(List<ChainStep> plan) {
        List<string> names = new List<string>();
        foreach (ChainStep step in plan) {
            names.Add(step.Item?.ConfigID);
        }

        return string.Join(" → ", names);
    }
}
