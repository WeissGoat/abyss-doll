using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

// Shared steps for the Play Mode scenarios (run by Assets/Tests/PlayMode/P3PlayTests). The game boots itself
// (PlayModeRuntimeEntrySceneBootstrap loads SampleScene), so a scenario waits for it and walks on with P3DevTools.
public static class PlayScenarioSupport {
    public const string MainScene = "SampleScene";
    public const int ScenarioSeed = 20261021;

    // To the workshop from wherever the previous scenario left the game: title, prologue, a dive or a fight.
    public static IEnumerator BootToWorkshop() {
        if (SceneManager.GetActiveScene().name != MainScene) {
            SceneManager.LoadScene(MainScene);
        }

        yield return Until(() => GameRoot.Core?.CurrentPlayer != null && GameFlowController.Instance != null, 30f, "the game did not boot");
        yield return new WaitForSecondsRealtime(0.5f);
        if (TitleScreen.IsShowing) {
            P3DevTools.Click(TitleScreen.NewGameLabel);
            yield return new WaitForSecondsRealtime(1f);
        }

        if (UnityEngine.Object.FindObjectOfType<PrologueFirstDiveController>() != null) {
            P3DevTools.SkipPrologue();
            yield return new WaitForSecondsRealtime(0.5f);
        }

        P3DevTools.GoTo("workshop");
        yield return null;
    }

    // Wins a fight and opens the loot screen with one-cell whetstones on offer (count) and at least one item in the
    // backpack, whatever the save and the fight's own drop were (the drop can be any shape; the backpack can be empty).
    // The fight's rolls come from a fixed GameRandom seed, and a full backpack is thinned to leave a free cell, so the
    // setup does not depend on what else ran first or how much the drop was.
    public static IEnumerator OpenWhetstoneLoot(string nodeID, int count = 1) {
        GameRandom.Seed(ScenarioSeed);
        P3DevTools.GoTo("combat");
        yield return Until(() => GameRoot.Core.Combat.CurrentState == CombatState.PlayerTurn, 10f, "combat did not start");
        foreach (FighterEntity enemy in GameRoot.Core.Combat.EnemyFaction.Fighters) {
            enemy.RuntimeHP = Mathf.Min(enemy.RuntimeHP, 1);
        }

        P3DevTools.AutoCombat();
        yield return Until(() => GameRoot.Core.Combat.CurrentState == CombatState.End && VisualQueue.Count == 0, 20f, "the fight did not finish");
        yield return new WaitForSecondsRealtime(1f);
        if (GameObject.Find("CombatLootPanel_Runtime") != null) {
            P3DevTools.TakeLoot();
            yield return Until(() => GameObject.Find("CombatLootPanel_Runtime") == null, 5f, "the loot screen did not close");
            yield return new WaitForSecondsRealtime(0.5f);
        }

        BackpackGrid grid = GameRoot.Core.CurrentPlayer.ActiveDoll.RuntimeGrid as BackpackGrid;
        if (grid != null && grid.ContainedItems.Count == 0) {
            InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll("PlayScenario");
            InventoryInteractionService.RequestPlace(ConfigManager.CreateItem("gear_whetstone"), 0, 0, context, out _);
        }

        // A free cell for the offer: the fight's drop can fill the backpack, so take out the latest items, keeping one.
        if (grid != null) {
            InventoryInteractionContext context = InventoryInteractionContext.FromCurrentDoll("PlayScenario");
            ItemEntity probe = ConfigManager.CreateItem("gear_whetstone");
            while (grid.ContainedItems.Count > 1 && !grid.TryFindFirstAvailable(probe, out _, out _)) {
                if (!InventoryInteractionService.RequestPickUp(grid.ContainedItems[grid.ContainedItems.Count - 1], context, out _, out _)) {
                    break;
                }
            }
        }

        CombatLootPickupResult offer = new CombatLootPickupResult { NodeID = nodeID };
        for (int i = 0; i < count; i++) {
            offer.OfferedItems.Add(ConfigManager.CreateItem("gear_whetstone"));
        }

        GameFlowController.Instance.EnterCombatLoot(offer);
        yield return Until(() => GameObject.Find("CombatLootPanel_Runtime") != null, 5f, "no loot screen");
        yield return new WaitForSecondsRealtime(0.5f);
    }

    // Waits (real time) until the condition holds; logs an error and stops the scenario's wait on timeout.
    public static IEnumerator Until(Func<bool> condition, float timeoutSeconds, string failure) {
        float start = Time.realtimeSinceStartup;
        while (!condition()) {
            if (Time.realtimeSinceStartup - start > timeoutSeconds) {
                Debug.LogError($"[PlayScenario] Timed out after {timeoutSeconds}s: {failure}\n{P3DevTools.Describe()}");
                yield break;
            }

            yield return null;
        }
    }
}
