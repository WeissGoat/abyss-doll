using System.Collections.Generic;
using UnityEngine;

public static class NarrativeRuntimeSpikeSmokeTest {
    public static void Run() {
        try {
            Debug.Log("=== Running Narrative Runtime Spike Smoke Test ===");

            P3YarnLikeNarrativeRuntime runtime = new P3YarnLikeNarrativeRuntime();
            List<NarrativeCommandOutput> callbacks = new List<NarrativeCommandOutput>();

            NarrativePlaybackResult result = runtime.PlayNode(BuildSampleScript(), "T0_01A_Opening_CG", callbacks.Add);

            bool candidateRecorded = NarrativeRuntimeSelection.CandidateName == "Yarn Spinner for Unity"
                && NarrativeRuntimeSelection.CandidateVersion == "v3.2.4"
                && NarrativeRuntimeSelection.CandidateLicense == "MIT"
                && NarrativeRuntimeSelection.CandidatePackageUrl.Contains("YarnSpinner-Unity.git#v3.2.4")
                && !NarrativeRuntimeSelection.IsExternalRuntimeLocked;

            bool nodeFound = result.Success && runtime.HasNode(BuildSampleScript(), "T0_01A_Opening_CG");
            bool lineOutput = result.Lines.Count == 2
                && result.Lines[0].SpeakerID == "protagonist"
                && result.Lines[0].LineKey == "prologue.t0_01a.black_wake.line_01"
                && result.Lines[0].Text == "Morning again.";
            bool commandOutput = result.Commands.Count == 2
                && callbacks.Count == 2
                && result.Commands[0].CommandName == "play_visual"
                && result.Commands[0].Arguments[0] == "cg_t0_01a_black_wake"
                && result.Commands[1].CommandName == "set_flag"
                && result.Commands[1].Arguments[0] == "PrologueStarted";

            if (candidateRecorded && nodeFound && lineOutput && commandOutput) {
                Debug.Log("Narrative Runtime Spike PASSED.");
            } else {
                Debug.LogError($"Narrative Runtime Spike FAILED. Candidate={candidateRecorded}, Node={nodeFound}, Lines={lineOutput}, Commands={commandOutput}, Errors={string.Join("|", result.Errors)}");
            }

            Debug.Log("=== Narrative Runtime Spike Smoke Test Finished ===");
        } catch (System.Exception ex) {
            Debug.LogError($"[NarrativeRuntimeSpikeSmokeTest Crash] {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static string BuildSampleScript() {
        return @"title: T0_01A_Opening_CG
---
<<play_visual cg_t0_01a_black_wake>>
protagonist: Morning again.#line:prologue.t0_01a.black_wake.line_01
protagonist: The note leaves more silence than answers.#line:prologue.t0_01a.repair_note.line_01
<<set_flag PrologueStarted>>
===
title: Other_Node
---
protagonist: not used
===";
    }
}
