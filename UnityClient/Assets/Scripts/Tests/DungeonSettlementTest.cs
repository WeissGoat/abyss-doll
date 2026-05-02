using UnityEngine;

public static class DungeonSettlementTest {
    private static bool _eventFired = false;
    private static bool _isVictory = false;
    private static DungeonSettlementResult _lastSettlementResult = null;

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
            _lastSettlementResult = null;
            DungeonEventBus.OnDungeonSettled += OnSettled;
            DungeonEventBus.OnDungeonSettlementPrepared += OnSettlementPrepared;

            // Trigger Evacuation (Victory)
            DungeonEventBus.PublishDungeonEvacuated();

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

            if (_lastSettlementResult != null &&
                _lastSettlementResult.IsVictory &&
                _lastSettlementResult.LootTransferredCount == 1 &&
                _lastSettlementResult.StashCountAfterSettlement == 1) {
                Debug.Log("Evacuation Settlement Summary PASSED.");
            } else {
                Debug.LogError("Evacuation Settlement Summary FAILED.");
            }

            // Test Defeat Scenario
            _eventFired = false;
            _lastSettlementResult = null;
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

            if (_lastSettlementResult != null &&
                !_lastSettlementResult.IsVictory &&
                _lastSettlementResult.LootTransferredCount == 0 &&
                _lastSettlementResult.StashCountAfterSettlement == 1) {
                Debug.Log("Defeat Settlement Summary PASSED.");
            } else {
                Debug.LogError("Defeat Settlement Summary FAILED.");
            }

            if (VisualQueue.Count == 0) {
                Debug.Log("Dungeon Domain Events Bypass VisualQueue PASSED.");
            } else {
                Debug.LogError($"Dungeon Domain Events unexpectedly left {VisualQueue.Count} visual commands queued.");
            }

            DungeonEventBus.OnDungeonSettled -= OnSettled;
            DungeonEventBus.OnDungeonSettlementPrepared -= OnSettlementPrepared;
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

    private static void OnSettlementPrepared(DungeonSettlementResult result) {
        _lastSettlementResult = result;
        Debug.Log($"Received settlement summary. Victory={result.IsVictory}, LootCount={result.LootTransferredCount}");
    }
}
