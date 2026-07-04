#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PrologueHalfOpenFlowEditorRunner {
    public static void RunFromBatchmode() {
        PrologueHalfOpenNarrativeFlowSmokeTest.Run();
        Debug.Log("[PrologueHalfOpenFlowEditorRunner] Finished.");
        if (Application.isBatchMode) {
            EditorApplication.Exit(0);
        }
    }
}
#endif
