using System.Collections.Generic;
using Newtonsoft.Json.Linq;
using UnityEngine;

// Monsters that answer the item archetypes (docs/specs/2026-10-17-content-expansion.md, 怪物配合): armour against small
// hits, a shield, cleansing poison, a blast through Zero's shield, shaking the backpack, and actions held back until a
// later round. Each runs on a real fight against a stand-in monster.
public static class MonsterCounterTest {
    private static readonly List<string> Failures = new List<string>();

    public static void Run() {
        Debug.Log("=== Running Monster Counter Test ===");
        Failures.Clear();
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        DollEntity doll = core.CurrentPlayer.ActiveDoll;

        // Armour: every hit Zero lands loses 3, but never drops below 1; poison goes past it.
        BackpackGrid grid = Fresh(doll);
        ItemEntity blade = Blade(grid, 0);
        DollFighter zero = Fight(core);
        MonsterFighter monster = Armoured(core, 3);
        int hpBefore = monster.RuntimeHP;
        zero.Attack(monster, blade);
        int expected = Mathf.Max(1, Mathf.RoundToInt(blade.Combat.RuntimeDamage) - 3);
        Check(hpBefore - monster.RuntimeHP == expected, $"armoured hit took {hpBefore - monster.RuntimeHP}, expected {expected}");
        Check(CombatStatusService.ModifyDamage(zero, monster, 2) == 1, "armour took a small hit below 1");
        Check(CombatStatusService.Describe(monster).StartsWith("护甲 3"), "status plate: " + CombatStatusService.Describe(monster));
        CombatStatusService.Apply(monster, CombatStatusType.Poison, 4);
        hpBefore = monster.RuntimeHP;
        CombatStatusService.TickTurnStart(monster);
        Check(hpBefore - monster.RuntimeHP == 4, $"poison through armour took {hpBefore - monster.RuntimeHP}, expected 4");

        // GainShield: the monster raises 8 shield.
        Fresh(doll);
        Fight(core);
        monster = Sturdy(core);
        Check(Execute(core, monster, Action("GainShield", "Self", ("Amount", 8))) && monster.RuntimeShield == 8, $"shield {monster.RuntimeShield}, expected 8");

        // Cleanse: poison washed off, 2 HP back.
        CombatStatusService.Apply(monster, CombatStatusType.Poison, 5);
        monster.RuntimeHP -= 10;
        hpBefore = monster.RuntimeHP;
        Check(Execute(core, monster, Action("Cleanse", "AllAllies", ("Heal", 2))), "cleanse did not run");
        Check(CombatStatusService.Get(monster, CombatStatusType.Poison) == 0 && monster.RuntimeHP == hpBefore + 2, $"after cleanse: poison {CombatStatusService.Get(monster, CombatStatusType.Poison)}, HP {hpBefore}->{monster.RuntimeHP}");

        // SelfDestruct: 10 straight to HP past a 50 shield, and the monster is gone.
        Fresh(doll);
        zero = Fight(core);
        monster = Sturdy(core);
        zero.AddShield(50);
        hpBefore = zero.RuntimeHP;
        MonsterActionConfig blast = Action("SelfDestruct", "FirstAlivePlayer", ("Damage", 10));
        MonsterActionIntentPreview blastPreview = MonsterIntentPreviewService.BuildActionPreview(monster, core.Combat.CreateMonsterActionContextForPreview(monster), blast);
        Check(blastPreview.IntentType == MonsterIntentType.SelfDestruct && blastPreview.ShortText.Contains("无视护盾"), "blast intent: " + blastPreview.ShortText);
        Check(Execute(core, monster, blast), "self-destruct did not run");
        Check(hpBefore - zero.RuntimeHP == 10 && zero.RuntimeShield == 50 && monster.RuntimeHP == 0, $"blast: Zero HP {hpBefore}->{zero.RuntimeHP}, shield {zero.RuntimeShield}, monster HP {monster.RuntimeHP}");

        // ShuffleItems: items move, none are lost, the grid still knows where each one is.
        grid = Fresh(doll);
        ItemEntity a = Blade(grid, 0);
        ItemEntity b = Blade(grid, 1);
        ItemEntity c = Blade(grid, 2);
        Dictionary<ItemEntity, string> before = Positions(a, b, c);
        List<ItemEntity> moved = ShuffleItemsAction.Shuffle(grid, 2);
        Check(moved.Count >= 1, "nothing moved");
        Check(grid.ContainedItems.Count == 3, $"backpack holds {grid.ContainedItems.Count} items after shaking, expected 3");
        foreach (ItemEntity item in moved) {
            Check(before[item] != Where(item), $"{item.InstanceID} reported moved but stayed at {Where(item)}");
        }

        foreach (ItemEntity item in new[] { a, b, c }) {
            Check(grid.GetItemAt(item.Grid.CurrentPos[0], item.Grid.CurrentPos[1]) == item, $"{item.InstanceID} is not at its recorded spot");
        }

        // A full backpack still gets shaken: two items swap places.
        grid = Fresh(doll);
        List<ItemEntity> filled = new List<ItemEntity>();
        for (int x = 0; x < grid.Width; x++) {
            ItemEntity filler = Blade(grid, x);
            if (filler != null) {
                filled.Add(filler);
            }
        }

        bool full = true;
        for (int x = 0; x < grid.Width && full; x++) {
            for (int y = 0; y < grid.Height; y++) {
                if (grid.CanPlaceItem(Single(), x, y, 0)) {
                    full = false;
                    break;
                }
            }
        }

        if (full && filled.Count >= 2) {
            moved = ShuffleItemsAction.Shuffle(grid, 1);
            Check(moved.Count == 2 && grid.ContainedItems.Count == filled.Count, $"full backpack: moved {moved.Count}, holds {grid.ContainedItems.Count}");
        }

        // FromRound: held back in round 1, allowed from round 2.
        Fresh(doll);
        Blade((BackpackGrid)doll.RuntimeGrid, 0);
        Fight(core);
        monster = Sturdy(core);
        MonsterActionConfig late = Action("GainShield", "Self", ("Amount", 1), ("FromRound", 2));
        late.Condition = "FromRound";
        Check(!MonsterActionConditionEvaluator.Evaluate(late.Condition, core.Combat.CreateMonsterActionContextForPreview(monster), late), "FromRound 2 allowed in round 1");
        core.Combat.EndPlayerTurn();
        Check(MonsterActionConditionEvaluator.Evaluate(late.Condition, core.Combat.CreateMonsterActionContextForPreview(monster), late), "FromRound 2 refused in round 2");

        if (Failures.Count == 0) {
            Debug.Log("Monster Counter PASSED.");
        } else {
            Debug.LogError("Monster Counter FAILED: " + string.Join("; ", Failures));
        }

        VisualQueue.Clear();
        Debug.Log("=== Monster Counter Test Finished ===");
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

    private static DollFighter Fight(CoreBackend core) {
        GridSolver.RecalculateAllEffects(core.CurrentPlayer.ActiveDoll);
        core.Combat.StartCombat(new List<string> { "mob_lost_miner_echo" });
        DollFighter zero = (DollFighter)core.Combat.PlayerFaction.Fighters[0];
        zero.MaxAP = 10;
        zero.CurrentAP = 10;
        return zero;
    }

    private static MonsterFighter Sturdy(CoreBackend core) {
        MonsterFighter monster = (MonsterFighter)core.Combat.EnemyFaction.Fighters[0];
        monster.RuntimeMaxHP = 999;
        monster.RuntimeHP = 999;
        return monster;
    }

    // The fight's monster, given its own copy of the config with armour (the shared config stays untouched).
    private static MonsterFighter Armoured(CoreBackend core, int armor) {
        MonsterFighter monster = Sturdy(core);
        MonsterEntity source = monster.DataRef;
        monster.DataRef = new MonsterEntity { MonsterID = source.MonsterID, Name = source.Name, HP = source.HP, AI = source.AI, Armor = armor };
        return monster;
    }

    private static ItemEntity Blade(BackpackGrid grid, int x) {
        ItemEntity item = ConfigManager.CreateItem("gear_tactical_blade");
        item.InstanceID = "counter_" + x + "_" + System.Guid.NewGuid().ToString("N").Substring(0, 6);
        item.Combat.Effects = new List<EffectData>();
        return grid.PlaceItem(item, x, 0, 0) ? item : null;
    }

    private static ItemEntity Single() {
        return ConfigManager.CreateItem("gear_corrosion_needle");
    }

    private static MonsterActionConfig Action(string type, string target, params (string key, int value)[] parameters) {
        MonsterActionConfig config = new MonsterActionConfig { ActionID = "test_" + type, ActionType = type, Target = target };
        foreach ((string key, int value) in parameters) {
            config.Params[key] = new JValue(value);
        }

        return config;
    }

    private static bool Execute(CoreBackend core, MonsterFighter monster, MonsterActionConfig config) {
        MonsterActionContext context = core.Combat.CreateMonsterActionContextForPreview(monster);
        MonsterActionBase action = MonsterActionFactory.Create(config);
        return action != null && action.CanExecute(context) && action.Execute(context);
    }

    private static Dictionary<ItemEntity, string> Positions(params ItemEntity[] items) {
        Dictionary<ItemEntity, string> positions = new Dictionary<ItemEntity, string>();
        foreach (ItemEntity item in items) {
            positions[item] = Where(item);
        }

        return positions;
    }

    private static string Where(ItemEntity item) {
        return $"{item.Grid.CurrentPos[0]},{item.Grid.CurrentPos[1]},{item.Grid.Rotation}";
    }
}
