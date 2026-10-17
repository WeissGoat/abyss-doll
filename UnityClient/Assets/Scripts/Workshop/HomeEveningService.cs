using System;
using System.Collections.Generic;
using System.Linq;

// The three rooms of the home (HomeRoomNavigator draws them).
public enum HomeRoom {
    Hall = 0,
    Studio = 1,
    Attic = 2
}

public enum EveningStamp {
    Unpack = 0,
    Care = 1,
    Business = 2,
    Goodnight = 3
}

public enum HaulChoice {
    Sell,
    Keep,
    Shelve,
    Give
}

// One evening at home as a thread (docs/specs/2026-10-18-home-evening-thread.md, A): four stamps (卸货 · 照看 · 开张 ·
// 晚安), unpacking what the dive brought home at the counter, Zero's word on entering a room and at the door in the
// morning, and 精神好 (+1 AP on the first turn of the next dive's first fight when she was clean at closing and the
// player talked with her).
public static class HomeEveningService {
    public const string RoomPool = "room";
    public const string HaulPool = "haul";
    public const string MorningPool = "morning";
    public const string ShelvedLine = "（零号看了它很久。）";
    public const int SpiritAP = 1;
    public const int ValuableValue = 150;

    // The dive came home: what it brought is tonight's to sort.
    public static void RecordHaul(PlayerProfile player, DiveLog log) {
        if (player == null || log == null || log.Prologue) {
            return;
        }

        player.TonightHaul = new List<string>(log.BroughtOutIDs ?? new List<string>());
    }

    // What is still undecided, as stash items (anything that already left the stash counts as decided).
    public static List<ItemEntity> Haul(PlayerProfile player) {
        if (player?.TonightHaul == null || player.StashInventory == null) {
            return new List<ItemEntity>();
        }

        List<ItemEntity> items = player.StashInventory.Where(item => item != null && player.TonightHaul.Contains(item.InstanceID)).ToList();
        player.TonightHaul.RemoveAll(id => items.All(item => item.InstanceID != id));
        return items;
    }

    public static bool Done(PlayerProfile player, EveningStamp stamp) {
        if (player == null) {
            return false;
        }

        switch (stamp) {
            case EveningStamp.Unpack:
                return Haul(player).Count == 0;
            case EveningStamp.Care:
                return (player.ActiveDoll?.Status.Grime ?? 0) == 0;
            case EveningStamp.Business:
                return !player.IsNight;
            default:
                return false;
        }
    }

    // The first stamp not done yet, in order.
    public static EveningStamp Next(PlayerProfile player) {
        foreach (EveningStamp stamp in new[] { EveningStamp.Unpack, EveningStamp.Care, EveningStamp.Business }) {
            if (!Done(player, stamp)) {
                return stamp;
            }
        }

        return EveningStamp.Goodnight;
    }

    // What the player can do with one haul item; Give only when it fits her wish.
    public static List<HaulChoice> Choices(PlayerProfile player, ItemEntity item) {
        List<HaulChoice> choices = new List<HaulChoice>();
        if (NightBusinessService.CanSell(item)) {
            choices.Add(HaulChoice.Sell);
        }

        choices.Add(HaulChoice.Keep);
        choices.Add(HaulChoice.Shelve);
        if (WishService.Offerable(player).Contains(item)) {
            choices.Add(HaulChoice.Give);
        }

        return choices;
    }

    // 卖掉 puts it at the showcase's first free spot, or in the dump box when the showcase is full (the dump box takes
    // no contraband). 收着 leaves it in the stash, 架子 puts it on the attic shelf, 给她 grants her wish.
    public static bool Decide(PlayerProfile player, ItemEntity item, HaulChoice choice, out string message, out WishResult wish) {
        message = string.Empty;
        wish = null;
        if (player == null || item == null || !Haul(player).Contains(item)) {
            message = "这件东西已经有去处了。";
            return false;
        }

        bool done;
        switch (choice) {
            case HaulChoice.Sell:
                done = NightBusinessService.TryMoveToShowcase(player, item, out string showcaseReason);
                if (done) {
                    message = $"{item.Name} 摆进了橱窗。";
                } else {
                    done = NightBusinessService.TryMoveToDumpBox(player, item, out string dumpReason);
                    message = done ? $"橱窗满了，{item.Name} 放进倾倒箱。" : dumpReason ?? showcaseReason;
                }
                break;
            case HaulChoice.Keep:
                done = true;
                message = $"{item.Name} 收进仓库。";
                break;
            case HaulChoice.Shelve:
                done = MementoService.Keep(player, item, ShelvedLine) != null;
                message = done ? $"{item.Name} 摆上了阁楼的架子。" : "摆不上去。";
                break;
            case HaulChoice.Give:
                wish = WishService.Give(player, item);
                done = wish != null;
                message = done ? string.Empty : "她现在不需要这个。";
                break;
            default:
                done = false;
                break;
        }

        if (done) {
            player.TonightHaul.Remove(item.InstanceID);
        }

        return done;
    }

    // 剩下的都收着: everything undecided stays in the stash.
    public static int KeepAll(PlayerProfile player) {
        int count = Haul(player).Count;
        player?.TonightHaul?.Clear();
        return count;
    }

    // The business closes the night: anything not sorted simply stays in the stash.
    public static void CloseNight(PlayerProfile player) {
        if (player == null) {
            return;
        }

        player.TonightHaul?.Clear();
        if ((player.ActiveDoll?.Status.Grime ?? 1) == 0) {
            player.CleanNightDay = player.CurrentDay;
        }
    }

    // A dive today starts 精神好 when she was clean at last night's closing and the player talked with her last night
    // or this morning.
    public static bool IsSpirited(PlayerProfile player) {
        if (player == null || player.CurrentDay <= 1) {
            return false;
        }

        int yesterday = player.CurrentDay - 1;
        return player.CleanNightDay == yesterday && (player.TalkedDay == yesterday || player.TalkedDay == player.CurrentDay);
    }

    // The +1 AP, once: true on the first player turn of a 精神好 dive.
    public static bool TakeSpirit() {
        DiveLog log = DiveLogService.Current;
        if (log == null || !log.Spirited || log.SpiritUsed) {
            return false;
        }

        log.SpiritUsed = true;
        return true;
    }

    // The item she comments on while unpacking: one she wished for, otherwise the most valuable.
    public static ItemEntity Remarkable(PlayerProfile player, List<ItemEntity> haul) {
        if (haul == null || haul.Count == 0) {
            return null;
        }

        List<ItemEntity> wished = WishService.Offerable(player);
        return haul.FirstOrDefault(wished.Contains) ?? haul.OrderByDescending(item => item.BaseValue).First();
    }

    public static string HaulLine(PlayerProfile player, ItemEntity item) {
        if (item == null) {
            return string.Empty;
        }

        HashSet<string> facts = new HashSet<string>();
        foreach (string tag in item.Tags ?? new List<string>()) {
            facts.Add("tag:" + tag);
        }

        facts.Add("type:" + item.ItemType);
        if (item.BaseValue >= ValuableValue) {
            facts.Add("valuable");
        }

        if (WishService.Offerable(player).Contains(item)) {
            facts.Add("wish");
        }

        return ZeroLineService.Pick(HaulPool, facts, player?.CurrentDay ?? 0).Replace("{item}", item.Name);
    }

    // What she says walking into a room: the room, the hour and how she is.
    public static string RoomLine(PlayerProfile player, HomeRoom room) {
        HashSet<string> facts = StateFacts(player);
        facts.Add(room.ToString().ToLowerInvariant());
        facts.Add(player != null && player.IsNight ? "evening" : "morning");
        if (Haul(player).Count > 0) {
            facts.Add("haul");
        }

        DiveLog today = Today(player);
        if (today != null && player.IsNight) {
            facts.UnionWith(DiveLogService.Facts(today));
        }

        return ZeroLineService.Pick(RoomPool, facts, player?.CurrentDay ?? 0);
    }

    // At the door in the morning: how last night went, and 精神好.
    public static string MorningLine(PlayerProfile player) {
        HashSet<string> facts = StateFacts(player);
        if (IsSpirited(player)) {
            facts.Add("spirited");
        }

        // A promise for today comes first at the door.
        if (PromiseService.Active(player) != null) {
            HashSet<string> promised = new HashSet<string> { "promise" };
            if (facts.Contains("spirited")) {
                promised.Add("spirited");
            }

            return ZeroLineService.Pick(MorningPool, promised, player.CurrentDay);
        }

        DiveLog last = player?.DiveJournal != null && player.DiveJournal.Count > 0 ? player.DiveJournal[player.DiveJournal.Count - 1] : null;
        if (last != null && last.Day == (player?.CurrentDay ?? 0) - 1) {
            facts.Add(last.Defeated ? "after_defeat" : "after_dive");
        } else {
            facts.Add("after_rest");
        }

        return ZeroLineService.Pick(MorningPool, facts, player?.CurrentDay ?? 0);
    }

    // How she is right now, shared by the room, morning, chat and touch pools.
    public static HashSet<string> StateFacts(PlayerProfile player) {
        HashSet<string> facts = new HashSet<string>();
        DollEntity doll = player?.ActiveDoll;
        if (doll == null) {
            return facts;
        }

        for (int level = 1; level <= BondService.Level(doll); level++) {
            facts.Add("bond" + level);
        }

        if (doll.Status.SAN_Max > 0 && doll.Status.SAN_Current < doll.Status.SAN_Max * ZeroLineService.LowSANShare) {
            facts.Add("low_san");
        }

        if (doll.Status.HP_Max > 0 && doll.Status.HP_Current < doll.Status.HP_Max * ZeroLineService.HurtShare) {
            facts.Add("hurt");
        }

        if (doll.Status.WearAndTear >= ZeroLineService.WornThreshold || doll.Status.Corruption >= ZeroLineService.WornThreshold) {
            facts.Add("worn");
        }

        if (doll.Status.Grime > 0) {
            facts.Add("grimy");
        }

        if (WishService.Active(player) != null) {
            facts.Add("wish");
        }

        DayCalendarInfo calendar = DayCycleService.GetCalendar(player);
        if (calendar.RentCountdown <= 2) {
            facts.Add("rent_soon");
        }

        return facts;
    }

    private static DiveLog Today(PlayerProfile player) {
        List<DiveLog> journal = player?.DiveJournal;
        DiveLog last = journal != null && journal.Count > 0 ? journal[journal.Count - 1] : null;
        return last != null && last.Day == player.CurrentDay ? last : null;
    }
}
