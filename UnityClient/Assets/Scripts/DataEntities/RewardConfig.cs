using System;
using System.Collections.Generic;

[Serializable]
public class RewardConfig {
    public string RewardID;
    public string Name;
    public List<string> Tags = new List<string>();
    public List<RewardEntry> Guaranteed = new List<RewardEntry>();
    public List<RewardPool> WeightedPools = new List<RewardPool>();
}

[Serializable]
public class RewardPool {
    public string PoolID;
    public int RollCount = 1;
    public bool AllowDuplicate = true;
    public List<RewardEntry> Entries = new List<RewardEntry>();
}

[Serializable]
public class RewardEntry {
    public string Type;
    public string ItemID;
    public string RewardID;
    public int Money;
    public int Weight;
    public int Count = 1;
    public int MinCount;
    public int MaxCount;
    public string Condition;
}

