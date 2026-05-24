using UnityEngine;

public static class InventoryGridLayoutAssetValidatorTest {
    public static void Run() {
        Debug.Log("=== Running Inventory Grid Layout Asset Validator Test ===");

        if (Application.isPlaying) {
            Debug.LogWarning("Inventory Grid Layout Asset Validator skipped in PlayMode because it opens scenes through EditorSceneManager.");
            Debug.Log("=== Inventory Grid Layout Asset Validator Test Finished ===");
            return;
        }

        InventoryGridLayoutValidationReport report = InventoryGridLayoutAssetValidator.ValidateAllAssets(false);
        if (report.Passed) {
            Debug.Log($"Inventory Grid Layout Asset Validator PASSED. Checked={report.CheckedGridCount}");
        } else {
            foreach (InventoryGridLayoutValidationIssue issue in report.Issues) {
                Debug.LogError($"Inventory Grid Layout Asset Validator issue: {issue}");
            }

            Debug.LogError($"Inventory Grid Layout Asset Validator FAILED. Checked={report.CheckedGridCount}, Issues={report.Issues.Count}");
        }

        Debug.Log("=== Inventory Grid Layout Asset Validator Test Finished ===");
    }
}
