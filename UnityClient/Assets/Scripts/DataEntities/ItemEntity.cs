using System;
using System.Collections.Generic;
using UnityEngine;

[Serializable]
public class ItemEntity {
    public string InstanceID;
    public string ConfigID;
    public string Name;
    public string ItemType; // Represented as string for simple JSON mapping
    public string Rarity;
    public int BaseValue;
    
    public ItemGridComponent Grid;
    public ItemCombatComponent Combat;
    public List<string> Tags = new List<string>();
}

[Serializable]
public class ItemGridComponent {
    public int[][] Shape;
    public int Rotation;
    public int GridCost;
    public int[] CurrentPos = new int[2] { 0, 0 };
}

[Serializable]
public class EffectData {
    public string EffectID;
    public int Level;
    public string Target; // e.g., "Right", "Self"
    public float[] Params;
}

[Serializable]
public class ItemCombatComponent {
    public string TriggerType; // "Manual", "Passive"
    public int APCost;
    public string DamageType;
    public int BaseValue;
    public List<EffectData> Effects = new List<EffectData>();
    
    [NonSerialized]
    public float RuntimeDamage;
}
