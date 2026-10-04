using UnityEngine;

// Wear (磨损) and corruption (侵蚀) build up in the abyss and come home with Zero (GDD_02 §5.1, GDD_08 ④): wear from
// HP lost in combat and from defeats, corruption from every node walked. Maintenance and rest bring them down;
// DiveReadinessService blocks a dive when either is extreme. Rates come from the doll (Stats) and layer configs.
public static class DollStrainService {
    public const float Max = 100f;

    public static void AddWearFromHPLoss(DollEntity doll, int hpLost) {
        if (doll?.Stats == null || hpLost <= 0) {
            return;
        }

        AddWear(doll, hpLost * Mathf.Max(0f, doll.Stats.WearPerHPLost));
    }

    public static void AddCorruptionForNode(DollEntity doll, int layerID) {
        if (doll == null || !ConfigManager.Dungeons.TryGetValue(layerID, out DungeonConfig layer)) {
            return;
        }

        AddCorruption(doll, Mathf.Max(0f, layer.CorruptionPerNode));
    }

    // GDD_02 §5.2: a defeat leaves a sharp wear spike and a SAN drop on top of the lost loot.
    public static void ApplyDefeat(DollEntity doll) {
        if (doll?.Stats == null) {
            return;
        }

        AddWear(doll, Mathf.Max(0f, doll.Stats.DefeatWearGain));
        doll.Status.SAN_Current = Mathf.Max(0, doll.Status.SAN_Current - Mathf.Max(0, doll.Stats.DefeatSANLoss));
    }

    public static void ApplyRest(DollEntity doll, EconomyConfig config) {
        if (doll == null || config == null) {
            return;
        }

        doll.Status.WearAndTear = Mathf.Clamp(doll.Status.WearAndTear - Mathf.Max(0f, config.RestWearReduction), 0f, Max);
        doll.Status.Corruption = Mathf.Clamp(doll.Status.Corruption - Mathf.Max(0f, config.RestCorruptionReduction), 0f, Max);
    }

    private static void AddWear(DollEntity doll, float amount) {
        doll.Status.WearAndTear = Mathf.Clamp(doll.Status.WearAndTear + amount, 0f, Max);
    }

    private static void AddCorruption(DollEntity doll, float amount) {
        doll.Status.Corruption = Mathf.Clamp(doll.Status.Corruption + amount, 0f, Max);
    }
}
