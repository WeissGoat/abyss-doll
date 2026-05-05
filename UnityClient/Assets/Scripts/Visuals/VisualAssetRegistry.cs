using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class VisualAssetEntry {
    public string VisualID;
    public Sprite Sprite;
    public GameObject Prefab;
    public AudioClip AudioClip;
    public Material Material;
}

[CreateAssetMenu(menuName = "P3/Visual Asset Registry", fileName = "VisualAssetRegistry")]
public class VisualAssetRegistry : ScriptableObject {
    public List<VisualAssetEntry> Entries = new List<VisualAssetEntry>();
    public Sprite MissingSprite;
    public GameObject MissingPrefab;

    private Dictionary<string, VisualAssetEntry> _entryMap;

    public bool TryGetEntry(string visualID, out VisualAssetEntry entry) {
        entry = null;
        if (string.IsNullOrEmpty(visualID)) {
            return false;
        }

        EnsureLookup();
        return _entryMap.TryGetValue(visualID, out entry);
    }

    public void RebuildLookup() {
        _entryMap = new Dictionary<string, VisualAssetEntry>();
        foreach (var entry in Entries) {
            if (entry == null || string.IsNullOrEmpty(entry.VisualID)) {
                continue;
            }

            if (_entryMap.ContainsKey(entry.VisualID)) {
                Debug.LogWarning($"[VisualAssetRegistry] Duplicate VisualID ignored: {entry.VisualID}");
                continue;
            }

            _entryMap.Add(entry.VisualID, entry);
        }
    }

    private void EnsureLookup() {
        if (_entryMap == null) {
            RebuildLookup();
        }
    }
}
