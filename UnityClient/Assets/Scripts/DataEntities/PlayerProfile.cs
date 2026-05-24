using System;
using System.Collections.Generic;

[Serializable]
public class PlayerProfile {
    public string UID;
    public int Money;
    public int DebtLevel;
    public int EconomyDebtAmount;
    public int ConsecutiveHeavyDefaultCount;
    public int CurrentDay = 1;
    public int CurrentMonth = 1;
    public int CurrentMonthDay = 1;
    public int MonthlyGrossIncome;
    public int LastDailyEconomySettlementDay;
    public bool HasPendingMonthlyRent;
    public int PendingMonthlyBillAmount;
    public int PendingMonthlyRentDay;
    public int WorkshopLevel;
    public int MemoryFragments;
    public int HighestUnlockedDungeonLayer = 1;
    public int LastSelectedDungeonStartLayer = 1;
    
    public string ActiveDollID;
    public DollEntity ActiveDoll;
    
    public List<string> UnlockedDolls = new List<string>();
    public List<ItemEntity> StashInventory = new List<ItemEntity>();
}
