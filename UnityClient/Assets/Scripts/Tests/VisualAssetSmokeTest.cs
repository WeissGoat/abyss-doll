using UnityEngine;
using UnityEngine.UI;

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
                "prosthetic_pros_power_arm_icon",
                VisualAssetService.UIPanelInfoID,
                VisualAssetService.UIButtonPrimaryID,
                VisualAssetService.UIButtonSecondaryID,
                VisualAssetService.UIButtonDangerID,
                VisualAssetService.UIInventoryChassisPanelID,
                VisualAssetService.UIInventorySlotAvailableID,
                VisualAssetService.UIInventorySlotLockedID,
                VisualAssetService.UIInventorySlotHoverID,
                VisualAssetService.UIInventorySlotValidID,
                VisualAssetService.UIInventorySlotInvalidID,
                VisualAssetService.UILootPickupPanelID,
                VisualAssetService.UILootDropZoneID,
                VisualAssetService.UICombatEnemyCardID,
                VisualAssetService.UICombatEnemyCardSelectedID,
                VisualAssetService.UICombatEntityShadowID,
                VisualAssetService.UICombatTargetRingID,
                VisualAssetService.UICombatStatusBarHpID,
                VisualAssetService.UICombatStatusBarShieldID,
                VisualAssetService.UICombatApPipID,
                VisualAssetService.UICombatTurnBannerID,
                VisualAssetService.UIIconMoneyID,
                VisualAssetService.UIIconMaintenanceID,
                VisualAssetService.UIIconBillID,
                VisualAssetService.UIIconWarningID,
                VisualAssetService.UIIconShopChannelID,
                VisualAssetService.UIIconBlackMarketID,
                VisualAssetService.UIIconOrderID,
                VisualAssetService.UIIconFactionID,
                VisualAssetService.UIIconDeadlineID,
                VisualAssetService.UIIconRumorID,
                VisualAssetService.UIIconPriceUpID,
                VisualAssetService.UIIconPriceDownID,
                VisualAssetService.UIIconIncomeID,
                VisualAssetService.UIIconExpenseID,
                VisualAssetService.UIIconDebtRentID,
                VisualAssetService.UIIconWearRepairID,
                VisualAssetService.UIIconCorruptionPurifyID,
                VisualAssetService.UIIconDivePermitID,
                VisualAssetService.UIIconBusinessSettlementID,
                VisualAssetService.UIIconCustomerID,
                VisualAssetService.UIIconSaleSparkID,
                VisualAssetService.UIIconChassisUpgradeID,
                VisualAssetService.UIIconBlueprintID,
                VisualAssetService.UIIconMaterialNeedID,
                VisualAssetService.UIIconTouchID,
                VisualAssetService.UIIconTalkID,
                VisualAssetService.UIIconGiftID,
                VisualAssetService.UIIconMementoID,
                VisualAssetService.UIIconDiaryID,
                VisualAssetService.UIRoomMementoSlotID,
                VisualAssetService.DollRoomAtticBackgroundID,
                VisualAssetService.UIIconReputationID,
                VisualAssetService.UIIconTrustID,
                VisualAssetService.UIIconEventID,
                VisualAssetService.UIIconLoreID,
                VisualAssetService.UIIconSkipID,
                VisualAssetService.UISettlementOutcomeVictoryID,
                VisualAssetService.UISettlementOutcomeHpDefeatID,
                VisualAssetService.UISettlementOutcomeSanCollapseID,
                VisualAssetService.UISettlementOutcomeHpSanDefeatID,
                VisualAssetService.UISettlementOutcomePartyWipeID,
                VisualAssetService.SafeRoomBackgroundID,
                VisualAssetService.StairsRoomBackgroundID,
                VisualAssetService.LayerSelectBackgroundID,
                VisualAssetService.SettlementVictoryBackgroundID,
                VisualAssetService.SettlementDefeatBackgroundID,
                VisualAssetService.UIPanelMainID,
                VisualAssetService.UIListRowNormalID,
                VisualAssetService.UIListRowSelectedID,
                VisualAssetService.UISettlementVictoryPanelID,
                VisualAssetService.UISettlementDefeatPanelID,
                VisualAssetService.UIDungeonNodePlateID,
                VisualAssetService.UIDungeonRouteLineID,
                VisualAssetService.UIIconLockedID,
                VisualAssetService.UIIconEquippedID,
                VisualAssetService.UITitleDividerID
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

            string combatVisualID = VisualAssetService.ResolveMonsterCombatVisualID(monster);
            if (combatVisualID == "monster_mob_scavenger_bug_combat") {
                Debug.Log("Monster Combat Visual Resolver PASSED.");
            } else {
                Debug.LogError($"Monster Combat Visual Resolver FAILED. Got {combatVisualID}");
            }

            MonsterEntity legacyMonster = new MonsterEntity { MonsterID = "legacy_debug_monster", PortraitID = "monster_legacy_debug_monster_portrait" };
            string fallbackCombatVisualID = VisualAssetService.ResolveMonsterCombatVisualID(legacyMonster);
            if (fallbackCombatVisualID == "monster_legacy_debug_monster_portrait") {
                Debug.Log("Monster Combat Visual Portrait Fallback PASSED.");
            } else {
                Debug.LogError($"Monster Combat Visual Portrait Fallback FAILED. Got {fallbackCombatVisualID}");
            }

            if (VisualAssetService.ResolveNodeIconID(new CombatNode { NodeID = "layer_1_node_0", NodeIconID = "node_combat_icon" }) == "node_combat_icon"
                && VisualAssetService.ResolveNodeIconID(new CombatNode { NodeID = "layer_1_boss", NodeIconID = "node_boss_icon" }) == "node_boss_icon"
                && VisualAssetService.ResolveNodeIconID(new SafeRoomNode { NodeID = "layer_1_safe", NodeIconID = "node_safe_room_icon" }) == "node_safe_room_icon"
                && VisualAssetService.TryGetSprite(VisualAssetService.ResolveNodeIconID(new StairsNode { NodeID = "layer_1_end", NodeIconID = "node_stairs_icon" }), out _)) {
                Debug.Log("Node Icon Resolver PASSED.");
            } else {
                Debug.LogError("Node Icon Resolver FAILED.");
            }

            DungeonLayer layer = new DungeonLayer { LayerID = 2, MapBackgroundID = "bg_dungeon_layer_2" };
            if (VisualAssetService.ResolveDungeonMapBackgroundID(layer) == "bg_dungeon_layer_2"
                && VisualAssetService.ResolveCombatBackgroundID(layer) == "bg_combat_abyss"
                && VisualAssetService.ResolveWorkshopBackgroundID() == "bg_workshop_day"
                && VisualAssetService.ResolveSafeRoomBackgroundID() == VisualAssetService.SafeRoomBackgroundID
                && VisualAssetService.ResolveStairsRoomBackgroundID() == VisualAssetService.StairsRoomBackgroundID
                && VisualAssetService.ResolveLayerSelectBackgroundID() == VisualAssetService.LayerSelectBackgroundID
                && VisualAssetService.ResolveSettlementBackgroundID(true) == VisualAssetService.SettlementVictoryBackgroundID
                && VisualAssetService.ResolveSettlementBackgroundID(false) == VisualAssetService.SettlementDefeatBackgroundID) {
                Debug.Log("Background Resolver PASSED.");
            } else {
                Debug.LogError("Background Resolver FAILED.");
            }

            if (VisualAssetService.ResolveSettlementPanelID(true) == VisualAssetService.UISettlementVictoryPanelID
                && VisualAssetService.ResolveSettlementPanelID(false) == VisualAssetService.UISettlementDefeatPanelID) {
                Debug.Log("Settlement Panel Resolver PASSED.");
            } else {
                Debug.LogError("Settlement Panel Resolver FAILED.");
            }

            RunDisplaySpecAssertions();

            Debug.Log("=== Visual Asset Smoke Test Finished ===");
        } catch (System.Exception ex) {
            Debug.LogError($"[VisualAssetSmokeTest Crash] {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static void RunDisplaySpecAssertions() {
        GameObject iconObject = new GameObject("DisplaySpec_ItemIcon_Test", typeof(RectTransform));
        Image icon = iconObject.AddComponent<Image>();
        VisualUIHelper.ApplyContainSprite(icon, VisualAssetService.MissingSpriteVisualID, VisualDisplaySpecs.ItemIcon, Color.white, Color.red);
        if (Approximately(icon.rectTransform.sizeDelta, VisualDisplaySpecs.ItemIcon) && icon.preserveAspect && icon.GetComponent<LayoutElement>() != null) {
            Debug.Log("DisplaySpec Item Icon Container PASSED.");
        } else {
            Debug.LogError($"DisplaySpec Item Icon Container FAILED. Size={icon.rectTransform.sizeDelta}, PreserveAspect={icon.preserveAspect}");
        }
        DestroyTestObject(iconObject);

        GameObject portraitObject = new GameObject("DisplaySpec_MonsterPortrait_Test", typeof(RectTransform));
        Image portrait = portraitObject.AddComponent<Image>();
        VisualUIHelper.ApplyContainSprite(portrait, VisualAssetService.MissingSpriteVisualID, VisualDisplaySpecs.MonsterPortrait, Color.white, Color.red, false);
        if (Approximately(portrait.rectTransform.sizeDelta, VisualDisplaySpecs.MonsterPortrait) && portrait.preserveAspect) {
            Debug.Log("DisplaySpec Monster Portrait Container PASSED.");
        } else {
            Debug.LogError($"DisplaySpec Monster Portrait Container FAILED. Size={portrait.rectTransform.sizeDelta}, PreserveAspect={portrait.preserveAspect}");
        }
        DestroyTestObject(portraitObject);

        GameObject backgroundParent = new GameObject("DisplaySpec_BackgroundParent_Test", typeof(RectTransform));
        RectTransform parentRect = backgroundParent.GetComponent<RectTransform>();
        parentRect.sizeDelta = VisualDisplaySpecs.BackgroundReferenceViewport;
        Image background = VisualUIHelper.EnsurePanelBackground(backgroundParent.transform, null, "Background_Image");
        VisualUIHelper.ApplyCoverSprite(background, VisualAssetService.DefaultDungeonMapBackgroundID, Color.white, Color.black);
        AspectRatioFitter fitter = background.GetComponent<AspectRatioFitter>();
        RectTransform backgroundRect = background.rectTransform;
        bool backgroundStretchesParent = backgroundRect.anchorMin == Vector2.zero
            && backgroundRect.anchorMax == Vector2.one;
        if (fitter != null && fitter.aspectMode == AspectRatioFitter.AspectMode.EnvelopeParent && !background.raycastTarget && backgroundStretchesParent) {
            Debug.Log("DisplaySpec Background Cover Container PASSED.");
        } else {
            Debug.LogError("DisplaySpec Background Cover Container FAILED.");
        }
        DestroyTestObject(backgroundParent);
    }

    private static bool Approximately(Vector2 left, Vector2 right) {
        return Mathf.Abs(left.x - right.x) < 0.01f && Mathf.Abs(left.y - right.y) < 0.01f;
    }

    private static void DestroyTestObject(GameObject target) {
        if (target == null) {
            return;
        }

        if (Application.isPlaying) {
            Object.Destroy(target);
        } else {
            Object.DestroyImmediate(target);
        }
    }
}
