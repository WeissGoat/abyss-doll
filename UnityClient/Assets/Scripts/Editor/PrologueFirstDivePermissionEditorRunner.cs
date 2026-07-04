#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;

public static class PrologueFirstDivePermissionEditorRunner {
    public static void RunFromBatchmode() {
        PrologueFirstDivePermissionSmokeSuite.Run();
        Debug.Log("[PrologueFirstDivePermissionEditorRunner] Finished.");
        if (Application.isBatchMode) {
            EditorApplication.Exit(0);
        }
    }
}
#endif
