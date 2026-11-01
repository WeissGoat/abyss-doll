using System;
using System.Collections.Generic;

// A dive in progress as the save keeps it (GDD_02 §5.3): taken on the map, when Zero stands between nodes, and again
// the moment she steps onto one. The map itself is not stored; it is rebuilt from the layer and the run seed, and the
// nodes she walked are marked again by ID. A node she had stepped onto but not finished (PendingNodeID) is entered
// again on resume, from the state she was in before it; its rolls are seeded from the run, so it plays out the same.
[Serializable]
public class DungeonRunSnapshot {
    public int LayerID;
    public int RunSeed;
    public bool Prologue;
    public List<string> VisitedNodeIDs = new List<string>();
    public string CurrentNodeID;
    public string PendingNodeID;

    // Loot picked up this run: the ones still in the backpack by instance ID, the ones she later left behind whole
    // (the settlement counts both).
    public List<string> AcceptedLootIDs = new List<string>();
    public List<ItemEntity> AcceptedLootGone = new List<ItemEntity>();

    // The dive's journal page and the state she set out in, for the night card.
    public DiveLog Log;
    public int LogHPBeforeStep;
    public DiveDayStart DayStart;
}

[Serializable]
public class DiveDayStart {
    public int HP = -1;
    public int SAN = -1;
    public float Wear = -1f;
    public float Corruption = -1f;
    public int LayerID;
}
