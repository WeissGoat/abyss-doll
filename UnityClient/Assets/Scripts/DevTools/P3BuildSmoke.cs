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
            if (ScenePanel.IsShowing && !shallowCaptured && GameRoot.Core.Dungeon.CurrentLayer != null && !PrologueReturnHome.IsPending(GameRoot.Core.Dungeon)) {
                shallowCaptured = true;
                yield return Capture("01b_shallow_edge");
            } else if (ScenePanel.IsShowing && !returnCaptured && PrologueReturnHome.IsPending(GameRoot.Core.Dungeon)) {
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
        while (dives < TargetDives && Time.realtimeSinceStartup - startedAt < TimeoutSeconds) {
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
                // Night: dump the loot and open the business like a player, so the build checks the night flow too.
                Note(P3DevTools.DumpAllForBusiness());
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Capture($"0{3 + dives}_business_after_dive_{dives}");
                int moneyBefore = GameRoot.Core.CurrentPlayer.Money;
                bool wasNight = GameRoot.Core.CurrentPlayer.IsNight;
                Note(P3DevTools.ResolveDayDialogs());
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
        // Every dive and every rest takes one day.
        bool passed = prologueDone && !stuck && dives >= TargetDives && player.CurrentDay == firstTownDay + dives + rests;
        string verdict = $"{(passed ? "PASS" : "FAIL")}: prologue {(prologueDone ? "done" : "NOT FINISHED")} (town from day {firstTownDay}), {dives}/{TargetDives} dives, {rests} rest(s), day {player.CurrentDay}, money {player.Money} (night business net {soldTotal}G), "
            + $"{Time.realtimeSinceStartup - startedAt:0}s{(stuck ? ", stuck (see the last report below)" : string.Empty)}";
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
