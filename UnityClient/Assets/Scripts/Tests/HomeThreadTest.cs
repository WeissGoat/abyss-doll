using System.Collections.Generic;
using UnityEngine;

// The evening as a thread (docs/specs/2026-10-18-home-evening-thread.md, A): what the dive brought home is unpacked at
// the counter (sell, keep, shelf, give her), the stamps follow, and 精神好 (clean at closing and talked to) gives the
// next dive's first turn +1 AP once.
public static class HomeThreadTest {
    public static void Run() {
        Debug.Log("=== Running Home Thread Test ===");

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

        // An evening after a dive that brought four things home; she wishes for something mechanical.
        player.CurrentDay = 4;
        DayCycleService.Rest(player, out _);
        Check(player.IsNight, "not night after the day's choice");
        player.StashInventory.Clear();
        ItemEntity forSale = Stash(player, "loot_rusty_coil");
        ItemEntity forStash = Stash(player, "con_repair_kit");
        ItemEntity forShelf = Stash(player, "loot_crystal_scale");
        ItemEntity forHer = Stash(player, "loot_gear_scrap");
        player.ActiveWishID = "wish_gear";
        player.WishSinceDay = player.CurrentDay;
        HomeEveningService.RecordHaul(player, new DiveLog { BroughtOutIDs = new List<string> { forSale.InstanceID, forStash.InstanceID, forShelf.InstanceID, forHer.InstanceID } });
        doll.Status.Grime = 2;

        Check(HomeEveningService.Haul(player).Count == 4, $"haul has {HomeEveningService.Haul(player).Count} items");
        Check(HomeEveningService.Next(player) == EveningStamp.Unpack, $"first stamp {HomeEveningService.Next(player)}");
        Check(HomeEveningService.Choices(player, forHer).Contains(HaulChoice.Give), "the wished item cannot be given");
        Check(!HomeEveningService.Choices(player, forStash).Contains(HaulChoice.Give), "an unwished item can be given");
        Check(WishService.Offerable(player).Contains(HomeEveningService.Remarkable(player, HomeEveningService.Haul(player))), "she does not remark on a wished item");
        string haulLine = HomeEveningService.HaulLine(player, forHer);
        Check(haulLine.Contains(forHer.Name), "haul line: " + haulLine);
        Check(!string.IsNullOrEmpty(HomeEveningService.RoomLine(player, HomeRoom.Hall)), "no hall line in the evening");

        // The four places.
        Check(HomeEveningService.Decide(player, forSale, HaulChoice.Sell, out string sold, out _) && player.Showcase.Contains(forSale), "sell did not reach the showcase: " + sold);
        Check(HomeEveningService.Decide(player, forStash, HaulChoice.Keep, out _, out _) && player.StashInventory.Contains(forStash), "kept item left the stash");
        int shelf = player.Shelf.Count;
        Check(HomeEveningService.Decide(player, forShelf, HaulChoice.Shelve, out _, out _) && player.Shelf.Count == shelf + 1 && !player.StashInventory.Contains(forShelf), "shelving failed");
        Check(HomeEveningService.Decide(player, forHer, HaulChoice.Give, out _, out WishResult wish) && wish != null && player.ActiveWishID == null, "giving her the wished item did not grant the wish");
        Check(!HomeEveningService.Decide(player, forStash, HaulChoice.Sell, out _, out _), "a decided item was decided again");
        Check(HomeEveningService.Done(player, EveningStamp.Unpack), "unpack not done after every item was placed");

        // Then care, then the business.
        Check(HomeEveningService.Next(player) == EveningStamp.Care, $"after unpacking the next stamp is {HomeEveningService.Next(player)}");
        doll.Status.Grime = 0;
        Check(HomeEveningService.Next(player) == EveningStamp.Business, $"after care the next stamp is {HomeEveningService.Next(player)}");

        // 精神好: clean at closing and talked to that evening.
        player.TalkedDay = player.CurrentDay;
        int night = player.CurrentDay;
        DayCycleService.FinishNight(player);
        DayCycleService.TakePendingReport();
        Check(player.CleanNightDay == night, $"clean night recorded as {player.CleanNightDay}, expected {night}");
        Check(player.TonightHaul.Count == 0, "haul survived the closing");
        Check(HomeEveningService.IsSpirited(player), "not spirited the morning after a clean, talked-to night");
        string morning = HomeEveningService.MorningLine(player);
        Check(morning.Contains("准备好了") || morning.Contains("走远一点"), "morning line: " + morning);
        DiveLogService.Begin(player, 1, false);
        Check(DiveLogService.Current.Spirited, "the dive did not start spirited");
        Check(HomeEveningService.TakeSpirit() && !HomeEveningService.TakeSpirit(), "the +1 AP did not apply exactly once");

        // Not talked to: no 精神好.
        player.TalkedDay = 0;
        Check(!HomeEveningService.IsSpirited(player), "spirited without talking");

        // The haul is part of the save.
        player.TonightHaul = new List<string> { forStash.InstanceID };
        SaveFile restored = SaveService.Deserialize(SaveService.Serialize(player));
        Check(restored.Player.TonightHaul.Count == 1 && restored.Player.CleanNightDay == night, "haul or clean night lost in the save");

        if (failures.Count == 0) {
            Debug.Log("Home Thread PASSED.");
        } else {
            Debug.LogError("Home Thread FAILED: " + string.Join("; ", failures));
        }

        Debug.Log("=== Home Thread Test Finished ===");
    }

    private static ItemEntity Stash(PlayerProfile player, string configID) {
        ItemEntity item = ConfigManager.CreateItem(configID);
        ItemLifecycleService.MarkGroundInventoryItem(player, item, "Test");
        return item;
    }
}
