using System;
using System.Collections.Generic;

[Serializable]
public class MonsterEntity {
    public string MonsterID;
    public string Name;
    public int Layer;
    public int HP;
    public int AttacksPerTurn;
    public int DamageValue;
    
    public string GridInterference; // e.g., "None", "ReduceDamage", "AddCursedItem"
    public GridInterferenceParams GridInterferenceParams;
    
    public List<LootPoolEntry> LootPool = new List<LootPoolEntry>();
}

[Serializable]
public class GridInterferenceParams {
    public string Target;
    public float Effect;
    public int DurationTurns;
    public string ItemID;
    public List<string> OverrideTags;
    public int OverrideValue;
}

[Serializable]
public class LootPoolEntry {
    public string ItemID;
    public int Weight;
}
