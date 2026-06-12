using UnityEngine;
using UnityEngine.UI;

public static class VisualAssetService {
    private const string DefaultRegistryResourcePath = "VisualAssetRegistry";
    public const string MissingSpriteVisualID = "ui_missing_sprite";
    public const string WorkshopBackgroundID = "bg_workshop_day";
    public const string CombatBackgroundID = "bg_combat_abyss";
    public const string DefaultDungeonMapBackgroundID = "bg_dungeon_map";
    public const string SafeRoomBackgroundID = "bg_safe_room";
    public const string StairsRoomBackgroundID = "bg_stairs_room";
    public const string LayerSelectBackgroundID = "bg_layer_select";
    public const string SettlementVictoryBackgroundID = "bg_settlement_victory";
    public const string SettlementDefeatBackgroundID = "bg_settlement_defeat";
    public const string CombatNodeIconID = "node_combat_icon";
    public const string BossNodeIconID = "node_boss_icon";
    public const string SafeRoomNodeIconID = "node_safe_room_icon";
    public const string StairsNodeIconID = "node_stairs_icon";
    public const string UIPanelInfoID = "ui_panel_info";
    public const string UIPanelMainID = "ui_panel_main";
    public const string UIButtonPrimaryID = "ui_button_primary";
    public const string UIButtonSecondaryID = "ui_button_secondary";
    public const string UIButtonDangerID = "ui_button_danger";
    public const string UIListRowNormalID = "ui_list_row_normal";
    public const string UIListRowSelectedID = "ui_list_row_selected";
    public const string UISettlementVictoryPanelID = "ui_settlement_victory_panel";
    public const string UISettlementDefeatPanelID = "ui_settlement_defeat_panel";
    public const string UIDungeonNodePlateID = "ui_dungeon_node_plate";
    public const string UIDungeonRouteLineID = "ui_dungeon_route_line";
    public const string UIIconLockedID = "ui_icon_locked";
    public const string UIIconEquippedID = "ui_icon_equipped";
    public const string UITitleDividerID = "ui_title_divider";
    public const string UIInventoryChassisPanelID = "ui_inventory_chassis_panel";
    public const string UIInventorySlotAvailableID = "ui_inventory_slot_available";
    public const string UIInventorySlotLockedID = "ui_inventory_slot_locked";
    public const string UIInventorySlotHoverID = "ui_inventory_slot_hover";
    public const string UIInventorySlotValidID = "ui_inventory_slot_valid";
    public const string UIInventorySlotInvalidID = "ui_inventory_slot_invalid";
    public const string UILootPickupPanelID = "ui_loot_pickup_panel";
    public const string UILootDropZoneID = "ui_loot_drop_zone";
    public const string UICombatEnemyCardID = "ui_combat_enemy_card";
    public const string UICombatEnemyCardSelectedID = "ui_combat_enemy_card_selected";
    public const string UICombatEntityShadowID = "ui_combat_entity_shadow";
    public const string UICombatTargetRingID = "ui_combat_target_ring";
    public const string UICombatStatusBarHpID = "ui_combat_status_bar_hp";
    public const string UICombatStatusBarShieldID = "ui_combat_status_bar_shield";
    public const string UICombatApPipID = "ui_combat_ap_pip";
    public const string UICombatTurnBannerID = "ui_combat_turn_banner";
    public const string UIIconMoneyID = "ui_icon_money";
    public const string UIIconMaintenanceID = "ui_icon_maintenance";
    public const string UIIconBillID = "ui_icon_bill";
    public const string UIIconWarningID = "ui_icon_warning";
    public const string UIIconShopChannelID = "ui_icon_shop_channel";
    public const string UIIconBlackMarketID = "ui_icon_black_market";
    public const string UIIconOrderID = "ui_icon_order";
    public const string UIIconFactionID = "ui_icon_faction";
    public const string UIIconDeadlineID = "ui_icon_deadline";
    public const string UIIconRumorID = "ui_icon_rumor";
    public const string UIIconPriceUpID = "ui_icon_price_up";
    public const string UIIconPriceDownID = "ui_icon_price_down";
    public const string UIIconIncomeID = "ui_icon_income";
    public const string UIIconExpenseID = "ui_icon_expense";
    public const string UIIconDebtRentID = "ui_icon_debt_rent";
    public const string UIIconWearRepairID = "ui_icon_wear_repair";
    public const string UIIconCorruptionPurifyID = "ui_icon_corruption_purify";
    public const string UIIconDivePermitID = "ui_icon_dive_permit";
    public const string UIIconBusinessSettlementID = "ui_icon_business_settlement";
    public const string UIIconCustomerID = "ui_icon_customer";
    public const string UIIconSaleSparkID = "ui_icon_sale_spark";
    public const string UIIconChassisUpgradeID = "ui_icon_chassis_upgrade";
    public const string UIIconBlueprintID = "ui_icon_blueprint";
    public const string UIIconMaterialNeedID = "ui_icon_material_need";
    public const string UIIconTouchID = "ui_icon_touch";
    public const string UIIconTalkID = "ui_icon_talk";
    public const string UIIconGiftID = "ui_icon_gift";
    public const string UIIconMementoID = "ui_icon_memento";
    public const string UIIconDiaryID = "ui_icon_diary";
    public const string UIRoomMementoSlotID = "ui_room_memento_slot";
    public const string DollRoomAtticBackgroundID = "bg_doll_room_attic";
    public const string UIIconReputationID = "ui_icon_reputation";
    public const string UIIconTrustID = "ui_icon_trust";
    public const string UIIconEventID = "ui_icon_event";
    public const string UIIconLoreID = "ui_icon_lore";
    public const string UIIconSkipID = "ui_icon_skip";
    public const string UISettlementOutcomeVictoryID = "ui_settlement_outcome_victory";
    public const string UISettlementOutcomeHpDefeatID = "ui_settlement_outcome_hp_defeat";
    public const string UISettlementOutcomeSanCollapseID = "ui_settlement_outcome_san_collapse";
    public const string UISettlementOutcomeHpSanDefeatID = "ui_settlement_outcome_hp_san_defeat";
    public const string UISettlementOutcomePartyWipeID = "ui_settlement_outcome_party_wipe";

    private static VisualAssetRegistry _registry;
    private static Sprite _runtimeMissingSprite;

    public static void SetRegistry(VisualAssetRegistry registry) {
        _registry = registry;
        _registry?.RebuildLookup();
    }

    public static Sprite GetSprite(string visualID) {
        if (TryGetSprite(visualID, out Sprite sprite)) {
            return sprite;
        }

        VisualAssetRegistry registry = ResolveRegistry();
        if (registry != null && registry.MissingSprite != null) {
            return registry.MissingSprite;
        }

        return GetRuntimeMissingSprite();
    }

    public static bool TryGetSprite(string visualID, out Sprite sprite) {
        sprite = null;
        VisualAssetRegistry registry = ResolveRegistry();
        if (registry != null && registry.TryGetEntry(visualID, out var entry) && entry.Sprite != null) {
            sprite = entry.Sprite;
            return true;
        }

        return false;
    }

    public static GameObject GetPrefab(string visualID) {
        if (TryGetPrefab(visualID, out GameObject prefab)) {
            return prefab;
        }

        VisualAssetRegistry registry = ResolveRegistry();
        return registry != null ? registry.MissingPrefab : null;
    }

    public static bool TryGetPrefab(string visualID, out GameObject prefab) {
        prefab = null;
        VisualAssetRegistry registry = ResolveRegistry();
        if (registry != null && registry.TryGetEntry(visualID, out var entry) && entry.Prefab != null) {
            prefab = entry.Prefab;
            return true;
        }

        return false;
    }

    public static AudioClip GetAudioClip(string visualID) {
        VisualAssetRegistry registry = ResolveRegistry();
        if (registry != null && registry.TryGetEntry(visualID, out var entry) && entry.AudioClip != null) {
            return entry.AudioClip;
        }

        return null;
    }

    public static string ResolveItemIconID(ItemEntity item) {
        if (item == null) {
            return string.Empty;
        }

        if (!string.IsNullOrEmpty(item.IconID)) {
            return item.IconID;
        }

        return string.IsNullOrEmpty(item.ConfigID) ? string.Empty : $"item_{item.ConfigID}_icon";
    }

    public static string ResolveProstheticIconID(ProstheticEntity prosthetic) {
        if (prosthetic == null) {
            return MissingSpriteVisualID;
        }

        if (!string.IsNullOrEmpty(prosthetic.IconID)) {
            return prosthetic.IconID;
        }

        return string.IsNullOrEmpty(prosthetic.ProstheticID)
            ? MissingSpriteVisualID
            : $"prosthetic_{prosthetic.ProstheticID}_icon";
    }

    public static string ResolveMonsterPortraitID(MonsterEntity monster) {
        if (monster == null) {
            return MissingSpriteVisualID;
        }

        if (!string.IsNullOrEmpty(monster.PortraitID)) {
            return monster.PortraitID;
        }

        return string.IsNullOrEmpty(monster.MonsterID)
            ? MissingSpriteVisualID
            : $"monster_{monster.MonsterID}_portrait";
    }

    public static string ResolveMonsterCombatVisualID(MonsterEntity monster) {
        if (monster == null) {
            return MissingSpriteVisualID;
        }

        if (!string.IsNullOrEmpty(monster.CombatVisualID)) {
            return monster.CombatVisualID;
        }

        return ResolveMonsterPortraitID(monster);
    }

    public static string ResolveNodeIconID(NodeBase node) {
        if (node != null && !string.IsNullOrEmpty(node.NodeIconID)) {
            return node.NodeIconID;
        }

        if (node is StairsNode) {
            return StairsNodeIconID;
        }

        if (node is SafeRoomNode) {
            return SafeRoomNodeIconID;
        }

        if (node is CombatNode) {
            return CombatNodeIconID;
        }

        return MissingSpriteVisualID;
    }

    public static string ResolveDungeonMapBackgroundID(DungeonLayer layer) {
        if (layer != null && !string.IsNullOrEmpty(layer.MapBackgroundID)) {
            return layer.MapBackgroundID;
        }

        if (layer != null) {
            string layerBackgroundID = $"bg_dungeon_layer_{layer.LayerID}";
            if (TryGetSprite(layerBackgroundID, out _)) {
                return layerBackgroundID;
            }
        }

        return DefaultDungeonMapBackgroundID;
    }

    public static string ResolveCombatBackgroundID(DungeonLayer layer = null) {
        return CombatBackgroundID;
    }

    public static string ResolveWorkshopBackgroundID() {
        return WorkshopBackgroundID;
    }

    public static string ResolveSafeRoomBackgroundID() {
        return SafeRoomBackgroundID;
    }

    public static string ResolveStairsRoomBackgroundID() {
        return StairsRoomBackgroundID;
    }

    public static string ResolveLayerSelectBackgroundID() {
        return LayerSelectBackgroundID;
    }

    public static string ResolveSettlementBackgroundID(bool isVictory) {
        return isVictory ? SettlementVictoryBackgroundID : SettlementDefeatBackgroundID;
    }

    public static string ResolveSettlementPanelID(bool isVictory) {
        return isVictory ? UISettlementVictoryPanelID : UISettlementDefeatPanelID;
    }

    private static VisualAssetRegistry ResolveRegistry() {
        if (_registry != null) {
            return _registry;
        }

        _registry = Resources.Load<VisualAssetRegistry>(DefaultRegistryResourcePath);
        if (_registry != null) {
            _registry.RebuildLookup();
        }

        return _registry;
    }

    private static Sprite GetRuntimeMissingSprite() {
        if (_runtimeMissingSprite != null) {
            return _runtimeMissingSprite;
        }

        Texture2D texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
        texture.SetPixels(new[] {
            new Color(0.9f, 0.2f, 0.2f, 1f),
            new Color(0.15f, 0.15f, 0.15f, 1f),
            new Color(0.15f, 0.15f, 0.15f, 1f),
            new Color(0.9f, 0.2f, 0.2f, 1f)
        });
        texture.Apply();
        texture.name = "RuntimeMissingVisualTexture";

        _runtimeMissingSprite = Sprite.Create(texture, new Rect(0f, 0f, 2f, 2f), new Vector2(0.5f, 0.5f), 2f);
        _runtimeMissingSprite.name = "RuntimeMissingVisualSprite";
        return _runtimeMissingSprite;
    }
}

public static class VisualDisplaySpecs {
    public static readonly Vector2 ReferenceResolution = new Vector2(1920f, 1080f);
    public static readonly Vector2 ItemIcon = new Vector2(64f, 64f);
    public static readonly Vector2 UIIcon = new Vector2(64f, 64f);
    public static readonly Vector2 NodeIcon = new Vector2(80f, 80f);
    public static readonly Vector2 ProstheticIcon = new Vector2(80f, 80f);
    public static readonly Vector2 MissingSprite = new Vector2(64f, 64f);
    public static readonly Vector2 MonsterPortrait = new Vector2(320f, 320f);
    public static readonly Vector2 MonsterCombatEntity = new Vector2(300f, 360f);
    public static readonly Vector2 MonsterCombatEntityLarge = new Vector2(360f, 420f);
    public static readonly Vector2 CombatEntityShadow = new Vector2(260f, 78f);
    public static readonly Vector2 CombatTargetRing = new Vector2(240f, 96f);
    public static readonly Vector2 CombatDoll = new Vector2(360f, 560f);
    public static readonly Vector2 DollStand = new Vector2(420f, 720f);
    public static readonly Vector2 ChassisFrame = new Vector2(512f, 512f);
    public static readonly Vector2 BackgroundReferenceViewport = new Vector2(1920f, 1080f);
    public const float BackgroundReferenceAspect = 16f / 9f;
}

public static class VisualUIHelper {
    private static Sprite _runtimeSolidColorSprite;

    public static Image EnsurePanelBackground(Transform parent, Image currentImage, string objectName) {
        if (parent == null) {
            return currentImage;
        }

        Image image = currentImage;
        if (image == null) {
            Transform existing = parent.Find(objectName);
            if (existing != null) {
                image = existing.GetComponent<Image>();
            }
        }

        if (image == null) {
            GameObject bgObj = new GameObject(objectName);
            bgObj.transform.SetParent(parent, false);
            image = bgObj.AddComponent<Image>();
        }

        RectTransform rect = image.GetComponent<RectTransform>();
        rect.anchorMin = Vector2.zero;
        rect.anchorMax = Vector2.one;
        rect.pivot = new Vector2(0.5f, 0.5f);
        rect.offsetMin = Vector2.zero;
        rect.offsetMax = Vector2.zero;

        image.raycastTarget = false;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.transform.SetAsFirstSibling();
        return image;
    }

    public static bool ApplySprite(Image image, string visualID, Color registeredColor, Color missingColor, bool preserveAspect = true) {
        if (image == null) {
            return false;
        }

        bool hasRegisteredSprite = VisualAssetService.TryGetSprite(visualID, out Sprite sprite);
        image.sprite = hasRegisteredSprite ? sprite : VisualAssetService.GetSprite(visualID);
        image.color = hasRegisteredSprite ? registeredColor : missingColor;
        image.preserveAspect = preserveAspect;
        image.raycastTarget = false;
        return hasRegisteredSprite;
    }

    public static bool ApplySimpleSprite(Image image, string visualID, Color registeredColor, Color missingColor, bool raycastTarget, bool preserveAspect = true) {
        bool hasRegisteredSprite = ApplySprite(image, visualID, registeredColor, missingColor, preserveAspect);
        if (image == null) {
            return false;
        }

        image.type = Image.Type.Simple;
        image.raycastTarget = raycastTarget;
        return hasRegisteredSprite;
    }

    public static bool ApplySlicedSprite(Image image, string visualID, Color registeredColor, Color missingColor, bool raycastTarget) {
        bool hasRegisteredSprite = ApplySprite(image, visualID, registeredColor, missingColor, false);
        if (image == null) {
            return false;
        }

        image.type = Image.Type.Sliced;
        image.raycastTarget = raycastTarget;
        return hasRegisteredSprite;
    }

    public static void ApplySolidColor(Image image, Color color, bool raycastTarget = false) {
        if (image == null) {
            return;
        }

        image.sprite = GetRuntimeSolidColorSprite();
        image.color = color;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = raycastTarget;
    }

    public static void ApplyButtonSkin(Button button, string visualID, Color missingColor) {
        if (button == null) {
            return;
        }

        Image image = button.GetComponent<Image>();
        if (image == null) {
            image = button.gameObject.AddComponent<Image>();
            button.targetGraphic = image;
        }

        ApplySlicedSprite(image, visualID, Color.white, missingColor, true);
        button.targetGraphic = image;
    }

    public static void ApplyInventorySlotSkin(Image image, bool isLocked, string stateVisualID = null) {
        string visualID = stateVisualID;
        if (string.IsNullOrEmpty(visualID)) {
            visualID = isLocked
                ? VisualAssetService.UIInventorySlotLockedID
                : VisualAssetService.UIInventorySlotAvailableID;
        }

        Color missingColor = isLocked
            ? new Color(0.18f, 0.18f, 0.18f, 1f)
            : new Color(1f, 1f, 1f, 0.5f);
        ApplySimpleSprite(image, visualID, Color.white, missingColor, true, false);
    }

    public static bool ApplyContainSprite(Image image, string visualID, Vector2 containerSize, Color registeredColor, Color missingColor, bool bindLayoutElement = true) {
        if (image == null) {
            return false;
        }

        RemoveAspectRatioFitter(image);
        ApplyFixedContainer(image.rectTransform, containerSize, bindLayoutElement);
        bool hasRegisteredSprite = ApplySprite(image, visualID, registeredColor, missingColor, true);
        image.type = Image.Type.Simple;
        return hasRegisteredSprite;
    }

    public static bool ApplyCoverSprite(Image image, string visualID, Color registeredColor, Color missingColor) {
        if (image == null) {
            return false;
        }

        bool hasRegisteredSprite = VisualAssetService.TryGetSprite(visualID, out Sprite sprite);
        image.sprite = hasRegisteredSprite ? sprite : VisualAssetService.GetSprite(visualID);
        image.color = hasRegisteredSprite ? registeredColor : missingColor;
        image.type = Image.Type.Simple;
        image.preserveAspect = false;
        image.raycastTarget = false;

        AspectRatioFitter fitter = image.GetComponent<AspectRatioFitter>();
        if (fitter == null) {
            fitter = image.gameObject.AddComponent<AspectRatioFitter>();
        }

        fitter.aspectMode = AspectRatioFitter.AspectMode.EnvelopeParent;
        fitter.aspectRatio = ResolveSpriteAspectRatio(image.sprite);
        return hasRegisteredSprite;
    }

    private static Sprite GetRuntimeSolidColorSprite() {
        if (_runtimeSolidColorSprite != null) {
            return _runtimeSolidColorSprite;
        }

        Texture2D texture = new Texture2D(1, 1, TextureFormat.RGBA32, false) {
            name = "RuntimeSolidColorSpriteTexture",
            filterMode = FilterMode.Point,
            wrapMode = TextureWrapMode.Clamp
        };
        texture.SetPixel(0, 0, Color.white);
        texture.Apply(false, true);

        _runtimeSolidColorSprite = Sprite.Create(texture, new Rect(0f, 0f, 1f, 1f), new Vector2(0.5f, 0.5f), 1f);
        _runtimeSolidColorSprite.name = "runtime_solid_color_sprite";
        return _runtimeSolidColorSprite;
    }

    public static void ApplyFixedContainer(RectTransform rect, Vector2 containerSize, bool bindLayoutElement = true) {
        if (rect == null) {
            return;
        }

        rect.sizeDelta = containerSize;

        if (!bindLayoutElement) {
            return;
        }

        LayoutElement layoutElement = rect.GetComponent<LayoutElement>();
        if (layoutElement == null) {
            layoutElement = rect.gameObject.AddComponent<LayoutElement>();
        }

        layoutElement.minWidth = containerSize.x;
        layoutElement.preferredWidth = containerSize.x;
        layoutElement.flexibleWidth = 0f;
        layoutElement.minHeight = containerSize.y;
        layoutElement.preferredHeight = containerSize.y;
        layoutElement.flexibleHeight = 0f;
    }

    private static float ResolveSpriteAspectRatio(Sprite sprite) {
        if (sprite == null || sprite.rect.height <= 0f) {
            return VisualDisplaySpecs.BackgroundReferenceAspect;
        }

        return sprite.rect.width / sprite.rect.height;
    }

    private static void RemoveAspectRatioFitter(Image image) {
        AspectRatioFitter fitter = image.GetComponent<AspectRatioFitter>();
        if (fitter == null) {
            return;
        }

        if (Application.isPlaying) {
            Object.Destroy(fitter);
        } else {
            Object.DestroyImmediate(fitter);
        }
    }
}
