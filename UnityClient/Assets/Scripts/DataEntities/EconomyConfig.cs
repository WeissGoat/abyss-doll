using System;
using System.Collections.Generic;

[Serializable]
public class EconomyConfig {
    public string EconomyConfigID;
    public string Name;
    public int WeekLength = 7;
    public int MonthLength = 28;
    public string RentCurveID;
    // Days per rent period. 0 means one period per month (MonthLength). The Demo uses 7: rent every week (producer
    // decision 2026-10-04), charged at the end of the period's last day.
    public int RentPeriodDays;
    public List<RentCurveStepConfig> RentCurve = new List<RentCurveStepConfig>();
    public int WorkshopMaintenanceBase;
    public int LicenseFeeBase;
    public float DebtInterestRate = 0.1f;
    public float LightDebtThresholdRatio = 0.1f;
    public float PawnValueMultiplier = 0.25f;
    public List<string> PawnProtectedTags = new List<string>();
    // Workshop supply shop: what can always be bought, and the markup over the current sell price (keeps buy > sell).
    public float ShopPriceMultiplier = 1.2f;
    public List<string> ShopStock = new List<string>();
    // 停工休息 (GDD_07 §2.3 B): share of max HP / SAN restored by a rest day.
    public float RestHPRecoverRatio = 1f;
    public float RestSANRecoverRatio = 1f;
    public float RestWearReduction = 15f;
    public float RestCorruptionReduction = 15f;
    // Consecutive heavy rent defaults that end the run (GDD_07 §5.3, 经济破产).
    public int BankruptcyHeavyDefaults = 2;
}

[Serializable]
public class RentCurveStepConfig {
    // Rent period this step starts at (1 = first period). Month is the older name and still read when Period is 0.
    public int Period;
    public int Month;
    public int BaseRent;

    public int StartPeriod => Period > 0 ? Period : Month;
}
