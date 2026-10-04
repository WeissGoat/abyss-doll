using System.Collections.Generic;
using UnityEngine;

// 出发前整备 and the dive-day settlement (docs/specs/2026-10-04-pre-dive-loadout.md). Regression for the empty
// backpack on the second dive (found 2026-10-04): after an evacuation everything sits in the stash, and the player
// needs a way to take a weapon back out.
public static class PreDiveLoadoutTest {
    public static void Run() {
        Debug.Log("=== Running Pre-Dive Loadout Test ===");

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();

        PlayerProfile player = core.CurrentPlayer;
        BackpackGrid grid = (BackpackGrid)player.ActiveDoll.RuntimeGrid;
        List<string> failures = new List<string>();
        void Check(bool condition, string message) {
            if (!condition) {
                failures.Add(message);
            }
        }

        // First dive: the starting kit is remembered at departure.
        int startingItems = grid.ContainedItems.Count;
        Check(PreDiveLoadoutService.HasWeapon(player), "starting backpack has no weapon");
        Check(core.Dungeon.StartRunAtLayer(1), "first dive did not start");
        Check(player.LastDiveLoadout.Count == startingItems, $"remembered {player.LastDiveLoadout.Count} of {startingItems} items");

        // Evacuation: everything goes to the stash and the day moves on.
        DungeonEventBus.PublishDungeonEvacuated();
        Check(grid.ContainedItems.Count == 0, $"backpack not empty after evacuation ({grid.ContainedItems.Count})");
        Check(player.StashInventory.Count == startingItems, $"stash has {player.StashInventory.Count} after evacuation");
        Check(player.CurrentDay == 2, $"day after first dive is {player.CurrentDay}, expected 2");
        Check(!PreDiveLoadoutService.HasWeapon(player), "weapon still in backpack after evacuation");

        // 带上上次的装备 puts the kit back where it was.
        LoadoutRestoreReport restore = PreDiveLoadoutService.RestoreLastLoadout(player);
        Check(restore.PlacedAtSamePosition == startingItems, $"restored {restore.PlacedAtSamePosition} at the same position, expected {startingItems}");
        Check(PreDiveLoadoutService.HasWeapon(player), "no weapon after restoring the last loadout");
        Check(player.StashInventory.Count == 0, $"stash has {player.StashInventory.Count} after restore");

        // Return to stash, then bring back by click.
        ItemEntity weapon = grid.ContainedItems.Find(item => item.ItemType == "Weapon");
        Check(PreDiveLoadoutService.TryReturnToStash(player, weapon, out string returnReason), "return to stash failed: " + returnReason);
        Check(player.StashInventory.Contains(weapon) && !grid.ContainedItems.Contains(weapon), "weapon did not move to the stash");
        Check(PreDiveLoadoutService.TryBringFromStash(player, weapon, out string bringReason), "bring from stash failed: " + bringReason);
        Check(grid.ContainedItems.Contains(weapon) && weapon.ContainerType == ItemContainerType.Backpack, "weapon did not move to the backpack");

        // Any stash item may be brought; it is bound and survives defeat.
        ItemEntity loot = ConfigManager.CreateItem("loot_gear_scrap");
        ItemLifecycleService.MarkGroundInventoryItem(player, loot, "Test");
        Check(PreDiveLoadoutService.Classify(loot) == LoadoutStashGroup.LootAndMaterial, "loot classified as " + PreDiveLoadoutService.Classify(loot));
        Check(PreDiveLoadoutService.TryBringFromStash(player, loot, out string lootReason), "bringing loot failed: " + lootReason);
        Check(loot.IsBinding, "brought loot is not bound");

        // A full backpack refuses with a reason.
        int added = 0;
        string fullReason = string.Empty;
        for (int i = 0; i < 40; i++) {
            ItemEntity filler = ConfigManager.CreateItem("con_repair_kit");
            ItemLifecycleService.MarkGroundInventoryItem(player, filler, "Test");
            if (!PreDiveLoadoutService.TryBringFromStash(player, filler, out fullReason)) {
                break;
            }
            added++;
        }
        Check(fullReason.Contains("放不下"), $"full backpack gave '{fullReason}' after {added} fillers");

        // Prep actions take no time.
        Check(player.CurrentDay == 2, $"loadout changed the day to {player.CurrentDay}");

        // Second dive ends in defeat: brought items survive and the day still moves on.
        Check(core.Dungeon.StartRunAtLayer(1), "second dive did not start");
        DungeonEventBus.PublishDungeonDefeated();
        Check(player.StashInventory.Contains(loot), "brought loot was lost on defeat");
        Check(player.StashInventory.Contains(weapon), "weapon was lost on defeat");
        Check(player.CurrentDay == 3, $"day after second dive is {player.CurrentDay}, expected 3");

        if (failures.Count == 0) {
            Debug.Log("Pre-Dive Loadout PASSED.");
        } else {
            Debug.LogError("Pre-Dive Loadout FAILED: " + string.Join("; ", failures));
        }

        Debug.Log("=== Pre-Dive Loadout Test Finished ===");
    }
}
