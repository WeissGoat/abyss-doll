using UnityEngine;

public static class VisualAssetSmokeTest {
    public static void Run() {
        try {
            Debug.Log("=== Running Visual Asset Smoke Test ===");

            CoreBackend core = new CoreBackend();
            core.InitAllSystems();
            GameRoot.Core = core;

            ItemEntity item = ConfigManager.CreateItem("gear_tactical_blade");
            if (item == null) {
                Debug.LogError("Visual Asset Smoke Test FAILED: missing test item.");
                return;
            }

            string iconID = VisualAssetService.ResolveItemIconID(item);
            if (iconID == "item_gear_tactical_blade_icon") {
                Debug.Log("Item Explicit IconID PASSED.");
            } else {
                Debug.LogError($"Item Explicit IconID FAILED. Got {iconID}");
            }

            ItemEntity legacyItem = new ItemEntity { ConfigID = "legacy_debug_item" };
            string legacyIconID = VisualAssetService.ResolveItemIconID(legacyItem);
            if (legacyIconID == "item_legacy_debug_item_icon") {
                Debug.Log("Item IconID Fallback PASSED.");
            } else {
                Debug.LogError($"Item IconID Fallback FAILED. Got {legacyIconID}");
            }

            Sprite fallbackSprite = VisualAssetService.GetSprite(iconID);
            if (fallbackSprite != null) {
                Debug.Log("Missing Sprite Fallback PASSED.");
            } else {
                Debug.LogError("Missing Sprite Fallback FAILED.");
            }

            Debug.Log("=== Visual Asset Smoke Test Finished ===");
        } catch (System.Exception ex) {
            Debug.LogError($"[VisualAssetSmokeTest Crash] {ex.Message}\n{ex.StackTrace}");
        }
    }
}
