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
    // Backpack layout at the last departure, for 「带上上次的装备」.
    public List<LoadoutSlotRecord> LastDiveLoadout = new List<LoadoutSlotRecord>();
    // The wall journal: one page per dive, newest last (DiveLogService).
    public List<DiveLog> DiveJournal = new List<DiveLog>();
    // Zero's wish (WishService): the active one, when it appeared, when the last one ended or was done.
    public string ActiveWishID;
    public int WishSinceDay;
    public int LastWishEndDay;
    public int LastWishDoneDay;
    public List<string> DoneWishes = new List<string>();
    // The attic shelf (MementoService) and the milestones already put there.
    public List<Memento> Shelf = new List<Memento>();
    public List<string> Milestones = new List<string>();
    // Home upgrades bought (HomeUpgradeService).
    public List<string> HomeUpgrades = new List<string>();
    // Night phase (GDD_07 §3.1): after the day's dive or rest, before the business opens and the calendar moves on.
    public bool IsNight;
    // Night business (GDD_04): goods moved out of the stash into the dump box or the showcase (Grid.CurrentPos and
    // Rotation hold the showcase cell). They sell when the business opens and go back to the stash when taken out.
    public List<ItemEntity> DumpBox = new List<ItemEntity>();
    public List<ItemEntity> Showcase = new List<ItemEntity>();
}

[Serializable]
public class LoadoutSlotRecord {
    public string InstanceID;
    public int X;
    public int Y;
    public int Rotation;
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
