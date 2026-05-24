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
            && intent.DamagePerHit == 10
            && intent.RepeatCount == 1
            && intent.EstimatedTotalDamage == 10
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

        core.Combat.StartCombat(new System.Collections.Generic.List<string> { "mob_acid_slime" });

        MonsterIntentReport report = MonsterIntentPreviewService.BuildReport(core.Combat);
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

        bool passed = report.Success
            && card != null
            && cursed != null
            && cursed.CanExecute
            && cursed.CursedItemID == "loot_gear_scrap"
            && cursed.CursedItemName == "废旧齿轮"
            && cursed.CursedItemGridCost == 1
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
