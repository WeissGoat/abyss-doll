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
    
    public List<NodePoolEntry> NodePool = new List<NodePoolEntry>();
    public string BossNode;
    public string BossNodeIconID;
    public NodePoolEntry EndNode;
}

[Serializable]
public class NodePoolEntry {
    public string NodeType; // "CombatNode", "SafeRoomNode", "StairsNode"
    public string NodeIconID;
    public List<string> MonsterIDs = new List<string>();
    public string RewardID;
    public int Weight;
}
