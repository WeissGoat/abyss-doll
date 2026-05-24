using System;
using System.Collections.Generic;

public enum EconomySellChannel {
    DumpBox = 0,
    Showcase = 1,
    BlackMarket = 2
}

public enum EconomyRumorType {
    DemandUp = 0,
    DemandCrash = 1,
    ContrabandWindow = 2,
    FactionRequest = 3,
    RiskPremium = 4
}

public enum EconomyOrderType {
    Procurement = 0,
    TargetItem = 1,
    ExplorationReport = 2,
    LiveCapture = 3,
    BlackMarketBetrayal = 4
}

public enum EconomyOrderStatus {
    Available = 0,
    Accepted = 1,
    InProgress = 2,
    Completed = 3,
    Expired = 4,
    Abandoned = 5,
    Betrayed = 6
}

[Serializable]
public class FactionConfig {
    public string FactionID;
    public string DisplayName;
    public bool IsBlackMarket;
    public int VisibleOrderSlots = 2;
    public int MaxActiveOrders = 3;
    public List<string> FactionTags = new List<string>();
    public List<ReputationRankConfig> ReputationRanks = new List<ReputationRankConfig>();
}

[Serializable]
public class ReputationRankConfig {
    public int Rank;
    public int Threshold;
    public List<string> UnlockRefs = new List<string>();
}

[Serializable]
public class OrderConfig {
    public string OrderID;
    public string FactionID;
    public string OrderType;
    public string DisplayName;
    public string Description;
    public OrderRequirementConfig Requirement = new OrderRequirementConfig();
    public int DeadlineDays = 7;
    public string RewardID;
    public int FixedGold;
    public int ReputationDelta;
    public int TrustDelta;
    public OrderFailurePenaltyConfig FailurePenalty = new OrderFailurePenaltyConfig();
    public string RepeatPolicy = "Repeatable";
    public int Weight = 1;
    public List<string> Tags = new List<string>();
}

[Serializable]
public class OrderRequirementConfig {
    public List<string> RequiredItemIDs = new List<string>();
    public List<string> RequiredTags = new List<string>();
    public List<string> ForbiddenTags = new List<string>();
    public int RequiredCount = 1;
    public int MinGridCost;
    public int MinLayer;
}

[Serializable]
public class OrderFailurePenaltyConfig {
    public int ExpiredReputationDelta;
    public int AbandonedReputationDelta;
    public int BetrayedReputationDelta;
    public int CooldownDays;
}

[Serializable]
public class RumorConfig {
    public string RumorID;
    public string DisplayName;
    public string RumorType;
    public string Description;
    public List<string> TargetTags = new List<string>();
    public int TargetLayer;
    public string Channel = "DumpBox";
    public float PriceMultiplier = 1f;
    public int DurationDays = 7;
    public int Weight = 1;
    public List<string> BoostedOrderTags = new List<string>();
}

[Serializable]
public class FactionRuntimeState {
    public string FactionID;
    public int ReputationValue;
    public int TrustValue;
    public int FailedOrderCount;
    public int CooldownUntilDay;
    public List<string> CompletedOrderIDs = new List<string>();
    public List<string> KnownFlags = new List<string>();
}

[Serializable]
public class OrderInstanceState {
    public string InstanceID;
    public string OrderID;
    public string FactionID;
    public string Status = EconomyOrderStatus.Available.ToString();
    public int GeneratedDay;
    public int AcceptedDay;
    public int DeadlineDay;
    public int LogSeed;
    public List<string> DeliveredItemInstanceIDs = new List<string>();
    public string FailureReason;
}

[Serializable]
public class ActiveRumorState {
    public string RumorID;
    public int StartDay;
    public int ExpireDay;
    public int LogSeed;
}
