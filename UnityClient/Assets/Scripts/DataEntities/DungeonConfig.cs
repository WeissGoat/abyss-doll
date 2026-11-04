using System;
using System.Collections.Generic;

[Serializable]
public class DungeonConfig {
    public int LayerID;
    public string Name;
    public int SANCostPerNode;
    // Extra SAN per step on this layer when she is hurt, overloaded or corroded (GDD_02 §3.5).
    public DungeonSanTaxRules SANTax = new DungeonSanTaxRules();
    // Corruption (侵蚀) Zero gains for every node walked on this layer (DollStrainService).
    public float CorruptionPerNode;
    public int ExpectedNodeCount;
    public string MapProfileID;
    public int MapSeed;
    // Rows of route nodes when the layer has no Segments.
    public int RowCount;
    // Rows of route nodes in each segment (GDD_13: the layer is walked in 2-3 segments); a lift (SegmentExit) stands
    // between two segments, where she can go home with what she carries.
    public List<int> Segments = new List<int>();
    public NodePoolEntry SegmentExit;
    public DungeonSafeZoneRules SafeZoneRules = new DungeonSafeZoneRules();
    public int MinWidth;
    public int MaxWidth;
    public int MinRouteCount;
    public string MapBackgroundID;
    // The fight screen's stage background on this layer.
    public string CombatBackgroundID;
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
    // A lift or safe room: the share of max HP / SAN it gives back on the way on (0: none).
    public float RecoverHPRatio;
    public float RecoverSANRatio;
    public List<DungeonNodeOutcomeConfig> OutcomeEffects = new List<DungeonNodeOutcomeConfig>();
    public int Weight;
}

// The safe zone after the layer's boss (GDD_13: it no longer heals fully, so wear carries from one layer into the next).
[Serializable]
public class DungeonSanTaxRules {
    // HP below this share of max adds LowHPExtra a step; 0 extra turns a rule off.
    public float LowHPRatio = 0.3f;
    public int LowHPExtra;
    // The backpack's open cells filled to this share (junk counts) adds OverloadExtra a step.
    public float OverloadRatio = 0.8f;
    public int OverloadExtra;
    // Her corruption (0-100) at or above this adds CorruptionExtra a step.
    public float CorruptionThreshold = 50f;
    public int CorruptionExtra;
}

[Serializable]
public class DungeonSafeZoneRules {
    public float RecoverHPRatio = 1f;
    public float RecoverSANRatio = 1f;
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
