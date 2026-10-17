using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class WishConfig {
    public string WishID;
    public string Text;
    public string Hint;
    public string Kind;
    public List<string> Tags = new List<string>();
    public string Param;
    public int MinDay;
    public int Days = 4;
    public string DoneLine;
}

[Serializable]
public class MilestoneConfig {
    public string MilestoneID;
    public string Name;
    public string IconID;
    public string Line;
}

// A file in 配置表(JSON)/Home: a line pool or the wish catalog.
[Serializable]
public class HomeConfigFile {
    public string PoolID;
    public List<HomeLine> Lines;
    public List<WishConfig> Wishes;
    public List<MilestoneConfig> Milestones;
    public List<HomeUpgradeConfig> Upgrades;
}

// One thing on the attic shelf: a wish's gift, a milestone, or something the player put there.
[Serializable]
public class Memento {
    public string MementoID;
    public string Name;
    public string IconID;
    public int Day;
    public string Line;
}

// The attic shelf (docs/specs/2026-10-17-home-loop.md, batch 4): milestones appear on their own, once.
public static class MementoService {
    public const int BaseShelfSlots = 6;
    public const int MaxShelfSlots = 9;

    // Shelf room: 6, more with the second shelf (HomeUpgradeService).
    public static int ShelfSlots(PlayerProfile player) {
        return Math.Min(MaxShelfSlots, BaseShelfSlots + (int)HomeUpgradeService.Value(player, "ShelfSlots"));
    }

    public static Memento Add(PlayerProfile player, string id, string name, string iconID, string line) {
        if (player == null) {
            return null;
        }

        player.Shelf = player.Shelf ?? new List<Memento>();
        Memento memento = new Memento { MementoID = id, Name = name, IconID = iconID, Day = player.CurrentDay, Line = line };
        player.Shelf.Add(memento);
        return memento;
    }

    // The first boss, the first defeat, the first rent paid, the first wish: each puts something on the shelf once.
    public static Memento Milestone(PlayerProfile player, string milestoneID) {
        if (player == null || string.IsNullOrEmpty(milestoneID)) {
            return null;
        }

        player.Milestones = player.Milestones ?? new List<string>();
        if (player.Milestones.Contains(milestoneID)) {
            return null;
        }

        MilestoneConfig config = ConfigManager.Milestones.FirstOrDefault(m => m != null && m.MilestoneID == milestoneID);
        if (config == null) {
            return null;
        }

        player.Milestones.Add(milestoneID);
        return Add(player, milestoneID, config.Name, config.IconID, config.Line);
    }

    // Puts a stash item on the shelf for good: it leaves the stash and is not sold.
    public static Memento Keep(PlayerProfile player, ItemEntity item, string line) {
        if (player?.StashInventory == null || item == null || !player.StashInventory.Remove(item)) {
            return null;
        }

        return Add(player, item.ConfigID, item.Name, VisualAssetService.ResolveItemIconID(item), line);
    }
}

public class WishResult {
    public WishConfig Wish;
    public BondChange Bond;
    public Memento Memento;
}

// Zero's small wishes (docs/specs/2026-10-17-home-loop.md, batch 4): one at most, appearing some mornings, done at
// home (an item handed to her, a day of rest, a wipe) or by what a dive did (a node visited, a layer reached, the boss,
// no hurt). Done: bond +2, something on the shelf, her line. Not done in time: let go without a penalty.
public static class WishService {
    public const int FirstDay = 3;
    public const int GapDays = 1;
    public const int BondReward = 2;

    public static WishConfig Active(PlayerProfile player) {
        if (player == null || string.IsNullOrEmpty(player.ActiveWishID)) {
            return null;
        }

        return ConfigManager.Wishes.FirstOrDefault(wish => wish != null && wish.WishID == player.ActiveWishID);
    }

    // A new morning: let an old wish go, and maybe a new one appears. Returns the new wish, if any.
    public static WishConfig Morning(PlayerProfile player, out WishConfig expired) {
        expired = null;
        if (player == null) {
            return null;
        }

        WishConfig active = Active(player);
        if (active != null) {
            if (player.CurrentDay - player.WishSinceDay < Math.Max(1, active.Days)) {
                return null;
            }

            expired = active;
            Clear(player);
        }

        if (player.CurrentDay < FirstDay || player.CurrentDay - player.LastWishEndDay <= GapDays && player.LastWishEndDay > 0) {
            return null;
        }

        List<WishConfig> candidates = ConfigManager.Wishes
            .Where(wish => wish != null && !player.DoneWishes.Contains(wish.WishID) && player.CurrentDay >= wish.MinDay && Fits(player, wish))
            .ToList();
        if (candidates.Count == 0) {
            return null;
        }

        WishConfig picked = candidates[player.CurrentDay * 7 % candidates.Count];
        player.ActiveWishID = picked.WishID;
        player.WishSinceDay = player.CurrentDay;
        return picked;
    }

    // Items in the stash she would take for the active wish.
    public static List<ItemEntity> Offerable(PlayerProfile player) {
        WishConfig wish = Active(player);
        if (wish == null || wish.Kind != "BringTag" || player?.StashInventory == null) {
            return new List<ItemEntity>();
        }

        return player.StashInventory.Where(item => item != null && item.Tags != null && item.Tags.Any(tag => wish.Tags.Contains(tag))).ToList();
    }

    public static WishResult Give(PlayerProfile player, ItemEntity item) {
        WishConfig wish = Active(player);
        if (wish == null || !Offerable(player).Contains(item)) {
            return null;
        }

        WishResult result = Complete(player, wish, false);
        result.Memento = MementoService.Keep(player, item, wish.DoneLine);
        return result;
    }

    // What a dive did: visiting, reaching, the boss, coming back unhurt.
    public static WishResult OnDive(PlayerProfile player, DiveLog log) {
        WishConfig wish = Active(player);
        if (wish == null || log == null || log.Prologue) {
            return null;
        }

        bool done;
        switch (wish.Kind) {
            case "VisitNode":
                done = log.Steps.Any(step => step != null && step.Kind == wish.Param);
                break;
            case "ReachLayer":
                done = int.TryParse(wish.Param, out int layer) && log.LayerID >= layer;
                break;
            case "BossDefeated":
                done = log.BossDefeated;
                break;
            case "Unhurt":
                done = !log.Defeated && log.Steps.Count > 0 && log.Steps.All(step => step == null || !step.Hurt) && log.LowestHP >= log.HPStart;
                break;
            default:
                done = false;
                break;
        }

        if (!done) {
            return null;
        }

        log.WishDoneLine = wish.DoneLine;
        return Complete(player, wish, true);
    }

    public static WishResult OnRest(PlayerProfile player) {
        WishConfig wish = Active(player);
        return wish != null && wish.Kind == "Rest" ? Complete(player, wish, true) : null;
    }

    public static WishResult OnClean(PlayerProfile player) {
        WishConfig wish = Active(player);
        return wish != null && wish.Kind == "Clean" && (player.ActiveDoll?.Status.Grime ?? 1) == 0 ? Complete(player, wish, true) : null;
    }

    private static bool Fits(PlayerProfile player, WishConfig wish) {
        switch (wish.Kind) {
            case "Clean":
                return (player.ActiveDoll?.Status.Grime ?? 0) > 0;
            case "ReachLayer":
                return int.TryParse(wish.Param, out int layer) && player.HighestUnlockedDungeonLayer >= layer - 1;
            default:
                return true;
        }
    }

    private static WishResult Complete(PlayerProfile player, WishConfig wish, bool memento) {
        WishResult result = new WishResult { Wish = wish, Bond = BondService.Add(player.ActiveDoll, BondReward) };
        player.DoneWishes.Add(wish.WishID);
        player.LastWishDoneDay = player.CurrentDay;
        Clear(player);
        if (memento) {
            result.Memento = MementoService.Add(player, wish.WishID, wish.Text.Replace("想要", string.Empty).Replace("想", string.Empty), VisualAssetService.UIIconGiftID, wish.DoneLine);
        }

        MementoService.Milestone(player, "first_wish");
        return result;
    }

    private static void Clear(PlayerProfile player) {
        player.ActiveWishID = null;
        player.LastWishEndDay = player.CurrentDay;
    }
}

// Where a dive meets the home: what the run leaves on Zero, wishes it fulfilled, milestones for the shelf, and the
// bond-3 hold-on during the run.
public static class HomeLoop {
    public const int HoldOnLevel = 3;
    public const int HintLevel = 2;

    public static void AfterDive(PlayerProfile player, DiveLog log) {
        if (player == null || log == null) {
            return;
        }

        CareService.AddGrimeFromDive(player.ActiveDoll, log);
        WishService.OnDive(player, log);
        if (log.BossDefeated && !log.Prologue) {
            MementoService.Milestone(player, "first_boss");
        }

        if (log.Defeated && !log.Prologue) {
            MementoService.Milestone(player, "first_defeat");
        }
    }

    // Once a dive, at bond 3 or more. True means the blow is held off at 1 HP.
    public static bool TryHoldOn(DollEntity doll) {
        DiveLog log = DiveLogService.Current;
        if (log == null || log.HeldOn || BondService.Level(doll) < HoldOnLevel) {
            return false;
        }

        log.HeldOn = true;
        return true;
    }

    // What each bond level opens, for the level-up note and the studio plate.
    public static string UnlockText(int level) {
        switch (level) {
            case 1: return "夜里她会多说一些话";
            case 2: return "安全屋里，她会提醒你前面的路";
            case 3: return "每次下潜一次：致命的一击，她会咬牙撑住（留 1 HP）";
            default: return "她更信任你了";
        }
    }

    // Bond 2: in a safe room or on the stairs she says what she senses on the paths ahead (two steps).
    public static string PathHint(NodeBase node) {
        if (node?.NextNodes == null || node.NextNodes.Count == 0) {
            return string.Empty;
        }

        List<NodeBase> ahead = new List<NodeBase>(node.NextNodes);
        foreach (NodeBase next in node.NextNodes) {
            if (next?.NextNodes != null) {
                ahead.AddRange(next.NextNodes);
            }
        }

        List<string> kinds = ahead.Where(n => n != null).Select(DiveLogService.KindOf).ToList();
        if (kinds.Contains("首领")) return "最下面的那个……就在前面了。";
        if (kinds.Contains("精英")) return "前面有很重的脚步声。可以的话，绕开它。";
        if (kinds.Contains("宝藏")) return "前面有东西在发光。";
        if (kinds.Contains("营地")) return "前面有暖的地方。";
        return "前面很安静。";
    }
}
