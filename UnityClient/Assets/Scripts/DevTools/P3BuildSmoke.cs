using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

// Self-check for a built player, started with `P3.exe -p3smoke`: starts a new game from the title, plays the whole
// prologue like a player (dialogue, the shallow-edge first dive, the return home; P3DevTools.PlayPrologue), plays three
// dives through P3DevTools the way a player would (layer select, pre-dive loadout, map, combat, loot, back to town,
// selling loot in the ledger), screenshots the key screens, then writes P3Smoke.txt next to the screenshots and
// quits (exit code 0 = PASS).
// Without the flag it does nothing. tools/build/Test-WindowsBuild.ps1 runs it.
public class P3BuildSmoke : MonoBehaviour {
    private const string Flag = "-p3smoke";
    private const int TargetDives = 3;
    private const float TimeoutSeconds = 600f;
    private const float PrologueTimeoutSeconds = 240f;

    private readonly StringBuilder _log = new StringBuilder();
    private string _outDir;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void Bootstrap() {
        if (Array.IndexOf(Environment.GetCommandLineArgs(), Flag) < 0) {
            return;
        }

        GameObject root = new GameObject("P3BuildSmoke_Runtime");
        DontDestroyOnLoad(root);
        root.AddComponent<P3BuildSmoke>();
    }

    private IEnumerator Start() {
        _outDir = ResolveOutputDir();
        Directory.CreateDirectory(_outDir);
        float startedAt = Time.realtimeSinceStartup;

        yield return new WaitUntil(() => GameRoot.Core?.CurrentPlayer != null && GameFlowController.Instance != null);
        yield return new WaitUntil(() => TitleScreen.IsShowing || Time.realtimeSinceStartup - startedAt > 30f);
        yield return new WaitForSecondsRealtime(1f);
        yield return Capture("00_title");
        Note(P3DevTools.Click(TitleScreen.NewGameLabel));
        yield return new WaitForSecondsRealtime(4f);
        yield return Capture("01_prologue");

        // The prologue, one PlayPrologue call per beat; screenshots of the first shallow-edge scene and the return home.
        bool prologueDone = false;
        bool shallowCaptured = false;
        bool returnCaptured = false;
        float prologueStartedAt = Time.realtimeSinceStartup;
        while (!prologueDone && Time.realtimeSinceStartup - prologueStartedAt < PrologueTimeoutSeconds) {
            // The shallow edge is still the dive day; the return home plays at night.
            bool night = GameRoot.Core.CurrentPlayer.IsNight;
            if (ScenePanel.IsShowing && !shallowCaptured && !night) {
                shallowCaptured = true;
                yield return Capture("01b_shallow_edge");
            } else if (ScenePanel.IsShowing && !returnCaptured && night) {
                returnCaptured = true;
                yield return Capture("01c_return_home");
            }

            string report = P3DevTools.PlayPrologue(1);
            Note(report);
            prologueDone = report.Contains("Prologue finished.");
            yield return new WaitForSecondsRealtime(report.Contains("animation(s) still playing") ? 3f : 0.4f);
        }

        // The town is rebuilt by a scene reload when the prologue ends.
        yield return new WaitForSecondsRealtime(2f);
        int firstTownDay = GameRoot.Core.CurrentPlayer.CurrentDay;
        yield return Capture("02_workshop");

        // 和她说话: she steps up close as her large portrait with the line's face; a click sends her back.
        Note("[talk] " + P3DevTools.Talk());
        yield return new WaitForSecondsRealtime(0.5f);
        yield return Capture("02a_talk_close_up");
        HomeRoomNavigator talkRooms = FindObjectOfType<WorkshopUIController>()?.Rooms;
        talkRooms?.Zero(talkRooms.Current)?.EndCloseUp();
        yield return null;

        // A CG motion clip decodes and plays in the build (the Editor's test runner cannot be trusted to): the
        // prologue's "start Zero" page, whose close-up panel has a clip.
        Note(P3DevTools.ShowCG("vfx_no0_core_start"));
        yield return new WaitForSecondsRealtime(1.5f);
        long motionFrame = -1;
        foreach (UIMotionCG motion in FindObjectsOfType<UIMotionCG>()) {
            if (motion.Clip != null && motion.FrameShown) {
                motionFrame = Math.Max(motionFrame, motion.Frame);
            }
        }

        bool motionPlayed = motionFrame >= 10;
        Note($"[motion CG] frame {motionFrame} after 1.5 s: " + P3DevTools.MotionCG());
        yield return Capture("02b_motion_cg");
        Note(P3DevTools.HideCG());
        yield return null;

        P3DevTools.Click(TownDayHUD.DiveLabel);
        yield return null;
        P3DevTools.Click("开始下潜");
        yield return new WaitForSecondsRealtime(0.5f);
        yield return Capture("03_loadout");

        int dives = 0;
        int rests = 0;
        int soldTotal = 0;
        bool stuck = false;
        string lastReport = null;
        int repeats = 0;
        bool combatCaptured = false;
        bool journalCaptured = false;
        int journalPages = 0;
        bool unpackCaptured = false;
        int unpacked = 0;
        int talks = 0;
        int replies = 0;
        int portraits = 0;
        while (dives < TargetDives && Time.realtimeSinceStartup - startedAt < TimeoutSeconds) {
            // Home again: the dive's journal page is open in the hall (2026-10-17 home loop).
            HomeRoomNavigator rooms = FindObjectOfType<WorkshopUIController>()?.Rooms;
            if (!journalCaptured && rooms != null && rooms.Journal != null && rooms.Journal.IsShowing && rooms.Journal.IsArrival) {
                journalCaptured = true;
                yield return Capture("03c_journal_page");
            }

            string report = P3DevTools.RunDive(40, !combatCaptured);
            Note(report);
            if (report.Contains("Combat ready.")) {
                // The first player turn: enemy intents with numbers and the incoming-damage preview (M2 batch 6).
                combatCaptured = true;
                yield return new WaitForSecondsRealtime(1f);
                yield return Capture("03b_combat");
                continue;
            }
            if (report.Contains("Rested:")) {
                rests++;
            }
            if (report.Contains("Dive finished.")) {
                dives++;
                yield return new WaitForSecondsRealtime(0.5f);
                // Back in town: sell the loot through the ledger like a player, so the build checks selling too.
                // Night: unpack at the counter (2026-10-18 evening thread), dump the rest and open the business like a
                // player, so the build checks the night flow too.
                if (!unpackCaptured && HomeEveningService.Haul(GameRoot.Core.CurrentPlayer).Count > 0) {
                    unpackCaptured = true;
                    FindObjectOfType<WorkshopUIController>()?.Rooms?.OpenCounter();
                    yield return new WaitForSecondsRealtime(0.5f);
                    yield return Capture("03d_unpack");
                }

                // A word with her every evening (2026-10-18), so 精神好 is checked in the build too.
                string talk = P3DevTools.Talk();
                Note(talk);
                if (talk.Contains("Zero says:")) {
                    talks++;
                }

                int haulBefore = HomeEveningService.Haul(GameRoot.Core.CurrentPlayer).Count;
                Note(P3DevTools.Unpack());
                unpacked += haulBefore - HomeEveningService.Haul(GameRoot.Core.CurrentPlayer).Count;
                Note(P3DevTools.DumpAllForBusiness());
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Capture($"0{3 + dives}_business_after_dive_{dives}");
                int moneyBefore = GameRoot.Core.CurrentPlayer.Money;
                bool wasNight = GameRoot.Core.CurrentPlayer.IsNight;
                string night = P3DevTools.ResolveDayDialogs();
                Note(night);
                if (night.Contains("Replied at night")) {
                    replies++;
                    portraits += night.Contains("(face ") ? 1 : 0;
                }
                soldTotal += GameRoot.Core.CurrentPlayer.Money - moneyBefore;
                Note($"[business] night={wasNight}, income incl. rent {GameRoot.Core.CurrentPlayer.Money - moneyBefore}G");
                yield return null;
                continue;
            }

            // The same report 40 times in a row (about 20 s) with no animations left to play means nothing is moving.
            repeats = report == lastReport && VisualQueue.Count == 0 ? repeats + 1 : 0;
            lastReport = report;
            if (report.Contains("Stopped:") || repeats >= 40) {
                stuck = true;
                break;
            }

            // Animations or a screen that reacts a frame later; give the game time to catch up.
            yield return new WaitForSecondsRealtime(report.Contains("animation(s) still playing") ? 3f : 0.5f);
        }

        PlayerProfile player = GameRoot.Core.CurrentPlayer;
        int dayAfterDives = player.CurrentDay;

        // The end of the Demo (2026-10-24): layer 3 with easy fights (the route, not the numbers), the comic at the
        // stairs, her song line at the door, the ending's night talk and 「第一章 · 完」.
        bool comicCaptured = false;
        bool doorCaptured = false;
        bool endCardShown = false;
        if (!stuck) {
            Note(P3DevTools.PrepareDemoEnding());
            for (int step = 0; step < 120 && !endCardShown && Time.realtimeSinceStartup - startedAt < TimeoutSeconds + 120f; step++) {
                HomeRoomNavigator rooms = FindObjectOfType<WorkshopUIController>()?.Rooms;
                if (!comicCaptured && DemoEndingComic.IsPlaying) {
                    comicCaptured = true;
                    yield return new WaitForSecondsRealtime(0.5f);
                    yield return Capture("07_demo_end_comic");
                }

                if (!doorCaptured && rooms != null && rooms.Journal != null && rooms.Journal.IsShowing && rooms.Journal.IsArrival && DemoEndingService.Pending(player)) {
                    doorCaptured = true;
                    yield return Capture("07b_demo_end_door");
                }

                string report = P3DevTools.RunDive(40);
                Note("[ending] " + report);
                if (report.Contains("Dive finished.")) {
                    yield return new WaitForSecondsRealtime(0.5f);
                    rooms = FindObjectOfType<WorkshopUIController>()?.Rooms;
                    if (!doorCaptured && rooms != null && rooms.Journal != null && rooms.Journal.IsShowing) {
                        doorCaptured = true;
                        yield return Capture("07b_demo_end_door");
                    }

                    Note(P3DevTools.Unpack());
                    Note(P3DevTools.DumpAllForBusiness());
                    yield return new WaitForSecondsRealtime(0.5f);
                    string night = P3DevTools.ResolveDayDialogs();
                    Note("[ending night] " + night);
                    if (DemoEndCard.IsShowing) {
                        endCardShown = true;
                        yield return new WaitForSecondsRealtime(0.5f);
                        yield return Capture("07c_demo_end_card");
                        DemoEndCard.Active.Close();
                    }

                    break;
                }

                if (report.Contains("Stopped:")) {
                    break;
                }

                yield return new WaitForSecondsRealtime(report.Contains("animation(s) still playing") ? 3f : 0.5f);
            }

            P3DevTools.EasyFights = false;
        }

        bool endingDone = comicCaptured && endCardShown && player.DemoEndingSeen;
        journalPages = player.DiveJournal?.Count ?? 0;
        // Every dive and every rest takes one day; every dive leaves a journal page.
        bool journalKept = journalPages >= dives;
        // Her large night-talk portrait answers with a face whenever there is a reply.
        bool portraitShown = portraits == replies;
        bool passed = prologueDone && motionPlayed && journalKept && portraitShown && endingDone && !stuck && dives >= TargetDives && dayAfterDives == firstTownDay + dives + rests;
        string verdict = $"{(passed ? "PASS" : "FAIL")}: prologue {(prologueDone ? "done" : "NOT FINISHED")} (town from day {firstTownDay}), {dives}/{TargetDives} dives, {rests} rest(s), day {player.CurrentDay}, money {player.Money} (night business net {soldTotal}G), "
            + $"CG motion {(motionPlayed ? "plays" : "DID NOT PLAY")}, journal {journalPages} page(s){(journalKept ? string.Empty : " MISSING PAGES")}, unpacked {unpacked} item(s), talked {talks} time(s), {replies} night reply(ies){(portraitShown ? string.Empty : $" but only {portraits} with her portrait")}, ending {(endingDone ? "comic + end card" : $"NOT REACHED (comic {comicCaptured}, card {endCardShown})")}, {Time.realtimeSinceStartup - startedAt:0}s{(stuck ? ", stuck (see the last report below)" : string.Empty)}";
        Debug.Log("[P3BuildSmoke] " + verdict);
        File.WriteAllText(Path.Combine(_outDir, "P3Smoke.txt"), verdict + "\n\n" + _log);
        Application.Quit(passed ? 0 : 1);
    }

    private IEnumerator Capture(string name) {
        yield return new WaitForEndOfFrame();
        ScreenCapture.CaptureScreenshot(Path.Combine(_outDir, name + ".png"));
        yield return null;
        Note($"[screenshot] {name}.png");
    }

    private void Note(string text) {
        _log.AppendLine(text);
        _log.AppendLine("----");
    }

    // `-p3smokeOut <dir>` picks the folder; otherwise P3Smoke/ next to the executable.
    private static string ResolveOutputDir() {
        string[] args = Environment.GetCommandLineArgs();
        int index = Array.IndexOf(args, "-p3smokeOut");
        if (index >= 0 && index + 1 < args.Length) {
            return args[index + 1];
        }

        return Path.Combine(Path.GetDirectoryName(Application.dataPath) ?? ".", "P3Smoke");
    }
}
