using UnityEngine;

public static class DungeonSettlementTest {
    private static bool _eventFired = false;
    private static bool _isVictory = false;

    public static void Run() {
        try {
            Debug.Log("=== Running Dungeon Settlement Test ===");
            
            CoreBackend core = new CoreBackend();
            core.InitAllSystems();
            GameRoot.Core = core; 
            
            var player = core.CurrentPlayer;
            var doll = player.ActiveDoll;
            
            if (doll == null || doll.Chassis == null) {
                Debug.LogError("Bootstrap failed: ActiveDoll or Chassis is null.");
                return;
            }
            
            // Give player a backpack grid
            doll.RuntimeGrid = new BackpackGrid(doll.Chassis);
            
            // Put an item in the backpack
            var item = ConfigManager.CreateItem("mat_core_tier1");
            ((BackpackGrid)doll.RuntimeGrid).PlaceItem(item, 0, 0);
            
            Debug.Log($"[Before] Stash Count: {player.StashInventory.Count}, Backpack Count: {((BackpackGrid)doll.RuntimeGrid).ContainedItems.Count}");
            
            // Listen to Settlement Event
            _eventFired = false;
            DungeonEventBus.OnDungeonSettled += OnSettled;

            // Trigger Evacuation (Victory)
            DungeonEventBus.PublishDungeonEvacuated();
            
            // Wait, VisualQueue needs to be processed because events are enqueued!
            VisualQueue.IsHeadless = true;
            
            // Actually, PublishDungeonEvacuated enqueues commands. We need to manually process them if IsHeadless was not true at the moment, but it defaults to true.
            // Let's verify.
            
            Debug.Log($"[After Evacuate] Stash Count: {player.StashInventory.Count}, Backpack Count: {((BackpackGrid)doll.RuntimeGrid).ContainedItems.Count}");
            
            if (player.StashInventory.Count == 1 && ((BackpackGrid)doll.RuntimeGrid).ContainedItems.Count == 0) {
                Debug.Log("Evacuation Loot Transfer PASSED.");
            } else {
                Debug.LogError("Evacuation Loot Transfer FAILED.");
            }
            
            if (_eventFired && _isVictory) {
                Debug.Log("Evacuation Event Publishing PASSED.");
            } else {
                Debug.LogError("Evacuation Event Publishing FAILED.");
            }

            // Test Defeat Scenario
            _eventFired = false;
            ((BackpackGrid)doll.RuntimeGrid).PlaceItem(item, 0, 0); // Put it back
            
            DungeonEventBus.PublishDungeonDefeated();
            
            if (((BackpackGrid)doll.RuntimeGrid).ContainedItems.Count == 0) {
                Debug.Log("Defeat Loot Penalty PASSED.");
            } else {
                Debug.LogError("Defeat Loot Penalty FAILED.");
            }
            
            if (_eventFired && !_isVictory) {
                Debug.Log("Defeat Event Publishing PASSED.");
            } else {
                Debug.LogError("Defeat Event Publishing FAILED.");
            }

            DungeonEventBus.OnDungeonSettled -= OnSettled;
            Debug.Log("=== Dungeon Settlement Test Finished ===");
        } catch (System.Exception ex) {
            Debug.LogError($"[Test Crash] {ex.Message}\n{ex.StackTrace}");
        }
    }

    private static void OnSettled(bool isVictory) {
        _eventFired = true;
        _isVictory = isVictory;
        Debug.Log($"Received OnDungeonSettled event with isVictory={isVictory}");
    }
}