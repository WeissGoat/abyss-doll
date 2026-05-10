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

            VisualAssetRegistry registry = Resources.Load<VisualAssetRegistry>("VisualAssetRegistry");
            if (registry != null && registry.Entries != null && registry.Entries.Count >= 30 && registry.MissingSprite != null) {
                Debug.Log("Approved Sprite Registry Count PASSED.");
            } else {
                int count = registry?.Entries?.Count ?? 0;
                Debug.LogError($"Approved Sprite Registry Count FAILED. Count={count}, MissingSprite={(registry != null && registry.MissingSprite != null)}");
            }

            string[] representativeVisualIDs = {
                "item_gear_wooden_shield_icon",
                "monster_mob_scavenger_bug_portrait",
                "node_safe_room_icon",
                "bg_dungeon_map",
                "doll_proto_0_stand",
                "prosthetic_pros_power_arm_icon"
            };

            bool allRepresentativeSpritesFound = true;
            foreach (string visualID in representativeVisualIDs) {
                if (!VisualAssetService.TryGetSprite(visualID, out Sprite sprite) || sprite == null) {
                    allRepresentativeSpritesFound = false;
                    Debug.LogError($"Approved Sprite Registration FAILED. Missing VisualID={visualID}");
                }
            }

            if (allRepresentativeSpritesFound) {
                Debug.Log("Approved Sprite Registration PASSED.");
            }

            Debug.Log("=== Visual Asset Smoke Test Finished ===");
        } catch (System.Exception ex) {
            Debug.LogError($"[VisualAssetSmokeTest Crash] {ex.Message}\n{ex.StackTrace}");
        }
    }
}
