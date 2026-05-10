using UnityEngine;

public static class ConfigValidationSmokeTest {
    public static void Run() {
        Debug.Log("=== Running Config Validation Smoke Test ===");

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;

        ConfigValidationReport report = ConfigValidator.ValidateLoadedConfigs();
        report.LogSummary();

        if (report.IsValid) {
            Debug.Log($"Config Validation PASSED. Warnings={report.Warnings.Count}");
        } else {
            Debug.LogError($"Config Validation FAILED. Errors={report.Errors.Count}, Warnings={report.Warnings.Count}");
        }

        Debug.Log("=== Config Validation Smoke Test Finished ===");
    }
}
