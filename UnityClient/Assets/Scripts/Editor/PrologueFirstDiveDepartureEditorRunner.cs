#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PrologueFirstDiveDepartureEditorRunner {
    public static void RunFromBatchmode() {
        PrologueFirstDiveDepartureSmokeTest.Run();
        Debug.Log("[PrologueFirstDiveDepartureEditorRunner] Finished.");
        if (Application.isBatchMode) {
            EditorApplication.Exit(0);
        }
    }
}
#endif
