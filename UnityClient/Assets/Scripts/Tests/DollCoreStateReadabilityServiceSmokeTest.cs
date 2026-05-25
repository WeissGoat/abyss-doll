using System.Linq;
using UnityEngine;

public static class DollCoreStateReadabilityServiceSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Doll Core State Readability Service Smoke Test ===");

        TestDefaultSnapshot();
        TestSanThresholdAndMaintenanceRiskSnapshot();
        TestBondProstheticAndTraitSnapshot();
        TestMissingDollSnapshot();

        Debug.Log("=== Doll Core State Readability Service Smoke Test Finished ===");
    }

    private static void TestDefaultSnapshot() {
        CoreBackend core = CreateCore();
        DollCoreStateReadabilitySnapshot snapshot = DollCoreStateReadabilityService.BuildSnapshot(core.CurrentPlayer);

        bool passed = snapshot.Success
            && snapshot.DollID == "doll_proto_0"
            && Mathf.Approximately(snapshot.HPPercent, 1f)
            && Mathf.Approximately(snapshot.SANPercent, 1f)
            && snapshot.SanStateText.Contains("稳定")
            && snapshot.EmotionState == DollCoreEmotionState.Calm
            && snapshot.ChassisText.Contains("chassis_lv1_basic")
            && snapshot.CombinedText.Contains("人偶状态");

        if (passed) {
            Debug.Log("Doll Core Default Snapshot PASSED.");
        } else {
            Debug.LogError($"Doll Core Default Snapshot FAILED. Success={snapshot.Success}, Doll={snapshot.DollID}, HP={snapshot.HPText}, SAN={snapshot.SANText}, Emotion={snapshot.EmotionText}, Text={snapshot.CombinedText}");
        }
    }

    private static void TestSanThresholdAndMaintenanceRiskSnapshot() {
        CoreBackend core = CreateCore();
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        doll.Status.HP_Current = 20;
        doll.Status.HP_Max = 100;
        doll.Status.SAN_Current = 4;
        doll.Status.SAN_Max = 50;
        doll.Status.WearAndTear = DiveReadinessService.HeavyWearWarningThreshold;
        doll.Status.Corruption = DiveReadinessService.HighCorruptionWarningThreshold;

        DollCoreStateReadabilitySnapshot snapshot = DollCoreStateReadabilityService.BuildSnapshot(doll);

        bool passed = snapshot.Success
            && snapshot.EmotionState == DollCoreEmotionState.Panic
            && snapshot.SanStateText.Contains("崩溃边缘")
            && snapshot.MaintenanceText.Contains("高磨损")
            && snapshot.MaintenanceText.Contains("高侵蚀")
            && snapshot.WarningLines.Any(line => line.Contains("HP偏低"))
            && snapshot.WarningLines.Any(line => line.Contains("SAN偏低"))
            && snapshot.CombinedText.Contains("警告");

        if (passed) {
            Debug.Log("Doll Core SAN Threshold And Maintenance Risk Snapshot PASSED.");
        } else {
            Debug.LogError($"Doll Core SAN Threshold And Maintenance Risk Snapshot FAILED. Success={snapshot.Success}, SAN={snapshot.SanStateText}, Emotion={snapshot.EmotionText}, Maintenance={snapshot.MaintenanceText}, Warnings={snapshot.WarningLines.Count}, Text={snapshot.CombinedText}");
        }
    }

    private static void TestBondProstheticAndTraitSnapshot() {
        CoreBackend core = CreateCore();
        DollEntity doll = core.CurrentPlayer.ActiveDoll;
        doll.Status.SAN_Current = doll.Status.SAN_Max;
        doll.Bond.AffectionLevel = 5;
        doll.Bond.HiddenTrust = 42;
        doll.Traits.Add("trait_test_focus");
        doll.EquippedProsthetics.Add("pros_power_arm");
        doll.EquippedProsthetics.Add("pros_cooling_system");

        DollCoreStateReadabilitySnapshot snapshot = DollCoreStateReadabilityService.BuildSnapshot(core.CurrentPlayer);

        bool passed = snapshot.Success
            && snapshot.EmotionState == DollCoreEmotionState.Energetic
            && snapshot.BondStageText.Contains("Lv.5")
            && snapshot.BondStageText.Contains("羁绊")
            && snapshot.BondStageText.Contains("42")
            && snapshot.TraitLines.Contains("trait_test_focus")
            && snapshot.ProstheticLines.Count >= 2
            && snapshot.ProstheticLines.Any(line => line.ProstheticID == "pros_power_arm" && line.ConfigFound)
            && snapshot.ProstheticLines.Any(line => line.ProstheticID == "pros_cooling_system" && line.ConfigFound)
            && snapshot.CombinedText.Contains("义体")
            && snapshot.CombinedText.Contains("特质");

        if (passed) {
            Debug.Log("Doll Core Bond Prosthetic And Trait Snapshot PASSED.");
        } else {
            Debug.LogError($"Doll Core Bond Prosthetic And Trait Snapshot FAILED. Success={snapshot.Success}, Emotion={snapshot.EmotionText}, Bond={snapshot.BondStageText}, Prosthetics={snapshot.ProstheticLines.Count}, Traits={snapshot.TraitLines.Count}, Text={snapshot.CombinedText}");
        }
    }

    private static void TestMissingDollSnapshot() {
        DollCoreStateReadabilitySnapshot playerSnapshot = DollCoreStateReadabilityService.BuildSnapshot(new PlayerProfile());
        DollCoreStateReadabilitySnapshot dollSnapshot = DollCoreStateReadabilityService.BuildSnapshot((DollEntity)null);

        bool passed = !playerSnapshot.Success
            && playerSnapshot.Reason.Contains("Active doll")
            && !dollSnapshot.Success
            && dollSnapshot.Reason.Contains("Doll entity")
            && playerSnapshot.CombinedText.Contains("人偶数据不可用")
            && dollSnapshot.CombinedText.Contains("人偶数据不可用");

        if (passed) {
            Debug.Log("Doll Core Missing Doll Snapshot PASSED.");
        } else {
            Debug.LogError($"Doll Core Missing Doll Snapshot FAILED. PlayerSuccess={playerSnapshot.Success}, PlayerReason={playerSnapshot.Reason}, DollSuccess={dollSnapshot.Success}, DollReason={dollSnapshot.Reason}");
        }
    }

    private static CoreBackend CreateCore() {
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();
        return core;
    }
}
