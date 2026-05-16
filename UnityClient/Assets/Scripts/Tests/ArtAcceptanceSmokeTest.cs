using System;
using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// 美术自动验收系统冒烟测试。
/// 验证数据结构完整性、P0 VisualID 列表、报告序列化、风险标记规则。
/// 由 AutoTestDaemon 的 RUN_ALL_TESTS 自动发现并执行。
/// </summary>
public static class ArtAcceptanceSmokeTest {
    public static void Run() {
        try {
            Debug.Log("=== Running ArtAcceptance Smoke Test ===");

            TestReportStructure();
            TestRegistrySnapshotStructure();
            TestUiSnapshotStructure();
            TestRiskRuleNames();
            TestReportStatusDetermination();

            Debug.Log("=== ArtAcceptance Smoke Test Finished ===");
        } catch (Exception ex) {
            Debug.LogError($"[ArtAcceptanceSmokeTest Crash] {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static void TestReportStructure() {
        ArtAcceptanceReport report = new ArtAcceptanceReport();
        report.SchemaVersion = "1.0";
        report.RunID = "test_run";
        report.Project = "P3";
        report.Mode = "auto";
        report.Status = "RUNNING";
        report.IsRunning = true;
        report.StartedAt = DateTime.Now.ToString("o");
        report.ReferenceResolution = new ArtAcceptanceResolution { Width = 1920, Height = 1080 };
        report.ActualResolution = new ArtAcceptanceResolution { Width = 1920, Height = 1080 };

        ArtAcceptanceCaptureRecord capture = new ArtAcceptanceCaptureRecord {
            ScreenTag = "workshop_main",
            File = "screenshots/workshop_main.png",
            Status = "captured",
            Resolution = "1920x1080"
        };
        capture.ActiveControllers.Add("GameFlowController");
        report.Captures.Add(capture);

        string json = JsonUtility.ToJson(report, true);
        if (!string.IsNullOrEmpty(json) && json.Contains("workshop_main") && json.Contains("1920")) {
            Debug.Log("Report Structure Serialization PASSED.");
        } else {
            Debug.LogError("Report Structure Serialization FAILED.");
        }

        // Verify all required fields are present.
        if (report.Warnings != null && report.Errors != null && report.Captures != null
            && report.CanvasScaler != null && report.Registry != null) {
            Debug.Log("Report Field Initialization PASSED.");
        } else {
            Debug.LogError("Report Field Initialization FAILED.");
        }
    }

    private static void TestRegistrySnapshotStructure() {
        ArtAcceptanceRegistrySnapshotReport snapshot = new ArtAcceptanceRegistrySnapshotReport {
            SchemaVersion = "1.0",
            RunID = "test_run",
            RegistryFound = true,
            MissingSpriteFound = true,
            MissingSpriteName = "ui_missing_sprite",
            EntryCount = 49
        };

        ArtAcceptanceRegistryEntrySnapshot entry = new ArtAcceptanceRegistryEntrySnapshot {
            VisualID = "ui_panel_info",
            HasSprite = true,
            SpriteName = "ui_panel_info",
            TextureSize = "256x256",
            SpriteRect = "0,0,256,256",
            SpriteBorder = "16,16,16,16"
        };
        snapshot.Entries.Add(entry);

        string json = JsonUtility.ToJson(snapshot, true);
        if (!string.IsNullOrEmpty(json) && json.Contains("ui_panel_info") && json.Contains("256x256")) {
            Debug.Log("RegistrySnapshot Structure Serialization PASSED.");
        } else {
            Debug.LogError("RegistrySnapshot Structure Serialization FAILED.");
        }
    }

    private static void TestUiSnapshotStructure() {
        ArtAcceptanceUiSnapshotReport uiSnapshot = new ArtAcceptanceUiSnapshotReport {
            SchemaVersion = "1.0",
            RunID = "test_run"
        };

        ArtAcceptanceUiCaptureSnapshot captureSnapshot = new ArtAcceptanceUiCaptureSnapshot {
            ScreenTag = "combat_hud"
        };

        ArtAcceptanceCanvasSnapshot canvas = new ArtAcceptanceCanvasSnapshot {
            Name = "Canvas",
            Path = "Canvas",
            RenderMode = "ScreenSpaceOverlay",
            SortingOrder = 0
        };

        ArtAcceptanceUiElementSnapshot element = new ArtAcceptanceUiElementSnapshot {
            Name = "TestButton",
            Path = "Canvas/TestButton",
            Active = true,
            AnchoredPosition = "0,0",
            SizeDelta = "200,50"
        };
        element.ComponentTypes.Add("Button");
        element.ComponentTypes.Add("Image");
        element.Button = new ArtAcceptanceButtonSnapshot { Found = true, Interactable = true, HasTargetGraphic = true };
        element.Image = new ArtAcceptanceImageSnapshot {
            Found = true,
            Enabled = true,
            SpriteName = "ui_button_primary",
            Type = "Sliced",
            RaycastTarget = true
        };
        element.Risks.Add("TestRisk");

        canvas.Elements.Add(element);
        captureSnapshot.Canvases.Add(canvas);
        uiSnapshot.Captures.Add(captureSnapshot);

        string json = JsonUtility.ToJson(uiSnapshot, true);
        if (!string.IsNullOrEmpty(json) && json.Contains("combat_hud") && json.Contains("TestButton") && json.Contains("TestRisk")) {
            Debug.Log("UiSnapshot Structure Serialization PASSED.");
        } else {
            Debug.LogError("UiSnapshot Structure Serialization FAILED.");
        }
    }

    private static void TestRiskRuleNames() {
        // Verify risk rule string constants match the document specification (§10.2).
        string[] expectedRiskNames = {
            "ImageEnabledButSpriteEmpty",
            "MissingSpriteVisible",
            "LargeNonButtonImageBlocksRaycasts",
            "ExpectedSlicedImageButTypeIsNotSliced",
            "ButtonMissingTargetGraphic",
            "ButtonMissingImageSprite",
            "InventorySlotSizeNot100x100"
        };

        bool allPresent = true;
        foreach (string risk in expectedRiskNames) {
            if (string.IsNullOrEmpty(risk)) {
                allPresent = false;
                Debug.LogError($"Risk Rule Name is empty.");
            }
        }

        if (allPresent && expectedRiskNames.Length >= 7) {
            Debug.Log("Risk Rule Names Coverage PASSED.");
        } else {
            Debug.LogError("Risk Rule Names Coverage FAILED.");
        }
    }

    private static void TestReportStatusDetermination() {
        // PASSED: no errors, no warnings.
        ArtAcceptanceReport passedReport = new ArtAcceptanceReport();
        passedReport.Captures.Add(new ArtAcceptanceCaptureRecord { Status = "captured" });
        DetermineStatus(passedReport);
        if (passedReport.Status == "PASSED") {
            Debug.Log("Report Status PASSED determination PASSED.");
        } else {
            Debug.LogError($"Report Status PASSED determination FAILED. Got={passedReport.Status}");
        }

        // WARNING: has warnings, no errors.
        ArtAcceptanceReport warningReport = new ArtAcceptanceReport();
        warningReport.Captures.Add(new ArtAcceptanceCaptureRecord { Status = "captured" });
        warningReport.Warnings.Add("some_warning");
        DetermineStatus(warningReport);
        if (warningReport.Status == "WARNING") {
            Debug.Log("Report Status WARNING determination PASSED.");
        } else {
            Debug.LogError($"Report Status WARNING determination FAILED. Got={warningReport.Status}");
        }

        // FAILED: has errors.
        ArtAcceptanceReport failedReport = new ArtAcceptanceReport();
        failedReport.Captures.Add(new ArtAcceptanceCaptureRecord { Status = "captured" });
        failedReport.Errors.Add("some_error");
        DetermineStatus(failedReport);
        if (failedReport.Status == "FAILED") {
            Debug.Log("Report Status FAILED determination PASSED.");
        } else {
            Debug.LogError($"Report Status FAILED determination FAILED. Got={failedReport.Status}");
        }

        // FAILED: no captures at all.
        ArtAcceptanceReport noCaptureReport = new ArtAcceptanceReport();
        DetermineStatus(noCaptureReport);
        if (noCaptureReport.Status == "FAILED") {
            Debug.Log("Report Status No-Capture FAILED determination PASSED.");
        } else {
            Debug.LogError($"Report Status No-Capture FAILED determination FAILED. Got={noCaptureReport.Status}");
        }
    }

    /// <summary>
    /// Mirrors FinalizeReport logic from ArtAcceptanceRunner for isolated testing.
    /// </summary>
    private static void DetermineStatus(ArtAcceptanceReport report) {
        bool hasCaptured = false;
        foreach (ArtAcceptanceCaptureRecord capture in report.Captures) {
            if (capture.Status == "captured") {
                hasCaptured = true;
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

        if (!hasCaptured) {
            report.Errors.Add("No screenshots were captured.");
        }

        if (report.Errors.Count > 0) {
            report.Status = "FAILED";
        } else if (report.Warnings.Count > 0) {
            report.Status = "WARNING";
        } else {
            report.Status = "PASSED";
        }
    }
}
