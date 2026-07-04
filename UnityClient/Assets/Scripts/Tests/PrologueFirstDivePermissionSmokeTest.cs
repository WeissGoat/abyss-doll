using System;
using System.Collections.Generic;
using UnityEngine;

public static class PrologueFirstDivePermissionSmokeSuite {
    public static void Run() {
        Debug.Log("=== Running Prologue First Dive Permission Smoke Test ===");

        var diagnostics = new List<string>();
        bool allPassed = true;

        allPassed &= RunStep("NarrativeConfigLoad", RunNarrativeConfigLoadStep, diagnostics);
        allPassed &= RunStep("NarrativeScheduler", NarrativeSchedulerSmokeTest.Run, diagnostics);
        allPassed &= RunStep("NarrativeOverlay", NarrativeOverlaySmokeTest.Run, diagnostics);
        allPassed &= RunStep("PrologueOpening", PrologueOpeningNarrativeFlowSmokeTest.Run, diagnostics);
        allPassed &= RunStep("PrologueDollWake", PrologueDollWakeNarrativeFlowSmokeTest.Run, diagnostics);
        allPassed &= RunStep("PrologueHalfOpen", PrologueHalfOpenNarrativeFlowSmokeTest.Run, diagnostics);
        allPassed &= RunStep("PrologueLayerConfirm", PrologueFirstDiveLayerConfirmSmokeTest.Run, diagnostics);
        allPassed &= RunStep("PrologueDeparture", PrologueFirstDiveDepartureSmokeTest.Run, diagnostics);

        if (allPassed) {
            Debug.Log("Prologue First Dive Permission Smoke PASSED.");
        } else {
            Debug.LogError(
                "Prologue First Dive Permission Smoke FAILED. "
                + $"Diagnostics={string.Join(" || ", diagnostics)}");
        }

        Debug.Log("=== Prologue First Dive Permission Smoke Test Finished ===");
    }

    private static void RunNarrativeConfigLoadStep() {
        ConfigManager.LoadAllConfigs();

        bool loaded = ConfigManager.Narrative != null
            && ConfigManager.Narrative.Nodes != null
            && ConfigManager.Narrative.Triggers != null
            && ConfigManager.Narrative.Speakers != null
            && ConfigManager.Narrative.Flags != null
            && ConfigManager.Narrative.Commands != null
            && ConfigManager.Narrative.Nodes.Count >= 8
            && ConfigManager.Narrative.Triggers.Count >= 8
            && ConfigManager.Narrative.Speakers.ContainsKey("protagonist")
            && ConfigManager.Narrative.Speakers.ContainsKey("no0")
            && ConfigManager.Narrative.Nodes.ContainsKey("T0_01A_Opening_CG")
            && ConfigManager.Narrative.Nodes.ContainsKey("T0_01A_FirstDepart");

        if (loaded) {
            Debug.Log("Prologue Narrative Config Load Step PASSED.");
        } else {
            Debug.LogError("Prologue Narrative Config Load Step FAILED.");
        }
    }

    private static bool RunStep(string stepName, Action action, List<string> diagnostics) {
        int errorCount = 0;
        string firstError = string.Empty;
        bool threw = false;
        string exceptionMessage = string.Empty;

        Application.LogCallback callback = (condition, stackTrace, type) => {
            if (type == LogType.Error || type == LogType.Assert || type == LogType.Exception) {
                errorCount++;
                if (string.IsNullOrEmpty(firstError)) {
                    firstError = condition;
                }
            }
        };

        Application.logMessageReceived += callback;
        try {
            action();
        } catch (Exception ex) {
            threw = true;
            exceptionMessage = ex.Message;
            Debug.LogError($"[PrologueFirstDivePermissionSmokeSuite] {stepName} threw: {ex}");
        } finally {
            Application.logMessageReceived -= callback;
        }

        bool passed = !threw && errorCount == 0;
        diagnostics.Add(
            passed
                ? $"{stepName}=PASSED"
                : $"{stepName}=FAILED errors={errorCount} firstError={firstError} exception={exceptionMessage}");
        return passed;
    }
}
