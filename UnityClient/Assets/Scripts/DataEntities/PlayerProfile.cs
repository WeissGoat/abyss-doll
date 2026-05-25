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
    public int LastEconomyRefreshWeek;
    public int WorkshopLevel;
    public int MemoryFragments;
    public int HighestUnlockedDungeonLayer = 1;
    public int LastSelectedDungeonStartLayer = 1;
    public DollInteractionRuntimeState DollInteractionState = new DollInteractionRuntimeState();
    
    public string ActiveDollID;
    public DollEntity ActiveDoll;
    
    public List<string> UnlockedDolls = new List<string>();
    public List<ItemEntity> StashInventory = new List<ItemEntity>();
    public List<FactionRuntimeState> FactionStates = new List<FactionRuntimeState>();
    public List<OrderInstanceState> ActiveOrders = new List<OrderInstanceState>();
    public List<ActiveRumorState> ActiveRumors = new List<ActiveRumorState>();
}

[Serializable]
public class DollInteractionRuntimeState {
    public List<DollDailyInteractionState> DailyStates = new List<DollDailyInteractionState>();

    public DollDailyInteractionState GetOrCreateDailyState(int day) {
        int safeDay = Math.Max(1, day);
        if (DailyStates == null) {
            DailyStates = new List<DollDailyInteractionState>();
        }

        foreach (DollDailyInteractionState state in DailyStates) {
            if (state != null && state.Day == safeDay) {
                state.Normalize();
                return state;
            }
        }

        DollDailyInteractionState newState = new DollDailyInteractionState {
            Day = safeDay
        };
        DailyStates.Add(newState);
        return newState;
    }
}

[Serializable]
public class DollDailyInteractionState {
    public int Day;
    public int TotalTouchCount;
    public int EffectiveTouchCount;
    public string LastTouchRegion;
    public int RepeatedTouchRegionCount;
    public int TotalTalkCount;
    public int EffectiveTalkCount;
    public int TotalGiftCount;
    public int AcceptedGiftCount;
    public int RejectedGiftCount;
    public List<string> LogLines = new List<string>();

    public void Normalize() {
        Day = Math.Max(1, Day);
        TotalTouchCount = Math.Max(0, TotalTouchCount);
        EffectiveTouchCount = Math.Max(0, EffectiveTouchCount);
        RepeatedTouchRegionCount = Math.Max(0, RepeatedTouchRegionCount);
        TotalTalkCount = Math.Max(0, TotalTalkCount);
        EffectiveTalkCount = Math.Max(0, EffectiveTalkCount);
        TotalGiftCount = Math.Max(0, TotalGiftCount);
        AcceptedGiftCount = Math.Max(0, AcceptedGiftCount);
        RejectedGiftCount = Math.Max(0, RejectedGiftCount);
        if (LogLines == null) {
            LogLines = new List<string>();
        }
    }
}
