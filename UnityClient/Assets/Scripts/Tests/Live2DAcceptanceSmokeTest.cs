using System;
using UnityEngine;

public static class Live2DAcceptanceSmokeTest {
    public static void Run() {
        try {
            Debug.Log("=== Running Live2D Acceptance Smoke Test ===");
            TestReportSerialization();
            TestFallbackOnlyStatusContract();
            TestCaptureStepContract();
            Debug.Log("=== Live2D Acceptance Smoke Test Finished ===");
        } catch (Exception ex) {
            Debug.LogError($"[Live2DAcceptanceSmokeTest Crash] {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static void TestReportSerialization() {
        Live2DAcceptanceReport report = CreateFallbackOnlyReport();
        string json = JsonUtility.ToJson(report, true);
        if (!string.IsNullOrEmpty(json)
            && json.Contains("doll_proto_0_live2d")
            && json.Contains("FormalV2ArtAcceptanceTouched")
            && json.Contains("fallback_captured")) {
            Debug.Log("Live2D Acceptance Report Serialization PASSED.");
        } else {
            Debug.LogError("Live2D Acceptance Report Serialization FAILED.");
        }
    }

    private static void TestFallbackOnlyStatusContract() {
        Live2DAcceptanceReport report = CreateFallbackOnlyReport();
        DetermineStatus(report);
        if (report.Status == "FALLBACK_ONLY") {
            Debug.Log("Live2D Acceptance Fallback Status PASSED.");
        } else {
            Debug.LogError($"Live2D Acceptance Fallback Status FAILED. Got={report.Status}");
        }
    }

    private static void TestCaptureStepContract() {
        string[] requiredSteps = {
            "idle_0s",
            "idle_1s",
            "expression_2s",
            "low_san_idle_3s",
            "repair_react",
            "hit_react",
            "fallback"
        };

        Live2DAcceptanceReport report = CreateFallbackOnlyReport();
        bool allPresent = true;
        foreach (string step in requiredSteps) {
            bool found = false;
            foreach (Live2DAcceptanceCaptureRecord capture in report.Captures) {
                if (capture.StepID == step && capture.File == $"screenshots/{step}.png") {
                    found = true;
                    break;
                }
            }

            if (!found) {
                allPresent = false;
                Debug.LogError($"Live2D Acceptance required step missing: {step}");
            }
        }

        if (allPresent && report.Captures.Count == requiredSteps.Length) {
            Debug.Log("Live2D Acceptance Capture Step Contract PASSED.");
        } else {
            Debug.LogError("Live2D Acceptance Capture Step Contract FAILED.");
        }
    }

    private static Live2DAcceptanceReport CreateFallbackOnlyReport() {
        Live2DAcceptanceReport report = new Live2DAcceptanceReport {
            SchemaVersion = "1.0",
            RunID = "test_run",
            Project = "P3",
            Mode = "live2d_independent",
            Status = "RUNNING",
            IsRunning = false,
            OutputRoot = Live2DAcceptanceRunner.OutputRootRelative,
            Visual = new Live2DAcceptanceVisualSummary {
                DollID = DollLive2DVisualResolver.DefaultDollID,
                DynamicVisualID = DollLive2DVisualResolver.DefaultDynamicVisualID,
                FallbackSpriteID = DollLive2DVisualResolver.DefaultFallbackSpriteID,
                VisualMode = DollLive2DVisualMode.StaticFallback.ToString(),
                FallbackReason = "dynamic_prefab_missing",
                RuntimePackageAvailable = false,
                DynamicPrefabFound = false,
                FallbackSpriteFound = true,
                PresenterMode = "fallback_presenter"
            },
            Boundaries = new Live2DAcceptanceBoundarySummary {
                FormalV2ArtAcceptanceTouched = false,
                ScreenLayoutsTouched = false,
                ApprovedAssetsTouched = false,
                PackageManifestTouched = false,
                Notes = "Smoke test contract."
            }
        };

        AddCapture(report, 1, "idle_0s", DollLive2DMotionIDs.Idle, DollLive2DExpressionIDs.Normal);
        AddCapture(report, 2, "idle_1s", DollLive2DMotionIDs.Idle, DollLive2DExpressionIDs.Blink);
        AddCapture(report, 3, "expression_2s", DollLive2DMotionIDs.Idle, DollLive2DExpressionIDs.Relaxed);
        AddCapture(report, 4, "low_san_idle_3s", DollLive2DMotionIDs.LowSanIdle, DollLive2DExpressionIDs.LowSan);
        AddCapture(report, 5, "repair_react", DollLive2DMotionIDs.RepairReact, DollLive2DExpressionIDs.Relaxed);
        AddCapture(report, 6, "hit_react", DollLive2DMotionIDs.HitReact, DollLive2DExpressionIDs.Hurt);
        AddCapture(report, 7, "fallback", DollLive2DMotionIDs.Idle, DollLive2DExpressionIDs.Normal);
        return report;
    }

    private static void AddCapture(Live2DAcceptanceReport report, int index, string stepID, string motionID, string expressionID) {
        report.Captures.Add(new Live2DAcceptanceCaptureRecord {
            Index = index,
            StepID = stepID,
            MotionID = motionID,
            ExpressionID = expressionID,
            File = $"screenshots/{stepID}.png",
            Status = "fallback_captured",
            Resolution = "1920x1080",
            DynamicFrameProven = false,
            FallbackVisible = true
        });
    }

    private static void DetermineStatus(Live2DAcceptanceReport report) {
        bool hasScreenshot = false;
        foreach (Live2DAcceptanceCaptureRecord capture in report.Captures) {
            if (capture.Status == "captured" || capture.Status == "fallback_captured") {
                hasScreenshot = true;
            }

            foreach (string warning in capture.Warnings) {
                if (!report.Warnings.Contains(warning)) {
                    report.Warnings.Add(warning);
                }
            }

            foreach (string error in capture.Errors) {
                if (!report.Errors.Contains(error)) {
                    report.Errors.Add(error);
                }
            }
        }

        if (!hasScreenshot) {
            report.Errors.Add("No Live2D acceptance screenshots were captured.");
        }

        if (report.Errors.Count > 0) {
            report.Status = "FAILED";
        } else if (report.Visual.DynamicPrefabFound && report.Visual.PresenterMode == "dynamic_presenter") {
            report.Status = "PASSED";
        } else if (report.Visual.FallbackSpriteFound) {
            report.Status = "FALLBACK_ONLY";
        } else {
            report.Status = "WARNING";
        }
    }
}
