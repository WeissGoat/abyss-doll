using UnityEngine;
using UnityEditor;
using System.IO;
using System.Reflection;
using System.Collections.Generic;

[InitializeOnLoad]
public static class AutoTestDaemon {
    private static FileSystemWatcher _watcher;
    private static string _triggerFile;
    private static string _reportFile;
    private static bool _needsRefresh = false;

    static AutoTestDaemon() {
        string logsDir = Path.GetFullPath(Path.Combine(Application.dataPath, "../Logs"));
        if (!Directory.Exists(logsDir)) Directory.CreateDirectory(logsDir);
        
        _triggerFile = Path.Combine(logsDir, ".test_trigger");
        _reportFile = Path.Combine(logsDir, "TestReport.json");

        if (!File.Exists(_triggerFile)) File.WriteAllText(_triggerFile, "");

        // Initialize Watcher
        _watcher = new FileSystemWatcher(logsDir, ".test_trigger");
        _watcher.NotifyFilter = NotifyFilters.LastWrite | NotifyFilters.Size;
        _watcher.Changed += OnFileChanged;
        _watcher.EnableRaisingEvents = true;

        EditorApplication.update += OnEditorUpdate;
    }

    private static void OnFileChanged(object source, FileSystemEventArgs e) {
        _needsRefresh = true;
    }

    private static void OnEditorUpdate() {
        if (_needsRefresh) {
            _needsRefresh = false;
            // Wait slightly for file write lock to clear
            System.Threading.Thread.Sleep(200);
            
            string content = "";
            try { content = File.ReadAllText(_triggerFile).Trim(); } catch { return; }
            
            if (!string.IsNullOrEmpty(content) && content != "DONE") {
                Debug.Log($"[AutoTestDaemon] Received command: {content}. Triggering compilation...");
                
                // Clear trigger to prevent looping
                try { File.WriteAllText(_triggerFile, "DONE"); } catch {}
                
                EditorPrefs.SetString("AutoTestDaemon_PendingCommand", content);
                
                // Force recompile
                AssetDatabase.Refresh();
                
                // If Refresh didn't trigger compile (no files changed), run manually
                if (!EditorApplication.isCompiling) {
                    RunPendingTest();
                }
            }
        }
    }

    [UnityEditor.Callbacks.DidReloadScripts]
    private static void OnScriptsReloaded() {
        RunPendingTest();
    }

    private static void RunPendingTest() {
        string command = EditorPrefs.GetString("AutoTestDaemon_PendingCommand", "");
        if (string.IsNullOrEmpty(command)) return;

        EditorPrefs.SetString("AutoTestDaemon_PendingCommand", "");
        Debug.Log($"[AutoTestDaemon] Executing test command: {command}");

        TestReport report = new TestReport { Command = command, Status = "PASSED", Logs = new List<string>() };
        
        Application.LogCallback logHandler = (condition, stackTrace, type) => {
            report.Logs.Add($"[{type}] {condition}");
            if (type == LogType.Error || type == LogType.Exception || type == LogType.Assert) {
                report.Status = "FAILED";
                report.Logs.Add(stackTrace);
            }
        };

        Application.logMessageReceived += logHandler;

        try {
            if (command.ToUpper() == "RUN_ALL_TESTS") {
                RunAllTests(report);
            } else {
                string[] parts = command.Split('.');
                if (parts.Length >= 2) {
                    string methodName = parts[parts.Length - 1];
                    string className = command.Substring(0, command.LastIndexOf('.'));
                    
                    System.Type targetType = null;
                    foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies()) {
                        targetType = assembly.GetType(className);
                        if (targetType != null) break;
                    }

                    if (targetType != null) {
                        MethodInfo method = targetType.GetMethod(methodName, BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                        if (method != null) {
                            method.Invoke(null, null);
                        } else {
                            report.Logs.Add($"[Error] Method {methodName} not found on {className}");
                            report.Status = "FAILED";
                        }
                    } else {
                        report.Logs.Add($"[Error] Type {className} not found");
                        report.Status = "FAILED";
                    }
                } else {
                    report.Logs.Add("[Error] Invalid command format. Use Namespace.ClassName.MethodName or RUN_ALL_TESTS");
                    report.Status = "FAILED";
                }
            }
        } catch (System.Exception ex) {
            report.Status = "FAILED";
            report.Logs.Add($"[Exception] {(ex.InnerException != null ? ex.InnerException.Message : ex.Message)}\n{ex.StackTrace}");
        } finally {
            Application.logMessageReceived -= logHandler;
        }

        File.WriteAllText(_reportFile, JsonUtility.ToJson(report, true));
        Debug.Log($"[AutoTestDaemon] Test finished with status {report.Status}. Report saved to {_reportFile}");
    }

    private static void RunAllTests(TestReport report) {
        report.Logs.Add("=== Starting Full Regression Test Suite ===");
        int totalTests = 0;
        int passedTests = 0;

        foreach (var assembly in System.AppDomain.CurrentDomain.GetAssemblies()) {
            // 只扫描主要的业务程序集，过滤掉大量引擎和插件DLL提升速度
            if (!assembly.FullName.StartsWith("Assembly-CSharp") && !assembly.FullName.StartsWith("Assembly-CSharp-Editor")) continue;

            foreach (var type in assembly.GetTypes()) {
                if (type.IsClass && type.Name.EndsWith("Test") && !type.Name.Contains("<")) {
                    MethodInfo runMethod = type.GetMethod("Run", BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic);
                    if (runMethod != null) {
                        totalTests++;
                        report.Logs.Add($"\n--- Running Test: {type.Name}.Run() ---");
                        try {
                            runMethod.Invoke(null, null);
                            passedTests++;
                        } catch (System.Exception ex) {
                            report.Status = "FAILED";
                            report.Logs.Add($"[Test Execution Failed] {type.Name}.Run() crashed.");
                            report.Logs.Add($"[Exception] {(ex.InnerException != null ? ex.InnerException.Message : ex.Message)}\n{ex.StackTrace}");
                        }
                    }
                }
            }
        }
        
        report.Logs.Add($"\n=== Regression Test Suite Completed. Passed: {passedTests}/{totalTests} ===");
        if (passedTests < totalTests) {
            report.Status = "FAILED";
        }
    }
}

[System.Serializable]
public class TestReport {
    public string Status;
    public string Command;
    public List<string> Logs;
}