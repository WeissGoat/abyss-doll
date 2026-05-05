using UnityEngine;

public static class VisualAssetService {
    private const string DefaultRegistryResourcePath = "VisualAssetRegistry";
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
