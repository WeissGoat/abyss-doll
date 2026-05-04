using System.Collections.Generic;

public class DungeonSettlementResult {
    public bool IsVictory;
    public int LootTransferredCount;
    public int LootEstimatedValue;
    public int StashCountAfterSettlement;
    public List<string> LootNames = new List<string>();
    public int PickedUpCount;
    public int PickedUpEstimatedValue;
    public List<string> PickedUpNames = new List<string>();
    public int BroughtOutCount;
    public int BroughtOutEstimatedValue;
    public List<string> BroughtOutNames = new List<string>();
    public int LostCount;
    public int LostEstimatedValue;
    public List<string> LostNames = new List<string>();
}

public class CombatLootPickupResult {
    public string NodeID;
    public List<string> SourceMonsterIDs = new List<string>();
    public List<ItemEntity> OfferedItems = new List<ItemEntity>();
    public int TotalEstimatedValue;
}

public class CombatLootCollectionResult {
    public string NodeID;
    public List<ItemEntity> AcceptedItems = new List<ItemEntity>();
    public List<ItemEntity> DiscardedItems = new List<ItemEntity>();
}
