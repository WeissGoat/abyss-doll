using System;
using System.Collections;
using System.IO;
using System.Text;
using UnityEngine;

// Self-check for a built player, started with `P3.exe -p3smoke`: screenshots the prologue, skips it, plays three
// dives through P3DevTools the way a player would (layer select, pre-dive loadout, map, combat, loot, back to town,
// selling loot in the ledger), screenshots the key screens, then writes P3Smoke.txt next to the screenshots and
// quits (exit code 0 = PASS).
// Without the flag it does nothing. tools/build/Test-WindowsBuild.ps1 runs it.
public class P3BuildSmoke : MonoBehaviour {
    private const string Flag = "-p3smoke";
    private const int TargetDives = 3;
    private const float TimeoutSeconds = 600f;

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
        yield return new WaitForSecondsRealtime(4f);
        yield return Capture("01_prologue");
        Note(P3DevTools.SkipPrologue());
        yield return new WaitForSecondsRealtime(1f);
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
        while (dives < TargetDives && Time.realtimeSinceStartup - startedAt < TimeoutSeconds) {
            string report = P3DevTools.RunDive();
            Note(report);
            if (report.Contains("Rested:")) {
                rests++;
            }
            if (report.Contains("Dive finished.")) {
                dives++;
                yield return new WaitForSecondsRealtime(0.5f);
                // Back in town: sell the loot through the ledger like a player, so the build checks selling too.
                Note(P3DevTools.ResolveDayDialogs());
                int moneyBefore = GameRoot.Core.CurrentPlayer.Money;
                P3DevTools.Click("账本");
                yield return null;
                P3DevTools.Click("出售全部战利品");
                yield return new WaitForSecondsRealtime(0.5f);
                yield return Capture($"0{3 + dives}_ledger_after_dive_{dives}");
                soldTotal += GameRoot.Core.CurrentPlayer.Money - moneyBefore;
                Note($"[sell] ledger income {GameRoot.Core.CurrentPlayer.Money - moneyBefore}G");
                P3DevTools.Click("关闭");
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
        bool passed = !stuck && dives >= TargetDives && player.CurrentDay == 1 + dives + rests;
        string verdict = $"{(passed ? "PASS" : "FAIL")}: {dives}/{TargetDives} dives, {rests} rest(s), day {player.CurrentDay}, money {player.Money} (ledger sales {soldTotal}G), "
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
