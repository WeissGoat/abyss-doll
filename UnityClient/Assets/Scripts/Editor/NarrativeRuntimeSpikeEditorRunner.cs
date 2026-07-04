#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class NarrativeRuntimeSpikeEditorRunner {
    public static void RunFromBatchmode() {
        NarrativeRuntimeSpikeSmokeTest.Run();
        Debug.Log("[NarrativeRuntimeSpikeEditorRunner] Finished.");
        if (Application.isBatchMode) {
            EditorApplication.Exit(0);
        }
    }
}
#endif
