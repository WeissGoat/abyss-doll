using System.Collections;
using UnityEngine;

// M4 playtest bug: loot dragged from the pickup tray slid under the backpack grid. On the real loot screen after a
// fight (with a whetstone added to the offer, so there is always something to drag), DragCheck holds a loot item over
// the backpack and reports what is drawn on top of it.
public static class HeldLootOnTopPlayScenario {
    public static IEnumerator Run() {
        yield return PlayScenarioSupport.BootToWorkshop();
        P3DevTools.GoTo("combat");
        yield return PlayScenarioSupport.Until(() => GameRoot.Core.Combat.CurrentState == CombatState.PlayerTurn, 10f, "combat did not start");
        // The fight is only the way to the loot screen: with every hit now played on screen, a long one would eat the
        // timeout, so each enemy goes down to the first blow.
        foreach (FighterEntity enemy in GameRoot.Core.Combat.EnemyFaction.Fighters) {
            enemy.RuntimeHP = Mathf.Min(enemy.RuntimeHP, 1);
        }

        P3DevTools.AutoCombat();
        yield return PlayScenarioSupport.Until(() => GameRoot.Core.Combat.CurrentState == CombatState.End && VisualQueue.Count == 0, 20f, "the fight did not finish");
        yield return new WaitForSecondsRealtime(1f);

        if (GameObject.Find("CombatLootPanel_Runtime") == null) {
            CombatLootPickupResult offer = new CombatLootPickupResult { NodeID = "play_scenario_loot" };
            offer.OfferedItems.Add(ConfigManager.CreateItem("gear_whetstone"));
            GameFlowController.Instance.EnterCombatLoot(offer);
        }

        yield return PlayScenarioSupport.Until(() => GameObject.Find("CombatLootPanel_Runtime") != null, 5f, "no loot screen");
        yield return new WaitForSecondsRealtime(0.5f);

        string report = P3DevTools.DragCheck();
        if (report.Contains("COVERED") || report.Contains("no item")) {
            Debug.LogError("[HeldLootOnTop] " + report);
        } else {
            Debug.Log("[HeldLootOnTop] " + report);
        }

        P3DevTools.TakeLoot();
    }
}
