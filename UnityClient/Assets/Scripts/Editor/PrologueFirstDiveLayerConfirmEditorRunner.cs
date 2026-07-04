#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PrologueFirstDiveLayerConfirmEditorRunner {
    public static void RunFromBatchmode() {
        PrologueFirstDiveLayerConfirmSmokeTest.Run();
        Debug.Log("[PrologueFirstDiveLayerConfirmEditorRunner] Finished.");
        if (Application.isBatchMode) {
            EditorApplication.Exit(0);
        }
    }
}
#endif
