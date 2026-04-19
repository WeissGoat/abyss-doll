using System;
using System.Collections.Generic;

[Serializable]
public class PlayerProfile {
    public string UID;
    public int Money;
    public int DebtLevel;
    public int WorkshopLevel;
    public int MemoryFragments;
    
    public string ActiveDollID;
    public DollEntity ActiveDoll;
    
    public List<string> UnlockedDolls = new List<string>();
    public List<ItemEntity> StashInventory = new List<ItemEntity>();
}
