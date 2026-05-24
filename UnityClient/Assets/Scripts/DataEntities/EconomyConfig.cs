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
}

[Serializable]
public class RentCurveStepConfig {
    public int Month;
    public int BaseRent;
}
