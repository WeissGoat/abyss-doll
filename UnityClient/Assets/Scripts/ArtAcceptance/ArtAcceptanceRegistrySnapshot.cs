using System;
using System.Collections.Generic;

/// <summary>
/// VisualAssetRegistry 运行时快照数据结构。
/// 由 ArtAcceptanceRunner 在验收流程中序列化为 registry_snapshot.json。
/// 记录 registry 是否存在、MissingSprite 状态、P0 VisualID 覆盖率及每条 entry 的资源详情。
/// </summary>
[Serializable]
public class ArtAcceptanceRegistrySnapshotReport {
    public string SchemaVersion;
    public string RunID;
    public bool RegistryFound;
    public bool MissingSpriteFound;
    public string MissingSpriteVisualID;
    public string MissingSpriteName;
    public int EntryCount;
    public List<string> MissingRequiredVisualIDs = new List<string>();
    public List<ArtAcceptanceRegistryEntrySnapshot> Entries = new List<ArtAcceptanceRegistryEntrySnapshot>();
}

[Serializable]
public class ArtAcceptanceRegistryEntrySnapshot {
    public string VisualID;
    public bool HasSprite;
    public bool HasPrefab;
    public bool HasAudioClip;
    public bool HasMaterial;
    public string SpriteName;
    public string TextureSize;
    public string SpriteRect;
    public string SpriteBorder;
}
