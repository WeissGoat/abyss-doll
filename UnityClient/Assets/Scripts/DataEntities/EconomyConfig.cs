using System;
using System.Collections.Generic;

[Serializable]
public class EconomyConfig {
    public string EconomyConfigID;
    public string Name;
    public int WeekLength = 7;
    public int MonthLength = 28;
    public string RentCurveID;
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
}

[Serializable]
public class RentCurveStepConfig {
    public int Month;
    public int BaseRent;
}
