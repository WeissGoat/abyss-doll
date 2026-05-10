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

        registry.Entries.Sort((left, right) => string.Compare(left?.VisualID, right?.VisualID, StringComparison.Ordinal));
        registry.RebuildLookup();
        EditorUtility.SetDirty(registry);
        AssetDatabase.SaveAssets();
        AssetDatabase.Refresh();
        Debug.Log($"[VisualAssetRegistryEditorTools] Rebuilt approved sprite registry. EntriesUpdated={addedOrUpdated}, TotalEntries={registry.Entries.Count}, MissingSprite={(registry.MissingSprite != null ? registry.MissingSprite.name : "None")}.");
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

            if (!importer.alphaIsTransparency) {
                importer.alphaIsTransparency = true;
                changed = true;
            }

            if (importer.mipmapEnabled) {
                importer.mipmapEnabled = false;
                changed = true;
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
}
#endif
