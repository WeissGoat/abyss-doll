using System.Collections.Generic;
using UnityEngine;

// The monsters' new interference with the backpack (GDD_01 §6.2, docs/specs/2026-11-07-trigger-chains.md batch 3):
// crystal seals cells (the item on it cannot be used, its chain stops there, nothing can be put on it; it melts after
// the given turns or when the item is moved off), spores foul an item (SAN on every use, creeping into a neighbour each
// turn), and the solvent spray clears both. The monsters that carry them can roll them.
public static class MonsterInterferenceTest {
    private static readonly List<string> Failures = new List<string>();

    public static void Run() {
        Debug.Log("=== Running Monster Interference Test ===");
        Failures.Clear();
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        doll.Chassis = ConfigManager.Chassis["chassis_lv2_expanded"];

        // Crystal under the rivet driver: it cannot be used, the battery's line stops there, the cell takes nothing new.
        BackpackGrid grid = Fresh(doll);
        ItemEntity battery = Place(grid, "gear_spark_cell", 0, 1, 0);
        ItemEntity rivet = Place(grid, "gear_rivet_driver", 1, 1, 0);
        ItemEntity cannon = Place(grid, "gear_steam_cannon", 3, 0, 0);
        DollFighter zero = Fight(core);
        FighterEntity enemy = Sturdy(core.Combat.EnemyFaction.Fighters[0]);
        grid.SetCrystal(2, 1, 2);
        Check(grid.SealedTurns(rivet) == 2, $"rivet sealed for {grid.SealedTurns(rivet)} turns, expected 2");
        Check(!CombatItemEffects.CanUse(rivet, out string reason) && reason.Contains("结晶"), "a sealed item should not be usable: " + reason);
        Check(!CombatItemEffects.CanUse(battery, out _), "the battery's only line runs through the sealed rivet, so it has nothing to fire");
        Check(!TriggerChain.Plan(battery, grid).Exists(step => step.Item == cannon && TriggerChain.CanFire(step.Item, out _)) || TriggerChain.Workers(battery, grid) == 0,
            "the chain should stop at the sealed rivet");
        ItemEntity probe = ConfigManager.CreateItem("gear_spark_cell");
        grid.SetCrystal(4, 4, 2);
        Check(!grid.CanPlaceItem(probe, 4, 4, 0), "nothing can be put on crystal");
        core.Combat.EndPlayerTurn();
        VisualQueue.Clear();
        Check(grid.SealedTurns(rivet) == 1, $"after one turn the rivet should have 1 turn left, has {grid.SealedTurns(rivet)}");
        core.Combat.EndPlayerTurn();
        VisualQueue.Clear();
        Check(grid.SealedTurns(rivet) == 0 && grid.CrystalCells().Count == 0, "crystal should melt after its turns");

        // Moving the item off the crystal frees it; the crystal stays on the cell.
        grid.SetCrystal(1, 1, 3);
        grid.RemoveItem(rivet);
        Check(grid.PlaceItem(rivet, 1, 3, 0) && grid.SealedTurns(rivet) == 0 && grid.IsCrystal(1, 1), "moving an item off the crystal should free it");

        // The action seals cells under items that do something first.
        grid = Fresh(doll);
        ItemEntity dagger = Place(grid, "gear_rusty_dagger", 0, 0, 0);
        Place(grid, "gear_wooden_shield", 2, 0, 0);
        List<int[]> sealedCells = LockCellsAction.Lock(grid, 2, 2);
        Check(sealedCells.Count == 2 && grid.SealedTurns(dagger) > 0, $"lock action sealed {sealedCells.Count} cells, dagger sealed {grid.SealedTurns(dagger)}");

        // Spores: each use of the fouled dagger costs 2 SAN; at the next turn start they creep into the touching shield.
        grid = Fresh(doll);
        dagger = Place(grid, "gear_rusty_dagger", 0, 0, 0);
        ItemEntity shield = Place(grid, "gear_wooden_shield", 1, 0, 0);
        ItemEntity farShield = Place(grid, "gear_wooden_shield", 4, 3, 0);
        zero = Fight(core);
        enemy = Sturdy(core.Combat.EnemyFaction.Fighters[0]);
        doll.Status.SAN_Current = 40;
        core.Combat.Items.Pollute(dagger, 2);
        zero.Attack(enemy, dagger);
        Check(doll.Status.SAN_Current == 38, $"SAN after a fouled strike {doll.Status.SAN_Current}, expected 38");
        core.Combat.EndPlayerTurn();
        VisualQueue.Clear();
        Check(CombatItemEffects.Pollution(shield) == 2 && CombatItemEffects.Pollution(farShield) == 0, $"spores should creep into the touching shield only (shield {CombatItemEffects.Pollution(shield)}, far {CombatItemEffects.Pollution(farShield)})");

        // The solvent spray melts crystal and washes the spores off.
        ItemEntity spray = Place(grid, "con_solvent_spray", 4, 0, 0);
        grid.SetCrystal(3, 3, 2);
        zero.CurrentAP = zero.MaxAP;
        Check(ItemUseService.TryUseItem(spray, out reason), "solvent spray: " + reason);
        Check(grid.CrystalCells().Count == 0 && CombatItemEffects.Pollution(dagger) == 0 && CombatItemEffects.Pollution(shield) == 0, "the spray should clear crystal and spores");

        // The spore action fouls a manual item; a fight start clears everything.
        grid = Fresh(doll);
        dagger = Place(grid, "gear_rusty_dagger", 0, 0, 0);
        Check(SporePolluteAction.Pick(grid, false) == dagger, "the spore action should pick the dagger");
        Fight(core);
        Check(grid.CrystalCells().Count == 0 && core.Combat.Items.PollutedCount == 0, "a new fight should start clean");

        // The monsters carry the new actions.
        foreach (string id in new[] { "mob_crystal_guard", "elite_crystal_bulwark", "mob_spore_archer", "elite_spore_matriarch", "mob_mycelium_crawler" }) {
            bool has = ConfigManager.Monsters.TryGetValue(id, out MonsterEntity monster)
                && monster.AI.Actions.Exists(action => action.ActionType == "LockCells" || action.ActionType == "SporePollute");
            Check(has, $"{id} should carry crystal or spores");
        }

        core.Combat.CurrentState = CombatState.End;
        VisualQueue.Clear();
        if (Failures.Count > 0) {
            foreach (string failure in Failures) {
                Debug.LogError("[MonsterInterferenceTest] " + failure);
            }

            throw new System.Exception($"MonsterInterferenceTest: {Failures.Count} failure(s), first: {Failures[0]}");
        }

        Debug.Log("=== Monster Interference Test Passed ===");
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
            Failures.Add($"could not place {configID} at ({x},{y})");
        }

        return item;
    }

    private static DollFighter Fight(CoreBackend core) {
        GridSolver.RecalculateAllEffects(core.CurrentPlayer.ActiveDoll);
        core.Combat.StartCombat(new List<string> { "mob_lost_miner_echo" });
        DollFighter zero = (DollFighter)core.Combat.PlayerFaction.Fighters[0];
        zero.RuntimeMaxHP = 999;
        zero.RuntimeHP = 999;
        return zero;
    }

    private static FighterEntity Sturdy(FighterEntity enemy) {
        enemy.RuntimeMaxHP = 999;
        enemy.RuntimeHP = 999;
        return enemy;
    }
}
