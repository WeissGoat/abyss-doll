using System.Collections.Generic;
using UnityEngine;

// M2 batch 6: enemy intents carry their numbers, the HUD previews the hit after the shield, the turn can tell when
// the player is out of actions, and the adjacency support items boost the weapons next to them.
public static class CombatPreviewTest {
    public static void Run() {
        Debug.Log("=== Running Combat Preview Test ===");

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();

        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        BackpackGrid grid = new BackpackGrid(doll.Chassis);
        doll.RuntimeGrid = grid;
        GridSolver.RecalculateAllEffects(doll);
        List<string> failures = new List<string>();
        void Check(bool condition, string message) {
            if (!condition) {
                failures.Add(message);
            }
        }

        // Intent with its number, and the incoming damage after the shield.
        core.Combat.StartCombat(new List<string> { "mob_scavenger_bug" });
        DollFighter player = core.Combat.PlayerFaction.Fighters[0] as DollFighter;
        MonsterIntentReport report = MonsterIntentPreviewService.BuildReport(core.Combat);
        Check(report.Monsters.Count == 1 && report.Monsters[0].SelectedIntent?.ShortText == "攻击 9", "intent short text: " + report.Monsters[0].SelectedIntent?.ShortText);
        Check(report.Monsters[0].SelectedIntent?.Badge == "9", "intent badge: " + report.Monsters[0].SelectedIntent?.Badge);

        int hp = player.RuntimeHP;
        IncomingDamagePreview none = CombatReadabilityTextService.BuildIncomingDamagePreview(core.Combat);
        Check(none.Total == 9 && none.Blocked == 0 && none.HPLoss == 9 && none.HPAfter == hp - 9 && none.Text.Contains("没有护盾"), "no shield: " + none.Text);
        player.AddShield(5);
        IncomingDamagePreview partial = CombatReadabilityTextService.BuildIncomingDamagePreview(core.Combat);
        Check(partial.Blocked == 5 && partial.HPLoss == 4 && partial.Text.Contains("护盾挡 5"), "partial shield: " + partial.Text);
        player.AddShield(20);
        IncomingDamagePreview full = CombatReadabilityTextService.BuildIncomingDamagePreview(core.Combat);
        Check(full.Blocked == 9 && full.HPLoss == 0 && !full.Lethal, "full shield: " + full.Text);
        player.RuntimeShield = 0;
        player.RuntimeHP = 3;
        IncomingDamagePreview lethal = CombatReadabilityTextService.BuildIncomingDamagePreview(core.Combat);
        Check(lethal.Lethal && lethal.Text.Contains("会倒下"), "lethal: " + lethal.Text);
        player.RuntimeHP = hp;

        // Out of AP: no bare-hands strike and no affordable manual item left.
        player.CurrentAP = player.MaxAP;
        Check(core.Combat.PlayerHasActionLeft(), "full AP but no action left");
        player.CurrentAP = 0;
        Check(!core.Combat.PlayerHasActionLeft(), "zero AP but an action left");
        core.Combat.EndPlayerTurn();
        Check(core.Combat.CurrentState == CombatState.PlayerTurn && core.Combat.PlayerHasActionLeft(), "the next turn has no action");

        // Junk intents name the item and its size.
        core.Combat.StartCombat(new List<string> { "mob_acid_slime" });
        MonsterFighter slime = core.Combat.EnemyFaction.Fighters[0] as MonsterFighter;
        MonsterActionConfig junk = slime.DataRef.AI.Actions.Find(action => action.ActionID == "acid_add_scrap");
        MonsterActionIntentPreview junkPreview = MonsterIntentPreviewService.BuildActionPreview(slime, core.Combat.CreateMonsterActionContextForPreview(slime), junk);
        Check(junkPreview.ShortText != null && junkPreview.ShortText.StartsWith("塞入") && junkPreview.ShortText.Contains("格"), "junk short text: " + junkPreview.ShortText);
        Check(junkPreview.Badge == junkPreview.CursedItemGridCost + "格", "junk badge: " + junkPreview.Badge);

        // Adjacency: a whetstone next to the dagger adds 25%; a lens adds 50% only to the item on its right.
        ItemEntity dagger = ConfigManager.CreateItem("gear_rusty_dagger");
        Check(grid.TryPlaceFirstAvailable(dagger, out _, out _), "dagger did not fit");
        float baseDamage = dagger.Combat.BaseValue;
        List<int[]> daggerCells = grid.GetOccupiedCells(dagger, dagger.Grid.CurrentPos[0], dagger.Grid.CurrentPos[1]);
        ItemEntity whetstone = ConfigManager.CreateItem("gear_whetstone");
        Check(PlaceTouching(grid, whetstone, daggerCells, 0, null), "whetstone did not fit next to the dagger");
        GridSolver.RecalculateAllEffects(doll);
        Check(Mathf.Approximately(dagger.Combat.RuntimeDamage, baseDamage * 1.25f), $"whetstone: {baseDamage} -> {dagger.Combat.RuntimeDamage}");
        Check(ItemPresentationRules.BuildCompactSummary(dagger).Contains("(+"), "summary hides the boost: " + ItemPresentationRules.BuildCompactSummary(dagger));
        grid.RemoveItem(whetstone);

        // Move the dagger one column right of a free cell for the lens.
        grid.RemoveItem(dagger);
        ItemEntity lens = ConfigManager.CreateItem("gear_focus_lens");
        bool lensPlaced = false;
        for (int y = 0; y < grid.Height && !lensPlaced; y++) {
            for (int x = 0; x + 1 < grid.Width && !lensPlaced; x++) {
                if (grid.CanPlaceItem(lens, x, y, 0) && grid.CanPlaceItem(dagger, x + 1, y, dagger.Grid.Rotation)) {
                    lensPlaced = grid.PlaceItem(dagger, x + 1, y, dagger.Grid.Rotation) && grid.PlaceItem(lens, x, y, 0);
                }
            }
        }
        Check(lensPlaced, "lens did not fit left of the dagger");
        GridSolver.RecalculateAllEffects(doll);
        Check(Mathf.Approximately(dagger.Combat.RuntimeDamage, baseDamage * 1.5f), $"lens pointing right: {baseDamage} -> {dagger.Combat.RuntimeDamage}");
        int[] lensCell = { lens.Grid.CurrentPos[0], lens.Grid.CurrentPos[1] };
        grid.RemoveItem(lens);
        Check(grid.PlaceItem(lens, lensCell[0], lensCell[1], 180), "lens could not turn around");
        GridSolver.RecalculateAllEffects(doll);
        Check(Mathf.Approximately(dagger.Combat.RuntimeDamage, baseDamage), $"lens pointing away still boosts: {dagger.Combat.RuntimeDamage}");

        if (failures.Count == 0) {
            Debug.Log("Combat Preview PASSED.");
        } else {
            Debug.LogError("Combat Preview FAILED: " + string.Join("; ", failures));
        }

        GameRoot.Core = null;
        Debug.Log("=== Combat Preview Test Finished ===");
    }

    // Places a 1x1 item in a free cell touching one of the target cells; acceptOffset filters the (target - cell) offset.
    private static bool PlaceTouching(BackpackGrid grid, ItemEntity item, List<int[]> targetCells, int rotation, System.Func<int[], bool> acceptOffset) {
        int[][] offsets = { new[] { 1, 0 }, new[] { -1, 0 }, new[] { 0, 1 }, new[] { 0, -1 } };
        foreach (int[] target in targetCells) {
            foreach (int[] offset in offsets) {
                if (acceptOffset != null && !acceptOffset(offset)) {
                    continue;
                }

                int x = target[0] - offset[0];
                int y = target[1] - offset[1];
                if (grid.CanPlaceItem(item, x, y, rotation) && grid.PlaceItem(item, x, y, rotation)) {
                    return true;
                }
            }
        }

        return false;
    }
}
