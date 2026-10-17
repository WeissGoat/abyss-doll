using System.Collections.Generic;
using UnityEngine;

// Wishes, the shelf and what the bond opens (docs/specs/2026-10-17-home-loop.md, batch 4): a wish appears from day 3,
// at most one; an item wish is granted by handing over a matching stash item (which goes on the shelf), a dive wish by
// what the dive did, a rest wish by resting; an old wish is let go; milestones land on the shelf once; at bond 3 a
// lethal blow leaves Zero at 1 HP once a dive; at bond 2 she reads the path ahead.
public static class HomeWishTest {
    public static void Run() {
        Debug.Log("=== Running Home Wish Test ===");

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();
        DayCycleService.TakePendingReport();

        PlayerProfile player = core.CurrentPlayer;
        DollEntity doll = player.ActiveDoll;
        List<string> failures = new List<string>();
        void Check(bool condition, string message) {
            if (!condition) {
                failures.Add(message);
            }
        }

        Check(ConfigManager.Wishes.Count >= 10 && ConfigManager.Milestones.Count == 4, $"catalog: {ConfigManager.Wishes.Count} wishes, {ConfigManager.Milestones.Count} milestones");

        // No wish before day 3; one at most after.
        player.CurrentDay = 2;
        Check(WishService.Morning(player, out _) == null, "a wish appeared on day 2");
        player.CurrentDay = 3;
        WishConfig first = WishService.Morning(player, out _);
        Check(first != null && WishService.Active(player) == first, "no wish on day 3");
        Check(WishService.Morning(player, out _) == null, "a second wish appeared while one is active");

        // An item wish: hand over a matching item; it leaves the stash for the shelf, bond +2, first-wish milestone.
        player.ActiveWishID = "wish_gear";
        player.WishSinceDay = 3;
        doll.Bond = new DollBondComponent();
        ItemEntity gear = ConfigManager.CreateItem("loot_gear_scrap");
        player.StashInventory.Add(gear);
        player.StashInventory.Add(ConfigManager.CreateItem("con_cheap_sedative"));
        List<ItemEntity> offerable = WishService.Offerable(player);
        Check(offerable.Count == 1 && offerable[0] == gear, $"offerable items: {offerable.Count}");
        WishResult given = WishService.Give(player, gear);
        Check(given != null && !player.StashInventory.Contains(gear) && WishService.Active(player) == null, "giving the gear did not grant the wish");
        Check(BondService.Points(doll) == WishService.BondReward, $"bond after the wish: {BondService.Points(doll)}");
        Check(player.Shelf.Exists(m => m.Name == gear.Name) && player.Milestones.Contains("first_wish"), "shelf after the wish: " + player.Shelf.Count);

        // A dive wish: visiting a camp grants it and its line greets at the door.
        player.ActiveWishID = "wish_camp";
        DiveLog log = new DiveLog { LayerID = 1 };
        log.Steps.Add(new DiveLogStep { Kind = "战斗" });
        log.Steps.Add(new DiveLogStep { Kind = "营地" });
        WishResult camp = WishService.OnDive(player, log);
        Check(camp != null && log.WishDoneLine == camp.Wish.DoneLine && ZeroLineService.ArrivalLine(log) == camp.Wish.DoneLine, "camp wish not granted by the dive");

        // A rest wish through the day cycle; the night line is the wish's.
        player.ActiveWishID = "wish_sleep";
        player.IsNight = false;
        player.HasPendingMonthlyRent = false;
        DayEndReport rest = DayCycleService.Rest(player, out string restReason);
        Check(rest != null && rest.WishDone != null && ZeroLineService.NightLine(rest, player) == rest.WishDone.Wish.DoneLine, "rest wish: " + restReason);
        DayCycleService.FinishNight(player);
        DayCycleService.TakePendingReport();

        // An old wish is let go without a penalty.
        player.ActiveWishID = "wish_glow";
        player.WishSinceDay = player.CurrentDay - 10;
        int bondBefore = BondService.Points(doll);
        WishService.Morning(player, out WishConfig expired);
        Check(expired != null && expired.WishID == "wish_glow" && BondService.Points(doll) == bondBefore, "old wish not let go quietly");

        // Milestones once.
        int shelf = player.Shelf.Count;
        MementoService.Milestone(player, "first_boss");
        MementoService.Milestone(player, "first_boss");
        Check(player.Shelf.Count == shelf + 1, "a milestone landed twice");

        // Bond 3: one hold-on per dive.
        doll.Bond = new DollBondComponent();
        BondService.Add(doll, 5 + 9 + 12);
        Check(BondService.Level(doll) == 3, "bond did not reach 3");
        Check(core.Dungeon.StartRunAtLayer(1), "dive did not start");
        DollFighter fighter = new DollFighter(doll, new CombatFaction { Type = FactionType.Player });
        fighter.TakeDamage(fighter.RuntimeHP + 50);
        Check(fighter.RuntimeHP == 1 && DiveLogService.Current.HeldOn, $"hold-on left HP {fighter.RuntimeHP}");
        fighter.TakeDamage(10);
        Check(fighter.RuntimeHP == 0, "held on twice in one dive");
        fighter.Cleanup();

        // Bond 2: the path ahead.
        NodeBase entry = core.Dungeon.CurrentLayer.EntryNodes.Count > 0 ? core.Dungeon.CurrentLayer.EntryNodes[0] : core.Dungeon.CurrentLayer.RootNode;
        Check(!string.IsNullOrEmpty(HomeLoop.PathHint(entry)), "no path hint");

        // The shelf and wishes are saved.
        SaveFile restored = SaveService.Deserialize(SaveService.Serialize(player));
        Check(restored.Player.Shelf.Count == player.Shelf.Count && restored.Player.DoneWishes.Count == player.DoneWishes.Count, "shelf or wishes lost in the save");

        if (failures.Count == 0) {
            Debug.Log("Home Wish PASSED.");
        } else {
            Debug.LogError("Home Wish FAILED: " + string.Join("; ", failures));
        }

        Debug.Log("=== Home Wish Test Finished ===");
    }
}
