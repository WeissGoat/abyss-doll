using System.Linq;
using UnityEngine;

public static class MonsterIntentPreviewServiceSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Monster Intent Preview Service Smoke Test ===");

        TestDamageIntentPreview();
        TestWeaponCorrosionIntentPreview();
        TestCursedItemIntentPreviewAndReadOnlyBehavior();

        Debug.Log("=== Monster Intent Preview Service Smoke Test Finished ===");
    }

    private static void TestDamageIntentPreview() {
        CoreBackend core = CreateCoreWithEmptyGrid();
        core.Combat.StartCombat(new System.Collections.Generic.List<string> { "mob_scavenger_bug" });

        MonsterIntentReport report = MonsterIntentPreviewService.BuildReport(core.Combat);
        MonsterIntentCard card = report.Monsters.FirstOrDefault();
        MonsterActionIntentPreview intent = card?.SelectedIntent;

        bool passed = report.Success
            && card != null
            && card.MonsterID == "mob_scavenger_bug"
            && intent != null
            && intent.IntentType == MonsterIntentType.Attack
            && intent.CanExecute
            && intent.DamagePerHit == 9
            && intent.RepeatCount == 1
            && intent.EstimatedTotalDamage == 9
            && intent.Title == "攻击";

        if (passed) {
            Debug.Log("Monster Intent Damage Preview PASSED.");
        } else {
            Debug.LogError($"Monster Intent Damage Preview FAILED. Success={report.Success}, Monster={card?.MonsterID}, Intent={intent?.IntentType}, Damage={intent?.EstimatedTotalDamage ?? -1}, Reason={report.Reason ?? intent?.BlockReason}");
        }
    }

    private static void TestWeaponCorrosionIntentPreview() {
        CoreBackend core = CreateCoreWithEmptyGrid();
        BackpackGrid grid = core.CurrentPlayer.ActiveDoll.RuntimeGrid as BackpackGrid;
        ItemEntity weapon = ConfigManager.CreateItem("gear_tactical_blade");
        grid.PlaceItem(weapon, 0, 0);
        GridSolver.RecalculateAllEffects(core.CurrentPlayer.ActiveDoll);

        // Since the 2026-06-08 formal configs no layer-1 monster corrodes weapons (it moved to layer 2), so the test
        // registers its own copy of the slime with the old corrosion action instead of depending on content.
        const string testMonsterID = "test_corrosion_slime";
        MonsterEntity slime = ConfigManager.Monsters["mob_acid_slime"];
        MonsterEntity corrosionSlime = Newtonsoft.Json.JsonConvert.DeserializeObject<MonsterEntity>(
            Newtonsoft.Json.JsonConvert.SerializeObject(slime));
        corrosionSlime.MonsterID = testMonsterID;
        corrosionSlime.AI.Actions.Add(new MonsterActionConfig {
            ActionID = "test_corrode_weapon",
            ActionType = "ReduceWeaponDamage",
            Target = "RandomPlayerWeapon",
            Weight = 30,
            CooldownTurns = 2,
            Condition = "PlayerHasWeapon",
            Params = new System.Collections.Generic.Dictionary<string, Newtonsoft.Json.Linq.JToken> {
                { "Multiplier", 0.5f },
                { "DurationPlayerTurns", 1 }
            }
        });
        ConfigManager.Monsters[testMonsterID] = corrosionSlime;

        MonsterIntentReport report;
        try {
            core.Combat.StartCombat(new System.Collections.Generic.List<string> { testMonsterID });
            report = MonsterIntentPreviewService.BuildReport(core.Combat);
        } finally {
            ConfigManager.Monsters.Remove(testMonsterID);
        }
        MonsterIntentCard card = report.Monsters.FirstOrDefault();
        MonsterActionIntentPreview corrosion = card?.ActionIntents.FirstOrDefault(intent => intent.IntentType == MonsterIntentType.WeakenWeapon);

        bool passed = report.Success
            && card != null
            && corrosion != null
            && corrosion.CanExecute
            && corrosion.WeaponDamageMultiplier == 0.5f
            && corrosion.DurationPlayerTurns == 1
            && corrosion.Target == "RandomPlayerWeapon"
            && corrosion.Description.Contains("50%");

        if (passed) {
            Debug.Log("Monster Intent Weapon Corrosion Preview PASSED.");
        } else {
            Debug.LogError($"Monster Intent Weapon Corrosion Preview FAILED. Success={report.Success}, Monster={card?.MonsterID}, Found={corrosion != null}, CanExecute={corrosion?.CanExecute ?? false}, Multiplier={corrosion?.WeaponDamageMultiplier ?? -1f}, Reason={report.Reason ?? corrosion?.BlockReason}");
        }
    }

    private static void TestCursedItemIntentPreviewAndReadOnlyBehavior() {
        CoreBackend core = CreateCoreWithEmptyGrid();
        BackpackGrid grid = core.CurrentPlayer.ActiveDoll.RuntimeGrid as BackpackGrid;
        int beforeCount = grid.ContainedItems.Count;

        core.Combat.StartCombat(new System.Collections.Generic.List<string> { "elite_mutant_amalgam" });

        MonsterIntentReport report = MonsterIntentPreviewService.BuildReport(core.Combat);
        MonsterIntentCard card = report.Monsters.FirstOrDefault();
        MonsterActionIntentPreview cursed = card?.ActionIntents.FirstOrDefault(intent => intent.IntentType == MonsterIntentType.AddJunk);
        int afterCount = grid.ContainedItems.Count;

        // Since the 2026-06-08 formal configs the amalgam plants an acid gland; read its name and size from config.
        ItemEntity expectedCursedItem = ConfigManager.CreateItem("loot_acid_gland");
        bool passed = report.Success
            && card != null
            && cursed != null
            && cursed.CanExecute
            && cursed.CursedItemID == "loot_acid_gland"
            && cursed.CursedItemName == expectedCursedItem.Name
            && cursed.CursedItemGridCost == expectedCursedItem.Grid.GridCost
            && cursed.OverrideTags.Contains("Cursed")
            && cursed.OverrideTags.Contains("Toxic")
            && beforeCount == afterCount;

        if (passed) {
            Debug.Log("Monster Intent Cursed Item Preview PASSED.");
        } else {
            Debug.LogError($"Monster Intent Cursed Item Preview FAILED. Success={report.Success}, Monster={card?.MonsterID}, Found={cursed != null}, CanExecute={cursed?.CanExecute ?? false}, Item={cursed?.CursedItemID}, Before={beforeCount}, After={afterCount}, Reason={report.Reason ?? cursed?.BlockReason}");
        }
    }

    private static CoreBackend CreateCoreWithEmptyGrid() {
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
}
