using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DollEntity {
    public string DollID;
    public string Name;
    public string DefaultChassisID;
    
    public DollStatusComponent Status = new DollStatusComponent();
    public DollStatsComponent Stats = new DollStatsComponent();
    public ChassisComponent Chassis;
    public DollBondComponent Bond = new DollBondComponent();
    
    public List<string> Traits = new List<string>();
    public List<string> EquippedProsthetics = new List<string>();
    
    [NonSerialized]
    public object RuntimeGrid; // Use object or interface here to avoid coupling if BackpackGrid is elsewhere
    
    // --- 运行时生命周期管理 ---
    public void InitializeRuntime() {
        // 订阅系统事件，主动管理自身状态
        DungeonEventBus.OnNodeEntered += HandleNodeEntered;
    }
    
    public void CleanupRuntime() {
        DungeonEventBus.OnNodeEntered -= HandleNodeEntered;
    }
    
    private void HandleNodeEntered(NodeBase node, int sanCost) {
        Status.SAN_Current -= sanCost;
        if (Status.SAN_Current < 0) Status.SAN_Current = 0;
        
        Debug.Log($"[DollEntity:{Name}] Event received! Self-deducted {sanCost} SAN for moving to node {node.NodeID}. Current SAN: {Status.SAN_Current}");
    }
}

[Serializable]
public class DollStatusComponent {
    public int HP_Current;
    public int HP_Max;
    public int SAN_Current;
    public int SAN_Max;
    public float WearAndTear;
    public float Corruption;
}

[Serializable]
public class DollStatsComponent {
    public int MaxAP;
    public int APRegen;
    public int Power;
    public int Compute;
    public int Charm;
}

[Serializable]
public class ChassisComponent {
    public string ChassisID;
    public int Level;
    public int GridWidth;
    public int GridHeight;
    public bool[][] GridMask;
    public List<Vector2Int> LockedCells = new List<Vector2Int>();
    public ChassisUpgradeCost UpgradeCost;
}

[Serializable]
public class ChassisUpgradeCost : CraftingCost {
    public string NextChassisID;
}

[Serializable]
public class DollBondComponent {
    public int AffectionLevel;
    public int HiddenTrust;
}
