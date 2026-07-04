using System.Collections.Generic;
using UnityEngine;

public static class PrologueOpeningNarrativeFlowSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Prologue Opening Narrative Flow Smoke Test ===");

        ConfigManager.LoadAllConfigs();
        PrologueOpeningNarrativeFlow flow = new PrologueOpeningNarrativeFlow(ConfigManager.Narrative);
        PrologueOpeningFlowResult result = flow.PlayOpeningToNo0Found();

        bool nodesPlayed = result.Nodes.Count == 2
            && result.Nodes[0].NodeID == PrologueOpeningNarrativeFlow.OpeningNodeID
            && result.Nodes[1].NodeID == PrologueOpeningNarrativeFlow.FindNo0NodeID;

        bool flagsWritten = result.State.GetFlag("PrologueStarted")
            && result.State.GetFlag("DebtNoticeSeen")
            && result.State.GetFlag("RepairNoteSeen")
            && result.State.GetFlag("LastCoreShardSeen")
            && result.State.GetFlag("No0Found");

        bool firstActionOnly = result.HasOnlyStartDollActionAfterNo0
            && !result.State.GetFlag("No0Started")
            && !result.State.GetFlag("FirstStatusShown")
            && !result.State.GetFlag("FirstDiveUnlocked")
            && !result.State.GetFlag("Layer1ConfirmOpened");

        bool visualSequence = ContainsInOrder(
            result.VisualSequence,
            "cg_t0_01a_black_wake",
            "cg_t0_01a_debt_notice",
            "cg_t0_01a_repair_note",
            "cg_t0_01a_core_shard",
            "cg_t0_01a_find_no0");

        bool dialogueCoverage = result.LinesShown == 9
            && result.CommandsExecuted >= 8
            && ContainsLine(result.Nodes[0].PlaybackResult, "prologue.t0_01a.black_wake.line_01")
            && ContainsLine(result.Nodes[1].PlaybackResult, "prologue.t0_01a.find_no0.line_02")
            && ContainsCommand(result.Nodes[1].PlaybackResult, "show_prologue_action", "start_doll");

        bool stepOrder = result.Nodes[0].PlaybackResult.Steps.Count > 0
            && result.Nodes[0].PlaybackResult.Steps[0].StepType == NarrativePlaybackStepType.Command
            && result.Nodes[0].PlaybackResult.Steps[1].StepType == NarrativePlaybackStepType.Line;

        if (result.Success && nodesPlayed && flagsWritten && firstActionOnly && visualSequence && dialogueCoverage && stepOrder) {
            Debug.Log("Prologue Opening Narrative Flow Smoke PASSED.");
        } else {
            Debug.LogError(
                "Prologue Opening Narrative Flow Smoke FAILED. "
                + $"Success={result.Success}, Nodes={nodesPlayed}, Flags={flagsWritten}, FirstActionOnly={firstActionOnly}, "
                + $"Visuals={visualSequence}, Dialogue={dialogueCoverage}, StepOrder={stepOrder}, "
                + $"Lines={result.LinesShown}, Commands={result.CommandsExecuted}, "
                + $"VisualSequence={string.Join("|", result.VisualSequence)}, Errors={string.Join("|", result.Errors)}");
        }

        Debug.Log("=== Prologue Opening Narrative Flow Smoke Test Finished ===");
    }

    private static bool ContainsInOrder(List<string> values, params string[] expected) {
        if (values == null || expected == null) {
            return false;
        }

        int cursor = 0;
        foreach (string value in values) {
            if (cursor < expected.Length && value == expected[cursor]) {
                cursor++;
            }
        }

        return cursor == expected.Length;
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

    private static bool ContainsCommand(NarrativePlaybackResult result, string commandName, string firstArgument) {
        if (result?.Commands == null) {
            return false;
        }

        foreach (NarrativeCommandOutput command in result.Commands) {
            if (command.CommandName == commandName
                && command.Arguments != null
                && command.Arguments.Count > 0
                && command.Arguments[0] == firstArgument) {
                return true;
            }
        }

        return false;
    }
}
