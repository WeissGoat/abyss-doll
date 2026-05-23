using System.Collections.Generic;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public sealed class InventoryGridLayoutValidationIssue {
    public string AssetPath;
    public string ObjectPath;
    public string Message;

    public override string ToString() {
        return $"{AssetPath}:{ObjectPath} - {Message}";
    }
}

public sealed class InventoryGridLayoutValidationReport {
    public int CheckedGridCount;
    public readonly List<InventoryGridLayoutValidationIssue> Issues = new List<InventoryGridLayoutValidationIssue>();

    public bool Passed => Issues.Count == 0;
}

public static class InventoryGridLayoutAssetValidator {
    private const float Tolerance = 0.01f;

    [MenuItem("Tools/P3/Inventory/Validate Grid Layout Assets")]
    public static void ValidateFromMenu() {
        InventoryGridLayoutValidationReport report = ValidateAllAssets(true);
        if (!report.Passed) {
            Debug.LogError($"[InventoryGridLayoutAssetValidator] FAILED. Checked={report.CheckedGridCount}, Issues={report.Issues.Count}");
            return;
        }

        Debug.Log($"[InventoryGridLayoutAssetValidator] PASSED. Checked={report.CheckedGridCount}");
    }

    [MenuItem("Tools/P3/Inventory/Repair Grid Layout Assets")]
    public static void RepairFromMenu() {
        int repaired = RepairAllAssets();
        Debug.Log($"[InventoryGridLayoutAssetValidator] Repaired {repaired} inventory grid layout assets.");
    }

    public static InventoryGridLayoutValidationReport ValidateAllAssets(bool logIssues) {
        InventoryGridLayoutValidationReport report = new InventoryGridLayoutValidationReport();
        foreach (string prefabPath in AssetDatabase.FindAssets("t:Prefab")) {
            ValidatePrefab(AssetDatabase.GUIDToAssetPath(prefabPath), report);
        }

        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        try {
            foreach (string sceneGuid in AssetDatabase.FindAssets("t:Scene")) {
                ValidateScene(AssetDatabase.GUIDToAssetPath(sceneGuid), report);
            }
        } finally {
            if (previousSetup != null && previousSetup.Length > 0) {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        if (logIssues) {
            foreach (InventoryGridLayoutValidationIssue issue in report.Issues) {
                Debug.LogError($"[InventoryGridLayoutAssetValidator] {issue}");
            }
        }

        return report;
    }

    public static int RepairAllAssets() {
        int repaired = 0;
        foreach (string prefabGuid in AssetDatabase.FindAssets("t:Prefab")) {
            string path = AssetDatabase.GUIDToAssetPath(prefabGuid);
            if (RepairPrefab(path)) {
                repaired++;
            }
        }

        SceneSetup[] previousSetup = EditorSceneManager.GetSceneManagerSetup();
        try {
            foreach (string sceneGuid in AssetDatabase.FindAssets("t:Scene")) {
                string path = AssetDatabase.GUIDToAssetPath(sceneGuid);
                if (RepairScene(path)) {
                    repaired++;
                }
            }
        } finally {
            if (previousSetup != null && previousSetup.Length > 0) {
                EditorSceneManager.RestoreSceneManagerSetup(previousSetup);
            }
        }

        AssetDatabase.SaveAssets();
        return repaired;
    }

    private static void ValidatePrefab(string assetPath, InventoryGridLayoutValidationReport report) {
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(assetPath);
        if (prefab == null) {
            return;
        }

        foreach (GridGenerator generator in prefab.GetComponentsInChildren<GridGenerator>(true)) {
            ValidateGenerator(assetPath, generator, report);
        }
    }

    private static void ValidateScene(string assetPath, InventoryGridLayoutValidationReport report) {
        Scene alreadyLoaded = FindLoadedScene(assetPath);
        if (alreadyLoaded.IsValid()) {
            ValidateSceneObjects(assetPath, alreadyLoaded, report);
            return;
        }

        Scene scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive);
        try {
            ValidateSceneObjects(assetPath, scene, report);
        } finally {
            EditorSceneManager.CloseScene(scene, true);
        }
    }

    private static void ValidateSceneObjects(string assetPath, Scene scene, InventoryGridLayoutValidationReport report) {
        foreach (GameObject root in scene.GetRootGameObjects()) {
            foreach (GridGenerator generator in root.GetComponentsInChildren<GridGenerator>(true)) {
                ValidateGenerator(assetPath, generator, report);
            }
        }
    }

    private static void ValidateGenerator(string assetPath, GridGenerator generator, InventoryGridLayoutValidationReport report) {
        if (generator == null) {
            return;
        }

        report.CheckedGridCount++;
        string objectPath = BuildObjectPath(generator.transform);
        if (generator.gridParent == null) {
            AddIssue(report, assetPath, objectPath, "GridGenerator.gridParent is missing.");
            return;
        }

        GridLayoutGroup layout = generator.gridParent.GetComponent<GridLayoutGroup>();
        if (layout == null) {
            AddIssue(report, assetPath, objectPath, "gridParent is missing GridLayoutGroup.");
            return;
        }

        if (!Approximately(layout.cellSize, InventoryDisplaySpec.CellSizeVector)) {
            AddIssue(report, assetPath, objectPath, $"cellSize={layout.cellSize}, expected={InventoryDisplaySpec.CellSizeVector}.");
        }

        if (!Approximately(layout.spacing, InventoryDisplaySpec.CellSpacingVector)) {
            AddIssue(report, assetPath, objectPath, $"spacing={layout.spacing}, expected={InventoryDisplaySpec.CellSpacingVector}.");
        }

        if (layout.constraint != GridLayoutGroup.Constraint.FixedColumnCount) {
            AddIssue(report, assetPath, objectPath, $"constraint={layout.constraint}, expected={GridLayoutGroup.Constraint.FixedColumnCount}.");
        }
    }

    private static bool RepairPrefab(string assetPath) {
        GameObject prefabRoot = PrefabUtility.LoadPrefabContents(assetPath);
        if (prefabRoot == null) {
            return false;
        }

        try {
            bool changed = false;
            foreach (GridGenerator generator in prefabRoot.GetComponentsInChildren<GridGenerator>(true)) {
                changed |= ApplySpec(generator);
            }

            if (changed) {
                PrefabUtility.SaveAsPrefabAsset(prefabRoot, assetPath);
            }

            return changed;
        } finally {
            PrefabUtility.UnloadPrefabContents(prefabRoot);
        }
    }

    private static bool RepairScene(string assetPath) {
        Scene scene = EditorSceneManager.OpenScene(assetPath, OpenSceneMode.Additive);
        bool changed = false;
        try {
            foreach (GameObject root in scene.GetRootGameObjects()) {
                foreach (GridGenerator generator in root.GetComponentsInChildren<GridGenerator>(true)) {
                    changed |= ApplySpec(generator);
                }
            }

            if (changed) {
                EditorSceneManager.MarkSceneDirty(scene);
                EditorSceneManager.SaveScene(scene);
            }
        } finally {
            EditorSceneManager.CloseScene(scene, true);
        }

        return changed;
    }

    private static bool ApplySpec(GridGenerator generator) {
        if (generator == null || generator.gridParent == null) {
            return false;
        }

        GridLayoutGroup layout = generator.gridParent.GetComponent<GridLayoutGroup>();
        if (layout == null) {
            return false;
        }

        bool changed =
            !Approximately(layout.cellSize, InventoryDisplaySpec.CellSizeVector)
            || !Approximately(layout.spacing, InventoryDisplaySpec.CellSpacingVector)
            || layout.constraint != GridLayoutGroup.Constraint.FixedColumnCount;

        if (changed) {
            Undo.RecordObject(layout, "Apply Inventory Display Spec");
            InventoryDisplaySpec.ApplyGridLayout(layout);
            EditorUtility.SetDirty(layout);
        }

        return changed;
    }

    private static Scene FindLoadedScene(string assetPath) {
        for (int index = 0; index < SceneManager.sceneCount; index++) {
            Scene scene = SceneManager.GetSceneAt(index);
            if (scene.path == assetPath) {
                return scene;
            }
        }

        return default;
    }

    private static bool Approximately(Vector2 actual, Vector2 expected) {
        return Mathf.Abs(actual.x - expected.x) <= Tolerance && Mathf.Abs(actual.y - expected.y) <= Tolerance;
    }

    private static void AddIssue(InventoryGridLayoutValidationReport report, string assetPath, string objectPath, string message) {
        report.Issues.Add(new InventoryGridLayoutValidationIssue {
            AssetPath = assetPath,
            ObjectPath = objectPath,
            Message = message
        });
    }

    private static string BuildObjectPath(Transform transform) {
        if (transform == null) {
            return string.Empty;
        }

        string path = transform.name;
        Transform current = transform.parent;
        while (current != null) {
            path = $"{current.name}/{path}";
            current = current.parent;
        }

        return path;
    }
}
