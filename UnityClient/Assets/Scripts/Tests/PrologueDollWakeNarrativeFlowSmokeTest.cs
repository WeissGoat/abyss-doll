using System.Collections.Generic;
using UnityEngine;

public static class PrologueDollWakeNarrativeFlowSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Prologue Doll Wake Narrative Flow Smoke Test ===");

        ConfigManager.LoadAllConfigs();
        PrologueDollWakeNarrativeFlow flow = new PrologueDollWakeNarrativeFlow(ConfigManager.Narrative);
        PrologueDollWakeFlowResult result = flow.PlayStartDollToCoreWipe();

        bool nodesPlayed = result.Nodes.Count == 4
            && result.Nodes[0].NodeID == PrologueDollWakeNarrativeFlow.StartDollNodeID
            && result.Nodes[1].NodeID == PrologueDollWakeNarrativeFlow.No0WakeNodeID
            && result.Nodes[2].NodeID == PrologueDollWakeNarrativeFlow.FirstStatusCardNodeID
            && result.Nodes[3].NodeID == PrologueDollWakeNarrativeFlow.FirstCoreWipeNodeID;

        bool flagsWritten = result.State.GetFlag("No0Started")
            && result.State.GetFlag("No0WakeDialogueSeen")
            && result.State.GetFlag("FirstStatusShown")
            && result.State.GetFlag("FirstCoreWiped");

        bool noEarlyDive = !result.State.GetFlag("FirstDiveUnlocked")
            && !result.State.GetFlag("Layer1ConfirmOpened")
            && result.UnlockUiRequests == 0
            && result.LayerConfirmRequests == 0;

        bool actionSemantics = Count(result.PrologueActionIDs, "start_doll") == 0
            && Count(result.PrologueActionIDs, "wipe_core") == 1;

        bool statusCards = result.StatusCardIDs.Count == 2
            && result.StatusCardIDs[0] == "first_status_unstable"
            && result.StatusCardIDs[1] == "first_status_stable";

        bool dialogueCoverage = result.LinesShown == 15
            && ContainsLine(result.Nodes[1].PlaybackResult, "prologue.t0_01a.no0_wake.line_08")
            && ContainsLine(result.Nodes[3].PlaybackResult, "prologue.t0_01a.core_wipe.line_03");

        bool visualAndCharacter = Contains(result.VisualSequence, "vfx_no0_core_start")
            && Contains(result.CharacterSequence, "no0");

        if (result.Success && nodesPlayed && flagsWritten && noEarlyDive && actionSemantics && statusCards && dialogueCoverage && visualAndCharacter) {
            Debug.Log("Prologue Doll Wake Narrative Flow Smoke PASSED.");
        } else {
            Debug.LogError(
                "Prologue Doll Wake Narrative Flow Smoke FAILED. "
                + $"Success={result.Success}, Nodes={nodesPlayed}, Flags={flagsWritten}, NoEarlyDive={noEarlyDive}, "
                + $"Actions={actionSemantics}, StatusCards={statusCards}, Dialogue={dialogueCoverage}, Visuals={visualAndCharacter}, "
                + $"Lines={result.LinesShown}, Commands={result.CommandsExecuted}, "
                + $"ActionsSeen={string.Join("|", result.PrologueActionIDs)}, StatusCardsSeen={string.Join("|", result.StatusCardIDs)}, "
                + $"Errors={string.Join("|", result.Errors)}");
        }

        Debug.Log("=== Prologue Doll Wake Narrative Flow Smoke Test Finished ===");
    }

    private static bool ContainsLine(NarrativePlaybackResult result, string lineKey) {
        if (result?.Lines == null) {
            return false;
        }

        foreach (NarrativeLineOutput line in result.Lines) {
            if (line.LineKey == lineKey) {
                return true;
            }
        }

        return false;
    }

    private static bool Contains(List<string> values, string expected) {
        if (values == null) {
            return false;
        }

        foreach (string value in values) {
            if (value == expected) {
                return true;
            }
        }

        return false;
    }

    private static int Count(List<string> values, string expected) {
        int count = 0;
        if (values == null) {
            return count;
        }

        foreach (string value in values) {
            if (value == expected) {
                count++;
            }
        }

        return count;
    }
}
