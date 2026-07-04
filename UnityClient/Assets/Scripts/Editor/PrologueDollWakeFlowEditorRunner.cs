#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PrologueDollWakeFlowEditorRunner {
    public static void RunFromBatchmode() {
        PrologueDollWakeNarrativeFlowSmokeTest.Run();
        Debug.Log("[PrologueDollWakeFlowEditorRunner] Finished.");
        if (Application.isBatchMode) {
            EditorApplication.Exit(0);
        }
    }
}
#endif
