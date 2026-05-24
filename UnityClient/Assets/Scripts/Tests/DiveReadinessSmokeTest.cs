using UnityEngine;

public static class DiveReadinessSmokeTest {
    private static string _lastRejectedReason;

    public static void Run() {
        Debug.Log("=== Running Dive Readiness Smoke Test ===");

        TestReadyDollCanStartRun();
        TestExtremeWearBlocksStartRun();
        TestExtremeCorruptionBlocksStartRun();
        TestMissingChassisBlocksStartRun();
        TestRuntimeGridMismatchBlocksStartRun();
        TestInvalidProstheticReferenceAutoUnequipsOnStart();
        TestWarningsDoNotBlockDive();

        Debug.Log("=== Dive Readiness Smoke Test Finished ===");
    }

    private static void TestReadyDollCanStartRun() {
        CoreBackend core = CreateCore();
        DiveReadinessResult result = DiveReadinessService.Evaluate(core.CurrentPlayer, 1);
        bool started = core.Dungeon.StartRunAtLayer(1);

        if (result.CanDive && started && core.Dungeon.CurrentLayer != null && core.Dungeon.CurrentLayer.LayerID == 1) {
            Debug.Log("Dive Readiness Ready Doll PASSED.");
        } else {
            Debug.LogError($"Dive Readiness Ready Doll FAILED. CanDive={result.CanDive}, Started={started}, Layer={core.Dungeon.CurrentLayer?.LayerID ?? -1}, Reason={result.BuildSummary()}");
        }
    }

    private static void TestExtremeWearBlocksStartRun() {
        CoreBackend core = CreateCore();
        core.CurrentPlayer.ActiveDoll.Status.WearAndTear = DiveReadinessService.ExtremeWearThreshold;

        bool rejectedEventFired = CaptureStartRejection(core, 1, out bool started);
        DiveReadinessResult result = DiveReadinessService.Evaluate(core.CurrentPlayer, 1);

        if (!result.CanDive && !started && rejectedEventFired && HasIssue(result, DiveReadinessIssueCode.ExtremeWear)) {
            Debug.Log("Dive Readiness Extreme Wear Block PASSED.");
        } else {
            Debug.LogError($"Dive Readiness Extreme Wear Block FAILED. CanDive={result.CanDive}, Started={started}, Rejected={rejectedEventFired}, Reason={_lastRejectedReason}");
        }
    }

    private static void TestExtremeCorruptionBlocksStartRun() {
        CoreBackend core = CreateCore();
        core.CurrentPlayer.ActiveDoll.Status.Corruption = DiveReadinessService.ExtremeCorruptionThreshold;

        bool rejectedEventFired = CaptureStartRejection(core, 1, out bool started);
        DiveReadinessResult result = DiveReadinessService.Evaluate(core.CurrentPlayer, 1);

        if (!result.CanDive && !started && rejectedEventFired && HasIssue(result, DiveReadinessIssueCode.ExtremeCorruption)) {
            Debug.Log("Dive Readiness Extreme Corruption Block PASSED.");
        } else {
            Debug.LogError($"Dive Readiness Extreme Corruption Block FAILED. CanDive={result.CanDive}, Started={started}, Rejected={rejectedEventFired}, Reason={_lastRejectedReason}");
        }
    }

    private static void TestMissingChassisBlocksStartRun() {
        CoreBackend core = CreateCore();
        core.CurrentPlayer.ActiveDoll.Chassis = null;

        bool rejectedEventFired = CaptureStartRejection(core, 1, out bool started);
        DiveReadinessResult result = DiveReadinessService.Evaluate(core.CurrentPlayer, 1);

        if (!result.CanDive && !started && rejectedEventFired && HasIssue(result, DiveReadinessIssueCode.MissingChassis)) {
            Debug.Log("Dive Readiness Missing Chassis Block PASSED.");
        } else {
            Debug.LogError($"Dive Readiness Missing Chassis Block FAILED. CanDive={result.CanDive}, Started={started}, Rejected={rejectedEventFired}, Reason={_lastRejectedReason}");
        }
    }

    private static void TestRuntimeGridMismatchBlocksStartRun() {
        CoreBackend core = CreateCore();
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        doll.Chassis.GridWidth += 1;

        bool rejectedEventFired = CaptureStartRejection(core, 1, out bool started);
        DiveReadinessResult result = DiveReadinessService.Evaluate(core.CurrentPlayer, 1);

        if (!result.CanDive && !started && rejectedEventFired && HasIssue(result, DiveReadinessIssueCode.RuntimeGridMismatch)) {
            Debug.Log("Dive Readiness Runtime Grid Mismatch Block PASSED.");
        } else {
            Debug.LogError($"Dive Readiness Runtime Grid Mismatch Block FAILED. CanDive={result.CanDive}, Started={started}, Rejected={rejectedEventFired}, Reason={_lastRejectedReason}");
        }
    }

    private static void TestInvalidProstheticReferenceAutoUnequipsOnStart() {
        CoreBackend core = CreateCore();
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        doll.EquippedProsthetics.Add("missing_prosthetic_for_smoke");

        DiveReadinessResult preview = DiveReadinessService.Evaluate(core.CurrentPlayer, 1, false);
        bool started = core.Dungeon.StartRunAtLayer(1);
        bool removed = !doll.EquippedProsthetics.Contains("missing_prosthetic_for_smoke");
        DiveReadinessResult after = DiveReadinessService.Evaluate(core.CurrentPlayer, 1, false);

        if (preview.CanDive && HasIssue(preview, DiveReadinessIssueCode.InvalidProstheticReference) && started && removed && after.CanDive && !HasIssue(after, DiveReadinessIssueCode.InvalidProstheticReference)) {
            Debug.Log("Dive Readiness Invalid Prosthetic Auto Unequip PASSED.");
        } else {
            Debug.LogError($"Dive Readiness Invalid Prosthetic Auto Unequip FAILED. PreviewCanDive={preview.CanDive}, Started={started}, Removed={removed}, AfterCanDive={after.CanDive}, Preview={preview.BuildSummary()}, After={after.BuildSummary()}");
        }
    }

    private static void TestWarningsDoNotBlockDive() {
        CoreBackend core = CreateCore();
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        doll.Status.WearAndTear = DiveReadinessService.HeavyWearWarningThreshold;
        doll.Status.Corruption = DiveReadinessService.HighCorruptionWarningThreshold;

        DiveReadinessResult result = DiveReadinessService.Evaluate(core.CurrentPlayer, 1);
        bool started = core.Dungeon.StartRunAtLayer(1);

        if (result.CanDive && started && HasIssue(result, DiveReadinessIssueCode.HeavyWearWarning) && HasIssue(result, DiveReadinessIssueCode.HighCorruptionWarning)) {
            Debug.Log("Dive Readiness Warning Allows Dive PASSED.");
        } else {
            Debug.LogError($"Dive Readiness Warning Allows Dive FAILED. CanDive={result.CanDive}, Started={started}, Reason={result.BuildSummary()}");
        }
    }

    private static CoreBackend CreateCore() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        return core;
    }

    private static bool CaptureStartRejection(CoreBackend core, int layerID, out bool started) {
        _lastRejectedReason = string.Empty;
        DungeonEventBus.OnDungeonStartLayerRejected += HandleStartLayerRejected;
        started = core.Dungeon.StartRunAtLayer(layerID);
        DungeonEventBus.OnDungeonStartLayerRejected -= HandleStartLayerRejected;
        return !string.IsNullOrEmpty(_lastRejectedReason);
    }

    private static void HandleStartLayerRejected(int layerID, string reason) {
        _lastRejectedReason = reason;
    }

    private static bool HasIssue(DiveReadinessResult result, DiveReadinessIssueCode code) {
        if (result?.Issues == null) {
            return false;
        }

        foreach (DiveReadinessIssue issue in result.Issues) {
            if (issue != null && issue.Code == code) {
                return true;
            }
        }

        return false;
    }
}
