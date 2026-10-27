using System;
using System.Collections.Generic;

[Serializable]
public class DungeonConfig {
    public int LayerID;
    public string Name;
    public int SANCostPerNode;
    // Corruption (侵蚀) Zero gains for every node walked on this layer (DollStrainService).
    public float CorruptionPerNode;
    public int ExpectedNodeCount;
    public string MapProfileID;
    public int MapSeed;
    public int RowCount;
    public int MinWidth;
    public int MaxWidth;
    public int MinRouteCount;
    public string MapBackgroundID;
    // The fight screen on this layer: its stage background, and the light on the fighters standing in it (ambient
    // multiplies their colours, rim is the cool edge light from behind). Colours as "#RRGGBB"; empty uses the defaults.
    public string CombatBackgroundID;
    public string CombatAmbientColor;
    public string CombatRimColor;
    public string FogProfile;
    public int NodeRevealDepth = 1;
    public int NodePreviewDepth = 1;
    
    public List<NodePoolEntry> NodePool = new List<NodePoolEntry>();
    public string BossNode;
    public string BossNodeIconID;
    public NodePoolEntry EndNode;
}

[Serializable]
public class NodePoolEntry {
    public string NodeType; // "CombatNode", "SafeRoomNode", "StairsNode", "TreasureNode", etc.
    public string NodeIconID;
    public string Title;
    public string Description;
    public string RiskLevel;
    public string RiskHint;
    public List<string> MonsterIDs = new List<string>();
    public string RewardID;
    public List<DungeonNodeOutcomeConfig> OutcomeEffects = new List<DungeonNodeOutcomeConfig>();
    public int Weight;
}

[Serializable]
public class DungeonNodeOutcomeConfig {
    public string Type;
    public string Resource;
    public int Amount;
}

public enum DungeonNodeOutcomeType {
    ModifyResource = 0
}
