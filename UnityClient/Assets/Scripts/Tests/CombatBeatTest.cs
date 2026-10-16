using System.Collections.Generic;
using UnityEngine;

// Combat beats are what the screen animates: the player's strike then the enemy being hit, and in the enemy turn each
// enemy's strike followed by its hit on Zero before the next enemy acts, with the numbers the floating text shows.
public static class CombatBeatTest {
    public static void Run() {
        Debug.Log("=== Running Combat Beat Test ===");
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();

        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        GameRoot.Core = core;

        List<CombatBeat> beats = new List<CombatBeat>();
        GameEventBus.OnCombatBeat += beats.Add;
        try {
            core.Combat.StartCombat(new List<string> { "mob_scavenger_bug", "mob_scavenger_bug" });
            DollFighter player = (DollFighter)core.Combat.PlayerFaction.Fighters[0];
            FighterEntity first = core.Combat.EnemyFaction.Fighters[0];
            FighterEntity second = core.Combat.EnemyFaction.Fighters[1];

            ItemEntity weapon = ConfigManager.CreateItem("gear_tactical_blade");
            core.CurrentPlayer.ActiveDoll.RuntimeGrid = new BackpackGrid(core.CurrentPlayer.ActiveDoll.Chassis);
            ((BackpackGrid)core.CurrentPlayer.ActiveDoll.RuntimeGrid).PlaceItem(weapon, 0, 0);
            GridSolver.RecalculateAllEffects(core.CurrentPlayer.ActiveDoll);

            beats.Clear();
            int hpBefore = second.RuntimeHP;
            player.Attack(second, weapon);
            Expect(beats.Count == 2, $"player attack publishes 2 beats, got {beats.Count}");
            if (beats.Count == 2) {
                Expect(beats[0].Kind == CombatBeatKind.Strike && beats[0].Actor == player && beats[0].Target == second,
                    "first beat is the player's strike on the chosen enemy");
                Expect(beats[1].Kind == CombatBeatKind.Hit && beats[1].Target == second && beats[1].Target != first,
                    "second beat is the hit on that same enemy object, not the other one with the same name");
                Expect(beats[1].HpLoss == hpBefore - second.RuntimeHP, $"hit HpLoss {beats[1].HpLoss} matches the HP lost {hpBefore - second.RuntimeHP}");
            }

            beats.Clear();
            core.Combat.EndPlayerTurn();
            List<CombatBeat> strikes = beats.FindAll(b => b.Kind == CombatBeatKind.Strike || b.Kind == CombatBeatKind.Interfere);
            Expect(strikes.Count > 0, "the enemy turn publishes at least one strike");
            int lastIndex = -1;
            for (int i = 0; i < beats.Count; i++) {
                CombatBeat beat = beats[i];
                if (beat.Kind == CombatBeatKind.Strike || beat.Kind == CombatBeatKind.Interfere) {
                    int index = core.Combat.EnemyFaction.Fighters.IndexOf(beat.Actor);
                    Expect(index >= lastIndex, $"enemies act in order (beat {i} from enemy {index} after enemy {lastIndex})");
                    lastIndex = index;
                }

                if (beat.Kind == CombatBeatKind.Strike && beat.Target == player) {
                    CombatBeat hit = i + 1 < beats.Count ? beats[i + 1] : null;
                    Expect(hit != null && hit.Kind == CombatBeatKind.Hit && hit.Target == player,
                        $"an enemy strike on Zero is followed straight away by its hit (beat {i})");
                    if (hit != null && hit.Kind == CombatBeatKind.Hit && !hit.Defeated) {
                        Expect(hit.HpLoss + hit.ShieldLoss == hit.Amount,
                            $"hit splits its {hit.Amount} damage into HP {hit.HpLoss} and shield {hit.ShieldLoss}");
                    }
                }
            }

            beats.Clear();
            player.AddShield(5);
            Expect(beats.Count == 1 && beats[0].Kind == CombatBeatKind.Shield && beats[0].Amount == 5, "gaining shield publishes a shield beat");

            Expect(CombatBeat.HoldSeconds(CombatBeatKind.Strike) + CombatBeat.HoldSeconds(CombatBeatKind.Hit) >= 0.5f,
                "each attack holds the queue for at least half a second, so enemies act one at a time");
        } finally {
            GameEventBus.OnCombatBeat -= beats.Add;
            VisualQueue.Clear();
        }

        Debug.Log("=== Combat Beat Test Finished ===");
    }

    private static void Expect(bool condition, string message) {
        if (condition) {
            Debug.Log("PASSED: " + message);
        } else {
            Debug.LogError("FAILED: " + message);
        }
    }
}
