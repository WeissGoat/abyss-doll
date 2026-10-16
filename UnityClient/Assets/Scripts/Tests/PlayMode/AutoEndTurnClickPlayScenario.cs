using System;
using System.Collections;
using UnityEngine;

// M4 playtest bug: the enemy turn resolves instantly, so an end-turn click landing just after the auto end ended the
// fresh player turn as well. Runs the real HUD: AP to zero, wait for the auto end, click at once, then click again
// after the grace.
public static class AutoEndTurnClickPlayScenario {
    public static IEnumerator Run() {
        yield return PlayScenarioSupport.BootToWorkshop();
        P3DevTools.GoTo("combat");
        HUDController hud = null;
        yield return PlayScenarioSupport.Until(() => {
            hud = UnityEngine.Object.FindObjectOfType<HUDController>();
            return hud != null && hud.endTurnBtn != null && GameRoot.Core.Combat.CurrentState == CombatState.PlayerTurn;
        }, 10f, "no combat HUD in a player turn");
        if (hud == null) {
            yield break;
        }

        // Let the turn-start grace from entering combat pass.
        yield return new WaitForSecondsRealtime(EndTurnClickGuard.GraceSeconds + 0.2f);

        CombatSystem combat = GameRoot.Core.Combat;
        int playerTurns = 0;
        Action<CombatEventType, CombatFaction> countTurns = (phase, faction) => {
            if (phase == CombatEventType.OnTurnStart && faction?.Type == FactionType.Player) {
                playerTurns++;
            }
        };

        CombatEventBus.OnCombatPhase += countTurns;
        try {
            DollFighter player = (DollFighter)combat.PlayerFaction.Fighters[0];
            player.CurrentAP = 0;
            if (combat.PlayerHasActionLeft()) {
                Debug.LogError("[AutoEndTurnClick] The backpack holds a 0 AP manual item, so the turn never auto ends; the scenario needs a backpack without one.");
                yield break;
            }

            GameEventBus.PublishAPChanged(player.Name, 0, player.MaxAP);
            yield return PlayScenarioSupport.Until(() => playerTurns >= 1 || combat.CurrentState == CombatState.End, 5f, "the turn did not auto end with 0 AP");

            // The player's click lands in the same frame the new turn starts.
            hud.endTurnBtn.onClick.Invoke();
            yield return new WaitForSecondsRealtime(EndTurnClickGuard.GraceSeconds + 0.4f);
            if (combat.CurrentState == CombatState.End) {
                Debug.LogWarning("[AutoEndTurnClick] Combat ended during the enemy turn; inconclusive.");
                yield break;
            }

            if (playerTurns != 1) {
                Debug.LogError($"[AutoEndTurnClick] A click right after the auto end ended the new turn too ({playerTurns} player turns started).");
                yield break;
            }

            // A deliberate click after the grace still ends the turn.
            hud.endTurnBtn.onClick.Invoke();
            yield return null;
            if (combat.CurrentState == CombatState.PlayerTurn && playerTurns != 2) {
                Debug.LogError($"[AutoEndTurnClick] A click after the grace did not end the turn ({playerTurns} player turns started).");
            }
        } finally {
            CombatEventBus.OnCombatPhase -= countTurns;
        }
    }
}
