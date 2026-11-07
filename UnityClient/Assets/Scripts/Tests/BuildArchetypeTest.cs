using System.Collections.Generic;
using UnityEngine;

// The special builds (GDD_01 §5.2, docs/specs/2026-11-07-trigger-chains.md batch 2): the trash cannon burns the
// cheapest loot for damage by its cells and cannot fire with nothing to burn; the lonely blade grows with empty cells;
// the music box lifts its neighbours' damage and shields only while Zero is low or corrupted, more with a deeper bond.
public static class BuildArchetypeTest {
    private static readonly List<string> Failures = new List<string>();

    public static void Run() {
        Debug.Log("=== Running Build Archetype Test ===");
        Failures.Clear();
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        doll.Chassis = ConfigManager.Chassis["chassis_lv2_expanded"];

        // Trash cannon: burns the cheapest loot (the 1-cell scrap before the dearer coil), 6 + 9 per cell.
        BackpackGrid grid = Fresh(doll);
        ItemEntity cannon = Place(grid, "gear_scrap_cannon", 0, 0, 0);
        ItemEntity scrap = Place(grid, "loot_gear_scrap", 3, 0, 0);
        ItemEntity coil = Place(grid, "loot_rusty_coil", 3, 2, 0);
        bool scrapIsCheaper = scrap.BaseValue <= coil.BaseValue;
        ItemEntity expected = scrapIsCheaper ? scrap : coil;
        int expectedDamage = 6 + 9 * expected.Grid.Shape.Length;
        DollFighter zero = Fight(core);
        FighterEntity enemy = Sturdy(core.Combat.EnemyFaction.Fighters[0]);
        int hp = enemy.RuntimeHP;
        zero.Attack(enemy, cannon);
        Check(hp - enemy.RuntimeHP == expectedDamage, $"scrap cannon dealt {hp - enemy.RuntimeHP}, expected {expectedDamage} burning {expected.ConfigID}");
        Check(!grid.ContainedItems.Contains(expected) && grid.ContainedItems.Contains(scrapIsCheaper ? coil : scrap), "the cannon should burn the cheapest loot and only that");
        grid.RemoveItem(scrapIsCheaper ? coil : scrap);
        Check(!CombatItemEffects.CanUse(cannon, out _), "the cannon should not fire with nothing to burn");

        // Lonely blade: 6 + 2 per empty cell (25 cells, the blade takes 3, a shield 2: 20 empty -> 46).
        grid = Fresh(doll);
        ItemEntity blade = Place(grid, "gear_lonely_blade", 0, 0, 0);
        Place(grid, "gear_wooden_shield", 0, 2, 0);
        int empty = grid.UsableCellCount() - grid.FilledCellCount();
        zero = Fight(core);
        enemy = Sturdy(core.Combat.EnemyFaction.Fighters[0]);
        hp = enemy.RuntimeHP;
        zero.Attack(enemy, blade);
        Check(hp - enemy.RuntimeHP == 6 + 2 * empty, $"lonely blade with {empty} empty dealt {hp - enemy.RuntimeHP}, expected {6 + 2 * empty}");
        Place(grid, "loot_rusty_coil", 3, 3, 0);
        hp = enemy.RuntimeHP;
        zero.Attack(enemy, blade);
        Check(hp - enemy.RuntimeHP < 6 + 2 * empty, "the lonely blade should weaken once loot fills a cell");

        // Music box: nothing at full HP; at 30% HP the touching blade gets +30% (+10% per bond level).
        grid = Fresh(doll);
        Place(grid, "gear_music_box", 0, 0, 0);
        ItemEntity dagger = Place(grid, "gear_rusty_dagger", 2, 0, 0);
        ItemEntity far = Place(grid, "gear_rusty_dagger", 4, 3, 0);
        doll.Bond.AffectionLevel = 0;
        zero = Fight(core);
        enemy = Sturdy(core.Combat.EnemyFaction.Fighters[0]);
        zero.MaxAP = 10;
        zero.CurrentAP = 10;
        int baseDamage = Mathf.RoundToInt(dagger.Combat.RuntimeDamage);
        hp = enemy.RuntimeHP;
        zero.Attack(enemy, dagger);
        Check(hp - enemy.RuntimeHP == baseDamage, $"music box at full HP: dagger dealt {hp - enemy.RuntimeHP}, expected {baseDamage}");
        zero.RuntimeHP = Mathf.FloorToInt(zero.RuntimeMaxHP * 0.3f);
        hp = enemy.RuntimeHP;
        zero.Attack(enemy, dagger);
        Check(hp - enemy.RuntimeHP == Mathf.RoundToInt(baseDamage * 1.3f), $"music box at 30% HP: dagger dealt {hp - enemy.RuntimeHP}, expected {Mathf.RoundToInt(baseDamage * 1.3f)}");
        hp = enemy.RuntimeHP;
        zero.Attack(enemy, far);
        Check(hp - enemy.RuntimeHP == baseDamage, "a dagger away from the music box should not resonate");
        doll.Bond.AffectionLevel = 3;
        hp = enemy.RuntimeHP;
        zero.Attack(enemy, dagger);
        Check(hp - enemy.RuntimeHP == Mathf.RoundToInt(baseDamage * 1.6f), $"music box at bond 3: dagger dealt {hp - enemy.RuntimeHP}, expected {Mathf.RoundToInt(baseDamage * 1.6f)}");
        doll.Bond.AffectionLevel = 0;

        core.Combat.CurrentState = CombatState.End;
        VisualQueue.Clear();
        if (Failures.Count > 0) {
            foreach (string failure in Failures) {
                Debug.LogError("[BuildArchetypeTest] " + failure);
            }

            throw new System.Exception($"BuildArchetypeTest: {Failures.Count} failure(s), first: {Failures[0]}");
        }

        Debug.Log("=== Build Archetype Test Passed ===");
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
        if (!grid.PlaceItem(item, x, y, rotation) && !grid.TryPlaceFirstAvailable(item, out _, out _)) {
            Failures.Add($"could not place {configID}");
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
}
