using UnityEngine;
using System.Collections.Generic;

public static class CombatQueueTest {
    public static void Run() {
        Debug.Log("=== Running Combat Queue Test ===");
        
        CoreBackend core = new CoreBackend();
        core.InitAllSystems();
        
        if (core.CurrentPlayer == null || core.CurrentPlayer.ActiveDoll == null) {
            Debug.LogError("Bootstrap failed.");
            return;
        }
        
        GameRoot.Core = core; // Set static ref
        
        // Ensure headless mode
        VisualQueue.IsHeadless = true;
        VisualQueue.Clear();
        
        // Start Combat
        core.Combat.StartCombat(new List<string> { "mob_scavenger_bug" });
        
        // Enemy HP should be 40
        var enemy = core.Combat.EnemyFaction.Fighters[0];
        Debug.Log($"Enemy initial HP: {enemy.RuntimeHP}");
        
        // Player attacks manually
        var weapon = ConfigManager.CreateItem("gear_tactical_blade");
        
        // [修正] 必须初始化并放入网格，计算出 RuntimeDamage，否则攻击力为 0
        core.CurrentPlayer.ActiveDoll.RuntimeGrid = new BackpackGrid(core.CurrentPlayer.ActiveDoll.Chassis);
        ((BackpackGrid)core.CurrentPlayer.ActiveDoll.RuntimeGrid).PlaceItem(weapon, 0, 0);
        GridSolver.RecalculateAllEffects(core.CurrentPlayer.ActiveDoll);
        int expectedDamage = (int)weapon.Combat.RuntimeDamage;
        int expectedRemainingHp = Mathf.Max(0, 40 - expectedDamage);
        
        core.Combat.PlayerFaction.Fighters[0].Attack(enemy, weapon);
        
        Debug.Log($"Enemy HP after attack: {enemy.RuntimeHP}");
        
        if (enemy.RuntimeHP == expectedRemainingHp) {
            Debug.Log("Combat Math PASSED.");
        } else {
            Debug.LogError($"Combat Math FAILED. Expected {expectedRemainingHp}, got {enemy.RuntimeHP}");
        }
        
        // Wait, IsHeadless should have consumed the queue instantly.
        if (VisualQueue.Count == 0) {
            Debug.Log("VisualQueue Headless Consumption PASSED.");
        } else {
            Debug.LogError($"VisualQueue has {VisualQueue.Count} items remaining! FAILED.");
        }

        Debug.Log("=== Combat Queue Test Finished ===");
    }
}
