#if UNITY_EDITOR
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEngine;

public static class VisualAssetRegistryEditorTools {
    private const string RegistryPath = "Assets/Resources/VisualAssetRegistry.asset";
    private const string ApprovedArtFolder = "Assets/Art/Approved";
    private const string MissingSpriteVisualID = "ui_missing_sprite";

    [MenuItem("Tools/P3 Art/Rebuild Item Icon Registry")]
    public static void RebuildItemIconRegistryFromApprovedFolder() {
        RebuildApprovedSpriteRegistryFromApprovedFolder();
    }

    [MenuItem("Tools/P3 Art/Rebuild Approved Sprite Registry")]
    public static void RebuildApprovedSpriteRegistryFromApprovedFolder() {
        EnsureFolder("Assets/Resources");

        VisualAssetRegistry registry = AssetDatabase.LoadAssetAtPath<VisualAssetRegistry>(RegistryPath);
        if (registry == null) {
            registry = ScriptableObject.CreateInstance<VisualAssetRegistry>();
            AssetDatabase.CreateAsset(registry, RegistryPath);
            Debug.Log($"[VisualAssetRegistryEditorTools] Created registry at {RegistryPath}.");
        }

        EnsureApprovedSprites();

        Dictionary<string, VisualAssetEntry> existingEntries = new Dictionary<string, VisualAssetEntry>();
        foreach (var entry in registry.Entries) {
            if (entry != null && !string.IsNullOrEmpty(entry.VisualID) && !existingEntries.ContainsKey(entry.VisualID)) {
                existingEntries.Add(entry.VisualID, entry);
            }
        }

        string[] iconGuids = AssetDatabase.FindAssets("t:Sprite", new[] { ApprovedArtFolder });
        int addedOrUpdated = 0;
        foreach (string guid in iconGuids.OrderBy(guid => AssetDatabase.GUIDToAssetPath(guid), StringComparer.Ordinal)) {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            Sprite sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (sprite == null) {
                continue;
            }

            string visualID = Path.GetFileNameWithoutExtension(assetPath);
            if (!existingEntries.TryGetValue(visualID, out VisualAssetEntry entry)) {
                entry = new VisualAssetEntry { VisualID = visualID };
                registry.Entries.Add(entry);
                existingEntries.Add(visualID, entry);
            }

            entry.Sprite = sprite;
            if (visualID == MissingSpriteVisualID) {
                registry.MissingSprite = sprite;
            }
            addedOrUpdated++;
        }

        // Motion clips for CGs ("<cg visualID>_motion", see UIMotionCG), by file name like the sprites.
        foreach (string guid in AssetDatabase.FindAssets("t:VideoClip", new[] { ApprovedArtFolder })) {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            UnityEngine.Video.VideoClip clip = AssetDatabase.LoadAssetAtPath<UnityEngine.Video.VideoClip>(assetPath);
            if (clip == null) {
                continue;
            }

            string visualID = Path.GetFileNameWithoutExtension(assetPath);
            if (!existingEntries.TryGetValue(visualID, out VisualAssetEntry entry)) {
                entry = new VisualAssetEntry { VisualID = visualID };
                registry.Entries.Add(entry);
                existingEntries.Add(visualID, entry);
            }

            entry.VideoClip = clip;
            addedOrUpdated++;
        }

        // Entries whose files were deleted from Approved point at nothing; drop them.
        int removed = registry.Entries.RemoveAll(entry => entry == null
            || (entry.Sprite == null && entry.Prefab == null && entry.AudioClip == null && entry.Material == null && entry.VideoClip == null));
        if (removed > 0) {
            Debug.Log($"[VisualAssetRegistryEditorTools] Removed {removed} entries whose assets are gone.");
        }

        registry.Entries.Sort((left, right) => string.Compare(left?.VisualID, right?.VisualID, StringComparison.Ordinal));
        registry.RebuildLookup();
        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[VisualAssetRegistryEditorTools] Rebuilt approved sprite registry. EntriesUpdated={addedOrUpdated}, TotalEntries={registry.Entries.Count}, MissingSprite={(registry.MissingSprite != null ? registry.MissingSprite.name : "None")}.");
    }

    [MenuItem("Tools/P3 Art/Validate Approved Display Specs")]
    public static void ValidateApprovedDisplaySpecs() {
        if (!AssetDatabase.IsValidFolder(ApprovedArtFolder)) {
            Debug.LogWarning($"[VisualAssetRegistryEditorTools] Approved art folder not found: {ApprovedArtFolder}");
            return;
        }

        VisualAssetRegistry registry = AssetDatabase.LoadAssetAtPath<VisualAssetRegistry>(RegistryPath);
        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { ApprovedArtFolder });
        int checkedCount = 0;
        int warningCount = 0;

        foreach (string guid in textureGuids.OrderBy(guid => AssetDatabase.GUIDToAssetPath(guid), StringComparer.Ordinal)) {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            ApprovedSpriteSpec spec = ResolveApprovedSpriteSpec(assetPath);
            if (spec == null) {
                warningCount += LogValidationWarning(assetPath, "is under Approved but does not match a known art category.");
                continue;
            }

            checkedCount++;
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            Texture2D texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
            string visualID = Path.GetFileNameWithoutExtension(assetPath);

            if (texture != null && (texture.width != spec.SourceWidth || texture.height != spec.SourceHeight)) {
                warningCount += LogValidationWarning(assetPath, $"source size is {texture.width}x{texture.height}, expected {spec.SourceWidth}x{spec.SourceHeight} for {spec.Category}.");
            }

            if (importer == null) {
                warningCount += LogValidationWarning(assetPath, "has no TextureImporter.");
                continue;
            }

            if (importer.textureType != TextureImporterType.Sprite) {
                warningCount += LogValidationWarning(assetPath, "Texture Type should be Sprite (2D and UI).");
            }

            if (importer.spriteImportMode != SpriteImportMode.Single) {
                warningCount += LogValidationWarning(assetPath, "Sprite Mode should be Single.");
            }

            if (importer.alphaIsTransparency != spec.AlphaIsTransparency) {
                warningCount += LogValidationWarning(assetPath, $"Alpha Is Transparency should be {spec.AlphaIsTransparency}.");
            }

            if (importer.maxTextureSize != spec.MaxTextureSize) {
                warningCount += LogValidationWarning(assetPath, $"Max Size should be {spec.MaxTextureSize}.");
            }

            if (importer.filterMode != FilterMode.Bilinear) {
                warningCount += LogValidationWarning(assetPath, "Filter Mode should be Bilinear.");
            }

            if (registry == null || !registry.TryGetEntry(visualID, out VisualAssetEntry entry) || entry == null || entry.Sprite == null) {
                warningCount += LogValidationWarning(assetPath, $"VisualID [{visualID}] is missing from {RegistryPath}. Run Tools/P3 Art/Rebuild Approved Sprite Registry.");
            }
        }

        if (registry == null) {
            warningCount++;
            Debug.LogWarning($"[VisualAssetRegistryEditorTools] Registry not found: {RegistryPath}");
        } else if (registry.MissingSprite == null) {
            warningCount++;
            Debug.LogWarning("[VisualAssetRegistryEditorTools] Registry MissingSprite is not assigned.");
        }

        Debug.Log($"[VisualAssetRegistryEditorTools] Approved display spec validation finished. Checked={checkedCount}, Warnings={warningCount}.");
    }

    private static void EnsureApprovedSprites() {
        if (!AssetDatabase.IsValidFolder(ApprovedArtFolder)) {
            Debug.LogWarning($"[VisualAssetRegistryEditorTools] Approved art folder not found: {ApprovedArtFolder}");
            return;
        }

        string[] textureGuids = AssetDatabase.FindAssets("t:Texture2D", new[] { ApprovedArtFolder });
        foreach (string guid in textureGuids) {
            string assetPath = AssetDatabase.GUIDToAssetPath(guid);
            TextureImporter importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) {
                continue;
            }

            bool changed = false;
            if (importer.textureType != TextureImporterType.Sprite) {
                importer.textureType = TextureImporterType.Sprite;
                changed = true;
            }

            if (importer.spriteImportMode != SpriteImportMode.Single) {
                importer.spriteImportMode = SpriteImportMode.Single;
                changed = true;
            }

            if (Math.Abs(importer.spritePixelsPerUnit - 100f) > 0.01f) {
                importer.spritePixelsPerUnit = 100f;
                changed = true;
            }

            ApprovedSpriteSpec spec = ResolveApprovedSpriteSpec(assetPath);
            bool alphaIsTransparency = spec?.AlphaIsTransparency ?? true;
            if (importer.alphaIsTransparency != alphaIsTransparency) {
                importer.alphaIsTransparency = alphaIsTransparency;
                changed = true;
            }

            int maxTextureSize = spec?.MaxTextureSize ?? 1024;
            if (importer.maxTextureSize != maxTextureSize) {
                importer.maxTextureSize = maxTextureSize;
                changed = true;
            }

            if (importer.filterMode != FilterMode.Bilinear) {
                importer.filterMode = FilterMode.Bilinear;
                changed = true;
            }

            bool mipmaps = spec?.Mipmaps ?? false;
            if (importer.mipmapEnabled != mipmaps) {
                importer.mipmapEnabled = mipmaps;
                changed = true;
            }

            if (mipmaps && importer.mipmapFilter != TextureImporterMipFilter.KaiserFilter) {
                importer.mipmapFilter = TextureImporterMipFilter.KaiserFilter;
                changed = true;
            }

            if (spec != null && spec.FullRect) {
                TextureImporterSettings settings = new TextureImporterSettings();
                importer.ReadTextureSettings(settings);
                if (settings.spriteMeshType != SpriteMeshType.FullRect) {
                    settings.spriteMeshType = SpriteMeshType.FullRect;
                    importer.SetTextureSettings(settings);
                    changed = true;
                }
            }

            if (changed) {
                importer.SaveAndReimport();
            }
        }
    }

    private static void EnsureFolder(string folderPath) {
        if (AssetDatabase.IsValidFolder(folderPath)) {
            return;
        }

        string[] parts = folderPath.Split('/');
        string current = parts[0];
        for (int i = 1; i < parts.Length; i++) {
            string next = $"{current}/{parts[i]}";
            if (!AssetDatabase.IsValidFolder(next)) {
                AssetDatabase.CreateFolder(current, parts[i]);
            }
            current = next;
        }
    }

    private static int LogValidationWarning(string assetPath, string message) {
        Debug.LogWarning($"[VisualAssetRegistryEditorTools] {assetPath}: {message}");
        return 1;
    }

    private static ApprovedSpriteSpec ResolveApprovedSpriteSpec(string assetPath) {
        string normalized = assetPath.Replace('\\', '/');

        if (normalized.Contains("/Backgrounds/")) {
            return new ApprovedSpriteSpec("background", 1920, 1080, false, 2048);
        }

        if (normalized.Contains("/Chassis/")) {
            return new ApprovedSpriteSpec("chassis", 1024, 1024, true, 1024);
        }

        // The rigs are weights read pixel by pixel, not drawn.
        if (normalized.Contains("/Dolls/Rigs/")) {
            return new ApprovedSpriteSpec("doll rig", 512, 768, true, 2048);
        }

        if (normalized.Contains("/Dolls/Bust/")) {
            return new ApprovedSpriteSpec("doll bust", 1024, 1280, true, 2048);
        }

        // Her room portrait (with the arm drawn over it and its hair and body layers) has thin crisp lines and is shown
        // at a third to a half of its size: without mipmaps the lines break into dots. Only these: a 1024x1536 texture
        // with mipmaps cannot be block-compressed and takes 8 MB instead of 1.5 MB. Full rect: a tight sprite is drawn
        // as its opaque box only, and the sway and puppet (whose weights, neck and cheeks are in the whole canvas)
        // would read the box's corners as the canvas's; the layers drawn over it would not line up either.
        if (normalized.Contains("/Dolls/") && (normalized.EndsWith("_stand.png") || normalized.EndsWith("_stand_hold.png")
            || normalized.EndsWith("_stand_hair.png") || normalized.EndsWith("_stand_body.png"))) {
            return new ApprovedSpriteSpec("doll", 1024, 1536, true, 2048, true, true);
        }

        if (normalized.Contains("/Dolls/")) {
            return new ApprovedSpriteSpec("doll", 1024, 1536, true, 2048);
        }

        if (normalized.Contains("/Items/Icons/")) {
            return new ApprovedSpriteSpec("item icon", 512, 512, true, 512);
        }

        if (normalized.Contains("/Monsters/Portraits/")) {
            return new ApprovedSpriteSpec("monster portrait", 1024, 1024, true, 1024);
        }

        if (normalized.Contains("/Monsters/Combat/")) {
            return new ApprovedSpriteSpec("monster combat entity", 1024, 1024, true, 1024);
        }

        if (normalized.Contains("/Nodes/Icons/")) {
            return new ApprovedSpriteSpec("node icon", 512, 512, true, 512);
        }

        if (normalized.Contains("/Prosthetics/Icons/")) {
            return new ApprovedSpriteSpec("prosthetic icon", 512, 512, true, 512);
        }

        if (normalized.Contains("/UI/")) {
            string visualID = Path.GetFileNameWithoutExtension(normalized);
            return ResolveUiSpriteSpec(visualID);
        }

        return null;
    }

    private static ApprovedSpriteSpec ResolveUiSpriteSpec(string visualID) {
        switch (visualID) {
            case VisualAssetService.UIButtonPrimaryID:
            case VisualAssetService.UIButtonSecondaryID:
            case VisualAssetService.UIButtonDangerID:
                return new ApprovedSpriteSpec("ui button", 512, 160, true, 512);
            case VisualAssetService.UICombatTraySatchelID:
                return new ApprovedSpriteSpec("ui tray frame", 1024, 838, true, 1024);
            case VisualAssetService.UICombatEndTurnSealID:
                return new ApprovedSpriteSpec("ui icon", 512, 512, true, 512);
            case VisualAssetService.UICombatIntentTagID:
            case VisualAssetService.UICombatIntentTagDangerID:
                return new ApprovedSpriteSpec("ui tag", 512, 160, true, 512);
            case VisualAssetService.UICombatHpBarFrameID:
                return new ApprovedSpriteSpec("ui status bar", 512, 96, true, 512);
            case VisualAssetService.UICombatApLanternLitID:
            case VisualAssetService.UICombatApLanternUnlitID:
            case VisualAssetService.UICombatSanCandleID:
            case VisualAssetService.UICombatShieldBadgeID:
            case VisualAssetService.UICombatApPipID:
            case VisualAssetService.UIInventorySlotAvailableID:
            case VisualAssetService.UIInventorySlotLockedID:
            case VisualAssetService.UIInventorySlotHoverID:
            case VisualAssetService.UIInventorySlotValidID:
            case VisualAssetService.UIInventorySlotInvalidID:
                return new ApprovedSpriteSpec("ui small icon", 256, 256, true, 512);
            case VisualAssetService.UICombatEnemyCardID:
            case VisualAssetService.UICombatEnemyCardSelectedID:
                return new ApprovedSpriteSpec("ui combat card", 768, 768, true, 1024);
            case VisualAssetService.UICombatStatusBarHpID:
            case VisualAssetService.UICombatStatusBarShieldID:
                return new ApprovedSpriteSpec("ui status bar", 512, 96, true, 512);
            case VisualAssetService.UICombatTurnBannerID:
                return new ApprovedSpriteSpec("ui banner", 1024, 256, true, 1024);
            case VisualAssetService.UICombatEntityShadowID:
            case VisualAssetService.UICombatTargetRingID:
                return new ApprovedSpriteSpec("ui combat floor marker", 512, 256, true, 512);
            case VisualAssetService.UILootPickupPanelID:
            case VisualAssetService.UIPanelMainID:
            case VisualAssetService.UISettlementVictoryPanelID:
            case VisualAssetService.UISettlementDefeatPanelID:
                return new ApprovedSpriteSpec("ui large panel", 1024, 768, true, 1024);
            case VisualAssetService.UILootDropZoneID:
            case VisualAssetService.UIPanelInfoID:
                return new ApprovedSpriteSpec("ui panel", visualID == VisualAssetService.UILootDropZoneID ? 768 : 768, visualID == VisualAssetService.UILootDropZoneID ? 512 : 384, true, 1024);
            case VisualAssetService.UIInventoryChassisPanelID:
                return new ApprovedSpriteSpec("ui chassis panel", 1024, 1024, true, 1024);
            case VisualAssetService.UIListRowNormalID:
            case VisualAssetService.UIListRowSelectedID:
            case VisualAssetService.UITitleDividerID:
                return new ApprovedSpriteSpec("ui strip", 1024, 128, true, 1024);
            case VisualAssetService.UIDungeonRouteLineID:
                return new ApprovedSpriteSpec("ui route line", 512, 128, true, 512);
            case VisualAssetService.UIDungeonNodePlateID:
            case VisualAssetService.UIIconLockedID:
            case VisualAssetService.UIIconEquippedID:
            case VisualAssetService.UIIconMoneyID:
            case VisualAssetService.MissingSpriteVisualID:
                return new ApprovedSpriteSpec("ui icon", 512, 512, true, 512);
            default:
                return new ApprovedSpriteSpec("ui", 512, 512, true, 512);
        }
    }

    private sealed class ApprovedSpriteSpec {
        public readonly string Category;
        public readonly int SourceWidth;
        public readonly int SourceHeight;
        public readonly bool AlphaIsTransparency;
        public readonly int MaxTextureSize;
        public readonly bool Mipmaps;
        public readonly bool FullRect;

        public ApprovedSpriteSpec(string category, int sourceWidth, int sourceHeight, bool alphaIsTransparency, int maxTextureSize, bool mipmaps = false, bool fullRect = false) {
            Category = category;
            SourceWidth = sourceWidth;
            SourceHeight = sourceHeight;
            AlphaIsTransparency = alphaIsTransparency;
            MaxTextureSize = maxTextureSize;
            Mipmaps = mipmaps;
            FullRect = fullRect;
        }
    }
}
#endif
