using System;
using System.Collections.Generic;
using System.Linq;

[Serializable]
public class NightReplyConfig {
    public string Text;
    public string Answer;
    // Her face while she answers (ZeroFace); empty is neutral.
    public string AnswerFace;
    public int Bond;
    // Set: this reply makes a promise for the next dive (promises.json).
    public string PromiseID;
}

// A night when Zero's word comes with two replies (night_talks.json).
[Serializable]
public class NightTalkConfig {
    public string TalkID;
    public List<string> Tags = new List<string>();
    public string Line;
    // Her face while she says the line (ZeroFace); empty is neutral.
    public string Face;
    public List<NightReplyConfig> Replies = new List<NightReplyConfig>();
}

// Something the player promised her for the next dive (promises.json).
[Serializable]
public class PromiseConfig {
    public string PromiseID;
    public string Text;
    public string Kind;
    public string Param;
    public string KeptLine;
    public string BrokenLine;
}

public class NightReplyResult {
    public string Answer;
    public string Face;
    public BondChange Bond;
    public PromiseConfig Promise;
}

// The night talk's replies and the promises they make (docs/specs/2026-10-18-home-evening-thread.md, C). A talk comes
// every other night or so, and any night something weighs on her (a near fall, a defeat, the rent, low SAN). A reply
// moves the bond by 0 or 1 and may promise one thing for the next dive; the dive's end tells whether it was kept
// (kept: bond +1 and her answer at the door; not kept: only a line). A rest day keeps the promise for the next dive.
public static class PromiseService {
    public const int GapNights = 2;
    public const int KeptBond = 1;
    public static readonly string[] UrgentFacts = { "nearly_fell", "defeated", "rent_short", "low_san" };

    public static PromiseConfig Active(PlayerProfile player) {
        if (player == null || string.IsNullOrEmpty(player.ActivePromiseID)) {
            return null;
        }

        return ConfigManager.Promises.FirstOrDefault(promise => promise != null && promise.PromiseID == player.ActivePromiseID);
    }

    // Tonight's talk with replies, if any. Marks the night so the next one waits.
    public static NightTalkConfig PickTalk(DayEndReport report, PlayerProfile player) {
        if (report == null || player == null || report.EndedDay <= 1 || (report.DiveLog != null && report.DiveLog.Prologue)) {
            return null;
        }

        HashSet<string> facts = ZeroLineService.NightFacts(report, player);
        bool urgent = UrgentFacts.Any(facts.Contains);
        if (!urgent && player.LastNightTalkDay > 0 && report.EndedDay - player.LastNightTalkDay < GapNights) {
            return null;
        }

        List<NightTalkConfig> matching = ConfigManager.NightTalks
            .Where(talk => talk != null && talk.Replies != null && talk.Replies.Count > 0 && (talk.Tags ?? new List<string>()).All(facts.Contains))
            .ToList();
        if (matching.Count == 0) {
            return null;
        }

        int best = matching.Max(talk => talk.Tags?.Count ?? 0);
        List<NightTalkConfig> top = matching.Where(talk => (talk.Tags?.Count ?? 0) == best).ToList();
        player.LastNightTalkDay = report.EndedDay;
        return top[report.EndedDay % top.Count];
    }

    // The player's reply: bond, her answer, and maybe a promise (a new one replaces the old).
    public static NightReplyResult Reply(PlayerProfile player, DayEndReport report, NightReplyConfig reply) {
        NightReplyResult result = new NightReplyResult { Answer = reply?.Answer, Face = reply?.AnswerFace };
        if (player == null || reply == null) {
            return result;
        }

        player.TalkedDay = report?.EndedDay ?? player.CurrentDay;
        if (reply.Bond > 0) {
            result.Bond = BondService.Add(player.ActiveDoll, reply.Bond);
        }

        PromiseConfig promise = ConfigManager.Promises.FirstOrDefault(p => p != null && p.PromiseID == reply.PromiseID);
        if (promise != null) {
            player.ActivePromiseID = promise.PromiseID;
            player.PromiseSinceDay = player.CurrentDay;
            result.Promise = promise;
        }

        return result;
    }

    // The dive is over: was the promise kept? Writes it on the journal page.
    public static void OnDive(PlayerProfile player, DiveLog log) {
        PromiseConfig promise = Active(player);
        if (promise == null || log == null || log.Prologue) {
            return;
        }

        bool kept = Kept(promise, log);
        log.PromiseText = promise.Text;
        log.PromiseKept = kept;
        log.PromiseLine = kept ? promise.KeptLine : promise.BrokenLine;
        player.ActivePromiseID = null;
        if (kept) {
            player.KeptPromises++;
            BondService.Add(player.ActiveDoll, KeptBond);
        }
    }

    public static bool Kept(PromiseConfig promise, DiveLog log) {
        switch (promise?.Kind) {
            case "NoNearFall":
                float share = float.TryParse(promise.Param, System.Globalization.NumberStyles.Float, System.Globalization.CultureInfo.InvariantCulture, out float parsed) ? parsed : 0.5f;
                return !log.Defeated && log.HPMax > 0 && log.LowestHP >= Math.Ceiling(log.HPMax * share);
            case "ShortDive":
                return !log.Defeated && int.TryParse(promise.Param, out int steps) && log.Steps.Count <= steps;
            case "BringHome":
                return int.TryParse(promise.Param, out int count) && log.BroughtOut >= count;
            case "NoLeftBehind":
                return !log.Defeated && log.LeftBehind == 0;
            case "EliteFought":
                return !log.Defeated && log.EliteFought;
            case "VisitNode":
                return log.Steps.Any(step => step != null && step.Kind == promise.Param);
            case "Unhurt":
                return !log.Defeated && log.Steps.Count > 0 && log.Steps.All(step => step == null || !step.Hurt);
            default:
                return false;
        }
    }

    // The line on the dive map: what was promised.
    public static string MapLine(PlayerProfile player) {
        PromiseConfig promise = Active(player);
        return promise == null ? string.Empty : $"和零号的约定：{promise.Text}";
    }
}
