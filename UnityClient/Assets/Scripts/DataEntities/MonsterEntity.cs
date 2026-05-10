using System;
using System.Collections.Generic;
using Newtonsoft.Json.Linq;

[Serializable]
public class MonsterEntity {
    public string MonsterID;
    public string Name;
    public int Layer;
    public int HP;
    public string PortraitID;
    public string CombatVisualID;
    public string HitVFXID;
    public string DeathVFXID;
    public string RewardID;
    public MonsterAIConfig AI;

    public List<LootPoolEntry> LootPool = new List<LootPoolEntry>();
}

[Serializable]
public class MonsterAIConfig {
    public string Selector = "WeightedRandom";
    public List<MonsterActionConfig> Actions = new List<MonsterActionConfig>();
}

[Serializable]
public class MonsterActionConfig {
    public string ActionID;
    public string ActionType;
    public string Target;
    public int Weight = 100;
    public int CooldownTurns;
    public int UsesPerCombat;
    public string Condition = "Always";
    public Dictionary<string, JToken> Params = new Dictionary<string, JToken>();
}

[Serializable]
public class LootPoolEntry {
    public string ItemID;
    public int Weight;
}
