using System.Collections.Generic;
using System.Linq;

public enum TouchRegion {
    Head = 0,
    Shoulder = 1,
    Hand = 2
}

// The small move her portrait makes when she answers (ZeroPresence plays it).
public enum ZeroReaction {
    None = 0,
    Hop = 1,
    Flinch = 2,
    Lean = 3
}

public class ZeroAnswer {
    public string Line;
    // Her face while she says it (ZeroFace), for her large portrait when you talk with her.
    public string Face;
    // One more piece of her own story (lore pool), told on the day's first chat when the bond allows.
    public bool IsLore;
    public ZeroReaction Reaction;
    public string Emote;
    public bool Accepted;
    public BondChange Bond;
}

// Talking with Zero and touching her at home (docs/specs/2026-10-18-home-evening-thread.md, B). The day's first chat
// and the day's first welcome touch give bond +1; later ones only answer. Her story comes a piece at a time on the
// day's first chat, each piece waiting for a bond level. Low SAN makes her shy away from a hand on her head, a hurt
// shoulder makes her wince; neither costs bond (it never goes down).
public static class ZeroInteractionService {
    public const string ChatPool = "chat";
    public const string TouchPool = "touch";
    public const string LorePool = "lore";
    public const int ChatBond = 1;
    public const int TouchBond = 1;
    // After this many touches in a day she has had enough for today (a line, no move).
    public const int TouchLimit = 6;
    public const int LeanLevel = 3;

    public static ZeroAnswer Chat(PlayerProfile player, HomeRoom room) {
        ZeroAnswer answer = new ZeroAnswer { Accepted = true, Reaction = ZeroReaction.Hop };
        if (player?.ActiveDoll == null) {
            return answer;
        }

        Roll(player);
        bool first = player.ChatsToday == 0;
        player.ChatsToday++;
        player.TalkedDay = player.CurrentDay;
        if (first) {
            answer.Bond = BondService.Add(player.ActiveDoll, ChatBond);
            HomeLine lore = NextLoreLine(player);
            if (lore != null) {
                player.LoreHeard++;
                answer.Line = lore.Text;
                answer.Face = lore.Face;
                answer.IsLore = true;
                answer.Reaction = ZeroReaction.None;
                return answer;
            }
        }

        HashSet<string> facts = HomeEveningService.StateFacts(player);
        facts.Add(room.ToString().ToLowerInvariant());
        facts.Add(player.IsNight ? "evening" : "morning");
        if (!first) {
            facts.Add("again");
        }

        HomeLine line = ZeroLineService.PickLine(ChatPool, facts, player.CurrentDay * 7 + player.ChatsToday);
        answer.Line = line?.Text ?? string.Empty;
        answer.Face = line?.Face;
        return answer;
    }

    public static ZeroAnswer Touch(PlayerProfile player, TouchRegion region) {
        ZeroAnswer answer = new ZeroAnswer();
        DollEntity doll = player?.ActiveDoll;
        if (doll == null) {
            return answer;
        }

        Roll(player);
        player.TouchesToday++;
        HashSet<string> state = HomeEveningService.StateFacts(player);
        string regionTag = region.ToString().ToLowerInvariant();
        // A reaction of its own (shying away, wincing, leaning in, enough for today) speaks only for itself; an ordinary
        // touch answers to how she is.
        string kind = null;
        if (player.TouchesToday > TouchLimit) {
            kind = "enough";
            answer.Emote = "……";
        } else if (region == TouchRegion.Head && state.Contains("low_san")) {
            kind = "flinch";
            answer.Reaction = ZeroReaction.Flinch;
            answer.Emote = "！";
        } else if (region == TouchRegion.Shoulder && state.Contains("hurt")) {
            kind = "wince";
            answer.Reaction = ZeroReaction.Flinch;
            answer.Emote = "！";
        } else {
            answer.Accepted = true;
            bool lean = BondService.Level(doll) >= LeanLevel;
            kind = lean ? "lean" : null;
            answer.Reaction = lean ? ZeroReaction.Lean : ZeroReaction.Hop;
            answer.Emote = lean ? "♡" : "♪";
        }

        HashSet<string> facts = kind == null ? state : new HashSet<string>(state.Where(fact => fact.StartsWith("bond"))) { kind };
        if (kind != "enough") {
            facts.Add(regionTag);
        }

        // The day's first welcome touch.
        if (answer.Accepted && player.TouchBondDay != player.CurrentDay) {
            player.TouchBondDay = player.CurrentDay;
            answer.Bond = BondService.Add(doll, TouchBond);
        }

        answer.Line = ZeroLineService.Pick(TouchPool, facts, player.CurrentDay * 5 + player.TouchesToday);
        return answer;
    }

    // The next piece of her story, if the bond has reached it.
    public static HomeLine NextLoreLine(PlayerProfile player) {
        if (!ConfigManager.HomeLines.TryGetValue(LorePool, out HomeLinePool pool) || pool?.Lines == null || player == null) {
            return null;
        }

        if (player.LoreHeard < 0 || player.LoreHeard >= pool.Lines.Count) {
            return null;
        }

        HomeLine next = pool.Lines[player.LoreHeard];
        HashSet<string> facts = HomeEveningService.StateFacts(player);
        return next != null && !string.IsNullOrEmpty(next.Text) && (next.Tags ?? new List<string>()).All(facts.Contains) ? next : null;
    }

    // Counts belong to the day: a new day starts them over.
    private static void Roll(PlayerProfile player) {
        if (player.InteractionDay != player.CurrentDay) {
            player.InteractionDay = player.CurrentDay;
            player.ChatsToday = 0;
            player.TouchesToday = 0;
        }
    }
}
