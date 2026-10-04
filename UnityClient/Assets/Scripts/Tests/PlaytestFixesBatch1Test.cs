using System.Collections.Generic;
using UnityEngine;

// M2 batch 1 (producer playtest 2026-10-04): right-click rotation of items already in the backpack, the 补给铺 that
// sells basic weapons, the bare-hands strike for a backpack without weapons, and the small-window startup check.
public static class PlaytestFixesBatch1Test {
    public static void Run() {
        Debug.Log("=== Running Playtest Fixes Batch 1 Test ===");

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();

        PlayerProfile player = core.CurrentPlayer;
        DollEntity doll = player.ActiveDoll;
        BackpackGrid grid = (BackpackGrid)doll.RuntimeGrid;
        List<string> failures = new List<string>();
        void Check(bool condition, string message) {
            if (!condition) {
                failures.Add(message);
            }
        }

        // Start from an empty backpack (the kit goes to the stash).
        foreach (ItemEntity item in new List<ItemEntity>(grid.ContainedItems)) {
            PreDiveLoadoutService.TryReturnToStash(player, item, out _);
        }
        InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll("Test");

        // 1. A 2x3 armour is rotatable now and turns in place.
        ItemEntity armor = ConfigManager.CreateItem("gear_cracked_iron_armor");
        Check(BackpackGrid.CanRotateItem(armor), "2x3 armour is still not rotatable");
        Check(InventoryInteractionService.RequestPlace(armor, 0, 0, 0, context, out string placeReason), "placing armour failed: " + placeReason);
        bool rotated = InventoryInteractionService.RequestRotateInPlace(armor, context, out InventoryItemPlacement placement, out string rotateReason);
        Check(rotated && placement.Rotation == 90 && BackpackGrid.NormalizeRotation(armor.Grid.Rotation) == 90, $"rotate in place failed: {rotateReason}, rotation {armor.Grid.Rotation}");
        Check(grid.ContainedItems.Contains(armor), "armour left the backpack after rotating");

        // 2. A fixed-orientation item says so instead of failing silently.
        ItemEntity sedative = ConfigManager.CreateItem("con_cheap_sedative");
        Check(InventoryInteractionService.RequestPlace(sedative, 4, 4, 0, context, out _) || grid.TryPlaceFirstAvailable(sedative, out _, out _), "could not place the sedative");
        Check(!InventoryInteractionService.RequestRotateInPlace(sedative, context, out _, out string fixedReason) && fixedReason.Contains("不能旋转"), "fixed item rotation reason: " + fixedReason);

        // 3. No room to turn: the item stays where it was.
        int fillers = 0;
        while (fillers < 60) {
            ItemEntity filler = ConfigManager.CreateItem("loot_gear_scrap");
            if (!grid.TryPlaceFirstAvailable(filler, out _, out _)) {
                break;
            }
            fillers++;
        }
        int[] before = { armor.Grid.CurrentPos[0], armor.Grid.CurrentPos[1], armor.Grid.Rotation };
        bool turnedWhenFull = InventoryInteractionService.RequestRotateInPlace(armor, context, out _, out string fullReason);
        Check(!turnedWhenFull && fullReason.Contains("转不开"), "rotating in a full backpack: " + fullReason);
        Check(grid.ContainedItems.Contains(armor) && armor.Grid.CurrentPos[0] == before[0] && armor.Grid.CurrentPos[1] == before[1] && armor.Grid.Rotation == before[2], "failed rotation moved the armour");
        grid.ClearAllItems();

        // 4. 补给铺: a basic weapon is always for sale, priced above what it sells for; buying puts it in the stash.
        List<WorkshopShopOffer> offers = WorkshopShopService.GetOffers(player);
        WorkshopShopOffer dagger = offers.Find(offer => offer.ConfigID == "gear_rusty_dagger");
        Check(dagger != null, "the shop does not sell the rusty dagger");
        if (dagger != null) {
            int sellValue = TownEconomyService.CalculateItemSellValue(player, ConfigManager.Items["gear_rusty_dagger"], EconomySellChannel.DumpBox).FinalValue;
            Check(dagger.Price > sellValue, $"dagger costs {dagger.Price} but sells for {sellValue}");

            player.Money = dagger.Price - 1;
            int stashBefore = player.StashInventory.Count;
            WorkshopShopPurchaseResult poor = WorkshopShopService.TryBuy(player, "gear_rusty_dagger");
            Check(!poor.Success && player.Money == dagger.Price - 1 && player.StashInventory.Count == stashBefore, "buying without enough money changed something");

            player.Money = dagger.Price + 10;
            WorkshopShopPurchaseResult bought = WorkshopShopService.TryBuy(player, "gear_rusty_dagger");
            Check(bought.Success && player.Money == 10, $"buying the dagger: {bought.Reason}, money {player.Money}");
            Check(bought.Item != null && player.StashInventory.Contains(bought.Item) && bought.Item.ContainerType == ItemContainerType.GroundInventory, "bought dagger is not in the stash");
            Check(!WorkshopShopService.TryBuy(player, "loot_gear_scrap").Success, "the shop sold something it does not stock");
        }

        // 5. Bare hands: with no weapon in the backpack the doll can still hit for UnarmedDamage.
        CombatNode combatNode = new CombatNode { NodeID = "test_unarmed" };
        combatNode.MonsterIDs.Add("mob_scavenger_bug");
        core.Dungeon.CurrentLayer = new DungeonLayer { LayerID = 1, RootNode = combatNode, CurrentNode = combatNode };
        core.Combat.StartCombat(new List<string> { "mob_scavenger_bug" });
        FighterEntity enemy = core.Combat.EnemyFaction.Fighters[0];
        enemy.RuntimeHP = 100;
        DollFighter fighter = (DollFighter)core.Combat.PlayerFaction.Fighters[0];
        int apBefore = fighter.CurrentAP;
        int toughnessBefore = enemy.RuntimeHP + enemy.RuntimeShield;
        bool began = ItemUseService.TryBeginUnarmedStrike(out string unarmedReason);
        bool hit = began && ItemUseService.TryConfirmPendingTarget(enemy, out unarmedReason);
        int expected = ItemUseService.GetUnarmedDamage(doll);
        int dealt = toughnessBefore - (enemy.RuntimeHP + enemy.RuntimeShield);
        Check(hit && dealt == expected, $"unarmed strike: began={began}, hit={hit}, reason={unarmedReason}, dealt {dealt}, expected {expected}");
        Check(fighter.CurrentAP == apBefore - ItemUseService.GetUnarmedAPCost(doll), $"unarmed strike AP {apBefore} -> {fighter.CurrentAP}");

        // 6. Startup window check.
        Check(DisplayStartupGuard.NeedsReset(FullScreenMode.Windowed, 637, 357), "a 637x357 window is not reset");
        Check(!DisplayStartupGuard.NeedsReset(FullScreenMode.Windowed, 1600, 900), "a 1600x900 window is reset");
        Check(!DisplayStartupGuard.NeedsReset(FullScreenMode.FullScreenWindow, 800, 600), "fullscreen is reset");

        if (failures.Count == 0) {
            Debug.Log("Playtest Fixes Batch 1 PASSED.");
        } else {
            Debug.LogError("Playtest Fixes Batch 1 FAILED: " + string.Join("; ", failures));
        }

        Debug.Log("=== Playtest Fixes Batch 1 Test Finished ===");
    }
}
