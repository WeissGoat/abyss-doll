using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class HomeLine {
    public List<string> Tags = new List<string>();
    public string Text;
}

// A pool of Zero's lines at home (配置表(JSON)/Home, see its README).
[Serializable]
public class HomeLinePool {
    public string PoolID;
    public List<HomeLine> Lines = new List<HomeLine>();
}

// Picks what Zero says at home from the facts of the day: the most specific line whose tags all hold, rotating by
// day among equally specific ones (so the same evening always reads the same).
public static class ZeroLineService {
    public const string ArrivalPool = "arrival";
    public const string NightPool = "night";
    public const float LowSANShare = 0.3f;
    public const float HurtShare = 0.5f;
    public const float WornThreshold = 60f;
    public const int GoodSalesIncome = 300;

    public static string Pick(string poolID, ICollection<string> facts, int day) {
        if (!ConfigManager.HomeLines.TryGetValue(poolID ?? string.Empty, out HomeLinePool pool) || pool?.Lines == null) {
            return string.Empty;
        }

        List<HomeLine> matching = pool.Lines
            .Where(line => line != null && !string.IsNullOrEmpty(line.Text) && (line.Tags ?? new List<string>()).All(facts.Contains))
            .ToList();
        if (matching.Count == 0) {
            return string.Empty;
        }

        int best = matching.Max(line => line.Tags?.Count ?? 0);
        List<HomeLine> top = matching.Where(line => (line.Tags?.Count ?? 0) == best).ToList();
        return top[((day % top.Count) + top.Count) % top.Count].Text;
    }

    // A wish the dive fulfilled speaks first, then the promise it carried; otherwise the most memorable thing of the dive.
    public static string ArrivalLine(DiveLog log) {
        if (log == null) {
            return string.Empty;
        }

        if (!string.IsNullOrEmpty(log.WishDoneLine)) {
            return log.WishDoneLine;
        }

        return !string.IsNullOrEmpty(log.PromiseLine) ? log.PromiseLine : Pick(ArrivalPool, DiveLogService.Facts(log), log.Day);
    }

    // A wish granted today (the rest-day one) speaks first; otherwise the day's facts pick.
    public static string NightLine(DayEndReport report, PlayerProfile player) {
        if (report == null) {
            return string.Empty;
        }

        return !string.IsNullOrEmpty(report.WishDone?.Wish?.DoneLine) ? report.WishDone.Wish.DoneLine : Pick(NightPool, NightFacts(report, player), report.EndedDay);
    }

    public static HashSet<string> NightFacts(DayEndReport report, PlayerProfile player) {
        HashSet<string> facts = new HashSet<string>();
        if (report == null) {
            return facts;
        }

        if (report.Choice == DayChoice.Rest) {
            facts.Add("rest");
        } else {
            facts.Add("dive");
            DiveLog log = report.DiveLog;
            if (log != null) {
                facts.UnionWith(DiveLogService.Facts(log));
            } else if (report.DiveDefeated) {
                facts.Add("defeated");
            }
        }

        MonthlyRentSettlementReport rent = report.Rent;
        if (rent != null) {
            facts.Add(rent.Status == MonthlyRentSettlementStatus.Paid ? "rent_paid" : "rent_short");
        } else if (report.Economy != null && report.Economy.RentCountdown <= 2) {
            facts.Add("rent_soon");
        }

        BusinessReport business = report.Business;
        if (business != null) {
            if (business.Lines.Count == 0) {
                facts.Add("no_sales");
            } else if (business.Total >= GoodSalesIncome) {
                facts.Add("good_sales");
            }
        }

        if (player != null && player.LastWishDoneDay == report.EndedDay) {
            facts.Add("wish_done");
        }

        DollEntity doll = player?.ActiveDoll;
        for (int level = 1; level <= BondService.Level(doll); level++) {
            facts.Add("bond" + level);
        }

        if (doll != null) {
            if (doll.Status.SAN_Max > 0 && doll.Status.SAN_Current < doll.Status.SAN_Max * LowSANShare) {
                facts.Add("low_san");
            }

            if (doll.Status.HP_Max > 0 && doll.Status.HP_Current < doll.Status.HP_Max * HurtShare) {
                facts.Add("hurt");
            }

            if (doll.Status.WearAndTear >= WornThreshold || doll.Status.Corruption >= WornThreshold) {
                facts.Add("worn");
            }

            if (doll.Status.Grime >= 3) {
                facts.Add("grimy");
            }
        }

        return facts;
    }
}
