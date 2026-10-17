using System;
using System.Collections.Generic;

public enum MaintenanceTargetState {
    Wear,
    Corruption,
    HP,
    SAN
}

[Serializable]
public class MaintenanceConfig {
    public string MaintenanceID;
    public string Name;
    public string Description;
    public string QualityTier;
    public CraftingCost Cost = new CraftingCost();
    public List<MaintenanceEffectConfig> Effects = new List<MaintenanceEffectConfig>();
    public int DailyLimit;
    public bool RestoresDivePermit;
    // Bond points for a kind treatment (the flower oil); BondService adds them when it is applied.
    public int BondPoints;
}

[Serializable]
public class MaintenanceEffectConfig {
    public string TargetState;
    public float Amount;
}
