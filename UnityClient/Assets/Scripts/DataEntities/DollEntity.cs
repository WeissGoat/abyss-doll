using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class DollEntity {
    public string DollID;
    public string Name;
    
    public DollStatusComponent Status = new DollStatusComponent();
    public DollStatsComponent Stats = new DollStatsComponent();
    public ChassisComponent Chassis;
    public DollBondComponent Bond = new DollBondComponent();
    
    public List<string> Traits = new List<string>();
    public List<string> EquippedProsthetics = new List<string>();
    
    [NonSerialized]
    public object RuntimeGrid; // Use object or interface here to avoid coupling if BackpackGrid is elsewhere
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
}

[Serializable]
public class DollBondComponent {
    public int AffectionLevel;
    public int HiddenTrust;
}
