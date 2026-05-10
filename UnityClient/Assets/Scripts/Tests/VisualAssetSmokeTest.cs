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
                "monster_elite_mutant_amalgam_portrait",
                "node_combat_icon",
                "node_boss_icon",
                "node_safe_room_icon",
                "bg_dungeon_map",
                "bg_dungeon_layer_2",
                "bg_combat_abyss",
                "bg_workshop_day",
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

            MonsterEntity monster = ConfigManager.Monsters["mob_scavenger_bug"];
            string portraitID = VisualAssetService.ResolveMonsterPortraitID(monster);
            if (portraitID == "monster_mob_scavenger_bug_portrait") {
                Debug.Log("Monster Portrait Resolver PASSED.");
            } else {
                Debug.LogError($"Monster Portrait Resolver FAILED. Got {portraitID}");
            }

            if (VisualAssetService.ResolveNodeIconID(new CombatNode { NodeID = "layer_1_node_0", NodeIconID = "node_combat_icon" }) == "node_combat_icon"
                && VisualAssetService.ResolveNodeIconID(new CombatNode { NodeID = "layer_1_boss", NodeIconID = "node_boss_icon" }) == "node_boss_icon"
                && VisualAssetService.ResolveNodeIconID(new SafeRoomNode { NodeID = "layer_1_safe", NodeIconID = "node_safe_room_icon" }) == "node_safe_room_icon"
                && VisualAssetService.GetSprite(VisualAssetService.ResolveNodeIconID(new StairsNode { NodeID = "layer_1_end", NodeIconID = "node_stairs_icon" })) != null) {
                Debug.Log("Node Icon Resolver PASSED.");
            } else {
                Debug.LogError("Node Icon Resolver FAILED.");
            }

            DungeonLayer layer = new DungeonLayer { LayerID = 2, MapBackgroundID = "bg_dungeon_layer_2" };
            if (VisualAssetService.ResolveDungeonMapBackgroundID(layer) == "bg_dungeon_layer_2"
                && VisualAssetService.ResolveCombatBackgroundID(layer) == "bg_combat_abyss"
                && VisualAssetService.ResolveWorkshopBackgroundID() == "bg_workshop_day") {
                Debug.Log("Background Resolver PASSED.");
            } else {
                Debug.LogError("Background Resolver FAILED.");
            }

            Debug.Log("=== Visual Asset Smoke Test Finished ===");
        } catch (System.Exception ex) {
            Debug.LogError($"[VisualAssetSmokeTest Crash] {ex.Message}\n{ex.StackTrace}");
        }
    }
}
