using System.Collections.Generic;
using UnityEngine;

// Caring for Zero (docs/specs/2026-10-17-home-loop.md, batch 3): a dive leaves a smudge per fight, wiping one is free
// and gives SAN and a bond point, cracks and haze follow wear and corruption in bands with their own treatments, the
// flower oil adds bond, and the bond climbs its levels without going past 5.
public static class HomeCareTest {
    public static void Run() {
        Debug.Log("=== Running Home Care Test ===");

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();
        DayCycleService.TakePendingReport();

        PlayerProfile player = core.CurrentPlayer;
        DollEntity doll = player.ActiveDoll;
        List<string> failures = new List<string>();
        void Check(bool condition, string message) {
            if (!condition) {
                failures.Add(message);
            }
        }

        // Smudges: one per fight, two more on a defeat, at most six.
        doll.Status.Grime = 0;
        DiveLog log = new DiveLog();
        log.Steps.Add(new DiveLogStep { Kind = "战斗" });
        log.Steps.Add(new DiveLogStep { Kind = "宝藏" });
        log.Steps.Add(new DiveLogStep { Kind = "首领" });
        CareService.AddGrimeFromDive(doll, log);
        Check(doll.Status.Grime == 2, $"two fights left {doll.Status.Grime} smudge(s)");
        log.Defeated = true;
        CareService.AddGrimeFromDive(doll, log);
        CareService.AddGrimeFromDive(doll, log);
        Check(doll.Status.Grime == CareService.MaxGrime, $"smudges went to {doll.Status.Grime}");

        // Wiping: free, SAN +1, a bond point; nothing to wipe when clean.
        doll.Bond = new DollBondComponent();
        doll.Status.SAN_Current = 10;
        int money = player.Money;
        Check(CareService.Wipe(doll, out _) && doll.Status.Grime == CareService.MaxGrime - 1 && doll.Status.SAN_Current == 11
            && BondService.Points(doll) == 1 && player.Money == money, "wipe did not take one smudge for SAN and a bond point");
        doll.Status.Grime = 0;
        Check(!CareService.Wipe(doll, out _), "wiped a clean doll");

        // Cracks and haze by band.
        doll.Status.WearAndTear = 65f;
        doll.Status.Corruption = 29f;
        Check(CareService.Count(doll, CareMarkKind.Crack) == 2 && CareService.Count(doll, CareMarkKind.Haze) == 0, "bands wrong");
        Check(CareService.Treatments(CareMarkKind.Crack).Exists(config => config.MaintenanceID == "maint_flower_oil")
            && !CareService.Treatments(CareMarkKind.Haze).Exists(config => config.MaintenanceID == "maint_flower_oil"), "treatments are not split by mark");

        // The flower oil mends more and adds bond.
        player.Money = 1000;
        MaintenanceApplicationResult oil = MaintenanceService.Apply(player, doll, "maint_flower_oil");
        Check(oil.Success && doll.Status.WearAndTear <= 20f && oil.Bond != null && oil.Bond.PointsAdded == 3, $"flower oil: {oil.Reason}, wear {doll.Status.WearAndTear}");

        // Levels: 5 points to level 1, then 9; the top is 5 and stays there.
        doll.Bond = new DollBondComponent();
        BondChange first = BondService.Add(doll, 5);
        Check(first.LeveledUp && BondService.Level(doll) == 1 && BondService.Points(doll) == 0, "5 points did not make level 1");
        BondService.Add(doll, 8);
        Check(BondService.Level(doll) == 1 && BondService.Points(doll) == 8 && BondService.Needed(doll) == 9, "level 1 progress wrong");
        BondService.Add(doll, 1000);
        Check(BondService.Level(doll) == BondService.MaxLevel && BondService.Needed(doll) == 0, "bond did not stop at the top");

        // Smudges and bond are saved.
        doll.Status.Grime = 3;
        SaveFile restored = SaveService.Deserialize(SaveService.Serialize(player));
        Check(restored.Player.ActiveDoll.Status.Grime == 3 && restored.Player.ActiveDoll.Bond.AffectionLevel == BondService.MaxLevel, "care state lost in the save");

        if (failures.Count == 0) {
            Debug.Log("Home Care PASSED.");
        } else {
            Debug.LogError("Home Care FAILED: " + string.Join("; ", failures));
        }

        Debug.Log("=== Home Care Test Finished ===");
    }
}
