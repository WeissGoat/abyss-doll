using System;
using System.Collections.Generic;

[Serializable]
public class DungeonConfig {
    public int LayerID;
    public string Name;
    public int SANCostPerNode;
    public int ExpectedNodeCount;
    public string MapProfileID;
    public int MapSeed;
    public int RowCount;
    public int MinWidth;
    public int MaxWidth;
    public int MinRouteCount;
    public string MapBackgroundID;
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
