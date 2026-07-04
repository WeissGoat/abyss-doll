#if UNITY_EDITOR
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

public static class P3ExpressionPreviewSceneBuilder {
    private const string PreviewScenePath = "Assets/Scenes/P3ExpressionPreview.unity";

    [MenuItem("Tools/P3 Preview/Create Expression Preview Scene")]
    public static void CreateExpressionPreviewScene() {
        Scene scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
        scene.name = "P3ExpressionPreview";

        GameObject cameraObject = new GameObject("Preview Camera");
        Camera camera = cameraObject.AddComponent<Camera>();
        camera.clearFlags = CameraClearFlags.SolidColor;
        camera.backgroundColor = new Color(0.045f, 0.038f, 0.055f, 1f);
        camera.orthographic = true;
        camera.orthographicSize = 5.4f;

        GameObject previewObject = new GameObject("P3 Expression Preview");
        previewObject.AddComponent<P3ExpressionPreviewController>();

        EnsureFolder("Assets/Scenes");
        EditorSceneManager.SaveScene(scene, PreviewScenePath);
        AssetDatabase.Refresh();
        Debug.Log($"[P3ExpressionPreviewSceneBuilder] Created preview scene at {PreviewScenePath}.");
    }

    private static void EnsureFolder(string folderPath) {
        if (AssetDatabase.IsValidFolder(folderPath)) {
            return;
        }

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++) {
            string next = current + "/" + parts[i];
            if (!AssetDatabase.IsValidFolder(next)) {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }
}
#endif
