using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

public static class MonsterActionAITest {
    public static void Run() {
        Debug.Log("=== Running Monster Action AI Test ===");

        TestEnemyTurnUsesMonsterActionRunner();
        TestReduceWeaponDamageRuntimeModifier();
        TestAddCursedItemAction();

        Debug.Log("=== Monster Action AI Test Finished ===");
    }

    private static void TestEnemyTurnUsesMonsterActionRunner() {
        CoreBackend core = BootstrapCoreWithEmptyGrid();
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        doll.Status.HP_Max = 100;
        doll.Status.HP_Current = 100;

        core.Combat.StartCombat(new List<string> { "mob_scavenger_bug" });
        core.Combat.EndPlayerTurn();

        DollFighter playerFighter = core.Combat.PlayerFaction.Fighters[0] as DollFighter;
        if (playerFighter == null || playerFighter.RuntimeHP != 90) {
            Debug.LogError($"MonsterActionRunner damage FAILED. Expected player HP 90, got {playerFighter?.RuntimeHP}");
            return;
        }

        Debug.Log("MonsterActionRunner damage PASSED.");
    }

    private static void TestReduceWeaponDamageRuntimeModifier() {
        CoreBackend core = BootstrapCoreWithEmptyGrid();
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        BackpackGrid grid = doll.RuntimeGrid as BackpackGrid;

        ItemEntity weapon = ConfigManager.CreateItem("gear_tactical_blade");
        grid.PlaceItem(weapon, 0, 0);
        GridSolver.RecalculateAllEffects(doll);

        int baseDamage = Mathf.RoundToInt(weapon.Combat.RuntimeDamage);
        MonsterActionConfig actionConfig = new MonsterActionConfig {
            ActionID = "test_reduce_weapon",
            ActionType = "ReduceWeaponDamage",
            Target = "RandomPlayerWeapon",
            Params = new Dictionary<string, JToken> {
                { "Multiplier", JToken.FromObject(0.5f) },
                { "DurationPlayerTurns", JToken.FromObject(1) }
            }
        };

        MonsterActionContext context = BuildDirectActionContext(core, "mob_acid_slime");
        bool executed = MonsterActionFactory.Create(actionConfig).Execute(context);
        int reducedDamage = Mathf.RoundToInt(weapon.Combat.RuntimeDamage);
        int expectedReduced = Mathf.RoundToInt(baseDamage * 0.5f);

        if (!executed || reducedDamage != expectedReduced) {
            Debug.LogError($"ReduceWeaponDamage FAILED. Expected {expectedReduced}, got {reducedDamage}");
            return;
        }

        core.Combat.MonsterRuntimeModifiers.AdvancePlayerTurnEnd();
        GridSolver.RecalculateAllEffects(doll);
        int restoredDamage = Mathf.RoundToInt(weapon.Combat.RuntimeDamage);
        if (restoredDamage != baseDamage) {
            Debug.LogError($"ReduceWeaponDamage expiry FAILED. Expected restored {baseDamage}, got {restoredDamage}");
            return;
        }

        Debug.Log("ReduceWeaponDamage runtime modifier PASSED.");
    }

    private static void TestAddCursedItemAction() {
        CoreBackend core = BootstrapCoreWithEmptyGrid();
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        BackpackGrid grid = doll.RuntimeGrid as BackpackGrid;

        MonsterActionConfig actionConfig = new MonsterActionConfig {
            ActionID = "test_add_curse",
            ActionType = "AddCursedItem",
            Target = "PlayerGridFirstFit",
            Params = new Dictionary<string, JToken> {
                { "ItemID", JToken.FromObject("loot_gear_scrap") },
                { "OverrideTags", JToken.FromObject(new List<string> { "Cursed", "Toxic" }) },
                { "OverrideValue", JToken.FromObject(0) }
            }
        };

        MonsterActionContext context = BuildDirectActionContext(core, "elite_mutant_amalgam");
        int beforeCount = grid.ContainedItems.Count;
        bool executed = MonsterActionFactory.Create(actionConfig).Execute(context);
        int afterCount = grid.ContainedItems.Count;

        ItemEntity cursedItem = grid.ContainedItems.Find(item =>
            item.Tags != null &&
            item.Tags.Contains("Cursed") &&
            item.Tags.Contains("Toxic") &&
            item.BaseValue == 0);

        if (!executed || afterCount != beforeCount + 1 || cursedItem == null) {
            Debug.LogError($"AddCursedItem FAILED. Before={beforeCount}, After={afterCount}, CursedFound={cursedItem != null}");
            return;
        }

        Debug.Log("AddCursedItem action PASSED.");
    }

    private static CoreBackend BootstrapCoreWithEmptyGrid() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();

        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        doll.RuntimeGrid = new BackpackGrid(doll.Chassis);
        GridSolver.RecalculateAllEffects(doll);
        return core;
    }

    private static MonsterActionContext BuildDirectActionContext(CoreBackend core, string monsterID) {
        CombatFaction enemyFaction = new CombatFaction { Type = FactionType.Enemy };
        MonsterFighter actor = new MonsterFighter(ConfigManager.Monsters[monsterID], enemyFaction);
        enemyFaction.Fighters.Add(actor);

        CombatFaction playerFaction = new CombatFaction { Type = FactionType.Player };
        playerFaction.Fighters.Add(new DollFighter(core.CurrentPlayer.ActiveDoll, playerFaction));

        return new MonsterActionContext {
            Combat = core.Combat,
            Actor = actor,
            PlayerFaction = playerFaction,
            EnemyFaction = enemyFaction,
            ActiveDoll = core.CurrentPlayer.ActiveDoll,
            RuntimeState = new MonsterActionRuntimeState(),
            RuntimeModifiers = core.Combat.MonsterRuntimeModifiers
        };
    }
}
