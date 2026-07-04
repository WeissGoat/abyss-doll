#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PrologueOpeningFlowEditorRunner {
    public static void RunFromBatchmode() {
        PrologueOpeningNarrativeFlowSmokeTest.Run();
        Debug.Log("[PrologueOpeningFlowEditorRunner] Finished.");
        if (Application.isBatchMode) {
            EditorApplication.Exit(0);
        }
    }
}
#endif
