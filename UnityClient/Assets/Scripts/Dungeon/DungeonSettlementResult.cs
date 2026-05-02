using System.Collections.Generic;

public class DungeonSettlementResult {
    public bool IsVictory;
    public int LootTransferredCount;
    public int LootEstimatedValue;
    public int StashCountAfterSettlement;
    public List<string> LootNames = new List<string>();
}

public class CombatLootPickupResult {
    public string NodeID;
    public List<string> SourceMonsterIDs = new List<string>();
    public List<ItemEntity> OfferedItems = new List<ItemEntity>();
    public int TotalEstimatedValue;
}
