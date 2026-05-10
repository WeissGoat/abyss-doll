using UnityEngine;
using UnityEngine.UI;

public static class VisualAssetService {
    private const string DefaultRegistryResourcePath = "VisualAssetRegistry";
    public const string MissingSpriteVisualID = "ui_missing_sprite";
    public const string WorkshopBackgroundID = "bg_workshop_day";
    public const string CombatBackgroundID = "bg_combat_abyss";
    public const string DefaultDungeonMapBackgroundID = "bg_dungeon_map";
    public const string CombatNodeIconID = "node_combat_icon";
    public const string BossNodeIconID = "node_boss_icon";
    public const string SafeRoomNodeIconID = "node_safe_room_icon";
    public const string StairsNodeIconID = "node_stairs_icon";

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
        VisualAssetRegistry registry = ResolveRegistry();
        if (registry != null && registry.TryGetEntry(visualID, out var entry) && entry.Prefab != null) {
            return entry.Prefab;
        }

        return registry != null ? registry.MissingPrefab : null;
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

public static class VisualUIHelper {
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

            RectTransform rect = bgObj.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

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
}
