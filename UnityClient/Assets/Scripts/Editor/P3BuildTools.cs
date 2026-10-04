using System;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

// Windows builds for playtesting. Output goes to <repo>/Builds/Windows/P3_<yyyyMMdd_HHmm>[_dev]/P3.exe with a
// BUILD_INFO.txt (commit, time, flavour). Three ways in:
//   - Editor menu Tools/P3/打包 Windows 试玩版 (or 开发版).
//   - Unity MCP while the Editor is open: `return P3BuildTools.BuildWindows(false);`.
//   - Editor closed: tools/build/Build-Windows.ps1, which runs BuildWindowsFromCommandLine in batch mode.
// The playtest build plays the prologue and has no F1 panel; the development build has the F1 panel.
public static class P3BuildTools {
    private const string ExeName = "P3.exe";

    [MenuItem("Tools/P3/打包 Windows 试玩版")]
    private static void BuildPlaytestMenu() {
        ShowResult(BuildWindows(false));
    }

    [MenuItem("Tools/P3/打包 Windows 开发版（带 F1 面板）")]
    private static void BuildDevelopmentMenu() {
        ShowResult(BuildWindows(true));
    }

    // Batch mode entry: Unity.exe -batchmode -quit -projectPath UnityClient -executeMethod
    // P3BuildTools.BuildWindowsFromCommandLine [-p3Development]. Exits with code 1 when the build fails.
    public static void BuildWindowsFromCommandLine() {
        bool development = Array.IndexOf(Environment.GetCommandLineArgs(), "-p3Development") >= 0;
        string report = BuildWindows(development);
        Debug.Log(report);
        if (!report.StartsWith("Build succeeded", StringComparison.Ordinal)) {
            EditorApplication.Exit(1);
        }
    }

    // Returns a short text report; the first line starts with "Build succeeded" or "Build failed".
    public static string BuildWindows(bool development) {
        if (EditorApplication.isPlayingOrWillChangePlaymode) {
            return "Build failed: leave Play Mode first.";
        }

        string repoRoot = Path.GetFullPath(Path.Combine(Application.dataPath, "..", ".."));
        string folderName = $"P3_{DateTime.Now:yyyyMMdd_HHmm}{(development ? "_dev" : string.Empty)}";
        string outputDir = Path.Combine(repoRoot, "Builds", "Windows", folderName);
        Directory.CreateDirectory(outputDir);

        BuildPlayerOptions options = new BuildPlayerOptions {
            scenes = EnabledScenes(),
            locationPathName = Path.Combine(outputDir, ExeName),
            target = BuildTarget.StandaloneWindows64,
            targetGroup = BuildTargetGroup.Standalone,
            options = development ? BuildOptions.Development : BuildOptions.None
        };

        BuildReport report = BuildPipeline.BuildPlayer(options);
        BuildSummary summary = report.summary;
        if (summary.result != BuildResult.Succeeded) {
            return $"Build failed: {summary.result}, {summary.totalErrors} error(s). See the Editor log.";
        }

        string commit = ReadGitCommit(repoRoot);
        File.WriteAllText(Path.Combine(outputDir, "BUILD_INFO.txt"), new StringBuilder()
            .AppendLine($"P3 {(development ? "development" : "playtest")} build")
            .AppendLine($"Built: {DateTime.Now:yyyy-MM-dd HH:mm}")
            .AppendLine($"Commit: {commit}")
            .AppendLine($"Unity: {Application.unityVersion}")
            .ToString());

        File.WriteAllText(Path.Combine(outputDir, "跳过序章启动.bat"), "@start \"\" \"%~dp0" + ExeName + "\" -p3skipprologue\r\n");
        File.WriteAllText(Path.Combine(outputDir, "试玩说明.txt"), PlaytestNotes(development), new UTF8Encoding(true));

        return $"Build succeeded: {options.locationPathName}\n"
            + $"Size {summary.totalSize / (1024f * 1024f):0.0} MB, {summary.totalTime.TotalSeconds:0}s, {summary.totalWarnings} warning(s), commit {commit}.";
    }

    private static string PlaytestNotes(bool development) {
        return string.Join("\r\n", new[] {
            "P3 试玩版",
            "",
            "启动：双击 P3.exe，从序章开始。重开时想跳过序章，双击「跳过序章启动.bat」。",
            "全屏 / 窗口：Alt+Enter 切换。退出：Alt+F4（还没有暂停菜单）。",
            "还没有存档：关掉游戏进度就没了。",
            development ? "开发版：按 F1 打开开发者面板。" : "",
            "",
            "试玩时请留意：",
            "- 前 15 分钟有没有哪一步不知道该做什么。",
            "- 战斗、拾取、撤还是继续，哪里有意思，哪里无聊。",
            "- 回城以后想不想再下一次，为什么。",
            "- 卡住、报错、画面不对的地方，记下大概在哪一步，并把 P3.exe 旁边的 Logs 文件夹一起发过来。",
            "",
            "版本信息见 BUILD_INFO.txt。",
            ""
        });
    }

    private static string[] EnabledScenes() {
        return Array.ConvertAll(Array.FindAll(EditorBuildSettings.scenes, scene => scene.enabled), scene => scene.path);
    }

    // Reads HEAD without running git: a branch ref file, packed-refs, or a detached hash.
    private static string ReadGitCommit(string repoRoot) {
        try {
            string gitDir = Path.Combine(repoRoot, ".git");
            string head = File.ReadAllText(Path.Combine(gitDir, "HEAD")).Trim();
            if (!head.StartsWith("ref: ", StringComparison.Ordinal)) {
                return Short(head);
            }

            string refName = head.Substring(5);
            string refPath = Path.Combine(gitDir, refName.Replace('/', Path.DirectorySeparatorChar));
            if (File.Exists(refPath)) {
                return $"{Short(File.ReadAllText(refPath).Trim())} ({refName.Replace("refs/heads/", string.Empty)})";
            }

            foreach (string line in File.ReadAllLines(Path.Combine(gitDir, "packed-refs"))) {
                if (line.EndsWith(" " + refName, StringComparison.Ordinal)) {
                    return $"{Short(line.Substring(0, line.IndexOf(' ')))} ({refName.Replace("refs/heads/", string.Empty)})";
                }
            }
        } catch (Exception e) {
            return "unknown (" + e.Message + ")";
        }

        return "unknown";
    }

    private static string Short(string hash) {
        return hash.Length > 7 ? hash.Substring(0, 7) : hash;
    }

    private static void ShowResult(string report) {
        Debug.Log("[P3BuildTools] " + report);
        EditorUtility.DisplayDialog("P3 打包", report, "好");
    }
}
