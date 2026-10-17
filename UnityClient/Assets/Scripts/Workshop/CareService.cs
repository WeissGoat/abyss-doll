using System;
using System.Collections.Generic;
using UnityEngine;

public class BondChange {
    public int PointsAdded;
    public int LevelBefore;
    public int LevelAfter;
    public bool LeveledUp => LevelAfter > LevelBefore;
}

// The bond with Zero in the Demo: 0-5, raised by caring for her (docs/specs/2026-10-17-home-loop.md). It never goes
// down; leaving her be only means it does not grow. What each level opens comes in batch 4.
public static class BondService {
    public const int MaxLevel = 5;
    // Points needed to reach each level from the one before (level 1 needs 5, level 2 needs 9 more, ...).
    public static readonly int[] PointsToNext = { 5, 9, 12, 16, 20 };

    public static int Level(DollEntity doll) {
        return Mathf.Clamp(doll?.Bond?.AffectionLevel ?? 0, 0, MaxLevel);
    }

    public static int Points(DollEntity doll) {
        return Mathf.Max(0, doll?.Bond?.Points ?? 0);
    }

    // Points still needed for the next level (0 at the top).
    public static int Needed(DollEntity doll) {
        int level = Level(doll);
        return level >= MaxLevel ? 0 : PointsToNext[level];
    }

    public static BondChange Add(DollEntity doll, int points) {
        BondChange change = new BondChange { LevelBefore = Level(doll), LevelAfter = Level(doll) };
        if (doll == null || points <= 0) {
            return change;
        }

        doll.Bond = doll.Bond ?? new DollBondComponent();
        doll.Bond.AffectionLevel = Level(doll);
        int level = doll.Bond.AffectionLevel;
        int total = Points(doll) + points;
        while (level < MaxLevel && total >= PointsToNext[level]) {
            total -= PointsToNext[level];
            level++;
        }

        doll.Bond.AffectionLevel = level;
        doll.Bond.Points = level >= MaxLevel ? 0 : total;
        change.PointsAdded = points;
        change.LevelAfter = level;
        return change;
    }
}

public enum CareMarkKind {
    Grime = 0,
    Crack = 1,
    Haze = 2
}

// What a dive leaves on Zero and what caring for her takes off (docs/specs/2026-10-17-home-loop.md, batch 3): a smudge
// per fight, wiped off by hand for free; cracks by wear and a dark haze by corruption, shown in bands, fixed with
// the maintenance options (MaintenanceService). Rules only; the studio shows the marks.
public static class CareService {
    public const int MaxGrime = 6;
    public const int DefeatGrime = 2;
    public const int WipeSAN = 1;
    public const int WipeBond = 1;
    public static readonly float[] Bands = { 30f, 60f, 90f };

    public static void AddGrimeFromDive(DollEntity doll, DiveLog log) {
        if (doll?.Status == null || log == null) {
            return;
        }

        int fights = 0;
        foreach (DiveLogStep step in log.Steps) {
            if (step != null && (step.Kind == "战斗" || step.Kind == "精英" || step.Kind == "首领")) {
                fights++;
            }
        }

        doll.Status.Grime = Math.Min(MaxGrime, doll.Status.Grime + fights + (log.Defeated ? DefeatGrime : 0));
    }

    // One smudge wiped off: a little SAN and a bond point. False when she is already clean.
    public static bool Wipe(DollEntity doll, out BondChange bond) {
        bond = new BondChange { LevelBefore = BondService.Level(doll), LevelAfter = BondService.Level(doll) };
        if (doll?.Status == null || doll.Status.Grime <= 0) {
            return false;
        }

        doll.Status.Grime--;
        doll.Status.SAN_Current = Math.Min(doll.Status.SAN_Max, doll.Status.SAN_Current + WipeSAN);
        GameEventBus.PublishSANChanged(doll.Name, doll.Status.SAN_Current, doll.Status.SAN_Max);
        bond = BondService.Add(doll, WipeBond);
        return true;
    }

    // A smudge wiped by the player: also the clean-up wish, when that was the last one.
    public static bool Wipe(PlayerProfile player, out BondChange bond, out WishResult wish) {
        wish = null;
        bool wiped = Wipe(player?.ActiveDoll, out bond);
        if (wiped) {
            wish = WishService.OnClean(player);
        }

        return wiped;
    }

    // How many marks of a kind show: smudges one by one, cracks and haze by band (30 / 60 / 90).
    public static int Count(DollEntity doll, CareMarkKind kind) {
        if (doll?.Status == null) {
            return 0;
        }

        switch (kind) {
            case CareMarkKind.Grime:
                return Mathf.Clamp(doll.Status.Grime, 0, MaxGrime);
            case CareMarkKind.Crack:
                return Band(doll.Status.WearAndTear);
            default:
                return Band(doll.Status.Corruption);
        }
    }

    public static int Band(float value) {
        int band = 0;
        foreach (float threshold in Bands) {
            if (value >= threshold) {
                band++;
            }
        }

        return band;
    }

    // The maintenance options that treat a kind of mark: wear for cracks, corruption for the haze.
    public static List<MaintenanceConfig> Treatments(CareMarkKind kind) {
        string target = kind == CareMarkKind.Crack ? "Wear" : "Corruption";
        List<MaintenanceConfig> options = new List<MaintenanceConfig>();
        foreach (MaintenanceConfig config in ConfigManager.MaintenanceConfigs.Values) {
            if (config?.Effects != null && config.Effects.Exists(effect => effect != null && string.Equals(effect.TargetState, target, StringComparison.OrdinalIgnoreCase))) {
                options.Add(config);
            }
        }

        options.Sort((a, b) => (a.Cost?.Money ?? 0).CompareTo(b.Cost?.Money ?? 0));
        return options;
    }
}
