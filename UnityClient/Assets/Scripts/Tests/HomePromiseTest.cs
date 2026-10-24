using System.Collections.Generic;
using UnityEngine;

// The night talk's replies and the promises they make (docs/specs/2026-10-18-home-evening-thread.md, C): a talk every
// other night, any night something weighs on her; a reply moves the bond and may promise something; the next dive
// tells whether it was kept (kept: bond +1 and her answer at the door; not: only a line); the map shows it.
public static class HomePromiseTest {
    public static void Run() {
        Debug.Log("=== Running Home Promise Test ===");

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();

        PlayerProfile player = core.CurrentPlayer;
        DollEntity doll = player.ActiveDoll;
        List<string> failures = new List<string>();
        void Check(bool condition, string message) {
            if (!condition) {
                failures.Add(message);
            }
        }

        Check(ConfigManager.NightTalks.Count >= 8 && ConfigManager.Promises.Count >= 6, $"{ConfigManager.NightTalks.Count} talks, {ConfigManager.Promises.Count} promises in config");
        bool KnownFace(string face) => string.IsNullOrEmpty(face) || System.Array.IndexOf(ZeroFace.All, face) >= 0;
        foreach (NightTalkConfig talk in ConfigManager.NightTalks) {
            Check(KnownFace(talk.Face), $"{talk.TalkID} has unknown face {talk.Face}");
            foreach (NightReplyConfig reply in talk.Replies) {
                Check(string.IsNullOrEmpty(reply.PromiseID) || ConfigManager.Promises.Exists(p => p.PromiseID == reply.PromiseID), $"{talk.TalkID} promises unknown {reply.PromiseID}");
                Check(KnownFace(reply.AnswerFace), $"{talk.TalkID} answers with unknown face {reply.AnswerFace}");
            }
        }

        foreach (HomeLinePool pool in ConfigManager.HomeLines.Values) {
            foreach (HomeLine line in pool.Lines) {
                Check(KnownFace(line.Face), $"{pool.PoolID} line {line.Text} has unknown face {line.Face}");
            }
        }

        // Story scenes: a face only on Zero's own lines.
        foreach (SceneConfig scene in ConfigManager.Scenes.Values) {
            foreach (SceneLine line in scene.Lines) {
                Check(KnownFace(line.Face) && (string.IsNullOrEmpty(line.Face) || line.Speaker == ScenePanel.ZeroSpeaker), $"{scene.SceneID} line {line.Text} has face {line.Face} for {line.Speaker}");
            }
        }

        doll.Status.HP_Current = doll.Status.HP_Max;
        doll.Status.SAN_Current = doll.Status.SAN_Max;
        player.CurrentDay = 6;

        // A night after a near fall: the talk comes, even right after another one.
        DiveLog nearFall = new DiveLog { Day = 6, LayerID = 1, HPStart = 100, HPEnd = 15, HPMax = 100, LowestHP = 15 };
        DayEndReport report = new DayEndReport { Choice = DayChoice.Dive, EndedDay = 6, DiveLog = nearFall };
        player.LastNightTalkDay = 5;
        NightTalkConfig talkTonight = PromiseService.PickTalk(report, player);
        Check(talkTonight != null && talkTonight.TalkID == "talk_nearly_fell", "near-fall talk: " + talkTonight?.TalkID);

        // An ordinary night right after a talk: no replies.
        DayEndReport quiet = new DayEndReport { Choice = DayChoice.Dive, EndedDay = 7, DiveLog = new DiveLog { Day = 7, LayerID = 1, HPStart = 100, HPEnd = 90, HPMax = 100, LowestHP = 90 } };
        Check(PromiseService.PickTalk(quiet, player) == null, "a quiet night right after a talk still had replies");

        // Reply with the promise: bond +1, the promise is on.
        NightReplyResult answer = PromiseService.Reply(player, report, talkTonight.Replies[0]);
        Check(answer.Promise != null && player.ActivePromiseID == "p_no_strain", "no promise after promising");
        Check(!string.IsNullOrEmpty(answer.Face) && answer.Face == talkTonight.Replies[0].AnswerFace, "her answer lost its face: " + answer.Face);
        Check(answer.Bond != null && answer.Bond.PointsAdded == 1, "the reply gave no bond");
        Check(player.TalkedDay == 6, "the reply did not count as talking that night");
        Check(PromiseService.MapLine(player).Contains("别硬撑"), "map line: " + PromiseService.MapLine(player));
        Check(HomeEveningService.MorningLine(player).Contains("说好"), "morning line with a promise: " + HomeEveningService.MorningLine(player));

        // Kept: the next dive never drops below half.
        DiveLog kept = new DiveLog { Day = 8, HPStart = 100, HPEnd = 70, HPMax = 100, LowestHP = 60 };
        kept.Steps.Add(new DiveLogStep { Kind = "战斗" });
        int keptBefore = player.KeptPromises;
        PromiseService.OnDive(player, kept);
        Check(kept.PromiseKept && kept.PromiseText.Contains("别硬撑") && player.KeptPromises == keptBefore + 1 && player.ActivePromiseID == null, "kept promise not recorded");
        Check(ZeroLineService.ArrivalLine(kept) == "我听话了。没有硬撑。", "arrival line for a kept promise: " + ZeroLineService.ArrivalLine(kept));
        Check(ZeroLineService.ArrivalPick(kept).Face == ZeroFace.Smile, "a kept promise does not make her smile at the door");
        Check(HomeJournalView.BuildBody(kept).Contains("做到了"), "the journal page does not say it was kept");

        // Broken: a promise to visit the camp, a dive that never got there.
        player.ActivePromiseID = "p_camp";
        DiveLog broken = new DiveLog { Day = 9, HPStart = 100, HPEnd = 100, HPMax = 100, LowestHP = 100 };
        broken.Steps.Add(new DiveLogStep { Kind = "宝藏" });
        int points = BondService.Points(doll);
        int level = BondService.Level(doll);
        PromiseService.OnDive(player, broken);
        Check(!broken.PromiseKept && broken.PromiseLine.Contains("营地") && player.ActivePromiseID == null, "broken promise not recorded");
        Check(BondService.Points(doll) == points && BondService.Level(doll) == level, "a broken promise changed the bond");

        // The kinds read the page right.
        Check(PromiseService.Kept(new PromiseConfig { Kind = "ShortDive", Param = "5" }, new DiveLog { Steps = { new DiveLogStep(), new DiveLogStep() } }), "a short dive did not count");
        Check(!PromiseService.Kept(new PromiseConfig { Kind = "ShortDive", Param = "5" }, new DiveLog { Defeated = true }), "a defeat counted as coming back early");
        Check(PromiseService.Kept(new PromiseConfig { Kind = "BringHome", Param = "4" }, new DiveLog { BroughtOut = 4 }), "bringing four home did not count");

        // Saved.
        player.ActivePromiseID = "p_unhurt";
        SaveFile restored = SaveService.Deserialize(SaveService.Serialize(player));
        Check(restored.Player.ActivePromiseID == "p_unhurt" && restored.Player.KeptPromises == player.KeptPromises && restored.Player.LastNightTalkDay == 6, "promise state lost in the save");

        if (failures.Count == 0) {
            Debug.Log("Home Promise PASSED.");
        } else {
            Debug.LogError("Home Promise FAILED: " + string.Join("; ", failures));
        }

        Debug.Log("=== Home Promise Test Finished ===");
    }
}
